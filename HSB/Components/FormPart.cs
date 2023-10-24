using System.Text;

namespace HSB.Components;


/// <summary>
/// Represents a generic part of a multipart form
/// </summary>
public class FormPart
{
    public string ContentDisposition;
    public string Name;
    public byte[] Data;

    public FormPart(byte[] data)
    {
        int offset = Utils.IndexOf(data, Encoding.UTF8.GetBytes("\r\n")) + 2;
        ContentDisposition = Encoding.UTF8.GetString(data[..offset]);
        Name = ContentDisposition.Split(";")[1].Split("=")[1].Replace("\"", "");
        Data = data[offset..^2];

    }

    public override string ToString()
    {
        return "FormPart : " + ContentDisposition;
    }

    public static FormPart Build(byte[] data)
    {
        var formPart = new FormPart(data);
        if (formPart.ContentDisposition.Contains("filename"))
        {
            return new FilePart(data);
        }
        return formPart;
    }
}