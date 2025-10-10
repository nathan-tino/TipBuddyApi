using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Linq;
using TipBuddyApi.Configuration;
using TipBuddyApi.Contracts;
using TipBuddyApi.Converters;
using TipBuddyApi.Data;
using TipBuddyApi.Repository;
using TipBuddyApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Initialize Serilog early so startup logs are captured
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

// Connect to database
var connectionString = builder.Configuration.GetConnectionString("TipBuddyDbConnectionString");
builder.Services.AddDbContext<TipBuddyDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Add Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<TipBuddyDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ClockSkew = TimeSpan.Zero
    };

    // Read JWT from the HttpOnly cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("access_token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var req = context.Request;
            var hasTokenCookie = req.Cookies.ContainsKey("access_token");
            var userAgent = req.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;

            // Log exception with structured fields for easier searching and debugging
            Log.ForContext<Program>().Error(context.Exception,
                "Authentication failed for {Method} {Path}. Scheme: {Scheme}. HasAccessTokenCookie: {HasTokenCookie}. UserAgent: {UserAgent}. ExceptionMessage: {ExceptionMessage}",
                req.Method, req.Path, context.Scheme, hasTokenCookie, userAgent, context.Exception?.Message);

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var req = context.Request;
            var hasTokenCookie = req.Cookies.ContainsKey("access_token");
            var userAgent = req.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;

            Log.ForContext<Program>().Warning(
                "Authentication challenge for {Method} {Path}. Scheme: {Scheme}. Error: {Error}. ErrorDescription: {ErrorDescription}. HasAccessTokenCookie: {HasTokenCookie}. UserAgent: {UserAgent}",
                req.Method, req.Path, context.Scheme, context.Error, context.ErrorDescription, hasTokenCookie, userAgent);

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new CustomDateTimeConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MapperConfig>();
});

// Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IShiftsRepository, ShiftsRepository>();

// Services
builder.Services.AddScoped<ITimeZoneService, TimeZoneService>();
builder.Services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

// Build and run the app inside try/catch so Serilog can capture failures
var app = builder.Build();

// Resolve logger from DI for Program
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Application starting up");

    // Seed demo user
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var demoDataSeeder = services.GetRequiredService<IDemoDataSeeder>();
        await demoDataSeeder.SeedDemoDataAsync();
    }

    // Use CORS policy
    app.UseCors("AllowAngularApp");

    // Middleware to log incoming Cookie header (redacted) and outgoing Set-Cookie headers
    app.Use(async (ctx, next) =>
    {
        var req = ctx.Request;

        // Redact access_token values in incoming Cookie header
        if (req.Headers.TryGetValue("Cookie", out var cookieHeaderValues))
        {
            var header = cookieHeaderValues.FirstOrDefault() ?? string.Empty;
            var redacted = string.Join("; ", header.Split(';')
                .Select(p =>
                {
                    var kv = p.Trim();
                    var idx = kv.IndexOf('=');
                    if (idx > 0)
                    {
                        var name = kv.Substring(0, idx);
                        if (string.Equals(name, "access_token", StringComparison.OrdinalIgnoreCase))
                            return name + "=REDACTED";
                    }
                    return kv;
                }));

            Log.ForContext<Program>().Information("Incoming cookies for {Method} {Path}: {Cookies}", req.Method, req.Path, redacted);
        }
        else
        {
            Log.ForContext<Program>().Information("No Cookie header for {Method} {Path}", req.Method, req.Path);
        }

        await next();

        // After the response, log Set-Cookie headers (redacting access_token values)
        if (ctx.Response.Headers.TryGetValue("Set-Cookie", out var setCookieValues))
        {
            var redactedSet = setCookieValues.Select(sc =>
            {
                var parts = sc.Split(';');
                var first = parts.FirstOrDefault() ?? string.Empty;
                var idx = first.IndexOf('=');
                if (idx > 0)
                {
                    var name = first.Substring(0, idx);
                    if (string.Equals(name, "access_token", StringComparison.OrdinalIgnoreCase))
                    {
                        var attrs = string.Join(';', parts.Skip(1));
                        return name + "=REDACTED;" + attrs;
                    }
                }
                return sc;
            });

            Log.ForContext<Program>().Information("Outgoing Set-Cookie headers for {Method} {Path}: {SetCookies}", req.Method, req.Path, string.Join(" | ", redactedSet));
        }
    });

    app.UseAuthentication();
    app.UseAuthorization();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Allow Serilog to automatically log requests
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    // Use Serilog directly for fatal errors during startup
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
