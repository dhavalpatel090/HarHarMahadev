using Khaparwada.Web.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (details in Infrastructure/Extensions/ServiceCollectionExtensions.cs) ----
builder.Services.AddKhaparwadaOptions(builder.Configuration);
builder.Services.AddKhaparwadaData(builder.Configuration);
builder.Services.AddKhaparwadaIdentity();

builder.Services.AddControllersWithViews(options =>
{
    // Check the antiforgery token on every POST/PUT/DELETE, so no form can forget it (CSRF protection).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Upgrade the database (Development only) and insert required data before the first request.
await app.InitializeDatabaseAsync();

// ---- HTTP request pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
