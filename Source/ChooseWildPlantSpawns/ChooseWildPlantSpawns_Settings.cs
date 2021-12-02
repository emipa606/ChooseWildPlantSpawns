using System.Collections.Generic;
using Verse;

namespace ChooseWildPlantSpawns;

public class ChooseWildPlantSpawns_Settings : ModSettings
{
    public Dictionary<string, float> CustomCaveWeights = new Dictionary<string, float>();
    private List<string> customCaveWeightsKeys;
    private List<float> customCaveWeightsValues;
    public Dictionary<string, float> CustomDensities = new Dictionary<string, float>();
    private List<string> customDensitiesKeys;
    private List<float> customDensitiesValues;

    public Dictionary<string, SaveableDictionary> CustomSpawnRates =
        new Dictionary<string, SaveableDictionary>();

    private List<string> customSpawnRatesKeys;

    private List<SaveableDictionary> customSpawnRatesValues;

    public bool VerboseLogging;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
        Scribe_Collections.Look(ref CustomSpawnRates, "CustomSpawnRates", LookMode.Value,
            LookMode.Value,
            ref customSpawnRatesKeys, ref customSpawnRatesValues);
        Scribe_Collections.Look(ref CustomDensities, "CustomDensities", LookMode.Value,
            LookMode.Value,
            ref customDensitiesKeys, ref customDensitiesValues);
        Scribe_Collections.Look(ref CustomCaveWeights, "CustomCaveWeights", LookMode.Value,
            LookMode.Value,
            ref customCaveWeightsKeys, ref customCaveWeightsValues);
    }

    public void ResetManualValues()
    {
        customSpawnRatesKeys = new List<string>();
        customSpawnRatesValues = new List<SaveableDictionary>();
        CustomSpawnRates = new Dictionary<string, SaveableDictionary>();
        customDensitiesKeys = new List<string>();
        customDensitiesValues = new List<float>();
        CustomDensities = new Dictionary<string, float>();
        customCaveWeightsKeys = new List<string>();
        customCaveWeightsValues = new List<float>();
        CustomCaveWeights = new Dictionary<string, float>();
        Main.ApplyBiomeSettings();
        foreach (var cavePlant in Main.AllCavePlants)
        {
            cavePlant.plant.cavePlantWeight = Main.VanillaCaveWeights[cavePlant.defName];
        }
    }


    public void ResetOneValue(string BiomeDefName)
    {
        if (BiomeDefName == "Caves")
        {
            foreach (var cavePlant in Main.AllCavePlants)
            {
                cavePlant.plant.cavePlantWeight = Main.VanillaCaveWeights[cavePlant.defName];
            }

            return;
        }

        if (CustomSpawnRates.ContainsKey(BiomeDefName))
        {
            CustomSpawnRates.Remove(BiomeDefName);
        }

        if (CustomDensities.ContainsKey(BiomeDefName))
        {
            CustomDensities.Remove(BiomeDefName);
        }

        Main.ApplyBiomeSettings();
    }
}