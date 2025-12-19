using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Services.Interfaces; // Servis Interface'i
using FinansUygulmasi.Data; // Sadece User sorgusu için (Session yönetimi ayrı bir konu)
using System.Linq;
using System.Threading.Tasks;

namespace FinansUygulmasi.Controllers
{
    public class ForumController : Controller
    {
        private readonly IForumService _forumService;
        private readonly ApplicationDbContext _sqlContext; // Sadece User Auth için tuttum

        // Dependency Injection ile Servisi alıyoruz
        public ForumController(IForumService forumService, ApplicationDbContext sqlContext)
        {
            _forumService = forumService;
            _sqlContext = sqlContext;
        }

        public async Task<IActionResult> Index()
        {
            var konular = await _forumService.KonulariListeleAsync();
            return View(konular);
        }

        [HttpGet]
        public IActionResult Olustur()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Olustur(KonuOlusturViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = GetCurrentUserInfo();
            await _forumService.KonuOlusturAsync(model, user.UserId, user.Username);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Detay(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            var detayModel = await _forumService.KonuDetayGetirAsync(id);
            if (detayModel == null) return NotFound("Konu bulunamadı.");

            var user = GetCurrentUserInfo();
            ViewBag.CurrentUserId = user.UserId;

            return View(detayModel);
        }

        [HttpPost]
        public async Task<IActionResult> CevapYaz(string id, string mesaj, string ustYorumId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mesaj))
            {
                TempData["Hata"] = "Mesaj boş olamaz!";
                return RedirectToAction("Detay", new { id = id });
            }

            var user = GetCurrentUserInfo();
            await _forumService.CevapYazAsync(id, mesaj, ustYorumId, user.UserId, user.Username);

            return RedirectToAction("Detay", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> YorumSil(string konuId, string yorumId)
        {
            var user = GetCurrentUserInfo();
            await _forumService.YorumSilAsync(konuId, yorumId, user.UserId);
            return RedirectToAction("Detay", new { id = konuId });
        }

        // Auth yardımcısı (Bunu daha sonra BaseController'a veya IdentityService'e taşımalıyız)
        private (int UserId, string Username) GetCurrentUserInfo()
        {
            var loggedInEmail = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(loggedInEmail))
            {
                var userFromDb = _sqlContext.Users.FirstOrDefault(u => u.Email == loggedInEmail);
                if (userFromDb != null) return (userFromDb.UserId, userFromDb.Username);
            }
            return (99, "Misafir_Kullanici");
        }
    }
}