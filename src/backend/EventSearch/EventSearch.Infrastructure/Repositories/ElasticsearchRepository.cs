using EventSearch.Core.Entities;
using EventSearch.Core.Interfaces;
using EventSearch.Core.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System.Diagnostics;

namespace EventSearch.Infrastructure.Repositories;

public class ElasticsearchRepository : IElasticsearchRepository
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchRepository> _logger;
    private const string IndexName = "events";

    public ElasticsearchRepository(IElasticClient client, ILogger<ElasticsearchRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<SearchResult<SearchableEvent>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var searchRequest = BuildSearchRequest(query);
            var response = await _client.SearchAsync<SearchableEvent>(searchRequest, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.DebugInformation);
                return new SearchResult<SearchableEvent>();
            }

            var result = new SearchResult<SearchableEvent>
            {
                Items = response.Documents.ToList(),
                TotalCount = response.Total,
                Page = query.Page,
                PageSize = query.PageSize,
                SearchTimeMs = stopwatch.ElapsedMilliseconds,
                Facets = ExtractFacets(response)
            };

            _logger.LogInformation("Search completed in {ElapsedMs}ms, found {TotalResults} results", 
                stopwatch.ElapsedMilliseconds, response.Total);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Elasticsearch search");
            return new SearchResult<SearchableEvent> { SearchTimeMs = stopwatch.ElapsedMilliseconds };
        }
    }

    public async Task<List<AutocompleteResult>> SuggestAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.SearchAsync<SearchableEvent>(s => s
                .Index(IndexName)
                .Size(0)
                .Suggest(su => su
                    .Completion("event_suggest", c => c
                        .Field(f => f.Title.Suffix("suggest"))
                        .Prefix(query)
                        .Size(maxResults)
                    )
                    .Completion("category_suggest", c => c
                        .Field(f => f.Category.Suffix("suggest"))
                        .Prefix(query)
                        .Size(maxResults)
                    )
                    .Completion("location_suggest", c => c
                        .Field(f => f.City.Suffix("suggest"))
                        .Prefix(query)
                        .Size(maxResults)
                    )
                ), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Elasticsearch suggest failed: {Error}", response.DebugInformation);
                return new List<AutocompleteResult>();
            }

            var suggestions = new List<AutocompleteResult>();

            // Extract event suggestions
            if (response.Suggest.ContainsKey("event_suggest"))
            {
                var eventSuggestions = response.Suggest["event_suggest"];
                foreach (var suggestion in eventSuggestions.FirstOrDefault()?.Options ?? Enumerable.Empty<ISuggestOption<SearchableEvent>>())
                {
                    suggestions.Add(new AutocompleteResult 
                    { 
                        Text = suggestion.Text, 
                        Type = "event", 
                        Score = (int)suggestion.Score 
                    });
                }
            }

            // Extract category suggestions
            if (response.Suggest.ContainsKey("category_suggest"))
            {
                var categorySuggestions = response.Suggest["category_suggest"];
                foreach (var suggestion in categorySuggestions.FirstOrDefault()?.Options ?? Enumerable.Empty<ISuggestOption<SearchableEvent>>())
                {
                    suggestions.Add(new AutocompleteResult 
                    { 
                        Text = suggestion.Text, 
                        Type = "category", 
                        Score = (int)suggestion.Score 
                    });
                }
            }

            // Extract location suggestions
            if (response.Suggest.ContainsKey("location_suggest"))
            {
                var locationSuggestions = response.Suggest["location_suggest"];
                foreach (var suggestion in locationSuggestions.FirstOrDefault()?.Options ?? Enumerable.Empty<ISuggestOption<SearchableEvent>>())
                {
                    suggestions.Add(new AutocompleteResult 
                    { 
                        Text = suggestion.Text, 
                        Type = "location", 
                        Score = (int)suggestion.Score 
                    });
                }
            }

            return suggestions.OrderByDescending(s => s.Score).Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Elasticsearch suggest");
            return new List<AutocompleteResult>();
        }
    }

    public async Task<SearchResult<SearchableEvent>> MoreLikeThisAsync(Guid eventId, int maxResults, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await _client.SearchAsync<SearchableEvent>(s => s
                .Index(IndexName)
                .Size(maxResults)
                .Query(q => q
                    .MoreLikeThis(mlt => mlt
                        .Fields(f => f
                            .Field(e => e.Title)
                            .Field(e => e.Description)
                            .Field(e => e.Category)
                            .Field(e => e.Tags)
                        )
                        .Like(l => l.Document(d => d.Index(IndexName).Id(eventId)))
                        .MinTermFrequency(1)
                        .MinDocumentFrequency(1)
                        .MaxQueryTerms(50)
                    )
                    && +q.Term(t => t.Field(f => f.IsActive).Value(true))
                    && !q.Term(t => t.Field(f => f.Id).Value(eventId))
                )
                .Sort(so => so
                    .Descending(SortSpecialField.Score)
                    .Field(f => f.Popularity, SortOrder.Descending)
                )
            , cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Elasticsearch more-like-this failed: {Error}", response.DebugInformation);
                return new SearchResult<SearchableEvent>();
            }

            return new SearchResult<SearchableEvent>
            {
                Items = response.Documents.ToList(),
                TotalCount = response.Total,
                Page = 1,
                PageSize = maxResults,
                SearchTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Elasticsearch more-like-this search");
            return new SearchResult<SearchableEvent> { SearchTimeMs = stopwatch.ElapsedMilliseconds };
        }
    }

    public async Task<bool> IndexDocumentAsync(SearchableEvent document, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.IndexDocumentAsync(document, cancellationToken);
            
            if (!response.IsValid)
            {
                _logger.LogError("Failed to index document {DocumentId}: {Error}", document.Id, response.DebugInformation);
                return false;
            }

            _logger.LogDebug("Successfully indexed document {DocumentId}", document.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {DocumentId}", document.Id);
            return false;
        }
    }

    public async Task<bool> IndexDocumentsAsync(IEnumerable<SearchableEvent> documents, CancellationToken cancellationToken = default)
    {
        var documentList = documents.ToList();
        if (!documentList.Any())
        {
            return true;
        }

        try
        {
            var response = await _client.BulkAsync(b => b
                .Index(IndexName)
                .IndexMany(documentList)
                , cancellationToken);

            if (!response.IsValid || response.Errors)
            {
                _logger.LogError("Bulk indexing failed: {Error}", response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully bulk indexed {DocumentCount} documents", documentList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk indexing of {DocumentCount} documents", documentList.Count);
            return false;
        }
    }

    public async Task<bool> UpdateDocumentAsync(SearchableEvent document, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.UpdateAsync<SearchableEvent>(document.Id, u => u
                .Index(IndexName)
                .Doc(document)
                .DocAsUpsert()
                , cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to update document {DocumentId}: {Error}", document.Id, response.DebugInformation);
                return false;
            }

            _logger.LogDebug("Successfully updated document {DocumentId}", document.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", document.Id);
            return false;
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync<SearchableEvent>(documentId, d => d
                .Index(IndexName)
                , cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to delete document {DocumentId}: {Error}", documentId, response.DebugInformation);
                return false;
            }

            _logger.LogDebug("Successfully deleted document {DocumentId}", documentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DocumentExistsAsync<SearchableEvent>(documentId, d => d
                .Index(IndexName)
            );

            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document exists {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> CreateIndexIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existsResponse = await _client.Indices.ExistsAsync(IndexName);
            
            if (existsResponse.Exists)
            {
                _logger.LogDebug("Index {IndexName} already exists", IndexName);
                return true;
            }

            var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("event_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "stop", "snowball")
                            )
                        )
                    )
                )
                .Map<SearchableEvent>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Text(t => t
                            .Name(n => n.Title)
                            .Analyzer("event_analyzer")
                            .Fields(f => f
                                .Completion(c => c.Name("suggest"))
                                .Keyword(k => k.Name("keyword"))
                            )
                        )
                        .Text(t => t
                            .Name(n => n.Description)
                            .Analyzer("event_analyzer")
                        )
                        .Keyword(k => k
                            .Name(n => n.Category)
                            .Fields(f => f.Completion(c => c.Name("suggest")))
                        )
                        .Keyword(k => k
                            .Name(n => n.City)
                            .Fields(f => f.Completion(c => c.Name("suggest")))
                        )
                        .Keyword(k => k.Name(n => n.Country))
                        .Keyword(k => k.Name(n => n.Venue))
                        .Keyword(k => k.Name(n => n.Organizer))
                        .Keyword(k => k.Name(n => n.Tags))
                        .Number(n => n.Name(f => f.Price).Type(NumberType.Double))
                        .Number(n => n.Name(f => f.Popularity).Type(NumberType.Double))
                        .Number(n => n.Name(f => f.ViewCount).Type(NumberType.Integer))
                        .Number(n => n.Name(f => f.BookingCount).Type(NumberType.Integer))
                        .Number(n => n.Name(f => f.AverageRating).Type(NumberType.Double))
                        .Number(n => n.Name(f => f.RatingCount).Type(NumberType.Integer))
                        .Number(n => n.Name(f => f.AvailableTickets).Type(NumberType.Integer))
                        .Date(d => d.Name(f => f.StartDate))
                        .Date(d => d.Name(f => f.EndDate))
                        .Date(d => d.Name(f => f.CreatedAt))
                        .Date(d => d.Name(f => f.UpdatedAt))
                        .Boolean(b => b.Name(f => f.IsActive))
                    )
                )
                , cancellationToken);

            if (!createResponse.IsValid)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}", IndexName, createResponse.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully created index {IndexName}", IndexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", IndexName);
            return false;
        }
    }

    private SearchRequest<SearchableEvent> BuildSearchRequest(SearchQuery query)
    {
        var searchRequest = new SearchRequest<SearchableEvent>(IndexName)
        {
            Size = query.PageSize,
            From = (query.Page - 1) * query.PageSize,
            TrackTotalHits = true
        };

        // Build the main query
        var queryDescriptor = new BoolQueryDescriptor<SearchableEvent>();

        // Must clauses
        var mustClauses = new List<QueryContainer>();

        // Always filter by active events
        mustClauses.Add(new TermQuery { Field = Infer.Field<SearchableEvent>(f => f.IsActive), Value = true });

        // Text search
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            mustClauses.Add(new MultiMatchQuery
            {
                Query = query.SearchText,
                Fields = new[]
                {
                    Infer.Field<SearchableEvent>(f => f.Title, 3.0),
                    Infer.Field<SearchableEvent>(f => f.Description, 1.0),
                    Infer.Field<SearchableEvent>(f => f.Category, 2.0),
                    Infer.Field<SearchableEvent>(f => f.Tags, 1.5),
                    Infer.Field<SearchableEvent>(f => f.Organizer, 1.0)
                },
                Type = TextQueryType.BestFields,
                Fuzziness = Fuzziness.Auto
            });
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            mustClauses.Add(new TermQuery { Field = Infer.Field<SearchableEvent>(f => f.Category), Value = query.Category });
        }

        // Location filters
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            mustClauses.Add(new TermQuery { Field = Infer.Field<SearchableEvent>(f => f.City), Value = query.City });
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            mustClauses.Add(new TermQuery { Field = Infer.Field<SearchableEvent>(f => f.Country), Value = query.Country });
        }

        // Price range
        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            var priceRange = new NumericRangeQuery
            {
                Field = Infer.Field<SearchableEvent>(f => f.Price)
            };

            if (query.MinPrice.HasValue)
                priceRange.GreaterThanOrEqualTo = (double)query.MinPrice.Value;
            
            if (query.MaxPrice.HasValue)
                priceRange.LessThanOrEqualTo = (double)query.MaxPrice.Value;

            mustClauses.Add(priceRange);
        }

        // Date range
        if (query.StartDate.HasValue || query.EndDate.HasValue)
        {
            var dateRange = new DateRangeQuery
            {
                Field = Infer.Field<SearchableEvent>(f => f.StartDate)
            };

            if (query.StartDate.HasValue)
                dateRange.GreaterThanOrEqualTo = query.StartDate.Value;
            
            if (query.EndDate.HasValue)
                dateRange.LessThanOrEqualTo = query.EndDate.Value;

            mustClauses.Add(dateRange);
        }

        // Tags filter
        if (query.Tags.Any())
        {
            mustClauses.Add(new TermsQuery
            {
                Field = Infer.Field<SearchableEvent>(f => f.Tags),
                Terms = query.Tags
            });
        }

        var boolQuery = queryDescriptor.Must(mustClauses.ToArray());
        searchRequest.Query = new BoolQuery { Must = mustClauses };

        // Add sorting
        var sorts = new List<ISort>();
        
        switch (query.SortBy)
        {
            case SearchSortOption.Relevance:
                sorts.Add(new FieldSort { Field = "_score", Order = SortOrder.Descending });
                sorts.Add(new FieldSort { Field = Infer.Field<SearchableEvent>(f => f.Popularity), Order = SortOrder.Descending });
                break;
            case SearchSortOption.Date:
                sorts.Add(new FieldSort { Field = Infer.Field<SearchableEvent>(f => f.StartDate), Order = query.SortDescending ? SortOrder.Descending : SortOrder.Ascending });
                break;
            case SearchSortOption.Price:
                sorts.Add(new FieldSort { Field = Infer.Field<SearchableEvent>(f => f.Price), Order = query.SortDescending ? SortOrder.Descending : SortOrder.Ascending });
                break;
            case SearchSortOption.Popularity:
                sorts.Add(new FieldSort { Field = Infer.Field<SearchableEvent>(f => f.Popularity), Order = query.SortDescending ? SortOrder.Descending : SortOrder.Ascending });
                break;
            case SearchSortOption.Rating:
                sorts.Add(new FieldSort { Field = Infer.Field<SearchableEvent>(f => f.AverageRating), Order = query.SortDescending ? SortOrder.Descending : SortOrder.Ascending });
                break;
            case SearchSortOption.CreatedAt:
                sorts.Add(new FieldSort { Field = Infer.Field<SearchableEvent>(f => f.CreatedAt), Order = query.SortDescending ? SortOrder.Descending : SortOrder.Ascending });
                break;
        }

        searchRequest.Sort = sorts;

        // Add aggregations for facets
        searchRequest.Aggregations = new AggregationDictionary
        {
            { "categories", new TermsAggregation("categories")
                {
                    Field = Infer.Field<SearchableEvent>(f => f.Category),
                    Size = 20
                }
            },
            { "cities", new TermsAggregation("cities")
                {
                    Field = Infer.Field<SearchableEvent>(f => f.City),
                    Size = 50
                }
            },
            { "countries", new TermsAggregation("countries")
                {
                    Field = Infer.Field<SearchableEvent>(f => f.Country),
                    Size = 20
                }
            },
            { "tags", new TermsAggregation("tags")
                {
                    Field = Infer.Field<SearchableEvent>(f => f.Tags),
                    Size = 30
                }
            },
            { "price_ranges", new RangeAggregation("price_ranges")
                {
                    Field = Infer.Field<SearchableEvent>(f => f.Price),
                    Ranges = new List<AggregationRange>
                    {
                        new() { Key = "under_25", To = 25 },
                        new() { Key = "25_to_50", From = 25, To = 50 },
                        new() { Key = "50_to_100", From = 50, To = 100 },
                        new() { Key = "100_to_200", From = 100, To = 200 },
                        new() { Key = "over_200", From = 200 }
                    }
                }
            }
        };

        return searchRequest;
    }

    private SearchFacets ExtractFacets(ISearchResponse<SearchableEvent> response)
    {
        var facets = new SearchFacets();

        // Categories
        if (response.Aggregations.TryGetValue("categories", out var categoriesAgg) && categoriesAgg is BucketAggregate categoryBuckets)
        {
            facets.Categories = categoryBuckets.Items
                .OfType<KeyedBucket<object>>()
                .ToDictionary(b => b.Key.ToString()!, b => b.DocCount ?? 0);
        }

        // Cities
        if (response.Aggregations.TryGetValue("cities", out var citiesAgg) && citiesAgg is BucketAggregate cityBuckets)
        {
            facets.Cities = cityBuckets.Items
                .OfType<KeyedBucket<object>>()
                .ToDictionary(b => b.Key.ToString()!, b => b.DocCount ?? 0);
        }

        // Countries
        if (response.Aggregations.TryGetValue("countries", out var countriesAgg) && countriesAgg is BucketAggregate countryBuckets)
        {
            facets.Countries = countryBuckets.Items
                .OfType<KeyedBucket<object>>()
                .ToDictionary(b => b.Key.ToString()!, b => b.DocCount ?? 0);
        }

        // Tags
        if (response.Aggregations.TryGetValue("tags", out var tagsAgg) && tagsAgg is BucketAggregate tagBuckets)
        {
            facets.Tags = tagBuckets.Items
                .OfType<KeyedBucket<object>>()
                .ToDictionary(b => b.Key.ToString()!, b => b.DocCount ?? 0);
        }

        // Price ranges
        if (response.Aggregations.TryGetValue("price_ranges", out var priceRangesAgg) && priceRangesAgg is BucketAggregate priceRangeBuckets)
        {
            var priceBuckets = priceRangeBuckets.Items.OfType<RangeBucket>().ToList();
            
            facets.PriceRanges.Under25 = priceBuckets.FirstOrDefault(b => b.Key == "under_25")?.DocCount ?? 0;
            facets.PriceRanges.Range25To50 = priceBuckets.FirstOrDefault(b => b.Key == "25_to_50")?.DocCount ?? 0;
            facets.PriceRanges.Range50To100 = priceBuckets.FirstOrDefault(b => b.Key == "50_to_100")?.DocCount ?? 0;
            facets.PriceRanges.Range100To200 = priceBuckets.FirstOrDefault(b => b.Key == "100_to_200")?.DocCount ?? 0;
            facets.PriceRanges.Over200 = priceBuckets.FirstOrDefault(b => b.Key == "over_200")?.DocCount ?? 0;
        }

        return facets;
    }
}