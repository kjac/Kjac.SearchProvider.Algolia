using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Kjac.SearchProvider.Algolia.DependencyInjection;
using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Sync;

namespace Kjac.SearchProvider.Algolia.Tests;

[TestFixture]
public abstract class AlgoliaTestBase
{
    private ServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void SetUp()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddAlgolia(configuration)
            .AddLogging();

        serviceCollection.AddSingleton<IServerRoleAccessor, SingleServerRoleAccessor>();

        PerformAdditionalConfiguration(serviceCollection);

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }

    protected virtual void PerformAdditionalConfiguration(ServiceCollection serviceCollection)
    {
    }

    protected T GetRequiredService<T>() where T : notnull
        => _serviceProvider.GetRequiredService<T>();

    protected async Task DeleteIndex(string indexAlias)
    {
        SearchClient client = GetRequiredService<IAlgoliaClientFactory>().GetClient();

        var validIndexAlias = indexAlias.ValidIndexAlias();
        var indexExists = await client.IndexExistsAsync(validIndexAlias);
        if (indexExists is false)
        {
            return;
        }

        DeletedAtResponse? response = await client.DeleteIndexAsync(validIndexAlias);
        if (response is null)
        {
            return;
        }

        await client.WaitForTaskAsync(validIndexAlias, response.TaskID);
    }
}
