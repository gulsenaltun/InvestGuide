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
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); 
            }

            return View();
        }

        public IActionResult Altin()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); 
            }

            return View();
        }

        public IActionResult Dolar()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); 
            }

            return View();
        }

        public IActionResult Euro()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); 
            }

            return View();
        }

        public IActionResult Bitcoin()
        {
            if (AdminController.MarketErisimiAcik == false)
            {
                return MarketKapaliMesaji(); 
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

        //Alım işlemleri için
        [HttpGet]
        public IActionResult Buy(string symbol)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == user.UserId);

            //Model Hazırlığı
            var model = new TradeViewModel
            {
                Symbol = symbol,
                CurrentPrice = FiyatGetir(symbol),
                WalletBalance = wallet != null ? wallet.Balance : 0 
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Sell(string symbol)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            // Kullanıcının elinde bu varlıktan kaç tane var?
            var asset = _context.Assets.FirstOrDefault(a => a.Symbol == symbol);
            var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == asset.AssetId);

            decimal sahipOlunanMiktar = userAsset != null ? userAsset.Amount : 0;

            // Model Hazırlığı
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

        [HttpPost]
        public IActionResult IslemTamamla(TradeViewModel model, string islemTuru)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == user.UserId);

            // Asset ID'sini bul 
            var assetEntity = _context.Assets.FirstOrDefault(a => a.Symbol == model.Symbol);
            if (assetEntity == null) return RedirectToAction("Index", "Acilis"); // Hata: Asset bulunamadı

            // Fiyat Hesapla
            decimal guncelFiyat = FiyatGetir(model.Symbol);
            decimal toplamTutar = guncelFiyat * model.Amount;
            string siparisNo = "TRX-" + DateTime.Now.Ticks.ToString().Substring(10);

            if (islemTuru == "Alis")
            {
                // Bakiye Yeterli mi?
                if (wallet.Balance < toplamTutar)
                {
                    TempData["Hata"] = "Yetersiz Bakiye!";
                    return RedirectToAction("Buy", new { symbol = model.Symbol });
                }

                //Cüzdandan Düş
                wallet.Balance -= toplamTutar;

                //UserAssets (Varlık) Ekle veya Güncelle
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
            else //SATIŞ
            {
                // Elde yeterli varlık var mı?
                var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == assetEntity.AssetId);

                if (userAsset == null || userAsset.Amount < model.Amount)
                {
                    TempData["Hata"] = "Satılacak kadar varlığınız yok!";
                    return RedirectToAction("Sell", new { symbol = model.Symbol });
                }

                //Varlıktan Düş
                userAsset.Amount -= model.Amount;

                //Cüzdana Para Ekle
                wallet.Balance += toplamTutar;

                ViewBag.Mesaj = "Satış Emri";
            }

            //Veritabanını Kaydet
            _context.SaveChanges();

            //Bilgi Ekranı - tempdata ile taşınıyor
            TempData["Receipt_Symbol"] = model.Symbol;
            TempData["Receipt_Miktar"] = model.Amount.ToString();
            TempData["Receipt_Fiyat"] = guncelFiyat.ToString();
            TempData["Receipt_Toplam"] = toplamTutar.ToString();
            TempData["Receipt_IslemTuru"] = islemTuru;
            TempData["Receipt_Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            TempData["Receipt_No"] = "TRX-" + DateTime.Now.Ticks.ToString().Substring(10);

            return RedirectToAction("Receipt");
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

            // Başka bir action türü daha kullanıyoruz
            return Content(htmlIcerik, "text/html; charset=utf-8");
        }

        public IActionResult Ask(string symbol)
        {
            // Market Kapalıysa Engelle
            if (AdminController.MarketErisimiAcik == false)
            {
                // MarketKapalıMesaji() nıda çağırabilirdim
                return RedirectToAction("Index"); 
            }

            // Sembol boş geldiyse ana sayfaya at
            if (string.IsNullOrEmpty(symbol)) return RedirectToAction("Index");

            // Ask.cshtml sayfasındaki "ViewBag.Symbol" burayı okur.
            ViewBag.Symbol = symbol;

            return View();
        }

        //Dekont sayfası
        [HttpGet]
        public IActionResult Receipt()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Giris", "Acilis");

            if (TempData["Receipt_Symbol"] == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.IslemNo = TempData["Receipt_No"];
            ViewBag.Tarih = TempData["Receipt_Tarih"];
            ViewBag.Symbol = TempData["Receipt_Symbol"];
            ViewBag.Miktar = TempData["Receipt_Miktar"];
            ViewBag.Fiyat = Convert.ToDecimal(TempData["Receipt_Fiyat"]);
            ViewBag.Toplam = Convert.ToDecimal(TempData["Receipt_Toplam"]);
            ViewBag.IslemTuru = TempData["Receipt_IslemTuru"];

            return View(); 
        }
    }
}