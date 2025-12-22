using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinansUygulmasi.Models.Entities
{
    public class vw_PortfolioDetails
    {
        public int user_id { get; set; } //
        public int asset_id { get; set; }
        public string symbol { get; set; }
        public string name { get; set; }
        public decimal amount { get; set; } //
        public decimal average_cost { get; set; }
    }
}