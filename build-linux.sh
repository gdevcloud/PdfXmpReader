#!/bin/sh
set -eu

apt-get update
apt-get install -y --no-install-recommends build-essential cmake ninja-build zlib1g-dev libjpeg62-turbo-dev

cmake -S /src/qpdf -B /tmp/qpdf-build -G Ninja \
    -DCMAKE_BUILD_TYPE=Release \
    -DBUILD_SHARED_LIBS=ON \
    -DBUILD_STATIC_LIBS=OFF \
    -DUSE_IMPLICIT_CRYPTO=OFF \
    -DREQUIRE_CRYPTO_NATIVE=ON \
    -DCMAKE_BUILD_WITH_INSTALL_RPATH=ON \
    '-DCMAKE_INSTALL_RPATH=$ORIGIN'
cmake --build /tmp/qpdf-build --target libqpdf --parallel 2

output=/package/runtimes/linux-x64/native
mkdir -p "$output"
# Remove the obsolete Ubuntu dependency closure, including its newer glibc requirements.
for library in libcrypto.so.3 libffi.so.8 libgcc_s.so.1 libgmp.so.10 \
    libgnutls.so.30 libhogweed.so.6 libidn2.so.0 libjpeg.so.8 libnettle.so.8 \
    libp11-kit.so.0 libqpdf.so.30 libstdc++.so.6 libtasn1.so.6 libunistring.so.5 libz.so.1
do
    rm -f "$output/$library"
done
cp -L /tmp/qpdf-build/libqpdf/libqpdf.so "$output/libqpdf.so"
cp -L /usr/lib/x86_64-linux-gnu/libjpeg.so.62 "$output/libjpeg.so.62"
strip "$output/libqpdf.so"
ldd "$output/libqpdf.so"
