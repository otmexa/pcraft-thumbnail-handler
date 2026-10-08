param(
    [string]$OutputDirectory = 'dist',
    [switch]$RunSelfTest
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
} else {
    $outputRoot = [System.IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
}

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$payloadArchive = Join-Path $root 'PhotoCraftThumbnailPayload.zip'
$providerSource = Join-Path $root 'PcraftThumbnailProvider.cs'
$assemblyInfo = Join-Path $root 'AssemblyInfo.cs'
$setupSource = Join-Path $root 'PhotoCraftThumbnailSetup.cs'
$formSource = Join-Path $root 'ThumbnailCraftInstallerForm.cs'
$manifest = Join-Path $root 'PhotoCraftThumbnailSetup.manifest'
$iconPath = Join-Path $root 'ThumbnailCraftIcon.ico'

foreach ($requiredFile in @($csc, $payloadArchive, $providerSource, $assemblyInfo, $setupSource, $formSource, $manifest, $iconPath)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required build input not found: $requiredFile"
    }
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('ThumbnailCraft-Build-' + [Guid]::NewGuid().ToString('N'))
$sourcePayloadRoot = Join-Path $tempRoot 'source-payload'
$payloadRoot = Join-Path $tempRoot 'payload'
$builtPayload = Join-Path $tempRoot 'PhotoCraftThumbnailPayload.zip'
$previewImage = Join-Path $tempRoot 'ThumbnailCraftIcon-preview.png'
$builtSetup = Join-Path $tempRoot 'ThumbnailCraftSetup.exe'

try {
    New-Item -ItemType Directory -Force -Path $sourcePayloadRoot, $payloadRoot | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($payloadArchive, $sourcePayloadRoot)

    $popplerSource = Join-Path $sourcePayloadRoot 'poppler'
    if (-not (Test-Path -LiteralPath (Join-Path $popplerSource 'pdftoppm.exe') -PathType Leaf)) {
        throw 'The bundled runtime is incomplete: poppler/pdftoppm.exe is missing from PhotoCraftThumbnailPayload.zip.'
    }

    Copy-Item -LiteralPath $popplerSource -Destination $payloadRoot -Recurse -Force

    $providerDll = Join-Path $payloadRoot 'PhotoCraft.PcraftThumbnailHandler.dll'
    $providerArguments = @(
        '/nologo',
        '/target:library',
        '/platform:x64',
        '/optimize+',
        '/debug-',
        "/out:$providerDll",
        '/reference:System.dll,System.Core.dll,System.Drawing.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll',
        $providerSource,
        $assemblyInfo
    )
    & $csc @providerArguments
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $providerDll -PathType Leaf)) {
        throw 'Could not compile PhotoCraft.PcraftThumbnailHandler.dll.'
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $payloadRoot,
        $builtPayload,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false
    )

    $icon = [System.Drawing.Icon]::new($iconPath)
    try {
        $bitmap = $icon.ToBitmap()
        try {
            $bitmap.Save($previewImage, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $bitmap.Dispose()
        }
    } finally {
        $icon.Dispose()
    }

    $setupArguments = @(
        '/nologo',
        '/target:winexe',
        '/platform:x64',
        '/optimize+',
        '/debug-',
        "/out:$builtSetup",
        "/win32icon:$iconPath",
        "/win32manifest:$manifest",
        "/resource:$builtPayload,PhotoCraftThumbnailPayload.zip",
        "/resource:$previewImage,ThumbnailCraftIcon-preview.png",
        '/reference:System.dll,System.Core.dll,System.Drawing.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll,System.Windows.Forms.dll',
        $setupSource,
        $formSource,
        $assemblyInfo
    )
    & $csc @setupArguments
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $builtSetup -PathType Leaf)) {
        throw 'Could not compile ThumbnailCraftSetup.exe.'
    }

    $setupPath = Join-Path $outputRoot 'ThumbnailCraftSetup.exe'
    Copy-Item -LiteralPath $providerDll -Destination (Join-Path $outputRoot 'PhotoCraft.PcraftThumbnailHandler.dll') -Force
    Copy-Item -LiteralPath $builtPayload -Destination (Join-Path $outputRoot 'PhotoCraftThumbnailPayload.zip') -Force
    Copy-Item -LiteralPath $previewImage -Destination (Join-Path $outputRoot 'ThumbnailCraftIcon-preview.png') -Force
    Copy-Item -LiteralPath $builtSetup -Destination $setupPath -Force

    $sha256 = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath ($setupPath + '.sha256') -Value ($sha256 + '  ThumbnailCraftSetup.exe') -Encoding ASCII

    if ($RunSelfTest) {
        $selfTest = Start-Process -FilePath $setupPath -ArgumentList @('/selftest', '/quiet') -Wait -PassThru -NoNewWindow
        if ($selfTest.ExitCode -ne 0) {
            throw "Installer self-test failed with exit code $($selfTest.ExitCode)."
        }
    }

    Write-Output "Installer: $setupPath"
    Write-Output "SHA-256: $sha256"
} finally {
    $tempPath = [System.IO.Path]::GetFullPath($tempRoot)
    $tempPrefix = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $tempLeaf = Split-Path -Leaf $tempPath
    if ($tempPath.StartsWith($tempPrefix, [System.StringComparison]::OrdinalIgnoreCase) -and
        $tempLeaf.StartsWith('ThumbnailCraft-Build-', [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $tempPath -PathType Container)) {
        Remove-Item -LiteralPath $tempPath -Recurse -Force
    }
}
