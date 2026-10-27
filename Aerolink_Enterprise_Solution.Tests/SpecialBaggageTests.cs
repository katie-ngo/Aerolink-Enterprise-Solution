
using Aerolink_Enterprise_Solution.Data;
using Aerolink_Enterprise_Solution.Models;
using Aerolink_Enterprise_Solution.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aerolink_Enterprise_Solution.Tests;


public class SpecialBaggageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AeroLinkDbContext _db;

    
    public SpecialBaggageTests()
    {
        // in-memory database for this test.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AeroLinkDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    //bag and flight records to in memory sql
    private async Task AddBagAsync(
        int bagId,
        string tag,
        string handlingType,
        bool acknowledged = false)
    {
        var flightId = bagId + 1000;

        _db.Flights.Add(new Flight
        {
            FlightId = flightId,
            FlightNumber = $"TEST{flightId}"
        });

        _db.Bags.Add(new Bag
        {
            BagId = bagId,
            FlightId = flightId,
            Tag = tag,
            HandlingType = handlingType,
            HandlingInstruction = "Handle with care",
            Outcome = "Pending",
            SpecialHandlingAcknowledged = acknowledged
        });
        await _db.SaveChangesAsync();
    }


    [Fact]
    public async Task OnGetAsync_LoadsOnlySpecialBags()
    {
        //Arrange
        await AddBagAsync(1, "BAG001", "Fragile");
        await AddBagAsync(2, "BAG002", "Standard");
        await AddBagAsync(3, "BAG003", "Oversized");

        var page = new BaggageSpecialHandlingModel(_db);

        //Act
        await page.OnGetAsync();

        //Assert
        Assert.Equal(2, page.SpecialBags.Count);
        Assert.All(
            page.SpecialBags,
            bag => Assert.NotEqual("Standard", bag.HandlingType));
    }

    [Fact]
    public async Task OnGetAsync_OrdersByTag()
    {
        //Arrange
        await AddBagAsync(1, "BAG003", "Fragile");
        await AddBagAsync(2, "BAG001", "Oversized");
        await AddBagAsync(3, "BAG002", "Wheelchair");

        var page = new BaggageSpecialHandlingModel(_db);
        //Act
        await page.OnGetAsync();

        //Assert

        Assert.Equal(
            new[] { "BAG001", "BAG002", "BAG003" },
            page.SpecialBags.Select(bag => bag.Tag).ToArray());
    }

    [Fact]
    public async Task OnPostAcknowledgeAsync_WithoutConfirmationDoesNotAcknowledge()
    {
        //Arrange
        await AddBagAsync(1, "BAG001", "Fragile");

        var page = new BaggageSpecialHandlingModel(_db)
        {
            BagId = 1,
            ConfirmAcknowledgement = false
        };

        //Act
        var result = await page.OnPostAcknowledgeAsync();

        //Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(
            "Please confirm that you have read the instructions.",
            page.Message);

        var bag = await _db.Bags.SingleAsync(b => b.BagId == 1);
        Assert.False(bag.SpecialHandlingAcknowledged);
    }

    [Fact]
    public async Task OnPostAcknowledgeAsync_WithConfirmationSavesAcknowledgement()
    {
        //Arrange
        await AddBagAsync(1, "BAG001", "Fragile");

        var page = new BaggageSpecialHandlingModel(_db)
        {
            BagId = 1,
            ConfirmAcknowledgement = true
        };

        //Act
        var result = await page.OnPostAcknowledgeAsync();
        //Assert
        Assert.IsType<PageResult>(result);

        var bag = await _db.Bags.SingleAsync(b => b.BagId == 1);
        Assert.True(bag.SpecialHandlingAcknowledged);

        Assert.Equal(
            "Instructions acknowledged for bag BAG001.",
            page.Message);
    }

    [Fact]
    public async Task OnPostAcknowledgeAsync_ForStandardBagDoesNotAcknowledge()
    {
        //Arrange
        await AddBagAsync(1, "BAG001", "Standard");

        var page = new BaggageSpecialHandlingModel(_db)
        {
            BagId = 1,
            ConfirmAcknowledgement = true
        };
        //Act
        var result = await page.OnPostAcknowledgeAsync();

        //Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Special baggage record not found.", page.Message);

        var bag = await _db.Bags.SingleAsync(b => b.BagId == 1);
        Assert.False(bag.SpecialHandlingAcknowledged);
    }
}
