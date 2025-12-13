using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinansUygulmasi.Models.Entities
{
    [Table("MarketHistory")]
    public class MarketHistory
    {
        [Key]
        [Column("history_id")]
        public int HistoryId { get; set; }

        [Column("asset_id")]
        public int AssetId { get; set; }

        [Column("price", TypeName = "decimal(15, 2)")]
        public decimal Price { get; set; }

        [Column("recorded_at")]
        public DateTime RecordedAt { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }
    }
}