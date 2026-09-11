using System.Runtime.InteropServices;
using System.Text;

namespace PdfXmpReader;

internal static class QpdfNativeApi
{
    private const string LibraryName = "qpdf";
    private const int CustomLogger = 4;

    public static void ExecuteJob(string json)
    {
        var errors = new StringBuilder();
        var callbackHandle = GCHandle.Alloc(errors);
        var logger = IntPtr.Zero;
        var job = IntPtr.Zero;
        try
        {
            logger = Native.qpdflogger_create();
            if (logger == IntPtr.Zero)
                throw new PdfNativeException("qpdf could not create a logger.");

            Native.qpdflogger_set_error(logger, CustomLogger, LogCallbackPointer, GCHandle.ToIntPtr(callbackHandle));
            job = Native.qpdfjob_init();
            if (job == IntPtr.Zero)
                throw new PdfNativeException("qpdf could not initialize a job.");
            Native.qpdfjob_set_logger(job, logger);

            var initializeResult = Native.qpdfjob_initialize_from_json(job, json);
            if (initializeResult != 0)
                throw CreateException("qpdf could not initialize the JSON job", errors, initializeResult);

            var result = Native.qpdfjob_run(job);
            if (result != 0)
                throw CreateException("qpdf could not read the PDF", errors, result);
        }
        finally
        {
            if (job != IntPtr.Zero)
                Native.qpdfjob_cleanup(ref job);
            if (logger != IntPtr.Zero)
                Native.qpdflogger_cleanup(ref logger);
            if (callbackHandle.IsAllocated)
                callbackHandle.Free();
        }
    }

    private static PdfNativeException CreateException(string message, StringBuilder errors, int code) =>
        new($"{message} (qpdf exit code {code}).{Environment.NewLine}{errors}");

    private static int LogCallback(IntPtr data, nuint length, IntPtr userData)
    {
        if (length > int.MaxValue)
            return 1;
        var bytes = new byte[(int)length];
        Marshal.Copy(data, bytes, 0, bytes.Length);
        var target = GCHandle.FromIntPtr(userData).Target as StringBuilder;
        target?.Append(Encoding.UTF8.GetString(bytes));
        return 0;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int LogCallbackDelegate(IntPtr data, nuint length, IntPtr userData);

    private static readonly LogCallbackDelegate LogCallbackInstance = LogCallback;
    private static readonly IntPtr LogCallbackPointer = Marshal.GetFunctionPointerForDelegate(LogCallbackInstance);

    private static class Native
    {
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr qpdfjob_init();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void qpdfjob_cleanup(ref IntPtr job);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int qpdfjob_initialize_from_json(IntPtr job, [MarshalAs(UnmanagedType.LPStr)] string json);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int qpdfjob_run(IntPtr job);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr qpdflogger_create();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void qpdflogger_cleanup(ref IntPtr logger);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void qpdflogger_set_error(IntPtr logger, int destination, IntPtr callback, IntPtr userData);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void qpdfjob_set_logger(IntPtr job, IntPtr logger);
    }
}

internal sealed class PdfNativeException(string message) : Exception(message);
