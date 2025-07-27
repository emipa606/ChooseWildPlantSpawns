# RimWorld Modding Project: ChooseWildPlantSpawns

Welcome to the `ChooseWildPlantSpawns` mod for RimWorld! This document serves as a guide to understanding the structure and coding conventions used in this project. It will also provide you with instructions and suggestions for using GitHub Copilot to assist you in writing and enhancing mod code.

## Mod Overview and Purpose

The `ChooseWildPlantSpawns` mod allows players to customize the types of wild plants that can spawn in different biomes within their RimWorld game. This enhances the gameplay experience by giving players more control over their environment and optimizing it for their personal strategies and aesthetics.

## Key Features and Systems

- **Customizable Plant Spawns**: Players can choose which plants are allowed to spawn in each biome, tailoring their game to their preferences.
- **Settings Management**: The mod includes settings that enable players to reset plant spawn preferences either globally or for individual biomes.
- **Saveable Dictionaries**: Data regarding plant preferences is stored and saved between game sessions, providing a consistent player experience.

## Coding Patterns and Conventions

- **Class Structures**: Key classes in this mod include `ChooseWildPlantSpawns_Mod`, `ChooseWildPlantSpawns_Settings`, `ListingExtension`, `Main`, and `SaveableDictionary`.
- **Class and Method Visibility**: Most classes are public to ensure accessibility across the mod, while methods are either public when they need to be accessed externally or private for internal logic.
- **Naming Conventions**: Classes are named using PascalCase, and methods use camelCase. This is in line with C# conventions and improves the readability and maintainability of the code.

## XML Integration

Though the summarized data does not include specific XML files, it's worth noting that RimWorld modding often involves XML definitions. These define items, plants, biomes, and other in-game objects that the C# code interacts with. Ensure all XML attributes and elements are correct and align with C# classes for a seamless integration.

## Harmony Patching

- **Harmony Usage**: Although not detailed in the summary, Harmony is a necessary tool for modifying RimWorld's existing behavior. If the mod intends to change game behaviors without directly editing the game's assembly, use Harmony patches wisely to avoid conflicts.
- Create patches when altering game functions to ensure compatibility with other mods and updates.

## Suggestions for Copilot

- **Code Completion**: Use Copilot to generate repetitive code such as getters and setters, which can ease the development of settings and configuration scripts.
- **Complex Logic**: For more complex logic, such as customizing biome-based plant spawning or extending existing game systems, Copilot can help provide boilerplate or suggest algorithms based on contextual cues.
- **Documentation**: Leverage Copilot to suggest documentation comments, improving code readability and maintainability.
- **Error Handling**: Ensure Copilot's suggestions include proper exception handling, especially when dealing with file operations and mod settings adjustments.

By following these guidelines and utilizing GitHub Copilot effectively, you can enhance your mod's development process and ensure a robust and enjoyable experience for your players.
