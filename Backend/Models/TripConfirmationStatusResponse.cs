using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.DurableTask.Client;
using Newtonsoft.Json.Serialization;

namespace TravelPlannerFunctions.Models;

public record TripConfirmationStatusResponse(
    string InstanceId,
    bool IsConfirmed,
    bool IsRejected,
    string ConfirmationMessage,
    OrchestrationRuntimeStatus RuntimeStatus
);

public static class TripConfirmationStatusResponseExamples
{
    public static readonly TripConfirmationStatusResponse ConfirmedExample = new(
        InstanceId: "abc123",
        IsConfirmed: true,
        IsRejected: false,
        ConfirmationMessage: "Booking confirmed for travel itinerary",
        RuntimeStatus: OrchestrationRuntimeStatus.Completed
    );

    public static readonly TripConfirmationStatusResponse RejectedExample = new(
        InstanceId: "xyz789",
        IsConfirmed: false,
        IsRejected: true,
        ConfirmationMessage: "Booking not approved due to invalid payment details",
        RuntimeStatus: OrchestrationRuntimeStatus.Completed
    );

    public static readonly TripConfirmationStatusResponse PendingExample = new(
        InstanceId: "pnd456",
        IsConfirmed: false,
        IsRejected: false,
        ConfirmationMessage: "",
        RuntimeStatus: OrchestrationRuntimeStatus.Running
    );
}

[ExcludeFromCodeCoverage]
public class TripConfirmationStatusResponseExample 
    : OpenApiExample<TripConfirmationStatusResponse>
{
    public override IOpenApiExample<TripConfirmationStatusResponse> Build(
        NamingStrategy namingStrategy = null)
    {
        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Confirmed",
            TripConfirmationStatusResponseExamples.ConfirmedExample,
            namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Rejected",
            TripConfirmationStatusResponseExamples.RejectedExample,
            namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Pending",
            TripConfirmationStatusResponseExamples.PendingExample,
            namingStrategy));

        return this;
    }
}