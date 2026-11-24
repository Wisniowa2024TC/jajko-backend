# Known issues

Places where the code doesn't match [docs.md](docs.md) or behaves unexpectedly.

## Blocking

- **`Jwt:Key` is empty** in `appsettings.json`. It's only set in `appsettings.Development.json`, so outside Development (e.g. in Docker) the app throws at startup unless `Jwt__Key` is set.

## Cart

- `update` and `remove` return `401` for someone else's cart entry ([CartController.cs:89](Controllers/CartController.cs#L89), [115](Controllers/CartController.cs#L115)). `401` means "not logged in", so a frontend that logs the user out on `401` would do that here. Return `404` (as for a missing entry) or `403` instead.
- There's no endpoint to read the cart, so the frontend can't show its contents.
- `quantity` isn't validated in `add` or `update`. It can be 0, negative or more than the stock (including after `add` increases an existing entry).
- `CartItemNotFound()` is public, so ASP.NET exposes it as an action at `/api/cart` for every HTTP method ([CartController.cs:71](Controllers/CartController.cs#L71)). Make it `private` or mark it `[NonAction]`.

## Orders

- `GET /api/orders/get/byOrderId/{orderId}` has no `[Authorize]` and no owner check ([OrdersController.cs:94](Controllers/OrdersController.cs#L94)), so anyone, even without logging in, can read any order by guessing its id.
- `POST /api/orders/order` doesn't store the price paid, so the order total can't be calculated once the item's price changes.
- Orders aren't linked to users by a foreign key. `ApplicationUser.Orders` makes EF add a shadow `ApplicationUserId` column that is never set, while `Order.UserId` has no FK.

## Items

- `ReferencePrice` is in, but the old discount system is still there: `DiscountsController`, `GET /api/items/{id}/discount`, and `POST /api/orders/order` still looks up a `Discount` and stores it in `OrderedItem.Discount` ([OrdersController.cs:53](Controllers/OrdersController.cs#L53)).
- **`POST /api/items` always returns `500`**, even though the item is saved. `PriceHistory` has an `Item` navigation and is also configured with `HasOne<Item>()` without one ([AppDbContext.cs:101](AppDbContext.cs#L101)). As a result, EF created a second required foreign key, `ItemId1`, that is never set ([AppDbContextModelSnapshot.cs:453](Migrations/AppDbContextModelSnapshot.cs#L453)), so saving the price-history row fails. Fix: `HasOne(p => p.Item)` plus a new migration.
- `PUT /api/items/{id}` never records price history, because the new price is assigned before it's compared to the old one ([ItemsController.cs:145](Controllers/ItemsController.cs#L145), [150](Controllers/ItemsController.cs#L150)). As a result, `lowest-price` never sees price changes.
- `GET /api/items/{id}/lowest-price` returns `500` when an item has no price-history entry in the last 30 days (`MinAsync` on an empty set, [ItemsController.cs:97](Controllers/ItemsController.cs#L97)). That covers any item whose price hasn't changed for 30 days. It should also take into account the price that was in effect when the 30-day window started.

## Item stocks

- `PUT` and `DELETE` don't work. The route parameter is `{id}`, but the method parameter is `itemId`, so it's never bound and is always `0` ([ItemStocksController.cs:66](Controllers/ItemStocksController.cs#L66), [89](Controllers/ItemStocksController.cs#L89)). As a result, `PUT` returns `400` and `DELETE` returns `404`.
- `POST` for an item that already has a stock, or for an item that doesn't exist, returns `500` instead of `400`/`409`.

## Categories

- Deleting a category that still has subcategories or items returns `500`. The foreign key is `Restrict` and the error isn't handled ([CategoriesController.cs:139](Controllers/CategoriesController.cs#L139)).

## Docker

- **The container crashes on startup** with `SQLite Error 14: 'unable to open database file'`. The database path is `./data/jajko.db`, but `data/` isn't part of the publish output, so it doesn't exist in `/app`. The app also runs as `$APP_UID`, which can't create it there. Fix: create `/app/data` owned by `$APP_UID` in the Dockerfile and mount a volume on it, otherwise the database is lost with the container.
- `Jwt__Key` has to be passed as an environment variable (see Blocking).

## Minor

- `rememberMe` in `LoginRequest` is accepted but ignored ([AuthDtos.cs:14](Models/DTOs/AuthDtos.cs#L14)).
- `ChangePasswordRequest` and `UserDto` exist, but there are no endpoints for changing the password or getting the current user.
- The discount end date is inclusive in `OrdersController` (`now <= EndDate`) but exclusive in Items/Discounts (`EndDate > now`). Also, `d.ItemId == item.ItemId` is checked twice ([OrdersController.cs:54](Controllers/OrdersController.cs#L54)).
- `app.MapControllers()` is called twice ([Program.cs:79](Program.cs#L79), [85](Program.cs#L85)). It's harmless, but one call can go.
- The build warns that `Microsoft.OpenApi` 2.0.0 (pulled in by `Microsoft.AspNetCore.OpenApi`) has a known high-severity vulnerability (NU1903).
