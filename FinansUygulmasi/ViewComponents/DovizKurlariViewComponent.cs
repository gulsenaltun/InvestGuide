using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore; // ToListAsync için gerekli
using System.Collections.Generic;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services;

namespace FinansUygulmasi.ViewComponenet
{
    public class DovizKurlariViewComponent: ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public DovizKurlariViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        { 
            var dbAssets = await _context.Assets
                                        .Where (x=>x.Symbol=="GOLD"||x.Symbol == "GA" || x.Symbol == "USD" || x.Symbol == "EUR" || x.Symbol == "BTC")
                                        .ToListAsync();

            var canliFiyatlar = MarketDataService.GetTumVeriler();

            var sonucListesi = new List<MarketDetailViewModel>();

            foreach (var asset in dbAssets)
            {
                var fiyatVerisi = canliFiyatlar.FirstOrDefault(x => x.Symbol == asset.Symbol);

                sonucListesi.Add(new MarketDetailViewModel
                {
                    Symbol = asset.Symbol,
                    Name = asset.Name, // Veritabanındaki isim (Örn: Amerikan Doları)
                    CurrentPrice = fiyatVerisi != null ? fiyatVerisi.CurrentPrice : 0, // Servisten gelen fiyat
                    IconClass = IkonGetir(asset.Symbol) // Aşağıdaki metoddan ikon al
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
                _ => "fa-solid fa-coins"
            };
        }
        
            
    }
}
