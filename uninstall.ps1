$ErrorActionPreference = 'Stop'
$clsid = '{D4E4A682-2E13-4ABF-8E5C-7B4885A85A0B}'
$thumbShellEx = '{E357FCCD-A995-4576-B01F-234630154E96}'
$clsidKey = "HKCU:\Software\Classes\CLSID\$clsid"
$progIdRoot = 'HKCU:\Software\Classes\PhotoCraft.PcraftThumbnailProvider'
$extensionKey = "HKCU:\Software\Classes\.pcraft\ShellEx\$thumbShellEx"
$pcraftSystemAssociationKey = "HKCU:\Software\Classes\SystemFileAssociations\.pcraft\ShellEx\$thumbShellEx"
$vectorCraftExtensionKey = "HKCU:\Software\Classes\.vectorcraft\ShellEx\$thumbShellEx"
$vectorCraftSystemAssociationKey = "HKCU:\Software\Classes\SystemFileAssociations\.vectorcraft\ShellEx\$thumbShellEx"
$pdfExtensionKey = "HKCU:\Software\Classes\.pdf\ShellEx\$thumbShellEx"
$pdfSystemAssociationKey = "HKCU:\Software\Classes\SystemFileAssociations\.pdf\ShellEx\$thumbShellEx"
$pdfAcrobatProgIdKey = "HKCU:\Software\Classes\Acrobat.Document.DC\ShellEx\$thumbShellEx"
$pdfXChangeProgIdKey = "HKCU:\Software\Classes\PXCEditor.PDF\ShellEx\$thumbShellEx"

if (Test-Path -LiteralPath $extensionKey) {
    $value = (Get-ItemProperty -LiteralPath $extensionKey -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
    if ($value -eq $clsid) {
        Remove-Item -LiteralPath $extensionKey -Force
    }
}

if (Test-Path -LiteralPath $pdfExtensionKey) {
    $value = (Get-ItemProperty -LiteralPath $pdfExtensionKey -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
    if ($value -eq $clsid) {
        Remove-Item -LiteralPath $pdfExtensionKey -Force
    }
}

if (Test-Path -LiteralPath $pcraftSystemAssociationKey) {
    $value = (Get-ItemProperty -LiteralPath $pcraftSystemAssociationKey -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
    if ($value -eq $clsid) {
        Remove-Item -LiteralPath $pcraftSystemAssociationKey -Force
    }
}

foreach ($key in @($vectorCraftExtensionKey, $vectorCraftSystemAssociationKey)) {
    if (Test-Path -LiteralPath $key) {
        $value = (Get-ItemProperty -LiteralPath $key -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
        if ($value -eq $clsid) {
            Remove-Item -LiteralPath $key -Force
        }
    }
}

foreach ($key in @($pdfSystemAssociationKey, $pdfAcrobatProgIdKey, $pdfXChangeProgIdKey)) {
    if (Test-Path -LiteralPath $key) {
        $value = (Get-ItemProperty -LiteralPath $key -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
        if ($value -eq $clsid) {
            Remove-Item -LiteralPath $key -Force
        }
    }
}

if (Test-Path -LiteralPath $clsidKey) {
    Remove-Item -LiteralPath $clsidKey -Recurse -Force
}

if (Test-Path -LiteralPath $progIdRoot) {
    Remove-Item -LiteralPath $progIdRoot -Recurse -Force
}

Write-Output 'Handler de miniaturas de PhotoCraft desinstalado de HKCU.'
