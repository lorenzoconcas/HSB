using System;
namespace HSB
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class Binding : Attribute
    {
        private string path;

        public Binding(string path)
        {
            this.path = path;
        }

        public string Path
        {
            get { return path; }
        }
    }
}

