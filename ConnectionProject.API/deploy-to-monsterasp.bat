@echo off
echo ================================
echo UniShare Backend - MonsterASP.NET Deployment Script
echo ================================

echo.
echo Step 1: Cleaning previous builds...
if exist "publish" (
    rmdir /s /q "publish"
    echo Previous build cleaned.
) else (
    echo No previous build found.
)

echo.
echo Step 2: Building project in Release mode...
dotnet clean --configuration Release
dotnet restore
dotnet build --configuration Release --no-restore

if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo Build successful!

echo.
echo Step 3: Publishing to /publish folder...
dotnet publish --configuration Release --output "publish" --no-build

if %ERRORLEVEL% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

echo.
echo ================================
echo ✅ DEPLOYMENT READY!
echo ================================
echo.
echo Your files are ready in the /publish folder.
echo.
echo NEXT STEPS:
echo 1. Update your MonsterASP.NET database connection string in appsettings.Production.json
echo 2. Upload the contents of /publish folder to your MonsterASP.NET wwwroot via FTP
echo 3. Update CORS policy in Program.cs with your Cloudflare Pages domain
echo.
echo Files to upload: %cd%\publish\*
echo.
echo ⚠️  REMEMBER TO UPDATE:
echo - ConnectionStrings:DefaultConnection (MonsterASP database details)
echo - JwtSettings:SecretKey (generate a strong 64+ character key)
echo - CORS ProductionPolicy origins (your Cloudflare Pages domain)
echo.
pause