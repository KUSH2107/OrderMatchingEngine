using Xunit;
using WebApplication1.Domain;

namespace WebApplication1.Tests
{
    public class PositionTrackerTests
    {
        [Fact]
        public void ProcessFill_OpenLongPosition_ShouldUpdatePosition()
        {
            var tracker = new PositionTracker();
            var fill = new Fill("BUY1", "SELL1", 100m, 10);

            tracker.ProcessFill(fill, OrderSide.BUY);

            Assert.Equal(10, tracker.NetQty);
            Assert.Equal(100m, tracker.AvgPrice);
            Assert.Equal(0m, tracker.RealizedPnL);
        }

        [Fact]
        public void ProcessFill_OpenShortPosition_ShouldUpdatePosition()
        {
            var tracker = new PositionTracker();
            var fill = new Fill("BUY1", "SELL1", 100m, 10);

            tracker.ProcessFill(fill, OrderSide.SELL);

            Assert.Equal(-10, tracker.NetQty);
            Assert.Equal(100m, tracker.AvgPrice);
            Assert.Equal(0m, tracker.RealizedPnL);
        }

        [Fact]
        public void ProcessFill_AddToPosition_ShouldUpdateAvgPrice()
        {
            var tracker = new PositionTracker();
            var fill1 = new Fill("BUY1", "SELL1", 100m, 10);
            var fill2 = new Fill("BUY2", "SELL2", 110m, 10);

            tracker.ProcessFill(fill1, OrderSide.BUY);
            tracker.ProcessFill(fill2, OrderSide.BUY);

            Assert.Equal(20, tracker.NetQty);
            Assert.Equal(105m, tracker.AvgPrice);
            Assert.Equal(0m, tracker.RealizedPnL);
        }

        [Fact]
        public void ProcessFill_ReduceLongPosition_ShouldRealizePnL()
        {
            var tracker = new PositionTracker();
            var buyFill = new Fill("BUY1", "SELL1", 100m, 10);
            var sellFill = new Fill("BUY2", "SELL2", 110m, 5);

            tracker.ProcessFill(buyFill, OrderSide.BUY);
            tracker.ProcessFill(sellFill, OrderSide.SELL);

            var netQty = tracker.NetQty;
            var avgPrice = tracker.AvgPrice;
            var realizedPnL = tracker.RealizedPnL;

            Assert.Equal(5, netQty);
            Assert.Equal(100m, avgPrice);
            Assert.Equal(50m, realizedPnL);
        }

        [Fact]
        public void ProcessFill_CloseLongPosition_ShouldRealizeFullPnL()
        {
            var tracker = new PositionTracker();
            var buyFill = new Fill("BUY1", "SELL1", 100m, 10);
            var sellFill = new Fill("BUY2", "SELL2", 105m, 10);

            tracker.ProcessFill(buyFill, OrderSide.BUY);
            tracker.ProcessFill(sellFill, OrderSide.SELL);

            Assert.Equal(0, tracker.NetQty);
            Assert.Equal(0m, tracker.AvgPrice);
            Assert.Equal(50m, tracker.RealizedPnL); // (105 - 100) * 10
        }

        [Fact]
        public void ProcessFill_ReversePosition_ShouldUpdateAvgPrice()
        {
            var tracker = new PositionTracker();
            var buyFill = new Fill("BUY1", "SELL1", 100m, 10);
            var sellFill = new Fill("BUY2", "SELL2", 105m, 15);

            tracker.ProcessFill(buyFill, OrderSide.BUY);
            tracker.ProcessFill(sellFill, OrderSide.SELL);

            Assert.Equal(-5, tracker.NetQty);
            Assert.Equal(105m, tracker.AvgPrice);
            Assert.Equal(50m, tracker.RealizedPnL); // (105 - 100) * 10 for closed portion
        }

        [Fact]
        public void CalculateUnrealizedPnL_LongPosition_HigherPrice()
        {
            var tracker = new PositionTracker();
            var fill = new Fill("BUY1", "SELL1", 100m, 10);

            tracker.ProcessFill(fill, OrderSide.BUY);
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(110m);

            Assert.Equal(100m, unrealizedPnL); // (110 - 100) * 10
        }

        [Fact]
        public void CalculateUnrealizedPnL_LongPosition_LowerPrice()
        {
            var tracker = new PositionTracker();
            var fill = new Fill("BUY1", "SELL1", 100m, 10);

            tracker.ProcessFill(fill, OrderSide.BUY);
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(90m);

            Assert.Equal(-100m, unrealizedPnL); // (90 - 100) * 10
        }

        [Fact]
        public void CalculateUnrealizedPnL_ShortPosition_LowerPrice()
        {
            var tracker = new PositionTracker();
            var fill = new Fill("BUY1", "SELL1", 100m, 10);

            tracker.ProcessFill(fill, OrderSide.SELL);
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(90m);

            Assert.Equal(100m, unrealizedPnL); // (100 - 90) * 10
        }

        [Fact]
        public void CalculateUnrealizedPnL_ShortPosition_HigherPrice()
        {
            var tracker = new PositionTracker();
            var fill = new Fill("BUY1", "SELL1", 100m, 10);

            tracker.ProcessFill(fill, OrderSide.SELL);
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(110m);

            Assert.Equal(-100m, unrealizedPnL); // (100 - 110) * 10
        }

        [Fact]
        public void CalculateUnrealizedPnL_FlatPosition_ShouldBeZero()
        {
            var tracker = new PositionTracker();
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(100m);

            Assert.Equal(0m, unrealizedPnL);
        }

        [Fact]
        public void CalculateTotalPnL_CombinedRealizedAndUnrealized()
        {
            var tracker = new PositionTracker();
            var buyFill = new Fill("BUY1", "SELL1", 100m, 10);
            var sellFill = new Fill("BUY2", "SELL2", 105m, 5);

            tracker.ProcessFill(buyFill, OrderSide.BUY);
            tracker.ProcessFill(sellFill, OrderSide.SELL);

            // Realized: (105 - 100) * 5 = 25
            // Remaining position: 5 @ 100
            // Unrealized at price 110: (110 - 100) * 5 = 50
            var totalPnL = tracker.CalculateTotalPnL(110m);

            Assert.Equal(75m, totalPnL); // 25 + 50
        }

        [Fact]
        public void ProcessFill_DuplicateFill_ShouldThrow()
        {
            var tracker = new PositionTracker();
            var fill = new Fill("BUY1", "SELL1", 100m, 10);

            tracker.ProcessFill(fill, OrderSide.BUY);

            Assert.Throws<InvalidOperationException>(() => 
                tracker.ProcessFill(fill, OrderSide.BUY));
        }

        [Fact]
        public void CalculateUnrealizedPnL_MultipleFillerClosed()
        {
            var tracker = new PositionTracker();
            var buyFill1 = new Fill("BUY1", "SELL1", 100m, 10);
            var buyFill2 = new Fill("BUY2", "SELL2", 110m, 10);

            tracker.ProcessFill(buyFill1, OrderSide.BUY);
            tracker.ProcessFill(buyFill2, OrderSide.BUY);

            var unrealizedPnL = tracker.CalculateUnrealizedPnL(115m);

            // Avg price: 105, Net Qty: 20
            // Unrealized: (115 - 105) * 20 = 200
            Assert.Equal(200m, unrealizedPnL);
        }
    }
}
