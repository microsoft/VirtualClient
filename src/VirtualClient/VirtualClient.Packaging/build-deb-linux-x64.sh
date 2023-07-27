#!/bin/bash

if [ $# -eq 1 ]; then
    PACKAGE_VERSION="$1"
else
    # Set a default version if no argument is provided
    PACKAGE_VERSION="1.0.0"
fi

# Define variables
PACKAGE_NAME="virtualclient"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/../../../"
BUILD_DIR="$REPO_ROOT/out/bin/Debug/x64/VirtualClient.Main/net6.0/linux-x64/publish/"
DEB_DIR="$REPO_ROOT/out/packages"

# Create the DEBIAN control directory
mkdir -p "$DEB_DIR/DEBIAN"

# Create the control file with package metadata
cat > "$DEB_DIR/DEBIAN/control" << EOF
Package: $PACKAGE_NAME
Version: $PACKAGE_VERSION
Architecture: all
Maintainer: Virtual Client Team <virtualclient@microsoft.com>
Description: VirtualClient, the open sourced workload automation.
EOF

cat > "$DEB_DIR/DEBIAN/postinst" << EOF
#!/bin/bash
ln -sf /opt/virtualclient/VirtualClient /usr/local/bin/VirtualClient
EOF

# Copy the build files to the package directory (/opt/package-name)
mkdir -p "$DEB_DIR/opt/$PACKAGE_NAME"
cp -r "$BUILD_DIR"/* "$DEB_DIR/opt/$PACKAGE_NAME"

# Set permissions (adjust as needed)
chmod -R 775 "$DEB_DIR/opt/$PACKAGE_NAME"
chmod -R 775 "$DEB_DIR/DEBIAN"

# Build the package using dpkg-deb
dpkg-deb --build "$DEB_DIR" "$PACKAGE_NAME"_"$DEB_DIR/$PACKAGE_VERSION"_amd64.deb

echo "Debian package created: "$PACKAGE_NAME"_"$DEB_DIR/$PACKAGE_VERSION"_amd64.deb"