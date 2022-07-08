namespace MachineDataApi.Models;

public class MachineDataDto
{
    public string Id { get; set; }
    public string MachineId { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Status { get; set; }
}