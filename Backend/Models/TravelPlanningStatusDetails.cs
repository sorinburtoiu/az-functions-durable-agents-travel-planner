using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Newtonsoft.Json.Serialization;
using TravelPlannerFunctions.Models;

namespace TravelPlannerFunctions.Models;

public record TravelPlanningStatusDetails(
    string InstanceId,
    string RuntimeStatus,
    object Input,
    object Output,
    DateTime CreatedTime,
    DateTime LastUpdatedTime
);

public static class TravelPlanningStatusDetailsExamples
{
    public static readonly TravelPlanningStatusDetails RunningExample = new(
        InstanceId: "a1b2c3d4e5f6",
        RuntimeStatus: "Running",
        Input: new { userName = "john.doe" },
        Output: null,
        CreatedTime: DateTime.Parse("2026-03-01T10:00:00Z"),
        LastUpdatedTime: DateTime.Parse("2026-03-01T10:05:00Z")
    );

    public static readonly TravelPlanningStatusDetails CompletedExample = new(
        InstanceId: "z9y8x7w6v5u4",
        RuntimeStatus: "Completed",
        Input: new { userName = "ana.popescu" },
        Output: new { result = "Flights booked." },
        CreatedTime: DateTime.Parse("2026-03-01T08:00:00Z"),
        LastUpdatedTime: DateTime.Parse("2026-03-01T08:20:00Z")
    );
}

[ExcludeFromCodeCoverage]
public class TravelPlanningStatusDetailsExample : OpenApiExample<TravelPlanningStatusDetails>
{
    public override IOpenApiExample<TravelPlanningStatusDetails> Build(NamingStrategy namingStrategy = null)
    {
        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Running Orchestration",
            TravelPlanningStatusDetailsExamples.RunningExample,
            namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Completed Orchestration",
            TravelPlanningStatusDetailsExamples.CompletedExample,
            namingStrategy));

        return this;
    }
}
