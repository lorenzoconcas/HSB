using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using HSB.Constants;

namespace HSB;

public class FilePart : FormPart
{

    public readonly string ContentType;
    public readonly string FileName;

    public FilePart(byte[] data) : base(data)
    {
        //first line is Content-Disposition
        //second line is Content-Type
        //third and forth line are CRLF
        //the rest is the data

        FileName = ContentDisposition.Split(";")[2].Split("=")[1].Replace("\"", "").Replace("\r\n", "");

        int contentTypeLineStart = data.IndexOf("\r\n"u8.ToArray());
        int contentTypeLineEnd = data.IndexOf("\r\n\r\n"u8.ToArray());
        try
        {
            ContentType = Encoding.UTF8
                .GetString(data[(contentTypeLineStart + 2)..contentTypeLineEnd])
                .Split("Content-Type: ")[1];
        }
        catch (Exception)
        {
            ContentType = MimeTypeUtils.APPLICATION_OCTET;
        }
        base.Data = data[(contentTypeLineEnd + 4)..^2]; //skip the two CRLF at the begin and the one at the end
    }

    public string GetMimeType(){
        return ContentType;
    }

    public byte[] GetBytes()
    {
        return Data;
    }
    public void SaveToDisk(string path)
    {
        string _path;
        if (Path.HasExtension(path))
            _path = path;
        else
        {
            _path = Path.Combine(path, FileName);
            if (Utils.IsUnsafePath(FileName))
            {
                var detectedExt = MimeTypeUtils.GetExtension(ContentType);
                if (detectedExt == "")
                    detectedExt = ".bin";
                _path = Path.Combine(path,
                "file_" + Utils.GenerateRandomString(4) + "." + detectedExt);
            }
        }
        
        File.WriteAllBytes(_path, Data);
    }

    public override string ToString()
    {
        return $"Filename : {FileName} | Content-Type : {ContentType} | Size {Data.Length.AsSizeHumanReadable()}";
    }

}