using System.Collections.Generic;
using FinansUygulmasi.Models.ViewModels;

namespace FinansUygulmasi.Services
{
    public static class MarketDataService
    {
        // Tüm coin verilerini tek seferde tanımlıyoruz
        public static List<MarketDetailViewModel> GetTumVeriler()
        {
            return new List<MarketDetailViewModel>
            {
                new MarketDetailViewModel { Symbol = "GOLD", Name = "Gram Altın", CurrentPrice = 2450.50m, IconClass = "fa-coins", ColorClass = "text-yellow-400", AIComment = "Güvenli liman alımları sürüyor." },
                new MarketDetailViewModel { Symbol = "USD", Name = "ABD Doları", CurrentPrice = 34.18m, IconClass = "fa-dollar-sign", ColorClass = "text-green-400", AIComment = "Faiz kararı bekleniyor." },
                new MarketDetailViewModel { Symbol = "EUR", Name = "Euro", CurrentPrice = 37.45m, IconClass = "fa-euro-sign", ColorClass = "text-blue-400", AIComment = "Parite stabil seyrediyor." },
                new MarketDetailViewModel { Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 2250000.00m, IconClass = "fa-brands fa-bitcoin", ColorClass = "text-orange-500", AIComment = "Balina hareketleri gözleniyor." }
            };
        }

        // Tek bir coini getiren metod
        public static MarketDetailViewModel GetDetay(string symbol)
        {
            // Listeden bul, yoksa varsayılan döndür
            var veri = GetTumVeriler().Find(x => x.Symbol == symbol);
            if (veri == null) return new MarketDetailViewModel { Symbol = "GENEL", Name = "Piyasa", CurrentPrice = 0 };

            // Tahmin verilerini burada rastgele ekleyelim (Kod kalabalığı yapmasın diye)
            veri.ConfidenceScore = 85;
            veri.PredictionDirection = "Yukselis";
            veri.PredictedPrice = veri.CurrentPrice * 1.02m; // %2 artış varsayalım

            return veri;
        }
    }
}