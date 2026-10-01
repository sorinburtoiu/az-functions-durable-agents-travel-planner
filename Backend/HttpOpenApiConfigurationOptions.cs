using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace TravelPlannerFunctions.Functions;

/// <summary>
/// Configuration for main Web API Application.
/// </summary>
public class HttpOpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
    private string _version;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="version">The version of the Flood API</param>
    public HttpOpenApiConfigurationOptions(string version)
    {
        _version = version;
    }

    /// <summary>
    /// Include host name.
    /// </summary>
    public override bool IncludeRequestingHostName { get { return false; } }

    /// <summary>
    /// OpenAPI version.
    /// </summary>
    public override OpenApiVersionType OpenApiVersion { get { return OpenApiVersionType.V3; } }

    /// <summary>
    /// Web Api Informations: Version, Title or Description.
    /// They will be displayed in Swagger page.
    /// </summary>
    public override OpenApiInfo Info
    {
        get
        {
            return new OpenApiInfo
            {
                Version = _version,
                Title = "Travel Planner API",
                Description = "The Travel Planner API allows external software applications to use the AI travel planning engine to plan trips."
            };
        }
    }
}