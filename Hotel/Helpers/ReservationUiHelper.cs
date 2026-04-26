using Hotel.Enums;

namespace Hotel.Helpers
{
    public static class ReservationUiHelper
    {
        public static string GetReservationStatusText(ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "Потвърдена",
                ReservationStatus.Confirmed => "Потвърдена",
                ReservationStatus.ArrivingToday => "Пристигащ днес",
                ReservationStatus.CheckedIn => "Настанен",
                ReservationStatus.CheckingOutToday => "Напускащ днес",
                ReservationStatus.CheckedOut => "Напуснал",
                ReservationStatus.Cancelled => "Отказана",
                _ => status.ToString()
            };
        }
    }
}