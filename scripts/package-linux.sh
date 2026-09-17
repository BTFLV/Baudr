#!/usr/bin/env bash
set -euo pipefail

RID="${1:-linux-x64}"
CONFIGURATION="${2:-Release}"
PUBLISH_AOT="${3:-true}"
OUTPUT_DIR="${4:-artifacts}"

echo "=== Packaging Baudr for Linux ($RID) ==="

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

# Prepare clean release directory
STAGING_DIR="dist/staging-$RID"
rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR"

cp "$BINARY_PATH" "$STAGING_DIR/Baudr"
chmod +x "$STAGING_DIR/Baudr"

[ -f "README.md" ] && cp "README.md" "$STAGING_DIR/"
[ -f "LICENSE" ] && cp "LICENSE" "$STAGING_DIR/"
[ -f "THIRD-PARTY-NOTICES.md" ] && cp "THIRD-PARTY-NOTICES.md" "$STAGING_DIR/"

# Create zip
ZIP_NAME="Baudr-$RID.zip"
ZIP_PATH="$OUTPUT_DIR/$ZIP_NAME"
rm -f "$ZIP_PATH"

echo "Creating archive $ZIP_PATH..."
(cd "$STAGING_DIR" && zip -r -y "../../$ZIP_PATH" .)

echo "Successfully packaged $ZIP_PATH"

