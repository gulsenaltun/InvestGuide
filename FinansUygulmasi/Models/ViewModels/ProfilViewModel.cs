using System;
using System.Collections.Generic;

namespace FinansUygulmasi.Models.ViewModels
{
    public class ProfilViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal NakitBakiye { get; set; } // Cash Balance

        public decimal ToplamVarlikDegeri { get; set; } 
        public decimal GenelToplam { get; set; }     
        public List<VarlikDetay> Varliklar { get; set; } = new List<VarlikDetay>();
    }

    public class VarlikDetay
    {
        public string Symbol { get; set; }    
        public string Name { get; set; }       
        public decimal Miktar { get; set; }    
        public decimal OrtalamaMaliyet { get; set; } 
        public decimal GuncelFiyat { get; set; } 
        public decimal ToplamDeger { get; set; }
        public decimal KarZarar { get; set; }    
        public decimal KarZararOrani { get; set; } 
        public double PortfoyYuzdesi { get; set; }
        public decimal Yuzde { get; set; }

        public string RenkKod { get; set; }     
    }

    
}