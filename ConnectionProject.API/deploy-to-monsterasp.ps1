# UniShare Backend Deployment Script for MonsterASP.NET
# PowerShell version

Write-Host "================================" -ForegroundColor Cyan
Write-Host "UniShare Backend - MonsterASP.NET Deployment Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Step 1: Clean previous builds
Write-Host ""
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow

if (Test-Path "publish") {
    Remove-Item -Recurse -Force "publish"
    Write-Host "Previous build cleaned." -ForegroundColor Green
} else {
    Write-Host "No previous build found." -ForegroundColor Gray
}

# Step 2: Build project
Write-Host ""
Write-Host "Step 2: Building project in Release mode..." -ForegroundColor Yellow

try {
    dotnet clean --configuration Release
    dotnet restore
    dotnet build --configuration Release --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "Build successful!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 3: Publish
Write-Host ""
Write-Host "Step 3: Publishing to /publish folder..." -ForegroundColor Yellow

try {
    dotnet publish --configuration Release --output "publish" --no-build
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "Publish successful!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Publish failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Success message
Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "✅ DEPLOYMENT READY!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

Write-Host "Your files are ready in the /publish folder." -ForegroundColor White
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Cyan
Write-Host "1. Update your MonsterASP.NET database connection string in appsettings.Production.json" -ForegroundColor White
Write-Host "2. Upload the contents of /publish folder to your MonsterASP.NET wwwroot via FTP" -ForegroundColor White
Write-Host "3. Update CORS policy in Program.cs with your Cloudflare Pages domain" -ForegroundColor White
Write-Host ""
Write-Host "Files to upload: $(Get-Location)\publish\*" -ForegroundColor Gray
Write-Host ""
Write-Host "⚠️  REMEMBER TO UPDATE:" -ForegroundColor Yellow
Write-Host "- ConnectionStrings:DefaultConnection (MonsterASP database details)" -ForegroundColor White
Write-Host "- JwtSettings:SecretKey (generate a strong 64+ character key)" -ForegroundColor White
Write-Host "- CORS ProductionPolicy origins (your Cloudflare Pages domain)" -ForegroundColor White
Write-Host ""

# Display file count
$fileCount = (Get-ChildItem -Recurse "publish" | Measure-Object).Count
$folderSize = (Get-ChildItem -Recurse "publish" | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "📊 Published $fileCount files ($('{0:N1}' -f $folderSize) MB)" -ForegroundColor Cyan

Write-Host ""
Read-Host "Press Enter to exit"