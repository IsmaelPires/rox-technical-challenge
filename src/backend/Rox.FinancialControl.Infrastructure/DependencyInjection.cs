using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Infrastructure.Messaging;
using Rox.FinancialControl.Infrastructure.Persistence;
using Rox.FinancialControl.Infrastructure.Repositories;

namespace Rox.FinancialControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedInfrastructure(configuration);
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddHostedService<OutboxPublisherBackgroundService>();
        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = false;
            options.StartTimeout = TimeSpan.FromSeconds(10);
            options.StopTimeout = TimeSpan.FromSeconds(10);
        });

        services.AddMassTransit(bus =>
        {
            bus.UsingRabbitMq((context, cfg) => ConfigureRabbitMq(cfg, context, configuration));
        });

        return services;
    }

    public static IServiceCollection AddWorkerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedInfrastructure(configuration);
        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = false;
            options.StartTimeout = TimeSpan.FromSeconds(10);
            options.StopTimeout = TimeSpan.FromSeconds(10);
        });

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<CashEntryRegisteredConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
                    ?? new RabbitMqOptions();

                ConfigureRabbitMq(cfg, context, configuration);

                cfg.ReceiveEndpoint(rabbit.ConsolidationQueueName, endpoint =>
                {
                    endpoint.PrefetchCount = 64;
                    endpoint.ConcurrentMessageLimit = 16;
                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Exponential(
                            retryLimit: 5,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(30),
                            intervalDelta: TimeSpan.FromSeconds(3));
                    });
                    endpoint.ConfigureConsumer<CashEntryRegisteredConsumer>(context);
                });
            });
        });

        return services;
    }

    private static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string DefaultConnection was not configured.");

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IClock, SystemClock>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<ICashEntryRepository, EfCashEntryRepository>();
        services.AddScoped<IDailyBalanceRepository, EfDailyBalanceRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }

    private static void ConfigureRabbitMq(
        IRabbitMqBusFactoryConfigurator cfg,
        IBusRegistrationContext context,
        IConfiguration configuration)
    {
        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        cfg.Host(rabbit.Host, rabbit.VirtualHost, host =>
        {
            host.Username(rabbit.Username);
            host.Password(rabbit.Password);
        });
    }
}
