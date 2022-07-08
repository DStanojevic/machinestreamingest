using System.Text.Json;
using System.Text.Json.Serialization;
using MachineDataApi.Implementation.Repositories;
using MachineDataApi.Mappers;
using MachineDataApi.Models;
using Optional;

namespace MachineDataApi.Implementation.Services;


public interface IMachineDataService
{
    Task SaveRawMessage(byte[] rawMessageBytes);
    Task<PagedResult<MachineDataDto>> GetAllDataPaged(int skip = 0, int take = 10);
    Task<Option<MachineDataDto>> GetMessage(Guid id);
    Task<Option<PagedResult<MachineDataDto>>> GetMachineDataPaged(Guid machineId, int skip = 0, int take = 10);
    Task<IEnumerable<string>> GetMachines();
}

public class MachineDataService : IMachineDataService
{
    private readonly IMachineDataRepository _machineDataRepository;

    public MachineDataService(IMachineDataRepository machineDataRepository)
    {
        _machineDataRepository = machineDataRepository;
    }
    public Task SaveRawMessage(byte[] rawMessageBytes)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var machineMessage = JsonSerializer.Deserialize<MachineMessage>(rawMessageBytes, jsonOptions);

        if (machineMessage == null)
        {
            throw new InvalidOperationException("Failed to deserialize message");
        }
        return _machineDataRepository.Insert(machineMessage.Payload);
    }

    public async Task<PagedResult<MachineDataDto>> GetAllDataPaged(int skip = 0, int take = 10)
    {
        var pagedData = await _machineDataRepository.GetAllDataPaged(skip, take);
        return new PagedResult<MachineDataDto>
        {
            Items = pagedData.Items.Select(p => p.ToMachineDataDto()).ToArray(),
            TotalCount = pagedData.TotalCount
        };
    }

    public async Task<Option<MachineDataDto>> GetMessage(Guid id)
    {
        var messageOption = await _machineDataRepository.GetItemById(id);
        return messageOption.Map(p => p.ToMachineDataDto());
    }

    public async Task<Option<PagedResult<MachineDataDto>>> GetMachineDataPaged(Guid machineId, int skip = 0, int take = 10)
    {
        var machineDataOption = await _machineDataRepository.GetMachineDataPaged(machineId, skip, take);
        return machineDataOption.Map(p => new PagedResult<MachineDataDto>
        {
            Items = p.Items.Select(i => i.ToMachineDataDto()).ToArray(),
            TotalCount = p.TotalCount
        });
    }

    public async Task<IEnumerable<string>> GetMachines()
    {
        var machineIds = await _machineDataRepository.GetMachines();
        return machineIds.Select(p => p.ToString());
    }
}