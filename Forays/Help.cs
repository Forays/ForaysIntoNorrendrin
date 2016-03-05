/*Copyright (c) 2011-2016  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Utilities;
namespace Forays{
	public enum HelpTopic{Overview,Skills,Feats,Spells,Items,Commands,Advanced,Tips};
	public enum TutorialTopic{Movement,Attacking,Torch,Fire,Exhaustion,Recovery,SwitchingEquipment,RangedAttacks,Shrines,DistributionOfShrines,
		Feats,ActiveFeats,Combat,Defense,Magic,Spirit,Stealth,FindingConsumables,IdentifiedConsumables,UnidentifiedConsumables,MagicTrinkets,
		SpellTiers,SpellFailure,CastingWithoutMana,ExhaustionAndArmor,ShinyPlateArmor,HeavyPlateArmor,MakingNoise,CriticalHits,InstantKills,Dodging,
		FightingTheUnseen,NotRevealedByLight,Traps,CrackedWall,FirePit,Poppies,StoneSlab,BlastFungus,PoolOfRestoration,Demonstone,IncreasedSpeed,
		Stunned,Frozen,Slimed,Oiled,Vulnerable,Silenced,Confusion,Bleeding,Immobilized,Acidified,Afraid,Grabbed,Dulled,Possessed,Heavy,
		Merciful,Negated,Stuck,Infested,WeakPoint,WornOut,Damaged,Stoneform,Vampirism,Roots,BrutishStrength,MysticMind};
	public static class Help{
		public static Dict<TutorialTopic,bool> displayed = new Dict<TutorialTopic,bool>();
		public static void DisplayHelp(){ DisplayHelp(HelpTopic.Overview); }
		public static void DisplayHelp(HelpTopic h){
			MouseUI.PushButtonMap(MouseMode.ScrollableMenu);
			Screen.Blank();
			int num_topics = Enum.GetValues(typeof(HelpTopic)).Length;
			int topic_start_row = (Global.SCREEN_H - num_topics - 1) / 2;
			const int text_col = 18;
			Screen.WriteString(topic_start_row-2,5,"Topics:",Color.Yellow);
			for(int i=0;i<num_topics+1;++i){
				Screen.WriteString(i+topic_start_row,1,"[ ]");
				Screen.WriteChar(i+topic_start_row,2,(char)(i+'a'),Color.Cyan);
				MouseUI.CreateButton((ConsoleKey)(ConsoleKey.A + i),false,i+topic_start_row,0,1,text_col - 1);
			}
			Screen.WriteString(num_topics+topic_start_row,5,"Quit");
			const int text_width = 64;
			const int text_height = 26;
			MouseUI.CreateButton(ConsoleKey.OemMinus,false,0,text_col,1,text_width);
			MouseUI.CreateButton(ConsoleKey.OemPlus,false,Global.SCREEN_H-1,text_col,1,text_width);
			Screen.WriteString(0,text_col,"".PadRight(text_width - 3,'-'));
			Screen.WriteString(Global.SCREEN_H-1,text_col,"".PadRight(text_width - 3,'-'));
			List<string> text = HelpText(h);
			int startline = 0;
			ConsoleKeyInfo command;
			char ch;
			for(bool done=false;!done;){
				foreach(HelpTopic help in Enum.GetValues(typeof(HelpTopic))){
					if(h == help){
						Screen.WriteString((int)help + topic_start_row,5,Enum.GetName(typeof(HelpTopic),help),Color.Yellow);
					}
					else{
						Screen.WriteString((int)help + topic_start_row,5,Enum.GetName(typeof(HelpTopic),help));
					}
				}
				if(startline > 0){
					Screen.WriteString(0,text_col + text_width - 3,new colorstring("[",Color.Yellow,"-",Color.Cyan,"]",Color.Yellow));
				}
				else{
					Screen.WriteString(0,text_col + text_width - 3,"---");
				}
				bool more = false;
				if(startline + text_height < text.Count){
					more = true;
				}
				if(more){
					Screen.WriteString(Global.SCREEN_H-1,text_col + text_width - 3,new colorstring("[",Color.Yellow,"+",Color.Cyan,"]",Color.Yellow));
				}
				else{
					Screen.WriteString(Global.SCREEN_H-1,text_col + text_width - 3,"---");
				}
				for(int i=1;i<Global.SCREEN_H-1;++i){
					if(text.Count - startline < i){
						Screen.WriteString(i,text_col,"".PadRight(text_width));
					}
					else{
						Screen.WriteString(i,text_col,text[i+startline-1].PadRight(text_width));
					}
				}
				command = Input.ReadKey(false);
				ConsoleKey ck = command.Key;
				switch(ck){
				case ConsoleKey.Backspace:
				case ConsoleKey.PageUp:
				case ConsoleKey.NumPad9:
					ch = (char)8;
					break;
				case ConsoleKey.PageDown:
				case ConsoleKey.NumPad3:
					ch = ' ';
					break;
				case ConsoleKey.Home:
				case ConsoleKey.NumPad7:
					ch = '[';
					break;
				case ConsoleKey.End:
				case ConsoleKey.NumPad1:
					ch = ']';
					break;
				default:
					ch = command.GetCommandChar();
					break;
				}
				switch(ch){
				case 'a':
					if(h != HelpTopic.Overview){
						h = HelpTopic.Overview;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'b':
					if(h != HelpTopic.Skills){
						h = HelpTopic.Skills;
						text = HelpText(h);
						startline = 0;
						
					}
					break;
				case 'c':
					if(h != HelpTopic.Feats){
						h = HelpTopic.Feats;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'd':
					if(h != HelpTopic.Spells){
						h = HelpTopic.Spells;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'e':
					if(h != HelpTopic.Items){
						h = HelpTopic.Items;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'f':
					if(h != HelpTopic.Commands){
						h = HelpTopic.Commands;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'g':
					if(h != HelpTopic.Advanced){
						h = HelpTopic.Advanced;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'h':
					if(h != HelpTopic.Tips){
						h = HelpTopic.Tips;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'i':
				case (char)27:
					done = true;
					break;
				case '8':
				case '-':
				case '_':
					if(startline > 0){
						--startline;
					}
					break;
				case '2':
				case '+':
				case '=':
					if(more){
						++startline;
					}
					break;
				case (char)8:
					if(startline > 0){
						startline -= text_height;
						if(startline < 0){
							startline = 0;
						}
					}
					break;
				case ' ':
				case (char)13:
					if(text.Count > text_height){
						startline += text_height;
						if(startline + text_height > text.Count){
							startline = text.Count - text_height;
						}
					}
					break;
				case '[':
					startline = 0;
					break;
				case ']':
					startline = Math.Max(0,text.Count - text_height);
					break;
				default:
					break;
				}
			}
			Screen.Blank();
			MouseUI.PopButtonMap();
		}
		public static List<string> HelpText(HelpTopic h){
			string path = "";
			int startline = 0;
			int num_lines = -1; //-1 means read until end
			switch(h){
			case HelpTopic.Overview:
				path = "help.txt";
				num_lines = 50;
				break;
			case HelpTopic.Commands:
				path = "help.txt";
				startline = 50;
				break;
			case HelpTopic.Items:
				path = "item_help.txt";
				break;
			case HelpTopic.Skills:
				path = "feat_help.txt";
				num_lines = 26;
				break;
			case HelpTopic.Feats:
				path = "feat_help.txt";
				startline = 27;
				break;
			case HelpTopic.Spells:
				path = "spell_help.txt";
				break;
			case HelpTopic.Advanced:
				path = "advanced_help.txt";
				break;
			default:
				path = "feat_help.txt";
				break;
			}
			List<string> result = new List<string>();
			if(h == HelpTopic.Tips){ //these aren't read from a file
				result.Add("Viewing all tutorial tips:");
				result.Add("");
				result.Add("");
				result.Add("");
				foreach(TutorialTopic topic in Enum.GetValues(typeof(TutorialTopic))){
					foreach(string s in TutorialText(topic)){
						result.Add(s);
					}
					result.Add("");
					result.Add("");
					result.Add("");
				}
				return result;
			}
			if(path != ""){
				StreamReader file = new StreamReader(Path.Combine("ForaysHelp",path));
				for(int i=0;i<startline;++i){
					file.ReadLine();
				}
				for(int i=0;i<num_lines || num_lines == -1;++i){
					if(file.Peek() != -1){
						result.Add(file.ReadLine());
					}
					else{
						break;
					}
				}
				file.Close();
			}
			return result;
		}
		private static List<colorstring> BoxAnimationFrame(int height,int width){
			Color box_edge_color = Color.Blue;
			Color box_corner_color = Color.Yellow;
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(width-2,'-'),box_edge_color,"+",box_corner_color));
			for(int i=0;i<height-2;++i){
				box.Add(new colorstring("|",box_edge_color,"".PadRight(width-2),Color.Gray,"|",box_edge_color));
			}
			box.Add(new colorstring("+",box_corner_color,"".PadRight(width-2,'-'),box_edge_color,"+",box_corner_color));
			return box;
		}
		private static int FrameWidth(int previous_height,int previous_width){
			return previous_width - (previous_width * 2 / previous_height); //2 lines are removed, so the width loses 2/height to keep similar dimensions
		}
		public static void TutorialTip(TutorialTopic topic){ TutorialTip(topic,false); }
		public static void TutorialTip(TutorialTopic topic,bool no_displaynow_call){
			if(Global.Option(OptionType.NEVER_DISPLAY_TIPS) || displayed[topic]){
				return;
			}
			MouseUI.PushButtonMap();
			const Color box_edge_color = Color.Blue;
			const Color box_corner_color = Color.Yellow;
			const Color first_line_color = Color.Yellow;
			const Color text_color = Color.Gray;
			string[] text = TutorialText(topic);
			int stringwidth = 27; // length of "[Press any key to continue]"
			foreach(string s in text){
				if(s.Length > stringwidth){
					stringwidth = s.Length;
				}
			}
			stringwidth += 4; //2 blanks on each side
			int boxwidth = stringwidth + 2;
			int boxheight = text.Length + 5;
			colorstring[] box = new colorstring[boxheight]; //maybe i should make this a list to match the others
			box[0] = new colorstring("+",box_corner_color,"".PadRight(stringwidth,'-'),box_edge_color,"+",box_corner_color);
			box[text.Length + 1] = new colorstring("|",box_edge_color,"".PadRight(stringwidth),Color.Gray,"|",box_edge_color);
			box[text.Length + 2] = new colorstring("|",box_edge_color) + "[Press any key to continue]".PadOuter(stringwidth).GetColorString(text_color) + new colorstring("|",box_edge_color);
			box[text.Length + 3] = new colorstring("|",box_edge_color) + "[=] Stop showing tips".PadOuter(stringwidth).GetColorString(text_color) + new colorstring("|",box_edge_color);
			box[text.Length + 4] = new colorstring("+",box_corner_color,"".PadRight(stringwidth,'-'),box_edge_color,"+",box_corner_color);
			int pos = 1;
			foreach(string s in text){
				box[pos] = new colorstring("|",box_edge_color) + s.PadOuter(stringwidth).GetColorString(text_color) + new colorstring("|",box_edge_color);
				if(pos == 1){
					box[pos] = new colorstring();
					box[pos].strings.Add(new cstr("|",box_edge_color));
					box[pos].strings.Add(new cstr(s.PadOuter(stringwidth),first_line_color));
					box[pos].strings.Add(new cstr("|",box_edge_color));
				}
				++pos;
			}
			int y = (Global.SCREEN_H - boxheight) / 2;
			int x = (Global.SCREEN_W - boxwidth) / 2;
			int spaces_on_left = stringwidth - 27;
			MouseUI.CreateButton(ConsoleKey.A,false,y + boxheight - 3,x + 1 + spaces_on_left/2,1,27);
			spaces_on_left = stringwidth - 21;
			MouseUI.CreateButton(ConsoleKey.OemPlus,false,y + boxheight - 2,x + 1 + spaces_on_left/2,1,21);
			colorchar[,] memory = Screen.GetCurrentRect(y,x,boxheight,boxwidth);
			List<List<colorstring>> frames = new List<List<colorstring>>();
			frames.Add(BoxAnimationFrame(boxheight-2,FrameWidth(boxheight,boxwidth)));
			for(int i=boxheight-4;i>0;i-=2){
				frames.Add(BoxAnimationFrame(i,FrameWidth(frames.Last().Count,frames.Last()[0].Length())));
			}
			UI.DisplayStats();
			if(!no_displaynow_call){
				Actor.B.DisplayContents();
			}
			for(int i=frames.Count-1;i>=0;--i){ //since the frames are in reverse order
				int y_offset = i + 1;
				int x_offset = (boxwidth - frames[i][0].Length()) / 2;
				Screen.WriteList(y+y_offset,x+x_offset,frames[i]);
				Screen.GLUpdate();
				Thread.Sleep(20);
			}
			foreach(colorstring s in box){
				Screen.WriteString(y,x,s);
				++y;
			}
			bool mouse_movement = MouseUI.IgnoreMouseMovement;
			MouseUI.IgnoreMouseMovement = false;
			Screen.GLUpdate();
			Thread.Sleep(500);
			Input.FlushInput();
			if(!Actor.player.HasAttr(AttrType.RESTING)){
				Actor.player.Interrupt();
			}
			if(Input.ReadKey(false).KeyChar == '='){
				Global.Options[OptionType.NEVER_DISPLAY_TIPS] = true;
			}
			MouseUI.IgnoreMouseMovement = mouse_movement;
			Screen.WriteArray((Global.SCREEN_H - boxheight) / 2,x,memory);
			MouseUI.PopButtonMap();
			UI.DisplayStats();
			displayed[topic] = true;
		}
		public static string[] TutorialText(TutorialTopic topic){
			switch(topic){
			case TutorialTopic.Acidified:
			return new string[]{
				"Acid",
				"",
				"Acid can damage your metal equipment. (Your",
				"sword, mace, dagger, chainmail, and plate can",
				"be damaged by acid.)",
				"",
				"Acidic attacks will wear out your metal armor",
				"first, and then damage it. (Damaged armor gives",
				"no protection.)",
				"",
				"Some monsters are acidic enough that attacking",
				"them will dull your metal weapons, too. (A dulled",
				"weapon deals minimum damage.)"};
			case TutorialTopic.ActiveFeats:
			return new string[]{
				"Active feats",
				"",
				"You've learned an active feat that you can",
				"activate at any time.",
				"",
				"To use an active feat, open the character",
				"screen with [c], then press the key shown",
				"next to that feat."};
			case TutorialTopic.Afraid:
			return new string[]{
				"Terrified",
				"",
				"While frightened, you're unable to attack the",
				"source of your terror, and unable to move into",
				"any space next to it (unless there's",
				"nowhere else to run)."};
			case TutorialTopic.Attacking:
			return new string[]{
				"Attacking enemies",
				"",
				"To make a melee attack, simply try to",
				"move directly into an adjacent monster."};
			case TutorialTopic.BlastFungus:
			return new string[]{
				"Blast fungus",
				"",
				"Any light will ignite the fuse of a blast fungus.",
				"After a few turns, it'll explode, dealing 6d6",
				"damage within a radius of 3 spaces.",
				"",
				"The fuse of a blast fungus also ignites",
				"as it gets pulled out of the ground."};
			case TutorialTopic.Bleeding:
			return new string[]{
				"Bleeding",
				"",
				"While bleeding, you'll lose 2 HP each turn",
				"until the bleeding stops.",
				"",
				"Applying a bandage will prevent bleeding damage,",
				"but be careful - any major damage will remove",
				"the bandage, and if you're still bleeding",
				"you'll continue taking damage."};
			case TutorialTopic.BrutishStrength:
			return new string[]{
				"Brutish strength",
				"",
				"Brutish strength makes your attacks",
				"overwhelmingly powerful, dealing maximum",
				"damage and sending enemies flying back",
				"5 spaces when hit. In addition, you'll",
				"move into the space your foe just",
				"vacated, if possible."};
			case TutorialTopic.CastingWithoutMana:
			return new string[]{
				"Casting without mana",
				"",
				"Spells can be cast even when you're out of mana.",
				"",
				"Doing this is exhausting - your exhaustion will",
				"increase by 5% for every missing point of mana."};
			case TutorialTopic.Combat:
			return new string[]{
				"Combat skill",
				"",
				"Each point of Combat skill increases the damage",
				"dealt by your weapons by 1, and increases the",
				"chance for your weapons to hit by 1%.",
				"",
				"(Base weapon damage is 2d6. Base hit chance",
				"is 75% for melee and 45% for bows.)"};
			case TutorialTopic.Confusion:
			return new string[]{
				"Confusion",
				"",
				"While confused, any attempt to move or",
				"attack will send you in a random direction.",
				"",
				"(If there's an enemy in that direction,",
				"you'll attack.)"};
			case TutorialTopic.CrackedWall:
			return new string[]{
				"Cracked walls",
				"",
				"These damaged walls will break if something is",
				"knocked through them, or when hit with an",
				"explosive force.",
				"",
				"(Small enemies and items won't",
				"be knocked through.)"};
			case TutorialTopic.CriticalHits:
			return new string[]{
				"Critical hits",
				"",
				"Some attacks are nastier than others. Each",
				"attack has a 1 in 8 chance of being a",
				"critical hit.",
				"",
				"A critical hit might deal more damage or",
				"inflict a status condition. Different",
				"monsters (and each of your weapons) have",
				"different critical effects, so be prepared!"};
			case TutorialTopic.Damaged:
			return new string[]{
				"Damaged",
				"",
				"When your armor is damaged, it provides no",
				"protection, and enemies will score critical",
				"hits twice as often. Any negatives (such as",
				"stealth penalties) still apply.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.Defense:
			return new string[]{
				"Defense skill",
				"",
				"Each point of Defense skill adds 3% to",
				"your chance to avoid melee or bow attacks.",
				"",
				"(Without any Defense skill, most enemies",
				"have a 75% chance to hit you.)"};
			case TutorialTopic.Demonstone:
			return new string[]{
				"Demonstone",
				"",
				"Any fire ignited on demonstone will burn",
				"forever, never dying out on its own."
			};
			case TutorialTopic.DistributionOfShrines:
			return new string[]{
				"Distribution of shrines",
				"",
				"Five shrines will appear somewhere on each",
				"pair of levels. (Depths 1 & 2 will have five",
				"shrines between them, as will depths 3 & 4,",
				"depths 5 & 6, and so on.)",
				"",
				"If you feel an ancient power calling you back,",
				"it means there's a shrine on this level",
				"you haven't activated yet!"};
			case TutorialTopic.Dodging:
			return new string[]{
				"Dodging",
				"",
				"Some enemies can dodge attacks made against",
				"them. (Potions of haste will temporarily",
				"give you this ability, too.)",
				"",
				"When attacked, a defender with this ability",
				"has a chance to leap into an adjacent space,",
				"causing the attack to miss. More open space",
				"means a better chance to dodge, so consider",
				"this when choosing your battleground!"};
			case TutorialTopic.Dulled:
			return new string[]{
				"Dulled",
				"",
				"A dull weapon deals minimum damage. (Since",
				"all your melee weapons deal 2d6 damage",
				"normally, they deal 2 damage while dull.)",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.Exhaustion:
			return new string[]{
				"Exhaustion",
				"",
				"Many actions (and the occasional hazard",
				"or enemy) can increase your exhaustion.",
				"",
				"If your exhaustion gets high enough,",
				"it'll interfere with your ability to",
				"wear armor properly, cast spells, and",
				"use weapons.",
				"",
				"You can remove all exhaustion by [r]esting."};
			case TutorialTopic.ExhaustionAndArmor:
			return new string[]{
				"Exhaustion and armor",
				"",
				"As your exhaustion grows, your armor will",
				"weigh heavily upon you. ",
				"",
				"At 25% exhaustion, you get no",
				"protection from plate armor.",
				"",
				"At 50%, chainmail is too heavy.",
				"",
				"At 75%, you can't wear your leather",
				"armor effectively.",
				"(The Armor mastery feat increases these",
				"thresholds by 25%.)"};
			case TutorialTopic.Feats:
			return new string[]{
				"Feats",
				"",
				"Feats are special abilities",
				"you can learn at shrines.",
				"",
				"For each set of five shrines,",
				"one of them will grant you a",
				"feat when you activate it.",
				"(Therefore, you'll learn one new",
				"feat per two dungeon levels.)",
				"",
				"(The feat starts working immediately",
				"upon choosing it.)"};
			case TutorialTopic.FightingTheUnseen:
			return new string[]{
				"Fighting the unseen",
				"",
				"An unseen attacker has a big advantage.",
				"Every attack becomes a sneak attack with",
				"perfect accuracy (though they can still",
				"be stopped by armor).",
				"",
				"Additionally, an attack against an unseen",
				"enemy has an extra 50% chance of",
				"missing them entirely!"};
			case TutorialTopic.FindingConsumables:
			return new string[]{
				"Consumable items",
				"",
				"This is an unidentified item. It's probably",
				"useful, but you won't know exactly what it",
				"does until you try it. (A scroll of knowledge",
				"will also identify your items.)",
				"",
				"You can press [\\] to see the list of item",
				"types. This can help you figure out what",
				"an unknown item could be."};
			case TutorialTopic.Fire:
			return new string[]{
				"Fire",
				"",
				"A common hazard, fire will spread to nearby",
				"flammable dungeon features and items.",
				"Burning objects will generate enough heat",
				"to sear those nearby, so even standing near",
				"a roaring fire is dangerous.",
				"",
				"If you catch fire, you'll take damage for several",
				"turns before you stop burning. Water and slime",
				"will extinguish a fire immediately, as will any",
				"thick, heavy gas (like poison and dust clouds)."};
			case TutorialTopic.FirePit:
			return new string[]{
				"Firepits",
				"",
				"Firepits are stone circles containing glowing",
				"embers. They're safe to walk over, but if",
				"anything flammable is dropped or knocked in, it",
				"will catch fire.",
				"",
				"(This includes scrolls, oil, and you.)"};
			case TutorialTopic.Frozen:
			return new string[]{
				"Frozen",
				"",
				"While you're encased in ice, you don't take damage",
				"(except poison) and you can't take any actions.",
				"",
				"If you attempt to move or take an action, you'll",
				"try to break free, which will tire you slightly.",
				"The ice weakens as it absorbs damage, and fire",
				"damage will melt the ice instantly."};
			case TutorialTopic.Grabbed:
			return new string[]{
				"Grabbed",
				"",
				"When an enemy grabs you, you can't step away",
				"from it. You can still move away by other",
				"means, and you can maneuver around that enemy",
				"as long as you stay adjacent to it.",
				"",
				"If you're covered in a slippery substance,",
				"you'll escape from grabs effortlessly."};
			case TutorialTopic.Heavy:
			return new string[]{
				"Heavy",
				"",
				"A heavy weapon has a 50% chance of increasing",
				"your exhaustion with each attack.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.HeavyPlateArmor:
			return new string[]{
				"Heavy plate armor",
				"",
				"Plate armor provides excellent protection, but",
				"its weight will add to your exhaustion each time",
				"it blocks an attack."};
			case TutorialTopic.IdentifiedConsumables:
			return new string[]{
				"Using consumable items",
				"",
				"Sometimes death is unavoidable.",
				"",
				"However, consumable items can",
				"get you out of some desperate",
				"situations.",
				"",
				"When all hope seems lost, be sure to",
				"check your inventory."};
			case TutorialTopic.Immobilized:
			return new string[]{
				"Immobilized",
				"",
				"Some effects can entirely prevent your movement.",
				"You won't be able to walk, teleport, be pulled",
				"or pushed, etc.",
				"",
				"Pressing a directional key to move while",
				"immobilized won't consume a turn - if you want",
				"to wait it out, press [.] to stay where you are."};
			case TutorialTopic.IncreasedSpeed:
			return new string[]{
				"Increased speed",
				"",
				"Effects that increase your speed (such as",
				"potions of haste and the Flying leap spell)",
				"only affect the speed of your movement.",
				"Other actions, like attacking and using items,",
				"will be unaffected."};
			case TutorialTopic.Infested:
			return new string[]{
				"Infested",
				"",
				"While your armor is infested, wearing it will",
				"subject you to dozens of bites, constantly",
				"damaging you.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.InstantKills:
			return new string[]{
				"Instant kills",
				"",
				"Against most living enemies, scoring a critical",
				"hit on a sneak attack (any attack where you",
				"strike from hiding) is devastating enough",
				"to instantly kill that foe."};
			case TutorialTopic.Magic:
			return new string[]{
				"Magic skill",
				"",
				"Each time you increase your Magic skill, your",
				"mana (and mana capacity) increases by 5,",
				"and you learn a new spell."};
			case TutorialTopic.MagicTrinkets:
			return new string[]{
				"Magic trinkets",
				"",
				"Sometimes you'll find magical equipment instead",
				"of consumable items. These trinkets give you a",
				"permanent boost or passive effect. ",
				"",
				"(You'll automatically wear all of the",
				"magic trinkets you find.)"};
			case TutorialTopic.MakingNoise:
			return new string[]{
				"Making noise",
				"",
				"Noises can draw nearby enemies to investigate.",
				"",
				"The loudest noises (like explosions) can be heard",
				"from 12 spaces away, while the quietest (like chugging",
				"a potion, or the *CLICK* of a trap) can only be heard",
				"from adjacent spaces.",
				"",
				"Combat will draw attention to both parties from up to 8",
				"spaces, and words of magic (scrolls & spells) from 6.",
				"(A complete list of noise volumes is available in Help,",
				"in the Advanced section.)",
				"",
				"If you make a noise while in plain sight of an enemy,",
				"you'll lose the advantage of stealth and immediately be",
				"spotted. However, if you weren't in plain sight (perhaps",
				"you were around a corner, out of sight range, or invisible)",
				"then enemies won't automatically know where you are.",
				"(This can give you an opportunity to",
				"sneak attack when they come to investigate!)"
			};
			case TutorialTopic.Merciful:
			return new string[]{
				"Merciful",
				"",
				"A merciful weapon will reduce an enemy to 1 HP",
				"but no further.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.Movement:
			return new string[]{
				"Moving around",
				"",
				"Use the numpad [1-9] to move. Press",
				"[5] to wait.",
				"",
				"If you have no numpad, you can use",
				"the arrow keys or [hjkl] to move,",
				"using [yubn] for diagonal moves.",
				"(You can also use the mouse.)",
				"",
				"This tip won't appear again. If you",
				"wish to view all tips, you can find",
				"them by pressing [?] for help."};
			case TutorialTopic.MysticMind:
			return new string[]{
				"Mystic mind",
				"",
				"While your mind is expanded, you'll be able to",
				"sense your foes even if you can't see them -",
				"perfect for countering blindness or",
				"invisibility!",
				"",
				"Additionally, your mental enhancement grants",
				"immunity to stuns, sleep, fear, rage, and",
				"confusion."};
			case TutorialTopic.Negated:
			return new string[]{
				"Negated",
				"",
				"A negated weapon has its enchantment suppressed.",
				"That enchantment won't do anything until",
				"the weapon is no longer negated.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.NotRevealedByLight:
			return new string[]{
				"Darkness, items, and dungeon features",
				"",
				"In darkness, you can't identify the exact",
				"type of items or certain dungeon features",
				"until you're standing on top of them.",
				"For instance, you'll be able to tell it's",
				"a trap, but not what kind of trap it is.",
				"",
				"(Once you've seen something in the light,",
				"you'll remember what it is.)"};
			case TutorialTopic.Oiled:
			return new string[]{
				"Covered in oil",
				"",
				"Being covered in oil has several effects. It's",
				"very easy to catch fire - any fire damage will",
				"instantly ignite you.",
				"",
				"You can't be grabbed or stuck in webs while",
				"covered in a slippery substance.",
				"",
				"However, you have a chance to slip and drop any",
				"consumable item that you try to use."};
			case TutorialTopic.PoolOfRestoration:
			return new string[]{
				"Pools of restoration",
				"",
				"Perhaps a relative of wishing wells, these",
				"pools are a rare feature of the dungeon that",
				"can fully restore your health and mana, and",
				"remove all exhaustion from you.",
				"",
				"To activate a pool of restoration, drop in",
				"an item by pressing [d]."};
			case TutorialTopic.Poppies:
			return new string[]{
				"Poppies",
				"",
				"These bright red flowers are one of the",
				"dungeon's many hazards. Those who breathe",
				"in the poppies for 4 consecutive turns",
				"will drift off into sleep, leaving",
				"them helpless for several turns (or",
				"until damaged)."};
			case TutorialTopic.Possessed:
			return new string[]{
				"Possessed",
				"",
				"A possessed weapon will sometimes choose its own",
				"target. Half of your attacks will be redirected",
				"to a random target within range, including your",
				"original target and yourself.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.RangedAttacks:
			return new string[]{
				"Ranged attacks",
				"",
				"There are some monsters that are best dispatched",
				"at a safe distance. Don't stand next to them!",
				"",
				"You can switch to your bow by pressing [e] to",
				"access the equipment screen.",
				"",
				"Once you've readied your bow, press [s] to shoot."};
			case TutorialTopic.Recovery:
			return new string[]{
				"Recovering health",
				"",
				"Once per dungeon level, you can rest",
				"to restore your health, mana, and exhaustion,",
				"in addition to repairing all damage done to",
				"your weapons and armor.",
				"",
				"Press [r], and if you remain undisturbed for",
				"10 turns, you'll successfully recover.",
				"",
				"For minor wounds, [a]pply the bandages that are in",
				"your pack. You'll gradually recover 20 HP."};
			case TutorialTopic.Roots:
			return new string[]{
				"Roots",
				"",
				"While you're rooted to the ground, you are",
				"entirely immobile. Walking, teleportation,",
				"knockback, and similar effects will fail.",
				"",
				"Your Defense skill is also temporarily",
				"increased by 10 points, making attacks",
				"30% less likely to hit you."};
			case TutorialTopic.Shrines:
			return new string[]{
				"Shrines",
				"",
				"Shrines will provide a permanent boost to",
				"one of your 5 skills (Combat, Defense,",
				"Magic, Spirit, Stealth) when you activate",
				"them by pressing [g].",
				"",
				"Some shrines appear in pairs. When you activate",
				"one, the other will also be depleted."};
			case TutorialTopic.ShinyPlateArmor:
			return new string[]{
				"Shiny plate armor",
				"",
				"Your plate armor is polished and shiny.",
				"As a result, standing in the light gives",
				"away your position as though you were",
				"holding a torch."};
			case TutorialTopic.Silenced:
			return new string[]{
				"Silenced",
				"",
				"While silenced, you can't read scrolls or",
				"cast spells, and any sounds originating",
				"from your location are suppressed.",
				"",
				"An aura of silence extends this effect to",
				"everything within 2 spaces."};
			case TutorialTopic.Slimed:
			return new string[]{
				"Slimed",
				"",
				"Being covered in slime has several effects. You",
				"can't be set on fire while covered in slime",
				"(although you take fire damage as normal). Cold",
				"causes the slime to harden and fall off.",
				"",
				"You can't be grabbed or stuck in webs while",
				"covered in a slippery substance.",
				"",
				"However, you have a chance to slip and drop any",
				"consumable item that you try to use."};
			case TutorialTopic.SpellFailure:
			return new string[]{
				"Spell failure",
				"",
				"If you're exhausted enough, your spells can",
				"fail when you try to cast them. Your turn will",
				"be used up, but you won't lose any mana.",
				"",
				"Each point of exhaustion above 20% adds to",
				"the failure rate of a tier 5 spell. Each point",
				"above 40% affects tier 4 spells, and so on.",
				"",
				"(Therefore, at 45% exhaustion a tier 5 spell",
				"has a 25% chance to fail, a tier 4 spell has",
				"a 5% chance to fail, and a tier 3 or lower",
				"spell will never fail.)"};
			case TutorialTopic.SpellTiers:
			return new string[]{
				"Spell tiers",
				"",
				"Spells are divided into 5 tiers, based on",
				"their power and difficulty. The most basic",
				"are tier 1, while tier 5 contains the most",
				"powerful.",
				"",
				"Spell tier affects two things: Mana cost,",
				"and failure chance while exhausted.",
				"",
				"(Each spell costs an amount of mana equal",
				"to its tier. Higher tiers are more",
				"difficult, so they're the first to be",
				"affected by exhaustion.)"};
			case TutorialTopic.Spirit:
			return new string[]{
				"Spirit skill",
				"",
				"This skill can entirely prevent certain",
				"temporary effects like stuns, poison, fear,",
				"and blindness.",
				"",
				"Each point of Spirit skill increases your",
				"chance to shrug off these effects by 8%.",
				"",
				"(Spirit won't counteract the effects of potions,",
				"and doesn't prevent external statuses like",
				"burning, freezing, or being caught in a web.)"};
			case TutorialTopic.Stealth:
			return new string[]{
				"Stealth skill",
				"",
				"Each point of Stealth skill increases the",
				"average time it takes for an enemy to",
				"notice you.",
				"",
				"Carrying a light source gives away your",
				"position. Standing in darkness gives an",
				"effective bonus of +2 to your Stealth."};
			case TutorialTopic.Stoneform:
			return new string[]{
				"Stoneform",
				"",
				"Stoneform transformation has several effects:",
				"",
				"Your rocky skin makes it impossible for you",
				"to catch fire and reduces all damage by 1.",
				"",
				"You become immune to effects that work only",
				"on living creatures. This includes life drain",
				"and toxins of all types.",
				"",
				"However, this also renders you immune to the",
				"effects of all potions. (Any active potion",
				"effects will end.)"};
			case TutorialTopic.StoneSlab:
			return new string[]{
				"Stone slabs",
				"",
				"These barriers will open when light",
				"shines upon them."};
			case TutorialTopic.Stuck:
			return new string[]{
				"Stuck",
				"",
				"A weapon (or armor) affected by this curse is",
				"impossible to drop or remove. You won't be able",
				"to switch to other weapons (or armors).",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.Stunned:
			return new string[]{
				"Stunned",
				"",
				"While stunned, you can walk normally, but any",
				"other action might fail, causing you to stagger",
				"in a random direction."};
			case TutorialTopic.SwitchingEquipment:
			return new string[]{
				"Switching equipment",
				"",
				"You have several different weapons and armors",
				"available to you at all times. Each has its own",
				"strengths and weaknesses, so pick the best for",
				"each situation.",
				"",
				"You can switch to another weapon or armor by",
				"pressing [e] to access the equipment screen."};
			case TutorialTopic.Torch:
			return new string[]{
				"Using your torch",
				"",
				"You carry a torch that illuminates",
				"your surroundings, but its light makes",
				"your presence obvious to enemies.",
				"",
				"To put your torch away (or bring it",
				"back out), press [t].",
				"",
				"You won't be able to see quite as far without",
				"your torch (and you'll have a harder time",
				"spotting hidden things), but you'll be able",
				"to sneak around without automatically",
				"alerting monsters."};
			case TutorialTopic.Traps:
			return new string[]{
				"Traps",
				"",
				"These dangerous mechanisms are triggered when",
				"something steps or lands on them. They have a",
				"variety of effects - you'll be able to",
				"identify the type of a trap if you examine it",
				"in the light.",
				"",
				"Once a trap has been triggered, it won't",
				"trigger again. You can [f]ling items onto",
				"traps to set them off from a safe distance."};
			case TutorialTopic.UnidentifiedConsumables:
			return new string[]{
				"Trying consumable items",
				"",
				"It's important to try the items that you find",
				"in the dungeon. Without learning what they do,",
				"you won't know which items can help you in an",
				"emergency."};
			case TutorialTopic.Vampirism:
			return new string[]{
				"Vampirism",
				"",
				"While vampiric, you float in the air, avoiding",
				"traps and some other terrain hazards. You also",
				"drain life from living foes on successful",
				"attacks.",
				"",
				"However, exposure to light will leave you",
				"vulnerable. (Vulnerability will make you take",
				"an extra 3d6 damage the next time you're hit.)"};
			case TutorialTopic.Vulnerable:
			return new string[]{
				"Vulnerable",
				"",
				"While you're vulnerable, taking any major",
				"damage will activate its effect. You'll take",
				"3d6 extra damage, after which you'll",
				"no longer be vulnerable."};
			case TutorialTopic.WeakPoint:
			return new string[]{
				"Weak point",
				"",
				"Wearing armor that has a weak point makes it",
				"twice as likely that enemies will score critical",
				"hits on you.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			case TutorialTopic.WornOut:
			return new string[]{
				"Worn out",
				"",
				"If your armor is worn out, its condition isn't",
				"yet bad enough to grant any penalties. However,",
				"further wear can cause it to become damaged",
				"enough to provide no protection at all.",
				"",
				"Like all equipment damage, this effect will end",
				"when you [r]est to repair your equipment."};
			default:
				return new string[0]{};
			}
		}
	}
}