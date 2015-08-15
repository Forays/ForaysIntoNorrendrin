using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using OpenTK.Input;
namespace Forays{
	public static class Input{
		public static bool KeyPressed = false;
		public static ConsoleKeyInfo LastKey;

		public static Dictionary<ConsoleKeyInfo,ConsoleKeyInfo> global_rebindings = new Dictionary<ConsoleKeyInfo,ConsoleKeyInfo>();
		public static Dictionary<ConsoleKeyInfo,ConsoleKeyInfo> action_rebindings = new Dictionary<ConsoleKeyInfo,ConsoleKeyInfo>();

		public static bool KeyIsAvailable(){
			if(Screen.GLMode){
				return KeyPressed;
			}
			return Console.KeyAvailable;
		}
		public static void FlushInput(){
			while(KeyIsAvailable()){
				ReadKey();
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
				if(!Game.GLUpdate()){
					Global.Quit();
				}
				if(Screen.CursorVisible){
					TimeSpan time = GLGame.Timer.Elapsed;
					if(time.Seconds >= 1){
						Screen.UpdateCursor(true);
						GLGame.Timer.Reset();
						GLGame.Timer.Start();
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
				"# are pressed, do it like this:  \"all t:m\".",
				"# ",
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
				"# ",
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
	}
}

