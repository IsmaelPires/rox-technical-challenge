using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.Balances;

public sealed record DailyBalanceQuery(DateOnly? From, DateOnly? To, CashEntryOrigin Origin);
