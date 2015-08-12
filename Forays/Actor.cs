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
using Utilities;
namespace Forays{
	public class AttackInfo{
		public int cost;
		public Damage damage;
		public CriticalEffect crit;
		public string hit;
		public string miss;
		public string blocked;
		public AttackInfo(int cost_,int dice_,CriticalEffect crit_,string hit_){
			cost = cost_;
			damage.dice = dice_;
			damage.type = DamageType.NORMAL;
			damage.damclass = DamageClass.PHYSICAL;
			crit = crit_;
			hit = hit_;
			miss = "";
			blocked = "";
		}
		public AttackInfo(int cost_,int dice_,CriticalEffect crit_,string hit_,string miss_,string blocked_){
			cost = cost_;
			damage.dice = dice_;
			damage.type = DamageType.NORMAL;
			damage.damclass = DamageClass.PHYSICAL;
			crit = crit_;
			hit = hit_;
			miss = miss_;
			blocked = blocked_;
		}
		public AttackInfo(AttackInfo a){
			cost = a.cost;
			damage = a.damage;
			crit = a.crit;
			hit = a.hit;
			miss = a.miss;
			blocked = a.blocked;
		}
	}
	public enum TiebreakerAssignment{AtEnd,InsertAfterCurrent,UseCurrent};
	public struct Damage{
		public int amount{ //amount isn't determined until you ask for it
			get{
				if(!num.HasValue){
					num = R.Roll(dice,6);
				}
				return num.Value;
			}
			set{
				num = value;
			}
		}
		private int? num;
		public int dice;
		public DamageType type;
		public DamageClass damclass;
		public bool major_damage;
		public Actor source;
		public WeaponType weapon_used;
		public Damage(int dice_,DamageType type_,DamageClass damclass_,bool major,Actor source_){
			dice=dice_;
			num = null;
			type=type_;
			damclass=damclass_;
			major_damage = major;
			source=source_;
			weapon_used = WeaponType.NO_WEAPON;
		}
		public Damage(DamageType type_,DamageClass damclass_,bool major,Actor source_,int totaldamage){
			dice=0;
			num=totaldamage;
			type=type_;
			damclass=damclass_;
			major_damage = major;
			source=source_;
			weapon_used = WeaponType.NO_WEAPON;
		}
	}
	public class Actor : PhysicalObject{
		public ActorType type;
		public int maxhp;
		public int curhp;
		public int maxmp;
		public int curmp;
		public int speed;
		public Actor target;
		public List<Item> inv;
		public Dict<AttrType,int> attrs = new Dict<AttrType,int>();
		public Dict<SkillType,int> skills = new Dict<SkillType,int>();
		public Dict<FeatType,bool> feats = new Dict<FeatType,bool>();
		public Dict<SpellType,bool> spells = new Dict<SpellType,bool>();
		public int exhaustion;
		public int time_of_last_action;
		public int recover_time;
		public List<pos> path = new List<pos>();
		public Tile target_location;
		public int player_visibility_duration;
		public List<Actor> group = null;
		public LinkedList<Weapon> weapons = new LinkedList<Weapon>();
		public LinkedList<Armor> armors = new LinkedList<Armor>();
		public List<MagicTrinketType> magic_trinkets = new List<MagicTrinketType>(); //todo: init to null?

		public static string player_name;
		public static List<FeatType> feats_in_order = null;
		public static List<SpellType> spells_in_order = null; //used only for keeping track of the order in which feats/spells were learned by the player
		public static List<pos> footsteps = new List<pos>();
		public static List<pos> previous_footsteps = new List<pos>();
		public static List<Actor> tiebreakers = null; //a list of all actors on this level. used to determine sub-turn order of events
		public static Dict<ActorType,List<AttackInfo>> attack = new Dict<ActorType,List<AttackInfo>>();
		public static pos interrupted_path = new pos(-1,-1);
		public static bool viewing_more_commands = false; //used in DisplayStats to show extra commands
		public static bool viewing_map_shrine_info = false; //used in DisplayStats to show map info
		
		private static Dict<ActorType,Actor> proto = new Dict<ActorType, Actor>();
		public static Actor Prototype(ActorType type){ return proto[type]; }
		static Actor(){
			Define(ActorType.SPECIAL,"rat",'r',Color.DarkGray,10,90,0,AttrType.LOW_LIGHT_VISION,AttrType.SMALL,AttrType.KEEN_SENSES,AttrType.TERRITORIAL);
			DefineAttack(ActorType.SPECIAL,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.GOBLIN,"goblin",'g',Color.Green,15,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION,AttrType.AGGRESSIVE);
			DefineAttack(ActorType.GOBLIN,100,2,CriticalEffect.STUN,"& hits *");

			Define(ActorType.GIANT_BAT,"giant bat",'b',Color.DarkGray,10,50,0,AttrType.FLYING,AttrType.SMALL,AttrType.KEEN_SENSES,AttrType.BLINDSIGHT,AttrType.TERRITORIAL);
			DefineAttack(ActorType.GIANT_BAT,100,1,CriticalEffect.NO_CRIT,"& bites *");
			DefineAttack(ActorType.GIANT_BAT,100,1,CriticalEffect.NO_CRIT,"& scratches *");

			Define(ActorType.LONE_WOLF,"lone wolf",'c',Color.DarkYellow,15,50,0,AttrType.LOW_LIGHT_VISION,AttrType.KEEN_SENSES,AttrType.TERRITORIAL);
			DefineAttack(ActorType.LONE_WOLF,100,2,CriticalEffect.TRIP,"& bites *");

			Define(ActorType.BLOOD_MOTH,"blood moth",'i',Color.Red,15,100,0,AttrType.FLYING,AttrType.SMALL,AttrType.MINDLESS);
			DefineAttack(ActorType.BLOOD_MOTH,100,3,CriticalEffect.DRAIN_LIFE,"& bites *");

			Define(ActorType.DARKNESS_DWELLER,"darkness dweller",'h',Color.DarkGreen,25,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION,AttrType.AVOIDS_LIGHT);
			DefineAttack(ActorType.DARKNESS_DWELLER,100,2,CriticalEffect.STUN,"& hits *");

			Define(ActorType.CARNIVOROUS_BRAMBLE,"carnivorous bramble",'B',Color.DarkYellow,20,100,0,AttrType.PLANTLIKE,AttrType.IMMOBILE,AttrType.BLINDSIGHT,AttrType.MINDLESS);
			DefineAttack(ActorType.CARNIVOROUS_BRAMBLE,100,6,CriticalEffect.MAX_DAMAGE,"& rakes *");

			Define(ActorType.FROSTLING,"frostling",'E',Color.Gray,20,100,0,AttrType.IMMUNE_COLD);
			DefineAttack(ActorType.FROSTLING,100,2,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.SWORDSMAN,"swordsman",'p',Color.White,25,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.SWORDSMAN,100,2,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.SWORDSMAN).skills[SkillType.DEFENSE] = 2;
			Prototype(ActorType.SWORDSMAN).skills[SkillType.SPIRIT] = 2;

			Define(ActorType.DREAM_WARRIOR,"dream warrior",'p',Color.Cyan,20,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.DREAM_WARRIOR,100,2,CriticalEffect.CONFUSE,"& hits *");

			Define(ActorType.DREAM_WARRIOR_CLONE,"dream warrior",'p',Color.Cyan,1,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.NONLIVING,AttrType.NO_CORPSE_KNOCKBACK);
			DefineAttack(ActorType.DREAM_WARRIOR_CLONE,100,0,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.SPITTING_COBRA,"spitting cobra",'S',Color.Red,20,100,0,AttrType.SMALL,AttrType.KEEN_SENSES,AttrType.POISON_HIT,AttrType.TERRITORIAL);
			DefineAttack(ActorType.SPITTING_COBRA,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.KOBOLD,"kobold",'k',Color.Blue,10,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION,AttrType.AGGRESSIVE,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.KOBOLD,100,1,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.SPORE_POD,"spore pod",'e',Color.DarkMagenta,10,100,0,AttrType.FLYING,AttrType.SPORE_BURST,AttrType.PLANTLIKE,AttrType.BLINDSIGHT,AttrType.SMALL,AttrType.NO_CORPSE_KNOCKBACK,AttrType.MINDLESS);
			DefineAttack(ActorType.SPORE_POD,100,0,CriticalEffect.NO_CRIT,"& bumps *");

			Define(ActorType.FORASECT,"forasect",'i',Color.Gray,20,100,0,AttrType.REGENERATING);
			DefineAttack(ActorType.FORASECT,100,2,CriticalEffect.WEAK_POINT,"& bites *");

			Define(ActorType.POLTERGEIST,"poltergeist",'G',Color.DarkGreen,20,100,0,AttrType.NONLIVING,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.FLYING,AttrType.MINDLESS);
			DefineAttack(ActorType.POLTERGEIST,100,2,CriticalEffect.NO_CRIT,"& pokes *"); //change back to "slimes you", and add slime effect?
			DefineAttack(ActorType.POLTERGEIST,100,0,CriticalEffect.NO_CRIT,"& grabs at *");

			Define(ActorType.CULTIST,"cultist",'p',Color.DarkRed,20,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SMALL_GROUP,AttrType.AGGRESSIVE,AttrType.MINDLESS);
			DefineAttack(ActorType.CULTIST,100,2,CriticalEffect.MAX_DAMAGE,"& hits *");
			DefineAttack(ActorType.FINAL_LEVEL_CULTIST,100,2,CriticalEffect.MAX_DAMAGE,"& hits *");

			Define(ActorType.GOBLIN_ARCHER,"goblin archer",'g',Color.DarkCyan,15,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION,AttrType.AGGRESSIVE,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.GOBLIN_ARCHER,100,2,CriticalEffect.STUN,"& hits *");

			Define(ActorType.GOBLIN_SHAMAN,"goblin shaman",'g',Color.Magenta,15,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION,AttrType.AGGRESSIVE);
			DefineAttack(ActorType.GOBLIN_SHAMAN,100,2,CriticalEffect.STUN,"& hits *");
			Prototype(ActorType.GOBLIN_SHAMAN).DefineMagicSkillForMonster(4);
			Prototype(ActorType.GOBLIN_SHAMAN).GainSpell(SpellType.FORCE_PALM,SpellType.SCORCH);

			Define(ActorType.GOLDEN_DART_FROG,"golden dart frog",'t',Color.Yellow,20,100,0,AttrType.LOW_LIGHT_VISION,AttrType.CAN_POISON_WEAPONS,AttrType.TERRITORIAL);
			DefineAttack(ActorType.GOLDEN_DART_FROG,100,2,CriticalEffect.POISON,"& slams *");

			Define(ActorType.SKELETON,"skeleton",'s',Color.White,10,100,0,AttrType.NONLIVING,AttrType.IMMUNE_BURNING,AttrType.IMMUNE_COLD,AttrType.REASSEMBLES,AttrType.LOW_LIGHT_VISION,AttrType.MINDLESS);
			DefineAttack(ActorType.SKELETON,100,2,CriticalEffect.MAX_DAMAGE,"& hits *");

			Define(ActorType.SHADOW,"shadow",'G',Color.DarkGray,20,100,0,AttrType.NONLIVING,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.SHADOW_CLOAK,AttrType.LIGHT_SENSITIVE,AttrType.DESTROYED_BY_SUNLIGHT,AttrType.MINDLESS);
			DefineAttack(ActorType.SHADOW,100,1,CriticalEffect.DIM_VISION,"& hits *");

			Define(ActorType.MIMIC,"mimic",'m',Color.White,30,200,0,AttrType.GRAB_HIT);
			DefineAttack(ActorType.MIMIC,100,2,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.PHASE_SPIDER,"phase spider",'A',Color.Cyan,25,100,0,AttrType.POISON_HIT,AttrType.LOW_LIGHT_VISION,AttrType.NONEUCLIDEAN_MOVEMENT); //changed to 25hp from 30
			DefineAttack(ActorType.PHASE_SPIDER,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.ZOMBIE,"zombie",'z',Color.DarkGray,50,150,0,AttrType.NONLIVING,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.MINDLESS);
			DefineAttack(ActorType.ZOMBIE,100,4,CriticalEffect.GRAB,"& bites *");
			DefineAttack(ActorType.ZOMBIE,200,2,CriticalEffect.NO_CRIT,"& lunges forward and hits *","& lunges forward and misses *","& lunges");

			Define(ActorType.BERSERKER,"berserker",'p',Color.Red,30,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.BERSERKER,100,4,CriticalEffect.MAX_DAMAGE,"& hits *");
			Prototype(ActorType.BERSERKER).skills[SkillType.SPIRIT] = 4;

			Define(ActorType.GIANT_SLUG,"giant slug",'w',Color.DarkGreen,50,150,0,AttrType.ACID_HIT,AttrType.SLIMED);
			DefineAttack(ActorType.GIANT_SLUG,100,1,CriticalEffect.SLIME,"& slams *");
			DefineAttack(ActorType.GIANT_SLUG,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.VULGAR_DEMON,"vulgar demon",'d',Color.Red,30,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE,AttrType.DAMAGE_RESISTANCE);
			DefineAttack(ActorType.VULGAR_DEMON,100,3,CriticalEffect.WEAK_POINT,"& hits *");

			Define(ActorType.BANSHEE,"banshee",'G',Color.Magenta,20,50,0,AttrType.NONLIVING,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.FLYING,AttrType.AGGRESSIVE,AttrType.MINDLESS);
			DefineAttack(ActorType.BANSHEE,100,3,CriticalEffect.MAX_DAMAGE,"& claws *");

			Define(ActorType.CAVERN_HAG,"cavern hag",'h',Color.Blue,30,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.CAVERN_HAG,100,2,CriticalEffect.GRAB,"& clutches at *");

			Define(ActorType.ROBED_ZEALOT,"robed zealot",'p',Color.Yellow,35,100,2,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.ROBED_ZEALOT,100,3,CriticalEffect.KNOCKBACK,"& hammers *");
			Prototype(ActorType.ROBED_ZEALOT).skills[SkillType.SPIRIT] = 5;

			Define(ActorType.DIRE_RAT,"dire rat",'r',Color.DarkRed,15,100,0,AttrType.LOW_LIGHT_VISION,AttrType.LARGE_GROUP,AttrType.SMALL,AttrType.KEEN_SENSES);
			DefineAttack(ActorType.DIRE_RAT,100,1,CriticalEffect.INFLICT_VULNERABILITY,"& bites *");

			Define(ActorType.SKULKING_KILLER,"skulking killer",'p',Color.DarkBlue,30,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION,AttrType.CAN_DODGE,AttrType.POISON_HIT);
			DefineAttack(ActorType.SKULKING_KILLER,100,3,CriticalEffect.WEAK_POINT,"& hits *");
			Prototype(ActorType.SKULKING_KILLER).skills[SkillType.STEALTH] = 5;
			Prototype(ActorType.SKULKING_KILLER).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.WILD_BOAR,"wild boar",'q',Color.DarkYellow,70,100,0,AttrType.LOW_LIGHT_VISION,AttrType.KEEN_SENSES,AttrType.TERRITORIAL);
			DefineAttack(ActorType.WILD_BOAR,100,1,CriticalEffect.NO_CRIT,"& gores *");

			Define(ActorType.TROLL,"troll",'T',Color.DarkGreen,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.TROLL,100,4,CriticalEffect.WORN_OUT,"& claws *");

			Define(ActorType.DREAM_SPRITE,"dream sprite",'y',Color.Cyan,30,100,0,AttrType.SMALL,AttrType.FLYING,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.DREAM_SPRITE,100,1,CriticalEffect.NO_CRIT,"& pokes *");

			Define(ActorType.DREAM_SPRITE_CLONE,"dream sprite",'y',Color.Cyan,1,0,0,AttrType.SMALL,AttrType.FLYING,AttrType.NONLIVING,AttrType.NO_CORPSE_KNOCKBACK); //speed is set to 100 *after* a clone is created for technical reasons
			DefineAttack(ActorType.DREAM_SPRITE_CLONE,100,0,CriticalEffect.NO_CRIT,"& pokes *");

			Define(ActorType.CLOUD_ELEMENTAL,"cloud elemental",'E',Color.RandomLightning,35,100,0,AttrType.NONLIVING,AttrType.FLYING,AttrType.IMMUNE_ELECTRICITY,AttrType.BLINDSIGHT,AttrType.NO_CORPSE_KNOCKBACK,AttrType.MINDLESS); //todo: give weapon resistance, lower hp?
			DefineAttack(ActorType.CLOUD_ELEMENTAL,100,0,CriticalEffect.NO_CRIT,"& bumps *");

			Define(ActorType.DERANGED_ASCETIC,"deranged ascetic",'p',Color.RandomDark,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SILENCE_AURA,AttrType.CAN_DODGE);
			DefineAttack(ActorType.DERANGED_ASCETIC,100,3,CriticalEffect.SWAP_POSITIONS,"& strikes *");
			DefineAttack(ActorType.DERANGED_ASCETIC,100,3,CriticalEffect.SWAP_POSITIONS,"& punches *");
			DefineAttack(ActorType.DERANGED_ASCETIC,100,3,CriticalEffect.SWAP_POSITIONS,"& kicks *");
			Prototype(ActorType.DERANGED_ASCETIC).skills[SkillType.SPIRIT] = 6;

			Define(ActorType.ORC_GRENADIER,"orc grenadier",'o',Color.DarkYellow,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.ORC_GRENADIER,100,3,CriticalEffect.STUN,"& hits *");
			Prototype(ActorType.ORC_GRENADIER).skills[SkillType.DEFENSE] = 6;
			
			Define(ActorType.WARG,"warg",'c',Color.White,25,50,0,AttrType.LOW_LIGHT_VISION,AttrType.MEDIUM_GROUP,AttrType.KEEN_SENSES,AttrType.AGGRESSIVE);
			DefineAttack(ActorType.WARG,100,3,CriticalEffect.TRIP,"& bites *");

			Define(ActorType.ALASI_SCOUT,"alasi scout",'a',Color.Blue,35,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.ALASI_SCOUT,100,3,CriticalEffect.WEAK_POINT,"& hits *");
			DefineAttack(ActorType.ALASI_SCOUT,100,3,CriticalEffect.NO_CRIT,"& fires a phantom blade at *","& misses * with a phantom blade","& fires a phantom blade");
			Prototype(ActorType.ALASI_SCOUT).skills[SkillType.DEFENSE] = 6;

			Define(ActorType.CARRION_CRAWLER,"carrion crawler",'i',Color.DarkGreen,20,100,0,AttrType.PARALYSIS_HIT,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.CARRION_CRAWLER,100,0,CriticalEffect.NO_CRIT,"& lashes * with a tentacle");
			DefineAttack(ActorType.CARRION_CRAWLER,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.MECHANICAL_KNIGHT,"mechanical knight",'K',Color.DarkRed,10,100,0,AttrType.NONLIVING,AttrType.MECHANICAL_SHIELD,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.DULLS_BLADES,AttrType.IMMUNE_BURNING,AttrType.MINDLESS,AttrType.MINOR_IMMUNITY,AttrType.MENTAL_IMMUNITY);
			DefineAttack(ActorType.MECHANICAL_KNIGHT,100,3,CriticalEffect.WEAK_POINT,"& hits *");
			DefineAttack(ActorType.MECHANICAL_KNIGHT,100,3,CriticalEffect.WEAK_POINT,"& kicks *");

			Define(ActorType.RUNIC_TRANSCENDENT,"runic transcendent",'h',Color.Magenta,35,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE,AttrType.REGENERATING,AttrType.MENTAL_IMMUNITY,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.RUNIC_TRANSCENDENT,100,2,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.RUNIC_TRANSCENDENT).DefineMagicSkillForMonster(6);
			Prototype(ActorType.RUNIC_TRANSCENDENT).GainSpell(SpellType.MERCURIAL_SPHERE);
			Prototype(ActorType.RUNIC_TRANSCENDENT).skills[SkillType.SPIRIT] = 6;

			Define(ActorType.ALASI_BATTLEMAGE,"alasi battlemage",'a',Color.Yellow,35,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.ALASI_BATTLEMAGE,100,2,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.ALASI_BATTLEMAGE).skills[SkillType.DEFENSE] = 6;
			Prototype(ActorType.ALASI_BATTLEMAGE).DefineMagicSkillForMonster(7);
			Prototype(ActorType.ALASI_BATTLEMAGE).GainSpell(SpellType.FLYING_LEAP,SpellType.MAGIC_HAMMER);

			Define(ActorType.ALASI_SOLDIER,"alasi soldier",'a',Color.White,40,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.ALASI_SOLDIER,100,4,CriticalEffect.NO_CRIT,"& hits * with its spear","& misses * with its spear","& thrusts its spear");
			Prototype(ActorType.ALASI_SOLDIER).skills[SkillType.DEFENSE] = 6;
			Prototype(ActorType.ALASI_SOLDIER).skills[SkillType.SPIRIT] = 4;

			Define(ActorType.SKITTERMOSS,"skittermoss",'F',Color.Gray,40,50,0,AttrType.BLINDSIGHT,AttrType.MINDLESS);
			DefineAttack(ActorType.SKITTERMOSS,100,3,CriticalEffect.INFEST,"& hits *");

			Define(ActorType.STONE_GOLEM,"stone golem",'x',Color.Gray,70,150,0,AttrType.NONLIVING,AttrType.STALAGMITE_HIT,AttrType.DULLS_BLADES,AttrType.IMMUNE_BURNING,AttrType.LOW_LIGHT_VISION,AttrType.MINDLESS,AttrType.DAMAGE_RESISTANCE);
			DefineAttack(ActorType.STONE_GOLEM,100,4,CriticalEffect.NO_CRIT,"& slams *");

			Define(ActorType.MUD_ELEMENTAL,"mud elemental",'E',Color.DarkYellow,20,100,0,AttrType.NONLIVING,AttrType.BLINDSIGHT,AttrType.RESIST_WEAPONS,AttrType.MINDLESS);//,AttrType.IMMUNE_ARROWS); //todo: keep immunity to arrows or not? nothing else currently has it.
			DefineAttack(ActorType.MUD_ELEMENTAL,100,2,CriticalEffect.BLIND,"& hits *");

			Define(ActorType.MUD_TENTACLE,"mud tentacle",'~',Color.DarkYellow,1,100,0,AttrType.NONLIVING,AttrType.BLINDSIGHT,AttrType.GRAB_HIT,AttrType.IMMOBILE,AttrType.MINDLESS);
			DefineAttack(ActorType.MUD_TENTACLE,100,1,CriticalEffect.NO_CRIT,"& hits *"); //this attack has a special hack - it deals 1 damage, not 1d6

			Define(ActorType.FLAMETONGUE_TOAD,"flametongue toad",'t',Color.Red,30,100,0,AttrType.MEDIUM_GROUP,AttrType.IMMUNE_FIRE,AttrType.IMMUNE_BURNING,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.FLAMETONGUE_TOAD,100,2,CriticalEffect.KNOCKBACK,"& slams *");

			Define(ActorType.ENTRANCER,"entrancer",'p',Color.DarkMagenta,35,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.ENTRANCER,100,2,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.ENTRANCER).skills[SkillType.SPIRIT] = 4;

			Define(ActorType.OGRE_BARBARIAN,"ogre barbarian",'O',Color.DarkYellow,50,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.LOW_LIGHT_VISION,AttrType.GRAB_HIT,AttrType.AGGRESSIVE);
			DefineAttack(ActorType.OGRE_BARBARIAN,100,3,CriticalEffect.NO_CRIT,"& hits *");
			DefineAttack(ActorType.OGRE_BARBARIAN,100,3,CriticalEffect.NO_CRIT,"& lifts * and slams * down");

			Define(ActorType.SNEAK_THIEF,"sneak thief",'p',Color.DarkCyan,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.SNEAK_THIEF,100,3,CriticalEffect.STEAL,"& hits *");
			Prototype(ActorType.SNEAK_THIEF).skills[SkillType.DEFENSE] = 2;
			
			Define(ActorType.LASHER_FUNGUS,"lasher fungus",'F',Color.DarkGreen,50,100,0,AttrType.PLANTLIKE,AttrType.SPORE_BURST,AttrType.IMMUNE_BURNING,AttrType.BLINDSIGHT,AttrType.IMMOBILE,AttrType.MINDLESS);
			DefineAttack(ActorType.LASHER_FUNGUS,100,1,CriticalEffect.TRIP,"& extends a tentacle and whips *","& misses * with a tentacle","& extends a tentacle");
			DefineAttack(ActorType.LASHER_FUNGUS,100,1,CriticalEffect.TRIP,"& extends a tentacle and drags * closer","& misses * with a tentacle","& extends a tentacle");

			Define(ActorType.CRUSADING_KNIGHT,"crusading knight",'K',Color.DarkGray,50,100,6,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.CRUSADING_KNIGHT,200,7,CriticalEffect.STRONG_KNOCKBACK,"& hits * with a huge mace","& misses * with a huge mace","& swings a huge mace");
			Prototype(ActorType.CRUSADING_KNIGHT).skills[SkillType.DEFENSE] = 20;
			Prototype(ActorType.CRUSADING_KNIGHT).skills[SkillType.SPIRIT] = 4;

			Define(ActorType.TROLL_BLOODWITCH,"troll bloodwitch",'T',Color.DarkRed,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.REGENERATING,AttrType.REGENERATING,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.TROLL_BLOODWITCH,100,4,CriticalEffect.WORN_OUT,"& claws *");

			Define(ActorType.LUMINOUS_AVENGER,"luminous avenger",'E',Color.Yellow,40,50,10,AttrType.NONLIVING);
			DefineAttack(ActorType.LUMINOUS_AVENGER,100,4,CriticalEffect.BLIND,"& strikes *");
			
			Define(ActorType.MARBLE_HORROR,"marble horror",'5',Color.Gray,50,100,0,AttrType.NONLIVING,AttrType.HUMANOID_INTELLIGENCE,AttrType.LOW_LIGHT_VISION,AttrType.BLEED_HIT,AttrType.AVOIDS_LIGHT);
			DefineAttack(ActorType.MARBLE_HORROR,100,1,CriticalEffect.NO_CRIT,"& touches *");
			DefineAttack(ActorType.MARBLE_HORROR_STATUE,100,0,CriticalEffect.NO_CRIT,"& glares at *");

			Define(ActorType.CORROSIVE_OOZE,"corrosive ooze",'J',Color.Green,35,100,0,AttrType.PLANTLIKE,AttrType.BLINDSIGHT,AttrType.ACID_HIT,AttrType.MINDLESS);
			DefineAttack(ActorType.CORROSIVE_OOZE,100,3,CriticalEffect.NO_CRIT,"& splashes *");

			Define(ActorType.PYREN_ARCHER,"pyren archer",'P',Color.DarkRed,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.FIERY_ARROWS,AttrType.IMMUNE_BURNING,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.PYREN_ARCHER,100,3,CriticalEffect.WEAK_POINT,"& hits *"); //was ignite
			Prototype(ActorType.PYREN_ARCHER).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.SPELLMUDDLE_PIXIE,"spellmuddle pixie",'y',Color.RandomBright,15,50,0,AttrType.SMALL,AttrType.FLYING,AttrType.SILENCE_AURA,AttrType.LARGE_GROUP);
			DefineAttack(ActorType.SPELLMUDDLE_PIXIE,100,2,CriticalEffect.MAX_DAMAGE,"& scratches *");

			Define(ActorType.ALASI_SENTINEL,"alasi sentinel",'a',Color.DarkGray,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.DAMAGE_RESISTANCE,AttrType.DAMAGE_RESISTANCE,AttrType.DAMAGE_RESISTANCE,AttrType.DAMAGE_RESISTANCE,AttrType.DAMAGE_RESISTANCE);
			DefineAttack(ActorType.ALASI_SENTINEL,100,3,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.ALASI_SENTINEL).skills[SkillType.DEFENSE] = 10;
			Prototype(ActorType.ALASI_SENTINEL).skills[SkillType.SPIRIT] = 4;

			Define(ActorType.NOXIOUS_WORM,"noxious worm",'W',Color.DarkMagenta,55,150,0);
			DefineAttack(ActorType.NOXIOUS_WORM,100,3,CriticalEffect.STUN,"& slams *"); //knockback?
			DefineAttack(ActorType.NOXIOUS_WORM,100,3,CriticalEffect.STUN,"& bites *");

			Define(ActorType.CYCLOPEAN_TITAN,"cyclopean titan",'H',Color.DarkYellow,150,100,0,AttrType.LOW_LIGHT_VISION,AttrType.BRUTISH_STRENGTH);
			DefineAttack(ActorType.CYCLOPEAN_TITAN,100,5,CriticalEffect.WORN_OUT,"& clobbers *"); //brutish strength means this attack will always deal 30 damage

			Define(ActorType.VAMPIRE,"vampire",'V',Color.Blue,50,50,0,AttrType.NONLIVING,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE,AttrType.RESIST_NECK_SNAP,AttrType.FLYING,AttrType.LIGHT_SENSITIVE,AttrType.DESTROYED_BY_SUNLIGHT,AttrType.LIFE_DRAIN_HIT,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.SHADOW_CLOAK);
			DefineAttack(ActorType.VAMPIRE,100,4,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.ORC_WARMAGE,"orc warmage",'o',Color.Red,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.ORC_WARMAGE,100,3,CriticalEffect.STUN,"& hits *");
			Prototype(ActorType.ORC_WARMAGE).GainSpell(SpellType.DETECT_MOVEMENT,SpellType.BLINK,SpellType.SCORCH,SpellType.MAGIC_HAMMER,SpellType.DOOM,SpellType.COLLAPSE);
			Prototype(ActorType.ORC_WARMAGE).DefineMagicSkillForMonster(10);
			Prototype(ActorType.ORC_WARMAGE).skills[SkillType.DEFENSE] = 2;
			
			Define(ActorType.NECROMANCER,"necromancer",'p',Color.Blue,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.KEEPS_DISTANCE);
			DefineAttack(ActorType.NECROMANCER,100,2,CriticalEffect.DRAIN_LIFE,"& hits *");

			Define(ActorType.STALKING_WEBSTRIDER,"stalking webstrider",'A',Color.Red,50,50,0,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.POISON_HIT,AttrType.CAN_DODGE);
			DefineAttack(ActorType.STALKING_WEBSTRIDER,100,3,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.ORC_ASSASSIN,"orc assassin",'o',Color.DarkBlue,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION,AttrType.SILENCE_HIT,AttrType.CAN_DODGE);
			DefineAttack(ActorType.ORC_ASSASSIN,100,4,CriticalEffect.NO_CRIT,"& hits *");
			DefineAttack(ActorType.ORC_ASSASSIN,100,4,CriticalEffect.NO_CRIT,"& lunges to hit *","& lunges but misses *","& lunges");
			Prototype(ActorType.ORC_ASSASSIN).skills[SkillType.STEALTH] = 10;
			Prototype(ActorType.ORC_ASSASSIN).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.CORPSETOWER_BEHEMOTH,"corpsetower behemoth",'Z',Color.DarkMagenta,75,100,0,AttrType.NONLIVING,AttrType.REGENERATING,AttrType.IMMUNE_COLD,AttrType.STUN_HIT,AttrType.WORN_OUT_HIT,AttrType.LOW_LIGHT_VISION,AttrType.MINDLESS);
			DefineAttack(ActorType.CORPSETOWER_BEHEMOTH,100,7,CriticalEffect.NO_CRIT,"& clobbers *");

			Define(ActorType.MACHINE_OF_WAR,"machine of war",'M',Color.DarkGray,55,100,0,AttrType.NONLIVING,AttrType.BLINDSIGHT,AttrType.DULLS_BLADES,AttrType.IMMUNE_FIRE,AttrType.IMMUNE_BURNING,AttrType.AGGRESSIVE,AttrType.MINDLESS,AttrType.DAMAGE_RESISTANCE,AttrType.DAMAGE_RESISTANCE,AttrType.MENTAL_IMMUNITY);
			DefineAttack(ActorType.MACHINE_OF_WAR,100,0,CriticalEffect.NO_CRIT,"& bumps *");

			Define(ActorType.IMPOSSIBLE_NIGHTMARE,"impossible nightmare",'X',Color.RandomDoom,100,200,0,AttrType.AGGRESSIVE,AttrType.KEEN_SENSES,AttrType.BLINDSIGHT,AttrType.MINDLESS,AttrType.ONE_HP_HIT,AttrType.NONEUCLIDEAN_MOVEMENT,AttrType.DAMAGE_RESISTANCE,AttrType.DAMAGE_RESISTANCE);
			DefineAttack(ActorType.IMPOSSIBLE_NIGHTMARE,200,1,CriticalEffect.NO_CRIT,"& brushes against *");

			Define(ActorType.FIRE_DRAKE,"fire drake",'D',Color.DarkRed,200,50,2,AttrType.BOSS_MONSTER,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.FIRE_DRAKE,100,3,CriticalEffect.MAX_DAMAGE,"& bites *");
			DefineAttack(ActorType.FIRE_DRAKE,100,3,CriticalEffect.MAX_DAMAGE,"& claws *");

			Define(ActorType.GHOST,"ghost",'G',Color.White,20,100,0,AttrType.NONLIVING,AttrType.FLYING,AttrType.MINDLESS);
			DefineAttack(ActorType.GHOST,100,2,CriticalEffect.INFLICT_VULNERABILITY,"& touches *");

			Define(ActorType.BLADE,"blade",')',Color.White,5,0,0,AttrType.NONLIVING,AttrType.BLINDSIGHT,AttrType.IMMUNE_BURNING,AttrType.MINDLESS,AttrType.FLYING,AttrType.SMALL,AttrType.AGGRESSIVE); //speed is set after creation
			DefineAttack(ActorType.BLADE,100,2,CriticalEffect.NO_CRIT,"& slices *");

			Define(ActorType.MINOR_DEMON,"minor demon",'d',Color.DarkGray,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE,AttrType.IMMUNE_BURNING);
			DefineAttack(ActorType.MINOR_DEMON,100,3,CriticalEffect.STUN,"& hits *");

			Define(ActorType.FROST_DEMON,"frost demon",'d',Color.RandomIce,55,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE,AttrType.IMMUNE_COLD,AttrType.IMMUNE_BURNING);
			DefineAttack(ActorType.FROST_DEMON,100,3,CriticalEffect.FREEZE,"& hits *");

			Define(ActorType.BEAST_DEMON,"beast demon",'d',Color.DarkGreen,50,50,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE,AttrType.IMMUNE_BURNING);
			DefineAttack(ActorType.BEAST_DEMON,100,5,CriticalEffect.KNOCKBACK,"& hits *");

			Define(ActorType.DEMON_LORD,"demon lord",'d',Color.Magenta,75,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE,AttrType.IMMUNE_BURNING);
			DefineAttack(ActorType.DEMON_LORD,100,5,CriticalEffect.GRAB,"& whips *","& misses * with its whip","& swings its whip");
			Prototype(ActorType.DEMON_LORD).skills[SkillType.DEFENSE] = 6;

			Define(ActorType.PHANTOM,"phantom",'?',Color.Cyan,1,100,0,AttrType.NONLIVING,AttrType.FLYING,AttrType.NO_CORPSE_KNOCKBACK,AttrType.MINDLESS); //the template on which the different types of phantoms are based
			DefineAttack(ActorType.PHANTOM_ARCHER,100,2,CriticalEffect.NO_CRIT,"& hits *");
			DefineAttack(ActorType.PHANTOM_BEHEMOTH,100,7,CriticalEffect.NO_CRIT,"& clobbers *");
			DefineAttack(ActorType.PHANTOM_BLIGHTWING,100,3,CriticalEffect.MAX_DAMAGE,"& bites *");
			DefineAttack(ActorType.PHANTOM_BLIGHTWING,100,3,CriticalEffect.MAX_DAMAGE,"& scratches *");
			DefineAttack(ActorType.PHANTOM_CONSTRICTOR,100,2,CriticalEffect.NO_CRIT,"& hits *");
			DefineAttack(ActorType.PHANTOM_CRUSADER,200,7,CriticalEffect.STRONG_KNOCKBACK,"& hits * with a huge mace","& misses * with a huge mace","& swings a huge mace");
			DefineAttack(ActorType.PHANTOM_WASP,100,1,CriticalEffect.NO_CRIT,"& stings *");
			DefineAttack(ActorType.PHANTOM_SWORDMASTER,100,3,CriticalEffect.NO_CRIT,"& hits *");
			DefineAttack(ActorType.PHANTOM_TIGER,100,4,CriticalEffect.SLOW,"& bites *");
			DefineAttack(ActorType.PHANTOM_ZOMBIE,100,4,CriticalEffect.GRAB,"& bites *");
			DefineAttack(ActorType.PHANTOM_ZOMBIE,200,2,CriticalEffect.NO_CRIT,"& lunges forward and hits *","& lunges forward and misses *","& lunges");
		}
		private static void Define(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int light_radius_,params AttrType[] attrlist){
			proto[type_] = new Actor(type_,name_,symbol_,color_,maxhp_,speed_,light_radius_,attrlist);
		}
		private static void DefineAttack(ActorType type,int cost,int damage_dice,CriticalEffect crit,string message){ DefineAttack(type,cost,damage_dice,crit,message,"",""); }
		private static void DefineAttack(ActorType type,int cost,int damage_dice,CriticalEffect crit,string message,string miss_message,string armor_blocked_message){
			if(attack[type] == null){
				attack[type] = new List<AttackInfo>();
			}
			attack[type].Add(new AttackInfo(cost,damage_dice,crit,message,miss_message,armor_blocked_message)); //monsters will try to use attack 0 while confused, so the first one should be a basic attack when possible.
		}
		public Actor(){
			inv = new List<Item>();
			attrs = new Dict<AttrType, int>();
			skills = new Dict<SkillType,int>();
			feats = new Dict<FeatType,bool>();
			spells = new Dict<SpellType,bool>();
		}
		public Actor(Actor a,int r,int c){
			type = a.type;
			name = a.name;
			the_name = a.the_name;
			a_name = a.a_name;
			symbol = a.symbol;
			color = a.color;
			maxhp = a.maxhp;
			curhp = maxhp;
			maxmp = a.maxmp;
			curmp = maxmp;
			speed = a.speed;
			light_radius = a.light_radius;
			target = null;
			inv = new List<Item>();
			row = r;
			col = c;
			target_location = null;
			time_of_last_action = 0;
			recover_time = 0;
			player_visibility_duration = 0;
			weapons.AddFirst(new Weapon(WeaponType.NO_WEAPON));
			armors.AddFirst(new Armor(ArmorType.NO_ARMOR));
			attrs = new Dict<AttrType, int>(a.attrs);
			skills = new Dict<SkillType,int>(a.skills);
			feats = new Dict<FeatType,bool>(a.feats);
			spells = new Dict<SpellType,bool>(a.spells);
			exhaustion = 0;
			sprite_offset = a.sprite_offset;
		}
		public Actor(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int light_radius_,params AttrType[] attrlist){
			type = type_;
			SetName(name_);
			symbol = symbol_;
			color = color_;
			maxhp = maxhp_;
			curhp = maxhp;
			maxmp = 0;
			curmp = maxmp;
			speed = speed_;
			light_radius = light_radius_;
			target = null;
			inv = null;
			target_location = null;
			time_of_last_action = 0;
			recover_time = 0;
			player_visibility_duration = 0;
			exhaustion = 0;
			foreach(AttrType at in attrlist){
				attrs[at]++;
			}//row and col are -1
			switch(type){
			case ActorType.PLAYER:
				sprite_offset = new pos(0,32);
				break;
			case ActorType.MUD_TENTACLE:
				sprite_offset = new pos(13,32);
				break;
			case ActorType.GHOST:
				sprite_offset = new pos(13,34);
				break;
			default:
				if(type >= ActorType.MINOR_DEMON && type <= ActorType.DEMON_LORD){
					int diff = type - ActorType.MINOR_DEMON;
					sprite_offset = new pos(13,36+diff);
				}
				else{ //phantoms are handled in CreatePhantom()
					int diff = type - ActorType.GOBLIN;
					sprite_offset = new pos(4 + diff / 8,32 + (diff % 8)*2);
				}
				break;
			}
		}
		public static Actor Create(ActorType type,int r,int c){ return Create(type,r,c,TiebreakerAssignment.AtEnd); }
		public static Actor Create(ActorType type,int r,int c,TiebreakerAssignment tiebreaker){
			Actor a = null;
			if(M.actor[r,c] == null){
				a = new Actor(proto[type],r,c);
				M.actor[r,c] = a;
				switch(tiebreaker){
				case TiebreakerAssignment.AtEnd:
				{
					tiebreakers.Add(a);
					Event e = new Event(a,a.Speed(),EventType.MOVE);
					e.tiebreaker = tiebreakers.Count - 1; //since it's the last one
					Q.Add(e);
					break;
				}
				case TiebreakerAssignment.InsertAfterCurrent:
				{
					tiebreakers.Insert(Q.Tiebreaker + 1,a);
					Q.UpdateTiebreaker(Q.Tiebreaker + 1);
					Event e = new Event(a,a.Speed(),EventType.MOVE);
					e.tiebreaker = Q.Tiebreaker + 1;
					Q.Add(e);
					break;
				}
				case TiebreakerAssignment.UseCurrent:
				{
					tiebreakers[Q.Tiebreaker] = a;
					a.QS();
					break;
				}
				}
				if(R.OneIn(10) && a.Is(ActorType.SWORDSMAN,ActorType.BERSERKER,ActorType.ENTRANCER,ActorType.DERANGED_ASCETIC,ActorType.ALASI_BATTLEMAGE,ActorType.ALASI_SCOUT,
					ActorType.ALASI_SENTINEL,ActorType.ALASI_SOLDIER,ActorType.PYREN_ARCHER,ActorType.NECROMANCER)){
					a.light_radius = 4;
				}
				if(a.light_radius > 0){
					a.UpdateRadius(0,a.light_radius);
				}
			}
			return a;
		}
		public static Actor CreatePhantom(int r,int c){
			Actor a = Create(ActorType.PHANTOM,r,c,TiebreakerAssignment.InsertAfterCurrent);
			if(a == null){
				return null;
			}
			ActorType type = (ActorType)(R.Roll(9) + (int)ActorType.PHANTOM);
			a.type = type;
			switch(type){
			case ActorType.PHANTOM_ARCHER:
				a.SetName("phantom archer");
				a.symbol = 'g';
				break;
			case ActorType.PHANTOM_BEHEMOTH:
				a.SetName("phantom behemoth");
				a.symbol = 'H';
				a.attrs[AttrType.STUN_HIT]++;
				a.attrs[AttrType.WORN_OUT_HIT]++;
				break;
			case ActorType.PHANTOM_BLIGHTWING:
				a.SetName("phantom blightwing");
				a.symbol = 'b';
				a.speed = 50;
				break;
			case ActorType.PHANTOM_CONSTRICTOR:
				a.SetName("phantom constrictor");
				a.symbol = 'S';
				a.attrs[AttrType.GRAB_HIT]++;
				break;
			case ActorType.PHANTOM_CRUSADER:
				a.SetName("phantom crusader");
				a.symbol = 'K';
				a.UpdateRadius(0,6,true);
				break;
			case ActorType.PHANTOM_WASP:
				a.SetName("phantom wasp");
				a.symbol = 'i';
				a.attrs[AttrType.EXHAUST_HIT]++;
				a.attrs[AttrType.CAN_DODGE]++;
				break;
			case ActorType.PHANTOM_SWORDMASTER:
				a.SetName("phantom swordmaster");
				a.symbol = 'h';
				break;
			case ActorType.PHANTOM_TIGER:
				a.SetName("phantom tiger");
				a.symbol = 'f';
				a.speed = 50;
				break;
			case ActorType.PHANTOM_ZOMBIE:
				a.SetName("phantom zombie");
				a.symbol = 'z';
				a.speed = 150;
				break;
			}
			int diff = (a.type - ActorType.PHANTOM_ZOMBIE) + 6;
			a.sprite_offset = new pos(13 + diff / 8,32 + (diff % 8)*2);
			return a;
		}
		public bool Is(params ActorType[] types){
			foreach(ActorType at in types){
				if(type == at){
					return true;
				}
			}
			return false;
		}
		public bool IsFinalLevelDemon(){
			return Is(ActorType.MINOR_DEMON,ActorType.FROST_DEMON,ActorType.BEAST_DEMON,ActorType.DEMON_LORD);
		}
		public string AName(bool consider_visibility){
			if(!consider_visibility || player.CanSee(this)){
				return a_name;
			}
			else{
				return "something";
			}
		}
		public string TheName(bool consider_visibility){
			if(!consider_visibility || player.CanSee(this)){
				return the_name;
			}
			else{
				return "something";
			}
		}
		override public string YouVisible(string s){ return YouVisible(s,false); }
		override public string YouVisible(string s,bool ends_in_es){ //if not visible, YouVisible("attack") returns "something attacks"
			if(name == "you"){
				return "you " + s;
			}
			else{
				if(ends_in_es){
					return TheName(true) + " " + s + "es";
				}
				else{
					return TheName(true) + " " + s + "s";
				}
			}
		}
		public string YouVisibleAre(){
			if(name == "you"){
				return "you are";
			}
			else{
				if(player.CanSee(this)){
					return the_name + " is";
				}
				else{
					return "something is";
				}
			}
		}
		public string YourVisible(){
			if(name == "you"){
				return "your";
			}
			else{
				if(player.CanSee(this)){
					return the_name + "'s";
				}
				else{
					return "something's";
				}
			}
		}
		public void Move(int r,int c){ Move(r,c,true); }
		public void Move(int r,int c,bool trigger_traps){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				if(row >= 0 && row < ROWS && col >= 0 && col < COLS){
					if(this == player){
						if(DistanceFrom(r,c) == 1 && !tile().opaque){ //makes your trail harder to follow in fog etc.
							tile().direction_exited = DirectionOf(new pos(r,c));
						}
						else{
							tile().direction_exited = 0;
						}
					}
					else{
						if(DistanceFrom(r,c) == 1){
							attrs[AttrType.DIRECTION_OF_PREVIOUS_TILE] = DirectionOf(new pos(r,c)).RotateDir(true,4);
						}
						else{
							attrs[AttrType.DIRECTION_OF_PREVIOUS_TILE] = -1;
						}
					}
				}
				if(M.actor[r,c] == null){
					if(HasAttr(AttrType.GRABBED)){
						foreach(Actor a in ActorsAtDistance(1)){
							if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this)){
								if(a.DistanceFrom(r,c) > 1){
									attrs[AttrType.GRABBED]--;
									a.attrs[AttrType.GRABBING] = 0;
								}
								else{
									a.attrs[AttrType.GRABBING] = a.DirectionOf(new pos(r,c));
								}
							}
						}
					}
					if(HasAttr(AttrType.GRABBING)){
						Actor a = ActorInDirection(attrs[AttrType.GRABBING]);
						if(a != null && a.HasAttr(AttrType.GRABBED)){
							if(a.DistanceFrom(r,c) > 1){
								a.attrs[AttrType.GRABBED]--;
								attrs[AttrType.GRABBING] = 0;
							}
							else{
								attrs[AttrType.GRABBING] = M.tile[r,c].DirectionOf(a);
							}
						}
					}
					bool torch = false;
					if(LightRadius() > 0){
						torch = true;
						UpdateRadius(LightRadius(),0);
					}
					M.actor[r,c] = this;
					if(row>=0 && row<ROWS && col>=0 && col<COLS){
						M.actor[row,col] = null;
						if(this == player && M.tile[row,col].inv != null && !HasAttr(AttrType.TURN_INTO_CORPSE)){
							M.tile[row,col].inv.ignored = true;
						}
					}
					row = r;
					col = c;
					if(torch){
						UpdateRadius(0,LightRadius());
					}
					if(tile().features.Contains(FeatureType.FIRE)){
						ApplyBurning();
					}
					else{
						if(HasAttr(AttrType.BURNING)){
							if(tile().Is(FeatureType.POISON_GAS,FeatureType.THICK_DUST)){
								RefreshDuration(AttrType.BURNING,0);
							}
							else{
								tile().ApplyEffect(DamageType.FIRE);
							}
						}
					}
					if(trigger_traps && tile().IsTrap() && !HasAttr(AttrType.FLYING)
					   && (type==ActorType.PLAYER || target == player)){ //prevents wandering monsters from triggering traps
						tile().TriggerTrap();
					}
				}
				else{ //default is now to swap places, rather than do nothing, since everything checks anyway.
					Actor a = M.actor[r,c]; //todo: there's no grab check here. Move() needs a bit of a rework.
					if(!a.HasAttr(AttrType.IMMOBILE)){
						bool torch = false;
						bool other_torch = false;
						if(LightRadius() > 0){
							torch = true;
							UpdateRadius(LightRadius(),0);
						}
						if(a.LightRadius() > 0){
							other_torch = true;
							a.UpdateRadius(a.LightRadius(),0);
						}
						if(row>=0 && row<ROWS && col>=0 && col<COLS){
							if(this == player && M.tile[row,col].inv != null){
								M.tile[row,col].inv.ignored = true;
							}
						}
						M.actor[r,c] = this;
						M.actor[row,col] = a;
						a.row = row;
						a.col = col;
						row = r;
						col = c;
						if(torch){
							UpdateRadius(0,LightRadius());
						}
						if(other_torch){
							a.UpdateRadius(0,a.LightRadius());
						}
						if(tile().features.Contains(FeatureType.FIRE)){
							ApplyBurning();
						}
						else{
							if(HasAttr(AttrType.BURNING)){
								if(tile().Is(FeatureType.POISON_GAS,FeatureType.THICK_DUST)){
									RefreshDuration(AttrType.BURNING,0);
								}
								else{
									tile().ApplyEffect(DamageType.FIRE);
								}
							}
						}
						if(a.tile().features.Contains(FeatureType.FIRE)){
							a.ApplyBurning();
						}
						else{
							if(a.HasAttr(AttrType.BURNING)){
								if(a.tile().Is(FeatureType.POISON_GAS,FeatureType.THICK_DUST)){
									a.RefreshDuration(AttrType.BURNING,0);
								}
								else{
									a.tile().ApplyEffect(DamageType.FIRE);
								}
							}
						}
						if(trigger_traps && tile().IsTrap() && !HasAttr(AttrType.FLYING)
							&& (type==ActorType.PLAYER || target == player)){ //prevents wandering monsters from triggering traps
							tile().TriggerTrap(); //todo: only the moving entity triggers traps right now - i should probably make the other one trigger traps, too.
						}
					}
				}
				if(this == player){
					M.safetymap = null;
				}
				else{
					if(player.HasAttr(AttrType.DETECTING_MOVEMENT) && DistanceFrom(player) <= 8 && !player.CanSee(this)){
						footsteps.AddUnique(p);
					}
				}
			}
		}
		public bool MovementPrevented(PhysicalObject o){
			if(HasAttr(AttrType.FROZEN,AttrType.IMMOBILE) || GrabPreventsMovement(o)){
				return true;
			}
			return false;
		}
		public bool GrabPreventsMovement(PhysicalObject o){
			if(!HasAttr(AttrType.GRABBED) || DistanceFrom(o) > 1 || HasAttr(AttrType.BRUTISH_STRENGTH) || HasAttr(AttrType.SLIMED) || HasAttr(AttrType.OIL_COVERED)){
				return false;
			}
			List<Actor> grabbers = new List<Actor>();
			foreach(Actor a in ActorsAtDistance(1)){
				if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this)){
					grabbers.Add(a);
				}
			}
			foreach(Actor a in grabbers){
				if(o.DistanceFrom(a) > 1){
					return true;
				}
			}
			return false;
		}
		public int InventoryCount(){
			int result = 0;
			foreach(Item i in inv){
				result += i.quantity;
			}
			return result;
		}
		public bool GetItem(Item i){
			if(InventoryCount() + i.quantity > Global.MAX_INVENTORY_SIZE){
				return false;
			}
			foreach(Item held in inv){
				if(held.type == i.type && !held.do_not_stack && !i.do_not_stack){
					held.quantity += i.quantity;
					return true;
				}
			}
			List<Item> new_inv = new List<Item>();
			bool added = false;
			foreach(Item held in inv){
				if(!added && i.SortOrderOfItemType() < held.SortOrderOfItemType()){
					new_inv.Add(i);
					added = true;
				}
				new_inv.Add(held);
			}
			if(!added){
				new_inv.Add(i);
			}
			inv = new_inv;
			if(this == player && !Item.identified[i.type]){
				Help.TutorialTip(TutorialTopic.FindingConsumables);
			}
			return true;
		}
		public bool HasAttr(AttrType attr){ return attrs[attr] > 0; }
		public bool HasAttr(params AttrType[] at){
			foreach(AttrType attr in at){
				if(attrs[attr] > 0){
					return true;
				}
			}
			return false;
		}
		public bool HasFeat(FeatType feat){ return feats[feat]; }
		public bool HasSpell(SpellType spell){ return spells[spell]; }
		public void GainAttr(AttrType attr,int duration){
			attrs[attr]++;
			Q.Add(new Event(this,duration,attr));
		}
		public void GainAttr(AttrType attr,int duration,int value){
			attrs[attr] += value;
			Q.Add(new Event(this,duration,attr,value));
		}
		public void GainAttr(AttrType attr,int duration,string msg,params PhysicalObject[] objs){
			attrs[attr]++;
			Q.Add(new Event(this,duration,attr,msg,objs));
		}
		public void GainAttr(AttrType attr,int duration,int value,string msg,params PhysicalObject[] objs){
			attrs[attr] += value;
			Q.Add(new Event(this,duration,attr,value,msg,objs));
		}
		public void GainAttrRefreshDuration(AttrType attr,int duration){
			attrs[attr]++;
			Event e = Q.FindAttrEvent(this,attr);
			if(e != null){
				if(e.TimeToExecute() < duration + Q.turn){ //if the new one would last longer than the old one, replace it.
					e.dead = true;
					Q.Add(new Event(this,duration,attr,attrs[attr]));
				}
				else{ //(if the old one still lasts longer, update it so it removes the new value)
					e.value = attrs[attr];
				}
			}
			else{
				Q.Add(new Event(this,duration,attr,attrs[attr]));
			}
		}
		public void GainAttrRefreshDuration(AttrType attr,int duration,string msg,params PhysicalObject[] objs){
			attrs[attr]++;
			Event e = Q.FindAttrEvent(this,attr);
			if(e != null){
				if(e.TimeToExecute() < duration + Q.turn){ //if the new one would last longer than the old one, replace it.
					e.dead = true;
					Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
				}
				else{ //(if the old one still lasts longer, update it so it removes the new value)
					e.value = attrs[attr];
				}
			}
			else{
				Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
			}
		}
		public void RefreshDuration(AttrType attr,int duration){ RefreshDuration(attr,duration,""); }
		public void RefreshDuration(AttrType attr,int duration,string msg,params PhysicalObject[] objs){
			if(duration == 0){
				if(attr == AttrType.BURNING && HasAttr(AttrType.BURNING)){
					if(light_radius == 0){
						UpdateRadius(1,0);
					}
					Fire.burning_objects.Remove(this);
				}
				int total_removed = 0;
				foreach(Event e in Q.list){
					if(!e.dead && e.target == this && e.attr == attr){
						total_removed += e.value;
						e.dead = true;
						if(e.msg != ""){
							B.Add(e.msg,e.msg_objs.ToArray());
						}
					}
				}
				attrs[attr] -= total_removed;
				if(attrs[attr] < 0){
					attrs[attr] = 0;
				}
				return;
			}
			Event ev = Q.FindAttrEvent(this,attr);
			if(ev != null){
				if(attrs[attr] == 0){
					attrs[attr]++;
				}
				if(ev.TimeToExecute() < duration + Q.turn){ //if the new one would3 last longer than the old one, replace it.
					ev.dead = true;
					Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
				} //(if the old one still lasts longer, do nothing)
			}
			else{
				if(attrs[attr] == 0){ //if this attr is already present but there's no attr for it, it must be permanent. in this case, do nothing.
					attrs[attr]++;
					Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
				}
			}
		}
		private string[] GetMessagesForStatus(AttrType attr){
			string status = attr.ToString().ToLower();
			/*if(attr == AttrType.MAGICAL_DROWSINESS){
				status = "drowsy";
			}*/
			//if statused is true, print "you are (status)ed" and "you are no longer (status)ed"
			bool statused = new List<AttrType>{AttrType.BLIND,AttrType.PARALYZED,AttrType.STUNNED,AttrType.POISONED,AttrType.SLOWED,AttrType.CONFUSED,AttrType.ENRAGED}.Contains(attr); //todo others?
			//otherwise, print "you become (status)" and "you feel less (status)"
			if(statused && status.Length >= 2){
				if(status.Substring(status.Length-2,2) != "ed"){
					if(status[status.Length-1] == 'e'){
						status = status + "d";
					}
					else{
						status = status + "ed";
					}
				}
			}
			string[] result = new string[3];
			if(statused){
				result[0] = YouAre() + " " + status + "! ";
				result[1] = YouAre() + " no longer " + status + ". ";
			}
			else{
				result[0] = You("become") + " " + status + "! ";
				result[1] = YouFeel() + " less " + status + ". ";
			}
			switch(attr){
			case AttrType.BLIND:
				result[0] = YouAre() + " blind! ";
				result[2] = Your() + " vision fades, but only for a moment. ";
				break;
			case AttrType.PARALYZED:
				result[2] = Your() + " muscles stiffen, but only for a moment. ";
				break;
			case AttrType.POISONED:
				result[2] = You("resist") + " the poison. ";
				//result[2] = YouFeel() + " sick, but only for a moment. ";
				break;
			case AttrType.SLOWED:
				result[2] = You("slow") + " down, but only for a moment. ";
				break;
			case AttrType.STUNNED:
				result[2] = You("resist") + " being stunned. ";
				break;
			case AttrType.VULNERABLE:
				result[2] = YouFeel() + " vulnerable, but only for a moment. ";
				break;
			case AttrType.SILENCED:
				result[2] = YouAre() + " silenced, but only for a moment. ";
				break;
			/*case AttrType.MAGICAL_DROWSINESS:
				result[2] = YouFeel() + " drowsy, but only for a moment. ";
				break;*/
			case AttrType.ASLEEP:
			{
				if(HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
					result[0] = You("become") + " dormant. ";
					result[1] = You("wake") + " from dormancy. ";
					result[2] = You("resist") + " becoming dormant. ";
				}
				else{
					result[0] = You("fall") + " asleep. "; //this isn't actually used; sleep uses a counter because monsters need to print the message a turn before they start acting again
					result[1] = You("wake") + " up. ";
					result[2] = You("resist") + " falling asleep. ";
				}
				break;
			}
			case AttrType.CONFUSED:
				result[2] = You("resist") + " being confused. ";
				break;
			default:
				result[2] = You("resist") + "! ";
				break;
			}
			return result;
		}
		private bool NoMessageOnRefresh(AttrType attr){ //todo: most gases go here
			switch(attr){
			case AttrType.POISONED:
			//case AttrType.MAGICAL_DROWSINESS: //todo: vulnerable too, or not?
			case AttrType.SILENCED:
			case AttrType.BLIND: //perhaps I need separate lists for monsters and the player
			case AttrType.CONFUSED:
			case AttrType.ASLEEP:
				return true;
			default:
				return false;
			}
		}
		private bool MentalEffect(AttrType attr){
			switch(attr){
			case AttrType.STUNNED:
			case AttrType.CONFUSED:
			//case AttrType.MAGICAL_DROWSINESS:
			case AttrType.ASLEEP:
			case AttrType.ENRAGED:
				return true;
			default:
				return false;
			}
		}
		public void ApplyStatus(AttrType attr,int duration){ ApplyStatus(attr,duration,"","",""); }
		public void ApplyStatus(AttrType attr,int duration,string message,string expiration_message,string resisted_message){
			ApplyStatus(attr,duration,true,message,expiration_message,resisted_message);
		}
		public void ApplyStatus(AttrType attr,int duration,bool use_default_messages,string message,string expiration_message,string resisted_message){
			string[] strings = GetMessagesForStatus(attr);
			if(use_default_messages){
				if(message == ""){
					message = strings[0];
				}
				if(expiration_message == ""){
					expiration_message = strings[1];
				}
				if(resisted_message == ""){
					resisted_message = strings[2];
				}
			}
			if(HasAttr(AttrType.MENTAL_IMMUNITY) && MentalEffect(attr)){
				//no message for now
			}
			else{
				if(R.PercentChance(TotalSkill(SkillType.SPIRIT)*8)){
					if(!(HasAttr(attr) && NoMessageOnRefresh(attr))){
						B.Add(resisted_message,this);
					}
				}
				else{
					if(!(HasAttr(attr) && NoMessageOnRefresh(attr))){
						B.Add(message,this);
					}
					RefreshDuration(attr,duration,expiration_message,this);
					if(this == player){
						switch(attr){
						case AttrType.CONFUSED:
							Help.TutorialTip(TutorialTopic.Confusion);
							break;
						case AttrType.STUNNED:
							Help.TutorialTip(TutorialTopic.Stunned);
							break;
						case AttrType.VULNERABLE:
							Help.TutorialTip(TutorialTopic.Vulnerable);
							break;
						case AttrType.SILENCED:
							Help.TutorialTip(TutorialTopic.Silenced);
							break;
						}
					}
				}
			}
		}
		public void DefineMagicSkillForMonster(int value){ //assumes this will happen only for prototypes
			skills[SkillType.MAGIC] = value;
			maxmp = skills[SkillType.MAGIC] * 5;
			curmp = maxmp;
		}
		public void GainSpell(params SpellType[] spell_list){
			foreach(SpellType spell in spell_list){
				spells[spell] = true;
			}
		}
		public Weapon EquippedWeapon{
			get{
				if(weapons != null && weapons.Count > 0){
					return weapons.First.Value;
				}
				return null;
			}
			set{
				if(weapons == null){
					weapons = new LinkedList<Weapon>();
				}
				if(weapons.Contains(value)){
					while(weapons.First.Value != value){
						Weapon temp = weapons.First.Value;
						weapons.Remove(temp);
						weapons.AddLast(temp);
					}
				}
				else{
					weapons.AddFirst(value);
				}
			}
		}
		public Armor EquippedArmor{
			get{
				if(armors != null && armors.Count > 0){
					return armors.First.Value;
				}
				return null;
			}
			set{
				if(armors == null){
					armors = new LinkedList<Armor>();
				}
				if(armors.Contains(value)){
					while(armors.First.Value != value){
						Armor temp = armors.First.Value;
						armors.Remove(temp);
						armors.AddLast(temp);
					}
				}
				else{
					armors.AddFirst(value);
				}
			}
		}
		private Weapon WeaponOfType(WeaponType w){
			if(weapons == null || weapons.Count == 0){
				return null;
			}
			LinkedListNode<Weapon> n = weapons.First;
			while(n.Value.type != w){
				if(n == weapons.Last){
					return null; //reached the end
				}
				else{
					n = n.Next;
				}
			}
			return n.Value;
		}
		private Armor ArmorOfType(ArmorType a){
			if(armors == null || armors.Count == 0){
				return null;
			}
			LinkedListNode<Armor> n = armors.First;
			while(n.Value.type != a){
				if(n == armors.Last){
					return null; //reached the end
				}
				else{
					n = n.Next;
				}
			}
			return n.Value;
		}
		public Weapon Sword{get{return WeaponOfType(WeaponType.SWORD);}}
		public Weapon Mace{get{return WeaponOfType(WeaponType.MACE);}}
		public Weapon Dagger{get{return WeaponOfType(WeaponType.DAGGER);}}
		public Weapon Staff{get{return WeaponOfType(WeaponType.STAFF);}}
		public Weapon Bow{get{return WeaponOfType(WeaponType.BOW);}}
		public Armor Leather{get{return ArmorOfType(ArmorType.LEATHER);}}
		public Armor Chainmail{get{return ArmorOfType(ArmorType.CHAINMAIL);}}
		public Armor Plate{get{return ArmorOfType(ArmorType.FULL_PLATE);}}
		public int Speed(){
			int bloodboil = attrs[AttrType.BLOOD_BOILED]*10;
			int vigor = HasAttr(AttrType.VIGOR)? 50 : 0;
			int leap = HasAttr(AttrType.FLYING_LEAP)? 50 : 0;
			int haste = Math.Max(bloodboil,Math.Max(vigor,leap)); //only the biggest applies
			if(HasAttr(AttrType.SLOWED)){
				return (speed - haste) * 2;
			}
			else{
				return speed - haste;
			}
		}
		public int LightRadius(){ return Math.Max(light_radius + attrs[AttrType.SHINING]*2 - attrs[AttrType.DIM_LIGHT],attrs[AttrType.BURNING]); }
		public int TotalProtectionFromArmor(){
			if(HasAttr(AttrType.SWITCHING_ARMOR)){
				return 0;
			}
			int effective_exhaustion = exhaustion;
			if(HasFeat(FeatType.ARMOR_MASTERY)){
				effective_exhaustion -= 25;
			}
			switch(EquippedArmor.type){
			case ArmorType.LEATHER:
				if(effective_exhaustion >= 75){
					return 0;
				}
				break;
			case ArmorType.CHAINMAIL:
				if(effective_exhaustion >= 50){
					return 0;
				}
				break;
			case ArmorType.FULL_PLATE:
				if(effective_exhaustion >= 25){
					return 0;
				}
				break;
			}
			return EquippedArmor.Protection();
		}
		public int TotalSkill(SkillType skill){
			int result = skills[skill];
			switch(skill){
			case SkillType.COMBAT:
				result += attrs[AttrType.BONUS_COMBAT];
				break;
			case SkillType.DEFENSE:
				result += attrs[AttrType.BONUS_DEFENSE];
				result += TotalProtectionFromArmor();
				break;
			case SkillType.MAGIC:
				result += attrs[AttrType.BONUS_MAGIC];
				break;
			case SkillType.SPIRIT:
				result += attrs[AttrType.BONUS_SPIRIT];
				break;
			case SkillType.STEALTH:
				result += attrs[AttrType.BONUS_STEALTH];
				if((LightRadius() > 0 && !M.wiz_lite && !M.wiz_dark) || (EquippedArmor.type == ArmorType.FULL_PLATE && tile().light_value > 0)){
					return 0;
				}
				if(!tile().IsLit()){
					if(type == ActorType.PLAYER || !player.HasAttr(AttrType.SHADOWSIGHT)){ //+2 stealth while in darkness unless shadowsight is in effect
						result += 2;
					}
				}
				result -= EquippedArmor.StealthPenalty();
				break;
			}
			return result;
		}
		public string WoundStatus(){
			if(type == ActorType.DREAM_WARRIOR_CLONE){
				if(group != null && group.Count > 0){
					foreach(Actor a in group){
						if(a.type == ActorType.DREAM_WARRIOR){
							return a.WoundStatus();
						}
					}
				}
			}
			if(type == ActorType.DREAM_SPRITE_CLONE){
				if(group != null && group.Count > 0){
					foreach(Actor a in group){
						if(a.type == ActorType.DREAM_SPRITE){
							return a.WoundStatus();
						}
					}
				}
			}
			string awareness = ", unaware)";
			if(player_visibility_duration < 0){
				awareness = ", alerted)";
			}
			int percentage = (curhp * 100) / maxhp;
			if(percentage >= 100){
				return "(unhurt" + awareness;
			}
			else{
				if(percentage > 90){
					return "(scratched" + awareness;
				}
				else{
					if(percentage > 70){
						return "(slightly damaged" + awareness;
					}
					else{
						if(percentage > 50){
							return "(somewhat damaged" + awareness;
						}
						else{
							if(percentage > 30){
								return "(heavily damaged" + awareness;
							}
							else{
								if(percentage > 10){
									return "(extremely damaged" + awareness;
								}
								else{
									if(HasAttr(AttrType.NONLIVING)){
										return "(almost destroyed" + awareness;
									}
									else{
										return "(almost dead" + awareness;
									}
								}
							}
						}
					}
				}
			}
		}
		public void ApplyBurning(){
			if(!HasAttr(AttrType.IMMUNE_BURNING,AttrType.SLIMED) && !tile().Is(FeatureType.POISON_GAS,FeatureType.THICK_DUST)){
				if(!HasAttr(AttrType.BURNING)){
					if(LightRadius() == 0){
						UpdateRadius(0,1);
					}
					attrs[AttrType.BURNING] = 1;
					attrs[AttrType.OIL_COVERED] = 0;
					Fire.AddBurningObject(this);
					tile().ApplyEffect(DamageType.FIRE);
				}
				Q.KillEvents(this,AttrType.BURNING);
				Q.Add(new Event(this,R.Between(7,10) * 100,AttrType.BURNING,YouAre() + " no longer burning. ",this));
			}
		}
		public void ApplyFreezing(){
			if(!IsBurning()){
				attrs[AttrType.FROZEN] = 35;
				attrs[AttrType.SLIMED] = 0;
				attrs[AttrType.OIL_COVERED] = 0;
				attrs[AttrType.GRABBED] = 0;
				B.Add(YouAre() + " encased in ice. ",this);
				if(this == player){
					Help.TutorialTip(TutorialTopic.Frozen);
				}
			}
		}
		public bool ResistedBySpirit(){
			return R.PercentChance(TotalSkill(SkillType.SPIRIT)*8);
		}
		/*public int DurationOfMagicalEffect(int original){ //intended to be used with whole turns, not "ticks"
			int diff = (original * TotalSkill(SkillType.SPIRIT)) / 20; //each point of Spirit takes off 1/20th of the duration
			int result = original - diff; //therefore, maxed Spirit cuts durations in half
			if(result < 1){
				result = 1; //no negative turncounts please
			}
			return result;
		}*/
		public bool CanWanderAtLevelGen(){
			if(NeverWanders()){
				return false;
			}
			switch(type){
			case ActorType.SKELETON:
			case ActorType.STONE_GOLEM:
			case ActorType.MACHINE_OF_WAR:
				return false; //just a bit of flavor for these monsters, I suppose
			default:
				return true;
			}
		}
		public bool NeverWanders(){
			switch(type){
			case ActorType.GIANT_BAT:
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.BLOOD_MOTH:
			case ActorType.SPORE_POD:
			case ActorType.MIMIC:
			case ActorType.PHASE_SPIDER:
			case ActorType.POLTERGEIST:
			case ActorType.MARBLE_HORROR:
			case ActorType.NOXIOUS_WORM:
			case ActorType.LASHER_FUNGUS:
			case ActorType.MUD_TENTACLE:
			case ActorType.IMPOSSIBLE_NIGHTMARE:
			case ActorType.PLAYER:
			case ActorType.FIRE_DRAKE:
			case ActorType.FINAL_LEVEL_CULTIST:
				return true;
			default:
				return false;
			}
		}
		public bool AlwaysWanders(){
			switch(type){
			case ActorType.LONE_WOLF:
			case ActorType.SPITTING_COBRA:
			case ActorType.GOLDEN_DART_FROG: //consider a new wandering mode for animals - one that doesn't take them so far from where they started.
			case ActorType.WILD_BOAR:
			case ActorType.KOBOLD:
			case ActorType.SKULKING_KILLER:
			case ActorType.ORC_ASSASSIN:
			case ActorType.SNEAK_THIEF:
			case ActorType.ENTRANCER:
				return true;
			default:
				return false;
			}
		}
		/*public static int Rarity(ActorType type){
			int result = 1;
			if(((int)type)%3 == 2){
				result = 2;
			}
			if(type == ActorType.PLAYER || type == ActorType.FIRE_DRAKE
			|| type == ActorType.SPECIAL || type == ActorType.DREAM_WARRIOR_CLONE){
				return 0;
			}
			return result;
		}*/
		/*public void UpdateRadius(int from,int to){ UpdateRadius(from,to,false); }
		public void UpdateRadius(int from,int to,bool change){
			if(from > 0){
				for(int i=row-from;i<=row+from;++i){
					for(int j=col-from;j<=col+from;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(!M.tile[i,j].opaque && (HasBresenhamLine(i,j) || M.tile[i,j].HasBresenhamLine(row,col))){
								M.tile[i,j].light_value--;
							}
						}
					}
				}
			}
			if(to > 0){
				for(int i=row-to;i<=row+to;++i){
					for(int j=col-to;j<=col+to;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(!M.tile[i,j].opaque && (HasBresenhamLine(i,j) || M.tile[i,j].HasBresenhamLine(row,col))){
								M.tile[i,j].light_value++;
							}
						}
					}
				}
			}
			if(change){
				light_radius = to;
			}
		}*/
		public void RemoveTarget(Actor a){
			if(target == a){
				target = null;
			}
		}
		public void Q0(){ //add movement event to queue, zero turns
			Q.Add(new Event(this,0));
		}
		public void Q1(){ //one turn
			Q.Add(new Event(this,100));
		}
		public void QS(){ //equal to speed
			Q.Add(new Event(this,Speed()));
		}
		public override string ToString(){ return symbol.ToString(); }
		public void Input(){
			bool skip_input = false;
			pos old_position = p;
			if(HasAttr(AttrType.DESTROYED_BY_SUNLIGHT) && M.wiz_lite){
				B.Add(You("turn") + " to dust! ",this);
				Kill();
				return;
			}
			if(type == ActorType.LUMINOUS_AVENGER && M.wiz_dark){
				B.Add(YouAre() + " consumed by the magical darkness! ",this);
				Kill();
				return;
			}
			if(HasAttr(AttrType.LIFESPAN)){
				attrs[AttrType.LIFESPAN]--;
				if(!HasAttr(AttrType.LIFESPAN)){
					Kill();
					return;
				}
			}
			if(tile().type == TileType.FIRE_RIFT && !HasAttr(AttrType.FLYING)){
				if(this == player){
					B.Add("The maw of the Demon King opens beneath you. ");
					B.PrintAll();
					B.Add("You fall... ");
					B.PrintAll();
					B.Add("Fortunately, you burn to death before you reach the bottom. ");
					Kill();
					curhp = 0;
					return;
				}
				else{
					B.Add(the_name + " plunges into the fire rift. ",this);
					Kill();
					return;
				}
			}
			if(HasAttr(AttrType.AGGRAVATING)){ //this probably wouldn't work well for anyone but the player, yet.
				foreach(Actor a in ActorsWithinDistance(12)){
					a.player_visibility_duration = -1; //todo: is this conceptually different than just making lots of noise?
					a.attrs[AttrType.PLAYER_NOTICED] = 1; //todo: if not, maybe change this.
					if(a.HasLOS(this)){
						a.target_location = tile();
					}
					else{
						a.FindPath(this);
					}
				}
			}
			if(HasAttr(AttrType.IN_COMBAT)){
				attrs[AttrType.IN_COMBAT] = 0;
				if(HasFeat(FeatType.CONVICTION)){
					GainAttrRefreshDuration(AttrType.CONVICTION,Math.Max(Speed(),100));
					attrs[AttrType.BONUS_SPIRIT]++;
					if(attrs[AttrType.CONVICTION] % 2 == 1){
						attrs[AttrType.BONUS_COMBAT]++;
					}
				}
			}
			/*else{
				if(HasAttr(AttrType.MAGICAL_DROWSINESS) && !HasAttr(AttrType.ASLEEP) && R.OneIn(4) && time_of_last_action < Q.turn){
					if(ResistedBySpirit()){
						if(player.HasLOS(this)){
							if(HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
								B.Add(You("resist") + " becoming dormant. ",this);
							}
							else{
								B.Add(You("almost fall") + " asleep. ",this);
							}
						}
					}
					else{
						if(player.HasLOS(this)){
							if(HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
								B.Add(You("become") + " dormant. ",this);
							}
							else{
								B.Add(You("fall") + " asleep. ",this);
							}
						}
						attrs[AttrType.ASLEEP] = 4 + R.Roll(2);
					}
				}
			}*/
			if(HasAttr(AttrType.TELEPORTING) && time_of_last_action < Q.turn){
				attrs[AttrType.TELEPORTING]--;
				if(!HasAttr(AttrType.TELEPORTING)){
					if(!HasAttr(AttrType.IMMOBILE)){
						for(int i=0;i<9999;++i){
							int rr = R.Roll(1,Global.ROWS-2);
							int rc = R.Roll(1,Global.COLS-2);
							if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null){
								if(type == ActorType.PLAYER){
									B.Add("You are suddenly somewhere else. ");
									Interrupt();
									Move(rr,rc);
								}
								else{
									bool seen = false;
									if(player.CanSee(this)){
										seen = true;
									}
									if(player.CanSee(tile())){
										B.Add(the_name + " suddenly disappears. ",this);
									}
									Move(rr,rc);
									if(player.CanSee(tile())){
										if(seen){
											B.Add(the_name + " reappears. ",this);
										}
										else{
											B.Add(a_name + " suddenly appears! ",this);
										}
									}
								}
								break;
							}
						}
					}
					attrs[AttrType.TELEPORTING] = R.Roll(2,10) + 5;
				}
			}
			attrs[AttrType.JUST_AWOKE] = 0;
			if(HasAttr(AttrType.ASLEEP)){
				attrs[AttrType.ASLEEP]--;
				if(this == player){
					Global.FlushInput();
				}
				if(!HasAttr(AttrType.ASLEEP)){
					if(player.HasLOS(this)){
						if(HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
							B.Add(You("wake") + " from dormancy. ",this);
						}
						else{
							B.Add(You("wake") + " up. ",this);
						}
					}
				}
				if(type != ActorType.PLAYER){
					attrs[AttrType.JUST_AWOKE] = 1;
					if(!skip_input){
						Q1();
						skip_input = true;
					}
				}
			}
			if(HasAttr(AttrType.PARALYZED)){
				attrs[AttrType.PARALYZED]--;
				if(type == ActorType.PLAYER){
					//B.AddDependingOnLastPartialMessage("You can't move! ");
					if(!HasAttr(AttrType.PARALYZED)){
						B.Add("You can move again. ");
					}
				}
				else{ //handled differently for the player: since the map still needs to be drawn,
					if(HasAttr(AttrType.PARALYZED)){
						if(attrs[AttrType.PARALYZED] == 1){
							B.Add(the_name + " can move again. ",this);
						}
						/*else{
							B.Add(the_name + " can't move! ",this);
						}*/
						if(!skip_input){
							Q1();						// this is handled in InputHuman().
							skip_input = true; //the message is still printed, of course.
						}
					}
				}
			}
			if(HasAttr(AttrType.AMNESIA_STUN)){
				attrs[AttrType.AMNESIA_STUN]--;
				if(!skip_input){
					Q1();
					skip_input = true;
				}
			}
			if(type == ActorType.KOBOLD && HasAttr(AttrType.COOLDOWN_2) && !skip_input){
				skip_input = true;
				Q1();
			}
			if(HasAttr(AttrType.FROZEN) && this != player && !skip_input){ //todo: in the future, noneuclidean monsters will slip out of ice
				if(type == ActorType.GIANT_SLUG){
					if(curhp > 10){
						B.Add("The cold devastates the giant slug. ",this);
						curhp -= 10;
					}
					else{
						B.Add("The cold kills the giant slug. ",this);
						Kill();
						return;
					}
				}
				int damage = R.Roll(attack[type].WhereGreatest(x=>x.damage.dice)[0].damage.dice,6) + TotalSkill(SkillType.COMBAT);
				if(damage > 0){ //anything with no damaging attacks is trapped in the ice until something else removes it
					attrs[AttrType.FROZEN] -= damage;
					if(attrs[AttrType.FROZEN] < 0 || HasAttr(AttrType.BRUTISH_STRENGTH)){
						attrs[AttrType.FROZEN] = 0;
					}
					if(HasAttr(AttrType.FROZEN)){
						B.Add(the_name + " tries to break free. ",this);
					}
					else{
						B.Add(the_name + " breaks free! ",this);
					}
					if(!HasAttr(AttrType.BRUTISH_STRENGTH)){
						IncreaseExhaustion(1);
					}
				}
				if(!HasAttr(AttrType.BRUTISH_STRENGTH)){
					Q1();
					skip_input = true;
				}
			}
			if(curhp < maxhp - attrs[AttrType.PERMANENT_DAMAGE] && !HasAttr(AttrType.NONLIVING)){
				if(HasAttr(AttrType.REGENERATING) && time_of_last_action < Q.turn){
					int recovered = attrs[AttrType.REGENERATING];
					if(curhp + recovered > maxhp - attrs[AttrType.PERMANENT_DAMAGE]){
						recovered = (maxhp - attrs[AttrType.PERMANENT_DAMAGE]) - curhp;
					}
					curhp += recovered;
					if(curhp > maxhp){
						curhp = maxhp;
					}
					if(player.HasLOS(this)){
						B.Add(You("regenerate") + ". ",this);
					}
					if(type == ActorType.TROLL_BLOODWITCH){
						List<pos> cells = new List<pos>();
						List<colorchar> cch = new List<colorchar>();
						foreach(pos p2 in PositionsWithinDistance(4)){
							if(M.tile[p2].passable && HasLOE(M.tile[p2]) && player.CanSee(M.tile[p2])){
								cells.Add(p2);
								colorchar ch = M.VisibleColorChar(p2.row,p2.col);
								ch.color = Color.Red;
								cch.Add(ch);
							}
						}
						if(cells.Count > 0){
							M.Draw();
							Screen.AnimateMapCells(cells,cch,40);
						}
						foreach(Actor a in ActorsWithinDistance(4,true)){
							if(HasLOE(a)){
								if(a == player){
									B.Add("Ow! ");
								}
								a.TakeDamage(DamageType.NORMAL,DamageClass.MAGICAL,recovered,this,"trollish blood magic");
							}
						}
					}
				}
				else{
					bool recover = false;
					if(HasFeat(FeatType.ENDURING_SOUL) && curhp % 10 != 0){
						recover = true;
					}
					if(HasAttr(AttrType.BANDAGED)){
						recover = true;
					}
					if(recover && recover_time <= Q.turn){
						curhp++;
						recover_time = Q.turn + 500;
						if(HasAttr(AttrType.BANDAGED)){
							attrs[AttrType.BANDAGED]--;
							recover_time = Q.turn + 100;
						}
					}
				}
			}
			if(tile().Is(FeatureType.THICK_DUST) && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.PLANTLIKE,AttrType.BLINDSIGHT)){
					if(this == player && !HasAttr(AttrType.ASLEEP,AttrType.PARALYZED)){
						B.Add("Dust fills the air here! ");
					}
					ApplyStatus(AttrType.BLIND,R.Between(1,3)*100);
				}
			}
			if(tile().Is(FeatureType.POISON_GAS) && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.NONLIVING) && !HasAttr(AttrType.PLANTLIKE) && type != ActorType.NOXIOUS_WORM){
					if(!HasAttr(AttrType.POISONED) && this == player){
						B.Add("Poisonous fumes fill your lungs! ");
					}
					ApplyStatus(AttrType.POISONED,300);
				}
			}
			if(tile().Is(FeatureType.SPORES) && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE,AttrType.SPORE_BURST)){
					if(ResistedBySpirit()){
						B.Add(You("resist") + " the effect of the spores. ",this);
					}
					else{
						int duration = R.Between(7,11)*100;
						if(this == player){
							if(!HasAttr(AttrType.POISONED,AttrType.STUNNED)){
								B.Add("Choking spores fill your lungs! ");
								B.Add("You are stunned and poisoned. ");
							}
							RefreshDuration(AttrType.POISONED,duration,"The effect of the spores wears off. ");
							if(!HasAttr(AttrType.MENTAL_IMMUNITY)){
								RefreshDuration(AttrType.STUNNED,duration);
							}
							Help.TutorialTip(TutorialTopic.Stunned);
						}
						else{
							RefreshDuration(AttrType.POISONED,duration);
							if(!HasAttr(AttrType.MENTAL_IMMUNITY)){
								RefreshDuration(AttrType.STUNNED,duration);
							}
						}
					}
				}
			}
			if(tile().Is(FeatureType.CONFUSION_GAS) && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE,AttrType.MENTAL_IMMUNITY)){
					ApplyStatus(AttrType.CONFUSED,R.Between(5,8)*100);
				}
			}
			if(HasAttr(AttrType.POISONED) && time_of_last_action < Q.turn){
				if(!TakeDamage(DamageType.POISON,DamageClass.NO_TYPE,false,R.Roll(3)-1,null,"*succumbed to poison")){
					return;
				}
			}
			if(HasAttr(AttrType.BURNING) && time_of_last_action < Q.turn){
				if(player.HasLOS(this)){
					B.Add(YouAre() + " on fire! ",this);
				}
				int damage = R.Between(2,3);
				if(magic_trinkets.Contains(MagicTrinketType.RING_OF_THE_LETHARGIC_FLAME)){
					damage = 1;
				}
				if(!TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,false,damage,null,"*burned to death")){
					return;
				}
				if(this == player){
					Help.TutorialTip(TutorialTopic.Fire);
				}
			}
			if(HasAttr(AttrType.ACIDIFIED) && time_of_last_action < Q.turn){
				if(player.HasLOS(this)){
					B.Add("The acid burns " + the_name + ". ",this);
				}
				if(!TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,false,R.Between(2,3),this,"*dissolved by acid")){
					return;
				}
			}
			if(HasAttr(AttrType.BLEEDING) && time_of_last_action < Q.turn){
				attrs[AttrType.BLEEDING]--;
				if(!HasAttr(AttrType.BANDAGED,AttrType.NONLIVING,AttrType.SHIELDED)){
					if(this == player){
						B.Add("You're bleeding! ");
					}
					else{
						B.Add(the_name + " is bleeding! ",this);
					}
					if(type == ActorType.HOMUNCULUS){
						if(R.CoinFlip()){
							tile().AddFeature(FeatureType.OIL);
						}
					}
					else{
						if(tile().symbol == '.' && tile().color == Color.White && R.CoinFlip()){
							tile().color = BloodColor();
						}
					}
					int amount = (maxhp + 49) / 50; //50 turns of bleeding is lethal - if you have 100hp, bleeding deals 2 damage. if you have 101hp, bleeding deals 3.
					if(!TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,false,amount,this,"*bled out")){
						return;
					}
				}
			}
			if(EquippedArmor != null && EquippedArmor.status[EquipmentStatus.INFESTED] && !HasAttr(AttrType.RESTING) && R.CoinFlip() && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.JUST_BITTEN)){
					B.Add("From within your " + EquippedArmor.NameWithoutEnchantment() + " you feel dozens of insect bites! ");
				}
				else{
					B.Add("Dozens of insects bite you! ");
				}
				if(!TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,false,1,null,"an insect infestation")){
					return;
				}
				else{
					RefreshDuration(AttrType.JUST_BITTEN,2000);
				}
			}
			if(tile().Is(FeatureType.PIXIE_DUST) && time_of_last_action < Q.turn){
				if(maxmp > 0){
					if(!HasAttr(AttrType.EMPOWERED_SPELLS)){
						B.Add("The pixie dust empowers " + the_name + ". ",this);
					}
					if(curmp < maxmp){
						curmp++;
					}
					RefreshDuration(AttrType.EMPOWERED_SPELLS,R.Between(4,7)*100,Your() + " spells are no longer empowered. ",this);
				}
				else{
					if(!HasAttr(AttrType.EMPOWERED_SPELLS) && this == player){
						B.Add("The pixie dust makes your skin tingle. ");
					}
					RefreshDuration(AttrType.EMPOWERED_SPELLS,R.Between(4,7)*100);
				}
			}
			if(HasAttr(AttrType.LIGHT_SENSITIVE) && ((tile().IsLit() && tile().light_value > 0) || M.wiz_lite) && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.VULNERABLE)){ //no Spirit resistance here because it's from a potion, and because it would lead to a lot of message spam.
					B.Add("The light weakens " + the_name + ". ",this);
				}
				if(type == ActorType.PLAYER){
					RefreshDuration(AttrType.VULNERABLE,R.Between(5,9) * 100,"You shake off the memory of the harsh light. ");
					Help.TutorialTip(TutorialTopic.Vulnerable);
				}
				else{
					RefreshDuration(AttrType.VULNERABLE,R.Between(5,9)*100);
				}
			}
			if(HasAttr(AttrType.DESCENDING) && time_of_last_action < Q.turn){
				attrs[AttrType.DESCENDING]--;
				if(!HasAttr(AttrType.DESCENDING)){
					attrs[AttrType.FLYING] = 0;
					if(tile().IsTrap()){
						tile().TriggerTrap();
					}
				}
			}
			if(this == player && EquippedArmor == Plate && time_of_last_action < Q.turn){
				if(HasAttr(AttrType.NO_PLATE_ARMOR_NOISE)){
					attrs[AttrType.NO_PLATE_ARMOR_NOISE] = 0;
				}
				else{
					MakeNoise(3);
				}
			}
			if(!skip_input){
				if(type==ActorType.PLAYER){
					InputHuman();
					MouseUI.IgnoreMouseMovement = true;
				}
				else{
					InputAI();
				}
			}
			if(curhp <= 0 && type != ActorType.BERSERKER){
				Q.KillEvents(this,EventType.ANY_EVENT); //hack to prevent unkillable monsters when traps kill them
				M.RemoveTargets(this);
				return;
			}
			if(HasAttr(AttrType.STEALTHY) && !HasAttr(AttrType.BURROWING)){ //monsters only
				if((player.IsWithinSightRangeOf(row,col) || M.tile[row,col].IsLit()) && player.HasLOS(row,col) && !IsInvisibleHere()){
					if(IsHiddenFrom(player)){  //if they're stealthed and near the player...
						if(TotalSkill(SkillType.STEALTH) * DistanceFrom(player) * 10 - attrs[AttrType.TURNS_VISIBLE]++*5 < R.Roll(1,100)){
							attrs[AttrType.TURNS_VISIBLE] = -1;
							if(DistanceFrom(player) > 3){
								B.Add("You notice " + a_name + ". ");
							}
							else{
								B.Add("You notice " + a_name + " nearby. ");
							}
						}
					}
					else{
						attrs[AttrType.TURNS_VISIBLE] = -1;
					}
				}
				else{
					if(attrs[AttrType.TURNS_VISIBLE] >= 0){ //if they hadn't been seen yet...
						attrs[AttrType.TURNS_VISIBLE] = 0;
					}
					else{
						if(attrs[AttrType.TURNS_VISIBLE]-- == -10){ //check this value for balance
							attrs[AttrType.TURNS_VISIBLE] = 0;
						}
					}
				}
			}
			if(!HasAttr(AttrType.BURROWING) && tile().IsBurning() && time_of_last_action < Q.turn){
				ApplyBurning();
			}
			/*if(HasAttr(AttrType.ARCANE_SHIELDED) && time_of_last_action < Q.turn){
				attrs[AttrType.ARCANE_SHIELDED]--;
				if(!HasAttr(AttrType.ARCANE_SHIELDED) && !HasAttr(AttrType.BURROWING)){
					B.Add(Your() + " shield fades. ",this);
				}
			}*/
			if(old_position.row == row && old_position.col == col && time_of_last_action < Q.turn && !HasAttr(AttrType.BURROWING)){
				if(Q.turn - time_of_last_action != 50){ //don't update if the last action was a 50-speed move
					attrs[AttrType.TURNS_HERE]++;
				}
			}
			else{
				attrs[AttrType.TURNS_HERE] = 0;
			}
			time_of_last_action = Q.turn;
			if(Global.SAVING){
				Global.SaveGame(B,M,Q);
			}
		}
		public static char ConvertInput(ConsoleKeyInfo k){
			switch(k.Key){
			case ConsoleKey.UpArrow: //notes: the existing design necessitated that I choose characters to assign to the toprow numbers.
			case ConsoleKey.NumPad8: //Not being able to think of anything better, I went with '!' through ')' ...
				return '8';
			case ConsoleKey.D8: // (perhaps I'll redesign if needed)
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '8';
				}
				return '*';
			case ConsoleKey.DownArrow:
			case ConsoleKey.NumPad2:
				return '2';
			case ConsoleKey.D2:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '2';
				}
				return '@';
			case ConsoleKey.LeftArrow:
			case ConsoleKey.NumPad4:
				return '4';
			case ConsoleKey.D4:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '4';
				}
				return '$';
			case ConsoleKey.Clear:
			case ConsoleKey.NumPad5:
				return '5';
			case ConsoleKey.D5:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '5';
				}
				return '%';
			case ConsoleKey.RightArrow:
			case ConsoleKey.NumPad6:
				return '6';
			case ConsoleKey.D6:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '6';
				}
				return '^';
			case ConsoleKey.Home:
			case ConsoleKey.NumPad7:
				return '7';
			case ConsoleKey.D7:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '7';
				}
				return '&';
			case ConsoleKey.PageUp:
			case ConsoleKey.NumPad9:
				return '9';
			case ConsoleKey.D9:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '9';
				}
				return '(';
			case ConsoleKey.End:
			case ConsoleKey.NumPad1:
				return '1';
			case ConsoleKey.D1:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '1';
				}
				return '!';
			case ConsoleKey.PageDown:
			case ConsoleKey.NumPad3:
				return '3';
			case ConsoleKey.D3:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return '3';
				}
				return '#';
			case ConsoleKey.D0:
				if(Global.Option(OptionType.TOP_ROW_MOVEMENT)){
					return ' ';
				}
				return ')';
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
		public static char ConvertVIKeys(char ch){
			switch(ch){
			case 'h':
			case 'H':
				return '4';
			case 'j':
			case 'J':
				return '2';
			case 'k':
			case 'K':
				return '8';
			case 'l':
			case 'L':
				return '6';
			case 'y':
			case 'Y':
				return '7';
			case 'u':
			case 'U':
				return '9';
			case 'b':
			case 'B':
				return '1';
			case 'n':
			case 'N':
				return '3';
			default:
				return ch;
			}
		}
		public void InputHuman(){
			if(HasAttr(AttrType.DETECTING_MOVEMENT) && footsteps.Count > 0 && time_of_last_action < Q.turn){
				Screen.CursorVisible = false;
				Screen.AnimateMapCells(footsteps,new colorchar('!',Color.Red));
				previous_footsteps = footsteps;
				footsteps = new List<pos>();
			}
			if(HasAttr(AttrType.SWITCHING_ARMOR)){
				attrs[AttrType.SWITCHING_ARMOR]--;
			}
			DisplayStats(true);
			if(HasFeat(FeatType.DANGER_SENSE)){
				M.UpdateDangerValues();
			}
			Screen.UpdateScreenCenterColumn(col);
			M.Draw();
			if(HasAttr(AttrType.AUTOEXPLORE)){
				if(path.Count == 0){ //todo: autoexplore could also track whether the current path is leading to an unexplored tile instead of to an item/shrine/etc.
					if(!FindAutoexplorePath()){ // - in this case I could check that tile's neighbors each turn, and calculate a new path early if they've all been mapped now.
						B.Add("You don't see a path for further exploration. ");
					}
				}
			}
			if(!HasAttr(AttrType.PARALYZED) && !HasAttr(AttrType.ASLEEP)){
				B.Print(false);
			}
			else{
				B.DisplayNow();
			}
			Cursor();
			Screen.CursorVisible = true;
			if(HasAttr(AttrType.PARALYZED,AttrType.ASLEEP)){
				if(HasAttr(AttrType.ASLEEP)){
					Thread.Sleep(25);
				}
				Q1();
				return;
			}
			if(HasAttr(AttrType.ENRAGED) && !HasAttr(AttrType.FROZEN)){
				Thread.Sleep(100);
				EnragedMove();
				return;
			}
			if(Global.Option(OptionType.AUTOPICKUP) && ((tile().inv != null && !tile().inv.ignored && InventoryCount() < Global.MAX_INVENTORY_SIZE) || tile().type == TileType.CHEST)){
				if(!NextStepIsDangerous(tile())){
					if(StunnedThisTurn()){
						return;
					}
					if(tile().type == TileType.CHEST){
						tile().OpenChest();
						Q1();
					}
					else{
						if(InventoryCount() + tile().inv.quantity <= Global.MAX_INVENTORY_SIZE){
							Item i = tile().inv;
							tile().inv = null;
							if(i.light_radius > 0){
								i.UpdateRadius(i.light_radius,0);
							}
							i.row = -1;
							i.col = -1;
							B.Add("You pick up " + i.TheName() + ". ");
							GetItem(i);
							Q1();
						}
						else{
							int space_left = Global.MAX_INVENTORY_SIZE - InventoryCount();
							Item i = tile().inv;
							Item newitem = new Item(i,row,col);
							newitem.quantity = space_left;
							i.quantity -= space_left;
							B.Add("You pick up " + newitem.TheName() + ", but have no room for the other " + i.quantity.ToString() + ". ");
							GetItem(newitem);
							Q1();
						}
					}
					return;
				}
			}
			if(path.Count > 0){
				if(!NextStepIsDangerous(M.tile[path[0]])){
					if(Global.KeyIsAvailable()){
						ConsoleKeyInfo key = Global.ReadKey();
						if(ConvertInput(key) == 'x' && HasAttr(AttrType.AUTOEXPLORE)){
							PlayerWalk(DirectionOf(path[0]));
							if(path.Count > 0){
								if(DistanceFrom(path[0]) == 0){
									path.RemoveAt(0);
								}
							}
							return;
						}
						else{
							Interrupt();
						}
					}
					else{
						PlayerWalk(DirectionOf(path[0]));
						if(path.Count > 0){
							if(DistanceFrom(path[0]) == 0){
								path.RemoveAt(0);
							}
						}
						return;
					}
				}
				else{
					Interrupt();
				}
			}
			if(HasAttr(AttrType.RUNNING)){
				Tile next = TileInDirection(attrs[AttrType.RUNNING]);
				if(!NextStepIsDangerous(next) && !Global.KeyIsAvailable()){
					if(attrs[AttrType.RUNNING] == 5){
						bool recover = false;
						if(!HasAttr(AttrType.NONLIVING)){
							if(HasFeat(FeatType.ENDURING_SOUL) && curhp % 10 != 0){
								recover = true;
							}
							if(HasAttr(AttrType.BANDAGED)){
								recover = true;
							}
						}
						if(!recover){
							if(HasAttr(AttrType.WAITING)){
								attrs[AttrType.WAITING]--;
								Q1();
								return;
							}
							else{
								attrs[AttrType.RUNNING] = 0;
							}
						}
						else{
							Q1();
							return;
						}
					}
					else{
						bool corridor = true;
						foreach(int dir in U.FourDirections){
							if(TileInDirection(dir).passable && TileInDirection(dir.RotateDir(true,1)).passable && TileInDirection(dir.RotateDir(true,2)).passable){
								corridor = false;
								break;
							}
						}
						List<Tile> tiles = new List<Tile>();
						if(corridor){
							List<int> blocked = new List<int>();
							for(int i=-1;i<=1;++i){
								blocked.Add(attrs[AttrType.RUNNING].RotateDir(true,4+i));
							}
							tiles = TilesAtDistance(1).Where(x=>(x.passable || x.Is(TileType.DOOR_C,TileType.RUBBLE)) && ApproximateEuclideanDistanceFromX10(x) == 10 && !blocked.Contains(DirectionOf(x)));
						}
						if(!corridor && next.passable){
							PlayerWalk(attrs[AttrType.RUNNING]);
							return;
						}
						else{
							if(corridor && tiles.Count == 1){
								attrs[AttrType.RUNNING] = DirectionOf(tiles[0]);
								PlayerWalk(attrs[AttrType.RUNNING]);
								foreach(int dir in U.FourDirections){ //now check again to see whether the player has entered a room
									if(TileInDirection(dir).passable && TileInDirection(dir.RotateDir(true,1)).passable && TileInDirection(dir.RotateDir(true,2)).passable){
										corridor = false;
										break;
									}
								}
								if(!corridor){
									attrs[AttrType.RUNNING] = 0;
									attrs[AttrType.WAITING] = 0;
								}
								return;
							}
							else{
								attrs[AttrType.RUNNING] = 0;
								attrs[AttrType.WAITING] = 0;
							}
							/*Tile opposite = TileInDirection(attrs[AttrType.RUNNING].RotateDir(true,4));
							int num_floors = 0;
							int floor_dir = 0;
							foreach(Tile t2 in TilesAtDistance(1)){
								//if(t2 != opposite && t2.name == "floor"){
								if(t2 != opposite && (t2.passable || t2.type == TileType.DOOR_C)){
									num_floors++;
									floor_dir = DirectionOf(t2);
								}
							}
							if(num_floors == 1){
								attrs[AttrType.RUNNING] = floor_dir;//the purpose of this code is to detect whether there's a valid turn to make
								PlayerWalk(floor_dir); //and take it if so. if there's a branch, it should stop.
								return;
							}
							else{
								attrs[AttrType.RUNNING] = 0;
								attrs[AttrType.WAITING] = 0;
							}*/
						}
					}
				}
				else{
					if(Global.KeyIsAvailable()){
						Global.ReadKey();
					}
					attrs[AttrType.RUNNING] = 0;
					attrs[AttrType.WAITING] = 0;
				}
			}
			if(HasAttr(AttrType.RESTING)){
				if(attrs[AttrType.RESTING] == 10){
					attrs[AttrType.RESTING] = -1;
					curhp = maxhp;
					curmp = maxmp;
					B.Add("You rest...you feel great! ");
					RemoveExhaustion();
					bool repaired = false;
					foreach(EquipmentStatus eqstatus in Enum.GetValues(typeof(EquipmentStatus))){
						foreach(Weapon w in weapons){
							if(w.status[eqstatus]){
								repaired = true;
								w.status[eqstatus] = false;
							}
						}
						foreach(Armor a in armors){
							if(a.status[eqstatus]){
								repaired = true;
								a.status[eqstatus] = false;
							}
						}
					}
					if(repaired){
						B.Add("You finish repairing your equipment. ");
					}
					if(magic_trinkets.Contains(MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE)){
						Event hiddencheck = null;
						foreach(Event e in Q.list){
							if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
								hiddencheck = e;
								break;
							}
						}
						List<Tile> valid_list = M.AllTiles().Where(x=>x.passable && !x.seen);
						while(valid_list.Count > 0){
							Tile chosen = valid_list.RemoveRandom();
							if(chosen == null){
								break;
							}
							var dijkstra = M.tile.GetDijkstraMap(x=>!M.tile[x].passable && !M.tile[x].IsDoorType(true),new List<pos>{chosen.p});
							if(chosen.TilesWithinDistance(18).Where(x=>x.passable && !x.seen && dijkstra[x.p] <= 18).Count < 20){
								continue;
							}
							foreach(Tile t in chosen.TilesWithinDistance(12)){
								if(t.type != TileType.FLOOR && !t.solid_rock){
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
								}
							}
							M.Draw();
							B.Add("Your " + MagicTrinket.Name(MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE) + " grants you a vision. ");
							break;
						}
					}
					B.Print(false);
					DisplayStats(true);
					Cursor();
				}
				else{
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a != this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
							if(!a.Is(ActorType.CARNIVOROUS_BRAMBLE,ActorType.MUD_TENTACLE) || DistanceFrom(a) <= 1){
								monsters_visible = true;
							}
						}
					}
					if(monsters_visible || Global.KeyIsAvailable()){
						if(Global.KeyIsAvailable()){
							Global.ReadKey();
						}
						if(monsters_visible){
							attrs[AttrType.RESTING] = 0;
							B.Add("You rest...you are interrupted! ");
							B.Print(false);
							Cursor();
						}
						else{
							attrs[AttrType.RESTING] = 0;
							B.Add("You rest...you stop resting. ");
							B.Print(false);
							Cursor();
						}
					}
					else{
						attrs[AttrType.RESTING]++;
						B.Add("You rest... ");
						Q1();
						return;
					}
				}
			}
			if(Q.turn == 0){
				Help.TutorialTip(TutorialTopic.Movement); //todo: move this elsewhere?
				Cursor();
			}
			if(!Help.displayed[TutorialTopic.Attacking] && M.AllActors().Any(a=>(a != this && CanSee(a)))){
				Help.TutorialTip(TutorialTopic.Attacking);
				Cursor();
			}
			MouseUI.IgnoreMouseMovement = false;
			ConsoleKeyInfo command = Global.ReadKey();
			/*ConsoleKeyInfo command;
			bool command_entered = false;
			while(!command_entered){
				if(Global.KeyPressed){
					command = Global.ReadKey();
					command_entered = true;
					break;
				}
				Screen.AnimateCellNonBlocking(row+Global.MAP_OFFSET_ROWS,col+Global.MAP_OFFSET_COLS,new colorchar('@',Color.DarkGray),200);
				if(Global.KeyPressed){
					command = Global.ReadKey();
					command_entered = true;
					break;
				}
				Screen.AnimateCellNonBlocking(row+Global.MAP_OFFSET_ROWS,col+Global.MAP_OFFSET_COLS,new colorchar('@',Color.DarkRed),200);
			}*/
			char ch = ConvertInput(command);
			ch = ConvertVIKeys(ch);
			bool alt = false;
			bool ctrl = false;
			bool shift = false;
			if((command.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt){
				alt = true;
			}
			if((command.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control){
				ctrl = true;
			}
			if((command.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift){
				shift = true;
			}
			/*if(true){
				char old_ch = ch;
				if(ch == safe_mode_char && DateTime.Now < safe_mode_time.AddMilliseconds(300)){
					ch = ' ';
				}
				safe_mode_char = old_ch;
				safe_mode_time = DateTime.Now;
			}*/
			switch(ch){
			case '7':
			case '8':
			case '9':
			case '4':
			case '6':
			case '1':
			case '2':
			case '3':
			{
				if(FrozenThisTurn()){
					break;
				}
				if(HasAttr(AttrType.CONFUSED)){
					PlayerWalk(Global.RandomDirection());
					break;
				}
				int dir = ch - 48; //ascii 0-9 are 48-57
				if(shift || alt || ctrl){
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){
							if(!a.Is(ActorType.CARNIVOROUS_BRAMBLE,ActorType.MUD_TENTACLE) || DistanceFrom(a) <= 2){
								monsters_visible = true;
							}
						}
					}
					PlayerWalk(dir);
					if(!monsters_visible){
						attrs[AttrType.RUNNING] = dir;
					}
				}
				else{
					PlayerWalk(dir);
				}
				break;
			}
			case '5':
			case '.':
			{
				if(tile().inv != null){
					tile().inv.revealed_by_light = true;
					if(tile().inv.quantity > 1){
						B.Add("There are " + tile().inv.AName() + " here. ");
					}
					else{
						B.Add("There is " + tile().inv.AName() + " here. ");
					}
					//B.Add("You see " + M.tile[row,col].inv.AName() + ". ");
				}
				if(HasAttr(AttrType.BURNING)){
					if(tile().IsWater() && !tile().Is(FeatureType.OIL)){
						B.Add("You extinguish the flames. ");
						attrs[AttrType.BURNING] = 0;
						if(light_radius == 0){
							UpdateRadius(1,0);
						}
						Q.KillEvents(this,AttrType.BURNING);
						Fire.burning_objects.Remove(this);
					}
					else{
						if(tile().Is(FeatureType.SLIME)){
							B.Add("You cover yourself in slime to remove the flames. ");
							attrs[AttrType.BURNING] = 0;
							if(light_radius == 0){
								UpdateRadius(1,0);
							}
							Q.KillEvents(this,AttrType.BURNING);
							attrs[AttrType.SLIMED] = 1;
							Fire.burning_objects.Remove(this);
							Help.TutorialTip(TutorialTopic.Slimed);
						}
					}
				}
				if(HasAttr(AttrType.SLIMED) && tile().IsWater() && !tile().Is(FeatureType.FIRE)){
					attrs[AttrType.SLIMED] = 0;
					B.Add("You wash off the slime. ");
				}
				if(HasAttr(AttrType.OIL_COVERED) && tile().Is(FeatureType.SLIME)){
					attrs[AttrType.OIL_COVERED] = 0;
					attrs[AttrType.SLIMED] = 1;
					B.Add("You cover yourself in slime to remove the oil. ");
					Help.TutorialTip(TutorialTopic.Slimed);
				}
				if(HasAttr(AttrType.OIL_COVERED) && tile().IsWater() && !tile().Is(FeatureType.FIRE,FeatureType.OIL)){
					attrs[AttrType.OIL_COVERED] = 0;
					B.Add("You wash off the oil. ");
					tile().AddFeature(FeatureType.OIL);
				}
				attrs[AttrType.NO_PLATE_ARMOR_NOISE] = 1;
				if(Speed() < 100){
					QS();
				}
				else{
					Q1();
				}
				break;
			}
			case 'w':
			{
				int dir = GetDirection("Start walking in which direction? ",false,true);
				if(dir != 0){
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a != this && CanSee(a) && HasLOS(a.row,a.col)){
							if(!a.Is(ActorType.CARNIVOROUS_BRAMBLE,ActorType.MUD_TENTACLE) || DistanceFrom(a) <= 2){
								monsters_visible = true;
							}
						}
					}
					if(dir != 5){
						if(FrozenThisTurn()){
							break;
						}
						PlayerWalk(dir);
					}
					else{
						QS();
					}
					if(!monsters_visible){
						attrs[AttrType.RUNNING] = dir;
						bool recover = false;
						if(!HasAttr(AttrType.NONLIVING)){
							if(HasFeat(FeatType.ENDURING_SOUL) && curhp % 10 != 0){
								recover = true;
							}
							if(HasAttr(AttrType.BANDAGED)){
								recover = true;
							}
						}
						if(!recover && dir == 5){
							attrs[AttrType.WAITING] = 20;
						}
					}
				}
				else{
					Q0();
				}
				break;
			}
			case 'o':
			{
				if(FrozenThisTurn()){
					break;
				}
				int dir = GetDirection("Operate something in which direction? ");
				if(dir != -1){
					Tile t = TileInDirection(dir);
					if(t.IsKnownTrap()){
						if(HasFeat(FeatType.DISARM_TRAP)){
							if(ActorInDirection(dir) != null){
								B.Add("There is " + ActorInDirection(dir).AName(true) + " in the way. ");
								Q0();
								return;
							}
							if(StunnedThisTurn()){
								return;
							}
							if(t.name.Contains("(safe)")){
								B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
								t.Toggle(this);
							}
							else{
								B.Add("You make " + Tile.Prototype(t.type).the_name + " safe to cross. ");
								t.SetName(Tile.Prototype(t.type).name + " (safe)");
							}
							Q1();
						}
						else{
							B.Add("You don't know how to disable that trap. ");
							Q0();
							return;
						}
					}
					else{
						switch(t.type){
						case TileType.DOOR_C:
						case TileType.DOOR_O:
						case TileType.RUBBLE:
							if(StunnedThisTurn()){
								break;
							}
							if(t.type == TileType.RUBBLE && !HasAttr(AttrType.BRUTISH_STRENGTH)){
								IncreaseExhaustion(1);
							}
							t.Toggle(this);
							Q1();
							break;
						case TileType.CHEST:
							B.Add("Stand on the chest and press 'g' to retrieve its contents. ");
							Q0();
							break;
						case TileType.STAIRS:
							B.Add("Stand on the stairs and press '>' to descend. ");
							Q0();
							break;
						case TileType.POOL_OF_RESTORATION:
							B.Add("Stand over the pool and drop an item in to activate it. ");
							Q0();
							break;
						case TileType.STONE_SLAB:
							B.Add("The slab will open if light shines upon it. ");
							Q0();
							break;
						default:
							if(t.IsShrine()){
								B.Add("Stand on the shrine and press 'g' to activate it. ");
							}
							Q0();
							break;
						}
					}
				}
				else{
					Q0();
				}
				break;
			}
			case 's':
			{
				if(FrozenThisTurn()){
					break;
				}
				if(Bow.status[EquipmentStatus.OUT_OF_ARROWS]){
					B.Add("You're out of arrows! ");
					Q0();
				}
				else{
					if(EquippedWeapon.type == WeaponType.BOW || HasFeat(FeatType.QUICK_DRAW)){
						if(ActorsAtDistance(1).Count > 0){
							int seen = ActorsAtDistance(1).Where(x=>CanSee(x)).Count;
							if(seen > 0){
								if(seen == 1){
									B.Add("You can't fire with an enemy so close. ");
								}
								else{
									B.Add("You can't fire with enemies so close. ");
								}
								Q0();
							}
							else{
								B.Add("As you raise your bow, something knocks it down! ");
								B.Print(true);
								Q1();
							}
						}
						else{
							MouseUI.fire_arrow_hack = true;
							List<Tile> line = GetTargetLine(12);
							if(line != null && line.Last() != tile()){
								if(EquippedWeapon != Bow && HasFeat(FeatType.QUICK_DRAW)){
									EquippedWeapon = Bow;
								}
								FireArrow(line);
								if(Bow.status[EquipmentStatus.ONE_ARROW_LEFT]){
									Bow.status[EquipmentStatus.ONE_ARROW_LEFT] = false;
									Bow.status[EquipmentStatus.OUT_OF_ARROWS] = true;
								}
								else{
									if(Bow.status[EquipmentStatus.ALMOST_OUT_OF_ARROWS]){
										if(R.OneIn(15)){
											Bow.status[EquipmentStatus.ALMOST_OUT_OF_ARROWS] = false;
											Bow.status[EquipmentStatus.ONE_ARROW_LEFT] = true;
											B.Add("You're down to your last arrow! ");
											B.PrintAll();
										}
									}
									else{
										if(Bow.status[EquipmentStatus.LOW_ON_ARROWS]){
											if(R.OneIn(20)){
												Bow.status[EquipmentStatus.LOW_ON_ARROWS] = false;
												Bow.status[EquipmentStatus.ALMOST_OUT_OF_ARROWS] = true;
												B.Add("You have only a few arrows left! ");
												B.PrintAll();
											}
										}
										else{
											if(R.OneIn(25)){
												Bow.status[EquipmentStatus.LOW_ON_ARROWS] = true;
												B.Add("You're running a bit low on arrows. ");
												B.PrintAll();
											}
										}
									}
								}
							}
							else{
								Q0();
							}
						}
					}
					else{
						B.Add("You need your bow to fire arrows - press e to switch equipment. ");
						//B.Add("You can't fire arrows without your bow equipped - press e to switch equipment. ");
						Q0();
					}
				}
				break;
			}
			case 'z':
			{
				if(FrozenThisTurn()){
					break;
				}
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SILENCE_AURA) && a.HasLOE(this)){
						if(this == player){
							if(CanSee(a)){
								B.Add(a.Your() + " aura of silence prevents you from casting! ");
							}
							else{
								B.Add("An aura of silence prevents you from casting! ");
							}
						}
						Q0();
						return;
					}
				}
				if(HasAttr(AttrType.SILENCED)){
					B.Add("You can't cast while silenced. ");
					Q0();
					return;
				}
				List<colorstring> ls = new List<colorstring>();
				List<SpellType> sp = new List<SpellType>();
				//foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
				bool bonus_marked = false;
				foreach(SpellType spell in spells_in_order){
					if(HasSpell(spell)){
						//string s = Spell.Name(spell).PadRight(15) + Spell.Tier(spell).ToString().PadLeft(3);
						//s = s + FailRate(spell).ToString().PadLeft(9) + "%";
						//s = s + Spell.Description(spell).PadLeft(34);
						//this is the recent one!   colorstring cs = new colorstring(Spell.Name(spell).PadRight(17) + Spell.Tier(spell).ToString().PadLeft(3),Color.Gray);
						colorstring cs = new colorstring(Spell.Name(spell).PadRight(17) + Spell.Tier(spell).ToString().PadLeft(2),Color.Gray);
						//cs.strings.Add(new cstr(FailRate(spell).ToString().PadLeft(9) + "%",FailColor(spell)));
						int failrate = Spell.FailRate(spell,exhaustion);
						cs.strings.Add(new cstr("/",Color.DarkGray));
						cs.strings.Add(new cstr((failrate.ToString() + "%  ").PadRight(5),FailColor(failrate)));
						// this too cs.strings.Add(new cstr("".PadLeft(5),Color.Gray));
						if(HasFeat(FeatType.MASTERS_EDGE) && Spell.IsDamaging(spell) && !bonus_marked){
							bonus_marked = true;
							cs = cs + Spell.DescriptionWithIncreasedDamage(spell);
						}
						else{
							cs = cs + Spell.Description(spell);
						}
						ls.Add(cs);
						sp.Add(spell);
					}
				}
				if(sp.Count > 0){
					colorstring topborder = new colorstring("-------------------Tier/Fail%-----------Description---------------",Color.Gray);
					//colorstring topborder = new colorstring("-------------------Tier (fail%)---------Description---------------",Color.Gray);
					//colorstring topborder = new colorstring("---------------------Tier-----------------Description-------------",Color.Gray);
					colorstring bottomborder = new colorstring("".PadRight(25,'-') + "[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(COLS,'-'),Color.Gray);
					//colorstring bottomborder = new colorstring("----------------" + "Exhaustion: ".PadLeft(12+(3-basefail.ToString().Length),'-'),Color.Gray,(basefail.ToString() + "%"),FailColor(basefail),"----------[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(22,'-'),Color.Gray);
					//int i = Select("Cast which spell? ",topborder,bottomborder,ls);
					int i = Select("Cast which spell? ",topborder,bottomborder,ls,false,false,true,true,HelpTopic.Spells);
					if(i != -1){
						if(!CastSpell(sp[i])){
							Q0();
						}
					}
					else{
						Q0();
					}
				}
				else{
					B.Add("You don't know any spells. ");
					Q0();
				}
				break;
			}
			case 'r':
				if(FrozenThisTurn()){
					break;
				}
				if(attrs[AttrType.RESTING] != -1){ //gets set to -1 if you've rested on this level
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a != this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
							if(!a.Is(ActorType.CARNIVOROUS_BRAMBLE,ActorType.MUD_TENTACLE) || DistanceFrom(a) <= 1){
								monsters_visible = true;
							}
						}
					}
					bool equipment_can_be_repaired = false;
					foreach(EquipmentStatus eqs in Enum.GetValues(typeof(EquipmentStatus))){
						foreach(Weapon w in weapons){
							if(w.status[eqs]){
								equipment_can_be_repaired = true;
								break;
							}
						}
						foreach(Armor a in armors){
							if(a.status[eqs]){
								equipment_can_be_repaired = true;
								break;
							}
						}
						if(equipment_can_be_repaired){
							break;
						}
					}
					if(!monsters_visible){
						if(curhp < maxhp || curmp < maxmp || exhaustion > 0 || equipment_can_be_repaired){
							if(Global.Option(OptionType.CONFIRM_BEFORE_RESTING) && !B.YesOrNoPrompt("Rest and repair your equipment?")){
								Q0();
								break;
							}
							if(StunnedThisTurn()){
								break;
							}
							attrs[AttrType.RESTING] = 1;
							B.Add("You rest... ");
							Q1();
						}
						else{
							B.Add("You don't need to rest right now. ");
							Q0();
						}
					}
					else{
						B.Add("You can't rest while there are enemies around! ");
						Q0();
					}
				}
				else{
					B.Add("You find it impossible to rest again on this dungeon level. ");
					Q0();
				}
				break;
			case '>':
				if(FrozenThisTurn()){
					break;
				}
				if(M.tile[row,col].type == TileType.STAIRS){
					if(StunnedThisTurn()){
						break;
					}
					bool equipment_can_be_repaired = false;
					foreach(EquipmentStatus eqs in Enum.GetValues(typeof(EquipmentStatus))){
						foreach(Weapon w in weapons){
							if(w.status[eqs]){
								equipment_can_be_repaired = true;
								break;
							}
						}
						foreach(Armor a in armors){
							if(a.status[eqs]){
								equipment_can_be_repaired = true;
								break;
							}
						}
						if(equipment_can_be_repaired){
							break;
						}
					}
					if(attrs[AttrType.RESTING] != -1 && (curhp < maxhp || curmp < maxmp || exhaustion > 0 || equipment_can_be_repaired)){
						MouseUI.descend_hack = true;
						if(!B.YesOrNoPrompt("Really take the stairs without resting first?")){
							Q0();
							return;
						}
					}
					bool shrine_remaining = false;
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(M.tile[i,j].IsShrine() && M.tile[i,j].type != TileType.SPELL_EXCHANGE_SHRINE){
								shrine_remaining = true;
								break;
							}
						}
						if(shrine_remaining){
							MouseUI.descend_hack = true;
							break;
						}
					}
					if(shrine_remaining){
						Help.TutorialTip(TutorialTopic.DistributionOfShrines);
						if(!B.YesOrNoPrompt("You feel an ancient power calling you back. Leave anyway?")){
							Q0();
							return;
						}
					}
					B.Add("You walk down the stairs. ");
					B.PrintAll();
					if(M.current_level < 20){
						M.GenerateLevel();
					}
					else{
						M.GenerateFinalLevel();
						B.Add("Strange chants and sulfurous smoke fill the air here. ");
					}
					if(magic_trinkets.Contains(MagicTrinketType.LENS_OF_SCRYING)){
						Item i = inv.Where(x=>!Item.identified[x.type]).Random();
						if(i != null){
							string itemname = i.NameWithoutQuantity();
							Item.identified[i.type] = true;
							string IDedname = i.NameWithoutQuantity();
							string isare = " is a ";
							if(i.quantity > 1){
								isare = " are ";
							}
							B.Add("Your " + MagicTrinket.Name(MagicTrinketType.LENS_OF_SCRYING) + " reveals that your " + itemname + isare + IDedname + ". ");
						}
					}
					if(M.current_level == 3){
						Help.TutorialTip(TutorialTopic.SwitchingEquipment);
					}
					Q0();
				}
				else{
					Tile stairs = null;
					foreach(Tile t in M.AllTiles()){
						if(t.type == TileType.STAIRS && t.seen){
							stairs = t;
							break;
						}
					}
					if(stairs != null){
						List<pos> stairpath = GetPath(stairs,-1,true);
						foreach(pos p in stairpath){
							if(p.row != row || p.col != col){
								colorchar cch = Screen.MapChar(p.row,p.col);
								if(p.row == stairs.row && p.col == stairs.col){
									cch.bgcolor = Color.Green;
									if(Global.LINUX && !Screen.GLMode){ //no bright bg in terminals
										cch.bgcolor = Color.DarkGreen;
									}
									if(cch.color == cch.bgcolor){
										cch.color = Color.Black;
									}
									Screen.WriteMapChar(p.row,p.col,cch);
								}
								else{
									cch.bgcolor = Color.DarkGreen;
									if(cch.color == cch.bgcolor){
										cch.color = Color.Black;
									}
									Screen.WriteMapChar(p.row,p.col,cch);
								}
							}
						}
						MouseUI.PushButtonMap(MouseMode.YesNoPrompt);
						MouseUI.CreateButton(ConsoleKey.Y,false,2,Global.MAP_OFFSET_COLS + 22,1,2);
						MouseUI.CreateButton(ConsoleKey.N,false,2,Global.MAP_OFFSET_COLS + 25,1,2);
						B.DisplayNow("Travel to the stairs? (y/n): ");
						Screen.CursorVisible = true;
						bool done = false;
						while(!done){
							command = Global.ReadKey();
							switch(command.KeyChar){
							case 'y':
							case 'Y':
							case '>':
							case (char)13:
								done = true;
								MouseUI.PopButtonMap();
								break;
							default:
								Q0();
								MouseUI.PopButtonMap();
								return;
							}
						}
						FindPath(stairs,-1,true);
						if(path.Count > 0){
							PlayerWalk(DirectionOf(path[0]));
							if(path.Count > 0){
								if(DistanceFrom(path[0]) == 0){
									path.RemoveAt(0);
								}
							}
						}
						else{
							B.Add("There's no path to the stairs. ");
							Q0();
						}
					}
					else{
						B.Add("You don't see any stairs here. ");
						Q0();
					}
				}
				break;
			case 'x':
			{
				if(FrozenThisTurn()){
					break;
				}
				if(!FindAutoexplorePath()){
					B.Add("You don't see a path for further exploration. ");
					Q0();
				}
				else{
					attrs[AttrType.AUTOEXPLORE]++;
					PlayerWalk(DirectionOf(path[0]));
					if(path.Count > 0){
						if(DistanceFrom(path[0]) == 0){
							path.RemoveAt(0);
						}
					}
				}
				break;
			}
			case 'X':
				if(FrozenThisTurn()){
					break;
				}
				Screen.CursorVisible = false;
				if(!interrupted_path.BoundsCheck(M.tile)){
					B.DisplayNow("Move cursor to choose destination, then press Enter. ");
				}
				Dictionary<Actor,colorchar> old_ch = new Dictionary<Actor,colorchar>();
				List<Actor> drawn = new List<Actor>();
				foreach(Actor a in M.AllActors()){
					if(CanSee(a)){
						old_ch.Add(a,M.last_seen[a.row,a.col]);
						M.last_seen[a.row,a.col] = new colorchar(a.symbol,a.color);
						drawn.Add(a);
					}
				}
				Screen.MapDrawWithStrings(M.last_seen,0,0,ROWS,COLS);
				ChoosePathingDestination();
				foreach(Actor a in drawn){
					M.last_seen[a.row,a.col] = old_ch[a];
				}
				M.Redraw();
				if(path.Count > 0){
					PlayerWalk(DirectionOf(path[0]));
					if(path.Count > 0){
						if(DistanceFrom(path[0]) == 0){
							path.RemoveAt(0);
						}
					}
				}
				else{
					Q0();
				}
				break;
			case 'g':
			case ';':
			{
				if(FrozenThisTurn()){
					break;
				}
				if(tile().inv == null){
					if(tile().type == TileType.CHEST){
						if(StunnedThisTurn()){
							break;
						}
						tile().OpenChest();
						Q1();
					}
					else{
						if(tile().IsShrine()){
							if(StunnedThisTurn()){
								break;
							}
							switch(tile().type){
							case TileType.COMBAT_SHRINE:
								Help.TutorialTip(TutorialTopic.Combat);
								IncreaseSkill(SkillType.COMBAT);
								break;
							case TileType.DEFENSE_SHRINE:
								Help.TutorialTip(TutorialTopic.Defense);
								IncreaseSkill(SkillType.DEFENSE);
								break;
							case TileType.MAGIC_SHRINE:
								Help.TutorialTip(TutorialTopic.Magic);
								IncreaseSkill(SkillType.MAGIC);
								break;
							case TileType.SPIRIT_SHRINE:
								Help.TutorialTip(TutorialTopic.Spirit);
								IncreaseSkill(SkillType.SPIRIT);
								break;
							case TileType.STEALTH_SHRINE:
								Help.TutorialTip(TutorialTopic.Stealth);
								IncreaseSkill(SkillType.STEALTH);
								break;
							case TileType.SPELL_EXCHANGE_SHRINE: //currently disabled
							{
								List<colorstring> ls = new List<colorstring>();
								List<SpellType> sp = new List<SpellType>();
								bool bonus_marked = false;
								foreach(SpellType spell in spells_in_order){
									if(HasSpell(spell)){
										colorstring cs = new colorstring(Spell.Name(spell).PadRight(18) + Spell.Tier(spell).ToString().PadLeft(3),Color.Gray);
										//cs.strings.Add(new cstr(FailRate(spell).ToString().PadLeft(9) + "%",FailColor(spell)));
										cs.strings.Add(new cstr("".PadRight(5),Color.Gray));
										if(HasFeat(FeatType.MASTERS_EDGE) && Spell.IsDamaging(spell) && !bonus_marked){
											bonus_marked = true;
											cs = cs + Spell.DescriptionWithIncreasedDamage(spell);
										}
										else{
											cs = cs + Spell.Description(spell);
										}
										ls.Add(cs);
										sp.Add(spell);
									}
								}
								if(sp.Count > 0){
									colorstring topborder = new colorstring("----------------------Tier-----------------Description------------",Color.Gray);
									int basefail = exhaustion;
									colorstring bottomborder = new colorstring("----------------" + "Exhaustion: ".PadLeft(12+(3-basefail.ToString().Length),'-'),Color.Gray,(basefail.ToString() + "%"),FailColor(basefail),"----------[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(22,'-'),Color.Gray);
									int i = Select("Trade one of your spells for another? ",topborder,bottomborder,ls,false,false,true,true,HelpTopic.Spells);
									if(i != -1){
										List<SpellType> unknown = new List<SpellType>();
										foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
											if(!HasSpell(spell) && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
												unknown.Add(spell);
											}
										}
										SpellType forgotten = sp[i];
										spells_in_order.Remove(forgotten);
										spells[forgotten] = false;
										SpellType learned = unknown.Random();
										spells[learned] = true;
										spells_in_order.Add(learned);
										B.Add("You forget " + Spell.Name(forgotten) + ". You learn " + Spell.Name(learned) + ". ");
										tile().TransformTo(TileType.RUINED_SHRINE);
										tile().SetName("ruined shrine of magic");
									}
									else{
										Q0();
									}
								}
								break;
							}
							default:
								break;
							}
							if(tile().type != TileType.SPELL_EXCHANGE_SHRINE){
								Q1();
							}
							//if(tile().type == TileType.MAGIC_SHRINE && spells_in_order.Count > 1){
							//	tile().TransformTo(TileType.SPELL_EXCHANGE_SHRINE);
							//}
							//else{
								if(tile().type != TileType.SPELL_EXCHANGE_SHRINE && !tile().name.Contains("ruined")){
									string oldname = tile().name;
									tile().TransformTo(TileType.RUINED_SHRINE);
									tile().SetName("ruined " + oldname);
								}
							//}
							foreach(Tile t in TilesWithinDistance(2)){
								if(t.IsShrine()){
									string oldname = t.name;
									t.TransformTo(TileType.RUINED_SHRINE);
									t.SetName("ruined " + oldname);
								}
							}
						}
						else{
							if(tile().type == TileType.BLAST_FUNGUS){
								if(InventoryCount() < Global.MAX_INVENTORY_SIZE){
									if(StunnedThisTurn()){
										break;
									}
									B.Add("You pull the blast fungus from the floor. ");
									B.Add("Its fuse ignites! ");
									tile().Toggle(null);
									Item i = Item.Create(ConsumableType.BLAST_FUNGUS,this);
									if(i != null){
										i.other_data = 3;
										i.revealed_by_light = true;
										Q.Add(new Event(i,100,EventType.BLAST_FUNGUS));
										Screen.AnimateMapCell(row,col,new colorchar('3',Color.Red),100);
									}
									Q1();
								}
								else{
									B.Add("Your pack is too full to pick up the blast fungus. ");
									Q0();
								}
							}
							else{
								if(tile().type == TileType.RUINED_SHRINE){
									B.Add("This " + tile().name + " has no power left. ");
									Q0();
								}
								else{
									B.Add("There's nothing here to pick up. ");
									Q0();
								}
							}
						}
					}
				}
				else{
					if(InventoryCount() < Global.MAX_INVENTORY_SIZE){
						if(StunnedThisTurn()){
							break;
						}
						if(InventoryCount() + tile().inv.quantity <= Global.MAX_INVENTORY_SIZE){
							Item i = tile().inv;
							tile().inv = null;
							if(i.light_radius > 0){
								i.UpdateRadius(i.light_radius,0);
							}
							i.row = -1;
							i.col = -1;
							i.revealed_by_light = true;
							B.Add("You pick up " + i.TheName() + ". ");
							GetItem(i);
							Q1();
						}
						else{
							int space_left = Global.MAX_INVENTORY_SIZE - InventoryCount();
							Item i = tile().inv;
							Item newitem = new Item(i,row,col);
							newitem.quantity = space_left;
							i.quantity -= space_left;
							newitem.revealed_by_light = true;
							B.Add("You pick up " + newitem.TheName() + ", but have no room for the other " + i.quantity.ToString() + ". ");
							GetItem(newitem);
							Q1();
						}
					}
					else{
						tile().inv.revealed_by_light = true;
						B.Add("Your pack is too full to pick up " + tile().inv.TheName() + ". ");
						Q0();
					}
				}
				break;
			}
			case 'i':
			case 'a': //these are handled in the same case label so I can drop down from the selection to the action
			case 'f':
			case 'd':
			{
				if(inv.Count == 0){
					B.Add("You have nothing in your pack. ");
					Q0();
					break;
				}
				if(ch == 'a' || ch == 'f' || ch == 'd'){
					if(FrozenThisTurn()){
						break;
					}
				}
				string msg = "In your pack: ";
				bool no_redraw = (ch == 'i');
				switch(ch){
				case 'a':
					msg = "Apply which item? ";
					break;
				case 'f':
					msg = "Fling which item? ";
					break;
				case 'd':
					msg = "Drop which item? ";
					break;
				}
				ItemSelection sel = new ItemSelection();
				sel.value = -2;
				while(sel.value != -1){
					sel = SelectItem(msg,no_redraw);
					if(ch == 'i'){
						sel.description_requested = true;
					}
					if(sel.value != -1 && sel.description_requested){
						MouseUI.PushButtonMap(MouseMode.Inventory);
						MouseUI.AutomaticButtonsFromStrings = true;
						Screen.CursorVisible = false;
						colorchar[,] screen = Screen.GetCurrentScreen();
						for(int letter=0;letter<inv.Count;++letter){
							Screen.WriteMapChar(letter+1,1,(char)(letter+'a'),Color.DarkCyan);
						}
						List<colorstring> box = ItemDescriptionBox(inv[sel.value],false,false,31);
						int i = (Global.SCREEN_H - box.Count) / 2;
						int j = (Global.SCREEN_W - box[0].Length()) / 2;
						foreach(colorstring cs in box){
							Screen.WriteString(i,j,cs);
							++i;
						}
						switch(ConvertInput(Global.ReadKey())){
						case 'a':
							ch = 'a';
							sel.description_requested = false;
							break;
						case 'f':
							ch = 'f';
							sel.description_requested = false;
							break;
						case 'd':
							ch = 'd';
							sel.description_requested = false;
							break;
						}
						MouseUI.PopButtonMap();
						MouseUI.AutomaticButtonsFromStrings = false;
						if(sel.description_requested){
							Screen.WriteArray(0,0,screen);
						}
						else{
							M.Redraw(); //this will break if the box goes off the map, todo
							break;
						}
						Screen.CursorVisible = true;
					}
					else{
						break;
					}
				}
				if(sel.value == -1){
					Q0();
					break;
				}
				int num = sel.value;
				if(ch == 'f' && (inv[num].NameOfItemType() == "orb" || inv[num].type == ConsumableType.BLAST_FUNGUS)){
					ch = 'a';
				}
				switch(ch){
				case 'a':
				{
					if(FrozenThisTurn()){
						break;
					}
					if(HasAttr(AttrType.NONLIVING) && inv[num].NameOfItemType() == "potion"){
						B.Add("Potions have no effect on you in stone form. ");
						Q0();
					}
					else{
						if(HasAttr(AttrType.NONLIVING) && inv[num].type == ConsumableType.BANDAGES){
							B.Add("Bandages have no effect on you in stone form. ");
							Q0();
						}
						else{
							if(HasAttr(AttrType.BLIND) && inv[num].NameOfItemType() == "scroll"){
								B.Add("You can't read scrolls while blind. ");
								Q0();
							}
							else{
								if(inv[num].NameOfItemType() == "wand" && inv[num].charges == 0 && inv[num].other_data == -1){
									B.Add("That wand has no charges left. ");
									Q0();
								}
								else{
									bool silenced = HasAttr(AttrType.SILENCED,AttrType.SILENCE_AURA);
									string silence_message = "You can't read scrolls while silenced. ";
									if(HasAttr(AttrType.SILENCE_AURA)){
										silence_message = "Your silence aura makes reading scrolls impossible. ";
									}
									else{
										List<Actor> auras = ActorsWithinDistance(2).Where(x=>x.HasAttr(AttrType.SILENCE_AURA) && x.HasLOE(this));
										if(auras.Count > 0){
											silenced = true;
											Actor a = auras.Where(x=>CanSee(x)).Random();
											if(a != null){
												silence_message = a.the_name + "'s silence aura makes reading scrolls impossible. ";
											}
											else{
												silence_message = "An aura of silence makes reading scrolls impossible. ";
											}
										}
									}
									if(silenced && inv[num].NameOfItemType() == "scroll"){
										B.Add(silence_message);
										Q0();
									}
									else{
										Actor thief = ActorsAtDistance(1).Where(x=>x.type == ActorType.SNEAK_THIEF).Random();
										if(thief != null){
											B.Add(thief.YouVisible("snatch",true) + " your " + inv[num].SingularName(false) + "! ");
											B.PrintAll();
											Item stolen = inv[num];
											if(inv[num].quantity > 1){
												stolen = new Item(inv[num],inv[num].row,inv[num].col);
												stolen.revealed_by_light = inv[num].revealed_by_light;
												inv[num].quantity--;
											}
											else{
												inv.Remove(stolen);
											}
											thief.GetItem(stolen);
											Q1();
										}
										else{
											if(StunnedThisTurn()){
												break;
											}
											if(HasAttr(AttrType.SLIMED,AttrType.OIL_COVERED) && R.OneIn(5)){
												Item i = inv[num];
												B.Add("The " + i.SingularName() + " slips out of your hands! ");
												i.revealed_by_light = true;
												if(i.quantity <= 1){
													if(tile().type == TileType.POOL_OF_RESTORATION){
														B.Add("You drop " + i.TheName() + " into the pool. ");
														inv.Remove(i);
														if(curhp < maxhp || curmp < maxmp || exhaustion > 0){
															B.Add("The pool's glow restores you. ");
															curhp = maxhp;
															curmp = maxmp;
															RemoveExhaustion();
														}
														else{
															B.Add("The pool of restoration glows briefly, then dries up. ");
														}
														tile().TurnToFloor();
													}
													else{
														if(tile().GetItem(i)){
															B.Add("You drop " + i.TheName() + ". ");
															inv.Remove(i);
														}
														else{
															//this only happens if every tile is full - i.e. never
														}
													}
												}
												else{
													if(tile().type == TileType.POOL_OF_RESTORATION){
														Item newitem = new Item(i,row,col);
														newitem.quantity = 1;
														i.quantity--;
														B.Add("You drop " + newitem.TheName() + " into the pool. ");
														if(curhp < maxhp || curmp < maxmp){
															B.Add("The pool's glow restores you. ");
															curhp = maxhp;
															curmp = maxmp;
														}
														else{
															B.Add("The pool of restoration glows briefly, then dries up. ");
														}
														tile().TurnToFloor();
													}
													else{
														Item newitem = new Item(i,row,col);
														newitem.quantity = 1;
														newitem.revealed_by_light = true;
														if(tile().GetItem(newitem)){
															i.quantity -= 1;
															B.Add("You drop " + newitem.TheName() + ". ");
														}
														else{
															//should never happen
														}
													}
												}
												Q1();
												break;
											}
											if(inv[num].Use(this)){
												Q1();
											}
											else{
												Q0();
											}
										}
									}
								}
							}
						}
					}
					break;
				}
				case 'f':
				{
					if(FrozenThisTurn()){
						break;
					}
					if(StunnedThisTurn()){
						break;
					}
					List<Tile> line = GetTargetTile(12,0,false,false);
					if(line != null){
						Item i = null;
						if(inv[num].quantity == 1){
							i = inv[num];
						}
						else{
							i = new Item(inv[num],-1,-1);
							inv[num].quantity--;
						}
						i.revealed_by_light = true;
						i.ignored = true;
						Tile t = line.LastBeforeSolidTile();
						Actor first = FirstActorInLine(line);
						B.Add(You("fling") + " " + i.TheName() + ". ");
						if(first != null && first != this){
							t = first.tile();
							B.Add("It hits " + first.the_name + ". ",first);
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
						if(line.Count > 0){
							AnimateProjectile(line,i.symbol,i.color);
						}
						if(i.IsBreakable()){
							B.Add("It breaks! ",t);
							i.CheckForMimic();
						}
						else{
							bool broken = false;
							if(i.type == ConsumableType.FLINT_AND_STEEL){
								if(R.OneIn(3)){
									i.other_data--;
									if(i.other_data == 2){
										B.Add("Your flint & steel shows signs of wear. ",t);
									}
									if(i.other_data == 1){
										B.Add("Your flint & steel is almost depleted. ",t);
									}
									if(i.other_data == 0){
										broken = true;
										B.Add("Your flint & steel is used up. ",t);
									}
								}
							}
							if(!broken){
								t.GetItem(i);
							}
						}
						inv.Remove(i);
						t.MakeNoise(2);
						if(first != null && first != this){
							first.player_visibility_duration = -1;
							first.attrs[AttrType.PLAYER_NOTICED]++;
						}
						else{
							if(t.IsTrap()){
								t.TriggerTrap();
							}
						}
						Q1();
					}
					else{
						Q0();
					}
					break;
				}
				case 'd':
				{
					if(FrozenThisTurn()){
						break;
					}
					if(StunnedThisTurn()){
						break;
					}
					Item i = inv[num];
					i.revealed_by_light = true;
					if(i.quantity <= 1){
						if(tile().type == TileType.POOL_OF_RESTORATION){
							B.Add("You drop " + i.TheName() + " into the pool. ");
							inv.Remove(i);
							if(curhp < maxhp || curmp < maxmp){
								B.Add("The pool's glow restores you. ");
								curhp = maxhp;
								curmp = maxmp;
							}
							else{
								B.Add("The pool of restoration glows briefly, then dries up. ");
							}
							tile().TurnToFloor();
							Q1();
						}
						else{
							if(tile().GetItem(i)){
								B.Add("You drop " + i.TheName() + ". ");
								inv.Remove(i);
								i.ignored = true;
								Q1();
							}
							else{
								B.Add("There is no room. ");
								Q0();
							}
						}
					}
					else{
						if(tile().type == TileType.POOL_OF_RESTORATION){
							Item newitem = new Item(i,row,col);
							newitem.quantity = 1;
							i.quantity--;
							B.Add("You drop " + newitem.TheName() + " into the pool. ");
							if(curhp < maxhp || curmp < maxmp){
								B.Add("The pool's glow restores you. ");
								curhp = maxhp;
								curmp = maxmp;
							}
							else{
								B.Add("The pool of restoration glows briefly, then dries up. ");
							}
							tile().TurnToFloor();
							Q1();
						}
						else{
							//B.DisplayNow("Drop how many? (1-" + i.quantity + "): ");
							//int count = Global.EnterInt();
							int count = 1;
							if(count == 0){
								Q0();
							}
							else{
								if(count >= i.quantity || count == -1){
									if(tile().GetItem(i)){
										B.Add("You drop " + i.TheName() + ". ");
										inv.Remove(i);
										i.ignored = true;
										Q1();
									}
									else{
										B.Add("There is no room. ");
										Q0();
									}
								}
								else{
									Item newitem = new Item(i,row,col);
									newitem.quantity = count;
									newitem.revealed_by_light = true;
									if(tile().GetItem(newitem)){
										i.quantity -= count;
										B.Add("You drop " + newitem.TheName() + ". ");
										newitem.ignored = true;
										Q1();
									}
									else{
										B.Add("There is no room. ");
										Q0();
									}
								}
							}
						}
					}
					break;
				}
				}
				break;
			}
			/*case 'i':
			case 'a': //these are handled in the same case label so I can drop down from 'i' to the others
			case 'f':
			case 'd':
			{
				int num = -2;
				if(ch == 'i'){
					if(inv.Count == 0){
						B.Add("You have nothing in your pack. ");
						Q0();
					}
					else{
						num = -2;
						while(num != -1){
							num = SelectItem("In your pack: ",true);
							if(num != -1){
								Screen.CursorVisible = false;
								colorchar[,] screen = Screen.GetCurrentScreen();
								for(int letter=0;letter<inv.Count;++letter){
									Screen.WriteMapChar(letter+1,1,(char)(letter+'a'),Color.DarkCyan);
								}
								List<colorstring> box = ItemDescriptionBox(inv[num],false,31);
								int i = (Global.SCREEN_H - box.Count) / 2;
								int j = (Global.SCREEN_W - box[0].Length()) / 2;
								foreach(colorstring cs in box){
									Screen.WriteString(i,j,cs);
									++i;
								}
								switch(ConvertInput(Global.ReadKey())){
								case 'a':
									ch = 'a';
									break;
								case 'f':
									ch = 'f';
									break;
								case 'd':
									ch = 'd';
									break;
								}
								if(ch == 'i'){
									Screen.WriteArray(0,0,screen);
								}
								else{
									M.Redraw(); //this will break if the box goes off the map, todo
									break;
								}
								Screen.CursorVisible = true;
							}
						}
						if(num == -1){
							Q0();
						}
					}
				}
				switch(ch){
				case 'a':
					if(FrozenThisTurn()){
						break;
					}
					if(inv.Count == 0){
						B.Add("You have nothing in your pack. ");
						Q0();
					}
					else{
						if(num == -2){
							num = SelectItem("Apply which item? ");
						}
						if(num != -1){
							if(HasAttr(AttrType.NONLIVING) && inv[num].NameOfItemType() == "potion"){
								B.Add("Potions have no effect on you in stone form. ");
								Q0();
							}
							else{
								bool silenced = HasAttr(AttrType.SILENCED);
								foreach(Actor a in ActorsWithinDistance(2)){
									if(a.HasAttr(AttrType.SILENCE_AURA) && a.HasLOE(this)){
										silenced = true;
									}
								}
								if(silenced && inv[num].NameOfItemType() == "scroll"){
									B.Add("You can't read scrolls while silenced. ");
									Q0();
								}
								else{
									if(StunnedThisTurn()){
										break;
									}
									if(HasAttr(AttrType.SLIMED,AttrType.OIL_COVERED) && R.OneIn(5)){
										Item i = inv[num];
										B.Add("The " + i.SingularName() + " slips out of your hands! ");
										i.revealed_by_light = true;
										if(i.quantity <= 1){
											if(tile().type == TileType.POOL_OF_RESTORATION){
												B.Add("You drop " + i.TheName() + " into the pool. ");
												inv.Remove(i);
												if(curhp < maxhp || curmp < maxmp){
													B.Add("The pool's glow restores you. ");
													curhp = maxhp;
													curmp = maxmp; //todo: exhaustion too?
												}
												else{
													B.Add("The pool of restoration glows briefly, then dries up. ");
												}
												tile().TurnToFloor();
											}
											else{
												if(tile().GetItem(i)){
													B.Add("You drop " + i.TheName() + ". ");
													inv.Remove(i);
												}
												else{
													//this only happens if every tile is full - i.e. never
												}
											}
										}
										else{
											if(tile().type == TileType.POOL_OF_RESTORATION){
												Item newitem = new Item(i,row,col);
												newitem.quantity = 1;
												i.quantity--;
												B.Add("You drop " + newitem.TheName() + " into the pool. ");
												if(curhp < maxhp || curmp < maxmp){
													B.Add("The pool's glow restores you. ");
													curhp = maxhp;
													curmp = maxmp;
												}
												else{
													B.Add("The pool of restoration glows briefly, then dries up. ");
												}
												tile().TurnToFloor();
											}
											else{
												Item newitem = new Item(i,row,col);
												newitem.quantity = 1;
												newitem.revealed_by_light = true;
												if(tile().GetItem(newitem)){
													i.quantity -= 1;
													B.Add("You drop " + newitem.TheName() + ". ");
												}
												else{
													//should never happen
												}
											}
										}
										Q1();
										break;
									}
									if(inv[num].Use(this)){
										Q1();
									}
									else{
										Q0();
									}
								}
							}
						}
						else{
							Q0();
						}
					}
					break;
				case 'f':
					if(FrozenThisTurn()){
						break;
					}
					if(inv.Count == 0){
						B.Add("You have nothing in your pack. ");
						Q0();
					}
					else{
						if(num == -2){
							num = SelectItem("Fling which item? ");
						}
						if(num != -1){
							if(StunnedThisTurn()){
								break;
							}
							bool target_nearest_enemy = false;
							if(inv[num].NameOfItemType() == "orb" || inv[num].type == ConsumableType.BLAST_FUNGUS){
								target_nearest_enemy = true;
							}
							List<Tile> line = GetTarget(false,12,0,false,false,target_nearest_enemy,"");
							if(line != null){
								if(inv[num].NameOfItemType() == "orb" || inv[num].type == ConsumableType.BLAST_FUNGUS){
									inv[num].Use(this,line);
									Q1();
								}
								else{
									Item i = null;
									if(inv[num].quantity == 1){
										i = inv[num];
									}
									else{
										i = new Item(inv[num],-1,-1);
										inv[num].quantity--;
									}
									i.revealed_by_light = true;
									i.ignored = true;
									Tile t = line.LastBeforeSolidTile();
									Actor first = FirstActorInLine(line);
									B.Add(You("fling") + " " + i.TheName() + ". ");
									if(first != null && first != this){
										t = first.tile();
										B.Add("It hits " + first.the_name + ". ",first);
									}
									line = line.ToFirstSolidTileOrActor();
									if(line.Count > 0){
										line.RemoveAt(line.Count - 1);
									}
									if(line.Count > 0){
										line.RemoveAt(line.Count - 1); //i forget why I needed to do this twice, but it seems to work
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
									if(line.Count > 0){
										AnimateProjectile(line,i.symbol,i.color);
									}
									if(i.IsBreakable()){
										B.Add("It breaks! ",t);
									}
									else{
										t.GetItem(i);
									}
									inv.Remove(i);
									t.MakeNoise(2);
									if(first != null && first != this){
										first.player_visibility_duration = -1;
										first.attrs[AttrType.PLAYER_NOTICED]++;
									}
									else{
										if(t.IsTrap()){
											t.TriggerTrap();
										}
									}
									Q1();
								}
							}
							else{
								Q0();
							}
						}
						else{
							Q0();
						}
					}
					break;
				case 'd':
					if(FrozenThisTurn()){
						break;
					}
					if(inv.Count == 0){
						B.Add("You have nothing to drop. ");
						Q0();
					}
					else{
						if(num == -2){
							num = SelectItem("Drop which item? ");
						}
						if(num != -1){
							if(StunnedThisTurn()){
								break;
							}
							Item i = inv[num];
							i.revealed_by_light = true;
							if(i.quantity <= 1){
								if(tile().type == TileType.POOL_OF_RESTORATION){
									B.Add("You drop " + i.TheName() + " into the pool. ");
									inv.Remove(i);
									if(curhp < maxhp || curmp < maxmp){
										B.Add("The pool's glow restores you. ");
										curhp = maxhp;
										curmp = maxmp;
									}
									else{
										B.Add("The pool of restoration glows briefly, then dries up. ");
									}
									tile().TurnToFloor();
									Q1();
								}
								else{
									if(tile().GetItem(i)){
										B.Add("You drop " + i.TheName() + ". ");
										inv.Remove(i);
										i.ignored = true;
										Q1();
									}
									else{
										B.Add("There is no room. ");
										Q0();
									}
								}
							}
							else{
								if(tile().type == TileType.POOL_OF_RESTORATION){
									Item newitem = new Item(i,row,col);
									newitem.quantity = 1;
									i.quantity--;
									B.Add("You drop " + newitem.TheName() + " into the pool. ");
									if(curhp < maxhp || curmp < maxmp){
										B.Add("The pool's glow restores you. ");
										curhp = maxhp;
										curmp = maxmp;
									}
									else{
										B.Add("The pool of restoration glows briefly, then dries up. ");
									}
									tile().TurnToFloor();
									Q1();
								}
								else{
									B.DisplayNow("Drop how many? (1-" + i.quantity + "): ");
									int count = Global.EnterInt();
									if(count == 0){
										Q0();
									}
									else{
										if(count >= i.quantity || count == -1){
											if(tile().GetItem(i)){
												B.Add("You drop " + i.TheName() + ". ");
												inv.Remove(i);
												i.ignored = true;
												Q1();
											}
											else{
												B.Add("There is no room. ");
												Q0();
											}
										}
										else{
											Item newitem = new Item(i,row,col);
											newitem.quantity = count;
											newitem.revealed_by_light = true;
											if(tile().GetItem(newitem)){
												i.quantity -= count;
												B.Add("You drop " + newitem.TheName() + ". ");
												newitem.ignored = true;
												Q1();
											}
											else{
												B.Add("There is no room. ");
												Q0();
											}
										}
									}
								}
							}
						}
						else{
							Q0();
						}
					}
					break;
				}
				break;
			}*/
			case 'e':
			{
				int[] changes = DisplayEquipment();
				M.Redraw();
				Weapon new_weapon = WeaponOfType((WeaponType)changes[0]);
				Armor new_armor = ArmorOfType((ArmorType)changes[1]);
				Weapon old_weapon = EquippedWeapon;
				Armor old_armor = EquippedArmor;
				bool weapon_changed = (new_weapon != old_weapon);
				bool armor_changed = (new_armor != old_armor);
				if(weapon_changed || armor_changed){
					if(FrozenThisTurn()){
						break;
					}
				}
				bool cursed_weapon = false;
				bool cursed_armor = false;
				if(weapon_changed && EquippedWeapon.status[EquipmentStatus.STUCK]){
					cursed_weapon = true;
					weapon_changed = false;
					armor_changed = false;
				}
				if(armor_changed && EquippedArmor.status[EquipmentStatus.STUCK]){
					cursed_armor = true;
					armor_changed = false;
					weapon_changed = false;
				}
				if(!weapon_changed && !armor_changed){
					if(cursed_weapon){
						B.Add("Your " + EquippedWeapon + " is stuck to your hand and can't be put away. ");
					}
					if(cursed_armor){
						B.Add("Your " + EquippedArmor + " is stuck to your body and can't be removed. ");
					}
					Q0();
				}
				else{
					if(StunnedThisTurn()){
						break;
					}
					Help.displayed[TutorialTopic.SwitchingEquipment] = true;
					if(weapon_changed){
						EquippedWeapon = new_weapon;
						if(HasFeat(FeatType.QUICK_DRAW) && !armor_changed && old_weapon != Bow){
							B.Add("You quickly ready your " + EquippedWeapon + ". ");
						}
						else{
							if(old_weapon == Bow){
								B.Add("You put away your bow and ready your " + EquippedWeapon + ". ");
							}
							else{
								B.Add("You ready your " + EquippedWeapon + ". ");
							}
						}
						//UpdateOnEquip(old_weapon,EquippedWeapon);
					}
					if(armor_changed){
						EquippedArmor = new_armor;
						//UpdateOnEquip(old_armor,EquippedArmor);
						if(TotalProtectionFromArmor() == 0){
							if(EquippedArmor.status[EquipmentStatus.DAMAGED]){
								B.Add("You wear your damaged " + EquippedArmor + ". ");
								B.Add("(It provides no protection until repaired.) ");
							}
							else{
								B.Add("You wear your " + EquippedArmor + ". ");
								B.Add("You can't wear it effectively in your exhausted state. ");
							}
						}
						else{
							B.Add("You wear your " + EquippedArmor + ". ");
						}
						attrs[AttrType.SWITCHING_ARMOR] = 1;
					}
					if(cursed_weapon){
						B.Add("Your " + EquippedWeapon + " is stuck to your hand and can't be put away. ");
					}
					if(cursed_armor){
						B.Add("Your " + EquippedArmor + " is stuck to your body and can't be removed. ");
					}
					if(HasFeat(FeatType.QUICK_DRAW) && !armor_changed && old_weapon != Bow){
						Q0();
					}
					else{
						Q1();
					}
				}
				break;
			}
			case '!': //note that these are the top-row numbers, NOT the actual shifted versions
			case '@': //<---this is the '2' above the 'w'    (not the '@', and not the numpad 2)
			case '#':
			case '$':
			case '%':
			{
				if(FrozenThisTurn()){
					break;
				}
				if(EquippedWeapon.status[EquipmentStatus.STUCK]){
					B.Add("Your " + EquippedWeapon + " is stuck to your hand and can't be put away. ");
					Q0();
				}
				else{
					Weapon new_weapon = null;
					switch(ch){
					case '!':
						new_weapon = Sword;
						break;
					case '@':
						new_weapon = Mace;
						break;
					case '#':
						new_weapon = Dagger;
						break;
					case '$':
						new_weapon = Staff;
						break;
					case '%':
						new_weapon = Bow;
						break;
					}
					Weapon old_weapon = EquippedWeapon;
					if(new_weapon == old_weapon){
						Q0();
					}
					else{
						if(StunnedThisTurn()){
							break;
						}
						Help.displayed[TutorialTopic.SwitchingEquipment] = true;
						EquippedWeapon = new_weapon;
						if(HasFeat(FeatType.QUICK_DRAW) && old_weapon != Bow){
							B.Add("You quickly ready your " + EquippedWeapon + ". ");
							Q0();
						}
						else{
							if(old_weapon == Bow){
								B.Add("You put away your bow and ready your " + EquippedWeapon + ". ");
							}
							else{
								B.Add("You ready your " + EquippedWeapon + ". ");
							}
							Q1();
						}
						//UpdateOnEquip(old_weapon,EquippedWeapon);
					}
				}
				break;
			}
			case '*': //these are toprow numbers, not shifted versions. see above.
			case '(':
			case ')':
			{
				if(FrozenThisTurn()){
					break;
				}
				if(EquippedArmor.status[EquipmentStatus.STUCK]){
					B.Add("Your " + EquippedArmor + " is stuck to your body and can't be removed. ");
					Q0();
				}
				else{
					Armor new_armor = null;
					switch(ch){
					case '*':
						new_armor = Leather;
						break;
					case '(':
						new_armor = Chainmail;
						break;
					case ')':
						new_armor = Plate;
						break;
					}
					Armor old_armor = EquippedArmor;
					if(new_armor == old_armor){
						Q0();
					}
					else{
						if(StunnedThisTurn()){
							break;
						}
						Help.displayed[TutorialTopic.SwitchingEquipment] = true;
						EquippedArmor = new_armor;
						if(TotalProtectionFromArmor() == 0){
							if(EquippedArmor.status[EquipmentStatus.DAMAGED]){
								B.Add("You wear your damaged " + EquippedArmor + ". ");
								B.Add("(It provides no protection until repaired.) ");
							}
							else{
								B.Add("You wear your " + EquippedArmor + ". ");
								B.Add("You can't wear it effectively in your exhausted state. ");
							}
						}
						else{
							B.Add("You wear your " + EquippedArmor + ". ");
						}
						attrs[AttrType.SWITCHING_ARMOR] = 1;
						Q1();
						//UpdateOnEquip(old_weapon,EquippedWeapon);
					}
				}
				break;
			}
			case 't':
				if(FrozenThisTurn()){
					break;
				}
				if(StunnedThisTurn()){
					break;
				}
				if(light_radius==0){
					if(!M.wiz_dark){
						B.Add("You bring out your torch. ");
					}
					else{
						B.Add("You bring out your torch, but it gives off no light! ");
					}
					int old = LightRadius();
					light_radius = 6;
					if(old != LightRadius()){
						UpdateRadius(old,LightRadius());
					}
					if(HasAttr(AttrType.DIM_LIGHT)){
						CalculateDimming();
					}
				}
				else{
					if(!M.wiz_lite){
						B.Add("You put away your torch. ");
					}
					else{
						B.Add("You put away your torch. The air still shines brightly. ");
					}
					int old = LightRadius();
					if(HasAttr(AttrType.SHINING)){
						RefreshDuration(AttrType.SHINING,0);
					}
					light_radius = 0;
					if(old != LightRadius()){
						UpdateRadius(old,LightRadius());
					}
					if(HasAttr(AttrType.SHADOW_CLOAK) && !tile().IsLit()){
						B.Add("You fade away in the darkness. ");
					}
				}
				Q1();
				break;
			case (char)9:
			{
				GetTarget(true,-1,0,false,false,true,"");
				if(path.Count > 0){
					if(FrozenThisTurn()){
						break;
					}
					PlayerWalk(DirectionOf(path[0]));
					if(path.Count > 0){
						if(DistanceFrom(path[0]) == 0){
							path.RemoveAt(0);
						}
					}
				}
				else{
					Q0();
				}
				break;
			}
			case 'm':
			{
				MouseUI.PushButtonMap();
				Screen.CursorVisible = false;
				Tile stairs = M.AllTiles().Where(x=>x.type == TileType.STAIRS && x.seen).Random();
				colorchar cch = Screen.BlankChar();
				if(stairs != null){
					cch = M.last_seen[stairs.row,stairs.col];
					M.last_seen[stairs.row,stairs.col] = new colorchar('>',stairs.color);
				}
				Screen.MapDrawWithStrings(M.last_seen,0,0,ROWS,COLS);
				Screen.CursorVisible = true;
				viewing_map_shrine_info = true;
				ChoosePathingDestination(false,false,"Map of dungeon level " + M.current_level + ": ");
				viewing_map_shrine_info = false;
				if(stairs != null){
					M.last_seen[stairs.row,stairs.col] = cch;
				}
				MouseUI.PopButtonMap();
				M.Redraw();
				if(path.Count > 0){
					if(FrozenThisTurn()){
						break;
					}
					PlayerWalk(DirectionOf(path[0]));
					if(path.Count > 0){
						if(DistanceFrom(path[0]) == 0){
							path.RemoveAt(0);
						}
					}
				}
				else{
					Q0();
				}
				break;
			}
			case 'p':
			{
				SharedEffect.ShowPreviousMessages(true);
				Q0();
				break;
			}
			case 'c':
			{
				int feat = DisplayCharacterInfo();
				if(feat >= 0){
					if(FrozenThisTurn()){
						break;
					}
					foreach(FeatType f in feats_in_order){
						if(Feat.IsActivated(f)){
							if(feat == 0){
								M.Redraw();
								if(StunnedThisTurn()){
									break;
								}
								if(!UseFeat(f)){
									Q0();
								}
								break;
							}
							else{
								--feat;
							}
						}
					}
				}
				else{
					Q0();
				}
				break;
			}
			case '\\':
			{
				SharedEffect.ShowKnownItems(Item.identified);
				Q0();
				break;
				/*MouseUI.PushButtonMap();
				List<colorstring> potions = new List<colorstring>();
				List<colorstring> scrolls = new List<colorstring>();
				List<colorstring> orbs = new List<colorstring>();
				foreach(ConsumableType ct in Enum.GetValues(typeof(ConsumableType))){
					string type_name = "    " + ct.ToString()[0] + ct.ToString().Substring(1).ToLower();
					type_name = type_name.Replace('_',' ');
					Color ided_color = Color.Cyan;
					if(Item.NameOfItemType(ct) == "potion"){
						if(Item.identified[ct]){
							potions.Add(new colorstring(type_name,ided_color));
			            }
			            else{
							potions.Add(new colorstring(type_name,Color.DarkGray));
						}
					}
					else{
						if(Item.NameOfItemType(ct) == "scroll"){
							if(Item.identified[ct]){
								scrolls.Add(new colorstring(type_name,ided_color));
				            }
				            else{
								scrolls.Add(new colorstring(type_name,Color.DarkGray));
							}
						}
						else{
							if(Item.NameOfItemType(ct) == "orb"){
								if(Item.identified[ct]){
									orbs.Add(new colorstring(type_name,ided_color));
					            }
					            else{
									orbs.Add(new colorstring(type_name,Color.DarkGray));
								}
							}
						}
					}
				}
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				for(int i=1;i<ROWS-1;++i){
					Screen.WriteMapString(i,0,"".PadToMapSize());
				}
				Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
				Color label_color = Color.Yellow;
				Screen.WriteMapString(1,0,"  - Potions -",label_color);
				Screen.WriteMapString(1,33,"  - Scrolls -",label_color);
				int line = 2;
				foreach(colorstring s in potions){
					Screen.WriteMapString(line,0,s);
					++line;
				}
				line = 2;
				foreach(colorstring s in scrolls){
					Screen.WriteMapString(line,33,s);
					++line;
				}
				Screen.WriteMapString(12,0,"  - Orbs -",label_color);
				line = 13;
				foreach(colorstring s in orbs){
					Screen.WriteMapString(line,0,s);
					++line;
				}
				B.DisplayNow("Discovered item types: ");
				Screen.CursorVisible = true;
				Global.ReadKey();
				MouseUI.PopButtonMap();
				Q0();
				break;*/
			}
			case 'O':
			case '=':
			{
				MouseUI.PushButtonMap();
				for(bool done=false;!done;){
					List<string> ls = new List<string>();
					ls.Add("Disable wall sliding".PadRight(58) + (Global.Option(OptionType.NO_WALL_SLIDING)? "yes ":"no ").PadLeft(4));
					ls.Add("Automatically pick up items (if safe)".PadRight(58) + (Global.Option(OptionType.AUTOPICKUP)? "yes ":"no ").PadLeft(4));
					ls.Add("Hide the path shown by mouse movement".PadRight(58) + (!MouseUI.VisiblePath? "yes ":"no ").PadLeft(4));
					ls.Add("Use top-row numbers for movement".PadRight(58) + (Global.Option(OptionType.TOP_ROW_MOVEMENT)? "yes ":"no ").PadLeft(4));
					ls.Add("Ask for confirmation before resting".PadRight(58) + (Global.Option(OptionType.CONFIRM_BEFORE_RESTING)? "yes ":"no ").PadLeft(4));
					ls.Add("Show out-of-sight areas in dark gray instead".PadRight(58) + (Global.Option(OptionType.DARK_GRAY_UNSEEN)? "yes ":"no ").PadLeft(4));
					ls.Add("Never show tutorial tips".PadRight(58) + (Global.Option(OptionType.NEVER_DISPLAY_TIPS)? "yes ":"no ").PadLeft(4));
					ls.Add("Reset tutorial tips before each game".PadRight(58) + (Global.Option(OptionType.ALWAYS_RESET_TIPS)? "yes ":"no ").PadLeft(4));
					if(Global.LINUX && !Screen.GLMode){
						ls.Add("Attempt to fix display glitches on certain terminals".PadRight(COLS));
					}
					/*if(Screen.GLMode){
						ls.Add("Disable graphics".PadRight(58) + (Global.Option(OptionType.DISABLE_GRAPHICS)? "yes ":"no ").PadLeft(4));
					}*/
					Select("Options: ",ls,true,false,false);
					Screen.CursorVisible = true;
					ch = ConvertInput(Global.ReadKey());
					switch(ch){
					case 'a':
						Global.Options[OptionType.NO_WALL_SLIDING] = !Global.Option(OptionType.NO_WALL_SLIDING);
						break;
					case 'b':
						Global.Options[OptionType.AUTOPICKUP] = !Global.Option(OptionType.AUTOPICKUP);
						break;
					case 'c':
						MouseUI.VisiblePath = !MouseUI.VisiblePath;
						break;
					case 'd':
						Global.Options[OptionType.TOP_ROW_MOVEMENT] = !Global.Option(OptionType.TOP_ROW_MOVEMENT);
						break;
					case 'e':
						Global.Options[OptionType.CONFIRM_BEFORE_RESTING] = !Global.Option(OptionType.CONFIRM_BEFORE_RESTING);
						break;
					case 'f':
						Global.Options[OptionType.DARK_GRAY_UNSEEN] = !Global.Option(OptionType.DARK_GRAY_UNSEEN);
						M.Draw();
						if(Screen.GLMode){
							Screen.UpdateGLBuffer(0,0,Global.SCREEN_H-1,Global.SCREEN_W-1);
						}
						break;
					case 'g':
						Global.Options[OptionType.NEVER_DISPLAY_TIPS] = !Global.Option(OptionType.NEVER_DISPLAY_TIPS);
						break;
					case 'h':
						Global.Options[OptionType.ALWAYS_RESET_TIPS] = !Global.Option(OptionType.ALWAYS_RESET_TIPS);
						break;
					case 'i':
						if(Global.LINUX && !Screen.GLMode){
							colorchar[,] screen = Screen.GetCurrentScreen();
							colorchar cch = new colorchar('@',Color.White);
							for(int i=0;i<Global.SCREEN_H;++i){
								for(int j=i%2;j<Global.SCREEN_W;j+=2){
									if(i != Global.SCREEN_H-1 || j != Global.SCREEN_W-1){
										Screen.WriteChar(i,j,cch);
									}
								}
							}
							cch = new colorchar('@',Color.Green);
							for(int i=0;i<Global.SCREEN_H;++i){
								Screen.WriteChar(i,0,cch);
								for(int j=1 + i%2;j<Global.SCREEN_W;j+=2){
									if(i != Global.SCREEN_H-1 || j != Global.SCREEN_W-1){
										Screen.WriteChar(i,j,cch);
									}
								}
							}
							cch = new colorchar('@',Color.Cyan);
							for(int j=0;j<Global.SCREEN_W;++j){
								for(int i=0;i<Global.SCREEN_H;++i){
									if(i != Global.SCREEN_H-1 || j != Global.SCREEN_W-1){
										Screen.WriteChar(i,j,cch);
									}
								}
							}
							for(int i=0;i<Global.SCREEN_H;++i){
								for(int j=0;j<Global.SCREEN_W;++j){
									if(i != Global.SCREEN_H-1 || j != Global.SCREEN_W-1){
										Screen.WriteChar(i,j,screen[i,j]);
									}
								}
							}
						}
						/*else{
							if(Screen.GLMode){
								Global.Options[OptionType.DISABLE_GRAPHICS] = !Global.Option(OptionType.DISABLE_GRAPHICS);
								GLGame.graphics_surface.Disabled = Global.Option(OptionType.DISABLE_GRAPHICS);
								Global.GRAPHICAL = !Global.Option(OptionType.DISABLE_GRAPHICS);
								if(Global.Option(OptionType.DISABLE_GRAPHICS)){
									Screen.UpdateCursor(false);
								}
								else{
									Game.gl.ResizeToDefault();
								}
							}
						}*/
						break;
					case (char)27:
					case ' ':
					case (char)13:
						done = true;
						break;
					default:
						break;
					}
				}
				MouseUI.PopButtonMap();
				Q0();
				break;
			}
			case '?':
			case '/':
			{
				Help.DisplayHelp();
				Q0();
				break;
			}
			case '-':
			{
				MouseUI.PushButtonMap();
				Screen.CursorVisible = false;
				List<string> commandhelp = Help.HelpText(HelpTopic.Commands);
				commandhelp.RemoveRange(0,2);
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				for(int i=0;i<20;++i){
					Screen.WriteMapString(i+1,0,commandhelp[i].PadRight(COLS));
				}
				Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
				B.DisplayNow("Commands: ");
				Screen.CursorVisible = true;
				Global.ReadKey();
				MouseUI.PopButtonMap();
				Q0();
				break;
			}
			case 'q':
			{
				List<string> ls = new List<string>();
				ls.Add("Save your progress and exit to main menu");
				ls.Add("Save your progress and quit game");
				ls.Add("Abandon character and exit to main menu");
				ls.Add("Abandon character and quit game");
				ls.Add("Quit game immediately - don't save anything");
				ls.Add("Continue playing");
				bool no_close = Game.NoClose;
				Game.NoClose = false;
				Screen.CursorVisible = true;
				switch(Select("Quit? ",ls)){
				case 0:
					Global.GAME_OVER = true;
					Global.SAVING = true;
					break;
				case 1:
					Global.GAME_OVER = true;
					Global.QUITTING = true;
					Global.SAVING = true;
					break;
				case 2:
					Global.GAME_OVER = true;
					Global.KILLED_BY = "giving up";
					break;
				case 3:
					Global.GAME_OVER = true;
					Global.QUITTING = true;
					Global.KILLED_BY = "giving up";
					break;
				case 4:
					Global.Quit();
					break;
				case 5:
				default:
					break;
				}
				if(!Global.SAVING){
					Q0();
				}
				Game.NoClose = no_close;
				break;
			}
			case 'v':
				if(viewing_more_commands){
					viewing_more_commands = false;
					MouseUI.PopButtonMap();
					MouseUI.PushButtonMap(MouseMode.Map);
					MouseUI.CreateStatsButton(ConsoleKey.I,false,12,1);
					MouseUI.CreateStatsButton(ConsoleKey.E,false,13,1);
					MouseUI.CreateStatsButton(ConsoleKey.C,false,14,1);
					MouseUI.CreateStatsButton(ConsoleKey.T,false,15,1);
					MouseUI.CreateStatsButton(ConsoleKey.Tab,false,16,1);
					MouseUI.CreateStatsButton(ConsoleKey.R,false,17,1);
					MouseUI.CreateStatsButton(ConsoleKey.A,false,18,1);
					MouseUI.CreateStatsButton(ConsoleKey.G,false,19,1);
					MouseUI.CreateStatsButton(ConsoleKey.F,false,20,1);
					MouseUI.CreateStatsButton(ConsoleKey.S,false,21,1);
					MouseUI.CreateStatsButton(ConsoleKey.Z,false,22,1);
					MouseUI.CreateStatsButton(ConsoleKey.X,false,23,1);
					MouseUI.CreateStatsButton(ConsoleKey.V,false,24,1);
					MouseUI.CreateStatsButton(ConsoleKey.E,false,7,2);
					MouseUI.CreateMapButton(ConsoleKey.P,false,0,3);
				}
				else{
					viewing_more_commands = true;
					MouseUI.PopButtonMap();
					MouseUI.PushButtonMap(MouseMode.Map);
					MouseUI.CreateStatsButton(ConsoleKey.O,false,12,1);
					MouseUI.CreateStatsButton(ConsoleKey.W,false,13,1);
					MouseUI.CreateStatsButton(ConsoleKey.X,true,14,1);
					MouseUI.CreateStatsButton(ConsoleKey.OemPeriod,false,15,1);
					MouseUI.CreateStatsButton(ConsoleKey.OemPeriod,true,16,1); //>
					MouseUI.CreateStatsButton(ConsoleKey.M,false,17,1);
					MouseUI.CreateStatsButton(ConsoleKey.Oem5,false,18,1); //backslash
					MouseUI.CreateStatsButton(ConsoleKey.OemPlus,false,19,1); //=
					MouseUI.CreateStatsButton(ConsoleKey.Q,false,20,1);
					MouseUI.CreateStatsButton(ConsoleKey.V,false,24,1);
					MouseUI.CreateStatsButton(ConsoleKey.E,false,7,2);
					MouseUI.CreateMapButton(ConsoleKey.P,false,0,3);
				}
				Q0();
				break;
			case '~': //debug mode 
			{
				if(false){
					List<string> l = new List<string>();
					l.Add("blink");
					l.Add("create chests");
					l.Add("gain power");
					l.Add("spawn monster");
					l.Add("Forget the map");
					l.Add("Heal to full");
					l.Add("Become invulnerable");
					l.Add("get items!");
					l.Add("other");
					l.Add("Use a rune of passage");
					l.Add("See the entire level");
					l.Add("Generate new level");
					l.Add("Create ice or slime");
					l.Add("Spawn shrines");
					l.Add("create trap");
					l.Add("create door");
					l.Add("spawn lots of goblins and lose neck snap");
					l.Add("brushfire test");
					l.Add("detect monsters forever");
					l.Add("get specific items");
					switch(Select("Activate which cheat? ",l)){
					case 0:
					{
						//new Item(ConsumableType.DETONATION,"orb of detonation",'*',Color.White).Use(this);
						new Item(ConsumableType.BLINKING,"orb of detonation",'*',Color.White).Use(this);
						Q1();
						break;
					}
					case 1:
					{
						foreach(Tile t in TilesWithinDistance(3)){
							t.TransformTo(TileType.CHEST);
						}
						Q0();
						//Screen.AnimateExplosion(this,5,new colorchar(Color.RandomIce,'*'),25);
						//Q1();
						break;
					}
					case 2:
					{
						/*int[,] row_displacement = GetDiamondSquarePlasmaFractal(ROWS,COLS);
						int[,] col_displacement = GetDiamondSquarePlasmaFractal(ROWS,COLS);
						colorchar[,] scr = Screen.GetCurrentMap();
						M.actor[p] = null;
						scr[p.row,p.col] = M.VisibleColorChar(p.row,p.col);
						M.actor[p] = this;
						int total_rd = 0;
						int total_cd = 0;
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								total_rd += row_displacement[i,j];
								total_cd += col_displacement[i,j];
							}
						}
						int avg_rd = total_rd / (ROWS*COLS);
						int avg_cd = total_cd / (ROWS*COLS);
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								row_displacement[i,j] -= avg_rd;
								col_displacement[i,j] -= avg_cd;
							}
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(i == p.row && j == p.col){
									Screen.WriteMapChar(i,j,'@',Color.White);
								}
								else{
									row_displacement[i,j] /= 8;
									col_displacement[i,j] /= 8;
									if(U.BoundsCheck(i+row_displacement[i,j],j+col_displacement[i,j])){
										Screen.WriteMapChar(i,j,scr[i+row_displacement[i,j],j+col_displacement[i,j]]);
									}
									else{
										Screen.WriteMapChar(i,j,Screen.BlankChar());
									}
								}
							}
						}
						Global.ReadKey();



						var noise = U.GetNoise(17);
						for(int i=0;i<17;++i){
							for(int j=0;j<17;++j){
								char ch2 = '~';
								if(noise[i,j] > 0.5f){
									ch2 = '^';
								}
								else{
									if(noise[i,j] > 0.1f){
										ch2 = '#';
									}
									else{
										if(noise[i,j] > -0.1f){
											ch2 = '.';
										}
										else{
											if(noise[i,j] > -0.5f){
												ch2 = ',';
											}
										}
									}
								}
								Screen.WriteChar(i,j,ch2);
							}
							//Screen.WriteMapString(i,0,Global.GenerateCharacterName().PadToMapSize());
							//Screen.WriteMapString(i,0,Item.RandomItem().ToString().PadToMapSize());
						}



						pos p1 = new pos(10,9);
						pos p1b = new pos(11,9);
						pos p2 = new pos(10,56);
						pos p2b = new pos(11,56);
						int dist = p1.ApproximateEuclideanDistanceFromX10(1,32) + p2.ApproximateEuclideanDistanceFromX10(1,32);
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(p1.ApproximateEuclideanDistanceFromX10(i,j) + p2.ApproximateEuclideanDistanceFromX10(i,j) <= dist
								|| p1b.ApproximateEuclideanDistanceFromX10(i,j) + p2b.ApproximateEuclideanDistanceFromX10(i,j) <= dist){
									Screen.WriteMapChar(i,j,'.');
								}
								else{
									Screen.WriteMapChar(i,j,'#');
								}
							}
						}
						//Global.ReadKey();
						//M.InitLevel();
						//Move(1,1);
						//List<TileType> tilelist = new List<TileType>{TileType.WALL,TileType.WATER,TileType.VINE,TileType.WAX_WALL,TileType.FLOOR,TileType.BREACHED_WALL,TileType.DOOR_C,TileType.DOOR_O,TileType.CHEST,TileType.STATUE,TileType.FIREPIT,TileType.STALAGMITE,TileType.RUBBLE,TileType.COMBAT_SHRINE,TileType.DEFENSE_SHRINE,TileType.MAGIC_SHRINE,TileType.SPIRIT_SHRINE,TileType.STEALTH_SHRINE,TileType.SPELL_EXCHANGE_SHRINE,TileType.RUINED_SHRINE,TileType.FIRE_GEYSER,TileType.FOG_VENT,TileType.POISON_GAS_VENT,TileType.ICE,TileType.STONE_SLAB,TileType.CHASM,TileType.CRACKED_WALL,TileType.BRUSH,TileType.POPPY_FIELD,TileType.BLAST_FUNGUS,TileType.GLOWING_FUNGUS,TileType.TOMBSTONE,TileType.GRAVE_DIRT,TileType.BARREL,TileType.STANDING_TORCH,TileType.POISON_BULB,TileType.DEMONIC_IDOL,TileType.FIRE_TRAP,TileType.TELEPORT_TRAP,TileType.LIGHT_TRAP,TileType.SLIDING_WALL_TRAP,TileType.GRENADE_TRAP,TileType.SHOCK_TRAP,TileType.ALARM_TRAP,TileType.DARKNESS_TRAP,TileType.POISON_GAS_TRAP,TileType.BLINDING_TRAP,TileType.ICE_TRAP,TileType.PHANTOM_TRAP,TileType.SCALDING_OIL_TRAP,TileType.FLING_TRAP,TileType.STONE_RAIN_TRAP};
						//List<pos> tileposlist = new List<pos>{new pos(0,0),new pos(4,0),new pos(8,0),new pos(12,0),new pos(0,8),new pos(0,9),new pos(0,10),new pos(2,10),new pos(4,10),new pos(8,10),new pos(12,10),new pos(13,10),new pos(14,10),new pos(0,11),new pos(1,11),new pos(2,11),new pos(3,11),new pos(4,11),new pos(5,11),new pos(6,11),new pos(8,11),new pos(9,11),new pos(10,11),new pos(11,11),new pos(12,11),new pos(14,11),new pos(15,11),new pos(0,12),new pos(4,12),new pos(5,12),new pos(10,12),new pos(12,12),new pos(13,12),new pos(14,12),new pos(15,12),new pos(0,13),new pos(1,13),new pos(0,14),new pos(1,14),new pos(2,14),new pos(3,14),new pos(4,14),new pos(5,14),new pos(6,14),new pos(7,14),new pos(8,14),new pos(9,14),new pos(10,14),new pos(11,14),new pos(12,14),new pos(13,14),new pos(14,14)};




						int[] intarray = new int[16];
						intarray[1] = 1;
						intarray[2] = 1;
						intarray[5] = -1;
						intarray[8] = 1;
						intarray[9] = -1;
						intarray[10] = 3;



						Bitmap bmp = new Bitmap("todo_remove.png");
						Bitmap result;
						using(var temp = new Bitmap("result_remove.png")){
							result = new Bitmap(temp);
						}



						//Bitmap result = new Bitmap("result_remove.png");
						//int idx=0;
						foreach(ConsumableType ct in Enum.GetValues(typeof(ConsumableType))){
							if(Item.NameOfItemType(ct) == "other"){
								M.tile[2,2].inv = null;
								Item.Create(ct,2,2);
								colorchar cch = M.VisibleColorChar(2,2);
								int ch_offset = (int)(cch.c) * 16;
								int pos_col = 48 + (idx % 16);
								int pos_row = 5 + idx / 16;
								for(int i=0;i<16;++i){
									for(int j=0;j<16;++j){
										if(bmp.GetPixel(ch_offset + j,i).ToArgb() == System.Drawing.Color.Black.ToArgb()){
											System.Drawing.Color pixel_color = System.Drawing.Color.FromArgb(GLGame.ConvertColor(Screen.ResolveColor(cch.color)).ToArgb());
											result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,pixel_color);
										}
										else{
											result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,System.Drawing.Color.Transparent);
										}
									}
								}
								++idx;
							}
						}



						attrs[AttrType.DETECTING_MONSTERS] = 1;
						for(ActorType at = ActorType.GOBLIN;at <= ActorType.DEMON_LORD;at++){
							if(at != ActorType.MARBLE_HORROR_STATUE && at != ActorType.FINAL_LEVEL_CULTIST){
								M.actor[2,2] = null;
								Actor.Create(at,2,2);
								colorchar cch = M.VisibleColorChar(2,2);
								int ch_offset = (int)(cch.c) * 16;
								int pos_col = 32 + (idx % 16);
								int pos_row = 4 + idx / 16;
								for(int i=0;i<16;++i){
									for(int j=0;j<16;++j){
										if(bmp.GetPixel(ch_offset + j,i).ToArgb() == System.Drawing.Color.Black.ToArgb()){
											System.Drawing.Color pixel_color = System.Drawing.Color.FromArgb(GLGame.ConvertColor(Screen.ResolveColor(cch.color)).ToArgb());
											result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,pixel_color);
											result.SetPixel(pos_col * 16 + j + 16,pos_row * 16 + i,pixel_color);
										}
										else{
											result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,System.Drawing.Color.Transparent);
											result.SetPixel(pos_col * 16 + j + 16,pos_row * 16 + i,System.Drawing.Color.Transparent);
										}
									}
								}
								idx += 2;
							}
						}
						{
							colorchar cch = M.VisibleColorChar(1,1);
							int ch_offset = (int)(cch.c) * 16;
							int pos_col = 32;
							int pos_row = 0;
							for(int i=0;i<16;++i){
								for(int j=0;j<16;++j){
									if(bmp.GetPixel(ch_offset + j,i).ToArgb() == System.Drawing.Color.Black.ToArgb()){
										System.Drawing.Color pixel_color = System.Drawing.Color.FromArgb(GLGame.ConvertColor(Screen.ResolveColor(cch.color)).ToArgb());
										result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,pixel_color);
										result.SetPixel(pos_col * 16 + j + 16,pos_row * 16 + i,pixel_color);
									}
									else{
										result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,System.Drawing.Color.Transparent);
										result.SetPixel(pos_col * 16 + j + 16,pos_row * 16 + i,System.Drawing.Color.Transparent);
									}
								}
							}
						}
						for(ActorType at = ActorType.PHANTOM_ZOMBIE;at <= ActorType.PHANTOM_CONSTRICTOR;at++){
								M.actor[2,2] = null;
							Actor.CreatePhantom(2,2);
							if(!M.actor[2,2].Is(at)){
								at--;
								continue;
							}
								colorchar cch = M.VisibleColorChar(2,2);
								int ch_offset = (int)(cch.c) * 16;
								int pos_col = 32 + (idx % 16);
								int pos_row = 4 + idx / 16;
								for(int i=0;i<16;++i){
									for(int j=0;j<16;++j){
										if(bmp.GetPixel(ch_offset + j,i).ToArgb() == System.Drawing.Color.Black.ToArgb()){
											System.Drawing.Color pixel_color = System.Drawing.Color.FromArgb(GLGame.ConvertColor(Screen.ResolveColor(cch.color)).ToArgb());
											result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,pixel_color);
											result.SetPixel(pos_col * 16 + j + 16,pos_row * 16 + i,pixel_color);
										}
										else{
											result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,System.Drawing.Color.Transparent);
											result.SetPixel(pos_col * 16 + j + 16,pos_row * 16 + i,System.Drawing.Color.Transparent);
										}
									}
								}
								idx += 2;
						}



						int diff = 0;
						foreach(FeatureType ft in Enum.GetValues(typeof(FeatureType))){
							M.tile[2,2] = null;
							Tile.Create(TileType.FLOOR,2,2);
							M.tile[2,2].revealed_by_light = true;
							M.tile[2,2].AddFeature(ft);
							colorchar cch = M.VisibleColorChar(2,2);
							int ch_offset = (int)(cch.c) * 16;
							int pos_col = 16 + (diff % 16);
							int pos_row = diff / 16;
							for(int i=0;i<16;++i){
								for(int j=0;j<16;++j){
									if(bmp.GetPixel(ch_offset + j,i).ToArgb() == System.Drawing.Color.Black.ToArgb()){
										System.Drawing.Color pixel_color = System.Drawing.Color.FromArgb(GLGame.ConvertColor(Screen.ResolveColor(cch.color)).ToArgb());
										result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,pixel_color);
									}
									else{
										result.SetPixel(pos_col * 16 + j,pos_row * 16 + i,System.Drawing.Color.Transparent);
									}
								}
							}
							diff += intarray[idx];
							diff++;
							++idx;
						}



						foreach(TileType tt in tilelist){
							M.tile[2,2] = null;
							pos tilepos = tileposlist[idx];
							Tile.Create(tt,2,2);
							M.tile[2,2].revealed_by_light = true;
							colorchar cch = M.VisibleColorChar(2,2);
							int ch_offset = (int)(cch.c) * 16;
							for(int i=0;i<16;++i){
								for(int j=0;j<16;++j){
									if(bmp.GetPixel(ch_offset + j,i).ToArgb() == System.Drawing.Color.Black.ToArgb()){
										System.Drawing.Color pixel_color = System.Drawing.Color.FromArgb(GLGame.ConvertColor(Screen.ResolveColor(cch.color)).ToArgb());
										result.SetPixel(tilepos.row * 16 + j,tilepos.col * 16 + i,pixel_color);
									}
									else{
										result.SetPixel(tilepos.row * 16 + j,tilepos.col * 16 + i,System.Drawing.Color.Black);
									}
								}
							}
							++idx;
						}*/
						Q0();
						maxmp = 99;
						curmp = maxmp;
						skills[SkillType.MAGIC] = 10;
						foreach(SpellType sp in new List<SpellType>{SpellType.TELEKINESIS,SpellType.COLLAPSE,SpellType.FORCE_PALM,SpellType.GREASE,SpellType.AMNESIA}){
							GainSpell(sp);
							spells_in_order.Add(sp);
						}
						IsHiddenFrom(this);
						/*M.UpdateSafetyMap(player);
						var dijk = U.GetDijkstraMap(M.tile,x=>M.tile[x].BlocksConnectivityOfMap(),new List<pos>{this.p});
						foreach(pos p in M.AllPositions()){
							int v = M.safetymap[p];
							colorchar cch = new colorchar(' ',Color.White);
							if(v == U.DijkstraMin){
								cch.c = '#';
							}
							if(v == U.DijkstraMax){
								cch.c = '!';
							}
							if(v.IsValidDijkstraValue()){
								v /= 10;
								cch.c = (char)('z' - v.Modulo(26));
								cch.color = (Color)(3 - (v+1)/26);
							}
							Screen.WriteMapChar(p.row,p.col,cch);
						}
						Global.ReadKey();
						foreach(pos p in M.AllPositions()){
							int v = M.safetymap[p];
							colorchar cch = new colorchar(' ',Color.White);
							if(v == U.DijkstraMin){
								cch.c = '#';
							}
							if(v == U.DijkstraMax){
								cch.c = '!';
							}
							if(v.IsValidDijkstraValue()){
								v = v/10 + dijk[p];
								if(v > -2){
									cch.c = ':';
								}
								else{
									cch.c = '.';
								}
								cch.c = (char)('z' - v.Modulo(26));
								cch.color = (Color)(3 - (v+1)/26);
							}
							Screen.WriteMapChar(p.row,p.col,cch);
						}
						Global.ReadKey();*/
						break;
					}
					case 3:
					{
						/*ConsoleKeyInfo command2 = Global.ReadKey();
						Screen.WriteMapString(14,14,((int)(command2.KeyChar)).ToString());
						Global.ReadKey();
						List<Tile> line = GetTarget(-1,-1);
						if(line != null){
							Tile t = line.Last();
							if(t != null){
								t.AddOpaqueFeature(FeatureType.FOG);
							}
						}*/
						Actor a = M.SpawnMob(ActorType.LONE_WOLF);
						/*foreach(Actor a in M.AllActors()){
							if(a.type == ActorType.WARG){
								a.attrs[AttrType.WANDERING] = 1;
							}
						}*/
						Q0();
						//M.GenerateFinalLevel();
						break;
					}
					case 4:
					{
						Screen.CursorVisible = false;
						colorchar cch;
						cch.c = ' ';
						cch.color = Color.Black;
						cch.bgcolor = Color.Black;
						foreach(Tile t in M.AllTiles()){
							t.seen = false;
							Screen.WriteMapChar(t.row,t.col,cch);
						}
						Screen.CursorVisible = true;
						Q0();
						break;
					}
					case 5:
						curhp = maxhp;
						Q0();
						break;
					case 6:
						if(!HasAttr(AttrType.INVULNERABLE)){
							attrs[AttrType.INVULNERABLE]++;
							B.Add("On. ");
						}
						else{
							attrs[AttrType.INVULNERABLE] = 0;
							B.Add("Off. ");
						}
						Q0();
						break;
					case 7:
					{
						if(InventoryCount() >= Global.MAX_INVENTORY_SIZE){
							inv = new List<Item>();
						}
						while(InventoryCount() < Global.MAX_INVENTORY_SIZE){
							Item.Create(Item.RandomItem(),this);
						}
						foreach(Item i in inv){
							i.revealed_by_light = true;
						}
						Q0();
						break;
					}
					case 8:
					{
						//int[,] a = GetBinaryNoise(ROWS,COLS);
						/*int[,] a = GetDividedNoise(ROWS,COLS,40);
						int[,] chances = new int[ROWS,COLS];
						int[,] values = new int[ROWS,COLS];
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								bool passable = (a[i,j] == 1);
								if(passable){
									values[i,j] = -1;
								}
								else{
									values[i,j] = 0;
								}
							}
						}
						int minrow = 1;
						int maxrow = ROWS-2;
						int mincol = 1;
						int maxcol = COLS-2;
						int val = 0;
						bool done = false;
						while(!done){
							done = true;
							for(int i=minrow;i<=maxrow;++i){
								for(int j=mincol;j<=maxcol;++j){
									if(values[i,j] == val){
										for(int s=i-1;s<=i+1;++s){
											for(int t=j-1;t<=j+1;++t){
												if(values[s,t] == -1){
													values[s,t] = val + 1;
													done = false;
												}
											}
										}
									}
								}
							}
							++val;
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(a[i,j] == 1){
									//distances[i,j] = values[i,j];
									int k = 5 + values[i,j];
									if(k >= 10){
										chances[i,j] = 10;
									}
									else{
										chances[i,j] = k;
									}
								}
							}
						}
						values = new int[ROWS,COLS];
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								bool passable = (a[i,j] == -1);
								if(passable){
									values[i,j] = -1;
								}
								else{
									values[i,j] = 0;
								}
							}
						}
						val = 0;
						done = false;
						while(!done){
							done = true;
							for(int i=minrow;i<=maxrow;++i){
								for(int j=mincol;j<=maxcol;++j){
									if(values[i,j] == val){
										for(int s=i-1;s<=i+1;++s){
											for(int t=j-1;t<=j+1;++t){
												if(values[s,t] == -1){
													values[s,t] = val + 1;
													done = false;
												}
											}
										}
									}
								}
							}
							++val;
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(a[i,j] == -1){
									//distances[i,j] = -(values[i,j]);
									int k = 5 + values[i,j];
									if(k >= 10){
										chances[i,j] = 0;
									}
									else{
										chances[i,j] = 10 - k;
									}
								}
							}
						}
						DungeonGen.StandardDungeon dungeon1 = new DungeonGen.StandardDungeon();
						char[,] map1 = dungeon1.GenerateStandard();
						DungeonGen.StandardDungeon dungeon2 = new DungeonGen.StandardDungeon();
						char[,] map2 = dungeon2.GenerateCave();
						char[,] map3 = new char[ROWS,COLS];
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(a[i,j] == -1){
									map3[i,j] = map1[i,j];
								}
								else{
									if(a[i,j] == 1){
										map3[i,j] = map2[i,j];
									}
									else{
										if(map1[i,j] == '#'){
											map3[i,j] = map2[i,j];
										}
										else{
											if(map2[i,j] == '#'){
												map3[i,j] = map1[i,j];
											}
											else{
												if(R.CoinFlip()){
													map3[i,j] = map1[i,j];
												}
												else{
													map3[i,j] = map2[i,j];
												}
											}
										}
									}
								}
							}
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								Screen.WriteMapChar(i,j,map3[i,j]);
								if(distances[i,j] > -10){
									if(distances[i,j] < 10){
										if(distances[i,j] < 0){
											Screen.WriteMapChar(i,j,(-distances[i,j]).ToString()[0],Color.DarkMagenta);
										}
										else{
											if(distances[i,j] > 0){
												Screen.WriteMapChar(i,j,distances[i,j].ToString()[0],Color.DarkCyan);
											}
											else{
												Screen.WriteMapChar(i,j,distances[i,j].ToString()[0],Color.DarkGray);
											}
										}
									}
									else{
										Screen.WriteMapChar(i,j,'+',Color.DarkCyan);
									}
								}
								else{
									Screen.WriteMapChar(i,j,'-',Color.DarkMagenta);
								}
							}
						}


						tile().Toggle(null,TileType.BLAST_FUNGUS);


						List<Tile> area = new List<Tile>();
						foreach(Tile t in TilesWithinDistance(3).Where(x=>x.passable && HasLOE(x))){
							t.Toggle(null,TileType.POPPY_FIELD);
							area.Add(t);
						}
						Q.Add(new Event(area,100,EventType.POPPIES));


						tile().Toggle(null,TileType.TOMBSTONE);
						Global.ReadKey();


						List<string> movement = new List<string>{"is immobile","moves quickly","moves slowly"};
						List<string> ability = new List<string>{"can step back after attacking","moves erratically","flies","can use an aggressive stance","uses a ranged attack","uses a burst attack","lunges","has a poisonous attack","drains life","has a powerful but slow attack","grabs its targets","has a knockback attack","has a slowing attack","has a silencing attack","can steal items","explodes when defeated","stays at range",
						"sidesteps when it attacks","has a paralyzing attack","has a stunning attack","is stealthy","appears with others of its type","carries a light source","is invisible in darkness","disrupts nearby spells","regenerates","comes back after death","wears armor","has heightened senses","has hard skin that can blunt edged weapons","casts spells","can reduce its target's attack power","can burrow"};
						List<string> rare_ability = new List<string>{"is attracted to light","is blind in the light","can create illusions of itself","sets itself on fire","throws a bola to slow its targets","dims nearby light sources","screams to terrify its prey","howls to embolden others attacking its prey","breathes poison","is surrounded by a poisonous cloud",
						"is surrounded by a cloud of fog","can summon a minion","can fill the area with sunlight","is resistant to weapons","can turn into a statue","can throw explosives","can create stalagmites","collapses into rubble when defeated","has a fiery attack","can teleport its foes away","can pull its targets closer from a distance","releases spores when attacked","can absorb light to heal","leaves a trail as it travels","breathes fire","can spit blinding poison","lays volatile eggs","can breach nearby walls","is knocked back by blunt weapons","causes attackers to become exhausted","can create a temporary wall","can throw its foes overhead"};
						char randomsymbol = (char)((R.Roll(26)-1) + (int)'a');
						if(R.CoinFlip()){
							randomsymbol = randomsymbol.ToString().ToUpper()[0];
						}
						string s1 = "This monster is a " + Screen.GetColor(Color.RandomAny).ToString().ToLower().Replace("dark","dark ") + " '" + randomsymbol + "'. ";
						string s2 = "It ";
						bool add_move = R.OneIn(5);
						int num_abilities = R.Roll(2) + 1;
						if(R.OneIn(10)){
							++num_abilities;
						}
						int total = num_abilities;
						if(add_move){ ++total; }
						if(add_move){
							--total;
							if(total == 0){
								s2 = s2 + "and " + movement.Random() + ". ";
							}
							else{
								s2 = s2 + movement.Random() + ", ";
							}
						}
						for(int i=num_abilities;i>0;--i){
							--total;
							string a = "";
							if(R.PercentChance(50)){
								a = ability.Random();
							}
							else{
								a = rare_ability.Random();
							}
							if(!s2.Contains(a)){
								if(total == 0){
									s2 = s2 + "and " + a + ". ";
								}
								else{
									s2 = s2 + a + ", ";
								}
							}
							else{
								++i;
								++total;
							}
						}


						if(add_rare){
							--total;
							if(total == 0){
								s2 = s2 + "and " + rare_ability.Random() + ". ";
							}
							else{
								s2 = s2 + rare_ability.Random() + ", ";
							}
						}


						B.Add(s1);
						B.Add(s2);
						if(s2.Contains("casts spells")){
							List<SpellType> all_spells = new List<SpellType>();
							foreach(SpellType spl in Enum.GetValues(typeof(SpellType))){
								all_spells.Add(spl);
							}
							all_spells.Remove(SpellType.NO_SPELL);
							all_spells.Remove(SpellType.NUM_SPELLS);
							string sp = "It can cast ";
							for(int num_spells = R.Roll(4);num_spells > 0;--num_spells){
								if(num_spells == 1){
									sp = sp + "and " + all_spells.RemoveRandom().ToString().ToLower().Replace('_',' ') + ". ";
								}
								else{
									sp = sp + all_spells.RemoveRandom().ToString().ToLower().Replace('_',' ') + ", ";
								}
							}
							B.Add(sp);
						}*/
						Q0();
						foreach(Tile t in M.AllTiles()){
							if(t.passable && DistanceFrom(t) > 4 && R.OneIn(10)){
								ActorType at = (ActorType)R.Between(3,72);
								Actor.Create(at,t.row,t.col);
							}
						}
						break;
					}
					case 9:
						new Item(ConsumableType.PASSAGE,"rune of passage",'&',Color.White).Use(this);
						Q1();
						break;
					case 10:
						foreach(Tile t in M.AllTiles()){
							t.seen = true;
							colorchar ch2 = Screen.BlankChar();
							if(t.IsKnownTrap() || t.IsShrine() || t.Is(TileType.RUINED_SHRINE)){
								t.revealed_by_light = true;
							}
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
							Screen.WriteMapChar(t.row,t.col,ch2);
						}
						//M.Draw();
						foreach(Actor a in M.AllActors()){
							Screen.WriteMapChar(a.row,a.col,new colorchar(a.color,Color.Black,a.symbol));
						}
						Global.ReadKey();
						Q0();
						break;
					case 11:
						for(int i=0;i<1;++i){
							if(M.current_level < 20){
								//M.level_types[M.current_level] = LevelType.Standard;
							}
							M.GenerateLevel();
							/*foreach(Tile t in M.AllTiles()){
								if(t.TilesWithinDistance(1).Any(x=>x.type != TileType.WALL)){
									t.seen = true;
								}
							}
							B.Print(false);
							M.Draw();*/
						}
						Q0();
						break;
					case 12:
					{
						/*PosArray<int> map = new PosArray<int>(ROWS,COLS);
						pos center = new pos(ROWS/2,COLS/2);
						int n = 2;
						foreach(pos p in center.PositionsWithinDistance(n-1)){
							map[p] = 1;
						}
						bool changed = true;
						while(changed){
							changed = false;
							List<pos> list = center.PositionsAtDistance(n);
							while(list.Count > 0){
								pos p = list.RemoveRandom();
								int count = p.PositionsAtDistance(1).Where(x=>map[x] == 1).Count;
								if(R.PercentChance(count*25)){ //this number can be anywhere from ~19 to 25
									map[p] = 1;
									changed = true;
								}
							}
							++n;
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								//pos p = new pos(i,j);
								//if(p.PositionsWithinDistance(1).Where(x=>map[x] == 1).Count >= 5){
								if(map[i,j] == 1){
									Screen.WriteMapChar(i,j,'.',Color.Green);
								}
								else{
									Screen.WriteMapChar(i,j,'~',Color.Blue);
								}
							}
						}
						Global.ReadKey();
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								pos p = new pos(i,j);
								if(p.PositionsWithinDistance(1).Where(x=>map[x] == 1).Count >= 5){
								//if(map[i,j] == 1){
									Screen.WriteMapChar(i,j,'.',Color.Green);
								}
								else{
									Screen.WriteMapChar(i,j,'~',Color.Blue);
								}
							}
						}
						Global.ReadKey();


						level = 10;
						skills[SkillType.COMBAT] = 10;
						skills[SkillType.DEFENSE] = 10;
						skills[SkillType.MAGIC] = 10;
						skills[SkillType.SPIRIT] = 10;
						skills[SkillType.STEALTH] = 10;
						foreach(FeatType f in Enum.GetValues(typeof(FeatType))){
							if(f != FeatType.NO_FEAT && f != FeatType.NUM_FEATS){
								feats[f] = true;
							}
						}



						foreach(Tile t in M.AllTiles()){
							if(t.type == TileType.FLOOR){
								if(R.CoinFlip()){
									t.Toggle(null,TileType.STONE_RAIN_TRAP);
								}
								else{
									t.Toggle(null,TileType.FLING_TRAP);
								}
							}
						}
						for(int i=0;i<10;++i){
							M.SpawnMob(ActorType.FROSTLING);
						}*/
						RefreshDuration(AttrType.ENRAGED,1500);
						Q0();
						break;
					}
					case 13:
					{
						//LevelUp();
							foreach(Tile t in TilesWithinDistance(2)){
								t.TransformTo((TileType)(R.Between(0,4)+(int)TileType.COMBAT_SHRINE));
							}
						Q0();
						break;
					}
					case 14:
					{
						foreach(Tile t in TilesAtDistance(1)){
							t.TransformTo(Tile.RandomTrap());
						}
						Q0();
						break;
					}
					case 15:
					{
							List<Tile> line = GetTargetTile(-1,0,false,false);
							if(line != null){
								Tile t = line.Last();
								if(t != null){
									//t.TransformTo(TileType.DOOR_O);
								if(t.Is(FeatureType.FIRE)){
									t.RemoveFeature(FeatureType.FIRE);
									t.Toggle(null,TileType.DEMONIC_IDOL);
								}
								else{
								t.AddFeature(FeatureType.FIRE);
								}
								}
							}
						Q0();
						break;
					}
					case 16:
					{
						for(int i=0;i<100;++i){
							M.SpawnMob(ActorType.GOBLIN);
						}
						if(HasFeat(FeatType.NECK_SNAP)){
							feats[FeatType.NECK_SNAP] = false;
						}
						Q0();
						break;
					}
					case 17:
					{
						/*List<Tile> list = new List<Tile>();
						while(list.Count < 15){
							int rr = R.Roll(ROWS-2);
							int rc = R.Roll(COLS-2);
							if(M.tile[rr,rc].passable){
								list.AddUnique(M.tile[rr,rc]);
							}
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(M.tile[i,j].passable){
									List<Tile> closest_tiles = list.WhereLeast(x => x.ApproximateEuclideanDistanceFromX10(i,j));
									//List<Tile> closest_tiles = list.WhereLeast(x => new Actor(this,i,j).GetPath(x.row,x.col).Count);
									if(closest_tiles.Count == 2){
										Screen.WriteMapChar(i,j,'=',Color.White);
									}
									else{
										int idx = list.IndexOf(closest_tiles[0]);
										Screen.WriteMapChar(i,j,M.tile[i,j].symbol,(Color)(idx+3));
									}
								}
								else{
									if(!M.tile[i,j].solid_rock){
										Screen.WriteMapChar(i,j,M.tile[i,j].symbol,M.tile[i,j].color);
									}
									else{
										Screen.WriteMapChar(i,j,Screen.BlankChar());
									}
								}
							}
						}


						Screen.Blank();
						Tile[] prev = new Tile[20];
						int idx = 0;
						foreach(Tile t in M.ReachableTilesByDistance(row,col,false,TileType.DOOR_C)){
							Screen.WriteMapChar(t.row,t.col,t.symbol,t.color);
							prev[idx] = t;
							idx = (idx + 1) % 20;
							if(prev[idx] != null){
								Screen.WriteMapChar(prev[idx].row,prev[idx].col,Screen.BlankChar());
							}
							Thread.Sleep(10);
						}*/
						foreach(Actor a in M.AllActors()){
							if(a != this){
								a.Kill();
							}
						}
						UpdateRadius(LightRadius(),0);
						for(int i=1;i<ROWS-1;++i){
							for(int j=1;j<COLS-1;++j){
								M.tile[i,j] = null;
								Tile.Create(TileType.BRUSH,i,j);
							}
						}
						UpdateRadius(0,LightRadius());
						foreach(Tile t in M.AllTiles()){
							if(t.TilesWithinDistance(1).Any(x=>x.type != TileType.WALL)){
								t.seen = true;
							}
						}
						Q.KillEvents(null,EventType.CHECK_FOR_HIDDEN);
						M.wiz_lite = true;
						M.wiz_dark = false;
						B.Print(false);
						M.Draw();
						Q0();
						break;
					}
					case 18:
					{
						Actor a = null;
						a.AName(true);
						if(attrs[AttrType.DETECTING_MONSTERS] == 0){
							attrs[AttrType.DETECTING_MONSTERS] = 1;
						}
						else{
							attrs[AttrType.DETECTING_MONSTERS] = 0;
						}
						Q0();
						break;
					}
					case 19:
					{
						/*List<Tile> line = GetTargetTile(-1,0,false);
						if(line != null){
							Tile t = line.Last();
							if(t != null){
								t.Toggle(null,TileType.CHASM);
								Q.Add(new Event(t,100,EventType.FLOOR_COLLAPSE));
								B.Add("The floor begins to collapse! ");
							}
						}*/
						if(tile().inv == null){
							tile().inv = Item.Create(ConsumableType.TELEPORTAL,row,col);
							//TileInDirection(8).inv = Item.Create(ConsumableType.FLAMES,-1,-1);
							B.Add("You feel something roll beneath your feet. ");
							//magic_trinkets.Add(MagicTrinketType.PENDANT_OF_LIFE);
						}
						Q0();
						break;
					}
					default:
						Q0();
						break;
					}
				}
				else{
					Q0();
				}
				break;
			}
			case ' ':
				Q0();
				break;
			default:
				B.Add("Press '?' for help. ");
				Q0();
				break;
			}
			if(ch != 'x'){
				attrs[AttrType.AUTOEXPLORE] = 0;
			}
		}
		public void PlayerWalk(int dir){
			if(dir > 0){
				if(ActorInDirection(dir) != null){
					if(!ActorInDirection(dir).IsHiddenFrom(this) || ActorInDirection(dir).HasAttr(AttrType.DANGER_SENSED)){
						ActorInDirection(dir).attrs[AttrType.TURNS_VISIBLE] = -1;
						ActorInDirection(dir).attrs[AttrType.NOTICED] = 1;
						ActorInDirection(dir).attrs[AttrType.DANGER_SENSED] = 1;
						if(ActorInDirection(dir).HasAttr(AttrType.TERRIFYING) && CanSee(ActorInDirection(dir)) && !HasAttr(AttrType.CONFUSED,AttrType.ENRAGED,AttrType.MENTAL_IMMUNITY)){
							CheckForFear(TileInDirection(dir));
							Q0();
							return;
						}
						Attack(0,ActorInDirection(dir));
					}
					else{
						if(HasAttr(AttrType.IMMOBILE)){
							if(HasAttr(AttrType.CONFUSED)){
								B.Add("You struggle, forgetting your immobility. ");
								Q1();
								return;
							}
							if(HasAttr(AttrType.ROOTS)){
								B.Add("You're rooted to the ground! ");
							}
							else{
								B.Add("You can't move! ");
							}
							Help.TutorialTip(TutorialTopic.Immobilized);
							Q0();
							return;
						}
						ActorInDirection(dir).attrs[AttrType.TURNS_VISIBLE] = -1;
						ActorInDirection(dir).attrs[AttrType.NOTICED]++;
						if(!IsHiddenFrom(ActorInDirection(dir))){
							B.Add("You walk straight into " + ActorInDirection(dir).AName(true) + "! ");
						}
						else{
							B.Add("You walk straight into " + ActorInDirection(dir).AName(true) + "! ");
							if(!ActorInDirection(dir).HasAttr(AttrType.MINDLESS) && CanSee(ActorInDirection(dir))){
								B.Add(ActorInDirection(dir).the_name + " looks just as surprised as you. ");
							}
							ActorInDirection(dir).player_visibility_duration = -1;
							ActorInDirection(dir).attrs[AttrType.PLAYER_NOTICED]++;
						}
						Q1();
					}
				}
				else{
					Tile t = TileInDirection(dir);
					if(t.passable){
						if(HasAttr(AttrType.IMMOBILE)){
							if(HasAttr(AttrType.CONFUSED)){
								B.Add("You struggle, forgetting your immobility. ");
								Q1();
								return;
							}
							if(HasAttr(AttrType.ROOTS)){
								B.Add("You're rooted to the ground! ");
							}
							else{
								B.Add("You can't move! ");
							}
							Help.TutorialTip(TutorialTopic.Immobilized);
							Q0();
							return;
						}
						if(GrabPreventsMovement(t)){
							List<Actor> grabbers = new List<Actor>();
							foreach(Actor a in ActorsAtDistance(1)){
								if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this)){
									grabbers.Add(a);
								}
							}
							B.Add(grabbers.Random().TheName(true) + " prevents you from moving away! ");
							if(HasAttr(AttrType.CONFUSED)){
								Q1();
							}
							else{
								Q0();
							}
							return;
						}
						if(tile().Is(FeatureType.WEB)){
							if(HasAttr(AttrType.BRUTISH_STRENGTH,AttrType.SLIMED,AttrType.OIL_COVERED)){
								tile().RemoveFeature(FeatureType.WEB);
							}
							else{
								if(R.CoinFlip()){
									if(player.HasLOS(this)){
										B.Add(You("break") + " free. ",this);
									}
									tile().RemoveFeature(FeatureType.WEB);
								}
								else{
									if(player.HasLOS(this)){
										B.Add(You("try",false,true) + " to break free. ",this);
									}
								}
								IncreaseExhaustion(3);
								Q1();
								return;
							}
						}
						for(int i=t.row-1;i<=t.row+1;++i){
							for(int j=t.col-1;j<=t.col+1;++j){
								if(M.actor[i,j] != null && M.actor[i,j].HasAttr(AttrType.TERRIFYING) && CanSee(M.actor[i,j])){
									if(!HasAttr(AttrType.CONFUSED,AttrType.ENRAGED,AttrType.MENTAL_IMMUNITY) && CheckForFear(t)){ //this part will determine whether this move is still allowed - if the player is cornered, for example.
										Q0();
										return;
									}
									else{
										i = t.row + 999;
										break;
									}
								}
							}
						}
						if(!ConfirmsSafetyPrompts(t)){
							Q0();
							return;
						}
						/*if(CanSee(t) && !HasAttr(AttrType.CONFUSED)){
							if(t.IsKnownTrap() && !HasAttr(AttrType.FLYING,AttrType.BRUTISH_STRENGTH) && !t.name.Contains("(safe)")){
								if(!B.YesOrNoPrompt("Really step on " + t.TheName(true) + "?")){
									Q0();
									return;
								}
							}
							if(t.IsBurning() && !IsBurning() && !HasAttr(AttrType.SLIMED,AttrType.NONLIVING)){ //nonliving indicates stoneform
								if(!B.YesOrNoPrompt("Really walk into the fire?")){
									Q0();
									return;
								}
							}
							if(HasAttr(AttrType.OIL_COVERED) && !HasAttr(AttrType.IMMUNE_BURNING,AttrType.IMMUNE_FIRE)){
								bool fire = false;
								foreach(Tile t2 in t.TilesWithinDistance(1)){
									if((t2.actor() != null && t2.actor().IsBurning()) || t2.IsBurning()){
										fire = true;
										break;
									}
								}
								if(fire && !B.YesOrNoPrompt("Really step next to the fire?")){
									Q0();
									return;
								}
							}
							if(t.Is(FeatureType.WEB) && !HasAttr(AttrType.SLIMED,AttrType.OIL_COVERED,AttrType.BRUTISH_STRENGTH) && !B.YesOrNoPrompt("Really walk into the web?")){
								Q0();
								return;
							}
							if(!HasAttr(AttrType.NONLIVING)){
								if(t.Is(FeatureType.CONFUSION_GAS) && !B.YesOrNoPrompt("Really walk into the confusion gas?")){
									Q0();
									return;
								}
								if(t.Is(FeatureType.POISON_GAS) && !HasAttr(AttrType.POISONED) && !B.YesOrNoPrompt("Really walk into the poison gas?")){
									Q0();
									return;
								}
								if(t.Is(FeatureType.SPORES) && (!HasAttr(AttrType.POISONED) || !HasAttr(AttrType.STUNNED)) && !B.YesOrNoPrompt("Really walk into the spores?")){
									Q0();
									return;
								}
								if(t.Is(FeatureType.THICK_DUST) && !B.YesOrNoPrompt("Really walk into the cloud of dust?")){
									Q0();
									return;
								}
							}
						}*/
						if(tile().IsSlippery()){
							if(R.OneIn(5) && !HasAttr(AttrType.FLYING,AttrType.NONEUCLIDEAN_MOVEMENT) && !magic_trinkets.Contains(MagicTrinketType.BOOTS_OF_GRIPPING)){
								B.Add("You slip and almost fall! ");
								QS();
								return;
							}
						}
						if(HasAttr(AttrType.GRABBED)){ //any grab that prevented this movement would have already taken place above. this part checks for slipping from grabs.
							List<Actor> grabbers = new List<Actor>();
							foreach(Actor a in ActorsAtDistance(1)){
								if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this) && a.DistanceFrom(t) > 1){
									grabbers.Add(a);
									a.attrs[AttrType.GRABBING] = 0;
									attrs[AttrType.GRABBED]--;
									if(attrs[AttrType.GRABBED] < 0){
										attrs[AttrType.GRABBED] = 0;
									}
								}
							}
							if(grabbers.Count > 0){
								if(HasAttr(AttrType.BRUTISH_STRENGTH)){
									B.Add("You force your way out of the grab. ");
								}
								else{
									B.Add("You slip out of the grab! ");
								}
							}
						}
						if(IsInvisibleHere() && !IsSilencedHere()){
							foreach(Actor a in ActorsAtDistance(1)){
								if(a.player_visibility_duration < 0 && a.target == player){
									a.player_visibility_duration = -1;
									a.target_location = t;
								}
							}
						}
						if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.IsTrap()){
							t.SetName(Tile.Prototype(t.type).name);
							B.Add("You smash " + t.TheName(true) + ". ");
							t.TurnToFloor();
						}
						if(HasAttr(AttrType.BURNING)){
							if(t.IsWater() && !t.Is(FeatureType.OIL)){
								B.Add("You extinguish the flames. ");
								if(light_radius == 0){
									UpdateRadius(1,0);
								}
								attrs[AttrType.BURNING] = 0;
								Q.KillEvents(this,AttrType.BURNING);
								Fire.burning_objects.Remove(this);
							}
							else{
								if(t.Is(FeatureType.SLIME)){
									B.Add("You cover yourself in slime to remove the flames. ");
									attrs[AttrType.BURNING] = 0;
									if(light_radius == 0){
										UpdateRadius(1,0);
									}
									Q.KillEvents(this,AttrType.BURNING);
									attrs[AttrType.SLIMED] = 1;
									Fire.burning_objects.Remove(this);
									Help.TutorialTip(TutorialTopic.Slimed);
								}
							}
						}
						if(HasAttr(AttrType.SLIMED) && t.IsWater() && !t.Is(FeatureType.FIRE)){
							attrs[AttrType.SLIMED] = 0;
							B.Add("You wash off the slime. ");
						}
						if(HasAttr(AttrType.OIL_COVERED) && t.Is(FeatureType.SLIME)){
							attrs[AttrType.OIL_COVERED] = 0;
							attrs[AttrType.SLIMED] = 1;
							B.Add("You cover yourself in slime to remove the oil. ");
							Help.TutorialTip(TutorialTopic.Slimed);
						}
						if(HasAttr(AttrType.OIL_COVERED) && t.IsWater() && !t.Is(FeatureType.FIRE,FeatureType.OIL)){
							attrs[AttrType.OIL_COVERED] = 0;
							B.Add("You wash off the oil. ");
							t.AddFeature(FeatureType.OIL); //todo: remove this if [o]perate-to-cover-self is added
						}
						if(t.Is(FeatureType.FORASECT_EGG) && !HasAttr(AttrType.FLYING)){
							B.Add("You crush the forasect egg. ");
							t.RemoveFeature(FeatureType.FORASECT_EGG);
							foreach(Actor a in t.ActorsWithinDistance(12)){
								if(a.type == ActorType.FORASECT){
									a.FindPath(t);
								}
							}
						}
						if(t.type == TileType.STAIRS){
							B.Add("There are stairs here - press > to descend. ");
						}
						if(t.IsShrine()){
							B.Add(t.the_name + " glows faintly - press g to touch it. ");
							t.revealed_by_light = true;
							foreach(Tile nearby in t.TilesWithinDistance(2)){
								if(nearby.IsShrine()){
									nearby.revealed_by_light = true;
									if(!nearby.seen && nearby != t){
										colorchar ch2 = nearby.visual;
										ch2.color = Color.Blue;
										M.last_seen[nearby.row,nearby.col] = ch2;
										ch2.color = nearby.color;
										Screen.AnimateMapCell(nearby.row,nearby.col,ch2);
									}
									nearby.seen = true;
								}
							}
							Help.TutorialTip(TutorialTopic.Shrines);
						}
						if(t.Is(TileType.CHEST)){
							B.Add("There is a chest here - press g to open it. ");
						}
						if(t.Is(TileType.POOL_OF_RESTORATION)){
							B.Add("There is a pool of restoration here. ");
							Help.TutorialTip(TutorialTopic.PoolOfRestoration);
						}
						if(t.Is(TileType.FIRE_RIFT) && !HasAttr(AttrType.FLYING,AttrType.CONFUSED)){
							B.Add("That would be certain death! ");
							Q0();
							return;
						}
						if(t.Is(TileType.FIREPIT)){ //some of these could go into a switch if I really wanted to optimize
							B.Add("You tread carefully over the fire pit. ");
						}
						if(t.Is(TileType.FIRE_GEYSER,TileType.FOG_VENT,TileType.POISON_GAS_VENT)){
							t.revealed_by_light = true;
							B.Add("There is " + t.AName(true) + " here. ");
						}
						/*if(t.Is(TileType.CHASM) && !HasAttr(AttrType.FLYING)){
							if(!B.YesOrNoPrompt("Jump into the chasm?")){
								Q0();
								return;
							}
						}*/
						if(t.inv != null){
							t.inv.revealed_by_light = true;
							if(t.inv.quantity > 1){
								B.Add("There are " + t.inv.AName() + " here. ");
							}
							else{
								B.Add("There is " + t.inv.AName() + " here. ");
							}
							//B.Add("You see " + t.inv.AName() + ". ");
						}
						bool trigger_traps = (t.IsTrap() && (!t.name.Contains("(safe)") || HasAttr(AttrType.CONFUSED)));
						if(HasFeat(FeatType.WHIRLWIND_STYLE)){
							WhirlwindMove(t.row,t.col,trigger_traps,null);
						}
						else{
							Move(t.row,t.col,trigger_traps);
						}
						if(t.Is(TileType.GRAVEL) && !HasAttr(AttrType.FLYING)){
							if(!HasAttr(AttrType.GRAVEL_MESSAGE_COOLDOWN)){
								B.Add("The gravel crunches. ",t);
							}
							RefreshDuration(AttrType.GRAVEL_MESSAGE_COOLDOWN,500);
							MakeNoise(3);
						}
						if(t.Is(FeatureType.WEB) && !HasAttr(AttrType.BURNING,AttrType.SLIMED,AttrType.OIL_COVERED,AttrType.BRUTISH_STRENGTH)){
							B.Add("You're stuck in the web! ");
						}
						if(!Help.displayed[TutorialTopic.Recovery] && curhp <= 20 && !HasAttr(AttrType.BURNING,AttrType.ACIDIFIED,AttrType.BLIND,AttrType.POISONED) && !M.AllActors().Any(a=>(a != this && CanSee(a)))){
							Help.TutorialTip(TutorialTopic.Recovery);
						}
						if(!Help.displayed[TutorialTopic.ShinyPlateArmor] && EquippedArmor == Plate && light_radius == 0 && tile().IsLit()){
							Help.TutorialTip(TutorialTopic.ShinyPlateArmor);
						}
						if(!Help.displayed[TutorialTopic.BlastFungus] && (tile().type == TileType.BLAST_FUNGUS || (tile().inv != null && tile().inv.type == ConsumableType.BLAST_FUNGUS))){
							Help.TutorialTip(TutorialTopic.BlastFungus);
						}
						if(!Help.displayed[TutorialTopic.FirePit] && tile().type == TileType.FIREPIT){
							Help.TutorialTip(TutorialTopic.FirePit);
						}
						QS();
					}
					else{
						if(HasAttr(AttrType.BRUTISH_STRENGTH) && !MovementPrevented(t) && t.Is(TileType.CRACKED_WALL,TileType.DOOR_C,TileType.STALAGMITE,TileType.STATUE,TileType.RUBBLE)){
							B.Add("You smash " + t.TheName(true) + ". ");
							t.Smash(0);
							/*if(t.Is(TileType.STALAGMITE)){
								t.Toggle(this);
							}
							else{
								t.TurnToFloor();
							}*/
							if(HasFeat(FeatType.WHIRLWIND_STYLE)){
								WhirlwindMove(t.row,t.col);
							}
							else{
								Move(t.row,t.col);
							}
							QS();
						}
						else{
							if(t.Is(TileType.DOOR_C,TileType.RUBBLE)){
								if(StunnedThisTurn()){
									return;
								}
								if(t.Is(TileType.RUBBLE) && !HasAttr(AttrType.BRUTISH_STRENGTH)){
									IncreaseExhaustion(1);
								}
								t.Toggle(this);
								Q1();
							}
							else{
								if(t.Is(TileType.BARREL,TileType.STANDING_TORCH,TileType.POISON_BULB)){
									if(t.Is(TileType.POISON_BULB) && !B.YesOrNoPrompt("Really break the poison bulb?")){
										Q0();
										return;
									}
									if(t.Is(TileType.BARREL,TileType.STANDING_TORCH) && !t.TileInDirection(DirectionOf(t)).passable){
										B.Add(t.TheName(true) + " is blocked. ");
										if(HasAttr(AttrType.BLIND) && !t.seen){
											t.seen = true;
											colorchar ch2 = Screen.BlankChar();
											ch2.c = t.symbol;
											ch2.color = Color.Blue;
											M.last_seen[t.row,t.col] = ch2;
											Screen.WriteMapChar(t.row,t.col,ch2);
										}
										Q0();
									}
									else{
										t.Bump(DirectionOf(t));
										Q1();
									}
								}
								else{
									if(t.Is(TileType.DEMONIC_IDOL)){
										if(StunnedThisTurn()){
											return;
										}
										if(t.name.Contains("damaged") || (HasAttr(AttrType.BRUTISH_STRENGTH) && !MovementPrevented(t))){
											if(HasAttr(AttrType.BRUTISH_STRENGTH)){
												B.Add("You smash the demonic idol. ");
											}
											else{
												B.Add("You destroy the demonic idol. ");
											}
											t.Smash(0);
											if(Global.GAME_OVER){
												return;
											}
											if(HasAttr(AttrType.BRUTISH_STRENGTH) && !MovementPrevented(t) && !Global.GAME_OVER){
												if(HasFeat(FeatType.WHIRLWIND_STYLE)){
													WhirlwindMove(t.row,t.col);
												}
												else{
													Move(t.row,t.col);
												}
											}
											Q1();
										}
										else{
											if(t.name.Contains("scratched")){
												B.Add("You damage the demonic idol. ");
												t.SetName("demonic idol (damaged)");
												Q1();
											}
											else{
												B.Add("You scratch the demonic idol. ");
												t.SetName("demonic idol (scratched)");
												Q1();
											}
										}
									}
									else{
										bool wall_slide = false;
										if(!HasAttr(AttrType.CONFUSED) && !Global.Option(OptionType.NO_WALL_SLIDING)){
											if(t.seen && TileInDirection(dir.RotateDir(true)).seen && TileInDirection(dir.RotateDir(false)).seen){
												if(TileInDirection(dir.RotateDir(true)).IsPassableOrDoor() && !TileInDirection(dir.RotateDir(false)).IsPassableOrDoor()){
													PlayerWalk(dir.RotateDir(true));
													wall_slide = true;
												}
												else{
													if(TileInDirection(dir.RotateDir(false)).IsPassableOrDoor() && !TileInDirection(dir.RotateDir(true)).IsPassableOrDoor()){
														PlayerWalk(dir.RotateDir(false));
														wall_slide = true;
													}
												}
											}
										}
										if(!wall_slide){
											if(HasAttr(AttrType.CONFUSED)){
												B.Add("You stumble into " + t.a_name + ". ");
											}
											else{
												B.Add("There is " + t.a_name + " in the way. ");
											}
											if(HasAttr(AttrType.BLIND) && !t.seen){
												t.seen = true;
												colorchar ch2 = Screen.BlankChar();
												ch2.c = t.symbol;
												ch2.color = Color.Blue;
												M.last_seen[t.row,t.col] = ch2;
												Screen.WriteMapChar(t.row,t.col,ch2);
											}
											if(t.type == TileType.CRACKED_WALL){
												Help.TutorialTip(TutorialTopic.CrackedWall);
											}
											if(t.type == TileType.STONE_SLAB){
												Help.TutorialTip(TutorialTopic.StoneSlab);
											}
											if(HasAttr(AttrType.CONFUSED)){
												Q1();
											}
											else{
												Q0();
											}
										}
									}
								}
							}
						}
					}
				}
			}
			else{
				Q0();
			}
		}
		public bool ConfirmsSafetyPrompts(Tile t){
			if(CanSee(t) && !HasAttr(AttrType.CONFUSED)){
				if(t.IsKnownTrap() && !HasAttr(AttrType.FLYING,AttrType.BRUTISH_STRENGTH) && !t.name.Contains("(safe)")){
					if(!B.YesOrNoPrompt("Really step on " + t.TheName(true) + "?")){
						return false;
					}
				}
				if(t.IsBurning() && !IsBurning() && !HasAttr(AttrType.SLIMED,AttrType.NONLIVING)){ //nonliving indicates stoneform
					if(!B.YesOrNoPrompt("Really walk into the fire?")){
						return false;
					}
				}
				if(HasAttr(AttrType.OIL_COVERED) && !HasAttr(AttrType.IMMUNE_BURNING,AttrType.IMMUNE_FIRE)){
					bool fire = false;
					foreach(Tile t2 in t.TilesWithinDistance(1)){
						if((t2.actor() != null && t2.actor().IsBurning()) || t2.IsBurning()){
							fire = true;
							break;
						}
					}
					if(fire && !B.YesOrNoPrompt("Really step next to the fire?")){
						return false;
					}
				}
				if(t.Is(FeatureType.WEB) && !HasAttr(AttrType.SLIMED,AttrType.OIL_COVERED,AttrType.BRUTISH_STRENGTH) && !B.YesOrNoPrompt("Really walk into the web?")){
					return false;
				}
				if(!HasAttr(AttrType.NONLIVING)){
					if(t.Is(FeatureType.CONFUSION_GAS) && !B.YesOrNoPrompt("Really walk into the confusion gas?")){
						return false;
					}
					if(t.Is(FeatureType.POISON_GAS) && !HasAttr(AttrType.POISONED) && !B.YesOrNoPrompt("Really walk into the poison gas?")){
						return false;
					}
					if(t.Is(FeatureType.SPORES) && (!HasAttr(AttrType.POISONED) || !HasAttr(AttrType.STUNNED)) && !B.YesOrNoPrompt("Really walk into the spores?")){
						return false;
					}
					if(t.Is(FeatureType.THICK_DUST) && !B.YesOrNoPrompt("Really walk into the cloud of dust?")){
						return false;
					}
				}
			}
			return true;
		}
		private void WhirlwindMove(int row,int col){ WhirlwindMove(row,col,true,null); }
		private void WhirlwindMove(int row,int col,bool trigger_traps,List<Actor> excluded){
			List<Actor> previously_adjacent = new List<Actor>();
			foreach(Actor a in ActorsAtDistance(1)){
				if(!a.IsHiddenFrom(this) && (excluded == null || !excluded.Contains(a))){
					previously_adjacent.Add(a);
				}
			}
			Move(row,col,trigger_traps);
			if(previously_adjacent.Count > 0){
				List<Actor> still_adjacent = new List<Actor>();
				foreach(Actor a in ActorsAtDistance(1)){
					if(previously_adjacent.Contains(a)){
						still_adjacent.Add(a);
					}
				}
				if(still_adjacent.Count > 0){
					if(HasAttr(AttrType.STUNNED) && R.OneIn(3)){
						B.Add(You("stagger") + ". ",this);
					}
					else{
						if(exhaustion == 100 && R.CoinFlip()){
							B.Add(You("fumble") + " from exhaustion. ",this);
						}
						else{
							bool possessed = false;
							if(EquippedWeapon.status[EquipmentStatus.POSSESSED] && R.CoinFlip()){
								List<Actor> actors = ActorsWithinDistance(1);
								Actor chosen = actors.Random();
								if(chosen == this){
									possessed = true;
									B.Add(Your() + " possessed " + EquippedWeapon.NameWithEnchantment() + " tries to attack " + the_name + "! ",this);
									B.Add(You("fight") + " it off! ",this);
								}
							}
							if(!possessed){
								while(still_adjacent.Count > 0){
									Actor a = still_adjacent.RemoveRandom();
									Attack(0,a,true);
								}
							}
						}
					}
				}
			}
		}
		private bool CheckForFear(Tile t){
			int least_frightening_value = 99;
			List<Tile> least_frightening = new List<Tile>();
			List<Actor> attackable_actors = new List<Actor>();
			foreach(Tile neighbor in TilesAtDistance(1)){
				if(neighbor.passable){
					if(neighbor.actor() != null && CanSee(neighbor.actor())){
						if(!neighbor.actor().HasAttr(AttrType.TERRIFYING)){
							attackable_actors.Add(neighbor.actor());
						}
						continue;
					}
					int value = 0;
					foreach(int dir2 in U.EightDirections){
						Actor n2 = neighbor.ActorInDirection(dir2);
						if(n2 != null && n2.HasAttr(AttrType.TERRIFYING) && CanSee(n2)){
							++value;
						}
					}
					if(value < least_frightening_value){
						least_frightening_value = value;
						least_frightening.Clear();
						least_frightening.Add(neighbor);
					}
					else{
						if(value == least_frightening_value){
							least_frightening.Add(neighbor);
						}
					}
				}
			}
			foreach(Actor a in attackable_actors){
				least_frightening.Add(a.tile());
			}
			if(!least_frightening.Contains(t)){
				B.Add("You're too afraid! ");
				List<pos> cells = new List<pos>();
				List<colorchar> chars = new List<colorchar>();
				foreach(Tile neighbor in TilesAtDistance(1)){
					if(neighbor.passable && !least_frightening.Contains(neighbor)){
						cells.Add(neighbor.p);
						colorchar cch = M.VisibleColorChar(neighbor.row,neighbor.col);
						cch.bgcolor = Color.DarkMagenta;
						if(!CanSee(neighbor)){
							cch.color = Color.Black;
							cch.c = ' ';
						}
						chars.Add(cch);
					}
				}
				AnimateVisibleMapCells(cells,chars);
				return true;
			}
			return false;
		}
		private void PrintAggressionMessage(){
			if(target == null){
				return;
			}
			bool within_aggression_range = true;
			if(target != null && !HasAttr(AttrType.AGGRESSIVE) && curhp == maxhp && type != ActorType.BLOOD_MOTH){
				if(HasAttr(AttrType.TERRITORIAL) && DistanceFrom(target) > 3){
					within_aggression_range = false;
				}
				else{
					if(DistanceFrom(target) > 12){
						within_aggression_range = false;
					}
				}
			}
			if(type == ActorType.BLOOD_MOTH && (target == null || !HasLOS(target))){
				within_aggression_range = false;
			}
			bool message_printed = false;
			if(within_aggression_range){
				message_printed = true;
				switch(type){
				case ActorType.SPECIAL:
				case ActorType.DIRE_RAT:
					//B.Add(TheName(true) + " squeaks at you. ");
					B.Add(TheName(true) + " squeaks at " + target.TheName(true) + ". ",this,target);
					MakeNoise(4);
					break;
				case ActorType.GOBLIN:
				case ActorType.GOBLIN_ARCHER:
				case ActorType.GOBLIN_SHAMAN:
					B.Add(TheName(true) + " growls. ");
					MakeNoise(4);
					break;
				case ActorType.BLOOD_MOTH:
					if(target == player && !M.wiz_lite && !M.wiz_dark && player.LightRadius() > 0 && HasLOS(player)){
						B.Add(the_name + " notices your light. ",this);
					}
					break;
				case ActorType.CULTIST:
				case ActorType.ROBED_ZEALOT:
					B.Add(TheName(true) + " yells. ");
					MakeNoise(4);
					break;
				case ActorType.FINAL_LEVEL_CULTIST:
					B.Add(TheName(true) + " yells. ");
					break;
				case ActorType.ZOMBIE:
					B.Add(TheName(true) + " moans. Uhhhhhhghhh. ");
					MakeNoise(4);
					break;
				case ActorType.LONE_WOLF:
					B.Add(TheName(true) + " snarls at " + target.TheName(true) + ". ",this,target);
					MakeNoise(4);
					break;
				case ActorType.FROSTLING:
					B.Add(TheName(true) + " makes a chittering sound. ");
					MakeNoise(4);
					break;
				case ActorType.SWORDSMAN:
				case ActorType.BERSERKER:
				case ActorType.CRUSADING_KNIGHT:
				case ActorType.ALASI_BATTLEMAGE:
				case ActorType.ALASI_SCOUT:
				case ActorType.ALASI_SENTINEL:
				case ActorType.ALASI_SOLDIER:
					B.Add(TheName(true) + " shouts. ");
					MakeNoise(4);
					break;
				case ActorType.BANSHEE:
					B.Add(TheName(true) + " shrieks. ");
					MakeNoise(4);
					break;
				case ActorType.WARG:
					B.Add(TheName(true) + " snarls viciously. ");
					MakeNoise(4);
					break;
				case ActorType.DERANGED_ASCETIC:
					B.Add(the_name + " adopts a fighting stance. ",this);
					break;
				case ActorType.CAVERN_HAG:
					B.Add(TheName(true) + " cackles. ");
					MakeNoise(4);
					break;
				case ActorType.OGRE_BARBARIAN:
					B.Add(TheName(true) + " bellows at " + target.TheName(true) + ". ",this,target);
					MakeNoise(4);
					break;
				case ActorType.CYCLOPEAN_TITAN:
					B.Add(TheName(true) + " roars. ");
					MakeNoise(4);
					break;
				case ActorType.GIANT_SLUG:
				case ActorType.MUD_ELEMENTAL:
				case ActorType.CORROSIVE_OOZE:
					B.Add(TheName(true) + " makes a squelching sound. ");
					break;
				case ActorType.SHADOW:
				case ActorType.SPITTING_COBRA:
					B.Add(TheName(true) + " hisses faintly. ");
					break;
					//B.Add(TheName(true) + " hisses. ");
					//break;
				case ActorType.ORC_GRENADIER:
				case ActorType.ORC_WARMAGE:
					B.Add(TheName(true) + " snarls loudly. ");
					MakeNoise(4);
					break;
				case ActorType.ENTRANCER:
					B.Add(the_name + " stares at " + target.TheName(true) + " for a moment. ",this);
					break;
				case ActorType.STONE_GOLEM:
				case ActorType.MACHINE_OF_WAR:
					B.Add(the_name + " starts moving. ",this);
					break;
				case ActorType.NECROMANCER:
					B.Add(TheName(true) + " starts chanting in low tones. ");
					break;
				case ActorType.TROLL:
				case ActorType.TROLL_BLOODWITCH:
					B.Add(TheName(true) + " growls viciously. ");
					MakeNoise(4);
					break;
				case ActorType.FORASECT:
					B.Add(TheName(true) + " makes a clicking sound. ");
					MakeNoise(4);
					break;
				case ActorType.GOLDEN_DART_FROG:
				case ActorType.FLAMETONGUE_TOAD:
					B.Add(TheName(true) + " croaks. ");
					MakeNoise(4);
					break;
				case ActorType.WILD_BOAR:
					B.Add(TheName(true) + " grunts at " + target.TheName(true) + ". ",this,target);
					MakeNoise(4);
					break;
				case ActorType.VULGAR_DEMON:
					B.Add(the_name + " flicks its forked tongue. ",this);
					break;
				case ActorType.SKITTERMOSS:
					B.Add(TheName(true) + " rustles. ");
					break;
				case ActorType.CARNIVOROUS_BRAMBLE:
				case ActorType.MIMIC:
				case ActorType.MUD_TENTACLE:
				case ActorType.MARBLE_HORROR:
				case ActorType.MARBLE_HORROR_STATUE:
				case ActorType.LASHER_FUNGUS:
				case ActorType.BLADE:
				case ActorType.BEAST_DEMON:
				case ActorType.DEMON_LORD:
				case ActorType.FROST_DEMON:
				case ActorType.MINOR_DEMON:
				case ActorType.IMPOSSIBLE_NIGHTMARE:
					break;
				case ActorType.CLOUD_ELEMENTAL:
					if(player.CanSee(this)){
						B.Add("You hear a peal of thunder from the cloud elemental. ");
					}
					else{
						B.Add("You hear a peal of thunder. ");
					}
					MakeNoise(4);
					break;
				default:
					message_printed = false;
					break;
				}
			}
			if(!message_printed){
				if(player_visibility_duration >= 0 && type != ActorType.BLOOD_MOTH && target == player){
					B.Add(the_name + " notices you. ",this);
				}
			}
			else{
				attrs[AttrType.AGGRESSION_MESSAGE_PRINTED] = 1;
			}
		}
		public void InputAI(){
			if(type == ActorType.DREAM_SPRITE && HasAttr(AttrType.COOLDOWN_2) && target != null){
				bool no_los_needed = !target.CanSee(this);
				Tile t = target.TilesAtDistance(DistanceFrom(target)).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(this) > 1 && (no_los_needed || target.CanSee(x))).Random();
				if(t == null){ //gradually loosening the restrictions on placement...
					t = target.TilesAtDistance(DistanceFrom(target)).Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = target.TilesWithinDistance(12).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(target) >= this.DistanceFrom(target) && x.DistanceFrom(this) > 1 && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = target.TilesWithinDistance(12).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(target) >= this.DistanceFrom(target) && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = TilesAtDistance(2).Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = TilesWithinDistance(6).Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = M.AllTiles().Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t != null){
					attrs[AttrType.COOLDOWN_2] = 0;
					if(group == null){
						group = new List<Actor>{this};
					}
					Actor clone = Create(ActorType.DREAM_SPRITE_CLONE,t.row,t.col,TiebreakerAssignment.InsertAfterCurrent);
					clone.speed = 100;
					bool seen = target.CanSee(clone);
					clone.player_visibility_duration = -1;
					group.Add(clone);
					clone.group = group;
					group.Randomize();
					List<Tile> valid_tiles = new List<Tile>();
					foreach(Actor a in group){
						valid_tiles.Add(a.tile());
					}
					Tile newtile = valid_tiles.Random();
					if(newtile != tile()){
						Move(newtile.row,newtile.col,false);
					}
					if(seen){
						B.Add("Another " + name + " appears! ");
					}
				}
			}
			if(type == ActorType.MACHINE_OF_WAR){
				attrs[AttrType.COOLDOWN_1]++;
				if(attrs[AttrType.COOLDOWN_1] == 16){
					B.Add(the_name + " vents fire! ",this);
					if(HasAttr(AttrType.FROZEN)){
						TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,1,null);
					}
					foreach(Tile t in TilesWithinDistance(1)){
						if(t.actor() != null && t.actor() != this){
							if(t.actor().TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(5,6),this,a_name)){
								t.actor().ApplyBurning();
							}
						}
						t.ApplyEffect(DamageType.FIRE);
					}
					attrs[AttrType.COOLDOWN_1] = 0;
				}
			}
			bool no_act = false;
			if(!no_act && HasAttr(AttrType.CONFUSED)){
				ConfusedMove();
				no_act = true;
			}
			if(!no_act && HasAttr(AttrType.BLIND)){
				Stagger();
				no_act = true;
			}
			if(!no_act && HasAttr(AttrType.ENRAGED)){
				EnragedMove();
				no_act = true;
			}
			bool aware_of_player = CanSee(player);
			if(HasAttr(AttrType.SEES_ADJACENT_PLAYER)){
				if(DistanceFrom(player) == 1){ //this allows them to attack when the player is shadow cloaked
					aware_of_player = true;
				}
				else{
					attrs[AttrType.SEES_ADJACENT_PLAYER] = 0;
				}
			}
			if(target == player && player.tile() == target_location && HasLOE(player)){
				aware_of_player = true;
			}
			if(target == player && player_visibility_duration == -1 && DistanceFrom(player) == 1){ //the comparison to -1 exactly is what makes them lose track of the player after a turn of movement - i think.
				aware_of_player = true;
			}
			if(aware_of_player){
				if(!Is(ActorType.BLADE) || target == null){
					target = player;
					target_location = M.tile[player.row,player.col];
					path.Clear();
				}
				player_visibility_duration = -1;
			}
			else{
				bool might_notice = false;
				if((IsWithinSightRangeOf(player.row,player.col) || (player.tile().IsLit() && !HasAttr(AttrType.BLINDSIGHT))) //if they're stealthed and nearby...
					&& HasLOS(player.row,player.col) && (!player.IsInvisibleHere() || HasAttr(AttrType.BLINDSIGHT))){ //((removed player_noticed check from this line))
					might_notice = true;
				}
				if(type == ActorType.CLOUD_ELEMENTAL){
					List<pos> cloud = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].features.Contains(FeatureType.FOG));
					foreach(pos p2 in cloud){
						if(player.DistanceFrom(p2) <= 12){
							if(M.tile[p2].HasLOS(player)){
								might_notice = true;
								break;
							}
						}
					}
				}
				if(player.HasFeat(FeatType.CORNER_CLIMB) && DistanceFrom(player) > 1 && !player.tile().IsLit()){
					List<pos> valid_open_doors = new List<pos>();
					foreach(int dir in U.DiagonalDirections){
						if(TileInDirection(dir).type == TileType.DOOR_O){
							valid_open_doors.Add(TileInDirection(dir).p);
						}
					}
					if(SchismExtensionMethods.Extensions.ConsecutiveAdjacent(player.p,x=>valid_open_doors.Contains(x) || M.tile[x].Is(TileType.WALL,TileType.CRACKED_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR,TileType.STATUE,TileType.STONE_SLAB,TileType.WAX_WALL)) >= 5){
						might_notice = false;
					}
				}
				if(type == ActorType.ORC_WARMAGE && HasAttr(AttrType.DETECTING_MOVEMENT) && player_visibility_duration >= 0 && !player.HasAttr(AttrType.TURNS_HERE) && DistanceFrom(player) <= 12){
					might_notice = true;
				}
				if(!no_act && might_notice){
					int multiplier = HasAttr(AttrType.KEEN_SENSES)? 5 : 10; //animals etc. are approximately twice as hard to sneak past
					int stealth = player.TotalSkill(SkillType.STEALTH);
					if(HasAttr(AttrType.BLINDSIGHT) && !player.tile().IsLit()){ //if this monster has blindsight, take away the stealth bonus for being in darkness
						stealth -= 2;
					}
					bool noticed = false;
					if(type == ActorType.ORC_WARMAGE && HasAttr(AttrType.DETECTING_MOVEMENT) && !player.HasAttr(AttrType.TURNS_HERE) && DistanceFrom(player) <= 12 && HasLOS(player)){
						noticed = true;
					}
					if(stealth * DistanceFrom(player) * multiplier - player_visibility_duration++*5 < R.Roll(1,100)){
						noticed = true;
					}
					if(noticed){
						if(type == ActorType.BLADE){
							if(target == null){
								target = player;
								target_location = M.tile[player.row,player.col];
								path.Clear();
							}
						}
						else{
							target = player;
							target_location = M.tile[player.row,player.col];
							path.Clear();
							PrintAggressionMessage();
							Q1();
							no_act = true;
						}
						player_visibility_duration = -1;
						attrs[AttrType.PLAYER_NOTICED]++;
						if(group != null){
							foreach(Actor a in group){
								if(a != this && DistanceFrom(a) < 3){
									a.player_visibility_duration = -1;
									a.attrs[AttrType.PLAYER_NOTICED]++;
									a.target = player;
									a.target_location = M.tile[player.row,player.col];
								}
							}
						}
					}
				}
				else{
					if(player_visibility_duration >= 0){ //if they hadn't seen the player yet...
						player_visibility_duration = 0;
					}
					else{
						int alerted = 1 + attrs[AttrType.ALERTED];
						int turns_required = 10 * alerted * alerted;
						if(target_location == null && player_visibility_duration-- <= -turns_required){
							attrs[AttrType.ALERTED]++;
							player_visibility_duration = 0;
							attrs[AttrType.AGGRESSION_MESSAGE_PRINTED] = 0;
							target = null;
						}
					}
				}
			}
			if(target != null && !HasAttr(AttrType.AGGRESSIVE) && curhp == maxhp){
				if(HasAttr(AttrType.TERRITORIAL) && DistanceFrom(target) > 3){
					target = null;
					target_location = null;
				}
				else{
					if(DistanceFrom(target) > 12){
						target = null;
						target_location = null;
					}
				}
			}
			if(type == ActorType.DARKNESS_DWELLER){
				if((tile().IsLit() && tile().light_value > 0) || M.wiz_lite){
					if(attrs[AttrType.COOLDOWN_1] < 7){
						if(!HasAttr(AttrType.COOLDOWN_1) && player.HasLOS(this)){
							B.Add(the_name + " is blinded by the light! ",this);
						}
						RefreshDuration(AttrType.BLIND,100,the_name + " can see again. ",this);
						attrs[AttrType.COOLDOWN_1]++;
						attrs[AttrType.COOLDOWN_2] = 7;
					}
				}
				else{
					Event e = Q.FindAttrEvent(this,AttrType.BLIND);
					if(e == null || e.delay == 100){
						RefreshDuration(AttrType.BLIND,0);
					}
					if(attrs[AttrType.COOLDOWN_2] > 0){
						attrs[AttrType.COOLDOWN_2]--;
						if(!HasAttr(AttrType.COOLDOWN_2)){
							attrs[AttrType.COOLDOWN_1] = 0;
							B.Add(Your() + " eyes adjust to the darkness. ",this);
						}
					}
				}
			}
			/*if(Is(ActorType.DARKNESS_DWELLER)){ //this is checked before & after the dweller moves, but the duration is only updated after.
				if((tile().IsLit() && tile().light_value > 0) || M.wiz_lite){
					RefreshDuration(AttrType.BLIND,100,the_name + " is no longer blinded. ",this);
				}
				if(false && !HasAttr(AttrType.COOLDOWN_2)){
					if((tile().IsLit() && tile().light_value > 0) || M.wiz_lite){ //light_value check prevents glowing fungus from blinding them
						if(player.HasLOS(this)){
							B.Add(the_name + " is blinded by the light! ",this);
						}
						//attrs[AttrType.BLIND]++;
						attrs[AttrType.COOLDOWN_2] = 5;
						RefreshDuration(AttrType.BLIND,R.Between(5,6)*100,the_name + " is no longer blinded. ",this);
						//Q.Add(new Event(this,(R.Roll(2)+4)*100,AttrType.BLIND,the_name + " is no longer blinded. ",this));
					}
				}
			}*/
			if(type == ActorType.MARBLE_HORROR && tile().IsLit()){
				B.Add("The marble horror reverts to its statue form. ",this);
				type = ActorType.MARBLE_HORROR_STATUE;
				SetName("marble horror statue");
				attrs[AttrType.IMMOBILE] = 1;
				attrs[AttrType.INVULNERABLE] = 1;
				if(HasAttr(AttrType.BURNING)){
					RefreshDuration(AttrType.BURNING,0);
				}
				attrs[AttrType.IMMUNE_BURNING] = 1;
				if(curhp > 0){
					curhp = maxhp;
				}
			}
			if(type == ActorType.MARBLE_HORROR_STATUE && !tile().IsLit()){
				B.Add("The marble horror animates once more. ",this);
				type = ActorType.MARBLE_HORROR;
				SetName("marble horror");
				attrs[AttrType.IMMOBILE] = 0;
				attrs[AttrType.INVULNERABLE] = 0;
				attrs[AttrType.IMMUNE_BURNING] = 0;
			}
			if(type == ActorType.CORPSETOWER_BEHEMOTH && tile().Is(TileType.FLOOR)){
				tile().Toggle(null,TileType.GRAVE_DIRT);
				bool found = false;
				foreach(Event e in Q.list){
					if(!e.dead && e.type == EventType.GRAVE_DIRT){
						e.area.Add(tile());
						found = true;
						break;
					}
				}
				if(!found){
					Q.Add(new Event(new List<Tile>{tile()},100,EventType.GRAVE_DIRT));
				}
			}
			int danger_threshold = (target != null? 1 : 0);
			if(!no_act && GetDangerRating(tile()) > danger_threshold){
				if(HasAttr(AttrType.NONEUCLIDEAN_MOVEMENT) && target != null){
					List<Tile> safest = target.TilesWithinDistance(DistanceFrom(target)+1).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(target) >= DistanceFrom(target)-1).WhereLeast(x=>GetDangerRating(x));
					if(CanSee(target)){
						if(HasAttr(AttrType.KEEPS_DISTANCE)){
							safest = safest.WhereGreatest(x=>x.DistanceFrom(target));
						}
						else{
							safest = safest.WhereLeast(x=>x.DistanceFrom(target));
						}
					}
					if(safest.Count > 0){
						Tile t = safest.Random();
						Move(t.row,t.col);
						QS();
						no_act = true;
					}
				}
				else{
					List<Tile> safest = TilesWithinDistance(1).Where(x=>x.passable && x.actor() == null).WhereLeast(x=>GetDangerRating(x));
					if(target != null && CanSee(target)){
						if(HasAttr(AttrType.KEEPS_DISTANCE)){
							safest = safest.WhereGreatest(x=>x.DistanceFrom(target));
						}
						else{
							safest = safest.WhereLeast(x=>x.DistanceFrom(target));
						}
					}
					if(safest.Count > 0){
						AI_Step(safest.Random());
						QS();
						no_act = true;
					}
				}
			}
			if(type == ActorType.MECHANICAL_KNIGHT && attrs[AttrType.COOLDOWN_1] != 1){
				attrs[AttrType.MECHANICAL_SHIELD] = 1; //if the knight dropped its guard, it regains its shield here (unless it has no arms)
			}
			if(group != null && group.Count == 0){ //this shouldn't happen, but does. this stops it from crashing.
				group = null;
			}
			if(!no_act){
				if(Is(ActorType.BLOOD_MOTH,ActorType.GHOST,ActorType.BLADE,ActorType.MINOR_DEMON,ActorType.FROST_DEMON,ActorType.BEAST_DEMON,ActorType.DEMON_LORD) || (type == ActorType.BERSERKER && HasAttr(AttrType.COOLDOWN_2))){
					ActiveAI();
				}
				else{
					if(target != null){
						if(CanSee(target) || (target == player && aware_of_player)){
							ActiveAI();
						}
						else{
							SeekAI();
						}
					}
					else{
						IdleAI();
					}
				}
			}
			/*if(Is(ActorType.DARKNESS_DWELLER)){
				if((tile().IsLit() && tile().light_value > 0) || M.wiz_lite){ //light_value check prevents glowing fungus from blinding them
					RefreshDuration(AttrType.BLIND,100,the_name + " is no longer blinded. ",this);
				}/*
				/*if(HasAttr(AttrType.COOLDOWN_2)){
					if((tile().IsLit() && tile().light_value > 0) || M.wiz_lite){ //light_value check prevents glowing fungus from blinding them
						attrs[AttrType.COOLDOWN_2] = 5;
					}
					else{
						attrs[AttrType.COOLDOWN_2]--;
						if(!HasAttr(AttrType.COOLDOWN_2)){
							B.Add(Your() + " eyes adjust to the darkness. ",this);
						}
					}
				}
				else{
					if((tile().IsLit() && tile().light_value > 0) || M.wiz_lite){ //light_value check prevents glowing fungus from blinding them
						if(player.HasLOS(this)){
							B.Add(the_name + " is blinded by the light! ",this);
						}
						//attrs[AttrType.BLIND]++;
						attrs[AttrType.COOLDOWN_2] = 5;
						//Q.Add(new Event(this,(R.Roll(2)+4)*100,AttrType.BLIND,the_name + " is no longer blinded. ",this));
						RefreshDuration(AttrType.BLIND,R.Between(5,6)*100,the_name + " is no longer blinded. ",this);
					}
				}*/
			//}
			if(type == ActorType.SHADOW){
				CalculateDimming();
			}
			if(type == ActorType.STALKING_WEBSTRIDER && !HasAttr(AttrType.BURROWING) && !tile().Is(FeatureType.WEB,FeatureType.FIRE)){
				if(target != null && (CanSee(target) || target == player && aware_of_player)){ //not while wandering, just while chasing the player.
					tile().AddFeature(FeatureType.WEB);
				}
			}
			if(type == ActorType.CLOUD_ELEMENTAL){
				List<Tile> area = new List<Tile>();
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.passable){
						t.AddFeature(FeatureType.FOG);
						area.Add(t);
					}
				}
				List<Tile> area2 = tile().AddGaseousFeature(FeatureType.FOG,2);
				area.AddRange(area2);
				if(area.Count > 0){
					Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
					Event.RemoveGas(area,101,FeatureType.FOG,75);
				}
			}
			if(type == ActorType.NOXIOUS_WORM){
				List<Tile> area = tile().AddGaseousFeature(FeatureType.POISON_GAS,2);
				if(area.Count > 0){
					Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
					Event.RemoveGas(area,200,FeatureType.POISON_GAS,18);
				}
			}
			if(HasAttr(AttrType.SILENCE_AURA) && DistanceFrom(player) <= 2 && HasLOE(player)){
				if(!player.HasAttr(AttrType.SILENCE_AURA_MESSAGE_COOLDOWN)){
					if(player.CanSee(this)){
						B.Add("Utter silence falls as " + the_name + " draws near. ");
					}
					else{
						B.Add("Utter silence falls around you. ");
					}
					Help.TutorialTip(TutorialTopic.Silenced);
				}
				player.RefreshDuration(AttrType.SILENCE_AURA_MESSAGE_COOLDOWN,301);
			}
		}
		public void ActiveAI(){
			if(path.Count > 0){
				path.Clear();
			}
			if(!HasAttr(AttrType.AGGRESSION_MESSAGE_PRINTED)){
				PrintAggressionMessage();
			}
			switch(type){
			case ActorType.GIANT_BAT:
			case ActorType.PHANTOM_BLIGHTWING:
				if(DistanceFrom(target) == 1){
					int idx = R.Roll(1,2) - 1;
					Attack(idx,target);
					if(target != null && R.CoinFlip()){ //chance of retreating
						AI_Step(target,true);
					}
				}
				else{
					if(R.CoinFlip()){
						AI_Step(target);
						QS();
					}
					else{
						AI_Step(TileInDirection(Global.RandomDirection()));
						QS();
					}
				}
				break;
			case ActorType.BLOOD_MOTH:
			{
				Tile brightest = null;
				if(!M.wiz_dark && !M.wiz_lite && !HasAttr(AttrType.BLIND)){
					List<Tile> valid = M.AllTiles().Where(x=>x.light_value > 0 && CanSee(x));
					valid = valid.WhereGreatest(x=>{
						int result = x.light_radius;
						if(x.Is(FeatureType.FIRE) && result == 0){
							result = 1;
						}
						if(x.inv != null && x.inv.light_radius > result){
							result = x.inv.light_radius;
						}
						if(x.actor() != null && x.actor().LightRadius() > result){
							result = x.actor().LightRadius();
						}
						return result;
					});
					valid = valid.WhereLeast(x=>DistanceFrom(x));
					if(valid.Count > 0){
						brightest = valid.Random();
					}
				}
				if(brightest != null){
					if(DistanceFrom(brightest) <= 1){
						if(target != null && brightest == target.tile()){
							Attack(0,target);
							if(target == player && player.curhp > 0){
								Help.TutorialTip(TutorialTopic.Torch);
							}
						}
						else{
							List<Tile> open = new List<Tile>();
							foreach(Tile t in TilesAtDistance(1)){
								if(t.DistanceFrom(brightest) <= 1 && t.passable && t.actor() == null){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								AI_Step(open.Random());
							}
							QS();
						}
					}
					else{
						List<Tile> tiles = new List<Tile>();
						if(brightest.row == row || brightest.col == col){
							int targetdir = DirectionOf(brightest);
							for(int i=-1;i<=1;++i){
								pos adj = p.PosInDir(targetdir.RotateDir(true,i));
								if(M.tile[adj].passable && M.actor[adj] == null){
									tiles.Add(M.tile[adj]);
								}
							}
						}
						if(tiles.Count > 0){
							AI_Step(tiles.Random());
						}
						else{
							AI_Step(brightest);
						}
						QS();
					}
				}
				else{
					int dir = Global.RandomDirection();
					if(!TileInDirection(dir).passable && TilesAtDistance(1).Where(t => !t.passable).Count > 4){
						dir = Global.RandomDirection();
					}
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null){
						AI_Step(TileInDirection(dir));
						QS();
					}
					else{
						if(curhp < maxhp && target != null && ActorInDirection(dir) == target){
							Attack(0,target);
						}
						else{
							if(player.HasLOS(TileInDirection(dir)) && player.HasLOS(this)){
								if(!TileInDirection(dir).passable){
									B.Add(the_name + " brushes up against " + TileInDirection(dir).the_name + ". ",this);
								}
								else{
									if(ActorInDirection(dir) != null){
										B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheName(true) + ". ",this);
									}
								}
							}
							QS();
						}
					}
				}
				/*PhysicalObject brightest = null;
				if(!M.wiz_lite && !M.wiz_dark){
					List<PhysicalObject> current_brightest = new List<PhysicalObject>();
					foreach(Tile t in M.AllTiles()){
						int pos_radius = t.light_radius;
						PhysicalObject pos_obj = t;
						if(t.Is(FeatureType.FIRE) && pos_radius == 0){
							pos_radius = 1;
						}
						if(t.inv != null && t.inv.light_radius > pos_radius){
							pos_radius = t.inv.light_radius;
							pos_obj = t.inv;
						}
						if(t.actor() != null && t.actor().LightRadius() > pos_radius){
							pos_radius = t.actor().LightRadius();
							pos_obj = t.actor();
						}
						if(pos_radius > 0){
							if(current_brightest.Count == 0 && CanSee(t)){
								current_brightest.Add(pos_obj);
							}
							else{
								foreach(PhysicalObject o in current_brightest){
									int object_radius = o.light_radius;
									if(o is Actor){
										object_radius = (o as Actor).LightRadius();
									}
									if(object_radius == 0 && o is Tile && (o as Tile).Is(FeatureType.FIRE)){
										object_radius = 1;
									}
									if(pos_radius > object_radius){
										if(CanSee(t)){
											current_brightest.Clear();
											current_brightest.Add(pos_obj);
											break;
										}
									}
									else{
										if(pos_radius == object_radius && DistanceFrom(t) < DistanceFrom(o)){
											if(CanSee(t)){
												current_brightest.Clear();
												current_brightest.Add(pos_obj);
												break;
											}
										}
										else{
											if(pos_radius == object_radius && DistanceFrom(t) == DistanceFrom(o) && pos_obj == player){
												if(CanSee(t)){
													current_brightest.Clear();
													current_brightest.Add(pos_obj);
													break;
												}
											}
										}
									}
								}
							}
						}
					}
					if(current_brightest.Count > 0){
						brightest = current_brightest.Random();
					}
				}
				if(brightest != null){
					if(DistanceFrom(brightest) <= 1){
						if(brightest == target){
							Attack(0,target);
							if(target == player && player.curhp > 0){
								Help.TutorialTip(TutorialTopic.Torch);
							}
						}
						else{
							List<Tile> open = new List<Tile>();
							foreach(Tile t in TilesAtDistance(1)){
								if(t.DistanceFrom(brightest) <= 1 && t.passable && t.actor() == null){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								AI_Step(open.Random());
							}
							QS();
						}
					}
					else{
						AI_Step(brightest);
						QS();
					}
				}
				else{
					int dir = Global.RandomDirection();
					if(TilesAtDistance(1).Where(t => !t.passable).Count > 4 && !TileInDirection(dir).passable){
						dir = Global.RandomDirection();
					}
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null){
						AI_Step(TileInDirection(dir));
						QS();
					}
					else{
						if(curhp < maxhp && target != null && ActorInDirection(dir) == target){
							Attack(0,target);
						}
						else{
							if(player.HasLOS(TileInDirection(dir)) && player.HasLOS(this)){
								if(!TileInDirection(dir).passable){
									B.Add(the_name + " brushes up against " + TileInDirection(dir).the_name + ". ",this);
								}
								else{
									if(ActorInDirection(dir) != null){
										B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheName(true) + ". ",this);
									}
								}
							}
							QS();
						}
					}
				}*/
				break;
			}
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.MUD_TENTACLE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(target == player && player.curhp > 0){
						Help.TutorialTip(TutorialTopic.RangedAttacks);
					}
				}
				else{
					QS();
				}
				break;
			case ActorType.FROSTLING:
			{
				if(DistanceFrom(target) == 1){
					if(R.CoinFlip()){
						Attack(0,target);
					}
					else{
						if(AI_Step(target,true)){
							QS();
						}
						else{
							Attack(0,target);
						}
					}
				}
				else{
					if(FirstActorInLine(target) == target && !HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 6){
						int cooldown = R.Roll(1,4);
						if(cooldown != 1){
							RefreshDuration(AttrType.COOLDOWN_1,cooldown*100);
						}
						AnimateBoltProjectile(target,Color.RandomIce);
						if(R.CoinFlip()){
							B.Add(TheName(true) + " hits " + target.the_name + " with a blast of cold. ",target);
							target.TakeDamage(DamageType.COLD,DamageClass.PHYSICAL,R.Roll(2,6),this,"a frostling");
						}
						else{
							B.Add(TheName(true) + " misses " + target.the_name + " with a blast of cold. ",target);
						}
						foreach(Tile t in GetBestLineOfEffect(target)){
							t.ApplyEffect(DamageType.COLD);
						}
						Q1();
					}
					else{
						if(!HasAttr(AttrType.COOLDOWN_2)){
							AI_Step(target);
						}
						else{
							AI_Sidestep(target); //message for this? hmm.
						}
						QS();
					}
				}
				break;
			}
			case ActorType.SWORDSMAN:
			case ActorType.PHANTOM_SWORDMASTER:
				if(DistanceFrom(target) == 1){
					pos target_pos = target.p;
					Attack(0,target);
					if(target != null && target.p.Equals(target_pos)){
						List<Tile> valid_dirs = new List<Tile>();
						foreach(Tile t in target.TilesAtDistance(1)){
							if(t.passable && t.actor() == null && DistanceFrom(t) == 1){
								valid_dirs.Add(t);
							}
						}
						if(valid_dirs.Count > 0){
							AI_Step(valid_dirs.Random());
						}
					}
				}
				else{
					attrs[AttrType.COMBO_ATTACK] = 0;
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.DREAM_WARRIOR:
				if(DistanceFrom(target) == 1){
					if(curhp <= 10 && !HasAttr(AttrType.COOLDOWN_1)){ //todo: changed to 20hp and a 10hp threshold...better?
						attrs[AttrType.COOLDOWN_1]++;
						List<Tile> openspaces = new List<Tile>();
						foreach(Tile t in target.TilesAtDistance(1)){
							if(t.passable && t.actor() == null){
								openspaces.Add(t);
							}
						}
						foreach(Tile t in openspaces){
							if(group == null){
								group = new List<Actor>{this};
							}
							Create(ActorType.DREAM_WARRIOR_CLONE,t.row,t.col,TiebreakerAssignment.InsertAfterCurrent);
							t.actor().player_visibility_duration = -1;
							t.actor().attrs[AttrType.NO_ITEM]++;
							group.Add(M.actor[t.row,t.col]);
							M.actor[t.row,t.col].group = group;
							group.Randomize();
						}
						openspaces.Add(tile());
						Tile newtile = openspaces[R.Roll(openspaces.Count)-1];
						if(newtile != tile()){
							Move(newtile.row,newtile.col,false);
						}
						if(openspaces.Count > 1){
							B.Add(the_name + " is suddenly standing all around " + target.the_name + ". ",this,target);
							Q1();
						}
						else{
							Attack(0,target);
						}
					}
					else{
						Attack(0,target);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.SPITTING_COBRA:
				if(DistanceFrom(target) <= 3 && !HasAttr(AttrType.COOLDOWN_1) && FirstActorInLine(target) == target){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(50,75)*100);
					B.Add(TheName(true) + " spits poison in " + target.YourVisible() + " eyes! ",this,target);
					AnimateBoltProjectile(target,Color.DarkGreen);
					if(!target.HasAttr(AttrType.NONLIVING)){
						target.ApplyStatus(AttrType.BLIND,R.Between(5,8)*100);
						/*B.Add(target.YouAre() + " blind! ",target);
						target.RefreshDuration(AttrType.BLIND,R.Between(5,8)*100,target.YouAre() + " no longer blinded. ",target);*/
					}
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						List<Tile> tiles = new List<Tile>();
						if(target.row == row || target.col == col){
							int targetdir = DirectionOf(target);
							for(int i=-1;i<=1;++i){
								pos adj = p.PosInDir(targetdir.RotateDir(true,i));
								if(M.tile[adj].passable && M.actor[adj] == null){
									tiles.Add(M.tile[adj]);
								}
							}
						}
						if(tiles.Count > 0){
							AI_Step(tiles.Random());
						}
						else{
							AI_Step(target);
						}
						QS();
					}
				}
				break;
			case ActorType.KOBOLD:
				if(!HasAttr(AttrType.COOLDOWN_1)){
					if(DistanceFrom(target) > 12){
						AI_Step(target);
						QS();
					}
					else{
						if(FirstActorInLine(target) != target){
							AI_Sidestep(target);
							QS();
						}
						else{
							attrs[AttrType.COOLDOWN_1]++;
							AnimateBoltProjectile(target,Color.DarkCyan,30);
							if(player.CanSee(this)){
								B.Add(the_name + " fires a dart at " + target.the_name + ". ",this,target);
							}
							else{
								B.Add("A dart hits " + target.the_name + "! ",target);
								if(player.CanSee(tile()) && !IsInvisibleHere()){
									attrs[AttrType.TURNS_VISIBLE] = -1;
									attrs[AttrType.NOTICED] = 1;
									B.Add("You spot " + the_name + " that fired it. ",this);
									//B.Add("You notice " + a_name + ". ",tile());
								}
							}
							if(target.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),this,"a kobold's dart")){
								target.ApplyStatus(AttrType.VULNERABLE,R.Between(2,4)*100);
								/*if(!target.HasAttr(AttrType.VULNERABLE)){
									B.Add(target.YouFeel() + " vulnerable. ",target);
								}
								target.RefreshDuration(AttrType.VULNERABLE,R.Between(2,4)*100,target.YouFeel() + " less vulnerable. ",target);*/
								if(target == player){
									Help.TutorialTip(TutorialTopic.Vulnerable);
								}
							}
							Q1();
						}
					}
				}
				else{
					if(DistanceFrom(target) <= 2){
						AI_Flee();
						QS();
					}
					else{
						B.Add(the_name + " starts reloading. ",this);
						attrs[AttrType.COOLDOWN_1] = 0;
						Q1();
						RefreshDuration(AttrType.COOLDOWN_2,R.Between(5,6)*100 - 50);
						//Q.Add(new Event(this,R.Between(5,6)*100,EventType.MOVE));
					}
				}
				break;
			case ActorType.SPORE_POD:
				if(DistanceFrom(target) == 1){
					TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,100,null);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.FORASECT:
			{
				bool burrow = false;
				if((curhp * 2 <= maxhp || DistanceFrom(target) > 6) && R.CoinFlip()){
					burrow = true;
				}
				if(DistanceFrom(target) <= 6 && DistanceFrom(target) > 1){
					if(R.OneIn(10)){
						burrow = true;
					}
				}
				if(burrow && !HasAttr(AttrType.COOLDOWN_1)){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(8,11)*100);
					if(curhp * 2 <= maxhp){
						Burrow(TilesWithinDistance(6));
					}
					else{
						Burrow(GetCone(DirectionOf(target),6,true));
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.POLTERGEIST:
				if(inv.Count == 0){
					if(DistanceFrom(target) == 1){
						pos target_p = target.p;
						if(Attack(0,target) && M.actor[target_p] != null && M.actor[target_p].inv.Any(i=>!i.do_not_stack)){
							target = M.actor[target_p];
							Item item = target.inv.Where(i=>!i.do_not_stack).Random();
							if(item.quantity > 1){
								inv.Add(new Item(item,-1,-1));
								item.quantity--;
								B.Add(YouVisible("steal") + " " + target.YourVisible() + " " + inv[0].Name() + "! ",this,target);
							}
							else{
								inv.Add(item);
								target.inv.Remove(item);
								B.Add(YouVisible("steal") + " " + target.YourVisible() + " " + item.Name() + "! ",this,target);
							}
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				else{
					attrs[AttrType.KEEPS_DISTANCE] = 1;
					List<Tile> line = target.GetBestExtendedLineOfEffect(this);
					Tile next = null;
					bool found = false;
					foreach(Tile t in line){
						if(found){
							next = t;
							break;
						}
						else{
							if(t.actor() == this){
								found = true;
							}
						}
					}
					if(next != null){
						if(next.passable && next.actor() == null && AI_Step(next)){
							QS();
						}
						else{
							if(!next.passable){
								B.Add(the_name + " disappears into " + next.the_name + ". ",this);
								foreach(Tile t in TilesWithinDistance(1)){
									if(t.DistanceFrom(next) == 1 && t.name == "floor"){
										t.AddFeature(FeatureType.SLIME);
									}
								}
								Event e = null;
								foreach(Event e2 in Q.list){
									if(e2.target == this && e2.type == EventType.POLTERGEIST){
										e = e2;
										break;
									}
								}
								if(e != null){
	 								e.target = inv[0];
									Actor.tiebreakers[e.tiebreaker] = null;
								}
								inv.Clear();
								Kill();
							}
							else{
								if(next.actor() != null){
									if(!next.actor().HasAttr(AttrType.IMMOBILE)){
										Move(next.row,next.col);
										QS();
									}
									else{
										if(next.actor().HasAttr(AttrType.IMMOBILE)){
											if(AI_Step(next)){
												QS();
											}
											else{
												if(DistanceFrom(target) == 1){
													Attack(1,target);
												}
												else{
													QS();
												}
											}
										}
									}
								}
								else{
									QS();
								}
							}
						}
					}
				}
				break;
			case ActorType.CULTIST:
			case ActorType.FINAL_LEVEL_CULTIST:
				if(curhp <= 10 && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					string invocation;
					switch(R.Roll(4)){
					case 1:
						invocation = "ae vatra kersai";
						break;
					case 2:
						invocation = "kersai dzaggath";
						break;
					case 3:
						invocation = "od fir od bahgal";
						break;
					case 4:
						invocation = "denei kersai nammat";
						break;
					default:
						invocation = "denommus pilgni";
						break;
					}
					if(R.CoinFlip()){
						B.Add(You("whisper") + " '" + invocation + "'. ",this);
					}
					else{
						B.Add(You("scream") + " '" + invocation.ToUpper() + "'. ",this);
					}
					if(HasAttr(AttrType.SLIMED)){
						B.Add("Nothing happens. ",this);
					}
					else{
						B.Add("Flames erupt from " + the_name + ". ",this);
						AnimateExplosion(this,1,Color.RandomFire,'*');
						ApplyBurning();
						foreach(Tile t in TilesWithinDistance(1)){
							t.ApplyEffect(DamageType.FIRE);
							if(t.actor() != null){
								t.actor().ApplyBurning();
							}
						}
					}
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.GOBLIN_ARCHER:
			case ActorType.PHANTOM_ARCHER:
				switch(DistanceFrom(target)){
				case 1:
					/*if(target.EnemiesAdjacent() > 1){
						Attack(0,target);
					}
					else{*/
						if(AI_Flee()){
							QS();
						}
						else{
							Attack(0,target);
						}
					//}
					break;
				case 2:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Flee()){
							QS();
						}
						else{ 
							if(AI_Sidestep(target)){
								B.Add(the_name + " tries to line up a shot. ",this);
							}
							QS();
						}
					}
					break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Sidestep(target)){
							B.Add(the_name + " tries to line up a shot. ",this);
						}
						QS();
					}
					break;
				default:
					AI_Step(target);
					QS();
					break;
				}
				break;
			case ActorType.GOBLIN_SHAMAN:
			{
				if(SilencedThisTurn()){
					return;
				}
				if(DistanceFrom(target) == 1){
					if(exhaustion > 50){
						Attack(0,target);
					}
					else{
						CastCloseRangeSpellOrAttack(target);
					}
				}
				else{
					if(DistanceFrom(target) > 12){
						AI_Step(target);
						QS();
					}
					else{
						if(FirstActorInLine(target) != target || R.CoinFlip()){
							AI_Step(target);
							QS();
						}
						else{
							CastRangedSpellOrMove(target);
						}
					}
				}
				break;
			}
			case ActorType.PHASE_SPIDER:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					Tile t = target.TilesAtDistance(DistanceFrom(target)-1).Where(x=>x.passable && x.actor() == null).Random();
					if(t != null){
						Move(t.row,t.col);
					}
					QS();
				}
				break;
			case ActorType.ZOMBIE:
			case ActorType.PHANTOM_ZOMBIE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					AI_Step(target);
					if(DistanceFrom(target) == 1){
						Attack(1,target);
					}
					else{
						QS();
					}
				}
				break;
			case ActorType.ROBED_ZEALOT:
				if(HasAttr(AttrType.COOLDOWN_3)){
					if(DistanceFrom(target) <= 12 && HasLOS(target)){
						target.AnimateExplosion(target,1,Color.Yellow,'*');
						B.Add(YouVisible("smite") + " " + target.the_name + "! ",target);
						int amount = target.curhp / 10;
						bool still_alive = target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Math.Max(amount,1),this,"a zealot's wrath");
						attrs[AttrType.COOLDOWN_3]--;
						attrs[AttrType.DETECTING_MONSTERS]--;
						if(!HasAttr(AttrType.COOLDOWN_3)){
							B.Add(YouVisible("stop") + " praying. ");
							if(still_alive && target.EquippedWeapon.type != WeaponType.NO_WEAPON && !target.EquippedWeapon.status[EquipmentStatus.MERCIFUL]){
								target.EquippedWeapon.status[EquipmentStatus.MERCIFUL] = true;
								B.Add(target.You("feel") + " a strange power enter " + target.Your() + " " + target.EquippedWeapon.NameWithoutEnchantment() + "! ",target);
								B.PrintAll();
								Help.TutorialTip(TutorialTopic.Merciful);
							}
						}
					}
					else{
						attrs[AttrType.COOLDOWN_3]--;
						attrs[AttrType.DETECTING_MONSTERS]--;
					}
					Q1();
				}
				else{
					if(!HasAttr(AttrType.COOLDOWN_1)){
						attrs[AttrType.COOLDOWN_1] = maxhp; //initialize this value here instead of complicating the spawning code
					}
					if(DistanceFrom(target) <= 12 && !HasAttr(AttrType.COOLDOWN_2) && curhp < attrs[AttrType.COOLDOWN_1]){ //if the ability is ready and additional damage has been taken...
						RefreshDuration(AttrType.COOLDOWN_2,R.Between(11,13)*100);
						attrs[AttrType.COOLDOWN_1] = curhp;
						attrs[AttrType.COOLDOWN_3] = 4;
						attrs[AttrType.DETECTING_MONSTERS] = 4;
						B.Add(YouVisible("start") + " praying. ");
						B.Add(the_name + " points directly at you. ",this);
						Q1();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				/*if(HasAttr(AttrType.COOLDOWN_2)){
					attrs[AttrType.COOLDOWN_2] = 0;
					B.Add(the_name + " finishes the prayer. ",this);
					if(DistanceFrom(target) == 1 && target.EquippedWeapon.type != WeaponType.NO_WEAPON){
						target.EquippedWeapon.status[EquipmentStatus.MERCIFUL] = true;
						B.Add("You feel a strange power enter " + target.Your() + " " + target.EquippedWeapon.NameWithoutEnchantment() + "! ",target);
						B.PrintAll();
						Help.TutorialTip(TutorialTopic.Merciful);
					}
					Q1();
				}
				else{
					if((maxhp / 5) * 4 > curhp && !HasAttr(AttrType.COOLDOWN_1)){
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(14,16)*100);
						attrs[AttrType.COOLDOWN_2]++;
						B.Add(the_name + " starts praying. ",this);
						B.Add("A fiery halo appears above " + the_name + ". ",this);
						RefreshDuration(AttrType.RADIANT_HALO,R.Between(8,10)*100,Your() + " halo fades. ",this);
						Q1();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}*/
				break;
			case ActorType.GIANT_SLUG:
			{
				if(DistanceFrom(target) == 1){
					Attack(R.Between(0,1),target);
				}
				else{
					if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(11,14)*100);
						B.Add(TheName(true) + " spits slime at " + target.the_name + ". ",target);
						List<Tile> slimed = GetBestLineOfEffect(target);
						List<Tile> added = new List<Tile>();
						foreach(Tile t in slimed){
							foreach(int dir in U.FourDirections){
								Tile neighbor = t.TileInDirection(dir);
								if(R.OneIn(3) && neighbor.passable && !slimed.Contains(neighbor)){
									added.AddUnique(neighbor);
								}
							}
						}
						slimed.AddRange(added);
						List<pos> cells = new List<pos>();
						List<Actor> slimed_actors = new List<Actor>();
						for(int i=0;slimed.Count > 0;++i){
							List<Tile> removed = new List<Tile>();
							foreach(Tile t in slimed){
								if(DistanceFrom(t) == i){
									t.AddFeature(FeatureType.SLIME);
									if(t.actor() != null && t.actor() != this && !t.actor().HasAttr(AttrType.SLIMED,AttrType.FROZEN)){
										slimed_actors.Add(t.actor());
									}
									removed.Add(t);
									if(DistanceFrom(t) > 0){
										cells.Add(t.p);
									}
								}
							}
							foreach(Tile t in removed){
								slimed.Remove(t);
							}
							if(cells.Count > 0){
								Screen.AnimateMapCells(cells,new colorchar(',',Color.Green),20);
							}
						}
						M.Draw();
						slimed_actors.AddUnique(target);
						foreach(Actor a in slimed_actors){
							a.attrs[AttrType.SLIMED] = 1;
							a.attrs[AttrType.OIL_COVERED] = 0;
							a.RefreshDuration(AttrType.BURNING,0);
							B.Add(a.YouAre() + " covered in slime. ",a);
						}
						Q1();
					}
					else{
						AI_Step(target);
						if(tile().Is(FeatureType.SLIME)){
							speed = 50;
							QS(); //normal speed is 150
							speed = 150;
						}
						else{
							QS();
						}
					}
				}
				break;
			}
			case ActorType.BANSHEE:
			{
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(13,15)*100);
					if(player.CanSee(this)){
						if(player.IsSilencedHere()){
							B.Add(You("seem") + " to scream. ",this);
						}
						else{
							B.Add(You("scream") + ". ",this);
						}
					}
					else{
						if(!player.IsSilencedHere()){
							B.Add("You hear a scream! ");
						}
					}
					if(!target.IsSilencedHere()){
						if(target.ResistedBySpirit() || target.HasAttr(AttrType.MENTAL_IMMUNITY)){
							B.Add(target.You("remain") + " courageous. ",target);
						}
						else{
							B.Add(target.YouAre() + " terrified! ",target);
							RefreshDuration(AttrType.TERRIFYING,R.Between(5,8)*100,target.YouAre() + " no longer afraid. ",target);
							Help.TutorialTip(TutorialTopic.Afraid);
						}
					}
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.CAVERN_HAG:
				if(curhp < maxhp && HasAttr(AttrType.COOLDOWN_2) && !HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					B.Add(TheName(true) + " curses you! ");
					if(target.ResistedBySpirit()){
						B.Add("You resist the curse. ");
					}
					else{
						switch(R.Roll(4)){
						case 1: //light allergy
							B.Add("You become allergic to light! ");
							target.RefreshDuration(AttrType.LIGHT_SENSITIVE,(R.Roll(2,20) + 70) * 100,"You are no longer allergic to light. ");
							break;
						case 2: //aggravate monsters
							B.Add("Every sound you make becomes amplified and echoes across the dungeon. ");
							target.RefreshDuration(AttrType.AGGRAVATING,(R.Roll(2,20) + 70) * 100,"Your sounds are no longer amplified. ");
							break;
						case 3: //cursed weapon
							B.Add("Your " + target.EquippedWeapon + " becomes stuck to your hand! ");
							target.EquippedWeapon.status[EquipmentStatus.STUCK] = true;
							Help.TutorialTip(TutorialTopic.Stuck);
							break;
						case 4: //heavy weapon
							B.Add("Your " + target.EquippedWeapon + " suddenly feels much heavier. ");
							target.EquippedWeapon.status[EquipmentStatus.HEAVY] = true;
							Help.TutorialTip(TutorialTopic.Heavy);
							break;
						}
					}
					attrs[AttrType.COOLDOWN_1]++;
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.BERSERKER:
			{
				if(HasAttr(AttrType.COOLDOWN_2)){
					int dir = attrs[AttrType.COOLDOWN_2];
					bool cw = R.CoinFlip();
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null && !MovementPrevented(TileInDirection(dir))){
						B.Add(the_name + " leaps forward swinging his axe! ",this);
						Move(TileInDirection(dir).row,TileInDirection(dir).col);
						M.Draw();
						for(int i=-1;i<=1;++i){
							Screen.AnimateBoltProjectile(new List<Tile>{tile(),TileInDirection(dir.RotateDir(cw,i))},Color.Red,30);
						}
						for(int i=-1;i<=1;++i){
							Actor a = ActorInDirection(dir.RotateDir(cw,i));
							if(a != null){
								B.Add(YourVisible() + " axe hits " + a.TheName(true) + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(4,6),this,"a berserker's axe");
							}
							TileInDirection(dir.RotateDir(cw,i)).Bump(dir.RotateDir(cw,i));
						}
						Q1();
					}
					else{
						if(ActorInDirection(dir) != null || MovementPrevented(TileInDirection(dir)) || TileInDirection(dir).Is(TileType.STANDING_TORCH,TileType.BARREL,TileType.POISON_BULB)){
							B.Add(the_name + " swings his axe furiously! ",this);
							for(int i=-1;i<=1;++i){
								Screen.AnimateBoltProjectile(new List<Tile>{tile(),TileInDirection(dir.RotateDir(cw,i))},Color.Red,30);
							}
							for(int i=-1;i<=1;++i){
								Actor a = ActorInDirection(dir.RotateDir(cw,i));
								if(a != null){
									B.Add(YourVisible() + " axe hits " + a.TheName(true) + ". ",this,a);
									a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(4,6),this,"a berserker's axe");
								}
								TileInDirection(dir.RotateDir(cw,i)).Bump(dir.RotateDir(cw,i));
							}
							Q1();
						}
						else{
							if(target != null && HasLOS(target)){
								B.Add(the_name + " turns to face " + target.the_name + ". ",this);
								attrs[AttrType.COOLDOWN_2] = DirectionOf(target);
								Q1();
							}
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
						if(target != null && R.Roll(3) == 3){
							B.Add(the_name + " screams with fury! ",this);
							attrs[AttrType.COOLDOWN_2] = DirectionOf(target);
							Q.Add(new Event(this,350,AttrType.COOLDOWN_2,Your() + " rage diminishes. ",this));
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.DIRE_RAT:
			{
				bool slip_past = false;
				if(DistanceFrom(target) == 1){
					foreach(Actor a in ActorsAtDistance(1)){
						if(a.type == ActorType.DIRE_RAT && a.DistanceFrom(target) > this.DistanceFrom(target)){
							bool can_walk = false;
							foreach(Tile t in a.TilesAtDistance(1)){
								if(t.DistanceFrom(target) < a.DistanceFrom(target) && t.passable && t.actor() == null){
									can_walk = true;
									break;
								}
							}
							if(!can_walk){ //there's a rat that would benefit from a space opening up - now check to see whether a move is possible
								foreach(Tile t in target.TilesAtDistance(1)){
									if(t.passable && t.actor() == null){
										slip_past = true;
										break;
									}
								}
								break;
							}
						}
					}
				}
				if(slip_past){
					bool moved = false;
					foreach(Tile t in TilesAtDistance(1)){
						if(t.DistanceFrom(target) == 1 && t.passable && t.actor() == null){
							AI_Step(t);
							QS();
							moved = true;
							break;
						}
					}
					if(!moved){
						Tile t = target.TilesAtDistance(1).Where(x=>x.passable && x.actor() == null).Random();
						if(t != null){
							B.Add(TheName(true) + " slips past " + target.TheName(true) + ". ",this,target);
							Move(t.row,t.col);
							Q.Add(new Event(this,Speed() + 100,EventType.MOVE));
						}
						else{
							QS();
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.SKULKING_KILLER:
			{
				if(HasAttr(AttrType.KEEPS_DISTANCE)){
					bool try_to_hide = false;
					if(AI_Flee()){
						try_to_hide = true;
						QS();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{ //give up on fleeing, just attack
							attrs[AttrType.COOLDOWN_2] = 0;
							attrs[AttrType.KEEPS_DISTANCE] = 0;
							AI_Step(target);
							QS();
						}
					}
					if(try_to_hide){
						bool visible = player.CanSee(this);
						if(!R.OneIn(5) && (!player.HasLOE(this) || !visible || DistanceFrom(player) > 12)){ //just to add some uncertainty
							attrs[AttrType.COOLDOWN_2]++;
							if(attrs[AttrType.COOLDOWN_2] >= 3){
								attrs[AttrType.KEEPS_DISTANCE] = 0;
								attrs[AttrType.COOLDOWN_2] = 0;
								if(!visible){
									attrs[AttrType.TURNS_VISIBLE] = 0;
								}
							}
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						if(Attack(0,target)){
							attrs[AttrType.KEEPS_DISTANCE] = 1;
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				/*if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 3 && R.OneIn(3) && HasLOE(target)){
					attrs[AttrType.COOLDOWN_1]++;
					AnimateProjectile(target,Color.DarkYellow,'%');
					Global.FlushInput();
					if(target.CanSee(this)){
						B.Add(the_name + " throws a bola at " + target.the_name + ". ",this,target);
					}
					else{
						B.Add("A bola whirls toward " + target.the_name + ". ",this,target);
					}
					attrs[AttrType.TURNS_VISIBLE] = -1;
					target.RefreshDuration(AttrType.SLOWED,(R.Roll(3)+6)*100,target.YouAre() + " no longer slowed. ",target);
					B.Add(target.YouAre() + " slowed by the bola. ",target);
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}*/
				break;
			}
			case ActorType.WILD_BOAR:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(HasAttr(AttrType.JUST_FLUNG)){ //if it just flung its target...
						attrs[AttrType.JUST_FLUNG] = 0;
						attrs[AttrType.COOLDOWN_1] = 0;
					}
					else{ //...otherwise it might prepare to fling again
						if(!HasAttr(AttrType.COOLDOWN_1)){
							if(!HasAttr(AttrType.COOLDOWN_2) || R.OneIn(5)){
								attrs[AttrType.COOLDOWN_2]++;
								B.Add(the_name + " lowers its head. ",this);
								attrs[AttrType.COOLDOWN_1]++;
							}
						}
					}
				}
				else{
					AI_Step(target);
					if(!HasAttr(AttrType.COOLDOWN_2)){
						attrs[AttrType.COOLDOWN_2]++;
						B.Add(the_name + " lowers its head. ",this);
						attrs[AttrType.COOLDOWN_1]++;
					}
					QS();
				}
				break;
			case ActorType.DREAM_SPRITE:
				if(!HasAttr(AttrType.COOLDOWN_1)){
					if(DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(3,4)*100);
						bool visible = false;
						List<List<Tile>> lines = new List<List<Tile>>{GetBestLineOfEffect(target)};
						if(group != null && group.Count > 0){
							foreach(Actor a in group){
								if(target == player && player.CanSee(a)){
									visible = true;
								}
								if(a.type == ActorType.DREAM_SPRITE_CLONE){
									a.attrs[AttrType.COOLDOWN_1]++; //for them, it means 'skip next turn'
									if(a.FirstActorInLine(target) == target){
										lines.Add(a.GetBestLineOfEffect(target));
									}
								}
							}
						}
						foreach(List<Tile> line in lines){
							if(line.Count > 0){
								line.RemoveAt(0);
							}
						}
						if(visible){
							B.Add(the_name + " hits " + target.the_name + " with stinging magic. ",target);
						}
						else{
							B.Add(TheName(true) + " hits " + target.the_name + " with stinging magic. ",target);
						}
						int max = lines.WhereGreatest(x=>x.Count)[0].Count;
						for(int i=0;i<max;++i){
							List<pos> cells = new List<pos>();
							foreach(List<Tile> line in lines){
								if(line.Count > i){
									cells.Add(line[i].p);
								}
							}
							Screen.AnimateMapCells(cells,new colorchar('*',Color.RandomRainbow));
						}
						target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(2,6),this,"a blast of fairy magic");
						Q1();
					}
					else{
						if(DistanceFrom(target) > 12){
							AI_Step(target);
						}
						else{
							AI_Sidestep(target);
						}
						QS();
					}
				}
				else{
					if(DistanceFrom(target) > 5){
						AI_Step(target);
					}
					else{
						if(DistanceFrom(target) < 3){
							AI_Flee();
						}
						else{
							Tile t = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null).Random();
							if(t != null){
								AI_Step(t);
							}
						}
					}
					QS();
				}
				break;
			case ActorType.DREAM_SPRITE_CLONE:
				if(HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1] = 0;
					Q1();
				}
				else{
					if(DistanceFrom(target) > 5){
						AI_Step(target);
					}
					else{
						if(DistanceFrom(target) < 3){
							AI_Flee();
						}
						else{
							Tile t = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null).Random();
							if(t != null){
								AI_Step(t);
							}
						}
					}
					QS();
				}
				break;
			case ActorType.CLOUD_ELEMENTAL:
			{
				List<pos> cloud = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].features.Contains(FeatureType.FOG));
				PhysicalObject[] objs = new PhysicalObject[cloud.Count + 1];
				int idx = 0;
				foreach(pos p2 in cloud){
					objs[idx++] = M.tile[p2];
				}
				objs[idx] = this;
				List<colorchar> chars = new List<colorchar>();
				colorchar cch = new colorchar('*',Color.RandomLightning);
				if(cloud.Contains(target.p)){
					B.Add(the_name + " electrifies the cloud! ",objs);
					foreach(pos p2 in cloud){
						if(M.actor[p2] != null && M.actor[p2] != this){
							M.actor[p2].TakeDamage(DamageType.ELECTRIC,DamageClass.PHYSICAL,R.Roll(3,6),this,"*electrocuted by a cloud elemental");
						}
						if(M.actor[p2] == this){
							chars.Add(visual);
						}
						else{
							chars.Add(cch);
						}
					}
					Screen.AnimateMapCells(cloud,chars,50);
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Tile t = TilesAtDistance(1).Where(x=>x.actor() == null && x.passable).Random();
						if(t != null){
							AI_Step(t);
						}
						QS();
					}
					else{
						if(R.OneIn(4)){
							Tile t = TilesAtDistance(1).Where(x=>x.actor() == null && x.passable).Random();
							if(t != null){
								AI_Step(t);
							}
							QS();
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			}
			case ActorType.DERANGED_ASCETIC:
				if(DistanceFrom(target) == 1){
					Attack(R.Roll(3)-1,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.SNEAK_THIEF:
			{
				if(DistanceFrom(target) <= 12 && !R.OneIn(3) && AI_UseRandomItem()){
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
						if(target != null){
							List<Tile> valid_dirs = new List<Tile>();
							foreach(Tile t in target.TilesAtDistance(1)){
								if(t.passable && t.actor() == null && DistanceFrom(t) == 1){
									valid_dirs.Add(t);
								}
							}
							if(valid_dirs.Count > 0){
								AI_Step(valid_dirs.Random());
							}
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.WARG:
			{
				bool howl = false;
				if(DistanceFrom(target) == 1){
					if(R.CoinFlip() || group == null || group.Count < 2 || HasAttr(AttrType.COOLDOWN_1)){
						Attack(0,target);
					}
					else{
						howl = true;
					}
				}
				else{
					if(group == null || group.Count < 2 || HasAttr(AttrType.COOLDOWN_1)){
						if(AI_Step(target)){
							QS();
						}
						else{
							howl = true;
						}
					}
					else{
						howl = true;
					}
				}
				if(howl){
					if(group == null || group.Count < 2){
						Q1();
						break;
					}
					B.Add(TheName(true) + " howls. ");
					PosArray<int> paths = new PosArray<int>(ROWS,COLS);
					foreach(Actor packmate in group){
						packmate.RefreshDuration(AttrType.COOLDOWN_1,2000);
						if(packmate != this){
							var dijkstra = M.tile.GetDijkstraMap(x=>!M.tile[x].passable,y=>M.actor[y] != null? 5 : paths[y]+1,new List<pos>{target.p});
							if(!dijkstra[packmate.p].IsValidDijkstraValue()){
								continue;
							}
							List<pos> new_path = new List<pos>();
							pos p = packmate.p;
							while(!p.Equals(target.p)){
								p = p.PositionsAtDistance(1).Where(x=>dijkstra[x] != U.DijkstraMin).WhereLeast(x=>dijkstra[x]).Random();
								new_path.Add(p);
								paths[p]++;
							}
							packmate.path = new_path;
						}
					}
					Q1();
				}
				break;
			}
			case ActorType.RUNIC_TRANSCENDENT:
			{
				if(SilencedThisTurn()){
					return;
				}
				if(!HasSpell(SpellType.MERCURIAL_SPHERE)){
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
					return;
				}
				if(curmp < 2){
					B.Add(the_name + " absorbs mana from the universe. ",this);
					curmp = maxmp;
					Q1();
				}
				else{
					if(M.safetymap == null){
						M.UpdateSafetyMap(player);
					}
					Tile t = TilesAtDistance(1).Where(x=>x.DistanceFrom(target) == 3 && x.passable && x.actor() == null).WhereLeast(x=>M.safetymap[x.p]).Random();
					if(t != null){ //check safety map. if there's a safer spot at distance 3, step there.
						AI_Step(t);
					}
					else{
						if(DistanceFrom(target) > 3){
							AI_Step(target);
						}
						else{
							if(DistanceFrom(target) < 3){
								AI_Flee();
							}
						}
					}
					if(DistanceFrom(target) <= 12 && FirstActorInLine(target) != null && FirstActorInLine(target).DistanceFrom(target) <= 3){
						CastSpell(SpellType.MERCURIAL_SPHERE,target);
					}
					else{
						QS();
					}
				}
				break;
			}
			case ActorType.CARRION_CRAWLER:
				if(DistanceFrom(target) == 1){
					if(!target.HasAttr(AttrType.PARALYZED)){
						Attack(0,target);
					}
					else{
						Attack(1,target);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.MECHANICAL_KNIGHT:
				if(attrs[AttrType.COOLDOWN_1] == 3){ //no head
					int dir = Global.RandomDirection();
					if(R.CoinFlip()){
						Actor a = ActorInDirection(dir);
						if(a != null){
							if(!Attack(0,a)){
								B.Add(the_name + " drops its guard! ",this);
								attrs[AttrType.MECHANICAL_SHIELD] = 0;
							}
						}
						else{
							B.Add(the_name + " attacks empty space. ",this);
							TileInDirection(dir).Bump(dir);
							B.Add(the_name + " drops its guard! ",this);
							attrs[AttrType.MECHANICAL_SHIELD] = 0;
							Q1();
						}
					}
					else{
						Tile t = TileInDirection(dir);
						if(t.passable){
							if(t.actor() == null){
								AI_Step(t);
								QS();
							}
							else{
								B.Add(the_name + " bumps into " + t.actor().TheName(true) + ". ",this);
								QS();
							}
						}
						else{
							B.Add(the_name + " bumps into " + t.TheName(true) + ". ",this);
							t.Bump(DirectionOf(t));
							QS();
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						if(attrs[AttrType.COOLDOWN_1] == 1){ //no arms
							Attack(1,target);
						}
						else{
							if(!Attack(0,target)){
								B.Add(the_name + " drops its guard! ",this);
								attrs[AttrType.MECHANICAL_SHIELD] = 0;
							}
						}
					}
					else{
						if(attrs[AttrType.COOLDOWN_1] != 2){ //no legs
							AI_Step(target);
						}
						QS();
					}
				}
				break;
			case ActorType.ALASI_BATTLEMAGE:
				if(SilencedThisTurn()){
					return;
				}
				if(DistanceFrom(target) > 12){
					AI_Step(target);
					QS();
				}
				else{
					if(DistanceFrom(target) == 1){
						if(exhaustion < 50){
							CastCloseRangeSpellOrAttack(null,target,true);
						}
						else{
							Attack(0,target);
						}
					}
					else{
						CastRangedSpellOrMove(target);
					}
				}
				break;
			case ActorType.ALASI_SOLDIER:
				if(DistanceFrom(target) > 2){
					AI_Step(target);
					QS();
					attrs[AttrType.COMBO_ATTACK] = 0;
				}
				else{
					if(FirstActorInLine(target) != null && !FirstActorInLine(target).name.Contains("alasi")){ //I had planned to make this attack possibly hit multiple targets, but not yet.
						Attack(0,target);
					}
					else{
						if(AI_Step(target)){
							QS();
						}
						else{
							AI_Sidestep(target);
							QS();
						}
						attrs[AttrType.COMBO_ATTACK] = 0;
					}
				}
				break;
			case ActorType.SKITTERMOSS:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(target != null && R.CoinFlip()){ //chance of retreating
						AI_Step(target,true);
					}
				}
				else{
					if(R.CoinFlip()){
						AI_Step(target);
						QS();
					}
					else{
						AI_Step(TileInDirection(Global.RandomDirection()));
						QS();
					}
				}
				break;
			case ActorType.ALASI_SCOUT:
			{
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					if(curhp == maxhp){
						if(FirstActorInLine(target) == target){
							Attack(1,target);
						}
						else{
							AI_Sidestep(target);
							QS();
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.MUD_ELEMENTAL:
			{
				int count = 0;
				int walls = 0;
				foreach(Tile t in target.TilesAtDistance(1)){
					if(t.p.BoundsCheck(M.tile,false) && t.type == TileType.WALL){
						++walls;
						if(t.actor() == null){
							++count;
						}
					}
				}
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12 && count >= 2 || (count == 1 && walls == 1)){
					RefreshDuration(AttrType.COOLDOWN_1,150);
					foreach(Tile t in target.TilesAtDistance(1)){
						if(t.p.BoundsCheck(M.tile,false) && t.type == TileType.WALL && t.actor() == null){
							Create(ActorType.MUD_TENTACLE,t.row,t.col,TiebreakerAssignment.InsertAfterCurrent);
							M.actor[t.p].player_visibility_duration = -1;
							M.actor[t.p].attrs[AttrType.LIFESPAN] = 20;
						}
					}
					if(count >= 2){
						if(player.CanSee(this)){
							B.Add(the_name + " calls mud tentacles from the walls! ");
						}
						else{
							B.Add("Mud tentacles emerge from the walls! ");
						}
					}
					else{
						if(player.CanSee(this)){
							B.Add(the_name + " calls a mud tentacle from the wall! ");
						}
						else{
							B.Add("A mud tentacle emerges from the wall! ");
						}
					}
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.FLAMETONGUE_TOAD:
			{
				bool burrow = false;
				if((curhp * 3 <= maxhp || DistanceFrom(target) > 6) && R.CoinFlip()){
					burrow = true;
				}
				if(DistanceFrom(target) <= 6 && DistanceFrom(target) > 1){
					if(R.OneIn(20)){
						burrow = true;
					}
				}
				if(burrow && !HasAttr(AttrType.COOLDOWN_1)){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(12,16)*100);
					if(curhp * 3 <= maxhp){
						Burrow(TilesWithinDistance(6));
					}
					else{
						Burrow(GetCone(DirectionOf(target),6,true));
					}
				}
				else{
					if(!HasAttr(AttrType.COOLDOWN_2) && FirstActorInLine(target) != null && FirstActorInLine(target).DistanceFrom(target) <= 1){
						RefreshDuration(AttrType.COOLDOWN_2,R.Between(10,14)*100);
						Actor first = FirstActorInLine(target);
						B.Add(TheName(true) + " breathes fire! ",this,first);
						AnimateProjectile(first,'*',Color.RandomFire);
						AnimateExplosion(first,1,'*',Color.RandomFire);
						foreach(Tile t in GetBestLineOfEffect(first)){
							t.ApplyEffect(DamageType.FIRE);
						}
						foreach(Tile t in first.TilesWithinDistance(1)){
							t.ApplyEffect(DamageType.FIRE);
							if(t.actor() != null){
								t.actor().ApplyBurning();
							}
						}
						Q1();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			}
			case ActorType.ENTRANCER:
				if(group == null){
					if(AI_Flee()){
						QS();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							QS();
						}
					}
				}
				else{
					Actor thrall = group[1];
					if(CanSee(thrall) && HasLOE(thrall)){ //cooldown 1 is teleport. cooldown 2 is shield.
						//if the thrall is visible and you have LOE, the next goal is for the entrancer to be somewhere on the line that starts at the target and extends through the thrall.
						List<Tile> line_from_target = target.GetBestExtendedLineOfEffect(thrall);
						bool on_line = line_from_target.Contains(tile());
						bool space_near_target = line_from_target.Count > 1 && line_from_target[1].passable && line_from_target[1].actor() == null;
						if(on_line && DistanceFrom(target) > thrall.DistanceFrom(target)){
							if(!HasAttr(AttrType.COOLDOWN_2) && thrall.curhp <= thrall.maxhp/2){ //check whether you can shield it, if the thrall is low on health.
								RefreshDuration(AttrType.COOLDOWN_2,1500);
								B.Add(TheName(true) + " shields " + thrall.TheName(true) + ". ",this,thrall);
								B.DisplayNow();
								Screen.AnimateStorm(thrall.p,1,2,5,'*',Color.White);
								thrall.attrs[AttrType.SHIELDED] = 1;
								Q1();
							}
							else{ //check whether you can teleport the thrall closer.
								if(!HasAttr(AttrType.COOLDOWN_1) && thrall.DistanceFrom(target) > 1 && space_near_target){
									Tile dest = line_from_target[1];
									RefreshDuration(AttrType.COOLDOWN_1,400);
									B.Add(TheName(true) + " teleports " + thrall.TheName(true) + ". ",this,thrall);
									M.Draw();
									thrall.Move(dest.row,dest.col);
									B.DisplayNow();
									Screen.AnimateStorm(dest.p,1,1,4,thrall.symbol,thrall.color);
									foreach(Tile t2 in thrall.GetBestLineOfEffect(dest)){
										Screen.AnimateStorm(t2.p,1,1,4,thrall.symbol,thrall.color);
									}
									Q1();
								}
								else{ //check whether you can shield it, if the thrall isn't low on health.
									if(!HasAttr(AttrType.COOLDOWN_2)){
										RefreshDuration(AttrType.COOLDOWN_2,1500);
										B.Add(TheName(true) + " shields " + thrall.TheName(true) + ". ",this,thrall);
										B.DisplayNow();
										Screen.AnimateStorm(thrall.p,1,2,5,'*',Color.White);
										thrall.attrs[AttrType.SHIELDED] = 1;
										Q1();
									}
									else{ //check whether you are adjacent to thrall and can step away while remaining on line.
										List<Tile> valid = line_from_target.Where(x=>DistanceFrom(x) == 1 && x.actor() == null && x.passable);
										if(DistanceFrom(thrall) == 1 && valid.Count > 0){
											AI_Step(valid.Random());
										}
										QS();
									}
								}
							}
						}
						else{
							if(on_line){ //if on the line but not behind the thrall, we might be able to swap places or teleport
								if(DistanceFrom(thrall) == 1){
									Move(thrall.row,thrall.col);
									QS();
								}
								else{
									Tile dest = null;
									foreach(Tile t in line_from_target){
										if(t.passable && t.actor() == null){
											dest = t;
											break;
										}
									}
									if(dest != null){
										RefreshDuration(AttrType.COOLDOWN_1,400);
										B.Add(TheName(true) + " teleports " + thrall.TheName(true) + ". ",this,thrall);
										M.Draw();
										thrall.Move(dest.row,dest.col);
										B.DisplayNow();
										Screen.AnimateStorm(dest.p,1,1,4,thrall.symbol,thrall.color);
										foreach(Tile t2 in thrall.GetBestLineOfEffect(dest)){
											Screen.AnimateStorm(t2.p,1,1,4,thrall.symbol,thrall.color);
										}
									}
									Q1();
								}
							}
							else{ //if there's a free adjacent space on the line and behind the thrall, step there.
								List<Tile> valid = line_from_target.From(thrall).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(this) == 1);
								if(valid.Count > 0){
									AI_Step(valid.Random());
									QS();
								}
								else{ //if you can teleport and there's a free tile on the line between you and the target, teleport the thrall there.
									List<Tile> valid_between = GetBestLineOfEffect(target).Where(x=>x.passable && x.actor() == null && thrall.HasLOE(x));
									if(!HasAttr(AttrType.COOLDOWN_1) && valid_between.Count > 0){
										Tile dest = valid_between.Random();
										RefreshDuration(AttrType.COOLDOWN_1,400);
										B.Add(TheName(true) + " teleports " + thrall.TheName(true) + ". ",this,thrall);
										M.Draw();
										thrall.Move(dest.row,dest.col);
										B.DisplayNow();
										Screen.AnimateStorm(dest.p,1,1,4,thrall.symbol,thrall.color);
										foreach(Tile t2 in thrall.GetBestLineOfEffect(dest)){
											Screen.AnimateStorm(t2.p,1,1,4,thrall.symbol,thrall.color);
										}
										Q1();
									}
									else{ //step toward a tile on the line (and behind the thrall)
										List<Tile> valid_behind_thrall = line_from_target.From(thrall).Where(x=>x.passable && x.actor() == null);
										if(valid_behind_thrall.Count > 0){
											AI_Step(valid_behind_thrall.Random());
										}
										QS();
									}
								}
							}
						}
						//the old code:
						/*if(DistanceFrom(target) < thrall.DistanceFrom(target) && DistanceFrom(thrall) == 1){
							Move(thrall.row,thrall.col);
							QS();
						}
						else{
							if(DistanceFrom(target) == 1 && curhp < maxhp){
								List<Tile> safe = TilesAtDistance(1).Where(t=>t.passable && t.actor() == null && target.GetBestExtendedLineOfEffect(thrall).Contains(t));
								if(DistanceFrom(thrall) == 1 && safe.Count > 0){
									AI_Step(safe.Random());
									QS();
								}
								else{
									if(AI_Flee()){
										QS();
									}
									else{
										Attack(0,target);
									}
								}
							}
							else{
								if(!HasAttr(AttrType.COOLDOWN_1) && (thrall.DistanceFrom(target) > 1 || !target.GetBestExtendedLineOfEffect(thrall).Any(t=>t.actor()==this))){ //the entrancer tries to be smart about placing the thrall in a position that blocks ranged attacks
									List<Tile> closest = new List<Tile>();
									int dist = 99;
									foreach(Tile t in thrall.TilesWithinDistance(2).Where(x=>x.passable && (x.actor()==null || x.actor()==thrall))){
										if(t.DistanceFrom(target) < dist){
											closest.Clear();
											closest.Add(t);
											dist = t.DistanceFrom(target);
										}
										else{
											if(t.DistanceFrom(target) == dist){
												closest.Add(t);
											}
										}
									}
									List<Tile> in_line = new List<Tile>();
									foreach(Tile t in closest){
										if(target.GetBestExtendedLineOfEffect(t).Any(x=>x.actor()==this)){
											in_line.Add(t);
										}
									}
									Tile tile2 = null;
									if(in_line.Count > 0){
										tile2 = in_line.Random();
									}
									else{
										if(closest.Count > 0){
											tile2 = closest.Random();
										}
									}
									if(tile2 != null && tile2.actor() != thrall){
										GainAttr(AttrType.COOLDOWN_1,400);
										B.Add(TheName(true) + " teleports " + thrall.TheName(true) + ". ",this,thrall);
										M.Draw();
										thrall.Move(tile2.row,tile2.col);
										B.DisplayNow();
										Screen.AnimateStorm(tile2.p,1,1,4,thrall.symbol,thrall.color);
										foreach(Tile t2 in thrall.GetBestLineOfEffect(tile2)){
											Screen.AnimateStorm(t2.p,1,1,4,thrall.symbol,thrall.color);
										}
										Q1();
									}
									else{
										List<Tile> safe = target.GetBestExtendedLineOfEffect(thrall).Where(t=>t.passable
										&& t.actor() == null && t.DistanceFrom(target) > thrall.DistanceFrom(target)).WhereLeast(t=>DistanceFrom(t));
										if(safe.Count > 0){
											if(safe.Any(t=>t.DistanceFrom(target) > 2)){
												AI_Step(safe.Where(t=>t.DistanceFrom(target) > 2).Random());
											}
											else{
												AI_Step(safe.Random());
											}
										}
										QS();
									}
								}
								else{
									if(!HasAttr(AttrType.COOLDOWN_2) && thrall.attrs[AttrType.ARCANE_SHIELDED] < 25){
										GainAttr(AttrType.COOLDOWN_2,1500);
										B.Add(TheName(true) + " shields " + thrall.TheName(true) + ". ",this,thrall);
										B.DisplayNow();
										Screen.AnimateStorm(thrall.p,1,2,5,'*',Color.White);
										thrall.attrs[AttrType.ARCANE_SHIELDED] = 25;
										Q1();
									}
									else{
										List<Tile> safe = target.GetBestExtendedLineOfEffect(thrall).Where(t=>t.passable && t.actor() == null).WhereLeast(t=>DistanceFrom(t));
										if(safe.Count > 0){
											if(safe.Any(t=>t.DistanceFrom(target) > 2)){
												AI_Step(safe.Where(t=>t.DistanceFrom(target) > 2).Random());
											}
											else{
												AI_Step(safe.Random());
											}
										}
										QS();
									}
								}
							}
						}*/
					}
					else{
						group[1].FindPath(this); //call for help
						if(AI_Flee()){
							QS();
						}
						else{
							if(DistanceFrom(target) == 1){
								Attack(0,target);
							}
							else{
								QS();
							}
						}
					}
				}
				break;
			case ActorType.ORC_GRENADIER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 8){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(R.Roll(2)*100)+150,AttrType.COOLDOWN_1));
					B.Add(TheName(true) + " tosses a grenade toward " + target.the_name + ". ",target);
					List<Tile> tiles = new List<Tile>();
					foreach(Tile tile in target.TilesWithinDistance(1)){
						if(tile.passable && !tile.Is(FeatureType.GRENADE)){
							tiles.Add(tile);
						}
					}
					Tile t = tiles[R.Roll(tiles.Count)-1];
					if(t.actor() != null){
						if(t.actor() == player){
							B.Add("It lands under you! ");
						}
						else{
							B.Add("It lands under " + t.actor().the_name + ". ",t.actor());
						}
					}
					else{
						if(t.inv != null){
							B.Add("It lands under " + t.inv.TheName() + ". ",t);
						}
					}
					t.features.Add(FeatureType.GRENADE);
					Q.Add(new Event(t,100,EventType.GRENADE));
					Q1();
				}
				else{
					if(curhp <= 18){
						if(AI_Step(target,true)){
							QS();
						}
						else{
							if(DistanceFrom(target) == 1){
								Attack(0,target);
							}
							else{
								QS();
							}
						}
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			case ActorType.MARBLE_HORROR:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.SPELLMUDDLE_PIXIE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(target != null && R.CoinFlip()){
						AI_Step(target,true);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.OGRE_BARBARIAN:
				//if has grabbed target, check for open spaces near the opposite side.
				//if one is found, slam target into that tile, then do the attack.
				//otherwise, slam target into a solid tile (target doesn't move), then attack.
				//if nothing is grabbed yet, just keep attacking.
				if(DistanceFrom(target) == 1){
					if(target.HasAttr(AttrType.GRABBED) && attrs[AttrType.GRABBING] == DirectionOf(target) && !target.MovementPrevented(tile())){
						Tile t = null;
						Tile opposite = TileInDirection(DirectionOf(target).RotateDir(true,4));
						if(opposite.passable && opposite.actor() == null){
							t = opposite;
						}
						if(t == null){
							List<Tile> near_opposite = new List<Tile>();
							foreach(int i in new int[]{-1,1}){
								Tile near = TileInDirection(DirectionOf(target).RotateDir(true,4+i));
								if(near.passable && near.actor() == null){
									near_opposite.Add(near);
								}
							}
							if(near_opposite.Count > 0){
								t = near_opposite.Random();
							}
						}
						attrs[AttrType.STUN_HIT] = 1; //the slam no longer deals extra damage, but stuns instead
						if(t != null){
							target.attrs[AttrType.TURN_INTO_CORPSE]++;
							Attack(1,target);
							target.Move(t.row,t.col);
							target.CollideWith(target.tile());
							target.CorpseCleanup();
						}
						else{
							target.attrs[AttrType.TURN_INTO_CORPSE]++;
							Attack(1,target);
							target.CollideWith(target.tile());
							target.CorpseCleanup();
						}
						attrs[AttrType.STUN_HIT] = 0;
					}
					else{
						Attack(0,target);
					}
				}
				else{
					if(speed == 100){
						speed = 50;
					}
					if(!HasAttr(AttrType.COOLDOWN_1) && target == player && player.CanSee(this)){
						B.Add(the_name + " charges! ");
						attrs[AttrType.COOLDOWN_1] = 1;
					}
					AI_Step(target);
					if(!HasAttr(AttrType.COOLDOWN_1) && target == player && player.CanSee(this)){ //check twice so the message appears ASAP
						B.Add(the_name + " charges! ");
						attrs[AttrType.COOLDOWN_1] = 1;
					}
					QS();
				}
				break;
			case ActorType.MARBLE_HORROR_STATUE:
				QS();
				break;
			case ActorType.PYREN_ARCHER: //still considering some sort of fire trail movement ability for this guy
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1){
						Attack(0,target);
					}
					else{
						if(AI_Flee()){
							QS();
						}
						else{
							Attack(0,target);
						}
					}
					break;
				case 2:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Flee()){
							QS();
						}
						else{ 
							if(AI_Sidestep(target)){
								B.Add(the_name + " tries to line up a shot. ",this);
							}
							QS();
						}
					}
					break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Sidestep(target)){
							B.Add(the_name + " tries to line up a shot. ",this);
						}
						QS();
					}
					break;
				default:
					AI_Step(target);
					QS();
					break;
				}
				break;
			case ActorType.CYCLOPEAN_TITAN:
			{
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					if(DistanceFrom(target) > 2 && DistanceFrom(target) <= 12 && R.OneIn(15) && FirstActorInLine(target) == target){
						B.Add(TheName(true) + " lobs a huge rock! ",this,target);
						AnimateProjectile(target,'*',Color.Gray);
						pos tp = target.p;
						int plus_to_hit = -target.TotalSkill(SkillType.DEFENSE)*3;
						if(target.IsHit(plus_to_hit)){
							B.Add("It hits " + target.the_name + "! ",target);
							if(target.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(4,6),this,"a cyclopean titan's rock")){
								if(R.OneIn(8)){
									target.ApplyStatus(AttrType.STUNNED,R.Between(3,4)*100);
								}
							}
						}
						else{
							int armor_value = target.TotalProtectionFromArmor();
							if(target != player){
								armor_value = target.TotalSkill(SkillType.DEFENSE); //if monsters have Defense skill, it's from armor
							}
							int roll = R.Roll(25 - plus_to_hit);
							if(roll <= armor_value * 3){
								B.Add(target.Your() + " armor blocks it! ",target);
							}
							else{
								if(target.HasAttr(AttrType.ROOTS) && roll <= (armor_value + 10) * 3){ //potion of roots gives 10 defense
									B.Add(target.Your() + " root shell blocks it! ",target);
								}
								else{
									B.Add(target.You("avoid") + " it! ",target);
								}
							}
						}
						foreach(pos neighbor in tp.PositionsWithinDistance(1)){
							Tile t = M.tile[neighbor];
							if(t.Is(TileType.FLOOR) && R.OneIn(4)){
								t.Toggle(null,TileType.GRAVEL);
							}
						}
						Q1();
					}
					else{
						bool smashed = false;
						if(DistanceFrom(target) == 2 && !HasLOE(target)){
							Tile t = FirstSolidTileInLine(target);
							if(t != null && !t.passable){
								smashed = true;
								B.Add(You("smash",true) + " through " + t.TheName(true) + "! ",t);
								foreach(int dir in DirectionOf(t).GetArc(1)){
									TileInDirection(dir).Smash(dir);
								}
								Move(t.row,t.col);
								QS();
							}
						}
						if(!smashed){
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			}
			case ActorType.ALASI_SENTINEL:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(HasAttr(AttrType.JUST_FLUNG)){
						attrs[AttrType.JUST_FLUNG] = 0;
					}
					else{
						if(target != null){
							List<Tile> valid_dirs = new List<Tile>();
							foreach(Tile t in target.TilesAtDistance(1)){
								if(t.passable && t.actor() == null && DistanceFrom(t) == 1){
									valid_dirs.Add(t);
								}
							}
							if(valid_dirs.Count > 0){
								AI_Step(valid_dirs.Random());
							}
						}
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.NOXIOUS_WORM:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12 && HasLOE(target)){
					B.Add(TheName(true) + " breathes poisonous gas. ");
					List<Tile> area = new List<Tile>();
					foreach(Tile t in target.TilesWithinDistance(1)){
						if(t.passable && target.HasLOE(t)){
							t.AddFeature(FeatureType.POISON_GAS);
							area.Add(t);
						}
					}
					List<Tile> area2 = target.tile().AddGaseousFeature(FeatureType.POISON_GAS,8);
					area.AddRange(area2);
					Event.RemoveGas(area,600,FeatureType.POISON_GAS,18);
					RefreshDuration(AttrType.COOLDOWN_1,(R.Roll(6) + 18) * 100);
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.LASHER_FUNGUS:
			{
				if(DistanceFrom(target) <= 12){
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						if(FirstActorInLine(target) == target){
							List<Tile> line = GetBestLineOfEffect(target.row,target.col);
							line.Remove(line[line.Count-1]);
							AnimateBoltBeam(line,Color.DarkGreen);
							pos target_p = target.p;
							if(Attack(1,target) && M.actor[target_p] != null){
								target = M.actor[target_p];
								int rowchange = 0;
								int colchange = 0;
								if(target.row < row){
									rowchange = 1;
								}
								if(target.row > row){
									rowchange = -1;
								}
								if(target.col < col){
									colchange = 1;
								}
								if(target.col > col){
									colchange = -1;
								}
								if(!target.AI_MoveOrOpen(target.row+rowchange,target.col+colchange)){
									bool moved = false;
									if(Math.Abs(target.row - row) > Math.Abs(target.col - col)){
										if(target.AI_Step(M.tile[row,target.col])){
											moved = true;
										}
									}
									else{
										if(Math.Abs(target.row - row) < Math.Abs(target.col - col)){
											if(target.AI_Step(M.tile[target.row,col])){
												moved = true;
											}
										}
										else{
											if(target.AI_Step(this)){
												moved = true;
											}
										}
									}
									if(!moved){ //todo: this still isn't ideal. maybe I need an AI_Step that only considers 3 directions - right now, it'll make you move even if it isn't closer.
										B.Add(target.You("do",true) + "n't move far. ",target);
									}
								}
							}
						}
						else{
							Q1();
						}
					}
				}
				else{
					Q1();
				}
				break;
			}
			case ActorType.LUMINOUS_AVENGER:
			{
				if(DistanceFrom(target) <= 3){
					List<Tile> ext = GetBestExtendedLineOfEffect(target);
					int max_count = Math.Min(5,ext.Count); //look 4 spaces away unless the line is even shorter than that.
					List<Actor> targets = new List<Actor>();
					Tile destination = null;
					for(int i=0;i<max_count;++i){
						Tile t = ext[i];
						if(t.passable){
							if(t.actor() == null){
								if(targets.Contains(target)){
									destination = t;
								}
							}
							else{
								if(t.actor() != this){
									targets.Add(t.actor());
								}
							}
						}
						else{
							break;
						}
					}
					if(destination != null){
						Move(destination.row,destination.col);
						foreach(Tile t in ext.To(destination)){
							colorchar cch = M.VisibleColorChar(t.row,t.col);
							cch.bgcolor = Color.Yellow;
							if(Global.LINUX && !Screen.GLMode){
								cch.bgcolor = Color.DarkYellow;
							}
							if(cch.color == cch.bgcolor){
								cch.color = Color.Black;
							}
							Screen.WriteMapChar(t.row,t.col,cch);
							Game.GLUpdate();
							Thread.Sleep(15);
						}
						foreach(Actor a in targets){
							Attack(0,a,true);
						}
						Q1();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			}
			case ActorType.VAMPIRE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					if(DistanceFrom(target) <= 12){
						if(tile().IsLit() && !HasAttr(AttrType.COOLDOWN_1)){
							attrs[AttrType.COOLDOWN_1]++;
							B.Add(the_name + " gestures. ",this);
							List<Tile> tiles = new List<Tile>();
							foreach(Tile t in target.TilesWithinDistance(6)){
								if(t.passable && t.actor() == null && DistanceFrom(t) >= DistanceFrom(target)
								&& target.HasLOS(t) && target.HasLOE(t)){
									tiles.Add(t);
								}
							}
							if(tiles.Count == 0){
								foreach(Tile t in target.TilesWithinDistance(6)){ //same, but with no distance requirement
									if(t.passable && t.actor() == null && target.HasLOS(t) && target.HasLOE(t)){
										tiles.Add(t);
									}
								}
							}
							if(tiles.Count == 0){
								B.Add("Nothing happens. ",this);
							}
							else{
								if(tiles.Count == 1){
									B.Add("A blood moth appears! ");
								}
								else{
									B.Add("Blood moths appear! ");
								}
								for(int i=0;i<2;++i){
									if(tiles.Count > 0){
										Tile t = tiles.RemoveRandom();
										Create(ActorType.BLOOD_MOTH,t.row,t.col,TiebreakerAssignment.InsertAfterCurrent);
										M.actor[t.row,t.col].player_visibility_duration = -1;
									}
								}
							}
							Q1();
						}
						else{
							AI_Step(target);
							QS();
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.ORC_WARMAGE:
			{
				if(SilencedThisTurn()){
					return;
				}
				switch(DistanceFrom(target)){
				case 1:
				{
					List<SpellType> close_range = new List<SpellType>();
					close_range.Add(SpellType.MAGIC_HAMMER);
					close_range.Add(SpellType.MAGIC_HAMMER);
					close_range.Add(SpellType.BLINK);
					if(target.EnemiesAdjacent() > 1 || R.CoinFlip()){
						CastCloseRangeSpellOrAttack(close_range,target,false);
					}
					else{
						if(AI_Step(target,true)){
							QS();
						}
						else{
							CastCloseRangeSpellOrAttack(close_range,target,false);
						}
					}
					break;
				}
				case 2:
					if(R.CoinFlip()){
						if(AI_Step(target,true)){
							QS();
						}
						else{
							if(FirstActorInLine(target) == target){
								CastRangedSpellOrMove(target);
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					else{
						if(FirstActorInLine(target) == target){
							CastRangedSpellOrMove(target);
						}
						else{
							if(AI_Step(target,true)){
								QS();
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
					if(FirstActorInLine(target) == target){
						CastRangedSpellOrMove(target);
					}
					else{
						AI_Sidestep(target);
						QS();
					}
					break;
				default:
					AI_Step(target);
					QS();
					break;
				}
				break;
			}
			case ActorType.NECROMANCER:
			{
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(R.Roll(4)+8)*100,AttrType.COOLDOWN_1));
					B.Add(the_name + " calls out to the dead. ",this);
					ActorType summon = R.CoinFlip()? ActorType.SKELETON : ActorType.ZOMBIE;
					List<Tile> tiles = new List<Tile>();
					foreach(Tile tile in TilesWithinDistance(2)){
						if(tile.passable && tile.actor() == null && DirectionOf(tile) == DirectionOf(target)){
							tiles.Add(tile);
						}
					}
					if(tiles.Count == 0){
						foreach(Tile tile in TilesWithinDistance(2)){
							if(tile.passable && tile.actor() == null){
								tiles.Add(tile);
							}
						}
					}
					if(tiles.Count == 0 || (group != null && group.Count > 3)){
						B.Add("Nothing happens. ",this);
					}
					else{
						Tile t = tiles.Random();
						B.Add(Prototype(summon).a_name + " digs through the floor! ");
						Create(summon,t.row,t.col,TiebreakerAssignment.InsertAfterCurrent);
						M.actor[t.row,t.col].player_visibility_duration = -1;
						if(group == null){
							group = new List<Actor>{this};
						}
						group.Add(M.actor[t.row,t.col]);
						M.actor[t.row,t.col].group = group;
					}
					Q1();
				}
				else{
					bool blast = false;
					switch(DistanceFrom(target)){
					case 1:
						if(AI_Step(target,true)){
							QS();
						}
						else{
							Attack(0,target);
						}
						break;
					case 2:
						if(R.CoinFlip() && FirstActorInLine(target) == target){
							blast = true;
						}
						else{
							if(AI_Step(target,true)){
								QS();
							}
							else{
								blast = true;
							}
						}
						break;
					case 3:
					case 4:
					case 5:
					case 6:
						if(FirstActorInLine(target) == target){
							blast = true;
						}
						else{
							AI_Sidestep(target);
							QS();
						}
						break;
					default:
						AI_Step(target);
						QS();
						break;
					}
					if(blast){
						B.Add(TheName(true) + " fires dark energy at " + target.TheName(true) + ". ",this,target);
						AnimateBoltProjectile(target,Color.DarkBlue);
						if(target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(6),this,"*blasted by a necromancer")){
							target.IncreaseExhaustion(R.Roll(3));
						}
						Q1();
					}
				}
				break;
			}
			case ActorType.STALKING_WEBSTRIDER:
			{
				bool burrow = false;
				if(DistanceFrom(target) >= 2 && DistanceFrom(target) <= 6){
					if(R.CoinFlip() && !target.tile().Is(FeatureType.WEB)){
						burrow = true;
					}
				}
				if((DistanceFrom(target) > 6 || target.HasAttr(AttrType.POISONED))){
					burrow = true;
				}
				if(burrow && !HasAttr(AttrType.COOLDOWN_1)){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(5,8)*100);
					if(DistanceFrom(target) <= 2){
						Burrow(TilesWithinDistance(6));
					}
					else{
						Burrow(GetCone(DirectionOf(target),6,true));
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.ORC_ASSASSIN:
				if(DistanceFrom(target) > 2 && attrs[AttrType.TURNS_VISIBLE] < 0){
					Tile t = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null && target.DistanceFrom(x) == target.DistanceFrom(this)-1 && !target.CanSee(x)).Random();
					if(t != null){
						AI_Step(t);
						FindPath(target); //so it won't forget where the target is...
						QS();
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						if(DistanceFrom(target) == 1){
							Attack(1,target);
						}
						else{
							QS();
						}
					}
				}
				break;
			case ActorType.MACHINE_OF_WAR:
			{
				if(attrs[AttrType.COOLDOWN_1] % 2 == 0){ //the machine of war moves on even turns and fires on odd turns.
					AI_Step(target);
					QS();
				}
				else{
					if(DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
						B.Add(TheName(true) + " fires a stream of scalding oil at " + target.the_name + ". ",target);
						List<Tile> covered_in_oil = GetBestLineOfEffect(target);
						List<Tile> added = new List<Tile>();
						foreach(Tile t in covered_in_oil){
							foreach(int dir in U.FourDirections){
								Tile neighbor = t.TileInDirection(dir);
								if(R.OneIn(3) && neighbor.passable && !covered_in_oil.Contains(neighbor)){
									added.AddUnique(neighbor);
								}
							}
						}
						covered_in_oil.AddRange(added);
						List<pos> cells = new List<pos>();
						List<Actor> oiled_actors = new List<Actor>();
						for(int i=0;covered_in_oil.Count > 0;++i){
							List<Tile> removed = new List<Tile>();
							foreach(Tile t in covered_in_oil){
								if(DistanceFrom(t) == i){
									t.AddFeature(FeatureType.OIL);
									if(t.actor() != null && t.actor() != this){
										oiled_actors.Add(t.actor());
									}
									removed.Add(t);
									if(DistanceFrom(t) > 0){
										cells.Add(t.p);
									}
								}
							}
							foreach(Tile t in removed){
								covered_in_oil.Remove(t);
							}
							if(cells.Count > 0){
								Screen.AnimateMapCells(cells,new colorchar(',',Color.DarkYellow),20);
							}
						}
						oiled_actors.AddUnique(target);
						M.Draw();
						foreach(Actor a in oiled_actors){
							if(a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(4,6),this,"a stream of scalding oil")){
								if(a.IsBurning()){
									a.ApplyBurning();
								}
								else{
									if(!a.HasAttr(AttrType.SLIMED,AttrType.FROZEN)){
										a.attrs[AttrType.OIL_COVERED]++;
										B.Add(a.YouAre() + " covered in oil. ",a);
									}
								}
							}
						}
						Q1();
					}
					else{
						Q1();
					}
				}
				break;
			}
			case ActorType.IMPOSSIBLE_NIGHTMARE:
			{
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					Tile t = target.TilesAtDistance(DistanceFrom(target)-1).Where(x=>x.passable && x.actor() == null).Random();
					if(t != null){
						Move(t.row,t.col); //todo: fear effect?
					}
					QS();
				}
				break;
			}
			case ActorType.FIRE_DRAKE:
				/*if(player.magic_trinkets.Contains(MagicTrinketType.RING_OF_RESISTANCE) && DistanceFrom(player) <= 12 && CanSee(player)){
					B.Add(the_name + " exhales an orange mist toward you. ");
					foreach(Tile t in GetBestLineOfEffect(player)){
						Screen.AnimateStorm(t.p,1,2,3,'*',Color.Red);
					}
					B.Add("Your ring of resistance melts and drips onto the floor! ");
					player.magic_trinkets.Remove(MagicTrinketType.RING_OF_RESISTANCE);
					Q.Add(new Event(this,100,EventType.MOVE));
				}
				else{
					if(player.EquippedArmor == ArmorType.FULL_PLATE_OF_RESISTANCE && DistanceFrom(player) <= 12 && CanSee(player)){
						B.Add(the_name + " exhales an orange mist toward you. ");
						foreach(Tile t in GetBestLine(player)){
							Screen.AnimateStorm(t.p,1,2,3,'*',Color.Red);
						}
						B.Add("The runes drip from your full plate of resistance! ");
						player.EquippedArmor = ArmorType.FULL_PLATE;
						player.UpdateOnEquip(ArmorType.FULL_PLATE_OF_RESISTANCE,ArmorType.FULL_PLATE);
						Q.Add(new Event(this,100,EventType.MOVE));
					}
					else{*/
					if(!HasAttr(AttrType.COOLDOWN_1)){
						if(DistanceFrom(target) <= 12){
							attrs[AttrType.COOLDOWN_1]++;
							int cooldown = (R.Roll(1,4)+1) * 100;
							Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_1));
							AnimateBeam(target,Color.RandomFire,'*');
							B.Add(TheName(true) + " breathes fire. ",target);
							target.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(6,6),this,"*roasted by fire breath");
						target.ApplyBurning();
							Q.Add(new Event(this,200,EventType.MOVE));
						}
						else{
							AI_Step(target);
							QS();
						}
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(R.Roll(1,2)-1,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
					//}
				//}
				break;
			case ActorType.GHOST:
			{
				attrs[AttrType.AGGRESSION_MESSAGE_PRINTED] = 1;
				bool tombstone = false;
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.type == TileType.TOMBSTONE){
						tombstone = true;
					}
				}
				if(!tombstone){
					B.Add("The ghost vanishes. ",this);
					Kill();
					return;
				}
				if(target == null || DistanceFrom(target) > 2){
					List<Tile> valid = TilesAtDistance(1).Where(x=>x.TilesWithinDistance(1).Any(y=>y.type == TileType.TOMBSTONE));
					if(valid.Count > 0){
						AI_Step(valid.Random());
					}
					QS();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						List<Tile> valid = tile().NeighborsBetween(target.row,target.col).Where(x=>x.passable && x.actor() == null && x.TilesWithinDistance(1).Any(y=>y.type == TileType.TOMBSTONE));
						if(valid.Count == 0){
							valid = TilesAtDistance(1).Where(x=>x.TilesWithinDistance(1).Any(y=>y.type == TileType.TOMBSTONE));
						}
						if(valid.Count > 0){
							AI_Step(valid.Random());
						}
						QS();
					}
				}
				break;
			}
			case ActorType.BLADE:
			{
				attrs[AttrType.AGGRESSION_MESSAGE_PRINTED] = 1;
				List<Actor> valid_targets = new List<Actor>(); //this is based on EnragedMove(), with an exception for other blades
				int max_dist = Math.Max(Math.Max(row,col),Math.Max(ROWS-row,COLS-col)); //this should find the farthest edge of the map
				for(int i=1;i<max_dist && valid_targets.Count == 0;++i){
					foreach(Actor a in ActorsAtDistance(i)){
						if(a.type != ActorType.BLADE && CanSee(a) && HasLOE(a)){
							valid_targets.Add(a);
						}
					}
				}
				if(valid_targets.Count > 0){
					if(target == null || !valid_targets.Contains(target)){ //keep old target if possible
						target = valid_targets.Random();
					}
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				else{
					if(target != null){
						SeekAI();
					}
					else{
						QS();
					}
				}
				break;
			}
			case ActorType.PHANTOM_CONSTRICTOR:
			case ActorType.PHANTOM_WASP:
			{
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					List<Tile> tiles = new List<Tile>(); //i should turn this "slither" movement into a standardized attribute or something
					if(target.row == row || target.col == col){
						int targetdir = DirectionOf(target);
						for(int i=-1;i<=1;++i){
							pos adj = p.PosInDir(targetdir.RotateDir(true,i));
							if(M.tile[adj].passable && M.actor[adj] == null){
								tiles.Add(M.tile[adj]);
							}
						}
					}
					if(tiles.Count > 0){
						AI_Step(tiles.Random());
					}
					else{
						AI_Step(target);
					}
					QS();
				}
				break;
			}
			case ActorType.MINOR_DEMON:
			case ActorType.FROST_DEMON:
			case ActorType.BEAST_DEMON:
			case ActorType.DEMON_LORD:
			{
				int damage_threshold = 1;
				if(type == ActorType.BEAST_DEMON){
					damage_threshold = 0;
				}
				if(target == player && attrs[AttrType.COOLDOWN_2] > damage_threshold && CanSee(target)){
					switch(type){
					case ActorType.MINOR_DEMON:
					case ActorType.BEAST_DEMON:
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
						break;
					case ActorType.FROST_DEMON:
						if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
							attrs[AttrType.COOLDOWN_1] = 1;
							AnimateProjectile(target,'*',Color.RandomIce);
							foreach(Tile t in GetBestLineOfEffect(target)){
								t.ApplyEffect(DamageType.COLD);
							}
							B.Add(TheName(true) + " fires a chilling sphere. ",target);
							if(target.TakeDamage(DamageType.COLD,DamageClass.PHYSICAL,R.Roll(3,6),this,"a frost demon")){
								target.ApplyStatus(AttrType.SLOWED,R.Between(4,7)*100);
								//target.RefreshDuration(AttrType.SLOWED,R.Between(4,7)*100,target.YouAre() + " no longer slowed. ",target);
							}
							Q1();
						}
						else{
							if(DistanceFrom(target) == 1){
								Attack(0,target);
							}
							else{
								AI_Step(target);
								QS();
							}
						}
						break;
					case ActorType.DEMON_LORD:
						if(DistanceFrom(target) > 2){
							AI_Step(target);
							QS();
						}
						else{
							if(FirstActorInLine(target) != null){
								Attack(0,target);
							}
							else{
								if(AI_Step(target)){
									QS();
								}
								else{
									AI_Sidestep(target);
									QS();
								}
							}
						}
						break;
					}
				}
				else{
					if(row >= 7 && row <= 12 && col >= 30 && col <= 35){ //near the center
						foreach(Actor a in ActorsAtDistance(1)){
							if(a.IsFinalLevelDemon()){
								List<Tile> dist2 = new List<Tile>();
								foreach(Tile t in TilesWithinDistance(5)){
									if(t.TilesAtDistance(2).Any(x=>x.type == TileType.FIRE_RIFT) && !t.TilesAtDistance(1).Any(x=>x.type == TileType.FIRE_RIFT)){
										dist2.Add(t);
									}
								} //if there's another distance 2 (from the center) tile with no adjacent demons, move there
								//List<Tile> valid = dist2.Where(x=>DistanceFrom(x) == 1 && x.actor() == null && !x.TilesAtDistance(1).Any(y=>y.actor() != null && y.actor().Is(ActorType.MINOR_DEMON,ActorType.FROST_DEMON,ActorType.BEAST_DEMON,ActorType.DEMON_LORD)));
								List<Tile> valid = dist2.Where(x=>DistanceFrom(x) == 1);
								valid = valid.Where(x=>x.actor() == null && !x.TilesAtDistance(1).Any(y=>y.actor() != null && y.actor() != this && y.actor().IsFinalLevelDemon()));
								if(valid.Count > 0){
									AI_Step(valid.Random());
								}
								break;
							}
						}
						if(player.HasLOS(this)){
							B.Add(TheName(true) + " chants. ",this);
						}
						M.IncrementClock();
						Q1();
					}
					else{
						if(path != null && path.Count > 0){
							if(!PathStep()){
								QS();
							}
						}
						else{
							FindPath(9+R.Between(0,1),32+R.Between(0,1));
							if(!PathStep()){
								QS();
							}
						}
					}
				}
				break;
			}
			default:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			}
		}
		public void SeekAI(){
			if(type == ActorType.MACHINE_OF_WAR && attrs[AttrType.COOLDOWN_1] % 2 == 1){
				Q1();
				return;
			}
			if(type == ActorType.ROBED_ZEALOT && HasAttr(AttrType.COOLDOWN_3)){ //todo: move most of these into the switch statement. use goto default. 
				attrs[AttrType.COOLDOWN_3]--;
				Q1();
				return;
			}
			if(type == ActorType.SKULKING_KILLER && HasAttr(AttrType.KEEPS_DISTANCE) && !R.OneIn(5) && (!player.HasLOE(this) || !player.CanSee(this) || DistanceFrom(player) > 12)){
				attrs[AttrType.COOLDOWN_2]++;
				if(attrs[AttrType.COOLDOWN_2] >= 3){
					attrs[AttrType.KEEPS_DISTANCE] = 0;
					attrs[AttrType.COOLDOWN_2] = 0;
					if(!player.CanSee(this)){
						attrs[AttrType.TURNS_VISIBLE] = 0;
					}
				}
				Q1();
				return;
			}
			if(type == ActorType.STALKING_WEBSTRIDER && tile().Is(FeatureType.WEB)){
				List<pos> webs = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].Is(FeatureType.WEB));
				if(webs.Contains(target.p)){
					FindPath(target);
					if(PathStep()){
						return;
					}
					else{
						path.Clear();
					}
				}
			}
			if(type == ActorType.SWORDSMAN || type == ActorType.PHANTOM_SWORDMASTER || type == ActorType.ALASI_SOLDIER){
				attrs[AttrType.COMBO_ATTACK] = 0;
			}
			if(PathStep()){ //todo: consider the placement of this call. does this all happen in the correct order?
				return;
			}
			switch(type){
			case ActorType.KOBOLD:
			{
				if(HasAttr(AttrType.COOLDOWN_1)){ //if the kobold needs to reload...
					if(target != null && DistanceFrom(target) <= 2){ //would be better as pathing distance, but oh well
						AI_Flee();
						QS();
						return;
					}
					else{
						B.Add(the_name + " starts reloading. ",this);
						attrs[AttrType.COOLDOWN_1] = 0;
						Q1();
						RefreshDuration(AttrType.COOLDOWN_2,R.Between(5,6)*100 - 50);
						return;
					}
				}
				else{
					goto default;
				}
			}
			case ActorType.PHASE_SPIDER:
			case ActorType.IMPOSSIBLE_NIGHTMARE:
				if(DistanceFrom(target_location) <= 12){
					Tile t = target_location.TilesAtDistance(DistanceFrom(target_location)-1).Where(x=>x.passable && x.actor() == null).Random();
					if(t != null){
						Move(t.row,t.col);
					}
				}
				QS();
				break;
			case ActorType.ORC_WARMAGE: //warmages not following the player has worked pretty well so far. Maybe they could get a chance to go back to wandering?
				QS();
				break;
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.MUD_TENTACLE:
			case ActorType.LASHER_FUNGUS:
			case ActorType.MARBLE_HORROR_STATUE:
				QS();
				break;
			case ActorType.CYCLOPEAN_TITAN:
			{
				bool smashed = false;
				if(target_location != null && target != null && DistanceFrom(target_location) == 1 && DistanceFrom(target) == 2 && !HasLOE(target)){
					Tile t = FirstSolidTileInLine(target);
					if(t != null && !t.passable){
						smashed = true;
						B.Add(You("smash",true) + " through " + t.TheName(true) + "! ",t);
						foreach(int dir in DirectionOf(t).GetArc(1)){
							TileInDirection(dir).Smash(dir);
						}
						Move(t.row,t.col);
						QS();
					}
				}
				if(!smashed){
					goto default;
				}
				break;
			}
			case ActorType.FIRE_DRAKE:
				FindPath(player);
				QS();
				break;
			default:
			{
				if(target_location != null){
					if(DistanceFrom(target_location) == 1 && M.actor[target_location.p] != null){
						if(MovementPrevented(target_location) || M.actor[target_location.p].MovementPrevented(tile()) || !AI_WillingToMove(tile(),target_location,target)){
							QS();
						}
						else{
							Move(target_location.row,target_location.col); //swap places
							target_location = null;
							attrs[AttrType.FOLLOW_DIRECTION_EXITED]++;
							QS();
						}
					}
					else{
						int dist = DistanceFrom(target_location);
						if(!HasLOE(target_location)){
							List<pos> path2 = GetPath(target_location,dist+1);
							if(path2.Count > 0){
								path = path2;
								if(PathStep()){
									return;
								}
							}
						}
						if(AI_Step(target_location)){
							QS();
							if(DistanceFrom(target_location) == 0){
								target_location = null;
								attrs[AttrType.FOLLOW_DIRECTION_EXITED]++;
							}
							else{
								if(DistanceFrom(target_location) == dist && !HasLOE(target_location)){ //if you didn't get any closer and you can't see it...
									target_location = null;
								}
							}
						}
						else{ //could not move, end turn.
							if((DistanceFrom(target_location) == 1 && !target_location.passable) || DistanceFrom(target_location) == 0){
								target_location = null;
							}
							QS();
						}
					}
					if(target_location == null){
						if(!NeverWanders()){
							if(group == null || group.Count < 2 || group[0] == this){
								attrs[AttrType.WANDERING] = 1;
							}
						}
					}
				}
				else{
					if(DistanceFrom(target) <= 2 && !HasLOS(target) && !HasLOE(target)){ //this part is just for pillar dancing
						List<pos> path2 = GetPath(target,2);
						if(path2.Count > 0){
							path = path2;
							player_visibility_duration = -1; //stay at -1 while in close pursuit
						}
						if(PathStep()){
							path.Clear(); //testing this; seems to be working.
							return;
						}
						QS();
					}
					else{
						if(HasAttr(AttrType.FOLLOW_DIRECTION_EXITED) && tile().direction_exited > 0){
							AI_Step(TileInDirection(tile().direction_exited));
							attrs[AttrType.FOLLOW_DIRECTION_EXITED] = 0;
						}
						else{
							bool corridor = HasAttr(AttrType.DIRECTION_OF_PREVIOUS_TILE); //if it's 0 or -1, ignore it
							foreach(int dir in U.FourDirections){
								if(TileInDirection(dir).passable && TileInDirection(dir.RotateDir(true,1)).passable && TileInDirection(dir.RotateDir(true,2)).passable){
									corridor = false;
									break;
								}
							}
							if(corridor){
								List<int> blocked = new List<int>();
								for(int i=-1;i<=1;++i){
									blocked.Add(attrs[AttrType.DIRECTION_OF_PREVIOUS_TILE].RotateDir(true,i));
								}
								List<Tile> tiles = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null && !blocked.Contains(DirectionOf(x)));
								if(tiles.Count > 0){
									bool multiple_paths = false;
									foreach(Tile t1 in tiles){
										foreach(Tile t2 in tiles){
											if(t1 != t2 && t1.ApproximateEuclideanDistanceFromX10(t2) > 10){ //cardinally adjacent only
												multiple_paths = true;
												break;
											}
										}
										if(multiple_paths){
											if(player_visibility_duration < 0){
												player_visibility_duration -= 2; //at each fork in the road, the monster gets 3 steps closer to forgetting the player - just because 1 takes too long when the corridors are loopy.
											}
											break;
										}
									}
									if(!multiple_paths && player_visibility_duration < -1){ //this part could use some documentation.
										++player_visibility_duration;
									}
									AI_Step(tiles.Random()); //this whole section deals with monsters following corridors instead of giving up on the chase.
								}
							}
							else{
								if(group != null && group[0] != this){ //groups try to get back together
									if(DistanceFrom(group[0]) > 1){
										int dir = DirectionOf(group[0]);
										bool found = false;
										for(int i=-1;i<=1;++i){
											Actor a = ActorInDirection(dir.RotateDir(true,i));
											if(a != null && group.Contains(a)){
												found = true;
												break;
											}
										}
										if(!found){
											if(HasLOS(group[0])){
												AI_Step(group[0]);
											}
											else{
												FindPath(group[0],8);
												if(PathStep()){
													return;
												}
											}
										}
									}
								}
							}
						}
						QS();
					}
				}
				break;
			}
			}
		}
		public void IdleAI(){
			if(type == ActorType.MACHINE_OF_WAR && attrs[AttrType.COOLDOWN_1] % 2 == 1){
				Q1();
				return;
			}
			if(type == ActorType.OGRE_BARBARIAN){
				speed = 100;
				attrs[AttrType.COOLDOWN_1] = 0;
			}
			if(PathStep()){
				return;
			}
			switch(type){
			case ActorType.GIANT_BAT: //flies around
			case ActorType.PHANTOM_BLIGHTWING:
				AI_Step(TileInDirection(Global.RandomDirection()));
				QS();
				return; //<--!
			case ActorType.NOXIOUS_WORM:
			{
				if(TilesWithinDistance(1).All(x=>x.Is(TileType.WALL,TileType.CRACKED_WALL,TileType.WAX_WALL))){
					if(DistanceFrom(player) == 2){
						player_visibility_duration = -1;
						Tile t = tile().NeighborsBetween(player.row,player.col).Random();
						Move(t.row,t.col);
						t.TurnToFloor();
						B.Add(AName(true) + " bursts through the wall! ",t);
						B.Print(true);
						List<Tile> area = t.AddGaseousFeature(FeatureType.POISON_GAS,5);
						if(area.Count > 0){
							Q.RemoveTilesFromEventAreas(area,EventType.REMOVE_GAS);
							Event.RemoveGas(area,300,FeatureType.POISON_GAS,18);
						}
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(2,5)*100);
						Q1();
						return;
					}
					else{
						List<Tile> valid = TilesAtDistance(1).Where(x=>x.Is(TileType.CRACKED_WALL) && !x.TilesAtDistance(1).Any(y=>y.passable));
						if(valid.Count > 0){
							Tile t = valid.Random();
							Move(t.row,t.col);
							QS();
							return;
						}
					}
				}
				break;
			}
			case ActorType.STALKING_WEBSTRIDER:
				if(tile().Is(FeatureType.WEB)){
					List<pos> webs = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].Is(FeatureType.WEB));
					if(webs.Contains(player.p)){
						player_visibility_duration = -1; //todo: check this: what does PLAYER_NOTICED do again?
						FindPath(player);
						if(PathStep()){
							return;
						}
						else{
							path.Clear();
						}
					}
				}
				break;
			case ActorType.ORC_WARMAGE:
			{
				int num = (curmp - 40) * 10; //100 at full(50) mana, 0 at 40 mana
				if(!IsSilencedHere() && num > 0 && !HasAttr(AttrType.DETECTING_MOVEMENT) && HasSpell(SpellType.DETECT_MOVEMENT)){
					if(R.PercentChance(num/2)){
						CastSpell(SpellType.DETECT_MOVEMENT);
						return; //<--!
					}
				}
				break;
			}
			case ActorType.SWORDSMAN:
			case ActorType.PHANTOM_SWORDMASTER:
			case ActorType.ALASI_SOLDIER:
				attrs[AttrType.COMBO_ATTACK] = 0;
				break;
			case ActorType.FIRE_DRAKE:
				FindPath(player);
				QS();
				return; //<--!
			case ActorType.FINAL_LEVEL_CULTIST:
			{
				pos circle = M.FinalLevelSummoningCircle(attrs[AttrType.COOLDOWN_2]);
				if(!circle.PositionsWithinDistance(2).Any(x=>M.tile[x].Is(TileType.DEMONIC_IDOL))){
					List<int> valid_circles = new List<int>(); //if that one is broken, find a new one...
					for(int i=0;i<5;++i){
						if(M.FinalLevelSummoningCircle(i).PositionsWithinDistance(2).Any(x=>M.tile[x].Is(TileType.DEMONIC_IDOL))){
							valid_circles.Add(i);
						}
					}
					int nearest = valid_circles.WhereLeast(x=>DistanceFrom(M.FinalLevelSummoningCircle(x))).Random();
					attrs[AttrType.COOLDOWN_2] = nearest;
					circle = M.FinalLevelSummoningCircle(nearest);
				}
				if(DistanceFrom(circle) > 1){
					FindPath(circle.row,circle.col);
					if(PathStep()){
						return;
					}
				}
				QS();
				return; //<--!
			}
			default:
				break;
			}
			if(HasAttr(AttrType.WANDERING)){
				if(R.Roll(10) <= 6){
					List<Tile> in_los = new List<Tile>();
					foreach(Tile t in M.AllTiles()){
						if(t.passable && CanSee(t)){
							in_los.Add(t);
						}
					}
					if(in_los.Count > 0){
						FindPath(in_los.Random());
					}
				}
				else{
					if(R.OneIn(4)){
						List<Tile> passable = new List<Tile>();
						foreach(Tile t in M.AllTiles()){
							if(t.passable){
								passable.Add(t);
							}
						}
						if(passable.Count > 0){
							FindPath(passable.Random());
						}
					}
					else{
						List<Tile> nearby = new List<Tile>();
						foreach(Tile t in M.AllTiles()){
							if(t.passable && DistanceFrom(t) <= 12){
								nearby.Add(t);
							}
						}
						if(nearby.Count > 0){
							FindPath(nearby.Random());
						}
					}
				}
				if(PathStep()){
					return;
				}
				QS();
			}
			else{
				if(group != null && group[0] != this){
					if(DistanceFrom(group[0]) > 1){
						int dir = DirectionOf(group[0]);
						bool found = false;
						for(int i=-1;i<=1;++i){
							Actor a = ActorInDirection(dir.RotateDir(true,i));
							if(a != null && group.Contains(a)){
								found = true;
								break;
							}
						}
						if(!found){
							if(HasLOS(group[0])){
								AI_Step(group[0]);
							}
							else{
								FindPath(group[0],8);
								if(PathStep()){
									return;
								}
							}
						}
					}
				}
				QS();
			}
		}
		private void Stagger(){
			string verb = "stagger";
			bool es = false;
			if(HasAttr(AttrType.FLYING)){
				verb = "careen";
			}
			else{
				if(Is(ActorType.SPITTING_COBRA,ActorType.MIMIC,ActorType.GIANT_SLUG,ActorType.SKITTERMOSS,ActorType.NOXIOUS_WORM,ActorType.CORROSIVE_OOZE,ActorType.IMPOSSIBLE_NIGHTMARE)){
					verb = "lurch";
					es = true;
				}
			}
			Tile t = null;
			if(HasAttr(AttrType.NONEUCLIDEAN_MOVEMENT)){
				if(target != null){
					t = target.TilesWithinDistance(DistanceFrom(target)+1).Where(x=>!x.solid_rock && x.DistanceFrom(target) >= DistanceFrom(target)-1).Random();
				}
			}
			else{
				t = TileInDirection(Global.RandomDirection());
			}
			if(t != null){
				if(MovementPrevented(t)){
					if(player.HasLOS(this)){
						if(type == ActorType.PLAYER){
							B.Add(You(verb,es) + " and almost fall over. ",this);
						}
						else{
							B.Add(You(verb,es) + " and almost falls over. ",this);
						}
					}
				}
				else{
					if(SlippedOrStruggled()){
						QS();
						return;
					}
					bool message_printed = false;
					Actor a = t.actor();
					if(a != null){
						pos original_pos = this.p;
						if(player.HasLOS(this)){
							B.Add(YouVisible(verb,es) + " into " + a.TheName(true) + ". ",this,a);
						}
						if(a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK) && a.maxhp == 1){
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,false,1,this);
						}
						else{
							if(type == ActorType.CYCLOPEAN_TITAN){
								a.attrs[AttrType.TURN_INTO_CORPSE]++;
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),this,"*trampled by " + a_name);
								KnockObjectBack(a,5,this);
								a.CorpseCleanup();
							}
						}
						if(type == ActorType.CYCLOPEAN_TITAN && p.Equals(original_pos) && t.actor() == null){
							message_printed = true; //keep going, but don't print any more messages
						}
						else{
							QS();
							return;
						}
					}
					if(!t.passable){
						if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.Is(TileType.CRACKED_WALL,TileType.DOOR_C,TileType.STALAGMITE,TileType.STATUE,TileType.RUBBLE)){
							if(!message_printed && player.HasLOS(this)){
								B.Add(YouVisible(verb,es) + ", smashing " + t.TheName(true) + ". ",this,t);
							}
							t.Smash(0);
							Move(t.row,t.col);
						}
						else{
							if(type == ActorType.CYCLOPEAN_TITAN && t.p.BoundsCheck(M.tile,false)){
								if(!message_printed && player.HasLOS(this)){
									B.Add(YouVisible(verb,es) + ", smashing through " + t.TheName(true) + ". ",this,t);
								}
								foreach(int dir in DirectionOf(t).GetArc(1)){
									TileInDirection(dir).Smash(dir);
								}
								Move(t.row,t.col);
							}
							else{
								if(!message_printed && player.CanSee(this)){
									B.Add(You(verb,es) + " into " + t.TheName(true) + ". ",t);
								}
								if(!HasAttr(AttrType.SMALL) || t.Is(TileType.POISON_BULB)){ //small monsters can't bump anything but fragile terrain like bulbs
									t.Bump(DirectionOf(t));
								}
							}
						}
					}
					else{
						if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.IsTrap()){
							t.SetName(Tile.Prototype(t.type).name);
							if(!message_printed && player.HasLOS(this)){
								B.Add(YouVisible(verb,es) + ", smashing " + t.TheName(true) + ". ",this,t);
							}
							t.TurnToFloor();
							Move(t.row,t.col);
						}
						else{
							if(!message_printed && player.HasLOS(this)){
								B.Add(You(verb,es) + ". ",this);
							}
							Move(t.row,t.col);
						}
					}
				}
			}
			QS();
		}
		private void ConfusedMove(){
			Tile t = null;
			if(HasAttr(AttrType.NONEUCLIDEAN_MOVEMENT)){
				if(target != null){
					t = target.TilesWithinDistance(DistanceFrom(target)+1).Where(x=>!x.solid_rock && x.DistanceFrom(target) >= DistanceFrom(target)-1).Random();
				}
			}
			else{
				t = TileInDirection(Global.RandomDirection());
			}
			if(t != null){
				Actor a = t.actor();
				if(a != null){
					if(a == this){
						B.Add(the_name + " hurts itself in its confusion. ",this); //for the rare case of a phase spider picking its own tile
						if(!TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null)){
							return;
						}
					}
					else{
						Attack(0,a);
						return;
					}
				}
				else{
					if(MovementPrevented(t)){
						if(player.HasLOS(this)){
							B.Add(You("almost fall") + " over in confusion. ",this);
						}
					}
					else{
						bool message_printed = false;
						if(SlippedOrStruggled()){
							QS();
							return;
						}
						if(!t.passable){
							if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.Is(TileType.CRACKED_WALL,TileType.DOOR_C,TileType.STALAGMITE,TileType.STATUE,TileType.RUBBLE)){
								if(!message_printed && player.HasLOS(this)){
									B.Add(YouVisible("smash",true) + " " + t.TheName(true) + ". ",this,t);
									//B.Add(YouVisible(verb,es) + ", smashing " + t.TheName(true) + ". ",this,t);
								}
								t.Smash(0);
								Move(t.row,t.col);
							}
							else{
								if(type == ActorType.CYCLOPEAN_TITAN && t.p.BoundsCheck(M.tile,false)){
									if(!message_printed && player.HasLOS(this)){
										B.Add(YouVisible("smash",true) + " through " + t.TheName(true) + ". ",this,t);
										//B.Add(YouVisible(verb,es) + ", smashing through " + t.TheName(true) + ". ",this,t);
									}
									foreach(int dir in DirectionOf(t).GetArc(1)){
										TileInDirection(dir).Smash(dir);
									}
									Move(t.row,t.col);
								}
								else{
									if(!message_printed && player.HasLOS(this)){
										B.Add(YouVisible("bump") + " into " + t.TheName(true) + ". ",this,t);
									}
									if(!HasAttr(AttrType.SMALL) || t.Is(TileType.POISON_BULB)){ //small monsters can't bump anything but fragile terrain like bulbs
										t.Bump(DirectionOf(t));
									}
								}
							}
						}
						else{
							if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.IsTrap()){
								t.SetName(Tile.Prototype(t.type).name);
								if(!message_printed && player.HasLOS(this)){
									B.Add(YouVisible("smash",true) + " " + t.TheName(true) + ". ",this,t);
								}
								t.TurnToFloor();
								Move(t.row,t.col);
							}
							else{
								if(!message_printed && player.HasLOS(this)){
									//B.Add(YouAre() + " confused! ",this);
								}
								Move(t.row,t.col);
							}
						}
					}
				}
			}
			QS();
		}
		private void EnragedMove(){
			List<Actor> valid_targets = new List<Actor>();
			int max_dist = Math.Max(Math.Max(row,col),Math.Max(ROWS-row,COLS-col)); //this should find the farthest edge of the map
			for(int i=1;i<max_dist && valid_targets.Count == 0;++i){
				foreach(Actor a in ActorsAtDistance(i)){
					if(CanSee(a) && HasLOE(a)){
						valid_targets.Add(a);
					}
				}
			}
			if(valid_targets.Count > 0){
				if(target == null || !valid_targets.Contains(target)){ //keep old target if possible
					target = valid_targets.Random();
				}
				int attack_dist = 1;
				switch(type){
				case ActorType.ALASI_SOLDIER:
				case ActorType.DEMON_LORD:
					attack_dist = 2;
					break;
				case ActorType.LASHER_FUNGUS:
					attack_dist = 12;
					break;
				}
				if(DistanceFrom(target) <= attack_dist){
					Attack(0,target);
				}
				else{
					if(HasAttr(AttrType.NONEUCLIDEAN_MOVEMENT)){
						Tile t = target.TilesAtDistance(DistanceFrom(target)-1).Where(x=>x.passable && x.actor() == null).Random();
						if(t != null){
							Move(t.row,t.col);
						}
					}
					else{
						AI_Step(target);
					}
					attrs[AttrType.COMBO_ATTACK] = 0;
					QS();
				}
			}
			else{
				if(target != null && this != player){
					SeekAI();
				}
				else{
					if(this == player && ResistedBySpirit()){
						B.Add("You manage to calm yourself. ");
						RefreshDuration(AttrType.ENRAGED,0);
					}
					else{
						B.Add(YouFeel() + " furious! ",this);
						AI_Step(TileInDirection(Global.RandomDirection()));
					}
					attrs[AttrType.COMBO_ATTACK] = 0;
					QS();
				}
			}
		}
		public void CalculateDimming(){
			if(M.wiz_lite || M.wiz_dark){
				return;
			}
			List<Actor> actors = new List<Actor>();
			foreach(Actor a in M.AllActors()){
				//if(a.light_radius > 0 || a.LightRadius() > 0){
				if(a.light_radius > 0 || a.HasAttr(AttrType.DIM_LIGHT)){
					actors.Add(a);
				}
			}
			foreach(Actor actor in actors){
				int dist = 100;
				Actor closest_shadow = null;
				foreach(Actor a in actor.ActorsWithinDistance(10)){
					if(a.type == ActorType.SHADOW){
						if(a.DistanceFrom(actor) < dist){
							dist = a.DistanceFrom(actor);
							closest_shadow = a;
						}
					}
				}
				if(closest_shadow == null){
					if(actor.HasAttr(AttrType.DIM_LIGHT)){
						int old = actor.LightRadius();
						actor.attrs[AttrType.DIM_LIGHT] = 0;
						if(old != actor.LightRadius()){
							actor.UpdateRadius(old,actor.LightRadius());
						}
						if(player.HasLOS(actor) && actor.LightRadius() > 0){
							B.Add(actor.Your() + " light grows brighter. ",actor);
						}
					}
				}
				else{
					Actor sh = closest_shadow; //laziness
					int dimness = 0;
					if(sh.DistanceFrom(actor) <= 1){
						dimness = 6;
					}
					else{
						if(sh.DistanceFrom(actor) <= 2){
							dimness = 5;
						}
						else{
							if(sh.DistanceFrom(actor) <= 3){
								dimness = 4;
							}
							else{
								if(sh.DistanceFrom(actor) <= 5){
									dimness = 3;
								}
								else{
									if(sh.DistanceFrom(actor) <= 7){
										dimness = 2;
									}
									else{
										if(sh.DistanceFrom(actor) <= 10){
											dimness = 1;
										}
									}
								}
							}
						}
					}
					if(dimness > actor.attrs[AttrType.DIM_LIGHT]){
						int old = actor.LightRadius();
						actor.attrs[AttrType.DIM_LIGHT] = dimness;
						if(player.HasLOS(actor) && actor.LightRadius() > 0){
							B.Add(actor.Your() + " light grows dimmer. ",actor);
						}
						if(old != actor.LightRadius()){
							actor.UpdateRadius(old,actor.LightRadius());
						}
					}
					else{
						if(dimness < actor.attrs[AttrType.DIM_LIGHT]){
							int old = actor.LightRadius();
							actor.attrs[AttrType.DIM_LIGHT] = dimness;
							if(old != actor.LightRadius()){
								actor.UpdateRadius(old,actor.LightRadius());
							}
							if(player.HasLOS(actor) && actor.LightRadius() > 0){
								B.Add(actor.Your() + " light grows brighter. ",actor);
							}
						}
					}
				}
			}
		}
		public void Burrow(List<Tile> area){
			if(player.CanSee(tile())){
				AnimateStorm(1,2,3,'*',Color.Gray);
			}
			B.Add(TheName(true) + " burrows into the ground. ",this,tile());
			M.RemoveTargets(this);
			if(group != null){
				foreach(Actor a in group){
					if(a != this){
						a.group = null;
					}
				}
				group.Clear();
				group = null;
			}
			if(LightRadius() > 0){
				UpdateRadius(LightRadius(),0);
			}
			M.actor[row,col] = null;
			row = -1;
			col = -1;
			int duration = R.Between(3,6);
			if(HasAttr(AttrType.REGENERATING)){
				curhp += attrs[AttrType.REGENERATING];
				if(curhp > maxhp){
					curhp = maxhp;
				}
			}
			attrs[AttrType.BURROWING] = 1;
			Event e = new Event(this,area,duration*100,EventType.BURROWING);
			e.tiebreaker = tiebreakers.IndexOf(this);
			Q.Add(e);
		}
		public bool AI_Step(PhysicalObject obj){ return AI_Step(obj,false); }
		public bool AI_Step(PhysicalObject obj,bool flee){
			if(HasAttr(AttrType.IMMOBILE) || (type == ActorType.MECHANICAL_KNIGHT && attrs[AttrType.COOLDOWN_1] == 2)){
				return false;
			}
			if(SlippedOrStruggled()){
				return true;
			}
			List<int> dirs = new List<int>();
			List<int> sideways_directions = new List<int>();
			AI_Step_Build_Direction_Lists(tile(),obj,flee,dirs,sideways_directions);
			List<int> partially_blocked_dirs = new List<int>();
			foreach(int i in dirs){
				if(ActorInDirection(i) != null && ActorInDirection(i).IsHiddenFrom(this)){
					player_visibility_duration = -1;
					if(ActorInDirection(i) == player){
						attrs[AttrType.PLAYER_NOTICED]++;
					}
					target = player; //not extensible yet
					target_location = M.tile[player.row,player.col];
					string walks = " walks straight into you! ";
					if(HasAttr(AttrType.FLYING)){
						walks = " flies straight into you! ";
					}
					if(!IsHiddenFrom(player)){
						B.Add(TheName(true) + walks);
						if(!HasAttr(AttrType.MINDLESS) && player.CanSee(this)){
							B.Add(the_name + " looks startled. ");
						}
					}
					else{
						attrs[AttrType.TURNS_VISIBLE] = -1;
						attrs[AttrType.NOTICED]++;
						B.Add(AName(true) + walks);
						if(!HasAttr(AttrType.MINDLESS) && player.CanSee(this)){
							B.Add(the_name + " looks just as surprised as you. ");
						}
					}
					return true;
				}
				Tile t = TileInDirection(i);
				if(t.Is(TileType.RUBBLE) && (path == null || path.Count == 0 || t != M.tile[path[0]])){ //other tiles might go here eventually
					partially_blocked_dirs.Add(i);
				}
				else{
					if(AI_WillingToMove(tile(),t,obj) && AI_MoveOrOpen(i)){
						return true;
					}
				}
			}
			foreach(int i in partially_blocked_dirs){
				if(AI_WillingToMove(tile(),TileInDirection(i),obj) && AI_MoveOrOpen(i)){
					return true;
				}
			}
			foreach(int i in sideways_directions){
				if(AI_WillingToMove(tile(),TileInDirection(i),obj) && AI_MoveOrOpen(i)){
					return true;
				}
			}
			return false;
		}
		private static void AI_Step_Build_Direction_Lists(PhysicalObject start,PhysicalObject obj,bool flee,List<int> dirs,List<int> sideways_dirs){
			int rowchange = 0;
			int colchange = 0;
			if(obj.row < start.row){
				rowchange = -1;
			}
			if(obj.row > start.row){
				rowchange = 1;
			}
			if(obj.col < start.col){
				colchange = -1;
			}
			if(obj.col > start.col){
				colchange = 1;
			}
			if(flee){
				rowchange = -rowchange;
				colchange = -colchange;
			}
			if(rowchange == -1){
				if(colchange == -1){
					dirs.Add(7);
				}
				if(colchange == 0){
					dirs.Add(8);
				}
				if(colchange == 1){
					dirs.Add(9);
				}
			}
			if(rowchange == 0){
				if(colchange == -1){
					dirs.Add(4);
				}
				if(colchange == 1){
					dirs.Add(6);
				}
			}
			if(rowchange == 1){
				if(colchange == -1){
					dirs.Add(1);
				}
				if(colchange == 0){
					dirs.Add(2);
				}
				if(colchange == 1){
					dirs.Add(3);
				}
			}
			if(dirs.Count == 0){ return; }
			bool clockwise = R.CoinFlip();
			if(obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(true))) < obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(false)))){
				clockwise = true;
			}
			else{
				if(obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(false))) < obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(true)))){
					clockwise = false;
				}
			}
			if(clockwise){
				dirs.Add(dirs[0].RotateDir(true));
				dirs.Add(dirs[0].RotateDir(false)); //building a list of directions to try: first the primary direction,
			}
			else{
				dirs.Add(dirs[0].RotateDir(false));
				dirs.Add(dirs[0].RotateDir(true));
			}
			clockwise = R.CoinFlip(); //then the ones next to it, then the ones next to THOSE(in random order, unless one is closer)
			if(obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(true,2))) < obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(false,2)))){
				clockwise = true;
			}
			else{
				if(obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(false,2))) < obj.DistanceFrom(start.TileInDirection(dirs[0].RotateDir(true,2)))){
					clockwise = false;
				}
			}
			sideways_dirs.Add(dirs[0].RotateDir(clockwise,2)); //these 2 are considered last.
			sideways_dirs.Add(dirs[0].RotateDir(!clockwise,2)); //this completes the list of 5 directions.
		}
		public bool AI_Flee(){
			if(SlippedOrStruggled()){
				return true;
			}
			if(M.safetymap == null){
				M.UpdateSafetyMap(player);
			}
			List<pos> best = PositionsWithinDistance(1).Where(x=>(M.actor[x] == null || M.actor[x] == this) && M.safetymap[x].IsValidDijkstraValue() && AI_WillingToMove(tile(),M.tile[x],null)).WhereLeast(x=>M.safetymap[x]).WhereGreatest(x=>player.DistanceFrom(x));
			if(best.Count > 0){
				pos p = best.Random();
				return AI_MoveOrOpen(p.row,p.col);
			}
			else{
				return false;
			}
		}
		private bool AI_WillingToMove(Tile start,Tile next_step,PhysicalObject final_destination){
			if(!next_step.p.BoundsCheck(M.tile,false)){
				return false;
			}
			int destination_danger = GetDangerRating(next_step); //DESTINATION: DANGER!
			int danger_threshold = (target != null? 1 : 0);
			if(destination_danger > danger_threshold && destination_danger > GetDangerRating(start)){
				return false;
			} //so, if it's actually too dangerous, the above check'll handle that. Next, let's check for unnecessary ventures into minor hazards:
			if(destination_danger > 0 && final_destination != null){
				if(!AI_CheckNextStep(next_step,final_destination,false)){
					return false;
				}
			}
			if(next_step.Is(TileType.CHASM,TileType.FIRE_RIFT) && (!HasAttr(AttrType.FLYING) || HasAttr(AttrType.DESCENDING))){
				return false;
			}
			if(HasAttr(AttrType.AVOIDS_LIGHT) && !M.wiz_dark && !M.wiz_lite){
				if(next_step.light_value > 0 && start.light_value == 0){
					if(!(type == ActorType.DARKNESS_DWELLER && HasAttr(AttrType.COOLDOWN_2))){
						return false;
					}
				}
			}
			return true;
		}
		private bool AI_CheckNextStep(Tile start,PhysicalObject obj,bool flee){
			List<int> dirs = new List<int>();
			List<int> sideways_directions = new List<int>();
			AI_Step_Build_Direction_Lists(start,obj,flee,dirs,sideways_directions);
			List<int> partially_blocked_dirs = new List<int>();
			Tile t = null;
			foreach(int i in dirs){
				t = start.TileInDirection(i);
				if(t.Is(TileType.RUBBLE)){ //other tiles might go here eventually
					partially_blocked_dirs.Add(i);
				}
				else{
					if(AI_WillingToMove(start,t,null)){
						return true;
					}
				}
			}
			foreach(int i in partially_blocked_dirs){
				if(AI_WillingToMove(start,TileInDirection(i),null)){
					return true;
				}
			}
			return false;
		}
		public bool AI_MoveOrOpen(int dir){
			return AI_MoveOrOpen(TileInDirection(dir).row,TileInDirection(dir).col);
		}
		public bool AI_MoveOrOpen(int r,int c){
			Tile t = M.tile[r,c];
			if(t.passable && M.actor[r,c] == null && !MovementPrevented(t)){
				if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.IsTrap()){
					t.SetName(Tile.Prototype(t.type).name);
					B.Add(YouVisible("smash",true) + " " + t.TheName(true) + ". ",this,t);
					t.TurnToFloor();
				}
				Move(r,c);
				if(this != player && tile().IsTrap() && tile().name == "floor" && target == null && !HasAttr(AttrType.FLYING) && player.CanSee(this) && player.CanSee(tile())){
					Event hiddencheck = null;
					foreach(Event e in Q.list){
						if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
							hiddencheck = e;
							break;
						}
					}
					if(hiddencheck != null){
						hiddencheck.area.Remove(tile());
					}
					tile().name = Tile.Prototype(tile().type).name;
					tile().a_name = Tile.Prototype(tile().type).a_name;
					tile().the_name = Tile.Prototype(tile().type).the_name;
					tile().symbol = Tile.Prototype(tile().type).symbol;
					tile().color = Tile.Prototype(tile().type).color;
					B.Add(the_name + " avoids " + tile().AName(true) + ". ");
				}
				if(tile().Is(TileType.GRAVEL) && !HasAttr(AttrType.FLYING)){
					if(player.DistanceFrom(tile()) <= 3){
						if(player.CanSee(tile())){
							B.Add("The gravel crunches. ",tile());
						}
						else{
							B.Add("You hear gravel crunching. ");
						}
					}
					MakeNoise(3);
				}
				if(type == ActorType.CYCLOPEAN_TITAN && DistanceFrom(player) <= 6){
					if(!player.HasAttr(AttrType.TITAN_MESSAGE_COOLDOWN)){
						B.Add("The ground trembles slightly. ");
					}
					player.RefreshDuration(AttrType.TITAN_MESSAGE_COOLDOWN,400);
				}
				if(HasAttr(AttrType.BURNING) && HasAttr(AttrType.HUMANOID_INTELLIGENCE) && !Is(ActorType.CULTIST,ActorType.FINAL_LEVEL_CULTIST) && !HasAttr(AttrType.IMMUNE_FIRE) && tile().IsWater() && !tile().Is(FeatureType.OIL)){
					B.Add(You("extinguish",true) + " the flames. ",this);
					attrs[AttrType.BURNING] = 0;
					if(light_radius == 0){
						UpdateRadius(1,0);
					}
					Q.KillEvents(this,AttrType.BURNING);
					Fire.burning_objects.Remove(this);
				}
				return true;
			}
			else{
				if(t.type == TileType.DOOR_C && HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
					t.Toggle(this);
					return true;
				}
				else{
					if(t.type == TileType.RUBBLE){
						if(HasAttr(AttrType.SMALL)){
							if(M.actor[r,c] == null && !MovementPrevented(t)){
								if(player.HasLOS(this)){
									B.Add(the_name + " slips through the rubble. ",this);
								}
								Move(r,c);
							}
							else{
								return false;
							}
						}
						else{
							t.Toggle(this);
							if(!HasAttr(AttrType.BRUTISH_STRENGTH)){
								IncreaseExhaustion(1);
							}
						}
						return true;
					}
					else{
						if(t.type == TileType.HIDDEN_DOOR && HasAttr(AttrType.BOSS_MONSTER)){
							t.Toggle(this);
							t.Toggle(this);
							return true;
						}
					}
				}
			}
			return false;
		}
		public bool AI_Sidestep(PhysicalObject obj){
			int dist = DistanceFrom(obj);
			List<Tile> tiles = new List<Tile>();
			for(int i=row-1;i<=row+1;++i){
				for(int j=col-1;j<=col+1;++j){
					if(M.tile[i,j].DistanceFrom(obj) == dist && M.tile[i,j].passable && M.actor[i,j] == null){
						tiles.Add(M.tile[i,j]);
					}
				}
			}
			while(tiles.Count > 0){
				int idx = R.Roll(1,tiles.Count)-1;
				if(AI_Step(tiles[idx])){
					return true;
				}
				else{
					tiles.RemoveAt(idx);
				}
			}
			return false;
		}
		public bool SlippedOrStruggled(){
			if(tile().Is(FeatureType.WEB) && type != ActorType.STALKING_WEBSTRIDER && !HasAttr(AttrType.NONEUCLIDEAN_MOVEMENT)){
				if(HasAttr(AttrType.BRUTISH_STRENGTH,AttrType.SLIMED,AttrType.OIL_COVERED)){
					tile().RemoveFeature(FeatureType.WEB);
				}
				else{
					if(R.CoinFlip()){
						if(player.HasLOS(this)){
							B.Add(You("break") + " free. ",this);
						}
						tile().RemoveFeature(FeatureType.WEB);
					}
					else{
						if(player.HasLOS(this)){
							B.Add(You("try",false,true) + " to break free. ",this);
						}
					}
					IncreaseExhaustion(3);
					return true;
				}
			}
			if(tile().IsSlippery() && !(tile().Is(TileType.ICE) && type == ActorType.FROSTLING)){
				if(R.OneIn(5) && !HasAttr(AttrType.FLYING,AttrType.NONEUCLIDEAN_MOVEMENT) && !Is(ActorType.GIANT_SLUG,ActorType.MACHINE_OF_WAR,ActorType.MUD_ELEMENTAL)){
					if(this == player){
						B.Add("You slip and almost fall! ");
					}
					else{
						if(player.HasLOS(this)){
							B.Add(the_name + " slips and almost falls! ",this);
						}
					}
					return true;
				}
			}
			return false;
		}
		public bool PathStep(){ return PathStep(false); }
		public bool PathStep(bool never_clear_path){
			if(path.Count > 0 && !HasAttr(AttrType.IMMOBILE,AttrType.NONEUCLIDEAN_MOVEMENT) && AI_WillingToMove(tile(),M.tile[path[0]],null)){
				if(DistanceFrom(path[0]) == 1 && M.actor[path[0]] != null){
					if(group != null && group[0] == this && group.Contains(M.actor[path[0]])){
						if(MovementPrevented(M.tile[path[0]]) || M.actor[path[0]].MovementPrevented(tile())){
							path.Clear();
						}
						else{
							Move(path[0].row,path[0].col); //leaders can push through their followers
							if(DistanceFrom(path[0]) == 0){
								path.RemoveAt(0);
							}
						}
					}
					else{
						if(path.Count == 1 && M.actor[path[0]] != player){
							if(!never_clear_path){
								path.Clear();
							}
						}
						else{
							if(AI_Step(M.tile[path[0]])){
								if(path.Count > 1){
									if(DistanceFrom(path[1]) > 1){
										if(!never_clear_path){
											path.Clear();
										}
									}
									else{
										if(DistanceFrom(path[1]) == 1){
											path.RemoveAt(0);
										}
										else{
											if(DistanceFrom(path[1]) == 0){
												path.RemoveAt(0);
												path.RemoveAt(0);
											}
										}
									}
								}
							}
							else{
								if(M.actor[path[0]].attrs[AttrType.TURNS_HERE] > 0 && !MovementPrevented(M.tile[path[0]]) && !M.actor[path[0]].MovementPrevented(tile())){
									M.actor[path[0]].attrs[AttrType.TURNS_HERE] = 0;
									attrs[AttrType.TURNS_HERE] = 0;
									Move(path[0].row,path[0].col); //switch places
									if(DistanceFrom(path[0]) == 0){
										path.RemoveAt(0);
									}
								}
							}
						}
					}
				}
				else{
					if(DistanceFrom(path[0]) == 1 && !M.tile[path[0]].passable && !M.tile[path[0]].IsDoorType(true)){
						if(HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
							B.Add(the_name + " looks vexed. ",this);
						}
						path.Clear();
					}
					else{
						AI_Step(M.tile[path[0]]);
						if(path.Count > 0 && DistanceFrom(path[0]) == 0){
							path.RemoveAt(0);
						}
						else{
							if(path.Count > 0 && (M.tile[path[0]].Is(TileType.CHASM,TileType.FIRE_RIFT))){
								path.Clear();
							}
							if(path.Count > 1 && DistanceFrom(path[1]) == 1){
								path.RemoveAt(0);
							}
						}
					}
				}
				QS();
				return true;
			}
			return false;
		}
		public Color BloodColor(){
			switch(type){
			case ActorType.NOXIOUS_WORM:
			case ActorType.IMPOSSIBLE_NIGHTMARE:
				return Color.DarkMagenta;
			case ActorType.CORROSIVE_OOZE:
			case ActorType.FORASECT:
			case ActorType.PHASE_SPIDER:
			case ActorType.CARRION_CRAWLER:
			case ActorType.STALKING_WEBSTRIDER:
			case ActorType.VULGAR_DEMON:
				return Color.Green;
			case ActorType.MUD_ELEMENTAL:
				//return Color.DarkYellow; //conflicts with wax walls
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.FROSTLING:
			case ActorType.SPORE_POD:
			case ActorType.POLTERGEIST:
			case ActorType.SKELETON:
			case ActorType.SHADOW:
			case ActorType.ZOMBIE:
			case ActorType.BANSHEE:
			case ActorType.CLOUD_ELEMENTAL:
			case ActorType.MECHANICAL_KNIGHT:
			case ActorType.STONE_GOLEM:
			case ActorType.LASHER_FUNGUS:
			case ActorType.VAMPIRE:
			case ActorType.LUMINOUS_AVENGER:
			case ActorType.CORPSETOWER_BEHEMOTH:
			case ActorType.GHOST:
			case ActorType.DREAM_WARRIOR_CLONE:
			case ActorType.DREAM_SPRITE_CLONE:
			case ActorType.BLOOD_MOTH:
			case ActorType.GIANT_SLUG:
			case ActorType.DREAM_SPRITE:
			case ActorType.SKITTERMOSS:
			case ActorType.SPELLMUDDLE_PIXIE:
			case ActorType.MACHINE_OF_WAR:
			case ActorType.MUD_TENTACLE:
				return Color.Black;
			default:
				if(name.Contains("phantom")){
					return Color.Black;
				}
				return Color.DarkRed;
			}
		}
		public bool Attack(int attack_idx,Actor a){ return Attack(attack_idx,a,false); }
		public bool Attack(int attack_idx,Actor a,bool attack_is_part_of_another_action){ //returns true if attack hit
			AttackInfo info = attack[type][attack_idx];
			pos original_pos = p;
			pos target_original_pos = a.p;
			if(EquippedWeapon.type != WeaponType.NO_WEAPON){
				info = EquippedWeapon.Attack();
			}
			info.damage.source = this;
			if(a.HasFeat(FeatType.DEFLECT_ATTACK) && DistanceFrom(a) == 1){
				//Actor other = a.ActorsWithinDistance(1).Where(x=>x.DistanceFrom(this) == 1).Random();
				Actor other = a.ActorsWithinDistance(1).Where(x=>x != this).Random();
				if(other != a){
					B.Add(a.You("deflect") + "! ",this,a);
					return Attack(attack_idx,other,attack_is_part_of_another_action);
				}
			}
			if(!attack_is_part_of_another_action && StunnedThisTurn()){
				return false;
			}
			if(!attack_is_part_of_another_action && exhaustion == 100 && R.CoinFlip()){
				B.Add(You("fumble") + " from exhaustion. ",this);
				Q1(); //this is checked in PlayerWalk if attack_is_part_of_another_action is true
				return false;
			}
			if(!attack_is_part_of_another_action && this == player && EquippedWeapon.status[EquipmentStatus.POSSESSED] && R.CoinFlip()){
				List<Actor> actors = ActorsWithinDistance(1);
				Actor chosen = actors.Random();
				if(chosen != a){
					if(chosen == this){
						B.Add("Your possessed " + EquippedWeapon.NameWithEnchantment() + " tries to attack you! ");
						B.Add("You fight it off! "); //this is also checked in PlayerWalk if attack_is_part_of_another_action is true
						Q1();
						return false;
					}
					else{
						return Attack(attack_idx,chosen);
					}
				}
			}
			bool player_in_combat = false;
			if(this == player || a == player){
				player_in_combat = true;
			}
			/*if(a == player && (type == ActorType.DREAM_WARRIOR_CLONE || type == ActorType.DREAM_SPRITE_CLONE)){
				player_in_combat = false;
			}*/
			if(player_in_combat){
				player.attrs[AttrType.IN_COMBAT]++;
			}
			if(a.HasAttr(AttrType.CAN_DODGE) && a.CanSee(this)){
				int dodge_dir = R.Roll(9);
				Tile dodge_tile = a.TileInDirection(dodge_dir);
				bool failed_to_dodge = false;
				if(HasAttr(AttrType.CONFUSED,AttrType.SLOWED,AttrType.STUNNED) && R.CoinFlip()){
					failed_to_dodge = true;
				}
				if(a.tile().Is(FeatureType.WEB) && !a.HasAttr(AttrType.BURNING,AttrType.OIL_COVERED,AttrType.SLIMED,AttrType.BRUTISH_STRENGTH)){
					failed_to_dodge = true;
				}
				if(!failed_to_dodge && dodge_tile.passable && dodge_tile.actor() == null && !a.MovementPrevented(dodge_tile) && !a.HasAttr(AttrType.PARALYZED)){
					B.Add(a.You("dodge") + " " + YourVisible() + " attack. ",this,a);
					if(player.CanSee(a)){
						Help.TutorialTip(TutorialTopic.Dodging);
					}
					if(a == player){
						B.DisplayNow();
						Screen.AnimateMapCell(a.row,a.col,new colorchar('!',Color.Green),80);
					}
					a.Move(dodge_tile.row,dodge_tile.col);
					if(a != player && DistanceFrom(dodge_tile) > 1){
						M.Draw();
						Thread.Sleep(40);
					}
					if(!attack_is_part_of_another_action){
						Q.Add(new Event(this,info.cost));
					}
					return false;
				}
			}
			if(a.HasFeat(FeatType.CUNNING_DODGE) && !this.HasAttr(AttrType.DODGED)){
				attrs[AttrType.DODGED]++;
				B.Add(a.You("dodge") + " " + YourVisible() + " attack. ",this,a);
				if(!attack_is_part_of_another_action){
					Q.Add(new Event(this,info.cost));
				}
				return false;
			}
			if(IsInvisibleHere() || a.IsInvisibleHere()){
				Help.TutorialTip(TutorialTopic.FightingTheUnseen);
			}
			//pos pos_of_target = new pos(a.row,a.col);
			bool a_moved_last_turn = !a.HasAttr(AttrType.TURNS_HERE);
			bool drive_back_applied = HasFeat(FeatType.DRIVE_BACK);
			if(drive_back_applied && !ConfirmsSafetyPrompts(a.tile())){
				drive_back_applied = false;
			}
			bool drive_back_nowhere_to_run = false;
			if(!attack_is_part_of_another_action && drive_back_applied){ //doesn't work while moving
				drive_back_nowhere_to_run = true;
				int dir = DirectionOf(a);
				foreach(int next_dir in new List<int>{dir,dir.RotateDir(true),dir.RotateDir(false)}){
					Tile t = a.TileInDirection(next_dir);
					if(t.passable && t.actor() == null && !a.MovementPrevented(t)){
						drive_back_nowhere_to_run = false;
						break;
					}
				}
				/*if(a.TileInDirection(dir).passable && a.ActorInDirection(dir) == null && !a.GrabPreventsMovement(TileInDirection(dir))){
					drive_back_nowhere_to_run = false;
				}
				if(a.TileInDirection(dir.RotateDir(true)).passable && a.ActorInDirection(dir.RotateDir(true)) == null && !a.GrabPreventsMovement(TileInDirection(dir.RotateDir(true)))){
					drive_back_nowhere_to_run = false;
				}
				if(a.TileInDirection(dir.RotateDir(false)).passable && a.ActorInDirection(dir.RotateDir(false)) == null && !a.GrabPreventsMovement(TileInDirection(dir.RotateDir(false)))){
					drive_back_nowhere_to_run = false;
				}*/
				if(a.tile().IsSlippery() && !(a.tile().Is(TileType.ICE) && a.type == ActorType.FROSTLING)){
					if(R.OneIn(5) && !HasAttr(AttrType.FLYING,AttrType.NONEUCLIDEAN_MOVEMENT) && !Is(ActorType.GIANT_SLUG,ActorType.MACHINE_OF_WAR,ActorType.MUD_ELEMENTAL)){
						drive_back_nowhere_to_run = true;
					}
				}
				if(a.HasAttr(AttrType.FROZEN) || a.HasAttr(AttrType.IMMOBILE)){
					drive_back_nowhere_to_run = true; //todo: exception for noneuclidean monsters? i think they'll just move out of the way.
				}
			}
			bool obscured_vision_miss = false;
			{
				bool fog = false;
				bool hidden = false;
				if((this.tile().Is(FeatureType.FOG,FeatureType.THICK_DUST) || a.tile().Is(FeatureType.FOG,FeatureType.THICK_DUST))){
					fog = true;
				}
				if(a.IsHiddenFrom(this) || !CanSee(a) || (a.IsInvisibleHere() && !HasAttr(AttrType.BLINDSIGHT))){
					hidden = true;
				}
				if(!HasAttr(AttrType.DETECTING_MONSTERS) && (fog || hidden) && R.CoinFlip()){
					obscured_vision_miss = true;
				}
			}
			int plus_to_hit = TotalSkill(SkillType.COMBAT);
			bool sneak_attack = false;
			if(this.IsHiddenFrom(a) || !a.CanSee(this) || (this == player && IsInvisibleHere() && !a.HasAttr(AttrType.BLINDSIGHT))){
				sneak_attack = true;
				a.attrs[AttrType.SEES_ADJACENT_PLAYER] = 1;
				if(DistanceFrom(a) > 2 && this != player){
					sneak_attack = false; //no phantom blade sneak attacks from outside your view - but the player can sneak attack at this range with a wand of reach.
				}
			} //...insert any other changes to sneak attack calculation here...
			if(sneak_attack || HasAttr(AttrType.LUNGING_AUTO_HIT) || (EquippedWeapon == Dagger && !tile().IsLit()) || (EquippedWeapon == Staff && a_moved_last_turn) || a.HasAttr(AttrType.SWITCHING_ARMOR)){ //some attacks get +25% accuracy. this usually totals 100% vs. unarmored targets.
				plus_to_hit += 25;
			}
			plus_to_hit -= a.TotalSkill(SkillType.DEFENSE) * 3;
			bool attack_roll_hit = a.IsHit(plus_to_hit);
			bool blocked_by_armor_miss = false;
			bool blocked_by_root_shell_miss = false;
			bool mace_through_armor = false;
			if(!attack_roll_hit){
				int armor_value = a.TotalProtectionFromArmor();
				if(a != player){
					armor_value = a.TotalSkill(SkillType.DEFENSE); //if monsters have Defense skill, it's from armor
				}
				int roll = R.Roll(25 - plus_to_hit);
				if(roll <= armor_value * 3){
					bool mace = (EquippedWeapon == Mace || type == ActorType.CRUSADING_KNIGHT || type == ActorType.PHANTOM_CRUSADER);
					if(mace){
						attack_roll_hit = true;
						mace_through_armor = true;
					}
					else{
						if(type == ActorType.CORROSIVE_OOZE || type == ActorType.LASHER_FUNGUS){ //this is a bit hacky, but these are the only ones that aren't stopped by armor right now.
							attack_roll_hit = true;
						}
						else{
							blocked_by_armor_miss = true;
						}
					}
				}
				else{
					if(a.HasAttr(AttrType.ROOTS) && roll <= (armor_value + 10) * 3){ //potion of roots gives 10 defense
						blocked_by_root_shell_miss = true;
					}
				}
			}
			bool hit = true;
			if(obscured_vision_miss){ //this calculation turned out to be pretty complicated
				hit = false;
			}
			else{
				if(blocked_by_armor_miss || blocked_by_root_shell_miss){
					hit = false;
				}
				else{
					if(drive_back_nowhere_to_run || attack_roll_hit){
						hit = true;
					}
					else{
						hit = false;
					}
				}
			}
			if(a.HasAttr(AttrType.GRABBED) && attrs[AttrType.GRABBING] == DirectionOf(a)){
				hit = true; //one more modifier: automatically hit things you're grabbing.
			}
			bool weapon_just_poisoned = false;
			if(!hit){
				if(blocked_by_armor_miss){
					bool initial_message_printed = false; //for better pronoun usage
					if(info.blocked != ""){
						initial_message_printed = true;
						string s = info.blocked + ". ";
						int pos = -1;
						do{
							pos = s.IndexOf('&');
							if(pos != -1){
								s = s.Substring(0,pos) + TheName(true) + s.Substring(pos+1);
							}
						}
						while(pos != -1);
						//
						do{
							pos = s.IndexOf('*');
							if(pos != -1){
								s = s.Substring(0,pos) + a.TheName(true) + s.Substring(pos+1);
							}
						}
						while(pos != -1);
						B.Add(s,this,a);
					}
					if(a.HasFeat(FeatType.ARMOR_MASTERY) && !(a.type == ActorType.ALASI_SCOUT && attack_idx == 1)){
						B.Add(a.YourVisible() + " armor blocks the attack, leaving " + TheName(true) + " off-balance. ",a,this);
						RefreshDuration(AttrType.SUSCEPTIBLE_TO_CRITS,100);
					}
					else{
						if(initial_message_printed){
							B.Add(a.YourVisible() + " armor blocks the attack. ",this,a);
						}
						else{
							B.Add(a.YourVisible() + " armor blocks " + YourVisible() + " attack. ",this,a);
						}
					}
					if(a.EquippedArmor.type == ArmorType.FULL_PLATE && !HasAttr(AttrType.BRUTISH_STRENGTH)){
						a.IncreaseExhaustion(3);
						Help.TutorialTip(TutorialTopic.HeavyPlateArmor);
					}
				}
				else{
					if(blocked_by_root_shell_miss){
						B.Add(a.YourVisible() + " root shell blocks " + YourVisible() + " attack. ",this,a);
					}
					else{
						if(obscured_vision_miss){
							B.Add(Your() + " attack goes wide. ",this);
						}
						else{
							if(!attack_is_part_of_another_action && drive_back_applied && !MovementPrevented(M.tile[target_original_pos])){
								B.Add(You("drive") + " " + a.TheName(true) + " back. ",this,a);
								/*if(!a.HasAttr(AttrType.FROZEN) && !HasAttr(AttrType.FROZEN)){
									a.AI_Step(this,true);
									AI_Step(a);
								}*/
								Tile dest = null;
								int dir = DirectionOf(target_original_pos);
								foreach(int next_dir in new List<int>{dir,dir.RotateDir(true),dir.RotateDir(false)}){
									Tile t = a.TileInDirection(next_dir);
									if(t.passable && t.actor() == null && !a.MovementPrevented(t)){
										dest = t;
										break;
									}
								}
								if(dest != null){
									a.AI_MoveOrOpen(dest.row,dest.col);
									if(M.actor[target_original_pos] == null){
										AI_MoveOrOpen(target_original_pos.row,target_original_pos.col);
									}
								}
							}
							else{
								if(info.miss != ""){
									string s = info.miss + ". ";
									int pos = -1;
									do{
										pos = s.IndexOf('&');
										if(pos != -1){
											s = s.Substring(0,pos) + TheName(true) + s.Substring(pos+1);
										}
									}
									while(pos != -1);
									//
									do{
										pos = s.IndexOf('*');
										if(pos != -1){
											s = s.Substring(0,pos) + a.TheName(true) + s.Substring(pos+1);
										}
									}
									while(pos != -1);
									B.Add(s,this,a);
								}
								else{
									B.Add(YouVisible("miss",true) + " " + a.TheName(true) + ". ",this,a);
								}
							}
						}
					}
				}
				if(type == ActorType.SWORDSMAN || type == ActorType.PHANTOM_SWORDMASTER || type == ActorType.ALASI_SOLDIER){
					attrs[AttrType.COMBO_ATTACK] = 0;
				}
			}
			else{
				string s = info.hit + ". ";
				if(!attack_is_part_of_another_action && HasFeat(FeatType.NECK_SNAP) && a.HasAttr(AttrType.MEDIUM_HUMANOID) && (IsHiddenFrom(a) || a.IsHelpless())){
					if(!a.HasAttr(AttrType.RESIST_NECK_SNAP)){
						B.Add(You("silently snap") + " " + a.Your() + " neck. ");
						a.Kill();
						Q1();
						return true;
					}
					else{
						B.Add(You("silently snap") + " " + a.Your() + " neck. ");
						B.Add("It doesn't seem to affect " + a.the_name + ". ");
					}
				}
				bool crit = false;
				int crit_chance = 8; //base crit rate is 1/8
				if(EquippedWeapon.type == WeaponType.DAGGER && !tile().IsLit()){
					crit_chance /= 2;
				}
				if(a.EquippedArmor != null && (a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] || a.EquippedArmor.status[EquipmentStatus.DAMAGED] || a.HasAttr(AttrType.SWITCHING_ARMOR))){
					crit_chance /= 2;
				}
				if(a.HasAttr(AttrType.SUSCEPTIBLE_TO_CRITS)){ //caused by armor mastery
					crit_chance /= 2;
				}
				if(EquippedWeapon.enchantment == EnchantmentType.PRECISION && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
					crit_chance /= 2;
				}
				if(drive_back_nowhere_to_run){
					crit_chance /= 2;
				}
				if(crit_chance <= 1 || R.OneIn(crit_chance)){
					crit = true;
				}
				int pos = -1;
				do{
					pos = s.IndexOf('&');
					if(pos != -1){
						s = s.Substring(0,pos) + TheName(true) + s.Substring(pos+1);
					}
				}
				while(pos != -1);
				//
				do{
					pos = s.IndexOf('*');
					if(pos != -1){
						s = s.Substring(0,pos) + a.TheName(true) + s.Substring(pos+1);
					}
				}
				while(pos != -1);
				int dice = info.damage.dice;
				if(sneak_attack && crit && this == player){
					if(!a.HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE,AttrType.BOSS_MONSTER) && a.type != ActorType.CYCLOPEAN_TITAN){
						switch(EquippedWeapon.type){ //todo: should this check for shielded/blocking?
						case WeaponType.SWORD:
							B.Add("You run " + a.TheName(true) + " through! ");
							break;
						case WeaponType.MACE:
							B.Add("You bash " + a.YourVisible() + " head in! ");
							break;
						case WeaponType.DAGGER:
							B.Add("You pierce one of " + a.YourVisible() + " vital organs! ");
							break;
						case WeaponType.STAFF:
							B.Add("You bring your staff down on " + a.YourVisible() + " head with a loud crack! ");
							break;
						case WeaponType.BOW:
							B.Add("You choke " + a.TheName(true) + " with your bowstring! ");
							break;
						default:
							break;
						}
						Help.TutorialTip(TutorialTopic.InstantKills);
						MakeNoise(6);
						if(a.type == ActorType.BERSERKER && a.target == this){
							a.attrs[AttrType.SHIELDED] = 0;
							a.TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,a.curhp,this);
						}
						else{
							a.Kill();
						}
						if(!attack_is_part_of_another_action){
							Q1();
						}
						return true;
					}
				}
				if(sneak_attack && (this == player || a == player)){
					B.Add(YouVisible("strike") + " from hiding! ");
					if(type != ActorType.PLAYER){
						if(a == player && attrs[AttrType.TURNS_VISIBLE] >= 0){
							B.PrintAll();
						}
						attrs[AttrType.TURNS_VISIBLE] = -1;
						attrs[AttrType.NOTICED] = 1;
						attrs[AttrType.DANGER_SENSED] = 1;
					}
					else{
						a.player_visibility_duration = -1;
						a.attrs[AttrType.PLAYER_NOTICED] = 1;
					}
				}
				if(a == player){
					if(a.HasAttr(AttrType.SWITCHING_ARMOR)){
						B.Add("You're unguarded! ");
					}
					else{
						if(a.EquippedArmor.status[EquipmentStatus.DAMAGED]){
							B.Add("Your damaged armor leaves you open! ");
						}
						else{
							if(crit && R.CoinFlip()){
								if(a.EquippedArmor.status[EquipmentStatus.WEAK_POINT]){
									B.Add(TheName(true) + " finds a weak point. ");
								}
							}
						}
					}
				}
				if(mace_through_armor){
					if(type == ActorType.CRUSADING_KNIGHT || type == ActorType.PHANTOM_CRUSADER){
						B.Add(YourVisible() + " huge mace punches through " + a.YourVisible() + " armor. ",this,a);
					}
					else{
						B.Add(YourVisible() + " mace punches through " + a.YourVisible() + " armor. ",this,a);
					}
				}
				else{
					B.Add(s,this,a);
				}
				if(crit && info.crit != CriticalEffect.NO_CRIT){
					if(this == player || a == player){
						Help.TutorialTip(TutorialTopic.CriticalHits);
					}
				}
				if(a == player && !player.CanSee(this)){
					Screen.AnimateMapCell(row,col,new colorchar('?',Color.DarkGray),50);
				}
				if(a.type == ActorType.GHOST && EquippedWeapon.enchantment != EnchantmentType.NO_ENCHANTMENT && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
					EquippedWeapon.status[EquipmentStatus.NEGATED] = true;
					B.Add(Your() + " " + EquippedWeapon.NameWithEnchantment() + "'s magic is suppressed! ",this);
					Help.TutorialTip(TutorialTopic.Negated);
				}
				if(!Help.displayed[TutorialTopic.SwitchingEquipment] && this == player && a.Is(ActorType.SPORE_POD,ActorType.SKELETON,ActorType.STONE_GOLEM,ActorType.MECHANICAL_KNIGHT,ActorType.MACHINE_OF_WAR) && EquippedWeapon.type == WeaponType.SWORD){
					Help.TutorialTip(TutorialTopic.SwitchingEquipment);
				}
				int dmg = R.Roll(dice,6);
				bool no_max_damage_message = false;
				List<CriticalEffect> effects = new List<CriticalEffect>(); //critical effects actually include all on-hit things
				if(crit && info.crit != CriticalEffect.NO_CRIT){
					effects.AddUnique(info.crit);
				}
				/*if(HasAttr(AttrType.DIM_VISION_HIT)){
					effects.AddUnique(CriticalEffect.DIM_VISION);
				}*/
				if(type == ActorType.DEMON_LORD && DistanceFrom(a) == 2){
					effects.AddUnique(CriticalEffect.PULL);
				}
				if(HasAttr(AttrType.GRAB_HIT)){
					effects.AddUnique(CriticalEffect.GRAB);
				}
				if(HasAttr(AttrType.LIFE_DRAIN_HIT)){
					effects.AddUnique(CriticalEffect.DRAIN_LIFE);
				}
				if(HasAttr(AttrType.PARALYSIS_HIT)){
					effects.AddUnique(CriticalEffect.PARALYZE);
				}
				if(HasAttr(AttrType.STALAGMITE_HIT)){
					effects.AddUnique(CriticalEffect.STALAGMITES);
				}
				if(HasAttr(AttrType.STUN_HIT)){
					effects.AddUnique(CriticalEffect.STUN);
				}
				if(HasAttr(AttrType.WORN_OUT_HIT)){
					effects.AddUnique(CriticalEffect.WORN_OUT);
				}
				if(HasAttr(AttrType.ACID_HIT)){
					effects.AddUnique(CriticalEffect.ACID);
				}
				if(HasAttr(AttrType.ONE_HP_HIT)){
					effects.AddUnique(CriticalEffect.ONE_HP);
				}
				if(HasAttr(AttrType.BLEED_HIT)){
					effects.AddUnique(CriticalEffect.BLEED);
				}
				if(HasAttr(AttrType.SILENCE_HIT)){
					effects.AddUnique(CriticalEffect.SILENCE);
				}
				if(HasAttr(AttrType.EXHAUST_HIT)){
					effects.AddUnique(CriticalEffect.EXHAUST);
				}
				if(type == ActorType.SWORDSMAN && attrs[AttrType.COMBO_ATTACK] == 2){
					effects.AddUnique(CriticalEffect.BLEED);
					effects.AddUnique(CriticalEffect.STRONG_KNOCKBACK);
				}
				if(type == ActorType.PHANTOM_SWORDMASTER && attrs[AttrType.COMBO_ATTACK] == 2){
					effects.AddUnique(CriticalEffect.PERCENT_DAMAGE);
					effects.AddUnique(CriticalEffect.STRONG_KNOCKBACK);
				}
				if(type == ActorType.ALASI_SOLDIER){
					if(attrs[AttrType.COMBO_ATTACK] == 1){
						effects.AddUnique(CriticalEffect.ONE_TURN_STUN);
					}
					else{
						if(attrs[AttrType.COMBO_ATTACK] == 2){
							effects.AddUnique(CriticalEffect.ONE_TURN_PARALYZE);
						}
					}
				}
				if(type == ActorType.WILD_BOAR && HasAttr(AttrType.COOLDOWN_1)){
					effects.AddUnique(CriticalEffect.FLING);
				}
				if(type == ActorType.ALASI_SENTINEL && R.OneIn(3)){
					effects.AddUnique(CriticalEffect.FLING);
				}
				if(this == player && a.type == ActorType.CYCLOPEAN_TITAN && crit){
					effects = new List<CriticalEffect>(); //remove all other effects (so far) and check for edged weapons
					if(EquippedWeapon == Sword || EquippedWeapon == Dagger){
						effects.Add(CriticalEffect.PERMANENT_BLIND);
					}
				}
				if(HasAttr(AttrType.POISON_HIT) || EquippedWeapon.status[EquipmentStatus.POISONED]){
					effects.AddUnique(CriticalEffect.POISON);
				}
				if(HasAttr(AttrType.BRUTISH_STRENGTH)){
					effects.AddUnique(CriticalEffect.MAX_DAMAGE);
					effects.AddUnique(CriticalEffect.STRONG_KNOCKBACK);
					effects.Remove(CriticalEffect.KNOCKBACK); //strong knockback replaces these
					effects.Remove(CriticalEffect.TRIP);
					effects.Remove(CriticalEffect.FLING);
				}
				if(EquippedWeapon != null && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
					switch(EquippedWeapon.enchantment){
					case EnchantmentType.CHILLING:
						effects.AddUnique(CriticalEffect.CHILL);
						break;
					case EnchantmentType.DISRUPTION:
						effects.AddUnique(CriticalEffect.DISRUPTION); //not entirely sure that these should be crit effects
						break;
					case EnchantmentType.VICTORY:
						if(a.maxhp > 1){ // no illusions, phantoms, or minions
							effects.AddUnique(CriticalEffect.VICTORY);
						}
						break;
					}
				}
				if(type == ActorType.SKITTERMOSS && HasAttr(AttrType.COOLDOWN_1)){
					effects.Remove(CriticalEffect.INFEST);
				}
				if(a.HasAttr(AttrType.NONLIVING)){
					effects.Remove(CriticalEffect.DRAIN_LIFE);
				}
				if(type == ActorType.CARRION_CRAWLER && attack_idx == 1){
					effects.Remove(CriticalEffect.PARALYZE);
				}
				if(type == ActorType.GIANT_SLUG && attack_idx == 0){
					effects.Remove(CriticalEffect.ACID);
				}
				foreach(CriticalEffect effect in effects){ //pre-damage effects - these can alter the amount of damage.
					switch(effect){
					case CriticalEffect.MAX_DAMAGE:
						dmg = Math.Max(dmg,dice * 6);
						break;
					case CriticalEffect.PERCENT_DAMAGE:
						dmg = Math.Max(dmg,(a.maxhp+1)/2);
						no_max_damage_message = true;
						if(!EquippedWeapon.status[EquipmentStatus.DULLED]){
							if(this == player){
								B.Add("Your sword cuts deep! ");
							}
							else{
								B.Add(Your() + " attack cuts deep! ",this);
							}
						}
						break;
					case CriticalEffect.ONE_HP:
						dmg = a.curhp - 1;
						if(a.HasAttr(AttrType.VULNERABLE)){
							a.attrs[AttrType.VULNERABLE] = 0;
						}
						if(a == player){
							B.Add("You shudder. ");
						}
						no_max_damage_message = true;
						break;
					}
				}
				if(dice < 2){
					no_max_damage_message = true;
				}
				if(a.type == ActorType.SPORE_POD && EquippedWeapon.IsBlunt()){
					no_max_damage_message = true;
				}
				if(EquippedWeapon.status[EquipmentStatus.MERCIFUL]){
					no_max_damage_message = true;
				}
				if(a.HasAttr(AttrType.RESIST_WEAPONS) && EquippedWeapon.type != WeaponType.NO_WEAPON){
					B.Add("Your " + EquippedWeapon.NameWithoutEnchantment() + " isn't very effective. ");
					dmg = dice; //minimum damage
				}
				else{
					if(EquippedWeapon.status[EquipmentStatus.DULLED]){
						B.Add("Your dull " + EquippedWeapon.NameWithoutEnchantment() + " isn't very effective. ");
						dmg = dice; //minimum damage
					}
					else{
						if(type == ActorType.MUD_TENTACLE){ //getting surrounded by these guys should be dangerous, but not simply because of their damage.
							dmg = dice;
						}
					}
				}
				if(dmg >= dice * 6 && !no_max_damage_message){
					if(this == player){
						B.Add("It was a good hit! ");
					}
					else{
						if(a == player){
							B.Add("Ow! ");
						}
					}
				}
				dmg += TotalSkill(SkillType.COMBAT);
				if(a.type == ActorType.SPORE_POD && EquippedWeapon.IsBlunt()){
					dmg = 0;
					dice = 0;
					effects.AddUnique(CriticalEffect.STRONG_KNOCKBACK);
					B.Add("Your " + EquippedWeapon.NameWithoutEnchantment() + " knocks the spore pod away. ",a);
				}
				if(EquippedWeapon.status[EquipmentStatus.MERCIFUL] && dmg >= a.curhp){
					dmg = a.curhp - 1;
					B.Add("Your " + EquippedWeapon.NameWithoutEnchantment() + " refuses to finish " + a.TheName(true) + ". ");
					B.Print(true);
				}
				if(a.HasAttr(AttrType.DULLS_BLADES) && R.CoinFlip() && (EquippedWeapon == Sword || EquippedWeapon == Dagger)){
					EquippedWeapon.status[EquipmentStatus.DULLED] = true;
					B.Add(Your() + " " + EquippedWeapon.NameWithEnchantment() + " becomes dull! ",this);
					Help.TutorialTip(TutorialTopic.Dulled);
				}
				if(a.type == ActorType.CORROSIVE_OOZE && R.CoinFlip() && (EquippedWeapon == Sword || EquippedWeapon == Dagger || EquippedWeapon == Mace)){
					EquippedWeapon.status[EquipmentStatus.DULLED] = true;
					B.Add("The acid dulls " + Your() + " " + EquippedWeapon.NameWithEnchantment() + "! ",this);
					Help.TutorialTip(TutorialTopic.Acidified);
					Help.TutorialTip(TutorialTopic.Dulled);
				}
				if(a.HasAttr(AttrType.CAN_POISON_WEAPONS) && R.CoinFlip() && EquippedWeapon.type != WeaponType.NO_WEAPON && !EquippedWeapon.status[EquipmentStatus.POISONED]){
					EquippedWeapon.status[EquipmentStatus.POISONED] = true;
					weapon_just_poisoned = true;
					B.Add(Your() + " " + EquippedWeapon.NameWithEnchantment() + " is covered in poison! ",this);
				}
				int r = a.row;
				int c = a.col;
				bool still_alive = true;
				bool knockback_effect = effects.Contains(CriticalEffect.KNOCKBACK) || effects.Contains(CriticalEffect.STRONG_KNOCKBACK) || effects.Contains(CriticalEffect.TRIP) || effects.Contains(CriticalEffect.FLING) || effects.Contains(CriticalEffect.SWAP_POSITIONS) || effects.Contains(CriticalEffect.DRAIN_LIFE);
				if(knockback_effect){
					a.attrs[AttrType.TURN_INTO_CORPSE]++;
				}
				Color blood = a.BloodColor();
				bool homunculus = a.type == ActorType.HOMUNCULUS;
				if(dmg > 0){
					Damage damage = new Damage(info.damage.type,info.damage.damclass,true,this,dmg);
					damage.weapon_used = EquippedWeapon.type;
					still_alive = a.TakeDamage(damage,a_name);
				}
				if(homunculus){ //todo: or will this happen on any major damage, not just melee attacks?
					M.tile[target_original_pos].AddFeature(FeatureType.OIL);
				}
				else{
					if(blood != Color.Black && (!still_alive || !a.HasAttr(AttrType.FROZEN,AttrType.INVULNERABLE))){
						/*List<Tile> valid = new List<Tile>{M.tile[target_original_pos]};
						for(int i=-1;i<=1;++i){
							valid.Add(M.tile[target_original_pos].TileInDirection(original_pos.DirectionOf(target_original_pos).RotateDir(true,i)));
						}
						for(int i=dmg/10;i>0;--i){
							Tile t = valid.RemoveRandom();
							if(t.Is(TileType.WALL) || t.name == "floor"){
								t.color = blood;
							}
						}*/
						List<Tile> cone = M.tile[target_original_pos].GetCone(original_pos.DirectionOf(target_original_pos),dmg>=20? 2 : 1,false);
						cone.Add(M.tile[target_original_pos].TileInDirection(original_pos.DirectionOf(target_original_pos)));
						cone.Add(M.tile[target_original_pos].TileInDirection(original_pos.DirectionOf(target_original_pos)));
						cone.Add(M.tile[target_original_pos].TileInDirection(original_pos.DirectionOf(target_original_pos)));
						for(int i=(dmg-5)/5;i>0;--i){
							if(cone.Count == 0){
								break;
							}
							Tile t = cone.Random();
							while(cone.Remove(t)){ } //remove all
							if(t.Is(TileType.WALL) || t.name == "floor"){
								t.color = blood;
							}
						}
					}
				}
				if(still_alive){ //post-damage crit effects that require the target to still be alive
					foreach(CriticalEffect effect in effects){
						if(still_alive){
							switch(effect){ //todo: some of these messages shouldn't be printed if the effect already exists
							case CriticalEffect.CONFUSE:
								a.ApplyStatus(AttrType.CONFUSED,R.Between(2,3)*100);
								break;
							case CriticalEffect.BLEED:
								if(!a.HasAttr(AttrType.NONLIVING,AttrType.FROZEN)){
									if(a.HasAttr(AttrType.BLEEDING)){
										if(a == player){
											if(a.attrs[AttrType.BLEEDING] > 15){
												B.Add("Your bleeding worsens. ");
											}
											else{
												B.Add("You're bleeding badly now! ");
											}
										}
										else{
											B.Add(a.YouAre() + " bleeding badly! ",a);
										}
									}
									a.attrs[AttrType.BLEEDING] += R.Between(10,15);
									if(a.attrs[AttrType.BLEEDING] > 25){
										a.attrs[AttrType.BLEEDING] = 25; //this seems like a reasonable cap, so repeated bleed effects don't just last *forever*.
									}
									if(a == player){
										Help.TutorialTip(TutorialTopic.Bleeding);
									}
								}
								break;
							case CriticalEffect.BLIND:
								a.ApplyStatus(AttrType.BLIND,R.Between(5,7)*100);
								//B.Add(a.YouAre() + " blinded! ",a);
								//a.RefreshDuration(AttrType.BLIND,R.Between(5,7)*100);
								break;
							case CriticalEffect.PERMANENT_BLIND:
							{
								if(!a.HasAttr(AttrType.COOLDOWN_1)){
									B.Add("You drive your " + EquippedWeapon.NameWithoutEnchantment() + " into its eye, blinding it! ");
									Q.KillEvents(a,AttrType.BLIND);
									a.attrs[AttrType.BLIND] = 1;
									a.attrs[AttrType.COOLDOWN_1] = 1;
								}
								break;
							}
							case CriticalEffect.DIM_VISION:
								if(a.ResistedBySpirit()){
									B.Add(a.Your() + " vision is dimmed, but only for a moment. ",a);
								}
								else{
									B.Add(a.Your() + " vision is dimmed. ",a);
									a.RefreshDuration(AttrType.DIM_VISION,(R.Roll(2,20)+20)*100);
								}
								break;
							case CriticalEffect.CHILL:
								if(!a.HasAttr(AttrType.IMMUNE_COLD)){
									B.Add(a.the_name + " is chilled. ",a);
									if(!a.HasAttr(AttrType.CHILLED)){
										a.attrs[AttrType.CHILLED] = 1;
									}
									else{
										a.attrs[AttrType.CHILLED] *= 2;
									}
									if(!a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,a.attrs[AttrType.CHILLED],this)){
										still_alive = false;
									}
								}
								break;
							case CriticalEffect.DISRUPTION:
								if(a.HasAttr(AttrType.NONLIVING)){
									B.Add(a.the_name + " is disrupted. ",a);
									if(!a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,a.maxhp / 5,this)){
										still_alive = false;
									}
								}
								break;
							case CriticalEffect.FREEZE:
								a.tile().ApplyEffect(DamageType.COLD);
								a.ApplyFreezing();
								break;
							case CriticalEffect.GRAB:
								if(!HasAttr(AttrType.GRABBING) && DistanceFrom(a) == 1 && !a.HasAttr(AttrType.FROZEN)){
									a.attrs[AttrType.GRABBED]++;
									attrs[AttrType.GRABBING] = DirectionOf(a);
									B.Add(YouVisible("grab") + " " + a.TheName(true) + ". ",this,a);
									if(a == player){
										Help.TutorialTip(TutorialTopic.Grabbed);
									}
								}
								break;
							case CriticalEffect.POISON:
								if(!a.HasAttr(AttrType.NONLIVING,AttrType.CAN_POISON_WEAPONS,AttrType.INVULNERABLE,AttrType.FROZEN)){
									a.ApplyStatus(AttrType.POISONED,(R.Roll(2,6)+2)*100);
								}
								break;
							case CriticalEffect.PARALYZE:
								if(!a.HasAttr(AttrType.NONLIVING) || type != ActorType.CARRION_CRAWLER){
									if(a.ResistedBySpirit()){
										B.Add(a.Your() + " muscles stiffen, but only for a moment. ",a);
									}
									else{
										if(a == player){
											B.Add("You suddenly can't move! ");
										}
										else{
											B.Add(a.YouAre() + " paralyzed. ",a);
										}
										a.attrs[AttrType.PARALYZED] = R.Between(3,5);
									}
								}
								break;
							case CriticalEffect.ONE_TURN_PARALYZE:
								Event e = Q.FindAttrEvent(a,AttrType.STUNNED);
								if(e != null && e.delay == 100 && e.TimeToExecute() == Q.turn){ //if the target was hit with a 1-turn stun that's about to expire, don't print a message for it.
									e.msg = "";
								}
								if(a.ResistedBySpirit()){
									B.Add(a.Your() + " muscles stiffen, but only for a moment. ",a);
								}
								else{
									B.Add(a.YouAre() + " paralyzed! ",a);
									a.attrs[AttrType.PARALYZED] = 2; //setting it to 1 means it would end immediately
								}
								break;
							case CriticalEffect.INFLICT_VULNERABILITY:
								a.ApplyStatus(AttrType.VULNERABLE,R.Between(2,4)*100);
								/*B.Add(a.You("become") + " vulnerable. ",a);
								a.RefreshDuration(AttrType.VULNERABLE,R.Between(2,4)*100);*/
								if(a == player){
									Help.TutorialTip(TutorialTopic.Vulnerable);
								}
								break;
							case CriticalEffect.IGNITE:
								break;
							case CriticalEffect.INFEST:
								if(a == player && !a.EquippedArmor.status[EquipmentStatus.INFESTED]){
									B.Add("Thousands of insects crawl into your " + a.EquippedArmor.NameWithoutEnchantment() + "! ");
									a.EquippedArmor.status[EquipmentStatus.INFESTED] = true;
									Help.TutorialTip(TutorialTopic.Infested);
								}
								break;
							case CriticalEffect.SLOW:
								a.ApplyStatus(AttrType.SLOWED,R.Between(4,6)*100);
								break;
							case CriticalEffect.REDUCE_ACCURACY: //also about 2d4 turns?
								break;
							case CriticalEffect.SLIME:
								B.Add(a.YouAre() + " covered in slime. ",a);
								a.attrs[AttrType.SLIMED] = 1;
								if(a == player){
									Help.TutorialTip(TutorialTopic.Slimed);
								}
								break;
							case CriticalEffect.STUN: //2d3 turns, at most
							{
								a.ApplyStatus(AttrType.STUNNED,R.Roll(2,3)*100);
								/*B.Add(a.YouAre() + " stunned! ",a);
								a.RefreshDuration(AttrType.STUNNED,a.DurationOfMagicalEffect(R.Roll(2,3)) * 100,a.YouAre() + " no longer stunned. ",a);*/
								if(a == player){
									Help.TutorialTip(TutorialTopic.Stunned);
								}
								break;
							}
							case CriticalEffect.ONE_TURN_STUN:
							{
								a.ApplyStatus(AttrType.STUNNED,100);
								/*B.Add(a.YouAre() + " stunned! ",a);
								a.RefreshDuration(AttrType.STUNNED,100,a.YouAre() + " no longer stunned. ",a);*/
								if(a == player){
									Help.TutorialTip(TutorialTopic.Stunned);
								}
								break;
							}
							case CriticalEffect.SILENCE:
							{
								if(a.ResistedBySpirit()){
									if(!HasAttr(AttrType.SILENCED)){
										B.Add(a.You("resist") + " being silenced. ",a);
									}
								}
								else{
									if(!HasAttr(AttrType.SILENCED)){
										B.Add(TheName(true) + " silences " + a.the_name + ". ",a);
									}
									a.RefreshDuration(AttrType.SILENCED,R.Between(3,4)*100,a.YouAre() + " no longer silenced. ",a);
								}
								if(a == player){
									Help.TutorialTip(TutorialTopic.Silenced);
								}
								break;
							}
							case CriticalEffect.WEAK_POINT:
								if(!a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] && a == player){
									a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] = true;
									B.Add(YouVisible("expose") + " a weak point on your armor! ",this);
									Help.TutorialTip(TutorialTopic.WeakPoint);
								}
								break;
							case CriticalEffect.WORN_OUT:
								if(a == player && !a.EquippedArmor.status[EquipmentStatus.DAMAGED]){
									if(a.EquippedArmor.status[EquipmentStatus.WORN_OUT]){
										a.EquippedArmor.status[EquipmentStatus.WORN_OUT] = false;
										a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] = false;
										a.EquippedArmor.status[EquipmentStatus.DAMAGED] = true;
										B.Add(a.Your() + " " + a.EquippedArmor.NameWithEnchantment() + " is damaged! ");
										Help.TutorialTip(TutorialTopic.Damaged);
									}
									else{
										a.EquippedArmor.status[EquipmentStatus.WORN_OUT] = true;
										B.Add(a.Your() + " " + a.EquippedArmor.NameWithEnchantment() + " looks worn out. ");
										Help.TutorialTip(TutorialTopic.WornOut);
									}
								}
								break;
							case CriticalEffect.ACID:
								if(a == player && !a.HasAttr(AttrType.ACIDIFIED) && R.CoinFlip()){
									a.RefreshDuration(AttrType.ACIDIFIED,300);
									if(a.EquippedArmor != a.Leather && !a.EquippedArmor.status[EquipmentStatus.DAMAGED]){
										B.Add("The acid hisses as it touches your " + a.EquippedArmor.NameWithEnchantment() + ". ");
										if(a.EquippedArmor.status[EquipmentStatus.WORN_OUT]){
											a.EquippedArmor.status[EquipmentStatus.WORN_OUT] = false;
											a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] = false;
											a.EquippedArmor.status[EquipmentStatus.DAMAGED] = true;
											B.Add(a.Your() + " " + a.EquippedArmor.NameWithEnchantment() + " is damaged! ");
										}
										else{
											a.EquippedArmor.status[EquipmentStatus.WORN_OUT] = true;
											B.Add(a.Your() + " " + a.EquippedArmor.NameWithEnchantment() + " looks worn out. ");
										}
										Help.TutorialTip(TutorialTopic.Acidified);
									}
								}
								break;
							case CriticalEffect.PULL:
							{
								List<Tile> tiles = tile().NeighborsBetween(a.row,a.col).Where(x=>x.actor() == null && x.passable);
								if(tiles.Count > 0){
									Tile t = tiles.Random();
									if(!a.MovementPrevented(t)){
										B.Add(TheName(true) + " pulls " + a.TheName(true) + " closer. ",this,a);
										a.Move(t.row,t.col);
									}
								}
								break;
							}
							case CriticalEffect.STEAL:
							{
								if(a.inv != null && a.inv.Count > 0){
									Item i = a.inv.Random();
									Item stolen = i;
									if(i.quantity > 1){
										stolen = new Item(i,i.row,i.col);
										stolen.revealed_by_light = i.revealed_by_light;
										i.quantity--;
									}
									else{
										a.inv.Remove(stolen);
									}
									GetItem(stolen);
									B.Add(YouVisible("steal") + " " + a.YourVisible() + " " + stolen.SingularName() + "! ",this,a);
									B.PrintAll();
								}
								break;
							}
							case CriticalEffect.EXHAUST:
							{
								if(a == player){
									B.Add("You feel fatigued. ");
								}
								a.IncreaseExhaustion(R.Roll(2,4));
								break;
							}
							}
						}
					}
				}
				foreach(CriticalEffect effect in effects){ //effects that don't care whether the target is still alive
					switch(effect){
					case CriticalEffect.DRAIN_LIFE:
					{
						if(curhp < maxhp){
							curhp += 10;
							if(curhp > maxhp){
								curhp = maxhp;
							}
							B.Add(You("drain") + " some life from " + a.TheName(true) + ". ",this);
						}
						break;
					}
					case CriticalEffect.VICTORY:
						if(!still_alive){
							curhp += 5;
							if(curhp > maxhp){
								curhp = maxhp;
							}
						}
						break;
					case CriticalEffect.STALAGMITES:
					{
						List<Tile> tiles = new List<Tile>();
						foreach(Tile t in M.tile[r,c].TilesWithinDistance(1)){
							//if(t.actor() == null && (t.type == TileType.FLOOR || t.type == TileType.STALAGMITE)){
							if(t.actor() == null && t.inv == null && (t.IsTrap() || t.Is(TileType.FLOOR,TileType.GRAVE_DIRT,TileType.GRAVEL,TileType.STALAGMITE))){
								if(R.CoinFlip()){
									tiles.Add(t);
								}
							}
						}
						foreach(Tile t in tiles){
							if(t.type == TileType.STALAGMITE){
								Q.KillEvents(t,EventType.STALAGMITE);
							}
							else{
								TileType previous_type = t.type;
								t.Toggle(this,TileType.STALAGMITE);
								t.toggles_into = previous_type;
							}
						}
						Q.Add(new Event(tiles,150,EventType.STALAGMITE));
						break;
					}
					case CriticalEffect.MAKE_NOISE:
						break;
					case CriticalEffect.SWAP_POSITIONS:
						if(original_pos.DistanceFrom(target_original_pos) == 1 && p.Equals(original_pos) && M.actor[target_original_pos] != null && !M.actor[target_original_pos].HasAttr(AttrType.IMMOBILE)){
							B.Add(YouVisible("move") + " past " + M.actor[target_original_pos].TheName(true) + ". ",this,M.actor[target_original_pos]);
							Move(target_original_pos.row,target_original_pos.col);
						}
						break;
					case CriticalEffect.TRIP:
						if(!a.HasAttr(AttrType.FLYING) && (a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK))){
							B.Add(YouVisible("trip") + " " + a.TheName(true) + ". ",this,a);
							a.IncreaseExhaustion(R.Between(2,4));
							a.CollideWith(a.tile());//todo: if it's a corpse, ONLY trip it if something is going to happen when it collides with the floor.
						}
						break;
					case CriticalEffect.KNOCKBACK:
						if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							KnockObjectBack(a,3,this);
						}
						break;
					case CriticalEffect.STRONG_KNOCKBACK:
						if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							KnockObjectBack(a,5,this);
						}
						break;
					case CriticalEffect.FLING:
						if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							attrs[AttrType.JUST_FLUNG] = 1;
							int dir = DirectionOf(a).RotateDir(true,4);
							Tile t = null;
							if(tile().p.PosInDir(dir).PosInDir(dir).BoundsCheck(M.tile,true)){
								Tile t2 = tile().TileInDirection(dir).TileInDirection(dir);
								if(HasLOE(t2)){
									t = t2;
								}
							}
							if(t == null){
								if(tile().p.PosInDir(dir).BoundsCheck(M.tile,false)){
									Tile t2 = tile().TileInDirection(dir);
									if(HasLOE(t2)){
										t = t2;
									}
								}
							}
							if(t == null){
								t = tile();
							}
							B.Add(YouVisible("fling") + " " + a.TheName(true) + "! ",this,a);
							foreach(Tile nearby in M.ReachableTilesByDistance(t.row,t.col,false)){
								if(nearby.passable && nearby.actor() == null && HasLOE(nearby)){
									a.Move(nearby.row,nearby.col);
									a.CollideWith(nearby);
									break;
								}
							}
						}
						break;
					}
				}
				if(knockback_effect){
					if(a.curhp > 0 && this != player){
						target_location = target.tile();
					}
					a.CorpseCleanup();
				}
				if(type == ActorType.SWORDSMAN || type == ActorType.PHANTOM_SWORDMASTER || type == ActorType.ALASI_SOLDIER){
					if(attrs[AttrType.COMBO_ATTACK] == 1 && (type == ActorType.SWORDSMAN || type == ActorType.PHANTOM_SWORDMASTER)){
						B.Add(the_name + " prepares a devastating strike! ",this);
					}
					attrs[AttrType.COMBO_ATTACK]++;
					if(attrs[AttrType.COMBO_ATTACK] == 3){ //all these have 3-part combos
						attrs[AttrType.COMBO_ATTACK] = 0;
					}
				}
			}
			/*if(!hit && HasAttr(AttrType.BRUTISH_STRENGTH) && p.Equals(original_pos) && M.actor[target_original_pos] != null){
				Actor a2 = M.actor[target_original_pos];
				if(a2.HasAttr(AttrType.NO_CORPSE_KNOCKBACK) && a2.maxhp == 1){
					B.Add(YouVisible("push",true) + " " + a2.TheName(true) + ". ",this,a2);
					a2.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,false,1,this);
				}
				else{
					a2.attrs[AttrType.TURN_INTO_CORPSE]++;
					KnockObjectBack(a2,5);
					a2.CorpseCleanup();
				}
			}*/
			if(!hit && sneak_attack && this != player){
				attrs[AttrType.TURNS_VISIBLE] = -1;
				attrs[AttrType.NOTICED]++;
			}
			if(!attack_is_part_of_another_action && hit && HasAttr(AttrType.BRUTISH_STRENGTH) && p.Equals(original_pos) && M.actor[target_original_pos] == null && DistanceFrom(target_original_pos) == 1 && !MovementPrevented(M.tile[target_original_pos])){
				Tile t = M.tile[target_original_pos];
				if(t.IsTrap()){
					t.SetName(Tile.Prototype(t.type).name);
					t.TurnToFloor();
				}
				if(HasFeat(FeatType.WHIRLWIND_STYLE)){
					WhirlwindMove(t.row,t.col);
				}
				else{
					Move(t.row,t.col);
				}
			}
			if(hit && EquippedWeapon.enchantment == EnchantmentType.ECHOES && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
				List<Tile> line = GetBestExtendedLineOfEffect(target_original_pos.row,target_original_pos.col);
				int idx = line.IndexOf(M.tile[target_original_pos]);
				if(idx != -1 && line.Count > idx + 1){
					Actor next = line[idx+1].actor();
					if(next != null && next != this){
						Attack(attack_idx,next,true);
					}
				}
			}
			//if(!attack_is_part_of_another_action && EquippedWeapon == Staff && p.Equals(original_pos) && a_moved_last_turn && !HasAttr(AttrType.IMMOBILE) && M.tile[target_original_pos].passable && (M.actor[target_original_pos] == null || !M.actor[target_original_pos].HasAttr(AttrType.IMMOBILE))){
			if(!attack_is_part_of_another_action && EquippedWeapon == Staff && p.Equals(original_pos) && a_moved_last_turn && !MovementPrevented(M.tile[target_original_pos]) && M.tile[target_original_pos].passable && (M.actor[target_original_pos] == null || !M.actor[target_original_pos].MovementPrevented(this))){
				if(M.actor[target_original_pos] != null){
					M.actor[target_original_pos].attrs[AttrType.TURNS_HERE]++; //this is a hack to prevent fast monsters from swapping *back* on the next hit.
				}
				if(HasFeat(FeatType.WHIRLWIND_STYLE)){
					WhirlwindMove(target_original_pos.row,target_original_pos.col,true,new List<Actor>{M.actor[target_original_pos]}); //whirlwind move, but don't attack the original target again
				}
				else{
					Move(target_original_pos.row,target_original_pos.col);
				}
			}
			if(!attack_is_part_of_another_action && EquippedWeapon.status[EquipmentStatus.POISONED] && !weapon_just_poisoned && R.OneIn(16)){
				ApplyStatus(AttrType.POISONED,(R.Roll(2,6)+2)*100,"You manage to poison yourself with your " + EquippedWeapon.NameWithoutEnchantment() + ". ","","You resist the poison dripping from your " + EquippedWeapon.NameWithoutEnchantment() + ". ");
			}
			if(!attack_is_part_of_another_action && EquippedWeapon.status[EquipmentStatus.HEAVY] && R.CoinFlip() && !HasAttr(AttrType.BRUTISH_STRENGTH)){
				B.Add("Attacking with your heavy " + EquippedWeapon.NameWithoutEnchantment() + " exhausts you. ");
				IncreaseExhaustion(5);
			}
			MakeNoise(6);
			if(!attack_is_part_of_another_action){
				Q.Add(new Event(this,info.cost));
			}
			return hit;
		}
		public void FireArrow(PhysicalObject obj){ FireArrow(GetBestExtendedLineOfEffect(obj),false); }
		public void FireArrow(List<Tile> line){ FireArrow(line,false); }
		public void FireArrow(List<Tile> line,bool free_attack){
			if(!free_attack && StunnedThisTurn()){
				return;
			}
			int mod = -30; //bows have base accuracy 45%
			/*if(magic_trinkets.Contains(MagicTrinketType.RING_OF_KEEN_SIGHT)){
				mod = -15; //keen sight now only affects trap detection
			}*/
			if(this == player && Bow.status[EquipmentStatus.ONE_ARROW_LEFT]){
				mod = 25; //...but the last arrow gets a bonus
			}
			mod += TotalSkill(SkillType.COMBAT);
			Tile t = null;
			Actor a = null;
			bool solid_object_hit = false;
			bool no_terrain_collision_message = free_attack; //don't show "the arrow hits the wall" for echoes.
			List<string> misses = new List<string>();
			List<Actor> missed = new List<Actor>();
			List<Tile> animation_line = new List<Tile>(line);
			line.RemoveAt(0); //remove the source of the arrow first
			if(line.Count > 12){
				line = line.GetRange(0,Math.Min(12,line.Count));
			}
			int flaming_arrow_start = HasAttr(AttrType.FIERY_ARROWS)? 0 : -1; //tracks where an arrow caught fire
			bool blocked_by_armor_miss = false;
			bool blocked_by_root_shell_miss = false;
			for(int i=0;i<line.Count;++i){
				a = line[i].actor();
				t = line[i];
				if(a != null){
					no_terrain_collision_message = true;
					int plus_to_hit = mod - a.TotalSkill(SkillType.DEFENSE)*3;
					bool hit = true;
					if(!a.IsHit(plus_to_hit)){
						hit = false;
						int armor_value = a.TotalProtectionFromArmor();
						if(a != player){
							armor_value = a.TotalSkill(SkillType.DEFENSE); //if monsters have Defense skill, it's from armor
						}
						int roll = R.Roll(55 - plus_to_hit);
						if(roll <= armor_value * 3){
							blocked_by_armor_miss = true;
						}
						else{
							if(a.HasAttr(AttrType.ROOTS) && roll <= (armor_value + 10) * 3){ //potion of roots gives 10 defense
								blocked_by_root_shell_miss = true;
							}
						}
					}
					else{
						if(a.HasAttr(AttrType.TUMBLING)){
							a.attrs[AttrType.TUMBLING] = 0;
							hit = false;
						}
					}
					if(R.CoinFlip() && !CanSee(a)){ //extra 50% miss chance for enemies you can't see
						hit = false;
						blocked_by_armor_miss = false;
						blocked_by_root_shell_miss = false;
					}
					if(hit || blocked_by_armor_miss || blocked_by_root_shell_miss){
						solid_object_hit = true;
						break;
					}
					else{
						misses.Add("The arrow misses " + a.the_name + ". ");
						missed.Add(a);
					}
					a = null;
				}
				if(!t.passable){
					a = null;
					solid_object_hit = true;
					break;
				}
				if(flaming_arrow_start == -1 && t.IsBurning()){
					flaming_arrow_start = i;
				}
			}
			if(!free_attack){
				if(Bow != null && Bow.status[EquipmentStatus.ONE_ARROW_LEFT]){
					B.Add(You("take") + " careful aim. ",this);
					B.Add(You("fire") + " " + Your() + " last arrow. ",this);
				}
				else{
					if(HasAttr(AttrType.FIERY_ARROWS)){
						B.Add(You("fire") + " a flaming arrow. ",this);
					}
					else{
						B.Add(You("fire") + " an arrow. ",this);
					}
				}
				B.DisplayNow();
			}
			int idx = 0;
			/*foreach(Tile tile2 in animation_line){
				if(tile2.seen){
					++idx; //todo: remove idx here, right?
				}
				else{
					animation_line = animation_line.To(tile2);
					if(animation_line.Count > 0){
						animation_line.RemoveAt(animation_line.Count - 1);
					}
					break;
				}
			}*/
			if(animation_line.Count > 0){
				Screen.CursorVisible = false;
				PhysicalObject o = t;
				if(a != null){
					o = a;
				}
				animation_line = animation_line.To(o);
				if(flaming_arrow_start >= 0 && !M.wiz_dark && !player.HasAttr(AttrType.BLIND)){ //fire should be visible unless darkened or blind
					List<Tile> first_line = animation_line.ToCount(flaming_arrow_start+2);
					List<Tile> second_line = animation_line.FromCount(flaming_arrow_start+2);
					if(first_line.Count > 0){
						Screen.AnimateBoltProjectile(animation_line.ToCount(flaming_arrow_start+2),Color.DarkYellow,20);
					}
					Screen.CursorVisible = false;
					if(second_line.Count > 0){
						Screen.AnimateBoltProjectile(animation_line.FromCount(flaming_arrow_start+2),Color.RandomFire,20);
					}
				}
				else{
					Screen.AnimateBoltProjectile(animation_line,Color.DarkYellow,20);
				}
				Screen.CursorVisible = false;
				if(this == player && solid_object_hit && !player.CanSee(o) && (o is Actor || !o.tile().seen)){
					Screen.AnimateMapCell(o.row,o.col,new colorchar('?',Color.DarkGray),50);
				}
				Screen.CursorVisible = false;
			}
			idx = 0;
			foreach(string s in misses){
				B.Add(s,missed[idx]);
				if(missed[idx] != player){
					missed[idx].player_visibility_duration = -1;
					if(HasLOE(missed[idx])){
						missed[idx].target = this;
						missed[idx].target_location = tile();
					}
				}
				++idx;
			}
			if(flaming_arrow_start != -1){
				foreach(Tile affected in line.FromCount(flaming_arrow_start+2)){
					affected.ApplyEffect(DamageType.FIRE);
					if((a != null && affected.actor() == a) || affected == t){
						break;
					}
				}
			}
			if(a != null){
				pos target_original_position = a.p;
				if(a.HasAttr(AttrType.IMMUNE_ARROWS)){
					B.Add("The arrow sticks out ineffectively from " + a.the_name + ". ",a);
				}
				else{
					if(a.magic_trinkets.Contains(MagicTrinketType.BRACERS_OF_ARROW_DEFLECTION)){
						B.Add(a.You("deflect") + " the arrow! ",a);
					}
					else{
						if(blocked_by_armor_miss){
							B.Add(a.YourVisible() + " armor blocks the arrow. ",a);
						}
						else{
							if(blocked_by_root_shell_miss){
								B.Add(a.YourVisible() + " root shell blocks the arrow. ",a);
							}
							else{
								bool alive = true;
								bool crit = false;
								int crit_chance = 8; //base crit rate is 1/8
								if(a.EquippedArmor != null && (a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] || a.EquippedArmor.status[EquipmentStatus.DAMAGED] || a.HasAttr(AttrType.SWITCHING_ARMOR))){
									crit_chance /= 2;
								}
								if(a.HasAttr(AttrType.SUSCEPTIBLE_TO_CRITS)){
									crit_chance /= 2;
								}
								if(Bow != null && Bow.enchantment == EnchantmentType.PRECISION && !Bow.status[EquipmentStatus.NEGATED]){
									crit_chance /= 2;
								}
								if(crit_chance <= 1 || R.OneIn(crit_chance)){
									crit = true;
								}
								if(this == player && IsHiddenFrom(a) && crit && !a.HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE,AttrType.BOSS_MONSTER) && a.type != ActorType.CYCLOPEAN_TITAN){ //none of the bow-wielding monsters should ever be hidden from the player
									B.Add(a.the_name + " falls with your arrow between the eyes. ",a);
									//B.Add("Headshot! ",a);
									a.Kill();
									alive = false;
								}
								else{
									B.Add("The arrow hits " + a.the_name + ". ",a);
									if(!a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(2,6)+TotalSkill(SkillType.COMBAT),this,a_name + "'s arrow")){
										alive = false;
									}
									if(crit && alive){
										if(a.type == ActorType.CYCLOPEAN_TITAN){
											if(!a.HasAttr(AttrType.COOLDOWN_1)){
												B.Add(YourVisible() + " arrow pierces its eye, blinding it! ",this,a);
												Q.KillEvents(a,AttrType.BLIND);
												a.attrs[AttrType.BLIND] = 1;
												a.attrs[AttrType.COOLDOWN_1] = 1;
											}
										}
										else{
											Event e = Q.FindAttrEvent(a,AttrType.IMMOBILE);
											if(!a.HasAttr(AttrType.IMMOBILE) || (e != null && e.msg.Contains("no longer pinned"))){ //i.e. don't pin naturally immobile monsters //todo - new refreshduration implementation should allow this to be done more naturally
												B.Add(a.YouAre() + " pinned! ",a);
												a.RefreshDuration(AttrType.IMMOBILE,100,a.YouAre() + " no longer pinned. ",a);
												if(a.HasAttr(AttrType.FLYING) && a.tile().IsTrap()){
													a.tile().TriggerTrap();
												}
											}
										}
									}
									if(alive && a.HasAttr(AttrType.NONLIVING)){
										if(Bow != null && Bow.enchantment == EnchantmentType.DISRUPTION && !Bow.status[EquipmentStatus.NEGATED]){
											B.Add(a.YouAre() + " disrupted! ",a);
											if(!a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,a.maxhp / 5,this)){
												alive = false;
											}
										}
									}
									if(alive && !a.HasAttr(AttrType.IMMUNE_COLD)){
										if(Bow != null && Bow.enchantment == EnchantmentType.CHILLING && !Bow.status[EquipmentStatus.NEGATED]){
											B.Add(a.YouAre() + " chilled. ",a);
											if(!a.HasAttr(AttrType.CHILLED)){
												a.attrs[AttrType.CHILLED] = 1;
											}
											else{
												a.attrs[AttrType.CHILLED] *= 2;
											}
											if(!a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,a.attrs[AttrType.CHILLED],this)){
												alive = false;
											}
										}
									}
									if(alive && flaming_arrow_start != -1){
										a.ApplyBurning();
									}
								}
								if(!alive && Bow != null && Bow.enchantment == EnchantmentType.VICTORY && !Bow.status[EquipmentStatus.NEGATED]){
									curhp += 5;
									if(curhp > maxhp){
										curhp = maxhp;
									}
								}
							}
						}
					}
				}
				if(Bow != null && Bow.enchantment == EnchantmentType.ECHOES && !Bow.status[EquipmentStatus.NEGATED]){
					List<Tile> line2 = line.From(M.tile[target_original_position]);
					if(line2.Count > 1){
						FireArrow(line2,true); //todo: does this need special handling? should a burning monster cause a flaming echo?
					}
				}
			}
			else{
				if(!no_terrain_collision_message || t.Is(TileType.POISON_BULB) || (t.Is(TileType.WAX_WALL) && flaming_arrow_start != -1)){
					B.Add("The arrow hits " + t.the_name + ". ",t);
					if(t.Is(TileType.POISON_BULB)){
						t.Bump(DirectionOf(t));
					}
					if(flaming_arrow_start != -1){
						t.ApplyEffect(DamageType.FIRE);
					}
				}
			}
			if(!free_attack){
				Q1();
			}
		}
		public bool IsHit(int plus_to_hit){
			if(R.Roll(1,100) + plus_to_hit <= 25){ //base hit chance is 75%
				return false;
			}
			return true;
		}
		public void CorpseCleanup(){
			if(HasAttr(AttrType.CORPSE)){
				attrs[AttrType.CORPSE]--;
				if(!HasAttr(AttrType.CORPSE)){ //when the last is removed... ( todo: not sure this part actually works in every case - what if it has both CORPSE and TURN_INTO_CORPSE?)
					Kill();
				}
			}
			else{
				if(HasAttr(AttrType.TURN_INTO_CORPSE)){
					attrs[AttrType.TURN_INTO_CORPSE]--;
				}
			}
			/*if(HasAttr(AttrType.TURN_INTO_CORPSE)){
				attrs[AttrType.TURN_INTO_CORPSE]--;
			}
			else{
				if(HasAttr(AttrType.CORPSE)){
					attrs[AttrType.CORPSE]--;
					if(!HasAttr(AttrType.CORPSE)){ //when the last is removed...
						Kill();
					}
				}
			}*/
		}
		public bool Kill(){ return TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9999,null); }
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,int dmg,Actor source){
			return TakeDamage(new Damage(dmgtype,damclass,true,source,dmg),"");
		}
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,bool major_damage,int dmg,Actor source){
			return TakeDamage(new Damage(dmgtype,damclass,major_damage,source,dmg),"");
		}
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,int dmg,Actor source,string cause_of_death){
			return TakeDamage(new Damage(dmgtype,damclass,true,source,dmg),cause_of_death);
		}
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,bool major_damage,int dmg,Actor source,string cause_of_death){
			return TakeDamage(new Damage(dmgtype,damclass,major_damage,source,dmg),cause_of_death);
		}
		public bool TakeDamage(Damage dmg,string cause_of_death){ //returns true if still alive
			if(dmg.amount == 0){
				return true;
			}
			bool damage_dealt = false;
			int old_hp = curhp;
			if(curhp <= 0 && dmg.amount < 1000){ //then we're dealing with a corpse, and they don't take normal amounts of damage
				return true;
			}
			bool ice_removed = false;
			if(dmg.amount < 1000){
				if(HasAttr(AttrType.FROZEN) && (dmg.major_damage || dmg.type == DamageType.FIRE)){ //this should ignore bleeding and poison, but not searing
					attrs[AttrType.FROZEN] -= dmg.amount;
					if(attrs[AttrType.FROZEN] <= 0){
						attrs[AttrType.FROZEN] = 0;
						B.Add("The ice breaks! ",this);
						ice_removed = true;
					}
					dmg.amount = 0;
					if(dmg.type == DamageType.FIRE && HasAttr(AttrType.FROZEN)){
						attrs[AttrType.FROZEN] = 0;
						B.Add("The ice melts! ",this);
						ice_removed = true;
					}
				}
				if(dmg.type == DamageType.FIRE && HasAttr(AttrType.OIL_COVERED)){
					if(HasAttr(AttrType.IMMUNE_BURNING)){
						B.Add("The oil burns off of " + the_name + ". ",this);
						attrs[AttrType.OIL_COVERED] = 0;
					}
					else{
						B.Add(You("catch",true) + " fire! ",this);
						ApplyBurning();
					}
				}
				if(dmg.type == DamageType.COLD && HasAttr(AttrType.SLIMED)){
					attrs[AttrType.SLIMED] = 0;
					B.Add("The slime freezes and falls from " + the_name + ". ",this);
				}
				if(!dmg.major_damage && HasAttr(AttrType.MINOR_IMMUNITY,AttrType.SHIELDED)){
					return true;
				}
				if(HasAttr(AttrType.MECHANICAL_SHIELD)){
					B.Add(Your() + " shield moves to protect it from harm. ",this);
					return true;
				}
				if(dmg.major_damage){
					if(HasAttr(AttrType.BLOCKING)){
						B.Add(You("block") + "! ",this); //todo: extra effects will go here eventually
						attrs[AttrType.BLOCKING]--;
						return true;
					}
					else{
						if(HasAttr(AttrType.SHIELDED)){
							B.Add(Your() + " shield flashes! ",this);
							attrs[AttrType.SHIELDED]--;
							return true;
						}
						else{
							if(HasAttr(AttrType.VULNERABLE)){
								attrs[AttrType.VULNERABLE] = 0;
								if(this == player){
									B.Add("Ouch! ");
								}
								else{
									B.Add(YouAre() + " devastated! ",this);
								}
								foreach(Event e in Q.list){
									if(!e.dead && e.target == this && e.type == EventType.REMOVE_ATTR && e.attr == AttrType.VULNERABLE){
										e.dead = true;
									}
								}
								dmg.amount += R.Roll(3,6);
							}
						}
					}
				}
				if(HasAttr(AttrType.INVULNERABLE)){
					dmg.amount = 0;
				}
				/*if(HasAttr(AttrType.TOUGH) && dmg.damclass == DamageClass.PHYSICAL){
					dmg.amount -= 2;
				}
				if(HasAttr(AttrType.ARCANE_SHIELDED)){
					if(attrs[AttrType.ARCANE_SHIELDED] >= dmg.amount){
						attrs[AttrType.ARCANE_SHIELDED] -= dmg.amount;
						dmg.amount = 0;
					}
					else{
						dmg.amount -= attrs[AttrType.ARCANE_SHIELDED];
						attrs[AttrType.ARCANE_SHIELDED] = 0;
					}
					if(!HasAttr(AttrType.ARCANE_SHIELDED)){
						B.Add(Your() + " shield fades. ",this);
					}
				}
				if(dmg.damclass == DamageClass.MAGICAL){
					dmg.amount -= TotalSkill(SkillType.SPIRIT) / 2;
				}
				if(HasAttr(AttrType.DAMAGE_REDUCTION) && dmg.amount > 5){
					dmg.amount = 5;
				}*/
				if(dmg.amount > 15 && magic_trinkets.Contains(MagicTrinketType.BELT_OF_WARDING)){
					dmg.amount = 15;
					B.Add(Your() + " " + MagicTrinket.Name(MagicTrinketType.BELT_OF_WARDING) + " softens the blow. ",this);
				}
				dmg.amount -= attrs[AttrType.DAMAGE_RESISTANCE];
				switch(dmg.type){
				case DamageType.NORMAL:
					if(dmg.amount > 0){
						curhp -= dmg.amount;
						damage_dealt = true;
					}
					else{
						if(!ice_removed){
							B.Add(YouAre() + " undamaged. ",this);
						}
					}
					break;
				case DamageType.MAGIC:
					if(dmg.amount > 0){
						curhp -= dmg.amount;
						damage_dealt = true;
					}
					else{
						if(!ice_removed){
							B.Add(YouAre() + " unharmed. ",this);
						}
					}
					break;
				case DamageType.FIRE:
				{
					if(HasAttr(AttrType.IMMUNE_FIRE)){
						dmg.amount = 0;
						//B.Add(the_name + " is immune! ",this);
						if(HasFeat(FeatType.BOILING_BLOOD) && attrs[AttrType.BLOOD_BOILED] == 5){
							RefreshDuration(AttrType.IMMUNE_FIRE,1000);
							RefreshDuration(AttrType.BLOOD_BOILED,1000,"Your blood cools. ");
						}
					}
					if(dmg.amount > 0){
						curhp -= dmg.amount;
						damage_dealt = true;
					}
					else{
						if((this == player || dmg.amount > 1) && !ice_removed && !HasAttr(AttrType.IMMUNE_FIRE) && !HasAttr(AttrType.JUST_SEARED)){
							B.Add(YouAre() + " unburnt. ",this);
						}
					}
					break;
				}
				case DamageType.COLD:
				{
					if(HasAttr(AttrType.IMMUNE_COLD)){
						dmg.amount = 0;
						//B.Add(YouAre() + " unharmed. ",this);
					}
					if(dmg.amount > 0){
						curhp -= dmg.amount;
						damage_dealt = true;
						if(type == ActorType.GIANT_SLUG){
							B.Add("The cold leaves " + the_name + " vulnerable. ",this);
							RefreshDuration(AttrType.VULNERABLE,R.Between(7,13)*100,the_name + " is no longer vulnerable. ",this);
						}
					}
					else{
						if(!ice_removed && !HasAttr(AttrType.IMMUNE_COLD)){
							B.Add(YouAre() + " unharmed. ",this);
						}
					}
					break;
				}
				case DamageType.ELECTRIC:
				{
					if(HasAttr(AttrType.IMMUNE_ELECTRICITY)){
						dmg.amount = 0;
					}
					if(dmg.amount > 0){
						curhp -= dmg.amount;
						damage_dealt = true;
					}
					else{
						if(!ice_removed && !HasAttr(AttrType.IMMUNE_ELECTRICITY)){
							B.Add(YouAre() + " unharmed. ",this);
						}
					}
					break;
				}
				case DamageType.POISON:
					if(HasAttr(AttrType.NONLIVING)){
						dmg.amount = 0;
					}
					if(dmg.amount > 0){
						curhp -= dmg.amount;
						damage_dealt = true;
						if(type == ActorType.PLAYER){
							B.Add("The poison burns! ");
						}
						else{
							if(R.Roll(1,5) == 5 && !HasAttr(AttrType.PLANTLIKE)){ //hmm
								B.Add(the_name + " shudders. ",this);
							}
						}
					}
					break;
				case DamageType.NONE:
					break;
				}
			}
			else{
				if(curhp > 0){
					curhp = 0;
				}
			}
			/*if(dmg.source != null && dmg.source == player && dmg.damclass == DamageClass.PHYSICAL && resisted && !cause_of_death.Contains("arrow")){
				Help.TutorialTip(TutorialTopic.Resistance);
			}*/
			if(damage_dealt){
				Interrupt();
				attrs[AttrType.AMNESIA_STUN] = 0;
				if(dmg.major_damage){
					recover_time = Q.turn + 500;
					attrs[AttrType.BANDAGED] = 0;
				}
				if(HasAttr(AttrType.ASLEEP)){
					attrs[AttrType.ASLEEP] = 0;
					attrs[AttrType.JUST_AWOKE] = 1;
					if(this == player){
						Global.FlushInput();
						B.Add("You wake up. ");
					}
				}
				if(dmg.source != null){
					if(type != ActorType.PLAYER && dmg.source != this && !HasAttr(AttrType.CONFUSED)){
						target = dmg.source;
						target_location = M.tile[dmg.source.row,dmg.source.col];
						if(dmg.source.IsHiddenFrom(this)){
							player_visibility_duration = -1;
						}
						if(type == ActorType.CAVERN_HAG && dmg.source == player){
							attrs[AttrType.COOLDOWN_2] = 1;
						}
						if(type == ActorType.CRUSADING_KNIGHT && dmg.source == player && !HasAttr(AttrType.COOLDOWN_1) && !M.wiz_lite && !CanSee(player) && curhp > 0){
							List<string> verb = new List<string>{"Show yourself","Reveal yourself","Unfold thyself","Present yourself","Unveil yourself","Make yourself known"};
							List<string> adjective = new List<string>{"despicable","filthy","foul","nefarious","vulgar","sorry","unworthy"};
							List<string> noun = new List<string>{"villain","blackguard","devil","scoundrel","wretch","cur","rogue"};
							//B.Add(TheName(true) + " shouts \"" + verb.Random() + ", " + adjective.Random() + " " + noun.Random() + "!\" ");
							B.Add("\"" + verb.Random() + ", " + adjective.Random() + " " + noun.Random() + "!\" ");
							B.Add(the_name + " raises a gauntlet. ",this);
							B.Add("Sunlight fills the dungeon. ");
							M.wiz_lite = true;
							M.wiz_dark = false;
							Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
							M.Draw();
							B.Print(true);
							attrs[AttrType.COOLDOWN_1]++;
							foreach(Actor a in M.AllActors()){
								if(a != this && a != player && !a.HasAttr(AttrType.BLINDSIGHT) && HasLOS(a)){
									a.ApplyStatus(AttrType.BLIND,R.Between(5,9)*100);
									/*B.Add(a.YouAre() + " blinded! ",a);
									a.RefreshDuration(AttrType.BLIND,R.Between(5,9)*100,a.YouAre() + " no longer blinded. ",a);*/
								}
							}
							if(!player.HasAttr(AttrType.BLINDSIGHT) && HasLOS(player)){ //do the player last, so all the previous messages can be seen.
								player.ApplyStatus(AttrType.BLIND,R.Between(5,9)*100);
								/*B.Add(player.YouAre() + " blinded! ");
								player.RefreshDuration(AttrType.BLIND,R.Between(5,9)*100,player.YouAre() + " no longer blinded. ");*/
							}
						}
						/*if(HasAttr(AttrType.RADIANT_HALO) && !M.wiz_dark && DistanceFrom(dmg.source) <= LightRadius() && HasLOS(dmg.source)){
							B.Add(YourVisible() + " radiant halo burns " + dmg.source.TheName(true) + ". ",this,dmg.source);
							int amount = R.Roll(2,6);
							if(amount >= dmg.source.curhp){
								amount = dmg.source.curhp - 1;
							}
							if(dmg.source.curhp > 1){ //this should prevent infinite loops if one haloed entity attacks another
								dmg.source.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,amount,this);
							}
						}*/
						if(dmg.source == player && IsFinalLevelDemon() && attrs[AttrType.COOLDOWN_2] < 2){
							attrs[AttrType.COOLDOWN_2]++;
						}
					}
				}
				if(HasAttr(AttrType.SPORE_BURST)){
					if(type == ActorType.SPORE_POD){
						curhp = 0;
						if(player.CanSee(this)){
							B.Add("The spore pod bursts! ",this);
						}
						else{
							if(DistanceFrom(player) == 1){
								B.Add(YouVisible("burst") + "! ");
							}
						}
						List<Tile> area = tile().AddGaseousFeature(FeatureType.SPORES,18);
						Event.RemoveGas(area,600,FeatureType.SPORES,12);
					}
					else{
						if(!HasAttr(AttrType.COOLDOWN_1) && dmg.major_damage){
							RefreshDuration(AttrType.COOLDOWN_1,50); //cooldown added mostly to prevent several triggers while surrounded by fire. todo: this cooldown is no longer necessary now that searing is minor damage.
							B.Add(You("retaliate") + " with a burst of spores! ",this);
							List<Tile> area = tile().AddGaseousFeature(FeatureType.SPORES,8);
							Event.RemoveGas(area,600,FeatureType.SPORES,12);
						}
					}
				}
				if(HasFeat(FeatType.BOILING_BLOOD) && dmg.type != DamageType.POISON){
					if(attrs[AttrType.BLOOD_BOILED] < 5){
						B.Add("Your blood boils! ");
						if(attrs[AttrType.BLOOD_BOILED] == 4){
							RefreshDuration(AttrType.IMMUNE_FIRE,1000);
						}
						GainAttrRefreshDuration(AttrType.BLOOD_BOILED,1000,"Your blood cools. ");
						if(this == player){
							Help.TutorialTip(TutorialTopic.IncreasedSpeed);
						}
					}
					else{
						RefreshDuration(AttrType.IMMUNE_FIRE,1000);
						RefreshDuration(AttrType.BLOOD_BOILED,1000,"Your blood cools. ");
					}
				}
				if(type == ActorType.DREAM_SPRITE && (dmg.source != null || (HasLOE(player) && DistanceFrom(player) <= 12))){
					attrs[AttrType.COOLDOWN_2] = 1;
				}
				if(type == ActorType.MECHANICAL_KNIGHT){
					if(old_hp == 5){
						curhp = 0;
					}
					else{
						if(old_hp == 10){
							curhp = 5;
							switch(R.Roll(3)){
							case 1:
								B.Add(Your() + " arms are destroyed! ",this);
								attrs[AttrType.COOLDOWN_1] = 1;
								attrs[AttrType.MECHANICAL_SHIELD] = 0;
								break;
							case 2:
								B.Add(Your() + " legs are destroyed! ",this);
								attrs[AttrType.COOLDOWN_1] = 2;
								path.Clear();
								target_location = null;
								break;
							case 3:
								B.Add(Your() + " head is destroyed! ",this);
								attrs[AttrType.COOLDOWN_1] = 3;
								break;
							}
						}
					}
				}
				if(dmg.type == DamageType.FIRE && (type == ActorType.TROLL || type == ActorType.TROLL_BLOODWITCH)){
					attrs[AttrType.PERMANENT_DAMAGE] += dmg.amount; //permanent damage doesn't regenerate
				}
				if(dmg.type == DamageType.FIRE && type == ActorType.SKITTERMOSS && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					B.Add("The fire kills " + Your() + " insects. ",this);
					color = Color.White;
				}
				if(type == ActorType.ALASI_SCOUT && old_hp == maxhp){
					B.Add("The glow leaves " + Your() + " sword. ",this);
					attrs[AttrType.KEEPS_DISTANCE] = 0;
				}
				/*if(type == ActorType.LUMINOUS_AVENGER && light_radius != curhp / 4){ //no longer changes its light radius based on remaining hp
					int old = LightRadius();
					light_radius = curhp / 4;
					if(old != LightRadius()){
						UpdateRadius(old,LightRadius());
					}
				}*/
			}
			if(curhp <= 0){
				if(type == ActorType.PLAYER){
					if(magic_trinkets.Contains(MagicTrinketType.PENDANT_OF_LIFE)){
						curhp = 1;
						/*attrs[AttrType.INVULNERABLE]++;
						Q.Add(new Event(this,1,AttrType.INVULNERABLE));*/
						if(R.CoinFlip()){
							B.Add("Your pendant glows brightly, then crumbles to dust! ");
							magic_trinkets.Remove(MagicTrinketType.PENDANT_OF_LIFE);
						}
						else{
							B.Add("Your pendant glows brightly! ");
						}
					}
					else{
						if(cause_of_death.Length > 0 && cause_of_death[0] == '*'){
							Global.KILLED_BY = cause_of_death.Substring(1);
						}
						else{
							Global.KILLED_BY = "killed by " + cause_of_death;
						}
						M.Draw();
						if(Global.GAME_OVER == false){
							B.Add("You die. ");
						}
						B.PrintAll();
						Global.GAME_OVER = true;
						return false;
					}
				}
				else{
					if(HasAttr(AttrType.BOSS_MONSTER)){
						M.Draw();
						B.Add("The fire drake dies. ");
						B.PrintAll();
						if(player.curhp > 0){
							B.Add("The threat to your nation has been slain! You begin the long trek home to deliver the good news... ");
							Global.KILLED_BY = "Died of ripe old age";
						}
						else{
							B.Add("The threat to your nation has been slain! Unfortunately, you won't be able to deliver the news... ");
						}
						B.PrintAll();
						Global.GAME_OVER = true;
						Global.BOSS_KILLED = true;
					}
					if(dmg.amount < 1000){ //everything that deals this much damage prints its own message
						if(type == ActorType.BERSERKER){
							if(!HasAttr(AttrType.COOLDOWN_1)){
								attrs[AttrType.COOLDOWN_1]++;
								Q.Add(new Event(this,R.Between(3,5)*100,AttrType.COOLDOWN_1)); //changed from 350
								Q.KillEvents(this,AttrType.COOLDOWN_2);
								if(!HasAttr(AttrType.COOLDOWN_2)){
									attrs[AttrType.COOLDOWN_2] = DirectionOf(player);
								}
								B.Add(the_name + " somehow remains standing! He screams with fury! ",this);
							}
							return true;
						}
						if(type == ActorType.GHOST){
							Event e = Q.FindTargetedEvent(this,EventType.TOMBSTONE_GHOST);
							if(e != null){
								e.dead = true;
								Q.Add(new Event(null,e.area,R.Between(3,6)*100,EventType.TOMBSTONE_GHOST));
							}
						}
						if(HasAttr(AttrType.REGENERATES_FROM_DEATH) && dmg.type != DamageType.FIRE){
							B.Add(the_name + " collapses, still twitching. ",this);
						}
						else{
							if(HasAttr(AttrType.REASSEMBLES)){
								if(Weapon.IsBlunt(dmg.weapon_used) && R.CoinFlip()){
									B.Add(the_name + " is smashed to pieces. ",this);
									attrs[AttrType.REASSEMBLES] = 0;
								}
								else{
									B.Add(the_name + " collapses into a pile of bones. ",this);
								}
							}
							else{
								if(!HasAttr(AttrType.BOSS_MONSTER) && type != ActorType.SPORE_POD){
									if(HasAttr(AttrType.NONLIVING)){
										B.Add(the_name + " is destroyed. ",this);
									}
									else{
										if(type == ActorType.FINAL_LEVEL_CULTIST && dmg.type == DamageType.FIRE){
											B.Add(the_name + " is consumed by flames. ",this);
											List<int> valid_circles = new List<int>();
											for(int i=0;i<5;++i){
												if(M.FinalLevelSummoningCircle(i).PositionsWithinDistance(2).Any(x=>M.tile[x].Is(TileType.DEMONIC_IDOL))){
													valid_circles.Add(i);
												}
											}
											int nearest = valid_circles.WhereLeast(x=>DistanceFrom(M.FinalLevelSummoningCircle(x))).Random();
											pos circle = M.FinalLevelSummoningCircle(nearest);
											if(M.actor[circle] != null){
												circle = circle.PositionsWithinDistance(3).Where(x=>M.tile[x].passable && M.actor[x] == null).Random();
											}
											M.final_level_cultist_count[nearest]++;
											if(M.final_level_cultist_count[nearest] >= 5){
												M.final_level_cultist_count[nearest] = 0;
												List<ActorType> valid_types = new List<ActorType>{ActorType.MINOR_DEMON};
												if(M.final_level_demon_count > 3){
													valid_types.Add(ActorType.FROST_DEMON);
												}
												if(M.final_level_demon_count > 5){
													valid_types.Add(ActorType.BEAST_DEMON);
												}
												if(M.final_level_demon_count > 11){
													valid_types.Add(ActorType.DEMON_LORD);
												}
												if(M.final_level_demon_count > 21){ //eventually, the majority will be demon lords
													valid_types.Add(ActorType.DEMON_LORD);
												}
												if(M.final_level_demon_count > 25){
													valid_types.Add(ActorType.DEMON_LORD);
												}
												if(M.final_level_demon_count > 31){
													valid_types.Add(ActorType.DEMON_LORD);
												}
												if(M.final_level_demon_count > 36){
													valid_types.Add(ActorType.DEMON_LORD);
													valid_types.Add(ActorType.DEMON_LORD);
													valid_types.Add(ActorType.DEMON_LORD);
												}
												if(player.CanSee(M.tile[circle])){
													B.Add("The flames leap and swirl, and a demon appears! ");
												}
												else{
													B.Add("You feel an evil presence. ");
												}
												Create(valid_types.Random(),circle.row,circle.col);
												if(M.actor[circle] != null){
													M.actor[circle].player_visibility_duration = -1;
													M.actor[circle].attrs[AttrType.PLAYER_NOTICED] = 1;
													if(M.actor[circle].type != ActorType.DEMON_LORD){
														M.actor[circle].attrs[AttrType.NO_ITEM] = 1;
													}
												}
												M.final_level_demon_count++;
											}
										}
										else{
											B.Add(the_name + " dies. ",this);
										}
									}
								}
								if(IsFinalLevelDemon()){
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
										if(a != this && a.IsFinalLevelDemon()){
											demons = true;
											break;
										}
									}
									if(!circles && !demons){ //victory
										curhp = 100;
										B.Add("As the last demon falls, your victory gives you a new surge of strength. ");
										B.PrintAll();
										B.Add("Kersai's summoning has been stopped. His cult will no longer threaten the area. ");
										B.PrintAll();
										B.Add("You begin the journey home to deliver the news. ");
										B.PrintAll();
										Global.GAME_OVER = true;
										Global.BOSS_KILLED = true;
										Global.KILLED_BY = "nothing";
										return false;
									}
								}
								if(HasAttr(AttrType.REGENERATES_FROM_DEATH) && dmg.type == DamageType.FIRE){
									attrs[AttrType.REGENERATES_FROM_DEATH] = 0;
								}
							}
						}
					}
					if(HasAttr(AttrType.TURN_INTO_CORPSE)){
						attrs[AttrType.CORPSE] = attrs[AttrType.TURN_INTO_CORPSE];
						attrs[AttrType.TURN_INTO_CORPSE] = 0;
						if(!HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							if(HasAttr(AttrType.NONLIVING)){
								SetName("destroyed " + name);
							}
							else{
								SetName(name + "'s corpse");
							}
						}
						return false;
					}
					if(HasAttr(AttrType.BURNING)){
						tile().AddFeature(FeatureType.FIRE);
					}
					if(LightRadius() > 0){
						UpdateRadius(LightRadius(),0);
					}
					if(type == ActorType.SHADOW){
						type = ActorType.ZOMBIE; //awful awful hack. (CalculateDimming checks for Shadows)
						CalculateDimming();
						type = ActorType.SHADOW;
					}
					if(HasAttr(AttrType.REGENERATES_FROM_DEATH)){
						Tile troll = null;
						foreach(Tile t in M.ReachableTilesByDistance(row,col,false)){
							if(!t.Is(TileType.DOOR_O) && !t.Is(FeatureType.TROLL_CORPSE,FeatureType.TROLL_BLOODWITCH_CORPSE,FeatureType.BONES)){
								if(type == ActorType.TROLL){
									t.AddFeature(FeatureType.TROLL_CORPSE);
								}
								else{
									t.AddFeature(FeatureType.TROLL_BLOODWITCH_CORPSE);
								}
								troll = t;
								break;
							}
						}
						if(curhp > -3){
							curhp = -3;
						}
						//curhp -= R.Roll(10)+5;
						if(curhp < -50){
							curhp = -50;
						}
						Event e = new Event(troll,100,EventType.REGENERATING_FROM_DEATH);
						e.value = curhp;
						e.secondary_value = attrs[AttrType.PERMANENT_DAMAGE];
						e.tiebreaker = tiebreakers.IndexOf(this);
						Q.Add(e);
					}
					if(HasAttr(AttrType.REASSEMBLES)){
						Tile sk = null;
						foreach(Tile t in M.ReachableTilesByDistance(row,col,false)){
							if(!t.Is(TileType.DOOR_O) && !t.Is(FeatureType.TROLL_CORPSE,FeatureType.TROLL_BLOODWITCH_CORPSE,FeatureType.BONES)){
								if(type == ActorType.SKELETON){
									t.AddFeature(FeatureType.BONES);
								}
								sk = t;
								break;
							}
						}
						Event e = new Event(sk,R.Between(10,20)*100,EventType.REASSEMBLING);
						e.tiebreaker = tiebreakers.IndexOf(this);
						Q.Add(e);
					}
					if(type == ActorType.STONE_GOLEM){
						List<Tile> deleted = new List<Tile>();
						while(true){
							bool changed = false;
							foreach(Tile t in TilesWithinDistance(3)){
								if(t.Is(TileType.STALAGMITE) && HasLOE(t)){
									t.Toggle(null);
									deleted.Add(t);
									changed = true;
								}
							}
							if(!changed){
								break;
							}
						}
						Q.RemoveTilesFromEventAreas(deleted,EventType.STALAGMITE);
						List<Tile> area = new List<Tile>();
						foreach(Tile t in TilesWithinDistance(3)){
							if((t.IsTrap() || t.Is(TileType.FLOOR,TileType.GRAVE_DIRT,TileType.GRAVEL)) && t.inv == null && (t.actor() == null || t.actor() == this) && HasLOE(t)){
								if(R.CoinFlip()){
									area.Add(t);
								}
							}
						}
						if(area.Count > 0){
							foreach(Tile t in area){
								TileType previous_type = t.type;
								t.Toggle(null,TileType.STALAGMITE);
								t.toggles_into = previous_type;
							}
							Q.Add(new Event(area,150,EventType.STALAGMITE,5));
						}
					}
					if(type == ActorType.VULGAR_DEMON && DistanceFrom(player) == 1){
						B.Add("The vulgar demon possesses your " + player.EquippedWeapon + "! ");
						B.Print(true);
						player.EquippedWeapon.status[EquipmentStatus.POSSESSED] = true;
						Help.TutorialTip(TutorialTopic.Possessed);
					}
					if(type == ActorType.DREAM_SPRITE){
						int num = R.Roll(5) + 4;
						List<Tile> new_area = tile().AddGaseousFeature(FeatureType.PIXIE_DUST,num);
						if(new_area.Count > 0){
							Event.RemoveGas(new_area,400,FeatureType.PIXIE_DUST,25);
						}
					}
					if(type == ActorType.FROSTLING){
						if(player.CanSee(tile()) && player.HasLOS(tile())){
							AnimateExplosion(this,2,Color.RandomIce,'*');
							B.Add("The air freezes around the defeated frostling. ",this);
						}
						foreach(Tile t in TilesWithinDistance(2)){
							if(HasLOE(t)){
								t.ApplyEffect(DamageType.COLD);
								Actor a = t.actor();
								if(a != null && a != this){
									a.ApplyFreezing();
								}
							}
						}
					}
					if(player.HasAttr(AttrType.CONVICTION)){
						player.attrs[AttrType.KILLSTREAK]++;
					}
					if(HasAttr(AttrType.HUMANOID_INTELLIGENCE) || type == ActorType.ZOMBIE){
						if(R.OneIn(3) && !HasAttr(AttrType.NO_ITEM)){
							tile().GetItem(Item.Create(Item.RandomItem(),-1,-1));
						}
					}
					foreach(Item item in inv){
						tile().GetItem(item);
					}
					Q.KillEvents(this,EventType.ANY_EVENT);
					M.RemoveTargets(this);
					int idx = tiebreakers.IndexOf(this);
					if(idx != -1){
						tiebreakers[idx] = null;
					}
					if(group != null){
						if(type == ActorType.DREAM_WARRIOR || type == ActorType.DREAM_SPRITE){
							List<Actor> temp = new List<Actor>();
							foreach(Actor a in group){
								if(a != this){
									temp.Add(a);
									a.group = null;
								}
							}
							foreach(Actor a in temp){
								a.Kill();
							}
						}
						else{
							if(group.Count >= 2 && this == group[0] && HasAttr(AttrType.WANDERING)){
								if(type != ActorType.NECROMANCER){
									group[1].attrs[AttrType.WANDERING]++;
								}
							}
							if(group.Count <= 2 || type == ActorType.NECROMANCER){
								foreach(Actor a in group){
									if(a != this){
										a.group = null;
									}
								}
								group.Clear();
								group = null;
							}
							else{
								group.Remove(this);
								group = null;
							}
						}
					}
					M.actor[row,col] = null;
					return false;
				}
			}
			else{
				if(HasFeat(FeatType.FEEL_NO_PAIN) && damage_dealt && curhp < 20 && old_hp >= 20){
					B.Add("You can feel no pain! ");
					attrs[AttrType.INVULNERABLE]++;
					Q.Add(new Event(this,500,AttrType.INVULNERABLE,"You can feel pain again. "));
				}
				if(magic_trinkets.Contains(MagicTrinketType.CLOAK_OF_SAFETY) && damage_dealt && dmg.amount >= curhp){
					B.PrintAll();
					M.Draw();
					if(B.YesOrNoPrompt("Your cloak starts to vanish. Use your cloak to escape?",false)){
						bool[,] good = new bool[ROWS,COLS];
						foreach(Tile t in M.AllTiles()){
							if(t.passable){
								good[t.row,t.col] = true;
							}
							else{
								good[t.row,t.col] = false;
							}
						}
						foreach(Actor a in M.AllActors()){
							foreach(Tile t in M.AllTiles()){
								if(good[t.row,t.col]){
									if(a.DistanceFrom(t) < 6 || a.HasLOS(t.row,t.col)){ //was CanSee, but this is safer
										good[t.row,t.col] = false;
									}
								}
							}
						}
						List<Tile> tilelist = new List<Tile>();
						Tile destination = null;
						for(int i=4;i<COLS;++i){
							foreach(pos p in PositionsAtDistance(i)){
								if(good[p.row,p.col]){
									tilelist.Add(M.tile[p.row,p.col]);
								}
							}
							if(tilelist.Count > 0){
								destination = tilelist[R.Roll(1,tilelist.Count)-1];
								break;
							}
						}
						if(destination != null){
							Move(destination.row,destination.col);
						}
						else{
							for(int i=0;i<9999;++i){
								int rr = R.Roll(1,ROWS-2);
								int rc = R.Roll(1,COLS-2);
								if(M.tile[rr,rc].passable && M.actor[rr,rc] == null && DistanceFrom(rr,rc) >= 6 && !M.tile[rr,rc].IsTrap()){
									Move(rr,rc);
									break;
								}
							}
						}
						B.Add("You escape. ");
					}
					B.Add("Your cloak vanishes completely! ");
					magic_trinkets.Remove(MagicTrinketType.CLOAK_OF_SAFETY);
				}
			}
			return true;
		}
		public void IncreaseExhaustion(int amount){
			int previous = exhaustion;
			int effective_previous = exhaustion;
			exhaustion += amount;
			if(exhaustion > 100){
				exhaustion = 100;
			}
			if(exhaustion < 0){
				exhaustion = 0;
			}
			if(this == player){
				if(exhaustion > 0){
					Help.TutorialTip(TutorialTopic.Exhaustion);
				}
				int effective_exhaustion = exhaustion;
				if(HasFeat(FeatType.ARMOR_MASTERY)){
					effective_exhaustion -= 25;
					effective_previous -= 25;
				}
				bool msg = false;
				switch(EquippedArmor.type){
				case ArmorType.LEATHER:
					if(effective_exhaustion >= 75 && effective_previous < 75){
						msg = true;
					}
					break;
				case ArmorType.CHAINMAIL:
					if(effective_exhaustion >= 50 && effective_previous < 50){
						msg = true;
					}
					break;
				case ArmorType.FULL_PLATE:
					if(effective_exhaustion >= 25 && effective_previous < 25){
						msg = true;
					}
					break;
				}
				if(msg){
					B.Add("You can no longer wear your " + EquippedArmor + " effectively in your exhausted state. ");
					Help.TutorialTip(TutorialTopic.ExhaustionAndArmor);
				}
				if(spells_in_order.Count > 0 && !HasFeat(FeatType.FORCE_OF_WILL)){
					int highest_tier = Spell.Tier(spells_in_order.WhereGreatest(x=>Spell.Tier(x))[0]);
					//so at tier 5, the threshold is 20
					int threshold = 120 - highest_tier*20;
					if(exhaustion > threshold && previous <= threshold){
						Help.TutorialTip(TutorialTopic.SpellFailure);
					}
				}
				if(exhaustion == 100 && previous < 100){
					B.Add("Your exhaustion makes it hard to even lift your " + EquippedWeapon + ". ");
				}
			}
		}
		public void RemoveExhaustion(){
			int previous = exhaustion;
			int effective_previous = exhaustion;
			exhaustion = 0;
			if(this == player){
				if(HasFeat(FeatType.ARMOR_MASTERY)){
					effective_previous -= 25;
				}
				bool msg = false;
				switch(EquippedArmor.type){
				case ArmorType.LEATHER:
					if(effective_previous >= 75){
						msg = true;
					}
					break;
				case ArmorType.CHAINMAIL:
					if(effective_previous >= 50){
						msg = true;
					}
					break;
				case ArmorType.FULL_PLATE:
					if(effective_previous >= 25){
						msg = true;
					}
					break;
				}
				if(msg){
					B.Add("You feel comfortable in your " + EquippedArmor + " again. ");
				}
				if(previous == 100){
					B.Add("You can wield your " + EquippedWeapon + " properly again. ");
				}
			}
		}
		public bool IsSilencedHere(){
			if(HasAttr(AttrType.SILENCED)){
				return true;
			}
			foreach(Actor a in ActorsWithinDistance(2)){
				if(a.HasAttr(AttrType.SILENCE_AURA) && a.HasLOE(this)){
					return true;
				}
			}
			return false;
		}
		public bool SilencedThisTurn(){
			if(target == null){
				return false;
			}
			if(curhp == maxhp && HasAttr(AttrType.SILENCED) && CanSee(target)){
				AI_Flee();
				QS();
				return true;
			}
			//if at max health, flee from the source, whatever it is. (giving priority to the player)
			//otherwise, revert to basic AI.
			bool aura = false;
			bool target_has_silence_aura = false;
			foreach(Actor a in ActorsWithinDistance(2)){
				if(a.HasAttr(AttrType.SILENCE_AURA) && a.HasLOE(this)){
					if(a == target){
						target_has_silence_aura = true;
					}
					aura = true;
				}
			}
			if(aura || HasAttr(AttrType.SILENCED)){ //this could also check for exhaustion too high to cast any known spells
				if(curhp == maxhp){
					if(target_has_silence_aura){
						AI_Flee();
					}
					else{
						AI_Step(ActorsWithinDistance(2).Where(x=>x.HasAttr(AttrType.SILENCE_AURA) && x.HasLOE(this)).Random(),true);
					}
					QS();
					return true;
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
					return true;
				}
			}
			return false;
		}
		public bool AI_UseRandomItem(){
			if(inv == null){
				return false;
			}
			List<Item> valid = new List<Item>();
			foreach(Item i in inv){
				string n = i.NameOfItemType();
				if(n == "potion"){
					if(!HasAttr(AttrType.NONLIVING)){
						valid.Add(i);
					}
				}
				else{
					if(n == "scroll"){
						if(!IsSilencedHere()){
							valid.Add(i);
						}
					}
					else{
						if(n == "orb"){
							if(target != null && FirstActorInLine(target) != null && target.DistanceFrom(FirstActorInLine(target)) <= 1 && CanSee(target) && HasLOE(target)){
								valid.Add(i);
							}
						}
						else{
							if(n == "wand"){
								if(target != null && CanSee(target) && HasLOE(target)){
									valid.Add(i);
								}
							}
							else{
								if(i.type == ConsumableType.BANDAGES && target != null && DistanceFrom(target) > 2 && !HasAttr(AttrType.BANDAGED) && (maxhp - curhp) >= 10){
									valid.Add(i);
								}
								else{
									if(i.type == ConsumableType.FLINT_AND_STEEL && target != null && DistanceFrom(target) == 1 && (target.tile().IsCurrentlyFlammable() || target.HasAttr(AttrType.OIL_COVERED))){
										valid.Add(i);
									}
								}
							}
						}
					}
				}
			}
			if(valid.Count > 0){
				Item i = valid.Random();
				string n = i.NameOfItemType();
				if(n == "potion"){
					B.Add(the_name + " drinks a potion. ",this); //this could print the potion type, whether known or not
				}
				else{
					if(n == "scroll"){
						B.Add(the_name + " reads a scroll. ",this);
					}
					else{
						if(n == "wand"){
							if(CanSee(player)){
								B.Add(the_name + " points a wand at you. ",this);
							}
							else{
								B.Add(the_name + " points a wand. ",this);
							}
						}
					}
				}
				bool break_wand = false;
				if(n == "wand" && i.charges == 0){
					break_wand = true;
				}
				i.Use(this,GetBestExtendedLineOfEffect(player));
				if(break_wand){
					B.Add(the_name + " breaks the wand in anger. ",this);
					inv.Remove(i);
				}
				return true;
			}
			return false;
		}
		public void CastCloseRangeSpellOrAttack(Actor a){ CastCloseRangeSpellOrAttack(null,a,false); }
		public void CastCloseRangeSpellOrAttack(List<SpellType> sp,Actor a,bool range_one_only){
			if(sp == null){
				sp = new List<SpellType>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(HasSpell(spell)){
						switch(spell){
						case SpellType.FORCE_PALM:
						case SpellType.MAGIC_HAMMER:
							sp.Add(spell);
							break;
						case SpellType.MERCURIAL_SPHERE:
						case SpellType.LIGHTNING_BOLT:
						case SpellType.DOOM:
						case SpellType.COLLAPSE:
						case SpellType.BLIZZARD:
						case SpellType.TELEKINESIS:
						case SpellType.STONE_SPIKES:
							if(!range_one_only){
								sp.Add(spell);
							}
							break;
						case SpellType.FREEZE:
							if(!range_one_only && !a.HasAttr(AttrType.FROZEN)){
								sp.Add(spell);
							}
							break;
						case SpellType.SCORCH:
							if(!range_one_only && (!a.HasAttr(AttrType.BURNING) || type == ActorType.GOBLIN_SHAMAN)){ //goblins are dumb
								sp.Add(spell);
							}
							break;
						default:
							break;
						}
					}
				}
			}
			if(sp.Count > 0){
				CastRandomSpell(a,sp.ToArray());
			}
			else{
				Attack(0,a);
			}
		}
		public void CastRangedSpellOrMove(Actor a){ CastRangedSpellOrMove(null,a); }
		public void CastRangedSpellOrMove(List<SpellType> sp,Actor a){
			if(sp == null){
				sp = new List<SpellType>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(HasSpell(spell)){
						switch(spell){
						case SpellType.MERCURIAL_SPHERE:
						case SpellType.LIGHTNING_BOLT:
						case SpellType.DOOM:
						case SpellType.COLLAPSE:
						case SpellType.STONE_SPIKES:
						case SpellType.TELEKINESIS: //todo update these lists
							sp.Add(spell);
							break;
						case SpellType.FLYING_LEAP:
							if(!HasAttr(AttrType.FLYING_LEAP)){
								sp.Add(spell);
							}
							break;
						case SpellType.FREEZE:
							if(!a.HasAttr(AttrType.FROZEN)){
								sp.Add(spell);
							}
							break;
						case SpellType.SCORCH:
							if(!a.HasAttr(AttrType.BURNING) || type == ActorType.GOBLIN_SHAMAN){
								sp.Add(spell);
							}
							break;
						case SpellType.BLIZZARD:
							if(DistanceFrom(a) <= 5){
								sp.Add(SpellType.BLIZZARD);
							}
							break;
						default:
							break;
						}
					}
				}
			}
			if(sp.Count > 0){
				CastRandomSpell(a,sp.ToArray());
			}
			else{
				AI_Step(a);
				QS();
			}
		}
		public bool CastSpell(SpellType spell){ return CastSpell(spell,null); }
		public bool CastSpell(SpellType spell,PhysicalObject obj){ //returns false if targeting is canceled.
			if(StunnedThisTurn()){ //eventually this will be moved to the last possible second
				return true; //returns true because turn was used up. 
			}
			if(!HasSpell(spell)){
				return false;
			}
			if(HasAttr(AttrType.SILENCED)){
				if(this == player){
					B.Add("You can't cast while silenced. ");
				}
				return false;
			}
			foreach(Actor a in ActorsWithinDistance(2)){
				if(a.HasAttr(AttrType.SILENCE_AURA) && a.HasLOE(this)){
					if(this == player){
						if(CanSee(a)){
							B.Add(a.Your() + " aura of silence disrupts your spell! ");
						}
						else{
							B.Add("An aura of silence disrupts your spell! ");
						}
					}
					return false;
				}
			}
			int required_mana = Spell.Tier(spell);
			if(HasAttr(AttrType.CHAIN_CAST) && required_mana > 1){
				required_mana--;
			}
			if(curmp < required_mana && this == player){
				int missing_mana = required_mana - curmp;
				if(exhaustion + missing_mana*5 > 100){
					B.Add("You're too exhausted! ");
					return false;
				}
				if(!B.YesOrNoPrompt("Really exhaust yourself to cast this spell?")){
					return false;
				}
				Screen.CursorVisible = false;
			}
			Tile t = null;
			List<Tile> line = null;
			if(obj != null){
				t = M.tile[obj.row,obj.col];
				line = GetBestLineOfEffect(t);
			}
			if(exhaustion > 0){
				int fail = Spell.FailRate(spell,exhaustion);
				if(R.PercentChance(fail)){
					if(HasFeat(FeatType.FORCE_OF_WILL)){
						B.Add("You focus your will. ");
					}
					else{
						if(player.CanSee(this)){
							B.Add("Sparks fly from " + Your() + " fingers. ",this);
						}
						else{
							if(player.DistanceFrom(this) <= 4 || (player.DistanceFrom(this) <= 12 && player.HasLOE(row,col))){
								B.Add("You hear words of magic, but nothing happens. ");
							}
						}
						if(this == player){
							Help.TutorialTip(TutorialTopic.SpellFailure);
						}
						Q1();
						return true;
					}
				}
			}
			int bonus = 0; //used for bonus damage on spells
			if(HasFeat(FeatType.MASTERS_EDGE)){
				foreach(SpellType s in spells_in_order){
					if(Spell.IsDamaging(s)){
						if(s == spell){
							bonus = 1;
						}
						break;
					}
				}
			}
			if(HasAttr(AttrType.EMPOWERED_SPELLS)){
				bonus++;
			}
			switch(spell){
			case SpellType.RADIANCE:
			{
				if(M.wiz_dark){
					B.Add("The magical darkness makes this spell impossible to cast. ");
					return false;
				}
				if(t == null){
					line = GetTargetTile(12,0,true,false);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " radiance. ",this);
					PhysicalObject o = null;
					int rad = -1;
					if(t.actor() != null){
						Actor a = t.actor();
						o = a;
						int old_rad = a.LightRadius();
						a.RefreshDuration(AttrType.SHINING,(R.Roll(2,20)+40)*100,a.You("no longer shine") + ". ",a);
						if(old_rad != a.LightRadius()){
							a.UpdateRadius(old_rad,a.LightRadius());
						}
						if(a != player){
							a.attrs[AttrType.TURNS_VISIBLE] = -1;
						}
						rad = a.LightRadius();
					}
					else{
						if(t.inv != null && t.inv.light_radius > 0){
							o = t.inv;
							rad = o.light_radius;
						}
						else{
							if(t.light_radius > 0){
								o = t;
								rad = o.light_radius;
							}
						}
					}
					if(o != null){
						if(o is Item){
							B.Add((o as Item).TheName(false) + " shines brightly. ",o);
						}
						else{
							B.Add(o.You("shine") + " brightly. ",o);
						}
						foreach(Actor a in o.ActorsWithinDistance(rad).Where(x=>x != this && o.HasLOS(x))){
							B.Add("The light burns " + a.the_name + ". ",a);
							a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(1+bonus,6),this,"a shining " + o.name);
						}
					}
					else{
						B.Add("Nothing happens. ");
					}
				}
				else{
					return false;
				}
				break;
			}
			case SpellType.FORCE_PALM:
				if(t == null){
					t = TileInDirection(GetDirection());
				}
				if(t != null){
					Actor a = M.actor[t.row,t.col];
					B.Add(You("cast") + " force palm. ",this);
					B.DisplayNow();
					Screen.AnimateMapCell(t.row,t.col,new colorchar('*',Color.Blue),100);
					bool self_knockback = false;
					if(a != null){
						B.Add(You("strike") + " " + a.TheName(true) + ". ",this,a);
						if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
							a.curmp += Spell.Tier(spell);
							if(a.curmp > a.maxmp){
								a.curmp = a.maxmp;
							}
							a.GainSpell(spell);
							B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
						}
						a.attrs[AttrType.TURN_INTO_CORPSE]++;
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(1+bonus,6),this,a_name);
						if(a.HasAttr(AttrType.IMMOBILE,AttrType.FROZEN)){
							self_knockback = true;
						}
						else{
							if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
								KnockObjectBack(a,1,this);
							}
						}
						a.CorpseCleanup();
					}
					else{
						if(t.passable){
							B.Add("You strike at empty space. ");
						}
						else{
							B.Add("You strike " + t.the_name + " with your palm. ");
							switch(t.type){
							case TileType.DOOR_C:
								B.Add("It flies open! ");
								t.Toggle(this);
								break;
							case TileType.HIDDEN_DOOR:
								B.Add("A hidden door flies open! ");
								t.Toggle(this);
								t.Toggle(this);
								break;
							case TileType.RUBBLE:
								B.Add("It scatters! ");
								t.Toggle(null);
								break;
							case TileType.CRACKED_WALL:
								B.Add("It falls to pieces! ");
								t.Toggle(null,TileType.FLOOR);
								foreach(Tile neighbor in t.TilesAtDistance(1)){
									neighbor.solid_rock = false;
								}
								break;
							case TileType.BARREL:
							case TileType.STANDING_TORCH:
							case TileType.POISON_BULB:
								t.Bump(DirectionOf(t));
								break;
							default:
								self_knockback = true;
								break;
							}
						}
					}
					if(self_knockback){
						attrs[AttrType.TURN_INTO_CORPSE]++;
						t.KnockObjectBack(this,1,this);
						CorpseCleanup();
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.DETECT_MOVEMENT:
				B.Add(You("cast") + " detect movement. ",this);
				if(this == player){
					B.Add("Your senses sharpen. ");
					if(!HasAttr(AttrType.DETECTING_MOVEMENT)){
						previous_footsteps = new List<pos>(); //prevents old footsteps from appearing
					}
					RefreshDuration(AttrType.DETECTING_MOVEMENT,(R.Roll(2,20)+30)*100,"You no longer detect movement. ");
				}
				else{
					RefreshDuration(AttrType.DETECTING_MOVEMENT,(R.Roll(2,20)+30)*100);
				}
				break;
			case SpellType.FLYING_LEAP:
				B.Add(You("cast") + " flying leap. ",this);
				RefreshDuration(AttrType.FLYING_LEAP,299);
				RefreshDuration(AttrType.FLYING,299);
				B.Add(You("move") + " quickly through the air. ",this);
				if(this == player){
					Help.TutorialTip(TutorialTopic.IncreasedSpeed);
				}
				break;
			case SpellType.MERCURIAL_SPHERE:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " mercurial sphere. ",this);
					Actor a = FirstActorInLine(line);
					line = line.ToFirstSolidTileOrActor();
					M.Draw();
					AnimateProjectile(line,'*',Color.Blue);
					List<string> targets = new List<string>();
					List<Tile> locations = new List<Tile>();
					if(a != null){
						for(int i=0;i<4;++i){
							if(player.CanSee(a)){
								targets.AddUnique(a.the_name);
							}
							Tile atile = a.tile();
							if(a != this){
								if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
									a.curmp += Spell.Tier(spell);
									if(a.curmp > a.maxmp){
										a.curmp = a.maxmp;
									}
									a.GainSpell(spell);
									B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
								}
								a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(2+bonus,6),this,a_name);
							}
							a = atile.ActorsWithinDistance(3,true).Where(x=>atile.HasLOE(x)).Random();
							locations.AddUnique(atile);
							if(a == null){
								break;
							}
							if(i < 3){
								Screen.AnimateProjectile(atile.GetBestLineOfEffect(a),new colorchar('*',Color.Blue));
							}
						}
						int unknown = locations.Count - targets.Count; //every location for which we didn't see an actor
						if(unknown > 0){
							if(unknown == 1){
								targets.Add("one unseen creature");
							}
							else{
								targets.Add(unknown.ToString() + " unseen creatures");
							}
						}
						if(targets.Contains("you")){
							targets.Remove("you");
							targets.Add("you"); //move it to the end of the list
						}
						if(targets.Count == 1){
							B.Add("The sphere hits " + targets[0] + ". ",locations.ToArray());
						}
						else{
							B.Add("The sphere bounces between " + targets.ConcatenateListWithCommas() + ". ",locations.ToArray());
						}
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.GREASE:
			{
				Tile prev = null;
				if(t == null){
					line = GetTargetTile(12,1,true,false);
					if(line != null){
						t = line.Last();
						prev = line.LastBeforeSolidTile();
					}
				}
				if(t != null){
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					B.Add(You("cast") + " grease. ",this);
					B.Add("Oil covers the floor. ",t);
					foreach(Tile neighbor in t.TilesWithinDistance(1)){
						if(neighbor.passable && LOE_tile.HasLOE(neighbor)){
							neighbor.AddFeature(FeatureType.OIL);
						}
					}
				}
				else{
					return false;
				}
				break;
			}
			case SpellType.BLINK:
				if(HasAttr(AttrType.IMMOBILE)){
					if(this == player){
						B.Add("You can't blink while immobilized. ");
						return false;
					}
					else{
						B.Add(You("cast") + " blink. ",this);
						B.Add("The spell fails. ",this);
						Q1();
						return true;
					}
				}
				for(int i=0;i<9999;++i){
					int a = R.Roll(1,17) - 9; //-8 to 8
					int b = R.Roll(1,17) - 9;
					if(Math.Abs(a) + Math.Abs(b) >= 6){
						a += row;
						b += col;
						if(M.BoundsCheck(a,b) && M.tile[a,b].passable && M.actor[a,b] == null){
							B.Add(You("cast") + " blink. ",this);
							B.Add(You("step") + " through a rip in reality. ",this);
							if(player.CanSee(this)){
								AnimateStorm(2,3,4,'*',Color.DarkMagenta);
							}
							Move(a,b);
							M.Draw();
							if(player.CanSee(this)){
								AnimateStorm(2,3,4,'*',Color.DarkMagenta);
							}
							break;
						}
					}
				}
				break;
			case SpellType.FREEZE:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " freeze. ",this);
					Actor a = FirstActorInLine(line);
					AnimateBoltBeam(line.ToFirstSolidTileOrActor(),Color.Cyan);
					foreach(Tile t2 in line){
						t2.ApplyEffect(DamageType.COLD);
					}
					if(a != null){
						a.ApplyFreezing();
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SCORCH:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " scorch. ",this);
					Actor a = FirstActorInLine(line);
					line = line.ToFirstSolidTileOrActor();
					AnimateProjectile(line,'*',Color.RandomFire);
					foreach(Tile t2 in line){
						t2.ApplyEffect(DamageType.FIRE);
					}
					if(a != null){
						B.Add("The scorching bolt hits " + a.the_name + ". ",a);
						if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
							a.curmp += Spell.Tier(spell);
							if(a.curmp > a.maxmp){
								a.curmp = a.maxmp;
							}
							a.GainSpell(spell);
							B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
						}
						//if(a.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,R.Roll(1+bonus,6),this,a_name)){ //todo: testing this without damage
							a.ApplyBurning();
						//}
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.LIGHTNING_BOLT: //todo: limit the bolt to 12 tiles or not? should it spread over an entire huge lake? 12 tiles is still quite a lot.
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " lightning bolt. ",this);
					PhysicalObject bolt_target = null;
					List<Actor> damage_targets = new List<Actor>();
					foreach(Tile t2 in line){
						if(t2.actor() != null && t2.actor() != this){
							bolt_target = t2.actor();
							damage_targets.Add(t2.actor());
							break;
						}
						else{
							if(t2.ConductsElectricity()){
								bolt_target = t2;
								break;
							}
						}
					}
					if(bolt_target != null){ //this code, man
						Dict<PhysicalObject,List<PhysicalObject>> chain = new Dict<PhysicalObject,List<PhysicalObject>>();
						chain[this] = new List<PhysicalObject>{bolt_target};
						List<PhysicalObject> last_added = new List<PhysicalObject>{bolt_target};
						for(bool done=false;!done;){
							done = true;
							List<PhysicalObject> new_last_added = new List<PhysicalObject>();
							foreach(PhysicalObject added in last_added){
								List<PhysicalObject> sort_list = new List<PhysicalObject>();
								foreach(Tile nearby in added.TilesWithinDistance(3,true)){
									if(nearby.actor() != null || nearby.ConductsElectricity()){
										if(added.HasLOE(nearby)){
											if(nearby.actor() != null){
												bolt_target = nearby.actor();
											}
											else{
												bolt_target = nearby;
											}
											bool contains_value = false;
											foreach(List<PhysicalObject> list in chain.d.Values){
												foreach(PhysicalObject o in list){
													if(o == bolt_target){
														contains_value = true;
														break;
													}
												}
												if(contains_value){
													break;
												}
											}
											if(!chain.d.ContainsKey(bolt_target) && !contains_value){
												if(bolt_target as Actor != null){
													damage_targets.AddUnique(bolt_target as Actor);
												}
												done = false;
												if(sort_list.Count == 0){
													sort_list.Add(bolt_target);
												}
												else{
													int idx = 0;
													foreach(PhysicalObject o in sort_list){
														if(bolt_target.DistanceFrom(added) < o.DistanceFrom(added)){
															sort_list.Insert(idx,bolt_target);
															break;
														}
														++idx;
													}
													if(idx == sort_list.Count){
														sort_list.Add(bolt_target);
													}
												}
												if(chain[added] == null){
													chain[added] = new List<PhysicalObject>{bolt_target};
												}
												else{
													chain[added].Add(bolt_target);
												}
											}
										}
									}
								}
								foreach(PhysicalObject o in sort_list){
									new_last_added.Add(o);
								}
							}
							if(!done){
								last_added = new_last_added;
							}
						} //whew. the tree structure is complete. start at chain[this] and go from there...
						Dict<int,List<pos>> frames = new Dict<int,List<pos>>();
						Dict<PhysicalObject,int> line_length = new Dict<PhysicalObject,int>();
						line_length[this] = 0;
						List<PhysicalObject> current = new List<PhysicalObject>{this};
						List<PhysicalObject> next = new List<PhysicalObject>();
						while(current.Count > 0){
							foreach(PhysicalObject o in current){
								if(chain[o] != null){
									foreach(PhysicalObject o2 in chain[o]){
										List<Tile> bres = o.GetBestLineOfEffect(o2);
										bres.RemoveAt(0);
										line_length[o2] = bres.Count + line_length[o];
										int idx = 0;
										foreach(Tile t2 in bres){
											if(frames[idx + line_length[o]] != null){
												frames[idx + line_length[o]].Add(new pos(t2.row,t2.col));
											}
											else{
												frames[idx + line_length[o]] = new List<pos>{new pos(t2.row,t2.col)};
											}
											++idx;
										}
										next.Add(o2);
									}
								}
							}
							current = next;
							next = new List<PhysicalObject>();
						}
						List<pos> frame = frames[0];
						for(int i=0;frame != null;++i){
							foreach(pos p in frame){
								Screen.WriteMapChar(p.row,p.col,'*',Color.RandomLightning);
							}
							Game.GLUpdate();
							Thread.Sleep(50);
							frame = frames[i];
						}
						foreach(Actor a in damage_targets){
							B.Add("The bolt hits " + a.the_name + ". ",a);
							if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
								a.curmp += Spell.Tier(spell);
								if(a.curmp > a.maxmp){
									a.curmp = a.maxmp;
								}
								a.GainSpell(spell);
								B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
							}
							a.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,R.Roll(3+bonus,6),this,a_name);
						}
					}
					else{
						AnimateBeam(line,'*',Color.RandomLightning);
						B.Add("The bolt hits " + t.the_name + ". ",t);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.MAGIC_HAMMER:
				if(t == null){
					t = TileInDirection(GetDirection());
				}
				if(t != null){
					Actor a = t.actor();
					B.Add(You("cast") + " magic hammer. ",this);
					M.Draw();
					B.DisplayNow();
					Screen.AnimateMapCell(t.row,t.col,new colorchar('*',Color.Magenta),100);
					if(a != null){
						B.Add(You("wallop") + " " + a.TheName(true) + ". ",this,a);
						if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(4+bonus,6),this,a_name)){
							a.ApplyStatus(AttrType.STUNNED,201);
							/*a.RefreshDuration(AttrType.STUNNED,a.DurationOfMagicalEffect(2) * 100 + 1,a.YouAre() + " no longer stunned. ",a); //todo fix this so it doesn't use the +1, since nothing else does that any more.
							B.Add(a.YouAre() + " stunned. ",a);*/
						}
					}
					else{
						B.Add("You strike " + t.the_name + ". ");
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.PORTAL: //player-only for now
			{
				t = tile();
				if(t.Is(FeatureType.INACTIVE_TELEPORTAL,FeatureType.STABLE_TELEPORTAL,FeatureType.TELEPORTAL) || t.Is(TileType.DOOR_O,TileType.STAIRS)){
					B.Add("You can't create a portal here. ");
					return false;
				}
				B.Add("You cast portal. ");
				List<Tile> other_portals = M.AllTiles().Where(x=>x.Is(FeatureType.INACTIVE_TELEPORTAL,FeatureType.STABLE_TELEPORTAL));
				if(other_portals.Count == 0){
					B.Add("You create a dormant portal. ");
					t.AddFeature(FeatureType.INACTIVE_TELEPORTAL);
				}
				else{
					if(other_portals.Count == 1){ //it should be inactive in this case
						B.Add("You open a portal. ");
						t.AddFeature(FeatureType.STABLE_TELEPORTAL);
						Tile t2 = other_portals[0];
						t2.RemoveFeature(FeatureType.INACTIVE_TELEPORTAL);
						t2.AddFeature(FeatureType.STABLE_TELEPORTAL);
						Q.Add(new Event(t,new List<Tile>{t2},100,EventType.TELEPORTAL,AttrType.NO_ATTR,100,""));
						Q.Add(new Event(t2,new List<Tile>{t},100,EventType.TELEPORTAL,AttrType.NO_ATTR,100,""));
					}
					else{
						B.Add("You open a portal. ");
						t.AddFeature(FeatureType.STABLE_TELEPORTAL);
						Q.Add(new Event(t,other_portals,100,EventType.TELEPORTAL,AttrType.NO_ATTR,100,""));
						foreach(Tile t2 in other_portals){
							Event e = Q.FindTargetedEvent(t2,EventType.TELEPORTAL);
							if(e != null){
								e.area.Add(t);
							}
						}
					}
				}
				break;
			}
			case SpellType.PASSAGE:
			{
				if(this == player && HasAttr(AttrType.IMMOBILE)){
					B.Add("You can't travel through a passage while immobilized. ");
					return false;
				}
				int dir = -1;
				if(t == null){
					dir = GetDirection(true,false);
					t = TileInDirection(dir);
				}
				else{
					dir = DirectionOf(t);
				}
				if(t != null){
					if(t.Is(TileType.WALL,TileType.CRACKED_WALL,TileType.WAX_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR,TileType.STONE_SLAB)){
						B.Add(You("cast") + " passage. ",this);
						colorchar ch = new colorchar(Color.Cyan,'!');
						if(this == player){
							Screen.CursorVisible = false;
							switch(DirectionOf(t)){
							case 8:
							case 2:
								ch.c = '|';
								break;
							case 4:
							case 6:
								ch.c = '-';
								break;
							}
						}
						else{
							if(HasAttr(AttrType.IMMOBILE)){
								B.Add("The spell fails. ",this);
								Q1();
								return true;
							}
						}
						List<Tile> tiles = new List<Tile>();
						List<colorchar> memlist = new List<colorchar>();
						Screen.CursorVisible = false;
						Tile last_wall = null;
						while(!t.passable){
							if(t.row == 0 || t.row == ROWS-1 || t.col == 0 || t.col == COLS-1){
								break;
							}
							if(this == player){
								tiles.Add(t);
								memlist.Add(Screen.MapChar(t.row,t.col));
								Screen.WriteMapChar(t.row,t.col,ch);
								Game.GLUpdate();
								Thread.Sleep(35);
							}
							last_wall = t;
							t = t.TileInDirection(dir);
						}
						Global.FlushInput();
						if(t.passable){
							if(t.actor() == null){
								int r = row;
								int c = col;
								Move(t.row,t.col);
								if(this == player){
									Screen.WriteMapChar(r,c,M.VisibleColorChar(r,c));
									Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
									int idx = 0;
									foreach(Tile tile in tiles){
										Screen.WriteMapChar(tile.row,tile.col,memlist[idx++]);
										Game.GLUpdate();
										Thread.Sleep(35);
									}
								}
								Global.FlushInput();
								B.Add(You("travel") + " through the passage. ",this,t);
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
									int r = row;
									int c = col;
									Move(destination.row,destination.col);
									if(this == player){
										Screen.WriteMapChar(r,c,M.VisibleColorChar(r,c));
										Screen.WriteMapChar(destination.row,destination.col,M.VisibleColorChar(destination.row,destination.col));
										int idx = 0;
										foreach(Tile tile in tiles){
											Screen.WriteMapChar(tile.row,tile.col,memlist[idx++]);
											Game.GLUpdate();
											Thread.Sleep(35);
										}
									}
									Global.FlushInput();
									B.Add(You("travel") + " through the passage. ",this,destination);
								}
								else{
									B.Add("Something blocks " + Your() + " movement through the passage. ",this);
								}
							}
						}
						else{
							if(this == player){
								int idx = 0;
								foreach(Tile tile in tiles){
									Screen.WriteMapChar(tile.row,tile.col,memlist[idx++]);
									Game.GLUpdate();
									Thread.Sleep(35);
								}
								Global.FlushInput();
								B.Add("The passage is blocked. ",this);
							}
						}
					}
					else{
						if(this == player){
							B.Add("There's no wall here. ");
						}
						return false;
					}
				}
				else{
					return false;
				}
				break;
			}
			case SpellType.DOOM:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " doom. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateProjectile(line.ToFirstSolidTileOrActor(),'*',Color.RandomDoom);
						if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
							a.curmp += Spell.Tier(spell);
							if(a.curmp > a.maxmp){
								a.curmp = a.maxmp;
							}
							a.GainSpell(spell);
							B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
						}
						if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(4+bonus,6),this,a_name)){
							a.ApplyStatus(AttrType.VULNERABLE,R.Between(4,8)*100);
							/*if(!a.HasAttr(AttrType.VULNERABLE)){
								B.Add(a.You("become") + " vulnerable. ",a);
							}
							a.RefreshDuration(AttrType.VULNERABLE,a.DurationOfMagicalEffect(R.Between(4,8)) * 100,a.YouAre() + " no longer so vulnerable. ",a);*/
							if(a == player){
								Help.TutorialTip(TutorialTopic.Vulnerable);
							}
						}
					}
					else{
						AnimateProjectile(line,'*',Color.RandomDoom);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.AMNESIA:
				if(t == null){
					t = TileInDirection(GetDirection());
				}
				if(t != null){
					Actor a = t.actor();
					if(a != null && (CanSee(a) || a.HasAttr(AttrType.DANGER_SENSED))){
						B.Add(You("cast") + " amnesia. ",this);
						a.AnimateStorm(2,4,4,'*',Color.RandomRainbow);
						if(a.ResistedBySpirit() || a.HasAttr(AttrType.MENTAL_IMMUNITY)){
							B.Add(a.You("resist") + "! ",a);
						}
						else{
							B.Add("You fade from " + a.TheName(true) + "'s awareness. ");
							a.player_visibility_duration = 0;
							a.target = null;
							a.target_location = null;
							a.attrs[AttrType.AMNESIA_STUN] = R.Between(7,8);
						}
					}
					else{
						B.Add("There's nothing to target there. ");
						return false;
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SHADOWSIGHT:
				B.Add("You cast shadowsight. ");
				B.Add("Your eyes pierce the darkness. ");
				int duration = (R.Roll(2,20) + 60) * 100;
				RefreshDuration(AttrType.SHADOWSIGHT,duration,"Your shadowsight wears off. ");
				RefreshDuration(AttrType.LOW_LIGHT_VISION,duration);
				break;
			case SpellType.BLIZZARD:
			{
				List<Actor> targets = ActorsWithinDistance(5,true).Where(x=>HasLOE(x));
				B.Add(You("cast") + " blizzard. ",this);
				AnimateStorm(5,8,24,'*',Color.RandomIce);
				B.Add("An ice storm surrounds " + the_name + ". ",this);
				foreach(Tile t2 in TilesWithinDistance(5).Where(x=>HasLOE(x))){
					t2.ApplyEffect(DamageType.COLD);
				}
				while(targets.Count > 0){
					int idx = R.Roll(1,targets.Count) - 1;
					Actor a = targets[idx];
					targets.Remove(a);
					//B.Add("The blizzard hits " + a.the_name + ". ",a);
					if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
						a.curmp += Spell.Tier(spell);
						if(a.curmp > a.maxmp){
							a.curmp = a.maxmp;
						}
						a.GainSpell(spell);
						B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
					}
					if(a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,R.Roll(5+bonus,6),this,a_name)){
						if(!a.HasAttr(AttrType.BURNING,AttrType.IMMUNE_COLD)){
							a.ApplyStatus(AttrType.SLOWED,R.Between(6,10)*100);
							/*B.Add(a.YouAre() + " slowed. ",a);
							a.RefreshDuration(AttrType.SLOWED,a.DurationOfMagicalEffect(R.Between(6,10)) * 100,a.YouAre() + " no longer slowed. ",a);*/
						}
					}
				}
				break;
			}
			case SpellType.TELEKINESIS:
				if(t == null){
					line = GetTargetTile(12,0,true,true);
					if(line != null){
						t = line.Last();
					}
				}
				if(!SharedEffect.Telekinesis(true,this,t)){
					return false;
				}
				break;
			case SpellType.COLLAPSE:
				if(t == null){
					line = GetTargetTile(12,0,true,false);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " collapse. ",this);
					B.DisplayNow();
					for(int dist=2;dist>0;--dist){
						List<pos> cells = new List<pos>();
						List<colorchar> chars = new List<colorchar>();
						pos p2 = new pos(t.row-dist,t.col-dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('\\',Color.DarkGreen));
						}
						p2 = new pos(t.row-dist,t.col+dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('/',Color.DarkGreen));
						}
						p2 = new pos(t.row+dist,t.col-dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('/',Color.DarkGreen));
						}
						p2 = new pos(t.row+dist,t.col+dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('\\',Color.DarkGreen));
						}
						Screen.AnimateMapCells(cells,chars);
					}
					Screen.AnimateMapCell(t.row,t.col,new colorchar('X',Color.DarkGreen));
					foreach(Tile neighbor in t.TilesWithinDistance(1).Randomize()){
						if(neighbor.p.BoundsCheck(M.tile,false)){
							if(neighbor.IsTrap()){
								B.Add("A falling stone triggers a trap. ",neighbor);
								//neighbor.TriggerTrap();
							}
							if(neighbor.passable){
								neighbor.ApplyEffect(DamageType.NORMAL); //break items and set off traps
							}
							if((neighbor == t && t.Is(TileType.WALL,TileType.FLOOR,TileType.RUBBLE,TileType.CRACKED_WALL)) || neighbor.Is(TileType.RUBBLE,TileType.FLOOR)){
								neighbor.Toggle(null,TileType.GRAVEL);
								neighbor.RemoveFeature(FeatureType.SLIME);
								neighbor.RemoveFeature(FeatureType.OIL);
								foreach(Tile n2 in neighbor.TilesAtDistance(1)){
									n2.solid_rock = false;
								}
							}
							else{
								if(neighbor.Is(TileType.CRACKED_WALL)){
									neighbor.Toggle(null,R.CoinFlip()? TileType.RUBBLE : TileType.GRAVEL);
									foreach(Tile n2 in neighbor.TilesAtDistance(1)){
										n2.solid_rock = false;
									}
								}
								else{
									if(neighbor.Is(TileType.WALL)){
										TileType new_type = TileType.FLOOR;
										switch(R.Roll(3)){
										case 1:
											new_type = TileType.CRACKED_WALL;
											break;
										case 2:
											new_type = TileType.RUBBLE;
											break;
										case 3:
											new_type = TileType.GRAVEL;
											break;
										}
										neighbor.Toggle(null,new_type);
										foreach(Tile n2 in neighbor.TilesAtDistance(1)){
											n2.solid_rock = false;
										}
									}
								}
							}
						}
					}
					foreach(Actor a in t.ActorsWithinDistance(1)){
						if(a != this){
							B.Add("Rubble falls on " + a.TheName(true) + ". ",a.tile());
							if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
								a.curmp += Spell.Tier(spell);
								if(a.curmp > a.maxmp){
									a.curmp = a.maxmp;
								}
								a.GainSpell(spell);
								B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
							}
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3+bonus,6),this,"falling rubble");
						}
					}
					t.MakeNoise(6);
				}
				else{
					return false;
				}
				break;
			case SpellType.STONE_SPIKES:
			{
				Tile prev = null;
				if(t == null){
					line = GetTargetTile(12,2,true,true);
					if(line != null){
						t = line.Last();
						prev = line.LastBeforeSolidTile();
					}
				}
				if(t != null){
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					B.Add(You("cast") + " stone spikes. ",this);
					B.Add("Stalagmites shoot up from the ground! ");
					List<Tile> deleted = new List<Tile>();
					while(true){
						bool changed = false;
						foreach(Tile nearby in t.TilesWithinDistance(2)){
							if(nearby.Is(TileType.STALAGMITE) && LOE_tile.HasLOE(nearby)){
								nearby.Toggle(null);
								deleted.Add(nearby);
								changed = true;
							}
						}
						if(!changed){
							break;
						}
					}
					Q.RemoveTilesFromEventAreas(deleted,EventType.STALAGMITE);
					List<Tile> area = new List<Tile>();
					List<Actor> affected_actors = new List<Actor>();
					foreach(Tile t2 in t.TilesWithinDistance(2)){
						if(LOE_tile.HasLOE(t2)){
							if(t2.actor() != null){
								affected_actors.Add(t2.actor());
							}
							else{
								if((t2.IsTrap() || t2.Is(TileType.FLOOR,TileType.GRAVE_DIRT,TileType.GRAVEL)) && t2.inv == null){
									if(!R.OneIn(4)){
										area.Add(t2);
									}
								}
							}
						}
					}
					foreach(Actor a in affected_actors){
						if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
							a.curmp += Spell.Tier(spell);
							if(a.curmp > a.maxmp){
								a.curmp = a.maxmp;
							}
							a.GainSpell(spell);
							B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
						}
						if(a != this){
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(4+bonus,6),this,a_name);
						}
					}
					if(area.Count > 0){
						foreach(Tile t2 in area){
							TileType previous_type = t2.type;
							t2.Toggle(null,TileType.STALAGMITE);
							t2.toggles_into = previous_type;
						}
						Q.Add(new Event(area,150,EventType.STALAGMITE,5));
					}
				}
				else{
					return false;
				}
				break;
			}
			}
			if(curmp >= required_mana){
				curmp -= required_mana;
			}
			else{
				IncreaseExhaustion((required_mana - curmp)*5);
				curmp = 0;
			}
			if(HasFeat(FeatType.ARCANE_INTERFERENCE)){
				foreach(Actor a in ActorsWithinDistance(12,true)){
					if(a.maxmp > 0 && HasLOE(a)){
						a.ApplyStatus(AttrType.STUNNED,R.Between(3,6)*100);
						if(a.HasSpell(spell)){
							B.Add(a.the_name + " can no longer cast " + Spell.Name(spell) + ". ",a);
							a.spells[spell] = false;
						}
					}
				}
				/*bool empowered = false;
				foreach(Actor a in ActorsWithinDistance(12,true)){
					if(a.HasSpell(spell) && HasLOE(a)){
						B.Add(a.the_name + " can no longer cast " + Spell.Name(spell) + ". ",a);
						a.spells[spell] = false;
						empowered = true;
					}
				}
				if(empowered){
					B.Add("Arcane feedback empowers your spells! ");
					RefreshDuration(AttrType.EMPOWERED_SPELLS,R.Between(7,12)*100,Your() + " spells are no longer empowered. ",this);
				}*/
			}
			if(HasFeat(FeatType.CHAIN_CASTING)){
				RefreshDuration(AttrType.CHAIN_CAST,100);
			}
			MakeNoise(4);
			if(this == player && !Help.displayed[TutorialTopic.CastingWithoutMana]){
				int max_tier = Spell.Tier(spells_in_order.WhereGreatest(x=>Spell.Tier(x)).Last());
				if(curmp < max_tier){
					Help.TutorialTip(TutorialTopic.CastingWithoutMana);
				}
			}
			Q1();
			return true;
		}
		public bool CastRandomSpell(PhysicalObject obj,params SpellType[] spells){
			if(spells.Length == 0){
				return false;
			}
			return CastSpell(spells[R.Roll(1,spells.Length)-1],obj);
		}
		public Color FailColor(int failrate){
			Color failcolor = Color.DarkGray;
			if(failrate > 50){
				failcolor = Color.DarkRed;
			}
			else{
				if(failrate > 20){
					failcolor = Color.Red;
				}
				else{
					if(failrate > 0){
						failcolor = Color.Yellow;
					}
				}
			}
			return failcolor;
		}
		public void ResetForNewLevel(){
			target = null;
			target_location = null;
			if(HasAttr(AttrType.DIM_LIGHT)){
				attrs[AttrType.DIM_LIGHT] = 0;
			}
			if(attrs[AttrType.RESTING] == -1){
				attrs[AttrType.RESTING] = 0;
			}
			if(HasAttr(AttrType.GRABBED)){
				attrs[AttrType.GRABBED] = 0;
			}
			Q.KillEvents(null,EventType.CHECK_FOR_HIDDEN);
		}
		public bool UseFeat(FeatType feat){
			switch(feat){
			case FeatType.LUNGE:
			{
				List<Tile> line = GetTargetTile(2,0,false,true);
				Tile t = null;
				if(line != null && line.Last() != tile()){
					t = line.Last();
				}
				if(t != null && t.actor() != null){
					bool moved = false;
					if(DistanceFrom(t) == 2 && line[1].passable && line[1].actor() == null && !MovementPrevented(line[1])){
						moved = true;
						B.Add("You lunge! ");
						Move(line[1].row,line[1].col);
						attrs[AttrType.LUNGING_AUTO_HIT] = 1;
						Attack(0,t.actor());
						attrs[AttrType.LUNGING_AUTO_HIT] = 0;
					}
					if(!moved){
						if(MovementPrevented(line[1])){
							B.Add("You can't currently reach that spot. ");
							return false;
						}
						else{
							B.Add("The way is blocked! ");
							return false;
						}
					}
					else{
						return true;
					}
				}
				else{
					return false;
				}
				//break;
			}
			case FeatType.TUMBLE:
			{
				target = null; //don't try to automatically pick previous targets while tumbling. this solution isn't ideal.
				List<Tile> line = GetTargetTile(2,0,false,false);
				target = null; //then, don't remember an actor picked as the target of tumble
				Tile t = null;
				if(line != null && line.Last() != tile()){
					t = line.Last();
				}
				if(t != null && t.passable && t.actor() == null && !MovementPrevented(t)){
					if(!t.seen){
						B.Add("You don't know what's over there! ");
						return false;
					}
					bool moved = false;
					foreach(Tile neighbor in t.PassableNeighborsBetween(row,col)){
						if(neighbor.passable && !moved){
							B.Add("You tumble. ");
							Move(t.row,t.col);
							moved = true;
							attrs[AttrType.TUMBLING]++;
						}
					}
					if(moved){
						Q.Add(new Event(this,Speed() + 100,EventType.MOVE));
						return true;
					}
					else{
						B.Add("The way is blocked! ");
						return false;
					}
				}
				else{
					if(MovementPrevented(t)){
						B.Add("You can't currently reach that spot. ");
					}
					return false;
				}
			}
			case FeatType.DISARM_TRAP:
			{
				int dir = GetDirection("Disarm which trap? ");
				Tile t = TileInDirection(dir);
				if(dir != -1 && t.IsKnownTrap()){
					if(ActorInDirection(dir) != null){
						B.Add("There is " + ActorInDirection(dir).AName(true) + " in the way. ");
						Q0();
						return true;
					}
					if(t.name.Contains("(safe)")){
						B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
						t.Toggle(this);
					}
					else{
						B.Add("You make " + Tile.Prototype(t.type).the_name + " safe to cross. ");
						t.SetName(Tile.Prototype(t.type).name + " (safe)");
					}
					Q1();
				}
				else{
					Q0();
				}
				return true;
			}
			default:
				return false;
			}
		}
		public void Interrupt(){
			if(HasAttr(AttrType.RESTING)){
				attrs[AttrType.RESTING] = 0;
			}
			attrs[AttrType.RUNNING] = 0;
			attrs[AttrType.WAITING] = 0;
			if(path != null && path.Count > 0){
				if(this == player && !HasAttr(AttrType.AUTOEXPLORE)){
					interrupted_path = path.Last();
				}
				path.Clear();
			}
			attrs[AttrType.AUTOEXPLORE] = 0;
		}
		public bool NextStepIsDangerous(Tile next){ //todo: add vents and geysers to GetCost and GetDangerRating?
			if(HasAttr(AttrType.BLIND) && next != tile()){
				return true;
			}
			if(HasAttr(AttrType.BURNING,AttrType.POISONED,AttrType.ACIDIFIED)){
				return true;
			}
			if(HasAttr(AttrType.BLEEDING) && !HasAttr(AttrType.BANDAGED,AttrType.NONLIVING)){
				return true;
			}
			if(next.IsKnownTrap() && !next.name.Contains("safe") && (!HasAttr(AttrType.FLYING) || HasAttr(AttrType.DESCENDING))){
				return true;
			}
			if(GetDangerRating(next) > 0){
				return true;
			}
			if(next.Is(FeatureType.FORASECT_EGG,FeatureType.STABLE_TELEPORTAL,FeatureType.TELEPORTAL,FeatureType.CONFUSION_GAS,FeatureType.THICK_DUST)){
				return true; //todo: remove some of these once they're added to GetDangerRating
			}
			foreach(Tile t in next.TilesWithinDistance(3)){ //could calculate and store these to prevent checking so many tiles each step.
				if(t.Is(TileType.POISON_GAS_VENT,TileType.FIRE_GEYSER) && t.seen && next.HasLOE(t)){
					return true;
				}
				if(t.inv != null && t.inv.type == ConsumableType.BLAST_FUNGUS && t.seen && next.HasLOE(t)){
					return true;
				}
			}
			foreach(Actor a in M.AllActors()){
				if(a != this && (!a.Is(ActorType.CARNIVOROUS_BRAMBLE,ActorType.MUD_TENTACLE) || next.DistanceFrom(a) <= 1) && CanSee(a) && HasLOS(a)){
					return true;
				}
			}
			return false;
		}
		public int GetDangerRating(Tile t){ //0 is no danger. 1 is minor danger, often ignored in favor of chasing the player. 2 is major danger, always avoided.
			if(HasAttr(AttrType.MINDLESS) || (type == ActorType.BERSERKER && HasAttr(AttrType.COOLDOWN_2)) || type == ActorType.FINAL_LEVEL_CULTIST){
				return 0;
			}
			int dr = attrs[AttrType.DAMAGE_RESISTANCE];
			int result = 0;
			if(HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
				if(!HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
					if(t.Is(FeatureType.CONFUSION_GAS) && !HasAttr(AttrType.MENTAL_IMMUNITY)){
						return 2;
					}
					if(t.Is(FeatureType.SPORES) && (!HasAttr(AttrType.MENTAL_IMMUNITY) || dr < 2)){
						if(HasAttr(AttrType.MENTAL_IMMUNITY,AttrType.STUNNED) && (HasAttr(AttrType.POISONED) || dr > 0)){
							result = 1;
						}
						else{
							return 2;
						}
					}
					if(t.Is(FeatureType.POISON_GAS) && type != ActorType.NOXIOUS_WORM && dr < 2){
						if(HasAttr(AttrType.POISONED) || dr > 0){
							result = 1;
						}
						else{
							return 2;
						}
					}
					/*if(t.Is(TileType.POPPY_FIELD) && !HasAttr(AttrType.MAGICAL_DROWSINESS,AttrType.MENTAL_IMMUNITY)){ //todo
						int counter = attrs[AttrType.POPPY_COUNTER] + M.poppy_distance_map[t.p]; //todo: check for valid dijkstra int here?
						if(counter > 3){
							return 2;
						}
						if(counter > 1){
							result = 1;
						}
					}*/
					if(t.Is(TileType.POPPY_FIELD) && !HasAttr(AttrType.MENTAL_IMMUNITY)){
						if(M.poppy_distance_map[t.p] == 1){ //the outermost layer is considered less dangerous
							if(attrs[AttrType.POPPY_COUNTER] >= 3){
								return 2;
							}
							else{
								if(this != player){
									//result = 1;
								}
								//result = 1;
							}
						}
						else{
							if(M.poppy_distance_map[t.p].IsValidDijkstraValue()){
								if(attrs[AttrType.POPPY_COUNTER] >= 2){
									return 2;
								}
								else{
									if(this != player){
										result = 1;
									}
									//result = 1;
								}
							}
						}
					}
				}
				if(t.Is(FeatureType.THICK_DUST) && !HasAttr(AttrType.BLINDSIGHT,AttrType.PLANTLIKE)){
					return 2;
				}
				foreach(Tile neighbor in t.TilesWithinDistance(1)){
					if(neighbor.Is(FeatureType.GRENADE)){
						return 2;
					}
				}
			}
			if(!HasAttr(AttrType.IMMUNE_FIRE) && dr < 3){
				bool might_catch = false;
				int searing_tiles = 0;
				bool currently_flammable = (t.IsCurrentlyFlammable() || HasAttr(AttrType.OIL_COVERED)) && HasAttr(AttrType.HUMANOID_INTELLIGENCE);
				foreach(Tile neighbor in t.TilesWithinDistance(1)){
					if(neighbor.IsBurning()){
						searing_tiles++;
						if(currently_flammable){
							might_catch = true;
						}
					}
				}
				if(dr == 0){
					if(searing_tiles > 3){
						return 2;
					}
					else{
						if(searing_tiles > 0){
							if(type == ActorType.ALASI_SCOUT && curhp == maxhp){
								return 2;
							}
							result = 1;
						}
					}
				}
				if((might_catch || t.IsBurning()) && !HasAttr(AttrType.IMMUNE_BURNING)){
					if(dr == 2){
						result = 1;
					}
					else{
						return 2;
					}
				}
			}
			return result;
		}
		/*public int GetDangerRating(Tile t){
			if(HasAttr(AttrType.MINDLESS) || (type == ActorType.BERSERKER && HasAttr(AttrType.COOLDOWN_2)) || type == ActorType.FINAL_LEVEL_CULTIST){
				return 0;
			}
			int total = 0;
			int dr = attrs[AttrType.DAMAGE_RESISTANCE];
			if(!HasAttr(AttrType.IMMUNE_BURNING,AttrType.IMMUNE_FIRE) && dr < 3){
				bool might_catch = false;
				foreach(Tile neighbor in t.TilesWithinDistance(1)){
					if(neighbor.IsBurning()){
						if(dr == 0){
							++total;
						}
						if(t.IsCurrentlyFlammable() && HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
							might_catch = true;
						}
					}
				}
				if(t.IsBurning()){
					if(IsBurning()){
						total += 3;
					}
					else{
						if(dr == 0){ //this DR handling is not very good! can be improved later.
							total += 20;
						}
						else{
							total += 15;
						}
					}
				}
				else{
					if(might_catch){
						if(dr == 0){
							total += 15;
						}
						else{
							total += 10;
						}
					}
				}
			}
			if(HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
				if(!HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
					if(t.Is(FeatureType.SPORES)){
						if(!HasAttr(AttrType.MENTAL_IMMUNITY)){
							if(HasAttr(AttrType.STUNNED)){
								++total;
							}
							else{
								total += 4;
							}
						}
						if(dr < 2){
							if(HasAttr(AttrType.POISONED)){
								++total;
							}
							else{
								if(dr > 0){
									total += 2;
								}
								else{
									total += 4;
								}
							}
						}
					}
					if(t.Is(FeatureType.POISON_GAS) && type != ActorType.NOXIOUS_WORM && dr < 2){
						if(HasAttr(AttrType.POISONED)){
							++total;
						}
						else{
							if(dr > 0){
								total += 2;
							}
							else{
								total += 4;
							}
						}
					}
					if(t.Is(FeatureType.CONFUSION_GAS) && !HasAttr(AttrType.MENTAL_IMMUNITY)){
						total += 15;
					}
					if(t.Is(TileType.POPPY_FIELD) && !HasAttr(AttrType.MAGICAL_DROWSINESS,AttrType.MENTAL_IMMUNITY) && attrs[AttrType.POPPY_COUNTER] + M.poppy_distance_map[t.p] >= 4){
						total += 7; //todo: check for valid dijkstra int here
					}
				}
				if(t.Is(FeatureType.THICK_DUST) && !HasAttr(AttrType.BLINDSIGHT,AttrType.PLANTLIKE)){
					total += 8;
				}
				if(t.Is(FeatureType.GRENADE)){
					total += 20;
				}
				foreach(Tile neighbor in t.TilesAtDistance(1)){
					if(neighbor.Is(FeatureType.GRENADE)){
						total += 20;
					}
				}
			}
			return total;
		}*/
		public bool CollideWith(Tile t){
			if(t.Is(TileType.FIREPIT) && !t.Is(FeatureType.SLIME)){
				B.Add(You("fall") + " into the fire pit. ",this);
				ApplyBurning();
			}
			if(IsBurning()){
				t.ApplyEffect(DamageType.FIRE);
			}
			if(t.Is(FeatureType.SLIME) && !HasAttr(AttrType.SLIMED)){
				B.Add(YouAre() + " covered in slime. ",this);
				attrs[AttrType.SLIMED] = 1;
				attrs[AttrType.OIL_COVERED] = 0;
				RefreshDuration(AttrType.BURNING,0);
				if(this == player){
					Help.TutorialTip(TutorialTopic.Slimed);
				}
			}
			else{
				if(t.Is(FeatureType.OIL) && !HasAttr(AttrType.SLIMED,AttrType.OIL_COVERED)){
					B.Add(YouAre() + " covered in oil. ",this);
					attrs[AttrType.OIL_COVERED] = 1;
					if(this == player){
						Help.TutorialTip(TutorialTopic.Oiled);
					}
				}
				else{
					if(t.IsBurning()){
						ApplyBurning();
					}
				}
			}
			if(!HasAttr(AttrType.SMALL)){
				t.ApplyEffect(DamageType.NORMAL);
			}
			return !HasAttr(AttrType.CORPSE);
		}
		public bool StunnedThisTurn(){
			if(HasAttr(AttrType.STUNNED) && R.OneIn(3)){
				if(HasAttr(AttrType.IMMOBILE)){
					B.Add(YouAre() + " momentarily stunned. ",this);
					QS();
					return true;
				}
				Stagger();
				return true;
			}
			return false;
		}
		public bool FrozenThisTurn(){ //for the player only - monster freezing is handled elsewhere
			if(HasAttr(AttrType.FROZEN)){
				if(HasAttr(AttrType.BRUTISH_STRENGTH)){
					attrs[AttrType.FROZEN] = 0;
					B.Add("You smash through the ice! ");
					Q0();
					return true;
				}
				else{
					int damage = R.Roll(EquippedWeapon.Attack().damage.dice,6) + TotalSkill(SkillType.COMBAT);
					attrs[AttrType.FROZEN] -= damage;
					if(attrs[AttrType.FROZEN] < 0){
						attrs[AttrType.FROZEN] = 0;
					}
					if(HasAttr(AttrType.FROZEN)){
						B.Add("You attempt to break free. ");
					}
					else{
						B.Add("You break free! ");
					}
					IncreaseExhaustion(1);
					Q1();
					return true;
				}
			}
			return false;
		}
		public List<string> InventoryList(){
			List<string> result = new List<string>();
			foreach(Item i in inv){
				result.Add(i.AName());
			}
			return result;
		}
		public static List<colorstring> ItemDescriptionBox(Item item,bool lookmode,bool mouselook,int max_string_length){
			List<string> text = item.Description().GetWordWrappedList(max_string_length);
			Color box_edge_color = Color.DarkGreen;
			Color box_corner_color = Color.Green;
			Color text_color = Color.Gray;
			int widest = 31; // length of "[Press any other key to cancel]"
			if(lookmode){
				widest = 20; // length of "[=] Hide description"
			}
			foreach(string s in text){
				if(s.Length > widest){
					widest = s.Length;
				}
			}
			if((!lookmode || mouselook) && item.Name(true).Length > widest){
				widest = item.Name(true).Length;
			}
			widest += 2; //one space on each side
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			if(!lookmode || mouselook){
				box.Add(new colorstring("|",box_edge_color) + item.Name(true).PadOuter(widest).GetColorString(Color.White) + new colorstring("|",box_edge_color));
				box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
			}
			foreach(string s in text){
				box.Add(new colorstring("|",box_edge_color) + s.PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			if(!mouselook){
				box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
				if(lookmode){
					box.Add(new colorstring("|",box_edge_color) + "[=] Hide description".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
				}
				else{
					box.Add(new colorstring("|",box_edge_color) + "[a]pply  [f]ling  [d]rop".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
					//box.Add(new colorstring("|",box_edge_color) + "[Press any other key to cancel]".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
				}
			}
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			return box;
		}
		public void DisplayStats(){ DisplayStats(false); }
		public void DisplayStats(bool cyan_letters){
			bool buttons = MouseUI.AutomaticButtonsFromStrings;
			MouseUI.AutomaticButtonsFromStrings = false;
			Screen.CursorVisible = false;
			if(!viewing_map_shrine_info){
				Screen.WriteStatsString(2,0,"HP: ");
				if(curhp < 50){
					if(curhp < 20){
						Screen.WriteStatsString(2,4,new cstr(Color.DarkRed,curhp.ToString() + "  "));
					}
					else{
						Screen.WriteStatsString(2,4,new cstr(Color.Red,curhp.ToString() + "  "));
					}
				}
				else{
					Screen.WriteStatsString(2,4,curhp.ToString() + "  ");
				}
				Screen.WriteStatsString(3,0,"Mana: ");
				Screen.WriteStatsString(3,6,curmp + "  ");
				Screen.WriteStatsString(4,0,"Exhaust:");
				if(exhaustion == 100){
					Screen.WriteStatsString(4,8,"100%");
				}
				else{
					Screen.WriteStatsString(4,8," " + (exhaustion.ToString() + "%").PadRight(4));
				}
				Screen.WriteStatsString(5,0,"Depth: " + M.current_level + "  ");
				Screen.WriteStatsString(6,0,"            ");
				cstr cs = EquippedWeapon.StatsName();
				cs.s = cs.s.PadRight(12);
				Screen.WriteStatsString(7,0,cs);
				colorstring statuses = new colorstring();
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(EquippedWeapon.status[(EquipmentStatus)i]){
						statuses.strings.Add(new cstr("*",Weapon.StatusColor((EquipmentStatus)i)));
						if(EquippedWeapon.StatsName().s.Length + statuses.Length() >= 11){
							break;
						}
					}
				}
				Screen.WriteString(7,EquippedWeapon.StatsName().s.Length + 1,statuses);
				cs = EquippedArmor.StatsName();
				cs.s = cs.s.PadRight(12);
				Screen.WriteStatsString(8,0,cs);
				statuses = new colorstring();
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					if(EquippedArmor.status[(EquipmentStatus)i]){
						statuses.strings.Add(new cstr("*",Weapon.StatusColor((EquipmentStatus)i)));
						if(EquippedArmor.StatsName().s.Length + statuses.Length() >= 11){
							break;
						}
					}
				}
				Screen.WriteString(8,EquippedArmor.StatsName().s.Length + 1,statuses);
				Screen.WriteStatsString(9,0,"            ");
				if(HasAttr(AttrType.BURNING)){
					Screen.WriteStatsString(10,0,"Burning",Color.Red);
				}
				else{
					if(HasAttr(AttrType.SLIMED)){
						Screen.WriteStatsString(10,0,"Slimed ",Color.Green);
					}
					else{
						if(HasAttr(AttrType.OIL_COVERED)){
							Screen.WriteStatsString(10,0,"Oiled  ",Color.DarkYellow);
						}
						else{
							if(HasAttr(AttrType.FROZEN)){
								Screen.WriteStatsString(10,0,"Frozen ",Color.Blue);
							}
							else{
								if(tile().Is(FeatureType.WEB) && !HasAttr(AttrType.BURNING,AttrType.OIL_COVERED,AttrType.SLIMED)){
									Screen.WriteStatsString(10,0,"Webbed ",Color.White);
								}
								else{
									Screen.WriteStatsString(10,0,"       ");
								}
							}
						}
					}
				}
			}
			else{
				Color[] colors = new Color[5];
				string[] shrines = new string[]{"  Combat    ","  Defense   ","  Magic     ","  Spirit    ","  Stealth   "};
				for(int i=0;i<5;++i){
					if(Map.shrine_locations[i].BoundsCheck(M.tile,true) && M.tile[Map.shrine_locations[i]].seen){
						if(M.tile[Map.shrine_locations[i]].type == TileType.RUINED_SHRINE){
							colors[i] = Color.DarkGray;
						}
						else{
							colors[i] = Color.Gray;
						}
					}
					else{
						colors[i] = Color.DarkGray;
						//shrines[i] = "    ---     ";
						shrines[i] = "  -------   ";
					}
				}
				Screen.WriteStatsString(2,0,"            ");
				Screen.WriteStatsString(3,0,"            ");
				Screen.WriteStatsString(4,0," -Shrines-  ",Color.Yellow);
				for(int i=0;i<5;++i){
					Screen.WriteStatsString(5+i,0,shrines[i],colors[i]);
				}
				/*Screen.WriteStatsString(5,0,"  Combat    ",colors[0]);
				Screen.WriteStatsString(6,0,"  Defense   ",colors[1]);
				Screen.WriteStatsString(7,0,"  Magic     ",colors[2]);
				Screen.WriteStatsString(8,0,"  Spirit    ",colors[3]);
				Screen.WriteStatsString(9,0,"  Stealth   ",colors[4]);*/
				Screen.WriteStatsString(10,0,"            ");
			}
			string[] commandhints;
			List<int> blocked_commands = new List<int>();
			if(viewing_more_commands){
				commandhints = new string[]{"[o]perate   ","[w]alk      ","Travel [X]  ","Wait [.]    ","Descend [>] ",
					"[m]ap       ","Known [\\]   ","Options [=] ","[q]uit      ","            ","            ",
					"            ","[v]iew more "};
			}
			else{
				commandhints = new string[]{"[i]nventory ","[e]quipment ","[c]haracter ","[t]orch     ",
					"Look [Tab]  ","[r]est      ","[a]pply item","[g]et item  ","[f]ling item","[s]hoot bow ",
					"Cast [z]    ","E[x]plore   ","[v]iew more "};
				if(attrs[AttrType.RESTING] == -1){
					blocked_commands.Add(5);
				}
				if(M.wiz_dark || M.wiz_lite){
					blocked_commands.Add(3);
				}
			}
			Color wordcolor = cyan_letters? Color.Gray : Color.DarkGray;
			Color lettercolor = cyan_letters? Color.Cyan : Color.DarkCyan;
			for(int i=0;i<commandhints.Length;++i){
				if(blocked_commands.Contains(i)){
					Screen.WriteString(12+i,0,commandhints[i].GetColorString(Color.DarkGray,Color.DarkCyan));
				}
				else{
					Screen.WriteString(12+i,0,commandhints[i].GetColorString(wordcolor,lettercolor));
				}
			}
			Screen.ResetColors();
			MouseUI.AutomaticButtonsFromStrings = buttons;
		}
		public int DisplayCharacterInfo(){ return DisplayCharacterInfo(true); }
		public int DisplayCharacterInfo(bool readkey){
			MouseUI.PushButtonMap();
			DisplayStats();
			for(int i=1;i<ROWS-1;++i){
				Screen.WriteMapString(i,0,"".PadRight(COLS));
			}
			Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
			Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
			Color catcolor = Color.Green;
			string s = ("Name: " + player_name).PadRight(COLS/2) + "Turns played: " + (Q.turn / 100);
			Screen.WriteMapString(2,0,s);
			Screen.WriteMapString(2,0,new cstr(catcolor,"Name"));
			Screen.WriteMapString(2,COLS/2,new cstr(catcolor,"Turns played"));
			Screen.WriteMapString(5,0,"Skills:");
			Screen.WriteMapString(5,0,new cstr(catcolor,"Skills"));
			int pos = 7;
			for(SkillType sk = SkillType.COMBAT;sk < SkillType.NUM_SKILLS;++sk){
				if(sk == SkillType.STEALTH && pos > 50){
					Screen.WriteMapString(6,8,"Stealth(" + skills[SkillType.STEALTH].ToString());
					pos = 16 + skills[SkillType.STEALTH].ToString().Length;
					int bonus = attrs[AttrType.BONUS_STEALTH] - EquippedArmor.StealthPenalty();
					if(bonus != 0){
						if(bonus > 0){
							Screen.WriteMapString(6,pos,new cstr(Color.Yellow,"+" + bonus.ToString()));
							pos += bonus.ToString().Length + 1;
						}
						else{
							Screen.WriteMapString(6,pos,new cstr(Color.Blue,bonus.ToString()));
							pos += bonus.ToString().Length;
						}
					}
					Screen.WriteMapChar(6,pos,')');
				}
				else{
					Screen.WriteMapString(5,pos," " + Skill.Name(sk));
					pos += Skill.Name(sk).Length + 1;
					string count1 = skills[sk].ToString();
					string count2;
					switch(sk){
					case SkillType.COMBAT:
						count2 = attrs[AttrType.BONUS_COMBAT].ToString();
						break;
					case SkillType.DEFENSE:
						count2 = (attrs[AttrType.BONUS_DEFENSE] + TotalProtectionFromArmor()).ToString();
						break;
					case SkillType.MAGIC:
						count2 = attrs[AttrType.BONUS_MAGIC].ToString();
						break;
					case SkillType.SPIRIT:
						count2 = attrs[AttrType.BONUS_SPIRIT].ToString();
						break;
					case SkillType.STEALTH:
						count2 = (attrs[AttrType.BONUS_STEALTH] - EquippedArmor.StealthPenalty()).ToString();
						break;
					default:
						count2 = "error";
						break;
					}
					Screen.WriteMapString(5,pos,"(" + count1);
					pos += count1.Length + 1;
					if(count2 != "0"){
						if(sk == SkillType.STEALTH && attrs[AttrType.BONUS_STEALTH] - EquippedArmor.StealthPenalty() < 0){
							Screen.WriteMapString(5,pos,new cstr(Color.Blue,count2));
							pos += count2.Length;
						}
						else{
							Screen.WriteMapString(5,pos,new cstr(Color.Yellow,"+" + count2));
							pos += count2.Length + 1;
						}
					}
					Screen.WriteMapChar(5,pos,')');
					pos++;
				}
			}
			Screen.WriteMapString(8,0,"Feats: ");
			Screen.WriteMapString(8,0,new cstr(catcolor,"Feats"));
			string featlist = "";
			int active_feat_count = 0;
			foreach(FeatType f in feats_in_order){
				if(featlist.Length > 0){
					featlist = featlist + ", ";
				}
				if(Feat.IsActivated(f)){
					featlist = featlist + "[" + (char)(active_feat_count + 'a') + "] " + Feat.Name(f);
					++active_feat_count;
				}
				else{
					featlist = featlist + Feat.Name(f);
				}
			}
			MouseUI.AutomaticButtonsFromStrings = true;
			int currentrow = 8;
			while(featlist.Length > COLS-7){
				int currentcol = COLS-8;
				while(featlist[currentcol] != ','){
					--currentcol;
				}
				Screen.WriteString(currentrow + Global.MAP_OFFSET_ROWS,7 + Global.MAP_OFFSET_COLS,featlist.Substring(0,currentcol+1).GetColorString());
				//Screen.WriteMapString(currentrow,7,featlist.Substring(0,currentcol+1).GetColorString());
				featlist = featlist.Substring(currentcol+2);
				++currentrow;
			}
			Screen.WriteString(currentrow + Global.MAP_OFFSET_ROWS,7 + Global.MAP_OFFSET_COLS,featlist.GetColorString());
			MouseUI.AutomaticButtonsFromStrings = false;
			Screen.WriteMapString(11,0,"Spells: ");
			Screen.WriteMapString(11,0,new cstr(catcolor,"Spells"));
			string spelllist = "";
			for(SpellType sp = SpellType.RADIANCE;sp < SpellType.NUM_SPELLS;++sp){
				if(HasSpell(sp)){
					if(spelllist.Length == 0){ //if this is the first one...
						spelllist = spelllist + Spell.Name(sp);
					}
					else{
						spelllist = spelllist + ", " + Spell.Name(sp);
					}
				}
			}
			currentrow = 11;
			while(spelllist.Length > COLS-8){
				int currentcol = COLS-9;
				while(spelllist[currentcol] != ','){
					--currentcol;
				}
				Screen.WriteMapString(currentrow,8,spelllist.Substring(0,currentcol+1));
				spelllist = spelllist.Substring(currentcol+2);
				++currentrow;
			}
			Screen.WriteMapString(currentrow,8,spelllist);
			Screen.WriteMapString(14,0,"Magical equipment: ");
			Screen.WriteMapString(14,0,new cstr(catcolor,"Magical equipment"));
			string equipmentlist = "";
			foreach(Weapon w in weapons){
				if(w.enchantment != EnchantmentType.NO_ENCHANTMENT){
					string weapon_name = w.NameWithEnchantment().ToUpper()[0] + w.NameWithEnchantment().Substring(1);
					if(equipmentlist.Length == 0){
						equipmentlist = equipmentlist + weapon_name;
					}
					else{
						equipmentlist = equipmentlist + ", " + weapon_name;
					}
				}
			}
			foreach(MagicTrinketType trinket in magic_trinkets){
				string trinket_name = MagicTrinket.Name(trinket).ToUpper()[0] + MagicTrinket.Name(trinket).Substring(1);
				if(equipmentlist.Length == 0){
					equipmentlist = equipmentlist + trinket_name;
				}
				else{
					equipmentlist = equipmentlist + ", " + trinket_name;
				}
			}
			currentrow = 14;
			if(equipmentlist.Length > COLS-19){
				int currentcol = COLS-20;
				while(equipmentlist[currentcol] != ','){
					--currentcol;
				}
				Screen.WriteMapString(currentrow,19,equipmentlist.Substring(0,currentcol+1));
				equipmentlist = equipmentlist.Substring(currentcol+2);
				++currentrow;
			}
			while(equipmentlist.Length > COLS-2){
				int currentcol = COLS-3;
				while(equipmentlist[currentcol] != ','){
					--currentcol;
				}
				Screen.WriteMapString(currentrow,2,equipmentlist.Substring(0,currentcol+1));
				equipmentlist = equipmentlist.Substring(currentcol+2);
				++currentrow;
				if(currentrow == ROWS){
					break;
				}
			}
			if(currentrow == 14){
				Screen.WriteMapString(currentrow,19,equipmentlist);
			}
			else{
				if(currentrow != ROWS){
					Screen.WriteMapString(currentrow,2,equipmentlist);
				}
			}
			Screen.ResetColors();
			B.DisplayNow("Character information: ");
			Screen.CursorVisible = true;
			int num_active_feats = 0;
			foreach(FeatType feat in Enum.GetValues(typeof(FeatType))){
				if(HasFeat(feat) && Feat.IsActivated(feat)){
					++num_active_feats;
				}
			}
			if(readkey){
				int result = GetSelection("Character information: ",num_active_feats,false,true,false);
				MouseUI.PopButtonMap();
				return result;
				//Global.ReadKey();
			}
			else{
				MouseUI.PopButtonMap();
				return -1;
			}
		}
		public int[] DisplayEquipment(){
			MouseUI.PushButtonMap();
			WeaponType new_weapon_type = EquippedWeapon.type;
			ArmorType new_armor_type = EquippedArmor.type;
			int selected_magic_trinket_idx = -1;
			if(magic_trinkets.Count > 0){
				selected_magic_trinket_idx = R.Roll(magic_trinkets.Count)-1;
				int i = 0;
				foreach(MagicTrinketType trinket in magic_trinkets){
					MouseUI.CreateButton((ConsoleKey)(ConsoleKey.I + i),false,i+1+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS + 32,1,34);
					++i;
				}
			}
			Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
			for(int i=1;i<ROWS-1;++i){
				Screen.WriteMapString(i,0,"".PadRight(COLS));
			}
			int line = 1;
			for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
				Screen.WriteMapString(line,6,WeaponOfType(w).EquipmentScreenName());
				ConsoleKey key = (ConsoleKey)(ConsoleKey.A + line-1);
				if(w == new_weapon_type){
					key = ConsoleKey.Enter;
				}
				if(magic_trinkets.Count >= line){
					MouseUI.CreateButton(key,false,line+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,1,32);
				}
				else{
					MouseUI.CreateMapButton(key,false,line+Global.MAP_OFFSET_ROWS,1);
				}
				++line;
			}
			line = 8;
			for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
				Screen.WriteMapString(line,6,ArmorOfType(a).EquipmentScreenName());
				ConsoleKey key = (ConsoleKey)(ConsoleKey.A + line-3);
				if(a == new_armor_type){
					key = ConsoleKey.Enter;
				}
				if(magic_trinkets.Count >= line){
					MouseUI.CreateButton(key,false,line+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS,1,32);
				}
				else{
					MouseUI.CreateMapButton(key,false,line+Global.MAP_OFFSET_ROWS,1);
				}
				++line;
			}
			line = 1;
			foreach(MagicTrinketType m in magic_trinkets){
				string s = MagicTrinket.Name(m);
				Screen.WriteMapString(line,38,s[0].ToString().ToUpper() + s.Substring(1));
				++line;
			}
			Screen.WriteMapString(12,0,new cstr(Color.DarkRed,"Weapon: "));
			Screen.WriteMapChar(12,6,':');
			Screen.WriteMapString(16,0,new cstr(Color.DarkCyan,"Armor: "));
			Screen.WriteMapChar(16,5,':');
			Screen.WriteMapString(19,0,new cstr(Color.DarkGreen,"Magic trinket: "));
			Screen.WriteMapChar(19,13,':');
			Screen.WriteMapString(11,0,"".PadRight(COLS,'-'));
			ConsoleKeyInfo command;
			bool done = false;
			while(!done){
				Weapon new_weapon = WeaponOfType(new_weapon_type);
				Armor new_armor = ArmorOfType(new_armor_type);
				line = 1;
				for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
					if(new_weapon_type == w){
						Screen.WriteMapChar(line,0,'>');
						Screen.WriteMapString(line,2,new colorstring("[",Color.Gray,((char)(w+(int)'a')).ToString(),Color.Red,"]",Color.Gray));
						//Screen.WriteMapString(line,2,new cstr(Color.Red,"[" + (char)(w+(int)'a') + "]"));
					}
					else{
						Color letter_color = Color.Cyan;
						if(EquippedWeapon.status[EquipmentStatus.STUCK]){
							letter_color = Color.Red;
						}
						Screen.WriteMapChar(line,0,' ');
						Screen.WriteMapString(line,2,new colorstring("[",Color.Gray,((char)(w+(int)'a')).ToString(),letter_color,"]",Color.Gray));
						//Screen.WriteMapString(line,2,new cstr(letter_color,"[" + (char)(w+(int)'a') + "]"));
					}
					++line;
				}
				line = 8;
				for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
					if(new_armor_type == a){
						Screen.WriteMapChar(line,0,'>');
						Screen.WriteMapString(line,2,new colorstring("[",Color.Gray,((char)(a+(int)'f')).ToString(),Color.Red,"]",Color.Gray));
						//Screen.WriteMapString(line,2,new cstr(Color.Red,"[" + (char)(a+(int)'f') + "]"));
					}
					else{
						Color letter_color = Color.Cyan;
						if(EquippedArmor.status[EquipmentStatus.STUCK]){
							letter_color = Color.Red;
						}
						Screen.WriteMapChar(line,0,' ');
						Screen.WriteMapString(line,2,new colorstring("[",Color.Gray,((char)(a+(int)'f')).ToString(),letter_color,"]",Color.Gray));
						//Screen.WriteMapString(line,2,new cstr(letter_color,"[" + (char)(a+(int)'f') + "]"));
					}
					++line;
				}
				line = 1;
				int letter = 0;
				foreach(MagicTrinketType m in magic_trinkets){
					if(selected_magic_trinket_idx == magic_trinkets.IndexOf(m)){
						Screen.WriteMapChar(line,32,'>');
					}
					else{
						Screen.WriteMapChar(line,32,' ');
					}
					Screen.WriteMapString(line,34,new colorstring("[",Color.Gray,((char)(letter+(int)'i')).ToString(),Color.Red,"]",Color.Gray));
					//Screen.WriteMapString(line,34,new cstr(Color.Red,"[" + (char)(letter+(int)'i') + "]"));
					++line;
					++letter;
				}
				Screen.WriteMapString(12,8,new_weapon.Description()[0].PadRight(COLS));
				Screen.WriteMapString(13,0,new_weapon.Description()[1].PadRight(COLS));
				colorstring weaponstatus = new colorstring();
				colorstring armorstatus = new colorstring();
				cstr weaponstatusdescription = new cstr();
				cstr armorstatusdescription = new cstr();
				int weaponstatuscount = 0;
				int armorstatuscount = 0;
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					EquipmentStatus st = (EquipmentStatus)i;
					if(new_weapon.status[st]){
						weaponstatus.strings.Add(new cstr(Weapon.StatusName(st) + "  ",Weapon.StatusColor(st)));
						weaponstatusdescription = new cstr(Weapon.StatusDescription(st).PadRight(COLS),Weapon.StatusColor(st));
						++weaponstatuscount;
					}
					if(new_armor.status[st]){
						armorstatus.strings.Add(new cstr(Weapon.StatusName(st) + "  ",Weapon.StatusColor(st)));
						armorstatusdescription = new cstr(Weapon.StatusDescription(st).PadRight(COLS),Weapon.StatusColor(st));
						++armorstatuscount;
					}
				}
				int ws_length = weaponstatus.Length();
				if(ws_length < COLS && ws_length > 0){
					cstr cs = weaponstatus.strings.Last();
					string s = cs.s;
					s = s.Substring(0,s.Length-2); //remove the last 2 spaces
					ws_length -= 2;
					weaponstatus.strings[weaponstatus.strings.Count-1] = new cstr(s,cs.color);
					int left_half = ((COLS - ws_length) / 2) - 2;
					int right_half = (COLS - ws_length) - left_half;
					weaponstatus.strings.Insert(0,new cstr("".PadRight(left_half),Color.Gray));
					weaponstatus.strings.Add(new cstr("".PadRight(right_half),Color.Gray));
				}
				int as_length = armorstatus.Length();
				if(as_length < COLS && as_length > 0){
					cstr cs = armorstatus.strings.Last();
					string s = cs.s;
					s = s.Substring(0,s.Length-2); //remove the last 2 spaces
					as_length -= 2;
					armorstatus.strings[armorstatus.strings.Count-1] = new cstr(s,cs.color);
					int left_half = ((COLS - as_length) / 2) - 2;
					int right_half = (COLS - as_length) - left_half;
					armorstatus.strings.Insert(0,new cstr("".PadRight(left_half),Color.Gray));
					armorstatus.strings.Add(new cstr("".PadRight(right_half),Color.Gray));
				}
				if(new_weapon.enchantment != EnchantmentType.NO_ENCHANTMENT){
					Screen.WriteMapString(14,1,new_weapon.DescriptionOfEnchantment().PadRight(COLS),new_weapon.EnchantmentColor());
					if(weaponstatuscount == 1){
						Screen.WriteMapString(15,1,weaponstatusdescription);
					}
					else{
						Screen.WriteMapString(15,1,weaponstatus);
					}
				}
				else{
					if(weaponstatuscount == 1){
						Screen.WriteMapString(14,1,weaponstatusdescription);
					}
					else{
						Screen.WriteMapString(14,1,weaponstatus);
					}
					Screen.WriteMapString(15,1,"".PadRight(COLS));
				}
				Screen.WriteMapString(16,7,new_armor.Description()[0].PadRight(COLS));
				Screen.WriteMapString(17,0,new_armor.Description()[1].PadRight(COLS));
				if(new_armor.enchantment != EnchantmentType.NO_ENCHANTMENT){
					Screen.WriteMapString(18,8,new_armor.DescriptionOfEnchantment().PadRight(COLS));
					if(armorstatuscount == 1){
						Screen.WriteMapString(19,1,armorstatusdescription);
					}
					else{
						Screen.WriteMapString(19,1,armorstatus);
					}
				}
				else{
					if(armorstatuscount == 1){
						Screen.WriteMapString(18,1,armorstatusdescription);
					}
					else{
						Screen.WriteMapString(18,1,armorstatus);
					}
				}
				if(selected_magic_trinket_idx >= 0){
					string[] magic_item_desc = MagicTrinket.Description(magic_trinkets[selected_magic_trinket_idx]);
					Screen.WriteMapString(19,15,magic_item_desc[0].PadRight(51));
					Screen.WriteMapString(20,15,magic_item_desc[1].PadRight(51));
				}
				else{
					Screen.WriteMapString(19,15,"(none)");
				}
				if(new_weapon == EquippedWeapon && new_armor == EquippedArmor){
					Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
				}
				else{
					if((new_weapon != EquippedWeapon && EquippedWeapon.status[EquipmentStatus.STUCK]) || (new_armor != EquippedArmor && EquippedArmor.status[EquipmentStatus.STUCK])){
						Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
						MouseUI.RemoveButton(Global.MAP_OFFSET_ROWS+ROWS-1,Global.MAP_OFFSET_COLS);
					}
					else{
						Screen.WriteMapString(ROWS-1,0,"[Enter] to confirm-----".PadLeft(43,'-'));
						Screen.WriteMapString(ROWS-1,21,new cstr(Color.Magenta,"Enter"));
						MouseUI.CreateMapButton(ConsoleKey.Enter,false,Global.MAP_OFFSET_ROWS+ROWS-1,1);
					}
				}
				Screen.ResetColors();
				B.DisplayNow("Your equipment: ");
				Screen.CursorVisible = true;
				command = Global.ReadKey();
				char ch = ConvertInput(command);
				switch(ch){
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case '!':
				case '@':
				case '#':
				case '$':
				case '%':
				{
					switch(ch){
					case '!':
						ch = 'a';
						break;
					case '@':
						ch = 'b';
						break;
					case '#':
						ch = 'c';
						break;
					case '$':
						ch = 'd';
						break;
					case '%':
						ch = 'e';
						break;
					}
					int num = (int)(ch - 'a');
					if(num != (int)(new_weapon_type)){
						MouseUI.GetButton((int)(new_weapon_type)+1+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = (ConsoleKey)(ConsoleKey.A + (int)new_weapon_type);
						MouseUI.GetButton(num+1+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = ConsoleKey.Enter;
						new_weapon_type = (WeaponType)num;
					}
					break;
				}
				case 'f':
				case 'g':
				case 'h':
				case '*':
				case '(':
				case ')':
				{
					switch(ch){
					case '*':
						ch = 'f';
						break;
					case '(':
						ch = 'g';
						break;
					case ')':
						ch = 'h';
						break;
					}
					int num = (int)(ch - 'f');
					if(num != (int)(new_armor_type)){
						MouseUI.GetButton((int)(new_armor_type)+8+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = (ConsoleKey)(ConsoleKey.F + (int)new_armor_type);
						MouseUI.GetButton(num+8+Global.MAP_OFFSET_ROWS,Global.MAP_OFFSET_COLS).key = ConsoleKey.Enter;
						new_armor_type = (ArmorType)num;
					}
					break;
				}
				case 'i':
				case 'j':
				case 'k':
				case 'l':
				case 'm':
				case 'n':
				case 'o':
				case 'p':
				case 'q':
				case 'r':
				{
					int num = (int)ch - (int)'i';
					if(num < magic_trinkets.Count && num != selected_magic_trinket_idx){
						selected_magic_trinket_idx = num;
					}
					break;
				}
				case (char)27:
				case ' ':
					new_weapon_type = EquippedWeapon.type; //reset
					new_armor_type = EquippedArmor.type;
					done = true;
					break;
				case (char)13:
					done = true;
					break;
				default:
					break;
				}
			}
			MouseUI.PopButtonMap();
			return new int[]{(int)new_weapon_type,(int)new_armor_type};
		}
		public void IncreaseSkill(SkillType skill){
			List<string> learned = new List<string>();
			skills[skill]++;
			bool active_feat_learned = false;
			B.Add("You feel a rush of power. ");
			//DisplayStats();
			B.PrintAll();
			ConsoleKeyInfo command;
			bool gain_feat = false;
			if(!M.feat_gained_this_level){
				int total = 0;
				foreach(FeatType ft in feats_in_order){
					if(Feat.Skill(ft) == skill){
						++total;
					}
				}
				if(total < 4){
					List<List<Tile>> shrines = new List<List<Tile>>{new List<Tile>{this.tile()}};
					foreach(Tile neighbor in tile().TilesAtDistance(2)){
						if(neighbor.IsShrine() && neighbor.type != TileType.SPELL_EXCHANGE_SHRINE){
							shrines[0].Add(neighbor);
							break;
						}
					}
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(M.tile[i,j].IsShrine() && M.tile[i,j].type != TileType.SPELL_EXCHANGE_SHRINE){
								bool found = false;
								foreach(List<Tile> l in shrines){
									foreach(Tile t in l){
										if(t == M.tile[i,j]){
											found = true;
											break;
										}
									}
									if(found){
										break;
									}
								}
								if(!found){
									shrines.Add(M.tile[i,j].TilesWithinDistance(2).Where(x=>x.IsShrine() && x.type != TileType.SPELL_EXCHANGE_SHRINE));
								}
							}
						}
					}
					List<List<Tile>> removed = new List<List<Tile>>();
					for(int idx = 1;idx < shrines.Count;++idx){
						foreach(Tile t in shrines[idx]){
							SkillType t_skill = SkillType.COMBAT + (t.type - TileType.COMBAT_SHRINE);
							int total2 = 0;
							foreach(FeatType ft in feats_in_order){
								if(Feat.Skill(ft) == t_skill){
									++total2;
								}
							}
							if(total2 >= 4){
								removed.Add(shrines[idx]);
								break;
							}
						}
					}
					foreach(List<Tile> l in removed){
						shrines.Remove(l);
					}
					List<Tile> chosen_shrines = shrines.Random();
					if(chosen_shrines.Contains(this.tile())){
						gain_feat = true;
					}
				}
			}
			if(gain_feat){
				M.feat_gained_this_level = true;
				FeatType feat_chosen = FeatType.NO_FEAT;
				bool done = false;
				MouseUI.PushButtonMap();
				for(int i=0;i<4;++i){
					MouseUI.CreateMapButton((ConsoleKey)(ConsoleKey.A + i),false,Global.MAP_OFFSET_ROWS + 1 + i*5,5);
				}
				MouseUI.CreateButton(ConsoleKey.Oem2,true,Global.MAP_OFFSET_ROWS + ROWS-1,45,1,12);
				while(!done){
					Screen.ResetColors();
					Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
					for(int i=0;i<4;++i){
						FeatType ft = Feat.OfSkill(skill,i);
						Color featcolor = (feat_chosen == ft)? Color.Green : Color.Gray;
						Color lettercolor = Color.Cyan;
						if(HasFeat(ft)){
							featcolor = Color.Magenta;
							lettercolor = Color.DarkRed;
						}
						Screen.WriteMapString(1+i*5,0,("["+(char)(i+97)+"] "));
						Screen.WriteMapChar(1+i*5,1,(char)(i+97),lettercolor);
						Screen.WriteMapString(1+i*5,4,Feat.Name(ft).PadRight(30),featcolor);
						if(Feat.IsActivated(ft)){
							Screen.WriteMapString(1+i*5,30,"        Active".PadToMapSize(),featcolor);
						}
						else{
							Screen.WriteMapString(1+i*5,30,"        Passive".PadToMapSize(),featcolor);
						}
						List<string> desc = Feat.Description(ft);
						for(int j=0;j<4;++j){
							if(desc.Count > j){
								Screen.WriteMapString(2+j+i*5,0,"    " + desc[j].PadRight(64),featcolor);
							}
							else{
								Screen.WriteMapString(2+j+i*5,0,"".PadRight(66));
							}
						}
					}
					if(feat_chosen != FeatType.NO_FEAT){
						Screen.WriteMapString(21,0,"--Type [a-d] to choose a feat---[?] for help---[Enter] to accept--");
						Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
						Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'d'));
						Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
						Screen.WriteMapString(21,48,new cstr(Color.Magenta,"Enter"));
						MouseUI.CreateButton(ConsoleKey.Enter,false,Global.MAP_OFFSET_ROWS + ROWS-1,60,1,17);
					}
					else{
						Screen.WriteMapString(21,0,"--Type [a-d] to choose a feat---[?] for help----------------------");
						Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
						Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'d'));
						Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
						MouseUI.RemoveButton(Global.MAP_OFFSET_ROWS + ROWS-1,60);
					}
					B.DisplayNow("Your " + Skill.Name(skill) + " skill increases to " + skills[skill] + ". Choose a feat: ");
					if(!Help.displayed[TutorialTopic.Feats]){
						Help.TutorialTip(TutorialTopic.Feats,true);
						B.DisplayNow("Your " + Skill.Name(skill) + " skill increases to " + skills[skill] + ". Choose a feat: ");
					}
					Screen.CursorVisible = true;
					command = Global.ReadKey();
					Screen.CursorVisible = false;
					char ch = ConvertInput(command);
					switch(ch){
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					{
						FeatType ft = Feat.OfSkill(skill,(int)(ch-97));
						int i = (int)(ch - 'a');
						if(feat_chosen == ft){
							feat_chosen = FeatType.NO_FEAT;
							MouseUI.RemoveButton(Global.MAP_OFFSET_ROWS + 1 + i*5,60);
							MouseUI.CreateMapButton((ConsoleKey)(ConsoleKey.A + i),false,Global.MAP_OFFSET_ROWS + 1 + i*5,5);
						}
						else{
							if(!HasFeat(ft)){
								if(feat_chosen != FeatType.NO_FEAT){
									int num = (int)feat_chosen % 4;
									MouseUI.RemoveButton(Global.MAP_OFFSET_ROWS + 1 + num*5,60);
									MouseUI.CreateMapButton((ConsoleKey)(ConsoleKey.A + num),false,Global.MAP_OFFSET_ROWS + 1 + num*5,5);
								}
								feat_chosen = ft;
								MouseUI.RemoveButton(Global.MAP_OFFSET_ROWS + 1 + i*5,60);
								MouseUI.CreateMapButton(ConsoleKey.Enter,false,Global.MAP_OFFSET_ROWS + 1 + i*5,5);
							}
						}
						break;
					}
					case '?':
						Help.DisplayHelp(HelpTopic.Feats);
						DisplayStats();
						break;
					case (char)13:
						if(feat_chosen != FeatType.NO_FEAT){
							done = true;
						}
						break;
					default:
						break;
					}
				}
				feats[feat_chosen] = true;
				feats_in_order.Add(feat_chosen);
				learned.Add("You master the " + Feat.Name(feat_chosen) + " feat. ");
				if(Feat.IsActivated(feat_chosen)){
					active_feat_learned = true;
				}
				MouseUI.PopButtonMap();
			}
			else{
				learned.Add("Your " + Skill.Name(skill) + " skill increases to " + skills[skill] + ". ");
			}
			if(skill == SkillType.MAGIC){
				maxmp += 5;
				curmp += 5;
				List<SpellType> unknown = new List<SpellType>();
				List<colorstring> unknownstr = new List<colorstring>();
				List<SpellType> random_spell_list = new List<SpellType>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					random_spell_list.Add(spell);
				}
				while(unknown.Count < 5 && random_spell_list.Count > 0){
					SpellType spell = random_spell_list.RemoveRandom();
					if(!HasSpell(spell) && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
						unknown.Add(spell);
					}
				}
				unknown.Sort((sp1,sp2)=>Spell.Tier(sp1).CompareTo(Spell.Tier(sp2)));
				foreach(SpellType spell in unknown){
					colorstring cs = new colorstring();
					cs.strings.Add(new cstr(Spell.Name(spell).PadRight(17) + Spell.Tier(spell).ToString().PadLeft(3),Color.Gray));
					cs.strings.Add(new cstr("".PadRight(5),Color.Gray));
					unknownstr.Add(cs + Spell.Description(spell));
				}
				M.Draw();
				/*for(int i=unknown.Count+2;i<ROWS;++i){
					Screen.WriteMapString(i,0,"".PadRight(COLS));
				}*/
				Help.TutorialTip(TutorialTopic.SpellTiers);
				Screen.WriteMapString(unknown.Count+2,0,"".PadRight(COLS));
				colorstring topborder = new colorstring("---------------------Tier-----------------Description-------------",Color.Gray);
				int selection = Select("Learn which spell? ",topborder,new colorstring("".PadRight(25,'-') + "[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(COLS,'-'),Color.Gray),unknownstr,false,true,false,true,HelpTopic.Spells);
				spells[unknown[selection]] = true;
				learned.Add("You learn " + Spell.Name(unknown[selection]) + ". ");
				spells_in_order.Add(unknown[selection]);
			}
			if(learned.Count > 0){
				foreach(string s in learned){
					B.Add(s);
				}
			}
			if(active_feat_learned){
				M.Draw();
				Help.TutorialTip(TutorialTopic.ActiveFeats);
			}
		}
		/*public void GainXP(int num){
			if(num <= 0){
				num = 1;
			}
			xp += num;
			//here's the formula for gaining the next level:
			// (standard experience is mlevel * (10 + mlevel - playerlevel) )
			// the number of monsters of the CURRENT level you would need to slay in order to reach the next level is equal to
			//  10 + (currentlevel-1)*2 / 3
			// therefore you reach level 2 after defeating 10 level 1 foes, which give 10xp each,
			// and you reach level 3 after defeating 11 level 2 foes, which give 20xp each.
			// (and so on)
			List<string> learned = null;
			switch(level){
			case 0:
				if(xp >= 0){
					learned = LevelUp();
				}
				break;
			case 1:
				if(xp >= 100){
					learned = LevelUp();
				}
				break;
			case 2:
				if(xp >= 320){
					learned = LevelUp();
				}
				break;
			case 3:
				if(xp >= 680){
					learned = LevelUp();
				}
				break;
			case 4:
				if(xp >= 1160){
					learned = LevelUp();
				}
				break;
			case 5:
				if(xp >= 1810){
					learned = LevelUp();
				}
				break;
			case 6:
				if(xp >= 2650){
					learned = LevelUp();
				}
				break;
			case 7:
				if(xp >= 3630){
					learned = LevelUp();
				}
				break;
			case 8:
				if(xp >= 4830){
					learned = LevelUp();
				}
				break;
			case 9:
				if(xp >= 6270){
					learned = LevelUp();
				}
				break;
			}
			if(learned != null){
				foreach(string s in learned){
					B.Add(s);
				}
			}
		}
		public List<string> LevelUp(){
			List<string> learned = new List<string>();
			++level;
			if(level == 1){
				//B.Add("Welcome, adventurer! ");
				B.Add("Welcome, " + player_name + "! ");
			}
			else{
				B.Add("Welcome to level " + level + ". ");
			}
			DisplayStats();
			B.PrintAll();
			ConsoleKeyInfo command;
			List<SkillType> skills_increased = new List<SkillType>();
			List<FeatType> feats_increased = new List<FeatType>();
			bool done = false;
			while(!done){
				Screen.ResetColors();
				B.DisplayNow("Choose which skills you'll increase: ");
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				for(int i=0;i<5;++i){
					SkillType sk = (SkillType)i;
					Screen.WriteMapString(1+i*4,0,("["+(char)(i+97)+"] " + Skill.Name(sk)).PadRight(22));
					Screen.WriteMapChar(1+i*4,1,new colorchar(Color.Cyan,(char)(i+97)));
					Color levelcolor = skills_increased.Contains(sk)? Color.Green : Color.Gray;
					int skill_level = skills_increased.Contains(sk)? skills[sk] + 1 : skills[sk];
					Screen.WriteMapString(1+i*4,22,new cstr(levelcolor,("Level " + skill_level).PadRight(70)));
					FeatType ft = Feat.OfSkill(sk,0);
					Color featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					int feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(2+i*4,0,new cstr(featcolor,("    " + Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(35)));
					ft = Feat.OfSkill(sk,1);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(2+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
					ft = Feat.OfSkill(sk,2);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(3+i*4,0,new cstr(featcolor,("    " + Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(35)));
					ft = Feat.OfSkill(sk,3);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(3+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
					Screen.WriteMapString(4+i*4,0,"".PadRight(COLS));
				}
				if(skills_increased.Count == 3){
					Screen.WriteMapString(21,0,"--Type [a-e] to choose a skill--[?] for help--[Enter] to accept---");
					Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
					Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'e'));
					Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
					Screen.WriteMapString(21,47,new cstr(Color.Magenta,"Enter"));
				}
				else{
					Screen.WriteMapString(21,0,"--Type [a-e] to choose a skill--[?] for help-------(" + (3-skills_increased.Count) + " left)-------");
					Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
					Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'e'));
					Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
				}
				Screen.SetCursorPosition(37+Global.MAP_OFFSET_COLS,2);
				Screen.CursorVisible = true;
				command = Global.ReadKey();
				Screen.CursorVisible = false;
				char ch = ConvertInput(command);
				switch(ch){
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
					SkillType chosen_skill = (SkillType)(((int)ch)-97);
					if(skills_increased.Count == 3 && !skills_increased.Contains(chosen_skill)){
						break;
					}
					if(skills_increased.Contains(chosen_skill)){
						skills_increased.Remove(chosen_skill);
						for(int i=0;i<4;++i){
							if(feats_increased.Contains(Feat.OfSkill(chosen_skill,i))){
								feats_increased.Remove(Feat.OfSkill(chosen_skill,i));
							}
						}
					}
					else{
						skills_increased.Add(chosen_skill);
						bool done2 = false;
						while(!done2){
							Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
							for(int i=0;i<5;++i){
								SkillType sk = (SkillType)i;
								Color graycolor = Color.DarkGray;
								Color greencolor = Color.DarkGreen;
								Color magentacolor = Color.DarkMagenta;
								if(sk == chosen_skill){
									graycolor = Color.Gray;
									greencolor = Color.Green;
									magentacolor = Color.Magenta;
								}
								Screen.WriteMapString(1+i*4,0,new cstr(graycolor,("    " + Skill.Name(sk)).PadRight(22)));
								Color levelcolor = skills_increased.Contains(sk)? greencolor : graycolor;
								int skill_level = skills_increased.Contains(sk)? skills[sk] + 1 : skills[sk];
								Screen.WriteMapString(1+i*4,22,new cstr(levelcolor,("Level " + skill_level).PadRight(70)));
								FeatType ft = Feat.OfSkill(sk,0);
								Color featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								int feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(2+i*4,4,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(31)));
								ft = Feat.OfSkill(sk,1);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(2+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
								ft = Feat.OfSkill(sk,2);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(3+i*4,4,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(31)));
								ft = Feat.OfSkill(sk,3);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(3+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
								Screen.WriteMapString(4+i*4,0,"".PadRight(COLS));
							}
							Screen.WriteMapString(2+4*(int)chosen_skill,0,"[a]");
							Screen.WriteMapString(2+4*(int)chosen_skill,31,"[b]");
							Screen.WriteMapString(3+4*(int)chosen_skill,0,"[c]");
							Screen.WriteMapString(3+4*(int)chosen_skill,31,"[d]");
							if(feats[Feat.OfSkill(chosen_skill,0)] == 1){
								Screen.WriteMapChar(2+4*(int)chosen_skill,1,new colorchar(Color.DarkRed,'a'));
							}
							else{
								Screen.WriteMapChar(2+4*(int)chosen_skill,1,new colorchar(Color.Cyan,'a'));
							}
							if(feats[Feat.OfSkill(chosen_skill,1)] == 1){
								Screen.WriteMapChar(2+4*(int)chosen_skill,32,new colorchar(Color.DarkRed,'b'));
							}
							else{
								Screen.WriteMapChar(2+4*(int)chosen_skill,32,new colorchar(Color.Cyan,'b'));
							}
							if(feats[Feat.OfSkill(chosen_skill,2)] == 1){
								Screen.WriteMapChar(3+4*(int)chosen_skill,1,new colorchar(Color.DarkRed,'c'));
							}
							else{
								Screen.WriteMapChar(3+4*(int)chosen_skill,1,new colorchar(Color.Cyan,'c'));
							}
							if(feats[Feat.OfSkill(chosen_skill,3)] == 1){
								Screen.WriteMapChar(3+4*(int)chosen_skill,32,new colorchar(Color.DarkRed,'d'));
							}
							else{
								Screen.WriteMapChar(3+4*(int)chosen_skill,32,new colorchar(Color.Cyan,'d'));
							}
							Screen.WriteMapString(21,0,"--Type [a-d] to choose a feat---[?] for help----------------------");
							Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
							Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'d'));
							Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
							Screen.ResetColors();
							B.DisplayNow("Choose a " + Skill.Name(chosen_skill) + " feat: ");
							Screen.CursorVisible = true;
							command = Global.ReadKey();
							Screen.CursorVisible = false;
							ch = ConvertInput(command);
							switch(ch){
							case 'a':
							case 'b':
							case 'c':
							case 'd':
								{
								FeatType feat = Feat.OfSkill(chosen_skill,((int)ch)-97);
								if(!HasFeat(feat)){
									feats_increased.Add(feat);
									done2 = true;
								}
								break;
								}
							case '?':
								Help.DisplayHelp(HelpTopic.Feats);
								DisplayStats();
								break;
							case ' ':
							case (char)27:
								skills_increased.Remove(chosen_skill);
								done2 = true;
								break;
							default:
								break;
							}
						}
					}
					break;
				case '?':
					Help.DisplayHelp(HelpTopic.Feats);
					DisplayStats();
					break;
				case (char)13:
					if(skills_increased.Count == 3){
						done = true;
					}
					break;
				default:
					break;
				}
			}
			foreach(SkillType skill in skills_increased){
				skills[skill]++;
				if(Global.quickstartinfo != null){
					Global.quickstartinfo.Add(skill.ToString());
				}
			}
			foreach(FeatType feat in feats_increased){
				feats[feat]--; //negative values are used until you've completely learned a feat
				if(feats[feat] == -(Feat.MaxRank(feat))){
					feats[feat] = 1;
					learned.Add("You learn the " + Feat.Name(feat) + " feat. ");
					if(feat == FeatType.DANGER_SENSE){
						attrs[AttrType.DANGER_SENSE_ON]++;
					}
					if(feat == FeatType.DRIVE_BACK){
						attrs[AttrType.DRIVE_BACK_ON]++;
					}
				}
				if(Global.quickstartinfo != null){
					Global.quickstartinfo.Add(feat.ToString());
				}
			}
			if(skills_increased.Contains(SkillType.MAGIC)){
				List<SpellType> unknown = new List<SpellType>();
				List<colorstring> unknownstr = new List<colorstring>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(!HasSpell(spell) && spell != SpellType.BLESS && spell != SpellType.MINOR_HEAL
					&& spell != SpellType.HOLY_SHIELD && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
						unknown.Add(spell);
						cstr cs1 = new cstr(Spell.Name(spell).PadRight(15) + Spell.Tier(spell).ToString().PadLeft(3),Color.Gray);
						int failrate = (Spell.Tier(spell) - TotalSkill(SkillType.MAGIC)) * 5;
						if(failrate < 0){
							failrate = 0;
						}
						Color failcolor = Color.White;
						if(failrate > 50){
							failcolor = Color.DarkRed;
						}
						else{
							if(failrate > 20){
								failcolor = Color.Red;
							}
							else{
								if(failrate > 0){
									failcolor = Color.Yellow;
								}
							}
						}
						cstr cs2 = new cstr(failrate.ToString().PadLeft(9) + "%",failcolor);
						cstr cs3 = new cstr(Spell.Description(spell).PadLeft(34),Color.Gray);
						unknownstr.Add(new colorstring(cs1,cs2,cs3));
					}
				}
				for(int i=unknown.Count+2;i<ROWS;++i){
					Screen.WriteMapString(i,0,"".PadRight(COLS));
				}
				colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
				int selection = Select("Learn which spell? ",topborder,new colorstring("".PadRight(COLS,'-'),Color.Gray),unknownstr,false,true,false,true,HelpTopic.Spells);
				spells[unknown[selection]] = 1;
				learned.Add("You learn " + Spell.Name(unknown[selection]) + ". ");
				if(Global.quickstartinfo != null){
					Global.quickstartinfo.Add(unknown[selection].ToString());
				}
			}
			return learned;
		}*/
		public bool CanSee(int r,int c){ return CanSee(M.tile[r,c]); }
		public bool CanSee(PhysicalObject o){
			if(o == this || p.Equals(o.p)){ //same object or same location
				return true;
			}
			if(HasAttr(AttrType.ASLEEP)){
				return false;
			}
			Actor a = o as Actor;
			if(a != null){
				if(HasAttr(AttrType.DETECTING_MONSTERS)){
					if(this == player){
						a.attrs[AttrType.DANGER_SENSED] = 1;
					}
					return true;
				}
				if(a.IsInvisibleHere() && !HasAttr(AttrType.BLINDSIGHT)){
					return false;
				}
			}
			Tile t = o as Tile;
			if(t != null){
				if(t.solid_rock){
					return false;
				}
			}
			if(HasAttr(AttrType.BLIND) && !HasAttr(AttrType.BLINDSIGHT)){
				return false;
			}
			if(type == ActorType.CLOUD_ELEMENTAL){
				List<pos> cloud = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].features.Contains(FeatureType.FOG));
				foreach(pos p2 in cloud){
					if(o.DistanceFrom(p2) <= 12){
						if(M.tile[p2].HasLOS(o.row,o.col)){
							if(o is Actor){
								if((o as Actor).IsHiddenFrom(this)){
									return false;
								}
								return true;
							}
							else{
								return true;
							}
						}
					}
				}
				return false;
			}
			else{
				if(IsWithinSightRangeOf(o.row,o.col) || (M.tile[o.row,o.col].IsLit() && !HasAttr(AttrType.BLINDSIGHT))){
					if(HasLOS(o.row,o.col)){
						if(a != null && a.IsHiddenFrom(this)){
							return false;
						}
						if(a != null && this == player){
							a.attrs[AttrType.DANGER_SENSED] = 1;
						}
						return true;
					}
				}
			}
			return false;
		}
		public bool IsWithinSightRangeOf(PhysicalObject o){ return IsWithinSightRangeOf(o.row,o.col); }
		public bool IsWithinSightRangeOf(int r,int c){
			int dist = DistanceFrom(r,c);
			int divisor = HasAttr(AttrType.DIM_VISION)? 2 : 1;
			if(dist <= 2/divisor){
				return true;
			}
			if(dist <= 5/divisor && HasAttr(AttrType.LOW_LIGHT_VISION)){
				return true;
			}
			if(dist <= 12/divisor && HasAttr(AttrType.BLINDSIGHT)){
				return true;
			}
			if(M.tile[r,c].opaque){
				foreach(Tile t in M.tile[r,c].NonOpaqueNeighborsBetween(row,col)){
					if(IsWithinSightRangeOf(t.row,t.col)){
						return true;
					}
				}
			}
			return false;
		}
		public bool IsHiddenFrom(Actor a){
			if(this == a){ //you can always see yourself
				return false;
			}
			//if(a.HasAttr(AttrType.ASLEEP)){ //testing this
			//	return true;
			//}
			if(a.HasAttr(AttrType.DETECTING_MONSTERS)){
				return false;
			}
			if(IsInvisibleHere() && !a.HasAttr(AttrType.BLINDSIGHT)){
				if(this == player && !a.HasAttr(AttrType.PLAYER_NOTICED)){ //monsters aren't hidden from each other
					return true; //todo: this is the only place PLAYER_NOTICED is checked - i suspect it does nothing important now.
				}
				if(a == player && !HasAttr(AttrType.NOTICED)){
					return true;
				}
			}
			if(type == ActorType.PLAYER){
				if(a.player_visibility_duration < 0 || HasAttr(AttrType.ENRAGED)){
					return false;
				}
				return true;
			}
			else{
				if(a.type != ActorType.PLAYER){ //monsters are never hidden from each other
					return false;
				}
				if(HasAttr(AttrType.STEALTHY) && attrs[AttrType.TURNS_VISIBLE] >= 0 && !IsBurning() && !HasAttr(AttrType.ENRAGED)){ //todo: added burning here. make sure this works. if not, put a check for LightRadius() > 0 somewhere
					return true;
				}
				return false;
			}
		}
		public bool IsInvisibleHere(){ //returns true if this is not visible at its current location, without considering the abilities of any viewers.
			if(HasAttr(AttrType.INVISIBLE) && (LightRadius() == 0 || M.wiz_dark || M.wiz_lite) && !IsBurning() && !tile().IsBurning()){ //todo: check for gases?
				return true;
			}
			if(HasAttr(AttrType.SHADOW_CLOAK) && !tile().IsLit() && !IsBurning()){ //todo: check tile burning? gases?
				return true;
			}
			return false;
		}
		public bool IsHelpless(){
			return HasAttr(AttrType.PARALYZED,AttrType.ASLEEP,AttrType.AMNESIA_STUN); //anything else?
		}
		public static string MonsterDescriptionText(ActorType type){
			switch(type){
			case ActorType.GOBLIN:
				return "Goblins are stunted and primitive scavengers, a frequent nuisance to the civilized races whose lands they inhabit.";
			case ActorType.GIANT_BAT:
				return "Swooping down from the ceiling to snatch its insect prey, the giant bat poses little threat to the prepared adventurer.";
			case ActorType.LONE_WOLF:
				return "Lithe and quick, this canine predator has formidable teeth and powerful jaws.";
			case ActorType.SKELETON:
				return "Bones rattle as the skeleton moves, held together by necromantic magics or ancient curses.";
			case ActorType.BLOOD_MOTH:
				return "Irresistibly drawn to light, this strange moth has a wide razor-filled mouth. Rivulets of crimson on its enormous wings mimic dripping blood, lending the moth its name.";
			case ActorType.SWORDSMAN:
				return "Always ready for a fight, this swordsman wears the practical leather armor of a mercenary. His eyes never leave his foe, watching and waiting for the next advance.";
			case ActorType.DARKNESS_DWELLER:
				return "This pale dirty humanoid wears tattered rags. Its huge eyes are sensitive to light.";
			case ActorType.CARNIVOROUS_BRAMBLE:
				return "Sharp tangles of thorny branches spread out from its center. The closest seem to follow your movements.";
			case ActorType.FROSTLING:
				return "An alien-looking creature of cold, the frostling possesses insectlike mandibles, claws, and smooth whitish skin. A fog of chill condensation surrounds it.";
			case ActorType.DREAM_WARRIOR:
			case ActorType.DREAM_WARRIOR_CLONE:
				return "The features of this warrior are hard to make out, but the curved blade held at the ready is clear enough.";
			case ActorType.CULTIST:
			case ActorType.FINAL_LEVEL_CULTIST:
				return "This cultist wears a crimson robe that reaches the ground. His head has been shaved and tattooed in devotion to his demon lord.";
			case ActorType.GOBLIN_ARCHER:
				return "A hunter and warrior for its tribe, this goblin carries a crude bow and wears a quiver of arrows.";
			case ActorType.GOBLIN_SHAMAN:
				return "This goblin's tattoos identify it as a tribe leader and shaman. It carries a small staff and wears a necklace of ears and fingers.";
			case ActorType.MIMIC:
			return "The mimic disguises itself as an ordinary object, then waits for an unwary treasure hunter. It secretes a powerful adhesive to hold its prey.";
				//return "The mimic changes its shape to that of an ordinary object, then waits for an unwary goblin or adventurer. It can secrete a powerful adhesive to hold its prey.";
			case ActorType.SKULKING_KILLER:
				return "This smirking rogue dashes from shadow to shadow, dagger in hand. A faint sheen can be seen on the edge of his blade.";
			case ActorType.ZOMBIE:
				return "The zombie is a rotting, shambling corpse animated by the dark art of necromancy. It mindlessly seeks the flesh of the living.";
			case ActorType.DIRE_RAT:
				return "A cacophony of squeaks and chirps follows any swarm of dire rats. With red eyes and long yellow teeth, each of these huge rats reaches halfway to a man's knee, but is easily capable of leaping for his throat.";
			case ActorType.ROBED_ZEALOT:
				return "A holy symbol hangs, silver and forked, from the neck of the zealot. The holy prayer of the church promises the zealot a swift victory over heretics.";
			case ActorType.SHADOW:
				return "Shadows are manifest darkness, barely maintaining a physical presence. A dark environment hides them utterly, but the light reveals their warped human shape.";
			case ActorType.BANSHEE:
				return "The banshee floats shrieking, trailing wisps of a faded dress behind her. Her nails are blood-caked claws. Only the most courageous can resist the chill of the banshee's hateful scream.";
			case ActorType.WARG:
				return "This evil relative of the wolf has white fur with black facial markings. Its eyes are too human for your liking.";
			case ActorType.PHASE_SPIDER:
				return "Heedless of the laws of nature, this brilliantly iridescent spider steps to the side and appears twenty feet away. Even when you're looking right at it, you think you can hear it behind you.";
			case ActorType.DERANGED_ASCETIC:
				return "This solitary monk constantly kicks and punches at empty space, never making a sound. Those nearby will find themselves inexplicably forced to uphold the ascetic's vow of silence.";
			case ActorType.POLTERGEIST:
				return "This troublesome spirit has a penchant for throwing things and upending furniture. It affords no rest to intruders in the area that it haunts.";
			case ActorType.CAVERN_HAG:
				return "The hag's foul brand of magic can impart a nasty curse on those who cross her. Cracked, warty skin hides surprising strength, used to wrestle her victims into the stewpot.";
			case ActorType.NOXIOUS_WORM:
				return "Taller than most men, the noxious worm vomits a thick stench from its maw. Those who survive its poison will be crushed by its weight.";
			case ActorType.BERSERKER:
				return "In battle, the berserker enters a state of unfeeling rage, axe swinging at anything within reach. Trophies of war adorn the berserker's unarmored form.";
			case ActorType.TROLL:
				return "The troll stands taller than you, all muscles, claws, and warty greenish skin. Fire is a troll's bane; its regenerative powers will quickly heal any other injury.";
			case ActorType.VAMPIRE:
				return "The vampire floats above the ground with hunger in its eyes. A dark cape flows around its pale form.";
			case ActorType.CRUSADING_KNIGHT:
				return "This knight's armor bears the holy symbols of his church. He holds his torch aloft, awaiting the appearance of evildoers.";
			case ActorType.SKITTERMOSS:
				return "A roiling mass of moss and debris, this creature is a shambling nest filled with innumerable insects.";
			case ActorType.MUD_ELEMENTAL:
				return "As the mud elemental oozes across the floor, bits of dirt seem to animate and are absorbed into its body.";
			case ActorType.MUD_TENTACLE:
				return "A writhing, grasping tendril of mud emerges from the wall.";
			case ActorType.ENTRANCER:
				return "The entrancer bends a weak-minded being to her will and has it fight on her behalf, at least until a more desirable thrall appears. In battle, the entrancer can protect and teleport the enthralled creature.";
			case ActorType.MARBLE_HORROR:
				return "Its shape is still that of a statue, but the darkness reveals the diseased appearance of its pale skin. No light is reflected from its empty eyes.";
			case ActorType.MARBLE_HORROR_STATUE:
				return "As a statue, the marble horror is invulnerable and inactive. It will remain in this form as long as light falls upon it.";
			case ActorType.OGRE_BARBARIAN:
				return "The smallest of the giant races, ogres sometimes fashion crude armor from scrap and carry huge tree-clubs. This one wears filthy furs and prefers to rip and crush with its bare hands.";
			case ActorType.ORC_GRENADIER:
				return "Orcs are a burly and warlike race, quick to make enemies. The grenadier carries a satchel filled with deadly orcish explosives.";
			case ActorType.SNEAK_THIEF:
				return "This experienced thief prefers to simply snatch items away. He wields a thin blade and uses a spinning style in combat.";
			case ActorType.CARRION_CRAWLER:
				return "Though usually an eater of corpses, the carrion crawler will attack the living when hungry. Tentacles on its head apply a paralyzing toxin to its prey.";
			case ActorType.SPELLMUDDLE_PIXIE:
				return "Using fairy enchantments, this pixie causes its every wingbeat to reverberate in the skulls of those nearby, stifling all other sounds.";
			case ActorType.STONE_GOLEM:
				return "Constructs of stone are often created to guard or serve. In combat, they call upon the earth magics used in their creation.";
			case ActorType.PYREN_ARCHER:
				return "Tall and wide-shouldered descendants of flame, the pyren are a strange race of men. Though they are flesh and blood, they still possess the power to ignite nearby objects.";
			case ActorType.ORC_ASSASSIN:
				return "This orcish stalker is well camouflaged. A wicked grin shows off sharp teeth as the assassin brandishes a long blade.";
			case ActorType.MECHANICAL_KNIGHT:
				return "The mechanical knight's shield moves with unnatural speed, ready to foil any onslaught. Its exposed gears appear vulnerable to any attack that could bypass its shield.";
			case ActorType.ORC_WARMAGE:
				return "The destruction wreaked by warmages evokes respect and fear even among their own kind. They often lead raids and war parties, using tracking spells to complement their lethal magic.";
			case ActorType.LASHER_FUNGUS:
				return "The lasher is a tall mass of fungal growth with several ropelike tentacles extending from it.";
			case ActorType.NECROMANCER:
				return "Necromancers practice the dark arts, raising the dead to serve them. They gain power through unholy rituals that make them unwelcome in any civilized place.";
			case ActorType.LUMINOUS_AVENGER:
				return "The radiance of this empyreal being makes your eyes hurt after a few seconds. When you look again it still has the shape of a human, but occasionally its silhouette seems to have wings, horns, or four legs.";
			case ActorType.CORPSETOWER_BEHEMOTH:
				return "This monstrosity looks like it was stitched together from corpses of several different species. You see pieces of humans, orcs, and trolls, in addition to some you can't begin to identify.";
			case ActorType.FIRE_DRAKE:
				return "Huge, deadly, and hungry for your charred flesh, the fire drake prepares to drag your valuables back to its lair. You have no doubts that you now face the snarling fiery master of this dungeon.";
			case ActorType.SPITTING_COBRA:
				return "This snake is feared for its toxic bite and its ability to spit venom into its target's face with uncanny accuracy.";
			case ActorType.KOBOLD:
				return "Kobolds look like small goblins with canine muzzles. They prefer to fire at intruders from the safety of their secret holes.";
			case ActorType.SPORE_POD:
				return "A sac full of spores, floating through the air. Contact with any sharp edge will cause it to burst.";
			case ActorType.FORASECT:
				return "An insect with powerful jaws, built low to the ground. It can burrow through the ground faster than a man can walk.";
			case ActorType.GOLDEN_DART_FROG:
				return "This large frog's skin has a brilliant pattern of color, warning attackers of its poisonous nature.";
			case ActorType.GIANT_SLUG:
				return "This enormous slug secretes thick slime which it can spit long distances. Its wide mouth is especially caustic.";
			case ActorType.VULGAR_DEMON:
				return "A displaced denizen of some distant Hell, this red-skinned creature leaps about as it taunts its foes.";
			case ActorType.WILD_BOAR:
				return "This great beast has long tusks that can skewer a man. One boar can feed a goblin tribe for weeks.";
			case ActorType.DREAM_SPRITE:
			case ActorType.DREAM_SPRITE_CLONE:
				return "This illusion-casting fairy likes to mislead travelers, then zap them once they're hopelessly lost.";
			case ActorType.CLOUD_ELEMENTAL:
				return "An animated bank of vapor, the cloud elemental rumbles with thunder as it rolls through the air toward you.";
			case ActorType.RUNIC_TRANSCENDENT:
				return "Glowing symbols are carved deeply into this creature's flesh. Its form is human but its manner is something entirely alien.";
			case ActorType.ALASI_BATTLEMAGE:
				return "Using the magical craft of her race, this caster's armor is covered entirely in runes, proof against both physical and magical assaults.";
			case ActorType.ALASI_SOLDIER:
				return "Alasi soldiers wield runed spears that can deliver stunning strikes from a safe distance.";
			case ActorType.ALASI_SCOUT:
				return "At the forefront of the alasi force, the scout is equipped with an enchanted sword and feather-weight armor.";
			case ActorType.FLAMETONGUE_TOAD:
				return "Flametongues are burrowing creatures that generate an astonishing amount of heat. Though small, settlements regard flametongues as a serious threat due to their tendency to cause unexpected fires.";
			case ActorType.TROLL_BLOODWITCH:
				return "A practitioner of ancient blood magic passed down among the trolls, more than one unprepared militia has met its end at the claws of a bloodwitch.";
			case ActorType.CYCLOPEAN_TITAN:
				return "The ground trembles as this towering colossus strides forward. The cyclops has a huge central eye and a satchel of boulders at its side. It hefts a massive stone club as though it weighs nothing.";
				//return "The ground trembles as this towering colossus strides forward. A satchel of boulders hangs at the titan's side. Its large eyes are adapted to seeing in the pitch blackness of the massive underground caves.";
			case ActorType.ALASI_SENTINEL:
				return "A juggernaut in runed armor, the sentinels of the alasi are well-protected inside their enchanted suits.";
			case ActorType.STALKING_WEBSTRIDER:
				return "The webstrider moves with alarming speed through web-filled tunnels. It often devours prey faster than its poison can kill.";
			case ActorType.MACHINE_OF_WAR:
				return "Perhaps a siege engine run amok, this clattering contraption sputters forward haltingly. It swivels its turret constantly, occasionally releasing a burst of fire from the vents on its sides.";
			case ActorType.IMPOSSIBLE_NIGHTMARE:
				return "This horror defies reason and sanity, seeming at once too large for its surroundings, and too distant. Logic and proportion are forgotten as the universe fights back against this nameless thing.";
			case ActorType.GHOST:
				return "Ghosts are restless spirits bound to this world by some unfinished task.";
			case ActorType.MINOR_DEMON:
				return "The least of Kersai's demon army, this creature has a forked tail and mottled skin.";
			case ActorType.FROST_DEMON:
				return "The frost demon is surrounded by an indistinct cloud of whitish-blue. Large curved horns rise out of the cloud.";
			case ActorType.BEAST_DEMON:
				return "A feral-looking demon with the face of a lion. These creatures hunt those who would escape from Kersai's power.";
			case ActorType.DEMON_LORD:
				return "Demon lords are the largest and most brutal members of Kersai's army. With their greatwhips they lead the demon swarm into battle.";
			default:
				return "Phantoms are beings of illusion, but real enough to do lasting harm. They vanish at the slightest touch.";
			}
		}
		public static List<colorstring> MonsterDescriptionBox(Actor a,bool mouselook,int max_string_length){
			ActorType type = a.type;
			List<string> text = MonsterDescriptionText(type).GetWordWrappedList(max_string_length);
			Color box_edge_color = Color.Green;
			Color box_corner_color = Color.Yellow;
			Color text_color = Color.Gray;
			int widest = 20; // length of "[=] Hide description"
			foreach(string s in text){
				if(s.Length > widest){
					widest = s.Length;
				}
			}
			if(mouselook){
				if(a.name.Length > widest){
					widest = a.name.Length;
				}
				if(a.WoundStatus().Length > widest){
					widest = a.WoundStatus().Length;
				}
			}
			widest += 2; //one space on each side
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			if(mouselook){
				box.Add(new colorstring("|",box_edge_color) + a.name.PadOuter(widest).GetColorString(Color.White) + new colorstring("|",box_edge_color));
				box.Add(new colorstring("|",box_edge_color) + a.WoundStatus().PadOuter(widest).GetColorString(Color.White) + new colorstring("|",box_edge_color));
				box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
			}
			foreach(string s in text){
				box.Add(new colorstring("|",box_edge_color) + s.PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			if(!mouselook){
				box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
				box.Add(new colorstring("|",box_edge_color) + "[=] Hide description".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			return box;
		}
		public enum UnknownTilePathingPreference{UnknownTilesAreKnown,UnknownTilesAreClosed,UnknownTilesAreOpen};
		public int GetCost(pos x,bool smart_pathing_near_difficult_terrain,List<pos> threatened_positions,UnknownTilePathingPreference unknown_tile_pref){
			if(smart_pathing_near_difficult_terrain && threatened_positions.Contains(x)){
				return 20000; //todo: hope this doesn't break anything.
			}
			if(unknown_tile_pref == UnknownTilePathingPreference.UnknownTilesAreOpen && !M.tile[x].seen){
				return 20;
			}
			if(HasAttr(AttrType.HUMANOID_INTELLIGENCE) && M.tile[x].Is(TileType.DOOR_C)){
				return 20;
			}
			if(M.tile[x].Is(TileType.RUBBLE) && !HasAttr(AttrType.SMALL)){
				if(this == player){
					return 2000; //made more expensive for the player because it exhausts
				}
				else{
					return 20;
				}
			}
			if(M.tile[x].Is(TileType.STONE_SLAB)){
				return 20000;
			}
			if(smart_pathing_near_difficult_terrain){
				bool known_trap = M.tile[x].IsKnownTrap() || (M.tile[x].IsTrap() && this != player);
				bool flying = HasAttr(AttrType.FLYING) && !HasAttr(AttrType.DESCENDING);
				bool safe_trap = (this == player && M.tile[x].name.Contains("safe"));
				if(known_trap){
					if(flying || safe_trap){ //this'd be better if I could cheaply consider remaining flight duration
						return 30;
					}
					else{
						return 20000;
					}
				}
				if(M.tile[x].Is(FeatureType.WEB) && !HasAttr(AttrType.BRUTISH_STRENGTH,AttrType.OIL_COVERED,AttrType.SLIMED)){
					if(this == player){
						return 5000;
					}
					else{
						return 100;
					}
				}
				if(M.tile[x].Is(TileType.POPPY_FIELD) && !HasAttr(AttrType.NONLIVING,AttrType.MENTAL_IMMUNITY)){
					//return M.poppy_distance_map[x] * 10; //todo: testing this
					if(M.poppy_distance_map[x] == 1){
						return 19;
					}
					else{
						if(M.poppy_distance_map[x].IsValidDijkstraValue()){
							return 200;
						}
					}
				}
				bool can_slip = M.tile[x].IsSlippery() && (magic_trinkets == null || !magic_trinkets.Contains(MagicTrinketType.BOOTS_OF_GRIPPING));
				if(can_slip || M.tile[x].Is(TileType.GRAVE_DIRT)){
					return 15;
				}
				if(this == player){
					if(M.tile[x].Is(TileType.TOMBSTONE)){
						if(M.tile[x].color == Color.White){
							return 20000;
						}
						else{
							return 11;
						}
					}
				}
			}
			return 10;
		}
		public void FindPath(PhysicalObject o){ path = GetPath(o); } //todo: probably remove most of these overloads. Give the important versions their own names.
		public void FindPath(PhysicalObject o,int max_distance){ path = GetPath(o,max_distance); }
		public void FindPath(PhysicalObject o,int max_distance,bool smart_pathing_near_difficult_terrain){ path = GetPath(o,max_distance,smart_pathing_near_difficult_terrain); }
		public void FindPath(int r,int c){ path = GetPath(r,c); }
		public void FindPath(int r,int c,int max_distance){ path = GetPath(r,c,max_distance); }
		public void FindPath(int r,int c,int max_distance,bool smart_pathing_near_difficult_terrain){ path = GetPath(r,c,max_distance,smart_pathing_near_difficult_terrain); }
		public List<pos> GetPath(PhysicalObject o){ return GetPath(o.row,o.col,-1,false); }
		public List<pos> GetPath(PhysicalObject o,int max_distance){ return GetPath(o.row,o.col,max_distance,false); }
		public List<pos> GetPath(PhysicalObject o,int max_distance,bool smart_pathing_near_difficult_terrain){ return GetPath(o.row,o.col,max_distance,smart_pathing_near_difficult_terrain); }
		public List<pos> GetPath(int r,int c){ return GetPath(r,c,-1,false); }
		public List<pos> GetPath(int r,int c,int max_distance){ return GetPath(r,c,max_distance,false); }
		public List<pos> GetPath(int r,int c,int max_distance,bool smart_pathing_near_difficult_terrain){ return GetPath(r,c,max_distance,smart_pathing_near_difficult_terrain,false,UnknownTilePathingPreference.UnknownTilesAreKnown); } //todo: this defaults to nondeterministic pathing! check to see whether this is a good idea. (generally, the player should use deterministic and monsters shouldn't, right?)
		public List<pos> GetPath(int r,int c,int max_distance,bool smart_pathing_near_difficult_terrain,bool deterministic_results,UnknownTilePathingPreference unknown_tile_pref){
			U.BooleanPositionDelegate is_blocked = x=>{
				if(!M.tile[x].seen){
					if(unknown_tile_pref == UnknownTilePathingPreference.UnknownTilesAreOpen && x.BoundsCheck(M.tile,false)){
						return false;
					}
					if(unknown_tile_pref == UnknownTilePathingPreference.UnknownTilesAreClosed){
						return true;
					}
				}
				if(max_distance != -1 && DistanceFrom(x) > max_distance){
					return true;
				}
				if(M.tile[x].Is(TileType.CHASM,TileType.FIRE_RIFT)){ //chasm is currently unused
					return true;
				}
				if(M.tile[x].passable || M.tile[x].Is(TileType.RUBBLE) || (HasAttr(AttrType.HUMANOID_INTELLIGENCE) && M.tile[x].Is(TileType.DOOR_C)) || (x.row == r && x.col == c)){
					return false;
				}
				if(this == player && M.tile[x].type == TileType.STONE_SLAB){
					return false;
				}
				return true;
			};
			List<pos> threatened_positions = new List<pos>();
			if(this == player){
				foreach(Actor a in tiebreakers){ //testing this as a replacement for AllActors()
					if(a != null && a.HasAttr(AttrType.IMMOBILE) && CanSee(a)){
						foreach(pos neighbor in a.PositionsWithinDistance(1)){
							threatened_positions.Add(neighbor);
						}
					}
				}
				foreach(Tile t in TilesAtDistance(1)){
					if(GetDangerRating(t) > 0){
						threatened_positions.Add(t.p);
					}
				}
			}
			U.IntegerPositionDelegate get_cost = x => GetCost(x,smart_pathing_near_difficult_terrain,threatened_positions,unknown_tile_pref);
			var dijkstra = M.tile.GetDijkstraMap(is_blocked,get_cost,new List<pos>{new pos(r,c)},this.p);
			List<pos> new_path = new List<pos>();
			pos current_position = this.p;
			while(true){
				List<pos> valid = current_position.PositionsAtDistance(1).Where(x=>dijkstra[x].IsValidDijkstraValue() && dijkstra[x] < dijkstra[current_position]).WhereLeast(x=>dijkstra[x]).WhereLeast(x=>x.ApproximateEuclideanDistanceFromX10(current_position));
				if(valid.Count > 0){
					if(deterministic_results){
						current_position = valid.Last();
					}
					else{
						current_position = valid.Random();
					}
					new_path.Add(current_position);
					if(current_position.row == r && current_position.col == c){
						return new_path;
					}
				}
				else{
					new_path.Clear();
					return new_path;
				}
			}
		}
		public bool FindAutoexplorePath(){ //returns true if successful
			U.BooleanPositionDelegate is_blocked = x=>{
				if(!M.tile[x].seen){
					return true;
				}
				if(M.tile[x].Is(TileType.CHASM,TileType.FIRE_RIFT)){ //chasm is currently unused
					return true;
				}
				//M.tile[x].passable || M.tile[x].Is(TileType.RUBBLE) || (HasAttr(AttrType.HUMANOID_INTELLIGENCE) && M.tile[x].Is(TileType.DOOR_C))){
				if(!M.tile[x].BlocksConnectivityOfMap() && M.tile[x].type != TileType.HIDDEN_DOOR){
					return false;
				}
				return true;
			};
			U.BooleanPositionDelegate is_target = x=>{
				if(x.Equals(p) || (M.tile[x].IsKnownTrap() && (!HasAttr(AttrType.FLYING) || HasAttr(AttrType.DESCENDING)))){
					return false;
				}
				if(M.tile[x].inv != null && !M.tile[x].inv.ignored){
					return true;
				}
				if(M.tile[x].Is(TileType.CHEST) && M.tile[x].direction_exited == 0){
					return true;
				}
				if(M.tile[x].IsShrine() && M.tile[x].direction_exited == 0){
					return true;
				}
				//(M.tile[x].passable || M.tile[x].Is(TileType.RUBBLE) || (HasAttr(AttrType.HUMANOID_INTELLIGENCE) && M.tile[x].Is(TileType.DOOR_C)))){
				if(M.tile[x].seen && !M.tile[x].BlocksConnectivityOfMap() && M.tile[x].type != TileType.HIDDEN_DOOR){
					foreach(Tile neighbor in M.tile[x].TilesAtDistance(1)){
						if(!neighbor.seen){
							return true;
						}
					}
				}
				return false;
			};
			List<pos> threatened_positions = new List<pos>();
			foreach(Actor a in tiebreakers){
				if(a != null && a.HasAttr(AttrType.IMMOBILE) && (HasAttr(AttrType.DANGER_SENSED) || CanSee(a))){
					foreach(pos neighbor in a.PositionsWithinDistance(1)){
						threatened_positions.Add(neighbor);
					}
				}
			}
			foreach(Tile t in TilesAtDistance(1)){
				if(GetDangerRating(t) > 0){
					threatened_positions.Add(t.p);
				}
			}
			U.IntegerPositionDelegate get_cost = x => GetCost(x,true,threatened_positions,UnknownTilePathingPreference.UnknownTilesAreClosed);
			var dijkstra = M.tile.GetDijkstraMap(is_blocked,is_target,get_cost,get_cost);
			List<pos> new_path = new List<pos>();
			pos current_position = this.p;
			while(true){
				List<pos> valid = current_position.PositionsAtDistance(1).Where(x=>dijkstra[x].IsValidDijkstraValue() && dijkstra[x] < dijkstra[current_position]).WhereLeast(x=>dijkstra[x]).WhereLeast(x=>x.ApproximateEuclideanDistanceFromX10(current_position));
				if(valid.Count > 0){
					current_position = valid.Random();
					new_path.Add(current_position);
					if(is_target(current_position)){
					//if(dijkstra[current_position] == 0){
						path = new_path;
						if(this == player){
							interrupted_path = new pos(-1,-1);
						}
 						return true;
					}
				}
				else{
					return false;
				}
			}
		}
		public int EnemiesAdjacent(){ //currently counts ALL actors adjacent, and as such really only applies to the player.
			int count = -1; //don't count self
			for(int i=row-1;i<=row+1;++i){
				for(int j=col-1;j<=col+1;++j){
					if(M.actor[i,j] != null){ //no bounds check, actors shouldn't be on edge tiles.
						++count;
					}
				}
			}
			return count; //this method should be removed, really
		}
		public int GetDirection(){ return GetDirection("Which direction? ",false,false); }
		public int GetDirection(bool orth,bool allow_self_targeting){ return GetDirection("Which direction? ",orth,allow_self_targeting); }
		public int GetDirection(string s){ return GetDirection(s,false,false); }
		public int GetDirection(string s,bool orth,bool allow_self_targeting){
			MouseUI.PushButtonMap(MouseMode.Directional);
			foreach(int dir in U.EightDirections){
				pos neighbor = p.PosInDir(dir);
				MouseUI.CreateButton((ConsoleKey)(ConsoleKey.NumPad0 + dir),false,Global.MAP_OFFSET_ROWS + neighbor.row,Global.MAP_OFFSET_COLS + neighbor.col,1,1);
			}
			MouseUI.CreateButton(ConsoleKey.NumPad5,false,Global.MAP_OFFSET_ROWS + row,Global.MAP_OFFSET_COLS + col,1,1);
			B.DisplayNow(s);
			ConsoleKeyInfo command;
			char ch;
			Screen.CursorVisible = true;
			while(true){
				command = Global.ReadKey();
				ch = ConvertInput(command);
				if(command.KeyChar == '.'){
					ch = '5';
				}
				ch = ConvertVIKeys(ch);
				int i = (int)Char.GetNumericValue(ch);
				if(i>=1 && i<=9){
					if(i != 5){
						if(!orth || i%2==0){ //in orthogonal mode, return only even dirs
							Screen.CursorVisible = false;
							MouseUI.PopButtonMap();
							return i;
						}
					}
					else{
						if(allow_self_targeting){
							Screen.CursorVisible = false;
							MouseUI.PopButtonMap();
							return i;
						}
					}
				}
				if(ch == (char)27){ //escape
					Screen.CursorVisible = false;
					MouseUI.PopButtonMap();
					return -1;
				}
				if(ch == ' '){
					Screen.CursorVisible = false;
					MouseUI.PopButtonMap();
					return -1;
				}
			}
		}
		public void ChoosePathingDestination(){ ChoosePathingDestination(true,true,"Move cursor to choose destination, then press Enter. "); }
		public void ChoosePathingDestination(bool visible_path,bool visible_monsters,string unseen_area_message){
			MouseUI.PushButtonMap(MouseMode.Targeting);
			ConsoleKeyInfo command;
			int r = row;
			int c = col;
			if(visible_path && interrupted_path.BoundsCheck(M.tile)){
				r = interrupted_path.row;
				c = interrupted_path.col;
			}
			int minrow = 0;
			int maxrow = ROWS-1;
			int mincol = 0;
			int maxcol = COLS-1;
			colorchar[,] mem = new colorchar[ROWS,COLS];
			List<Tile> line = new List<Tile>();
			List<Tile> oldline = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					mem[i,j] = Screen.MapChar(i,j);
				}
			}
			/*if(!start_at_interesting_target || interesting_targets.Count == 0){
				if(lookmode){
					B.DisplayNow("Move the cursor to look around. ");
				}
				else{
					B.DisplayNow(unseen_area_message);
				}
			}*/
			List<PhysicalObject> interesting_targets = new List<PhysicalObject>();
			for(int i=1;i<ROWS-1;++i){
				for(int j=1;j<COLS-1;++j){
					if(M.tile[i,j].seen){
						if(M.tile[i,j].Is(TileType.CHEST,TileType.POOL_OF_RESTORATION,TileType.STAIRS) || M.tile[i,j].IsShrine() || M.tile[i,j].inv != null || (CanSee(M.tile[i,j]) && M.tile[i,j].Is(FeatureType.TELEPORTAL,FeatureType.STABLE_TELEPORTAL))){
							interesting_targets.Add(M.tile[i,j]);
						}
					}
				}
			}
			PosArray<bool> known_reachable = M.tile.GetFloodFillArray(this.p,false,x=>((M.tile[x].passable || M.tile[x].IsDoorType(false)) && M.tile[x].seen));
			PosArray<int> distance_to_nearest_known_passable = M.tile.GetDijkstraMap(x=>false,y=>M.tile[y].seen && (M.tile[y].passable || M.tile[y].IsDoorType(false)) && !M.tile[y].IsKnownTrap() && known_reachable[y]);
			if(r == row && c == col){
				B.DisplayNow(unseen_area_message);
			}
			bool first_iteration = true;
			bool done = false; //when done==true, we're ready to return 'result'
			Tile nearest = M.tile[r,c];
			while(!done){
				Screen.ResetColors();
				nearest = M.tile[r,c];
				if(!first_iteration || r != row || c != col){
					Targeting_DisplayContents(nearest,"",unseen_area_message,visible_monsters,first_iteration);
				}
				Screen.CursorVisible = false;
				bool blocked = false;
				if(visible_path){
					//if(!nearest.seen || nearest.IsKnownTrap() || !nearest.TilesWithinDistance(1).Any(x=>x.passable && known_reachable[x.p])){
					if(!nearest.TilesWithinDistance(1).Any(x=>(x.passable || x.IsDoorType(false)) && x.seen && known_reachable[x.p])){
						nearest = nearest.TilesAtDistance(distance_to_nearest_known_passable[r,c]).Where(x=>x.seen && (x.passable || x.IsDoorType(false)) && !x.IsKnownTrap() && known_reachable[x.p]).WhereLeast(x=>x.ApproximateEuclideanDistanceFromX10(r,c)).Last();
					}
					if(nearest != null){
						line.Clear();
						List<pos> temp = GetPath(nearest.row,nearest.col,-1,true,true,UnknownTilePathingPreference.UnknownTilesAreClosed);
						foreach(pos p in temp){ //i should switch 'line' over to using positions anyway
							line.Add(M.tile[p]);
						}
					}
					Targeting_ShowLine(M.tile[r,c],0,mem,line,oldline,ref blocked,x=>false);
					if(!line.Contains(M.tile[r,c])){
						if(r != row || c != col){
							colorchar cch = mem[r,c];
							cch.bgcolor = Color.Green;
							if(Global.LINUX && !Screen.GLMode){ //no bright bg in terminals
								cch.bgcolor = Color.DarkGreen;
							}
							if(cch.color == cch.bgcolor){
								cch.color = Color.Black;
							}
							Screen.WriteMapChar(r,c,cch);
							line.Add(M.tile[r,c]);
							oldline.Remove(M.tile[r,c]);
						}
					}
				}
				else{
					colorchar cch = mem[r,c];
					cch.bgcolor = Color.Green;
					if(Global.LINUX && !Screen.GLMode){ //no bright bg in terminals
						cch.bgcolor = Color.DarkGreen;
					}
					if(cch.color == cch.bgcolor){
						cch.color = Color.Black;
					}
					Screen.WriteMapChar(r,c,cch);
					line = new List<Tile>{M.tile[r,c]};
					oldline.Remove(M.tile[r,c]);
				}
				foreach(Tile t in oldline){
					Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
				}
				oldline = new List<Tile>(line);
				first_iteration = false;
				M.tile[r,c].Cursor();
				Screen.CursorVisible = true;
				command = Global.ReadKey();
				char ch = ConvertInput(command);
				ch = ConvertVIKeys(ch);
				if(ch == 'X' && visible_path){
					ch = (char)13;
				}
				if(ch == '>' && M.tile[r,c].type == TileType.STAIRS && visible_path){
					ch = (char)13;
				}
				if(!Targeting_HandleCommonCommands(command,ch,ref r,ref c,interesting_targets,ref done,minrow,maxrow,mincol,maxcol,true)){
					switch(ch){
					case '>':
					{
						Tile stairs = null;
						foreach(Tile t in M.AllTiles()){
							if(t.type == TileType.STAIRS && t.seen){
								stairs = t;
								break;
							}
						}
						if(stairs != null){
							r = stairs.row;
							c = stairs.col;
						}
						break;
					}
					case 'X':
					{
						if(this == player && B.YesOrNoPrompt("Travel to this location?")){
							if(!nearest.seen || nearest.IsKnownTrap() || !nearest.TilesWithinDistance(1).Any(x=>x.passable && known_reachable[x.p])){
								nearest = nearest.TilesAtDistance(distance_to_nearest_known_passable[r,c]).Where(x=>x.seen && (x.passable || x.IsDoorType(false)) && !x.IsKnownTrap() && known_reachable[x.p]).WhereLeast(x=>x.ApproximateEuclideanDistanceFromX10(r,c)).Last();
							}
							path = GetPath(nearest.row,nearest.col,-1,true,true,UnknownTilePathingPreference.UnknownTilesAreClosed);
							if(path.Count > 0 && !M.tile[path.Last()].passable){
								path.RemoveLast();
							}
							interrupted_path = new pos(-1,-1);
							done = true;
						}
						break;
					}
					case (char)13:
					if(visible_path && line.Count > 0){
						if(nearest != null){
							path = GetPath(nearest.row,nearest.col,-1,true,true,UnknownTilePathingPreference.UnknownTilesAreClosed);
						}
						else{
							path = new List<pos>();
							foreach(Tile t in line){
								path.Add(t.p);
							}
						}
						if(!line.Last().passable){
							path.RemoveLast();
						}
						interrupted_path = new pos(-1,-1);
					}
					done = true;
					break;
					}
				}
			}
			Targeting_RemoveLine(nearest,done,line,mem,0);
			MouseUI.PopButtonMap();
		}
		/*public void ChoosePathingDestination(){
			MouseUI.PushButtonMap(MouseMode.Targeting);
			ConsoleKeyInfo command;
			int r = row;
			int c = col;
			if(interrupted_path.BoundsCheck(M.tile)){
				r = interrupted_path.row;
				c = interrupted_path.col;
			}
			int minrow = 0;
			int maxrow = ROWS-1;
			int mincol = 0;
			int maxcol = COLS-1;
			colorchar[,] mem = new colorchar[ROWS,COLS];
			List<Tile> line = new List<Tile>();
			List<Tile> oldline = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					mem[i,j] = Screen.MapChar(i,j);
				}
			}
			List<PhysicalObject> interesting_targets = new List<PhysicalObject>();
			for(int i=1;i<ROWS-1;++i){
				for(int j=1;j<COLS-1;++j){
					if(M.tile[i,j].seen){
						if(M.tile[i,j].Is(TileType.CHEST,TileType.POOL_OF_RESTORATION,TileType.STAIRS) || M.tile[i,j].IsShrine() || M.tile[i,j].inv != null || (CanSee(M.tile[i,j]) && M.tile[i,j].Is(FeatureType.TELEPORTAL,FeatureType.STABLE_TELEPORTAL))){
							interesting_targets.Add(M.tile[i,j]);
						}
					}
				}
			}
			PosArray<bool> known_reachable = M.tile.GetFloodFillArray(this.p,false,x=>(M.tile[x].passable || M.tile[x].IsDoorType(false)) && M.tile[x].seen);
			PosArray<int> distance_to_nearest_known_passable = M.tile.GetDijkstraMap(x=>false,y=>M.tile[y].seen && (M.tile[y].passable || M.tile[y].IsDoorType(false)) && !M.tile[y].IsKnownTrap() && known_reachable[y]);
			if(r == row && c == col){
				B.DisplayNow("Move cursor to choose destination, then press Enter. ");
			}
			bool first_iteration = true;
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
				Screen.ResetColors();
				string contents = "You see ";
				List<string> items = new List<string>();
				if(M.actor[r,c] != null && M.actor[r,c] != this && CanSee(M.actor[r,c])){
					items.Add(M.actor[r,c].a_name + " " + M.actor[r,c].WoundStatus());
				}
				if(M.tile[r,c].inv != null){
					items.Add(M.tile[r,c].inv.AName(true));
				}
				foreach(FeatureType f in M.tile[r,c].features){
					items.Add(Tile.Feature(f).a_name);
				}
				if(items.Count == 0){
					contents += M.tile[r,c].AName(true);
				}
				else{
					if(items.Count == 1){
						contents += items[0] + M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
					}
					else{
						if(items.Count == 2){
							if(M.tile[r,c].type != TileType.FLOOR){
								if(M.tile[r,c].Preposition() == " and "){
									contents += items[0] + ", " + items[1] + ",";
									contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
								}
								else{
									contents += items[0] + " and " + items[1];
									contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
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
									if(M.tile[r,c].type != TileType.FLOOR){
										contents += s + ","; //because preposition contains a space already
									}
									else{
										contents += "and " + s;
									}
								}
							}
							if(M.tile[r,c].type != TileType.FLOOR){
								contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
							}
						}
					}
				}
				if(r == row && c == col){
					if(!first_iteration){
						string s = "You're standing here. ";
						if(items.Count == 0 && M.tile[r,c].type == TileType.FLOOR){
							B.DisplayNow(s);
						}
						else{
							B.DisplayNow(s + contents + " here. ");
						}
					}
				}
				else{
					if(CanSee(M.tile[r,c])){
						B.DisplayNow(contents + ". ");
					}
					else{
						if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
							B.DisplayNow("You sense " + M.actor[r,c].a_name + " " + M.actor[r,c].WoundStatus() + ". ");
						}
						else{
							if(M.tile[r,c].seen){
								if(M.tile[r,c].inv != null){
									char itemch = M.tile[r,c].inv.symbol;
									char screench = Screen.MapChar(r,c).c;
									if(itemch == screench){ //hacky, but it seems to work (when a monster drops an item you haven't seen yet)
										if(M.tile[r,c].inv.quantity > 1){
											B.DisplayNow("You can no longer see these " + M.tile[r,c].inv.Name(true) + ". ");
										}
										else{
											B.DisplayNow("You can no longer see this " + M.tile[r,c].inv.Name(true) + ". ");
										}
									}
									else{
										B.DisplayNow("You can no longer see this " + M.tile[r,c].Name(true) + ". ");
									}
								}
								else{
									B.DisplayNow("You can no longer see this " + M.tile[r,c].Name(true) + ". ");
								}
							}
							else{
								B.DisplayNow("Move cursor to choose destination, then press Enter. ");
							}
						}
					}
				}
				Screen.CursorVisible = false;
				Tile nearest = M.tile[r,c];
				//if(!nearest.seen || nearest.IsKnownTrap() || !nearest.TilesWithinDistance(1).Any(x=>x.passable) || !known_reachable[nearest.p]){
				if(!nearest.seen || nearest.IsKnownTrap() || !nearest.TilesWithinDistance(1).Any(x=>x.passable && known_reachable[x.p])){
					nearest = nearest.TilesAtDistance(distance_to_nearest_known_passable[r,c]).Where(x=>x.seen && (x.passable || x.IsDoorType(false)) && !x.IsKnownTrap() && known_reachable[x.p]).WhereLeast(x=>x.ApproximateEuclideanDistanceFromX10(r,c)).Last();
				}
				if(nearest != null){
					line.Clear();
					List<pos> temp = GetPath(nearest.row,nearest.col,-1,true,true,UnknownTilePathingPreference.UnknownTilesAreClosed);
					foreach(pos p in temp){ //i should switch 'line' over to using positions anyway
						line.Add(M.tile[p]);
					}
				}
				foreach(Tile t in line){
					if(t.row != row || t.col != col){
						colorchar cch = mem[t.row,t.col];
						if(t.row == r && t.col == c){
							cch.bgcolor = Color.Green;
							if(Global.LINUX && !Screen.GLMode){ //no bright bg in terminals
								cch.bgcolor = Color.DarkGreen;
							}
							if(cch.color == cch.bgcolor){
								cch.color = Color.Black;
							}
							Screen.WriteMapChar(t.row,t.col,cch);
						}
						else{
							cch.bgcolor = Color.DarkGreen;
							if(cch.color == cch.bgcolor){
								cch.color = Color.Black;
							}
							Screen.WriteMapChar(t.row,t.col,cch);
						}
					}
					oldline.Remove(t);
				}
				if(!line.Contains(M.tile[r,c])){
					if(r != row || c != col){
						colorchar cch = mem[r,c];
						cch.bgcolor = Color.Green;
						if(Global.LINUX && !Screen.GLMode){ //no bright bg in terminals
							cch.bgcolor = Color.DarkGreen;
						}
						if(cch.color == cch.bgcolor){
							cch.color = Color.Black;
						}
						Screen.WriteMapChar(r,c,cch);
						line.Add(M.tile[r,c]);
						oldline.Remove(M.tile[r,c]);
					}
				}
				foreach(Tile t in oldline){
					Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
				}
				oldline = new List<Tile>(line);
				first_iteration = false;
				M.tile[r,c].Cursor();
				Screen.CursorVisible = true;
				command = Global.ReadKey();
				char ch = ConvertInput(command);
				ch = ConvertVIKeys(ch);
				int move_value = 1;
				if((command.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt
					|| (command.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control
					|| (command.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift){
					move_value = 6;
				}
				switch(ch){
				case '7':
				r -= move_value;
				c -= move_value;
				break;
				case '8':
				r -= move_value;
				break;
				case '9':
				r -= move_value;
				c += move_value;
				break;
				case '4':
				c -= move_value;
				break;
				case '6':
				c += move_value;
				break;
				case '1':
				r += move_value;
				c -= move_value;
				break;
				case '2':
				r += move_value;
				break;
				case '3':
				r += move_value;
				c += move_value;
				break;
				case (char)9:
				if((command.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift){
					if(interesting_targets.Count > 0){
						List<PhysicalObject> reversed_targets = new List<PhysicalObject>(interesting_targets);
						reversed_targets.Reverse();
						int idx = 0;
						int dist = DistanceFrom(r,c);
						int idx_of_next_closest = -1;
						bool found = false;
						foreach(PhysicalObject o in reversed_targets){
							if(o.row == r && o.col == c){
								int prev_idx = idx + 1; //this goes backwards because the list goes backwards
								if(prev_idx == reversed_targets.Count){
									prev_idx = 0;
								}
								r = reversed_targets[prev_idx].row;
								c = reversed_targets[prev_idx].col;
								found = true;
								break;
							}
							else{
								if(idx_of_next_closest == -1 && DistanceFrom(o) < dist){
									idx_of_next_closest = idx;
								}
							}
							++idx;
						}
						if(!found){
							if(idx_of_next_closest == -1){
								r = reversed_targets[0].row;
								c = reversed_targets[0].col;
							}
							else{
								r = reversed_targets[idx_of_next_closest].row;
								c = reversed_targets[idx_of_next_closest].col;
							}
						}
					}
				}
				else{
					if(interesting_targets.Count > 0){
						int idx = 0;
						int dist = DistanceFrom(r,c);
						int idx_of_next_farthest = -1;
						bool found = false;
						foreach(PhysicalObject o in interesting_targets){
							if(o.row == r && o.col == c){
								int next_idx = idx + 1;
								if(next_idx == interesting_targets.Count){
									next_idx = 0;
								}
								r = interesting_targets[next_idx].row;
								c = interesting_targets[next_idx].col;
								found = true;
								break;
							}
							else{
								if(idx_of_next_farthest == -1 && DistanceFrom(o) > dist){
									idx_of_next_farthest = idx;
								}
							}
							++idx;
						}
						if(!found){
							if(idx_of_next_farthest == -1){
								r = interesting_targets[0].row;
								c = interesting_targets[0].col;
							}
							else{
								r = interesting_targets[idx_of_next_farthest].row;
								c = interesting_targets[idx_of_next_farthest].col;
							}
						}
					}
				}
				break;
				case '>':
				{
					Tile stairs = null;
					foreach(Tile t in M.AllTiles()){
						if(t.type == TileType.STAIRS && t.seen){
							stairs = t;
							break;
						}
					}
					if(stairs != null){
						r = stairs.row;
						c = stairs.col;
					}
					break;
				}
				case (char)27:
				case ' ':
				done = true;
				break;
				case (char)13:
				if(line.Count > 0){
					if(nearest != null){
						path = GetPath(nearest.row,nearest.col,-1,true,true,UnknownTilePathingPreference.UnknownTilesAreClosed);
					}
					else{
						path = new List<pos>();
						foreach(Tile t in line){
							path.Add(t.p);
						}
						if(!line.Last().passable){
							path.RemoveLast();
						}
					}
					interrupted_path = new pos(-1,-1);
				}
				done = true;
				break;
				default:
				if(command.Key == ConsoleKey.F1){
					r = MouseUI.LastRow - Global.MAP_OFFSET_ROWS;
					c = MouseUI.LastCol - Global.MAP_OFFSET_COLS;
				}
				else{
					if(command.Key == ConsoleKey.F2){
						r = row;
						c = col;
					}
				}
				break;
				}
				if(r < minrow){
					r = minrow;
				}
				if(r > maxrow){
					r = maxrow;
				}
				if(c < mincol){
					c = mincol;
				}
				if(c > maxcol){
					c = maxcol;
				}
				if(done){
					Screen.CursorVisible = false;
					foreach(Tile t in line){
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
					Screen.CursorVisible = true;
					MouseUI.PopButtonMap();
				}
			}
		}*/
		public class ItemSelection{
			public int value = -1;
			public bool description_requested = false;
			public ItemSelection(){}
		}
		public ItemSelection SelectItem(string message){ return SelectItem(message,false); }
		public ItemSelection SelectItem(string message,bool never_redraw_map){
			MouseUI.PushButtonMap();
			MouseUI.AutomaticButtonsFromStrings = true;
			colorstring top_border = "".PadRight(COLS,'-').GetColorString();
			colorstring bottom_border = ("------Space left: " + (Global.MAX_INVENTORY_SIZE - InventoryCount()).ToString().PadRight(7,'-') + "[?] for help").PadRight(COLS,'-').GetColorString();
			List<colorstring> strings = InventoryList().GetColorStrings();
			bool no_ask = false;
			bool no_cancel = false;
			bool easy_cancel = true;
			bool help_key = true;
			HelpTopic help_topic = HelpTopic.Items;
			ItemSelection result = new ItemSelection();
			result.value = -2;
			while(result.value == -2){ //this part is hacked together from Select()
				Screen.WriteMapString(0,0,top_border);
				char letter = 'a';
				int i=1;
				foreach(colorstring s in strings){
					Screen.WriteMapString(i,0,new colorstring("[",Color.Gray,letter.ToString(),Color.Cyan,"] ",Color.Gray));
					Screen.WriteMapString(i,4,s);
					if(s.Length() < COLS-4){
						Screen.WriteMapString(i,s.Length()+4,"".PadRight(COLS - (s.Length()+4)));
					}
					letter++;
					i++;
				}
				Screen.WriteMapString(i,0,bottom_border);
				if(i < ROWS-1){
					Screen.WriteMapString(i+1,0,"".PadRight(COLS));
				}
				if(no_ask){
					B.DisplayNow(message);
					result.value = -1;
					MouseUI.PopButtonMap();
					MouseUI.AutomaticButtonsFromStrings = false;
					return result;
				}
				else{
					result = GetItemSelection(message,strings.Count,no_cancel,easy_cancel,help_key);
					if(result.value == -2){
						MouseUI.AutomaticButtonsFromStrings = false;
						Help.DisplayHelp(help_topic);
						MouseUI.AutomaticButtonsFromStrings = true;
					}
					else{
						if(!never_redraw_map && result.value != -1 && !result.description_requested){
							M.Redraw();
						}
						MouseUI.PopButtonMap();
						MouseUI.AutomaticButtonsFromStrings = false;
						return result;
					}
				}
			}
			result.value = -1;
			MouseUI.PopButtonMap();
			MouseUI.AutomaticButtonsFromStrings = false;
			return result;
		}
		public ItemSelection GetItemSelection(string s,int count,bool no_cancel,bool easy_cancel,bool help_key){
			ItemSelection result = new ItemSelection();
			B.DisplayNow(s);
			Screen.CursorVisible = true;
			ConsoleKeyInfo command;
			char ch;
			while(true){
				command = Global.ReadKey();
				ch = ConvertInput(command);
				int i = ch - 'a';
				if(i >= 0 && i < count){
					result.value = i;
					return result;
				}
				if(help_key && ch == '?'){
					result.value = -2;
					return result;
				}
				int j = Char.ToLower(ch) - 'a';
				if(j >= 0 && j < count){
					result.value = j;
					result.description_requested = true;
					return result;
				}
				if(no_cancel == false){
					if(easy_cancel){
						result.value = -1;
						return result;
					}
					if(ch == (char)27 || ch == ' '){
						result.value = -1;
						return result;
					}
				}
				if(count == 0){
					result.value = -1;
					return result;
				}
			}
		}
		public int Select(string message,List<string> strings){ return Select(message,"".PadLeft(COLS,'-'),"".PadLeft(COLS,'-'),strings,false,false,true); }
		public int Select(string message,List<string> strings,bool no_ask,bool no_cancel,bool easy_cancel){ return Select(message,"".PadLeft(COLS,'-'),"".PadLeft(COLS,'-'),strings,no_ask,no_cancel,easy_cancel); }
		public int Select(string message,string top_border,List<string> strings){ return Select(message,top_border,"".PadLeft(COLS,'-'),strings,false,false,true); }
		public int Select(string message,string top_border,List<string> strings,bool no_ask,bool no_cancel,bool easy_cancel){ return Select(message,top_border,"".PadLeft(COLS,'-'),strings,no_ask,no_cancel,easy_cancel); }
		public int Select(string message,string top_border,string bottom_border,List<string> strings){ return Select(message,top_border,bottom_border,strings,false,false,true); }
		public int Select(string message,string top_border,string bottom_border,List<string> strings,bool no_ask,bool no_cancel,bool easy_cancel){
			if(!no_ask){
				MouseUI.PushButtonMap();
			}
			MouseUI.AutomaticButtonsFromStrings = true;
			Screen.WriteMapString(0,0,top_border);
			char letter = 'a';
			int i=1;
			foreach(string s in strings){
				string s2 = "[" + letter + "] " + s;
				Screen.WriteMapString(i,0,s2.PadRight(COLS));
				Screen.WriteMapChar(i,1,new colorchar(Color.Cyan,letter));
				letter++;
				i++;
			}
			Screen.WriteMapString(i,0,bottom_border);
			if(i < ROWS-1){
				Screen.WriteMapString(i+1,0,"".PadRight(COLS));
			}
			if(no_ask){
				B.DisplayNow(message);
				if(!no_ask){
					MouseUI.PopButtonMap();
				}
				MouseUI.AutomaticButtonsFromStrings = false;
				return -1;
			}
			else{
				int result = GetSelection(message,strings.Count,no_cancel,easy_cancel,false);
				if(result != -1){
					if(!Global.GRAPHICAL){
						M.Redraw(); //again, todo: why is this here? - i think it's as close as it's gonna get now.
					}
					else{
						M.Draw();
					}
				}
				if(!no_ask){
					MouseUI.PopButtonMap();
				}
				MouseUI.AutomaticButtonsFromStrings = false;
				return result;
			}
		} //todo: check how many things actually use the non-colorstring version of Select and consider removing it
		public int Select(string message,colorstring top_border,colorstring bottom_border,List<colorstring> strings,bool no_ask,bool no_cancel,bool easy_cancel,bool help_key,HelpTopic help_topic){ return Select(message,top_border,bottom_border,strings,no_ask,no_cancel,easy_cancel,false,help_key,help_topic); }
		public int Select(string message,colorstring top_border,colorstring bottom_border,List<colorstring> strings,bool no_ask,bool no_cancel,bool easy_cancel,bool never_redraw_map,bool help_key,HelpTopic help_topic){
			if(!no_ask){
				MouseUI.PushButtonMap();
			}
			MouseUI.AutomaticButtonsFromStrings = true;
			int result = -2;
			while(result == -2){
				Screen.WriteMapString(0,0,top_border);
				char letter = 'a';
				int i=1;
				foreach(colorstring s in strings){
					Screen.WriteMapString(i,0,new colorstring("[",Color.Gray,letter.ToString(),Color.Cyan,"] ",Color.Gray));
					Screen.WriteMapString(i,4,s);
					if(s.Length() < COLS-4){
						Screen.WriteMapString(i,s.Length()+4,"".PadRight(COLS - (s.Length()+4)));
					}
					letter++;
					i++;
				}
				Screen.WriteMapString(i,0,bottom_border);
				if(i < ROWS-1){
					Screen.WriteMapString(i+1,0,"".PadRight(COLS));
				}
				if(no_ask){
					B.DisplayNow(message);
					if(!no_ask){
						MouseUI.PopButtonMap();
					}
					MouseUI.AutomaticButtonsFromStrings = false;
					return -1;
				}
				else{
					result = GetSelection(message,strings.Count,no_cancel,easy_cancel,help_key);
					if(result == -2){
						MouseUI.AutomaticButtonsFromStrings = false;
						Help.DisplayHelp(help_topic);
						MouseUI.AutomaticButtonsFromStrings = true;
					}
					else{
						if(!never_redraw_map && result != -1){
							M.Redraw();
						}
						if(!no_ask){
							MouseUI.PopButtonMap();
						}
						MouseUI.AutomaticButtonsFromStrings = false;
						return result;
					}
				}
			}
			if(!no_ask){
				MouseUI.PopButtonMap();
			}
			MouseUI.AutomaticButtonsFromStrings = false;
			return -1;
		}
		public int GetSelection(string s,int count,bool no_cancel,bool easy_cancel,bool help_key){
			//if(count == 0){ return -1; }
			B.DisplayNow(s);
			Screen.CursorVisible = true;
			ConsoleKeyInfo command;
			char ch;
			while(true){
				command = Global.ReadKey();
				ch = ConvertInput(command);
				int i = ch - 'a';
				if(i >= 0 && i < count){
					return i;
				}
				if(help_key && ch == '?'){
					return -2;
				}
				if(no_cancel == false){
					if(easy_cancel){
						return -1;
					}
					if(ch == (char)27 || ch == ' '){
						return -1;
					}
				}
				if(count == 0){
					return -1;
				}
			}
		}
		public void AnimateProjectile(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateProjectile(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateMapCell(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateMapCell(o.row,o.col,new colorchar(color,c));
		}
		public void AnimateBoltProjectile(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltProjectile(GetBestLineOfEffect(o.row,o.col),color);
		}
		public void AnimateBoltProjectile(PhysicalObject o,Color color,int duration){
			B.DisplayNow();
			Screen.AnimateBoltProjectile(GetBestLineOfEffect(o.row,o.col),color,duration);
		}
		public void AnimateExplosion(PhysicalObject o,int radius,Color color,char c){
			B.DisplayNow();
			Screen.AnimateExplosion(o,radius,new colorchar(color,c));
		}
		public void AnimateBeam(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateBeam(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateBoltBeam(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltBeam(GetBestLineOfEffect(o.row,o.col),color);
		}
		//
		// i should have made them (char,color) from the start..
		//
		public void AnimateProjectile(PhysicalObject o,char c,Color color){
			B.DisplayNow();
			Screen.AnimateProjectile(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateMapCell(PhysicalObject o,char c,Color color){
			B.DisplayNow();
			Screen.AnimateMapCell(o.row,o.col,new colorchar(color,c));
		}
		public void AnimateExplosion(PhysicalObject o,int radius,char c,Color color){
			B.DisplayNow();
			Screen.AnimateExplosion(o,radius,new colorchar(color,c));
		}
		public void AnimateBeam(PhysicalObject o,char c,Color color){
			B.DisplayNow();
			Screen.AnimateBeam(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		//from here forward, i'll just do (char,color)..
		public void AnimateStorm(int radius,int num_frames,int num_per_frame,char c,Color color){
			B.DisplayNow();
			Screen.AnimateStorm(p,radius,num_frames,num_per_frame,new colorchar(c,color));
		}
		public void AnimateProjectile(List<Tile> line,char c,Color color){
			B.DisplayNow();
			Screen.AnimateProjectile(line,new colorchar(color,c));
		}
		public void AnimateBeam(List<Tile> line,char c,Color color){
			B.DisplayNow();
			Screen.AnimateBeam(line,new colorchar(color,c));
		}
		public void AnimateBoltProjectile(List<Tile> line,Color color){
			B.DisplayNow();
			Screen.AnimateBoltProjectile(line,color);
		}
		public void AnimateBoltBeam(List<Tile> line,Color color){
			B.DisplayNow();
			Screen.AnimateBoltBeam(line,color);
		}
		public void AnimateVisibleMapCells(List<pos> cells,colorchar ch){ AnimateVisibleMapCells(cells,ch,50); }
		public void AnimateVisibleMapCells(List<pos> cells,colorchar ch,int duration){
			List<pos> new_cells = cells.Where(x=>CanSee(x.row,x.col));
			if(new_cells.Count > 0){
				B.DisplayNow();
				Screen.AnimateMapCells(new_cells,ch,duration);
			}
		}
		public void AnimateVisibleMapCells(List<pos> cells,List<colorchar> chars){ AnimateVisibleMapCells(cells,chars,50); }
		public void AnimateVisibleMapCells(List<pos> cells,List<colorchar> chars,int duration){
			List<pos> new_cells = new List<pos>();
			List<colorchar> new_chars = new List<colorchar>();
			for(int i=0;i<cells.Count;++i){
				if(CanSee(cells[i].row,cells[i].col)){
					new_cells.Add(cells[i]);
					new_chars.Add(chars[i]);
				}
			}
			if(new_cells.Count > 0){
				B.DisplayNow();
				Screen.AnimateMapCells(new_cells,new_chars,duration);
			}
		}
	}
	public static class Skill{
		public static string Name(SkillType type){
			switch(type){
			case SkillType.COMBAT:
				return "Combat";
			case SkillType.DEFENSE:
				return "Defense";
			case SkillType.MAGIC:
				return "Magic";
			case SkillType.SPIRIT:
				return "Spirit";
			case SkillType.STEALTH:
				return "Stealth";
			default:
				return "no skill";
			}
		}
	}
	public static class Feat{
		public static bool IsActivated(FeatType type){
			switch(type){
			case FeatType.LUNGE:
			case FeatType.TUMBLE:
			case FeatType.DISARM_TRAP:
				return true;
			case FeatType.QUICK_DRAW:
			case FeatType.WHIRLWIND_STYLE:
			case FeatType.DRIVE_BACK:
			case FeatType.CUNNING_DODGE:
			case FeatType.ARMOR_MASTERY:
			case FeatType.DEFLECT_ATTACK:
			case FeatType.MASTERS_EDGE:
			case FeatType.ARCANE_INTERFERENCE:
			case FeatType.CHAIN_CASTING:
			case FeatType.FORCE_OF_WILL:
			case FeatType.CONVICTION:
			case FeatType.ENDURING_SOUL:
			case FeatType.FEEL_NO_PAIN:
			case FeatType.BOILING_BLOOD:
			case FeatType.NECK_SNAP:
			case FeatType.CORNER_CLIMB:
			case FeatType.DANGER_SENSE:
			default:
				return false;
			}
		}
		public static FeatType OfSkill(SkillType skill,int num){ // 0 through 3
			switch(skill){
			case SkillType.COMBAT:
				return (FeatType)num;
			case SkillType.DEFENSE:
				return (FeatType)num+4;
			case SkillType.MAGIC:
				return (FeatType)num+8;
			case SkillType.SPIRIT:
				return (FeatType)num+12;
			case SkillType.STEALTH:
				return (FeatType)num+16;
			default:
				return FeatType.NO_FEAT;
			}
		}
		public static SkillType Skill(FeatType type){
			switch(type){
			case FeatType.QUICK_DRAW:
			case FeatType.WHIRLWIND_STYLE:
			case FeatType.LUNGE:
			case FeatType.DRIVE_BACK:
				return SkillType.COMBAT;
			case FeatType.ARMOR_MASTERY:
			case FeatType.DEFLECT_ATTACK:
			case FeatType.TUMBLE:
			case FeatType.CUNNING_DODGE:
				return SkillType.DEFENSE;
			case FeatType.MASTERS_EDGE:
			case FeatType.ARCANE_INTERFERENCE:
			case FeatType.CHAIN_CASTING:
			case FeatType.FORCE_OF_WILL:
				return SkillType.MAGIC;
			case FeatType.ENDURING_SOUL:
			case FeatType.BOILING_BLOOD:
			case FeatType.CONVICTION:
			case FeatType.FEEL_NO_PAIN:
				return SkillType.SPIRIT;
			case FeatType.CORNER_CLIMB:
			case FeatType.DANGER_SENSE:
			case FeatType.NECK_SNAP:
			case FeatType.DISARM_TRAP:
				return SkillType.STEALTH;
			default:
				return SkillType.NO_SKILL;
			}
		}
		public static string Name(FeatType type){
			switch(type){
			case FeatType.CORNER_CLIMB:
				return "Corner climb";
			case FeatType.QUICK_DRAW:
				return "Quick draw";
			case FeatType.CUNNING_DODGE:
				return "Cunning dodge";
			case FeatType.DANGER_SENSE:
				return "Danger sense";
			case FeatType.DEFLECT_ATTACK:
				return "Deflect attack";
			case FeatType.ENDURING_SOUL:
				return "Enduring soul";
			case FeatType.NECK_SNAP:
				return "Neck snap";
			case FeatType.BOILING_BLOOD:
				return "Boiling blood";
			case FeatType.WHIRLWIND_STYLE:
				return "Whirlwind style";
			case FeatType.LUNGE:
				return "Lunge";
			case FeatType.DRIVE_BACK:
				return "Drive back";
			case FeatType.ARMOR_MASTERY:
				return "Armor mastery";
			case FeatType.TUMBLE:
				return "Tumble";
			case FeatType.MASTERS_EDGE:
				return "Master's edge";
			case FeatType.ARCANE_INTERFERENCE:
				return "Arcane interference";
			case FeatType.CHAIN_CASTING:
				return "Chain casting";
			case FeatType.FORCE_OF_WILL:
				return "Force of will";
			case FeatType.CONVICTION:
				return "Conviction";
			case FeatType.FEEL_NO_PAIN:
				return "Feel no pain";
			case FeatType.DISARM_TRAP:
				return "Disarm trap";
			default:
				return "no feat";
			}
		}
		public static List<string> Description(FeatType type){
			switch(type){
			case FeatType.QUICK_DRAW:
				return new List<string>{
					"Switch from a melee weapon to another weapon instantly. (You",
					"can fire arrows without first switching to your bow. However,",
					"putting your bow away still takes time.)"};
			case FeatType.WHIRLWIND_STYLE:
				return new List<string>{
					"When you take a step, you automatically attack every enemy",
					"still adjacent to you."};
			case FeatType.LUNGE:
				return new List<string>{
					"Leap from one space away and attack your target with perfect",
					"accuracy. The intervening space must be unoccupied."};
			case FeatType.DRIVE_BACK:
				return new List<string>{
					"Enemies must yield ground in order to avoid your attacks, and",
					"cornered enemies are more vulnerable to critical hits. (When",
					"your target has nowhere to run, your attacks won't miss.)"};
				/*return new List<string>{
					"Enemies must yield ground in order to avoid your attacks.",
					"(If your target has nowhere to run, your attacks will",
					"automatically hit.)"};*/
			case FeatType.ARMOR_MASTERY:
				return new List<string>{
					"When your armor blocks an enemy's attack, you're twice as",
					"likely to score a critical hit on it next turn. The exhaustion",
					"total at which armor becomes too heavy is increased by 25%."};
			case FeatType.CUNNING_DODGE:
				return new List<string>{
					"Relying on mobility and tricks, you can entirely avoid the",
					"first attack of each foe you face."};
			case FeatType.DEFLECT_ATTACK:
				return new List<string>{
					"When an enemy attacks you, you might deflect the attack to",
					"any other enemy next to you. (The two enemies don't need to",
					"be adjacent to one another.)"};
				/*return new List<string>{
					"When an enemy attacks you, you might deflect the attack to",
					"another enemy (one that is adjacent to both you and your",
					"attacker)."};*/
			case FeatType.TUMBLE:
				return new List<string>{
					"Move up to two spaces while avoiding arrows. Takes two turns",
					"to perform."};
			case FeatType.MASTERS_EDGE:
				return new List<string>{
					"The first offensive spell you've learned is empowered - it'll",
					"deal 1d6 extra damage. (Affects the first spell in the list",
					"that deals damage directly.)"};
				/*return new List<string>{
					"The first offensive spell you've learned will deal 1d6 extra",
					"damage. (Affects the first spell in the list that deals damage",
					"directly.)"};*/
			case FeatType.ARCANE_INTERFERENCE:
				return new List<string>{
					"When you cast a spell, nearby enemy spellcasters are stunned",
					"for a short time. Additionally, they permanently lose their",
					"ability to cast the spell you just cast."};
				/*return new List<string>{
					"When you cast a spell, nearby enemies lose their ability to",
					"cast that spell. If this happens, your spells will be",
					"empowered for several turns."};*/
			case FeatType.CHAIN_CASTING:
				return new List<string>{
					"The mana cost of your spells is reduced by one if you cast",
					"a spell last turn. (This doesn't ever reduce mana costs",
					"to zero.)"};
			case FeatType.FORCE_OF_WILL:
				return new List<string>{
					"Exhaustion will never make you fail an attempt at casting.",
					"(Casting without mana still increases your exhaustion, and",
					"you can't cast if your exhaustion would exceed 100%.)"};
			/*return new List<string>{
					"Exhaustion will never make you fail an attempt at casting.",
					"(Casting without mana still increases your exhaustion.)"};*/
			case FeatType.CONVICTION:
				return new List<string>{
					"Each turn you're engaged in combat (attacking or being",
					"attacked) you gain 1 bonus Spirit, and bonus Combat skill",
					"equal to half that, rounded up."};
			case FeatType.ENDURING_SOUL:
				return new List<string>{
					"Your health recovers over time. You'll regain one HP every",
					"five turns until your total health is a multiple of 10."};
			case FeatType.FEEL_NO_PAIN:
				return new List<string>{
					"When your health becomes very low (less than 20%), you",
					"briefly enter a state of invulnerability. (For about 5 turns,",
					"you'll be immune to damage, but not other effects.)"};
			case FeatType.BOILING_BLOOD:
				return new List<string>{
					"Taking damage briefly increases your movement speed. (This",
					"effect can stack up to 5 times. At 5 stacks, your speed is",
					"doubled and you are immune to fire damage.)"};
			case FeatType.NECK_SNAP:
				return new List<string>{
					"Automatically perform a stealth kill when attacking an unaware",
					"medium humanoid. (Living enemies of approximately human size.)"};
			case FeatType.DISARM_TRAP:
				return new List<string>{
					"Alter a trap so that you can walk past it safely (but enemies",
					"still trigger it). You can use this feat again to disable it",
					"entirely."};
			case FeatType.CORNER_CLIMB:
				return new List<string>{
					"If you're in a corner (and in the dark), enemies can't see you",
					"unless they're adjacent to you."};
			case FeatType.DANGER_SENSE:
				return new List<string>{
					"Once you've seen or detected an enemy, you can sense where",
					"it's safe and where that enemy might detect you. Your torch",
					"must be extinguished while you're sneaking."};
			default:
				return null;
			}
		}
	}
}

