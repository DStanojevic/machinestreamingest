using MachineDataApi.Implementation;

namespace MachineDataApi.BackgroundServices;

public class MachineDataIngestHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMachineStreamClientFactory _machineStreamClientFactory;
    private IServiceScope _serviceScope;
    private IMachineStreamClient _machineStreamClient;

    public MachineDataIngestHostedService(IServiceScopeFactory serviceScopeFactory, IMachineStreamClientFactory machineStreamClientFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _machineStreamClientFactory = machineStreamClientFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceScope = _serviceScopeFactory.CreateScope();
        _machineStreamClient = _machineStreamClientFactory.CreateMachineStreamClient(_serviceScope);
        try
        {
            return _machineStreamClient.StartAsync(cancellationToken);
        }
        catch
        {
            _serviceScope.Dispose();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            return _machineStreamClient.StopAsync(cancellationToken);
        }
        finally
        {
            _serviceScope.Dispose();
        }
    }
}