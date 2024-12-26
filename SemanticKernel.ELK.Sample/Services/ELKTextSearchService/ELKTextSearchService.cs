using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SemanticKernel.ELK.Sample.Services.ELKTextSearchService.Entities;

namespace SemanticKernel.ELK.Sample.Services.ELKTextSearchService;

[Experimental("SKEXP0001")]
public class ElkTextSearchService : IElkTextSearchService
{
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;
    private readonly IVectorStoreRecordCollection<string, Hotel> _hotelCollection;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly VectorStoreTextSearch<Hotel> _textSearch;

    public ElkTextSearchService(Kernel kernel,
        IWebHostEnvironment webHostEnvironment)
    {
        _kernel = kernel;
        _webHostEnvironment = webHostEnvironment;
        _embeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _hotelCollection = kernel.GetRequiredService<IVectorStoreRecordCollection<string, Hotel>>();
        _textSearch = kernel.GetRequiredService<VectorStoreTextSearch<Hotel>>();
    }

    public async Task SeedHotelCollectionAsync()
    {
        // Combine the web root path with the relative path to the file
        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "assets", "hotels.csv");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Hotels file not found.", filePath);
        }

        var hotels = (await File.ReadAllLinesAsync(filePath))
            .Select(x => x.Split(';'));

        foreach (var chunk in hotels.Chunk(25))
        {
            var descriptionEmbeddings =
                await _embeddingGenerationService.GenerateEmbeddingsAsync(chunk.Select(x => x[2]).ToArray());

            for (var i = 0; i < chunk.Length; ++i)
            {
                var hotel = chunk[i];
                await _hotelCollection.UpsertAsync(new Hotel
                {
                    HotelId = hotel[0],
                    HotelName = hotel[1],
                    Description = hotel[2],
                    DescriptionEmbedding = descriptionEmbeddings[i],
                    ReferenceLink = hotel[3]
                });
            }
        }
    }

    public async Task<string> GetRelatedHotelChatAsync(string query)
    {
        var response = await _kernel.InvokePromptAsync(
            promptTemplate: """
                            Please use this information to answer the question:
                            {{#with (SearchPlugin-GetTextSearchResults question)}}
                              {{#each this}}
                                Name: {{Name}}
                                Value: {{Value}}
                                Source: {{Link}}
                                -----------------
                              {{/each}}
                            {{/with}}

                            Include the source of relevant information in the response.

                            Question: {{question}}
                            """,
            arguments: new KernelArguments
            {
                { "question", query },
            },
            templateFormat: "handlebars",
            promptTemplateFactory: new HandlebarsPromptTemplateFactory());
        return response.ToString();
    }

    public async Task<List<HotelSearchResult>> GetVectorRelatedHotelsResultAsync(string query, int top = 10)
    {
        var result = new List<HotelSearchResult>();
        var embeddings = await _embeddingGenerationService.GenerateEmbeddingAsync(query);
        var vectorSearchOptions = new VectorSearchOptions()
        {
            Top = top
        };
        var hotels = await _hotelCollection.VectorizedSearchAsync(embeddings, vectorSearchOptions);
        await foreach (var temp in hotels.Results)
        {
            result.Add(new HotelSearchResult
            {
                Id = temp.Record.HotelId,
                Name = temp.Record.HotelName,
                Description = temp.Record.Description,
                Link = temp.Record.ReferenceLink,
                Relevance = temp.Score.ToString()
            });
        }

        return result;
    }

    public async Task<string> GetVectorStoreTextSearchAsync(string query)
    {
        var sb = new StringBuilder();
        var textSearchResult = await _textSearch.SearchAsync(query);
        await foreach (var temp in textSearchResult.Results)
        {
            sb.Append(temp);
        }

        return sb.ToString();
    }
}

public class HotelSearchResult
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }
    public string Relevance { get; set; }
}