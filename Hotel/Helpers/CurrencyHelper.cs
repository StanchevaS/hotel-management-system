namespace Hotel.Helpers
{
    public static class CurrencyHelper
    {
        public static string ToEuro(decimal value)
        {
            return $"{value:F2} €";
        }
    }
}