JajkoBackend – API Endpoints
Cart
Metoda	Endpoint	Opis	Autoryzacja
POST	/add	Dodaje produkt do koszyka	Wymagana
Categories
Metoda	Endpoint	Opis
GET	/api/categories	Pobiera wszystkie kategorie
GET	/api/categories/{id}	Pobiera kategorię po ID
GET	/api/categories/{id}/children	Pobiera kategorię potomne
POST	/api/categories	Tworzy kategorię
PUT	/api/categories/{id}	Aktualizuje kategorię
DELETE	/api/categories/{id}	Usuwa kategorię
Discounts
Metoda	Endpoint	Opis
GET	/api/discounts	Pobiera wszystkie rabaty
GET	/api/discounts/{id}	Pobiera rabat po ID
POST	/api/discounts	Tworzy rabat
PUT	/api/discounts/{id}	Aktualizuje rabat
DELETE	/api/discounts/{id}	Usuwa rabat
Items
Metoda	Endpoint	Opis
GET	/api/items	Pobiera wszystkie produkty
GET	/api/items/{id}	Pobiera produkt po ID
GET	/api/items/{id}/discount	Pobiera aktywny rabat produktu
GET	/api/items/{id}/lowest-price	Pobiera najniższą cenę z ostatnich 30 dni
POST	/api/items	Tworzy produkt i zapisuje historię ceny
PUT	/api/items/{id}	Aktualizuje produkt
DELETE	/api/items/{id}	Usuwa produkt
ItemStocks
Metoda	Endpoint	Opis
GET	/api/itemstocks	Pobiera wszystkie stany magazynowe
GET	/api/itemstocks/{itemId}	Pobiera stan magazynowy produktu
POST	/api/itemstocks	Tworzy stan magazynowy
PUT	/api/itemstocks/{id}	Aktualizuje stan magazynowy
DELETE	/api/itemstocks/{id}	Usuwa stan magazynowy
Orders
Metoda	Endpoint	Opis	Autoryzacja
POST	/order	Tworzy zamówienie na podstawie koszyka	Wymagana
GET	/get	Pobiera zamówienia użytkownika (implementacja niekompletna)	Brak atrybutu [Authorize]
GET	/get/byOrderId/{orderId}	Pobiera szczegóły zamówienia po ID	Brak atrybutu [Authorize]
Uwagi
Endpointy Cart i Orders mają bezwzględne trasy zdefiniowane przez atrybuty zaczynające się od /. W związku z tym nie dziedziczą prefiksów /api/cart ani /api/orders.
Endpointy Cart i Orders korzystają z identyfikatora użytkownika pobieranego z kontekstu uwierzytelnienia w wybranych metodach.
GetForUser() nie filtruje obecnie zamówień według userId i nie zwraca pobranych pozycji zamówień.
GetByOrderId() nie sprawdza, czy zamówienie należy do zalogowanego użytkownika.
W Items/{id}/lowest-price brak historii cen może spowodować wyjątek przy wywołaniu MinAsync().
