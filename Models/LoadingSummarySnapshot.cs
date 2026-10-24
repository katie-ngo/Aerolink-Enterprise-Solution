namespace Aerolink_Enterprise_Solution.Models;

public class LoadingSummarySnapshot
{
    public Flight Flight { get; set; } = new();
    public List<Bag> Bags { get; set; } = new();
    public List<BaggageException> Exceptions { get; set; } = new();
}