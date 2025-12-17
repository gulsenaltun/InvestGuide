using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.Entities;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity; //hashleme işlemi için

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
                return RedirectToAction("Index", "Profil"); //form yönlendirmek için
            }

            var model = new LoginViewModel();


            //Kayıt olunurken kullanılan emaili direkt tempdata ile giriş ekranına gönderir
            if (TempData["KayitOlunanEmail"] != null)
            {
                model.Email = TempData["KayitOlunanEmail"].ToString();
            }

            // Modeli View'a gönderiyoruz (böylece input dolu gelecek)
            return View(model);
        }

        [HttpPost]
        public IActionResult Giris(LoginViewModel model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user != null)
            {
                var hasher = new PasswordHasher<User>();

                //kullanıcı nesnesi, veritabanındaki hash, girilen şifre
                var verificationResult = hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

                //sonuç başarılı yani succes ise bunları yap
                if (verificationResult == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("UserName", user.Username);

                    if (user.Role == "admin")
                        return RedirectToAction("Index", "Admin");
                    // admin değilsa kullanıcı profil sayfasına yönlendiriyoruz
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Hata = "Hatalı e-posta veya şifre!";
            return View(model);
        }

        public IActionResult Cikis()
        {
            // Session'ı temizle
            HttpContext.Session.Clear();
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
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email); //kullanıcıyı veritabanında bulurum

                if (user == null)
                {
                    ViewBag.Hata = "Bu e-posta adresiyle kayıtlı bir kullanıcı bulunamadı.";
                    return View(model);
                }

                string yeniSifre = RastgeleSifreOlustur();

                try
                {
                    MailGonder(user.Email, yeniSifre);
                    var hasher = new PasswordHasher<User>();

                    //mesaj gönderildikten sonra şifreyi güncelliyorum
                    user.PasswordHash = hasher.HashPassword(user, yeniSifre);
                    _context.SaveChanges();

                    TempData["BasariMesaji"] = "Yeni şifreniz e-posta adresinize gönderildi. Lütfen gelen kutunuzu (ve spam klasörünü) kontrol ediniz.";
                    return RedirectToAction("Giris"); // Başarılıysa girişe at
                }
                catch (Exception)
                {
                    ViewBag.Hata = "Şifre güncellendi ancak mail gönderilemedi. Lütfen yöneticiyle iletişime geçin.";
                }
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

                if (_context.Users.Any(u => u.Username == model.UserName))
                {
                    ViewBag.Hata = "Bu kullanıcı adı zaten kullanımda.";
                    return View(model);
                }

                var yeniKullanici = new User
                { 
                    // ViewModel'deki isimlendirmene göre yapıyorum
                    Username = model.UserName, 
                    Email = model.Email, 
                    Role = "standard",
                    CreatedAt = DateTime.Now
                };

                var hasher = new PasswordHasher<User>();
                yeniKullanici.PasswordHash = hasher.HashPassword(yeniKullanici, model.Password);

                _context.Users.Add(yeniKullanici);
                _context.SaveChanges();

                TempData["BasariMesaji"] = "Tebrikler! Hesabınız başarıyla oluşturuldu.";
                TempData["KayitOlunanEmail"] = model.Email;

                return RedirectToAction("Giris");
            }
            return View(model);
        }
        private string RastgeleSifreOlustur()
        {
            // Guid benzersiz karmaşık sayılar üretir ve ilk sekizini alırız
            return Guid.NewGuid().ToString().Substring(0, 8);
        }

        private void MailGonder(string aliciEmail, string yeniSifre)
        {
            try
            {
                string gonderenEmail = "senin_emailin@gmail.com"; //mesajı yollayacak emaili gir
                string gonderenSifre = "senin_uygulama_sifren"; // app pasword al

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com"; // Gmail sunucusu
                smtp.Port = 587; // Standart port
                smtp.EnableSsl = true; // Güvenli bağlantı
                smtp.Credentials = new NetworkCredential(gonderenEmail, gonderenSifre);

                // Mesajı oluştur
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(gonderenEmail, "Finans AI Destek");
                mail.To.Add(aliciEmail);
                mail.Subject = "Yeni Şifreniz Oluşturuldu";
                mail.Body = $@"
            <h2>Merhaba,</h2>
            <p>Hesabınız için şifre sıfırlama talebinde bulundunuz.</p>
            <p><strong>Yeni Şifreniz:</strong> {yeniSifre}</p>
            <p>Lütfen giriş yaptıktan sonra şifrenizi değiştirmeyi unutmayın.</p>
            <br>
            <p>Saygılarımızla,<br>Finans AI Ekibi</p>";
                mail.IsBodyHtml = true; // HTML formatında gönder

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                // Hata olursa loglayabilirsin, şimdilik boş geçiyoruz
                throw new Exception("Mail gönderilirken hata oluştu: " + ex.Message);
            }
        }
    }
}
