using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TipBuddyApi.Configuration;
using TipBuddyApi.Contracts;
using TipBuddyApi.Converters;
using TipBuddyApi.Data;
using TipBuddyApi.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Connect to database
var connectionString = builder.Configuration.GetConnectionString("TipBuddyDbConnectionString");
builder.Services.AddDbContext<TipBuddyDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<TipBuddyDbContext>();

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
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

// Allow requests from outside of host machine; Third-party access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyHeader()
             .AllowAnyOrigin()
             .AllowAnyMethod());
});

// Logger
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig.WriteTo.Console() //write to console
    .ReadFrom.Configuration(context.Configuration); //read config from addsettings.json
});

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MapperConfig>();
});

// Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IShiftsRepository, ShiftsRepository>();

var app = builder.Build();

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

// Allow requests from outside of host machine; Third-party access
app.UseCors("AllowAll");

app.MapControllers();

app.Run();
