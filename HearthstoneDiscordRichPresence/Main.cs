using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DiscordRPC;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using HearthDb.Enums;
using HearthMirror.Enums; // Required for updated Mode structure mapping

namespace HearthstoneDiscordRichPresence
{
    public class Main
    {
        private const string ApplicationID = "565295209936715776";
        private static DiscordRpcClient discord;
        public static bool isRunning = false;

        internal static void Load()
        {
            discord = new DiscordRpcClient(ApplicationID);
            discord.Initialize();
        }

        internal static void Unload()
        {
            ClearPresence();
            discord.Dispose();
        }

        // Updated parameter target to leverage the modernized HearthMirror Mode enum
        internal static void HandleUpdate(Mode mode)
        {
            HandleUpdate();
        }

        internal static void HandleUpdate(ActivePlayer player)
        {
            HandleUpdate();
        }

        internal static void HandleUpdate()
        {
            // Checks HDT's current UI engine mode state
            switch (Hearthstone_Deck_Tracker.Core.Game.CurrentMode)
            {
                case Mode.HUB:
                    UpdatePresence("In Main Menu");
                    break;
                case Mode.GAMEPLAY:
                    UpdatePresenceGameplay();
                    break;
                case Mode.COLLECTIONMANAGER:
                    UpdatePresence("Browsing Collection");
                    break;
                case Mode.TOURNAMENT:
                    UpdatePresence("Preparing for a battle");
                    break;
                case Mode.ADVENTURE:
                    UpdatePresence("Preparing for an Adventure");
                    break;
                case Mode.TAVERN_BRAWL:
                    UpdatePresence("Preparing for a Tavern Brawl");
                    break;
                case Mode.PACKOPENING:
                    UpdatePresence("Opening Packs!");
                    break;
                case Mode.DRAFT:
                    UpdatePresence("Preparing for an Arena");
                    break;
                case Mode.FATAL_ERROR:
                    ClearPresence();
                    break;
            }
        }

        private static void UpdatePresenceGameplay()
        {
            string detail = "";
            string state = "";

            switch (Hearthstone_Deck_Tracker.Core.Game.CurrentGameMode)
            {
                case GameMode.Ranked:
                case GameMode.Casual:
                    detail = GetDetail(Hearthstone_Deck_Tracker.Core.Game.CurrentFormat, Hearthstone_Deck_Tracker.Core.Game.CurrentGameMode);
                    state = Hearthstone_Deck_Tracker.Core.Game.Player.Class + " vs. " + Hearthstone_Deck_Tracker.Core.Game.Opponent.Class;
                    if (Hearthstone_Deck_Tracker.Core.Game.GetTurnNumber() > 0)
                    {
                        state += " - turn " + Hearthstone_Deck_Tracker.Core.Game.GetTurnNumber();
                    }
                    break;

                case GameMode.Battlegrounds:
                    detail = "Playing Battlegrounds";
                    // Dynamically grabs your Battlegrounds MMR rating from the HDT memory cache
                    var rating = Hearthstone_Deck_Tracker.Core.Game.BattlegroundsRating;
                    state = rating > 0 ? $"Rating: {rating}" : "In Combat Phase";
                    break;

                case GameMode.Spectator:
                    detail = "Spectating a game";
                    break;
                    
                case GameMode.Brawl:
                    detail = "Playing in Tavern Brawl";
                    break;
                    
                case GameMode.Practice:
                    detail = "Playing against AI";
                    break;
                    
                case GameMode.Arena:
                    detail = "Playing in Arena";
                    break;
                    
                case GameMode.Friendly:
                    detail = "Playing with Friend";
                    break;

                default:
                    detail = "In Match";
                    break;
            }

            UpdatePresence(detail, state);
        }

        private static string GetDetail(Format? currentFormat, GameMode currentGameMode)
        {
            string detail = "Playing in " + currentGameMode + " " + currentFormat;
            
            if (currentGameMode == GameMode.Ranked && Hearthstone_Deck_Tracker.Core.Game.MatchInfo != null)
            {
                var localPlayer = Hearthstone_Deck_Tracker.Core.Game.MatchInfo.LocalPlayer;
                if (localPlayer != null)
                {
                    if (currentFormat == Format.Standard)
                    {
                        detail += localPlayer.StandardLegendRank > 0 
                            ? " - Legend Rank " + localPlayer.StandardLegendRank 
                            : " - Rank " + localPlayer.StandardRank;
                    }
                    else if (currentFormat == Format.Wild)
                    {
                        detail += localPlayer.WildLegendRank > 0 
                            ? " - Legend Rank " + localPlayer.WildLegendRank 
                            : " - Rank " + localPlayer.WildRank;
                    }
                }
            }

            return detail;
        }

        private static void UpdatePresence(string detail)
        {
            UpdatePresence(detail, null);
        }

        private static void UpdatePresence(string detail, string state)
        {
            Assets assets = new Assets()
            {
                LargeImageKey = "hs-logo"
            };
            discord?.SetPresence(new RichPresence()
            {
                Details = detail,
                State = state,
                Assets = assets
            });
        }

        private static void ClearPresence()
        {
            discord?.ClearPresence();
        }
    }

    public class MainPlugin : IPlugin
    {
        public string Name => "Discord Rich Presence";
        public string Description => "Plugin to show detailed stats of your Hearthstone game on Discord.";
        public string ButtonText => null;
        public string Author => "MISI90";
        public Version Version => new Version(1, 0, 0); // Bumped version for framework upgrade
        public MenuItem MenuItem => null;

        public void OnButtonPress()
        {
        }

        public void OnLoad()
        {
            Main.Load();
            GameEvents.OnGameStart.Add(Main.HandleUpdate);
            GameEvents.OnTurnStart.Add(Main.HandleUpdate);
            GameEvents.OnModeChanged.Add(Main.HandleUpdate);
        }

        public void OnUnload()
        {
            Main.Unload();
        }

        public void OnUpdate()
        {
            // Checks the global application engine execution loops rather than internal legacy fields
            bool gameRunning = Hearthstone_Deck_Tracker.Hearthstone.Watchers.ExperienceWatcher.IsHearthstoneRunning();

            if (Main.isRunning && !gameRunning)
            {
                Main.isRunning = false;
                Main.Unload();
            }
            if (!Main.isRunning && gameRunning)
            {
                Main.isRunning = true;
                Main.Load();
            }
        }
    }
}
