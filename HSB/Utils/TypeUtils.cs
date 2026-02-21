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
}