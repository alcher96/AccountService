using AccountService.Messaging;
using AccountService.Messaging.Events;
using AccountService.Messaging.Events.Client;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Utility
{
    /// <summary>
    /// структура для схем в свагере
    /// </summary>
    public class EventSchemasFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(MetaData))
            {
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["correlationId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["causationId"] = new OpenApiSchema { Type = "string", Format = "uuid" }
                };
                return;
            }

            if (context.Type == typeof(AccountOpenedEvent))
            {
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["eventId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["occurredAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                    ["accountId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["ownerId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["currency"] = new OpenApiSchema { Type = "string" },
                    ["type"] = new OpenApiSchema { Type = "string" },
                    ["meta"] = context.SchemaGenerator.GenerateSchema(typeof(MetaData), context.SchemaRepository)
                };
            }

            if (context.Type == typeof(MoneyCreditedEvent) || context.Type == typeof(MoneyDebitedEvent))
            {
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["eventId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["occurredAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                    ["accountId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["amount"] = new OpenApiSchema { Type = "number", Format = "decimal" },
                    ["currency"] = new OpenApiSchema { Type = "string" },
                    ["operationId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["reason"] = new OpenApiSchema { Type = "string", Nullable = true },
                    ["meta"] = context.SchemaGenerator.GenerateSchema(typeof(MetaData), context.SchemaRepository)
                };
            }

            if (context.Type == typeof(InterestAccruedEvent))
            {
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["eventId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["occurredAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                    ["accountId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["periodFrom"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                    ["periodTo"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                    ["amount"] = new OpenApiSchema { Type = "number", Format = "decimal" },
                    ["meta"] = context.SchemaGenerator.GenerateSchema(typeof(MetaData), context.SchemaRepository)
                };
            }

            if (context.Type == typeof(TransferCompletedEvent))
            {
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["eventId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["occurredAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                    ["sourceAccountId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["destinationAccountId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["amount"] = new OpenApiSchema { Type = "number", Format = "decimal" },
                    ["currency"] = new OpenApiSchema { Type = "string" },
                    ["transferId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["meta"] = context.SchemaGenerator.GenerateSchema(typeof(MetaData), context.SchemaRepository)
                };
            }

            if (context.Type == typeof(ClientBlockedEvent) || context.Type == typeof(ClientUnblockedEvent))
            {
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["eventId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["occurredAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                    ["clientId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                    ["meta"] = context.SchemaGenerator.GenerateSchema(typeof(MetaData), context.SchemaRepository)
                };
            }
        }
    }
}
