using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session işlemleri için şart
using System.Linq;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services;
using FinansUygulmasi.Models.Entities;
using System.Collections.Generic;

namespace FinansUygulmasi.Controllers
{
    public class ProfilController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfilController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. KONTROL: Session dolu mu? (Giriş yapılmış mı?)
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                // Session boşsa, giriş sayfasına geri gönder
                return RedirectToAction("Giris", "Acilis");
            }

            // 2. KULLANICIYI BUL (Session'daki e-posta ile)
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                // Kritik hata: Session var ama kullanıcı DB'den silinmiş
                HttpContext.Session.Clear();
                return RedirectToAction("Giris", "Acilis");
            }

            // 3. CÜZDAN BİLGİLERİ
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == user.UserId);
            decimal nakitBakiye = wallet != null ? wallet.Balance : 0;

            // 4. MODELİ DOLDUR
            var profilModel = new ProfilViewModel
            {
                UserId = user.UserId, // <--- BUNU EKLEMEZSEN ID "0" OLARAK GİDER
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                NakitBakiye = nakitBakiye
            };

            // --- BURADAN AŞAĞISI AYNI (Varlık Hesaplamaları) ---
            var userAssets = _context.UserAssets.Where(ua => ua.UserId == user.UserId).ToList();
            var allAssets = _context.Assets.ToList();
            var piyasaVerileri = MarketDataService.GetTumVeriler();

            var varliklarim = new List<VarlikDetay>();

            foreach (var ua in userAssets)
            {
                var assetInfo = allAssets.FirstOrDefault(a => a.AssetId == ua.AssetId);
                if (assetInfo != null && ua.Amount > 0)
                {
                    var guncelVeri = piyasaVerileri.FirstOrDefault(p => p.Symbol == assetInfo.Symbol);
                    decimal guncelFiyat = guncelVeri != null ? guncelVeri.CurrentPrice : 0;

                    varliklarim.Add(new VarlikDetay
                    {
                        Symbol = assetInfo.Symbol,
                        Name = assetInfo.Name,
                        Miktar = ua.Amount,
                        GuncelFiyat = guncelFiyat,
                        ToplamDeger = ua.Amount * guncelFiyat,
                        RenkKod = RenkGetir(assetInfo.Symbol)
                    });
                }
            }

            decimal toplamVarlikDegeri = varliklarim.Sum(v => v.ToplamDeger);

            // Yüzde hesaplama
            foreach (var v in varliklarim)
            {
                v.Yuzde = toplamVarlikDegeri > 0 ? (v.ToplamDeger / toplamVarlikDegeri) * 100 : 0;
            }

            profilModel.Varliklar = varliklarim;
            profilModel.ToplamVarlikDegeri = toplamVarlikDegeri;
            profilModel.GenelToplam = nakitBakiye + toplamVarlikDegeri;

            return View(profilModel);
        }

        private string RenkGetir(string symbol)
        {
            return symbol switch
            {
                "GOLD" => "#facc15",
                "GA" => "#facc15",
                "USD" => "#4ade80",
                "EUR" => "#60a5fa",
                "BTC" => "#f97316",
                "ETH" => "#818cf8",
                _ => "#9ca3af"
            };
        }
    }
}