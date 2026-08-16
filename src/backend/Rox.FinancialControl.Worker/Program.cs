using Rox.FinancialControl.Infrastructure;
using Rox.FinancialControl.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWorkerInfrastructure(builder.Configuration);

var host = builder.Build();
await host.Services.EnsureDatabaseCreatedAsync(
    host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer"));

host.Run();
