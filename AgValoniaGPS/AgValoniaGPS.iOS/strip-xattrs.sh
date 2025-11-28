#!/bin/bash
# Strip extended attributes from iOS app bundle
# Called from MSBuild during build process

APP_BUNDLE="$1"

if [ -z "$APP_BUNDLE" ]; then
    echo "Error: No app bundle path provided"
    exit 1
fi

if [ ! -d "$APP_BUNDLE" ]; then
    echo "Error: App bundle not found: $APP_BUNDLE"
    exit 1
fi

echo "=== Stripping xattrs from: $APP_BUNDLE ==="

# Use dot_clean to remove AppleDouble files first
dot_clean "$APP_BUNDLE" 2>/dev/null || true

# Remove specific problematic xattrs recursively from entire bundle
/usr/bin/xattr -rd com.apple.FinderInfo "$APP_BUNDLE" 2>/dev/null || true
/usr/bin/xattr -rd "com.apple.fileprovider.fpfs#P" "$APP_BUNDLE" 2>/dev/null || true
/usr/bin/xattr -rd com.apple.provenance "$APP_BUNDLE" 2>/dev/null || true

# Also specifically target the Frameworks directory where native libs live
FRAMEWORKS_DIR="$APP_BUNDLE/Frameworks"
if [ -d "$FRAMEWORKS_DIR" ]; then
    echo "Stripping Frameworks directory..."
    # Strip each framework individually
    for framework in "$FRAMEWORKS_DIR"/*.framework; do
        if [ -d "$framework" ]; then
            /usr/bin/xattr -cr "$framework" 2>/dev/null || true
        fi
    done
fi

# Verify FinderInfo is gone from critical locations
echo "Verifying frameworks..."
if [ -d "$FRAMEWORKS_DIR" ]; then
    for framework in "$FRAMEWORKS_DIR"/*.framework; do
        if [ -d "$framework" ]; then
            if /usr/bin/xattr "$framework" 2>/dev/null | grep -q FinderInfo; then
                echo "WARNING: FinderInfo still present on $framework"
            else
                echo "OK: $(basename "$framework")"
            fi
        fi
    done
fi

echo "=== Done stripping xattrs ==="
exit 0
