# Running the tests

These tests are created with [NUnit](https://nunit.org/), and must be executed against an Algolia application of your choice.

You can configure the test client connectivity in [appsettings.json](https://github.com/kjac/Kjac.SearchProvider.Algolia/blob/main/src/Kjac.SearchProvider.Algolia.Tests/appsettings.json), exactly the same way you'd configure the search provider.

To minimize the indexing operations performed against Algolia, the test indexes are reused across tests to the extent possible. Re-creating a test index is done by invoking the `RebuildIndex` "tests" in the base classes - for example in [AlgoliaSearcherTests](https://github.com/kjac/Kjac.SearchProvider.Algolia/blob/main/src/Kjac.SearchProvider.Algolia.Tests/AlgoliaSearcherTests.cs). These "tests" are ignored by default, so you'll have to remove the `Ignore` attribute to invoke them.
