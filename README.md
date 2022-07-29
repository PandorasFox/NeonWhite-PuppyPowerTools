# In this fork I am working on creating Collider (hitbox) visibility settings. 

# Puppy Powertools

This mod aims to just be a speedrunning mod with some minor Quality of Life features that are allowed for use in speedrun submissions.

This mod started as just a speedometer, but has evolved into a handful of independent modules that can be independently configured.

## Current Features

* Speedometer (lateral/vertical velocity components separated)
  * Optional precise position/facing direction info 
* Chapter timer for chapter runs
* on-the-fly powerprefs adjustment (currently just the Level Rush Shuffle seed)
* VFX toggling (currently, just for turning off the fireball particle fx around the screen)

## Installation

This mod uses [MelonLoader](https://github.com/LavaGang/MelonLoader) as its modloader. Install that on your Neon White install first, and then run the game once so it generates some folders, and to verify it's installed (you should see a MelonLoader splash screen).

After that, grab the latest .dll from the [Releases page](https://github.com/PandorasFox/Neon-White-Mods/releases) and drop it in the "Mods" folder in your Neon White folder, e.g. `SteamLibrary\steamapps\common\Neon White\Mods`.

## Configuration

Configuration for this mod is provided by the **Mono Varient** of [Melon Preferences Manager](https://github.com/sinai-dev/MelonPreferencesManager/releases/). Its default in-game bind is F5.

The IL2CPP version of Melon Preferences Manager will **NOT** work. You **must** use the Mono variant.
