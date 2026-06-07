namespace WebApplication1.Domain
{
    public class Fill
    {
        public string BuyOrderId { get; set; }
        public string SellOrderId { get; set; }
        public decimal Price { get; set; }
        public long Quantity { get; set; }
        public DateTime FillTime { get; set; }

        public Fill(string buyOrderId, string sellOrderId, decimal price, long quantity, DateTime? fillTime = null)
        {
            BuyOrderId = buyOrderId;
            SellOrderId = sellOrderId;
            Price = price;
            Quantity = quantity;
            FillTime = fillTime ?? DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"FILL: {Quantity}@{Price} (Buy: {BuyOrderId}, Sell: {SellOrderId})";
        }
    }
}
