namespace Aerolink_Enterprise_Solution.Models;

public class BaggageException
{
    public int ExceptionId { get; set; }
    public int BagId { get; set; }
    public int FlightId { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open";
    public string DecisionReason { get; set; } = "";
}