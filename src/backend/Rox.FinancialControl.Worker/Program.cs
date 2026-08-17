using Rox.FinancialControl.Infrastructure;
using Rox.FinancialControl.Infrastructure.Persistence;
using Rox.FinancialControl.Worker.ValidationAutomation;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWorkerInfrastructure(builder.Configuration);
builder.Services.Configure<ValidationSchedulerOptions>(
    builder.Configuration.GetSection(ValidationSchedulerOptions.SectionName));
builder.Services.AddHostedService<BusinessValidationSchedulerService>();

var host = builder.Build();
await host.Services.EnsureDatabaseCreatedAsync(
    host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer"));

host.Run();
