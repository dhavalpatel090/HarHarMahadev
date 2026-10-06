using Khaparwada.Web.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (details in Infrastructure/Extensions/ServiceCollectionExtensions.cs) ----
builder.Services.AddKhaparwadaOptions(builder.Configuration);
builder.Services.AddKhaparwadaData(builder.Configuration);
builder.Services.AddKhaparwadaIdentity();
builder.Services.AddKhaparwadaWeb();

var app = builder.Build();

// Upgrade the database (Development only) and insert required data before the first request.
await app.InitializeDatabaseAsync();

// ---- HTTP request pipeline (order matters) ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");  // friendly 404 and other error pages
app.UseHttpsRedirection();
app.UseUploadedFiles();                             // /uploads/… from the uploads folder (outside wwwroot)
app.UseLanguageFromUrl();                           // "/gu/..." → Gujarati; must come before routing
app.UseRouting();
app.UseRequestLocalization();
app.UseRateLimiter();                               // sign-in attempts per network address
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers()
    .WithStaticAssets();

await app.RunAsync();
