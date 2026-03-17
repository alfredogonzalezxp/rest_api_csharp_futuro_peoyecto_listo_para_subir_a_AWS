using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using api.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace MyApi.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCatchException_AndReturnInternalServerError()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        mockNext.Setup(next => next(It.IsAny<HttpContext>()))
                .Throws(new Exception("Test exception"));

        var mockLogger = new Mock<ILogger<ExceptionMiddleware>>();
        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(env => env.EnvironmentName).Returns("Development");

        var middleware = new ExceptionMiddleware(mockNext.Object, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
    }
}
