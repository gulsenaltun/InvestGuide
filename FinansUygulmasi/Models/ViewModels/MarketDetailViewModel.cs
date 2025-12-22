using System;

namespace FinansUygulmasi.Models.ViewModels
{
    public class MarketDetailViewModel
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal ChangeRate { get; set; }


        public string IconClass { get; set; }
        public string ColorClass { get; set; }
        public string AIComment { get; set; }


        public decimal PredictedPrice { get; set; }
        public int ConfidenceScore { get; set; }
        public DateTime TargetDate { get; set; }
        public string PredictionDirection { get; set; }

        public bool IsSuccess {  get; set; }
        public string Message { get; set; }

        public decimal AltinFiyat { get; set; }
        public decimal DolarKur { get; set; }
        public decimal EuroKur { get; set; }
        public decimal BitcoinFiyat { get; set; }
    }
}