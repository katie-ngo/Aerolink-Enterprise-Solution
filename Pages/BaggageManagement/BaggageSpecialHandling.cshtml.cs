
using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Pages;

public class BaggageSpecialHandlingModel : PageModel
{
    private readonly AeroLinkDbContext _db;

    public BaggageSpecialHandlingModel(AeroLinkDbContext db)
    {
        _db = db;
    }

    public List<Bag> SpecialBags { get; private set; } = new();

    [BindProperty]
    public int BagId { get; set; }

    [BindProperty]
    public bool ConfirmAcknowledgement { get; set; }

    public string? Message { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadSpecialBagsAsync();
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync()
    {
        if (!ConfirmAcknowledgement)
        {
            Message = "Please confirm that you have read the instructions.";
            await LoadSpecialBagsAsync();
            return Page();
        }

        var baggage = await _db.Bags
            .FirstOrDefaultAsync(b => b.BagId == BagId);

        if (baggage == null ||
            baggage.HandlingType == "Standard")
        {
            Message = "Special baggage record not found.";
            await LoadSpecialBagsAsync();
            return Page();
        }

        baggage.SpecialHandlingAcknowledged = true;

        await _db.SaveChangesAsync();

        Message = $"Instructions acknowledged for bag {baggage.Tag}.";

        await LoadSpecialBagsAsync();
        return Page();
    }

    private async Task LoadSpecialBagsAsync()
    {
        SpecialBags = await _db.Bags
            .AsNoTracking()
            .Where(b => b.HandlingType != "Standard")
            .OrderBy(b => b.Tag)
            .ToListAsync();
    }
}
