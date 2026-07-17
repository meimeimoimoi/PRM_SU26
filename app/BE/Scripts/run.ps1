# Di chuyển đến thư mục cha chứa file docker-compose.yml
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location "$ScriptDir\.."

Write-Host '🚀 Starting SmartDine Microservices...' -ForegroundColor Cyan

# Khởi chạy Docker Compose
docker-compose up -d --build

Write-Host ''
Write-Host '✅ All services started!' -ForegroundColor Green
Write-Host ''

# Sử dụng nháy đơn để tránh lỗi phân tích cú pháp ký tự đặc biệt
docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'

Write-Host ''
Write-Host '📍 Services:' -ForegroundColor Yellow
Write-Host '   Gateway:    http://localhost:5000'
Write-Host '   Identity:   http://localhost:5001'
Write-Host '   Menu:       http://localhost:5002'
Write-Host '   Order:      http://localhost:5003'
Write-Host '   Table:      http://localhost:5004'
Write-Host '   AI:         http://localhost:5005'
Write-Host '   PostgreSQL: localhost:5432'
Write-Host '   Ollama:     http://localhost:11434'