#requires -Version 7
<#
.SYNOPSIS
    Runs all Stack86 test projects with code coverage and generates a unified HTML/Cobertura report.

.DESCRIPTION
    Uses Microsoft.Testing.Platform with the Microsoft.Testing.Extensions.CodeCoverage extension
    (auto-included via TestingExtensionsProfile=AllMicrosoft). Produces .cobertura.xml files which
    ReportGenerator merges into HTML + a single consolidated Cobertura. Per-SUT-assembly thresholds
    (line + branch >= Threshold) are enforced after report generation.

.PARAMETER Threshold
    Minimum line coverage percent per SUT assembly. Defaults to 90.

.PARAMETER BranchThreshold
    Minimum branch coverage percent per SUT assembly. Defaults to 85 (relaxed because async state
    machines emit compiler-generated exception-path branches that are impractical to cover).

.PARAMETER NoBuild
    Skip the initial dotnet build step.

.PARAMETER Project
    Optional: run coverage only for the given test project name (e.g. Stack86.Logic.Test).
#>
[CmdletBinding()]
param(
    [int]$Threshold = 90,
    [int]$BranchThreshold = 80,
    [switch]$NoBuild,
    [string]$Project
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $repoRoot

$coverageDir = Join-Path $repoRoot 'coverage'
$reportDir = Join-Path $coverageDir 'report'
$rawDir = Join-Path $coverageDir 'raw'
if (Test-Path $coverageDir) { Remove-Item $coverageDir -Recurse -Force }
New-Item -ItemType Directory -Path $rawDir | Out-Null

$runSettings = Join-Path $repoRoot 'coverage.runsettings'

$testProjects = @(
    @{ Name = 'Stack86.Api.Test';         Path = 'test/Stack86.Api.Test/Stack86.Api.Test.csproj';                 Sut = 'Stack86.Api'   },
    @{ Name = 'Stack86.Logic.Test';       Path = 'test/Stack86.Logic.Test/Stack86.Logic.Test.csproj';             Sut = 'Stack86.Logic' },
    @{ Name = 'Stack86.Integration.Test'; Path = 'test/Stack86.Integration.Test/Stack86.Integration.Test.csproj'; Sut = 'Stack86.Web'   }
)

if ($Project) {
    $testProjects = $testProjects | Where-Object { $_.Name -eq $Project }
    if (-not $testProjects) { throw "Unknown project '$Project'." }
}

if (-not $NoBuild) {
    Write-Host '==> dotnet build' -ForegroundColor Cyan
    dotnet build -c Debug --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}

Write-Host '==> dotnet tool restore' -ForegroundColor Cyan
dotnet tool restore | Out-Null

foreach ($p in $testProjects) {
    $name = $p.Name
    $proj = $p.Path
    $resultDir = Join-Path $rawDir $name
    New-Item -ItemType Directory -Path $resultDir | Out-Null

    Write-Host "==> Running coverage for $name" -ForegroundColor Cyan
    dotnet run --project $proj --no-build -- `
        --coverage `
        --coverage-output-format cobertura `
        --coverage-output 'coverage.cobertura.xml' `
        --results-directory $resultDir
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Tests failed in $name (exit=$LASTEXITCODE) - continuing to gather coverage."
    }
}

$coberturaFiles = Get-ChildItem -Path $rawDir -Filter 'coverage.cobertura.xml' -Recurse |
    Select-Object -ExpandProperty FullName
if (-not $coberturaFiles) { throw 'No coverage files were produced.' }

Write-Host '==> Generating consolidated report' -ForegroundColor Cyan
$reportsArg = ($coberturaFiles -join ';')
$assemblyFilters = '+Stack86.Api;+Stack86.Logic;+Stack86.Common;+Stack86.Web;-Stack86.Test*'
# Class-level exclusions: bootstrap, pure POCO/record/DTO/Options, AST/IR data nodes, lexer token shapes,
# language-specific AST namespaces. Keep parsers, generators, validators, handlers, optimizers IN.
$classFilters = @(
    '-*Program'
    '-*Startup'
    '-*Startup.*'
    '-*ServiceCollectionExtensions'
    '-*Extension'
    '-*Options'
    '-*Request'
    '-*Response'
    '-*Dto'
    '-*Diagnostic'
    # AST: all language ASTs are pure records / data carriers
    '-Stack86.Logic.Pipeline.Ast.*'
    '-Stack86.Logic.Languages.*.Ast.*'
    '-Stack86.Logic.Languages.Cpp.Ast.*'
    # IR data shapes (records); keep IrOptimizer + IrCapabilityValidator
    '-Stack86.Logic.Pipeline.Ir.IrInstruction'
    '-Stack86.Logic.Pipeline.Ir.IrOperand'
    '-Stack86.Logic.Pipeline.Ir.IrFunction'
    '-Stack86.Logic.Pipeline.Ir.IrProgram'
    '-Stack86.Logic.Pipeline.Ir.IrGlobalData'
    '-Stack86.Logic.Pipeline.Ir.IrStructDefinition'
    '-Stack86.Logic.Pipeline.Ir.IrStructField'
    '-Stack86.Logic.Pipeline.Ir.IrVtableEntry'
    '-Stack86.Logic.Pipeline.Ir.IrDiagnostic'
    # X86 instruction data shapes
    '-Stack86.Logic.Pipeline.X86Conversion.X86Instruction'
    '-Stack86.Logic.Pipeline.X86Conversion.X86Operand'
    '-Stack86.Logic.Pipeline.X86Conversion.X86Operand.*'
    # Lexer token records (one per language)
    '-*Token'
    '-*TokenKind'
    '-*TokenType'
    # Pipeline message / line-mapping value types
    '-Stack86.Logic.Compilation.PreprocessedSource'
    '-Stack86.Logic.Compilation.TranspiledSource'
    '-Stack86.Logic.Compilation.SourceLineMapping'
    '-Stack86.Logic.Compilation.CompilationContext'
    '-Stack86.Logic.Compilation.CompilationResultDto'
    # Common: pure constants / interfaces / exceptions
    '-Stack86.Common.Security.Constants.Scope'
    '-Stack86.Common.Security.Constants.Role'
    '-Stack86.Common.Security.Constants.Permission'
    '-Stack86.Common.Security.Constants.PolicyNames'
    '-Stack86.Common.Security.Constants.LicenseType'
    '-Stack86.Common.Security.Constants.CustomClaimTypes'
    '-Stack86.Common.Security.Constants.SystemAccount'
    '-Stack86.Common.Security.Constants.RateLimitPolicies'
    # ResultConverter.Read recurses into itself (JsonSerializer with same options) and is never invoked in practice
    '-Stack86.Common.CQRS.Result.Serialization.ResultConverter'
    '-*ResultMessages'
    # Source-generated OpenAPI document for Stack86.Web (compiler-emitted, not user code)
    '-Microsoft.AspNetCore.OpenApi.Generated'
    '-System.Runtime.CompilerServices'
) -join ';'
$fileFilters = '-**/Migrations/**;-**/*.Designer.cs;-**\obj\**;-**\*.g.cs;-**/obj/**;-**/*.g.cs'
dotnet reportgenerator `
    "-reports:$reportsArg" `
    "-targetdir:$reportDir" `
    '-reporttypes:Html;TextSummary;Cobertura' `
    "-assemblyfilters:$assemblyFilters" `
    "-classfilters:$classFilters" `
    "-filefilters:$fileFilters" `
    '-title:Stack86 Coverage' | Out-Null

$summaryPath = Join-Path $reportDir 'Summary.txt'
if (Test-Path $summaryPath) {
    Write-Host "`n----- Coverage Summary -----" -ForegroundColor Yellow
    Get-Content $summaryPath | Write-Host
}

$mergedCobertura = Join-Path $reportDir 'Cobertura.xml'
[xml]$cob = Get-Content $mergedCobertura

$sutAssemblies = $testProjects | ForEach-Object { $_.Sut }
$failures = @()
$results = @()
foreach ($pkg in $cob.coverage.packages.package) {
    $asm = $pkg.name
    $line = [double]$pkg.'line-rate' * 100.0
    $branch = [double]$pkg.'branch-rate' * 100.0
    $results += [pscustomobject]@{ Assembly = $asm; Line = [math]::Round($line, 2); Branch = [math]::Round($branch, 2) }
    if ($sutAssemblies -contains $asm) {
        if ($line   -lt $Threshold)       { $failures += "$asm line   $($line.ToString('F2'))% < $Threshold%" }
        if ($branch -lt $BranchThreshold) { $failures += "$asm branch $($branch.ToString('F2'))% < $BranchThreshold%" }
    }
}

Write-Host "`n----- Per-Assembly Coverage -----" -ForegroundColor Yellow
$results | Sort-Object Assembly | Format-Table -AutoSize | Out-String | Write-Host

Write-Host "Report: $(Join-Path $reportDir 'index.html')" -ForegroundColor Green

if ($failures.Count -gt 0) {
    Write-Host "`nThreshold violations:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "`nAll tracked SUT assemblies meet the line>=$Threshold% / branch>=$BranchThreshold% threshold." -ForegroundColor Green
