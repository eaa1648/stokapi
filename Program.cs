using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

var builder = WebApplication.CreateBuilder(args);

// Serilog yapılandırmasını burada yapın
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Minimum log seviyesi
    .WriteTo.Console()     // Konsola log yazma
    .WriteTo.File("logs/apideneme.log", rollingInterval: RollingInterval.Day) // Dosyaya log yazma
    .CreateLogger();

try
{
    // JWT ayarlarını appsettings.json'dan al
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 16)
    {
        throw new InvalidOperationException("SecretKey is not configured correctly in appsettings.json. It should be at least 128 bits (16 bytes) long.");
    }

    // Authentication'ı yapılandır
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

    // Servisleri ekleyin
    builder.Services.AddSingleton<ScrapingService>();
    builder.Services.AddSingleton<UserService>(provider =>
    {
        return new UserService(provider.GetRequiredService<IConfiguration>().GetConnectionString("PostgreSql"));
    });

    // AdminService'i ekleyin
    builder.Services.AddSingleton<AdminService>(provider =>
    {
        return new AdminService(provider.GetRequiredService<IConfiguration>());
    });

    // JSON ayrıştırma seçeneklerini yapılandırın
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        // JSON geçersiz kontrol karakterlerinden dolayı JSON ayrıştırıcısının kırılmasını önleyin
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    // Swagger servisini ekleyin ve JWT kimlik doğrulamasını yapılandırın
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

        // JWT Authentication'ı Swagger'a eklemek için
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
                new string[] {}
            }
        });
    });

    // Authorization servisini ekleyin
    builder.Services.AddAuthorization();

    // SMTP ayarlarını yapılandırın
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

    // Servisleri ekleyin
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

var app = builder.Build();

// Serilog ile yapılandırılmış loglama
app.Logger.LogInformation("Starting up");

// Swagger middleware'i ekleyin
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Yapılandırılabilir bağlantı noktasını kullanarak uygulamayı başlatın
var port = builder.Configuration.GetValue<string>("Port") ?? "7203";
app.Run($"https://*:7203");
