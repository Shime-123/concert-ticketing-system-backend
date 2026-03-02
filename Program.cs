using Concert_Backend.Data;
using Concert_Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Context (Updated for PostgreSQL/Neon) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)); // Changed from UseSqlServer to UseNpgsql

// --- 2. Register Custom Services ---
builder.Services.AddScoped<IEmailService, EmailService>();

// --- 3. CORS Policy ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy.WithOrigins("https://concert-ticketing-system-frontend.onrender.com")
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

// --- 5. AUTOMATIC DATABASE MIGRATION (The part you were missing) ---
// This runs every time the app starts on Render and creates your Neon tables.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Database migration successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred during migration: {ex.Message}");
    }
}

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();

app.Run();