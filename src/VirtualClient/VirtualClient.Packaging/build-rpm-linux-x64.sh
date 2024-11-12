#!/bin/bash

if [ $# -eq 1 ]; then
    PACKAGE_VERSION="$1"
else
    # Set a default version if no argument is provided
    PACKAGE_VERSION="1.0.0"
fi

# Define variables
PACKAGE_NAME="virtualclient"
REPO_ROOT="$(cd "$(dirname "$0")" && pwd)/../../../"
BUILD_DIR="$REPO_ROOT/out/bin/Release/x64/VirtualClient.Main/net8.0/linux-x64/"
RPM_DIR="$REPO_ROOT/out/packages/rpm_x64"
OUT_DIR="$REPO_ROOT/out/packages/"

# Create RPM build structure
mkdir -p "$RPM_DIR/SPECS"
mkdir -p "$RPM_DIR/BUILD"
mkdir -p "$RPM_DIR/RPMS"
mkdir -p "$RPM_DIR/SOURCES"
mkdir -p "$RPM_DIR/SRPMS"

# Create the RPM spec file with package metadata
cat > "$RPM_DIR/SPECS/$PACKAGE_NAME.spec" << EOF
Name: $PACKAGE_NAME
Version: $PACKAGE_VERSION
Release: 1
Summary: VirtualClient, the open sourced workload automation
License: MIT

%description
VirtualClient is an open-source workload automation tool.

%install
rm -rf \$RPM_BUILD_ROOT
mkdir -p \$RPM_BUILD_ROOT/opt/$PACKAGE_NAME
cp -r $BUILD_DIR/* \$RPM_BUILD_ROOT/opt/$PACKAGE_NAME

%files
/opt/$PACKAGE_NAME

%post
ln -s /opt/$PACKAGE_NAME/VirtualClient /usr/bin/VirtualClient
ln -s /opt/$PACKAGE_NAME/VirtualClient /usr/bin/virtualclient

%clean

%prep

%build

%pre
EOF

# Copy the build files to the RPM package directory
cp -r "$BUILD_DIR"/* "$RPM_DIR/SOURCES/"

# Build the RPM package
rpmbuild --target x86_64 --define "_topdir $RPM_DIR" -ba "$RPM_DIR/SPECS/$PACKAGE_NAME.spec"

# out/packages/rpm_x64/RPMS/x86_64/virtualclient-1.0.0-1.aarch64.rpm
echo "RPM package created: $RPM_DIR/RPMS/x86_64/$PACKAGE_NAME-$PACKAGE_VERSION-1.x86_64.rpm"
