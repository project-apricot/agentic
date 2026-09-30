using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Core;

namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// The client a host would declare for the billing service: a type per service.
/// </summary>
/// <param name="invoker">What the factory hands it.</param>
public sealed class BillingAgentToolsClient(CallInvoker invoker) : AgentTools.AgentToolsClient(invoker);
