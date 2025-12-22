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

        public DbSet<vw_PortfolioDetails> PortfolioDetails { get; set; }
        public DbSet<vw_UserSummary> UserSummaries { get; set; }
        public DbSet<vw_PredictionReport> PredictionReports { get; set; }
        public DbSet<vw_UserList> UserList { get; set; }
        public DbSet<vw_UserBalance> UserBalances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<vw_PortfolioDetails>().HasNoKey().ToView("vw_PortfolioDetails");
            modelBuilder.Entity<vw_UserSummary>().HasNoKey().ToView("vw_UserSummary");
            modelBuilder.Entity<vw_PredictionReport>().HasNoKey().ToView("vw_PredictionReport");
            modelBuilder.Entity<vw_UserList>().HasNoKey().ToView("vw_UserList");
            modelBuilder.Entity<vw_UserBalance>().HasNoKey().ToView("vw_UserBalance");
        }
    }
}