using Microsoft.EntityFrameworkCore;
using patoquiz.Data;
using Pomelo.EntityFrameworkCore.MySql; // New namespace

var builder = WebApplication.CreateBuilder(args);

// Service configuration (before building the app)
builder.Services.AddRazorPages();
builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion("8.0.36")
    ));
builder.Services.AddDistributedMemoryCache(); // Required for session
builder.Services.AddSession(options =>         // Enable session services
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Middleware pipeline (order matters)
app.UseExceptionHandler("/Error");
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();   // Serve CSS, images, etc.
app.UseSession();       // Enable session before routing
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();