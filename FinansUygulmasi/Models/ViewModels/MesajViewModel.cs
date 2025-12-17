namespace FinansUygulmasi.Models.ViewModels
{
    public class MesajViewModel
    {
        // MongoDB ObjectId veya Guid string olduğu için int yerine string yapıyoruz
        public string Id { get; set; }
        public int YorumId { get; set; }
        public string KonuBasligi { get; set; } 
        public string KullaniciAdi { get; set; }
        public string Icerik { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime Tarih { get; set; }

        public string TarihFormatli => Tarih.ToString("HH:mm");
        public List<AdminYanitViewModel> Yanitlar { get; set; } = new List<AdminYanitViewModel>();
    }

    public class AdminYanitViewModel
    {
        public string KullaniciAdi { get; set; }
        public string Icerik { get; set; }
        public DateTime Tarih { get; set; }
        public string TarihFormatli => Tarih.ToString("dd MMM HH:mm");
    }
}