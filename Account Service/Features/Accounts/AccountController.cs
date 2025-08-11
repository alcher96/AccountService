using AccountService.Features.Accounts.AddAccount.Command;
using AccountService.Features.Accounts.DeleteAccount.Command;
using AccountService.Features.Accounts.GetAccount.Query;
using AccountService.Features.Accounts.PatchAccount.Command;
using AccountService.Features.Accounts.UpdateAccount.Command;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Accounts
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MbResult<AccountDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<AccountDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MbResult<object>))]
        [HttpPost]
        [Authorize(Roles = "user")] 
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
        {
            var result = await mediator.Send(command);
            //доп.проверка по полю IsSuccess
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(GetAccountById), new { id = result.Value!.AccountId }, result);
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> GetAccounts([FromQuery] Guid? ownerId, [FromQuery] AccountType? type)
        {
            var query = new GetAccountsQuery { OwnerId = ownerId, Type = type };
            var result = await mediator.Send(query);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> GetAccountById(Guid id)
        {
            var query = new GetAccountByIdQuery { Id = id };
            var result = await mediator.Send(query);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [SwaggerOperation(Summary = "Обновляет данные счёта")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequestDto request)
        {
            var command = new UpdateAccountCommand { Id = id, Request = request };
            var result = await mediator.Send(command);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
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
        /// <response code="401">Для запроса требуется аутентификация</response>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [SwaggerOperation(Summary = "Частично обновляет данные счёта")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> PatchAccount(Guid id, [FromBody] PatchAccountRequestDto request)
        {
            var command = new PatchAccountCommand { Id = id, Request = request };
            var result = await mediator.Send(command);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
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
        /// <response code="400">Ошибка валидации</response>
        /// <response code="404">Счёт не найден</response>
        /// <response code="401">Для запроса требуется аутентификация</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> DeleteAccount(Guid id)
        {
            var command = new DeleteAccountCommand { Id = id };
            var result = await mediator.Send(command);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return NoContent();
        }



    }
}
