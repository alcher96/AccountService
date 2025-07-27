using Account_Service.Accounts;
using Account_Service.Accounts.AddAccount.Command;
using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Transactions.AddTransaction.Command;
using Account_Service.Transactions.PerformTransfer.Command;
using Account_Service.Transactions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Account_Service.Accounts.UpdateAccount.Command;
using Account_Service.Accounts.PatchAccount.Command;

namespace AccountService.Tests;

public class AccountControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AccountController(_mediatorMock.Object);
    }

    [Fact]
    public async Task CreateAccount_ValidCommand_ReturnsCreatedResult()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            AccountType = AccountType.Deposit,
            Currency = "USD",
            Balance = 1000,
            InterestRate = 1.5m
        };
        var accountDto = new AccountDto
        {
            AccountId = Guid.NewGuid(),
            OwnerId = command.OwnerId,
            AccountType = command.AccountType,
            Currency = command.Currency,
            Balance = command.Balance,
            InterestRate = command.InterestRate,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(accountDto);

        // Act
        var result = await _controller.CreateAccount(command);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(AccountController.GetAccountById), createdResult.ActionName);
        Assert.Equal(accountDto.AccountId, createdResult.RouteValues?["id"]);
        Assert.Equal(accountDto, createdResult.Value);
    }


    [Fact]
    public async Task GetAccountById_ValidId_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var accountDto = new AccountDto
        {
            AccountId = accountId,
            OwnerId = Guid.NewGuid(),
            AccountType = AccountType.Deposit,
            Currency = "USD",
            Balance = 1000,
            InterestRate = 1.5m,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountByIdQuery>(q => q.Id == accountId), It.IsAny<CancellationToken>())).ReturnsAsync(accountDto);

        // Act
        var result = await _controller.GetAccountById(accountId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(accountDto, okResult.Value);
    }

   

    [Fact]
    public async Task CreateTransaction_ValidCommand_ReturnsCreatedResult()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Type = TransactionType.Credit,
            Amount = 500,
            Currency = "USD",
            Description = "Test deposit"
        };
        var transactionDto = new TransactionDto
        {
            TransactionId = Guid.NewGuid(),
            AccountId = command.AccountId,
            Type = command.Type,
            Amount = command.Amount,
            Currency = command.Currency,
            Description = command.Description,
            DateTime = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(transactionDto);

        // Act
        var result = await _controller.CreateTransaction(command);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(AccountController.GetAccountById), createdResult.ActionName);
        Assert.Equal(command.AccountId, createdResult.RouteValues?["id"]);
        Assert.Equal(transactionDto, createdResult.Value);
    }

   

    [Fact]
    public async Task PerformTransfer_ValidCommand_ReturnsCreatedResult()
    {
        // Arrange
        var command = new PerformTransferCommand
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 500,
            Currency = "USD",
            Description = "Test transfer"
        };
        var transactions = new[]
        {
            new TransactionDto
            {
                TransactionId = Guid.NewGuid(),
                AccountId = command.FromAccountId,
                Type = TransactionType.Debit,
                Amount = command.Amount,
                Currency = command.Currency,
                Description = command.Description,
                DateTime = DateTime.UtcNow
            },
            new TransactionDto
            {
                TransactionId = Guid.NewGuid(),
                AccountId = command.ToAccountId,
                Type = TransactionType.Credit,
                Amount = command.Amount,
                Currency = command.Currency,
                Description = command.Description,
                DateTime = DateTime.UtcNow
            }
        };
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);

        // Act
        var result = await _controller.PerformTransfer(command);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(AccountController.GetAccountById), createdResult.ActionName);
        Assert.Equal(command.FromAccountId, createdResult.RouteValues?["id"]);
        Assert.Equal(transactions, createdResult.Value);
    }

    [Fact]
    public async Task CreateAccount_InvalidCurrency_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            AccountType = AccountType.Deposit,
            Currency = "invalid",
            Balance = 1000,
            InterestRate = 1.5m
        };
        var validationFailure = new ValidationFailure("Currency", "Валюта не поддерживается")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.CreateAccount(command));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Currency", error.PropertyName);
        Assert.Equal("Валюта не поддерживается", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
        // Удалена проверка ErrorCode, так как валидатор не задаёт его
    }


    [Fact]
    public async Task UpdateAccount_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateAccountRequestDto
        {
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Deposit,
            Currency = "USD",
            InterestRate = 1.5m
        };
        var command = new UpdateAccountCommand
        {
            Id = id,
            Request = request
        };
        var accountDto = new AccountDto
        {
            AccountId = id,
            OwnerId = request.OwnerId,
            AccountType = request.Type,
            Currency = request.Currency,
            Balance = 1000,
            InterestRate = request.InterestRate,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(It.Is<UpdateAccountCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>())).ReturnsAsync(accountDto);

        // Act
        var result = await _controller.UpdateAccount(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(accountDto, okResult.Value);
    }

    [Fact]
    public async Task UpdateAccount_InvalidCurrency_ThrowsValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateAccountRequestDto
        {
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Deposit,
            Currency = "INVALID",
            InterestRate = 1.5m
        };
        var command = new UpdateAccountCommand
        {
            Id = id,
            Request = request
        };
        var validationFailure = new ValidationFailure("Request.Currency", "Валюта не поддерживается")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(It.Is<UpdateAccountCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAccount(id, request));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Request.Currency", error.PropertyName);
        Assert.Equal("Валюта не поддерживается", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
    }

    [Fact]
    public async Task UpdateAccount_NonExistingAccount_ThrowsValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateAccountRequestDto
        {
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Deposit,
            Currency = "USD",
            InterestRate = 1.5m
        };
        var command = new UpdateAccountCommand
        {
            Id = id,
            Request = request
        };
        var validationFailure = new ValidationFailure("Id", "Счет не найден")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(It.Is<UpdateAccountCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAccount(id, request));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Id", error.PropertyName);
        Assert.Equal("Счет не найден", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
    }

    [Fact]
    public async Task PatchAccount_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new PatchAccountRequestDto
        {
            Currency = "USD",
            Type = AccountType.Deposit,
            InterestRate = 1.5m,
            Balance = 2000
        };
        var command = new PatchAccountCommand
        {
            Id = id,
            Request = request
        };
        var accountDto = new AccountDto
        {
            AccountId = id,
            OwnerId = Guid.NewGuid(),
            AccountType = request.Type!.Value,
            Currency = request.Currency,
            Balance = request.Balance!.Value,
            InterestRate = request.InterestRate,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(It.Is<PatchAccountCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>())).ReturnsAsync(accountDto);

        // Act
        var result = await _controller.PatchAccount(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(accountDto, okResult.Value);
    }

    [Fact]
    public async Task PatchAccount_InvalidCurrency_ThrowsValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new PatchAccountRequestDto
        {
            Currency = "INVALID",
            Type = AccountType.Deposit,
            InterestRate = 1.5m,
            Balance = 2000
        };
        var command = new PatchAccountCommand
        {
            Id = id,
            Request = request
        };
        var validationFailure = new ValidationFailure("Request.Currency", "Валюта не поддерживается")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(It.Is<PatchAccountCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.PatchAccount(id, request));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Request.Currency", error.PropertyName);
        Assert.Equal("Валюта не поддерживается", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
    }

    [Fact]
    public async Task PatchAccount_CurrencyChangeWithTransactions_ThrowsValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new PatchAccountRequestDto
        {
            Currency = "EUR"
        };
        var command = new PatchAccountCommand
        {
            Id = id,
            Request = request
        };
        var validationFailure = new ValidationFailure("Currency", "Нельзя изменить валюту счета с существующими транзакциями")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(It.Is<PatchAccountCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.PatchAccount(id, request));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Currency", error.PropertyName);
        Assert.Equal("Нельзя изменить валюту счета с существующими транзакциями", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
    }

    [Fact]
    public async Task PatchAccount_NonExistingAccount_ThrowsValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new PatchAccountRequestDto
        {
            Currency = "USD",
            Type = AccountType.Deposit,
            InterestRate = 1.5m,
            Balance = 2000
        };
        var command = new PatchAccountCommand
        {
            Id = id,
            Request = request
        };
        var validationFailure = new ValidationFailure("Id", "Счет не найден")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(It.Is<PatchAccountCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.PatchAccount(id, request));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Id", error.PropertyName);
        Assert.Equal("Счет не найден", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
    }

    [Fact]
    public async Task CreateTransaction_InsufficientFunds_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Type = TransactionType.Debit,
            Amount = 1000,
            Currency = "USD",
            Description = "Test"
        };
        var validationFailure = new ValidationFailure("Amount", "Недостаточно средств на счёте")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.CreateTransaction(command));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Amount", error.PropertyName);
        Assert.Equal("Недостаточно средств на счёте", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
        // Удалена проверка ErrorCode, так как валидатор не задаёт его
    }

    [Fact]
    public async Task PerformTransfer_DifferentCurrencies_ThrowsValidationException()
    {
        // Arrange
        var command = new PerformTransferCommand
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 500,
            Currency = "USD",
            Description = "Test"
        };
        var validationFailure = new ValidationFailure("Currency", "Валюты счетов не совпадают")
        {
            Severity = Severity.Error
        };
        var validationException = new ValidationException(new[] { validationFailure });
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ThrowsAsync(validationException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.PerformTransfer(command));
        Assert.Single(exception.Errors);
        var error = exception.Errors.First();
        Assert.Equal("Currency", error.PropertyName);
        Assert.Equal("Валюты счетов не совпадают", error.ErrorMessage);
        Assert.Equal(Severity.Error, error.Severity);
        // Удалена проверка ErrorCode, так как валидатор не задаёт его
    }
}