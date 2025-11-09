using Business.Abstract;
using Business.Concrete;
using DataAccess.Abstract;
using DataAccess.Concrete;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// MVC
builder.Services.AddControllersWithViews();
// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
// HttpContextAccessor (bazý yerlerde lazým olabilir)
builder.Services.AddHttpContextAccessor();
// ===== DI KAYITLARI =====
// Repository
builder.Services.AddScoped<ITimeCapsuleMessageDal, TimeCapsuleMessageDal>();
// Þifreleme servisi
builder.Services.AddScoped<ICryptoService, AesCryptoService>();
// Spam koruma
builder.Services.AddScoped<ISpamProtectionService, SpamProtectionService>();
// Mail gönderici
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Ana servis
builder.Services.AddScoped<ITimeCapsuleService, TimeCapsuleService>();

builder.Services.AddScoped<ISpamProtectionService, SpamProtectionService>();
builder.Services.AddSingleton<IHashService, HashService>();

// ===== Hangfire =====
builder.Services.AddHangfire(cfg =>
{
    cfg.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

builder.Services.AddHangfireServer();

var app = builder.Build();

// ===== PIPELINE =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Hangfire dashboard
app.UseHangfireDashboard("/hangfire");

// Her dakika zamaný gelen mailleri kontrol et
RecurringJob.AddOrUpdate<ITimeCapsuleService>(
    "timecapsule-send-due",
    s => s.ProcessDueMessagesAsync(),
    "*/1 * * * *"
);

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TimeCapsule}/{action=Create}/{id?}");

app.Run();
