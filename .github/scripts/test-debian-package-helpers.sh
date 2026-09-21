#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=debian-package-helpers.sh
source "$script_dir/debian-package-helpers.sh"

test_root="$(mktemp -d)"
trap 'rm -rf "$test_root"' EXIT

# A current .NET self-contained publish can carry an optional LTTng provider
# linked to the obsolete liblttng-ust.so.0 ABI. Verify that only this exact
# optional component is pruned.
diagnostics_root="$test_root/diagnostics"
mkdir -p "$diagnostics_root"
cat > "$test_root/lttng.c" <<'EOF'
void lttng_test_symbol(void) {}
EOF
cat > "$test_root/trace-provider.c" <<'EOF'
extern void lttng_test_symbol(void);
void trace_provider_test(void) { lttng_test_symbol(); }
EOF
gcc -fPIC -shared -Wl,-soname,liblttng-ust.so.0 \
  -o "$test_root/liblttng-ust.so.0" "$test_root/lttng.c"
ln -s liblttng-ust.so.0 "$test_root/liblttng-ust.so"
gcc -fPIC -shared \
  -o "$diagnostics_root/libcoreclrtraceptprovider.so" \
  "$test_root/trace-provider.c" \
  -L"$test_root" \
  -Wl,--no-as-needed \
  -llttng-ust

debian_prune_optional_dotnet_diagnostics "$diagnostics_root"
if [ -e "$diagnostics_root/libcoreclrtraceptprovider.so" ]; then
  echo "Legacy .NET LTTng provider was not pruned" >&2
  exit 1
fi

install -m 0755 /bin/true "$diagnostics_root/libcoreclrtraceptprovider.so"
debian_prune_optional_dotnet_diagnostics "$diagnostics_root"
if [ ! -e "$diagnostics_root/libcoreclrtraceptprovider.so" ]; then
  echo "Compatible diagnostics provider was incorrectly pruned" >&2
  exit 1
fi

package_root="$test_root/package"
mkdir -p "$package_root/DEBIAN" "$package_root/usr/bin"
install -m 0755 /bin/true "$package_root/usr/bin/helper-test"

generated="$(debian_generate_shlib_depends helper-test "$package_root")"
case "$generated" in
  *libc6*) ;;
  *)
    echo "Expected dpkg-shlibdeps to generate a libc6 dependency, got: $generated" >&2
    exit 1
    ;;
esac

# Exercise the split-package path used by INDI: an executable in one package
# links to a private SONAME library supplied by a second package.
provider_root="$test_root/provider"
consumer_root="$test_root/consumer"
mkdir -p "$provider_root/usr/lib/helper-test" "$consumer_root/usr/bin"
cat > "$test_root/private.c" <<'EOF'
int helper_private_answer(void) { return 42; }
EOF
cat > "$test_root/consumer.c" <<'EOF'
extern int helper_private_answer(void);
int main(void) { return helper_private_answer() == 42 ? 0 : 1; }
EOF
gcc -fPIC -shared \
  -Wl,-soname,libhelper-private.so.1 \
  -o "$provider_root/usr/lib/helper-test/libhelper-private.so.1.0" \
  "$test_root/private.c"
ln -s libhelper-private.so.1.0 "$provider_root/usr/lib/helper-test/libhelper-private.so.1"
ln -s libhelper-private.so.1 "$provider_root/usr/lib/helper-test/libhelper-private.so"
gcc \
  -o "$consumer_root/usr/bin/helper-consumer" \
  "$test_root/consumer.c" \
  -L"$provider_root/usr/lib/helper-test" \
  -lhelper-private

private_generated="$(debian_generate_shlib_depends \
  helper-consumer \
  "$consumer_root" \
  "$provider_root|helper-provider (= 1.0)")"
case "$private_generated" in
  *'helper-provider (= 1.0)'*) ;;
  *)
    echo "Expected a dependency on the private library provider, got: $private_generated" >&2
    exit 1
    ;;
esac

mkdir -p "$consumer_root/DEBIAN"
cat > "$consumer_root/DEBIAN/control" <<EOF
Package: helper-consumer
Version: 1.0
Architecture: amd64
Maintainer: PINS CI <noreply@github.com>
Depends: $private_generated
Description: Split-library dependency test package
EOF
consumer_package="$test_root/helper-consumer.deb"
dpkg-deb --root-owner-group --build "$consumer_root" "$consumer_package" >/dev/null
debian_assert_elf_closure "$consumer_package" "$provider_root"

merged="$(debian_merge_depends "$generated" 'ca-certificates, libc6')"
case "$merged" in
  *libc6*ca-certificates*) ;;
  *)
    echo "Dependency merge returned an unexpected result: $merged" >&2
    exit 1
    ;;
esac

cat > "$package_root/DEBIAN/control" <<'EOF'
Package: helper-test
Version: 1.0.0
Section: utils
Priority: optional
Architecture: amd64
Maintainer: PINS CI <noreply@github.com>
Description: Test package for the shared Debian workflow helpers
EOF
debian_control_set_depends "$package_root/DEBIAN/control" "$merged"

package_path="$test_root/helper-test.deb"
dpkg-deb --root-owner-group --build "$package_root" "$package_path" >/dev/null
debian_validate_package "$package_path" yes
debian_assert_elf_closure "$package_path"

echo "Debian package helper tests passed."
