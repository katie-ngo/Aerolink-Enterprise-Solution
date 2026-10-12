namespace Aerolink_Enterprise_Solution.Models;

public class Flight
{
    public int FlightId { get; set; }
    public string FlightLabel { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public string DepartureAirport { get; set; } = "";
    public string ArrivalAirport { get; set; } = "";
    public DateTime ScheduledDepartureUtc { get; set; }
    public string AircraftId { get; set; } = "";
    public string LoadingStatus { get; set; } = "NotStarted";
}