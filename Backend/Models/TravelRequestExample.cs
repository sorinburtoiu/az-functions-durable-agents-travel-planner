using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Newtonsoft.Json.Serialization;

namespace TravelPlannerFunctions.Models;

[ExcludeFromCodeCoverage]
public class TravelRequestExample : OpenApiExample<TravelRequest>
{
    /// <summary>
    /// Builds example TravelRequest objects for Swagger.
    /// </summary>
    /// <param name="namingStrategy">JSON naming strategy.</param>
    /// <returns>The examples collection.</returns>
    public override IOpenApiExample<TravelRequest> Build(NamingStrategy namingStrategy = null)
    {
        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Valid request",
            TravelRequestExamples.ValidRequest,  namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Valid request without special requirements",
            TravelRequestExamples.ValidRequestWithoutSpecialRequirements,
            namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Missing username",
            TravelRequestExamples.MissingUserName,
            namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Invalid duration",
            TravelRequestExamples.InvalidDuration,
            namingStrategy));

        return this;
    }
}

public static class TravelRequestExamples
{
    public static readonly TravelRequest ValidRequest = new(
        UserName: "john.doe",
        Preferences: "Beach, adventure activities",
        DurationInDays: 7,
        Budget: "1500 EUR",
        TravelDates: "2026-06-01 to 2026-06-08",
        SpecialRequirements: "Vegetarian meals"
    );

    public static readonly TravelRequest ValidRequestWithoutSpecialRequirements = new(
        UserName: "ana.popescu",
        Preferences: "City breaks, museums",
        DurationInDays: 3,
        Budget: "500 EUR",
        TravelDates: "2026-04-12 to 2026-04-15",
        SpecialRequirements: null
    );

    public static readonly TravelRequest MissingUserName = new(
        UserName: null,
        Preferences: "Mountains, hiking",
        DurationInDays: 5,
        Budget: "800 EUR",
        TravelDates: "2026-09-10 to 2026-09-15",
        SpecialRequirements: "Allergy‑friendly accommodation"
    );

    public static readonly TravelRequest InvalidDuration = new(
        UserName: "invalid.user",
        Preferences: "Relaxation",
        DurationInDays: -3, // invalid duration
        Budget: "1000 EUR",
        TravelDates: "2026-05-01 to 2026-05-04",
        SpecialRequirements: null
    );
}
