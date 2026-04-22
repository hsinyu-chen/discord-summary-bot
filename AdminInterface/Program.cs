using AdminInterface.Components;
using AdminInterface.Services;
using Microsoft.EntityFrameworkCore;
using SummaryAndCheck.Models;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
var connectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<SummaryAndCheckDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
}).AddCookie(options =>
{
    options.LoginPath = "/LoginPage";
});
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<PasskeyService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthService>(); // Will create next


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
