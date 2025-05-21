using Microsoft.EntityFrameworkCore;
using EduScriptAI.Data;
using EduScriptAI.Services;
using EduScriptAI.Models;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<EduScriptContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient
builder.Services.AddHttpClient();

// Add Options
builder.Services.Configure<GoogleApiOptions>(
    builder.Configuration.GetSection("GoogleApi"));
builder.Services.Configure<LanguageToolOptions>(
    builder.Configuration.GetSection("LanguageTool"));

// Add Services
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IGrammarService, LanguageToolService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Script}/{action=Index}/{id?}");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<EduScriptContext>();
    context.Database.EnsureCreated();
}

app.Run();
