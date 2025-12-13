using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinansUygulmasi.Models.Entities
{
    [Table("Predictions")]
    public class Prediction
    {
        [Key]
        [Column("prediction_id")]
        public int PredictionId { get; set; }

        [Column("asset_id")]
        public int AssetId { get; set; }

        [Column("predicted_price", TypeName = "decimal(15, 2)")]
        public decimal PredictedPrice { get; set; }

        [Column("target_date")]
        public DateTime TargetDate { get; set; }

        [Column("confidence_score")]
        public byte ConfidenceScore { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }
    }
}