using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rox.FinancialControl.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task EnsureDatabaseCreatedAsync(
        this IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 30;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                logger.LogInformation("Ensuring database schema exists. Attempt {Attempt}/{MaxAttempts}.", attempt, maxAttempts);
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                await EnsureOperationalSchemaUpdatesAsync(dbContext, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database is not ready yet. Retrying in 2 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private static async Task EnsureOperationalSchemaUpdatesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DECLARE @SchemaUpdateLockResult int;

            EXEC @SchemaUpdateLockResult = sp_getapplock
                @Resource = N'rox-financial-control-schema-update',
                @LockMode = N'Exclusive',
                @LockOwner = N'Session',
                @LockTimeout = 30000;

            IF @SchemaUpdateLockResult < 0
            BEGIN
                THROW 51001, 'Could not acquire schema update application lock.', 1;
            END;

            BEGIN TRY
            IF COL_LENGTH('cash_entries', 'Origin') IS NULL
            BEGIN
                ALTER TABLE cash_entries
                ADD Origin nvarchar(32) NOT NULL
                    CONSTRAINT DF_cash_entries_Origin DEFAULT 'Business';
            END;

            IF COL_LENGTH('daily_balances', 'Origin') IS NULL
            BEGIN
                ALTER TABLE daily_balances
                ADD Origin nvarchar(32) NOT NULL
                    CONSTRAINT DF_daily_balances_Origin DEFAULT 'Business';
            END;

            DECLARE @DailyBalancePkName sysname;

            SELECT @DailyBalancePkName = keys.name
            FROM sys.key_constraints keys
            WHERE keys.parent_object_id = OBJECT_ID('daily_balances')
              AND keys.type = 'PK';

            IF @DailyBalancePkName IS NOT NULL
               AND NOT EXISTS
               (
                   SELECT 1
                   FROM sys.index_columns columns
                   INNER JOIN sys.columns table_columns
                       ON table_columns.object_id = columns.object_id
                      AND table_columns.column_id = columns.column_id
                   WHERE columns.object_id = OBJECT_ID('daily_balances')
                     AND columns.index_id =
                     (
                         SELECT unique_index_id
                         FROM sys.key_constraints
                         WHERE name = @DailyBalancePkName
                     )
                     AND table_columns.name = 'Origin'
               )
            BEGIN
                EXEC('ALTER TABLE daily_balances DROP CONSTRAINT [' + @DailyBalancePkName + ']');
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.key_constraints keys
                WHERE keys.parent_object_id = OBJECT_ID('daily_balances')
                  AND keys.type = 'PK'
            )
            BEGIN
                ALTER TABLE daily_balances
                ADD CONSTRAINT PK_daily_balances PRIMARY KEY (BusinessDate, Origin);
            END;

            DECLARE @ReclassifiedProcessedEntries TABLE
            (
                BusinessDate date NOT NULL,
                TargetOrigin nvarchar(32) NOT NULL,
                Type nvarchar(16) NOT NULL,
                Amount decimal(18, 2) NOT NULL
            );

            INSERT INTO @ReclassifiedProcessedEntries (BusinessDate, TargetOrigin, Type, Amount)
            SELECT
                entries.BusinessDate,
                CASE
                    WHEN entries.Description LIKE N'Validação funcional %'
                      OR entries.Description LIKE N'Validacao funcional %'
                        THEN N'Validation'
                    ELSE N'LoadSimulation'
                END,
                entries.Type,
                entries.Amount
            FROM cash_entries entries
            INNER JOIN processed_cash_entries processed_entries
                ON processed_entries.CashEntryId = entries.Id
            WHERE entries.Origin = N'Business'
              AND (
                  entries.Description LIKE N'Validação funcional %'
                  OR entries.Description LIKE N'Validacao funcional %'
                  OR entries.Description LIKE N'Simulação de carga - %'
                  OR entries.Description LIKE N'Simulacao de carga - %'
              );

            IF EXISTS (SELECT 1 FROM @ReclassifiedProcessedEntries)
            BEGIN
                WITH BusinessAdjustments AS
                (
                    SELECT
                        BusinessDate,
                        SUM(CASE WHEN Type = N'Credit' THEN Amount ELSE 0 END) AS TotalCredits,
                        SUM(CASE WHEN Type = N'Debit' THEN Amount ELSE 0 END) AS TotalDebits,
                        COUNT(1) AS EntriesCount
                    FROM @ReclassifiedProcessedEntries
                    GROUP BY BusinessDate
                )
                UPDATE balances
                SET TotalCredits = balances.TotalCredits - adjustments.TotalCredits,
                    TotalDebits = balances.TotalDebits - adjustments.TotalDebits,
                    EntriesCount = balances.EntriesCount - adjustments.EntriesCount,
                    LastUpdatedAt = SYSUTCDATETIME()
                FROM daily_balances balances
                INNER JOIN BusinessAdjustments adjustments
                    ON adjustments.BusinessDate = balances.BusinessDate
                WHERE balances.Origin = N'Business';

                WITH TargetAdjustments AS
                (
                    SELECT
                        BusinessDate,
                        TargetOrigin,
                        SUM(CASE WHEN Type = N'Credit' THEN Amount ELSE 0 END) AS TotalCredits,
                        SUM(CASE WHEN Type = N'Debit' THEN Amount ELSE 0 END) AS TotalDebits,
                        COUNT(1) AS EntriesCount
                    FROM @ReclassifiedProcessedEntries
                    GROUP BY BusinessDate, TargetOrigin
                )
                MERGE daily_balances AS target
                USING TargetAdjustments AS source
                    ON target.BusinessDate = source.BusinessDate
                   AND target.Origin = source.TargetOrigin
                WHEN MATCHED THEN
                    UPDATE SET
                        TotalCredits = target.TotalCredits + source.TotalCredits,
                        TotalDebits = target.TotalDebits + source.TotalDebits,
                        EntriesCount = target.EntriesCount + source.EntriesCount,
                        LastUpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (BusinessDate, Origin, TotalCredits, TotalDebits, EntriesCount, LastUpdatedAt)
                    VALUES (source.BusinessDate, source.TargetOrigin, source.TotalCredits, source.TotalDebits, source.EntriesCount, SYSUTCDATETIME());
            END;

            UPDATE cash_entries
            SET Origin = N'Validation'
            WHERE Origin = N'Business'
              AND (
                  Description LIKE N'Validação funcional %'
                  OR Description LIKE N'Validacao funcional %'
              );

            UPDATE cash_entries
            SET Origin = N'LoadSimulation'
            WHERE Origin = N'Business'
              AND (
                  Description LIKE N'Simulação de carga - %'
                  OR Description LIKE N'Simulacao de carga - %'
              );

            EXEC sp_releaseapplock
                @Resource = N'rox-financial-control-schema-update',
                @LockOwner = N'Session';
            END TRY
            BEGIN CATCH
                EXEC sp_releaseapplock
                    @Resource = N'rox-financial-control-schema-update',
                    @LockOwner = N'Session';

                THROW;
            END CATCH;
            """,
            cancellationToken);
    }
}
