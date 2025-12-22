using FinansUygulmasi.Models.Entities;
using FinansUygulmasi.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinansUygulmasi.Services.Interfaces
{
    public interface IForumService
    {
        Task<List<ForumKonu>> KonulariListeleAsync();
        Task KonuOlusturAsync(KonuOlusturViewModel model, int userId, string username);
        Task<KonuDetayViewModel> KonuDetayGetirAsync(string id);
        
        // Cevap yazma mantığı burada birleşir
        Task CevapYazAsync(string konuId, string mesaj, string ustYorumId, int userId, string username);
        Task YorumSilAsync(string konuId, string yorumId, int userId);
    }
}