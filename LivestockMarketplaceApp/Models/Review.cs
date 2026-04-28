using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockMarketplaceApp.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } // من 1 إلى 5 نجوم

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // الشخص المقيَّم (صاحب الحلال)
        public int SellerId { get; set; }

        // الشخص الذي كتب التقييم
        public int ReviewerId { get; set; }

        [ForeignKey("ReviewerId")]
        public virtual User? Reviewer { get; set; }
    }
}
