# PINS auf Linux Mint 22 (Ubuntu 24.04) installieren

Diese Anleitung beschreibt die vollständige Installation von PINS (PI 'N' Stars – Linux-Port von N.I.N.A.)
auf einem frischen Linux Mint 22 x64-System, inklusive aller Abhängigkeiten.

> **Schnellinstallation mit vorgebautem INDI-Bundle?**
> Wenn du das Repo von einem Astromint-Snapshot geklont hast, der eine
> `artifacts/indi-bundle/indi-pins-bundle_*.deb` enthält, kannst du INDI und alle
> 3rd-Party-Treiber sofort installieren – ohne selbst zu kompilieren.
> Springe direkt zu [Schritt 1b – Vorgebautes INDI-Bundle installieren](#schritt-1b--vorgebautes-indi-bundle-installieren).
> Für einen vollständigen Neubau aller Komponenten starte bei **Schritt 1**.

---

## Systemvoraussetzungen

| | Minimum |
|---|---|
| **OS** | Linux Mint 22 / Ubuntu 24.04 LTS (Noble) x64 |
| **CPU** | x86-64, ≥ 4 Kerne empfohlen (OpenCV-Kompilierung) |
| **RAM** | ≥ 8 GB (16 GB empfohlen) |
| **Disk** | ≥ 30 GB frei (Build-Artefakte, OpenCV-Sourcen) |
| **Netz** | Internetverbindung während des Builds |
| **Zeit** | ca. 60–120 min (je nach CPU, SSD) |

---

## Schritt 1 – Repo klonen

```bash
sudo apt-get install -y git git-lfs
git lfs install
git clone --recurse-submodules https://github.com/ggtux/pins_Mint.git ~/pins-build-src
cd ~/pins-build-src
git lfs pull   # lädt große Binärartefakte (INDI-Bundle, External-DLLs) herunter
```

> Dieser Fork enthält alle Mint-22-spezifischen Fixes (Ninja-Generator, INDI-Konflikt, PHD2-pkg-config u. a.)
> sowie die vorbereitete INDI-Treiberliste, das Post-Install-Script und das vorgebaute INDI-Bundle.
> Das Upstream-Repo ist `https://github.com/acocalypso/pins.git` (als `upstream` remote hinterlegt):
> ```bash
> git remote add upstream https://github.com/acocalypso/pins.git
> ```

---

## Schritt 1b – Vorgebautes INDI-Bundle installieren

Das Repo enthält ein vorgebautes `indi-pins-bundle_*.deb` (via git-lfs) mit:
- `indiserver` + alle INDI-Core-Treiber (v2.1.9)
- Alle `indi-3rdparty`-Treiber (~200 Binaries, inkl. EQMod, ASI, QHY, SVBony u. a.)
- Vendor-SDKs: `libASICamera2`, `libEFWFilter`, `libUSB2ST4Conv`, `libqhyccd`
- INDI-XML-Datendateien und udev-Regeln

**Diesen Schritt ausführen, bevor `build-and-install-pins-x64.sh` gestartet wird:**

```bash
cd ~/pins-build-src

# Laufzeit-Abhängigkeiten des Bundles installieren
sudo apt-get install -y libusb-1.0-0 libstdc++6 libc6

# Eventuell vorhandene Distro-INDI-Pakete zuerst entfernen
sudo apt-get remove -y libindi-plugins libindialignmentdriver1 libindidriver1 \
  libindilx200-1 indi-bin libindi1 libindi-data libindi-dev 2>/dev/null || true

# Bundle installieren (ersetzt alle Einzel-INDI-Pakete)
sudo dpkg -i artifacts/indi-bundle/indi-pins-bundle_*.deb

# Fehlende Laufzeit-Deps nachholen (falls dpkg Fehler meldet)
sudo apt-get install -f -y
```

> Nach dieser Installation kann der vollständige Build in **Schritt 2** mit
> `PHD2_BUILD_INDI_3RDPARTY=false` gestartet werden – die 3rd-Party-Treiber sind
> bereits installiert und müssen nicht neu gebaut werden.

---

## Schritt 2 – Build und Installation (vollautomatisch)

Das Skript installiert alle Abhängigkeiten, kompiliert und installiert alles als `.deb`-Pakete:

```bash
cd ~/pins-build-src
bash build-and-install-pins-x64.sh
```

Das Skript erledigt automatisch:

| Schritt | Details |
|---|---|
| **System-Build-Deps** | `build-essential`, `cmake`, `ninja-build`, `libboost-all-dev`, `libcfitsio-dev`, `libnova-dev`, `libgphoto2-dev`, u. v. m. |
| **.NET 10 SDK** | Über Microsoft-Repository |
| **Node.js 22** | Über NodeSource |
| **OpenCV 4.11.0** | Aus Quellcode (Ninja, `-DBUILD_TESTS=OFF -DBUILD_PERF_TESTS=OFF`) |
| **LibXISF** | Aus Quellcode (mit ZSTD) |
| **INDI 2.1.9** | Aus Quellcode als `.deb` |
| **indi-3rdparty** | EQMod, ASI, ZWO, Pegasus, Wanderer u. a. |
| **PHD2** | Fork mit PINS-Erweiterungen |
| **PINS Core** | .NET 10, als `.deb` mit systemd-Service |
| **Alle Plugins** | ninaAPI, Touch-N-Stars, LiveStack, PolarAlignment u. a. |

> **Hinweis:** Kollidiert der Build mit bereits installierten Distro-INDI-Paketen, entfernt das Skript
> diese automatisch (`libindi-plugins`, `libindi1` etc.) und installiert die selbst gebauten Versionen.

---

## Schritt 3 – Post-Install-Setup (einmalig manuell)

Nach dem Build-Skript sind noch einige manuelle Schritte nötig. Das beigelegte Script
`post-install-setup.sh` erledigt sie automatisch:

```bash
cd ~/pins-build-src
bash post-install-setup.sh
```

Was das Script macht:

1. **`indiserver.service` deaktivieren** – verhindert Port-7624-Konflikt mit PINS' eigenem indiserver
2. **Touch-N-Stars Frontend aktualisieren** – neuere Version aus dem Submodule in den Pluginordner (`3.0.0`) kopieren
3. **INDI Treiberliste** (`3rdparty.json`) – aus `config/indi-3rdparty.json` in `~/Dokumente/INDI/` (DE) bzw. `~/Documents/INDI/` (EN) installieren

---

## Schritt 4 – ASTAP Sternkatalog (optional, für Plate-Solving)

```bash
# ASTAP CLI
sudo apt-get install -y astap

# D50-Katalog (827 MB)
wget -O /tmp/d50.deb \
  "https://sourceforge.net/projects/astap-program/files/star_databases/d50_star_database.deb/download"
sudo dpkg -i /tmp/d50.deb
sudo mv /opt/astap/d50_*.1476 /usr/share/astap/data/ 2>/dev/null || true
```

---

## Schritt 5 – NINA-Profil konfigurieren

PINS startet beim ersten Lauf mit einem leeren Profil. Konfiguriere deine Geräte
über das **Touch-N-Stars Webinterface** (`http://<hostname>:5000`):

### INDI-Treiber einstellen

1. Browser: `http://localhost:5000` öffnen
2. **Equipment Control** → **INDI Setup** Button klicken
3. Für jedes Gerät den passenden INDI-Treiber aus der Dropdown-Liste wählen
4. **Save** – die Einstellungen werden direkt ins NINA-Profil geschrieben

Typische Treiber:

| Gerät | INDI-Treiber |
|---|---|
| EQ6-R / HEQ5 (EQMod) | `EQMod Mount` |
| ZWO Fokussierer EAF | `ZWO EAF` |
| ZWO Filterrad EFW | `ZWO EFW` |
| Wanderer Rotator Mini | `Wanderer Rotator Mini` |
| Pegasus PPBA | `Pegasus PPBA` |

### Kamera (ZWO ASI)

ZWO ASI-Kameras werden **nicht** über INDI gesteuert, sondern über das native SDK (`libASICamera2.so`).
Die Kamera erscheint automatisch als `ZWO ASI [Modell]` sobald sie per USB verbunden ist.

### indiserver – wichtiger Hinweis

NINA/PINS startet `indiserver` selbst im FIFO-Modus (`/tmp/indiFIFO`).
Ein parallel laufender System-`indiserver` würde den Port 7624 blockieren.
`post-install-setup.sh` deaktiviert ihn bereits; prüfen mit:

```bash
systemctl is-enabled indiserver.service 2>/dev/null || echo "nicht vorhanden"
```

---

## Schritt 6 – PINS-Service prüfen

```bash
# Status
systemctl status pins.service

# Logs
journalctl -u pins.service -f

# Neustart
sudo systemctl restart pins.service
```

Touch-N-Stars erreichbar unter: `http://localhost:5000`
ninaAPI erreichbar unter: `http://localhost:1889/v2/api/version`

---

## Vollständige Paket-Übersicht nach Installation

| Paket | Version | Quelle |
|---|---|---|
| PINS Core | 3.3.0.x | Aus Quellcode |
| OpenCV | 4.11.0 | Aus Quellcode |
| LibXISF | aktuell | Aus Quellcode |
| PHD2 | 2.6.14 | Aus Quellcode |
| **indi-pins-bundle** | **2.1.9** | **Vorgebaut (git-lfs) oder Aus Quellcode** |
| – indiserver + Core-Treiber | 2.1.9 | (enthalten im Bundle) |
| – indi-3rdparty (~200 Treiber) | 2.1.9 | (enthalten im Bundle) |
| – Vendor-SDKs (ASI, EFW, QHY) | herstellerspezifisch | (enthalten im Bundle) |
| ninaAPI Plugin | 2.2.x | Submodule |
| Touch-N-Stars Plugin | 1.2.x | Submodule |
| ASTAP | aktuell | APT |
| ASTAP D50-Katalog | 1.0 | Manuell |

---

## Bekannte Probleme und Fixes

Siehe [`BUILD-FIXES.md`](BUILD-FIXES.md) für detaillierte Dokumentation aller angewandten Fixes:

- **Fix 1–3:** Ninja-Generator für OpenCV/LibXISF (Race-Condition bei parallelem Build)
- **Fix 4:** `libindi.pc` manuell erstellt (PHD2 pkg-config)
- **Fix 5:** Distro-INDI-Pakete vor Installation entfernen (Dateikonflikt)
- **Fix 6:** `.NET`-Paketfeed-URL: Auf Mint 22 liefert `VERSION_ID` den Wert `22` statt der Ubuntu-Basis-Version. Im Skript ist die URL deshalb auf `23.04` fest kodiert (Zeilen 835–836).
- **TNS Frontend:** Korrektes Plugin-Verzeichnis ist `3.0.0`, nicht `3.3.0`
- **INDI Treiberliste:** `3rdparty.json` mit allen INDI 2.1.9 Treibern unter `config/indi-3rdparty.json`
