using FinansUygulmasi.Data;
using Microsoft.EntityFrameworkCore;
// AŞAĞIDAKİLER EKSİKTİ, BUNLARI EKLE:
using FinansUygulmasi.Repositories;
using FinansUygulmasi.Repositories.Interfaces;
using FinansUygulmasi.Services;
using FinansUygulmasi.Services.Interfaces;
using Finans.GrpcServer; // Proto dosyasından gelen namespace (gRPC için)

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. REPOSITORY VE SERVİSLER (Dependency Injection)
// Yazım hatasını düzelterek (Forum...) ekliyoruz
builder.Services.AddScoped<IForumRepository, ForumRepository>();
builder.Services.AddScoped<IForumService, ForumService>();

// 2. GRPC CLIENT EKLEME (Burası Yeni!)
// Ana proje, diğer projeye (localhost:5xxx) buradan bağlanacak.
// DİKKAT: Port numarasını Server çalışınca göreceğiz, şimdilik 5000 yazdım.
builder.Services.AddGrpcClient<MarketPricer.MarketPricerClient>(o =>
{
    o.Address = new Uri("http://localhost:5154"); 
});


// ... Diğer veritabanı kodların aynen kalsın ...
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddSingleton<FinansUygulmasi.Data.MongoDbContext>();

var app = builder.Build();

// ... Alt kısımlar aynen kalsın ...
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Session Middleware

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();