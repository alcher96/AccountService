# AccountService API

AccountService API — это RESTful API для управления банковскими счетами, транзакциями и переводами. Проект построен на .NET с использованием MediatR, FluentValidation, AutoMapper и Swashbuckle для документации API. Поддерживаются операции создания, обновления, частичного обновления и удаления счетов, а также регистрация транзакций и переводов между счетами. Проект обновлен в контексте третьего задания курса по создания микросервиса "Банковские счета"

## Установка, запуск и важные замечания!
 **Клонируйте репозиторий**:
```bash
   git clone https://github.com/alcher96/AccountService
   в папке проекта Account Service
   docker compose up -d --build
```
- Контйнеры соединены в сеть, при поднятии контейнеров, выполнить 
```
dotnet ef database update
для применения миграций
```
- Для keycloak не установлен healthcheck поэтому какое то время(~10 сек) запрос на получения токена может не отправляться.
- При аутентификации через swagger в поле нужно вставлять по шаблону "Bearer {jwt}" (если вставить только токен, работать не будет!).
- Выполнена сереализация транзакций и оптимистическая блокировка
- В проекте AccountService.Tests выполнить 
```
dotnet build
dotnet test
```
## Доступ к Swagger UI и Hangfire**:
   - В режиме разработки приложение автоматически перенаправляет с корневого URL (`/`) на `/swagger`.
   - Откройте в браузере: `(https://localhost:80)`.
   - Откройте в браузере: `(https://localhost:80/hangfire)` для доступа к hangfire. Так же можно тригернуть задачу.

### Получение токена

Воспользуйтесь файлом для postman для получения токена или используйте curl

curl -v -X POST 'http://localhost:8080/realms/account-service/protocol/openid-connect/token' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=account-service-client' \
  -d 'client_secret=your-actual-client-secret' \
  -d 'username=test-user' \
  -d 'password=test-password' \
  -d 'grant_type=password' \
  -d 'scope=openid profile email'


## Технологии

- **.NET 9**: Основной фреймворк.
- **MediatR**: Для реализации CQRS (команды и запросы).
- **FluentValidation**: Для валидации входных данных.
- **AutoMapper**: Для маппинга между моделями и DTO.
- **Swashbuckle.AspNetCore**: Для генерации Swagger-документации.
- **Keycloak**: Для JWT-аутентификации.
- **Moq**: Для модульного тестирования.
- **PostgreSql**: Заглушка репозитория заменена на реальную БД.
- **Hangfire**: Для background задачи по начислению процентов.




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

## Аутентификация

API использует JWT-аутентификацию через Keycloak. Для доступа к защищённым эндпоинтам (`/api/Account`, `/api/Transaction`) требуется токен с ролью `user`.

## Примеры использования API


### Создание счёта

**Запрос**:
```http
POST /api/Account
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "accountType": "Checking",
  "currency": "USD",
  "interestRate": null
}
```

**Ответ** (HTTP 201 Created):
```json
{
  "isSuccess": true,
  "value": {
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accountType": "Checking",
    "currency": "USD",
    "balance": 0,
    "interestRate": 0,
    "openingDate": "2025-08-03T18:58:00Z",
    "closingDate": null
  },
  "mbError": null,
  "validationErrors": null
}
```

**Ответ** (HTTP 400 Bad Request):
```json
{
  "isSuccess": false,
  "value": null,
  "mbError": "Validation failed",
  "validationErrors": {
    "currency": ["Unsupported currency"],
    "accountType": ["Invalid account type"]
  }
}
```

**Ответ** (HTTP 401 Unauthorized):
```json
{
  "isSuccess": false,
  "value": null,
  "mbError": "Unauthorized: Invalid or missing token",
  "validationErrors": null
}
```

**Запрос**:
```http
POST /api/Transaction
Content-Type: application/json
Authorization: Bearer <access_token>

{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 100.50,
  "transactionType": "Deposit",
  "description": "Initial deposit"
}
```

**Ответ** (HTTP 201 Created):
```json
{
  "isSuccess": true,
  "value": {
    "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": 100.50,
    "transactionType": "Deposit",
    "transactionDate": "2025-08-03T18:58:00Z",
    "description": "Initial deposit"
  },
  "mbError": null,
  "validationErrors": null
}
```

**Ответ** (HTTP 400 Bad Request):
```json
{
  "isSuccess": false,
  "value": null,
  "mbError": "Validation failed",
  "validationErrors": {
    "amount": ["Amount must be positive"],
    "transactionType": ["Invalid transaction type"]
  }
}
```

## Особенности реализации

- **Аутентификация**: Интеграция с Keycloak через JWT-токены. Все защищённые эндпоинты требуют заголовок `Authorization: Bearer <access_token>` с ролью `user`.
- **MbResult<T>**: Все ответы API возвращаются в формате `MbResult<T>`, включающем поля `isSuccess`, `value`, `mbError` и `validationErrors` для единообразной обработки успехов и ошибок.
- **CORS**: Настроен для поддержки кросс-доменных запросов, позволяя интеграцию с фронтенд-приложениями.
- **Валидация**: FluentValidation проверяет входные данные. Поддерживаемые валюты (`RUB`, `USD`, `EUR`) и типы счетов (`Checking`, `Deposit`, `Credit`) вынесены в класс `Constants`.
