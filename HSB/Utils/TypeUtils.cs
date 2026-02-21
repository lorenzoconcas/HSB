namespace HSB.Utils;

public static class TypeUtils
{
    public static string MapTypeToOpenApiType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return "integer";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return "number";
        if (type == typeof(bool))
            return "boolean";
        if (type.IsArray)
            return "array";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return "array";
        if(type == typeof(DateTime))
            return "string"; // OpenAPI uses string format for dates
        
        
        // Default to string for unknown types
        return "Object";
    }
    
    public static object ConvertToType(string value, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(int))
                return int.Parse(value);
            if (targetType == typeof(long))
                return long.Parse(value);
            if (targetType == typeof(short))
                return short.Parse(value);
            if (targetType == typeof(byte))
                return byte.Parse(value);
            if (targetType == typeof(float))
                return float.Parse(value);
            if (targetType == typeof(double))
                return double.Parse(value);
            if (targetType == typeof(decimal))
                return decimal.Parse(value);
            if (targetType == typeof(bool))
                return bool.Parse(value);
            if (targetType == typeof(DateTime))
                return DateTime.Parse(value);

            // For complex types, you might want to implement a more robust conversion logic
            throw new NotSupportedException($"Conversion to type {targetType.Name} is not supported.");
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to convert '{value}' to type {targetType.Name}.", ex);
        }
    }
}