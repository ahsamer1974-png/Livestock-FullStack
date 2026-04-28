using System.ComponentModel.DataAnnotations;

namespace LivestockMarketplaceApp.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        public string Name { get; set; }

        public string? IconUrl { get; set; } // مسار أيقونة القسم (اختياري)

        public virtual ICollection<Listing>? Listings { get; set; }
    }
}
