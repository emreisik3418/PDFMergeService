using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using PDFMergeService.Core.Settings;
using PDFMergeService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PdfSettings>(
    builder.Configuration.GetSection("PdfSettings"));

builder.Services.Configure<SharePointSettings>(
    builder.Configuration.GetSection("SharePointSettings"));

builder.Services.Configure<LdapSettings>(
    builder.Configuration.GetSection("Ldap"));

builder.Services.Configure<ExadataSettings>(
    builder.Configuration.GetSection("Exadata"));

builder.Services.Configure<MockAuthSettings>(
    builder.Configuration.GetSection("MockAuth"));

builder.Services.Configure<AuthorizedUsersSettings>(
    builder.Configuration.GetSection("AuthorizedUsers"));

builder.Services.Configure<ActivityLogSettings>(
    builder.Configuration.GetSection("ActivityLog"));

builder.Services.AddPdfServices();
builder.Services.AddAuthServices(builder.Configuration);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ReportDeck.Auth";
        options.Cookie.HttpOnly = true;
    });

builder.Services.AddAuthorization();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PdfMerge}/{action=Index}/{id?}");

app.Run();
