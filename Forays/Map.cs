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
using PosArrays;
using SchismExtensionMethods;
namespace Forays{
	public enum LevelType{Standard,Cave,Hive,Mine,Fortress,Sewer,Garden,Crypt,Hellish,Final};
	public enum AestheticFeature{None,Charred,BloodDarkRed,BloodOther}; //todo: blood colors? smashed barrels? melted wax walls? crushed forasect eggs?
	public class Map{
		public PosArray<Tile> tile = new PosArray<Tile>(ROWS,COLS);
		public PosArray<Actor> actor = new PosArray<Actor>(ROWS,COLS);
		public List<LevelType> level_types;
		public int currentLevelIdx;
		public LevelType CurrentLevelType{ get{ return level_types[currentLevelIdx]; } }
		public int Depth{ //This is the effective level for difficulty calculations etc.
			get{ return currentLevelIdx + 1; }
			set{ currentLevelIdx = value - 1; }
		}
		public bool wiz_lite{get{ return internal_wiz_lite; }
			set{
				internal_wiz_lite = value;
				if(value == true){
					foreach(Tile t in AllTiles()){
						if(t.Is(TileType.BLAST_FUNGUS)){
							t.IgniteBlastFungus();
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
							t.IgniteBlastFungus();
						}
					}
				}
			}
		}
		private bool internal_wiz_dark;
		private Dict<ActorType,int> generated_this_level = new Dict<ActorType,int>(); //used for rejecting monsters if too many already exist on the current level
		private PosArray<int> monster_density = new PosArray<int>(ROWS,COLS);
		private bool[,] danger_sensed;
		private static List<pos> allpositions = new List<pos>();
		public PosArray<int> safetymap = null;
		public PosArray<int> poppy_distance_map = null;
		public PosArray<int> travel_map = null;
		public PosArray<AestheticFeature> aesthetics = null;
		public string dungeonDescription = "";
		//public int[,] row_displacement = null;
		//public int[,] col_displacement = null;
		public colorchar[,] last_seen = new colorchar[ROWS,COLS];
		public int[] final_level_cultist_count = new int[5];
		public int final_level_demon_count = 0;
		public int final_level_clock = 0;
		public bool feat_gained_this_level = false;
		public int extra_danger = 0; //used to eventually spawn more threatening wandering monsters
		public List<CellType> nextLevelShrines = null;
		public int[] shrinesFound = null;
		public bool currentlyGeneratingLevel = false;

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
			currentLevelIdx = -1;
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
			bool hellish_placed = false;
			while(level_types.Count < 20){
				int num = R.Roll(2,2) - 1;
				if(current == LevelType.Standard) num++;
				for(int i=0;i<num;++i){
					if(level_types.Count < 20){
						level_types.Add(current);
					}
				}
				if(level_types.Count >= 15 && !hellish_placed){
					level_types.Add(LevelType.Hellish);
					hellish_placed = true;
				}
				current = ChooseNextLevelType(current);
			}
			level_types.Add(LevelType.Final);
		}
		private LevelType ChooseNextLevelType(LevelType current){
			LevelType result = current;
			while(result == current){
				result = R.WeightedChoice(
					new int[]{40,20,5,18,12,7,5,12},
					new LevelType[]{LevelType.Standard,LevelType.Cave,LevelType.Hive,LevelType.Mine,LevelType.Fortress,LevelType.Sewer,LevelType.Garden,LevelType.Crypt}); //hellish is handled elsewhere.
			}
			return result;
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
		public void CalculatePoppyDistanceMap(){
			poppy_distance_map = tile.GetDijkstraMap(x=>tile[x].passable && !tile[x].Is(TileType.POPPY_FIELD),x=>!tile[x].Is(TileType.POPPY_FIELD));
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
		public void Draw(){ //Draw should be faster than Redraw when most of the screen is unchanged.
			if(Screen.MapChar(0,0).c == '-' && !Global.GRAPHICAL){ //kinda hacky. there won't be an open door in the corner, so this looks for
				Redraw(); //evidence of Select being called (& therefore, the map needing to be redrawn entirely) //todo! this breaks in console mode if you have the option on.
			}
			else{
				MouseUI.mouselook_objects = new PhysicalObject[Global.SCREEN_H,Global.SCREEN_W];
				UI.sidebar_objects = new List<PhysicalObject>();
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
				UI.SortStatusBarObjects();
				Screen.ResetColors();
				Screen.NoGLUpdate = false;
				Screen.GLUpdate();
			}
		}
		public void Redraw(){ //Redraw should be faster than Draw when most of the screen has changed.
			MouseUI.mouselook_objects = new PhysicalObject[Global.SCREEN_H,Global.SCREEN_W];
			UI.sidebar_objects = new List<PhysicalObject>();
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
						ch.color = Colors.ResolveColor(ch.color);
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
			UI.SortStatusBarObjects();
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
						ch.color = Colors.darkcolor;
					}
					last_seen[r,c] = ch;
					MouseUI.mouselook_objects[r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS] = tile[r,c].inv;
					UI.sidebar_objects.Add(tile[r,c].inv);
					tile[r,c].UpdateStatusBarWithTile();
					tile[r,c].UpdateStatusBarWithFeatures();
				}
				else{
					if(tile[r,c].features.Count > 0){
						ch = tile[r,c].FeatureVisual();
						last_seen[r,c] = ch;
						tile[r,c].UpdateStatusBarWithTile();
						tile[r,c].UpdateStatusBarWithFeatures();
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
								ch.color = Colors.darkcolor;
								last_seen[r,c] = ch;
							}
							else{
								last_seen[r,c] = ch;
								ch.color = Colors.darkcolor;
							}
						}
						else{
							last_seen[r,c] = ch;
						}
						tile[r,c].UpdateStatusBarWithTile();
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
						MouseUI.mouselook_objects[r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS] = actor[r,c];
						UI.sidebar_objects.Add(actor[r,c]);
					}
					if(actor[r,c] == player){
						if(player.HasFeat(FeatType.DANGER_SENSE) && danger_sensed != null && danger_sensed[r,c]
							&& player.LightRadius() == 0 && !wiz_lite){
							if(tile[r,c].IsLit() && !player.HasAttr(AttrType.BLIND)){
								ch.color = Color.Red;
							}
							else{
								ch.color = Color.DarkRed;
							}
						}
						else{
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
										if(ch.color != Color.DarkGray){ //if it's dark gray at this point, it means you're invisible.
											ch.color = Colors.darkcolor;
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
						MouseUI.mouselook_objects[r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS] = actor[r,c];
						UI.sidebar_objects.Add(actor[r,c]);
					}
				}
				else{
					if(tile[r,c].seen){
						ch.c = last_seen[r,c].c;
						ch.color = Colors.unseencolor;
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
		public pos GetRandomPosition(LevelType levelType,bool minDist2FromBorder = false){
			if(levelType == LevelType.Hellish){
				if(minDist2FromBorder){
					return new pos(R.Roll(ROWS-4)+1,R.Between(19,COLS-9));
				}
				return new pos(R.Roll(ROWS-2),R.Between(18,COLS-8)); // don't return the leftmost or rightmost part of hellish levels.
			}
			if(minDist2FromBorder){
				return new pos(R.Roll(ROWS-4)+1,R.Roll(COLS-4)+1);
			}
			return tile.RandomPosition(false);
		}
		public Item SpawnItem(){
			ConsumableType result = Item.RandomItem();
			for(bool done=false;!done;){
				pos rp = GetRandomPosition(CurrentLevelType);
				int rr = rp.row;
				int rc = rp.col;
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
		public ActorType ChooseMobType(bool wanderingOnly,bool shallowOnly){
			ActorType result = ActorType.SPECIAL;
			int effectiveDepth = (Depth + extra_danger).Clamp(1,20);
			int baseMonsterTier = (effectiveDepth+1) / 2; // monster tiers are 1-10.
			while(true){
				int monsterTier = 1;
				if(effectiveDepth > 1){ // depth 1 only generates tier 1 monsters.
					List<int> tiers = new List<int>();
					if(shallowOnly){ // shallow mobs are the ones that can normally no longer appear at the current depth.
						if(R.OneIn(baseMonsterTier-3)){ // tier 1 is the only possibility for shallow monsters at base tier 4, but becomes less likely on deeper levels.
							tiers.Add(1);
						}
						for(int i=2;i<baseMonsterTier-2;++i){ // the rest of the shallow tiers are always considered.
							tiers.Add(i);
						}
					}
					else{
						for(int i=-2;i<=2;++i){
							if(baseMonsterTier + i >= 1 && baseMonsterTier + i <= 10){
								int j = 1 + Math.Abs(i);
								if(R.OneIn(j)){ // current depth is considered 1 out of 1 times, depth+1 and depth-1 are considered 1 out of 2 times, etc.
									tiers.Add(baseMonsterTier + i);
								}
							}
						}
					}
					if(tiers.Count > 0) monsterTier = tiers.Random();
				}
				LevelType lt = CurrentLevelType;
				if(lt == LevelType.Hellish && R.CoinFlip()){ // these get chosen quite a lot, but quickly start being rejected because there are too many, so it tends to balance out.
					if(shallowOnly){
						result = ActorType.MINOR_DEMON;
					}
					else{
						result = ActorType.SPECIAL;
					}
				}
				else{
					if(!shallowOnly && R.OneIn(10) && (lt == LevelType.Cave || lt == LevelType.Crypt || lt == LevelType.Hive || lt == LevelType.Mine || lt == LevelType.Sewer)){
						result = ActorType.SPECIAL; // zombies in crypts, kobolds in mines, etc.
					}
					else{
						if(monsterTier == 1){ //level 1 monsters are all equal in rarity
							result = (ActorType)R.Between(3,9);
						}
						else{
							int roll = R.Roll(100);
							if(roll <= 3){ //3% rare
								result = (ActorType)(monsterTier*7 + 2);
							}
							else{
								if(roll <= 22){ //19% uncommon (9.5% each)
									result = (ActorType)(monsterTier*7 + R.Between(0,1));
								}
								else{ //78% common (19.5% each)
									result = (ActorType)(monsterTier*7 + R.Between(-4,-1));
								}
							}
						}
					}
				}
				if(wanderingOnly){ // now that we have a type, we check to see if it's valid.
					if(!Actor.Prototype(result).HasAttr(AttrType.IMMOBILE) && result != ActorType.MIMIC && result != ActorType.MARBLE_HORROR && result != ActorType.POLTERGEIST){
						return result;
					}
				}
				else{
					if(R.OneIn(generated_this_level[result]+1)){ // 1 in 2 for the 2nd, 1 in 3 for the 3rd, and so on.
						generated_this_level[result]++;
						return result;
					}
				}
			}
		}
		private void UpdateDensity(PhysicalObject obj){ UpdateDensity(obj.p); }
		private void UpdateDensity(pos position){
			foreach(pos p in position.PositionsWithinDistance(8,monster_density)){
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
		public List<ActorType> GetMobTypes(ActorType type){
			List<ActorType> result = new List<ActorType>();
			int number = 1;
			if(type == ActorType.SPECIAL){
				if(CurrentLevelType == LevelType.Hellish){
					if(R.OneIn(3)){
						for(int i=0;i<3;++i) result.Add(ActorType.MINOR_DEMON);
					}
					else{
						if(R.CoinFlip()){
							result.Add(ActorType.FROST_DEMON);
						}
						else{
							result.Add(ActorType.BEAST_DEMON);
						}
					}
				}
				else{
					number = 1 + (Depth-3) / 4; //was number = 1 + (Depth-3) / 3;
					if(number > 1 && R.CoinFlip()){
						--number;
					}
					for(int i=0;i<number;++i){
						switch(CurrentLevelType){
						case LevelType.Cave:
						if(R.CoinFlip()){
							result.Add(ActorType.GOBLIN);
						}
						else{
							if(R.CoinFlip()){
								result.Add(ActorType.GOBLIN_ARCHER);
							}
							else{
								result.Add(ActorType.GOBLIN_SHAMAN);
							}
						}
						break;
						case LevelType.Crypt:
						result.Add(ActorType.ZOMBIE);
						break;
						case LevelType.Hive:
						result.Add(ActorType.FORASECT);
						break;
						case LevelType.Mine:
						result.Add(ActorType.KOBOLD);
						break;
						case LevelType.Sewer:
						result.Add(ActorType.GIANT_SLUG);
						break;
						}
					}
				}
			}
			else{
				if(Actor.Prototype(type).HasAttr(AttrType.SMALL_GROUP)){
					number = R.Roll(2)+1;
				}
				if(Actor.Prototype(type).HasAttr(AttrType.MEDIUM_GROUP)){
					number = R.Roll(2)+2;
				}
				if(Actor.Prototype(type).HasAttr(AttrType.LARGE_GROUP)){
					number = R.Roll(2)+4;
				}
				if(type == ActorType.CULTIST && CurrentLevelType == LevelType.Final){
					number = 0;
					for(int i=0;i<5;++i){
						if(FinalLevelSummoningCircle(i).PositionsWithinDistance(2,tile).Any(x=>tile[x].Is(TileType.DEMONIC_IDOL))){
							number++;
						}
					}
				}
				for(int i=0;i<number;++i) result.Add(type);
			}
			return result;
		}
		public Actor SpawnMob(){ return SpawnMob(ChooseMobType(false,false)); }
		public Actor SpawnMob(ActorType type){
			Actor result = null;
			if(type == ActorType.POLTERGEIST){
				for(int tries=0;tries<1000;++tries){
					pos rp = GetRandomPosition(CurrentLevelType,true);
					int rr = rp.row;
					int rc = rp.col;
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
					pos rp = GetRandomPosition(CurrentLevelType);
					int rr = rp.row;
					int rc = rp.col;
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
				Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).RandomOrDefault();
				if(statue != null){
					Q.Add(new Event(statue,100,EventType.MARBLE_HORROR));
				}
				return null;
			}
			if(type == ActorType.NOXIOUS_WORM){
				//get a dijkstra map with nonwalls as origins. we're looking for distance 2+.
				var dijkstra = tile.GetDijkstraMap(x=>!tile[x].Is(TileType.WALL),x=>false);
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
					Tile dest = burrow.RandomOrDefault();
					return Actor.Create(type,dest.row,dest.col);
				}
			}
			List<ActorType> mobTypes = GetMobTypes(type);
			List<Tile> group_tiles = new List<Tile>();
			List<Actor> group = null;
			if(mobTypes.Count > 1){
				group = new List<Actor>();
			}
			for(int i=0;i<mobTypes.Count;++i){
				ActorType final_type = mobTypes[i];
				if(i == 0){
					int density_target_number = 2;
					for(int j=0;j<2000;++j){
						pos rp = GetRandomPosition(CurrentLevelType);
						int rr = rp.row;
						int rc = rp.col;
						bool good = true;
						if(tile[rr,rc].IsTrap()){
							good = false;
						}
						if(tile[rr,rc].Is(TileType.POPPY_FIELD) && !Actor.Prototype(final_type).HasAttr(AttrType.NONLIVING,AttrType.MENTAL_IMMUNITY)){
							good = false;
						}
						if(CurrentLevelType == LevelType.Final){
							foreach(Tile t in tile[rr,rc].TilesWithinDistance(2)){
								if(tile[rr,rc].HasLOE(t) && player.CanSee(t)){
									good = false;
									break;
								}
							}
						}
						else{
							if(CurrentLevelType != LevelType.Final && monster_density[rr,rc] >= density_target_number){
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
							if(mobTypes.Count > 1){
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
								if(CurrentLevelType != LevelType.Final){
									UpdateDensity(group[0]);
								}
								return group[0];
							}
							else{
								if(result != null && CurrentLevelType != LevelType.Final){
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
			if(mobTypes.Count > 1){
				if(CurrentLevelType != LevelType.Final){
					UpdateDensity(group[0]);
				}
				return group[0];
			}
			else{
				if(result != null && CurrentLevelType != LevelType.Final){
					UpdateDensity(result);
				}
				return result;
			}
		}
		public Actor SpawnWanderingMob(){
			ActorType type = ChooseMobType(true,false);
			Actor result = null;
			List<ActorType> mobTypes = GetMobTypes(type);
			List<Tile> group_tiles = new List<Tile>();
			List<Actor> group = null;
			if(mobTypes.Count > 1){
				group = new List<Actor>();
			}
			var dijkstra = tile.GetDijkstraMap(x=>player.HasLOS(tile[x]) || player.HasLOE(tile[x]),x=>!tile[x].passable);
			for(int i=0;i<mobTypes.Count;++i){
				ActorType final_type = mobTypes[i];
				if(i == 0){
					for(int j=0;j<1999;++j){
						pos rp = GetRandomPosition(CurrentLevelType);
						int rr = rp.row;
						int rc = rp.col;
						if(!tile[rr,rc].IsTrap() && tile[rr,rc].passable && actor[rr,rc] == null && dijkstra[rr,rc] >= 6){
							result = Actor.Create(final_type,rr,rc);
							if(mobTypes.Count > 1){
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
			if(mobTypes.Count > 1){
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
						U.DefaultMetric = DistanceMetric.Manhattan;
						var dijk = d.map.GetDijkstraMap(x=>!d.map[x].IsWall(),x=>false);
						for(int i=1;i<ROWS-1;++i){
							for(int j=1;j<COLS-1;++j){
								U.DefaultMetric = DistanceMetric.Manhattan;
								if(dijk[i,j] == 1){
									pos p = new pos(i,j);
									List<pos> floors = null;
									foreach(int dir in U.FourDirections){
										pos n = p.PosInDir(dir);
										if(dijk[n] == 1){
											if(floors == null){
												floors = p.PositionsAtDistance(1,dijk).Where(x=>dijk[x] == 0);
											}
											List<pos> floors2 = new List<pos>();
											foreach(pos n2 in n.PositionsAtDistance(1,dijk)){
												if(dijk[n2] == 0 && !floors.Contains(n2)){
													floors2.Add(n2);
												}
											}
											if(floors.Count > 0 && floors2.Count > 0 && R.OneIn(5)){ //IIRC this checks each pair twice, so that affects the chance here
												pos f1 = floors.Random();
												pos f2 = floors2.Random();
												U.DefaultMetric = DistanceMetric.Chebyshev;
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
							U.DefaultMetric = DistanceMetric.Chebyshev;
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
						List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck(tile) && d.map[y].IsFloor()));
						while(!d.IsFullyConnected() && thin_walls.Count > 0){
							pos p = thin_walls.Random();
							d.map[p] = CellType.CorridorIntersection;
							foreach(pos neighbor in p.PositionsWithinDistance(1,d.map)){
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
			{
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
					List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck(tile) && d.map[y].IsFloor()));
					while(!d.IsFullyConnected() && thin_walls.Count > 0){
						pos p = thin_walls.Random();
						d.map[p] = CellType.CorridorIntersection;
						foreach(pos neighbor in p.PositionsWithinDistance(2,d.map)){
							thin_walls.Remove(neighbor);
						}
					}
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors();
					d.RemoveUnconnectedAreas();
					d.MarkInterestingLocations();
					//to find rooms big enough for stuff in the center:
				//var dijkstra = d.map.GetDijkstraMap(x=>d.map[x].IsWall(),d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasAdjacentWhere(y=>d.map.BoundsCheck(y) && !d.map[y].IsWall())));
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
			case LevelType.Sewer:
			{
				while(true){
					d.RoomHeightMax = 20;
					d.RoomHeightMin = 14;
					d.RoomWidthMax = 16;
					d.RoomWidthMin = 8;
					pos big_room = new pos();
					while(true){
						if(d.CreateRoom()){
							d.ForEachRectangularRoom((start_r, start_c, end_r, end_c) => {
								big_room = new pos(start_r + 3,start_c + 3);
								//d.MoveRoom(new pos(start_r,start_c),R.CoinFlip()? 4 : 6);
								return true;
							});
							d.MakeRoomsElliptical(100);
							for(int i=0;i<ROWS;++i){
								for(int j=0;j<COLS;++j){
									if(d[i,j].IsRoomType() && new pos(i,j).ConsecutiveAdjacent(x=>!d[x].IsRoomType()) >= 4){
										d[i,j] = CellType.RoomCorner;
									}
								}
							}
							break;
						}
					}
					d.RoomHeightMax = 5;
					d.RoomHeightMin = 4;
					d.RoomWidthMax = 5;
					d.RoomWidthMin = 4;
					d.CreateBasicMap();
					d.MakeRoomsElliptical(85);
					d.RoomHeightMax = 3;
					d.RoomHeightMin = 3;
					d.RoomWidthMax = 3;
					d.RoomWidthMin = 3;
					for(int i=0;i<20;++i){
						d.CreateRoom();
					}
					d.AttemptToConnectAllRooms();
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors();
					d.RemoveUnconnectedAreas();
					if(!d[big_room].IsPassable() || d.NumberOfFloors() < 420 || d.HasLargeUnusedSpaces(300)){ //todo: req too high?
						d.Clear();
					}
					else{
						bool central_pillar = R.CoinFlip();
						var wall_distance = d.map.GetDijkstraMap(x=>d[x] == CellType.Wall,x=>false);
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(wall_distance[i,j] > 1){
									if(central_pillar && wall_distance[i,j] > 4){
										d[i,j] = CellType.Pillar;
									}
									else{
										d[i,j] = CellType.ShallowWater;
									}
								}
							}
						}
						var water_distance_8way = d.map.GetDijkstraMap(x=>d[x] == CellType.ShallowWater,x=>!d[x].IsPassable());
						U.DefaultMetric = DistanceMetric.Manhattan;
						var water_distance_4way = d.map.GetDijkstraMap(x=>d[x] == CellType.ShallowWater,x=>!d[x].IsPassable());
						U.DefaultMetric = DistanceMetric.Chebyshev;
						var noise = U.GetNoise(ROWS,COLS);
						float total = 0.0f;
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								total += noise[i,j];
							}
						}
						float avg = total / (float)(ROWS*COLS);
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(water_distance_8way[i,j] == 0){ //water
									if(noise[i,j] - avg < -0.3f){ //very dry, turns to floor
										d[i,j] = CellType.RoomInterior;
									}
									else{
										if(noise[i,j] - avg < -0.11f){ //dry, turns to slime
											d[i,j] = CellType.Slime;
										}
									}
								}
								else{
									if(d[i,j].IsPassable()){ //passable but not water
										if(water_distance_4way[i,j] == 1 && R.OneIn(5) && noise[i,j] > -0.1){ //floor near water - not dry, turns to water
											d[i,j] = CellType.ShallowWater;
										}
										else{
											if(water_distance_8way[i,j] == 1 && noise[i,j] - avg > 0.1f){ //floor near water - wet, turns to water
												d[i,j] = CellType.ShallowWater;
											}
											else{
												if(noise[i,j] - avg > 0.18f){ //floor anywhere - very wet, turns to water
													d[i,j] = CellType.ShallowWater;
												}
											}
										}
									}
								}
							}
						}
						d.MarkInterestingLocations();
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			}
			case LevelType.Garden:
			{
				d.RoomHeightMin = 4;
				d.RoomHeightMax = 10;
				d.RoomWidthMin = 4;
				d.RoomWidthMax = 10;
				while(true){
					d.CreateBasicMap();
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.RemoveDeadEndCorridors();
					var dijkstra = d.map.GetDijkstraMap(x=>d[x].IsPassable(),x=>false);
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
							foreach(pos neighbor in p.PositionsWithinDistance(1,d.map)){
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
							if(!new pos(start_r+1,start_c).PositionsAtDistance(1,d.map).Any(x=>d[x].IsCorridorType())){
								water.Add(new pos(start_r+1,start_c));
								water.Add(new pos(start_r+2,start_c));
							}
							if(!new pos(start_r,start_c+1).PositionsAtDistance(1,d.map).Any(x=>d[x].IsCorridorType())){
								water.Add(new pos(start_r,start_c+1));
								water.Add(new pos(start_r,start_c+2));
							}
							if(!new pos(end_r-1,end_c).PositionsAtDistance(1,d.map).Any(x=>d[x].IsCorridorType())){
								water.Add(new pos(end_r-1,end_c));
								water.Add(new pos(end_r-2,end_c));
							}
							if(!new pos(end_r,end_c-1).PositionsAtDistance(1,d.map).Any(x=>d[x].IsCorridorType())){
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
					var dijkstra = d.map.GetDijkstraMap(x=>d.map[x] == CellType.Wall,x=>false); //todo: among these Map dijkstra maps I have, like, 3 different ways of testing for walls. are these all correct?
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
						foreach(pos neighbor in p.PositionsAtDistance(1,d.map)){
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
					dijkstra = d.map.GetDijkstraMap(x=>d.map[x] == CellType.Wall,x=>false);
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
						List<pos> tombstones = offsets.WhereGreatest(x=>x.Count).RandomOrDefault();
						if(tombstones != null){
							foreach(pos p in tombstones){
								d.map[p] = CellType.Tombstone;
							}
						}
						return true;
					});
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(d[i,j] == CellType.Door){
								pos p = new pos(i,j);
								List<pos> potential_statues = p.PositionsAtDistance(1,d.map).Where(x=>!d[x].IsWall() && !central_room.Contains(x) && p.DirectionOf(x) % 2 != 0 && !x.PositionsAtDistance(1,d.map).Any(y=>d[y].Is(CellType.Tombstone)));
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
									foreach(pos neighbor in p.PositionsAtDistance(1,d2.map)){
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
			case LevelType.Hellish:
			{
				while(true){
					const int H2 = 20;
					const int W2 = 41;
					Dungeon d2 = new Dungeon(H2,W2);
					d2.BSPFill(3,3,1,1,true);
					PosArray<int> adjWalls = new PosArray<int>(H2,W2);
					for(int i=0;i<H2;++i){
						for(int j=0;j<W2;++j){
							pos p = new pos(i,j);
							if(d2[p] == CellType.Wall){
								foreach(int dir in U.FourDirections){
									if(d2.BoundsCheck(p.PosInDir(dir),true)){
										adjWalls[p.PosInDir(dir)]++;
									}
								}
							}
						}
					}
					List<List<pos>> separatingWalls = new List<List<pos>>();
					Dictionary<List<pos>,int> wallValue = new Dictionary<List<pos>,int>();
					PosArray<bool> visited = new PosArray<bool>(H2,W2);
					for(int i=0;i<H2;++i){
						for(int j=0;j<W2;++j){
							pos p = new pos(i,j);
							if(!visited[p] && d2[p] == CellType.Wall && adjWalls[p] < 3){
								U.DefaultMetric = DistanceMetric.Manhattan;
								var ff = d2.map.GetFloodFillPositions(p,false,x=>d2[x] == CellType.Wall && adjWalls[x] < 3);
								U.DefaultMetric = DistanceMetric.Chebyshev;
								if(ff.Count > 0){
									separatingWalls.Add(ff);
									wallValue.Add(ff,R.Roll(ff.Count));
									//wallValue.Add(ff,ff.Count);
								}
								foreach(pos ffp in ff){
									visited[ffp] = true;
								}
							}
						}
					}
					separatingWalls.Randomize().Sort((list1,list2)=>-(wallValue[list1].CompareTo(wallValue[list2]))); //shorter walls will tend to be near the end of the list & be considered first.
					PosArray<int> rooms = new PosArray<int>(H2,W2);
					int next = 1;
					for(int i=0;i<H2;++i){
						for(int j=0;j<W2;++j){
							if(d2[i,j] != CellType.Wall && rooms[i,j] == 0){
								var ff = d2.map.GetFloodFillPositions(new pos(i,j),false,x=>d2[x] != CellType.Wall);
								foreach(pos ffp in ff){
									rooms[ffp] = next;
								}
								++next;
							}
						}
					} //at this point, 'next' is 1 greater than the number of rooms. X rooms require X-1 connections, so (next-2) connections must be made:
					Equivalizer<int> roomConnections = new Equivalizer<int>();
					while(next > 2){
						List<pos> wall = separatingWalls.RemoveLast();
						pos p = wall[0];
						int firstRoom = -1;
						foreach(int dir in U.FourDirections){
							pos neighbor = p.PosInDir(dir);
							if(d2.map.BoundsCheck(neighbor,true) && d2[neighbor] != CellType.Wall){
								if(firstRoom == -1){
									firstRoom = rooms[neighbor];
								}
								else{
									if(!roomConnections.AreEquivalent(firstRoom,rooms[neighbor])){
										foreach(pos wallp in wall){
											d2[wallp] = CellType.RoomInterior;
										}
										roomConnections.Join(firstRoom,rooms[neighbor]);
									}
									else{
										++next; //if the 2 rooms are already connected, this doesn't count; keep going.
									}
									break;
								}
							}
						}
						--next;
					}
					ReadSpecialLevel(d.map,HellishLevelLayout(),null,null);
					for(int i=0;i<H2;++i){
						for(int j=0;j<W2;++j){
							d[i+1,j+18] = d2[i,j];
						}
					}
					const int stairRoomHeight = 3;
					const int stairRoomWidth = 6;
					const int stairRoomDistanceFromEdge = 7;
					int stairRoomRow = R.Between(1,ROWS-1-stairRoomHeight);
					for(int i=stairRoomRow;i<stairRoomRow+3;++i){
						for(int j=COLS-stairRoomDistanceFromEdge;j<COLS-stairRoomDistanceFromEdge+stairRoomWidth;++j){
							d[i,j] = CellType.RoomInterior;
						}
					}
					pos stairPos = new pos(stairRoomRow+1,COLS-stairRoomDistanceFromEdge+3);
					d[stairPos.row,stairPos.col-1] = CellType.RoomFeature1;
					d[stairPos] = CellType.Stairs;
					d[stairPos.row,stairPos.col+1] = CellType.RoomFeature1;
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors();
					d.RemoveUnconnectedAreas();
					d.AddDoors(40);
					var stairDist = d.map.GetDijkstraMap(new List<pos>{stairPos},x=>d[x].IsWall());
					List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck(tile) && d.map[y].IsFloor()));
					foreach(pos p in thin_walls){
						if(R.OneIn(10)){
							foreach(var pair in U.FourDirectionPairs){
								pos p1 = p.PosInDir(pair[0]);
								pos p2 = p.PosInDir(pair[1]);
								if(d[p1].IsPassable() && d[p2].IsPassable()){
									if(stairDist[p1] > 32 && stairDist[p2] > 32){
										if(d.map.PathingDistanceFrom(p1,p2,x=>d[x].IsWall()) > 15){
											d[p] = CellType.Door;
										}
									}
									break;
								}
							}
						}
					}
					d.MarkInterestingLocations();
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<18;++j){ //don't put anything at the first part of the level.
							if(d[i,j] == CellType.InterestingLocation) d[i,j] = CellType.RoomInterior;
						}
					}
					if(d.NumberOfFloors() < 350 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						List<pos> bloodSplatters = new List<pos>();
						int numSplatters = R.Between(3,5);
						while(bloodSplatters.Count < numSplatters){
							pos rp = GetRandomPosition(LevelType.Hellish);
							if(d[rp].IsPassable()) bloodSplatters.Add(rp);
						}
						var bloodMap = d.map.GetDijkstraMap(bloodSplatters,x=>d[x].IsWall());
						List<pos> demonstone = new List<pos>();
						int numDemonstone = R.Between(0,2);
						while(demonstone.Count < numDemonstone){
							pos rp = GetRandomPosition(LevelType.Hellish);
							if(d[rp].IsPassable()) demonstone.Add(rp);
						}
						U.DefaultMetric = DistanceMetric.Manhattan;
						var demonstoneMap = d.map.GetDijkstraMap(demonstone,x=>d[x].IsWall());
						U.DefaultMetric = DistanceMetric.Chebyshev;
						int numBarrels = R.Between(2,5);
						if(R.OneIn(20)) numBarrels += R.Roll(10);
						int placed = 0;
						int failed = 0;
						while(placed < numBarrels){
							pos rp = GetRandomPosition(LevelType.Hellish);
							int total = 0;
							foreach(pos neighbor in rp.CardinalAdjacentPositions()){
								if(d[neighbor].IsFloor()) ++total;
							}
							if(total >= 3){
								d[rp] = CellType.Barrel;
								placed++;
								failed = 0;
							}
							else{
								if(++failed >= 50){
									numBarrels--;
									failed = 0;
								}
							}
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(d[i,j] == CellType.RoomInterior){
									if(demonstoneMap[i,j] <= R.Between(2,3)){
										d[i,j] = CellType.RoomFeature5;
									}
									else{
										if(bloodMap[i,j] <= 2 && R.OneIn(2)){
											d[i,j] = CellType.CorridorFeature1;
										}
									}
								}
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
		private void ReadSpecialLevel(PosArray<CellType> map,string[] layout,PosArray<bool> doors,List<List<pos>> doorSets){
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					switch(layout[i][j]){
					case '#':
					map[i,j] = CellType.Wall;
					break;
					case '.':
					map[i,j] = CellType.RoomInterior;
					break;
					case '5':
					map[i,j] = CellType.Statue;
					break;
					case '2':
					map[i,j] = CellType.RoomFeature1;
					break;
					case '*':
					map[i,j] = CellType.RoomFeature2;
					break;
					case '^':
					map[i,j] = CellType.RoomFeature3;
					break;
					case '&':
					map[i,j] = CellType.RoomFeature4;
					break;
					case '~':
					map[i,j] = CellType.RoomFeature5;
					break;
					case '!':
					map[i,j] = CellType.CorridorFeature1;
					break;
					case 'X':
					map[i,j] = CellType.CorridorFeature2;
					break;
					case '+':
					{
						map[i,j] = CellType.Wall;
						if(!doors[i,j]){
							doors[i,j] = true;
							pos p = new pos(i,j);
							List<pos> door_set = new List<pos>{p};
							foreach(int dir in new int[]{2,6}){
								p = new pos(i,j);
								while(true){
									p = p.PosInDir(dir);
									if(p.BoundsCheck(tile) && layout[p.row][p.col] == '+'){
										doors[p] = true;
										door_set.Add(p);
									}
									else{
										break;
									}
								}
							}
							doorSets.Add(door_set);
						}
						break;
					}
					default:
					map[i,j] = CellType.RoomInterior;
					break;
					}
				}
			}
		}
		private string GetDungeonDescription(){
			if(R.OneIn(2000)){
				return R.Choose("You sense a certain tension.");
			}
			switch(CurrentLevelType){
			case LevelType.Standard:
			return R.Choose("This place stinks of mildew.",
				"Moss grows through every crack in the stones here.",
				"A draft with no obvious source lends a chill to the air here.",
				"Stagnant humidity clings to your skin.",
				"Cold drops of water fall occasionally from the ceiling.",
				"The air here is stale. Dust resettles quickly after each footfall.",
				"Truly ancient glyphs decorate the walls, now barely discernible.",
				"Gloom and dusty cobwebs obscure the dim corners above.",
				"The shoddy stonework here is crumbling.",
				"Vaulted ceilings create a cavernous echo.",
				"The stones here have been worn smooth by time."
			);
			case LevelType.Cave:
			if(R.OneIn(2000)){
				return "The pungent stench of mildew emanates from the wet dungeon walls.";
			}
			return R.Choose("Enormous stalactites hang from the cave ceiling.",
				"Wide patches of gray lichen cling to the cave walls.",
				"The steady plink of dripping water reaches your ears.",
				"The smooth cave floor slopes gently.",
				"An echo follows every sound in this cavernous space."
			);
			case LevelType.Crypt:
			return R.Choose("A layer of gritty dust covers the ground.",
				"An unnatural quiet pervades this place.",
				"Faint whispers seem to follow you in this haunted place.",
				"The air in this gloomy place is musty and still.",
				"A strange chill accompanies you."
			);
			case LevelType.Fortress:
			return R.Choose("Warped wooden supports bulge awkwardly from the walls.",
				"Soot and cinders attest to this ruined fortress's fate.",
				"Arrow slits are spaced regularly around the walls.",
				"Bones of the stronghold's former denizens lie scattered."
			);
			case LevelType.Garden:
			return R.Choose("A sweet fragrance fills the air.",
				"Ornate tapestries hang from every wall.",
				"A tidy pathway winds between the rooms here."
			);
			case LevelType.Hive:
			return R.Choose("A constant droning sound reverberates throughout the hive.",
				"The walls and floor vibrate slightly to the touch.",
				"Hardened wax forms a dome overhead."
			);
			case LevelType.Mine:
			return R.Choose("Exposed veins of a dull metal stretch along the walls.",
				"Tiny hexagonal crystals sprout in clusters from the walls.",
				"Twisting dead-end tunnels extend in all directions."
				//todo: need more
			);
			case LevelType.Sewer:
			return R.Choose("Squeaking vermin dart here and there, avoiding your presence.",
				"Nauseating mold grows thick on every surface.",
				"Filth and stench seep from every crack in the stones here."
				//"You hear the burble of flowing water.",
				//__456789012345678901234567890123456789012345678901234567890123456
			);
			case LevelType.Hellish:
			return R.Choose("Blood has been smeared onto the walls in intricate patterns.");
			case LevelType.Final:
			return R.Choose("What is this hellish place?");
			default:
			return "";
			}
		}
		private void InitializeNewLevel(){
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
						if(tile[i,j].IsShrine() && shrinesFound[(int)(tile[i,j].type.GetAssociatedSkill())] < 2){
							shrinesFound[(int)(tile[i,j].type.GetAssociatedSkill())] = 2;
						}
						tile[i,j].inv = null;
					}
					tile[i,j] = null;
				}
			}
			wiz_lite = false;
			wiz_dark = false;
			if(Depth % 2 == 1){
				feat_gained_this_level = false;
				shrinesFound = new int[5];
			}
			generated_this_level = new Dict<ActorType, int>();
			monster_density = new PosArray<int>(ROWS,COLS);
			aesthetics = new PosArray<AestheticFeature>(ROWS,COLS);
			extra_danger = 0;
			safetymap = null;
			travel_map = null;
			Q.ResetForNewLevel();
			last_seen = new colorchar[ROWS,COLS];
			Fire.fire_event = null;
			Fire.burning_objects.Clear();
			if(player.IsBurning()){
				Fire.AddBurningObject(player);
			}
			Actor.tiebreakers = new List<Actor>{player};
			Actor.interrupted_path = new pos(-1,-1);
		}
		public void GenerateLevel(){
			currentlyGeneratingLevel = true;
			if(Depth < 20){
				++currentLevelIdx;
			}
			InitializeNewLevel();
			PosArray<CellType> map = GenerateMap(CurrentLevelType);
			List<pos> interesting_tiles = new List<pos>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(map[i,j] == CellType.InterestingLocation){
						interesting_tiles.Add(new pos(i,j));
					}
				}
			}
			List<CellType> shrines = null;
			if(Depth % 2 == 1){
				shrines = new List<CellType>{CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5};
				shrines.Randomize(); // after this initialization step, shrines are only removed from the front of the list, to enable prediction of pairs.
				nextLevelShrines = new List<CellType>();
				int n = R.Between(0,5);
				for(int i=0;i<n;++i){
					nextLevelShrines.Add(shrines.RemoveLast());
				}
			}
			else{
				shrines = nextLevelShrines;
				nextLevelShrines = null;
			}
			int attempts = 0;
			while(shrines.Count > 0){
				attempts = 0;
				for(bool done=false;!done;++attempts){
					pos rp = GetRandomPosition(CurrentLevelType,true);
					int rr = rp.row;
					int rc = rp.col;
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
								foreach(pos p in temp.PositionsWithinDistance(4,map)){
									CellType ch = map[p];
									if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
										good = false;
									}
								}
								if(good){
									List<pos> dist2 = new List<pos>();
									foreach(pos p2 in temp.PositionsAtDistance(2,map)){
										if(map[p2].IsFloor()){
											dist2.Add(p2);
										}
									}
									if(dist2.Count > 0){
										map[rr,rc] = shrines.RemoveFirst();
										pos p2 = dist2.Random();
										map[p2.row,p2.col] = shrines.RemoveFirst();
										done = true;
										break;
									}
								}
								else{
									interesting_tiles.Remove(temp);
								}
							}
							bool floors = true;
							foreach(pos p in temp.PositionsAtDistance(1,map)){
								if(!map[p.row,p.col].IsFloor()){
									floors = false;
								}
							}
							foreach(pos p in temp.PositionsWithinDistance(3,map)){
								CellType ch = map[p];
								if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
									floors = false;
								}
							}
							if(floors){
								if(R.CoinFlip()){
									map[rr-1,rc] = shrines.RemoveFirst();
									map[rr+1,rc] = shrines.RemoveFirst();
								}
								else{
									map[rr,rc-1] = shrines.RemoveFirst();
									map[rr,rc+1] = shrines.RemoveFirst();
								}
								CellType center = CellType.Wall;
								while(center == CellType.Wall){
									switch(R.Roll(5)){
									case 1:
										if(CurrentLevelType != LevelType.Hive && CurrentLevelType != LevelType.Garden){
											center = CellType.Pillar;
										}
										break;
									case 2:
										center = CellType.Statue;
										break;
									case 3:
										if(CurrentLevelType != LevelType.Garden && CurrentLevelType != LevelType.Hellish){
											center = CellType.FirePit;
										}
										break;
									case 4:
										if(CurrentLevelType != LevelType.Hellish){
											center = CellType.ShallowWater;
										}
										break;
									case 5:
										if(CurrentLevelType != LevelType.Hive && CurrentLevelType != LevelType.Hellish){
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
							foreach(pos p in temp.PositionsWithinDistance(2,map)){
								CellType ch = map[p];
								if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
									good = false;
								}
							}
							if(good){
								if(attempts > 1000){
									map[rr,rc] = shrines.RemoveFirst();
									interesting_tiles.Remove(temp);
									done = true;
									break;
								}
								else{
									bool floors = true;
									foreach(pos p in temp.PositionsAtDistance(1,map)){
										if(!map[p.row,p.col].IsFloor()){
											floors = false;
										}
									}
									if(floors){
										map[rr,rc] = shrines.RemoveFirst();
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
							foreach(pos p in temp.PositionsWithinDistance(2,map)){
								CellType ch = map[p];
								if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
									no_good = true;
								}
							}
							if(no_good){
								continue;
							}
							int walls = 0;
							foreach(pos p in temp.PositionsAtDistance(1,map)){
								if(map[p].IsWall()){
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
										map[rr,rc] = shrines.RemoveFirst();
										interesting_tiles.Remove(temp);
										break;
									}
								}
							}
						}
					}
				}
			}
			int num_chests = R.Between(0,1);
			if(CurrentLevelType == LevelType.Crypt){
				num_chests -= map.PositionsWhere(x=>map[x] == CellType.Chest).Count;
			}
			for(int i=0;i<num_chests;++i){
				int tries = 0;
				for(bool done=false;!done;++tries){
					pos rp = GetRandomPosition(CurrentLevelType,true);
					int rr = rp.row;
					int rc = rp.col;
					if(interesting_tiles.Count > 0){
						pos p = interesting_tiles.RemoveRandom();
						rr = p.row;
						rc = p.col;
						map[rr,rc] = CellType.RoomInterior;
					}
					if(map[rr,rc].IsFloor()){
						bool floors = true;
						pos temp = new pos(rr,rc);
						foreach(pos p in temp.PositionsAtDistance(1,map)){
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
			if(CurrentLevelType != LevelType.Hellish){
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
						foreach(pos p in temp.PositionsAtDistance(1,map)){
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
			}
			if(CurrentLevelType != LevelType.Garden && CurrentLevelType != LevelType.Hellish){
				if(CurrentLevelType != LevelType.Sewer){
					GenerateFloorTypes(map);
				}
				GenerateFeatures(map,interesting_tiles);
			}
			int num_traps = R.Roll(2,3);
			for(int i=0;i<num_traps;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					pos rp = GetRandomPosition(CurrentLevelType,false);
					if(map[rp].IsFloor() && map[rp] != CellType.ShallowWater){
						map[rp] = CellType.Trap;
						done = true;
					}
				}
			}
			List<Tile> hidden = new List<Tile>();
			Event grave_dirt_event = null;
			Event poppy_event = null;
			Tile stairs = null;
			List<Tile> addFire = new List<Tile>();
			List<Tile> addBlood = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					//Screen.WriteMapChar(i,j,map[i,j]);
					switch(map[i,j]){
					case CellType.Wall:
					case CellType.Pillar:
						Tile.Create(TileType.WALL,i,j);
						break;
					case CellType.Door:
						if(R.OneIn(120) && CurrentLevelType != LevelType.Hellish){
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
						if(Depth < 20){
							Tile.Create(TileType.STAIRS,i,j);
							stairs = tile[i,j];
						}
						else{
							if(Depth == 20){
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
						break;
					case CellType.SpecialFeature2:
						Tile.Create(TileType.DEFENSE_SHRINE,i,j);
						break;
					case CellType.SpecialFeature3:
						Tile.Create(TileType.MAGIC_SHRINE,i,j);
						break;
					case CellType.SpecialFeature4:
						Tile.Create(TileType.SPIRIT_SHRINE,i,j);
						break;
					case CellType.SpecialFeature5:
						Tile.Create(TileType.STEALTH_SHRINE,i,j);
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
					case CellType.RoomFeature1: //used only by hellish levels
					Tile.Create(TileType.DEMONIC_IDOL,i,j);
					break;
					case CellType.RoomFeature2: //used only by hellish levels
					{
						Tile t = Tile.Create(TileType.FLOOR,i,j);
						t.color = Color.RandomDoom;
						break;
					}
					case CellType.RoomFeature3: //used only by hellish levels
					{
						Tile t = Tile.Create(TileType.DEMONSTONE,i,j);
						if(R.CoinFlip()){
							addFire.Add(t);
						}
						break;
					}
					case CellType.RoomFeature4: //used only by hellish levels
					{
						Tile t = Tile.Create(TileType.DEMONSTONE,i,j);
						addFire.Add(t);
						break;
					}
					case CellType.RoomFeature5: //used only by hellish levels
					{
						Tile.Create(TileType.DEMONSTONE,i,j);
						break;
					}
					case CellType.CorridorFeature1: //used only by hellish levels
					{
						Tile t = Tile.Create(TileType.FLOOR,i,j);
						if(R.CoinFlip()){
							addBlood.Add(t);
						}
						break;
					}
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					//alltiles.Add(tile[i,j]);
					tile[i,j].solid_rock = true;
				}
			}
			if(CurrentLevelType == LevelType.Hellish || (currentLevelIdx+1 < level_types.Count && level_types[currentLevelIdx+1] == LevelType.Hellish)){
				foreach(int dir in new int[]{4,6}){
					Tile neighbor = stairs?.TileInDirection(dir);
					neighbor?.Toggle(null,TileType.DEMONIC_IDOL);
				}
				stairs.color = Color.RandomDoom;
			}
			//Input.ReadKey();
			player.ResetForNewLevel();
			foreach(Tile t in AllTiles()){
				if(t.light_radius > 0){
					t.UpdateRadius(0,t.light_radius);
				}
			}
			foreach(Tile t in addFire){
				t.AddFeature(FeatureType.FIRE);
			}
			foreach(Tile t in addBlood){
				t.AddBlood(Color.DarkRed);
				if(R.CoinFlip()){
					foreach(int dir in U.FourDirections){
						if(R.CoinFlip()){
							Tile neighbor = t.TileInDirection(dir);
							if(neighbor?.name == "floor" && neighbor.color == Color.White){
								neighbor.AddBlood(Color.DarkRed);
							}
						}
					}
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
				ActorType type = ChooseMobType(false,false);
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
							Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).RandomOrDefault();
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
											thralltype = ChooseMobType(false,false);
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
			for(int i=(Depth-5)/2;i>0;--i){ //yes, this is all copied and pasted for a one-line change. i'll try to fix it later.
				if(!R.OneIn(3)){ //generate some shallow monsters
					ActorType type = ChooseMobType(false,true);
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
								Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).RandomOrDefault();
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
												thralltype = ChooseMobType(false,false);
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
			List<Tile> goodtiles = new List<Tile>();
			if(CurrentLevelType == LevelType.Hellish){
				goodtiles.Add(tile[15,1]);
				int[] cultistRows = new int[]{9,10,10};
				int[] cultistCols = new int[]{3,3,4};
				for(int i=0;i<3;++i){
					Actor a = Actor.Create(ActorType.CULTIST,cultistRows[i],cultistCols[i]);
					a.attrs[AttrType.NO_ITEM] = 1;
					a.ApplyBurning();
					a.curhp = R.Between(7,16);
				}
				final_level_cultist_count[0] = 3;
			}
			else{
				int minimum_distance_from_stairs = 0;
				PosArray<int> distance_from_stairs = null;
				if(stairs != null){
					distance_from_stairs = tile.GetDijkstraMap(new List<pos>{stairs.p},x=>tile[x].BlocksConnectivityOfMap());
					minimum_distance_from_stairs = distance_from_stairs[distance_from_stairs.PositionsWhere(x=>distance_from_stairs[x].IsValidDijkstraValue()).WhereGreatest(x=>distance_from_stairs[x]).Random()] / 2;
				}
				PosArray<bool> rejectedLocations = new PosArray<bool>(ROWS,COLS);
				for(int i=0;i<ROWS;++i){
					for(int j=0;j<COLS;++j){
						if(actor[i,j] != null || tile[i,j].type == TileType.BLAST_FUNGUS){
							Tile t = tile[i,j];
							foreach(Tile target in tile){
								if(rejectedLocations[target.p] == false && t.HasLOS(target)){
									rejectedLocations[target.p] = true;
								}
							}
						}
						if(tile[i,j].IsTrap()){
							foreach(Tile neighbor in tile[i,j].TilesWithinDistance(1)){
								rejectedLocations[neighbor.p] = true;
							}
						}
						if(!tile[i,j].Is(TileType.FLOOR) || tile[i,j].Is(FeatureType.WEB) || (stairs != null && distance_from_stairs[i,j] < minimum_distance_from_stairs)){
							rejectedLocations[i,j] = true;
						}
					}
				}
				for(int i=0;i<ROWS;++i){
					for(int j=0;j<COLS;++j){
						if(!rejectedLocations[i,j]){
							goodtiles.Add(tile[i,j]);
						}
					}
				}
			}
			Tile startTile = null;
			if(goodtiles.Count > 0){
				startTile = goodtiles.Random();
			}
			else{
				while(true){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
						if(t.IsTrap() || t.actor() != null){
							good = false;
						}
					}
					if(good && tile[rr,rc].passable && actor[rr,rc] == null){
						startTile = tile[rr,rc];
						break;
					}
				}
			}
			{ //now that we've got a start tile for the player...
				int light = player.light_radius;
				int burning = player.attrs[AttrType.BURNING];
				int shining = player.attrs[AttrType.SHINING];
				player.light_radius = 0;
				player.attrs[AttrType.BURNING] = 0;
				player.attrs[AttrType.SHINING] = 0;
				player.Move(startTile.row,startTile.col);
				player.light_radius = light;
				player.attrs[AttrType.BURNING] = burning;
				player.attrs[AttrType.SHINING] = shining;
				player.UpdateRadius(0,player.LightRadius());
			}
			actor[player.row,player.col] = player; //this line fixes a bug that occurs when the player ends up in the same position on a new level
			Screen.screen_center_col = player.col;
			if(R.PercentChance(40) && CurrentLevelType != LevelType.Hellish){ //todo: copied and pasted below
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
								if(R.FractionalChance(Depth,20)){
									tile[rr,rc].TileInDirection(dirs[0].RotateDir(true,4)).color = Color.Yellow;
								}
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
								if(R.FractionalChance(Depth,20)){
									tile[rr,rc].color = Color.Yellow;
								}
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
			if(CurrentLevelType == LevelType.Hive){
				var dijkstra = tile.GetDijkstraMap(x=>tile[x].type != TileType.WALL,x=>false);
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
			if(CurrentLevelType == LevelType.Fortress){
				foreach(pos p in tile.PositionsWhere(x=>tile[x].Is(TileType.WALL,TileType.HIDDEN_DOOR))){
					tile[p].color = Color.TerrainDarkGray;
				}
			}
			if(currentLevelIdx == 0 || CurrentLevelType != level_types[currentLevelIdx-1]){ //i.e. on leveltype changes
				dungeonDescription = GetDungeonDescription();
			}
			if(poppy_event != null){
				CalculatePoppyDistanceMap();
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
			if(Depth == 1){
				B.Add("In the mountain pass where travelers vanish, a stone staircase leads downward... Welcome, " + Actor.player_name + "! ");
			}
			else{
				B.Add(LevelMessage());
			}
			currentlyGeneratingLevel = false;
		}
		private string[] HellishLevelLayout(){
			return new string[]{
				"##################################################################",
				"##################################################################",
				"##################################################################",
				"##5~~.......~^5###################################################",
				"##~~.........~^###################################################",
				"##.............###################################################",
				"##....####.....###################################################",
				"##****####.!.!.###################################################",
				"#*2**2*###....!.!.################################################",
				"#**&&**###^~.!!!!!################################################",
				"#**&&**###5&~...!.################################################",
				"#*2**2*###########################################################",
				"##****############################################################",
				"##....############################################################",
				"#...!..###########################################################",
				"#@...!.###########################################################",
				"##.55.############################################################",
				"##....############################################################",
				"##&&&&############################################################",
				"##################################################################",
				"##################################################################",
				"##################################################################"
			};
		}
		private string[] FinalLevelLayout(){
			/*return new string[]{
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
			};*/
			return new string[]{
				"##################################################################",
				"##################....######..........######....##################",
				"###############......##****##........##****##......###############",
				"############..+......+*2**2*+........+*2**2*+......+..############",
				"#########.....+......+**&&**+........+**&&**+......+.....#########",
				"########......##++++##**&&**##++++++##**&&**##++++##......########",
				"#######.......##.....+*2**2*#........#*2**2*+.....##.......#######",
				"######........+......##****##........##****##......+........######",
				"######........+......##+++##..........##+++##......+........######",
				"#####...#++#++##.....+....+.....XX.....+....+.....##++#++#...#####",
				"#####+++#..#...##....+....+.....XX.....+....+....##...#..#+++#####",
				"#####......+....###++#+#..+............+..#+#++###....+......#####",
				"#####......+.....##****##+##..........##+##****##.....+......#####",
				"######.....+.....+*2**2*+..##........##..+*2**2*+.....+.....######",
				"######.....+.....+**&&**+...###++++###...+**&&**+.....+.....######",
				"#######....#..#++#**&&**+....##****##....+**&&**#++#..#....#######",
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
									neighbor.solid_rock = false;
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
						Global.KILLED_BY = "turned to ash";
						break;
					}
				}
			}
		}
		public void GenerateFinalLevel(){
			currentlyGeneratingLevel = true;
			final_level_cultist_count = new int[5];
			final_level_demon_count = 0;
			final_level_clock = 0;
			Depth = 21;
			InitializeNewLevel();
			PosArray<CellType> map = new PosArray<CellType>(ROWS,COLS);
			PosArray<bool> doors = new PosArray<bool>(ROWS,COLS);
			List<List<pos>> door_sets = new List<List<pos>>();
			ReadSpecialLevel(map,FinalLevelLayout(),doors,door_sets);
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
						tile[i,j].color = Color.RandomDoom;
						break;
					case CellType.RoomFeature3:
					{
						Tile t = Tile.Create(TileType.DEMONSTONE,i,j);
						if(R.CoinFlip()){
							flames.Add(t);
						}
						break;
					}
					case CellType.RoomFeature4:
						Tile.Create(TileType.DEMONSTONE,i,j);
						flames.Add(tile[i,j]);
						break;
					case CellType.RoomFeature5:
						Tile.Create(TileType.DEMONSTONE,i,j);
						break;
					case CellType.CorridorFeature2:
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
			//player.Move(6,7);
			player.Move(7,29);
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
			Actor.Create(ActorType.MINOR_DEMON,7,31);
			Actor.Create(ActorType.MINOR_DEMON,9,30);
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
			dungeonDescription = GetDungeonDescription();
			currentlyGeneratingLevel = false;
		}
		private enum FloorType{Brush,Water,Gravel,GlowingFungus,Ice,PoppyField,GraveDirt};
		public void GenerateFloorTypes(PosArray<CellType> map){
			List<FloorType> floors = new List<FloorType>();
			int[] rarity = null; //the given rarity means "1 in X"
			switch(CurrentLevelType){
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
			case LevelType.Sewer:
				rarity = new int[]{0,0,0,0,0,0,0}; //special handling, see GenerateLevel()
				break;
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
			if(CurrentLevelType == LevelType.Mine){
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
						if(CurrentLevelType == LevelType.Fortress && max_radius > 2){
							max_radius = 2;
						}
						map[rr,rc] = cell;
						for(int j=1;j<=max_radius;++j){
							List<pos> added = new List<pos>();
							foreach(pos p in new pos(rr,rc).PositionsWithinDistance(j,map)){
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
							if(!new pos(rr,rc).PositionsAtDistance(1,map).Any(x=>map[x] == CellType.Tombstone)){
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
			switch(CurrentLevelType){
			case LevelType.Standard:
				rarity = new int[]{30,40,15,30,
					25,6,8,15,15,3,3,4,4,4};
				break;
			case LevelType.Cave:
				rarity = new int[]{30,15,10,15,
					15,100,8,10,30,5,25,6,3,4};
				break;
			case LevelType.Sewer:
				rarity = new int[]{100,0,5,16,
					80,40,7,5,30,30,80,10,15,2};
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
				thin_walls = map.AllPositions().Where(x=>map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck(tile) && map[y].IsFloor()));
			}
			while(result.Count > 0){
				DungeonFeature df = result.RemoveRandom();
				switch(df){
				case DungeonFeature.POOL_OF_RESTORATION:
				case DungeonFeature.FIRE_PIT:
				{
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
							foreach(pos p in new pos(rr,rc).PositionsAtDistance(1,map)){
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
				}
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
				{
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
								foreach(pos p in new pos(rr,rc).PositionsWithinDistance(j,map)){
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
				}
				case DungeonFeature.SLIME:
				case DungeonFeature.OIL:
				{
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
								foreach(pos p in new pos(rr,rc).PositionsWithinDistance(j,map)){
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
				}
				case DungeonFeature.FIRE_GEYSER:
				{
					for(int i=0;i<50;++i){
						int rr = R.Roll(ROWS-4)+1;
						int rc = R.Roll(COLS-4)+1;
						if(map[rr,rc].IsFloor()){
							bool floors = true;
							foreach(pos p in new pos(rr,rc).PositionsAtDistance(1,map)){
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
				}
				case DungeonFeature.VINES:
				{
					for(int i=0;i<500;++i){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
						pos p = new pos(rr,rc);
						if(map[p].IsRoomType() && p.HasAdjacentWhere(x=>map.BoundsCheck(x) && map[x].IsWall())){
							PosArray<bool> vine = map.GetFloodFillArray(p,false,x=>map[x].IsRoomType() && x.HasAdjacentWhere(y=>map.BoundsCheck(y) && map[y].IsWall()) && !R.OneIn(3)); //changed from one in 6 so vines won't fill caves so often
							rr = R.Roll(ROWS-2);
							rc = R.Roll(COLS-2);
							pos p2 = new pos(rr,rc);
							PosArray<bool> new_vine = new PosArray<bool>(ROWS,COLS);
							int max = Math.Max(ROWS,COLS);
							for(int dist=0;dist<max;++dist){
								bool found = false;
								foreach(pos possible_vine in p2.PositionsAtDistance(dist)){
									if(possible_vine.BoundsCheck(new_vine,false)){
										found = true;
										if(vine[possible_vine] && possible_vine.PositionsAtDistance(1,new_vine).Where(x=>new_vine[x] || map[x] == CellType.Vine).Count < 3){
											new_vine[possible_vine] = true;
										}
									}
								}
								if(!found){
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
				}
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
			if(Depth == 1 || level_types[currentLevelIdx - 1] == CurrentLevelType){
				return "";
			}
			if(R.OneIn(2000)){
				return R.Choose("This level can't be all bad... "); // "What a boring place..."
			}

			List<string> messages = new List<string>();
			switch(CurrentLevelType){
			case LevelType.Standard:
				messages.Add("You enter a complex of ancient rooms and hallways. ");
				messages.Add("Well-worn corridors suggest that these rooms are frequently used. ");
				//messages.Add("You find another network of hallways and rooms. ");
				messages.Add("A web of tunnels stretches away in front of you. ");
				messages.Add("The path leads you to another maze of dim tunnels. ");
				break;
			case LevelType.Cave:
				messages.Add("You enter a large natural cave. ");
				messages.Add("This cavern's rough walls shine with moisture. ");
				messages.Add("A cave opens up before you. A dry, dusty scent lingers in the ancient tunnels. ");
				break;
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
			case LevelType.Sewer:
				messages.Add("Unwholesome water streams along the walls here, forming pools that froth and swirl. ");
				messages.Add("The rank odor of these slimy tunnels assaulted you before you reached them. ");
				messages.Add("The sound of rushing water fills these tunnels. ");
			break;
			case LevelType.Hellish:
				messages.Add("Before you, leering idols and demonic glyphs surround leaping flames. As you watch, cultists approach the fire, then throw themselves in! ");
			break;
			/*case LevelType.Ruined:
				messages.Add("You enter a badly damaged rubble-strewn area of the dungeon. ");
				messages.Add("Broken walls and piles of rubble cover parts of the floor here. ");
				messages.Add("This section of the dungeon has partially collapsed. ");
				break;
			case LevelType.Extravagant:
				messages.Add("This area is decorated with fine tapestries, marble statues, and other luxuries. ");
				messages.Add("Patterned decorative tiles, fine rugs, and beautifully worked stone greet you upon entering this level. ");
				break;*/
			default:
				messages.Add("What is this strange place? ");
				break;
			}
			if(Depth > 1){
				string transition = TransitionMessage(level_types[currentLevelIdx - 1],CurrentLevelType);
				if(transition != ""){
					messages.Add(transition);
				}
				if(CurrentLevelType == LevelType.Standard){
					switch(level_types[currentLevelIdx - 1]){
					case LevelType.Cave:
					case LevelType.Garden:
					case LevelType.Mine:
					case LevelType.Sewer:
					messages.Add("You leave the " + level_types[currentLevelIdx - 1].ToString().ToLower() + "s behind. "); 
					break;
					case LevelType.Crypt:
					case LevelType.Fortress:
					case LevelType.Hive:
					messages.Add("You leave the " + level_types[currentLevelIdx - 1].ToString().ToLower() + " behind. "); 
					break;
					}
				}
			}
			return messages.Random();
		}
		public string TransitionMessage(LevelType from,LevelType to){
			switch(from){
			case LevelType.Standard:
				switch(to){
				case LevelType.Cave:
					return "Narrow corridors disappear from your surroundings as you reach a large natural cavern. ";
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
				case LevelType.Sewer:
					return "These low tunnels are more suited to carrying volumes of dirty water than to allowing easy passage. ";
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
					return "The rooms around you begin to look more typical, created by picks rather than the jaws of countless insects. ";
				case LevelType.Cave:
					return "You leave the cramped chambers behind and enter a wider cave. ";
				/*case LevelType.Ruined:
					return "This area was clearly built by intelligent life, but nature seems to be reclaiming the ruined tunnels. ";
				case LevelType.Extravagant:
					return "Your skin stops crawling as you leave the hives behind and enter a beautifully furnished area. ";*/
				case LevelType.Mine:
					//return "Tools on the ground reveal that the rooms here are being made by humanoids rather than insects. ";
					return "";
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
					//return "You leave the mines behind and return to finished corridors and rooms. ";
					return "";
				case LevelType.Cave:
					return "The half-finished tunnels disappear as natural cave walls surround you. ";
				/*case LevelType.Ruined:
					return "This area is collapsing and ruined. It looks much older than the mines you just left. ";
				case LevelType.Extravagant:
					return "As you walk, incomplete tunnels turn into luxurious carpeted hallways. ";*/
				case LevelType.Hive:
					return "As you continue, signs of humanoid construction vanish and hive walls appear. ";
				case LevelType.Fortress:
					return "You reach a section that is not only complete but easily defensible. ";
				case LevelType.Crypt:
					return "Gravestones appear as you leave the mines behind. ";
				case LevelType.Garden:
					return "You leave the narrow mines to find rooms carefully arranged with rows of flowering plants. ";
				case LevelType.Sewer:
					return "As you continue, water appears in some of the tunnels, occasionally reaching your knees. ";
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
				case LevelType.Sewer:
					return "Beyond the fortress you encounter halls full of polluted water. ";
				}
				break;
			case LevelType.Crypt:
				switch(to){
				case LevelType.Standard:
					//return "You leave the crypt behind and again encounter rooms used by the living. ";
					return "";
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
				case LevelType.Sewer:
					return "This area of the crypt is flooded with fetid water. ";
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
				case LevelType.Sewer:
					return "The sweet scents of the garden are replaced by the lingering funk of filthy standing water. ";
				}
				break;
			case LevelType.Sewer:
				switch(to){
				case LevelType.Garden:
					return "Wonderful fresh air and pools of clean water appear. ";
				} //todo: more?
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

