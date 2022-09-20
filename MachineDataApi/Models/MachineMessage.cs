using System.Text.Json.Serialization;

namespace MachineDataApi.Models;

public class MachineMessage
{
    public string Topic { get; set; }
    public string Ref { get; set; }
    [JsonPropertyName("join_ref")]
    public string JoinRef { get; set; }
    public string Event { get; set; }
    public MachineData Payload { get; set; }
}

public class MachineData
{
    public Guid Id { get; set; }
    [JsonPropertyName("machine_id")]
    public Guid MachineId { get; set; }
    [JsonPropertyName("timestamp")]
    public DateTime TimeStamp { get; set; }
    public MachineStatus Status { get; set; }
}


public class Machine
{
    public Guid Id { get; set; }

    public IList<MachineData> Data { get; set; } = new List<MachineData>();

}

public enum MachineStatus
{
    Idle,
    Running,
    Finished,
    Errored,
    Repaired
}