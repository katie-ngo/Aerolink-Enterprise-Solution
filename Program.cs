using Aerolink_Enterprise_Solution.Services;
using Aerolink_Enterprise_Solution.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AeroLinkDbContext>(options =>
    options.UseSqlite(
        $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "AeroLink.db")}"));

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/TurnaroundPlanning");
    options.Conventions.AuthorizePage("/BaggageManagement/BaggageManagement", "BaggageOperationsPolicy");
    options.Conventions.AuthorizePage("/DepartureReadiness");
});

builder.Services.AddHttpClient<IHrService, HrService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/");
});

builder.Services.AddAuthentication("AeroLinkAuth")
    .AddCookie("AeroLinkAuth", options =>
    {
        options.Cookie.Name = "AeroLink.Auth";
        options.LoginPath = "/SignIn";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BaggageOperationsPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Baggage Handler", "BG_Supervisor");
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AeroLinkDbContext>();

    db.Database.EnsureCreated();

    ManifestImporter.ImportIfEmpty(
        db,
        Path.Combine(
            app.Environment.ContentRootPath,
            "Data",
            "BaggageManifest.xlsx"));
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
