using ApricotFramework.Agentic.Tools.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// Describing a tool that is a method.
/// </summary>
/// <remarks>
/// Everything hard here is already done by <see cref="AIFunctionFactory"/>: it binds the
/// parameters, generates the schemas, understands a cancellation token and a progress reporter,
/// and - given a way to make the target - builds a fresh one for every call. Supplying the
/// caller's own scope as that way is what makes an attributed method behave exactly like a
/// class-per-tool, and like an endpoint.
/// </remarks>
internal static class AgentToolMethods
{
    /// <summary>
    /// Finds the methods declared as tools on a type.
    /// </summary>
    /// <param name="toolType">The type to look at.</param>
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
    /// Reads what a method declares, and returns it as a function resolved per call.
    /// </summary>
    /// <param name="toolType">The type the method belongs to.</param>
    /// <param name="method">The method behind the tool.</param>
    /// <param name="metadata">What a host said about it.</param>
    /// <returns>The descriptor.</returns>
    /// <exception cref="ArgumentException">Thrown when the method does not declare itself a tool.</exception>
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
    /// Decides how one parameter is filled in.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>How to bind it, or the default to let the factory decide.</returns>
    /// <remarks>
    /// <para>
    /// A method asking for the invocation gets it, and it does not appear in the schema - a
    /// model should not be asked to fill in who is calling.
    /// </para>
    /// <para>
    /// Everything else is the factory's business: the parameter list is the argument shape, and
    /// a cancellation token or a progress reporter is already understood there.
    /// </para>
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
    /// Fills in a parameter asking for the invocation.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <param name="arguments">The arguments, carrying the invocation.</param>
    /// <returns>The invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the host builds a different kind of context than the method asks for.</exception>
    /// <remarks>
    /// A method may ask for a host's own derived context. Saying plainly that the factory built
    /// something else is better than handing over a null the method will dereference.
    /// </remarks>
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
    /// Builds the object a call runs against.
    /// </summary>
    /// <param name="arguments">The arguments, carrying the scope.</param>
    /// <param name="toolType">The type to build.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the call carries no scope.</exception>
    private static object Target(AIFunctionArguments arguments, Type toolType)
    {
        var services = arguments.Services
                       ?? throw new InvalidOperationException(
                           $"'{toolType.Name}' is resolved per call and the call carried no services. Run it through IAgentToolExecutor.");

        return ActivatorUtilities.GetServiceOrCreateInstance(services, toolType);
    }
}
