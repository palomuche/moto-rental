using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MotoRentalApi.Entities
{
    public class Order
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.Now;

        public decimal RideValue { get; set; }

        public OrderStatus Status { get; set; }
    }

    public enum OrderStatus
    {
        Disponivel,
        Aceito,
        Entregue
    }
}
