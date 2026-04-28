using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ضرورية لميزة ForeignKey

namespace LivestockMarketplaceApp.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual User? Sender { get; set; }

        public int ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public virtual User? Receiver { get; set; }

        public int? ListingId { get; set; }
        [ForeignKey("ListingId")]
        public virtual Listing? Listing { get; set; }
    }
}