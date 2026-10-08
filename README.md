# Thumbnail Craft document thumbnail handler

This is a small Windows Explorer thumbnail provider for PhotoCraft native files,
VectorCraft documents, and PDFs. For `.pcraft`, it reads the embedded `thumb.png`
and falls back to `composite/preview.png`. For `.vectorcraft`, it uses the
installed `VectorCraft\vectorcraft-cli.exe` to render a temporary PNG preview.
For `.pdf`, it renders only the first page through the local Poppler GLib/Cairo
libraries in-process. It does not start PhotoCraft and it does not modify
document contents.

## One-file installer

`ThumbnailCraftSetup.exe` is the private, x64 installer. Its manifest asks
Windows for administrator elevation, extracts the handler and the bundled
Poppler runtime under `C:\Program Files\PhotoCraft\ThumbnailHandler`, registers
the COM provider machine-wide, refreshes Explorer, and supports:

```text
ThumbnailCraftSetup.exe /uninstall
ThumbnailCraftSetup.exe /selftest
```

The interactive installer uses a branded window with the Thumbnail Craft icon,
asks for confirmation before making changes, reports completion when Explorer
has been refreshed, and shows `benny@zigovo.com` as the private support contact.
If the provider is already registered and points to the installed DLL, the
window reports that it is already installed and exits without reinstalling.

The installer is intended for trusted private machines. It replaces the
thumbnail provider registration for `.pcraft`, `.vectorcraft`, and `.pdf` on
that computer.

## Install

From PowerShell:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\install.ps1 -RestartExplorer
```

The installer compiles a 64-bit .NET Framework COM DLL, copies the local Poppler
runtime libraries, and registers `.pcraft`, `.vectorcraft`, and `.pdf` per-user under
`HKCU\Software\Classes`; administrator rights are not required.

After installation, use Large or Extra large icons in Explorer. The handler can
be removed with:

```powershell
.\uninstall.ps1
```

The provider intentionally has conservative limits when reading an archive and
only opens the two preview entries written by PhotoCraft. VectorCraft previews
require VectorCraft to be installed so its CLI renderer is available.
