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
}