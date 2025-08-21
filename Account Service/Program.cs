using AccountService;
using AccountService.Extensions;
using AccountService.Repositories;
using FluentValidation;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;
using AccountService.Data;
using AccountService.Utility;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.PostgreSql;
using AccountService.Messaging;
using Serilog.Events;
using Serilog;
// ReSharper disable StringLiteralTypo

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddJwtAuthentication();
builder.Services.AddSwaggerGen(c =>
{
    c.SchemaFilter<EventSchemasFilter>();
    // явно включаем схемы событий
    c.DocumentFilter<CustomEventSchemasDocumentFilter>();
});
builder.Services.AddSwaggerConfiguration();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new PostgreSqlStorageOptions
    {
        PrepareSchemaIfNecessary = true //
    }));
#pragma warning restore CS0618 // Type or member is obsolete

// Register Hangfire server only if not in test environment
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHangfireServer();
}

// MassTransit настройка
builder.Services.AddCustomMassTransit();

// ƒобавл€ем кастомный publisher как background service
builder.Services.AddHostedService<CustomOutboxPublisherService>();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API v1");
        options.RoutePrefix = string.Empty;
    });
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AllowAnonymousAuthorizationFilter() }
    });
}

app.UseMiddleware<AuthenticationErrorMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
HangfireJobConfiguration.ConfigureJobs(app);
app.Run();
