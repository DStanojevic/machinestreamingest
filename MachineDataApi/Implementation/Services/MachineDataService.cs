using System.Text.Json;
using System.Text.Json.Serialization;
using MachineDataApi.Implementation.Repositories;
using MachineDataApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MachineDataApi.Implementation.Services;


public interface IMachineDataService
{
    Task SaveRawMessage(byte[] rawMessageBytes);
    Task<IActionResult> GetAllDataPaged(int skip = 0, int take = 10);
    Task<IActionResult> GetMessage(Guid id);
    Task<IActionResult> GetMachineDataPaged(Guid machineId, int skip = 0, int take = 10);
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

    public async Task<IActionResult> GetAllDataPaged(int skip = 0, int take = 10)
    {
        var pagedData = await _machineDataRepository.GetAllDataPaged(skip, take);
        return new OkObjectResult(pagedData);
    }

    public async Task<IActionResult> GetMessage(Guid id)
    {
        var messageOption = await _machineDataRepository.GetItemById(id);
        return messageOption.Match(
            msg => (IActionResult) new OkObjectResult(msg),
            () => (IActionResult) new NotFoundResult());
    }

    public async Task<IActionResult> GetMachineDataPaged(Guid machineId, int skip = 0, int take = 10)
    {
        var machineDataOption = await _machineDataRepository.GetMachineDataPaged(machineId, skip, take);
        return machineDataOption.Match(
            pd => (IActionResult) new OkObjectResult(pd),
            () => (IActionResult) new NotFoundResult());
    }
}