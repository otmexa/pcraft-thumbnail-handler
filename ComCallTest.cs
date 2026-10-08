using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

internal static class ComCallNative
{
    [DllImport("ole32.dll")]
    internal static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, out IntPtr instance);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    internal static extern int SHCreateStreamOnFileEx(string path, uint mode, uint attributes, bool create, IStream template, out IStream stream);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);
}

internal static class ComCallTest
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int InitializeDelegate(IntPtr self, IntPtr stream, uint mode);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetThumbnailDelegate(IntPtr self, uint cx, out IntPtr hBitmap, out int alpha);

    private static int Main(string[] args)
    {
        Guid clsid = new Guid("D4E4A682-2E13-4ABF-8E5C-7B4885A85A0B");
        Guid iidUnknown = new Guid("00000000-0000-0000-C000-000000000046");
        Guid iidInitialize = new Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F");
        Guid iidThumbnail = new Guid("E357FCCD-A995-4576-B01F-234630154E96");

        IStream stream;
        int hr = ComCallNative.SHCreateStreamOnFileEx(args[0], 0, 0, false, null, out stream);
        if (hr != 0) throw new InvalidOperationException("Open 0x" + hr.ToString("X8"));

        IntPtr unknown;
        hr = ComCallNative.CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iidUnknown, out unknown);
        if (hr != 0) throw new InvalidOperationException("CoCreate 0x" + hr.ToString("X8"));

        IntPtr initialize;
        hr = Marshal.QueryInterface(unknown, ref iidInitialize, out initialize);
        if (hr != 0) throw new InvalidOperationException("QI Initialize 0x" + hr.ToString("X8"));
        IntPtr thumbnail;
        hr = Marshal.QueryInterface(unknown, ref iidThumbnail, out thumbnail);
        if (hr != 0) throw new InvalidOperationException("QI Thumbnail 0x" + hr.ToString("X8"));

        IntPtr streamUnknown = Marshal.GetIUnknownForObject(stream);
        try
        {
            IntPtr initializeVtable = Marshal.ReadIntPtr(initialize);
            var initializeCall = (InitializeDelegate)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(initializeVtable, IntPtr.Size * 3), typeof(InitializeDelegate));
            hr = initializeCall(initialize, streamUnknown, 0);
            if (hr != 0) throw new InvalidOperationException("Initialize 0x" + hr.ToString("X8"));

            IntPtr thumbnailVtable = Marshal.ReadIntPtr(thumbnail);
            var thumbnailCall = (GetThumbnailDelegate)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(thumbnailVtable, IntPtr.Size * 3), typeof(GetThumbnailDelegate));
            IntPtr hBitmap;
            int alpha;
            hr = thumbnailCall(thumbnail, 256, out hBitmap, out alpha);
            if (hr != 0 || hBitmap == IntPtr.Zero) throw new InvalidOperationException("GetThumbnail 0x" + hr.ToString("X8"));

            using (var image = Image.FromHbitmap(hBitmap))
            {
                image.Save(args[1], ImageFormat.Png);
            }
            ComCallNative.DeleteObject(hBitmap);
            Console.WriteLine("OK " + args[1] + " alpha=" + alpha);
        }
        finally
        {
            Marshal.Release(streamUnknown);
            Marshal.Release(initialize);
            Marshal.Release(thumbnail);
            Marshal.Release(unknown);
        }

        return 0;
    }
}
