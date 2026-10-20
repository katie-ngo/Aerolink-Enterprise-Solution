using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Pages.Special_Baggage
{
    public class ReportBaggageExceptionModel : PageModel
    {
        private readonly AeroLinkDbContext _db;

    public ReportBaggageExceptionModel(AeroLinkDbContext db)
    {
        _db = db;
    }
    public List<SelectListItem> FlightOptions { get; set; } = new();
    public List<SelectListItem> BagOptions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int FlightId { get; set; }
     [BindProperty]
    public int BagId { get; set; }

    [BindProperty]
    public string Category { get; set; } = "";

    [BindProperty]
    public string Description { get; set; } = "";

    
     public async Task OnGetAsync()
    {
        await LoadOptionsAsync(); 
    }
    //submits report and loads flight and bag data
         public async Task<IActionResult> OnPostAsync()
    {
        await LoadOptionsAsync();

        if (Category != "MissingBag" && Category != "DamagedBag")
        {
            ModelState.AddModelError(nameof(Category),
                "Select Missing or Damaged.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError(nameof(Description),
                "Please describe the baggage problem.");
        }
        //check flight exists
        var flightExists = await _db.Flights
            .AnyAsync(f => f.FlightId == FlightId);

        //find bag
        var bag = await _db.Bags
            .FirstOrDefaultAsync(b => b.BagId == BagId);

        if (!flightExists)
        {
            ModelState.AddModelError(nameof(FlightId),
                "Select a valid departure flight.");
        }

        //If bag was not found or bag belongs to different flight

        if (bag == null || bag.FlightId != FlightId)
        {
            ModelState.AddModelError(nameof(BagId),
                "Please select a bag belonging to the selected flight.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        //Create exception record
        var exception = new BaggageException
        {
            BagId = BagId,
            FlightId = FlightId,
            Category = Category,
            Description = Description.Trim(),
            Status = "Open",
            DecisionReason = "",
            ReportedAt = DateTime.UtcNow
        };

        _db.BaggageExceptions.Add(exception);
        await _db.SaveChangesAsync();

        Message = $"Baggage problem reported successfully. " +
                  $"Exception ID: {exception.ExceptionId}";

        //Clear form
        ModelState.Clear();
        FlightId = 0;
        BagId = 0;
        Category = "";
        Description = "";

        await LoadOptionsAsync();

        return Page();
    }

    //Dropdown to select flight and bag
    private async Task LoadOptionsAsync()
    {
        FlightOptions = await _db.Flights
            .AsNoTracking() //readonly
            .OrderBy(f => f.ScheduledDepartureUtc)
            .Select(f => new SelectListItem
            {
                Value = f.FlightId.ToString(),
                Text = f.FlightLabel + " (" + f.FlightNumber + ")"
            })
            .ToListAsync();

        //Show bag options for specific flight
        BagOptions = await (
            from bag in _db.Bags.AsNoTracking()
            join flight in _db.Flights.AsNoTracking()
                on bag.FlightId equals flight.FlightId
            orderby bag.Tag
            select new SelectListItem
            {
                Value = bag.BagId.ToString(),
                Text = bag.Tag + " — " + flight.FlightLabel
            })
            .ToListAsync();
        }
    

    public string? Message { get; set; }

    }
}
