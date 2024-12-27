using Microsoft.EntityFrameworkCore;


// main
using test.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// Configure PostgreSQL Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add DALs
builder.Services.AddScoped<BookDAL>();
builder.Services.AddScoped<DiscountDAL>();
builder.Services.AddScoped<UserDAL>();
builder.Services.AddScoped<PurchaseDAL>();
builder.Services.AddScoped<BorrowDAL>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ------------------------------------------------------------------
//Force database to drop and recreate
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    dbContext.Database.EnsureDeleted(); // Drops the database
    dbContext.Database.EnsureCreated(); // Recreates it based on models
    // Use dbContext.Database.Migrate(); if you want to apply migrations instead
}
// ------------------------------------------------------------------

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Add this line to enable session middleware

app.UseAuthorization();

// Map routes for Razor views and controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Start the application
app.Run();
