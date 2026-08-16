using Microsoft.Extensions.DependencyInjection;
using Rox.FinancialControl.Application.Balances;
using Rox.FinancialControl.Application.CashEntries;

namespace Rox.FinancialControl.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateCashEntryHandler>();
        services.AddScoped<GetCashEntryHandler>();
        services.AddScoped<ListCashEntriesHandler>();
        services.AddScoped<GetDailyBalanceHandler>();
        services.AddScoped<ListDailyBalancesHandler>();

        return services;
    }
}
