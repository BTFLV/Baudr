#!/usr/bin/env bash
set -euo pipefail

RID="${1:-osx-arm64}"
CONFIGURATION="${2:-Release}"
PUBLISH_AOT="${3:-true}"
OUTPUT_DIR="${4:-artifacts}"

echo "=== Packaging Baudr for macOS ($RID) ==="

mkdir -p "$OUTPUT_DIR"
DIST_DIR="dist/$RID"
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

PUBLISH_ARGS=(
  publish src/Baudr.App/Baudr.App.csproj
  -c "$CONFIGURATION"
  -r "$RID"
  --self-contained
  -p:PublishSingleFile=true
  -o "$DIST_DIR"
)

if [ "$PUBLISH_AOT" = "true" ]; then
  PUBLISH_ARGS+=("-p:PublishAot=true")
fi

echo "Running: dotnet ${PUBLISH_ARGS[*]}"
dotnet "${PUBLISH_ARGS[@]}"

BINARY_PATH="$DIST_DIR/Baudr"
if [ ! -f "$BINARY_PATH" ]; then
  echo "Error: Binary not found at $BINARY_PATH"
  exit 1
fi
chmod +x "$BINARY_PATH"

# Run non-interactive verification
echo "Running package sanity check..."
VERIFY_REPORT="$DIST_DIR/verification.json"
"$BINARY_PATH" --verify-package "$VERIFY_REPORT"
if [ ! -f "$VERIFY_REPORT" ]; then
  echo "Error: Verification report not generated"
  exit 1
fi
echo "Verification Report: $(cat "$VERIFY_REPORT")"

# Assemble macOS App Bundle
APP_BUNDLE="dist/staging-$RID/Baudr.app"
rm -rf "dist/staging-$RID"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy binary
cp "$BINARY_PATH" "$APP_BUNDLE/Contents/MacOS/Baudr"

# Generate Info.plist
cat <<EOF > "$APP_BUNDLE/Contents/Info.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>Baudr</string>
    <key>CFBundleDisplayName</key>
    <string>Baudr</string>
    <key>CFBundleIdentifier</key>
    <string>com.baudr.app</string>
    <key>CFBundleVersion</key>
    <string>0.1.0</string>
    <key>CFBundleShortVersionString</key>
    <string>0.1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleExecutable</key>
    <string>Baudr</string>
    <key>CFBundleIconFile</key>
    <string>baudr-icon.icns</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSRequiresAquaSystemAppearance</key>
    <false/>
</dict>
</plist>
EOF

# PkgInfo
echo -n "APPL????" > "$APP_BUNDLE/Contents/PkgInfo"

# Copy Icon if present or copy png
if [ -f "src/Baudr.App/Assets/baudr-icon.icns" ]; then
  cp "src/Baudr.App/Assets/baudr-icon.icns" "$APP_BUNDLE/Contents/Resources/baudr-icon.icns"
fi

# Ad-hoc sign
if command -v codesign &> /dev/null; then
  echo "Ad-hoc signing $APP_BUNDLE..."
  codesign --force --deep -s - "$APP_BUNDLE" || true
fi

# Include documentation in staging
STAGING_DIR="dist/staging-$RID"
[ -f "README.md" ] && cp "README.md" "$STAGING_DIR/"
[ -f "LICENSE" ] && cp "LICENSE" "$STAGING_DIR/"
[ -f "THIRD-PARTY-NOTICES.md" ] && cp "THIRD-PARTY-NOTICES.md" "$STAGING_DIR/"

# Create zip (preserving symlinks and executable bits)
ZIP_NAME="Baudr-$RID.zip"
ZIP_PATH="$OUTPUT_DIR/$ZIP_NAME"
rm -f "$ZIP_PATH"

echo "Creating archive $ZIP_PATH..."
(cd "$STAGING_DIR" && zip -r -y "../../$ZIP_PATH" .)

echo "Successfully packaged $ZIP_PATH"

