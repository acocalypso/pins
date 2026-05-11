# PINS – PI 'N' Stars · Mint 22 / Ubuntu 24.04 Fork (amd64)

[![License: MPL 2.0](https://img.shields.io/badge/License-MPL%202.0-brightgreen.svg)](https://www.mozilla.org/en-US/MPL/2.0/)
[![PINS](https://img.shields.io/badge/PINS-3.3.0--nightly-blue)](https://github.com/acocalypso/pins)
[![INDI](https://img.shields.io/badge/INDI-2.1.9-orange)](https://indilib.org)
[![.NET](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com)

**[Deutsch](#deutsch) | [English](#english)**

---

<a name="deutsch"></a>
## Deutsch

Dieser Fork bringt **PINS (PI 'N' Stars)** – den Linux-Port von [N.I.N.A.](https://github.com/Isbeorn/N.I.N.A.) – auf **Linux Mint 22 und Ubuntu 24.04 LTS (Noble) x86-64**.

Das Upstream-Projekt [acocalypso/pins](https://github.com/acocalypso/pins) zielt primär auf Raspberry Pi (ARM64) ab.
Dieser Fork enthält alle nötigen Fixes, ein vollständiges Build-Skript und ein vorgebautes INDI-Bundle für den Einsatz auf handelsüblichen x86-64-Rechnern.

### Was ist enthalten?

| Komponente | Version | Quelle |
|---|---|---|
| **PINS Core** | 3.3.0.1036-nightly | Dieser Fork |
| **INDI** (Core + alle 3rd-Party-Treiber) | 2.1.9 | Vorgebaut via git-lfs |
| **PHD2** | 2.6.14 | [acocalypso/phd2](https://github.com/acocalypso/phd2) |
| **OpenCV** | 4.11.0 | Aus Quellcode |
| **LibXISF** | aktuell | [joxda/libXISF](https://github.com/joxda/libXISF) |
| **ninaAPI Plugin** | 2.2.x | Submodul |
| **Touch-N-Stars** (Frontend + Plugin) | aktuell | Submodul |
| **LiveStack Plugin** | 1.0.x | Submodul |
| **PolarAlignment Plugin** | aktuell | Submodul |
| **Joko Plugins** | 3.0.x | Submodul |
| **TenMicron Plugin** | aktuell | Submodul |
| **Orbuculum Plugin** | aktuell | Submodul |
| **PHD2Tools Plugin** | aktuell | Submodul |
| **WandererETASDK** | aktuell | Submodul |

### Was unterscheidet diesen Fork?

- **Zielplattform:** x86-64 (amd64), Mint 22 / Ubuntu 24.04 LTS — kein Raspberry Pi
- **Vorgebautes INDI-Bundle:** `artifacts/indi-bundle/indi-pins-bundle_*.deb` (109 MB, via git-lfs) mit  
  indiserver · alle Core- und 3rd-Party-Treiber · ZWO-ASI/EFW/QHY-SDKs · udev-Regeln —  
  kein INDI-Quellcode-Build auf dem Zielrechner nötig
- **Build-Fixes für Mint 22:** Ninja-Generator für OpenCV/LibXISF, .NET-Paketfeed-URL hardcodiert auf Ubuntu 23.04, automatisches Entfernen kollidierender Distro-INDI-Pakete
- **Vollautomatisches Build-Skript:** `build-and-install-pins-x64.sh` baut und installiert alles als `.deb`-Pakete inkl. systemd-Service
- **Zweisprachige Dokumentation** (Deutsch / Englisch)

### Schnellstart

**Voraussetzungen:** Linux Mint 22 oder Ubuntu 24.04 LTS, x86-64 · ≥ 8 GB RAM · ≥ 30 GB freier Speicher

**1 – Repo klonen**

```bash
sudo apt-get install -y git git-lfs
git lfs install
git clone --recurse-submodules https://github.com/ggtux/pins_Mint.git ~/pins-build-src
cd ~/pins-build-src
git lfs pull          # lädt das vorgebaute INDI-Bundle (109 MB) herunter
```

**2a – INDI-Bundle vorab installieren (empfohlen, spart ~30 min Build-Zeit)**

```bash
sudo apt-get remove -y libindi-plugins libindialignmentdriver1 libindidriver1 \
  libindilx200-1 indi-bin libindi1 libindi-data libindi-dev 2>/dev/null || true
sudo dpkg -i artifacts/indi-bundle/indi-pins-bundle_*.deb
sudo apt-get install -f -y
```

**2b – Vollständiger Build** (ca. 60–90 min)

```bash
bash build-and-install-pins-x64.sh
```

**3 – Post-Install-Setup (einmalig)**

```bash
bash post-install-setup.sh
```

### Webinterfaces

| Interface | URL | Beschreibung |
|---|---|---|
| **Touch-N-Stars** | `http://<hostname>:5000` | Hauptoberfläche: Sequencer, Equipment, Fokus |
| **ninaAPI** | `http://<hostname>:1889/v2/api/version` | REST-API für externe Steuerung |

### PINS-Service

```bash
systemctl status pins.service        # Status
sudo systemctl restart pins.service  # Neustart
journalctl -u pins.service -f        # Live-Log
```

### Detaillierte Installationsanleitung

→ [`INSTALL-Mint22.md`](INSTALL-Mint22.md)

### Bekannte Grenzen

- Nur **x86-64** getestet (kein ARM64 / Raspberry Pi in diesem Fork)
- Das INDI-Bundle ist ein Snapshot von **Linux Mint 22.3** — auf anderen Distros evtl. Bibliotheks-Inkompatibilitäten möglich
- Nightly-Build: kein Stabilitätsversprechen

### Haftungsausschluss

Dieses Repository ist ein **privater Fork** ohne Verbindung zu den Entwicklern von N.I.N.A. oder PINS.

Die Software wird **ohne jegliche Gewährleistung** bereitgestellt. Weder Funktion noch Stabilität werden garantiert. Die Nutzung erfolgt auf eigenes Risiko.

Insbesondere:
- Fehler in der Anwendung können zu **Datenverlust, Fehlfunktionen angeschlossener Geräte oder Beschädigung von Ausrüstung** führen
- Das INDI-Bundle ist ein Snapshot einer spezifischen Systemkonfiguration (Astromint / Mint 22.3) — Kompatibilität auf anderen Systemen ist nicht garantiert
- Nightly-Builds sind instabil und können sich jederzeit ändern

Für den produktiven Einsatz mit wertvoller Ausrüstung wird empfohlen, Funktionen zunächst in einer sicheren Testumgebung zu verifizieren.

---

<a name="english"></a>
## English

This fork brings **PINS (PI 'N' Stars)** – the Linux port of [N.I.N.A.](https://github.com/Isbeorn/N.I.N.A.) – to **Linux Mint 22 and Ubuntu 24.04 LTS (Noble) x86-64**.

The upstream project [acocalypso/pins](https://github.com/acocalypso/pins) primarily targets Raspberry Pi (ARM64).
This fork includes all necessary build fixes, a fully automated build script, and a prebuilt INDI bundle for use on standard x86-64 desktop or mini-PC hardware.

### What's included?

| Component | Version | Source |
|---|---|---|
| **PINS Core** | 3.3.0.1036-nightly | This fork |
| **INDI** (Core + all 3rd-party drivers) | 2.1.9 | Prebuilt via git-lfs |
| **PHD2** | 2.6.14 | [acocalypso/phd2](https://github.com/acocalypso/phd2) |
| **OpenCV** | 4.11.0 | Built from source |
| **LibXISF** | current | [joxda/libXISF](https://github.com/joxda/libXISF) |
| **ninaAPI Plugin** | 2.2.x | Submodule |
| **Touch-N-Stars** (Frontend + Plugin) | current | Submodule |
| **LiveStack Plugin** | 1.0.x | Submodule |
| **PolarAlignment Plugin** | current | Submodule |
| **Joko Plugins** | 3.0.x | Submodule |
| **TenMicron Plugin** | current | Submodule |
| **Orbuculum Plugin** | current | Submodule |
| **PHD2Tools Plugin** | current | Submodule |
| **WandererETASDK** | current | Submodule |

### What makes this fork different?

- **Target platform:** x86-64 (amd64), Mint 22 / Ubuntu 24.04 LTS — not Raspberry Pi
- **Prebuilt INDI bundle:** `artifacts/indi-bundle/indi-pins-bundle_*.deb` (109 MB, via git-lfs) containing  
  indiserver · all core and 3rd-party drivers · ZWO ASI/EFW and QHY vendor SDKs · udev rules —  
  no INDI source build required on the target machine
- **Mint 22 build fixes:** Ninja generator for OpenCV/LibXISF, .NET package feed URL hardcoded to Ubuntu 23.04, automatic removal of conflicting distro INDI packages
- **Fully automated build script:** `build-and-install-pins-x64.sh` builds and installs everything as `.deb` packages including a systemd service
- **Bilingual documentation** (German / English)

### Quick start

**Requirements:** Linux Mint 22 or Ubuntu 24.04 LTS, x86-64 · ≥ 8 GB RAM · ≥ 30 GB free disk space

**1 – Clone the repo**

```bash
sudo apt-get install -y git git-lfs
git lfs install
git clone --recurse-submodules https://github.com/ggtux/pins_Mint.git ~/pins-build-src
cd ~/pins-build-src
git lfs pull          # downloads the prebuilt INDI bundle (109 MB)
```

**2a – Install the INDI bundle first (recommended, saves ~30 min build time)**

```bash
sudo apt-get remove -y libindi-plugins libindialignmentdriver1 libindidriver1 \
  libindilx200-1 indi-bin libindi1 libindi-data libindi-dev 2>/dev/null || true
sudo dpkg -i artifacts/indi-bundle/indi-pins-bundle_*.deb
sudo apt-get install -f -y
```

**2b – Full build** (~60–90 min depending on CPU)

```bash
bash build-and-install-pins-x64.sh
```

**3 – Post-install setup (once)**

```bash
bash post-install-setup.sh
```

Disables the system `indiserver` service (which would block PINS' own instance), installs the Touch-N-Stars frontend version and the INDI driver list.

### Web interfaces

| Interface | URL | Description |
|---|---|---|
| **Touch-N-Stars** | `http://<hostname>:5000` | Main UI: sequencer, equipment control, focus |
| **ninaAPI** | `http://<hostname>:1889/v2/api/version` | REST API for external control |

### Managing the PINS service

```bash
systemctl status pins.service        # status
sudo systemctl restart pins.service  # restart
journalctl -u pins.service -f        # live log
```

### Full installation guide

→ [`INSTALL-Mint22.md`](INSTALL-Mint22.md) *(German)*

### Known limitations

- Only tested on **x86-64** (no ARM64 / Raspberry Pi support in this fork)
- The INDI bundle is a snapshot of a **Linux Mint 22.3** system — library compatibility on other distros is not guaranteed
- Nightly build: no stability guarantee

### Disclaimer

This repository is a **private fork** with no affiliation to the developers of N.I.N.A. or PINS.

The software is provided **without any warranty**. Neither functionality nor stability is guaranteed. Use at your own risk.

In particular:
- Bugs in the application may lead to **data loss, malfunctioning connected devices, or damage to equipment**
- The INDI bundle is a snapshot of a specific system configuration (Astromint / Mint 22.3) — compatibility on other systems is not guaranteed
- Nightly builds are unstable and may change at any time

For production use with valuable equipment, it is strongly recommended to verify all functionality in a safe test environment first.

---

## Upstream / Credits

| Project | Link |
|---|---|
| N.I.N.A. (original) | [Isbeorn/N.I.N.A.](https://github.com/Isbeorn/N.I.N.A.) |
| PINS upstream | [acocalypso/pins](https://github.com/acocalypso/pins) |
| INDI Library | [indilib/indi](https://github.com/indilib/indi) |
| Touch-N-Stars | [Touch-N-Stars/Touch-N-Stars](https://github.com/Touch-N-Stars/Touch-N-Stars) |
| PHD2 (fork) | [acocalypso/phd2](https://github.com/acocalypso/phd2) |

License: [Mozilla Public License 2.0](LICENSE.txt)
