using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Rox.FinancialControl.Worker.ValidationAutomation;

public sealed class BusinessValidationSchedulerService(
    IOptionsMonitor<ValidationSchedulerOptions> optionsMonitor,
    ILogger<BusinessValidationSchedulerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstCycle = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = Normalize(optionsMonitor.CurrentValue);

            if (!options.Enabled)
            {
                logger.LogInformation("Automated business validation scheduler is disabled.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                continue;
            }

            if (firstCycle && options.RunOnStartup)
            {
                logger.LogInformation(
                    "Automated business validation will run after startup delay of {DelaySeconds} seconds.",
                    options.StartupDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(options.StartupDelaySeconds), stoppingToken);
                await RunValidationAsync(options, stoppingToken);
                firstCycle = false;
                continue;
            }

            firstCycle = false;
            var nextRunAt = CalculateNextRun(DateTimeOffset.UtcNow, options);
            var delay = nextRunAt - DateTimeOffset.UtcNow;

            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            logger.LogInformation(
                "Next automated business validation scheduled for {NextRunAt:u}. Mode: {ScheduleMode}.",
                nextRunAt,
                options.ScheduleMode);

            await Task.Delay(delay, stoppingToken);
            await RunValidationAsync(options, stoppingToken);
        }
    }

    private async Task RunValidationAsync(
        ValidationSchedulerOptions options,
        CancellationToken cancellationToken)
    {
        var payload = new StartBusinessValidationRequest(
            options.EntriesCount,
            options.CreditPercentage,
            options.CreditAmount,
            options.DebitAmount,
            ParseBusinessDate(options.BusinessDate),
            options.TimeoutSeconds,
            options.IncludeInvalidCases);

        try
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(options.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(180)
            };

            logger.LogInformation(
                "Running automated business validation with {EntriesCount} entries against {ApiBaseUrl}.",
                payload.EntriesCount,
                options.ApiBaseUrl);

            using var response = await client.PostAsJsonAsync(
                "/api/operations/business-validation/run",
                payload,
                JsonOptions,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Automated business validation request failed with status {StatusCode}. Body: {Body}",
                    (int)response.StatusCode,
                    body);
                return;
            }

            var result = JsonSerializer.Deserialize<BusinessValidationRunResult>(body, JsonOptions);
            if (result is null)
            {
                logger.LogWarning("Automated business validation returned an empty response.");
                return;
            }

            if (result.Passed)
            {
                logger.LogInformation(
                    "Automated business validation {RunId} passed. Entries: {CreatedEntries}. Expected balance: {ExpectedBalance}. Observed balance: {ObservedBalance}.",
                    result.RunId,
                    result.Totals.CreatedEntries,
                    result.Totals.ExpectedBalance,
                    result.Totals.ObservedBalance);
                return;
            }

            logger.LogError(
                "Automated business validation {RunId} failed. Passed steps: {PassedSteps}/{TotalSteps}.",
                result.RunId,
                result.Steps.Count(step => step.Passed),
                result.Steps.Count);

            foreach (var step in result.Steps.Where(step => !step.Passed))
            {
                logger.LogError(
                    "Failed validation step {StepName}. Expected: {Expected}. Actual: {Actual}. Details: {Details}",
                    step.Name,
                    step.Expected,
                    step.Actual,
                    step.Details);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Automated business validation execution failed.");
        }
    }

    private static ValidationSchedulerOptions Normalize(ValidationSchedulerOptions options)
    {
        return new ValidationSchedulerOptions
        {
            Enabled = options.Enabled,
            ApiBaseUrl = string.IsNullOrWhiteSpace(options.ApiBaseUrl)
                ? "http://localhost:5080"
                : options.ApiBaseUrl.Trim().TrimEnd('/'),
            ScheduleMode = string.Equals(options.ScheduleMode, "Daily", StringComparison.OrdinalIgnoreCase)
                ? "Daily"
                : "Interval",
            RunOnStartup = options.RunOnStartup,
            StartupDelaySeconds = Math.Clamp(options.StartupDelaySeconds, 5, 300),
            IntervalMinutes = Math.Clamp(options.IntervalMinutes, 1, 1440),
            DailyAt = string.IsNullOrWhiteSpace(options.DailyAt) ? "02:00" : options.DailyAt.Trim(),
            TimeZoneId = string.IsNullOrWhiteSpace(options.TimeZoneId) ? "UTC" : options.TimeZoneId.Trim(),
            EntriesCount = Math.Clamp(options.EntriesCount, 1, 100),
            CreditPercentage = Math.Clamp(options.CreditPercentage, 0, 100),
            CreditAmount = options.CreditAmount <= 0 ? 100m : decimal.Round(options.CreditAmount, 2),
            DebitAmount = options.DebitAmount <= 0 ? 40m : decimal.Round(options.DebitAmount, 2),
            BusinessDate = string.IsNullOrWhiteSpace(options.BusinessDate) ? null : options.BusinessDate.Trim(),
            TimeoutSeconds = Math.Clamp(options.TimeoutSeconds, 5, 120),
            IncludeInvalidCases = options.IncludeInvalidCases
        };
    }

    private static DateTimeOffset CalculateNextRun(DateTimeOffset now, ValidationSchedulerOptions options)
    {
        if (!string.Equals(options.ScheduleMode, "Daily", StringComparison.OrdinalIgnoreCase))
        {
            return now.AddMinutes(options.IntervalMinutes);
        }

        var timeZone = ResolveTimeZone(options.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);

        if (!TimeOnly.TryParse(options.DailyAt, out var dailyAt))
        {
            dailyAt = new TimeOnly(2, 0);
        }

        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        var nextLocal = localDate.ToDateTime(dailyAt);

        if (nextLocal <= localNow.DateTime)
        {
            nextLocal = localDate.AddDays(1).ToDateTime(dailyAt);
        }

        return TimeZoneInfo.ConvertTimeToUtc(nextLocal, timeZone);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static DateOnly? ParseBusinessDate(string? businessDate)
    {
        return DateOnly.TryParse(businessDate, out var parsed)
            ? parsed
            : null;
    }

    private sealed record StartBusinessValidationRequest(
        int EntriesCount,
        int CreditPercentage,
        decimal CreditAmount,
        decimal DebitAmount,
        DateOnly? BusinessDate,
        int TimeoutSeconds,
        bool IncludeInvalidCases);

    private sealed record BusinessValidationRunResult(
        Guid RunId,
        bool Passed,
        BusinessValidationTotals Totals,
        IReadOnlyCollection<BusinessValidationStep> Steps);

    private sealed record BusinessValidationTotals(
        int CreatedEntries,
        decimal ExpectedBalance,
        decimal ObservedBalance);

    private sealed record BusinessValidationStep(
        string Name,
        bool Passed,
        string Expected,
        string Actual,
        string? Details);
}
