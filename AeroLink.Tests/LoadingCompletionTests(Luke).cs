using System.Security.Claims;
using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Aerolink_Enterprise_Solution.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

public class LoadingCompletionTests
{
    [Fact]
    public Task OutstandingBag_PreventsCompletion() =>
        CheckCompletionAsync("Pending", false, false);

    [Fact]
    public Task OpenException_PreventsCompletion() =>
        CheckCompletionAsync("Loaded", true, false);

    [Fact]
    public Task AllBagsLoaded_CompletesAndSavesSummary() =>
        CheckCompletionAsync("Loaded", false, true, 2, 0);

    [Fact]
    public Task ApprovedNotToLoad_CompletesWithCorrectTotals() =>
        CheckCompletionAsync("ApprovedNotToLoad", false, true, 1, 1);

    private async Task CheckCompletionAsync(
        string secondBagOutcome, bool openException, bool shouldComplete,
        int expectedLoaded = 0, int expectedApproved = 0)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(connection).Options;

        await using var db = new AeroLinkDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Flights.Add(new Flight
        {
            FlightId = 101,
            FlightNumber = "TEST101",
            LoadingStatus = "NotStarted"
        });

        db.Bags.AddRange(
            new Bag
            {
                BagId = 1,
                FlightId = 101,
                Tag = "TEST-001",
                Outcome = "Loaded"
            },
            new Bag
            {
                BagId = 2,
                FlightId = 101,
                Tag = "TEST-002",
                Outcome = secondBagOutcome
            });

        if (openException)
        {
            db.BaggageExceptions.Add(new BaggageException
            {
                ExceptionId = 1,
                BagId = 1,
                FlightId = 101,
                Category = "DamagedBag",
                Status = "Open"
            });
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "200100"),
            new Claim(ClaimTypes.Name, "James Cooper"),
            new Claim(ClaimTypes.Role, "BG_Supervisor")
        }, "Test");

        var page = new CompleteLoadingModel(db)
        {
            FlightId = 101,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };

        var result = await page.OnPostAsync();
        db.ChangeTracker.Clear();

        var flight = await db.Flights.SingleAsync();
        var completion = await db.LoadingCompletions.SingleOrDefaultAsync();

        if (!shouldComplete)
        {
            Assert.IsType<PageResult>(result);
            Assert.Equal("NotStarted", flight.LoadingStatus);
            Assert.Null(completion);
            return;
        }

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/BaggageManagement/LoadingSummary", redirect.PageName);
        Assert.Equal("Completed", flight.LoadingStatus);
        Assert.NotNull(completion);

        var report = new LoadingSummaryModel(db);
        Assert.IsType<PageResult>(await report.OnGetAsync(101));
        Assert.Equal(2, report.Summary.Bags.Count);
        Assert.Equal(expectedLoaded, report.LoadedCount);
        Assert.Equal(expectedApproved, report.ApprovedNotToLoadCount);
        Assert.Equal("200100", report.Completion.CompletedByEmployeeId);
        Assert.Equal("James Cooper", report.Completion.CompletedByName);

        Assert.Contains(report.Summary.Bags,
            b => b.Tag == "TEST-001" && b.Outcome == "Loaded");

        Assert.Contains(report.Summary.Bags,
            b => b.Tag == "TEST-002" && b.Outcome == secondBagOutcome);
    }
}