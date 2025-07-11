﻿namespace Tutorial8.Models.DTOs;

public class TripForClientDTO
{
    public int IdTrip { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public int RegisteredAt { get; set; }
    public int? PaymentDate { get; set; }
    public List<string> Countries { get; set; } = new();
}