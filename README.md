# Thumbnail Craft

English first; Spanish follows below. Thumbnail Craft adds Windows Explorer
thumbnails for PhotoCraft `.pcraft` files, VectorCraft `.vectorcraft` documents,
and PDFs. It reads the embedded PhotoCraft preview (with a composite-preview
fallback), uses VectorCraft's installed CLI renderer for `.vectorcraft`, and
renders the first PDF page with the bundled Poppler runtime. It does not edit
the source documents.

## Download and install

Download `ThumbnailCraftSetup.exe` from the
[latest GitHub release](https://github.com/otmexa/pcraft-thumbnail-handler/releases/latest).
The x64 installer requests administrator permission, installs to
`C:\Program Files\PhotoCraft\ThumbnailHandler`, registers the thumbnail
providers machine-wide, and refreshes Explorer. It takes over the thumbnail
provider registration for these extensions, so use it only on a private or
trusted PC. `.vectorcraft` thumbnails also require VectorCraft installed in a
standard Program Files location (or beside the handler) with
`vectorcraft-cli.exe` available.

## Build from source (Windows x64)

Requirements: 64-bit Windows, Windows PowerShell 5.1 or PowerShell 7, the
64-bit .NET Framework compiler at
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, and the checked-in
`PhotoCraftThumbnailPayload.zip` (which contains the bundled Poppler runtime).
The build recompiles the handler DLL from source and embeds it with Poppler; it
does not download dependencies or rely on a Codex cache.

From the repository folder:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1 -OutputDirectory .\dist -RunSelfTest
```

The `dist` folder contains the x64 installer, SHA-256 checksum, handler DLL,
rebuilt payload ZIP, and icon preview. `/selftest` checks that the installer
contains the required handler and Poppler payload. The installer manifest
requests elevation, so Windows may show a UAC prompt when running the test.

For a per-user developer install (no administrator permission), run
`.\install.ps1`; add `-RestartExplorer` to refresh Explorer automatically.
That script registers under `HKCU`, separately from the machine-wide EXE
installation. Remove the developer registration with `.\uninstall.ps1`.
Remove the machine-wide install with `ThumbnailCraftSetup.exe /uninstall` as
administrator.

## GitHub Actions and releases

- **Windows CI** builds and self-tests on pushes and pull requests to `main`,
  and on manual dispatch. It uploads the installer and checksum as a 30-day
  workflow artifact.
- **Attach Windows installer** runs when a GitHub Release is published. It
  builds the source at that release tag and attaches the EXE and checksum. It
  uses GitHub's built-in `GITHUB_TOKEN` with `contents: write` only for that
  job; no personal access token or repository secret is required.
- Enable Actions in **Settings → Actions → General**. The workflows use
  GitHub-hosted Windows Server 2025 runners.

Use English first and Spanish second in each release description; a bilingual
starter is in `.github/RELEASE_TEMPLATE.md`. Editing an already-published
release does not rerun its published-event workflow; the attachment workflow
will run on the next newly published release.

## Support and distribution

Contact: [benny@zigovo.com](mailto:benny@zigovo.com).

There is currently no `LICENSE` file. A public repository is not automatically
licensed for reuse or redistribution; choose and add a license before inviting
general redistribution. The bundled `PhotoCraftThumbnailPayload.zip` also
contains Poppler binaries but no license, notice, or version/readme files were
found inside the archive. Identify the exact bundled build and include its
applicable third-party notices and any required source materials before broader
redistribution.

---

# Thumbnail Craft (Español)

Thumbnail Craft agrega miniaturas al Explorador de Windows para archivos
PhotoCraft `.pcraft`, documentos VectorCraft `.vectorcraft` y PDF. Lee la vista
previa incrustada de PhotoCraft (con una imagen compuesta como alternativa), usa
el CLI instalado de VectorCraft para `.vectorcraft` y renderiza la primera página
del PDF con Poppler incluido. No modifica los documentos originales.

## Descargar e instalar

Descarga `ThumbnailCraftSetup.exe` desde el
[release más reciente](https://github.com/otmexa/pcraft-thumbnail-handler/releases/latest).
El instalador x64 solicita permisos de administrador, se instala en
`C:\Program Files\PhotoCraft\ThumbnailHandler`, registra los proveedores para
toda la máquina y actualiza el Explorador. Reemplaza el registro del proveedor de
miniaturas para estas extensiones, así que úsalo únicamente en un equipo privado
o de confianza. Para `.vectorcraft` también se requiere VectorCraft instalado en
una ubicación estándar de Program Files (o junto al handler), con
`vectorcraft-cli.exe` disponible.

## Compilar desde el código (Windows x64)

Requisitos: Windows de 64 bits, Windows PowerShell 5.1 o PowerShell 7, el
compilador C# de .NET Framework de 64 bits en
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` y el archivo
`PhotoCraftThumbnailPayload.zip` incluido en el repositorio (contiene Poppler).
La compilación vuelve a crear el DLL desde el código y lo empaqueta con Poppler;
no descarga dependencias ni depende de una caché de Codex.

Desde la carpeta del repositorio:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1 -OutputDirectory .\dist -RunSelfTest
```

La carpeta `dist` contiene el instalador x64, su checksum SHA-256, el DLL del
handler, el ZIP de payload actualizado y la vista previa del icono. `/selftest`
comprueba que el instalador incluya el handler y Poppler. Como el manifiesto del
instalador solicita elevación, Windows podría mostrar una confirmación UAC al
ejecutar la prueba.

Para instalar como desarrollador solo en el usuario actual (sin administrador),
ejecuta `.\install.ps1`; agrega `-RestartExplorer` para actualizar el
Explorador automáticamente. Este script registra el handler en `HKCU`, separado
de la instalación de máquina del EXE. Quita ese registro con
`.\uninstall.ps1`. Para desinstalar la versión instalada para toda la máquina,
ejecuta `ThumbnailCraftSetup.exe /uninstall` como administrador.

## GitHub Actions y releases

- **Windows CI** compila y ejecuta la autoprueba en cada push y pull request a
  `main`, y también permite ejecuciones manuales. Sube el instalador y su
  checksum como artefacto de Actions durante 30 días.
- **Attach Windows installer** se activa al publicar un GitHub Release. Compila
  el código de esa etiqueta y adjunta el EXE y su checksum. Usa el `GITHUB_TOKEN`
  integrado de GitHub con permiso `contents: write` solo para ese trabajo; no
  requiere token personal ni secretos del repositorio.
- Habilita Actions en **Settings → Actions → General**. Los workflows usan
  runners Windows Server 2025 de GitHub.

En cada descripción del release, coloca primero el inglés y después el español.
Hay una plantilla bilingüe en `.github/RELEASE_TEMPLATE.md`. Editar un release ya
publicado no vuelve a ejecutar el workflow; este se activará con la siguiente
publicación nueva.

## Soporte y distribución

Contacto: [benny@zigovo.com](mailto:benny@zigovo.com).

El repositorio todavía no contiene un archivo `LICENSE`. Que sea público no
concede automáticamente permisos de reutilización o redistribución; conviene
elegir y agregar una licencia antes de invitar a redistribuirlo. Además, el ZIP
`PhotoCraftThumbnailPayload.zip` incluye binarios de Poppler, pero no encontré
dentro del archivo una licencia, aviso ni información de versión/README. Antes
de una redistribución más amplia, identifica esa compilación e incluye sus avisos
de terceros y el material fuente que corresponda.
