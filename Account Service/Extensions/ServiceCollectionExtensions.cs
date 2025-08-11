using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member избыточный xml комментарий

namespace AccountService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = "http://keycloak:8080/realms/account-service";
                options.RequireHttpsMetadata = false;
                options.Audience = "account";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "http://keycloak:8080/realms/account-service",
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
                        {
                            var realmAccessClaim = context.Principal.FindFirst("realm_access");
                            if (realmAccessClaim != null)
                            {
                                var realmAccess = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(realmAccessClaim.Value);
                                if (realmAccess != null && realmAccess.TryGetValue("roles", out var roles))
                                {
                                    foreach (var role in roles)
                                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = _ => Task.CompletedTask
                };
            });

            return services;
        }

        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Account Service API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Введите 'Bearer' пробел и токен. Например: 'Bearer eyJhbGci...'"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                    
            });

            return services;
        }
    }
}
