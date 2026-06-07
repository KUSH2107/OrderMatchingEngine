using Xunit;
using WebApplication1.Domain;
using WebApplication1.Services;

namespace WebApplication1.Tests
{
    public class Task2CommandTests
    {
        private readonly OrderMatchingEngine _engine;

        public Task2CommandTests()
        {
            _engine = new OrderMatchingEngine();
        }

        #region FILL Command Tests

        [Fact]
        public void HandleFill_ValidFill_WithoutMatchingOrders_ShouldRecordFill()
        {
            // Arrange - Create orders but don't let them match
            _engine.ProcessCommand("NEW BUY001 BUY 99.00 100");
            _engine.ProcessCommand("NEW SELL001 SELL 101.00 100");

            // Act
            var fills = _engine.ProcessCommand("FILL BUY001 SELL001 100.00 100");

            // Assert
            Assert.Single(fills);
            Assert.Equal("BUY001", fills[0].BuyOrderId);
            Assert.Equal("SELL001", fills[0].SellOrderId);
            Assert.Equal(100m, fills[0].Price);
            Assert.Equal(100, fills[0].Quantity);

            var tracker = _engine.GetPositionTracker();
            Assert.Equal(100, tracker.NetQty);
            Assert.Equal(100m, tracker.AvgPrice);
        }

        [Fact]
        public void HandleFill_NonExistentBuyOrder_ShouldThrow()
        {
            // Arrange
            _engine.ProcessCommand("NEW SELL001 SELL 100.00 100");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("FILL BUY999 SELL001 100.00 100"));
            Assert.Contains("Buy order BUY999 not found", ex.Message);
        }

        [Fact]
        public void HandleFill_NonExistentSellOrder_ShouldThrow()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 100.00 100");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("FILL BUY001 SELL999 100.00 100"));
            Assert.Contains("Sell order SELL999 not found", ex.Message);
        }

        [Fact]
        public void HandleFill_InvalidPrice_ShouldThrow()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 100.00 100");
            _engine.ProcessCommand("NEW SELL001 SELL 100.00 100");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("FILL BUY001 SELL001 -50 100"));
            Assert.Contains("Price must be a positive number", ex.Message);
        }

        [Fact]
        public void HandleFill_InvalidQuantity_ShouldThrow()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 100.00 100");
            _engine.ProcessCommand("NEW SELL001 SELL 100.00 100");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("FILL BUY001 SELL001 100.00 -50"));
            Assert.Contains("Quantity must be a positive number", ex.Message);
        }

        [Fact]
        public void HandleFill_WrongCommandFormat_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("FILL BUY001 SELL001 100.00"));
            Assert.Contains("FILL command requires", ex.Message);
        }

        #endregion

        #region PRICE Command Tests

        [Fact]
        public void HandlePrice_ValidPrice_ShouldUpdateMarketPrice()
        {
            // Act
            _engine.ProcessCommand("PRICE 105.00");

            // Assert
            Assert.Equal(105m, _engine.GetCurrentMarketPrice());
        }

        [Fact]
        public void HandlePrice_InvalidPrice_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("PRICE -50"));
            Assert.Contains("Price must be a positive number", ex.Message);
        }

        [Fact]
        public void HandlePrice_ZeroPrice_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("PRICE 0"));
            Assert.Contains("Price must be a positive number", ex.Message);
        }

        [Fact]
        public void HandlePrice_WrongCommandFormat_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _engine.ProcessCommand("PRICE"));
            Assert.Contains("PRICE command requires", ex.Message);
        }

        [Fact]
        public void HandlePrice_MultipleUpdates_ShouldUpdateToLatestPrice()
        {
            // Act
            _engine.ProcessCommand("PRICE 100.00");
            Assert.Equal(100m, _engine.GetCurrentMarketPrice());

            _engine.ProcessCommand("PRICE 110.00");
            Assert.Equal(110m, _engine.GetCurrentMarketPrice());

            _engine.ProcessCommand("PRICE 95.00");
            Assert.Equal(95m, _engine.GetCurrentMarketPrice());

            // Assert
            Assert.Equal(95m, _engine.GetCurrentMarketPrice());
        }

        #endregion

        #region Position & PnL Integration Tests

        [Fact]
        public void PositionTracker_LongPosition_FromAutomaticMatching_ShouldCalculateUnrealizedPnL()
        {
            // Arrange - Create matching orders so they automatically fill
            _engine.ProcessCommand("NEW BUY001 BUY 100.00 100");
            _engine.ProcessCommand("NEW SELL001 SELL 100.00 100");

            // Act
            _engine.ProcessCommand("PRICE 105.00");

            // Assert
            var tracker = _engine.GetPositionTracker();
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(105m);
            Assert.Equal(500m, unrealizedPnL); // (105 - 100) * 100 = 500
        }

        [Fact]
        public void PositionTracker_FlatPosition_ShouldHaveZeroUnrealizedPnL()
        {
            // Arrange
            var tracker = _engine.GetPositionTracker();

            // Act
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(105m);

            // Assert
            Assert.Equal(0m, unrealizedPnL);
        }

        #endregion

        #region Integration Tests with All Task 1 & Task 2 Commands

        [Fact]
        public void AllCommands_OrderMatching_AndPriceUpdate_ShouldExecuteSuccessfully()
        {
            // Arrange & Act
            _engine.ProcessCommand("NEW ORD001 BUY 100.00 100");
            _engine.ProcessCommand("NEW ORD002 SELL 100.00 100");
            _engine.ProcessCommand("PRICE 105.00");

            // Assert
            var tracker = _engine.GetPositionTracker();
            Assert.Equal(100, tracker.NetQty);
            Assert.Equal(100m, tracker.AvgPrice);
            Assert.Equal(0m, tracker.RealizedPnL);
            Assert.Equal(500m, tracker.CalculateUnrealizedPnL(105m));
        }

        [Fact]
        public void AllCommands_CancelAndModifyOrders_ShouldWorkCorrectly()
        {
            // Arrange & Act
            _engine.ProcessCommand("NEW ORD001 BUY 100.00 100");
            _engine.ProcessCommand("NEW ORD002 SELL 100.00 50");
            // First order partially matches (50 shares)

            _engine.ProcessCommand("MODIFY ORD001 101.00 150");
            _engine.ProcessCommand("PRICE 102.00");

            // Assert
            var tracker = _engine.GetPositionTracker();
            Assert.Equal(50, tracker.NetQty); // 100 - 50 from auto-match
            Assert.Equal(100m, tracker.AvgPrice);
        }

        [Fact]
        public void AllCommands_ComplexOrderMatching_ShouldTrackPositionCorrectly()
        {
            // Arrange & Act
            _engine.ProcessCommand("NEW BUY1 BUY 100.00 100");
            _engine.ProcessCommand("NEW SELL1 SELL 100.00 50");  // Partial match
            _engine.ProcessCommand("NEW SELL2 SELL 100.00 50");  // Complete match

            _engine.ProcessCommand("PRICE 110.00");

            // Assert
            var tracker = _engine.GetPositionTracker();
            Assert.Equal(100, tracker.NetQty);
            Assert.Equal(100m, tracker.AvgPrice);
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(110m);
            Assert.Equal(1000m, unrealizedPnL); // (110 - 100) * 100
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EdgeCase_LargeQuantityMatching_ShouldCalculateCorrectly()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 1000.00 1000000");
            _engine.ProcessCommand("NEW SELL001 SELL 1000.00 1000000");

            // Act
            _engine.ProcessCommand("PRICE 1100.00");

            // Assert
            var tracker = _engine.GetPositionTracker();
            Assert.Equal(1000000, tracker.NetQty);
            Assert.Equal(100000000m, tracker.CalculateUnrealizedPnL(1100m)); // (1100 - 1000) * 1000000
        }

        [Fact]
        public void EdgeCase_SmallPriceDifferences_ShouldCalculateAccurately()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 100.00 1");
            _engine.ProcessCommand("NEW SELL001 SELL 100.00 1");

            // Act
            _engine.ProcessCommand("PRICE 100.01");

            // Assert
            var tracker = _engine.GetPositionTracker();
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(100.01m);
            Assert.Equal(0.01m, unrealizedPnL);
        }

        [Fact]
        public void EdgeCase_DecimalPrice_ShouldHandleCorrectly()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 100.555 100");
            _engine.ProcessCommand("NEW SELL001 SELL 100.555 100");

            // Act & Assert
            var tracker = _engine.GetPositionTracker();
            Assert.Equal(100.555m, tracker.AvgPrice);
        }

        #endregion
    }

    public class Task2APIEndpointTests
    {
        private readonly OrderMatchingEngine _engine;

        public Task2APIEndpointTests()
        {
            _engine = new OrderMatchingEngine();
        }

        [Fact]
        public void GetPosition_ReturnsAllPositionMetrics()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 100.00 100");
            _engine.ProcessCommand("NEW SELL001 SELL 100.00 100");
            _engine.ProcessCommand("PRICE 110.00");

            // Act
            var tracker = _engine.GetPositionTracker();
            var currentPrice = _engine.GetCurrentMarketPrice();
            var unrealizedPnL = tracker.CalculateUnrealizedPnL(currentPrice);

            // Assert
            Assert.Equal(100, tracker.NetQty);
            Assert.Equal(100m, tracker.AvgPrice);
            Assert.Equal(0m, tracker.RealizedPnL);
            Assert.Equal(1000m, unrealizedPnL);
            Assert.Equal(110m, currentPrice);
        }

        [Fact]
        public void SetMarketPrice_UpdatesCurrentPrice()
        {
            // Act
            _engine.ProcessCommand("PRICE 125.00");

            // Assert
            Assert.Equal(125m, _engine.GetCurrentMarketPrice());
        }

        [Fact]
        public void CreateFill_WithoutMatchingOrders_RecordsAndUpdatesPosition()
        {
            // Arrange
            _engine.ProcessCommand("NEW BUY001 BUY 99.00 100");
            _engine.ProcessCommand("NEW SELL001 SELL 101.00 100");

            // Act
            var fills = _engine.ProcessCommand("FILL BUY001 SELL001 100.00 100");

            // Assert
            Assert.Single(fills);
            var tracker = _engine.GetPositionTracker();
            Assert.Equal(100, tracker.NetQty);
        }
    }
}

