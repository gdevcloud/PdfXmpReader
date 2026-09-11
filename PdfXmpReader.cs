namespace PdfXmpReader;

/// <summary>Reads XMP metadata packets from PDF files using the qpdf native library.</summary>
public static class PdfXmpReader
{
    /// <summary>Reads XMP metadata from a PDF file.</summary>
    /// <returns>The raw XMP XML packet, or <see langword="null" /> when no metadata exists.</returns>
    public static string? ReadXmp(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("A PDF file path is required.", nameof(filePath));
        return QpdfMetadataReader.Read(filePath);
    }

    /// <summary>Reads XMP metadata from a readable PDF stream without closing it.</summary>
    /// <returns>The raw XMP XML packet, or <see langword="null" /> when no metadata exists.</returns>
    public static string? ReadXmp(Stream pdfStream)
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

            return QpdfMetadataReader.Read(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
