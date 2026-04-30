# Handoff: PPBM Win11 Redesign

## Overview
A complete Windows 11–style dark UI redesign for **PPBM (Processor Performance Boost Mode Manager)** — a WPF desktop app that lets users inspect and control the hidden Windows processor boost power setting. The redesign improves visual hierarchy, adds a NavigationView sidebar, and adopts a strict monochromatic dark palette.

## About the Design Files
The files in this bundle are **design references created in HTML** — high-fidelity prototypes showing intended look, layout, and interactive behavior. They are **not** production code to copy directly. The task is to **recreate these designs in the existing WPF/XAML codebase** using its established patterns (ResourceDictionaries, Styles, ControlTemplates, DataTemplates, MVVM bindings). Do not ship the HTML files.

## Fidelity
**High-fidelity.** The HTML prototype uses final colors, typography, spacing, and interactions. Recreate the UI pixel-precisely using WPF's existing styling system and the token values listed below.

---

## Screens / Views

### 1. Dashboard
**Purpose:** System overview — current boost mode, CPU temp, quick profile switching, and frequency cap.

**Layout:**
- Full window: `display: flex; flex-direction: column`
- Title bar: 36px tall, `background: #0e0e0e`
- Shell: `flex: 1; flex-direction: row`
  - Left nav pane: 220px wide, `background: rgba(255,255,255,0.025)`, `border-right: 1px solid #2e2e2e`
  - Content area: remaining width, `overflow-y: auto`, `padding: 28px`

**Page Header:**
- Title: `font-size: 24px; font-weight: 600; color: #f0f0f0; letter-spacing: -0.01em`
- Subtitle: `font-size: 12px; color: #606060`

**Hero Card (CPU Status):**
- Background: `#1e1e1e`, border: `1px solid #2e2e2e`, radius: `8px`, padding: `20px 22px`
- Grid: 4 columns — `[auto] [1px divider] [1fr] [auto]`, gap: `20px`
- **Left col — Temp block:**
  - Label "CPU TEMP": `font-size: 11px; font-weight: 600; color: #606060; text-transform: uppercase; letter-spacing: 0.06em`
  - Temp value: `font-size: 52px; font-weight: 300; line-height: 1; letter-spacing: -2px; color: #e0e0e0`
  - Meta rows (Max core / Package / Load): key `font-size: 11px; color: #606060`, value `font-size: 12px; font-weight: 500; color: #a0a0a0`
- **Divider:** 1px wide, `background: #2e2e2e`, `margin: 4px 0`
- **Center col — Info:**
  - CPU name: `font-size: 18px; font-weight: 600; color: #f0f0f0`
  - Boost infobox: `background: rgba(255,255,255,0.04); border: 1px solid #2e2e2e; border-radius: 6px; padding: 10px 14px`
    - Dot: 8px circle, white when danger (`#f0f0f0` + faint glow)
    - Mode label: `font-size: 11px; color: #606060`
    - Mode name: `font-size: 14px; font-weight: 600; color: #f0f0f0`
    - Hex: monospace `font-size: 11px; color: #606060`
  - Alert banner (shown when Aggressive): `background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.10); border-radius: 6px; padding: 10px 14px; font-size: 13px; font-weight: 500; color: #a0a0a0`
- **Right col — Actions:** width `170px`, flex column, gap `8px`
  - Primary button: `background: rgba(255,255,255,0.10); border: 1px solid rgba(255,255,255,0.20); color: #f0f0f0; border-radius: 6px; padding: 8px 16px; font-size: 13px; font-weight: 500`
  - Secondary button: same but `background: rgba(255,255,255,0.05)`

**Section Label:**
- `font-size: 13px; font-weight: 600; color: #a0a0a0; margin: 20px 0 8px; letter-spacing: 0.01em`

**Profile List:**
- Each item: `background: #1e1e1e; border: 1px solid #2e2e2e; border-radius: 8px; padding: 14px 16px; margin-bottom: 2px`
- Grid: `[24px radio] [1fr body] [auto apply-btn]`, gap `12px`
- Selected item: `background: rgba(255,255,255,0.04); border-color: rgba(255,255,255,0.12)`
- Recommended item: `border-color: rgba(255,255,255,0.08)`
- Radio circle: 18px, border `2px solid #3a3a3a`; selected: filled `#f0f0f0`, inner dot `6px #111111`
- Profile name: `font-size: 14px; font-weight: 600; color: #f0f0f0`
- Badge (RECOMMENDED): `background: rgba(255,255,255,0.06); color: #c0c0c0; border: 1px solid rgba(255,255,255,0.12); font-size: 10px; font-weight: 700; padding: 2px 7px; border-radius: 4px; text-transform: uppercase`
- Badge (HOT / PERFORMANCE): `background: rgba(255,255,255,0.04); color: #707070; border: 1px solid rgba(255,255,255,0.08)`
- Description: `font-size: 12px; color: #a0a0a0`
- Use case + Temp meta: `font-size: 11px; color: #606060`

**CPU Frequency Cap Card:**
- Card: `background: #1e1e1e; border: 1px solid #2e2e2e; border-radius: 8px; padding: 14px 16px`
- Setting row (icon + text + toggle): `display: flex; align-items: center; gap: 16px`
  - Icon box: `32x32px; background: rgba(255,255,255,0.05); border-radius: 6px; color: #a0a0a0`
  - Title: `font-size: 13px; font-weight: 500; color: #f0f0f0`
  - Subtitle: `font-size: 12px; color: #606060`
  - Toggle: 40×22px, track `background: #3a3a3a` → checked `background: rgba(255,255,255,0.25)`, thumb: 14px circle white
- Slider: `height: 4px; border-radius: 2px`, filled portion `#d0d0d0`, unfilled `#3a3a3a`; thumb: 18px white circle
- Value badge: `background: rgba(255,255,255,0.05); border: 1px solid #2e2e2e; border-radius: 5px; padding: 4px 10px; font-size: 14px; font-weight: 600`

---

### 2. Power Profiles
**Purpose:** Full list of profiles for detailed selection.
Same profile list component as Dashboard — just the full page view with page header.

---

### 3. Monitors
**Purpose:** Display connected monitor info and refresh rate warnings.

**Monitor chips:** `background: #1e1e1e; border: 1px solid #2e2e2e; border-radius: 8px; padding: 12px 16px; min-width: 180px`
- Warn state: `border-color: rgba(255,255,255,0.10)`
- Monitor name: `font-size: 13px; font-weight: 600; color: #f0f0f0`
- Hz value: `font-size: 22px; font-weight: 300; color: #f0f0f0`
- Warning text: `font-size: 11px; color: #888888`
- OK text: `font-size: 11px; color: #606060`

---

### 4. Utilities
**Purpose:** Run power setting utilities (unhide, survive updates, open log).

**Util rows:** `display: flex; align-items: center; gap: 12px; padding: 14px 16px; border-bottom: 1px solid #2e2e2e`
- Icon box: `32x32px; border-radius: 6px`
  - Unhide: `background: rgba(255,255,255,0.05); color: #a0a0a0`
  - Survive: `background: rgba(255,255,255,0.04); color: #a0a0a0`
  - Debug log: `background: rgba(255,255,255,0.04); color: #606060`
- Title: `font-size: 13px; font-weight: 500; color: #f0f0f0`
- Desc: `font-size: 12px; color: #606060`
- Action button: ghost style (see tokens)

---

## Navigation Pane
- Width: `220px`
- Background: `rgba(255,255,255,0.025)`
- Border right: `1px solid #2e2e2e`
- Nav item: `padding: 9px 16px; border-radius: 6px; margin: 1px 6px; font-size: 13px; color: #a0a0a0`
  - Hover: `background: rgba(255,255,255,0.06); color: #f0f0f0`
  - Active: `background: rgba(255,255,255,0.08); color: #f0f0f0; font-weight: 500`
  - Active indicator: `3px wide; 18px tall; background: #f0f0f0; border-radius: 0 2px 2px 0; left: 2px` (absolute)
- Icons: 16×16, filled, `currentColor`
- Bottom section: `border-top: 1px solid #2e2e2e; padding-top: 8px`

---

## Title Bar
- Height: `36px`; background: `#0e0e0e`
- App icon: `16x16px; background: #e0e0e0; border-radius: 3px` (custom grid icon)
- Title text: `font-size: 12px; font-weight: 500; color: #a0a0a0`
- Window buttons: `46x36px`, hover: `background: rgba(255,255,255,0.08)`; close hover: `background: #c42b1c; color: white`

---

## Status Bar
- Height: `28px`; background: `#0e0e0e`; border-top: `1px solid #1e1e1e`; padding: `0 16px`
- Status message left: `font-size: 11px; color: #606060`
- Status pills right: `font-size: 11px; color: #606060`
  - Dot: `6px circle` — active: `#f0f0f0`, admin: `#c0c0c0`

---

## Interactions & Behavior
- **Auto-Detect & Fix:** Sets boost mode to "Disabled", hides alert banner, updates boost dot to inactive, drops temp display, updates status bar pill
- **Profile Apply:** Selects profile radio, updates status bar message
- **Frequency toggle:** Enables/disables slider + value badge (opacity 0.4 when disabled)
- **Slider:** Fills track with `#d0d0d0` proportionally; updates value badge live
- **Nav items:** Switch active page; only one page shown at a time
- **Survive Updates button:** Toggles between "Enable" / "Disable" text

---

## Design Tokens

### Colors
| Token | Value | Usage |
|---|---|---|
| `--bg` | `#111111` | Window root background |
| `--mica` | `#161616` | Shell/nav background |
| `--layer` | `#1e1e1e` | Cards |
| `--layer-hover` | `#2a2a2a` | Card hover |
| `--stroke` | `#2e2e2e` | Card borders |
| `--stroke2` | `#3a3a3a` | Control borders |
| `--text-pri` | `#f0f0f0` | Primary text |
| `--text-sec` | `#a0a0a0` | Secondary text |
| `--text-ter` | `#606060` | Tertiary/caption text |
| `--text-dis` | `#404040` | Disabled text |
| `--titlebar` | `#0e0e0e` | Title bar |

### Typography
- Font stack: `'Segoe UI Variable', 'Segoe UI', sans-serif`
- Page title: `24px / 600`
- Section label: `13px / 600`
- Card title: `14px / 600`
- Body: `13px / 400`
- Caption: `12px / 400`
- Monospace (hex values): `'Cascadia Code', 'Consolas', monospace`

### Spacing
- Card padding: `14px 16px`
- Hero card padding: `20px 22px`
- Content padding: `28px`
- Section gap: `20px top margin`
- Item gap: `2px between list items`
- Row gap: `16px between icon/text/control`

### Border Radius
- Cards: `8px`
- Buttons: `6px`
- Badges: `4px`
- Icon boxes: `6px`
- Toggle: `11px`
- Slider thumb: `50%`

---

## Assets
- No external images. Icons are inline SVG (Bootstrap Icons style, 16×16, filled).
- Window chrome icons (minimize, maximize, close) are simple geometric SVGs.

---

## Files
| File | Description |
|---|---|
| `PPBM.html` | Complete high-fidelity HTML prototype — all 4 pages, interactions, and styles |

---

## Notes for Implementation
1. The existing XAML already uses Windows 11 dark palette tokens — update the `SolidColorBrush` ResourceDictionary entries to match the monochrome values above.
2. Replace `AccentDefault (#60CDFF)` with `rgba(255,255,255,0.12)` equivalents for button fills.
3. Add a `NavigationView` (WinUI 3) or replicate the left nav with a `StackPanel` + custom item style.
4. The profile list uses a standard `ItemsControl` with a custom `DataTemplate` — the existing one just needs style updates.
5. Status bar is already implemented — update brush references only.
