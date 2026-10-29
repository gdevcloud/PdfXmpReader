# PdfXmpReader

Read XMP metadata from PDF files in .NET applications using the native qpdf library.

Source code and issues: [gdevcloud/PdfXmpReader](https://github.com/gdevcloud/PdfXmpReader).

## Installation

```bash
dotnet add package PdfXmpReader
```

The package includes the qpdf native runtime files for Windows x64 (`win-x64`),
glibc Linux x64 (`linux-x64`), and Alpine Linux x64 (`linux-musl-x64`).
ARM64 native binaries are not included.

### Debian Linux containers

Version 1.0.3 builds the glibc native library on Debian 12 (glibc 2.36).
Restore and publish with `linux-x64` for Debian-based images, including Azure
Functions .NET 8 isolated images. No system qpdf installation or custom
`DllImport` resolver is required. The package bundles libjpeg; glibc, zlib,
and the GCC runtime are supplied by the .NET images.

Versions 1.0.1 and 1.0.2 bundled Ubuntu libraries requiring glibc 2.38 and
cannot load in Debian 12 without a native-library workaround.

To rebuild with qpdf 12.4.2 checked out in a sibling `qpdf` directory, run
from the PdfXmpReader directory (PowerShell):

```powershell
docker run --rm --platform linux/amd64 `
    --mount "type=bind,source=$((Resolve-Path ../qpdf).Path),target=/src/qpdf,readonly" `
    --mount "type=bind,source=$((Get-Location).Path),target=/package" `
    mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim sh /package/build-linux.sh
```

The script replaces the old Ubuntu dependency closure with `libqpdf.so` and
`libjpeg.so.62`, and uses qpdf's native crypto provider.

### Alpine Linux containers

Use the Alpine runtime identifier for both restore and publish:

```bash
dotnet restore --runtime linux-musl-x64
dotnet publish --configuration Release --runtime linux-musl-x64 --self-contained false --no-restore
```

The application can use `mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.22` with
`mcr.microsoft.com/dotnet/sdk:8.0-alpine3.22` for building and testing. No qpdf
installation or glibc compatibility layer is needed. The package also bundles
libjpeg; musl, zlib, and the GCC runtime are supplied by these .NET images.

Do not publish with `linux-x64` for an Alpine container: that selects the glibc
binary instead of the musl binary.

### Rebuilding the Alpine native library

With qpdf 12.4.2 checked out in a sibling `qpdf` directory, run the following
from the PdfXmpReader directory (PowerShell):

```powershell
docker run --rm --platform linux/amd64 `
    --mount "type=bind,source=$((Resolve-Path ../qpdf).Path),target=/src/qpdf,readonly" `
    --mount "type=bind,source=$((Get-Location).Path),target=/package" `
    mcr.microsoft.com/dotnet/sdk:8.0-alpine3.22 sh /package/build-linux-musl.sh
```

The script writes `libqpdf.so` and `libjpeg.so.8` into
`runtimes/linux-musl-x64/native/`, and uses qpdf's native crypto provider.

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

The result is the decoded XMP XML packet, or `null` when the PDF does not contain
XMP metadata. XML encoding detection honors the byte order mark and encoding
declaration, including UTF-8, UTF-16LE and UTF-16BE. A byte order mark is not
included in the returned string. The input stream remains open after the call.

For XML parsers that accept bytes, use `ReadXmpBytes` to preserve the original
encoding, declaration and byte order mark without a string conversion:

```csharp
using var stream = File.OpenRead("document.pdf");
var xmpBytes = PdfXmpReader.ReadXmpBytes(stream);
if (xmpBytes is not null)
{
    using var metadataStream = new MemoryStream(xmpBytes);
    using var reader = System.Xml.XmlReader.Create(metadataStream);
    var metadata = System.Xml.Linq.XDocument.Load(reader);
}
```

Both `ReadXmpBytes` and `ReadXmp` accept a file path or a readable stream and
return `null` when metadata is absent. Do not re-encode the string result as
UTF-8 while retaining a declaration for a different encoding; use the byte API
instead. Version 1.0.4 fixes the unconditional UTF-8 decoding in earlier versions;
the bundled native qpdf libraries are unchanged.

## Target frameworks

- .NET Standard 2.0
- .NET 8.0
- .NET 10.0

## License

PdfXmpReader is available under the MIT license. The package also includes qpdf and its associated license and notice files. See [LICENSE](LICENSE), [LICENSE.qpdf.txt](LICENSE.qpdf.txt), and [NOTICE.qpdf.txt](NOTICE.qpdf.txt).

The Linux native libraries incorporate libjpeg-turbo. This software is based in
part on the work of the Independent JPEG Group.