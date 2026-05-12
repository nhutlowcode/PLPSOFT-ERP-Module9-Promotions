using Microsoft.EntityFrameworkCore;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Repositories;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════
// DbContext & Services
// ═══════════════════════════════════════════════════════════

// Module Promotions - DbContext
builder.Services.AddDbContext<PromotionDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Module Promotions - Repository & Services (DI)
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPromotionEngineService, PromotionEngineService>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
