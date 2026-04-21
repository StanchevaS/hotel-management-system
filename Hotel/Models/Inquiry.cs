using System.ComponentModel.DataAnnotations;

namespace Hotel.Models
{
    public class Inquiry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [Display(Name = "Име")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефонът е задължителен.")]
        [StringLength(30, ErrorMessage = "Телефонният номер не може да бъде по-дълъг от 30 символа.")]
        [RegularExpression(@"^[0-9+\-\s()]+$", ErrorMessage = "Телефонният номер може да съдържа само цифри, интервали, +, - и скоби.")]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Настаняване")]
        [Required(ErrorMessage = "Датата на настаняване е задължителна.")]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Напускане")]
        [Required(ErrorMessage = "Датата на напускане е задължителна.")]
        public DateTime CheckOut { get; set; }

        [Range(1, 20, ErrorMessage = "Броят гости трябва да бъде между 1 и 20.")]
        [Display(Name = "Брой гости")]
        public int GuestsCount { get; set; }

        [Display(Name = "Предпочитана стая")]
        public string? PreferredRoom { get; set; }

        [Display(Name = "Съобщение")]
        public string? Message { get; set; }

        [Display(Name = "Статус")]
        public string Status { get; set; } = "Ново";

        [Display(Name = "Създадено на")]
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}