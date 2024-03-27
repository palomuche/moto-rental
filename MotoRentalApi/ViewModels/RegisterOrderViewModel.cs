using MotoRentalApi.Entities;

namespace MotoRentalApi.ViewModels
{
    public class RegisterOrderViewModel
    {
        public DateTime? CreationDate { get; set; } = DateTime.UtcNow;

        public decimal RidePrice { get; set; }
    }
}
