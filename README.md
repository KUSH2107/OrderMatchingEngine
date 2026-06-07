# Order Matching & Position/PnL Engine

A high-performance, in-memory order matching engine with position tracking and P&L calculation. Built with .NET 10, Razor Pages UI, and comprehensive unit tests.

## Features

- **Order Matching Engine** - In-memory order book with price-time priority matching
- **Position & PnL Tracking** - Real-time position management and profit/loss calculation
- **Web UI** - Interactive Razor Pages interface for all operations
- **REST API** - Complete REST API for programmatic access
- **Comprehensive Tests** - 43 unit tests with full coverage

## Quick Start

### Running the Application

```bash
cd "C:\Users\HP\Desktop\DotNet Projects\WebApplication1"
dotnet run
```

Navigate to:
- **Order Book UI**: https://localhost:7000 (or http://localhost:5000)
- **Analytics Dashboard**: https://localhost:7000/analytics

### Running Tests

```bash
dotnet test
```

All 43 tests should pass:
- 16 OrderBook tests
- 14 PositionTracker tests
- 13 OrderMatchingEngine tests

## Overview

This project implements two core trading systems:

1. **Task 1: Order Matching Engine** - In-memory order book with price-time priority matching
2. **Task 2: Position & PnL Engine** - Position tracking and profit/loss calculation

## Architecture

### Core Components

#### 1. Order Matching Engine (`Domain/OrderBook.cs`)

**Features:**
- In-memory order storage with efficient matching
- Price-time priority (FIFO within same price level)
- Automatic order matching on insertion
- Support for partial fills
- Buy orders sorted by price descending, then timestamp ascending
- Sell orders sorted by price ascending, then timestamp ascending

**Order Matching Rules:**
- **BUY matches SELL:** BUY order matches lowest-priced SELL orders at SELL's price
- **SELL matches BUY:** SELL order matches highest-priced BUY orders at BUY's price
- Price-time priority ensures fair queue management
- Partial fills allow orders to be filled across multiple counterparties

**Data Structures:**
```csharp
// Order representation
public class Order
{
	string OrderId        // Unique identifier
	OrderSide Side        // BUY or SELL
	decimal Price         // Limit price
	long Quantity         // Total quantity
	long FilledQuantity   // Filled quantity
	DateTime Timestamp    // Creation time for priority
}

// Fill representation
public class Fill
{
	string BuyOrderId     // Matching buy order
	string SellOrderId    // Matching sell order
	decimal Price         // Execution price
	long Quantity         // Filled quantity
	DateTime FillTime     // When fill occurred
}
```

#### 2. Position Tracker (`Domain/PositionTracker.cs`)

**Features:**
- Track net quantity and average price
- Calculate realized P&L (from closed positions)
- Calculate unrealized P&L (from open positions)
- Support for long and short positions

**Formulas:**

**Adding to Position:**
```
NewAvgPrice = (OldAvgPrice * OldQty + TradePrice * TradeQty) / (OldQty + TradeQty)
```

**Closing Position (Realizing P&L):**
```
// For long positions (NetQty > 0):
RealizedPnL += (SalePrice - AvgPrice) * ClosedQty

// For short positions (NetQty < 0):
RealizedPnL += (AvgPrice - BuyPrice) * ClosedQty
```

**Unrealized P&L:**
```
// For long positions:
UnrealizedPnL = (CurrentPrice - AvgPrice) * NetQty

// For short positions:
UnrealizedPnL = (AvgPrice - CurrentPrice) * |NetQty|
```

#### 3. Command Engine (`Services/OrderMatchingEngine.cs`)

Command processor supporting:

**NEW** - Add a new order
```
NEW <OrderId> <BUY|SELL> <Price> <Quantity>
Example: NEW ORD001 BUY 100.50 1000
```

**CANCEL** - Cancel an existing order
```
CANCEL <OrderId>
Example: CANCEL ORD001
```

**MODIFY** - Modify an order's price and quantity
```
MODIFY <OrderId> <NewPrice> <NewQuantity>
Example: MODIFY ORD001 101.00 500
```

**PRINT** - Display order book or specific order
```
PRINT              # Print entire order book
PRINT <OrderId>    # Print specific order
```

## Web User Interface

### Main Order Book Page (/)

![Order Book Interface](README_files/orderbook.png)

**Left Column - Order Management:**
- **Create New Order** - Add BUY/SELL orders to the book
- **Order Actions** - Cancel existing orders
- **Modify Order** - Update price and quantity

**Right Column - Order Display:**
- **Buy Orders Tab** - All BUY orders sorted by priority
- **Sell Orders Tab** - All SELL orders sorted by priority
- **Fills Tab** - All executed trades with fill prices

**Features:**
- Real-time order book updates (every 3 seconds)
- Color-coded order display (green=BUY, red=SELL)
- Automatic fill notifications
- Load indicators during operations

### Analytics Dashboard (/analytics)

![Analytics Dashboard](README_files/analytics.png)

**Statistics Display:**
- **Net Position** - Current position quantity
- **Avg Price** - Average entry price
- **Realized PnL** - Profits/losses from closed positions
- **Unrealized PnL** - Current P&L from open positions
- **Total PnL** - Combined realized + unrealized P&L
- **Return %** - Percentage return on position

**Position History:**
- Complete fill history table
- Trade value calculations
- Timestamps for each trade

**Charts:**
- Cumulative trade value graph
- Real-time P&L tracking

## REST API

### Endpoints

#### Create Order
```http
POST /api/orders/new
Content-Type: application/json

{
  "orderId": "ORD001",
  "side": "BUY",
  "price": 100.50,
  "quantity": 1000
}

Response:
{
  "success": true,
  "fills": [
	{
	  "buyOrderId": "BUY1",
	  "sellOrderId": "SELL1",
	  "price": 100.50,
	  "quantity": 500
	}
  ]
}
```

#### Cancel Order
```http
POST /api/orders/cancel
Content-Type: application/json

{
  "orderId": "ORD001"
}

Response:
{
  "success": true
}
```

#### Modify Order
```http
POST /api/orders/modify
Content-Type: application/json

{
  "orderId": "ORD001",
  "newPrice": 101.00,
  "newQuantity": 500
}

Response:
{
  "success": true,
  "fills": [...]
}
```

#### Get Order Book
```http
GET /api/orders/book

Response:
{
  "buyOrders": [
	{
	  "orderId": "BUY1",
	  "side": "BUY",
	  "price": 100.50,
	  "quantity": 1000,
	  "filledQuantity": 500,
	  "remainingQuantity": 500,
	  "timestamp": "2026-06-07T10:30:00Z"
	}
  ],
  "sellOrders": [...],
  "fills": [...]
}
```

#### Get Specific Order
```http
GET /api/orders/order/{orderId}

Response:
{
  "orderId": "BUY1",
  "side": "BUY",
  "price": 100.50,
  "quantity": 1000,
  "filledQuantity": 500,
  "remainingQuantity": 500,
  "timestamp": "2026-06-07T10:30:00Z"
}
```

## Usage Examples

### Web UI Workflow

1. **Create Orders:**
   - Navigate to Order Book page
   - Enter Order ID, select BUY/SELL, enter Price and Quantity
   - Click "Create Order"
   - Watch for automatic fills

2. **Monitor Position:**
   - Go to Analytics page
   - View current Net Position and Avg Price
   - Enter Market Price to calculate P&L

3. **Modify Orders:**
   - Use "Modify Order" panel
   - Enter new price and quantity
   - View any resulting fills

### Programmatic Access

```csharp
using WebApplication1.Services;
using WebApplication1.Domain;

var engine = new OrderMatchingEngine();

// Create orders
var fills = engine.ProcessCommand("NEW SELL1 SELL 100.00 10");
var fills = engine.ProcessCommand("NEW BUY1 BUY 100.00 15");
// BUY1 partially filled: 10 units

// Modify order
var fills = engine.ProcessCommand("MODIFY BUY1 100 20");

// Get order book
var book = engine.GetOrderBook();
var buyOrders = book.GetBuyOrders();
var sellOrders = book.GetSellOrders();
var allFills = book.Fills;
```

### Position Tracking

```csharp
var tracker = new PositionTracker();

// Open long position
var fill1 = new Fill("BUY1", "SELL1", 100m, 10);
tracker.ProcessFill(fill1, OrderSide.BUY);

// Add to position
var fill2 = new Fill("BUY2", "SELL2", 110m, 10);
tracker.ProcessFill(fill2, OrderSide.BUY);

// Average price is now 105
Assert.Equal(105m, tracker.AvgPrice);

// Partially close position
var fill3 = new Fill("BUY3", "SELL3", 115m, 5);
tracker.ProcessFill(fill3, OrderSide.SELL);

// Realized P&L: (115 - 105) * 5 = 50
Assert.Equal(50m, tracker.RealizedPnL);

// Unrealized P&L at price 120: (120 - 105) * 5 = 75
Assert.Equal(75m, tracker.CalculateUnrealizedPnL(120m));
```

## Implementation Details

### Price-Time Priority in OrderBook

**Buy Orders (Descending Price, Ascending Time):**
```
Sorted as: 105 (t1), 105 (t2), 100 (t1), 100 (t2)
		   ^^^^^^ highest priority
```

**Sell Orders (Ascending Price, Ascending Time):**
```
Sorted as: 95 (t1), 95 (t2), 100 (t1), 100 (t2)
		   ^^^^^ highest priority (best price)
```

### Matching Algorithm

**For BUY orders:**
1. Get lowest-priced SELL from queue
2. If BUY price >= SELL price: execute partial or full match
3. Repeat until BUY is filled or no more matching SELLs

**For SELL orders:**
1. Get highest-priced BUY from queue
2. If SELL price <= BUY price: execute partial or full match
3. Repeat until SELL is filled or no more matching BUYs

### Order Cancellation and Modification

- **Cancel:** Remove order from orderBook and both sides' priority queues
- **Modify:** Cancel old order (preserving timestamp for priority), create new order with updated parameters and re-add

## Tests

Comprehensive unit test coverage includes:

### OrderBook Tests (16 tests)
- ✓ Add orders (buy/sell)
- ✓ Order matching (full/partial fills)
- ✓ Price-time priority enforcement
- ✓ Order cancellation
- ✓ Order modification
- ✓ Multiple matches
- ✓ Duplicate order detection

### PositionTracker Tests (14 tests)
- ✓ Opening positions (long/short)
- ✓ Adding to positions
- ✓ Closing positions with P&L realization
- ✓ Position reversal
- ✓ Unrealized P&L calculation
- ✓ Total P&L calculation
- ✓ Duplicate fill detection

### Command Engine Tests (13 tests)
- ✓ Command parsing
- ✓ NEW command
- ✓ CANCEL command
- ✓ MODIFY command
- ✓ PRINT command
- ✓ Error handling

**Run tests:**
```bash
dotnet test
# All 43 tests pass
```

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| Add Order | O(log n) | SortedSet insertion + matching |
| Cancel Order | O(log n) | SortedSet removal |
| Modify Order | O(log n) | Cancel + Add |
| Match Single Order | O(m log n) | m = fills, n = total orders |

Where:
- **n** = number of orders in book
- **m** = number of fills generated

## Optional: Concurrency (Producer-Consumer Model)

The current implementation is thread-safe at the OrderBook level through:
- Atomic SortedSet operations
- Immutable order state during matching

For producer-consumer model with multiple threads:

```csharp
// Example concurrent usage
var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
var orderBook = new OrderBook();

// Producer thread
Task.Run(async () => {
	foreach (var command in commands)
		await channel.Writer.WriteAsync(command);
	channel.Writer.TryComplete();
});

// Consumer thread
await foreach (var command in channel.Reader.ReadAllAsync())
{
	var fills = engine.ProcessCommand(command);
	// Process fills...
}
```

## Technology Stack

- **.NET 10** - Latest framework
- **ASP.NET Core** - Web framework
- **Razor Pages** - UI framework
- **xUnit** - Unit testing framework
- **Bootstrap 5** - UI styling
- **Chart.js** - Real-time charting
- **C# 13** - Language features

## Project Structure

```
WebApplication1/
├── Controllers/
│   └── OrdersController.cs      # REST API endpoints
├── Domain/
│   ├── Order.cs                 # Order model
│   ├── OrderSide.cs             # BUY/SELL enum
│   ├── Fill.cs                  # Fill/trade result
│   ├── OrderBook.cs             # Main matching engine
│   └── PositionTracker.cs       # PnL tracking
├── Pages/
│   ├── Index.cshtml             # Order book UI
│   ├── Index.cshtml.cs          # Order book code-behind
│   ├── Analytics.cshtml         # Analytics dashboard
│   └── Analytics.cshtml.cs      # Analytics code-behind
├── Services/
│   └── OrderMatchingEngine.cs   # Command processor
├── Tests/
│   ├── OrderBookTests.cs        # Order book tests
│   ├── PositionTrackerTests.cs  # Position tests
│   └── OrderMatchingEngineTests.cs # Command tests
├── Program.cs                   # Application startup
└── README.md                    # This file
```

## Key Design Decisions

1. **SortedSet for Order Storage**: Provides O(log n) insertion and automatic sorting by price-time priority
2. **Immutable Timestamp**: Order timestamps are set at creation and preserved during modifications to maintain queue fairness
3. **Fill-based Processing**: Orders are matched immediately upon insertion; fills are generated and returned
4. **Separate Position Tracking**: Position logic is decoupled from matching engine for flexibility
5. **Web UI**: Interactive Razor Pages interface for ease of use
6. **REST API**: Programmatic access to all functionality

## Error Handling

- Invalid prices (≤ 0): Throws `ArgumentException`
- Invalid quantities (≤ 0): Throws `ArgumentException`
- Duplicate order IDs: Throws `InvalidOperationException`
- Non-existent orders: Returns false for cancellation, throws for retrieval
- Invalid commands: Throws `ArgumentException` with helpful message

## Future Enhancements

1. **Persistence**: Add event sourcing or snapshot-based persistence
2. **Multi-asset support**: Track multiple symbols/products
3. **Order types**: Support stop-loss, limit orders with time conditions
4. **Market data**: Track bid-ask spread, VWAP, market depth
5. **Concurrency**: Thread-safe concurrent order processing
6. **Performance metrics**: Latency tracking, throughput measurement
7. **Websocket support**: Real-time updates without polling
8. **Database persistence**: Store orders and trades in database

## References

- **Order Matching Engine Pattern**: Standard in financial exchanges
- **Price-Time Priority**: Fair queue discipline used by most exchanges (NASDAQ, NYSE, EUREX)
- **P&L Calculation**: Standard accounting in trading systems (FIFO, LIFO, Average Cost methods)

## License

MIT

## Author

GitHub Copilot - AI Programming Assistant

