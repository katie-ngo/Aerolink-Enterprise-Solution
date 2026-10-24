namespace Aerolink_Enterprise_Solution.Models;

public class LoadingCompletion
{
    public int FlightId { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public string CompletedByEmployeeId { get; set; } = "";
    public string CompletedByName { get; set; } = "";
    public string SummaryJson { get; set; } = "";
}