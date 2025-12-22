using Microsoft.AspNetCore.Mvc;
using FinansUygulmasi.Data;
using FinansUygulmasi.Models.ViewModels;
using FinansUygulmasi.Models.Entities;
using MongoDB.Driver;
using System.Linq;
using System.Collections.Generic;
using System;

namespace FinansUygulmasi.ViewComponents
{
    public class SonMesajlarViewComponent : ViewComponent
    {
        private readonly MongoDbContext _context;

        public SonMesajlarViewComponent(MongoDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            // YORUMLARI DEĞİL, KONULARI GETİREN KOD
            var sonKonular = _context.Tartismalar.Find(x => true)
                .SortByDescending(x => x.CreatedAt) // Konunun oluşturulma tarihi
                .Limit(5) // Son 5 konu
                .ToList()
                .Select(x => new MesajViewModel
                {
                    KullaniciAdi = x.Author.Username ?? "Anonim",

                    // Konu içeriğini göster
                    Icerik = (x.Content ?? "").Length > 40
                             ? (x.Content ?? "").Substring(0, 40) + "..."
                             : (x.Content ?? ""),

                    Tarih = x.CreatedAt, // Konunun açıldığı tarih

                    KonuBasligi = x.Title // Konu başlığı
                })
                .ToList();

            return View(sonKonular);
        }
    }
}