using Account_Service.Accounts;
using Account_Service.Accounts.AddAccount.Command;
using Account_Service.Accounts.GetAccount.Query;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Account_Service.Accounts.UpdateAccount.Command;
using Account_Service.Accounts.PatchAccount.Command;
using Account_Service;

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
            Currency = "USD",
            AccountType = AccountType.Deposit,
            InterestRate = 1.5m
        };
        var accountDto = new AccountDto
        {
            AccountId = Guid.NewGuid(),
            OwnerId = command.OwnerId,
            Currency = command.Currency,
            AccountType = command.AccountType,
            InterestRate = command.InterestRate,
            Balance = 0,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(It.Is<CreateAccountCommand>(c => c.OwnerId == command.OwnerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Success(accountDto));

        // Act
        var result = await _controller.CreateAccount(command);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(AccountController.GetAccountById), createdResult.ActionName);
        Assert.Equal(accountDto.AccountId, createdResult.RouteValues["id"]);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(createdResult.Value);
        Assert.True(mbResult.IsSuccess);
        Assert.Equal(accountDto, mbResult.Value);
    }

    [Fact]
    public async Task CreateAccount_InvalidCurrency_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            Currency = "XYZ",
            AccountType = AccountType.Deposit,
            InterestRate = 1.5m
        };
        var validationErrors = new Dictionary<string, string[]> { { "Currency", new[] { "Валюта не поддерживается" } } };
        _mediatorMock.Setup(m => m.Send(It.Is<CreateAccountCommand>(c => c.OwnerId == command.OwnerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Failure(validationErrors));

        // Act
        var result = await _controller.CreateAccount(command);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(badRequestResult.Value);
        Assert.False(mbResult.IsSuccess);
        Assert.Equal("Validation failed", mbResult.Error);
        Assert.Contains("Currency", mbResult.ValidationErrors);
        Assert.Equal("Валюта не поддерживается", mbResult.ValidationErrors["Currency"][0]);
    }

    [Fact]
    public async Task GetAccountById_ValidId_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountByIdQuery { Id = accountId };
        var accountDto = new AccountDto
        {
            AccountId = accountId,
            OwnerId = Guid.NewGuid(),
            Currency = "USD",
            AccountType = AccountType.Deposit,
            InterestRate = 1.5m,
            Balance = 1000,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountByIdQuery>(q => q.Id == accountId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Success(accountDto));

        // Act
        var result = await _controller.GetAccountById(accountId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(okResult.Value);
        Assert.True(mbResult.IsSuccess);
        Assert.Equal(accountDto, mbResult.Value);
    }

    [Fact]
    public async Task GetAccountById_NonExistingId_ReturnsBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountByIdQuery { Id = accountId };
        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountByIdQuery>(q => q.Id == accountId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Failure("Счёт не найден"));

        // Act
        var result = await _controller.GetAccountById(accountId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(badRequestResult.Value);
        Assert.False(mbResult.IsSuccess);
        Assert.Equal("Счёт не найден", mbResult.Error);
    }

    [Fact]
    public async Task GetAccounts_ValidQuery_ReturnsOkResult()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var query = new GetAccountsQuery { OwnerId = ownerId, Type = AccountType.Deposit };
        var accounts = new List<AccountDto>
        {
            new AccountDto
            {
                AccountId = Guid.NewGuid(),
                OwnerId = ownerId,
                Currency = "USD",
                AccountType = AccountType.Deposit,
                InterestRate = 1.5m,
                Balance = 1000,
                OpeningDate = DateTime.UtcNow
            }
        };
        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountsQuery>(q => q.OwnerId == ownerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<List<AccountDto>>.Success(accounts));

        // Act
        var result = await _controller.GetAccounts(ownerId, AccountType.Deposit);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<List<AccountDto>>>(okResult.Value);
        Assert.True(mbResult.IsSuccess);
        Assert.Equal(accounts, mbResult.Value);
    }

    [Fact]
    public async Task GetAccounts_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var query = new GetAccountsQuery { OwnerId = ownerId, Type = (AccountType)999 };
        var validationErrors = new Dictionary<string, string[]> { { "Type", new[] { "Недопустимый тип счёта" } } };
        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountsQuery>(q => q.OwnerId == ownerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<List<AccountDto>>.Failure(validationErrors));

        // Act
        var result = await _controller.GetAccounts(ownerId, (AccountType)999);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<List<AccountDto>>>(badRequestResult.Value);
        Assert.False(mbResult.IsSuccess);
        Assert.Equal("Validation failed", mbResult.Error);
        Assert.Contains("Type", mbResult.ValidationErrors);
        Assert.Equal("Недопустимый тип счёта", mbResult.ValidationErrors["Type"][0]);
    }

    [Fact]
    public async Task PatchAccount_ValidCommand_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new PatchAccountCommand
        {
            Id = accountId,
            Request = new PatchAccountRequestDto { Currency = "USD", Type = AccountType.Deposit, InterestRate = 2.0m }
        };
        var accountDto = new AccountDto
        {
            AccountId = accountId,
            OwnerId = Guid.NewGuid(),
            Currency = "USD",
            AccountType = AccountType.Deposit,
            InterestRate = 2.0m,
            Balance = 1000,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(It.Is<PatchAccountCommand>(c => c.Id == accountId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Success(accountDto));

        // Act
        var result = await _controller.PatchAccount(accountId, command.Request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(okResult.Value);
        Assert.True(mbResult.IsSuccess);
        Assert.Equal(accountDto, mbResult.Value);
    }

    [Fact]
    public async Task PatchAccount_NonExistingAccount_ReturnsBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new PatchAccountCommand
        {
            Id = accountId,
            Request = new PatchAccountRequestDto { Currency = "USD", Type = AccountType.Deposit, InterestRate = 2.0m }
        };
        _mediatorMock.Setup(m => m.Send(It.Is<PatchAccountCommand>(c => c.Id == accountId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Failure("Счёт не найден"));

        // Act
        var result = await _controller.PatchAccount(accountId, command.Request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(badRequestResult.Value);
        Assert.False(mbResult.IsSuccess);
        Assert.Equal("Счёт не найден", mbResult.Error);
    }

    [Fact]
    public async Task UpdateAccount_ValidCommand_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new UpdateAccountCommand
        {
            Id = accountId,
            Request = new UpdateAccountRequestDto { Currency = "USD", Type = AccountType.Deposit, InterestRate = 2.0m }
        };
        var accountDto = new AccountDto
        {
            AccountId = accountId,
            OwnerId = Guid.NewGuid(),
            Currency = "USD",
            AccountType = AccountType.Deposit,
            InterestRate = 2.0m,
            Balance = 1000,
            OpeningDate = DateTime.UtcNow
        };
        _mediatorMock.Setup(m => m.Send(It.Is<UpdateAccountCommand>(c => c.Id == accountId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Success(accountDto));

        // Act
        var result = await _controller.UpdateAccount(accountId, command.Request);

        // Act
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(okResult.Value);
        Assert.True(mbResult.IsSuccess);
        Assert.Equal(accountDto, mbResult.Value);
    }

    [Fact]
    public async Task UpdateAccount_NonExistingAccount_ReturnsBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new UpdateAccountCommand
        {
            Id = accountId,
            Request = new UpdateAccountRequestDto { Currency = "USD", Type = AccountType.Deposit, InterestRate = 2.0m }
        };
        _mediatorMock.Setup(m => m.Send(It.Is<UpdateAccountCommand>(c => c.Id == accountId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<AccountDto>.Failure("Счёт не найден"));

        // Act
        var result = await _controller.UpdateAccount(accountId, command.Request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var mbResult = Assert.IsType<MbResult<AccountDto>>(badRequestResult.Value);
        Assert.False(mbResult.IsSuccess);
        Assert.Equal("Счёт не найден", mbResult.Error);
    }
}