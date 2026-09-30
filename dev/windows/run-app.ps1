[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath
)

function Get-ProjectName {
    param(
        [string]$FullProjectPath
    )

    $resolvedPath = (Resolve-Path -Path $FullProjectPath).Path
    return Split-Path -Path $resolvedPath -Leaf
}

function Get-TargetFramework {
    param(
        [string]$FullProjectPath
    )

    $projectName = Get-ProjectName -FullProjectPath $FullProjectPath
    $xml = [xml](Get-Content -Path "$($FullProjectPath)\$($projectName).csproj")

    $targetFramework = $xml.Project.PropertyGroup.TargetFramework

    if ([string]::IsNullOrEmpty($targetFramework)) {
        $targetFramework = $xml.Project.PropertyGroup.TargetFrameworks
    }

    return "$($targetFramework)".Trim()
}

function Get-RuntimeIdentifier {
    param(
        [string]$FullProjectPath
    )

    $projectName = Get-ProjectName -FullProjectPath $FullProjectPath
    $xml = [xml](Get-Content -Path "$($FullProjectPath)\$($projectName).csproj")

    $runtimeIdentifier = $xml.Project.PropertyGroup.RuntimeIdentifier

    if ([string]::IsNullOrEmpty($runtimeIdentifier)) {
        $runtimeIdentifier = $xml.Project.PropertyGroup.RuntimeIdentifiers
    }

    return "$($runtimeIdentifier)".Trim()
}

function Remove-Tree {
    param(
        [int]$ppid
    )

    Get-CimInstance Win32_Process | Where-Object { $_.ParentProcessId -eq $ppid } | ForEach-Object { Remove-Tree -ppid $_.ProcessId }
    Stop-Process -Id $ppid -ErrorAction SilentlyContinue
}

$projectPath = (Resolve-Path -Path $ProjectPath).Path
$project = Get-ProjectName -FullProjectPath $projectPath
$targetFramework = Get-TargetFramework -FullProjectPath $projectPath
$runtimeIdentifier = Get-RuntimeIdentifier -FullProjectPath $projectPath

$outputSegments = @("bin", "Debug", $targetFramework)
if (-not [string]::IsNullOrEmpty($runtimeIdentifier)) {
    $outputSegments += $runtimeIdentifier
}
$waitPath = $projectPath
foreach ($segment in $outputSegments) {
    $waitPath = Join-Path -Path $waitPath -ChildPath $segment
}
$waitForFile = Join-Path -Path $waitPath -ChildPath "build_completed.txt"
$buildStartingFile = Join-Path -Path $waitPath -ChildPath "build_starting.txt"
$commandArgs = @("run", "--project", $projectPath, "--no-build", "--framework", $targetFramework)
$quotedArgs = ($commandArgs | ForEach-Object {
        if ($_.Contains(' ')) { '"{0}"' -f $_ } else { $_ }
    }) -join ' '
$command = "dotnet $quotedArgs"

$Host.UI.RawUI.WindowTitle = $project

try {
    :runCommand while ($true) {
        [console]::TreatControlCAsInput = $true;

        $lastBuildStarting = (Get-Item $buildStartingFile -ErrorAction SilentlyContinue).LastWriteTimeUtc
        $lastBuildCompleted = (Get-Item $waitForFile -ErrorAction SilentlyContinue).LastWriteTimeUtc

        Write-Host "$project> $command";

        try {
            $w = Start-Process -FilePath "dotnet" -ArgumentList $commandArgs -NoNewWindow -PassThru
        } catch {
            Write-Host $_ -ForegroundColor Red;
            break
        }

        $waitingForBuild = $false;

        :waitForExit while ($true) {
            # Detect new build starting while process is running — kill to release DLL locks
            if (-not $w.HasExited -and (Test-Path $buildStartingFile)) {
                $currentStarting = (Get-Item $buildStartingFile).LastWriteTimeUtc
                if ($currentStarting -ne $lastBuildStarting) {
                    $lastBuildStarting = $currentStarting;
                    Write-Host "Changes detected, stopping for rebuild..." -ForegroundColor Yellow;
                    Remove-Tree -ppid $w.Id;
                }
            }

            # Once process is down, wait for build to complete then restart
            if ($w.HasExited) {
                if (-not $waitingForBuild) {
                    Write-Host "$project stopped, waiting for build..." -ForegroundColor Yellow;
                    $waitingForBuild = $true;
                }

                if ((Test-Path $waitForFile) -and ((Get-Item $waitForFile).LastWriteTimeUtc -ne $lastBuildCompleted)) {
                    Write-Host "Build completed, restarting..." -ForegroundColor Yellow;
                    continue runCommand;
                }
            }

            while ([console]::KeyAvailable) {
                $key = [console]::ReadKey($true);
                if ($key.Key -eq 'C' -and ($key.Modifiers -band [System.ConsoleModifiers]'Control')) {
                    $Host.UI.RawUI.FlushInputBuffer();

                    if (-not $w.HasExited) {
                        Remove-Tree -ppid $w.Id;
                    }

                    break waitForExit;
                }
            }

            Start-Sleep -Milliseconds 100;
        }

        Write-Host "Press enter to restart or Ctrl+C again to exit." -ForegroundColor Yellow;
        Read-Host
    }
} finally {
    [console]::TreatControlCAsInput = $false;
}
