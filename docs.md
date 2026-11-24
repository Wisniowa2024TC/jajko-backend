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
  price: number             // current price
  referencePrice: number    // crossed-out price, if higher than price the item is discounted
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
| POST | `/api/cart/add` | `{ itemId, quantity }` | `number`, id of the cart entry. If the item is already in the cart, its quantity is increased. Sold out: `400` |
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
| POST | `/api/orders/order` 🔒 | | `number`, id of the new order. Orders the whole cart, decreases the stock and empties the cart. Empty cart: `404`, not enough stock: `400` |
| GET | `/api/orders/get` 🔒 | | `OrderDetails[]`, all orders of the logged-in user |
| GET | `/api/orders/get/byOrderId/{orderId}` | | `OrderDetails` |

## BLIK payments

Jajko is the merchant: the shopper gets a 6-digit code in their GoonBank app, types it at checkout, and approves the payment in that app. Jajko starts the payment at the bank and polls it. The `bank` section of `appsettings.json` sets the bank URL and our `merchantId`.

```ts
type BlichPayment = {
  paymentId: string        // our id, not the bank's
  orderId: number
  status: "Initiating" | "Pending" | "Approved" | "Failed" | "Expired"
  amount: number           // the order total, set by the server
  expiresAt: string        // UTC; about 2 min 15 s after the start
  orderPaid: boolean
  failureReason: string | null
}
```

| Method | Path | Body | Returns |
|---|---|---|---|
| 🔒 POST | `/api/orders/{orderId}/blich-payments` | `{ blikCode: string }` (6 digits) | `202` `BlichPayment` with a `Location` header. `400` bad code or the bank refused it (`{ message }`), `404` not your order, `409` already paid or a payment is in progress, `429` more than 5 attempts a minute, `502` bank unavailable |
| 🔒 GET | `/api/blich-payments/{paymentId}` | | `BlichPayment`, `404` if it isn't yours. Poll every ~2 s until `status` isn't `Pending` |
| 🔒 GET | `/api/orders/{orderId}/blich-payments` | | `BlichPayment[]`, newest first |

The order becomes paid (`order.paymentStatus` = `1`, `order.paidAt` set) when the bank approves, even if the checkout page was closed: a background service polls pending payments every 3 s. `Failed` covers a rejection, a decline and a bank-side timeout; `Expired` means it wasn't approved in time. The bank can't cancel a payment, so abandoning checkout just lets it expire.
