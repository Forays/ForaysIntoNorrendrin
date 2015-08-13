/*Copyright (c) 2011-2015  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
using Utilities;
namespace Forays{
	public class Buffer{
		private int max_length;
		private List<string> str = new List<string>();
		private string overflow;
		private int log_length = 1000;
		private string[] log;
		private int num_messages;
		private int position;

		public static Map M;
		public static Actor player;
		public Buffer(Game g){
			max_length = Global.COLS; //because the message window runs along the top of the map
			str.Add("");
			overflow = "";
			log = new string[log_length];
			for(int i=0;i<log_length;++i){
				log[i] = "";
			}
			position = 18;
			num_messages = position;
			M = g.M;
			player = g.player;
		}
		public void Add(string s,params PhysicalObject[] objs){ //if there's at least one object, the player must be able to
			bool add = false;
			if(objs != null && objs.Length > 0){ //see at least one of them. if not, no message is added. 
				foreach(PhysicalObject obj in objs){
					if(obj == player || (obj != null && player.CanSee(obj))){
						add = true;
						break;
					}
				}
			}
			else{
				add = true;
			}
			if(add && s.Length > 0){
				if(Char.IsLetter(s[0])){
					char[] c = s.ToCharArray();
					c[0] = Char.ToUpper(s[0]);
					s = new string(c);
				}
				AddToStr(s);
			}
		}
		private void AddToStr(string s){
			if(s.Length > 0){
				int idx = str.Count - 1;
				str[idx] = str[idx] + s;
				while(str[idx].Length > max_length){
					int extra_space_for_more = 7;
					if(str.Count < 3){
						extra_space_for_more = 1;
					}
					for(int i=max_length-extra_space_for_more;i>=0;--i){
						if(str[idx].Substring(i,1)==" "){
							if(str.Count == 3){
								overflow = str[idx].Substring(i+1);
								str[idx] = str[idx].Substring(0,i+1);
							}
							else{
								str.Add(str[idx].Substring(i+1));
								str[idx] = str[idx].Substring(0,i+1);
								idx++;
							}
							break;
						}
					}
					if(overflow != ""){
						Screen.ResetColors();
						Print(false);
						idx = str.Count - 1;
					}
				}
			}
		}
		public void DisplayNow(string s){ DisplayNow(s,true); }
		public void DisplayNow(string s,bool display_stats){
			if(display_stats){
				player.DisplayStats();
			}
			Screen.CursorVisible = false;
			List<string> strings = new List<string>();
			if(s.Length > max_length){
				for(int i=max_length-1;i>=0;--i){
					if(s.Substring(i,1)==" "){
						strings.Add(s.Substring(0,i+1));
						s = s.Substring(i+1);
						break;
					}
				}
			}
			if(s.Length > max_length){
				for(int i=max_length-1;i>=0;--i){
					if(s.Substring(i,1)==" "){
						strings.Add(s.Substring(0,i+1));
						s = s.Substring(i+1);
						break;
					}
				}
			}
			strings.Add(s);
			for(int i=0;i<3;++i){
				if(3-i > strings.Count){
					Screen.WriteMapString(i-3,0,"".PadToMapSize());
				}
				else{
					Screen.WriteMapString(i-3,0,strings[(i+strings.Count)-3].PadToMapSize());
				}
			}
			Screen.SetCursorPosition(Global.MAP_OFFSET_COLS + s.Length,2);
		}
		public void DisplayNow(){ //displays whatever is in the buffer. used before animations.
			Screen.CursorVisible = false;
			Screen.ResetColors();
			int lines = str.Count;
			bool repeated_message = false;
			string repeated_string = "";
			if(str.LastOrDefault() == ""){
				--lines;
			}
			else{
				string s = str.LastOrDefault();
				int last = (position-1).Modulo(log_length);
				//if(last == -1){ last = 19; }
				string prev = log[last];
				string count = "1";
				int pos = prev.LastIndexOf(" (x");
				if(pos != -1){
					count = prev.Substring(pos+3);
					count = count.Substring(0,count.Length-1);
					prev = prev.Substring(0,pos+1);
				}
				bool too_long_if_repeated = false;
				if(prev.Length + 3 + (Convert.ToInt32(count)+1).ToString().Length > max_length){
					too_long_if_repeated = true;
				}
				if(prev == s && str.Count == 1 && !too_long_if_repeated){ //trying this - only add the (x2) part if it's a single-line message, for ease of reading
					if(s != "You can't move! " && s != "You're rooted to the ground! "){
						repeated_string = prev + "(x" + (Convert.ToInt32(count)+1).ToString() + ")"; //the immobilization messages could be confusing when repeated
					}
					repeated_message = true;
				}
			}
			int offset = 0;
			if(repeated_message){
				offset = 1;
			}
			for(int i=0;i<3;++i){
				bool old_message = true;
				if(3-i <= lines){
					old_message = false;
				}
				if(old_message){
					Screen.WriteMapString(i-3,0,PreviousMessage(offset+3-(i+lines)).PadToMapSize(),Color.DarkGray);
					//Screen.ForegroundColor = ConsoleColor.Gray;
				}
				else{
					if(repeated_message){
						int pos = repeated_string.LastIndexOf(" (x");
						if(pos != -1){
							Screen.WriteMapString(i-3,0,repeated_string.Substring(0,pos));
							Screen.WriteMapString(i-3,pos,repeated_string.Substring(pos).PadToMapSize(),Color.DarkGray);
						}
						else{
							Screen.WriteMapString(i-3,0,repeated_string.PadToMapSize());
						}
					}
					else{
						Screen.WriteMapString(i-3,0,str[(i+lines)-3].PadToMapSize());
					}
				}
			}
		}
		public void Print(bool special_message){
			Screen.CursorVisible = false;
			int idx = str.Count - 1;
			while(special_message && str[idx].Length > max_length - 7){
				for(int i=max_length-8;i>=0;--i){
					if(str[idx].Substring(i,1)==" "){
						overflow = str[idx].Substring(i+1);
						str[idx] = str[idx].Substring(0,i+1);
						break;
					}
				}
				if(overflow != ""){
					Screen.ResetColors();
					Print(false);
					idx = str.Count - 1;
				}
			}
			foreach(string s in str){
				if(s != "You regenerate. " && s != "You rest... " && s != "You breathe in the overwhelming scent of the poppies. " && s != ""){ //eventually this will become a list of ignored strings
					if(!player.HasAttr(AttrType.RESTING)){
						player.Interrupt();
					}
				}
			}
			bool repeated_message = false;
			foreach(string s in str){
				if(s != ""){
					int last = (position-1).Modulo(log_length);
					//if(last == -1){ last = 19; }
					string prev = log[last];
					string count = "1";
					int pos = prev.LastIndexOf(" (x");
					if(pos != -1){
						count = prev.Substring(pos+3);
						count = count.Substring(0,count.Length-1);
						prev = prev.Substring(0,pos+1);
					}
					bool too_long_if_repeated = false;
					if(prev.Length + 3 + (Convert.ToInt32(count)+1).ToString().Length > max_length){
						too_long_if_repeated = true;
					}
					if(prev == s && str.Count == 1 && !too_long_if_repeated){ //trying this - only add the (x2) part if it's a single-line message, for ease of reading
						if(s != "You can't move! " && s != "You're rooted to the ground! "){
							log[last] = prev + "(x" + (Convert.ToInt32(count)+1).ToString() + ")"; //the immobilization messages could be confusing when repeated
						}
						repeated_message = true;
					}
					else{
						log[position] = s;
						position = (position+1).Modulo(log_length);
						if(num_messages < 1000){
							++num_messages;
						}
						//++position;
						//if(position == 20){ position = 0; }
						repeated_message = false;
					}
				}
			}
			int lines = str.Count;
			if(str.LastOrDefault() == ""){
				--lines;
			}
			for(int i=0;i<3;++i){
				bool old_message = true;
				if(3-i <= lines){
					old_message = false;
				}
				if(old_message){
					Screen.WriteMapString(i-3,0,PreviousMessage(3-i).PadToMapSize(),Color.DarkGray);
					//Screen.ForegroundColor = ConsoleColor.Gray;
				}
				else{
					if(repeated_message){
						int pos = PreviousMessage(3-i).LastIndexOf(" (x");
						if(pos != -1){
							Screen.WriteMapString(i-3,0,PreviousMessage(3-i).Substring(0,pos));
							Screen.WriteMapString(i-3,pos,PreviousMessage(3-i).Substring(pos).PadToMapSize(),Color.DarkGray);
							//Screen.ForegroundColor = ConsoleColor.Gray;
						}
						else{
							Screen.WriteMapString(i-3,0,PreviousMessage(3-i).PadToMapSize());
						}
					}
					else{
						Screen.WriteMapString(i-3,0,PreviousMessage(3-i).PadToMapSize());
					}
				}
			}
			if(overflow != "" || special_message == true){
				int cursor_col = str.LastOrDefault().Length + Global.MAP_OFFSET_COLS;
				int cursor_row = Screen.CursorTop;
				if(cursor_row > 2){
					cursor_row = 2; //hack - attempts a quick fix for the [more] appearing at the player's row
				}
				M.Draw();
				Screen.WriteString(cursor_row,cursor_col,"[more]",Color.Yellow);
				MouseUI.PushButtonMap();
				Screen.SetCursorPosition(cursor_col+6,cursor_row);
				//Screen.ForegroundColor = ConsoleColor.Gray;
				Screen.CursorVisible = true;
				Global.ReadKey();
				MouseUI.PopButtonMap();
			}
			str.Clear();
			str.Add("");
			string temp = overflow;
			overflow = "";
			AddToStr(temp);
		}
		public void PrintAll(){
			Screen.ResetColors();
			if(str.LastOrDefault() != ""){
				if(str.LastOrDefault().Length > max_length-7){
					for(int i=max_length-8;i>=0;--i){
						if(str.LastOrDefault().Substring(i,1)==" "){
							overflow = str.LastOrDefault().Substring(i+1);
							str[str.Count-1] = str.LastOrDefault().Substring(0,i+1);
							break;
						}
					}
					Print(true);
					if(str.LastOrDefault() != ""){
						Print(true);
					}
				}
				else{
					Print(true);
				}
			}
		}
		public string Printed(int num){ //Printed(0) is the oldest message, Printed(1) the 2nd oldest...
			if(num_messages < log_length){
				return log[num];
			}
			else{
				return log[(position+num).Modulo(log_length)];
			}
		}
		public string PreviousMessage(int num){ //PreviousMessage(1) is the latest, PreviousMessage(2) is the next latest...
			return log[(position-num).Modulo(log_length)];
		}
		public List<string> GetMessages(){
			List<string> result = new List<string>();
			int count = Math.Min(log_length,num_messages);
			for(int i=0;i<count;++i){
				result.Add(Printed(i));
			}
			return result;
		}
		public string[] SaveMessages(){ return log; }
		public int SavePosition(){ return position; }
		public int SaveNumMessages(){ return num_messages; }
		public void LoadMessagesAndPosition(string[] s,int p,int num_msgs){
			log = s;
			position = p;
			num_messages = num_msgs;
		}
		public void AddDependingOnLastPartialMessage(string s){ //   =|
			if(!str.LastOrDefault().EndsWith(s,true,null)){
				Add(s);
			}
		}
		public void AddIfEmpty(string s){
			if(str.LastOrDefault().Length == 0){
				Add(s);
			}
		}
		public bool YesOrNoPrompt(string s){ return YesOrNoPrompt(s,true); }
		public bool YesOrNoPrompt(string s,bool easy_cancel){
			player.Interrupt();
			MouseUI.PushButtonMap(MouseMode.YesNoPrompt);
			MouseUI.CreateButton(ConsoleKey.Y,false,2,Global.MAP_OFFSET_COLS + s.Length + 1,1,2);
			MouseUI.CreateButton(ConsoleKey.N,false,2,Global.MAP_OFFSET_COLS + s.Length + 4,1,2);
			if(MouseUI.descend_hack && Actor.viewing_more_commands){
				MouseUI.CreateStatsButton(ConsoleKey.N,false,16,1);
				MouseUI.descend_hack = false;
			}
			DisplayNow(s + " (y/n): ");
			Screen.CursorVisible = true;
			while(true){
				switch(Global.ReadKey().KeyChar){
				case 'y':
				case 'Y':
					MouseUI.PopButtonMap();
					return true;
				case 'n':
				case 'N':
					MouseUI.PopButtonMap();
					return false;
				default:
					if(easy_cancel){
						MouseUI.PopButtonMap();
						return false;
					}
					break;
				}
			}
		}
	}
}

