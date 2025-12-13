using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Data; // SQL Context için
using FinansUygulmasi.Models.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FinansUygulmasi.Models.ViewModels;

namespace FinansUygulmasi.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public static bool MarketErisimiAcik
        {
            get
            {
                string dosyaYolu = Path.Combine(Directory.GetCurrentDirectory(), "market_durumu.txt");
                if (!System.IO.File.Exists(dosyaYolu)) return true;
                return System.IO.File.ReadAllText(dosyaYolu) == "1";
            }
            set
            {
                string dosyaYolu = Path.Combine(Directory.GetCurrentDirectory(), "market_durumu.txt");
                System.IO.File.WriteAllText(dosyaYolu, value ? "1" : "0");
            }
        }
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var girisYapanEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(girisYapanEmail))
            {
                return false;
            }

            // 2. Adım: Bu e-posta ile veritabanındaki kullanıcıyı bul
            var user = _context.Users.FirstOrDefault(x => x.Email == girisYapanEmail);

            if (user != null && user.Role == "admin")
            {
                return true;
            }

            return false;
        }

        public IActionResult Index(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // İstatistikler
            ViewBag.ToplamKullanici = _context.Users.Count();
            ViewBag.MarketDurumu = MarketErisimiAcik ? "Açık" : "Kapalı";

            // Kullanıcı Listesi Sorgusu
            var usersQuery = _context.Users.AsQueryable();

            // Eğer arama yapıldıysa filtrele
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                usersQuery = usersQuery.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

                // Arama kutusunda yazı kalsın diye ViewBag'e atıyoruz
                ViewBag.CurrentSearch = search;
            }

            // Sonuçları listele (Arama yoksa son 10, varsa hepsi)
            var userList = string.IsNullOrEmpty(search)
                           ? usersQuery.OrderByDescending(u => u.UserId).Take(10).ToList()
                           : usersQuery.ToList();

            return View(userList);
        }

        [HttpGet]
        public IActionResult KullaniciDuzenle(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var user = _context.Users.Find(id);
            if (user == null) return RedirectToAction("Index");

            return View(user);
        }

        [HttpPost]
        public IActionResult KullaniciDuzenle(User model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var existingUser = _context.Users.Find(model.UserId);
            if (existingUser != null)
            {
                // Sadece izin verilen alanları güncelliyoruz
                existingUser.Username = model.Username;
                existingUser.Email = model.Email;
                existingUser.Role = model.Role; // Admin yetkisi verme/alma

                // Şifre alanı boş değilse şifreyi de güncelle (Boşsa eski şifre kalsın)
                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    existingUser.PasswordHash = model.PasswordHash;
                }

                _context.SaveChanges();
                TempData["Mesaj"] = "Kullanıcı başarıyla güncellendi.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet] // Bu attribute'u eklemek iyi bir pratiktir
        public IActionResult Kullanicilar(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // 1. Gelen verinin başındaki/sonundaki boşlukları sil
                search = search.Trim();

                // 2. Arama filtresini uygula
                users = users.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
            }

            // 3. Kullanıcının aradığı kelimeyi sayfaya geri gönder (Input içinde kalsın)
            ViewBag.CurrentSearch = search;

            return View(users.ToList());
        }

        public IActionResult KullaniciSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var user = _context.Users.Find(id);

            if (user != null)
            {
                if (user.Role == "admin")
                {
                    TempData["Hata"] = "Yönetici hesabı silinemez!";
                    return RedirectToAction("Kullanicilar");
                }

                var kullaniciIslemleri = _context.Transactions.Where(x => x.UserId == id).ToList();

                if (kullaniciIslemleri.Any())
                {
                    _context.Transactions.RemoveRange(kullaniciIslemleri);
                }


                // --- 2. ADIM: KULLANICIYI SİL ---
                _context.Users.Remove(user);

                // Tüm değişiklikleri (hem transaction silme hem user silme) kaydet
                _context.SaveChanges();
            }

            return RedirectToAction("Kullanicilar");
        }

        public IActionResult MarketDurumunuDegistir()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Durumu tersine çevir (Açıksa kapat, kapalıysa aç)
            MarketErisimiAcik = !MarketErisimiAcik;

            return RedirectToAction("Index");
        }
    }
}