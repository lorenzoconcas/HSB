using System.ComponentModel;
using System.Reflection;
using HSB.OpenApi.Attributes;
using HSB.OpenApi.models;
using HSB.Utils;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.OpenApi;

public class OpenApiBuilder(Configuration configuration, List<Map> routes)
{
    public void Init()
    {
        switch (configuration.OpenApiSettings.Mode)
        {
            case Mode.Disabled: return;
            case Mode.SwaggerOnly:
                BuildOpenApiYaml();
                SetEndpoints();
                break;
            case Mode.FileOnly:
                BuildOpenApiYaml();
                File.WriteAllText(configuration.OpenApiSettings.FilePath,
                    configuration.GetSharedObject("openapi.json") as string);
                break;
            case Mode.Full:
                BuildOpenApiYaml();
                SetEndpoints();
                File.WriteAllText(configuration.OpenApiSettings.FilePath,
                    configuration.GetSharedObject("openapi.json") as string);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SetEndpoints()
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
            res.SendHTMLContent(page);
        });
    }

    private Dictionary<string, PathItem> GetPaths()
    {
        var paths = new Dictionary<string, PathItem>();

        foreach (var route in routes)
        {
            //group subroutes by path
            var groups = route.SubRoutes.GroupBy(r => r.Path).ToArray();


            foreach (var group in groups)
            {
                var methods = group.ToList();

                var pathItem = new PathItem()
                {
                    Get = GetOperation(methods.FirstOrDefault(r => r.HttpMethod == HttpMethod.Get)),
                    Post = GetOperation(methods.FirstOrDefault(r => r.HttpMethod == HttpMethod.Post)),
                    Put = GetOperation(methods.FirstOrDefault(r => r.HttpMethod == HttpMethod.Put)),
                    Delete = GetOperation(methods.FirstOrDefault(r => r.HttpMethod == HttpMethod.Delete)),
                    Patch = GetOperation(methods.FirstOrDefault(r => r.HttpMethod == HttpMethod.Patch)),
                    Options = GetOperation(methods.FirstOrDefault(r => r.HttpMethod == HttpMethod.Options)),
                    Head = GetOperation(methods.FirstOrDefault(r => r.HttpMethod == HttpMethod.Head))
                };


                var suggestedTag = route.Class.GetCustomAttribute<ApiTag>()?.Tag ?? route.Path;

                pathItem.Get?.Tags = [suggestedTag];
                pathItem.Post?.Tags = [suggestedTag];
                pathItem.Put?.Tags = [suggestedTag];
                pathItem.Delete?.Tags = [suggestedTag];
                pathItem.Patch?.Tags = [suggestedTag];
                pathItem.Options?.Tags = [suggestedTag];
                pathItem.Head?.Tags = [suggestedTag];


                paths.Add(PathUtils.JoinPath(route.Path, group.Key), pathItem);
            }
        }

        return paths;
    }

    private Dictionary<string, Schema> CollectAllResponseModels()
    {
        string[] excludeList = ["System", "Microsoft", "Internal", "HSB"];
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !excludeList.Any(e => a.FullName!.StartsWith(e)));

        var models = assemblies.SelectMany(assembly => assembly.GetTypes(), (assembly, type) => new {assembly, type})
            .Where(@t => @t.type.IsClass || @t.type.IsInterface)
            .Select(@t => @t.type)
            .Where(@t => @t.GetCustomAttribute<ApiResponseModel>() != null)
            .ToList();


        var components = new Dictionary<string, Schema>();

        foreach (var model in models)
        {
            //convert class/interface to schema
            /*
             *  ["IResponse"] = new Schema("IResponse", new Dictionary<string, Schema>()
                          {
                              ["message"] = new Schema("message", []) {Type = "string"}
                          })
             */

            var modelSchema = new Dictionary<string, Schema>();
            foreach (var prop in model.GetProperties())
            {
                var propSchema = new Schema()
                {
                    Type = TypeUtils.MapTypeToOpenApiType(prop.PropertyType)
                };
                modelSchema.Add(prop.Name, propSchema);
            }

            var suggestedName = model.GetCustomAttribute<ApiResponseModel>()?.Name ?? model.Name;

            components.Add(model.Name, new Schema(suggestedName, modelSchema));
        }


        return components;
    }

    private static Operation? GetOperation(RoutableMethod routableMethod)
    {
        if (!routableMethod.IsValid) return null;
        var method = routableMethod.MethodInfo;
        var summary = method.GetCustomAttribute<ApiSummary>()?.Summary ?? "No summary provided";
        var description = method.GetCustomAttribute<ApiDescription>()?.Description ?? "No description provided";
        var operationId = method.Name;

        var parameters = method
            .GetCustomAttributes<ApiParameter>()
            .Select(p => new Parameter(p.Name, p.Description)
            {
                In = "query", // Default to query, can be adjusted based on attribute
                Required = p.Required,
                Schema = new Schema() {Type = p.Type}
            })
            .ToList();

        var responses = method
            .GetCustomAttributes<ApiResponse>()
            .ToDictionary(r => r.StatusCode.ToString(), r => new HSB.OpenApi.models.Response(r.Description)
            {
                Ref = $"#/components/schemas/{r.ResponseType}"
            });

        var operation = new Operation(
            summary,
            description,
            operationId,
            parameters,
            responses
        );
        return operation;
    }


    private void BuildOpenApiYaml()
    {
        var api = new models.OpenApi("3.0.0")
        {
            Info = configuration.OpenApiSettings.Info,
            Servers = [],
            Paths = GetPaths(),
            Components = new models.Components(CollectAllResponseModels()),
        };

        var json = System.Text.Json.JsonSerializer.Serialize(api,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });


        configuration.AddSharedObject("openapi.json", json);
    }
}