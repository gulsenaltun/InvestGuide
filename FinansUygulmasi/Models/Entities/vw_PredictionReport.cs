using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinansUygulmasi.Models.Entities
{
    public class vw_PredictionReport
    {
        public string name { get; set; }
        public decimal predicted_price { get; set; }
        public DateTime target_date { get; set; }
        public int confidence_score { get; set; }
    }
}