using System.ComponentModel.DataAnnotations.Schema;

namespace Aerolink_Enterprise_Solution.Models;

public class Bag
{
    public int BagId { get; set; }
    public int FlightId { get; set; }
    public string Tag { get; set; } = "";

    public string HandlingType { get; set; } = "";
    public string HandlingInstruction { get; set; } = "";
    public string Outcome { get; set; } = "Pending";

    public bool SpecialHandlingAcknowledged { get; set; } = false;
}