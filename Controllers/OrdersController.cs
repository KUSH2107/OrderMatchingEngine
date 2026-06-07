using Microsoft.AspNetCore.Mvc;
using WebApplication1.Domain;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderMatchingEngine _engine;

        public OrdersController(OrderMatchingEngine engine)
        {
            _engine = engine;
        }

        [HttpPost("new")]
        public IActionResult NewOrder([FromBody] NewOrderRequest request)
        {
            try
            {
                var fills = _engine.ProcessCommand($"NEW {request.OrderId} {request.Side} {request.Price} {request.Quantity}");
                return Ok(new { success = true, fills = fills.Select(f => new { f.BuyOrderId, f.SellOrderId, f.Price, f.Quantity }) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("cancel")]
        public IActionResult CancelOrder([FromBody] CancelOrderRequest request)
        {
            try
            {
                _engine.ProcessCommand($"CANCEL {request.OrderId}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("modify")]
        public IActionResult ModifyOrder([FromBody] ModifyOrderRequest request)
        {
            try
            {
                var fills = _engine.ProcessCommand($"MODIFY {request.OrderId} {request.NewPrice} {request.NewQuantity}");
                return Ok(new { success = true, fills = fills.Select(f => new { f.BuyOrderId, f.SellOrderId, f.Price, f.Quantity }) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("book")]
        public IActionResult GetOrderBook()
        {
            try
            {
                var book = _engine.GetOrderBook();
                var buyOrders = book.GetBuyOrders().Select(o => new { o.OrderId, o.Side, o.Price, o.Quantity, o.FilledQuantity, o.RemainingQuantity, o.Timestamp }).ToList();
                var sellOrders = book.GetSellOrders().Select(o => new { o.OrderId, o.Side, o.Price, o.Quantity, o.FilledQuantity, o.RemainingQuantity, o.Timestamp }).ToList();
                var fills = book.Fills.Select(f => new { f.BuyOrderId, f.SellOrderId, f.Price, f.Quantity, f.FillTime }).ToList();

                return Ok(new { buyOrders, sellOrders, fills });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("position")]
        public IActionResult GetPosition()
        {
            try
            {
                var tracker = _engine.GetPositionTracker();
                var currentPrice = _engine.GetCurrentMarketPrice();
                var unrealizedPnL = tracker.CalculateUnrealizedPnL(currentPrice);
                var totalPnL = tracker.CalculateTotalPnL(currentPrice);

                return Ok(new
                {
                    netQty = tracker.NetQty,
                    avgPrice = tracker.AvgPrice,
                    realizedPnL = tracker.RealizedPnL,
                    unrealizedPnL = unrealizedPnL,
                    totalPnL = totalPnL,
                    currentPrice = currentPrice
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("fill")]
        public IActionResult CreateFill([FromBody] CreateFillRequest request)
        {
            try
            {
                var fills = _engine.ProcessCommand($"FILL {request.BuyOrderId} {request.SellOrderId} {request.Price} {request.Quantity}");
                return Ok(new { success = true, fills = fills.Select(f => new { f.BuyOrderId, f.SellOrderId, f.Price, f.Quantity }) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("price")]
        public IActionResult SetMarketPrice([FromBody] SetPriceRequest request)
        {
            try
            {
                _engine.ProcessCommand($"PRICE {request.CurrentPrice}");
                return Ok(new { success = true, message = $"Market price set to {request.CurrentPrice}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("position/print")]
        public IActionResult PrintPosition()
        {
            try
            {
                var tracker = _engine.GetPositionTracker();
                var currentPrice = _engine.GetCurrentMarketPrice();
                var unrealizedPnL = tracker.CalculateUnrealizedPnL(currentPrice);
                var totalPnL = tracker.CalculateTotalPnL(currentPrice);

                return Ok(new
                {
                    netQty = tracker.NetQty,
                    avgPrice = tracker.AvgPrice,
                    realizedPnL = tracker.RealizedPnL,
                    unrealizedPnL = unrealizedPnL,
                    totalPnL = totalPnL,
                    currentPrice = currentPrice,
                    position = tracker.NetQty == 0 ? "FLAT" : (tracker.NetQty > 0 ? "LONG" : "SHORT")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }

    public class CreateFillRequest
    {
        public string BuyOrderId { get; set; }
        public string SellOrderId { get; set; }
        public decimal Price { get; set; }
        public long Quantity { get; set; }
    }

    public class SetPriceRequest
    {
        public decimal CurrentPrice { get; set; }
    }

    public class NewOrderRequest
    {
        public string OrderId { get; set; }
        public string Side { get; set; }
        public decimal Price { get; set; }
        public long Quantity { get; set; }
    }

    public class CancelOrderRequest
    {
        public string OrderId { get; set; }
    }

    public class ModifyOrderRequest
    {
        public string OrderId { get; set; }
        public decimal NewPrice { get; set; }
        public long NewQuantity { get; set; }
    }
}
