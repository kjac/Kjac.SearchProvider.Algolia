using Algolia.Search.Clients;
using Kjac.SearchProvider.Algolia.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kjac.SearchProvider.Algolia.Services;

internal sealed class AlgoliaClientFactory : IAlgoliaClientFactory
{
    private readonly SearchClient _searchClient;

    public AlgoliaClientFactory(IOptions<ClientOptions> options)
    {
        ClientOptions algoliaClientOptions = options.Value;

        ILoggerFactory loggerFactory = LoggerFactory.Create(
            builder =>
            {
                // Log everything from Algolia in the console, including debug logs
                builder.AddFilter("Algolia", algoliaClientOptions.LogLevel);
            }
        );
        _searchClient = new SearchClient(
            new SearchConfig(algoliaClientOptions.AppId, algoliaClientOptions.ApiKey),
            loggerFactory
        );
    }

    public SearchClient GetClient() => _searchClient;
}
