using ApricotFramework.Agentic.Tools.Serialization;
using ApricotFramework.Agentic.Tools.Tests;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests.Serialization;

/// <summary>Schema generation.</summary>
public class AgentToolJsonTests
{
    [Fact]
    public void Schema_PropertyWithDescription_CarriesItIntoTheSchema()
    {
        var schema = AgentToolJson.Schema<ProbeArguments>();

        Assert.Equal("The identifier to read.", schema.GetProperty("properties").GetProperty("id").GetProperty("description").GetString());
    }

    [Fact]
    public void Schema_NullableProperty_IsNotRequired()
    {
        var schema = AgentToolJson.Schema<ProbeArguments>();

        var required = schema.GetProperty("required").EnumerateArray().Select(entry => entry.GetString()).ToList();

        Assert.Contains("id", required);
        Assert.DoesNotContain("filter", required);
    }

    [Fact]
    public void Schema_SameTypeTwice_IsTheSameInstance()
    {
        Assert.Equal(AgentToolJson.Schema<ProbeResult>().GetRawText(), AgentToolJson.Schema<ProbeResult>().GetRawText());
    }

    [Fact]
    public void SerializerOptions_Int64BeyondDoublePrecision_RoundTripsExactly()
    {
        // the reason a declaration carries json text rather than a protobuf struct, where every
        // number is a double and an identifier of this size loses its low bits
        const long Identifier = 7306159834127433729L;

        var json = JsonSerializer.Serialize(new ProbeResult(Identifier, "x", null), AgentToolJson.DefaultSerializerOptions);

        Assert.Equal(Identifier, JsonSerializer.Deserialize<ProbeResult>(json, AgentToolJson.DefaultSerializerOptions)!.Id);
    }
}
