using HSB.Constants;
namespace HSB
{
    public class Servlet
    {
        protected Request req;
        protected Response res;
        protected Configuration configuration;
        //in case of an unsupported http method, we can specify a generic handler
        protected Delegate? handlerFallback;

        private Dictionary<string, Delegate> CustomMethodsMap;
        public Servlet(Request req, Response res)
        {
            if (req == null || res == null)
                throw new Exception("Request or Response cannot be null!");
            this.req = req;
            this.res = res;
            configuration = new();
            CustomMethodsMap = new();
        }

        public Servlet(Request req, Response res, Configuration conf)
        {
            if (req == null || res == null)
                throw new Exception("Request or Response cannot be null!");
            this.req = req;
            this.res = res;
            configuration = conf;
            CustomMethodsMap = new();
        }

        public void AddCustomMethodHandler(string name, Delegate handler)
        {
            CustomMethodsMap.Add(name.ToUpper(), handler);
        }

        public void RemoveCustomMethodHandler(string name)
        {
            CustomMethodsMap.Remove(name);
        }


        public void Process()
        {
            switch (req.METHOD)
            {
                case HTTP_METHOD.GET:
                    ProcessGet();
                    break;
                case HTTP_METHOD.POST:
                    ProcessPost();
                    break;
                case HTTP_METHOD.PUT:
                    ProcessPut();
                    break;
                case HTTP_METHOD.DELETE:
                    ProcessDelete();
                    break;
                case HTTP_METHOD.HEAD:
                    ProcessHead();
                    break;
                case HTTP_METHOD.PATCH:
                    ProcessPatch();
                    break;
                case HTTP_METHOD.OPTIONS:
                    ProcessOptions();
                    break;
                case HTTP_METHOD.TRACE:
                    ProcessTrace();
                    break;
                case HTTP_METHOD.CONNECT:
                    ProcessConnect();
                    break;


                default:


                    if (CustomMethodsMap.ContainsKey(req.RawMethod.ToUpper()))
                    {
                        Terminal.INFO($"Custom method requested for route '{req.URL}'", true);
                        CustomMethodsMap[req.RawMethod].DynamicInvoke(req, res);
                        return;
                    }
                    if (handlerFallback != null)
                    {
                        handlerFallback.DynamicInvoke();
                        return;
                    }
                    Terminal.ERROR($"Can't process request, unknown HTTP method or malformed request : {req.GetRawRequest}", true);
                    res.SendCode(405);
                    break;

            }
        }

        public virtual void ProcessPost()
        {
            res.SendCode(405);
        }

        public virtual void ProcessGet()
        {
            res.SendCode(405);
        }

        public virtual void ProcessDelete()
        {
            res.SendCode(405);
        }

        public virtual void ProcessPut()
        {
            res.SendCode(405);
        }

        public virtual void ProcessHead()
        {
            res.SendCode(405);
        }

        public virtual void ProcessPatch()
        {
            res.SendCode(405);
        }

        public virtual void ProcessOptions()
        {
            res.SendCode(405);
        }

        public virtual void ProcessTrace()
        {
            res.SendCode(405);
        }

        public virtual void ProcessConnect()
        {
            res.SendCode(405);

        }
    }
}
