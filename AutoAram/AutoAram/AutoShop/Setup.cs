﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoAram.AutoShop.Sequences;
using EloBuddy;

namespace AutoAram.AutoShop
{
    /// <summary>
    ///     The class where AutoShop is set-up
    /// </summary>
    internal class Setup
    {
        /// <summary>
        ///     Path to the build folder, containing all the champion builds
        /// </summary>
        public static readonly string BuildPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                                  "\\EloBuddy\\AutoAram\\Builds";

        /// <summary>
        ///     Path to the temporary folder which contains the in-game cache
        /// </summary>
        public static readonly string TempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                                 "\\EloBuddy\\AutoAram\\temp";

        /// <summary>
        ///     A Dictionary that contains all the builds detected
        ///     in ChampionName:BuildData format
        /// </summary>
        public static Dictionary<string, string> Builds = new Dictionary<string, string>();

        /// <summary>
        ///     The build detected for the current champion that
        ///     is being played.
        /// </summary>
        public static Build CurrentChampionBuild = new Build();

        /// <summary>
        ///     Initializes the AutoShop system
        /// </summary>
        public static void Init()
        {
            try
            {
                // Create the build path directory
                Directory.CreateDirectory(BuildPath);

                // Check if the index file exists
                if (!File.Exists(TempPath + "buildindex.dat"))
                {
                    // If not, create the index file
                    Buy.CreateIndexFile();
                }

                // Loop through all the builds in the build path directory
                foreach (var build in Directory.GetFiles(BuildPath))
                {
                    // Get the name of the champion from the build
                    var parsed = build.Replace(".json", "").Replace(BuildPath + "\\", "");

                    // Add the build to the Builds dictionary in a ChampionName : BuildData format
                    Builds.Add(parsed, File.ReadAllText(build));
                }

                // Check if there are any builds for our champion
                if (!Builds.Keys.Any(b => b == Player.Instance.ChampionName))
                {
                    // If not, warn the user
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss - ") +
                                      "AutoAram Warning] There are no builds for your champion.");
                    Console.WriteLine("AutoAram Warning] No build is currently used!");
                    Console.ResetColor();

                    // and terminate the AutoShop
                    return;
                }
                // Check if the parse of the build for the champion completed successfully and output it to public
                // variable CurrentChampionBuild
                if (
                    Builds.FirstOrDefault(b => b.Key == Player.Instance.ChampionName)
                        .Value.TryParseData(out CurrentChampionBuild))
                {
                    // If the parse is successful, notify the user that the initialization process is finished
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss - ") +
                                      "AutoAram Info] AutoShop has been fully and succesfully initialized!");
                    Console.ResetColor();

                    // and set up event listeners
                    SetUpEventListeners();
                }
                else
                {
                    // An error occured during parsing. Catch the error and print it in the console
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss - ") +
                                      "AutoAram Error] The selected AutoShop JSON could not be parsed.");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss - ") +
                                      "AutoAram Warning] No build is currently used!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                // An exception occured somewhere else. Notify the user of the error, and print the exception to the console
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(DateTime.Now.ToString(Environment.NewLine + "[hh:mm:ss - ") +
                                  "AutoAram Error] Exception occurred on initialization of AutoShop:");
                Console.ResetColor();
                Console.Write(ex);

                // Warn the user about the exception
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(DateTime.Now.ToString("hh:mm:ss - ") +
                                  "AutoAram Warning] Exception occurred during AutoShop initialization. AutoShop will most likely NOT work properly!");
                Console.ResetColor();
            }
        }

        /// <summary>
        ///     Method that sets up event listeners
        /// </summary>
        private static void SetUpEventListeners()
        {
            // When we can buy items
            Events.OnBuyAllow += Events_OnBuyAllow;

            // When the user forced a build reset
            Events.OnBuildReset += Events_OnBuildReset;

            // When the game ends
            Game.OnEnd += Game_OnEnd;

            // When the game starts
            Game.OnLoad += Game_OnLoad;
        }
        /// <summary>
        /// Fired when the game starts
        /// </summary>
        /// <param name="args">Arguments providing with information about the GameOnLoad</param>
        private static void Game_OnLoad(EventArgs args)
        {
            // Delete the index file if it exists
            if (File.Exists(TempPath + "\\buildindex.dat"))
                File.Delete(TempPath + "\\buildindex.dat");
        }

        /// <summary>
        ///     Fired when the game ends
        /// </summary>
        /// <param name="args">Arguments providing with information about the GameEnd</param>
        private static void Game_OnEnd(GameEndEventArgs args)
        {
            // Delete the index file if it exists
            if (File.Exists(TempPath + "\\buildindex.dat"))
                File.Delete(TempPath + "\\buildindex.dat");
        }

        /// <summary>
        ///     Fired when a build reset is forced
        /// </summary>
        /// <param name="args">Arguments of the event</param>
        private static void Events_OnBuildReset(EventArgs args)
        {
            // Notify the user that the build has been reset
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss - ") + "AutoAram Info] Build has been reset!");
            Console.ResetColor();

            // Reset the build index, restarting the build process from the start
            Buy.ResetIndex();
        }

        /// <summary>
        ///     Fired when buying is allowed
        /// </summary>
        /// <param name="args">Arguments of the event</param>
        private static void Events_OnBuyAllow(EventArgs args)
        {
            // Notify the user that we are going to try to buy items now
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss - ") + "AutoAram Info] Can buy items");
            Console.ResetColor();

            // Attempt to buy as many consecutive items on the build as we can
            Buy.BuyNextItem(CurrentChampionBuild);
        }
    }
}