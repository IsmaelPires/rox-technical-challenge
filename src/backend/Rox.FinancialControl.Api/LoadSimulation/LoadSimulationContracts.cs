namespace Rox.FinancialControl.Api.LoadSimulation;

public sealed record StartLoadSimulationRequest(
    int RequestsPerBatch,
    int IntervalSeconds,
    int? MaxBatches,
    int CreditPercentage,
    decimal MinAmount,
    decimal MaxAmount,
    DateOnly? BusinessDate);

public sealed record LoadSimulationConfiguration(
    int RequestsPerBatch,
    int IntervalSeconds,
    int? MaxBatches,
    int CreditPercentage,
    decimal MinAmount,
    decimal MaxAmount,
    DateOnly? BusinessDate);

public sealed record LoadSimulationError(
    DateTimeOffset OccurredAt,
    int StatusCode,
    string Message);

public sealed record LoadSimulationStatus(
    bool IsRunning,
    bool IsBatchRunning,
    LoadSimulationConfiguration? Configuration,
    DateTimeOffset? StartedAt,
    DateTimeOffset? StoppedAt,
    DateTimeOffset? LastRunAt,
    DateTimeOffset? NextRunAt,
    int BatchesExecuted,
    int TotalRequested,
    int TotalSucceeded,
    int TotalFailed,
    IReadOnlyCollection<LoadSimulationError> LastErrors);
