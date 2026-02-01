# Pantheon Architect

Design and play custom Pantheon 5 lineups in Hollow Knight! Create your perfect boss gauntlet with any bosses in any order, then share your creations with friends using simple codes.

## 🎮 What is This?

Pantheon Architect lets you customize Pantheon of Hallownest (Pantheon 5) with your own boss lineups. Want to fight 10 Absolute Radiances in a row? Or create a speedrun-friendly lineup? Maybe a themed challenge with only ghost bosses? You can do it all!

## ✨ Features

- **Any Boss, Any Order**: Choose exactly which bosses you want to fight and in what sequence
- **Flexible Length**: Create lineups from 1 to 255 bosses (yes, even just one!)
- **Boss Repetition**: Fight the same boss multiple times if you want the challenge
- **Easy Sharing**: Every lineup gets a unique code you can share with friends
- **Web-Based Creator**: Use the simple website - no technical knowledge required

## 📥 Installation

### Option 1: Scarab Mod Manager (Easiest)
1. Download and install [Scarab](https://github.com/fifty-six/Scarab)
2. Open Scarab and search for "Pantheon Architect"
3. Click Install
4. Launch Hollow Knight!

### Option 2: Manual Installation
1. Download the latest release from the [Releases page](../../releases)
2. Find your Hollow Knight mods folder:
   - **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods`
3. Extract the downloaded zip into the mods directory
4. Launch Hollow Knight!

## 🎯 How to Use

### Creating Your Lineup

1. Visit [**pantheons.web.app**](https://pantheons.web.app/)
2. Select bosses from the dropdown menus
3. Click "Add Boss" to make your lineup longer, or "Remove Boss" to shorten it
4. Click "Generate Code" to get your shareable lineup code
5. Copy the code!

### Playing Your Lineup

1. Launch Hollow Knight and go to Godhome (NOT in the hall of gods or in a pantheon)
2. Press **F1** to open the lineup input
3. Pause the game to be able to use the mouse
4. Paste your lineup code into the text box
5. Click "Load Lineup"
6. Start Pantheon of Hallownest (Pantheon 5)
7. Enjoy your custom challenge!

### Sharing with Friends

Just send them your lineup code! They can paste it into their game and play the exact same lineup you created.

## 💡 Tips

- Your lineup is saved in the input UI, so you don't need to re-enter it every time
- You can load someone else's code to see what bosses they chose
- After completing your custom lineup, you'll be returned to Godhome
- Press F1 again to hide the input UI while playing

## 🤝 Compatibility

- Works with Hollow Knight version 1.5 and later
- Compatible with most other mods
- If you have other Pantheon 5 mods, they might conflict

## ❓ FAQ

**Q: Can I use this in other Pantheons?**  
A: Not currently - this mod only works with Pantheon 5.

**Q: Will this work for speedruns?**  
A: Custom lineups are great for practice, but they're not official pantheon completions.

**Q: My code isn't working!**  
A: Make sure you copied the entire code. Codes are case-sensitive and can be quite long for big lineups.

**Q: Can I create a random lineup?**  
A: Yes! Through the lineup website at [**pantheons.web.app**](https://pantheons.web.app/)

## 🛠️ For Developers

<details>
<summary>Click to expand developer information</summary>

### Building from Source

**Requirements:**
- .NET Framework 4.7.2 SDK or higher
- Hollow Knight game files

**Setup:**
1. Clone this repository
2. Create a `Directory.Build.props` file with your Hollow Knight paths:
```xml
<Project>
  <PropertyGroup>
    <HollowKnightRefs>C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed</HollowKnightRefs>
    <HollowKnightMods>%USERPROFILE%\AppData\LocalLow\Team Cherry\Hollow Knight\Mods</HollowKnightMods>
  </PropertyGroup>
</Project>
```

**Build:**
```bash
dotnet build
```

### How It Works

The mod intercepts scene loading when starting Pantheon 5:
1. Lineup codes are decoded into boss scene names
2. Each boss transition is redirected to the next boss in your lineup
3. After the final boss, you're returned to Godhome

**Lineup Code Format:**
- Base64-encoded byte array
- First byte: Number of bosses (1-255)
- Remaining bytes: Boss indices (0-39) from the Pantheon 5 boss list

</details>

## 📜 License

MIT License - Free to use and modify!
