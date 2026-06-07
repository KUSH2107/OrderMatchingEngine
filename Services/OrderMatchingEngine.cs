namespace WebApplication1.Services
{
    using WebApplication1.Domain;
    using System.Collections.Generic;

    /// <summary>
    /// Command processor for the order matching engine.
    /// Supports: NEW, CANCEL, MODIFY, PRINT, FILL, PRICE commands.
    /// </summary>
    public class OrderMatchingEngine
    {
        private readonly OrderBook _orderBook;
        private readonly PositionTracker _positionTracker;
        private decimal _currentMarketPrice = 100m;

        public OrderMatchingEngine()
        {
            _orderBook = new OrderBook();
            _positionTracker = new PositionTracker();
        }

        public List<Fill> ProcessCommand(string command)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                throw new ArgumentException("Empty command");

            return parts[0].ToUpper() switch
            {
                "NEW" => HandleNewOrder(parts),
                "CANCEL" => HandleCancelOrder(parts),
                "MODIFY" => HandleModifyOrder(parts),
                "FILL" => HandleFill(parts),
                "PRICE" => HandlePrice(parts),
                "PRINT" => HandlePrint(parts),
                _ => throw new ArgumentException($"Unknown command: {parts[0]}")
            };
        }

        private List<Fill> HandleNewOrder(string[] parts)
        {
            // NEW <OrderId> <BUY|SELL> <Price> <Quantity>
            if (parts.Length != 5)
                throw new ArgumentException("NEW command requires: NEW <OrderId> <BUY|SELL> <Price> <Quantity>");

            var orderId = parts[1];
            var side = Enum.Parse<OrderSide>(parts[2].ToUpper());
            if (!decimal.TryParse(parts[3], out var price) || price <= 0)
                throw new ArgumentException("Price must be a positive number");
            if (!long.TryParse(parts[4], out var quantity) || quantity <= 0)
                throw new ArgumentException("Quantity must be a positive number");

            var order = new Order(orderId, side, price, quantity);
            var fills = _orderBook.AddOrder(order);

            // Process fills through PositionTracker
            foreach (var fill in fills)
            {
                _positionTracker.ProcessFill(fill, OrderSide.BUY);
            }

            return fills;
        }

        private List<Fill> HandleCancelOrder(string[] parts)
        {
            // CANCEL <OrderId>
            if (parts.Length != 2)
                throw new ArgumentException("CANCEL command requires: CANCEL <OrderId>");

            var orderId = parts[1];
            bool cancelled = _orderBook.CancelOrder(orderId);

            if (!cancelled)
                throw new ArgumentException($"Order {orderId} not found");

            return new List<Fill>();
        }

        private List<Fill> HandleModifyOrder(string[] parts)
        {
            // MODIFY <OrderId> <NewPrice> <NewQuantity>
            if (parts.Length != 4)
                throw new ArgumentException("MODIFY command requires: MODIFY <OrderId> <NewPrice> <NewQuantity>");

            var orderId = parts[1];
            if (!decimal.TryParse(parts[2], out var newPrice) || newPrice <= 0)
                throw new ArgumentException("Price must be a positive number");
            if (!long.TryParse(parts[3], out var newQuantity) || newQuantity <= 0)
                throw new ArgumentException("Quantity must be a positive number");

            return _orderBook.ModifyOrder(orderId, newQuantity, newPrice);
        }

        private List<Fill> HandlePrint(string[] parts)
        {
            // PRINT [OrderId]
            if (parts.Length == 1)
            {
                _orderBook.PrintOrderBook();
            }
            else if (parts.Length == 2)
            {
                var orderId = parts[1];
                var order = _orderBook.GetOrder(orderId);
                if (order == null)
                    throw new ArgumentException($"Order {orderId} not found");
                Console.WriteLine($"\n{order}\n");
            }
            else
            {
                throw new ArgumentException("PRINT command: PRINT [OrderId]");
            }

            return new List<Fill>();
        }

        private List<Fill> HandleFill(string[] parts)
        {
            // FILL <BuyOrderId> <SellOrderId> <Price> <Quantity>
            if (parts.Length != 5)
                throw new ArgumentException("FILL command requires: FILL <BuyOrderId> <SellOrderId> <Price> <Quantity>");

            var buyOrderId = parts[1];
            var sellOrderId = parts[2];
            if (!decimal.TryParse(parts[3], out var price) || price <= 0)
                throw new ArgumentException("Price must be a positive number");
            if (!long.TryParse(parts[4], out var quantity) || quantity <= 0)
                throw new ArgumentException("Quantity must be a positive number");

            // Verify both orders exist
            var buyOrder = _orderBook.GetOrder(buyOrderId);
            var sellOrder = _orderBook.GetOrder(sellOrderId);

            if (buyOrder == null)
                throw new ArgumentException($"Buy order {buyOrderId} not found");
            if (sellOrder == null)
                throw new ArgumentException($"Sell order {sellOrderId} not found");

            // Create and process fill
            var fill = new Fill(buyOrderId, sellOrderId, price, quantity);
            _positionTracker.ProcessFill(fill, OrderSide.BUY);

            return new List<Fill> { fill };
        }

        private List<Fill> HandlePrice(string[] parts)
        {
            // PRICE <CurrentPrice>
            if (parts.Length != 2)
                throw new ArgumentException("PRICE command requires: PRICE <CurrentPrice>");

            if (!decimal.TryParse(parts[1], out var price) || price <= 0)
                throw new ArgumentException("Price must be a positive number");

            _currentMarketPrice = price;
            return new List<Fill>();
        }

        public OrderBook GetOrderBook() => _orderBook;
        public PositionTracker GetPositionTracker() => _positionTracker;
        public decimal GetCurrentMarketPrice() => _currentMarketPrice;
        public void SetCurrentMarketPrice(decimal price) => _currentMarketPrice = price;
    }
}
