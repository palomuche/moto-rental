using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoRentalApi.Data;
using MotoRentalApi.Entities;

namespace MotoRentalApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : Controller
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<MotoController> _logger;

        public NotificationController(ApiDbContext context, ILogger<MotoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            var notifications = _context.Notifications
                .Include(n => n.Deliverer)
                .Include(n => n.Order)
                .ToList()
                .Select(s => new {
                    NotificationId = s.Id,
                    NotificationDate = s.NotificationDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    OrderId = s.OrderId,
                    OrderStatus = s.Order?.Status.ToString(),
                    DeliveryDate = s.Order?.DeliveryDate?.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    DelivererId = s.DelivererId,
                    DelivererUserName = s.Deliverer?.UserName,
                    DelivererName = s.Deliverer?.Name,
                });

            return Ok(notifications);
        }
    }
}
