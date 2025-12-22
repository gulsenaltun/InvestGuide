using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Data; // SQL Context için
using FinansUygulmasi.Models.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FinansUygulmasi.Models.ViewModels;
using MongoDB.Driver;
using MongoDB.Bson;

namespace FinansUygulmasi.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _sqlContext; 
        private readonly MongoDbContext _mongoContext;
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
        public AdminController(ApplicationDbContext context, MongoDbContext mongoContext)
        {
            _sqlContext = context;
            _mongoContext = mongoContext;

        }

        private bool IsAdmin()
        {
            var girisYapanEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(girisYapanEmail))
            {
                return false;
            }

            // 2. Adım: Bu e-posta ile veritabanındaki kullanıcıyı bul
            var user = _sqlContext.Users.FirstOrDefault(x => x.Email == girisYapanEmail);

            if (user != null && user.Role == "admin")
            {
                return true;
            }

            return false;
        }

        public IActionResult Index(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.ToplamKullanici = _sqlContext.Users.Count();
            ViewBag.MarketDurumu = MarketErisimiAcik ? "Açık" : "Kapalı";

            // 1. KULLANIM: SQL View ile Users tablosunu user_id üzerinden birleştiriyoruz
            // Bu sayede hem sıralama (View'dan gelen) korunur hem de User modelini döndürebiliriz.
            var usersQuery = from v in _sqlContext.UserList
                            join u in _sqlContext.Users on v.user_id equals u.UserId
                            select u; // Burası önemli! User entity'sini seçiyoruz.

            // 2. ARAMA: Filtreleme yapalım
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                usersQuery = usersQuery.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
                ViewBag.CurrentSearch = search;
            }

            // 3. SONUÇ: View'a List<User> gönderiyoruz (Hata böylece çözülür)
            var userList = usersQuery.ToList();

            return View(userList);
        }

        [HttpGet]
        public IActionResult KullaniciDuzenle(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var user = _sqlContext.Users.Find(id);
            if (user == null) return RedirectToAction("Index");

            return View(user);
        }

        [HttpPost]
        public IActionResult KullaniciDuzenle(User model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var existingUser = _sqlContext.Users.Find(model.UserId);
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

                _sqlContext.SaveChanges();
                TempData["Mesaj"] = "Kullanıcı başarıyla güncellendi.";
            }

            return RedirectToAction("Index");
        }



       [HttpGet] 
public IActionResult Kullanicilar(string search)
{
    try 
    {
        // Önce Index sayfasının ihtiyaç duyduğu verileri garantiye alıyoruz
        ViewBag.ToplamKullanici = _sqlContext.Users.Count();
        ViewBag.MarketDurumu = MarketErisimiAcik ? "Açık" : "Kapalı";
        ViewBag.CurrentSearch = search;

        var users = _sqlContext.Users.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim();
            users = users.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
        }

        // BURASI KRİTİK: Eğer "Kullanicilar" diye bir sayfa yoksa, 
        // doğrudan "Index" sayfasını aç diyoruz. Hata ihtimalini sıfırlıyoruz.
        return View("Index", users.ToList());
    }
    catch (Exception)
    {
        // Ola ki veritabanı vs. bir hata olursa, patlamak yerine Index'e tazeleme yap
        return RedirectToAction("Index");
    }
}

public IActionResult KullaniciSil(int id)
{
    try
    {
        if (!IsAdmin()) return RedirectToAction("Index", "Home");

        var user = _sqlContext.Users.Find(id);
        if (user != null && user.Role != "admin")
        {
            var kullaniciIslemleri = _sqlContext.Transactions.Where(x => x.UserId == id).ToList();
            if (kullaniciIslemleri.Any()) _sqlContext.Transactions.RemoveRange(kullaniciIslemleri);

            _sqlContext.Users.Remove(user);
            _sqlContext.SaveChanges();
            TempData["Mesaj"] = "Kullanıcı başarıyla silindi.";
        }
    }
    catch (Exception ex)
    {
        TempData["Hata"] = "Bir hata oluştu: " + ex.Message;
    }

    // Silme bittikten veya hata aldıktan sonra 
    // ASLA "Kullanicilar"a gitme, hep çalışan "Index"e git.
    return RedirectToAction("Index");
}

        public IActionResult MarketDurumunuDegistir()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Durumu tersine çevir (Açıksa kapat, kapalıysa aç)
            MarketErisimiAcik = !MarketErisimiAcik;

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Yorumlar()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Verileri çek
            var tumKonular = await _mongoContext.Tartismalar.Find(_ => true).ToListAsync();
            var adminYorumListesi = new List<MesajViewModel>();

            foreach (var konu in tumKonular)
            {
                if (konu.Comments == null || !konu.Comments.Any()) continue;

                foreach (var yorum in konu.Comments)
                {
                    // -- YANITLARI HAZIRLIYORUZ --
                    var yanitlarListesi = new List<AdminYanitViewModel>();

                    if (yorum.Replies != null)
                    {
                        foreach (var rep in yorum.Replies)
                        {
                            yanitlarListesi.Add(new AdminYanitViewModel
                            {
                                // Kullanıcı adı boşsa "Anonim" yazsın, hata vermesin
                                KullaniciAdi = !string.IsNullOrEmpty(rep.Username) ? rep.Username : "Anonim",
                                Icerik = rep.Text,
                                Tarih = rep.Date
                            });
                        }
                    }

                    // -- ANA LİSTEYE EKLİYORUZ --
                    adminYorumListesi.Add(new MesajViewModel
                    {
                        Id = konu.Id,
                        YorumId = yorum.CommentId,
                        KonuBasligi = konu.Title,
                        KullaniciAdi = !string.IsNullOrEmpty(yorum.Username) ? yorum.Username : "Anonim",
                        Icerik = yorum.Text,
                        Tarih = yorum.Date,

                        // Hazırladığımız listeyi ViewModel'e atıyoruz
                        Yanitlar = yanitlarListesi
                    });
                }
            }

            // En yeniden en eskiye sırala
            var siraliListe = adminYorumListesi.OrderByDescending(x => x.Tarih).ToList();
            return View(siraliListe);
        }

        public async Task<IActionResult> YorumSil(string konuId, int yorumId)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var update = Builders<FinansUygulmasi.Models.Entities.ForumKonu>.Update
                .PullFilter(x => x.Comments, c => c.CommentId == yorumId);

            // _mongoContext kullanarak silme işlemi yapıyoruz
            var result = await _mongoContext.Tartismalar
                .UpdateOneAsync(x => x.Id == konuId, update);

            if (result.ModifiedCount > 0)
                TempData["Mesaj"] = "Yorum silindi.";
            else
                TempData["Hata"] = "Silinemedi.";

            return RedirectToAction("Yorumlar");
        }
        public async Task<IActionResult> YanitSil(string konuId, int anaYorumId, string yanitIcerik)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Konuyu bul
            var konu = await _mongoContext.Tartismalar.Find(x => x.Id == konuId).FirstOrDefaultAsync();

            if (konu != null && konu.Comments != null)
            {
                // Yorumu bul
                var anaYorum = konu.Comments.FirstOrDefault(c => c.CommentId == anaYorumId);

                if (anaYorum != null && anaYorum.Replies != null)
                {
                    // Silinecek yanıtı içeriğine göre bul
                    var silinecek = anaYorum.Replies.FirstOrDefault(r => r.Text == yanitIcerik);

                    if (silinecek != null)
                    {
                        anaYorum.Replies.Remove(silinecek); // Listeden sil

                        // Veritabanını güncelle
                        await _mongoContext.Tartismalar.ReplaceOneAsync(x => x.Id == konuId, konu);

                        TempData["Mesaj"] = "Yanıt başarıyla silindi.";
                        return RedirectToAction("Yorumlar");
                    }
                }
            }

            TempData["Hata"] = "Yanıt bulunamadı.";
            return RedirectToAction("Yorumlar");
        }
    }
}