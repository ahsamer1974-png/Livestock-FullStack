using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockMarketplaceApp.Models 
{
    public class TransportRequest
    {
        [Key]
        public int Id { get; set; }

        public int ListingId { get; set; }
        [ForeignKey("ListingId")]
        public Listing Listing { get; set; }

        public string BuyerId { get; set; }

        public string? TransporterId { get; set; }

        [Required]
        public string DeliveryAddress { get; set; }

        // تم إضافة هذا السطر لحل التحذير الأصفر
        [Column(TypeName = "decimal(18,2)")]
        public decimal TransportFee { get; set; }

        public int Status { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}