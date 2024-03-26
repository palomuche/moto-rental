using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoRentalApi.Data;
using MotoRentalApi.Entities;
using MotoRentalApi.ViewModels;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace MotoRentalApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/order")]
    public class OrderController : Controller
    {
        private readonly ApiDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApiDbContext context,
                              UserManager<IdentityUser> userManager,
                              ILogger<OrderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            var notifications = _context.Orders
                .Include(n => n.Deliverer)
                .ToList()
                .Select(s => new {
                    OrderId = s.Id,
                    CreationDate = s.CreationDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    RidePrice = s.RidePrice,
                    Status = s.Status.ToString(),
                    DelivererId = s.DelivererId,
                    DelivererUserName = s.Deliverer?.UserName,
                    DelivererName = s.Deliverer?.Name,
                    DeliveryDate = s.DeliveryDate?.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                });

            return Ok(notifications);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Post([FromBody] RegisterOrderViewModel registerOrder)
        {
            var order = new Order
            {
                RidePrice = registerOrder.RidePrice,
                Status = OrderStatus.Available,
            };

            _context.Add(order);
            _context.SaveChanges();

            var deliverersIds = _context.Rentals
                .Where(r => r.EndDate == null) // Active rentals
                .Select(r => r.DelivererId)
                .Distinct()
                .ToList();

            var deliverersIdsWithOrder = _context.Orders
                .Where(o => o.DelivererId != null && o.Status != OrderStatus.Delivered) // Deliverers with undelivered orders
                .Select(o => o.DelivererId)
                .ToList();

            var deliverersIdsToNotify = deliverersIds.Except(deliverersIdsWithOrder).ToList();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            foreach (var delivererId in deliverersIdsToNotify)
            {
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "notification_queue",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    string message = JsonConvert.SerializeObject(new { OrderId = order.Id, DelivererId = delivererId } );
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "notification_queue",
                                         basicProperties: null,
                                         body: body);
                }
            }

            return Ok(new { OrderId = order.Id, Message = "Order created."});
        }


        [Authorize(Roles = "Deliverer")]
        [HttpPost("take-order/{orderId}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult TakeOrder(int orderId)
        {
            var username = _userManager.GetUserName(User);
            var deliverer = _userManager.FindByNameAsync(username).Result;
            if (deliverer == null) return NotFound("User not found.");

            var notification = _context.Notifications
                .FirstOrDefault(m => m.OrderId == orderId && m.DelivererId == deliverer.Id);

            if (notification == null) return NotFound("Notification not found.");

            var order = _context.Orders.Find(orderId);

            order.DelivererId = deliverer.Id;
            order.Status = OrderStatus.Accepted;

            _context.Update(order);
            _context.SaveChanges();

            return Ok($"Order {order.Id} accepted.");
        }



        [Authorize(Roles = "Deliverer")]
        [HttpPost("deliver-order/{orderId}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DeliverOrder(int orderId)
        {
            var username = _userManager.GetUserName(User);
            var deliverer = _userManager.FindByNameAsync(username).Result;
            if (deliverer == null) return NotFound("User not found.");

            var order = _context.Orders.Find(orderId);

            if (order == null) return NotFound("Order not found");

            order.Status = OrderStatus.Delivered;
            order.DeliveryDate = DateTime.UtcNow;

            _context.Update(order);
            _context.SaveChanges();

            return Ok($"Order {order.Id} delivered.");
        }

    }
}
