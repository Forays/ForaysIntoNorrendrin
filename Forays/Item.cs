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
using PosArrays;
using Utilities;
namespace Forays{
	public class Item : PhysicalObject{
		public ConsumableType type;
		public int quantity;
		public int charges;
		public int other_data;
		public bool ignored; //whether autoexplore and autopickup should ignore this item
		public bool do_not_stack; //whether the item should be combined with other stacks. used for mimic items too.
		public bool revealed_by_light;

		public static Dictionary<ConsumableType,string> unIDed_name = new Dictionary<ConsumableType,string>();
		public static Dict<ConsumableType,bool> identified = new Dict<ConsumableType, bool>();

		public static Dictionary<ConsumableType,Item> proto = new Dictionary<ConsumableType,Item>();
		public static Item Prototype(ConsumableType type){ return proto[type]; }
		static Item(){
			Define(ConsumableType.HEALING,"potion~ of healing",'!',Color.White);
			Define(ConsumableType.REGENERATION,"potion~ of regeneration",'!',Color.White);
			Define(ConsumableType.STONEFORM,"potion~ of stoneform",'!',Color.White);
			Define(ConsumableType.VAMPIRISM,"potion~ of vampirism",'!',Color.White);
			Define(ConsumableType.BRUTISH_STRENGTH,"potion~ of brutish strength",'!',Color.White);
			Define(ConsumableType.ROOTS,"potion~ of roots",'!',Color.White);
			Define(ConsumableType.HASTE,"potion~ of haste",'!',Color.White);
			Define(ConsumableType.SILENCE,"potion~ of silence",'!',Color.White);
			Define(ConsumableType.CLOAKING,"potion~ of cloaking",'!',Color.White);
			Define(ConsumableType.MYSTIC_MIND,"potion~ of mystic mind",'!',Color.White);
			Define(ConsumableType.BLINKING,"scroll~ of blinking",'?',Color.White);
			Define(ConsumableType.PASSAGE,"scroll~ of passage",'?',Color.White);
			Define(ConsumableType.TIME,"scroll~ of time",'?',Color.White);
			Define(ConsumableType.KNOWLEDGE,"scroll~ of knowledge",'?',Color.White);
			Define(ConsumableType.SUNLIGHT,"scroll~ of sunlight",'?',Color.White);
			Define(ConsumableType.DARKNESS,"scroll~ of darkness",'?',Color.White);
			Define(ConsumableType.RENEWAL,"scroll~ of renewal",'?',Color.White);
			Define(ConsumableType.CALLING,"scroll~ of calling",'?',Color.White);
			Define(ConsumableType.TRAP_CLEARING,"scroll~ of trap clearing",'?',Color.White);
			Define(ConsumableType.ENCHANTMENT,"scroll~ of enchantment",'?',Color.White);
			Define(ConsumableType.THUNDERCLAP,"scroll~ of thunderclap",'?',Color.White);
			Define(ConsumableType.FIRE_RING,"scroll~ of fire ring",'?',Color.White);
			Define(ConsumableType.RAGE,"scroll~ of rage",'?',Color.White);
			Define(ConsumableType.FREEZING,"orb~ of freezing",'*',Color.White);
			Define(ConsumableType.FLAMES,"orb~ of flames",'*',Color.White);
			Define(ConsumableType.FOG,"orb~ of fog",'*',Color.White);
			Define(ConsumableType.DETONATION,"orb~ of detonation",'*',Color.White);
			Define(ConsumableType.BREACHING,"orb~ of breaching",'*',Color.White);
			Define(ConsumableType.SHIELDING,"orb~ of shielding",'*',Color.White);
			Define(ConsumableType.TELEPORTAL,"orb~ of teleportal",'*',Color.White);
			Define(ConsumableType.PAIN,"orb~ of pain",'*',Color.White);
			Define(ConsumableType.CONFUSION,"orb~ of confusion",'*',Color.White);
			Define(ConsumableType.BLADES,"orb~ of blades",'*',Color.White);
			Define(ConsumableType.WEBS,"wand~ of webs",'\\',Color.Yellow);
			proto[ConsumableType.WEBS].do_not_stack = true;
			Define(ConsumableType.DUST_STORM,"wand~ of dust storm",'\\',Color.Yellow);
			proto[ConsumableType.DUST_STORM].do_not_stack = true;
			Define(ConsumableType.FLESH_TO_FIRE,"wand~ of flesh to fire",'\\',Color.Yellow);
			proto[ConsumableType.FLESH_TO_FIRE].do_not_stack = true;
			Define(ConsumableType.INVISIBILITY,"wand~ of invisibility",'\\',Color.Yellow);
			proto[ConsumableType.INVISIBILITY].do_not_stack = true;
			Define(ConsumableType.REACH,"wand~ of reach",'\\',Color.Yellow);
			proto[ConsumableType.REACH].do_not_stack = true;
			Define(ConsumableType.SLUMBER,"wand~ of slumber",'\\',Color.Yellow);
			proto[ConsumableType.SLUMBER].do_not_stack = true;
			Define(ConsumableType.TELEKINESIS,"wand~ of telekinesis",'\\',Color.Yellow);
			proto[ConsumableType.TELEKINESIS].do_not_stack = true;
			Define(ConsumableType.BANDAGES,"roll~ of bandages",'{',Color.White);
			proto[ConsumableType.BANDAGES].revealed_by_light = true;
			Define(ConsumableType.FLINT_AND_STEEL,"flint & steel~",'}',Color.Red);
			proto[ConsumableType.FLINT_AND_STEEL].a_name = "flint & steel~";
			proto[ConsumableType.FLINT_AND_STEEL].revealed_by_light = true;
			Define(ConsumableType.BLAST_FUNGUS,"blast fungus",'%',Color.Red);
			proto[ConsumableType.BLAST_FUNGUS].do_not_stack = true;
			proto[ConsumableType.BLAST_FUNGUS].revealed_by_light = true;
		}
		private static void Define(ConsumableType type_,string name_,char symbol_,Color color_){
			proto[type_] = new Item(type_,name_,symbol_,color_);
		}
		public Item(){}
		public Item(ConsumableType type_,string name_,char symbol_,Color color_){
			type = type_;
			quantity = 1;
			charges = 0;
			other_data = 0;
			ignored = false;
			do_not_stack = false;
			revealed_by_light = false;
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
			row = -1;
			col = -1;
			light_radius = 0;
			sprite_offset = new pos(0,1);
		}
		public Item(Item i,int r,int c){
			type = i.type;
			quantity = 1;
			charges = i.charges;
			other_data = i.other_data;
			switch(type){
			case ConsumableType.INVISIBILITY:
				charges = R.Between(2,3);
				break;
			case ConsumableType.DUST_STORM:
			case ConsumableType.SLUMBER:
			case ConsumableType.TELEKINESIS:
				charges = R.Between(2,6);
				break;
			//case ConsumableType.DIGGING:
			case ConsumableType.FLESH_TO_FIRE:
				charges = R.Between(3,6);
				break;
			case ConsumableType.REACH:
				charges = R.Between(4,7);
				break;
			case ConsumableType.WEBS:
				charges = R.Between(4,9);
				break;
			}
			ignored = false;
			do_not_stack = proto[type].do_not_stack;
			revealed_by_light = proto[type].revealed_by_light;
			name = i.name;
			a_name = i.a_name;
			the_name = i.the_name;
			symbol = i.symbol;
			color = i.color;
			row = r;
			col = c;
			light_radius = i.light_radius;
			sprite_offset = i.sprite_offset;
		}
		public static Item Create(ConsumableType type,int r,int c){
			Item i = null;
			if(M.tile.BoundsCheck(r,c)){
				if(M.tile[r,c].inv == null){
					i = new Item(proto[type],r,c);
					if(i.light_radius > 0){
						i.UpdateRadius(0,i.light_radius);
					}
					M.tile[r,c].inv = i;
				}
				else{
					if(M.tile[r,c].inv.type == type){
						M.tile[r,c].inv.quantity++;
						return M.tile[r,c].inv;
					}
				}
			}
			else{
				i = new Item(proto[type],r,c);
			}
			return i;
		}
		public static Item Create(ConsumableType type,Actor a){
			Item i = null;
			if(a.InventoryCount() < Global.MAX_INVENTORY_SIZE){
				i = new Item(proto[type],-1,-1);
				a.GetItem(i);
				/*foreach(Item held in a.inv){
					if(held.type == type && !held.do_not_stack){
						held.quantity++;
						return held;
					}
				}
				a.inv.Add(i);*/
			}
			else{
				i = Create(type,a.row,a.col);
			}
			return i;
		}
		public bool Is(params ConsumableType[] types){
			foreach(ConsumableType ct in types){
				if(type == ct){
					return true;
				}
			}
			return false;
		}
		public string SingularName(){ return SingularName(false); }
		public string SingularName(bool include_a_or_an){
			string result;
			int position;
			if(identified[type]){
				result = name;
			}
			else{
				result = unIDed_name[type];
			}
			if(include_a_or_an){
				switch(result[0]){
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
					result = "an " + result;
					break;
				default:
					result = "a " + result;
					break;
				}
			}
			position = result.IndexOf('~');
			if(position != -1){
				result = result.Substring(0,position) + result.Substring(position+1);
			}
			return result;
		}
		public string PluralName(){ //with no quantity attached
			string result;
			int position;
			if(identified[type]){
				result = name;
			}
			else{
				result = unIDed_name[type];
			}
			position = result.IndexOf('~');
			if(position != -1){
				result = result.Substring(0,position) + 's' + result.Substring(position+1);
			}
			return result;
		}
		public string NameWithoutQuantity(){
			if(quantity > 1){
				return PluralName();
			}
			return SingularName(false);
		}
		public string Name(){ return Name(false); }
		public string AName(){ return AName(false); }
		public string TheName(){ return TheName(false); }
		public string Name(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "buggy item";
			case 1:
				if(!consider_low_light || !M.tile.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = result.Substring(0,position) + result.Substring(position+1);
					}
					if(type == ConsumableType.BANDAGES || type == ConsumableType.FLINT_AND_STEEL){
						result = result + " (" + other_data.ToString() + ")";
					}
					if(NameOfItemType() == "wand"){
						result = AddWandInfo(result);
					}
					return result;
				}
				else{
					return NameOfItemType();
				}
			default:
				if(!consider_low_light || !M.tile.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
					}
					if(type == ConsumableType.BANDAGES || type == ConsumableType.FLINT_AND_STEEL){
						result = result + " (" + other_data.ToString() + ")";
					}
					if(NameOfItemType() == "wand"){
						result = AddWandInfo(result);
					}
					return result;
				}
				else{
					return qty + " " + NameOfItemType() + "s";
				}
			}
		}
		public string AName(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "a buggy item";
			case 1:
				if(!consider_low_light || !M.tile.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					switch(result[0]){
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
						result = "an " + result;
						break;
					default:
						result = "a " + result;
						break;
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = result.Substring(0,position) + result.Substring(position+1);
					}
					if(type == ConsumableType.BANDAGES || type == ConsumableType.FLINT_AND_STEEL){
						result = result + " (" + other_data.ToString() + ")";
					}
					if(NameOfItemType() == "wand"){
						result = AddWandInfo(result);
					}
					return result;
				}
				else{
					if(NameOfItemType() == "orb"){
						return "an orb";
					}
					else{
						return "a " + NameOfItemType();
					}
				}
			default:
				if(!consider_low_light || !M.tile.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
					}
					if(type == ConsumableType.BANDAGES || type == ConsumableType.FLINT_AND_STEEL){
						result = result + " (" + other_data.ToString() + ")";
					}
					if(NameOfItemType() == "wand"){
						result = AddWandInfo(result);
					}
					return result;
				}
				else{
					return qty + " " + NameOfItemType() + "s";
				}
			}
		}
		public string TheName(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "the buggy item";
			case 1:
				if(!consider_low_light || !M.tile.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = the_name;
					}
					else{
						result = "the " + unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = result.Substring(0,position) + result.Substring(position+1);
					}
					if(type == ConsumableType.BANDAGES || type == ConsumableType.FLINT_AND_STEEL){
						result = result + " (" + other_data.ToString() + ")";
					}
					if(NameOfItemType() == "wand"){
						result = AddWandInfo(result);
					}
					return result;
				}
				else{
					return "the " + NameOfItemType();
				}
			default:
				if(!consider_low_light || !M.tile.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
					}
					if(type == ConsumableType.BANDAGES || type == ConsumableType.FLINT_AND_STEEL){
						result = result + " (" + other_data.ToString() + ")";
					}
					if(NameOfItemType() == "wand"){
						result = AddWandInfo(result);
					}
					return result;
				}
				else{
					return qty + " " + NameOfItemType() + "s";
				}
			}
		}
		private string AddWandInfo(string s){
			string result = s;
			if(other_data != 0 && !identified[type] && unIDed_name[type].Contains("{tried}")){
				result = result.Replace(" {tried}","");
			}
			switch(other_data){ //other_data tracks the number of times the wand has been used
			case -1:
				return result + " (" + charges.ToString() + ")"; // -1 means "number of charges is known"
			case 0:
				return result;
			case 1:
				return result + " {zapped once}";
			case 2:
				return result + " {zapped twice}";
			case 3:
				return result + " {zapped thrice}";
			default:
				return result + " {zapped " + other_data.ToString() + " times}";
			}
		}
		public string NameOfItemType(){
			return NameOfItemType(type);
		}
		public static string NameOfItemType(ConsumableType type){
			switch(type){
			case ConsumableType.HEALING:
			case ConsumableType.REGENERATION:
			case ConsumableType.STONEFORM:
			case ConsumableType.VAMPIRISM:
			case ConsumableType.BRUTISH_STRENGTH:
			case ConsumableType.ROOTS:
			case ConsumableType.HASTE:
			case ConsumableType.SILENCE:
			case ConsumableType.CLOAKING:
			case ConsumableType.MYSTIC_MIND:
				return "potion";
			case ConsumableType.BLINKING:
			case ConsumableType.PASSAGE:
			case ConsumableType.TIME:
			case ConsumableType.KNOWLEDGE:
			case ConsumableType.SUNLIGHT:
			case ConsumableType.DARKNESS:
			case ConsumableType.RENEWAL:
			case ConsumableType.CALLING:
			case ConsumableType.TRAP_CLEARING:
			case ConsumableType.ENCHANTMENT:
			case ConsumableType.THUNDERCLAP:
			case ConsumableType.FIRE_RING:
			case ConsumableType.RAGE:
				return "scroll";
			case ConsumableType.FREEZING:
			case ConsumableType.FLAMES:
			case ConsumableType.FOG:
			case ConsumableType.DETONATION:
			case ConsumableType.BREACHING:
			case ConsumableType.SHIELDING:
			case ConsumableType.TELEPORTAL:
			case ConsumableType.PAIN:
			case ConsumableType.CONFUSION:
			case ConsumableType.BLADES:
				return "orb";
			case ConsumableType.DUST_STORM:
			case ConsumableType.FLESH_TO_FIRE:
			case ConsumableType.INVISIBILITY:
			case ConsumableType.REACH:
			case ConsumableType.SLUMBER:
			case ConsumableType.TELEKINESIS:
			case ConsumableType.WEBS:
				return "wand";
			case ConsumableType.BANDAGES:
			case ConsumableType.FLINT_AND_STEEL:
			case ConsumableType.BLAST_FUNGUS:
				return "other";
			default:
				return "unknown item";
			}
		}
		public int SortOrderOfItemType(){
			switch(type){
			case ConsumableType.HEALING:
			case ConsumableType.REGENERATION:
			case ConsumableType.STONEFORM:
			case ConsumableType.VAMPIRISM:
			case ConsumableType.BRUTISH_STRENGTH:
			case ConsumableType.ROOTS:
			case ConsumableType.HASTE:
			case ConsumableType.SILENCE:
			case ConsumableType.CLOAKING:
			case ConsumableType.MYSTIC_MIND:
				return 0;
			case ConsumableType.BLINKING:
			case ConsumableType.PASSAGE:
			case ConsumableType.TIME:
			case ConsumableType.KNOWLEDGE:
			case ConsumableType.SUNLIGHT:
			case ConsumableType.DARKNESS:
			case ConsumableType.RENEWAL:
			case ConsumableType.CALLING:
			case ConsumableType.TRAP_CLEARING:
			case ConsumableType.ENCHANTMENT:
			case ConsumableType.THUNDERCLAP:
			case ConsumableType.FIRE_RING:
			case ConsumableType.RAGE:
				return 1;
			case ConsumableType.FREEZING:
			case ConsumableType.FLAMES:
			case ConsumableType.FOG:
			case ConsumableType.DETONATION:
			case ConsumableType.BREACHING:
			case ConsumableType.SHIELDING:
			case ConsumableType.TELEPORTAL:
			case ConsumableType.PAIN:
			case ConsumableType.CONFUSION:
			case ConsumableType.BLADES:
				return 2;
			case ConsumableType.DUST_STORM:
			case ConsumableType.FLESH_TO_FIRE:
			case ConsumableType.INVISIBILITY:
			case ConsumableType.REACH:
			case ConsumableType.SLUMBER:
			case ConsumableType.TELEKINESIS:
			case ConsumableType.WEBS:
				return 3;
			case ConsumableType.BANDAGES:
			case ConsumableType.FLINT_AND_STEEL:
				return 4;
			case ConsumableType.BLAST_FUNGUS:
				return 5;
			default:
				return 4;
			}
		}
		public static int Rarity(ConsumableType type){
			switch(type){
			case ConsumableType.ENCHANTMENT:
				return 7;
			case ConsumableType.REACH:
			case ConsumableType.INVISIBILITY:
			case ConsumableType.FLESH_TO_FIRE:
			case ConsumableType.TELEKINESIS:
				return 6;
			case ConsumableType.SLUMBER:
				return 5;
			case ConsumableType.WEBS:
			case ConsumableType.DUST_STORM:
				return 4;
			case ConsumableType.REGENERATION:
			case ConsumableType.SILENCE:
			case ConsumableType.SUNLIGHT:
			case ConsumableType.DARKNESS:
			case ConsumableType.CALLING:
			case ConsumableType.TRAP_CLEARING:
			case ConsumableType.FIRE_RING:
			case ConsumableType.RAGE:
			case ConsumableType.BREACHING:
			case ConsumableType.SHIELDING:
			case ConsumableType.BLADES:
				return 3;
			case ConsumableType.ROOTS:
			case ConsumableType.VAMPIRISM:
			case ConsumableType.TIME:
			case ConsumableType.RENEWAL:
			case ConsumableType.THUNDERCLAP:
			case ConsumableType.FOG:
			case ConsumableType.DETONATION:
			case ConsumableType.TELEPORTAL:
			case ConsumableType.PAIN:
			case ConsumableType.CONFUSION:
				return 2;
			case ConsumableType.BANDAGES:
			case ConsumableType.FLINT_AND_STEEL:
			case ConsumableType.BLAST_FUNGUS:
			case ConsumableType.MAGIC_TRINKET:
				return 0;
			default:
				return 1;
			}
		}
		public static ConsumableType RandomItem(){
			List<ConsumableType> list = new List<ConsumableType>();
			foreach(ConsumableType item in Enum.GetValues(typeof(ConsumableType))){
				if(Item.Rarity(item) == 1){
					list.Add(item);
				}
				else{
					if(Item.Rarity(item) == 0){
						continue;
					}
					if(R.OneIn(Item.Rarity(item))){
						list.Add(item);
					}
				}
			}
			return list.RandomOrDefault();
		}
		public static ConsumableType RandomChestItem(){ //ignores item rarity and includes magic trinkets
			List<ConsumableType> list = new List<ConsumableType>();
			foreach(ConsumableType item in Enum.GetValues(typeof(ConsumableType))){
				if(Item.Rarity(item) >= 1){
					list.Add(item);
				}
			}
			if(R.OneIn(player.magic_trinkets.Count + 1) && player.magic_trinkets.Count < 10){
				for(int i=0;i<5;++i){
					list.Add(ConsumableType.MAGIC_TRINKET);
				}
			}
			return list.RandomOrDefault();
		}
		public bool IsBreakable(){
			if(NameOfItemType() == "potion" || NameOfItemType() == "orb"){
				return true;
			}
			return false;
		}
		public static void GenerateUnIDedNames(){
			identified = new Dict<ConsumableType,bool>();
			List<string> potion_flavors = new List<string>{"vermilion","cerulean","emerald","fuchsia","aquamarine","goldenrod","violet","silver","indigo","crimson"};
			List<Color> potion_colors = new List<Color>{Color.Red,Color.Blue,Color.Green,Color.Magenta,Color.Cyan,Color.Yellow,Color.DarkMagenta,Color.Gray,Color.DarkBlue,Color.DarkRed};
			List<pos> potion_sprites = new List<pos>();
			for(int i=0;i<10;++i){
				potion_sprites.Add(new pos(0,48+i));
			}
			List<string> orb_flavors = new List<string>{"flickering","iridescent","sparkling","chromatic","psychedelic","scintillating","glittering","glimmering","shimmering","kaleidoscopic"};
			List<Color> orb_colors = new List<Color>{Color.RandomRGB,Color.RandomCMY,Color.RandomDRGB,Color.RandomDCMY,Color.RandomRGBW,Color.RandomCMYW,Color.RandomRainbow,Color.RandomBright,Color.RandomDark,Color.RandomAny};
			List<string> wand_flavors = new List<string>{"runed","bone","crystal","brittle","twisted","slender","bent","serpentine","carved","tapered"}; //...etched sturdy smooth flexible inscribed banded polished thick gilded
			foreach(ConsumableType type in Enum.GetValues(typeof(ConsumableType))){
				string type_name = NameOfItemType(type);
				if(type_name == "potion"){
					int num = R.Roll(potion_flavors.Count) - 1;
					unIDed_name[type] = potion_flavors[num] + " potion~";
					proto[type].color = potion_colors[num];
					proto[type].sprite_offset = potion_sprites[num];
					potion_flavors.RemoveAt(num);
					potion_colors.RemoveAt(num);
					potion_sprites.RemoveAt(num);
				}
				else{
					if(type_name == "scroll"){
						unIDed_name[type] = "scroll~ labeled '" + GenerateScrollName() + "'";
						proto[type].sprite_offset = new pos(2,48);
					}
					else{
						if(type_name == "orb"){
							unIDed_name[type] = orb_flavors.RemoveRandom() + " orb~";
							int color_num = R.Roll(orb_colors.Count) - 1;
							proto[type].color = orb_colors[color_num]; //note that color isn't tied to name for orbs. they're all random.
							orb_colors.RemoveAt(color_num);
							proto[type].sprite_offset = new pos(3,48+color_num);
							if(type == ConsumableType.TELEPORTAL){
								Tile.Feature(FeatureType.TELEPORTAL).color = proto[type].color;
							}
						}
						else{
							if(type_name == "wand"){
								unIDed_name[type] = wand_flavors.RemoveRandom() + " wand~";
							}
							else{
								identified[type] = true; //bandages, trap, blast fungus...
								switch(type){
								case ConsumableType.BANDAGES:
									proto[type].sprite_offset = new pos(5,48);
									break;
								case ConsumableType.FLINT_AND_STEEL:
									proto[type].sprite_offset = new pos(5,49);
									break;
								case ConsumableType.BLAST_FUNGUS:
									proto[type].sprite_offset = new pos(5,50);
									break;
								}
							}
						}
					}
				}
			}
		}
		public static string GenerateScrollName(){
			//List<string> vowel = new List<string>{"a","e","i","o","u"};
			//List<string> consonant = new List<string>{"k","s","t","n","h","m","y","r","w","g","d","p","b"}; //Japanese-inspired - used AEIOU, 4 syllables max, and 3-9 total
			//List<string> consonant = new List<string>{"h","k","l","n","m","p","w"}; //Hawaiian-inspired
			//List<string> vowel = new List<string>{"y","i","e","u","ae"}; //some kinda Gaelic-inspired
			//List<string> consonant = new List<string>{"r","t","s","rr","m","n","w","b","c","d","f","g","l","ss","v"}; //some kinda Gaelic-inspired
			List<string> vowel = new List<string>{"a","e","i","o","u","ea","ei","io","a","e","i","o","u","a","e","i","o","u","a","e","i","o","oo","ee","a","e","o"}; //the result of a bunch of tweaking
			List<string> consonant = new List<string>{"k","s","t","n","h","m","y","r","w","g","d","p","b","f","l","v","z","ch","br","cr","dr","fr","gr","kr","pr","tr","th","sc","sh","sk","sl","sm","sn","sp","st","k","s","t","n","m","r","g","d","p","b","l","k","s","t","n","m","r","d","p","b","l",};
			int syllables = 0;
			List<int> syllable_count = null;
			do{
				syllables = R.Roll(4) + 2;
				syllable_count = new List<int>();
				while(syllables > 0){
					if(syllable_count.Count == 2){
						syllable_count.Add(syllables);
						syllables = 0;
						break;
					}
					int R2 = Math.Min(syllables,3);
					int M = 0;
					if(syllable_count.Count == 0){ //sorry, magic numbers here
						M = 6;
					}
					if(syllable_count.Count == 1){
						M = 5;
					}
					int D = 0;
					if(syllable_count.Count == 0){
						D = Math.Max(0,syllables - M);
					}
					int s = R.Roll(R2 - D) + D;
					syllable_count.Add(s);
					syllables -= s;
				}
			}
			while(!syllable_count.Any(x => x!=1)); // if every word has only 1 syllable, try again
			string result = "";
			while(syllable_count.Count > 0){
				string word = "";
				if(R.OneIn(5)){
					word = word + vowel.Random();
				}
				for(int count = syllable_count.RemoveRandom();count > 0;--count){
					word = word + consonant.Random() + vowel.Random();
					/*if(R.OneIn(20)){ //used for the Japanese-inspired one
						word = word + "n";
					}*/
				}
				if(result == ""){
					result = result + word;
				}
				else{
					result = result + " " + word;
				}
			}
			return result;
		}
		public bool Use(Actor user){ return Use(user,null); }
		public bool Use(Actor user,List<Tile> line){
			bool used = true;
			bool IDed = true;
			switch(type){
			case ConsumableType.HEALING:
				user.curhp = user.maxhp;
				B.Add(user.Your() + " wounds are healed completely. ",user);
				break;
			case ConsumableType.REGENERATION:
			{
				if(user == player){
					B.Add("Your blood tingles. ");
				}
				else{
					B.Add(user.the_name + " looks energized. ",user);
				}
				user.attrs[AttrType.REGENERATING]++;
				int duration = 100;
				Q.Add(new Event(user,duration*100,AttrType.REGENERATING));
				break;
			}
			case ConsumableType.STONEFORM:
			{
				B.Add(user.You("transform") + " into a being of animated stone. ",user);
				int duration = R.Roll(2,20) + 20;
				List<AttrType> attributes = new List<AttrType>{AttrType.REGENERATING,AttrType.BRUTISH_STRENGTH,AttrType.VIGOR,AttrType.SILENCE_AURA,AttrType.SHADOW_CLOAK,AttrType.CAN_DODGE,AttrType.MENTAL_IMMUNITY,AttrType.DETECTING_MONSTERS};
				foreach(AttrType at in attributes){ //in the rare case where a monster drinks this potion, it can lose these natural statuses permanently. this might eventually be fixed.
					if(user.HasAttr(at)){
						user.attrs[at] = 0;
						Q.KillEvents(user,at);
						switch(at){
						case AttrType.REGENERATING:
							B.Add(user.You("no longer regenerate") + ". ",user);
							break;
						case AttrType.BRUTISH_STRENGTH:
							B.Add(user.Your() + " brutish strength fades. ",user);
							break;
						case AttrType.VIGOR:
							B.Add(user.Your() + " extraordinary speed fades. ",user);
							break;
						case AttrType.SILENCED:
							B.Add(user.You("no longer radiate") + " an aura of silence. ",user);
							break;
						case AttrType.SHADOW_CLOAK:
							B.Add(user.YouAre() + " no longer cloaked. ",user);
							break;
						case AttrType.MENTAL_IMMUNITY:
							B.Add(user.Your() + " consciousness returns to normal. ",user);
							break;
						}
					}
				}
				if(user.HasAttr(AttrType.PSEUDO_VAMPIRIC)){
					user.attrs[AttrType.LIGHT_SENSITIVE] = 0;
					user.attrs[AttrType.FLYING] = 0;
					user.attrs[AttrType.PSEUDO_VAMPIRIC] = 0;
					Q.KillEvents(user,AttrType.LIGHT_SENSITIVE);
					Q.KillEvents(user,AttrType.FLYING);
					Q.KillEvents(user,AttrType.PSEUDO_VAMPIRIC);
					B.Add(user.YouAre() + " no longer vampiric. ",user);
				}
				if(user.HasAttr(AttrType.ROOTS)){
					foreach(Event e in Q.list){
						if(e.target == user && !e.dead){
							if(e.attr == AttrType.IMMOBILE && e.msg.Contains("rooted to the ground")){
								e.dead = true;
								user.attrs[AttrType.IMMOBILE]--;
								B.Add(user.YouAre() + " no longer rooted to the ground. ",user);
							}
							else{
								if(e.attr == AttrType.BONUS_DEFENSE && e.value == 10){
									e.dead = true; //this would break if there were other timed effects that gave the same amount of defense
									user.attrs[AttrType.BONUS_DEFENSE] -= 10;
								}
								else{
									if(e.attr == AttrType.ROOTS){
										e.dead = true;
										user.attrs[AttrType.ROOTS]--;
									}
								}
							}
						}
					}
				}
				if(user.HasAttr(AttrType.BURNING)){
					user.RefreshDuration(AttrType.BURNING,0);
				}
				user.attrs[AttrType.IMMUNE_BURNING]++;
				Q.Add(new Event(user,duration*100,AttrType.IMMUNE_BURNING));
				user.attrs[AttrType.DAMAGE_RESISTANCE]++;
				Q.Add(new Event(user,duration*100,AttrType.DAMAGE_RESISTANCE));
				user.RefreshDuration(AttrType.NONLIVING,duration*100,user.Your() + " rocky form reverts to flesh. ",user);
				if(user == player){
					Help.TutorialTip(TutorialTopic.Stoneform);
				}
				break;
			}
			case ConsumableType.VAMPIRISM:
			{
				B.Add(user.You("become") + " vampiric. ",user);
				B.Add(user.You("rise") + " into the air. ",user);
				int duration = R.Roll(2,20) + 20;
				user.RefreshDuration(AttrType.LIGHT_SENSITIVE,duration*100);
				user.RefreshDuration(AttrType.FLYING,duration*100);
				user.RefreshDuration(AttrType.PSEUDO_VAMPIRIC,duration*100,user.YouAre() + " no longer vampiric. ",user);
				if(user == player){
					Help.TutorialTip(TutorialTopic.Vampirism);
				}
				break;
			}
			case ConsumableType.BRUTISH_STRENGTH:
			{
				if(user == player){
					B.Add("You feel a surge of strength. ");
				}
				else{
					B.Add(user.Your() + " muscles ripple. ",user);
				}
				user.RefreshDuration(AttrType.BRUTISH_STRENGTH,(R.Roll(3,6)+16)*100,user.Your() + " incredible strength wears off. ",user);
				if(user == player){
					Help.TutorialTip(TutorialTopic.BrutishStrength);
				}
				break;
			}
			case ConsumableType.ROOTS:
			{
				if(user.HasAttr(AttrType.ROOTS)){
					foreach(Event e in Q.list){
						if(e.target == user && !e.dead){
							if(e.attr == AttrType.IMMOBILE && e.msg.Contains("rooted to the ground")){
								e.dead = true;
								user.attrs[AttrType.IMMOBILE]--;
							}
							else{
								if(e.attr == AttrType.BONUS_DEFENSE && e.value == 10){
									e.dead = true; //this would break if there were other timed effects that gave 5 defense
									user.attrs[AttrType.BONUS_DEFENSE] -= 10;
								}
								else{
									if(e.attr == AttrType.ROOTS){
										e.dead = true;
										user.attrs[AttrType.ROOTS]--;
									}
								}
							}
						}
					}
					B.Add(user.Your() + " roots extend deeper into the ground. ",user);
				}
				else{
					B.Add(user.You("grow") + " roots and a hard shell of bark. ",user);
				}
				int duration = R.Roll(20) + 20;
				user.RefreshDuration(AttrType.ROOTS,duration*100);
				user.attrs[AttrType.BONUS_DEFENSE] += 10;
				Q.Add(new Event(user,duration*100,AttrType.BONUS_DEFENSE,10));
				user.attrs[AttrType.IMMOBILE]++;
				Q.Add(new Event(user,duration*100,AttrType.IMMOBILE,user.YouAre() + " no longer rooted to the ground. ",user));
				if(user == player){
					Help.TutorialTip(TutorialTopic.Roots);
				}
				if(user.HasAttr(AttrType.FLYING) && user.tile().IsTrap()){
					user.tile().TriggerTrap();
				}
				break;
			}
			case ConsumableType.HASTE:
			{
				B.Add(user.You("start") + " moving with extraordinary speed. ",user);
				int duration = (R.Roll(2,10) + 10) * 100;
				user.RefreshDuration(AttrType.CAN_DODGE,duration); //todo: dodging tip goes here
				user.RefreshDuration(AttrType.VIGOR,duration,user.Your() + " extraordinary speed fades. ",user);
				if(user == player){
					Help.TutorialTip(TutorialTopic.IncreasedSpeed);
				}
				break;
			}
			case ConsumableType.SILENCE:
			{
				B.Add("A hush falls around " + user.the_name + ". ",user);
				user.RefreshDuration(AttrType.SILENCE_AURA,(R.Roll(2,20)+20)*100,user.You("no longer radiate") + " an aura of silence. ",user);
				if(user == player){
					Help.TutorialTip(TutorialTopic.Silenced);
				}
				break;
			}
			case ConsumableType.CLOAKING:
				if(user.tile().IsLit()){
					if(user == player){
						B.Add("You would feel at home in the shadows. ");
					}
					else{
						B.Add("A shadow moves across " + user.the_name + ". ",user);
					}
				}
				else{
					B.Add(user.You("fade") + " away in the darkness. ",user);
				}
				user.RefreshDuration(AttrType.SHADOW_CLOAK,(R.Roll(2,20)+30)*100,user.YouAre() + " no longer cloaked. ",user);
				break;
			case ConsumableType.MYSTIC_MIND:
			{
				B.Add(user.Your() + " mind expands. ",user);
				int duration = R.Roll(2,20)+60;
				user.attrs[AttrType.ASLEEP] = 0;
				//user.RefreshDuration(AttrType.MAGICAL_DROWSINESS,0);
				user.RefreshDuration(AttrType.CONFUSED,0);
				user.RefreshDuration(AttrType.STUNNED,0);
				user.RefreshDuration(AttrType.ENRAGED,0);
				user.RefreshDuration(AttrType.MENTAL_IMMUNITY,duration*100);
				user.RefreshDuration(AttrType.DETECTING_MONSTERS,duration*100,user.Your() + " consciousness returns to normal. ",user);
				if(user == player){
					Help.TutorialTip(TutorialTopic.MysticMind);
				}
				break;
			}
			case ConsumableType.BLINKING:
			{
				List<Tile> tiles = user.TilesWithinDistance(8).Where(x => x.passable && x.actor() == null && user.ApproximateEuclideanDistanceFromX10(x) >= 45);
				if(tiles.Count > 0 && !user.HasAttr(AttrType.IMMOBILE)){
					Tile t = tiles.Random();
					B.Add(user.You("step") + " through a rip in reality. ",M.tile[user.p],t);
					user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
					user.Move(t.row,t.col);
					M.Draw();
					user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
				}
				else{
					B.Add("Nothing happens. ",user);
					IDed = false;
				}
				break;
			}
			case ConsumableType.PASSAGE:
			{
				if(user.HasAttr(AttrType.IMMOBILE)){
					B.Add("Nothing happens. ",user);
					IDed = false;
					break;
				}
				List<int> valid_dirs = new List<int>();
				foreach(int dir in U.FourDirections){
					Tile t = user.TileInDirection(dir);
					if(t != null && t.Is(TileType.WALL,TileType.CRACKED_WALL,TileType.WAX_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR,TileType.STONE_SLAB)){
						while(!t.passable){
							if(t.row == 0 || t.row == Global.ROWS-1 || t.col == 0 || t.col == Global.COLS-1){
								break;
							}
							t = t.TileInDirection(dir);
						}
						if(t.passable){
							valid_dirs.Add(dir);
						}
					}
				}
				if(valid_dirs.Count > 0){
					int dir = valid_dirs.Random();
					Tile t = user.TileInDirection(dir);
					colorchar ch = new colorchar(Color.Cyan,'!');
					switch(user.DirectionOf(t)){
					case 8:
					case 2:
						ch.c = '|';
						break;
					case 4:
					case 6:
						ch.c = '-';
						break;
					}
					List<Tile> tiles = new List<Tile>();
					List<colorchar> memlist = new List<colorchar>();
					Screen.CursorVisible = false;
					Tile last_wall = null;
					while(!t.passable){
						tiles.Add(t);
						memlist.Add(Screen.MapChar(t.row,t.col));
						Screen.WriteMapChar(t.row,t.col,ch);
						Game.GLUpdate();
						Thread.Sleep(35);
						last_wall = t;
						t = t.TileInDirection(dir);
					}
					Input.FlushInput();
					if(t.actor() == null){
						int r = user.row;
						int c = user.col;
						user.Move(t.row,t.col);
						Screen.WriteMapChar(r,c,M.VisibleColorChar(r,c));
						Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
						int idx = 0;
						foreach(Tile tile in tiles){
							Screen.WriteMapChar(tile.row,tile.col,memlist[idx++]);
							Game.GLUpdate();
							Thread.Sleep(35);
						}
						Input.FlushInput();
						B.Add(user.You("travel") + " through the passage. ",user,t);
					}
					else{
						Tile destination = null;
						List<Tile> adjacent = t.TilesAtDistance(1).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(last_wall) == 1);
						if(adjacent.Count > 0){
							destination = adjacent.Random();
						}
						else{
							foreach(Tile tile in M.ReachableTilesByDistance(t.row,t.col,false)){
								if(tile.actor() == null){
									destination = tile;
									break;
								}
							}
						}
						if(destination != null){
							int r = user.row;
							int c = user.col;
							user.Move(destination.row,destination.col);
							Screen.WriteMapChar(r,c,M.VisibleColorChar(r,c));
							Screen.WriteMapChar(destination.row,destination.col,M.VisibleColorChar(destination.row,destination.col));
							int idx = 0;
							foreach(Tile tile in tiles){
								Screen.WriteMapChar(tile.row,tile.col,memlist[idx++]);
								Game.GLUpdate();
								Thread.Sleep(35);
							}
							Input.FlushInput();
							B.Add(user.You("travel") + " through the passage. ",user,destination);
						}
						else{
							B.Add("Something blocks " + user.Your() + " movement through the passage. ",user);
						}
					}
				}
				else{
					B.Add("Nothing happens. ",user);
					IDed = false;
				}
				break;
			}
			case ConsumableType.TIME:
				if(user == player){
					B.Add("Time stops for a moment. ",user);
				}
				else{
					B.Add("Time warps around " + user.the_name + "! ",user);
					B.PrintAll();
				}
				if(Fire.fire_event == null){ //this prevents fire from updating while time is frozen
					Fire.fire_event = new Event(0,EventType.FIRE);
					Fire.fire_event.tiebreaker = 0;
					Q.Add(Fire.fire_event);
				}
				Q.turn -= 200;
				break;
			case ConsumableType.KNOWLEDGE:
			{
				if(user == player){
					B.Add("Knowledge fills your mind. ");
					Event hiddencheck = null;
					foreach(Event e in Q.list){
						if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
							hiddencheck = e;
							break;
						}
					}
					int max_dist = 0;
					List<Tile> last_tiles = new List<Tile>();
					foreach(Tile t in M.ReachableTilesByDistance(user.row,user.col,true,TileType.STONE_SLAB,TileType.DOOR_C,TileType.STALAGMITE,TileType.RUBBLE,TileType.HIDDEN_DOOR)){
						if(t.type != TileType.FLOOR){
							t.seen = true;
							if(t.type != TileType.WALL){
								t.revealed_by_light = true;
							}
							if(t.IsTrap() || t.Is(TileType.HIDDEN_DOOR)){
								if(hiddencheck != null){
									hiddencheck.area.Remove(t);
								}
							}
							if(t.IsTrap()){
								t.name = Tile.Prototype(t.type).name;
								t.a_name = Tile.Prototype(t.type).a_name;
								t.the_name = Tile.Prototype(t.type).the_name;
								t.symbol = Tile.Prototype(t.type).symbol;
								t.color = Tile.Prototype(t.type).color;
							}
							if(t.Is(TileType.HIDDEN_DOOR)){
								t.Toggle(null);
							}
							colorchar ch2 = Screen.BlankChar();
							if(t.inv != null){
								t.inv.revealed_by_light = true;
								ch2.c = t.inv.symbol;
								ch2.color = t.inv.color;
								M.last_seen[t.row,t.col] = ch2;
							}
							else{
								if(t.features.Count > 0){
									ch2 = t.FeatureVisual();
									M.last_seen[t.row,t.col] = ch2;
								}
								else{
									ch2.c = t.symbol;
									ch2.color = t.color;
									if(ch2.c == '#' && ch2.color == Color.RandomGlowingFungus){
										ch2.color = Color.Gray;
									}
									M.last_seen[t.row,t.col] = ch2;
								}
							}
							Screen.WriteMapChar(t.row,t.col,t.symbol,Color.RandomRainbow);
							//Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
							if(user.DistanceFrom(t) > max_dist){
								max_dist = user.DistanceFrom(t);
								Game.GLUpdate();
								Thread.Sleep(10);
								while(last_tiles.Count > 0){
									Tile t2 = last_tiles.RemoveRandom();
									Screen.WriteMapChar(t2.row,t2.col,M.last_seen[t2.row,t2.col]);
									//Screen.WriteMapChar(t2.row,t2.col,M.VisibleColorChar(t2.row,t2.col));
								}
							}
							last_tiles.Add(t);
						}
					}
					if(user.inv.Count > 0){
						foreach(Item i in user.inv){
							identified[i.type] = true;
							if(i.NameOfItemType() == "wand"){
								i.other_data = -1;
							}
						}
					}
				}
				else{
					B.Add(user.the_name + " looks more knowledgeable. ",user);
				}
				break;
			}
			case ConsumableType.SUNLIGHT:
				if(M.wiz_lite == false){
					B.Add("The air itself seems to shine. ");
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
				break;
			case ConsumableType.DARKNESS:
				if(M.wiz_dark == false){
					B.Add("The air itself grows dark. ");
					if(player.light_radius > 0){
						B.Add("Your light is extinguished! ");
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
				break;
			case ConsumableType.RENEWAL:
			{
				B.Add("A glow envelops " + user.the_name + ". ",user);
				//B.Add("A glow envelops " + user.Your() + " equipment. ",user);
				bool repaired = false;
				foreach(EquipmentStatus eqstatus in Enum.GetValues(typeof(EquipmentStatus))){
					foreach(Weapon w in user.weapons){
						if(w.status[eqstatus]){
							repaired = true;
							w.status[eqstatus] = false;
						}
					}
					foreach(Armor a in user.armors){
						if(a.status[eqstatus]){
							repaired = true;
							a.status[eqstatus] = false;
						}
					}
				}
				if(repaired){
					B.Add(user.Your() + " equipment looks as good as new! ",user);
				}
				if(user.HasAttr(AttrType.SLIMED)){
					B.Add(user.YouAre() + " no longer covered in slime. ",user);
					user.attrs[AttrType.SLIMED] = 0;
				}
				if(user.HasAttr(AttrType.OIL_COVERED)){
					B.Add(user.YouAre() + " no longer covered in oil. ",user);
					user.attrs[AttrType.OIL_COVERED] = 0;
				}
				int recharged = 0;
				foreach(Item i in user.inv){
					if(i.NameOfItemType() == "wand"){
						i.charges++;
						recharged++;
					}
				}
				if(recharged > 0){
					if(recharged == 1){
						B.Add("The glow charges " + user.Your() + " wand. ",user);
					}
					else{
						B.Add("The glow charges " + user.Your() + " wands. ",user);
					}
				}
				break;
			}
			case ConsumableType.CALLING:
			{
				bool found = false;
				if(user == player){
					for(int dist = 1;dist < Math.Max(Global.ROWS,Global.COLS);++dist){
						List<Tile> tiles = user.TilesAtDistance(dist).Where(x=>x.actor() != null && !x.actor().HasAttr(AttrType.IMMOBILE));
						if(tiles.Count > 0){
							Actor a = tiles.Random().actor();
							Tile t2 = user.TileInDirection(user.DirectionOf(a));
							if(t2.passable && t2.actor() == null){
								B.Add("The scroll calls " + a.a_name + " to you. ");
								a.Move(t2.row,t2.col);
								found = true;
								break;
							}
							foreach(Tile t in M.ReachableTilesByDistance(user.row,user.col,false)){
								if(t.actor() == null){
									B.Add("The scroll calls " + a.a_name + " to you. ");
									a.Move(t.row,t.col);
									found = true;
									break;
								}
							}
							if(found){
								break;
							}
						}
					}
				}
				else{
					if(!player.HasAttr(AttrType.IMMOBILE) && user.DistanceFrom(player) > 1){
						Tile t2 = user.TileInDirection(user.DirectionOf(player));
						if(t2.passable && t2.actor() == null){
							B.Add("The scroll calls you to " + user.TheName(true) + ". ");
							player.Move(t2.row,t2.col);
							found = true;
						}
						if(!found){
							foreach(Tile t in M.ReachableTilesByDistance(user.row,user.col,false)){
								if(t.actor() == null){
									B.Add("The scroll calls you to " + user.TheName(true) + ". ");
									player.Move(t.row,t.col);
									found = true;
									break;
								}
							}
						}
					}
				}
				if(!found){
					B.Add("Nothing happens. ",user);
					IDed = false;
				}
				break;
			}
			case ConsumableType.TRAP_CLEARING:
			{
				List<Tile> traps = new List<Tile>();
				{
					List<Tile>[] traparray = new List<Tile>[5];
					for(int i=0;i<5;++i){
						traparray[i] = new List<Tile>();
					}
					for(int i=0;i<=12;++i){
						foreach(Tile t in user.TilesAtDistance(i)){ //all this ensures that the traps go off in the best order
							switch(t.type){
							case TileType.ALARM_TRAP:
							case TileType.TELEPORT_TRAP:
							case TileType.ICE_TRAP:
							case TileType.BLINDING_TRAP:
							case TileType.SHOCK_TRAP:
							case TileType.FIRE_TRAP:
							case TileType.SCALDING_OIL_TRAP:
								traparray[0].Add(t);
								break;
							case TileType.POISON_GAS_TRAP:
							case TileType.GRENADE_TRAP:
								traparray[1].Add(t);
								break;
							case TileType.SLIDING_WALL_TRAP:
							case TileType.PHANTOM_TRAP:
								traparray[2].Add(t);
								break;
							case TileType.LIGHT_TRAP:
							case TileType.DARKNESS_TRAP:
								traparray[3].Add(t);
								break;
							case TileType.FLING_TRAP:
							case TileType.STONE_RAIN_TRAP:
								traparray[4].Add(t);
								break;
							}
						}
					}
					for(int i=0;i<5;++i){
						foreach(Tile t in traparray[i]){
							traps.Add(t);
						}
					}
				}
				if(traps.Count > 0){
					B.Add("*CLICK*. ");
					foreach(Tile t in traps){
						t.TriggerTrap(false);
					}
				}
				else{
					B.Add("Nothing happens. ",user);
					IDed = false;
				}
				break;
			}
			case ConsumableType.ENCHANTMENT:
			{
				if(user == player){
					EnchantmentType ench = (EnchantmentType)R.Between(0,4);
					while(ench == user.EquippedWeapon.enchantment){
						ench = (EnchantmentType)R.Between(0,4);
					}
					B.Add("Your " + user.EquippedWeapon.NameWithEnchantment() + " glows brightly! ");
					user.EquippedWeapon.enchantment = ench;
					B.Add("Your " + user.EquippedWeapon.NameWithoutEnchantment() + " is now a " + user.EquippedWeapon.NameWithEnchantment() + "! ");
				}
				else{
					B.Add("Nothing happens. ",user);
					IDed = false;
				}
				break;
			}
			case ConsumableType.THUNDERCLAP:
			{
				B.Add("Thunder crashes! ",user);
				var scr = Screen.GetCurrentMap();
				List<Tile>[] printed = new List<Tile>[13];
				Color leading_edge_color = Color.White;
				Color trail_color = Color.DarkCyan;
				if(Global.LINUX && !Screen.GLMode){
					leading_edge_color = Color.Gray;
				}
				for(int dist=0;dist<=12;++dist){
					printed[dist] = new List<Tile>();
					foreach(Tile t in user.TilesAtDistance(dist)){
						if(t.seen && user.HasLOE(t)){
							printed[dist].Add(t);
						}
					}
					foreach(Tile t in printed[dist]){
						colorchar cch = M.VisibleColorChar(t.row,t.col);
						cch.bgcolor = leading_edge_color;
						if(cch.color == leading_edge_color){
							cch.color = Color.Black;
						}
						Screen.WriteMapChar(t.row,t.col,cch);
					}
					if(dist > 0){
						foreach(Tile t in printed[dist-1]){
							colorchar cch = M.VisibleColorChar(t.row,t.col);
							cch.bgcolor = trail_color;
							if(cch.color == trail_color){
								cch.color = Color.Black;
							}
							Screen.WriteMapChar(t.row,t.col,cch);
						}
						if(dist > 4){
							foreach(Tile t in printed[dist-5]){
								Screen.WriteMapChar(t.row,t.col,scr[t.row,t.col]);
							}
						}
					}
					Game.GLUpdate();
					Thread.Sleep(10);
				}
				List<Actor> actors = new List<Actor>();
				for(int dist=0;dist<=12;++dist){
					foreach(Tile t in user.TilesAtDistance(dist).Randomize()){
						if(user.HasLOE(t)){
							if(t.actor() != null && t.actor() != user){
								actors.Add(t.actor());
							}
							t.BreakFragileFeatures();
						}
					}
				}
				foreach(Actor a in actors){
					if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(4,6),user,"a scroll of thunderclap")){
						a.ApplyStatus(AttrType.STUNNED,R.Between(5,10)*100);
					}
				}
				user.MakeNoise(12);
				break;
			}
			case ConsumableType.FIRE_RING:
			{
				List<pos> cells = new List<pos>();
				List<Tile> valid = new List<Tile>();
				foreach(Tile t in user.TilesWithinDistance(3)){
					if(t.passable && user.DistanceFrom(t) > 1 && user.HasLOE(t) && user.ApproximateEuclideanDistanceFromX10(t) < 45){
						valid.Add(t);
						cells.Add(t.p);
					}
				}
				if(valid.Count > 0){
					if(player.CanSee(user)){
						B.Add("A ring of fire surrounds " + user.the_name + ". ");
					}
					else{
						B.Add("A ring of fire appears! ",user.tile());
					}
					valid.Randomize();
					foreach(Tile t in valid){
						t.AddFeature(FeatureType.FIRE);
					}
					Screen.AnimateMapCells(cells,new colorchar('&',Color.RandomFire));
				}
				else{
					B.Add("Nothing happens. ",user);
					IDed = false;
				}
				break;
			}
			case ConsumableType.RAGE:
			{
				B.Add("A murderous red glow cascades outward. ",user);
				List<Tile>[] printed = new List<Tile>[13];
				Color leading_edge_color = Color.Red;
				Color trail_color = Color.DarkRed;
				if(Global.LINUX && !Screen.GLMode){
					leading_edge_color = Color.DarkRed;
				}
				for(int dist=0;dist<=12;++dist){
					printed[dist] = new List<Tile>();
					foreach(Tile t in user.TilesAtDistance(dist)){
						if(t.seen && user.HasLOS(t)){
							printed[dist].Add(t);
						}
					}
					foreach(Tile t in printed[dist]){
						colorchar cch = M.VisibleColorChar(t.row,t.col);
						cch.bgcolor = leading_edge_color;
						if(cch.color == leading_edge_color){
							cch.color = Color.Black;
						}
						Screen.WriteMapChar(t.row,t.col,cch);
					}
					if(dist > 0){
						foreach(Tile t in printed[dist-1]){
							colorchar cch = M.VisibleColorChar(t.row,t.col);
							cch.bgcolor = trail_color;
							if(cch.color == trail_color){
								cch.color = Color.Black;
							}
							Screen.WriteMapChar(t.row,t.col,cch);
						}
					}
					Game.GLUpdate();
					Thread.Sleep(5);
				}
				int actors_affected = 0;
				string name_is = "";
				foreach(Actor a in M.AllActors()){
					if(a != user && user.DistanceFrom(a) <= 12 && user.HasLOS(a)){
						a.ApplyStatus(AttrType.ENRAGED,R.Between(10,17)*100,false,"",a.You("calm") + " down. ",a.You("resist") + "! ");
						actors_affected++;
						if(player.CanSee(a)){
							name_is = a.YouAre();
						}
					}
				}
				if(actors_affected > 0){
					if(actors_affected == 1){
						B.Add(name_is + " enraged! ");
					}
					else{
						B.Add("Bloodlust fills the air. ");
					}
				}
				break;
			}
			case ConsumableType.FREEZING:
			{
				ItemUseResult orb_result = UseOrb(2,false,user,line,(t,LOE_tile,results)=>{
					Screen.AnimateExplosion(t,2,new colorchar('*',Color.RandomIce));
					List<Tile> targets = new List<Tile>();
					foreach(Tile t2 in t.TilesWithinDistance(2)){
						if(LOE_tile.HasLOE(t2)){
							targets.Add(t2);
						}
					}
					while(targets.Count > 0){
						Tile t2 = targets.RemoveRandom();
						t2.ApplyEffect(DamageType.COLD);
						Actor ac = t2.actor();
						if(ac != null){
							ac.ApplyFreezing();
						}
					}
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.FLAMES:
			{
				ItemUseResult orb_result = UseOrb(2,false,user,line,(t,LOE_tile,results)=>{
					List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					foreach(Tile tile in t.TilesWithinDistance(2)){
						if(LOE_tile.HasLOE(tile)){
							if(tile.passable){
								tile.AddFeature(FeatureType.FIRE);
							}
							else{
								tile.ApplyEffect(DamageType.FIRE);
							}
							if(tile.Is(FeatureType.FIRE)){
								area.Add(tile);
							}
							cells.Add(tile.p);
						}
					}
					Screen.AnimateMapCells(cells,new colorchar('&',Color.RandomFire));
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.FOG:
			{
				ItemUseResult orb_result = UseOrb(3,false,user,line,(t,LOE_tile,results)=>{
					List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					colorchar cch = new colorchar('*',Color.Gray);
					for(int i=0;i<=3;++i){
						foreach(Tile tile in t.TilesAtDistance(i)){
							if(tile.passable && LOE_tile.HasLOE(tile)){
								tile.AddFeature(FeatureType.FOG);
								area.Add(tile);
								cells.Add(tile.p);
								if(tile.seen){
									M.last_seen[tile.row,tile.col] = cch;
								}
							}
						}
						Screen.AnimateMapCells(cells,cch,40);
					}
					Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
					Event.RemoveGas(area,800,FeatureType.FOG,25);
					//Q.Add(new Event(area,600,EventType.FOG,25));
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.DETONATION:
			{
				ItemUseResult orb_result = UseOrb(3,false,user,line,(t,LOE_tile,results)=>{
					LOE_tile.ApplyExplosion(3,user,"an orb of detonation");
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.BREACHING:
			{
				ItemUseResult orb_result = UseOrb(5,false,user,line,(t,LOE_tile,results)=>{
					int max_dist = -1;
					foreach(Tile t2 in M.TilesByDistance(t.row,t.col,false,true)){
						if(t.DistanceFrom(t2) > 5){
							break;
						}
						if(t2.Is(TileType.WALL,TileType.WAX_WALL,TileType.STALAGMITE,TileType.CRACKED_WALL,TileType.DOOR_C)){
							Screen.WriteMapChar(t2.row,t2.col,t2.symbol,Color.RandomBreached);
							if(t.DistanceFrom(t2) > max_dist){
								max_dist = t.DistanceFrom(t2);
								Game.GLUpdate(); //todo: stalagmites - if I add them to caves, they should no longer always vanish. check for an event, maybe?
								Thread.Sleep(50);
							}
						}
					}
					List<Tile> area = new List<Tile>();
					foreach(Tile tile in t.TilesWithinDistance(5)){
						if(tile.Is(TileType.WALL,TileType.WAX_WALL,TileType.STALAGMITE,TileType.CRACKED_WALL,TileType.DOOR_C) && tile.p.BoundsCheck(M.tile,false)){
							TileType prev_type = tile.type;
							if(tile.Is(TileType.STALAGMITE)){
								tile.Toggle(null,TileType.FLOOR);
							}
							else{
								tile.Toggle(null,TileType.BREACHED_WALL);
								tile.toggles_into = prev_type;
								area.Add(tile);
							}
							foreach(Tile neighbor in tile.TilesWithinDistance(1)){
								neighbor.solid_rock = false;
							}
						}
					}
					if(area.Count > 0){
						Q.Add(new Event(t,area,500,EventType.BREACH));
					}
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.SHIELDING:
			{
				ItemUseResult orb_result = UseOrb(1,true,user,line,(t,LOE_tile,results)=>{
					List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					List<colorchar> symbols = new List<colorchar>();
					foreach(Tile tile in t.TilesWithinDistance(1)){
						if(tile.passable && LOE_tile.HasLOE(tile)){
							colorchar cch = tile.visual;
							if(tile.actor() != null){
								if(!tile.actor().HasAttr(AttrType.SHIELDED)){
									tile.actor().attrs[AttrType.SHIELDED] = 1;
									B.Add(tile.actor().YouAre() + " shielded. ",tile.actor());
								}
								if(player.CanSee(tile.actor())){
									cch = tile.actor().visual;
								}
							}
							cch.bgcolor = Color.Blue;
							if(Global.LINUX && !Screen.GLMode){
								cch.bgcolor = Color.DarkBlue;
							}
							if(cch.color == cch.bgcolor){
								cch.color = Color.Black;
							}
							if(cch.c == '.'){
								cch.c = '+';
							}
							symbols.Add(cch);
							cells.Add(tile.p);
							area.Add(tile);
						}
					}
					Screen.AnimateMapCells(cells,symbols,150);
					foreach(Tile tile in area){
						if(player.CanSee(tile)){
							B.Add("A zone of protection is created. ");
							break;
						}
					}
					Q.Add(new Event(area,100,EventType.SHIELDING,R.Roll(2,6)+6));
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.TELEPORTAL:
			{
				ItemUseResult orb_result = UseOrb(0,false,user,line,(t,LOE_tile,results)=>{
					LOE_tile.AddFeature(FeatureType.TELEPORTAL);
					if(LOE_tile.Is(FeatureType.TELEPORTAL)){
						Q.Add(new Event(LOE_tile,0,EventType.TELEPORTAL,100));
					}
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.PAIN:
			{
				ItemUseResult orb_result = UseOrb(5,false,user,line,(t,LOE_tile,results)=>{
					List<pos> cells = new List<pos>();
					List<colorchar> symbols = new List<colorchar>();
					foreach(Tile tile in t.TilesWithinDistance(5)){
						if(LOE_tile.HasLOE(tile)){
							Actor a = tile.actor();
							if(a != null){
								if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(2,6),user,"an orb of pain")){
									a.ApplyStatus(AttrType.VULNERABLE,(R.Roll(2,6)+6)*100);
									if(a == player){
										Help.TutorialTip(TutorialTopic.Vulnerable);
									}
								}
							}
							symbols.Add(new colorchar('*',Color.RandomDoom));
							/*if(tile.DistanceFrom(t) % 2 == 0){
								symbols.Add(new colorchar('*',Color.DarkMagenta));
							}
							else{
								symbols.Add(new colorchar('*',Color.DarkRed));
							}*/
							cells.Add(tile.p);
						}
					}
					player.AnimateVisibleMapCells(cells,symbols,80);
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.CONFUSION:
			{
				ItemUseResult orb_result = UseOrb(2,false,user,line,(t,LOE_tile,results)=>{
					List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					colorchar cch = new colorchar('*',Color.RandomConfusion);
					for(int i=0;i<=2;++i){
						foreach(Tile tile in t.TilesAtDistance(i)){
							if(tile.passable && LOE_tile.HasLOE(tile)){
								tile.AddFeature(FeatureType.CONFUSION_GAS);
								area.Add(tile);
								cells.Add(tile.p);
								if(tile.seen){
									M.last_seen[tile.row,tile.col] = cch;
								}
							}
						}
						Screen.AnimateMapCells(cells,cch,40);
					}
					Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
					Event.RemoveGas(area,R.Between(7,9)*100,FeatureType.CONFUSION_GAS,20);
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.BLADES:
			{
				ItemUseResult orb_result = UseOrb(1,false,user,line,(t,LOE_tile,results)=>{
					List<Tile> targets = new List<Tile>();
					foreach(Tile t2 in t.TilesWithinDistance(1)){
						if(t2.passable && t2.actor() == null && LOE_tile.HasLOE(t2)){
							targets.Add(t2);
						}
					}
					targets.Randomize();
					foreach(Tile t2 in targets){
						Actor a = Actor.Create(ActorType.BLADE,t2.row,t2.col);
						if(a != null){
							a.speed = 50;
							a.attrs[AttrType.LIFESPAN] = 20;
						}
					}
				});
				used = orb_result.used;
				IDed = orb_result.IDed;
				break;
			}
			case ConsumableType.DUST_STORM:
			{
				ItemUseResult wand_result = UseWand(true,false,user,line,(LOE_tile,targeting,results)=>{
					List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					foreach(Tile neighbor in LOE_tile.TilesWithinDistance(1)){
						if(neighbor.passable){
							area.Add(neighbor);
						}
					}
					List<Tile> added = new List<Tile>();
					foreach(Tile n1 in area){
						foreach(int dir in U.FourDirections){
							if(R.CoinFlip() && n1.TileInDirection(dir).passable){
								added.Add(n1.TileInDirection(dir));
							}
						}
					}
					foreach(Tile n1 in added){
						area.AddUnique(n1);
					}
					colorchar cch = new colorchar('*',Color.TerrainDarkGray);
					foreach(Tile t2 in area){
						t2.AddFeature(FeatureType.THICK_DUST);
						cells.Add(t2.p);
						if(t2.seen){
							M.last_seen[t2.row,t2.col] = cch;
						}
						Actor a = t2.actor();
						if(a != null && t2.Is(FeatureType.THICK_DUST)){
							if(!a.HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE,AttrType.BLINDSIGHT)){
								if(a == player){
									B.Add("Thick dust fills the air! ");
								}
								a.ApplyStatus(AttrType.BLIND,R.Between(1,3)*100);
							}
						}
					}
					Screen.AnimateMapCells(cells,cch,80);
					Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
					Event.RemoveGas(area,R.Between(20,25)*100,FeatureType.THICK_DUST,8);
				});
				used = wand_result.used;
				IDed = wand_result.IDed;
				break;
			}
			case ConsumableType.FLESH_TO_FIRE:
			{
				ItemUseResult wand_result = UseWand(true,false,user,line,(LOE_tile,targeting,results)=>{
					Actor a = targeting.targeted.actor();
					if(a != null){
						B.Add("Jets of flame erupt from " + a.TheName(true) + ". ",a,targeting.targeted);
						Screen.AnimateMapCell(a.row,a.col,new colorchar('&',Color.RandomFire));
						int dmg = (a.curhp+1)/2;
						if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,dmg,user,"a wand of flesh to fire")){
							a.ApplyBurning();
						}
					}
					else{
						if(targeting.targeted.Is(FeatureType.TROLL_CORPSE)){
							B.Add("Jets of flame erupt from the troll corpse. ",a,targeting.targeted);
							targeting.targeted.ApplyEffect(DamageType.FIRE);
							if(targeting.targeted.Is(FeatureType.TROLL_CORPSE)){ //if it's still there because of thick gas, it still gets destroyed.
								targeting.targeted.RemoveFeature(FeatureType.TROLL_CORPSE);
								B.Add("The troll corpse burns to ashes! ",targeting.targeted);
							}
						}
						else{
							if(targeting.targeted.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
								B.Add("Jets of flame erupt from the troll bloodwitch corpse. ",a,targeting.targeted);
								targeting.targeted.ApplyEffect(DamageType.FIRE);
								if(targeting.targeted.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){ //if it's still there because of thick gas, it still gets destroyed.
									targeting.targeted.RemoveFeature(FeatureType.TROLL_BLOODWITCH_CORPSE);
									B.Add("The troll bloodwitch corpse burns to ashes! ",targeting.targeted);
								}
							}
							else{
								B.Add("Nothing happens. ",user);
								results.IDed = false;
							}
						}
					}
				});
				used = wand_result.used;
				IDed = wand_result.IDed;
				break;
			}
			case ConsumableType.INVISIBILITY:
			{
				ItemUseResult wand_result = UseWand(false,false,user,line,(LOE_tile,targeting,results)=>{
					Actor a = targeting.targeted.actor();
					if(a != null){
						B.Add(a.You("vanish",true) + " from view. ",a);
						if(a.light_radius > 0 && !M.wiz_dark && !M.wiz_lite){
							B.Add(a.Your() + " light still reveals " + a.Your() + " location. ",a);
						}
						a.RefreshDuration(AttrType.INVISIBLE,(R.Between(2,20)+30)*100,a.YouAre() + " no longer invisible. ",a);
					}
					else{
						B.Add("Nothing happens. ",user);
						results.IDed = false;
					}
				});
				used = wand_result.used;
				IDed = wand_result.IDed;
				break;
			}
			case ConsumableType.REACH:
			{
				ItemUseResult wand_result = UseWand(true,false,user,line,(LOE_tile,targeting,results)=>{
					Actor a = targeting.targeted.actor();
					if(a != null && a != user){
						user.Attack(0,a,true);
					}
					else{
						B.Add("Nothing happens. ",user);
						results.IDed = false;
					}
				});
				used = wand_result.used;
				IDed = wand_result.IDed;
				break;
			}
			case ConsumableType.SLUMBER:
			{
				ItemUseResult wand_result = UseWand(true,false,user,line,(LOE_tile,targeting,results)=>{
					Actor a = targeting.targeted.actor();
					if(a != null){
						if(a.HasAttr(AttrType.MENTAL_IMMUNITY)){
							if(a.HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
								B.Add(a.You("resist") + " becoming dormant. ",a);
							}
							else{
								B.Add(a.You("resist") + " falling asleep. ",a);
							}
						}
						else{
							if(a.ResistedBySpirit()){
								if(player.HasLOS(a)){
									if(a.HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
										B.Add(a.You("resist") + " becoming dormant. ",a);
									}
									else{
										B.Add(a.You("almost fall") + " asleep. ",a);
									}
								}
							}
							else{
								if(player.HasLOS(a)){
									if(a.HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
										B.Add(a.You("become") + " dormant. ",a);
									}
									else{
										B.Add(a.You("fall") + " asleep. ",a);
									}
								}
								a.attrs[AttrType.ASLEEP] = 6 + R.Roll(4,6);
							}
						}
					}
					else{
						B.Add("Nothing happens. ",user);
						results.IDed = false;
					}
				});
				used = wand_result.used;
				IDed = wand_result.IDed;
				break;
			}
			case ConsumableType.TELEKINESIS:
			{
				ItemUseResult wand_result = UseWand(true,false,user,line,(LOE_tile,targeting,results)=>{
					if(!SharedEffect.Telekinesis(false,user,targeting.targeted)){
						results.used = false;
					}
				});
				used = wand_result.used;
				IDed = wand_result.IDed;
				break;
			}
			case ConsumableType.WEBS:
			{
				ItemUseResult wand_result = UseWand(true,true,user,line,(LOE_tile,targeting,results)=>{
					if(targeting.targeted == user.tile()){
						B.Add("Nothing happens. ",user);
						results.IDed = false;
					}
					else{
						Screen.CursorVisible = false;
						foreach(Tile t in targeting.line_to_targeted){
							if(t.passable && t != user.tile()){
								t.AddFeature(FeatureType.WEB);
								if(t.seen){
									Screen.WriteMapChar(t.row,t.col,';',Color.White);
									Game.GLUpdate();
									Thread.Sleep(15);
								}
							}
						}
						M.Draw();
					}
				});
				used = wand_result.used;
				IDed = wand_result.IDed;
				break;
			}
			case ConsumableType.BLAST_FUNGUS:
			{
				if(line == null){
					line = user.GetTargetTile(12,0,false,true);
				}
				if(line != null){
					revealed_by_light = true;
					ignored = true;
					Tile t = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " " + TheName() + ". ");
					if(first != null && first != user){
						t = first.tile();
						B.Add("It hits " + first.the_name + ". ",first);
					}
					line = line.ToFirstSolidTileOrActor();
					if(line.Count > 0){
						line.RemoveAt(line.Count - 1);
					}
					int idx = 0;
					foreach(Tile tile2 in line){
						if(tile2.seen){
							++idx;
						}
						else{
							line = line.To(tile2);
							if(line.Count > 0){
								line.RemoveAt(line.Count - 1);
							}
							break;
						}
					}
					if(line.Count > 0){
						user.AnimateProjectile(line,symbol,color);
					}
					t.GetItem(this);
					//inv.Remove(i);
					t.MakeNoise(2);
					if(first != null && first != user){
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						if(t.IsTrap()){
							t.TriggerTrap();
						}
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.BANDAGES:
				if(!user.HasAttr(AttrType.BANDAGED)){
					user.attrs[AttrType.BANDAGED] = 20;
					//user.recover_time = Q.turn + 100;
					B.Add(user.You("apply",false,true) + " a bandage. ",user);
				}
				else{
					B.Add(user.the_name + " can't apply another bandage yet. ",user);
					used = false;
				}
				break;
			case ConsumableType.FLINT_AND_STEEL:
			{
				int dir = -1;
				if(user == player){
					dir = user.GetDirection("Which direction? ",false,true);
				}
				else{
					dir = user.DirectionOf(player);
				}
				if(dir != -1){
					Tile t = user.TileInDirection(dir);
					B.Add(user.You("use") + " your flint & steel. ",user);
					if(t.actor() != null && t.actor().HasAttr(AttrType.OIL_COVERED) && !t.Is(FeatureType.POISON_GAS,FeatureType.THICK_DUST)){
						t.actor().ApplyBurning();
					}
					if(!t.Is(TileType.WAX_WALL)){
						t.ApplyEffect(DamageType.FIRE);
					}
				}
				else{
					used = false;
				}
				break;
			}
			default:
				used = false;
				break;
			}
			if(used){
				if(IDed){
					bool seen = true; //i'll try letting orbs always be IDed. keep an eye on this.
					/*bool seen = (user == player);
					if(user != player){
						if(player.CanSee(line[0])){ //fix this line - or at least check for null/empty
							seen = true;
						}
						if(user != null && player.CanSee(user)){ //heck, I could even check to see whose turn it is, if I really wanted to be hacky.
							seen = true;
						}
					}*/
					if(!identified[type] && seen){
						identified[type] = true;
						B.Add("(It was " + SingularName(true) + "!) ");
					}
				}
				else{
					if(!unIDed_name[type].Contains("{tried}")){
						unIDed_name[type] = unIDed_name[type] + " {tried}";
					}
				}
				if(quantity > 1){
					--quantity;
				}
				else{
					if(type == ConsumableType.BANDAGES){
						--other_data;
						if(user != null && other_data == 0){
							B.Add(user.You("use") + " your last bandage. ",user);
							user.inv.Remove(this);
						}
					}
					else{
						if(type == ConsumableType.FLINT_AND_STEEL){
							if(R.OneIn(3)){
								--other_data;
								if(user != null){
									if(other_data == 2){
										B.Add("Your flint & steel shows signs of wear. ",user);
									}
									if(other_data == 1){
										B.Add("Your flint & steel is almost depleted. ",user);
									}
									if(other_data == 0){
										B.Add("Your flint & steel is used up. ",user);
										user.inv.Remove(this);
									}
								}
							}
						}
						else{
							if(NameOfItemType() == "wand"){
								if(charges > 0){
									--charges;
									if(other_data >= 0){
										++other_data;
									}
								}
								else{
									other_data = -1;
								}
							}
							else{
								if(user != null){
									user.inv.Remove(this);
								}
							}
						}
					}
				}
				CheckForMimic();
			}
			return used;
		}
		private class ItemUseResult{
			public bool used;
			public bool IDed;
			public ItemUseResult(bool used_,bool IDed_){
				used = used_;
				IDed = IDed_;
			}
		}
		private delegate void OrbTargetingDelegate(Tile t,Tile LOE_tile,ItemUseResult result);
		private ItemUseResult UseOrb(int radius,bool never_target_enemies,Actor user,List<Tile> line,OrbTargetingDelegate orb_effect){
			ItemUseResult result = new ItemUseResult(true,true);
			if(line == null){
				if(!identified[type]){
					radius = 0;
				}
				line = user.GetTargetTile(12,radius,false,!(never_target_enemies && identified[type]));
			}
			if(line != null){
				Tile t = line.LastOrDefault();
				Tile prev = line.LastBeforeSolidTile();
				Actor first = null;
				bool trigger_trap = true;
				Screen.CursorVisible = false;
				if(user != null){
					first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						if(player.CanSee(user)){
							B.Add("It shatters on " + first.the_name + "! ",first);
						}
						else{
							B.Add("Something shatters on " + first.the_name + "! ",first);
						}
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						if(player.CanSee(user)){
							B.Add("It shatters on " + t.the_name + "! ",t);
						}
						else{
							B.Add("Something shatters on " + t.the_name + "! ",t);
						}
					}
					user.AnimateProjectile(line.ToFirstSolidTileOrActor(),'*',color);
					Screen.CursorVisible = false;
				}
				else{
					trigger_trap = false;
				}
				Tile LOE_tile = t;
				if(!t.passable && prev != null){
					LOE_tile = prev;
				}
				orb_effect(t,LOE_tile,result);
				t.MakeNoise(2);
				if(trigger_trap && t.IsTrap()){
					t.TriggerTrap();
				}
				if(!revealed_by_light){
					result.IDed = false;
				}
			}
			else{
				result.used = false;
			}
			return result;
		}
		private delegate void WandTargetingDelegate(Tile LOE_tile,TargetInfo targeting,ItemUseResult result);
		private ItemUseResult UseWand(bool targets_enemies,bool visible_line,Actor user,List<Tile> line,WandTargetingDelegate wand_effect){
			ItemUseResult result = new ItemUseResult(true,true);
			if(charges == 0){
				B.Add(TheName(true) + " is empty. Nothing happens. ",user);
				other_data = -1;
				result.IDed = false;
				return result;
			}
			TargetInfo info = null;
			if(user != player && line != null){
				info = new TargetInfo();
				info.extended_line = line;
				info.targeted = player.tile();
			}
			if(!identified[type]){
				targets_enemies = true;
				visible_line = false;
			}
			if(info == null){
				info = user.GetTarget(false,12,0,!visible_line,false,targets_enemies,"");
			}
			if(info != null){
				if(user != null && !user.HasLOE(info.targeted)){
					foreach(Tile t in info.extended_line){
						if(!t.passable){
							info.targeted = t;
							break;
						}
					}
				}
				if(user == null || user.HasLOE(info.targeted)){
					Tile LOE_tile = null;
					if(user != null){
						LOE_tile = user.tile();
					}
					else{
						LOE_tile = info.extended_line[0];
					}
					Tile prev = null;
					foreach(Tile t in info.extended_line){
						if(!t.passable){
							if(prev != null){
								LOE_tile = prev;
							}
							break;
						}
						else{
							if(t == info.targeted){
								LOE_tile = t;
								break;
							}
						}
						prev = t;
					}
					wand_effect(LOE_tile,info,result);
				}
			}
			else{
				result.used = false;
			}
			return result;
		}
		public void CheckForMimic(){
			Event e = Q.FindTargetedEvent(this,EventType.MIMIC);
			if(e != null){
				e.dead = true;
				B.Add("You hear a pained screech. ");
			}
		}
		public string Description(){
			if(!revealed_by_light){
				return "You can't see what type of " + NameOfItemType(type) + " this is.";
			}
			else{
				if(!identified[type]){
					if(NameOfItemType(type) == "scroll"){
						return "Rolled paper with words of magic, activated by speaking them aloud. The words on this scroll are unfamiliar to you.";
					}
					else{
						if(NameOfItemType(type) == "potion"){
							return "A glass bottle filled with mysterious liquid.";
						}
						else{
							if(NameOfItemType(type) == "orb"){
								return "Shifting lights dance inside this orb. Breaking it will release the unknown magic contained within.";
							}
							else{
								if(NameOfItemType(type) == "wand"){
									return "This thin wand holds an undiscovered arcane power.";
								}
							}
						}
					}
				}
				switch(type){
				case ConsumableType.BANDAGES:
					return "A bandage's effect will last for 20 turns, restoring 1 HP per turn and preventing bleed damage. However, taking any major damage will end its effect.";
					//return "Applying a bandage will slowly restore 10 HP.";
				case ConsumableType.BLAST_FUNGUS:
					return "This blast fungus is about to explode!";
				case ConsumableType.BLINKING:
					return "This scroll will teleport you a short distance randomly.";
				case ConsumableType.BREACHING:
					return "This orb will temporarily lower nearby walls, which will slowly return to their original state.";
				case ConsumableType.BRUTISH_STRENGTH:
					return "Drinking this potion grants the strength of a juggernaut. For a short time you'll be able to smash through various dungeon features. Additionally, your attacks will deal maximum damage and knock foes back 5 spaces, and you'll keep moving afterward if possible.";
					//return "Drinking this potion grants the strength of a juggernaut. For a short time you'll be able to smash through various dungeon features. Additionally, you'll still move after making an attack, which will deal maximum damage and knock foes back 5 spaces.";
				case ConsumableType.CALLING:
					return "This scroll's magic will find the nearest foe and transport it next to you. Immobile creatures are immune to this effect.";
				case ConsumableType.CLOAKING:
					return "This potion will cover you in shadows, causing you to fade to invisibility while in darkness.";
				case ConsumableType.DARKNESS:
					return "This scroll covers the dungeon in a blanket of darkness that suppresses all light.";
				case ConsumableType.MYSTIC_MIND:
					return "This potion will expand your mind, allowing you to sense foes no matter where they are, and granting immunity to stuns, sleep, rage, confusion, and fear.";
					//return "This scroll reveals the location of foes on the current dungeon level for a while.";
				case ConsumableType.DETONATION:
					return "On impact, this orb will explode violently, inflicting great damage on its surroundings.";
				case ConsumableType.ENCHANTMENT:
					return "This potent scroll will impart a permanent magical effect on the weapon you're holding.";
				case ConsumableType.FLAMES:
					return "Leaping flames will pour from this orb when it shatters.";
				case ConsumableType.FLINT_AND_STEEL:
					return "Used for creating sparks, enough to ignite flammable objects (but not enough to damage a foe).";
				case ConsumableType.FOG:
					return "Dense fog will expand from this orb when it breaks, blocking sight and reducing accuracy.";
				case ConsumableType.FREEZING:
					return "Breaking this orb will encase nearby entities in ice.";
				case ConsumableType.HEALING:
					return "This invaluable elixir will instantly restore you to full health.";
				case ConsumableType.KNOWLEDGE:
					return "Reading this scroll will grant knowledge of the items in your pack, and of the current level, including secret doors and traps.";
					//return "This scroll will show you the layout of the current level, including secret doors and traps.";
				case ConsumableType.PAIN:
					return "Anything caught in this orb's area of effect will become vulnerable to extra damage.";
				case ConsumableType.PASSAGE:
					return "This scroll will move you to the other side of an adjacent wall (but not diagonally).";
				case ConsumableType.REGENERATION:
					return "The potent healing magic in this potion will steadily grant you health for 100 turns.";
				case ConsumableType.RENEWAL:
					return "This scroll's magic will strip negative effects from your weapons & armor, charge any wands in your possession, and remove any slime or oil covering you.";
					//return "This scroll's power will strip negative effects from your weapons & armor, and will remove any slime or oil covering you.";
				case ConsumableType.ROOTS:
					return "Drinking this potion will cause thick roots to grow from you, holding you tightly to the ground and providing defense against attacks.";
				case ConsumableType.SHIELDING:
					return "This orb will create a zone of protection, shielding entities within for several turns.";
				case ConsumableType.SILENCE:
					return "This potion will cause you to radiate an aura of silence, preventing all sounds within 2 spaces. This can help you remain stealthy, but leaves you unable to speak words of magic.";
					//return "This potion will cause your actions to become entirely soundless. You'll attract less attention, but you'll be unable to speak words of magic.";
				case ConsumableType.STONEFORM:
					return "This potion will change you temporarily to unliving stone, granting a slight resistance to all damage. You'll no longer be able to catch fire, and no toxin, gas, or potion will affect you.";
				case ConsumableType.SUNLIGHT:
					return "This scroll fills the level with sunlight, illuminating every corner of the dungeon.";
				case ConsumableType.TELEPORTAL:
					return "Releasing this orb's energy will create an unstable rift. Creatures stepping into the rift will be transported elsewhere in the dungeon.";
				case ConsumableType.TIME:
					return "Reading this scroll will halt the flow of time for a single turn, allowing you to take an extra action.";
				case ConsumableType.TRAP_CLEARING:
					return "This scroll will cause all traps within 12 spaces to be triggered simultaneously.";
				case ConsumableType.VAMPIRISM:
					return "Consuming this potion will grant many of the powers of a true vampire. You'll fly and drain life from living enemies, but light will leave you vulnerable.";
				case ConsumableType.HASTE:
					return "Drinking this potion will double your movement speed and greatly enhance your reflexes. You'll sometimes dodge an attack by leaping to a nearby space. You're more likely to dodge in an open area, but be careful around dangerous terrain.";
				case ConsumableType.THUNDERCLAP:
					return "When this scroll is read, a cacophonous crash of thunder sweeps outward, shattering fragile objects like potions & cracked walls, and damaging & stunning foes.";
				case ConsumableType.FIRE_RING:
					return "This scroll will surround you with a ring of fire, just far enough to avoid its heat...if you stand still.";
				case ConsumableType.RAGE:
					return "When read, this scroll radiates a wave of fury over those nearby. Those affected will attack anything nearest to them, friend or foe.";
				case ConsumableType.BLADES:
					return "This orb's magic manifests in the form of several flying blades. These animated weapons fly quickly and attack anything nearby.";
				case ConsumableType.CONFUSION:
					return "Breaking this orb will release a befuddling gas, causing those affected to move and attack in random directions.";
				case ConsumableType.DUST_STORM:
					return "When zapped, this wand creates a billowing cloud of thick blinding dust, so heavy that it can extinguish flames.";
				case ConsumableType.INVISIBILITY:
					return "This wand will render its target invisible. Carrying a light source (or being on fire) will still reveal an invisible being's location.";
				case ConsumableType.FLESH_TO_FIRE:
					return "The target of this wand undergoes a grisly transfiguration as part of its substance turns to fire! It'll lose half of its current health and burst into flames.";
				case ConsumableType.WEBS:
					return "This wand will create a line of sticky webs, stretching between you and the targeted space.";
				case ConsumableType.SLUMBER:
					return "The target of this wand will fall asleep (or become dormant, in the case of nonliving creatures) for a while. Sleepers will awaken upon taking damage as usual.";
				case ConsumableType.REACH:
					return "By zapping this wand, you can make a melee attack at range (with your current weapon) against your chosen target.";
				case ConsumableType.TELEKINESIS:
					return "After zapping this wand at your target, you can throw it forcefully. You can throw items, monsters, terrain features, and even yourself.";
				}
			}
			return "Unknown item.";
		}
	}
	public class Weapon{
		public WeaponType type;
		public EnchantmentType enchantment;
		public Dict<EquipmentStatus,bool> status = new Dict<EquipmentStatus,bool>();
		public Weapon(WeaponType type_){
			type = type_;
			enchantment = EnchantmentType.NO_ENCHANTMENT;
		}
		public Weapon(WeaponType type_,EnchantmentType enchantment_){
			type = type_;
			enchantment = enchantment_;
		}
		public bool IsEdged(){
			if(type == WeaponType.SWORD || type == WeaponType.DAGGER){
				return true;
			}
			return false;
		} //note that the bow is neither
		public bool IsBlunt(){
			if(type == WeaponType.MACE || type == WeaponType.STAFF){
				return true;
			}
			return false;
		}
		public static bool IsEdged(WeaponType type){
			if(type == WeaponType.SWORD || type == WeaponType.DAGGER){
				return true;
			}
			return false;
		}
		public static bool IsBlunt(WeaponType type){
			if(type == WeaponType.MACE || type == WeaponType.STAFF){
				return true;
			}
			return false;
		}
		public AttackInfo Attack(){
			switch(type){
			case WeaponType.SWORD:
				return new AttackInfo(100,2,AttackEffect.PERCENT_DAMAGE,"& hit *");
			case WeaponType.MACE:
				return new AttackInfo(100,2,AttackEffect.KNOCKBACK,"& hit *");
			case WeaponType.DAGGER:
				return new AttackInfo(100,2,AttackEffect.STUN,"& hit *");
			case WeaponType.STAFF:
				return new AttackInfo(100,2,AttackEffect.TRIP,"& hit *");
			case WeaponType.BOW: //bow's melee damage
				return new AttackInfo(100,1,AttackEffect.NO_CRIT,"& hit *");
			default:
				return new AttackInfo(100,0,AttackEffect.NO_CRIT,"error");
			}
		}
		public override string ToString(){
			return NameWithEnchantment();
		}
		public string NameWithoutEnchantment(){
			switch(type){
			case WeaponType.SWORD:
				return "sword";
			case WeaponType.MACE:
				return "mace";
			case WeaponType.DAGGER:
				return "dagger";
			case WeaponType.STAFF:
				return "staff";
			case WeaponType.BOW:
				return "bow";
			default:
				return "no weapon";
			}
		}
		public string NameWithEnchantment(){
			string ench = "";
			switch(enchantment){
			case EnchantmentType.ECHOES:
				ench = " of echoes";
				break;
			case EnchantmentType.CHILLING:
				ench = " of chilling";
				break;
			case EnchantmentType.PRECISION:
				ench = " of precision";
				break;
			case EnchantmentType.DISRUPTION:
				ench = " of disruption";
				break;
			case EnchantmentType.VICTORY:
				ench = " of victory";
				break;
			default:
				break;
			}
			return NameWithoutEnchantment() + ench;
		}
		public cstr StatsName(){
			cstr cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case WeaponType.SWORD:
				cs.s = "Sword";
				break;
			case WeaponType.MACE:
				cs.s = "Mace";
				break;
			case WeaponType.DAGGER:
				cs.s = "Dagger";
				break;
			case WeaponType.STAFF:
				cs.s = "Staff";
				break;
			case WeaponType.BOW:
				cs.s = "Bow";
				break;
			default:
				cs.s = "no weapon";
				break;
			}
			if(enchantment != EnchantmentType.NO_ENCHANTMENT){
				cs.s = "+" + cs.s + "+";
			}
			cs.color = EnchantmentColor();
			return cs;
		}
		public Color EnchantmentColor(){
			switch(enchantment){
			case EnchantmentType.ECHOES:
				return Color.Magenta;
			case EnchantmentType.CHILLING:
				return Color.Blue;
			case EnchantmentType.PRECISION:
				return Color.White;
			case EnchantmentType.DISRUPTION:
				return Color.Yellow;
			case EnchantmentType.VICTORY:
				return Color.Red;
			default:
				return Color.Gray;
			}
		}
		public static Color StatusColor(EquipmentStatus status){
			switch(status){
			case EquipmentStatus.LOW_ON_ARROWS:
				return Color.DarkBlue;
			case EquipmentStatus.ALMOST_OUT_OF_ARROWS:
				return Color.Blue;
			case EquipmentStatus.ONE_ARROW_LEFT: //if I add more statuses, I might make these all the same color. don't want to hog ALL the blue.
				return Color.DarkCyan;
			case EquipmentStatus.OUT_OF_ARROWS:
				return Color.Cyan;
			case EquipmentStatus.POISONED: //weapon-only statuses
				return Color.Green;
			case EquipmentStatus.MERCIFUL:
				return Color.Yellow;
			case EquipmentStatus.POSSESSED:
				return Color.Red;
			case EquipmentStatus.DULLED:
				return Color.DarkGray;
			case EquipmentStatus.HEAVY: //statuses that might apply to both
				return Color.DarkYellow;
			case EquipmentStatus.STUCK:
				return Color.Magenta;
			case EquipmentStatus.NEGATED:
				return Color.White;
			case EquipmentStatus.WEAK_POINT: //armor-only statuses
				return Color.Blue;
			case EquipmentStatus.WORN_OUT:
				return Color.Yellow;
			case EquipmentStatus.DAMAGED:
				return Color.Red;
			case EquipmentStatus.INFESTED:
				return Color.DarkGreen;
			case EquipmentStatus.RUSTED:
				return Color.DarkRed;
			default:
				return Color.RandomDark;
			}
		}
		public static string StatusName(EquipmentStatus status){
			switch(status){
			case EquipmentStatus.HEAVY:
				return "Heavy";
			case EquipmentStatus.STUCK:
				return "Stuck";
			case EquipmentStatus.MERCIFUL:
				return "Merciful";
			case EquipmentStatus.DAMAGED:
				return "Damaged";
			case EquipmentStatus.DULLED:
				return "Dulled";
			case EquipmentStatus.INFESTED:
				return "Infested";
			case EquipmentStatus.NEGATED:
				return "Negated";
			case EquipmentStatus.POSSESSED:
				return "Possessed";
			case EquipmentStatus.RUSTED:
				return "Rusted";
			case EquipmentStatus.WEAK_POINT:
				return "Weak point";
			case EquipmentStatus.WORN_OUT:
				return "Worn out";
			case EquipmentStatus.POISONED:
				return "Poisoned";
			case EquipmentStatus.LOW_ON_ARROWS:
				return "Low on arrows";
			case EquipmentStatus.ALMOST_OUT_OF_ARROWS:
				return "Almost out of arrows";
			case EquipmentStatus.ONE_ARROW_LEFT:
				return "One arrow left";
			case EquipmentStatus.OUT_OF_ARROWS:
				return "Out of arrows";
			default:
				return "No status";
			}
		}
		public colorstring EquipmentScreenName(){
			colorstring result = new colorstring(StatsName());
			result.strings[0] = new cstr(NameWithEnchantment().Substring(0,1).ToUpper() + NameWithEnchantment().Substring(1) + " ",result.strings[0].color);
			for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
				if(status[(EquipmentStatus)i]){
					result.strings.Add(new cstr("*",StatusColor((EquipmentStatus)i)));
					if(result.Length() >= 25){
						break;
					}
				}
			}
			return result;
		}
		public string[] Description(){
			switch(type){
			case WeaponType.SWORD:
				return new string[]{"Sword -- A basic weapon, the sword delivers powerful",
									"     critical hits that remove half of a foe's maximum health."};
			case WeaponType.MACE:
				return new string[]{"Mace -- The mace won't be stopped by armor. Critical",
									"           hits will knock the foe back three spaces."};
			case WeaponType.DAGGER:
				return new string[]{"Dagger -- In darkness, the dagger always hits and is",
									"     twice as likely to score a critical hit, stunning the foe."};
			case WeaponType.STAFF:
				return new string[]{"Staff -- Always hits against a foe that just moved, in",
									"  addition to swapping places. Critical hits will trip the foe."};
			case WeaponType.BOW:
				return new string[]{"Bow -- A ranged weapon, less accurate than melee.",
									"        Critical hits will immobilize the target briefly."};
			default:
				return new string[]{"no weapon","description"};
			}
		}
		public string DescriptionOfEnchantment(){
			switch(enchantment){
			case EnchantmentType.ECHOES:
				return "  Successful hits will also attack anything behind the target.";
			case EnchantmentType.CHILLING:
				return "   Deals 1 extra cold damage. This damage doubles on each hit.";
			case EnchantmentType.PRECISION:
				return "     This weapon is twice as likely to score critical hits.";
			case EnchantmentType.DISRUPTION:
				return "Nonliving foes will lose 20% of their maximum health on each hit.";
			case EnchantmentType.VICTORY:
				return "     Defeating an enemy with this weapon will restore 5 HP.";
			}
			return "";
		}
		public static string StatusDescription(EquipmentStatus status){
			switch(status){
			case EquipmentStatus.POISONED: //weapon-only statuses
				return "    Poisoned -- Poisons the target, and might poison you too.";
			case EquipmentStatus.MERCIFUL:
				return " Merciful -- Unable to take the last bit of health from an enemy.";
			case EquipmentStatus.POSSESSED:
				return " Possessed -- This weapon will sometimes attack the wrong target.";
			case EquipmentStatus.DULLED:
				return "     Dulled -- This weapon deals minimum damage on each hit.";
			case EquipmentStatus.LOW_ON_ARROWS:
				return "        Low on arrows -- Your quiver feels a bit light.";
			case EquipmentStatus.ALMOST_OUT_OF_ARROWS:
				return "     Almost out of arrows -- Your quiver is almost empty.";
			case EquipmentStatus.ONE_ARROW_LEFT:
				return " One arrow left -- This final arrow will have perfect accuracy.";
			case EquipmentStatus.OUT_OF_ARROWS:
				return " Out of arrows -- Repair your equipment to restock your arrows.";
			case EquipmentStatus.HEAVY: //statuses that might apply to both
				return "   Heavy -- Attacking with this weapon often causes exhaustion.";
			case EquipmentStatus.STUCK:
				return "             Stuck -- This item can't be unequipped.";
			case EquipmentStatus.NEGATED:
				return "   Negated -- The enchantment on this item has been suppressed.";
			case EquipmentStatus.WEAK_POINT: //armor-only statuses
				return "Weak point -- Enemies are twice as likely to score critical hits.";
			case EquipmentStatus.WORN_OUT:
				return "   Worn out -- Any further wear on this armor might damage it.";
			case EquipmentStatus.DAMAGED:
				return "          Damaged -- This armor provides no protection.";
			case EquipmentStatus.INFESTED:
				return "          Infested -- Insects constantly bite the wearer.";
			case EquipmentStatus.RUSTED:
				return "                          Rusted"; //not implemented
			default:
				return "No status";
			}
		}
	}
	public class Armor{
		public ArmorType type;
		public EnchantmentType enchantment;
		public Dict<EquipmentStatus,bool> status = new Dict<EquipmentStatus,bool>();
		public Armor(ArmorType type_){
			type = type_;
			enchantment = EnchantmentType.NO_ENCHANTMENT;
		}
		public Armor(ArmorType type_,EnchantmentType enchantment_){
			type = type_;
			enchantment = enchantment_;
		}
		public int Protection(){
			if(status[EquipmentStatus.DAMAGED]){
				return 0;
			}
			switch(type){
			case ArmorType.LEATHER:
				return 2;
			case ArmorType.CHAINMAIL:
				return 6;
			case ArmorType.FULL_PLATE:
				return 10;
			default:
				return 0;
			}
		}
		public int StealthPenalty(){
			switch(type){
			case ArmorType.CHAINMAIL:
				return 1;
			case ArmorType.FULL_PLATE:
				return 3;
			default:
				return 0;
			}
		}
		public override string ToString(){
			return NameWithEnchantment();
		}
		public string NameWithoutEnchantment(){
			switch(type){
			case ArmorType.LEATHER:
				return "leather";
			case ArmorType.CHAINMAIL:
				return "chainmail";
			case ArmorType.FULL_PLATE:
				return "full plate";
			default:
				return "no armor";
			}
		}
		public string NameWithEnchantment(){
			string ench = "";
			/*switch(enchantment){
			case EnchantmentType.ECHOES:
				ench = " of echoes";
				break;
			case EnchantmentType.FIRE:
				ench = " of fire";
				break;
			case EnchantmentType.FORCE:
				ench = " of force";
				break;
			case EnchantmentType.NULLIFICATION:
				ench = " of nullification";
				break;
			case EnchantmentType.ICE:
				ench = " of ice";
				break;
			default:
				break;
			}*/
			return NameWithoutEnchantment() + ench;
		}
		public cstr StatsName(){
			cstr cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case ArmorType.LEATHER:
				cs.s = "Leather";
				break;
			case ArmorType.CHAINMAIL:
				cs.s = "Chainmail";
				break;
			case ArmorType.FULL_PLATE:
				cs.s = "Full plate";
				break;
			default:
				cs.s = "no armor";
				break;
			}
			if(enchantment != EnchantmentType.NO_ENCHANTMENT){
				cs.s = "+" + cs.s + "+";
			}
			cs.color = EnchantmentColor();
			return cs;
		}
		public Color EnchantmentColor(){
			return Color.Gray;
		}
		public colorstring EquipmentScreenName(){
			colorstring result = new colorstring(StatsName());
			result.strings[0] = new cstr(result.strings[0].s + " ",result.strings[0].color);
			for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
				if(status[(EquipmentStatus)i]){
					result.strings.Add(new cstr("*",Weapon.StatusColor((EquipmentStatus)i)));
				}
			}
			return result;
		}
		public string[] Description(){
			switch(type){
			case ArmorType.LEATHER:
				return new string[]{"Leather -- +2 Defense. Leather armor is light and quiet",
									"         but provides only basic protection against attacks."};
			case ArmorType.CHAINMAIL:
				return new string[]{"Chainmail -- +6 Defense, -1 Stealth. Chainmail provides",
									"            good protection but hampers stealth slightly."};
			case ArmorType.FULL_PLATE:
				return new string[]{"Full plate -- +10 Defense, -3 Stealth. Plate is noisy,",
									" shiny, & tiring, providing great defense at the cost of stealth."};
				/*return new string[]{"Full plate -- +10 Defense, -3 Stealth. Plate armor is",
									" noisy and shiny, providing great defense at the cost of stealth."};*/
			default:
				return new string[]{"no armor",""};
			}
		}
		public string DescriptionOfEnchantment(){
			return "";
		}
	}
	public static class MagicTrinket{
		public static string Name(MagicTrinketType type){
			switch(type){
			case MagicTrinketType.PENDANT_OF_LIFE:
				return "pendant of life";
			case MagicTrinketType.CLOAK_OF_SAFETY:
				return "cloak of safety";
			case MagicTrinketType.BELT_OF_WARDING:
				return "belt of warding";
			case MagicTrinketType.BRACERS_OF_ARROW_DEFLECTION:
				return "bracers of arrow deflection";
			case MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE:
				return "circlet of the third eye";
			case MagicTrinketType.LENS_OF_SCRYING:
				return "lens of scrying";
			case MagicTrinketType.RING_OF_KEEN_SIGHT:
				return "ring of keen sight";
			case MagicTrinketType.RING_OF_THE_LETHARGIC_FLAME:
				return "ring of the lethargic flame";
			case MagicTrinketType.BOOTS_OF_GRIPPING:
				return "boots of gripping";
			default:
				return "no item";
			}
		}
		public static string[] Description(MagicTrinketType type){
			switch(type){
			case MagicTrinketType.PENDANT_OF_LIFE:
				return new string[]{"Pendant of life -- Prevents a lethal attack from","finishing you, but often vanishes afterward."};
				//return new string[]{"Pendant of life -- Prevents a lethal attack from","finishing you, but crumbles after a few uses."};
			case MagicTrinketType.CLOAK_OF_SAFETY:
				return new string[]{"Cloak of safety -- Lets you escape to safety","if your health falls too low. Works only once."};
			case MagicTrinketType.BELT_OF_WARDING:
				return new string[]{"Belt of warding -- If you would take more than 15","damage at once, this item reduces the amount to 15."};
			case MagicTrinketType.BRACERS_OF_ARROW_DEFLECTION:
				return new string[]{"Bracers of arrow deflection -- Blocks every arrow","fired at you."};
			case MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE:
				return new string[]{"Circlet of the third eye -- Grants a vision of","your surroundings when you rest."};
			case MagicTrinketType.LENS_OF_SCRYING:
				return new string[]{"Lens of scrying -- Identifies a random unknown item","from your pack when you descend to a new depth."};
			case MagicTrinketType.RING_OF_KEEN_SIGHT:
				return new string[]{"Ring of keen sight -- Doubles your chance to","find traps."};
			case MagicTrinketType.RING_OF_THE_LETHARGIC_FLAME:
				return new string[]{"Ring of the lethargic flame -- While you're on","fire, you'll only burn for 1 damage each turn."};
			case MagicTrinketType.BOOTS_OF_GRIPPING:
				return new string[]{"Boots of gripping -- Lets you walk across slippery","surfaces without losing traction."};
			default:
				return new string[]{"no item","here"};
			}
		}
	}
}

