
using MotoRentalApi.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace MotoRentalApi.ViewModels
{
    public class RegisterDelivererViewModel : RegisterUserViewModel
    {
        public required string Name { get; set; }

        public required string CNPJ { get; set; }

        public DateTime BirthDate { get; set; }

        public required string DriverLicenseNumber { get; set; }

        public LicenseType DriverLicenseType { get; set; }
    }
}
