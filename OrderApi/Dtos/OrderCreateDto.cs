namespace OrderApi.Dtos
{
    public class OrderCreateDto
    {
        public string BuyerId { get; set; }
        public List<OrderItemDto> orderItems { get; set; }
        public PaymentDto payment { get; set; }
        public AddressDto Address { get; set; }
    }
}
