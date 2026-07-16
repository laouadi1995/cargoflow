using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using CargoFlow.Api.Data;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("geocoder", c =>
{
    c.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
    c.DefaultRequestHeaders.Add("User-Agent", "CargoFlow-App/1.0");
    c.Timeout = TimeSpan.FromSeconds(10);
});

// 🔥 Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 🔥 مهم جدا
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(3),
            errorNumbersToAdd: null);
        mySqlOptions.CommandTimeout(30);
    });
});

var app = builder.Build();

// Middleware to capture errors
app.UseExceptionHandler((appError) =>
{
    appError.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            Console.WriteLine($"\n❌ ERROR: {error.Error.Message}");
            Console.WriteLine($"Stack: {error.Error.StackTrace}\n");
        }
    });
});

// Middleware to log requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});

// Use CORS before mapping controllers
app.UseCors("AllowAll");

// Serve uploaded photos as static files
app.UseStaticFiles();

app.MapControllers();

// Listen on localhost — use Railway PORT env var if available, fallback to 5009
var port = Environment.GetEnvironmentVariable("PORT") ?? "5009";
app.Urls.Add($"http://localhost:{port}");
app.Urls.Add($"http://127.0.0.1:{port}");

try
{
    Console.WriteLine("\n✅ Backend started successfully!");
    Console.WriteLine($"📱 Listening on: http://localhost:{port}");
    Console.WriteLine($"🔗 To test: http://localhost:{port}/api/auth/test\n");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ CRITICAL ERROR AT STARTUP: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}\n");
}