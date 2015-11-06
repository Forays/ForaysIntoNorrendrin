/*Copyright (c) 2015  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
using System.Drawing;
using PosArrays;
using Utilities;
namespace Forays{
	public class Button{
		public ConsoleKey key;
		public ConsoleModifiers mods;
		public Rectangle rect;
		public int row{get{ return rect.Y; } }
		public int col{get{ return rect.X; } }
		public int height{get{ return rect.Height; } }
		public int width{get{ return rect.Width; } }
		public Button(ConsoleKey key_,bool alt,bool ctrl,bool shift,int row,int col,int height,int width){
			key = key_;
			mods = (ConsoleModifiers)0;
			if(alt){
				mods |= ConsoleModifiers.Alt;
			}
			if(ctrl){
				mods |= ConsoleModifiers.Control;
			}
			if(shift){
				mods |= ConsoleModifiers.Shift;
			}
			rect = new Rectangle(col,row,width,height);
		}
	}
	public enum MouseMode{Map,Inventory,Menu,ScrollableMenu,NameEntry,Targeting,Directional,YesNoPrompt};
	public static class MouseUI{
		public static bool AutomaticButtonsFromStrings = false;
		public static bool IgnoreMouseMovement = false;
		public static bool IgnoreMouseClicks = false;
		public static bool VisiblePath = true;
		private static List<Button[,]> button_map = new List<Button[,]>();
		private static List<MouseMode> mouse_mode = new List<MouseMode>();
		public static int MaxDescriptionBoxLength = 28;
		public static Button Highlighted = null;
		public static PhysicalObject[,] mouselook_objects = new PhysicalObject[Global.SCREEN_H,Global.SCREEN_W];
		public static PhysicalObject mouselook_current_target = null;
		public static Rectangle mouselook_current_desc_area = Rectangle.Empty;
		public static List<pos> mouse_path = null;
		public static bool fire_arrow_hack = false; //hack, used to allow double-clicking [s]hoot to fire arrows.
		public static bool descend_hack = false; //hack, used to make double-clicking Descend [>] cancel the action.
		public static Button GetButton(int row,int col){
			if(button_map.LastOrDefault() == null || row < 0 || col < 0 || row >= Global.SCREEN_H || col >= Global.SCREEN_W){
				return null;
			}
			return button_map.Last()[row,col];
		}
		public static MouseMode Mode{
			get{
				if(mouse_mode.Count == 0){
					return MouseMode.Menu;
				}
				return mouse_mode[mouse_mode.Count-1];
			}
		}
		public static Button[,] ButtonMap{
			get{
				if(button_map.LastOrDefault() == null){
					button_map[button_map.Count-1] = new Button[Global.SCREEN_H,Global.SCREEN_W];
				}
				return button_map[button_map.Count-1];
			}
		}
		public static void PushButtonMap(){
			button_map.Add(null);
			mouse_mode.Add(MouseMode.Menu);
			RemoveHighlight();
			RemoveMouseover();
		}
		public static void PushButtonMap(MouseMode mode){
			button_map.Add(null);
			mouse_mode.Add(mode);
			RemoveHighlight();
			RemoveMouseover();
		}
		public static void PopButtonMap(){
			RemoveHighlight();
			RemoveMouseover();
			button_map.RemoveLast();
			mouse_mode.RemoveLast();
		}
		public static void CreateButton(ConsoleKey key_,bool shifted,int row,int col,int height,int width){
			Button[,] buttons = ButtonMap;
			if(buttons[row,col] == null){ //if there's already a button there, do nothing.
				Button b = new Button(key_,false,false,shifted,row,col,height,width);
				for(int i=row;i<row+height;++i){
					for(int j=col;j<col+width;++j){
						buttons[i,j] = b;
					}
				}
			}
		}
		public static void CreateMapButton(ConsoleKey key_,bool shifted,int row,int height){
			Button[,] buttons = ButtonMap;
			row += Global.MAP_OFFSET_ROWS;
			int col = Global.MAP_OFFSET_COLS;
			int width = Global.COLS;
			if(buttons[row,col] == null){ //if there's already a button there, do nothing.
				Button b = new Button(key_,false,false,shifted,row,col,height,width);
				for(int i=row;i<row+height;++i){
					for(int j=col;j<col+width;++j){
						buttons[i,j] = b;
					}
				}
			}
		}
		public static void CreateStatsButton(ConsoleKey key_,bool shifted,int row,int height){
			Button[,] buttons = ButtonMap;
			if(buttons[row,0] == null){ //if there's already a button there, do nothing.
				Button b = new Button(key_,false,false,shifted,row,0,height,Global.STATUS_WIDTH);
				for(int i=row;i<row+height;++i){
					for(int j=0;j<Global.STATUS_WIDTH;++j){
						buttons[i,j] = b;
					}
				}
			}
		}
		public static void RemoveButton(int row,int col){
			Button b = GetButton(row,col);
			if(b != null){
				RemoveButton(b);
			}
		}
		public static void RemoveButton(Button b){
			Button[,] buttons = button_map[button_map.Count-1];
			if(buttons == null){
				return;
			}
			int row = b.rect.Y;
			int col = b.rect.X;
			int height = b.rect.Height;
			int width = b.rect.Width;
			for(int i=row;i<row+height;++i){
				for(int j=col;j<col+width;++j){
					buttons[i,j] = null;
				}
			}
		}
		public static void RemoveHighlight(){
			if(Highlighted != null){
				colorchar[,] highlight = new colorchar[Highlighted.height,Highlighted.width];
				int hh = Highlighted.height;
				int hw = Highlighted.width;
				int hr = Highlighted.row;
				int hc = Highlighted.col;
				for(int i=0;i<hh;++i){
					for(int j=0;j<hw;++j){
						highlight[i,j] = Screen.Char(i+hr,j+hc);
					}
				}
				Screen.UpdateGLBuffer(Highlighted.row,Highlighted.col,highlight);
				Highlighted = null;
			}
		}
		public static void RemoveMouseover(){
			if(mouselook_current_target != null){
				if(mouselook_current_desc_area != Rectangle.Empty){
					int h = mouselook_current_desc_area.Height;
					int w = mouselook_current_desc_area.Width;
					Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS + mouselook_current_desc_area.Left,Global.MAP_OFFSET_ROWS + h - 1,Global.MAP_OFFSET_COLS + mouselook_current_desc_area.Right - 1);
					mouselook_current_desc_area = Rectangle.Empty;
				}
				mouselook_current_target = null;
				Screen.CursorVisible = true;
			}
			if(mouse_path != null){
				foreach(pos p in mouse_path){
					int i = p.row;
					int j = p.col;
					colorchar cch = Screen.MapChar(i,j); //I tried doing this with a single call to UpdateVertexArray. It was slow.
					Screen.gl.UpdateOtherSingleVertex(Screen.textSurface,U.Get1DIndex(i+Global.MAP_OFFSET_ROWS,j+Global.MAP_OFFSET_COLS,Global.SCREEN_W),(int)cch.c,0,cch.color.GetFloatValues(),cch.bgcolor.GetFloatValues());
					//Game.gl.UpdateVertexArray(i+Global.MAP_OFFSET_ROWS,j+Global.MAP_OFFSET_COLS,GLGame.text_surface,0,(int)cch.c,cch.color.GetFloatValues(),cch.bgcolor.GetFloatValues());
				}
				mouse_path = null;
			}
		}
		public static void CreateStatsButtons(){
			switch(UI.viewing_commands_idx){
			case 0:
			UI.status_row_cutoff = Global.SCREEN_H - 9;
			CreateStatsButton(ConsoleKey.Tab,false,Global.SCREEN_H-8,1); //look
			CreateStatsButton(ConsoleKey.P,false,Global.SCREEN_H-7,1); //previous messages
			CreateStatsButton(ConsoleKey.A,false,Global.SCREEN_H-6,1); //apply
			CreateStatsButton(ConsoleKey.G,false,Global.SCREEN_H-5,1); //get
			CreateStatsButton(ConsoleKey.F,false,Global.SCREEN_H-4,1); //fling
			CreateStatsButton(ConsoleKey.OemPeriod,false,Global.SCREEN_H-3,1); //wait [.]
			CreateStatsButton(ConsoleKey.Oem2,true,Global.SCREEN_H-2,1); //help [?]
			CreateStatsButton(ConsoleKey.V,false,Global.SCREEN_H-1,1); //view more
			break;
			case 1:
			UI.status_row_cutoff = Global.SCREEN_H - 9;
			CreateStatsButton(ConsoleKey.Oem5,false,Global.SCREEN_H-8,1); //known items [\]
			CreateStatsButton(ConsoleKey.O,false,Global.SCREEN_H-7,1); //operate
			CreateStatsButton(ConsoleKey.X,true,Global.SCREEN_H-6,1); //travel
			CreateStatsButton(ConsoleKey.OemPeriod,true,Global.SCREEN_H-5,1); //descend [>]
			CreateStatsButton(ConsoleKey.W,false,Global.SCREEN_H-4,1); //walk
			CreateStatsButton(ConsoleKey.OemPlus,false,Global.SCREEN_H-3,1); //options [=]
			CreateStatsButton(ConsoleKey.Q,false,Global.SCREEN_H-2,1); //quit
			CreateStatsButton(ConsoleKey.V,false,Global.SCREEN_H-1,1); //view more
			break;
			case 2:
			if(Global.Option(OptionType.HIDE_VIEW_MORE)){
				UI.status_row_cutoff = Global.SCREEN_H - 1;
			}
			else{
				UI.status_row_cutoff = Global.SCREEN_H - 2;
				CreateStatsButton(ConsoleKey.V,false,Global.SCREEN_H-1,1); //view more
			}
			break;
			}

			CreateMapButton(ConsoleKey.P,false,-3,3);
			CreatePlayerStatsButtons();
			CreateButton(ConsoleKey.X,false,Global.SCREEN_H-2,Global.MAP_OFFSET_COLS,1,9); //explore
			CreateButton(ConsoleKey.T,false,Global.SCREEN_H-2,Global.MAP_OFFSET_COLS+14,1,7); //torch
			CreateButton(ConsoleKey.S,false,Global.SCREEN_H-2,Global.MAP_OFFSET_COLS+26,1,11); //shoot bow
			CreateButton(ConsoleKey.R,false,Global.SCREEN_H-2,Global.MAP_OFFSET_COLS+41,1,6); //rest
			CreateButton(ConsoleKey.Z,false,Global.SCREEN_H-2,Global.MAP_OFFSET_COLS+52,1,14); //cast spell
			CreateButton(ConsoleKey.I,false,Global.SCREEN_H-1,Global.MAP_OFFSET_COLS,1,11); //inventory
			CreateButton(ConsoleKey.E,false,Global.SCREEN_H-1,Global.MAP_OFFSET_COLS+14,1,11); //equipment
			CreateButton(ConsoleKey.C,false,Global.SCREEN_H-1,Global.MAP_OFFSET_COLS+26,1,11); //character
			CreateButton(ConsoleKey.M,false,Global.SCREEN_H-1,Global.MAP_OFFSET_COLS+41,1,5); //map
			CreateButton(ConsoleKey.F21,false,Global.SCREEN_H-1,Global.MAP_OFFSET_COLS+60,1,6); //menu
		}
		public static void CreatePlayerStatsButtons(){
			if(Mode != MouseMode.Map) return;
			ConsoleKey[] keys = new ConsoleKey[]{ConsoleKey.C,ConsoleKey.E,ConsoleKey.M};
			int[] rows = new int[]{0,UI.equipment_row,UI.depth_row};
			int[] heights = new int[]{UI.equipment_row,UI.depth_row - UI.equipment_row,UI.status_row_start - UI.depth_row - 1};
			if(MouseUI.GetButton(0,0) == null){ // if there's no button here, assume that there are no buttons in this area at all.
				for(int n=0;n<3;++n){
					if(rows[n] + heights[n] > UI.status_row_cutoff){
						return;
					}
					CreateStatsButton(keys[n],false,rows[n],heights[n]);
				}
			}
			else{
				bool all_found = false;
				for(int n=0;n<3;++n){
					if(heights[n] <= 0 || rows[n] + heights[n] > UI.status_row_cutoff){
						break;
					}
					Button b = MouseUI.GetButton(rows[n],0);
					if(b != null && b.key == keys[n] && b.row == rows[n] && b.height == heights[n]){ //perfect match, keep it there.
						if(b.key == ConsoleKey.M){
							all_found = true;
						}
					}
					else{
						for(int i=rows[n];i<rows[n]+heights[n];++i){
							Button b2 = MouseUI.GetButton(i,0);
							if(b2 != null){
								if(b2.key == ConsoleKey.M){
									all_found = true;
								}
								MouseUI.RemoveButton(b2);
							}
						}
						CreateStatsButton(keys[n],false,rows[n],heights[n]);
					}
				}
				if(!all_found){
					for(int i=rows[2]+heights[2];i<=UI.status_row_cutoff;++i){ //gotta continue downward until all the previous
						Button b = MouseUI.GetButton(i,0); // buttons have been accounted for.
						if(b != null){
							MouseUI.RemoveButton(b);
							if(b.key == ConsoleKey.M){
								break;
							}
						}
					}
				}
			}
		}
	}
}
