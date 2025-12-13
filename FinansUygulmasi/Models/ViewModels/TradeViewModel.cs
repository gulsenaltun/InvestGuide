namespace FinansUygulmasi.Models.ViewModels
{
    public class TradeViewModel
    {
        public string Symbol { get; set; }        // Örn: GOLD
        public decimal CurrentPrice { get; set; } // Örn: 2450.00
        public decimal Amount { get; set; }       // Girilen Miktar
        public decimal TotalPrice { get; set; }   // Toplam Tutar (Miktar * Fiyat)
        public decimal WalletBalance { get; set; } // Cüzdan Bakiyesi
    }
}