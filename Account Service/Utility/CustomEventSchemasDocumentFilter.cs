using AccountService.Messaging.Events.Client;
using AccountService.Messaging.Events;
using AccountService.Messaging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Utility
{
    /// <summary>
    /// кастомные схемы для свагера
    /// </summary>
    public class CustomEventSchemasDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Список типов событий для документирования
            var eventTypes = new[]
            {
                typeof(AccountOpenedEvent),
                typeof(MoneyCreditedEvent),
                typeof(MoneyDebitedEvent),
                typeof(InterestAccruedEvent),
                typeof(TransferCompletedEvent),
                typeof(ClientBlockedEvent),
                typeof(ClientUnblockedEvent),
                typeof(MetaData)
            };

            foreach (var eventType in eventTypes)
            {
                // Генерируем схему для каждого типа
                var schema = context.SchemaGenerator.GenerateSchema(eventType, context.SchemaRepository);
                // Добавляем схему в документ, если она еще не добавлена
                if (!swaggerDoc.Components.Schemas.ContainsKey(eventType.Name))
                {
                    swaggerDoc.Components.Schemas.Add(eventType.Name, schema);
                }
            }
        }
    }
}
