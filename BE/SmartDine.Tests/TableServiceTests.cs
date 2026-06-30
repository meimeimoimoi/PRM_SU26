using Moq;
using SmartDine.Application.Services;
using SmartDine.Application.DTOs.Tables;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Tests;

public class TableServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly Mock<IDiningSessionRepository> _sessionRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<ITableReservationRepository> _reservationRepoMock;
    private readonly Mock<IRepository<SessionParticipant>> _participantRepoMock;
    private readonly TableService _tableService;

    public TableServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _tableRepoMock = new Mock<ITableRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _reservationRepoMock = new Mock<ITableReservationRepository>();
        _participantRepoMock = new Mock<IRepository<SessionParticipant>>();

        _uowMock.Setup(u => u.Tables).Returns(_tableRepoMock.Object);
        _uowMock.Setup(u => u.DiningSessions).Returns(_sessionRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.TableReservations).Returns(_reservationRepoMock.Object);
        _uowMock.Setup(u => u.SessionParticipants).Returns(_participantRepoMock.Object);
        _participantRepoMock.Setup(r => r.AddAsync(It.IsAny<SessionParticipant>()))
            .ReturnsAsync((SessionParticipant p) => p);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _tableService = new TableService(_uowMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 1: GetAllAsync — GET /api/v1/tables
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_NoFilters_ReturnsAllTables()
    {
        var tables = new List<Table>
        {
            new() { Id = 1, TableNumber = 1, Capacity = 4, Status = TableStatus.AVAILABLE },
            new() { Id = 2, TableNumber = 2, Capacity = 6, Status = TableStatus.OCCUPIED }
        };
        _tableRepoMock.Setup(r => r.GetFilteredAsync(null, null)).ReturnsAsync(tables);

        var result = await _tableService.GetAllAsync(null, null);

        Assert.Equal(2, result.Count);
        Assert.Equal("AVAILABLE", result[0].Status);
        Assert.Equal("OCCUPIED", result[1].Status);
    }

    [Fact]
    public async Task GetAll_FilterByStatus_PassesStatusToRepo()
    {
        _tableRepoMock.Setup(r => r.GetFilteredAsync("AVAILABLE", null))
            .ReturnsAsync(new List<Table> { new() { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE } });

        var result = await _tableService.GetAllAsync("AVAILABLE", null);

        Assert.Single(result);
        _tableRepoMock.Verify(r => r.GetFilteredAsync("AVAILABLE", null), Times.Once);
    }

    [Fact]
    public async Task GetAll_FilterByCapacity_PassesCapacityToRepo()
    {
        _tableRepoMock.Setup(r => r.GetFilteredAsync(null, 6)).ReturnsAsync(new List<Table>());

        var result = await _tableService.GetAllAsync(null, 6);

        Assert.Empty(result);
        _tableRepoMock.Verify(r => r.GetFilteredAsync(null, 6), Times.Once);
    }

    [Fact]
    public async Task GetAll_FilterByBothStatusAndCapacity_PassesBothToRepo()
    {
        _tableRepoMock.Setup(r => r.GetFilteredAsync("AVAILABLE", 4))
            .ReturnsAsync(new List<Table> { new() { Id = 1, TableNumber = 1, Capacity = 4, Status = TableStatus.AVAILABLE } });

        var result = await _tableService.GetAllAsync("AVAILABLE", 4);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAll_InvalidStatus_ThrowsBusinessRuleViolation()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.GetAllAsync("BUSY", null));

        Assert.Contains("AVAILABLE", ex.Message);
        Assert.Contains("OCCUPIED", ex.Message);
    }

    [Fact]
    public async Task GetAll_CaseInsensitiveStatus_Works()
    {
        _tableRepoMock.Setup(r => r.GetFilteredAsync("available", null))
            .ReturnsAsync(new List<Table>());

        // Enum.TryParse with ignoreCase=true
        await _tableService.GetAllAsync("available", null);
        _tableRepoMock.Verify(r => r.GetFilteredAsync("available", null), Times.Once);
    }

    [Fact]
    public async Task GetAll_NoMatchingTables_ReturnsEmptyList()
    {
        _tableRepoMock.Setup(r => r.GetFilteredAsync("RESERVED", null)).ReturnsAsync(new List<Table>());

        var result = await _tableService.GetAllAsync("RESERVED", null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_ResponseContainsQrCode()
    {
        _tableRepoMock.Setup(r => r.GetFilteredAsync(null, null))
            .ReturnsAsync(new List<Table> { new() { Id = 1, TableNumber = 5, QrCode = "QR-TABLE-5", Status = TableStatus.AVAILABLE } });

        var result = await _tableService.GetAllAsync(null, null);

        Assert.Equal("QR-TABLE-5", result[0].QrCode);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 2: ScanTableAsync — POST /api/v1/tables/{id}/scan
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Scan_AvailableTableNoSession_CreatesNewSessionAndSetsOccupied()
    {
        var table = new Table { Id = 1, TableNumber = 10, Status = TableStatus.AVAILABLE, Capacity = 4 };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => { s.Id = 50; return s; });

        var result = await _tableService.ScanTableAsync(1, new ScanTableRequest());

        Assert.True(result.IsNewSession);
        Assert.Equal(50, result.SessionId);
        Assert.Equal(TableStatus.OCCUPIED, table.Status);
        // SaveChanges được gọi 2 lần: 1 lần cho session+table, 1 lần cho participant (HOST)
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Scan_TableWithExistingSession_JoinsSessionWithoutCreatingNew()
    {
        var table = new Table { Id = 1, TableNumber = 10, Status = TableStatus.OCCUPIED };
        var existingSession = new DiningSession { Id = 99, TableId = 1, Status = DiningSessionStatus.ACTIVE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync(existingSession);

        var result = await _tableService.ScanTableAsync(1, new ScanTableRequest());

        Assert.False(result.IsNewSession);
        Assert.Equal(99, result.SessionId);
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Never);
        // SaveChanges được gọi 1 lần vì khách mới (chưa có CustomerId/GuestSessionId trùng) được thêm làm participant
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _participantRepoMock.Verify(r => r.AddAsync(It.IsAny<SessionParticipant>()), Times.Once);
    }

    // BUG DETECTION: "alreadyIn" check so sánh p.GuestSessionId == request.GuestSessionId bằng
    // in-memory LINQ (List<T>.Any), nên khi CẢ HAI đều null, C# coi null == null là true.
    // Hệ quả: 2 khách ẩn danh KHÁC NHAU (không CustomerId, không GuestSessionId — vd CUSTOMER
    // quét bàn nhưng client không gửi CustomerId trong body) cùng bàn sẽ bị nhận diện là MỘT người,
    // người thứ 2 không được thêm participant mới -> GetParticipantsAsync sẽ thiếu người này.
    [Fact]
    public async Task Scan_ExistingSession_TwoAnonymousScans_SecondPersonMergedWithFirst_PotentialBug()
    {
        var table = new Table { Id = 1, TableNumber = 10, Status = TableStatus.OCCUPIED };
        var existingSession = new DiningSession { Id = 99, TableId = 1, Status = DiningSessionStatus.ACTIVE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync(existingSession);

        // Khách ẩn danh #1 quét bàn -> được thêm làm participant (CustomerId=null, GuestSessionId=null)
        await _tableService.ScanTableAsync(1, new ScanTableRequest());

        // Mô phỏng việc participant vừa tạo được persist + load lại từ DB (như trong production thực tế,
        // GetActiveByTableIdAsync sẽ Include participant này ở lần gọi kế tiếp)
        existingSession.Participants.Add(new SessionParticipant
        {
            SessionId = 99, CustomerId = null, GuestSessionId = null, Role = ParticipantRole.MEMBER
        });

        // Khách ẩn danh #2 (một người hoàn toàn khác) quét cùng bàn
        await _tableService.ScanTableAsync(1, new ScanTableRequest());

        // BUG: lẽ ra phải có 2 participant record (2 người khác nhau), nhưng vì null == null
        // nên khách #2 bị coi là "đã có mặt" -> AddAsync chỉ được gọi 1 lần duy nhất.
        _participantRepoMock.Verify(r => r.AddAsync(It.IsAny<SessionParticipant>()), Times.Once);
    }

    [Fact]
    public async Task Scan_WithCustomerId_ValidatesCustomerExists()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        var customer = new Customer { Id = 5, FullName = "Test" };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _customerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(customer);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);

        var result = await _tableService.ScanTableAsync(1, new ScanTableRequest { CustomerId = 5 });

        Assert.True(result.IsNewSession);
        _customerRepoMock.Verify(r => r.GetByIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task Scan_WithInvalidCustomerId_ThrowsEntityNotFound()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _customerRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _tableService.ScanTableAsync(1, new ScanTableRequest { CustomerId = 999 }));
    }

    [Fact]
    public async Task Scan_TableNotFound_ThrowsEntityNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _tableService.ScanTableAsync(999, new ScanTableRequest()));
    }

    [Fact]
    public async Task Scan_MaintenanceTable_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, TableNumber = 5, Status = TableStatus.MAINTENANCE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.ScanTableAsync(1, new ScanTableRequest()));

        Assert.Contains("bảo trì", ex.Message);
    }

    [Fact]
    public async Task Scan_ReservedTable_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, TableNumber = 5, Status = TableStatus.RESERVED };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.ScanTableAsync(1, new ScanTableRequest()));

        Assert.Contains("đặt trước", ex.Message);
    }

    [Fact]
    public async Task Scan_NewSession_SetsCustomerIdInSession()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _customerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5 });
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);

        DiningSession? captured = null;
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .Callback<DiningSession>(s => captured = s)
            .ReturnsAsync((DiningSession s) => s);

        await _tableService.ScanTableAsync(1, new ScanTableRequest { CustomerId = 5 });

        Assert.Equal(5, captured!.CustomerId);
        Assert.Equal(DiningSessionStatus.ACTIVE, captured.Status);
    }

    [Fact]
    public async Task Scan_WithoutCustomerId_SessionHasNullCustomerId()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);

        DiningSession? captured = null;
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .Callback<DiningSession>(s => captured = s)
            .ReturnsAsync((DiningSession s) => s);

        await _tableService.ScanTableAsync(1, new ScanTableRequest());

        Assert.Null(captured!.CustomerId);
    }

    [Fact]
    public async Task Scan_ResponseMessageContainsTableNumber()
    {
        var table = new Table { Id = 1, TableNumber = 42, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);

        var result = await _tableService.ScanTableAsync(1, new ScanTableRequest());

        Assert.Contains("42", result.Message);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 3: UpdateStatusAsync — PATCH /api/v1/tables/{id}/status
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatus_AvailableToReserved_UpdatesSuccessfully()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var result = await _tableService.UpdateStatusAsync(1, "RESERVED");

        Assert.Equal("RESERVED", result.Status);
        Assert.Equal(TableStatus.RESERVED, table.Status);
    }

    [Fact]
    public async Task UpdateStatus_OccupiedToAvailable_ClosesActiveSession()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.OCCUPIED };
        var activeSession = new DiningSession { Id = 10, Status = DiningSessionStatus.ACTIVE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync(activeSession);

        await _tableService.UpdateStatusAsync(1, "AVAILABLE");

        Assert.Equal(DiningSessionStatus.CLOSED, activeSession.Status);
        Assert.NotNull(activeSession.EndedAt);
        Assert.Equal(TableStatus.AVAILABLE, table.Status);
    }

    [Fact]
    public async Task UpdateStatus_OccupiedToAvailable_NoActiveSession_StillWorks()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.OCCUPIED };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);

        var result = await _tableService.UpdateStatusAsync(1, "AVAILABLE");

        Assert.Equal("AVAILABLE", result.Status);
    }

    [Fact]
    public async Task UpdateStatus_MaintenanceToOccupied_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, TableNumber = 5, Status = TableStatus.MAINTENANCE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateStatusAsync(1, "OCCUPIED"));

        Assert.Contains("bảo trì", ex.Message);
    }

    [Fact]
    public async Task UpdateStatus_TableNotFound_ThrowsEntityNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _tableService.UpdateStatusAsync(999, "AVAILABLE"));
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateStatusAsync(1, "INVALID_STATUS"));
    }

    [Fact]
    public async Task UpdateStatus_MaintenanceToAvailable_AllowedNoSideEffect()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.MAINTENANCE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var result = await _tableService.UpdateStatusAsync(1, "AVAILABLE");

        Assert.Equal("AVAILABLE", result.Status);
        _sessionRepoMock.Verify(r => r.GetActiveByTableIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_CallsSaveChanges()
    {
        var table = new Table { Id = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        await _tableService.UpdateStatusAsync(1, "OCCUPIED");

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 4: CreateReservationAsync — POST /api/v1/tables/reservations
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateReservation_ValidCustomer_CreatesWithPendingStatus()
    {
        var table = new Table { Id = 1, TableNumber = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        var customer = new Customer { Id = 5, FullName = "Nguyễn Văn A" };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _customerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(customer);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TableReservation>());
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<TableReservation>()))
            .ReturnsAsync((TableReservation tr) => { tr.Id = 100; return tr; });

        var result = await _tableService.CreateReservationAsync(new CreateReservationRequest
        {
            TableId = 1,
            CustomerId = 5,
            PartySize = 3,
            ReservationTime = DateTime.UtcNow.AddHours(5)
        });

        Assert.Equal("PENDING", result.Status);
        Assert.Equal(100, result.ReservationId);
        Assert.Equal(1, result.TableNumber);
    }

    [Fact]
    public async Task CreateReservation_GuestWithoutAccount_CreatesWithGuestInfo()
    {
        var table = new Table { Id = 1, TableNumber = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TableReservation>());
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<TableReservation>()))
            .ReturnsAsync((TableReservation tr) => { tr.Id = 101; return tr; });

        var result = await _tableService.CreateReservationAsync(new CreateReservationRequest
        {
            TableId = 1,
            GuestName = "Trần Văn B",
            GuestPhone = "0901234567",
            PartySize = 2,
            ReservationTime = DateTime.UtcNow.AddHours(3)
        });

        Assert.Equal("Trần Văn B", result.GuestName);
        Assert.Equal("0901234567", result.GuestPhone);
    }

    [Fact]
    public async Task CreateReservation_TableNotFound_ThrowsEntityNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 999, CustomerId = 1, PartySize = 2,
                ReservationTime = DateTime.UtcNow.AddHours(1)
            }));
    }

    [Fact]
    public async Task CreateReservation_MaintenanceTable_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, TableNumber = 5, Status = TableStatus.MAINTENANCE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, CustomerId = 1, PartySize = 2,
                ReservationTime = DateTime.UtcNow.AddHours(1)
            }));

        Assert.Contains("bảo trì", ex.Message);
    }

    [Fact]
    public async Task CreateReservation_PartySizeZero_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, CustomerId = 1, PartySize = 0,
                ReservationTime = DateTime.UtcNow.AddHours(1)
            }));
    }

    [Fact]
    public async Task CreateReservation_NegativePartySize_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, CustomerId = 1, PartySize = -3,
                ReservationTime = DateTime.UtcNow.AddHours(1)
            }));
    }

    [Fact]
    public async Task CreateReservation_PartySizeExceedsCapacity_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, TableNumber = 3, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, CustomerId = 1, PartySize = 8,
                ReservationTime = DateTime.UtcNow.AddHours(1)
            }));

        Assert.Contains("sức chứa", ex.Message);
    }

    [Fact]
    public async Task CreateReservation_PastTime_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, CustomerId = 1, PartySize = 2,
                ReservationTime = DateTime.UtcNow.AddMinutes(-30)
            }));
    }

    [Fact]
    public async Task CreateReservation_TimeConflict_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, TableNumber = 3, Capacity = 4, Status = TableStatus.AVAILABLE };
        var futureTime = DateTime.UtcNow.AddHours(5);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, futureTime))
            .ReturnsAsync(new List<TableReservation> { new() { Id = 50, Status = ReservationStatus.CONFIRMED } });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, CustomerId = 1, PartySize = 2,
                ReservationTime = futureTime
            }));

        Assert.Contains("khung giờ", ex.Message);
    }

    [Fact]
    public async Task CreateReservation_CustomerNotFound_ThrowsEntityNotFound()
    {
        var table = new Table { Id = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TableReservation>());
        _customerRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, CustomerId = 999, PartySize = 2,
                ReservationTime = DateTime.UtcNow.AddHours(3)
            }));
    }

    [Fact]
    public async Task CreateReservation_NoCustomerIdAndNoGuestName_ThrowsBusinessRuleViolation()
    {
        var table = new Table { Id = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TableReservation>());

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.CreateReservationAsync(new CreateReservationRequest
            {
                TableId = 1, PartySize = 2,
                ReservationTime = DateTime.UtcNow.AddHours(3)
            }));
    }

    [Fact]
    public async Task CreateReservation_PartySizeExactlyEqualToCapacity_Succeeds()
    {
        var table = new Table { Id = 1, TableNumber = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TableReservation>());
        _customerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5 });
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<TableReservation>()))
            .ReturnsAsync((TableReservation tr) => tr);

        var result = await _tableService.CreateReservationAsync(new CreateReservationRequest
        {
            TableId = 1, CustomerId = 5, PartySize = 4,
            ReservationTime = DateTime.UtcNow.AddHours(3)
        });

        Assert.Equal("PENDING", result.Status);
    }

    [Fact]
    public async Task CreateReservation_NotesArePersisted()
    {
        var table = new Table { Id = 1, TableNumber = 1, Capacity = 4, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TableReservation>());
        _customerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5 });
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<TableReservation>()))
            .ReturnsAsync((TableReservation tr) => tr);

        var result = await _tableService.CreateReservationAsync(new CreateReservationRequest
        {
            TableId = 1, CustomerId = 5, PartySize = 2,
            ReservationTime = DateTime.UtcNow.AddHours(3),
            Notes = "Cần ghế trẻ em"
        });

        Assert.Equal("Cần ghế trẻ em", result.Notes);
    }

    // BUG DETECTION: có thể đặt bàn OCCUPIED — bàn đang có khách vẫn cho đặt trước
    [Fact]
    public async Task CreateReservation_OccupiedTable_DoesNotBlock_PotentialBug()
    {
        var table = new Table { Id = 1, TableNumber = 1, Capacity = 4, Status = TableStatus.OCCUPIED };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _reservationRepoMock.Setup(r => r.GetActiveByTableAndTimeAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TableReservation>());
        _customerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5 });
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<TableReservation>()))
            .ReturnsAsync((TableReservation tr) => tr);

        // BUG: Đặt bàn đang OCCUPIED không bị chặn (chỉ chặn MAINTENANCE).
        // Trong thực tế, nếu khách đặt trước cho bàn đang có khách ngồi
        // thì vẫn ok nếu thời gian đặt là tương lai xa (VD: 3 tiếng nữa).
        // Nhưng nếu đặt 30 phút nữa mà bàn đang OCCUPIED thì rủi ro cao.
        // → Gợi ý: thêm cảnh báo hoặc chặn nếu bàn OCCUPIED + thời gian quá gần.
        var result = await _tableService.CreateReservationAsync(new CreateReservationRequest
        {
            TableId = 1, CustomerId = 5, PartySize = 2,
            ReservationTime = DateTime.UtcNow.AddHours(3)
        });

        Assert.Equal("PENDING", result.Status);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 5: UpdateReservationStatusAsync — PATCH /api/v1/tables/reservations/{id}/status
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateReservationStatus_PendingToConfirmed_Success()
    {
        var reservation = new TableReservation { Id = 1, TableId = 1, Status = ReservationStatus.PENDING };
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var result = await _tableService.UpdateReservationStatusAsync(1, "CONFIRMED");

        Assert.Equal("CONFIRMED", result.Status);
        Assert.Null(result.TableStatus);
    }

    [Fact]
    public async Task UpdateReservationStatus_ConfirmedToCheckedIn_SetsTableOccupiedAndCreatesSession()
    {
        var reservation = new TableReservation
        {
            Id = 1, TableId = 1, CustomerId = 5, Status = ReservationStatus.CONFIRMED,
            GuestName = "Test", GuestPhone = "0901234567"
        };
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.RESERVED };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);

        var result = await _tableService.UpdateReservationStatusAsync(1, "CHECKED_IN");

        Assert.Equal("CHECKED_IN", result.Status);
        Assert.Equal("OCCUPIED", result.TableStatus);
        Assert.Equal(TableStatus.OCCUPIED, table.Status);
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationStatus_CheckedIn_CreatesSessionWithCorrectData()
    {
        var reservation = new TableReservation
        {
            Id = 1, TableId = 3, CustomerId = 7, Status = ReservationStatus.CONFIRMED,
            GuestName = "Nguyễn A", GuestPhone = "0909090909"
        };
        var table = new Table { Id = 3, TableNumber = 5, Status = TableStatus.AVAILABLE };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(table);

        DiningSession? capturedSession = null;
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .Callback<DiningSession>(s => capturedSession = s)
            .ReturnsAsync((DiningSession s) => s);

        await _tableService.UpdateReservationStatusAsync(1, "CHECKED_IN");

        Assert.NotNull(capturedSession);
        Assert.Equal(7, capturedSession!.CustomerId);
        Assert.Equal(3, capturedSession.TableId);
        Assert.Equal("Nguyễn A", capturedSession.GuestName);
        Assert.Equal("0909090909", capturedSession.GuestPhone);
        Assert.Equal(DiningSessionStatus.ACTIVE, capturedSession.Status);
    }

    [Fact]
    public async Task UpdateReservationStatus_ConfirmedToNoShow_Success()
    {
        var reservation = new TableReservation { Id = 1, TableId = 1, Status = ReservationStatus.CONFIRMED };
        var table = new Table { Id = 1, Status = TableStatus.RESERVED };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var result = await _tableService.UpdateReservationStatusAsync(1, "NO_SHOW");

        Assert.Equal("NO_SHOW", result.Status);
    }

    [Fact]
    public async Task UpdateReservationStatus_PendingToCancelled_Success()
    {
        var reservation = new TableReservation { Id = 1, TableId = 1, Status = ReservationStatus.PENDING };
        var table = new Table { Id = 1, Status = TableStatus.AVAILABLE };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var result = await _tableService.UpdateReservationStatusAsync(1, "CANCELLED");

        Assert.Equal("CANCELLED", result.Status);
    }

    [Fact]
    public async Task UpdateReservationStatus_ReservationNotFound_ThrowsEntityNotFound()
    {
        _reservationRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((TableReservation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _tableService.UpdateReservationStatusAsync(999, "CONFIRMED"));
    }

    [Fact]
    public async Task UpdateReservationStatus_InvalidStatus_ThrowsBusinessRuleViolation()
    {
        var reservation = new TableReservation { Id = 1, Status = ReservationStatus.PENDING };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateReservationStatusAsync(1, "INVALID"));
    }

    [Fact]
    public async Task UpdateReservationStatus_AlreadyCheckedIn_ThrowsBusinessRuleViolation()
    {
        var reservation = new TableReservation { Id = 1, Status = ReservationStatus.CHECKED_IN };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateReservationStatusAsync(1, "CANCELLED"));

        Assert.Contains("check-in", ex.Message);
    }

    [Fact]
    public async Task UpdateReservationStatus_AlreadyCancelled_ThrowsBusinessRuleViolation()
    {
        var reservation = new TableReservation { Id = 1, Status = ReservationStatus.CANCELLED };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateReservationStatusAsync(1, "CONFIRMED"));
    }

    [Fact]
    public async Task UpdateReservationStatus_AlreadyNoShow_ThrowsBusinessRuleViolation()
    {
        var reservation = new TableReservation { Id = 1, Status = ReservationStatus.NO_SHOW };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateReservationStatusAsync(1, "CONFIRMED"));
    }

    [Fact]
    public async Task UpdateReservationStatus_CheckInOccupiedTable_ThrowsBusinessRuleViolation()
    {
        var reservation = new TableReservation { Id = 1, TableId = 1, Status = ReservationStatus.CONFIRMED };
        var table = new Table { Id = 1, TableNumber = 5, Status = TableStatus.OCCUPIED };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateReservationStatusAsync(1, "CHECKED_IN"));

        Assert.Contains("có khách", ex.Message);
    }

    [Fact]
    public async Task UpdateReservationStatus_CheckInMaintenanceTable_ThrowsBusinessRuleViolation()
    {
        var reservation = new TableReservation { Id = 1, TableId = 1, Status = ReservationStatus.CONFIRMED };
        var table = new Table { Id = 1, TableNumber = 5, Status = TableStatus.MAINTENANCE };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _tableService.UpdateReservationStatusAsync(1, "CHECKED_IN"));

        Assert.Contains("bảo trì", ex.Message);
    }

    [Fact]
    public async Task UpdateReservationStatus_TableDeletedAfterReservation_ThrowsEntityNotFound()
    {
        var reservation = new TableReservation { Id = 1, TableId = 99, Status = ReservationStatus.PENDING };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Table?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _tableService.UpdateReservationStatusAsync(1, "CONFIRMED"));
    }

    // BUG DETECTION: PENDING → CHECKED_IN bỏ qua CONFIRMED
    [Fact]
    public async Task UpdateReservationStatus_PendingToCheckedIn_AllowedButSkipsConfirm_PotentialBug()
    {
        var reservation = new TableReservation { Id = 1, TableId = 1, Status = ReservationStatus.PENDING };
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);

        // BUG: Cho phép PENDING → CHECKED_IN trực tiếp, bỏ qua bước CONFIRMED.
        // Trong thực tế nhà hàng, quy trình đúng: PENDING → CONFIRMED → CHECKED_IN.
        // Bỏ qua CONFIRMED có thể gây nhầm lẫn khi đối soát lịch đặt bàn.
        // → Gợi ý: thêm state machine validation cho reservation status transitions.
        var result = await _tableService.UpdateReservationStatusAsync(1, "CHECKED_IN");

        Assert.Equal("CHECKED_IN", result.Status);
    }

    // BUG DETECTION: PENDING → NO_SHOW — khách chưa confirm mà đánh absent
    [Fact]
    public async Task UpdateReservationStatus_PendingToNoShow_AllowedButSemanticallyWrong()
    {
        var reservation = new TableReservation { Id = 1, TableId = 1, Status = ReservationStatus.PENDING };
        var table = new Table { Id = 1, Status = TableStatus.AVAILABLE };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        // BUG: Khách chưa xác nhận (PENDING) đã bị đánh NO_SHOW.
        // Đúng ra NO_SHOW chỉ nên từ CONFIRMED (khách xác nhận rồi nhưng không đến).
        // → Gợi ý: chặn PENDING → NO_SHOW.
        var result = await _tableService.UpdateReservationStatusAsync(1, "NO_SHOW");

        Assert.Equal("NO_SHOW", result.Status);
    }
}
