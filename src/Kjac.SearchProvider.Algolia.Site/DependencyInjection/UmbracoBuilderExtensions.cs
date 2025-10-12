﻿using System.Text.Json.Serialization.Metadata;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;

namespace Kjac.SearchProvider.Algolia.Site.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureJsonOptions(this IUmbracoBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(
            options =>
            {
                options.JsonSerializerOptions.TypeInfoResolver =
                    options.JsonSerializerOptions.TypeInfoResolver!.WithAddedModifier(
                        typeInfo =>
                        {
                            if (typeInfo.Type != typeof(FacetValue))
                            {
                                return;
                            }

                            typeInfo.PolymorphismOptions = new()
                            {
                                DerivedTypes =
                                {
                                    new JsonDerivedType(typeof(IntegerRangeFacetValue)),
                                    new JsonDerivedType(typeof(DecimalRangeFacetValue)),
                                    new JsonDerivedType(typeof(DateTimeOffsetRangeFacetValue)),
                                    new JsonDerivedType(typeof(IntegerExactFacetValue)),
                                    new JsonDerivedType(typeof(DecimalExactFacetValue)),
                                    new JsonDerivedType(typeof(DateTimeOffsetExactFacetValue)),
                                    new JsonDerivedType(typeof(KeywordFacetValue)),
                                }
                            };
                        }
                    );
            }
        );

        return builder;
    }
}
