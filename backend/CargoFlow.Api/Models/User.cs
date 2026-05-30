namespace CargoFlow.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public string DriverLicense { get; set; }
        public string DeliveryId { get; set; }
        public bool IsAdmin { get; set; } = false;
    }
}