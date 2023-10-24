﻿using System.Text;

namespace HSB.Components;

public class MultiPartFormData
{
    private readonly List<FormPart> parts = new();
    private byte[] body;
    private readonly string Boundary;

    public MultiPartFormData(byte[] body, string boundary)
    {
        Boundary = boundary;
        this.body = body;
    }

    private void ExtractParts()
    {
        var boundaryBytes = Encoding.UTF8.GetBytes("--" + Boundary);

        body = body[..^2];//remove last \r\n
        body = body[..^2]; //remove last "--"
        body = body[..^boundaryBytes.Length]; //remove trailing boundary 

        var partsData = body.Split(boundaryBytes);
        partsData.RemoveAt(0);//skip first byte array, it's empty
        foreach (var part in partsData)
        {
            parts.Add(FormPart.Build(part[2..])); //remove \r\n at start
        }

        body = Array.Empty<byte>();

    }
    /// <summary>
    /// Returns all parts of the forms which are not files
    /// </summary>
    /// <returns></returns>
    public List<FormPart> GetParts()
    {
        if (body.Length > 0)
            ExtractParts();
        return parts
        .Where(p => p is not FilePart)
        .ToList();
    }

    public List<FilePart> GetFiles()
    {
        if (body.Length > 0)
            ExtractParts();
        return
            parts
            .Where(p => p is FilePart)
            .Select(p => (FilePart)p) //filter and cast to FilePart 
            .ToList();
    }

}
