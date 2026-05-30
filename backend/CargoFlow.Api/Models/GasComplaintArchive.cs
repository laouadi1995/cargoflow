namespace CargoFlow.Api.Models
{
    /// <summary>
    /// Archived (dismissed) complaint — never deleted, admin only dismisses from view.
    /// </summary>
    public class GasComplaintArchive
    {
        public int Id { get; set; }
        public int CargoId { get; set; }
        public string CargoName { get; set; } = "";
        public string DriverFullName { get; set; } = "";
        public int? DriverId { get; set; }
        public double GasPickup { get; set; }
        public double GasReturn { get; set; }
        public double FineAmount { get; set; }
        public DateTime? TakenAt { get; set; }
        public DateTime? LeaveAt { get; set; }
        public string? DashboardImagePickup { get; set; }
        public string? DashboardImageLeave { get; set; }
        public DateTime DismissedAt { get; set; } = DateTime.UtcNow;
    }
}
