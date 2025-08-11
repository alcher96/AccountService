using AccountService.Features.Accounts;
using AccountService.Features.Accounts.AddAccount.Command;
using AccountService.Features.Accounts.GetAccount.Query;
using AccountService.Features.Accounts.PatchAccount.Command;
using AccountService.Features.Accounts.UpdateAccount.Command;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AccountService.Tests
{

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
            Assert.Equal(accountDto.AccountId, createdResult.RouteValues!["id"]);
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
            var validationErrors = new Dictionary<string, string[]> { { "Currency", ["Валюта не поддерживается"] } };
            _mediatorMock.Setup(m => m.Send(It.Is<CreateAccountCommand>(c => c.OwnerId == command.OwnerId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<AccountDto>.Failure(validationErrors));

            // Act
            var result = await _controller.CreateAccount(command);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var mbResult = Assert.IsType<MbResult<AccountDto>>(badRequestResult.Value);
            Assert.False(mbResult.IsSuccess);
            Assert.Equal("Validation failed", mbResult.MbError);
            Assert.Contains("Currency", mbResult.ValidationErrors!);
            Assert.Equal("Валюта не поддерживается", mbResult.ValidationErrors!["Currency"][0]);
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
            _mediatorMock.Setup(m => m.Send(It.Is<GetAccountByIdQuery>(q => q.Id == accountId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<AccountDto>.Failure("Счёт не найден"));

            // Act
            var result = await _controller.GetAccountById(accountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var mbResult = Assert.IsType<MbResult<AccountDto>>(badRequestResult.Value);
            Assert.False(mbResult.IsSuccess);
            Assert.Equal("Счёт не найден", mbResult.MbError);
        }

        [Fact]
        public async Task GetAccounts_ValidQuery_ReturnsOkResult()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var accounts = new List<AccountDto>
        {
            new()
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
            var validationErrors = new Dictionary<string, string[]> { { "Type", ["Недопустимый тип счёта"] } };
            _mediatorMock.Setup(m => m.Send(It.Is<GetAccountsQuery>(q => q.OwnerId == ownerId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<List<AccountDto>>.Failure(validationErrors));

            // Act
            var result = await _controller.GetAccounts(ownerId, (AccountType)999);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var mbResult = Assert.IsType<MbResult<List<AccountDto>>>(badRequestResult.Value);
            Assert.False(mbResult.IsSuccess);
            Assert.Equal("Validation failed", mbResult.MbError);
            Assert.Contains("Type", mbResult.ValidationErrors!);
            Assert.Equal("Недопустимый тип счёта", mbResult.ValidationErrors!["Type"][0]);
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
            Assert.Equal("Счёт не найден", mbResult.MbError);
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
            Assert.Equal("Счёт не найден", mbResult.MbError);
        }

    }



}
