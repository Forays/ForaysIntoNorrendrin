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
using PosArrays;
namespace Forays{
	public class Tile : PhysicalObject{
		public TileType type;
		public bool passable;
		public bool opaque{
			get{
				return internal_opaque || features.Contains(FeatureType.FOG) || features.Contains(FeatureType.THICK_DUST);
			}
			set{
				internal_opaque = value;
			}
		}
		private bool internal_opaque;
		public bool seen{
			get{
				return internal_seen;
			}
			set{
				if(value == true){
					if(!internal_seen){
						internal_seen = true;
						if(row > 0){
							M.tile[row-1,col].CheckForSpriteUpdate();
							if(col > 0){
								M.tile[row-1,col-1].CheckForSpriteUpdate();
							}
							if(col < COLS-1){
								M.tile[row-1,col+1].CheckForSpriteUpdate();
							}
						}
						if(col > 0){
							M.tile[row,col-1].CheckForSpriteUpdate();
						}
						if(col < COLS-1){
							M.tile[row,col+1].CheckForSpriteUpdate();
						}
					}
				}
				else{
					internal_seen = false;
				}
			}
		}
		private bool internal_seen;
		public bool revealed_by_light;
		public bool solid_rock; //used for walls that will never be seen, to speed up LOS checks
		public int light_value{
			get{
				return internal_light_value;
			}
			set{
				internal_light_value = value;
				if(value > 0 && type == TileType.BLAST_FUNGUS && !M.wiz_dark){
					B.Add("The blast fungus starts to smolder in the light. ",this);
					Toggle(null);
					if(inv == null){ //should always be true
						Item.Create(ConsumableType.BLAST_FUNGUS,row,col);
						inv.other_data = 3;
						inv.revealed_by_light = true;
					}
					Q.Add(new Event(inv,100,EventType.BLAST_FUNGUS));
				}
			}
		}
		private int internal_light_value; //no need to ever access this directly, either
		public int direction_exited; //used to improve AI's handling of corners
		public TileType? toggles_into;
		public Item inv;
		public List<FeatureType> features = new List<FeatureType>();
		private static List<FeatureType> feature_priority = new List<FeatureType>{FeatureType.GRENADE,FeatureType.FIRE,FeatureType.SPORES,FeatureType.POISON_GAS,FeatureType.PIXIE_DUST,FeatureType.CONFUSION_GAS,FeatureType.THICK_DUST,FeatureType.TELEPORTAL,FeatureType.STABLE_TELEPORTAL,FeatureType.FOG,FeatureType.WEB,FeatureType.TROLL_BLOODWITCH_CORPSE,FeatureType.TROLL_CORPSE,FeatureType.BONES,FeatureType.INACTIVE_TELEPORTAL,FeatureType.OIL,FeatureType.SLIME,FeatureType.FORASECT_EGG};
		private static int spellbooks_generated = 0;
		
		private static Dictionary<TileType,Tile> proto= new Dictionary<TileType, Tile>();
		public static Tile Prototype(TileType type){ return proto[type]; }
		private static Dictionary<FeatureType,PhysicalObject> proto_feature = new Dictionary<FeatureType, PhysicalObject>();
		public static PhysicalObject Feature(FeatureType type){ return proto_feature[type]; }
		static Tile(){
			Define(TileType.FLOOR,"floor",'.',Color.White,true,false,null);
			Define(TileType.WALL,"wall",'#',Color.Gray,false,true,null);
			Define(TileType.DOOR_C,"closed door",'+',Color.DarkYellow,false,true,TileType.DOOR_O);
			Define(TileType.DOOR_O,"open door",'-',Color.DarkYellow,true,false,TileType.DOOR_C);
			Define(TileType.STAIRS,"stairway",'>',Color.White,true,false,null);
			proto[TileType.STAIRS].revealed_by_light = true;
			Define(TileType.CHEST,"treasure chest",'=',Color.DarkYellow,true,false,null);
			Define(TileType.FIREPIT,"fire pit",'0',Color.Red,true,false,null);
			proto[TileType.FIREPIT].light_radius = 1;
			proto[TileType.FIREPIT].revealed_by_light = true;
			Define(TileType.UNLIT_FIREPIT,"unlit fire pit",'0',Color.TerrainDarkGray,true,false,null);
			proto[TileType.UNLIT_FIREPIT].revealed_by_light = true;
			Define(TileType.STALAGMITE,"stalagmite",'1',Color.White,false,true,TileType.FLOOR);
			proto[TileType.STALAGMITE].revealed_by_light = true;
			Define(TileType.FIRE_TRAP,"fire trap",'^',Color.RandomFire,true,false,TileType.FLOOR);
			Define(TileType.LIGHT_TRAP,"sunlight trap",'^',Color.Yellow,true,false,TileType.FLOOR);
			Define(TileType.TELEPORT_TRAP,"teleport trap",'^',Color.Magenta,true,false,TileType.FLOOR);
			Define(TileType.SLIDING_WALL_TRAP,"sliding wall trap",'^',Color.DarkCyan,true,false,TileType.FLOOR);
			Define(TileType.GRENADE_TRAP,"grenade trap",'^',Color.TerrainDarkGray,true,false,TileType.FLOOR);
			Define(TileType.SHOCK_TRAP,"shock trap",'^',Color.RandomLightning,true,false,TileType.FLOOR);
			Define(TileType.ALARM_TRAP,"alarm trap",'^',Color.Red,true,false,TileType.FLOOR);
			Define(TileType.DARKNESS_TRAP,"darkness trap",'^',Color.Blue,true,false,TileType.FLOOR);
			Define(TileType.POISON_GAS_TRAP,"poison gas trap",'^',Color.Green,true,false,TileType.FLOOR);
			Define(TileType.BLINDING_TRAP,"blinding trap",'^',Color.DarkMagenta,true,false,TileType.FLOOR);
			Define(TileType.ICE_TRAP,"ice trap",'^',Color.RandomIce,true,false,TileType.FLOOR);
			Define(TileType.PHANTOM_TRAP,"phantom trap",'^',Color.Cyan,true,false,TileType.FLOOR);
			Define(TileType.SCALDING_OIL_TRAP,"scalding oil trap",'^',Color.DarkYellow,true,false,TileType.FLOOR);
			Define(TileType.FLING_TRAP,"fling trap",'^',Color.DarkRed,true,false,TileType.FLOOR);
			Define(TileType.STONE_RAIN_TRAP,"stone rain trap",'^',Color.White,true,false,TileType.FLOOR);
			Define(TileType.HIDDEN_DOOR,"wall",'#',Color.Gray,false,true,TileType.DOOR_C);
			Define(TileType.RUBBLE,"pile of rubble",':',Color.Gray,false,false,TileType.FLOOR);
			Define(TileType.COMBAT_SHRINE,"shrine of combat",'_',Color.DarkRed,true,false,TileType.RUINED_SHRINE);
			Define(TileType.DEFENSE_SHRINE,"shrine of defense",'_',Color.White,true,false,TileType.RUINED_SHRINE);
			Define(TileType.MAGIC_SHRINE,"shrine of magic",'_',Color.Magenta,true,false,TileType.RUINED_SHRINE);
			Define(TileType.SPIRIT_SHRINE,"shrine of spirit",'_',Color.Yellow,true,false,TileType.RUINED_SHRINE);
			Define(TileType.STEALTH_SHRINE,"shrine of stealth",'_',Color.Blue,true,false,TileType.RUINED_SHRINE);
			Define(TileType.RUINED_SHRINE,"ruined shrine",'_',Color.TerrainDarkGray,true,false,null);
			Define(TileType.SPELL_EXCHANGE_SHRINE,"spell exchange shrine",'_',Color.DarkMagenta,true,false,TileType.RUINED_SHRINE);
			Define(TileType.FIRE_GEYSER,"fire geyser",'~',Color.Red,true,false,null);
			Prototype(TileType.FIRE_GEYSER).revealed_by_light = true;
			Define(TileType.STATUE,"statue",'5',Color.Gray,false,false,null);
			Define(TileType.POOL_OF_RESTORATION,"pool of restoration",'0',Color.Cyan,true,false,TileType.FLOOR);
			proto[TileType.POOL_OF_RESTORATION].revealed_by_light = true;
			Define(TileType.FOG_VENT,"fog vent",'~',Color.Gray,true,false,null);
			Prototype(TileType.FOG_VENT).revealed_by_light = true;
			Define(TileType.POISON_GAS_VENT,"poison gas vent",'~',Color.DarkGreen,true,false,null);
			Prototype(TileType.POISON_GAS_VENT).revealed_by_light = true;
			Define(TileType.STONE_SLAB,"stone slab",'#',Color.White,false,true,TileType.STONE_SLAB_OPEN);
			proto[TileType.STONE_SLAB].revealed_by_light = true;
			Define(TileType.STONE_SLAB_OPEN,"stone slab",'-',Color.White,true,false,TileType.STONE_SLAB);
			proto[TileType.STONE_SLAB_OPEN].revealed_by_light = true;
			Define(TileType.CHASM,"chasm",'\'',Color.DarkBlue,true,false,null);
			Define(TileType.BREACHED_WALL,"floor",'.',Color.RandomBreached,true,false,TileType.WALL);
			Define(TileType.CRACKED_WALL,"cracked wall",'#',Color.DarkGreen,false,true,TileType.FLOOR);
			proto[TileType.CRACKED_WALL].revealed_by_light = true;
			Define(TileType.BRUSH,"brush",'"',Color.DarkYellow,true,false,TileType.FLOOR);
			proto[TileType.BRUSH].a_name = "brush";
			proto[TileType.BRUSH].revealed_by_light = true;
			Define(TileType.WATER,"shallow water",'~',Color.Blue,true,false,null);
			proto[TileType.WATER].a_name = "shallow water";
			Prototype(TileType.WATER).revealed_by_light = true;
			Define(TileType.ICE,"ice",'~',Color.Cyan,true,false,null);
			proto[TileType.ICE].a_name = "ice";
			proto[TileType.ICE].revealed_by_light = true;
			Define(TileType.POPPY_FIELD,"poppy field",'"',Color.Red,true,false,TileType.FLOOR);
			proto[TileType.POPPY_FIELD].revealed_by_light = true;
			Define(TileType.GRAVEL,"gravel",',',Color.TerrainDarkGray,true,false,null);
			proto[TileType.GRAVEL].a_name = "gravel";
			proto[TileType.GRAVEL].revealed_by_light = true;
			Define(TileType.JUNGLE,"thick jungle",'&',Color.DarkGreen,true,true,null); //unused
			Define(TileType.BLAST_FUNGUS,"blast fungus",'%',Color.DarkRed,true,false,TileType.FLOOR);
			proto[TileType.BLAST_FUNGUS].revealed_by_light = true;
			Define(TileType.GLOWING_FUNGUS,"glowing fungus",',',Color.RandomGlowingFungus,true,false,null);
			Prototype(TileType.GLOWING_FUNGUS).revealed_by_light = true;
			Define(TileType.TOMBSTONE,"tombstone",'+',Color.Gray,true,false,null);
			proto[TileType.TOMBSTONE].revealed_by_light = true;
			Define(TileType.GRAVE_DIRT,"grave dirt",';',Color.DarkYellow,true,false,null);
			proto[TileType.GRAVE_DIRT].a_name = "grave dirt";
			proto[TileType.GRAVE_DIRT].revealed_by_light = true;
			Define(TileType.BARREL,"barrel of oil",'0',Color.DarkYellow,false,false,TileType.FLOOR);
			Prototype(TileType.BARREL).revealed_by_light = true;
			Define(TileType.STANDING_TORCH,"standing torch",'|',Color.RandomTorch,false,false,TileType.FLOOR);
			Prototype(TileType.STANDING_TORCH).light_radius = 3;
			Prototype(TileType.STANDING_TORCH).revealed_by_light = true;
			Define(TileType.VINE,"vine",';',Color.DarkGreen,true,false,null);
			Prototype(TileType.VINE).revealed_by_light = true;
			Define(TileType.POISON_BULB,"poison bulb",'%',Color.Green,false,false,null);
			Prototype(TileType.POISON_BULB).revealed_by_light = true;
			Define(TileType.WAX_WALL,"wax wall",'#',Color.DarkYellow,false,true,null);
			Prototype(TileType.WAX_WALL).revealed_by_light = true;
			Define(TileType.DEMONIC_IDOL,"demonic idol",'2',Color.Red,false,false,null);
			Prototype(TileType.DEMONIC_IDOL).revealed_by_light = true;
			Define(TileType.FIRE_RIFT,"fire rift",'~',Color.Red,true,false,null);
			Prototype(TileType.FIRE_RIFT).revealed_by_light = true;

			Define(FeatureType.GRENADE,"grenade",',',Color.Red);
			Define(FeatureType.TROLL_CORPSE,"troll corpse",'%',Color.DarkGreen);
			Define(FeatureType.TROLL_BLOODWITCH_CORPSE,"troll bloodwitch corpse",'%',Color.DarkRed);
			Define(FeatureType.POISON_GAS,"thick cloud of poison gas",'*',Color.DarkGreen);
			Define(FeatureType.FOG,"cloud of fog",'*',Color.Gray);
			Define(FeatureType.SLIME,"slime",',',Color.Green);
			proto_feature[FeatureType.SLIME].a_name = "slime";
			Define(FeatureType.TELEPORTAL,"teleportal",'8',Color.White);
			Define(FeatureType.INACTIVE_TELEPORTAL,"inactive teleportal",'8',Color.Gray);
			Define(FeatureType.STABLE_TELEPORTAL,"stable teleportal",'8',Color.Magenta);
			Define(FeatureType.FIRE,"fire",'&',Color.RandomFire);
			proto_feature[FeatureType.FIRE].a_name = "fire";
			proto_feature[FeatureType.FIRE].light_radius = 1; //this isn't actually functional
			Define(FeatureType.OIL,"oil",',',Color.DarkYellow);
			proto_feature[FeatureType.OIL].a_name = "oil";
			Define(FeatureType.BONES,"pile of bones",'%',Color.White);
			Define(FeatureType.WEB,"web",';',Color.White);
			Define(FeatureType.PIXIE_DUST,"cloud of pixie dust",'*',Color.RandomGlowingFungus); //might need to change this name
			Define(FeatureType.FORASECT_EGG,"forasect egg",'%',Color.TerrainDarkGray);
			Define(FeatureType.SPORES,"cloud of spores",'*',Color.DarkYellow);
			Define(FeatureType.THICK_DUST,"thick cloud of dust",'*',Color.TerrainDarkGray);
			Define(FeatureType.CONFUSION_GAS,"cloud of confusion gas",'*',Color.RandomConfusion);
		}
		private static void Define(TileType type_,string name_,char symbol_,Color color_,bool passable_,bool opaque_,TileType? toggles_into_){
			proto[type_] = new Tile(type_,name_,symbol_,color_,passable_,opaque_,toggles_into_);
		}
		private static void Define(FeatureType type_,string name_,char symbol_,Color color_){
			proto_feature[type_] = new PhysicalObject(name_,symbol_,color_);
			switch(type_){
			case FeatureType.BONES:
				proto_feature[type_].sprite_offset = new pos(0,31);
				break;
			case FeatureType.FIRE:
				proto_feature[type_].sprite_offset = new pos(0,27);
				break;
			case FeatureType.FOG:
				proto_feature[type_].sprite_offset = new pos(0,21);
				break;
			case FeatureType.FORASECT_EGG:
				proto_feature[type_].sprite_offset = new pos(1,18);
				break;
			case FeatureType.GRENADE:
				proto_feature[type_].sprite_offset = new pos(0,16);
				break;
			case FeatureType.INACTIVE_TELEPORTAL:
				proto_feature[type_].sprite_offset = new pos(0,24);
				break;
			case FeatureType.OIL:
				proto_feature[type_].sprite_offset = new pos(2,20);
				break;
			case FeatureType.PIXIE_DUST:
				proto_feature[type_].sprite_offset = new pos(1,17);
				break;
			case FeatureType.POISON_GAS:
				proto_feature[type_].sprite_offset = new pos(0,22);
				break;
			case FeatureType.SLIME:
				proto_feature[type_].sprite_offset = new pos(2,16);
				break;
			case FeatureType.SPORES:
				proto_feature[type_].sprite_offset = new pos(1,19);
				break;
			case FeatureType.STABLE_TELEPORTAL:
				proto_feature[type_].sprite_offset = new pos(0,25);
				break;
			case FeatureType.TELEPORTAL:
				proto_feature[type_].sprite_offset = new pos(0,23);
				break;
			case FeatureType.TROLL_BLOODWITCH_CORPSE:
				proto_feature[type_].sprite_offset = new pos(0,19);
				break;
			case FeatureType.TROLL_CORPSE:
				proto_feature[type_].sprite_offset = new pos(0,17);
				break;
			case FeatureType.WEB:
				proto_feature[type_].sprite_offset = new pos(1,16);
				break; //todo: no dust, confusion gas, any other new stuff yet.
			}
		}
		public Tile(){}
		public Tile(Tile t,int r,int c){
			type = t.type;
			name = t.name;
			a_name = t.a_name;
			the_name = t.the_name;
			symbol = t.symbol;
			color = t.color;
			if(t.type == TileType.BRUSH){
				if(R.CoinFlip()){
					color = Color.Yellow;
				}
				if(R.OneIn(20)){
					color = Color.Green;
				}
			}
			passable = t.passable;
			opaque = t.opaque;
			seen = false;
			revealed_by_light = t.revealed_by_light;
			solid_rock = false;
			light_value = 0;
			toggles_into = t.toggles_into;
			inv = null;
			row = r;
			col = c;
			light_radius = t.light_radius;
			direction_exited = 0;
			sprite_offset = t.sprite_offset;
		}
		public Tile(TileType type_,string name_,char symbol_,Color color_,bool passable_,bool opaque_,TileType? toggles_into_){
			type = type_;
			name = name_;
			the_name = "the " + name;
			switch(name[0]){
			case 'a':
			case 'e':
			case 'i':
			case 'o':
			case 'u':
			case 'A':
			case 'E':
			case 'I':
			case 'O':
			case 'U':
				a_name = "an " + name;
				break;
			default:
				a_name = "a " + name;
				break;
			}
			symbol = symbol_;
			color = color_;
			passable = passable_;
			opaque = opaque_;
			seen = false;
			solid_rock = false;
			revealed_by_light = false;
			if(Is(TileType.STAIRS,TileType.CHEST,TileType.FIREPIT,TileType.POOL_OF_RESTORATION)){
				revealed_by_light = true;
			}
			light_value = 0;
			toggles_into = toggles_into_;
			inv = null;
			light_radius = 0;
			direction_exited = 0;
			if(type >= TileType.COMBAT_SHRINE && type <= TileType.STEALTH_SHRINE){
				int diff = type - TileType.COMBAT_SHRINE;
				sprite_offset = new pos(11,diff);
			}
			else{
				if(type >= TileType.FIRE_TRAP && type <= TileType.STONE_RAIN_TRAP){
					int diff = type - TileType.FIRE_TRAP;
					sprite_offset = new pos(14 + diff/16,diff%16);
				}
				else{
					if(passable){
						sprite_offset = new pos(8,0);
					}
					else{
						sprite_offset = new pos(0,0);
					}
					switch(type){
					case TileType.BARREL:
						sprite_offset = new pos(12,14);
						break;
					case TileType.BLAST_FUNGUS:
						sprite_offset = new pos(12,5);
						break;
					case TileType.BREACHED_WALL:
						sprite_offset = new pos(9,0);
						break;
					case TileType.BRUSH:
						sprite_offset = new pos(12,0);
						break;
					case TileType.CHASM:
						sprite_offset = new pos(11,14);
						break;
					case TileType.CHEST:
						sprite_offset = new pos(10,4);
						break;
					case TileType.CRACKED_WALL:
						sprite_offset = new pos(0,1);
						break;
					case TileType.DEMONIC_IDOL:
						sprite_offset = new pos(13,1);
						break;
					case TileType.DOOR_C:
						sprite_offset = new pos(10,0);
						break;
					case TileType.DOOR_O:
						sprite_offset = new pos(10,2);
						break;
					case TileType.FIRE_GEYSER:
						sprite_offset = new pos(11,8);
						break;
					case TileType.FIREPIT:
						sprite_offset = new pos(10,12);
						break;
					case TileType.FLOOR:
						sprite_offset = new pos(8,0);
						break;
					case TileType.FOG_VENT:
						sprite_offset = new pos(11,9);
						break;
					case TileType.GLOWING_FUNGUS:
						sprite_offset = new pos(12,10);
						break;
					case TileType.GRAVE_DIRT:
						sprite_offset = new pos(12,13);
						break;
					case TileType.GRAVEL:
						sprite_offset = new pos(10,15);
						break;
					case TileType.ICE:
						sprite_offset = new pos(11,11);
						break;
					case TileType.JUNGLE:
						sprite_offset = new pos(0,0); //unused
						break;
					case TileType.POISON_BULB:
						sprite_offset = new pos(13,0);
						break;
					case TileType.POISON_GAS_VENT:
						sprite_offset = new pos(11,10);
						break;
					case TileType.POOL_OF_RESTORATION:
						sprite_offset = new pos(13,5);
						break;
					case TileType.POPPY_FIELD:
						sprite_offset = new pos(12,4);
						break;
					case TileType.RUBBLE:
						sprite_offset = new pos(10,14);
						break;
					case TileType.STAIRS:
						sprite_offset = new pos(13,6);
						break;
					case TileType.STALAGMITE:
						sprite_offset = new pos(10,13);
						break;
					case TileType.STANDING_TORCH:
						sprite_offset = new pos(12,15);
						break;
					case TileType.STATUE:
						sprite_offset = new pos(10,8);
						break;
					case TileType.STONE_SLAB:
						sprite_offset = new pos(11,12);
						break;
					case TileType.TOMBSTONE:
						sprite_offset = new pos(12,12);
						break;
					case TileType.VINE:
						sprite_offset = new pos(0,8);
						break;
					case TileType.WALL:
					case TileType.HIDDEN_DOOR:
						sprite_offset = new pos(0,0);
						break;
					case TileType.WATER:
						sprite_offset = new pos(0,4);
						break;
					case TileType.WAX_WALL:
						sprite_offset = new pos(0,12);
						break;
					}
				}
			}
		}
		public override string ToString(){
			return symbol.ToString();
		}
		public static Tile Create(TileType type,int r,int c){
			Tile t = null;
			if(M.tile[r,c] == null){
				t = new Tile(proto[type],r,c);
				M.tile[r,c] = t; //bounds checking here?
			}
			return t;
		}
		public bool IsVisuallyWall(){
			return Is(TileType.WALL,TileType.CRACKED_WALL,TileType.HIDDEN_DOOR,TileType.STONE_SLAB,TileType.DOOR_C,TileType.DOOR_O);
		}
		public void CheckForSpriteUpdate(){ //makes walls face the right way, for instance.
			switch(type){
			case TileType.WALL:
			case TileType.HIDDEN_DOOR:
				if(row < ROWS-1 && M.tile[row+1,col].seen && !M.tile[row+1,col].IsVisuallyWall()){
					sprite_offset = new pos(2,0);
				}
				else{
					bool side_wall = false;
					if(col > 0){
						if(row < ROWS-1 && M.tile[row+1,col-1].seen && !M.tile[row+1,col-1].IsVisuallyWall()){
							side_wall = true;
						}
						if(!side_wall && M.tile[row,col-1].seen && !M.tile[row,col-1].IsVisuallyWall()){
							side_wall = true;
						}
					}
					if(!side_wall && col < COLS-1){
						if(row < ROWS-1 && M.tile[row+1,col+1].seen && !M.tile[row+1,col+1].IsVisuallyWall()){
							side_wall = true;
						}
						if(!side_wall && M.tile[row,col+1].seen && !M.tile[row,col+1].IsVisuallyWall()){
							side_wall = true;
						}
					}
					if(side_wall){
						sprite_offset = new pos(1,0);
					}
					else{
						sprite_offset = new pos(0,0);
					}
				}
				break;
			case TileType.CRACKED_WALL:
				if(row < ROWS-1 && M.tile[row+1,col].seen && !M.tile[row+1,col].IsVisuallyWall()){
					sprite_offset = new pos(2,1);
				}
				else{
					bool side_wall = false;
					if(col > 0){
						if(row < ROWS-1 && M.tile[row+1,col-1].seen && !M.tile[row+1,col-1].IsVisuallyWall()){
							side_wall = true;
						}
						if(!side_wall && M.tile[row,col-1].seen && !M.tile[row,col-1].IsVisuallyWall()){
							side_wall = true;
						}
					}
					if(!side_wall && col < COLS-1){
						if(row < ROWS-1 && M.tile[row+1,col+1].seen && !M.tile[row+1,col+1].IsVisuallyWall()){
							side_wall = true;
						}
						if(!side_wall && M.tile[row,col+1].seen && !M.tile[row,col+1].IsVisuallyWall()){
							side_wall = true;
						}
					}
					if(side_wall){
						sprite_offset = new pos(1,1);
					}
					else{
						sprite_offset = new pos(0,1);
					}
				}
				break;
			case TileType.DOOR_C:
				if(!M.tile[row+1,col].passable || !M.tile[row-1,col].passable){
					sprite_offset = new pos(10,0);
				}
				else{
					sprite_offset = new pos(10,1);
				}
				break;
			case TileType.DOOR_O:
				if(!M.tile[row+1,col].passable || !M.tile[row-1,col].passable){
					sprite_offset = new pos(10,2);
				}
				else{
					sprite_offset = new pos(10,3);
				}
				break;
			}
		}
		public bool GetInternalOpacity(){ //annoying - this is the only value I need to do this for, right now, so I'll hack it in and move on.
			return internal_opaque;
		}
		public void SetInternalOpacity(bool value){ //these 2 methods are now used for save/load and updating light radius.
			internal_opaque = value;
		}
		public void SetInternalSeen(bool value){
			internal_seen = value;
		}
		public static TileType RandomTrap(){
			return (TileType)(TileType.FIRE_TRAP + R.Between(0,14));
			//int i = R.Roll(15) + 8;
			//return (TileType)i;
		}
		public string Name(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			if(!consider_low_light || IsLit()){
				return name;
			}
			else{
				if(IsKnownTrap()){
					return "trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "shrine";
				}
				return name;
			}
		}
		public string AName(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			if(!consider_low_light || IsLit()){
				return a_name;
			}
			else{
				if(IsKnownTrap()){
					return "a trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "a shrine";
				}
				return a_name;
			}
		}
		public string TheName(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			if(!consider_low_light || IsLit()){
				return the_name;
			}
			else{
				if(IsKnownTrap()){
					return "the trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "the shrine";
				}
				return the_name;
			}
		}
		public override List<colorstring> GetStatusBarInfo(){
			List<colorstring> result = new List<colorstring>();
			Color text = Color.Gray;
			if(p.Equals(UI.MapCursor)){
				text = Colors.status_highlight;
			}
			foreach(string s in Name(true).GetWordWrappedList(17,true)){
				colorstring cs = new colorstring();
				result.Add(cs);
				if(result.Count == 1){
					Color c = color;
					if(!revealed_by_light && !IsLit()){
						c = Colors.darkcolor;
					}
					cs.strings.Add(new cstr(symbol.ToString(),c));
					cs.strings.Add(new cstr(": " + s,text));
				}
				else{
					cs.strings.Add(new cstr("   " + s,text));
				}
			}
			return result;
		}
		public bool Is(TileType t){
			if(type == t){
				return true;
			}
			return false;
		}
		public bool Is(FeatureType t){
			foreach(FeatureType feature in features){
				if(feature == t){
					return true;
				}
			}
			return false;
		}
		public bool Is(params TileType[] types){
			foreach(TileType t in types){
				if(type == t){
					return true;
				}
			}
			return false;
		}
		public bool Is(params FeatureType[] types){
			foreach(FeatureType t1 in types){
				foreach(FeatureType t2 in features){
					if(t1 == t2){
						return true;
					}
				}
			}
			return false;
		}
		public colorchar FeatureVisual(){
			foreach(FeatureType ft in feature_priority){
				if(Is(ft)){
					if(ft == FeatureType.OIL || ft == FeatureType.SLIME){ //special hack - important tile types (like stairs and traps) get priority over oil & slime
						if(IsKnownTrap() || IsShrine() || Is(TileType.CHEST,TileType.STAIRS,TileType.BLAST_FUNGUS)){
							return visual;
						}
					}
					if(ft == FeatureType.FIRE){
						if(type == TileType.BARREL){
							return new colorchar('0',Tile.Feature(ft).color);
						}
						if(type == TileType.FIRE_GEYSER){
							return new colorchar('~',Tile.Feature(ft).color);
						}
					}
					if(ft == FeatureType.OIL && type == TileType.WATER){
						return new colorchar('~',Tile.Feature(ft).color);
					}
					return Tile.Feature(ft).visual;
				}
			}
			return visual;
		}
		public pos FeatureSprite(){ //todo
			foreach(FeatureType ft in feature_priority){
				if(Is(ft)){
					if(ft == FeatureType.OIL || ft == FeatureType.SLIME){ //special hack - important tile types (like stairs and traps) get priority over oil & slime
						if(IsKnownTrap() || IsShrine() || Is(TileType.CHEST,TileType.STAIRS,TileType.BLAST_FUNGUS)){
							return sprite_offset;
						}
					}
					if(ft == FeatureType.FIRE){
						if(type == TileType.BARREL){
							//todo return new colorchar('0',Tile.Feature(ft).color);
						}
						if(type == TileType.FIRE_GEYSER){
							//todo return new colorchar('~',Tile.Feature(ft).color);
						}
					}
					if(ft == FeatureType.OIL && type == TileType.WATER){
						//todo return new colorchar('~',Tile.Feature(ft).color);
					}
					return Tile.Feature(ft).sprite_offset;
				}
			}
			return sprite_offset;
		}
		/*public Color FeatureColor(){
			foreach(FeatureType ft in feature_priority){
				if(ft == FeatureType.OIL){ //special hack - important tile types (like stairs and traps) get priority over oil & slime
					if(IsKnownTrap() || IsShrine() || Is(TileType.CHEST,TileType.RUINED_SHRINE,TileType.STAIRS,TileType.BLAST_FUNGUS)){
						return color;
					}
				}
				if(Is(ft)){
					return Tile.Feature(ft).color;
				}
			}
			return color;
		}*/
		public string Preposition(){
			switch(type){
			case TileType.FLOOR:
			case TileType.STAIRS:
				return " on ";
			case TileType.DOOR_O:
				return " in ";
			default:
				return " and ";
			}
		}
		public int ContentsCount(){
			int count = 0;
			if(actor() != null && actor() != player && player.CanSee(actor())){
				++count;
			}
			if(inv != null){
				++count;
			}
			foreach(FeatureType f in features){
				++count;
			}
			return count;
		}
		public string ContentsString(){ return ContentsString(true); }
		public string ContentsString(bool include_monsters){
			string contents = "You see ";
			List<string> items = new List<string>();
			if(include_monsters && actor() != null && actor() != player && player.CanSee(actor())){
				items.Add(actor().a_name + " " + actor().WoundStatus());
			}
			if(inv != null){
				items.Add(inv.AName(true));
			}
			foreach(FeatureType f in features){
				items.Add(Tile.Feature(f).a_name);
			}
			if(items.Count == 0){
				contents += AName(true);
			}
			else{
				if(items.Count == 1){
					contents += items[0] + Preposition() + AName(true);
				}
				else{
					if(items.Count == 2){
						if(type != TileType.FLOOR){
							if(Preposition() == " and "){
								contents += items[0] + ", " + items[1] + ",";
								contents += Preposition() + AName(true);
							}
							else{
								contents += items[0] + " and " + items[1];
								contents += Preposition() + AName(true);
							}
						}
						else{
							contents += items[0] + " and " + items[1];
						}
					}
					else{
						foreach(string s in items){
							if(s != items.Last()){
								contents += s + ", ";
							}
							else{
								if(type != TileType.FLOOR){
									contents += s + ","; //because preposition contains a space already
								}
								else{
									contents += "and " + s;
								}
							}
						}
						if(type != TileType.FLOOR){
							contents += Preposition() + AName(true);
						}
					}
				}
			}
			return contents;
		}
		public bool CanGetItem(){ return CanGetItem(null); }
		public bool CanGetItem(Item i){
			if(Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS)){
				return false;
			}
			if(inv == null){
				return true;
			}
			if(i != null && inv.type == i.type && !inv.do_not_stack && !i.do_not_stack){
				return true;
			}
			return false;
		}
		public bool GetItem(Item item){
			if(item.type == ConsumableType.BLAST_FUNGUS && (IsWater() || Is(FeatureType.SLIME))){
				B.Add("The blast fungus is doused. ",this);
				return true;
			}
			if(inv == null && !Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS)){
				if((IsBurning() || Is(TileType.FIREPIT) || (actor() != null && actor().IsBurning())) && (item.NameOfItemType() == "scroll" || item.type == ConsumableType.BANDAGES)){
					B.Add(item.TheName(true) + " burns up! ",this); //should there be a check for water or slime here?
					item.CheckForMimic();
					if(Is(TileType.FIREPIT) || (actor() != null && actor().IsBurning())){
						AddFeature(FeatureType.FIRE);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				else{
					item.row = row;
					item.col = col;
					if(item.light_radius > 0){
						item.UpdateRadius(0,item.light_radius);
					}
					inv = item;
				}
				return true;
			}
			else{
				if(!Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS) && inv.type == item.type && !inv.do_not_stack && !item.do_not_stack){
					inv.quantity += item.quantity;
					return true;
				}
				else{
					foreach(Tile t in M.ReachableTilesByDistance(row,col,false,TileType.DOOR_C,TileType.RUBBLE,TileType.STONE_SLAB)){
						if(item.type == ConsumableType.BLAST_FUNGUS && (t.IsWater() || t.Is(FeatureType.SLIME))){
							B.Add("The blast fungus is doused. ",t);
							return true;
						}
						if(t.passable && t.inv == null && !t.Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS)){
							if((t.IsBurning() || t.Is(TileType.FIREPIT) || (t.actor() != null && t.actor().IsBurning())) && (item.NameOfItemType() == "scroll" || item.type == ConsumableType.BANDAGES)){
								B.Add(item.TheName(true) + " burns up! ",t);
								item.CheckForMimic();
								if(t.Is(TileType.FIREPIT) || (t.actor() != null && t.actor().IsBurning())){
									t.AddFeature(FeatureType.FIRE);
								}
								if(t.actor() != null){
									t.actor().ApplyBurning();
								}
							}
							else{
								item.row = t.row;
								item.col = t.col;
								if(item.light_radius > 0){
									item.UpdateRadius(0,item.light_radius);
								}
								t.inv = item;
							}
							return true;
						}
					}
					return false;
				}
			}
		}
		public void Bump(int direction_of_motion){
			switch(type){
			case TileType.BARREL:
			{
				if(TileInDirection(direction_of_motion).passable){
					B.Add("The barrel tips over and smashes. ",this);
					TurnToFloor();
					List<Tile> cone = GetCone(direction_of_motion,2,true).Where(x=>x.passable && HasLOE(x));
					List<Tile> added = new List<Tile>();
					foreach(Tile t in cone){
						foreach(int dir in U.FourDirections){
							if(R.CoinFlip() && t.TileInDirection(dir).passable){
								added.AddUnique(t.TileInDirection(dir));
							}
						}
					}
					cone.AddRange(added);
					cone.Remove(this);
					foreach(Tile t in cone){
						t.AddFeature(FeatureType.OIL);
					}
					if(Is(FeatureType.FIRE)){
						RemoveFeature(FeatureType.FIRE);
						TileInDirection(direction_of_motion).ApplyEffect(DamageType.FIRE);
					}
				}
				break;
			}
			case TileType.STANDING_TORCH:
				if(TileInDirection(direction_of_motion).passable){
					B.Add("The torch tips over. ",this);
					TurnToFloor();
					TileInDirection(direction_of_motion).AddFeature(FeatureType.FIRE);
				}
				break;
			case TileType.POISON_BULB:
			{
				B.Add("The poison bulb bursts. ",this);
				TurnToFloor();
				List<Tile> area = AddGaseousFeature(FeatureType.POISON_GAS,8);
				if(area.Count > 0){
					Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
					Event.RemoveGas(area,200,FeatureType.POISON_GAS,18);
				}
				break;
			}
			}
		}
		public void Smash(int direction_of_motion){
			if(!p.BoundsCheck(M.tile,false)){
				return; //no smashing the edge of the map!
			}
			switch(type){
			case TileType.WALL:
			case TileType.WAX_WALL:
			case TileType.CRACKED_WALL:
			case TileType.DOOR_C:
			case TileType.RUBBLE:
			case TileType.STATUE:
				TurnToFloor();
				foreach(Tile neighbor in TilesAtDistance(1)){
					neighbor.solid_rock = false;
				}
				break;
			case TileType.STALAGMITE:
				Toggle(null);
				break;
			case TileType.HIDDEN_DOOR:
			{
				foreach(Event e in Q.list){
					if(e.type == EventType.CHECK_FOR_HIDDEN){
						e.area.Remove(this);
						if(e.area.Count == 0){
							e.dead = true;
						}
						break;
					}
				}
				TurnToFloor();
				break;
			}
			case TileType.STONE_SLAB:
			{
				Event e = Q.FindTargetedEvent(this,EventType.STONE_SLAB);
				if(e != null){
					e.dead = true;
				}
				TurnToFloor();
				break;
			}
			case TileType.POISON_BULB:
				Bump(0);
				break;
			case TileType.STANDING_TORCH:
				Bump(direction_of_motion);
				if(type == TileType.STANDING_TORCH){
					TurnToFloor();
					AddFeature(FeatureType.FIRE);
				}
				break;
			case TileType.BARREL:
				Bump(direction_of_motion);
				if(type == TileType.BARREL){
					TurnToFloor();
					List<Tile> cone = TilesWithinDistance(1).Where(x=>x.passable);
					List<Tile> added = new List<Tile>();
					foreach(Tile t in cone){
						foreach(int dir in U.FourDirections){
							if(R.CoinFlip() && t.TileInDirection(dir).passable){
								added.AddUnique(t.TileInDirection(dir));
							}
						}
					}
					cone.AddRange(added);
					foreach(Tile t in cone){
						t.AddFeature(FeatureType.OIL);
						if(t.actor() != null && !t.actor().HasAttr(AttrType.OIL_COVERED,AttrType.SLIMED)){
							if(t.actor().IsBurning()){
								t.actor().ApplyBurning();
							}
							else{
								t.actor().attrs[AttrType.OIL_COVERED] = 1;
								B.Add(t.actor().YouAre() + " covered in oil. ",t.actor());
								if(t.actor() == player){
									Help.TutorialTip(TutorialTopic.Oiled);
								}
							}
						}
					}
				}
				break;
			case TileType.DEMONIC_IDOL:
			{
				TurnToFloor();
				if(!TilesWithinDistance(3).Any(x=>x.type == TileType.DEMONIC_IDOL)){
					foreach(Tile t2 in TilesWithinDistance(4)){
						if(t2.color == Color.RandomDoom){
							t2.color = Colors.ResolveColor(Color.RandomDoom);
						}
					}
					B.Add("You feel the power leave this summoning circle. ");
					bool circles = false;
					bool demons = false;
					for(int i=0;i<5;++i){
						Tile circle = M.tile[M.FinalLevelSummoningCircle(i)];
						if(circle.TilesWithinDistance(3).Any(x=>x.type == TileType.DEMONIC_IDOL)){
							circles = true;
							break;
						}
					}
					foreach(Actor a in M.AllActors()){
						if(a.Is(ActorType.MINOR_DEMON,ActorType.FROST_DEMON,ActorType.BEAST_DEMON,ActorType.DEMON_LORD)){
							demons = true;
							break;
						}
					}
					if(!circles && !demons){ //victory
						player.curhp = 100;
						B.Add("As the last summoning circle is destroyed, your victory gives you a new surge of strength. ");
						B.PrintAll();
						B.Add("Kersai's summoning has been stopped. His cult will no longer threaten the area. ");
						B.PrintAll();
						B.Add("You begin the journey home to deliver the news. ");
						B.PrintAll();
						Global.GAME_OVER = true;
						Global.BOSS_KILLED = true;
						Global.KILLED_BY = "nothing";
					}
				}
				break;
			}
			}
		}
		public void Toggle(Actor toggler){
			if(toggles_into != null){
				Toggle(toggler,toggles_into.Value);
			}
		}
		public void Toggle(Actor toggler,TileType toggle_to){
			bool lighting_update = false;
			List<PhysicalObject> light_sources = new List<PhysicalObject>();
			TileType original_type = type;
			bool original_passable = passable;
			if(opaque != Prototype(toggle_to).opaque){
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.tile[i,j].IsLit(player.row,player.col,true)){
							lighting_update = true;
						}
					}
				}
			}
			if(lighting_update){
				for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
					for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
								light_sources.Add(M.actor[i,j]);
								M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
							}
							if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
								light_sources.Add(M.tile[i,j].inv);
								M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
							}
							if(M.tile[i,j].light_radius > 0){
								light_sources.Add(M.tile[i,j]);
								M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
							}
							else{
								if(M.tile[i,j].Is(FeatureType.FIRE)){
									light_sources.Add(M.tile[i,j]);
									M.tile[i,j].UpdateRadius(1,0);
								}
							}
						}
					}
				}
			}

			TransformTo(toggle_to);

			if(lighting_update){
				foreach(PhysicalObject o in light_sources){
					if(o is Actor){
						Actor a = o as Actor;
						a.UpdateRadius(0,a.LightRadius());
					}
					else{
						if(o is Tile && o.light_radius == 0 && (o as Tile).Is(FeatureType.FIRE)){
							o.UpdateRadius(0,1);
						}
						else{
							o.UpdateRadius(0,o.light_radius);
						}
					}
				}
			}
			if(Prototype(type).revealed_by_light){
				revealed_by_light = true;
			}
			if(toggler != null && toggler != player){
				if(type == TileType.DOOR_C && original_type == TileType.DOOR_O){
					if(player.CanSee(this)){
						B.Add(toggler.TheName(true) + " closes the door. ");
					}
				}
				if(type == TileType.DOOR_O && original_type == TileType.DOOR_C){
					if(player.CanSee(this)){
						B.Add(toggler.TheName(true) + " opens the door. ");
					}
				}
			}
			if(toggler != null){
				if(original_type == TileType.RUBBLE){
					B.Add(toggler.YouVisible("scatter") + " the rubble. ",this);
				}
			}
			if(!passable && original_passable){
				if(features.Contains(FeatureType.STABLE_TELEPORTAL)){
					Event e = Q.FindTargetedEvent(this,EventType.TELEPORTAL);
					if(e != null){
						foreach(Tile t in e.area){
							Event e2 = Q.FindTargetedEvent(t,EventType.TELEPORTAL);
							if(e2 != null && t.features.Contains(FeatureType.STABLE_TELEPORTAL)){
								e2.area.Remove(this);
								if(e2.area.Count == 0){
									t.RemoveFeature(FeatureType.STABLE_TELEPORTAL);
									t.AddFeature(FeatureType.INACTIVE_TELEPORTAL);
									e2.dead = true;
								}
							}
						}
					}
				}
				foreach(FeatureType ft in new List<FeatureType>(features)){
					RemoveFeature(ft);
				}
			}
			CheckForSpriteUpdate();
		}
		public void TransformTo(TileType type_){
			name=Prototype(type_).name;
			a_name=Prototype(type_).a_name;
			the_name=Prototype(type_).the_name;
			symbol=Prototype(type_).symbol;
			color=Prototype(type_).color;
			type=Prototype(type_).type;
			passable=Prototype(type_).passable;
			opaque=Prototype(type_).opaque;
			toggles_into=Prototype(type_).toggles_into;
			if(opaque){
				light_value = 0;
			}
			if(light_radius != Prototype(type_).light_radius){
				UpdateRadius(light_radius,Prototype(type_).light_radius);
			}
			light_radius = Prototype(type_).light_radius;
			if(name == "floor"){ //this could be handled better, by tracking which types are never 'revealed'
				revealed_by_light = false;
			}
			else{
				if(Prototype(type_).revealed_by_light){
					revealed_by_light = true;
				}
			}
			sprite_offset = Prototype(type_).sprite_offset;
		}
		public void TurnToFloor(){
			Toggle(null,TileType.FLOOR);
			/*bool lighting_update = false;
			List<PhysicalObject> light_sources = new List<PhysicalObject>();
			if(opaque){
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.IsLit(player.row,player.col,true)){
						lighting_update = true;
					}
				}
			}
			if(lighting_update){
				for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
					for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
								light_sources.Add(M.actor[i,j]);
								M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
							}
							if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
								light_sources.Add(M.tile[i,j].inv);
								M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
							}
							if(M.tile[i,j].light_radius > 0){
								light_sources.Add(M.tile[i,j]);
								M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
							}
						}
					}
				}
			}
			
			TransformTo(TileType.FLOOR);
			
			if(lighting_update){
				foreach(PhysicalObject o in light_sources){
					if(o is Actor){
						Actor a = o as Actor;
						a.UpdateRadius(0,a.LightRadius());
					}
					else{
						o.UpdateRadius(0,o.light_radius);
					}
				}
			}*/
		}
		public void TriggerTrap(){ TriggerTrap(true); }
		public void TriggerTrap(bool click){
			bool actor_here = (actor() != null);
			if(actor_here && actor().type == ActorType.CYCLOPEAN_TITAN){
				if(name == "floor"){
					B.Add(actor().TheName(true) + " smashes " + Tile.Prototype(type).a_name + ". ",this);
				}
				else{
					B.Add(actor().TheName(true) + " smashes " + the_name + ". ",this);
				}
				TransformTo(TileType.FLOOR);
				return;
			}
			if(click){
				if(actor() == player || (actor() == null && player.CanSee(this))){
					B.Add("*CLICK* ",this);
					B.PrintAll();
				}
				else{
					if(actor() != null && player.CanSee(this) && player.CanSee(actor())){
						B.Add("You hear a *CLICK* from under " + actor().the_name + ". ");
						B.PrintAll();
					}
					else{
						if(DistanceFrom(player) <= 12){
							B.Add("You hear a *CLICK* nearby. ");
							B.PrintAll();
						}
						else{
							B.Add("You hear a *CLICK* in the distance. ");
							B.PrintAll();
						}
					}
				}
			}
			if(actor() == player){
				Help.TutorialTip(TutorialTopic.Traps);
			}
			switch(type){
			case TileType.GRENADE_TRAP:
			{
				if(actor_here && player.CanSee(actor())){
					B.Add("Grenades fall from the ceiling above " + actor().the_name + "! ",this);
				}
				else{
					B.Add("Grenades fall from the ceiling! ",this);
				}
				List<Tile> valid = new List<Tile>();
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.passable && !t.Is(FeatureType.GRENADE)){
						valid.Add(t);
					}
				}
				int count = R.OneIn(10)? 3 : 2;
				for(;count>0 & valid.Count > 0;--count){
					Tile t = valid.Random();
					if(t.actor() != null){
						if(t.actor() == player){
							B.Add("One lands under you! ");
						}
						else{
							if(player.CanSee(this)){
								B.Add("One lands under " + t.actor().the_name + ". ",t.actor());
							}
						}
					}
					else{
						if(t.inv != null){
							B.Add("One lands under " + t.inv.TheName() + ". ",t);
						}
					}
					t.features.Add(FeatureType.GRENADE);
					valid.Remove(t);
					Q.Add(new Event(t,100,EventType.GRENADE));
				}
				Toggle(actor());
				break;
			}
			case TileType.SLIDING_WALL_TRAP:
			{
				List<int> dirs = new List<int>();
				for(int i=2;i<=8;i+=2){
					Tile t = this;
					bool good = true;
					while(t.type != TileType.WALL){
						t = t.TileInDirection(i);
						if(t.opaque && t.type != TileType.WALL){
							good = false;
							break;
						}
						if(DistanceFrom(t) > 6){
							good = false;
							break;
						}
					}
					if(good && t.row > 0 && t.row < ROWS-1 && t.col > 0 && t.col < COLS-1){
						t = t.TileInDirection(i);
					}
					else{
						good = false;
					}
					if(good && t.row > 0 && t.row < ROWS-1 && t.col > 0 && t.col < COLS-1){
						foreach(Tile tt in t.TilesWithinDistance(1)){
							if(tt.type != TileType.WALL){
								good = false;
							}
						}
					}
					else{
						good = false;
					}
					if(good){
						dirs.Add(i);
					}
				}
				if(dirs.Count == 0){
					B.Add("Nothing happens. ",this);
				}
				else{
					int dir = dirs[R.Roll(dirs.Count)-1];
					Tile first = this;
					while(first.type != TileType.WALL){
						first = first.TileInDirection(dir);
					}
					first.TileInDirection(dir).TurnToFloor();
					ActorType ac = ActorType.SKELETON;
					if(M.current_level >= 3 && R.CoinFlip()){
						ac = ActorType.ZOMBIE;
					}
					if(M.current_level >= 9 && R.OneIn(10)){
						ac = ActorType.STONE_GOLEM;
					}
					if(M.current_level >= 7 && R.PercentChance(1)){
						ac = ActorType.MECHANICAL_KNIGHT;
					}
					if(M.current_level >= 15 && R.PercentChance(1)){
						ac = ActorType.CORPSETOWER_BEHEMOTH;
					}
					if(M.current_level >= 15 && R.PercentChance(1)){
						ac = ActorType.MACHINE_OF_WAR;
					}
					if(R.PercentChance(1)){
						first.TileInDirection(dir).TransformTo(TileType.CHEST);
						if(R.PercentChance(1)){
							first.TileInDirection(dir).color = Color.Yellow;
						}
					}
					else{
						Actor.Create(ac,first.TileInDirection(dir).row,first.TileInDirection(dir).col,TiebreakerAssignment.InsertAfterCurrent);
					}
					first.TurnToFloor();
					foreach(Tile t in first.TileInDirection(dir).TilesWithinDistance(1)){
						t.solid_rock = false;
					}
					if(first.ActorInDirection(dir) != null){
						first.ActorInDirection(dir).FindPath(first.TileInDirection(dir.RotateDir(true,4)));
						//first.ActorInDirection(dir).FindPath(TileInDirection(dir));
					}
					if(player.CanSee(first)){
						B.Add("The wall slides away. ");
					}
					else{
						if(DistanceFrom(player) <= 6){
							B.Add("You hear rock sliding on rock. ");
						}
					}
				}
				Toggle(actor());
				break;
			}
			case TileType.TELEPORT_TRAP:
			{
				if(actor_here){
					B.Add("An unstable energy covers " + actor().TheName(true) + ". ",actor());
					actor().attrs[AttrType.TELEPORTING] = R.Roll(4);
					Q.KillEvents(actor(),AttrType.TELEPORTING); //should be replaced by refreshduration eventually. works the same way, though.
					Q.Add(new Event(actor(),(R.Roll(10)+25)*100,AttrType.TELEPORTING,actor().YouFeel() + " more stable. ",actor()));
				}
				else{
					if(inv != null){
						B.Add("An unstable energy covers " + inv.TheName(true) + ". ",this);
						Tile dest = M.AllTiles().Where(x=>x.passable && x.CanGetItem()).RandomOrDefault();
						if(dest != null){
							B.Add("It vanishes! ",this);
							bool seen = player.CanSee(this);
							Item i = inv;
							inv = null;
							dest.GetItem(i);
							if(seen){
								B.Add("It reappears! ",dest);
							}
							else{
								B.Add(i.AName(true) + " appears! ",dest);
							}
						}
						else{
							B.Add("Nothing happens. ",this);
						}
					}
					else{
						B.Add("An unstable energy crackles for a moment, then dissipates. ",this);
					}
				}
				Toggle(actor());
				break;
			}
			case TileType.SHOCK_TRAP:
			{
				//int old_radius = light_radius; //This was a cool effect, but caused bugs when the tile's radius changed mid-trigger.
				//UpdateRadius(old_radius,3,true); //I'll restore it when I figure out how...
				if(actor_here){
					if(player.CanSee(actor())){
						B.Add("Electricity zaps " + actor().the_name + ". ",this);
					}
					if(actor().TakeDamage(DamageType.ELECTRIC,DamageClass.PHYSICAL,R.Roll(3,6),null,"a shock trap")){
						actor().ApplyStatus(AttrType.STUNNED,(R.Roll(6)+7)*100);
						/*B.Add(actor().YouAre() + " stunned! ",actor());
						actor().RefreshDuration(AttrType.STUNNED,actor().DurationOfMagicalEffect(R.Roll(6)+7)*100,(actor().YouAre() + " no longer stunned. "),actor());*/
						if(actor() == player){
							Help.TutorialTip(TutorialTopic.Stunned);
						}
					}
				}
				else{
					B.Add("Arcs of electricity appear and sizzle briefly. ",this); //apply electricity, once wands have been added
				}
				//M.Draw();
				//UpdateRadius(3,old_radius,true);
				Toggle(actor());
				break;
			}
			case TileType.LIGHT_TRAP:
				if(M.wiz_lite == false){
					if(actor_here && player.HasLOS(row,col) && !actor().IsHiddenFrom(player)){
						B.Add("A wave of light washes out from above " + actor().TheName(true) + "! ");
					}
					else{
						B.Add("A wave of light washes over the area! ");
					}
					M.wiz_lite = true;
					M.wiz_dark = false;
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even brighter for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				Toggle(actor());
				break;
			case TileType.DARKNESS_TRAP:
				if(M.wiz_dark == false){
					if(actor_here && player.CanSee(actor())){
						B.Add("A surge of darkness radiates out from above " + actor().TheName(true) + "! ");
						if(player.light_radius > 0){
							B.Add("Your light is extinguished! ");
						}
					}
					else{
						B.Add("A surge of darkness radiates over the area! ");
						if(player.light_radius > 0){
							B.Add("Your light is extinguished! ");
						}
					}
					M.wiz_dark = true;
					M.wiz_lite = false;
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even darker for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				Toggle(actor());
				break;
			case TileType.FIRE_TRAP:
			{
				if(actor_here){
					B.Add("A column of flame engulfs " + actor().TheName(true) + "! ",this);
					actor().ApplyBurning();
				}
				else{
					B.Add("A column of flame appears! ",this);
				}
				AddFeature(FeatureType.FIRE);
				Toggle(actor());
				break;
			}
			case TileType.ALARM_TRAP:
				if(actor() == player){
					B.Add("A high-pitched ringing sound reverberates from above you. ");
				}
				else{
					if(actor_here && player.CanSee(actor())){
						B.Add("A high-pitched ringing sound reverberates from above " + actor().the_name + ". ");
					}
					else{
						B.Add("You hear a high-pitched ringing sound. ");
					}
				}
				foreach(Actor a in ActorsWithinDistance(12,true)){
					if(a.type != ActorType.GIANT_BAT && a.type != ActorType.BLOOD_MOTH && a.type != ActorType.CARNIVOROUS_BRAMBLE
					&& a.type != ActorType.LASHER_FUNGUS && a.type != ActorType.PHASE_SPIDER){
						a.FindPath(this);
					}
				}
				Toggle(actor());
				break;
			case TileType.BLINDING_TRAP:
				if(actor_here){
					B.Add("A dart flies out and strikes " + actor().TheName(true) + ". ",this); //todo: what about replacing this with blinding dust?
					if(!actor().HasAttr(AttrType.NONLIVING,AttrType.BLINDSIGHT)){
						actor().ApplyStatus(AttrType.BLIND,(R.Roll(2,6)+6)*100);
						/*B.Add(actor().YouAre() + " blind! ",actor());
						actor().RefreshDuration(AttrType.BLIND,(R.Roll(3,6) + 6) * 100,actor().YouAre() + " no longer blinded. ",actor());*/
					}
					else{
						B.Add("It doesn't affect " + actor().the_name + ". ",actor());
					}
				}
				else{
					B.Add("A dart flies out and hits the floor. ",this);
				}
				Toggle(actor());
				break;
			case TileType.ICE_TRAP:
				if(actor_here){
					if(!actor().IsBurning()){
						if(player.CanSee(this)){
							B.Add("The air suddenly freezes around " + actor().TheName(true) + ". ");
						}
						actor().ApplyFreezing();
					}
					else{
						if(player.CanSee(this)){
							if(player.CanSee(actor())){
								B.Add("Ice crystals form in the air around " + actor().the_name + " but quickly vanish. ");
							}
							else{
								B.Add("Ice crystals form in the air but quickly vanish. ");
							}
						}
					}
				}
				else{
					B.Add("Ice crystals form in the air but quickly vanish. ");
				}
				Toggle(actor());
				break;
			case TileType.PHANTOM_TRAP:
			{
				Tile open = TilesWithinDistance(3).Where(t => t.passable && t.actor() == null && t.HasLOE(this)).RandomOrDefault();
				if(open != null){
					Actor a = Actor.CreatePhantom(open.row,open.col);
					if(a != null){
						a.attrs[AttrType.PLAYER_NOTICED]++;
						a.player_visibility_duration = -1;
						if(player.HasLOS(a)){ //don't print a message if you're just detecting monsters
							B.Add("A ghostly image rises! ",a);
						}
					}
					else{
						B.Add("Nothing happens. ",this);
					}
				}
				else{
					B.Add("Nothing happens. ",this);
				}
				Toggle(actor());
				break;
			}
			case TileType.POISON_GAS_TRAP:
			{
				bool spores = false;
				if(M.current_level >= 5 && R.PercentChance((M.current_level - 4) * 3)){
					//spores = true; //3% at level 5...33% at level 15...48% at level 20. - disabled for now
				}
				int num = R.Roll(5) + 8;
				if(spores){
					List<Tile> new_area = AddGaseousFeature(FeatureType.SPORES,num); //todo: should this be its own trap type? what about other gases?
					if(new_area.Count > 0){
						B.Add("A cloud of spores fills the area! ",this);
						Event.RemoveGas(new_area,600,FeatureType.SPORES,12);
					}
				}
				else{
					List<Tile> new_area = AddGaseousFeature(FeatureType.POISON_GAS,num);
					if(new_area.Count > 0){
						B.Add("Poisonous gas fills the area! ",this);
						Event.RemoveGas(new_area,300,FeatureType.POISON_GAS,18);
					}
				}
				Toggle(actor());
				break;
			}
			case TileType.SCALDING_OIL_TRAP:
			{
				if(actor_here){
					B.Add("Scalding oil pours over " + actor().TheName(true) + "! ",this);
					if(actor().TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(3,6),null,"a scalding oil trap")){
						if(!actor().HasAttr(AttrType.BURNING,AttrType.SLIMED) && !IsBurning()){
							actor().attrs[AttrType.OIL_COVERED] = 1;
							B.Add(actor().YouAre() + " covered in oil. ",actor());
							if(actor() == player){
								Help.TutorialTip(TutorialTopic.Oiled);
							}
						}
					}
				}
				else{
					B.Add("Scalding oil pours over the floor. ",this);
				}
				List<Tile> covered_in_oil = new List<Tile>{this};
				List<Tile> added = new List<Tile>();
				for(int i=0;i<2;++i){
					foreach(Tile t in covered_in_oil){
						foreach(int dir in U.FourDirections){
							Tile neighbor = t.TileInDirection(dir);
							if(neighbor.DistanceFrom(this) == 1 && R.OneIn(3) && neighbor.passable && !covered_in_oil.Contains(neighbor)){
								added.AddUnique(neighbor);
							}
						}
					}
					covered_in_oil.AddRange(added);
				}
				foreach(Tile t in covered_in_oil){
					t.AddFeature(FeatureType.OIL);
				}
				Toggle(actor());
				break;
			}
			case TileType.FLING_TRAP:
			{
				List<int> valid_dirs = new List<int>();
				foreach(int d in U.EightDirections){
					bool good = true;
					Tile current = this;
					for(int i=0;i<2;++i){
						current = current.TileInDirection(d);
						if(current == null || (!current.passable && !current.Is(TileType.BARREL,TileType.CRACKED_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR,TileType.POISON_BULB,TileType.STANDING_TORCH))){
							good = false; //try to pick directions that are either open, or that have interesting things to be knocked into
							break;
						}
					}
					if(good){
						valid_dirs.Add(d);
					}
				}
				Toggle(actor());
				int dir = -1;
				if(valid_dirs.Count > 0){
					dir = valid_dirs.Random();
				}
				else{
					dir = Global.RandomDirection();
				}
				if(actor_here){
					Actor a = actor();
					B.Add("The floor suddenly tilts up under " + a.TheName(true) + "! ",this);
					a.attrs[AttrType.TURN_INTO_CORPSE]++;
					KnockObjectBack(actor(),GetBestExtendedLineOfEffect(TileInDirection(dir)),5,null);
					a.CorpseCleanup();
				}
				else{
					if(inv != null){
						B.Add("The floor suddenly tilts up under " + inv.TheName(true) + "! ",this);
						string item_name = "it";
						string punctuation = ". ";
						if(!player.CanSee(this)){
							item_name = inv.AName(true);
							punctuation = "! ";
						}
						Item i = inv;
						inv = null;
						List<Tile> line = GetBestExtendedLineOfEffect(TileInDirection(dir));
						if(line.Count > 13){
							line = line.ToCount(13); //for range 12
						}
						Tile t = line.LastBeforeSolidTile();
						Actor first = FirstActorInLine(line);
						if(first != null){
							t = first.tile();
							B.Add(item_name + " hits " + first.the_name + punctuation,first);
						}
						line = line.ToFirstSolidTileOrActor();
						if(line.Count > 0){
							line.RemoveAt(line.Count - 1);
						}
						{
							Tile first_unseen = null;
							foreach(Tile tile2 in line){
								if(!tile2.seen){
									first_unseen = tile2;
									break;
								}
							}
							if(first_unseen != null){
								line = line.To(first_unseen);
								if(line.Count > 0){
									line.RemoveAt(line.Count - 1);
								}
							}
						}
						M.Draw();
						if(line.Count > 0){
							Screen.AnimateProjectile(line,new colorchar(i.symbol,i.color));
						}
						if(i.IsBreakable()){
							B.Add("It breaks! ",t);
							i.CheckForMimic();
						}
						else{
							t.GetItem(i);
						}
						t.MakeNoise(2);
						if(first != null){
							//first.player_visibility_duration = -1; //not sure how angry monsters should get in this case
							//first.attrs[AttrType.PLAYER_NOTICED]++;
						}
						else{
							if(t.IsTrap()){
								t.TriggerTrap();
							}
						}
					}
					else{
						B.Add("Nothing happens. ",this);
					}
				}
				break;
			}
			case TileType.STONE_RAIN_TRAP:
				B.Add("Stones fall from the ceiling! ",this);
				if(actor_here){
					Actor a = actor();
					B.Add(a.YouVisibleAre() + " hit! ",this);
					a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),null,"falling stones");
				}
				Toggle(actor());
				foreach(Tile neighbor in TilesWithinDistance(1).Randomize()){
					if(R.PercentChance(40)){
						if(neighbor.IsTrap()){
							B.Add("A bouncing stone triggers a trap. ",neighbor);
						}
						neighbor.ApplyEffect(DamageType.NORMAL); //break items and set off traps
						if(neighbor.Is(TileType.FLOOR)){
							neighbor.Toggle(null,TileType.GRAVEL);
							neighbor.RemoveFeature(FeatureType.SLIME);
							neighbor.RemoveFeature(FeatureType.OIL);
						}
					}
				}
				break;
			default:
				break;
			}
		}
		public void OpenChest(){
			if(type == TileType.CHEST){
				if(spellbooks_generated < 5 && R.OneIn(50)){ //keep an eye on this value
					++spellbooks_generated;
					SpellType spell = SpellType.NO_SPELL;
					List<SpellType> random_spell_list = new List<SpellType>();
					foreach(SpellType sp in Enum.GetValues(typeof(SpellType))){
						random_spell_list.Add(sp);
					}
					while(spell == SpellType.NO_SPELL && random_spell_list.Count > 0){
						SpellType sp = random_spell_list.RemoveRandom();
						if(!player.HasSpell(sp) && sp != SpellType.NO_SPELL && sp != SpellType.NUM_SPELLS){
							spell = sp;
						}
					}
					if(spell != SpellType.NO_SPELL){
						B.Add("You find a spellbook! ");
						B.Add("You learn " + Spell.Name(spell) + ". ");
						player.spells[spell] = true;
						Actor.spells_in_order.Add(spell);
					}
					else{
						B.Add("The chest is empty! ");
					}
				}
				else{
					ConsumableType item = Item.RandomChestItem();
					if(item == ConsumableType.MAGIC_TRINKET){
						List<MagicTrinketType> valid = new List<MagicTrinketType>();
						foreach(MagicTrinketType trinket in Enum.GetValues(typeof(MagicTrinketType))){
							if(trinket != MagicTrinketType.NO_MAGIC_TRINKET && trinket != MagicTrinketType.NUM_MAGIC_TRINKETS && !player.magic_trinkets.Contains(trinket)){
								valid.Add(trinket);
							}
						}
						if(valid.Count > 0){
							MagicTrinketType trinket = valid.Random();
							if(trinket == MagicTrinketType.BRACERS_OF_ARROW_DEFLECTION || trinket == MagicTrinketType.BOOTS_OF_GRIPPING){
								B.Add("You find " + MagicTrinket.Name(trinket) + "! ");
							}
							else{
								B.Add("You find a " + MagicTrinket.Name(trinket) + "! ");
							}
							player.magic_trinkets.Add(trinket);
							Help.TutorialTip(TutorialTopic.MagicTrinkets);
						}
						else{
							B.Add("The chest is empty! ");
						}
					}
					else{
						bool no_room = false;
						if(player.InventoryCount() >= Global.MAX_INVENTORY_SIZE){
							no_room = true;
						}
						Item i = Item.Create(Item.RandomItem(),player);
						if(i != null){
							i.revealed_by_light = true;
							B.Add("You find " + Item.Prototype(i.type).AName() + ". ");
							if(no_room){
								B.Add("Your pack is too full to pick it up. ");
							}
						}
					}
				}
				if(color == Color.Yellow){
					B.Add("There's something else in the chest! ");
					color = Color.DarkYellow;
				}
				else{
					TurnToFloor();
				}
			}
		}
		public bool AppearsOnStatusBar(){
			return IsShrine() || IsKnownTrap() || Is(TileType.STAIRS,TileType.CHEST,TileType.FIRE_GEYSER,TileType.FOG_VENT,TileType.POISON_GAS_VENT,TileType.POOL_OF_RESTORATION,TileType.BLAST_FUNGUS,TileType.DEMONIC_IDOL);
		}
		public void UpdateStatusBarWithTile(){
			if(AppearsOnStatusBar()){
				UI.sidebar_objects.Add(this);
			}
		}
		public void UpdateStatusBarWithFeatures(){
			foreach(FeatureType ft in features){
				if(ft == FeatureType.BONES || ft == FeatureType.GRENADE || ft == FeatureType.STABLE_TELEPORTAL || ft == FeatureType.TELEPORTAL || ft == FeatureType.TROLL_BLOODWITCH_CORPSE || ft == FeatureType.TROLL_CORPSE){
					PhysicalObject o = new PhysicalObject(proto_feature[ft].name,proto_feature[ft].symbol,proto_feature[ft].color);
					o.p = p;
					UI.sidebar_objects.Add(o);
				}
			}
		}
		public bool IsLit(){ //default is player as viewer
			return IsLit(player.row,player.col,false);
		}
		public bool IsLit(int viewer_row,int viewer_col,bool ignore_wizlite_wizdark){
			if(solid_rock){
				return false;
			}
			if(!ignore_wizlite_wizdark){
				if(M.wiz_lite){
					return true;
				}
				if(M.wiz_dark){
					return false;
				}
			}
			if(light_value > 0 || type == TileType.GLOWING_FUNGUS){
				return true;
			}
			if(opaque){
				foreach(Tile t in NonOpaqueNeighborsBetween(viewer_row,viewer_col)){
					if(t.IsLit()){
						return true;
					}
				}
				if(M.actor[viewer_row,viewer_col] != null && M.actor[viewer_row,viewer_col].LightRadius() > 0){
					if(M.actor[viewer_row,viewer_col].LightRadius() >= DistanceFrom(viewer_row,viewer_col)){
						if(M.actor[viewer_row,viewer_col].HasBresenhamLineOfSight(row,col)){
							return true;
						}
					}
				}
			}
			return false;
		}
		public bool IsLitFromAnywhere(){ return IsLitFromAnywhere(opaque); }
		public bool IsLitFromAnywhere(bool considered_opaque){
			if(solid_rock){
				return false;
			}
			if(M.wiz_lite){
				return true;
			}
			if(M.wiz_dark){
				return false;
			}
			if(light_value > 0){
				return true;
			}
			if(considered_opaque){
				foreach(Tile t in TilesAtDistance(1)){
					if(t.light_value > 0){
						return true;
					}
				}
				foreach(Actor a in ActorsWithinDistance(Global.MAX_LIGHT_RADIUS)){
					if(a.LightRadius() > 0 && a.LightRadius() >= a.DistanceFrom(this) && a.HasBresenhamLineOfSight(row,col)){
						return true;
					}
				}
			}
			return false;
		}
		public bool IsTrap(){
			switch(type){
			case TileType.FIRE_TRAP:
			case TileType.GRENADE_TRAP:
			case TileType.LIGHT_TRAP:
			case TileType.SLIDING_WALL_TRAP:
			case TileType.TELEPORT_TRAP:
			case TileType.SHOCK_TRAP:
			case TileType.ALARM_TRAP:
			case TileType.DARKNESS_TRAP:
			case TileType.BLINDING_TRAP:
			case TileType.ICE_TRAP:
			case TileType.PHANTOM_TRAP:
			case TileType.POISON_GAS_TRAP:
			case TileType.SCALDING_OIL_TRAP:
			case TileType.FLING_TRAP:
			case TileType.STONE_RAIN_TRAP:
				return true;
			default:
				return false;
			}
		}
		public bool IsKnownTrap(){
			if(IsTrap() && name != "floor"){
				return true;
			}
			return false;
		}
		public bool IsShrine(){
			switch(type){
			case TileType.COMBAT_SHRINE:
			case TileType.DEFENSE_SHRINE:
			case TileType.MAGIC_SHRINE:
			case TileType.SPIRIT_SHRINE:
			case TileType.STEALTH_SHRINE:
			case TileType.SPELL_EXCHANGE_SHRINE:
				return true;
			default:
				return false;
			}
		}
		public bool IsDoorType(bool count_hidden_doors_as_passable){ //things that aren't passable but shouldn't block certain pathfinding routines
			switch(type){
			case TileType.DOOR_C:
			case TileType.RUBBLE:
			case TileType.STONE_SLAB:
				return true;
			case TileType.HIDDEN_DOOR:
				if(count_hidden_doors_as_passable){
					return true;
				}
				break;
			}
			return false;
		}
		public bool IsPassableOrDoor(){ //but only real doors. maybe I should rename one of these.
			return (passable || type == TileType.DOOR_C);
		}
		public bool BlocksConnectivityOfMap(bool count_hidden_doors_as_passable = true){ //todo: this is all in need of refactoring. How many door methods do I need?!?
			if(passable || IsDoorType(count_hidden_doors_as_passable)){
				return false;
			}
			return true;
		}
		public bool IsSlippery(){
			if(Is(TileType.ICE)){
				return true;
			}
			if(Is(FeatureType.OIL,FeatureType.SLIME) && !IsWater()){
				return true;
			}
			return false;
		}
		public bool IsWater(){
			return Is(TileType.WATER,TileType.POOL_OF_RESTORATION);
		}
		public bool IsFlammableTerrainType(){ //used for terrain that turns to floor when it burns
			switch(type){
			case TileType.BRUSH:
			case TileType.POPPY_FIELD:
			case TileType.POISON_BULB:
			case TileType.VINE:
				return true;
			}
			return false;
		}
		public bool IsCurrentlyFlammable(){
			if(Is(FeatureType.FIRE,FeatureType.POISON_GAS,FeatureType.THICK_DUST)){
				return false;
			}
			if(Is(FeatureType.WEB,FeatureType.OIL,FeatureType.SPORES,FeatureType.CONFUSION_GAS)){
				return true;
			} //todo: check for oiled actors here, too?
			switch(type){
			case TileType.WATER:
			case TileType.POOL_OF_RESTORATION:
				return false;
			case TileType.BRUSH:
			case TileType.POPPY_FIELD:
			case TileType.VINE:
			case TileType.POISON_BULB:
			case TileType.BARREL:
				return true;
			} //below this point, we're checking for things that would be protected from burning by water:
			if(Is(FeatureType.TROLL_CORPSE,FeatureType.TROLL_BLOODWITCH_CORPSE)){
				return true;
			}
			if(inv != null){
				if(inv.type == ConsumableType.BANDAGES || inv.NameOfItemType() == "scroll"){
					return true;
				}
			}
			return false;
		}
		public bool ConductsElectricity(){
			if(IsShrine() || Is(TileType.CHEST,TileType.RUINED_SHRINE,TileType.WATER,TileType.POOL_OF_RESTORATION,TileType.STANDING_TORCH)){
				return true;
			}
			return false;
		}
		delegate int del(int i);
		public List<Tile> NeighborsBetween(int r,int c){ //list of tiles next to this one that are between you and it
			del Clamp = x => x<-1? -1 : x>1? 1 : x; //clamps to a value between -1 and 1
			int dy = r - row;
			int dx = c - col;
			List<Tile> result = new List<Tile>();
			if(dy==0 && dx==0){
				return result; //return the empty set
			}
			int newrow = row+Clamp(dy);
			int newcol = col+Clamp(dx);
			result.Add(M.tile[newrow,newcol]);
			if(Math.Abs(dy) < Math.Abs(dx) && dy!=0){
				newrow -= Clamp(dy);
				result.Add(M.tile[newrow,newcol]);
			}
			if(Math.Abs(dx) < Math.Abs(dy) && dx!=0){
				newcol -= Clamp(dx);
				result.Add(M.tile[newrow,newcol]);
			}
			return result;
		}
		public List<Tile> NonOpaqueNeighborsBetween(int r,int c){ //list of non-opaque tiles next to this one that are between you and it
			del Clamp = x => x<-1? -1 : x>1? 1 : x; //clamps to a value between -1 and 1
			int dy = r - row;
			int dx = c - col;
			List<Tile> result = new List<Tile>();
			if(dy==0 && dx==0){
				return result; //return the empty set
			}
			int newrow = row+Clamp(dy);
			int newcol = col+Clamp(dx);
			if(!M.tile[newrow,newcol].opaque){
				result.Add(M.tile[newrow,newcol]);
			}
			if(Math.Abs(dy) < Math.Abs(dx) && dy!=0){
				newrow -= Clamp(dy);
				if(!M.tile[newrow,newcol].opaque){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			if(Math.Abs(dx) < Math.Abs(dy) && dx!=0){
				newcol -= Clamp(dx);
				if(!M.tile[newrow,newcol].opaque){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			return result;
		}
		public List<Tile> PassableNeighborsBetween(int r,int c){ //list of passable tiles next to this one that are between you and it
			del Clamp = x => x<-1? -1 : x>1? 1 : x; //clamps to a value between -1 and 1
			int dy = r - row;
			int dx = c - col;
			List<Tile> result = new List<Tile>();
			if(dy==0 && dx==0){
				return result; //return the empty set
			}
			int newrow = row+Clamp(dy);
			int newcol = col+Clamp(dx);
			if(M.tile[newrow,newcol].passable){
				result.Add(M.tile[newrow,newcol]);
			}
			if(Math.Abs(dy) < Math.Abs(dx) && dy!=0){
				newrow -= Clamp(dy);
				if(M.tile[newrow,newcol].passable){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			if(Math.Abs(dx) < Math.Abs(dy) && dx!=0){
				newcol -= Clamp(dx);
				if(M.tile[newrow,newcol].passable){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			return result;
		}
		public List<Tile> NeighborsBetweenWithCondition(int r,int c,TileDelegate condition){ //list of tiles next to this one that are between you and it
			del Clamp = x => x<-1? -1 : x>1? 1 : x; //clamps to a value between -1 and 1
			int dy = r - row;
			int dx = c - col;
			List<Tile> result = new List<Tile>();
			if(dy == 0 && dx == 0){
				return result; //return the empty set
			}
			int newrow = row+Clamp(dy);
			int newcol = col+Clamp(dx);
			if(condition(M.tile[newrow,newcol])){
				result.Add(M.tile[newrow,newcol]);
			}
			if(Math.Abs(dy) < Math.Abs(dx) && dy != 0){
				newrow -= Clamp(dy);
				if(condition(M.tile[newrow,newcol])){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			if(Math.Abs(dx) < Math.Abs(dy) && dx != 0){
				newcol -= Clamp(dx);
				if(condition(M.tile[newrow,newcol])){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			return result;
		}
		public List<Tile> AddGaseousFeature(FeatureType f,int num){
			List<Tile> area = new List<Tile>();
			Tile current = this;
			for(int i=0;i<num;++i){
				if(!current.Is(f)){
					current.AddFeature(f);
					area.Add(current);
				}
				else{
					for(int tries=0;tries<50;++tries){
						List<Tile> open = new List<Tile>();
						foreach(Tile t in current.TilesAtDistance(1)){
							if(t.passable){
								open.Add(t);
								if(!t.Is(f)){
									open.Add(t); //3x as likely if it can expand there
									open.Add(t);
								}
							}
						}
						/*foreach(int dir in U.FourDirections){
							if(current.TileInDirection(dir).passable){
								open.Add(current.TileInDirection(dir));
							}
						}*/
						if(open.Count > 0){
							Tile possible = open.Random();
							if(!possible.Is(f)){
								possible.AddFeature(f);
								area.Add(possible);
								break;
							}
							else{
								current = possible;
							}
						}
						else{
							break;
						}
					}
				}
			}
			return area;
		}
		public void ApplyEffect(DamageType effect){
			switch(effect){
			case DamageType.FIRE:
			{
				if(Is(FeatureType.FIRE,FeatureType.POISON_GAS,FeatureType.THICK_DUST)){
					return;
				}
				if(Is(FeatureType.FOG)){
					RemoveOpaqueFeature(FeatureType.FOG);
				}
				if(Is(FeatureType.OIL) || Is(TileType.BARREL)){
					features.Remove(FeatureType.OIL);
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(FeatureType.WEB)){
					features.Remove(FeatureType.WEB);
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				//todo: add static list flammable_gases to Tile class, then foreach it here:
				if(Is(FeatureType.SPORES)){
					features.Remove(FeatureType.SPORES);
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(FeatureType.CONFUSION_GAS)){
					features.Remove(FeatureType.CONFUSION_GAS);
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(TileType.ICE)){
					Toggle(null,TileType.WATER);
				}
				else{
					if(IsWater() || Is(FeatureType.SLIME)){
						return;
					}
				}
				if(Is(FeatureType.TROLL_CORPSE)){
					features.Remove(FeatureType.TROLL_CORPSE);
					B.Add("The troll corpse burns to ashes! ",this);
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
					features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
					B.Add("The troll bloodwitch corpse burns to ashes! ",this);
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(inv != null && (inv.NameOfItemType() == "scroll" || inv.type == ConsumableType.BANDAGES)){
					B.Add(inv.TheName(true) + " burns up! ",this);
					inv.CheckForMimic();
					inv = null;
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(IsFlammableTerrainType()){
					if(Is(TileType.POISON_BULB)){
						B.Add("The poison bulb bursts. ",this);
						TurnToFloor();
						List<Tile> area = AddGaseousFeature(FeatureType.POISON_GAS,8);
						if(area.Count > 0){
							Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
							Event.RemoveGas(area,200,FeatureType.POISON_GAS,18);
						}
						if(Is(FeatureType.POISON_GAS)){
							break;
						}
					}
					else{
						TurnToFloor();
					}
					add_fire_to_features();
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(TileType.WAX_WALL)){
					TurnToFloor();
					foreach(Tile neighbor in TilesAtDistance(1)){
						neighbor.solid_rock = false;
					}
					color = Color.DarkYellow;
				}
				break;
			}
			case DamageType.ELECTRIC:
			{
				break;
			}
			case DamageType.COLD:
			{
				if(Is(FeatureType.SLIME)){
					features.Remove(FeatureType.SLIME);
				}
				if(Is(TileType.WATER)){
					Toggle(null,TileType.ICE);
					if(actor() != null && !actor().HasAttr(AttrType.FLYING) && actor().type != ActorType.FROSTLING){
						B.Add(actor().YouAre() + " stuck in the ice! ",actor());
						actor().RefreshDuration(AttrType.IMMOBILE,100,actor().YouAre() + " no longer stuck in the ice. ",actor());
					}
				}
				if(inv != null){
					if(inv.NameOfItemType() == "potion"){
						if(inv.quantity > 1){
							B.Add(inv.TheName(true) + " break! ",this);
						}
						else{
							B.Add(inv.TheName(true) + " breaks! ",this);
						}
						inv = null;
					}
				}
				break;
			}
			case DamageType.NORMAL:
			{
				BreakFragileFeatures();
				if(type == TileType.HIDDEN_DOOR){
					Toggle(null);
					Toggle(null);
				}
				if(type == TileType.DOOR_C){
					Toggle(null);
				}
				if(IsTrap()){
					TriggerTrap();
				}
				if(Is(TileType.RUBBLE)){
					Toggle(null); //todo: gravel?
				}
				break;
			}
			}
		}
		public void BreakFragileFeatures(){
			if(inv != null){
				if(inv.NameOfItemType() == "potion"){
					if(inv.quantity > 1){
						B.Add(inv.TheName(true) + " break! ",this);
					}
					else{
						B.Add(inv.TheName(true) + " breaks! ",this);
					}
					inv = null;
				}
				else{
					if(inv.NameOfItemType() == "orb"){
						if(inv.quantity > 1){
							B.Add(inv.TheName(true) + " break! ",this);
						}
						else{
							B.Add(inv.TheName(true) + " breaks! ",this);
						}
						Item i = inv;
						inv = null;
						i.Use(null,new List<Tile>{this});
					}
				}
			}
			if(type == TileType.CRACKED_WALL){
				Toggle(null,TileType.FLOOR); //todo: gravel?
				foreach(Tile neighbor in TilesAtDistance(1)){
					neighbor.solid_rock = false;
				}
			}
		}
		public void AddFeature(FeatureType f){
			if(!features.Contains(f)){
				switch(f){
				case FeatureType.FOG:
					if(!Is(FeatureType.FIRE)){
						RemoveAllGases();
						AddOpaqueFeature(FeatureType.FOG);
					}
					break;
				case FeatureType.SPORES:
					if(IsBurning()){
						return;
					}
					RemoveAllGases();
					features.Add(FeatureType.SPORES);
					break;
				case FeatureType.CONFUSION_GAS:
					if(IsBurning()){
						return;
					}
					RemoveAllGases();
					features.Add(FeatureType.CONFUSION_GAS);
					break;
				case FeatureType.POISON_GAS:
				{
					RemoveAllGases();
					if(Is(FeatureType.FIRE)){
						RemoveFeature(FeatureType.FIRE);
						Fire.burning_objects.Remove(this);
						if(name == "floor" && type != TileType.BREACHED_WALL){
							if(R.OneIn(4)){
								color = Color.Gray;
							}
							else{
								color = Color.TerrainDarkGray;
							}
						}
					}
					if(actor() != null && actor().IsBurning()){
						actor().RefreshDuration(AttrType.BURNING,0);
					}
					if(Is(TileType.FIREPIT)){
						Toggle(null,TileType.UNLIT_FIREPIT);
					}
					features.Add(FeatureType.POISON_GAS);
					break;
				}
				case FeatureType.THICK_DUST:
				{
					RemoveAllGases();
					if(Is(FeatureType.FIRE)){
						RemoveFeature(FeatureType.FIRE);
						Fire.burning_objects.Remove(this);
						if(name == "floor" && type != TileType.BREACHED_WALL){
							if(R.OneIn(4)){
								color = Color.Gray;
							}
							else{
								color = Color.TerrainDarkGray;
							}
						}
					}
					if(actor() != null && actor().IsBurning()){
						actor().RefreshDuration(AttrType.BURNING,0);
					}
					if(Is(TileType.FIREPIT)){
						Toggle(null,TileType.UNLIT_FIREPIT);
					}
					AddOpaqueFeature(FeatureType.THICK_DUST);
					break;
				}
				case FeatureType.PIXIE_DUST: //todo
					RemoveAllGases();
					features.Add(FeatureType.PIXIE_DUST);
					break;
				case FeatureType.OIL:
					if(actor() != null && actor().HasAttr(AttrType.BURNING)){
						actor().ApplyBurning();
						AddFeature(FeatureType.FIRE);
						//ApplyEffect(DamageType.FIRE);
					}
					if(Is(FeatureType.SLIME,FeatureType.FIRE) || Is(TileType.CHASM,TileType.BRUSH,TileType.POPPY_FIELD,TileType.GRAVE_DIRT,TileType.GRAVEL,TileType.BLAST_FUNGUS,TileType.GLOWING_FUNGUS,TileType.JUNGLE,TileType.VINE,TileType.TOMBSTONE)){
						return;
					}
					if(type == TileType.FIREPIT){
						add_fire_to_features();
						if(actor() != null){
							actor().ApplyBurning();
						}
					}
					else{
						features.Add(FeatureType.OIL);
					}
					break;
				case FeatureType.FIRE:
				{
					ApplyEffect(DamageType.FIRE);
					if(!Is(FeatureType.POISON_GAS,FeatureType.THICK_DUST,FeatureType.SLIME) && !IsWater()){
						add_fire_to_features();
						if(actor() != null){
							actor().ApplyBurning();
						}
					}
					break;
				}
				case FeatureType.SLIME:
				{
					if(Is(TileType.ICE,TileType.WATER,TileType.POOL_OF_RESTORATION,TileType.CHASM,TileType.BRUSH,TileType.POPPY_FIELD,TileType.GRAVE_DIRT,TileType.GRAVEL,TileType.BLAST_FUNGUS,TileType.GLOWING_FUNGUS,TileType.JUNGLE,TileType.VINE,TileType.TOMBSTONE)){
						return;
					}
					if(Is(FeatureType.FIRE)){
						RemoveFeature(FeatureType.FIRE);
					}
					if(Is(FeatureType.OIL)){
						RemoveFeature(FeatureType.OIL);
					}
					if(Is(TileType.FIREPIT)){
						Toggle(null,TileType.UNLIT_FIREPIT);
					}
					features.Add(FeatureType.SLIME);
					break;
				}
				case FeatureType.TROLL_CORPSE:
				case FeatureType.TROLL_BLOODWITCH_CORPSE:
					if(Is(FeatureType.FIRE) || (Is(TileType.FIREPIT) && !Is(FeatureType.SLIME))){
						B.Add(proto_feature[f].the_name + " burns to ashes! ",this);
						AddFeature(FeatureType.FIRE);
					}
					else{
						features.Add(f);
					}
					break;
				case FeatureType.WEB:
					if(!Is(FeatureType.FIRE)){
						features.Add(FeatureType.WEB);
					}
					break;
				default:
					features.Add(f);
					break;
				}
			}
		}
		public void RemoveFeature(FeatureType f){
			if(features.Contains(f)){
				switch(f){
				case FeatureType.FOG:
					RemoveOpaqueFeature(FeatureType.FOG);
					break;
				case FeatureType.THICK_DUST:
					RemoveOpaqueFeature(FeatureType.THICK_DUST);
					break;
				case FeatureType.FIRE:
					UpdateRadius(1,Prototype(type).light_radius);
					features.Remove(f);
					break;
				default:
					features.Remove(f);
					break;
				}
			}
		}
		public void RemoveAllGases(){
			foreach(FeatureType f in new List<FeatureType>{FeatureType.FOG,FeatureType.PIXIE_DUST,FeatureType.POISON_GAS,FeatureType.SPORES,FeatureType.THICK_DUST,FeatureType.CONFUSION_GAS}){
				RemoveFeature(f);
			}
		}
		private void add_fire_to_features(){ //Let's try lowercase_with_underscores for private 'helper' methods.
			if(!features.Contains(FeatureType.FIRE)){
				if(light_radius == 0){
					UpdateRadius(0,1);
				}
				features.Add(FeatureType.FIRE);
				Fire.AddBurningObject(this);
				if(Is(TileType.UNLIT_FIREPIT)){
					Toggle(null,TileType.FIREPIT);
				}
			}
		}
		private void AddOpaqueFeature(FeatureType f){
			if(!features.Contains(f)){
				bool lighting_update = false;
				List<PhysicalObject> light_sources = new List<PhysicalObject>();
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.tile[i,j].IsLit(player.row,player.col,true)){
							lighting_update = true;
						}
					}
				}
				if(lighting_update){
					for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
						for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
							if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
								if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
									light_sources.Add(M.actor[i,j]);
									M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
								}
								if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
									light_sources.Add(M.tile[i,j].inv);
									M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
								}
								if(M.tile[i,j].light_radius > 0){
									light_sources.Add(M.tile[i,j]);
									M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
								}
								else{
									if(M.tile[i,j].Is(FeatureType.FIRE)){
										light_sources.Add(M.tile[i,j]);
										M.tile[i,j].UpdateRadius(1,0);
									}
								}
							}
						}
					}
				}
	
				features.Add(f);
	
				if(lighting_update){
					foreach(PhysicalObject o in light_sources){
						if(o is Actor){
							Actor a = o as Actor;
							a.UpdateRadius(0,a.LightRadius());
						}
						else{
							if(o is Tile && o.light_radius == 0 && (o as Tile).Is(FeatureType.FIRE)){
								o.UpdateRadius(0,1);
							}
							else{
								o.UpdateRadius(0,o.light_radius);
							}
						}
					}
				}
			}
		}
		private void RemoveOpaqueFeature(FeatureType f){
			if(features.Contains(f)){
				bool lighting_update = false;
				List<PhysicalObject> light_sources = new List<PhysicalObject>();
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.tile[i,j].IsLit(player.row,player.col,true)){
							lighting_update = true;
						}
					}
				}
				if(lighting_update){
					for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
						for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
							if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
								if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
									light_sources.Add(M.actor[i,j]);
									M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
								}
								if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
									light_sources.Add(M.tile[i,j].inv);
									M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
								}
								if(M.tile[i,j].light_radius > 0){
									light_sources.Add(M.tile[i,j]);
									M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
								}
								else{
									if(M.tile[i,j].Is(FeatureType.FIRE)){
										light_sources.Add(M.tile[i,j]);
										M.tile[i,j].UpdateRadius(1,0);
									}
								}
							}
						}
					}
				}
	
				features.Remove(f);
	
				if(lighting_update){
					foreach(PhysicalObject o in light_sources){
						if(o is Actor){
							Actor a = o as Actor;
							a.UpdateRadius(0,a.LightRadius());
						}
						else{
							if(o is Tile && o.light_radius == 0 && (o as Tile).Is(FeatureType.FIRE)){
								o.UpdateRadius(0,1);
							}
							else{
								o.UpdateRadius(0,o.light_radius);
							}
						}
					}
				}
			}
		}
	}
}

