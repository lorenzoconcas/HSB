using HSB;
using HSB.Components;
using HSB.Constants;
namespace Runner;


[Binding("/fileupload.html")]
[Binding("/fileuploadmulti.html")]
[Binding("/fileupload")]
public class FileUpload(Request req, Response res) : Servlet(req, res)
{
    private const string savePath = "./uploaded";

    public override void GET()
    {
        switch (req.Url)
        {
            case "/fileupload.html":
                res.SendHTMLContent("<form action=\"/fileupload\" method=\"post\" enctype=\"multipart/form-data\">" +
                                    "<input type=\"text\" name=\"value1\" id=\"value1\"></input>" +
                                    "<input type=\"file\" name=\"fileToUpload\" id=\"fileToUpload\">" +
                                    "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
                                    "</form>");
                break;
            case "/fileupload":
                res.SendHTMLContent("<h1>File Uploaded</h1>");
                break;
            case "/fileuploadmulti.html":
                res.SendHTMLContent("<form action=\"/fileupload\" method=\"post\" enctype=\"multipart/form-data\">" +
                                    "<input type=\"file\" name=\"fileToUpload\" id=\"fileToUpload\">" +
                                    "<input type=\"file\" name=\"fileToUpload2\" id=\"fileToUpload2\">" +
                                    "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
                                    "</form>");
                break;
            default:
                res.SendHTMLContent("<h1>404 Not Found</h1>");
                break;
        }
    }

    public override void POST()
    {
        if (req.Url == "/fileupload" && req.IsFileUpload())
        {
            MultiPartFormData? data = req.GetMultiPartFormData();
            if (data != null)
            {
                var files = data.GetFiles();
                if (files.Count < 0)
                {
                    res.SendCode(HttpCodes.NOT_ACCEPTABLE);
                    return;
                }
                if (!Path.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                    foreach (var f in files)
                    {
                        f.SaveToDisk(savePath);
                        Terminal.INFO(f);
                    }
                }
                res.SendFile(files.First()); //send first file to che client
                return;
            }
            res.SendCode(HttpCodes.NOT_ACCEPTABLE);
        }
        else
        {
            res.SendCode(HttpCodes.FORBIDDEN);
        }

    }
}