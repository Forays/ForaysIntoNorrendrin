/*Copyright (c) 2011-2016  Derrick Creamer
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
using ForaysUtilities;
namespace Forays{
	public enum Priority { Minor = -1, Normal = 0, Important = 1 };
	public class MessageBuffer { //todo: move some of these fields and methods around for better organization.
		public MessageBuffer(Game g) {
			game = g;
			MessageVisibility = MessageVisibilityLevel.Default;
			MaxLength = Global.COLS;
			buffer = new StringWrapBuffer(NumLines,MaxLength,null,new char[] {' '});
			buffer.ReservedSpace = more.Length;
			buffer.BufferFull += HandleOverflow;
			log = new List<string>();
			for(int i=0;i<Global.ROWS-1;++i) log.Add(""); //These blank lines cause the very first message to appear at the bottom of the previous message screen.
			repetitionCount = 0;
			interruptPlayer = false;
			HideRepeatCountStrings = new List<string> { "You can't move!","You're rooted to the ground!" }; //todo: is this the best place for these?
		}
		public Game game;
		public void Add(string message,params PhysicalObject[] objs) { Add(Priority.Normal,message,objs); }
		public void Add(Priority importance,string message,params PhysicalObject[] objs) {
			if(string.IsNullOrEmpty(message)
				|| MessageVisibility == MessageVisibilityLevel.None
				|| (MessageVisibility == MessageVisibilityLevel.ImportantOnly && importance != Priority.Important)) {
				return;
			}
			bool sightChecked = false;
			bool seen = false;
			if(objs != null) {
				foreach(PhysicalObject o in objs) {
					if(o != null) {
						sightChecked = true;
						if(game.player.CanSee(o)) {
							seen = true;
							break;
						}
					}
				}
			}
			if(seen || !sightChecked || MessageVisibility == MessageVisibilityLevel.All) {
				if(importance != Priority.Minor) interruptPlayer = true;
				buffer.Add(message.Capitalize());
				if(importance == Priority.Important) Print(true);
			}
		}
		public void Print(bool requireMorePrompt) {
			if(requireMorePrompt) buffer.ConfirmReservedSpace();
			DisplayLines(buffer.Clear(),requireMorePrompt,true);
			if(interruptPlayer) {
				game.player.Interrupt();
				interruptPlayer = false;
			}
		}
		public void DisplayContents() {
			DisplayLines(buffer.Contents,false,false);
		}
		public List<string> GetMessageLog() { return new List<string>(log); }

		public string[] SaveMessages() { return null; }
		public int SavePosition() { return 0; } //todo: These 4 will be changed soon with a new serialization update. I'm leaving them broken until then.
		public int SaveNumMessages() { return 0; }
		public void LoadMessagesAndPosition(string[] s,int p,int num_msgs) { }

		public enum MessageVisibilityLevel { Default,All,ImportantOnly,None };
		public MessageVisibilityLevel MessageVisibility;
		public List<string> HideRepeatCountStrings;
		protected StringWrapBuffer buffer;
		protected List<string> log;
		protected int repetitionCount;
		protected static readonly string more = " [more] ";
		protected const int NumLines = 3;
		protected readonly int MaxLength;
		protected bool interruptPlayer;
		protected void HandleOverflow(List<string> lines) {
			DisplayLines(lines,true,true);
			//game.M.Draw(); //todo: necessary? and wouldn't it need to happen *before* the [more]?
			if(interruptPlayer) {
				game.player.Interrupt();
				interruptPlayer = false;
			}
		}
		protected void DisplayLines(List<string> lines,bool morePrompt,bool addToLog) {
			for(int i=0;i<lines.Count;++i) lines[i] = RemoveTrailingSpaces(lines[i]);
			bool repeated = false;
			bool printCount = true;
			if(lines.Count == 1 && log.Count > 0) {
				string last = GetPreviousMessage(0);
				string lastWithoutCount = last;
				if(repetitionCount > 0) {
					int repIdx = last.LastIndexOf(" (x" + (repetitionCount + 1) + ")");
					if(repIdx != -1) {
						lastWithoutCount = last.Substring(0,repIdx);
					}
                }
				if(lines[0] == lastWithoutCount) { //if the new line matches the last one, verify that there's room for the (xN)
					repeated = true;
					if(HideRepeatCountStrings.Contains(lastWithoutCount)) {
						printCount = false;
					}
					else {
						int max = MaxLength;
						if(morePrompt) max -= more.Length;
						if((lastWithoutCount + " (x" + (repetitionCount + 2) + ")").Length > max) {
							repeated = false;
						}
					}
				}
			}
			int numPrev = NumLines - lines.Count;
			int prevStartIdx = numPrev - 1;
			if(repeated) prevStartIdx++;
			Screen.CursorVisible = false;
			for(int i=0;i<numPrev;++i) {
				Screen.WriteString(i,Global.MAP_OFFSET_COLS,GetPreviousMessage(prevStartIdx - i).PadToMapSize(),Color.DarkGray);
			}
			if(lines.Count == 0) return;
			for(int i = 0;i<lines.Count;++i) {
				Screen.WriteString(i + numPrev,Global.MAP_OFFSET_COLS,lines[i].PadToMapSize());
			}
			int extraIdx = lines[lines.Count - 1].Length + Global.MAP_OFFSET_COLS;
			if(repeated) {
				if(printCount) {
					string xCount = " (x" + (repetitionCount + 2) + ")";
					Screen.WriteString(NumLines - 1,extraIdx,xCount,Color.DarkGray);
					extraIdx += xCount.Length;
					if(addToLog) {
						log[log.Count - 1] = lines[lines.Count - 1] + xCount;
					}
				}
				if(addToLog) ++repetitionCount;
			}
			else {
				if(addToLog) {
					repetitionCount = 0;
					AddToLog(lines);
				}
			}
			if(morePrompt) {
				Screen.WriteString(NumLines - 1,extraIdx,more,Color.Yellow);
				MouseUI.PushButtonMap();
				Screen.SetCursorPosition(extraIdx + more.Length - 1,NumLines - 1);
				Screen.CursorVisible = true;
				Input.ReadKey();
				MouseUI.PopButtonMap();
			}
		}
		/// <summary>
		/// Get the nth-from-last message. (Therefore, 0 is the most recent.)
		/// </summary>
		protected string GetPreviousMessage(int n) {
			if(log.Count - 1 - n < 0) return "";
			return log[log.Count - 1 - n];
		}
		protected void AddToLog(IEnumerable<string> lines) {
			foreach(string s in lines) log.Add(s);
		}
		protected static string RemoveTrailingSpaces(string s) {
			int idx = s.Length;
			for(int i=s.Length - 1;i>=0;--i) {
				if(s[i] == ' ') idx = i;
				else break;
			}
			if(idx == s.Length) return s;
			return s.Substring(0,idx);
		}
	}
}

