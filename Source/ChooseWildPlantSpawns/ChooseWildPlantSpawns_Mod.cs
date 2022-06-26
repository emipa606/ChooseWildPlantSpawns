using System;
using System.Collections.Generic;
using System.Linq;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseWildPlantSpawns.Settings;

public class ChooseWildPlantSpawns_Mod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static ChooseWildPlantSpawns_Mod instance;

    private static readonly Vector2 buttonSize = new Vector2(120f, 25f);

    private static readonly Vector2 searchSize = new Vector2(200f, 25f);

    private static readonly Vector2 iconSize = new Vector2(48f, 48f);

    private static readonly int buttonSpacer = 200;

    private static readonly float columnSpacer = 0.1f;

    private static float leftSideWidth;

    private static Listing_Standard listing_Standard;

    private static Vector2 tabsScrollPosition;

    private static string currentVersion;

    private static Vector2 scrollPosition;

    private static Dictionary<ThingDef, float> currentBiomePlantRecords;
    private static Dictionary<ThingDef, int> currentBiomePlantDecimals;

    private static float currentBiomePlantDensity;

    private static string selectedDef = "Settings";

    private static string searchText = "";

    private static readonly Color alternateBackground = new Color(0.1f, 0.1f, 0.1f, 0.5f);

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
        instance = this;
        searchText = string.Empty;
        ParseHelper.Parsers<SaveableDictionary>.Register(SaveableDictionary.FromString);
        if (instance.Settings.CustomSpawnRates == null)
        {
            instance.Settings.CustomSpawnRates = new Dictionary<string, SaveableDictionary>();
        }

        if (instance.Settings.CustomDensities == null)
        {
            instance.Settings.CustomDensities = new Dictionary<string, float>();
        }

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(
                ModLister.GetActiveModWithIdentifier("Mlie.ChooseWildPlantSpawns"));
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal ChooseWildPlantSpawns_Settings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = GetSettings<ChooseWildPlantSpawns_Settings>();
            }

            return settings;
        }

        set => settings = value;
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

            currentBiomePlantRecords = new Dictionary<ThingDef, float>();
            currentBiomePlantDecimals = new Dictionary<ThingDef, int>();
            currentBiomePlantDensity = 0;
            selectedDef = value;

            if (value == null || value == "Settings" || value == "Caves")
            {
                return;
            }

            var selectedBiome = BiomeDef.Named(selectedDef);
            currentBiomePlantDensity = selectedBiome.plantDensity;
            foreach (var plant in Main.AllPlants)
            {
                currentBiomePlantRecords[plant] = selectedBiome.CommonalityOfPlant(plant);
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
        try
        {
            if (SelectedDef == "Settings" || SelectedDef == "Caves")
            {
                return;
            }

            if (currentBiomePlantDensity == Main.VanillaDensities[SelectedDef])
            {
                if (instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    instance.Settings.CustomDensities.Remove(SelectedDef);
                }
            }
            else
            {
                instance.Settings.CustomDensities[SelectedDef] = currentBiomePlantDensity;
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
                if (instance.Settings.CustomSpawnRates.ContainsKey(SelectedDef))
                {
                    instance.Settings.CustomSpawnRates.Remove(SelectedDef);
                }

                currentBiomePlantRecords = new Dictionary<ThingDef, float>();
                currentBiomePlantDecimals = new Dictionary<ThingDef, int>();
                Main.LogMessage($"currentBiomeList for {SelectedDef} empty");
                return;
            }

            instance.Settings.CustomSpawnRates[SelectedDef] = new SaveableDictionary(currentBiomeList);
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

        listing_Standard = new Listing_Standard();

        DrawOptions(rect2);
        DrawTabsList(rect2);
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


    private static void DrawButton(Action action, string text, Vector2 pos)
    {
        var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }


    private void DrawIcon(ThingDef thingDef, Rect rect)
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

    private void DrawOptions(Rect rect)
    {
        var optionsOuterContainer = rect.ContractedBy(10);
        optionsOuterContainer.x += leftSideWidth + columnSpacer;
        optionsOuterContainer.width -= leftSideWidth + columnSpacer;
        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = optionsInnerContainer.ContractedBy(10);
        frameRect.x = leftSideWidth + columnSpacer + 20;
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
                listing_Standard.Begin(frameRect);
                Text.Font = GameFont.Medium;
                listing_Standard.Label("CWPS.settings".Translate());
                Text.Font = GameFont.Small;
                listing_Standard.Gap();

                if (instance.Settings.CustomSpawnRates?.Any() == true ||
                    instance.Settings.CustomDensities?.Any() == true ||
                    instance.Settings.CustomCaveWeights?.Any() == true)
                {
                    var labelPoint = listing_Standard.Label("CWPS.resetall.label".Translate(), -1F,
                        "CWPS.resetall.tooltip".Translate());
                    DrawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWPS.resetall.confirm".Translate(),
                                delegate { instance.Settings.ResetManualValues(); }));
                        }, "CWPS.resetall.button".Translate(),
                        new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                }

                listing_Standard.CheckboxLabeled("CWPS.logging.label".Translate(), ref Settings.VerboseLogging,
                    "CWPS.logging.tooltip".Translate());
                if (currentVersion != null)
                {
                    listing_Standard.Gap();
                    GUI.contentColor = Color.gray;
                    listing_Standard.Label("CWPS.version.label".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }

                listing_Standard.End();
                break;
            }
            case "Caves":
            {
                listing_Standard.Begin(frameRect);
                Text.Font = GameFont.Medium;
                var description = "CWPS.caves.description".Translate();
                var headerLabel = listing_Standard.Label("CWPS.caves".Translate());
                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position,
                    searchSize), description);

                if (instance.Settings.CustomCaveWeights == null)
                {
                    instance.Settings.CustomCaveWeights = new Dictionary<string, float>();
                }

                if (instance.Settings.CustomCaveWeights.Any())
                {
                    DrawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWPS.resetone.confirm".Translate(SelectedDef.Translate()),
                                delegate { instance.Settings.ResetOneValue(SelectedDef); }));
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

                listing_Standard.End();

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
                        instance.Settings.CustomCaveWeights[cavePlant.defName] =
                            cavePlant.plant.cavePlantWeight;
                        GUI.color = Color.green;
                    }
                    else
                    {
                        if (instance.Settings.CustomCaveWeights.ContainsKey(cavePlant.defName))
                        {
                            instance.Settings.CustomCaveWeights.Remove(cavePlant.defName);
                        }
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
                    DrawIcon(cavePlant,
                        new Rect(rowRect.position, iconSize));
                }

                scrollListing.End();
                Widgets.EndScrollView();
                break;
            }

            default:
            {
                var currentDef = BiomeDef.Named(SelectedDef);
                listing_Standard.Begin(frameRect);
                if (currentDef == null)
                {
                    listing_Standard.End();
                    break;
                }

                var description = currentDef.description;
                if (string.IsNullOrEmpty(description))
                {
                    description = currentDef.defName;
                }

                var headerLabel = listing_Standard.Label(currentDef.label.CapitalizeFirst());
                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position,
                    searchSize), description);


                if (instance.Settings.CustomSpawnRates?.ContainsKey(SelectedDef) == true ||
                    instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    DrawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWPS.resetone.confirm".Translate(currentDef.LabelCap),
                                delegate
                                {
                                    instance.Settings.ResetOneValue(SelectedDef);
                                    currentBiomePlantDensity = Main.VanillaDensities[SelectedDef];
                                    var selectedBiome = BiomeDef.Named(SelectedDef);
                                    foreach (var plant in Main.AllPlants)
                                    {
                                        currentBiomePlantRecords[plant] =
                                            selectedBiome.CommonalityOfPlant(plant);
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
                        }, "CWPS.reset.button".Translate(),
                        new Vector2(headerLabel.position.x + headerLabel.width - (buttonSize.x * 2),
                            headerLabel.position.y));
                }

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

                DrawButton(delegate { CopySpawnValues(SelectedDef); }, "CWPS.copy.button".Translate(),
                    headerLabel.position + new Vector2(frameRect.width - buttonSize.x, 0));

                if (instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    GUI.color = Color.green;
                }

                listing_Standard.Gap();
                currentBiomePlantDensity =
                    (float)Math.Round((decimal)Widgets.HorizontalSlider(
                        listing_Standard.GetRect(50),
                        currentBiomePlantDensity, 0,
                        3f, false,
                        currentBiomePlantDensity.ToString(),
                        "CWPS.commonality.label".Translate()), 4);
                GUI.color = Color.white;

                listing_Standard.End();

                var plants = Main.AllPlants;
                if (!string.IsNullOrEmpty(searchText))
                {
                    plants = Main.AllPlants.Where(def =>
                            def.label.ToLower().Contains(searchText.ToLower()) || def.modContentPack?.Name?.ToLower()
                                .Contains(searchText.ToLower()) == true)
                        .ToList();
                }

                var borderRect = frameRect;
                borderRect.y += headerLabel.y + 90;
                borderRect.height -= headerLabel.y + 90;
                var scrollContentRect = frameRect;
                scrollContentRect.height = plants.Count * 51f;
                scrollContentRect.width -= 20;
                scrollContentRect.x = 0;
                scrollContentRect.y = 0;

                var scrollListing = new Listing_Standard();
                Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
                scrollListing.Begin(scrollContentRect);

                var alternate = false;
                foreach (var plant in plants)
                {
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

                    if (instance.Settings.CustomSpawnRates != null &&
                        instance.Settings.CustomSpawnRates.ContainsKey(SelectedDef) && instance.Settings
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
                    DrawIcon(plant,
                        new Rect(rowRect.position, iconSize));
                }

                scrollListing.End();
                Widgets.EndScrollView();
                break;
            }
        }
    }

    private static void CopySpawnValues(string originalDef)
    {
        var list = new List<FloatMenuOption>();

        foreach (var biome in Main.AllBiomes.Where(biomeDef => biomeDef.defName != originalDef))
        {
            void action()
            {
                Main.LogMessage($"Copying overall plant density from {biome.defName} to {originalDef}");
                currentBiomePlantDensity = Main.VanillaDensities[biome.defName];
                if (instance.Settings.CustomDensities.ContainsKey(biome.defName))
                {
                    currentBiomePlantDensity = instance.Settings.CustomDensities[biome.defName];
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

            list.Add(new FloatMenuOption(biome.LabelCap, action));
        }

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private void DrawTabsList(Rect rect)
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
        var listAddition = 50;

        tabContentRect.height = (allBiomes.Count * 27f) + listAddition;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listing_Standard.Begin(tabContentRect);
        //Text.Font = GameFont.Tiny;
        if (listing_Standard.ListItemSelectable("CWPS.settings".Translate(), Color.yellow,
                out _, SelectedDef == "Settings"))
        {
            SelectedDef = SelectedDef == "Settings" ? null : "Settings";
        }

        listing_Standard.ListItemSelectable(null, Color.yellow, out _);

        var toolTip = string.Empty;
        if (instance.Settings.CustomCaveWeights?.Any() == true)
        {
            GUI.color = Color.green;
            toolTip = "CWPS.customexists".Translate();
        }

        if (listing_Standard.ListItemSelectable("CWPS.caves".Translate(), Color.yellow,
                out _, SelectedDef == "Caves", false, toolTip))
        {
            SelectedDef = SelectedDef == "Caves" ? null : "Caves";
        }

        GUI.color = Color.white;

        listing_Standard.ListItemSelectable(null, Color.yellow, out _);
        foreach (var biomeDef in allBiomes)
        {
            toolTip = string.Empty;
            if (instance.Settings.CustomSpawnRates.ContainsKey(biomeDef.defName) ||
                instance.Settings.CustomDensities.ContainsKey(biomeDef.defName))
            {
                GUI.color = Color.green;
                toolTip = "CWPS.customexists".Translate();
            }

            if (listing_Standard.ListItemSelectable(biomeDef.label.CapitalizeFirst(), Color.yellow,
                    out _,
                    SelectedDef == biomeDef.defName, false, toolTip))
            {
                SelectedDef = SelectedDef == biomeDef.defName ? null : biomeDef.defName;
            }

            GUI.color = Color.white;
        }

        //Text.Font = GameFont.Small;
        listing_Standard.End();
        Widgets.EndScrollView();
    }
}