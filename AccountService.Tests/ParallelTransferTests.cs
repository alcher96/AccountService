using AccountService.Data;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json.Serialization;
using System.Text.Json;
using AccountService.Features.Accounts;
// ReSharper disable Xunit.XunitTestWithConsoleOutput
// ReSharper disable AccessToModifiedClosure
// ReSharper disable IdentifierTypo
// ReSharper disable ConvertConstructorToMemberInitializers

namespace AccountService.Tests
{
    public class ParallelTransferTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgreSqlContainer;
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;
        private readonly JsonSerializerOptions _jsonOptions;

        public ParallelTransferTests()
        {
            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("account_service_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            _jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task InitializeAsync()
        {
            await _postgreSqlContainer.StartAsync();
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test"); // Задаём тестовое окружение
                    builder.ConfigureServices(services =>
                    {
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AccountDbContext>));
                        if (descriptor != null)
                            services.Remove(descriptor);

                        services.AddDbContext<AccountDbContext>(options =>
                            options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));

                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = "Test";
                            options.DefaultChallengeScheme = "Test";
                        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                    });
                });
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
            await dbContext.Database.MigrateAsync();
            await InitializeTestData(dbContext);
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            await _factory.DisposeAsync();
            await _postgreSqlContainer.StopAsync();
            await _postgreSqlContainer.DisposeAsync();
        }

        private async Task InitializeTestData(AccountDbContext dbContext)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"Accounts\" CASCADE; TRUNCATE TABLE \"Transactions\" CASCADE;");
            var account1 = new Account
            {
                AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Balance = 10000.00m,
                AccountType = AccountType.Deposit,
                Currency = "USD",
                InterestRate = 0.05m,
                OpeningDate = DateTime.UtcNow,
            };
            var account2 = new Account
            {
                AccountId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Balance = 5000.00m,
                AccountType = AccountType.Deposit,
                Currency = "USD",
                InterestRate = 0.05m,
                OpeningDate = DateTime.UtcNow,
            };
            dbContext.Accounts.AddRange(account1, account2);
            try
            {
                var saved = await dbContext.SaveChangesAsync();
                Console.WriteLine($"[TEST-INIT] Сохранено объектов: {saved}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST-INIT] Ошибка при сохранении аккаунтов: {ex}");
                throw;
            }

            var accountsFromDb = dbContext.Accounts.ToList();
            Console.WriteLine("[TEST-INIT] Список аккаунтов в БД:");
            foreach (var acc in accountsFromDb)
            {
                Console.WriteLine(
                    $" ID: {acc.AccountId} | Owner: {acc.OwnerId} | Balance: {acc.Balance} | " +
                    $"Type: {acc.AccountType} | Currency: {acc.Currency} | " +
                    $"OpeningDate: {acc.OpeningDate:O} | ClosingDate: {acc.ClosingDate?.ToString("O") ?? "null"}"
                );
            }
        }

        [Fact]
        public async Task ParallelTransfer_ShouldPreserveTotalBalance()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            // Arrange
            var response = await _client.GetAsync("api/Account");
            Console.WriteLine($"[TEST] GET /api/Account Status: {response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TEST] GET /api/Account Body: {responseBody}");
            var result = await _client.GetFromJsonAsync<MbResult<List<AccountDto>>>("api/Account", _jsonOptions);
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            var initialAccounts = result.Value;
            Console.WriteLine("[TEST] Начальное состояние счетов:");
            foreach (var acc in initialAccounts)
            {
                Console.WriteLine($" {acc.AccountId} | Balance: {acc.Balance} {acc.Currency}");
            }
            int transferCount = 50;
            var tasks = new List<Task>();
            int successfulTransfers = 0;
            int conflictTransfers = 0;
            Console.WriteLine($"[TEST] Запуск {transferCount} параллельных переводов...");
            for (int i = 0; i < transferCount; i++)
            {
                var request = new
                {
                    FromAccountId = initialAccounts[0].AccountId,
                    ToAccountId = initialAccounts[1].AccountId,
                    Amount = 100.00m,
                    Currency = "USD",
                    Description = $"Transfer #{i + 1}"
                };
                Console.WriteLine(
                    $"[TEST] Sending transfer request: FromAccountId={request.FromAccountId}, ToAccountId={request.ToAccountId}, Amount={request.Amount}");
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(i * 10); // небольшая задержка для стабильноости
                        var postAsJsonAsync = await _client.PostAsJsonAsync("api/Transaction/transfers", request, _jsonOptions);
                        var body = await postAsJsonAsync.Content.ReadAsStringAsync();
                        Console.WriteLine($"[TRANSFER #{i + 1}] Status={postAsJsonAsync.StatusCode}");
                        if (!string.IsNullOrWhiteSpace(body))
                            Console.WriteLine($"[TRANSFER #{i + 1}] Body: {body}");
                        if (postAsJsonAsync.IsSuccessStatusCode)
                        {
                            Interlocked.Increment(ref successfulTransfers);
                        }
                        else switch (postAsJsonAsync.StatusCode)
                        {
                            case HttpStatusCode.Conflict:
                            {
                                Interlocked.Increment(ref conflictTransfers);
                                var errorResponse = JsonSerializer.Deserialize<MbResult<object>>(body, _jsonOptions);
                                errorResponse.Should().NotBeNull();
                                errorResponse.MbError.Should().Be("Concurrency conflict", $"Transfer #{i + 1} should return 'Concurrency conflict' for HTTP 409");
                                Console.WriteLine($"[TRANSFER #{i + 1}] Conflict: {errorResponse.MbError}");
                                break;
                            }
                            case HttpStatusCode.BadRequest:
                            {
                                var errorResponse = JsonSerializer.Deserialize<MbResult<object>>(body, _jsonOptions);
                                Console.WriteLine($"[TRANSFER #{i + 1}] Error: {errorResponse?.MbError}");
                                if (errorResponse?.ValidationErrors != null)
                                {
                                    foreach (var error in errorResponse.ValidationErrors)
                                    {
                                        Console.WriteLine(
                                            $"[TRANSFER #{i + 1}] Validation Error: {error.Key} - {string.Join(", ", error.Value)}");
                                    }
                                }

                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TRANSFER #{i + 1}] Exception: {ex}");
                    }
                }));
            }
            // Act
            await Task.WhenAll(tasks);
            // Assert
            Console.WriteLine($"[TEST] Успешных переводов: {successfulTransfers} из {transferCount}");
            Console.WriteLine($"[TEST] Конфликтных переводов: {conflictTransfers}");
            var resultAccountsResponse = await _client.GetAsync("api/Account");
            Console.WriteLine($"[TEST] GET /api/Account (final) Status: {resultAccountsResponse.StatusCode}");
            var resultAccountsBody = await resultAccountsResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[TEST] GET /api/Account (final) Body: {resultAccountsBody}");
            var resultAccounts = await _client.GetFromJsonAsync<MbResult<List<AccountDto>>>("api/Account", _jsonOptions);
            resultAccounts.Should().NotBeNull();
            resultAccounts.IsSuccess.Should().BeTrue();
            resultAccounts.Value.Should().NotBeNull();
            var finalAccounts = resultAccounts.Value;
            Console.WriteLine("[TEST] Итоговое состояние счетов:");
            foreach (var acc in finalAccounts)
            {
                Console.WriteLine($" {acc.AccountId} | Balance: {acc.Balance} {acc.Currency}");
            }
            var totalInitial = initialAccounts.Sum(a => a.Balance);
            var totalFinal = finalAccounts.Sum(a => a.Balance);
            totalFinal.Should().Be(totalInitial, "Суммарный баланс должен остаться неизменным");
            successfulTransfers.Should().BeGreaterThan(0, "Ожидалось хотя бы одно успешное выполнение перевода");
            conflictTransfers.Should().BeGreaterThan(0, "Ожидался хотя бы один конфликтный перевод");
        }
    }
}

