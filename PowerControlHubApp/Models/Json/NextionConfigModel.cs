namespace PowerControlHubApp.Models.Json;

public sealed class NextionConfigModel
{
    public bool? Enabled { get; set; }
    public bool? HardwareSerial { get; set; }
    public int? RxPin { get; set; }
    public int? TxPin { get; set; }
    public int? BaudRate { get; set; }
    public int? UartNumber { get; set; }
}
