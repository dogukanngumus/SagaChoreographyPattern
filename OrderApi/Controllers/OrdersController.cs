using MassTransit;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Dtos;
using OrderApi.Models;
using Shared;

namespace OrderApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly IPublishEndpoint _publishEndpoint;

        public OrdersController(AppDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> Create(OrderCreateDto orderCreate)
        {
            // We can use auto mapper in here.
            var newOrder = new Models.Order
            {
                BuyerId = orderCreate.BuyerId,
                Status = OrderStatus.Suspend,
                Address = new Address { Line = orderCreate.Address.Line, Province = orderCreate.Address.Province, District = orderCreate.Address.District },
                CreatedDate = DateTime.Now
            };

            orderCreate.orderItems.ForEach(item =>
            {
                newOrder.Items.Add(new OrderItem() { Price = item.Price, ProductId = item.ProductId, Count = item.Count });
            });

            await _context.AddAsync(newOrder);

            // We are commiting the transaction.
            await _context.SaveChangesAsync();

            // After that we have to inform the other project so i created a new event in here.
            var orderCreatedEvent = new OrderCreatedEvent()
            {
                BuyerId = orderCreate.BuyerId,
                OrderId = newOrder.Id,
                Payment = new PaymentMessage
                {
                    CardName = orderCreate.payment.CardName,
                    CardNumber = orderCreate.payment.CardNumber,
                    Expiration = orderCreate.payment.Expiration,
                    CVV = orderCreate.payment.CVV,
                    TotalPrice = orderCreate.orderItems.Sum(x => x.Price * x.Count)
                },
            };

            orderCreate.orderItems.ForEach(item =>
            {
                orderCreatedEvent.orderItems.Add(new OrderItemMessage { Count = item.Count, ProductId = item.ProductId });
            });

            
            // We are publising the event. It will be create an Fanout Exchange consumer should create and bind a queue.
            await _publishEndpoint.Publish(orderCreatedEvent);

            return Ok();
        }
    }
}
