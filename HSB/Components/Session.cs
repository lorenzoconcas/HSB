namespace HSB;

public class SessionManager
{
    private readonly Dictionary<string, Session> data = new();
    private static SessionManager? instance = null;
    private SessionManager()
    {
    }

    public static SessionManager GetInstance()
    {
        instance ??= new();
        return instance;
    }

    public void CreateSession(string UUID, Session sessionData)
    {
        data.Add(UUID, sessionData);
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
    protected internal bool valid = false;
    public long ExpirationTime { get; set; }
    public readonly Dictionary<string, object> attributes;


    public Session()
    {
        attributes = new();
        ExpirationTime = -1;
        //  SourceIP = "";
    }

    public Session(long expirationTime)
    {

        ExpirationTime = expirationTime;
        attributes = new();
        valid = true;
    }



    public T? GetAttribute<T>(string name)
    {
        return (T)attributes[name];
    }


    public void SetAttribute<T>(string name, T item)
    {
        attributes.Add(name, item!);
    }

}

