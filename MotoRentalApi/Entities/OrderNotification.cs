using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MotoRentalApi.Entities
{
    public class OrderNotification
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime NotificationDate { get; set; } = DateTime.Now;
    }
}
