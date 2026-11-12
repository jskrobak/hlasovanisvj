using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj;
using hlasovanisvj.Components;
using hlasovanisvj.Data;
using hlasovanisvj.Hubs;
using hlasovanisvj.Services;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Aplikace je česká – formát čísel a dat nezávisle na nastavení serveru/kontejneru.
var czech = new CultureInfo("cs-CZ");
CultureInfo.DefaultThreadCurrentCulture = czech;
CultureInfo.DefaultThreadCurrentUICulture = czech;

// Klíče pro přihlašovací cookie musí přežít restart kontejneru.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
	builder.Services.AddDataProtection()
		.SetApplicationName("hlasovanisvj")
		.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
	throw new InvalidOperationException(
		"Chybí connection string 'DefaultConnection' (user-secrets nebo proměnná ConnectionStrings__DefaultConnection).");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
	options.UseNpgsql(connectionString);
});

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();
	//.AddInteractiveWebAssemblyComponents();

	builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
		.AddCookie(options =>
		{
			options.LoginPath = "/login";
			options.LogoutPath = "/logout";
			options.AccessDeniedPath = "/login";
			options.SlidingExpiration = true;
			options.ExpireTimeSpan = TimeSpan.FromDays(7);
		})
		.AddGoogleIfConfigured(builder.Configuration, out var googleEnabled);

	builder.Services.AddSingleton(new ExternalAuthInfo(googleEnabled));


	builder.Services.AddAuthorization();
	builder.Services.AddCascadingAuthenticationState();
	builder.Services.AddTransient<ISecurityService, SecurityService>();

builder.Services.AddHxServices();
builder.Services.AddHxMessenger();
builder.Services.AddHxMessageBoxHost();

builder.Services.AddSingleton<PdfService>();
builder.Services.AddScoped<DataImportService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<VoteService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<SessionReportService>();
builder.Services.AddScoped<PasswordResetService>();

var smtp = builder.Configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
if (string.IsNullOrWhiteSpace(smtp.Host))
	builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
else
	builder.Services.AddSingleton<IEmailSender>(new SmtpEmailSender(smtp));
builder.Services.AddSingleton<SessionReportPdf>();

builder.Services.AddTransient<IUploadService, UploadToMemoryCacheService>();
builder.Services.AddMemoryCache();

// Za reverse proxy (nginx/Traefik): respektovat X-Forwarded-Proto/For, aby Google redirect_uri
// a odkazy byly https. Aplikace má být dostupná jen přes proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
	options.KnownIPNetworks.Clear();
	options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Aplikuje čekající EF migrace při startu (nová instance si databázi založí sama).
// Database:SkipMigrations=true jen pro lokální náhled bez dostupné databáze.
if (!app.Configuration.GetValue("Database:SkipMigrations", false))
await using (var scope = app.Services.CreateAsyncScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	//app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapHub<VoteHub>("/hubs/vote");

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();
	//.AddInteractiveWebAssemblyRenderMode();
	//.AddAdditionalAssemblies(typeof(hlasovanisvj.Client._Imports).Assembly);

// Define file upload endpoint
var uploadService = app.Services.GetRequiredService<IUploadService>();
app.MapPost("/upload", async ([FromForm] IFormFile file) =>
{
	if (file == null || file.Length == 0)
		return Results.BadRequest("No file uploaded.");
	if (file.Length > 10 * 1024 * 1024)
		return Results.BadRequest("Soubor je příliš velký (max. 10 MB).");

	var fileId = await uploadService.SaveFileAsync(file);

	return Results.Ok(fileId);
}).DisableAntiforgery().RequireAuthorization();

app.MapGet("/download", async (HttpContext http, IMemoryCache memoryCache, [FromQuery] Guid guid) =>
{
	if (!memoryCache.TryGetValue(guid, out byte[] data))
		return Results.NotFound();

	return Results.File(data, "application/octet-stream", $"listky.pdf");
}).DisableAntiforgery().RequireAuthorization();

app.MapGet("/downloadXls", async (HttpContext http, IMemoryCache memoryCache, [FromQuery] Guid guid) =>
{
	if (!memoryCache.TryGetValue(guid, out byte[] data))
		return Results.NotFound();

	return Results.File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "members.xlsx");
	//return Results.File(data, "application/octet-stream", $"členové.xlsx");
}).DisableAntiforgery().RequireAuthorization(); 

// --- PDF report schůze (jen pro organizaci, které schůze patří) ---
app.MapGet("/sessions/{id:int}/report.pdf", async (int id, HttpContext http,
	SessionReportService reportService, SessionReportPdf reportPdf) =>
{
	var organizationId = await reportService.GetOrganizationIdAsync(http.User.FindFirstValue(ClaimTypes.Email));
	if (organizationId == null)
		return Results.Forbid();

	var report = await reportService.GetReportAsync(id, organizationId.Value);
	if (report == null)
		return Results.NotFound();

	var fileName = $"schuze-{report.Session.Date:yyyy-MM-dd}-{id}.pdf";
	return Results.File(reportPdf.Generate(report), "application/pdf", fileName);
}).RequireAuthorization();

// --- Login endpoint (POST) ---
app.MapPost("/login",
	async (HttpContext http, [FromBody] LoginRequest req, AppDbContext dbContext, ISecurityService securityService) =>
	{

		var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == req.Username);
		if (user is null || !securityService.VerifyUser(user, req.Password))
			return Results.Unauthorized();

		var principal = user.CreatePrincipal(); // securityService.CreatePrincipal(user);

		await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

		return Results.NoContent();
	});

app.MapGoogleAuth(googleEnabled);

// --- Logout endpoint (POST/GET) ---
app.MapPost("/logout", async (HttpContext http) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return Results.Redirect("/login?loggedout=1");
});

app.MapGet("/logout", async (HttpContext http, string? returnUrl) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	var local = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\");
	return Results.Redirect(local ? $"/login?loggedout=1&returnUrl={Uri.EscapeDataString(returnUrl!)}" : "/login?loggedout=1");
});

app.Run();
