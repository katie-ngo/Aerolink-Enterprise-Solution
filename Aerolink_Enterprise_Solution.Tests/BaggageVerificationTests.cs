using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Aerolink_Enterprise_Solution.Pages;

namespace Aerolink_Enterprise_Solution.Tests;

public class BaggageVerificationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AeroLinkDbContext _context;

    public BaggageVerificationTests()
    {
        // connect to sql
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AeroLinkDbContext(options);
        _context.Database.EnsureCreated();

        SeedDatabase();
    }
    // seed databse with flights, bags, and exceptions for testing
    private void SeedDatabase()
    {
        var flight1 = new Flight
        {
            FlightId = 1,
            FlightNumber = "QF400",
            DepartureAirport = "BNE",
            ArrivalAirport = "SYD",
            ScheduledDepartureUtc = DateTime.UtcNow.AddHours(2)
        };

        var flight2 = new Flight
        {
            FlightId = 2,
            FlightNumber = "VA820",
            DepartureAirport = "BNE",
            ArrivalAirport = "MEL",
            ScheduledDepartureUtc = DateTime.UtcNow.AddHours(4)
        };

        _context.Flights.AddRange(flight1, flight2);

        _context.Bags.AddRange(
            new Bag { BagId = 1, FlightId = 1, Tag = "BG1001", Outcome = "Pending", HandlingType = "Standard" },
            new Bag { BagId = 2, FlightId = 1, Tag = "BG1002", Outcome = "Loaded", HandlingType = "Standard" },
            new Bag { BagId = 3, FlightId = 1, Tag = "BG1003", Outcome = "Pending", HandlingType = "Standard" },
            new Bag { BagId = 4, FlightId = 2, Tag = "BG2001", Outcome = "Pending", HandlingType = "Standard" }
        );

        _context.BaggageExceptions.Add(
            new BaggageException
            {
                ExceptionId = 1,
                FlightId = 1,
                BagId = 3,
                Category = "Damaged",
                Description = "Torn zipper",
                Status = "Open"
            }
        );

        _context.SaveChanges();
    }

    private BaggageManagementModel CreatePageModel()
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TestTempDataProvider());

        return new BaggageManagementModel(_context)
        {
            TempData = tempData
        };
    }

    [Fact]
    public async Task TC_I2_01_ValidTag_ConfirmsLoading_UpdatesDatabase()
    {
        // test: Enter a valid tag for Flight 1 and verify that it updates the bag's outcome to "Loaded"
        var model = CreatePageModel();
        model.FlightId = 1;
        model.EnteredBagTag = "BG1001";

        
        var result = await model.OnPostVerifyBagAsync();

        
        var updatedBag = await _context.Bags.FirstAsync(b => b.Tag == "BG1001");
        Assert.Equal("Loaded", updatedBag.Outcome);
        Assert.Equal("success", model.FeedbackType);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task TC_I2_02_NonExistentTag_RejectsOperation()
    {
        // test: Enter a tag that does not exist in the database
        var model = CreatePageModel();
        model.FlightId = 1;
        model.EnteredBagTag = "INVALID-999";

        var result = await model.OnPostVerifyBagAsync();

        Assert.Equal("danger", model.FeedbackType);
        Assert.Contains("invalid and does not exist", model.FeedbackMessage);
    }

    [Fact]
    public async Task TC_I2_03_WrongFlightTag_RejectsOperation()
    {
        // test: BG2001 belongs to Flight 2, but we are loading Flight 1
        var model = CreatePageModel();
        model.FlightId = 1;
        model.EnteredBagTag = "BG2001";

  
        var result = await model.OnPostVerifyBagAsync();

        
        var bag = await _context.Bags.FirstAsync(b => b.Tag == "BG2001");
        Assert.Equal("Pending", bag.Outcome);
        Assert.Equal("danger", model.FeedbackType);
        Assert.Contains("belongs to flight", model.FeedbackMessage);
    }

    [Fact]
    public async Task TC_I2_04_DuplicateLoad_RejectsWithoutDoubleCounting()
    {
        // test: Attempt to load a bag that is already loaded
        var model = CreatePageModel();
        model.FlightId = 1;
        model.EnteredBagTag = "BG1002";

        
        var result = await model.OnPostVerifyBagAsync();

        Assert.Equal("warning", model.FeedbackType);
        Assert.Contains("already been loaded", model.FeedbackMessage);
    }

    [Fact]
    public async Task TC_I2_05_BagWithOpenException_RejectsLoading()
    {
        // test: Attempt to load a bag that has an open exception
        var model = CreatePageModel();
        model.FlightId = 1;
        model.EnteredBagTag = "BG1003";

        var result = await model.OnPostVerifyBagAsync();

 
        var bag = await _context.Bags.FirstAsync(b => b.Tag == "BG1003");
        Assert.Equal("Pending", bag.Outcome);
        Assert.Equal("danger", model.FeedbackType);
        Assert.Contains("unresolved exception", model.FeedbackMessage);
    }

    [Fact]
    public async Task TC_I2_06_CaseInsensitiveAndWhitespaceTag_Succeeds()
    {
        // test: Enter a valid tag with different casing and whitespace, and verify that it still updates the bag's outcome to "Loaded"
        var model = CreatePageModel();
        model.FlightId = 1;
        model.EnteredBagTag = "   bg1001   ";


        var result = await model.OnPostVerifyBagAsync();


        var bag = await _context.Bags.FirstAsync(b => b.Tag == "BG1001");
        Assert.Equal("Loaded", bag.Outcome);
        Assert.Equal("success", model.FeedbackType);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
    }
}

// In-memory stub for TempData in unit tests
internal class TestTempDataProvider : ITempDataProvider
{
    private readonly System.Collections.Generic.Dictionary<string, object> _values = new();

    public System.Collections.Generic.IDictionary<string, object> LoadTempData(HttpContext context) => _values;

    public void SaveTempData(HttpContext context, System.Collections.Generic.IDictionary<string, object> values)
    {
        _values.Clear();
        if (values != null)
        {
            foreach (var kvp in values)
            {
                _values[kvp.Key] = kvp.Value;
            }
        }
    }
}