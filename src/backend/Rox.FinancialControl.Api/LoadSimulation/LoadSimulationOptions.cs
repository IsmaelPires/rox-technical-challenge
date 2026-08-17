namespace Rox.FinancialControl.Api.LoadSimulation;

public sealed class LoadSimulationOptions
{
    public const string SectionName = "LoadSimulation";

    public string ApiBaseUrl { get; init; } = "http://localhost:5080";
}
