using ApricotFramework.Agentic.Tools.ErrorDefinitions.Extensions;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.ErrorDefinitions;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.ErrorDefinitions.Tests;

/// <summary>Error definitions thrown by a tool arrive as failures of the right kind.</summary>
public class ErrorDefinitionExceptionTranslatorTests
{
    private static IAgentToolExecutor Executor()
    {
        var services = new ServiceCollection();

        services.AddAgentToolsCore();
        services.AddAgentToolType<LedgerTools>();
        services.AddAgentToolErrorDefinitions();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true })
            .GetRequiredService<IAgentToolExecutor>();
    }

    [Fact]
    public async Task ANotFound_IsAFailureOfThatKind_WithItsCode_AndTheOriginalInside()
    {
        var exception = await Assert.ThrowsAsync<AgentToolFailedException>(
            () => Executor().InvokeCompleteAsync("ledger_get", """{"id":7}""", TestContext.Current.CancellationToken));

        Assert.Equal(AgentToolFailureKind.NotFound, exception.Kind);
        Assert.False(exception.IsRetryable);
        Assert.Equal("No entry 7. (LEDGER_ENTRY_NOT_FOUND)", exception.Message);
        Assert.IsType<ErrorDefinitionException>(exception.InnerException);
    }

    [Fact]
    public async Task AValidationFailure_ListsEveryError()
    {
        var exception = await Assert.ThrowsAsync<AgentToolFailedException>(
            () => Executor().InvokeCompleteAsync("ledger_post", null, TestContext.Current.CancellationToken));

        Assert.Equal(AgentToolFailureKind.Invalid, exception.Kind);
        Assert.Contains("AMOUNT_REQUIRED", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ACCOUNT_REQUIRED", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnOutage_IsRetryable()
    {
        var exception = await Assert.ThrowsAsync<AgentToolFailedException>(
            () => Executor().InvokeCompleteAsync("ledger_sync", null, TestContext.Current.CancellationToken));

        Assert.Equal(AgentToolFailureKind.Unavailable, exception.Kind);
        Assert.True(exception.IsRetryable);
        Assert.Equal("The ledger is down.", exception.Message);
    }

    [Fact]
    public async Task AnythingElse_GoesThroughAsItIs()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Executor().InvokeCompleteAsync("ledger_crash", null, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(ErrorKinds.AlreadyExists, AgentToolFailureKind.Conflict)]
    [InlineData(ErrorKinds.AccessDenied, AgentToolFailureKind.Denied)]
    [InlineData(ErrorKinds.NotAuthenticated, AgentToolFailureKind.Unauthenticated)]
    [InlineData(ErrorKinds.ResourceExhausted, AgentToolFailureKind.RateLimited)]
    [InlineData(ErrorKinds.Timeout, AgentToolFailureKind.Timeout)]
    [InlineData(ErrorKinds.Internal, AgentToolFailureKind.Fault)]
    [InlineData("something_new", AgentToolFailureKind.Fault)]
    public void EachKind_MapsToTheClosestFailure(string kind, AgentToolFailureKind expected)
    {
        Assert.Equal(expected, ErrorDefinitionExceptionTranslator.Kind(kind));
    }
}
