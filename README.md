# Gemporium Grab Items

[中文说明](README.zh-CN.md) · [Downloads](https://github.com/jeanmuss/gemporium-grab-items/releases)

A standalone BepInEx plugin for the Windows x64 Steam edition of Gemporium. Press **F8** to browse polished gems and unpolished artifacts.

## Features

- 15 gem types in two categories: polished cracked gems and polished intact gems.
- Unpolished artifacts from the curated base-game catalog.
- Left click adds **1**, right click adds **10** to home inventory.
- Native game icons, English item names, bilingual category labels, search and 20-item pages.
- Normal random gem weights. Artifacts are not automatically registered as collected.
- Amber, DLC-only entries, shop merchandise and story keys are excluded.
- Gameplay input is suppressed while the panel is open. A runtime cursor patch prevents the game from recentering the hardware mouse.

## Installation

Version **0.1.3**, public preview. Built against Steam build **25046214**, Unity **6000.0.67f1**, BepInEx **5.4.23.6**.

1. Close the game and back up your save.
2. Install [BepInEx 5.4.23.6 Windows x64](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6) separately.
3. Download **GemporiumGrabItems-0.1.3-BepInEx.zip** from Releases. Automatic source archives are not installable packages.
4. Extract it into the game directory. The DLL belongs at `BepInEx/plugins/GemporiumGrabItems/GemporiumGrabItems.dll`.
5. Launch through Steam, load a save into the shop/home scene, and press F8.

Added items persist through the game's normal save process. Existing items are not converted to polished gems. The plugin targets home inventory, not the limited mining bag.

## Updating and uninstalling

Close the game before replacing the DLL. To uninstall, remove the GemporiumGrabItems plugin folder. Previously saved items remain in the save.

## Building and compatibility

See [BUILDING.md](BUILDING.md). The plugin uses Harmony runtime patches; it does not replace game assemblies on disk. Game assemblies, assets, saves and the loader are not redistributed.

Compilation and package verification passed. The latest gameplay changes have not received a confirmed in-game acceptance test; test cursor movement, clicking, closing the panel, saving and reloading. Compatibility with other mods and future game versions is unverified.

This is an unofficial mod. Game content belongs to its respective owners; icons are loaded at runtime. See [CHANGELOG.md](CHANGELOG.md).
