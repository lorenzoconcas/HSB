using System.Text;
using HSB.Constants;
using HSB.Utils;

namespace HSB.Components;

/// <summary>
/// Represents a file of a multipart form
/// </summary>
public class FilePart : FormPart
{
    public readonly string ContentType;
    public readonly string FileName;
    private readonly string? tempFilePath;
    private readonly bool ownsTempFile;
    private readonly long size;

    public FilePart(byte[] data) : base(data)
    {
        //first line is Content-Disposition
        //second line is Content-Type
        //third and forth line are CRLF
        //the rest is the data

        FileName = ContentDisposition.Split(";")[2].Split("=")[1].Replace("\"", "").Replace("\r\n", "");

        int contentTypeLineStart = MemoryExtensions.IndexOf(data, "\r\n"u8.ToArray());
        int contentTypeLineEnd = MemoryExtensions.IndexOf(data, "\r\n\r\n"u8.ToArray());
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
        size = Data.LongLength;
    }

    internal FilePart(
        string name,
        string contentDisposition,
        string fileName,
        string contentType,
        string tempFilePath,
        long size,
        bool ownsTempFile) : base(contentDisposition, name, [])
    {
        FileName = fileName;
        ContentType = contentType;
        this.tempFilePath = tempFilePath;
        this.ownsTempFile = ownsTempFile;
        this.size = size;
    }

    public string GetMimeType()
    {
        return ContentType;
    }

    public override long Length => size;

    public byte[] GetBytes()
    {
        if (tempFilePath != null)
        {
            return File.ReadAllBytes(tempFilePath);
        }

        return Data;
    }

    internal Stream OpenReadStream()
    {
        if (tempFilePath != null)
        {
            return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        }

        return new MemoryStream(Data, writable: false);
    }

    public void SaveToDisk(string path)
    {
        string _path;
        if (Path.HasExtension(path))
            _path = path;
        else
        {
            _path = Path.Combine(path, FileName);
            if (PathUtils.IsUnsafePath(FileName))
            {
                var detectedExt = MimeTypeUtils.GetExtension(ContentType);
                if (detectedExt == "")
                    detectedExt = ".bin";
                _path = Path.Combine(path,
                "file_" + GenericUtils.GenerateRandomString(4) + "." + detectedExt);
            }
        }

        if (tempFilePath != null)
        {
            File.Copy(tempFilePath, _path, overwrite: true);
            return;
        }

        File.WriteAllBytes(_path, Data);
    }

    internal void Cleanup()
    {
        if (!ownsTempFile || tempFilePath == null)
        {
            return;
        }

        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch
        {
            // Best effort cleanup for request-scoped temp files.
        }
    }

    public override string ToString()
    {
        return $"Filename : {FileName} | Content-Type : {ContentType} | Size {((int)Math.Min(int.MaxValue, Length)).AsSizeHumanReadable()}";
    }
}
