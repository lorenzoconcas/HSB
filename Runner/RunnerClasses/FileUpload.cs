using HSB;
using HSB.Components;
using HSB.Constants;
namespace Runner;


[Binding("/fileupload.html")]
[Binding("/fileuploadmulti.html")]
[Binding("/fileupload")]
public class FileUpload : Servlet
{
    private const string savePath = "./uploaded";
    public FileUpload(Request req, Response res) : base(req, res)
    {

    }

    public override void ProcessGet()
    {
        if (req.URL == "/fileupload.html")
        {
            res.SendHTMLContent("<form action=\"/fileupload\" method=\"post\" enctype=\"multipart/form-data\">" +
            "<input type=\"file\" name=\"fileToUpload\" id=\"fileToUpload\">" +
            "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
            "</form>");
        }
        else if (req.URL == "/fileupload")
        {
            res.SendHTMLContent("<h1>File Uploaded</h1>");
        }
        else if (req.URL == "/fileuploadmulti.html")
        {
            res.SendHTMLContent("<form action=\"/fileupload\" method=\"post\" enctype=\"multipart/form-data\">" +
            "<input type=\"file\" name=\"fileToUpload\" id=\"fileToUpload\">" +
            "<input type=\"file\" name=\"fileToUpload2\" id=\"fileToUpload2\">" +
            "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
            "</form>");
        }
        else
        {
            res.SendHTMLContent("<h1>404 Not Found</h1>");
        }
    }

    public override void ProcessPost()
    {
        if (req.URL == "/fileupload" && req.IsFileUpload())
        {
            FormData? data = req.GetFormData();
            if(data != null){
                var files = data.GetFiles();
                if(files.Count < 0) {
                    res.SendCode(HttpCodes.NOT_ACCEPTABLE);
                    return;
                }
                if(!Path.Exists(savePath)){
                    Directory.CreateDirectory(savePath);
                }
                foreach(var f in files){
                    f.SaveToDisk(savePath);
                    Terminal.INFO(f);
                }              
                res.SendFile(files.First()); //send first file to che client
                return;
            }
            res.SendCode(HttpCodes.NOT_ACCEPTABLE);
        }

    }
}