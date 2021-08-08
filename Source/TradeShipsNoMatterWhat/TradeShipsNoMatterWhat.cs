﻿using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace TradeShipsNoMatterWhat
{

    [StaticConstructorOnStartup]
    public static class MainLoader
    {
        public static string lastDocumentElementName;

        public static int staticMax = 1;
        public static int staticMin = 0;
        public static bool staticUraniumTraders;
        public static Map thisMap;

        //public static System.Random rNum = new System.Random();
        //public static int ticksTillNextShip = rNum.Next(staticMin * 60000, staticMax * 60000);
        public static long ticksTillNextShip = 60000;

        static MainLoader()
        {
            Log.Message("trade ships no matter what loaded");
            var harmony = new Harmony("TradeShipsNoMatterWhat");
            harmony.PatchAll();
            Log.Message("patched harmony assembly");

        }

        public static void makeTradersHaveUranium()
        {
            Log.Message("passingSHips.Size = " + MainLoader.thisMap.passingShipManager.passingShips.Count.ToString());
            TradeShip yeah = (TradeShip)MainLoader.thisMap.passingShipManager.passingShips[0];
            Log.Message("trader kind = " + yeah.TraderKind.orbital);
            //StockGenerator newStockVar = yeah.TraderKind.stockGenerators[0];
            Log.Message("step 1");
            StockGenerator_SingleDef newStockGen = new StockGenerator_SingleDef();
            Log.Message("step 2");
            newStockGen.countRange = IntRange.FromString("38~500");
            Log.Message("step 3");
            //ThingDefCountRangeClass newCount = new ThingDefCountRangeClass(ThingDefOf.Uranium, 38, 500);
            //Log.Message("Step 2");
            //yeah.TraderKind.stockGenerators.Add(StockGenerator_SingleDef)
        }

        public static void makeTraderHaveUranium(TradeShip theShip)
        {
            StockGenerator_SingleDef uranium = new StockGenerator_SingleDef();
            uranium.countRange = IntRange.FromString("100~400");
            uranium.HandlesThingDef(ThingDefOf.Uranium);
            theShip.def.stockGenerators.AddItem(uranium);
            theShip.GenerateThings();


        }

    }

    [HarmonyPatch(typeof(ScribeSaver))]
    [HarmonyPatch(nameof(ScribeSaver.InitSaving))]
    static class saveInfoGetter
    {
        static void Prefix(string filePath, string documentElementName)
        {
            MainLoader.lastDocumentElementName = documentElementName;
        }
    }

    [HarmonyPatch(typeof(ScribeSaver))]
    [HarmonyPatch(nameof(ScribeSaver.FinalizeSaving))]
    static class SaveGameFinalizeMod
    {
        static void Prefix()
        {
            if (MainLoader.lastDocumentElementName == "savegame")
            {
                Log.Message("attemtping to inject via FinalizeSaving()");
                Scribe.saver.EnterNode("TSNMW");
                Scribe.saver.WriteAttribute("ticksTillNextShip", MainLoader.ticksTillNextShip.ToString());
                Scribe.saver.WriteAttribute("min", MainLoader.staticMin.ToString());
                Scribe.saver.WriteAttribute("max", MainLoader.staticMax.ToString());

                Scribe.saver.ExitNode();
                Log.Message("done");
            }
            else
            {
                Log.Message("open file is of type " + MainLoader.lastDocumentElementName + "and not a save game *.rws file");
            }

        }
    }

    [HarmonyPatch(typeof(ScribeLoader))]
    [HarmonyPatch(nameof(ScribeLoader.FinalizeLoading))]
    static class LoadGameFinalizeMod
    {
        static void Prefix()
        {
            try
            {
                Log.Message("Attempting to load values from save file and inside xmlnode " + Scribe.loader.curXmlParent);
                if (Scribe.loader.curXmlParent.Name == "game")
                {
                    Log.Message("Exiting the game xml node");
                    Scribe.loader.ExitNode();
                }
                //Scribe.loader.EnterNode("savegame");
                int tempMin = Convert.ToInt32(Scribe.loader.curXmlParent["TSNMW"].Attributes["min"].Value);
                int tempMax = Convert.ToInt32(Scribe.loader.curXmlParent["TSNMW"].Attributes["max"].Value);
                MainLoader.ticksTillNextShip = Convert.ToInt64(Scribe.loader.curXmlParent["TSNMW"].Attributes["ticksTillNextShip"].Value);
                //XmlAttributeCollection attibs = Scribe.loader.curXmlParent.Attributes;
                //Log.Message(attibs.Item(1).ToString());
                //Log.Message(attibs.Item(2).ToString());
                Scribe.loader.ExitNode();
                //Log.Message(attibs["ticksTillNextShip"].Value);
                Log.Message("value loading done");

                if (tempMin != MainLoader.staticMin || tempMax != MainLoader.staticMax)
                {
                    //the value got changed while the save wasn't loaded
                    Log.Message("Trade ships no matter what has had it's settings changed since last time this save was run. Regenerating timeTillNextShip");
                    //regenerate random number
                    System.Random rNum = new System.Random();
                    MainLoader.ticksTillNextShip = rNum.Next(MainLoader.staticMin * 60000, MainLoader.staticMax * 60000);
                    Log.Message("There will be " + MainLoader.ticksTillNextShip + " ticks until the next ship shows up");
                }
                Log.Message("ticksTillNextShip = " + MainLoader.ticksTillNextShip);
            }
            catch (Exception e)
            {
                Log.Message("Not loading a save game (exception caught)");
            }
        }
    }

    //in order to ensure "trade ships no matter what" lives up to its name, it's hijacking the game tick bypassing any and all possible things such as mod incompatibilites, mods chaning difficulty or Randy being an asshole which may block the trade ships
    [HarmonyPatch(typeof(TickManager))]
    [HarmonyPatch(nameof(TickManager.DoSingleTick))]
    static class HiJackGameTicker
    {
        static void Prefix()
        {
            //run this on every tick
            MainLoader.ticksTillNextShip--;
            if (MainLoader.ticksTillNextShip <= 0)
            {
                //make a ship show up
                IncidentParms paramss = new IncidentParms();
                //player

                //if player has multiple bases, pick one at random.
                //otherwise, spawn a trade ship on the 1 map owned by the player
                Map thisMap = Find.RandomPlayerHomeMap;
                if (thisMap != null)
                {
                    paramss.target = thisMap;
                    MainLoader.thisMap = thisMap;
                    IncidentDefOf.OrbitalTraderArrival.Worker.TryExecute(paramss);
                }
                else
                {
                    Log.Message("[TradeShipsNoMatterWhat] player does not own a map valid map tile");
                }
                //regenerate random number
                System.Random rNum = new System.Random();
                MainLoader.ticksTillNextShip = rNum.Next(MainLoader.staticMin * 60000, MainLoader.staticMax * 60000);
                Log.Message("There will be " + MainLoader.ticksTillNextShip + " ticks until the next ship shows up");

                //MainLoader.makeTradersHaveUranium();
            }
        }
    }


    public class TradeShipsNoMatterWhatSettings : ModSettings
    {
        public string name = "TSNMW";
        public int loadID = 0;
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        /// 
        // the feature to force uranium traders doesn't work. It's too well locked down with private variables on everything to perform this programmatically
        //public bool forceUraniumTraders;
        //public float exampleFloat = 200f;
        public int minDays = 3;
        public int maxDays = 7;
        public bool saveFileFunctionality;
        //public List<Pawn> exampleListOfPawns = new List<Pawn>();

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            //there's no way to get this option to work (I would love to be proven wrong) so I'm disabling it.
            //Scribe_Values.Look(ref forceUraniumTraders, "forceUraniumTraders");


            Scribe_Values.Look(ref minDays, "minDays", 3);
            Scribe_Values.Look(ref maxDays, "maxDays", 7);
            //Scribe_Collections.Look(ref exampleListOfPawns, "exampleListOfPawns", LookMode.Reference);

            //MainLoader.staticUraniumTraders = forceUraniumTraders;
            //MainLoader.ticksTillNextShip = rNum.Next(minDays * 60000, maxDays * 60000);
            base.ExposeData();
            MainLoader.staticMax = maxDays;
            MainLoader.staticMin = minDays;
            System.Random rNum = new System.Random();
            MainLoader.ticksTillNextShip = rNum.Next(minDays * 60000, maxDays * 60000);
            Log.Message("There will be " + MainLoader.ticksTillNextShip + " ticks until the next ship shows up");
        }
    }

    public class TradeShipsNoMatterWhat : Mod
    {

        /// <summary>
        /// A reference to our settings.
        /// </summary>
        public TradeShipsNoMatterWhatSettings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public TradeShipsNoMatterWhat(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<TradeShipsNoMatterWhatSettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            //disable uranium traders feature since it doesn't work
            //listingStandard.CheckboxLabeled("force uranium traders", ref settings.forceUraniumTraders, "forces there to be 1 or more bulk goods trader that carries uranium. Some times, the game spawns bulk goods traders with uranium upon creation of a new game and sometimes it doesn't. I think that's bullshit.");
            listingStandard.Label("Minimum days between orbital trader arrivals: " + settings.minDays);
            settings.minDays = Convert.ToInt32(Math.Floor(listingStandard.Slider(settings.minDays, 0, 119)));
            if (settings.maxDays <= settings.minDays) settings.maxDays = settings.minDays + 1;
            listingStandard.Label("Maximum days between orbital trader arrivals: " + settings.maxDays);
            settings.maxDays = Convert.ToInt32(Math.Floor(listingStandard.Slider(settings.maxDays, 1, 120)));
            if (settings.minDays >= settings.maxDays) settings.minDays = settings.maxDays - 1;
            listingStandard.CheckboxLabeled("disable save file xml entry", ref settings.saveFileFunctionality, "Trade Ships No Matter What saves an xml node to your save file to avoid regenerating the trade ship arrival time each time you load the game. This is the only thing could could possibly break compatibility in future rimworld versions/other similar mods/etc. If you are having problems, turn this option off. It must be noted that you do not have to start a new save for the changes to take effect regardless of what this is set to.");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            //return "MyExampleModName".Translate();
            return "Trade Ships No Matter What";
        }
    }
}
