# Setup SSL Certificates for Frontend Development
# This script generates trusted local SSL certificates using mkcert

$certsDir = ".certs"
$certBaseName = "_wildcard.localtest.me"

Write-Host "Setting up SSL certificates for frontend development..." -ForegroundColor Green
Write-Host ""

# Check if mkcert is installed
try {
    $mkcertVersion = mkcert -version 2>&1
    Write-Host "✓ mkcert found: $mkcertVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ mkcert is not installed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install mkcert first:" -ForegroundColor Yellow
    Write-Host "  Windows: choco install mkcert (requires admin)" -ForegroundColor White
    Write-Host "  Mac: brew install mkcert" -ForegroundColor White
    Write-Host "  Linux: See https://github.com/FiloSottile/mkcert#installation" -ForegroundColor White
    Write-Host ""
    Write-Host "After installing, run: mkcert -install" -ForegroundColor Yellow
    exit 1
}

# Create .certs directory if it doesn't exist
if (-not (Test-Path $certsDir)) {
    Write-Host "Creating $certsDir directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $certsDir | Out-Null
}

# Check if certificates already exist
$certFile = Join-Path $certsDir "$certBaseName.pem"
$keyFile = Join-Path $certsDir "$certBaseName-key.pem"

if ((Test-Path $certFile) -and (Test-Path $keyFile)) {
    Write-Host ""
    Write-Host "Certificates already exist:" -ForegroundColor Yellow
    Write-Host "  Cert: $certFile" -ForegroundColor Gray
    Write-Host "  Key:  $keyFile" -ForegroundColor Gray
    Write-Host ""
    $response = Read-Host "Regenerate certificates? (y/n)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "Keeping existing certificates." -ForegroundColor Green
        exit 0
    }
    Write-Host ""
}

# Generate certificates
Write-Host "Generating SSL certificates..." -ForegroundColor Yellow
Push-Location $certsDir

try {
    # Generate wildcard certificate for *.localtest.me and localtest.me
    mkcert "*.localtest.me" "localtest.me"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✓ Certificates generated successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Certificate files:" -ForegroundColor Cyan
        Get-ChildItem -Filter "*.pem" | ForEach-Object {
            Write-Host "  - $($_.Name)" -ForegroundColor White
        }
        Write-Host ""
        Write-Host "✓ Your development server will now use HTTPS with trusted certificates" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "✗ Certificate generation failed" -ForegroundColor Red
        Write-Host ""
        Write-Host "Troubleshooting:" -ForegroundColor Yellow
        Write-Host "  1. Make sure you ran 'mkcert -install' as administrator" -ForegroundColor White
        Write-Host "  2. Check if mkcert is in your PATH" -ForegroundColor White
        Write-Host "  3. Try running this script as administrator" -ForegroundColor White
        exit 1
    }
} finally {
    Pop-Location
}
