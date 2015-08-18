using System;
using System.Collections.Generic;
using Utilities;
namespace Forays{
	public static class UI{
		public static Actor player{get{ return Actor.player; } }
		public static Map M{get{ return Actor.M; } }

		public static bool viewing_more_commands = false; //used in DisplayStats to show extra commands
		public static bool viewing_map_shrine_info = false; //used in DisplayStats to show map info

		public static List<colorstring> ItemDescriptionBox(Item item,bool lookmode,bool mouselook,int max_string_length){
			List<string> text = item.Description().GetWordWrappedList(max_string_length);
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
		public static void DisplayStats(){ DisplayStats(false); }
		public static void DisplayStats(bool cyan_letters){
			bool buttons = MouseUI.AutomaticButtonsFromStrings;
			MouseUI.AutomaticButtonsFromStrings = false;
			Screen.CursorVisible = false;
			int row = 0;
			if(!viewing_map_shrine_info){
				//string s = "  Health:     " + player.curhp.ToString().PadLeft(4) + "  ";
				string s = ("Health: " + player.curhp.ToString()).PadOuter(20);
				int idx = Math.Max(0,20 * (player.curhp + 4) / player.maxhp);
				Screen.WriteString(row,0,new colorstring(new cstr(s.Substring(0,idx),Color.Gray,Color.DarkRed),new cstr(s.Substring(idx),Color.Gray,Color.Black)));
				++row;
				if(player.maxmp > 0){
					s = ("Mana: " + player.curmp.ToString()).PadOuter(20);
					idx = Math.Max(0,20 * (player.curmp) / player.maxmp);
					Screen.WriteString(row,0,new colorstring(new cstr(s.Substring(0,idx),Color.Gray,Color.DarkCyan),new cstr(s.Substring(idx),Color.Gray,Color.Black)));
					//Screen.WriteString(row,0,new cstr("  Mana:       " + player.curmp.ToString().PadLeft(4) + "  ",Color.Gray,Color.DarkBlue));
					++row;
				}
				if(player.exhaustion > 0){
					s = ("Exhaustion: " + player.exhaustion.ToString() + "%").PadOuter(20);
					idx = Math.Max(0,20 * (player.exhaustion) / 100);
					Screen.WriteString(row,0,new colorstring(new cstr(s.Substring(0,idx),Color.Gray,Color.DarkYellow),new cstr(s.Substring(idx),Color.Gray,Color.Black)));
					//Screen.WriteString(row,0,new cstr("  Exhaustion: " + (player.exhaustion.ToString() + "%").PadLeft(4) + "  ",Color.Gray,Color.DarkGreen));
					++row;
				}
				cstr cs = player.EquippedWeapon.StatsName();
				cs.s = ("-- " + cs.s + " --").PadOuter(20);
				Screen.WriteString(row,0,cs);
				colorstring statuses = new colorstring();
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(player.EquippedWeapon.status[(EquipmentStatus)i]){
						statuses.strings.Add(new cstr("*",Weapon.StatusColor((EquipmentStatus)i)));
						if(player.EquippedWeapon.StatsName().s.Length + statuses.Length() >= 19){
							break;
						}
					}
				}
				Screen.WriteString(row,player.EquippedWeapon.StatsName().s.Length + 1,statuses);
				++row;
				cs = player.EquippedArmor.StatsName();
				cs.s = ("-- " + cs.s + " --").PadOuter(20);
				Screen.WriteString(row,0,cs);
				statuses = new colorstring();
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(player.EquippedArmor.status[(EquipmentStatus)i]){
						statuses.strings.Add(new cstr("*",Weapon.StatusColor((EquipmentStatus)i)));
						if(player.EquippedArmor.StatsName().s.Length + statuses.Length() >= 19){
							break;
						}
					}
				}
				Screen.WriteString(row,player.EquippedArmor.StatsName().s.Length + 1,statuses);
				++row;
				Screen.WriteString(row,0,("Depth: " + M.current_level.ToString()).PadOuter(20));
				++row;
				Screen.WriteString(row,0,"            ");
				++row;
				//Screen.WriteString(row,0,"g",Color.Green);
				//Screen.WriteString(row,1,": goblin",Color.Gray);
				++row;
				//Screen.WriteString(row,0,new cstr(" (unaware)   HP: 15 ",Color.Gray,Color.DarkRed));
				++row;
				++row;
				//Screen.WriteString(row,0,"!: a silver potion",Color.Gray);
				++row;
				if(player.HasAttr(AttrType.BURNING)){
					Screen.WriteStatsString(row,0,"Burning",Color.Red);
				}
				else{
					if(player.HasAttr(AttrType.SLIMED)){
						Screen.WriteStatsString(row,0,"Slimed ",Color.Green);
					}
					else{
						if(player.HasAttr(AttrType.OIL_COVERED)){
							Screen.WriteStatsString(row,0,"Oiled  ",Color.DarkYellow);
						}
						else{
							if(player.HasAttr(AttrType.FROZEN)){
								Screen.WriteStatsString(row,0,"Frozen ",Color.Blue);
							}
							else{
								if(player.tile().Is(FeatureType.WEB) && !player.HasAttr(AttrType.BURNING,AttrType.OIL_COVERED,AttrType.SLIMED)){
									Screen.WriteStatsString(row,0,"Webbed ",Color.White);
								}
								else{
									Screen.WriteStatsString(row,0,"       ");
								}
							}
						}
					}
				}
			}
			else{
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
			string[] commandhints;
			List<int> blocked_commands = new List<int>();
			if(viewing_more_commands){
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
			}
			else{
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
				/*if(player.attrs[AttrType.RESTING] == -1){
					blocked_commands.Add(5);
				}
				if(M.wiz_dark || M.wiz_lite){
					blocked_commands.Add(3);
				}*/
			}
			Color wordcolor = cyan_letters? Color.Gray : Color.DarkGray;
			Color lettercolor = cyan_letters? Color.Cyan : Color.DarkCyan;
			row = Global.SCREEN_H - commandhints.Length;
			for(int i=0;i<commandhints.Length;++i){
				if(blocked_commands.Contains(i)){
					Screen.WriteString(row+i,0,commandhints[i].GetColorString(Color.DarkGray,Color.DarkCyan));
				}
				else{
					Screen.WriteString(row+i,0,commandhints[i].GetColorString(wordcolor,lettercolor));
				}
			}
			//row += 7;
			//Screen.WriteString(row,0,commandhints[7].GetColorString(wordcolor,lettercolor));
			// centering the environmental line is pretty nice:
			//Screen.WriteString(Global.SCREEN_H - 2,Global.MAP_OFFSET_COLS,"   E[x]plore   [t]orch   [s]hoot bow   Cast spell [z]   [r]est    ".GetColorString(wordcolor,lettercolor));
			Color hack_color_todo = Color.Black; //so, darker gray looks pretty decent. Not sure what to do in 16 color mode.
			//dark green is also not completely terrible. 
			Color hackcolor2 = Color.Gray;
			Screen.WriteString(Global.SCREEN_H - 2,Global.MAP_OFFSET_COLS,"E[x]plore     [t]orch     [s]hoot bow    [r]est     Cast spell [z]".GetColorString(hackcolor2,lettercolor,hack_color_todo));
			Screen.WriteString(Global.SCREEN_H - 1,Global.MAP_OFFSET_COLS,"[i]nventory   [e]quipment [c]haracter    [m]ap              [Menu]".GetColorString(hackcolor2,lettercolor,hack_color_todo));
			//Screen.WriteString(Global.SCREEN_H - 2,Global.MAP_OFFSET_COLS,"   E[x]plore   [t]orch   [s]hoot bow   [r]est   Cast spell [z]    ".GetColorString(wordcolor,lettercolor,hack_color_todo));
			//Screen.WriteString(Global.SCREEN_H - 1,Global.MAP_OFFSET_COLS,"     [i]nventory   [e]quipment   [c]haracter   [m]ap   [Menu]     ".GetColorString(wordcolor,lettercolor,hack_color_todo));
			Screen.ResetColors();
			MouseUI.AutomaticButtonsFromStrings = buttons;
		}
		public static void CreateDefaultStatsButtons(){
			MouseUI.PushButtonMap(MouseMode.Map);
			MouseUI.CreateStatsButton(ConsoleKey.I,false,12,1);
			MouseUI.CreateStatsButton(ConsoleKey.E,false,13,1);
			MouseUI.CreateStatsButton(ConsoleKey.C,false,14,1);
			MouseUI.CreateStatsButton(ConsoleKey.T,false,15,1);
			MouseUI.CreateStatsButton(ConsoleKey.Tab,false,16,1);
			MouseUI.CreateStatsButton(ConsoleKey.R,false,17,1);
			MouseUI.CreateStatsButton(ConsoleKey.A,false,18,1);
			MouseUI.CreateStatsButton(ConsoleKey.G,false,19,1);
			MouseUI.CreateStatsButton(ConsoleKey.F,false,20,1);
			MouseUI.CreateStatsButton(ConsoleKey.S,false,21,1);
			MouseUI.CreateStatsButton(ConsoleKey.Z,false,22,1);
			MouseUI.CreateStatsButton(ConsoleKey.X,false,23,1);
			MouseUI.CreateStatsButton(ConsoleKey.V,false,24,1);
			MouseUI.CreateStatsButton(ConsoleKey.E,false,7,2);
			MouseUI.CreateMapButton(ConsoleKey.P,false,0,3);
		}
	}
}

