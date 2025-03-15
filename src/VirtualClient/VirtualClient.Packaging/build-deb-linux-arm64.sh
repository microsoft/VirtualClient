#!/bin/bash

EXIT_CODE=0
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE}"))"

# The default build version is defined in the repo VERSION file.
PACKAGE_VERSION=$(cat "$SCRIPT_DIR/../../../VERSION") | 's/^[[:space:]]*//;s/[[:space:]]*$//'

# The default build version can be overridden by the 'VCBuildVersion' 
# environment variable
if [[ -v "VCBuildVersion" && -n "$VCBuildVersion" ]]; then
    PACKAGE_VERSION="$VCBuildVersion"
fi

# Define variables
PACKAGE_NAME="virtualclient"
REPO_ROOT="$(cd "$(dirname "$0")" && pwd)/../../../"
BUILD_DIR="$REPO_ROOT/out/bin/Release/ARM64/VirtualClient.Main/net8.0/linux-arm64/"
DEB_DIR="$REPO_ROOT/out/packages/deb_arm64"
OUT_DIR="$REPO_ROOT/out/packages/"

# Create the DEBIAN control directory
mkdir -p "$DEB_DIR/DEBIAN"

# Create the control file with package metadata
cat > "$DEB_DIR/DEBIAN/control" << EOF
Package: $PACKAGE_NAME
Version: $PACKAGE_VERSION
Architecture: arm64
Maintainer: Virtual Client Team <virtualclient@microsoft.com>
Description: VirtualClient, the open sourced workload automation.
EOF

cat > "$DEB_DIR/DEBIAN/postinst" << EOF
#!/bin/bash
ln -sf /opt/virtualclient/VirtualClient /usr/bin/VirtualClient
ln -sf /opt/virtualclient/VirtualClient /usr/bin/virtualclient
EOF

# Copy the build files to the package directory (/opt/package-name)
mkdir -p "$DEB_DIR/opt/$PACKAGE_NAME"
cp -r "$BUILD_DIR"/* "$DEB_DIR/opt/$PACKAGE_NAME"

# Set permissions (adjust as needed)
chmod -R 775 "$DEB_DIR/opt/$PACKAGE_NAME"
chmod -R 775 "$DEB_DIR/DEBIAN"

# Build the package using dpkg-deb
dpkg-deb --build "$DEB_DIR" "$OUT_DIR/$PACKAGE_NAME"_"$PACKAGE_VERSION"_arm64.deb

echo "Debian package created: "$OUT_DIR/$PACKAGE_NAME"_"$PACKAGE_VERSION"_arm64.deb"