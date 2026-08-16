namespace Rox.FinancialControl.Application.Balances;

public sealed record DailyBalanceDto(
    DateOnly BusinessDate,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal Balance,
    int EntriesCount,
    DateTimeOffset LastUpdatedAt);
