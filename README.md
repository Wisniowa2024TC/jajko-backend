Dokumentacja bazy danych – JajkoBackend
1. Informacje ogólne

Baza danych projektu JajkoBackend wykorzystuje Entity Framework Core w wersji 10.0.12. Na podstawie typów kolumn można wnioskować, że zastosowano SQLite.

2. Struktura bazy danych
Items – przechowuje produkty, ich nazwy, ceny, alergeny i identyfikatory kategorii.
Orders – przechowuje zamówienia przypisane do użytkowników.
OrderedItems – zawiera produkty przypisane do zamówień, ich ilości, rabaty i uwagi.
AspNetUsers – przechowuje dane użytkowników.
AspNetRoles – przechowuje role użytkowników.

Dodatkowo baza zawiera tabele systemowe ASP.NET Core Identity do obsługi uprawnień, deklaracji, logowania i tokenów.

3. Relacje
Jeden użytkownik może posiadać wiele zamówień.
Użytkownicy mogą mieć przypisane role.
Tabela OrderedItems łączy zamówienia z produktami za pomocą klucza złożonego (OrderId, ItemId).
4. Klucze i zabezpieczenia
Tabele posiadają klucze główne identyfikujące rekordy.
Nazwy użytkowników i ról mają unikalne indeksy.
Usunięcie użytkownika powoduje kaskadowe usunięcie jego zamówień.
ASP.NET Core Identity obsługuje dane związane z uwierzytelnianiem i zarządzaniem użytkownikami.
5. Migracje

Plik AppDbContextModelSnapshot przedstawia aktualny model danych i jest wykorzystywany przez Entity Framework Core podczas tworzenia kolejnych migracji.
