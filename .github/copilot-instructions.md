# GitHub Copilot Instructions for Choose Wild Plant Spawns Mod

## Mod Overview and Purpose

**Mod Name:** Choose Wild Plant Spawns  
**Author:** Mlie  
**PackageId:** Mlie.ChooseWildPlantSpawns  
**Description:**  
This mod allows users to define the spawn rate of plants in all biomes of RimWorld. It provides a slider for overall plant density per biome and enables copying values between biomes. For higher accuracy, users can integrate the BetterSliders mod.

## Key Features and Systems

### Main Features
1. **Plant Spawn Rate Adjustment:**
   - Allows users to adjust the spawn rate of plants in each biome using a slider.
   
2. **Copy Values Between Biomes:**
   - Provides functionality to copy plant spawn rates from one biome to another.

3. **Settings Window:**
   - Includes a detailed settings window for configuring plant spawn rates and other related options.

### Systems
1. **Settings Management:**
   - Manages user-defined plant spawn rates and provides options to reset settings to their default values.

2. **Harmony Patching:**
   - Utilizes Harmony for patching vanilla game mechanics to integrate custom plant spawn rates.

3. **Saveable Dictionary:**
   - Implements a custom saveable dictionary to persist user settings across game sessions.

## Coding Patterns and Conventions

1. **Mod Structure:**
   - The mod is structured with clear separation of concerns, with each class handling specific functionalities.
   - The main mod class (`ChooseWildPlantSpawns_Mod`) manages the overall settings and user interface.
   - Additional classes (`ListingExtension`, `SaveableDictionary`, `Main`) handle specific features like UI extensions, data persistence, and vanilla value management.

2. **Code Naming Conventions:**
   - Class names are PascalCase and descriptive, e.g., `ChooseWildPlantSpawns_Mod`, `ChooseWildPlantSpawns_Settings`.
   - Method names are also PascalCase and descriptive, e.g., `DoSettingsWindowContents`, `ApplyBiomeSettings`.

3. **Code Comments:**
   - Methods and classes are well-documented with XML comments to explain their functionality and usage.

4. **Dependency Management:**
   - The mod depends on the Harmony mod (brrainz.harmony) for patching vanilla game mechanics.

## XML Integration

1. **XML Defs:**
   - The mod does not introduce new XML definitions. Instead, it modifies existing game mechanics through Harmony patches.

2. **Harmony Patching:**
   - Harmony is used to patch vanilla game methods to integrate custom plant spawn rates without altering the original game files.

## Harmony Patching

1. **Patch Usage:**
   - Harmony patches are used to modify the behavior of vanilla game methods related to plant spawning and biome settings.

2. **Patch Implementation:**
   - Patches are implemented in the `Main.cs` file, where methods like `ApplyBiomeSettings` are used to apply user-defined settings to the game.

## Suggestions for Copilot

1. **XML Integration:**
   - When generating code, consider the integration with Harmony for patching vanilla game mechanics.

2. **Settings Management:**
   - Focus on generating code that manages user settings and provides options to reset to default values.

3. **UI Extensions:**
   - Generate code that extends the game's UI to include new settings options and buttons for copying plant spawn rates.

4. **Data Persistence:**
   - Provide examples of how to implement custom saveable dictionaries to persist user settings across game sessions.

5. **Code Comments:**
   - Include XML comments in generated code to explain the functionality and usage of methods and classes.

6. **Mod Structure:**
   - Ensure that generated code follows the mod's structure, with clear separation of concerns and descriptive naming conventions.

By following these guidelines, Copilot can generate code that is well-suited for the Choose Wild Plant Spawns mod, enhancing its functionality and maintainability.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

