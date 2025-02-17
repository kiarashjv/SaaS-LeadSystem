# SaaS Lead Management System

A microservices-based lead management system for SaaS companies, featuring AI-based lead qualification and automated CMS integration.

## System Architecture

The system consists of three microservices:

1. **ServerX (Main Server)**

   - Core orchestration service
   - Handles incoming lead submissions
   - Coordinates between AI evaluation and CMS storage
   - Implements fallback mechanisms and error handling

2. **ServerA (AI Evaluation)**

   - Simulated AI service for lead qualification
   - Evaluates leads based on criteria
   - Provides both queue and HTTP-based interfaces

3. **ServerY (CMS)**
   - Manages qualified lead storage
   - Provides lead retrieval and management
   - Implements in-memory storage (for demo purposes)

## Key Features

- Message queue-based communication (RabbitMQ)
- HTTP fallback mechanisms
- Retry policies for failed requests
- Comprehensive error handling
- Detailed logging
- CORS support
- Swagger API documentation

## Risk Analysis & Mitigation

1. **Message Queue Failures**

   - Risk: RabbitMQ service unavailability
   - Mitigation: HTTP fallback implementation
   - Reference: LeadEvaluationService.cs (lines 50-97)

2. **Network Timeouts**

   - Risk: Slow or unresponsive services
   - Mitigation:
     - 10-second timeout for queue operations
     - Retry policy for HTTP requests
     - Reference: CmsService.cs (lines 34-86)

3. **Data Consistency**

   - Risk: Lead data inconsistency across services
   - Mitigation:
     - Email-based deduplication
     - Atomic operations in storage service
     - Reference: LeadStorageService.cs (lines 23-39)

4. **Service Dependencies**

   - Risk: Cascading failures
   - Mitigation:
     - Independent service operation
     - Circuit breaker pattern (TODO)
     - Graceful degradation

5. **Data Validation**
   - Risk: Invalid lead data
   - Mitigation:
     - Input validation at API level
     - Reference: ServerX LeadsController.cs (lines 27-31)

## Setup Instructions

1. **Prerequisites**

   - .NET 7.0 or later
   - RabbitMQ server
   - Visual Studio 2022 or VS Code

2. **RabbitMQ Setup**

   ```bash
   # Install RabbitMQ (Windows)
   choco install rabbitmq

   # Or using Docker
   docker run -d --hostname my-rabbit --name my-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:management
   ```

3. **Project Setup**

   ```bash
   # Clone repository
   git clone https://github.com/kiarashjv/SaaS-LeadSystem.git
   cd SaaS-LeadSystem

   # Build solution
   dotnet build

   # Run services (in separate terminals)
   cd src/ServerX
   dotnet run

   cd src/ServerA
   dotnet run

   cd src/ServerY
   dotnet run
   ```

## API Documentation

### ServerX API

1. **Submit Lead for Evaluation**

   ```http
   POST /api/Leads/evaluate
   Content-Type: application/json

   {
     "name": "John Doe",
     "email": "john@example.com",
     "phone": "1234567890",
     "companyName": "Example Corp"
   }
   ```

### ServerY API

1. **Get All Qualified Leads**

   ```http
   GET /api/Leads
   ```

2. **Get Lead by Email**

   ```http
   GET /api/Leads/{email}
   ```

## Development Guidelines

1. **Error Handling**

   - Always use try-catch blocks in controllers
   - Implement proper logging
   - Return appropriate HTTP status codes

2. **Message Queue Usage**

   - Use queues for primary communication
   - Implement HTTP fallback
   - Handle timeout scenarios

3. **Testing**
   - Unit tests (TODO)
   - Integration tests (TODO)
   - Load testing (TODO)

## Future Improvements

1. Circuit breaker implementation
2. Distributed tracing
3. Metrics collection
4. Containerization
5. Unit and integration tests
6. Frontend implementation
7. Persistent storage
8. Authentication and authorization

## License

MIT
