using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoRentalApi.Data;
using MotoRentalApi.Entities;
using MotoRentalApi.ViewModels;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MotoRentalApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/order")]
    public class OrderController : Controller
    {
        private readonly ApiDbContext _context;
        private readonly ConnectionFactory _factory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApiDbContext context,
                              UserManager<IdentityUser> userManager,
                              ILogger<OrderController> logger)
        {
            _context = context;
            _factory = new ConnectionFactory() { HostName = "localhost" };
            _userManager = userManager;
            _logger = logger;
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

            foreach (var delivererId in deliverersIdsToNotify)
            {
                using (var connection = _factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "notification_queue_" + delivererId,
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    string message = JsonConvert.SerializeObject(order);
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "notification_queue_" + delivererId,
                                         basicProperties: null,
                                         body: body);
                }
            }

            return Ok($"Order {order.Id} created.");
        }

        [Authorize(Roles = "Deliverer")]
        [HttpGet("consume-notifications")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ConsumeNotifications()
        {
            var username = _userManager.GetUserName(User);
            var deliverer = _userManager.FindByNameAsync(username).Result;

            if (deliverer == null) return NotFound("User not found.");

            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "notification_queue_" + deliverer.Id,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                int? orderId = null;
                consumer.Received += (model, ea) =>
                {
                    _logger.LogInformation(ea.ToString());
                    var body = ea.Body.ToArray();
                    if (body.Length > 0)
                    {
                        var message = Encoding.UTF8.GetString(body);

                        _logger.LogInformation(message);

                        dynamic orderInfo = JsonConvert.DeserializeObject(message);
                        orderId = orderInfo.Id;

                        var configuration = new ConfigurationBuilder()
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddJsonFile("appsettings.json")
                            .Build();

                        var connectionString = configuration.GetConnectionString("DefaultConnection");
                        var serviceCollection = new ServiceCollection();

                        serviceCollection.AddDbContext<ApiDbContext>(options =>
                            options.UseNpgsql(connectionString));

                        var serviceProvider = serviceCollection.BuildServiceProvider();

                        using (var scope = serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
                            var notification = new Notification
                            {
                                DelivererId = deliverer.Id,
                                OrderId = (int)orderId,
                            };
                            context.Notifications.Add(notification);
                            context.SaveChanges();
                        }
                    }
                    else
                    {
                        throw new Exception("Received empty message.");
                    }
                };

                channel.BasicConsume(queue: "notification_queue_" + deliverer.Id,
                                        autoAck: true,
                                        consumer: consumer);
            }

            return Ok("Started consuming notifications for DelivererId=" + deliverer.Id);
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
