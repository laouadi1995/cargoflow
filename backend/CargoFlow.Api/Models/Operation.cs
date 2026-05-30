namespace CargoFlow.Api.Models
{
    public class Operation
    {
        public int Id { get; set; }

        public int TruckId { get; set; }

        public string DriverName { get; set; } = "";

        public string Type { get; set; } = "";

        public DateTime Date { get; set; } = DateTime.Now;
    }
}