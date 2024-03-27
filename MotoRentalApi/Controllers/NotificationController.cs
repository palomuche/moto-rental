using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MotoRentalApi.Data;
using MotoRentalApi.Entities;
using MotoRentalApi.Migrations;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace MotoRentalApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : Controller
    {
        private readonly ApiDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<MotoController> _logger;

        public NotificationController(ApiDbContext context, 
                                      ILogger<MotoController> logger, 
                                      UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            var notifications = _context.Notifications
                .Include(n => n.Deliverer)
                .Include(n => n.Order)
                .OrderBy(n => n.Id)
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Post()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "notification_queue",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            int? orderId = null;
            string? delivererId = null;

            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    if (body.Length > 0)
                    {
                        var message = Encoding.UTF8.GetString(body);

                        _logger.LogInformation(message);

                        dynamic orderInfo = JsonConvert.DeserializeObject(message);
                        orderId = orderInfo.OrderId;
                        delivererId = orderInfo.DelivererId;

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
                                DelivererId = delivererId,
                                OrderId = (int)orderId,
                            };
                            context.Notifications.Add(notification);
                            context.SaveChanges();
                            channel.BasicAck(ea.DeliveryTag, false);
                        }

                    }
                    else
                    {
                        throw new Exception("Received empty message.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            channel.BasicConsume(queue: "notification_queue",
                                    autoAck: false,
                                    consumer: consumer);        

            return Ok("Started consuming notifications.");
        }
    }
}
