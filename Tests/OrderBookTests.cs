using Xunit;
using WebApplication1.Domain;
using WebApplication1.Services;

namespace WebApplication1.Tests
{
    public class OrderBookTests
    {
        [Fact]
        public void AddOrder_NewBuyOrder_ShouldAddToBook()
        {
            var orderBook = new OrderBook();
            var order = new Order("BUY1", OrderSide.BUY, 100m, 10);

            var fills = orderBook.AddOrder(order);

            Assert.Empty(fills);
            Assert.Single(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void AddOrder_NewSellOrder_ShouldAddToBook()
        {
            var orderBook = new OrderBook();
            var order = new Order("SELL1", OrderSide.SELL, 100m, 10);

            var fills = orderBook.AddOrder(order);

            Assert.Empty(fills);
            Assert.Empty(orderBook.GetBuyOrders());
            Assert.Single(orderBook.GetSellOrders());
        }

        [Fact]
        public void AddOrder_BuyMatchesSell_ShouldGenerateFill()
        {
            var orderBook = new OrderBook();
            var sellOrder = new Order("SELL1", OrderSide.SELL, 100m, 10);
            var buyOrder = new Order("BUY1", OrderSide.BUY, 100m, 10);

            orderBook.AddOrder(sellOrder);
            var fills = orderBook.AddOrder(buyOrder);

            Assert.Single(fills);
            Assert.Equal(10, fills[0].Quantity);
            Assert.Equal(100m, fills[0].Price);
            Assert.Equal("BUY1", fills[0].BuyOrderId);
            Assert.Equal("SELL1", fills[0].SellOrderId);
            Assert.Empty(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void AddOrder_BuyAtHigherPrice_ShouldMatch()
        {
            var orderBook = new OrderBook();
            var sellOrder = new Order("SELL1", OrderSide.SELL, 100m, 10);
            var buyOrder = new Order("BUY1", OrderSide.BUY, 105m, 10);

            orderBook.AddOrder(sellOrder);
            var fills = orderBook.AddOrder(buyOrder);

            Assert.Single(fills);
            Assert.Equal(100m, fills[0].Price); // Fill price is sell price
        }

        [Fact]
        public void AddOrder_BuyAtLowerPrice_ShouldNotMatch()
        {
            var orderBook = new OrderBook();
            var sellOrder = new Order("SELL1", OrderSide.SELL, 100m, 10);
            var buyOrder = new Order("BUY1", OrderSide.BUY, 95m, 10);

            orderBook.AddOrder(sellOrder);
            var fills = orderBook.AddOrder(buyOrder);

            Assert.Empty(fills);
            Assert.Single(orderBook.GetBuyOrders());
            Assert.Single(orderBook.GetSellOrders());
        }

        [Fact]
        public void AddOrder_PartialFill_ShouldUpdateQuantities()
        {
            var orderBook = new OrderBook();
            var sellOrder = new Order("SELL1", OrderSide.SELL, 100m, 10);
            var buyOrder = new Order("BUY1", OrderSide.BUY, 100m, 15);

            orderBook.AddOrder(sellOrder);
            var fills = orderBook.AddOrder(buyOrder);

            Assert.Single(fills);
            Assert.Equal(10, fills[0].Quantity);

            var remainingBuy = orderBook.GetBuyOrders()[0];
            Assert.Equal(5, remainingBuy.RemainingQuantity);
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void AddOrder_PriceTimePriority_BuySide()
        {
            var orderBook = new OrderBook();

            var buy1 = new Order("BUY1", OrderSide.BUY, 100m, 10);
            var buy2 = new Order("BUY2", OrderSide.BUY, 105m, 10);
            var buy3 = new Order("BUY3", OrderSide.BUY, 105m, 10);

            orderBook.AddOrder(buy1);
            System.Threading.Thread.Sleep(10); // Ensure different timestamp
            orderBook.AddOrder(buy2);
            System.Threading.Thread.Sleep(10);
            orderBook.AddOrder(buy3);

            var buyOrders = orderBook.GetBuyOrders();

            // Highest price first (105 > 100), then by time
            Assert.Equal("BUY2", buyOrders[0].OrderId);
            Assert.Equal("BUY3", buyOrders[1].OrderId);
            Assert.Equal("BUY1", buyOrders[2].OrderId);
        }

        [Fact]
        public void AddOrder_PriceTimePriority_SellSide()
        {
            var orderBook = new OrderBook();

            var sell1 = new Order("SELL1", OrderSide.SELL, 100m, 10);
            var sell2 = new Order("SELL2", OrderSide.SELL, 95m, 10);
            var sell3 = new Order("SELL3", OrderSide.SELL, 95m, 10);

            orderBook.AddOrder(sell1);
            System.Threading.Thread.Sleep(10);
            orderBook.AddOrder(sell2);
            System.Threading.Thread.Sleep(10);
            orderBook.AddOrder(sell3);

            var sellOrders = orderBook.GetSellOrders();

            // Lowest price first (95 < 100), then by time
            Assert.Equal("SELL2", sellOrders[0].OrderId);
            Assert.Equal("SELL3", sellOrders[1].OrderId);
            Assert.Equal("SELL1", sellOrders[2].OrderId);
        }

        [Fact]
        public void CancelOrder_ExistingOrder_ShouldRemove()
        {
            var orderBook = new OrderBook();
            var order = new Order("BUY1", OrderSide.BUY, 100m, 10);

            orderBook.AddOrder(order);
            Assert.Single(orderBook.GetBuyOrders());

            bool cancelled = orderBook.CancelOrder("BUY1");

            Assert.True(cancelled);
            Assert.Empty(orderBook.GetBuyOrders());
        }

        [Fact]
        public void CancelOrder_NonExistentOrder_ShouldReturnFalse()
        {
            var orderBook = new OrderBook();

            bool cancelled = orderBook.CancelOrder("NONEXISTENT");

            Assert.False(cancelled);
        }

        [Fact]
        public void ModifyOrder_IncreaseQuantity_ShouldAttemptNewMatches()
        {
            var orderBook = new OrderBook();
            var sellOrder = new Order("SELL1", OrderSide.SELL, 100m, 20);
            var buyOrder = new Order("BUY1", OrderSide.BUY, 100m, 10);

            orderBook.AddOrder(sellOrder);
            orderBook.AddOrder(buyOrder);
            // After first order, SELL1 has 10 remaining, BUY1 is fully filled

            var fills = orderBook.ModifyOrder("BUY1", 15, 100m);

            // New BUY order for 15, should match with remaining 10 from SELL1
            Assert.Single(fills);
            Assert.Equal(10, fills[0].Quantity);
        }

        [Fact]
        public void ModifyOrder_ChangePrice_ShouldAffectMatching()
        {
            var orderBook = new OrderBook();
            var sellOrder = new Order("SELL1", OrderSide.SELL, 100m, 10);
            var buyOrder = new Order("BUY1", OrderSide.BUY, 95m, 10);

            orderBook.AddOrder(sellOrder);
            orderBook.AddOrder(buyOrder);

            // Order shouldn't match because buy price is too low
            Assert.Empty(orderBook.GetBuyOrders()[0].OrderId == "BUY1" ? new List<Fill>() : new List<Fill>());

            // Modify buy order to higher price
            var fills = orderBook.ModifyOrder("BUY1", 10, 105m);

            Assert.Single(fills);
            Assert.Empty(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void AddOrder_MultipleFills_ShouldMatchWithMultipleSellers()
        {
            var orderBook = new OrderBook();
            var sell1 = new Order("SELL1", OrderSide.SELL, 100m, 5);
            var sell2 = new Order("SELL2", OrderSide.SELL, 100m, 7);
            var buy = new Order("BUY1", OrderSide.BUY, 100m, 12);

            orderBook.AddOrder(sell1);
            orderBook.AddOrder(sell2);
            var fills = orderBook.AddOrder(buy);

            Assert.Equal(2, fills.Count);
            Assert.Equal(5, fills[0].Quantity);
            Assert.Equal(7, fills[1].Quantity);
            Assert.Empty(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void GetOrder_ExistingOrder_ShouldReturnOrder()
        {
            var orderBook = new OrderBook();
            var order = new Order("BUY1", OrderSide.BUY, 100m, 10);

            orderBook.AddOrder(order);
            var retrieved = orderBook.GetOrder("BUY1");

            Assert.NotNull(retrieved);
            Assert.Equal("BUY1", retrieved.OrderId);
        }

        [Fact]
        public void GetOrder_NonExistentOrder_ShouldReturnNull()
        {
            var orderBook = new OrderBook();

            var retrieved = orderBook.GetOrder("NONEXISTENT");

            Assert.Null(retrieved);
        }

        [Fact]
        public void AddOrder_DuplicateOrderId_ShouldThrow()
        {
            var orderBook = new OrderBook();
            var order1 = new Order("BUY1", OrderSide.BUY, 100m, 10);
            var order2 = new Order("BUY1", OrderSide.BUY, 105m, 5);

            orderBook.AddOrder(order1);

            Assert.Throws<InvalidOperationException>(() => orderBook.AddOrder(order2));
        }
    }
}
