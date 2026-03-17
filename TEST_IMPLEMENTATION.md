# Test Implementation Guide (Moq & xUnit)

This guide documents the setup and implementation of the test suite for the .NET 9 API project.

## 1. Environment Setup

The project was standardized to **.NET 9.0**. All projects (`api.csproj` and `MyApi.Tests.csproj`) were updated to target this version to ensure compatibility and access to the latest features.

## 2. Test Project Configuration

A separate test project `MyApi.Tests` was created in the root directory.

### Key Dependencies Added:

- **xUnit**: The primary testing framework.
- **Moq**: Used for creating mock objects to isolate units of code.
- **Microsoft.AspNetCore.Mvc.Testing**: Provides the `WebApplicationFactory` for integration testing.
- **coverlet.collector**: Enables code coverage reporting.

## 3. Mocking Strategy (Moq)

We use Moq to simulate dependencies that are difficult to set up in a unit test (like databases or external APIs).

**Example (Middleware Testing):**
Located in `MyApi.Tests/Middleware/ExceptionMiddlewareTests.cs`

- Mocked `RequestDelegate` to throw an exception.
- Mocked `ILogger` and `IHostEnvironment`.
- Verified that the middleware catches the exception and returns a `500 Internal Server Error`.

```csharp
var mockNext = new Mock<RequestDelegate>();
mockNext.Setup(next => next(It.IsAny<HttpContext>())).Throws(new Exception("Test"));
// ... middleware call and assertion
```

## 4. Integration Testing (xUnit)

Located in `MyApi.Tests/Integration/BasicIntegrationTests.cs`
We use `WebApplicationFactory<Program>` to bootstrap the entire API in memory for end-to-end testing.

- **Check Health Endpoint**: Verifies the API is up and running.
- **HttpClient**: Used to make actual HTTP calls to the in-memory server.

## 5. Directory Structure

- `MyApi.Tests/Controllers/`: Unit tests for API endpoints.
- `MyApi.Tests/Services/`: Business logic validation.
- `MyApi.Tests/Middleware/`: Pipeline and error handling tests.
- `MyApi.Tests/Integration/`: Full API flow tests.

## 6. How to Run Tests

You can run all tests from the root directory using the terminal:

```bash
dotnet test
```

To run tests with a detailed summary:

```bash
dotnet test --logger "console;verbosity=detailed"
```
