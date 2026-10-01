using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;

namespace ApricotFramework.Agentic.Tools.Tests.Invocation;

/// <summary>A host's own exceptions, described once, the same way on every surface.</summary>
public class ExceptionTranslationTests
{
    private static AgentToolInvoker Invoker(params IAgentToolExceptionTranslator[] translators) =>
        new(new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe(), new FailingSequenceProbe(), new ThrowingProbe())]), null, null, translators);

    [Fact]
    public async Task ARecognisedException_BecomesTheFailureTheTranslatorDescribed_WithTheOriginalInside()
    {
        var exception = await Assert.ThrowsAsync<AgentToolFailedException>(
            () => Invoker(new Translating()).InvokeCompleteAsync("probe_items_throw", null, Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal(AgentToolFailureKind.NotFound, exception.Kind);
        Assert.False(exception.IsRetryable);
        Assert.IsType<KeyNotFoundException>(exception.InnerException);
    }

    [Fact]
    public async Task AFailurePartWayThroughASequence_IsTranslatedToo()
    {
        var items = new List<string>();

        var exception = await Assert.ThrowsAsync<AgentToolFailedException>(async () =>
        {
            await foreach (var item in Invoker(new Translating()).InvokeAsync("probe_items_fail", null, Probes.Context(), TestContext.Current.CancellationToken))
            {
                items.Add(item);
            }
        });

        Assert.Single(items);
        Assert.Equal(AgentToolFailureKind.Unavailable, exception.Kind);
        Assert.True(exception.IsRetryable);
    }

    [Fact]
    public async Task AnExceptionNoTranslatorRecognises_GoesThroughAsItIs()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => Invoker().InvokeCompleteAsync("probe_items_throw", null, Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TheFirstTranslatorToAnswer_Wins()
    {
        var exception = await Assert.ThrowsAsync<AgentToolFailedException>(
            () => Invoker(new Silent(), new Translating(), new Faulting()).InvokeCompleteAsync("probe_items_throw", null, Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal(AgentToolFailureKind.NotFound, exception.Kind);
    }

    [Fact]
    public async Task ARefusalOfThisLibrarysOwn_IsNeverOfferedToATranslator()
    {
        var faulting = new Faulting();

        await Assert.ThrowsAsync<AgentToolNotFoundException>(
            () => Invoker(faulting).InvokeCompleteAsync("probe_items_missing", null, Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal(0, faulting.Calls);
    }

    [Fact]
    public async Task AnAuthorizationRefusalForWantOfACaller_IsUnauthenticated()
    {
        var invoker = new AgentToolInvoker(new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe())]), null, [new Anonymous()]);

        await Assert.ThrowsAsync<AgentToolUnauthenticatedException>(
            () => invoker.InvokeCompleteAsync("probe_items_get", """{"id":1}""", Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AFailureKind_DecidesWhetherItIsRetryable_UnlessToldOtherwise()
    {
        Assert.True(new AgentToolFailedException(AgentToolFailureKind.Timeout, "slow").IsRetryable);
        Assert.False(new AgentToolFailedException(AgentToolFailureKind.Invalid, "bad").IsRetryable);
        Assert.True(new AgentToolFailedException(AgentToolFailureKind.Fault, "odd", retryable: true).IsRetryable);
    }

    private sealed class ThrowingProbe : AgentTool
    {
        public override string Name => "probe_items_throw";

        public override string Title => "Throw";

        public override string Description => "Always throws the host's own not-found.";

        public override bool IsReadOnly => true;

        public override bool IsDestructive => false;

        public override AgentToolResultKind ResultKind => AgentToolResultKind.Whole;

        protected override ValueTask<object?> InvokeCoreAsync(Microsoft.Extensions.AI.AIFunctionArguments arguments, CancellationToken cancellationToken) =>
            throw new KeyNotFoundException("no item 7");
    }

    private sealed class Translating : IAgentToolExceptionTranslator
    {
        public AgentToolException? Translate(Exception exception, AgentToolDescriptor tool) => exception switch
        {
            KeyNotFoundException => new AgentToolFailedException(AgentToolFailureKind.NotFound, exception.Message, exception),
            InvalidOperationException => new AgentToolFailedException(AgentToolFailureKind.Unavailable, exception.Message, exception),
            _ => null
        };
    }

    private sealed class Silent : IAgentToolExceptionTranslator
    {
        public AgentToolException? Translate(Exception exception, AgentToolDescriptor tool) => null;
    }

    private sealed class Faulting : IAgentToolExceptionTranslator
    {
        public int Calls { get; private set; }

        public AgentToolException? Translate(Exception exception, AgentToolDescriptor tool)
        {
            this.Calls++;

            return new AgentToolFailedException(AgentToolFailureKind.Fault, "fault", exception);
        }
    }

    private sealed class Anonymous : IAgentToolAuthorizationFilter
    {
        public ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(AgentToolAuthorizationDecision.Unauthenticated("Nobody is calling."));
    }
}
