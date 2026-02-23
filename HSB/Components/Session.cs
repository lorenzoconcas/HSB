namespace HSB;

public class SessionManager
{
    private readonly Dictionary<string, Session> data = [];
    private static SessionManager? _instance = null;
    private SessionManager()
    {
    }

    public static SessionManager GetInstance()
    {
        _instance ??= new SessionManager();
        return _instance;
    }

    public void CreateSession(string uuid, Session sessionData)
    {
        data.Add(uuid, sessionData);
    }

    public string CreateSession(Session sessionData)
    {
        //generate a string with a random uuid
        string uuid;
        do
        {
            uuid = Guid.NewGuid().ToString().Replace("-", "");

        } while (data.ContainsKey(uuid));
        CreateSession(uuid, sessionData);
        return uuid;
    }

    public Session GetSession(string token)
    {
        return data[token];
    }

    internal bool IsValidSession(string value)
    {
        return data.ContainsKey(value);
    }
}

public class Session
{
    protected internal bool Valid = false;
    public long ExpirationTime { get; set; }
    private readonly Dictionary<string, object> attributes;


    public Session()
    {
        attributes = [];
        ExpirationTime = -1;
        //  SourceIP = "";
    }

    public Session(long expirationTime)
    {

        ExpirationTime = expirationTime;
        attributes = [];
        Valid = true;
    }



    public T? GetAttribute<T>(string name)
    {
        return (T)attributes[name] ?? default;
    }


    public void SetAttribute<T>(string name, T item)
    {
        attributes.Add(name, item!);
    }

}

