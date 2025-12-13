using System.ComponentModel.DataAnnotations;

namespace FinansUygulmasi.Models.ViewModels
{
    public class KayitViewModel
    {
        [Display(Name = "Kullanıcı Adı")]
        [Required(ErrorMessage = "Kullanıcı adı girilmelidir.")]
        public string UserName { get; set; }

        [Display(Name = "E-Posta Adresi")]
        [Required(ErrorMessage = "E-posta girilmelidir.")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; }

        [Display(Name = "Şifre")]
        [Required(ErrorMessage = "Şifre girilmelidir.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifreniz en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // Password alanı ile aynı olmak zorunda
        [Display(Name = "Şifre Tekrar")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
    }
}
