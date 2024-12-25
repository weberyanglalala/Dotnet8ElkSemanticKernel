namespace SemanticKernel.ELK.Sample.Services.ELKTextSearchService;

public interface IElkTextSearchService
{
    Task SeedHotelCollectionAsync();
    Task<string> GetRelatedHotelChatAsync(string query);
    Task<List<HotelSearchResult>> GetVectorRelatedHotelsResultAsync(string query, int top);
    Task<string> GetVectorStoreTextSearchAsync(string query);
}