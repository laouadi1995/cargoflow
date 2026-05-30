namespace CargoFlow.Api.Models
{
    public class Truck
    {
        public int Id { get; set; }

        public string TruckName { get; set; } = "";

        public string Status { get; set; } = "Available";
    }
}