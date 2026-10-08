using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PhotoCraft.PcraftThumbnailHandler
{
    internal static class PdfRenderNative
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetDllDirectory(string path);

        [DllImport("poppler-glib.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr poppler_document_new_from_file(
            [MarshalAs(UnmanagedType.LPStr)] string uri,
            [MarshalAs(UnmanagedType.LPStr)] string password,
            IntPtr error);

        [DllImport("poppler-glib.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr poppler_document_get_page(IntPtr document, int index);

        [DllImport("poppler-glib.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void poppler_page_get_size(IntPtr page, out double width, out double height);

        [DllImport("poppler-glib.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void poppler_page_render(IntPtr page, IntPtr cairo);

        [DllImport("gobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void g_object_unref(IntPtr instance);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr cairo_image_surface_create(int format, int width, int height);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr cairo_create(IntPtr surface);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cairo_set_source_rgb(IntPtr cairo, double red, double green, double blue);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cairo_paint(IntPtr cairo);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cairo_scale(IntPtr cairo, double sx, double sy);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cairo_destroy(IntPtr cairo);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cairo_surface_flush(IntPtr surface);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr cairo_image_surface_get_data(IntPtr surface);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cairo_image_surface_get_stride(IntPtr surface);

        [DllImport("cairo.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cairo_surface_destroy(IntPtr surface);
    }

    [ComVisible(true)]
    [Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithStream
    {
        [PreserveSig]
        int Initialize(IStream stream, uint grfMode);
    }

    [ComVisible(true)]
    [Guid("E357FCCD-A995-4576-B01F-234630154E96")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IThumbnailProvider
    {
        [PreserveSig]
        int GetThumbnail(uint cx, out IntPtr phbmp, out WTS_ALPHATYPE pdwAlpha);
    }

    public enum WTS_ALPHATYPE
    {
        WTSAT_RGB = 0,
        WTSAT_ARGB = 1,
        WTSAT_RGBA = 2,
        WTSAT_UNKNOWN = 3
    }

    [ComVisible(true)]
    [Guid("D4E4A682-2E13-4ABF-8E5C-7B4885A85A0B")]
    [ProgId("PhotoCraft.PcraftThumbnailProvider")]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class PcraftThumbnailProvider : IInitializeWithStream, IThumbnailProvider
    {
        private const int S_OK = 0;
        private const int E_FAIL = unchecked((int)0x80004005);
        private const int E_INVALIDARG = unchecked((int)0x80070057);
        private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        private const int MAX_PCRAFT_BYTES = 256 * 1024 * 1024;
        private const int MAX_THUMB_BYTES = 32 * 1024 * 1024;

        private byte[] _pcraftBytes;

        public int Initialize(IStream stream, uint grfMode)
        {
            if (stream == null)
            {
                return E_INVALIDARG;
            }

            try
            {
                _pcraftBytes = ReadComStream(stream, MAX_PCRAFT_BYTES);
                return _pcraftBytes.Length == 0 ? E_FAIL : S_OK;
            }
            catch (OutOfMemoryException)
            {
                return E_OUTOFMEMORY;
            }
            catch
            {
                _pcraftBytes = null;
                return E_FAIL;
            }
        }

        public int GetThumbnail(uint cx, out IntPtr phbmp, out WTS_ALPHATYPE pdwAlpha)
        {
            phbmp = IntPtr.Zero;
            pdwAlpha = WTS_ALPHATYPE.WTSAT_RGB;

            if (_pcraftBytes == null || _pcraftBytes.Length == 0)
            {
                return E_FAIL;
            }

            try
            {
                byte[] png = GetPreviewPng(_pcraftBytes);
                if (png == null || png.Length == 0)
                {
                    return E_FAIL;
                }

                int side = (int)Math.Max(64U, Math.Min(cx == 0 ? 256U : cx, 1024U));
                using (var sourceStream = new MemoryStream(png, false))
                using (var source = Image.FromStream(sourceStream, true, true))
                using (var canvas = new Bitmap(side, side, PixelFormat.Format32bppArgb))
                using (var graphics = Graphics.FromImage(canvas))
                {
                    graphics.Clear(Color.White);
                    graphics.CompositingMode = CompositingMode.SourceOver;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;

                    const int margin = 4;
                    float available = side - (margin * 2);
                    float scale = Math.Min(available / source.Width, available / source.Height);
                    float width = Math.Max(1f, source.Width * scale);
                    float height = Math.Max(1f, source.Height * scale);
                    float left = (side - width) / 2f;
                    float top = (side - height) / 2f;

                    graphics.DrawImage(source, new RectangleF(left, top, width, height));
                    phbmp = canvas.GetHbitmap(Color.White);
                }

                return phbmp == IntPtr.Zero ? E_FAIL : S_OK;
            }
            catch (OutOfMemoryException)
            {
                return E_OUTOFMEMORY;
            }
            catch
            {
                phbmp = IntPtr.Zero;
                return E_FAIL;
            }
        }

        private static byte[] ReadComStream(IStream stream, int maxBytes)
        {
            using (var output = new MemoryStream())
            {
                byte[] buffer = new byte[64 * 1024];
                IntPtr readPtr = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    while (true)
                    {
                        Marshal.WriteInt32(readPtr, 0);
                        stream.Read(buffer, buffer.Length, readPtr);
                        int read = Marshal.ReadInt32(readPtr);
                        if (read <= 0)
                        {
                            break;
                        }

                        if (read > buffer.Length || output.Length + read > maxBytes)
                        {
                            throw new InvalidDataException("The .pcraft stream is too large.");
                        }

                        output.Write(buffer, 0, read);
                        if (read < buffer.Length)
                        {
                            break;
                        }
                    }

                    return output.ToArray();
                }
                finally
                {
                    Marshal.FreeHGlobal(readPtr);
                }
            }
        }

        private static byte[] GetPreviewPng(byte[] documentBytes)
        {
            if (LooksLikeZip(documentBytes))
            {
                try
                {
                    using (var archiveStream = new MemoryStream(documentBytes, false))
                    using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, false))
                    {
                        ZipArchiveEntry entry = FindEntry(archive, "thumb.png") ?? FindEntry(archive, "composite/preview.png");
                        if (entry == null || entry.Length <= 0 || entry.Length > MAX_THUMB_BYTES)
                        {
                            return null;
                        }

                        using (Stream source = entry.Open())
                        using (var output = new MemoryStream((int)entry.Length))
                        {
                            source.CopyTo(output);
                            return output.ToArray();
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }

            if (LooksLikePdf(documentBytes))
            {
                return RenderFirstPdfPage(documentBytes);
            }

            if (LooksLikeVectorCraft(documentBytes))
            {
                return RenderVectorCraft(documentBytes);
            }

            return null;
        }

        private static bool LooksLikeZip(byte[] bytes)
        {
            return bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B;
        }

        private static bool LooksLikePdf(byte[] bytes)
        {
            return bytes.Length >= 5 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46 && bytes[4] == 0x2D;
        }

        private static bool LooksLikeVectorCraft(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return false;
            }

            int length = Math.Min(bytes.Length, 4096);
            string header = Encoding.UTF8.GetString(bytes, 0, length).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            return header.StartsWith("{", StringComparison.Ordinal) &&
                header.IndexOf("\"format\"", StringComparison.OrdinalIgnoreCase) >= 0 &&
                header.IndexOf("vectorcraft", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static byte[] RenderVectorCraft(byte[] vectorCraftBytes)
        {
            string cli = FindVectorCraftCli();
            if (string.IsNullOrEmpty(cli))
            {
                return null;
            }

            string id = "thumbnail-craft-vectorcraft-" + Guid.NewGuid().ToString("N");
            string input = Path.Combine(Path.GetTempPath(), id + ".vectorcraft");
            string output = Path.Combine(Path.GetTempPath(), id + ".png");

            try
            {
                File.WriteAllBytes(input, vectorCraftBytes);
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = cli,
                    Arguments = "convert " + QuoteArgument(input) + " " + QuoteArgument(output),
                    WorkingDirectory = Path.GetDirectoryName(cli),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null || !process.WaitForExit(10000))
                    {
                        try { if (process != null && !process.HasExited) process.Kill(); } catch { }
                        return null;
                    }

                    if (process.ExitCode != 0 || !File.Exists(output))
                    {
                        return null;
                    }
                }

                FileInfo result = new FileInfo(output);
                if (result.Length <= 0 || result.Length > MAX_THUMB_BYTES)
                {
                    return null;
                }

                return File.ReadAllBytes(output);
            }
            catch
            {
                return null;
            }
            finally
            {
                TryDelete(input);
                TryDelete(output);
            }
        }

        private static string FindVectorCraftCli()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string assemblyDirectory = Path.GetDirectoryName(typeof(PcraftThumbnailProvider).Assembly.Location);
            string[] candidates =
            {
                Path.Combine(programFiles, "VectorCraft", "vectorcraft-cli.exe"),
                Path.Combine(programFilesX86, "VectorCraft", "vectorcraft-cli.exe"),
                Path.Combine(assemblyDirectory ?? string.Empty, "vectorcraft-cli.exe")
            };

            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static byte[] RenderFirstPdfPage(byte[] pdfBytes)
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(PcraftThumbnailProvider).Assembly.Location);
            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                return null;
            }

            string popplerDirectory = Path.Combine(assemblyDirectory, "poppler");
            string popplerLibrary = Path.Combine(popplerDirectory, "poppler-glib.dll");
            string cairoLibrary = Path.Combine(popplerDirectory, "cairo.dll");
            if (!File.Exists(popplerLibrary) || !File.Exists(cairoLibrary))
            {
                return null;
            }

            string id = "photocraft-pdf-thumb-" + Guid.NewGuid().ToString("N");
            string input = Path.Combine(Path.GetTempPath(), id + ".pdf");

            try
            {
                File.WriteAllBytes(input, pdfBytes);
                if (!PdfRenderNative.SetDllDirectory(popplerDirectory))
                {
                    return null;
                }

                IntPtr document = PdfRenderNative.poppler_document_new_from_file(new Uri(input).AbsoluteUri, null, IntPtr.Zero);
                if (document == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    IntPtr page = PdfRenderNative.poppler_document_get_page(document, 0);
                    if (page == IntPtr.Zero)
                    {
                        return null;
                    }

                    try
                    {
                        double pageWidth;
                        double pageHeight;
                        PdfRenderNative.poppler_page_get_size(page, out pageWidth, out pageHeight);
                        if (pageWidth <= 0 || pageHeight <= 0 || double.IsNaN(pageWidth) || double.IsNaN(pageHeight))
                        {
                            return null;
                        }

                        double scale = Math.Min(512.0 / pageWidth, 512.0 / pageHeight);
                        int width = Math.Max(1, Math.Min(1024, (int)Math.Ceiling(pageWidth * scale)));
                        int height = Math.Max(1, Math.Min(1024, (int)Math.Ceiling(pageHeight * scale)));
                        IntPtr surface = PdfRenderNative.cairo_image_surface_create(0, width, height);
                        if (surface == IntPtr.Zero)
                        {
                            return null;
                        }

                        try
                        {
                            IntPtr cairo = PdfRenderNative.cairo_create(surface);
                            if (cairo == IntPtr.Zero)
                            {
                                return null;
                            }

                            try
                            {
                                PdfRenderNative.cairo_set_source_rgb(cairo, 1.0, 1.0, 1.0);
                                PdfRenderNative.cairo_paint(cairo);
                                PdfRenderNative.cairo_scale(cairo, scale, scale);
                                PdfRenderNative.poppler_page_render(page, cairo);
                                PdfRenderNative.cairo_surface_flush(surface);

                                IntPtr sourceData = PdfRenderNative.cairo_image_surface_get_data(surface);
                                int sourceStride = PdfRenderNative.cairo_image_surface_get_stride(surface);
                                if (sourceData == IntPtr.Zero || sourceStride < width * 4)
                                {
                                    return null;
                                }

                                using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb))
                                {
                                    BitmapData bitmapData = bitmap.LockBits(
                                        new Rectangle(0, 0, width, height),
                                        ImageLockMode.WriteOnly,
                                        PixelFormat.Format32bppPArgb);
                                    try
                                    {
                                        byte[] row = new byte[width * 4];
                                        for (int y = 0; y < height; y++)
                                        {
                                            Marshal.Copy(IntPtr.Add(sourceData, y * sourceStride), row, 0, row.Length);
                                            Marshal.Copy(row, 0, IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride), row.Length);
                                        }
                                    }
                                    finally
                                    {
                                        bitmap.UnlockBits(bitmapData);
                                    }

                                    using (var output = new MemoryStream())
                                    {
                                        bitmap.Save(output, ImageFormat.Png);
                                        return output.ToArray();
                                    }
                                }
                            }
                            finally
                            {
                                PdfRenderNative.cairo_destroy(cairo);
                            }
                        }
                        finally
                        {
                            PdfRenderNative.cairo_surface_destroy(surface);
                        }
                    }
                    finally
                    {
                        PdfRenderNative.g_object_unref(page);
                    }
                }
                finally
                {
                    PdfRenderNative.g_object_unref(document);
                }
            }
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                TryDelete(input);
                try { PdfRenderNative.SetDllDirectory(null); } catch { }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Thumbnail generation must never make Explorer fail.
            }
        }

        private static ZipArchiveEntry FindEntry(ZipArchive archive, string name)
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.Equals(entry.FullName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
