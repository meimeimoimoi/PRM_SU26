using SmartDine.Domain.Enums;

namespace SmartDine.Application.DTOs.Tables;

public class TableResponse
{
    public Guid Id { get; set; }
    public int TableNumber { get; set; }
    public int Capacity { get; set; }
    public TableStatus Status { get; set; }
    public string? QrCode { get; set; }
    public string? Location { get; set; }
}

public class UpdateTableStatusRequest
{
    public TableStatus Status { get; set; }
}

public class CreateTableRequest
{
    public int TableNumber { get; set; }
    public int Capacity { get; set; } = 4;
    public string? Location { get; set; }
}
