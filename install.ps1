param(
    [switch]$RestartExplorer
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $root 'PcraftThumbnailProvider.cs'
$assemblyInfo = Join-Path $root 'AssemblyInfo.cs'
$bin = Join-Path $root 'bin'
$dll = Join-Path $bin 'PhotoCraft.PcraftThumbnailHandler.dll'
$popplerSource = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\native\poppler\Library\bin'
$popplerTarget = Join-Path $bin 'poppler'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$assemblyVersion = '1.0.0.0'

if (-not (Test-Path -LiteralPath $csc)) {
    throw "No se encontró el compilador .NET Framework de 64 bits: $csc"
}

New-Item -ItemType Directory -Force -Path $bin | Out-Null
& $csc /nologo /target:library /platform:x64 /optimize+ /debug- `
    /out:$dll `
    /reference:System.dll,System.Core.dll,System.Drawing.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll `
    $source, $assemblyInfo

if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $dll)) {
    throw 'No se pudo compilar el handler de miniaturas.'
}

if (-not (Test-Path -LiteralPath (Join-Path $popplerSource 'pdftoppm.exe'))) {
    throw "No se encontró pdftoppm.exe en el runtime local: $popplerSource"
}

New-Item -ItemType Directory -Force -Path $popplerTarget | Out-Null
Copy-Item -Path (Join-Path $popplerSource '*') -Destination $popplerTarget -Force

$clsid = '{D4E4A682-2E13-4ABF-8E5C-7B4885A85A0B}'
$thumbShellEx = '{E357FCCD-A995-4576-B01F-234630154E96}'
$assemblyName = "PhotoCraft.PcraftThumbnailHandler, Version=$assemblyVersion, Culture=neutral, PublicKeyToken=null"
$runtimeVersion = 'v4.0.30319'
$codeBase = ([Uri]$dll).AbsoluteUri

# Per-user COM registration avoids requiring administrator rights.
$clsidKey = "HKCU:\Software\Classes\CLSID\$clsid"
$inprocKey = Join-Path $clsidKey 'InprocServer32'
$versionKey = Join-Path $inprocKey $assemblyVersion
$progIdKey = Join-Path $clsidKey 'ProgID'
$progIdRoot = 'HKCU:\Software\Classes\PhotoCraft.PcraftThumbnailProvider'
$progIdClsid = Join-Path $progIdRoot 'CLSID'
$categoryKey = Join-Path $clsidKey 'Implemented Categories\{62C8FE65-4EBB-45E7-B440-6E39B2CDBF29}'
$extensionKey = "HKCU:\Software\Classes\.pcraft\ShellEx\$thumbShellEx"
$pcraftSystemAssociationKey = "HKCU:\Software\Classes\SystemFileAssociations\.pcraft\ShellEx\$thumbShellEx"
$vectorCraftExtensionKey = "HKCU:\Software\Classes\.vectorcraft\ShellEx\$thumbShellEx"
$vectorCraftSystemAssociationKey = "HKCU:\Software\Classes\SystemFileAssociations\.vectorcraft\ShellEx\$thumbShellEx"
$vectorCraftProgIdKey = "HKCU:\Software\Classes\vectorcraft_auto_file\ShellEx\$thumbShellEx"
$pdfExtensionKey = "HKCU:\Software\Classes\.pdf\ShellEx\$thumbShellEx"
$pdfSystemAssociationKey = "HKCU:\Software\Classes\SystemFileAssociations\.pdf\ShellEx\$thumbShellEx"
$pdfAcrobatProgIdKey = "HKCU:\Software\Classes\Acrobat.Document.DC\ShellEx\$thumbShellEx"
$pdfXChangeProgIdKey = "HKCU:\Software\Classes\PXCEditor.PDF\ShellEx\$thumbShellEx"

New-Item -ItemType Directory -Force -Path $clsidKey, $inprocKey, $versionKey, $progIdKey, $progIdRoot, $progIdClsid, $categoryKey, $extensionKey, $pcraftSystemAssociationKey, $vectorCraftExtensionKey, $vectorCraftSystemAssociationKey, $vectorCraftProgIdKey, $pdfExtensionKey, $pdfSystemAssociationKey, $pdfAcrobatProgIdKey, $pdfXChangeProgIdKey | Out-Null
Set-ItemProperty -LiteralPath $clsidKey -Name '(default)' -Value 'PhotoCraft .pcraft Thumbnail Provider'
Set-ItemProperty -LiteralPath $clsidKey -Name 'DisableProcessIsolation' -Value 1 -Type DWord
Set-ItemProperty -LiteralPath $inprocKey -Name '(default)' -Value 'mscoree.dll'
Set-ItemProperty -LiteralPath $inprocKey -Name 'ThreadingModel' -Value 'Both'
Set-ItemProperty -LiteralPath $inprocKey -Name 'Class' -Value 'PhotoCraft.PcraftThumbnailHandler.PcraftThumbnailProvider'
Set-ItemProperty -LiteralPath $inprocKey -Name 'Assembly' -Value $assemblyName
Set-ItemProperty -LiteralPath $inprocKey -Name 'RuntimeVersion' -Value $runtimeVersion
Set-ItemProperty -LiteralPath $inprocKey -Name 'CodeBase' -Value $codeBase
Set-ItemProperty -LiteralPath $versionKey -Name 'Class' -Value 'PhotoCraft.PcraftThumbnailHandler.PcraftThumbnailProvider'
Set-ItemProperty -LiteralPath $versionKey -Name 'Assembly' -Value $assemblyName
Set-ItemProperty -LiteralPath $versionKey -Name 'RuntimeVersion' -Value $runtimeVersion
Set-ItemProperty -LiteralPath $versionKey -Name 'CodeBase' -Value $codeBase
Set-ItemProperty -LiteralPath $progIdKey -Name '(default)' -Value 'PhotoCraft.PcraftThumbnailProvider'
Set-ItemProperty -LiteralPath $progIdRoot -Name '(default)' -Value 'PhotoCraft.PcraftThumbnailHandler.PcraftThumbnailProvider'
Set-ItemProperty -LiteralPath $progIdClsid -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $extensionKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $pcraftSystemAssociationKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $vectorCraftExtensionKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $vectorCraftSystemAssociationKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $vectorCraftProgIdKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $pdfExtensionKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $pdfSystemAssociationKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $pdfAcrobatProgIdKey -Name '(default)' -Value $clsid
Set-ItemProperty -LiteralPath $pdfXChangeProgIdKey -Name '(default)' -Value $clsid

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class PhotoCraftShellNotify {
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
}
'@
[PhotoCraftShellNotify]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)

Write-Output "Compilado: $dll"
Write-Output "Registrado para .pcraft y .vectorcraft en HKCU (sin permisos de administrador)."
Write-Output "Registrado también para .pdf usando Poppler local en proceso para renderizar la primera página."

if ($RestartExplorer) {
    Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
    Start-Process explorer.exe
    Write-Output 'Explorer reiniciado.'
}
