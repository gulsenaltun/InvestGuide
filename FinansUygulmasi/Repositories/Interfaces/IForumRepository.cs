using FinansUygulmasi.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinansUygulmasi.Repositories.Interfaces
{
    public interface IForumRepository
    {
        Task<List<ForumKonu>> TumKonulariGetirAsync();
        Task<ForumKonu> KonuGetirIdIleAsync(string id);
        Task KonuEkleAsync(ForumKonu konu);
        
        // Yorum ve Yanıt ekleme işlemleri update gerektirir
        Task YorumEkleAsync(string konuId, ForumComment yorum);
        Task YanitEkleAsync(string konuId, int parentCommentId, ForumReply yanit);
        Task YorumSilAsync(string konuId, int yorumId, int userId);
    }
}