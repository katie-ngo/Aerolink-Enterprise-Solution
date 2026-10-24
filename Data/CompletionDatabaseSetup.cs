using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Data;

public static class CompletionDatabaseSetup
{
    public static void EnsureTable(AeroLinkDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "LoadingCompletions" (
                "FlightId" INTEGER NOT NULL PRIMARY KEY,
                "CompletedAtUtc" TEXT NOT NULL,
                "CompletedByEmployeeId" TEXT NOT NULL,
                "CompletedByName" TEXT NOT NULL,
                "SummaryJson" TEXT NOT NULL,
                FOREIGN KEY ("FlightId")
                    REFERENCES "Flights" ("FlightId")
                    ON DELETE RESTRICT
            );
            """);
    }
}