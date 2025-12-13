using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinansUygulmasi.Models
{
    [Table("Assets")]
    public class Asset//varlık
    {
        [Key]
        [Column("asset_id")] 
        public int AssetId { get; set; }

        [Required]
        [StringLength(10)]
        [Column("symbol")]
        public string Symbol { get; set; } 
        [Required]
        [StringLength(50)]
        [Column("name")]
        public string Name { get; set; } 

        [Required]
        [Column("type")]
        public string Type { get; set; } 
    }
}