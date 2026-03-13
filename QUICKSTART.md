# FeColorizer Quick Start Guide

Get up and running with FeColorizer in under 2 minutes.

---

## What is FeColorizer?

FeColorizer automatically color-codes your folders in Windows File Explorer based on their first letter. A folders get Aqua, B folders get Blue, C folders get Cyan, and so on through Z. It's a simple, visual way to organize your file system.

---

## Installation (30 seconds)

1. **Download** `FeColorizer-Setup.exe`
2. **Double-click** the installer
3. Click **Yes** at the UAC prompt
4. The installer registers the context menu and generates all color icons automatically

That's it — FeColorizer is ready to use immediately after the installer closes.

---

## First Use (30 seconds)

1. **Open File Explorer** (Windows key + E)
2. **Navigate** to a folder with several subfolders (e.g. Documents or a project folder)
3. **Right-click** the parent folder
4. Select **Colorize subfolders**
5. Your subfolders will instantly show colored icons

Works on drives too — right-click any drive (e.g. D:\) in **This PC** and select **Colorize subfolders**.

---

## How the Colors Work

FeColorizer assigns colors based on the **first letter** of each folder name:

- **Documents** → starts with D → Denim (deep blue)
- **Photos** → starts with P → Purple
- **Work** → starts with W → White (light gray)
- **Archive** → starts with A → Aqua

Folders with numeric prefixes (e.g. `01-Downloads`) use the first letter found — so `01-Downloads` gets D → Denim.

---

## Removing Colors

1. **Right-click** the parent folder
2. Select **Remove folder colors**
3. Folders return to their default yellow icons

---

## Common Workflows

### Organize a Project Folder
```
Right-click Projects folder → Colorize subfolders
```
Each project gets a unique color based on its name.

### Color-Code a Drive
```
Right-click D:\ in This PC → Colorize subfolders
```
All top-level folders on the drive get color-coded instantly.

### Clean Up
```
Right-click the same folder → Remove folder colors
```

---

## Tips & Best Practices

### 1. Name Strategically
Since colors are based on first letters, you can group folders by color intentionally:
- `Archive_Photos`, `Archive_Videos` → All Aqua
- `Project_Alpha`, `Project_Beta` → All Purple

### 2. One Click Per Level
FeColorizer only colors immediate subfolders. Right-click each level separately if you need nested folders colorized.

### 3. Don't Overuse
Reserve colorization for frequently accessed directories and areas where visual distinction adds value.

---

## Understanding What FeColorizer Does

**Behind the scenes:**
- Generates colored `.ico` files in `%AppData%\FeColorizer\icons\` (done once at install time)
- Writes a `desktop.ini` inside each colorized folder pointing to its icon
- Windows reads `desktop.ini` and displays the custom icon
- All changes are local to your PC

**What it doesn't do:**
- No background processes
- No constant monitoring
- No automatic re-coloring
- No system-wide changes

---

## Troubleshooting

### Colors Not Appearing?
1. Make sure you right-clicked the **parent folder**, not a subfolder
2. Switch to **List view** in File Explorer, then switch back to **Large icons** — this forces a refresh
3. If colors still don't appear, run **Reset Thumbnail Cache** (see below)

### Resetting the Thumbnail Cache
Windows caches folder thumbnails. If stale entries prevent colors from displaying, clear the cache:

1. Open `C:\Program Files\FeColorizer\`
2. Run **ResetThumbnailCache.bat**
3. Click **Yes** at the UAC prompt
4. Explorer will restart automatically with a clean cache

After the cache is cleared, re-apply colors via **Colorize subfolders**.

### "Access Denied" on a Folder?
You're trying to colorize a protected folder. Avoid system folders (Windows, Program Files). User directories (Documents, Desktop, custom drives) work fine.

---

## Uninstalling

1. Open **Windows Settings → Apps**
2. Search for **FeColorizer**
3. Click **Uninstall**

Colorized folders will keep their colors until you revert them via right-click before uninstalling.

---

## Support

- GitHub: [github.com/yourusername/fecolorizer](https://github.com/yourusername/fecolorizer)

---

**You're all set!** Right-click a folder and start organizing.
