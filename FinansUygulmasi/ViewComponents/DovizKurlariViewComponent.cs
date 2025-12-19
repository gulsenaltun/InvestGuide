using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using Finans.GrpcServer; // gRPC namespace'i

namespace FinansUygulmasi.ViewComponents // Namespace adına dikkat et, klasör adınla aynı olmalı
{
    public class DovizKurlariViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly MarketPricer.MarketPricerClient _priceClient; // gRPC Müşterisi

        // Constructor'da hem veritabanını hem gRPC servisini çağırıyoruz
        public DovizKurlariViewComponent(ApplicationDbContext context, MarketPricer.MarketPricerClient priceClient)
        {
            _context = context;
            _priceClient = priceClient;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        { 
            // 1. Veritabanından gösterilecek varlıkları çek
            var dbAssets = await _context.Assets
                                        .Where(x => x.Symbol == "GOLD" || x.Symbol == "GA" || x.Symbol == "XAU" || 
                                                    x.Symbol == "USD" || x.Symbol == "EUR" || x.Symbol == "BTC")
                                        .ToListAsync();

            var sonucListesi = new List<MarketDetailViewModel>();

            foreach (var asset in dbAssets)
            {
                // 2. Her varlık için gRPC servisine sor: "Fiyatın kaç?"
                decimal guncelFiyat = 0;
                try
                {
                    // Altın sembol karmaşasını burada da çözelim
                    string arananSembol = asset.Symbol;
                    if (asset.Symbol == "GA" || asset.Symbol == "GOLD") arananSembol = "XAU";

                    var request = new PriceRequest { Symbol = arananSembol };
                    var response = _priceClient.GetCurrentPrice(request);
                    
                    if (response.IsSuccess)
                    {
                        guncelFiyat = (decimal)response.Price;
                    }
                }
                catch
                {
                    // Servise ulaşılamazsa 0 kalır
                    guncelFiyat = 0;
                }

                sonucListesi.Add(new MarketDetailViewModel
                {
                    Symbol = asset.Symbol,
                    Name = asset.Name,
                    CurrentPrice = guncelFiyat, // Artık canlı fiyat!
                    IconClass = IkonGetir(asset.Symbol)
                });
            }

            return View(sonucListesi);
        }

        private string IkonGetir(string symbol)
        {
            return symbol switch
            {
                "USD" => "fa-solid fa-dollar-sign",
                "EUR" => "fa-solid fa-euro-sign",
                "BTC" => "fa-brands fa-bitcoin",
                "GOLD" => "fa-solid fa-coins",
                "GA" => "fa-solid fa-coins",
                "XAU" => "fa-solid fa-coins",
                _ => "fa-solid fa-coins"
            };
        }
    }
}