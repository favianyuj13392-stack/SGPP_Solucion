using Microsoft.EntityFrameworkCore;
using SGPP.Infrastructure;
using SGPP.Infrastructure.Persistence;
using SGPP.Domain.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
    options.Conventions.AuthorizeFolder("/Tutor", "TutorPolicy");
    options.Conventions.AuthorizeFolder("/Student", "StudentPolicy");
}).AddRazorPagesOptions(options => {
    // options.Conventions.AddPageRoute("/Account/Login", ""); // Handled by Index.cshtml
});

// Configure Policies (Optional but good for cleanliness beyond [Authorize])
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TutorPolicy", policy => policy.RequireRole("Tutor"));
    options.AddPolicy("StudentPolicy", policy => policy.RequireRole("Estudiante"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<SGPP.Infrastructure.Services.IExcelExportService, SGPP.Infrastructure.Services.ExcelExportService>();

var app = builder.Build();

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Ejecuta las migraciones pendientes de forma automática y segura en Producción
        await context.Database.MigrateAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        await DbInitializer.Initialize(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
