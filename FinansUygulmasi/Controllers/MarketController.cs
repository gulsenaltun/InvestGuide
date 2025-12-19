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
            return View();
        }

        public IActionResult Altin() => RedirectToAction("Detay", new { symbol = "XAU" });
        public IActionResult Dolar() => RedirectToAction("Detay", new { symbol = "USD" });
        public IActionResult Euro() => RedirectToAction("Detay", new { symbol = "EUR" });
        public IActionResult Bitcoin() => RedirectToAction("Detay", new { symbol = "BTC" });

// NOT: Az önce yazdığımız "public async Task<IActionResult> Detay(string symbol)" 
// metodu controller'da ekli olmalı (önceki cevabımda vermiştim).

        // --- FİYAT GETİR (gRPC Kullanır - Al/Sat işlemleri için) ---
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

        // --- YAPAY ZEKA TAHMİNİ (Node.js API Kullanır - Asistan Sayfası için) ---
        [HttpGet]
        public async Task<IActionResult> Ask(string symbol, double price)
        {
            // 1. GÜVENLİK: Eğer sembol yoksa veya fiyat 0 ise anasayfaya at
            if (string.IsNullOrEmpty(symbol) || price == 0) 
            {
                return RedirectToAction("Index", "Home");
            }

            // 2. MODELİ EN BAŞTA OLUŞTUR (Hata olsa bile bu model dönecek)
            var model = new TahminViewModel
            {
                Symbol = symbol,
                CurrentPrice = price,
                PredictedPrice = 0,
                IsSuccess = false,
                Message = "İşlem başlatılıyor..."
            };

            // 3. Node.js API URL'i
            string formattedPrice = price.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string apiUrl = $"http://localhost:3000/api/predict?symbol={symbol}&currentPrice={formattedPrice}";

            using (var client = new HttpClient())
            {
                try
                {
                    // Node.js Docker konteynerine istek at
                    var responseString = await client.GetStringAsync(apiUrl);
                
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);

                    if (jsonResponse.success == true)
                    {
                        model.IsSuccess = true;
                        model.PredictedPrice = (double)jsonResponse.predicted_price;
                        model.Date = (string)jsonResponse.date;
                        model.Message = (string)jsonResponse.message;
                    }
                    else
                    {
                        model.Message = "Yapay zeka şu an cevap veremiyor.";
                    }
                }   
                catch (Exception ex)
                {
                    model.Message = "Tahmin servisine bağlanılamadı. Docker çalışıyor mu? Hata: " + ex.Message;
                }
            }

            // 4. KRİTİK: Modeli View'a gönderiyoruz!
            return View(model);
        }

        [HttpGet]
        public IActionResult Buy(string symbol)
        {
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

        [HttpGet]
        public IActionResult Sell(string symbol)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Giris", "Acilis");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            var asset = _context.Assets.FirstOrDefault(a => a.Symbol == symbol);
            var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == asset.AssetId);

            decimal sahipOlunanMiktar = userAsset != null ? userAsset.Amount : 0;

            var model = new TradeViewModel
            {
                Symbol = symbol,
                CurrentPrice = FiyatGetir(symbol),
                WalletBalance = sahipOlunanMiktar
            };

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
            var assetEntity = _context.Assets.FirstOrDefault(a => a.Symbol == model.Symbol);

            if (assetEntity == null) return RedirectToAction("Index", "Acilis");

            decimal guncelFiyat = FiyatGetir(model.Symbol);
            if (guncelFiyat == 0)
            {
                 TempData["Hata"] = "Fiyat sunucusuna erişilemiyor!";
                 return RedirectToAction("Buy", new { symbol = model.Symbol });
            }

            decimal toplamTutar = guncelFiyat * model.Amount;

            if (islemTuru == "Alis")
            {
                if (wallet.Balance < toplamTutar)
                {
                    TempData["Hata"] = "Yetersiz Bakiye!";
                    return RedirectToAction("Buy", new { symbol = model.Symbol });
                }

                wallet.Balance -= toplamTutar;
                var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == assetEntity.AssetId);
                
                if (userAsset == null)
                {
                    userAsset = new UserAsset { UserId = user.UserId, AssetId = assetEntity.AssetId, Amount = model.Amount };
                    _context.UserAssets.Add(userAsset);
                }
                else
                {
                    userAsset.Amount += model.Amount;
                }
            }
            else // SATIŞ
            {
                var userAsset = _context.UserAssets.FirstOrDefault(ua => ua.UserId == user.UserId && ua.AssetId == assetEntity.AssetId);
                if (userAsset == null || userAsset.Amount < model.Amount)
                {
                    TempData["Hata"] = "Satılacak kadar varlığınız yok!";
                    return RedirectToAction("Sell", new { symbol = model.Symbol });
                }

                userAsset.Amount -= model.Amount;
                wallet.Balance += toplamTutar;
            }

            _context.SaveChanges();

            TempData["Receipt_Symbol"] = model.Symbol;
            TempData["Receipt_Miktar"] = model.Amount.ToString();
            TempData["Receipt_Fiyat"] = guncelFiyat.ToString();
            TempData["Receipt_Toplam"] = toplamTutar.ToString();
            TempData["Receipt_IslemTuru"] = islemTuru;
            TempData["Receipt_Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            TempData["Receipt_No"] = "TRX-" + DateTime.Now.Ticks.ToString().Substring(10);

            return RedirectToAction("Receipt");
        }

        [HttpGet]
        public IActionResult Receipt()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Giris", "Acilis");

            if (TempData["Receipt_Symbol"] == null) return RedirectToAction("Index");

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