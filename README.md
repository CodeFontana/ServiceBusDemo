# ServiceBusDemo

This project demonstrates a simple Azure Service Bus workflow using .NET.

## Attribution

This code is an updated adaptation of Tim Corey's YouTube video on Azure Service Bus:  
https://www.youtube.com/watch?v=v52yC9kq0Yg

## What This Project Shows

- Sending messages to an Azure Service Bus queue or topic
- Receiving and processing messages from Service Bus
- Running against a local Service Bus implementation for demo/testing scenarios
- Basic local development workflow for testing message-based communication

### Option 1: Local Demo Mode (No Azure Required)

- Use the local Service Bus implementation included in this project

### Option 2: Service Bus Emulator in Docker

- Docker Desktop (or another local Docker runtime)
- A Service Bus emulator container running locally

### Option 3: Azure Service Bus

- An Azure subscription
- An Azure Service Bus namespace with at least one queue or topic/subscription
- A valid Service Bus connection string

## Configuration

1. Clone or open the project locally.
2. Choose your target transport:
   - local implementation
   - Docker-based Service Bus emulator
   - Azure Service Bus
3. Add the corresponding connection details to app configuration (for example, `appsettings.json` or user secrets, depending on project setup).
4. Verify queue/topic settings match the selected target.

## Running the Demo

1. Build the project.
2. Start the sender component to publish messages.
3. Start the receiver component to process messages.
4. Confirm messages are sent and received successfully in console output (and Azure metrics if using Azure Service Bus).

## Notes

- Keep secrets out of source control (use environment variables or user secrets).
- This project is intended as a learning/demo application and can be extended with retries, dead-letter handling, and structured logging.
