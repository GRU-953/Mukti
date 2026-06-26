#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
VERSION="2.0.8"
INSTALL_PATH="/Library/Application Support/Mukti"
SERVER_PORT="43017"
OUTPUT_DIR="$SCRIPT_DIR/output"

echo "Building Mukti v2 Mac installer..."
mkdir -p "$OUTPUT_DIR"

echo "Publishing server x64..."
dotnet publish "$REPO_ROOT/src/Mukti.Server/Mukti.Server.csproj" \
    --configuration Release --runtime osx-x64 --self-contained true \
    -p:PublishSingleFile=true --output "$OUTPUT_DIR/server-x64"

echo "Publishing server arm64..."
dotnet publish "$REPO_ROOT/src/Mukti.Server/Mukti.Server.csproj" \
    --configuration Release --runtime osx-arm64 --self-contained true \
    -p:PublishSingleFile=true --output "$OUTPUT_DIR/server-arm64"

echo "Creating universal binary..."
mkdir -p "$OUTPUT_DIR/server-universal"
lipo -create "$OUTPUT_DIR/server-x64/mukti-server" "$OUTPUT_DIR/server-arm64/mukti-server" \
    -output "$OUTPUT_DIR/server-universal/mukti-server"
chmod +x "$OUTPUT_DIR/server-universal/mukti-server"

echo "Building Blazor WASM..."
dotnet publish "$REPO_ROOT/src/Mukti.Mac/Mukti.Mac.csproj" \
    --configuration Release --output "$OUTPUT_DIR/wasm-build"
WWWROOT="$OUTPUT_DIR/wasm-build/wwwroot"

echo "Substituting port in manifest..."
sed "s/__PORT__/$SERVER_PORT/g" "$SCRIPT_DIR/manifest-template.xml" > "$WWWROOT/manifest.xml"

echo "Assembling payload..."
PAYLOAD="$OUTPUT_DIR/payload"
APP_DIR="${PAYLOAD}${INSTALL_PATH}"
mkdir -p "$APP_DIR"
cp "$OUTPUT_DIR/server-universal/mukti-server" "$APP_DIR/mukti-server"
chmod +x "$APP_DIR/mukti-server"
cp -r "$WWWROOT" "$APP_DIR/wwwroot"
mkdir -p "$APP_DIR/data"
cp "$REPO_ROOT/data/bijoy-sutonnymj.json" "$APP_DIR/data/"

LAUNCH_AGENTS_DIR="$PAYLOAD/Library/LaunchAgents"
mkdir -p "$LAUNCH_AGENTS_DIR"
sed "s|__MUKTI_INSTALL_PATH__|$INSTALL_PATH|g" \
    "$SCRIPT_DIR/com.mukti.server.plist" > "$LAUNCH_AGENTS_DIR/com.mukti.server.plist"

chmod +x "$SCRIPT_DIR/pkg-scripts/postinstall"
chmod +x "$SCRIPT_DIR/pkg-scripts/preremove"

echo "Building .pkg..."
pkgbuild --root "$PAYLOAD" --identifier "com.mukti.addin" --version "$VERSION" \
    --scripts "$SCRIPT_DIR/pkg-scripts" "$OUTPUT_DIR/Mukti-$VERSION.pkg"

echo "Done: $OUTPUT_DIR/Mukti-$VERSION.pkg"
