using System.Text;
using NUnit.Framework;

namespace PdfXmpReader.Tests;

public sealed class PdfXmpReaderTests
{
    [Test]
    public void ReadsUncompressedXmpFromTraditionalPdf()
    {
        const string xmp = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/></x:xmpmeta>";
        var pdf = BuildPdf($"<< /Type /Metadata /Subtype /XML /Length {Encoding.UTF8.GetByteCount(xmp)} >>\nstream\n{xmp}\nendstream");

        var result = PdfXmpReader.ReadXmp(new MemoryStream(pdf));

        Assert.That(result, Does.Contain("xmpmeta"));
    }

    [Test]
    public void ReturnsNullWhenCatalogHasNoMetadata()
    {
        var pdf = BuildPdf("<< /Type /Catalog >>", includeMetadataReference: false);

        Assert.That(PdfXmpReader.ReadXmp(new MemoryStream(pdf)), Is.Null);
    }

    [Test]
    public void RejectsMalformedPdf()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\nstartxref\n9\n%%EOF");

        try
        {
            _ = PdfXmpReader.ReadXmp(new MemoryStream(pdf));
            Assert.Fail("Expected malformed PDF to be rejected.");
        }
        catch (Exception exception) when (exception is not AssertionException)
        {
        }
    }


    private static byte[] BuildPdf(string metadataObject, bool includeMetadataReference = true)
    {
        var catalog = includeMetadataReference ? "<< /Type /Catalog /Pages 2 0 R /Metadata 3 0 R >>" : "<< /Type /Catalog /Pages 2 0 R >>";
        var pages = "<< /Type /Pages /Count 0 /Kids [] >>";
        var objects = new List<string> { catalog, pages };
        if (includeMetadataReference) objects.Add(metadataObject);
        using var output = new MemoryStream();
        Write(output, "%PDF-1.7\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write(output, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefOffset = output.Position;
        Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return output.ToArray();
    }

    private static void Write(Stream stream, string value) => stream.Write(Encoding.UTF8.GetBytes(value));

}
