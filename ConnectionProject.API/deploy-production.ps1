# UniShare API Production Deployment Script
# Deploys to MonsterASP.NET hosting with IIS

param(
    [switch]$Build = $true,
    [switch]$Publish = $true,
    [switch]$UploadViaFTP = $false,
    [string]$FTPHost = "site42646.siteasp.net",
    [string]$FTPUser = "site42646",
    [string]$FTPPassword = "",
    [string]$LocalPublishPath = "./publish",
    [string]$RemotePath = "/wwwroot"
)

Write-Host "=== UniShare API Production Deployment ===" -ForegroundColor Green
Write-Host "Target: MonsterASP.NET (unishareapp.runasp.net)" -ForegroundColor Cyan
Write-Host ""

# Check if we're in the correct directory
if (!(Test-Path "UniShareProject.API.csproj")) {
    Write-Error "Please run this script from the ConnectionProject.API directory"
    exit 1
}

# Step 1: Clean previous builds
if ($Build) {
    Write-Host "[CLEAN] Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $LocalPublishPath) {
        Remove-Item -Path $LocalPublishPath -Recurse -Force
        Write-Host "   [OK] Removed existing publish directory" -ForegroundColor Green
    }
    
    dotnet clean -c Release
    Write-Host "   [OK] Cleaned solution" -ForegroundColor Green
    Write-Host ""
}

# Step 2: Build and Publish
if ($Publish) {
    Write-Host "[BUILD] Building and publishing for production..." -ForegroundColor Yellow
    
    # Publish with framework-dependent deployment for IIS
    $publishResult = dotnet publish -c Release -o $LocalPublishPath --self-contained false --verbosity minimal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   [OK] Published successfully to $LocalPublishPath" -ForegroundColor Green
        
        # Verify web.config was generated
        $webConfigPath = Join-Path $LocalPublishPath "web.config"
        if (Test-Path $webConfigPath) {
            Write-Host "   [OK] web.config generated automatically" -ForegroundColor Green
        } else {
            Write-Warning "   [WARN] web.config not found - will create manually"
            
            # Create web.config manually
            $webConfigContent = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified"/>
    </handlers>
    <aspNetCore processPath="dotnet" arguments="UniShareProject.API.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="InProcess" />
  </system.webServer>
</configuration>
'@
            $webConfigContent | Out-File -FilePath $webConfigPath -Encoding UTF8
            Write-Host "   [OK] Created web.config manually" -ForegroundColor Green
        }
        
        # Create logs directory for IIS
        $logsPath = Join-Path $LocalPublishPath "logs"
        if (!(Test-Path $logsPath)) {
            New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
            Write-Host "   [OK] Created logs directory for IIS" -ForegroundColor Green
        }
        
        # List published files
        $publishedFiles = Get-ChildItem -Path $LocalPublishPath -Recurse -File | Measure-Object
        Write-Host "   [INFO] Published $($publishedFiles.Count) files" -ForegroundColor Cyan
        
    } else {
        Write-Error "   [ERROR] Publish failed with exit code $LASTEXITCODE"
        exit 1
    }
    Write-Host ""
}

# Step 3: Display deployment information
Write-Host "[INFO] Deployment Information:" -ForegroundColor Yellow
Write-Host "   Published files location: $LocalPublishPath" -ForegroundColor White
Write-Host "   Target server: $FTPHost" -ForegroundColor White
Write-Host "   Target directory: $RemotePath" -ForegroundColor White
Write-Host "   API URL: https://unishareapp.runasp.net" -ForegroundColor White
Write-Host "   Swagger URL: https://unishareapp.runasp.net/swagger" -ForegroundColor White
Write-Host ""

# Step 4: FTP Upload (optional)
if ($UploadViaFTP -and $FTPPassword) {
    Write-Host "[UPLOAD] Uploading to MonsterASP via FTP..." -ForegroundColor Yellow
    
    try {
        # Create FTP webclient
        $webclient = New-Object System.Net.WebClient
        $webclient.Credentials = New-Object System.Net.NetworkCredential($FTPUser, $FTPPassword)
        
        # Get all files to upload
        $filesToUpload = Get-ChildItem -Path $LocalPublishPath -Recurse -File
        $totalFiles = $filesToUpload.Count
        $uploadedFiles = 0
        
        foreach ($file in $filesToUpload) {
            $uploadedFiles++
            $relativePath = $file.FullName.Substring($LocalPublishPath.Length + 1).Replace('\', '/')
            $ftpUri = "ftp://$FTPHost$RemotePath/$relativePath"
            
            # Upload file
            try {
                $webclient.UploadFile($ftpUri, $file.FullName)
                Write-Progress -Activity "Uploading files" -Status "Uploaded $relativePath" -PercentComplete (($uploadedFiles / $totalFiles) * 100)
            } catch {
                Write-Warning "   [WARN] Failed to upload $relativePath : $($_.Exception.Message)"
            }
        }
        
        Write-Host "   [OK] FTP upload completed" -ForegroundColor Green
        $webclient.Dispose()
        
    } catch {
        Write-Error "   [ERROR] FTP upload failed: $($_.Exception.Message)"
        Write-Host "   [INFO] You can manually upload files from $LocalPublishPath to $RemotePath using FileZilla or similar FTP client" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Step 5: Manual upload instructions
if (!$UploadViaFTP) {
    Write-Host "[MANUAL] Manual Upload Instructions:" -ForegroundColor Yellow
    Write-Host "   1. Open FileZilla or your preferred FTP client" -ForegroundColor White
    Write-Host "   2. Connect to: $FTPHost" -ForegroundColor White
    Write-Host "   3. Username: $FTPUser" -ForegroundColor White
    Write-Host "   4. Password: [Your FTP password from MonsterASP panel]" -ForegroundColor White
    Write-Host "   5. Navigate to remote directory: $RemotePath" -ForegroundColor White
    Write-Host "   6. Upload ALL files from: $LocalPublishPath" -ForegroundColor White
    Write-Host "   7. Ensure web.config and all DLLs are uploaded" -ForegroundColor White
    Write-Host ""
}

# Step 6: Testing instructions
Write-Host "[TEST] Testing Instructions:" -ForegroundColor Yellow
Write-Host "   1. Test API health: https://unishareapp.runasp.net" -ForegroundColor White
Write-Host "   2. Test Swagger UI: https://unishareapp.runasp.net/swagger" -ForegroundColor White
Write-Host "   3. Test registration: POST https://unishareapp.runasp.net/api/v1/auth/register" -ForegroundColor White
Write-Host "   4. Test login: POST https://unishareapp.runasp.net/api/v1/auth/login" -ForegroundColor White
Write-Host "   5. Check database via: https://webmssql.monsterasp.net" -ForegroundColor White
Write-Host ""

Write-Host "[SUCCESS] Deployment script completed!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Upload files to MonsterASP (if not done automatically)" -ForegroundColor White
Write-Host "2. Test the API endpoints" -ForegroundColor White
Write-Host "3. Update Cloudflare Pages environment variables" -ForegroundColor White
Write-Host "4. Deploy frontend with updated API URL" -ForegroundColor White