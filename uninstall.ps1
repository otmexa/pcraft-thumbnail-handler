$ErrorActionPreference = 'Stop'
$clsid = '{D4E4A682-2E13-4ABF-8E5C-7B4885A85A0B}'
$thumbShellEx = '{E357FCCD-A995-4576-B01F-234630154E96}'
$providerClass = 'PhotoCraft.PcraftThumbnailHandler.PcraftThumbnailProvider'

$associationPaths = @(
    "Software\Classes\.pcraft\ShellEx\$thumbShellEx",
    "Software\Classes\SystemFileAssociations\.pcraft\ShellEx\$thumbShellEx",
    "Software\Classes\.vectorcraft\ShellEx\$thumbShellEx",
    "Software\Classes\SystemFileAssociations\.vectorcraft\ShellEx\$thumbShellEx",
    "Software\Classes\vectorcraft_auto_file\ShellEx\$thumbShellEx",
    "Software\Classes\.pdf\ShellEx\$thumbShellEx",
    "Software\Classes\SystemFileAssociations\.pdf\ShellEx\$thumbShellEx",
    "Software\Classes\Acrobat.Document.DC\ShellEx\$thumbShellEx",
    "Software\Classes\PXCEditor.PDF\ShellEx\$thumbShellEx"
)

foreach ($path in $associationPaths) {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($path, $true)
    if ($null -eq $key) { continue }
    try {
        $owned = [string]::Equals([string]$key.GetValue($null), $clsid, [System.StringComparison]::OrdinalIgnoreCase)
    } finally {
        $key.Dispose()
    }
    if ($owned) {
        [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($path, $false)
    }
}

$clsidPath = "Software\Classes\CLSID\$clsid"
$inproc = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey("$clsidPath\InprocServer32", $false)
$registeredClass = $null
if ($null -ne $inproc) {
    try { $registeredClass = [string]$inproc.GetValue('Class') } finally { $inproc.Dispose() }
}
if ([string]::Equals($registeredClass, $providerClass, [System.StringComparison]::OrdinalIgnoreCase)) {
    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($clsidPath, $false)
}

$progIdPath = 'Software\Classes\PhotoCraft.PcraftThumbnailProvider'
$progId = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($progIdPath, $false)
$registeredProgId = $null
if ($null -ne $progId) {
    try { $registeredProgId = [string]$progId.GetValue($null) } finally { $progId.Dispose() }
}
if ([string]::Equals($registeredProgId, $providerClass, [System.StringComparison]::OrdinalIgnoreCase)) {
    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($progIdPath, $false)
}

Write-Output 'The per-user thumbnail handler registration was removed from HKCU.'
Write-Output 'This script does not remove a machine-wide installation made by ThumbnailCraftSetup.exe.'
