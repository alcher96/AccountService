using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AccountService.Features.Transactions.AddTransaction.Command;
using AccountService.Features.Transactions.PerformTransfer.Command;
using AccountService.Features.Accounts;
using AccountService.Features.Transactions.AccrueInterest.Command;
using AccountService.Features.Transactions.GetAccountTransactions.Query;

#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Transactions
{
    /// <summary>
    /// Контроллер для управления транзакциями
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController(IMediator mediator) : ControllerBase
    {
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionCommand command)
        {
            var result = await mediator.Send(command);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(AccountController.GetAccountById),
                new { controller = "Account", id = command.AccountId }, result);
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [HttpPost("transfers")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> PerformTransfer([FromBody] PerformTransferCommand command)
        {
            var result = await mediator.Send(command);
            if (!result.IsSuccess)
            {
                if (result.MbError == "Concurrency conflict")
                {
                    return Conflict(result);
                }
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(AccountController.GetAccountById),
                new { controller = "Account", id = command.FromAccountId }, result);
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [HttpGet("{accountId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> GetAccountTransactions(Guid accountId, [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = new GetAccountTransactionsQuery
                { AccountId = accountId, StartDate = startDate, EndDate = endDate };
            var result = await mediator.Send(query);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// тестовый ендпоинт для проверки, во время разработки, до введения hangfire в самом hangfire используется IMediator
        /// </summary>
        [HttpPost("accrue-interest")]
        public async Task<IActionResult> AccrueInterest(CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new AccrueInterestCommand(), cancellationToken);
            if (!result.IsSuccess)
            {
                if (result.MbError?.Contains("Concurrency conflict") == true)
                {
                    return Conflict(new { error = result.MbError });
                }

                Console.WriteLine($"Accrue interest failed: {System.Text.Json.JsonSerializer.Serialize(result.MbError)}");
                return BadRequest(result.MbError);
            }
            return Ok(result);
        }
    }
}