using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseWildPlantSpawns.Settings;

public class ChooseWildPlantSpawns_Mod : Mod
{
    private const int ButtonSpacer = 200;

    private const float ColumnSpacer = 0.1f;

    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static ChooseWildPlantSpawns_Mod Instance;

    private static readonly Vector2 buttonSize = new(120f, 25f);

    private static readonly Vector2 searchSize = new(200f, 25f);

    private static readonly Vector2 iconSize = new(48f, 48f);

    private static float leftSideWidth;

    private static Listing_Standard listingStandard;

    private static Vector2 tabsScrollPosition;

    private static string currentVersion;

    private static Vector2 scrollPosition;

    private static Dictionary<ThingDef, float> currentBiomePlantRecords;
    private static Dictionary<ThingDef, int> currentBiomePlantDecimals;

    private static Dictionary<BiomeDef, float> currentPlantBiomeRecords;
    private static Dictionary<BiomeDef, int> currentPlantBiomeDecimals;

    private static float currentBiomePlantDensity;

    private static string selectedDef = "Settings";

    private static string searchText = "";

    private static float globalValue;

    private static readonly Color alternateBackground = new(0.1f, 0.1f, 0.1f, 0.5f);

    /// <summary>
    ///     The private settings
    /// </summary>
    private ChooseWildPlantSpawns_Settings settings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public ChooseWildPlantSpawns_Mod(ModContentPack content)
        : base(content)
    {
        Instance = this;
        searchText = string.Empty;
        ParseHelper.Parsers<SaveableDictionary>.Register(SaveableDictionary.FromString);
        Instance.Settings.CustomSpawnRates ??= new Dictionary<string, SaveableDictionary>();

        Instance.Settings.CustomDensities ??= new Dictionary<string, float>();

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal ChooseWildPlantSpawns_Settings Settings
    {
        get
        {
            settings ??= GetSettings<ChooseWildPlantSpawns_Settings>();

            return settings;
        }
    }

    private static string SelectedDef
    {
        get => selectedDef;
        set
        {
            if (selectedDef != null && selectedDef != "Settings" && selectedDef != "Caves")
            {
                saveBiomeSettings();
                Main.ApplyBiomeSettings();
            }

            currentPlantBiomeRecords = new Dictionary<BiomeDef, float>();
            currentPlantBiomeDecimals = new Dictionary<BiomeDef, int>();
            currentBiomePlantRecords = new Dictionary<ThingDef, float>();
            currentBiomePlantDecimals = new Dictionary<ThingDef, int>();
            currentBiomePlantDensity = 0;
            selectedDef = value;

            if (value is null or "Settings" or "Caves")
            {
                return;
            }

            Traverse cachedCommonalitiesTraverse;
            Dictionary<ThingDef, float> cachedPlantCommonalities;
            if (Instance.Settings.ReverseSettingsMode)
            {
                var selectedPlantDef = ThingDef.Named(selectedDef);
                foreach (var biomeDef in Main.AllBiomes)
                {
                    cachedCommonalitiesTraverse = Traverse.Create(biomeDef)
                        .Field("cachedPlantCommonalities");
                    if (cachedCommonalitiesTraverse.GetValue() == null)
                    {
                        _ = biomeDef.CommonalityOfPlant(selectedPlantDef);
                    }

                    cachedPlantCommonalities = (Dictionary<ThingDef, float>)cachedCommonalitiesTraverse.GetValue();

                    var commonality = cachedPlantCommonalities.GetValueOrDefault(selectedPlantDef, 0f);

                    currentPlantBiomeRecords[biomeDef] = commonality;
                    var decimals =
                        (currentPlantBiomeRecords[biomeDef] -
                         Math.Truncate(currentPlantBiomeRecords[biomeDef]))
                        .ToString().Length;

                    if (decimals < 4)
                    {
                        decimals = 4;
                    }

                    currentPlantBiomeDecimals[biomeDef] = decimals;
                }

                return;
            }

            var selectedBiome = BiomeDef.Named(selectedDef);
            currentBiomePlantDensity = selectedBiome.plantDensity;

            cachedCommonalitiesTraverse = Traverse.Create(selectedBiome).Field("cachedPlantCommonalities");
            if (cachedCommonalitiesTraverse.GetValue() == null)
            {
                _ = selectedBiome.CommonalityOfPlant(Main.AllPlants.First());
            }

            cachedPlantCommonalities = (Dictionary<ThingDef, float>)cachedCommonalitiesTraverse.GetValue();
            foreach (var plant in Main.AllPlants)
            {
                var commonality = cachedPlantCommonalities.GetValueOrDefault(plant, 0f);

                currentBiomePlantRecords[plant] = commonality;
                var decimals =
                    (currentBiomePlantRecords[plant] - Math.Truncate(currentBiomePlantRecords[plant]))
                    .ToString().Length;

                if (decimals < 4)
                {
                    decimals = 4;
                }

                currentBiomePlantDecimals[plant] = decimals;
            }
        }
    }

    public override void WriteSettings()
    {
        saveBiomeSettings();
        base.WriteSettings();
        Main.ApplyBiomeSettings();
        SelectedDef = "Settings";
    }


    private static void saveBiomeSettings()
    {
        if (SelectedDef is "Settings" or "Caves")
        {
            return;
        }

        if (Instance.Settings.ReverseSettingsMode)
        {
            savePlantSetting();
            return;
        }

        saveABiomeSetting();
    }

    private static void savePlantSetting()
    {
        try
        {
            var plant = ThingDef.Named(SelectedDef);
            if (currentPlantBiomeRecords == null)
            {
                currentPlantBiomeRecords = new Dictionary<BiomeDef, float>();
                currentPlantBiomeDecimals = new Dictionary<BiomeDef, int>();
                Main.LogMessage($"currentPlantBiomeRecords null for {SelectedDef}");
                return;
            }

            if (!currentPlantBiomeRecords.Any())
            {
                Main.LogMessage($"currentPlantBiomeRecords for {SelectedDef} empty");
                return;
            }

            foreach (var biomeDef in Main.AllBiomes)
            {
                var biomeDefName = biomeDef.defName;

                if (!Main.VanillaSpawnRates.TryGetValue(biomeDefName, out var rate))
                {
                    Main.LogMessage($"VanillaSpawnRates not contain {biomeDefName}");
                    continue;
                }

                var vanillaValue = rate
                    .FirstOrFallback(record => record.plant == plant);
                if (vanillaValue != null && vanillaValue.commonality.ToString() ==
                    currentPlantBiomeRecords[biomeDef].ToString())
                {
                    if (Instance.Settings.CustomSpawnRates.ContainsKey(biomeDefName) && Instance.Settings
                            .CustomSpawnRates[biomeDefName].dictionary.Remove(SelectedDef))
                    {
                        if (!Instance.Settings.CustomSpawnRates[biomeDefName].dictionary.Any())
                        {
                            Main.LogMessage($"currentBiomeList for {biomeDefName} empty");
                            Instance.Settings.CustomSpawnRates.Remove(biomeDefName);
                        }
                    }

                    continue;
                }

                if (vanillaValue == null && currentPlantBiomeRecords[biomeDef] == 0)
                {
                    continue;
                }

                Main.LogMessage(
                    $"{plant.label} in {biomeDefName}: chosen value {currentPlantBiomeRecords[biomeDef]}, vanilla value {vanillaValue?.commonality}");

                if (!Instance.Settings.CustomSpawnRates.ContainsKey(biomeDefName))
                {
                    Instance.Settings.CustomSpawnRates[biomeDefName] = new SaveableDictionary();
                }

                Instance.Settings.CustomSpawnRates[biomeDefName].dictionary[SelectedDef] =
                    currentPlantBiomeRecords[biomeDef];
            }

            currentPlantBiomeRecords = new Dictionary<BiomeDef, float>();
            currentPlantBiomeDecimals = new Dictionary<BiomeDef, int>();
        }
        catch (Exception exception)
        {
            Main.LogMessage($"Failed to save settings for {SelectedDef}, {exception}", true, true);
        }
    }

    private static void saveABiomeSetting()
    {
        try
        {
            if (currentBiomePlantDensity == Main.VanillaDensities[SelectedDef])
            {
                Instance.Settings.CustomDensities?.Remove(SelectedDef);
            }
            else
            {
                Instance.Settings.CustomDensities[SelectedDef] = currentBiomePlantDensity;
            }

            if (currentBiomePlantRecords == null)
            {
                currentBiomePlantRecords = new Dictionary<ThingDef, float>();
                currentBiomePlantDecimals = new Dictionary<ThingDef, int>();
                Main.LogMessage($"currentBiomePlantRecords null for {SelectedDef}");
                return;
            }

            if (!currentBiomePlantRecords.Any())
            {
                Main.LogMessage($"currentBiomePlantRecords for {SelectedDef} empty");
                return;
            }

            if (!Main.VanillaSpawnRates.ContainsKey(SelectedDef))
            {
                Main.LogMessage($"VanillaSpawnRates not contain {SelectedDef}");
                currentBiomePlantRecords = new Dictionary<ThingDef, float>();
                currentBiomePlantDecimals = new Dictionary<ThingDef, int>();
                return;
            }

            var currentBiomeList = new Dictionary<string, float>();
            foreach (var plant in Main.AllPlants)
            {
                var vanillaValue = Main.VanillaSpawnRates[SelectedDef]
                    .FirstOrFallback(record => record.plant == plant);
                if (vanillaValue != null && vanillaValue.commonality.ToString() ==
                    currentBiomePlantRecords[plant].ToString())
                {
                    continue;
                }

                if (vanillaValue == null && currentBiomePlantRecords[plant] == 0)
                {
                    continue;
                }

                Main.LogMessage(
                    $"{plant.label}: chosen value {currentBiomePlantRecords[plant]}, vanilla value {vanillaValue?.commonality}");
                currentBiomeList.Add(plant.defName, currentBiomePlantRecords[plant]);
            }

            if (!currentBiomeList.Any())
            {
                Instance.Settings.CustomSpawnRates.Remove(SelectedDef);

                currentBiomePlantRecords = new Dictionary<ThingDef, float>();
                currentBiomePlantDecimals = new Dictionary<ThingDef, int>();
                Main.LogMessage($"currentBiomeList for {SelectedDef} empty");
                return;
            }

            Instance.Settings.CustomSpawnRates[SelectedDef] = new SaveableDictionary(currentBiomeList);
            currentBiomePlantRecords = new Dictionary<ThingDef, float>();
            currentBiomePlantDecimals = new Dictionary<ThingDef, int>();
        }
        catch (Exception exception)
        {
            Main.LogMessage($"Failed to save values, {exception}", true, true);
        }
    }

    /// <summary>
    ///     The settings-window
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        base.DoSettingsWindowContents(rect);

        var rect2 = rect.ContractedBy(1);
        leftSideWidth = rect2.ContractedBy(10).width / 4;

        listingStandard = new Listing_Standard();

        drawOptions(rect2);
        drawTabsList(rect2);
        Settings.Write();
    }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "Choose Wild Plant Spawns";
    }


    private static void drawButton(Action action, string text, Vector2 pos)
    {
        var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }


    private static void drawIcon(ThingDef thingDef, Rect rect)
    {
        if (thingDef == null)
        {
            return;
        }

        var texture2D = thingDef.graphicData?.Graphic?.MatSingle?.mainTexture;

        if (thingDef.graphicData?.graphicClass == typeof(Graphic_Random))
        {
            texture2D = ((Graphic_Random)thingDef.graphicData.Graphic)?.FirstSubgraphic().MatSingle.mainTexture;
        }

        if (thingDef.graphicData?.graphicClass == typeof(Graphic_StackCount))
        {
            texture2D = ((Graphic_StackCount)thingDef.graphicData.Graphic)?.SubGraphicForStackCount(1, thingDef)
                .MatSingle
                .mainTexture;
        }

        if (texture2D == null)
        {
            return;
        }

        var toolTip = $"{thingDef.LabelCap}\n{thingDef.description}";
        if (texture2D.width != texture2D.height)
        {
            var ratio = (float)texture2D.width / texture2D.height;

            if (ratio < 1)
            {
                rect.x += (rect.width - (rect.width * ratio)) / 2;
                rect.width *= ratio;
            }
            else
            {
                rect.y += (rect.height - (rect.height / ratio)) / 2;
                rect.height /= ratio;
            }
        }

        GUI.DrawTexture(rect, texture2D);
        TooltipHandler.TipRegion(rect, toolTip);
    }

    private void drawOptions(Rect rect)
    {
        var optionsOuterContainer = rect.ContractedBy(10);
        optionsOuterContainer.x += leftSideWidth + ColumnSpacer;
        optionsOuterContainer.width -= leftSideWidth + ColumnSpacer;
        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = optionsInnerContainer.ContractedBy(10);
        frameRect.x = leftSideWidth + ColumnSpacer + 20;
        frameRect.y += 15;
        frameRect.height -= 15;
        var contentRect = frameRect;
        contentRect.x = 0;
        contentRect.y = 0;
        switch (SelectedDef)
        {
            case null:
                return;
            case "Settings":
            {
                listingStandard.Begin(frameRect);
                Text.Font = GameFont.Medium;
                listingStandard.Label("CWPS.settings".Translate());
                Text.Font = GameFont.Small;
                listingStandard.Gap();

                if (Instance.Settings.CustomSpawnRates?.Any() == true ||
                    Instance.Settings.CustomDensities?.Any() == true ||
                    Instance.Settings.CustomCaveWeights?.Any() == true)
                {
                    var labelPoint = listingStandard.Label("CWPS.resetall.label".Translate(), -1F,
                        "CWPS.resetall.tooltip".Translate());
                    drawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWPS.resetall.confirm".Translate(),
                                delegate { Instance.Settings.ResetManualValues(); }));
                        }, "CWPS.resetall.button".Translate(),
                        new Vector2(labelPoint.position.x + ButtonSpacer, labelPoint.position.y));
                }

                listingStandard.CheckboxLabeled("CWPS.reversemode.label".Translate(),
                    ref Instance.Settings.ReverseSettingsMode,
                    "CWPS.reversemode.tooltip".Translate());
                listingStandard.CheckboxLabeled("CWPS.logging.label".Translate(), ref Settings.VerboseLogging,
                    "CWPS.logging.tooltip".Translate());
                if (currentVersion != null)
                {
                    listingStandard.Gap();
                    GUI.contentColor = Color.gray;
                    listingStandard.Label("CWPS.version.label".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }

                listingStandard.End();
                break;
            }
            case "Caves":
            {
                listingStandard.Begin(frameRect);
                Text.Font = GameFont.Medium;
                var description = "CWPS.caves.description".Translate();
                var headerLabel = listingStandard.Label("CWPS.caves".Translate());
                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position,
                    searchSize), description);

                Instance.Settings.CustomCaveWeights ??= new Dictionary<string, float>();

                if (Instance.Settings.CustomCaveWeights.Any())
                {
                    drawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWPS.resetone.confirm".Translate(SelectedDef.Translate()),
                                delegate { Instance.Settings.ResetOneBiome(SelectedDef); }));
                        }, "CWPS.reset.button".Translate(),
                        new Vector2(headerLabel.position.x + headerLabel.width - buttonSize.x,
                            headerLabel.position.y));
                }

                Text.Font = GameFont.Small;

                searchText =
                    Widgets.TextField(
                        new Rect(headerLabel.position + new Vector2((frameRect.width / 2) - (searchSize.x / 2), 0),
                            searchSize),
                        searchText);
                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position + new Vector2((frameRect.width / 2) - (searchSize.x / 2), 0),
                    searchSize), "CWPS.search".Translate());

                listingStandard.End();

                var cavePlants = Main.AllCavePlants;
                if (!string.IsNullOrEmpty(searchText))
                {
                    cavePlants = Main.AllCavePlants.Where(def =>
                            def.label.ToLower().Contains(searchText.ToLower()) || def.modContentPack?.Name?.ToLower()
                                .Contains(searchText.ToLower()) == true)
                        .ToList();
                }

                var borderRect = frameRect;
                borderRect.y += headerLabel.y + 40;
                borderRect.height -= headerLabel.y + 40;
                var scrollContentRect = frameRect;
                scrollContentRect.height = cavePlants.Count * 51f;
                scrollContentRect.width -= 20;
                scrollContentRect.x = 0;
                scrollContentRect.y = 0;


                var scrollListing = new Listing_Standard();
                Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
                scrollListing.Begin(scrollContentRect);
                var alternate = false;
                foreach (var cavePlant in cavePlants)
                {
                    var modInfo = cavePlant.modContentPack?.Name;
                    var rowRect = scrollListing.GetRect(50);
                    alternate = !alternate;
                    if (alternate)
                    {
                        Widgets.DrawBoxSolid(rowRect.ExpandedBy(10, 0), alternateBackground);
                    }

                    var sliderRect = new Rect(rowRect.position + new Vector2(iconSize.x, 0),
                        rowRect.size - new Vector2(iconSize.x, 0));

                    var plantLabel = $"{cavePlant.label.CapitalizeFirst()} ({cavePlant.defName})";
                    if (plantLabel.Length > 45)
                    {
                        plantLabel = $"{plantLabel.Substring(0, 42)}...";
                    }

                    if (modInfo is { Length: > 45 })
                    {
                        modInfo = $"{modInfo.Substring(0, 42)}...";
                    }

                    if (cavePlant.plant.cavePlantWeight !=
                        Main.VanillaCaveWeights[cavePlant.defName])
                    {
                        Instance.Settings.CustomCaveWeights[cavePlant.defName] =
                            cavePlant.plant.cavePlantWeight;
                        GUI.color = Color.green;
                    }
                    else
                    {
                        Instance.Settings.CustomCaveWeights.Remove(cavePlant.defName);
                    }

                    cavePlant.plant.cavePlantWeight =
                        (float)Math.Round((decimal)Widgets.HorizontalSlider(
                            sliderRect,
                            cavePlant.plant.cavePlantWeight, 0,
                            2f, false,
                            cavePlant.plant.cavePlantWeight.ToString(),
                            plantLabel,
                            modInfo), 4);

                    GUI.color = Color.white;
                    drawIcon(cavePlant,
                        new Rect(rowRect.position, iconSize));
                }

                scrollListing.End();
                Widgets.EndScrollView();
                break;
            }

            default:
            {
                BiomeDef currentBiomeDef = null;
                ThingDef currentPlantDef = null;
                string description;
                Rect headerLabel;
                listingStandard.Begin(frameRect);
                if (Instance.Settings.ReverseSettingsMode)
                {
                    currentPlantDef = ThingDef.Named(SelectedDef);
                    if (currentPlantDef == null)
                    {
                        listingStandard.End();
                        break;
                    }

                    description = currentPlantDef.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = currentPlantDef.defName;
                    }

                    headerLabel = listingStandard.Label(currentPlantDef.label.CapitalizeFirst());
                }
                else
                {
                    currentBiomeDef = BiomeDef.Named(SelectedDef);
                    if (currentBiomeDef == null)
                    {
                        listingStandard.End();
                        break;
                    }

                    description = currentBiomeDef.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = currentBiomeDef.defName;
                    }

                    headerLabel = listingStandard.Label(currentBiomeDef.label.CapitalizeFirst());
                }

                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position,
                    searchSize), description);

                searchText =
                    Widgets.TextField(
                        new Rect(
                            headerLabel.position +
                            new Vector2((frameRect.width / 2) - (searchSize.x / 2) - (buttonSize.x / 2), 0),
                            searchSize),
                        searchText);
                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position + new Vector2((frameRect.width / 2) - (searchSize.x / 2), 0),
                    searchSize), "CWPS.search".Translate());

                Rect borderRect;
                Rect scrollContentRect;
                Listing_Standard scrollListing;
                bool alternate;
                float currentGlobal;
                bool forceGlobal;

                if (Instance.Settings.ReverseSettingsMode)
                {
                    if (Instance.Settings.CustomSpawnRates?.Any(pair =>
                            pair.Value.dictionary.ContainsKey(SelectedDef)) == true)
                    {
                        drawButton(() =>
                            {
                                if (currentPlantDef != null)
                                {
                                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                        "CWPS.resetone.confirm".Translate(currentPlantDef.LabelCap),
                                        delegate
                                        {
                                            Instance.Settings.ResetOnePlant(SelectedDef);
                                            var selectedPlant = ThingDef.Named(SelectedDef);
                                            foreach (var biomeDef in Main.AllBiomes)
                                            {
                                                var cachedCommonalitiesTraverse = Traverse.Create(biomeDef)
                                                    .Field("cachedPlantCommonalities");
                                                if (cachedCommonalitiesTraverse.GetValue() == null)
                                                {
                                                    _ = biomeDef.CommonalityOfPlant(selectedPlant);
                                                }

                                                var cachedPlantCommonalities =
                                                    (Dictionary<ThingDef, float>)
                                                    cachedCommonalitiesTraverse.GetValue();

                                                var commonality =
                                                    cachedPlantCommonalities.GetValueOrDefault(selectedPlant, 0f);

                                                currentPlantBiomeRecords[biomeDef] = commonality;
                                                var decimals =
                                                    (currentPlantBiomeRecords[biomeDef] -
                                                     Math.Truncate(currentPlantBiomeRecords[biomeDef]))
                                                    .ToString().Length;

                                                if (decimals < 4)
                                                {
                                                    decimals = 4;
                                                }

                                                currentPlantBiomeDecimals[biomeDef] = decimals;
                                            }
                                        }));
                                }
                            }, "CWPS.reset.button".Translate(),
                            new Vector2(headerLabel.position.x + headerLabel.width - (buttonSize.x * 2),
                                headerLabel.position.y));
                    }

                    drawButton(delegate { copyOtherPlantValues(SelectedDef); }, "CWPS.copy.button".Translate(),
                        headerLabel.position + new Vector2(frameRect.width - buttonSize.x, 0));

                    listingStandard.End();
                    var biomes = Main.AllBiomes;
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        biomes = Main.AllBiomes.Where(def =>
                                def.label.ToLower().Contains(searchText.ToLower()) || def.modContentPack?.Name.ToLower()
                                    .Contains(searchText.ToLower()) == true)
                            .ToList();
                    }

                    borderRect = frameRect;
                    borderRect.y += headerLabel.y + 30;
                    borderRect.height -= headerLabel.y + 30;
                    scrollContentRect = frameRect;
                    scrollContentRect.height = biomes.Count * 51f;
                    scrollContentRect.width -= 20;
                    scrollContentRect.x = 0;
                    scrollContentRect.y = 0;

                    scrollListing = new Listing_Standard();
                    Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
                    scrollListing.Begin(scrollContentRect);

                    alternate = true;
                    currentGlobal = globalValue;
                    globalValue =
                        (float)Math.Round((decimal)Widgets.HorizontalSlider(
                            scrollListing.GetRect(50),
                            globalValue, 0,
                            3f, false,
                            globalValue.ToString("N4")
                                .TrimEnd('0').TrimEnd('.'),
                            "CWPS.globalvalue".Translate()), 4);
                    forceGlobal = currentGlobal != globalValue;

                    foreach (var biomeDef in biomes)
                    {
                        if (forceGlobal)
                        {
                            currentPlantBiomeRecords[biomeDef] = globalValue;
                        }

                        var modInfo = biomeDef.modContentPack?.Name;
                        var rowRect = scrollListing.GetRect(50);
                        alternate = !alternate;
                        if (alternate)
                        {
                            Widgets.DrawBoxSolid(rowRect.ExpandedBy(10, 0), alternateBackground);
                        }

                        var biomeTitle = biomeDef.label.CapitalizeFirst();
                        if (biomeTitle.Length > 30)
                        {
                            biomeTitle = $"{biomeTitle.Substring(0, 27)}...";
                        }

                        if (modInfo is { Length: > 30 })
                        {
                            modInfo = $"{modInfo.Substring(0, 27)}...";
                        }

                        if (Instance.Settings.CustomSpawnRates != null && Instance.Settings
                                .CustomSpawnRates.ContainsKey(biomeDef.defName) && Instance.Settings
                                .CustomSpawnRates[biomeDef.defName].dictionary?.ContainsKey(SelectedDef) == true)
                        {
                            GUI.color = Color.green;
                        }

                        currentPlantBiomeRecords[biomeDef] =
                            (float)Math.Round(
                                (decimal)Widgets.HorizontalSlider(rowRect, currentPlantBiomeRecords[biomeDef], 0, 3f,
                                    false,
                                    currentPlantBiomeRecords[biomeDef]
                                        .ToString($"N{currentPlantBiomeDecimals[biomeDef]}").TrimEnd('0').TrimEnd('.'),
                                    biomeTitle, modInfo), currentPlantBiomeDecimals[biomeDef]);
                        GUI.color = Color.white;
                    }

                    scrollListing.End();
                    Widgets.EndScrollView();
                    break;
                }

                if (Instance.Settings.CustomSpawnRates?.ContainsKey(SelectedDef) == true ||
                    Instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    drawButton(() =>
                        {
                            if (currentBiomeDef != null)
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                    "CWPS.resetone.confirm".Translate(currentBiomeDef.LabelCap),
                                    delegate
                                    {
                                        Instance.Settings.ResetOneBiome(SelectedDef);
                                        currentBiomePlantDensity = Main.VanillaDensities[SelectedDef];
                                        var selectedBiome = BiomeDef.Named(SelectedDef);

                                        var cachedCommonailtiesTraverse = Traverse.Create(selectedBiome)
                                            .Field("cachedPlantCommonalities");
                                        if (cachedCommonailtiesTraverse.GetValue() == null)
                                        {
                                            _ = selectedBiome.CommonalityOfPlant(Main.AllPlants.First());
                                        }

                                        var cachedPlantCommonalities =
                                            (Dictionary<ThingDef, float>)cachedCommonailtiesTraverse.GetValue();

                                        foreach (var plant in Main.AllPlants)
                                        {
                                            var commonality = cachedPlantCommonalities.GetValueOrDefault(plant, 0f);

                                            currentBiomePlantRecords[plant] = commonality;
                                            var decimals =
                                                (currentBiomePlantRecords[plant] -
                                                 Math.Truncate(currentBiomePlantRecords[plant]))
                                                .ToString().Length;

                                            if (decimals < 4)
                                            {
                                                decimals = 4;
                                            }

                                            currentBiomePlantDecimals[plant] = decimals;
                                        }
                                    }));
                            }
                        }, "CWPS.reset.button".Translate(),
                        new Vector2(headerLabel.position.x + headerLabel.width - (buttonSize.x * 2),
                            headerLabel.position.y));
                }


                drawButton(delegate { copySpawnValues(SelectedDef); }, "CWPS.copy.button".Translate(),
                    headerLabel.position + new Vector2(frameRect.width - buttonSize.x, 0));

                if (Instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    GUI.color = Color.green;
                }

                listingStandard.Gap();
                currentBiomePlantDensity =
                    (float)Math.Round((decimal)Widgets.HorizontalSlider(
                        listingStandard.GetRect(50),
                        currentBiomePlantDensity, 0,
                        3f, false,
                        currentBiomePlantDensity.ToString(),
                        "CWPS.commonality.label".Translate()), 4);
                GUI.color = Color.white;

                listingStandard.End();

                var plants = Main.AllPlants;
                if (!string.IsNullOrEmpty(searchText))
                {
                    plants = Main.AllPlants.Where(def =>
                            def.label.ToLower().Contains(searchText.ToLower()) || def.modContentPack?.Name?.ToLower()
                                .Contains(searchText.ToLower()) == true)
                        .ToList();
                }

                borderRect = frameRect;
                borderRect.y += headerLabel.y + 90;
                borderRect.height -= headerLabel.y + 90;
                scrollContentRect = frameRect;
                scrollContentRect.height = plants.Count * 51f;
                scrollContentRect.width -= 20;
                scrollContentRect.x = 0;
                scrollContentRect.y = 0;

                scrollListing = new Listing_Standard();
                Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
                scrollListing.Begin(scrollContentRect);

                alternate = false;
                currentGlobal = globalValue;
                globalValue =
                    (float)Math.Round((decimal)Widgets.HorizontalSlider(
                        scrollListing.GetRect(50),
                        globalValue, 0,
                        10f, false,
                        globalValue.ToString("N4")
                            .TrimEnd('0').TrimEnd('.'),
                        "CWPS.globalvalue".Translate()), 4);
                forceGlobal = currentGlobal != globalValue;
                foreach (var plant in plants)
                {
                    if (forceGlobal)
                    {
                        currentBiomePlantRecords[plant] = globalValue;
                    }

                    var modInfo = plant.modContentPack?.Name;
                    var rowRect = scrollListing.GetRect(50);
                    alternate = !alternate;
                    if (alternate)
                    {
                        Widgets.DrawBoxSolid(rowRect.ExpandedBy(10, 0), alternateBackground);
                    }

                    var sliderRect = new Rect(rowRect.position + new Vector2(iconSize.x, 0),
                        rowRect.size - new Vector2(iconSize.x, 0));
                    var plantTitle = plant.label.CapitalizeFirst();
                    if (plantTitle.Length > 30)
                    {
                        plantTitle = $"{plantTitle.Substring(0, 27)}...";
                    }

                    if (modInfo is { Length: > 30 })
                    {
                        modInfo = $"{modInfo.Substring(0, 27)}...";
                    }

                    if (Instance.Settings.CustomSpawnRates != null &&
                        Instance.Settings.CustomSpawnRates.ContainsKey(SelectedDef) && Instance.Settings
                            .CustomSpawnRates[SelectedDef]?.dictionary?.ContainsKey(plant.defName) ==
                        true)
                    {
                        GUI.color = Color.green;
                    }

                    currentBiomePlantRecords[plant] = (float)Math.Round((decimal)Widgets.HorizontalSlider(
                        sliderRect,
                        currentBiomePlantRecords[plant], 0,
                        10f, false,
                        currentBiomePlantRecords[plant].ToString($"N{currentBiomePlantDecimals[plant]}")
                            .TrimEnd('0').TrimEnd('.'), plantTitle,
                        modInfo), currentBiomePlantDecimals[plant]);
                    GUI.color = Color.white;
                    drawIcon(plant,
                        new Rect(rowRect.position, iconSize));
                }

                scrollListing.End();
                Widgets.EndScrollView();
                break;
            }
        }
    }

    private static void copySpawnValues(string originalDef)
    {
        var list = new List<FloatMenuOption>();

        foreach (var biome in Main.AllBiomes.Where(biomeDef => biomeDef.defName != originalDef))
        {
            list.Add(new FloatMenuOption(biome.LabelCap, action));
            continue;

            void action()
            {
                Main.LogMessage($"Copying overall plant density from {biome.defName} to {originalDef}");
                currentBiomePlantDensity = Main.VanillaDensities[biome.defName];
                if (Instance.Settings.CustomDensities.TryGetValue(biome.defName, out var density))
                {
                    currentBiomePlantDensity = density;
                }

                foreach (var plant in Main.AllPlants)
                {
                    currentBiomePlantRecords[plant] =
                        biome.CommonalityOfPlant(plant);
                    var decimals =
                        (currentBiomePlantRecords[plant] -
                         Math.Truncate(currentBiomePlantRecords[plant]))
                        .ToString().Length;

                    if (decimals < 4)
                    {
                        decimals = 4;
                    }

                    currentBiomePlantDecimals[plant] = decimals;
                }

                SelectedDef = originalDef;
            }
        }

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private static void copyOtherPlantValues(string originalDef)
    {
        var list = new List<FloatMenuOption>();

        foreach (var plant in Main.AllPlants.Where(plantDef => plantDef.defName != originalDef))
        {
            list.Add(new FloatMenuOption(plant.LabelCap, action));
            continue;

            void action()
            {
                Main.LogMessage($"Setting spawnrate from {plant.defName}");

                foreach (var biomeDef in Main.AllBiomes)
                {
                    var cachedCommonailtiesTraverse = Traverse.Create(biomeDef)
                        .Field("cachedPlantCommonalities");
                    if (cachedCommonailtiesTraverse.GetValue() == null)
                    {
                        _ = biomeDef.CommonalityOfPlant(plant);
                    }

                    var cachedPlantCommonalities =
                        (Dictionary<ThingDef, float>)cachedCommonailtiesTraverse.GetValue();

                    var commonality = cachedPlantCommonalities.GetValueOrDefault(plant, 0f);

                    currentPlantBiomeRecords[biomeDef] = commonality;
                    var decimals =
                        (currentPlantBiomeRecords[biomeDef] -
                         Math.Truncate(currentPlantBiomeRecords[biomeDef]))
                        .ToString().Length;

                    if (decimals < 4)
                    {
                        decimals = 4;
                    }

                    currentPlantBiomeDecimals[biomeDef] = decimals;
                }

                SelectedDef = originalDef;
            }
        }

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private static void drawTabsList(Rect rect)
    {
        var scrollContainer = rect.ContractedBy(10);
        scrollContainer.width = leftSideWidth;
        Widgets.DrawBoxSolid(scrollContainer, Color.grey);
        var innerContainer = scrollContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
        var tabFrameRect = innerContainer.ContractedBy(5);
        tabFrameRect.y += 15;
        tabFrameRect.height -= 15;
        var tabContentRect = tabFrameRect;
        tabContentRect.x = 0;
        tabContentRect.y = 0;
        tabContentRect.width -= 20;
        var allBiomes = Main.AllBiomes;
        var allPlants = Main.AllPlants;
        const int listAddition = 50;

        var height = (allBiomes.Count * 27f) + listAddition;

        if (Instance.Settings.ReverseSettingsMode)
        {
            height = allPlants.Count * 27f;
        }

        tabContentRect.height = height;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listingStandard.Begin(tabContentRect);
        if (listingStandard.ListItemSelectable("CWPS.settings".Translate(), Color.yellow, SelectedDef == "Settings"))
        {
            SelectedDef = SelectedDef == "Settings" ? null : "Settings";
        }

        listingStandard.ListItemSelectable(null, Color.yellow);
        string toolTip;
        if (Instance.Settings.ReverseSettingsMode)
        {
            foreach (var plantDef in allPlants)
            {
                toolTip = $"{plantDef.defName} ({plantDef.modContentPack?.Name})\n{plantDef.description}";
                if (Instance.Settings.CustomSpawnRates?.Any(pair =>
                        pair.Value.dictionary.ContainsKey(plantDef.defName)) == true)
                {
                    GUI.color = Color.green;
                    toolTip = "CWPS.customexists".Translate();
                }

                if (listingStandard.ListItemSelectable(plantDef.label.CapitalizeFirst(), Color.yellow,
                        SelectedDef == plantDef.defName, false, toolTip))
                {
                    SelectedDef = SelectedDef == plantDef.defName ? null : plantDef.defName;
                }

                GUI.color = Color.white;
            }

            listingStandard.End();
            Widgets.EndScrollView();
            return;
        }

        toolTip = string.Empty;
        if (Instance.Settings.CustomCaveWeights?.Any() == true)
        {
            GUI.color = Color.green;
            toolTip = "CWPS.customexists".Translate();
        }

        if (listingStandard.ListItemSelectable("CWPS.caves".Translate(), Color.yellow, SelectedDef == "Caves", false,
                toolTip))
        {
            SelectedDef = SelectedDef == "Caves" ? null : "Caves";
        }

        GUI.color = Color.white;

        listingStandard.ListItemSelectable(null, Color.yellow);
        foreach (var biomeDef in allBiomes)
        {
            toolTip = $"{biomeDef.defName} ({biomeDef.modContentPack?.Name})\n{biomeDef.description}";
            if (Instance.Settings.CustomSpawnRates.ContainsKey(biomeDef.defName) ||
                Instance.Settings.CustomDensities.ContainsKey(biomeDef.defName))
            {
                GUI.color = Color.green;
                toolTip += "\n" + "CWPS.customexists".Translate();
            }

            if (listingStandard.ListItemSelectable(biomeDef.label.CapitalizeFirst(), Color.yellow,
                    SelectedDef == biomeDef.defName, false, toolTip))
            {
                SelectedDef = SelectedDef == biomeDef.defName ? null : biomeDef.defName;
            }

            GUI.color = Color.white;
        }

        listingStandard.End();
        Widgets.EndScrollView();
    }
}