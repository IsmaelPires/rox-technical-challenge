using Microsoft.EntityFrameworkCore;
using Rox.FinancialControl.Application.Balances;
using Rox.FinancialControl.Application.CashEntries;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Common;
using Rox.FinancialControl.Domain.Entries;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Api.BusinessValidation;

public sealed class BusinessValidationRunner(
    CreateCashEntryHandler createCashEntryHandler,
    ApplicationDbContext dbContext,
    ILogger<BusinessValidationRunner> logger)
{
    public async Task<BusinessValidationRunResult> RunAsync(
        StartBusinessValidationRequest request,
        CancellationToken cancellationToken)
    {
        var configuration = Normalize(request);
        var runId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;
        var steps = new List<BusinessValidationStep>();

        logger.LogInformation(
            "Starting business validation run {RunId} for {EntriesCount} entries on {BusinessDate}.",
            runId,
            configuration.EntriesCount,
            configuration.BusinessDate);

        if (configuration.IncludeInvalidCases)
        {
            steps.Add(ValidateInvalidCashEntryAmount());
            steps.Add(ValidateInvalidDailyBalanceDate(configuration.BusinessDate));
        }

        var baseline = await GetBalanceSnapshotAsync(configuration.BusinessDate, cancellationToken);
        var creditCount = CalculateCreditCount(configuration.EntriesCount, configuration.CreditPercentage);
        var debitCount = configuration.EntriesCount - creditCount;
        var expectedCredits = creditCount * configuration.CreditAmount;
        var expectedDebits = debitCount * configuration.DebitAmount;
        var createdEntries = 0;

        for (var index = 1; index <= configuration.EntriesCount; index++)
        {
            var isCredit = index <= creditCount;
            var amount = isCredit ? configuration.CreditAmount : configuration.DebitAmount;
            var type = isCredit ? "Credit" : "Debit";

            await createCashEntryHandler.HandleAsync(
                new CreateCashEntryRequest(
                    configuration.BusinessDate,
                    type,
                    amount,
                    $"Validação funcional {runId:N} #{index}",
                    startedAt.AddMilliseconds(index),
                    CashEntryOrigin.Validation.ToString()),
                cancellationToken);

            createdEntries++;
        }

        steps.Add(new BusinessValidationStep(
            "Registro de lançamentos",
            createdEntries == configuration.EntriesCount,
            $"{configuration.EntriesCount} lançamentos",
            $"{createdEntries} lançamentos"));

        var expected = baseline with
        {
            TotalCredits = baseline.TotalCredits + expectedCredits,
            TotalDebits = baseline.TotalDebits + expectedDebits,
            EntriesCount = baseline.EntriesCount + configuration.EntriesCount
        };

        var observed = await WaitForConsolidationAsync(
            configuration.BusinessDate,
            expected,
            TimeSpan.FromSeconds(configuration.TimeoutSeconds),
            cancellationToken);

        steps.Add(new BusinessValidationStep(
            "Consolidação diária",
            observed.Matches(expected),
            FormatBalance(expected),
            FormatBalance(observed),
            "Confere totais de crédito, débito e quantidade de lançamentos consolidados."));

        steps.Add(new BusinessValidationStep(
            "Saldo consolidado",
            observed.Balance == expected.Balance,
            expected.Balance.ToString("F2"),
            observed.Balance.ToString("F2")));

        var pendingOutboxMessages = await dbContext.OutboxMessages
            .AsNoTracking()
            .CountAsync(
                message => message.ProcessedAt == null && message.Payload.Contains(runId.ToString("N")),
                cancellationToken);

        steps.Add(new BusinessValidationStep(
            "Publicação via outbox",
            pendingOutboxMessages == 0,
            "0 mensagens pendentes do cenário",
            $"{pendingOutboxMessages} mensagens pendentes do cenário"));

        var finishedAt = DateTimeOffset.UtcNow;
        var totals = new BusinessValidationTotals(
            createdEntries,
            expectedCredits,
            expectedDebits,
            expectedCredits - expectedDebits,
            observed.TotalCredits - baseline.TotalCredits,
            observed.TotalDebits - baseline.TotalDebits,
            (observed.TotalCredits - baseline.TotalCredits) - (observed.TotalDebits - baseline.TotalDebits),
            observed.EntriesCount - baseline.EntriesCount);

        return new BusinessValidationRunResult(
            runId,
            steps.All(step => step.Passed),
            startedAt,
            finishedAt,
            configuration,
            totals,
            steps);
    }

    private static BusinessValidationConfiguration Normalize(StartBusinessValidationRequest request)
    {
        if (request.EntriesCount is < 1 or > 100)
        {
            throw new ValidationException("Informe entre 1 e 100 lançamentos para validação.");
        }

        if (request.CreditPercentage is < 0 or > 100)
        {
            throw new ValidationException("Informe um percentual de créditos entre 0 e 100.");
        }

        if (request.CreditAmount <= 0 || request.DebitAmount <= 0)
        {
            throw new ValidationException("Os valores de crédito e débito devem ser maiores que zero.");
        }

        if (request.TimeoutSeconds is < 5 or > 120)
        {
            throw new ValidationException("Informe um timeout entre 5 e 120 segundos.");
        }

        return new BusinessValidationConfiguration(
            request.EntriesCount,
            request.CreditPercentage,
            decimal.Round(request.CreditAmount, 2),
            decimal.Round(request.DebitAmount, 2),
            request.BusinessDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            request.TimeoutSeconds,
            request.IncludeInvalidCases);
    }

    private static int CalculateCreditCount(int entriesCount, int creditPercentage)
    {
        return (int)Math.Round(entriesCount * (creditPercentage / 100m), MidpointRounding.AwayFromZero);
    }

    private static BusinessValidationStep ValidateInvalidCashEntryAmount()
    {
        var now = DateTimeOffset.UtcNow;

        try
        {
            CashEntry.Create(
                DateOnly.FromDateTime(now.Date),
                CashEntryType.Credit,
                CashEntryOrigin.Validation,
                0,
                "Cenário inválido",
                now,
                now);

            return new BusinessValidationStep(
                "Regra de valor obrigatório",
                false,
                "DomainException",
                "Nenhuma exceção");
        }
        catch (DomainException)
        {
            return new BusinessValidationStep(
                "Regra de valor obrigatório",
                true,
                "DomainException",
                "DomainException");
        }
    }

    private static BusinessValidationStep ValidateInvalidDailyBalanceDate(DateOnly businessDate)
    {
        try
        {
            var balance = DailyBalance.Create(businessDate, CashEntryOrigin.Validation, DateTimeOffset.UtcNow);
            balance.Apply(
                new CashEntrySnapshot(
                    Guid.NewGuid(),
                    businessDate.AddDays(1),
                    CashEntryType.Credit,
                    CashEntryOrigin.Validation,
                    10m),
                DateTimeOffset.UtcNow);

            return new BusinessValidationStep(
                "Regra de data da consolidação",
                false,
                "DomainException",
                "Nenhuma exceção");
        }
        catch (DomainException)
        {
            return new BusinessValidationStep(
                "Regra de data da consolidação",
                true,
                "DomainException",
                "DomainException");
        }
    }

    private async Task<BalanceSnapshot> WaitForConsolidationAsync(
        DateOnly businessDate,
        BalanceSnapshot expected,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var observed = await GetBalanceSnapshotAsync(businessDate, cancellationToken);

        while (!observed.Matches(expected) && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            observed = await GetBalanceSnapshotAsync(businessDate, cancellationToken);
        }

        return observed;
    }

    private async Task<BalanceSnapshot> GetBalanceSnapshotAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var balance = await dbContext.DailyBalances
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.BusinessDate == businessDate && item.Origin == CashEntryOrigin.Validation,
                cancellationToken);

        return balance is null
            ? new BalanceSnapshot(0, 0, 0)
            : new BalanceSnapshot(balance.TotalCredits, balance.TotalDebits, balance.EntriesCount);
    }

    private static string FormatBalance(BalanceSnapshot snapshot)
    {
        return $"créditos {snapshot.TotalCredits:F2}, débitos {snapshot.TotalDebits:F2}, lançamentos {snapshot.EntriesCount}";
    }

    private sealed record BalanceSnapshot(decimal TotalCredits, decimal TotalDebits, int EntriesCount)
    {
        public decimal Balance => TotalCredits - TotalDebits;

        public bool Matches(BalanceSnapshot expected)
        {
            return TotalCredits == expected.TotalCredits
                && TotalDebits == expected.TotalDebits
                && EntriesCount == expected.EntriesCount;
        }
    }
}
