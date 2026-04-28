using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockMarketplaceApp.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; }

        public int ListingId { get; set; }
        [ForeignKey("ListingId")]
        public virtual Listing? Listing { get; set; }

        public int BuyerId { get; set; }
        [ForeignKey("BuyerId")]
        public virtual User? Buyer { get; set; }
    }
}
