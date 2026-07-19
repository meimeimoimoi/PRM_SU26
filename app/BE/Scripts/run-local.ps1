# Chạy 6 service .NET local qua `dotnet run` (KHÔNG dùng Docker) — mỗi service mở 1 cửa sổ
# PowerShell riêng để xem log/lỗi trực tiếp, giống hệt cách mở tay 6 terminal.
#
# Yêu cầu trước khi chạy:
#   - Postgres tại localhost:5432, user postgres, password 12345, database smartdine
#     (khớp appsettings.Development.json của Identity/Menu/Order/Table).
#     Cách nhanh nhất: docker run --name smartdine-pg -e POSTGRES_PASSWORD=12345 `
#       -e POSTGRES_DB=smartdine -p 5432:5432 -d postgres:15-alpine
#   - KHÔNG cần tự chạy migration tay: Identity.API tự động MigrateAsync() + seed dữ liệu mẫu
#     mỗi khi khởi động (xem Program.cs). Chỉ cần chạy script này, Identity sẽ tự lo phần DB.
#     Tài khoản MANAGER seed sẵn: admin@smartdine.com / Password123!

$RootDir = Split-Path -Parent $PSScriptRoot

$Services = @(
    @{ Name = "Identity"; Path = "Services\SmartDine.Identity.API"; Url = "http://localhost:5001" }
    @{ Name = "Menu";     Path = "Services\SmartDine.Menu.API";     Url = "http://localhost:5002" }
    @{ Name = "Order";    Path = "Services\SmartDine.Order.API";    Url = "http://localhost:5003" }
    @{ Name = "Table";    Path = "Services\SmartDine.Table.API";    Url = "http://localhost:5004" }
    @{ Name = "AI";       Path = "Services\SmartDine.AI.API";       Url = "http://localhost:5005" }
    @{ Name = "Gateway";  Path = "Services\SmartDine.Gateway";      Url = "http://localhost:5000" }
)

Write-Host "Kiem tra Postgres tai localhost:5432..." -ForegroundColor Cyan
$pgReady = Test-NetConnection -ComputerName localhost -Port 5432 -WarningAction SilentlyContinue
if (-not $pgReady.TcpTestSucceeded) {
    Write-Host "CANH BAO: khong ket noi duoc Postgres o localhost:5432." -ForegroundColor Yellow
    Write-Host "  Chay truoc: docker run --name smartdine-pg -e POSTGRES_PASSWORD=12345 -e POSTGRES_DB=smartdine -p 5432:5432 -d postgres:15-alpine" -ForegroundColor Yellow
    $continue = Read-Host "Van tiep tuc khoi chay cac service? (y/N)"
    if ($continue -ne 'y') { exit 1 }
}

Write-Host ""
Write-Host "Dang khoi chay $($Services.Count) service (moi service 1 cua so rieng)..." -ForegroundColor Cyan

foreach ($svc in $Services) {
    $fullPath = Join-Path $RootDir $svc.Path
    Write-Host "  -> $($svc.Name) ($($svc.Url))"
    Start-Process powershell -ArgumentList @(
        '-NoExit',
        '-Command',
        "cd '$fullPath'; Write-Host 'SmartDine $($svc.Name) API - $($svc.Url)' -ForegroundColor Green; dotnet run"
    )
    Start-Sleep -Milliseconds 800
}

Write-Host ""
Write-Host "Da mo $($Services.Count) cua so. Doi vai giay de cac service khoi dong xong." -ForegroundColor Green
Write-Host ""
Write-Host "Services:" -ForegroundColor Yellow
foreach ($svc in $Services) {
    Write-Host "   $($svc.Name): $($svc.Url)"
}
Write-Host "   PostgreSQL: localhost:5432"
Write-Host ""
Write-Host "Dong tung cua so PowerShell (hoac Ctrl+C ben trong) de tat service tuong ung." -ForegroundColor Cyan
