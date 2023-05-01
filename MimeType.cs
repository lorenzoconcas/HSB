using System;
using MimeTypes;

namespace HSB
{
    public static class MimeType
    {
        public static readonly string TEXT_PLAIN = "text/plain";
        public static readonly string TEXT_HTML = "text/html";

        public static string GetMimeType(string data)
        {
            return MimeTypeMap.GetMimeType(data);
        }
    }
}

