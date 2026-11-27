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
        foreach (var detector in PiiDetectors.All())
        {
            services.AddSingleton<IPiiDetector>(detector);
        }

        services.AddSingleton<IPiiMasker, PiiMasker>();
    }
}
