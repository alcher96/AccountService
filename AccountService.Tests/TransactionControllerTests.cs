using Account_Service.Accounts;
using Account_Service.Transactions;
using Account_Service.Transactions.AddTransaction.Command;
using Account_Service.Transactions.GetAccountTransactions.Query;
using Account_Service.Transactions.PerformTransfer.Command;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentValidation.Results;
using Account_Service;

namespace AccountService.Tests
{
    public class TransactionControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new TransactionController(_mediatorMock.Object);
        }

        [Fact]
        public async Task CreateTransaction_ValidCommand_ReturnsCreatedResult()
        {
            // Arrange
            var command = new CreateTransactionCommand
            {
                AccountId = Guid.NewGuid(),
                Type = TransactionType.Debit,
                Amount = 1000,
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
            _mediatorMock.Setup(m => m.Send(It.Is<CreateTransactionCommand>(c => c.AccountId == command.AccountId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<TransactionDto>.Success(transactionDto));

            // Act
            var result = await _controller.CreateTransaction(command);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
            Assert.Equal(nameof(AccountController.GetAccountById), createdResult.ActionName);
            Assert.Equal("Account", createdResult.RouteValues?["controller"]);
            Assert.Equal(command.AccountId, createdResult.RouteValues?["id"]);
            var mbResult = Assert.IsType<MbResult<TransactionDto>>(createdResult.Value);
            Assert.True(mbResult.IsSuccess);
            Assert.Equal(transactionDto, mbResult.Value);
        }

        [Fact]
        public async Task CreateTransaction_InsufficientFunds_ReturnsBadRequest()
        {
            // Arrange
            var command = new CreateTransactionCommand
            {
                AccountId = Guid.NewGuid(),
                Type = TransactionType.Credit,
                Amount = 1000,
                Currency = "USD",
                Description = "Test withdrawal"
            };
            var validationErrors = new Dictionary<string, string[]>
                { { "Amount", new[] { "Недостаточно средств на счёте" } } };
            _mediatorMock.Setup(m => m.Send(It.Is<CreateTransactionCommand>(c => c.AccountId == command.AccountId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<TransactionDto>.Failure(validationErrors));

            // Act
            var result = await _controller.CreateTransaction(command);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var mbResult = Assert.IsType<MbResult<TransactionDto>>(badRequestResult.Value);
            Assert.False(mbResult.IsSuccess);
            Assert.Equal("Validation failed", mbResult.Error);
            Assert.Contains("Amount", mbResult.ValidationErrors);
            Assert.Equal("Недостаточно средств на счёте", mbResult.ValidationErrors["Amount"][0]);
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
                    Type = TransactionType.Credit,
                    Amount = command.Amount,
                    Currency = command.Currency,
                    Description = command.Description,
                    DateTime = DateTime.UtcNow
                },
                new TransactionDto
                {
                    TransactionId = Guid.NewGuid(),
                    AccountId = command.ToAccountId,
                    Type = TransactionType.Debit,
                    Amount = command.Amount,
                    Currency = command.Currency,
                    Description = command.Description,
                    DateTime = DateTime.UtcNow
                }
            };
            _mediatorMock.Setup(m =>
                    m.Send(It.Is<PerformTransferCommand>(c => c.FromAccountId == command.FromAccountId),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<TransactionDto[]>.Success(transactions));

            // Act
            var result = await _controller.PerformTransfer(command);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
            Assert.Equal(nameof(AccountController.GetAccountById), createdResult.ActionName);
            Assert.Equal("Account", createdResult.RouteValues?["controller"]);
            Assert.Equal(command.FromAccountId, createdResult.RouteValues?["id"]);
            var mbResult = Assert.IsType<MbResult<TransactionDto[]>>(createdResult.Value);
            Assert.True(mbResult.IsSuccess);
            Assert.Equal(transactions, mbResult.Value);
        }

        [Fact]
        public async Task PerformTransfer_DifferentCurrencies_ReturnsBadRequest()
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
            var validationErrors = new Dictionary<string, string[]>
                { { "Currency", new[] { "Валюты счетов не совпадают" } } };
            _mediatorMock.Setup(m =>
                    m.Send(It.Is<PerformTransferCommand>(c => c.FromAccountId == command.FromAccountId),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<TransactionDto[]>.Failure(validationErrors));

            // Act
            var result = await _controller.PerformTransfer(command);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var mbResult = Assert.IsType<MbResult<TransactionDto[]>>(badRequestResult.Value);
            Assert.False(mbResult.IsSuccess);
            Assert.Equal("Validation failed", mbResult.Error);
            Assert.Contains("Currency", mbResult.ValidationErrors);
            Assert.Equal("Валюты счетов не совпадают", mbResult.ValidationErrors["Currency"][0]);
        }

        [Fact]
        public async Task GetAccountTransactions_ValidQuery_ReturnsOkResult()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    TransactionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Type = TransactionType.Debit,
                    Amount = 1000,
                    Currency = "USD",
                    Description = "Test deposit",
                    DateTime = DateTime.UtcNow
                }
            };
            _mediatorMock.Setup(m => m.Send(It.Is<GetAccountTransactionsQuery>(q => q.AccountId == accountId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<List<TransactionDto>>.Success(transactions));

            // Act
            var result = await _controller.GetAccountTransactions(accountId, startDate, endDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var mbResult = Assert.IsType<MbResult<List<TransactionDto>>>(okResult.Value);
            Assert.True(mbResult.IsSuccess);
            Assert.Equal(transactions, mbResult.Value);
        }

        [Fact]
        public async Task GetAccountTransactions_InvalidDates_ReturnsBadRequest()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddDays(-1);
            var validationErrors = new Dictionary<string, string[]>
                { { "EndDate", new[] { "Конечная дата не может быть раньше начальной" } } };
            _mediatorMock.Setup(m => m.Send(It.Is<GetAccountTransactionsQuery>(q => q.AccountId == accountId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(MbResult<List<TransactionDto>>.Failure(validationErrors));

            // Act
            var result = await _controller.GetAccountTransactions(accountId, startDate, endDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var mbResult = Assert.IsType<MbResult<List<TransactionDto>>>(badRequestResult.Value);
            Assert.False(mbResult.IsSuccess);
            Assert.Equal("Validation failed", mbResult.Error);
            Assert.Contains("EndDate", mbResult.ValidationErrors);
            Assert.Equal("Конечная дата не может быть раньше начальной", mbResult.ValidationErrors["EndDate"][0]);
        }
    }
}
