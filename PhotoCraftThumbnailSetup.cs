using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Forms;

internal static class PhotoCraftThumbnailSetup
{
    private const string ProductName = "Thumbnail Craft";
    private const string ContactEmail = "benny@zigovo.com";
    private const string HandlerClsid = "{D4E4A682-2E13-4ABF-8E5C-7B4885A85A0B}";
    private const string ThumbnailShellEx = "{E357FCCD-A995-4576-B01F-234630154E96}";
    private const string AssemblyName = "PhotoCraft.PcraftThumbnailHandler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
    private const string RuntimeVersion = "v4.0.30319";
    private const string PayloadResource = "PhotoCraftThumbnailPayload.zip";

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!Environment.Is64BitOperatingSystem)
                throw new InvalidOperationException("Esta versión requiere Windows de 64 bits.");

            bool uninstall = HasArgument(args, "/uninstall") || HasArgument(args, "-uninstall");
            bool selfTest = HasArgument(args, "/selftest") || HasArgument(args, "-selftest");
            bool refresh = HasArgument(args, "/refresh") || HasArgument(args, "-refresh");
            bool quiet = HasArgument(args, "/quiet") || HasArgument(args, "-quiet");

            if (selfTest)
            {
                VerifyPayload();
                if (!quiet) MessageBox.Show("El paquete de miniaturas está completo.", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            if (refresh)
            {
                NotifyShellAndRestartExplorer();
                return 0;
            }

            if (uninstall)
                Uninstall(quiet);
            else
            {
                if (IsInstalled())
                {
                    if (!quiet)
                    {
                        NotifyShellAndRestartExplorer();
                        ShowAlreadyInstalled();
                    }
                    return 0;
                }

                if (quiet)
                    Install(true);
                else
                    RunInteractiveInstall();
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (!HasArgument(args, "/quiet") && !HasArgument(args, "-quiet"))
                MessageBox.Show(ex.Message, ProductName + " - error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void RunInteractiveInstall()
    {
        using (ThumbnailCraftInstallerForm form = new ThumbnailCraftInstallerForm(ThumbnailCraftInstallerMode.Install))
            form.ShowDialog();
    }

    private static void ShowAlreadyInstalled()
    {
        using (ThumbnailCraftInstallerForm form = new ThumbnailCraftInstallerForm(ThumbnailCraftInstallerMode.AlreadyInstalled))
            form.ShowDialog();
    }

    private static bool IsInstalled()
    {
        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PhotoCraft", "ThumbnailHandler");
        string dllPath = Path.Combine(installDirectory, "PhotoCraft.PcraftThumbnailHandler.dll");
        if (!File.Exists(dllPath)) return false;

        string inprocPath = @"Software\Classes\CLSID\" + HandlerClsid + @"\InprocServer32";
        using (RegistryKey inproc = Registry.LocalMachine.OpenSubKey(inprocPath, false))
        {
            if (inproc == null) return false;
            string codeBase = inproc.GetValue("CodeBase") as string;
            if (string.IsNullOrWhiteSpace(codeBase)) return false;

            try
            {
                string registeredPath = new Uri(codeBase).LocalPath;
                return string.Equals(
                    Path.GetFullPath(registeredPath),
                    Path.GetFullPath(dllPath),
                    StringComparison.OrdinalIgnoreCase) &&
                    IsThumbnailAssociationOwned(@"Software\Classes\.vectorcraft\ShellEx\" + ThumbnailShellEx) &&
                    IsUserThumbnailAssociationOwned(@"Software\Classes\vectorcraft_auto_file\ShellEx\" + ThumbnailShellEx);
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
    }

    private static bool HasArgument(string[] args, string value)
    {
        foreach (string arg in args)
            if (string.Equals(arg, value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static void Install(bool quiet)
    {
        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PhotoCraft", "ThumbnailHandler");
        string tempDirectory = Path.Combine(Path.GetTempPath(), "PhotoCraftThumbnail-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(installDirectory);
        Directory.CreateDirectory(tempDirectory);
        try
        {
            ExtractPayload(tempDirectory);
            CopyDirectoryContents(tempDirectory, installDirectory);

            string dllPath = Path.Combine(installDirectory, "PhotoCraft.PcraftThumbnailHandler.dll");
            if (!File.Exists(dllPath)) throw new FileNotFoundException("Falta el DLL del proveedor de miniaturas.", dllPath);

            RegisterShellExtension(dllPath);
            CopyInstallerForUninstall(installDirectory);
            NotifyShellAndRestartExplorer();

            if (!quiet)
            {
                MessageBox.Show(
                    "¡Thumbnail Craft se instaló correctamente!\n\n" +
                    "Las miniaturas de .pcraft, .vectorcraft y PDF ya están activas.\n" +
                    "El Explorador de Windows fue actualizado.\n\n" +
                    "Contacto: " + ContactEmail,
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static void ExtractPayload(string destination)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream resource = assembly.GetManifestResourceStream(PayloadResource))
        {
            if (resource == null) throw new InvalidOperationException("No se encontró el payload incluido en el instalador.");
            string zipPath = Path.Combine(destination, "payload.zip");
            using (FileStream output = File.Create(zipPath)) resource.CopyTo(output);
            ZipFile.ExtractToDirectory(zipPath, destination);
        }
    }

    private static void VerifyPayload()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream resource = assembly.GetManifestResourceStream(PayloadResource))
        {
            if (resource == null) throw new InvalidOperationException("No se encontró el payload incluido en el instalador.");
            string temp = Path.Combine(Path.GetTempPath(), "PhotoCraftPayloadCheck-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                string zipPath = Path.Combine(temp, "payload.zip");
                using (FileStream output = File.Create(zipPath)) resource.CopyTo(output);
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    bool dll = false;
                    bool poppler = false;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.Equals(entry.FullName, "PhotoCraft.PcraftThumbnailHandler.dll", StringComparison.OrdinalIgnoreCase)) dll = true;
                        if (entry.FullName.StartsWith("poppler/", StringComparison.OrdinalIgnoreCase) || entry.FullName.StartsWith("poppler\\", StringComparison.OrdinalIgnoreCase)) poppler = true;
                    }
                    if (!dll || !poppler) throw new InvalidOperationException("El payload no contiene todos los componentes requeridos.");
                }
            }
            finally { TryDeleteDirectory(temp); }
        }
    }

    private static void RegisterShellExtension(string dllPath)
    {
        string codeBase = new Uri(dllPath).AbsoluteUri;
        string clsid = @"Software\Classes\CLSID\" + HandlerClsid;
        using (RegistryKey clsidKey = Registry.LocalMachine.CreateSubKey(clsid))
        {
            SetDefault(clsidKey, "PhotoCraft Thumbnail Provider");
            clsidKey.SetValue("DisableProcessIsolation", 1, RegistryValueKind.DWord);
            using (RegistryKey inproc = clsidKey.CreateSubKey("InprocServer32"))
            {
                SetDefault(inproc, "mscoree.dll");
                inproc.SetValue("ThreadingModel", "Both");
                inproc.SetValue("Class", "PhotoCraft.PcraftThumbnailHandler.PcraftThumbnailProvider");
                inproc.SetValue("Assembly", AssemblyName);
                inproc.SetValue("RuntimeVersion", RuntimeVersion);
                inproc.SetValue("CodeBase", codeBase);
                using (RegistryKey version = inproc.CreateSubKey("1.0.0.0"))
                {
                    version.SetValue("Class", "PhotoCraft.PcraftThumbnailHandler.PcraftThumbnailProvider");
                    version.SetValue("Assembly", AssemblyName);
                    version.SetValue("RuntimeVersion", RuntimeVersion);
                    version.SetValue("CodeBase", codeBase);
                }
            }
            using (RegistryKey category = clsidKey.CreateSubKey(@"Implemented Categories\{62C8FE65-4EBB-45E7-B440-6E39B2CDBF29}")) { }
        }

        using (RegistryKey progId = Registry.LocalMachine.CreateSubKey(@"Software\Classes\PhotoCraft.Pcraft"))
        {
            SetDefault(progId, "PhotoCraft document");
            progId.SetValue("ThumbnailCutoff", 0, RegistryValueKind.DWord);
            using (RegistryKey progClsid = progId.CreateSubKey("CLSID")) SetDefault(progClsid, HandlerClsid);
            using (RegistryKey shellEx = progId.CreateSubKey(@"ShellEx\" + ThumbnailShellEx)) SetDefault(shellEx, HandlerClsid);

            string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PhotoCraft", "photocraft.exe");
            if (File.Exists(appPath))
            {
                using (RegistryKey icon = progId.CreateSubKey("DefaultIcon")) SetDefault(icon, appPath + ",0");
                using (RegistryKey open = progId.CreateSubKey(@"shell\open\command")) SetDefault(open, "\"" + appPath + "\" \"%1\"");
            }
        }

        RegisterThumbnailAssociation(@"Software\Classes\.pcraft\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\SystemFileAssociations\.pcraft\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\.vectorcraft\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\SystemFileAssociations\.vectorcraft\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\vectorcraft_auto_file\ShellEx\" + ThumbnailShellEx);
        RegisterUserThumbnailAssociation(@"Software\Classes\.vectorcraft\ShellEx\" + ThumbnailShellEx);
        RegisterUserThumbnailAssociation(@"Software\Classes\SystemFileAssociations\.vectorcraft\ShellEx\" + ThumbnailShellEx);
        RegisterUserThumbnailAssociation(@"Software\Classes\vectorcraft_auto_file\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\.pdf\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\SystemFileAssociations\.pdf\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\Acrobat.Document.DC\ShellEx\" + ThumbnailShellEx);
        RegisterThumbnailAssociation(@"Software\Classes\PXCEditor.PDF\ShellEx\" + ThumbnailShellEx);

        using (RegistryKey extension = Registry.LocalMachine.CreateSubKey(@"Software\Classes\.pcraft"))
            SetDefault(extension, "PhotoCraft.Pcraft");
    }

    private static void RegisterThumbnailAssociation(string path)
    {
        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(path)) SetDefault(key, HandlerClsid);
    }

    private static bool IsThumbnailAssociationOwned(string path)
    {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path, false))
        {
            return key != null && string.Equals(key.GetValue(null) as string, HandlerClsid, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool IsUserThumbnailAssociationOwned(string path)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path, false))
        {
            return key != null && string.Equals(key.GetValue(null) as string, HandlerClsid, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void RegisterUserThumbnailAssociation(string path)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(path)) SetDefault(key, HandlerClsid);
    }

    private static void Uninstall(bool quiet)
    {
        string[] associationPaths =
        {
            @"Software\Classes\.pcraft\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\SystemFileAssociations\.pcraft\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\.vectorcraft\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\SystemFileAssociations\.vectorcraft\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\vectorcraft_auto_file\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\.pdf\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\SystemFileAssociations\.pdf\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\Acrobat.Document.DC\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\PXCEditor.PDF\ShellEx\" + ThumbnailShellEx
        };

        foreach (string path in associationPaths) RemoveIfOwned(path);
        foreach (string path in new[]
        {
            @"Software\Classes\.vectorcraft\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\SystemFileAssociations\.vectorcraft\ShellEx\" + ThumbnailShellEx,
            @"Software\Classes\vectorcraft_auto_file\ShellEx\" + ThumbnailShellEx
        }) RemoveIfOwned(Registry.CurrentUser, path);
        Registry.LocalMachine.DeleteSubKeyTree(@"Software\Classes\CLSID\" + HandlerClsid, false);
        Registry.LocalMachine.DeleteSubKeyTree(@"Software\Classes\PhotoCraft.Pcraft", false);

        using (RegistryKey extension = Registry.LocalMachine.OpenSubKey(@"Software\Classes\.pcraft", true))
        {
            if (extension != null && string.Equals(extension.GetValue(null) as string, "PhotoCraft.Pcraft", StringComparison.OrdinalIgnoreCase))
                extension.DeleteValue(null, false);
        }

        NotifyShellAndRestartExplorer();
        if (!quiet) MessageBox.Show("Thumbnail Craft fue desinstalado.", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void RemoveIfOwned(string path)
    {
        RemoveIfOwned(Registry.LocalMachine, path);
    }

    private static void RemoveIfOwned(RegistryKey root, string path)
    {
        using (RegistryKey key = root.OpenSubKey(path, true))
        {
            if (key != null && string.Equals(key.GetValue(null) as string, HandlerClsid, StringComparison.OrdinalIgnoreCase))
            {
                string parent = path.Substring(0, path.LastIndexOf('\\'));
                string name = path.Substring(path.LastIndexOf('\\') + 1);
                using (RegistryKey parentKey = root.OpenSubKey(parent, true))
                {
                    if (parentKey != null) parentKey.DeleteSubKeyTree(name, false);
                }
            }
        }
    }

    private static void CopyInstallerForUninstall(string installDirectory)
    {
        string source = Assembly.GetExecutingAssembly().Location;
        string destination = Path.Combine(installDirectory, "ThumbnailCraftSetup.exe");
        if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            File.Copy(source, destination, true);
    }

    private static void CopyDirectoryContents(string source, string destination)
    {
        foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(directory.Replace(source, destination));
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string target = Path.Combine(destination, relative);
            string targetDirectory = Path.GetDirectoryName(target);
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
            if (!string.Equals(Path.GetFileName(file), "payload.zip", StringComparison.OrdinalIgnoreCase)) File.Copy(file, target, true);
        }
    }

    private static void NotifyShellAndRestartExplorer()
    {
        foreach (Process process in Process.GetProcessesByName("explorer"))
        {
            try { process.Kill(); } catch { }
        }
        System.Threading.Thread.Sleep(700);
        ClearExplorerThumbnailCache();
        Process.Start("explorer.exe");
        System.Threading.Thread.Sleep(700);
        SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    }

    private static void ClearExplorerThumbnailCache()
    {
        string cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Explorer");
        if (!Directory.Exists(cacheDirectory)) return;

        foreach (string pattern in new[] { "thumbcache_*.db", "iconcache_*.db" })
        {
            foreach (string file in Directory.GetFiles(cacheDirectory, pattern, SearchOption.TopDirectoryOnly))
            {
                try { File.Delete(file); } catch { }
            }
        }
    }

    private static void SetDefault(RegistryKey key, string value)
    {
        if (key != null) key.SetValue(null, value, RegistryValueKind.String);
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }
}
