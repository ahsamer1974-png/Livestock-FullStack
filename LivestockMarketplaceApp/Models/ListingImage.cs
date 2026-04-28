using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockMarketplaceApp.Models
{
    public class ListingImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; } // مسار الصورة في السيرفر

        public int ListingId { get; set; }
        [ForeignKey("ListingId")]
        public virtual Listing? Listing { get; set; }
    }
}
