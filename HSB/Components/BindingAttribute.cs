using System;
namespace HSB
{
    /// <summary>
    /// Defines the path of the servlet, optionally it can catch ALL the
    /// requests that search for path starting with the given value
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class Binding : Attribute
    {
        /// <summary>
        /// The path of the servlet
        /// </summary>
        private readonly string path;
        /// <summary>
        /// Whether or not the servlet must respond to all request where url starts with path
        /// </summary>
        private readonly bool startsWith;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">Path of the servlet</param>
        /// <param name="startsWith">Whether or not the servlet must respond to all request where url starts with path</param>
        public Binding(string path, bool startsWith = false)
        {
            this.path = path;
            this.startsWith = startsWith;
        }

        public string Path
        {
            get { return path; }
        }

        public bool StartsWith
        {
            get { return startsWith; }
        }
    }
}

