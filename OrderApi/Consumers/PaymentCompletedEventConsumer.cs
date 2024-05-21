using MassTransit;
using OrderApi.Models;
using Shared;

namespace OrderApi.Consumers
{
    public class PaymentCompletedEventConsumer : IConsumer<PaymentSuccessedEvent>
    {
        private readonly AppDbContext _context;

        private readonly ILogger<PaymentCompletedEventConsumer> _logger;

        public PaymentCompletedEventConsumer(AppDbContext context, ILogger<PaymentCompletedEventConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentSuccessedEvent> context)
        {
            var order = await _context.Orders.FindAsync(context.Message.orderId);

            if (order != null)
            {
                order.Status = OrderStatus.Complete;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order (Id={context.Message.orderId}) status changed : {order.Status}");
            }
            else
            {
                _logger.LogError($"Order (Id={context.Message.orderId}) not found");
            }
        }
    }
}
