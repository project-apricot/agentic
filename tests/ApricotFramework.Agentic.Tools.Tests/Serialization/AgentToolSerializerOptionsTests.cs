using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Serialization;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Tests;
using System.ComponentModel;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests.Serialization;

/// <summary>A tool choosing how its own JSON is shaped.</summary>
public class AgentToolSerializerOptionsTests
{
    private static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>A result whose property names differ between naming policies.</summary>
    /// <param name="PublicEmail">The address published for contact.</param>
    /// <param name="DisplayName">The name shown.</param>
    public sealed record Contact(
        [property: Description("The address published for contact.")] string PublicEmail,
        [property: Description("The name shown.")] string DisplayName);

    private sealed class SnakeCaseProbe : AgentTool<AgentToolNoArguments, Contact>
    {
        public override string Name => "probe_contacts_snake";

        public override string Title => "Probe";

        public override string Description => "Writes its result in snake case.";

        public override bool IsReadOnly => true;

        public override bool IsDestructive => false;


        public override JsonSerializerOptions SerializerOptions => SnakeCase;

        protected override Task<Contact> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
            => Task.FromResult(new Contact("ada@example.org", "Ada"));
    }

    [Fact]
    public void Schema_UnderTheDefaultOptions_IsCamelCase()
    {
        Assert.True(AgentToolJson.Schema<Contact>().GetProperty("properties").TryGetProperty("publicEmail", out _));
    }

    [Fact]
    public void OutputSchema_FollowsTheToolsOwnOptions()
    {
        var properties = new SnakeCaseProbe().OutputSchema!.Value.GetProperty("properties");

        Assert.True(properties.TryGetProperty("public_email", out _));
        Assert.False(properties.TryGetProperty("publicEmail", out _));
    }

    [Fact]
    public void Schema_SameTypeUnderDifferentOptions_DiffersAndIsCachedSeparately()
    {
        var byDefault = AgentToolJson.Schema<Contact>();
        var snake = AgentToolJson.Schema<Contact>(SnakeCase);

        Assert.NotEqual(byDefault.GetRawText(), snake.GetRawText());
        Assert.Equal(snake.GetRawText(), AgentToolJson.Schema<Contact>(SnakeCase).GetRawText());
    }

    [Fact]
    public async Task Result_IsWrittenUnderTheSameOptionsTheSchemaWasGeneratedUnder()
    {
        // the two disagreeing is what silently drops fields a model filled in
        var invoker = new AgentToolInvoker(
            new AgentToolRegistry([StaticAgentToolSource.For(new SnakeCaseProbe())]),
            null);

        var json = await invoker.InvokeCompleteAsync(
            "probe_contacts_snake", null, new AgentToolContext(), TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);

        var properties = new SnakeCaseProbe().OutputSchema!.Value.GetProperty("properties");

        foreach (var property in document.RootElement.EnumerateObject())
        {
            Assert.True(properties.TryGetProperty(property.Name, out _), $"'{property.Name}' is not in the schema");
        }

        Assert.True(document.RootElement.TryGetProperty("public_email", out _));
    }
}
