using System.Buffers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using HSB.Http;

namespace HSB.Components;

public sealed class MultiPartFormData : IDisposable
{
    private readonly List<FormPart> parts = [];
    private readonly List<FilePart> fileParts = [];
    private readonly string boundary;
    private readonly byte[]? inMemoryBody;
    private readonly string? tempBodyPath;
    private readonly Stream? sourceBodyStream;
    private readonly bool leaveSourceStreamOpen;
    private readonly UploadOptions uploadOptions;
    private readonly HttpOptions httpOptions;
    private bool parsed;
    private bool disposed;

    public MultiPartFormData(byte[] body, string boundary)
        : this(body, boundary, new UploadOptions(), new HttpOptions())
    {
    }

    internal MultiPartFormData(
        byte[] body,
        string boundary,
        UploadOptions uploadOptions,
        HttpOptions httpOptions)
    {
        inMemoryBody = body;
        this.boundary = boundary.Trim('"');
        this.uploadOptions = uploadOptions;
        this.httpOptions = httpOptions;
    }

    internal MultiPartFormData(
        Stream bodyStream,
        string boundary,
        UploadOptions uploadOptions,
        HttpOptions httpOptions,
        bool leaveSourceStreamOpen)
    {
        sourceBodyStream = bodyStream;
        this.boundary = boundary.Trim('"');
        this.uploadOptions = uploadOptions;
        this.httpOptions = httpOptions;
        this.leaveSourceStreamOpen = leaveSourceStreamOpen;
    }

    internal MultiPartFormData(
        string tempBodyPath,
        string boundary,
        UploadOptions uploadOptions,
        HttpOptions httpOptions)
    {
        tempBodyPath = Path.GetFullPath(tempBodyPath);
        this.tempBodyPath = tempBodyPath;
        this.boundary = boundary.Trim('"');
        this.uploadOptions = uploadOptions;
        this.httpOptions = httpOptions;
    }

    internal static MultiPartFormData Parse(
        Stream bodyStream,
        string boundary,
        UploadOptions uploadOptions,
        HttpOptions httpOptions,
        bool leaveSourceStreamOpen = false)
    {
        var formData = new MultiPartFormData(bodyStream, boundary, uploadOptions, httpOptions, leaveSourceStreamOpen);
        try
        {
            formData.EnsureParsed();
            return formData;
        }
        catch
        {
            formData.Dispose();
            throw;
        }
    }

    public List<FormPart> GetParts()
    {
        EnsureParsed();
        return parts.Where(p => p is not FilePart).ToList();
    }

    public List<FilePart> GetFiles()
    {
        EnsureParsed();
        return fileParts.ToList();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (var file in fileParts)
        {
            file.Cleanup();
        }

        CleanupBodyTempFile();
    }

    private void EnsureParsed()
    {
        if (parsed)
        {
            return;
        }

        parsed = true;

        Stream bodyStream;
        if (sourceBodyStream != null)
        {
            bodyStream = sourceBodyStream;
        }
        else if (tempBodyPath != null)
        {
            bodyStream = new FileStream(tempBodyPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        }
        else
        {
            bodyStream = new MemoryStream(inMemoryBody ?? [], writable: false);
        }

        try
        {
            ParseBodyStream(bodyStream);
        }
        finally
        {
            if (sourceBodyStream == null || !leaveSourceStreamOpen)
            {
                bodyStream.Dispose();
            }

            CleanupBodyTempFile();
        }
    }

    private void ParseBodyStream(Stream bodyStream)
    {
        var reader = new MultipartReader(boundary, bodyStream)
        {
            BodyLengthLimit = httpOptions.MaxBodySizeBytes,
            HeadersCountLimit = httpOptions.MaxHeaders,
            HeadersLengthLimit = httpOptions.MaxHeaderSizeBytes
        };

        MultipartSection? section;
        while ((section = reader.ReadNextSectionAsync().GetAwaiter().GetResult()) != null)
        {
            ParseSection(section);
        }
    }

    private void ParseSection(MultipartSection section)
    {
        if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
        {
            throw new MultipartParseException("Invalid multipart content disposition", Constants.HttpCodes.BAD_REQUEST);
        }

        var name = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value ?? string.Empty;
        var contentDispositionValue = section.ContentDisposition ?? string.Empty;

        if (contentDisposition.IsFileDisposition())
        {
            var fileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileNameStar).Value
                           ?? HeaderUtilities.RemoveQuotes(contentDisposition.FileName).Value
                           ?? "upload.bin";

            var mimeType = NormalizeMimeType(section.ContentType);
            var tempFilePath = CreateTempFilePath(fileName);
            long written = 0;
            var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);

            try
            {
                using var target = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.SequentialScan);
                while (true)
                {
                    var read = section.Body.Read(buffer, 0, 64 * 1024);
                    if (read <= 0)
                    {
                        break;
                    }

                    written += read;
                    if (written > uploadOptions.MaxFileSizeBytes)
                    {
                        target.Dispose();
                        TryDeleteFile(tempFilePath);
                        throw new MultipartParseException("Multipart file exceeds configured limit", Constants.HttpCodes.PAYLOAD_TOO_LARGE);
                    }

                    target.Write(buffer, 0, read);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            var filePart = new FilePart(
                name,
                contentDispositionValue,
                fileName,
                mimeType,
                tempFilePath,
                written,
                ownsTempFile: true);

            fileParts.Add(filePart);
            parts.Add(filePart);
            return;
        }

        if (!contentDisposition.IsFormDisposition())
        {
            throw new MultipartParseException("Unsupported multipart section disposition", Constants.HttpCodes.BAD_REQUEST);
        }

        using var memory = new PooledByteBuffer(256);
        var fieldBuffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        long total = 0;

        try
        {
            while (true)
            {
                var read = section.Body.Read(fieldBuffer, 0, 16 * 1024);
                if (read <= 0)
                {
                    break;
                }

                total += read;
                if (total > uploadOptions.MaxFormFieldSizeBytes)
                {
                    throw new MultipartParseException("Multipart form field exceeds configured limit", Constants.HttpCodes.PAYLOAD_TOO_LARGE);
                }

                memory.Append(fieldBuffer.AsSpan(0, read));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(fieldBuffer);
        }

        parts.Add(new FormPart(contentDispositionValue, name, memory.ToArray()));
    }

    private string NormalizeMimeType(string? rawMimeType)
    {
        if (string.IsNullOrWhiteSpace(rawMimeType))
        {
            return Constants.MimeTypeUtils.APPLICATION_OCTET;
        }

        var normalized = rawMimeType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        if (!uploadOptions.RejectInvalidMimeType)
        {
            return normalized;
        }

        var slashIndex = normalized.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == normalized.Length - 1)
        {
            throw new MultipartParseException("Invalid multipart file mime type", Constants.HttpCodes.UNSUPPORTED_MEDIA_TYPE);
        }

        foreach (var ch in normalized)
        {
            if (char.IsControl(ch) || char.IsWhiteSpace(ch))
            {
                throw new MultipartParseException("Invalid multipart file mime type", Constants.HttpCodes.UNSUPPORTED_MEDIA_TYPE);
            }
        }

        return normalized;
    }

    private string CreateTempFilePath(string originalFileName)
    {
        var root = Path.GetFullPath(uploadOptions.TempPath);
        Directory.CreateDirectory(root);
        var safeName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeName);
        return Path.Combine(root, $"{Guid.NewGuid():N}{extension}");
    }

    private void CleanupBodyTempFile()
    {
        if (tempBodyPath == null)
        {
            return;
        }

        TryDeleteFile(tempBodyPath);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}

internal sealed class MultipartParseException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
