# PI.N.S. (PI 'N' Stars)

[![License: MPL 2.0](https://img.shields.io/badge/License-MPL%202.0-brightgreen.svg)](https://www.mozilla.org/en-US/MPL/2.0/)

This repository contains the source code for **PI.N.S. (PI 'N' Stars)**, a Linux port and fork of the original [N.I.N.A. - Nighttime Imaging 'N' Astronomy](https://github.com/Isbeorn/N.I.N.A.) imaging software.

PI.N.S. aims to bring the powerful features of N.I.N.A. to Linux users.

## Combined unstable packages

Run **Build PINS and ninaAPI unstable packages** from the GitHub Actions tab to
build Debian Trixie ARM64 packages from `nitr57/pins@unstable` and
`nitr57/ninaAPI@unstable`. The workflow builds ninaAPI against the same PINS
source and publishes one GitHub prerelease in the repository running the workflow.

Each prerelease contains `pins` and `pins-plugin-ninaapi` Debian packages and
their SHA-256 checksums. The release description records both source commit IDs.
Versions include `~unstable` and a unique run identifier; ninaAPI requires the
exact PINS package version from that release. Install both downloaded packages
together with `sudo apt install ./pins_*.deb ./pins-plugin-ninaapi_*.deb`.

The workflow is started manually and publishes only after both packages pass
dependency validation and a combined APT installation dry run. It is defined in
[build-unstable-packages.yml](.github/workflows/build-unstable-packages.yml).

---

## 🧭 About

**PI.N.S. (PI 'N' Stars)** is a modular astrophotography suite for Linux, designed to simplify and streamline image acquisition. It is based on the N.I.N.A. project, with modifications and improvements for Linux compatibility.

**Project Goal:** Make PI.N.S. run efficiently on a Raspberry Pi, enabling affordable and portable astrophotography setups.

This fork is not affiliated with the original N.I.N.A. authors. Please see [N.I.N.A.](https://github.com/Isbeorn/N.I.N.A.) for the official Windows version.

---

## ✅ Already Tested Devices

PI.N.S. supports a wide range of astronomy equipment. Here is a list of tested devices:

### 📷 **Cameras**
- QHY (some users report timing issues)
- ToupTek
- ZWO

### 🔍 **Filter Wheels**
- Astroasis
- ToupTek
- ZWO

### 🎯 **Focusers**
- Astroasis
- Gemini (indi\_myfocuserpro2\_focus)
- Nitecrawler
- QHY
- ToupTek
- ZWO

### 🔄 **Rotators**
- Nitecrawler
- Wanderer

### 🔭 **Mounts**
- 10Micron
- iOptron
- OnStep
- SkyWatcher
- ZWO

### 🌟 **Flat Panels**
- Gemini
- Wanderer

### 🔌 **Switches**
- Svbony

### 🛠️ **Other Accessories**
- ToupTek-a-like
- Wanderer ETA
- ZWO Seestar (Alpaca)

---

## 💬 Community & Support

Questions or need help? Join the [Touch-N-Stars Discord](https://discord.com/invite/4gZJEMWFcN) — the frontend used by PI.N.S. — for support and discussion.

[![](https://dcbadge.limes.pink/api/server/4gZJEMWFcN)](https://discord.com/invite/4gZJEMWFcN)

---

## 🤝 Contributing

Contributions, bug reports, and feature requests are welcome! Please open an issue or pull request on GitHub.

---

## ⚖ License

This project is licensed under the **Mozilla Public License 2.0 (MPL 2.0)**.
See the [`LICENSE.txt`](./LICENSE.txt) file for details.

If you use or modify this code, you must comply with the terms of the MPL. For more information, see the [Mozilla Public License 2.0 FAQ](https://www.mozilla.org/en-US/MPL/2.0/FAQ/).

---

## 💖 Early Supporters

We are grateful to the following companies for their early support of the PI.N.S. project:

[![ToupTek Astro](docs/sponsors/touptekastro.jpg)](https://www.touptekastro.com/)

[**ToupTek Astro**](https://www.touptekastro.com/) is a dedicated brand crafting astrophotography equipment for stargazers. From beginners to pros, ToupTek Astro provides the tools to capture the cosmos.

---

## 🙏 Attribution

PI.N.S. is a fork of [N.I.N.A. - Nighttime Imaging 'N' Astronomy](https://github.com/Isbeorn/N.I.N.A.), copyright (c) Stefan Berg.
All original credits and license terms are retained.
We welcome all kinds of contributions — from small fixes to large feature proposals.

---
