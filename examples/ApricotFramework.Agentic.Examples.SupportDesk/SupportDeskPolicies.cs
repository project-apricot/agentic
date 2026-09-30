namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// The authorization policies this example gates its tools on.
/// </summary>
/// <remarks>
/// Ordinary ASP.NET Core policy names. The library never learns what they mean - it reads the
/// attributes a tool declares and hands the requirements to <c>IAuthorizationService</c>.
/// </remarks>
public static class SupportDeskPolicies
{
    /// <summary>Read tickets.</summary>
    public const string TicketsRead = "tickets.read";

    /// <summary>Create, change and close tickets.</summary>
    public const string TicketsWrite = "tickets.write";

    /// <summary>Delete and purge tickets.</summary>
    public const string TicketsAdmin = "tickets.admin";

    /// <summary>Read customers.</summary>
    public const string CustomersRead = "customers.read";
}
