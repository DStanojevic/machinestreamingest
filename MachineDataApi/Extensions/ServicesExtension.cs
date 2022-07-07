using MachineDataApi.Configuration;
using Microsoft.Extensions.Options;

namespace MachineDataApi.Extensions;

public static class ServicesExtension
{
    public static void RegisterApplicationConfiguration(this IServiceCollection serviceCollection, IConfiguration conf)
    {
        var section = conf.GetRequiredSection("applicationConfiguration");
        serviceCollection.Configure<ApplicationConfiguration>(section);
        serviceCollection.AddSingleton(GetApplicationConfiguration);
    }

    public static ApplicationConfiguration GetApplicationConfiguration(this IServiceProvider serviceProvider)
    {
        var config  = serviceProvider.GetService<IOptions<ApplicationConfiguration>>().Value;
        config.Validate();
        return config;
    }
}