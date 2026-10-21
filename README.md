
# JajkoBackend - API Endpoints

## Table of Contents

- [Cart](#cart)
- [Categories](#categories)
- [Discounts](#discounts)
- [Items](#items)
- [Item Stocks](#item-stocks)
- [Orders](#orders)
- [Notes](#notes)

---

## Cart

| Method | Endpoint | Description | Authorization |
|:---:|---|---|:---:|
| `POST` | `/add` | Adds an item to the cart. | Required |

---

## Categories

| Method | Endpoint | Description |
|:---:|---|---|
| `GET` | `/api/categories` | Gets all categories. |
| `GET` | `/api/categories/{id}` | Gets a category by ID. |
| `GET` | `/api/categories/{id}/children` | Gets child categories recursively. |
| `POST` | `/api/categories` | Creates a category. |
| `PUT` | `/api/categories/{id}` | Updates a category. |
| `DELETE` | `/api/categories/{id}` | Deletes a category. |

---

## Discounts

| Method | Endpoint | Description |
|:---:|---|---|
| `GET` | `/api/discounts` | Gets all discounts. |
| `GET` | `/api/discounts/{id}` | Gets a discount by ID. |
| `POST` | `/api/discounts` | Creates a discount. |
| `PUT` | `/api/discounts/{id}` | Updates a discount. |
| `DELETE` | `/api/discounts/{id}` | Deletes a discount. |

---

## Items

| Method | Endpoint | Description |
|:---:|---|---|
| `GET` | `/api/items` | Gets all items. |
| `GET` | `/api/items/{id}` | Gets an item by ID. |
| `GET` | `/api/items/{id}/discount` | Gets the active discount for an item. |
| `GET` | `/api/items/{id}/lowest-price` | Gets the lowest price in the last 30 days. |
| `POST` | `/api/items` | Creates an item and records its price. |
| `PUT` | `/api/items/{id}` | Updates an item. |
| `DELETE` | `/api/items/{id}` | Deletes an item. |

---

## Item Stocks

| Method | Endpoint | Description |
|:---:|---|---|
| `GET` | `/api/itemstocks` | Gets all stock records. |
| `GET` | `/api/itemstocks/{itemId}` | Gets stock for a specific item. |
| `POST` | `/api/itemstocks` | Creates a stock record. |
| `PUT` | `/api/itemstocks/{id}` | Updates a stock record. |
| `DELETE` | `/api/itemstocks/{id}` | Deletes a stock record. |

---

## Orders

| Method | Endpoint | Description | Authorization / Status |
|:---:|---|---|---|
| `POST` | `/order` | Creates an order from the user's cart. | Required |
| `GET` | `/get` | Gets user orders. | Implementation incomplete |
| `GET` | `/get/byOrderId/{orderId}` | Gets order details by ID. | Ownership verification missing |

---

## Notes

### Routing

- Cart and Orders use absolute route attributes.
- Their routes may differ from the standard `/api/{controller}` pattern.

### Security

- Authentication depends on the `[Authorize]` attribute.
- `GetForUser()` does not currently filter orders by user ID.
- `GetByOrderId()` does not verify order ownership.

> **Security recommendation:** Filter orders by the authenticated user's ID and verify order ownership before returning order details.

### Price History

- `GetLowestPrice()` may fail if no price history exists for the requested item.

> **Recommendation:** Handle missing price history and return an appropriate response, such as `404 Not Found` or a nullable result.

### Implementation Status

| Feature | Status |
|---|---|
| Cart authorization | Required |
| Order creation authorization | Required |
| User-specific order filtering | Needs implementation |
| Order ownership verification | Needs implementation |
| Missing price history handling | Needs implementation |
