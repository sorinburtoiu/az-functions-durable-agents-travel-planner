using System.Net;
using System.Net.Mime;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using TravelPlannerFunctions.Models;

namespace TravelPlannerFunctions.Functions;

public class TravelPlannerApi
{
    private readonly ILogger _logger;

    public TravelPlannerApi(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TravelPlannerApi>();
    }

    [Function(nameof(StartTravelPlanning))]
    [OpenApiOperation(tags: ["Travel Planner"],
    Summary = "Starts travel planning process.",
    Description = "Initiates the travel planning process for a given travel request.<BR>Possible statuses after starting:<BR>" +
    "✓ Running - The travel planning process is running.<BR>" +
    "✓ Completed - The travel planning process has completed successfully.<BR>" +
    "The status remains the same if the process fails.")]
    [OpenApiRequestBody(contentType: MediaTypeNames.Application.Json, bodyType: typeof(TravelRequest), Example = typeof(TravelRequestExample), Description = "Details of the travel request")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Accepted,
        contentType: MediaTypeNames.Application.Json,
        bodyType: typeof(TravelPlanningStatusResponse),
        Example = typeof(TravelPlanningStatusResponseExample),
        Description = "The status of the travel planning process"
    )]
    public async Task<HttpResponseData> StartTravelPlanning(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travel-planner")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Travel planning request received");

        // Parse the request
        TravelRequest travelRequest;
        try
        {
            travelRequest = await req.ReadFromJsonAsync<TravelRequest>() 
                ?? throw new InvalidOperationException("Invalid request body");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse travel request");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("Invalid request format");
            return errorResponse;
        }

        // Start the orchestration
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(TravelPlannerOrchestrator.RunTravelPlannerOrchestration), travelRequest);

        _logger.LogInformation("Started orchestration with ID = {instanceId}", instanceId);

        // Return a response with the status URL
        var response = req.CreateResponse(HttpStatusCode.Accepted);
        
        // Add a Location header that points to the status endpoint
        response.Headers.Add("Location", $"/api/travel-planner/status/{instanceId}");
        

        var payload = new TravelPlanningStatusResponse(
            Id: instanceId,
            StatusQueryUrl: $"/api/travel-planner/status/{instanceId}"
        );

        await response.WriteAsJsonAsync(payload);

        return response;
    }

[Function(nameof(GetTravelPlanningStatus))]
[OpenApiOperation(
    tags: ["Travel Planner"],
    Summary = "Gets the status of a travel planning orchestration.",
    Description = "Returns the current runtime status and details of a travel planning orchestration instance.")]
[OpenApiParameter(
    name: "instanceId",
    In = ParameterLocation.Path,
    Required = true,
    Type = typeof(string),
    Description = "The orchestration instance ID.")]
[OpenApiResponseWithBody(
    statusCode: HttpStatusCode.OK,
    contentType: MediaTypeNames.Application.Json,
    bodyType: typeof(TravelPlanningStatusDetails),
    Example = typeof(TravelPlanningStatusDetailsExample),
    Description = "Details of the orchestration status.")]
[OpenApiResponseWithoutBody(
    statusCode: HttpStatusCode.NotFound,
    Description = "No orchestration with the specified ID exists.")]
public async Task<HttpResponseData> GetTravelPlanningStatus(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "travel-planner/status/{instanceId}")] HttpRequestData req,
    string instanceId,
    [DurableClient] DurableTaskClient client)
{
    _logger.LogInformation("Getting status for orchestration with ID = {instanceId}", instanceId);

    var status = await client.GetInstanceAsync(instanceId, true);

    if (status == null)
    {
        var notFound = req.CreateResponse(HttpStatusCode.NotFound);
        await notFound.WriteStringAsync($"No orchestration found with ID = {instanceId}");
        return notFound;
    }

    var responseModel = new TravelPlanningStatusDetails(
        InstanceId: status.InstanceId,
        RuntimeStatus: status.RuntimeStatus.ToString(),
        Input: status.ReadInputAs<string>(),
        Output: status.ReadOutputAs<string>(),
        CreatedTime: status.CreatedAt ,
        LastUpdatedTime: status.LastUpdatedAt
    );

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(responseModel);
    return response;
}

[Function(nameof(HandleApprovalResponse))]
[OpenApiOperation(
    tags: ["Travel Planner"],
    Summary = "Handles approval or rejection from the user.",
    Description = "Receives an approval or rejection response and forwards it to the travel planning orchestration.")]
[OpenApiParameter(
    name: "instanceId",
    In = ParameterLocation.Path,
    Required = true,
    Type = typeof(string),
    Description = "The orchestration instance ID to send the approval response to.")]
[OpenApiRequestBody(
    contentType: MediaTypeNames.Application.Json,
    bodyType: typeof(ApprovalResponse),
    Example = typeof(ApprovalResponseExample),
    Required = true,
    Description = "Approval response payload.")]
[OpenApiResponseWithBody(
    statusCode: HttpStatusCode.OK,
    contentType: MediaTypeNames.Application.Json,
    bodyType: typeof(ApprovalProcessingResult),
    Example = typeof(ApprovalProcessingResultExample),
    Description = "Indicates that the approval response was successfully processed.")]
[OpenApiResponseWithoutBody(
    statusCode: HttpStatusCode.BadRequest,
    Description = "Invalid approval response format.")]
public async Task<HttpResponseData> HandleApprovalResponse(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travel-planner/approve/{instanceId}")] HttpRequestData req,
    string instanceId,
    [DurableClient] DurableTaskClient client)
{
    _logger.LogInformation("Received approval response for orchestration with ID = {instanceId}", instanceId);

    // Parse the approval response
    ApprovalResponse approvalResponse;
    try
    {
        approvalResponse = await req.ReadFromJsonAsync<ApprovalResponse>()
            ?? throw new InvalidOperationException("Invalid approval response");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to parse approval response");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Invalid approval response format");
        return badResponse;
    }

    // Send to orchestration
    _logger.LogInformation("Sending approval response to orchestration: Approved = {approved}", approvalResponse.Approved);
    await client.RaiseEventAsync(instanceId, "ApprovalEvent", approvalResponse);

    // Build clean response model
    var result = new ApprovalProcessingResult(
        InstanceId: instanceId,
        Message: "Approval response processed successfully",
        Approved: approvalResponse.Approved
    );

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(result);
    return response;
}



   [Function(nameof(GetTripConfirmationStatus))]
[OpenApiOperation(
    tags: ["Travel Planner"],
    Summary = "Gets the booking confirmation status.",
    Description = "Returns whether the trip was confirmed, rejected, or is still pending based on the orchestration output."
)]
[OpenApiParameter(
    name: "instanceId",
    In = ParameterLocation.Path,
    Required = true,
    Type = typeof(string),
    Description = "The orchestration instance ID."
)]
[OpenApiResponseWithBody(
    statusCode: HttpStatusCode.OK,
    contentType: MediaTypeNames.Application.Json,
    bodyType: typeof(TripConfirmationStatusResponse),
    Example = typeof(TripConfirmationStatusResponseExample),
    Description = "The confirmation status for the trip."
)]
[OpenApiResponseWithoutBody(
    statusCode: HttpStatusCode.NotFound,
    Description = "No orchestration instance was found with the provided ID."
)]
public async Task<HttpResponseData> GetTripConfirmationStatus(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "travel-planner/confirmation/{instanceId}")] HttpRequestData req,
    string instanceId,
    [DurableClient] DurableTaskClient client)
{
    _logger.LogInformation("Getting confirmation status for orchestration with ID = {instanceId}", instanceId);

    var status = await client.GetInstanceAsync(instanceId, true);

    if (status == null)
    {
        var notFound = req.CreateResponse(HttpStatusCode.NotFound);
        await notFound.WriteStringAsync($"No orchestration found with ID = {instanceId}");
        return notFound;
    }

    bool isConfirmed = false;
    bool isRejected = false;
    string confirmationMessage = "";

    if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed &&
        status.ReadOutputAs<object>() != null)
    {
        try
        {
            var jsonOutput = status.ReadOutputAs<System.Text.Json.JsonElement>();

            if (jsonOutput.TryGetProperty("bookingConfirmation", out var bookingProperty) ||
                jsonOutput.TryGetProperty("BookingConfirmation", out bookingProperty))
            {
                string? raw = bookingProperty.GetString();

                if (!string.IsNullOrEmpty(raw))
                {
                    if (raw.Contains("Booking confirmed"))
                    {
                        isConfirmed = true;
                        confirmationMessage = raw;
                    }
                    else if (raw.Contains("not approved"))
                    {
                        isRejected = true;
                        confirmationMessage = raw;
                    }
                }
            }
            else
            {
                string fallback = jsonOutput.ToString();
                if (fallback.Contains("Booking confirmed"))
                {
                    isConfirmed = true;
                    confirmationMessage = fallback;
                }
                else if (fallback.Contains("not approved"))
                {
                    isRejected = true;
                    confirmationMessage = fallback;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing orchestration output");
        }
    }

    var payload = new TripConfirmationStatusResponse(
        InstanceId: instanceId,
        IsConfirmed: isConfirmed,
        IsRejected: isRejected,
        ConfirmationMessage: confirmationMessage,
        RuntimeStatus: status.RuntimeStatus.ToString()
    );

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(payload);

    return response;
}

}