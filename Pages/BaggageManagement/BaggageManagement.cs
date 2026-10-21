using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Pages;

public class BaggageManagementModel : PageModel
{
    private readonly AeroLinkDbContext _db;

    public BaggageManagementModel(AeroLinkDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }


    [BindProperty]
    public string EnteredBagTag { get; set; } = string.Empty;

    [TempData]
    public string? FeedbackMessage { get; set; }

    [TempData]
    public string? FeedbackType { get; set; }

    public List<Flight> Flights { get; private set; } = new();
    public Flight? SelectedFlight { get; private set; }
    public List<Bag> Bags { get; private set; } = new();
    public List<BaggageException> Exceptions { get; private set; } = new();

    public int ExpectedCount => Bags.Count;

    public int LoadedCount =>
        Bags.Count(b => b.Outcome == "Loaded");

    public int ApprovedNotToLoadCount =>
        Bags.Count(b => b.Outcome == "ApprovedNotToLoad");

    public int PendingCount =>
        Bags.Count(b => b.Outcome == "Pending");

    public int OpenExceptionCount =>
        Exceptions.Count(e => e.Status == "Open");

    public List<Bag> OutstandingBags =>
        Bags.Where(b =>
            b.Outcome != "Loaded" &&
            b.Outcome != "ApprovedNotToLoad")
        .ToList();

    public int OutstandingCount => OutstandingBags.Count;

    public bool ReadyForCompletion =>
        SelectedFlight is not null &&
        ExpectedCount > 0 &&
        OutstandingCount == 0 &&
        OpenExceptionCount == 0;

    public async Task<IActionResult> OnGetAsync()
    {
        Flights = await _db.Flights
            .AsNoTracking()
            .OrderBy(f => f.ScheduledDepartureUtc)
            .ThenBy(f => f.FlightId)
            .ToListAsync();

        if (!FlightId.HasValue)
        {
            return Page();
        }

        SelectedFlight = Flights
            .FirstOrDefault(f => f.FlightId == FlightId.Value);

        if (SelectedFlight is null)
        {
            return NotFound();
        }

        Bags = await _db.Bags
            .AsNoTracking()
            .Where(b => b.FlightId == FlightId.Value)
            .OrderBy(b => b.Tag)
            .ToListAsync();

        Exceptions = await _db.BaggageExceptions
            .AsNoTracking()
            .Where(e => e.FlightId == FlightId.Value)
            .OrderBy(e => e.ExceptionId)
            .ToListAsync();

        return Page();
    }
    public async Task<IActionResult> OnPostVerifyBagAsync()
    {
        if (!FlightId.HasValue)
        {
            FeedbackMessage = "Please select a flight first.";
            FeedbackType = "danger";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(EnteredBagTag))
        {
            FeedbackMessage = "Please enter a valid Bag Tag.";
            FeedbackType = "danger";
            return RedirectToPage(new { FlightId });
        }

        string cleanTag = EnteredBagTag.Trim().ToUpperInvariant();

        var bag = await _db.Bags
            .FirstOrDefaultAsync(b => b.Tag.ToUpper() == cleanTag);


        if (bag is null)
        {
            FeedbackMessage = $"Rejection: Bag tag '{cleanTag}' is invalid and does not exist in the system.";
            FeedbackType = "danger";
            return RedirectToPage(new { FlightId });
        }


        if (bag.FlightId != FlightId.Value)
        {
            var otherFlight = await _db.Flights.FindAsync(bag.FlightId);
            FeedbackMessage = $"Rejection: Bag '{cleanTag}' belongs to flight {(otherFlight?.FlightNumber ?? bag.FlightId.ToString())}, not this flight!";
            FeedbackType = "danger";
            return RedirectToPage(new { FlightId });
        }

        if (bag.Outcome == "Loaded")
        {
            FeedbackMessage = $"Rejection: Bag '{cleanTag}' has already been confirmed as loaded.";
            FeedbackType = "warning";
            return RedirectToPage(new { FlightId });
        }


        var hasOpenException = await _db.BaggageExceptions
            .AnyAsync(e => e.BagId == bag.BagId && e.Status == "Open");

        if (hasOpenException)
        {
            FeedbackMessage = $"Rejection: Bag '{cleanTag}' has an unresolved exception and cannot be loaded.";
            FeedbackType = "danger";
            return RedirectToPage(new { FlightId });
        }


        bag.Outcome = "Loaded";
        await _db.SaveChangesAsync();

        FeedbackMessage = $"Success: Bag '{cleanTag}' verified and confirmed as Loaded.";
        FeedbackType = "success";

        return RedirectToPage(new { FlightId });
    }

    private async Task LoadFlightDataAsync()
    {
        Flights = await _db.Flights
            .AsNoTracking()
            .OrderBy(f => f.ScheduledDepartureUtc)
            .ThenBy(f => f.FlightId)
            .ToListAsync();
    }

    private async Task LoadBagsAndExceptionsAsync(int flightId)
    {
        Bags = await _db.Bags
            .AsNoTracking()
            .Where(b => b.FlightId == flightId)
            .OrderBy(b => b.Tag)
            .ToListAsync();

        Exceptions = await _db.BaggageExceptions
            .AsNoTracking()
            .Where(e => e.FlightId == flightId)
            .OrderBy(e => e.ExceptionId)
            .ToListAsync();
    }
}