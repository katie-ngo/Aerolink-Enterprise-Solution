using System;

namespace Aerolink_Enterprise_Solution.Models;

public class ExceptionBagage
{
    public int ExceptionId{get; set;} 
    public int BagId{get; set;} 
    public int FlightId {get; set;}
    public Category? BaggageCategory {get;set;}
    public Status? BaggageStatus {get;set;}
    public string? BagDescription{get;set;}
    public string? DecisionReason{get;set;}
}
public enum Category
{
    MissingBag,
    DamagedBag,
}
public enum Status
{
    Open,
    Closed
}