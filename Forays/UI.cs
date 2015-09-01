//
using System;
using System.Collections.Generic;
using PosArrays;
using Utilities;
namespace Forays{
	public static class UI{
		public static Actor player{get{ return Actor.player; } }
		public static Map M{get{ return Actor.M; } }
		public static Queue Q{get{ return Actor.Q; } }

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
			AttrType.DIM_VISION,AttrType.IMMOBILE,AttrType.AGGRAVATING,AttrType.POPPY_COUNTER,AttrType.GRABBED,AttrType.GRABBING, //'typical' statuses

			AttrType.TELEPORTING,AttrType.SLIMED,AttrType.OIL_COVERED,AttrType.SHINING,AttrType.ROOTS,AttrType.PSEUDO_VAMPIRIC,AttrType.STONEFORM,
			AttrType.SILENCED,AttrType.SILENCE_AURA,AttrType.FROZEN, //'neutral' statuses

			AttrType.INVULNERABLE,AttrType.MECHANICAL_SHIELD,AttrType.SHIELDED,AttrType.REGENERATING,AttrType.BANDAGED,AttrType.RESTING, //shields and healing

			AttrType.INVISIBLE,AttrType.SHADOW_CLOAK, //visibility modifiers

			AttrType.FLYING,AttrType.FLYING_LEAP,AttrType.VIGOR,AttrType.BLOOD_BOILED, //movement

			AttrType.SHADOWSIGHT,AttrType.MYSTIC_MIND,AttrType.DETECTING_MOVEMENT,AttrType.DETECTING_MONSTERS, //detection and senses

			AttrType.BRUTISH_STRENGTH,AttrType.RADIANT_HALO,AttrType.EMPOWERED_SPELLS,AttrType.CONVICTION, //other positive effects
			};

		public static void DisplayStats(){ DisplayStats(false); }
		public static void DisplayStats(bool cyan_letters){
			bool buttons = MouseUI.AutomaticButtonsFromStrings;
			MouseUI.AutomaticButtonsFromStrings = false;
			Screen.CursorVisible = false;
			int row = 0;
			if(!viewing_map_shrine_info){ //todo: make this shrine stuff a separate method or something.
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
				if(player.tile().Is(FeatureType.WEB) && !player.HasAttr(AttrType.BURNING,AttrType.SLIMED,AttrType.OIL_COVERED)){
					StatusWriteString(ref row,new colorstring("Webbed".PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkMagenta));
				}
				if(player.IsSilencedHere() && !player.HasAttr(AttrType.SILENCED,AttrType.SILENCE_AURA)){
					StatusWriteString(ref row,new colorstring(AttrType.SILENCED.StatusName(player).PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkMagenta));
				}
				equipment_row = row;
				StatusWriteString(ref row,player.EquippedWeapon.StatusName());
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(player.EquippedWeapon.status[(EquipmentStatus)i]){
						StatusWriteString(ref row,new colorstring(Weapon.StatusName((EquipmentStatus)i).PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkBlue));
					}
				}
				StatusWriteString(ref row,player.EquippedArmor.StatusName());
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(player.EquippedArmor.status[(EquipmentStatus)i]){
						StatusWriteString(ref row,new colorstring(Weapon.StatusName((EquipmentStatus)i).PadOuter(Global.STATUS_WIDTH),Color.Gray,Color.DarkBlue));
					}
				}
				depth_row = row;
				StatusWriteString(ref row,("Depth: " + M.current_level.ToString()).PadOuter(Global.STATUS_WIDTH));
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
			else{ //todo fix this:
				Color[] colors = new Color[5];
				string[] shrines = new string[]{"  Combat    ","  Defense   ","  Magic     ","  Spirit    ","  Stealth   "};
				for(int i=0;i<5;++i){
					if(Map.shrine_locations[i].BoundsCheck(M.tile,true) && M.tile[Map.shrine_locations[i]].seen){
						if(M.tile[Map.shrine_locations[i]].type == TileType.RUINED_SHRINE){
							colors[i] = Color.DarkGray;
						}
						else{
							colors[i] = Color.Gray;
						}
					}
					else{
						colors[i] = Color.DarkGray;
						//shrines[i] = "    ---     ";
						shrines[i] = "  -------   ";
					}
				}
				Screen.WriteStatsString(row,0,"            ");
				++row;
				Screen.WriteStatsString(row,0,"            ");
				++row;
				Screen.WriteStatsString(row,0," -Shrines-  ",Color.Yellow);
				for(int i=0;i<5;++i){
					Screen.WriteStatsString(row,0,shrines[i],colors[i]);
					++row;
				}
				/*Screen.WriteStatsString(5,0,"  Combat    ",colors[0]);
				Screen.WriteStatsString(6,0,"  Defense   ",colors[1]);
				Screen.WriteStatsString(7,0,"  Magic     ",colors[2]);
				Screen.WriteStatsString(8,0,"  Spirit    ",colors[3]);
				Screen.WriteStatsString(9,0,"  Stealth   ",colors[4]);*/
				++row;
				Screen.WriteStatsString(row,0,"            "); //todo test
			}
			string[] commandhints = null;
			List<int> blocked_commands = new List<int>();
			switch(viewing_commands_idx){
			case 0:
				commandhints = new string[]{
					"Look around [Tab]   ",
					"[a]pply item        ",
					"[g]et item          ",
					"[f]ling item        ",
					"Wait a turn [.]     ",
					"Travel somewhere [X]",
					"Descend stairs [>]  ",
					"[v]iew more         ",
				};
			break;
			case 1:
				commandhints = new string[]{
					"[w]alk              ",
					"[o]perate something ",
					"View known items [\\]",
					"[p]revious messages ",
					"Help [?]            ",
					"Options [=]         ",
					"[q]uit              ",
					"[v]iew more         ",
				};
			break;
			case 2:
			commandhints = new string[]{
				"[v]iew more         ",
			};
			break;
			}
			/*if(player.attrs[AttrType.RESTING] == -1){
				blocked_commands.Add(5);
			}
			if(M.wiz_dark || M.wiz_lite){
				blocked_commands.Add(3);
			}*/
			Color wordcolor = cyan_letters? Color.Gray : Color.DarkGray;
			Color lettercolor = cyan_letters? Color.Cyan : Color.DarkCyan;
			for(int i=0;i<commandhints.Length;++i){
				if(blocked_commands.Contains(i)){
					Screen.WriteString(status_row_cutoff+i+1,0,commandhints[i].GetColorString(Color.DarkGray,Color.DarkCyan));
				}
				else{
					Screen.WriteString(status_row_cutoff+i+1,0,commandhints[i].GetColorString(wordcolor,lettercolor));
				}
			}
			Screen.WriteString(Global.SCREEN_H - 3,Global.MAP_OFFSET_COLS,"You are in a maze of twisty passages, all alike. ".PadOuter(Global.COLS),Color.Gray,Color.DarkerGray);
			//Screen.WriteString(Global.SCREEN_H - 2,Global.MAP_OFFSET_COLS,"   E[x]plore   [t]orch   [s]hoot bow   Cast spell [z]   [r]est    ".GetColorString(wordcolor,lettercolor));
			Color hack_color_todo = Color.Black; //so, darker gray looks pretty decent. Not sure what to do in 16 color mode.
			//dark green is also not completely terrible. 
			Color hackcolor2 = Color.Gray; //todo - add these colors to Color.cs as statics. Set in Main based on GLMode?
			Screen.WriteString(Global.SCREEN_H - 2,Global.MAP_OFFSET_COLS,"E[x]plore     [t]orch     [s]hoot bow    [r]est     Cast spell [z]".GetColorString(hackcolor2,lettercolor,hack_color_todo));
			Screen.WriteString(Global.SCREEN_H - 1,Global.MAP_OFFSET_COLS,"[i]nventory   [e]quipment [c]haracter    [m]ap              [Menu]".GetColorString(hackcolor2,lettercolor,hack_color_todo));
			//Screen.WriteString(Global.SCREEN_H - 2,Global.MAP_OFFSET_COLS,"   E[x]plore   [t]orch   [s]hoot bow   [r]est   Cast spell [z]    ".GetColorString(wordcolor,lettercolor,hack_color_todo));
			//Screen.WriteString(Global.SCREEN_H - 1,Global.MAP_OFFSET_COLS,"     [i]nventory   [e]quipment   [c]haracter   [m]ap   [Menu]     ".GetColorString(wordcolor,lettercolor,hack_color_todo));
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
		public static int status_row_start = 5;
		public static int equipment_row = 1;
		public static int depth_row = 3;
		public static int status_row_cutoff = Global.SCREEN_H - 9;
		public static void DisplayStatusBarObjects(){
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
			return "Boiling blood"; //"Blood-boiled", hmm.
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
			case AttrType.DETECTING_MONSTERS:
			if(a != null && a.type == ActorType.ROBED_ZEALOT){
				return "Praying";
			}
			break;
			//case AttrType.IMMOBILE:
			//return "Immobilized";
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
			if((!lookmode || mouselook) && item.Name(true).Length > widest){
				widest = item.Name(true).Length;
			}
			widest += 2; //one space on each side
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			if(!lookmode || mouselook){
				box.Add(new colorstring("|",box_edge_color) + item.Name(true).PadOuter(widest).GetColorString(Color.White) + new colorstring("|",box_edge_color));
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
	}
}

