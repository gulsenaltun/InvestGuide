using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinansUygulmasi.Models.Entities
{
    [Table("UserAssets")]
    public class UserAsset
    {
        [Key]
        [Column("portfolio_id")]
        public int PortfolioId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("asset_id")]
        public int AssetId { get; set; }

        [Column("amount", TypeName = "decimal(15, 8)")]
        public decimal Amount { get; set; }

        [Column("average_cost", TypeName = "decimal(15, 2)")]
        public decimal AverageCost { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }
    }
}