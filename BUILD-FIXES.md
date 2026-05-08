# Build-Fixes für build-and-install-pins-x64.sh

Dieses Dokument beschreibt alle Anpassungen, die am Skript `build-and-install-pins-x64.sh` vorgenommen wurden, um einen erfolgreichen Build auf Ubuntu 24.04 (Linux Mint Zena) mit 8 Kernen sicherzustellen.

---

## Fix 1: Ninja-Generator für OpenCV-Build

**Datei:** `build-and-install-pins-x64.sh`
**Funktion:** `build_and_install_opencv_required_version()`

**Problem:**
`cmake --build ... --parallel $(nproc)` mit dem Standard-Makefile-Generator löste eine Race-Condition aus: GCC versuchte Dependency-Dateien (`.o.d`) in Unterverzeichnisse zu schreiben, die bei hohem Parallelismus noch nicht angelegt waren:
```
fatal error: opening dependency file CMakeFiles/opencv_calib3d.dir/src/dls.cpp.o.d: Datei oder Verzeichnis nicht gefunden
```

**Fix:**
`-G Ninja` zu den cmake-Konfigurationsargumenten hinzugefügt. Ninja erstellt den Build-Graphen korrekt vor dem parallelen Kompilieren.

```bash
local cmake_args=(
    -G Ninja                        # NEU
    -DCMAKE_BUILD_TYPE=Release
    -DCMAKE_INSTALL_PREFIX="$OPENCV_INSTALL_PREFIX"
    -DOPENCV_GENERATE_PKGCONFIG=ON
    -DBUILD_TESTS=OFF               # NEU (siehe Fix 2)
    -DBUILD_PERF_TESTS=OFF          # NEU (siehe Fix 2)
)
```

---

## Fix 2: OpenCV-Tests deaktivieren

**Datei:** `build-and-install-pins-x64.sh`
**Funktion:** `build_and_install_opencv_required_version()`

**Problem:**
Das OpenCV-Test-Binary `opencv_test_text` konnte nicht gelinkt werden:
```
/usr/bin/ld: /lib/x86_64-linux-gnu/libtesseract.so.5: undefined reference to `curl_easy_perform@CURL_OPENSSL_4'
```
Das System-`libtesseract` wurde gegen `libcurl-openssl` kompiliert (erwartet `CURL_OPENSSL_4`), aber eine andere curl-Variante ist installiert. Test-Binaries werden für den NINA-Betrieb nicht benötigt.

**Fix:**
`-DBUILD_TESTS=OFF -DBUILD_PERF_TESTS=OFF` zu den cmake-Argumenten hinzugefügt. Das verkürzt die Build-Zeit zusätzlich um ca. 30 % (1375 statt 2176 Dateien).

---

## Fix 3: Ninja-Generator für LibXISF-Build

**Datei:** `build-and-install-pins-x64.sh`
**Funktion:** `build_and_install_libxisf()`

**Problem:** Gleiche Race-Condition wie bei OpenCV (Fix 1).

**Fix:**
`-G Ninja` zum cmake-Konfigurationsaufruf für LibXISF hinzugefügt:
```bash
cmake -S "$LIBXISF_WORKDIR" -B "$LIBXISF_WORKDIR/build" \
    -G Ninja \                      # NEU
    -DCMAKE_BUILD_TYPE=Release \
    ...
```

---

## Fix 4: libindi.pc nach INDI-cmake-Install erstellen

**Datei:** `build-and-install-pins-x64.sh`
**Funktion:** `build_phd2_package()`

**Problem:**
INDI wird für den PHD2-Build aus dem Quellcode per cmake in `/usr` installiert. Der cmake-Build von INDI generiert **keine** pkg-config-Datei. PHD2s `FindINDI.cmake` sucht jedoch per `pkg_check_modules(LIBINDI libindi)` nach der Bibliothek und scheiterte:
```
-- Checking for module 'libindi'
--   Package 'libindi', required by 'virtual:world', not found
CMake Error at cmake_modules/FindINDI.cmake:136
```

**Fix:**
Nach dem cmake-Install wird eine `libindi.pc`-Datei manuell erstellt:
```bash
local indi_multiarch
indi_multiarch="$(dpkg-architecture -qDEB_HOST_MULTIARCH 2>/dev/null || echo "x86_64-linux-gnu")"
run_as_root bash -c "mkdir -p /usr/lib/${indi_multiarch}/pkgconfig && cat > /usr/lib/${indi_multiarch}/pkgconfig/libindi.pc <<'PKGEOF'
prefix=/usr
exec_prefix=${prefix}
libdir=${exec_prefix}/lib/${indi_multiarch}
includedir=${prefix}/include/libindi

Name: libindi
Description: Instrument-Neutral Distributed Interface
Version: ${PHD2_INDI_VERSION}
Libs: -L${libdir} -lindidriver -lindiclient
Cflags: -I${includedir}
PKGEOF"
```

---

## Fix 5: Distro-INDI-Pakete vor Installation entfernen

**Datei:** `build-and-install-pins-x64.sh`
**Funktion:** `install_built_packages()`

**Problem:**
Das System hatte Ubuntu-INDI-Pakete (Version 1.9.9) installiert, die Dateien besitzen, die auch in unserem gebauten `libindi1 2.1.9`-Paket enthalten sind. dpkg verweigerte die Installation:
```
Versuch, '/usr/lib/x86_64-linux-gnu/indi/MathPlugins/libindi_Nearest_MathPlugin.so' zu überschreiben,
welches auch in Paket libindi-plugins:amd64 1.9.9+dfsg-3build3 ist
```

**Fix:**
Vor der dpkg-Installation werden die kollidierenden Ubuntu-INDI-Pakete entfernt, und `--force-overwrite` wird für den dpkg-Install-Aufruf gesetzt:
```bash
# Konfliktierende Distro-Pakete entfernen
run_as_root apt-get remove -y --allow-remove-essential \
    libindi-plugins libindialignmentdriver1 libindiclient1 libindidriver1 libindilx200-1 \
    indi-bin libindi-dev libindi1 libindi-data 2>/dev/null || true

run_as_root dpkg -i --force-overwrite "${unique_debs[@]}" || true
run_as_root apt-get -f install -y
run_as_root dpkg -i --force-overwrite "${unique_debs[@]}"
```

---

## ASTAP D50-Sternkatalog

**Nicht im Skript — manuelle Installation:**

Der D50-Sternkatalog (827 MB, 1476 Zonen-Dateien) wurde heruntergeladen und installiert:

```bash
wget -O /tmp/d50_star_database.deb \
    "https://sourceforge.net/projects/astap-program/files/star_databases/d50_star_database.deb/download"
sudo dpkg -i /tmp/d50_star_database.deb
# Dateien von /opt/astap/ in den ASTAP-Standardpfad verschieben:
sudo mv /opt/astap/d50_*.1476 /usr/share/astap/data/
```

**Installationspfad:** `/usr/share/astap/data/` (1477 Dateien)

---

## Build-Ergebnis

Erfolgreicher Build (exit code 0) mit folgenden Paketen:

| Paket | Version |
|-------|---------|
| PINS Core | 3.3.0.1036-nightly+1778172314 |
| OpenCV | 4.11.0 (aus Quellcode) |
| LibXISF | aus Quellcode (mit ZSTD) |
| PHD2 | 2.6.14 |
| INDI | 2.1.9 |
| ASTAP D50 | 1.0 |

Commit: `3ec0d2ae8` auf Branch `develop` (gepusht nach `ggtux/pins_Mint`)
Pull Request: https://github.com/acocalypso/pins/pull/8

---

## INDI-Gerätekonfiguration

**Datei:** `/home/geo/.local/share/NINA/Profiles/adfd1406-d294-4c9b-a7df-8589b7646b1d.profile`

NINA/PINS startet `indiserver` im FIFO-Modus (`/tmp/indiFIFO`) und lädt Treiber on-demand.
Der externe `indiserver.service` wurde deaktiviert, um Port-Konflikt auf 7624 zu vermeiden:

```bash
sudo systemctl stop indiserver.service
sudo systemctl disable indiserver.service
```

Die konfigurierten Geräte im aktiven Profil (`IndiDriver`-Feld in den jeweiligen Settings):

| Gerät | NINA-Einstellung | INDI-Treiber | Executable |
|-------|-----------------|--------------|------------|
| Mount | `TelescopeSettings.IndiDriver` | `EQMod Mount` | `indi_eqmod_telescope` |
| Focuser | `FocuserSettings.IndiDriver` | `ZWO EAF` | `indi_asi_focuser` |
| Filter Wheel | `FilterWheelSettings.IndiDriver` | `ZWO EFW` | `indi_asi_wheel` |
| Rotator | `RotatorSettings.IndiDriver` | `Wanderer Rotator Mini` | `indi_wanderer_rotator_mini` |
| Switch | `SwitchSettings.IndiDriver` | `Pegasus PPBA` | `indi_pegasus_ppba` |

**Hinweise zu nicht-INDI-Geräten:**

- **Kamera (ZWO ASI):** NINA unterstützt keine INDI-Kamera. Es wird die native `libASICamera2.so` SDK verwendet. Die Kamera erscheint automatisch als `ZWO ASI [Modell]` sobald sie per USB verbunden ist.
- **Wanderer Mini V2:** War zunächst falsch als PowerBox eingetragen — ist ein Rotator (`Wanderer Rotator Mini`). Das Gerät wird per INDI gesteuert.

**Wichtig:** Der `IndiDriver`-Wert muss dem `label`-Attribut aus der INDI XML-Datenbank (`/usr/share/indi/*.xml`) entsprechen, **nicht** dem Binär-Namen.

---

## Touch-N-Stars Frontend-Update (neuere Version)

**Nicht im Skript — manuelle Installation:**

Das installierte Touch-N-Stars-Plugin lädt sein Web-Frontend aus dem Verzeichnis
`~/.local/share/NINA/Plugins/3.0.0/Touch N Stars/app/`
(NINA lädt Plugins kompatibel mit API-Version 3.0.0 aus dem `3.0.0`-Unterordner, **nicht** `3.3.0`).

Das mitgelieferte Frontend (1.2.7.0) fehlt den blauen Profil-Management-Button und den INDI-Setup-Button.
Der neuere Build aus dem Touch-N-Stars-Submodule (`NINA.Plugins/Touch-N-Stars/Touch-N-Stars/app/`) enthält diese Features und wurde manuell installiert:

```bash
SRC="$HOME/pins-build-src/NINA.Plugins/Touch-N-Stars/Touch-N-Stars/app"
DEST="$HOME/.local/share/NINA/Plugins/3.0.0/Touch N Stars/app"

cp -r "$SRC/." "$DEST/"
rm -f "$DEST/js/app.5e7917e6.js" "$DEST/js/app.5e7917e6.js.map"
rm -f "$DEST/css/app.e1dc5079.css"
```

Danach im Browser **Ctrl+Shift+R** (Hard Reload) ausführen.

---

## Touch-N-Stars INDI Treiberliste (3rdparty.json)

**Nicht im Skript — manuelle Installation:**

Der TNS INDI-Setup-Dialog liest Treiber aus eingebetteten JSON-Ressourcen der DLL **plus** einer benutzerbefüllbaren Datei `3rdparty.json`. Auf Linux (deutsches Locale) liegt diese unter:

```
~/Dokumente/INDI/3rdparty.json
```

Die eingebettete Liste enthält viele gängige Treiber, aber **nicht** EQMod Mount, ZWO EAF/EFW, Wanderer Rotator Mini oder Pegasus PPBA. Diese werden über `3rdparty.json` nachgetragen.

Die vollständige Liste wurde aus `/usr/share/indi/*.xml` (INDI 2.1.9) generiert und liegt im Repo unter `config/indi-3rdparty.json`. Installation:

```bash
INDI_DIR="$HOME/Dokumente/INDI"   # Linux DE; EN-Locale: ~/Documents/INDI
mkdir -p "$INDI_DIR"
cp "$HOME/pins-build-src/config/indi-3rdparty.json" "$INDI_DIR/3rdparty.json"
```

Enthält (aus `/usr/share/indi/*.xml` generiert, INDI 2.1.9):

| Kategorie | Treiber |
|-----------|---------|
| telescope | 52 (inkl. EQMod Mount) |
| focuser | 68 (inkl. ZWO EAF) |
| filterwheel | 32 (inkl. ZWO EFW) |
| rotator | 14 (inkl. Wanderer Rotator Mini) |
| switches | 60 (inkl. Pegasus PPBA) |
| dome | 19 |
| weather | 18 |
