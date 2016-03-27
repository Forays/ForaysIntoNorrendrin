/*Copyright (c) 2015-2016  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
using PosArrays;
using Utilities;
using Nym;
using static Nym.NameElement;
namespace Forays{
	public static class UI{
		private static Actor player{get{ return Actor.player; } }
		private static Map M{get{ return Actor.M; } }
		private static Queue Q{get{ return Actor.Q; } }
		private static MessageBuffer B{get{ return Actor.B; } }
		private static int ROWS{ get{ return Global.ROWS; } }
		private static int COLS{ get{ return Global.COLS; } }

		public static bool status_hover = false;
		private static pos internal_map_cursor = new pos(-1,-1);
		public static pos MapCursor{
			get{
				return internal_map_cursor;
			}
			set{
				if(!internal_map_cursor.Equals(value)){
					internal_map_cursor = value;
					status_hover = false;
					DisplayStatusBarObjects();
				}
			}
		}
		public static void SetMapCursor(pos p,bool from_status_bar){
			if(!internal_map_cursor.Equals(p)){
				internal_map_cursor = p;
				status_hover = from_status_bar;
				DisplayStatusBarObjects();
			}
		}

		public static int viewing_commands_idx = 0; //used in DisplayStats to show extra commands
		public static bool viewing_map_shrine_info = false; //used in DisplayStats to show map info

		public static List<PhysicalObject> sidebar_objects = new List<PhysicalObject>();

		public static readonly AttrType[] displayed_statuses = new AttrType[]{AttrType.LIFESPAN,
			
			AttrType.BURNING,AttrType.POISONED,AttrType.ACIDIFIED,AttrType.BLEEDING, //damage over time

			AttrType.VULNERABLE,AttrType.SUSCEPTIBLE_TO_CRITS,AttrType.SWITCHING_ARMOR,AttrType.CHILLED, //extra damage opportunities

			AttrType.PARALYZED,AttrType.ASLEEP,AttrType.STUNNED,AttrType.BLIND,AttrType.CONFUSED,AttrType.ENRAGED,AttrType.SLOWED,AttrType.AMNESIA_STUN,
			AttrType.DIM_VISION,AttrType.IMMOBILE,AttrType.AGGRAVATING,AttrType.POPPY_COUNTER,AttrType.GRABBED,AttrType.GRABBING,
			AttrType.LIGHT_SENSITIVE, //'typical' statuses

			AttrType.TELEPORTING,AttrType.SLIMED,AttrType.OIL_COVERED,AttrType.SHINING,AttrType.ROOTS,AttrType.PSEUDO_VAMPIRIC,AttrType.STONEFORM,
			AttrType.SILENCED,AttrType.SILENCE_AURA,AttrType.FROZEN, //'neutral' statuses

			AttrType.INVULNERABLE,AttrType.MECHANICAL_SHIELD,AttrType.SHIELDED,AttrType.REGENERATING,AttrType.BANDAGED,AttrType.RESTING, //shields and healing

			AttrType.INVISIBLE,AttrType.SHADOW_CLOAK, //visibility modifiers

			AttrType.FLYING,AttrType.FLYING_LEAP,AttrType.VIGOR,AttrType.BLOOD_BOILED, //movement

			AttrType.SHADOWSIGHT,AttrType.MYSTIC_MIND,AttrType.DETECTING_MOVEMENT,AttrType.DETECTING_MONSTERS, //detection and senses

			AttrType.BRUTISH_STRENGTH,AttrType.RADIANT_HALO,AttrType.EMPOWERED_SPELLS,AttrType.CONVICTION, //other positive effects
			};

		public static void DisplayStats(){
			bool buttons = MouseUI.AutomaticButtonsFromStrings;
			MouseUI.AutomaticButtonsFromStrings = false;
			bool commands_darkened = MouseUI.Mode != MouseMode.Map;
			int row = 0;
			if(!viewing_map_shrine_info){
				string s = ("Health: " + player.curhp.ToString()).PadOuter(Global.STATUS_WIDTH);
				int idx = GetStatusBarIndex(player.curhp,player.maxhp);
				StatusWriteString(ref row,new colorstring(new cstr(s.SafeSubstring(0,idx),Color.Gray,Color.DarkRed),new cstr(s.SafeSubstring(idx),Color.Gray,Color.Black)));
				if(player.maxmp > 0){
					s = ("Mana: " + player.curmp.ToString()).PadOuter(Global.STATUS_WIDTH);
					idx = GetStatusBarIndex(player.curmp,player.maxmp);
					StatusWriteString(ref row,new colorstring(new cstr(s.SafeSubstring(0,idx),Color.Gray,Color.DarkCyan),new cstr(s.SafeSubstring(idx),Color.Gray,Color.Black)));
				}
				if(player.exhaustion > 0){
					s = ("Exhaustion: " + player.exhaustion.ToString() + "%").PadOuter(Global.STATUS_WIDTH);
					idx = GetStatusBarIndex(player.exhaustion,100);
					StatusWriteString(ref row,new colorstring(new cstr(s.SafeSubstring(0,idx),Color.Gray,Color.DarkYellow),new cstr(s.SafeSubstring(idx),Color.Gray,Color.Black)));
				}
				Dictionary<AttrType,Event> events = Q.StatusEvents.ContainsKey(player)? Q.StatusEvents[player] : null;
				foreach(AttrType attr in displayed_statuses){
					if(player.HasAttr(attr) && !attr.StatusIsHidden(player)){
						int value = 1;
						int max = 1; // If no other data is found, a full bar (1/1) will be shown.
						if(!attr.StatusByStrength(player,(events != null && events.ContainsKey(attr))? events[attr] : null,ref value,ref max)){
							if(events != null && events.ContainsKey(attr)){
								Event e = events[attr];
								value = e.delay + e.time_created + 100 - Q.turn;
								max = e.delay;
							}
						}
						int attr_idx = UI.GetStatusBarIndex(value,max);
						string attr_name = attr.StatusName(player).PadOuter(Global.STATUS_WIDTH);
						StatusWriteString(ref row,new colorstring(new cstr(attr_name.SafeSubstring(0,attr_idx),Color.Gray,Color.DarkMagenta),new cstr(attr_name.SafeSubstring(attr_idx),Color.Gray)));
					}
				}
				if(player.tile().Is(FeatureType.WEB) && !player.HasAttr(AttrType.BURNING)){
					string webbed = "Webbed";
					if(player.HasAttr(AttrType.SLIMED,AttrType.OIL_COVERED)){
						webbed = "In a web";
					}
					StatusWriteString(ref row,new colorstring(webbed.PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkMagenta));
				}
				if(player.IsSilencedHere() && !player.HasAttr(AttrType.SILENCED,AttrType.SILENCE_AURA)){
					StatusWriteString(ref row,new colorstring(AttrType.SILENCED.StatusName(player).PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkMagenta));
				}
				equipment_row = row;
				StatusWriteString(ref row,player.EquippedWeapon.NameOnStatusBar());
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(player.EquippedWeapon.status[(EquipmentStatus)i]){
						StatusWriteString(ref row,new colorstring(Weapon.StatusName((EquipmentStatus)i).PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkBlue));
					}
				}
				StatusWriteString(ref row,player.EquippedArmor.NameOnStatusBar());
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(player.EquippedArmor.status[(EquipmentStatus)i]){
						StatusWriteString(ref row,new colorstring(Weapon.StatusName((EquipmentStatus)i).PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkBlue));
					}
				}
				depth_row = row;
				StatusWriteString(ref row,$"Depth: {M.Depth}".PadOuter(Global.STATUS_WIDTH));
				if(M.wiz_dark || M.wiz_lite){
					Event e = Q.LightingEvent;
					if(e != null){
						int value = e.delay + e.time_created + 100 - Q.turn;
						int max = e.delay;
						int e_idx = UI.GetStatusBarIndex(value,max);
						string e_name = (M.wiz_dark? "Darkness" : "Sunlight").PadOuter(Global.STATUS_WIDTH);
						StatusWriteString(ref row,new colorstring(new cstr(e_name.SafeSubstring(0,e_idx),Color.Gray,Color.DarkBlue),new cstr(e_name.SafeSubstring(e_idx),Color.Gray)));
					}
				}
				StatusWriteString(ref row,"".PadRight(Global.STATUS_WIDTH));
				status_row_start = row;
				MouseUI.CreatePlayerStatsButtons();
				DisplayStatusBarObjects();
			}
			else{
				for(int i=0;i<UI.status_row_cutoff;++i){
					Screen.WriteStatsString(i,0,"".PadRight(Global.STATUS_WIDTH));
				}
				const int distFromTop = 5;
				Screen.WriteStatsString(distFromTop,6,"Shrines:",Color.Yellow);
				for(int i=0;i<5;++i){
					string name = Skill.Name(i) + ":";
					string status;
					Color color;
					switch(M.shrinesFound[i]){
					case 1:
					status = "Found";
					color = Color.Gray;
					break;
					case 2:
					status = "Missed";
					color = Color.Red;
					break;
					case 3:
					status = "Depleted";
					color = Color.DarkGreen;
					break;
					case 4:
					status = "Used";
					color = Color.Cyan;
					break;
					case 0:
					default:
					status = "Not found";
					color = Color.DarkGray;
					break;
					}
					Screen.WriteStatsString(distFromTop + 2 + i*2,0,name.PadBetween(status,Global.STATUS_WIDTH),color);
					Screen.WriteStatsString(distFromTop + 2 + i*2,0,name);
				}
			}
			string[] commandhints = null;
			List<int> blocked_commands = new List<int>();
			switch(viewing_commands_idx){
			case 0:
				commandhints = new string[]{
					"Look around [Tab]   ",
					"[p]revious messages ",
					"[a]pply item        ",
					"[g]et item          ",
					"[f]ling item        ",
					"Wait a turn [.]     ",
					"Help [?]            ",
					"[v]iew more         ",
				};
			break;
			case 1:
				commandhints = new string[]{
					"View known items [\\]",
					"[o]perate something ",
					"Travel somewhere [X]",
					"Descend stairs [>]  ",
					"[w]alk              ",
					"Options [=]         ",
					"[q]uit or save      ",
					"[v]iew more         ",
				};
			break;
			case 2:
			if(Global.Option(OptionType.HIDE_VIEW_MORE)){
				commandhints = new string[]{};
			}
			else{
				commandhints = new string[]{
					"[v]iew more         ",
				};
			}
			break;
			}
			Color wordcolor = commands_darkened? Color.DarkGray : Color.Gray;
			Color lettercolor = commands_darkened? Color.DarkCyan : Color.Cyan;
			for(int i=0;i<commandhints.Length;++i){
				if(blocked_commands.Contains(i)){
					Screen.WriteString(status_row_cutoff+i+1,0,commandhints[i].GetColorString(Color.DarkGray,Color.DarkCyan));
				}
				else{
					Screen.WriteString(status_row_cutoff+i+1,0,commandhints[i].GetColorString(wordcolor,lettercolor));
				}
			}
			if(draw_bottom_commands){
				Screen.WriteString(Global.SCREEN_H - 3,Global.MAP_OFFSET_COLS,UI.GetEnvironmentalDescription().PadRight(Global.COLS),commands_darkened? Color.DarkEnvironmentDescription : Color.EnvironmentDescription,Color.Black);
				Screen.WriteString(Global.SCREEN_H - 2,Global.MAP_OFFSET_COLS,"E[x]plore     [t]orch     [s]hoot bow    [r]est     Cast spell [z]".GetColorString(wordcolor,lettercolor));
				if(Screen.GLMode){
					Screen.WriteString(Global.SCREEN_H - 1,Global.MAP_OFFSET_COLS,"[i]nventory   [e]quipment [c]haracter    [m]ap              [Menu]".GetColorString(wordcolor,lettercolor));
				}
				else{
					Screen.WriteString(Global.SCREEN_H - 1,Global.MAP_OFFSET_COLS,"[i]nventory   [e]quipment [c]haracter    [m]ap                    ".GetColorString(wordcolor,lettercolor));
				}
			}
			Screen.ResetColors();
			MouseUI.AutomaticButtonsFromStrings = buttons;
		}
		public static void StatusWriteString(ref int row,colorstring s){
			if(row >= status_row_cutoff){
				return;
			}
			Screen.WriteString(row,0,s);
			++row;
		}
		public static bool draw_bottom_commands = true;
		public static bool darken_status_bar = false;
		public static int status_row_start = 5;
		public static int equipment_row = 1;
		public static int depth_row = 3;
		public static int status_row_cutoff = Global.SCREEN_H - 9;
		public static void DisplayStatusBarObjects(){
			if(UI.viewing_map_shrine_info) return;
			int row = status_row_start;
			List<PhysicalObject> objs = null;
			List<List<colorstring>> names = null;
			List<PhysicalObject> extra_objs = null; // objects under MapCursor that should be displayed but weren't visible on the first pass.
			bool extras_found = false;
			do{
				row = status_row_start;
				extras_found = false;
				objs = new List<PhysicalObject>();
				names = new List<List<colorstring>>();
				bool screen_too_small = false;
				if(extra_objs != null){
					SortStatusBarObjects(extra_objs);
					foreach(PhysicalObject o in extra_objs){
						var strings = o.GetStatusBarInfo();
						if(row + strings.Count > status_row_cutoff){
							screen_too_small = true;
							break;
						}
						objs.Add(o);
						names.Add(strings);
						row += strings.Count + 1;
					}
				}
				if(screen_too_small){
					break;
				}
				bool overflow = false;
				foreach(PhysicalObject o in UI.sidebar_objects){
					var strings = o.GetStatusBarInfo();
					if(overflow || row + strings.Count > status_row_cutoff){
						overflow = true; //Once the limit has been exceeded once, don't put any more objects in the list, even if later ones would fit.
						if(status_hover){ //When hovering over the status bar, don't rearrange anything.
							break;
						}
						else{
							if(o.p.Equals(UI.MapCursor) && (extra_objs == null || !extra_objs.Contains(o)) && (MouseUI.Mode != MouseMode.Targeting || !o.p.Equals(player.p))){
								if(extra_objs == null){
									extra_objs = new List<PhysicalObject>();
								}
								extras_found = true;
								extra_objs.Add(o); // If another object is found here, it needs to be added to the top of the list.
							}
							continue;
						}
					}
					objs.Add(o);
					names.Add(strings);
					row += strings.Count + 1;
				}
			}
			while(extras_found);
			row = status_row_start;
			for(int i=0;i<objs.Count;++i){
				PhysicalObject o = objs[i];
				foreach(colorstring cs2 in names[i]){
					Screen.WriteString(row,0,cs2);
					if(extra_objs == null || !extra_objs.Contains(o)){
						for(int j=0;j<Global.STATUS_WIDTH;++j){
							MouseUI.mouselook_objects[row,j] = o;
						}
					}
					++row;
				}
				Screen.WriteString(row,0,"".PadRight(Global.STATUS_WIDTH));
				for(int j=0;j<Global.STATUS_WIDTH;++j){
					MouseUI.mouselook_objects[row,j] = null;
				}
				++row;
			}
			while(row <= status_row_cutoff){
				Screen.WriteString(row,0,"".PadRight(Global.STATUS_WIDTH));
				for(int j=0;j<Global.STATUS_WIDTH;++j){
					MouseUI.mouselook_objects[row,j] = null;
				}
				++row;
			}
		}
		public static int GetStatusBarIndex(int value,int max){
			if(max == 0) return 0;
			int adjustment = Math.Max(0,max - Global.STATUS_WIDTH); // The adjustment prevents bars from looking empty until they're at 0.
			int result = (Global.STATUS_WIDTH*value + adjustment) / max;
			if(result < 0) return 0;
			if(result > Global.STATUS_WIDTH) return Global.STATUS_WIDTH;
			return result;
		}
		public static string StatusName(this AttrType attr,Actor a){
			switch(attr){
			case AttrType.ROOTS:
			return "Rooted";
			case AttrType.BLIND:
			return "Blinded";
			case AttrType.SUSCEPTIBLE_TO_CRITS:
			return "Off-balance";
			case AttrType.PSEUDO_VAMPIRIC:
			return "Vampiric";
			case AttrType.BLOOD_BOILED:
			if(a != null && a.attrs[attr] > 1){
				return "Boiling blood " + a.attrs[attr].ToString();
			}
			return "Boiling blood";
			case AttrType.VIGOR:
			return "Hasted";
			case AttrType.AMNESIA_STUN:
			return "Amnesiac";
			case AttrType.DIM_VISION:
			return "Dimmed vision";
			case AttrType.SHADOW_CLOAK:
			return "Shadow cloaked";
			case AttrType.EMPOWERED_SPELLS:
			return "Empowered magic";
			case AttrType.POPPY_COUNTER:
			return "Breathing poppies";
			case AttrType.REGENERATING:
			if(a != null && a.attrs[attr] > 1){
				return "Regenerating " + a.attrs[attr].ToString();
			}
			break;
			case AttrType.CHILLED:
			if(a != null && a.attrs[attr] > 1){
				return "Chilled " + a.attrs[attr].ToString();
			}
			break;
			case AttrType.CONVICTION:
			if(a != null && a.attrs[attr] > 1){
				return "Conviction " + a.attrs[attr].ToString();
			}
			break;
			case AttrType.DETECTING_MONSTERS:
			if(a != null && a.type == ActorType.ROBED_ZEALOT){
				return "Praying";
			}
			break;
			case AttrType.IMMOBILE:
			if(a != null){
				Actor proto = Actor.Prototype(a.type);
				if(a == player || (proto != null && !proto.HasAttr(AttrType.IMMOBILE))){
					return "Immobilized";
				}
			}
			break;
			case AttrType.LIGHT_SENSITIVE:
			return "Allergic to light";
			}
			return attr.ToString().ToLower().Capitalize().Replace('_',' ');
		}
		public static bool StatusIsHidden(this AttrType attr,Actor a){
			switch(attr){
			case AttrType.FLYING:
			return a.HasAttr(AttrType.FLYING_LEAP,AttrType.PSEUDO_VAMPIRIC,AttrType.DESCENDING);
			case AttrType.IMMUNE_BURNING:
			return a.HasAttr(AttrType.STONEFORM);
			case AttrType.NONLIVING:
			return a.HasAttr(AttrType.STONEFORM);
			case AttrType.IMMOBILE:
			//return a.HasAttr(AttrType.ROOTS) || Actor.Prototype(a.type).HasAttr(AttrType.IMMOBILE);
			return a.HasAttr(AttrType.ROOTS);
			case AttrType.SILENCED:
			return a.HasAttr(AttrType.SILENCE_AURA);
			case AttrType.DETECTING_MONSTERS:
			return a.HasAttr(AttrType.MYSTIC_MIND);
			case AttrType.MENTAL_IMMUNITY:
			return a.HasAttr(AttrType.MYSTIC_MIND);
			case AttrType.LIGHT_SENSITIVE:
			return (a != player || a.HasAttr(AttrType.PSEUDO_VAMPIRIC));
			default:
			return false;
			}
		}
		public static bool StatusByStrength(this AttrType attr,Actor a,Event e,ref int value,ref int max){
			switch(attr){
			case AttrType.FROZEN:
			max = 35;
			break;
			case AttrType.POPPY_COUNTER:
			max = 4;
			break;
			case AttrType.RESTING:
			max = 10;
			break;
			case AttrType.BLIND:
			if(a.type == ActorType.DARKNESS_DWELLER && e != null && e.delay == 100){
				value = 8 - a.attrs[AttrType.COOLDOWN_1];
				max = 7;
				return true;
			}
			else{
				return false;
			}
			case AttrType.LIFESPAN:
			max = Actor.Prototype(a.type).attrs[AttrType.LIFESPAN];
			break;
			case AttrType.AMNESIA_STUN:
			max = 6;
			break;
			case AttrType.BLEEDING:
			max = 25;
			break;
			case AttrType.BANDAGED:
			max = 20;
			break;
			case AttrType.ASLEEP:
			max = 5;
			break;
			case AttrType.FLYING_LEAP:
			if(e != null){
				value = e.delay + e.time_created + 50 - Q.turn;
				max = e.delay - 50;
				return true;
			}
			break;
			case AttrType.DETECTING_MONSTERS:
			if(a != null && a.type == ActorType.ROBED_ZEALOT){
				max = 4;
			}
			else{
				return false;
			}
			break;
			default:
			return false;
			}
			value = a.attrs[attr];
			return true;
		}
		public static void SortStatusBarObjects(){ SortStatusBarObjects(sidebar_objects); }
		private static void SortStatusBarObjects(List<PhysicalObject> l){
			//in order of priority:
			//actors
			//features (grenades, troll corpses, bones)
			//items
			//terrain
			l.Sort((o1,o2)=>{
				int n1 = 1;
				if(o1 is Item){
					n1 = 2;
				}
				else{
					if(o1 is Actor){
						if(!player.CanSee(M.tile[o1.p])){
							n1 = 8;
						}
						else{
							n1 = 0;
						}
					}
					else{
						if(o1 is Tile){
							n1 = 4;
						}
					}
				}
				int n2 = 1;
				if(o2 is Item){
					n2 = 2;
				}
				else{
					if(o2 is Actor){
						if(!player.CanSee(M.tile[o2.p])){
							n2 = 8;
						}
						else{
							n2 = 0;
						}
					}
					else{
						if(o2 is Tile){
							n2 = 4;
						}
					}
				}
				int result = n1.CompareTo(n2);
				if(result == 0){
					result = o1.DistanceFrom(player).CompareTo(o2.DistanceFrom(player));
					if(result == 0){
						return o1.ApproximateEuclideanDistanceFromX10(player).CompareTo(o2.ApproximateEuclideanDistanceFromX10(player));
					}
					return result;
				}
				return result;
			});
		}
		public static List<colorstring> ItemDescriptionBox(Item item,bool lookmode,bool mouselook,int max_string_length){
			List<string> text = item.Description().GetWordWrappedList(max_string_length,false);
			Color box_edge_color = Color.DarkGreen;
			Color box_corner_color = Color.Green;
			Color text_color = Color.Gray;
			int widest = 31; // length of "[Press any other key to cancel]"
			if(lookmode){
				widest = 20; // length of "[=] Hide description"
			}
			foreach(string s in text){
				if(s.Length > widest){
					widest = s.Length;
				}
			}
			if((!lookmode || mouselook) && item.GetName(true,Extra).Length > widest){
				widest = item.GetName(true,Extra).Length;
			}
			widest += 2; //one space on each side
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			if(!lookmode || mouselook){
				box.Add(new colorstring("|",box_edge_color) + item.GetName(true,Extra).PadOuter(widest).GetColorString(Color.White) + new colorstring("|",box_edge_color));
				box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
			}
			foreach(string s in text){
				box.Add(new colorstring("|",box_edge_color) + s.PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			if(!mouselook){
				box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
				if(lookmode){
					box.Add(new colorstring("|",box_edge_color) + "[=] Hide description".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
				}
				else{
					box.Add(new colorstring("|",box_edge_color) + "[a]pply  [f]ling  [d]rop".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
					//box.Add(new colorstring("|",box_edge_color) + "[Press any other key to cancel]".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
				}
			}
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			return box;
		}
		public static int DisplayCharacterInfo(){ return DisplayCharacterInfo(true); }
		public static int DisplayCharacterInfo(bool readkey){
			MouseUI.PushButtonMap();
			UI.DisplayStats();
			UI.draw_bottom_commands = false;
			UI.darken_status_bar = true;

			const Color c = Color.Green;
			const Color text = Color.Gray;
			List<colorstring> top = new List<colorstring>{new colorstring("".PadRight(COLS,'-'),text)};
			List<colorstring> name = new List<colorstring>{(new cstr("Name",c) + new cstr(": " + Actor.player_name + "  ",text)).PadRight(COLS/2) + (new cstr("Turns played",c) + new cstr(": " + Q.turn/100,text))};
			List<colorstring> skills = null;
			List<colorstring> feats = null;
			List<colorstring> spells = null;
			List<colorstring> trinkets = null;
			List<colorstring> divider = new List<colorstring>{new colorstring("Active abilities",c,":",text)};
			List<colorstring> actives = new List<colorstring>();

			List<List<colorstring>> potential_skills = new List<List<colorstring>>();
			for(int indent=7;indent>=1;--indent){
				List<colorstring> list = new List<colorstring>{new colorstring("Skills",c,":",text)};
				potential_skills.Add(list);
				for(SkillType sk = SkillType.COMBAT;sk < SkillType.NUM_SKILLS;++sk){
					int skill_base = player.skills[sk];
					int skill_mod = player.BonusSkill(sk);
					colorstring skill_string = new colorstring(" " + Skill.Name(sk) + "(",text,skill_base.ToString(),Color.White);
					if(skill_mod > 0){
						skill_string.Add($"+{skill_mod}",Color.Yellow);
					}
					else{
						if(skill_mod < 0){
							skill_string.strings.Add(new cstr(skill_mod.ToString(),Color.Blue));
						}
					}
					skill_string.strings.Add(new cstr(")",text));
					list.AddWithWrap(skill_string,Global.COLS,indent);
				}
			}
			skills = potential_skills.WhereLeast(x=>x.Count)[0]; //Take the first result (i.e. highest indent) from the lowest count.

			List<List<colorstring>> potential_feats = new List<List<colorstring>>();
			for(int indent=6;indent>=1;--indent){
				List<colorstring> list = new List<colorstring>{new colorstring("Feats",c,":",text)};
				potential_feats.Add(list);
				for(int i=0;i<Actor.feats_in_order.Count;++i){
					string comma = (i == Actor.feats_in_order.Count - 1)? "" : ",";
					colorstring feat_string = new colorstring(" " + Feat.Name(Actor.feats_in_order[i]) + comma);
					list.AddWithWrap(feat_string,Global.COLS,indent);
				}
			}
			feats = potential_feats.WhereLeast(x=>x.Count)[0]; //Take the first result (i.e. highest indent) from the lowest count.

			List<List<colorstring>> potential_spells = new List<List<colorstring>>();
			for(int indent=7;indent>=1;--indent){
				List<colorstring> list = new List<colorstring>{new colorstring("Spells",c,":",text)};
				potential_spells.Add(list);
				for(int i=0;i<Actor.spells_in_order.Count;++i){
					string comma = (i == Actor.spells_in_order.Count - 1)? "" : ",";
					colorstring spell_string = new colorstring(" " + Spell.Name(Actor.spells_in_order[i]) + comma);
					list.AddWithWrap(spell_string,Global.COLS,indent);
				}
			}
			spells = potential_spells.WhereLeast(x=>x.Count)[0]; //Take the first result (i.e. highest indent) from the lowest count.

			List<string> magic_equipment_names = new List<string>();
			foreach(Weapon w in player.weapons){
				if(w.IsEnchanted()){
					magic_equipment_names.Add(w.NameWithEnchantment());
				}
			}
			foreach(Armor a in player.armors){
				if(a.IsEnchanted()){
					magic_equipment_names.Add(a.NameWithEnchantment());
				}
			}
			foreach(MagicTrinketType trinket in player.magic_trinkets){
				magic_equipment_names.Add(MagicTrinket.Name(trinket));
			}

			List<List<colorstring>> potential_trinkets = new List<List<colorstring>>();
			for(int indent=18;indent>=1;--indent){ //todo keep this value?
				List<colorstring> list = new List<colorstring>{new colorstring("Magical equipment",c,":",text)};
				potential_trinkets.Add(list);
				for(int i=0;i<magic_equipment_names.Count;++i){
					string comma = (i == magic_equipment_names.Count - 1)? "" : ",";
					colorstring equip_string = new colorstring(" " + magic_equipment_names[i].Capitalize() + comma);
					list.AddWithWrap(equip_string,Global.COLS,indent);
				}
			}
			trinkets = potential_trinkets.WhereLeast(x=>x.Count)[0]; //Take the first result (i.e. highest indent) from the lowest count.

			List<FeatType> active_feats = new List<FeatType>();

			foreach(FeatType f in Actor.feats_in_order){
				if(Feat.IsActivated(f)){
					active_feats.Add(f);
				}
			}

			for(int i=0;i<active_feats.Count;++i){
				actives.Add(new colorstring("  [",text,((char)(i+'a')).ToString(),Color.Cyan,"] " + Feat.Name(active_feats[i]),text));
			}

			//eventually activated trinkets or temporary effects will go here.

			const int total_height = Global.SCREEN_H - Global.MAP_OFFSET_ROWS;
			List<List<colorstring>> all = new List<List<colorstring>>{top,name,skills,feats,spells,trinkets,divider,actives};
			List<List<colorstring>> some = new List<List<colorstring>>{name,skills,feats,spells};
			List<List<colorstring>> top_titles = new List<List<colorstring>>{name,skills,feats,spells,trinkets};
			int rows_left = total_height - 1; // -1 for the bottom border
			foreach(var list in all){
				rows_left -= list.Count;
			}

			//Here's how the extra rows are distributed:
			if(rows_left >= 1){
				trinkets.Add(new colorstring(""));
				--rows_left;
			}

			if(rows_left >= 1){
				actives.Add(new colorstring(""));
				--rows_left;
			}

			List<List<colorstring>> cramped = new List<List<colorstring>>();
			foreach(var list in some){
				if(list.Count == 1){
					cramped.Add(list); //a list is considered cramped if its title (in green) is right next to another title.
				}
			}
			if(rows_left >= cramped.Count){
				foreach(var list in cramped){
					list.Add(new colorstring(""));
					--rows_left;
				}
			}

			List<List<colorstring>> no_blank_line = new List<List<colorstring>>();
			foreach(var list in some){
				if(list.Last().Length() > 0){
					no_blank_line.Add(list); //this time, we try to put a space between each list and the next title.
				}
			}
			if(rows_left >= no_blank_line.Count){
				foreach(var list in no_blank_line){
					list.Add(new colorstring(""));
					--rows_left;
				}
			}

			/*if(rows_left >= 1){
				divider.Add(new colorstring(""));
				--rows_left;
			}*/

			int top_text_height = 0;
			foreach(var list in top_titles){
				top_text_height += list.Count;
			}
			if(rows_left >= 1 && top_text_height < 14){
				top.Add(new colorstring(""));
				--rows_left;
			}

			for(int i=0;i<rows_left;++i){
				actives.Add(new colorstring(""));
			}

			int row = 0;
			foreach(var list in all){
				foreach(colorstring cs in list){
					Screen.WriteMapString(row,0,cs.PadRight(COLS));
					++row;
				}
			}
			Screen.WriteMapString(row,0,"".PadRight(COLS,'-'),text);
			Screen.ResetColors();
			UI.Display("Character information: ");

			int result = -1;
			if(readkey){
				result = player.GetSelection("Character information: ",active_feats.Count,false,true,false); //todo, currently only feats.
			}
			MouseUI.PopButtonMap();
			UI.draw_bottom_commands = true;
			UI.darken_status_bar = false;
			return result;
		}
		public static int[] DisplayEquipment(){
			MouseUI.PushButtonMap();
			UI.draw_bottom_commands = false;
			UI.darken_status_bar = true;
			Weapon equippedWeapon = player.EquippedWeapon;
			Armor equippedArmor = player.EquippedArmor;
			WeaponType selectedWeapon = equippedWeapon.type;
			ArmorType selectedArmor = equippedArmor.type;
			List<MagicTrinketType> trinkets = player.magic_trinkets;
			int selectedTrinketIdx = -1;
			if(trinkets.Count > 0){
				selectedTrinketIdx = R.Roll(trinkets.Count)-1;
				int i = 0;
				foreach(MagicTrinketType trinket in trinkets){
					MouseUI.CreateButton((ConsoleKey)(ConsoleKey.I + i),false,i+1+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS + 32,1,34);
					++i;
				}
			}
			Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
			for(int i=1;i<ROWS-1;++i){
				Screen.WriteMapString(i,0,"".PadRight(COLS));
			}
			int line = 1;
			for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
				Screen.WriteMapString(line,2,"[ ] " + player.WeaponOfType(w).EquipmentScreenName());
				ConsoleKey key = (ConsoleKey)(ConsoleKey.A + line-1);
				if(w == selectedWeapon){
					key = ConsoleKey.Enter;
				}
				if(trinkets.Count >= line){
					MouseUI.CreateButton(key,false,line+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,1,32);
				}
				else{
					MouseUI.CreateMapButton(key,false,line,1);
				}
				++line;
			}
			line = 8;
			for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
				Screen.WriteMapString(line,2,"[ ] " + player.ArmorOfType(a).EquipmentScreenName());
				ConsoleKey key = (ConsoleKey)(ConsoleKey.A + line-3);
				if(a == selectedArmor){
					key = ConsoleKey.Enter;
				}
				if(trinkets.Count >= line){
					MouseUI.CreateButton(key,false,line+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,1,32);
				}
				else{
					MouseUI.CreateMapButton(key,false,line,1);
				}
				++line;
			}
			line = 1;
			foreach(MagicTrinketType m in trinkets){
				Screen.WriteMapString(line,34,"[ ] " + MagicTrinket.Name(m).Capitalize());
				++line;
			}
			Screen.WriteMapString(11,0,"".PadRight(COLS,'-'));
			ConsoleKeyInfo command;
			bool done = false;
			while(!done){
				line = 1;
				for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
					char c = ' ';
					Color letter_color = Color.Cyan;
					if(equippedWeapon.status[EquipmentStatus.STUCK]){
						letter_color = Color.Red;
					}
					if(selectedWeapon == w){
						c = '>';
						letter_color = Color.Red;
					}
					Screen.WriteMapChar(line,0,c);
					Screen.WriteMapChar(line,3,(char)(w+(int)'a'),letter_color);
					++line;
				}
				line = 8;
				for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
					char c = ' ';
					Color letter_color = Color.Cyan;
					if(equippedArmor.status[EquipmentStatus.STUCK]){
						letter_color = Color.Red;
					}
					if(selectedArmor == a){
						c = '>';
						letter_color = Color.Red;
					}
					Screen.WriteMapChar(line,0,c);
					Screen.WriteMapChar(line,3,(char)(a+(int)'f'),letter_color);
					++line;
				}
				line = 1;
				int letter = 0;
				foreach(MagicTrinketType m in trinkets){
					if(selectedTrinketIdx == trinkets.IndexOf(m)){
						Screen.WriteMapChar(line,32,'>');
					}
					else{
						Screen.WriteMapChar(line,32,' ');
					}
					Screen.WriteMapChar(line,35,(char)(letter+(int)'i'),Color.Red);
					++line;
					++letter;
				}
				Weapon newWeapon = player.WeaponOfType(selectedWeapon);
				Armor newArmor = player.ArmorOfType(selectedArmor);
				MagicTrinketType selectedTrinket = selectedTrinketIdx >= 0? trinkets[selectedTrinketIdx] : MagicTrinketType.NO_MAGIC_TRINKET;
				List<colorstring> wStr = newWeapon.Description().GetColorStrings();
				wStr[0] = new colorstring("Weapon",Color.DarkRed,": ",Color.Gray) + wStr[0];
				List<colorstring> aStr = newArmor.Description().GetColorStrings();
				aStr[0] = new colorstring("Armor",Color.DarkCyan,": ",Color.Gray) + aStr[0];
				{
					string wEnch = newWeapon.DescriptionOfEnchantment();
					string aEnch = newArmor.DescriptionOfEnchantment();
					if(wEnch != ""){
						wStr.Add(new colorstring(wEnch,newWeapon.EnchantmentColor()));
					}
					if(aEnch != ""){
						aStr.Add(new colorstring(aEnch,newArmor.EnchantmentColor()));
					}
				}
				List<colorstring> mStr = MagicTrinket.Description(selectedTrinket).GetColorStrings();
				mStr[0] = new colorstring("Magic trinket",Color.DarkGreen,": ",Color.Gray) + mStr[0];
				if(mStr.Count > 1){
					mStr[1] = "".PadRight(15) + mStr[1];
				}
				List<colorstring> wStatusShort = newWeapon.ShortStatusList();
				List<colorstring> aStatusShort = newArmor.ShortStatusList();

				int wMin = wStr.Count + wStatusShort.Count;
				int aMin = aStr.Count + aStatusShort.Count;

				int lines_free = 12 - wMin - aMin - mStr.Count;

				List<colorstring> wStatusLong = newWeapon.LongStatusList();
				List<colorstring> aStatusLong = newArmor.LongStatusList();
				int wDiff = wStatusLong.Count - wStatusShort.Count;
				int aDiff = aStatusLong.Count - aStatusShort.Count;

				bool expandW = false;
				bool expandA = false;
				if(wDiff + aDiff <= lines_free){ //if there's room to use the full versions of both, do that.
					expandW = true;
					expandA = true;
				}
				else{
					if(wDiff > aDiff){ //otherwise, try to fit the biggest.
						if(wDiff <= lines_free){
							expandW = true;
						}
						else{
							if(aDiff <= lines_free){
								expandA = true;
							} //if that doesn't work, we settle for the condensed version.
						}
					}
					else{
						if(aDiff <= lines_free){
							expandA = true;
						}
						else{
							if(wDiff <= lines_free){
								expandW = true;
							}
						}
					}
				}
				if(expandW){
					wStr.AddRange(wStatusLong);
					lines_free -= wDiff;
				}
				else{
					wStr.AddRange(wStatusShort);
				}
				if(expandA){
					aStr.AddRange(aStatusLong);
					lines_free -= aDiff;
				}
				else{
					aStr.AddRange(aStatusShort);
				}

				List<colorstring> top = new List<colorstring>();

				//now let's distribute those extra lines. (0-7 possible)
				for(int i=0;i<2;++i){ //space between sections, then at the bottom. This takes care of 6 lines.
					if(lines_free >= 2){
						lines_free -= 2;
						wStr.Add(new colorstring(""));
						aStr.Add(new colorstring(""));
					}
					if(lines_free >= 1){
						lines_free -= 1;
						mStr.Add(new colorstring(""));
					}
				}
				if(lines_free >= 1){ //if there's one left, it means that we had the full 7 lines, so we need an extra space at the top.
					lines_free -= 1;
					top.Add(new colorstring(""));
				}

				int row = 12;
				foreach(List<colorstring> list in new List<List<colorstring>>{top,wStr,aStr,mStr}){
					foreach(colorstring cs in list){
						Screen.WriteMapString(row++,0,cs.PadRight(COLS));
					}
				}

				if(newWeapon == equippedWeapon && newArmor == equippedArmor){
					Screen.WriteMapString(row,0,"".PadRight(COLS,'-'));
					MouseUI.RemoveButton(Global.MAP_OFFSET_ROWS+row,Global.MAP_OFFSET_COLS);
				}
				else{
					if((newWeapon != equippedWeapon && equippedWeapon.status[EquipmentStatus.STUCK]) || (newArmor != equippedArmor && equippedArmor.status[EquipmentStatus.STUCK])){
						Screen.WriteMapString(row,0,"".PadRight(COLS,'-'));
						MouseUI.RemoveButton(Global.MAP_OFFSET_ROWS+row,Global.MAP_OFFSET_COLS);
					}
					else{
						Screen.WriteMapString(row,0,new colorstring("[",Color.Gray,"Enter",Color.Magenta,"] to confirm",Color.Gray).PadOuter(COLS,'-'));
						MouseUI.CreateMapButton(ConsoleKey.Enter,false,row,1);
					}
				}

				Screen.ResetColors();
				UI.Display("Your equipment: ");
				command = Input.ReadKey();
				char ch = command.GetCommandChar();
				switch(ch){
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case '!':
				case '@':
				case '#':
				case '$':
				case '%':
				{
					switch(ch){
					case '!':
					ch = 'a';
					break;
					case '@':
					ch = 'b';
					break;
					case '#':
					ch = 'c';
					break;
					case '$':
					ch = 'd';
					break;
					case '%':
					ch = 'e';
					break;
					}
					int num = (int)(ch - 'a');
					if(num != (int)(selectedWeapon)){
						MouseUI.GetButton((int)(selectedWeapon)+1+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = (ConsoleKey)(ConsoleKey.A + (int)selectedWeapon);
						MouseUI.GetButton(num+1+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = ConsoleKey.Enter;
						selectedWeapon = (WeaponType)num;
					}
					break;
				}
				case 'f':
				case 'g':
				case 'h':
				case '*':
				case '(':
				case ')':
				{
					switch(ch){
					case '*':
					ch = 'f';
					break;
					case '(':
					ch = 'g';
					break;
					case ')':
					ch = 'h';
					break;
					}
					int num = (int)(ch - 'f');
					if(num != (int)(selectedArmor)){
						MouseUI.GetButton((int)(selectedArmor)+8+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = (ConsoleKey)(ConsoleKey.F + (int)selectedArmor);
						MouseUI.GetButton(num+8+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = ConsoleKey.Enter;
						selectedArmor = (ArmorType)num;
					}
					break;
				}
				case 'i':
				case 'j':
				case 'k':
				case 'l':
				case 'm':
				case 'n':
				case 'o':
				case 'p':
				case 'q':
				case 'r':
				{
					int num = (int)ch - (int)'i';
					if(num < trinkets.Count && num != selectedTrinketIdx){
						selectedTrinketIdx = num;
					}
					break;
				}
				case (char)27:
				case ' ':
				selectedWeapon = equippedWeapon.type; //reset
				selectedArmor = equippedArmor.type;
				done = true;
				break;
				case (char)13:
				done = true;
				break;
				default:
				break;
				}
			}
			MouseUI.PopButtonMap();
			UI.draw_bottom_commands = true;
			UI.darken_status_bar = false;
			return new int[]{(int)selectedWeapon,(int)selectedArmor};
		}
		public static string GetEnvironmentalDescription(){
			Tile t = player.tile();
			if(player.HasAttr(AttrType.FROZEN)){
				return "You're stuck in the ice! ";
			}
			const string dust = "Blinding dust hangs thickly in the air.";
			const string fogVent = "Fog pours from a narrow crack in the stone.";
			const string poisonVent = "Choking poison seeps from the porous stone.";
			if(player.HasAttr(AttrType.BLIND)){
				if(t.Is(FeatureType.THICK_DUST)) return dust;
				return "Everything is pitch black. You strain to hear your surroundings.";
			}
			if(t.inv?.type == ConsumableType.BLAST_FUNGUS){
				return "A hiss from the blast fungus signals its impending detonation!";
			}
			if(t.features.Count > 0){
				foreach(FeatureType f in Tile.feature_priority){
					if(t.Is(f)){
						switch(f){
						case FeatureType.GRENADE:
						return "A grenade sits beneath you, moments from detonation!";
						case FeatureType.TROLL_CORPSE:
						case FeatureType.TROLL_BLOODWITCH_CORPSE:
						{
							string s = $"A mangled troll twitches on {t.GetName(The)}.";
							if(s.Length > COLS){
								return "A mangled troll twitches here.";
							}
							return s;
						}
						case FeatureType.FOG:
						{
							if(t.Is(TileType.FOG_VENT)) return fogVent;
							return "Dense fog envelops you.";
						}
						case FeatureType.POISON_GAS:
						{
							if(t.Is(TileType.POISON_GAS_VENT)) return poisonVent;
							return "Thick poisonous gas swirls around you, drifting slowly downward.";
						}
						case FeatureType.SLIME:
						{
							string s = $"A thick layer of slippery slime covers {t.GetName(The)}.";
							if(s.Length > COLS){
								return "A thick layer of slippery slime covers every surface here.";
							}
							return s;
						}
						case FeatureType.OIL:
						{
							string s = $"Oil is puddled on {t.GetName(The)} here, awaiting a spark.";
							if(s.Length > COLS){
								return "Oil is puddled here, awaiting a spark.";
							}
							return s;
						}
						case FeatureType.TELEPORTAL:
						case FeatureType.STABLE_TELEPORTAL:
						return "The teleportal crackles around you.";
						case FeatureType.INACTIVE_TELEPORTAL:
						return "A teleportal lies dormant here, waiting for a counterpart.";
						case FeatureType.FIRE:
						return "Flames leap around you.";
						case FeatureType.BONES:
						return "Threads of necromantic magic still hold these bones together.";
						case FeatureType.WEB:
						return "Sticky threads embrace you.";
						case FeatureType.PIXIE_DUST:
						return "Delicate clouds of pixie dust float here.";
						case FeatureType.FORASECT_EGG:
						return "A clutch of eggs quivers beneath you.";
						case FeatureType.SPORES:
						return "Foul fungal spores fill the air.";
						case FeatureType.THICK_DUST:
						return dust;
						case FeatureType.CONFUSION_GAS:
						return "The air here is a multi-hued haze of psychotropic fumes.";
						}
					}
				}
			}
			else{
				switch(t.type){
				case TileType.DOOR_O:
				return "You pass through the doorway.";
				case TileType.STAIRS:
				if(t.color == Color.RandomDoom){
					return "The stairway is sealed by the infernal power of the demonic idols.";
				}
				else{
					return "Stone steps lead downward out of sight.";
				}
				case TileType.CHEST:
				return "A small hinged chest rests on the floor.";
				case TileType.FIREPIT:
				if(player.HasAttr(AttrType.FLYING)){
					return "You float over the firepit.";
				}
				else{
					return "You tread carefully over the firepit.";
				}
				case TileType.UNLIT_FIREPIT:
				if(player.HasAttr(AttrType.FLYING)){
					return "You float over the cooling stones.";
				}
				else{
					return "You tread over the cooling stones.";
				}
				case TileType.COMBAT_SHRINE:
				case TileType.DEFENSE_SHRINE:
				case TileType.MAGIC_SHRINE:
				case TileType.SPIRIT_SHRINE:
				case TileType.STEALTH_SHRINE:
				case TileType.SPELL_EXCHANGE_SHRINE:
				return $"The {t.GetName()} glows faintly.";
				case TileType.RUINED_SHRINE:
				return $"The power has faded from the {t.GetName()}.";
				case TileType.FIRE_GEYSER:
				return "An unbearable wave of heat heralds another spray of liquid fire.";
				case TileType.POOL_OF_RESTORATION:
				return "Ripples play across the surface of sweet-smelling water.";
				case TileType.FOG_VENT:
				return fogVent;
				case TileType.POISON_GAS_VENT:
				return poisonVent;
				case TileType.STONE_SLAB:
				case TileType.STONE_SLAB_OPEN:
				return "The enormous slab hangs above you.";
				case TileType.CHASM:
				if(player.HasAttr(AttrType.FLYING)){
					return "You hover over the chasm.";
				}
				return "You fall...";
				case TileType.FIRE_RIFT:
				if(player.HasAttr(AttrType.FLYING)){
					return "You try to avoid glancing into the inferno far below you.";
				}
				return "You fall...";
				case TileType.BREACHED_WALL:
				return "The ethereal outline of the displaced wall remains visible.";
				case TileType.WATER:
				if(player.HasAttr(AttrType.FLYING)){
					return "You float over the shallow water.";
				}
				return "Cool water sloshes around your legs.";
				case TileType.ICE:
				if(player.HasAttr(AttrType.FLYING)){
					return "You float over the smooth ice.";
				}
				return "You tread over a thick layer of smooth ice.";
				case TileType.BRUSH:
				return "Dry grasses spring from the ground.";
				case TileType.POPPY_FIELD:
				return "Great clusters of scarlet poppies fill the air with a spicy scent.";
				case TileType.GRAVEL:
				if(player.HasAttr(AttrType.FLYING)){
					return "You float over the gravel.";
				}
				return "Gravel crunches underfoot.";
				case TileType.BLAST_FUNGUS:
				return "You move carefully over the explosive fungus.";
				case TileType.GLOWING_FUNGUS:
				return "Patches of luminescent fungus emit a spectral glow.";
				case TileType.TOMBSTONE:
				return "A weathered grave marker rests here.";
				case TileType.GRAVE_DIRT:
				return "The loose soil of the grave looks recently disturbed.";
				case TileType.VINE:
				return "Leafy vines crisscross and entwine.";
				case TileType.DEMONSTONE:
				return "The smell of sulfur rises from the porous demonstone.";
				default:
				break;
				}
			}
			if(t.IsKnownTrap()){
				if(t.Name.Singular.Contains("(safe)")){ //this is the hacky way in which Disarm Trap is implemented
					return $"You safely cross the {t.GetName()}.";
				}
				string s = $"The trigger mechanism of a {t.GetName()} waits beneath you.";
				if(!t.revealed_by_light || s.Length > COLS){
					return "The trigger mechanism of a dangerous trap waits beneath you.";
				}
				return s;
			}
			switch(M.aesthetics[t.p]){
			case AestheticFeature.BloodDarkRed:
			return "Blood is splashed across the floor.";
			case AestheticFeature.BloodOther:
			return "Strange ichor is splashed across the floor.";
			case AestheticFeature.Charred:
			return "The smell of smoke lingers over the charred floor.";
			case AestheticFeature.None:
			default:
			break;
			}
			if(M.dungeonDescription != "") return M.dungeonDescription;
			//turn "123456789012345678901234567890123456789012345678901234567890123456";
			return "You are in a maze of twisty passages, all alike.";
		}
		public static bool YesOrNoPrompt(string s,bool easyCancel = true) {
			player.Interrupt();
			MouseUI.PushButtonMap(MouseMode.YesNoPrompt);
			MouseUI.CreateButton(ConsoleKey.Y,false,2,Global.MAP_OFFSET_COLS + s.Length + 1,1,2);
			MouseUI.CreateButton(ConsoleKey.N,false,2,Global.MAP_OFFSET_COLS + s.Length + 4,1,2);
			if(MouseUI.descend_hack) {
				for(int i = 0;i<Global.SCREEN_H;++i) {
					if(MouseUI.mouselook_objects[i,0] != null) {
						Tile t = MouseUI.mouselook_objects[i,0] as Tile;
						if(t?.type == TileType.STAIRS) {
							MouseUI.CreateStatsButton(ConsoleKey.N,false,i,1);
						}
					}
				}
				if(UI.viewing_commands_idx == 1) {
					MouseUI.CreateStatsButton(ConsoleKey.N,false,Global.SCREEN_H-5,1);
				}
				MouseUI.descend_hack = false;
			}
			UI.Display(s + " (y/n): ");
			while(true) {
				switch(Input.ReadKey().KeyChar) {
					case 'y':
					case 'Y':
						MouseUI.PopButtonMap();
						return true;
					case 'n':
					case 'N':
						MouseUI.PopButtonMap();
						return false;
					default:
						if(easyCancel) {
							MouseUI.PopButtonMap();
							return false;
						}
						break;
				}
			}
		}
		public static void Display(string message,bool refreshStatus = true) {
			if(refreshStatus) UI.DisplayStats();
			var wrapped = message.GetWordWrappedList(Global.COLS,false);
			for(int i=0;i < 3 - wrapped.Count;++i) Screen.WriteString(i,Global.MAP_OFFSET_COLS,"".PadToMapSize());
			for(int i=0;i < wrapped.Count;++i) Screen.WriteString(3 - wrapped.Count + i,Global.MAP_OFFSET_COLS,message.PadToMapSize());
			Screen.SetCursorPosition(Global.MAP_OFFSET_COLS + message.Length,2);
		}
	}
}

