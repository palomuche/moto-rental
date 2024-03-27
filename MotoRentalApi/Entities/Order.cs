using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MotoRentalApi.Entities
{
    public class Order
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        public decimal RidePrice { get; set; }

        public OrderStatus Status { get; set; }

        [ForeignKey("DelivererId")]
        public virtual Deliverer? Deliverer { get; set; }
        public string? DelivererId { get; set; }

        public DateTime? DeliveryDate { get; set; }
    }

    public enum OrderStatus
    {
        Available,
        Accepted,
        Delivered
    }
}
