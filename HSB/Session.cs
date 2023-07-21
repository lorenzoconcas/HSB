using System;
namespace HSB
{
    public class Session
    {
        private Dictionary<string, SessionData> data = new();
        private static Session? instance = null;
        private Session()
        {
        }

        public static Session GetSession()
        {
            if (instance == null)
                instance = new();
            return instance;
        }

        public void CreateSession(string UUID, SessionData sessionData)
        {
            data.Add(UUID, sessionData);
        }

        public string CreateSession(SessionData sessionData)
        {
            //generate a string with a random uuid
            string uuid;
            do
            {
                uuid = Guid.NewGuid().ToString();

            } while (data.ContainsKey(uuid));
            CreateSession(uuid, sessionData);
            return uuid;
        }

        
        
    }

    public class SessionData
    {
        Dictionary<string, string> cookies {get; set;}
        private string sourceIP { get; set; }
        long expirationTime { get; set; }
        //for future
        //Authentication Data


        public SessionData()
        {

        }
        
        public SessionData(string sourceIP, long expirationTime, Dictionary<string, string>? cookies = null)
        {
            this.sourceIP = sourceIP;
            this.expirationTime = expirationTime;
            this.cookies = cookies ?? new Dictionary<string, string>();
        }
        
        
    }
}

