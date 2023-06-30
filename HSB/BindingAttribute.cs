using System;
namespace HSB
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class Binding : Attribute
    {
        private string path;
        private bool startsWith;

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

