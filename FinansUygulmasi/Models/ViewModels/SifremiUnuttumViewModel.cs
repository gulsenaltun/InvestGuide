using System.ComponentModel.DataAnnotations;

namespace FinansUygulmasi.Models.ViewModels
{
    public class SifremiUnuttumViewModel
    {
        [Display(Name = "E-Posta Adresi")]
        [Required(ErrorMessage = "Lütfen e-posta adresinizi giriniz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100, ErrorMessage = "E-posta adresi en fazla 100 karakter olabilir.")]
        public string Email { get; set; }
    }
}
