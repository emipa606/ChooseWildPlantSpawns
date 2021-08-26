using System.Collections.Generic;
using System.Linq;
using ChooseWildPlantSpawns.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChooseWildPlantSpawns
{
    [StaticConstructorOnStartup]
    public static class Main
    {
        public static readonly Dictionary<string, List<BiomePlantRecord>> VanillaSpawnRates =
            new Dictionary<string, List<BiomePlantRecord>>();

        public static readonly Dictionary<string, float> VanillaDensities = new Dictionary<string, float>();

        public static readonly Dictionary<string, float> VanillaCaveWeights = new Dictionary<string, float>();

        private static List<ThingDef> allPlants;
        private static List<ThingDef> allCavePlants;
        private static List<BiomeDef> allBiomes;

        static Main()
        {
            saveVanillaValues();
            clearPlantDefs();
            ApplyBiomeSettings();

            var customWeights = ChooseWildPlantSpawns_Mod.instance.Settings.CustomCaveWeights;
            foreach (var cavePlant in AllCavePlants)
            {
                if (customWeights?.ContainsKey(cavePlant.defName) == true)
                {
                    cavePlant.plant.cavePlantWeight = customWeights[cavePlant.defName];
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
            set => allPlants = value;
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
            set => allCavePlants = value;
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
            set => allBiomes = value;
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
            var custumSpawnRates = ChooseWildPlantSpawns_Mod.instance.Settings.CustomSpawnRates;
            var customDensities = ChooseWildPlantSpawns_Mod.instance.Settings.CustomDensities;

            foreach (var biome in AllBiomes)
            {
                var biomePlantList = new List<BiomePlantRecord>();
                var customBiomeDefs = new Dictionary<string, float>();
                if (custumSpawnRates.ContainsKey(biome.defName))
                {
                    customBiomeDefs = custumSpawnRates[biome.defName].dictionary;
                }

                biome.plantDensity = customDensities?.ContainsKey(biome.defName) == true
                    ? customDensities[biome.defName]
                    : VanillaDensities[biome.defName];

                var vanillaBiomeDefs = new List<BiomePlantRecord>();
                if (VanillaSpawnRates.ContainsKey(biome.defName))
                {
                    vanillaBiomeDefs = VanillaSpawnRates[biome.defName];
                }

                foreach (var thingDef in AllPlants)
                {
                    if (customBiomeDefs.ContainsKey(thingDef.defName))
                    {
                        biomePlantList.Add(new BiomePlantRecord
                            { plant = thingDef, commonality = customBiomeDefs[thingDef.defName] });
                        continue;
                    }

                    if (Enumerable.Any(vanillaBiomeDefs, record => record.plant == thingDef))
                    {
                        biomePlantList.Add(vanillaBiomeDefs.First(record => record.plant == thingDef));
                        continue;
                    }

                    biomePlantList.Add(new BiomePlantRecord { plant = thingDef, commonality = 0 });
                }

                Traverse.Create(biome).Field("wildPlants").SetValue(biomePlantList);

                Traverse.Create(biome).Field("cachedWildPlants").SetValue(null);

                Traverse.Create(biome).Field("cachedPlantCommonalities").SetValue(null);

                Traverse.Create(biome).Field("cachedPlantCommonalitiesSum").SetValue(null);

                Traverse.Create(biome).Field("cachedLowestWildPlantOrder").SetValue(null);

                Traverse.Create(biome).Field("cachedMaxWildPlantsClusterRadius").SetValue(null);
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
                    continue;
                }

                var currentBiomeRecord = new List<BiomePlantRecord>();
                foreach (var plant in biome.AllWildPlants)
                {
                    currentBiomeRecord.Add(new BiomePlantRecord
                        { plant = plant, commonality = biome.CommonalityOfPlant(plant) });
                }

                VanillaSpawnRates[biome.defName] = currentBiomeRecord;
            }

            foreach (var cavePlant in AllCavePlants)
            {
                VanillaCaveWeights[cavePlant.defName] = cavePlant.plant.cavePlantWeight;
            }
        }

        public static void ResetToVanillaRates()
        {
            foreach (var biome in AllBiomes)
            {
                biome.plantDensity = VanillaDensities[biome.defName];
                if (!biome.AllWildPlants.Any() && !VanillaSpawnRates.ContainsKey(biome.defName))
                {
                    continue;
                }

                Traverse.Create(biome).Field("wildPlants").SetValue(!VanillaSpawnRates.ContainsKey(biome.defName)
                    ? new List<BiomePlantRecord>()
                    : VanillaSpawnRates[biome.defName]);

                Traverse.Create(biome).Field("cachedPlantCommonalities").SetValue(null);
            }
        }

        public static void LogMessage(string message, bool forced = false, bool warning = false)
        {
            if (warning)
            {
                Log.Warning($"[ChooseWildPlantSpawns]: {message}");
                return;
            }

            if (!forced && !ChooseWildPlantSpawns_Mod.instance.Settings.VerboseLogging)
            {
                return;
            }

            Log.Message($"[ChooseWildPlantSpawns]: {message}");
        }
    }
}