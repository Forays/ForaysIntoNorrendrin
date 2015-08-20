//
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
		public static int LastRow = -1;
		public static int LastCol = -1;
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
			//row += Global.MAP_OFFSET_ROWS;
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
			int width = 12; //todo
			if(buttons[row,0] == null){ //if there's already a button there, do nothing.
				Button b = new Button(key_,false,false,shifted,row,0,height,width);
				for(int i=row;i<row+height;++i){
					for(int j=0;j<width;++j){
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
					Game.gl.UpdateVertexArray(i+Global.MAP_OFFSET_ROWS,j+Global.MAP_OFFSET_COLS,GLGame.text_surface,0,(int)cch.c,cch.color.GetFloatValues(),cch.bgcolor.GetFloatValues());
				}
				mouse_path = null;
			}
		}
	}
}
