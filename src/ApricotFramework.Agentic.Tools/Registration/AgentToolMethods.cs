using ApricotFramework.Agentic.Tools.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// Describes tools implemented as methods.
/// </summary>
/// <remarks>
/// Built on <see cref="AIFunctionFactory"/>, with the target resolved from the call's scope so a
/// method tool behaves like a class-per-tool.
/// </remarks>
internal static class AgentToolMethods
{
    /// <summary>
    /// Finds the tool methods declared on a type.
    /// </summary>
    /// <param name="toolType">The type to inspect.</param>
    /// <returns>The methods, in declaration order.</returns>
    internal static IEnumerable<MethodInfo> In(Type toolType)
    {
        ArgumentNullException.ThrowIfNull(toolType);

        return toolType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<AgentToolAttribute>(inherit: false) is not null
                             && method.GetCustomAttribute<AgentToolIgnoreAttribute>(inherit: false) is null);
    }

    /// <summary>
    /// Describes a tool method as a function whose target is resolved per call.
    /// </summary>
    /// <param name="toolType">The declaring type.</param>
    /// <param name="method">The method behind the tool.</param>
    /// <param name="metadata">Host-supplied metadata.</param>
    /// <returns>The descriptor.</returns>
    /// <exception cref="ArgumentException">The method is not marked as a tool.</exception>
    internal static AgentToolDescriptor Describe(Type toolType, MethodInfo method, IReadOnlyList<object> metadata)
    {
        ArgumentNullException.ThrowIfNull(toolType);
        ArgumentNullException.ThrowIfNull(method);

        var attribute = method.GetCustomAttribute<AgentToolAttribute>(inherit: false)
                        ?? throw new ArgumentException($"'{toolType.Name}.{method.Name}' does not carry {nameof(AgentToolAttribute)}.", nameof(method));

        var declaration = new AgentToolDeclaration
        {
            Name = attribute.Name,
            Description = method.GetCustomAttribute<DescriptionAttribute>(inherit: false)?.Description ?? string.Empty,
            IsReadOnly = attribute.ReadOnly,
            IsDestructive = attribute.Destructive,
            IsIdempotent = attribute.Idempotent,
            IsOpenWorld = attribute.OpenWorld,
            Labels = AgentToolLabels.ForMethod(method)
        };

        if (attribute.Title is { } title)
        {
            declaration = declaration with { Title = title };
        }

        var options = new AIFunctionFactoryOptions
        {
            Name = declaration.Name,
            Description = declaration.Description,
            SerializerOptions = AgentToolJson.DefaultSerializerOptions,
            AdditionalProperties = AgentToolAnnotations.For(declaration),
            ConfigureParameterBinding = Bind
        };

        var function = method.IsStatic
            ? AIFunctionFactory.Create(method, target: null, options)
            : AIFunctionFactory.Create(method, arguments => Target(arguments, toolType), options);

        return new AgentToolDescriptor(function, declaration, metadata);
    }

    /// <summary>
    /// Decides how one parameter is bound.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>The binding, or the default to let the factory decide.</returns>
    /// <remarks>
    /// <see cref="AgentToolContext"/> parameters are bound from the invocation and excluded from the schema.
    /// </remarks>
    private static AIFunctionFactoryOptions.ParameterBindingOptions Bind(ParameterInfo parameter) =>
        typeof(AgentToolContext).IsAssignableFrom(parameter.ParameterType)
            ? new AIFunctionFactoryOptions.ParameterBindingOptions
            {
                ExcludeFromSchema = true,
                BindParameter = Context
            }
            : default;

    /// <summary>
    /// Binds a context parameter.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <param name="arguments">The arguments carrying the context.</param>
    /// <returns>The context.</returns>
    /// <exception cref="InvalidOperationException">The host's context is not of the type the method asks for.</exception>
    private static AgentToolContext Context(ParameterInfo parameter, AIFunctionArguments arguments)
    {
        var context = arguments.RequireAgentToolContext();

        return parameter.ParameterType.IsInstanceOfType(context)
            ? context
            : throw new InvalidOperationException(
                $"A tool asks for '{parameter.ParameterType.Name}', but this host's IAgentToolContextFactory built " +
                $"'{context.GetType().Name}'. Register a factory that builds the context the tool expects.");
    }

    /// <summary>
    /// Resolves the instance a call runs against.
    /// </summary>
    /// <param name="arguments">The arguments carrying the scope.</param>
    /// <param name="toolType">The type to resolve.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="InvalidOperationException">The call carries no services.</exception>
    private static object Target(AIFunctionArguments arguments, Type toolType)
    {
        var services = arguments.Services
                       ?? throw new InvalidOperationException(
                           $"'{toolType.Name}' is resolved per call and the call carried no services. Run it through IAgentToolExecutor.");

        return ActivatorUtilities.GetServiceOrCreateInstance(services, toolType);
    }
}
