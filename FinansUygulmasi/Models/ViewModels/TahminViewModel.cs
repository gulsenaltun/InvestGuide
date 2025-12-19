using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinansUygulmasi.Models.ViewModels
{
    public class TahminViewModel
    {
        public string Symbol { get; set; }
        public double CurrentPrice { get; set; }
        public double PredictedPrice { get; set; }
        public string Date { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}