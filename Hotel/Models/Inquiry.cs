using System.ComponentModel.DataAnnotations;

namespace Hotel.Models
{
    public class Inquiry
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Име")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Настаняване")]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Напускане")]
        public DateTime CheckOut { get; set; }

        [Range(1, 20)]
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