/*Copyright (c) 2014-2015  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using GLDrawing;
using PosArrays;
using Utilities;
namespace Forays{
	public class GLGame : GLWindow{
		public static SpriteSurface text_surface = null;
		public static SpriteSurface graphics_surface = null;
		public static SpriteSurface actors_surface = null;
		public static SpriteSurface visibility_surface = null;
		public static SpriteSurface cursor_surface = null;
		public static SpriteSurface particle_surface = null;
		public static Stopwatch Timer = null; //todo: move this to Global
		public int CellRows{ //number of cells, for conversion of mouse movement & clicks
			get{
				return internal_cellrows;
			}
			set{
				internal_cellrows = value;
				cell_h = ClientRectangle.Height / value; //todo: i think this would break when fullscreened, since it uses the full height, regardless of border.
			}
		}
		public int CellCols{
			get{
				return internal_cellcols;
			}
			set{
				internal_cellcols = value;
				cell_w = ClientRectangle.Width / value; //todo: same here
			}
		}
		public int GameAreaHeight{
			get{
				return internal_cellrows * cell_h;
			}
		}
		public int GameAreaWidth{
			get{
				return internal_cellcols * cell_w;
			}
		}
		private int internal_cellrows;
		private int internal_cellcols;
		private int cell_h;
		private int cell_w;
		
		public GLGame(int h,int w,int cell_rows,int cell_cols,int snap_h,int snap_w,string title) : base(h,w,title){
			CellRows = cell_rows;
			CellCols = cell_cols;
			SnapHeight = snap_h;
			SnapWidth = snap_w;
			ResizingPreference = ResizeOption.ExactFitOnly; //todo: stretchtofit doesn't work correctly with mouse input yet
			ResizingFullScreenPreference = ResizeOption.AddBorder;
			AllowScaling = true;
			Mouse.Move += MouseMoveHandler;
			Mouse.ButtonUp += MouseClickHandler;
			Mouse.WheelChanged += MouseWheelHandler;
			MouseLeave += MouseLeaveHandler;
		}
		protected override void KeyDownHandler(object sender,KeyboardKeyEventArgs args){
			key_down[args.Key] = true;
			if(!Input.KeyPressed){
				ConsoleKey ck = Input.GetConsoleKey(args.Key);
				if(ck != ConsoleKey.NoName){
					bool alt = KeyIsDown(Key.LAlt) || KeyIsDown(Key.RAlt);
					bool shift = KeyIsDown(Key.LShift) || KeyIsDown(Key.RShift);
					bool ctrl = KeyIsDown(Key.LControl) || KeyIsDown(Key.RControl);
					if(ck == ConsoleKey.Enter && alt){
						if(FullScreen){
							FullScreen = false;
							WindowState = WindowState.Normal;
						}
						else{
							FullScreen = true;
							WindowState = WindowState.Fullscreen;
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
		void MouseMoveHandler(object sender,MouseMoveEventArgs args){
			if(MouseUI.IgnoreMouseMovement){
				return;
			}
			int row;
			int col;
			if(FullScreen){
				row = (int)(args.Y - ClientRectangle.Height * ((1.0f - screen_multiplier_h)*0.5f)) / cell_h; //todo: give this its own var?
				col = (int)(args.X - ClientRectangle.Width * ((1.0f - screen_multiplier_w)*0.5f)) / cell_w;
			}
			else{
				row = args.Y / cell_h;
				col = args.X / cell_w;
			}
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
					if(!Input.KeyPressed && (row != MouseUI.LastRow || col != MouseUI.LastCol) && !KeyIsDown(Key.LControl) && !KeyIsDown(Key.RControl)){
						MouseUI.LastRow = row;
						MouseUI.LastCol = col;
						Input.KeyPressed = true;
						if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
							ConsoleKey key = ConsoleKey.F21;
							Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
						}
						else{
							ConsoleKey key = ConsoleKey.F22;
							Input.LastKey = new ConsoleKeyInfo(Input.GetChar(key,false),key,false,false,false);
						}
					}
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
					/*for(int i=b.row;i<b.row+b.height;++i){
						for(int j=b.col;j<b.col+b.width;++j){
							colorchar cch = Screen.Char(i,j);
							cch.bgcolor = Color.Blue;
							UpdateVertexArray(i,j,cch.c,ConvertColor(cch.color),ConvertColor(cch.bgcolor));
						}
					}*/
				}
				else{
					if(MouseUI.Mode == MouseMode.Map){
						int map_row = row - Global.MAP_OFFSET_ROWS;
						int map_col = col - Global.MAP_OFFSET_COLS;
						PhysicalObject o = null;
						if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
							o = MouseUI.mouselook_objects[map_row,map_col];
							if(MouseUI.VisiblePath && o == null){
								o = Actor.M.tile[map_row,map_col];
							}
						}
						if(MouseUI.mouselook_current_target != null && MouseUI.mouselook_current_target != o){
							MouseUI.RemoveMouseover();
						}
						if(o != null && o != MouseUI.mouselook_current_target){
							MouseUI.mouselook_current_target = o;
							bool description_on_right = false;
							/*int max_length = 29;
							if(map_col - 6 < max_length){
								max_length = map_col - 6;
							}
							if(max_length < 20){
								description_on_right = true;
								max_length = 29;
							}*/
							int max_length = MouseUI.MaxDescriptionBoxLength;
							if(map_col <= 32){
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
								MouseUI.mouse_path = Actor.player.GetPlayerTravelPath(o.p);
								//MouseUI.mouse_path = Actor.player.GetPath(o.row,o.col,-1,true,true,Actor.UnknownTilePathingPreference.UnknownTilesAreOpen);
								if(MouseUI.mouse_path.Count == 0){
									foreach(Tile t in Actor.M.TilesByDistance(o.row,o.col,true,true)){
										if(t.passable){
											MouseUI.mouse_path = Actor.player.GetPlayerTravelPath(t.p);
											//MouseUI.mouse_path = Actor.player.GetPath(t.row,t.col,-1,true,true,Actor.UnknownTilePathingPreference.UnknownTilesAreOpen);
											break;
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
									//Game.gl.UpdateVertexArray(p.row+Global.MAP_OFFSET_ROWS,p.col+Global.MAP_OFFSET_COLS,text_surface,0,(int)cch.c);
									Game.gl.UpdateVertexArray(p.row+Global.MAP_OFFSET_ROWS,p.col+Global.MAP_OFFSET_COLS,text_surface,0,(int)cch.c,cch.color.GetFloatValues(),cch.bgcolor.GetFloatValues());
								}
								if(MouseUI.mouse_path != null && MouseUI.mouse_path.Count == 0){
									MouseUI.mouse_path = null;
								}
							}
						}
					}
				}
				break;
			}
			}
		}
		void MouseClickHandler(object sender,MouseButtonEventArgs args){
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
			int row;
			int col;
			if(FullScreen){
				row = (int)(args.Y - ClientRectangle.Height * ((1.0f - screen_multiplier_h)*0.5f)) / cell_h;
				col = (int)(args.X - ClientRectangle.Width * ((1.0f - screen_multiplier_w)*0.5f)) / cell_w;
			}
			else{
				row = args.Y / cell_h;
				col = args.X / cell_w;
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
						if(map_row >= 0 && map_row < Global.ROWS && map_col >= 0 && map_col < Global.COLS){
							if(map_row == Actor.player.row && map_col == Actor.player.col){
								Input.LastKey = new ConsoleKeyInfo('5',ConsoleKey.NumPad5,false,false,false);
							}
							else{
								if(KeyIsDown(Key.LControl) || KeyIsDown(Key.RControl) || (Math.Abs(map_row-Actor.player.row) <= 1 && Math.Abs(map_col-Actor.player.col) <= 1)){
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
									//Actor.player.path = Actor.player.GetPath(nearest.row,nearest.col,-1,true,true,Actor.UnknownTilePathingPreference.UnknownTilesAreOpen);
									if(Actor.player.path.Count > 0){
										Actor.player.path.StopAtBlockingTerrain();
										if(Actor.player.path.Count > 0){
											Actor.interrupted_path = new pos(-1,-1);
											ConsoleKey path_key = (ConsoleKey)(ConsoleKey.NumPad0 + Actor.player.DirectionOf(Actor.player.path[0]));
											Input.LastKey = new ConsoleKeyInfo(Input.GetChar(path_key,false),path_key,false,false,false);
											Actor.player.path.RemoveAt(0);
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
							Input.LastKey = new ConsoleKeyInfo((char)27,ConsoleKey.Escape,false,false,false);
						}
						break;
					}
					case MouseMode.YesNoPrompt:
						Input.LastKey = new ConsoleKeyInfo('y',ConsoleKey.Y,false,false,false);
						break;
					case MouseMode.Inventory:
						Input.LastKey = new ConsoleKeyInfo('a',ConsoleKey.A,false,false,false);
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
		void HandleRightClick(){
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
		void HandleMiddleClick(){
			if(!Input.KeyPressed){
				Input.KeyPressed = true;
				switch(MouseUI.Mode){
				case MouseMode.Map:
					Input.LastKey = new ConsoleKeyInfo('v',ConsoleKey.V,false,false,false);
					break;
				default:
					Input.LastKey = new ConsoleKeyInfo((char)27,ConsoleKey.Escape,false,false,false);
					break;
				}
			}
			MouseUI.RemoveHighlight();
			MouseUI.RemoveMouseover();
		}
		void MouseWheelHandler(object sender,MouseWheelEventArgs args){
			if(!Input.KeyPressed){
				if(args.Delta > 0){
					switch(MouseUI.Mode){
					case MouseMode.ScrollableMenu:
						Input.KeyPressed = true;
						Input.LastKey = new ConsoleKeyInfo('8',ConsoleKey.NumPad8,false,false,false);
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
						Input.LastKey = new ConsoleKeyInfo('2',ConsoleKey.NumPad2,false,false,false);
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
		void MouseLeaveHandler(object sender,EventArgs args){
			MouseUI.RemoveHighlight();
		}
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e){
			if(NoClose && !Input.KeyPressed && MouseUI.Mode == MouseMode.Map){
				Input.KeyPressed = true;
				Input.LastKey = new ConsoleKeyInfo('q',ConsoleKey.Q,false,false,false);
			}
			base.OnClosing(e);
		}
		protected override void OnResize(EventArgs e){
			if(!Resizing){
				int best = GetBestFontWidth();
				ChangeFont(best);
				base.OnResize(e);
			}
		}
		public void ResizeToDefault(){
			if(!FullScreen){
				Resizing = true;
				ChangeFont(8);
			}
		}
		public int GetBestFontWidth(){
			int largest_possible_tile_h = ClientRectangle.Height / CellRows;
			int largest_possible_tile_w = ClientRectangle.Width / CellCols;
			int largest_possible = Math.Min(largest_possible_tile_h/2,largest_possible_tile_w);
			if(largest_possible < 8){ //current valid sizes by width: 6,8,12,16,24,32
				return 6;
			}
			if(largest_possible < 12){
				return 8;
			}
			if(largest_possible < 16){
				return 12;
			}
			if(largest_possible < 24){
				return 16;
			}
			if(largest_possible < 32){
				return 24;
			}
			return 32;
		}
		public void ChangeFont(int new_width){
			if(new_width != cell_w){
				string font = "";
				float previous_w = text_surface.SpriteWidthPadded;
				switch(new_width){
				case 6:
					font = "font6x12.bmp";
					text_surface.SpriteWidthPadded = text_surface.SpriteWidth;
					break;
				case 8:
					font = "font8x16.bmp";
					text_surface.SpriteWidthPadded = text_surface.SpriteWidth * 8.0f / 9.0f;
					break;
				case 12:
					font = "font12x24.bmp";
					text_surface.SpriteWidthPadded = text_surface.SpriteWidth;
					break;
				case 16:
					font = "font16x32.bmp";
					text_surface.SpriteWidthPadded = text_surface.SpriteWidth;
					break;
				case 24:
					font = "font12x24.bmp";
					text_surface.SpriteWidthPadded = text_surface.SpriteWidth;
					break;
				case 32:
					font = "font16x32.bmp";
					text_surface.SpriteWidthPadded = text_surface.SpriteWidth;
					break;
				}
				cell_w = new_width;
				cell_h = new_width * 2;
				SnapHeight = cell_h * CellRows;
				SnapWidth = cell_w * CellCols;
				text_surface.TileWidth = new_width;
				text_surface.TileHeight = new_width * 2;
				ReplaceTexture(text_surface.TextureIndex,font);
				float width_difference = text_surface.SpriteWidthPadded - previous_w;
				if(width_difference != 0.0f){
					SpriteSurface s = text_surface;
					GL.BindBuffer(BufferTarget.ArrayBuffer,s.ArrayBufferID);
					IntPtr vbo = GL.MapBuffer(BufferTarget.ArrayBuffer,BufferAccess.ReadWrite);
					int max = s.Rows * s.Cols * 4 * s.TotalVertexAttribSize;
					for(int i=0;i<max;i += s.TotalVertexAttribSize*4){
						int offset = (i + 2 + s.TotalVertexAttribSize*2) * 4; //4 bytes per float
						byte[] bytes = BitConverter.GetBytes(width_difference + BitConverter.ToSingle(new byte[]{Marshal.ReadByte(vbo,offset),Marshal.ReadByte(vbo,offset+1),Marshal.ReadByte(vbo,offset+2),Marshal.ReadByte(vbo,offset+3)},0));
						for(int j=0;j<4;++j){
							Marshal.WriteByte(vbo,offset+j,bytes[j]);
						}
						offset += s.TotalVertexAttribSize * 4;
						bytes = BitConverter.GetBytes(width_difference + BitConverter.ToSingle(new byte[]{Marshal.ReadByte(vbo,offset),Marshal.ReadByte(vbo,offset+1),Marshal.ReadByte(vbo,offset+2),Marshal.ReadByte(vbo,offset+3)},0));
						for(int j=0;j<4;++j){
							Marshal.WriteByte(vbo,offset+j,bytes[j]);
						}
					}
					GL.UnmapBuffer(BufferTarget.ArrayBuffer);
				}
			}
		}
		public static Color4 ConvertColor(Color c){
			switch(c){
			case Color.Black:
				return Color4.Black;
			case Color.Blue:
				return new Color4(20,20,255,255);
				//return Color4.Blue;
			case Color.Cyan:
				return Color4.Cyan;
			case Color.DarkBlue:
				return new Color4(10,10,149,255);
				//return Color4.DarkBlue;
			case Color.DarkCyan:
				return Color4.DarkCyan;
			case Color.DarkGray:
				return Color4.DimGray;
			case Color.DarkGreen:
				return Color4.DarkGreen;
			case Color.DarkMagenta:
				return Color4.DarkMagenta;
			case Color.DarkRed:
				return Color4.DarkRed;
			case Color.DarkYellow:
				return Color4.DarkGoldenrod;
			case Color.Gray:
				return Color4.LightGray;
			case Color.Green:
				return Color4.Lime;
			case Color.Magenta:
				return Color4.Magenta;
			case Color.Red:
				return Color4.Red;
			case Color.White:
				return Color4.White;
			case Color.Yellow:
				return new Color4(255,248,0,255);
				//return Color4.Yellow;
			case Color.DarkerGray:
				return new Color4(50,50,50,255);
			case Color.Transparent:
				return Color4.Transparent;
			default:
				return Color4.Black;
			}
		}
		public static string GetParticleFragmentShader(){
			return 
				@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;
varying vec4 color_fs;
varying vec4 bgcolor_fs;

void main(){
vec4 v = texture2D(texture,texcoord_fs);
if(v.a > 0.1){
 if(v.r > 0.9){
  gl_FragColor = bgcolor_fs;
 }
 else{
  gl_FragColor = color_fs;
 }
}
else{
 gl_FragColor = v;
}
}
";
		}
		public void UpdateParticles(SpriteSurface s,List<AnimationParticle> l){
			if(l.Count == 0){
				s.NumElements = 0;
				s.Disabled = true;
				return;
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer,s.ArrayBufferID);
			int count = l.Count;
			s.NumElements = count * 6;
			List<float> all_values = new List<float>(4 * s.TotalVertexAttribSize * count);
			int[] indices = new int[s.NumElements];
			for(int i=0;i<count;++i){
				float tex_start_h = s.SpriteHeight * (float)l[i].sprite_pixel_row;
				float tex_start_w = s.SpriteWidth * (float)l[i].sprite_pixel_col;
				float tex_end_h = tex_start_h + s.SpriteHeight * (float)l[i].sprite_h;
				float tex_end_w = tex_start_w + s.SpriteWidth * (float)l[i].sprite_w;
				float flipped_row = (float)(s.Rows-1) - l[i].row;
				float col = l[i].col;
				float fi = screen_multiplier_h * ((flipped_row / s.HeightScale) + s.GLCoordHeightOffset);
				float fj = screen_multiplier_w * ((col / s.WidthScale) + s.GLCoordWidthOffset);
				float fi_plus1 = screen_multiplier_h * (((flipped_row+((float)l[i].sprite_h / (float)s.TileHeight)) / s.HeightScale) + s.GLCoordHeightOffset);
				float fj_plus1 = screen_multiplier_w * (((col+((float)l[i].sprite_w / (float)s.TileWidth)) / s.WidthScale) + s.GLCoordWidthOffset);
				float[] values = new float[4 * s.TotalVertexAttribSize];
				values[0] = fj;
				values[1] = fi;
				values[2] = tex_start_w;
				values[3] = tex_end_h;
				values[s.TotalVertexAttribSize] = fj;
				values[1 + s.TotalVertexAttribSize] = fi_plus1;
				values[2 + s.TotalVertexAttribSize] = tex_start_w;
				values[3 + s.TotalVertexAttribSize] = tex_start_h;
				values[s.TotalVertexAttribSize*2] = fj_plus1;
				values[1 + s.TotalVertexAttribSize*2] = fi_plus1;
				values[2 + s.TotalVertexAttribSize*2] = tex_end_w;
				values[3 + s.TotalVertexAttribSize*2] = tex_start_h;
				values[s.TotalVertexAttribSize*3] = fj_plus1;
				values[1 + s.TotalVertexAttribSize*3] = fi;
				values[2 + s.TotalVertexAttribSize*3] = tex_end_w;
				values[3 + s.TotalVertexAttribSize*3] = tex_end_h;
				int total_of_previous_attribs = 4;
				int k=0;
				foreach(float attrib in l[i].primary_color.GetFloatValues()){
					values[total_of_previous_attribs+k] = attrib;
					values[total_of_previous_attribs+k+s.TotalVertexAttribSize] = attrib;
					values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*2)] = attrib;
					values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*3)] = attrib;
					++k;
				}
				total_of_previous_attribs += 4;
				k=0;
				foreach(float attrib in l[i].secondary_color.GetFloatValues()){
					values[total_of_previous_attribs+k] = attrib;
					values[total_of_previous_attribs+k+s.TotalVertexAttribSize] = attrib;
					values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*2)] = attrib;
					values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*3)] = attrib;
					++k;
				}
				all_values.AddRange(values);
				int idx4 = i * 4;
				int idx6 = i * 6;
				indices[idx6] = idx4;
				indices[idx6 + 1] = idx4 + 1;
				indices[idx6 + 2] = idx4 + 2;
				indices[idx6 + 3] = idx4;
				indices[idx6 + 4] = idx4 + 2;
				indices[idx6 + 5] = idx4 + 3;
			}
			GL.BufferData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)* 4 * s.TotalVertexAttribSize * count),all_values.ToArray(),BufferUsageHint.StreamDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer,s.ElementArrayBufferID);
			GL.BufferData(BufferTarget.ElementArrayBuffer,new IntPtr(sizeof(int)*indices.Length),indices,BufferUsageHint.StreamDraw);
		}
	}
}
