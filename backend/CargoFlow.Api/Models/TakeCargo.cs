namespace CargoFlow.Api.Models
{
    public class TakeCargo
    {
        public int Id { get; set; }
        public string CargoName { get; set; }
        public string? Description { get; set; }
        public string? Destination { get; set; }
        public string? PickupLocation { get; set; }
        public double Km { get; set; }
        public double Gas { get; set; }
        public double? GasReturn { get; set; } // Gas/km returned after delivery
        public bool HasGarbage { get; set; }

        // Pickup photos
        public string? DashboardImage { get; set; }
        public string? ExteriorImages { get; set; }
        public string? GarbageImage { get; set; }
        // Delivery photos (leave cargo)
        public string? DashboardImageLeave { get; set; }
        public string? ExteriorImagesLeave { get; set; }
        public string? GarbageImageLeave { get; set; }
        
        public string? DriverFullName { get; set; }
        public int? DriverId { get; set; }
        public DateTime? TakenAt { get; set; } // Pickup date/time
        public DateTime? LeaveDatetime { get; set; } // When driver left/started delivery
        
        // 🔥 Statut et assignation
        public string Status { get; set; } = "Available"; // Available, Pending, Completed
        public string? AssignedDriver { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; } // Delivery completion date/time
    }
}