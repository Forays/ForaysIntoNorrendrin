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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Utilities;
using Forays;
using PosArrays;
using GLDrawing;
namespace Forays{
	public enum TileType{WALL,FLOOR,DOOR_O,DOOR_C,STAIRS,CHEST,FIREPIT,UNLIT_FIREPIT,STALAGMITE,FIRE_TRAP,TELEPORT_TRAP,LIGHT_TRAP,SLIDING_WALL_TRAP,GRENADE_TRAP,SHOCK_TRAP,ALARM_TRAP,DARKNESS_TRAP,POISON_GAS_TRAP,BLINDING_TRAP,ICE_TRAP,PHANTOM_TRAP,SCALDING_OIL_TRAP,FLING_TRAP,STONE_RAIN_TRAP,HIDDEN_DOOR,COMBAT_SHRINE,DEFENSE_SHRINE,MAGIC_SHRINE,SPIRIT_SHRINE,STEALTH_SHRINE,RUINED_SHRINE,SPELL_EXCHANGE_SHRINE,RUBBLE,FIRE_GEYSER,STATUE,POOL_OF_RESTORATION,FOG_VENT,POISON_GAS_VENT,STONE_SLAB,STONE_SLAB_OPEN,CHASM,BREACHED_WALL,WATER,ICE,CRACKED_WALL,BRUSH,POPPY_FIELD,JUNGLE,GRAVEL,BLAST_FUNGUS,GLOWING_FUNGUS,TOMBSTONE,GRAVE_DIRT,BARREL,STANDING_TORCH,VINE,POISON_BULB,WAX_WALL,DEMONIC_IDOL,FIRE_RIFT,DEMONSTONE};
	public enum FeatureType{GRENADE,TROLL_CORPSE,TROLL_BLOODWITCH_CORPSE,FOG,POISON_GAS,SLIME,TELEPORTAL,INACTIVE_TELEPORTAL,STABLE_TELEPORTAL,OIL,FIRE,BONES,WEB,PIXIE_DUST,FORASECT_EGG,SPORES,THICK_DUST,CONFUSION_GAS};
	public enum ActorType{PLAYER,SPECIAL,FIRE_DRAKE,GOBLIN,GIANT_BAT,LONE_WOLF,BLOOD_MOTH,DARKNESS_DWELLER,CARNIVOROUS_BRAMBLE,FROSTLING,SWORDSMAN,DREAM_WARRIOR,SPITTING_COBRA,KOBOLD,SPORE_POD,FORASECT,POLTERGEIST,CULTIST,GOBLIN_ARCHER,GOBLIN_SHAMAN,GOLDEN_DART_FROG,SKELETON,SHADOW,MIMIC,PHASE_SPIDER,ZOMBIE,BERSERKER,GIANT_SLUG,VULGAR_DEMON,BANSHEE,CAVERN_HAG,ROBED_ZEALOT,DIRE_RAT,SKULKING_KILLER,WILD_BOAR,TROLL,DREAM_SPRITE,CLOUD_ELEMENTAL,DERANGED_ASCETIC,ORC_GRENADIER,WARG,ALASI_SCOUT,CARRION_CRAWLER,MECHANICAL_KNIGHT,RUNIC_TRANSCENDENT,ALASI_BATTLEMAGE,ALASI_SOLDIER,SKITTERMOSS,STONE_GOLEM,MUD_ELEMENTAL,FLAMETONGUE_TOAD,ENTRANCER,OGRE_BARBARIAN,SNEAK_THIEF,LASHER_FUNGUS,CRUSADING_KNIGHT,TROLL_BLOODWITCH,LUMINOUS_AVENGER,MARBLE_HORROR,CORROSIVE_OOZE,PYREN_ARCHER,SPELLMUDDLE_PIXIE,ALASI_SENTINEL,NOXIOUS_WORM,CYCLOPEAN_TITAN,VAMPIRE,ORC_WARMAGE,NECROMANCER,STALKING_WEBSTRIDER,ORC_ASSASSIN,CORPSETOWER_BEHEMOTH,MACHINE_OF_WAR,IMPOSSIBLE_NIGHTMARE,DREAM_WARRIOR_CLONE,DREAM_SPRITE_CLONE,MUD_TENTACLE,MARBLE_HORROR_STATUE,GHOST,HOMUNCULUS,BLADE,FINAL_LEVEL_CULTIST,MINOR_DEMON,FROST_DEMON,BEAST_DEMON,DEMON_LORD,PHANTOM,PHANTOM_ZOMBIE,PHANTOM_CRUSADER,PHANTOM_TIGER,PHANTOM_WASP,PHANTOM_BEHEMOTH,PHANTOM_BLIGHTWING,PHANTOM_SWORDMASTER,PHANTOM_ARCHER,PHANTOM_CONSTRICTOR};
	public enum AttrType{STEALTHY,NONLIVING,PLANTLIKE,MEDIUM_HUMANOID,HUMANOID_INTELLIGENCE,MINDLESS,AGGRESSIVE,TERRITORIAL,AVOIDS_LIGHT,KEEPS_DISTANCE,NONEUCLIDEAN_MOVEMENT,KEEN_SENSES,BLINDSIGHT,SMALL,FLYING,DESCENDING,WANDERING,IMMOBILE,CAN_DODGE,SHADOW_CLOAK,INVISIBLE,BRUTISH_STRENGTH,MYSTIC_MIND,VIGOR,FLYING_LEAP,SILENCED,NOTICED,PLAYER_NOTICED,DANGER_SENSED,SHINING,LOW_LIGHT_VISION,REGENERATING,REGENERATES_FROM_DEATH,REASSEMBLES,NO_ITEM,STUNNED,PARALYZED,BLIND,POISONED,FROZEN,CHILLED,SLIMED,OIL_COVERED,BURNING,SLOWED,BLEEDING,POPPY_COUNTER,ASLEEP,AGGRAVATING,DETECTING_MONSTERS,DETECTING_MOVEMENT,TELEPORTING,VULNERABLE,SUSCEPTIBLE_TO_CRITS,LIGHT_SENSITIVE,PSEUDO_VAMPIRIC,DESTROYED_BY_SUNLIGHT,STONEFORM,DIM_VISION,DIM_LIGHT,CONFUSED,ENRAGED,FIERY_ARROWS,DULLS_BLADES,IMMUNE_BURNING,IMMUNE_FIRE,IMMUNE_COLD,IMMUNE_ELECTRICITY,MENTAL_IMMUNITY,RESIST_WEAPONS,IMMUNE_ARROWS,RESIST_NECK_SNAP,COMBO_ATTACK,COOLDOWN_1,COOLDOWN_2,COOLDOWN_3,LIFESPAN,RADIANT_HALO,SPORE_BURST,CAN_POISON_WEAPONS,ACIDIFIED,SILENCE_AURA,TERRIFYING,DAMAGE_RESISTANCE,BLOCKING,SHIELDED,MINOR_IMMUNITY,MECHANICAL_SHIELD,TURNS_HERE,TURNS_VISIBLE,RESTING,RUNNING,WAITING,AUTOEXPLORE,TUMBLING,BLOOD_BOILED,SHADOWSIGHT,IN_COMBAT,CONVICTION,KILLSTREAK,EMPOWERED_SPELLS,BANDAGED,PERMANENT_DAMAGE,SWITCHING_ARMOR,JUST_GRABBED,JUST_TELEPORTED,JUST_FLUNG,JUST_BITTEN,JUST_SEARED,JUST_AWOKE,JUST_LEARNED_FEEL_NO_PAIN,AGGRESSION_MESSAGE_PRINTED,NO_PLATE_ARMOR_NOISE,GRAVEL_MESSAGE_COOLDOWN,SILENCE_AURA_MESSAGE_COOLDOWN,TITAN_MESSAGE_COOLDOWN,LUNGING_AUTO_HIT,TELEKINETICALLY_THROWN,SELF_TK_NO_DAMAGE,BURROWING,ROOTS,DODGED,CHAIN_CAST,IGNORES_QUIET_SOUNDS,ALERTED,SEES_ADJACENT_PLAYER,HEARD_PLAYER,DIRECTION_OF_PREVIOUS_TILE,FOLLOW_DIRECTION_EXITED,AMNESIA_STUN,GRABBED,GRABBING,BONUS_COMBAT,BONUS_DEFENSE,BONUS_MAGIC,BONUS_SPIRIT,BONUS_STEALTH,INVULNERABLE,SMALL_GROUP,MEDIUM_GROUP,LARGE_GROUP,TURN_INTO_CORPSE,CORPSE,NO_CORPSE_KNOCKBACK,BOSS_MONSTER,NUM_ATTRS,NO_ATTR};
	public enum SpellType{RADIANCE,FORCE_PALM,DETECT_MOVEMENT,FLYING_LEAP,MERCURIAL_SPHERE,GREASE,BLINK,FREEZE,SCORCH,LIGHTNING_BOLT,MAGIC_HAMMER,PORTAL,PASSAGE,AMNESIA,STONE_SPIKES,SHADOWSIGHT,BLIZZARD,COLLAPSE,DOOM,TELEKINESIS,NUM_SPELLS,NO_SPELL};
	public enum SkillType{COMBAT,DEFENSE,MAGIC,SPIRIT,STEALTH,NUM_SKILLS,NO_SKILL};
	public enum FeatType{QUICK_DRAW,WHIRLWIND_STYLE,LUNGE,DRIVE_BACK,ARMOR_MASTERY,CUNNING_DODGE,DEFLECT_ATTACK,TUMBLE,MASTERS_EDGE,ARCANE_INTERFERENCE,CHAIN_CASTING,FORCE_OF_WILL,CONVICTION,ENDURING_SOUL,FEEL_NO_PAIN,BOILING_BLOOD,NECK_SNAP,DISARM_TRAP,CORNER_CLIMB,DANGER_SENSE,NUM_FEATS,NO_FEAT};
	public enum ConsumableType{HEALING,REGENERATION,STONEFORM,VAMPIRISM,BRUTISH_STRENGTH,ROOTS,HASTE,SILENCE,CLOAKING,MYSTIC_MIND,BLINKING,PASSAGE,TIME,KNOWLEDGE,SUNLIGHT,DARKNESS,RENEWAL,CALLING,TRAP_CLEARING,ENCHANTMENT,THUNDERCLAP,FIRE_RING,RAGE,FREEZING,FLAMES,FOG,DETONATION,BREACHING,SHIELDING,TELEPORTAL,PAIN,CONFUSION,BLADES,DUST_STORM,INVISIBILITY,FLESH_TO_FIRE,WEBS,SLUMBER,REACH,TELEKINESIS,BANDAGES,FLINT_AND_STEEL,BLAST_FUNGUS,MAGIC_TRINKET};
	public enum WeaponType{SWORD,MACE,DAGGER,STAFF,BOW,NUM_WEAPONS,NO_WEAPON};
	public enum ArmorType{LEATHER,CHAINMAIL,FULL_PLATE,NUM_ARMORS,NO_ARMOR};
	public enum EnchantmentType{CHILLING,ECHOES,DISRUPTION,PRECISION,VICTORY,NUM_ENCHANTMENTS,NO_ENCHANTMENT};
	public enum EquipmentStatus{DULLED,POSSESSED,HEAVY,MERCIFUL,NEGATED,STUCK,POISONED,INFESTED,RUSTED,DAMAGED,WORN_OUT,WEAK_POINT,LOW_ON_ARROWS,ALMOST_OUT_OF_ARROWS,ONE_ARROW_LEFT,OUT_OF_ARROWS,NUM_STATUS};
	public enum MagicTrinketType{PENDANT_OF_LIFE,CLOAK_OF_SAFETY,BRACERS_OF_ARROW_DEFLECTION,CIRCLET_OF_THE_THIRD_EYE,RING_OF_KEEN_SIGHT,RING_OF_THE_LETHARGIC_FLAME,LENS_OF_SCRYING,BELT_OF_WARDING,BOOTS_OF_GRIPPING,NUM_MAGIC_TRINKETS,NO_MAGIC_TRINKET};
	public enum DamageType{NORMAL,FIRE,COLD,ELECTRIC,POISON,MAGIC,NONE};
	public enum DamageClass{PHYSICAL,MAGICAL,NO_TYPE};
	public enum AttackEffect{STUN,ONE_TURN_STUN,MAX_DAMAGE,PERCENT_DAMAGE,WEAK_POINT,WORN_OUT,REDUCE_ACCURACY,DRAIN_LIFE,GRAB,CHILL,FREEZE,INFLICT_VULNERABILITY,TRIP,KNOCKBACK,STRONG_KNOCKBACK,IGNITE,DIM_VISION,SWAP_POSITIONS,SLIME,MAKE_NOISE,BLIND,PERMANENT_BLIND,SLOW,POISON,PARALYZE,ONE_TURN_PARALYZE,STALAGMITES,FLING,PULL,SILENCE,INFEST,DISRUPTION,VICTORY,ACID,CONFUSE,BLEED,ONE_HP,STEAL,EXHAUST,NO_CRIT};
	public enum EventType{ANY_EVENT,MOVE,REMOVE_ATTR,REMOVE_GAS,CHECK_FOR_HIDDEN,RELATIVELY_SAFE,POLTERGEIST,MIMIC,REGENERATING_FROM_DEATH,REASSEMBLING,GRENADE,BLAST_FUNGUS,STALAGMITE,FIRE_GEYSER,FIRE_GEYSER_ERUPTION,FOG_VENT,POISON_GAS_VENT,STONE_SLAB,MARBLE_HORROR,FIRE,NORMAL_LIGHTING,TELEPORTAL,BREACH,GRAVE_DIRT,POPPIES,TOMBSTONE_GHOST,SHIELDING,BURROWING,FINAL_LEVEL_SPAWN_CULTISTS,SPAWN_WANDERING_MONSTER};
	public enum OptionType{NO_WALL_SLIDING,AUTOPICKUP,TOP_ROW_MOVEMENT,NO_CONFIRMATION_BEFORE_RESTING,NEVER_DISPLAY_TIPS,ALWAYS_RESET_TIPS,DARK_GRAY_UNSEEN,HIDE_VIEW_MORE,DISABLE_GRAPHICS};
	public class Game{
		public Map M;
		public Queue Q;
		public Buffer B;
		public Actor player;

		static void Main(string[] args){
			//Screen.GLMode = false;
			{
				int os = (int)Environment.OSVersion.Platform;
				if(os == 4 || os == 6 ||  os == 128){
					Global.LINUX = true;
				}
			}
			if(args != null && args.Length > 0){
				if(args[0] == "-c" || args[0] == "--console"){
					Screen.GLMode = false;
				}
				if(args[0] == "-g" || args[0] == "--gl"){
					Screen.GLMode = true;
				}
			}
			if(!Screen.GLMode){
				if(Global.LINUX){
					Screen.CursorVisible = false;
					Screen.SetCursorPosition(0,0); //todo: this should still work fine but it's worth a verification.
					if(Console.BufferWidth < Global.SCREEN_W || Console.BufferHeight < Global.SCREEN_H){
						Console.Write("Please resize your terminal to {0}x{1}, then press any key.",Global.SCREEN_W,Global.SCREEN_H);
						Screen.SetCursorPosition(0,1);
						Console.Write("         Current dimensions are {0}x{1}.".PadRight(57),Console.BufferWidth,Console.BufferHeight);
						Input.ReadKey();
						Screen.SetCursorPosition(0,0);
						if(Console.BufferWidth < Global.SCREEN_W || Console.BufferHeight < Global.SCREEN_H){
							Environment.Exit(0);
						}
					}
					Screen.Blank();
					Console.TreatControlCAsInput = true;
				}
				else{
					if(Type.GetType("Mono.Runtime") != null){ // If you try to resize the Windows Command Prompt using Mono, it crashes, so just switch
						Screen.GLMode = true; // back to GL mode in that case. (Fortunately, nobody uses Mono on Windows unless they're compiling a project in MD/XS.)
					}
					else{
						Screen.CursorVisible = false;
						Console.Title = "Forays into Norrendrin";
						Console.BufferHeight = Global.SCREEN_H;
						Console.SetWindowSize(Global.SCREEN_W,Global.SCREEN_H);
						Console.TreatControlCAsInput = true;
					}
				}
			}
			if(Screen.GLMode){
				ToolkitOptions.Default.EnableHighResolution = false;
				int height_px = Global.SCREEN_H * 16;
				int width_px = Global.SCREEN_W * 8;
				Screen.gl = new GLWindow(width_px,height_px,"Forays into Norrendrin");
				Screen.gl.Icon = new System.Drawing.Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(Global.ForaysImageResources + "forays.ico"));
				Screen.gl.ResizingPreference = ResizeOption.SnapWindow;
				Screen.gl.ResizingFullScreenPreference = ResizeOption.AddBorder;
				Screen.gl.KeyDown += Input.KeyDownHandler;
				Screen.gl.Mouse.Move += Input.MouseMoveHandler;
				Screen.gl.Mouse.ButtonUp += Input.MouseClickHandler;
				Screen.gl.Mouse.WheelChanged += Input.MouseWheelHandler;
				Screen.gl.MouseLeave += Input.MouseLeaveHandler;
				Screen.gl.Closing += Input.OnClosing;
				Screen.gl.FinalResize += Input.HandleResize;
				Screen.textSurface = Surface.Create(Screen.gl,Global.ForaysImageResources + "font8x16.png",true,Shader.AAFontFS(),false,2,4,4);
				SpriteType.DefineSingleRowSprite(Screen.textSurface,8,1);
				CellLayout.CreateGrid(Screen.textSurface,Global.SCREEN_H,Global.SCREEN_W,16,8,0,0);
				Screen.textSurface.SetEasyLayoutCounts(Global.SCREEN_H * Global.SCREEN_W);
				Screen.textSurface.DefaultUpdatePositions();
				Screen.textSurface.SetDefaultSpriteType(0);
				Screen.textSurface.SetDefaultSprite(32); //space
				Screen.textSurface.SetDefaultOtherData(new List<float>(Color.Gray.GetFloatValues()),new List<float>(Color.Black.GetFloatValues()));
				Screen.textSurface.DefaultUpdateOtherData();
				Screen.gl.Surfaces.Add(Screen.textSurface);
				Screen.cursorSurface = Surface.Create(Screen.gl,Global.ForaysImageResources + "font8x16.png",true,Shader.AAFontFS(),false,2,4,4);
				Screen.cursorSurface.texture = Screen.textSurface.texture;
				CellLayout.CreateGrid(Screen.cursorSurface,1,1,2,8,0,0);
				Screen.cursorSurface.SetEasyLayoutCounts(1);
				Screen.cursorSurface.DefaultUpdatePositions();
				Screen.cursorSurface.SetDefaultSpriteType(0);
				Screen.cursorSurface.SetDefaultSprite(32);
				Screen.cursorSurface.SetDefaultOtherData(new List<float>(Color.Black.GetFloatValues()),new List<float>(Color.Gray.GetFloatValues()));
				Screen.cursorSurface.DefaultUpdateOtherData();
				Screen.gl.Surfaces.Add(Screen.cursorSurface);
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha,BlendingFactorDest.OneMinusSrcAlpha);
				Screen.gl.Visible = true;
				Global.Timer = new Stopwatch();
				Global.Timer.Start();
				Screen.CursorVisible = false;
			}
			Input.LoadKeyRebindings();
			TitleScreen();
			MainMenu();
		}
		static void TitleScreen(){
			if(Screen.GLMode){
				const int logoW = 512;
				const int logoH = 412;
				Surface logo = Surface.Create(Screen.gl,Global.ForaysImageResources + "logo.png",true,Shader.DefaultFS(),false,2);
				SpriteType.DefineSingleRowSprite(logo,logoW);
				CellLayout.CreateGrid(logo,1,1,logoH,logoW,(Screen.gl.ClientRectangle.Height - logoH)/16,(Screen.gl.ClientRectangle.Width - logoW)/2);
				logo.SetEasyLayoutCounts(1);
				logo.SetDefaultSpriteType(0);
				logo.SetDefaultSprite(0);
				logo.DefaultUpdate();
				Screen.WriteString(Global.SCREEN_H-2,Global.SCREEN_W-14,"version " + Global.VERSION + " ",Color.DarkGray);
				Screen.WriteString(Global.SCREEN_H-1,Global.SCREEN_W-19,"by Derrick Creamer ",Color.DarkGray);
				Screen.WriteString(Global.SCREEN_H-1,0,"logo by Soundlust",Color.DarkerGray);
				Input.ReadKey();
				Screen.gl.Surfaces.Remove(logo);
			}
			else{
				for(int i=0;i<Global.title[0].GetLength(0);++i){
					for(int j=0;j<Global.title[0][0].Length;++j){
						if(Global.title[0][i][j] != ' '){
							const int row_offset = 4;
							const int col_offset = 19;
							if(Global.title[0][i][j] == '#' && (!Global.LINUX || Screen.GLMode)){
								Screen.WriteChar(i+row_offset,j+col_offset,' ',Color.Black,Color.Yellow);
							}
							else{
								Screen.WriteChar(i+row_offset,j+col_offset,Global.title[0][i][j],Color.Yellow);
							}
						}
					}
				}
				for(int i=0;i<Global.title[1].GetLength(0);++i){
					for(int j=0;j<Global.title[1][0].Length;++j){
						Screen.WriteChar(i+19,j+37,Global.title[1][i][j],Color.Green);
					}
				}
				Screen.WriteString(Global.SCREEN_H-3,Global.SCREEN_W-14,"version " + Global.VERSION + " ",Color.DarkGray);
				Screen.WriteString(Global.SCREEN_H-2,Global.SCREEN_W-19,"by Derrick Creamer ",Color.DarkGray);
				Input.ReadKey();
			}
		}
		static void MainMenu(){
			ConsoleKeyInfo command;
			string recentname = "".PadRight(30);
			int recentdepth = -1;
			char recentwin = '-';
			string recentcause = "";
			bool on_highscore_list = false;
			MouseUI.PushButtonMap();
			while(true){
				Screen.Blank();
				int row = 8;
				int col = (Global.SCREEN_W - 28) / 2; //centering "Forays into Norrendrin x.y.z", which is 28 chars.
				Screen.WriteString(row++,col,new cstr(Color.Yellow,"Forays into Norrendrin " + Global.VERSION));
				Screen.WriteString(row++,col,new cstr(Color.Green,"".PadRight(28,'-')));
				col += 4; //recenter for menu options
				row++;
				bool saved_game = File.Exists("forays.sav");
				if(!saved_game){
					Screen.WriteString(row++,col,"[a] Start a new game");
				}
				else{
					Screen.WriteString(row++,col,"[a] Resume saved game");
				}
				Screen.WriteString(row++,col,"[b] How to play");
				Screen.WriteString(row++,col,"[c] High scores");
				Screen.WriteString(row++,col,"[d] Quit");
				for(int i=0;i<4;++i){
					Screen.WriteChar(i+row-4,col+1,new colorchar(Color.Cyan,(char)(i+'a')));
					MouseUI.CreateButton((ConsoleKey)(i + ConsoleKey.A),false,i+row-4,0,1,Global.SCREEN_W);
				}
				Screen.ResetColors();
				Screen.SetCursorPosition(Global.MAP_OFFSET_COLS,Global.MAP_OFFSET_ROWS+8);
				command = Input.ReadKey();
				switch(command.KeyChar){
				case 'a':
				{
					Global.GAME_OVER = false;
					Global.BOSS_KILLED = false;
					Global.SAVING = false;
					Global.LoadOptions();
					Game game = new Game();
					Actor.attack[ActorType.PLAYER] = new List<AttackInfo>{new AttackInfo(100,2,AttackEffect.NO_CRIT,"& hit *","& miss *","")};
					if(!saved_game){
						game.player = new Actor(ActorType.PLAYER,"you",'@',Color.White,100,100,0,0,AttrType.HUMANOID_INTELLIGENCE);
						game.player.inv = new List<Item>();
						Actor.feats_in_order = new List<FeatType>();
						Actor.spells_in_order = new List<SpellType>();
						game.player.weapons.AddLast(new Weapon(WeaponType.SWORD));
						game.player.weapons.AddLast(new Weapon(WeaponType.MACE));
						game.player.weapons.AddLast(new Weapon(WeaponType.DAGGER));
						game.player.weapons.AddLast(new Weapon(WeaponType.STAFF));
						game.player.weapons.AddLast(new Weapon(WeaponType.BOW));
						game.player.armors.AddLast(new Armor(ArmorType.LEATHER));
						game.player.armors.AddLast(new Armor(ArmorType.CHAINMAIL));
						game.player.armors.AddLast(new Armor(ArmorType.FULL_PLATE));
					}
					game.M = new Map(game);
					game.B = new Buffer(game);
					game.Q = new Queue(game);
					Map.Q = game.Q;
					Map.B = game.B;
					PhysicalObject.M = game.M;
					PhysicalObject.B = game.B;
					PhysicalObject.Q = game.Q;
					PhysicalObject.player = game.player;
					Event.Q = game.Q;
					Event.B = game.B;
					Event.M = game.M;
					Event.player = game.player;
					Fire.fire_event = null;
					Fire.burning_objects = new List<PhysicalObject>();
					if(!saved_game){
						Actor.player_name = "";
						if(File.Exists("name.txt")){
							StreamReader file = new StreamReader("name.txt");
							string base_name = file.ReadLine();
							if(base_name == "%random%"){
								Actor.player_name = Global.GenerateCharacterName();
							}
							else{
								Actor.player_name = base_name;
							}
							int num = 0;
							if(base_name != "%random%" && file.Peek() != -1){
								num = Convert.ToInt32(file.ReadLine());
								if(num > 1){
									Actor.player_name = Actor.player_name + " " + Global.RomanNumeral(num);
								}
							}
							file.Close();
							if(num > 0){
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine(base_name);
								fileout.WriteLine(num+1);
								fileout.Close();
							}
						}
						if(Actor.player_name == ""){
							MouseUI.PushButtonMap(MouseMode.NameEntry);
							Screen.Blank();
							/*for(int i=4;i<=7;++i){
								Screen.WriteMapString(i,0,"".PadToMapSize());
							}*/
							string s = "";
							int name_option = 0;
							int c = 3;
							while(true){
								Screen.WriteMapString(4,c,"Enter name: ");
								if(s == ""){
									Screen.WriteMapString(6,c,"(Press [Enter] for a random name)".GetColorString());
								}
								else{
									Screen.WriteMapString(6,c,"(Press [Enter] when finished)    ".GetColorString());
								}
								List<string> name_options = new List<string>{"Default: Choose a new name for each character","Static:  Use this name for every character","Legacy:  Name all future characters after this one","Random:  Name all future characters randomly"};
								for(int i=0;i<4;++i){
									Color option_color = Color.DarkGray;
									if(i == name_option){
										option_color = Color.White;
									}
									Screen.WriteMapString(15+i,c,name_options[i],option_color);
								}
								Screen.WriteMapString(20,c,"(Press [Tab] to change naming preference)".GetColorString());
								if(name_option != 0){
									Screen.WriteMapString(22,c-5,"(To stop naming characters automatically, delete name.txt)",Color.Green);
								}
								else{
									Screen.WriteMapString(22,c-5,"".PadToMapSize());
								}
								Screen.WriteMapString(4,c+12,s.PadRight(26));
								Screen.SetCursorPosition(c + Global.MAP_OFFSET_COLS + 12 + s.Length,Global.MAP_OFFSET_ROWS + 4);
								MouseUI.CreateButton(ConsoleKey.Enter,false,6+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.Tab,false,20+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F21,false,15+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F22,false,16+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F23,false,17+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F24,false,18+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								Screen.CursorVisible = true;
								command = Input.ReadKey();
								if((command.KeyChar >= '!' && command.KeyChar <= '~') || command.KeyChar == ' '){
									if(s.Length < 26){
										s = s + command.KeyChar;
									}
								}
								else{
									if(command.Key == ConsoleKey.Backspace && s.Length > 0){
										s = s.Substring(0,s.Length-1);
									}
									else{
										if(command.Key == ConsoleKey.Escape){
											s = "";
										}
										else{
											if(command.Key == ConsoleKey.Tab){
												name_option = (name_option + 1) % 4;
											}
											else{
												if(command.Key == ConsoleKey.Enter){
													if(s.Length == 0){
														s = Global.GenerateCharacterName();
													}
													else{
														Actor.player_name = s;
														break;
													}
												}
												else{
													switch(command.Key){
													case ConsoleKey.F21:
														name_option = 0;
														break;
													case ConsoleKey.F22:
														name_option = 1;
														break;
													case ConsoleKey.F23:
														name_option = 2;
														break;
													case ConsoleKey.F24:
														name_option = 3;
														break;
													}
												}
											}
										}
									}
								}
							}
							MouseUI.PopButtonMap();
							switch(name_option){
							case 1: //static
							{
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine(s);
								fileout.WriteLine(0);
								fileout.Close();
								break;
							}
							case 2: //legacy
							{
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine(s);
								fileout.WriteLine(2);
								fileout.Close();
								break;
							}
							case 3: //random
							{
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine("%random%");
								fileout.WriteLine(0);
								fileout.Close();
								break;
							}
							}
						}
						{
							Event e = new Event(game.player,0,EventType.MOVE);
							e.tiebreaker = 0;
							game.Q.Add(e);
						}
						Item.GenerateUnIDedNames();
						game.M.GenerateLevelTypes();
						game.M.GenerateLevel();
						game.player.UpdateRadius(0,6,true);
						Item.Create(ConsumableType.BANDAGES,game.player).other_data = 5;
						Item.Create(ConsumableType.FLINT_AND_STEEL,game.player).other_data = 3;
						game.player.inv[0].revealed_by_light = true;
						game.player.inv[1].revealed_by_light = true;
					}
					else{ //loading
						FileStream file = new FileStream("forays.sav",FileMode.Open);
						BinaryReader b = new BinaryReader(file);
						Dictionary<int,PhysicalObject> id = new Dictionary<int, PhysicalObject>();
						id.Add(0,null);
						Dict<PhysicalObject,int> missing_target_id = new Dict<PhysicalObject, int>();
						List<Actor> need_targets = new List<Actor>();
						Dict<PhysicalObject,int> missing_location_id = new Dict<PhysicalObject, int>();
						List<Actor> need_location = new List<Actor>();
						Actor.player_name = b.ReadString();
						game.M.currentLevelIdx = b.ReadInt32();
						game.M.level_types = new List<LevelType>();
						int numLevelTypes = b.ReadInt32();
						for(int i=0;i<numLevelTypes;++i){
							game.M.level_types.Add((LevelType)b.ReadInt32());
						}
						game.M.wiz_lite = b.ReadBoolean();
						game.M.wiz_dark = b.ReadBoolean();
						for(int i=0;i<Global.ROWS;++i){
							for(int j=0;j<Global.COLS;++j){
								game.M.last_seen[i,j].c = b.ReadChar();
								game.M.last_seen[i,j].color = (Color)b.ReadInt32();
								game.M.last_seen[i,j].bgcolor = (Color)b.ReadInt32();
							}
						}
						if(game.M.CurrentLevelType == LevelType.Final){
							game.M.final_level_cultist_count = new int[5];
							for(int i=0;i<5;++i){
								game.M.final_level_cultist_count[i] = b.ReadInt32();
							}
							game.M.final_level_demon_count = b.ReadInt32();
							game.M.final_level_clock = b.ReadInt32();
						}
						Actor.feats_in_order = new List<FeatType>();
						Actor.spells_in_order = new List<SpellType>();
						int num_featlist = b.ReadInt32();
						for(int i=0;i<num_featlist;++i){
							Actor.feats_in_order.Add((FeatType)b.ReadInt32());
						}
						int num_spelllist = b.ReadInt32();
						for(int i=0;i<num_spelllist;++i){
							Actor.spells_in_order.Add((SpellType)b.ReadInt32());
						}
						int num_actor_tiebreakers = b.ReadInt32();
						Actor.tiebreakers = new List<Actor>(num_actor_tiebreakers);
						for(int i=0;i<num_actor_tiebreakers;++i){
							int ID = b.ReadInt32();
							if(ID != 0){
								Actor a = new Actor();
								id.Add(ID,a);
								a.row = b.ReadInt32();
								a.col = b.ReadInt32();
								if(a.row >= 0 && a.row < Global.ROWS && a.col >= 0 && a.col < Global.COLS){
									game.M.actor[a.row,a.col] = a;
								}
								Actor.tiebreakers.Add(a);
								a.name = b.ReadString();
								a.the_name = b.ReadString();
								a.a_name = b.ReadString();
								a.symbol = b.ReadChar();
								a.color = (Color)b.ReadInt32();
								a.type = (ActorType)b.ReadInt32();
								if(a.type == ActorType.PLAYER){
									game.player = a;
									Actor.player = a;
									Buffer.player = a;
									Item.player = a;
									Map.player = a;
									Event.player = a;
									Tile.player = a;
								}
								a.maxhp = b.ReadInt32();
								a.curhp = b.ReadInt32();
								a.maxmp = b.ReadInt32();
								a.curmp = b.ReadInt32();
								a.speed = b.ReadInt32();
								a.light_radius = b.ReadInt32();
								int target_ID = b.ReadInt32();
								if(id.ContainsKey(target_ID)){
									a.target = (Actor)id[target_ID];
								}
								else{
									a.target = null;
									need_targets.Add(a);
									missing_target_id[a] = target_ID;
								}
								int num_items = b.ReadInt32();
								for(int j=0;j<num_items;++j){
									int item_id = b.ReadInt32();
									if(item_id != 0){
										Item item = new Item();
										id.Add(item_id,item);
										item.row = b.ReadInt32();
										item.col = b.ReadInt32();
										item.name = b.ReadString();
										item.the_name = b.ReadString();
										item.a_name = b.ReadString();
										item.symbol = b.ReadChar();
										item.color = (Color)b.ReadInt32();
										item.light_radius = b.ReadInt32();
										item.type = (ConsumableType)b.ReadInt32();
										item.quantity = b.ReadInt32();
										item.charges = b.ReadInt32();
										item.other_data = b.ReadInt32();
										item.ignored = b.ReadBoolean();
										item.do_not_stack = b.ReadBoolean();
										item.revealed_by_light = b.ReadBoolean();
										a.inv.Add(item);
									}
								}
								int num_attrs = b.ReadInt32();
								for(int j=0;j<num_attrs;++j){
									AttrType t = (AttrType)b.ReadInt32();
									a.attrs[t] = b.ReadInt32();
								}
								int num_skills = b.ReadInt32();
								for(int j=0;j<num_skills;++j){
									SkillType t = (SkillType)b.ReadInt32();
									a.skills[t] = b.ReadInt32();
								}
								int num_feats = b.ReadInt32();
								for(int j=0;j<num_feats;++j){
									FeatType t = (FeatType)b.ReadInt32();
									a.feats[t] = b.ReadBoolean();
								}
								int num_spells = b.ReadInt32();
								for(int j=0;j<num_spells;++j){
									SpellType t = (SpellType)b.ReadInt32();
									a.spells[t] = b.ReadBoolean();
								}
								a.exhaustion = b.ReadInt32();
								a.time_of_last_action = b.ReadInt32();
								a.recover_time = b.ReadInt32();
								int path_count = b.ReadInt32();
								for(int j=0;j<path_count;++j){
									int path_row = b.ReadInt32();
									int path_col = b.ReadInt32();
									a.path.Add(new pos(path_row,path_col));
								}
								int location_ID = b.ReadInt32();
								if(id.ContainsKey(location_ID)){
									a.target_location = (Tile)id[location_ID];
								}
								else{
									a.target_location = null;
									need_location.Add(a);
									missing_location_id[a] = location_ID;
								}
								a.player_visibility_duration = b.ReadInt32();
								int num_weapons = b.ReadInt32();
								for(int j=0;j<num_weapons;++j){
									Weapon w = new Weapon(WeaponType.NO_WEAPON);
									w.type = (WeaponType)b.ReadInt32();
									w.enchantment = (EnchantmentType)b.ReadInt32();
									int num_statuses = b.ReadInt32();
									for(int k=0;k<num_statuses;++k){
										EquipmentStatus st = (EquipmentStatus)b.ReadInt32();
										bool has_st = b.ReadBoolean();
										w.status[st] = has_st;
									}
									a.weapons.AddLast(w);
								}
								int num_armors = b.ReadInt32();
								for(int j=0;j<num_armors;++j){
									Armor ar = new Armor(ArmorType.NO_ARMOR);
									ar.type = (ArmorType)b.ReadInt32();
									ar.enchantment = (EnchantmentType)b.ReadInt32();
									int num_statuses = b.ReadInt32();
									for(int k=0;k<num_statuses;++k){
										EquipmentStatus st = (EquipmentStatus)b.ReadInt32();
										bool has_st = b.ReadBoolean();
										ar.status[st] = has_st;
									}
									a.armors.AddLast(ar);
								}
								int num_magic_trinkets = b.ReadInt32();
								for(int j=0;j<num_magic_trinkets;++j){
									a.magic_trinkets.Add((MagicTrinketType)b.ReadInt32());
								}
							}
							else{
								Actor.tiebreakers.Add(null);
							}
						}
						int num_groups = b.ReadInt32();
						for(int i=0;i<num_groups;++i){
							List<Actor> group = new List<Actor>();
							int group_size = b.ReadInt32();
							for(int j=0;j<group_size;++j){
								group.Add((Actor)id[b.ReadInt32()]);
							}
							foreach(Actor a in group){
								a.group = group;
							}
						}
						int num_tiles = b.ReadInt32();
						for(int i=0;i<num_tiles;++i){
							Tile t = new Tile();
							int ID = b.ReadInt32();
							id.Add(ID,t);
							t.row = b.ReadInt32();
							t.col = b.ReadInt32();
							game.M.tile[t.row,t.col] = t;
							t.name = b.ReadString();
							t.the_name = b.ReadString();
							t.a_name = b.ReadString();
							t.symbol = b.ReadChar();
							t.color = (Color)b.ReadInt32();
							t.light_radius = b.ReadInt32();
							t.type = (TileType)b.ReadInt32();
							t.passable = b.ReadBoolean();
							t.SetInternalOpacity(b.ReadBoolean());
							t.SetInternalSeen(b.ReadBoolean());
							//t.seen = b.ReadBoolean();
							t.revealed_by_light = b.ReadBoolean();
							t.solid_rock = b.ReadBoolean();
							t.light_value = b.ReadInt32();
							t.direction_exited = b.ReadInt32();
							if(b.ReadBoolean()){ //indicates a toggles_into value
								t.toggles_into = (TileType)b.ReadInt32();
							}
							else{
								t.toggles_into = null;
							}
							int item_id = b.ReadInt32();
							if(item_id != 0){
								t.inv = new Item();
								id.Add(item_id,t.inv);
								t.inv.row = b.ReadInt32();
								t.inv.col = b.ReadInt32();
								t.inv.name = b.ReadString();
								t.inv.the_name = b.ReadString();
								t.inv.a_name = b.ReadString();
								t.inv.symbol = b.ReadChar();
								t.inv.color = (Color)b.ReadInt32();
								t.inv.light_radius = b.ReadInt32();
								t.inv.type = (ConsumableType)b.ReadInt32();
								t.inv.quantity = b.ReadInt32();
								t.inv.charges = b.ReadInt32();
								t.inv.other_data = b.ReadInt32();
								t.inv.ignored = b.ReadBoolean();
								t.inv.do_not_stack = b.ReadBoolean();
								t.inv.revealed_by_light = b.ReadBoolean();
							}
							else{
								t.inv = null;
							}
							int num_features = b.ReadInt32();
							for(int j=0;j<num_features;++j){
								t.features.Add((FeatureType)b.ReadInt32());
							}
						}
						foreach(Actor a in need_targets){
							if(id.ContainsKey(missing_target_id[a])){
								a.target = (Actor)id[missing_target_id[a]];
							}
							else{
								throw new Exception("Error: some actors weren't loaded(1). ");
							}
						}
						foreach(Actor a in need_location){
							if(id.ContainsKey(missing_location_id[a])){
								a.target_location = (Tile)id[missing_location_id[a]];
							}
							else{
								throw new Exception("Error: some tiles weren't loaded(2). ");
							}
						}
						int game_turn = b.ReadInt32();
						game.Q.turn = -1; //this keeps events from being added incorrectly to the front of the queue while loading. turn is set correctly after events are all loaded.
						int num_events = b.ReadInt32();
						for(int i=0;i<num_events;++i){
							Event e = new Event();
							if(b.ReadBoolean()){ //if true, this is an item that doesn't exist elsewhere, so grab all its info.
								int item_id = b.ReadInt32();
								if(item_id != 0){
									Item item = new Item();
									id.Add(item_id,item);
									item.row = b.ReadInt32();
									item.col = b.ReadInt32();
									item.name = b.ReadString();
									item.the_name = b.ReadString();
									item.a_name = b.ReadString();
									item.symbol = b.ReadChar();
									item.color = (Color)b.ReadInt32();
									item.light_radius = b.ReadInt32();
									item.type = (ConsumableType)b.ReadInt32();
									item.quantity = b.ReadInt32();
									item.charges = b.ReadInt32();
									item.other_data = b.ReadInt32();
									item.ignored = b.ReadBoolean();
									item.do_not_stack = b.ReadBoolean();
									item.revealed_by_light = b.ReadBoolean();
									e.target = item;
								}
							}
							else{
								int target_ID = b.ReadInt32();
								if(id.ContainsKey(target_ID)){
									e.target = id[target_ID];
								}
								else{
									throw new Exception("Error: some tiles/actors weren't loaded(4). ");
								}
							}
							int area_count = b.ReadInt32();
							for(int j=0;j<area_count;++j){
								if(e.area == null){
									e.area = new List<Tile>();
								}
								int tile_ID = b.ReadInt32();
								if(id.ContainsKey(tile_ID)){
									e.area.Add((Tile)id[tile_ID]);
								}
								else{
									throw new Exception("Error: some tiles weren't loaded(5). ");
								}
							}
							e.delay = b.ReadInt32();
							e.type = (EventType)b.ReadInt32();
							e.attr = (AttrType)b.ReadInt32();
							e.feature = (FeatureType)b.ReadInt32();
							e.value = b.ReadInt32();
							e.secondary_value = b.ReadInt32();
							e.msg = b.ReadString();
							int objs_count = b.ReadInt32();
							for(int j=0;j<objs_count;++j){
								if(e.msg_objs == null){
									e.msg_objs = new List<PhysicalObject>();
								}
								int obj_ID = b.ReadInt32();
								if(id.ContainsKey(obj_ID)){
									e.msg_objs.Add(id[obj_ID]);
								}
								else{
									throw new Exception("Error: some actors/tiles weren't loaded(6). ");
								}
							}
							e.time_created = b.ReadInt32();
							e.dead = b.ReadBoolean();
							e.tiebreaker = b.ReadInt32();
							game.Q.Add(e);
							if(e.type == EventType.FIRE && !e.dead){
								Fire.fire_event = e;
							}
						}
						game.Q.turn = game_turn;
						foreach(Event e in game.Q.list){
							if(e.type == EventType.MOVE && e.target == game.player){
								game.Q.current_event = e;
								break;
							}
						}
						int num_footsteps = b.ReadInt32();
						for(int i=0;i<num_footsteps;++i){
							int step_row = b.ReadInt32();
							int step_col = b.ReadInt32();
							Actor.footsteps.Add(new pos(step_row,step_col));
						}
						int num_prev_footsteps = b.ReadInt32();
						for(int i=0;i<num_prev_footsteps;++i){
							int step_row = b.ReadInt32();
							int step_col = b.ReadInt32();
							Actor.previous_footsteps.Add(new pos(step_row,step_col));
						}
						Actor.interrupted_path.row = b.ReadInt32();
						Actor.interrupted_path.col = b.ReadInt32();
						UI.viewing_commands_idx = b.ReadInt32();
						game.M.feat_gained_this_level = b.ReadBoolean();
						game.M.extra_danger = b.ReadInt32();
						int num_unIDed = b.ReadInt32();
						for(int i=0;i<num_unIDed;++i){
							ConsumableType ct = (ConsumableType)b.ReadInt32();
							string s = b.ReadString();
							Item.unIDed_name[ct] = s;
						}
						int num_IDed = b.ReadInt32();
						for(int i=0;i<num_IDed;++i){
							ConsumableType ct = (ConsumableType)b.ReadInt32();
							bool IDed = b.ReadBoolean();
							Item.identified[ct] = IDed;
						}
						int num_item_colors = b.ReadInt32();
						for(int i=0;i<num_item_colors;++i){
							ConsumableType ct = (ConsumableType)b.ReadInt32();
							Item.proto[ct].color = (Color)b.ReadInt32();
						}
						int num_burning = b.ReadInt32();
						for(int i=0;i<num_burning;++i){
							int obj_ID = b.ReadInt32();
							if(id.ContainsKey(obj_ID)){
								Fire.burning_objects.Add(id[obj_ID]);
							}
							else{
								throw new Exception("Error: some actors/tiles weren't loaded(7). ");
							}
						}
						game.M.aesthetics = new PosArray<AestheticFeature>(Global.ROWS,Global.COLS);
						for(int i=0;i<Global.ROWS;++i){
							for(int j=0;j<Global.COLS;++j){
								game.M.aesthetics[i,j] = (AestheticFeature)b.ReadInt32();
							}
						}
						game.M.dungeonDescription = b.ReadString();
						if(b.ReadBoolean()){
							int numShrines = b.ReadInt32();
							game.M.nextLevelShrines = new List<SchismDungeonGenerator.CellType>();
							for(int i=0;i<numShrines;++i){
								game.M.nextLevelShrines.Add((SchismDungeonGenerator.CellType)b.ReadInt32());
							}
						}
						game.M.shrinesFound = new int[5];
						for(int i=0;i<5;++i){
							game.M.shrinesFound[i] = b.ReadInt32();
						}
						Tile.spellbooks_generated = b.ReadInt32();
						UI.viewing_commands_idx = b.ReadInt32();
						string[] messages = new string[Buffer.log_length];
						int num_messages = b.ReadInt32();
						for(int i=0;i<num_messages;++i){
							messages[i] = b.ReadString();
						}
						for(int i=num_messages;i<Buffer.log_length;++i){
							messages[i] = "";
						}
						int message_pos = b.ReadInt32();
						game.B.LoadMessagesAndPosition(messages,message_pos,num_messages);
						b.Close();
						file.Close();
						File.Delete("forays.sav");
						Tile.Feature(FeatureType.TELEPORTAL).color = Item.Prototype(ConsumableType.TELEPORTAL).color;
						game.M.CalculatePoppyDistanceMap();
						game.M.UpdateDangerValues();
					}
					Screen.NoClose = true;
					MouseUI.PushButtonMap(MouseMode.Map);
					MouseUI.CreateStatsButtons();
					try{
						while(!Global.GAME_OVER){ game.Q.Pop(); }
					}
					catch(Exception e){
						StreamWriter fileout = new StreamWriter("error.txt",false);
						fileout.WriteLine(e.Message);
						fileout.WriteLine(e.StackTrace);
						fileout.Close();
						MouseUI.IgnoreMouseMovement = true;
						MouseUI.IgnoreMouseClicks = true;
						Screen.CursorVisible = false;
						Screen.Blank();
						Screen.WriteString(12,0,"  An error has occured. See error.txt for more details. Press any key to quit.".PadOuter(Global.SCREEN_W));
						Input.ReadKey();
						Global.Quit();
					}
					MouseUI.PopButtonMap();
					MouseUI.IgnoreMouseMovement = false;
					Screen.NoClose = false;
					Screen.CursorVisible = false;
					Global.SaveOptions();
					recentdepth = game.M.Depth;
					recentname = Actor.player_name;
					recentwin = Global.BOSS_KILLED? 'W' : '-';
					recentcause = Global.KILLED_BY;
					on_highscore_list = false;
					if(!Global.SAVING){
						List<string> newhighscores = new List<string>();
						int num_scores = 0;
						bool added = false;
						if(File.Exists("highscore.txt")){
							StreamReader file = new StreamReader("highscore.txt");
							string s = "";
							while(s.Length < 2 || s.Substring(0,2) != "--"){
								s = file.ReadLine();
								newhighscores.Add(s);
							}
							s = "!!";
							while(s.Substring(0,2) != "--"){
								s = file.ReadLine();
								if(s.Substring(0,2) == "--"){
									if(!added && num_scores < Global.HIGH_SCORES){
										char symbol = Global.BOSS_KILLED? 'W' : '-';
										newhighscores.Add($"{game.M.Depth} {symbol} {Actor.player_name} -- {Global.KILLED_BY}");
										//newhighscores.Add(game.M.current_level.ToString() + " " + symbol + " " + Actor.player_name + " -- " + Global.KILLED_BY);
										on_highscore_list = true;
									}
									newhighscores.Add(s);
									break;
								}
								if(num_scores < Global.HIGH_SCORES){
									string[] tokens = s.Split(' ');
									int dlev = Convert.ToInt32(tokens[0]);
									if(dlev < game.M.Depth || (dlev == game.M.Depth && Global.BOSS_KILLED)){
										if(!added){
											char symbol = Global.BOSS_KILLED? 'W' : '-';
											newhighscores.Add($"{game.M.Depth} {symbol} {Actor.player_name} -- {Global.KILLED_BY}");
											//newhighscores.Add(game.M.current_level.ToString() + " " + symbol + " " + Actor.player_name + " -- " + Global.KILLED_BY);
											++num_scores;
											added = true;
											on_highscore_list = true;
										}
										if(num_scores < Global.HIGH_SCORES){
											newhighscores.Add(s);
											++num_scores;
										}
									}
									else{
										newhighscores.Add(s);
										++num_scores;
									}
								}
							}
							file.Close();
						}
						else{
							newhighscores.Add("High scores:");
							newhighscores.Add("--");
							char symbol = Global.BOSS_KILLED? 'W' : '-';
							newhighscores.Add($"{game.M.Depth} {symbol} {Actor.player_name} -- {Global.KILLED_BY}");
							//newhighscores.Add(game.M.current_level.ToString() + " " + symbol + " " + Actor.player_name + " -- " + Global.KILLED_BY);
							newhighscores.Add("--");
							on_highscore_list = true;
						}
						StreamWriter fileout = new StreamWriter("highscore.txt",false);
						foreach(string str in newhighscores){
							fileout.WriteLine(str);
						}
						fileout.Close();
					}
					if(!Global.QUITTING && !Global.SAVING){
						GameOverScreen(game);
					}
					break;
				}
				case 'b':
				{
					Help.DisplayHelp();
					break;
				}
				case 'c':
				{
					MouseUI.PushButtonMap();
					Screen.Blank();
					List<string> scores = new List<string>();
					{
						if(!File.Exists("highscore.txt")){
							List<string> newhighscores = new List<string>();
							newhighscores.Add("High scores:");
							newhighscores.Add("--");
							newhighscores.Add("--");
							StreamWriter fileout = new StreamWriter("highscore.txt",false);
							foreach(string str in newhighscores){
								fileout.WriteLine(str);
							}
							fileout.Close();
						}
						StreamReader file = new StreamReader("highscore.txt");
						string s = "";
						while(s.Length < 2 || s.Substring(0,2) != "--"){
							s = file.ReadLine();
						}
						s = "!!";
						while(s.Substring(0,2) != "--"){
							s = file.ReadLine();
							if(s.Substring(0,2) == "--" || scores.Count == Global.HIGH_SCORES){
								break;
							}
							else{
								scores.Add(s);
							}
						}
						file.Close();
					}
					if(scores.Count == Global.HIGH_SCORES && !on_highscore_list && recentdepth != -1){
						scores.RemoveLast();
						scores.Add(recentdepth.ToString() + " " + recentwin + " " + recentname + " -- " + recentcause);
					}
					int longest_name = 0;
					int longest_cause = 0;
					foreach(string s in scores){
						string[] tokens = s.Split(' ');
						string name_and_cause_of_death = s.Substring(tokens[0].Length + 3);
						int idx = name_and_cause_of_death.LastIndexOf(" -- ");
						string name = name_and_cause_of_death.Substring(0,idx);
						string cause_of_death = name_and_cause_of_death.Substring(idx+4);
						if(name.Length > longest_name){
							longest_name = name.Length;
						}
						if(cause_of_death.Length > longest_cause){
							longest_cause = cause_of_death.Length;
						}
					}
					int total_spaces = Global.SCREEN_W - (longest_name + 4 + longest_cause); //max name length is 26 and max cause length is 42. Depth is the '4'.
					int half_spaces = total_spaces / 2;
					int half_spaces_offset = (total_spaces+1) / 2;
					int spaces1 = half_spaces / 4;
					int spaces2 = half_spaces - (half_spaces / 4);
					int spaces3 = half_spaces_offset - (half_spaces_offset / 4);
					int name_middle = spaces1 + longest_name/2;
					int depth_middle = spaces1 + spaces2 + longest_name + 1;
					int cause_middle = spaces1 + spaces2 + spaces3 + longest_name + 4 + (longest_cause-1)/2;
					Color primary = Color.Green;
					Color recent = Color.Cyan;
					Screen.WriteString(0,(Global.SCREEN_W - 11) / 2,new cstr("HIGH SCORES",Color.Yellow)); //"HIGH SCORES" has width 11
					Screen.WriteString(1,(Global.SCREEN_W - 11) / 2,new cstr("-----------",Color.Cyan));
					Screen.WriteString(2,name_middle-4,new cstr("Character",primary));
					Screen.WriteString(2,depth_middle-2,new cstr("Depth",primary));
					Screen.WriteString(2,cause_middle-6,new cstr("Cause of death",primary));
					bool written_recent = false;
					int line = 3;
					foreach(string s in scores){
						if(line >= Global.SCREEN_H){
							break;
						}
						string[] tokens = s.Split(' ');
						int dlev = Convert.ToInt32(tokens[0]);
						char winning = tokens[1][0];
						string name_and_cause_of_death = s.Substring(tokens[0].Length + 3);
						int idx = name_and_cause_of_death.LastIndexOf(" -- ");
						string name = name_and_cause_of_death.Substring(0,idx);
						string cause_of_death = name_and_cause_of_death.Substring(idx+4);
						string cause_capitalized = cause_of_death.Substring(0,1).ToUpper() + cause_of_death.Substring(1);
						Color current_color = Color.White;
						if(!written_recent && name == recentname && dlev == recentdepth && winning == recentwin && cause_of_death == recentcause){
							current_color = recent;
							written_recent = true;
						}
						else{
							current_color = Color.White;
						}
						Screen.WriteString(line,spaces1,new cstr(name,current_color));
						Screen.WriteString(line,spaces1 + spaces2 + longest_name,new cstr(dlev.ToString().PadLeft(2),current_color));
						Screen.WriteString(line,spaces1 + spaces2 + spaces3 + longest_name + 4,new cstr(cause_capitalized,current_color));
						if(winning == 'W'){
							Screen.WriteString(line,spaces1 + spaces2 + longest_name + 3,new cstr("W",Color.Yellow));
						}
						++line;
					}
					Input.ReadKey();
					MouseUI.PopButtonMap();
					break;
				}
				case 'd':
					Global.Quit();
					break;
				default:
					break;
				}
				if(Global.QUITTING){
					Global.Quit();
				}
			}
		}
		static void GameOverScreen(Game game){
			MouseUI.PushButtonMap();
			game.player.attrs[AttrType.BLIND] = 0; //make sure the player can actually view the map
			game.player.attrs[AttrType.BURNING] = 0;
			game.player.attrs[AttrType.FROZEN] = 0; //...without borders
			//game.M.Draw();
			colorchar[,] mem = null;
			UI.DisplayStats();
			bool showed_IDed_tip = false;
			if(Global.KILLED_BY != "gave up" && !Help.displayed[TutorialTopic.IdentifiedConsumables]){
				if(game.player.inv.Where(item=>Item.identified[item.type] && item.Is(ConsumableType.HEALING,ConsumableType.TIME)).Count > 0){
					Help.TutorialTip(TutorialTopic.IdentifiedConsumables);
					Global.SaveOptions();
					showed_IDed_tip = true;
				}
			}
			if(!showed_IDed_tip && Global.KILLED_BY != "gave up" && !Help.displayed[TutorialTopic.UnidentifiedConsumables]){
				int known_count = 0;
				foreach(ConsumableType ct in Item.identified.d.Keys){
					if(Item.identified[ct] && Item.NameOfItemType(ct) != "other"){
						++known_count;
					}
				}
				if(known_count < 2 && game.player.inv.Where(item=>!Item.identified[item.type]).Count > 2){
					Help.TutorialTip(TutorialTopic.UnidentifiedConsumables);
					Global.SaveOptions();
				}
			}
			Dict<ConsumableType,bool> known_items = new Dict<ConsumableType,bool>(Item.identified);
			foreach(Item i in game.player.inv){
				if(i.NameOfItemType() != "other"){
					if(!Item.identified[i.type]){
						if(!Item.unIDed_name[i.type].Contains("{tried}")){
							Item.unIDed_name[i.type] = Item.unIDed_name[i.type] + " {untried}";
						}
						Item.identified[i.type] = true;
					}
					else{
						known_items[i.type] = true;
					}
					if(i.NameOfItemType() == "wand"){
						i.other_data = -1;
					}
					if(Item.unIDed_name[i.type].Contains("{tried}")){
						i.SetName(i.name + " {tried}");
					}
					else{
						if(Item.unIDed_name[i.type].Contains("{untried}")){
							i.SetName(i.name + " {untried}");
						}
					}
				}
			}
			List<string> ls = new List<string>();
			ls.Add("See the map");
			ls.Add("See last messages");
			ls.Add("Examine your equipment");
			ls.Add("Examine your inventory");
			ls.Add("View known item types");
			ls.Add("See character info");
			ls.Add("Write this information to a file");
			ls.Add("Done");
			for(bool done=false;!done;){
				if(mem != null){
					Screen.MapDrawWithStrings(mem,0,0,Global.ROWS,Global.COLS);
				}
				game.player.Select("Would you like to examine your character! ","".PadRight(Global.COLS),"".PadRight(Global.COLS),ls,true,false,false);
				int sel = game.player.GetSelection("Would you like to examine your character? ",ls.Count,true,false,false);
				mem = Screen.GetCurrentMap();
				switch(sel){
				case 0:
					MouseUI.PushButtonMap();
					Dictionary<Actor,colorchar> old_ch = new Dictionary<Actor,colorchar>();
					List<Actor> drawn = new List<Actor>();
					foreach(Actor a in game.M.AllActors()){
						if(game.player.CanSee(a)){
							old_ch.Add(a,game.M.last_seen[a.row,a.col]);
							game.M.last_seen[a.row,a.col] = new colorchar(a.symbol,a.color);
							drawn.Add(a);
						}
					}
					Screen.MapDrawWithStrings(game.M.last_seen,0,0,Global.ROWS,Global.COLS);
					game.player.GetTarget(true,-1,-1,true,false,false,"");
					//game.B.DisplayNow("Press any key to continue. ");
					//Screen.CursorVisible = true;
					//Input.ReadKey();
					MouseUI.PopButtonMap();
					foreach(Actor a in drawn){
						game.M.last_seen[a.row,a.col] = old_ch[a];
					}
					game.M.Redraw();
					/*foreach(Tile t in game.M.AllTiles()){
						if(t.type != TileType.FLOOR && !t.IsTrap()){
							bool good = false;
							foreach(Tile neighbor in t.TilesAtDistance(1)){
								if(neighbor.type != TileType.WALL){
									good = true;
								}
							}
							if(good){
								t.seen = true;
							}
						}
					}
					game.B.DisplayNow("Press any key to continue. ");
					Screen.CursorVisible = true;
					Screen.WriteMapChar(0,0,'-');
					game.M.Draw();
					Input.ReadKey();*/
					break;
				case 1:
				{
					SharedEffect.ShowPreviousMessages(false);
					break;
				}
				case 2:
					UI.DisplayEquipment();
					break;
				case 3:
					MouseUI.PushButtonMap();
					MouseUI.AutomaticButtonsFromStrings = true;
					for(int i=1;i<9;++i){
						Screen.WriteMapString(i,0,"".PadRight(Global.COLS));
					}
					MouseUI.AutomaticButtonsFromStrings = false;
					game.player.Select("In your pack: ",game.player.InventoryList(),true,false,false);
					Input.ReadKey();
					MouseUI.PopButtonMap();
					break;
				case 4:
				{
					SharedEffect.ShowKnownItems(known_items);
					break;
				}
				case 5:
					UI.DisplayCharacterInfo();
					break;
				case 6:
				{
					game.B.DisplayNow("Enter file name: ");
					Screen.CursorVisible = true;
					MouseUI.PushButtonMap();
					string filename = Input.EnterString(40);
					MouseUI.PopButtonMap();
					if(filename == ""){
						break;
					}
					if(!filename.Contains(".")){
						filename = filename + ".txt";
					}
					StreamWriter file = new StreamWriter(filename,true);
					UI.DisplayCharacterInfo(false);
					colorchar[,] screen = Screen.GetCurrentScreen();
					for(int i=2;i<Global.SCREEN_H;++i){
						for(int j=0;j<Global.SCREEN_W;++j){
							file.Write(screen[i,j].c);
						}
						file.WriteLine();
					}
					file.WriteLine();
					file.WriteLine("Inventory: ");
					foreach(string s in game.player.InventoryList()){
						file.WriteLine(s);
					}
					if(game.player.InventoryList().Count == 0){
						file.WriteLine("(nothing)");
					}
					file.WriteLine();
					file.WriteLine("Known items: ");
					bool known_items_found = false;
					foreach(ConsumableType ct in known_items.d.Keys){
						if(known_items[ct] && (Item.NameOfItemType(ct) == "potion" || Item.NameOfItemType(ct) == "scroll" || Item.NameOfItemType(ct) == "orb")){
							file.WriteLine(Item.Prototype(ct).Name(false));
							known_items_found = true;
						}
					}
					if(!known_items_found){
						file.WriteLine("(none)");
					}
					else{
						file.WriteLine();
					}
					file.WriteLine();
					foreach(Tile t in game.M.AllTiles()){
						if(t.type != TileType.FLOOR && !t.IsTrap()){
							bool good = false;
							foreach(Tile neighbor in t.TilesAtDistance(1)){
								if(neighbor.type != TileType.WALL){
									good = true;
								}
							}
							if(good){
								t.seen = true;
							}
						}
					}
					Screen.WriteMapChar(0,0,'-'); //todo: this was a hack. can now be replaced with the proper Redraw method, I think.
					game.M.Draw();
					int col = 0;
					foreach(colorchar cch in Screen.GetCurrentMap()){
						file.Write(cch.c);
						++col;
						if(col == Global.COLS){
							file.WriteLine();
							col = 0;
						}
					}
					file.WriteLine();
					file.WriteLine("Last messages: ");
					foreach(string s in game.B.GetMessages()){
						if(s != ""){
							file.WriteLine(s);
						}
					}
					/*Screen.WriteMapString(0,0,"".PadRight(Global.COLS,'-'));
					int line = 1;
					foreach(string s in game.B.GetMessages()){
						if(line < 21){
							Screen.WriteMapString(line,0,s.PadRight(Global.COLS));
						}
						++line;
					}
					Screen.WriteMapString(21,0,"".PadRight(Global.COLS,'-'));
					file.WriteLine("Last messages: ");
					col = 0;
					foreach(colorchar cch in Screen.GetCurrentMap()){
						file.Write(cch.c);
						++col;
						if(col == Global.COLS){
							file.WriteLine();
							col = 0;
						}
					}*/
					file.WriteLine();
					file.Close();
					break;
				}
				case 7:
					done = true;
					break;
				default:
					break;
				}
			}
			MouseUI.PopButtonMap();
		}
	}
}
