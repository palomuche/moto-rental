using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MotoRentalApi.Entities
{
    [Index(nameof(Plate), IsUnique = true)]
    public class Moto
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public required string Year { get; set; }

        public required string Model { get; set; }
        
        public required string Plate { get; set; }
    }
}
