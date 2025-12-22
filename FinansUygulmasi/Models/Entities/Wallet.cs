using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinansUygulmasi.Models.Entities
{
    [Table("Wallets")]
    public class Wallet
    {
        [Key]
        [Column("wallet_id")]
        public int WalletId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("balance", TypeName = "decimal(15, 2)")]
        public decimal Balance { get; set; } = 500.00m;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // User ile ilişki
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}