using System.Text.Json;
using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Pages;

[Authorize(Policy = "BaggageOperationsPolicy")]
public class LoadingSummaryModel : PageModel
{
    private readonly AeroLinkDbContext _db;

    public LoadingSummaryModel(AeroLinkDbContext db)
    {
        _db = db;
    }

    public LoadingCompletion Completion { get; private set; } = null!;
    public LoadingSummarySnapshot Summary { get; private set; } = null!;

    public int LoadedCount =>
        Summary.Bags.Count(b => b.Outcome == "Loaded");

    public int ApprovedNotToLoadCount =>
        Summary.Bags.Count(b => b.Outcome == "ApprovedNotToLoad");

    public async Task<IActionResult> OnGetAsync(int flightId)
    {
        var completion = await _db.LoadingCompletions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.FlightId == flightId);

        if (completion is null)
            return NotFound();

        var summary = JsonSerializer
            .Deserialize<LoadingSummarySnapshot>(completion.SummaryJson);

        if (summary is null)
            return StatusCode(500, "The saved summary could not be read.");

        Completion = completion;
        Summary = summary;

        return Page();
    }
}