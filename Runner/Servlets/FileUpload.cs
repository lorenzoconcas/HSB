using HSB;
using HSB.Components;
using HSB.Components.Controller;
using HSB.Constants;

namespace Runner;

[Controller("")]
public class FileUpload
{
    private const string SavePath = "./uploaded";
    private Request req = null!;
    private Response res = null!;

    [Get("/fileupload.html")]
    private void GetSingleUpload()
    {
        res.SendHtmlContent("<form action=\"/fileupload\" method=\"post\" enctype=\"multipart/form-data\">" +
                            "<input type=\"text\" name=\"value1\" id=\"value1\"></input>" +
                            "<input type=\"file\" name=\"fileToUpload\" id=\"fileToUpload\">" +
                            "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
                            "</form>");
    }

    [Get("/fileuploadmulti.html")]
    private void GetMultiUpload()
    {
        res.SendHtmlContent("<form action=\"/fileupload\" method=\"post\" enctype=\"multipart/form-data\">" +
                            "<input type=\"file\" name=\"fileToUpload\" id=\"fileToUpload\">" +
                            "<input type=\"file\" name=\"fileToUpload2\" id=\"fileToUpload2\">" +
                            "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
                            "</form>");
    }

    [Get("/fileupload")]
    private void GetUpload()
    {
        res.SendHtmlContent("<h1>File Uploaded</h1>");
    }

    [Post("/fileupload")]
    private void Upload()
    {
        if (!req.IsFileUpload())
        {
            res.SendCode(HttpCodes.FORBIDDEN);
            return;
        }

        MultiPartFormData? data = req.GetMultiPartFormData();
        if (data == null)
        {
            res.SendCode(HttpCodes.NOT_ACCEPTABLE);
            return;
        }

        var files = data.GetFiles();
        if (files.Count == 0)
        {
            res.SendCode(HttpCodes.NOT_ACCEPTABLE);
            return;
        }

        if (!Path.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }

        foreach (var file in files)
        {
            file.SaveToDisk(SavePath);
            Terminal.Info(file);
        }

        res.SendFile(files.First());
    }
}
