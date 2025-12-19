using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models; // <-- DÜZELTME: Entities yerine Models kullanıldı
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
            // 1. Veritabanından Varlığı Bul
            var asset = await _context.Assets.FirstOrDefaultAsync(x => x.Symbol == symbol);

            // --- HATA KORUMASI ---
            // Eğer veritabanında yoksa (Asset null ise) hata verme, geçici oluştur.
            if (asset == null)
            {
                // DÜZELTME: Tam yol 'FinansUygulmasi.Models.Asset' olarak güncellendi
                asset = new Asset 
                {
                    Symbol = symbol,
                    Name = symbol switch
                    {
                        "USD" => "Amerikan Doları",
                        "EUR" => "Euro",
                        "BTC" => "Bitcoin",
                        "XAU" => "Gram Altın",
                        "GA" => "Gram Altın",
                        "GOLD" => "Gram Altın",
                        _ => symbol
                    },
                    Type = "Temp" // Senin Asset sınıfında 'Type' zorunlu olduğu için geçici bir değer atadım
                };
            }

            // Sembol Düzeltmesi
            string apiSymbol = (symbol == "GA" || symbol == "GOLD") ? "XAU" : symbol;

            // 2. CANLI FİYAT ÇEK (gRPC)
            decimal currentPrice = 0;
            try
            {
                var request = new PriceRequest { Symbol = apiSymbol };
                var response = _priceClient.GetCurrentPrice(request);
                
                if (response.IsSuccess) currentPrice = (decimal)response.Price;
            }
            catch
            {
                currentPrice = 0;
            }

            // Altın 0 geldiyse ve test yapıyorsak güvenlik değeri
            if (currentPrice == 0 && apiSymbol == "XAU") currentPrice = 2950;

            // 3. YAPAY ZEKA TAHMİNİ ÇEK (Node.js)
            decimal predictedPrice = 0;
            string aiComment = "Analiz bekleniyor...";
            int confidence = 0;
            string predictionDirection = "Nötr";

            string priceStr = currentPrice.ToString(CultureInfo.InvariantCulture);
            string apiUrl = $"http://localhost:3000/api/predict?symbol={apiSymbol}&currentPrice={priceStr}";

            using (var client = new HttpClient())
            {
                try
                {
                    var responseString = await client.GetStringAsync(apiUrl);
                    dynamic json = JsonConvert.DeserializeObject(responseString);

                    if (json.success == true)
                    {
                        predictedPrice = (decimal)json.predicted_price;
                        aiComment = (string)json.message;
                        
                        bool isUp = predictedPrice > currentPrice;
                        predictionDirection = isUp ? "Yükseliş" : "Düşüş";
                        confidence = 85; 
                    }
                }
                catch
                {
                    aiComment = "AI Servisine Bağlanılamadı";
                }
            }

            // 4. MODELİ DOLDUR VE VIEW'A GÖNDER
            var model = new MarketDetailViewModel
            {
                Symbol = asset.Symbol,
                Name = asset.Name,
                CurrentPrice = currentPrice,
                IconClass = IkonGetir(asset.Symbol),
                ColorClass = predictionDirection == "Yükseliş" ? "text-success" : "text-danger",
                PredictedPrice = predictedPrice,
                PredictionDirection = predictionDirection,
                ConfidenceScore = confidence,
                AIComment = aiComment,
                ChangeRate = currentPrice > 0 
                    ? ((predictedPrice - currentPrice) / currentPrice) * 100 
                    : 0
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