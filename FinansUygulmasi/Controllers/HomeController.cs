using System.Diagnostics;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Net.Http; // 1. YENİ: HTTP istekleri için gerekli
using System.Threading.Tasks; // 2. YENİ: Asenkron işlemler için gerekli

namespace FinansUygulmasi.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoDbContext _mongoContext;

        public HomeController(MongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public IActionResult Index(string? gelenMail)
        {
            if (string.IsNullOrEmpty(gelenMail))
            {
                ViewBag.Kullanici = "Değerli Üye";
            }
            else
            {
                ViewBag.Kullanici = gelenMail;
            }
            
            var konular = _mongoContext.Tartismalar
                                       .AsQueryable()
                                       .OrderByDescending(x => x.CreatedAt)
                                       .ToList();

            return View(konular);
        }

        [HttpPost]
        public IActionResult IslemYap(int miktar, string tur, string sembol) 
        {
            // Marketin açık olup olmamasını kontrol et
            if (AdminController.MarketErisimiAcik == false)
            {
                TempData["Hata"] = "⛔ Market kapalı olduğu için işlem gerçekleştirilemedi.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        // --- 3. YENİ: YAPAY ZEKA TAHMİN METODU (Node.js Köprüsü) ---
        [HttpGet]
        public async Task<IActionResult> GetYapayZekaTahmini(string symbol, string currentPrice)
        {
            // 1. Gelen fiyat verisindeki virgülü noktaya çevir (URL uyumu için)
            // Örn: "34,18" -> "34.18"
            if (string.IsNullOrEmpty(currentPrice)) currentPrice = "0";
            string formattedPrice = currentPrice.Replace(",", ".");
            
            // 2. Node.js servisine istek atacak URL (Docker veya Localhost port 3000)
            string apiUrl = $"http://localhost:3000/api/predict?symbol={symbol}&currentPrice={formattedPrice}";

            using (var client = new HttpClient())
            {
                try
                {
                    // 3. Node.js'ten cevabı bekle (Asenkron)
                    var responseString = await client.GetStringAsync(apiUrl);
                    
                    // 4. Gelen JSON verisini direkt olarak JavaScript'e gönder
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