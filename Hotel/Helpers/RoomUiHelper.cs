using Hotel.Enums;

namespace Hotel.Helpers
{
    public static class RoomUiHelper
    {
        public static string GetRoomStatusText(RoomStatus status)
        {
            return status switch
            {
                RoomStatus.Available => "Свободна",
                RoomStatus.Reserved => "Резервирана",
                RoomStatus.Occupied => "Заета",
                RoomStatus.Maintenance => "Ремонт",
                _ => status.ToString()
            };
        }

        public static string GetRoomStatusClass(RoomStatus status)
        {
            return status switch
            {
                RoomStatus.Available => "badge bg-success",
                RoomStatus.Reserved => "badge bg-warning text-dark",
                RoomStatus.Occupied => "badge bg-danger",
                RoomStatus.Maintenance => "badge bg-secondary",
                _ => "badge bg-dark"
            };
        }

        public static string GetRoomTypeText(RoomType type)
        {
            return type switch
            {
                RoomType.StandardNoBalcony => "Стандартна стая без балкон",
                RoomType.Standard => "Стандартна стая",
                RoomType.Superior => "Супериорна стая",
                RoomType.Studio => "Студио",
                RoomType.FamilySuperior => "Фамилна супериорна стая",
                _ => type.ToString()
            };
        }

        public static int GetDefaultCapacity(RoomType type)
        {
            return type switch
            {
                RoomType.StandardNoBalcony => 2,
                RoomType.Standard => 2,
                RoomType.Superior => 2,
                RoomType.Studio => 3,
                RoomType.FamilySuperior => 4,
                _ => 2
            };
        }

        public static decimal GetDefaultPrice(RoomType type)
        {
            return type switch
            {
                RoomType.StandardNoBalcony => 74.00m,
                RoomType.Standard => 84.00m,
                RoomType.Superior => 99.00m,
                RoomType.Studio => 112.00m,
                RoomType.FamilySuperior => 139.00m,
                _ => 0m
            };
        }
    }
}