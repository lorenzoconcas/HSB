using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;
public class Info
{
    public Info(string title,
        string description,
        string? termsOfService,
        Contact? contact,
        License? license,
        string? version)
    {
        Title = title;
        Description = description;
        TermsOfService = termsOfService;
        Contact = contact;
        License = license;
        Version = version;
    }

    public Info(string title, string description)
    {
        this.Title = title;
        this.Description = description;
    }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("termsOfService")]
    public string? TermsOfService { get; set; }

    [JsonPropertyName("contact")]
    public Contact? Contact { get; set; }

    [JsonPropertyName("license")]
    public License? License { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}