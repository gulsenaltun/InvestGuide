using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinansUygulmasi.Models.Entities
{
    [Table("Transactions")]
    public class Transaction
    {
        [Key]
        [Column("transaction_id")]
        public int TransactionId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("asset_id")]
        public int AssetId { get; set; }

        [Required]
        [Column("type")] // 'buy' veya 'sell'
        public string Type { get; set; }

        [Column("amount", TypeName = "decimal(15, 8)")]
        public decimal Amount { get; set; }

        [Column("price_at_transaction", TypeName = "decimal(15, 2)")]
        public decimal PriceAtTransaction { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }
    }
}