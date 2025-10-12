﻿using Kjac.SearchProvider.Algolia.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Services;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Kjac.SearchProvider.Algolia.Site.Controllers;

[ApiController]
public class BooksApiController : ControllerBase
{
    private readonly ISearcherResolver _searcherResolver;
    private readonly IApiContentBuilder _apiContentBuilder;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<BooksApiController> _logger;

    public BooksApiController(
        ISearcherResolver searcherResolver,
        IApiContentBuilder apiContentBuilder,
        ICacheManager cacheManager,
        ILogger<BooksApiController> logger)
    {
        _searcherResolver = searcherResolver;
        _apiContentBuilder = apiContentBuilder;
        _cacheManager = cacheManager;
        _logger = logger;
    }

    [HttpGet("/api/books")]
    public async Task<IActionResult> GetBooks([FromQuery] BooksSearchRequest request)
    {
        // get the default searcher registered for published content
        ISearcher searcher = _searcherResolver.GetRequiredSearcher(SearchConstants.IndexAliases.PublishedContent);

        // get the filters, facets and sorters
        // - filters and sorters are influenced by the active request, facets are fixed
        IEnumerable<Filter> filters = GetFilters(request);
        Facet[] facets = GetFacets();
        IEnumerable<Sorter> sorters = GetSorters(request);

        // execute the search request
        SearchResult result = await searcher.SearchAsync(
            SearchConstants.IndexAliases.PublishedContent,
            request.Query,
            filters,
            facets,
            sorters,
            culture: null,
            segment: null,
            accessContext: null,
            request.Skip,
            request.Take
        );

        // build response models for the search results (the Delivery API output format)
        IApiContent[] documents = result.Documents
            .Select(
                document =>
                {
                    IPublishedContent? publishedContent = _cacheManager.Content.GetById(document.Id);
                    if (publishedContent is not null)
                    {
                        return _apiContentBuilder.Build(publishedContent);
                    }

                    _logger.LogWarning(
                        "Could not find published content for document with id: {documentId}",
                        document.Id
                    );
                    return null;
                }
            )
            .WhereNotNull()
            .ToArray();

        return Ok(
            new BookSearchResult { Total = result.Total, Facets = result.Facets.ToArray(), Documents = documents }
        );
    }

    private static IEnumerable<Filter> GetFilters(BooksSearchRequest request)
    {
        // only include the "book" document type in the results (the document type ID is hardcoded here for simplicity)
        yield return new KeywordFilter(
            SearchConstants.FieldNames.ContentTypeId,
            ["3acd95a1-b9bd-4392-be67-0281dbbe125f"],
            false
        );

        if (request.PublishYearRange?.Length > 0)
        {
            yield return new KeywordFilter("publishYearRange", request.PublishYearRange, false);
        }

        if (request.AuthorNationality?.Length > 0)
        {
            yield return new KeywordFilter("authorNationality", request.AuthorNationality, false);
        }

        if (request.Length?.Length > 0)
        {
            yield return new KeywordFilter("length", request.Length, false);
        }
    }

    private static Facet[] GetFacets()
    {
        var facets = new Facet[]
        {
            new KeywordFacet("length"),
            new KeywordFacet("authorNationality"),
            new KeywordFacet("publishYearRange"),
        };
        return facets;
    }

    private static IEnumerable<Sorter> GetSorters(BooksSearchRequest request)
    {
        Direction direction = request.SortDirection == "asc" ? Direction.Ascending : Direction.Descending;
        Sorter sorter = request.SortBy switch
        {
            "title" => new TextSorter(SearchConstants.FieldNames.Name, direction),
            "publishYear" => new IntegerSorter("publishYear", direction),
            _ => new ScoreSorter(direction)
        };

        return [sorter];
    }
}
