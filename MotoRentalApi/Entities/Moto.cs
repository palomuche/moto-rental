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

        [Required(ErrorMessage = "{0} is required")]
        public string Year { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        public string Model { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        public string Plate { get; set; }
    }
}
