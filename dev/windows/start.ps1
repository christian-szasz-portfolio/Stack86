$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$devRoot = Split-Path -Parent $scriptDirectory
$solutionRoot = Split-Path -Parent $devRoot

$backendPath = Join-Path $solutionRoot 'src\Stack86.Web'
$clientAppPath = Join-Path $solutionRoot 'src\Stack86.Web\ClientApp'

$runAppScript = Join-Path $scriptDirectory 'run-app.ps1'

wt.exe -p Powershell -d "$backendPath" --title "Backend" --suppressApplicationTitle pwsh -NoExit -Command "& '$runAppScript' -ProjectPath '$backendPath'" `; `
    new-tab -p Powershell -d "$clientAppPath" --title "Frontend" --suppressApplicationTitle pwsh -NoExit -Command "npm run dev" `; `
    focus-tab -t 0

Start-Sleep -Seconds 10

Start-Process "https://localhost:1234"
Start-Process "http://localhost:1998/scalar/v1"