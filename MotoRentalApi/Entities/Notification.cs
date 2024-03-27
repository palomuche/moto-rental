using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MotoRentalApi.Entities
{
    public class Notification
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime NotificationDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
        public int OrderId { get; set; }

        [ForeignKey("DelivererId")]
        public virtual Deliverer? Deliverer { get; set; }
        public string? DelivererId { get; set; }
    }
}
