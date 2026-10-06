#!/usr/bin/env bash
set -euo pipefail

# Plain Debian build containers need the target Pi OS repository to resolve
# pinsdaemon's raspi-config dependency. The optional root permits isolated checks.
apt_root="$(realpath "${1:-/}")"
keyring="${apt_root%/}/usr/share/keyrings/pins-raspberrypi-archive-keyring.pgp"
install -d -m 755 "$(dirname "$keyring")" "${apt_root%/}/etc/apt/sources.list.d" "${apt_root%/}/etc/apt/preferences.d"

# Use the current Pi OS keyring from an immutable pi-gen revision. The old
# raspberrypi.gpg.key has SHA-1 signatures rejected by modern Trixie APT.
curl --fail --silent --show-error --location --retry 3 \
  https://raw.githubusercontent.com/RPi-Distro/pi-gen/291c62c1108d03f594dbed65507e97eb0d9a2b0e/stage0/00-configure-apt/files/raspberrypi-archive-keyring.pgp \
  --output "$keyring"
chmod 644 "$keyring"

cat > "${apt_root%/}/etc/apt/sources.list.d/pins-raspberrypi.sources" <<EOF
Types: deb
URIs: https://archive.raspberrypi.com/debian/
Suites: trixie
Components: main
Architectures: arm64
Signed-By: $keyring
EOF

# Prefer Debian packages; use Pi packages only where Debian has no candidate.
cat > "${apt_root%/}/etc/apt/preferences.d/pins-raspberrypi" <<'EOF'
Package: *
Pin: origin "archive.raspberrypi.com"
Pin-Priority: 100
EOF
