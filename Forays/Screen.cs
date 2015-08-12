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
using System.Threading;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Utilities;
using GLDrawing;
namespace Forays{
	public enum Color{Black,White,Gray,Red,Green,Blue,Yellow,Magenta,Cyan,DarkGray,DarkRed,DarkGreen,DarkBlue,DarkYellow,DarkMagenta,DarkCyan,RandomFire,RandomIce,RandomLightning,RandomBreached,RandomExplosion,RandomGlowingFungus,RandomTorch,RandomDoom,RandomConfusion,RandomDark,RandomBright,RandomRGB,RandomDRGB,RandomRGBW,RandomCMY,RandomDCMY,RandomCMYW,RandomRainbow,RandomAny,OutOfSight,TerrainDarkGray,DarkerGray,Transparent}; //transparent is a special exception. it only works in GL mode.
	public struct colorchar{
		public Color color;
		public Color bgcolor;
		public char c;
		public colorchar(char c_,Color color_,Color bgcolor_){
			color = color_;
			bgcolor = bgcolor_;
			c = c_;
		}
		public colorchar(char c_,Color color_){
			color = color_;
			bgcolor = Color.Black;
			c = c_;
		}
		public colorchar(Color color_,Color bgcolor_,char c_){
			color = color_;
			bgcolor = bgcolor_;
			c = c_;
		}
		public colorchar(Color color_,char c_){
			color = color_;
			bgcolor = Color.Black;
			c = c_;
		}
	}
	public struct cstr{
		public Color color;
		public Color bgcolor;
		public string s;
		public static implicit operator colorstring(cstr c){
			return new colorstring(c);
		}
		public cstr(string s_,Color color_){
			color = color_;
			bgcolor = Color.Black;
			s = s_;
		}
		public cstr(string s_,Color color_,Color bgcolor_){
			color = color_;
			bgcolor = bgcolor_;
			s = s_;
		}
		public cstr(Color color_,string s_){
			color = color_;
			bgcolor = Color.Black;
			s = s_;
		}
		public cstr(Color color_,Color bgcolor_,string s_){
			color = color_;
			bgcolor = bgcolor_;
			s = s_;
		}
	}
	public class colorstring{
		public List<cstr> strings = new List<cstr>();
		public int Length(){
			int total = 0;
			foreach(cstr s in strings){
				total += s.s.Length;
			}
			return total;
		}
		public colorchar this[int index]{
			get{
				int cstr_idx = 0;
				while(index >= strings[cstr_idx].s.Length){
					index -= strings[cstr_idx].s.Length;
					++cstr_idx;
				}
				return new colorchar(strings[cstr_idx].s[index],strings[cstr_idx].color,strings[cstr_idx].bgcolor);
			}
		}
		public colorstring(string s1,Color c1){
			strings.Add(new cstr(s1,c1));
		}
		public colorstring(string s1,Color c1,string s2,Color c2){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3,string s4,Color c4){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
			strings.Add(new cstr(s4,c4));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3,string s4,Color c4,string s5,Color c5){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
			strings.Add(new cstr(s4,c4));
			strings.Add(new cstr(s5,c5));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3,string s4,Color c4,string s5,Color c5,string s6,Color c6){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
			strings.Add(new cstr(s4,c4));
			strings.Add(new cstr(s5,c5));
			strings.Add(new cstr(s6,c6));
		}
		public colorstring(params cstr[] cstrs){
			if(cstrs != null && cstrs.Length > 0){
				foreach(cstr cs in cstrs){
					strings.Add(cs);
				}
			}
		}
		public static colorstring operator +(colorstring one,colorstring two){
			colorstring result = new colorstring();
			foreach(cstr s in one.strings){
				result.strings.Add(s);
			}
			foreach(cstr s in two.strings){
				result.strings.Add(s);
			}
			return result;
		}
	}
	public static class Screen{
		private static colorchar[,] memory;
		private static bool terminal_bold = false; //for linux terminals
		private static readonly string bold_on = (char)27 + "[1m"; //VT100 codes, sweet
		private static readonly string bold_off = (char)27 + "[m";
		public static bool GLMode = true;
		public static bool NoGLUpdate = false; //if NoGLUpdate is true, UpdateGLBuffer won't be called - only the memory will be updated. This is useful if you wish to update all at once, instead of one at a time.
		public static int screen_center_col = -1;
		private static bool cursor_visible = true; //these 3 values are only used in GL mode - in console mode, the Console values are used directly.
		private static int cursor_top = 0;
		private static int cursor_left = 0;
		public static bool CursorVisible{
			get{
				if(GLMode){
					return cursor_visible;
				}
				return Console.CursorVisible;
			}
			set{
				if(GLMode){
					if(cursor_visible != value){
						cursor_visible = value;
						UpdateCursor(value);
					}
				}
				else{
					Console.CursorVisible = value;
				}
			}
		}
		public static int CursorTop{
			get{
				if(GLMode){
					return cursor_top;
				}
				return Console.CursorTop;
			}
			set{
				if(GLMode){
					if(cursor_top != value){
						cursor_top = value;
						UpdateCursor(cursor_visible);
					}
				}
				else{
					Console.CursorTop = value;
				}
			}
		}
		public static int CursorLeft{
			get{
				if(GLMode){
					return cursor_left;
				}
				return Console.CursorLeft;
			}
			set{
				if(GLMode){
					if(cursor_left != value){
						cursor_left = value;
						UpdateCursor(cursor_visible);
					}
				}
				else{
					Console.CursorLeft = value;
				}
			}
		}
		public static void SetCursorPosition(int left,int top){
			if(GLMode){
				if(cursor_left != left || cursor_top != top){
					cursor_left = left;
					cursor_top = top;
					UpdateCursor(cursor_visible);
				}
			}
			else{
				Console.SetCursorPosition(left,top);
			}
		}
		public static ConsoleColor ForegroundColor{
			get{
				if(Global.LINUX && terminal_bold){
					return Console.ForegroundColor+8;
				}
				return Console.ForegroundColor;
			}
			set{
				if(Global.LINUX && (int)value >= 8){
					Console.ForegroundColor = value - 8;
					if(!terminal_bold){
						terminal_bold = true;
						Console.Write(bold_on);
					}
				}
				else{
					if(Global.LINUX && terminal_bold){
						Console.Write(bold_off);
						terminal_bold = false;
					}
					Console.ForegroundColor = value;
				}
			}
		}
		public static ConsoleColor BackgroundColor{
			get{
				return Console.BackgroundColor;
			}
			set{
				if(Global.LINUX && (int)value >= 8){
					Console.BackgroundColor = value - 8;
				}
				else{
					Console.BackgroundColor = value;
				}
			}
		}
		public static void UpdateScreenCenterColumn(int col){ //this is the alternative to "always centered" behavior.
			if(col < screen_center_col-3){ //todo
				screen_center_col = col-3;
			}
			if(col > screen_center_col+3){
				screen_center_col = col+3;
			}
		}
		public static colorchar Char(int r,int c){ return memory[r,c]; }
		public static colorchar MapChar(int r,int c){ return memory[r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS]; }
		public static colorchar StatsChar(int r,int c){ return memory[r,c]; } //changed from r+1,c
		static Screen(){
			memory = new colorchar[Global.SCREEN_H,Global.SCREEN_W];
			for(int i=0;i<Global.SCREEN_H;++i){
				for(int j=0;j<Global.SCREEN_W;++j){
					memory[i,j].c = ' ';
					memory[i,j].color = Color.Black;
					memory[i,j].bgcolor = Color.Black;
				}
			}
			if(!GLMode){
				BackgroundColor = Console.BackgroundColor;
				ForegroundColor = Console.ForegroundColor;
			}
		}
		public static colorchar BlankChar(){ return new colorchar(Color.Black,' '); }
		public static colorchar[,] GetCurrentScreen(){
			colorchar[,] result = new colorchar[Global.SCREEN_H,Global.SCREEN_W];
			for(int i=0;i<Global.SCREEN_H;++i){
				for(int j=0;j<Global.SCREEN_W;++j){
					result[i,j] = Char(i,j);
				}
			}
			return result;
		}
		public static colorchar[,] GetCurrentMap(){
			colorchar[,] result = new colorchar[Global.ROWS,Global.COLS];
			for(int i=0;i<Global.ROWS;++i){
				for(int j=0;j<Global.COLS;++j){
					result[i,j] = MapChar(i,j);
				}
			}
			return result;
		}
		public static colorchar[,] GetCurrentRect(int row,int col,int height,int width){
			colorchar[,] result = new colorchar[height,width];
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					result[i,j] = Char(row+i,col+j);
				}
			}
			return result;
		}
		public static bool BoundsCheck(int r,int c){
			if(r>=0 && r<Global.SCREEN_H && c>=0 && c<Global.SCREEN_W){
				return true;
			}
			return false;
		}
		public static bool MapBoundsCheck(int r,int c){
			if(r>=0 && r<Global.ROWS && c>=0 && c<Global.COLS){
				return true;
			}
			return false;
		}
		public static void Blank(){
			CursorVisible = false;
			for(int i=0;i<Global.SCREEN_H;++i){
				WriteString(i,0,"".PadRight(Global.SCREEN_W));
				for(int j=0;j<Global.SCREEN_W;++j){
					memory[i,j].c = ' ';
					memory[i,j].color = Color.Black;
					memory[i,j].bgcolor = Color.Black;
				}
			}
		}
		public static void UpdateGLBuffer(int start_row,int start_col,int end_row,int end_col){
			int num_positions = ((end_col + end_row*Global.SCREEN_W) - (start_col + start_row*Global.SCREEN_W)) + 1;
			int row = start_row;
			int col = start_col;
			int[] sprite_rows = new int[num_positions];
			int[] sprite_cols = new int[num_positions];
			float[][] color_info = new float[2][];
			color_info[0] = new float[4 * num_positions];
			color_info[1] = new float[4 * num_positions];
			for(int i=0;i<num_positions;++i){
				colorchar cch = memory[row,col];
				Color4 color = GLGame.ConvertColor(cch.color);
				Color4 bgcolor = GLGame.ConvertColor(cch.bgcolor);
				sprite_rows[i] = 0;
				sprite_cols[i] = (int)cch.c;
				int idx4 = i * 4;
				color_info[0][idx4] = color.R;
				color_info[0][idx4 + 1] = color.G;
				color_info[0][idx4 + 2] = color.B;
				color_info[0][idx4 + 3] = color.A;
				color_info[1][idx4] = bgcolor.R;
				color_info[1][idx4 + 1] = bgcolor.G;
				color_info[1][idx4 + 2] = bgcolor.B;
				color_info[1][idx4 + 3] = bgcolor.A;
				col++;
				if(col == Global.SCREEN_W){
					row++;
					col = 0;
				}
			}
			//int idx = (start_col + start_row*Global.SCREEN_W) * 48;
			//GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)*idx),new IntPtr(sizeof(float)*48*num_positions),values.ToArray());
			Game.gl.UpdateVertexArray(start_row,start_col,GLGame.text_surface,sprite_rows,sprite_cols,color_info);
		}
		public static void UpdateGLBuffer(int row,int col){
			colorchar cch = memory[row,col];
			Game.gl.UpdateVertexArray(row,col,GLGame.text_surface,0,(int)cch.c,cch.color.GetFloatValues(),cch.bgcolor.GetFloatValues());
			/*Color4 color = GLGame.ConvertColor(memory[row,col].color);
			Color4 bgcolor = GLGame.ConvertColor(memory[row,col].bgcolor);
			float[][] color_info = new float[2][];
			color_info[0] = new float[4];
			color_info[1] = new float[4];
			color_info[0][0] = color.R;
			color_info[0][1] = color.G;
			color_info[0][2] = color.B;
			color_info[0][3] = color.A;
			color_info[1][0] = bgcolor.R;
			color_info[1][1] = bgcolor.G;
			color_info[1][2] = bgcolor.B;
			color_info[1][3] = bgcolor.A;
			Game.gl.UpdateVertexArray(row,col,GLGame.text_surface,0,(int)memory[row,col].c,color_info);*/
		}
		public static void UpdateCursor(bool make_visible){
			if(make_visible && (!Global.GRAPHICAL || MouseUI.Mode != MouseMode.Map)){
				float[] color_values = GLGame.ConvertColor(Color.Gray).GetFloatValues();
				SpriteSurface s = GLGame.cursor_surface;
				s.Disabled = false;
				s.PixelHeightOffset = cursor_top * GLGame.text_surface.TileHeight + GLGame.text_surface.TileHeight * 7 / 8;
				s.PixelWidthOffset = cursor_left * GLGame.text_surface.TileWidth;
				s.GLCoordHeightOffset = ((float)((Game.gl.GameAreaHeight - s.PixelHeightOffset) - s.Rows*s.TileHeight) / (float)Game.gl.GameAreaHeight) * 2.0f - 1.0f;
				s.GLCoordWidthOffset = ((float)s.PixelWidthOffset / (float)Game.gl.GameAreaWidth) * 2.0f - 1.0f;
				Game.gl.UpdateVertexArray(0,0,s,0,0,color_values,color_values);
			}
			else{
				GLGame.cursor_surface.Disabled = true;
			}
		}
		public static void UpdateGLBuffer(int start_row,int start_col,colorchar[,] array){
			int array_h = array.GetLength(0);
			int array_w = array.GetLength(1);
			int start_idx = start_col + start_row*Global.SCREEN_W;
			int end_idx = (start_col + array_w - 1) + (start_row + array_h - 1)*Global.SCREEN_W;
			int count = (end_idx - start_idx) + 1;
			int end_row = start_row + array_h - 1;
			int end_col = start_col + array_w - 1;
			int[] sprite_rows = new int[count];
			int[] sprite_cols = new int[count];
			float[][] color_info = new float[2][];
			color_info[0] = new float[4 * count];
			color_info[1] = new float[4 * count];
			for(int n=0;n<count;++n){
				int row = (n + start_col) / Global.SCREEN_W + start_row; //screen coords
				int col = (n + start_col) % Global.SCREEN_W;
				colorchar cch = (row >= start_row && row <= end_row && col >= start_col && col <= end_col)? array[row-start_row,col-start_col] : memory[row,col];
				Color4 color = GLGame.ConvertColor(cch.color);
				Color4 bgcolor = GLGame.ConvertColor(cch.bgcolor);
				//sprite_rows[n] = 0;
				sprite_cols[n] = (int)cch.c;
				int idx4 = n * 4;
				color_info[0][idx4] = color.R;
				color_info[0][idx4 + 1] = color.G;
				color_info[0][idx4 + 2] = color.B;
				color_info[0][idx4 + 3] = color.A;
				color_info[1][idx4] = bgcolor.R;
				color_info[1][idx4 + 1] = bgcolor.G;
				color_info[1][idx4 + 2] = bgcolor.B;
				color_info[1][idx4 + 3] = bgcolor.A;
			}
			Game.gl.UpdateVertexArray(start_row,start_col,GLGame.text_surface,sprite_rows,sprite_cols,color_info);
		}
		public static void UpdateSurface(int row,int col,SpriteSurface s,int sprite_row,int sprite_col){
			Game.gl.UpdateVertexArray(row,col,s,sprite_row,sprite_col,new float[][]{new float[]{1,1,1,1}});
		}
		public static void UpdateSurface(int row,int col,SpriteSurface s,int sprite_row,int sprite_col,float r,float g,float b){
			Game.gl.UpdateVertexArray(row,col,s,sprite_row,sprite_col,new float[][]{new float[]{r,g,b,1}});
		}
		public static void WriteChar(int r,int c,char ch){
			WriteChar(r,c,new colorchar(Color.Gray,ch));
		}
		public static void WriteChar(int r,int c,char ch,Color color){
			WriteChar(r,c,new colorchar(ch,color));
		}
		public static void WriteChar(int r,int c,char ch,Color color,Color bgcolor){
			WriteChar(r,c,new colorchar(ch,color,bgcolor));
		}
		public static void WriteChar(int r,int c,colorchar ch){
			if(!memory[r,c].Equals(ch)){
				ch.color = ResolveColor(ch.color);
				ch.bgcolor = ResolveColor(ch.bgcolor);
				if(GLMode){
					memory[r,c] = ch;
					if(!NoGLUpdate){
						UpdateGLBuffer(r,c);
					}
				}
				else{
					if(!memory[r,c].Equals(ch)){ //check for equality again now that the color has been resolved - still cheaper than actually writing to console
						memory[r,c] = ch;
						ConsoleColor co = GetColor(ch.color);
						if(co != ForegroundColor){
							ForegroundColor = co;
						}
						co = GetColor(ch.bgcolor);
						if(co != Console.BackgroundColor || Global.LINUX){//voodoo here. not sure why this is needed. (possible Mono bug)
							BackgroundColor = co;
						}
						Console.SetCursorPosition(c,r);
						Console.Write(ch.c);
					}
				}
			}
		}
		public static void WriteArray(int r,int c,colorchar[,] array){
			int h = array.GetLength(0);
			int w = array.GetLength(1);
			for(int i=0;i<h;++i){
				for(int j=0;j<w;++j){
					//WriteChar(i+r,j+c,array[i,j]);
					colorchar ch = array[i,j];
					if(!memory[r+i,c+j].Equals(ch)){
						ch.color = ResolveColor(ch.color);
						ch.bgcolor = ResolveColor(ch.bgcolor);
						//memory[r+i,c+j] = ch;
						array[i,j] = ch;
						if(!GLMode){
							if(!memory[r+i,c+j].Equals(ch)){ //check again to avoid writing to console when possible
								memory[r+i,c+j] = ch;
								ConsoleColor co = GetColor(ch.color);
								if(co != ForegroundColor){
									ForegroundColor = co;
								}
								co = GetColor(ch.bgcolor);
								if(co != Console.BackgroundColor || Global.LINUX){//voodoo here. not sure why this is needed. (possible Mono bug)
									BackgroundColor = co;
								}
								Console.SetCursorPosition(c+j,r+i);
								Console.Write(ch.c);
							}
						}
						else{
							memory[r+i,c+j] = ch;
						}
					}
				}
			}
			if(GLMode && !NoGLUpdate){
				UpdateGLBuffer(r,c,array);
			}
		}
		public static void WriteList(int r,int c,List<colorstring> ls){
			int line = r;
			foreach(colorstring cs in ls){
				WriteString(line,c,cs);
				++line;
			}
		}
		public static void WriteString(int r,int c,string s){ WriteString(r,c,new cstr(Color.Gray,s)); }
		public static void WriteString(int r,int c,string s,Color color){ WriteString(r,c,new cstr(s,color)); }
		public static void WriteString(int r,int c,cstr s){
			if(Global.SCREEN_W - c > s.s.Length){
				//s.s = s.s.Substring(0,; //don't move down to the next line
			}
			else{
				s.s = s.s.Substring(0,Global.SCREEN_W - c);
			}
			if(s.s.Length > 0){
				s.color = ResolveColor(s.color);
				s.bgcolor = ResolveColor(s.bgcolor);
				colorchar cch;
				cch.color = s.color;
				cch.bgcolor = s.bgcolor;
				if(!GLMode){
					ConsoleColor co = GetColor(s.color);
					if(ForegroundColor != co){
						ForegroundColor = co;
					}
					co = GetColor(s.bgcolor);
					if(BackgroundColor != co){
						BackgroundColor = co;
					}
				}
				int start_col = -1;
				int end_col = -1;
				int i = 0;
				bool changed = false;
				foreach(char ch in s.s){
					cch.c = ch;
					if(!memory[r,c+i].Equals(cch)){
						memory[r,c+i] = cch;
						if(start_col == -1){
							start_col = c+i;
						}
						end_col = c+i;
						changed = true;
					}
					++i;
				}
				if(changed){
					if(GLMode){
						if(!NoGLUpdate){
							UpdateGLBuffer(r,start_col,r,end_col);
						}
					}
					else{
						Console.SetCursorPosition(c,r);
						Console.Write(s.s);
					}
				}
				if(MouseUI.AutomaticButtonsFromStrings && GLMode){
					int idx = 0;
					int brace = -1;
					int start = -1;
					int end = -1;
					bool last_char_was_separator = false;
					while(true){
						if(brace == -1){
							if(s.s[idx] == '['){
								brace = 0;
								start = idx;
							}
						}
						else{
							if(brace == 0){
								if(s.s[idx] == ']'){
									brace = 1;
									end = idx;
								}
							}
							else{
								if(s.s[idx] == ' ' || s.s[idx] == '-' || s.s[idx] == ','){
									if(last_char_was_separator){
										ConsoleKey key = ConsoleKey.A;
										bool shifted = false;
										switch(s.s[start+1]){
										case 'E':
											key = ConsoleKey.Enter;
											break;
										case 'T':
											key = ConsoleKey.Tab;
											break;
										case 'P': //"Press any key"
											break;
										case '?':
											key = ConsoleKey.Oem2;
											shifted = true;
											break;
										case '=':
											key = ConsoleKey.OemPlus;
											break;
										default: //all others should be lowercase letters
											key = (ConsoleKey)(ConsoleKey.A + ((int)s.s[start+1] - (int)'a'));
											break;
										}
										MouseUI.CreateButton(key,shifted,r,c+start,1,end-start+1);
										brace = -1;
										start = -1;
										end = -1;
									}
									last_char_was_separator = !last_char_was_separator;
								}
								else{
									last_char_was_separator = false;
									end = idx;
								}
							}
						}
						++idx;
						if(idx == s.s.Length){
							if(brace == 1){
								ConsoleKey key = ConsoleKey.A;
								bool shifted = false;
								switch(s.s[start+1]){
								case 'E':
									key = ConsoleKey.Enter;
									break;
								case 'T':
									key = ConsoleKey.Tab;
									break;
								case 'P': //"Press any key"
									break;
								case '?':
									key = ConsoleKey.Oem2;
									shifted = true;
									break;
								case '=':
									key = ConsoleKey.OemPlus;
									break;
								default: //all others should be lowercase letters
									key = (ConsoleKey)(ConsoleKey.A + ((int)s.s[start+1] - (int)'a'));
									break;
								}
								MouseUI.CreateButton(key,shifted,r,c+start,1,end-start+1);
							}
							break;
						}
					}
				}
			}
		}
		public static void WriteString(int r,int c,colorstring cs){
			if(cs.Length() > 0){
				int pos = c;
				int start_col = -1;
				int end_col = -1;
				foreach(cstr s1 in cs.strings){
					cstr s = new cstr(s1.s,s1.color,s1.bgcolor);
					if(s.s.Length + pos > Global.SCREEN_W){
						s.s = s.s.Substring(0,Global.SCREEN_W - pos);
					}
					s.color = ResolveColor(s.color);
					s.bgcolor = ResolveColor(s.bgcolor);
					colorchar cch;
					cch.color = s.color;
					cch.bgcolor = s.bgcolor;
					if(!GLMode){
						ConsoleColor co = GetColor(s.color);
						if(ForegroundColor != co){
							ForegroundColor = co;
						}
						co = GetColor(s.bgcolor);
						if(BackgroundColor != co){
							BackgroundColor = co;
						}
					}
					int i = 0;
					bool changed = false;
					foreach(char ch in s.s){
						cch.c = ch;
						if(!memory[r,pos+i].Equals(cch)){
							memory[r,pos+i] = cch;
							if(start_col == -1){
								start_col = pos+i;
							}
							end_col = pos+i;
							changed = true;
						}
						++i;
					}
					if(changed && !GLMode){
						Console.SetCursorPosition(pos,r);
						Console.Write(s.s);
					}
					pos += s.s.Length;
				}
				if(GLMode && !NoGLUpdate && start_col != -1){
					UpdateGLBuffer(r,start_col,r,end_col);
				}
				if(MouseUI.AutomaticButtonsFromStrings && GLMode){
					int idx = 0;
					int brace = -1;
					int start = -1;
					int end = -1;
					bool last_char_was_separator = false;
					while(true){
						char ch = cs[idx].c;
						if(brace == -1){
							if(ch == '['){
								brace = 0;
								start = idx;
							}
						}
						else{
							if(brace == 0){
								if(ch == ']'){
									brace = 1;
									end = idx;
								}
							}
							else{
								if(ch == ' ' || ch == '-' || ch == ','){
									if(last_char_was_separator){
										ConsoleKey key = ConsoleKey.A;
										bool shifted = false;
										switch(cs[start+1].c){
										case 'E':
											key = ConsoleKey.Enter;
											break;
										case 'T':
											key = ConsoleKey.Tab;
											break;
										case 'P': //"Press any key"
											break;
										case '?':
											key = ConsoleKey.Oem2;
											shifted = true;
											break;
										case '=':
											key = ConsoleKey.OemPlus;
											break;
										default: //all others should be lowercase letters
											key = (ConsoleKey)(ConsoleKey.A + ((int)cs[start+1].c - (int)'a'));
											break;
										}
										MouseUI.CreateButton(key,shifted,r,c+start,1,end-start+1);
										brace = -1;
										start = -1;
										end = -1;
									}
									last_char_was_separator = !last_char_was_separator;
								}
								else{
									last_char_was_separator = false;
									end = idx;
								}
							}
						}
						++idx;
						if(idx == cs.Length()){
							if(brace == 1){
								ConsoleKey key = ConsoleKey.A;
								bool shifted = false;
								switch(cs[start+1].c){
								case 'E':
									key = ConsoleKey.Enter;
									break;
								case 'T':
									key = ConsoleKey.Tab;
									break;
								case 'P': //"Press any key"
									break;
								case '?':
									key = ConsoleKey.Oem2;
									shifted = true;
									break;
								case '=':
									key = ConsoleKey.OemPlus;
									break;
								default: //all others should be lowercase letters
									key = (ConsoleKey)(ConsoleKey.A + ((int)cs[start+1].c - (int)'a'));
									break;
								}
								MouseUI.CreateButton(key,shifted,r,c+start,1,end-start+1);
							}
							break;
						}
					}
				}
			}
		}
		public static void ResetColors(){
			if(!GLMode){
				if(ForegroundColor != ConsoleColor.Gray){
					ForegroundColor = ConsoleColor.Gray;
				}
				if(BackgroundColor != ConsoleColor.Black){
					BackgroundColor = ConsoleColor.Black;
				}
			}
		}
		public static void WriteMapChar(int r,int c,char ch){
			WriteMapChar(r,c,new colorchar(Color.Gray,ch));
		}
		public static void WriteMapChar(int r,int c,char ch,Color color){
			WriteMapChar(r,c,new colorchar(ch,color));
		}
		public static void WriteMapChar(int r,int c,char ch,Color color,Color bgcolor){
			WriteMapChar(r,c,new colorchar(ch,color,bgcolor));
		}
		public static void WriteMapChar(int r,int c,colorchar ch){
			WriteChar(r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS,ch);
		}
		public static void WriteMapString(int r,int c,string s){
			cstr cs;
			cs.color = Color.Gray;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteMapString(r,c,cs);
		}
		public static void WriteMapString(int r,int c,string s,Color color){
			cstr cs;
			cs.color = color;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteMapString(r,c,cs);
		}
		public static void WriteMapString(int r,int c,cstr s){
			if(Global.COLS - c > s.s.Length){
				//s.s = s.s.Substring(0); //don't move down to the next line
			}
			else{
				s.s = s.s.Substring(0,Global.COLS - c);
			}
			if(s.s.Length > 0){
				r += Global.MAP_OFFSET_ROWS;
				c += Global.MAP_OFFSET_COLS;
				s.color = ResolveColor(s.color);
				s.bgcolor = ResolveColor(s.bgcolor);
				colorchar cch;
				cch.color = s.color;
				cch.bgcolor = s.bgcolor;
				if(!GLMode){
					ConsoleColor co = GetColor(s.color);
					if(ForegroundColor != co){
						ForegroundColor = co;
					}
					co = GetColor(s.bgcolor);
					if(BackgroundColor != co){
						BackgroundColor = co;
					}
				}
				int start_col = -1;
				int end_col = -1;
				int i = 0;
				bool changed = false;
				foreach(char ch in s.s){
					cch.c = ch;
					if(!memory[r,c+i].Equals(cch)){
						memory[r,c+i] = cch;
						if(start_col == -1){
							start_col = c+i;
						}
						end_col = c+i;
						changed = true;
					}
					++i;
				}
				if(changed){
					if(GLMode){
						if(!NoGLUpdate){
							UpdateGLBuffer(r,start_col,r,end_col);
						}
					}
					else{
						Console.SetCursorPosition(c,r);
						Console.Write(s.s);
					}
				}
				if(MouseUI.AutomaticButtonsFromStrings && GLMode){
					int idx = s.s.IndexOf('['); //for now I'm only checking for a single brace here.
					if(idx != -1 && idx+1 < s.s.Length){
						ConsoleKey key = ConsoleKey.A;
						bool shifted = false;
						switch(s.s[idx+1]){
						case 'E':
							key = ConsoleKey.Enter;
							break;
						case 'T':
							key = ConsoleKey.Tab;
							break;
						case 'P': //"Press any key"
							break;
						case '?':
							key = ConsoleKey.Oem2;
							shifted = true;
							break;
						case '=':
							key = ConsoleKey.OemPlus;
							break;
						default: //all others should be lowercase letters
							key = (ConsoleKey)(ConsoleKey.A + ((int)s.s[idx+1] - (int)'a'));
							break;
						}
						MouseUI.CreateMapButton(key,shifted,r,1);
					}
				}
			}
		}
		public static void WriteMapString(int r,int c,colorstring cs){
			if(cs.Length() > 0){
				r += Global.MAP_OFFSET_ROWS;
				c += Global.MAP_OFFSET_COLS;
				int start_col = -1;
				int end_col = -1;
				int cpos = c;
				foreach(cstr s1 in cs.strings){
					cstr s = new cstr(s1.s,s1.color,s1.bgcolor);
					if(cpos-Global.MAP_OFFSET_COLS + s.s.Length > Global.COLS){
						s.s = s.s.Substring(0,Global.COLS-(cpos-Global.MAP_OFFSET_COLS));
					}
					s.color = ResolveColor(s.color);
					s.bgcolor = ResolveColor(s.bgcolor);
					colorchar cch;
					cch.color = s.color;
					cch.bgcolor = s.bgcolor;
					if(!GLMode){
						ConsoleColor co = GetColor(s.color);
						if(ForegroundColor != co){
							ForegroundColor = co;
						}
						co = GetColor(s.bgcolor);
						if(BackgroundColor != co){
							BackgroundColor = co;
						}
					}
					int i = 0;
					bool changed = false;
					foreach(char ch in s.s){
						cch.c = ch;
						if(!memory[r,cpos+i].Equals(cch)){
							memory[r,cpos+i] = cch;
							if(start_col == -1){
								start_col = cpos+i;
							}
							end_col = cpos+i;
							changed = true;
						}
						++i;
					}
					if(changed && !GLMode){
						Console.SetCursorPosition(cpos,r);
						Console.Write(s.s);
					}
					cpos += s.s.Length;
				}
				if(GLMode && !NoGLUpdate && start_col != -1){
					UpdateGLBuffer(r,start_col,r,end_col);
				}
				if(MouseUI.AutomaticButtonsFromStrings && GLMode){
					int idx = -1;
					int len = cs.Length();
					for(int i=0;i<len;++i){
						if(cs[i].c == '['){
							idx = i;
							break;
						}
					}
					if(idx != -1 && idx+1 < cs.Length()){
						ConsoleKey key = ConsoleKey.A;
						bool shifted = false;
						switch(cs[idx+1].c){
						case 'E':
							key = ConsoleKey.Enter;
							break;
						case 'T':
							key = ConsoleKey.Tab;
							break;
						case 'P': //"Press any key"
							break;
						case '?':
							key = ConsoleKey.Oem2;
							shifted = true;
							break;
						case '=':
							key = ConsoleKey.OemPlus;
							break;
						default: //all others should be lowercase letters
							key = (ConsoleKey)(ConsoleKey.A + ((int)cs[idx+1].c - (int)'a'));
							break;
						}
						MouseUI.CreateMapButton(key,shifted,r,1);
					}
				}
				/*if(cpos-Global.MAP_OFFSET_COLS < Global.COLS){
					WriteString(r,cpos,"".PadRight(Global.COLS-(cpos-Global.MAP_OFFSET_COLS)));
				}*/
			}
		}
		public static void WriteStatsChar(int r,int c,colorchar ch){ WriteChar(r,c,ch); } //was r+1,c
		public static void WriteStatsString(int r,int c,string s){
			cstr cs;
			cs.color = Color.Gray;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteStatsString(r,c,cs);
		}
		public static void WriteStatsString(int r,int c,string s,Color color){
			cstr cs;
			cs.color = color;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteStatsString(r,c,cs);
		}
		public static void WriteStatsString(int r,int c,cstr s){
			if(12 - c > s.s.Length){
				//s.s = s.s.Substring(0); //don't move down to the next line - 12 is the width of the stats area
			}
			else{
				s.s = s.s.Substring(0,12 - c);
			}
			if(s.s.Length > 0){
				//++r;
				s.color = ResolveColor(s.color);
				s.bgcolor = ResolveColor(s.bgcolor);
				colorchar cch;
				cch.color = s.color;
				cch.bgcolor = s.bgcolor;
				if(!GLMode){
					ConsoleColor co = GetColor(s.color);
					if(ForegroundColor != co){
						ForegroundColor = co;
					}
					co = GetColor(s.bgcolor);
					if(BackgroundColor != co){
						BackgroundColor = co;
					}
				}
				int start_col = -1;
				int end_col = -1;
				int i = 0;
				bool changed = false;
				foreach(char ch in s.s){
					cch.c = ch;
					if(!memory[r,c+i].Equals(cch)){
						memory[r,c+i] = cch;
						if(start_col == -1){
							start_col = c+i;
						}
						end_col = c+i;
						changed = true;
					}
					++i;
				}
				if(changed){
					if(GLMode){
						if(!NoGLUpdate){
							UpdateGLBuffer(r,start_col,r,end_col);
						}
					}
					else{
						Console.SetCursorPosition(c,r);
						Console.Write(s.s);
					}
				}
				if(MouseUI.AutomaticButtonsFromStrings && GLMode){
					int idx = s.s.IndexOf('['); //for now I'm only checking for a single brace here.
					if(idx != -1 && idx+1 < s.s.Length){
						ConsoleKey key = ConsoleKey.A;
						bool shifted = false;
						switch(s.s[idx+1]){
						case 'E':
							key = ConsoleKey.Enter;
							break;
						case 'T':
							key = ConsoleKey.Tab;
							break;
						case 'P': //"Press any key"
							break;
						case '?':
							key = ConsoleKey.Oem2;
							shifted = true;
							break;
						case '=':
							key = ConsoleKey.OemPlus;
							break;
						default: //all others should be lowercase letters
							key = (ConsoleKey)(ConsoleKey.A + ((int)s.s[idx+1] - (int)'a'));
							break;
						}
						MouseUI.CreateStatsButton(key,shifted,r,1);
					}
				}
			}
		}
		public static void MapDrawWithStrings(colorchar[,] array,int row,int col,int height,int width){
			cstr s;
			s.s = "";
			s.bgcolor = Color.Black;
			s.color = Color.Black;
			int current_c = col;
			for(int i=row;i<row+height;++i){
				s.s = "";
				current_c = col;
				for(int j=col;j<col+width;++j){
					colorchar ch = array[i,j];
					if(Screen.ResolveColor(ch.color) != s.color){
						if(s.s.Length > 0){
							Screen.WriteMapString(i,current_c,s);
							s.s = "";
							s.s += ch.c;
							s.color = ch.color;
							current_c = j;
						}
						else{
							s.s += ch.c;
							s.color = ch.color;
						}
					}
					else{
						s.s += ch.c;
					}
				}
				Screen.WriteMapString(i,current_c,s);
			}
		}
		public static void AnimateCell(int r,int c,colorchar ch,int duration){
			colorchar prev = memory[r,c];
			WriteChar(r,c,ch);
			Game.GLUpdate();
			Thread.Sleep(duration);
			WriteChar(r,c,prev);
		}
		/*public static void AnimateCellNonBlocking(int r,int c,colorchar ch,int duration){
			colorchar prev = memory[r,c]; //experimental animation for realtime input. seems to work decently so far.
			WriteChar(r,c,ch);
			for(int i=0;i<duration;i+=5){
				Thread.Sleep(5);
				if(Console.KeyAvailable){
					WriteChar(r,c,prev);
					return;
				}
			}
			WriteChar(r,c,prev);
		}*/
		public static void AnimateMapCell(int r,int c,colorchar ch){ AnimateMapCell(r,c,ch,50); }
		public static void AnimateMapCell(int r,int c,colorchar ch,int duration){
			AnimateCell(r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS,ch,duration);
		}
		public static void AnimateMapCells(List<pos> cells,List<colorchar> chars){ AnimateMapCells(cells,chars,50); }
		public static void AnimateMapCells(List<pos> cells,List<colorchar> chars,int duration){
			List<colorchar> prev = new List<colorchar>();
			int idx = 0;
			foreach(pos p in cells){
				prev.Add(MapChar(p.row,p.col));
				WriteMapChar(p.row,p.col,chars[idx]);
				++idx;
			}
			Game.GLUpdate();
			Thread.Sleep(duration);
			idx = 0;
			foreach(pos p in cells){
				WriteMapChar(p.row,p.col,prev[idx]);
				++idx;
			}
		}
		public static void AnimateMapCells(List<pos> cells,colorchar ch){ AnimateMapCells(cells,ch,50); }
		public static void AnimateMapCells(List<pos> cells,colorchar ch,int duration){
			List<colorchar> prev = new List<colorchar>();
			int idx = 0;
			foreach(pos p in cells){
				prev.Add(MapChar(p.row,p.col));
				WriteMapChar(p.row,p.col,ch);
				++idx;
			}
			Game.GLUpdate();
			Thread.Sleep(duration);
			idx = 0;
			foreach(pos p in cells){
				WriteMapChar(p.row,p.col,prev[idx]);
				++idx;
			}
		}
		public static void AnimateProjectile(List<Tile> list,colorchar ch){ AnimateProjectile(list,ch,50); }
		public static void AnimateProjectile(List<Tile> list,colorchar ch,int duration){
			CursorVisible = false;
			list.RemoveAt(0);
			foreach(Tile t in list){
				AnimateMapCell(t.row,t.col,ch,duration);
			}
			CursorVisible = true;
		}
		public static void AnimateBoltProjectile(List<Tile> list,Color color){ AnimateBoltProjectile(list,color,50); }
		public static void AnimateBoltProjectile(List<Tile> list,Color color,int duration){
			CursorVisible = false;
			colorchar ch;
			ch.color = color;
			ch.bgcolor = Color.Black;
			ch.c='!';
			switch(list[0].DirectionOf(list[list.Count-1])){
			case 7:
			case 3:
				ch.c = '\\';
				break;
			case 8:
			case 2:
				ch.c = '|';
				break;
			case 9:
			case 1:
				ch.c = '/';
				break;
			case 4:
			case 6:
				ch.c = '-';
				break;
			}
			list.RemoveAt(0);
			foreach(Tile t in list){
				AnimateMapCell(t.row,t.col,ch,duration);
			}
			CursorVisible = true;
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch){
			AnimateExplosion(obj,radius,ch,50,false);
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch,bool single_frame){
			AnimateExplosion(obj,radius,ch,50,single_frame);
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch,int duration){
			AnimateExplosion(obj,radius,ch,duration,false);
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch,int duration,bool single_frame){
			CursorVisible = false;
			colorchar[,] prev = new colorchar[radius*2+1,radius*2+1];
			for(int i=0;i<=radius*2;++i){
				for(int j=0;j<=radius*2;++j){
					if(MapBoundsCheck(obj.row-radius+i,obj.col-radius+j)){
						prev[i,j] = MapChar(obj.row-radius+i,obj.col-radius+j);
					}
				}
			}
			if(!single_frame){
				for(int i=0;i<=radius;++i){
					foreach(Tile t in obj.TilesAtDistance(i)){
						WriteMapChar(t.row,t.col,ch);
					}
					Game.GLUpdate();
					Thread.Sleep(duration);
				}
			}
			else{
				foreach(Tile t in obj.TilesWithinDistance(radius)){
					WriteMapChar(t.row,t.col,ch);
				}
				Game.GLUpdate();
				Thread.Sleep(duration);
			}
			for(int i=0;i<=radius*2;++i){
				for(int j=0;j<=radius*2;++j){
					if(MapBoundsCheck(obj.row-radius+i,obj.col-radius+j)){
						WriteMapChar(obj.row-radius+i,obj.col-radius+j,prev[i,j]);
					}
				}
			}
			CursorVisible = true;
		}
		public static void AnimateBoltBeam(List<Tile> list,Color color){ AnimateBoltBeam(list,color,50); }
		public static void AnimateBoltBeam(List<Tile> list,Color color,int duration){
			CursorVisible = false;
			colorchar ch;
			ch.color = color;
			ch.bgcolor = Color.Black;
			ch.c='!';
			switch(list[0].DirectionOf(list[list.Count-1])){
			case 7:
			case 3:
				ch.c = '\\';
				break;
			case 8:
			case 2:
				ch.c = '|';
				break;
			case 9:
			case 1:
				ch.c = '/';
				break;
			case 4:
			case 6:
				ch.c = '-';
				break;
			}
			list.RemoveAt(0);
			List<colorchar> memlist = new List<colorchar>();
			foreach(Tile t in list){
				memlist.Add(MapChar(t.row,t.col));
				WriteMapChar(t.row,t.col,ch);
				Game.GLUpdate();
				Thread.Sleep(duration);
			}
			int i = 0;
			foreach(Tile t in list){
				WriteMapChar(t.row,t.col,memlist[i++]);
			}
			CursorVisible = true;
		}
		public static void AnimateBeam(List<Tile> list,colorchar ch){ AnimateBeam(list,ch,50); }
		public static void AnimateBeam(List<Tile> list,colorchar ch,int duration){
			CursorVisible = false;
			list.RemoveAt(0);
			List<colorchar> memlist = new List<colorchar>();
			foreach(Tile t in list){
				memlist.Add(MapChar(t.row,t.col));
				WriteMapChar(t.row,t.col,ch);
				Game.GLUpdate();
				Thread.Sleep(duration);
			}
			int i = 0;
			foreach(Tile t in list){
				WriteMapChar(t.row,t.col,memlist[i++]);
			}
			CursorVisible = true;
		}
		public static void AnimateStorm(pos origin,int radius,int num_frames,int num_per_frame,char c,Color color){
			AnimateStorm(origin,radius,num_frames,num_per_frame,new colorchar(c,color));
		}
		public static void AnimateStorm(pos origin,int radius,int num_frames,int num_per_frame,colorchar ch){
			for(int i=0;i<num_frames;++i){
				List<pos> cells = new List<pos>();
				List<pos> nearby = origin.PositionsWithinDistance(radius);
				for(int j=0;j<num_per_frame;++j){
					cells.Add(nearby.RemoveRandom());
				}
				Screen.AnimateMapCells(cells,ch);
			}
		}
		public static void DrawMapBorder(colorchar ch){
			for(int i=0;i<Global.ROWS;i+=Global.ROWS-1){
				for(int j=0;j<Global.COLS;++j){
					WriteMapChar(i,j,ch);
				}
			}
			for(int j=0;j<Global.COLS;j+=Global.COLS-1){
				for(int i=0;i<Global.ROWS;++i){
					WriteMapChar(i,j,ch);
				}
			}
			ResetColors();
		}
		public static ConsoleColor GetColor(Color c){
			switch(c){
			case Color.Black:
				return ConsoleColor.Black;
			case Color.White:
				return ConsoleColor.White;
			case Color.Gray:
				return ConsoleColor.Gray;
			case Color.Red:
				return ConsoleColor.Red;
			case Color.Green:
				return ConsoleColor.Green;
			case Color.Blue:
				return ConsoleColor.Blue;
			case Color.Yellow:
				return ConsoleColor.Yellow;
			case Color.Magenta:
				return ConsoleColor.Magenta;
			case Color.Cyan:
				return ConsoleColor.Cyan;
			case Color.DarkGray:
				return ConsoleColor.DarkGray;
			case Color.DarkRed:
				return ConsoleColor.DarkRed;
			case Color.DarkGreen:
				return ConsoleColor.DarkGreen;
			case Color.DarkBlue:
				return ConsoleColor.DarkBlue;
			case Color.DarkYellow:
				return ConsoleColor.DarkYellow;
			case Color.DarkMagenta:
				return ConsoleColor.DarkMagenta;
			case Color.DarkCyan:
				return ConsoleColor.DarkCyan;
			case Color.RandomFire:
			case Color.RandomIce:
			case Color.RandomLightning:
			case Color.RandomBreached:
			case Color.RandomExplosion:
			case Color.RandomGlowingFungus:
			case Color.RandomTorch:
			case Color.RandomDoom:
			case Color.RandomConfusion:
			case Color.RandomDark:
			case Color.RandomBright:
			case Color.RandomRGB:
			case Color.RandomDRGB:
			case Color.RandomRGBW:
			case Color.RandomCMY:
			case Color.RandomDCMY:
			case Color.RandomCMYW:
			case Color.RandomRainbow:
			case Color.RandomAny:
			case Color.OutOfSight:
			case Color.TerrainDarkGray:
				return GetColor(ResolveColor(c));
			default:
				return ConsoleColor.Black;
			}
		}
		public static Color ResolveColor(Color c){
			switch(c){
			case Color.RandomFire:
				switch(R.Roll(1,3)){
				case 1:
					return Color.Red;
				case 2:
					return Color.DarkRed;
				case 3:
					return Color.Yellow;
				default:
					return Color.Black;
				}
			case Color.RandomIce:
				switch(R.Roll(1,4)){
				case 1:
					return Color.White;
				case 2:
					return Color.Cyan;
				case 3:
					return Color.Blue;
				case 4:
					return Color.DarkBlue;
				default:
					return Color.Black;
				}
			case Color.RandomLightning:
				switch(R.Roll(1,4)){
				case 1:
					return Color.White;
				case 2:
					return Color.Yellow;
				case 3:
					return Color.Yellow;
				case 4:
					return Color.DarkYellow;
				default:
					return Color.Black;
				}
			case Color.RandomBreached:
			{
				if(R.OneIn(4)){
					return Color.DarkGreen;
				}
				return Color.Green;
			}
			case Color.RandomExplosion:
				if(R.OneIn(4)){
					return Color.Red;
				}
				return Color.DarkRed;
			case Color.RandomGlowingFungus:
				if(R.OneIn(35)){
					return Color.DarkCyan;
				}
				return Color.Cyan;
			case Color.RandomTorch:
				if(R.OneIn(8)){
					if(R.CoinFlip()){
						return Color.White;
					}
					else{
						return Color.Red;
					}
				}
				return Color.Yellow;
			case Color.RandomDoom:
				switch(R.Roll(4)){
				case 1:
				case 2:
					return Color.DarkGray;
				case 3:
					return Color.DarkRed;
				case 4:
				default:
					return Color.DarkMagenta;
				}
			case Color.RandomConfusion:
				if(R.OneIn(16)){
					switch(R.Roll(6)){
					case 1:
						return Color.Red;
					case 2:
						return Color.Green;
					case 3:
						return Color.Blue;
					case 4:
						return Color.Cyan;
					case 5:
						return Color.Yellow;
					case 6:
						return Color.White;
					}
				}
				return Color.Magenta;
			case Color.RandomDark:
				switch(R.Roll(7)){
				case 1:
					return Color.DarkBlue;
				case 2:
					return Color.DarkCyan;
				case 3:
					return Color.DarkGray;
				case 4:
					return Color.DarkGreen;
				case 5:
					return Color.DarkMagenta;
				case 6:
					return Color.DarkRed;
				case 7:
					return Color.DarkYellow;
				default:
					return Color.Black;
				}
			case Color.RandomBright:
				switch(R.Roll(8)){
				case 1:
					return Color.Blue;
				case 2:
					return Color.Cyan;
				case 3:
					return Color.Gray;
				case 4:
					return Color.Green;
				case 5:
					return Color.Magenta;
				case 6:
					return Color.Red;
				case 7:
					return Color.Yellow;
				case 8:
					return Color.White;
				default:
					return Color.Black;
				}
			case Color.RandomRGB:
				switch(R.Roll(1,3)){
				case 1:
					return Color.Red;
				case 2:
					return Color.Green;
				case 3:
					return Color.Blue;
				default:
					return Color.Black;
				}
			case Color.RandomDRGB:
				switch(R.Roll(1,3)){
				case 1:
					return Color.DarkRed;
				case 2:
					return Color.DarkGreen;
				case 3:
					return Color.DarkBlue;
				default:
					return Color.Black;
				}
			case Color.RandomRGBW:
				switch(R.Roll(4)){
				case 1:
					return Color.Red;
				case 2:
					return Color.Green;
				case 3:
					return Color.Blue;
				case 4:
				default:
					return Color.White;
				}
			case Color.RandomCMY:
				switch(R.Roll(1,3)){
				case 1:
					return Color.Cyan;
				case 2:
					return Color.Magenta;
				case 3:
					return Color.Yellow;
				default:
					return Color.Black;
				}
			case Color.RandomDCMY:
				switch(R.Roll(1,3)){
				case 1:
					return Color.DarkCyan;
				case 2:
					return Color.DarkMagenta;
				case 3:
					return Color.DarkYellow;
				default:
					return Color.Black;
				}
			case Color.RandomCMYW:
				switch(R.Roll(4)){
				case 1:
					return Color.Cyan;
				case 2:
					return Color.Magenta;
				case 3:
					return Color.Yellow;
				case 4:
				default:
					return Color.White;
				}
			case Color.RandomRainbow:
				switch(R.Roll(12)){
				case 1:
					return Color.Red;
				case 2:
					return Color.Green;
				case 3:
					return Color.Blue;
				case 4:
					return Color.DarkRed;
				case 5:
					return Color.DarkGreen;
				case 6:
					return Color.DarkBlue;
				case 7:
					return Color.Cyan;
				case 8:
					return Color.Magenta;
				case 9:
					return Color.Yellow;
				case 10:
					return Color.DarkCyan;
				case 11:
					return Color.DarkMagenta;
				case 12:
					return Color.DarkYellow;
				default:
					return Color.Black;
				}
			case Color.RandomAny:
				switch(R.Roll(15)){
				case 1:
					return Color.DarkBlue;
				case 2:
					return Color.DarkCyan;
				case 3:
					return Color.DarkGray;
				case 4:
					return Color.DarkGreen;
				case 5:
					return Color.DarkMagenta;
				case 6:
					return Color.DarkRed;
				case 7:
					return Color.DarkYellow;
				case 8:
					return Color.Blue;
				case 9:
					return Color.Cyan;
				case 10:
					return Color.Gray;
				case 11:
					return Color.Green;
				case 12:
					return Color.Magenta;
				case 13:
					return Color.Red;
				case 14:
					return Color.Yellow;
				case 15:
					return Color.White;
				default:
					return Color.Black;
				}
			case Color.OutOfSight:
			if(Global.Option(OptionType.DARK_GRAY_UNSEEN)){
				if(Screen.GLMode){
					return Color.DarkerGray;
				}
				else{
					return Color.DarkGray;
				}
			}
			else{
				return Color.DarkBlue;
			}
			case Color.TerrainDarkGray:
			if(Screen.GLMode || !Global.Option(OptionType.DARK_GRAY_UNSEEN)){
				return Color.DarkGray;
			}
			else{
				return Color.Gray;
			}
			default:
				return c;
			}
		}
	}
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
		public static PhysicalObject[,] mouselook_objects = new PhysicalObject[Global.ROWS,Global.COLS];
		public static PhysicalObject mouselook_current_target = null;
		public static List<pos> mouse_path = null;
		public static int LastRow = -1;
		public static int LastCol = -1;
		public static bool fire_arrow_hack = false; //hack, used to allow double-clicking [s]hoot to fire arrows.
		public static bool descend_hack = false; //hack, used to make double-clicking Descend [>] cancel the action.
		public static Button GetButton(int row,int col){
			if(button_map.Last() == null || row < 0 || col < 0 || row >= Global.SCREEN_H || col >= Global.SCREEN_W){
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
				if(button_map.Last() == null){
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
			int width = 12;
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
				bool description_on_right = false;
				int max_length = MaxDescriptionBoxLength;
				if(mouselook_current_target.col - 6 < max_length){
					max_length = mouselook_current_target.col - 6;
				}
				if(max_length < 20){
					description_on_right = true;
					max_length = MaxDescriptionBoxLength;
				}
				List<colorstring> desc_box = null;
				Actor a = mouselook_current_target as Actor;
				if(a != null){
					desc_box = Actor.MonsterDescriptionBox(a,true,max_length);
				}
				else{
					Item i = mouselook_current_target as Item;
					if(i != null){
						desc_box = Actor.ItemDescriptionBox(i,true,true,max_length);
					}
				}
				if(desc_box != null){
					int h = desc_box.Count;
					int w = desc_box[0].Length();
					//colorchar[,] array = new colorchar[h,w];
					if(description_on_right){
						Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS + Global.COLS - w,Global.MAP_OFFSET_ROWS + h - 1,Global.MAP_OFFSET_COLS + Global.COLS - 1);
						/*for(int i=0;i<h;++i){
							for(int j=0;j<w;++j){
								array[i,j] = Screen.Char(i+Global.MAP_OFFSET_ROWS,j+Global.MAP_OFFSET_COLS + Global.COLS - w);
							}
						}
						Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS + Global.COLS - w,array); */
					}
					else{
						Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,Global.MAP_OFFSET_ROWS + h - 1,Global.MAP_OFFSET_COLS + w - 1);
						/*for(int i=0;i<h;++i){
							for(int j=0;j<w;++j){
								array[i,j] = Screen.Char(i+Global.MAP_OFFSET_ROWS,j+Global.MAP_OFFSET_COLS);
							}
						}
						Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,array);*/
					}
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
