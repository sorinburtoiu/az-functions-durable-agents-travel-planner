using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Newtonsoft.Json.Serialization;

namespace TravelPlannerFunctions.Models;

[ExcludeFromCodeCoverage]
public class ApprovalResponseExample : OpenApiExample<ApprovalResponse>
{
    public override IOpenApiExample<ApprovalResponse> Build(NamingStrategy namingStrategy = null)
    {
        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Approval granted",
            new ApprovalResponse(true,"Approved by user"),
            namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Approval denied",
            new ApprovalResponse(false,"Rejected by user"),
            namingStrategy));

        return this;
    }
}


[ExcludeFromCodeCoverage]
public class ApprovalProcessingResultExample : OpenApiExample<ApprovalProcessingResult>
{
    public override IOpenApiExample<ApprovalProcessingResult> Build(NamingStrategy namingStrategy = null)
    {
        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Approved",
            ApprovalProcessingResultExamples.ApprovedResponse,
            namingStrategy));

        Examples.Add(OpenApiCustomExampleResolver.Resolve(
            "Rejected",
            ApprovalProcessingResultExamples.RejectedResponse,
            namingStrategy));

        return this;
    }
}

public static class ApprovalProcessingResultExamples
{
    public static readonly ApprovalProcessingResult ApprovedResponse = new(
        InstanceId: "a1b2c3d4e5f6",
        Message: "Approval response processed successfully",
        Approved: true
    );

    public static readonly ApprovalProcessingResult RejectedResponse = new(
        InstanceId: "z9y8x7w6v5u4",
        Message: "Approval response processed successfully",
        Approved: false
    );
}

public record ApprovalProcessingResult(
    string InstanceId,
    string Message,
    bool Approved
);
