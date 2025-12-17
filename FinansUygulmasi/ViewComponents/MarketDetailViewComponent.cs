using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Veritabanı işlemleri için
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services;

namespace FinansUygulmasi.ViewComponents
{
	[ViewComponent(Name = "MarketDetail")]
	public class MarketDetailViewComponent : ViewComponent
	{
		private readonly ApplicationDbContext _context;

        public MarketDetailViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(string symbol, string sablon = "Default")
        {
            var asset = await _context.Assets.FirstOrDefaultAsync(x => x.Symbol == symbol);

            if (asset == null)
            {
                return Content($"Hata: {symbol} bulunamadı.");
            }

            
            var liveData = MarketDataService.GetDetay(symbol);

            var model = new MarketDetailViewModel
            {
                Symbol = asset.Symbol,
                Name = asset.Name, // İsim veritabanından gelir 

                // Aşağıdakiler Servisten gelir (Canlı veriler)
                CurrentPrice = liveData.CurrentPrice,
                ChangeRate = liveData.ChangeRate != 0 ? liveData.ChangeRate : 1.25m, // Örnek veri
                IconClass = liveData.IconClass,
                ColorClass = liveData.ColorClass,

                // AI Tahmin Verileri (Servisten)
                AIComment = liveData.AIComment,
                PredictionDirection = liveData.PredictionDirection,
                PredictedPrice = liveData.PredictedPrice,
                ConfidenceScore = liveData.ConfidenceScore
            };

            // 4. İstenilen Tasarım Şablonunu Döndür
            // sablon: "Default", "Chat", "Prediction" olabilir.
            return View(sablon, model);
        }
    }
}