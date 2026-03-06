# Micropad Web App — Porting Plan

## 1. Mapping: Windows App → Web Modules

| Windows App Module | Web Module | Notes |
|-------------------|------------|--------|
| **Micropad.App (WPF)** | `src/App.tsx`, `src/pages/*`, `src/components/*` | React + Tailwind; no WPF |
| **MainWindow.xaml** | `src/layouts/MainLayout.tsx` | Sidebar nav + content + status bar |
| **DevicesView + DevicesViewModel** | `src/pages/DevicesPage.tsx` + `src/stores/deviceStore.ts` | Scan/Connect/Fix/Diagnostics |
| **ProfilesView + ProfilesViewModel** | `src/pages/ProfilesPage.tsx` + `src/stores/profilesStore.ts` | List, key grid, layers, encoders, combos |
| **MacrosView + MacrosViewModel** | `src/pages/MacrosPage.tsx` + `src/stores/macrosStore.ts` | Steps, record, tags, slot grid |
| **ActionEditWindow** | `src/components/ActionEditModal.tsx` | Key assignment dialog |
| **PresetsView** | `src/pages/PresetsPage.tsx` | Templates (optional in v1) |
| **SettingsView** | `src/pages/SettingsPage.tsx` | App settings (minimal in v1) |
| **BleConnection.cs** | `src/device/bleConnection.ts` | Web Bluetooth API; same GATT UUIDs |
| **ProtocolHandler.cs** | `src/device/protocolHandler.ts` | Same JSON envelope + chunking |
| **ProfileSyncService.cs** | `src/services/profileSyncService.ts` | Push/Pull/Caps/Active/Delete |
| **LocalProfileStorage.cs** | `src/storage/profileStorage.ts` | IndexedDB via idb |
| **LocalMacroStorage.cs** | `src/storage/macroStorage.ts` | IndexedDB |
| **AppSettings / SettingsStorage** | `src/storage/settingsStorage.ts` | IndexedDB |
| **Micropad.Core (Models)** | `src/models/*` + `src/schemas/*` | TypeScript types + Zod |
| **ProfileConflictWindow** | `src/components/ProfileConflictModal.tsx` | Local vs Device conflict |
| **ComboEditWindow** | `src/components/ComboEditModal.tsx` | Combo key picker |
| **RenameProfileWindow** | `src/components/RenameModal.tsx` | Inline or modal |
| **CommandPaletteService** | `src/components/CommandPalette.tsx` | Ctrl+K (optional) |
| **SetupJourneyService** | `src/components/SetupJourney.tsx` | Getting started steps |
| **HudService / TrayService** | Omitted in v1 (no OS overlay/tray in browser) | Optional: toast only |

## 2. Protocol Summary (Device Sync)

- **Transport**: BLE GATT. Config Service `4fafc201-1fb5-459e-8fcc-c5c9c331914b`, CMD `...914c`, EVT `...914d`.
- **Request**: `{ v: 1, type: "request", id, cmd, profileId?, profile?, payload? }`.
- **Response**: `{ type: "response"|"RESP", id, payload?, ok? }`; errors: `ok: false`, `error`, `message`.
- **Events**: `{ type: "event", event: "profileChanged", payload: { profileId } }` (and optional `layerChanged`).
- **Chunking**: Messages > ~512 bytes UTF-8 are split; each chunk `{ chunk, total, dataB64 }`; reassemble then parse.
- **Commands**: `getDeviceInfo`, `getCaps`, `listProfiles`, `getProfile`, `setProfile`, `setActiveProfile`, `getActiveProfile`, `deleteProfile`, `getStats`, `factoryReset`, `reboot`.

Profile JSON on device: `id`, `name`, `version`, `keys[]`, `encoders[]`; layers/combos when firmware supports.

## 3. Repo Structure (Web App)

```
web-app/
├── public/
│   └── icons/           # PWA icons
├── src/
│   ├── components/     # UI components, modals, key grid, etc.
│   ├── device/         # Web Bluetooth + protocol (bleConnection, protocolHandler)
│   ├── features/       # Feature-specific components (devices, profiles, macros)
│   ├── layouts/        # MainLayout, sidebar
│   ├── models/        # TypeScript types (profile, macro, keyConfig, etc.)
│   ├── pages/         # Devices, Profiles, Macros, Settings, Presets
│   ├── schemas/        # Zod schemas + migrations
│   ├── services/      # profileSyncService, exportImport
│   ├── storage/        # IndexedDB (idb): profiles, macros, settings
│   ├── stores/         # Zustand: deviceStore, profilesStore, macrosStore, uiStore
│   ├── App.tsx
│   └── main.tsx
├── index.html
├── package.json
├── vite.config.ts
├── tailwind.config.js
├── tsconfig.json
└── README.md
```

## 4. Deployment for $0/month

- **Build**: `npm run build` → `dist/` static output.
- **Cloudflare Pages**: Connect repo, build command `npm run build`, output directory `dist`. No backend required.
- **GitHub Pages**: Use GitHub Actions to run `npm run build` and push `dist` to `gh-pages` (or use `vite-plugin-gh-pages` with base path).
- **Offline**: PWA with Workbox/Vite PWA caches app shell + assets; IndexedDB holds all data locally.

## 5. Features to Match

- Key matrix UI (3×4 keys + encoders), drag & drop action assignment.
- Macro editor: timing, loops (steps), variables in text (e.g. `{clipboard}`, `{date}`, `{time}`) if present.
- Profiles: create, duplicate, delete, rename; layers 0/1/2; encoders mapping; combos.
- Device sync: push/pull profiles, set active, delete from device; device status (battery, firmware, connection).
- Empty states, connection status journey (Scan → Found → Pair → Ready), conflict resolution (Local vs Device).
- Export/Import: single .zip or .json with profiles + macros (+ assets refs) for backup/transfer.
- Undo/redo for profile editing (in-memory history stack).

## 6. Browser Support

- **Web Bluetooth**: Chrome, Edge, Opera (desktop); limited on macOS (e.g. Chrome). Not in Firefox/Safari. Show clear message when unsupported.
- **HTTPS required** for Web Bluetooth (and PWA). Localhost is allowed for development.
