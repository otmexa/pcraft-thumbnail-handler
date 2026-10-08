using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
internal struct ShellSize
{
    public int cx;
    public int cy;
}

[Flags]
internal enum ShellImageFlags : uint
{
    ResizeToFit = 0,
    BiggerSizeOk = 1,
    ThumbnailOnly = 8
}

[ComImport]
[Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemImageFactory
{
    [PreserveSig]
    int GetImage(ShellSize size, ShellImageFlags flags, out IntPtr hBitmap);
}

internal static class ShellThumbNative
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern int SHCreateItemFromParsingName(
        string path,
        IntPtr bindCtx,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory item);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);
}

internal static class ShellThumbTest
{
    private static int Main(string[] args)
    {
        Guid iid = new Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B");
        for (int i = 0; i < args.Length; i += 2)
        {
            IShellItemImageFactory item;
            int hr = ShellThumbNative.SHCreateItemFromParsingName(args[i], IntPtr.Zero, ref iid, out item);
            if (hr != 0) throw new InvalidOperationException("SHCreateItemFromParsingName 0x" + hr.ToString("X8"));

            IntPtr hBitmap;
            hr = item.GetImage(new ShellSize { cx = 256, cy = 256 }, ShellImageFlags.ResizeToFit | ShellImageFlags.BiggerSizeOk, out hBitmap);
            if (hr != 0 || hBitmap == IntPtr.Zero) throw new InvalidOperationException("GetImage 0x" + hr.ToString("X8"));

            using (var image = Image.FromHbitmap(hBitmap))
            {
                image.Save(args[i + 1], ImageFormat.Png);
            }

            ShellThumbNative.DeleteObject(hBitmap);
            Console.WriteLine(args[i] + " => " + args[i + 1]);
        }

        return 0;
    }
}
