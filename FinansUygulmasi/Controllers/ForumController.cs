using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.Entities;
using FinansUygulmasi.Models.ViewModels;
using MongoDB.Driver;
using System.Security.Claims;
using System.Linq;

namespace FinansUygulmasi.Controllers
{
    public class ForumController : Controller
    {
        private readonly MongoDbContext _mongoContext;      // MongoDB (Forum)
        private readonly ApplicationDbContext _sqlContext;  // MySQL (User)

        public ForumController(MongoDbContext mongoContext, ApplicationDbContext sqlContext)
        {
            _mongoContext = mongoContext;
            _sqlContext = sqlContext;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var konular = await _mongoContext.Tartismalar.Find(k => true)
                                  .SortByDescending(x => x.CreatedAt)
                                  .ToListAsync();
            return View(konular);
        }

        // 2. YENİ KONU OLUŞTURMA SAYFASI
        [HttpGet]
        public IActionResult Olustur()
        {
            // DÜZELTME: Login sayfasına yönlendirmeyi kapattım ki 404 hatası alma.
            // if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            return View();
        }

        // 3. YENİ KONU KAYDETME
        [HttpPost]
        public IActionResult Olustur(KonuOlusturViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var currentUser = GetCurrentUserInfo();

            var yeniKonu = new ForumKonu
            {
                Title = model.Baslik,
                Content = model.Icerik,
                AssetTag = model.VarlıkTag,
                CreatedAt = DateTime.Now,
                Stats = new ForumStats { Views = 0, Likes = 0 },
                Author = new ForumAuthor
                {
                    UserId = currentUser.UserId,
                    Username = currentUser.Username, // MySQL'den gelen isim
                    Badge = "Standart"
                },
                Comments = new List<ForumComment>()
            };

            _mongoContext.Tartismalar.InsertOne(yeniKonu);
            return RedirectToAction("Index");
        }

        // 4. DETAY SAYFASI
        public IActionResult Detay(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            var konuEntity = _mongoContext.Tartismalar.Find(k => k.Id == id).FirstOrDefault();
            if (konuEntity == null) return NotFound("Konu bulunamadı.");

            var detayModel = new KonuDetayViewModel
            {
                Id = konuEntity.Id,
                Baslik = konuEntity.Title,
                Icerik = konuEntity.Content,
                YazarAdi = konuEntity.Author.Username,
                YazarRozet = konuEntity.Author.Badge,
                Tarih = konuEntity.CreatedAt.ToString("dd MMMM yyyy HH:mm"),
                Goruntulenme = konuEntity.Stats.Views,
                Begeni = konuEntity.Stats.Likes,

                Yorumlar = konuEntity.Comments.Select(c => new YorumViewModel
                {
                    CommentId = c.CommentId.ToString(),
                    YazarAdi = c.Username,
                    YazarId = c.UserId,
                    Icerik = c.Text,
                    Tarih = c.Date.ToString("dd MMM HH:mm"),

                    Yanitlar = c.Replies?.Select(r => new YanitViewModel
                    {
                        YazarAdi = r.Username,
                        Icerik = r.Text,
                        Tarih = r.Date.ToString("dd MMM")
                    }).ToList() ?? new List<YanitViewModel>()

                }).ToList()
            };

            var currentUser = GetCurrentUserInfo();
            ViewBag.CurrentUserId = currentUser.UserId;

            return View(detayModel);
        }

        // 5. YORUM YAZMA
        [HttpPost]
        [HttpPost]
        // 5. YORUM YAZMA (SAAT ve ID AYARLI)
        [HttpPost]
        public IActionResult CevapYaz(string id, string mesaj)
        {
            // ID veya Mesaj boş mu kontrolü
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mesaj))
            {
                TempData["Hata"] = "HATA: ID veya Mesaj boş geldi!";
                return RedirectToAction("Detay", new { id = id });
            }

            var currentUser = GetCurrentUserInfo();

            // Türkiye saati ayarı (UTC + 3 Saat)
            // Eğer sunucunuz zaten TR saatindeyse burayı DateTime.Now yapabilirsiniz.
            // Ama garanti olsun diye UtcNow.AddHours(3) kullanıyoruz.
            var trSaati = DateTime.UtcNow.AddHours(3);

            var yeniYorum = new ForumComment
            {
                // 1. DÜZELTME: Rastgele bir sayısal ID üretiyoruz (Çakışma ihtimali düşük)
                CommentId = new Random().Next(100000, 999999),

                UserId = currentUser.UserId,
                Username = currentUser.Username,
                Text = mesaj,

                // 2. DÜZELTME: Saati Türkiye saati olarak kaydediyoruz
                Date = trSaati,

                Replies = new List<ForumReply>()
            };

            // Filtreleme
            var filter = Builders<ForumKonu>.Filter.Eq(k => k.Id, id);
            var update = Builders<ForumKonu>.Update.Push(k => k.Comments, yeniYorum);

            // GÜNCELLEME
            var result = _mongoContext.Tartismalar.UpdateOne(filter, update);

            if (result.MatchedCount == 0)
            {
                TempData["Hata"] = $"KRİTİK HATA: Kayıt bulunamadı. ID: {id}";
            }
            else if (result.ModifiedCount > 0)
            {
                TempData["Hata"] = null;
            }
            else
            {
                TempData["Hata"] = "HATA: Yorum eklenemedi.";
            }

            return RedirectToAction("Detay", new { id = id });
        }
        // 6. YORUM SİLME
        // 6. YORUM SİLME (DÜZELTİLMİŞ HALİ)
        [HttpPost]
        public IActionResult YorumSil(string konuId, string yorumId)
        {
            var currentUser = GetCurrentUserInfo();

            // DÜZELTME BURADA:
            // HTML'den string olarak gelen yorumId'yi sayıya (int) çeviriyoruz.
            if (!int.TryParse(yorumId, out int silinecekYorumId))
            {
                TempData["Hata"] = "Geçersiz Yorum ID formatı.";
                return RedirectToAction("Detay", new { id = konuId });
            }

            var filter = Builders<ForumKonu>.Filter.And(
                Builders<ForumKonu>.Filter.Eq(k => k.Id, konuId),
                // Artık int == int kıyaslaması yapıyoruz, hata düzelir:
                Builders<ForumKonu>.Filter.ElemMatch(x => x.Comments, c => c.CommentId == silinecekYorumId && c.UserId == currentUser.UserId)
            );

            // Buradaki silme işleminde de int değişkeni kullanıyoruz
            var update = Builders<ForumKonu>.Update.PullFilter(k => k.Comments, c => c.CommentId == silinecekYorumId);

            var result = _mongoContext.Tartismalar.UpdateOne(filter, update);

            if (result.ModifiedCount == 0)
            {
                TempData["Hata"] = "Silme yetkiniz yok veya yorum bulunamadı.";
            }

            return RedirectToAction("Detay", new { id = konuId });
        }

        private (int UserId, string Username) GetCurrentUserInfo()
        {
            // 1. AcilisController'da kaydettiğin Session verisine bakıyoruz.
            // Orada "UserEmail" anahtarıyla maili tutmuştun.
            var loggedInEmail = HttpContext.Session.GetString("UserEmail");

            if (!string.IsNullOrEmpty(loggedInEmail))
            {
                // 2. Eğer Session'da mail varsa, MySQL'e gidip bu maile sahip kullanıcıyı buluyoruz.
                var userFromDb = _sqlContext.Users.FirstOrDefault(u => u.Email == loggedInEmail);

                if (userFromDb != null)
                {
                    // BINGO! Giriş yapan kişiyi bulduk.
                    // Onun gerçek ID'sini ve Adını döndürüyoruz.
                    return (userFromDb.UserId, userFromDb.Username);
                }
            }

            // Session boşsa veya kullanıcı bulunamazsa Misafir döner
            return (99, "Misafir_Kullanici");
        }


    }
}