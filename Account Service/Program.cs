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

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddJwtAuthentication();
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


builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
    }));

// Register Hangfire server only if not in test environment
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHangfireServer();
}

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
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
HangfireJobConfiguration.ConfigureJobs(app);
app.Run();
