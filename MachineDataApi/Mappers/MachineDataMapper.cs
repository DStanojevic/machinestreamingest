using MachineDataApi.Models;

namespace MachineDataApi.Mappers;

public static class MachineDataMapper
{
    public static MachineDataDto ToMachineDataDto(this MachineData machineData)
        => new MachineDataDto
        {
            Id = machineData.Id.ToString(),
            MachineId = machineData.MachineId.ToString(),
            Status = machineData.Status.ToString(),
            TimeStamp = machineData.TimeStamp
        };
}