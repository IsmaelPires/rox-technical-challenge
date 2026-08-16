using System.Text.Json;

namespace Rox.FinancialControl.Infrastructure.Messaging;

internal static class MessagingJsonOptions
{
    public static readonly JsonSerializerOptions Instance = new(JsonSerializerDefaults.Web);
}
