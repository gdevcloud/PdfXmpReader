using System.Xml;

namespace PdfXmpReader;

/// <summary>Reads XMP metadata packets from PDF files using the qpdf native library.</summary>
public static class PdfXmpReader
{
    /// <summary>Reads XMP metadata from a PDF file.</summary>
    /// <returns>The raw XMP XML packet, or <see langword="null" /> when no metadata exists.</returns>
    public static string? ReadXmp(string filePath) => DecodeXml(ReadXmpBytes(filePath));

    /// <summary>Reads XMP metadata from a readable PDF stream without closing it.</summary>
    /// <returns>The decoded XMP XML packet, or <see langword="null" /> when no metadata exists.</returns>
    public static string? ReadXmp(Stream pdfStream) => DecodeXml(ReadXmpBytes(pdfStream));

    /// <summary>Reads the original XMP metadata bytes from a PDF file.</summary>
    /// <returns>The XMP XML packet in its original encoding, or <see langword="null" /> when no metadata exists.</returns>
    public static byte[]? ReadXmpBytes(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("A PDF file path is required.", nameof(filePath));
        return QpdfMetadataReader.ReadBytes(filePath);
    }

    /// <summary>Reads the original XMP metadata bytes from a readable PDF stream without closing it.</summary>
    /// <returns>The XMP XML packet in its original encoding, or <see langword="null" /> when no metadata exists.</returns>
    public static byte[]? ReadXmpBytes(Stream pdfStream)
    {
        if (pdfStream is null)
            throw new ArgumentNullException(nameof(pdfStream));

        if (!pdfStream.CanRead)
            throw new ArgumentException("The PDF stream must be readable.", nameof(pdfStream));

        var path = Path.Combine(Path.GetTempPath(), $"pdf-xmp-reader-{Guid.NewGuid():N}.pdf");
        try
        {
            using (var output = File.Create(path))
                pdfStream.CopyTo(output);

            return QpdfMetadataReader.ReadBytes(path);
        }
        finally
        {
            File.Delete(path);
        }

    }

    private static string? DecodeXml(byte[]? bytes)
    {
        if (bytes is null)
            return null;

        using var stream = new MemoryStream(bytes);
        using var reader = new XmlTextReader(stream)
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        // Let the XML reader detect BOMs, byte order and the encoding declaration.
        reader.Read();
        var encoding = reader.Encoding ?? throw new XmlException("Unable to determine the XMP XML encoding.");
        var preamble = encoding.GetPreamble();
        var offset = bytes.Take(preamble.Length).SequenceEqual(preamble) ? preamble.Length : 0;
        return encoding.GetString(bytes, offset, bytes.Length - offset);
    }
}
