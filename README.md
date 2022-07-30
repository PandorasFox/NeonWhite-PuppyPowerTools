# Puppy Powertools

This mod aims to just be a speedrunning mod with some minor Quality of Life features that are allowed for use in speedrun submissions.

This mod started as just a speedometer, but has evolved into a handful of independent modules that can be independently configured.

## Current Features

* Speedometer (lateral/vertical velocity components separated)
  * Optional precise position/facing direction info 
* Chapter timer for chapter runs
* on-the-fly powerprefs adjustment (currently just the Level Rush Shuffle seed)
* VFX toggling
  * You can turn off the sun! Lighting is unaffected; just the lensflare machine is gone.
  * Fireball's particle effects around the edge of the screen can be turned off.
* Card customization
  * You can dynamically replace the text that is drawn on cards
  * Color customization may or may not come at a later date, pending rule approval.

## Installation & Usage

1. Download [MelonLoader](https://github.com/LavaGang/MelonLoader/releases/latest) and install it onto your `Neon White.exe`.
2. Run the game once. This will create required folders; you should see a splash screen if you installed the modloader correctly.
3. Download the **Mono** version of [Melon Preferences Manager](https://github.com/sinai-dev/MelonPreferencesManager/releases/latest), and put the .dlls from that zip into the `mods` folder of your Neon White install (e.g. `SteamLibrary\steamapps\common\Neon White\Mods`)
    * The preferences manager is *required* to use the powertools mod - it is how you turn parts of it on and off. Everything is off by default.
    * The default keybind for the mod preferences menu is F5; you can easily rebind this.
    * The IL2CPP version **WILL NOT WORK**; you **must** download `MelonPreferencesManager.Mono.zip`. 
4. Download the `PuppyPowertools.dll` from the [Releases page](https://github.com/PandorasFox/Neon-White-Mods/releases/latest) and drop it in the mods folder

### Additional Notes

You should probably add `--melonloader.hideconsole` to your game launch properties (right click the game in steam -> properties -> launch options at the bottom of that window) to hide the console that melonloader spawns. You really only need that if you're a mod developer; it's a weird default.

![image](https://user-images.githubusercontent.com/3235827/181994781-af470314-9836-49f4-beec-abdf1f9e37ea.png)

