using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MotoRentalApi.Data;
using MotoRentalApi.Entities;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Newtonsoft.Json;

namespace MotoRentalApi.Controllers
{
    [Authorize(Roles = "Deliverer")]
    [ApiController]
    [Route("api/rental")]
    public class RentalController : Controller
    {

        private readonly ApiDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RentalController> _logger;

        public RentalController(ApiDbContext context,
                                UserManager<IdentityUser> userManager,
                                ILogger<RentalController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("rent/{rentalPlan:int}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Post(RentalPlanType rentalPlan)
        {
            // Get the authenticated user
            var username = _userManager.GetUserName(User);
            var user = await _userManager.FindByNameAsync(username);
            var deliverer = await _context.Deliverers.FindAsync(user.Id);

            if (deliverer == null)
            {
                return BadRequest("Deliverer not found.");
            }

            // Check if the deliverer is allowed to rent a motorcycle (category A or A+B)
            if (deliverer.DriverLicenseType == LicenseType.B)
            {
                return BadRequest("Only deliverers with driver license category A or A+B are allowed to rent a motorcycle.");
            }

            var startDate = DateTime.UtcNow.Date.AddDays(1);
            var endDate = DateTime.UtcNow.Date.AddDays((int)rentalPlan);

            // Check if motorcycle is available
            var moto = GetAvailableMoto(startDate, endDate);
            if (moto == null)
            {
                return BadRequest("No moto available for rent.");
            }

            var rental = new Rental()
            {
                StartDate = startDate,
                PredictedEndDate = endDate,
                RentalPlan = rentalPlan,
                MotoId = moto.Id,
                DelivererId = deliverer.Id
            };

            // Save rental
            _context.Rentals.Add(rental);
            await _context.SaveChangesAsync();

            var rentalInfo = new
            {
                MotoId = moto.Id,
                Plate = moto.Plate,
                RentalId = rental.Id,
                StartDate = rental.StartDate.ToString("dd/MM/yyyy"),
                PredictedEndDate = rental.PredictedEndDate.ToString("dd/MM/yyyy"),
                Message = "Moto rented successfully."
            };

            return Ok(rentalInfo);
        }

        [HttpPost("return/{rentalId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Post(int rentalId, [FromBody] DateTime returnDateTime)
        {
            // Get the authenticated user
            var username = _userManager.GetUserName(User);
            var user = await _userManager.FindByNameAsync(username);
            var deliverer = await _context.Deliverers.FindAsync(user.Id);

            var rental = _context.Rentals.FirstOrDefault(r => r.Id == rentalId && r.DelivererId == deliverer.Id);
            if (rental == null) return NotFound("Rental not found.");

            var returnDate = returnDateTime.Date;

            // Calculate the days between the start date and the actual return date
            var rentedDays = (returnDate - rental.StartDate).Days + 1; // Including the return day
            var predictedDays = (rental.PredictedEndDate - rental.StartDate).Days + 1;
            var dailyRate = GetDailyRate(rental.RentalPlan);
            var penaltyRate = GetPenaltyRate(rental.RentalPlan);
            var totalCost = 0m;

            if (returnDate <= rental.PredictedEndDate)
            {
                // Calculate cost for the rented period
                var rentalPeriodCost = rentedDays * dailyRate;

                // Calculate penalty if returned before the predicted end date
                var unutilizedDays = predictedDays - rentedDays;
                decimal penaltyCost = unutilizedDays * dailyRate * penaltyRate;
                totalCost = rentalPeriodCost + penaltyCost;

                var costInfo = new
                {
                    RentalPlan = rental.RentalPlan,
                    RentedDays = rentedDays,
                    DailyRate = FormatCurrency(dailyRate),
                    RentalPeriodCost = FormatCurrency(rentalPeriodCost),
                    UnutilizedDays = unutilizedDays,
                    PenaltyRate = penaltyRate.ToString("P"),
                    PenaltyCost = FormatCurrency(penaltyCost),
                    TotalCost = FormatCurrency(totalCost),
                };

                string costInfoJson = JsonConvert.SerializeObject(costInfo);

                _logger.LogInformation(costInfoJson);
            }
            else
            {
                // Calculate cost for the predicted period
                var rentalPeriodCost = predictedDays * dailyRate;

                // Add additional charge for extra days
                var extraDays = rentedDays - predictedDays;
                var extraDayCost = 50m;
                var totalExtraDaysCost = extraDays * extraDayCost; // Additional charge per extra day
                totalCost = rentalPeriodCost + totalExtraDaysCost;

                var costInfo = new
                {
                    RentalPlan = rental.RentalPlan,
                    DailyRate = FormatCurrency(dailyRate),
                    RentalPeriodCost = FormatCurrency(rentalPeriodCost),
                    ExtraDays = extraDays,
                    CostPerExtraDay = FormatCurrency(extraDayCost),
                    TotalCostExtraDays = FormatCurrency(totalExtraDaysCost),
                    TotalCost = FormatCurrency(totalCost),
                };
                string costInfoJson = JsonConvert.SerializeObject(costInfo);

                _logger.LogInformation(costInfoJson);
            }

            rental.TotalCost = totalCost;
            rental.EndDate = returnDate;
            _context.Update(rental);
            _context.SaveChanges();

            return Ok(new { TotalCost = totalCost, Message = "The return date has been successfully recorded." });
        }


        private Moto GetAvailableMoto(DateTime startDate, DateTime endDate)
        {
            var occupiedMotos = _context.Rentals
                .Where(r => r.EndDate == null ||
                        (r.StartDate <= endDate && startDate <= r.EndDate)
                )
                .Select(r => r.MotoId)
                .Distinct();

            var availableMoto = _context.Motos
                .FirstOrDefault(m => !occupiedMotos.Contains(m.Id));

            return availableMoto;
        }

        /// <summary>
        /// Calculate the cost per day based on the selected plan
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        private decimal GetDailyRate(RentalPlanType plan)
        {
            switch (plan)
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

        /// <summary>
        /// Calculate the penalry per day based on the selected plan
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        private decimal GetPenaltyRate(RentalPlanType plan)
        {
            switch (plan)
            {
                case RentalPlanType.SevenDays: // 7-day plan
                    return 0.2m;
                case RentalPlanType.FifteenDays:// 15-day plan
                    return 0.4m;
                case RentalPlanType.ThirtyDays: // 30-day plan
                    return 0.6m;
                default:
                    throw new ArgumentException("Invalid rental plan");
            }
        }

        public static string FormatCurrency(decimal value)
        {
            CultureInfo culture = new CultureInfo("pt-BR");
            return value.ToString("C", culture);
        }
    }
}
