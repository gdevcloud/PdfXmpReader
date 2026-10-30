#!/bin/sh
set -eu

apk add --no-cache build-base cmake ninja zlib-dev libjpeg-turbo-dev

cmake -S /src/qpdf -B /tmp/qpdf-build -G Ninja \
    -DCMAKE_PROJECT_qpdf_INCLUDE=/package/qpdf-native-name.cmake \
    -DCMAKE_BUILD_TYPE=Release \
    -DBUILD_SHARED_LIBS=ON \
    -DBUILD_STATIC_LIBS=OFF \
    -DUSE_IMPLICIT_CRYPTO=OFF \
    -DREQUIRE_CRYPTO_NATIVE=ON \
    -DCMAKE_BUILD_WITH_INSTALL_RPATH=ON \
    '-DCMAKE_INSTALL_RPATH=$ORIGIN'
cmake --build /tmp/qpdf-build --target libqpdf --parallel 2

mkdir -p /package/runtimes/linux-musl-x64/native
rm -f /package/runtimes/linux-musl-x64/native/libqpdf.so
cp -L /tmp/qpdf-build/libqpdf/libpdfxmpreader_qpdf.so /package/runtimes/linux-musl-x64/native/libpdfxmpreader_qpdf.so
cp -L /usr/lib/libjpeg.so.8 /package/runtimes/linux-musl-x64/native/libjpeg.so.8
strip /package/runtimes/linux-musl-x64/native/libpdfxmpreader_qpdf.so
readelf -d /package/runtimes/linux-musl-x64/native/libpdfxmpreader_qpdf.so | grep SONAME
ldd /package/runtimes/linux-musl-x64/native/libpdfxmpreader_qpdf.so