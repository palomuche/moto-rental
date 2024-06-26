﻿using System.ComponentModel.DataAnnotations.Schema;
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

        public decimal? TotalCost { get; set; }

        public RentalPlanType RentalPlan { get; set; }

        [ForeignKey("DelivererId")]
        public virtual Deliverer? Deliverer { get; set; }
        public required string DelivererId { get; set; }

        [ForeignKey("MotoId")]
        public virtual Moto? Moto { get; set; }
        public int MotoId { get; set; }

    }

    public enum RentalPlanType
    {
        SevenDays = 7,
        FifteenDays = 15,
        ThirtyDays = 30
    }
}
