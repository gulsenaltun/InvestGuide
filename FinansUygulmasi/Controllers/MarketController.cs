using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Models.Entities;
using Finans.GrpcServer;
using System.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace FinansUygulmasi.Controllers
{
    public class MarketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MarketPricer.MarketPricerClient _priceClient;

        public MarketController(ApplicationDbContext context, MarketPricer.MarketPricerClient priceClient)
        {
            _context = context;
            _priceClient = priceClient;
        }

        public IActionResult Index()
        {
            // 1. KONTROL: Market kapalıysa özel mesaj döndür
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji();
            }
            return View();
        }

        // Kısa yollar - Detay sayfasına yönlendirir
        public IActionResult Altin() => RedirectToAction("Detay", new { symbol = "XAU" });
        public IActionResult Dolar() => RedirectToAction("Detay", new { symbol = "USD" });
        public IActionResult Euro() => RedirectToAction("Detay", new { symbol = "EUR" });
        public IActionResult Bitcoin() => RedirectToAction("Detay", new { symbol = "BTC" });

        public IActionResult Detay(string symbol)
        {
            if (AdminController.MarketErisimiAcik == false) return MarketKapaliMesaji();
            if (string.IsNullOrEmpty(symbol)) return RedirectToAction("Index", "Home");

            // 1. Varlığın bilgilerini ve güncel fiyatını getir
            var asset = _context.Assets.FirstOrDefault(a => a.Symbol == symbol);
            decimal currentPrice = FiyatGetir(symbol);

            // 2. VIEW KULLANIMI: Tahmin raporundan bu varlığın adıyla veri çek
            // SQL View içinde JOIN ile isimler zaten birleştirilmiştir.
            var prediction = _context.PredictionReports
                .FirstOrDefault(p => p.name == asset.Name);

            // 3. Modeli doldur (Tahmin varsa View'dan al, yoksa varsayılan ata)
            var model = new MarketDetailViewModel
            {
                Symbol = symbol,
                CurrentPrice = currentPrice,
                
                // View'dan gelen veriler
                TargetDate = prediction?.target_date ?? DateTime.Now.AddDays(7),
                PredictedPrice = prediction?.predicted_price ?? currentPrice,
                ConfidenceScore = prediction?.confidence_score ?? 50,
                
                AIComment = prediction != null 
                    ? $"Yapay zeka bu varlık için %{prediction.confidence_score} güvenle bir tahminde bulundu." 
                    : "Bu varlık için güncel bir tahmin bulunmamaktadır.",
                    
                PredictionDirection = prediction != null && prediction.predicted_price > currentPrice ? "Yükseliş" : "Düşüş",
                ChangeRate = prediction != null && currentPrice > 0 
                    ? ((prediction.predicted_price - currentPrice) / currentPrice) * 100 
                    : 0
            };

            return View(model);
        }

        // --- YARDIMCI METOT: Fiyat Çekme ---
        private decimal FiyatGetir(string symbol)
        {
            try
            {
                var request = new PriceRequest { Symbol = symbol };
                var response = _priceClient.GetCurrentPrice(request);
                if (response.IsSuccess) return (decimal)response.Price;
            }
            catch { return 0; }
            return 0;
        }

        // --- ALIM SAYFASI (GET) ---
        [HttpGet]
        public IActionResult Buy(string symbol)
        {
            if (AdminController.MarketErisimiAcik == false) return MarketKapaliMesaji();

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == user.UserId);

            var model = new TradeViewModel
            {
                Symbol = symbol,
                CurrentPrice = FiyatGetir(symbol),
                WalletBalance = wallet != null ? wallet.Balance : 0
            };

            return View(model);
        }

        // --- SATIŞ SAYFASI (GET) ---
        [HttpGet]
        public IActionResult Sell(string symbol)
        {
            if (AdminController.MarketErisimiAcik == false) return MarketKapaliMesaji();

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            // Kullanıcının elinde bu varlıktan kaç tane var?
            var asset = _context.Assets.FirstOrDefault(a => a.Symbol == symbol);

            decimal sahipOlunanMiktar = 0;
            if (asset != null)
            {
                var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == asset.AssetId);
                sahipOlunanMiktar = userAsset != null ? userAsset.Amount : 0;
            }

            var model = new TradeViewModel
            {
                Symbol = symbol,
                CurrentPrice = FiyatGetir(symbol),
                WalletBalance = sahipOlunanMiktar // Satış sayfasında "Eldeki Miktar" olarak gösterilecek
            };

            ViewBag.SahipOlunan = sahipOlunanMiktar;
            return View(model);
        }

        // --- İŞLEM SONUCU (POST) ---
        [HttpPost]
        public IActionResult IslemTamamla(TradeViewModel model, string islemTuru)
        {
            if (AdminController.MarketErisimiAcik == false) return MarketKapaliMesaji();

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            // C# tarafındaki nesne isimlerin (Wallet/Wallets) Context'e göre kalabilir
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == user.UserId);
            var assetEntity = _context.Assets.FirstOrDefault(a => a.Symbol == model.Symbol);

            if (assetEntity == null || wallet == null) return RedirectToAction("Index", "Acilis");

            decimal guncelFiyat = FiyatGetir(model.Symbol);
            if (guncelFiyat <= 0)
            {
                TempData["Hata"] = "Fiyat sunucusuna erişilemiyor!";
                return RedirectToAction(islemTuru == "Alis" ? "Buy" : "Sell", new { symbol = model.Symbol });
            }

            decimal toplamTutar = guncelFiyat * model.Amount;
            string siparisNo = "TRX-" + DateTime.Now.Ticks.ToString().Substring(10);

            if (islemTuru == "Alis")
            {
                try 
                {
                    // SQL'deki parametre sırası: user_id, asset_id, amount, price
                    _context.Database.ExecuteSqlRaw("CALL sp_BuyAsset({0}, {1}, {2}, {3})", 
                        user.UserId, 
                        assetEntity.AssetId, 
                        model.Amount, 
                        guncelFiyat);

                    // Veritabanı SP ile güncellendiği için EF'teki wallet nesnesini tazeliyoruz
                    _context.Entry(wallet).Reload(); 

                    ViewBag.Mesaj = "Alım Emri";
                }
                catch (Exception ex)
                {
                    // Eğer buraya düşüyorsa SP hata veriyor demektir.
                    TempData["Hata"] = "İşlem Hatası: " + (ex.InnerException?.Message ?? ex.Message);
                    return RedirectToAction("Buy", new { symbol = model.Symbol });
                }
            }
            else // SATIŞ (Manuel EF Mantığı)
            {
                var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == assetEntity.AssetId);

                if (userAsset == null || userAsset.Amount < model.Amount)
                {
                    TempData["Hata"] = "Satılacak kadar varlığınız yok!";
                    return RedirectToAction("Sell", new { symbol = model.Symbol });
                }

                userAsset.Amount -= model.Amount;
                wallet.Balance += toplamTutar; // C# property ismi 'Balance' ise böyle kalsın
                _context.SaveChanges();
                ViewBag.Mesaj = "Satış Emri";
            }

            // Bilgilendirme Ekranı (Basarili.cshtml) için veriler
            ViewBag.IslemNo = siparisNo;
            ViewBag.Tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            ViewBag.Symbol = model.Symbol;
            ViewBag.Miktar = model.Amount;
            ViewBag.Fiyat = guncelFiyat;
            ViewBag.Toplam = toplamTutar;
            ViewBag.Detay = $"Yeni Nakit Bakiyeniz: {wallet.Balance:N2} ₺";

            return View("Basarili");
        }

        // --- ASİSTAN (ASK) SAYFASI ---
        // Controllers/MarketController.cs

        // MarketController.cs içinde olduğunu varsayıyorum

        public IActionResult Ask(string symbol) 
        {
            // Terminalde ne geldiğini görmek için:
            Console.WriteLine("------> ASK METODUNA GELEN SEMBOL: " + symbol);

            if (string.IsNullOrEmpty(symbol))
            {
                // Eğer buraya düşüyorsa linkten veri gelmiyor demektir.
                // Hatanın nerede olduğunu anlamak için geçici olarak "HATA" yazalım
                ViewBag.Symbol = "HATA"; 
            }
            else
            {
                ViewBag.Symbol = symbol;
            }

            return View();
        }

        // API'den gelen cevabı karşılamak için yardımcı sınıf
        

        // --- DEKONT SAYFASI ---
        [HttpGet]
        public IActionResult Receipt(string symbol, decimal miktar, decimal fiyat, decimal toplam, string islemTuru, string tarih)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Giris", "Acilis");

            ViewBag.IslemNo = "TRX-" + DateTime.Now.Ticks.ToString().Substring(12);
            ViewBag.Tarih = tarih ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            ViewBag.Symbol = symbol;
            ViewBag.Miktar = miktar;
            ViewBag.Fiyat = fiyat;
            ViewBag.Toplam = toplam;
            ViewBag.IslemTuru = islemTuru;

            return View();
        }

        // --- ENGEL SAYFASI (HTML String) ---
        private ContentResult MarketKapaliMesaji()
        {
            string htmlIcerik = @"
            <body style='background-color:#0f172a; color:white; font-family:sans-serif; display:flex; justify-content:center; align-items:center; height:100vh; margin:0;'>
                <div style='text-align:center; border:1px solid #ef4444; padding:40px; border-radius:15px; background:rgba(239, 68, 68, 0.1);'>
                    <h1 style='color:#ef4444; font-size:3rem; margin-bottom:10px;'>⛔ Dur!</h1>
                    <h2 style='margin-bottom:20px;'>Erişim Engellendi</h2>
                    <p style='color:#cbd5e1; font-size:1.1rem;'>
                        Yönetici tarafından piyasa ekranlarına erişim geçici olarak kapatılmıştır.
                    </p>
                    <br>
                    <a href='/Home/Index' style='padding:10px 20px; background:#6366f1; color:white; text-decoration:none; border-radius:5px; font-weight:bold;'>
                        Ana Sayfaya Dön
                    </a>
                </div>
            </body>";

            return Content(htmlIcerik, "text/html; charset=utf-8");
        }
    }
}