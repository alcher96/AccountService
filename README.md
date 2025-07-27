# AccountService API

AccountService API — это RESTful API для управления банковскими счетами, транзакциями и переводами. Проект построен на .NET с использованием MediatR, FluentValidation, AutoMapper и Swashbuckle для документации API. Поддерживаются операции создания, обновления, частичного обновления и удаления счетов, а также регистрация транзакций и переводов между счетами. Проект сделан в контексте курса по создания микросервиса "Банковские счета"

## Основные возможности

- **Создание счёта**: Создание банковского счёта с указанием владельца, типа счёта, валюты и начального баланса.
- **Обновление счёта**: Полное (`PUT`) и частичное (`PATCH`) обновление данных счёта.
- **Транзакции**: Регистрация дебетовых и кредитных транзакций.
- **Переводы**: Перевод средств между счетами с проверкой валют.
- **Фильтрация счетов**: Получение списка счетов с фильтрацией по владельцу и типу.
- **Валидация**: Используется FluentValidation для проверки входных данных (например, поддерживаемые валюты: `RUB`, `USD`, `EUR`).
- **Swagger UI**: Интерактивная документация API с автоматическим открытием в режиме разработки.

## Технологии

- **.NET 8**: Основной фреймворк.
- **MediatR**: Для реализации CQRS (команды и запросы).
- **FluentValidation**: Для валидации входных данных.
- **AutoMapper**: Для маппинга между моделями и DTO.
- **Swashbuckle.AspNetCore**: Для генерации Swagger-документации.
- **Moq**: Для модульного тестирования.
- **InMemoryAccountRepository**: Заглушка репозитория для хранения данных в памяти.

## Доступ к Swagger UI**:
   - В режиме разработки приложение автоматически перенаправляет с корневого URL (`/`) на `/swagger`.
   - Откройте в браузере: `(https://localhost:7299)`.

## Эндпоинты API
```
[POST]/api/Account - Создание банковского счета
[GET]/api/Account  - Получение списка счетов
[GET]/api/Account/{id} - Получение данных счета по идентификатору
[PUT]/api/Account/{id} - Обновление данных счета
[PATCH]/api/Account/{id} - Частичное обновление данных счета
[DELETE]/api/Account/{id} - Удаление счета
[POST]/api/Account/transactions - Регистрация транзакции по счету
[POST]/api/Account/transfers - Выполнение перевода между счетами
[GET]/api/Account/{id}/transactions - Получение выписки по счету за период
```
## Примеры использования API

### Создание счёта

**Запрос**:
```http
POST /api/Account
Content-Type: application/json

{
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accountType": "Checking",
    "currency": "RUB",
    "balance": 222,
    "interestRate": null
}
```

**Ответ** (HTTP 201 Created):
```json
{
    "accountId": "ec47bdcc-e465-460c-83a1-98ae0522280e",
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accountType": "Checking",
    "currency": "RUB",
    "balance": 222,
    "interestRate": null,
    "openingDate": "2025-07-27T14:08:00Z",
    "closingDate": null
}
```

### Частичное обновление счёта

**Запрос**:
```http
PATCH /api/Account/ec47bdcc-e465-460c-83a1-98ae0522280e
Content-Type: application/json

{
    "currency": "USD",
    "balance": 500
}
```

**Ответ** (HTTP 200 OK):
```json
{
    "accountId": "ec47bdcc-e465-460c-83a1-98ae0522280e",
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accountType": "Checking",
    "currency": "USD",
    "balance": 500,
    "interestRate": null,
    "openingDate": "2025-07-27T14:08:00Z",
    "closingDate": null
}
```

### Перевод между счетами

**Запрос**:
```http
POST /api/Account/transfers
Content-Type: application/json

{
    "fromAccountId": "ec47bdcc-e465-460c-83a1-98ae0522280e",
    "toAccountId": "4b5c6d7e-8f9a-0b1c-2d3e-4f5a6b7c8d9e",
    "amount": 100,
    "currency": "USD",
    "description": "Test transfer"
}
```

**Ответ** (HTTP 201 Created):
```json
[
    {
        "id": "1a2b3c4d-5e6f-7a8b-9c0d-e1f2a3b4c5d6",
        "accountId": "ec47bdcc-e465-460c-83a1-98ae0522280e",
        "type": "Debit",
        "amount": 100,
        "currency": "USD",
        "description": "Test transfer",
        "dateTime": "2025-07-27T14:08:00Z"
    },
    {
        "id": "2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7f",
        "accountId": "4b5c6d7e-8f9a-0b1c-2d3e-4f5a6b7c8d9e",
        "type": "Credit",
        "amount": 100,
        "currency": "USD",
        "description": "Test transfer",
        "dateTime": "2025-07-27T14:08:00Z"
    }
]
```

## Особенности реализации

- **Валидация**: Все команды валидируются с помощью FluentValidation. Поддерживаемые валюты (`RUB`, `USD`, `EUR`) вынесены в общий класс `Constants` для устранения дублирования.
- **Swagger**: В режиме разработки корневой URL (`/`) перенаправляет на Swagger UI для удобного тестирования API.
- **CQRS**: MediatR реализует паттерн CQRS, разделяя команды (модификация данных) и запросы (чтение данных).
