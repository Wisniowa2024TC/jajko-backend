# Known issues

Places where the code doesn't match [docs.md](docs.md) or behaves unexpectedly.

## Blocking

- **Login always returns `500`.** `TokenService` calls `GetRolesAsync` ([TokenService.cs:26](Services/TokenService.cs#L26)), but Identity is registered without roles ([Program.cs:19](Program.cs#L19)), so it throws `Store does not implement IUserRoleStore`. Nobody can get a token, so every 🔒 endpoint is unreachable. Fix: add `.AddRoles<IdentityRole<int>>()` or drop the roles loop.
- **Cart routes ignore the `/api/cart` prefix.** The leading `/` in `[Route("/add")]`, `"/remove/{cartItemId}"` and `"/update"` ([CartController.cs:26](Controllers/CartController.cs#L26), [67](Controllers/CartController.cs#L67), [83](Controllers/CartController.cs#L83)) makes them absolute, so they're currently `/add`, `/remove/{id}` and `/update`, and `/api/cart/...` returns `404`. (Orders had the same bug and is already fixed.)
- **No CORS.** A frontend on another origin (e.g. `http://localhost:5173`) is blocked by the browser. Use `AddCors` + `UseCors` or a dev-server proxy.
- **`Jwt:Key` is empty** in `appsettings.json`. It's only set in `appsettings.Development.json`, so outside Development (e.g. in Docker) the app throws at startup unless `Jwt__Key` is set.

## Cart

- `update` and `remove` don't check that the cart entry belongs to the logged-in user, so anyone logged in can change or delete other users' cart entries ([CartController.cs:70](Controllers/CartController.cs#L70), [86](Controllers/CartController.cs#L86)).
- There's no endpoint to read the cart, so the frontend can't show its contents.
- `add` creates a new entry every time instead of increasing the quantity of an existing one ([CartController.cs:56](Controllers/CartController.cs#L56)).
- `quantity` isn't validated in `add` or `update`. It can be 0, negative or more than the stock.
- `CartItemNotFound()` is public, so ASP.NET exposes it as an action at `/api/cart` for every HTTP method ([CartController.cs:63](Controllers/CartController.cs#L63)). Make it `private` or mark it `[NonAction]`.

## Orders

- `GET /api/orders/get` isn't implemented. It always returns an empty `200`, and its query loads the ordered items of **all** users' orders, not only the current user's ([OrdersController.cs:80](Controllers/OrdersController.cs#L80)).
- `GET /api/orders/get/byOrderId/{orderId}` returns `500`. EF Core can't translate `FirstOrDefault(predicate, null)` ([OrdersController.cs:94](Controllers/OrdersController.cs#L94)). Use `FirstOrDefaultAsync(order => order.Id == orderId)`.
- `POST /api/orders/order` doesn't clear the cart, doesn't check or decrease stock, doesn't store the item price and doesn't return the new order's id. Every call creates another order from the same cart.
- Orders aren't linked to users by a foreign key. `ApplicationUser.Orders` makes EF add a shadow `ApplicationUserId` column that is never set, while `Order.UserId` has no FK.

## Items

- `PUT /api/items/{id}` never records price history, because the new price is assigned before it's compared to the old one ([ItemsController.cs:145](Controllers/ItemsController.cs#L145), [149](Controllers/ItemsController.cs#L149)). As a result, `lowest-price` never sees price changes.
- `GET /api/items/{id}/lowest-price` returns `500` when an item has no price-history entry in the last 30 days (`MinAsync` on an empty set, [ItemsController.cs:97](Controllers/ItemsController.cs#L97)). That covers any item whose price hasn't changed for 30 days. It should also take into account the price that was in effect when the 30-day window started.

## Item stocks

- `PUT` and `DELETE` don't work. The route parameter is `{id}`, but the method parameter is `itemId`, so it's never bound and is always `0` ([ItemStocksController.cs:66](Controllers/ItemStocksController.cs#L66), [89](Controllers/ItemStocksController.cs#L89)). As a result, `PUT` returns `400` and `DELETE` returns `404`.
- `POST` for an item that already has a stock, or for an item that doesn't exist, returns `500` instead of `400`/`409`.

## Categories

- Deleting a category that still has subcategories or items returns `500`. The foreign key is `Restrict` and the error isn't handled ([CategoriesController.cs:139](Controllers/CategoriesController.cs#L139)).

## Minor

- `rememberMe` in `LoginRequest` is accepted but ignored ([AuthDtos.cs:14](Models/DTOs/AuthDtos.cs#L14)).
- `ChangePasswordRequest` and `UserDto` exist, but there are no endpoints for changing the password or getting the current user.
- The discount end date is inclusive in `OrdersController` (`now <= EndDate`) but exclusive in Items/Discounts (`EndDate > now`). Also, `d.ItemId == item.ItemId` is checked twice ([OrdersController.cs:54](Controllers/OrdersController.cs#L54)).
- `app.MapControllers()` is called twice ([Program.cs:79](Program.cs#L79), [85](Program.cs#L85)). It's harmless, but one call can go.
- The build warns that `Microsoft.OpenApi` 2.0.0 (pulled in by `Microsoft.AspNetCore.OpenApi`) has a known high-severity vulnerability (NU1903).
