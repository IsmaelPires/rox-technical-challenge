namespace Rox.FinancialControl.Worker.ValidationAutomation;

public sealed class ValidationSchedulerOptions
{
    public const string SectionName = "ValidationScheduler";

    public bool Enabled { get; init; }

    public string ApiBaseUrl { get; init; } = "http://localhost:5080";

    public string ScheduleMode { get; init; } = "Interval";

    public bool RunOnStartup { get; init; }

    public int StartupDelaySeconds { get; init; } = 20;

    public int IntervalMinutes { get; init; } = 5;

    public string DailyAt { get; init; } = "02:00";

    public string TimeZoneId { get; init; } = "America/Sao_Paulo";

    public int EntriesCount { get; init; } = 8;

    public int CreditPercentage { get; init; } = 50;

    public decimal CreditAmount { get; init; } = 100m;

    public decimal DebitAmount { get; init; } = 40m;

    public string? BusinessDate { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public bool IncludeInvalidCases { get; init; } = true;
}
