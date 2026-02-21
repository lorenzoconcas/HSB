namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public  class ApiTag : Attribute
{
    
    public string Tag { get; }
    
    public ApiTag( string tag)
    {
        Tag = tag;
    }

}