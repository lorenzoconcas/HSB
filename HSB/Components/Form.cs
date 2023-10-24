namespace HSB.Components;

public class Form
{
    private Dictionary<string, string> parts = new();
    public Form(string body)
    {
        var values = body.Split("&");
        foreach (var v in values)
        {
            var d = v.Split("=");

            if (!parts.ContainsKey(d[0]))
                parts.Add(d[0], d[1]);
        }
    }

    public string Get(string value)
    {
        return parts[value] ?? "";
    }
}