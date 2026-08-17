namespace Rox.FinancialControl.Api.BusinessValidation;

public sealed record StartBusinessValidationRequest(
    int EntriesCount,
    int CreditPercentage,
    decimal CreditAmount,
    decimal DebitAmount,
    DateOnly? BusinessDate,
    int TimeoutSeconds,
    bool IncludeInvalidCases);

public sealed record BusinessValidationConfiguration(
    int EntriesCount,
    int CreditPercentage,
    decimal CreditAmount,
    decimal DebitAmount,
    DateOnly BusinessDate,
    int TimeoutSeconds,
    bool IncludeInvalidCases);

public sealed record BusinessValidationStep(
    string Name,
    bool Passed,
    string Expected,
    string Actual,
    string? Details = null);

public sealed record BusinessValidationTotals(
    int CreatedEntries,
    decimal ExpectedCredits,
    decimal ExpectedDebits,
    decimal ExpectedBalance,
    decimal ObservedCredits,
    decimal ObservedDebits,
    decimal ObservedBalance,
    int ObservedEntriesCount);

public sealed record BusinessValidationRunResult(
    Guid RunId,
    bool Passed,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    BusinessValidationConfiguration Configuration,
    BusinessValidationTotals Totals,
    IReadOnlyCollection<BusinessValidationStep> Steps);
