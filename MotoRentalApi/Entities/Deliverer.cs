using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using System;

namespace MotoRentalApi.Entities
{
    [Table("Deliverers")]
    [Index(nameof(CNPJ), IsUnique = true)]
    [Index(nameof(DriverLicenseNumber), IsUnique = true)]
    public class Deliverer : IdentityUser
    {
        public required string Name { get; set; }

        public required string CNPJ { get; set; }

        public DateTime BirthDate { get; set; }

        public required string DriverLicenseNumber { get; set; }

        public LicenseType DriverLicenseType { get; set; }

        public string? DriverLicensePhotoPath { get; set; }
    }


    public enum LicenseType
    {
        A,
        B,
        AB
    }
}
