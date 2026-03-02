using Concert_Backend.Data;
using Concert_Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Context ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 2. Register Custom Services (FIXED) ---
// This tells .NET: "When someone asks for IEmailService, give them EmailService"
builder.Services.AddScoped<IEmailService, EmailService>();

// --- 3. CORS Policy ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy.WithOrigins("https://concert-ticketing-system-backend.onrender.com")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// --- 4. Controllers ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger for development
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();

app.Run();