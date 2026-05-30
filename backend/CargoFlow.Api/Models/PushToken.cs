namespace CargoFlow.Api.Models
{
    public class PushToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = "";
        public int? UserId { get; set; }
        public bool IsAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
