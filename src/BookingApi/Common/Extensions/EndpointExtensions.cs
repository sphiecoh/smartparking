namespace BookingApi.Common.Extensions;

/// <summary>
/// Marker interface — each feature slice implements this to
/// self-register its minimal-API endpoints onto the app.
/// </summary>
public interface IEndpointDefinition
{
    void RegisterEndpoints(WebApplication app);
}

public static class EndpointExtensions
{
    /// <summary>
    /// Scans the assembly for all IEndpointDefinition implementations
    /// and calls RegisterEndpoints on each — keeping Program.cs clean.
    /// </summary>
    public static WebApplication MapAllEndpoints(this WebApplication app)
    {
        var definitions = typeof(Program).Assembly
            .GetTypes()
            .Where(t => typeof(IEndpointDefinition).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(Activator.CreateInstance)
            .Cast<IEndpointDefinition>();

        foreach (var def in definitions)
            def.RegisterEndpoints(app);

        return app;
    }
}
