using AccountService.Data;
using AccountService.Features.Accounts;
using AccountService.Messaging.Events;
using MassTransit.Testing;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using AccountService.Messaging.Consumer;
using AccountService.Messaging.Events.Client;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using AccountService.Messaging;
using AccountService.Repositories;
using Microsoft.Extensions.Logging;
// ReSharper disable ConvertConstructorToMemberInitializers
// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable Xunit.XunitTestWithConsoleOutput

namespace AccountService.Tests
{
    public class OutboxPublishesAfterFailureTests : IAsyncLifetime
    {
        // ReSharper disable once IdentifierTypo
        private readonly PostgreSqlContainer _postgreSqlContainer;
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _client;
        private ITestHarness? _harness;
        private IServiceScope? _harnessScope;
        private CustomOutboxPublisherService? _outboxPublisher;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly CancellationTokenSource _cts = new();

        public OutboxPublishesAfterFailureTests()
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
            Console.WriteLine("[TEST-INIT] Starting PostgreSQL container...");
            await _postgreSqlContainer.StartAsync();
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test");
                    builder.ConfigureServices(services =>
                    {
                        var mtDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IBusRegistrationConfigurator));
                        if (mtDescriptor != null) services.Remove(mtDescriptor);
                        var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AccountDbContext>));
                        if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
                        services.AddDbContext<AccountDbContext>(options =>
                            options.UseNpgsql(_postgreSqlContainer.GetConnectionString() + ";Include Error Detail=true"));
                        services.AddHostedService<CustomOutboxPublisherService>(provider =>
                            new CustomOutboxPublisherService(
                                provider.GetRequiredService<IServiceScopeFactory>(),
                                provider.GetRequiredService<ILogger<CustomOutboxPublisherService>>()));
                        services.AddScoped<CustomOutboxPublisherService>();
                        services.AddMassTransitTestHarness(x =>
                        {
                            x.AddConsumer<StubConsumer>();
                            x.AddConsumer<ClientStatusConsumer>();
                            x.SetKebabCaseEndpointNameFormatter();
                            x.UsingInMemory((context, cfg) =>
                            {
                                cfg.ReceiveEndpoint("account.crm", e =>
                                {
                                    e.ConfigureConsumer<StubConsumer>(context);
                                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                                    e.PrefetchCount = 1;
                                    e.ConcurrentMessageLimit = 1;
                                });
                                cfg.ReceiveEndpoint("account.notifications", e =>
                                {
                                    e.ConfigureConsumer<StubConsumer>(context);
                                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                                    e.PrefetchCount = 1;
                                    e.ConcurrentMessageLimit = 1;
                                });
                                cfg.ReceiveEndpoint("antifraud.client.status", e =>
                                {
                                    e.ConfigureConsumer<ClientStatusConsumer>(context);
                                    e.UseMessageRetry(r => r.Immediate(1));
                                    e.PrefetchCount = 1;
                                    e.ConcurrentMessageLimit = 1;
                                    e.UseRawJsonSerializer();
                                });
                                cfg.ReceiveEndpoint("quarantine", e => { e.PrefetchCount = 1; });
                            });
                        });
                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = "Test";
                            options.DefaultChallengeScheme = "Test";
                        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                    });
                });
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
            Console.WriteLine("[TEST-INIT] Applying migrations...");
            await dbContext.Database.MigrateAsync(_cts.Token);
            var migrations = await dbContext.Database.GetAppliedMigrationsAsync(_cts.Token);
            Console.WriteLine($"[TEST-INIT] Migrations: {string.Join(", ", migrations)}");
            var tables = await dbContext.Database
                .SqlQueryRaw<string>("SELECT tablename FROM pg_tables WHERE schemaname = 'public'")
                .ToListAsync(_cts.Token);
            Console.WriteLine($"[TEST-INIT] Tables: {string.Join(", ", tables)}");
            if (!tables.Contains("OutboxMessages"))
            {
                Console.WriteLine("[TEST-INIT] Error: OutboxMessages table not found. Re-applying migrations...");
                await dbContext.Database.MigrateAsync(_cts.Token);
                tables = await dbContext.Database
                    .SqlQueryRaw<string>("SELECT tablename FROM pg_tables WHERE schemaname = 'public'")
                    .ToListAsync(_cts.Token);
                Console.WriteLine($"[TEST-INIT] Tables after re-apply: {string.Join(", ", tables)}");
            }
            await InitializeTestData(dbContext);
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
            using var scopePublisher = _factory.Services.CreateScope();
            _outboxPublisher = scopePublisher.ServiceProvider.GetRequiredService<CustomOutboxPublisherService>();
            _outboxPublisher.SetInitialized();
            _harnessScope = _factory.Services.CreateScope();
            _harness = _harnessScope.ServiceProvider.GetRequiredService<ITestHarness>();
            Console.WriteLine("[TEST-INIT] Starting MassTransit TestHarness...");
            await _harness.Start();
            Console.WriteLine("[TEST-INIT] InitializeAsync completed.");
        }

        public async Task DisposeAsync()
        {
            Console.WriteLine("[TEST-DISPOSE] Starting cleanup...");
            try
            {
                if (_outboxPublisher != null)
                {
                    Console.WriteLine("[TEST-DISPOSE] Stopping CustomOutboxPublisherService...");
                    _outboxPublisher.DisablePublishing();
                    await _outboxPublisher.StopAsync(_cts.Token);
                    await Task.Delay(1000, CancellationToken.None); // Дополнительная задержка для завершения фоновых задач
                }
                if (_harness != null)
                {
                    Console.WriteLine("[TEST-DISPOSE] Stopping MassTransit TestHarness...");
                    await _harness.Stop();
                }
                if (_harnessScope != null)
                {
                    Console.WriteLine("[TEST-DISPOSE] Disposing harness scope...");
                    _harnessScope.Dispose();
                }
                if (_client != null)
                {
                    Console.WriteLine("[TEST-DISPOSE] Disposing HttpClient...");
                    _client.Dispose();
                }
                if (_factory != null)
                {
                    Console.WriteLine("[TEST-DISPOSE] Disposing WebApplicationFactory...");
                    await _factory.DisposeAsync();
                }
                Console.WriteLine("[TEST-DISPOSE] Stopping PostgreSQL container...");
                await _postgreSqlContainer.StopAsync();
                await _postgreSqlContainer.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST-DISPOSE] Error during cleanup: {ex}");
            }
            finally
            {
                _cts.Dispose();
            }
            Console.WriteLine("[TEST-DISPOSE] Cleanup completed.");
        }

        private async Task InitializeTestData(AccountDbContext dbContext)
        {
            Console.WriteLine("[TEST-INIT] Truncating tables...");
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "TRUNCATE TABLE \"Accounts\" CASCADE; " +
                    "TRUNCATE TABLE \"Transactions\" CASCADE; " +
                    "TRUNCATE TABLE \"OutboxMessages\" CASCADE; " +
                    "TRUNCATE TABLE \"InboxConsumed\" CASCADE;", _cts.Token);
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                Console.WriteLine("[TEST-INIT] Warning: One or more tables do not exist. Ensuring migrations...");
                await dbContext.Database.MigrateAsync(_cts.Token);
            }
            var account = new Account
            {
                AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Balance = 10000.00m,
                AccountType = AccountType.Deposit,
                Currency = "USD",
                InterestRate = 0.05m,
                OpeningDate = DateTime.UtcNow,
                IsFrozen = false
            };
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync(_cts.Token);
            Console.WriteLine("[TEST-INIT] Test data initialized.");
        }

        [Fact]
        public async Task ProcessOutboxAsync_PublishesAndMarksMessageAsSent()
        {
            // Arrange
            using var scope = _factory!.Services.CreateScope();

            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var publisher = scope.ServiceProvider.GetRequiredService<CustomOutboxPublisherService>();

            // Инициализируем сервис прямо здесь
            publisher.SetInitialized();

            var evt = new MoneyDebitedEvent
            {
                EventId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                OperationId = Guid.NewGuid(),
                Amount = 50,
                Currency = "USD",
                OccurredAt = DateTime.UtcNow,
                Meta = new MetaData
                {
                    Version = "v1",
                    Source = "account-service",
                    CorrelationId = Guid.NewGuid(),
                    CausationId = Guid.NewGuid()
                }
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "MoneyDebited",
                Payload = JsonSerializer.Serialize(evt, _jsonOptions),
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            await outboxRepo.AddAsync(outboxMessage);
          

            // Act
            await publisher.ProcessOutboxAsync(_cts.Token);

            // Assert
            OutboxMessage? updated = null;
            for (var i = 0; i < 30; i++)
            {
                updated = await scope.ServiceProvider.GetRequiredService<AccountDbContext>()
                    .OutboxMessages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == outboxMessage.Id, _cts.Token);

               
                if (updated?.SentAt != null)
                    break;

                await Task.Delay(1000, _cts.Token);
            }

            updated.Should().NotBeNull();
            updated.SentAt.Should().NotBeNull();

            bool consumed = false;
            for (int i = 0; i < 30; i++)
            {
                consumed = await _harness!.Consumed.Any<MoneyDebitedEvent>(x => x.Context.Message.AccountId == evt.AccountId);
                Console.WriteLine($"[TEST] Check {i + 1}: Consumed={consumed}");
                if (consumed) break;

                await Task.Delay(1000, _cts.Token);
            }

            consumed.Should().BeTrue();
        }

        [Fact]
        public async Task ClientBlockedPreventsDebit()
        {
            // Arrange
            var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var clientBlockedEvent = new ClientBlockedEvent
            {
                ClientId = ownerId,
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                Meta = new MetaData
                {
                    Version = "v1",
                    Source = "account-service",
                    CorrelationId = Guid.NewGuid(),
                    CausationId = Guid.NewGuid()
                }
            };
            await _harness!.Bus.Publish(clientBlockedEvent, _cts.Token);
            bool consumed = false;
            for (int i = 0; i < 20; i++)
            {
                consumed = await _harness.Consumed.Any<ClientBlockedEvent>(x =>
                    x.Context.Message.ClientId == ownerId);
                if (consumed) break;
                await Task.Delay(500, _cts.Token);
            }
            consumed.Should().BeTrue();
            using var scope = _factory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
            var account = await dbContext.Accounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == accountId, _cts.Token);
            account.Should().NotBeNull();
            account.IsFrozen.Should().BeTrue();
            var response = await _client!.PostAsJsonAsync("/api/Transaction", new
            {
                AccountId = accountId,
                Amount = 100.00m,
                Currency = "USD",
                Type = "Debit",
                Description = "Test"
            }, _jsonOptions, _cts.Token);
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            using var scope2 = _factory.Services.CreateScope();
            var outboxCount = await scope2.ServiceProvider.GetRequiredService<AccountDbContext>().OutboxMessages
                .Where(m => m.EventType == "MoneyDebited")
                .CountAsync(_cts.Token);
            outboxCount.Should().Be(0);
        }
    }


}


