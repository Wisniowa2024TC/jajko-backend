# Jajko API

Short API reference for the frontend. Some endpoints don't work like this yet, see [issues.md](issues.md).

- Bodies are JSON with camelCase keys. Dates are ISO strings (`"2026-10-28T12:00:00"`), prices are numbers (`12.99`).
- 🔒 means the endpoint needs the header `Authorization: Bearer <accessToken>` (from login). Without a valid token you get `401`.
- Not found returns `404`. An invalid body returns `400` with `{ errors: { [field]: string[] } }`.
- For `PUT .../{id}` the `id` in the body must match the one in the URL, otherwise `400`.

## Auth

```ts
type LoginResponse = {
  accessToken: string
  expiresAt: string     // the token is valid for 60 minutes
  tokenType: "Bearer"
}
```

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/auth/register` | `{ email, password, name, surname }` | `204` |
| POST | `/api/auth/login` | `{ email, password }` | `LoginResponse`, wrong credentials: `401` |

Password: at least 8 characters, with an uppercase letter, a lowercase letter, a digit and a symbol. Email must be unique, `name` and `surname` max 64 characters.

## Categories

```ts
type Category = {
  id: number
  parentCategory: number | null   // id of the parent, null = top level
  name: string                    // max 64 characters
}
```

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/categories` | | `Category[]` |
| GET | `/api/categories/{id}` | | `Category` |
| GET | `/api/categories/{id}/children` | | `Category[]`, all subcategories (nested ones too) as a flat list |
| POST | `/api/categories` | `Category` without `id` | `201` `Category` |
| PUT | `/api/categories/{id}` | `Category` | `204` |
| DELETE | `/api/categories/{id}` | | `204` |

## Items

```ts
type Item = {
  id: number
  categoryId: number
  name: string              // max 64 characters
  price: number
  allergens: string | null  // max 64 characters
}
```

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/items` | | `Item[]` |
| GET | `/api/items/{id}` | | `Item` |
| GET | `/api/items/{id}/discount` | | `Discount` active right now, `404` if there is none |
| GET | `/api/items/{id}/lowest-price` | | `number`, lowest price in the last 30 days |
| POST | `/api/items` | `Item` without `id` | `201` `Item` |
| PUT | `/api/items/{id}` | `Item` | `204` |
| DELETE | `/api/items/{id}` | | `204` |

## Discounts

```ts
type Discount = {
  id: number
  itemId: number
  startDate: string
  endDate: string
  value: number   // fraction off: 0.3 = 30% off
}
```

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/discounts` | | `Discount[]`, only the ones active right now |
| GET | `/api/discounts/{id}` | | `Discount`, `404` if it isn't active right now |
| POST | `/api/discounts` | `Discount` without `id` | `201` `Discount` |
| PUT | `/api/discounts/{id}` | `Discount` | `204` |
| DELETE | `/api/discounts/{id}` | | `204` |

## Item stocks

One stock per item, identified by `itemId`.

```ts
type ItemStock = {
  itemId: number
  amount: number
}
```

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/itemStocks` | | `ItemStock[]` |
| GET | `/api/itemStocks/{itemId}` | | `ItemStock` |
| POST | `/api/itemStocks` | `ItemStock` | `201` `ItemStock` |
| PUT | `/api/itemStocks/{itemId}` | `ItemStock` | `204` |
| DELETE | `/api/itemStocks/{itemId}` | | `204` |

## Cart 🔒

All cart endpoints require login. `id` / `cartItemId` is the id of the cart entry returned by `add`, not the item id.

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/cart/add` | `{ itemId, quantity }` | `number`, id of the new cart entry. Sold out: `400` |
| PUT | `/api/cart/update` | `{ id, quantity }` | `200` |
| DELETE | `/api/cart/remove/{cartItemId}` | | `200` |

## Orders

```ts
type OrderDetails = {
  order: { id: number, userId: number }
  orderedItems: {
    id: number
    itemId: number
    orderId: number
    amount: number
    discount: number   // fraction off at the time of ordering, 0 = none
  }[]
}
```

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/orders/order` 🔒 | | `200`, places an order from the whole cart. Empty cart: `404` |
| GET | `/api/orders/get` 🔒 | | the user's orders (not implemented yet) |
| GET | `/api/orders/get/byOrderId/{orderId}` | | `OrderDetails` |
