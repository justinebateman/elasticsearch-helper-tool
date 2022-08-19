using System.ComponentModel;

namespace ElasticsearchHelperTool.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum enumValue)
    {
        var field = enumValue.GetType().GetField(enumValue.ToString());
        if (field is null)
        {
            throw new ArgumentException($"Could not find item in {enumValue.GetType()}", nameof(enumValue));
        }
        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        return "";
    }
}
