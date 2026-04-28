using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LivestockMarketplaceApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "رقم الجوال مطلوب")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string? City { get; set; }

        public DateTime? BirthDate { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.Now;

        public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}