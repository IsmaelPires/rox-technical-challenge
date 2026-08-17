using Rox.FinancialControl.Application.Common;

namespace Rox.FinancialControl.Api.LoadSimulation;

public sealed class LoadSimulationState
{
    private readonly Lock sync = new();
    private readonly Queue<LoadSimulationError> lastErrors = new();

    private bool isRunning;
    private bool isBatchRunning;
    private LoadSimulationConfiguration? configuration;
    private DateTimeOffset? startedAt;
    private DateTimeOffset? stoppedAt;
    private DateTimeOffset? lastRunAt;
    private DateTimeOffset? nextRunAt;
    private int batchesExecuted;
    private int totalRequested;
    private int totalSucceeded;
    private int totalFailed;

    public LoadSimulationStatus Start(StartLoadSimulationRequest request, DateTimeOffset now)
    {
        var normalized = Normalize(request);

        lock (sync)
        {
            configuration = normalized;
            isRunning = true;
            isBatchRunning = false;
            startedAt = now;
            stoppedAt = null;
            lastRunAt = null;
            nextRunAt = now;
            batchesExecuted = 0;
            totalRequested = 0;
            totalSucceeded = 0;
            totalFailed = 0;
            lastErrors.Clear();

            return BuildStatus();
        }
    }

    public LoadSimulationStatus Stop(DateTimeOffset now)
    {
        lock (sync)
        {
            isRunning = false;
            stoppedAt = now;
            nextRunAt = null;

            return BuildStatus();
        }
    }

    public LoadSimulationConfiguration? TryReserveDueBatch(DateTimeOffset now)
    {
        lock (sync)
        {
            if (!isRunning || isBatchRunning || configuration is null || nextRunAt is null || nextRunAt > now)
            {
                return null;
            }

            isBatchRunning = true;
            nextRunAt = null;
            return configuration;
        }
    }

    public void CompleteBatch(
        int requested,
        int succeeded,
        IReadOnlyCollection<LoadSimulationError> errors,
        DateTimeOffset now)
    {
        lock (sync)
        {
            batchesExecuted++;
            totalRequested += requested;
            totalSucceeded += succeeded;
            totalFailed += errors.Count;
            lastRunAt = now;
            isBatchRunning = false;

            foreach (var error in errors)
            {
                lastErrors.Enqueue(error);
                while (lastErrors.Count > 8)
                {
                    lastErrors.Dequeue();
                }
            }

            if (configuration?.MaxBatches is not null && batchesExecuted >= configuration.MaxBatches.Value)
            {
                isRunning = false;
                stoppedAt = now;
                nextRunAt = null;
                return;
            }

            if (isRunning && configuration is not null)
            {
                nextRunAt = now.AddSeconds(configuration.IntervalSeconds);
            }
        }
    }

    public LoadSimulationStatus GetStatus()
    {
        lock (sync)
        {
            return BuildStatus();
        }
    }

    private LoadSimulationStatus BuildStatus()
    {
        return new LoadSimulationStatus(
            isRunning,
            isBatchRunning,
            configuration,
            startedAt,
            stoppedAt,
            lastRunAt,
            nextRunAt,
            batchesExecuted,
            totalRequested,
            totalSucceeded,
            totalFailed,
            lastErrors.ToArray());
    }

    private static LoadSimulationConfiguration Normalize(StartLoadSimulationRequest request)
    {
        if (request.RequestsPerBatch is < 1 or > 250)
        {
            throw new ValidationException("Informe entre 1 e 250 requisições por rodada.");
        }

        if (request.IntervalSeconds is < 5 or > 3600)
        {
            throw new ValidationException("Informe um intervalo entre 5 e 3600 segundos.");
        }

        if (request.MaxBatches is not null and (< 1 or > 1000))
        {
            throw new ValidationException("Informe no máximo 1000 rodadas.");
        }

        if (request.CreditPercentage is < 0 or > 100)
        {
            throw new ValidationException("Informe um percentual de créditos entre 0 e 100.");
        }

        if (request.MinAmount <= 0 || request.MaxAmount <= 0 || request.MinAmount > request.MaxAmount)
        {
            throw new ValidationException("Informe uma faixa de valores válida.");
        }

        return new LoadSimulationConfiguration(
            request.RequestsPerBatch,
            request.IntervalSeconds,
            request.MaxBatches,
            request.CreditPercentage,
            request.MinAmount,
            request.MaxAmount,
            request.BusinessDate);
    }
}
