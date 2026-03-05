# How Microsoft Agent Framework (MAF) Is Used

## Azure-Samples/durable-agents-travel-planner (C#)


## Overview

This project demonstrates how to build **durable AI agents** using:

-   Azure Functions
-   Durable Task Framework
-   Microsoft Agents AI libraries (MAF-related packages)
-   Azure OpenAI

In this solution:

-   **Microsoft Agent Framework components** are used for AI reasoning,
    structured outputs, and tool calling.
-   **Durable Functions** handle orchestration, state management, human
    approval, and long-running workflows.

------------------------------------------------------------------------

# 1. NuGet Packages (MAF-Related Libraries)

The Azure Functions project references these key packages:

-   `Microsoft.Agents.AI.DurableTask`
-   `Microsoft.Agents.AI.OpenAI`
-   `Microsoft.Agents.AI.Hosting.AzureFunctions`

These libraries enable:

-   Agent creation and execution
-   Integration with Azure OpenAI
-   Durable orchestration support
-   Agent threads and state persistence

This is where the Microsoft Agent Framework capabilities are brought
into the project.

------------------------------------------------------------------------

# 2. Agent Registration in Program.cs

In `Program.cs`, the application configures durable agents using:

``` csharp
.ConfigureDurableAgents(...)
```

Three AI agents are registered:

-   `DestinationRecommenderAgent`
-   `ItineraryPlannerAgent`
-   `LocalRecommendationsAgent`

Each agent factory:

1.  Creates an Azure OpenAI chat client
2.  Defines system instructions (prompt)
3.  Assigns a name
4.  Registers dependencies via dependency injection

Example conceptually:

``` csharp
new AzureOpenAIClient(...)
    .GetChatClient(deploymentName)
    .CreateAIAgent(...)
```

This is how MAF is used to define and configure agents at startup.

------------------------------------------------------------------------

# 3. Tool Calling (Function Invocation from the LLM)

The Itinerary agent is configured with tools using:

``` csharp
AIFunctionFactory.Create(CurrencyConverterTool.ConvertCurrency)
AIFunctionFactory.Create(CurrencyConverterTool.GetExchangeRate)
```

This allows the language model to:

-   Call C# methods during generation
-   Fetch exchange rates
-   Convert currency dynamically

The agent prompt instructs the model to use these tools when needed.

This is a core Microsoft Agent Framework feature: **LLM → structured
tool invocation → C# method execution → result returned to LLM**

------------------------------------------------------------------------

# 4. Using Agents Inside the Durable Orchestrator

In `TravelPlannerOrchestrator.cs`, the orchestration retrieves agents
from context:

``` csharp
DurableAIAgent destinationAgent = context.GetAgent("DestinationRecommenderAgent");
DurableAIAgent itineraryAgent = context.GetAgent("ItineraryPlannerAgent");
DurableAIAgent localRecommendationsAgent = context.GetAgent("LocalRecommendationsAgent");
```

Each agent gets its own thread:

``` csharp
AgentThread destinationThread = destinationAgent.GetNewThread();
```

Agents are then executed with strongly-typed outputs:

``` csharp
destinationAgent.RunAsync<DestinationRecommendations>(...)
itineraryAgent.RunAsync<TravelItinerary>(...)
localRecommendationsAgent.RunAsync<LocalRecommendations>(...)
```

The outputs are mapped directly into C# models.

Some agents run in parallel using:

``` csharp
Task.WhenAll(...)
```

This shows how MAF integrates with **Durable Task** for scalable, parallel
AI workflows.

------------------------------------------------------------------------

# 5. Durable Workflow Around the Agents

The system uses classic Durable Functions patterns:

## HTTP Trigger

Starts orchestration:

``` csharp
ScheduleNewOrchestrationInstanceAsync(...)
```

## Activities

Side effects like: - Saving to storage - Booking travel - Sending
notifications

``` csharp
context.CallActivityAsync(...)
```

## Human-in-the-Loop

Approval event pattern:

``` csharp
RaiseEventAsync(instanceId, "ApprovalEvent", ...)
```

Durable Functions handle: - State persistence - Long-running execution -
Event-driven approvals

Agents focus purely on AI reasoning.

------------------------------------------------------------------------

# Architectural Separation

| Responsibility | Technology |
|---------------|------------|
| AI reasoning, prompts, tool calls | Microsoft Agent Framework | 
| Workflow orchestration | Durable Task| 
| Long-running state | Durable Functions |
| External IO |Activities | 
| Human approval | Durable external events |

------------------------------------------------------------------------

# Summary

In this repository, Microsoft Agent Framework is used to:

1.  Define AI agents with system prompts
2.  Enable structured outputs into C# models
3.  Allow tool calling (LLM → C# functions)
4.  Maintain agent threads for stateful conversations
5.  Integrate with Durable Task for orchestration

Durable Functions manage the workflow and infrastructure, while MAF
provides the AI intelligence layer.

------------------------------------------------------------------------

# High-Level Flow

User Request\
    ↓\
Durable Orchestrator Starts\
    ↓\
Destination Agent Runs\
    ↓\
Itinerary + Local Recommendation Agents Run (Parallel)\
    ↓\
Optional Human Approval\
    ↓\
Final Travel Plan Returned

------------------------------------------------------------------------


