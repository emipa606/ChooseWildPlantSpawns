using System.Collections.Generic;
using System.Linq;
using ChooseWildPlantSpawns.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChooseWildPlantSpawns;

[StaticConstructorOnStartup]
public static class Main
{
    public static readonly Dictionary<string, List<BiomePlantRecord>> VanillaSpawnRates = new();

    public static readonly Dictionary<string, float> VanillaDensities = new();

    public static readonly Dictionary<string, float> VanillaCaveWeights = new();

    private static List<ThingDef> allPlants;
    private static List<ThingDef> allCavePlants;
    private static List<BiomeDef> allBiomes;

    static Main()
    {
        saveVanillaValues();
        clearPlantDefs();
        ApplyBiomeSettings();

        var customWeights = ChooseWildPlantSpawns_Mod.Instance.Settings.CustomCaveWeights;
        foreach (var cavePlant in AllCavePlants)
        {
            if (customWeights?.TryGetValue(cavePlant.defName, out var weight) is true)
            {
                cavePlant.plant.cavePlantWeight = weight;
            }
        }
    }

    public static List<ThingDef> AllPlants
    {
        get
        {
            if (allPlants == null || allPlants.Count == 0)
            {
                allPlants = (from plant in DefDatabase<ThingDef>.AllDefsListForReading
                    where plant.plant != null
                    orderby plant.label
                    select plant).ToList();
            }

            return allPlants;
        }
    }

    public static List<ThingDef> AllCavePlants
    {
        get
        {
            if (allCavePlants == null || allCavePlants.Count == 0)
            {
                allCavePlants = (from plant in DefDatabase<ThingDef>.AllDefsListForReading
                    where plant.plant is { cavePlant: true }
                    orderby plant.label
                    select plant).ToList();
            }

            return allCavePlants;
        }
    }

    public static List<BiomeDef> AllBiomes
    {
        get
        {
            if (allBiomes == null || allBiomes.Count == 0)
            {
                allBiomes = (from biome in DefDatabase<BiomeDef>.AllDefsListForReading
                    orderby biome.label
                    select biome).ToList();
            }

            return allBiomes;
        }
    }

    private static void clearPlantDefs()
    {
        foreach (var plantDef in AllPlants)
        {
            if (plantDef.plant.wildBiomes == null)
            {
                continue;
            }

            plantDef.plant.wildBiomes = null;
        }
    }

    public static void ApplyBiomeSettings()
    {
        var customSpawnRates = ChooseWildPlantSpawns_Mod.Instance.Settings.CustomSpawnRates;
        var customDensities = ChooseWildPlantSpawns_Mod.Instance.Settings.CustomDensities;


        foreach (var biome in AllBiomes)
        {
            var biomePlantList = new List<BiomePlantRecord>();
            var customBiomeDefs = new Dictionary<string, float>();
            if (customSpawnRates.TryGetValue(biome.defName, out var rate))
            {
                customBiomeDefs = rate.dictionary;
            }

            biome.plantDensity = customDensities?.TryGetValue(biome.defName, out var density) is true
                ? density
                : VanillaDensities[biome.defName];

            var vanillaBiomeDefs = new List<BiomePlantRecord>();
            if (VanillaSpawnRates.TryGetValue(biome.defName, out var spawnRate))
            {
                vanillaBiomeDefs = spawnRate;
            }

            foreach (var thingDef in AllPlants)
            {
                if (customBiomeDefs.TryGetValue(thingDef.defName, out var commonality))
                {
                    if (commonality == 0)
                    {
                        continue;
                    }

                    biomePlantList.Add(new BiomePlantRecord
                        { plant = thingDef, commonality = commonality });

                    continue;
                }

                var record = vanillaBiomeDefs.FirstOrDefault(record1 => record1.plant == thingDef);
                if (record == null || record.commonality == 0)
                {
                    continue;
                }

                biomePlantList.Add(record);
            }

            AccessTools.Field(typeof(BiomeDef), "wildPlants").SetValue(biome, biomePlantList);

            AccessTools.Field(typeof(BiomeDef), "cachedWildPlants").SetValue(biome, null);

            AccessTools.Field(typeof(BiomeDef), "cachedPlantCommonalities").SetValue(biome, null);

            AccessTools.Field(typeof(BiomeDef), "cachedLowestWildPlantOrder").SetValue(biome, null);

            AccessTools.Field(typeof(BiomeDef), "cachedMaxWildPlantsClusterRadius").SetValue(biome, null);
        }
    }

    private static void saveVanillaValues()
    {
        foreach (var biome in AllBiomes)
        {
            VanillaDensities[biome.defName] = biome.plantDensity;
            var allWildPlantsInBiome = biome.AllWildPlants;
            if (!allWildPlantsInBiome.Any())
            {
                VanillaSpawnRates[biome.defName] = [];
                continue;
            }

            var currentBiomeRecord = new List<BiomePlantRecord>();
            var cachedCommonailtiesTraverse = Traverse.Create(biome).Field("cachedPlantCommonalities");
            if (cachedCommonailtiesTraverse.GetValue() == null)
            {
                _ = biome.CommonalityOfPlant(AllPlants.First());
            }

            var cachedPlantCommonalities = (Dictionary<ThingDef, float>)cachedCommonailtiesTraverse.GetValue();
            foreach (var plant in biome.AllWildPlants)
            {
                var commonality = cachedPlantCommonalities.GetValueOrDefault(plant, 0f);

                currentBiomeRecord.Add(new BiomePlantRecord
                    { plant = plant, commonality = commonality });
            }

            VanillaSpawnRates[biome.defName] = currentBiomeRecord;
        }

        foreach (var cavePlant in AllCavePlants)
        {
            VanillaCaveWeights[cavePlant.defName] = cavePlant.plant.cavePlantWeight;
        }
    }

    public static void LogMessage(string message, bool forced = false, bool warning = false)
    {
        if (warning)
        {
            Log.Warning($"[ChooseWildPlantSpawns]: {message}");
            return;
        }

        if (!forced && !ChooseWildPlantSpawns_Mod.Instance.Settings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[ChooseWildPlantSpawns]: {message}");
    }
}