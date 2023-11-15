using HSB.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HSB.DefaultPages;

public class FileList(Request req, Response res, Configuration config) : Servlet(req, res, config)
{
    public override void ProcessGet()
    {
        //since this mode bypasses normal error handling we must handle them manually
        try
        {
            var cd = Environment.CurrentDirectory;
            var url = NormalizeIfWindows(req.URL.Replace("%20", " "));
            var cwd = cd;
            var rootRequested = req.URL == "/";


            /**
             * If path == / -> list files and folders of the cd (Environment.CurrentDirectory
             * If path == / + subdirectory -> list file of subdirectory
             * if path == / + file -> download
             * 
             * Disk path == cd + requested path (url)
             *
             **/


            //if is file -> download 
            if (File.Exists("." + url))
            {
                configuration.Debug.INFO($"{req.METHOD} '{url}' 200 (Static file)");
                res.SendFile("." + url);
                return;
            }

            var filelist = "";
            if (req.URL != "/")
            {
                filelist = "<div class='row'><a href='/'>.</a><br/></div>";
                filelist += $"<div class='row'><a href='{Path.GetRelativePath(cd, cwd)}'>..</a></br/></div>";
                cwd = Path.Combine(cd, url[1..]);
            }



            //if requested path is directory -> list files
            if (Directory.Exists(cwd))
            {

                configuration.Debug.INFO($"{req.METHOD} '{url}' 200");
                List<string> items = [.. Directory.GetDirectories(cwd)];
                items.AddRange(Directory.GetFiles(cwd).ToList());
                foreach (var i in items)
                {
                    var filename = Path.GetFileName(i);
                    var relativePath = i.Replace(cd, ""); //the path is relative to the current directory (cd)
                    filelist += $"<div class='row'><a href='{relativePath}'>{filename}</a></div>";
                }



                string version = "";
                if (Assembly.GetExecutingAssembly().GetName().Version != null)
                {
                    version = "v"+Assembly.GetExecutingAssembly().GetName().Version!.ToString();
                }


                string footer_div;
                string server_name;
                if (configuration.CustomServerName != "")
                {
                    server_name = configuration.CustomServerName;
                    footer_div = "";
                }
                else
                {

                    footer_div = "<div class=\"footer\">Copyright &copy; 2021-2023 Lorenzo L. Concas</div>";
                    server_name = "HSB<sup>#</sup>";
                }
                res.AddAttribute("folder", rootRequested ? " / " : url[1..]);
                res.AddAttribute("items", filelist);
                res.AddAttribute("footer_div", footer_div);
                res.AddAttribute("serverName", server_name);
                res.AddAttribute("footer_div", footer_div);
                res.AddAttribute("hsbVersion", version);
                res.AddAttribute("footerExtra", "- File Listing Mode");
                string page = ReadFromResources("filelisting.html");
                res.SendHTMLContent(page, true);
                return;
            }

            configuration.Debug.INFO($"{req.METHOD} '{url}' 404 (Resource not found)");
            new Error(req, res, configuration, "Page not found", HTTP_CODES.NOT_FOUND).Process();
        }
        catch(Exception e)
        {
            configuration.Debug.ERROR($"{req.METHOD} '{req.URL}' 500 (Internal Server Error)\n{e}");         
            new Error(req, res, configuration, e.ToString(), HTTP_CODES.INTERNAL_SERVER_ERROR).Process();
        }
    }

    private static string NormalizeIfWindows(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("/", "\\");
        }

        return url;
    }
}
