namespace HSB.Components.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DocumentClass : Attribute
{
    private readonly string description;

    public DocumentClass(string description)
    {
        this.description = description;
    }

    public string Description
    {
        get { return description; }
    }
}
