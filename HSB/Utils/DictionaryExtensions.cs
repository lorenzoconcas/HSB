namespace HSB.Utils;

public static class DictionaryExtensions
{

    public static string DictToString(this Dictionary<string, string> obj)
    {
        return obj.Aggregate("", (current, v) => current + (v.Key + " - " + v.Value + "\n"));
    }
}