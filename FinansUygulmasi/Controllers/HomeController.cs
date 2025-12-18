using System.Diagnostics;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; // 1. Ekleme: MongoDB komutlarý için gerekli

namespace FinansUygulmasi.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoDbContext _mongoContext;

        public HomeController(MongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public IActionResult Index(string? gelenMail)
        {
            if (string.IsNullOrEmpty(gelenMail))
            {
                ViewBag.Kullanici = "Deðerli Üye";
            }
            else
            {
                ViewBag.Kullanici = gelenMail;
            }

            
            var konular = _mongoContext.Tartismalar
                                       .AsQueryable()
                                       .OrderByDescending(x => x.CreatedAt) // SortBy yerine OrderByDescending
                                       .ToList();

            return View(konular);
        }

        [HttpPost]
        public IActionResult IslemYap(int miktar, string tur, string sembol) 
        {
            //marketin açýk olup olmamasýný kontrol et
            if (AdminController.MarketErisimiAcik == false)
            {
                
                TempData["Hata"] = "? Market kapalý olduðu için iþlem gerçekleþtirilemedi.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }
    }
}