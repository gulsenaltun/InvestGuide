using Microsoft.EntityFrameworkCore;
using FinansUygulmasi.Models;
using FinansUygulmasi.Models.Entities;
using System.Collections.Generic;

namespace FinansUygulmasi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } // Users tablosuna erişim sağlar
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<UserAsset> UserAssets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<MarketHistory> MarketHistory { get; set; }
        public DbSet<Prediction> Predictions { get; set; }

    }
}