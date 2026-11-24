using Backend.Services;

namespace TrueBalance.Tests;

public class BackendSmokeTests
{
    [Fact]
    public void Background_processor_service_is_registered_from_backend_layer()
    {
        var serviceType = typeof(LocalFileProcessorService);

        Assert.Equal("Backend.Services", serviceType.Namespace);
        Assert.Equal("Backend", serviceType.Assembly.GetName().Name);
    }
}
