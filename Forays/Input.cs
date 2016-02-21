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
using System.Threading;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PosArrays;
using Utilities;
using GLDrawing;
namespace Forays{
	public static class Input{
		public static bool KeyPressed = false;
		public static ConsoleKeyInfo LastKey;

		public static Dictionary<ConsoleKeyInfo,ConsoleKeyInfo> global_rebindings = new Dictionary<ConsoleKeyInfo,ConsoleKeyInfo>(); //todo: change these to Converters?
		public static Dictionary<ConsoleKeyInfo,ConsoleKeyInfo> action_rebindings = new Dictionary<ConsoleKeyInfo,ConsoleKeyInfo>();

		public static bool KeyIsAvailable(){
			if(Screen.GLMode){
				return KeyPressed;
			}
			return Console.KeyAvailable;
		}
		public static void FlushInput(){
			if(Screen.GLMode){
				Screen.gl.ProcessEvents();
				KeyPressed = false;
			}
			else{
				while(Console.KeyAvailable){
					Console.ReadKey(true);
				}
			}
		}
		public static ConsoleKey GetConsoleKey(Key key){ //convert from openTK's key enum to System.Console's key enum
			if(key >= Key.A && key <= Key.Z){
				return (ConsoleKey)(key - (Key.A - (int)ConsoleKey.A));
			}
			if(key >= Key.Number0 && key <= Key.Number9){
				return (ConsoleKey)(key - (Key.Number0 - (int)ConsoleKey.D0));
			}
			if(key >= Key.Keypad0 && key <= Key.Keypad9){
				return (ConsoleKey)(key - (Key.Keypad9 - (int)ConsoleKey.NumPad9));
			}
			if(key >= Key.F1 && key <= Key.F24){
				return (ConsoleKey)(key - (Key.F1 - (int)ConsoleKey.F1));
			}
			switch(key){
			case Key.BackSpace:
			return ConsoleKey.Backspace;
			case Key.Tab:
			return ConsoleKey.Tab;
			case Key.Enter:
			case Key.KeypadEnter:
			return ConsoleKey.Enter;
			case Key.Escape:
			return ConsoleKey.Escape;
			case Key.Space:
			return ConsoleKey.Spacebar;
			case Key.Delete:
			return ConsoleKey.Delete;
			case Key.Up:
			return ConsoleKey.UpArrow;
			case Key.Down:
			return ConsoleKey.DownArrow;
			case Key.Left:
			return ConsoleKey.LeftArrow;
			case Key.Right:
			return ConsoleKey.RightArrow;
			case Key.Comma:
			return ConsoleKey.OemComma;
			case Key.Period:
			return ConsoleKey.OemPeriod;
			case Key.Minus:
			return ConsoleKey.OemMinus;
			case Key.Plus:
			return ConsoleKey.OemPlus;
			case Key.Tilde:
			return ConsoleKey.Oem3;
			case Key.BracketLeft:
			return ConsoleKey.Oem4;
			case Key.BracketRight:
			return ConsoleKey.Oem6;
			case Key.BackSlash:
			return ConsoleKey.Oem5;
			case Key.Semicolon:
			return ConsoleKey.Oem1;
			case Key.Quote:
			return ConsoleKey.Oem7;
			case Key.Slash:
			return ConsoleKey.Oem2;
			case Key.KeypadDivide:
			return ConsoleKey.Divide;
			case Key.KeypadMultiply:
			return ConsoleKey.Multiply;
			case Key.KeypadMinus:
			return ConsoleKey.Subtract;
			case Key.KeypadAdd:
			return ConsoleKey.Add;
			case Key.KeypadDecimal:
			return ConsoleKey.Decimal;
			case Key.Home:
			return ConsoleKey.Home;
			case Key.End:
			return ConsoleKey.End;
			case Key.PageUp:
			return ConsoleKey.PageUp;
			case Key.PageDown:
			return ConsoleKey.PageDown;
			case Key.Clear:
			return ConsoleKey.Clear;
			case Key.Insert:
			return ConsoleKey.Insert;
			case Key.WinLeft:
			return ConsoleKey.LeftWindows;
			case Key.WinRight:
			return ConsoleKey.RightWindows;
			case Key.Menu:
			return ConsoleKey.Applications;
			default:
			return ConsoleKey.NoName;
			}
		}
		public static char GetChar(ConsoleKey k,bool shift){ //this method tries to return the most correct char for the given values. GetCommandChar(), OTOH, returns game-specific chars.
			if(k >= ConsoleKey.A && k <= ConsoleKey.Z){
				if(shift){
					return k.ToString()[0];
				}
				else{
					return k.ToString().ToLower()[0];
				}
			}
			if(k >= ConsoleKey.D0 && k <= ConsoleKey.D9){
				if(shift){
					switch(k){
					case ConsoleKey.D1:
					return '!';
					case ConsoleKey.D2:
					return '@';
					case ConsoleKey.D3:
					return '#';
					case ConsoleKey.D4:
					return '$';
					case ConsoleKey.D5:
					return '%';
					case ConsoleKey.D6:
					return '^';
					case ConsoleKey.D7:
					return '&';
					case ConsoleKey.D8:
					return '*';
					case ConsoleKey.D9:
					return '(';
					case ConsoleKey.D0:
					default:
					return ')';
					}
				}
				else{
					return k.ToString()[1];
				}
			}
			if(k >= ConsoleKey.NumPad0 && k <= ConsoleKey.NumPad9){
				return k.ToString()[6];
			}
			switch(k){
			case ConsoleKey.Tab:
			return (char)9;
			case ConsoleKey.Enter:
			return (char)13;
			case ConsoleKey.Escape:
			return (char)27;
			case ConsoleKey.Spacebar:
			return ' ';
			case ConsoleKey.OemComma:
			if(shift){
				return '<';
			}
			else{
				return ',';
			}
			case ConsoleKey.OemPeriod:
			if(shift){
				return '>';
			}
			else{
				return '.';
			}
			case ConsoleKey.OemMinus:
			if(shift){
				return '_';
			}
			else{
				return '-';
			}
			case ConsoleKey.OemPlus:
			if(shift){
				return '+';
			}
			else{
				return '=';
			}
			case ConsoleKey.Oem3:
			if(shift){
				return '~';
			}
			else{
				return '`';
			}
			case ConsoleKey.Oem4:
			if(shift){
				return '{';
			}
			else{
				return '[';
			}
			case ConsoleKey.Oem6:
			if(shift){
				return '}';
			}
			else{
				return ']';
			}
			case ConsoleKey.Oem5:
			if(shift){
				return '|';
			}
			else{
				return '\\';
			}
			case ConsoleKey.Oem1:
			if(shift){
				return ':';
			}
			else{
				return ';';
			}
			case ConsoleKey.Oem7:
			if(shift){
				return '"';
			}
			else{
				return '\'';
			}
			case ConsoleKey.Oem2:
			if(shift){
				return '?';
			}
			else{
				return '/';
			}
			case ConsoleKey.Divide:
			return '/';
			case ConsoleKey.Multiply:
			return '*';
			case ConsoleKey.Subtract:
			return '-';
			case ConsoleKey.Add:
			return '+';
			case ConsoleKey.Decimal:
			return '.';
			default:
			return (char)0;
			}
		}
		public static ConsoleKeyInfo ReadKey(){
			if(!Screen.GLMode){
				ConsoleKeyInfo raw = Console.ReadKey(true);
				bool shift = (raw.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift;
				ConsoleKeyInfo k = new ConsoleKeyInfo(GetChar(raw.Key,shift),raw.Key,shift,(raw.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt,(raw.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control);
				if(global_rebindings.ContainsKey(k)){
					return global_rebindings[k];
				}
				return k;
			}
			while(true){
				//Animations.Update();
				if(!Screen.GLUpdate()){
					Global.Quit();
				}
				if(Screen.CursorVisible){
					TimeSpan time = Global.Timer.Elapsed;
					if(time.Seconds >= 1){
						Screen.UpdateCursor(true);
						Global.Timer.Reset();
						Global.Timer.Start();
					}
					else{
						if(time.Milliseconds >= 500){
							Screen.UpdateCursor(false);
						}
					}
				}
				Thread.Sleep(10);
				if(KeyPressed){
					KeyPressed = false;
					if(global_rebindings.ContainsKey(LastKey)){
						return global_rebindings[LastKey];
					}
					return LastKey;
				}
			}
		}
		public static char GetCommandChar(this ConsoleKeyInfo k){
			if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
				if(k.Key == ConsoleKey.D0){
					return ' ';
				}
			}
			else{
				switch(k.Key){
				case ConsoleKey.D8:
				return '*';
				case ConsoleKey.D2:
				return '@';
				case ConsoleKey.D4:
				return '$';
				case ConsoleKey.D5:
				return '%';
				case ConsoleKey.D6:
				return '^';
				case ConsoleKey.D7:
				return '&';
				case ConsoleKey.D9:
				return '(';
				case ConsoleKey.D1:
				return '!';
				case ConsoleKey.D3:
				return '#';
				case ConsoleKey.D0:
				return ')';
				}
			}
			switch(k.Key){
			case ConsoleKey.Divide: // I prevent these from returning their usual chars because
			case ConsoleKey.Multiply: // pressing them accidentally is quite annoying.
			case ConsoleKey.Subtract:
			case ConsoleKey.Add:
			{
				return (char)0;
			}
			}
			return k.KeyChar;
		}
		public static ConsoleKeyInfo GetAction(this ConsoleKeyInfo k){
			if(action_rebindings.ContainsKey(k)){
				return action_rebindings[k];
			}
			return k;
		}
		public static List<string> GetDefaultBindings(){
			return new List<string>{
				"# This file controls key rebindings.",
				"# (Any line that starts with '#' is a comment and will be ignored.)",
				"# ",
				"# There are 2 types of rebindings: global rebindings & action rebindings.",
				"# Global rebindings affect the entire program, while action rebindings",
				"# affect only action commands - the ones that happen on the main game map.",
				"# The default mode is global, and if \"global\" or \"action\" appear on",
				"# their own line in this file, the mode will be changed for future rebindings.",
				"# ",
				"# A rebinding looks like this:  x:tab",
				"# That one means \"when x is pressed, generate a tab\". ",
				"# The modifier keys are shift, alt, and control:   \"escape:shift k\" means",
				"# \"when escape is pressed, generate shift-k\".",
				"# ",
				"# If you want to rebind a key to another key no matter what modifier keys",
				"# are pressed, do it like this:  \"all t:m\" - this binds 't' to 'm', 'alt-t'",
				"# to 'alt-m', 'ctrl-shift-t' to 'ctrl-shift-m', and so on.",
				"# ",
				"# ",
				"# To see the key names that'll work for rebinding, scroll to the end of this file.",
				"# ",
				"",
				"global",
				"",
				"# Here are the default bindings of the arrow keys to the cardinal directions:",
				"all uparrow:numpad8",
				"all leftarrow:numpad4",
				"all rightarrow:numpad6",
				"all downarrow:numpad2",
				"",
				"",
				"action",
				"",
				"# Here are the default vi-keys:",
				"all h:numpad4",
				"all j:numpad2",
				"all k:numpad8",
				"all l:numpad6",
				"all y:numpad7",
				"all u:numpad9",
				"all b:numpad1",
				"all n:numpad3",
				"",
				"# Here are the default numpad bindings:",
				"all home:numpad7",
				"all pageup:numpad9",
				"all clear:numpad5",
				"all end:numpad1",
				"all pagedown:numpad3",
				"",
				"# Default binding of '.' to '5':",
				"oemperiod:numpad5",
				"",
				"",
				"",
				"",
				"",
				"# Here are all the supported key names:",
				"# (note that these aren't case-sensitive) ",
				"# ",
				"# LeftArrow",
				"# UpArrow",
				"# RightArrow",
				"# DownArrow",
				"# Tab",
				"# Enter",
				"# Escape",
				"# Spacebar",
				"# PageUp",
				"# PageDown",
				"# End",
				"# Home",
				"# Clear    # (This is the middle key on the numpad with numlock off, on some systems)",
				"# Insert",
				"# Delete",
				"# Backspace",
				"# A",
				"# B",
				"# C",
				"# D",
				"# E",
				"# F",
				"# G",
				"# H",
				"# I",
				"# J",
				"# K",
				"# L",
				"# M",
				"# N",
				"# O",
				"# P",
				"# Q",
				"# R",
				"# S",
				"# T",
				"# U",
				"# V",
				"# W",
				"# X",
				"# Y",
				"# Z",
				"# D0",
				"# D1    # (D0-D9 are the top-row numbers)",
				"# D2",
				"# D3",
				"# D4",
				"# D5",
				"# D6",
				"# D7",
				"# D8",
				"# D9",
				"# NumPad0",
				"# NumPad1    # (These are of course the numpad numbers with numlock on)",
				"# NumPad2",
				"# NumPad3",
				"# NumPad4",
				"# NumPad5",
				"# NumPad6",
				"# NumPad7",
				"# NumPad8",
				"# NumPad9    # (The following Oem keys might change depending on keyboard layout)",
				"# Oem1    # (';' semicolon)",
				"# OemPlus    # ('=' equals, next to backspace)",
				"# OemComma    # (',' comma)",
				"# OemMinus    # ('-' hyphen, next to 0 on the top row)",
				"# OemPeriod    # ('.' period)",
				"# Oem2    # ('/' forward slash)",
				"# Oem3    # ('`' grave accent or backtick, on the same key as the '~' tilde)",
				"# Oem4    # ('[' left square bracket)",
				"# Oem5    # ('\\' backslash)",
				"# Oem6    # (']' right square bracket)",
				"# Oem7    # (''' apostrophe)",
				"# F1",
				"# F2",
				"# F3",
				"# F4",
				"# F5",
				"# F6",
				"# F7",
				"# F8",
				"# F9",
				"# F10",
				"# F11",
				"# F12",
				"# F13",
				"# F14",
				"# F15",
				"# F16",
				"# F17",
				"# F18",
				"# F19",
				"# F20    # (F21-F24 are reserved)",
				"# Multiply    # (These next 5 appear on the numpad but may not be",
				"# Add         #  generated on all systems)",
				"# Subtract",
				"# Decimal",
				"# Divide",
				"# PrintScreen",
				"# Pause    # (Everything beyond this point is a valid entry but",
				"# Separator    # probably won't do anything)",
				"# BrowserBack",
				"# BrowserForward",
				"# BrowserRefresh",
				"# BrowserStop",
				"# BrowserSearch",
				"# BrowserFavorites",
				"# BrowserHome",
				"# VolumeMute",
				"# VolumeDown",
				"# VolumeUp",
				"# MediaNext",
				"# MediaPrevious",
				"# MediaStop",
				"# MediaPlay",
				"# LaunchMail",
				"# LaunchMediaSelect",
				"# LaunchApp1",
				"# LaunchApp2",
				"# Oem8",
				"# Oem102",
				"# Process",
				"# Packet",
				"# Attention",
				"# CrSel",
				"# ExSel",
				"# EraseEndOfFile",
				"# Play",
				"# Zoom",
				"# NoName",
				"# Pa1",
				"# OemClear",
				"# Select",
				"# Print",
				"# Execute",
				"# Help",
				"# LeftWindows",
				"# RightWindows",
				"# Applications",
				"# Sleep",
				"# ",
				""};
		}
		public static void LoadKeyRebindings(){
			List<string> lines;
			if(File.Exists("keys.txt")){
				lines = new List<string>();
				StreamReader file = new StreamReader("keys.txt");
				while(true){
					if(file.Peek() != -1){
						lines.Add(file.ReadLine());
					}
					else{
						break;
					}
				}
				file.Close();
			}
			else{
				lines = GetDefaultBindings();
				StreamWriter file = new StreamWriter("keys.txt");
				foreach(string s in lines){
					file.WriteLine(s);
				}
				file.Close();
			}
			Dictionary<ConsoleKeyInfo,ConsoleKeyInfo> rebindings = global_rebindings;
			foreach(string line in lines){
				if(line.Length > 0 && line[0] == '#'){ //comment
					continue;
				}
				string[] part = line.Split(new char[]{':'},StringSplitOptions.RemoveEmptyEntries);
				if(part.Length != 2){
					if(part.Length > 0){
						if(part[0].ToLower() == "global"){
							rebindings = global_rebindings;
						}
						if(part[0].ToLower() == "action"){
							rebindings = action_rebindings;
						}
					}
					continue;
				}
				ConsoleKeyInfo[] cki = new ConsoleKeyInfo[2];
				int idx = 0;
				bool rebind_all = false;
				while(true){
					string[] words = part[idx].Split(' ');
					bool shift = false;
					bool alt = false;
					bool ctrl = false;
					List<ConsoleKey> key = new List<ConsoleKey>();
					foreach(string word in words){
						string s = word.ToLower();
						if(s == ""){
							continue;
						}
						if(s == "shift"){
							shift = true;
							continue;
						}
						if(s == "alt"){
							alt = true;
							continue;
						}
						if(s == "ctrl" || s == "control"){
							ctrl = true;
							continue;
						}
						if(s == "all"){
							rebind_all = true;
							continue;
						}
						try{
							ConsoleKey k = (ConsoleKey)Enum.Parse(typeof(ConsoleKey),s,true);
							if(k >= ConsoleKey.F21 && k <= ConsoleKey.F24){
								continue; //F21-F24 are reserved.
							}
							key.Add(k);
						}
						catch{}
					}
					if(key.Count == 1){
						cki[idx] = new ConsoleKeyInfo(GetChar(key[0],shift),key[0],shift,alt,ctrl);
						idx++;
						if(idx == 2){
							if(rebind_all){
								for(int i=0;i<8;++i){
									bool shift2 = (i & 1) == 1;
									bool alt2 = (i & 2) == 2;
									bool ctrl2 = (i & 4) == 4;
									ConsoleKeyInfo all_key = new ConsoleKeyInfo(GetChar(cki[0].Key,shift2),cki[0].Key,shift2,alt2,ctrl2);
									ConsoleKeyInfo all_value = new ConsoleKeyInfo(GetChar(cki[1].Key,shift2),cki[1].Key,shift2,alt2,ctrl2);
									if(!rebindings.ContainsKey(all_key)){
										rebindings.Add(all_key,all_value);
									}
								}
							}
							else{
								if(!rebindings.ContainsKey(cki[0])){
									rebindings.Add(cki[0],cki[1]);
								}
							}
							break;
						}
					}
					else{
						break;
					}
				}
			}
		}
		public static int EnterInt(){ return EnterInt(4); }
		public static int EnterInt(int max_length){
			string s = "";
			ConsoleKeyInfo command;
			Screen.CursorVisible = true;
			bool done = false;
			int pos = Screen.CursorLeft;
			Screen.WriteString(Screen.CursorTop,pos,"".PadRight(max_length));
			while(!done){
				Screen.SetCursorPosition(pos,Screen.CursorTop);
				command = ReadKey();
				if(command.KeyChar >= '0' && command.KeyChar <= '9'){
					if(s.Length < max_length){
						s = s + command.KeyChar;
						Screen.WriteChar(Screen.CursorTop,pos,command.KeyChar);
						++pos;
					}
				}
				else{
					if(command.Key == ConsoleKey.Backspace && s.Length > 0){
						s = s.Substring(0,s.Length-1);
						--pos;
						Screen.WriteChar(Screen.CursorTop,pos,' ');
						Screen.SetCursorPosition(pos,Screen.CursorTop);
					}
					else{
						if(command.Key == ConsoleKey.Escape){
							return 0;
						}
						else{
							if(command.Key == ConsoleKey.Enter){
								if(s.Length == 0){
									return -1;
								}
								done = true;
							}
						}
					}
				}
			}
			return Convert.ToInt32(s);
		}
		public static string EnterString(){ return EnterString(Global.COLS-1); }
		public static string EnterString(int max_length){
			string s = "";
			ConsoleKeyInfo command;
			Screen.CursorVisible = true;
			bool done = false;
			int cursor = Screen.CursorLeft;
			Screen.WriteString(Screen.CursorTop,cursor,"".PadRight(max_length));
			while(!done){
				Screen.SetCursorPosition(cursor,Screen.CursorTop);
				command = ReadKey();
				if((command.KeyChar >= '!' && command.KeyChar <= '~') || command.KeyChar == ' '){
					if(s.Length < max_length){
						s = s + command.KeyChar;
						Screen.WriteChar(Screen.CursorTop,cursor,command.KeyChar);
						++cursor;
					}
				}
				else{
					if(command.Key == ConsoleKey.Backspace && s.Length > 0){
						s = s.Substring(0,s.Length-1);
						--cursor;
						Screen.WriteChar(Screen.CursorTop,cursor,' ');
						Screen.SetCursorPosition(cursor,Screen.CursorTop);
					}
					else{
						if(command.Key == ConsoleKey.Escape){
							return "";
						}
						else{
							if(command.Key == ConsoleKey.Enter){
								if(s.Length == 0){
									return "";
								}
								done = true;
							}
						}
					}
				}
			}
			return s;
		}
		//public static bool KeyIsDown(Key key){ return Screen.gl.KeyIsDown(key);}
		private static bool AnyModifierHeld(){
			return Screen.gl.KeyIsDown(Key.LAlt) || Screen.gl.KeyIsDown(Key.RAlt) || Screen.gl.KeyIsDown(Key.LControl) || Screen.gl.KeyIsDown(Key.RControl) || Screen.gl.KeyIsDown(Key.LShift) || Screen.gl.KeyIsDown(Key.RShift);
		}
		private static bool ModifierHeld(ConsoleModifiers mod){
			switch(mod){
			case ConsoleModifiers.Alt:
			return Screen.gl.KeyIsDown(Key.LAlt) || Screen.gl.KeyIsDown(Key.RAlt);
			case ConsoleModifiers.Control:
			return Screen.gl.KeyIsDown(Key.LControl) || Screen.gl.KeyIsDown(Key.RControl);
			case ConsoleModifiers.Shift:
			return Screen.gl.KeyIsDown(Key.LShift) || Screen.gl.KeyIsDown(Key.RShift);
			default:
			return false;
			}
		}
		public static void KeyDownHandler(object sender,KeyboardKeyEventArgs args){
			if(!Input.KeyPressed){
				ConsoleKey ck = Input.GetConsoleKey(args.Key);
				if(ck != ConsoleKey.NoName){
					bool alt = Screen.gl.KeyIsDown(Key.LAlt) || Screen.gl.KeyIsDown(Key.RAlt);
					bool shift = Screen.gl.KeyIsDown(Key.LShift) || Screen.gl.KeyIsDown(Key.RShift);
					bool ctrl = Screen.gl.KeyIsDown(Key.LControl) || Screen.gl.KeyIsDown(Key.RControl);
					if(ck == ConsoleKey.Enter && alt){
						if(Screen.gl.FullScreen){
							Screen.gl.FullScreen = false;
							Screen.gl.WindowState = WindowState.Normal;
						}
						else{
							Screen.gl.FullScreen = true;
							Screen.gl.WindowState = WindowState.Fullscreen;
						}
					}
					else{
						Input.KeyPressed = true;
						Input.LastKey = new ConsoleKeyInfo(Input.GetChar(ck,shift),ck,shift,alt,ctrl);
					}
				}
				MouseUI.RemoveHighlight();
				MouseUI.RemoveMouseover();
			}
		}
		public static void MouseMoveHandler(object sender,MouseMoveEventArgs args){
			if(MouseUI.IgnoreMouseMovement){
				return;
			}
			int row = (args.Y - Screen.gl.Viewport.Y) / Screen.cellHeight;
			int col = (args.X - Screen.gl.Viewport.X) / Screen.cellWidth;
			switch(MouseUI.Mode){
			case MouseMode.Targeting:
			{
				int map_row = row - Global.MAP_OFFSET_ROWS;
				int map_col = col - Global.MAP_OFFSET_COLS;
				Button b = MouseUI.GetButton(row,col);
				if(MouseUI.Highlighted != null && MouseUI.Highlighted != b){
					MouseUI.RemoveHighlight();
				}
				if(args.XDelta == 0 && args.YDelta == 0){
					return; //don't re-highlight immediately after a click
				}
				if(b != null){
					if(b != MouseUI.Highlighted){
						MouseUI.Highlighted = b;
						colorchar[,] array = new colorchar[b.height,b.width];
						for(int i=0;i<b.height;++i){
							for(int j=0;j<b.width;++j){
								array[i,j] = Screen.Char(i + b.row,j + b.col);
								array[i,j].bgcolor = Color.Blue;
							}
						}
						Screen.UpdateGLBuffer(b.row,b.col,array);
					}
				}
				else{
					if(!Input.KeyPressed){
						if(!MouseUI.mouselook_objects.BoundsCheck(row,col)){
							//UI.MapCursor = new pos(-1,-1);
							Input.KeyPressed = true;
							const ConsoleKey key = ConsoleKey.F22;
							Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
						}
						else{
							if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
								if(map_row != UI.MapCursor.row || map_col != UI.MapCursor.col){
									UI.MapCursor = new pos(map_row,map_col);
									Input.KeyPressed = true;
									const ConsoleKey key = ConsoleKey.F21;
									Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
								}
							}
							else{
								PhysicalObject o = MouseUI.mouselook_objects[row,col];
								if(o != null && !UI.viewing_map_shrine_info){
									if(!o.p.Equals(UI.MapCursor)){
										UI.SetMapCursor(o.p,map_col < 0);
										Input.KeyPressed = true;
										const ConsoleKey key = ConsoleKey.F21;
										Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
									}
								}
								else{ // off the map, and not hovering over a status bar object
									if(map_row != UI.MapCursor.row || map_col != UI.MapCursor.col){
										//UI.MapCursor = new pos(map_row,map_col);
										Input.KeyPressed = true;
										const ConsoleKey key = ConsoleKey.F22;
										Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
									}
								}
							}
						}
					}
					/*if(!Input.KeyPressed && (map_row != UI.MapCursor.row || map_col != UI.MapCursor.col) && !KeyIsDown(Key.LControl) && !KeyIsDown(Key.RControl)){
						UI.MapCursor = new pos(map_row,map_col);
						Input.KeyPressed = true;
						if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
							ConsoleKey key = ConsoleKey.F21;
							Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
						}
						else{
							ConsoleKey key = ConsoleKey.F22;
							Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
						}
					}*/
				}
				break;
			}
			case MouseMode.Directional:
			{
				int map_row = row - Global.MAP_OFFSET_ROWS;
				int map_col = col - Global.MAP_OFFSET_COLS;
				if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
					int dir = Actor.player.DirectionOf(new pos(map_row,map_col));
					pos p = Actor.player.p.PosInDir(dir);
					Button dir_b = MouseUI.GetButton(Global.MAP_OFFSET_ROWS + p.row,Global.MAP_OFFSET_COLS + p.col);
					if(MouseUI.Highlighted != null && MouseUI.Highlighted != dir_b){
						MouseUI.RemoveHighlight();
					}
					if(dir_b != null && dir_b != MouseUI.Highlighted){
						MouseUI.Highlighted = dir_b;
						colorchar[,] array = new colorchar[1,1];
						array[0,0] = Screen.Char(Global.MAP_OFFSET_ROWS + p.row,Global.MAP_OFFSET_COLS + p.col);
						array[0,0].bgcolor = Color.Blue;
						Screen.UpdateGLBuffer(dir_b.row,dir_b.col,array);
					}
				}
				else{
					if(MouseUI.Highlighted != null){
						MouseUI.RemoveHighlight();
					}
				}
				break;
			}
			default:
			{
				Button b = MouseUI.GetButton(row,col);
				if(MouseUI.Highlighted != null && MouseUI.Highlighted != b){
					MouseUI.RemoveHighlight();
				}
				if(args.XDelta == 0 && args.YDelta == 0){
					return; //don't re-highlight immediately after a click
				}
				if(b != null && b != MouseUI.Highlighted){
					MouseUI.Highlighted = b;
					colorchar[,] array = new colorchar[b.height,b.width];
					for(int i=0;i<b.height;++i){
						for(int j=0;j<b.width;++j){
							array[i,j] = Screen.Char(i + b.row,j + b.col);
							array[i,j].bgcolor = Color.Blue;
						}
					}
					Screen.UpdateGLBuffer(b.row,b.col,array);
				}
				else{
					if(MouseUI.Mode == MouseMode.Map){
						if(!MouseUI.mouselook_objects.BoundsCheck(row,col)){
							UI.MapCursor = new pos(-1,-1);
							break;
						}
						PhysicalObject o = MouseUI.mouselook_objects[row,col];
						int map_row = row - Global.MAP_OFFSET_ROWS;
						int map_col = col - Global.MAP_OFFSET_COLS;
						if(MouseUI.VisiblePath && o == null){
							if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
								o = Actor.M.tile[map_row,map_col];
							}
						}
						if(MouseUI.mouselook_current_target != null && (o == null || !o.p.Equals(MouseUI.mouselook_current_target.p))){
							MouseUI.RemoveMouseover();
						}
						if(o == null){
							UI.MapCursor = new pos(-1,-1);
						}
						else{
							if(MouseUI.mouselook_current_target == null || !o.p.Equals(MouseUI.mouselook_current_target.p)){
								UI.SetMapCursor(o.p,map_col < 0);
								MouseUI.mouselook_current_target = o;
								bool description_on_right = false;
								int max_length = MouseUI.MaxDescriptionBoxLength;
								if(o.col <= 32){
									description_on_right = true;
								}
								List<colorstring> desc_box = null;
								Actor a = o as Actor;
								if(a != null){
									desc_box = Actor.MonsterDescriptionBox(a,true,max_length);
								}
								else{
									Item i = o as Item;
									if(i != null){
										desc_box = UI.ItemDescriptionBox(i,true,true,max_length);
									}
								}
								if(desc_box != null){
									int h = desc_box.Count;
									int w = desc_box[0].Length();
									MouseUI.mouselook_current_desc_area = new System.Drawing.Rectangle(description_on_right? Global.COLS - w : 0,0,w,h);
									int player_r = Actor.player.row;
									int player_c = Actor.player.col;
									colorchar[,] array = new colorchar[h,w];
									if(description_on_right){
										for(int i=0;i<h;++i){
											for(int j=0;j<w;++j){
												array[i,j] = desc_box[i][j];
												if(i == player_r && j + Global.COLS - w == player_c){
													Screen.CursorVisible = false;
													player_r = -1; //to prevent further attempts to set CV to false
												}
											}
										}
										Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS + Global.COLS - w,array);
									}
									else{
										for(int i=0;i<h;++i){
											for(int j=0;j<w;++j){
												array[i,j] = desc_box[i][j];
												if(i == player_r && j == player_c){
													Screen.CursorVisible = false;
													player_r = -1;
												}
											}
										}
										Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,array);
									}
								}
								if(MouseUI.VisiblePath){
									if(o != Actor.player && o.p.Equals(Actor.player.p)){
										MouseUI.mouse_path = new List<pos>{o.p};
									}
									else{
										MouseUI.mouse_path = Actor.player.GetPlayerTravelPath(o.p);
										if(MouseUI.mouse_path.Count == 0){
											foreach(Tile t in Actor.M.TilesByDistance(o.row,o.col,true,true)){
												if(t.passable){
													MouseUI.mouse_path = Actor.player.GetPlayerTravelPath(t.p);
													break;
												}
											}
										}
									}
									pos box_start = new pos(0,0);
									int box_h = -1;
									int box_w = -1;
									if(desc_box != null){
										box_h = desc_box.Count;
										box_w = desc_box[0].Length();
										if(description_on_right){
											box_start = new pos(0,Global.COLS - box_w);
										}
									}
									foreach(pos p in MouseUI.mouse_path){
										if(desc_box != null && p.row < box_start.row + box_h && p.row >= box_start.row && p.col < box_start.col + box_w && p.col >= box_start.col){
											continue;
										}
										colorchar cch = Screen.MapChar(p.row,p.col);
										cch.bgcolor = Color.DarkGreen;
										if(cch.color == Color.DarkGreen){
											cch.color = Color.Black;
										}
										//Game.gl.UpdateVertexArray(p.row+Global.MAP_OFFSET_ROWS,p.col+Global.MAP_OFFSET_COLS,text_surface,0,(int)cch.c,cch.color.GetFloatValues(),cch.bgcolor.GetFloatValues());
										Screen.gl.UpdateOtherSingleVertex(Screen.textSurface,U.Get1DIndex(p.row+Global.MAP_OFFSET_ROWS,p.col+Global.MAP_OFFSET_COLS,Global.SCREEN_W),(int)cch.c,0,cch.color.GetFloatValues(),cch.bgcolor.GetFloatValues());
									}
									if(MouseUI.mouse_path != null && MouseUI.mouse_path.Count == 0){
										MouseUI.mouse_path = null;
									}
								}
							}
						}
					}
				}
				break;
			}
			}
		}
		public static void MouseClickHandler(object sender,MouseButtonEventArgs args){
			if(MouseUI.IgnoreMouseClicks){
				return;
			}
			if(args.Button == MouseButton.Middle){
				HandleMiddleClick();
				return;
			}
			if(args.Button == MouseButton.Right){
				HandleRightClick();
				return;
			}
			int row = (args.Y - Screen.gl.Viewport.Y) / Screen.cellHeight;
			int col = (args.X - Screen.gl.Viewport.X) / Screen.cellWidth;
			if(!MouseUI.mouselook_objects.BoundsCheck(row,col)){
				return;
			}
			Button b = MouseUI.GetButton(row,col);
			if(!Input.KeyPressed){
				Input.KeyPressed = true;
				if(b != null){
					bool shifted = (b.mods & ConsoleModifiers.Shift) == ConsoleModifiers.Shift;
					Input.LastKey = new ConsoleKeyInfo(Input.GetChar(b.key,shifted),b.key,shifted,false,false);
				}
				else{
					switch(MouseUI.Mode){
					case MouseMode.Map:
					{
						int map_row = row - Global.MAP_OFFSET_ROWS;
						int map_col = col - Global.MAP_OFFSET_COLS;
						bool status_click = false;
						if(MouseUI.mouselook_objects[row,col] != null){
							if(map_col < 0){
								status_click = true;
							}
							map_row = MouseUI.mouselook_objects[row,col].row;
							map_col = MouseUI.mouselook_objects[row,col].col;
						}
						if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
							if(map_row == Actor.player.row && map_col == Actor.player.col){
								bool done = false;
								if(status_click){
									done = true;
									Tile t = Actor.M.tile[map_row,map_col];
									if(t.inv != null || t.Is(TileType.CHEST,TileType.BLAST_FUNGUS) || t.IsShrine()){
										Input.LastKey = new ConsoleKeyInfo('g',ConsoleKey.G,false,false,false);
									}
									else{
										if(t.Is(TileType.STAIRS)){
											Input.LastKey = new ConsoleKeyInfo('>',ConsoleKey.OemPeriod,true,false,false);
										}
										else{
											if(t.Is(TileType.POOL_OF_RESTORATION)){
												Input.LastKey = new ConsoleKeyInfo('d',ConsoleKey.D,false,false,false);
											}
											else{
												done = false;
											}
										}
									}
								}
								if(!done){
									Input.LastKey = new ConsoleKeyInfo('5',ConsoleKey.NumPad5,false,false,false);
								}
							}
							else{
								if(ModifierHeld(ConsoleModifiers.Control) || (Math.Abs(map_row-Actor.player.row) <= 1 && Math.Abs(map_col-Actor.player.col) <= 1)){
									int rowchange = 0;
									int colchange = 0;
									if(map_row > Actor.player.row){
										rowchange = 1;
									}
									else{
										if(map_row < Actor.player.row){
											rowchange = -1;
										}
									}
									if(map_col > Actor.player.col){
										colchange = 1;
									}
									else{
										if(map_col < Actor.player.col){
											colchange = -1;
										}
									}
									ConsoleKey dir_key = (ConsoleKey)(ConsoleKey.NumPad0 + Actor.player.DirectionOf(Actor.M.tile[Actor.player.row + rowchange,Actor.player.col + colchange]));
									Input.LastKey = new ConsoleKeyInfo(Input.GetChar(dir_key,false),dir_key,false,false,false);
								}
								else{
									Tile nearest = Actor.M.tile[map_row,map_col];
									Actor.player.path = Actor.player.GetPlayerTravelPath(nearest.p);
									if(Actor.player.path.Count > 0){
										Actor.player.path.StopAtBlockingTerrain();
										if(Actor.player.path.Count > 0){
											Actor.interrupted_path = new pos(-1,-1);
											ConsoleKey path_key = (ConsoleKey)(ConsoleKey.NumPad0 + Actor.player.DirectionOf(Actor.player.path[0]));
											Input.LastKey = new ConsoleKeyInfo(Input.GetChar(path_key,false),path_key,false,false,false);
											Actor.player.path.RemoveAt(0);
											if(nearest.inv != null || nearest.Is(TileType.CHEST)){
												Actor.grab_item_at_end_of_path = true;
											}
										}
										else{
											Input.LastKey = new ConsoleKeyInfo(' ',ConsoleKey.Spacebar,false,false,false);
										}
									}
									else{
										//int distance_of_first_passable = -1;
										//List<Tile> passable_tiles = new List<Tile>();
										foreach(Tile t in Actor.M.TilesByDistance(map_row,map_col,true,true)){
											//if(distance_of_first_passable != -1 && nearest.DistanceFrom(t) > distance_of_first_passable){
											//nearest = passable_tiles.Last();
											if(t.passable){
												nearest = t;
												Actor.player.path = Actor.player.GetPath(nearest.row,nearest.col,-1,true,true,Actor.UnknownTilePathingPreference.UnknownTilesAreOpen);
												Actor.player.path.StopAtBlockingTerrain();
												break;
											}
											/*}
											if(t.passable){
												distance_of_first_passable = nearest.DistanceFrom(t);
												passable_tiles.Add(t);
											}*/
										}
										if(Actor.player.path.Count > 0){
											Actor.interrupted_path = new pos(-1,-1);
											ConsoleKey path_key = (ConsoleKey)(ConsoleKey.NumPad0 + Actor.player.DirectionOf(Actor.player.path[0]));
											Input.LastKey = new ConsoleKeyInfo(Input.GetChar(path_key,false),path_key,false,false,false);
											Actor.player.path.RemoveAt(0);
											if(nearest.inv != null || nearest.Is(TileType.CHEST)){
												Actor.grab_item_at_end_of_path = true;
											}
										}
										else{
											Input.LastKey = new ConsoleKeyInfo(' ',ConsoleKey.Spacebar,false,false,false);
										}
									}
								}
							}
						}
						else{
							Input.LastKey = new ConsoleKeyInfo((char)13,ConsoleKey.Enter,false,false,false);
						}
						break;
					}
					case MouseMode.Directional:
					{
						int map_row = row - Global.MAP_OFFSET_ROWS;
						int map_col = col - Global.MAP_OFFSET_COLS;
						if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
							int dir = Actor.player.DirectionOf(new pos(map_row,map_col));
							pos p = Actor.player.p.PosInDir(dir);
							Button dir_b = MouseUI.GetButton(Global.MAP_OFFSET_ROWS + p.row,Global.MAP_OFFSET_COLS + p.col);
							if(dir_b != null){
								bool shifted = (dir_b.mods & ConsoleModifiers.Shift) == ConsoleModifiers.Shift;
								Input.LastKey = new ConsoleKeyInfo(Input.GetChar(dir_b.key,shifted),dir_b.key,shifted,false,false);
							}
						}
						else{
							Input.LastKey = new ConsoleKeyInfo((char)27,ConsoleKey.Escape,false,false,false);
						}
						break;
					}
					case MouseMode.Targeting:
					{
						int map_row = row - Global.MAP_OFFSET_ROWS;
						int map_col = col - Global.MAP_OFFSET_COLS;
						if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
							Input.LastKey = new ConsoleKeyInfo((char)13,ConsoleKey.Enter,false,false,false);
						}
						else{
							if(MouseUI.mouselook_objects.BoundsCheck(row,col) && MouseUI.mouselook_objects[row,col] != null){
								Input.LastKey = new ConsoleKeyInfo((char)13,ConsoleKey.Enter,false,false,false);
							}
							else{
								Input.LastKey = new ConsoleKeyInfo((char)27,ConsoleKey.Escape,false,false,false);
							}
						}
						break;
					}
					case MouseMode.YesNoPrompt:
					Input.LastKey = new ConsoleKeyInfo('y',ConsoleKey.Y,false,false,false);
					break;
					case MouseMode.Inventory:
					Input.LastKey = new ConsoleKeyInfo('a',ConsoleKey.A,false,false,false);
					break;
					case MouseMode.ScrollableMenu:
					if(AnyModifierHeld()){
						Input.LastKey = new ConsoleKeyInfo((char)8,ConsoleKey.Backspace,false,false,false);
					}
					else{
						Input.LastKey = new ConsoleKeyInfo((char)13,ConsoleKey.Enter,false,false,false);
					}
					break;
					default:
					Input.LastKey = new ConsoleKeyInfo((char)13,ConsoleKey.Enter,false,false,false);
					break;
					}
				}
			}
			MouseUI.RemoveHighlight();
			MouseUI.RemoveMouseover();
		}
		public static void HandleRightClick(){
			if(!Input.KeyPressed){
				Input.KeyPressed = true;
				switch(MouseUI.Mode){
				case MouseMode.YesNoPrompt:
				Input.LastKey = new ConsoleKeyInfo('n',ConsoleKey.N,false,false,false);
				break;
				case MouseMode.Map:
				Input.LastKey = new ConsoleKeyInfo('i',ConsoleKey.I,false,false,false);
				break;
				default:
				Input.LastKey = new ConsoleKeyInfo((char)27,ConsoleKey.Escape,false,false,false);
				break;
				}
			}
			MouseUI.RemoveHighlight();
			MouseUI.RemoveMouseover();
		}
		public static void HandleMiddleClick(){
			if(!Input.KeyPressed){
				Input.KeyPressed = true;
				switch(MouseUI.Mode){
				case MouseMode.Map:
				Input.LastKey = new ConsoleKeyInfo('v',ConsoleKey.V,false,false,false);
				break;
				case MouseMode.Targeting:
				Input.LastKey = new ConsoleKeyInfo('X',ConsoleKey.X,true,false,false);
				break;
				default:
				Input.LastKey = new ConsoleKeyInfo((char)27,ConsoleKey.Escape,false,false,false);
				break;
				}
			}
			MouseUI.RemoveHighlight();
			MouseUI.RemoveMouseover();
		}
		public static void MouseWheelHandler(object sender,MouseWheelEventArgs args){
			if(!Input.KeyPressed){
				if(args.Delta > 0){
					switch(MouseUI.Mode){
					case MouseMode.ScrollableMenu:
					Input.KeyPressed = true;
					if(AnyModifierHeld()){
						Input.LastKey = new ConsoleKeyInfo(Input.GetChar(ConsoleKey.PageUp,false),ConsoleKey.PageUp,false,false,false);
					}
					else{
						Input.LastKey = new ConsoleKeyInfo('8',ConsoleKey.NumPad8,false,false,false);
					}
					break;
					case MouseMode.Targeting:
					Input.KeyPressed = true;
					Input.LastKey = new ConsoleKeyInfo((char)9,ConsoleKey.Tab,true,false,false);
					break;
					case MouseMode.Map:
					Input.KeyPressed = true;
					Input.LastKey = new ConsoleKeyInfo((char)9,ConsoleKey.Tab,false,false,false);
					break;
					}
				}
				if(args.Delta < 0){
					switch(MouseUI.Mode){
					case MouseMode.ScrollableMenu:
					Input.KeyPressed = true;
					if(AnyModifierHeld()){
						Input.LastKey = new ConsoleKeyInfo(Input.GetChar(ConsoleKey.PageDown,false),ConsoleKey.PageDown,false,false,false);
					}
					else{
						Input.LastKey = new ConsoleKeyInfo('2',ConsoleKey.NumPad2,false,false,false);
					}
					break;
					case MouseMode.Targeting:
					Input.KeyPressed = true;
					Input.LastKey = new ConsoleKeyInfo((char)9,ConsoleKey.Tab,false,false,false);
					break;
					case MouseMode.Map:
					Input.KeyPressed = true;
					Input.LastKey = new ConsoleKeyInfo((char)9,ConsoleKey.Tab,false,false,false);
					break;
					}
				}
			}
			MouseUI.RemoveHighlight();
			MouseUI.RemoveMouseover();
		}
		public static void MouseLeaveHandler(object sender,EventArgs args){
			MouseUI.RemoveHighlight();
		}
		public static void OnClosing(object sender,System.ComponentModel.CancelEventArgs e){
			if(Screen.gl.NoClose && !Input.KeyPressed && MouseUI.Mode == MouseMode.Map){
				Input.KeyPressed = true;
				Input.LastKey = new ConsoleKeyInfo('q',ConsoleKey.Q,false,false,false);
			}
		}
		public static void HandleResize(){ HandleResize(false); }
		public static void HandleResize(bool forceBorder = false){
			int potentialWidth = Screen.gl.ClientRectangle.Width / Global.SCREEN_W;
			int potentialHeight = Screen.gl.ClientRectangle.Height / Global.SCREEN_H;
			int selectedIdx = 0;
			int[] fontWidths = new int[]{  6, 8, 8,10,12,12,14,15,16,18,21}; //since these are ordered by width, the greatest width will win in conflicts - for
			int[] fontHeights = new int[]{12,12,16,20,18,24,28,27,32,36,38}; //  example, if 12x20 is the potential, then 12x18 will be selected over 10x20.
			//t[] fontWidths = new int[]{  6, 8, 8,10,11,12,12,14,14,15,16,16,18,21}; //since these are ordered by width, the greatest width will win in conflicts - for
			//t[] fontHeights = new int[]{12,12,16,20,27,18,24,28,36,27,24,32,36,38}; //  example, if 12x20 is the potential, then 12x18 will be selected over 10x20.
			for(int i=0;i<fontWidths.GetLength(0);++i){
				if(potentialWidth >= fontWidths[i] && potentialHeight >= fontHeights[i]){
					selectedIdx = i;
				}
			}
			if(Screen.cellHeight != fontHeights[selectedIdx] || Screen.cellWidth != fontWidths[selectedIdx]){ //change font if needed
				Screen.cellHeight = fontHeights[selectedIdx];
				Screen.cellWidth = fontWidths[selectedIdx];
				string newFont = GetFontFilename(Screen.cellWidth,Screen.cellHeight);
				int fontPadding = GetFontPadding(newFont);
				Screen.textSurface.texture = Texture.Create(newFont,Screen.currentFont,true);
				Screen.currentFont = newFont;
				Screen.textSurface.texture.Sprite.Clear();
				SpriteType.DefineSingleRowSprite(Screen.textSurface,Screen.cellWidth,fontPadding);
				Screen.cursorSurface.texture = Screen.textSurface.texture;
				Screen.textSurface.layouts.Clear();
				CellLayout.CreateGrid(Screen.textSurface,Global.SCREEN_H,Global.SCREEN_W,Screen.cellHeight,Screen.cellWidth,0,0);
				Screen.cursorSurface.layouts.Clear();
				CellLayout.CreateGrid(Screen.cursorSurface,1,1,2,Screen.cellWidth,0,0);
			}
			if(Screen.gl.FullScreen || forceBorder){ //then, was fullscreen toggled?
				int xOffset = (Screen.gl.ClientRectangle.Width - Screen.cellWidth*Global.SCREEN_W) / 2;
				int yOffset = (Screen.gl.ClientRectangle.Height - Screen.cellHeight*Global.SCREEN_H) / 2;
				Screen.gl.SetViewport(xOffset,yOffset,Screen.cellWidth*Global.SCREEN_W,Screen.cellHeight*Global.SCREEN_H);
			}
			else{
				Screen.gl.ClientSize = new System.Drawing.Size(Screen.cellWidth * Global.SCREEN_W,Screen.cellHeight * Global.SCREEN_H);
				Screen.gl.SetViewport(0,0,Screen.gl.ClientRectangle.Width,Screen.gl.ClientRectangle.Height);
			}
			Screen.cursorSurface.DefaultUpdatePositions();
			Screen.UpdateCursor(Screen.CursorVisible);
			Screen.textSurface.DefaultUpdatePositions();
			Screen.UpdateGLBuffer(0,0,Global.SCREEN_H-1,Global.SCREEN_W-1);
		}
		private static string GetFontFilename(int w,int h){
			return Global.ForaysImageResources + $"font{w}x{h}.png";
		}
		private static int GetFontPadding(string filename){
			if(filename == Global.ForaysImageResources + "font8x16.png") return 1;
			return 0;
		}
	}
}

