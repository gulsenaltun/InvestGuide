using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models; 
using FinansUygulmasi.Models.ViewModels;
using Finans.GrpcServer; 
using System.Net.Http;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace FinansUygulmasi.ViewComponents
{
    [ViewComponent(Name = "MarketDetail")]
    public class MarketDetailViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly MarketPricer.MarketPricerClient _priceClient;

        public MarketDetailViewComponent(ApplicationDbContext context, MarketPricer.MarketPricerClient priceClient)
        {
            _context = context;
            _priceClient = priceClient;
        }

        public async Task<IViewComponentResult> InvokeAsync(string symbol, string sablon = "Default")
        {
            // Eğer ViewBag'den gelen sembol boşsa veya null ise varsayılan olarak BTC ata
            if (string.IsNullOrEmpty(symbol)) symbol = "BTC"; 

            // 1. Veritabanından Varlığı Bul
            var asset = await _context.Assets.FirstOrDefaultAsync(x => x.Symbol == symbol);
            if (asset == null)
            {
                asset = new Asset { Symbol = symbol, Name = symbol, Type = "Temp" };
            }

            string apiSymbol = (symbol == "GA" || symbol == "GOLD") ? "XAU" : symbol;

            // 2. CANLI FİYAT ÇEK
            decimal currentPrice = 0;
            try {
                var response = await _priceClient.GetCurrentPriceAsync(new PriceRequest { Symbol = apiSymbol });
                if (response.IsSuccess) currentPrice = (decimal)response.Price;
            } catch { currentPrice = 0; }

            if (currentPrice == 0 && apiSymbol == "XAU") currentPrice = 2950;

            // 3. YAPAY ZEKA TAHMİNİ
            decimal predictedPrice = 0;
            string aiComment = "Analiz yapılıyor...";
            int confidence = 0;
            string predictionDirection = "Nötr";

            using (var client = new HttpClient()) {
                try {
                    string priceStr = currentPrice.ToString(CultureInfo.InvariantCulture);
                    var responseString = await client.GetStringAsync($"http://localhost:3000/api/predict?symbol={apiSymbol}&currentPrice={priceStr}");
                    dynamic json = JsonConvert.DeserializeObject(responseString);

                    if (json != null && json.success == true) {
                        predictedPrice = (decimal)json.predicted_price;
                        aiComment = (string)json.message;
                        predictionDirection = predictedPrice > currentPrice ? "Yukselis" : "Dusus";
                        confidence = 85;
                    }
                } catch { aiComment = "Yapay Zeka Servisi Çevrimdışı"; }
            }

            // 4. MODELİ DOLDUR
            var model = new MarketDetailViewModel {
                Symbol = asset.Symbol,
                Name = asset.Name,
                CurrentPrice = currentPrice,
                IconClass = IkonGetir(asset.Symbol),
                ColorClass = predictionDirection == "Yükseliş" ? "text-success" : "text-danger",
                PredictedPrice = predictedPrice,
                PredictionDirection = predictionDirection,
                ConfidenceScore = confidence,
                AIComment = aiComment,
                ChangeRate = currentPrice > 0 ? ((predictedPrice - currentPrice) / currentPrice) * 100 : 0
            };

            return View(sablon, model);
        }

        private string IkonGetir(string symbol)
        {
            return symbol switch
            {
                "USD" => "fa-solid fa-dollar-sign",
                "EUR" => "fa-solid fa-euro-sign",
                "BTC" => "fa-brands fa-bitcoin",
                "XAU" => "fa-solid fa-coins",
                "GA" => "fa-solid fa-coins",
                "GOLD" => "fa-solid fa-coins",
                _ => "fa-solid fa-chart-line"
            };
        }
    }
}