using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSB
{
    public class Servlet
    {
        protected Request req;
        protected Response res;

        public Servlet(Request req, Response res)
        {
            if (req == null || res == null)
                throw new Exception("Request or Response cannot be null!");
            this.req = req;
            this.res = res;
        }


        public void Process()
        {
            switch (req.METHOD)
            {
                case HTTP_METHOD.GET:
                    ProcessGet(req, res);
                    break;
                case HTTP_METHOD.POST:
                    ProcessPost(req, res);
                    break;
                case HTTP_METHOD.PUT:
                    ProcessPut(req, res);
                    break;
                case HTTP_METHOD.DELETE:
                    ProcessDelete(req, res);
                    break;
                case HTTP_METHOD.HEAD:
                    ProcessHead(req, res);
                    break;
                default:
                    Terminal.ERROR($"Can't process request, unknown HTTP method or malformed request : {req.GetRawRequest}");
                    break;

            }
        }

        public virtual void ProcessPost(Request req, Response res)
        {

        }

        public virtual void ProcessGet(Request req, Response res)
        {

        }

        public virtual void ProcessDelete(Request req, Response res)
        {

        }

        public virtual void ProcessPut(Request req, Response res)
        {

        }

        public virtual void ProcessHead(Request req, Response res)
        {

        }
    }
}
