using HSB.OpenApi;
using HSB.Components;
using HSB.Utils;

namespace HSB.Modules;

[Module(ModuleType.Service, name: "OpenApi Module", author: "The HSB Team",
    description: "This module scans sets up a complete OpenAPI json and a swagger endpoint")]
public class OpenApi
{
    [ModuleInvokeMethod]
    public ModuleExitCode Run(Configuration config)
    {
        if (config.OpenApiSettings.Mode is Mode.Disabled)
        {
            return ModuleExitCode.Continue;
        }

        OpenApiBuilder.BuildOpenApiYaml(config, config.GetDetectedRoutes());

        if (config.OpenApiSettings.Mode is Mode.Full or Mode.FileOnly)
        {
            File.WriteAllText(config.OpenApiSettings.FilePath,
                config.GetSharedObject("openapi.json") as string);
        }


        return ModuleExitCode.Continue;
    }

    private static void SetEndpoints(Configuration configuration)
    {
        configuration.Get("/openapi.json", (Request request, Response res) =>
        {
            var openApiJson = configuration.GetSharedObject("openapi.json");
            res.SendObject(openApiJson, "openapi.json", 200, "application/json");
            //send the openapi.yaml file
            //res.SendFile("openapi.json", "application/json");
        });
        configuration.Get(configuration.OpenApiSettings.Path, (Request Request, HSB.Response res) =>
        {
            var page = ResourceUtils.LoadResource<string>("swagger_ui.html") ??
                       throw new Exception("Resource not found");
            res.SendHtmlContent(page);
        });
    }
}