using Hotel.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hotel.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на госта е задължително.")]
        [StringLength(100, ErrorMessage = "Името на госта не може да бъде по-дълго от 100 символа.")]
        public string GuestName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефонът е задължителен.")]
        [StringLength(30, ErrorMessage = "Телефонният номер не може да бъде по-дълъг от 30 символа.")]
        [RegularExpression(@"^[0-9+\-\s()]+$", ErrorMessage = "Телефонният номер може да съдържа само цифри, интервали, +, - и скоби.")]
        public string Phone { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Настаняване")]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Напускане")]
        public DateTime CheckOut { get; set; }

        [Range(1, 20, ErrorMessage = "Броят гости трябва да бъде между 1 и 20.")]
        public int GuestsCount { get; set; }

        public bool IsPaid { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Сумата трябва да бъде положителна.")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Моля, изберете стая.")]
        public int? RoomId { get; set; }

        public Room? Room { get; set; }

        [Required(ErrorMessage = "Статусът е задължителен.")]
        public ReservationStatus Status { get; set; }
    }
}