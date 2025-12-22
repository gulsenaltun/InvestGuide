using System.Diagnostics;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; // 1. Ekleme: MongoDB komutları için gerekli

namespace FinansUygulmasi.Controllers
{
    public class HomeController : Controller
    {
        // 2. Ekleme: Veritabanı bağlantı değişkenini tanımlıyoruz
        private readonly MongoDbContext _mongoContext;

        // 3. Ekleme: Constructor (Yapıcı Metot)
        // Program çalıştığında bu Controller, MongoDbContext'i talep eder.
        public HomeController(MongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public IActionResult Index(string? gelenMail)
        {
            // --- A. Kullanıcı Adı Kısmı ---
            if (string.IsNullOrEmpty(gelenMail))
            {
                ViewBag.Kullanici = "Değerli Üye";
            }
            else
            {
                ViewBag.Kullanici = gelenMail;
            }

            // --- B. Veritabanı Kısmı (DÜZELTİLEN KISIM) ---
            // AsQueryable() kullanarak işlemi standart C# sorgusuna çeviriyoruz.
            // Bu sayede .ToList() hatası ortadan kalkar.
            var konular = _mongoContext.Tartismalar
                                       .AsQueryable()
                                       .OrderByDescending(x => x.CreatedAt) // SortBy yerine OrderByDescending
                                       .ToList();

            return View(konular);
        }

        [HttpPost]
        public IActionResult IslemYap(int miktar, string tur, string sembol) // Sizin metodunuzun adı neyse
        {
            // 1. GÜVENLİK KONTROLÜ: Market Kapalıysa İşlemi Durdur
            if (AdminController.MarketErisimiAcik == false)
            {
                // İsterseniz burada da ContentResult döndürebilirsiniz ama
                // Kullanıcıyı ana sayfada tutup uyarı vermek daha şıktır.
                TempData["Hata"] = "? Market kapalı olduğu için işlem gerçekleştirilemedi.";
                return RedirectToAction("Index");
            }

            // ... Sizin mevcut bakiye düşme / coin ekleme kodlarınız ...

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetYapayZekaTahmini(string symbol, string currentPrice)
        {
            // Gelen fiyat verisindeki virgülü noktaya çevir (URL uyumu için)
            if (string.IsNullOrEmpty(currentPrice)) currentPrice = "0";
            string formattedPrice = currentPrice.Replace(",", ".");

            // Node.js servisine istek atacak URL (Docker veya Localhost port 3000)
            string apiUrl = $"http://localhost:3000/api/predict?symbol={symbol}&currentPrice={formattedPrice}";

            using (var client = new HttpClient())
            {
                try
                {
                    // Node.js'ten cevabı bekle (Asenkron)
                    var responseString = await client.GetStringAsync(apiUrl);

                    //  Gelen JSON verisini direkt olarak JavaScript'e gönder
                    return Content(responseString, "application/json");
                }
                catch (System.Exception ex)
                {
                    // Hata olursa JSON formatında hata dön
                    return Json(new { success = false, error = "Yapay zeka servisine ulaşılamadı: " + ex.Message });
                }
            }
        }
    }
}