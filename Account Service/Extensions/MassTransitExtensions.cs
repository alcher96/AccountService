using AccountService.Messaging.Consumer;
using AccountService.Messaging.Events;
using AccountService.Messaging.Events.Client;
using MassTransit;
using RabbitMQ.Client;
// ReSharper disable StringLiteralTypo
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Extensions
{
    /// <summary>
    /// настройка masstransit
    /// </summary>
    public static class MassTransitExtensions
    {
        public static IServiceCollection AddCustomMassTransit(this IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                x.AddConsumer<StubConsumer>();
                x.AddConsumer<ClientStatusConsumer>();

                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri("rabbitmq://rabbitmq"), h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    // Общие exchange для событий
                    cfg.Message<AccountOpenedEvent>(e => e.SetEntityName("account.events"));
                    cfg.Message<MoneyCreditedEvent>(e => e.SetEntityName("account.events"));
                    cfg.Message<MoneyDebitedEvent>(e => e.SetEntityName("account.events"));
                    cfg.Message<InterestAccruedEvent>(e => e.SetEntityName("account.events"));
                    cfg.Message<ClientBlockedEvent>(e => e.SetEntityName("account.events"));
                    cfg.Message<ClientUnblockedEvent>(e => e.SetEntityName("account.events"));

                    // Публикация в topic exchange
                    cfg.Publish<AccountOpenedEvent>(e => { e.ExchangeType = ExchangeType.Topic; e.Durable = true; });
                    cfg.Publish<MoneyCreditedEvent>(e => { e.ExchangeType = ExchangeType.Topic; e.Durable = true; });
                    cfg.Publish<MoneyDebitedEvent>(e => { e.ExchangeType = ExchangeType.Topic; e.Durable = true; });
                    cfg.Publish<InterestAccruedEvent>(e => { e.ExchangeType = ExchangeType.Topic; e.Durable = true; });
                    cfg.Publish<ClientBlockedEvent>(e => { e.ExchangeType = ExchangeType.Topic; e.Durable = true; });
                    cfg.Publish<ClientUnblockedEvent>(e => { e.ExchangeType = ExchangeType.Topic; e.Durable = true; });

                    // Консьюмеры
                    cfg.ReceiveEndpoint("account.crm", e =>
                    {
                        e.Bind("account.events", b =>
                        {
                            b.RoutingKey = "account.*";
                            b.ExchangeType = ExchangeType.Topic;
                        });
                        e.ConfigureConsumer<StubConsumer>(context);
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        e.PrefetchCount = 1;
                        e.ConfigureConsumeTopology = false;
                    });

                    cfg.ReceiveEndpoint("account.notifications", e =>
                    {
                        e.Bind("account.events", b =>
                        {
                            b.RoutingKey = "money.*";
                            b.ExchangeType = ExchangeType.Topic;
                        });
                        e.ConfigureConsumer<StubConsumer>(context);
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        e.PrefetchCount = 1;
                        e.ConfigureConsumeTopology = false;
                    });

                    cfg.ReceiveEndpoint("antifraud.client.status", e =>
                    {
                        e.Bind("account.events", b =>
                        {
                            b.RoutingKey = "client.*";
                            b.ExchangeType = ExchangeType.Topic;
                        });
                        e.ConfigureConsumer<ClientStatusConsumer>(context);
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        e.PrefetchCount = 1;
                        e.ConfigureConsumeTopology = false;
                        e.ConcurrentMessageLimit = 1;
                        e.UseRawJsonSerializer();
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
