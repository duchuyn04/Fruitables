# Plan E2E Playwright Cho SignalR

## Summary

Tạo Playwright test suite chính thức để kiểm tra SignalR chạy thật trong browser: guest nhận stock realtime, customer/admin join đúng order group, không còn duplicate `start()`, và order event không leak sang user khác. Test dùng endpoint fixture/test-only để phát event ổn định, không phụ thuộc thao tác checkout dài và dữ liệu DB ngẫu nhiên.

## Key Changes

- Thêm Playwright infra:
  - `package.json`: thêm script `e2e:signalr`.
  - `playwright.config.ts`: chạy Chromium, baseURL `http://127.0.0.1:5270`, webServer chạy `dotnet run --no-build --urls http://127.0.0.1:5270`.
  - Output artifacts: `test-results/`, trace on first retry.

- Thêm E2E-only backend hooks:
  - Tạo controller test-only, ví dụ `Controllers/E2eSignalRController.cs`.
  - Chỉ enable khi `ASPNETCORE_ENVIRONMENT == "Development"` và config `Features:E2eEndpoints == true`.
  - Endpoints:
    - `POST /__e2e/signalr/fixture`: tạo/ensure admin, customer, product, order thuộc customer; trả `adminEmail`, `customerEmail`, password test, `productId`, `orderId`.
    - `POST /__e2e/signalr/emit-stock`: gọi `IRealtimeNotifier.NotifyStockChangedAsync(productId, stock)`.
    - `POST /__e2e/signalr/emit-order-status`: gọi `NotifyOrderUpdatedAsync(orderId, userId, status)`.
    - `POST /__e2e/signalr/emit-payment-status`: gọi `NotifyPaymentStatusChangedAsync(orderId, userId, status)`.
  - Không expose endpoint này trong Production.

- Thêm test helpers:
  - `tests/e2e/helpers/auth.ts`: login bằng UI hoặc request context rồi giữ cookie.
  - `tests/e2e/helpers/console.ts`: collect console errors, fail nếu có lỗi SignalR duplicate start / undefined hub.
  - `tests/e2e/helpers/fixture.ts`: gọi endpoint fixture và emit event.

## E2E Scenarios

- `guest product detail receives stock update`
  - Gọi fixture lấy `productId`.
  - Guest mở `/Shop/Detail/{productId}`.
  - Assert console không có lỗi hub.
  - Gọi `emit-stock(productId, newStock)`.
  - Assert page reload hoặc stock UI thay đổi theo behavior hiện tại.

- `cart page joins product groups without duplicate start`
  - Fixture tạo product.
  - Login customer, thêm product vào cart hoặc seed cart qua fixture nếu cần.
  - Mở `/Cart`.
  - Assert không có lỗi `Cannot start a HubConnection that is not in the 'Disconnected' state`.
  - Gọi `emit-stock`.
  - Assert toast/reload behavior xuất hiện.

- `customer order detail receives own order status`
  - Fixture tạo order thuộc customer.
  - Login customer.
  - Mở `/OrderHistory/Details/{orderId}`.
  - Gọi `emit-order-status(orderId, "Processing")`.
  - Assert page reload hoặc status badge đổi.

- `anonymous cannot join order group`
  - Guest mở một test page hoặc dùng browser evaluate tạo hub connection.
  - Invoke `JoinOrderGroup(orderId)`.
  - Assert bị reject `HubException`, không có event order được nhận.

- `admin order detail receives order events`
  - Login admin.
  - Mở `/Admin/Order/Detail/{orderId}`.
  - Assert standalone page có `window.ecommerceHubReady`.
  - Gọi `emit-payment-status`.
  - Assert page reload hoặc payment badge đổi.

## Test Commands

- Install once:
  - `npm install`
  - `npx playwright install chromium`
- Run:
  - `npm run e2e:signalr`
- Existing checks still run:
  - `dotnet build Fruitables.csproj --no-restore`
  - `dotnet test Tests\Fruitables.Tests.csproj --no-restore`

## Done When

- Playwright test suite chạy được bằng một command.
- Guest stock realtime pass.
- Customer/admin order realtime pass.
- Anonymous không join được order group.
- Không còn console error do duplicate SignalR `start()` hoặc missing `ecommerceHubReady`.
- E2E-only endpoints bị chặn nếu không bật `Features:E2eEndpoints=true`.
