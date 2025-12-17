using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session için
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Models.Entities;
using FinansUygulmasi.Services; // Fiyatları buradan çekeceğiz
using System.Linq;
using System;

namespace FinansUygulmasi.Controllers
{
    public class MarketController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarketController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. KONTROL: Eğer kapalıysa HTML String döndür
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); // ContentResult döner
            }

            // 2. Açıksa normal View döner
            return View();
        }

        public IActionResult Altin()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); // ContentResult döner
            }

            return View();
        }

        public IActionResult Dolar()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); // ContentResult döner
            }

            return View();
        }

        public IActionResult Euro()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); // ContentResult döner
            }

            return View();
        }

        public IActionResult Bitcoin()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); // ContentResult döner
            }

            return View();
        }

        // Yardımcı Metod: Fiyatı Service'den çeker
        private decimal FiyatGetir(string symbol)
        {
            var veri = MarketDataService.GetTumVeriler()
                        .FirstOrDefault(x => x.Symbol == symbol);
            return veri != null ? veri.CurrentPrice : 0;
        }

        // --- ALIM SAYFASI (GET) ---
        [HttpGet]
        public IActionResult Buy(string symbol)
        {
            // 1. Session Kontrolü
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == user.UserId);

            // 2. Model Hazırlığı
            var model = new TradeViewModel
            {
                Symbol = symbol,
                CurrentPrice = FiyatGetir(symbol),
                WalletBalance = wallet != null ? wallet.Balance : 0 // DB'den gelen bakiye
            };

            return View(model);
        }

        // --- SATIŞ SAYFASI (GET) ---
        [HttpGet]
        public IActionResult Sell(string symbol)
        {
            // 1. Session Kontrolü
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            // Kullanıcının elinde bu varlıktan kaç tane var?
            var asset = _context.Assets.FirstOrDefault(a => a.Symbol == symbol);
            var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == asset.AssetId);

            decimal sahipOlunanMiktar = userAsset != null ? userAsset.Amount : 0;

            // 2. Model Hazırlığı
            var model = new TradeViewModel
            {
                Symbol = symbol,
                CurrentPrice = FiyatGetir(symbol),
                WalletBalance = sahipOlunanMiktar // Satış sayfasında bakiye yerine 'eldeki miktar'ı gösterebiliriz
            };

            // View'da bunu ayırmak için ViewBag kullanabilirsin veya ViewModel'e 'OwnedAmount' alanı ekleyebilirsin.
            ViewBag.SahipOlunan = sahipOlunanMiktar;

            return View(model);
        }

        // --- İŞLEM SONUCU (POST) ---
        [HttpPost]
        public IActionResult IslemTamamla(TradeViewModel model, string islemTuru)
        {
            // 1. Kullanıcıyı Bul
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == user.UserId);

            // Asset ID'sini bul (Tablolara kayıt için lazım)
            var assetEntity = _context.Assets.FirstOrDefault(a => a.Symbol == model.Symbol);
            if (assetEntity == null) return RedirectToAction("Index", "Acilis"); // Hata: Asset bulunamadı

            // 2. Fiyat Hesapla
            decimal guncelFiyat = FiyatGetir(model.Symbol);
            decimal toplamTutar = guncelFiyat * model.Amount;
            string siparisNo = "TRX-" + DateTime.Now.Ticks.ToString().Substring(10);

            // 3. İŞLEM MANTIĞI
            if (islemTuru == "Alis")
            {
                // Bakiye Yeterli mi?
                if (wallet.Balance < toplamTutar)
                {
                    TempData["Hata"] = "Yetersiz Bakiye!";
                    return RedirectToAction("Buy", new { symbol = model.Symbol });
                }

                // A) Cüzdandan Düş
                wallet.Balance -= toplamTutar;

                // B) UserAssets (Varlık) Ekle veya Güncelle
                var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == assetEntity.AssetId);

                if (userAsset == null)
                {
                    // İlk defa alıyor
                    userAsset = new UserAsset
                    {
                        UserId = user.UserId,
                        AssetId = assetEntity.AssetId,
                        Amount = model.Amount
                    };
                    _context.UserAssets.Add(userAsset);
                }
                else
                {
                    // Üzerine ekle
                    userAsset.Amount += model.Amount;
                }

                ViewBag.Mesaj = "Alım Emri";
            }
            else // SATIŞ
            {
                // Elde yeterli varlık var mı?
                var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == assetEntity.AssetId);

                if (userAsset == null || userAsset.Amount < model.Amount)
                {
                    TempData["Hata"] = "Satılacak kadar varlığınız yok!";
                    return RedirectToAction("Sell", new { symbol = model.Symbol });
                }

                // A) Varlıktan Düş
                userAsset.Amount -= model.Amount;

                // B) Cüzdana Para Ekle
                wallet.Balance += toplamTutar;

                ViewBag.Mesaj = "Satış Emri";
            }

            // 4. Veritabanını Kaydet
            _context.SaveChanges();

            // 5. Bilgi Ekranı
            ViewBag.IslemNo = siparisNo;
            ViewBag.Tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            ViewBag.Symbol = model.Symbol;
            ViewBag.Miktar = model.Amount;
            ViewBag.Fiyat = guncelFiyat;
            ViewBag.Toplam = toplamTutar;
            ViewBag.Detay = $"Yeni Nakit Bakiyeniz: {wallet.Balance:N2} ₺";

            return View("Basarili");
        }

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

            // 'text/html' diyerek tarayıcıya bunun bir kod olduğunu söylüyoruz
            return Content(htmlIcerik, "text/html; charset=utf-8");
        }

        // --- ASİSTAN (YAPAY ZEKA) SAYFASI (GET) ---
        // --- ASİSTAN SAYFASI (Eksik olan parça bu) ---
        // --- ASİSTAN (ASK) SAYFASI ---
        public IActionResult Ask(string symbol)
        {
            // 1. Market Kapalıysa Engelle
            if (AdminController.MarketErisimiAcik == false)
            {
                // İstersen burada "MarketKapaliMesaji()" metodunu da çağırabilirsin
                return RedirectToAction("Index");
            }

            // 2. Sembol boş geldiyse ana sayfaya at
            if (string.IsNullOrEmpty(symbol)) return RedirectToAction("Index");

            // 3. KRİTİK ADIM: View Component'in çalışması için bu veriyi taşıyoruz
            // Ask.cshtml sayfasındaki "ViewBag.Symbol" burayı okur.
            ViewBag.Symbol = symbol;

            // 4. Sadece View'ı aç (Geri kalan işi Component yapacak)
            return View();
        }

        // --- DEKONT GÖSTERME SAYFASI (GET) ---
        [HttpGet]
        public IActionResult Receipt(string symbol, decimal miktar, decimal fiyat, decimal toplam, string islemTuru, string tarih)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Giris", "Acilis");

            ViewBag.IslemNo = "TRX-" + DateTime.Now.Ticks.ToString().Substring(12); // Basit bir no üret
            ViewBag.Tarih = tarih ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            ViewBag.Symbol = symbol;
            ViewBag.Miktar = miktar;
            ViewBag.Fiyat = fiyat;
            ViewBag.Toplam = toplam;
            ViewBag.IslemTuru = islemTuru;

            return View(); // Receipt.cshtml dosyasını açar
        }
    }
}