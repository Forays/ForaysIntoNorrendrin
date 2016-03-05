/*Copyright (c) 2011-2016  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using OpenTK.Graphics;
using Utilities;
using PosArrays;
namespace Forays{
	public static class Global{
		public const string VERSION = "0.8.4";
		public static bool LINUX = false;
		public static bool GRAPHICAL = false;
		public const int SCREEN_H = 28;
		public const int SCREEN_W = 88;
		public const int ROWS = 22;
		public const int COLS = 66;
		public const int MAP_OFFSET_ROWS = 3;
		public const int MAP_OFFSET_COLS = 21;
		public const int STATUS_WIDTH = 20;
		public const int MAX_LIGHT_RADIUS = 12; //the maximum POSSIBLE light radius. used in light calculations.
		public const int MAX_INVENTORY_SIZE = 20;
		public const int HIGH_SCORES = 25;
		public static bool GAME_OVER = false;
		public static bool BOSS_KILLED = false;
		public static bool QUITTING = false;
		public static bool SAVING = false;
		public static string KILLED_BY = "";
		public const string ForaysImageResources = "Forays.ForaysImages.";
		public const int MESSAGE_LOG_LENGTH = 1000;

		public static Stopwatch Timer;

		public static Dictionary<OptionType,bool> Options = new Dictionary<OptionType, bool>();
		public static bool Option(OptionType option){
			bool result = false;
			Options.TryGetValue(option,out result);
			return result;
		}
		public static int RandomDirection(){
			int result = R.Roll(8);
			if(result == 5){
				result = 9;
			}
			return result;
		}
		public static string[][] title = new string[][]{new string[]{
				"#######                                ",
				"#######                                ",
				"##    #                                ",
				"##                                     ",
				"##  #                                  ",
				"#####                                  ",
				"#####                                  ",
				"##  #   ###   # ##   ###    #   #   ###",
				"##     #   #  ##    #   #   #   #  #   ",
				"##     #   #  #     #   #    # #    ## ",
				"##     #   #  #     #   #     #       #",
				"##      ###   #      ### ##   #    ### ",
				"                             #         ",
				"                            #          "},
				new string[]{
				"I N T O     N O R R E N D R I N"}
		};
		public static string RomanNumeral(int num){
			string result = "";
			while(num > 1000){
				result = result + "M";
				num -= 1000;
			}
			result = result + RomanPattern(num/100,'C','D','M');
			num -= (num/100)*100;
			result = result + RomanPattern(num/10,'X','L','C');
			num -= (num/10)*10;
			result = result + RomanPattern(num,'I','V','X');
			return result;
		}
		private static string RomanPattern(int num,char one,char five,char ten){
			switch(num){
			case 1:
				return "" + one;
			case 2:
				return "" + one + one;
			case 3:
				return "" + one + one + one;
			case 4:
				return "" + one + five;
			case 5:
				return "" + five;
			case 6:
				return "" + five + one;
			case 7:
				return "" + five + one + one;
			case 8:
				return "" + five + one + one + one;
			case 9:
				return "" + one + ten;
			default: //0
				return "";
			}
		}
		public static string GenerateCharacterName(){
			List<string> vowel = new List<string>{"a","e","i","o","u","ei","a","e","i","o","u","a","e","i","o","u","a","e","i","o","a","e","o"};
			List<string> end_vowel = new List<string>{"a","e","i","o","u","io","ia","a","e","i","o","a","e","i","o","a","e","o","a","e","o"};
			List<string> consonant = new List<string>{"k","s","t","n","h","m","y","r","w","g","d","p","b","f","l","v","z","ch","br","cr","dr","fr","gr","kr","pr","tr","th","sc","sh","sk","sl","sm","sn","sp","st","s","t","n","m","r","g","d","p","b","l","k","s","t","n","m","d","p","b","l"};
			List<string> end_consonant = new List<string>{"k","s","t","n","m","r","g","d","p","b","l","z","ch","th","sh","sk","sp","st","k","s","t","n","m","r","n","d","p","b","l","k","s","t","n","m","r","d","p","l","sk","th","st","d","m","s"};
			string result = "";
			if(R.OneIn(5)){
				if(R.CoinFlip()){
					result = vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + end_consonant.Random();
				}
				else{
					result = vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + end_vowel.Random();
				}
			}
			else{
				if(R.CoinFlip()){
					result = consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + end_consonant.Random();
				}
				else{
					result = consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + end_vowel.Random();
				}
			}
			result = result.Substring(0,1).ToUpper() + result.Substring(1);
			return result;
		}
		public static void CheckForVictory(bool circleDestroyed){
			Map M = Actor.M;
			Actor player = Actor.player; // can't wait to change this setup.
			MessageBuffer B = Actor.B;
			if(M.CurrentLevelType != LevelType.Final) return;
			bool circles = false;
			bool demons = false;
			for(int i=0;i<5;++i){
				Tile circle = M.tile[M.FinalLevelSummoningCircle(i)];
				if(circle.TilesWithinDistance(3).Any(x=>x.type == TileType.DEMONIC_IDOL)){
					circles = true;
					break;
				}
			}
			foreach(Actor a in M.AllActors()){
				if(a.IsFinalLevelDemon() && a.curhp > 0){
					demons = true;
					break;
				}
			}
			if(!circles){
				if(!demons){ //victory
					player.curhp = 100;
					if(circleDestroyed){
						B.Add(Priority.Important,"As the last summoning circle is destroyed, your victory gives you a new surge of strength. ");
					}
					else{
						B.Add(Priority.Important,"As the last demon falls, your victory gives you a new surge of strength. ");
					}
					B.Add(Priority.Important,"Kersai's summoning has been stopped. His cult will no longer threaten the area. ");
					B.Add(Priority.Important,"You begin the journey home to deliver the news. ");
					B.Print(true);
					Global.GAME_OVER = true;
					Global.BOSS_KILLED = true;
					Global.KILLED_BY = "nothing";
				}
				else{
					if(circleDestroyed){
						B.Add(Priority.Important,"The summoning circles have been destroyed, but demons yet remain! ");
					}
				}
			}
		}
		public static void LoadOptions(){
			if(!File.Exists("options.txt")){
				return;
			}
			using(StreamReader file = new StreamReader("options.txt")){
			string s = "";
			while(s.Length < 2 || s.Substring(0,2) != "--"){
				s = file.ReadLine();
				if(s.Length >= 2 && s.Substring(0,2) == "--"){
					break;
				}
				string[] tokens = s.Split(' ');
				if(tokens[0].Length == 1){
					char c = Char.ToUpper(tokens[0][0]);
					if(c == 'F' || c == 'T'){
						OptionType option = (OptionType)(-1);
						bool valid = true;
						try{
							option = (OptionType)Enum.Parse(typeof(OptionType),tokens[1],true);
						}
						catch(ArgumentException){
							valid = false;
						}
						if(valid){
							if(c == 'F'){
								Options[option] = false;
							}
							else{
								Options[option] = true;
							}
						}
					}
				}
			}
			s = "";
			while(s.Length < 2 || s.Substring(0,2) != "--"){
				s = file.ReadLine();
				if(s.Length >= 2 && s.Substring(0,2) == "--"){
					break;
				}
				string[] tokens = s.Split(' ');
				if(tokens[0].Length == 1){
					char c = Char.ToUpper(tokens[0][0]);
					if(c == 'F' || c == 'T'){
						TutorialTopic topic = (TutorialTopic)(-1);
						bool valid = true;
						try{
							topic = (TutorialTopic)Enum.Parse(typeof(TutorialTopic),tokens[1],true);
						}
						catch(ArgumentException){
							valid = false;
						}
						if(valid){
							if(c == 'F' || Global.Option(OptionType.ALWAYS_RESET_TIPS)){
								Help.displayed[topic] = false;
							}
							else{
								Help.displayed[topic] = true;
							}
						}
					}
				}
			}
			}
		}
		public static void SaveOptions(){
			using(StreamWriter file = new StreamWriter("options.txt",false)){
			file.WriteLine("Options:");
			file.WriteLine("Any line that starts with [TtFf] and a space MUST be one of the valid options(or, in the 2nd part, one of the valid tutorial tips):");
			string optionNames = "";
			foreach(OptionType op in Enum.GetValues(typeof(OptionType))){
				if(op != OptionType.DISABLE_GRAPHICS){
					optionNames += op.ToString().ToLower() + " ";
				}
			}
			file.WriteLine(optionNames);
			foreach(OptionType op in Enum.GetValues(typeof(OptionType))){
				if(op != OptionType.DISABLE_GRAPHICS){
					if(Option(op)){
						file.Write("t ");
					}
					else{
						file.Write("f ");
					}
					file.WriteLine(Enum.GetName(typeof(OptionType),op).ToLower());
				}
			}
			file.WriteLine("-- Tracking which tutorial tips have been displayed:");
			foreach(TutorialTopic topic in Enum.GetValues(typeof(TutorialTopic))){
				if(Help.displayed[topic]){
					file.Write("t ");
				}
				else{
					file.Write("f ");
				}
				file.WriteLine(Enum.GetName(typeof(TutorialTopic),topic));
			}
			file.WriteLine("--");
			}
		}
		public delegate int IDMethod(PhysicalObject o);
		public static void SaveGame(MessageBuffer B,Map M,Queue Q){ //games are loaded in Main.cs
			FileStream file = new FileStream("forays.sav",FileMode.CreateNew);
			BinaryWriter b = new BinaryWriter(file);
			Dictionary<PhysicalObject,int> id = new Dictionary<PhysicalObject, int>();
			int next_id = 1;
			IDMethod GetID = delegate(PhysicalObject o){
				if(o == null){
					return 0;
				}
				if(!id.ContainsKey(o)){
					id.Add(o,next_id);
					++next_id;
				}
				return id[o];
			};
			b.Write(Actor.player_name);
			b.Write(M.currentLevelIdx);
			b.Write(M.level_types.Count);
			foreach(LevelType lt in M.level_types){
				b.Write((int)lt);
			}
			b.Write(M.wiz_lite);
			b.Write(M.wiz_dark);
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					b.Write(M.last_seen[i,j].c);
					b.Write((int)M.last_seen[i,j].color);
					b.Write((int)M.last_seen[i,j].bgcolor);
				}
			}
			if(M.CurrentLevelType == LevelType.Final){
				for(int i=0;i<5;++i){
					b.Write(M.final_level_cultist_count[i]);
				}
				b.Write(M.final_level_demon_count);
				b.Write(M.final_level_clock);
			}
			b.Write(Actor.feats_in_order.Count);
			foreach(FeatType ft in Actor.feats_in_order){
				b.Write((int)ft);
			}
			b.Write(Actor.spells_in_order.Count);
			foreach(SpellType sp in Actor.spells_in_order){
				b.Write((int)sp);
			}
			List<List<Actor>> groups = new List<List<Actor>>();
			b.Write(Actor.tiebreakers.Count);
			foreach(Actor a in Actor.tiebreakers){
				if(a == null){
					b.Write(GetID(a));
				}
				else{
					SaveActor(a,b,groups,GetID);
				}
			}
			b.Write(groups.Count);
			foreach(List<Actor> group in groups){
				b.Write(group.Count);
				foreach(Actor a in group){
					b.Write(GetID(a));
				}
			}
			b.Write(M.AllTiles().Count);
			foreach(Tile t in M.AllTiles()){
				b.Write(GetID(t));
				b.Write(t.row);
				b.Write(t.col);
				b.Write(t.name);
				b.Write(t.the_name);
				b.Write(t.a_name);
				b.Write(t.symbol);
				b.Write((int)t.color);
				b.Write(t.light_radius);
				b.Write((int)t.type);
				b.Write(t.passable);
				b.Write(t.GetInternalOpacity());
				b.Write(t.seen);
				b.Write(t.revealed_by_light);
				b.Write(t.solid_rock);
				b.Write(t.light_value);
				b.Write(t.direction_exited);
				if(t.toggles_into.HasValue){
					b.Write(true);
					b.Write((int)t.toggles_into.Value);
				}
				else{
					b.Write(false);
				}
				if(t.inv != null){
					SaveItem(t.inv,b,GetID);
				}
				else{
					b.Write(GetID(null));
				}
				b.Write(t.features.Count);
				foreach(FeatureType f in t.features){
					b.Write((int)f);
				}
			}
			b.Write(Q.turn);
			int num_events = 0;
			foreach(Event e in Q.list){
				if(!e.dead){
					++num_events;
				}
			}
			b.Write(num_events);
			//b.Write(Q.list.Count);
			foreach(Event e in Q.list){
				if(e.dead){
					continue;
				}
				if(e.target is Item && !id.ContainsKey(e.target)){ //in this case, we have an item that isn't on the map or in an inventory, so we need to write all its info.
					b.Write(true);
					SaveItem(e.target as Item,b,GetID);
				}
				else{
					b.Write(false);
					b.Write(GetID(e.target)); //in every other case, the target should already be accounted for.
				}
				if(e.area == null){
					b.Write(0);
				}
				else{
					b.Write(e.area.Count);
					foreach(Tile t in e.area){
						b.Write(GetID(t));
					}
				}
				b.Write(e.delay);
				b.Write((int)e.type);
				b.Write((int)e.attr);
				b.Write((int)e.feature);
				b.Write(e.value);
				b.Write(e.secondary_value);
				b.Write(e.msg);
				if(e.msg_objs == null){
					b.Write(0);
				}
				else{
					b.Write(e.msg_objs.Count);
					foreach(PhysicalObject o in e.msg_objs){
						b.Write(GetID(o));
					}
				}
				b.Write(e.time_created);
				b.Write(e.dead);
				b.Write(e.tiebreaker);
			}
			b.Write(Actor.footsteps.Count);
			foreach(pos p in Actor.footsteps){
				b.Write(p.row);
				b.Write(p.col);
			}
			b.Write(Actor.previous_footsteps.Count);
			foreach(pos p in Actor.previous_footsteps){
				b.Write(p.row);
				b.Write(p.col);
			}
			b.Write(Actor.interrupted_path.row);
			b.Write(Actor.interrupted_path.col);
			b.Write(UI.viewing_commands_idx);
			b.Write(M.feat_gained_this_level);
			b.Write(M.extra_danger);
			b.Write(Item.unIDed_name.Count);
			foreach(ConsumableType ct in Item.unIDed_name.Keys){
				b.Write((int)ct);
				b.Write(Item.unIDed_name[ct]);
			}
			b.Write(Item.identified.d.Count);
			foreach(ConsumableType ct in Item.identified.d.Keys){
				b.Write((int)ct);
				b.Write(Item.identified[ct]);
			}
			b.Write(Item.proto.Keys.Count);
			foreach(ConsumableType ct in Item.proto.Keys){
				b.Write((int)ct);
				b.Write((int)Item.proto[ct].color);
			}
			b.Write(Fire.burning_objects.Count);
			foreach(PhysicalObject o in Fire.burning_objects){
				b.Write(GetID(o));
			}
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					b.Write((int)M.aesthetics[i,j]);
				}
			}
			b.Write(M.dungeonDescription);
			if(M.nextLevelShrines == null){
				b.Write(false);
			}
			else{
				b.Write(true);
				b.Write(M.nextLevelShrines.Count);
				foreach(SchismDungeonGenerator.CellType ct in M.nextLevelShrines){
					b.Write((int)ct);
				}
			}
			for(int i=0;i<5;++i){
				b.Write(M.shrinesFound[i]);
			}
			b.Write(Tile.spellbooks_generated);
			b.Write(UI.viewing_commands_idx);
			int num_messages = B.SaveNumMessages();
			b.Write(num_messages);
			string[] messages = B.SaveMessages();
			for(int i=0;i<num_messages;++i){
				b.Write(messages[i]);
			}
			b.Write(B.SavePosition());
			b.Close();
			file.Close();
		}
		private static void SaveActor(Actor a,BinaryWriter b,List<List<Actor>> groups,IDMethod get_id){
			b.Write(get_id(a));
			b.Write(a.row);
			b.Write(a.col);
			b.Write(a.name);
			b.Write(a.the_name);
			b.Write(a.a_name);
			b.Write(a.symbol);
			b.Write((int)a.color);
			b.Write((int)a.type);
			b.Write(a.maxhp);
			b.Write(a.curhp);
			b.Write(a.maxmp);
			b.Write(a.curmp);
			b.Write(a.speed);
			b.Write(a.light_radius);
			b.Write(get_id(a.target));
			b.Write(a.inv.Count);
			foreach(Item i in a.inv){
				SaveItem(i,b,get_id);
			}
			b.Write(a.attrs.d.Count);
			foreach(AttrType at in a.attrs.d.Keys){
				b.Write((int)at);
				b.Write(a.attrs[at]);
			}
			b.Write(a.skills.d.Count);
			foreach(SkillType st in a.skills.d.Keys){
				b.Write((int)st);
				b.Write(a.skills[st]);
			}
			b.Write(a.feats.d.Count);
			foreach(FeatType ft in a.feats.d.Keys){
				b.Write((int)ft);
				b.Write(a.feats[ft]);
			}
			b.Write(a.spells.d.Count);
			foreach(SpellType sp in a.spells.d.Keys){
				b.Write((int)sp);
				b.Write(a.spells[sp]);
			}
			b.Write(a.exhaustion);
			b.Write(a.time_of_last_action);
			b.Write(a.recover_time);
			b.Write(a.path.Count);
			foreach(pos p in a.path){
				b.Write(p.row);
				b.Write(p.col);
			}
			b.Write(get_id(a.target_location));
			b.Write(a.player_visibility_duration);
			if(a.group != null && groups != null){
				groups.AddUnique(a.group);
			}
			b.Write(a.weapons.Count);
			foreach(Weapon w in a.weapons){
				b.Write((int)w.type);
				b.Write((int)w.enchantment);
				b.Write(w.status.d.Count);
				foreach(EquipmentStatus st in w.status.d.Keys){
					b.Write((int)st);
					b.Write(w.status[st]);
				}
			}
			b.Write(a.armors.Count);
			foreach(Armor ar in a.armors){
				b.Write((int)ar.type);
				b.Write((int)ar.enchantment);
				b.Write(ar.status.d.Count);
				foreach(EquipmentStatus st in ar.status.d.Keys){
					b.Write((int)st);
					b.Write(ar.status[st]);
				}
			}
			b.Write(a.magic_trinkets.Count);
			foreach(MagicTrinketType m in a.magic_trinkets){
				b.Write((int)m);
			}
		}
		private static void SaveItem(Item i,BinaryWriter b,IDMethod get_id){
			b.Write(get_id(i));
			b.Write(i.row);
			b.Write(i.col);
			b.Write(i.name);
			b.Write(i.the_name);
			b.Write(i.a_name);
			b.Write(i.symbol);
			b.Write((int)i.color);
			b.Write(i.light_radius);
			b.Write((int)i.type);
			b.Write(i.quantity);
			b.Write(i.charges);
			b.Write(i.other_data);
			b.Write(i.ignored);
			b.Write(i.do_not_stack);
			b.Write(i.revealed_by_light);
		}
		public static void Quit(){
			if(LINUX && !Screen.GLMode){
				Screen.Blank();
				Screen.ResetColors();
				Screen.SetCursorPosition(0,0);
				Screen.CursorVisible = true;
			}
			Environment.Exit(0);
		}
	}
	public static class Extensions{
		public static List<string> GetWordWrappedList(this string s,int max_length,bool match_length_exactly){ //max_length MUST be longer than any single word in the string
			List<string> result = new List<string>();
			while(s.Length > max_length){
				for(int i=max_length;i>=0;--i){
					if(s[i] == ' '){
						if(match_length_exactly){
							result.Add(s.Substring(0,i).PadRight(max_length));
						}
						else{
							result.Add(s.Substring(0,i));
						}
						s = s.Substring(i+1);
						break;
					}
				}
			}
			if(match_length_exactly){
				result.Add(s.PadRight(max_length));
			}
			else{
				result.Add(s);
			}
			return result;
		}
		public static string ConcatenateListWithCommas(this List<string> ls){
			//"one" returns "one"
			//"one" "two" returns "one and two"
			//"one" "two" "three" returns "one, two, and three", and so on
			if(ls.Count == 1){
				return ls[0];
			}
			if(ls.Count == 2){
				return ls[0] + " and " + ls[1];
			}
			if(ls.Count > 2){
				string result = "";
				for(int i=0;i<ls.Count;++i){
					if(i == ls.Count - 1){
						result = result + "and " + ls[i];
					}
					else{
						result = result + ls[i] + ", ";
					}
				}
				return result;
			}
			return "";
		}
		public static string PadToMapSize(this string s){
			return s.PadRight(Global.COLS);
		}
		public static int LastSpaceBeforeWrap(this colorstring cs,int wrap_index){
			string s = "";
			foreach(cstr c in cs.strings){
				s += c.s;
			}
			return s.LastSpaceBeforeWrap(wrap_index);
		}
		public static int LastSpaceBeforeWrap(this string s,int wrap_index){
			if(wrap_index < 0 || wrap_index >= s.Length) return -1;
			for(int n=wrap_index;n>=0;--n){
				if(s[n] == ' '){
					return n;
				}
			}
			return -1;
		}
		public static colorstring GetColorString(this string s){ return GetColorString(s,Color.Gray,Color.Cyan,Color.Black); }
		public static colorstring GetColorString(this string s,Color text_color){ return GetColorString(s,text_color,Color.Cyan,Color.Black); }
		public static colorstring GetColorString(this string s,Color text_color,Color key_color){ return GetColorString(s,text_color,key_color,Color.Black); }
		public static colorstring GetColorString(this string s,Color text_color,Color key_color,Color bg_color){
			if(s.Contains("[")){
				string temp = s;
				colorstring result = new colorstring();
				while(temp.Contains("[")){
					int open = temp.IndexOf('[');
					int close = temp.IndexOf(']');
					if(close == -1){
						result.strings.Add(new cstr(temp,text_color,bg_color));
						temp = "";
					}
					else{
						int hyphen = temp.IndexOf('-');
						if(hyphen != -1 && hyphen > open && hyphen < close){
							result.strings.Add(new cstr(temp.Substring(0,open+1),text_color,bg_color));
							//result.strings.Add(new cstr(temp.Substring(open+1,(close-open)-1),Color.Cyan));
							result.strings.Add(new cstr(temp.Substring(open+1,(hyphen-open)-1),key_color,bg_color));
							result.strings.Add(new cstr("-",text_color,bg_color));
							result.strings.Add(new cstr(temp.Substring(hyphen+1,(close-hyphen)-1),key_color,bg_color));
							result.strings.Add(new cstr("]",text_color,bg_color));
							temp = temp.Substring(close+1);
						}
						else{
							result.strings.Add(new cstr(temp.Substring(0,open+1),text_color,bg_color));
							result.strings.Add(new cstr(temp.Substring(open+1,(close-open)-1),key_color,bg_color));
							result.strings.Add(new cstr("]",text_color,bg_color));
							temp = temp.Substring(close+1);
						}
					}
				}
				if(temp != ""){
					result.strings.Add(new cstr(temp,text_color,bg_color));
				}
				return result;
			}
			else{
				return new colorstring(s,text_color,bg_color);
			}
		}
		public static List<colorstring> GetColorStrings(this List<string> l){
			List<colorstring> result = new List<colorstring>();
			foreach(string s in l){
				result.Add(s.GetColorString());
			}
			return result;
		}
		public static void AddWithWrap(this List<colorstring> l,colorstring cs,int wrap_width,int indent = 0){
			colorstring last = l.LastOrDefault();
			colorstring remainder = null;
			if(last == null){
				remainder = cs;
			}
			else{
				if(last.Length() + cs.Length() <= wrap_width){
					foreach(cstr s in cs.strings){
						last.strings.Add(s);
					}
				}
				else{
					int split_idx = cs.LastSpaceBeforeWrap(wrap_width - last.Length());
					if(split_idx == -1){
						remainder = cs;
					}
					else{
						var split = cs.SplitAt(split_idx,true);
						foreach(cstr s in split[0].strings){
							last.strings.Add(s);
						}
						remainder = split[1];
					}
				}
			}
			while(remainder?.Length() > 0){
				if(indent + remainder.Length() <= wrap_width){
					l.Add(new colorstring("".PadRight(indent)) + remainder);
					break;
				}
				else{
					colorstring next = new colorstring("".PadRight(indent));
					l.Add(next);
					int split_idx = remainder.LastSpaceBeforeWrap(wrap_width - indent);
					bool remove_space = true;
					if(split_idx == -1){ // if there's nowhere better, just cut it off at the end of the line.
						split_idx = wrap_width - indent;
						remove_space = false;
					}
					var split = remainder.SplitAt(split_idx,remove_space);
					foreach(cstr s in split[0].strings){
						next.strings.Add(s);
					}
					remainder = split[1];
				}
			}
		}
		public static List<Tile> ToFirstSolidTile(this List<Tile> line){
			List<Tile> result = new List<Tile>();
			foreach(Tile t in line){
				result.Add(t);
				if(!t.passable){
					break;
				}
			}
			return result;
		}
		public static List<Tile> ToFirstSolidTileOrActor(this List<Tile> line){
			List<Tile> result = new List<Tile>();
			int idx = 0;
			foreach(Tile t in line){
				result.Add(t);
				if(idx != 0){ //skip the first, as it is assumed to be the origin
					if(!t.passable || t.actor() != null){
						break;
					}
				}
				++idx;
			}
			return result;
		}
		public static List<Tile> To(this List<Tile> line,PhysicalObject o){
			if(o == null){
				return new List<Tile>(line);
			}
			List<Tile> result = new List<Tile>();
			foreach(Tile t in line){
				result.Add(t);
				if(o.row == t.row && o.col == t.col){
					break;
				}
			}
			return result;
		}
		public static List<Tile> From(this List<Tile> line,PhysicalObject o){
			List<Tile> result = new List<Tile>();
			bool found = false;
			foreach(Tile t in line){
				if(o.row == t.row && o.col == t.col){
					found = true;
				}
				if(found){
					result.Add(t);
				}
			}
			return result;
		}
		public static List<T> ToCount<T>(this List<T> line,int count){
			if(line.Count <= count || count < 0){
				return new List<T>(line);
			}
			List<T> result = new List<T>();
			for(int i=0;i<count;++i){
				result.Add(line[i]);
			}
			return result;
		}
		public static List<T> FromCount<T>(this List<T> line,int count){ //note that ToCount(x) and FromCount(x) will both include the element at x.
			if(count <= 1){
				return new List<T>(line);
			}
			List<T> result = new List<T>();
			int total = line.Count;
			for(int i=count-1;i<total;++i){
				result.Add(line[i]);
			}
			return result;
		}
		public static Tile LastBeforeSolidTile(this List<Tile> line){
			Tile result = null;
			foreach(Tile t in line){
				if(!t.passable){
					break;
				}
				else{
					result = t;
				}
			}
			return result;
		}
		public static void StopAtBlockingTerrain(this List<pos> path){
			int i = 0;
			foreach(pos p in path){
				if(Actor.M.tile[p].BlocksConnectivityOfMap(false)){
					break;
				}
				++i;
			}
			if(i < path.Count){
				path.RemoveRange(i,path.Count - i);
			}
		}
		public static void StopAtUnseenTerrain(this List<pos> path){
			int i = 0;
			foreach(pos p in path){
				if(!Actor.M.tile[p].seen){
					break;
				}
				++i;
			}
			if(i < path.Count){
				path.RemoveRange(i,path.Count - i);
			}
		}
		/*public static List<pos> SharedNeighbors(this pos p,pos other,bool return_origins_if_adjacent){
			List<pos> result = p.PositionsWithinDistance(1,!return_origins_if_adjacent);
			List<pos> others = other.PositionsWithinDistance(1,!return_origins_if_adjacent);
			result.RemoveWhere(x=>!others.Contains(x));
			return result;
		}*/
		public static float[] GetFloatValues(this Color color){
			Color4 c = Colors.ConvertColor(color);
			return new float[]{c.R,c.G,c.B,c.A};
		}
		public static float[] GetFloatValues(this Color4 c){
			return new float[]{c.R,c.G,c.B,c.A};
		}
		public static SkillType GetAssociatedSkill(this TileType t){
			switch(t){
			case TileType.COMBAT_SHRINE:
			return SkillType.COMBAT;
			case TileType.DEFENSE_SHRINE:
			return SkillType.DEFENSE;
			case TileType.MAGIC_SHRINE:
			return SkillType.MAGIC;
			case TileType.SPIRIT_SHRINE:
			return SkillType.SPIRIT;
			case TileType.STEALTH_SHRINE:
			return SkillType.STEALTH;
			default:
			return SkillType.NO_SKILL;
			}
		}
		public static SkillType GetAssociatedSkill(this SchismDungeonGenerator.CellType t){
			switch(t){
			case SchismDungeonGenerator.CellType.SpecialFeature1:
			return SkillType.COMBAT;
			case SchismDungeonGenerator.CellType.SpecialFeature2:
			return SkillType.DEFENSE;
			case SchismDungeonGenerator.CellType.SpecialFeature3:
			return SkillType.MAGIC;
			case SchismDungeonGenerator.CellType.SpecialFeature4:
			return SkillType.SPIRIT;
			case SchismDungeonGenerator.CellType.SpecialFeature5:
			return SkillType.STEALTH;
			default:
			return SkillType.NO_SKILL;
			}
		}
	}
}
