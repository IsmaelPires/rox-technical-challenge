using Rox.FinancialControl.Api.Endpoints;
using Rox.FinancialControl.Api.Middleware;
using Rox.FinancialControl.Application;
using Rox.FinancialControl.Infrastructure;
using Rox.FinancialControl.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddApiInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:8080",
                "http://127.0.0.1:8080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("frontend");

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"))
    .ExcludeFromDescription();

app.MapCashEntryEndpoints();
app.MapDailyBalanceEndpoints();
app.MapOperationalEndpoints();

await app.Services.EnsureDatabaseCreatedAsync(app.Logger);

app.Run();

public partial class Program;
