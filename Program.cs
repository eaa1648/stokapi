using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

// WebApplication Builder'ı oluştur
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Minimum log level
    .WriteTo.Console()     // Log to console
    .WriteTo.File("logs/apideneme.log", rollingInterval: RollingInterval.Day) // Log to file
    .CreateLogger();

try
{
    // JWT settings from appsettings.json
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 16)
    {
        throw new InvalidOperationException("SecretKey is not configured correctly in appsettings.json. It should be at least 128 bits (16 bytes) long.");
    }

    // Configure Authentication
    builder.Services.AddAuthentication(options =>
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
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

    // Configure Services
    builder.Services.AddSingleton<TransactionService>();
    builder.Services.AddSingleton<ScrapingService>();
    builder.Services.AddSingleton<UserService>(provider =>
    {
        return new UserService(provider.GetRequiredService<IConfiguration>().GetConnectionString("PostgreSql"));
    });

    builder.Services.AddSingleton<AdminService>(provider =>
    {
        return new AdminService(provider.GetRequiredService<IConfiguration>());
    });

    // Configure UserRoleService
    builder.Services.AddSingleton<UserRoleService>(provider =>
    {
        return new UserRoleService(provider.GetRequiredService<IConfiguration>());
    });

    // Configure UserStockEmailService
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
    builder.Services.AddTransient<UserStockEmailService>();

    // Configure JSON options
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    // Configure Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your valid token in the text input below. Example: 'Bearer abcdef12345'"
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
                Array.Empty<string>()
            }
        });
    });

    // Configure Authorization
    builder.Services.AddAuthorization();

    // Register EmailService
    builder.Services.AddTransient<EmailService>();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Build the application
var app = builder.Build();

// Configure Serilog for logging
app.Logger.LogInformation("Starting up");

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Start the application
var port = builder.Configuration.GetValue<string>("Port") ?? "7203";
app.Run($"https://*:7203");
