JajkoBackend – API Endpoints
Cart
Method	Endpoint	Description	Authorization
POST	/add	Adds an item to the cart	Required
Categories
Method	Endpoint	Description
GET	/api/categories	Gets all categories
GET	/api/categories/{id}	Gets a category by ID
GET	/api/categories/{id}/children	Gets child categories recursively
POST	/api/categories	Creates a category
PUT	/api/categories/{id}	Updates a category
DELETE	/api/categories/{id}	Deletes a category
Discounts
Method	Endpoint	Description
GET	/api/discounts	Gets all discounts
GET	/api/discounts/{id}	Gets a discount by ID
POST	/api/discounts	Creates a discount
PUT	/api/discounts/{id}	Updates a discount
DELETE	/api/discounts/{id}	Deletes a discount
Items
Method	Endpoint	Description
GET	/api/items	Gets all items
GET	/api/items/{id}	Gets an item by ID
GET	/api/items/{id}/discount	Gets the active discount for an item
GET	/api/items/{id}/lowest-price	Gets the lowest price in the last 30 days
POST	/api/items	Creates an item and records its price
PUT	/api/items/{id}	Updates an item
DELETE	/api/items/{id}	Deletes an item
ItemStocks
Method	Endpoint	Description
GET	/api/itemstocks	Gets all stock records
GET	/api/itemstocks/{itemId}	Gets stock for an item
POST	/api/itemstocks	Creates a stock record
PUT	/api/itemstocks/{id}	Updates a stock record
DELETE	/api/itemstocks/{id}	Deletes a stock record
Orders
Method	Endpoint	Description	Authorization
POST	/order	Creates an order from the user's cart	Required
GET	/get	Gets user orders (implementation incomplete)	Not specified
GET	/get/byOrderId/{orderId}	Gets order details by ID	Not specified
Notes
Cart and Orders use absolute route attributes, so their endpoints do not inherit the controller prefixes.
GetForUser() currently does not filter orders by the authenticated user's ID.
GetByOrderId() does not verify whether the order belongs to the authenticated user.
GetLowestPrice() may fail if no price history exists for the specified item.
Authentication requirements depend on the [Authorize] attributes defined in the controllers.
