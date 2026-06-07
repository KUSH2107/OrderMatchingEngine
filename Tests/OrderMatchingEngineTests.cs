using Xunit;
using WebApplication1.Services;
using WebApplication1.Domain;

namespace WebApplication1.Tests
{
    public class OrderMatchingEngineTests
    {
        [Fact]
        public void ProcessCommand_NewBuyOrder_ShouldAddOrder()
        {
            var engine = new OrderMatchingEngine();
            var result = engine.ProcessCommand("NEW BUY1 BUY 100 10");

            Assert.Empty(result);
            Assert.Single(engine.GetOrderBook().GetBuyOrders());
        }

        [Fact]
        public void ProcessCommand_NewSellOrder_ShouldAddOrder()
        {
            var engine = new OrderMatchingEngine();
            var result = engine.ProcessCommand("NEW SELL1 SELL 100 10");

            Assert.Empty(result);
            Assert.Single(engine.GetOrderBook().GetSellOrders());
        }

        [Fact]
        public void ProcessCommand_MatchingOrders_ShouldGenerateFill()
        {
            var engine = new OrderMatchingEngine();
            engine.ProcessCommand("NEW SELL1 SELL 100 10");
            var result = engine.ProcessCommand("NEW BUY1 BUY 100 10");

            Assert.Single(result);
            Assert.Equal(10, result[0].Quantity);
        }

        [Fact]
        public void ProcessCommand_CancelOrder_ShouldRemoveOrder()
        {
            var engine = new OrderMatchingEngine();
            engine.ProcessCommand("NEW BUY1 BUY 100 10");
            engine.ProcessCommand("CANCEL BUY1");

            Assert.Empty(engine.GetOrderBook().GetBuyOrders());
        }

        [Fact]
        public void ProcessCommand_CancelNonExistent_ShouldThrow()
        {
            var engine = new OrderMatchingEngine();

            Assert.Throws<ArgumentException>(() => 
                engine.ProcessCommand("CANCEL NONEXISTENT"));
        }

        [Fact]
        public void ProcessCommand_ModifyOrder_ShouldUpdateOrder()
        {
            var engine = new OrderMatchingEngine();
            engine.ProcessCommand("NEW SELL1 SELL 100 20");
            engine.ProcessCommand("NEW BUY1 BUY 100 10");
            // BUY1 is now fully matched (10 fills from SELL1 which has 20)
            // SELL1 has 10 remaining

            var result = engine.ProcessCommand("MODIFY BUY1 100 15");
            // Modify BUY1: cancel old order (already matched, so no change)
            // Create new BUY1 order for 15 @ 100, matches with remaining 10 from SELL1

            Assert.Single(result);
            Assert.Equal(10, result[0].Quantity);
        }

        [Fact]
        public void ProcessCommand_PrintOrderBook_ShouldNotThrow()
        {
            var engine = new OrderMatchingEngine();
            engine.ProcessCommand("NEW BUY1 BUY 100 10");
            engine.ProcessCommand("NEW SELL1 SELL 105 10");

            // Should not throw
            engine.ProcessCommand("PRINT");
        }

        [Fact]
        public void ProcessCommand_PrintSpecificOrder_ShouldNotThrow()
        {
            var engine = new OrderMatchingEngine();
            engine.ProcessCommand("NEW BUY1 BUY 100 10");

            // Should not throw
            engine.ProcessCommand("PRINT BUY1");
        }

        [Fact]
        public void ProcessCommand_PrintNonExistentOrder_ShouldThrow()
        {
            var engine = new OrderMatchingEngine();

            Assert.Throws<ArgumentException>(() => 
                engine.ProcessCommand("PRINT NONEXISTENT"));
        }

        [Fact]
        public void ProcessCommand_InvalidCommand_ShouldThrow()
        {
            var engine = new OrderMatchingEngine();

            Assert.Throws<ArgumentException>(() => 
                engine.ProcessCommand("INVALID"));
        }

        [Fact]
        public void ProcessCommand_NewOrderInvalidPrice_ShouldThrow()
        {
            var engine = new OrderMatchingEngine();

            Assert.Throws<ArgumentException>(() => 
                engine.ProcessCommand("NEW BUY1 BUY -10 10"));
        }

        [Fact]
        public void ProcessCommand_NewOrderInvalidQuantity_ShouldThrow()
        {
            var engine = new OrderMatchingEngine();

            Assert.Throws<ArgumentException>(() => 
                engine.ProcessCommand("NEW BUY1 BUY 100 0"));
        }

        [Fact]
        public void ProcessCommand_SequenceOfCommands()
        {
            var engine = new OrderMatchingEngine();

            // Build sell side
            engine.ProcessCommand("NEW SELL1 SELL 100 5");
            engine.ProcessCommand("NEW SELL2 SELL 101 5");
            engine.ProcessCommand("NEW SELL3 SELL 102 5");

            // Buy should match SELL1 and SELL2
            var result = engine.ProcessCommand("NEW BUY1 BUY 101 10");

            Assert.Equal(2, result.Count);
            Assert.Equal(5, result[0].Quantity);
            Assert.Equal(5, result[1].Quantity);

            var remaining = engine.GetOrderBook().GetBuyOrders();
            Assert.Empty(remaining);
        }
    }
}
