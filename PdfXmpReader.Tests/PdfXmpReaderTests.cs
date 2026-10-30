using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace PdfXmpReader.Tests;

public sealed class PdfXmpReaderTests
{
    [Test, Order(-1)]
    public void NativeLibrariesCoexistWithQuestPdf()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pdf-xmp-coexist-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var input = Path.Combine(directory, "input.pdf");
            var merged = Path.Combine(directory, "merged.pdf");
            const string xmp = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>";
            var metadataPdf = BuildPdfWithMetadata(Encoding.UTF8.GetBytes(xmp));

            void ReadMetadata() =>
                Assert.That(PdfXmpReader.ReadXmp(new MemoryStream(metadataPdf)), Is.EqualTo(xmp));

            void MergeWithQuestPdf()
            {
                QuestPDF.Settings.License = LicenseType.Community;
                Document.Create(container =>
                    container.Page(page => page.Content().Text("Native library coexistence")))
                    .WithMetadata(new DocumentMetadata { Title = "Native coexistence" })
                    .WithSettings(new DocumentSettings { PDFA_Conformance = PDFA_Conformance.PDFA_3B })
                    .GeneratePdf(input);
                DocumentOperation.LoadFile(input, null)
                    .MergeFile(input, null)
                    .Save(merged);
                Assert.That(new FileInfo(merged).Length, Is.GreaterThan(100));
            }

            if (Environment.GetEnvironmentVariable("PDFXMPREADER_NATIVE_ORDER") == "quest-first")
            {
                MergeWithQuestPdf();
                ReadMetadata();
            }
            else
            {
                ReadMetadata();
                MergeWithQuestPdf();
            }

            Assert.That(PdfXmpReader.ReadXmp(merged), Does.Contain("Native coexistence"));
            ReadMetadata();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

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

        using var stream = new MemoryStream(pdf);
        Assert.That(PdfXmpReader.ReadXmp(stream), Is.Null);
        stream.Position = 0;
        Assert.That(PdfXmpReader.ReadXmpBytes(stream), Is.Null);
    }

    [TestCase("utf-8", false, false)]
    [TestCase("utf-8", false, true)]
    [TestCase("utf-8", true, false)]
    [TestCase("utf-8", true, true)]
    [TestCase("utf-16", false, false)]
    [TestCase("utf-16", false, true)]
    [TestCase("utf-16", true, false)]
    [TestCase("utf-16", true, true)]
    [TestCase("utf-16BE", false, false)]
    [TestCase("utf-16BE", false, true)]
    [TestCase("utf-16BE", true, false)]
    [TestCase("utf-16BE", true, true)]
    [TestCase("iso-8859-1", false, true)]
    public void PreservesOriginalBytesAndDecodesXml(string encodingName, bool includeBom, bool includeDeclaration)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var declaration = includeDeclaration ? $"<?xml version=\"1.0\" encoding=\"{encodingName}\"?>" : string.Empty;
        var xml = declaration + "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description rdf:about=\"\" xmlns:pdfx=\"http://ns.adobe.com/pdfx/1.3/\"><pdfx:Surname>Garc\u00eda</pdfx:Surname></rdf:Description></rdf:RDF></x:xmpmeta>";
        var bytes = (includeBom ? encoding.GetPreamble() : Array.Empty<byte>()).Concat(encoding.GetBytes(xml)).ToArray();
        using var stream = new MemoryStream(BuildPdfWithMetadata(bytes));

        var actualBytes = PdfXmpReader.ReadXmpBytes(stream);
        Assert.That(actualBytes, Is.EqualTo(bytes));
        Assert.That(stream.CanRead, Is.True);
        using (var metadataStream = new MemoryStream(actualBytes!))
        using (var reader = XmlReader.Create(metadataStream))
        {
            AssertSurname(XDocument.Load(reader));
        }

        stream.Position = 0;
        var actualXml = PdfXmpReader.ReadXmp(stream);
        Assert.That(actualXml, Is.EqualTo(xml));
        Assert.That(stream.CanRead, Is.True);
        AssertSurname(XDocument.Parse(actualXml!));

        var path = Path.Combine(Path.GetTempPath(), $"pdf-xmp-reader-test-{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(path, stream.ToArray());
            Assert.That(PdfXmpReader.ReadXmpBytes(path), Is.EqualTo(bytes));
            Assert.That(PdfXmpReader.ReadXmp(path), Is.EqualTo(xml));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void AssertSurname(XDocument document) =>
        Assert.That(document.Descendants(XName.Get("Surname", "http://ns.adobe.com/pdfx/1.3/")).Single().Value,
            Is.EqualTo("Garc\u00eda"));

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


    private static byte[] BuildPdf(string metadataObject, bool includeMetadataReference = true) =>
        BuildPdf(Encoding.UTF8.GetBytes(metadataObject), includeMetadataReference);

    private static byte[] BuildPdfWithMetadata(byte[] metadata)
    {
        using var output = new MemoryStream();
        Write(output, $"<< /Type /Metadata /Subtype /XML /Length {metadata.Length} >>\nstream\n");
        output.Write(metadata, 0, metadata.Length);
        Write(output, "\nendstream");
        return BuildPdf(output.ToArray(), includeMetadataReference: true);
    }

    private static byte[] BuildPdf(byte[] metadataObject, bool includeMetadataReference)
    {
        var catalog = includeMetadataReference ? "<< /Type /Catalog /Pages 2 0 R /Metadata 3 0 R >>" : "<< /Type /Catalog /Pages 2 0 R >>";
        var pages = "<< /Type /Pages /Count 0 /Kids [] >>";
        var objects = new List<byte[]> { Encoding.UTF8.GetBytes(catalog), Encoding.UTF8.GetBytes(pages) };
        if (includeMetadataReference) objects.Add(metadataObject);
        using var output = new MemoryStream();
        Write(output, "%PDF-1.7\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write(output, $"{i + 1} 0 obj\n");
            output.Write(objects[i], 0, objects[i].Length);
            Write(output, "\nendobj\n");
        }

        var xrefOffset = output.Position;
        Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return output.ToArray();
    }

    private static void Write(Stream stream, string value) => stream.Write(Encoding.UTF8.GetBytes(value));

}
