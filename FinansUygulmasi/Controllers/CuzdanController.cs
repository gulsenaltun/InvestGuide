using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.Entities;
using System.Linq;
using System; // DateTime için gerekli

namespace FinansUygulmasi.Controllers
{
    public class CuzdanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuzdanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Para Yükleme Sayfasını Aç
        [HttpGet]
        public IActionResult Deposit(int id)
        {
            if (id == 0) return RedirectToAction("Giris", "Acilis");
            ViewBag.UserId = id;
            return View();
        }

        // 2. Para Yükleme İşlemini Yap
        [HttpPost]
        public IActionResult AddFunds(int userId, decimal miktar, string kartAd, string kartNo, string skt, string cvv)
        {
            // 1. Miktar kontrolü
            if (miktar <= 0)
            {
                TempData["Hata"] = "Lütfen 0'dan büyük bir tutar giriniz.";
                return RedirectToAction("Deposit", new { id = userId });
            }

            // 2. Kullanıcı Cüzdanını Bulmaya Çalış
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == userId);

            // --- KRİTİK DÜZELTME BAŞLANGICI ---

            // Eğer cüzdan YOKSA, hemen orada yeni bir cüzdan oluşturuyoruz.
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0 
                };

                _context.Wallets.Add(wallet);
                _context.SaveChanges(); // Cüzdanı veritabanına kaydettik
            }

            // --- KRİTİK DÜZELTME BİTİŞİ ---

            // Artık cüzdanın var olduğundan %100 eminiz, parayı yüklüyoruz.
            wallet.Balance += miktar;
            _context.SaveChanges();

            // Dekont sayfasına yönlendir
            return RedirectToAction("Dekont", new
            {
                userId = userId,
                tutar = miktar,
                islemTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                kartSonDort = (!string.IsNullOrEmpty(kartNo) && kartNo.Length > 4) ? kartNo.Substring(kartNo.Length - 4) : "0000"
            });
        }

        // 3. Dekont Görüntüleme Sayfası (YENİ EKLENDİ)
        [HttpGet]
        public IActionResult Dekont(int userId, decimal tutar, string islemTarihi, string kartSonDort)
        {
            // Kullanıcı bilgilerini alalım (İsim soyisim göstermek için)
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

            ViewBag.AdSoyad = user != null ? user.Username : "Kullanıcı";
            ViewBag.Tutar = tutar;
            ViewBag.Tarih = islemTarihi;
            ViewBag.KartNo = "**** **** **** " + kartSonDort;
            ViewBag.UserId = userId; // Geri dön butonu için lazım

            return View();
        }
    }
}