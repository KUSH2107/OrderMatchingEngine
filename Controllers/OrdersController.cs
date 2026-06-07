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

        [HttpGet("order/{orderId}")]
        public IActionResult GetOrder(string orderId)
        {
            try
            {
                var order = _engine.GetOrderBook().GetOrder(orderId);
                if (order == null)
                    return NotFound(new { error = "Order not found" });

                return Ok(new { order.OrderId, order.Side, order.Price, order.Quantity, order.FilledQuantity, order.RemainingQuantity, order.Timestamp });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
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
