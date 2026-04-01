using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Helpers
{
    public static class EnumSelectListHelper
    {
        public static List<SelectListItem> CreateSelectList<TEnum>(
            Func<TEnum, string> textSelector,
            TEnum? selectedValue = null) where TEnum : struct, Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(value => new SelectListItem
                {
                    Value = value.ToString(),
                    Text = textSelector(value),
                    Selected = selectedValue.HasValue && EqualityComparer<TEnum>.Default.Equals(value, selectedValue.Value)
                })
                .ToList();
        }
    }
}