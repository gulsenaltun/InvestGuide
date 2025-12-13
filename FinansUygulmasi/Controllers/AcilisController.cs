using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.Entities;

namespace FinansUygulmasi.Controllers
{
    public class AcilisController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AcilisController(ApplicationDbContext context)
        { 
            _context = context; 
        }

        [HttpGet]
        public IActionResult Index()
        {
            var piyasaVerileri = MarketDataService.GetTumVeriler();

            return View(piyasaVerileri);
        }

        [HttpGet]
        public IActionResult Giris()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Index", "Profil");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        public IActionResult Giris(LoginViewModel model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.Password);

            if (user != null)
            {
                // Giriş başarılı olunca e-postayı session'a kaydediyoruz.
                HttpContext.Session.SetString("UserEmail", user.Email);

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.Username);

                if (user.Role == "admin")
                    return RedirectToAction("Index", "Admin");
                // Profil sayfasına yönlendiriyoruz
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Hata = "Hatalı e-posta veya şifre!";
            return View(model);
        }

        public IActionResult Cikis()
        {
            // Session'ı temizle
            HttpContext.Session.Clear();
            // Ana sayfaya veya giriş sayfasına at
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult SifremiUnuttum()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SifremiUnuttum(SifremiUnuttumViewModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["Basarimesaji"] = "Sıfırlama bağlantısı e-postanıza gönderildi. Kontrol ediniz";
                return RedirectToAction("Giris");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult Kayit()
        {
            return View(new KayitViewModel());
        }

        [HttpPost]
        public IActionResult Kayit(KayitViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Email daha önce alınmış mı kontrol et
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ViewBag.Hata = "Bu e-posta adresi zaten kullanımda.";
                    return View(model);
                }

                var yeniKullanici = new User
                {
                    Username = model.UserName, // ViewModel'deki isimlendirmene göre
                    Email = model.Email,
                    PasswordHash = model.Password, // Not: Gerçek projede şifreyi hashleyerek kaydetmelisin!
                    Role = "standard",
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(yeniKullanici);
                _context.SaveChanges();

                TempData["BasariMesaji"] = "Tebrikler! Hesabınız başarıyla oluşturuldu.";
                TempData["KayitOlunanEmail"] = model.Email;

                return RedirectToAction("Giris");
            }
            return View(model);
        }
        private bool KullaniciDogrula(string email, string password)
        {
            // ŞU AN: Sahte Kontrol (Mock Data)
            if (email == "admin@finans.com" && password == "1234")
            {
                return true;
            }
            return false;
        }

        
    }
}
