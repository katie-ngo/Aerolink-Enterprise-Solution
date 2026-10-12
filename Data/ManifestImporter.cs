using Aerolink_Enterprise_Solution.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Data;

public static class ManifestImporter
{
    public static void ImportIfEmpty(
        AeroLinkDbContext db, string filePath)
    {
        // Preserve existing records and operational updates.
        if (db.Flights.Any() ||
            db.Bags.Any() ||
            db.BaggageExceptions.Any())
        {
            return;
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The baggage manifest spreadsheet was not found.",
                filePath);
        }

        using var workbook = new XLWorkbook(filePath);
        using var transaction = db.Database.BeginTransaction();

        foreach (var row in workbook.Worksheet("Flights")
                     .RowsUsed().Skip(1))
        {
            db.Flights.Add(new Flight
            {
                FlightId = row.Cell(1).GetValue<int>(),
                FlightLabel = row.Cell(2).GetString(),
                FlightNumber = row.Cell(3).GetString(),
                DepartureAirport = row.Cell(4).GetString(),
                ArrivalAirport = row.Cell(5).GetString(),
                ScheduledDepartureUtc = DateTime.SpecifyKind(
                    row.Cell(6).GetValue<DateTime>(),
                    DateTimeKind.Utc),
                AircraftId = row.Cell(7).GetString(),
                LoadingStatus = row.Cell(8).GetString()
            });
        }

        db.SaveChanges();

        foreach (var row in workbook.Worksheet("Bags")
                     .RowsUsed().Skip(1))
        {
            db.Bags.Add(new Bag
            {
                BagId = row.Cell(1).GetValue<int>(),
                FlightId = row.Cell(2).GetValue<int>(),
                Tag = row.Cell(3).GetString(),
                HandlingType = row.Cell(4).GetString(),
                HandlingInstruction = row.Cell(5).GetString(),
                Outcome = row.Cell(6).GetString()
            });
        }

        db.SaveChanges();

        foreach (var row in workbook.Worksheet("Exceptions")
                     .RowsUsed().Skip(1))
        {
            db.BaggageExceptions.Add(new BaggageException
            {
                ExceptionId = row.Cell(1).GetValue<int>(),
                BagId = row.Cell(2).GetValue<int>(),
                FlightId = row.Cell(3).GetValue<int>(),
                Category = row.Cell(4).GetString(),
                Description = row.Cell(5).GetString(),
                Status = row.Cell(6).GetString(),
                DecisionReason = row.Cell(7).GetString()
            });
        }

        db.SaveChanges();
        transaction.Commit();
    }
}