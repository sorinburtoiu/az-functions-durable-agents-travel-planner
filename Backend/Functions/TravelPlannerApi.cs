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
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("Invalid approval response format");
            return errorResponse;
        }

        // Send the approval response to the orchestration
        _logger.LogInformation("Sending approval response to orchestration: Approved = {approved}", approvalResponse.Approved);
        await client.RaiseEventAsync(instanceId, "ApprovalEvent", approvalResponse);

        // Return a success response
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            instanceId,
            message = "Approval response processed successfully",
            approved = approvalResponse.Approved
        });

        return response;
    }



    [Function(nameof(GetTripConfirmationStatus))]
    public async Task<HttpResponseData> GetTripConfirmationStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "travel-planner/confirmation/{instanceId}")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Getting confirmation status for orchestration with ID = {instanceId}", instanceId);

        // Get the orchestration status
        var status = await client.GetInstanceAsync(instanceId, true);
        _logger.LogInformation("Confirmation status for instance = {instanceId}", status); 

        if (status == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync($"No orchestration found with ID = {instanceId}");
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        
        // Check if the booking has been confirmed
        bool isConfirmed = false;
        bool isRejected = false;
        string confirmationMessage = "";
        
        // Check if the orchestration is completed and has output with booking confirmation
        if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed && status.ReadOutputAs<object>() != null)
        {
            try
            {
                // First try to read as JSON element
                var jsonOutput = status.ReadOutputAs<System.Text.Json.JsonElement>();
                
                // Check for booking confirmation properties
                if (jsonOutput.TryGetProperty("bookingConfirmation", out var bookingConfirmationElement) || 
                    jsonOutput.TryGetProperty("BookingConfirmation", out bookingConfirmationElement))
                {
                    if (bookingConfirmationElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        string? bookingConfirmation = bookingConfirmationElement.GetString();
                        if (bookingConfirmation != null)
                        {
                            if (bookingConfirmation.Contains("Booking confirmed"))
                            {
                                isConfirmed = true;
                                confirmationMessage = bookingConfirmation;
                            }
                            else if (bookingConfirmation.Contains("not approved"))
                            {
                                isRejected = true;
                                confirmationMessage = bookingConfirmation;
                            }
                        }
                    }
                }
                // If we couldn't find the booking confirmation property, check other properties or the whole object
                else
                {
                    string outputString = jsonOutput.ToString();
                    if (!string.IsNullOrEmpty(outputString))
                    {
                        if (outputString.Contains("Booking confirmed"))
                        {
                            isConfirmed = true;
                            confirmationMessage = outputString;
                        }
                        else if (outputString.Contains("not approved"))
                        {
                            isRejected = true;
                            confirmationMessage = outputString;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orchestration output");
            }
        }
        
        await response.WriteAsJsonAsync(new
        {
            instanceId,
            isConfirmed,
            isRejected,
            confirmationMessage,
            status.RuntimeStatus
        });

        return response;
    }
}