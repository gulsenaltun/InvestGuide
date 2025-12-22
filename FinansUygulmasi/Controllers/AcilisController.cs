using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.Entities;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity; // Hashleme için gerekli
using Microsoft.EntityFrameworkCore;
using Finans.GrpcServer;

namespace FinansUygulmasi.Controllers
{
    public class AcilisController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MarketPricer.MarketPricerClient _priceClient;

        // Constructor
        public AcilisController(ApplicationDbContext context, MarketPricer.MarketPricerClient priceClient)
        {
            _context = context;
            _priceClient = priceClient;
        }

        public async Task<IActionResult> Index()
        {
            // Servisin desteklediği semboller
            string[] semboller = { "XAU", "USD", "EUR", "BTC" };
            var model = new List<MarketDetailViewModel>();

            foreach (var sembol in semboller)
            {
                try
                {
                    // gRPC servisinden canlı fiyatı istiyoruz
                    var request = new PriceRequest { Symbol = sembol };
                    var response = await _priceClient.GetCurrentPriceAsync(request);

                    if (response.IsSuccess)
                    {
                        model.Add(new MarketDetailViewModel
                        {
                            Symbol = response.Symbol,
                            CurrentPrice = (decimal)response.Price
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"gRPC Bağlantı Hatası ({sembol}): " + ex.Message);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Giris()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new LoginViewModel();

            if (TempData["KayitOlunanEmail"] != null)
            {
                model.Email = TempData["KayitOlunanEmail"].ToString();
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Giris(LoginViewModel model)
        {
            // 1. Kullanıcıyı e-posta ile bul
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user != null)
            {
                bool isPasswordValid = false;

                // --- Admin Kontrolü ---
                if (user.Role == "admin")
                {
                    if (user.PasswordHash == model.Password)
                    {
                        isPasswordValid = true;
                    }
                }
                // --- Standart Kullanıcı Kontrolü ---
                else
                {
                    try
                    {
                        var hasher = new PasswordHasher<User>();
                        // Burada Microsoft Identity formatında hash kontrolü yapılır
                        var verificationResult = hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

                        if (verificationResult == PasswordVerificationResult.Success)
                        {
                            isPasswordValid = true;
                        }
                    }
                    catch (FormatException)
                    {
                        ViewBag.Hata = "Kullanıcı verisi uyumsuz. Lütfen şifrenizi sıfırlayın.";
                        return View(model);
                    }
                }

                // 2. Giriş İşlemleri
                if (isPasswordValid)
                {
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("UserName", user.Username);

                    if (user.Role == "admin")
                        return RedirectToAction("Index", "Admin");

                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Hata = "Hatalı e-posta veya şifre!";
            return View(model);
        }

        public IActionResult Cikis()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult SifremiUnuttum()
        {
            return View();
        }

        // --- GÜNCELLENEN SİFREMI UNUTTUM METODU ---
        [HttpPost]
        public async Task<IActionResult> SifremiUnuttum(SifremiUnuttumViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Servis Bağlantısı
                    var client = new SifreServisim.AuthServiceClient(SifreServisim.AuthServiceClient.EndpointConfiguration.BasicHttpBinding_IAuthService, "http://localhost:5000/AuthService.svc");
                    // 1. Token al
                    var token = await client.CreatePasswordResetTokenAsync(model.Email);

                    if (!string.IsNullOrEmpty(token))
                    {
                        // 2. Rastgele DÜZ METİN şifre üret (Mail için bu lazım)
                        string yeniSifreDuz = Guid.NewGuid().ToString().Substring(0, 8);

                        // 3. Şifreyi Web formatında HASHLE (Veritabanı için bu lazım)
                        var hasher = new PasswordHasher<User>();
                        // 'new User()' boş bir nesnedir, sadece metodun imzası için gereklidir.
                        string yeniSifreHashli = hasher.HashPassword(new User(), yeniSifreDuz);

                        // 4. Servise HASHLİ olanı gönder (Veritabanına bu yazılacak)
                        var sonuc = await client.ResetPasswordAsync(token, yeniSifreHashli);

                        if (sonuc == true)
                        {
                            // 5. Kullanıcıya DÜZ METİN olanı mail at (Giriş yapabilmesi için)
                            MailGonder(model.Email, yeniSifreDuz);

                            TempData["BasariMesaji"] = "Yeni şifreniz e-posta adresinize gönderildi.";
                            return RedirectToAction("Giris");
                        }
                    }

                    ViewBag.Hata = "Kullanıcı bulunamadı veya işlem başarısız.";
                }
                catch (Exception ex)
                {
                    ViewBag.Hata = "Hata: " + ex.Message;
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Kayit()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Kayit(KayitViewModel model)
        {
            if (ModelState.IsValid)
            {
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

                var hasher = new PasswordHasher<User>();
                string hashedPassword = hasher.HashPassword(new User(), model.Password);

                try
                {
                    _context.Database.ExecuteSqlRaw("CALL sp_RegisterUser({0}, {1}, {2})",
                                                    model.UserName,
                                                    model.Email,
                                                    hashedPassword);

                    TempData["BasariMesaji"] = "Tebrikler! Hesabınız başarıyla oluşturuldu.";
                    TempData["KayitOlunanEmail"] = model.Email;

                    return RedirectToAction("Giris");
                }
                catch (Exception ex)
                {
                    ViewBag.Hata = "Kayıt işlemi sırasında teknik bir hata oluştu: " + ex.Message;
                    return View(model);
                }
            }
            return View(model);
        }

        // Mail Gönderme Metodu
        private void MailGonder(string aliciEmail, string yeniSifre)
        {
            try
            {
                // BURAYA KENDİ BİLGİLERİNİ GİR
                string gonderenEmail = "umutsah4152@gmail.com";
                string gonderenSifre = "dybzpzevdqqwficr"; // Senin App Password'un

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(gonderenEmail, gonderenSifre);

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
                mail.IsBodyHtml = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                // Mail hatasını yutmamak için log atılabilir veya fırlatılabilir
                throw new Exception("Mail gönderilirken hata oluştu: " + ex.Message);
            }
        }
    }
}