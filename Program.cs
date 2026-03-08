using Concert_Backend.Data;
using Concert_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders; // Required for PhysicalFileProvider

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Context ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

// --- 4. Controllers & HttpClient ---
builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This ensures "concertTitle" from React maps to "ConcertTitle" in C#
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; 
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 5. AUTOMATIC DATABASE MIGRATION ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        Console.WriteLine("Checking for pending migrations...");
        context.Database.Migrate(); 
        Console.WriteLine("Migrations applied successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MIGRATION ERROR: {ex.Message}");
    }
}

// --- 6. STATIC FILES CONFIGURATION (The missing piece for the 404 fix) ---
app.UseStaticFiles(); // Handles wwwroot

// This maps the physical "assets" folder to the "/assets" URL path
var assetsPath = Path.Combine(builder.Environment.ContentRootPath, "assets");

// Create the folder if it doesn't exist to prevent errors on startup
if (!Directory.Exists(assetsPath))
{
    Directory.CreateDirectory(assetsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(assetsPath),
    RequestPath = "/assets"
});

// --- 7. Middleware Pipeline ---
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();

app.Run();