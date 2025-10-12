using Microsoft.Extensions.Logging;

namespace Kjac.SearchProvider.Algolia.Configuration;

public sealed class ClientOptions
{
    public string AppId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
}
