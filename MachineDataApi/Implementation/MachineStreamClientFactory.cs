namespace MachineDataApi.Implementation;

public interface IMachineStreamClientFactory
{
    IMachineStreamClient CreateMachineStreamClient(IServiceScope serviceScope);
}

public class MachineStreamClientFactory : IMachineStreamClientFactory
{
    public IMachineStreamClient CreateMachineStreamClient(IServiceScope serviceScope) =>
        serviceScope.ServiceProvider.GetRequiredService<IMachineStreamClient>();
}