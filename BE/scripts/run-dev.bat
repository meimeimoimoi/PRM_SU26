@echo off
echo 🚀 Starting SmartDine in Development mode...
cd /d "%~dp0\..\docker-compose"
docker-compose -f docker-compose.yml up -d
echo ✅ All services started!
echo 🌐 Gateway: http://localhost:5000
echo 📊 Identity: http://localhost:5001
echo 📋 Menu: http://localhost:5002
echo 📦 Order: http://localhost:5003
echo 🪑 Table: http://localhost:5004
echo 🤖 AI: http://localhost:5005