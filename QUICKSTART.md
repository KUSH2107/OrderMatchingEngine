# Quick Start Guide - Order Matching Engine UI

## Running the Application

```bash
cd "C:\Users\HP\Desktop\DotNet Projects\WebApplication1"
dotnet run
```

The application will start and be available at:
- **Main UI**: https://localhost:7000 (or http://localhost:5000)
- **Analytics**: https://localhost:7000/analytics

## Main Dashboard (Order Book)

### Create New Orders

1. **Left Panel - Create New Order**
   - Enter **Order ID** (e.g., "BUY_001")
   - Select **Side** (BUY or SELL)
   - Enter **Price** (e.g., 100.50)
   - Enter **Quantity** (e.g., 1000)
   - Click **Create Order** button

2. **What happens:**
   - Order is added to the order book
   - Automatic matching occurs if there's a counterparty
   - Success notification shows number of fills generated
   - Order book updates automatically (every 3 seconds)

### View Order Book

**Right Panel - Order Book Display:**

- **BUY ORDERS Tab**
  - Shows all active BUY orders
  - Sorted by price (highest first), then time
  - Displays: Order ID, Price, Qty, Filled, Remaining, Time

- **SELL ORDERS Tab**
  - Shows all active SELL orders
  - Sorted by price (lowest first), then time
  - Displays: Order ID, Price, Qty, Filled, Remaining, Time

- **FILLS Tab**
  - Shows all executed trades
  - Displays: Buy Order, Sell Order, Price, Quantity, Time
  - Each fill links the matching orders

### Example Trading Scenario

**Step 1: Add Sell Orders**
```
Order ID: SELL_001
Side: SELL
Price: 100.00
Qty: 100
Click Create Order

Order ID: SELL_002
Side: SELL
Price: 100.50
Qty: 150
Click Create Order
```

**Step 2: Add Matching Buy Order**
```
Order ID: BUY_001
Side: BUY
Price: 100.50
Qty: 200
Click Create Order
```

**Result:**
- SELL_001: 100 shares matched @ 100.00
- SELL_002: 100 shares matched @ 100.50
- BUY_001: Partially filled (200/200), fully executed
- Notification: "Order created! 2 fills generated"
- View fills in FILLS tab to see both trades

### Modify Orders

**Modify Order Panel:**
1. Enter **Order ID** of existing order
2. Enter **New Price**
3. Enter **New Quantity**
4. Click **Modify Order**

**Important:** Modifying cancels the old order and creates a new one with updated parameters. The new order will be re-matched against current book.

### Cancel Orders

**Order Actions Panel:**
1. Enter **Order ID** to cancel
2. Click **Cancel Order** button

**Result:**
- Order is removed from the book
- No PnL impact (order wasn't filled)
- Order book updates

## Analytics Dashboard (/analytics)

### Position Stats

**Top Stats Row:**
- **Net Position** - Current net quantity (positive = long, negative = short)
- **Avg Price** - Average entry price across all fills
- **Realized PnL** - Profit/loss from closed positions (in currency)
- **Market Price Input** - Enter current market price to calculate P&L

### Calculating P&L

1. **Enter Market Price** in the "Market Price" stat box
2. **Click Calculate** button
3. **Unrealized PnL** updates - showing current P&L if you close now
4. **Total PnL** updates - Realized + Unrealized
5. **Return %** shows your percentage return

**Example:**
- Net Position: +10 shares
- Avg Price: $100.00
- Market Price: $110.00
- **Unrealized PnL**: $100.00 (profit from price increase)
- Previous trades closed: $50.00
- **Total PnL**: $150.00
- **Return %**: 15%

### Position History Table

Shows all fills in chronological order:
- Buy/Sell Order IDs
- Fill Price
- Quantity
- Trade Value (Price × Qty)
- Timestamp

### PnL Chart

Real-time line chart showing cumulative trade value over time as you execute more trades.

## API Usage (Programmatic)

### Create Order via API

```bash
curl -X POST https://localhost:7000/api/orders/new \
  -H "Content-Type: application/json" \
  -d '{
	"orderId": "API_BUY_001",
	"side": "BUY",
	"price": 100.50,
	"quantity": 1000
  }'
```

### Get Order Book via API

```bash
curl https://localhost:7000/api/orders/book
```

### Cancel Order via API

```bash
curl -X POST https://localhost:7000/api/orders/cancel \
  -H "Content-Type: application/json" \
  -d '{
	"orderId": "API_BUY_001"
  }'
```

### Modify Order via API

```bash
curl -X POST https://localhost:7000/api/orders/modify \
  -H "Content-Type: application/json" \
  -d '{
	"orderId": "API_BUY_001",
	"newPrice": 101.00,
	"newQuantity": 500
  }'
```

## Command Line Interface (CLI)

You can also use commands directly in code:

```csharp
using WebApplication1.Services;

var engine = new OrderMatchingEngine();

// Create orders
engine.ProcessCommand("NEW BUY_001 BUY 100.00 50");
engine.ProcessCommand("NEW SELL_001 SELL 99.00 100");

// Modify orders
engine.ProcessCommand("MODIFY BUY_001 101.00 75");

// Cancel orders
engine.ProcessCommand("CANCEL SELL_001");

// View order book
engine.ProcessCommand("PRINT");

// View specific order
engine.ProcessCommand("PRINT BUY_001");
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "OrderBookTests"
dotnet test --filter "PositionTrackerTests"
dotnet test --filter "OrderMatchingEngineTests"

# Run specific test method
dotnet test --filter "AddOrder_NewBuyOrder_ShouldAddToBook"
```

**Expected Result:** All 43 tests pass

## Troubleshooting

### "Order not found" Error
- Make sure you're using the exact Order ID you created
- Check that the order hasn't been cancelled
- Order IDs are case-sensitive

### "Order already exists" Error
- You're trying to create an order with an ID that already exists
- Use a unique Order ID for each new order

### No fills generated
- BUY price must be >= SELL price for a match
- Both orders must have the same price for matching
- Price-time priority means older orders match first at that price level

### Position doesn't update
- Position tracking is separate from order book
- You need to manually process fills in the Analytics page
- Enter market price and click Calculate to see P&L

## Tips & Tricks

1. **Real-time Updates**: The order book automatically updates every 3 seconds
2. **Order Priorities**: BUY orders sorted by highest price first, SELL by lowest price
3. **Partial Fills**: Orders can be filled across multiple counterparties
4. **Price-Time Priority**: At the same price level, earlier orders fill first
5. **Market Price**: Use realistic prices for testing (e.g., 100.00, not 1 million)

## Example Trading Scenario

Try this step-by-step:

```
1. Create: BUY_001 BUY 100.00 100
2. Create: BUY_002 BUY 99.50 50
3. Create: SELL_001 SELL 98.00 75
   -> Result: SELL_001 matches BUY_002 (50 shares @ 99.50)
   -> Remaining: BUY_001, BUY_002 (partially), SELL_001 (partially)

4. Create: SELL_002 SELL 100.00 200
   -> Result: SELL_002 matches BUY_001 (100 @ 100.00)
   ->         SELL_002 matches SELL_001 remaining (75 @ 100.00 - ERROR: wrong type)
   -> Actually: SELL_002 matches BUY_001 (100 @ 100.00)
   ->           SELL_002 remains 100

5. Go to Analytics
6. Enter Market Price: 101.00
7. Click Calculate -> See P&L
```

## Support & Documentation

- **Main README**: Full technical documentation
- **Code Comments**: Extensive inline documentation
- **Test Cases**: Examples of all functionality in test files
- **API Docs**: REST endpoints fully documented

Enjoy using the Order Matching Engine!
