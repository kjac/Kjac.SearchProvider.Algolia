namespace Kjac.SearchProvider.Algolia.Services;

public interface IAlgoliaIndexManager
{
    Task EnsureAsync(string indexAlias);
}
