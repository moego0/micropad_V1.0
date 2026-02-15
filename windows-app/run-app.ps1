# Run Micropad.App - closes any existing instance first so build can copy DLLs
$proc = Get-Process -Name "Micropad.App" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Closing existing Micropad.App (PID $($proc.Id))..."
    $proc | Stop-Process -Force
    Start-Sleep -Seconds 2
}
dotnet run --project Micropad.App @args
