using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinansUygulmasi.Models.Entities
{
    public class vw_UserList
    {
        public int user_id { get; set; } //
        public string username { get; set; }
        public decimal balance { get; set; } //
    }
}