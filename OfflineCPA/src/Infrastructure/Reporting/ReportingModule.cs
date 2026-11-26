using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Reporting;

public sealed class ReportingModule : IAdapterModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<IReportExporter, ClosedXmlReportExporter>();
    }
}
