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

            // İstatistikler
            ViewBag.ToplamKullanici = _sqlContext.Users.Count();
            ViewBag.MarketDurumu = MarketErisimiAcik ? "Açık" : "Kapalı";

            // Kullanıcı Listesi Sorgusu
            var usersQuery = _sqlContext.Users.AsQueryable();

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

        [HttpGet] // Bu attribute'u eklemek iyi bir pratiktir
        public IActionResult Kullanicilar(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var users = _sqlContext.Users.AsQueryable();

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

            var user = _sqlContext.Users.Find(id);

            if (user != null)
            {
                if (user.Role == "admin")
                {
                    TempData["Hata"] = "Yönetici hesabı silinemez!";
                    return RedirectToAction("Kullanicilar");
                }

                var kullaniciIslemleri = _sqlContext.Transactions.Where(x => x.UserId == id).ToList();

                if (kullaniciIslemleri.Any())
                {
                    _sqlContext.Transactions.RemoveRange(kullaniciIslemleri);
                }


                // --- 2. ADIM: KULLANICIYI SİL ---
                _sqlContext.Users.Remove(user);

                // Tüm değişiklikleri (hem transaction silme hem user silme) kaydet
                _sqlContext.SaveChanges();
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

        public async Task<IActionResult> Yorumlar()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // _mongoContext kullanarak verileri çekiyoruz
            var tumKonular = await _mongoContext.Tartismalar.Find(_ => true).ToListAsync();

            var adminYorumListesi = new List<MesajViewModel>();

            foreach(var konu in tumKonular)
            {
                if (konu.Comments != null)
                {
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
                                    KullaniciAdi = rep.Username,
                                    Icerik = rep.Text,
                                    Tarih = rep.Date
                                });
                            }
                        }

                        // -- ANA LİSTEYE EKLİYORUZ --
                        adminYorumListesi.Add(new MesajViewModel
                        {
                            Id = konu.Id,                // Konu ID
                            YorumId = yorum.CommentId,   // Yorum ID
                            KonuBasligi = konu.Title,
                            KullaniciAdi = yorum.Username,
                            Icerik = yorum.Text,
                            Tarih = yorum.Date,
                            Yanitlar = yanitlarListesi   // Yanıtları buraya koyduk
                        });
                    }
                }
            }

            return View(adminYorumListesi.OrderByDescending(x => x.Tarih).ToList());
        }

        public async Task<IActionResult> YorumSil(string konuId, int yorumId)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var update = Builders<FinansUygulmasi.Models.Entities.ForumKonu>.Update
                .PullFilter(x => x.Comments, c => c.CommentId == yorumId);

            await _mongoContext.Tartismalar.UpdateOneAsync(x => x.Id == konuId, update);
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

            // MongoDB Query: "Comments" dizisi içindeki "Replies" dizisinden, "text"i eşleşeni çıkar.
            var filter = Builders<ForumKonu>.Filter.And(
                Builders<ForumKonu>.Filter.Eq(x => x.Id, konuId),
                Builders<ForumKonu>.Filter.ElemMatch(x => x.Comments, c => c.CommentId == anaYorumId)
            );

            // BsonDocument filtresi için "MongoDB.Bson" kütüphanesi gereklidir (Yukarıya ekledim)
            var update = Builders<ForumKonu>.Update
                .PullFilter("Comments.$.Replies", Builders<BsonDocument>.Filter.Eq("text", yanitIcerik));

            await _mongoContext.Tartismalar.UpdateOneAsync(filter, update);

            TempData["Mesaj"] = "Yanıt silindi.";
            return RedirectToAction("Yorumlar");
        }
    }
}