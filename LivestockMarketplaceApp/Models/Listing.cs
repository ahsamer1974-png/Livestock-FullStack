using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockMarketplaceApp.Models
{
    public class Listing
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان الإعلان مطلوب")]
        public string Title { get; set; }

        [Required(ErrorMessage = "وصف الماشية مطلوب")]
        public string Description { get; set; }

        [Required(ErrorMessage = "السعر مطلوب")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? City { get; set; }
        public string? Age { get; set; }
        public string? Breed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSold { get; set; } = false;

        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<ListingImage> ListingImages { get; set; } = new List<ListingImage>();
    }
}
