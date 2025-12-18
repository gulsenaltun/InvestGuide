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
        private readonly MongoDbContext _mongoContext;      // MongoDB 
        private readonly ApplicationDbContext _sqlContext;  // MySQL 

        public ForumController(MongoDbContext mongoContext, ApplicationDbContext sqlContext)
        {
            _mongoContext = mongoContext;
            _sqlContext = sqlContext;
        }

        //  mesajları listeleme
        public async Task<IActionResult> Index()
        {
            var konular = await _mongoContext.Tartismalar.Find(k => true)
                                  .SortByDescending(x => x.CreatedAt)
                                  .ToListAsync();
            return View(konular);
        }

        //yeni konu oluşturma sayfası
        [HttpGet]
        public IActionResult Olustur()
        {
            return View();
        }

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
                    Username = currentUser.Username, 
                    Badge = "Standart"
                },
                Comments = new List<ForumComment>()
            };

            _mongoContext.Tartismalar.InsertOne(yeniKonu);
            return RedirectToAction("Index");
        }

        // detay sayfası
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

       //yorum yazma
        [HttpPost]
        [HttpPost]
        public IActionResult CevapYaz(string id, string mesaj, string ustYorumId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mesaj))
            {
                TempData["Hata"] = "Mesaj boş olamaz!";
                return RedirectToAction("Detay", new { id = id });
            }

            var currentUser = GetCurrentUserInfo();
            var trSaati = DateTime.UtcNow.AddHours(3);

            // SENARYO 1: BİR YORUMA CEVAP VERİLİYOR (REPLY EKLEME)
            // ustYorumId string geldiği için int'e çevirmeyi deniyoruz
            if (!string.IsNullOrEmpty(ustYorumId) && int.TryParse(ustYorumId, out int parentCommentId))
            {
                var yeniYanit = new ForumReply
                {
                    Username = currentUser.Username,
                    Text = mesaj,
                    Date = trSaati
                };

                // Doğru konuyu ve içindeki doğru yorumu bulup replies listesine ekle
                var filter = Builders<ForumKonu>.Filter.And(
                     Builders<ForumKonu>.Filter.Eq(x => x.Id, id),
                     Builders<ForumKonu>.Filter.ElemMatch(x => x.Comments, c => c.CommentId == parentCommentId)
                );

                var update = Builders<ForumKonu>.Update.Push("Comments.$.Replies", yeniYanit);
                _mongoContext.Tartismalar.UpdateOne(filter, update);
            }
            // SENARYO 2: KONUYA YENİ ANA YORUM YAPILIYOR (COMMENT EKLEME)
            else
            {
                var yeniYorum = new ForumComment
                {
                    CommentId = new Random().Next(100000, 999999), // Rastgele int ID
                    UserId = currentUser.UserId,
                    Username = currentUser.Username,
                    Text = mesaj,
                    Date = trSaati,
                    Replies = new List<ForumReply>()
                };

                var filter = Builders<ForumKonu>.Filter.Eq(k => k.Id, id);
                var update = Builders<ForumKonu>.Update.Push(k => k.Comments, yeniYorum);
                _mongoContext.Tartismalar.UpdateOne(filter, update);
            }

            return RedirectToAction("Detay", new { id = id });
        }

        //yorum silme
        [HttpPost]
        public IActionResult YorumSil(string konuId, string yorumId)
        {
            var currentUser = GetCurrentUserInfo();

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
            var loggedInEmail = HttpContext.Session.GetString("UserEmail");

            if (!string.IsNullOrEmpty(loggedInEmail))
            {
                // 2. Eğer Session'da mail varsa, MySQL'e gidip bu maile sahip kullanıcıyı buluyoruz.
                var userFromDb = _sqlContext.Users.FirstOrDefault(u => u.Email == loggedInEmail);

                if (userFromDb != null)
                {
                    // Onun gerçek ID'sini ve Adını döndürüyoruz.
                    return (userFromDb.UserId, userFromDb.Username);
                }
            }

            // Session boşsa veya kullanıcı bulunamazsa Misafir döner
            return (99, "Misafir_Kullanici");
        }


    }
}