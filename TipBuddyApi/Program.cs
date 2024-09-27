using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TipBuddyApi.Configuration;
using TipBuddyApi.Contracts;
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

builder.Services.AddControllers();

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
builder.Services.AddAutoMapper(typeof(MapperConfig));

// Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IShiftsRepository, ShiftsRepository>();

var app = builder.Build();

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
