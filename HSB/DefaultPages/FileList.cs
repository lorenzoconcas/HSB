using HSB.Constants;
using System.Runtime.InteropServices;
using HSB.Utils.HtmlUtils;

namespace HSB.DefaultPages;

public class FileList(Request req, Response res, Configuration configuration)
{
    public void Get()
    {
        //since this mode bypasses normal error handling we must handle them manually
        try
        {
            var cd = Environment.CurrentDirectory;
            //if a static file path is set use that as the current directory
            if (configuration.StaticFolderPath != "")
            {
                cd = configuration.StaticFolderPath;
            }

            var url = NormalizeIfWindows(req.Url.Replace("%20", " "));
            var cwd = cd;
            var rootRequested = req.Url == "/";


            /*
             * If path == / -> list files and folders of the cd (Environment.CurrentDirectory)
             * If path == / + subdirectory -> list file of subdirectory
             * if path == / + file -> download
             *
             * Disk path == cd + requested path (url), if url starts with "..", we remove it before concatenating with cd
             */


            //if is file -> download 

            var filePath = Path.Combine(cwd, url[1..]);

            if (File.Exists(filePath))
            {
                configuration.Debug.INFO($"{req.Method} '{url}' 200 (Static file)");
                res.SendFile(filePath);
                return;
            }

            var fileList = "";
            if (req.Url != "/")
            {
                fileList = "<div class='row'><a href='/'>.</a><br/></div>";
                fileList += $"<div class='row'><a href='{Path.GetRelativePath(cd, cwd)}'>..</a></br/></div>";
                cwd = Path.Combine(cd, url[1..]);
            }


            //if requested path is directory -> list files
            if (Directory.Exists(cwd))
            {
                configuration.Debug.INFO($"{req.Method} '{url}' 200");
                List<string> items = [.. Directory.GetDirectories(cwd)];
                items.AddRange(Directory.GetFiles(cwd).ToList());
                foreach (var i in items)
                {
                    var filename = Path.GetFileName(i);
                    var relativePath = i.Replace(cd, ""); //the path is relative to the current directory (cd)
                    fileList += $"<div class='row'><a href='{relativePath}'>{filename}</a></div>";
                }

                var content = $"""
                               <div class="scrollable-list">
                                {fileList}
                               </div>
                               """;


                var folder = rootRequested ? " / " : url[1..];
                var title = $"Directory listing for {folder}";
                var builder = new PageBuilder(title);
                builder.AddCard(
                    title,
                    content,
                    PageBuilder.GetFooterString(configuration)
                );


                res.SendHTMLContent(builder.Render());
                
            }

            configuration.Debug.INFO($"{req.Method} '{url}' 404 (Resource not found)");
            new Error(res, configuration, "Page not found", HttpCodes.NOT_FOUND).Throw();
        }
        catch (Exception e)
        {
            configuration.Debug.ERROR($"{req.Method} '{req.Url}' 500 (Internal Server Error)\n{e}");
            new Error(res, configuration, e.ToString(), HttpCodes.INTERNAL_SERVER_ERROR).Throw();
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