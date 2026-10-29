# PdfXmpReader

Read XMP metadata from PDF files in .NET applications using the native qpdf library.

## Installation

```bash
dotnet add package PdfXmpReader
```

The package includes the qpdf native runtime files for Windows x64 (`win-x64`),
glibc Linux x64 (`linux-x64`), and Alpine Linux x64 (`linux-musl-x64`).
ARM64 native binaries are not included.

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

The result is the raw XMP XML packet, or `null` when the PDF does not contain XMP metadata. The input stream remains open after the call.

## Target frameworks

- .NET Standard 2.0
- .NET 8.0
- .NET 10.0

## License

PdfXmpReader is available under the MIT license. The package also includes qpdf and its associated license and notice files. See [LICENSE](LICENSE), [LICENSE.qpdf.txt](LICENSE.qpdf.txt), and [NOTICE.qpdf.txt](NOTICE.qpdf.txt).

The Alpine native library incorporates libjpeg-turbo. This software is based in
part on the work of the Independent JPEG Group.