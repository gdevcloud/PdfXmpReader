# PdfXmpReader

Read XMP metadata from PDF files in .NET applications using the native qpdf library.

## Installation

```bash
dotnet add package PdfXmpReader
```

The package includes the qpdf native runtime files for Windows x64 and Linux x64.

## Usage

Read from a file path:

```csharp
using PdfXmpReader;

var xmp = PdfXmpReader.ReadXmp("document.pdf");
if (xmp is not null)
{
    Console.WriteLine(xmp);
}
```

Read from a readable stream:

```csharp
using PdfXmpReader;

using var stream = File.OpenRead("document.pdf");
var xmp = PdfXmpReader.ReadXmp(stream);
```

The result is the raw XMP XML packet, or `null` when the PDF does not contain XMP metadata. The input stream remains open after the call.

## Target frameworks

- .NET Standard 2.0
- .NET 8.0
- .NET 10.0

## License

PdfXmpReader is available under the MIT license. The package also includes qpdf and its associated license and notice files. See [LICENSE](LICENSE), [LICENSE.qpdf.txt](LICENSE.qpdf.txt), and [NOTICE.qpdf.txt](NOTICE.qpdf.txt).