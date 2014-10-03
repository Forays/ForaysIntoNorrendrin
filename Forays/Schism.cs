/*Copyright (c) 2013-2014 Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
/*
 * 


add "remove all actions" to new map menu


-option changes don't get saved sometimes. why?
looks like the default-value problem is key related. specifically, it works when you press space.
...nope!

-remove unconnected turns statues to walls

-possible "enforce corridor types" that checks for intersections, etc.
-rejection criteria. "tried 9999 times without meeting this requirement. removing requirement."
-filling map with floors should set them all to 'background', so things can be placed on them.
  -also, rooms placement should place walls, too, for cases like this.
-it's not hard to crash it, currently, because the borders aren't always walls.

[a], [b], and [c] menus - at least - require input, so my menu code might not even work without changes.

done menu?

how should map rejection work? rejection and setting a specific seed should be mutually exclusive.


fire pits, ruin?

--option to genericize percentage-based actions, i.e. 90% fill 40%, 10% fill 60%.  ...maybe?



12345678901234567890123456789012345678901234567890123456789012345678901234567890

fill map using plasma fractal noise
fill map using flow displaced rock layers(this might go under flavor instead)

quit
save to file

finalize?
add flavor?
mark as background?

 */
//using System.IO;
//using ParabolaConsoleLib;
using Utilities;
/*

---


	    #####	    	 ###		#############	      #######
	  ###+.+###  	    ##.##		#...........#	      #.....#
	 ##+.....+## 	   ##...##		##..........#	      #.....#
	 #+.......+# 	  ##.....##		 ##.........#	      #.....#
	##.........##	 ##.......##	  ##........#	#######.....#
	#+.........+#	##.........##	   ##.......#	#...........#
	#...........#	#...........#	    ##......#	#...........#
	#+.........+#	##.........##	     ##.....#	#...........#
	##.........##	 ##.......##	      ##....#	#...........#
	 #+.......+# 	  ##.....##	     	   ##...#	#...........#
	 ##+.....+## 	   ##...##	     	    ##..#	#...........#
	  ###+.+###  	    ##.##    	         ##.#	#...........#
	    #####    	     ###     	          ###	#############

can/should I prevent the "dead ends" in the middle 2 above? they'd get filled in by the dead-end algo.
	-probably by having the dead end algo only work on corridors.

   ######
####EEEc#
#cEEn..E#  
#E.....E##     
#E.....nE#	
#cEEEEEEc#
##########

-setting the 'background' flag

-menu with display options, including "Resize" - resizes directly on .net, waits for user on linux.

-scrolling functionality to view entire map when it's bigger than the screen

-plasma stuff. overlays for certain terrain types.

-output to file, with char conflict resolution.

but that stuff will come later. right now, let's write up some example use cases:


 (convert to another format or to simple wall/floor)?
 (add fire pits)? surely this can be genericized.
 apply 'ruin' effect - genericize?

*/

namespace SchismDungeonGenerator{
	using SchismExtensionMethods;
	public enum CellType{Wall,RoomCorner,RoomEdge,RoomInterior,RoomInteriorCorner,CorridorHorizontal,CorridorVertical,CorridorIntersection,Door,Pillar,Statue,InterestingLocation,ShallowWater,DeepWater,Tree,RoomFeature1,RoomFeature2,RoomFeature3,RoomFeature4,RoomFeature5,CorridorFeature1,CorridorFeature2,CorridorFeature3,CorridorFeature4,CorridorFeature5,SpecialFeature1,SpecialFeature2,SpecialFeature3,SpecialFeature4,SpecialFeature5,Stairs,Chest,FirePit,Pool,Chasm,BlastFungus,GlowingFungus,Geyser,Rubble,Trap,Tombstone,HiddenDoor,Barrel,Torch,Poppies,Ice,Webs,GraveDirt,Slime,Oil,Gravel,Brush,Vine,FogVent,PoisonVent,PoisonBulb,CrackedWall}; //this method of handling room features is terrible. still looking for a better one that doesn't involve a string -> type mapping.
	public class Dungeon{
		public int H;
		public int W;
		public PosArray<CellType> map;
		public Dungeon(int height,int width){
			H = height;
			W = width;
			map = new PosArray<CellType>(H,W);
		}
		public Dungeon(int height,int width,int seed){
			H = height;
			W = width;
			map = new PosArray<CellType>(H,W);
			R.SetSeed(seed); //this is a bad idea
		}
		public CellType this[int row,int col]{ //indexers, why not
			get{
				return map[row,col];
			}
			set{
				map[row,col] = value;
			}
		}
		public CellType this[pos p]{
			get{
				return map[p];
			}
			set{
				map[p] = value;
			}
		}
		//the options:
		public bool AllowAllCornerConnections = false; //0 (index of option)
		public bool AllowRoomsToOverwriteCorridors = true; //1
		public bool AllowRoomsToOverwriteRooms = true; //2
		public bool AllowCorridorChainsToOverlapThemselves = false; //3
		public bool RoomsMustHaveOddHeightAndWidth = false;
		//the generation parameters:
		public int CorridorLengthMin = 3; //0 (index of variable)
		public int CorridorLengthMax = 7; //1 - "max" might not be the actual maximum if CorridorExtraLength and CorridorExtraLengthChance are more than zero
		public int CorridorExtraLength = 8; //2
		public int CorridorExtraLengthChance = 50; //3 - percent chance to add CorridorExtraLength to a given corridor
		public int CorridorChainSizeMin = 1; //4 -the number of linked corridors that will be
		public int CorridorChainSizeMax = 4; //5 -      generated at one time
		public int MinimumSpaceBetweenCorridors = 3; //6 - two parallel corridors must have at least this many cells between them, if they run alongside one another
		public int RoomHeightMin = 3; //7
		public int RoomHeightMax = 8; //8
		public int RoomExtraHeight = 0; //9
		public int RoomExtraHeightChance = 0; //10
		public int RoomWidthMin = 3; //11
		public int RoomWidthMax = 10; //12
		public int RoomExtraWidth = 0; //13
		public int RoomExtraWidthChance = 0; //14
		//random:
		public static int Roll(int sides){ return R.Roll(sides); }
		public static int Roll(int sides,int dice){ return R.Roll(sides,dice); }
		public static int Between(int a,int b){ return R.Between(a,b); }
		public static bool CoinFlip(){ return R.CoinFlip(); }
		public static bool OneIn(int x){ return R.OneIn(x); }
		public static bool PercentChance(int x){ return R.PercentChance(x); }
		//useful directions
		public static int N = 8;
		public static int[] EightDirections = U.EightDirections;
		public static int[] FourDirections = U.FourDirections;
		public static int[] DiagonalDirections = U.DiagonalDirections;
		//information:
		public int NumberOfFloors(){ //i.e. nonwalls
			int total = 0;
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(!map[i,j].IsWall()){
						++total;
					}
				}
			}
			return total;
		}
		public bool IsFullyConnected(){
			int[,] num = new int[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(map[i,j].IsPassable()){
						num[i,j] = 0;
					}
					else{
						num[i,j] = -1;
					}
				}
			}
			int count = 0;
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(num[i,j] == 0){
						count++;
						if(count > 1){
							return false;
						}
						num[i,j] = count;
						bool changed = true;
						while(changed){
							changed = false;
							for(int s=0;s<H;++s){
								for(int t=0;t<W;++t){
									if(num[s,t] == count){
										for(int ds=-1;ds<=1;++ds){
											for(int dt=-1;dt<=1;++dt){
												if(num[s+ds,t+dt] == 0){
													num[s+ds,t+dt] = count;
													changed = true;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return true;
		}
		public bool HasLargeUnusedSpaces(){ return HasLargeUnusedSpaces(H * W / 5); }
		public bool HasLargeUnusedSpaces(int threshold){
			int min_height = threshold / (W-2);
			int min_width = threshold / (H-2);
			for(int i=1;i<H-1;++i){
				for(int j=1;j<W-1;++j){
					bool good = true;
					int width = -1;
					if((W-j)-1 < min_width || (H-i)-1 < min_height){
						good = false;
					}
					else{
						for(int k=0;k<(W-j)-1;++k){
							if(!map[i,j+k].IsWall()){
								if(k < min_width){
									good = false;
								}
								break;
							}
							else{
								width = k+1;
							}
						}
					}
					for(int lines = 1;lines<(H-i)-1 && good;++lines){
						if(lines * width >= threshold){
							return true;
						}
						for(int k=0;k<(W-j)-1;++k){
							if(!map[i+lines,j+k].IsWall()){
								if(k < min_width){
									good = false;
								}
								else{
									if(k+1 < width){
										width = k+1;
									}
								}
								break;
							}
						}
					}
				}
			}
			return false;
		}
		public bool IsLegal(int r,int c){ return IsLegal(new pos(r,c)); }
		public bool IsLegal(pos p){
			switch(map[p]){
			//case CellType.RoomInterior:
			case CellType.Pillar:
			case CellType.RoomFeature1:
			case CellType.RoomFeature2:
			case CellType.RoomFeature3:
			case CellType.InterestingLocation:
				foreach(pos neighbor in p.AdjacentPositionsClockwise()){
					if(!map[neighbor].IsRoomType()){
						return false;
					}
				}
				break;
			case CellType.RoomEdge:
			{
				int roomdir = 0;
				foreach(int dir in FourDirections){
					pos neighbor = p.PosInDir(dir);
					if(BoundsCheck(neighbor) && !map[neighbor].IsRoomType()){
						roomdir = dir.RotateDir(true,4);
						break;
					}
				}
				if(roomdir == 0){
					return false; //no room found, error - disable this if you want tiny rooms with h/w of 2
					/*char[] rotated = new char[8];
					for(int i=0;i<8;++i){
						rotated[i] = Map(PosInDir(r,c,RotateDir(8,true,i)));
					}
					int successive_corridors = 0;
					if(IsCorridor(rotated[7])){
						successive_corridors++;
					}
					for(int i=0;i<8;++i){
						if(IsCorridor(rotated[i])){
							successive_corridors++;
						}
						else{
							successive_corridors = 0;
						}
						if(successive_corridors == 2){
							return false;
						}
					}
					int successive_room_tiles = 0;
					if(IsRoom(rotated[5])){
						successive_room_tiles++;
					}
					if(IsRoom(rotated[6])){
						successive_room_tiles++;
					}
					else{
						successive_room_tiles = 0;
					}
					if(IsRoom(rotated[7])){
						successive_room_tiles++;
					}
					else{
						successive_room_tiles = 0;
					}
					for(int i=0;i<8;++i){
						if(IsRoom(rotated[i])){
							successive_room_tiles++;
						}
						else{
							successive_room_tiles = 0;
						}
						if(successive_room_tiles == 5){
							return true;
						}
					}*/
				}
				else{
					List<pos> rotated = p.AdjacentPositionsClockwise(roomdir);
					foreach(int dir in new int[]{0,1,7}){
						if(!map[rotated[dir]].IsRoomType()){
							return false;
						}
					}
					foreach(int dir in new int[]{2,6}){
						if(!map[rotated[dir]].IsRoomEdgeType()){
							return false;
						}
					}
					if((map[rotated[4]].IsWall() || (map[rotated[3]].IsWall() && map[rotated[5]].IsWall())) == false){
						return false;
					}
				}
				break;
			}
			case CellType.RoomCorner:
			{
				int roomdir = 0;
				foreach(int dir in DiagonalDirections){
					pos neighbor = p.PosInDir(dir);
					if(BoundsCheck(neighbor) && map[neighbor].IsRoomInteriorType()){
						roomdir = dir;
						break;
					}
				}
				if(roomdir == 0){
					return false; //no room found, error
				}
				List<pos> rotated = p.AdjacentPositionsClockwise(roomdir);
				foreach(int dir in new int[]{1,7}){
					if(!map[rotated[dir]].IsRoomEdgeType()){
						return false;
					}
				}
				if(AllowAllCornerConnections){
					if(!map[rotated[2]].IsWall() && !map[rotated[3]].IsWall()){
						return false;
					}
					if(!map[rotated[6]].IsWall() && !map[rotated[5]].IsWall()){
						return false;
					}
					if(!map[rotated[4]].IsWall()){ //if the corner isn't a wall...
						if(!map[rotated[3]].IsWall() && !map[rotated[5]].IsWall()){
							return false;
						}
						if(map[rotated[3]].IsWall() && map[rotated[5]].IsWall()){ //...reject it if there's not exactly 1 adjacent corridor
							return false;
						}
					}
				}
				else{
					foreach(int dir in new int[]{3,4,5}){
						if(!map[rotated[dir]].IsWall()){
							return false;
						}
					}
				}
				break;
			}
			case CellType.RoomInteriorCorner:
			{
				List<int> wall_dirs = new List<int>();
				List<int> edge_dirs = new List<int>();
				foreach(int dir in DiagonalDirections){
					pos neighbor = p.PosInDir(dir);
					if(BoundsCheck(neighbor) && map[neighbor].IsWall()){
						wall_dirs.Add(dir);
						edge_dirs.AddUnique(dir.RotateDir(true));
						edge_dirs.AddUnique(dir.RotateDir(false));
					}
				}
				if(wall_dirs.Count == 0){
					return false; //no room found, error
				}
				foreach(int dir in EightDirections){
					if(wall_dirs.Contains(dir)){
						if(!map[p.PosInDir(dir)].IsWall()){
							return false;
						}
					}
					else{
						if(edge_dirs.Contains(dir)){
							if(!map[p.PosInDir(dir)].IsRoomEdgeType()){
								return false;
							}
						}
						else{
							if(!map[p.PosInDir(dir)].IsRoomType()){
								return false;
							}
						}
					}
				}
				break;
			}
			case CellType.CorridorHorizontal:
				foreach(int dir in new int[]{2,8}){
					pos next = p;
					for(int i=1;i<=MinimumSpaceBetweenCorridors;++i){
						next = next.PosInDir(dir);
						if(BoundsCheck(next) && map[next] == CellType.CorridorHorizontal){
							return false;
						}
					}
				}
				break;
			case CellType.CorridorVertical:
				foreach(int dir in new int[]{4,6}){
					pos next = p;
					for(int i=1;i<=MinimumSpaceBetweenCorridors;++i){
						next = next.PosInDir(dir);
						if(BoundsCheck(next) && map[next] == CellType.CorridorVertical){
							return false;
						}
					}
				}
				break;
			case CellType.CorridorIntersection:
				if(p.ConsecutiveAdjacent(x => map[x].IsPassable()) >= 3){
					return false;
				}
				break;
			case CellType.Door:
			{
				int dir_of_wall = 0;
				if(map[p.PosInDir(8)].IsWall()){
					dir_of_wall = 8;
				}
				else{
					dir_of_wall = 4;
				}
				if(!map[p.PosInDir(dir_of_wall)].IsWall() || !map[p.PosInDir(dir_of_wall.RotateDir(true,4))].IsWall()
				|| map[p.PosInDir(dir_of_wall.RotateDir(true,2))].IsWall() || map[p.PosInDir(dir_of_wall.RotateDir(false,2))].IsWall()){
					return false; //needs 2 walls on opposite sides, 2 nonwalls on opposite sides
				}
				break;
			}
			}
			return true;
		}
		public bool IsCornerFloor(int r,int c){ return IsCornerFloor(new pos(r,c)); }
		public bool IsCornerFloor(pos p){
			if(p.BoundsCheck(map,false)){
				if(map[p].IsPassable() && p.ConsecutiveAdjacent(x=>map[x].IsWall()) == 5){
					int num_diagonal_walls = 0;
					foreach(int dir in U.DiagonalDirections){
						if(map[p.PosInDir(dir)].IsWall()){
							++num_diagonal_walls;
						}
					}
					if(num_diagonal_walls == 3){
						return true;
					}
				}
			}
			return false;
		}
		public bool SeparatesMultipleAreas(pos p){ return SeparatesMultipleAreas(p.row,p.col); }
		public bool SeparatesMultipleAreas(int r,int c){
			if(r == 0 || c == 0 || r == H-1 || c == W-1){
				return false;
			}
			int[,] num = new int[3,3];
			for(int i=0;i<3;++i){
				for(int j=0;j<3;++j){
					if(map[r+i-1,c+j-1].IsPassable()){
						num[i,j] = 0;
					}
					else{
						num[i,j] = -1;
					}
				}
			}
			num[1,1] = -1;
			int count = 0;
			for(int i=0;i<3;++i){
				for(int j=0;j<3;++j){
					if(num[i,j] == 0){
						count++;
						if(count > 1){
							return true;
						}
						num[i,j] = count;
						bool changed = true;
						while(changed){
							changed = false;
							for(int s=0;s<3;++s){
								for(int t=0;t<3;++t){
									if(num[s,t] == count){
										for(int ds=-1;ds<=1;++ds){
											for(int dt=-1;dt<=1;++dt){
												if(s+ds >= 0 && s+ds < 3 && t+dt >= 0 && t+dt < 3 && num[s+ds,t+dt] == 0){
													num[s+ds,t+dt] = count;
													changed = true;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return false;
		}
		//actions:
		public void Clear(){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					map[i,j] = CellType.Wall;
				}
			}
		}
		public void CreateDefaultMap(){ //todo: this should change based on map size
			//int tries = 0;
			while(true){
				//++tries;
				CreateBasicMap();
				//AddRoomsAndCorridors();
				ConnectDiagonals();
				RemoveDeadEndCorridors();
				RemoveUnconnectedAreas();
				AddDoors(25);
				AddPillars(30);
				MarkInterestingLocations();
				if(NumberOfFloors() < 320 || HasLargeUnusedSpaces(300)){
					Clear();
				}
				else{
					//Screen.Write(23,3,tries.ToString().PadRight(9));
					break;
				}
			}
		}
		public void CreateBasicMap(){
			int pointrows = 2;
			int pointcols = 4;
			List<pos> points = new List<pos>();
			for(int i=1;i<=pointrows;++i){
				for(int j=1;j<=pointcols;++j){
					points.Add(new pos((H*i)/(pointrows+1),(W*j)/(pointcols+1)));
				}
			}
			foreach(pos p in points){
				//map[p] = CellType.Wall;
				for(int tries=0;tries<100;++tries){
					if(CreateRoom(p.row,p.col)){
						break;
					}
				}
			}
			/*bool corners = false;
			for(int remaining=Roll(4);points.Count > remaining || !corners;){
				pos p = points.RemoveRandom();
				map[p] = CellType.Wall;
				for(int tries=0;tries<500;++tries){
					if(CreateRoom(p.row,p.col)){
						break;
					}
				}
				if(points.Contains(new pos(H/(pointrows+1),W/(pointcols+1))) == false
				   && points.Contains(new pos((H*pointrows)/(pointrows+1),(W*pointcols)/(pointcols+1))) == false){
					corners = true;
				}
				if(points.Contains(new pos(H/(pointrows+1),(W*pointcols)/(pointcols+1))) == false
				   && points.Contains(new pos((H*pointrows)/(pointrows+1),W/(pointcols+1))) == false){
					corners = true;
				}
			}
			foreach(pos p in points){
				if(map[p] == CellType.InterestingLocation){
					map[p] = CellType.Wall;
				}
			}*/
			int successes = 0;
			for(int count=0;count<400;++count){
				int rr = -1;
				int rc = -1;
				int dir = 0;
				pos p = new pos(-1,-1);
				for(int i=0;i<9999 && dir == 0;++i){
					rr = Roll(H-4) + 1;
					rc = Roll(W-4) + 1;
					p = new pos(rr,rc);
					if(map[rr,rc].IsWall()){
						int total = 0;
						int lastdir = 0;
						foreach(int direction in U.FourDirections){
							if(map[p.PosInDir(direction)].IsFloor()){
								++total;
								lastdir = direction;
							}
						}
						if(total == 1){
							dir = lastdir;
						}
					}
				}
				if(dir != 0){
					/*bool connecting_to_room = false;
					if(map[p.PosInDir(dir).PosInDir(dir)].IsFloor()){
						foreach(bool clockwise in new bool[]{true,false}){
							if(map[p.PosInDir(dir).PosInDir(dir.RotateDir(clockwise))].IsFloor() && map[p.PosInDir(dir.RotateDir(clockwise))].IsFloor()){
								connecting_to_room = true;
							}
						}
					}
					int extra_chance_of_corridor = 0;
					if(connecting_to_room){
						//extra_chance_of_corridor = 6;
					}
					if(Roll(1,10)+extra_chance_of_corridor > 7){*/ //corridor
					if(PercentChance(30)){
						if(CreateCorridor(rr,rc,dir)){
							++successes;
						}
					}
					else{
						if(CreateRoom(rr,rc,dir)){
							++successes;
						}
					}
					if(successes >= 40){
						return;
					}
				}
			}
		}
		public void AddRoomsAndCorridors(){
			List<pos> l = new List<pos>(); //not recommended atm; doesn't do quite what I want
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					l.Add(new pos(i,j));
				}
			}
			l.Randomize();
			int success_count = 0;
			int failcount = 0;
			for(int i=0;i<l.Count;++i){
				pos p = l[i];
				int dir = 0;
				if(map[p].IsWall()){
					int total = 0;
					int lastdir = 0;
					foreach(int direction in U.FourDirections){
						if(p.PosInDir(direction).BoundsCheck(map) && map[p.PosInDir(direction)].IsFloor()){
							++total;
							lastdir = direction;
						}
					}
					if(total == 1){
						dir = lastdir;
					}
					if(dir == 0){
						dir = Roll(4) * 2;
					}
					if(R.PercentChance(10)){
						if(CreateCorridor(p.row,p.col,Between(CorridorChainSizeMin,CorridorChainSizeMax),dir)){
							++success_count;
						}
						else{
							++failcount;
						}
					}
					else{
						if(CreateRoom(p.row,p.col,dir)){
							++success_count;
						}
						else{
							++failcount;
						}
					}
				}
				if(success_count > 60){
					return;
				}
			}
		}
		public bool CreateCorridor(){ return CreateCorridor(Roll(H-4)+1,Roll(W-4)+1,Between(CorridorChainSizeMin,CorridorChainSizeMax),Roll(4)*2); }
		public bool CreateCorridor(int count){ return CreateCorridor(Roll(H-4)+1,Roll(W-4)+1,count,Roll(4)*2); }
		public bool CreateCorridor(int rr,int rc){ return CreateCorridor(rr,rc,Between(CorridorChainSizeMin,CorridorChainSizeMax),Roll(4)*2); }
		public bool CreateCorridor(int rr,int rc,int count){ return CreateCorridor(rr,rc,count,Roll(4)*2); }
		public bool CreateCorridor(int rr,int rc,int count,int dir){
			bool result = false;
			pos endpoint = new pos(rr,rc);
			pos potential_endpoint;
			List<pos> chain = null;
			if(count > 1){
				chain = new List<pos>();
			}
			int tries = 0;
			while(count > 0 && tries < 100){ //assume there's no room for a corridor if it fails a lot
				tries++;
				rr = endpoint.row;
				rc = endpoint.col;
				potential_endpoint = endpoint;
				if(chain != null && chain.Count > 0){ //reroll direction ONLY after the first part of the chain.
					dir = Roll(4)*2;
				}
				int length = Between(CorridorLengthMin,CorridorLengthMax);
				if(PercentChance(CorridorExtraLengthChance)){
					length += CorridorExtraLength;
				}
				switch(dir){
				case 8: //make them all point either down..
					dir = 2;
					rr -= length-1;
					potential_endpoint.row = rr;
					break;
				case 2:
					potential_endpoint.row += length-1;
					break;
				case 4: //..or right
					dir = 6;
					rc -= length-1;
					potential_endpoint.col = rc;
					break;
				case 6:
					potential_endpoint.col += length-1;
					break;
				}
				switch(dir){
				case 2:
				{
					bool valid_position = true;
					for(int i=rr-1;i<rr+length+1;++i){ //was "int i=rr;i<rr+length"
						if(i >= rr && i < rr+length){
							if(!BoundsCheck(i,rc)){
								valid_position = false;
								break;
							}
						}
						if(AllowCorridorChainsToOverlapThemselves == false && chain != null){
							if(i == endpoint.row){
								continue; //overlapping at the source of the new corridor shouldn't cause rejection
							}
							for(int idx=1;idx<chain.Count;++idx){
								pos one = chain[idx-1];
								pos two = chain[idx];
								if(i == one.row && i == two.row){
									int min = Math.Min(one.col,two.col);
									int max = Math.Max(one.col,two.col);
									if(rc >= min && rc <= max){
										valid_position = false;
										break;
									}
								}
								else{
									if(rc == one.col && rc == two.col){
										int min = Math.Min(one.row,two.row);
										int max = Math.Max(one.row,two.row);
										if(i >= min && i <= max){
											valid_position = false;
											break;
										}
									}
								}
							}
						}
					}
					if(valid_position){
						CellType[] submap = new CellType[length+2];
						for(int i=0;i<length+2;++i){
							submap[i] = map[i+rr-1,rc];
						}
						bool good = true;
						for(int i=0;i<length;++i){
							if(map[i+rr,rc] == CellType.CorridorHorizontal
							   || map[i+rr,rc-1].Is(CellType.CorridorHorizontal,CellType.CorridorIntersection)
							   || map[i+rr,rc+1].Is(CellType.CorridorHorizontal,CellType.CorridorIntersection)){
								map[i+rr,rc] = CellType.CorridorIntersection;
							}
							else{
								switch(map[i+rr,rc]){
								case CellType.CorridorIntersection:
								case CellType.RoomEdge:
								case CellType.RoomInterior:
									break;
								case CellType.RoomCorner:
									if(AllowAllCornerConnections == false){
										good = false;
									}
									break;
								default:
									map[i+rr,rc] = CellType.CorridorVertical;
									break;
								}
							}
						}
						if(good && map[rr-1,rc].Is(CellType.CorridorHorizontal,CellType.CorridorIntersection)){
							map[rr-1,rc] = CellType.CorridorIntersection;
						}
						if(good && map[rr+length,rc].Is(CellType.CorridorHorizontal,CellType.CorridorIntersection)){
							map[rr+length,rc] = CellType.CorridorIntersection;
						}
						for(int i=rr-1;i<rr+length+1 && good;++i){ //note that it doesn't check the bottom or right, since
							for(int j=rc-1;j<rc+2;++j){ //they are checked by the others
								if(i != rr+length && j != rc+1){
									if(map[i,j].IsCorridorType() && map[i,j+1].IsCorridorType() && map[i+1,j].IsCorridorType() && map[i+1,j+1].IsCorridorType()){
										good = false;
										break;
									}
								}
								if(!IsLegal(i,j)){
									good = false;
									break;
								}
							}
						}
						if(!good){ //if this addition is illegal...
							for(int i=0;i<length+2;++i){
								map[i+rr-1,rc] = submap[i];
							}
						}
						else{
							count--;
							tries = 0;
							if(chain != null){
								if(chain.Count == 0){
									chain.Add(endpoint);
								}
								chain.Add(potential_endpoint);
								/*for(int i=rr;i<rr+length;++i){
									pos p = new pos(i,rc);
									if(!(p.Equals(endpoint))){
										chain.Add(p);
									}
								}*/
							}
							endpoint = potential_endpoint;
							result = true;
						}
					}
					break;
				}
				case 6:
				{
					bool valid_position = true;
					for(int j=rc-1;j<rc+length+1;++j){
						if(j >= rc && j < rc+length){
							if(!BoundsCheck(rr,j)){
								valid_position = false;
								break;
							}
						}
						if(AllowCorridorChainsToOverlapThemselves == false && chain != null){
							if(j == endpoint.col){
								continue; //overlapping at the source of the new corridor shouldn't cause rejection
							}
							for(int idx=1;idx<chain.Count;++idx){
								pos one = chain[idx-1];
								pos two = chain[idx];
								if(rr == one.row && rr == two.row){
									int min = Math.Min(one.col,two.col);
									int max = Math.Max(one.col,two.col);
									if(j >= min && j <= max){
										valid_position = false;
										break;
									}
								}
								else{
									if(j == one.col && j == two.col){
										int min = Math.Min(one.row,two.row);
										int max = Math.Max(one.row,two.row);
										if(rr >= min && rr <= max){
											valid_position = false;
											break;
										}
									}
								}
							}
						}
					}
					if(valid_position){
						CellType[] submap = new CellType[length+2];
						for(int j=0;j<length+2;++j){
							submap[j] = map[rr,j+rc-1];
						}
						bool good = true;
						for(int j=0;j<length;++j){
							if(map[rr,j+rc] == CellType.CorridorVertical || map[rr-1,j+rc] == CellType.CorridorVertical || map[rr+1,j+rc] == CellType.CorridorVertical){
								map[rr,j+rc] = CellType.CorridorIntersection;
							}
							else{
								switch(map[rr,j+rc]){
								case CellType.CorridorIntersection:
								case CellType.RoomEdge:
								case CellType.RoomInterior:
									break;
								case CellType.RoomCorner:
									if(AllowAllCornerConnections == false){
										good = false;
									}
									break;
								default:
									map[rr,j+rc] = CellType.CorridorHorizontal;
									break;
								}
							}
						}
						if(good && map[rr,rc-1] == CellType.CorridorVertical){
							map[rr,rc-1] = CellType.CorridorIntersection;
						}
						if(good && map[rr,rc+length] == CellType.CorridorVertical){
							map[rr,rc+length] = CellType.CorridorIntersection;
						}
						for(int i=rr-1;i<rr+2 && good;++i){ //note that it doesn't check the bottom or right, since
							for(int j=rc-1;j<rc+length+1;++j){ //they are checked by the others
								if(i != rr+1 && j != rc+length){
									if(map[i,j].IsCorridorType() && map[i,j+1].IsCorridorType() && map[i+1,j].IsCorridorType() && map[i+1,j+1].IsCorridorType()){
										good = false;
										break;
									}
								}
								if(!IsLegal(i,j)){
									good = false;
									break;
								}
							}
						}
						if(!good){ //if this addition is illegal...
							for(int j=0;j<length+2;++j){
								map[rr,j+rc-1] = submap[j];
							}
						}
						else{
							count--;
							tries = 0;
							if(chain != null){
								if(chain.Count == 0){
									chain.Add(endpoint);
								}
								chain.Add(potential_endpoint);
								/*for(int j=rc;j<rc+length;++j){
									pos p = new pos(rr,j);
									if(!(p.Equals(endpoint))){
										chain.Add(p);
									}
								}*/
							}
							endpoint = potential_endpoint;
							result = true;
						}
					}
					break;
				}
				}
			}
			return result;
		}
		public bool CreateRoom(){ return CreateRoom(Roll(H-2),Roll(W-2)); }
		public bool CreateRoom(int rr,int rc){
			int dir = (Roll(4)*2)-1;
			if(dir == 5){ dir = 9; }
			return CreateRoom(rr,rc,dir);
		}
		public bool CreateRoom(int rr,int rc,int dir){
			int height = Between(RoomHeightMin,RoomHeightMax);
			if(PercentChance(RoomExtraHeightChance)){
				height += RoomExtraHeight;
			}
			int width = Between(RoomWidthMin,RoomWidthMax);
			if(PercentChance(RoomExtraWidthChance)){
				width += RoomExtraWidth;
			}
			while(RoomsMustHaveOddHeightAndWidth && (height % 2 == 0 || width % 2 == 0)){
				if(height % 2 == 0){
					height = Between(RoomHeightMin,RoomHeightMax);
					if(PercentChance(RoomExtraHeightChance)){
						height += RoomExtraHeight;
					}
				}
				if(width % 2 == 0){
					width = Between(RoomWidthMin,RoomWidthMax);
					if(PercentChance(RoomExtraWidthChance)){
						width += RoomExtraWidth;
					}
				}
			}
			int h_offset = 0;
			int w_offset = 0;
			if(height % 2 == 0){
				h_offset = Roll(2) - 1;
			}
			if(width % 2 == 0){
				w_offset = Roll(2) - 1;
			}
			switch(dir){
			case 7:
				rr -= height-1;
				rc -= width-1;
				break;
			case 9:
				rr -= height-1;
				break;
			case 1:
				rc -= width-1;
				break;
			case 8:
				rr -= height-1;
				rc -= (width/2) - w_offset;
				break;
			case 2:
				rc -= (width/2) - w_offset;
				break;
			case 4:
				rr -= (height/2) - h_offset;
				rc -= width-1;
				break;
			case 6:
				rr -= (height/2) - h_offset;
				break;
			}
			dir = 3; //does nothing at the moment
			bool inbounds = true;
			for(int i=rr;i<rr+height && inbounds;++i){
				for(int j=rc;j<rc+width;++j){
					if(!BoundsCheck(i,j)){
						inbounds = false;
						break;
					}
				}
			}
			if(inbounds){
				CellType[,] submap = new CellType[height,width];
				for(int i=0;i<height;++i){
					for(int j=0;j<width;++j){
						submap[i,j] = map[i+rr,j+rc];
					}
				}
				bool good = true;
				for(int i=0;i<height && good;++i){
					for(int j=0;j<width && good;++j){
						bool place_here = false;
						switch(map[i+rr,j+rc]){
						case CellType.CorridorHorizontal:
						case CellType.CorridorVertical:
						case CellType.CorridorIntersection:
							if(AllowRoomsToOverwriteCorridors){
								place_here = true;
							}
							else{
								good = false;
							}
							break;
						case CellType.RoomEdge:
						case CellType.RoomCorner:
						case CellType.RoomInteriorCorner:
						case CellType.RoomInterior:
							if(AllowRoomsToOverwriteRooms){
								place_here = true;
							}
							else{
								good = false;
							}
							break;
						default:
							place_here = true;
							break;
						}
						if(place_here){
							int total = 0;
							if(i == 0){ ++total; }
							if(i == height-1){ ++total; }
							if(j == 0){ ++total; }
							if(j == width-1){ ++total; }
							switch(total){
							case 0:
								map[i+rr,j+rc] = CellType.RoomInterior;
								break;
							case 1:
								map[i+rr,j+rc] = CellType.RoomEdge;
								break;
							case 2:
								map[i+rr,j+rc] = CellType.RoomCorner;
								break;
							default:
								map[i+rr,j+rc] = CellType.InterestingLocation; //this indicates an error
								break;
							}
						}
					}
				}
				for(int i=-1;i<height+1 && good;++i){ 
					for(int j=-1;j<width+1 && good;++j){
						if(!IsLegal(i+rr,j+rc)){
							good = false;
						}
					}
				}
				if(!good){ //if this addition is illegal...
					for(int i=0;i<height;++i){
						for(int j=0;j<width;++j){
							map[i+rr,j+rc] = submap[i,j];
						}
					}
				}
				else{
					return true;
				}
			}
			return false;
		}
		public void ConnectDiagonals(){ ConnectDiagonals(false); }
		public void ConnectDiagonals(bool force_connection){
			List<pos> walls = new List<pos>();
			for(int i=1;i<H-2;++i){
				for(int j=1;j<W-2;++j){
					if(map[i,j].IsPassable() && map[i,j+1].IsWall()){
						if(map[i+1,j].IsWall() && map[i+1,j+1].IsPassable()){
							walls.Add(new pos(i,j+1));
							walls.Add(new pos(i+1,j));
						}
					}
					else{
						if(map[i,j].IsWall() && map[i,j+1].IsPassable()){
							if(map[i+1,j].IsPassable() && map[i+1,j+1].IsWall()){
								walls.Add(new pos(i,j));
								walls.Add(new pos(i+1,j+1));
							}
						}
					}
					if(walls.Count > 0){
						pos wall0 = walls[0];
						pos wall1 = walls[1];
						while(walls.Count > 0){
							pos p = walls.RemoveRandom();
							int direction_of_other_wall = 0;
							for(int ii=0;ii<8;++ii){
								pos other_wall = new pos(-1,-1);
								if(p.row == wall0.row && p.col == wall0.col){
									other_wall = wall1;
								}
								else{
									other_wall = wall0;
								}
								if(p.PosInDir(N.RotateDir(true,ii)).row == other_wall.row && p.PosInDir(N.RotateDir(true,ii)).col == other_wall.col){
									direction_of_other_wall = N.RotateDir(true,ii);
								}
							}
							bool good = true;
							for(int ii=3;ii<=5;++ii){
								if(map[p.PosInDir(direction_of_other_wall.RotateDir(true,ii))].IsWall() == false){
									good = false;
									break;
								}
							}
							int consecutive_walls = p.ConsecutiveAdjacent(x => map[x].IsWall());
							if(consecutive_walls >= 4 || good || force_connection){
								map[p] = CellType.CorridorIntersection;
								if(IsLegal(p) || force_connection){
									walls.Clear();
								}
								else{
									map[p] = CellType.Wall;
								}
							}
						}
					}
				}
			}
		}
		public void RemoveDeadEndCorridors(){
			bool changed = true;
			while(changed){
				changed = false;
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						if(map[i,j].IsPassable()){
							pos p = new pos(i,j);
							int total=0;
							foreach(int dir in FourDirections){
								if(!BoundsCheck(p.PosInDir(dir),true) || map[p.PosInDir(dir)].IsWall()){
									++total;
								}
							}
							if(total >= 3){
								map[i,j] = CellType.Wall;
								changed = true;
							}
						}
					}
				}
			}
		}
		public void RemoveUnconnectedAreas(){ //leaving only the largest
			int[,] num = new int[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(map[i,j].IsPassable()){
						num[i,j] = 0;
					}
					else{
						num[i,j] = -1;
					}
				}
			}
			int count = 0;
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(num[i,j] == 0){
						count++;
						num[i,j] = count;
						bool changed = true;
						while(changed){
							changed = false;
							for(int s=0;s<H;++s){
								for(int t=0;t<W;++t){
									if(num[s,t] == count){
										for(int ds=-1;ds<=1;++ds){
											for(int dt=-1;dt<=1;++dt){
												if(BoundsCheck(s+ds,t+dt,true) && num[s+ds,t+dt] == 0){
													num[s+ds,t+dt] = count;
													changed = true;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			int biggest_area = -1;
			int size_of_biggest_area = 0;
			for(int k=1;k<=count;++k){
				int size = 0;
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						if(num[i,j] == k){
							size++;
						}
					}
				}
				if(size > size_of_biggest_area){
					size_of_biggest_area = size;
					biggest_area = k;
				}
			}
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(num[i,j] != biggest_area){
						if(!map[i,j].IsPassable()){
							pos p = new pos(i,j);
							bool make_wall = true;
							foreach(pos neighbor in p.PositionsAtDistance(1)){
								if(neighbor.BoundsCheck(map) && num[neighbor.row,neighbor.col] == biggest_area){
									make_wall = false;
									break;
								}
							}
							if(make_wall){
								map[i,j] = CellType.Wall;
							}
						}
						else{
							map[i,j] = CellType.Wall;
						}
					}
				}
			}
		}
		public void AddLake(bool allow_disconnected_lakes){
			bool trees = CoinFlip();
			while(true){
				Dungeon shape = new Dungeon(21,21);
				shape[10,10] = CellType.DeepWater;
				for(bool done=false;!done;){
					pos p = new pos(Roll(19),Roll(19));
					if(shape[p].Is(CellType.Wall)){
						bool good = false;
						foreach(int dir in FourDirections){
							if(shape[p.PosInDir(dir)].Is(CellType.DeepWater)){
								good = true;
								break;
							}
						}
						if(good){
							if(p.ApproximateEuclideanDistanceFromX10(10,10) < 100){
								if(PercentChance(100 - p.ApproximateEuclideanDistanceFromX10(10,10))){
									shape[p] = CellType.DeepWater;
									if(p.row == 1 || p.col == 1 || p.row == 19 || p.col == 19){
										done = true;
									}
								}
							}
						}
					}
				}
				shape.RemoveDeadEndCorridors();
				shape.ApplyCellularAutomataXYRule(5);
				for(int i=0;i<21;++i){
					for(int j=0;j<21;++j){
						if(!shape[i,j].IsWall()){
							bool done = false;
							for(int s=0;s<21 && !done;++s){
								for(int t=0;t<21 && !done;++t){
									if(shape[s,t].IsWall() && new pos(i,j).ApproximateEuclideanDistanceFromX10(s,t) < 20){
										shape[i,j] = CellType.ShallowWater;
										done = true;
									}
									else{
										if(shape[s,t].IsWall() && new pos(i,j).ApproximateEuclideanDistanceFromX10(s,t) == 20){
											if(CoinFlip()){
												shape[i,j] = CellType.ShallowWater;
												done = true;
											}
										}
									}
								}
							}
							if(!done){
								shape[i,j] = CellType.DeepWater;
							}
						}
					}
				}
				int start_r = 999; //these are for the position of the lake within the "shape" variable
				int start_c = 999;
				int end_r = -1;
				int end_c = -1;
				for(int i=0;i<21;++i){
					for(int j=0;j<21;++j){
						if(!shape[i,j].IsWall()){
							if(i < start_r){
								start_r = i;
							}
							if(i > end_r){
								end_r = i;
							}
							if(j < start_c){
								start_c = j;
							}
							if(j > end_c){
								end_c = j;
							}
						}
					}
				}
				int lake_h = (end_r - start_r) + 1;
				int lake_w = (end_c - start_c) + 1;
				CellType[,] old_cells = new CellType[lake_h,lake_w];
				for(int tries=0;tries<200;++tries){
					int rr = Roll(H - lake_h - 1);
					int rc = Roll(W - lake_w - 1);
					for(int i=0;i<lake_h;++i){
						for(int j=0;j<lake_w;++j){
							old_cells[i,j] = map[i+rr,j+rc];
							if(shape[i+start_r,j+start_c].Is(CellType.ShallowWater,CellType.DeepWater) && !map[i+rr,j+rc].Is(CellType.DeepWater)){
								map[i+rr,j+rc] = shape[i+start_r,j+start_c];
							}
						}
					}
					if(allow_disconnected_lakes || this.IsFullyConnected()){
						for(int i=0;i<lake_h;++i){
							for(int j=0;j<lake_w;++j){
								if(map[i+rr,j+rc] == CellType.ShallowWater){
									if(trees){
										map[i+rr,j+rc] = CellType.Tree;
									}
								}
								if(map[i+rr,j+rc] == CellType.DeepWater){
									if(trees){
										map[i+rr,j+rc] = CellType.RoomInterior;
									}
								}
							}
						}
						return;
					}
					else{
						for(int i=0;i<lake_h;++i){ //undo what we just did and try again
							for(int j=0;j<lake_w;++j){
								map[i+rr,j+rc] = old_cells[i,j];
							}
						}
					}
				}
			}
		}
		public void AddTrees(int number_of_trees,int minimum_distance_from_walls,int minimum_distance_from_other_trees){
			List<pos> locations = new List<pos>(); //if number_of_trees is negative, keep going until no more can be placed.
			while(true){
				bool[,] valid = new bool[H,W];
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						valid[i,j] = true;
					}
				}
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						if(map[i,j].IsWall()){
							for(int s=i-minimum_distance_from_walls;s<=i+minimum_distance_from_walls;++s){
								for(int t=j-minimum_distance_from_walls;t<=j+minimum_distance_from_walls;++t){
									if(BoundsCheck(s,t)){
										valid[s,t] = false;
									}
								}
							}
						}
						else{
							if(map[i,j].Is(CellType.Tree)){
								for(int s=i-minimum_distance_from_other_trees;s<=i+minimum_distance_from_other_trees;++s){
									for(int t=j-minimum_distance_from_other_trees;t<=j+minimum_distance_from_other_trees;++t){
										if(BoundsCheck(s,t)){
											valid[s,t] = false;
										}
									}
								}
							}
						}
					}
				}
				locations.Clear();
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						if(valid[i,j] && map[i,j].IsFloor()){
							locations.Add(new pos(i,j));
						}
					}
				}
				if(locations.Count == 0){
					return; //nothing left to do
				}
				for(int tries_left = Math.Min(50,locations.Count);tries_left > 0;--tries_left){
					pos p = locations.RemoveRandom();
					int i = p.row;
					int j = p.col;
					bool good = true;
					for(int s=i-minimum_distance_from_walls;s<=i+minimum_distance_from_walls;++s){
						for(int t=j-minimum_distance_from_walls;t<=j+minimum_distance_from_walls;++t){
							if(BoundsCheck(s,t) && map[s,t].IsWall()){
								good = false;
							}
						}
					}
					for(int s=i-minimum_distance_from_other_trees;s<=i+minimum_distance_from_other_trees;++s){
						for(int t=j-minimum_distance_from_other_trees;t<=j+minimum_distance_from_other_trees;++t){
							if(BoundsCheck(s,t) && map[s,t].Is(CellType.Tree)){
								good = false;
							}
						}
					}
					if(good){
						map[p] = CellType.Tree;
						++tries_left;
						if(number_of_trees > 0){
							--number_of_trees;
							if(number_of_trees == 0){
								return;
							}
						}
					}
				}
			}
		}
		public void FillWithRandomWalls(int percent_chance_of_wall){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(PercentChance(percent_chance_of_wall)){
						map[i,j] = CellType.Wall;
					}
					else{
						map[i,j] = CellType.RoomInterior;
					}
				}
			}
		}
		/*public void FillUsingDiamondSquareAlgorithm(){
			int[,] a = GetDiamondSquarePlasmaFractal(H,W);
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					int k = a[i,j];
					int[] tile_num = new int[]{-30,-23,-8,-4,6,14,19,23};
					char[] tile_char = new char[]{'~','~','~',',','.',';','^','#','#'};
					ConsoleColor[] tile_color = new ConsoleColor[]{ConsoleColor.DarkBlue,ConsoleColor.DarkCyan,ConsoleColor.Blue,ConsoleColor.Yellow,ConsoleColor.Green,ConsoleColor.DarkGreen,ConsoleColor.DarkRed,ConsoleColor.DarkGray,ConsoleColor.White};
					//int[] tile_num = new int[]{-23,-8,-4,6,14,23};
					//char[] tile_char = new char[]{'~','~',',','.',';','#','#'};
					//ConsoleColor[] tile_color = new ConsoleColor[]{ConsoleColor.DarkBlue,ConsoleColor.Blue,ConsoleColor.Yellow,ConsoleColor.Green,ConsoleColor.DarkGreen,ConsoleColor.DarkGray,ConsoleColor.White};
					bool use_default = true;
					for(int s=0;s<tile_num.GetLength(0);++s){
						if(k < tile_num[s]){
							Screen.Write(i+2,j+3,tile_char[s],tile_color[s]);
							use_default = false;
							break;
						}
					}
					if(use_default){
						int l = tile_num.GetLength(0);
						Screen.Write(i+2,j+3,tile_char[l],tile_color[l]);
					}
				}
			}
		}*/
		private static int[,] GetDiamondSquarePlasmaFractal(int height,int width,int roughness){
			int[,] a = GetDiamondSquarePlasmaFractal(height*roughness,width*roughness);
			int[,] result = new int[height,width];
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					result[i,j] = a[i*roughness,j*roughness];
				}
			}
			return result;
		}
		private static int[,] GetDiamondSquarePlasmaFractal(int height,int width){
			int size = 1;
			int max = Math.Max(height,width);
			while(size < max){
				size *= 2; //find the smallest square that the dungeon fits into
			}
			size++; //diamond-square needs 2^x + 1
			int[,] a = GetDiamondSquarePlasmaFractal(size);
			int[,] result = new int[height,width];
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					result[i,j] = a[i,j];
				}
			}
			return result;
		}
		private static int[,] GetDiamondSquarePlasmaFractal(int size){
			int[,] a = new int[size,size];
			a[0,0] = 128;
			a[0,size-1] = 128;
			a[size-1,0] = 128;
			a[size-1,size-1] = 128;
			int step = 1;
			while(DiamondStep(a,step)){
				SquareStep(a,step);
				++step;
			}
			int total = 0;
			for(int i=0;i<size;++i){
				for(int j=0;j<size;++j){
					total += a[i,j];
				}
			}
			int mean = total / (size*size);
			for(int i=0;i<size;++i){
				for(int j=0;j<size;++j){
					a[i,j] -= mean;
				}
			}
			return a;
		}
		private static bool ArrayBoundsCheck(int[,] a,int r,int c){
			if(r < 0 || r > a.GetUpperBound(0) || c < 0 || c > a.GetUpperBound(1)){
				return false;
			}
			return true;
		}
		private static bool DiamondStep(int[,] a,int step){ //step starts at 1
			int divisions = 1; //divisions^2 is the number of squares
			while(step > 1){
				divisions *= 2;
				--step;
			}
			int increment = a.GetUpperBound(0) / divisions;
			if(increment == 1){
				return false; //done!
			}
			for(int i=0;i<divisions;++i){
				for(int j=0;j<divisions;++j){
					int total = 0;
					total += a[i*increment,j*increment];
					total += a[i*increment,(j+1)*increment];
					total += a[(i+1)*increment,j*increment];
					total += a[(i+1)*increment,(j+1)*increment];
					total = total / 4;
					total += Roll((128 / divisions) + 1) - ((64 / divisions) + 1);
					a[i*increment + increment/2,j*increment + increment/2] = total;
				}
			}
			return true;
		}
		private static void SquareStep(int[,] a,int step){
			int divisions = 1;
			while(step > 0){
				divisions *= 2;
				--step;
			}
			int increment = a.GetUpperBound(0) / divisions;
			for(int i=0;i<=divisions;++i){
				for(int j=0;j<=divisions;++j){
					if((i+j)%2 == 1){
						int total = 0;
						int num = 0;
						if(ArrayBoundsCheck(a,(i-1)*increment,j*increment)){
							++num;
							total += a[(i-1)*increment,j*increment];
						}
						if(ArrayBoundsCheck(a,i*increment,(j-1)*increment)){
							++num;
							total += a[i*increment,(j-1)*increment];
						}
						if(ArrayBoundsCheck(a,i*increment,(j+1)*increment)){
							++num;
							total += a[i*increment,(j+1)*increment];
						}
						if(ArrayBoundsCheck(a,(i+1)*increment,j*increment)){
							++num;
							total += a[(i+1)*increment,j*increment];
						}
						total = total / num;
						total += Roll((256 / divisions) + 1) - ((128 / divisions) + 1); //doubled because divisions are doubled
						a[i*increment,j*increment] = total;
					}
				}
			}
		}
		/*public void FillUsingFlowDisplacedRockLayers(){
			while(true){
				double layer_width = (double)(Roll(3)+5); //was Roll(3) + 5, heh
				double layer_angle = (double)(Roll(360) - 1);
				double center_row = ((double)(H-1)) / 2.0f;
				double center_col = ((double)(W-1)) / 2.0f;
				int[,] row_displacement = GetDiamondSquarePlasmaFractal(H,W);
				int[,] col_displacement = GetDiamondSquarePlasmaFractal(H,W);
				int[,] layer;
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						row_displacement[i,j] /= 2; //was * 1
						col_displacement[i,j] /= 2;
					}
				}
				layer = new int[H,W];
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						int row = i + row_displacement[i,j];
						int col = j + col_displacement[i,j];
						double opp = Math.Abs(center_row - (double)row);
						double adj = Math.Abs(center_col - (double)col);
						double hyp = Math.Sqrt(opp*opp + adj*adj);
						double point_angle_radians = Math.Asin(opp/hyp);
						double point_angle = point_angle_radians * 180.0 / Math.PI;
						if(row < center_row){
							if(col < center_col){
								point_angle += 180.0;
							}
							else{
								point_angle = 360.0 - point_angle;
							}
						}
						else{
							if(col < center_col){
								point_angle = 180.0 - point_angle;
							}
						}
						double angle_difference = point_angle - layer_angle;
						double angle_difference_radians = angle_difference * Math.PI / 180.0;
						double distance_from_line = Math.Sin(angle_difference_radians) * hyp;
						for(int layer_num = 1;;++layer_num){
							if(distance_from_line > layer_width * (layer_num-1) && distance_from_line <= layer_width * layer_num){
								layer[i,j] = layer_num;
								break;
							}
							if(distance_from_line < layer_width * -(layer_num-1) && distance_from_line >= layer_width * -layer_num){
								layer[i,j] = -layer_num;
								break;
							}
						}
					}
				}
				int[,] rd2 = new int[H,W];
				int[,] cd2 = new int[H,W];
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						int k = (Math.Abs(layer[i,j])) % 14;
						++k; //now 1-14
						if(layer[i,j] < 0){
							k = 15 - k;
						}
						if(k == 7){
							k = 15;
						}
						Screen.Write(i+2,j+3,'#',(ConsoleColor)k);
						int new_row = i - row_displacement[i,j];
						int new_col = j - col_displacement[i,j];
						if(ArrayBoundsCheck(rd2,new_row,j)){
							rd2[i,j] = row_displacement[new_row,j];
						}
						else{
							rd2[i,j] = 0;
						}
						if(ArrayBoundsCheck(cd2,i,new_col)){
							cd2[i,j] = col_displacement[i,new_col];
						}
						else{
							cd2[i,j] = 0;
						}
						if(ArrayBoundsCheck(rd2,i,new_col)){
							rd2[i,j] = row_displacement[i,new_col];
						}
						else{
							rd2[i,j] = 0;
						}
						if(ArrayBoundsCheck(cd2,new_row,j)){
							cd2[i,j] = col_displacement[new_row,j];
						}
						else{
							cd2[i,j] = 0;
						}
						if(ArrayBoundsCheck(rd2,new_row,new_col)){
							rd2[i,j] = row_displacement[new_row,new_col];
							cd2[i,j] = col_displacement[new_row,new_col];
						}
						else{
							rd2[i,j] = 0;
							cd2[i,j] = 0;
						}
					}
				}
				row_displacement = rd2;
				col_displacement = cd2;
				Console.ReadKey(true);
			}
			Environment.Exit(0);
		}*/
		public void ApplyCellularAutomataXYRule(int target_number_of_walls){
			CellType[,] result = new CellType[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					pos p = new pos(i,j);
					int num_walls = p.AdjacentPositionsClockwise().Where(x => !BoundsCheck(x,true) || map[x].IsWall()).Count;
					if(map[p].IsWall()){
						++num_walls;
					}
					if(num_walls >= target_number_of_walls){
						result[i,j] = CellType.Wall;
					}
					else{
						result[i,j] = CellType.RoomInterior;
					}
				}
			}
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					map[i,j] = result[i,j];
				}
			}
		}
		public bool PlaceFeature(CellType feature, int num,U.BooleanPositionDelegate conditions){
			List<pos> valid = map.AllPositions().Where(x=>map[x] != feature && conditions(x));
			int consecutive_failures = 0;
			for(int i=0;i<num;++i){
				if(valid.Count > 0){
					pos p = valid.Random();
					if(map[p] != feature && conditions(p)){
						map[p] = feature;
					}
					else{
						--i; //don't count this one toward the total
						if(++consecutive_failures == 10){
							consecutive_failures = 0;
							List<pos> temp = valid.Where(x=>map[x] != feature && conditions(x));
							valid = temp;
						}
					}
				}
				else{
					return false;
				}
			}
			return true;
		}
		public bool PlaceShape(Dungeon room_to_be_placed){ return PlaceShape(room_to_be_placed,true,true,false); }
		public bool PlaceShape(Dungeon room_to_be_placed,bool force_connection,bool force_cardinal_direction_connection,bool allow_room_placement_under_features,params CellType[] allowed_to_overwrite){
			int first_row = H+1;
			int first_col = W+1; //todo: this doesn't respect the map boundaries yet - it can overwrite the edge walls with the room.
			int last_row = -1;
			int last_col = -1;
			for(int i=0;i<room_to_be_placed.H;++i){
				for(int j=0;j<room_to_be_placed.W;++j){
					if(!room_to_be_placed[i,j].IsWall()){
						if(i < first_row){
							first_row = i;
						}
						if(i > last_row){
							last_row = i;
						}
						if(j < first_col){
							first_col = j;
						}
						if(j > last_col){
							last_col = j;
						}
					}
				}
			}
			Dungeon room = new Dungeon((last_row - first_row) + 1,(last_col - first_col) + 1);
			int rh = room.H;
			int rw = room.W; //'force connection' means that the added room must connect somehow to the existing passable tiles of the map.
			for(int i=0;i<rh;++i){
				for(int j=0;j<rw;++j){
					room[i,j] = room_to_be_placed[i+first_row,j+first_col];
				}
			}
			List<pos> positions = map.AllPositions().Where(x=>x.row < H-rh && x.col < W-rw).Randomize(); //'allow room placement under features' means, if the room has a floor and the map has a feature in the same location, that won't disqualify placement - instead, if the room passes, the feature will appear instead of the floor.
			Dungeon connected = new Dungeon(rh,rw);
			for(int num=0;num<positions.Count;++num){
				pos p = positions[num];
				bool bad_connection = force_connection;
				for(int i=0;i<rh;++i){
					for(int j=0;j<rw;++j){
						if(room[i,j].IsPassable()){
							foreach(pos neighbor in new pos(i,j).PositionsWithinDistance(1)){
								if(!force_cardinal_direction_connection){ //if this bool is set, check later.
									if(map[p.row+neighbor.row,p.col+neighbor.col].IsPassable()){
										bad_connection = false;
									}
								}
								if(connected.BoundsCheck(neighbor)){
									connected[neighbor] = map[p.row+neighbor.row,p.col+neighbor.col];
								}
							}
							if(force_cardinal_direction_connection){
								foreach(pos neighbor in new pos(i,j).CardinalAdjacentPositions()){
									if(connected.BoundsCheck(neighbor) && map[p.row+neighbor.row,p.col+neighbor.col].IsPassable()){
										bad_connection = false;
									}
								}
							}
						}
					}
				}
				if(!bad_connection && connected.IsFullyConnected()){
					bool valid = true;
					for(int i=0;i<rh && valid;++i){
						for(int j=0;j<rw && valid;++j){
							if(room[i,j] != CellType.Wall){
								bool this_position_valid = false;
								if(map[p.row+i,p.col+j].IsWall()){
									this_position_valid = true;
								}
								if(map[p.row+i,p.col+j].Is(allowed_to_overwrite)){
									this_position_valid = true;
								}
								if(map[p.row+i,p.col+j] == room[i,j]){
									this_position_valid = true;
								}
								if(map[p.row+i,p.col+j].IsFloor() && room[i,j].IsFloor()){
									this_position_valid = true;
								}
								if(room[i,j].IsFloor() && allow_room_placement_under_features){
									this_position_valid = true;
								}
								if(!this_position_valid){
									valid = false;
								}
							}
						}
					}
					if(valid){
						for(int i=0;i<rh;++i){
							for(int j=0;j<rw;++j){
								if(room[i,j] != CellType.Wall){
									if(map[p.row+i,p.col+j].IsWall() || map[p.row+i,p.col+j].Is(allowed_to_overwrite)){
										map[p.row+i,p.col+j] = room[i,j];
									}
								}
							}
						}
						return true;
					}
				}
			}
			return false;
		}
		/*public bool ConnectRoom(Dungeon room_to_be_placed,bool allow_corner_connections){ //tries to connect a room to an existing map by putting a single-tile "door" between them
			int first_row = H+1;
			int first_col = W+1;
			int last_row = -1;
			int last_col = -1;
			for(int i=0;i<room_to_be_placed.H;++i){
				for(int j=0;j<room_to_be_placed.W;++j){
					if(!room_to_be_placed[i,j].IsWall()){
						if(i < first_row){
							first_row = i;
						}
						if(i > last_row){
							last_row = i;
						}
						if(j < first_col){
							first_col = j;
						}
						if(j > last_col){
							last_col = j;
						}
					}
				}
			}
			Dungeon room = new Dungeon((last_row - first_row) + 1,(last_col - first_col) + 1);
			int rh = room.H;
			int rw = room.W; //first, remove any wall borders around the room...
			List<pos> valid_doors = new List<pos>();
			for(int i=0;i<rh;++i){
				for(int j=0;j<rw;++j){
					room[i,j] = room_to_be_placed[i+first_row,j+first_col];
					if(i == 0 || i == rh-1 || j == 0 || j == rw-1){
						if(room[i,j].IsPassable() && (room[i,j] != CellType.RoomCorner || allow_corner_connections)){
							valid_doors.Add(new pos(i,j));
						}
					}
				}
			}
			if(valid_doors.Count == 0){
				return false;
			}
			//incomplete
			//
			//
			return false;
		}*/
		public void MakeCavesMoreRectangular(int minimum_height_and_width_of_resulting_room){
			ForEachRoom(list => {
				int start_r = list.WhereLeast(x=>x.row)[0].row;
				int end_r = list.WhereGreatest(x=>x.row)[0].row;
				int start_c = list.WhereLeast(x=>x.col)[0].col;
				int end_c = list.WhereGreatest(x=>x.col)[0].col;
				List<pos> convert_to_walls = new List<pos>();
				for(int i=start_r;i<=end_r;++i){ //first, check each row...
					for(int j=start_c;j<=end_c;++j){
						List<pos> this_row = new List<pos>();
						while(j <= end_c && list.Contains(new pos(i,j))){
							this_row.Add(new pos(i,j));
							++j;
						}
						if(this_row.Count < minimum_height_and_width_of_resulting_room){
							foreach(pos p in this_row){
								convert_to_walls.Add(p);
							}
						}
					}
				}
				for(int j=start_c;j<=end_c;++j){ //...then each column
					for(int i=start_r;i<=end_r;++i){
						List<pos> this_col = new List<pos>();
						while(i <= end_r && list.Contains(new pos(i,j))){
							this_col.Add(new pos(i,j));
							++i;
						}
						if(this_col.Count < minimum_height_and_width_of_resulting_room){
							foreach(pos p in this_col){
								convert_to_walls.Add(p);
							}
						}
					}
				}
				foreach(pos p in convert_to_walls){
					map[p] = CellType.Wall;
				}
				return true;
			});
		}
		public void SmoothCorners(int percent_chance){
			List<pos> corners = new List<pos>();
			for(int i=1;i<H-1;++i){
				for(int j=1;j<W-1;++j){
					if(IsCornerFloor(i,j)){
						corners.Add(new pos(i,j));
					}
				}
			}
			while(corners.Count > 0){
				pos p = corners.RemoveRandom();
				corners.RemoveWhere(x=>x.DistanceFrom(p) <= 1);
				if(R.PercentChance(percent_chance)){
					map[p] = CellType.Wall;
				}
			}
		}
		public void SharpenCorners(){
			for(int i=1;i<H-1;++i){
				for(int j=1;j<W-1;++j){
					pos p = new pos(i,j);
					if(p.CardinalAdjacentPositions().Where(x=>map[x].IsRoomType()).Count == 2 && p.ConsecutiveAdjacent(x=>!map[x].IsRoomType()) == 5){
						map[p] = CellType.RoomCorner;
					}
				}
			}
		}
		public enum PillarArrangement{Single,Full,Corners,Row,StatueCorners,StatueEdges};
		public void AlterRooms(int no_change_freq,int add_pillars_freq,int cross_room_freq,int cave_widen_freq,int cave_fill_freq){
			List<int> modification = new List<int>();
			for(int i=0;i<no_change_freq;++i){
				modification.Add(0);
			}
			for(int i=0;i<add_pillars_freq;++i){
				modification.Add(1);
			}
			for(int i=0;i<cross_room_freq;++i){
				modification.Add(2);
			}
			for(int i=0;i<cave_widen_freq;++i){
				modification.Add(3);
			}
			for(int i=0;i<cave_fill_freq;++i){
				modification.Add(4);
			}
			if(modification.Count == 0){
				return;
			}
			ForEachRectangularRoom((start_r,start_c,end_r,end_c) => {
				int mod = modification.Random();
				switch(mod){
				case 0:
					return true;
				case 1:
				{
					int height = end_r - start_r + 1;
					int width = end_c - start_c + 1;
					if(height > 3 || width > 3){
						List<PillarArrangement> layouts = new List<PillarArrangement>();
						if(height % 2 == 1 && width % 2 == 1){
							layouts.Add(PillarArrangement.Single);
						}
						if((height % 2 == 1 || width % 2 == 1) && height != 4 && width != 4){
							layouts.Add(PillarArrangement.Row);
						}
						if(height >= 5 && width >= 5){
							layouts.Add(PillarArrangement.Corners);
						}
						if(height > 2 && width > 2 && height != 4 && width != 4){
							layouts.Add(PillarArrangement.Full);
						}
						if((width % 2 == 1 && width >= 5) || (height % 2 == 1 && height >= 5)){
							layouts.Add(PillarArrangement.StatueEdges);
						}
						if(layouts.Count == 0 || CoinFlip()){ //otherwise they're too common
							layouts.Add(PillarArrangement.StatueCorners);
						}
						if(layouts.Count > 0){
							CellType pillar = CellType.Pillar;
							switch(layouts.Random()){
							case PillarArrangement.Single:
								map[(start_r + end_r)/2,(start_c + end_c)/2] = pillar;
								break;
							case PillarArrangement.Row:
							{
								bool vertical;
								if(width % 2 == 1 && height % 2 == 0){
									vertical = true;
								}
								else{
									if(height % 2 == 1 && width % 2 == 0){
										vertical = false;
									}
									else{
										vertical = CoinFlip();
									}
								}
								if(vertical){
									if(height % 2 == 1){
										for(int i=start_r+1;i<=end_r-1;i+=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
									}
									else{
										int offset = 0;
										if(height % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_r+1+offset;i<(start_r + end_r)/2;i+=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
										for(int i=end_r-1-offset;i>(start_r + end_r)/2+1;i-=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
									}
								}
								else{
									if(width % 2 == 1){
										for(int i=start_c+1;i<=end_c-1;i+=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
									}
									else{
										int offset = 0;
										if(width % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_c+1+offset;i<(start_c + end_c)/2;i+=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
										for(int i=end_c-1-offset;i>(start_c + end_c)/2+1;i-=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
									}
								}
								break;
							}
							case PillarArrangement.Corners:
							{
								int v_offset = 0;
								int h_offset = 0;
								if(height % 4 == 0){
									v_offset = Roll(2) - 1;
								}
								if(width % 4 == 0){
									h_offset = Roll(2) - 1;
								}
								map[start_r + 1 + v_offset,start_c + 1 + h_offset] = pillar;
								map[start_r + 1 + v_offset,end_c - 1 - h_offset] = pillar;
								map[end_r - 1 - v_offset,start_c + 1 + h_offset] = pillar;
								map[end_r - 1 - v_offset,end_c - 1 - h_offset] = pillar;
								break;
							}
							case PillarArrangement.Full:
							{
								int v_offset = 0;
								int h_offset = 0;
								if(height % 4 == 0){
									v_offset = Roll(2) - 1;
								}
								if(width % 4 == 0){
									h_offset = Roll(2) - 1;
								}
								int half_r = (start_r + end_r)/2;
								int half_c = (start_c + end_c)/2;
								int half_r_offset = (start_r + end_r + 1)/2;
								int half_c_offset = (start_c + end_c + 1)/2;
								for(int i=start_r+1+v_offset;i<half_r;i+=2){
									for(int j=start_c+1+h_offset;j<half_c;j+=2){
										map[i,j] = pillar;
									}
								}
								for(int i=start_r+1+v_offset;i<half_r;i+=2){
									for(int j=end_c-1-h_offset;j>half_c_offset;j-=2){
										map[i,j] = pillar;
									}
								}
								for(int i=end_r-1-v_offset;i>half_r_offset;i-=2){
									for(int j=start_c+1+h_offset;j<half_c;j+=2){
										map[i,j] = pillar;
									}
								}
								for(int i=end_r-1-v_offset;i>half_r_offset;i-=2){
									for(int j=end_c-1-h_offset;j>half_c_offset;j-=2){
										map[i,j] = pillar;
									}
								}
								if((width+1) % 4 == 0){
									if(height % 2 == 1){
										for(int i=start_r+1;i<=end_r-1;i+=2){
											map[i,half_c] = pillar;
										}
									}
									else{
										int offset = 0;
										if(height % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_r+1+offset;i<half_r;i+=2){
											map[i,half_c] = pillar;
										}
										for(int i=end_r-1-offset;i>half_r_offset;i-=2){
											map[i,half_c] = pillar;
										}
									}
								}
								if((height+1) % 4 == 0){
									if(width % 2 == 1){
										for(int i=start_c+1;i<=end_c-1;i+=2){
											map[half_r,i] = pillar;
										}
									}
									else{
										int offset = 0;
										if(width % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_c+1+offset;i<half_c;i+=2){
											map[half_r,i] = pillar;
										}
										for(int i=end_c-1-offset;i>half_c_offset;i-=2){
											map[half_r,i] = pillar;
										}
									}
								}
								break;
							}
							case PillarArrangement.StatueCorners:
								map[start_r,start_c] = CellType.Statue;
								map[start_r,end_c] = CellType.Statue;
								map[end_r,start_c] = CellType.Statue;
								map[end_r,end_c] = CellType.Statue;
								break;
							case PillarArrangement.StatueEdges:
							{
								map[start_r,start_c] = CellType.Statue;
								map[start_r,end_c] = CellType.Statue;
								map[end_r,start_c] = CellType.Statue;
								map[end_r,end_c] = CellType.Statue;
								if(width % 2 == 1 && width > 3){
									int half_c = (start_c + end_c)/2;
									int corridors = new pos(start_r,half_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[start_r,half_c] = CellType.Statue;
									}
									corridors = new pos(end_r,half_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[end_r,half_c] = CellType.Statue;
									}
								}
								if(height % 2 == 1 && height > 3){
									int half_r = (start_r + end_r)/2;
									int corridors = new pos(half_r,start_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[half_r,start_c] = CellType.Statue;
									}
									corridors = new pos(half_r,end_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[half_r,end_c] = CellType.Statue;
									}
								}
								break;
							}
							default:
								break;
							}
						}
					}
					return true;
				}
				case 2:
				{
					int height = end_r - start_r + 1;
					int width = end_c - start_c + 1;
					if(height < 4 || width < 4){ //nothing happens until we get above 4x4
						return true;
					}
					int rows_to_convert = Roll((height/2)-1);
					int cols_to_convert = Roll((width/2)-1);
					if(rows_to_convert == 1 && cols_to_convert == 1){
						return true;
					}
					List<pos> blocked = new List<pos>();
					for(int i=start_r;i<=end_r;++i){
						for(int j=start_c;j<=end_c;++j){
							if((i < start_r + rows_to_convert || i > end_r - rows_to_convert) && (j < start_c + cols_to_convert || j > end_c - cols_to_convert)){
								pos p = new pos(i,j);
								foreach(pos neighbor in p.CardinalAdjacentPositions()){
									if(map[neighbor].IsCorridorType()){
										blocked.Add(p);
									}
								}
								map[i,j] = CellType.Wall;
							}
						}
					}
					blocked.Randomize();
					foreach(pos p in blocked){
						bool done = false;
						foreach(pos neighbor in p.CardinalAdjacentPositions()){
							if(map[neighbor].IsRoomType()){
								map[p] = CellType.RoomInterior;
								done = true;
								break;
							}
						}
						if(!done){
							List<int> valid_dirs = new List<int>();
							foreach(int dir in U.FourDirections){
								pos next = p.PosInDir(dir);
								while(next.row >= start_r && next.row <= end_r && next.col >= start_c && next.col <= end_c){
									if(next.CardinalAdjacentPositions().Any(x=>map[x].IsRoomType())){
										valid_dirs.Add(dir);
										break;
									}
									next = next.PosInDir(dir);
								}
							}
							int valid_dir = valid_dirs.Random();
							pos next2 = p.PosInDir(valid_dir);
							List<pos> new_corridor = new List<pos>{p};
							while(true){
								new_corridor.Add(next2);
								if(next2.CardinalAdjacentPositions().Any(x=>map[x].IsRoomType())){
									break;
								}
								next2 = next2.PosInDir(valid_dir);
							}
							foreach(pos p2 in new_corridor){
								map[p2] = CellType.RoomInterior;
							}
						}
					}
					return true;
				}
				case 3:
				{
					List<pos> list = map.PositionsWhere(x=>x.row >= start_r && x.row <= end_r && x.col >= start_c && x.col <= end_c);
					PosArray<CellType> old_map = new PosArray<CellType>(H,W);
					foreach(pos p in list){
						old_map[p] = map[p];
						map[p] = CellType.Wall;
					}
					PosArray<bool> rock = new PosArray<bool>(H,W);
					for(int i=0;i<H;++i){
						for(int j=0;j<W;++j){
							pos p = new pos(i,j);
							rock[p] = true;
							if(BoundsCheck(i,j,false)){
								foreach(pos neighbor in p.AdjacentPositionsClockwise()){
									if(map[neighbor] != CellType.Wall){
										rock[p] = false;
										break;
									}
								}
							}
						}
					}
					foreach(pos p in list){
						map[p] = CellType.RoomInterior; //todo: might this step be extraneous?
					}
					List<pos> frontier = new List<pos>();
					{
						PosArray<bool> in_list = new PosArray<bool>(H,W);
						foreach(pos p in list){
							in_list[p] = true;
						}
						for(int i=0;i<H;++i){
							for(int j=0;j<W;++j){
								pos p = new pos(i,j);
								if(in_list[p]){
									foreach(pos neighbor in p.PositionsAtDistance(1)){
										if(!in_list[neighbor]){
											frontier.Add(p);
											break;
										}
									}
								}
							}
						}
					}
					int fail_counter = 0;
					int num_added = 0;
					bool finished = false;
					while(!finished){
						if(frontier.Count == 0 || num_added >= 30){ //todo check this value
							finished = true;
							break;
						}
						pos f = frontier.RemoveRandom();
						foreach(pos neighbor in f.CardinalAdjacentPositions()){
							if(!BoundsCheck(neighbor,false) || !rock[neighbor.row,neighbor.col]){
								++fail_counter; //this might now be unreachable
								if(!BoundsCheck(neighbor,false)){
									fail_counter += 25; //fail quicker when against the edge of the map to prevent ugliness
								} //however, this doesn't actually fail as quickly as it should - i've overlooked something.
								if(fail_counter >= 50){
									finished = true;
									break;
								}
							}
							else{
								if(map[neighbor] != CellType.RoomInterior){
									map[neighbor] = CellType.RoomInterior;
									++num_added;
									bool add_neighbor = true;
									foreach(pos n2 in neighbor.CardinalAdjacentPositions()){
										if(!BoundsCheck(n2,false) || !rock[n2.row,n2.col]){
											add_neighbor = false;
											++fail_counter; //this might now be unreachable
											if(!BoundsCheck(neighbor,false)){
												fail_counter += 25; //fail quicker when against the edge of the map to prevent ugliness
											} //however, this doesn't actually fail as quickly as it should - i've overlooked something.
											if(fail_counter >= 50){
												finished = true;
											}
											break;
										}
									}
									if(finished){
										break;
									}
									if(add_neighbor){
										frontier.Add(neighbor);
									}
								}
							}
						}
					}
					foreach(pos p in list){
						map[p] = old_map[p];
					}
					return true;
				}
				case 4:
				{
					List<pos> list = map.PositionsWhere(x=>x.row >= start_r && x.row <= end_r && x.col >= start_c && x.col <= end_c);
					Dungeon room = new Dungeon((end_r - start_r) + 3,(end_c - start_c) + 3); //includes borders
					List<pos> map_exits = list.Where(x=>x.CardinalAdjacentPositions().Any(y=>map[y].IsCorridorType())); //grab the positions from list that have any adjacent corridor-type cells
					if(map_exits.Count < 2){
						return true;
					}
					List<pos> room_exits = new List<pos>();
					foreach(pos exit in map_exits){
						room_exits.Add(new pos(exit.row-start_r+1,exit.col-start_c+1));
					}
					int tries = 0;
					while(true){
						room.FillWithRandomWalls(25);
						room.ApplyCellularAutomataXYRule(3);
						room.ConnectDiagonals();
						room.RemoveDeadEndCorridors();
						room.RemoveUnconnectedAreas();
						bool exits_open = true;
						foreach(pos p in room_exits){
							if(!room[p].IsPassable()){
								exits_open = false;
							}
						}
						if(exits_open){
							for(int i=start_r;i<=end_r;++i){
								for(int j=start_c;j<=end_c;++j){
									if(list.Contains(new pos(i,j))){
										map[i,j] = room[(i-start_r)+1,(j-start_c)+1];
									}
								}
							}
							break;
						}
						++tries;
						if(tries > 50){
							return false;
						}
					}
					return true;
				}
				default:
					break;
				}
				return true;
			});
		}
		public void MakeCrossRooms(int percent_chance_per_rectangular_room,bool allow_corner_only_rooms){
			ForEachRectangularRoom((start_r,start_c,end_r,end_c) => {
				int height = end_r - start_r + 1;
				int width = end_c - start_c + 1;
				if(!PercentChance(percent_chance_per_rectangular_room) || height < 4 || width < 4){ //nothing happens until we get to 4x4
					return true;
				}
				int rows_to_convert = Roll((height/2)-1);
				int cols_to_convert = Roll((width/2)-1);
				if(!allow_corner_only_rooms && rows_to_convert == 1 && cols_to_convert == 1){
					return true;
				}
				for(int i=start_r;i<=end_r;++i){
					for(int j=start_c;j<=end_c;++j){
						if((i < start_r + rows_to_convert || i > end_r - rows_to_convert) && (j < start_c + cols_to_convert || j > end_c - cols_to_convert)){
							map[i,j] = CellType.Wall;
						}
					}
				}
				return true;
			});
		}
		public void Reflect(bool horizontal_axis,bool vertical_axis){
			int half_h = H/2;
			int half_w = W/2;
			if(horizontal_axis){
				if(vertical_axis){
					for(int i=0;i<half_h;++i){
						for(int j=0;j<half_w;++j){
							map[H-1-i,j] = map[i,j];
							map[i,W-1-j] = map[i,j];
							map[H-1-i,W-1-j] = map[i,j];
						}
					}
				}
				else{
					for(int i=0;i<half_h;++i){
						for(int j=0;j<W;++j){
							map[H-1-i,j] = map[i,j];
						}
					}
				}
			}
			else{
				if(vertical_axis){
					for(int i=0;i<H;++i){
						for(int j=0;j<half_w;++j){
							map[i,W-1-j] = map[i,j];
						}
					}
				}
			}
		}
		public List<pos> GetRoomFromPosition(pos position_in_room,bool allow_any_passable){ //get the room described by a single position inside it
			if(allow_any_passable){
				return map.GetFloodFillPositions(position_in_room,false,x=>map[x].IsPassable());
			}
			else{
				return map.GetFloodFillPositions(position_in_room,false,x=>map[x].IsRoomType());
			}
		}
		public pos MoveRoom(pos room,int direction){ return MoveRoom(GetRoomFromPosition(room,true),direction); }
		public pos MoveRoom(List<pos> room,int direction){
			List<CellType> prev_types = new List<CellType>();
			foreach(pos p in room){
				prev_types.Add(map[p]);
				map[p] = CellType.Wall;
			}
			List<pos> edge_positions = room.Where(x=>!room.Contains(x.PosInDir(direction)));
			pos offset = new pos(0,0);
			while(true){
				pos new_offset = offset.PosInDir(direction);
				bool good = true;
				foreach(pos p in edge_positions){
					pos n = new pos(p.row + new_offset.row,p.col + new_offset.col);
					foreach(pos neighbor in n.PositionsWithinDistance(1,false,true)){
						if(!neighbor.BoundsCheck(map,true) || !map[neighbor].IsWall()){
							good = false;
							break;
						}
					}
					if(!good){
						break;
					}
				}
				if(good){
					offset = new_offset;
				}
				else{
					break;
				}
			}
			int idx = 0;
			foreach(pos p in room){
				map[p.row + offset.row,p.col + offset.col] = prev_types[idx++];
			}
			return offset;
		}
		public delegate void DensityUpdateDelegate(pos p,PosArray<int> density,PosArray<CellType> cells);
		public bool CreateTwistyCave(bool two_walls_between_corridors,int percent_coverage){ return CreateTwistyCave(two_walls_between_corridors,percent_coverage,-1,(x,density,cells)=>{ return; }); }
		public bool CreateTwistyCave(bool two_walls_between_corridors,int percent_coverage,int density_threshold,DensityUpdateDelegate update_density){
			int target_number_of_floors = (H * W * percent_coverage) / 100;
			List<pos> frontier = new List<pos>();
			PosArray<int> density = new PosArray<int>(H,W);
			pos origin = new pos(R.Between(2,H-3),R.Between(2,W-3));
			frontier.Add(origin);
			map[origin] = CellType.RoomInterior;
			int count = 1;
			bool pick_random = false;
			while(frontier.Count > 0 && count < target_number_of_floors){
				pos p;
				if(pick_random || R.PercentChance(5)){
					p = frontier.RemoveRandom();
					pick_random = false;
				}
				else{
					p = frontier.RemoveLast();
				}
				if(density_threshold > 0 && density[p] >= density_threshold){
					continue;
				}
				List<int> valid_dirs = new List<int>();
				foreach(int dir in U.FourDirections){
					pos neighbor = p.PosInDir(dir);
					if(BoundsCheck(neighbor,false) && map[neighbor].IsWall()){
						bool valid = true;
						if(two_walls_between_corridors){
							int idx = 0;
							foreach(int dir2 in dir.GetArc(1)){
								if(!map[neighbor.PosInDir(dir2)].IsWall()){
									valid = false;
									break;
								}
								else{
									if(idx == 1){
										pos n = neighbor.PosInDir(dir2).PosInDir(dir2);
										if(n.BoundsCheck(map) && !map[n].IsWall()){
											valid = false;
											break;
										}
									}
									else{
										foreach(int dir3 in dir2.GetArc(1)){
											pos n = neighbor.PosInDir(dir2).PosInDir(dir3);
											if(n.BoundsCheck(map) && !map[n].IsWall()){
												valid = false;
												break;
											}
										}
									}
								}
								++idx;
							}
						}
						else{
							foreach(int dir2 in dir.GetArc(1)){
								if(!map[neighbor.PosInDir(dir2)].IsWall()){
									valid = false;
									break;
								}
							}
						}
						if(valid){
							valid_dirs.Add(dir);
						}
					}
				}
				if(valid_dirs.Count == 0){
					pick_random = true;
				}
				else{
					if(valid_dirs.Count == 1 && R.CoinFlip()){
						pick_random = true;
					}
					else{
						valid_dirs.Randomize();
						foreach(int i in valid_dirs){
							pos neighbor = p.PosInDir(i);
							map[neighbor] = CellType.RoomInterior;
							++count;
							update_density(neighbor,density,map);
							if(!pick_random){ //todo: should this check be removed? it can lead to abandoned paths that don't ever get filled
								frontier.Add(neighbor);
							}
						}
					}
				}
			}
			foreach(pos p in frontier){
				if(BoundsCheck(p,false)){
					map[p] = CellType.RoomInterior;
				}
			}
			return true;
		}
		public bool CreateCaveCorridor(int percent_coverage){
			return CreateTwistyCave(false,percent_coverage,80,(x,density,cells)=>{
				foreach(pos n2 in x.PositionsWithinDistance(8)){
					density[n2]++;
				}
				foreach(pos n2 in x.PositionsWithinDistance(4)){
					density[n2]++;
				}
			});
		}
		public bool MakeRoomsCavelike(){ return MakeRoomsCavelike(100,true); }
		public bool MakeRoomsCavelike(int percent_chance_per_room,bool ignore_rooms_with_single_exit){ //this isn't guaranteed to succeed, so you might need to check the return value
			return ForEachRoom(list => {
				if(PercentChance(percent_chance_per_room)){
					int start_r = list.WhereLeast(x=>x.row)[0].row;
					int end_r = list.WhereGreatest(x=>x.row)[0].row;
					int start_c = list.WhereLeast(x=>x.col)[0].col;
					int end_c = list.WhereGreatest(x=>x.col)[0].col;
					Dungeon room = new Dungeon((end_r - start_r) + 3,(end_c - start_c) + 3); //includes borders
					List<pos> map_exits = list.Where(x=>x.CardinalAdjacentPositions().Any(y=>map[y].IsCorridorType())); //grab the positions from list that have any adjacent corridor-type cells
					if(map_exits.Count < 2 && ignore_rooms_with_single_exit){
						return true;
					}
					List<pos> room_exits = new List<pos>();
					foreach(pos exit in map_exits){
						room_exits.Add(new pos(exit.row-start_r+1,exit.col-start_c+1));
					}
					int tries = 0;
					while(true){
						room.FillWithRandomWalls(25);
						room.ApplyCellularAutomataXYRule(3);
						room.ConnectDiagonals();
						room.RemoveDeadEndCorridors();
						room.RemoveUnconnectedAreas();
						bool exits_open = true;
						foreach(pos p in room_exits){
							if(!room[p].IsPassable()){
								exits_open = false;
							}
						}
						if(exits_open){
							for(int i=start_r;i<=end_r;++i){
								for(int j=start_c;j<=end_c;++j){
									if(list.Contains(new pos(i,j))){
										map[i,j] = room[(i-start_r)+1,(j-start_c)+1];
									}
								}
							}
							break;
						}
						++tries;
						if(tries > 50){
							return false;
						}
					}
				}
				return true;
			});
		}
		public bool AddRockFormations(){ return AddRockFormations(100,2); }
		public bool AddRockFormations(int percent_chance_per_room,int minimum_distance_from_wall){
			return ForEachRoom(list => {
				if(PercentChance(percent_chance_per_room)){
					int start_r = list.WhereLeast(x=>x.row)[0].row;
					int end_r = list.WhereGreatest(x=>x.row)[0].row;
					int start_c = list.WhereLeast(x=>x.col)[0].col;
					int end_c = list.WhereGreatest(x=>x.col)[0].col;
					Dungeon room = new Dungeon((end_r - start_r) + 3,(end_c - start_c) + 3); //includes borders
					while(true){
						room.FillWithRandomWalls(25);
						room.ApplyCellularAutomataXYRule(3);
						for(int i=start_r;i<=end_r;++i){
							for(int j=start_c;j<=end_c;++j){
								pos p = new pos(i,j);
								if(!p.PositionsWithinDistance(minimum_distance_from_wall-1).All(x=>list.Contains(x))){
									room[i-start_r+1,j-start_c+1] = CellType.RoomInterior;
								}
							}
						}
						room.ConnectDiagonals();
						room.RemoveDeadEndCorridors();
						room.RemoveUnconnectedAreas();
						for(int i=start_r;i<=end_r;++i){
							for(int j=start_c;j<=end_c;++j){
								if(list.Contains(new pos(i,j))){
									map[i,j] = room[(i-start_r)+1,(j-start_c)+1];
								}
							}
						}
						break;
					}
				}
				return true;
			});
		}
		public void AddPillars(int percent_chance_per_room){ //currently does 50% 'pillar', 25% 'statue', and 25% 'other', where relevant.
			ForEachRectangularRoom((start_r,start_c,end_r,end_c) => {
				if(PercentChance(percent_chance_per_room)){
					int height = end_r - start_r + 1;
					int width = end_c - start_c + 1;
					if(height > 3 || width > 3){
						List<PillarArrangement> layouts = new List<PillarArrangement>();
						if(height % 2 == 1 && width % 2 == 1){
							layouts.Add(PillarArrangement.Single);
						}
						if((height % 2 == 1 || width % 2 == 1) && height != 4 && width != 4){
							layouts.Add(PillarArrangement.Row);
						}
						if(height >= 5 && width >= 5){
							layouts.Add(PillarArrangement.Corners);
						}
						if(height > 2 && width > 2 && height != 4 && width != 4){
							layouts.Add(PillarArrangement.Full);
						}
						if((width % 2 == 1 && width >= 5) || (height % 2 == 1 && height >= 5)){
							layouts.Add(PillarArrangement.StatueEdges);
						}
						if(layouts.Count == 0 || CoinFlip()){ //otherwise they're too common
							layouts.Add(PillarArrangement.StatueCorners);
						}
						if(layouts.Count > 0){
							CellType pillar = CellType.Pillar;
							/*switch(Roll(4)){ //this part should be done later. Until then, they should remain pillars.
							case 1:
							case 2:
								pillar = CellType.Pillar;
								break;
							case 3:
								pillar = CellType.Statue;
								break;
							case 4:
								pillar = CellType.OtherRoomFeature;
								break;
							}*/
							switch(layouts.Random()){
							case PillarArrangement.Single:
								map[(start_r + end_r)/2,(start_c + end_c)/2] = pillar;
								break;
							case PillarArrangement.Row:
							{
								bool vertical;
								if(width % 2 == 1 && height % 2 == 0){
									vertical = true;
								}
								else{
									if(height % 2 == 1 && width % 2 == 0){
										vertical = false;
									}
									else{
										vertical = CoinFlip();
									}
								}
								if(vertical){
									if(height % 2 == 1){
										for(int i=start_r+1;i<=end_r-1;i+=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
									}
									else{
										int offset = 0;
										if(height % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_r+1+offset;i<(start_r + end_r)/2;i+=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
										for(int i=end_r-1-offset;i>(start_r + end_r)/2+1;i-=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
									}
								}
								else{
									if(width % 2 == 1){
										for(int i=start_c+1;i<=end_c-1;i+=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
									}
									else{
										int offset = 0;
										if(width % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_c+1+offset;i<(start_c + end_c)/2;i+=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
										for(int i=end_c-1-offset;i>(start_c + end_c)/2+1;i-=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
									}
								}
								break;
							}
							case PillarArrangement.Corners:
							{
								int v_offset = 0;
								int h_offset = 0;
								if(height % 4 == 0){
									v_offset = Roll(2) - 1;
								}
								if(width % 4 == 0){
									h_offset = Roll(2) - 1;
								}
								map[start_r + 1 + v_offset,start_c + 1 + h_offset] = pillar;
								map[start_r + 1 + v_offset,end_c - 1 - h_offset] = pillar;
								map[end_r - 1 - v_offset,start_c + 1 + h_offset] = pillar;
								map[end_r - 1 - v_offset,end_c - 1 - h_offset] = pillar;
								break;
							}
							case PillarArrangement.Full:
							{
								int v_offset = 0;
								int h_offset = 0;
								if(height % 4 == 0){
									v_offset = Roll(2) - 1;
								}
								if(width % 4 == 0){
									h_offset = Roll(2) - 1;
								}
								int half_r = (start_r + end_r)/2;
								int half_c = (start_c + end_c)/2;
								int half_r_offset = (start_r + end_r + 1)/2;
								int half_c_offset = (start_c + end_c + 1)/2;
								for(int i=start_r+1+v_offset;i<half_r;i+=2){
									for(int j=start_c+1+h_offset;j<half_c;j+=2){
										map[i,j] = pillar;
									}
								}
								for(int i=start_r+1+v_offset;i<half_r;i+=2){
									for(int j=end_c-1-h_offset;j>half_c_offset;j-=2){
										map[i,j] = pillar;
									}
								}
								for(int i=end_r-1-v_offset;i>half_r_offset;i-=2){
									for(int j=start_c+1+h_offset;j<half_c;j+=2){
										map[i,j] = pillar;
									}
								}
								for(int i=end_r-1-v_offset;i>half_r_offset;i-=2){
									for(int j=end_c-1-h_offset;j>half_c_offset;j-=2){
										map[i,j] = pillar;
									}
								}
								if((width+1) % 4 == 0){
									if(height % 2 == 1){
										for(int i=start_r+1;i<=end_r-1;i+=2){
											map[i,half_c] = pillar;
										}
									}
									else{
										int offset = 0;
										if(height % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_r+1+offset;i<half_r;i+=2){
											map[i,half_c] = pillar;
										}
										for(int i=end_r-1-offset;i>half_r_offset;i-=2){
											map[i,half_c] = pillar;
										}
									}
								}
								if((height+1) % 4 == 0){
									if(width % 2 == 1){
										for(int i=start_c+1;i<=end_c-1;i+=2){
											map[half_r,i] = pillar;
										}
									}
									else{
										int offset = 0;
										if(width % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_c+1+offset;i<half_c;i+=2){
											map[half_r,i] = pillar;
										}
										for(int i=end_c-1-offset;i>half_c_offset;i-=2){
											map[half_r,i] = pillar;
										}
									}
								}
								break;
							}
							case PillarArrangement.StatueCorners:
								map[start_r,start_c] = CellType.Statue;
								map[start_r,end_c] = CellType.Statue;
								map[end_r,start_c] = CellType.Statue;
								map[end_r,end_c] = CellType.Statue;
								break;
							case PillarArrangement.StatueEdges:
							{
								map[start_r,start_c] = CellType.Statue;
								map[start_r,end_c] = CellType.Statue;
								map[end_r,start_c] = CellType.Statue;
								map[end_r,end_c] = CellType.Statue;
								if(width % 2 == 1 && width > 3){
									int half_c = (start_c + end_c)/2;
									int corridors = new pos(start_r,half_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[start_r,half_c] = CellType.Statue;
									}
									corridors = new pos(end_r,half_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[end_r,half_c] = CellType.Statue;
									}
								}
								if(height % 2 == 1 && height > 3){
									int half_r = (start_r + end_r)/2;
									int corridors = new pos(half_r,start_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[half_r,start_c] = CellType.Statue;
									}
									corridors = new pos(half_r,end_c).CardinalAdjacentPositions().Where(x => BoundsCheck(x) && map[x].IsCorridorType()).Count;
									if(corridors == 0){
										map[half_r,end_c] = CellType.Statue;
									}
								}
								break;
							}
							default:
								break;
							}
						}
					}
				}
				return true;
			});
		}
		public void CaveWidenRooms(){ CaveWidenRooms(100,50); }
		public void CaveWidenRooms(int percent_chance_per_room,int number_of_tiles_to_add){
			List<List<pos>> roomlist = new List<List<pos>>();
			ForEachRoom(list=>{
				if(PercentChance(percent_chance_per_room)){
					roomlist.Add(list);
				}
				return true;
			});
			while(roomlist.Count > 0){
				List<pos> list = roomlist.RemoveRandom();
				PosArray<CellType> old_map = new PosArray<CellType>(H,W);
				foreach(pos p in list){
					old_map[p] = map[p];
					map[p] = CellType.Wall;
				}
				PosArray<bool> rock = new PosArray<bool>(H,W);
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						pos p = new pos(i,j);
						rock[p] = true;
						if(BoundsCheck(i,j,false)){
							foreach(pos neighbor in p.AdjacentPositionsClockwise()){
								if(map[neighbor] != CellType.Wall){
									rock[p] = false;
									break;
								}
							}
						}
					}
				}
				foreach(pos p in list){
					map[p] = CellType.RoomInterior; //todo: might this step be extraneous?
				}
				List<pos> frontier = new List<pos>();
				{
					PosArray<bool> in_list = new PosArray<bool>(H,W);
					foreach(pos p in list){
						in_list[p] = true;
					}
					for(int i=0;i<H;++i){
						for(int j=0;j<W;++j){
							pos p = new pos(i,j);
							if(in_list[p]){
								foreach(pos neighbor in p.PositionsAtDistance(1)){
									if(!in_list[neighbor]){
										frontier.Add(p);
										break;
									}
								}
							}
						}
					}
				}
				int fail_counter = 0;
				int num_added = 0;
				bool finished = false;
				while(!finished){
					if(frontier.Count == 0 || num_added >= number_of_tiles_to_add){
						finished = true;
						break;
					}
					pos f = frontier.RemoveRandom();
					foreach(pos neighbor in f.CardinalAdjacentPositions()){
						if(!BoundsCheck(neighbor,false) || !rock[neighbor.row,neighbor.col]){
							++fail_counter; //this might now be unreachable
							if(!BoundsCheck(neighbor,false)){
								fail_counter += 25; //fail quicker when against the edge of the map to prevent ugliness
							} //however, this doesn't actually fail as quickly as it should - i've overlooked something.
							if(fail_counter >= 50){
								finished = true;
								break;
							}
						}
						else{
							if(map[neighbor] != CellType.RoomInterior){
								map[neighbor] = CellType.RoomInterior;
								++num_added;
								bool add_neighbor = true;
								foreach(pos n2 in neighbor.CardinalAdjacentPositions()){
									if(!BoundsCheck(n2,false) || !rock[n2.row,n2.col]){
										add_neighbor = false;
										++fail_counter; //this might now be unreachable
										if(!BoundsCheck(neighbor,false)){
											fail_counter += 25; //fail quicker when against the edge of the map to prevent ugliness
										} //however, this doesn't actually fail as quickly as it should - i've overlooked something.
										if(fail_counter >= 50){
											finished = true;
										}
										break;
									}
								}
								if(finished){
									break;
								}
								if(add_neighbor){
									frontier.Add(neighbor);
								}
							}
						}
					}
				}
				foreach(pos p in list){
					map[p] = old_map[p];
				}
			}
		}
		public void ImproveMapEdges(int consecutive_floors_required){
			List<pos> corners = new List<pos>{new pos(H-2,1),new pos(H-2,W-2),new pos(1,W-2),new pos(1,1)};
			List<int> dirs = new List<int>{6,8,4,2};
			for(int i=0;i<4;++i){
				pos corner = corners[i];
				int dir = dirs[i];
				int check_dir = dir.RotateDir(false,2);
				pos current = corner;
				List<pos> edge_floors = new List<pos>();
				while(current.BoundsCheck(map,false)){
					bool add = true;
					if(map[current].IsFloor()){
						foreach(int rotated_dir in check_dir.GetArc(1)){
							if(!map[current.PosInDir(rotated_dir)].IsFloor()){
								add = false;
								break;
							}
						}
					}
					else{
						add = false;
					}
					if(add){
						edge_floors.Add(current);
					}
					else{
						if(edge_floors.Count >= consecutive_floors_required){
							foreach(pos p in edge_floors){
								if(R.CoinFlip()){
									map[p] = CellType.Wall;
								}
							}
						}
						edge_floors.Clear();
					}
					current = current.PosInDir(dir);
				}
				if(edge_floors.Count >= consecutive_floors_required){
					foreach(pos p in edge_floors){
						if(R.CoinFlip()){
							map[p] = CellType.Wall;
						}
					}
				}
			}
		}
		public void AddDoors(int percent_chance_of_door){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(map[i,j].IsPassable()){
						bool map_edge = false;
						pos p = new pos(i,j);
						foreach(int dir in FourDirections){
							if(!BoundsCheck(p.PosInDir(dir),true)){
								map_edge = true;
								break;
							}
						}
						if(!map_edge){
							if((map[i-1,j].IsWall() && map[i+1,j].IsWall()) || (map[i,j-1].IsWall() && map[i,j+1].IsWall())){ //walls on opposite sides
								bool potential_door = false;
								for(int k=2;k<=8;k+=2){
									if(map[p.PosInDir(k)].IsPassable() && map[p.PosInDir(k).PosInDir(k)].IsPassable()){
										if(map[p.PosInDir(k).PosInDir(k.RotateDir(false,2))].IsPassable()
										&& map[p.PosInDir(k).PosInDir(k.RotateDir(false,1))].IsPassable()){
											potential_door = true;
										}
										if(map[p.PosInDir(k).PosInDir(k.RotateDir(true,2))].IsPassable()
										&& map[p.PosInDir(k).PosInDir(k.RotateDir(true,1))].IsPassable()){
											potential_door = true;
										}
									}
									if(map[p.PosInDir(k)].Is(CellType.Door)){
										potential_door = false;
										break;
									}
								}
								if(potential_door && PercentChance(percent_chance_of_door)){
									map[i,j] = CellType.Door;
								}
							}
						}
					}
				}
			}
		}
		public void MarkInterestingLocationsNonRectangular(){
			var dijkstra = map.GetDijkstraMap(x=>!map[x].IsPassable(),x=>!map[x].IsPassable());
			PosArray<int> values = new PosArray<int>(H,W);
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(dijkstra[i,j].IsValidDijkstraValue() && dijkstra[i,j] > 1){
						values[i,j] = dijkstra[i,j] * 4;
						foreach(pos p in new pos(i,j).PositionsAtDistance(1)){
							if(dijkstra[p].IsValidDijkstraValue()){
								values[i,j] += dijkstra[p] * 2;
							}
						}
						foreach(pos p in new pos(i,j).PositionsAtDistance(2)){
							if(dijkstra[p].IsValidDijkstraValue()){
								values[i,j] += dijkstra[p];
							}
						}
					}
				}
			}
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(values[i,j] > 0){
						if(!new pos(i,j).PositionsAtDistance(1).Any(x=>values[x] > values[i,j])){
							map[i,j] = CellType.InterestingLocation;
						}
					}
				}
			}
		}
		public void MarkInterestingLocations(){
			ForEachRectangularRoom((start_r,start_c,end_r,end_c) => {
				int height = end_r - start_r + 1;
				int width = end_c - start_c + 1;
				if(height % 2 == 1 || width % 2 == 1){
					List<pos> exits = new List<pos>();
					for(int i=start_r;i<=end_r;++i){
						for(int j=start_c;j<=end_c;++j){
							if(i == start_r || j == start_c || i == end_r || j == end_c){
								pos p = new pos(i,j);
								foreach(pos neighbor in p.CardinalAdjacentPositions()){
									if(map[neighbor].IsCorridorType()){
										exits.Add(p);
										break;
									}
								}
							}
						}
					}
					int half_r = (start_r + end_r)/2;
					int half_c = (start_c + end_c)/2;
					int half_r_offset = (start_r + end_r + 1)/2;
					int half_c_offset = (start_c + end_c + 1)/2;
					List<pos> centers = new List<pos>();
					centers.Add(new pos(half_r,half_c));
					if(half_r != half_r_offset){
						centers.Add(new pos(half_r_offset,half_c));
					}
					else{ //these can't both be true because the dimension can't be even X even
						if(half_c != half_c_offset){
							centers.Add(new pos(half_r,half_c_offset));
						}
					}
					List<pos> in_middle_row_or_column = new List<pos>();
					if(width % 2 == 1){
						for(int i=start_r;i<=end_r;++i){
							in_middle_row_or_column.Add(new pos(i,half_c));
						}
					}
					if(height % 2 == 1){
						for(int j=start_c;j<=end_c;++j){
							bool good = true;
							foreach(pos p in in_middle_row_or_column){
								if(p.row == half_r && p.col == j){
									good = false;
									break;
								}
							}
							if(good){
								in_middle_row_or_column.Add(new pos(half_r,j));
							}
						}
					}
					List<pos> rejected = new List<pos>();
					foreach(pos p in in_middle_row_or_column){
						int floors = p.AdjacentPositionsClockwise().Where(x => !BoundsCheck(x,true) || map[x].IsPassable()).Count;
						if(!map[p].IsPassable() || floors != 8){
							rejected.Add(p);
						}
					}
					foreach(pos p in rejected){
						in_middle_row_or_column.Remove(p);
					}
					rejected.Clear();
					foreach(pos exit in exits){
						int greatest_distance = 0;
						foreach(pos center in centers){
							if(exit.ApproximateEuclideanDistanceFromX10(center) > greatest_distance){
								greatest_distance = exit.ApproximateEuclideanDistanceFromX10(center);
							}
						}
						foreach(pos potential in in_middle_row_or_column){
							if(exit.ApproximateEuclideanDistanceFromX10(potential) <= greatest_distance){
								rejected.Add(potential);
							}
						}
					}
					foreach(pos p in rejected){
						in_middle_row_or_column.Remove(p);
					}
					if(in_middle_row_or_column.Count > 0){
						int greatest_total_distance = 0;
						List<pos> positions_with_greatest_distance = new List<pos>();
						foreach(pos potential in in_middle_row_or_column){
							int total_distance = 0;
							foreach(pos exit in exits){
								total_distance += potential.ApproximateEuclideanDistanceFromX10(exit);
							}
							if(total_distance > greatest_total_distance){
								greatest_total_distance = total_distance;
								positions_with_greatest_distance.Clear();
								positions_with_greatest_distance.Add(potential);
							}
							else{
								if(total_distance == greatest_total_distance){
									positions_with_greatest_distance.Add(potential);
								}
							}
						}
						foreach(pos p in positions_with_greatest_distance){
							if(map[p].IsPassable()){
								map[p] = CellType.InterestingLocation;
							}
						}
					}
					else{
						if(height % 2 == 1 && width % 2 == 1){
							int floors = new pos(half_r,half_c).AdjacentPositionsClockwise().Where(x => !BoundsCheck(x,true) || map[x].IsPassable()).Count;
							if(map[half_r,half_c].IsPassable() && floors == 8){
								map[half_r,half_c] = CellType.InterestingLocation;
							}
						}
					}
				}
				return true;
			});
		}
		//utility:
		public delegate bool RectangularRoomDelegate(int start_r,int start_c,int end_r,int end_c);
		public bool ForEachRectangularRoom(RectangularRoomDelegate action){
			int[,] rooms = new int[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(map[i,j].IsRoomType()){
						rooms[i,j] = 0;
					}
					else{
						rooms[i,j] = -1;
					}
				}
			}
			int next_room_number = 1;
			for(int i=0;i<H-3;++i){
				for(int j=0;j<W-3;++j){
					if(rooms[i,j] == -1 && rooms[i+1,j+1] == 0 && rooms[i+2,j+2] == 0){ //checks 2 spaces down and right
						rooms[i+1,j+1] = next_room_number;
						for(bool done=false;!done;){
							done = true;
							for(int s=i+1;s<H-1;++s){
								for(int t=j+1;t<W-1;++t){
									if(rooms[s,t] == next_room_number){
										for(int u=s-1;u<=s+1;++u){
											for(int v=t-1;v<=t+1;++v){
												if(u != s || v != t){
													if(rooms[u,v] == 0){
														rooms[u,v] = next_room_number;
														done = false;
													}
												}
											}
										}
									}
								}
							}
						}
						++next_room_number;
					}
				}
			}
			for(int k=1;k<next_room_number;++k){
				int start_r = 999;
				int start_c = 999;
				int end_r = -1;
				int end_c = -1;
				for(int i=1;i<H-1;++i){
					for(int j=1;j<W-1;++j){
						if(rooms[i,j] == k){
							if(i < start_r){
								start_r = i;
							}
							if(i > end_r){
								end_r = i;
							}
							if(j < start_c){
								start_c = j;
							}
							if(j > end_c){
								end_c = j;
							}
						}
					}
				}
				bool rectangular = true;
				for(int i=start_r-1;i<=end_r+1 && rectangular;++i){
					for(int j=start_c-1;j<=end_c+1 && rectangular;++j){
						if(i == start_r-1 || j == start_c-1 || i == end_r+1 || j == end_c+1){
							if(BoundsCheck(i,j) && map[i,j].IsRoomType()){
								rectangular = false;
							}
						}
						else{
							if(!map[i,j].IsRoomType()){
								rectangular = false;
							}
						}
					}
				}
				if(rectangular){
					if(!action(start_r,start_c,end_r,end_c)){
						//return false;
					}
				}
			}
			return true;
		}
		public delegate bool RoomDelegate(List<pos> list);
		public bool ForEachRoom(RoomDelegate action){
			List<pos> completed = new List<pos>();
			for(int i=1;i<H-1;++i){
				for(int j=1;j<W-1;++j){
					pos p = new pos(i,j);
					bool good = true;
					foreach(pos neighbor in p.PositionsWithinDistance(1)){ //assumes at least 3x3 rooms. this might need to change in the future.
						if(!map[neighbor].IsRoomType()){
							good = false;
							break;
						}
					}
					if(good && !completed.Contains(p)){
						List<pos> room = map.GetFloodFillPositions(p,false,x=>map[x].IsRoomType());
						if(!action(room)){
							//return false;
						}
						foreach(pos p2 in room){
							completed.Add(p2);
						}
					}
				}
			}
			return true;
		}
		public List<pos> PositionSearch(List<CellType> stay_away_from,List<int> min_distance_away_from,List<CellType> stay_near,List<int> max_distance_away_from){
			if(stay_away_from == null){
				stay_away_from = new List<CellType>();
			}
			if(stay_near == null){ //yes, these are hacks
				stay_near = new List<CellType>();
			}
			List<pos> result = new List<pos>();
			Dict<CellType,int> source_values = new Dict<CellType,int>();
			int k = 0;
			foreach(CellType ct in stay_away_from){
				source_values[ct] = min_distance_away_from[k];
				++k;
			}
			CellType[] away = stay_away_from.ToArray();
			PosArray<int> distance = map.GetDijkstraMap(x=>map[x].Is(away),x=>map[x].Is(away),x=>1-source_values[map[x]],x=>1);
			List<pos> remaining = map.AllPositions();
			k = 0;
			foreach(CellType ct in stay_near){
				PosArray<int> nearby = map.GetDijkstraMap(x=>false,x=>map[x] == ct);
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						if(nearby[i,j] > max_distance_away_from[k]){
							remaining.Remove(new pos(i,j));
						}
					}
				}
				if(remaining.Count == 0){
					break;
				}
				++k;
			}
			foreach(pos p in remaining){
				if(distance[p] > 0){
					result.Add(p);
				}
			}
			return result;
		}
		//dijkstra distance check to lots of stuff goes here
		public bool BoundsCheck(pos p){ return BoundsCheck(p.row,p.col,false); }
		public bool BoundsCheck(int r,int c){ return BoundsCheck(r,c,false); }
		public bool BoundsCheck(pos p,bool allow_map_edges){ return BoundsCheck(p.row,p.col,allow_map_edges); }
		public bool BoundsCheck(int r,int c,bool allow_map_edges){
			if(r >= 0 && r < H && c >= 0 && c < W){
				if(!allow_map_edges){
					if(r == 0 || r == H-1 || c == 0 || c == W-1){
						return false;
					}
				}
				return true;
			}
			return false;
		}
	}
	public struct SchismAction{ //move this back to the interactive generator
		public int type;
		public int n;
		public int times;
		public int additional_random_numbers_consumed;
		public SchismAction(int type_){
			type = type_;
			n = 0;
			times = 1;
			additional_random_numbers_consumed = 0;
		}
		public SchismAction(int type_,int n_){
			type = type_;
			n = n_;
			times = 1;
			additional_random_numbers_consumed = 0;
		}
		public SchismAction(int type_,int n_,int times_){
			type = type_;
			n = n_;
			times = times_;
			additional_random_numbers_consumed = 0;
		}
	}
	/*
	 * list of actions:
	 *  0: create corridor/room. n% chance of being a corridor
	 *  1: connect diagonals
	 *  2: remove dead ends
	 *  3: remove unconnected
	 *  4: fill map with n% walls
	 *  5: apply cellular automata X-Y rule where Y == n - that is, a wall will be placed if there are at least n walls around.
	 *  6: make rooms cavelike
	 *  7: add pillars to rooms, with n% for each room
	 *  8: add doors, with n% chance
	 *  9: mark interesting locations
	 * 10: reflect map, on horizontal axis if n%2 == 1 and on vertical axis if n%4 == (2 or 3), i.e. the first and second bits
	 * 11: add lake shape
	 * 12: reject map if too empty. n is threshold of rectangular area.
	 * 13: reject map if floors < n
	 * 14: toggle option with index n
	 * 15: change dungeon variable with index n to the value of "times"
	 * 16: set the seed to n
	 * 17: add trees
	 */
}
namespace SchismExtensionMethods{
	using SchismDungeonGenerator;
	public static class Extensions{
		public static bool Is(this CellType c,params CellType[] types){
			foreach(CellType type in types){
				if(c == type){
					return true;
				}
			}
			return false;
		}
		public static bool IsWall(this CellType c){
			if(c == CellType.Wall){
				return true;
			}
			return false;
		}
		public static bool IsFloor(this CellType c){
			if(c.Is(CellType.RoomCorner,CellType.RoomEdge,CellType.RoomInterior,CellType.RoomInteriorCorner,
			        CellType.CorridorHorizontal,CellType.CorridorIntersection,CellType.CorridorVertical,CellType.ShallowWater)){
				return true;
			}
			return false;
		}
		public static bool IsRoomType(this CellType c){
			if(c.Is(CellType.RoomCorner,CellType.RoomEdge,CellType.RoomInterior,CellType.RoomInteriorCorner,
			        CellType.Pillar,CellType.Statue,CellType.RoomFeature1,CellType.RoomFeature2,CellType.RoomFeature3,CellType.RoomFeature4,CellType.RoomFeature5,CellType.InterestingLocation)){
				return true;
			}
			return false;
		}
		public static bool IsRoomInteriorType(this CellType c){ //note that statues aren't included here yet - for now, pillars are for the interior.
			if(c.Is(CellType.RoomInterior,CellType.Pillar,CellType.RoomFeature1,CellType.RoomFeature2,CellType.RoomFeature3,CellType.RoomFeature4,CellType.RoomFeature5,CellType.InterestingLocation)){
				return true;
			}
			return false;
		}
		public static bool IsRoomEdgeType(this CellType c){
			if(c.Is(CellType.RoomCorner,CellType.RoomEdge,CellType.RoomInteriorCorner,CellType.Statue)){
				return true;
			}
			return false;
		}
		public static bool IsCorridorType(this CellType c){
			if(c.Is(CellType.CorridorHorizontal,CellType.CorridorIntersection,CellType.CorridorVertical,CellType.Door,CellType.CorridorFeature1,CellType.CorridorFeature2,CellType.CorridorFeature3,CellType.CorridorFeature4,CellType.CorridorFeature5)){
				return true;
			}
			return false;
		}
		public static bool IsPassable(this CellType c){
			if(c.Is(CellType.Wall,CellType.Pillar,CellType.Statue,CellType.DeepWater)){
				return false;
			}
			return true;
		}
		public static char GetConvertedChar(this ConsoleKeyInfo k){
			switch(k.Key){
			case ConsoleKey.UpArrow:
			case ConsoleKey.NumPad8:
				return '8';
			case ConsoleKey.DownArrow:
			case ConsoleKey.NumPad2:
				return '2';
			case ConsoleKey.LeftArrow:
			case ConsoleKey.NumPad4:
				return '4';
			case ConsoleKey.Clear:
			case ConsoleKey.NumPad5:
				return '5';
			case ConsoleKey.RightArrow:
			case ConsoleKey.NumPad6:
				return '6';
			case ConsoleKey.Home:
			case ConsoleKey.NumPad7:
				return '7';
			case ConsoleKey.PageUp:
			case ConsoleKey.NumPad9:
				return '9';
			case ConsoleKey.End:
			case ConsoleKey.NumPad1:
				return '1';
			case ConsoleKey.PageDown:
			case ConsoleKey.NumPad3:
				return '3';
			case ConsoleKey.Tab:
				return (char)9;
			case ConsoleKey.Escape:
				return (char)27;
			case ConsoleKey.Enter:
				return (char)13;
			default:
				if((k.Modifiers & ConsoleModifiers.Shift)==ConsoleModifiers.Shift){
					return Char.ToUpper(k.KeyChar);
				}
				else{
					return k.KeyChar;
				}
			}
		}
		public static List<pos> CardinalAdjacentPositions(this pos p){
			List<pos> result = new List<pos>();
			for(int i=2;i<=8;i+=2){
				result.Add(p.PosInDir(i));
			}
			return result;
		}
		public static List<pos> AdjacentPositionsClockwise(this pos p){ return p.AdjacentPositionsClockwise(Dungeon.N); }
		public static List<pos> AdjacentPositionsClockwise(this pos p,int start_direction){
			List<pos> result = new List<pos>();
			for(int i=0;i<8;++i){
				result.Add(p.PosInDir(start_direction.RotateDir(true,i)));
			}
			return result;
		}
		public static int ConsecutiveAdjacent(this pos p,U.BooleanPositionDelegate condition){
			int max_count = 0;
			int count = 0;
			for(int times=0;times<2;++times){
				for(int i=0;i<8;++i){
					if(condition(p.PosInDir(Dungeon.N.RotateDir(true,i)))){
						++count;
					}
					else{
						if(count > max_count){
							max_count = count;
						}
						count = 0;
					}
				}
				if(count == 8){
					return 8;
				}
			}
			return max_count;
		}
		public static bool HasAdjacentWhere(this pos p,U.BooleanPositionDelegate condition){
			foreach(pos neighbor in p.PositionsAtDistance(1)){
				if(condition(neighbor)){
					return true;
				}
			}
			return false;
		}
		public static List<pos> OppositePairsWhere(this pos p,bool cardinal_only,U.BooleanPositionDelegate condition){
			List<pos> result = new List<pos>();
			for(int i=1;i<=4;++i){
				if(!cardinal_only || i%2 == 0){
					if(condition(p.PosInDir(i)) && condition(p.PosInDir(i.RotateDir(true,4)))){
						result.Add(p.PosInDir(i));
						result.Add(p.PosInDir(i.RotateDir(true,4)));
					}
				}
			}
			return result;
		}
		public static bool HasOppositePairWhere(this pos p,bool cardinal_only,U.BooleanPositionDelegate condition){
			for(int i=1;i<=4;++i){
				if(!cardinal_only || i%2 == 0){
					if(condition(p.PosInDir(i)) && condition(p.PosInDir(i.RotateDir(true,4)))){
						return true;
					}
				}
			}
			return false;
		}
	}
}
