using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services;
using FinansUygulmasi.Models.Entities;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var balanceInfo = _context.UserBalances.FirstOrDefault(b => b.email == userEmail);
    
            if (balanceInfo == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Giris", "Acilis");
            }

            // Kullanıcı adını vb. hala ana Users tablosundan çekebiliriz
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            var profilModel = new ProfilViewModel
            {
                UserId = balanceInfo.user_id, 
                Username = user?.Username ?? "Kullanıcı",
                Email = balanceInfo.email,
                NakitBakiye = balanceInfo.balance // View'dan gelen sade TL miktarı
            };
            // --- VIEW KULLANIMI 2: Portföy Detayları ---
            // UserAssets ve Assets tablolarını manuel birleştirmek yerine vw_PortfolioDetails kullanıyoruz.
            // Bu View içinde asset_id, symbol ve amount gibi alanlar mevcuttur.
            var portfolioData = _context.PortfolioDetails
                .Where(p => p.user_id == balanceInfo.user_id)
                .ToList();

            var piyasaVerileri = MarketDataService.GetTumVeriler();
            var varliklarim = new List<VarlikDetay>();

            foreach (var item in portfolioData)
            {
                // View'dan gelen amount verisini kontrol ediyoruz
                if (item.amount > 0)
                {   
                    string searchSymbol = item.symbol;
                    if (searchSymbol == "XAU") searchSymbol = "GOLD";

                    var guncelVeri = piyasaVerileri.FirstOrDefault(p => p.Symbol == searchSymbol);
                    decimal guncelFiyat = guncelVeri != null ? guncelVeri.CurrentPrice : 0;

                    decimal karZarar = 0;
                    decimal karZararOrani = 0;

                    // Kar/Zarar Fonksiyonu: Parametreler doğrudan View'dan (item) besleniyor
                    if (item.symbol == "USD" || item.symbol == "EUR")
                    {
                        var result = _context.Database
                            .SqlQueryRaw<decimal>("SELECT fn_CalculatePotentialProfit({0}, {1}, {2})", 
                                item.amount, //
                                item.average_cost, //
                                guncelFiyat)
                            .ToList();

                        karZarar = result.FirstOrDefault();

                        if (item.average_cost > 0)
                        {
                            karZararOrani = ((guncelFiyat - item.average_cost) / item.average_cost) * 100;
                        }
                    }

                    // Platform Toplam Miktar Fonksiyonu
                    var platformToplamMiktar = _context.Database
                        .SqlQueryRaw<decimal>("SELECT fn_GetTotalAssetAmount({0})", item.asset_id)
                        .ToList()
                        .FirstOrDefault();

                    varliklarim.Add(new VarlikDetay
                    {
                        Symbol = item.symbol,
                        Name = item.name,
                        Miktar = item.amount, //
                        OrtalamaMaliyet = item.average_cost, //
                        GuncelFiyat = guncelFiyat,
                        ToplamDeger = item.amount * guncelFiyat,
                        KarZarar = karZarar,
                        KarZararOrani = karZararOrani,
                        RenkKod = RenkGetir(item.symbol)
                    });
                }
            }

            decimal toplamVarlikDegeri = varliklarim.Sum(v => v.ToplamDeger);

            foreach (var v in varliklarim)
            {
                v.Yuzde = toplamVarlikDegeri > 0 ? (v.ToplamDeger / toplamVarlikDegeri) * 100 : 0;
            }

            profilModel.Varliklar = varliklarim;
            profilModel.ToplamVarlikDegeri = toplamVarlikDegeri;
            profilModel.GenelToplam = profilModel.NakitBakiye + toplamVarlikDegeri;

            return View(profilModel);
        }

        private string RenkGetir(string symbol)
        {
            return symbol switch
            {
                "GOLD" => "#facc15",
                "XAU" => "#facc15",
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