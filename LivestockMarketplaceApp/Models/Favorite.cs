using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockMarketplaceApp.Models
{
    public class Favorite
    {
        [Key]
        public int Id { get; set; }

        // ربط السجل بالمستخدم
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; } // أضفنا هذا السطر لربط المستخدم

        // ربط السجل بالإعلان
        public int ListingId { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing? Listing { get; set; }
    }
}