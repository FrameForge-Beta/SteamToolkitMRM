$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$portableDirectory = Join-Path $projectRoot "WindowsApp\Portatil"
$installerDirectory = Join-Path $projectRoot "WindowsApp\Instalador"
$payloadDirectory = Join-Path $projectRoot "native\obj\InstallerPayload"
$installerScript = Join-Path $projectRoot "installer\SteamToolkitMRM.iss"
$ludusaviVersion = "0.31.0"
$ludusaviArchiveUrl = "https://github.com/mtkennerly/ludusavi/releases/download/v$ludusaviVersion/ludusavi-v$ludusaviVersion-win64.zip"
$ludusaviLegalUrl = "https://github.com/mtkennerly/ludusavi/releases/download/v$ludusaviVersion/ludusavi-v$ludusaviVersion-legal.zip"
$ludusaviArchiveSha256 = "F47A8AD8C708F01D2EB124704973BEFFAB205E292F5287A10FC4A101F8D68706"
$ludusaviLegalSha256 = "9FCE11FC44942EFDD5969928D89B106C229F9A041D7A713C424A6DA981660EBF"
$buildDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "SteamToolkitMRM-$([guid]::NewGuid().ToString('N'))"
$ludusaviDirectory = Join-Path $buildDirectory "ludusavi"
$ludusaviArchive = Join-Path $buildDirectory "ludusavi.zip"
$ludusaviLegalArchive = Join-Path $buildDirectory "ludusavi-legal.zip"
$ludusaviExtractDirectory = Join-Path $buildDirectory "ludusavi-extract"
$ludusaviLegalExtractDirectory = Join-Path $buildDirectory "ludusavi-legal-extract"

$compilerCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($null -ne $compilerCommand) {
    $installerCompiler = $compilerCommand.Source
}
else {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    $installerCompiler = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($installerCompiler)) {
    throw "Inno Setup 6 não foi encontrado. Instala-o para gerar o instalador."
}

New-Item -ItemType Directory -Force -Path $portableDirectory, $installerDirectory, $buildDirectory | Out-Null
if (Test-Path $payloadDirectory) {
    Remove-Item -LiteralPath $payloadDirectory -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $payloadDirectory | Out-Null

try {
    $ProgressPreference = "SilentlyContinue"
    Invoke-WebRequest -Uri $ludusaviArchiveUrl -OutFile $ludusaviArchive
    Invoke-WebRequest -Uri $ludusaviLegalUrl -OutFile $ludusaviLegalArchive

    $actualArchiveHash = (Get-FileHash -LiteralPath $ludusaviArchive -Algorithm SHA256).Hash
    if ($actualArchiveHash -ne $ludusaviArchiveSha256) {
        throw "O SHA-256 do arquivo do Ludusavi não corresponde ao valor esperado."
    }
    $actualLegalHash = (Get-FileHash -LiteralPath $ludusaviLegalArchive -Algorithm SHA256).Hash
    if ($actualLegalHash -ne $ludusaviLegalSha256) {
        throw "O SHA-256 do arquivo de licenças do Ludusavi não corresponde ao valor esperado."
    }

    Expand-Archive -LiteralPath $ludusaviArchive -DestinationPath $ludusaviExtractDirectory
    Expand-Archive -LiteralPath $ludusaviLegalArchive -DestinationPath $ludusaviLegalExtractDirectory
    $ludusaviExecutable = Join-Path $ludusaviExtractDirectory "ludusavi.exe"
    $ludusaviLegalFile = Join-Path $ludusaviLegalExtractDirectory "ludusavi-v$ludusaviVersion-legal.txt"
    if (-not (Test-Path -LiteralPath $ludusaviExecutable) -or
        -not (Test-Path -LiteralPath $ludusaviLegalFile)) {
        throw "Os arquivos esperados não foram encontrados nos arquivos oficiais do Ludusavi."
    }
    New-Item -ItemType Directory -Force -Path $ludusaviDirectory | Out-Null
    Copy-Item -LiteralPath $ludusaviExecutable -Destination $ludusaviDirectory
    Copy-Item -LiteralPath $ludusaviLegalFile `
        -Destination (Join-Path $ludusaviDirectory "THIRD-PARTY-LICENSES.txt")

    dotnet publish (Join-Path $projectRoot "native\SteamToolkitMRM.csproj") `
        -c Release -r win-x64 --self-contained true `
        -p:RestoreSources=https://api.nuget.org/v3/index.json `
        -o $portableDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "A publicação da versão portátil falhou."
    }

    Copy-Item -Path (Join-Path $portableDirectory "*") -Destination $payloadDirectory -Recurse -Force
    $portableLudusaviDirectory = Join-Path $portableDirectory "ludusavi"
    if (Test-Path -LiteralPath $portableLudusaviDirectory) {
        Remove-Item -LiteralPath $portableLudusaviDirectory -Recurse -Force
    }
    Copy-Item -LiteralPath $ludusaviDirectory -Destination $portableDirectory -Recurse -Force
    Copy-Item -LiteralPath $ludusaviDirectory -Destination $payloadDirectory -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot "THIRD-PARTY-NOTICES.md") `
        -Destination (Join-Path $portableDirectory "THIRD-PARTY-NOTICES.md")
    Copy-Item -LiteralPath (Join-Path $projectRoot "THIRD-PARTY-NOTICES.md") `
        -Destination (Join-Path $payloadDirectory "THIRD-PARTY-NOTICES.md")
    Set-Content -LiteralPath (Join-Path $payloadDirectory "steamtoolkit-installed.flag") `
        -Value "1" -NoNewline -Encoding ASCII

    & $installerCompiler "/Qp" $installerScript
    if ($LASTEXITCODE -ne 0) {
        throw "A compilação do instalador falhou."
    }

    Write-Output "Versão portátil para testes: $portableDirectory"
    Write-Output "Instalador principal: $(Join-Path $installerDirectory 'SteamToolkitMRM-Setup.exe')"
}
finally {
    if (Test-Path $payloadDirectory) {
        Remove-Item -LiteralPath $payloadDirectory -Recurse -Force
    }
    if (Test-Path $buildDirectory) {
        Remove-Item -LiteralPath $buildDirectory -Recurse -Force
    }
}
