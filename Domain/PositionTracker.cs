namespace WebApplication1.Domain
{
    /// <summary>
    /// Tracks position (net quantity, average price) and calculates PnL.
    /// </summary>
    public class PositionTracker
    {
        private long _netQty;
        private decimal _avgPrice;
        private decimal _realizedPnL;
        private readonly Dictionary<string, Fill> _fillMap = new();

        public long NetQty => _netQty;
        public decimal AvgPrice => _avgPrice;
        public decimal RealizedPnL => _realizedPnL;

        public PositionTracker()
        {
            _netQty = 0;
            _avgPrice = 0;
            _realizedPnL = 0;
        }

        /// <summary>
        /// Process a fill with explicit side (BUY or SELL).
        /// </summary>
        public void ProcessFill(Fill fill, OrderSide side)
        {
            if (_fillMap.ContainsKey($"{fill.BuyOrderId}|{fill.SellOrderId}"))
                throw new InvalidOperationException("Fill already processed");

            long tradeQty = side == OrderSide.BUY ? fill.Quantity : -fill.Quantity;
            decimal tradePrice = fill.Price;

            if (_netQty == 0)
            {
                // Opening a new position
                _netQty = tradeQty;
                _avgPrice = tradePrice;
            }
            else if ((_netQty > 0 && tradeQty > 0) || (_netQty < 0 && tradeQty < 0))
            {
                // Adding to existing position (same direction)
                decimal oldAbsQty = Math.Abs(_netQty);
                decimal newAbsQty = Math.Abs(_netQty + tradeQty);
                _avgPrice = (_avgPrice * oldAbsQty + tradePrice * Math.Abs(tradeQty)) / newAbsQty;
                _netQty += tradeQty;
            }
            else
            {
                // Reducing or reversing position (opposite direction) - realize PnL
                long closedQty = Math.Min(Math.Abs(_netQty), Math.Abs(tradeQty));

                if (_netQty > 0)
                {
                    // Long position, selling
                    _realizedPnL += (tradePrice - _avgPrice) * closedQty;
                }
                else
                {
                    // Short position, buying
                    _realizedPnL += (_avgPrice - tradePrice) * closedQty;
                }

                _netQty += tradeQty;

                // If position reversed, update average price
                if (_netQty > 0)
                {
                    // Still long, but reduced - keep avg price
                    // Already has correct avg price
                }
                else if (_netQty < 0)
                {
                    // Now short, was long - new avg price is trade price
                    _avgPrice = tradePrice;
                }
                else
                {
                    // Flat position
                    _avgPrice = 0;
                }
            }

            _fillMap[$"{fill.BuyOrderId}|{fill.SellOrderId}"] = fill;
        }

        /// <summary>
        /// Process a fill (executed trade).
        /// </summary>
        public void ProcessFill(Fill fill)
        {
            if (_fillMap.ContainsKey($"{fill.BuyOrderId}|{fill.SellOrderId}"))
                throw new InvalidOperationException("Fill already processed");

            long tradeQty = fill.Quantity;
            decimal tradePrice = fill.Price;

            if (_netQty == 0)
            {
                // Opening a new position
                _netQty = tradeQty;
                _avgPrice = tradePrice;
            }
            else if (_netQty > 0)
            {
                // Long position - add more
                decimal oldAbsQty = Math.Abs(_netQty);
                decimal newAbsQty = Math.Abs(_netQty + tradeQty);
                _avgPrice = (_avgPrice * oldAbsQty + tradePrice * tradeQty) / newAbsQty;
                _netQty += tradeQty;
            }
            else
            {
                // Short position - add more
                decimal oldAbsQty = Math.Abs(_netQty);
                decimal newAbsQty = Math.Abs(_netQty + tradeQty);
                _avgPrice = (_avgPrice * oldAbsQty + tradePrice * tradeQty) / newAbsQty;
                _netQty -= tradeQty;
            }

            _fillMap[$"{fill.BuyOrderId}|{fill.SellOrderId}"] = fill;
        }

        /// <summary>
        /// Update current market price for unrealized PnL calculation.
        /// </summary>
        public decimal CalculateUnrealizedPnL(decimal currentPrice)
        {
            if (_netQty == 0)
                return 0;

            return _netQty > 0
                ? (currentPrice - _avgPrice) * _netQty
                : (_avgPrice - currentPrice) * Math.Abs(_netQty);
        }

        /// <summary>
        /// Get total PnL (realized + unrealized).
        /// </summary>
        public decimal CalculateTotalPnL(decimal currentPrice)
        {
            return _realizedPnL + CalculateUnrealizedPnL(currentPrice);
        }

        public void PrintPosition(decimal? currentPrice = null)
        {
            Console.WriteLine("\n=== POSITION ===");
            Console.WriteLine($"Net Qty:        {_netQty}");
            Console.WriteLine($"Avg Price:      {_avgPrice:F2}");
            Console.WriteLine($"Realized PnL:   {_realizedPnL:F2}");

            if (currentPrice.HasValue)
            {
                var unrealizedPnL = CalculateUnrealizedPnL(currentPrice.Value);
                var totalPnL = _realizedPnL + unrealizedPnL;
                Console.WriteLine($"Current Price:  {currentPrice:F2}");
                Console.WriteLine($"Unrealized PnL: {unrealizedPnL:F2}");
                Console.WriteLine($"Total PnL:      {totalPnL:F2}");
            }
            Console.WriteLine("================\n");
        }
    }
}
