using System.Text;
using AutoMapper;
using MedMateAI.Application.Service;
using MedMateAI.Application.IService;
using MedMateAI.Infrastructure.Identity;
using MedMateAI.Domain.Persistence;
using MedMateAI.Domain.Repository;
using MedMateAI.Infrastructure.Auth.Options;
using MedMateAI.Infrastructure.Auth.Providers;
using MedMateAI.Infrastructure.Auth.Services;
using MedMateAI.Application.Mapping;
using MedMateAI.Infrastructure.Mapping;
using MedMateAI.Infrastructure.Persistence.Seeder;
using MedMateAI.Infrastructure.Repositories;
using MedMateAI.Infrastructure.Email.Brevo;
using MedMateAI.Infrastructure.AI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MedMateAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is missing or invalid.");
        
        services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
        
        //
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        
        //
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMedicalDepartmentService, MedicalDepartmentService>();
        services.AddScoped<IMedicalFacilityService, MedicalFacilityService>();
        services.AddScoped<IPatientProfileService, PatientProfileService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<IAIConfigService, AIConfigService>();
        services.AddScoped<IWebChatbotService, MedMateAI.Infrastructure.AI.WebChatbotService>();
      
        
        //
        services.AddHttpContextAccessor();

        //
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Redis connection string is missing.");
        });

        services.AddHttpClient<IEmailOtpSender, BrevoEmailOtpService>(client =>
        {
            client.BaseAddress = new Uri("https://api.brevo.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<IAIChatProvider, OpenRouterChatProvider>();

        // 
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHostedService<IdentitySeedHostedService>();
        
        //
        services.AddOptions<JwtOptions>()
        .Bind(configuration.GetSection(JwtOptions.SectionName))
        .Validate(x =>
           !string.IsNullOrWhiteSpace(x.Secret) &&
            x.Secret.Length >= 32,
           "JWT secret must be at least 32 chars.")
         .ValidateOnStart();
        
        services.AddAutoMapper(cfg => { }, typeof(UserMappingProfile), typeof(PatientProfileMappingProfile));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        return services;
    }
}
