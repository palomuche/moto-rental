using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MotoRentalApi.Entities
{
    public class Rental
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime PredictedEndDate { get; set; }

        public decimal TotalCost { get; set; }

        public RentalPlanType RentalPlan { get; set; }

        [ForeignKey("DelivererId")]
        public virtual Deliverer? Deliverer { get; set; }
        public required string DelivererId { get; set; }

        [ForeignKey("MotoId")]
        public virtual Moto? Moto { get; set; }
        public int MotoId { get; set; }


        [NotMapped]
        public decimal CostPerDay
        {
            get
            {
                // Calculate the cost per day based on the selected plan
                switch (RentalPlan)
                {
                    case RentalPlanType.SevenDays: // 7-day plan
                        return 30m;
                    case RentalPlanType.FifteenDays: // 15-day plan
                        return 28m;
                    case RentalPlanType.ThirtyDays: // 30-day plan
                        return 22m;
                    default:
                        throw new InvalidOperationException("Invalid rental plan.");
                }
            }
        }
    }

    public enum RentalPlanType
    {
        SevenDays = 7,
        FifteenDays = 15,
        ThirtyDays = 30
    }
}
