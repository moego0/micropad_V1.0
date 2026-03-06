# Micropad Web App (SenetLabs)

PWA for configuring the Micropad device. Offline-first, no backend required. Connect via **Web Bluetooth** (Chrome, Edge, Opera).

## Run locally

```bash
cd web-app
npm install
npm run dev
```

Open `https://localhost:5173` (or the URL Vite prints). Web Bluetooth requires a **secure context** (HTTPS or localhost).

## Connect to device

1. **Supported browsers**: Chrome, Edge, or Opera (desktop). Chrome on Android also works. Safari and Firefox do not support Web Bluetooth.
2. **HTTPS**: The app must be served over HTTPS (or localhost). Deploy to Cloudflare Pages or GitHub Pages for production.
3. **User gesture**: Click **Connect** on the Devices page. The browser will show a system dialog to pick the Micropad (it must be advertising or already paired).
4. **If the device doesn’t appear**: Remove it from the OS Bluetooth settings, power-cycle the Micropad, then try Connect again.

## Features

- **Devices**: Scan/connect via Web Bluetooth, connection status (Scan → Found → Pair → Ready), device info (firmware, battery).
- **Profiles**: Create, duplicate, delete, rename; 3×4 key grid; Layer 0/1/2; encoders (Volume, Scroll, Zoom, Media presets); combos; push/pull to device, set active profile.
- **Macros**: Create steps (delay, text, key), reorder, save to library; assign macros to keys (reference or embed).
- **Export / Import**: Single .zip or .json backup (profiles + macros). Use Settings → Export backup and Import from file.

## Deploy for $0/month

### Cloudflare Pages

1. Push the repo to GitHub.
2. In [Cloudflare Dashboard](https://dash.cloudflare.com) → Pages → Create project → Connect to Git.
3. Select the repo; set **Build command**: `cd web-app && npm run build`, **Build output directory**: `web-app/dist`.
4. Deploy. Your app will be at `https://<project>.pages.dev`. Use **HTTPS** so Web Bluetooth works.

### GitHub Pages

1. In repo **Settings → Pages**, set source to **GitHub Actions**.
2. The workflow `.github/workflows/deploy-web-app.yml` at repo root builds `web-app` and deploys to GitHub Pages.
3. After the workflow runs, the site will be at `https://<user>.github.io/<repo>/`. If the repo name is not `<user>.github.io`, set **base** in `web-app/vite.config.ts` to `/<repo>/` (e.g. `base: '/code_V1/'`).

## Project structure

```
src/
  device/          # Web Bluetooth (bleConnection) + JSON protocol (protocolHandler)
  storage/         # IndexedDB (profiles, macros, settings)
  services/        # profileSyncService, exportImport
  stores/          # Zustand: deviceStore, profilesStore, macrosStore
  models/          # TypeScript types
  schemas/         # Zod schemas + export bundle
  pages/           # Devices, Profiles, Macros, Settings, Presets
  components/     # ActionEditModal, etc.
  layouts/         # MainLayout (sidebar + footer)
```

## Protocol

Same as the Windows app: BLE GATT service `4fafc201-1fb5-459e-8fcc-c5c9c331914b`, CMD write `...914c`, EVT notify `...914d`. JSON envelope: `{ v, type, id, cmd, profileId?, profile?, payload? }`. Chunking for messages &gt; 512 bytes. See repo root `PROTOCOL_SPEC.md`.

## Tests

```bash
npm run test
```

Schema validation tests: `src/schemas/profileSchema.test.ts`.

## PWA icons

Add `public/icons/icon-192.png` and `public/icons/icon-512.png` for the app manifest, or the build will still work with default favicon.
