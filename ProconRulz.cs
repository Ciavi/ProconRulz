
//******************************************************************************************************
// ProconRulz Procon plugin, by bambam
//******************************************************************************************************
/*  Copyright 2013 Ian Forster-Lewis

    This file is part of my ProconRulz plugin for ProCon.

    ProconRulz plugin for ProCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    ProconRulz plugin for ProCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with ProconRulz plugin for ProCon.  If not, see <http://www.gnu.org/licenses/>.
 */
#region release notes
// v44 - a. support for BF4
//       b. support for rulz files (Plugins\BF4\proconrulz_*.txt)
//       c. 'Not' modifier now also allowed with If and Text conditions
//       d. added VictimTeamKey
//       e. Enable/Disable rulz.txt files in settings
//       f. Linux support for external rulz files (credit FritzE)
//       g. On Init trigger added, for var initialisations
//       h. On RoundOver trigger added. Bugfix for NULL team
//       j. Update to reload .txt rulz files on plugin enable, and display full path if not found
// v43 - a. added 'rounding' to vars, e.g. %x.3% is 3 decimal places
//       b. Allow "Set %ini% 0" to reset ini file, Set "%ini_<section>% 0" to delete section,
//       c. bugfix for TargetPlayer condition when %targettext% is null. Added '+' for rule continuation
//       d. added %score% var for each player (aka %server_score[<playername>]%)
// v42 - a. Support %ini_XXX% variables stored in settings
//       b. New 'logging' options in plugin settings
//       c. arithmetic in Set clauses e.g. Set %x% %x%+1
//       d. another effort at the procon URLencode settings work-around
//       e. allow spaces in arithmetic e.g. "If %x% + %y% > 10;"
//       f. new subst vars %ts1%, %ts2%, %pts% for teamsize 1, 2, playerteamsize
//       g. new subst var %hms% for time, %seconds% for time in seconds, %ymd% for yyyy-mm-dd
// v41 - a. Allow parsing of chat text e.g. to pick out a ban reason
// v40 - a. Quoted strings in Exec e.g. Exec vars.serverName "Example server name - Admins Online"
//       b. Quoted strings in Set e.g. Set %a% "hello world", $..$ for subst vars
//       c. $ replaced with % in rulz_vars to avoid Procon settings encode bug
//       d. fix for admin kill triggering On Suicide
// v39 - a. TeamSay, SquadSay, TeamYell, SquadYell, 
//          %pcountry%, %pcountrykey%, %vcountry%, %vcountrykey% (country codes for player and victim)
//       b. Allow 'Exec' actions to call Procon commands
//          Allow 'int' value for yell delay (seconds) in Yell actions
//       c. Ban and TempBan ban by 'name' if no GUID available
//       d. %team_score% aka %server_team_score[<teamid>]%
// v38 - a. actions and conditions can be mixed in any order, 
//       b. %streak% is shorthand for %server_streak[%p%]%, 
//          %team_streak% is %server_team_streak[%ptk%]%
//          %squad_streak% is %server_squad_streak[%ptk%][%psk%]%
//          rulz vars can be nested using brackets, e.g. %kills[%weapon%]%
//       c. modify Exec action to support punkBuster commands
//       d. TargetActions are now immediate, TargetPlayer only succeeds IFF 1 player found
//       e. Yell delay (default 5 seconds) added to plugin settings
// v37 - string vars (as well as ints from v35)
//       b. var names can have embedded subst vars, e.g. "server_%v%_deaths" is a var name with a player name embedded
//       c. moved the subst vars processing into assign_keywords
//       d. moved most of the Details help text onto the web
//       e. bugfix for %c% - set value for actions, temp value for conditions
//       f. bugfix for PBBan message
//       g. If "%text% word abc" condition, 
//          allow %vars% in Set %newvar% %p%,%newvar% conditions
//       h. added punkBuster.pb_sv_command pb_sv_updbanfile to speed up PB Bans/Kicks  
// v36 - Punkbuster bans
// v35 - a. Multiple keys in conditions e.g. "Weapon RPG-7,SMAW"
//       b. xyzzy debug
//      c. continue
//      d. rulz vars Set Incr Decr If
//      e. actions thread inline, no decode on rulz, %ps% PlayerSquad
//      f. propagate trigger from prior rulz, default continue unless Kill/Kick/TempBan/Ban/End
// v34 - Protected now Admin, Admin_and_Reserved_Slots, Neither
// v33 - BF3 compatibility
//       TargetPlayer now auto-TargetConfirm if only one match
// v32 - TargetPlayer,TargetAction,TargetConfirm,TargetCancel, %t% substitution for target
//       PlayerCount,TeamCount,ServerCount, %tc% and %sc% substitution for team and server counts
//       TempBan action
//       Ping condition, %ping% substitution
//       PlayerYell,PlayerBoth, AdminSay, %text% substition
//       'Rates' now span round ends
//       bugfix for multi-word weapon keys, e.g. "M1A1 Thompson", heli weapons
//       bugfix for sp_shotgun_s
//       On Join, On Leave triggers
//       Whitelist for clans and players
//       PlayerFirst, TeamFirst, ServerFirst, PlayerOnce conditions
//       Player_loses_item_when_dead bugfix
// v31 - Map, MapMode conditions, On Round trigger, VictimSay action 
//  (31b bugfix for On Spawn;Damage...) 
//  (31c bugfix for teamsize)
// v30 - settings options - rules message as PlayerSay, disable rules message
// v29 - TeamKit, TeamDamage, TeamSpec, TeamWeapon counts conditions
// v28 - updated to use latest Procon 1 API
// v27 - "Protected" condition
// v26 - "On Say; Text xxx;" trigger, Headshot %h% substitution
// v25 - "Range N;" condition
// v24 - new conditions Admin (player an admin), Admins (admins on server)
// v23 - fixed Count bug
// v22 - added rule comments ('#' as first char)
// v20 - changed rule to have <list> of Conditions
// v19 - added "Not [Kit|Spec|Damage|Weapon] X" condition

#endregion

#region includes

using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PRoCon; // added for FileHostNamePort()
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Accounts;
using PRoCon.Core.Remote; // for FileHostNamePort()
using System.IO;

#endregion

namespace PRoConEvents
{
    public class ProconRulz : PRoConPluginAPI, IPRoConPluginInterface
    {
        public string version = "45a.0";

        #region Types

        // the 'Rates' condition keeps track of this many 'rule trigger times' for each player
        const int RATE_HISTORY = 60;

        //**********************************************************************************************
        //**********************************************************************************************
        //   DEFINE SOME TYPES
        //**********************************************************************************************
        //**********************************************************************************************

        enum GameIdEnum { BFBC2, MoH, BF3, BF4 }

        enum ReserveItemEnum { Player_loses_item_when_dead, Player_reserves_item_until_respawn }

        enum LogFileEnum { PluginConsole, Console, Chat, Events, Discard_Log_Messages }

        // these are the executable actions
        // note that 'TargetAction <action>' does not actually have 
        // 'TargetAction' as a defined action, instead it
        // is treated as a boolean flag on the PartClass to indicate the action is delayed
        enum PartEnum 
        { Yell, Say, PlayerYell, PlayerSay, VictimSay, TeamYell, TeamSay, SquadYell, SquadSay,
            Log, Both, PlayerBoth, All, AdminSay, // various message actions
            // actions affecting 'target' e.g. playername in say text:
            TargetConfirm, TargetCancel, 
            Kill, Kick, PlayerBlock, Ban, TempBan, PBBan, PBKick,
            Execute, Continue, End,
            Headshot, Protected, Admin, Admins, Team, Teamsize,
            Map, MapMode,
            Kit, Weapon, Spec, Damage, TeamKit, TeamWeapon, TeamSpec, TeamDamage,
            Range,
            Count, PlayerCount, TeamCount, ServerCount,
            Rate, Text, TargetPlayer, Ping,
            Set, Incr, Decr, Test,
            MapList
        };


        // flag for rule to fire either on player spawn, or a kill
        // Void mean absence of trigger
        enum TriggerEnum { Void, Round, RoundOver, Join, Kill, Spawn, TeamKill, Suicide, PlayerBlock, Say, Leave, Init } 
        
        enum ItemTypeEnum { None, Kit, Weapon, Spec, Damage } 
        
        // substitution values in messages
        enum SubstEnum {Player, Victim, 
                        Weapon, WeaponKey, Damage, DamageKey, Spec, SpecKey,
                        Kit, VictimKit, KitKey,
                        Count, TeamCount, ServerCount,
                        PlayerTeam, PlayerSquad,
                        PlayerTeamKey, PlayerSquadKey,
                        PlayerCountry, PlayerCountryKey, // from pb info
                        VictimCountry, VictimCountryKey,
                        Teamsize, Teamsize1, Teamsize2, PlayerTeamsize, 
                        VictimTeam, VictimTeamKey,Range, BlockedItem, Headshot, 
                        Map, MapMode, Target, Text, TargetText, Ping, EA_GUID, PB_GUID, IP,
                        Hhmmss, Seconds, Date
                        };

        static Dictionary<SubstEnum, List<string>> subst_keys = new Dictionary<SubstEnum, List<string>>();

        // which players should be 'protected from kicks, kills and bans by ProconRulz
        enum ProtectEnum { Admins, Admins_and_Reserved_Slots, Neither };

        // a PART is a statement within a rule, i.e. a condition or action
        class PartClass
        {
            // this action should be delayed and applied to another target not current player
            public bool target_action; 
            public PartEnum part_type; // e.g. Kick
            public bool negated; // conditions can be 'Not'
            public List<string> string_list;
            public int int1;
            public int int2;
            public bool has_count; // true if this part refers to a rule count (%c% or PlayerCount etc)

              // e.g. "You were kicked for teamkills". Can also be an integer string for Kill action
            public PartClass()
            {
                target_action = false;
                string_list = new List<string>();
                int1 = 0;
                int2 = 0;
                negated = false;
                has_count = false;
            }

            public string ToString()
            {
                string part_string;
                part_string = (negated ? " Not" : "");
                switch (part_type)
                {
                    case PartEnum.Headshot:
                        part_string += " Headshot;";
                        break;

                    case PartEnum.Protected:
                        part_string += " Player protected from kick/kill;";
                        break;

                    case PartEnum.Admin:
                        part_string += " Admin player;";
                        break;

                    case PartEnum.Admins:
                        part_string += " Admins are online;";
                        break;

                    case PartEnum.Ping:
                        part_string += String.Format(" Ping >= {0};", int1);
                        break;

                    case PartEnum.Team:
                        part_string += String.Format(" Team \"{0}\";", keys_join(string_list));
                        break;

                    case PartEnum.Teamsize:
                        part_string += String.Format(" Teamsize <= {0};", int1);
                        break;

                    case PartEnum.Map:
                        part_string += String.Format(" Map name includes \"{0}\";", keys_join(string_list));
                        break;

                    case PartEnum.MapMode:
                        part_string += String.Format(" Map mode is \"{0}\";", keys_join(string_list));
                        break;

                    case PartEnum.Kit:
                        part_string += " Kit \"" +
                            keys_join(string_list) + (int1 == 0 ? "\";" : String.Format("\" max({0});", int1));
                        break;

                    case PartEnum.Weapon:
                        part_string += " Weapon is \"" +
                            keys_join(string_list) + (int1 == 0 ? "\";" : String.Format("\" max({0});", int1));
                        break;

                    case PartEnum.Spec:
                        part_string += " Spec \"" +
                            keys_join(string_list) + (int1 == 0 ? "\";" : String.Format("\" max({0});", int1));
                        break;

                    case PartEnum.Damage:
                        part_string += " Damage \"" +
                            keys_join(string_list) + (int1 == 0 ? "\";" : String.Format("\" max({0});", int1));
                        break;

                    case PartEnum.TeamKit:
                        part_string += " TeamKit \"" +
                            keys_join(string_list) + String.Format("\" max({0});", int1);
                        break;

                    case PartEnum.TeamWeapon:
                        part_string += " TeamWeapon \"" +
                            keys_join(string_list) + String.Format("\" max({0});", int1);
                        break;

                    case PartEnum.TeamSpec:
                        part_string += " TeamSpec \"" +
                            keys_join(string_list) + String.Format("\" max({0});", int1);
                        break;

                    case PartEnum.TeamDamage:
                        part_string += " TeamDamage \"" +
                            keys_join(string_list) + String.Format("\" max({0});", int1);
                        break;

                    case PartEnum.Range:
                        part_string += " Range " + String.Format("over {0};", int1);
                        break;

                    case PartEnum.Count:
                    case PartEnum.PlayerCount:
                        part_string += " Player Rule Count is more than " + String.Format("{0};", int1);
                        break;

                    case PartEnum.TeamCount:
                        part_string += " Team Rule Count is more than " + String.Format("{0};", int1);
                        break;

                    case PartEnum.ServerCount:
                        part_string += " Server Rule Count is more than " + String.Format("{0};", int1);
                        break;

                    case PartEnum.Rate:
                        part_string += " Rate " + String.Format("{0} in {1} seconds;", int1, int2);
                        break;

                    case PartEnum.Text:
                        part_string += " Text " + String.Format("key \"{0}\";", keys_join(string_list));
                        break;

                    case PartEnum.TargetPlayer:
                        if (string_list != null)
                            part_string += " Target player contains " +
                                String.Format("\"{0}\";", keys_join(string_list));
                        else
                            part_string += " Target player %t% found;";
                        break;

                    case PartEnum.Incr:
                        part_string += String.Format(" Incr {0};", string_list[0]);
                        break;

                    case PartEnum.Decr:
                        part_string += String.Format(" Decr {0};", string_list[0]);
                        break;

                    case PartEnum.Set:
                        part_string += String.Format(" Set {0};", keys_join(string_list));
                        break;

                    case PartEnum.Test:
                        part_string += String.Format(" If [{0}];", keys_join(string_list));
                        break;

                    case PartEnum.MapList:
                        part_string += String.Format(" MapList {0};", keys_join(string_list));
                        break;

                    default:
                        part_string += (String.Format(" {0}{1} [int: {2}] [string: {3}];",
                                        target_action ? "TargetAction " : "",
                                        Enum.GetName(typeof(PartEnum), part_type),
                                        int1 == null || int1 == 0 ? "" : int1.ToString(),
                                        string_list[0]));
                        break;

                }
                return part_string;
            }

        } // end class PartClass

        // deferred actions from 'TargetAction' actions in a triggered rule
        class TargetActions
        {
            public string target;
            public List<PartClass> actions;

            public TargetActions(string t)
            {
                actions = new List<PartClass>();
                target = t;
            }
        }

        // here's how the rules are stored in ProconRulz (trigger, list of parts)
        class ParsedRule
        {
            public ParsedRule()
            {
                parts = new List<PartClass>();
                comment = false;
                trigger = TriggerEnum.Spawn;
            }
            public int id; // rule identifier 1..n
            public List<PartClass> parts; // e.g. [Not Kit Recon 2]
            public TriggerEnum trigger; // trigger rule e.g. on spawn or kill
            public string unparsed_rule; // the original string parsed into this rule
            public bool comment; // this 'rule' is a comment to ignore at runtime
        }

        #endregion

        #region PlayerList class

        class PlayerData
        {
            public bool updated; // set to true during admin.listPlayers processing
            public string name;
            public string squad;
            public string team;
            public string ip;
            public string ea_guid;
            public string pb_guid;
            public string clan;
            public int ping;
            public int score;
            public string country_key;
            public string country_name;

            public PlayerData() 
            { 
                updated = false;
                squad = "-1";
                team = "-1";
                //ip = "no IP";
                //ea_guid = "No_EA_GUID";
                //pb_guid = "No_PB_GUID";
                clan = "No clan";
                ping = -1;
                score = 0;
                country_key = ""; // country code
                country_name = ""; // country
            }
        }

        // this plugin maintains its own list of playernames on the server
        class PlayerList
        {
            // player info is stored as a dictionary playername->CPlayerInfo
            Dictionary<string, PlayerData> info;

            // when a player first joins, we cache their name/team in here (don't have a CPlayerInfo yet)
            Dictionary<string, string> new_player_teams;

            public PlayerList()
            {
                // info is the main player list
                info = new Dictionary<string, PlayerData>();
                // new_player_teams is the cache of players from OnPlayerJoin before 
                // they get CPlayerInfo from OnListPlayers
                new_player_teams = new Dictionary<string, string>();
            }

            public void reset()
            {
                info.Clear();
                new_player_teams.Clear();
            }

            // remove player entries that don't have 'updated' true
            public void scrub()
            {
                new_player_teams.Clear();

                List<string> scrub_keys = new List<string>();
                foreach (string player_name in info.Keys)
                    if (info[player_name].updated == false) scrub_keys.Add(player_name);
                foreach (string player_name in scrub_keys) info.Remove(player_name);
            }

            public void pre_scrub()
            {
                foreach (string player_name in info.Keys) info[player_name].updated = false;
            }

            public void remove(string player_name)
            {
                // remove from main list
                info.Remove(player_name);
                //and remove from new player cache if necessary
                new_player_teams.Remove(player_name);
            }

            // called by OnPlayerJoin
            // a new player has a name and maybe a team, that's all. No CPlayerInfo
            public void new_player(string player_name)
            {
                if (info.ContainsKey(player_name)) return; // already in main player list
                if (new_player_teams.ContainsKey(player_name)) return; // already in new player cache
                // create entry for the player, but put them in team -1 (i.e. they're not in a team yet)
                new_player_teams.Add(player_name, "-1");
            }

            // here's where we add a player to the main list
            // and remove them from the new player cache if necessary
            public void update(CPlayerInfo inf)
            {
                string player_name = inf.SoldierName;
                // remove from new player cache
                new_player_teams.Remove(player_name);
                // add to main player list (update existing entry if necessary)

                if (!info.ContainsKey(player_name) || info[player_name] == null)
                    info[player_name] = new PlayerData();
                info[player_name].name = player_name;
                info[player_name].squad = inf.SquadID.ToString();
                info[player_name].team = inf.TeamID.ToString();
                info[player_name].ea_guid = inf.GUID;
                info[player_name].clan = inf.ClanTag;
                info[player_name].score = inf.Score;
                info[player_name].updated = true;
            }

            // update based on Punkbuster info
            public void update(CPunkbusterInfo inf)
            {
                string player_name = inf.SoldierName;
                if (!info.ContainsKey(player_name) || info[player_name] == null)
                    info[player_name] = new PlayerData();
                info[player_name].name = player_name;
                info[player_name].pb_guid = inf.GUID;
                info[player_name].ip = inf.Ip;
                info[player_name].country_key = inf.PlayerCountryCode;
                info[player_name].country_name = inf.PlayerCountry;
            }

            public void team_move(string player_name, string team_id, string squad_id)
            {
                // attempt 1: update player entry in main list
                if (info.ContainsKey(player_name))
                {
                    info[player_name].team = team_id;
                    info[player_name].squad = squad_id;
                }
                // attempt 2: maybe they've just joined - update player entry in new player list
                if (new_player_teams.ContainsKey(player_name))
                    new_player_teams[player_name] = team_id;
                return;
            }

            // return the current team_id of player
            public string team_id(string player_name)
            {
                if (player_name == null) return "-1";
                // attempt #1: try main player list
                if (info.ContainsKey(player_name)) return info[player_name].team;
                // attempt #2: try cache of new players
                if (new_player_teams.ContainsKey(player_name)) return new_player_teams[player_name];
                else return "-1";
            }

            // return the current squad_id of player
            public string squad_id(string player_name)
            {
                if (player_name == null) return "-1";
                // attempt #1: try main player list
                if (info.ContainsKey(player_name)) return info[player_name].squad;
                return "-1";
            }

            // return the number of players in team
            public int teamsize(string team_id)
            {
                return list_players(team_id).Count;
            }

            // return current ping for this player, as updated in the latest admin.listPlayers
            public int ping(string player_name)
            {
                if (player_name == null ||
                    player_name == "" ||
                    !info.ContainsKey(player_name) ||
                    info[player_name].ping == null) return -1;

                return info[player_name].ping;
            }

            // return current score for this player, as updated in the latest admin.listPlayers
            public int score(string player_name)
            {
                if (player_name == null ||
                    player_name == "" ||
                    !info.ContainsKey(player_name) ||
                    info[player_name].score == null) return -1;

                return info[player_name].score;
            }

            // return EA GUID for this player, as updated in the latest admin.listPlayers
            public string ea_guid(string player_name)
            {
                if (player_name == null ||
                    player_name == "" ||
                    !info.ContainsKey(player_name) ||
                    info[player_name].ea_guid == null) return "";

                return info[player_name].ea_guid;
            }

            // return Punkbuster GUID for this player, as updated in the latest OnPunkbusterInfo
            public string pb_guid(string player_name)
            {
                if (player_name == null ||
                    player_name == "" ||
                    !info.ContainsKey(player_name) ||
                    info[player_name].pb_guid == null) return "";

                return info[player_name].pb_guid;
            }

            // return IP address for this player, as updated in the latest OnPunkbusterInfo
            public string ip(string player_name)
            {
                if (player_name == null ||
                    player_name == "" ||
                    !info.ContainsKey(player_name) ||
                    info[player_name].ip == null) return "";

                return info[player_name].ip;
            }

            // return country name for this player, as updated in the latest OnPunkbusterInfo
            public string cname(string player_name)
            {
                if (player_name == null ||
                    player_name == "" ||
                    !info.ContainsKey(player_name) ||
                    info[player_name].country_name == null) return "";

                return info[player_name].country_name;
            }

            // return country KEY for this player, as updated in the latest OnPunkbusterInfo
            public string ckey(string player_name)
            {
                if (player_name == null ||
                    player_name == "" ||
                    !info.ContainsKey(player_name) ||
                    info[player_name].country_key == null) return "";

                return info[player_name].country_key;
            }

            public List<string> list_new_players()
            {
                return new List<string>(new_player_teams.Keys);
            }

            public List<string> list_players()
            {
                return new List<string>(info.Keys);
            }

            public List<string> list_players(string team_id)
            {
                List<string> player_list = new List<string>();
                foreach (string player_name in info.Keys)
                    if (info[player_name].team == team_id) player_list.Add(player_name);
                // also check new players
                foreach (string player_name in new_player_teams.Keys)
                    if (new_player_teams[player_name] == team_id) player_list.Add(player_name);
                return player_list;
            }

            public int min_teamsize()
            {
                int size1 = teamsize("1");
                int size2 = teamsize("2");
                return size1 < size2 ? size1 : size2;
            }

            // return a list os all the team_ids found in the player list
            public List<string> list_team_ids()
            {
                List<string> team_ids = new List<string>();
                foreach (string player_name in info.Keys)
                    try {
                        if (!team_ids.Contains(info[player_name].team)) 
                            team_ids.Add(info[player_name].team);
                    } catch {}
                foreach (string player_name in new_player_teams.Keys)
                    try {
                        if (!team_ids.Contains(new_player_teams[player_name])) 
                            team_ids.Add(new_player_teams[player_name]);
                    } catch { }

                team_ids.Sort(); // sort ascending
                return team_ids;
            }

            public string clan(string player_name)
            {
                if (player_name == null) return "No clan";
                if (player_name == "") return "No clan";

                try
                {
                    if (info.ContainsKey(player_name))
                    {
                        string clan_name = info[player_name].clan;
                        if (clan_name == null) return "No clan";
                        if (clan_name.Trim() == "") return "No clan";
                        return clan_name;
                    }

                }
                catch { }

                return "No clan";
            }
        }

        #endregion

        #region SpawnCounts class
        // counts of spawned items in team_1 and team_2 - lists player names with each item
        // each entry is <team_id> -><item name>::[<player_name>,...]
        // e.g. Recon -> [sleepy, grumpy, doc]
        class SpawnCounts
        {
            Dictionary<string, Dictionary<string, List<string>>> counts;

            List<string> watched_items;

            public SpawnCounts()
            {
                //                      team_id    ->  (item_name) ->  (List of player names)
                counts = new Dictionary<string, Dictionary<string, List<string>>>();
                watched_items = new List<string>();
            }

            public List<string> list_items()
            {
                return watched_items;
            }

            // return a list of team_ids that have spawn counts recorded against them
            //public List<int> list_team_ids()
            //{
            //    return new List<int>(counts.Keys);
            //}

            // list the players spawned with an item in a given team
            public List<string> list_players(string item_name, string team_id)
            {
                if (!counts.ContainsKey(team_id)) return new List<string>(); // team has no watch items
                // team doesn't have this item watched:
                if (!counts[team_id].ContainsKey(item_name)) return new List<string>();
                return counts[team_id][item_name]; // return list of player names
            }

            // this is a bit subtle - the way we 'watch' items (kits, weapons, specs, damagetypes) is by
            // creating a dictionary key entry for the string name of that item (e.g. "recon").
            // The 'value' of that dictionary entry is the list of string playernames that have spawned
            // with that item.
            // This enables us to count the number of players with this item 
            // (i.e. the length of the player
            // name list in that dictionary entry.).
            public void watch(List<string> items_mixed)
            {
                foreach (string item_mixed in items_mixed)
                    if (!watched_items.Contains(item_mixed.ToLower())) watched_items.Add(item_mixed.ToLower());
            }

            // scrub player "name" from both spawn_counts (on spawn, so we can add a fresh entry)
            public void zero_player(string player_name)
            {
                if (player_name == null) return;
                if (player_name == "") return;
                foreach (string team_id in counts.Keys)
                {
                    foreach (string item in counts[team_id].Keys)
                    {
                        counts[team_id][item].Remove(player_name);
                    }
                }
            }

            // keep watch list but zero all counts (called at round end)
            public void zero()
            {
                counts.Clear();
            }

            // reset to startup status
            public void reset()
            {
                counts.Clear();
                watched_items.Clear();
            }

            // player "name" has just spawned with item 'item', so add to counts for that team
            public void add(string item_mixed, string team_id, string player_name)
            {
                if (player_name == null) return;
                if (player_name == "") return;

                string item_lcase = item_mixed.ToLower();

                if (!watched_items.Contains(item_lcase)) return; // ignore if item is not 'watched'

                if (!counts.ContainsKey(team_id)) // if no entry for team then create one
                    counts[team_id] = new Dictionary<string, List<string>>();

                if (!counts[team_id].ContainsKey(item_lcase)) // if no entry for item then create one
                    counts[team_id][item_lcase] = new List<string>();

                // add team/item/player_name if it's not already there
                if (!counts[team_id][item_lcase].Contains(player_name))
                    counts[team_id][item_lcase].Add(player_name);
            }

            // how many players on team currently have any 'item' on list
            public int count(List<string> items_mixed, string team_id)
            {
                if (!counts.ContainsKey(team_id)) return 0; // if team has no watched items then 0
                List<string> items_lcase = new List<string>();
                if (items_mixed != null)
                    foreach (string i in items_mixed) items_lcase.Add(i.ToLower());
                List<string> player_list = new List<string>(); // list of players with these items
                foreach (string item_lcase in items_lcase)
                {
                    if (!watched_items.Contains(item_lcase)) continue; // if not watched then 0
                    // if team does not have this item then 0
                    if (!counts[team_id].ContainsKey(item_lcase)) continue;
                    // return length of player name list for this team/item
                    player_list.AddRange(counts[team_id][item_lcase]);
                }
                return player_list.Count;
            }

            // return 'true' if player has any item on the list
            public bool has_item(List<string> items_mixed, string player_name)
            {
                if (player_name == null) return false;
                if (player_name == "") return false;
                List<string> items_lcase = new List<string>();
                if (items_mixed != null)
                    foreach (string i in items_mixed) items_lcase.Add(i.ToLower());
                foreach (string item_lcase in items_lcase)
                {
                    if (!watched_items.Contains(item_lcase)) continue;// if not watched then 0
                    foreach (string team_id in counts.Keys)
                        if (counts[team_id].ContainsKey(item_lcase) &&
                                counts[team_id][item_lcase].Contains(player_name)) return true;
                }
                return false;
            }
        }

        #endregion

        #region VarsClass class - this manages the rulz variables (Set, Incr, Decr, If)
        // stores values of rulz variables
        class VarsClass
        {
            // basic proconrulz vars
            Dictionary<string, string> vars;
            // persistent vars - stored in Configs/proconrulz.ini
            Dictionary<string, Dictionary<string, string>> ini_vars;

            string ini_filename;

            public VarsClass(string file_hostname_port)
            {
                ini_filename = "Configs" + Path.DirectorySeparatorChar + file_hostname_port + "_proconrulz.ini";
                // as of ver 37 all vars stored as strings
                vars = new Dictionary<string, string>();
                ini_vars = new Dictionary<string, Dictionary<string, string>>();
            }

            // keep list but zero all values (called at round end)
            public void zero()
            {
                vars.Clear();
            }

            // reset to startup status
            public void reset()
            {
                vars.Clear();
                ini_vars.Clear();
                ini_load(ini_filename);
            }

            // MANGLE is a fairly important function in ProconRulz vars processing...
            // all vars are converted to 'server' variables but the usage in rulz allows
            // them to appear as 'per-player' variables.
            // i.e. %streak% in a rule is a UNIQUE variable for each player
            // the rulz processing converts this to %server_streak[<playername>]% as the unique name
            // so in effect, %streak% is shorthand for %server_streak[%p%]%
            //
            // We convert the var name into something valid globally
            // i.e. %kills% -> %server_kills[<playername>]%
            // %squad_kills% -> %server_squad_kills[1][2]% (where 1 = team, 2 = squad id's)
            // %team_kills% -> %server_team_kills[1]% (where "1" is team id for player)
            // %server_kills% -> unchanged
            private string mangle(string player_name, string var_name)
            {
                var_name = var_name.Replace("$", "%");
                // check for a valid var name %...%
                if (var_name == null || 
                    var_name.Length < 3 || 
                    !var_name.StartsWith("%") || 
                    !var_name.EndsWith("%")) return null;
                // replace [%vars%] in this var name with their value
                var_name = replace_index_vars(player_name, var_name);
                // if it's a 'server variable' return it unchanged
                if (var_name.ToLower().StartsWith("%server_")) return var_name;
                // ini var name - return unchanged
                if (var_name.ToLower() == "%ini%") return var_name;
                if (var_name.ToLower().StartsWith("%ini_")) return var_name;
                // see if it's a team, squad or a player variable which means we much have a valid player name
                if (player_name == null) return null;
                // raw_name is a var name without the % % 
                string raw_name = var_name.Substring(1, var_name.Length - 2);
                if (var_name.ToLower().StartsWith("%team_")) 
                    // e.g. %team_kills% -> %server_team_kills[1]% (where "1" is team id for player)
                    return "%server_" + raw_name + "[" + players.team_id(player_name).ToString() + "]%";
                if (var_name.ToLower().StartsWith("%squad_"))
                    // e.g. %squad_kills% -> %server_squad_kills[1][2]% (where 1 = team, 2 = squad id's)
                    return "%server_" + raw_name + "[" + players.team_id(player_name).ToString() + "][" +
                        players.squad_id(player_name).ToString() + "]%";
                // proc has been called with a 'player variable', 
                // e.g. "%streak%", mangle to "server_streak[playername]%"
                return "%server_" + raw_name + "[" + player_name + "]%";
            }

            // value of an expression : replace %v%-type subst values, then replace %streak%-type rulz vars:
            private string get_value(string player_name, string input_exp, Dictionary<SubstEnum, string> keywords)
            {
                // replace substitution variables, e.g. %p% with playername from keywords
                string substituted_exp = replace_keys(input_exp, keywords);
                // now exp doesn't contain any substitution vars
                // replace user vars with their values, e.g. %server_kills% or whatever user has used in rulz
                string replaced_exp = replace_vars(player_name, substituted_exp);
                // now exp has no vars, but may include arithmetic
                string reduced_exp = reduce(replaced_exp); // "1+1" -> "2"
                return reduced_exp;
            }
 
            // convert %ini_<section>_<var>% to Array[0..2]<string> [ini,<section_name>,<var_name>]
            string[] var_to_ini(string full_var_name)
            {
                string[] ini_parts = new string[3];
                if (full_var_name == "%ini%")
                {
                    ini_parts[0] ="ini";
                }
                else if (full_var_name.StartsWith("%ini_"))
                {
                    ini_parts[0] = "ini";
                    // e.g. %ini_vars_plugin_settings% 
                    string[] var_parts = full_var_name.Split('_');
                    if (var_parts.Length >= 2)
                    {
                        if (var_parts.Length == 2) // no section name, e.g. %ini_myvar% so default to [vars]
                        {
                            ini_parts[1] = var_parts[1].TrimEnd(new char[] { '%' }); // var name
                        }
                        else
                        {
                            ini_parts[1] = var_parts[1]; // section name
                            ini_parts[2] = full_var_name.Substring(5 + var_parts[1].Length + 1).TrimEnd(new char[] { '%' });
                        }
                    }
                }
                return ini_parts;
            }

            // find the value of an 'atom' i.e. a string, int or variable
            // exp can be a variable name or an integer or a string
            private string atom_value(string player_name, string exp)
            {
                if (exp == null || exp == "") return "";
                // try to get a number
                float i;
                try
                {
                    i = float.Parse(exp);
                    return i.ToString();
                }
                catch { }
                // ok, we didn't get an int, so try a variable lookup
                // if not a %..% var just return the string
                if (!exp.StartsWith("%") || !exp.EndsWith("%")) return exp;
                string full_var_name = mangle(player_name, exp);
                if (full_var_name == null) return "";
                // return the variable value if there is one
                if (vars.ContainsKey(full_var_name))
                    return vars[full_var_name];
                else // we'll try the ini values
                {
                    //convert %ini_<section>_<varname>% into list [section_name, var_name]
                    string[] ini_parts = var_to_ini(full_var_name);
                    if (ini_parts[0] != null)
                    {
                        string section_name = ((ini_parts[1] == null) ? "vars" : ini_parts[1]);
                        string var_name = ini_parts[2];
                        if ( ini_vars.ContainsKey(section_name) &&
                             var_name != null &&
                             ini_vars[section_name].ContainsKey(var_name))
                        {
                            return ini_vars[section_name][var_name];
                        }
                    }
                }

                // this *is* a %..% var but we didn't get a value, so we'll return "0"
                return "0";
            }

            // SET the value of a variable
            // if playername is null, var must be %server_..%
            // keywords can be null (so no keyword substitutions)
            public void set_value(string player_name, string var_name, string assign_value, Dictionary<SubstEnum, string> keywords)
            {
                // substitute the %mm% type subst vars
                var_name = keywords == null ? var_name : replace_keys(var_name, keywords);

                string full_var_name = mangle(player_name, var_name);
                if (full_var_name == null) return;

                // now get result of assign_value
                string result = get_value(player_name, assign_value, keywords);

                // see if this var has a 'rounding' attribute, e.g. %xxx.2%
                int var_index = var_name.LastIndexOf('.');
                if (var_index > 0)
                {
                    // ok so it IS a 'rounding var'
                    // so check if the result has a '.' in it...
                    int result_index = result.LastIndexOf('.');
                    if (result_index >= 0)
                    {
                        // so now we're looking at "Set %x.2% 1.23456"
                        try
                        {
                            // decimals is the number in the var name after the '.'
                            int decimals = Int32.Parse(var_name.Substring(var_index + 1, var_name.Length - var_index - 2));
                            // add/subtract 0.005 (or similar) so truncate = rounding
                            double result_float = double.Parse(result);
                            double adjust = 0.5 / Math.Pow(10, decimals); // here is rounding adjust value
                            if (result_float >= 0)
                            {
                                result_float = result_float + adjust;
                            }
                            else
                            {
                                result_float = result_float - adjust;
                            }
                            result = result_float.ToString();

                            result_index = result.LastIndexOf('.'); // did '.' move ?
                            if (decimals == 0)
                            {
                                // for %x.0% remove the '.' as well as the decimal digits...
                                result = result.Substring(0, result_index);
                            }
                            else
                            {
                                // here's where we update 1.23456 to 1.23
                                result = result.Substring(0, result_index + decimals + 1);
                                // an exception could occur above if the string is too short, so result is unchanged
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                // try and get ini var parts from full_var_name
                string[] ini_parts = var_to_ini(full_var_name);
                if (ini_parts[0] == null)
                {
                    // didn't parse an ini section_name/var_name so do simple vars assign
                    vars[full_var_name] = result;
                }
                else
                {
                    // successfully parsed %ini_section_var% so assign and update ini file
                    string ini_section_name = ini_parts[1];
                    string ini_var_name = ini_parts[2];
                    // load entire file...
                    ini_load(ini_filename);
                    // update our local value ini_vars
                    ini_set_value(ini_section_name, ini_var_name, result);
                    // write all ini_vars out to proconrulz.ini file
                    ini_save(ini_filename);
                }
                return;
            }

            // INCREMENT THE VALUE OF A VARIABLE
            public void incr(string player_name, string var_name, Dictionary<SubstEnum, string> keywords)
            {
                int result = 1;
                try
                {
                    result = Int32.Parse(get_value(player_name, var_name, keywords)) + 1;
                }
                catch { }
                set_value(player_name, var_name, result.ToString(), keywords);
                return;
            }

            // DECREMENT THE VALUE OF A VARIABLE
            public void decr(string player_name, string var_name, Dictionary<SubstEnum, string> keywords)
            {
                int result = 0;
                try
                {
                    result = Int32.Parse(get_value(player_name, var_name, keywords)) - 1;
                    if (result < 0) result = 0;
                }
                catch { }
                set_value(player_name, var_name, result.ToString(), keywords);
                return;
            }

            public bool test(string player_name, string val_i, string cond, string val_j, Dictionary<SubstEnum, string> keywords)
            {
                string i = get_value(player_name, val_i, keywords).ToLower();
                string j = get_value(player_name, val_j, keywords).ToLower();
                switch (cond.ToLower())
                {
                    case "=":  return i == j;
                    case "==": return i == j;
                    case "!=": return i != j;
                    case "<>": return i != j;
                    case ">":  return bigger(i,j);                  // i > j
                    case "<":  return (i != j) && !bigger(i,j);    // i < j
                    case "=>": return (i == j) || bigger(i, j);    // i >= j
                    case ">=": return (i == j) || bigger(i, j);    // i >= j
                    case "<=": return !bigger(i, j);               // i <= j;
                    case "=<": return !bigger(i, j);               // i <= j;
                    case "contains": return i.Contains(j); // i contains j
                    case "word": return Regex.IsMatch(i, string.Format(@"\b{0}\b",j), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    default: return false;
                }
            }

            // 
            private bool bigger(string x, string y)
            {
                float i = 0, j = 0;
                try
                {
                    i = float.Parse(x);
                    j = float.Parse(y);
                    return i > j;
                }
                catch { }
                return String.Compare(x, y, true) > 0;
            }

            // reduce arithmetic expression to their value e.g. "2+2" -> "4"
            public string reduce(string exp)
            {
                if (!exp.Contains("+") && !exp.Contains("-") && !exp.Contains("*") && !exp.Contains("/"))
                {
                    return exp;
                }
                string left, right, left_result, right_result;
                float left_float = 0, right_float = 0;
                bool left_num = false;
                bool right_num = false;
                foreach (char op in new char[4] { '+', '-', '*', '/' })
                {
                    int i = exp.IndexOf(op);
                    if (i > 0)
                    {
                        left = exp.Substring(0, i);
                        right = exp.Substring(i + 1, exp.Length - i - 1);
                        left_result = reduce(left);
                        right_result = reduce(right);
                        try
                        {
                            left_float = float.Parse(left_result);
                            left_num = true;
                        }
                        catch { };
                        try
                        {
                            right_float = float.Parse(right_result);
                            right_num = true;
                        }
                        catch { };
                        if (left_num && right_num)
                        {
                            switch (op)
                            {
                                case '+':
                                    return (left_float + right_float).ToString();

                                case '-':
                                    return (left_float - right_float).ToString();

                                case '*':
                                    return (left_float * right_float).ToString();

                                case '/':
                                    if (right_float == 0) return "0";
                                    return (left_float / right_float).ToString();

                            }
                        }
                        if (left_num)
                        {
                            return left_float.ToString() + op + right_result;
                        }
                        if (right_num)
                        {
                            return left_result + op + right_float.ToString();
                        }
                        return left_result + op + right_result;
                    }
                }
                return exp;
            }

            // replace %var_name% in message with the var value
            // note it could be a var name using subst variables %server_%p%_deaths%
            // message already has subst vars replaced (e.g. %p%)
            public string replace_vars(string player_name, string message)
            {
                message = message.Replace("$", "%");
                // e.g. message is "player K/D is %kills%/%deaths%"
                if (message == null) return null;
                if (message.Length < 3) return message;

                // first replace rulz vars inside [ brackets e.g. %kills[%killer%]% -> %kills[bambam]%
                message = replace_index_vars(player_name, message);

                int j = 0;
                int fragment_start = 0;
                string vars_message = "";
                while (fragment_start < message.Length)
                {
                    int i = message.IndexOf("%", fragment_start);
                    j = -1; // second % not found - update if we find first %
                    if (i >= 0 && message.Length - i > 2) j = message.IndexOf("%", i + 2);
                    // now i is index of 1st %, and j the 2nd
                    // add the non-var fragment
                    if (i < 0 || j < 0) // didn't find the first % or second %
                    {
                        vars_message += message.Substring(fragment_start);
                        break;
                    }
                    // add the non-var piece
                    vars_message += message.Substring(fragment_start, i - fragment_start);
                    // add the var subsitution
                    vars_message += atom_value(player_name, message.Substring(i, j - i + 1));
                    fragment_start = j + 1;
                }
                return vars_message;
            }

            // here we replace the %...[%...%]% vars between []
            // e.g. %server_kills[%server_killername%]% -> %server_kills[bambam]%
            // note that the user could wrap a non-nested var in [] in a message, e.g. [%x%]
            public string replace_index_vars(string player_name, string message)
            {
                if (message == null) return null;
                if (message.Length < 8) return message; // "%x[%y%]%" is min length for any nested var

                int j = 0;
                int fragment_start = 0;
                string vars_message = "";
                bool in_var = false; // toggle to keep track of whether we're inside a %..% var or not
                while (fragment_start < message.Length)
                {
                    // find the start of the non-nested var
                    int i = message.IndexOf("%", fragment_start);
                    if (i < 0) // didn't find the first %
                    {
                        vars_message += message.Substring(fragment_start);
                        break;
                    }
                    // found a '%'
                    if (!in_var)
                    {
                        in_var = true;
                        // i is pointing to the second % of a non-nested var
                        // add the non-var piece up to and including the second %
                        vars_message += message.Substring(fragment_start, i - fragment_start + 1);
                        // copy fragment up to here and continue
                        fragment_start = i + 1;
                        continue;
                    }

                    // in_var = true
                    if (message[i - 1] != '[')
                    {
                        // in a var, but this is not [% opening a nested var
                        in_var = false;
                        // i is pointing to the second % of a non-nested var
                        // add the non-var piece up to and including the second %
                        vars_message += message.Substring(fragment_start, i - fragment_start + 1);
                        // copy fragment up to here and continue
                        fragment_start = i + 1;
                        continue;

                    }
                    // now i points to % in [% at start of nested var
                    // so HERE we have start of a nested var with % at i after [
                    j = -1; // now find %]
                    if (message.Length - i > 3) j = message.IndexOf("%]", i + 2);
                    // now i is index of 1st %, and j the index of %]
                    // add the non-var fragment
                    if (j < 0) // didn't find the %]
                    {
                        vars_message += message.Substring(fragment_start);
                        break;
                    }
                    // add the non-var piece up to and including the [
                    vars_message += message.Substring(fragment_start, i - fragment_start);
                    // add the var subsitution
                    vars_message += atom_value(player_name, message.Substring(i, j - i + 1));
                    // now add the closing ]
                    vars_message += "]";
                    fragment_start = j + 2;
                }
                return vars_message;
            }

            // return all the vars - called on "prdebug dump"
            public Dictionary<string, string> dump() { return vars; }

            /* Read/Write .ini Files
            /// 
            /// Version 1, 2009-08-15
            /// http://www.Stum.de
            /// It supports the simple .INI Format:
            /// 
            /// [SectionName]
            /// Key1=Value1
            /// Key2=Value2
            /// 
            /// [Section2]
            /// Key3=Value3
            /// 
            /// You can have empty lines (they are ignored), but comments are not supported
            /// Key4=Value4 ; This is supposed to be a comment, but will be part of Value4
            /// 
            /// Whitespace is not trimmed from the beginning and end of either Key and Value
            /// 
            /// Licensed under WTFPL
            /// http://sam.zoy.org/wtfpl/
            */ 
    
            private readonly Regex _sectionRegex = new Regex(@"^\[(?<SectionName>[^\]]+)(?=\])");
            private readonly Regex _keyValueRegex = new Regex(@"(?<Key>[^=]+)=(?<Value>.+)");
        
            /// Get a specific value from the .ini file
            /// <returns>The value of the given key in the given section, or NULL if not found</returns>
            public string ini_get_value(string sectionName, string key)
            {
                if (ini_vars.ContainsKey(sectionName) && ini_vars[sectionName].ContainsKey(key))
                    return ini_vars[sectionName][key];
                else
                    return null;
            }

            /// Set a specific value in a section
            public void ini_set_value(string sectionName, string key, string value)
            {
                // remove entry if value is 0
                if (value == "0")
                {
                    if (sectionName != null)
                    {
                        if (ini_vars.ContainsKey(sectionName))
                        {
                            if (key != null)
                            {
                                //remove this variable
                                ini_vars[sectionName].Remove(key);
                                // if section is now empty, remove that
                                if (ini_vars[sectionName].Count == 0)
                                {
                                    ini_vars.Remove(sectionName);
                                }
                            }
                            else
                            {
                                // sectionName not null, key is null, so remove section
                                ini_vars.Remove(sectionName);
                            }
                        }
                    }
                    else
                    {
                        // sectionName is null, so reset ALL variables
                        ini_vars.Clear();
                    }
                }
                else
                {
                    if (!ini_vars.ContainsKey(sectionName)) ini_vars[sectionName] = new Dictionary<string, string>();
                    ini_vars[sectionName][key] = value;
                }
            }

            /// Get all the Values for a section
            public Dictionary<string, string> ini_get_section(string sectionName)
            {
                if (ini_vars.ContainsKey(sectionName))
                    return new Dictionary<string, string>(ini_vars[sectionName]);
                else
                    return new Dictionary<string, string>();
            }

            /// Set an entire sections values
            public void ini_set_section(string sectionName, IDictionary<string, string> sectionValues)
            {
                if (sectionValues == null) return;
                ini_vars[sectionName] = new Dictionary<string, string>(sectionValues);
            }
            
            /// Load an .INI File
            public bool ini_load(string filename)
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        string[] content;
                        content = File.ReadAllLines(filename);
                        ini_vars = new Dictionary<string, Dictionary<string, string>>();
                        string currentSectionName = string.Empty;
                        foreach (string line in content)
                        {
                            Match m = _sectionRegex.Match(line.Trim());
                            if (m.Success)
                            {
                                currentSectionName = m.Groups["SectionName"].Value;
                            }
                            else
                            {
                                m = _keyValueRegex.Match(line);
                                if (m.Success)
                                {
                                    string key = m.Groups["Key"].Value.Trim();
                                    string value = m.Groups["Value"].Value.Trim();

                                    Dictionary<string, string> kvpList;
                                    if (ini_vars.ContainsKey(currentSectionName))
                                    {
                                        kvpList = ini_vars[currentSectionName];
                                    }
                                    else
                                    {
                                        kvpList = new Dictionary<string, string>();
                                    }
                                    kvpList[key] = value;
                                    ini_vars[currentSectionName] = kvpList;
                                }
                            }
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }
            }

            /// Save the content of this class to an INI File
            public bool ini_save(string filename)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                if (ini_vars != null)
                {
                    foreach (string sectionName in ini_vars.Keys)
                    {
                        sb.AppendFormat("[{0}]\r\n", sectionName);
                        foreach (string keyValue in ini_vars[sectionName].Keys)
                        {
                            sb.AppendFormat("{0}={1}\r\n", keyValue, ini_vars[sectionName][keyValue]);
                        }
                    }
                }
                try
                {
                    File.WriteAllText(filename, sb.ToString());
                    return true;
                } catch
                {
                    return false;
                }
            }
        }

        #endregion

        #region Global Vars

        #region server_ip.cfg variables
        //**********************************************************************************************
        //**********************************************************************************************
        //   VARIABLES USED IN server_ip.cfg
        //**********************************************************************************************
        //**********************************************************************************************

        // game type (for loading rulz files)
        GameIdEnum game_id = GameIdEnum.BF4;

        int yell_delay = 5; // how long to have the yell message on screen

        int kill_delay = 5000; // default (milliseconds) delay if the *rule* does not specify a delay 
                               // between the event and the player being killed

        int ban_delay = 60 * 60 * 24 * 7; // default temp ban is for a week in secs (not server_ip.cfg)

        // If 'yes' then players on procon 'Reserved' 
        // list have immunity from kick/kill
        ProtectEnum protect_players = ProtectEnum.Admins;

        // installing admin has to check the "Rules of Conduct" checkbox
        enumBoolYesNo roc_read = enumBoolYesNo.No; 

        ReserveItemEnum reservationMode = ReserveItemEnum.Player_reserves_item_until_respawn;

        // a flag if yes will output debug console writes
        static enumBoolYesNo trace_rules = enumBoolYesNo.No;

        // this list stores the rules read from server_ip.cfg and user rulz files
        //static Dictionary<string, List<string>> unparsed_rulz = new Dictionary<string, List<string>>();
        static List<string> unparsed_rules = new List<string>();

        // list of rulz.txt filenames to load
        static List<string> rulz_filenames = new List<string>();

        List<string> whitelist_players = new List<string>(); // list of playernames to be protected

        List<string> whitelist_clans = new List<string>(); // list of clans to be protected

        LogFileEnum log_file = LogFileEnum.PluginConsole;

        #endregion

        //**********************************************************************************************
        //**********************************************************************************************
        //   GLOBAL VARIABLES
        //**********************************************************************************************
        //**********************************************************************************************

        string auth_name = "bambam_ofc, Ciavi"; // Bambam, author of this plugin (see forum.myrcon.com), Ciavi kinda tweaked and added a few things starting with 45a.0.

        bool plugin_enabled; // set true and false by Procon, when admin enables/disables this plugin

        WeaponDictionary weaponDefines;       // <string, Weapon>
        SpecializationDictionary specDefines; // <string, Specialization>
        
        static PlayerList players = new PlayerList();

        // record GUIDs of players
        Dictionary<string, CPlayerInfo> player_info = new Dictionary<string, CPlayerInfo>();

        // lists of players spawned with watched items
        SpawnCounts spawn_counts = new SpawnCounts();
       
        // kill_counts accumulates the item counts on KILL for each player
        // e.g. kill_counts["bambam"]["AUG"] = 3 means bambam has 3 AUG kills.
        // count added with add_kill_count(player_name, item_name)
        // count retrieved with count_kill_item(player_name, item_name)
        Dictionary<string, Dictionary<string, int>> kill_counts 
            = new Dictionary<string, Dictionary<string, int>>();

        // the count of number of times each player has triggered each rule
        // i.e. player_name -> <rule_id,count>
        // reset at round start
        Dictionary<string, Dictionary<int, int>> rule_counts 
            = new Dictionary<string, Dictionary<int, int>>();

        // timestamps of players triggering rules, for 'Rate' calculations
        // i.e. player_name -> <rule_id,count>
        // reset when plugin loaded. Meanwhile players are individually 
        // 'scrubbed' out of the timestamp lists
        Dictionary<string, Dictionary<int, DateTime[]>> rule_times 
            = new Dictionary<string, Dictionary<int, DateTime[]>>();

        // player_blocks will block a player with a given item, triggering PlayerBlock rule
        Dictionary<string, List<string>> player_blocks = new Dictionary<string, List<string>>();

        // Dictionary playername->kit_key of the Kit the player spawned with
        Dictionary<string, string> player_kit = new Dictionary<string, string>();

        // whitelist of players not to kick or kill
        List<string> reserved_slot_players = new List<string>();

        // list of unparsed rules from user rulz files
        Dictionary<string, string[]> filez_rulz = new Dictionary<string, string[]>();

        // list of parsed rules loaded from user
        List<ParsedRule> parsed_rules = new List<ParsedRule>(); 
        
        CMap current_map; // map info of current loaded map, set by OnLoadingLevel()
        string current_map_mode = "None"; // mod for BF3, cannot derive map mode from map name

        int rulz_spam_limit = 2; // max # times a player can request RULZ/MOTD

        bool rulz_enable = true;

        // WriteDebugInfo will increment this - Procon bug may duplicate lines in log?
        int debug_write_count = 0;

        char rulz_key_separator = '&'; // char used to replace ' ' in rulz
        static char rulz_item_separator = ','; // separator for items in a condition, e.g. Weapon SMAW,RPG-7

        // BF4 only trigger OnRoundOver when ALL 3 events have completed (OnRoundOver/Players/TeamScores)
        int round_over_event_count = 0; // zeroed in OnLevelLoaded, incr in RoundOver events

        // object to hold runtime rulz variables
        VarsClass rulz_vars = null;

        ParsedRule rule_prefix; // used in parse_rules() as default first parts if rule has no trigger
                                     // at start of parse initialized to TriggerEnum.Spawn

        #endregion

        #region Populate dictionary of %% substitution keys and startup default rules

        public ProconRulz()
        {
            subst_keys.Add(SubstEnum.Player, new List<string>());
            subst_keys[SubstEnum.Player].Add("%p%");
            subst_keys[SubstEnum.Player].Add("$p$");
            subst_keys.Add(SubstEnum.Victim, new List<string>());
            subst_keys[SubstEnum.Victim].Add("%v%");
            subst_keys.Add(SubstEnum.Weapon, new List<string>());
            subst_keys[SubstEnum.Weapon].Add("%w%");
            subst_keys.Add(SubstEnum.WeaponKey, new List<string>());
            subst_keys[SubstEnum.WeaponKey].Add("%wk%");
            subst_keys.Add(SubstEnum.Damage, new List<string>());
            subst_keys[SubstEnum.Damage].Add("%d%");
            subst_keys.Add(SubstEnum.DamageKey, new List<string>());
            subst_keys[SubstEnum.DamageKey].Add("%dk%");
            subst_keys.Add(SubstEnum.Kit, new List<string>());
            subst_keys[SubstEnum.Kit].Add("%k%");
            subst_keys.Add(SubstEnum.VictimKit, new List<string>());
            subst_keys[SubstEnum.VictimKit].Add("%vk%");
            subst_keys.Add(SubstEnum.KitKey, new List<string>());
            subst_keys[SubstEnum.KitKey].Add("%kk%");
            subst_keys.Add(SubstEnum.Spec, new List<string>()); // available On Spawn only
            subst_keys[SubstEnum.Spec].Add("%spec%"); // available On Spawn only
            subst_keys.Add(SubstEnum.SpecKey, new List<string>()); // available On Spawn only
            subst_keys[SubstEnum.SpecKey].Add("%speck%"); // available On Spawn only
            subst_keys.Add(SubstEnum.Count, new List<string>());
            subst_keys[SubstEnum.Count].Add("%c%");
            subst_keys.Add(SubstEnum.TeamCount, new List<string>());
            subst_keys[SubstEnum.TeamCount].Add("%tc%");
            subst_keys.Add(SubstEnum.ServerCount, new List<string>());
            subst_keys[SubstEnum.ServerCount].Add("%sc%");
            subst_keys.Add(SubstEnum.PlayerTeam, new List<string>());
            subst_keys[SubstEnum.PlayerTeam].Add("%pt%");
            subst_keys.Add(SubstEnum.PlayerSquad, new List<string>());
            subst_keys[SubstEnum.PlayerSquad].Add("%ps%");
            subst_keys.Add(SubstEnum.PlayerTeamKey, new List<string>());
            subst_keys[SubstEnum.PlayerTeamKey].Add("%ptk%");
            subst_keys.Add(SubstEnum.VictimTeamKey, new List<string>());
            subst_keys[SubstEnum.VictimTeamKey].Add("%vtk%");
            subst_keys.Add(SubstEnum.PlayerSquadKey, new List<string>());
            subst_keys[SubstEnum.PlayerSquadKey].Add("%psk%");
            subst_keys.Add(SubstEnum.VictimTeam, new List<string>());
            subst_keys[SubstEnum.VictimTeam].Add("%vt%");
            subst_keys.Add(SubstEnum.Range, new List<string>());
            subst_keys[SubstEnum.Range].Add("%r%");
            subst_keys.Add(SubstEnum.BlockedItem, new List<string>());
            subst_keys[SubstEnum.BlockedItem].Add("%b%");
            subst_keys.Add(SubstEnum.Teamsize, new List<string>());
            subst_keys[SubstEnum.Teamsize].Add("%n%");
            subst_keys.Add(SubstEnum.Teamsize1, new List<string>());
            subst_keys[SubstEnum.Teamsize1].Add("%ts1%");
            subst_keys.Add(SubstEnum.Teamsize2, new List<string>());
            subst_keys[SubstEnum.Teamsize2].Add("%ts2%");
            subst_keys.Add(SubstEnum.PlayerTeamsize, new List<string>());
            subst_keys[SubstEnum.PlayerTeamsize].Add("%pts%");
            subst_keys.Add(SubstEnum.Headshot, new List<string>());
            subst_keys[SubstEnum.Headshot].Add("%h%");
            subst_keys.Add(SubstEnum.Map, new List<string>());
            subst_keys[SubstEnum.Map].Add("%m%");
            subst_keys.Add(SubstEnum.MapMode, new List<string>());
            subst_keys[SubstEnum.MapMode].Add("%mm%");
            subst_keys.Add(SubstEnum.Target, new List<string>()); // e.g. playername from say text
            subst_keys[SubstEnum.Target].Add("%t%"); // e.g. playername from say text
            subst_keys.Add(SubstEnum.Text, new List<string>()); // e.g. say text
            subst_keys[SubstEnum.Text].Add("%text%"); // e.g. say text
            subst_keys.Add(SubstEnum.TargetText, new List<string>()); // used only by TargetPlayer
            subst_keys[SubstEnum.TargetText].Add("%targettext%"); // used only by TargetPlayer
            subst_keys.Add(SubstEnum.Ping, new List<string>()); // ping in milliseconds (i.e as displayed)
            subst_keys[SubstEnum.Ping].Add("%ping%"); // ping in milliseconds (i.e as displayed)
            subst_keys.Add(SubstEnum.EA_GUID, new List<string>()); // from admin.listPlayers
            subst_keys[SubstEnum.EA_GUID].Add("%ea_guid%"); // from admin.listPlayers
            subst_keys.Add(SubstEnum.PB_GUID, new List<string>()); // from OnPunkbusterPlayerInfo
            subst_keys[SubstEnum.PB_GUID].Add("%pb_guid%"); // from OnPunkbusterPlayerInfo
            subst_keys.Add(SubstEnum.IP, new List<string>()); // IP address
            subst_keys[SubstEnum.IP].Add("%ip%"); // IP address
            subst_keys.Add(SubstEnum.PlayerCountry, new List<string>()); // from pb info
            subst_keys[SubstEnum.PlayerCountry].Add("%pcountry%"); // from pb info
            subst_keys.Add(SubstEnum.PlayerCountryKey, new List<string>()); // from pb info
            subst_keys[SubstEnum.PlayerCountryKey].Add("%pcountrykey%"); // from pb info
            subst_keys.Add(SubstEnum.VictimCountry, new List<string>()); // from pb info
            subst_keys[SubstEnum.VictimCountry].Add("%vcountry%"); // from pb info
            subst_keys.Add(SubstEnum.VictimCountryKey, new List<string>()); // from pb info
            subst_keys[SubstEnum.VictimCountryKey].Add("%vcountrykey%"); // from pb info

            subst_keys.Add(SubstEnum.Hhmmss, new List<string>()); // time HH:MM:SS
            subst_keys[SubstEnum.Hhmmss].Add("%hms%");
            subst_keys.Add(SubstEnum.Seconds, new List<string>()); // time seconds
            subst_keys[SubstEnum.Seconds].Add("%seconds%");
            subst_keys.Add(SubstEnum.Date, new List<string>()); // time seconds
            subst_keys[SubstEnum.Date].Add("%ymd%");

            // default rulz
            unparsed_rules.Add("#             JOINER/LEAVER LOG");
            unparsed_rules.Add("On Join;Say ^2%p%^0 has joined the server");
            unparsed_rules.Add("On Leave;Say ^2%p%^0 has left the server");

            rulz_filenames.Add("proconrulz_rules.txt");

        }
        
        #endregion

        #region Plugin startup routines and On Init procedure
        //**********************************************************************************************
        //**********************************************************************************************
        //   PROCON STARTUP ROUTINES
        //**********************************************************************************************
        //**********************************************************************************************
        
        public string GetPluginName()  { return "ProconRulz"; }

        public string GetPluginVersion() { return version; }

        public string GetPluginAuthor() { return auth_name; }

        public string GetPluginWebsite() { return ""; }

        public string GetPluginDescription()  { return get_details(); }
        
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            rulz_vars = new VarsClass(String.Format("{0}_{1}", strHostName, strPort));
            WriteConsole("ProconRulz loaded");
            try
            {
                weaponDefines = GetWeaponDefines();
            }
            catch
            {
                WriteConsole("ProconRulz: ^1failed to load weapon definitions");
            }
            try
            {
                specDefines = GetSpecializationDefines();
            }
            catch
            {
                WriteConsole("ProconRulz: ^1failed to load spec definitions");
            }

            WriteConsole(String.Format("weaponDefines size = {0}, specDefines size = {1}",
                                            weaponDefines.Count,
                                            specDefines.Count));

            this.RegisterEvents(this.GetType().Name, 
                                                        "OnPlayerSpawned",
                                                        "OnPlayerKilled",
                                                        "OnPlayerTeamChange",
                                                        "OnPlayerSquadChange",
                                                        "OnPlayerJoin",
                                                        "OnPlayerLeft",
                                                        "OnListPlayers",
                                                        "OnReservedSlotsList",
                                                        "OnRoundOver",
                                                        "OnRoundOverPlayers",
                                                        "OnRoundOverTeamScores",
                                                        "OnLoadingLevel", // for BFBC2
                                                        "OnLevelLoaded",  // for BF3
                                                        "OnCurrentLevel", // BFBC2 only
                                                        "OnGlobalChat",
                                                        "OnTeamChat",
                                                        "OnSquadChat",
                                                        "OnServerInfo",
                                                        "OnPunkbusterPlayerInfo"
                                                        );

            // exec listPlayers to initialise players global
            ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnPluginEnable()
        {
            plugin_enabled = true;
            this.WriteConsole(String.Format("^bProconRulz: ^2plugin enabled, version {0}", version));
            WriteDebugInfo(String.Format("weaponDefines size = {0}, specDefines size = {1}",
                                            weaponDefines.Count,
                                            specDefines.Count));
            rulz_enable = true;
            load_rulz_from_files();
            reset_counts(); // reset the list of 'watched' items and their counts
            // reset rulz runtime vars
            rulz_vars.reset();

            // search for user rulz file (Plugins/<gameid>/*rulz.txt)
            //load_rulz_from_files(game_id);

            parse_rules();
            load_reserved_slot_players();
            // exec currentLevel to initialise current_map global, so we don't have to wait for a map load
            // ** NOT working in BF3 R8
            ExecuteCommand("procon.protected.send", "admin.currentLevel");
        }

        public void OnPluginDisable()
        {
            plugin_enabled = false;
            WriteConsole("^bProconRulz: ^1plugin disabled");
            
            // the rest of this proc is just to output some debug info when user selects "disable plugin"
            WriteDebugInfo("ProconRulz: These rules were loaded from settings:");
            foreach (ParsedRule rule in parsed_rules)
            {
                print_parsed_rule(rule);
            } // end looping through parsed_rules
            WriteDebugInfo(String.Format("ProconRulz: These were the 'watched' items in the rules:"));
            WriteDebugInfo(String.Format("ProconRulz: {0}", 
                string.Join(", ", spawn_counts.list_items().ToArray())));

        }

        // proconRulz On Init (on plugin load, enable, round start)
        public void OnInit()
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: ********************OnInit******************************");

                scan_rules(TriggerEnum.Init, null,
                    new Dictionary<SubstEnum, string>(), null, null, null);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnInit");
                PrintException(ex);
            }
        }

        #endregion

        #region plugin settings incl. load the rulz
        //**********************************************************************************************
        //**********************************************************************************************
        //                         MANAGE THE PLUGIN SETTINGS
        //**********************************************************************************************
        //**********************************************************************************************

        // load rulz .txt files listed in rulz_filenames
        void load_rulz_from_files()
        {
            if (rulz_filenames.Count == 0) return;
            try
            {
                //string folder = @"Plugins\"+game_id.ToString()+@"\";
                string folder = @"Plugins"+Path.DirectorySeparatorChar+game_id.ToString()+Path.DirectorySeparatorChar;
                WriteConsole("ProconRulz: Loading rulz from .txt files in " + folder);
                //string[] file_paths = Directory.GetFiles(folder, "proconrulz_*.txt");
                // do nothing and return if no rulz files found
                //if (file_paths.Length == 0)
                //{
                //    WriteConsole("ProconRulz: no user rulz files defined (will just use settings)");
                //    return;
                //}

                // start with an empty list of user rulz from files
                filez_rulz.Clear();

                foreach (string filename in rulz_filenames)
                {
                    if (File.Exists(folder + filename))
                    {
                        WriteConsole("ProconRulz: Loading " + folder + filename);
                        string[] rulz = System.IO.File.ReadAllLines(folder + filename);
                        if (rulz.Length > 0 & !rulz[0].Contains("#disable"))
                            filez_rulz.Add(filename, rulz);
                    }
                    else
                    {
                        string path = Path.GetFullPath(folder + filename);
                        WriteConsole("ProconRulz: Skipping " + path + " NOT FOUND");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }

        }

        // plugin variables are declared in 'Global Vars' region above

        // ASSIGN values to program globals FROM server_ip.cfg and from Plugin Settings pane
        public void SetPluginVariable(string strVariable, string raw_strValue)
        {
            try
            {
                string strValue = CPluginVariable.Decode(raw_strValue);
                switch (strVariable)
                {
                    case "Game":
                        game_id = (GameIdEnum)Enum.Parse(typeof(GameIdEnum), strValue);
                        break;
                    case "Delay before kill":
                        kill_delay = Int32.Parse(strValue);
                        break;
                    case "Yell seconds":
                        yell_delay = Int32.Parse(strValue);
                        break;
                    //case "Player keeps items on respawn":
                    //    reservationMode = (ReserveItemEnum)Enum.Parse(typeof(ReserveItemEnum), strValue);
                    //    break;
                    case "EA Rules of Conduct read and accepted":
                        roc_read = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                        break;
                    case "Trace rules":
                        trace_rules = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                        break;
                    case "Protect these players from Kick or Kill":
                        protect_players
                            = (ProtectEnum)Enum.Parse(typeof(ProtectEnum), strValue);
                        break;
                    case "Rules":
                        string strValueHtmlDecode = raw_strValue.Contains(" ") ? raw_strValue : raw_strValue.Replace("+", " ");
                        string strValueUnencoded;
                        try
                        {
                            strValueUnencoded = Uri.UnescapeDataString(strValueHtmlDecode);
                        }
                        catch
                        {
                            strValueUnencoded = strValueHtmlDecode;
                        }
                        unparsed_rules = new List<string>(strValueUnencoded.Split(new char[] { '|' }));
                        rulz_vars.reset();
                        reset_counts();
                        parse_rules();
                        break;
                    case "Rulz .txt filenames":
                        rulz_filenames = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                        // look for rulz files and add those to unparsed_rules
                        load_rulz_from_files();
                        rulz_vars.reset();
                        reset_counts();
                        parse_rules();
                        break;
                    case "Player name whitelist":
                        whitelist_players = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                        break;
                    case "Clan name whitelist":
                        whitelist_clans = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                        break;
                    case "Send ProconRulz Log messages to:":
                        log_file = (LogFileEnum)Enum.Parse(typeof(LogFileEnum), strValue);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        // Allow procon to READ current values of program globals (and write values to server_ip.cfg)
        public List<PRoCon.Core.CPluginVariable> GetDisplayPluginVariables()
        {
            try
            {
                List<CPluginVariable> lst = new List<CPluginVariable>();
                lst.Add(new CPluginVariable("Game",
                    CreateEnumString(typeof(GameIdEnum)), game_id.ToString()));
                lst.Add(new CPluginVariable("Delay before kill", typeof(int), kill_delay));
                lst.Add(new CPluginVariable("Yell seconds", typeof(int), yell_delay));
                //lst.Add(new CPluginVariable("Player keeps items on respawn",
                //    CreateEnumString(typeof(ReserveItemEnum)), reservationMode.ToString()));
                lst.Add(new CPluginVariable("EA Rules of Conduct read and accepted",
                    typeof(enumBoolYesNo), roc_read));
                lst.Add(new CPluginVariable("Protect these players from Kick or Kill",
                    CreateEnumString(typeof(ProtectEnum)), protect_players.ToString()));
                lst.Add(new CPluginVariable("Trace rules", typeof(enumBoolYesNo), trace_rules));
                lst.Add(new CPluginVariable("Rules", typeof(string[]), unparsed_rules.ToArray()));
                lst.Add(new CPluginVariable("Rulz .txt filenames",
                    typeof(string[]), rulz_filenames.ToArray()));
                lst.Add(new CPluginVariable("Player name whitelist",
                    typeof(string[]), whitelist_players.ToArray()));
                lst.Add(new CPluginVariable("Clan name whitelist",
                    typeof(string[]), whitelist_clans.ToArray()));
                lst.Add(new CPluginVariable("Send ProconRulz Log messages to:",
                    CreateEnumString(typeof(LogFileEnum)), log_file.ToString()));
                return lst;
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return new List<CPluginVariable>();
            }
        }

        public List<PRoCon.Core.CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        #endregion

        #region Parse the rulz
        //**********************************************************************************************
        //**********************************************************************************************
        //   PARSE THE RULES THAT HAVE BEEN READ FROM THE PROCON SETTINGS (Configs\server_ip.cfg)
        //**********************************************************************************************
        //**********************************************************************************************

        // simple routine to output a console message when parse_rules chokes on a user input rule
        void parse_error(string key, string rule)
        {
            WriteConsole(
                String.Format("ProconRulz ^1SKIPPING RULE: Bad \"{0}\" clause in your rule \"{1}\" ", 
                key, rule));
            return;
        }
        
        // Here is where we try and parse the list of unparsed rules that 
        // have been read from the server_ip.cfg
        // Input is the List<string> unparsed_rules, as read from server_ip.cfg
        // Output is the List<ParsedRule> parsed_rules
        // each rule is split into parts, with each part split into fragments e.g.
        // "team attack;teamsize 8;kit recon 2;say No More Snipers!;kill"
        // the first part is "team attack"
        // the first fragment is "team"
        void parse_rules()
        {
            int rule_id = 1; // initialise first value of rule identifier

            // start with a default trigger of "On Spawn" and update as rulz with triggers are parsed
            rule_prefix = new ParsedRule();

            string rule_string_buffer = "empty"; // used to hold current rule for WriteConsole in case of exception

            try
            {
                if (!plugin_enabled) return;
                //WriteLog(String.Format("ProconRulz: Parsing your rules"));
                parsed_rules.Clear(); // start with an empty list

                ParsedRule parsed_rule = new ParsedRule(); // start with blank parsed rule
                parsed_rule.id = rule_id++;

                bool first_rule = true;

                // Concatenate user rules from settings AND user rulz files
                List<string> all_unparsed = new List<string>(unparsed_rules);
                foreach (KeyValuePair<string, string[]> rulz in filez_rulz)
                {
                    all_unparsed.AddRange(rulz.Value);
                }

                WriteConsole("ProconRulz: loading " + all_unparsed.Count.ToString() + " rulz");

                foreach (string rule_string in all_unparsed)
                {
                    rule_string_buffer = rule_string;
                    WriteDebugInfo("ProconRulz: parsing " + rule_string);

                    if ((rule_string.Length > 0 && rule_string[0] != '+') || rule_string.Length == 0) 
                    { // this is not a rule continuation, store accumulated rule
                        if (!first_rule)
                        {
                            parsed_rules.Add(parsed_rule); // add previous rule
                            WriteDebugInfo("ProconRulz: storing rule " + parsed_rule.id.ToString() +
                                            " as " + parsed_rule.unparsed_rule);
                            parsed_rule = new ParsedRule(); // start new rule
                            parsed_rule.trigger = TriggerEnum.Void; // we can check this to see if rule has trigger
                            parsed_rule.id = rule_id++;
                        }
                    }
                    if (parse_rule(ref parsed_rule, rule_string))
                    {
                        first_rule = false;
                        WriteDebugInfo("ProconRulz: parsed ok");
                        parsed_rule.unparsed_rule += rule_string;
                        // if rule did NOT have an On trigger, prepend the rule_prefix...
                        if (parsed_rule.trigger == TriggerEnum.Void)
                        {
                            parsed_rule.trigger = rule_prefix.trigger;
                            List<PartClass> l = new List<PartClass>();
                            foreach (PartClass p in rule_prefix.parts) l.Add(p);
                            foreach (PartClass p in parsed_rule.parts) l.Add(p);
                            parsed_rule.parts = l;
                        }
                        else
                        {
                            // rule DID have a trigger, so use this rule as new rule_prefix...
                            rule_prefix.trigger = parsed_rule.trigger;
                            rule_prefix.parts = new List<PartClass>(parsed_rule.parts);
                        }

                    }
                } // loop to next rule
                if (!first_rule) // flush last rule
                {
                    parsed_rules.Add(parsed_rule);
                    WriteDebugInfo("ProconRulz: storing last rule " + parsed_rule.id.ToString() +
                                    " as " + parsed_rule.unparsed_rule);
                }
                WriteConsole(string.Format("ProconRulz: {0} rules loaded", rule_id-1));
                // run 'On Init' rulz
                OnInit();
            }
            catch (Exception ex)
            {
                WriteConsole(String.Format("^1ProconRulz: Exception occurred parsing your rules"));
                WriteConsole(String.Format("^1ProconRulz: rule string was: "+rule_string_buffer));
                PrintException(ex);
            }
        }

        // parse a single rule string e.g. "On Kill;Damage SniperRifle;Kill 300"
        // return 'true' if parsed successfully
        private bool parse_rule(ref ParsedRule parsed_rule, string rule_string)
        {
            if (rule_string == null || rule_string.Length < 4)
            {
                return false;
            }
            // only parse if this rule is not a comment
            if (rule_string[0] == '#') parsed_rule.comment = true;
            else
            {
                string parse_string;
                if (rule_string[0] == '+')
                {
                    parse_string = ";" + rule_string.Substring(1);
                }
                else
                {
                    parse_string = rule_string;
                }
                string[] parts 
                    = parse_string.Replace("%3b", ";").Split(
                        new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string part in parts)
                {
                    // note we use Trim() to take off leading/trailing whitespace from part
                    if (!parse_part(ref parsed_rule, part.Trim(), false)) return false;
                } // loop to next part
            }    // looping through rule parts completed - now add to list "parsed_rules"
            return true;
        }

        // parse a 'part' of a rule, e.g. "Damage SniperRifle" or "TargetAction Kill 300"
        // returns 'true' if parse succeeded
        private bool parse_part(ref ParsedRule parsed_rule, string part, bool target_action)
        {

            try
            {
                bool rule_fail = false; // flag used to skip rest of rule on a parse error
                bool parsed_int = false; // flag used to confirm a part format with optional int was parsed ok

                // create action to be added to parsed_rule if we need it
                PartClass new_action = new PartClass();
                new_action.target_action = target_action;

                string[] fragments = quoted_split(part); // v40b update to allow quoted strings
                if (fragments == null || fragments.Length == 0) return true;
                switch (fragments[0].ToLower())
                {
                    // TRIGGER ("On Spawn" or "On Kill")
                    case "on": // trigger the rule on Spawn or a Kill
                        rule_fail = !parse_on(ref parsed_rule, fragments);
                        break;

                    // CONDITIONS: i.e. what is needed for this rule to fire
                    case "headshot": // e.g. "Headshot"
                        rule_fail = !parse_headshot(ref parsed_rule, false);
                        break;
                    case "protected": // e.g. "Protected" - this player is on reserved slots list
                        rule_fail = !parse_protected(ref parsed_rule, false);
                        break;
                    case "admin": // e.g. "Admin" - this player is an admin
                        rule_fail = !parse_admin(ref parsed_rule, false);
                        break;
                    case "admins": // e.g. "Admins" - admins are on the server
                        rule_fail = !parse_admins(ref parsed_rule, false);
                        break;
                    case "team": // e.g. "Team Attack" or defend
                        rule_fail = !parse_team(ref parsed_rule, fragments, false);
                        break;
                    case "ping": // e.g. "Ping 300"
                        rule_fail = !parse_ping(ref parsed_rule, fragments, false);
                        break;
                    case "teamsize":
                        // e.g. "Teamsize 8" this rule only applies to teams this small or smaller
                        rule_fail = !parse_teamsize(ref parsed_rule, fragments, false);
                        break;
                    case "map": // e.g. "Map Oasis" this rule only applies to map oasis
                        rule_fail = !parse_map(ref parsed_rule, part, false);
                        break;
                    case "mapmode": // e.g. "MapMode Rush" this rule only applies to maps in Rush mode
                        rule_fail = !parse_mapmode(ref parsed_rule, fragments, false);
                        break;
                    case "kit": // e.g. "Kit Recon 2" - max 2 recons on the team
                        rule_fail = !parse_kit(ref parsed_rule, fragments, false);
                        break;
                    case "weapon": // e.g. "Weapon AUG 3"
                        rule_fail = !parse_weapon(ref parsed_rule, fragments, false);
                        break;
                    case "spec": // e.g. "Spec sp_vdamage 3"
                        rule_fail = !parse_spec(ref parsed_rule, fragments, false);
                        break;
                    case "damage": // e.g. "Damage SniperRifle 8"
                        rule_fail = !parse_damage(ref parsed_rule, fragments, false);
                        break;
                    case "teamkit": // e.g. "TeamKit Recon 2" - max 2 recons on the team
                        rule_fail = !parse_teamkit(ref parsed_rule, fragments, false);
                        break;
                    case "teamweapon": // e.g. "TeamWeapon AUG 3"
                        rule_fail = !parse_teamweapon(ref parsed_rule, fragments, false);
                        break;
                    case "teamspec": // e.g. "TeamSpec sp_vdamage 3"
                        rule_fail = !parse_teamspec(ref parsed_rule, fragments, false);
                        break;
                    case "teamdamage": // e.g. "TeamDamage SniperRifle 8"
                        rule_fail = !parse_teamdamage(ref parsed_rule, fragments, false);
                        break;
                    case "range": // e.g. "Range 100"
                        rule_fail = !parse_range(ref parsed_rule, fragments, false);
                        break;
                    case "not": // e.g. "Not Damage SniperRifle"
                        rule_fail = !parse_not(ref parsed_rule, fragments, part);
                        break;
                    case "count": // e.g. "Count 8" - how many times PLAYER can trigger this rule
                    case "playercount":
                        // e.g. "PlayerCount 8" - how many times PLAYER can trigger this rule
                        rule_fail = !parse_count(ref parsed_rule, fragments, false);
                        break;
                    case "teamcount": // e.g. "TeamCount 8" - how many times TEAM can trigger this rule
                        rule_fail = !parse_teamcount(ref parsed_rule, fragments, false);
                        break;
                    case "servercount":
                        // e.g. "ServerCount 8" - how many times SERVER can trigger this rule
                        rule_fail = !parse_servercount(ref parsed_rule, fragments, false);
                        break;
                    case "playerfirst":
                    case "teamfirst":
                    case "serverfirst":
                    case "playeronce":
                        rule_fail = !parse_first(ref parsed_rule, fragments[0].ToLower(), false);
                        break;
                    case "rate": // e.g. "Rate 5 20" this rule triggered 5 times in 20 seconds
                        rule_fail = !parse_rate(ref parsed_rule, fragments, false);
                        break;
                    case "text": // e.g. "On Say;Text ofc;Yell OFc 4 Ever"
                        rule_fail = !parse_text(ref parsed_rule, part, false);
                        break;
                    case "targetplayer": // e.g. "TargetPlayer" (extract playername from say text)
                        rule_fail = !parse_targetplayer(ref parsed_rule, part, false);
                        break;
                    case "incr": // e.g. "Incr kill_count"
                        rule_fail = !parse_incr(ref parsed_rule, fragments);
                        break;
                    case "decr": // e.g. "Decr kill_count"
                        rule_fail = !parse_decr(ref parsed_rule, fragments);
                        break;
                    case "set": // e.g. "Set kill_count 0"
                        rule_fail = !parse_set(ref parsed_rule, fragments);
                        break;
                    case "if": // e.g. "If kill_count > 7"
                        rule_fail = !parse_test(ref parsed_rule, part, false);
                        break;
                    case "maplist": // e.g. "MapList 0 off"
                        rule_fail = !parse_maplist(ref parsed_rule, fragments);
                        break;

                    // ACTIONS i.e. what to do when conditions are true
                    // ************************************************
                    case "say": // e.g. "Say No more Snipers!"
                        if (part.Length < 5)
                        {
                            parse_error("Say", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.Say;
                        new_action.string_list.Add(part.Substring(4));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "playersay": // e.g. "PlayerSay No more Snipers!"
                        if (part.Length < 11)
                        {
                            parse_error("PlayerSay", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.PlayerSay;
                        new_action.string_list.Add(part.Substring(10));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "squadsay": // e.g. "SquadSay No more Snipers!"
                        if (part.Length < 10)
                        {
                            parse_error("SquadSay", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.SquadSay;
                        new_action.string_list.Add(part.Substring(9));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "teamsay": // e.g. "TeamSay No more Snipers!"
                        if (part.Length < 9)
                        {
                            parse_error("TeamSay", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.TeamSay;
                        new_action.string_list.Add(part.Substring(8));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "victimsay": // e.g. "VictimSay You were killed by %p%, range %r%!"
                        if (part.Length < 11)
                        {
                            parse_error("VictimSay", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.VictimSay;
                        new_action.string_list.Add(part.Substring(10));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "adminsay": // e.g. "AdminSay HACKER WARNING on %p% (5 headshots in 15 seconds)!!"
                        if (part.Length < 10)
                        {
                            parse_error("AdminSay", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.AdminSay;
                        new_action.string_list.Add(part.Substring(9));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "yell":
                        if (part.Length < 6)
                        {
                            parse_error("Yell", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.Yell;
                        parsed_int = false; // assume parsing yell_delay fails
                        try
                        {
                            new_action.int1 = Int32.Parse(fragments[1]); // try and pick up yell delay (seconds)
                            new_action.string_list.Add(String.Join(" ", fragments, 2, fragments.Length-2 )); // rest of string
                            parsed_int = true;
                        }
                        catch { }
                        if (!parsed_int) // we didn't parse a yell delay so use default
                        {
                            new_action.int1 = yell_delay;
                            new_action.string_list.Add(part.Substring(5));
                        }
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "playeryell": // e.g. "PlayerYell No more Snipers!"
                        if (part.Length < 12)
                        {
                            parse_error("PlayerYell", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.PlayerYell;
                        parsed_int = false; // assume parsing yell_delay fails
                        try
                        {
                            new_action.int1 = Int32.Parse(fragments[1]); // try and pick up yell delay (seconds)
                            new_action.string_list.Add(String.Join(" ", fragments, 2, fragments.Length-2 )); // rest of string
                            parsed_int = true;
                        }
                        catch { }
                        if (!parsed_int) // we didn't parse a yell delay so use default
                        {
                            new_action.int1 = yell_delay;
                            new_action.string_list.Add(part.Substring(11));
                        }
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "squadyell": // e.g. "SquadYell No more Snipers!"
                        if (part.Length < 11)
                        {
                            parse_error("SquadYell", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.SquadYell;
                        parsed_int = false; // assume parsing yell_delay fails
                        try
                        {
                            new_action.int1 = Int32.Parse(fragments[1]); // try and pick up yell delay (seconds)
                            new_action.string_list.Add(String.Join(" ", fragments, 2, fragments.Length-2 )); // rest of string
                            parsed_int = true;
                        }
                        catch { }
                        if (!parsed_int) // we didn't parse a yell delay so use default
                        {
                            new_action.int1 = yell_delay;
                            new_action.string_list.Add(part.Substring(10));
                        }
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "teamyell": // e.g. "TeamYell No more Snipers!"
                        if (part.Length < 10)
                        {
                            parse_error("TeamYell", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.TeamYell;
                        parsed_int = false; // assume parsing yell_delay fails
                        try
                        {
                            new_action.int1 = Int32.Parse(fragments[1]); // try and pick up yell delay (seconds)
                            new_action.string_list.Add(String.Join(" ", fragments, 2, fragments.Length-2 )); // rest of string
                            parsed_int = true;
                        }
                        catch { }
                        if (!parsed_int) // we didn't parse a yell delay so use default
                        {
                            new_action.int1 = yell_delay;
                            new_action.string_list.Add(part.Substring(9));
                        }
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "log":
                        if (part.Length < 5)
                        {
                            parse_error("Log", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.Log;
                        new_action.string_list.Add(part.Substring(4));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "all":  // say and yell and log
                        if (part.Length < 5)
                        {
                            parse_error("All", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.All;
                        new_action.string_list.Add(part.Substring(4));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "both": // say and yell
                        if (part.Length < 6)
                        {
                            parse_error("Both", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.Both;
                        new_action.string_list.Add(part.Substring(5));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "playerboth": // say and yell to a player
                        if (part.Length < 12)
                        {
                            parse_error("PlayerBoth", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.PlayerBoth;
                        new_action.string_list.Add(part.Substring(11));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "kill": // kill player that triggered this rule
                        new_action.part_type = PartEnum.Kill;
                        if (fragments.Length == 2) new_action.string_list.Add(fragments[1]);
                        else new_action.string_list.Add(kill_delay.ToString());
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "kick": // kick player that triggered this rule
                        new_action.part_type = PartEnum.Kick;
                        if (fragments.Length >= 2) new_action.string_list.Add(part.Substring(5));
                        else new_action.string_list.Add("Kicked automatically");
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "ban": // ban player that triggered this rule
                        new_action.part_type = PartEnum.Ban;
                        if (fragments.Length >= 2) new_action.string_list.Add(part.Substring(4));
                        else new_action.string_list.Add("[%p%] Banned automatically");
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "tempban": // temp ban player that triggered this rule
                        new_action.part_type = PartEnum.TempBan;
                        // try and pick up number of seconds for ban from fragments[1]
                        bool tempban_default = true;
                        if (fragments.Length >= 2)
                            try
                            {
                                new_action.int1 = Int32.Parse(fragments[1]);
                                tempban_default = false;
                            }
                            catch
                            { new_action.int1 = ban_delay; }
                        else new_action.int1 = ban_delay;

                        // now try and dig out message
                        // "TempBan" or "TempBan 7777"
                        if (fragments.Length == 1 || (!tempban_default && fragments.Length == 2))
                        {
                            new_action.string_list.Add("[%p%] automatic temp ban");
                        }
                        // TempBan <message>
                        else if (tempban_default && fragments.Length >= 2)
                        {
                            new_action.string_list.Add(part.Substring(8));
                        }
                        // TempBan <N> <message>
                        else if (!tempban_default && fragments.Length > 2)
                        {
                            new_action.string_list.Add(part.Substring(8 + fragments[1].Length));
                        }
                        else
                        {
                            parse_error("TempBan", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "pbban": // ban player that triggered this rule via PunkBuster
                        new_action.part_type = PartEnum.PBBan;
                        if (fragments.Length >= 2) new_action.string_list.Add(part.Substring(6));
                        else new_action.string_list.Add("[%p%] Banned automatically");
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "pbkick": // temp ban player that triggered this rule (i.e. kick with timeout via PunkBuster)
                        new_action.part_type = PartEnum.PBKick;
                        // try and pick up number of MINUTES for ban from fragments[1]
                        bool pbkick_default = true;
                        if (fragments.Length >= 2)
                            try
                            {
                                new_action.int1 = Int32.Parse(fragments[1]);
                                pbkick_default = false;
                            }
                            catch
                            { new_action.int1 = 0; } // default is NO temp ban on PB kick
                        else new_action.int1 = 0;

                        // now try and dig out message
                        // "PBKick" or "PBKick 77"
                        if (fragments.Length == 1 || (!pbkick_default && fragments.Length == 2))
                        {
                            new_action.string_list.Add("[%p%] automatic temp kick/ban");
                        }
                        // PBKick <message>
                        else if (pbkick_default && fragments.Length >= 2)
                        {
                            new_action.string_list.Add(part.Substring(7));
                        }
                        // PBKick <N> <message>
                        else if (!pbkick_default && fragments.Length > 2)
                        {
                            new_action.string_list.Add(part.Substring(7 + fragments[1].Length));
                        }
                        else
                        {
                            parse_error("PBKick", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "playerblock": // block player name from using item
                        new_action.part_type = PartEnum.PlayerBlock;
                        if (fragments.Length == 2) new_action.string_list.Add(fragments[1]); // item key
                        else new_action.string_list.Add("unknown");
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "targetconfirm": // trigger previous TargetAction actions from this player
                        if (fragments.Length != 1) parse_error("TargetConfirm", parsed_rule.unparsed_rule);
                        new_action.part_type = PartEnum.TargetConfirm;
                        new_action.string_list.Add("");
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "targetcancel": // cancel previous TargetAction actions from this player
                        if (fragments.Length != 1) parse_error("TargetCancel", parsed_rule.unparsed_rule);
                        new_action.part_type = PartEnum.TargetCancel;
                        new_action.string_list.Add("");
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "targetaction": // store delayed actions on target from this player
                        rule_fail = !parse_part(ref parsed_rule, part.Substring(13), true);
                        break;
                    case "exec": // e.g. "Exec levelVars.set level levels/mp_007gr vehiclesDisabled false"
                        if (part.Length < 6)
                        {
                            parse_error("Execute", parsed_rule.unparsed_rule); rule_fail = true; break;
                        }
                        new_action.part_type = PartEnum.Execute;
                        new_action.string_list.Add(part.Substring(5));
                        new_action.has_count = has_a_count(new_action);
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "continue":
                        new_action.part_type = PartEnum.Continue;
                        new_action.string_list.Add("");
                        parsed_rule.parts.Add(new_action);
                        break;
                    case "end":
                        new_action.part_type = PartEnum.End;
                        new_action.string_list.Add("");
                        parsed_rule.parts.Add(new_action);
                        break;
                    default:
                        WriteConsole(String.Format("^1ProconRulz: Unrecognised rule {0}",
                            parsed_rule.unparsed_rule));
                        rule_fail = true;
                        break;
                }
                return !rule_fail;
            }
            catch (Exception ex)
            {
                WriteConsole(String.Format("^1ProconRulz: Exception occurred parsing your rules"));
                WriteConsole(String.Format("^1ProconRulz: Rule was \"{0}\"", parsed_rule.unparsed_rule));
                WriteConsole(String.Format("^1ProconRulz: Part that failed was \"{0}\"", part));
                PrintException(ex);
                return false;
            }

        }

        private bool parse_on(ref ParsedRule parsed_rule, string[] fragments)
        {
            //WriteDebugInfo(String.Format("ProconRulz: Parsing On"));
            if (fragments.Length!=2) 
            { 
                parse_error("On", parsed_rule.unparsed_rule); 
                return false;
            }
            if (fragments[1].ToLower().StartsWith("roundover"))
            {
                parsed_rule.trigger = TriggerEnum.RoundOver;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("round"))
            {
                parsed_rule.trigger = TriggerEnum.Round;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("join"))
            {
                parsed_rule.trigger = TriggerEnum.Join;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("leave"))
            {
                parsed_rule.trigger = TriggerEnum.Leave;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("spawn"))
            {
                parsed_rule.trigger = TriggerEnum.Spawn;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("kill"))
            {
                parsed_rule.trigger = TriggerEnum.Kill;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("teamkill"))
            {
                parsed_rule.trigger = TriggerEnum.TeamKill;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("suicide"))
            {
                parsed_rule.trigger = TriggerEnum.Suicide;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("playerblock"))
            {
                parsed_rule.trigger = TriggerEnum.PlayerBlock;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("say"))
            {
                parsed_rule.trigger = TriggerEnum.Say;
                return true;
            }
            if (fragments[1].ToLower().StartsWith("init"))
            {
                parsed_rule.trigger = TriggerEnum.Init;
                return true;
            }
            parse_error("On", parsed_rule.unparsed_rule); 
            return false;
        }

        private bool parse_headshot(ref ParsedRule parsed_rule, bool negated)
        {
            PartClass c = new PartClass();
            c.part_type = PartEnum.Headshot;
            c.negated = negated;
            parsed_rule.parts.Add(c);
            return true;
        }

        // this player is on reserved slots list
        private bool parse_protected(ref ParsedRule parsed_rule, bool negated)
        {
            PartClass c = new PartClass();
            c.part_type = PartEnum.Protected;
            c.negated = negated;
            parsed_rule.parts.Add(c);
            return true;
        }

        // this player is an admin
        private bool parse_admin(ref ParsedRule parsed_rule, bool negated)
        {
            PartClass c = new PartClass();
            c.part_type = PartEnum.Admin;
            c.negated = negated;
            parsed_rule.parts.Add(c);
            return true;
        }

        // admins are currently on the server
        private bool parse_admins(ref ParsedRule parsed_rule, bool negated)
        {
            PartClass c = new PartClass();
            c.part_type = PartEnum.Admins;
            c.negated = negated;
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_team(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("Team", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Team;
            c.negated = negated;
            c.string_list = item_keys(fragments[negated ? 2 : 1].ToLower());
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_ping(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("Ping", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Ping;
            c.negated = negated;
            try
            {
                c.int1 = Int32.Parse(fragments[negated ? 2 : 1]);
            }
            catch
            {
                parse_error("Ping", parsed_rule.unparsed_rule);
                return false;
            }
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_teamsize(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("Teamsize", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Teamsize;
            c.negated = negated;
            try
            {
                c.int1 = Int32.Parse(fragments[negated ? 2 : 1]);
            }
            catch
            {
                parse_error("Teamsize", parsed_rule.unparsed_rule);
                return false;
            }
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_map(ref ParsedRule parsed_rule, string part, bool negated)
        {
            if (part.Length < 5) { parse_error("Map", parsed_rule.unparsed_rule); return false; }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Map;
            c.negated = negated;
            c.string_list = negated ? item_keys(part.Substring(8).ToLower()) : item_keys(part.Substring(4).ToLower());
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_mapmode(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length != (negated ? 3 : 2))
            {
                parse_error("MapMode", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.MapMode;
            c.negated = negated;
            c.string_list = item_keys(fragments[negated ? 2 : 1]);
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_kit(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("Kit", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Kit;
            c.negated = negated;
            if (fragments.Length == (negated ? 4 : 3))
                try
                {
                    c.int1 = Int32.Parse(fragments[negated ? 3 : 2]);
                }
                catch
                {
                    parse_error("Kit", parsed_rule.unparsed_rule);
                    return false;
                }
            try
            {
                c.string_list = item_keys(fragments[negated ? 2 : 1]);
                foreach (string kit_key in c.string_list)
                {
                    try
                    {
                        Kits k = (Kits)Enum.Parse(typeof(Kits), kit_key, true);
                    }
                    catch (ArgumentException)
                    {
                        WriteConsole(String.Format("ProconRulz: ^1Warning, kit {0} not found in Procon (but you can still use the key in ProconRulz)", kit_key));
                    }
                }
                parsed_rule.parts.Add(c);
                spawn_counts.watch(c.string_list);
            }
            catch { parse_error("Kit", parsed_rule.unparsed_rule); return false; }
            return true;
        }

        // updated to allow '&' char in weapon key instead of a space
        // this is complicated by the fact that weapon keys can 
        // INCLUDE SPACES (thankyou EA) eg "M1A1 Thompson" and heli weapons
        // Weapon AUG
        // Not Weapon AUG
        // Weapon AUG 3
        // Not Weapon AUG 3
        // Weapon M1A1 Thompson
        // Weapon M1A1 Thompson 3
        // Not Weapon M1A1 Thompson
        // Not Weapon M1A1 Thompson 3
        // multiple weapon keys can be separated with rulz_item_separator E.g. Weapon SMAW,RPG-7
        private bool parse_weapon(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            //WriteConsole(String.Format("rule {0}", parsed_rule.unparsed_rule));
            //WriteConsole(String.Format("length {0}, negated {1}", fragments.Length, negated));
            //WriteConsole(String.Format("fragments {0}", String.Join("---",fragments)));
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("Weapon", parsed_rule.unparsed_rule);
                return false;
            }
            // ok lets try and figure out what we've got in the rule
            List<string> weapon_keys = item_keys((negated ? fragments[2] : fragments[1]));
            // try and make a count out of the last fragment
            int weapon_count = -1;
            try
            {
                weapon_count = Int32.Parse(fragments[fragments.Length - 1]);
            }
            catch
            {
            }
            //WriteConsole(String.Format("weapon_count {0}", weapon_count));

            PartClass c = new PartClass();
            c.part_type = PartEnum.Weapon;
            c.negated = negated;
            if (weapon_count > -1) c.int1 = weapon_count;
            foreach (string weapon_key in weapon_keys)
            {
                if (!weaponDefines.Contains(weapon_key))
                    WriteConsole(String.Format("ProconRulz: ^1Warning, weapon {0} not found in Procon (but you can still use the key in ProconRulz)", weapon_key));
            }
            c.string_list = weapon_keys;
            parsed_rule.parts.Add(c);
            spawn_counts.watch(c.string_list);
            //else { parse_error("Weapon", parsed_rule.unparsed_rule); return false; }
            return true;
        }

        private bool parse_spec(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("Spec", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Spec;
            c.negated = negated;
            if (fragments.Length == (negated ? 4 : 3))
                try
                {
                    c.int1 = Int32.Parse(fragments[negated ? 3 : 2]);
                }
                catch
                {
                    parse_error("Spec", parsed_rule.unparsed_rule);
                    return false;
                }
            List<string> spec_keys = item_keys(fragments[negated ? 2 : 1]);
            foreach (string spec_key in spec_keys)
            {
                if (!specDefines.Contains(spec_key))
                    WriteConsole(String.Format("ProconRulz: ^1Warning, Specialization {0} not found in Procon (but you can still use the key in ProconRulz)", spec_key));
            }
            c.string_list = spec_keys;
            parsed_rule.parts.Add(c);
            spawn_counts.watch(c.string_list);
            return true;
        }

        private bool parse_damage(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("Damage", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Damage;
            c.negated = negated;
            if (fragments.Length == (negated ? 4 : 3))
                try
                {
                    c.int1 = Int32.Parse(fragments[negated ? 3 : 2]);
                }
                catch
                {
                    parse_error("Damage", parsed_rule.unparsed_rule);
                    return false;
                }
            try
            {
                c.string_list = item_keys(fragments[negated ? 2 : 1]);
                foreach (string damage_key in c.string_list)
                {
                    try
                    {
                        DamageTypes d = (DamageTypes)Enum.Parse(typeof(DamageTypes), damage_key, true);
                    }
                    catch (ArgumentException)
                    {
                        WriteConsole(String.Format("ProconRulz: ^1Warning, damage {0} not found in Procon (but you can still use the key in ProconRulz)", damage_key));
                    }
                }
                parsed_rule.parts.Add(c);
                spawn_counts.watch(c.string_list);
            }
            catch { parse_error("Damage", parsed_rule.unparsed_rule); return false; }
            return true;
        }

        // count of spawned Kits on team
        private bool parse_teamkit(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length != (negated ? 4 : 3))
            {
                parse_error("TeamKit", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.TeamKit;
            c.negated = negated;
            try
            {
                c.int1 = Int32.Parse(fragments[negated ? 3 : 2]);

                c.string_list = item_keys(fragments[negated ? 2 : 1]);
                foreach (string kit_key in c.string_list)
                {
                    try
                    {
                        Kits k = (Kits)Enum.Parse(typeof(Kits), kit_key, true);
                    }
                    catch (ArgumentException)
                    {
                        WriteConsole(String.Format("ProconRulz: ^1Warning, kit {0} not found in Procon (but you can still use the key in ProconRulz)", kit_key));
                    }
                }
                parsed_rule.parts.Add(c);
                spawn_counts.watch(c.string_list);
            }
            catch { parse_error("TeamKit", parsed_rule.unparsed_rule); return false; }
            return true;
        }

        // see parse_weapon for more comments (multi-word weapon keys)
        private bool parse_teamweapon(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            //WriteConsole(String.Format("rule {0}", parsed_rule.unparsed_rule));
            //WriteConsole(String.Format("length {0}, negated {1}", fragments.Length, negated));
            //WriteConsole(String.Format("fragments {0}", String.Join("---",fragments)));
            if (fragments.Length < (negated ? 3 : 2))
            {
                parse_error("TeamWeapon", parsed_rule.unparsed_rule);
                return false;
            }
            // ok lets try and figure out what we've got in the rule
            List<string> weapon_keys = item_keys(negated ? fragments[2] : fragments[1]);
            // try and make a count out of the last fragment
            int weapon_count = -1;
            try
            {
                weapon_count = Int32.Parse(fragments[fragments.Length - 1]);
            }
            catch
            {
            }
            //WriteConsole(String.Format("weapon_count {0}", weapon_count));
            //WriteConsole(String.Format("weapon_key {0}", weapon_key));

            PartClass c = new PartClass();
            c.part_type = PartEnum.TeamWeapon;
            c.negated = negated;
            if (weapon_count > -1) c.int1 = weapon_count;

            foreach (string weapon_key in weapon_keys)
            {
                if (!weaponDefines.Contains(weapon_key))
                    WriteConsole(String.Format("ProconRulz: ^1Warning, weapon {0} not found in Procon (but you can still use the key in ProconRulz)", weapon_key));
            }
            c.string_list = weapon_keys;
            parsed_rule.parts.Add(c);
            spawn_counts.watch(c.string_list);

            return true;
        }

        // count of spawned specializations on team
        private bool parse_teamspec(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length != (negated ? 4 : 3))
            {
                parse_error("TeamSpec", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.TeamSpec;
            c.negated = negated;
            c.int1 = Int32.Parse(fragments[negated ? 3 : 2]);

            List<string> spec_keys = item_keys(fragments[negated ? 2 : 1]);
            foreach (string spec_key in spec_keys)
            {
                if (!specDefines.Contains(spec_key))
                    WriteConsole(String.Format("ProconRulz: ^1Warning, Specialization {0} not found in Procon (but you can still use the key in ProconRulz)", spec_key));
            }
            c.string_list = spec_keys;
            parsed_rule.parts.Add(c);
            spawn_counts.watch(c.string_list);
            return true;
        }

        private bool parse_teamdamage(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length != (negated ? 4 : 3))
            {
                parse_error("TeamDamage", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Damage;
            c.negated = negated;
            try
            {
                c.int1 = Int32.Parse(fragments[negated ? 3 : 2]);
                c.string_list = item_keys(fragments[negated ? 2 : 1]);
                foreach (string damage_key in c.string_list)
                {
                    try
                    {
                        DamageTypes d = (DamageTypes)Enum.Parse(typeof(DamageTypes), damage_key, true);
                    }
                    catch (ArgumentException)
                    {
                        WriteConsole(String.Format("ProconRulz: ^1Warning, damage {0} not found in Procon (but you can still use the key in ProconRulz)", damage_key));
                    }
                }
                parsed_rule.parts.Add(c);
                spawn_counts.watch(c.string_list);
            }
            catch { parse_error("TeamDamage", parsed_rule.unparsed_rule); return false; }
            return true;
        }

        // range of the kill
        private bool parse_range(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length == (negated ? 3 : 2))
            {
                PartClass c = new PartClass();
                c.part_type = PartEnum.Range;
                c.negated = negated;
                c.int1 = Int32.Parse(fragments[negated ? 2 : 1]);
                parsed_rule.parts.Add(c);
                return true;
            }
            parse_error("Range", parsed_rule.unparsed_rule);
            return false;
        }

        // "Not <condition>" e.g. "Not Team Attack"
        // this implementation is inelegant - I should be recursively calling parse_part(...)
        private bool parse_not(ref ParsedRule parsed_rule, string[] fragments, string part)
        {
            if (fragments.Length < 2)
            {
                parse_error("Not", parsed_rule.unparsed_rule);
                return false;
            }
            switch (fragments[1].ToLower())
            {
                case "if":
                    return parse_test(ref parsed_rule, part, true);

                case "text":
                    return parse_text(ref parsed_rule, part, true);

                case "headshot":
                    return parse_headshot(ref parsed_rule, true);

                case "protected":
                    return parse_protected(ref parsed_rule, true);

                case "targetplayer":
                    return parse_targetplayer(ref parsed_rule, part, true);

                case "admin":
                    return parse_admin(ref parsed_rule, true);

                case "admins":
                    return parse_admins(ref parsed_rule, true);

                case "ping":
                    return parse_ping(ref parsed_rule, fragments, true);

                case "team":
                    return parse_team(ref parsed_rule, fragments, true);

                case "teamsize":
                    return parse_teamsize(ref parsed_rule, fragments, true);

                case "map":
                    return parse_map(ref parsed_rule, part, true);

                case "mapmode":
                    return parse_mapmode(ref parsed_rule, fragments, true);

                case "kit":
                    return parse_kit(ref parsed_rule, fragments, true);

                case "spec":
                    return parse_spec(ref parsed_rule, fragments, true);

                case "weapon":
                    return parse_weapon(ref parsed_rule, fragments, true);

                case "damage":
                    return parse_damage(ref parsed_rule, fragments, true);

                case "teamkit":
                    return parse_teamkit(ref parsed_rule, fragments, true);

                case "teamspec":
                    return parse_teamspec(ref parsed_rule, fragments, true);

                case "teamweapon":
                    return parse_teamweapon(ref parsed_rule, fragments, true);

                case "teamdamage":
                    return parse_teamdamage(ref parsed_rule, fragments, true);

                case "range":
                    return parse_range(ref parsed_rule, fragments, true);

                case "count":
                case "playercount":
                    return parse_count(ref parsed_rule, fragments, true);

                case "teamcount":
                    return parse_teamcount(ref parsed_rule, fragments, true);

                case "servercount":
                    return parse_servercount(ref parsed_rule, fragments, true);

                case "playerfirst":
                case "teamfirst":
                case "serverfirst":
                case "playeronce":
                    return parse_first(ref parsed_rule, fragments[1].ToLower(), true);

                case "rate":
                    return parse_rate(ref parsed_rule, fragments, true);

                default:
                    parse_error("Not", parsed_rule.unparsed_rule);
                    break;
            }
            return false;
        }

        private bool parse_count(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length == (negated ? 3 : 2))
            {
                PartClass p = new PartClass();
                p.part_type = PartEnum.Count;
                p.negated = negated;
                p.int1 = Int32.Parse(fragments[negated ? 2 : 1]);
                p.has_count = true;
                parsed_rule.parts.Add(p);
                return true;
            }
            parse_error("Count", parsed_rule.unparsed_rule);
            return false;
        }

        private bool parse_teamcount(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length == (negated ? 3 : 2))
            {
                PartClass p = new PartClass();
                p.part_type = PartEnum.TeamCount;
                p.negated = negated;
                p.int1 = Int32.Parse(fragments[negated ? 2 : 1]);
                p.has_count = true;
                parsed_rule.parts.Add(p);
                return true;
            }
            parse_error("TeamCount", parsed_rule.unparsed_rule);
            return false;
        }

        private bool parse_servercount(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            if (fragments.Length == (negated ? 3 : 2))
            {
                PartClass p = new PartClass();
                p.part_type = PartEnum.ServerCount;
                p.negated = negated;
                p.int1 = Int32.Parse(fragments[negated ? 2 : 1]);
                p.has_count = true;
                parsed_rule.parts.Add(p);
                return true;
            }
            parse_error("ServerCount", parsed_rule.unparsed_rule);
            return false;
        }

        private bool parse_rate(ref ParsedRule parsed_rule, string[] fragments, bool negated)
        {
            PartClass p = new PartClass();
            if (fragments.Length != (negated ? 4 : 3))
            {
                parse_error("Rate", parsed_rule.unparsed_rule);
                return false;
            }
            try
            {
                p.part_type = PartEnum.Rate;
                p.negated = negated;
                p.int1 = Int32.Parse(fragments[negated ? 2 : 1]);
                p.int2 = Int32.Parse(fragments[negated ? 3 : 2]);
            }
            catch { parse_error("Rate", parsed_rule.unparsed_rule); return false; }
            parsed_rule.parts.Add(p);
            return true;
        }

        // Here we translate some convenience conditions into actual equivalents:
        // PlayerFirst -> Not PlayerCount 1 (i.e. player count is not > 1, i.e. count MUST be 1)
        // TeamFirst -> Not TeamCount 1
        // ServerFirst -> Not ServerCount 1
        // PlayerOnce -> Not Rate 2 100000 (tricky to explain... rule has NOT fired twice in 100000 seconds,
        //                                  which is only true the FIRST time the rule fires for this player
        //                                  because the time period is long enough that the second time it fires
        //                                  will be inside the 100000 second window so the condition will fail
        //                                  the second (and later) time.
        //                                  This rule takes advantage of the fact that RATES continue across
        //                                  new round starts. Rates reset for a player on a new round when they are
        //                                  NOT online. Told you it was tricky).
        private bool parse_first(ref ParsedRule parsed_rule, string condition, bool negated)
        {
            PartClass p = new PartClass();
            p.int1 = 1; // 1 for 'first's, will change to 2 for PlayerOnce...
            p.negated = !negated; // i.e. "Not PlayerFirst" -> "PlayerCount 1", i.e. the Not inverts
            switch (condition)
            {
                case "playerfirst":
                    p.part_type = PartEnum.PlayerCount;
                    break;
                case "teamfirst":
                    p.part_type = PartEnum.TeamCount;
                    break;
                case "serverfirst":
                    p.part_type = PartEnum.ServerCount;
                    break;
                case "playeronce":
                    p.part_type = PartEnum.Rate;
                    p.int1 = 2; // is 1 for the above conditions, change to 2 here
                    p.int2 = 100000; // period for Rate set to 100000 seconds i.e. about a day
                    break;
            }
            p.has_count = true;
            parsed_rule.parts.Add(p);
            return true;

        }

        private bool parse_text(ref ParsedRule parsed_rule, string part, bool negated)
        {
            if (part.Length < 6) { parse_error("Text", parsed_rule.unparsed_rule); return false; }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Text;
            c.negated = negated;
            c.string_list = new List<string>(part.Substring(negated ? 9 : 5).ToLower().Split(new char[] { rulz_item_separator }, StringSplitOptions.RemoveEmptyEntries));
            parsed_rule.parts.Add(c);
            return true;
        }

        // a player name can be extracted from the say text (into %t%)
        private bool parse_targetplayer(ref ParsedRule parsed_rule, string part, bool negated)
        {
            PartClass c = new PartClass();
            c.part_type = PartEnum.TargetPlayer;
            c.negated = negated;
            if (part.Length > 12) c.string_list.Add(part.Substring(13));
            parsed_rule.parts.Add(c);
            return true;
        }

        // RULZ VARIABLE INCR, DECR, SET and TEST conditions
        private bool parse_incr(ref ParsedRule parsed_rule, string[] fragments)
        {
            if (fragments.Length != 2) { parse_error("Incr", parsed_rule.unparsed_rule); return false; }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Incr;
            c.negated = false;
            c.string_list.Add(fragments[1].ToLower());
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_decr(ref ParsedRule parsed_rule, string[] fragments)
        {
            if (fragments.Length != 2) { parse_error("Decr", parsed_rule.unparsed_rule); return false; }
            PartClass c = new PartClass();
            c.part_type = PartEnum.Decr;
            c.negated = false;
            c.string_list.Add(fragments[1].ToLower());
            parsed_rule.parts.Add(c);
            return true;
        }

        private bool parse_set(ref ParsedRule parsed_rule, string[] fragments)
        {
            if (fragments.Length < 3)
            {
                parse_error("Set", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass p = new PartClass();
            p.part_type = PartEnum.Set;
            p.negated = false;
            p.string_list.Add(fragments[1]); // string_list[0] = var_name
            string value = "";
            for (int i = 2; i < fragments.Length; i++) value += fragments[i];
            p.string_list.Add(value); //string_list[1] = value to assign
            p.has_count = has_a_count(p);
            parsed_rule.parts.Add(p);
            return true;
        }

        private bool parse_test(ref ParsedRule parsed_rule, string part, bool negated)
        {
            string[] fragments_in = quoted_split(part);
            string[] fragments;
            // if "Not If" then shift fragments left
            List<string> fragment_list = new List<string>();
            if (negated)
            {
                for (int i = 1; i < fragments_in.Length; i++)
                {
                    fragment_list.Add(fragments_in[i]);
                }
                fragments = fragment_list.ToArray();
            }
            else
                fragments = fragments_in;

            // OK we got to here with the fragments being "If", val1, condition, val2
            if (fragments.Length < 4)
            {
                parse_error("Bad If condition", parsed_rule.unparsed_rule);
                return false;
            }
            int condition_index = 0;
            for (int i = 1; i < fragments.Length; i++)
            {
                if (fragments[i] == "=" ||
                    fragments[i] == "!=" ||
                    fragments[i] == "==" ||
                    fragments[i] == "<>" ||
                    fragments[i] == ">" ||
                    fragments[i] == "<" ||
                    fragments[i] == ">=" ||
                    fragments[i] == "=>" ||
                    fragments[i] == "<=" ||
                    fragments[i] == "=<" ||
                    fragments[i].ToLower() == "contains" ||
                    fragments[i].ToLower() == "word"
                   )
                {
                    condition_index = i;
                    break;
                }
            }
            if (condition_index == 0)
            {
                parse_error("If condition has bad comparison operator", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass p = new PartClass();
            p.part_type = PartEnum.Test;
            p.negated = negated;
            string condition_part = "";
            for (int i = 1; i < condition_index; i++)
            {
                condition_part += fragments[i];
            }
            p.string_list.Add(condition_part);
            p.string_list.Add(fragments[condition_index]);
            condition_part = "";
            for (int i = condition_index + 1; i < fragments.Length; i++)
            {
                condition_part += fragments[i];
            }
            p.string_list.Add(condition_part);

            p.has_count = has_a_count(p);
            parsed_rule.parts.Add(p);
            return true;
        }

        private bool parse_maplist(ref ParsedRule parsed_rule, string[] fragments)
        {
            if (fragments.Length < 3)
            {
                parse_error("MapList", parsed_rule.unparsed_rule);
                return false;
            }
            PartClass p = new PartClass();
            p.part_type = PartEnum.MapList;
            p.negated = false;
            p.string_list.Add(fragments[1]); // string_list[0] = var_name
            string value = "";
            for (int i = 2; i < fragments.Length; i++) value += fragments[i];
            p.string_list.Add(value); //string_list[1] = value to assign
            p.has_count = has_a_count(p);
            parsed_rule.parts.Add(p);
            return true;
        }


#endregion

        #region Callbacks for misc. server EVENTS (player joins/moves, list players, round end etc)

        //********************************************************************************************
        //********************************************************************************************
        //************************* various other events from procon handled here
        //********************************************************************************************
        //********************************************************************************************
        public override void OnPlayerTeamChange(string player_name, int iTeamID, int iSquadID)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                players.team_move(player_name, iTeamID.ToString(), iSquadID.ToString());
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnPlayerTeamChange");
                PrintException(ex);
            }
        }

        public override void OnPlayerSquadChange(string player_name, int iTeamID, int iSquadID)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                players.team_move(player_name, iTeamID.ToString(), iSquadID.ToString());
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnPlayerSquadChange");
                PrintException(ex);
            }
        }

        public override void OnPlayerJoin(string player_name)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            WriteDebugInfo("ProconRulz: ********************OnPlayerJoin******************************" +
                player_name);
            if (trace_rules == enumBoolYesNo.Yes) prdebug("counts");
            players.new_player(player_name);

            // update the admin list with this player name if necessary
            admins_add(player_name);
            // scan for any "On Join" rulz
            scan_rules(TriggerEnum.Join, player_name, 
                new Dictionary<SubstEnum, string>(), null, null, null);
            if (trace_rules == enumBoolYesNo.Yes) prdebug("counts");
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnPlayerJoin");
                PrintException(ex);
            }
        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            WriteDebugInfo("ProconRulz: ********************OnPlayerLeft******************************" +
                cpiPlayer.SoldierName);

            scan_rules(TriggerEnum.Leave, cpiPlayer.SoldierName, 
                new Dictionary<SubstEnum, string>(), null, null, null);
            if (trace_rules == enumBoolYesNo.Yes) prdebug("counts");

            //Removes left player from all lists
            players.remove(cpiPlayer.SoldierName);

            spawn_counts.zero_player(cpiPlayer.SoldierName);
            admins_remove(cpiPlayer.SoldierName);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnPlayerLeft");
                PrintException(ex);
            }
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: ********************OnPunkbusterPlayerInfo******************************" +
                    cpbiPlayer.SoldierName);

                players.update(cpbiPlayer); // add pb_guid and ip
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnPunkbusterPlayerInfo");
                PrintException(ex);
            }
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            WriteDebugInfo("ProconRulz: ********************OnListPlayers******************************");
            // if (trace_rules == enumBoolYesNo.Yes) prdebug("counts");
            // if 'admin.listPlayers all' then do full update of players list and return
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                players.pre_scrub(); // reset all 'updated' flags to false

                admins_reset(); // empty list of currently logged on admins

                foreach (CPlayerInfo cp_info in lstPlayers)
                {
                    players.update(cp_info);
                    // add this player to list of logged-on admins if required
                    admins_add(cp_info.SoldierName);
                    // create/update a score variable for each player
                    string var_name = "%server_score[" + cp_info.SoldierName + "]%";
                    rulz_vars.set_value(null, var_name, cp_info.Score.ToString(), null);
                }
                players.scrub(); // remove all players that were not updated

            }
            if (trace_rules == enumBoolYesNo.Yes) prdebug("counts");
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnListPlayers");
                PrintException(ex);
            }
        }

        public override void OnReservedSlotsList(List<string> lstSoldierNames)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            this.reserved_slot_players = lstSoldierNames;
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnReservedSlotsList");
                PrintException(ex);
            }
        }

        // test_round_over will trigger On RoundOver rulz IF game is earlier than BF4 
        // OR all three BF4 round over events have fired
        // i.e. RoundOver, RoundOverPlayers, RoundOverTeamScores
        public void test_round_over()
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                if (game_id == GameIdEnum.BF3 ||
                    game_id == GameIdEnum.BFBC2 ||
                    game_id == GameIdEnum.MoH ||
                    ++round_over_event_count == 3)
                {
                    // check rules for On Round trigger
                    scan_rules(TriggerEnum.RoundOver, null, new Dictionary<SubstEnum, string>(), null, null, null);
                    round_over_event_count = 0;
                }
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in test_round_over()");
                PrintException(ex);
            }
        }

        public override void OnRoundOver(int iWinningTeamID)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: ********************OnRoundOver******************************");
                // trigger On RoundOver rulz if all round over events complete now
                test_round_over();
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnRoundOver");
                PrintException(ex);
            }
        }

        public override void OnRoundOverPlayers(List<CPlayerInfo> players)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: ********************OnRoundOverPlayers******************************");
                // trigger On RoundOver rulz if all round over events complete now
                test_round_over();
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnRoundOverPlayers");
                PrintException(ex);
            }
        }

        public override void OnRoundOverTeamScores(List<TeamScore> scores)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: ********************OnRoundOverTeamScores******************************");
                // trigger On RoundOver rulz if all round over events complete now
                test_round_over();
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnRoundOverTeamScores");
                PrintException(ex);
            }
        }

        // BFBC2 only
        // on round start we reset some of the ProconRulz counts 
        // (e.g. # times players have triggered rules)
        // the only subtlety is the rules haven't changed so we can keep those, 
        // and hence keep the list of 'watched' items
        // the 'Rates' counts (Rate 5 10) as of v32 continue past round/map change transitions...
        public override void OnLoadingLevel(string strMapFileName, int roundsPlayed, int roundsTotal)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: *******************OnLoadingLevel*****************************" +
                    strMapFileName);
                rulz_enable = true;
                current_map = this.GetMapByFilename(strMapFileName);
                current_map_mode = current_map.GameMode;

                // empty the rulz vars
                rulz_vars.reset();

                zero_counts();
                clear_blocks();
                // remove players no longer on the server from the rates counts
                scrub_rates(players.list_players());
                // exec listPlayers to initialise players global
                ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
                load_reserved_slot_players();
                // initialise ProconRulz On Init vars
                OnInit();
                // check rules for On Round trigger
                scan_rules(TriggerEnum.Round, null, new Dictionary<SubstEnum, string>(), null, null, null);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnLoadingLevel");
                PrintException(ex);
            }
        }

        // BF3 only
        // on round start we reset some of the ProconRulz counts 
        // (e.g. # times players have triggered rules)
        // the only subtlety is the rules haven't changed so we can keep those, 
        // and hence keep the list of 'watched' items
        // the 'Rates' counts (Rate 5 10) as of v32 continue past round/map change transitions...
        public void OnLevelLoaded(string strMapFileName, string strMapMode, int roundsPlayed, int roundsTotal)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: *******************OnLevelLoaded*****************************" +
                    strMapFileName + " " + strMapMode + " " + roundsPlayed + "/" + roundsTotal);
                // initialize counter for 3 BF4 RoundOver events (RoundOver, RoundOverPlayers, RoundOverTeamScores)
                round_over_event_count = 0;
                rulz_enable = true;
                current_map = this.GetMapByFilename(strMapFileName);
                current_map_mode = strMapMode;
                zero_counts();
                clear_blocks();
                // empty the rulz vars
                rulz_vars.reset();
                // remove players no longer on the server from the rates counts
                scrub_rates(players.list_players());
                // exec listPlayers to initialise players global
                ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
                load_reserved_slot_players();
                // initialise ProconRulz On Init vars
                OnInit();
                // check rules for On Round trigger
                scan_rules(TriggerEnum.Round, null, new Dictionary<SubstEnum, string>(), null, null, null);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnLevelLoaded");
                PrintException(ex);
            }
        }

        public void OnCurrentLevel(string strMapFileName)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            WriteDebugInfo("ProconRulz: *******************OnCurrentLevel*****************************" +
                strMapFileName);
            current_map = this.GetMapByFilename(strMapFileName);
            current_map_mode = current_map.GameMode;
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnCurrentLevel");
                PrintException(ex);
            }
        }

        public override void OnGlobalChat(string strSpeaker, string strMessage)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            say_rulz(strSpeaker, strMessage);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnGlobalChat");
                PrintException(ex);
            }
        }

        public override void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            say_rulz(strSpeaker, strMessage);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnTeamChat");
                PrintException(ex);
            }
        }

        public override void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
            say_rulz(strSpeaker, strMessage);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnSquadChat");
                PrintException(ex);
            }
        }

        public virtual void OnServerInfo(CServerInfo serverInfo) {
            List<TeamScore> scores = new List<TeamScore>(); // number of tickets remaining per team
            // EVENT EXCEPTION BLOCK:
            try
            {
                WriteDebugInfo("ProconRulz: *******************OnServerInfo*****************************");
                current_map = this.GetMapByFilename(serverInfo.Map);
                current_map_mode = serverInfo.GameMode;
                if (serverInfo.TeamScores != null)
                {
                    // set up team score variables %server_team_score[1]%, %server_team_score[2]% ...
                    foreach (TeamScore t in serverInfo.TeamScores)
                    {
                        if (t.TeamID == null || t.Score == null)
                        {
                            WriteConsole(String.Format("ProconRulz: OnServerInfo TeamID,Score error [{0}][{1}]", t.TeamID, t.Score));
                            break;
                        }
                        string var_name = "%server_team_score[" + t.TeamID.ToString() + "]%";
                        rulz_vars.set_value(null, var_name, t.Score.ToString(), null);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnServerInfo");
                PrintException(ex);
            }
        
        }

        #endregion

        #region Callbacks for SPAWN and KILL events - this is where most of the magic happens

        //**********************************************************************************************
        //**********************************************************************************************
        //   PROCESS VARIOUS PROCON 'EVENT' PROCEDURES - e.g. OnPlayerSpawned(), OnPlayerKilled()
        //**********************************************************************************************
        //**********************************************************************************************
        
        //********************************************************************************************
        // this procedure gets called when every player SPAWNS
        // so at that point we will scan the rules and decide whether to apply any actions
        //********************************************************************************************
        public override void OnPlayerSpawned(string player_name, Inventory inv)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                // debug stuff
                string blocks = "";
                if (player_blocks.ContainsKey(player_name)) 
                    blocks = String.Join("/", player_blocks[player_name].ToArray());
                WriteDebugInfo("********************OnPlayerSpawned***************************" +
                    player_name);
                WriteDebugInfo(String.Format("ProconRulz: OnPlayerSpawned [{0}] with blocks [{1}]", 
                    player_name, blocks));
                //end debug

                // create structure to hold substitution keywords and values
                Dictionary<SubstEnum, string> keywords = new Dictionary<SubstEnum, string>();

                // remove this player from the spawn item lists for now
                spawn_counts.zero_player(player_name); 
                remove_blocks(player_name);       // remove any player_blocks entries

                // BF bug workaround - 
                // check to see if player can do damage Shotgun before saving spec sp-shotgun_s
                bool has_damage_shotgun = false;

                string player_team_id = players.team_id(player_name);

                WriteDebugInfo(String.Format("ProconRulz: OnPlayerSpawned [{0}] team_id {1}", 
                    player_name, player_team_id));

                // add player to kit spawned list, if kit is 'watched'
                spawn_counts.add(item_key(inv.Kit), player_team_id, player_name); 

                player_kit[player_name] = item_key(inv.Kit); // record kit that this player spawned with
                WriteDebugInfo(String.Format("ProconRulz: OnPlayerSpawned [{0}] kit {1}", 
                    player_name, player_kit[player_name]));

                List<string> damages = new List<string>();
                List<string> weapon_keys = new List<string>();
                List<string> weapon_names = new List<string>();
                foreach (Weapon w in inv.Weapons)
                {
                    // add playername to watch list for weapon, if it's being watched
                    spawn_counts.add(item_key(w), player_team_id, player_name);

                    // add playername to watch list for weapon damage, if it's being watched
                    spawn_counts.add(item_key(w.Damage), player_team_id, player_name);
                    // BF bug workaround - remember if player can do damage shotgun
                    if (item_key(w.Damage).ToLower() == "shotgun") has_damage_shotgun = true;

                    // build 'weapons' and damages string lists for debug printout and subst variables
                    weapon_keys.Add(w.Name);
                    weapon_names.Add(weapon_desc(w.Name));
                    damages.Add(item_key(w.Damage));
                } // end looping through weapons in inventory
                // store %wk% subst var (weapon keys)
                if (weapon_keys.Count > 0)
                    keywords.Add(SubstEnum.WeaponKey, string.Join(", ", weapon_keys.ToArray()));
                else
                    keywords.Add(SubstEnum.WeaponKey, "No weapon key");
                // store %w% subst var (weapon names)
                if (weapon_names.Count > 0)
                    keywords.Add(SubstEnum.Weapon, string.Join(", ", weapon_names.ToArray()));
                else
                    keywords.Add(SubstEnum.Weapon, "No weapon");
                // store %d% subst var (damages)
                if (damages.Count > 0)
                    keywords.Add(SubstEnum.Damage, string.Join(", ", damages.ToArray()));
                else
                    keywords.Add(SubstEnum.Damage, "No damage");

                WriteDebugInfo(String.Format("ProconRulz: OnPlayerSpawned [{0}] weapons [{1}]", 
                    player_name, keywords[SubstEnum.Weapon]));

                List<string> spec_keys = new List<string>();
                List<string> spec_names = new List<string>();
                foreach (Specialization s in inv.Specializations)
                {
                    // condition is a BF bug workaround
                    // Add this Spec IF player has a weapon that does damage Shotgun, 
                    // OR if the spec is not 12-Gauge Sabot Rounds
                    if (has_damage_shotgun || (!(item_key(s).ToLower() == "sp_shotgun_s")))
                        spawn_counts.add(item_key(s), player_team_id, player_name);

                    spec_keys.Add(item_key(s));
                    spec_names.Add(spec_desc(s));
                } // end looping through specs in inventory
                // store %speck% subst var (spec keys)
                if (spec_keys.Count > 0)
                    keywords.Add(SubstEnum.SpecKey, string.Join(", ", spec_keys.ToArray()));
                else
                    keywords.Add(SubstEnum.SpecKey, "No spec key");
                // store %spec% subst var (specializations)
                if (spec_names.Count > 0)
                    keywords.Add(SubstEnum.Spec, string.Join(", ", spec_names.ToArray()));
                else
                    keywords.Add(SubstEnum.Spec, "No spec");

                WriteDebugInfo(
                    String.Format("^bProconRulz: [{0}] {1} spawned. Kit {2}. Weapons [{3}]. Specs [{4}]. Damages [{5}]",
                                        team_name(player_team_id),
                                        player_name,
                                        item_key(inv.Kit),
                                        keywords[SubstEnum.Weapon],
                                        keywords[SubstEnum.Spec],
                                        keywords[SubstEnum.Damage]
                                        ));

                // debug stuff
                if (trace_rules == enumBoolYesNo.Yes) prdebug("counts");

                //Check if the player carries any of the things we're looking for            
                scan_rules(TriggerEnum.Spawn, player_name, keywords, inv, null, null);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnPlayerSpawn");
                PrintException(ex);
            }
        }

        //********************************************************************************************
        // this procedure gets called when each KILL occurs in the game
        //********************************************************************************************
        public override void OnPlayerKilled(Kill k)
        {
            // EVENT EXCEPTION BLOCK:
            try
            {
                if (k == null)
                {
                    WriteDebugInfo("*******************OnPlayerKilled*****************NULL KILL OBJECT");
                    return;
                }

                if (k.Killer == null)
                {
                    WriteDebugInfo("*******************OnPlayerKilled***************NULL KILLER OBJECT");
                    return;
                }

                CPlayerInfo Killer = k.Killer;
                CPlayerInfo Victim = k.Victim;

                string player_name = Killer.SoldierName;
                string victim_name = Victim.SoldierName;

                // cache the player info in case we want the GUID
                if (player_name != null && player_name != "") player_info[player_name] = Killer;
                if (victim_name != null && victim_name != "") player_info[victim_name] = Victim;

                WriteDebugInfo("*******************OnPlayerKilled*************************"+player_name);

                if (Victim == null)
                {
                    WriteDebugInfo(String.Format("NULL VICTIM on this kill by {0}", player_name));
                    return;
                }

                WriteDebugInfo(String.Format("ProconRulz: OnPlayerKilled [{0}] [{1}]",
                                player_name, victim_name));
            
                string weapon_key = k.DamageType;
                if (weapon_key == "" || weapon_key == null) weapon_key = "No weapon key";

                WriteDebugInfo(String.Format("ProconRulz: OnPlayerKilled [{0}] weapon_key [{1}]",
                                player_name, weapon_key));

                Weapon weapon_used;
                try
                {
                    if (weapon_key == null) weapon_used = null;
                    else weapon_used = weaponDefines[weapon_key];
                }
                catch { weapon_used = null; }

                string weapon_descr = weapon_desc(weapon_key);
            
                WriteDebugInfo(String.Format("ProconRulz: OnPlayerKilled [{0}] weapon_descr [{1}]",
                                player_name, weapon_descr));
            
                string weapon_kit;
                try
                {
                    if (weapon_used == null) weapon_kit = "No weapon kit";
                    else weapon_kit = item_key(weapon_used.KitRestriction);
                }
                catch { weapon_kit = "No weapon kit"; }

                WriteDebugInfo(String.Format("ProconRulz: OnPlayerKilled [{0}] kit [{1}]",
                                player_name, weapon_kit));

                string damage;
                try
                {
                    damage = item_key(weapon_used.Damage);
                }
                catch { damage = "No damage key"; }

                WriteDebugInfo(String.Format("ProconRulz: OnPlayerKilled [{0}] damage [{1}]",
                                player_name, damage));
            
                add_kill_count(player_name, weapon_key);
                add_kill_count(player_name, damage);
                add_kill_count(player_name, weapon_kit);
            
                // debug
                string killer_counts = "";
            
                if (kill_counts.ContainsKey(player_name)) 
                    foreach (string item_name in kill_counts[player_name].Keys)
                    {
                        List<string> item_list = new List<string>();
                        item_list.Add(item_name);
                        killer_counts += item_name + "(" + 
                            count_kill_items(player_name, item_list).ToString() + ") ";
                    }
                else killer_counts = "0 kill counts";
            
                WriteDebugInfo(
                    String.Format("^bProconRulz: [{0} {1} [{2}]] killed [{3}] with [{4}], damage {5}, range {6}",
                    weapon_kit, player_name, killer_counts, 
                    victim_name, weapon_descr, damage, (Int32)k.Distance));
                //end debug
            
                // clear the dead soldier out of the 'counts' if it's first come first served
                // this will open up an opportunity for someone else to spawn with this players items
                if (reservationMode == ReserveItemEnum.Player_loses_item_when_dead)
                {
                    spawn_counts.zero_player(Victim.SoldierName);
                }
            
                TriggerEnum kill_type = TriggerEnum.Kill;
                string blocked_item = "";
                if (k.IsSuicide || player_name == null || player_name == "") // BF3 reports no killer with SoldierCollision
                {
                    kill_type = TriggerEnum.Suicide;
                    // - this is just for testing the suicide data
                    WriteDebugInfo("Suicide info: " +
                                    "k.IsSuicide=" + (k.IsSuicide ? "true" : "false") +
                                    ", player_name=" + (player_name == null ? "null" : "\"" + player_name + "\"") +
                                    ", victim_name=" + (victim_name == null ? "null" : "\"" + victim_name + "\"") +
                                    ", weapon_key=" + weapon_key
                                  );

                    if (player_name == null || player_name == "") player_name = victim_name;
                }
                else if (test_block(player_name, weapon_key))
                {
                    kill_type = TriggerEnum.PlayerBlock;
                    blocked_item = weapon_descr;
                    WriteDebugInfo(String.Format("ProconRulz: PlayerBlock [{0}] with weapon [{1}]",
                        player_name, blocked_item));
                }
                else if (test_block(player_name, weapon_kit))
                {
                    kill_type = TriggerEnum.PlayerBlock;
                    blocked_item = weapon_kit;
                    WriteDebugInfo(String.Format("ProconRulz: PlayerBlock [{0}] with kit [{1}]",
                        player_name, blocked_item));
                }
                else if (test_block(player_name, damage))
                {
                    kill_type = TriggerEnum.PlayerBlock;
                    blocked_item = damage;
                    WriteDebugInfo(String.Format("ProconRulz: PlayerBlock [{0}] with damage [{1}]",
                        player_name, blocked_item));
                }
                else if (Killer.TeamID == Victim.TeamID) kill_type = TriggerEnum.TeamKill;
            
                WriteDebugInfo(String.Format("ProconRulz: OnPlayerKilled for [{0}] is Event {1}", 
                                    player_name, 
                                    Enum.GetName(typeof(TriggerEnum), kill_type)));
                // now we do the main work of scanning the rules for this KILL
                scan_rules(kill_type, player_name, new Dictionary<SubstEnum, string>(), 
                    null, k, blocked_item);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in OnPlayerKilled");
                PrintException(ex);
            }
        }
        #endregion

        #region Step through the rules, looking for trigger that matches the event

        //**********************************************************************************************
        //**********************************************************************************************
        //   CHECK THE RULES to see if we should kill, kick, say
        //**********************************************************************************************
        //**********************************************************************************************
        // here is where we scan the rules after a player has joined, spawned or a kill has occurred
        private void scan_rules(TriggerEnum trigger, string name, 
                                Dictionary<SubstEnum, string> keywords, 
                                Inventory inv, Kill k, string item)
        {
            WriteDebugInfo(String.Format("ProconRulz: Scan_rules[{0}] with Event {1}",
                                name,
                                Enum.GetName(typeof(TriggerEnum), trigger)));

            // don't do anything if rulz_enable has been set to false
            if (!rulz_enable) return;

            // CATCH EXCEPTIONS
            try
            {
                // initial population of the 'keywords' dictionary
                assign_initial_keywords(name, ref keywords);

                // loop through the rules
                foreach (ParsedRule rule in parsed_rules)
                {
                    // skip comments
                    if (rule.comment) continue;

                    if (rule.trigger == trigger)
                    {
                        WriteDebugInfo(String.Format("ProconRulz: scan_rules[{0}] [{1}]", 
                            name, rule.unparsed_rule));
                        if (process_rule(trigger, rule, name, ref keywords, inv, k, item))
                        {
                            WriteDebugInfo(String.Format("ProconRulz: scan_rules[{0}] [{1}] FIRED", 
                                name, rule.unparsed_rule));
                            break; // break if any rule fires
                        }
                    }
                    // else WriteDebugInfo(String.Format("ProconRulz: scan_rules[{0}] skipped", name));
                } // end looping through the rules
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in scan_rules");
                PrintException(ex);
            }
        }
        
        // test a rule (we have already confirmed Spawn or Kill trigger is true)
        // will return 'true' if the rule is applied
        private bool process_rule(TriggerEnum trigger, ParsedRule rule, string player_name, 
                                        ref Dictionary<SubstEnum, string> keywords, 
                                        Inventory inv, Kill k, string item)
        {
            WriteDebugInfo(String.Format("ProconRulz:   process_rule[{0}] with event {1}",
                                player_name,
                                Enum.GetName(typeof(TriggerEnum), trigger)));

            // CATCH EXCEPTIONS
            try
            {
                List<PartClass> parts = new List<PartClass>(rule.parts);

                if (trigger == TriggerEnum.Say) keywords[SubstEnum.Text] = item;
                // Populate the Counts AS IF THIS RULE SUCCEEDED so conditions can use them
                keywords[SubstEnum.ServerCount] = count_server_rule(rule.id).ToString() + 1;
                keywords[SubstEnum.Count] = count_rule(player_name, rule.id).ToString() + 1;
                keywords[SubstEnum.TeamCount] = count_team_rule(players.team_id(player_name), rule.id).ToString() + 1;

                // populate the 'keywords' dictionary
                assign_keywords(trigger, rule, player_name, ref keywords, inv, k, item);

                if (!process_parts(rule, parts, player_name, ref keywords, k, item))
                {
                    WriteDebugInfo(String.Format("ProconRulz:   process_rule[{0}] in rule [{1}] tests NEGATIVE",
                        player_name, rule.unparsed_rule));
                    return false;
                }

                WriteDebugInfo(String.Format("ProconRulz:   process_rule[{0}] in rule [{1}] all conditions OK", 
                    player_name, rule.unparsed_rule));

                // return 'true' to quit rulz checks after this rule
                // if rule contains End, Kill, Kick, TempBan, Ban unless it contains Continue.
                return end_rulz(parts);
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in test_rule_and_exec(" +
                    rule.unparsed_rule + ")");
                PrintException(ex);
                return false;
            }
        }

        private bool process_parts(ParsedRule rule,  List<PartClass> parts, string player_name, 
                                        ref Dictionary<SubstEnum, string> keywords, 
                                        Kill k, string item)
        {
            PartClass current_part = null;
            try
            {
                bool playercount_updated = false; // we only update PlayerCount etc ONCE either after a successful
                // PlayerCount, or before the use of %c% etc
                // check each of the PARTS.
                // for each part in the rule, call process_part()
                foreach (PartClass p in parts)
                {
                    current_part = p; // so we can display the part that caused exception if needed
                    // see if we should update PlayerCount etc here
                    // rule can by NULL if parts is a list of TargetActions
                    if (rule != null && !playercount_updated && p.has_count)
                    {
                        update_counts(player_name, rule.id, ref keywords);
                        playercount_updated = true;
                    }
                    // HERE IS WHERE WE LEAVE THE PROC AND RETURN FALSE IF A CONDITION FAILS
                    if (!process_part(rule, p,
                            player_name, k, item, ref keywords))
                        return false;
                    WriteDebugInfo(String.Format("ProconRulz:     process_parts [{0}] OK", player_name));
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in process_parts (ProconRulz will continue)");
                WriteConsole("ProconRulz: Rule was: " + rule.unparsed_rule);
                if (current_part != null)
                {
                    WriteConsole("ProconRulz: Rule part was: " + current_part.ToString());
                }
                else
                {
                    WriteConsole("ProconRulz: Rule part was null");
                }
                PrintException(ex);
                return false;
            }
        }

        // return 'true' to end rulz if rule contains End, Kill, Kick, TempBan, Ban unless it contains Continue.
        bool end_rulz(List<PartClass> parts)
        {
            foreach (PartClass p in parts)
            {
                if (p.part_type == PartEnum.Continue) return false;
                if (p.part_type == PartEnum.End) return true;
            }
            foreach (PartClass p in parts)
            {
                if (p.part_type == PartEnum.Kill ||
                    p.part_type == PartEnum.Kick ||
                    p.part_type == PartEnum.TempBan ||
                    p.part_type == PartEnum.Ban ||
                    p.part_type == PartEnum.PBBan ||
                    p.part_type == PartEnum.PBKick
                    ) return true;
            }
            return false;
        }

        // return true if this part refers to a Count
        bool has_a_count(PartClass p)
        {
            if (p.part_type == PartEnum.PlayerCount ||
                p.part_type == PartEnum.TeamCount ||
                p.part_type == PartEnum.ServerCount) return true;

            foreach (string s in p.string_list)
            {
                foreach (string k in subst_keys[SubstEnum.Count])
                {
                    if (s.Contains(k)) return true;
                }
                foreach (string k in subst_keys[SubstEnum.TeamCount])
                {
                    if (s.Contains(k)) return true;
                }
                foreach (string k in subst_keys[SubstEnum.ServerCount])
                {
                    if (s.Contains(k)) return true;
                }
            }
            return false;
        }

        private void update_counts(string player_name, int rule_id,ref Dictionary<SubstEnum, string>keywords)
        {
            // increment the 'count' for this player for this rule
            add_rule_count(player_name, rule_id);
            // Now populate the Counts properly for the ACTIONS
            keywords[SubstEnum.ServerCount] = count_server_rule(rule_id).ToString();
            keywords[SubstEnum.Count] = count_rule(player_name, rule_id).ToString();
            keywords[SubstEnum.TeamCount] = count_team_rule(keywords[SubstEnum.PlayerTeamKey], rule_id).ToString();
        }

        private void assign_initial_keywords(string player_name, ref Dictionary<SubstEnum, string> keywords)
        {

            keywords[SubstEnum.Date] = DateTime.Now.ToString("yyyy_MM_dd");
            keywords[SubstEnum.Hhmmss] = DateTime.Now.ToString("HH:mm:ss");
            keywords[SubstEnum.Seconds] = Math.Floor(DateTime.Now.Subtract(new DateTime(2012, 1, 1, 0, 0, 0)).TotalSeconds).ToString();

            // put the map name and mode into the subst keys
            if (current_map != null)
            {
                keywords[SubstEnum.Map] = current_map.PublicLevelName;
                keywords[SubstEnum.MapMode] = current_map_mode;
            }

            keywords[SubstEnum.Teamsize] = players.min_teamsize().ToString();
            keywords[SubstEnum.Teamsize1] = players.teamsize("1").ToString();
            keywords[SubstEnum.Teamsize2] = players.teamsize("2").ToString();

            //debug -- these check values say where the exception was...
            int check = 1;
            if (player_name != null)
            {
                try
                {
                    check = 2;
                    keywords[SubstEnum.Player] = player_name;
                    check = 3;
                    keywords[SubstEnum.Ping] = players.ping(player_name).ToString();
                    check = 4;
                    keywords[SubstEnum.EA_GUID] = players.ea_guid(player_name);
                    check = 5;
                    keywords[SubstEnum.PB_GUID] = players.pb_guid(player_name);
                    check = 6;
                    keywords[SubstEnum.IP] = players.ip(player_name);
                    check = 7;

                    string player_team_id = players.team_id(player_name);
                    check = 8;
                    string player_squad_id = players.squad_id(player_name);
                    check = 9;

                    keywords[SubstEnum.PlayerTeamsize] = players.teamsize(player_team_id).ToString();
                    check = 10;

                    keywords[SubstEnum.PlayerTeam] = team_name(player_team_id);
                    check = 11;
                    keywords[SubstEnum.PlayerSquad] = squad_name(player_squad_id);
                    check = 12;
                    keywords[SubstEnum.PlayerTeamKey] = player_team_id;
                    check = 13;
                    keywords[SubstEnum.PlayerSquadKey] = player_squad_id;
                    check = 14;
                    keywords[SubstEnum.PlayerCountry] = players.cname(player_name);
                    check = 15;
                    keywords[SubstEnum.PlayerCountryKey] = players.ckey(player_name);    
                }
                catch (Exception ex)
                {
                    WriteConsole("ProconRulz: recoverable exception in assign_initial_keywords #1 check("+check.ToString()+")");
                    PrintException(ex);
                    return;
                }
            }
            else
            {
                keywords[SubstEnum.Player] = "";
            }
        }

        private void assign_keywords(TriggerEnum trigger, ParsedRule rulex, string player_name,
                                        ref Dictionary<SubstEnum, string> keywords,
                                        Inventory inv, Kill k, string item)
        {
            // HERE's WHERE WE UPDATE THE SUBST KEYWORDS (and conditions can as well, 
            // e.g. TargetPlayer)
            // this would be more efficient if I only updated the keywords that are
            // actually used in the rulz


            // update player count etc for this rule (for use of %c% parameter 
            // or subsequent Count condition)

            if (trigger == TriggerEnum.Spawn)
            {
                keywords[SubstEnum.Kit] = kit_desc(item_key(inv.Kit));
                keywords[SubstEnum.KitKey] = item_key(inv.Kit);
                // keywords[Weaponkey, Weapon, Damage, SpecKey, Spec] all set in OnPlaeyrSpawned
            }
            else if (trigger == TriggerEnum.Kill ||
                trigger == TriggerEnum.TeamKill ||
                trigger == TriggerEnum.Suicide ||
                trigger == TriggerEnum.PlayerBlock
                )
            {
                try
                {
                    // we're in a 'kill' type event here
                    // as far as I can tell, 'k.DamageType' usually contains 
                    // weapon key, but sometimes can be null

                    // with BF3, k.Killer.SoldierName can be empty (null or ""?)
                    if (k == null) return;

                    string victim_name = (k.Victim == null) ? "No victim" : k.Victim.SoldierName;
                    keywords[SubstEnum.Victim] = victim_name;

                    string weapon_key = k.DamageType;

                    Weapon weapon_used;
                    try
                    {
                        if (weapon_key == null) weapon_used = null;
                        else weapon_used = weaponDefines[weapon_key];
                    }
                    catch { weapon_used = null; }

                    string weapon_descr = weapon_desc(weapon_key);

                    string damage;
                    try
                    {
                        damage = (weapon_used == null) ? "No damage key" : item_key(weapon_used.Damage);
                    }
                    catch { damage = "No damage key"; }

                    keywords[SubstEnum.Weapon] = weapon_descr;
                    keywords[SubstEnum.WeaponKey] = weapon_key;

                    keywords[SubstEnum.KitKey] = spawned_kit(player_name);
                    keywords[SubstEnum.Kit] = kit_desc(spawned_kit(player_name));

                    keywords[SubstEnum.VictimKit] = kit_desc(spawned_kit(victim_name));
                    keywords[SubstEnum.VictimTeamKey] = players.team_id(victim_name);
                    keywords[SubstEnum.VictimTeam] = team_name(players.team_id(victim_name));

                    keywords[SubstEnum.VictimCountry] = players.cname(victim_name);
                    keywords[SubstEnum.VictimCountryKey] = players.ckey(victim_name);

                    keywords[SubstEnum.Damage] = damage;

                    keywords[SubstEnum.Range] = k.Distance.ToString("0.0");
                    keywords[SubstEnum.Headshot] = k.Headshot ? "Headshot" : "";
                }
                catch (Exception ex)
                {
                    WriteConsole("ProconRulz: recoverable exception in assign_keywords #2");
                    PrintException(ex);
                    return;
                }
            }

            if (trigger == TriggerEnum.PlayerBlock)
            {
                keywords.Add(SubstEnum.BlockedItem, item);
                WriteDebugInfo(String.Format("ProconRulz: test_rule[{0}] is PlayerBlock event for [{1}] OK",
                                    player_name, item));
            }

        }

        #endregion

        #region Process a single 'part' of rulz i.e. CONDITION or ACTION

        // check a condition (e.g. "Kit Recon 2") in the current rule
        // rule.trigger is already confirmed to be the current event (e.g. Kill, Spawn)
        private bool process_part(ParsedRule rule, PartClass p, 
                                        string player_name, 
                                        Kill k, string msg, 
                                        ref Dictionary<SubstEnum, string> keywords)
        {
            // CATCH EXCEPTIONS
            try
            {
                string not = p.negated ? "Not " : "";
                bool return_val = false;
                string player_team_id = "-1";
                if (keywords.ContainsKey(SubstEnum.PlayerTeamKey))
                {
                    player_team_id = keywords[SubstEnum.PlayerTeamKey];
                }
                switch (p.part_type)
                {
                    case PartEnum.Headshot:
                        // test "Headshot"
                        return_val = p.negated ? !k.Headshot : k.Headshot;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Headshot {2}", 
                            player_name, not, return_val));
                        return return_val;

                    case PartEnum.Protected:
                        // test player os on reserved slots list
                        return_val = p.negated ? !protected_player(player_name) : protected_player(player_name);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Protected {2}", 
                            player_name, not, return_val));
                        return return_val;

                    case PartEnum.Admin:
                        // test player is an admin
                        return_val = p.negated ? !is_admin(player_name) : is_admin(player_name);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Admin {2}", 
                            player_name, not, return_val));
                        return return_val;

                    case PartEnum.Admins:
                        // test if any admins are currently online
                        return_val = p.negated ? !admins_present() : admins_present();
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Admins {2}", 
                            player_name, not, return_val));
                        return return_val;

                    case PartEnum.Team:
                        // test "team attack|defend"
                        bool team_matches = team_match(p.string_list, player_team_id);
                        return_val = p.negated ? !team_matches : team_matches;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Actual team {2} versus {3} {4}", 
                            player_name, not, team_key(player_team_id), keys_join(p.string_list), return_val));
                        return return_val;

                    case PartEnum.Ping:
                        // test "Ping N"
                        int current_ping = players.ping(player_name);
                        return_val = p.negated ? current_ping < p.int1 : current_ping >= p.int1;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Ping {2} versus limit {3} {4}", 
                            player_name, not, current_ping, p.int1, return_val));
                        return return_val;

                    case PartEnum.Teamsize:
                        // test "Teamsize N"
                        int min_teamsize = players.min_teamsize();
                        return_val = p.negated ? min_teamsize > p.int1 : min_teamsize <= p.int1;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Teamsize {2} versus limit {3} {4}", 
                            player_name, not, min_teamsize, p.int1, return_val));
                        return return_val;

                    case PartEnum.Map:
                        // test map name or filename contains string1
                        return_val = p.negated ? !map_match(p.string_list) : map_match(p.string_list);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Actual map {2} versus {3} {4}", 
                            player_name, not,
                                                        current_map.PublicLevelName + " or " + current_map.FileName,
                                                        keys_join(p.string_list), return_val));
                        return return_val;

                    case PartEnum.MapMode:
                        // test "mapmode rush|conquest" 
                        return_val = p.negated ? !mapmode_match(p.string_list) : mapmode_match(p.string_list);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}Actual MapMode {2} versus {3} {4}", 
                            player_name, not, current_map_mode, keys_join(p.string_list), return_val));
                        return return_val;

                    case PartEnum.Kit:
                    case PartEnum.Weapon:
                    case PartEnum.Spec:
                    case PartEnum.Damage:
                        // test "Kit Recon 2" etc 
                        if (rule.trigger == TriggerEnum.Spawn)
                            // will check *player* item as well as team count (spawn)
                            return test_spawn_item(player_team_id, player_name, p);
                        else
                            // will also test kill item for TeamKill and Suicide
                            return test_kill_item(k, p);

                    case PartEnum.TeamKit:
                    case PartEnum.TeamWeapon:
                    case PartEnum.TeamSpec:
                    case PartEnum.TeamDamage:
                        return test_spawned_count(player_team_id, p);

                    case PartEnum.Range:
                        // test "Range > int1"
                        return_val = p.negated ? !(k.Distance < p.int1) : k.Distance > p.int1;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}range {2} > limit {3} {4}", 
                            player_name, not, k.Distance, p.int1, return_val));
                        return return_val;

                    case PartEnum.Count:
                    case PartEnum.PlayerCount:
                        // check how many times PLAYER has triggered this rule
                        int current_count = count_rule(player_name, rule.id);
                        bool count_valid = current_count > p.int1;
                        return_val = p.negated ? !count_valid : count_valid;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}PlayerCount {2} (actual {3}) {4}", 
                            player_name, not, p.int1, current_count, return_val));
                        return return_val;

                    case PartEnum.TeamCount:
                        // check how many times PLAYER'S TEAM has triggered this rule
                        int current_team_count = count_team_rule(players.team_id(player_name), rule.id);
                        bool count_team_valid = current_team_count > p.int1;
                        return_val = p.negated ? !count_team_valid : count_team_valid;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}TeamCount {2} (actual {3}) {4}", 
                            player_name, not, p.int1, current_team_count, return_val));
                        return return_val;

                    case PartEnum.ServerCount:
                        // check how many times ALL PLAYERS have triggered this rule
                        int current_server_count = count_server_rule(rule.id);
                        bool count_server_valid = current_server_count > p.int1;
                        return_val = p.negated ? !count_server_valid : count_server_valid;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}ServerCount {2} (Actual {3}) {4}", 
                            player_name, not, p.int1, current_server_count, return_val));
                        return return_val;

                    case PartEnum.Rate:
                        // check condition "Rate X Y" i.e. X hits on this rule in Y seconds
                        add_rate(player_name, rule.id);
                        bool rate_valid = check_rate(player_name, rule.id, p.int1, p.int2);
                        return p.negated ? !rate_valid : rate_valid;

                    case PartEnum.Text:
                        // check say text condition e.g. "Text ofc 4 ever,teamwork is everything"
                        int index = -1;
                        foreach (string t in p.string_list)
                        {
                            index = msg.ToLower().IndexOf(t.ToLower());
                            if (index >= 0 &&
                                    keywords[SubstEnum.Text] != null &&
                                    keywords[SubstEnum.Text].Length >= t.Length + 2)
                            {
                                // set up TargetText for TargetPlayer
                                keywords[SubstEnum.TargetText] = keywords[SubstEnum.Text].Substring(index + t.Length).Trim();
                            }
                            if (index >= 0) break;
                        }
                        return_val = p.negated ? index == -1 : index != -1;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}text {2} {3}", 
                            player_name, not, keys_join(p.string_list), return_val));
                        return return_val;

                    case PartEnum.TargetPlayer:
                        // check TargetPlayer condition, i.e. can we extract a playername from the say text
                        // updated from v33 for find_players to return a LIST of player names
                        // if only ONE playername matches, then automatically add TargetConfirm to action list...
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] checking TargetPlayer[{1}]",
                            player_name, keywords[SubstEnum.Text]));
                        List<string> player_names = new List<string>();
                        if (p.string_list != null && p.string_list.Count != 0)
                        {
                            // here the 'targettext' is specified in the rule e.g. "TargetPlayer bambam"
                            player_names = find_players(rulz_vars.replace_vars(player_name,
                                                                                replace_keys(p.string_list[0], keywords)));
                        }
                        // note only 1 playername is allowed in condition
                        else                                               // because it could contain rulz_item_separator
                        {
                            // here the targettext from a previous "Text" condition will be used
                            // if successful, we will modify TargetText to be AFTER the playername match string
                            string[] t_words = null;
                            if (keywords.ContainsKey(SubstEnum.TargetText))
                            {
                                t_words = quoted_split(keywords[SubstEnum.TargetText]);
                            }
                            if (t_words != null && t_words.Length > 0)
                            {
                                player_names = find_players(t_words[0]);
                                if (keywords[SubstEnum.TargetText].Length - t_words[0].Length > 1)
                                {
                                    keywords[SubstEnum.TargetText] =
                                        keywords[SubstEnum.TargetText].Substring(t_words[0].Length + 1);
                                }
                                else
                                {
                                    keywords[SubstEnum.TargetText] = "";
                                }
                            }
                        }
                        return_val = p.negated ? player_names.Count == 0 : player_names.Count == 1;
                        keywords[SubstEnum.Target] = player_names.Count == 0 ? "" : player_names[0];
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1}TargetPlayer {2} {3} with {4}",
                            player_name, not, keys_join(p.string_list), return_val, String.Join(",", player_names.ToArray())));
                        return return_val;

                    case PartEnum.Set:
                        // set rulz variable
                        rulz_vars.set_value(player_name, p.string_list[0], p.string_list[1], keywords);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] Set {1} {2}",
                            player_name, keys_join(p.string_list), true));
                        return true; // set always succeeds

                    case PartEnum.Incr:
                        rulz_vars.incr(player_name, p.string_list[0], keywords);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] Increment {1} {2}",
                            player_name, p.string_list[0], true));
                        return true; // Incr always succeeds

                    case PartEnum.Decr:
                        rulz_vars.decr(player_name, p.string_list[0], keywords);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] Decrement {1} {2}",
                            player_name, p.string_list[0], true));
                        return true; // Decr always succeeds

                    case PartEnum.Test: // aka If
                        // test var1 compare var2 (c.string_list[0..2])
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] IF %c% is [{1}]",
                            player_name, keywords[SubstEnum.Count]));
                        return_val = rulz_vars.test(player_name, p.string_list[0], p.string_list[1], p.string_list[2], keywords);
                        if (p.negated) return_val = !return_val;
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] {1} IF {2} {3}",
                            player_name, not, keys_join(p.string_list), return_val));
                        return return_val;

                    case PartEnum.MapList:
                        string mapList = p.string_list[0];
                        string status = p.string_list[1];
                        this.ExecuteCommand("procon.protected.plugins.call", "CUltimateMapManager", "CommandMapList", "ProconRulz", mapList, status);
                        WriteDebugInfo(String.Format("ProconRulz:     check_condition [{0}] MapList {1} {2}",
                            player_name, keys_join(p.string_list), true));
                        return true;

                    default:
                        take_action(player_name, p, keywords);
                        return true;
                }
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in process_part (ProconRulz will continue...)");
                WriteConsole("ProconRulz: process_part rule.unparsed_rule = " +
                                ((rule == null) ? 
                                    "(rule=null)" :
                                    (rule.unparsed_rule == null ? "(null)" : "[" + rule.unparsed_rule + "]")
                                )
                            );
                WriteConsole("ProconRulz: process_part player_name = " +
                                ((player_name == null) ? "(null)" :"[" + player_name + "]"));
                WriteConsole("ProconRulz: process_part p.part_type = " +
                                ((p == null) ? 
                                    "(p=null)" :
                                    (p.part_type == null ? "(null)" : p.part_type.ToString())
                                )
                            );
                WriteConsole("ProconRulz: process_part k.Killer.SoldierName = " +
                                ((k == null) ? 
                                    "(k=null)" :
                                    ((k.Killer == null) ? 
                                        "(k.Killer=null)" : 
                                        ((k.Killer.SoldierName==null) ? "(null)" : ("["+k.Killer.SoldierName+"]"))
                                    )
                                )
                            );
                PrintException(ex);
                return false;
            }

        }

        // test for an item at SPAWN
        // not_test is added to do a "Not Kit Recon" type test
        private bool test_spawn_item(string team_id, string player_name, PartClass c)
        {
            // e.g. "Kit Recon 2" => c.part_type = Kit, c.string1 = "Recon", c.int1 = 2
            bool found = spawn_counts.has_item(c.string_list, player_name);
            WriteDebugInfo(String.Format("ProconRulz: Test spawn item [KIT {0}] {1}", 
                keys_join(c.string_list), found ? "Found" : "Not Found"));

            // if we have NOT found the item then return FALSE for a regular condition,
            // or TRUE for a 'Not' condition:
            if (!found) return c.negated;

            // item found

            // if no count specified in condition then we can return now 
            // (success for normal rule, failure for 'Not' rule)
            if (c.int1 == 0) return !c.negated;

            // proceed to check item count
            return test_spawned_count(team_id, c);
        }

        // test for an item on a KILL
        private bool test_kill_item(Kill k, PartClass c)
        {
            try
            {
                if (k == null) return false;

                string weapon_key = k.DamageType;
                if (weapon_key == "") weapon_key = null;

                Weapon weapon_used;
                try
                {
                    if (weapon_key == null) weapon_used = null;
                    else weapon_used = weaponDefines[weapon_key];
                }
                catch { weapon_used = null; }

                string weapon_descr = weapon_desc(weapon_key);

                string weapon_kit;
                try
                {
                    if (weapon_used == null) weapon_kit = "No weapon kit";
                    else weapon_kit = item_key(weapon_used.KitRestriction);
                }
                catch { weapon_kit = "No weapon kit"; }

                string damage;
                try
                {
                    damage = item_key(weapon_used.Damage);
                }
                catch { damage = "No damage key"; }

                switch (c.part_type)
                {
                    case PartEnum.Weapon:
                        WriteDebugInfo(String.Format("ProconRulz: Test kill item [WEAPON {0}]",
                            keys_join(c.string_list)));
                        if (weapon_key == null) return c.negated;

                        if (keys_match(weapon_key, c.string_list))
                        {
                            WriteDebugInfo(String.Format("ProconRulz: Test kill item [WEAPON {0}] found", 
                                weapon_key));
                            break;
                        }
                        // not found, so return false unless c.negated
                        return c.negated;

                    case PartEnum.Damage:
                        WriteDebugInfo(String.Format("ProconRulz: Test kill item [DAMAGE {0}]", 
                            keys_join(c.string_list)));
                        if (keys_match(damage, c.string_list))
                        {
                            WriteDebugInfo(String.Format("ProconRulz: Test kill item [DAMAGE {0}] found", 
                                damage));
                            break;
                        }
                        return c.negated;

                    case PartEnum.Kit:
                        WriteDebugInfo(String.Format("ProconRulz: Test kill item [KIT {0}]", keys_join(c.string_list)));
                        string test_kit = weapon_kit;
                        // either use the kit type of the weapon, or the kit the player spawned with
                        if (player_kit.ContainsKey(k.Killer.SoldierName)) 
                            test_kit = player_kit[k.Killer.SoldierName];
                        if (keys_match(test_kit, c.string_list))
                        {
                            WriteDebugInfo(String.Format("ProconRulz: Test kill item [KIT {0}] found", 
                                test_kit));
                            break;
                        }
                        return c.negated;

                    default: // item type can be None
                        WriteDebugInfo(String.Format("ProconRulz: Test kill item [ignored] OK"));
                        return true;
                } // end switch on item type

                bool success;
                if (c.int1 == 0)
                {
                    success = true;
                    if (c.negated) success = !success;
                    WriteDebugInfo(String.Format("ProconRulz: Test kill item {0}", success));
                    // The item is being counted, and the count is above the limit in the rule
                    return success;
                }
                // check the item_limit value
                WriteDebugInfo(String.Format("ProconRulz: Test kill item [{0}] has {1}({2}) versus rule limit {3}",
                        k.Killer.SoldierName,
                        keys_join(c.string_list), 
                        count_kill_items(k.Killer.SoldierName, c.string_list),
                        c.int1));
                success = count_kill_items(k.Killer.SoldierName, c.string_list) > c.int1;
                if (c.negated) success = !success;
                return success;
            }
            catch (Exception ex)
            {
                WriteConsole("ProconRulz: recoverable exception in test_kill_item");
                PrintException(ex);
                return false;
            }
        }

        // test for an item at SPAWN
        // not_test is added to do a "Not Kit Recon" type test
        private bool test_spawned_count(string team_id, PartClass c)
        {
            // e.g. "TeamKit Recon 2" => c.part_type = Kit, c.string1 = "Recon", c.int1 = 2

            // check the item limit value
            int count = spawn_counts.count(c.string_list, team_id);
            WriteDebugInfo(String.Format("ProconRulz: Test spawn item count {0} versus limit {1}", 
                count, c.int1));
            if (count <= c.int1) return c.negated;

            return !c.negated; // i.e. spawn item above count, non-negated rule => return true
        }


#endregion

        #region process an ACTION

        //**********************************************************************************************
        //**********************************************************************************************
        //   EXECUTE THE ACTIONS IN THE CURRENT RULE
        //**********************************************************************************************
        //**********************************************************************************************
        // execute the next action 
        void take_action(string player_name, PartClass p, Dictionary<SubstEnum, string> keywords)
        {
            WriteDebugInfo(String.Format("ProconRulz: take_action[{0}] with action '{1}{2} {3}' by '{4}'", 
                                            player_name,
                                            p.target_action ? "TargetAction " : "",
                                            Enum.GetName(typeof(PartEnum), p.part_type),
                                            p.string_list[0],
                                            keywords[SubstEnum.Player]
                                         )
                          );

            // if current action is 'Continue' or 'End' then do nothing
            if (p.part_type == PartEnum.Continue || p.part_type == PartEnum.End) return;

            // if the current action was TargetAction, add to the players list and return
            if (p.target_action)
            {
                do_action(keywords[SubstEnum.Target], p, keywords);
                return;
            }

            // e.g. from a rule "On Say;Text Yes;TargetConfirm" -- 
            // do nothing (obsolete action)
            if (p.part_type == PartEnum.TargetConfirm)
            {
                return;
            }

            if (p.part_type == PartEnum.PlayerBlock)
            {
                add_block(player_name, p.string_list[0]);
                return;
            }

            // if the action is a "Kill", then IMMEDIATELY clear this soldier out of the spawn counts
            // this will open up an opportunity for someone else to spawn with this players items
            if (p.part_type == PartEnum.Kill && 
                reservationMode == ReserveItemEnum.Player_loses_item_when_dead &&
                !protected_player(player_name))
            {
                spawn_counts.zero_player(player_name);
            }

            do_action(player_name, p, keywords);
        }
        

        // execute the action (kill in a separate thread which can sleep if necessary)
        void do_action(string target, PartClass a, Dictionary<SubstEnum, string> keywords)
        {
            //object[] parameters = (object[])state;
            //string target = (string)parameters[0];
            //PartClass a = (PartClass)parameters[1];
            //Dictionary<SubstEnum, string> keywords = (Dictionary<SubstEnum,string>)parameters[2];

            // replace all the %p% etc with player name etc
            string message = replace_keys(a.string_list[0], keywords);
            message = rulz_vars.replace_vars(target, message);
            
            WriteDebugInfo(String.Format("ProconRulz: Doing action '{0} {1}' on {2}", 
                                            Enum.GetName(typeof(PartEnum), a.part_type),
                                            message,
                                            target ));       

            switch (a.part_type)
            {
                case PartEnum.Say:
                    ExecuteCommand("procon.protected.send", "admin.say", message, "all");
                    ExecuteCommand("procon.protected.chat.write", message);
                    break;

                case PartEnum.PlayerSay:
                    ExecuteCommand("procon.protected.send", "admin.say", message, "player", target);
                    ExecuteCommand("procon.protected.chat.write", String.Format("(PlayerSay {0}) ",
                        target) + message);
                    break;

                case PartEnum.TeamSay:
                    if (!keywords.ContainsKey(SubstEnum.PlayerTeamKey)) break; // skip if we don't know player team
                    ExecuteCommand("procon.protected.send", "admin.say", message, "team", keywords[SubstEnum.PlayerTeamKey]);
                    ExecuteCommand("procon.protected.chat.write", String.Format("(TeamSay[{0}] {1}) ",
                        keywords[SubstEnum.PlayerTeam],
                        target) + message);
                    break;

                case PartEnum.SquadSay:
                    if (!keywords.ContainsKey(SubstEnum.PlayerTeamKey)) break; // skip if we don't know player team
                    if (!keywords.ContainsKey(SubstEnum.PlayerSquadKey)) break; // skip if we don't know player squad
                    ExecuteCommand("procon.protected.send", 
                                   "admin.say", 
                                   message, 
                                   "squad", 
                                   keywords[SubstEnum.PlayerTeamKey], 
                                   keywords[SubstEnum.PlayerSquadKey]);
                    ExecuteCommand("procon.protected.chat.write", String.Format("(SquadSay[{0},{1}] {2}) ",
                        keywords[SubstEnum.PlayerTeam],
                        keywords[SubstEnum.PlayerSquad],
                        target) + message);
                    break;

                case PartEnum.VictimSay:
                    string victim_name = "";
                    try
                    {
                        victim_name = keywords[SubstEnum.Victim];
                    }
                    catch { }

                    if (victim_name != "")
                    {
                        ExecuteCommand("procon.protected.send",
                            "admin.say", message, "player", victim_name);
                        ExecuteCommand("procon.protected.chat.write", String.Format("(VictimSay {0}) ", 
                            victim_name) + message);
                    }
                    break;

                case PartEnum.AdminSay:
                    ExecuteCommand("procon.protected.chat.write", "(AdminSay) " + message);
                    if (!admins_present()) break;
                    foreach (string player_name in players.list_players()) 
                        if (is_admin(player_name)) 
                            ExecuteCommand("procon.protected.send", 
                                "admin.say", message, "player", player_name);
                    break;

                case PartEnum.Yell:
                    ExecuteCommand("procon.protected.send",
                        "admin.yell", message, a.int1.ToString(), "all");
                    ExecuteCommand("procon.protected.chat.write", message);
                    break;

                case PartEnum.PlayerYell:
                    ExecuteCommand("procon.protected.send",
                        "admin.yell", message, a.int1.ToString(), "player", target);
                    ExecuteCommand("procon.protected.chat.write",
                        String.Format("(PlayerYell {0}) ", target) + message);
                    break;

                case PartEnum.TeamYell:
                    if (!keywords.ContainsKey(SubstEnum.PlayerTeamKey)) break; // skip if we don't know player team
                    ExecuteCommand("procon.protected.send",
                        "admin.yell", message, a.int1.ToString(), "team", keywords[SubstEnum.PlayerTeamKey]);
                    ExecuteCommand("procon.protected.chat.write", String.Format("(TeamYell[{0}] {1}) ",
                        keywords[SubstEnum.PlayerTeam],
                        target) + message);
                    break;

                case PartEnum.SquadYell:
                    if (!keywords.ContainsKey(SubstEnum.PlayerTeamKey)) break; // skip if we don't know player team
                    if (!keywords.ContainsKey(SubstEnum.PlayerSquadKey)) break; // skip if we don't know player squad
                    ExecuteCommand("procon.protected.send",
                                   "admin.yell",
                                   message,
                                   a.int1.ToString(), 
                                   "squad",
                                   keywords[SubstEnum.PlayerTeamKey],
                                   keywords[SubstEnum.PlayerSquadKey]);
                    ExecuteCommand("procon.protected.chat.write", String.Format("(SquadYell[{0},{1}] {2}) ",
                        keywords[SubstEnum.PlayerTeam],
                        keywords[SubstEnum.PlayerSquad],
                        target) + message);
                    break;

                case PartEnum.Both:
                    ExecuteCommand("procon.protected.send", "admin.say", message, "all");
                    ExecuteCommand("procon.protected.send", 
                        "admin.yell", message, yell_delay.ToString(), "all");
                    ExecuteCommand("procon.protected.chat.write", message);
                    break;

                case PartEnum.PlayerBoth:
                    ExecuteCommand("procon.protected.send", "admin.say", message, "player", target);
                    ExecuteCommand("procon.protected.send", 
                        "admin.yell", message, yell_delay.ToString(), "player", target);
                    ExecuteCommand("procon.protected.chat.write", 
                        String.Format("(PlayerBoth {0}) ", target) + message);
                    break;

                case PartEnum.Log:
                    WriteLog(String.Format("ProconRulz: {0}", message));
                    break;
                    
                case PartEnum.All:
                    ExecuteCommand("procon.protected.send", "admin.say", message, "all");
                    ExecuteCommand("procon.protected.send", 
                        "admin.yell", message, yell_delay.ToString(), "all");            
                    WriteLog(String.Format("ProconRulz: {0}", message));
                    break;
                    
                case PartEnum.Kill:
                    if (protected_player(target))
                    {
                        WriteLog(String.Format("ProconRulz: Player {0} protected from Kill by ProconRulz",
                            target));
                        break;
                    }
                    do_kill(target, Int32.Parse(message));
                    break;

                case PartEnum.Kick:
                    if (protected_player(target))
                    {
                        WriteLog(String.Format("ProconRulz: Player {0} protected from Kick by ProconRulz",
                            target));
                        break;
                    }
                    //Thread.Sleep(kill_delay);
                    ExecuteCommand("procon.protected.send", "admin.kickPlayer", target, message);
                    WriteLog(String.Format("ProconRulz: Player {0} kicked", target));
                    break;

                case PartEnum.Ban:
                    if (protected_player(target))
                    {
                        WriteLog(String.Format("ProconRulz: Player {0} protected from Ban by ProconRulz",
                            target));
                        break;
                    }
                    try
                    {
                        ExecuteCommand("procon.protected.send",
                                            "banList.add",
                                            "guid",
                                            player_info[target].GUID,
                                            "perm",
                                            message);
                    }
                    catch
                    {
                        try
                        {
                            ExecuteCommand("procon.protected.send",
                                                "banList.add",
                                                "name",
                                                target,
                                                "perm",
                                                message);
                        }
                        catch
                        {
                            WriteLog(String.Format("ProconRulz: exception when banning {0}", target));
                        }
                    }
                    //Thread.Sleep(10000); // sleep for 10 seconds
                    ExecuteCommand("procon.protected.send", "banList.save");
                    //Thread.Sleep(10000); // sleep for 10 seconds
                    ExecuteCommand("procon.protected.send", "banList.list");
                    WriteLog(String.Format("ProconRulz: Player {0} banned", target));
                    break;

                case PartEnum.TempBan:
                    if (protected_player(target))
                    {
                        WriteLog(String.Format("ProconRulz: Player {0} protected from TempBan by ProconRulz",
                            target));
                        break;
                    }
                    try
                    {
                        ExecuteCommand("procon.protected.send",
                                            "banList.add",
                                            "guid",
                                            player_info[target].GUID,
                                            "seconds",
                                            a.int1.ToString(),
                                            message);
                    }
                    catch
                    {
                        try
                        {
                            ExecuteCommand("procon.protected.send",
                                                "banList.add",
                                                "name",
                                                target,
                                                "seconds",
                                                a.int1.ToString(),
                                                message);
                        }
                        catch
                        {
                            WriteLog(String.Format("ProconRulz: exception when TempBanning {0}", target));
                        }
                    }
                    //Thread.Sleep(10000); // sleep for 10 seconds
                    ExecuteCommand("procon.protected.send", "banList.save");
                    //Thread.Sleep(10000); // sleep for 10 seconds
                    ExecuteCommand("procon.protected.send", "banList.list");
                    WriteLog(String.Format("ProconRulz: Player {0} temp banned for {1} seconds",
                                                 target, a.int1.ToString()));
                    break;

                case PartEnum.PBBan:
                    if (protected_player(target))
                    {
                        WriteLog(String.Format("ProconRulz: Player {0} protected from PBBan by ProconRulz",
                            target));
                        break;
                    }
                    string guid = players.pb_guid(target);
                    if (guid == null || guid == "")
                        // no PB guid so try ban using name
                        ExecuteCommand("procon.protected.send", 
                                        "punkBuster.pb_sv_command", 
                                        String.Format("pb_sv_ban \"{0}\" \"{1}\"", 
                                                       target, 
                                                       message
                                                     )
                                      );
                    else // we have a PB guid
                        ExecuteCommand("procon.protected.send", 
                                        "punkBuster.pb_sv_command", 
                                        String.Format("pb_sv_banguid \"{0}\" \"{1}\" \"{2}\" \"{3}\"", 
                                                       guid,
                                                       target,
                                                       players.ip(target),
                                                       message
                                                     )
                                      );
                    ExecuteCommand("procon.protected.send", 
                                    "punkBuster.pb_sv_command", "pb_sv_updbanfile");  
                    WriteLog(String.Format("ProconRulz: Player {0} banned via Punkbuster", target));
                    break;

                case PartEnum.PBKick:
                    if (protected_player(target))
                    {
                        WriteLog(String.Format("ProconRulz: Player {0} protected from PBKick by ProconRulz",
                            target));
                        break;
                    }
                    ExecuteCommand("procon.protected.send", 
                                    "punkBuster.pb_sv_command",
                                    String.Format("pb_sv_kick \"{0}\" {1} \"{2}\"", 
                                                    target,
                                                    a.int1.ToString(),
                                                    message
                                                    )
                                    );
                    ExecuteCommand("procon.protected.send", 
                                    "punkBuster.pb_sv_command", "pb_sv_updbanfile");  
                    WriteLog(String.Format("ProconRulz: Player {0} kick/temp banned via Punkbuster for {1} minutes",
                                                 target, a.int1.ToString()));
                    break;

                case PartEnum.Execute:
                    // We need to make a string array out of 'procon.protected.send' 
                    // and the action message
                    // Note that we delay the %% substitutions until we have 'split' 
                    // the message in case we have spaces in subst values
                    List<string> parms_list = new List<string>();
                    // v39b.1 modification - Use command directly if it begins 'procon.'
                    if (a.string_list != null &&
                        a.string_list.Count != 0 &&
                        !(a.string_list[0].ToLower().StartsWith("procon.")))
                    {
                        parms_list.Add("procon.protected.send");
                    }
                    // if this is a punkbuster command then concatenate pb command into a single string
                    // e.g. pb_sv_getss "bambam"
                    if (a.string_list != null &&
                        a.string_list.Count != 0 &&
                        a.string_list[0].ToLower().StartsWith("punkbuster.pb_sv_command"))
                    {
                        parms_list.Add("punkBuster.pb_sv_command");
                        parms_list.Add(rulz_vars.replace_vars(target, replace_keys(a.string_list[0].Substring(25).TrimStart(), keywords)));
                    }
                    else
                        // for non-punkbuster commands each param is its own string...
                    {
                        WriteDebugInfo(String.Format("ProconRulz: do_action Exec <{0}>",
                                                a.string_list[0]));
                        foreach (string element in quoted_split(a.string_list[0])) // updated v40a.1 for quoted strings
                        {
                            // we 'replace_keys' for each fragment
                            parms_list.Add(rulz_vars.replace_vars(target, replace_keys(element, keywords)));
                            WriteDebugInfo(String.Format("ProconRulz: do_action Exec added element <{0}> <{1}>",
                                                            element, rulz_vars.replace_vars(target, replace_keys(element, keywords)))
                                          );

                        }
                    }

                    ExecuteCommand(parms_list.ToArray());

                    WriteLog(String.Format("ProconRulz: Executed command [{0}]", 
                                                String.Join(",",parms_list.ToArray())));
                    break;

                default:
                    WriteConsole(String.Format("ProconRulz: action thread error {0}", 
                        Enum.GetName(typeof(PartEnum), a.part_type)));
                    break;
                    
            }
        }

        // Kill in a separate thread, so we can sleep
        void do_kill(string player_name, int delay)
        {
            // spawn a thread to execute the action 
            // (the thread can include Sleep without shagging ProconRulz)
            WriteLog(String.Format("ProconRulz: Player {0} killed", player_name));
            ThreadPool.QueueUserWorkItem(new WaitCallback(kill_thread), new object[] { player_name, delay });

        }

        void kill_thread(object state)
        {
            object[] parameters = (object[])state;
            string target = (string)parameters[0];
            int delay = (int)parameters[1];

            Thread.Sleep(delay);
            ExecuteCommand("procon.protected.send", "admin.killPlayer", target);
        }
        #endregion

        #region Utility functions to test player Admin/Admins/Protected

        //**********************************************************************************************
        //*********************** Keep track of admins on the server  **********************************
        //**********************************************************************************************

        // admins_list is a list of playernames og logged-on admins
        List<string> admins_list = new List<string>();


        // return true if reserved list is being used AND player is on reserved list
        // or player or clan is explicitly listed in whitelist or player is admin
        private bool protected_player(string name)
        {
            // see if PLAYER NAME is on whitelist
            if (whitelist_players.Contains(name)) return true;
            // see if CLAN is on whitelist
            if (whitelist_clans.Contains(players.clan(name))) return true;
            // if we're not checking admins/reserved slots, then return false now
            if (protect_players == ProtectEnum.Neither) return false;
            // see if name is on ReservedSlots list
            if (protect_players == ProtectEnum.Admins_and_Reserved_Slots && reserved_slot_players.Contains(name))
                return true;
            // last shot - is this player an admin ?
            if (is_admin(name)) return true;
            // nope? return false then
            return false;
        }

        //**********************************************************************************************
        //*********************** Ask Procon if player is an admin  ************************************

        // do the procon api call to see if player_name has any procon admin rights
        bool procon_admin(string player_name)
        {
            CPrivileges p = this.GetAccountPrivileges(player_name);
            try
            {
                if (p.CanKillPlayers) return true;
            }
            catch { }
            // debug - have ProconRulz always accept [OFc] bambam
            return player_name == "PRDebug" || player_name == auth_name;
        }

        public void admins_add(string player_name)
        {
            // if player_name already on list the nothing needs to be done so just return immediately
            if (admins_list.Contains(player_name)) return;
            // otherwise check the procon admin status and add to list if necessary
            if (procon_admin(player_name)) admins_list.Add(player_name);
            return;
        }

        public void admins_remove(string player_name)
        {
            admins_list.Remove(player_name);
        }

        public bool is_admin(string player_name)
        {
            return player_name == "PRDebug" || admins_list.Contains(player_name);
        }

        public void admins_reset()
        {
            admins_list.Clear();
        }

        public bool admins_present()
        {
            return admins_list.Count > 0;
        }

        #endregion

        #region     Track various real-time counts of kills, rule rates etc

        #region Count resets, on startup and round change
        //**********************************************************************************************
        //**********************************************************************************************
        //   blocks, spawn counts and kill counts routines to keep track of 
        //   spawn(team) and kill(player) counts
        //**********************************************************************************************
        //**********************************************************************************************
        
        // zero all counts but keep keys in spawn counts - e.g. on round start, map load
        private void zero_counts()
        {
            spawn_counts.zero(); // remove entries for which players  have spawned with watched items
            kill_counts.Clear(); // empty out all kill counts for each player/item
            player_blocks.Clear(); // reset all player blocks
            rule_counts.Clear(); // reset number of times players have triggered rulz to 0
            //rule_times.Clear(); // reset timestamps of prior rules firing
            player_kit.Clear(); // reset the kit each player spawned with
        }

        // reset e.g. on plugin startup and loading new rules
        private void reset_counts()
        {
            spawn_counts.reset();
            kill_counts.Clear();
            player_blocks.Clear();
            rule_counts.Clear();
            rule_times.Clear(); // reset timestamps of prior rules firing
            player_kit.Clear(); // reset the kit each player spawned with
        }

        #endregion

        #region Kill counts for watched items

        private void add_kill_count(string player_name, string item_name)
        {
            string item_lcase = item_name == null ? "" : item_name.ToLower();
            List<string> item_names = new List<string>();
            item_names.Add(item_lcase);
            WriteDebugInfo(String.Format("ProconRulz:  add_kill_count to [{0}({1})] for [{2}]", 
                                item_name,
                                count_kill_items(player_name, item_names),
                                player_name));
            if (item_lcase == "none" || item_name == null || item_name == "" || player_name == null || 
                item_lcase == "no kit key" || item_lcase == "no weapon key" ||
                item_lcase == "no damage key" || item_lcase == "no spec key" ||
                player_name == "") return;
            if (!kill_counts.ContainsKey(player_name))
            {
                kill_counts[player_name] = new Dictionary<string, int>();
            }
            if (!kill_counts[player_name].ContainsKey(item_lcase))
            {
                kill_counts[player_name].Add(item_lcase, 1);
                return;
            }
            kill_counts[player_name][item_lcase] = kill_counts[player_name][item_lcase] + 1;
        }

        // return total count of kills with these items
        private int count_kill_items(string player_name, List<string> item_names)
        {
            if (item_names == null || item_names.Count == 0 || player_name == null || player_name == "") 
                return 0;
            if (!kill_counts.ContainsKey(player_name)) return 0;
            int count = 0;
            foreach (string i in item_names)
            {
                string item_lcase = i.ToLower();
                if (!kill_counts[player_name].ContainsKey(item_lcase)) continue;
                count += kill_counts[player_name][item_lcase];
            }
            return count;
        }

        #endregion

        #region Player rule counts

        // **********************************************************************************************
        // *************************** RULE COUNTS               ****************************************
        private void add_rule_count(string player_name, int rule_id)
        {
            if (player_name == null) // e.g. this could be an "On Round" rule
            {
                // if no player name then we can only update the 'server' count
                add_rule_count("proconrulz_server", rule_id);
                return;
            }
            // if this player is NOT special (i.e. team or server), then 
            // also add rule counts for the server and this player's team

            try
            {
                // this player isn't special, then add_rule_count for team and server
                if (!special_player(player_name))
                {
                    add_rule_count("proconrulz_server", rule_id);
                    add_rule_count(team_id_to_special_name(players.team_id(player_name)), rule_id);
                }
            }
            catch
            {
                WriteConsole(
                    String.Format("ProconRulz: RECOVERABLE ERROR exception in add_rule_count({0},{1})", 
                        player_name, rule_id));
            }

            WriteDebugInfo(String.Format("ProconRulz:  add_rule_count to [{0}({1})] for [{2}]", 
                                rule_id,
                                count_rule(player_name, rule_id),
                                player_name));
            if (!rule_counts.ContainsKey(player_name))
            {
                rule_counts[player_name] = new Dictionary<int, int>();
            }
            if (!rule_counts[player_name].ContainsKey(rule_id))
            {
                rule_counts[player_name].Add(rule_id, 1);
                return;
            }
            rule_counts[player_name][rule_id] = rule_counts[player_name][rule_id] + 1;
            return;
        }

        private int count_rule(string player_name, int rule_id)
        {
            string p = player_name;
            if (player_name == null) player_name = "proconrulz_server"; // e.g. could happen with "On Round" rule
            if (!rule_counts.ContainsKey(player_name)) return 0;
            if (!rule_counts[player_name].ContainsKey(rule_id)) return 0;
            return rule_counts[player_name][rule_id];
        }


        // manage counts for TEAM and SERVER
        private int count_team_rule(string team_id, int rule_id)
        {
            return count_rule(team_id_to_special_name(team_id), rule_id);
        }

        private int count_server_rule(int rule_id)
        {
            return count_rule("proconrulz_server", rule_id);
        }

        // we store total counts for the team and server, as well as the 'player counts'
        // the count for a team is stored as if there's a payer called "proconrulz_team_1" etc.
        private string team_id_to_special_name(string team_id)
        {
            if (team_id == null) return "proconrulz_team_unknown";
            return String.Format("proconrulz_team_{0}", team_id);
        }

        // returns true if this player name is a special name (i.e. team or server)
        private bool special_player(string player_name)
        {
            if (player_name == null) return false;
            return (player_name+"************").Substring(0, 10) == "proconrulz";
        }

        #endregion

        #region Rates functions

        // **********************************************************************************************
        // *************************** RATES FUNCTIONS           ****************************************

        // add a value into the 'rule_times' global for the most recent time 
        // this rule was triggered for this player
        private void add_rate(string player_name, int rule_id)
        {
            WriteDebugInfo(String.Format("ProconRulz:  add_rate to rule {0} for [{1}]",
                                rule_id,
                                player_name));
            // need to be careful if this gets called for an On Round event (i.e. no playername)
            if (player_name == null) return;
            if (player_name == "") return;
            if (!rule_times.ContainsKey(player_name))
            {
                rule_times[player_name] = new Dictionary<int, DateTime[]>();
            }
            if (!rule_times[player_name].ContainsKey(rule_id))
            {
                rule_times[player_name][rule_id] = new DateTime[RATE_HISTORY];
                rule_times[player_name][rule_id][0] = DateTime.Now;
                return;
            }
            // at the moment we shuffle all the times up by 1, and give entry [0] the current time
            for (int i = RATE_HISTORY - 1; i > 0; i--)
            {
                rule_times[player_name][rule_id][i] = rule_times[player_name][rule_id][i - 1];
            }
            rule_times[player_name][rule_id][0] = DateTime.Now;
            return;
        }

        // return true IF player_name has triggered rule[rule_id] rate_count times over rate_time seconds
        private bool check_rate(string player_name, int rule_id, int rate_count, int rate_time)
        {
            WriteDebugInfo(String.Format("ProconRulz:  check_rate to rule {0} for [{1}]",
                                rule_id,
                                player_name));
            if (player_name == null || player_name == ""|| !rule_times.ContainsKey(player_name))
            {
                WriteDebugInfo(String.Format("ProconRulz:  check_rate for player [{1}] not in rule_times!",
                                    player_name));
                return false;
            }
            if (!rule_times[player_name].ContainsKey(rule_id))
            {
                WriteDebugInfo(String.Format("ProconRulz:  check_rate no time for rule {0} for player [{1}]",
                                    rule_id,
                                    player_name));
                return false;
            }
            try
            {
                // if we're checking "Rate 5 10" (rule hit 5 times in 10 seconds)
                // we look up the timestamp of the 4th previous hit, 
                // and subtract that from Now, and see if thats <10 seconds
                DateTime prev = rule_times[player_name][rule_id][rate_count-1];
                DateTime now = DateTime.Now;
                double period = (now.Subtract(prev)).TotalSeconds;
                WriteDebugInfo(
                    String.Format("ProconRulz:  check_rate to rule {0} for [{1}], period {2} seconds (versus min {3})",
                                    rule_id,
                                    player_name,
                                    period,
                                    rate_time));
                return period < rate_time;
            } catch { return false; }
        }

        // we leave the rates accumulating through round changes, 
        // so we need a way of scrubbing out players that have
        // left the server (even though their arrays of timestamps will now be static).
        // scrub_rates will be given a list of players *currently* on the server, 
        // and will remove other entries
        // currently called on loading level
        private void scrub_rates(List<string> player_list)
        {
            List<string> rates_players = new List<string>(rule_times.Keys);
            foreach (string rates_player in rates_players)
            {
                if (!player_list.Contains(rates_player)) rule_times.Remove(rates_player);
            }
        }

        #endregion

        #region PlayerBlocks (i.e. players can be blocked from spawning with a given item

        //*********************************************************************************************
        // player_blocks Dictionary <string playername, List<string> item_names>
        // add_block(player_name, item_name)
        // remove_blocks(player_name)
        // test_block (player_name, item_name)
        // clear_blocks()
        
        private void add_block(string player_name, string item_name)
        {
            if (!player_blocks.ContainsKey(player_name))
            {
                player_blocks[player_name] = new List<string>();
            }
            player_blocks[player_name].Add(item_name);
            return;
        }
        
        private void remove_blocks(string player_name)
        {
            player_blocks.Remove(player_name);
        }

        private bool test_block(string player_name, string item_name)
        {
            if (!player_blocks.ContainsKey(player_name)) return false;
            if (player_blocks[player_name].Contains(item_name)) return true;
            return false;
        }
        
        private void clear_blocks() // remove all blocks at start of round etc.
        {
            player_blocks.Clear();
        }
        
        // track kit player spawned with
        private string spawned_kit(string player_name)
        {
            if (player_kit.ContainsKey(player_name)) return player_kit[player_name];
            else return "No kit key";
        }

        #endregion

        #endregion

        #region Misc utility functions (convert item key to item description, team names etc)

        //**********************************************************************************************
        //**********************************************************************************************
        //   UTILITY PROCEDURES
        //**********************************************************************************************
        //**********************************************************************************************

        // a bit of funky c# overloading to convert item to string key
        string item_key(Kits k) {
            if (k == null || k == Kits.None) return "No kit key";
            try
            {
                return Enum.GetName(typeof(Kits), k);
            }
            catch { }
            return "No kit key";
        }

        string item_key(Weapon w) {
            if (w == null) return "No weapon key";
            try
            {
                return w.Name; 
            }
            catch { }
            return "No weapon key";
        }

        string item_key(Specialization s) {
            if (s == null) return "No spec key";
            try
            {
                return s.Name;
            }
            catch { }
            return "No spec key";
        }

        string item_key(DamageTypes d)
        {
            if (d == null) return "No damage key";
            try
            {
                return Enum.GetName(typeof(DamageTypes), d);
            }
            catch { }
            return "No damage key";
        }

        // this func will return a list of strings where input was key1|key2|key3..
        // and replace '&' chars with ' ', so "M15&AT&MINE" becomes "M15 AT MINE"
        List<string> item_keys(string keys_in)
        {
            List<string> key_list = new List<string>();
            if (keys_in != null)
            try
            {
                string[] key_strings = keys_split(keys_in);
                foreach (string k in key_strings)
                    key_list.Add(k.Replace(rulz_key_separator, ' '));
                return key_list;
            }
            catch { }
            //key_list.Add("No key"); 
            return key_list;
        }

        // split a x|y|z string into an array
        string[] keys_split(string keys)
        {
            if (keys == null) return null;
            return keys.Split(new char[] { rulz_item_separator }, StringSplitOptions.RemoveEmptyEntries);
        }

        // split a x|y|z string into an array
        static string keys_join(List<string> keys)
        {
            if (keys == null) return null;
            return String.Join(rulz_item_separator.ToString(), keys.ToArray());
        }

        string keys_join(string[] keys)
        {
            if (keys == null) return null;
            return String.Join(rulz_item_separator.ToString(), keys);
        }

        // return true if exact key found in keys
        bool keys_match(string key, List<string> keys)
        {
            if (keys == null) return false;
            foreach (string k in keys) if (k.ToLower() == key.ToLower()) return true;
            return false;
        }

        // convert a key to 'rulz' format including '&' chars replacing spaces
        string rulz_key(string k)
        {
            if (k == null) return "No_key";
            try
            {
                return k.Replace(' ', rulz_key_separator);
            }
            catch { }
            return "No_key";
        }

        // convert an item key to its description (e.g. m95 -> "M95 Sniper Rifle")
        string kit_desc(string key) 
        {
            if (key == null || key == "" || key == "No kit key" || key == "None" ) return "No kit";
            try
            {
                return this.GetLocalized(key, String.Format("global.Kits.{0}", key)); 
            }
            catch { }
            return key + "(Kit has no Procon name)";
        }
        
        string weapon_desc(string key)
        {
            if (key == null || key == "" || key == "None" || key == "No weapon key") return "No weapon";
            try
            {
                return this.GetLocalized(key, String.Format("global.Weapons.{0}", key.ToLower()));
            }
            catch { }
            return key + "(Weapon has no Procon name)";
        }

        string damage_desc(DamageTypes damage)
        {
            if (damage == null) return "No damage";
            try
            {
                return Enum.GetName(typeof(DamageTypes), damage);
            }
            catch { }
            return "No damage";
        }

        string spec_desc(Specialization s)
        {
            if (s == null) return "No spec";
            try
            {
                return this.GetLocalized(s.Name, String.Format("global.Specialization.{0}", 
                    s.Name.ToLower()));
            }
            catch { }
            return "No spec";
        }

        // apply the %..% substitution vars to message (e.g. "hello %p%" becomes "hello bambam")
        static string replace_keys(string message, Dictionary<SubstEnum, string> keywords)
        {
            if (message == null) return null;
            if (keywords == null) return message;
            foreach (SubstEnum keyval in keywords.Keys)
            {
                foreach (string k in subst_keys[keyval])
                {
                    message = message.Replace(k, keywords[keyval]);
                }
                
            }
            return message;
        }

        // return the 'localization key' for the team with this ID
        private string team_key(string team_id)
        {
            try
            {
                if (current_map == null) return "No team key";

                foreach (CTeamName team in current_map.TeamNames)
                {
                    if (team.TeamID == Int32.Parse(team_id))
                        return team.LocalizationKey;
                }
            }
            catch { }
            return "No team key";
        }

        // return the 'localization key' for the squad with this ID
        private string squad_key(string squad_id)
        {
            try
            {
                int id = Int32.Parse(squad_id);
                if (id < 0) return "No squad key";
            }
            catch
            {
                return "No squad key";
            }
            return "global.Squad"+squad_id;
        }

        // convert an int 'team_id' into display name for team - varies by map
        private string team_name(string team_id)
        {
            string name = String.Format("[Map unknown](team:{0})", team_id);
            if (current_map == null) return name;

            try
            {
                string team_localization_key = team_key(team_id);
                if (team_localization_key == null)
                    name = String.Format("[Map OK team key unknown](team:{0})", team_id);
                else
                    name = this.GetLocalized(team_localization_key, team_localization_key);
            }
            catch { }
            return name;
        }

        // convert an int 'team_id' into display name for team - varies by map
        private string squad_name(string squad_id)
        {
            try
            {
                    string localization_key = squad_key(squad_id);
                    return this.GetLocalized(localization_key, localization_key);
            }
            catch { return "No squad name"; }
        }

        // test whether current map localization key (text) has any condition team as substring, or teamid
        private bool team_match(List<string> condition_teams, string team_id)
        {
            string key = team_key(team_id);
            try
            {
                foreach (string t in condition_teams)
                {
                    try { 
                        if (t == team_id) return true;
                    } catch {
                        if (key.ToLower().IndexOf(t.ToLower()) >= 0) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // test whether current map matches any in condition
        private bool map_match(List<string> condition_maps)
        {
            if (current_map == null) return false;
            foreach (string m in condition_maps)
                if ((current_map.PublicLevelName.ToLower().IndexOf(m.ToLower()) != -1) ||
                                 (current_map.FileName.ToLower().IndexOf(m.ToLower()) != -1)) return true;
            return false;
        }

        // test whether current map matches any in condition
        private bool mapmode_match(List<string> condition_modes)
        {
            if (current_map == null) return false;
            foreach (string m in condition_modes)
                if (current_map_mode.ToLower().IndexOf(m.ToLower()) != -1) return true;
            return false;
        }

        private void load_reserved_slot_players()
        {
            WriteDebugInfo(String.Format("ProconRulz: loading protected players list"));
            ExecuteCommand("procon.protected.send", "reservedSlots.list");
        }

        private string strip_braces(string s)
        {
            return s.Replace("{","~(").Replace("}",")~");
        }

        private List<string> find_players(string partname)
        {
            List<string> player_names = new List<string>();
            // debug
            if (partname == "PRDebug") player_names.Add("PRDebug");

            foreach (string player_name in players.list_players())
            {
                if (player_name.ToLower().IndexOf(partname.ToLower()) != -1)
                {
                    WriteDebugInfo(String.Format("ProconRulz:       find_player with {0} found {1}", 
                        partname, player_name));
                    player_names.Add(player_name);
                }
            }
            WriteDebugInfo(String.Format("ProconRulz:       find_player with {0} matches", player_names.Count));
            return player_names;
        }

        // split a string into elements separated by spaces, binding quoted strings into one element
        // e.g. Exec vars.serverName "OFc Server - no nubs" will be parsed to [vars.serverName,"OFc Server - no nubs"]
        private String[] quoted_split(string str)
        {
            string quoted_str = null; // var to accumulate full string, quoted or not
            char? quote_char = null; // ? makes char accept nulls --  null or opennig quote char of current quoted string
                                     // quote_char != null used as flag to confirm we are mid-quoted-string

            List<string> result = new List<string>();

            if (str == null) return result.ToArray();

            foreach (string s in str.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (s.StartsWith("\"") || s.StartsWith("\'")) // start of quoted string - allow matching " or'
                {
                    quote_char = s[0];
                }
                if (quote_char == null) // NOT in quoted-string so just add this element to list
                {
                    result.Add(s);
                }
                else //we're in a quoted string so accumulate
                {
                    if (quoted_str == null)
                    {
                        quoted_str = s; // no accumulated quoted string so far so start with s
                    }
                    else
                    {
                        quoted_str += " " + s; // append s to accumulated quoted_str
                    }
                }
                // check if we just ended a quoted string
                if (quote_char != null && s.EndsWith(quote_char.ToString())) // end of quoted string
                {
                    result.Add(quoted_str.Substring(1).Substring(0,quoted_str.Length-2));
                    quoted_str = null;
                    quote_char = null; // quoted_str is now complete
                }
            }
            // check to see if we've ended with an incomplete quoted string... if so add it
            if (quote_char != null && quoted_str != null)
            {
                result.Add(quoted_str);
            }
            return result.ToArray();
        }

        #endregion

        #region Processing of 'On Say' events to display 'rulz' message or scan rules

        private void say_rulz(string player_name, string msg)
        {
            WriteDebugInfo("****************say_rulz******************************"+player_name);
            if (msg.Contains("prdebug"))
            {
                prdebug(msg);
                return;
            }
            WriteDebugInfo(String.Format("ProconRulz: say_rulz({0},{1})", player_name, msg));
            if (player_name != "Server")             // scan for any "On Say" rulz
                scan_rules(TriggerEnum.Say, player_name, 
                    new Dictionary<SubstEnum, string>(), null, null, msg);

            WriteDebugInfo("say_rulz(" + player_name +","+msg+") - testing protected player");
            if (is_admin(player_name))
                if (msg.ToLower().IndexOf("rulz on") != -1)
                {
                    WriteDebugInfo("say_rulz(" + player_name + "," + msg + ") - rulz on");
                    rulz_enable = true;
                    ExecuteCommand("procon.protected.send",
                        "admin.say", "Rulz enabled", "all");
                    return;
                }
                else if (msg.ToLower().IndexOf("rulz off") != -1)
                {
                    rulz_enable = false;
                    ExecuteCommand("procon.protected.send", 
                        "admin.say", "Rulz disabled until next round", "player", player_name);
                    return;
                }
                // *** DEBUG rcon tests ***
                // test #1 - KICK (all servers)
                else if (msg.ToLower().IndexOf("xyzzy") == 0)
                {
                    // IMPORTANT DEBUG - limit these tests to ADMIN only
                    // this test should work on all game servers
                    if (!is_admin(player_name)) return; // safety in case top check removed
                    List<string> player_names = find_players(msg.Substring(6));
                    if (player_names.Count != 1) return;
                    ExecuteCommand("procon.protected.send", "admin.kickPlayer", player_names[0], "Player disconnected");
                }
                // test #2 - MOVE PLAYER (BFBC2-BF4)
                else if (msg.ToLower().IndexOf("xyzzm") == 0)
                {
                    // IMPORTANT DEBUG- limit these tests to ADMIN only
                    // this test for BF4 only
                    if (!is_admin(player_name)) return; // safety in case top check removed
                    List<string> player_names = find_players(msg.Substring(6));
                    if (player_names.Count != 1) return;
                    string team_id = players.team_id(player_names[0]);
                    if (team_id == "-1") return;
                    if (team_id == "0") return;
                    if (team_id == "1") team_id = "2";
                    else if (team_id == "2") team_id = "1";
                    else if (team_id == "3") team_id = "4";
                    else if (team_id == "4") team_id = "3";
                    else return;
                    ExecuteCommand("procon.protected.send", "admin.movePlayer", player_names[0], team_id, "0", "true" );
                }
                // test #3 - SQUAD LEADER (all games)
                else if (msg.ToLower().IndexOf("xyzzl") == 0)
                {
                    // IMPORTANT DEBUG- limit these tests to ADMIN only
                    // this test for BF4 only
                    if (!is_admin(player_name)) return; // safety in case top check removed
                    List<string> player_names = find_players(msg.Substring(6));
                    if (player_names.Count != 1) return;
                    string team_id = players.team_id(player_names[0]);
                    string squad_id = players.squad_id(player_names[0]);
                    if (team_id == "-1" || squad_id == "-1") return;
                    ExecuteCommand("procon.protected.send", "squad.leader", team_id, squad_id, player_names[0]);
                }
                // any admin player can type "prv" and see ProconRulz version
                else if (msg.ToLower().IndexOf("prversion") == 0)
                {
                    ExecuteCommand("procon.protected.send",
                        "admin.say", "ProconRulz version " + version.ToString(), "player", player_name);
                }
            //WriteDebugInfo(String.Format("ProconRulz: testing Rulz for [{0}] who said \"{1}\"",player_name, msg));
        }

        #endregion

        #region debug commands (via say text 'prdebug' commands)

        private void prdebug(string say_text)
        {
            if (say_text.IndexOf("players") >= 0)
            {
                WriteConsole("ProconRulz: ***************Debug command players*********************");
                WriteConsole(String.Format("ProconRulz: players in new player cache = {0}",
                                                String.Join(",", players.list_new_players().ToArray())
                                                ));

                foreach (string team_id in players.list_team_ids())
                {
                    WriteConsole(String.Format("ProconRulz: players (team {0}:{1}) = {2}",
                                                team_id,
                                                team_name(team_id),
                                                String.Join(",", players.list_players(team_id).ToArray())
                                                ));
                }
                return;
            }

            if (say_text.IndexOf("count") >= 0)
            {
                WriteConsole("ProconRulz: ****************Debug command counts**********************");
                prdebug("players");

                prdebug("teamsize");

                prdebug("watched");

                return;
            }

            if (say_text.IndexOf("watched") >= 0)
            {
                WriteConsole("ProconRulz: ********************Debug command watched******************");

                List<string> watched_items = spawn_counts.list_items();

                WriteConsole(String.Format("ProconRulz: Watched items are: {0}", 
                    string.Join(", ", spawn_counts.list_items().ToArray())));

                List<string> debug_list = new List<string>();

                foreach (string team_id in players.list_team_ids())
                {
                    foreach (string item_name in watched_items)
                    {
                        List<string> item_list = new List<string>();
                        item_list.Add(item_name);
                        debug_list.Add(String.Format("{0}({1}:{2})",
                                            item_name,
                                            spawn_counts.count(item_list, team_id),
                                            String.Join(",", spawn_counts.list_players(item_name, team_id).ToArray())
                                        ));
                    }

                    WriteConsole(String.Format("ProconRulz: spawn_counts (team {0}:{1}) = {2}", 
                        team_id, team_name(team_id), String.Join(" ", debug_list.ToArray())));
                }
                return;
            }

            if (say_text.IndexOf("teamsize") >= 0)
            {
                WriteConsole("ProconRulz: ********************Debug command teamsize******************");

                WriteConsole(String.Format("ProconRulz: min teamsize {0}", players.min_teamsize()));
                foreach (string team_id in players.list_team_ids())
                {
                    WriteConsole(String.Format("ProconRulz: players (team {0}:{1}) = {2} players: {3}",
                                                team_id,
                                                team_name(team_id),
                                                players.teamsize(team_id),
                                                String.Join(",", players.list_players(team_id).ToArray())
                                                ));
                }
                return;
            }

            int xsay_pos = say_text.IndexOf("xsay");
            if (xsay_pos >= 0)
            {
                say_rulz("PRDebug", say_text.Substring(xsay_pos+5));
                return;
            }

            if (say_text.IndexOf("dump") >= 0)
            {
                WriteConsole(String.Format("ProconRulz: Listing the rulz_vars:"));
                Dictionary<string, string> vars = rulz_vars.dump();
                foreach (string var_name in vars.Keys)
                {
                    WriteConsole(String.Format("ProconRulz: rulz_vars[{0}] = \"{1}\"",
                                                var_name,
                                                vars[var_name]
                                                ));
                }
                WriteConsole(String.Format("ProconRulz: Listing complete"));
                return;
            }

            WriteConsole("ProconRulz: Debug command not valid: \""+say_text+"\"");
        }
        #endregion

        #region Print rulz

        // this whole procedure is just for debugging purposes
        // it prints the rule out to the Procon console
        private void print_parsed_rule(ParsedRule rule)
        {
            if (rule.comment)
            {
                WriteDebugInfo(String.Format("ProconRulz: Rule {0}: {1}", rule.id, rule.unparsed_rule));
                return;
            }

            // read each rule variable, convert to string, and then print formatted
            string trigger_string = 
                String.Format("On {0}:",Enum.GetName(typeof(TriggerEnum), rule.trigger));

            string parts_string = (rule.parts == null || rule.parts.Count == 0) ? " CONTINUE;" : " ";

            foreach (PartClass p in rule.parts)
            {
                parts_string += p.ToString();
            }

            WriteDebugInfo(String.Format("ProconRulz: Rule {0}: {1}{2}", 
                rule.id, trigger_string, parts_string));
        }

        #endregion

        #region Display plugin details

        // get_details() returns the "Details" HTML description to display in Procon
        private string get_details()
        {
            string[] kit_keys = Enum.GetNames(typeof(Kits));
            string[] kit_lines = new string[kit_keys.Length+3];
            int wi = 2;
            kit_lines[0] = "<table>";
            kit_lines[1] = "<tr><th>Description</th><th>Kit key</th></tr>";

            string kit_descr;
            while (wi < kit_keys.Length+2)
            {
                kit_descr = kit_desc(kit_keys[wi-2]);
                kit_lines[wi] = String.Format("<tr><td><b>{0}</b></td><td>{1}</td></tr>",
                                            kit_descr,
                                            kit_keys[wi-2]
                                            ) ;
                wi++;
            }
            kit_lines[wi] = "</table>";
            string kits_string = string.Join(" ",kit_lines);
            
            string[] weapons = new string[weaponDefines.Count+3];
            weapons[0] = "<table>";
            weapons[1] = "<tr><th>Description</th><th>Weapon key</th><th>Damage</th><th>Kit</th></tr>";
            wi = 2;
            string wdesc, wname, wdamage, wkit;
            while (wi < weaponDefines.Count+2) //weapons[wi++] = "<tr><td>xxx</td><td>yyy</td><td>zzz</td></tr>";
            {
               wdesc = this.GetLocalized(weaponDefines[wi-2].Name,String.Format("global.Weapons.{0}",weaponDefines[wi-2].Name.ToLower()));
               wname = rulz_key(weaponDefines[wi-2].Name);
               wdamage = Enum.GetName(typeof(DamageTypes), weaponDefines[wi-2].Damage);
               wkit = Enum.GetName(typeof(Kits), weaponDefines[wi-2].KitRestriction);
               weapons[wi++] = String.Format("<tr><td><b>{0}</b></td><td>{1}</td><td>{2}</td><td>{3}</td></tr>",
                                            wdesc, //this.GetLocalized(weaponDefines[wi-2].Name,String.Format("global.Weapons.{0}",weaponDefines[wi-2].Name.ToLower())),
                                            wname, //weaponDefines[wi-2].Name,
                                            wdamage, //Enum.GetName(typeof(DamageTypes), weaponDefines[wi-2].Damage)
                                            wkit
                                            ) ;
            }
            weapons[wi] = "</table>";
            string weapon_string = string.Join(" ", weapons);
            
            string[] specs = new string[specDefines.Count+3];
            specs[0] = "<table>";
            specs[1] = "<tr><th>Description</th><th>Specialization key</th></tr>";
            wi = 2;
            string sdesc, sname;
            while (wi < specDefines.Count+2)
            {
                sdesc = this.GetLocalized(specDefines[wi-2].Name,String.Format("global.Specialization.{0}",specDefines[wi-2].Name.ToLower()));
                sname = item_key(specDefines[wi-2]);
                specs[wi++] = String.Format("<tr><td><b>{0}</b></td><td>{1}</td></tr>",
                                            sdesc,
                                            sname
                                            ) ;
            }
            specs[wi] = "</table>";
            string spec_string = string.Join("", specs);
            
            string desc = String.Format(@"<h2>ProconRulz Procon plugin</h2>
                <p>Please see <a href=""http://www.forsterlewis.com/proconrulz.pdf"">the ONLINE documentation</a>
                (RIGHT-CLICK and select Open in New Window...)
                for a fuller explanation of how to use ProconRulz.</p>

                <p>You can 'right-click' and select 'Print...' to print this page.</p>

                <p>Apply admin commands (e.g. Kill, Kick, Say) to players<br/>
                according to certain 'conditions' (e.g. spawned with Kit Recon)<br/>
                Allows programming of weapon or kit limits, with suitable messages.</p>

                <p><b>actions</b> include kick, ban, or just a warning (yell, say).</p>

                <p><b>conditions</b> include kit type, weapon type, and can be applied at 
                    Spawn time or on a Kill.</p>
                <p>Each rule has three parts:</p>
                <ol>
                    <li><b>Trigger</b> - i.e. when the rule should fire, On Spawn, On Kill, On Teamkill etc</li>
                    <li><b>Conditions</b> - list of tests to apply before actions are done, e.g. Headshot, Kit Recon etc</li>
                    <li><b>Actions</b> - list of admin actions to take if all conditions succeed, e.g. Kill, Kick, Say</li>
                </ol>
                        <h2>List of all weapons, kits and specializations</h2>
                        <h3>Kits</h3>{0}{4}{0}
                        <h3>Weapons</h3>{0}{1}{0}
                        <h3>Damage</h3>{0}{2}<br/><br/>{0}
                        <h3>Specializations</h3>{0}{3}{0}
                        ", 
                        Environment.NewLine,
                        weapon_string,
                        string.Join(", ", Enum.GetNames(typeof(DamageTypes))),
                        spec_string,
                        kits_string
                        );
            return desc;
        }
        
        #endregion

        #region Console, Chat window output routines

        public string CreateEnumString(string Name, string[] valueList)
        {
            return string.Format("enum.{0}_{1}({2})", GetType().Name, Name, string.Join("|", valueList));
        }
        public string CreateEnumString(Type enumeration)
        {
            return CreateEnumString(enumeration.Name, Enum.GetNames(enumeration));
        }

        public void PrintException(Exception ex)
        {
            WriteConsole("ProconRulz: " + ex.ToString());
        }

        public void WriteDebugInfo(string message)
        {
            if (trace_rules == enumBoolYesNo.Yes) 
                ExecuteCommand("procon.protected.pluginconsole.write", strip_braces(message));
        }

        public void WriteLog(string message)
        {
            //ExecuteCommand("procon.protected.pluginconsole.write", message);
            string m = strip_braces(message);
            switch (log_file)
            {
                case LogFileEnum.PluginConsole:
                    ExecuteCommand("procon.protected.pluginconsole.write", m);
                    break;
                case LogFileEnum.Console:
                    ExecuteCommand("procon.protected.console.write", m);
                    break;
                case LogFileEnum.Chat:
                    ExecuteCommand("procon.protected.chat.write", m);
                    break;
                case LogFileEnum.Events:
                    ExecuteCommand("procon.protected.events.write", m);
                    break;
                //case LogFileEnum.Discard_Log_Messages:
                default:
                    break;
            }
            if (message.IndexOf("prdebug") >= 0) prdebug(message);
        }

        public void WriteConsole(string message)
        {
            //ExecuteCommand("procon.protected.pluginconsole.write", message);
            ExecuteCommand("procon.protected.pluginconsole.write", strip_braces(message));
        }

        #endregion

        #region Commented Out listing of other Procon callbacks

        // updated for BF3

        #region Global/Login
        /*
        public virtual void OnLogin() { }
        public virtual void OnLogout() { }
        public virtual void OnQuit() { }
        public virtual void OnVersion(string serverType, string version) { }
        public virtual void OnHelp(List<string> commands) { }

        public virtual void OnRunScript(string scriptFileName) { }
        public virtual void OnRunScriptError(string scriptFileName, int lineError, string errorDescription) { }

        public virtual void OnServerInfo(CServerInfo serverInfo) { }
        public virtual void OnResponseError(List<string> requestWords, string error) { }

        public virtual void OnYelling(string message, int messageDuration, CPlayerSubset subset) { }
        public virtual void OnSaying(string message, CPlayerSubset subset) { }
        */
        #endregion

        #region Map Functions
        /*
        public virtual void OnRestartLevel() { }
        public virtual void OnSupportedMaps(string playlist, List<string> lstSupportedMaps) { }
        public virtual void OnListPlaylists(List<string> playlists) { }

        public virtual void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset) {
        }

        public virtual void OnEndRound(int iWinningTeamID) { }
        public virtual void OnRunNextLevel() { }
        public virtual void OnCurrentLevel(string mapFileName) { }
        */
        #region BFBC2
        /*
        public virtual void OnPlaylistSet(string playlist) { }
        */
        #endregion

        #endregion

        #region Banlist
        /*
        public virtual void OnBanAdded(CBanInfo ban) { }
        public virtual void OnBanRemoved(CBanInfo ban) { }
        public virtual void OnBanListClear() { }
        public virtual void OnBanListSave() { }
        public virtual void OnBanListLoad() { }
        public virtual void OnBanList(List<CBanInfo> banList) { }
        */
        #endregion

        #region Text Chat Moderation
	/// BFBC2 & MoH
    
	    /*
        public virtual void OnTextChatModerationAddPlayer(TextChatModerationEntry playerEntry) { }
        public virtual void OnTextChatModerationRemovePlayer(TextChatModerationEntry playerEntry) { }
        public virtual void OnTextChatModerationClear() { }
        public virtual void OnTextChatModerationSave() { }
        public virtual void OnTextChatModerationLoad() { }
        public virtual void OnTextChatModerationList(TextChatModerationDictionary moderationList) { }
        */
        #endregion
        
        #region Maplist
        /*
        public virtual void OnMaplistConfigFile(string configFileName) { }
        public virtual void OnMaplistLoad() { }
        public virtual void OnMaplistSave() { }

        public virtual void OnMaplistList(List<MaplistEntry> lstMaplist) { }
        public virtual void OnMaplistCleared() { }
        public virtual void OnMaplistMapAppended(string mapFileName) { }
        public virtual void OnMaplistNextLevelIndex(int mapIndex) { }
        public virtual void OnMaplistGetMapIndices(int mapIndex, int nextIndex) { } // BF3
        public virtual void OnMaplistMapRemoved(int mapIndex) { }
        public virtual void OnMaplistMapInserted(int mapIndex, string mapFileName) { }
        */
        #endregion

        #region Variables
        
        #region Details
        /*
        public virtual void OnServerName(string serverName) { }
        public virtual void OnServerDescription(string serverDescription) { }
        public virtual void OnBannerURL(string url) { }
        */
        #endregion

        #region Configuration
        /*
        public virtual void OnGamePassword(string gamePassword) { }
        public virtual void OnPunkbuster(bool isEnabled) { }
        public virtual void OnRanked(bool isEnabled) { }
        public virtual void OnRankLimit(int iRankLimit) { }
        public virtual void OnPlayerLimit(int limit) { }
        public virtual void OnMaxPlayerLimit(int limit) { }
        public virtual void OnCurrentPlayerLimit(int limit) { }
        public virtual void OnIdleTimeout(int limit) { }
        public virtual void OnProfanityFilter(bool isEnabled) { }
        */
        #endregion

        #region Gameplay
        /*
        public virtual void OnFriendlyFire(bool isEnabled) { }
        public virtual void OnHardcore(bool isEnabled) { }
        */
        #region BFBC2
        /*
        public virtual void OnTeamBalance(bool isEnabled) { }
        public virtual void OnKillCam(bool isEnabled) { }
        public virtual void OnMiniMap(bool isEnabled) { }
        public virtual void OnCrossHair(bool isEnabled) { }
        public virtual void On3dSpotting(bool isEnabled) { }
        public virtual void OnMiniMapSpotting(bool isEnabled) { }
        public virtual void OnThirdPersonVehicleCameras(bool isEnabled) { }
        */
        #endregion

        #endregion

        #region Team Kill
        /*
        public virtual void OnTeamKillCountForKick(int limit) { }
        public virtual void OnTeamKillValueIncrease(int limit) { }
        public virtual void OnTeamKillValueDecreasePerSecond(int limit) { }
        public virtual void OnTeamKillValueForKick(int limit) { }
        */
        #endregion

        #region Level Variables
	/// NOT BF3
        /*
        public virtual void OnLevelVariablesList(LevelVariable requestedContext, List<LevelVariable> returnedValues) { }
        public virtual void OnLevelVariablesEvaluate(LevelVariable requestedContext, LevelVariable returnedValue) { }
        public virtual void OnLevelVariablesClear(LevelVariable requestedContext) { }
        public virtual void OnLevelVariablesSet(LevelVariable requestedContext) { }
        public virtual void OnLevelVariablesGet(LevelVariable requestedContext, LevelVariable returnedValue) { }
        */
        #endregion

        #region Text Chat Moderation Settings
	/// NOT BF3
        /*
        public virtual void OnTextChatModerationMode(ServerModerationModeType mode) { }
        public virtual void OnTextChatSpamTriggerCount(int limit) { }
        public virtual void OnTextChatSpamDetectionTime(int limit) { }
        public virtual void OnTextChatSpamCoolDownTime(int limit) { }
        */
        #endregion

        #region Reserved/Specate Slots
        /// Note: This covers MoH's reserved spectate slots as well.
        /// NOT BF3 (yet)
        /*
        public virtual void OnReservedSlotsConfigFile(string configFileName) { }
        public virtual void OnReservedSlotsLoad() { }
        public virtual void OnReservedSlotsSave() { }
        public virtual void OnReservedSlotsPlayerAdded(string soldierName) { }
        public virtual void OnReservedSlotsPlayerRemoved(string soldierName) { }
        public virtual void OnReservedSlotsCleared() { }
        public virtual void OnReservedSlotsList(List<string> soldierNames) { }
        */
        #endregion

        #endregion

        #region Player Actions
        /*
        public virtual void OnPlayerKilledByAdmin(string soldierName) { }
        public virtual void OnPlayerKickedByAdmin(string soldierName, string reason) { }
        public virtual void OnPlayerMovedByAdmin(string soldierName, int destinationTeamId, int destinationSquadId, bool forceKilled) { }
        */
        #endregion
        
        // These events are sent from the server without any initial request from the client.
        #region Game Server Requests (Events)

        #region Players
        /*
        public virtual void OnPlayerJoin(string soldierName) {
        }

        public virtual void OnPlayerLeft(CPlayerInfo playerInfo) {
        }

        public virtual void OnPlayerAuthenticated(string soldierName, string guid) { }
        public virtual void OnPlayerKilled(Kill kKillerVictimDetails) { }
        public virtual void OnPlayerKicked(string soldierName, string reason) { }
        public virtual void OnPlayerSpawned(string soldierName, Inventory spawnedInventory) { }

        public virtual void OnPlayerTeamChange(string soldierName, int teamId, int squadId) { }
        public virtual void OnPlayerSquadChange(string soldierName, int teamId, int squadId) { }
        */
        #endregion

        #region Chat
        /*
        public virtual void OnGlobalChat(string speaker, string message) { }
        public virtual void OnTeamChat(string speaker, string message, int teamId) { }
        public virtual void OnSquadChat(string speaker, string message, int teamId, int squadId) { }
        */
        #endregion

        #region Round Over Events
        /*
        public virtual void OnRoundOverPlayers(List<CPlayerInfo> players) { }
        public virtual void OnRoundOverTeamScores(List<TeamScore> teamScores) { }
        public virtual void OnRoundOver(int winningTeamId) { }
        */
        #endregion

        #region Levels
        /*
        public virtual void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) { }
        public virtual void OnLevelStarted() { }
        public virtual void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal) { } // BF3
        */
        #endregion

        #region Punkbuster
        /*
        public virtual void OnPunkbusterMessage(string punkbusterMessage) { }

        public virtual void OnPunkbusterBanInfo(CBanInfo ban) { }

        public virtual void OnPunkbusterUnbanInfo(CBanInfo unban) { }

        public virtual void OnPunkbusterBeginPlayerInfo() { }

        public virtual void OnPunkbusterEndPlayerInfo() { }

        public virtual void OnPunkbusterPlayerInfo(CPunkbusterInfo playerInfo) {
        }
        */
        #endregion

        #endregion

        #region Internal Procon Events

        #region Accounts
        /*
        public virtual void OnAccountCreated(string username) { }
        public virtual void OnAccountDeleted(string username) { }
        public virtual void OnAccountPrivilegesUpdate(string username, CPrivileges privileges) { }

        public virtual void OnAccountLogin(string accountName, string ip, CPrivileges privileges) { }
        public virtual void OnAccountLogout(string accountName, string ip, CPrivileges privileges) { }

        */
        #endregion

        #region Command Registration
        /*
        public virtual void OnAnyMatchRegisteredCommand(string speaker, string text, MatchCommand matchedCommand, CapturedCommand capturedCommand, CPlayerSubset matchedScope) { }

        public virtual void OnRegisteredCommand(MatchCommand command) { }

        public virtual void OnUnregisteredCommand(MatchCommand command) { }
        */
        #endregion

        #region Battlemap Events
        /*
        public virtual void OnZoneTrespass(CPlayerInfo playerInfo, ZoneAction action, MapZone sender, Point3D tresspassLocation, float tresspassPercentage, object trespassState) { }
        */
        #endregion

        #region HTTP Server
        /*
        public virtual HttpWebServerResponseData OnHttpRequest(HttpWebServerRequestData data) {
        }
        */
        #endregion

        #endregion

        #region Layer Procon Events

        #region Variables
        /*
        public virtual void OnReceiveProconVariable(string variableName, string value) { }
        */
        #endregion

        #endregion

        #region previous callbcks for BFBC2
        //*************************************************************************************************
        //*************************************************************************************************
        //   UNUSED PROCON METHODS
        //*************************************************************************************************
        //*************************************************************************************************
        /*        
                public void OnAccountCreated(string strUsername) { }
                public void OnEndRound(int iWinningTeamID) { }
                public void OnAccountDeleted(string strUsername) { }
                public void OnAccountPrivilegesUpdate(string strUsername, CPrivileges spPrivs) { }
                public void OnReceiveProconVariable(string strVariableName, string strValue) { }
                public void OnConnectionClosed() { }
                public void OnPlayerAuthenticated(string strSoldierName, string strGuid) { }
                public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName) { }
                public void OnPunkbusterMessage(string strPunkbusterMessage) { }
                public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan) { }
                public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer) { }
                public void OnResponseError(List<string> lstRequestWords, string strError) { }
                public void OnHelp(List<string> lstCommands) { }
                public void OnVersion(string strServerType, string strVersion) { }
                public void OnLogin() { }
                public void OnLogout() { }
                public void OnQuit() { }
                public void OnRunScript(string strScriptFileName) { }
                public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription) { }
                public void OnServerInfo(CServerInfo csiServerInfo) { }
                public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset) { }
                public void OnSaying(string strMessage, CPlayerSubset cpsSubset) { }
                public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps) { }
                public void OnPlaylistSet(string strPlaylist) { }
                public void OnListPlaylists(List<string> lstPlaylists) { }
                public void OnPlayerKicked(string strSoldierName, string strReason) { }
                public void OnPlayerSquadChange(string strSoldierName, int iTeamID, int iSquadID) { }
                public void OnBanList(List<CBanInfo> lstBans) { }
                public void OnBanAdded(CBanInfo cbiBan) { }
                public void OnBanRemoved(CBanInfo cbiUnban) { }
                public void OnBanListClear() { }
                public void OnBanListLoad() { }
                public void OnBanListSave() { }
                public void OnReservedSlotsConfigFile(string strConfigFilename) { }
                public void OnReservedSlotsLoad() { }
                public void OnReservedSlotsSave() { }
                public void OnReservedSlotsPlayerAdded(string strSoldierName) { }
                public void OnReservedSlotsPlayerRemoved(string strSoldierName) { }
                public void OnReservedSlotsCleared() { }
                public void OnMaplistConfigFile(string strConfigFilename) { }
                public void OnMaplistLoad() { }
                public void OnMaplistSave() { }
                public void OnMaplistMapAppended(string strMapFileName) { }
                public void OnMaplistCleared() { }
                public void OnMaplistList(List<string> lstMapFileNames) { }
                public void OnMaplistNextLevelIndex(int iMapIndex) { }
                public void OnMaplistMapRemoved(int iMapIndex) { }
                public void OnMaplistMapInserted(int iMapIndex, string strMapFileName) { }
                public void OnRunNextLevel() { }
                public void OnCurrentLevel(string strCurrentLevel) { }
                public void OnRestartLevel() { }
                public void OnLevelStarted() { }
                public void OnGamePassword(string strGamePassword) { }
                public void OnPunkbuster(bool blEnabled) { }
                public void OnHardcore(bool blEnabled) { }
                public void OnRanked(bool blEnabled) { }
                public void OnRankLimit(int iRankLimit) { }
                public void OnTeamBalance(bool blEnabled) { }
                public void OnFriendlyFire(bool blEnabled) { }
                public void OnMaxPlayerLimit(int iMaxPlayerLimit) { }
                public void OnCurrentPlayerLimit(int iCurrentPlayerLimit) { }
                public void OnPlayerLimit(int iPlayerLimit) { }
                public void OnBannerURL(string strURL) { }
                public void OnServerDescription(string strServerDescription) { }
                public void OnKillCam(bool blEnabled) { }
                public void OnMiniMap(bool blEnabled) { }
                public void OnCrossHair(bool blEnabled) { }
                public void On3dSpotting(bool blEnabled) { }
                public void OnMiniMapSpotting(bool blEnabled) { }
                public void OnThirdPersonVehicleCameras(bool blEnabled) { }
                public void OnPlayerLeft(CPlayerInfo cpiPlayer) { }
                public void OnServerName(string strServerName) { }
                public void OnTeamKillCountForKick(int iLimit) { }
                public void OnTeamKillValueIncrease(int iLimit) { }
                public void OnTeamKillValueDecreasePerSecond(int iLimit) { }
                public void OnTeamKillValueForKick(int iLimit) { }
                public void OnIdleTimeout(int iLimit) { }
                public void OnProfanityFilter(bool isEnabled) { }
                public void OnRoundOverTeamScores(List<TeamScore> lstTeamScores) { }
                public void OnLevelVariablesList(LevelVariable lvRequestedContext, List<LevelVariable> lstReturnedValues) { }
                public void OnLevelVariablesEvaluate(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue) { }
                public void OnLevelVariablesClear(LevelVariable lvRequestedContext) { }
                public void OnLevelVariablesSet(LevelVariable lvRequestedContext) { }
                public void OnLevelVariablesGet(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue) { }

         */
        #endregion
        #endregion
    }
}
