using Account_Service.Accounts.AddAccount.Command;
using Account_Service.Accounts.DeleteAccount.Command;
using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Accounts.PatchAccount.Command;
using Account_Service.Accounts.UpdateAccount.Command;
using Account_Service.Transactions.AddTransaction.Command;
using Account_Service.Transactions.GetAccountTransactions.Query;
using Account_Service.Transactions.PerformTransfer.Command;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Account_Service.Accounts
{

    /// <summary>
    /// Контроллер для управления банковскими счетами
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(IMediator mediator) : ControllerBase
    {
        /// <summary>
        /// Создаёт новый банковский счёт
        /// </summary>
        /// <remarks>
        /// Создаёт счёт для указанного владельца с заданными параметрами.
        /// Поддерживаемые валюты: RUB, USD, EUR.
        /// Процентная ставка обязательна для типов Deposit и Credit, но не для Checking.
        /// </remarks>
        /// <param name="command">Данные для создания счёта</param>
        /// <returns>Созданный счёт</returns>
        /// <response code="201">Счёт успешно создан</response>
        /// <response code="400">Недопустимые данные (например, неподдерживаемая валюта)</response>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
        {
            var result = await mediator.Send(command);
            return CreatedAtAction(nameof(GetAccountById), new { id = result.AccountId }, result);
        }

        /// <summary>
        /// Получает список счетов с фильтрам по владельцу и типу
        /// </summary>
        /// <remarks>
        /// Возвращает список счетов, отфильтрованный по идентификатору владельца (опционально) и типу счёта (опционально).
        /// Если параметры не указаны, возвращает все доступные счета.
        /// </remarks>
        /// <param name="ownerId">ID владельца (опционально)</param>
        /// <param name="type">Тип счёта (опционально)</param>
        /// <returns>Список счетов</returns>
        /// <response code="200">Список счетов успешно возвращён</response>
        [HttpGet]
        public async Task<IActionResult> GetAccounts([FromQuery] Guid? ownerId, [FromQuery] AccountType? type)
        {
            var query = new GetAccountsQuery { OwnerId = ownerId, Type = type };
            var result = await mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Получает данные счёта по идентификатору
        /// </summary>
        /// <remarks>
        /// Возвращает полную информацию о счёте по указанному идентификатору.
        /// </remarks>
        /// <param name="id">Идентификатор счёта</param>
        /// <returns>Данные счёта</returns>
        /// <response code="200">Счёт найден и возвращён</response>
        /// <response code="400">Неверный формат идентификатора</response>
        /// <response code="404">Счёт не найден</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAccountById(Guid id)
        {
            var query = new GetAccountByIdQuery { Id = id };
            var result = await mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Обновляет данные счёта
        /// </summary>
        /// <remarks>
        /// Полностью обновляет данные счёта с указанным идентификатором.
        /// Все поля должны быть предоставлены.
        /// </remarks>
        /// <param name="id">Идентификатор счёта</param>
        /// <param name="request">Новые данные счёта</param>
        /// <returns>Обновлённый счёт</returns>
        /// <response code="200">Счёт успешно обновлён</response>
        /// <response code="400">Недопустимые данные</response>
        /// <response code="404">Счёт не найден</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Summary = "Обновляет данные счёта")]
        public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequestDto request)
        {
            var command = new UpdateAccountCommand { Id = id, Request = request };
            var result = await mediator.Send(command);
            return Ok(result);
        }


        /// <summary>
        /// Частично обновляет данные счёта
        /// </summary>
        /// <remarks>
        /// Обновляет только указанные поля счёта (например, валюта, процентная ставка).
        /// Поддерживаемые поля: Currency, Type, InterestRate, Balance.
        /// Нельзя изменить валюту, если по счёту есть транзакции.
        /// </remarks>
        /// <param name="id">Идентификатор счёта</param>
        /// <param name="request">Данные для частичного обновления</param>
        /// <returns>Обновлённый счёт</returns>
        /// <response code="200">Счёт успешно обновлён</response>
        /// <response code="400">Недопустимые данные или попытка изменить валюту при наличии транзакций</response>
        /// <response code="404">Счёт не найден</response>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Summary = "Частично обновляет данные счёта")]
        public async Task<IActionResult> PatchAccount(Guid id, [FromBody] PatchAccountRequestDto request)
        {
            var command = new PatchAccountCommand { Id = id, Request = request };
            var result = await mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Удаляет счёт
        /// </summary>
        /// <remarks>
        /// Удаляет счёт с указанным идентификатором.
        /// </remarks>
        /// <param name="id">Идентификатор счёта</param>
        /// <returns>Нет содержимого</returns>
        /// <response code="204">Счёт успешно удалён</response>
        /// <response code="404">Счёт не найден</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAccount(Guid id)
        {
            var command = new DeleteAccountCommand { Id = id };
            await mediator.Send(command);
            return NoContent();
        }


        /// <summary>
        /// Регистрирует транзакцию по счёту
        /// </summary>
        /// <remarks>
        /// Создаёт новую транзакцию (дебетовую или кредитную) для указанного счёта.
        /// Валюта транзакции должна совпадать с валютой счёта.
        /// </remarks>
        /// <param name="command">Данные транзакции</param>
        /// <returns>Созданная транзакция</returns>
        /// <response code="201">Транзакция успешно создана</response>
        /// <response code="400">Недопустимые данные (например, недостаточно средств)</response>
        /// <response code="404">Счёт не найден</response>
        [HttpPost("transactions")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionCommand command)
        {
            var result = await mediator.Send(command);
            return CreatedAtAction(nameof(GetAccountById), new { id = command.AccountId }, result);
        }

        /// <summary>
        /// Выполняет перевод между счетами
        /// </summary>
        /// <remarks>
        /// Создаёт две транзакции: дебетовую для счёта отправителя и кредитную для счёта получателя.
        /// Валюта перевода должна совпадать с валютами обоих счетов.
        /// </remarks>
        /// <param name="command">Данные перевода</param>
        /// <returns>Массив созданных транзакций</returns>
        /// <response code="201">Перевод успешно выполнен</response>
        /// <response code="400">Недопустимые данные (например, недостаточно средств или разные валюты)</response>
        /// <response code="404">Один из счетов не найден</response>
        [HttpPost("transfers")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PerformTransfer([FromBody] PerformTransferCommand command)
        {
            var result = await mediator.Send(command);
            return CreatedAtAction(nameof(GetAccountById), new { id = command.FromAccountId }, result);
        }

        /// <summary>
        /// Получает выписку по счёту за период
        /// </summary>
        /// <remarks>
        /// Возвращает список транзакций для указанного счёта, отфильтрованный по датам (опционально).
        /// Если даты не указаны, возвращаются все транзакции.
        /// </remarks>
        /// <param name="accountId">Идентификатор счёта</param>
        /// <param name="startDate">Начальная дата периода (опционально)</param>
        /// <param name="endDate">Конечная дата периода (опционально)</param>
        /// <returns>Список транзакций</returns>
        /// <response code="200">Выписка успешно возвращена</response>
        /// <response code="400">Недопустимые даты (например, endDate раньше startDate)</response>
        /// <response code="404">Счёт не найден</response>
        [HttpGet("{accountId}/transactions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAccountTransactions(Guid accountId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = new GetAccountTransactionsQuery { AccountId = accountId, StartDate = startDate, EndDate = endDate };
            var result = await mediator.Send(query);
            return Ok(result);
        }
    }
}
