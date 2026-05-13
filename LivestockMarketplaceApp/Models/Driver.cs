namespace LivestockMarketplaceAppMVC.Models
{
    public class Driver
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string CarType { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
