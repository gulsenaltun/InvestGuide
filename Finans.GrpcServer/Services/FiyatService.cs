using Grpc.Core;
using Finans.GrpcServer;
using System.Net.Http;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System;
using System.Globalization;

namespace Finans.GrpcServer.Services
{
    public class FiyatService : MarketPricer.MarketPricerBase
    {
        private readonly HttpClient _httpClient;

        public FiyatService()
        {
            _httpClient = new HttpClient();
        }

        public override async Task<PriceResponse> GetCurrentPrice(PriceRequest request, ServerCallContext context)
        {
            double fiyat = 0;
            string symbol = request.Symbol.ToUpper();

            // Altın sembollerini standartlaştır
            if (symbol == "GA" || symbol == "GOLD" || symbol == "GRAM-ALTIN" || symbol == "ALTIN") 
                symbol = "XAU";

            try
            {
                if (symbol == "BTC")
                {
                    // BITCOIN: Binance API
                    fiyat = await GetBitcoinPriceTL();
                }
                else if (symbol == "XAU")
                {
                    // ALTIN: Binance (Ons) + TCMB (Dolar) Hesaplaması
                    fiyat = await GetGoldPriceTL();
                }
                else
                {
                    // DOLAR, EURO: Merkez Bankası
                    fiyat = await GetTcmbRate(symbol);
                }

                Console.WriteLine($"✅ CANLI VERİ: {symbol} -> {fiyat:N2} TL");

                return new PriceResponse
                {
                    Symbol = symbol,
                    Price = fiyat,
                    IsSuccess = fiyat > 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA ({symbol}): {ex.Message}");
                return new PriceResponse { Symbol = symbol, Price = 0, IsSuccess = false };
            }
        }

        // --- MERKEZ BANKASI (Sadece Döviz İçin) ---
        private async Task<double> GetTcmbRate(string symbol)
        {
            string url = "https://www.tcmb.gov.tr/kurlar/today.xml";
            try 
            {
                var xmlString = await _httpClient.GetStringAsync(url);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);

                string xpath = $"Tarih_Date/Currency[@Kod='{symbol}']/BanknoteSelling";
                var node = doc.SelectSingleNode(xpath);
                
                if (node != null && !string.IsNullOrEmpty(node.InnerText))
                {
                    // Nokta/Virgül dönüşümü
                    string fiyatText = node.InnerText.Replace(".", ",");
                    if (double.TryParse(node.InnerText, NumberStyles.Any, new CultureInfo("en-US"), out double sonuc)) return sonuc;
                    if (double.TryParse(node.InnerText, NumberStyles.Any, new CultureInfo("tr-TR"), out double sonucTR)) return sonucTR;
                }
            }
            catch { return 0; }
            return 0;
        }

        // --- ALTIN HESAPLAMA (MÜHENDİS ÇÖZÜMÜ) ---
        private async Task<double> GetGoldPriceTL()
        {
            try
            {
                // 1. Binance'den ONS Altın fiyatını al (PAXGUSDT sembolü altına endeksli coindir)
                string url = "https://api.binance.com/api/v3/ticker/price?symbol=PAXGUSDT";
                var jsonString = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(jsonString);
                
                double onsFiyatiDolar = (double)json["price"]; // Örn: 2650 $

                // 2. Dolar kurunu al
                double dolarKuru = await GetTcmbRate("USD");
                if (dolarKuru == 0) dolarKuru = 34.0; // TCMB hata verirse yedek

                // 3. Formül: (Ons Fiyatı / 31.1035) * Dolar Kuru = Gram Altın TL
                double gramAltinDolar = onsFiyatiDolar / 31.1035;
                double gramAltinTL = gramAltinDolar * dolarKuru;

                return gramAltinTL;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Altın hesaplama hatası: " + ex.Message);
                return 0;
            }
        }

        // --- BITCOIN API ---
        private async Task<double> GetBitcoinPriceTL()
        {
            try 
            {
                string url = "https://api.binance.com/api/v3/ticker/price?symbol=BTCUSDT";
                var jsonString = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(jsonString);
                double btcUsd = (double)json["price"];

                double dolarKuru = await GetTcmbRate("USD");
                if (dolarKuru == 0) dolarKuru = 34.0;

                return btcUsd * dolarKuru;
            }
            catch { return 0; }
        }
    }
}