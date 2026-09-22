namespace ApricotFramework.Agentic.Tools.AspNetCore;

/// <summary>
/// A tool that has just been registered, so more can be said about it.
/// </summary>
/// <remarks>
/// <para>
/// Shaped after <c>IEndpointConventionBuilder</c>, which is a metadata list and no behavior: every
/// <c>Require*</c> in ASP.NET Core is an extension method appending to one, and every one here is
/// too.
/// </para>
/// <para>
/// Returned from every registration, so <c>RequireAuthorization</c> reads the same whether the
/// tool is a class, an instance, or something adapted from a function.
/// </para>
/// </remarks>
public interface IAgentToolBuilder
{
    /// <summary>
    /// Gets what is being said about the tool.
    /// </summary>
    /// <remarks>
    /// The same shape as <c>EndpointBuilder.Metadata</c>, and mutable for the same reason: this is
    /// the builder, not the product. A tool's registration stays open to additions until the
    /// container is built, which is what lets <c>RequireAuthorization</c> be a separate call from
    /// <c>AddAgentTool</c> at all.
    /// </remarks>
    IList<object> Metadata { get; }
}
