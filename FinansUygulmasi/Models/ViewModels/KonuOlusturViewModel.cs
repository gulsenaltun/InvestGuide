using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinansUygulmasi.Models.ViewModels
{
    public class KonuOlusturViewModel
    {
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [Display(Name = "Konu Başlığı")]
        public string Baslik { get; set; }

        [Required(ErrorMessage = "İçerik zorunludur.")]
        [Display(Name = "Mesajınız")]
        public string Icerik { get; set; }

        [Display(Name = "İlgili Varlık")]
        public string VarlıkTag { get; set; } = "GENEL";
    }
    public class KonuDetayViewModel
    {
        public string Id { get; set; }
        public string Baslik { get; set; }
        public string Icerik { get; set; }
        public string YazarAdi { get; set; }
        public string YazarRozet { get; set; } // Rozet eklendi
        public string Tarih { get; set; }
        public int Goruntulenme { get; set; }
        public int Begeni { get; set; }
        public List<YorumViewModel> Yorumlar { get; set; }
    }

    public class YorumViewModel
    {
        public string CommentId { get; set; }
        public string YazarAdi { get; set; }
        public int? YazarId { get; set; } // Silme yetkisi kontrolü için
        public string Icerik { get; set; }
        public string Tarih { get; set; }
        public List<YanitViewModel> Yanitlar { get; set; } // İç içe cevaplar
    }

    public class YanitViewModel
    {
        public string YazarAdi { get; set; }
        public string Icerik { get; set; }
        public string Tarih { get; set; }
    }
}