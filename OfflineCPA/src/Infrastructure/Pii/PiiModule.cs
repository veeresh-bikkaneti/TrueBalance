using Application.Interfaces;
using Domain.Pii;
using Infrastructure.Pii.Detectors;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure;

namespace Infrastructure.Pii;

public sealed class PiiModule : IAdapterModule
{
    public void Register(IServiceCollection services)
    {
        // Detector order does not decide precedence; the domain policy ranks
        // overlapping matches by PiiType priority.
        services.AddSingleton<IPiiDetector, AbaRoutingNumberDetector>();
        services.AddSingleton<IPiiDetector, SsnDetector>();
        services.AddSingleton<IPiiDetector, EinItinDetector>();
        services.AddSingleton<IPiiDetector, BankAccountNumberDetector>();
        services.AddSingleton<IPiiMasker, PiiMasker>();
    }
}
