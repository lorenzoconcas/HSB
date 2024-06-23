using System;
using System.Diagnostics;
namespace HSB;

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
    /// If set, the path will be automatically derived from the class name
    /// </summary>
    private readonly bool auto;

    /// <summary>
    /// Whether or not the servlet must respond to all request where url starts with path
    /// </summary>
    private readonly bool startsWith;


    public Binding()
    {
        this.auto = true;
        this.path = "";
        this.startsWith = false;
    }

    public Binding(string path, bool startsWith = false)
    {
        this.path = path;
        this.startsWith = startsWith;
        this.auto = false;
    }

    public string Path
    {
        get { return path; }
    }

    public bool StartsWith
    {
        get { return startsWith; }
    }

    public bool Auto
    {
        get { return auto; }
    }
}

