namespace Backend.Extensions;

public static class SecureErrorHandling
{
    public static IServiceCollection AddSecureErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }

    public static WebApplication UseSecureErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        return app;
    }
}
