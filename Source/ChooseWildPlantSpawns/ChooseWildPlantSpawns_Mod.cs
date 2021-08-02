using System;
using System.Collections.Generic;
using System.Linq;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseWildPlantSpawns.Settings
{
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

        private static string selectedDef = "Settings";

        private static string searchText = "";


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
            ParseHelper.Parsers<SaveableDictionary>.Register(SaveableDictionary.FromString);
            if (instance.Settings.CustomSpawnRates == null)
            {
                instance.Settings.CustomSpawnRates = new Dictionary<string, SaveableDictionary>();
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
                if (selectedDef != null && selectedDef != "Settings")
                {
                    saveBiomeSettings();
                    Main.ApplyBiomeSettings();
                }

                currentBiomePlantRecords = new Dictionary<ThingDef, float>();
                selectedDef = value;

                if (value == null || value == "Settings")
                {
                    return;
                }

                var selectedBiome = BiomeDef.Named(selectedDef);
                foreach (var plant in Main.AllPlants)
                {
                    currentBiomePlantRecords[plant] = selectedBiome.CommonalityOfPlant(plant);
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
            if (currentBiomePlantRecords == null)
            {
                currentBiomePlantRecords = new Dictionary<ThingDef, float>();
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
                Main.LogMessage($"currentBiomeList for {SelectedDef} empty");
                return;
            }

            instance.Settings.CustomSpawnRates[SelectedDef] = new SaveableDictionary(currentBiomeList);
            currentBiomePlantRecords = new Dictionary<ThingDef, float>();
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
                texture2D = ((Graphic_Random) thingDef.graphicData.Graphic)?.FirstSubgraphic().MatSingle.mainTexture;
            }

            if (thingDef.graphicData?.graphicClass == typeof(Graphic_StackCount))
            {
                texture2D = ((Graphic_StackCount) thingDef.graphicData.Graphic)?.SubGraphicForStackCount(1, thingDef)
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
                var ratio = (float) texture2D.width / texture2D.height;

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

                    if (instance.Settings.CustomSpawnRates?.Any() == true)
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

                default:
                {
                    var currentDef = BiomeDef.Named(SelectedDef);
                    listing_Standard.Begin(frameRect);
                    if (currentDef == null)
                    {
                        listing_Standard.End();
                        break;
                    }

                    Text.Font = GameFont.Medium;
                    var description = currentDef.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = currentDef.defName;
                    }

                    var headerLabel = listing_Standard.Label(currentDef.label.CapitalizeFirst());
                    TooltipHandler.TipRegion(new Rect(
                        headerLabel.position,
                        searchSize), description);


                    if (instance.Settings.CustomSpawnRates?.ContainsKey(SelectedDef) == true)
                    {
                        DrawButton(() =>
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                    "CWPS.resetone.confirm".Translate(currentDef.LabelCap),
                                    delegate
                                    {
                                        instance.Settings.ResetOneValue(SelectedDef);
                                        var selectedBiome = BiomeDef.Named(SelectedDef);
                                        foreach (var plant in Main.AllPlants)
                                        {
                                            currentBiomePlantRecords[plant] =
                                                selectedBiome.CommonalityOfPlant(plant);
                                        }
                                    }));
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

                    var plants = Main.AllPlants;
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        plants = Main.AllPlants.Where(def => def.label.ToLower().Contains(searchText.ToLower()))
                            .ToList();
                    }

                    var borderRect = frameRect;
                    borderRect.y += headerLabel.y + 40;
                    borderRect.height -= headerLabel.y + 40;
                    var scrollContentRect = frameRect;
                    scrollContentRect.height = plants.Count * 51f;
                    scrollContentRect.width -= 20;
                    scrollContentRect.x = 0;
                    scrollContentRect.y = 0;

                    var scrollListing = new Listing_Standard();
                    BeginScrollView(ref scrollListing, borderRect, ref scrollPosition, ref scrollContentRect);
                    foreach (var plant in plants)
                    {
                        var modInfo = plant.modContentPack?.Name;
                        var rowRect = scrollListing.GetRect(50);
                        var sliderRect = new Rect(rowRect.position + new Vector2(iconSize.x, 0),
                            rowRect.size - new Vector2(iconSize.x, 0));
                        var plantTitle = plant.label.CapitalizeFirst();
                        if (plantTitle.Length > 30)
                        {
                            plantTitle = $"{plantTitle.Substring(0, 27)}...";
                        }

                        if (modInfo is {Length: > 30})
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

                        currentBiomePlantRecords[plant] = (float) Math.Round((decimal) Widgets.HorizontalSlider(
                            sliderRect,
                            currentBiomePlantRecords[plant], 0,
                            10f, false, currentBiomePlantRecords[plant].ToString(), plantTitle,
                            modInfo, 0.0001f), 4);
                        GUI.color = Color.white;
                        DrawIcon(plant,
                            new Rect(rowRect.position, iconSize));
                    }

                    EndScrollView(ref scrollListing, ref borderRect, frameRect.width, listing_Standard.CurHeight);
                    break;
                }
            }
        }

        private static void BeginScrollView(ref Listing_Standard listingStandard, Rect rect, ref Vector2 position,
            ref Rect viewRect)
        {
            Widgets.BeginScrollView(rect, ref position, viewRect);
            rect.height = 100000f;
            rect.width -= 20f;
            listingStandard.Begin(rect.AtZero());
        }

        private void EndScrollView(ref Listing_Standard listingStandard, ref Rect viewRect, float width, float height)
        {
            viewRect = new Rect(0f, 0f, width, height);
            Widgets.EndScrollView();
            listingStandard.End();
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
            var listAddition = 24;

            tabContentRect.height = (allBiomes.Count * 25f) + listAddition;
            BeginScrollView(ref listing_Standard, tabFrameRect, ref tabsScrollPosition, ref tabContentRect);
            //Text.Font = GameFont.Tiny;
            if (listing_Standard.ListItemSelectable("CWPS.settings".Translate(), Color.yellow,
                out _, SelectedDef == "Settings"))
            {
                SelectedDef = SelectedDef == "Settings" ? null : "Settings";
            }

            listing_Standard.ListItemSelectable(null, Color.yellow, out _);
            foreach (var biomeDef in allBiomes)
            {
                var toolTip = string.Empty;
                if (instance.Settings.CustomSpawnRates.ContainsKey(biomeDef.defName))
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
            EndScrollView(ref listing_Standard, ref tabContentRect, tabFrameRect.width, listing_Standard.CurHeight);
        }
    }
}