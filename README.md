# PINS – PI 'N' Stars · Mint 22 / Ubuntu 24.04 Fork (amd64)

[![License: MPL 2.0](https://img.shields.io/badge/License-MPL%202.0-brightgreen.svg)](https://www.mozilla.org/en-US/MPL/2.0/)
[![PINS](https://img.shields.io/badge/PINS-3.3.0--nightly-blue)](https://github.com/acocalypso/pins)
[![INDI](https://img.shields.io/badge/INDI-2.1.9-orange)](https://indilib.org)
[![.NET](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com)

Dieser Fork bringt **PINS (PI 'N' Stars)** – den Linux-Port von [N.I.N.A.](https://github.com/Isbeorn/N.I.N.A.) – auf **Linux Mint 22 und Ubuntu 24.04 LTS (Noble) x86-64**.

Das Upstream-Projekt [acocalypso/pins](https://github.com/acocalypso/pins) zielt primär auf Raspberry Pi (ARM64) ab.
Dieser Fork enthält alle nötigen Fixes, ein vollständiges Build-Skript und ein vorgebautes INDI-Bundle für den Einsatz auf handelsüblichen x86-64-Rechnern.

---

## Was ist enthalten?

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

---

## Was unterscheidet diesen Fork?

- **Zielplattform:** x86-64 (amd64), Mint 22 / Ubuntu 24.04 LTS — kein Raspberry Pi
- **Vorgebautes INDI-Bundle:** `artifacts/indi-bundle/indi-pins-bundle_*.deb` (109 MB, via git-lfs) mit  
  indiserver · alle Core- und 3rd-Party-Treiber · ZWO-ASI/EFW/QHY-SDKs · udev-Regeln —  
  kein INDI-Quellcode-Build auf dem Zielrechner nötig
- **Build-Fixes für Mint 22:** Ninja-Generator für OpenCV/LibXISF, .NET-Paketfeed-URL hardcodiert auf Ubuntu 23.04, automatisches Entfernen kollidierender Distro-INDI-Pakete
- **Vollautomatisches Build-Skript:** `build-and-install-pins-x64.sh` baut und installiert alles als `.deb`-Pakete inkl. systemd-Service
- **Deutsche Installationsdokumentation**

---

## Schnellstart

### Voraussetzungen

- Linux Mint 22 oder Ubuntu 24.04 LTS, x86-64
- Internetverbindung, ≥ 8 GB RAM, ≥ 30 GB freier Speicher

### 1 – Repo klonen

```bash
sudo apt-get install -y git git-lfs
git lfs install
git clone --recurse-submodules https://github.com/ggtux/pins_Mint.git ~/pins-build-src
cd ~/pins-build-src
git lfs pull          # lädt das vorgebaute INDI-Bundle (109 MB) herunter
```

### 2a – INDI-Bundle vorab installieren (empfohlen)

Spart ~30 Minuten Build-Zeit. Vor dem Build-Skript ausführen:

```bash
# Eventuell vorhandene Distro-INDI-Pakete entfernen
sudo apt-get remove -y libindi-plugins libindialignmentdriver1 libindidriver1 \
  libindilx200-1 indi-bin libindi1 libindi-data libindi-dev 2>/dev/null || true

# Bundle installieren (enthält indiserver + alle Treiber + Vendor-SDKs)
sudo dpkg -i artifacts/indi-bundle/indi-pins-bundle_*.deb
sudo apt-get install -f -y
```

### 2b – Vollständiger Build

```bash
cd ~/pins-build-src
bash build-and-install-pins-x64.sh
```

Das Skript installiert alle Build-Abhängigkeiten, kompiliert und richtet alles als `.deb`-Pakete ein.
Dauer: ca. 60–90 min (abhängig von CPU; mit INDI-Bundle aus Schritt 2a kürzer).

### 3 – Post-Install-Setup (einmalig)

```bash
bash post-install-setup.sh
```

Deaktiviert den System-`indiserver`-Service (der PINS' eigenen blockieren würde),
installiert die Touch-N-Stars Frontend-Version und die INDI-Treiberliste.

---

## Erreichbare Webinterfaces nach der Installation

| Interface | URL | Beschreibung |
|---|---|---|
| **Touch-N-Stars** | `http://<hostname>:5000` | Hauptoberfläche: Sequencer, Equipment, Fokus |
| **ninaAPI** | `http://<hostname>:1889/v2/api/version` | REST-API für externe Steuerung |

---

## PINS-Service verwalten

```bash
systemctl status pins.service        # Status
sudo systemctl restart pins.service  # Neustart
journalctl -u pins.service -f        # Live-Log
```

---

## INDI-Bundle neu erstellen

Das Bundle wird mit dem gleichen Build-Skript auf Astromint erzeugt
und dann per git-lfs im Repo abgelegt. Um es auf einem anderen Stand neu zu bauen:

```bash
CREATE_INDI_BUNDLE=true bash build-and-install-pins-x64.sh
```

Das erzeugt `artifacts/indi-bundle/indi-pins-bundle_<version>_amd64.deb` als Schnappschuss
der aktuell installierten INDI-Umgebung (Core + alle 3rd-Party-Treiber + Vendor-SDKs).

---

## Detaillierte Installationsanleitung

→ [`INSTALL-Mint22.md`](INSTALL-Mint22.md)

Enthält: Systemvoraussetzungen · vollständige Schritt-für-Schritt-Anleitung ·
ASTAP Plate-Solving · INDI-Gerätekonfiguration über Touch-N-Stars · bekannte Probleme und Fixes.

---

## Bekannte Grenzen

- Nur **x86-64** getestet (kein ARM64 / Raspberry Pi in diesem Fork)
- Das INDI-Bundle ist ein Snapshot von **Linux Mint 22.3** — auf anderen Distros evtl. Bibliotheks-Inkompatibilitäten möglich
- Nightly-Build: kein Stabilitätsversprechen

---

## Haftungsausschluss

Dieses Repository ist ein **privater Fork** ohne Verbindung zu den Entwicklern von N.I.N.A. oder PINS.

Die Software wird **ohne jegliche Gewährleistung** bereitgestellt. Weder Funktion noch Stabilität werden garantiert. Die Nutzung erfolgt auf eigenes Risiko.

Insbesondere:
- Fehler in der Anwendung können zu **Datenverlust, Fehlfunktionen angeschlossener Geräte oder Beschädigung von Ausrüstung** führen
- Das INDI-Bundle ist ein Snapshot einer spezifischen Systemkonfiguration (Astromint / Mint 22.3) — Kompatibilität auf anderen Systemen ist nicht garantiert
- Nightly-Builds sind instabil und können sich jederzeit ändern

Für den produktiven Einsatz mit wertvoller Ausrüstung wird empfohlen, Funktionen zunächst in einer sicheren Testumgebung zu verifizieren.

---

## Upstream / Credits

| Projekt | Link |
|---|---|
| N.I.N.A. (Original) | [Isbeorn/N.I.N.A.](https://github.com/Isbeorn/N.I.N.A.) |
| PINS Upstream | [acocalypso/pins](https://github.com/acocalypso/pins) |
| INDI Library | [indilib/indi](https://github.com/indilib/indi) |
| Touch-N-Stars | [Touch-N-Stars/Touch-N-Stars](https://github.com/Touch-N-Stars/Touch-N-Stars) |
| PHD2 (Fork) | [acocalypso/phd2](https://github.com/acocalypso/phd2) |

Lizenz: [Mozilla Public License 2.0](LICENSE.txt)
