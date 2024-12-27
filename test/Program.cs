using Microsoft.EntityFrameworkCore;

using Microsoft.OpenApi.Models;
using test;

// main
using test.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation(); // Enable runtime compilation in development



// // Add ApplicationDbContext with PostgreSQL
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// // Add DALs
// builder.Services.AddScoped<BookDAL>();


// // Add Swagger generation
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo
//     {
//         Title = "Your Project API",
//         Version = "v1",
//         Description = "API Documentation for Your Project"
//     });
//     c.OperationFilter<GroupByHttpMethodOperationFilter>();
// });

// // Set up a single WebApplication instance
// builder.WebHost.UseUrls("http://localhost:8080"); // Main app listens on 8080


// Configure PostgreSQL Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Session
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

//     app.UseSwagger();
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your Project API v1");
//         c.RoutePrefix = "swagger"; // Access Swagger at http://localhost:8080/swagger
//     });

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