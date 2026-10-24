using System.ComponentModel.DataAnnotations;
using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Pages;

[Authorize(Roles = "BG_Supervisor")]
public class ResolveBaggageExceptionModel : PageModel
{
    private readonly AeroLinkDbContext _db;

    public ResolveBaggageExceptionModel(AeroLinkDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int ExceptionId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Select a decision.")]
    public string Decision { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "Enter a reason for your decision.")]
    [StringLength(1000,
        ErrorMessage = "The reason must be 1000 characters or fewer.")]
    public string DecisionReason { get; set; } = "";

    public BaggageException? ReportedException { get; private set; }
    public Bag? SelectedBag { get; private set; }
    public Flight? SelectedFlight { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadDetailsAsync())
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        if (!await LoadDetailsAsync())
        {
            return NotFound();
        }

        var exception = ReportedException!;
        var bag = SelectedBag!;
        var flight = SelectedFlight!;

        if (exception.Status != "Open")
        {
            ModelState.AddModelError("",
                "This exception has already been decided.");
        }

        if (flight.LoadingStatus == "Completed")
        {
            ModelState.AddModelError("",
                "Decisions cannot be changed after loading is completed.");
        }

        if (Decision != "Resolve" && Decision != "ApproveNotToLoad")
        {
            ModelState.AddModelError(nameof(Decision),
                "Select a valid decision.");
        }

        if (string.IsNullOrWhiteSpace(DecisionReason))
        {
            ModelState.AddModelError(nameof(DecisionReason),
                "Enter a reason for your decision.");
        }

        if (Decision == "ApproveNotToLoad" && bag.Outcome == "Loaded")
        {
            ModelState.AddModelError("",
                "This bag is already loaded and cannot be approved not to load.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        exception.DecisionReason = DecisionReason.Trim();
        exception.Status = "Resolved";

        if (Decision == "ApproveNotToLoad")
        {
            bag.Outcome = "ApprovedNotToLoad";
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["FeedbackMessage"] = Decision == "ApproveNotToLoad"
            ? $"Bag {bag.Tag} was approved not to load."
            : $"The problem for bag {bag.Tag} was resolved. Its loading outcome is unchanged.";

        TempData["FeedbackType"] = "success";

        return RedirectToPage(
            "/BaggageManagement/BaggageManagement",
            new { FlightId = flight.FlightId });
    }

    private async Task<bool> LoadDetailsAsync()
    {
        ReportedException = await _db.BaggageExceptions
            .FirstOrDefaultAsync(e => e.ExceptionId == ExceptionId);

        if (ReportedException is null)
        {
            return false;
        }

        SelectedBag = await _db.Bags.FirstOrDefaultAsync(b =>
            b.BagId == ReportedException.BagId &&
            b.FlightId == ReportedException.FlightId);

        SelectedFlight = await _db.Flights.FirstOrDefaultAsync(f =>
            f.FlightId == ReportedException.FlightId);

        return SelectedBag is not null && SelectedFlight is not null;
    }
}