#!/usr/bin/env bash
# post-install-setup.sh – Manuelle Post-Install-Schritte nach build-and-install-pins-x64.sh
set -euo pipefail

log()  { echo "[INFO] $*"; }
warn() { echo "[WARN] $*" >&2; }

run_as_root() {
  if [[ "${EUID}" -eq 0 ]]; then "$@"
  elif command -v sudo >/dev/null 2>&1; then sudo "$@"
  else echo "[ERROR] sudo required" >&2; exit 1; fi
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET_USER="${TARGET_USER:-${SUDO_USER:-$USER}}"
TARGET_HOME="$(getent passwd "$TARGET_USER" | cut -d: -f6 || echo "/home/$TARGET_USER")"

# ── 1. indiserver.service deaktivieren ──────────────────────────────────────
log "Step 1: Disable system indiserver.service (port 7624 conflict)"
if systemctl list-unit-files indiserver.service &>/dev/null; then
  run_as_root systemctl stop    indiserver.service 2>/dev/null || true
  run_as_root systemctl disable indiserver.service 2>/dev/null || true
  log "  indiserver.service disabled"
else
  log "  indiserver.service not found – nothing to do"
fi

# ── 2. Touch-N-Stars Frontend aktualisieren ──────────────────────────────────
log "Step 2: Update Touch-N-Stars frontend (3.0.0 plugin directory)"

TNS_SRC="$SCRIPT_DIR/NINA.Plugins/Touch-N-Stars/Touch-N-Stars/app"
TNS_DEST="$TARGET_HOME/.local/share/NINA/Plugins/3.0.0/Touch N Stars/app"

if [[ ! -d "$TNS_SRC" ]]; then
  warn "  TNS source not found at $TNS_SRC – skipping frontend update"
elif [[ ! -d "$TNS_DEST" ]]; then
  warn "  TNS install dir not found at $TNS_DEST – PINS not installed yet? Skipping"
else
  # Backup existing index.html
  cp "$TNS_DEST/index.html" "$TNS_DEST/index.html.bak" 2>/dev/null || true

  # Copy new frontend
  cp -r "$TNS_SRC/." "$TNS_DEST/"

  # Remove old JS/CSS that is no longer referenced
  rm -f "$TNS_DEST/js/app.5e7917e6.js"   "$TNS_DEST/js/app.5e7917e6.js.map"
  rm -f "$TNS_DEST/css/app.e1dc5079.css"

  NEW_JS=$(grep -o 'app\.[a-f0-9]*\.js' "$TNS_DEST/index.html" | head -1)
  log "  Frontend updated – main bundle: $NEW_JS"
  log "  Browser: Ctrl+Shift+R (Hard Reload) nach dem ersten Öffnen"
fi

# ── 3. INDI 3rdparty.json installieren ──────────────────────────────────────
log "Step 3: Install INDI driver list (3rdparty.json)"

INDI_SRC="$SCRIPT_DIR/config/indi-3rdparty.json"

if [[ ! -f "$INDI_SRC" ]]; then
  warn "  $INDI_SRC not found – skipping"
else
  # Locale-aware: Mint DE = Dokumente, EN = Documents
  DOCS_DIR=""
  for candidate in \
    "$TARGET_HOME/Dokumente" \
    "$TARGET_HOME/Documents" \
    "$(xdg-user-dir DOCUMENTS 2>/dev/null || true)"
  do
    if [[ -d "$candidate" ]]; then
      DOCS_DIR="$candidate"
      break
    fi
  done

  # Fallback: create Documents
  if [[ -z "$DOCS_DIR" ]]; then
    DOCS_DIR="$TARGET_HOME/Documents"
    mkdir -p "$DOCS_DIR"
    log "  Created $DOCS_DIR"
  fi

  INDI_DIR="$DOCS_DIR/INDI"
  mkdir -p "$INDI_DIR"

  # Only install if 3rdparty.json doesn't exist yet or is the default template
  if [[ -f "$INDI_DIR/3rdparty.json" ]]; then
    # Check if it's the empty template (< 500 bytes)
    size=$(wc -c < "$INDI_DIR/3rdparty.json")
    if [[ "$size" -lt 500 ]]; then
      cp "$INDI_SRC" "$INDI_DIR/3rdparty.json"
      log "  Replaced empty template with full driver list: $INDI_DIR/3rdparty.json"
    else
      log "  $INDI_DIR/3rdparty.json already populated – not overwritten"
      log "  To force update: cp $INDI_SRC $INDI_DIR/3rdparty.json"
    fi
  else
    cp "$INDI_SRC" "$INDI_DIR/3rdparty.json"
    log "  Installed: $INDI_DIR/3rdparty.json"
  fi

  DRIVER_COUNT=$(python3 -c \
    "import json; d=json.load(open('$INDI_DIR/3rdparty.json')); print(sum(len(v) for v in d.values()))" \
    2>/dev/null || echo "?")
  log "  Total INDI drivers available: $DRIVER_COUNT"
fi

# ── 4. PINS-Service neu starten ──────────────────────────────────────────────
log "Step 4: Restart PINS service"
if systemctl list-unit-files pins.service &>/dev/null; then
  run_as_root systemctl restart pins.service 2>/dev/null || warn "  Could not restart pins.service"
  sleep 2
  STATUS=$(systemctl is-active pins.service 2>/dev/null || echo "unknown")
  log "  pins.service status: $STATUS"
else
  warn "  pins.service not found – was build-and-install-pins-x64.sh run first?"
fi

# ── Zusammenfassung ──────────────────────────────────────────────────────────
echo
echo "═══════════════════════════════════════════════════════"
echo " Post-install setup complete"
echo "═══════════════════════════════════════════════════════"
echo " Touch-N-Stars:  http://localhost:5000"
echo " ninaAPI:        http://localhost:1889/v2/api/version"
echo
echo " Nächste Schritte:"
echo "  1. Browser: http://localhost:5000 öffnen (Ctrl+Shift+R)"
echo "  2. Equipment Control → INDI Setup → Treiber konfigurieren"
echo "  3. ASTAP D50-Katalog installieren (siehe INSTALL-Mint22.md)"
echo "═══════════════════════════════════════════════════════"
