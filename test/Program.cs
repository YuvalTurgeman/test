using Microsoft.EntityFrameworkCore;
using test.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using test.Services;

var builder = WebApplication.CreateBuilder(args);

//Email Services
builder.Services.AddSingleton<EmailService>();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();



// Add Authentication configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Redirect to login if not authenticated
        options.LogoutPath = "/Account/Logout"; // Redirect after logout
        options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect unauthorized users
        options.Cookie.HttpOnly = true; // Ensure cookies are not accessible via client-side scripts
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Adjusted for development
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Cookie/session expiration time
        options.SlidingExpiration = true; // Reset expiration on activity
    });

// Configure PostgreSQL Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register all DAL services
builder.Services.AddScoped<BookDAL>();
builder.Services.AddScoped<UserDAL>();
builder.Services.AddScoped<PurchaseDAL>();
builder.Services.AddScoped<BorrowDAL>();
builder.Services.AddScoped<DiscountDAL>();
builder.Services.AddScoped<ShoppingCartDAL>();
builder.Services.AddScoped<CartItemDAL>();

// Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true; // Secure session cookies
    options.Cookie.IsEssential = true; // Ensure session cookies are not blocked
});

// Configure Kestrel to use HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.UseHttps(); // Use development HTTPS certificate
    });
});



var app = builder.Build();

// Use session middleware
app.UseSession();

// Database initialization (for development only; remove in production)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Uncomment the lines below for development use (e.g., to recreate the database)
    // dbContext.Database.EnsureDeleted(); // Drop the database
    // dbContext.Database.EnsureCreated(); // Recreate the database
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enable strict transport security
}

app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
app.UseStaticFiles(); // Serve static files
app.UseRouting(); // Enable routing

// Add Authentication and Authorization middlewares
app.UseAuthentication(); // Enable authentication
app.UseAuthorization(); // Enable authorization checks

// Configure default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=UserHomePage}/{id?}");

app.Run();
