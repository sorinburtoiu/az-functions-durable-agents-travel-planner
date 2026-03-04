using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Newtonsoft.Json.Serialization;
using TravelPlannerFunctions.Models;

namespace TravelPlannerFunctions.Models;

public record TravelPlanningStatusResponse(
    string Id,
    string StatusQueryUrl
);

[ExcludeFromCodeCoverage]
public class TravelPlanningStatusResponseExample : OpenApiExample<TravelPlanningStatusResponse>
{
    public override IOpenApiExample<TravelPlanningStatusResponse> Build(NamingStrategy namingStrategy = null)
    {
        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Valid response",
            TravelPlanningStatusResponseExamples.ValidResponse,
            namingStrategy));

        return this;
    }
}

public static class TravelPlanningStatusResponseExamples
{
    public static readonly TravelPlanningStatusResponse ValidResponse = new(
        Id: "a1b2c3d4e5f6",
        StatusQueryUrl: "/api/travel-planner/status/a1b2c3d4e5f6"
    );
}