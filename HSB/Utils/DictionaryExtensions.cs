namespace HSB.Utils;

public static class DictionaryExtensions
{

    public static string DictToString(this Dictionary<string, string> obj)
    {
        var s = "";
        foreach (var v in obj)
        {
            s += v.Key + " - " + v.Value + "\n";
        }

        return s;
    }
}