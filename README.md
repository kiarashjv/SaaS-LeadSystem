# SaaS Lead Management System

A microservices-based lead management system for SaaS companies, featuring AI-based lead qualification and automated CMS integration.

## System Architecture

The system consists of three microservices and a React frontend:

1. **Frontend (React + TypeScript)**

   - Modern, responsive UI built with React
   - Form validation using Yup
   - Real-time feedback using react-hot-toast
   - Framer Motion animations
   - TypeScript for type safety

2. **ServerX (Main Server)**

   - Core orchestration service
   - Handles incoming lead submissions
   - Coordinates between AI evaluation and CMS storage
   - Implements fallback mechanisms and error handling
   - Message queue result handling

3. **ServerA (AI Evaluation)**

   - Lead qualification service
   - Evaluates leads based on specific criteria:
     - Full name validation (first and last name required)
     - Email format validation
     - Phone number validation (minimum 10 digits)
     - Company name validation (minimum 3 characters)
   - Provides both queue and HTTP-based interfaces

4. **ServerY (CMS)**
   - Manages qualified lead storage
   - Provides lead retrieval and management
   - Implements in-memory storage (for demo purposes)
   - Queue-based storage with HTTP fallback

## Key Features

- Message queue-based communication (RabbitMQ)
- HTTP fallback mechanisms
- Retry policies for failed requests
- Comprehensive error handling
- Detailed logging
- CORS support
- Swagger API documentation
- Direct Exchange pattern for RabbitMQ
- Deterministic lead evaluation logic

## Lead Evaluation Criteria

The system evaluates leads based on the following criteria:

1. **Full Name**

   - Must contain at least two words (first and last name)
   - Example: "John Smith" (valid), "John" (invalid)

2. **Email**

   - Must be in valid email format
   - Must contain '@' and '.'
   - Example: "<john.smith@company.com>" (valid), "invalid-email" (invalid)

3. **Phone Number**

   - Must contain at least 10 digits
   - Non-digit characters are ignored
   - Example: "+1 (555) 123-4567" (valid), "123" (invalid)

4. **Company Name**
   - Must be at least 3 characters long
   - Example: "ACME Corp" (valid), "AB" (invalid)

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

   - .NET 9.0 or later
   - Node.js 22 or later
   - Docker and Docker Compose
   - Visual Studio 2022 or VS Code

2. **RabbitMQ Setup**

   Create a `docker-compose.yml` file in the root directory:

   ```yaml
   services:
     rabbitmq:
       image: rabbitmq:3-management
       container_name: rabbitmq
       hostname: rabbitmq
       ports:
         - "5672:5672" # AMQP protocol port
         - "15672:15672" # Management UI port
       environment:
         - RABBITMQ_DEFAULT_USER=guest
         - RABBITMQ_DEFAULT_PASS=guest
       volumes:
         - rabbitmq_data:/var/lib/rabbitmq
       networks:
         - lead-network

   networks:
     lead-network:
       driver: bridge

   volumes:
     rabbitmq_data:
   ```

   Then run:

   ```bash
   # Start RabbitMQ
   docker-compose up -d

   # Check RabbitMQ status
   docker-compose ps

   # Access RabbitMQ management UI at:
   # http://localhost:15672
   # username: guest
   # password: guest
   ```

3. **Backend Setup**

   ```bash
   # Clone repository
   git clone https://github.com/kiarashjv/SaaS-LeadSystem.git
   cd SaaS-LeadSystem

   # Build solution
   dotnet build

   # Run services (in separate terminals)
   cd backend/src/ServerX
   dotnet run

   cd backend/src/ServerA
   dotnet run

   cd backend/src/ServerY
   dotnet run
   ```

4. **Frontend Setup**

   ```bash
   # Navigate to frontend directory
   cd frontend

   # Install dependencies
   npm install

   # Start development server
   npm run dev
   ```

## API Documentation

### ServerX API

1. **Submit Lead for Evaluation**

   ```http
   POST /api/Leads/evaluate
   Content-Type: application/json

   {
     "name": "John Smith",
     "email": "john@example.com",
     "phoneNumber": "1234567890",
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

   - Use try-catch blocks in controllers
   - Implement proper logging
   - Return appropriate HTTP status codes

2. **Message Queue Usage**

   - Use Direct Exchange pattern
   - Implement HTTP fallback
   - Handle timeout scenarios

3. **Frontend Development**
   - Use TypeScript for type safety
   - Implement proper form validation
   - Provide user feedback for all actions
   - Use proper error handling

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
