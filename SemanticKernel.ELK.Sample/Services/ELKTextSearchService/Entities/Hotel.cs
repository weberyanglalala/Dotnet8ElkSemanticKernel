using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace SemanticKernel.ELK.Sample.Services.ELKTextSearchService.Entities;

[Experimental("SKEXP0001")]
public sealed record Hotel
{
    [VectorStoreRecordKey]
    public required string HotelId { get; set; }

    [TextSearchResultName]
    [VectorStoreRecordData(IsFilterable = true)]
    [Experimental("SKEXP0001")]
    public required string HotelName { get; set; }

    [TextSearchResultValue]
    [VectorStoreRecordData(IsFullTextSearchable = true)]
    [Experimental("SKEXP0001")]
    public required string Description { get; set; }

    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    [TextSearchResultLink]
    [VectorStoreRecordData]
    [Experimental("SKEXP0001")]
    public string? ReferenceLink { get; set; }
}