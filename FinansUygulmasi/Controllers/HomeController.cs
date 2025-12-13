using System.Diagnostics;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; // 1. Ekleme: MongoDB komutlarý için gerekli

namespace FinansUygulmasi.Controllers
{
    public class HomeController : Controller
    {
        // 2. Ekleme: Veritabaný baðlantý deðiþkenini tanýmlýyoruz
        private readonly MongoDbContext _mongoContext;

        // 3. Ekleme: Constructor (Yapýcý Metot)
        // Program çalýþtýðýnda bu Controller, MongoDbContext'i talep eder.
        public HomeController(MongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public IActionResult Index(string? gelenMail)
        {
            // --- A. Kullanýcý Adý Kýsmý ---
            if (string.IsNullOrEmpty(gelenMail))
            {
                ViewBag.Kullanici = "Deðerli Üye";
            }
            else
            {
                ViewBag.Kullanici = gelenMail;
            }

            // --- B. Veritabaný Kýsmý (DÜZELTÝLEN KISIM) ---
            // AsQueryable() kullanarak iþlemi standart C# sorgusuna çeviriyoruz.
            // Bu sayede .ToList() hatasý ortadan kalkar.
            var konular = _mongoContext.Tartismalar
                                       .AsQueryable()
                                       .OrderByDescending(x => x.CreatedAt) // SortBy yerine OrderByDescending
                                       .ToList();

            return View(konular);
        }

        [HttpPost]
        public IActionResult IslemYap(int miktar, string tur, string sembol) // Sizin metodunuzun adý neyse
        {
            // 1. GÜVENLÝK KONTROLÜ: Market Kapalýysa Ýþlemi Durdur
            if (AdminController.MarketErisimiAcik == false)
            {
                // Ýsterseniz burada da ContentResult döndürebilirsiniz ama
                // Kullanýcýyý ana sayfada tutup uyarý vermek daha þýktýr.
                TempData["Hata"] = "? Market kapalý olduðu için iþlem gerçekleþtirilemedi.";
                return RedirectToAction("Index");
            }

            // ... Sizin mevcut bakiye düþme / coin ekleme kodlarýnýz ...

            return RedirectToAction("Index");
        }
    }
}