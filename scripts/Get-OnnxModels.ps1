[CmdletBinding()]
param(
    # Базовый URL, откуда скачивать модели (GitHub Release, Hugging Face, ModelScope, свой сервер).
    # Можно не указывать, если он задан в models.manifest.json -> baseUrl.
    [string]$BaseUrl,

    # Куда складывать модели.
    [string]$Destination = (Join-Path $PSScriptRoot '..\src\Shared\OnnxModels'),

    # Скачать заново, даже если файл уже есть.
    [switch]$Force,

    # Только проверить контрольные суммы уже имеющихся файлов (без скачивания).
    [switch]$VerifyOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

trap {
    Write-Host ""
    Write-Host ("ОШИБКА: {0}" -f $_.Exception.Message) -ForegroundColor Red
    exit 1
}

$manifestPath = Join-Path $PSScriptRoot 'models.manifest.json'
if (-not (Test-Path $manifestPath)) {
    throw "Не найден манифест моделей: $manifestPath"
}

$manifest = Get-Content $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json

if (-not (Test-Path $Destination)) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
}
$Destination = (Resolve-Path $Destination).Path

function Get-Sha256($path) {
    return (Get-FileHash -Path $path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$source = if ($BaseUrl) { $BaseUrl } else { [string]$manifest.baseUrl }
if (-not $VerifyOnly -and [string]::IsNullOrWhiteSpace($source)) {
    throw "Не задан источник моделей. Укажите -BaseUrl <url> или заполните baseUrl в scripts/models.manifest.json"
}
if ($source) {
    $source = $source.TrimEnd('/')
}

$ProgressPreference = 'SilentlyContinue'

$missing = @()
$broken = @()
$downloaded = 0

foreach ($file in $manifest.files) {
    $target = Join-Path $Destination $file.name
    $exists = Test-Path $target

    if ($exists) {
        $actual = Get-Sha256 $target
        $isValid = ($actual -eq $file.sha256)

        if ($isValid -and -not $Force) {
            Write-Host ("[ok]    {0}" -f $file.name) -ForegroundColor Green
            continue
        }

        if ($VerifyOnly) {
            if ($isValid) {
                Write-Host ("[ok]    {0}" -f $file.name) -ForegroundColor Green
            }
            else {
                Write-Host ("[bad]   {0}: sha256 {1}" -f $file.name, $actual) -ForegroundColor Red
                $broken += $file.name
            }
            continue
        }

        if ($isValid) {
            Write-Host ("[force] {0}: перекачиваю принудительно" -f $file.name) -ForegroundColor Yellow
        }
        else {
            Write-Host ("[stale] {0}: хеш не совпадает, перекачиваю" -f $file.name) -ForegroundColor Yellow
        }
    }
    elseif ($VerifyOnly) {
        Write-Host ("[miss]  {0}" -f $file.name) -ForegroundColor Yellow
        $missing += $file.name
        continue
    }

    if (Test-Path $target) {
        Remove-Item $target -Force
    }

    $url = "$source/$($file.name)"
    $temp = "$target.download"

    Write-Host ("[get]   {0} <- {1}" -f $file.name, $url)
    Invoke-WebRequest -Uri $url -OutFile $temp -UseBasicParsing

    $actual = Get-Sha256 $temp
    if ($actual -ne $file.sha256) {
        Remove-Item $temp -Force
        throw "Контрольная сумма не совпала для '$($file.name)'. Ожидалось $($file.sha256), получено $actual"
    }

    Move-Item -Path $temp -Destination $target -Force
    Write-Host ("[ok]    {0}" -f $file.name) -ForegroundColor Green
    $downloaded++
}

if ($VerifyOnly) {
    if ($missing.Count -gt 0 -or $broken.Count -gt 0) {
        Write-Host ""
        Write-Host ("Проверка не пройдена. Отсутствуют: {0}; повреждены: {1}" -f `
            ($(if ($missing.Count) { $missing -join ', ' } else { 'нет' })), `
            ($(if ($broken.Count) { $broken -join ', ' } else { 'нет' }))) -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "Все модели на месте, контрольные суммы совпадают." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host ("Готово. Скачано файлов: {0}. Каталог: {1}" -f $downloaded, $Destination) -ForegroundColor Green
exit 0
