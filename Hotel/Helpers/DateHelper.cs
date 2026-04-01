namespace Hotel.Helpers
{
    public static class DateHelper
    {
        public static string ToBgDate(DateTime date)
        {
            return date.ToString("dd.MM.yyyy");
        }
    }
}