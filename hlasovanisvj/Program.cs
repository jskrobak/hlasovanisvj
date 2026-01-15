using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj;
using hlasovanisvj.Components;
using hlasovanisvj.Data;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlite(connectionString));

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
		});


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

builder.Services.AddTransient<IUploadService, UploadToMemoryCacheService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

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

	var fileId = await uploadService.SaveFileAsync(file);

	return Results.Ok(fileId);
}).DisableAntiforgery();

app.MapGet("/download", async (HttpContext http, IMemoryCache memoryCache, [FromQuery] Guid guid) =>
{
	if (!memoryCache.TryGetValue(guid, out byte[] data))
		return Results.NotFound();

	return Results.File(data, "application/octet-stream", $"listky.pdf");
}).DisableAntiforgery(); 

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

// --- Logout endpoint (POST/GET) ---
app.MapPost("/logout", async (HttpContext http) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return Results.Redirect("/login?loggedout=1");
});

app.MapGet("/logout", async (HttpContext http) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return Results.Redirect("/login?loggedout=1");
}); 

app.Run();
