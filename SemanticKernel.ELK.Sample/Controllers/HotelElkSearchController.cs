using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using SemanticKernel.ELK.Sample.Services.ELKTextSearchService;

namespace SemanticKernel.ELK.Sample.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[Experimental("SKEXP0001")]
public class HotelElkSearchController : ControllerBase
{
    private readonly IElkTextSearchService _elkTextSearchService;

    public HotelElkSearchController(IElkTextSearchService elkTextSearchService)
    {
        _elkTextSearchService = elkTextSearchService ?? throw new ArgumentNullException(nameof(elkTextSearchService));
    }

    [HttpGet]
    public async Task<IActionResult> CreateHotelCollectionIfNotExistAsync()
    {
        await _elkTextSearchService.SeedHotelCollectionAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetHotelsChatAsync(string query)
    {
        var results = await _elkTextSearchService.GetRelatedHotelChatAsync(query);
        return Ok(results);
    }

    [HttpGet]
    public async Task<IActionResult> SearchHotelsListAsync(string query, int top = 10)
    {
        var results = await _elkTextSearchService.GetVectorRelatedHotelsResultAsync(query, top);
        return Ok(results);
    }

    [HttpGet]
    public async Task<IActionResult> GetVectorStoreTextSearchAsync(string query)
    {
        var results = await _elkTextSearchService.GetVectorStoreTextSearchAsync(query);
        return Ok(results);
    }
}