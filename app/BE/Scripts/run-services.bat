@echo off
cd /d "%~dp0.."
echo 🚀 Starting SmartDine Microservices...
docker-compose up -d --build
echo.
echo ✅ All services started!
echo.
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo.
echo 📍 Services:
echo    Gateway:  http://localhost:5000
echo    Identity: http://localhost:5001
echo    Menu:     http://localhost:5002
echo    Order:    http://localhost:5003
echo    Table:    http://localhost:5004
echo    AI:       http://localhost:5005
echo    PostgreSQL: localhost:5432
echo    Ollama:   http://localhost:11434