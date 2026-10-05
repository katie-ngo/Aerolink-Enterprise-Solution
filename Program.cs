using Aerolink_Enterprise_Solution.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<IHrService, HrService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/");
});

builder.Services.AddAuthentication("AeroLinkAuth")
    .AddCookie("AeroLinkAuth", options =>
    {
        options.Cookie.Name = "AeroLink.Session";
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BaggageOperationsPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("JobTitle", "Baggage Handler", "BG_Supervisor");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
