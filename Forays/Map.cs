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
using SchismDungeonGenerator;
using Utilities;
using SchismExtensionMethods;
namespace Forays{
	public enum LevelType{Standard,Cave,Hive,Mine,Fortress,Slime,Garden,Crypt}; //rename slime --> sewer
	public class Map{
		public PosArray<Tile> tile = new PosArray<Tile>(ROWS,COLS);
		public PosArray<Actor> actor = new PosArray<Actor>(ROWS,COLS);
		public int current_level;
		public List<LevelType> level_types;
		public bool wiz_lite{get{ return internal_wiz_lite; }
			set{
				internal_wiz_lite = value;
				if(value == true){
					foreach(Tile t in AllTiles()){
						if(t.Is(TileType.BLAST_FUNGUS)){
							B.Add("The blast fungus starts to smolder in the light. ",t);
							t.Toggle(null);
							if(t.inv == null){ //should always be true
								Item.Create(ConsumableType.BLAST_FUNGUS,t.row,t.col);
								t.inv.other_data = 3;
								t.inv.revealed_by_light = true;
							}
							Q.Add(new Event(t.inv,100,EventType.BLAST_FUNGUS));
						}
					}
				}
			}
		}
		private bool internal_wiz_lite;
		public bool wiz_dark{get{ return internal_wiz_dark; }
			set{
				internal_wiz_dark = value;
				if(value == false){
					foreach(Tile t in AllTiles()){
						if(t.Is(TileType.BLAST_FUNGUS) && t.light_value > 0){
							B.Add("The blast fungus starts to smolder in the light. ",t);
							t.Toggle(null);
							if(t.inv == null){ //should always be true
								Item.Create(ConsumableType.BLAST_FUNGUS,t.row,t.col);
								t.inv.other_data = 3;
								t.inv.revealed_by_light = true;
							}
							Q.Add(new Event(t.inv,100,EventType.BLAST_FUNGUS));
						}
					}
				}
			}
		}
		private bool internal_wiz_dark;
		private Dict<ActorType,int> generated_this_level = null; //used for rejecting monsters if too many already exist on the current level
		private PosArray<int> monster_density = null;
		private bool[,] danger_sensed;
		private static List<pos> allpositions = new List<pos>();
		public PosArray<int> safetymap = null;
		public PosArray<int> poppy_distance_map = null;
		//public int[,] row_displacement = null;
		//public int[,] col_displacement = null;
		public colorchar[,] last_seen = new colorchar[ROWS,COLS];
		public int[] final_level_cultist_count = null;
		public int final_level_demon_count = 0;
		public int final_level_clock = 0;
		public bool feat_gained_this_level = false;
		public int extra_danger = 0; //used to eventually spawn more threatening wandering monsters
		public static pos[] shrine_locations;

		public static Color darkcolor = Color.DarkCyan;
		public static Color unseencolor = Color.OutOfSight;
		private const int ROWS = Global.ROWS;
		private const int COLS = Global.COLS;
		public static Actor player;
		public static Queue Q;
		public static Buffer B;
		static Map(){
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					allpositions.Add(new pos(i,j));
				}
			}
		}
		public Map(Game g){
			//tile = new Tile[ROWS,COLS];
			//actor = new Actor[ROWS,COLS];
			current_level = 0;
			Map.player = g.player;
			Map.Q = g.Q;
			Map.B = g.B;
		}
		public bool BoundsCheck(int r,int c){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				return true;
			}
			return false;
		}
		public bool BoundsCheck(pos p){
			if(p.row>=0 && p.row<ROWS && p.col>=0 && p.col<COLS){
				return true;
			}
			return false;
		}
		public List<Tile> AllTiles(){ //possible speed issues? is there anywhere that I should be using 'alltiles' directly?
			List<Tile> result = new List<Tile>(); //should i have one method that allows modification and one that doesn't?
			if(tile[0,0] == null){ //if one is null, they are all null.
				return result;
			}
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					result.Add(tile[i,j]);
				}
			}
			return result;
		}
		public List<Actor> AllActors(){ //todo: make this return just the ones from tiebreakers? if so, check for null AND check for burrowing or otherwise removed from the map!
			List<Actor> result = new List<Actor>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j] != null){
						result.Add(actor[i,j]);
					}
				}
			}
			return result;
		}
		public List<pos> AllPositions(){ return allpositions; }
		public LevelType ChooseNextLevelType(LevelType current){
			List<LevelType> types = new List<LevelType>();
			foreach(LevelType l in Enum.GetValues(typeof(LevelType))){
				if(l != current && l != LevelType.Slime){
					types.Add(l);
				}
			}
			return types.Random();
		}
		public IEnumerable<Tile> ReachableTilesByDistance(int origin_row,int origin_col,bool return_reachable_walls,params TileType[] tiles_considered_passable){
			int[,] values = new int[ROWS,COLS]; //note that this method never returns the map borders. it'd need to check bounds if i wanted that.
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					bool passable = tile[i,j].passable;
					foreach(TileType tt in tiles_considered_passable){
						if(tile[i,j].type == tt){
							passable = true;
							break;
						}
					}
					if(return_reachable_walls && !tile[i,j].solid_rock){
						passable = true;
					}
					if(passable){
						values[i,j] = 0;
					}
					else{
						values[i,j] = -1;
					}
				}
			}
			int minrow = 1;
			int maxrow = ROWS-2;
			int mincol = 1; //todo: make it start at 1 radius and go out from there until it hits these limits.
			int maxcol = COLS-2;
			values[origin_row,origin_col] = 1;
			int val = 1;
			bool done = false;
			List<Tile> just_added = new List<Tile>{tile[origin_row,origin_col]};
			while(!done){
				done = true;
				while(just_added.Count > 0){
					yield return just_added.RemoveRandom();
				}
				for(int i=minrow;i<=maxrow;++i){
					for(int j=mincol;j<=maxcol;++j){
						if(values[i,j] == val){
							for(int s=i-1;s<=i+1;++s){
								for(int t=j-1;t<=j+1;++t){
									if(values[s,t] == 0){
										values[s,t] = val + 1;
										done = false;
										just_added.Add(tile[s,t]);
									}
								}
							}
						}
					}
				}
				++val;
			}
		}
		public IEnumerable<Tile> TilesByDistance(int origin_row,int origin_col,bool cardinal_directions_first,bool deterministic_results){
			Tile t = tile[origin_row,origin_col];
			int current_distance = 0;
			List<Tile> current_list = null;
			while(true){
				current_list = t.TilesAtDistance(current_distance);
				if(current_list.Count == 0){
					break;
				}
				while(current_list.Count > 0){
					if(cardinal_directions_first){
						List<Tile> closest = current_list.WhereLeast(x=>t.ApproximateEuclideanDistanceFromX10(x));
						foreach(Tile t2 in closest){
							current_list.Remove(t2);
						}
						while(closest.Count > 0){
							if(deterministic_results){
								yield return closest.RemoveLast();
							}
							else{
								yield return closest.RemoveRandom();
							}
						}
					}
					else{
						if(deterministic_results){
							yield return current_list.RemoveLast();
						}
						else{
							yield return current_list.RemoveRandom();
						}
					}
				}
				++current_distance;
			}
			/*int[,] values = new int[ROWS,COLS];
			values[origin_row,origin_col] = 1;
			int val = 1;
			bool done = false;
			while(!done){
				done = true;
				while(just_added.Count > 0){
					if(deterministic_results){
						yield return just_added.RemoveLast();
					}
					else{
						yield return just_added.RemoveRandom();
					}
				}
				for(int i=minrow;i<=maxrow;++i){
					for(int j=mincol;j<=maxcol;++j){
						if(values[i,j] == val){
							for(int s=i-1;s<=i+1;++s){
								for(int t=j-1;t<=j+1;++t){
									if(BoundsCheck(s,t) && values[s,t] == 0){
										values[s,t] = val + 1;
										done = false;
										just_added.Add(tile[s,t]);
									}
								}
							}
						}
					}
				}
				++val;
			}*/
		}
		public void GenerateLevelTypes(){
			level_types = new List<LevelType>();
			LevelType current = LevelType.Standard;
			while(level_types.Count < 20){
				int num = R.Roll(2,2) - 1;
				for(int i=0;i<num;++i){
					if(level_types.Count < 20){
						level_types.Add(current);
					}
				}
				current = ChooseNextLevelType(current);
			}
		}
		public void UpdateDangerValues(){
			danger_sensed = new bool[ROWS,COLS];
			foreach(Actor a in AllActors()){
				if(a != player && (a.HasAttr(AttrType.DANGER_SENSED) || player.CanSee(a))){
					//a.attrs[AttrType.DANGER_SENSED] = 1;
					foreach(Tile t in AllTiles()){
						if(danger_sensed[t.row,t.col] == false && t.passable && !t.opaque){
							if(a.CanSee(t)){
								int multiplier = a.HasAttr(AttrType.KEEN_SENSES)? 5 : 10;
								int stealth = player.TotalSkill(SkillType.STEALTH);
								if(!player.tile().IsLit()){
									stealth -= 2; //remove any bonus from the player's own tile...
								}
								if(!t.IsLit()){
									stealth += 2; //...and add any bonus from the tile in question
								}
								int value = (stealth * a.DistanceFrom(t) * multiplier) - 5 * a.player_visibility_duration;
								if(value < 100 || a.player_visibility_duration < 0){
									danger_sensed[t.row,t.col] = true;
								}
							}
						}
					}
				}
			}
		}
		public void UpdateSafetyMap(params PhysicalObject[] sources){
			List<cell> sourcelist = new List<cell>();
			foreach(PhysicalObject o in sources){
				sourcelist.Add(new cell(o.row,o.col,0));
			}
			IntLocationDelegate get_cost = (r,c) => {
				if(actor[r,c] != null){
					return 20 + (10 * actor[r,c].attrs[AttrType.TURNS_HERE]);
					//return 20;
				}
				else{
					if(tile[r,c].Is(TileType.DOOR_C,TileType.RUBBLE)){
						return 20;
					}
					else{
						return 10;
					}
				}
			};
			PosArray<int> a = GetDijkstraMap(Global.ROWS,Global.COLS,
			                                (s,t)=>!tile[s,t].passable && !tile[s,t].IsDoorType(false),
			                                //(s,t)=>tile[s,t].Is(TileType.WALL,TileType.HIDDEN_DOOR,TileType.STONE_SLAB,TileType.STATUE), 
											(u,v) => 10,sourcelist);
			for(int i=0;i<Global.ROWS;++i){
				for(int j=0;j<Global.COLS;++j){
					if(a[i,j] != U.DijkstraMin){
						if(a[i,j] != U.DijkstraMax && a[i,j] > 200){
							a[i,j] = 200; //todo: testing this modification. the idea here is to make more "best" spots to flee to, reducing the draw of the few farthest ones.
						}
						a[i,j] = -(a[i,j] * 14) / 10; //changed from 1.2 to 1.4 to test
					}
				}
			}
			foreach(PhysicalObject o in sources){
				a[o.row,o.col] = U.DijkstraMin; //now the player (or other sources) become blocking
			}
			UpdateDijkstraMap(a,get_cost);
			/*foreach(PhysicalObject o in sources){ //add a penalty for tiles adjacent to the player
				foreach(pos p in o.PositionsAtDistance(1)){
					if(a[p] != U.DijkstraMax && a[p] != U.DijkstraMin){
						a[p] += 30;
					}
				}
			}*/
			safetymap = a;
		}
		public delegate bool BooleanLocationDelegate(int row,int col);
		public delegate int IntLocationDelegate(int row,int col); //todo: remove dijkstra from this file, use the one in Utility?  (do I have UpdateDijkstra in Utility?)
		public static PosArray<int> GetDijkstraMap(int height,int width,BooleanLocationDelegate is_blocked,IntLocationDelegate get_cost,List<cell> sources){
			PriorityQueue<cell> frontier = new PriorityQueue<cell>(c => -c.value);
			PosArray<int> map = new PosArray<int>(height,width);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(is_blocked(i,j)){
						map[i,j] = U.DijkstraMin;
					}
					else{
						map[i,j] = U.DijkstraMax;
					}
				}
			}
			foreach(cell c in sources){
				map[c.row,c.col] = c.value;
				frontier.Add(c);
			}
			while(frontier.list.Count > 0){
				cell c = frontier.Pop();
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(c.row+s >= 0 && c.row+s < height && c.col+t >= 0 && c.col+t < width){
							int cost = get_cost(c.row+s,c.col+t);
							if(map[c.row+s,c.col+t] > c.value+cost){
								map[c.row+s,c.col+t] = c.value+cost;
								frontier.Add(new cell(c.row+s,c.col+t,c.value+cost));
							}
						}
					}
				}
			}
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(map[i,j] == U.DijkstraMax){
						map[i,j] = U.DijkstraMin; //any unreachable areas are marked unpassable
					}
				}
			}
			return map;
		}
		public static void UpdateDijkstraMap(PosArray<int> map,IntLocationDelegate get_cost){
			PriorityQueue<cell> frontier = new PriorityQueue<cell>(c => -c.value);
			int height = map.objs.GetLength(0);
			int width = map.objs.GetLength(1);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(map[i,j] != U.DijkstraMin){
						int v = map[i,j];
						bool good = true;
						for(int s=-1;s<=1 && good;++s){
							for(int t=-1;t<=1 && good;++t){
								if(i+s >= 0 && i+s < height && j+t >= 0 && j+t < width){
									if(map[i+s,j+t] < v && map[i+s,j+t] != U.DijkstraMin){
										good = false;
									}
								}
							}
						}
						if(good){ //find local minima and add them to the frontier
							frontier.Add(new cell(i,j,v));
						}
					}
				}
			}
			while(frontier.list.Count > 0){
				cell c = frontier.Pop();
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(c.row+s >= 0 && c.row+s < height && c.col+t >= 0 && c.col+t < width){
							int cost = get_cost(c.row+s,c.col+t);
							if(map[c.row+s,c.col+t] > c.value+cost){
								map[c.row+s,c.col+t] = c.value+cost;
								frontier.Add(new cell(c.row+s,c.col+t,c.value+cost));
							}
						}
					}
				}
			}
		}
		public void InitLevel(){ //creates an empty level surrounded by walls. used for testing purposes.
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					tile[i,j] = null;
					if(i==0 || j==0 || i==ROWS-1 || j==COLS-1){
						tile[i,j] = Tile.Create(TileType.WALL,i,j);
					}
					else{
						tile[i,j] = Tile.Create(TileType.FLOOR,i,j);
					}
					//alltiles.Add(tile[i,j]);
				}
			}
		}
		/*public void LoadLevel(string filename){ //this is ancient and was only used for testing purposes.
			TextReader file = new StreamReader(filename);
			char ch;
			List<Tile> hidden = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					ch = (char)file.Read();
					switch(ch){
					case '#':
						Tile.Create(TileType.WALL,i,j);
						break;
					case '.':
						Tile.Create(TileType.FLOOR,i,j);
						break;
					case '+':
						Tile.Create(TileType.DOOR_C,i,j);
						break;
					case '-':
						Tile.Create(TileType.DOOR_O,i,j);
						break;
					case '>':
						Tile.Create(TileType.STAIRS,i,j);
						break;
					case 'H':
						Tile.Create(TileType.HIDDEN_DOOR,i,j);
						hidden.Add(tile[i,j]);
						break;
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					//alltiles.Add(tile[i,j]);
				}
				file.ReadLine();
			}
			file.Close();
			if(hidden.Count > 0){
				Event e = new Event(hidden,100,EventType.CHECK_FOR_HIDDEN);
				e.tiebreaker = 0;
				Q.Add(e);
			}
		}*/
		public void Draw(){ //Draw should be faster than Redraw when most of the screen is unchanged.
			if(Screen.MapChar(0,0).c == '-' && !Global.GRAPHICAL){ //kinda hacky. there won't be an open door in the corner, so this looks for
				Redraw(); //evidence of Select being called (& therefore, the map needing to be redrawn entirely) //todo! this breaks in console mode if you have the option on.
			}
			else{
				MouseUI.mouselook_objects = new PhysicalObject[ROWS,COLS];
				Screen.CursorVisible = false;
				int i_start = 0;
				int j_start = 0;
				int row_limit = ROWS;
				int col_limit = COLS;
				if(player.HasAttr(AttrType.BURNING)){
					DrawMapBorder('&',Color.RandomFire);
					i_start++;
					j_start++;
					row_limit--;
					col_limit--;
				}
				else{
					if(player.HasAttr(AttrType.FROZEN)){
						DrawMapBorder('#',Color.RandomIce);
						i_start++;
						j_start++;
						row_limit--;
						col_limit--;
					}
				}
				if(!Global.GRAPHICAL){
					for(int i=i_start;i<row_limit;++i){ //if(ch.c == '#'){ ch.c = Encoding.GetEncoding(437).GetChars(new byte[] {177})[0]; } <--this shows how to print non-ASCII symbols in the windows console.
						for(int j=j_start;j<col_limit;++j){
							Screen.WriteMapChar(i,j,VisibleColorChar(i,j));
						}
					}
				}
				/*else{
					for(int i=i_start;i<row_limit;++i){
						for(int j=j_start;j<col_limit;++j){
							VisibleColorChar(i,j); //mark tiles as seen, etc.
							Screen.WriteMapChar(i,j,' ',Color.Transparent,Color.Transparent);
						}
					}
					int graphics_mode_first_col = Screen.screen_center_col - 16;
					int graphics_mode_last_col = Screen.screen_center_col + 16;
					if(graphics_mode_first_col < 0){
						graphics_mode_last_col -= graphics_mode_first_col;
						graphics_mode_first_col = 0;
					}
					else{
						if(graphics_mode_last_col >= COLS){
							int diff = graphics_mode_last_col - (COLS-1);
							graphics_mode_first_col -= diff;
							graphics_mode_last_col = COLS-1;
						}
					}
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<33;++j){ //todo: actually COLS/2
							pos offset = new pos(i,j+graphics_mode_first_col);
							if(tile[offset].seen){
								if(player.CanSee(tile[offset])){
									pos spr = tile[offset].sprite_offset;
									if(tile[offset].IsLit()){
										Screen.UpdateSurface(i,j,GLGame.graphics_surface,spr.row,spr.col);
										//Screen.UpdateSurface(i,j,GLGame.visibility_surface,0,0);
									}
									else{
										Screen.UpdateSurface(i,j,GLGame.graphics_surface,spr.row,spr.col,0.5f,0.5f,0.85f);
										//Screen.UpdateSurface(i,j,GLGame.visibility_surface,0,1);
									}
								}
								else{
									Screen.UpdateSurface(i,j,GLGame.graphics_surface,tile[offset].sprite_offset.row,tile[offset].sprite_offset.col,0.5f,0.5f,0.5f);
									//Screen.UpdateSurface(i,j,GLGame.visibility_surface,0,2);
								}
							}
							else{
								Screen.UpdateSurface(i,j,GLGame.graphics_surface,16,0);
								//Game.gl.UpdateVertexArray(i,j,GLGame.graphics_surface,16,0);
							}
							if(actor[offset] != null){
								Screen.UpdateSurface(i,j,GLGame.actors_surface,actor[offset].sprite_offset.row,actor[offset].sprite_offset.col);
							}
							else{
								if(tile[offset].features.Count > 0){
									pos spr = tile[offset].FeatureSprite();
									if(tile[offset].IsLit()){
										Screen.UpdateSurface(i,j,GLGame.actors_surface,spr.row,spr.col);
									}
									else{
										Screen.UpdateSurface(i,j,GLGame.actors_surface,spr.row,spr.col,0.5f,0.5f,0.85f);
									}
								}
								else{
									if(tile[offset].inv != null){
										Screen.UpdateSurface(i,j,GLGame.actors_surface,tile[offset].inv.sprite_offset.row,tile[offset].inv.sprite_offset.col);
									}
									else{
										Screen.UpdateSurface(i,j,GLGame.actors_surface,32,40); //todo: find a static spot for a transparent sprite
									}
								}
							}
						}
					}
				}*/
				Screen.ResetColors();
				Screen.NoGLUpdate = false;
				Game.GLUpdate();
			}
		}
		public void Redraw(){ //Redraw should be faster than Draw when most of the screen has changed.
			MouseUI.mouselook_objects = new PhysicalObject[ROWS,COLS];
			Screen.CursorVisible = false;
			int i_start = 0;
			int j_start = 0;
			int row_limit = ROWS;
			int col_limit = COLS;
			Screen.NoGLUpdate = true;
			if(player.HasAttr(AttrType.BURNING)){
				DrawMapBorder('&',Color.RandomFire);
				i_start++;
				j_start++;
				row_limit--;
				col_limit--;
			}
			else{
				if(player.HasAttr(AttrType.FROZEN)){
					DrawMapBorder('#',Color.RandomIce);
					i_start++;
					j_start++;
					row_limit--;
					col_limit--;
				}
			}
			if(Screen.GLMode){
				for(int i=i_start;i<row_limit;++i){
					for(int j=j_start;j<col_limit;++j){
						Screen.WriteMapChar(i,j,VisibleColorChar(i,j));
					}
				}
				Screen.UpdateGLBuffer(Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,Global.MAP_OFFSET_ROWS + Global.ROWS - 1,Global.MAP_OFFSET_COLS + Global.COLS - 1);
			}
			else{
				cstr s;
				s.s = "";
				s.bgcolor = Color.Black;
				s.color = Color.Black;
				int r = 0;
				int c = 0;
				for(int i=i_start;i<row_limit;++i){
					s.s = "";
					r = i;
					c = j_start;
					for(int j=j_start;j<col_limit;++j){
						colorchar ch = VisibleColorChar(i,j);
						ch.color = Screen.ResolveColor(ch.color);
						if(ch.color != s.color){ //ignores background color, assumes black
							if(s.s.Length > 0){
								Screen.WriteMapString(r,c,s);
								s.s = "";
								s.s += ch.c;
								s.color = ch.color;
								r = i;
								c = j;
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
					Screen.WriteMapString(r,c,s);
				}
				Screen.ResetColors();
			}
			Screen.NoGLUpdate = false;
		}
		public colorchar VisibleColorChar(int r,int c){
			colorchar ch = Screen.BlankChar();
			if(player.CanSee(r,c)){
				tile[r,c].seen = true;
				if(tile[r,c].IsLit() || player.HasAttr(AttrType.SHADOWSIGHT)){
					if(tile[r,c].IsKnownTrap() || tile[r,c].IsShrine() || tile[r,c].Is(TileType.RUINED_SHRINE)){
						tile[r,c].revealed_by_light = true;
					}
					if(tile[r,c].inv != null){
						tile[r,c].inv.revealed_by_light = true;
					}
				}
				if(tile[r,c].inv != null && !tile[r,c].IsBurning()){
					ch.c = tile[r,c].inv.symbol;
					ch.color = tile[r,c].inv.color;
					if(!tile[r,c].inv.revealed_by_light && !tile[r,c].IsLit()){
						ch.color = darkcolor;
					}
					last_seen[r,c] = ch;
					MouseUI.mouselook_objects[r,c] = tile[r,c].inv;
				}
				else{
					if(tile[r,c].features.Count > 0){
						ch = tile[r,c].FeatureVisual();
						last_seen[r,c] = ch;
					}
					else{
						ch.c = tile[r,c].symbol;
						ch.color = tile[r,c].color;
						if(ch.c == '#' && ch.color == Color.RandomGlowingFungus && !wiz_dark){
							bool fungus_found = false;
							foreach(Tile t in tile[r,c].NonOpaqueNeighborsBetween(player.row,player.col)){
								if(t.type == TileType.GLOWING_FUNGUS){
									fungus_found = true;
								}
								if(t.light_value > 0){ //they don't color walls that are lit by another light source
									fungus_found = false;
									break;
								}
							}
							if(!fungus_found || wiz_lite){
								ch.color = Color.Gray;
							}
						}
						if(!tile[r,c].revealed_by_light && !tile[r,c].IsLit()){
							if(tile[r,c].IsKnownTrap() || tile[r,c].IsShrine() || tile[r,c].Is(TileType.RUINED_SHRINE)){
								ch.color = darkcolor;
								last_seen[r,c] = ch;
							}
							else{
								last_seen[r,c] = ch;
								ch.color = darkcolor;
							}
						}
						else{
							last_seen[r,c] = ch;
						}
						if(player.HasFeat(FeatType.DANGER_SENSE) && danger_sensed != null
						   && danger_sensed[r,c] && player.LightRadius() == 0
						   && !wiz_lite && !tile[r,c].IsKnownTrap() && !tile[r,c].IsShrine()){
							if(tile[r,c].IsLit()){
								ch.color = Color.Red;
							}
							else{
								ch.color = Color.DarkRed;
							}
						}
					}
				}
				if(actor[r,c] != null && player.CanSee(actor[r,c])){
					actor[r,c].attrs[AttrType.DANGER_SENSED] = 1;
					actor[r,c].attrs[AttrType.NOTICED] = 1;
					ch.c = actor[r,c].symbol;
					ch.color = actor[r,c].color;
					if(actor[r,c] != player){
						MouseUI.mouselook_objects[r,c] = actor[r,c];
					}
					if(actor[r,c] == player && player.HasFeat(FeatType.DANGER_SENSE)
					&& danger_sensed != null && danger_sensed[r,c] && player.LightRadius() == 0
					&& !wiz_lite){
						if(tile[r,c].IsLit() && !player.HasAttr(AttrType.BLIND)){
							ch.color = Color.Red;
						}
						else{
							ch.color = Color.DarkRed;
						}
					}
					else{
						if(actor[r,c] == player){
							if(!player.HasAttr(AttrType.BLIND)){
								if(player.IsInvisibleHere()){
									ch.color = Color.DarkGray;
								}
								if(!tile[r,c].IsLit()){
									bool hidden_in_corner = false;
									if(player.HasFeat(FeatType.CORNER_CLIMB) && !player.tile().IsLit()){
										List<pos> valid_open_doors = new List<pos>();
										foreach(int dir in U.DiagonalDirections){
											if(player.TileInDirection(dir).type == TileType.DOOR_O){
												valid_open_doors.Add(player.TileInDirection(dir).p);
											}
										}
										if(SchismExtensionMethods.Extensions.ConsecutiveAdjacent(player.p,x=>valid_open_doors.Contains(x) || tile[x].Is(TileType.WALL,TileType.CRACKED_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR,TileType.STATUE,TileType.STONE_SLAB,TileType.WAX_WALL)) >= 5){
											hidden_in_corner = true;
										}
									}
									if(player.HasAttr(AttrType.SHADOW_CLOAK) || hidden_in_corner){
										ch.color = Color.DarkBlue;
									}
									else{
										if(ch.color != Color.DarkGray){ //if it's dark gray at this point, it means you're invisible. hacky.
											ch.color = darkcolor;
										}
									}
								}
							}
						}
					}
				}
			}
			else{
				if(actor[r,c] != null && player.CanSee(actor[r,c])){
					ch.c = actor[r,c].symbol;
					ch.color = actor[r,c].color;
					if(actor[r,c] != player){
						MouseUI.mouselook_objects[r,c] = actor[r,c];
					}
				}
				else{
					if(tile[r,c].seen){
						ch.c = last_seen[r,c].c;
						ch.color = unseencolor;
					}
				}
			}
			return ch;
		}
		public void DrawMapBorder(char ch,Color color){ //calls Screen.DrawMapBorder and handles updating 'seen' variables for tiles
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					VisibleColorChar(i,j);
					if(i != 0 && i != ROWS-1){
						j += ROWS-2; //meh
					}
				}
			}
			Screen.DrawMapBorder(new colorchar(ch,color));
		}
		public void RemoveTargets(Actor a){ //cleanup of references to dead monsters
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j]!=null){
						actor[i,j].RemoveTarget(a);
					}
				}
			}
			Fire.burning_objects.Remove(a);
		}
		public Item SpawnItem(){
			ConsumableType result = Item.RandomItem();
			for(bool done=false;!done;){
				int rr = R.Roll(ROWS-2);
				int rc = R.Roll(COLS-2);
				Tile t = tile[rr,rc];
				if(t.passable && t.inv == null && t.type != TileType.CHEST && t.type != TileType.FIREPIT
				&& t.type != TileType.STAIRS && !t.IsShrine()){
					return Item.Create(result,rr,rc);
					//done = true;
				}
			}
			//return result;
			return null;
		}
		public ActorType MobType(){
			ActorType result = ActorType.SPECIAL;
			bool good_result = false;
			while(!good_result){
				int level = 1;
				int monster_depth = (current_level+1) / 2; //1-10, not 1-20
				if(current_level != 1){ //depth 1 only generates level 1 monsters
					List<int> levels = new List<int>();
					for(int i=-2;i<=2;++i){
						if(monster_depth + i >= 1 && monster_depth + i <= 10){
							int j = 1 + Math.Abs(i);
							if(R.OneIn(j)){ //current depth is considered 1 out of 1 times, depth+1 and depth-1 one out of 2 times, etc.
								levels.Add(monster_depth + i);
							}
						}
					}
					level = levels.Random();
				}
				LevelType lt = level_types[current_level-1];
				if(R.OneIn(10) && (lt == LevelType.Cave || lt == LevelType.Crypt || lt == LevelType.Hive || lt == LevelType.Mine || lt == LevelType.Slime)){
					result = ActorType.SPECIAL; //zombies in crypts, kobolds in mines, etc.
				}
				else{
					if(level == 1){ //level 1 monsters are all equal in rarity
						result = (ActorType)(level*7 + R.Between(-4,2));
					}
					else{
						int roll = R.Roll(100);
						if(roll <= 3){ //3% rare
							result = (ActorType)(level*7 + 2);
						}
						else{
							if(roll <= 22){ //19% uncommon (9.5% each)
								result = (ActorType)(level*7 + R.Between(0,1));
							}
							else{ //78% common (19.5% each)
								result = (ActorType)(level*7 + R.Between(-4,-1));
							}
						}
					}
				}
				if(generated_this_level[result] == 0){
					good_result = true;
				}
				else{
					if(R.OneIn(generated_this_level[result]+1)){ // 1 in 2 for the 2nd, 1 in 3 for the 3rd, and so on
						good_result = true;
					}
				}
			}
			generated_this_level[result]++;
			return result;
		}
		public ActorType ShallowMobType(){
			ActorType result = ActorType.SPECIAL;
			bool good_result = false;
			while(!good_result){
				int monster_depth = (current_level+1) / 2; //1-10, not 1-20
				List<int> levels = new List<int>();
				for(int i=1;i<monster_depth-2;++i){
					levels.Add(i);
				}
				int level = levels.Random();
				if(level == 1){ //level 1 monsters are all equal in rarity
					result = (ActorType)(level*7 + R.Between(-4,2));
				}
				else{
					int roll = R.Roll(100);
					if(roll <= 3){ //3% rare
						result = (ActorType)(level*7 + 2);
					}
					else{
						if(roll <= 22){ //19% uncommon (9.5% each)
							result = (ActorType)(level*7 + R.Between(0,1));
						}
						else{ //78% common (19.5% each)
							result = (ActorType)(level*7 + R.Between(-4,-1));
						}
					}
				}
				if(generated_this_level[result] == 0){
					good_result = true;
				}
				else{
					if(R.OneIn(generated_this_level[result]+1)){ // 1 in 2 for the 2nd, 1 in 3 for the 3rd, and so on
						good_result = true;
					}
				}
			}
			generated_this_level[result]++;
			return result;
		}
		public ActorType WanderingMobType(){
			ActorType result = ActorType.SPECIAL;
			bool good_result = false;
			while(!good_result){
				int level = 1;
				int effective_current_level = current_level + extra_danger;
				if(effective_current_level > 20){
					effective_current_level = 20;
				}
				int monster_depth = (effective_current_level+1) / 2; //1-10, not 1-20
				if(current_level != 1){ //depth 1 only generates level 1 monsters
					List<int> levels = new List<int>();
					for(int i=-2;i<=2;++i){
						if(monster_depth + i >= 1 && monster_depth + i <= 10){
							int j = 1 + Math.Abs(i);
							if(R.OneIn(j)){ //current depth is considered 1 out of 1 times, depth+1 and depth-1 one out of 2 times, etc.
								levels.Add(monster_depth + i);
							}
						}
					}
					level = levels.Random();
				}
				LevelType lt = level_types[current_level-1];
				if(R.OneIn(10) && (lt == LevelType.Cave || lt == LevelType.Crypt || lt == LevelType.Hive || lt == LevelType.Mine || lt == LevelType.Slime)){
					result = ActorType.SPECIAL; //zombies in crypts, kobolds in mines, etc.
				}
				else{
					if(level == 1){ //level 1 monsters are all equal in rarity
						result = (ActorType)(level*7 + R.Between(-4,2));
					}
					else{
						int roll = R.Roll(100);
						if(roll <= 3){ //3% rare
							result = (ActorType)(level*7 + 2);
						}
						else{
							if(roll <= 22){ //19% uncommon (9.5% each)
								result = (ActorType)(level*7 + R.Between(0,1));
							}
							else{ //78% common (19.5% each)
								result = (ActorType)(level*7 + R.Between(-4,-1));
							}
						}
					}
				}
				if(!Actor.Prototype(result).HasAttr(AttrType.IMMOBILE) && result != ActorType.MIMIC && result != ActorType.MARBLE_HORROR && result != ActorType.POLTERGEIST){
					good_result = true;
				}
			}
			return result;
		}
		private void UpdateDensity(PhysicalObject obj){ UpdateDensity(obj.p); }
		private void UpdateDensity(pos position){
			foreach(pos p in position.PositionsWithinDistance(8)){
				int dist = p.DistanceFrom(position);
				if(dist <= 1){
					monster_density[p] += 3;
				}
				else{
					if(dist <= 4){
						monster_density[p] += 2;
					}
					else{
						monster_density[p]++;
					}
				}
			}
		}
		public Actor SpawnMob(){ return SpawnMob(MobType()); }
		public Actor SpawnMob(ActorType type){
			Actor result = null;
			if(type == ActorType.POLTERGEIST){
				for(int tries=0;tries<1000;++tries){
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
					List<Tile> tiles = new List<Tile>();
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(3)){
						if(t.passable || t.type == TileType.DOOR_C){
							tiles.Add(t);
						}
					}
					if(tiles.Count >= 15){
						Actor.tiebreakers.Add(null); //a placeholder for the poltergeist once it manifests
						Event e = new Event(null,tiles,(R.Roll(8)+6)*100,EventType.POLTERGEIST);
						e.tiebreaker = Actor.tiebreakers.Count - 1;
						Q.Add(e);
						//return type;
						return null;
					}
				}
				return null;
			}
			if(type == ActorType.MIMIC){
				while(true){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					Tile t = tile[rr,rc];
					if(t.passable && t.inv == null && t.type != TileType.CHEST && t.type != TileType.FIREPIT
					&& t.type != TileType.STAIRS && !t.IsShrine()){
						Item item = Item.Create(Item.RandomItem(),rr,rc);
						Actor.tiebreakers.Add(null); //placeholder
						Event e = new Event(item,new List<Tile>{t},100,EventType.MIMIC,AttrType.NO_ATTR,0,"");
						e.tiebreaker = Actor.tiebreakers.Count - 1;
						Q.Add(e);
						return null;
					}
				}
			}
			if(type == ActorType.MARBLE_HORROR){
				Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).Random();
				if(statue != null){
					Q.Add(new Event(statue,100,EventType.MARBLE_HORROR));
				}
				return null;
			}
			if(type == ActorType.NOXIOUS_WORM){
				//get a dijkstra map with nonwalls as origins. we're looking for distance 2+.
				var dijkstra = tile.GetDijkstraMap(x=>false,AllPositions().Where(y=>!tile[y].Is(TileType.WALL)));
				//for each of these, we're gonna check twice, first for horizontal matches.
				//so, now we iterate over the map, ignoring tiles too close to the edge, and checking the valid tiles.
				List<List<Tile>> valid_burrows = new List<List<Tile>>();
				List<Tile> valid = new List<Tile>();
				for(int i=3;i<ROWS-3;++i){
					for(int j=3;j<COLS-3;++j){
						if(dijkstra[i,j] >= 2 && tile[i+1,j].Is(TileType.WALL) && tile[i+2,j].Is(TileType.FLOOR) && tile[i-1,j].Is(TileType.WALL) && tile[i-2,j].Is(TileType.FLOOR)){
							valid.Add(tile[i,j]);
						}
						else{
							if(valid.Count >= 3){
								valid_burrows.Add(new List<Tile>(valid));
							}
							valid.Clear();
						}
					}
					if(valid.Count >= 3){
						valid_burrows.Add(new List<Tile>(valid));
					}
					valid.Clear();
				}
				for(int j=3;j<COLS-3;++j){
					for(int i=3;i<ROWS-3;++i){
						if(dijkstra[i,j] >= 2 && tile[i,j+1].Is(TileType.WALL) && tile[i,j+2].Is(TileType.FLOOR) && tile[i,j-1].Is(TileType.WALL) && tile[i,j-2].Is(TileType.FLOOR)){
							valid.Add(tile[i,j]);
						}
						else{
							if(valid.Count >= 3){
								valid_burrows.Add(new List<Tile>(valid));
							}
							valid.Clear();
						}
					}
					if(valid.Count >= 3){
						valid_burrows.Add(new List<Tile>(valid));
					}
					valid.Clear();
				}
				//if a valid tile has a wall above and below it, and has floors above and below those walls, it's good. increment a counter.
				//if we find 3 or more good tiles in a row, add them all to a list of lists or something.
				//...go over the whole map, then do the same for columns.
				//if there's at least one list in the list of lists, choose one at random, convert its tiles to cracked walls, and put the worm in there somewhere. done.
				if(valid_burrows.Count > 0){
					List<Tile> burrow = valid_burrows.Random();
					foreach(Tile t in burrow){
						t.Toggle(null,TileType.CRACKED_WALL);
						foreach(Tile neighbor in t.TilesAtDistance(1)){
							neighbor.solid_rock = false;
						}
					}
					Tile dest = burrow.Random();
					return Actor.Create(type,dest.row,dest.col);
				}
			}
			int number = 1;
			if(Actor.Prototype(type).HasAttr(AttrType.SMALL_GROUP)){
				number = R.Roll(2)+1;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.MEDIUM_GROUP)){
				number = R.Roll(2)+2;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.LARGE_GROUP)){
				number = R.Roll(2)+4;
			}
			if(current_level == 21 && type == ActorType.CULTIST){
				number = 0;
				for(int i=0;i<5;++i){
					if(FinalLevelSummoningCircle(i).PositionsWithinDistance(2).Any(x=>tile[x].Is(TileType.DEMONIC_IDOL))){
						number++;
					}
				}
			}
			if(type == ActorType.SPECIAL){
				number = 1 + (current_level-3)/3;
				if(current_level > 5 && R.CoinFlip()){
					--number;
				}
			}
			List<Tile> group_tiles = new List<Tile>();
			List<Actor> group = null;
			if(number > 1){
				group = new List<Actor>();
			}
			for(int i=0;i<number;++i){
				ActorType final_type = type;
				if(type == ActorType.SPECIAL){
					switch(level_types[current_level-1]){
					case LevelType.Cave:
						if(R.CoinFlip()){
							final_type = ActorType.GOBLIN;
						}
						else{
							if(R.CoinFlip()){
								final_type = ActorType.GOBLIN_ARCHER;
							}
							else{
								final_type = ActorType.GOBLIN_SHAMAN;
							}
						}
						break;
					case LevelType.Crypt:
						final_type = ActorType.ZOMBIE;
						break;
					case LevelType.Hive:
						final_type = ActorType.FORASECT;
						break;
					case LevelType.Mine:
						final_type = ActorType.KOBOLD;
						break;
					case LevelType.Slime:
						final_type = ActorType.GIANT_SLUG;
						break;
					}
				}
				if(i == 0){
					int density_target_number = 2;
					for(int j=0;j<2000;++j){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						bool good = true;
						if(tile[rr,rc].IsTrap()){
							good = false;
						}
						if(tile[rr,rc].Is(TileType.POPPY_FIELD) && !Actor.Prototype(final_type).HasAttr(AttrType.NONLIVING,AttrType.MENTAL_IMMUNITY)){
							good = false;
						}
						if(current_level == 21){
							foreach(Tile t in tile[rr,rc].TilesWithinDistance(2)){
								if(tile[rr,rc].HasLOE(t) && player.CanSee(t)){
									good = false;
									break;
								}
							}
						}
						else{
							if(current_level < 21 && monster_density[rr,rc] >= density_target_number){
								if(monster_density[rr,rc] == density_target_number){
									if(R.CoinFlip()){
										good = false;
									}
								}
								else{
									good = false;
									density_target_number = 2 + j / 100; //repeated failures will allow closer generation
								}
							}
						}
						if(good && tile[rr,rc].passable && !tile[rr,rc].Is(TileType.CHASM,TileType.FIRE_RIFT) && actor[rr,rc] == null){
							result = Actor.Create(final_type,rr,rc);
							if(number > 1){
								group_tiles.Add(tile[rr,rc]);
								group.Add(result);
								result.group = group;
							}
							break;
						}
					}
				}
				else{
					for(int j=0;j<1999;++j){
						if(group_tiles.Count == 0){ //no space left!
							if(group.Count > 0){
								if(current_level < 21){
									UpdateDensity(group[0]);
								}
								return group[0];
							}
							else{
								if(result != null && current_level < 21){
									UpdateDensity(result);
								}
								return result;
							}
						}
						Tile t = group_tiles.Random();
						List<Tile> empty_neighbors = new List<Tile>();
						foreach(Tile neighbor in t.TilesAtDistance(1)){
							if(neighbor.passable && !neighbor.Is(TileType.CHASM,TileType.FIRE_RIFT) && !neighbor.IsTrap() && neighbor.actor() == null){
								empty_neighbors.Add(neighbor);
							}
						}
						if(empty_neighbors.Count > 0){
							t = empty_neighbors.Random();
							result = Actor.Create(final_type,t.row,t.col);
							group_tiles.Add(t);
							group.Add(result);
							result.group = group;
							break;
						}
						else{
							group_tiles.Remove(t);
						}
					}
				}
			}
			//return type;
			if(number > 1){
				if(current_level < 21){
					UpdateDensity(group[0]);
				}
				return group[0];
			}
			else{
				if(result != null && current_level < 21){
					UpdateDensity(result);
				}
				return result;
			}
		}
		public Actor SpawnWanderingMob(){
			ActorType type = WanderingMobType();
			Actor result = null;
			int number = 1;
			if(Actor.Prototype(type).HasAttr(AttrType.SMALL_GROUP)){
				number = R.Roll(2)+1;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.MEDIUM_GROUP)){
				number = R.Roll(2)+2;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.LARGE_GROUP)){
				number = R.Roll(2)+4;
			}
			if(type == ActorType.SPECIAL){
				number = 1 + (current_level-3)/3;
				if(current_level > 5 && R.CoinFlip()){
					--number;
				}
			}
			List<Tile> group_tiles = new List<Tile>();
			List<Actor> group = null;
			if(number > 1){
				group = new List<Actor>();
			}
			var dijkstra = tile.GetDijkstraMap(x=>!tile[x].passable,x=>player.HasLOS(tile[x]) || player.HasLOE(tile[x]));
			for(int i=0;i<number;++i){
				ActorType final_type = type;
				if(type == ActorType.SPECIAL){
					switch(level_types[current_level-1]){
					case LevelType.Cave:
						if(R.CoinFlip()){
							final_type = ActorType.GOBLIN;
						}
						else{
							if(R.CoinFlip()){
								final_type = ActorType.GOBLIN_ARCHER;
							}
							else{
								final_type = ActorType.GOBLIN_SHAMAN;
							}
						}
						break;
					case LevelType.Crypt:
						final_type = ActorType.ZOMBIE;
						break;
					case LevelType.Hive:
						final_type = ActorType.FORASECT;
						break;
					case LevelType.Mine:
						final_type = ActorType.KOBOLD;
						break;
					case LevelType.Slime:
						final_type = ActorType.GIANT_SLUG;
						break;
					}
				}
				if(i == 0){
					for(int j=0;j<1999;++j){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						if(!tile[rr,rc].IsTrap() && tile[rr,rc].passable && actor[rr,rc] == null && dijkstra[rr,rc] >= 6){
							result = Actor.Create(final_type,rr,rc);
							if(number > 1){
								group_tiles.Add(tile[rr,rc]);
								group.Add(result);
								result.group = group;
							}
							break;
						}
					}
				}
				else{
					for(int j=0;j<1999;++j){
						if(group_tiles.Count == 0){ //no space left!
							if(group.Count > 0){
								return group[0];
							}
							else{
								return result;
							}
						}
						Tile t = group_tiles.Random();
						List<Tile> empty_neighbors = new List<Tile>();
						foreach(Tile neighbor in t.TilesAtDistance(1)){
							if(neighbor.passable && !neighbor.IsTrap() && neighbor.actor() == null && dijkstra[neighbor.row,neighbor.col] >= 3){
								empty_neighbors.Add(neighbor);
							}
						}
						if(empty_neighbors.Count > 0){
							t = empty_neighbors.Random();
							result = Actor.Create(final_type,t.row,t.col);
							group_tiles.Add(t);
							group.Add(result);
							result.group = group;
							break;
						}
						else{
							group_tiles.Remove(t);
						}
					}
				}
			}
			if(number > 1){
				return group[0];
			}
			else{
				return result;
			}
		}
		public PosArray<CellType> GenerateMap(LevelType type){
			PosArray<CellType> result = new PosArray<CellType>(ROWS,COLS);
			Dungeon d = new Dungeon(ROWS,COLS);
			switch(type){
			case LevelType.Standard:
				while(true){
					d.CreateBasicMap();
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.RemoveDeadEndCorridors();
					d.AddDoors(25);
					d.AlterRooms(5,2,2,1,0);
					d.MarkInterestingLocations();
					d.RemoveUnconnectedAreas();
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Cave:
			{
				int roll = R.Roll(2);
				if(R.OneIn(20)){
					roll = 3;
				}
				switch(roll){ //three different algorithms
				case 1:
				{
					while(true){
						d.FillWithRandomWalls(25);
						d.ApplyCellularAutomataXYRule(3);
						d.ConnectDiagonals();
						d.ImproveMapEdges(5);
						d.RemoveDeadEndCorridors();
						d.RemoveUnconnectedAreas();
						d.MarkInterestingLocationsNonRectangular();
						if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
							d.Clear();
						}
						else{
							for(int i=0;i<ROWS;++i){
								for(int j=0;j<COLS;++j){
									result[i,j] = d.map[i,j];
								}
							}
							return result;
						}
					}
				}
				case 2:
				{
					while(true){
						d.CreateTwistyCave(true,40);
						var dijk = d.map.GetManhattanDijkstraMap(x=>!d.map[x].IsWall(),x=>!d.map[x].IsWall());
						for(int i=1;i<ROWS-1;++i){
							for(int j=1;j<COLS-1;++j){
								if(dijk[i,j] == 1){
									pos p = new pos(i,j);
									List<pos> floors = null;
									foreach(int dir in U.FourDirections){
										pos n = p.PosInDir(dir);
										if(dijk[n] == 1){
											if(floors == null){
												floors = p.PositionsAtDistance(1).Where(x=>dijk[x] == 0 && p.DirectionOf(x)%2 == 0);
											}
											List<pos> floors2 = new List<pos>();
											foreach(pos n2 in n.PositionsAtDistance(1)){
												if(dijk[n2] == 0 && n.DirectionOf(n2)%2 == 0 && !floors.Contains(n2)){
													floors2.Add(n2);
												}
											}
											if(floors2.Count > 0 && R.OneIn(5)){ //IIRC this checks each pair twice, so that affects the chance here
												pos f1 = floors.Random();
												pos f2 = floors2.Random();
												int dist = d.map.PathingDistanceFrom(f1,f2,x=>!d.map[x].IsPassable() && d.map[x] != CellType.Door);
												if(dist > 22 || (dist > 8 && R.OneIn(4))){
													CellType rubble = R.OneIn(8)? CellType.Rubble : CellType.CorridorIntersection;
													d[p] = R.OneIn(3)? rubble : CellType.CorridorIntersection;
													d[n] = R.OneIn(3)? rubble : CellType.CorridorIntersection;
													List<pos> neighbors = new List<pos>();
													foreach(pos nearby in p.PositionsAtDistance(1)){
														if(nearby.BoundsCheck(d.map,false) && nearby.DistanceFrom(n) == 1){
															neighbors.Add(nearby);
														}
													}
													while(neighbors.Count > 0){
														pos neighbor = neighbors.RemoveRandom();
														if(R.OneIn(neighbors.Count + 3) && !d.SeparatesMultipleAreas(neighbor)){
															d[neighbor] = R.OneIn(2)? CellType.Rubble : CellType.CorridorIntersection;
														}
													}
												}
											}
											break;
										}
									}
								}
							}
						}
						/*List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck() && d.map[y].IsFloor()));
						while(thin_walls.Count > 0){
							pos p = thin_walls.Random();
							foreach(int dir in new int[]{8,4}){
								if(d.map[p.PosInDir(dir)] != CellType.Wall && d.map[p.PosInDir(dir.RotateDir(true,4))] != CellType.Wall){
									var dijkstra = d.map.GetDijkstraMap(x=>d[x] == CellType.Wall,new List<pos>{p.PosInDir(dir)}); //todo: this would be better as "get distance"
									if(Math.Abs(dijkstra[p.PosInDir(dir)] - dijkstra[p.PosInDir(dir.RotateDir(true,4))]) > 30){
										d.map[p] = CellType.CorridorIntersection;
										break;
									}
								}
							}
							thin_walls.Remove(p); //todo: move thin-wall-removal to schism
						}*/
						d.ConnectDiagonals();
						d.RemoveUnconnectedAreas();
						d.ImproveMapEdges(5);
						d.SmoothCorners(60);
						d.RemoveDeadEndCorridors();
						d.MarkInterestingLocationsNonRectangular();
						if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
							d.Clear();
						}
						else{
							for(int i=0;i<ROWS;++i){
								for(int j=0;j<COLS;++j){
									result[i,j] = d.map[i,j];
								}
							}
							return result;
						}
					}
				}
				case 3:
				{
					d.RoomHeightMax = 3;
					d.RoomWidthMax = 3;
					while(true){
						int successes = 0;
						int consecutive_failures = 0;
						while(successes < 13){
							if(d.CreateRoom()){
								++successes;
								consecutive_failures = 0;
							}
							else{
								if(consecutive_failures++ >= 50){
									d.Clear();
									successes = 0;
									consecutive_failures = 0;
								}
							}
						}
						d.CaveWidenRooms(100,50);
						d.AddRockFormations(40,2);
						List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck() && d.map[y].IsFloor()));
						while(!d.IsFullyConnected() && thin_walls.Count > 0){
							pos p = thin_walls.Random();
							d.map[p] = CellType.CorridorIntersection;
							foreach(pos neighbor in p.PositionsWithinDistance(1)){
								thin_walls.Remove(neighbor);
							}
						}
						d.ConnectDiagonals();
						d.RemoveDeadEndCorridors();
						d.RemoveUnconnectedAreas();
						d.MarkInterestingLocationsNonRectangular();
						if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){ //todo: add 'proper coverage' check here - make sure it stretches across enough of the map.
							d.Clear();
						}
						else{
							for(int i=0;i<ROWS;++i){
								for(int j=0;j<COLS;++j){
									result[i,j] = d.map[i,j];
								}
							}
							return result;
						}
					}
				}
				}
				break;
			}
			case LevelType.Hive:
				d.RoomHeightMax = 3;
				d.RoomWidthMax = 3;
				while(true){
					int successes = 0;
					int consecutive_failures = 0;
					while(successes < 35){
						if(d.CreateRoom()){
							++successes;
							consecutive_failures = 0;
						}
						else{
							if(consecutive_failures++ >= 40){
								d.Clear();
								successes = 0;
								consecutive_failures = 0;
							}
						}
					}
					d.CaveWidenRooms(100,10);
					d.CaveWidenRooms(3,20);
					List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck() && d.map[y].IsFloor()));
					while(!d.IsFullyConnected() && thin_walls.Count > 0){
						pos p = thin_walls.Random();
						d.map[p] = CellType.CorridorIntersection;
						foreach(pos neighbor in p.PositionsWithinDistance(2)){
							thin_walls.Remove(neighbor);
						}
					}
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors();
					d.RemoveUnconnectedAreas();
					d.MarkInterestingLocations();
					//to find rooms big enough for stuff in the center:
					//var dijkstra = d.map.GetDijkstraMap(x=>d.map[x].IsWall(),d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasAdjacentWhere(y=>!d.map[y].IsWall())));
					if(d.NumberOfFloors() < 340 || d.HasLargeUnusedSpaces(300)){ //todo: add 'proper coverage' check here - make sure it stretches across enough of the map.
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Mine:
			{
				d.CorridorExtraLengthChance = 0;
				d.CorridorChainSizeMax = 10;
				while(true){
					d.RoomHeightMin = 8;
					d.RoomWidthMin = 8;
					d.RoomHeightMax = 8;
					d.RoomWidthMax = 10;
					d.MinimumSpaceBetweenCorridors = 3;
					d.CorridorLengthMin = 3;
					d.CorridorLengthMax = 5;
					while(!d.CreateRoom()){}
					d.RoomHeightMin = 5;
					d.RoomWidthMin = 5;
					d.RoomHeightMax = 5;
					d.RoomWidthMax = 5;
					while(!d.CreateRoom()){}
					while(!d.CreateRoom()){}
					/*for(int i=0;i<10;++i){
						d.CreateRoom();
					}
					d.AddRockFormations(100,2);*/
					d.MinimumSpaceBetweenCorridors = 5;
					d.CorridorLengthMin = 4;
					d.CorridorLengthMax = 12;
					for(int i=0;i<70;++i){
						d.CreateCorridor();
					}
					d.CorridorLengthMin = 3;
					d.CorridorLengthMax = 5;
					d.MinimumSpaceBetweenCorridors = 3;
					for(int i=0;i<350;++i){
						d.CreateCorridor();
					}
					d.RemoveUnconnectedAreas();
					d.ConnectDiagonals(true);
					d.RemoveUnconnectedAreas();
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 250 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			}
			case LevelType.Fortress:
				while(true){
					int H = ROWS;
					int W = COLS;
					for(int i=H/2-1;i<H/2+1;++i){
						for(int j=1;j<W-1;++j){
							if(j==1 || j==W-2){
								d.map[i,j] = CellType.RoomCorner;
							}
							else{
								d.map[i,j] = CellType.RoomEdge;
							}
						}
					}
					for(int i=0;i<700;++i){
						if(R.OneIn(5)){
							d.CreateCorridor();
						}
						else{
							d.CreateRoom();
						}
					}
					bool reflect_features = R.PercentChance(80);
					if(reflect_features){
						d.AddDoors(25);
						d.AddPillars(30);
					}
					d.Reflect(true,false);
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors();
					d.RemoveUnconnectedAreas();
					if(!reflect_features){
						d.AddDoors(25);
						d.AddPillars(30);
					}
					bool door_right = false;
					bool door_left = false;
					int rightmost_door = 0;
					int leftmost_door = 999;
					for(int j=0;j<22;++j){
						if(d[H/2-2,j].IsCorridorType()){
							door_left = true;
							if(leftmost_door == 999){
								leftmost_door = j;
							}
						}
						if(d[H/2-2,W-1-j].IsCorridorType()){
							door_right = true;
							if(rightmost_door == 0){
								rightmost_door = W-1-j;
							}
						}
					}
					if(!door_left || !door_right){
						d.Clear();
						continue;
					}
					for(int j=1;j<leftmost_door-6;++j){
						d[H/2-1,j] = CellType.Wall;
						d[H/2,j] = CellType.Wall;
					}
					for(int j=W-2;j>rightmost_door+6;--j){
						d[H/2-1,j] = CellType.Wall;
						d[H/2,j] = CellType.Wall;
					}
					for(int j=1;j<W-1;++j){
						if(d[H/2-1,j].IsFloor()){
							d[H/2-1,j] = CellType.Statue;
							d[H/2,j] = CellType.Statue;
							break;
						}
						else{
							if(d[H/2-1,j] == CellType.Statue){
								break;
							}
						}
					}
					for(int j=W-2;j>0;--j){
						if(d[H/2-1,j].IsFloor()){
							d[H/2-1,j] = CellType.Statue;
							d[H/2,j] = CellType.Statue;
							break;
						}
						else{
							if(d[H/2-1,j] == CellType.Statue){
								break;
							}
						}
					}
					for(int i=H/2-1;i<H/2+1;++i){
						for(int j=1;j<W-1;++j){
							if(d[i,j] == CellType.RoomCorner || d[i,j] == CellType.RoomEdge){
								d[i,j] = CellType.CorridorIntersection;
							}
						}
					}
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 420 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Slime:
				while(true){
					/*for(int i=1;i<ROWS-1;++i){
						for(int j=1;j<COLS-1;++j){
							if(d[i,j].IsWall()){
								if(!d[i+1,j+1].IsWall()){
									d[i,j] = d[i+1,j+1];
								}
								else{
									if(!d[i+1,j].IsWall()){
										d[i,j] = d[i+1,j];
									}
									else{
										if(!d[i,j+1].IsWall()){
											d[i,j] = d[i,j+1];
										}
									}
								}
							}
						}
					}*/
					d.CreateBasicMap();
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.AddDoors(25);
					d.CaveWidenRooms(30,30);
					d.RemoveDeadEndCorridors();
					d.AddPillars(30);
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 120 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Garden:
				d.RoomHeightMin = 4;
				d.RoomHeightMax = 10;
				d.RoomWidthMin = 4;
				d.RoomWidthMax = 10;
				while(true){
					d.CreateBasicMap();
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.RemoveDeadEndCorridors();
					var dijkstra = d.map.GetDijkstraMap(x=>false,x=>d[x].IsPassable());
					List<pos> possible_room_centers = d.map.PositionsWhere(x=>dijkstra[x] == 3 && x.row > 1 && x.row < ROWS-2 && x.col > 1 && x.col < COLS-2);
					int rooms = 0;
					while(rooms < 6 && possible_room_centers.Count > 0){
						pos p = possible_room_centers.RemoveRandom();
						List<int> valid_dirs = new List<int>();
						foreach(int dir in U.FourDirections){
							pos p2 = p.PosInDir(dir).PosInDir(dir).PosInDir(dir);
							if(p2.BoundsCheck(d.map) && d[p2].IsPassable() && d[p2] != CellType.RoomCorner){
								valid_dirs.Add(dir);
							}
						}
						if(valid_dirs.Count > 0){
							foreach(pos neighbor in p.PositionsWithinDistance(1)){
								d[neighbor] = CellType.RoomInterior;
							}
							possible_room_centers.RemoveWhere(x=>p.DistanceFrom(x) <= 3);
							foreach(int dir in valid_dirs){
								d[p.PosInDir(dir).PosInDir(dir)] = CellType.CorridorIntersection;
							}
							++rooms;
						}
					}
					CellType water_type = CellType.ShallowWater;
					if(R.OneIn(8)){
						water_type = CellType.Ice;
					}
					d.ForEachRectangularRoom((start_r,start_c,end_r,end_c)=>{
						int room_height = (end_r - start_r) + 1;
						int room_width = (end_c - start_c) + 1;
						if(room_height <= 4 && room_width <= 4){
							if(room_height == 3 && room_width == 3){
								return true;
							}
							List<pos> water = new List<pos>();
							if(!new pos(start_r+1,start_c).PositionsAtDistance(1).Any(x=>d[x].IsCorridorType())){
								water.Add(new pos(start_r+1,start_c));
								water.Add(new pos(start_r+2,start_c));
							}
							if(!new pos(start_r,start_c+1).PositionsAtDistance(1).Any(x=>d[x].IsCorridorType())){
								water.Add(new pos(start_r,start_c+1));
								water.Add(new pos(start_r,start_c+2));
							}
							if(!new pos(end_r-1,end_c).PositionsAtDistance(1).Any(x=>d[x].IsCorridorType())){
								water.Add(new pos(end_r-1,end_c));
								water.Add(new pos(end_r-2,end_c));
							}
							if(!new pos(end_r,end_c-1).PositionsAtDistance(1).Any(x=>d[x].IsCorridorType())){
								water.Add(new pos(end_r,end_c-1));
								water.Add(new pos(end_r,end_c-2));
							}
							foreach(pos p in water){
								d[p] = water_type;
							}
							d[start_r,start_c] = CellType.Statue;
							d[start_r,end_c] = CellType.Statue;
							d[end_r,start_c] = CellType.Statue;
							d[end_r,end_c] = CellType.Statue;
						}
						else{
							CellType center_type = CellType.RoomFeature1;
							switch(R.Roll(3)){
							case 1:
								center_type = water_type;
								break;
							case 2:
								center_type = CellType.Poppies;
								break;
							case 3:
								center_type = CellType.Brush;
								break;
							}
							bool statues = R.CoinFlip();
							CellType statue_type = CellType.Statue;
							if(room_height <= 8 && room_width <= 8 && R.OneIn(8)){
								statue_type = CellType.Torch;
							}
							CellType edge_type = CellType.ShallowWater;
							if(center_type != water_type && !R.OneIn(4)){
								edge_type = CellType.ShallowWater;
							}
							else{
								int vine_chance = 50;
								if(!statues){
									vine_chance = 80;
								}
								if(R.PercentChance(vine_chance)){
									edge_type = CellType.Vine;
								}
								else{
									edge_type = CellType.Gravel;
								}
								if(R.OneIn(32)){
									if(R.CoinFlip()){
										edge_type = CellType.Statue;
									}
									else{
										edge_type = CellType.GlowingFungus;
									}
								}
							}
							bool gravel = R.OneIn(16);
							bool edges = R.CoinFlip();
							if(room_height < 6 || room_width < 6){
								edges = false;
							}
							if(room_height >= 8 && room_width >= 8){
								edges = !R.OneIn(4);
							}
							if(edges){
								for(int i=start_r;i<=end_r;++i){
									for(int j=start_c;j<=end_c;++j){
										if(i == start_r || i == end_r || j == start_c || j == end_c){ //edges
											if(statues && (i == start_r || i == end_r) && (j == start_c || j == end_c)){ //corners
												d[i,j] = statue_type;
											}
											else{
												pos p = new pos(i,j);
												if(!p.CardinalAdjacentPositions().Any(x=>d[x].IsCorridorType())){
													d[i,j] = edge_type;
												}
											}
										}
										else{
											if(i == start_r+1 || i == end_r-1 || j == start_c+1 || j == end_c-1){ //the path
												if(gravel){
													d[i,j] = CellType.Gravel;
												}
											}
											else{
												d[i,j] = center_type;
											}
										}
									}
								}
							}
							else{
								for(int i=start_r;i<=end_r;++i){
									for(int j=start_c;j<=end_c;++j){
										if(i == start_r || i == end_r || j == start_c || j == end_c){
											if(gravel){
												d[i,j] = CellType.Gravel;
											}
										}
										else{
											d[i,j] = center_type;
										}
									}
								}
							}
							if(center_type == water_type && room_height % 2 == 1 && room_width % 2 == 1){
								statue_type = CellType.Statue;
								if(room_height <= 7 && room_width <= 7 && R.OneIn(12)){
									statue_type = CellType.Torch;
								}
								d[(start_r+end_r)/2,(start_c+end_c)/2] = statue_type;
							}
						}
						return true;
					});
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.AddDoors(10);
					d.RemoveDeadEndCorridors();
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Crypt:
			{
				while(true){
					pos room1origin = new pos(ROWS/2,R.Roll(COLS/8 - 1) + COLS/8 - 1);
					pos room2origin = new pos(ROWS/2,R.Roll(COLS/8 - 1) + (COLS*6 / 8) - 1);
					while(!d.CreateRoom(room1origin.row,room1origin.col)){} //left half
					while(!d.CreateRoom(room2origin.row,room2origin.col)){} //right half
					d.CaveWidenRooms(100,150);
					d.MoveRoom(room1origin,4);
					d.MoveRoom(room2origin,6);
					var dijkstra = d.map.GetDijkstraMap(x=>d.map[x] == CellType.Wall,x=>d.map[x] == CellType.Wall);
					int distance_from_walls = 3;
					List<pos> central_room = d.map.PositionsWhere(x=>dijkstra[x] > distance_from_walls);
					int required_consecutive = 3;
					for(int i=0;i<ROWS;++i){ //first, check each row...
						for(int j=0;j<COLS;++j){
							List<pos> this_row = new List<pos>();
							while(j < COLS && dijkstra[i,j] > distance_from_walls){
								this_row.Add(new pos(i,j));
								++j;
							}
							if(this_row.Count < required_consecutive){
								foreach(pos p in this_row){
									central_room.Remove(p);
								}
							}
						}
					}
					for(int j=0;j<COLS;++j){ //...then each column
						for(int i=0;i<ROWS;++i){
							List<pos> this_col = new List<pos>();
							while(i < ROWS && dijkstra[i,j] > distance_from_walls){
								this_col.Add(new pos(i,j));
								++i;
							}
							if(this_col.Count < required_consecutive){
								foreach(pos p in this_col){
									central_room.Remove(p);
								}
							}
						}
					}
					central_room = d.map.GetFloodFillPositions(central_room.Where(x=>x.PositionsWithinDistance(1).All(y=>central_room.Contains(y))),false,x=>central_room.Contains(x));
					List<pos> walls = new List<pos>();
					foreach(pos p in central_room){
						d.map[p] = CellType.InterestingLocation;
						foreach(pos neighbor in p.PositionsAtDistance(1)){
							if(!central_room.Contains(neighbor)){
								d.map[neighbor] = CellType.Wall;
								walls.Add(neighbor);
							}
						}
					}
					while(true){
						List<pos> potential_doors = new List<pos>();
						foreach(pos p in walls){
							foreach(int dir in U.FourDirections){
								if(d.map[p.PosInDir(dir)] == CellType.InterestingLocation && d.map[p.PosInDir(dir.RotateDir(true,4))].IsRoomType() && d.map[p.PosInDir(dir.RotateDir(true,4))] != CellType.InterestingLocation){
									potential_doors.Add(p);
									break;
								}
							}
						}
						if(potential_doors.Count > 0){
							pos p = potential_doors.Random();
							d.map[p] = CellType.Door;
							List<pos> room = d.map.GetFloodFillPositions(p,true,x=>d.map[x] == CellType.InterestingLocation);
							foreach(pos p2 in room){
								d.map[p2] = CellType.RoomInterior;
							}
						}
						else{
							break;
						}
					}
					dijkstra = d.map.GetDijkstraMap(x=>d.map[x] == CellType.Wall,x=>d.map[x] == CellType.Wall);
					int num_chests = 0;
					d.ForEachRoom(list=>{
						if(central_room.Contains(list[0])){
							if(num_chests++ < 2){
								d[list.Random()] = CellType.Chest;
							}
							return true;
						}
						List<pos> room = list.Where(x=>dijkstra[x] > 1);
						int start_r = room.WhereLeast(x=>x.row)[0].row;
						int end_r = room.WhereGreatest(x=>x.row)[0].row;
						int start_c = room.WhereLeast(x=>x.col)[0].col;
						int end_c = room.WhereGreatest(x=>x.col)[0].col;
						List<List<pos>> offsets = new List<List<pos>>();
						for(int i=0;i<4;++i){
							offsets.Add(new List<pos>());
						}
						for(int i=start_r;i<=end_r;i+=2){
							for(int j=start_c;j<=end_c;j+=2){
								if(room.Contains(new pos(i,j))){
									offsets[0].Add(new pos(i,j));
								}
								if(i+1 <= end_r && room.Contains(new pos(i+1,j))){
									offsets[1].Add(new pos(i+1,j));
								}
								if(j+1 <= end_c && room.Contains(new pos(i,j+1))){
									offsets[2].Add(new pos(i,j+1));
								}
								if(i+1 <= end_r && j+1 <= end_c && room.Contains(new pos(i+1,j+1))){
									offsets[3].Add(new pos(i+1,j+1));
								}
							}
						}
						List<pos> tombstones = offsets.WhereGreatest(x=>x.Count).Random();
						foreach(pos p in tombstones){
							d.map[p] = CellType.Tombstone;
						}
						return true;
					});
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(d[i,j] == CellType.Door){
								pos p = new pos(i,j);
								List<pos> potential_statues = p.PositionsAtDistance(1).Where(x=>!d[x].IsWall() && !central_room.Contains(x) && p.DirectionOf(x) % 2 != 0 && !x.PositionsAtDistance(1).Any(y=>d[y].Is(CellType.Tombstone)));
								if(potential_statues.Count == 2){
									d[potential_statues[0]] = CellType.Statue;
									d[potential_statues[1]] = CellType.Statue;
								}
							}
						}
					}
					List<pos> room_one = null;
					List<pos> room_two = null;
					for(int j=0;j<COLS && room_one == null;++j){
						for(int i=0;i<ROWS;++i){
							if(d[i,j] != CellType.Wall){
								room_one = d.map.GetFloodFillPositions(new pos(i,j),false,x=>!d[x].IsWall());
								break;
							}
						}
					}
					for(int j=COLS-1;j>=0 && room_two == null;--j){
						for(int i=0;i<ROWS;++i){
							if(d[i,j] != CellType.Wall){
								room_two = d.map.GetFloodFillPositions(new pos(i,j),false,x=>!d[x].IsWall());
								break;
							}
						}
					}
					if(room_one.WhereGreatest(x=>x.col).Random().DistanceFrom(room_two.WhereLeast(x=>x.col).Random()) < 12){
						d.Clear();
						continue;
					}
					Dungeon d2 = new Dungeon(ROWS,COLS);
					int tries = 0;
					while(tries < 10){
						d2.CreateBasicMap();
						d2.ConnectDiagonals();
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(d[i,j] != CellType.Wall){
									pos p = new pos(i,j);
									foreach(pos neighbor in p.PositionsAtDistance(1)){
										d2[neighbor] = CellType.Wall;
									}
								}
							}
						}
						d2.RemoveUnconnectedAreas();
						List<pos> room_one_walls = new List<pos>();
						List<pos> room_two_walls = new List<pos>();
						for(int i=0;i<ROWS;++i){
							for(int j=COLS-1;j>=0;--j){
								pos p = new pos(i,j);
								if(room_one.Contains(p)){
									room_one_walls.Add(p);
									break;
								}
							}
							for(int j=0;j<COLS;++j){
								pos p = new pos(i,j);
								if(room_two.Contains(p)){
									room_two_walls.Add(p);
									break;
								}
							}
						}
						List<pos> room_one_valid_connections = new List<pos>();
						List<pos> room_two_valid_connections = new List<pos>();
						foreach(pos p in room_one_walls){
							pos next = p.PosInDir(6);
							while(BoundsCheck(next) && p.DistanceFrom(next) < 7){
								if(d2[next] != CellType.Wall){
									room_one_valid_connections.Add(p.PosInDir(6));
									break;
								}
								next = next.PosInDir(6);
							}
						}
						foreach(pos p in room_two_walls){
							pos next = p.PosInDir(4);
							while(BoundsCheck(next) && p.DistanceFrom(next) < 7){
								if(d2[next] != CellType.Wall){
									room_two_valid_connections.Add(p.PosInDir(4));
									break;
								}
								next = next.PosInDir(4);
							}
						}
						if(room_one_valid_connections.Count > 0 && room_two_valid_connections.Count > 0){
							pos one = room_one_valid_connections.Random();
							while(true){
								if(d2[one] == CellType.Wall){
									d2[one] = CellType.CorridorHorizontal;
								}
								else{
									break;
								}
								one = one.PosInDir(6);
							}
							pos two = room_two_valid_connections.Random();
							while(true){
								if(d2[two] == CellType.Wall){
									d2[two] = CellType.CorridorHorizontal;
								}
								else{
									break;
								}
								two = two.PosInDir(4);
							}
							break;
						}
						else{
							d2.Clear();
						}
						++tries;
					}
					if(tries == 10){
						d.Clear();
						continue;
					}
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(d2[i,j] != CellType.Wall){
								d[i,j] = d2[i,j];
							}
						}
					}
					//d.CaveWidenRooms(100,20);
					//d.MakeCavesMoreRectangular(4);
					//d.RemoveDeadEndCorridors();
					//d.MakeCavesMoreRectangular(1 + num++ / 10);
					//d.Clear();
					//continue;
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.RemoveDeadEndCorridors();
					d.MarkInterestingLocations();
					d.RemoveUnconnectedAreas();
					if(d.NumberOfFloors() < 340 || d.HasLargeUnusedSpaces(350)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
				}
			}
			return null;
		}
		public void GenerateLevel(){
			if(current_level < 20){
				++current_level;
			}
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j] != null){
						if(actor[i,j] != player){
							actor[i,j].inv.Clear();
							actor[i,j].target = null;
							//Q.KillEvents(actor[i,j],EventType.ANY_EVENT);
							if(actor[i,j].group != null){
								actor[i,j].group.Clear();
								actor[i,j].group = null;
							}
						}
						actor[i,j] = null;
					}
					if(tile[i,j] != null){
						tile[i,j].inv = null;
					}
					tile[i,j] = null;
				}
			}
			wiz_lite = false;
			wiz_dark = false;
			feat_gained_this_level = false;
			generated_this_level = new Dict<ActorType, int>();
			monster_density = new PosArray<int>(ROWS,COLS);
			shrine_locations = new pos[5];
			for(int i=0;i<5;++i){
				shrine_locations[i] = new pos(-1,-1);
			}
			Q.ResetForNewLevel();
			last_seen = new colorchar[ROWS,COLS];
			Fire.fire_event = null;
			Fire.burning_objects.Clear();
			if(player.IsBurning()){
				Fire.AddBurningObject(player);
			}
			Actor.tiebreakers = new List<Actor>{player};
			Actor.interrupted_path = new pos(-1,-1);
			PosArray<CellType> map = GenerateMap(level_types[current_level-1]);
			List<pos> interesting_tiles = new List<pos>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(map[i,j] == CellType.InterestingLocation){
						interesting_tiles.Add(new pos(i,j));
					}
				}
			}
			int attempts = 0;
			if(current_level%2 == 1){
				List<CellType> shrines = new List<CellType>{CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5};
				while(shrines.Count > 0){
					attempts = 0;
					for(bool done=false;!done;++attempts){
						int rr = R.Roll(ROWS-4) + 1;
						int rc = R.Roll(COLS-4) + 1;
						//if(interesting_tiles.Count > 0 && attempts > 1000){
						if(interesting_tiles.Count > 0){
							pos p = interesting_tiles.RemoveRandom();
							rr = p.row;
							rc = p.col;
							map[p] = CellType.RoomInterior;
						}
						pos temp = new pos(rr,rc);
						if(shrines.Count > 1){
							if(map[rr,rc].IsFloor()){
								if(attempts > 1000){
									bool good = true;
									foreach(pos p in temp.PositionsWithinDistance(4)){
										CellType ch = map[p.row,p.col];
										if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
											good = false;
										}
									}
									if(good){
										List<pos> dist2 = new List<pos>();
										foreach(pos p2 in temp.PositionsAtDistance(2)){
											if(map[p2.row,p2.col].IsFloor()){
												dist2.Add(p2);
											}
										}
										if(dist2.Count > 0){
											map[rr,rc] = shrines.RemoveRandom();
											pos p2 = dist2.Random();
											map[p2.row,p2.col] = shrines.RemoveRandom();
											done = true;
											break;
										}
									}
									else{
										interesting_tiles.Remove(temp);
									}
								}
								bool floors = true;
								foreach(pos p in temp.PositionsAtDistance(1)){
									if(!map[p.row,p.col].IsFloor()){
										floors = false;
									}
								}
								foreach(pos p in temp.PositionsWithinDistance(3)){
									CellType ch = map[p.row,p.col];
									if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
										floors = false;
									}
								}
								if(floors){
									if(R.CoinFlip()){
										map[rr-1,rc] = shrines.RemoveRandom();
										map[rr+1,rc] = shrines.RemoveRandom();
									}
									else{
										map[rr,rc-1] = shrines.RemoveRandom();
										map[rr,rc+1] = shrines.RemoveRandom();
									}
									CellType center = CellType.Wall;
									while(center == CellType.Wall){
										switch(R.Roll(5)){
										case 1:
											if(level_types[current_level-1] != LevelType.Hive && level_types[current_level-1] != LevelType.Garden){
												center = CellType.Pillar;
											}
											break;
										case 2:
											center = CellType.Statue;
											break;
										case 3:
											if(level_types[current_level-1] != LevelType.Garden){
												center = CellType.FirePit;
											}
											break;
										case 4:
											center = CellType.ShallowWater;
											break;
										case 5:
											if(level_types[current_level-1] != LevelType.Hive){
												center = CellType.Torch;
											}
											break;
										}
									}
									map[rr,rc] = center;
									interesting_tiles.Remove(temp);
									done = true;
									break;
								}
							}
						}
						else{
							if(map[rr,rc].IsFloor()){
								bool good = true;
								foreach(pos p in temp.PositionsWithinDistance(2)){
									CellType ch = map[p.row,p.col];
									if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
										good = false;
									}
								}
								if(good){
									if(attempts > 1000){
										map[rr,rc] = shrines.RemoveRandom();
										interesting_tiles.Remove(temp);
										done = true;
										break;
									}
									else{
										bool floors = true;
										foreach(pos p in temp.PositionsAtDistance(1)){
											if(!map[p.row,p.col].IsFloor()){
												floors = false;
											}
										}
										if(floors){
											map[rr,rc] = shrines.RemoveRandom();
											interesting_tiles.Remove(temp);
											done = true;
											break;
										}
									}
								}
							}
							if(map[rr,rc].IsWall()){
								if(!map[rr+1,rc].IsFloor() && !map[rr-1,rc].IsFloor() && !map[rr,rc-1].IsFloor() && !map[rr,rc+1].IsFloor()){
									continue; //no floors? retry.
								}
								bool no_good = false;
								foreach(pos p in temp.PositionsWithinDistance(2)){
									CellType ch = map[p.row,p.col];
									if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
										no_good = true;
									}
								}
								if(no_good){
									continue;
								}
								int walls = 0;
								foreach(pos p in temp.PositionsAtDistance(1)){
									if(map[p.row,p.col].IsWall()){
										++walls;
									}
								}
								if(walls >= 5){
									int successive_walls = 0;
									CellType[] rotated = new CellType[8];
									for(int i=0;i<8;++i){
										pos temp2;
										temp2 = temp.PosInDir(8.RotateDir(true,i));
										rotated[i] = map[temp2.row,temp2.col];
									}
									for(int i=0;i<15;++i){
										if(rotated[i%8].IsWall()){
											++successive_walls;
										}
										else{
											successive_walls = 0;
										}
										if(successive_walls == 5){
											done = true;
											map[rr,rc] = shrines.RemoveRandom();
											interesting_tiles.Remove(temp);
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			int num_chests = R.Between(0,1);
			if(level_types[current_level-1] == LevelType.Crypt){
				num_chests -= map.PositionsWhere(x=>map[x] == CellType.Chest).Count;
			}
			for(int i=0;i<num_chests;++i){
				int tries = 0;
				for(bool done=false;!done;++tries){
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
					if(interesting_tiles.Count > 0){
						pos p = interesting_tiles.RemoveRandom();
						rr = p.row;
						rc = p.col;
						map[rr,rc] = CellType.RoomInterior;
					}
					if(map[rr,rc].IsFloor()){
						bool floors = true;
						pos temp = new pos(rr,rc);
						foreach(pos p in temp.PositionsAtDistance(1)){
							if(!map[p.row,p.col].IsFloor()){
								floors = false;
							}
						}
						if(floors || tries > 1000){ //after 1000 tries, place it anywhere
							map[rr,rc] = CellType.Chest;
							done = true;
						}
					}
				}
			}
			attempts = 0;
			for(bool done=false;!done;++attempts){
				int rr = R.Roll(ROWS-4) + 1;
				int rc = R.Roll(COLS-4) + 1;
				if(interesting_tiles.Count > 0){
					pos p = interesting_tiles.RemoveRandom();
					rr = p.row;
					rc = p.col;
					map[p] = CellType.RoomInterior;
				}
				if(map[rr,rc].IsFloor()){
					bool floors = true;
					pos temp = new pos(rr,rc);
					foreach(pos p in temp.PositionsAtDistance(1)){
						if(!map[p.row,p.col].IsFloor()){
							floors = false;
						}
					}
					if(floors || attempts > 1000){
						map[rr,rc] = CellType.Stairs;
						done = true;
					}
				}
			}
			if(level_types[current_level-1] != LevelType.Garden){
				GenerateFloorTypes(map);
				GenerateFeatures(map,interesting_tiles);
			}
			int num_traps = R.Roll(2,3);
			for(int i=0;i<num_traps;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					if(map[rr,rc].IsFloor() && map[rr,rc] != CellType.ShallowWater){
						map[rr,rc] = CellType.Trap;
						done = true;
					}
				}
			}
			List<Tile> hidden = new List<Tile>();
			Event grave_dirt_event = null;
			Event poppy_event = null;
			Tile stairs = null;
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					//Screen.WriteMapChar(i,j,map[i,j]);
					switch(map[i,j]){
					case CellType.Wall:
					case CellType.Pillar:
						Tile.Create(TileType.WALL,i,j);
						break;
					case CellType.Door:
						if(R.OneIn(120)){
							if(R.CoinFlip()){
								Tile.Create(TileType.STONE_SLAB,i,j);
								Q.Add(new Event(tile[i,j],new List<Tile>{tile[i,j]},100,EventType.STONE_SLAB));
							}
							else{
								Tile.Create(TileType.HIDDEN_DOOR,i,j);
								hidden.Add(tile[i,j]);
							}
						}
						else{
							Tile.Create(TileType.DOOR_C,i,j);
						}
						break;
					case CellType.Stairs:
						if(current_level < 20){
							Tile.Create(TileType.STAIRS,i,j);
							stairs = tile[i,j];
						}
						else{
							if(current_level == 20){
								Tile.Create(TileType.STAIRS,i,j);
								tile[i,j].color = Color.Red;
								tile[i,j].SetName("scorched stairway");
								stairs = tile[i,j];
							}
							else{
								Tile.Create(TileType.FLOOR,i,j);
							}
						}
						break;
					case CellType.Statue:
						Tile.Create(TileType.STATUE,i,j);
						break;
					case CellType.Rubble:
						Tile.Create(TileType.RUBBLE,i,j);
						break;
					case CellType.FirePit:
						Tile.Create(TileType.FIREPIT,i,j);
						break;
					case CellType.Pool:
						Tile.Create(TileType.POOL_OF_RESTORATION,i,j);
						break;
					case CellType.BlastFungus:
						Tile.Create(TileType.BLAST_FUNGUS,i,j);
						break;
					case CellType.CrackedWall:
						Tile.Create(TileType.CRACKED_WALL,i,j);
						break;
					case CellType.Chest:
						Tile.Create(TileType.CHEST,i,j);
						break;
					case CellType.Trap:
					{
						TileType type = Tile.RandomTrap();
						Tile.Create(type,i,j);
						if(tile[i,j].IsTrap()){
							tile[i,j].name = "floor";
							tile[i,j].the_name = "the floor";
							tile[i,j].a_name = "a floor";
							tile[i,j].symbol = '.';
							tile[i,j].color = Color.White;
							hidden.Add(tile[i,j]);
						}
						break;
					}
					case CellType.Geyser:
						Tile.Create(TileType.FIRE_GEYSER,i,j);
						int frequency = R.Roll(31) + 8; //9-39
						int variance = R.Roll(10) - 1; //0-9
						int variance_amount = (frequency * variance) / 10;
						int number_of_values = variance_amount*2 + 1;
						int minimum_value = frequency - variance_amount;
						if(minimum_value < 5){
							int diff = 5 - minimum_value;
							number_of_values -= diff;
							minimum_value = 5;
						}
						int delay = ((minimum_value - 1) + R.Roll(number_of_values)) * 100;
						Q.Add(new Event(tile[i,j],delay + 200,EventType.FIRE_GEYSER,(frequency*10)+variance)); //notice the hacky way the value is stored
						Q.Add(new Event(tile[i,j],delay,EventType.FIRE_GEYSER_ERUPTION,2));
						break;
					case CellType.FogVent:
						Tile.Create(TileType.FOG_VENT,i,j);
						Q.Add(new Event(tile[i,j],100,EventType.FOG_VENT));
						break;
					case CellType.PoisonVent:
						Tile.Create(TileType.POISON_GAS_VENT,i,j);
						Q.Add(new Event(tile[i,j],100,EventType.POISON_GAS_VENT));
						break;
					case CellType.Tombstone:
					{
						Tile t = Tile.Create(TileType.TOMBSTONE,i,j);
						if(R.OneIn(8)){
							Q.Add(new Event(null,new List<Tile>{t},100,EventType.TOMBSTONE_GHOST));
						}
						break;
					}
					case CellType.HiddenDoor:
						Tile.Create(TileType.HIDDEN_DOOR,i,j);
						hidden.Add(tile[i,j]);
						break;
					case CellType.SpecialFeature1:
						Tile.Create(TileType.COMBAT_SHRINE,i,j);
						shrine_locations[0] = new pos(i,j);
						break;
					case CellType.SpecialFeature2:
						Tile.Create(TileType.DEFENSE_SHRINE,i,j);
						shrine_locations[1] = new pos(i,j);
						break;
					case CellType.SpecialFeature3:
						Tile.Create(TileType.MAGIC_SHRINE,i,j);
						shrine_locations[2] = new pos(i,j);
						break;
					case CellType.SpecialFeature4:
						Tile.Create(TileType.SPIRIT_SHRINE,i,j);
						shrine_locations[3] = new pos(i,j);
						break;
					case CellType.SpecialFeature5:
						Tile.Create(TileType.STEALTH_SHRINE,i,j);
						shrine_locations[4] = new pos(i,j);
						break;
					case CellType.DeepWater:
					case CellType.ShallowWater:
						Tile.Create(TileType.WATER,i,j);
						break;
					case CellType.Barrel:
						Tile.Create(TileType.BARREL,i,j);
						break;
					case CellType.Brush:
						Tile.Create(TileType.BRUSH,i,j);
						break;
					case CellType.GlowingFungus:
						Tile.Create(TileType.GLOWING_FUNGUS,i,j);
						break;
					case CellType.GraveDirt:
						Tile.Create(TileType.GRAVE_DIRT,i,j);
						if(grave_dirt_event == null){
							grave_dirt_event = new Event(new List<Tile>{tile[i,j]},100,EventType.GRAVE_DIRT);
							Q.Add(grave_dirt_event);
						}
						else{
							grave_dirt_event.area.Add(tile[i,j]);
						}
						break;
					case CellType.Gravel:
						Tile.Create(TileType.GRAVEL,i,j);
						break;
					case CellType.Ice:
						Tile.Create(TileType.ICE,i,j);
						break;
					case CellType.Oil:
						Tile.Create(TileType.FLOOR,i,j);
						tile[i,j].AddFeature(FeatureType.OIL);
						break;
					case CellType.PoisonBulb:
						Tile.Create(TileType.POISON_BULB,i,j);
						break;
					case CellType.Poppies:
						Tile.Create(TileType.POPPY_FIELD,i,j);
						if(poppy_event == null){
							poppy_event = new Event(new List<Tile>{tile[i,j]},100,EventType.POPPIES);
							Q.Add(poppy_event);
						}
						else{
							poppy_event.area.Add(tile[i,j]);
						}
						break;
					case CellType.Slime:
						Tile.Create(TileType.FLOOR,i,j);
						tile[i,j].AddFeature(FeatureType.SLIME);
						break;
					case CellType.Torch:
						Tile.Create(TileType.STANDING_TORCH,i,j);
						break;
					case CellType.Vine:
						Tile.Create(TileType.VINE,i,j);
						break;
					case CellType.Webs:
						Tile.Create(TileType.FLOOR,i,j);
						tile[i,j].AddFeature(FeatureType.WEB);
						break;
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					//alltiles.Add(tile[i,j]);
					tile[i,j].solid_rock = true;
				}
			}
			//Global.ReadKey();
			player.ResetForNewLevel();
			foreach(Tile t in AllTiles()){
				if(t.light_radius > 0){
					t.UpdateRadius(0,t.light_radius);
				}
			}
			int num_items = R.Between(0,2);
			for(int i=num_items;i>0;--i){
				SpawnItem();
			}
			bool poltergeist_spawned = false; //i'm not sure this is the right call, but for now
			bool mimic_spawned = false; // i'm limiting these guys, to avoid "empty" levels
			bool marble_horror_spawned = false;
			int num_monsters = R.Roll(2,2) + 4;
			if(num_monsters == 6){
				num_monsters += R.Roll(3); //this works out to 7/12 seven, 4/12 eight, and 1/12 nine.
			}
			for(int i=num_monsters;i>0;--i){
				ActorType type = MobType();
				if(type == ActorType.POLTERGEIST){
					if(!poltergeist_spawned){
						SpawnMob(type);
						poltergeist_spawned = true;
					}
					else{
						++i; //try again..
					}
				}
				else{
					if(type == ActorType.MIMIC){
						if(!mimic_spawned){
							SpawnMob(type);
							mimic_spawned = true;
						}
						else{
							++i;
						}
					}
					else{
						if(type == ActorType.MARBLE_HORROR){
							Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).Random();
							if(!marble_horror_spawned && statue != null){
								SpawnMob(type);
								marble_horror_spawned = true;
							}
							else{
								++i;
							}
						}
						else{
							if(type == ActorType.ENTRANCER){
								if(i >= 2){ //need 2 slots here
									Actor entrancer = SpawnMob(type);
									entrancer.attrs[AttrType.WANDERING]++;
									List<Tile> tiles = new List<Tile>();
									int dist = 1;
									while(tiles.Count == 0 && dist < 100){
										foreach(Tile t in entrancer.TilesAtDistance(dist)){
											if(t.passable && !t.IsTrap() && t.actor() == null){
												tiles.Add(t);
											}
										}
										++dist;
									}
									if(tiles.Count > 0){
										ActorType thralltype = ActorType.SPECIAL;
										bool done = false;
										while(!done){
											thralltype = MobType();
											switch(thralltype){ //not on the list: group/stealth/ranged/rare/immobile/hazardous monsters, plus carrion crawler, mechanical knight, mud elemental, and luminous avenger
											case ActorType.ROBED_ZEALOT:
											case ActorType.WILD_BOAR:
											case ActorType.TROLL:
											case ActorType.DERANGED_ASCETIC:
											case ActorType.ALASI_BATTLEMAGE:
											case ActorType.ALASI_SOLDIER:
											case ActorType.SKITTERMOSS:
											case ActorType.STONE_GOLEM:
											case ActorType.OGRE_BARBARIAN:
											case ActorType.SNEAK_THIEF:
											case ActorType.CRUSADING_KNIGHT:
											case ActorType.CORROSIVE_OOZE:
											case ActorType.ALASI_SENTINEL:
											case ActorType.CYCLOPEAN_TITAN:
												done = true;
												break;
											}
										}
										Tile t = tiles.Random();
										Actor thrall = Actor.Create(thralltype,t.row,t.col,TiebreakerAssignment.InsertAfterCurrent);
										if(entrancer.group == null){
											entrancer.group = new List<Actor>{entrancer};
										}
										entrancer.group.Add(thrall);
										thrall.group = entrancer.group;
										--i;
									}
								}
								else{
									++i;
								}
							}
							else{
								Actor a = SpawnMob(type);
								if(type == ActorType.WARG){
									if(a.group != null){
										foreach(Actor a2 in a.group){
											a2.attrs[AttrType.WANDERING]++;
										}
									}
								}
								else{
									if(a.AlwaysWanders() || (R.PercentChance(40) && a.CanWanderAtLevelGen())){
										a.attrs[AttrType.WANDERING]++;
									}
								}
							}
						}
					}
				}
			}
			for(int i=(current_level-3)/4;i>0;--i){ //yes, this is all copied and pasted for a one-line change. i'll try to fix it later.
				if(R.CoinFlip()){ //generate some shallow monsters
					ActorType type = ShallowMobType();
					if(type == ActorType.POLTERGEIST){
						if(!poltergeist_spawned){
							SpawnMob(type);
							poltergeist_spawned = true;
						}
						else{
							++i; //try again..
						}
					}
					else{
						if(type == ActorType.MIMIC){
							if(!mimic_spawned){
								SpawnMob(type);
								mimic_spawned = true;
							}
							else{
								++i;
							}
						}
						else{
							if(type == ActorType.MARBLE_HORROR){
								Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).Random();
								if(!marble_horror_spawned && statue != null){
									SpawnMob(type);
									marble_horror_spawned = true;
								}
								else{
									++i;
								}
							}
							else{
								if(type == ActorType.ENTRANCER){
									if(i >= 2){ //need 2 slots here
										Actor entrancer = SpawnMob(type);
										entrancer.attrs[AttrType.WANDERING]++;
										List<Tile> tiles = new List<Tile>();
										int dist = 1;
										while(tiles.Count == 0 && dist < 100){
											foreach(Tile t in entrancer.TilesAtDistance(dist)){
												if(t.passable && !t.IsTrap() && t.actor() == null){
													tiles.Add(t);
												}
											}
											++dist;
										}
										if(tiles.Count > 0){
											ActorType thralltype = ActorType.SPECIAL;
											bool done = false;
											while(!done){
												thralltype = MobType();
												switch(thralltype){
												case ActorType.ROBED_ZEALOT:
												case ActorType.DERANGED_ASCETIC:
												case ActorType.BERSERKER:
												case ActorType.TROLL:
												case ActorType.CRUSADING_KNIGHT:
												case ActorType.SKITTERMOSS:
												case ActorType.OGRE_BARBARIAN:
												case ActorType.SNEAK_THIEF:
												case ActorType.STONE_GOLEM:
												case ActorType.LUMINOUS_AVENGER:
												case ActorType.WILD_BOAR:
												case ActorType.ALASI_SOLDIER:
												case ActorType.CORROSIVE_OOZE:
												case ActorType.ALASI_SENTINEL:
													done = true;
													break;
												}
											}
											Tile t = tiles.Random();
											Actor thrall = Actor.Create(thralltype,t.row,t.col,TiebreakerAssignment.InsertAfterCurrent);
											if(entrancer.group == null){
												entrancer.group = new List<Actor>{entrancer};
											}
											entrancer.group.Add(thrall);
											thrall.group = entrancer.group;
											--i;
										}
									}
									else{
										++i;
									}
								}
								else{
									Actor a = SpawnMob(type);
									if(type == ActorType.WARG){
										if(a.group != null){
											foreach(Actor a2 in a.group){
												a2.attrs[AttrType.WANDERING]++;
											}
										}
									}
									else{
										if(a.AlwaysWanders() || (R.PercentChance(40) && a.CanWanderAtLevelGen())){
											a.attrs[AttrType.WANDERING]++;
										}
									}
								}
							}
						}
					}
				}
			}
			int minimum_distance_from_stairs = 0;
			PosArray<int> distance_from_stairs = null;
			if(stairs != null){
				distance_from_stairs = tile.GetDijkstraMap(x=>tile[x].BlocksConnectivityOfMap(),new List<pos>{stairs.p});
				minimum_distance_from_stairs = distance_from_stairs[distance_from_stairs.PositionsWhere(x=>distance_from_stairs[x].IsValidDijkstraValue()).WhereGreatest(x=>distance_from_stairs[x]).Random()] / 2;
			}
			bool[,] good_location = new bool[ROWS,COLS];
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(tile[i,j].Is(TileType.FLOOR) && !tile[i,j].Is(FeatureType.WEB) && (stairs == null || distance_from_stairs[i,j] >= minimum_distance_from_stairs)){
						good_location[i,j] = true;
					}
					else{
						good_location[i,j] = false;
					}
				}
			}
			foreach(Actor a in AllActors()){
				if(a != player){
					good_location[a.row,a.col] = false;
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(good_location[i,j] && a.HasLOS(i,j)){
								good_location[i,j] = false;
							}
						}
					}
				}
			}
			bool at_least_one_good = false;
			for(int i=0;i<ROWS && !at_least_one_good;++i){
				for(int j=0;j<COLS && !at_least_one_good;++j){
					if(good_location[i,j]){
						at_least_one_good = true;
					}
				}
			}
			if(!at_least_one_good){
				foreach(Actor a in AllActors()){
					if(a != player){
						good_location[a.row,a.col] = false;
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(good_location[i,j] && a.CanSee(i,j)){ //checking CanSee this time
									good_location[i,j] = false;
								}
							}
						}
					}
				}
			}
			List<Tile> goodtiles = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(good_location[i,j]){
						goodtiles.Add(tile[i,j]);
					}
				}
			}
			if(goodtiles.Count > 0){
				Tile t = goodtiles.Random();
				int light = player.light_radius;
				int burning = player.attrs[AttrType.BURNING];
				int shining = player.attrs[AttrType.SHINING];
				player.light_radius = 0;
				player.attrs[AttrType.BURNING] = 0;
				player.attrs[AttrType.SHINING] = 0;
				player.Move(t.row,t.col);
				player.light_radius = light;
				player.attrs[AttrType.BURNING] = burning;
				player.attrs[AttrType.SHINING] = shining;
				player.UpdateRadius(0,player.LightRadius());
			}
			else{
				for(bool done=false;!done;){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
						if(t.IsTrap()){
							good = false;
						}
					}
					if(good && tile[rr,rc].passable && actor[rr,rc] == null){
						int light = player.light_radius;
						int burning = player.attrs[AttrType.BURNING];
						int shining = player.attrs[AttrType.SHINING];
						player.light_radius = 0;
						player.attrs[AttrType.BURNING] = 0;
						player.attrs[AttrType.SHINING] = 0;
						player.Move(rr,rc);
						player.light_radius = light;
						player.attrs[AttrType.BURNING] = burning;
						player.attrs[AttrType.SHINING] = shining;
						player.UpdateRadius(0,player.LightRadius());
						done = true;
					}
				}
			}
			actor[player.row,player.col] = player; //this line fixes a bug that occurs when the player ends up in the same position on a new level
			Screen.screen_center_col = player.col;
			if(R.CoinFlip()){ //todo: copied and pasted below
				bool done = false;
				for(int tries=0;!done && tries<500;++tries){
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(2)){
						if(t.type != TileType.WALL){
							good = false;
							break;
						}
					}
					if(good){
						List<int> dirs = new List<int>();
						bool long_corridor = false;
						int connections = 0;
						for(int i=2;i<=8;i+=2){
							Tile t = tile[rr,rc].TileInDirection(i).TileInDirection(i);
							bool good_dir = true;
							int distance = -1;
							while(good_dir && t != null && t.type == TileType.WALL){
								if(t.TileInDirection(i.RotateDir(false,2)).type != TileType.WALL){
									good_dir = false;
								}
								if(t.TileInDirection(i.RotateDir(true,2)).type != TileType.WALL){
									good_dir = false;
								}
								t = t.TileInDirection(i);
								if(t != null && t.type == TileType.STATUE){
									good_dir = false;
								}
								++distance;
							}
							if(good_dir && t != null){
								dirs.Add(i);
								++connections;
								if(distance >= 4){
									long_corridor = true;
								}
							}
						}
						if(dirs.Count > 0){
							List<TileType> all_possible_traps = new List<TileType>{TileType.GRENADE_TRAP,TileType.POISON_GAS_TRAP,TileType.PHANTOM_TRAP,TileType.FIRE_TRAP,TileType.SHOCK_TRAP,TileType.SCALDING_OIL_TRAP,TileType.FLING_TRAP,TileType.STONE_RAIN_TRAP};
							List<TileType> possible_traps = new List<TileType>();
							//int trap_roll = R.Roll(7);
							int num_types = R.Between(2,3);
							/*if(trap_roll == 1){
								num_types = 1;
							}
							else{
								if(trap_roll <= 4){
									num_types = 2;
								}
								else{
									num_types = 3;
								}
							}*/
							for(int i=0;i<num_types;++i){
								TileType tt = all_possible_traps.Random();
								if(possible_traps.Contains(tt)){
									--i;
								}
								else{
									possible_traps.Add(tt);
								}
							}
							bool stone_slabs = false; //(instead of hidden doors)
							if(R.OneIn(4)){
								stone_slabs = true;
							}
							foreach(int i in dirs){
								Tile t = tile[rr,rc].TileInDirection(i);
								int distance = -2; //distance of the corridor between traps and secret door
								while(t.type == TileType.WALL){
									++distance;
									t = t.TileInDirection(i);
								}
								if(long_corridor && distance < 4){
									continue;
								}
								t = tile[rr,rc].TileInDirection(i);
								while(t.type == TileType.WALL){
									if(distance >= 4){
										TileType tt = TileType.FLOOR;
										if(R.Roll(3) >= 2){
											tt = possible_traps.Random();
											hidden.Add(t);
										}
										t.TransformTo(tt);
										t.name = "floor";
										t.the_name = "the floor";
										t.a_name = "a floor";
										t.symbol = '.';
										t.color = Color.White;
										if(t.DistanceFrom(tile[rr,rc]) < distance+2){
											Tile neighbor = t.TileInDirection(i.RotateDir(false,2));
											if(neighbor.TileInDirection(i.RotateDir(false,1)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(false,2)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(false,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(R.Roll(3) >= 2){
													tt = possible_traps.Random();
												}
												neighbor.TransformTo(tt);
												if(possible_traps.Contains(tt)){
													neighbor.name = "floor";
													neighbor.the_name = "the floor";
													neighbor.a_name = "a floor";
													neighbor.symbol = '.';
													neighbor.color = Color.White;
													hidden.Add(neighbor);
												}
											}
											neighbor = t.TileInDirection(i.RotateDir(true,2));
											if(neighbor.TileInDirection(i.RotateDir(true,1)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(true,2)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(true,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(R.Roll(3) >= 2){
													tt = possible_traps.Random();
												}
												neighbor.TransformTo(tt);
												if(possible_traps.Contains(tt)){
													neighbor.name = "floor";
													neighbor.the_name = "the floor";
													neighbor.a_name = "a floor";
													neighbor.symbol = '.';
													neighbor.color = Color.White;
													hidden.Add(neighbor);
												}
											}
										}
									}
									else{
										TileType tt = TileType.FLOOR;
										if(R.CoinFlip()){
											tt = Tile.RandomTrap();
											hidden.Add(t);
										}
										t.TransformTo(tt);
										if(tt != TileType.FLOOR){
											t.name = "floor";
											t.the_name = "the floor";
											t.a_name = "a floor";
											t.symbol = '.';
											t.color = Color.White;
										}
									}
									t = t.TileInDirection(i);
								}
								t = t.TileInDirection(i.RotateDir(true,4));
								if(stone_slabs){
									t.TransformTo(TileType.STONE_SLAB);
									Q.Add(new Event(t,new List<Tile>{t.TileInDirection(i.RotateDir(true,4))},100,EventType.STONE_SLAB));
								}
								else{
									t.TransformTo(TileType.HIDDEN_DOOR);
									hidden.AddUnique(t);
								}
								t = t.TileInDirection(i.RotateDir(true,4));
								if(R.CoinFlip()){
									if(t.IsTrap()){
										t.type = TileType.ALARM_TRAP;
									}
									else{
										t.TransformTo(TileType.ALARM_TRAP);
										t.name = "floor";
										t.the_name = "the floor";
										t.a_name = "a floor";
										t.symbol = '.';
										t.color = Color.White;
										hidden.AddUnique(t);
									}
								}
							}
							if(long_corridor && connections == 1){
								foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
									t.TransformTo(possible_traps.Random());
									t.name = "floor";
									t.the_name = "the floor";
									t.a_name = "a floor";
									t.symbol = '.';
									t.color = Color.White;
									hidden.Add(t);
								}
								tile[rr,rc].TileInDirection(dirs[0].RotateDir(true,4)).TransformTo(TileType.CHEST);
								tile[rr,rc].TileInDirection(dirs[0].RotateDir(true,4)).color = Color.Yellow;
							}
							else{
								foreach(Tile t in tile[rr,rc].TilesAtDistance(1)){
									t.TransformTo(Tile.RandomTrap());
									t.name = "floor";
									t.the_name = "the floor";
									t.a_name = "a floor";
									t.symbol = '.';
									t.color = Color.White;
									hidden.Add(t);
								}
								tile[rr,rc].TransformTo(TileType.CHEST);
								tile[rr,rc].color = Color.Yellow;
							}
							done = true;
						}
					}
				}
			}
			/*if(R.CoinFlip()){
				bool done = false;
				for(int tries=0;!done && tries<500;++tries){
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(2)){
						if(t.type != TileType.WALL){
							good = false;
							break;
						}
					}
					if(good){
						List<int> dirs = new List<int>();
						bool long_corridor = false;
						int connections = 0;
						for(int i=2;i<=8;i+=2){
							Tile t = tile[rr,rc].TileInDirection(i).TileInDirection(i);
							bool good_dir = true;
							int distance = -1;
							while(good_dir && t != null && t.type == TileType.WALL){
								if(t.TileInDirection(i.RotateDir(false,2)).type != TileType.WALL){
									good_dir = false;
								}
								if(t.TileInDirection(i.RotateDir(true,2)).type != TileType.WALL){
									good_dir = false;
								}
								t = t.TileInDirection(i);
								if(t != null && t.type == TileType.STATUE){
									good_dir = false;
								}
								++distance;
							}
							if(good_dir && t != null){
								dirs.Add(i);
								++connections;
								if(distance >= 4){
									long_corridor = true;
								}
							}
						}
						if(dirs.Count > 0){
							List<TileType> possible_traps = new List<TileType>();
							int trap_roll = R.Roll(7);
							if(trap_roll == 1 || trap_roll == 4 || trap_roll == 5 || trap_roll == 7){
								possible_traps.Add(TileType.GRENADE_TRAP);
							}
							if(trap_roll == 2 || trap_roll == 4 || trap_roll == 6 || trap_roll == 7){
								possible_traps.Add(TileType.POISON_GAS_TRAP);
							}
							if(trap_roll == 3 || trap_roll == 5 || trap_roll == 6 || trap_roll == 7){
								possible_traps.Add(TileType.PHANTOM_TRAP);
							}
							bool stone_slabs = false; //(instead of hidden doors)
							if(R.OneIn(4)){
								stone_slabs = true;
							}
							foreach(int i in dirs){
								Tile t = tile[rr,rc].TileInDirection(i);
								int distance = -2; //distance of the corridor between traps and secret door
								while(t.type == TileType.WALL){
									++distance;
									t = t.TileInDirection(i);
								}
								if(long_corridor && distance < 4){
									continue;
								}
								t = tile[rr,rc].TileInDirection(i);
								while(t.type == TileType.WALL){
									if(distance >= 4){
										TileType tt = TileType.FLOOR;
										if(R.Roll(3) >= 2){
											tt = possible_traps.Random();
											hidden.Add(t);
										}
										t.TransformTo(tt);
										t.name = "floor";
										t.the_name = "the floor";
										t.a_name = "a floor";
										t.symbol = '.';
										t.color = Color.White;
										if(t.DistanceFrom(tile[rr,rc]) < distance+2){
											Tile neighbor = t.TileInDirection(i.RotateDir(false,2));
											if(neighbor.TileInDirection(i.RotateDir(false,1)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(false,2)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(false,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(R.Roll(3) >= 2){
													tt = possible_traps.Random();
												}
												neighbor.TransformTo(tt);
												if(possible_traps.Contains(tt)){
													neighbor.name = "floor";
													neighbor.the_name = "the floor";
													neighbor.a_name = "a floor";
													neighbor.symbol = '.';
													neighbor.color = Color.White;
													hidden.Add(neighbor);
												}
											}
											neighbor = t.TileInDirection(i.RotateDir(true,2));
											if(neighbor.TileInDirection(i.RotateDir(true,1)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(true,2)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(true,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(R.Roll(3) >= 2){
													tt = possible_traps.Random();
												}
												neighbor.TransformTo(tt);
												if(possible_traps.Contains(tt)){
													neighbor.name = "floor";
													neighbor.the_name = "the floor";
													neighbor.a_name = "a floor";
													neighbor.symbol = '.';
													neighbor.color = Color.White;
													hidden.Add(neighbor);
												}
											}
										}
									}
									else{
										TileType tt = TileType.FLOOR;
										if(R.CoinFlip()){
											tt = Tile.RandomTrap();
											hidden.Add(t);
										}
										t.TransformTo(tt);
										if(tt != TileType.FLOOR){
											t.name = "floor";
											t.the_name = "the floor";
											t.a_name = "a floor";
											t.symbol = '.';
											t.color = Color.White;
										}
									}
									t = t.TileInDirection(i);
								}
								t = t.TileInDirection(i.RotateDir(true,4));
								if(stone_slabs){
									t.TransformTo(TileType.STONE_SLAB);
									Q.Add(new Event(t,new List<Tile>{t.TileInDirection(i.RotateDir(true,4))},100,EventType.STONE_SLAB));
								}
								else{
									t.TransformTo(TileType.HIDDEN_DOOR);
									hidden.AddUnique(t);
								}
								t = t.TileInDirection(i.RotateDir(true,4));
								if(R.CoinFlip()){
									if(t.IsTrap()){
										t.type = TileType.ALARM_TRAP;
									}
									else{
										t.TransformTo(TileType.ALARM_TRAP);
										t.name = "floor";
										t.the_name = "the floor";
										t.a_name = "a floor";
										t.symbol = '.';
										t.color = Color.White;
										hidden.AddUnique(t);
									}
								}
							}
							if(long_corridor && connections == 1){
								foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
									t.TransformTo(possible_traps.Random());
									t.name = "floor";
									t.the_name = "the floor";
									t.a_name = "a floor";
									t.symbol = '.';
									t.color = Color.White;
									hidden.Add(t);
								}
								tile[rr,rc].TileInDirection(dirs[0].RotateDir(true,4)).TransformTo(TileType.CHEST);
								tile[rr,rc].TileInDirection(dirs[0].RotateDir(true,4)).color = Color.Yellow;
							}
							else{
								foreach(Tile t in tile[rr,rc].TilesAtDistance(1)){
									t.TransformTo(Tile.RandomTrap());
									t.name = "floor";
									t.the_name = "the floor";
									t.a_name = "a floor";
									t.symbol = '.';
									t.color = Color.White;
									hidden.Add(t);
								}
								tile[rr,rc].TransformTo(TileType.CHEST);
								tile[rr,rc].color = Color.Yellow;
							}
							done = true;
						}
					}
				}
			}*/
			foreach(Tile t in AllTiles()){
				if(t.type != TileType.WALL){
					foreach(Tile neighbor in t.TilesAtDistance(1)){
						neighbor.solid_rock = false;
					}
				}
				if(t.type == TileType.GLOWING_FUNGUS){
					foreach(Tile neighbor in t.TilesAtDistance(1)){
						if(neighbor.type == TileType.WALL){
							neighbor.color = Color.RandomGlowingFungus;
						}
					}
				}
			}
			if(level_types[current_level-1] == LevelType.Hive){
				var dijkstra = tile.GetDijkstraMap(x=>false,y=>tile[y].type != TileType.WALL);
				for(int i=0;i<ROWS;++i){
					for(int j=0;j<COLS;++j){
						if((dijkstra[i,j] == 1 && !R.OneIn(20)) || (dijkstra[i,j] == 2 && R.CoinFlip())){
							if(i == 0 || j == 0 || i == ROWS-1 || j == COLS-1){
								tile[i,j].color = Color.DarkYellow;
								tile[i,j].SetName("waxy wall"); //borders become waxy but don't burn
							}
							else{
								tile[i,j].Toggle(null,TileType.WAX_WALL);
							}
						}
					}
				}
			}
			if(level_types[current_level-1] == LevelType.Fortress){
				foreach(pos p in tile.PositionsWhere(x=>tile[x].Is(TileType.WALL,TileType.HIDDEN_DOOR))){
					tile[p].color = Color.TerrainDarkGray;
				}
			}
			if(poppy_event != null){
				poppy_distance_map = tile.GetDijkstraMap(x=>!tile[x].Is(TileType.POPPY_FIELD),x=>tile[x].passable && !tile[x].Is(TileType.POPPY_FIELD));
			}
			if(hidden.Count > 0){
				Event e = new Event(hidden,100,EventType.CHECK_FOR_HIDDEN);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			{
				Event e = new Event(10000,EventType.RELATIVELY_SAFE);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			{
				Event e = new Event(R.Between(400,450)*100,EventType.SPAWN_WANDERING_MONSTER);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			if(current_level == 1){
				B.Add("In the mountain pass where travelers vanish, a stone staircase leads downward... Welcome, " + Actor.player_name + "! ");
			}
			else{
				B.Add(LevelMessage());
			}
		}
		private string[] FinalLevelLayout(){
			return new string[]{
				"##################################################################",
				"##################....######..........######....##################",
				"###############......##****##........##****##......###############",
				"############..+......+*2**2*+........+*2**2*+......+..############",
				"#########.....+......+**&&**+........+**&&**+......+.....#########",
				"########......##+++++#**&&**#++++++++#**&&**#+++++##......########",
				"#######.......##.....+*2**2*+........+*2**2*+.....##.......#######",
				"######........+......##****##........##****##......+........######",
				"######........+......##+++##..........##+++##......+........######",
				"#####...#++#++##.....+....+.....XX.....+....+.....##++#++#...#####",
				"#####+++#..+...##....+....+.....XX.....+....+....##...+..#+++#####",
				"#####......+....###++#+#..+............+..#+#++###....+......#####",
				"#####......+.....##****##+##..........##+##****##.....+......#####",
				"######.....+.....+*2**2*+..##........##..+*2**2*+.....+.....######",
				"######.....+.....+**&&**+...###++++###...+**&&**+.....+.....######",
				"#######....+..#++#**&&**+....##****##....+**&&**#++#..+....#######",
				"########...#++#..+*2**2*+....+*2**2*+....+*2**2*+..#++#...########",
				"#########..+.....##****##++++#**&&**#++++##****##.....+..#########",
				"############......#+#++#.....+**&&**+.....#++#+#......############",
				"###############.....+........+*2**2*+........+.....###############",
				"##################..+........##****##........+..##################",
				"##################################################################"
			};
		}
		public pos FinalLevelSummoningCircle(int num){
			int extra_row = R.Between(0,1);
			int extra_col = R.Between(0,1);
			switch(num){
			case 0:
				return new pos(4+extra_row,24+extra_col);
			case 1:
				return new pos(4+extra_row,40+extra_col);
			case 2:
				return new pos(14+extra_row,20+extra_col);
			case 3:
				return new pos(14+extra_row,44+extra_col);
			case 4:
			default:
				return new pos(17+extra_row,32+extra_col);
			}
		}
		public void IncrementClock(){
			final_level_clock++;
			int multiplier = 50;
			string[] messages = new string[]{"The dungeon trembles slightly. You feel that something bad is about to happen. ",
				"The shaking increases, and you hear a rumbling sound from the center of this area. ",
				"Dust falls from the ceiling as the dungeon shakes violently. ",
				"A demonic howling begins. ",
				"A chorus of demonic voices heralds the coming of Kersai. ",
				"A sense of urgency fills you. ",
				"A sense of doom fills you. ",
				"You hear booming laughter as the air turns to fire. Kersai is coming. ",
				"Walls crack and crumble around you. The air turns to pain. Kersai is coming. ",
				"A crescendo from the demonic chorus announces the arrival of the Demon King. Your flesh turns to ash as Kersai enters this world. "};
			if(final_level_clock == 1){
				B.Add(messages[0]);
				B.PrintAll();
				return;
			}
			for(int i=1;i<=9;++i){
				if(final_level_clock == i * multiplier){
					B.Add(messages[i]);
					B.PrintAll();
					switch(i){
					case 7:
						foreach(Actor a in AllActors()){
							a.ApplyBurning();
						}
						foreach(Tile t in AllTiles()){
							if(R.OneIn(20) && t.passable && t.actor() == null){
								t.AddFeature(FeatureType.FIRE);
							}
						}
						break;
					case 8:
						foreach(Tile t in AllTiles()){
							if(t.Is(TileType.WALL,TileType.CRACKED_WALL) && !t.solid_rock && t.p.BoundsCheck(tile,false) && R.OneIn(2)){
								t.TurnToFloor();
								foreach(Tile neighbor in t.TilesAtDistance(1)){
									t.solid_rock = false;
								}
							}
						}
						foreach(Actor a in AllActors()){
							if(!a.IsFinalLevelDemon()){
								a.curhp = 1;
							}
						}
						break;
					case 9:
						player.Kill();
						player.curhp = -R.Roll(66,6);
						break;
					}
				}
			}
		}
		public void GenerateFinalLevel(){
			final_level_cultist_count = new int[5];
			final_level_demon_count = 0;
			final_level_clock = 0;
			current_level = 21;
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j] != null){
						if(actor[i,j] != player){
							actor[i,j].inv.Clear();
							actor[i,j].target = null;
							if(actor[i,j].group != null){
								actor[i,j].group.Clear();
								actor[i,j].group = null;
							}
						}
						actor[i,j] = null;
					}
					if(tile[i,j] != null){
						tile[i,j].inv = null;
					}
					tile[i,j] = null;
				}
			}
			wiz_lite = false;
			wiz_dark = false;
			feat_gained_this_level = false;
			generated_this_level = new Dict<ActorType, int>();
			monster_density = new PosArray<int>(ROWS,COLS);
			shrine_locations = new pos[5];
			for(int i=0;i<5;++i){
				shrine_locations[i] = new pos(-1,-1);
			}
			Q.ResetForNewLevel();
			last_seen = new colorchar[ROWS,COLS];
			Fire.fire_event = null;
			Fire.burning_objects.Clear();
			if(player.IsBurning()){
				Fire.AddBurningObject(player);
			}
			Actor.tiebreakers = new List<Actor>{player};
			Actor.interrupted_path = new pos(-1,-1);
			string[] final_map = FinalLevelLayout();
			PosArray<CellType> map = new PosArray<CellType>(ROWS,COLS);
			PosArray<bool> doors = new PosArray<bool>(ROWS,COLS);
			List<List<pos>> door_sets = new List<List<pos>>();
			for(int i=0;i<ROWS;++i){
				string s = final_map[i];
				for(int j=0;j<COLS;++j){
					switch(s[j]){
					case '#':
						map[i,j] = CellType.Wall;
						break;
					case '.':
						map[i,j] = CellType.RoomInterior;
						break;
					case '2':
						map[i,j] = CellType.RoomFeature1;
						break;
					case '&':
						map[i,j] = CellType.RoomFeature2;
						break;
					case '*':
						map[i,j] = CellType.RoomFeature3;
						break;
					case 'X':
						map[i,j] = CellType.RoomFeature4;
						break;
					case '+':
						map[i,j] = CellType.Wall;
						if(!doors[i,j]){
							doors[i,j] = true;
							pos p = new pos(i,j);
							List<pos> door_set = new List<pos>{p};
							foreach(int dir in new int[]{2,6}){
								p = new pos(i,j);
								while(true){
									p = p.PosInDir(dir);
									if(p.BoundsCheck(tile) && final_map[p.row][p.col] == '+'){
										doors[p] = true;
										door_set.Add(p);
									}
									else{
										break;
									}
								}
							}
							door_sets.Add(door_set);
						}
						break;
					}
				}
			}
			Dungeon d = new Dungeon(ROWS,COLS);
			d.map = map;
			while(!d.IsFullyConnected() && door_sets.Count > 0){
				List<pos> door_set = door_sets.RemoveRandom();
				d.map[door_set.Random()] = CellType.RoomInterior;
			}
			List<Tile> flames = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					switch(map[i,j]){
					case CellType.Wall:
						Tile.Create(TileType.WALL,i,j);
						break;
					case CellType.RoomFeature1:
						Tile.Create(TileType.DEMONIC_IDOL,i,j);
						break;
					case CellType.RoomFeature2:
						Tile.Create(TileType.FLOOR,i,j);
						flames.Add(tile[i,j]);
						break;
					case CellType.RoomFeature3:
						Tile.Create(TileType.FLOOR,i,j);
						tile[i,j].color = Color.RandomDoom;
						break;
					case CellType.RoomFeature4:
						Tile.Create(TileType.FIRE_RIFT,i,j);
						break;
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					tile[i,j].solid_rock = true;
				}
			}
			player.ResetForNewLevel();
			foreach(Tile t in AllTiles()){
				if(t.light_radius > 0){
					t.UpdateRadius(0,t.light_radius);
				}
			}
			foreach(Tile t in flames){
				t.AddFeature(FeatureType.FIRE);
			}
			int light = player.light_radius;
			int fire = player.attrs[AttrType.BURNING];
			player.light_radius = 0;
			player.attrs[AttrType.BURNING] = 0;
			player.Move(6,7);
			player.UpdateRadius(0,Math.Max(light,fire),true);
			player.light_radius = light;
			player.attrs[AttrType.BURNING] = fire;
			foreach(Tile t in AllTiles()){
				if(t.type != TileType.WALL){
					foreach(Tile neighbor in t.TilesAtDistance(1)){
						neighbor.solid_rock = false;
					}
				}
			}
			for(int i=0;i<3;++i){
				Actor a = SpawnMob(ActorType.CULTIST);
				List<Actor> group = new List<Actor>(a.group);
				a.group.Clear();
				if(a != null && group != null){
					int ii = 0;
					foreach(Actor a2 in group){
						++ii;
						pos circle = FinalLevelSummoningCircle(ii);
						a2.FindPath(circle.row,circle.col);
						a2.attrs[AttrType.COOLDOWN_2] = ii;
						a2.type = ActorType.FINAL_LEVEL_CULTIST;
						a2.group = null;
						if(!R.OneIn(20)){
							a2.attrs[AttrType.NO_ITEM] = 1;
						}
					}
				}
			}
			Q.Add(new Event(500,EventType.FINAL_LEVEL_SPAWN_CULTISTS));
		}
		private enum FloorType{Brush,Water,Gravel,GlowingFungus,Ice,PoppyField,GraveDirt};
		public void GenerateFloorTypes(PosArray<CellType> map){
			List<FloorType> floors = new List<FloorType>();
			int[] rarity = null;
			switch(level_types[current_level-1]){
			case LevelType.Standard:
				rarity = new int[]{2,2,7,7,8,30,20};
				break;
			case LevelType.Cave:
				rarity = new int[]{5,4,3,2,5,30,10};
				break;
			case LevelType.Mine:
				rarity = new int[]{20,10,2,4,8,100,10};
				break;
			case LevelType.Hive:
				rarity = new int[]{15,10,5,20,20,20,100};
				break;
			case LevelType.Fortress:
				rarity = new int[]{20,20,5,10,20,100,15};
				break;
			case LevelType.Crypt:
				rarity = new int[]{6,10,5,4,4,30,4};
				break;
			case LevelType.Garden:
				rarity = new int[]{0,0,0,0,0,0,0}; //special handling, see GenerateLevel()
				break;
			case LevelType.Slime: //todo
			default:
				rarity = new int[]{2,2,4,4,6,30,10};
				break;
			}
			foreach(FloorType f in Enum.GetValues(typeof(FloorType))){
				floors.Add(f);
			}
			List<FloorType> floortype_pool = new List<FloorType>();
			while(floortype_pool.Count == 0){
				//floortype_pool.Clear();
				for(int i=0;i<7;++i){
					if(rarity[i] > 0 && R.OneIn(rarity[i])){
						floortype_pool.Add(floors[i]);
					}
				}
			}
			int num = R.Between(4,7);
			if(level_types[current_level-1] == LevelType.Mine){
				num = R.Between(1,3);
			}
			for(;num>0;--num){
				FloorType f = floortype_pool.Random();
				for(int i=0;i<50;++i){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					if(map[rr,rc].IsRoomType() && map[rr,rc].IsFloor()){
						CellType cell = CellType.Wall;
						int max_radius = 0;
						switch(f){
						case FloorType.GlowingFungus:
							cell = CellType.GlowingFungus;
							max_radius = R.Between(2,6);
							break;
						case FloorType.Gravel:
							cell = CellType.Gravel;
							max_radius = R.Between(3,4);
							break;
						case FloorType.Brush:
							cell = CellType.Brush;
							max_radius = R.Between(4,7);
							break;
						case FloorType.Water:
							cell = CellType.ShallowWater;
							max_radius = R.Between(4,6);
							break;
						case FloorType.PoppyField:
							cell = CellType.Poppies;
							max_radius = R.Between(4,5);
							break;
						case FloorType.Ice:
							cell = CellType.Ice;
							max_radius = R.Between(4,6);
							break;
						case FloorType.GraveDirt:
							cell = CellType.GraveDirt;
							max_radius = R.Between(3,5);
							break;
						}
						if(level_types[current_level-1] == LevelType.Fortress && max_radius > 2){
							max_radius = 2;
						}
						map[rr,rc] = cell;
						for(int j=1;j<=max_radius;++j){
							List<pos> added = new List<pos>();
							foreach(pos p in new pos(rr,rc).PositionsWithinDistance(j)){
								if(map[p] == cell){
									foreach(int dir in U.FourDirections){
										pos neighbor = p.PosInDir(dir);
										if(!neighbor.BoundsCheck(map)){
											continue;
										}
										if(R.CoinFlip()){
											if(map[neighbor].IsFloor()){
												if(cell.Is(CellType.ShallowWater,CellType.Ice,CellType.GraveDirt)){
													bool valid = true;
													foreach(pos p2 in neighbor.CardinalAdjacentPositions()){
														if(map[p2].IsCorridorType()){
															valid = false;
															break;
														}
													}
													if(valid){
														added.AddUnique(neighbor);
													}
												}
												else{
													added.AddUnique(neighbor);
												}
											}
											else{
												if(cell.Is(CellType.ShallowWater,CellType.Gravel,CellType.GraveDirt) && neighbor.BoundsCheck(tile,false) && map[neighbor].IsWall()){
													bool valid = true;
													for(int ii=-1;ii<=1;++ii){
														if(!map[neighbor.PosInDir(dir.RotateDir(true,ii))].IsWall()){
															valid = false;
															break;
														}
													}
													if(valid){
														added.AddUnique(neighbor);
													}
												}
											}
										}
									}
								}
							}
							foreach(pos p in added){
								map[p] = cell;
							}
						}
						if(cell == CellType.GraveDirt){
							if(!new pos(rr,rc).PositionsAtDistance(1).Any(x=>map[x] == CellType.Tombstone)){
								map[rr,rc] = CellType.Tombstone;
							}
						}
						break;
					}
				}
			}
		}
		private enum DungeonFeature{POOL_OF_RESTORATION,FIRE_GEYSER,BLAST_FUNGUS,POISON_VENT,
			FOG_VENT,BARREL,WEBS,SLIME,OIL,FIRE_PIT,TORCH,VINES,RUBBLE,CRACKED_WALL};
		public void GenerateFeatures(PosArray<CellType> map,List<pos> interesting_tiles){
			List<DungeonFeature> features = new List<DungeonFeature>();
			foreach(DungeonFeature df in Enum.GetValues(typeof(DungeonFeature))){
				features.Add(df);
			}
			int[] rarity = null;
			switch(level_types[current_level-1]){
			case LevelType.Standard:
				rarity = new int[]{30,40,15,30,
					25,6,8,15,15,3,3,4,4,4};
				break;
			case LevelType.Cave:
				rarity = new int[]{30,15,10,15,
					15,100,8,10,30,5,25,6,3,4};
				break;
			case LevelType.Mine:
				rarity = new int[]{30,20,5,16,
					18,6,7,15,10,3,5,30,1,1};
				break;
			case LevelType.Hive:
				rarity = new int[]{30,15,100,50,
					50,100,4,10,10,100,100,15,25,0};
				break;
			case LevelType.Fortress:
				rarity = new int[]{30,100,100,100,
					100,1,8,25,8,6,1,100,20,15};
				break;
			case LevelType.Garden:
				rarity = new int[]{20,50,50,0,
					20,30,20,20,20,20,5,5,8,15};
				break;
			case LevelType.Crypt:
				rarity = new int[]{30,50,50,25,
					25,30,8,10,15,20,30,5,8,8};
				break;
			case LevelType.Slime: //todo
			default:
				rarity = new int[]{30,20,15,12,
					10,4,8,10,7,3,3,3,4,10};
				break;
			}
			/*int[] rarity = new int[]{30,20,15,12,
					10,4,8,10,7,3,3,3,4};
			int[] frequency = new int[]{1,1,2,2,3,3,3,
				4,4,4,4,2,2,5,5,5,6,5,5,8};*/
			int[] removal_chance = new int[]{95,20,10,60,
				30,25,70,50,60,35,12,10,10,20};
			/*List<DungeonFeature> feature_pool = new List<DungeonFeature>();
			for(int i=0;i<20;++i){
				for(int j=frequency[i];j>0;--j){
					feature_pool.Add(features[i]);
				}
			}*/
			List<DungeonFeature> feature_pool = new List<DungeonFeature>();
			while(feature_pool.Count < 3){
				feature_pool.Clear();
				for(int i=0;i<14;++i){
					if(rarity[i] > 0 && R.OneIn(rarity[i])){
						feature_pool.Add(features[i]);
					}
				}
			}
			List<DungeonFeature> selected_features = new List<DungeonFeature>();
			for(int i=0;i<5 && feature_pool.Count > 0;++i){
				selected_features.Add(feature_pool.RemoveRandom());
			}
			List<DungeonFeature> result = new List<DungeonFeature>();
			for(int count=5;count>0 && selected_features.Count > 0;--count){
				DungeonFeature df = selected_features.Random();
				if(R.PercentChance(removal_chance[(int)df])){
					selected_features.Remove(df);
				}
				result.Add(df);
			}
			List<pos> thin_walls = null;
			if(result.Contains(DungeonFeature.CRACKED_WALL)){
				thin_walls = map.AllPositions().Where(x=>map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck() && map[y].IsFloor()));
			}
			while(result.Count > 0){
				DungeonFeature df = result.RemoveRandom();
				switch(df){
				case DungeonFeature.POOL_OF_RESTORATION:
				case DungeonFeature.FIRE_PIT:
					for(int i=0;i<50;++i){
						int rr = R.Roll(ROWS-4)+1;
						int rc = R.Roll(COLS-4)+1;
						if(interesting_tiles.Count > 0){
							pos p = interesting_tiles.RemoveRandom();
							rr = p.row;
							rc = p.col;
							map[p] = CellType.RoomInterior;
						}
						if(map[rr,rc].IsFloor()){
							bool floors = true;
							foreach(pos p in new pos(rr,rc).PositionsAtDistance(1)){
								if(!map[p].IsFloor()){
									floors = false;
									break;
								}
							}
							if(floors){
								if(df == DungeonFeature.POOL_OF_RESTORATION){
									map[rr,rc] = CellType.Pool;
								}
								if(df == DungeonFeature.FIRE_PIT){
									map[rr,rc] = CellType.FirePit;
								}
								break;
							}
						}
					}
					break;
				case DungeonFeature.BARREL:
				case DungeonFeature.TORCH:
					for(int i=0;i<50;++i){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						if(map[rr,rc].IsRoomType() && map[rr,rc].IsFloor()){
							if(df == DungeonFeature.BARREL){
								map[rr,rc] = CellType.Barrel;
							}
							if(df == DungeonFeature.TORCH){
								map[rr,rc] = CellType.Torch;
							}
							break;
						}
					}
					break;
				case DungeonFeature.WEBS:
				case DungeonFeature.RUBBLE:
					for(int i=0;i<50;++i){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						if(map[rr,rc].IsRoomType()){
							CellType cell = CellType.Webs;
							int max_radius = 2;
							switch(df){
							case DungeonFeature.WEBS:
								cell = CellType.Webs;
								max_radius = 3;
								break;
							case DungeonFeature.RUBBLE:
								cell = CellType.Rubble;
								max_radius = 2;
								break;
							}
							map[rr,rc] = cell;
							for(int j=1;j<=max_radius;++j){
								List<pos> added = new List<pos>();
								foreach(pos p in new pos(rr,rc).PositionsWithinDistance(j)){
									if(map[p] == cell){
										foreach(pos neighbor in p.CardinalAdjacentPositions()){
											if(map[neighbor].IsFloor() && R.CoinFlip()){
												added.AddUnique(neighbor);
											}
										}
									}
								}
								foreach(pos p in added){
									/*if(df == DungeonFeature.RUBBLE){
										foreach(pos neighbor in p.CardinalAdjacentPositions()){
											if(!added.Contains(neighbor) && map[neighbor].IsFloor() && R.OneIn(3)){
												map[neighbor] = CellType.Gravel;
											}
										}
									}*/
									map[p] = cell;
								}
							}
							break;
						}
					}
					break;
				case DungeonFeature.SLIME:
				case DungeonFeature.OIL:
					for(int i=0;i<50;++i){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						if(map[rr,rc].IsFloor()){
							CellType cell = CellType.Wall;
							int max_radius = 2;
							switch(df){
							case DungeonFeature.SLIME:
								cell = CellType.Slime;
								max_radius = 3;
								break;
							case DungeonFeature.OIL:
								cell = CellType.Oil;
								max_radius = 3;
								break;
							}
							map[rr,rc] = cell;
							for(int j=1;j<=max_radius;++j){
								List<pos> added = new List<pos>();
								foreach(pos p in new pos(rr,rc).PositionsWithinDistance(j)){
									if(map[p] == cell){
										foreach(pos neighbor in p.CardinalAdjacentPositions()){
											if(map[neighbor].IsFloor() && R.CoinFlip()){
												added.AddUnique(neighbor);
											}
										}
									}
								}
								foreach(pos p in added){
									map[p] = cell;
								}
							}
							break;
						}
					}
					break;
				case DungeonFeature.FIRE_GEYSER:
					for(int i=0;i<50;++i){
						int rr = R.Roll(ROWS-4)+1;
						int rc = R.Roll(COLS-4)+1;
						if(map[rr,rc].IsFloor()){
							bool floors = true;
							foreach(pos p in new pos(rr,rc).PositionsAtDistance(1)){
								if(!map[p].IsFloor()){
									floors = false;
									break;
								}
							}
							if(floors){
								map[rr,rc] = CellType.Geyser;
								break;
							}
						}
					}
					break;
				case DungeonFeature.VINES:
					for(int i=0;i<500;++i){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						pos p = new pos(rr,rc);
						if(map[p].IsRoomType() && p.HasAdjacentWhere(x=>map[x].IsWall())){
							PosArray<bool> vine = map.GetFloodFillArray(p,false,x=>map[x].IsRoomType() && x.HasAdjacentWhere(y=>map[y].IsWall()) && !R.OneIn(3)); //changed from one in 6 so vines won't fill caves so often
							rr = R.Roll(ROWS-2);
							rc = R.Roll(COLS-2);
							pos p2 = new pos(rr,rc);
							PosArray<bool> new_vine = new PosArray<bool>(ROWS,COLS);
							int max = Math.Max(ROWS,COLS);
							for(int dist=0;dist<max;++dist){
								List<pos> positions = p2.PositionsAtDistance(dist,false);
								if(positions.Count > 0){
									foreach(pos possible_vine in positions){
										if(possible_vine.BoundsCheck(new_vine,false) && vine[possible_vine] && possible_vine.PositionsAtDistance(1).Where(x=>new_vine[x] || map[x] == CellType.Vine).Count < 3){
											new_vine[possible_vine] = true;
										}
									}
								}
								else{
									break;
								}
							}
							List<pos> added = new List<pos>();
							for(int s=1;s<ROWS-1;++s){
								for(int t=1;t<COLS-1;++t){
									if(new_vine[s,t]){
										pos neighbor = new pos(s,t);
										foreach(int dir in U.FourDirections){
											if(R.OneIn(6) && map[neighbor.PosInDir(dir)].IsFloor()){
												added.AddUnique(neighbor.PosInDir(dir));
											}
										}
									}
								}
							}
							foreach(pos neighbor in added){
								new_vine[neighbor] = true;
							}
							for(int s=1;s<ROWS-1;++s){
								for(int t=1;t<COLS-1;++t){
									if(new_vine[s,t]){
										if(R.OneIn(35)){
											map[s,t] = CellType.PoisonBulb;
										}
										else{
											map[s,t] = CellType.Vine;
										}
									}
								}
							}
							break;
						}
					}
					break;
				case DungeonFeature.BLAST_FUNGUS:
				case DungeonFeature.FOG_VENT:
				case DungeonFeature.POISON_VENT:
					for(int i=0;i<50;++i){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						if(map[rr,rc].IsFloor()){
							if(df == DungeonFeature.BLAST_FUNGUS){
								map[rr,rc] = CellType.BlastFungus;
							}
							if(df == DungeonFeature.FOG_VENT){
								map[rr,rc] = CellType.FogVent;
							}
							if(df == DungeonFeature.POISON_VENT){
								map[rr,rc] = CellType.PoisonVent;
							}
							break;
						}
					}
					break;
				case DungeonFeature.CRACKED_WALL:
					for(int i=R.Between(2,4);i>0;--i){
						if(thin_walls.Count > 0){
							map[thin_walls.RemoveRandom()] = CellType.CrackedWall;
						}
					}
					break;
				}
			}
		}
		public string LevelMessage(){
			if(current_level == 1 || level_types[current_level-2] == level_types[current_level-1]){
				return "";
			}
			List<string> messages = new List<string>();
			switch(level_types[current_level-1]){
			case LevelType.Standard:
				messages.Add("You enter a complex of ancient rooms and hallways. ");
				messages.Add("Well-worn corridors suggest that these rooms are frequently used. ");
				messages.Add("You find another network of hallways and rooms. ");
				break;
			case LevelType.Cave:
				messages.Add("You enter a large natural cave. ");
				messages.Add("This cavern's rough walls shine with moisture. ");
				messages.Add("A cave opens up before you. A dry, dusty scent lingers in the ancient tunnels. ");
				break;
			/*case LevelType.Ruined:
				messages.Add("You enter a badly damaged rubble-strewn area of the dungeon. ");
				messages.Add("Broken walls and piles of rubble cover parts of the floor here. ");
				messages.Add("This section of the dungeon has partially collapsed. ");
				break;*/
			case LevelType.Hive:
				messages.Add("You enter an area made up of small chambers. The walls here seem to be made of wax. ");
				messages.Add("As you enter the small chambers here, you hear a faint buzzing. It sounds like insects. ");
				messages.Add("You enter a network of dark and cramped chambers, made entirely of wax. ");
				break;
			case LevelType.Mine:
				messages.Add("You enter a system of mining tunnels. ");
				messages.Add("Mining tools are scattered on the ground here. ");
				messages.Add("You notice half-finished tunnels and mining equipment here. ");
				messages.Add("These tunnels are strewn with mining implements. ");
				break;
			case LevelType.Fortress:
				messages.Add("You pass through a broken gate and enter the remnants of a fortress. ");
				messages.Add("This area looks like it was intended to be a stronghold. ");
				messages.Add("The remains of a fallen fortress appear before you. ");
				break;
			case LevelType.Crypt:
				messages.Add("A sudden wind chills you as you enter a huge underground crypt. ");
				messages.Add("A crypt stretches out in front of you. ");
				messages.Add("Graves appear all around you as you come to a burial area. ");
				break;
			case LevelType.Garden:
				messages.Add("The smell of flowers fills the air here. ");
				messages.Add("Neat rows of plants and small pools of water decorate the rooms here. ");
				messages.Add("You enter a cultivated area. Tiny insects occasionally fly past you. ");
				messages.Add("Curiously, this area seems to be a well-maintained garden. ");
				break;
			/*case LevelType.Extravagant:
				messages.Add("This area is decorated with fine tapestries, marble statues, and other luxuries. ");
				messages.Add("Patterned decorative tiles, fine rugs, and beautifully worked stone greet you upon entering this level. ");
				break;*/
			default:
				messages.Add("What is this strange place? ");
				break;
			}
			if(current_level > 1){
				string transition = TransitionMessage(level_types[current_level-2],level_types[current_level-1]);
				if(transition != ""){
					messages.Add(transition);
				}
			}
			return messages.Random();
		}
		public string TransitionMessage(LevelType from,LevelType to){
			switch(from){
			case LevelType.Standard:
				switch(to){
				case LevelType.Cave:
					return "Rooms and corridors disappear from your surroundings as you reach a large natural cavern. ";
				/*case LevelType.Ruined:
					return "More corridors and rooms appear before you, but many of the walls here are shattered and broken. Rubble covers the floor. ";
				case LevelType.Extravagant:
					return "As you continue, you notice that every corridor is extravagantly decorated and every room is magnificently furnished. ";*/
				case LevelType.Hive:
					return "The rooms get smaller as you continue. A waxy substance appears on some of the walls. ";
				case LevelType.Mine:
					return "As you continue, you notice that the rooms and corridors here seem only partly finished. ";
				case LevelType.Fortress:
					return "You pass through an undefended gate. This area was obviously intended to be secure against intruders. ";
				case LevelType.Crypt:
					return "A hush falls around you as you enter a large crypt. ";
				case LevelType.Garden:
					return "Flowers and statues appear in well-kept rooms as you continue. ";
				}
				break;
			case LevelType.Cave:
				switch(to){
				case LevelType.Standard:
					return "Leaving the cave behind, you again encounter signs of humanoid habitation. ";
				/*case LevelType.Ruined:
					return "The cave leads you to ruined corridors long abandoned by their creators. ";
				case LevelType.Extravagant:
					return "You encounter a beautifully crafted door in the cave wall. It leads to corridors richly decorated with tiles and tapestries. ";*/
				case LevelType.Hive:
					return "The wide-open spaces of the cave disappear, replaced by small chambers that remind you of an insect hive. ";
				case LevelType.Mine:
					return "As you continue, the rough natural edges of the cave are broken up by artificial tunnels. You notice mining tools on the ground. ";
				case LevelType.Fortress:
					return "A smashed set of double doors leads you out of the cave. This area seems to have been well-defended, once. ";
				case LevelType.Crypt:
					return "It appears this part of the cave has been used as a burial ground for centuries. ";
				case LevelType.Garden:
					return "Statues decorate the cave's exit. You follow them to a garden full of flowering plants. ";
				}
				break;
			/*case LevelType.Ruined:
				switch(to){
				case LevelType.Standard:
					return "This part of the dungeon is in much better condition. Rubble no longer covers the floor. ";
				case LevelType.Cave:
					return "You leave ruined rooms behind and enter natural cave tunnels, never touched by picks. ";
				case LevelType.Hive:
					return "It looks like this section was taken over by insects. The rubble has been cleared and used to build small chambers. ";
				case LevelType.Mine:
					return "Rubble still covers the floor here. However, this area isn't ruined - it's still being mined. ";
				case LevelType.Fortress:
					return "You no longer see crumbling walls in this section, but this fortress has clearly fallen into disuse. ";
				case LevelType.Extravagant:
					return "The rubble disappears, replaced by extravagant decorations. Whatever ruined that part of the dungeon didn't affect this area. ";
				}
				break;*/
			case LevelType.Hive:
				switch(to){
				case LevelType.Standard:
					return "The rooms around you begin to look more typical, created by picks instead of by thousands of insects. ";
				case LevelType.Cave:
					return "You leave the cramped chambers behind and enter a wider cave. ";
				/*case LevelType.Ruined:
					return "This area was clearly built by intelligent life, but nature seems to be reclaiming the ruined tunnels. ";
				case LevelType.Extravagant:
					return "Your skin stops crawling as you leave the hives behind and enter a beautifully furnished area. ";*/
				case LevelType.Mine:
					return "Tools on the ground reveal that the rooms here are being made by humanoids rather than insects. ";
				case LevelType.Fortress:
					return "A wide hole in the wall leads to a fortress, abandoned by its creators. ";
				case LevelType.Crypt:
					return "Leaving the narrow chambers, you encounter an ancient crypt. ";
				case LevelType.Garden:
					return "Leaving the hive, you find well-kept paths through rooms full of plant life. ";
				}
				break;
			case LevelType.Mine: //messages about veins, ore, crisscrossing networks of tunnels
				switch(to){
				case LevelType.Standard:
					return "You leave the mines behind and return to finished corridors and rooms. ";
				case LevelType.Cave:
					return "The half-finished tunnels disappear as natural cave walls surround you. ";
				/*case LevelType.Ruined:
					return "This area is collapsing and ruined. It looks much older than the mines you just left. ";
				case LevelType.Extravagant:
					return "As you walk, incomplete tunnels turn into luxurious carpeted hallways. ";*/
				case LevelType.Hive:
					return "As you continue, signs of humanoid construction vanish and hive walls appear. ";
				case LevelType.Fortress:
					return "You reach a section that is not only complete, but easily defensible. ";
				case LevelType.Crypt:
					return "Gravestones appear as you leave the unfinished mines behind you. ";
				case LevelType.Garden:
					return "You leave the narrow mines to find rooms carefully arranged with rows of flowering plants. ";
				}
				break;
			case LevelType.Fortress:
				switch(to){
				case LevelType.Standard:
					return "You enter a section outside the main area of the fortress. ";
				case LevelType.Cave:
					return "You leave the fortress behind. The corridors open up into natural caves. ";
				/*case LevelType.Ruined:
					return "Unlike the fortress, this area has deteriorated immensely. ";
				case LevelType.Extravagant:
					return "As you continue, the military focus of your surroundings is replaced by rich luxury. ";*/
				case LevelType.Hive:
					return "A wide hole in the wall leads to an area filled with small chambers. It reminds you of an insect hive. ";
				case LevelType.Mine:
					return "This section might have been part of the fortress, but pickaxes are still scattered in the unfinished rooms. ";
				case LevelType.Crypt:
					return "Outside the fortress, you come to a gravesite, passing headstones and the occasional statue. ";
				case LevelType.Garden:
					return "Beyond the protection of the fortress you find a garden full of well-maintained statues. ";
				}
				break;
			case LevelType.Crypt:
				switch(to){
				case LevelType.Standard:
					return "You leave the crypt behind and again encounter rooms used by the living. ";
				case LevelType.Cave:
					return "Natural formations appear, replacing the headstones that surrounded you previously. ";
				case LevelType.Hive:
					return "The burial ground vanishes as wax walls appear all around you. ";
				case LevelType.Mine:
					return "Shovels, picks, and rubble appear as you continue. Is this an unfinished part of the crypt? ";
				case LevelType.Fortress:
					return "The tombstones disappear as you come to a crumbling fortress. ";
				case LevelType.Garden:
					return "Life is suddenly all around you once more as you enter a cultivated garden. ";
				}
				break;
			case LevelType.Garden:
				switch(to){
				case LevelType.Standard:
					return "The harmony of the gardens vanishes as the rooms around you become more commonplace. ";
				case LevelType.Cave:
					return "The gardens end abruptly as you enter a natural cave. ";
				case LevelType.Hive:
					return "Insects have conquered this part of the garden, turning it into a giant hive. ";
				case LevelType.Mine:
					return "You leave the orderly gardens behind and enter a twisted network of mining tunnels. ";
				case LevelType.Fortress:
					return "Leaving the gardens through a broken door, you find a fortress in poor condition. ";
				case LevelType.Crypt:
					return "Stale air and decay replaces the scent of flowers. ";
				}
				break;
			/*case LevelType.Extravagant:
				switch(to){
				case LevelType.Standard:
					return "The marvelous luxury vanishes. These rooms look unexciting in comparison. ";
				case LevelType.Cave:
					return "Extravagance is replaced by nature as you enter a large cavern. ";
				case LevelType.Ruined:
					return "The opulence of your surroundings vanishes, replaced by ruined walls and scattered rubble. ";
				case LevelType.Hive:
					return "As you continue, the lavish decorations give way to the waxy walls of an insect hive. ";
				case LevelType.Mine:
					return "You find no comfortable excess of luxury here, just the tools of workers. ";
				case LevelType.Fortress:
					return "You enter what was once a fortress. Your new surroundings trade ornate comfort for spartan efficiency. ";
				}
				break;*/
			}
			return "";
		}
	}
}

