using System.Diagnostics.CodeAnalysis;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.ELK.Sample.Services.ELKTextSearchService;
using SemanticKernel.ELK.Sample.Services.ELKTextSearchService.Entities;

namespace SemanticKernel.ELK.Sample.Configurations;

public static class SemanticKernelConfiguration
{
    [Experimental("SKEXP0010")]
    public static IServiceCollection AddSemanticKernelService(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddSingleton<Kernel>(sp =>
        {
            var kernelBuilder = Kernel.CreateBuilder();

            var openAiApiKey = configuration["OpenAiApiKey"]
                               ?? throw new InvalidOperationException("OpenAiApiKey is not configured.");
            kernelBuilder.Services.AddOpenAIChatCompletion("gpt-4o-2024-08-06", openAiApiKey);
            kernelBuilder.Services.AddOpenAITextEmbeddingGeneration("text-embedding-ada-002", openAiApiKey);

            // Register text search service.
            kernelBuilder.AddVectorStoreTextSearch<Hotel>();

            var elkApiKey = configuration["ELKApiKey"]
                            ?? throw new InvalidOperationException("ELKApiKey is not configured.");
            var elkUrl = configuration["ELKUrl"]
                         ?? throw new InvalidOperationException("ELKUrl is not configured.");

            // Register Elasticsearch vector store.
            var elasticsearchClientSettings = new ElasticsearchClientSettings(new Uri(elkUrl))
                .Authentication(new ApiKey(elkApiKey));
            kernelBuilder.AddElasticsearchVectorStoreRecordCollection<string, Hotel>("skhotels",
                elasticsearchClientSettings);

            return kernelBuilder.Build();
        });

        return services;
    }

    [Experimental("SKEXP0001")]
    public static IServiceCollection AddElkVectorStoreTextSearch(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddSingleton<IElkTextSearchService, ElkTextSearchService>(sp =>
        {
            var serviceProvider = services.BuildServiceProvider();
            var kernel = serviceProvider.GetRequiredService<Kernel>();
            var embeddings = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            var vectorStoreCollection =
                kernel.GetRequiredService<IVectorStoreRecordCollection<string, Hotel>>();
            var textSearch = kernel.GetRequiredService<VectorStoreTextSearch<Hotel>>();
            kernel.Plugins.Add(textSearch.CreateWithGetTextSearchResults("SearchPlugin"));
            var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            return new ElkTextSearchService(kernel, embeddings, vectorStoreCollection, webHostEnvironment, textSearch);
        });
        return services;
    }
}