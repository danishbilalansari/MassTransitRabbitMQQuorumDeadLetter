using MassTransit;
using System.Text.RegularExpressions;

/// <summary>
/// Custom entity name formatter that converts class names to kebab-case.
/// </summary>
public class KebabCaseEntityNameFormatter : IEntityNameFormatter
{
    public string FormatEntityName<T>()
    {
        return ToKebabCase(typeof(T).Name);
    }

    private string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return Regex.Replace(value, "(?<!^)([A-Z])", "-$1").ToLower(); // Convert to kebab-case
    }
}