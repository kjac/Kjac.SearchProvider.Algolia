using System.Text.Json.Serialization;
using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Kjac.SearchProvider.Algolia.Constants;
using Kjac.SearchProvider.Algolia.Extensions;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Extensions;

namespace Kjac.SearchProvider.Algolia.Services;

internal sealed class AlgoliaSearcher : AlgoliaServiceBase, IAlgoliaSearcher
{
    private readonly IAlgoliaClientFactory _clientFactory;
    private readonly ILogger<AlgoliaSearcher> _logger;

    public AlgoliaSearcher(IAlgoliaClientFactory clientFactory, ILogger<AlgoliaSearcher> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(
        string indexAlias,
        string? query,
        IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets,
        IEnumerable<Sorter>? sorters,
        string? culture,
        string? segment,
        AccessContext? accessContext,
        int skip,
        int take)
    {
        PaginationHelper.ConvertSkipTakeToPaging(skip, take, out var pageNumber, out var pageSize);

        // filters needs splitting into two parts; regular filters (not used for faceting) and facet filters
        Filter[] filtersAsArray = filters as Filter[] ?? filters?.ToArray() ?? [];
        Facet[] facetsAsArray = facets as Facet[] ?? facets?.ToArray() ?? [];

        Facet[] unsupportedFacets = facetsAsArray.Where(IsUnsupportedFacetType).ToArray();
        if (unsupportedFacets.Length > 0)
        {
            _logger.LogWarning(
                "One or more unsupported facet types were omitted from the query (note that Algolia does not support range facets): {types}",
                string.Join(", ", unsupportedFacets.Select(facet => facet.GetType().Name))
            );
            facetsAsArray = facetsAsArray.Except(unsupportedFacets).ToArray();
        }

        var facetFieldNames = facetsAsArray.Select(facet => facet.FieldName).ToArray();
        Filter[] facetFilters = filtersAsArray.Where(f => facetFieldNames.InvariantContains(f.FieldName)).ToArray();
        Filter[] regularFilters = filtersAsArray.Except(facetFilters).ToArray();

        var validIndexAlias = indexAlias.ValidIndexAlias();

        // sorting relies on virtual index replicas in Algolia, so we'll grab the first sorter (if applicable) and
        // append a by-convention postfix to the index alias to handle sorting
        Sorter? sorter = sorters?.FirstOrDefault();
        if (sorter is not null && sorter is not ScoreSorter)
        {
            validIndexAlias = $"{validIndexAlias}_{SortingIndexReplicaPostfix(sorter)}";
        }

        var searchQueries = new List<SearchQuery>();

        void AddSearchQuery(Facet[] effectiveFacets, Filter[] effectiveFacetFilters, bool includeHits)
        {
            var searchParams = new SearchForHits(validIndexAlias)
            {
                // add full text search query (if any)
                Query = query
            };

            // regular (non-facet) filters
            var regularFilterValues = string.Join(" AND ", regularFilters.Select(FilterValue));

            // variance filters
            var indexCultures = culture is null
                ? new[] { IndexConstants.Variation.InvariantCulture }
                : new[] { culture.IndexCulture(), IndexConstants.Variation.InvariantCulture };
            var varianceFilterValues =
                $"{FilterValue(indexCultures.Select(indexCulture => $"{IndexConstants.FieldNames.Culture}:{indexCulture}").ToArray())} AND {IndexConstants.FieldNames.Segment}:{segment.IndexSegment()}";

            // search filters are the combination of variance filters (always there) and regular filters (if any)
            searchParams.Filters = regularFilterValues.IsNullOrWhiteSpace()
                ? varianceFilterValues
                : $"{varianceFilterValues} AND {regularFilterValues}";

            // search facets are just the names of the fields (attributes) to include in the result as facets
            searchParams.Facets = effectiveFacets.Select(FieldName).ToList();

            // facet filters are a somewhat weird construct based on the regular filter syntax
            searchParams.FacetFilters = new FacetFilters(
                effectiveFacetFilters
                    .Select(
                        filter => new FacetFilters(
                            FilterValues(filter)
                                .Select(value => new FacetFilters(value))
                                .ToList()
                        )
                    )
                    .ToList()
            );

            // pagination
            searchParams.HitsPerPage = includeHits ? pageSize : 0;
            searchParams.Page = includeHits ? Convert.ToInt32(pageNumber) : 0;

            // search result data needed in the Algolia search response
            searchParams.ResponseFields = ["hits", "facets", "facets_stats", "nbHits"];

            // fields required per search result hit to return an appropriate response for the consumer (the search abstraction)
            searchParams.AttributesToRetrieve = includeHits ? [IndexConstants.FieldNames.Key, IndexConstants.FieldNames.ObjectType] : [];

            searchQueries.Add(new SearchQuery(searchParams));
        }

        // add the "main" document search, which performs all filtering to return the relevant documents.
        // this returns incorrect facet values for active facets.
        AddSearchQuery(facetsAsArray, facetFilters, true);

        // add "facet" searches for all active facets, in order to retrieve correct facet values for these.
        // to NOT retrieve documents for these searches - documents should only be retrieved by the "main" search.
        foreach (Filter facetFilter in facetFilters)
        {
            Filter[] effectiveFacetFilters = facetFilters.Except([facetFilter]).ToArray();
            Facet effectiveFacet = facetsAsArray
                .First(facet => facet.FieldName.InvariantEquals(facetFilter.FieldName));
            AddSearchQuery([effectiveFacet], effectiveFacetFilters, false);
        }

        SearchClient client = _clientFactory.GetClient();

        SearchResponses<SearchResultDocument>? searchResponses = await client.SearchAsync<SearchResultDocument>(
            new SearchMethodParams { Requests = searchQueries }
        );

        SearchResponse<SearchResultDocument>[]? searchResults = searchResponses?
            .Results
            .Select(result => result.AsSearchResponse())
            .ToArray();

        if (searchResults is null || searchResults.Length == 0)
        {
            _logger.LogError("Unable to obtain a search response from Algolia index: {indexAlias}.", validIndexAlias);
            return new SearchResult(0, [], []);
        }

        // this is the "main" search result which contains the search result hits (if any)
        SearchResponse<SearchResultDocument> hitsResponse = searchResults.First();

        // construct the correct facet values:
        // - the active facets returned by the "facet" searches
        // - all other facets from the "main" search
        // yes, this works... but only because of the backwards iteration through the search results array :)
        var facetCounts = new Dictionary<string, Dictionary<string, int>>();
        for (var i = searchResults.Length - 1; i >= 0; i--)
        {
            SearchResponse<SearchResultDocument> searchResult = searchResults[i];
            if (searchResult.Facets is null)
            {
                continue;
            }

            foreach (KeyValuePair<string, Dictionary<string, int>> facetCount in searchResult
                         .Facets
                         // ignore facets that were picked up already in a previous iteration
                         .Where(facetCount => facetCounts.ContainsKey(facetCount.Key) is false))
            {
                facetCounts[facetCount.Key] = facetCount.Value;
            }
        }

        FacetResult[] facetResults = facetsAsArray.Select(
                facet =>
                {
                    if (facetCounts.TryGetValue(
                            FieldName(facet),
                            out Dictionary<string, int>? facetValue
                        ) is false)
                    {
                        _logger.LogWarning(
                            "Algolia did not return facet values for facet: {facetName}.",
                            facet.FieldName
                        );
                        return null;
                    }

                    FacetResult facetResult = facet switch
                    {
                        KeywordFacet keywordFacet => new FacetResult(
                            keywordFacet.FieldName,
                            facetValue.Select(kvp => new KeywordFacetValue(kvp.Key, kvp.Value))
                        ),
                        IntegerExactFacet integerExactFacet => new FacetResult(
                            integerExactFacet.FieldName,
                            facetValue.Select(kvp => new IntegerExactFacetValue(int.Parse(kvp.Key), kvp.Value))
                        ),
                        DecimalExactFacet decimalExactFacet => new FacetResult(
                            decimalExactFacet.FieldName,
                            facetValue.Select(kvp => new DecimalExactFacetValue(decimal.Parse(kvp.Key), kvp.Value))
                        ),
                        DateTimeOffsetExactFacet dateTimeOffsetExactFacet => new FacetResult(
                            dateTimeOffsetExactFacet.FieldName,
                            facetValue.Select(kvp => new DateTimeOffsetExactFacetValue(DateTimeOffset.FromUnixTimeSeconds(long.Parse(kvp.Key)), kvp.Value))
                        ),
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(facet),
                            $"Encountered an unsupported facet type (Algolia does not support range facets): {facet.GetType().Name}"
                        )
                    };

                    return facetResult;
                }
            )
            .WhereNotNull()
            .ToArray();

        Document[] documents = hitsResponse.Hits.Select(
                hit => new Document(
                    hit.Key,
                    Enum.TryParse(hit.ObjectType, out UmbracoObjectTypes umbracoObjectType)
                        ? umbracoObjectType
                        : UmbracoObjectTypes.Unknown
                )
            )
            .ToArray();

        return new SearchResult(hitsResponse.NbHits ?? 0, documents, facetResults);
    }

    // seems Algolia has no support for range facets
    private static bool IsUnsupportedFacetType(Facet facet)
        => facet is IntegerRangeFacet or DecimalRangeFacet or DateTimeOffsetRangeFacet;

    private static string FilterValue(Filter filter)
    {
        var filterValues = FilterValues(filter);
        return FilterValue(filterValues);
    }

    private static string FilterValue(string[] filterValues)
        => filterValues.Length == 1
            ? filterValues[0]
            : $"({string.Join(" OR ", filterValues)})";

    // TODO: support negated filters
    private static string[] FilterValues(Filter filter)
        => filter switch
        {
            TextFilter textFilter => textFilter.Values
                .Select(value => $"{FieldName(textFilter)}:{value.EscapedFilterValue()}").ToArray(),
            KeywordFilter keywordFilter => keywordFilter.Values
                .Select(value => $"{FieldName(keywordFilter)}:{value.EscapedFilterValue()}").ToArray(),
            IntegerExactFilter integerExactFilter => integerExactFilter.Values
                .Select(value => $"{FieldName(integerExactFilter)}:{value}").ToArray(),
            IntegerRangeFilter integerRangeFilter => integerRangeFilter.Ranges
                .Select(range
                    // NOTE: Algolia range filters include both lower and upper boundaries; Umbraco Search expects the upper
                    // boundary to be omitted, so we'll have to do this by hand (by subtracting from the upper boundary).
                    => $"{FieldName(integerRangeFilter)}:{range.MinValue ?? int.MinValue} TO {(range.MaxValue ?? int.MaxValue) - 1}"
                ).ToArray(),
            DecimalExactFilter decimalExactFilter => decimalExactFilter.Values
                .Select(value => $"{FieldName(decimalExactFilter)}:{value:F2}").ToArray(),
            DecimalRangeFilter decimalRangeFilter => decimalRangeFilter.Ranges
                .Select(range
                    // NOTE: Algolia range filters include both lower and upper boundaries; Umbraco Search expects the upper
                    // boundary to be omitted, so we'll have to do this by hand (by subtracting from the upper boundary).
                    => $"{FieldName(decimalRangeFilter)}:{(range.MinValue ?? decimal.MinValue):F2} TO {((range.MaxValue ?? decimal.MaxValue) - 0.01m):F2}"
                ).ToArray(),
            // NOTE: Algolia expects unix timestamps for dates
            DateTimeOffsetExactFilter dateTimeOffsetExactFilter => dateTimeOffsetExactFilter.Values
                .Select(value => $"{FieldName(dateTimeOffsetExactFilter)}:{value.ToUnixTimeSeconds()}").ToArray(),
            DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter => dateTimeOffsetRangeFilter.Ranges
                .Select(range
                    // NOTE: Algolia range filters include both lower and upper boundaries; Umbraco Search expects the upper
                    // boundary to be omitted, so we'll have to do this by hand (by subtracting from the upper boundary).
                    => $"{FieldName(dateTimeOffsetRangeFilter)}:{(range.MinValue ?? DateTimeOffset.UnixEpoch).ToUnixTimeSeconds()} TO {(range.MaxValue ?? DateTimeOffset.MaxValue).ToUnixTimeSeconds() - 1}"
                ).ToArray(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(filter),
                $"Encountered an unsupported filter type: {filter.GetType().Name}"
            )
        };

    private static string FieldName(Filter filter)
        => filter switch
        {
            DateTimeOffsetExactFilter or DateTimeOffsetRangeFilter => FieldName(
                filter.FieldName,
                IndexConstants.FieldTypePostfix.DateTimeOffsets
            ),
            DecimalExactFilter or DecimalRangeFilter => FieldName(
                filter.FieldName,
                IndexConstants.FieldTypePostfix.Decimals
            ),
            IntegerExactFilter or IntegerRangeFilter => FieldName(
                filter.FieldName,
                IndexConstants.FieldTypePostfix.Integers
            ),
            KeywordFilter => FieldName(filter.FieldName, IndexConstants.FieldTypePostfix.Keywords),
            TextFilter => FieldName(filter.FieldName, IndexConstants.FieldTypePostfix.Texts),
            _ => throw new ArgumentOutOfRangeException(
                nameof(filter),
                $"Encountered an unsupported filter type: {filter.GetType().Name}"
            )
        };

    private static string FieldName(Facet facet)
        => facet switch
        {
            KeywordFacet => FieldName(facet.FieldName, IndexConstants.FieldTypePostfix.Keywords),
            IntegerExactFacet => FieldName(
                facet.FieldName,
                IndexConstants.FieldTypePostfix.Integers
            ),
            DecimalExactFacet => FieldName(
                facet.FieldName,
                IndexConstants.FieldTypePostfix.Decimals
            ),
            DateTimeOffsetExactFacet => FieldName(
                facet.FieldName,
                IndexConstants.FieldTypePostfix.DateTimeOffsets
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(facet),
                $"Encountered an unsupported facet type: {facet.GetType().Name}"
            )
        };

    private static string SortingIndexReplicaPostfix(Sorter sorter)
    {
        var fieldTypePostfix = sorter switch
        {
            DateTimeOffsetSorter => IndexConstants.FieldTypePostfix.DateTimeOffsets,
            DecimalSorter => IndexConstants.FieldTypePostfix.Decimals,
            IntegerSorter => IndexConstants.FieldTypePostfix.Integers,
            KeywordSorter => IndexConstants.FieldTypePostfix.Keywords,
            TextSorter => IndexConstants.FieldTypePostfix.Texts,
            _ => throw new ArgumentOutOfRangeException(
                nameof(sorter),
                $"Encountered an unsupported sorter type: {sorter.GetType().Name}"
            )
        };

        return $"{sorter.FieldName}{fieldTypePostfix}_{(sorter.Direction is Direction.Ascending ? "asc" : "desc")}";
    }

    private record SearchResultDocument
    {
        [JsonPropertyName(IndexConstants.FieldNames.Id)]
        public required string Id { get; init; }

        [JsonPropertyName(IndexConstants.FieldNames.Key)]
        public required Guid Key { get; init; }

        [JsonPropertyName(IndexConstants.FieldNames.ObjectType)]
        public required string ObjectType { get; init; }
    }
}
