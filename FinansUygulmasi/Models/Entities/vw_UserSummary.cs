using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinansUygulmasi.Models.Entities
{
    public class vw_UserSummary
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public decimal balance { get; set; }
    }
}