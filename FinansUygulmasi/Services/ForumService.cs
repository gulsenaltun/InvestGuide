using FinansUygulmasi.Models.Entities;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Repositories.Interfaces;
using FinansUygulmasi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinansUygulmasi.Services
{
    public class ForumService : IForumService
    {
        private readonly IForumRepository _forumRepo;

        public ForumService(IForumRepository forumRepo)
        {
            _forumRepo = forumRepo;
        }

        public async Task<List<ForumKonu>> KonulariListeleAsync()
        {
            return await _forumRepo.TumKonulariGetirAsync();
        }

        public async Task KonuOlusturAsync(KonuOlusturViewModel model, int userId, string username)
        {
            var yeniKonu = new ForumKonu
            {
                Title = model.Baslik,
                Content = model.Icerik,
                AssetTag = model.VarlıkTag,
                CreatedAt = DateTime.Now,
                Stats = new ForumStats { Views = 0, Likes = 0 },
                Author = new ForumAuthor
                {
                    UserId = userId,
                    Username = username,
                    Badge = "Standart"
                },
                Comments = new List<ForumComment>()
            };
            await _forumRepo.KonuEkleAsync(yeniKonu);
        }

        public async Task<KonuDetayViewModel> KonuDetayGetirAsync(string id)
        {
            var konuEntity = await _forumRepo.KonuGetirIdIleAsync(id);
            if (konuEntity == null) return null;

            // Mapping (Entity -> ViewModel dönüşümü)
            return new KonuDetayViewModel
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
        }

        public async Task CevapYazAsync(string konuId, string mesaj, string ustYorumId, int userId, string username)
        {
            var trSaati = DateTime.UtcNow.AddHours(3);

            // Logic: Üst yorum ID varsa bu bir Yanıttır (Reply), yoksa Yorumdur (Comment)
            if (!string.IsNullOrEmpty(ustYorumId) && int.TryParse(ustYorumId, out int parentCommentId))
            {
                var yeniYanit = new ForumReply
                {
                    Username = username,
                    Text = mesaj,
                    Date = trSaati
                };
                await _forumRepo.YanitEkleAsync(konuId, parentCommentId, yeniYanit);
            }
            else
            {
                var yeniYorum = new ForumComment
                {
                    CommentId = new Random().Next(100000, 999999),
                    UserId = userId,
                    Username = username,
                    Text = mesaj,
                    Date = trSaati,
                    Replies = new List<ForumReply>()
                };
                await _forumRepo.YorumEkleAsync(konuId, yeniYorum);
            }
        }

        public async Task YorumSilAsync(string konuId, string yorumId, int userId)
        {
            if (int.TryParse(yorumId, out int silinecekYorumId))
            {
                await _forumRepo.YorumSilAsync(konuId, silinecekYorumId, userId);
            }
        }
    }
}