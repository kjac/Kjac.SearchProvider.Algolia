using Algolia.Search.Clients;

namespace Kjac.SearchProvider.Algolia.Services;

public interface IAlgoliaClientFactory
{
    SearchClient GetClient();
}
