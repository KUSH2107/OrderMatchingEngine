namespace WebApplication1.Domain
{
    public class Order
    {
        public string OrderId { get; set; }
        public OrderSide Side { get; set; }
        public decimal Price { get; set; }
        public long Quantity { get; set; }
        public long FilledQuantity { get; set; }
        public DateTime Timestamp { get; set; }

        public Order(string orderId, OrderSide side, decimal price, long quantity, DateTime? timestamp = null)
        {
            OrderId = orderId;
            Side = side;
            Price = price;
            Quantity = quantity;
            FilledQuantity = 0;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }

        public long RemainingQuantity => Quantity - FilledQuantity;

        public override string ToString()
        {
            return $"{Side} {RemainingQuantity}@{Price} (OrderId: {OrderId}, Filled: {FilledQuantity}/{Quantity}, Time: {Timestamp:yyyy-MM-dd HH:mm:ss.fff})";
        }
    }
}
