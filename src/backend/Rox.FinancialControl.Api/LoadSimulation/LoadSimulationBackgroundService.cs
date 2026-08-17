using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Rox.FinancialControl.Application.CashEntries;

namespace Rox.FinancialControl.Api.LoadSimulation;

public sealed class LoadSimulationBackgroundService(
    LoadSimulationState state,
    IHttpClientFactory httpClientFactory,
    IOptions<LoadSimulationOptions> options,
    ILogger<LoadSimulationBackgroundService> logger) : BackgroundService
{
    public const string HttpClientName = "load-simulation";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var configuration = state.TryReserveDueBatch(now);

            if (configuration is not null)
            {
                await RunBatchAsync(configuration, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task RunBatchAsync(LoadSimulationConfiguration configuration, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var client = httpClientFactory.CreateClient(HttpClientName);
        var errors = new List<LoadSimulationError>();

        logger.LogInformation(
            "Starting load simulation batch with {RequestsPerBatch} requests against {ApiBaseUrl}.",
            configuration.RequestsPerBatch,
            options.Value.ApiBaseUrl);

        var tasks = Enumerable
            .Range(1, configuration.RequestsPerBatch)
            .Select(index => SendCashEntryAsync(client, configuration, index, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var result in results.Where(result => result.Error is not null))
        {
            errors.Add(result.Error!);
        }

        state.CompleteBatch(
            configuration.RequestsPerBatch,
            results.Count(result => result.Succeeded),
            errors,
            DateTimeOffset.UtcNow);

        logger.LogInformation(
            "Load simulation batch finished in {ElapsedMs} ms. Success: {Succeeded}. Failed: {Failed}.",
            (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
            results.Count(result => result.Succeeded),
            errors.Count);
    }

    private static async Task<(bool Succeeded, LoadSimulationError? Error)> SendCashEntryAsync(
        HttpClient client,
        LoadSimulationConfiguration configuration,
        int index,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = CreatePayload(configuration, index);
            using var response = await client.PostAsJsonAsync("/api/cash-entries", payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, new LoadSimulationError(DateTimeOffset.UtcNow, (int)response.StatusCode, body));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, new LoadSimulationError(DateTimeOffset.UtcNow, 0, ex.Message));
        }
    }

    private static CreateCashEntryRequest CreatePayload(LoadSimulationConfiguration configuration, int index)
    {
        var businessDate = configuration.BusinessDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var isCredit = Random.Shared.Next(100) < configuration.CreditPercentage;
        var amount = NextAmount(configuration.MinAmount, configuration.MaxAmount);
        var occurredAt = DateTimeOffset.UtcNow.AddMilliseconds(index);

        return new CreateCashEntryRequest(
            businessDate,
            isCredit ? "Credit" : "Debit",
            amount,
            $"Simulação de carga - {(isCredit ? "crédito" : "débito")} #{index}",
            occurredAt,
            "LoadSimulation");
    }

    private static decimal NextAmount(decimal min, decimal max)
    {
        var range = max - min;
        var value = min + range * (decimal)Random.Shared.NextDouble();
        return decimal.Round(value, 2);
    }
}
