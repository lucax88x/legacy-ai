using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Legacy.Api.Models;

/// <summary>
/// Vector store record model for Product data stored in Qdrant.
/// Maps to the 'products' collection created by the CDC sync service.
/// </summary>
public class ProductVectorRecord
{
    [VectorStoreKey]
    public ulong Id { get; set; }

    [VectorStoreData(IsFullTextIndexed = true)]
    public string Name { get; set; } = string.Empty;

    [VectorStoreData(IsFullTextIndexed = true)]
    public string Description { get; set; } = string.Empty;

    [VectorStoreData]
    public float Price { get; set; }

    [VectorStoreData]
    public int StockQuantity { get; set; }

    [VectorStoreData(IsFullTextIndexed = true)]
    public string Category { get; set; } = string.Empty;

    [VectorStoreVector(1536)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
