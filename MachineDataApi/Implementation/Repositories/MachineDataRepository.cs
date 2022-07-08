using System.Collections.Concurrent;
using MachineDataApi.Models;
using Optional;

namespace MachineDataApi.Implementation.Repositories;

public interface IMachineDataRepository
{
    Task Insert(MachineData machineData);
    Task<Option<PagedResult<MachineData>>> GetMachineDataPaged(Guid machineId, int skip = 0, int take = 10);
    Task<PagedResult<MachineData>> GetAllDataPaged(int skip = 0, int take = 10);
    Task<Option<MachineData>> GetItemById(Guid id);
    Task<IEnumerable<Guid>> GetMachines();
}

public class InMemoryMachineDataRepository : IMachineDataRepository
{
    private static ConcurrentDictionary<Guid, List<MachineData>> _machineDataStorage = new();
    private static ConcurrentDictionary<Guid, MachineData> _plainDataStorage = new ();
    public Task Insert(MachineData machineData)
    {
        _plainDataStorage.AddOrUpdate(machineData.Id, machineData, (_, data) =>
        {
            data = machineData;
            return data;
        });
        _machineDataStorage.AddOrUpdate(machineData.MachineId, new List<MachineData> {machineData}, (_, machineDataItems) =>
        {
            machineDataItems.Add(machineData);
            return machineDataItems;
        });

        return Task.CompletedTask;
    }

    public Task<Option<PagedResult<MachineData>>> GetMachineDataPaged(Guid machineId, int skip = 0, int take = 10)
    {
        ValidatePagingParams(skip, take);

        if(!_machineDataStorage.TryGetValue(machineId, out var machineDataItems))
            return Task.FromResult(Option.None<PagedResult<MachineData>>());

        var result = new PagedResult<MachineData>
        {
            Items = machineDataItems.Skip(skip).Take(take).ToArray(),
            TotalCount = machineDataItems.Count
        };

        return Task.FromResult(result.Some());
    }

    public Task<PagedResult<MachineData>> GetAllDataPaged(int skip = 0, int take = 10)
    {
        ValidatePagingParams(skip, take);
        var result = new PagedResult<MachineData>
        {
            Items = _plainDataStorage.Values.Skip(skip).Take(take),
            TotalCount = _plainDataStorage.Count
        };

        return Task.FromResult(result);
    }

    public Task<Option<MachineData>> GetItemById(Guid id)
    {
        if (!_plainDataStorage.TryGetValue(id, out var machineData))
            return Task.FromResult(Option.None<MachineData>());

        return Task.FromResult(machineData.Some());
    }

    public Task<IEnumerable<Guid>> GetMachines()
        => Task.FromResult((IEnumerable<Guid>) _machineDataStorage.Keys);


    private void ValidatePagingParams(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentException("Skip paging parameter cannot be less than 0", nameof(skip));
        }

        if (take > 1000)
        {
            throw new ArgumentException("Maximum value for page size is 1000", nameof(skip));
        }
    }
}