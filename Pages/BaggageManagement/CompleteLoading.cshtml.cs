using System.Security.Claims;
using System.Text.Json;
using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Pages;

[Authorize(Roles = "BG_Supervisor")]
public class CompleteLoadingModel : PageModel
{
    private readonly AeroLinkDbContext _db;

    public CompleteLoadingModel(AeroLinkDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int FlightId { get; set; }

    public Flight? SelectedFlight { get; private set; }
    public List<Bag> Bags { get; private set; } = new();
    public List<BaggageException> Exceptions { get; private set; } = new();

    public int ExpectedCount => Bags.Count;
    public int LoadedCount => Bags.Count(b => b.Outcome == "Loaded");

    public int ApprovedNotToLoadCount =>
        Bags.Count(b => b.Outcome == "ApprovedNotToLoad");

    public List<Bag> OutstandingBags => Bags
        .Where(b => b.Outcome != "Loaded" &&
                    b.Outcome != "ApprovedNotToLoad")
        .ToList();

    public List<BaggageException> OpenExceptions =>
        Exceptions.Where(e => e.Status == "Open").ToList();

    public bool CanComplete =>
        SelectedFlight is not null &&
        SelectedFlight.LoadingStatus != "Completed" &&
        ExpectedCount > 0 &&
        OutstandingBags.Count == 0 &&
        OpenExceptions.Count == 0;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadDetailsAsync())
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        if (!await LoadDetailsAsync())
            return NotFound();

        if (SelectedFlight!.LoadingStatus == "Completed" ||
            await _db.LoadingCompletions.AnyAsync(c => c.FlightId == FlightId))
            ModelState.AddModelError("", "Loading has already been completed.");

        if (ExpectedCount == 0)
            ModelState.AddModelError("", "No manifest bags are recorded.");

        if (OutstandingBags.Count > 0)
            ModelState.AddModelError("",
                $"{OutstandingBags.Count} bags remain outstanding.");

        if (OpenExceptions.Count > 0)
            ModelState.AddModelError("",
                $"{OpenExceptions.Count} exceptions remain open.");

        var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(employeeId))
            ModelState.AddModelError("", "Sign in again before completing loading.");

        if (!ModelState.IsValid)
            return Page();

        SelectedFlight.LoadingStatus = "Completed";

        _db.LoadingCompletions.Add(new LoadingCompletion
        {
            FlightId = FlightId,
            CompletedAtUtc = DateTime.UtcNow,
            CompletedByEmployeeId = employeeId!,
            CompletedByName = User.Identity?.Name ?? employeeId!,
            SummaryJson = JsonSerializer.Serialize(new LoadingSummarySnapshot
            {
                Flight = SelectedFlight,
                Bags = Bags,
                Exceptions = Exceptions
            })
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToPage(
            "/BaggageManagement/LoadingSummary",
            new { FlightId });
    }

    private async Task<bool> LoadDetailsAsync()
    {
        SelectedFlight = await _db.Flights
            .FirstOrDefaultAsync(f => f.FlightId == FlightId);

        if (SelectedFlight is null)
            return false;

        Bags = await _db.Bags
            .Where(b => b.FlightId == FlightId)
            .OrderBy(b => b.Tag)
            .ToListAsync();

        Exceptions = await _db.BaggageExceptions
            .Where(e => e.FlightId == FlightId)
            .OrderBy(e => e.ExceptionId)
            .ToListAsync();

        return true;
    }
}