/*Copyright (c) 2011-2014  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
namespace Forays{
	public static class Spell{
		public static int Tier(SpellType spell){
			switch(spell){
			case SpellType.RADIANCE:
			case SpellType.FORCE_PALM:
			case SpellType.DETECT_MOVEMENT:
			case SpellType.FLYING_LEAP:
				return 1;
			case SpellType.MERCURIAL_SPHERE:
			case SpellType.GREASE:
			case SpellType.BLINK:
			case SpellType.FREEZE:
				return 2;
			case SpellType.SCORCH:
			case SpellType.LIGHTNING_BOLT:
			case SpellType.MAGIC_HAMMER:
			case SpellType.PORTAL:
				return 3;
			case SpellType.PASSAGE:
			case SpellType.AMNESIA:
			case SpellType.STONE_SPIKES:
			case SpellType.SHADOWSIGHT:
				return 4;
			case SpellType.BLIZZARD:
			case SpellType.COLLAPSE:
			case SpellType.DOOM:
			case SpellType.TELEKINESIS:
				return 5;
			default:
				return 5;
			}
		}
		public static int FailRate(SpellType spell,int exhaustion){
			return Math.Max(0,exhaustion - ((6-Tier(spell)) * 20)); //tier 5 spells have a 1% fail rate at 21% exhaustion...tier 4 at 41%...and so on.
		}
		public static string Name(SpellType spell){
			switch(spell){
			case SpellType.RADIANCE:
				return "Radiance";
			case SpellType.FORCE_PALM:
				return "Force palm";
			case SpellType.DETECT_MOVEMENT:
				return "Detect movement";
			case SpellType.FLYING_LEAP:
				return "Flying leap";
			case SpellType.MERCURIAL_SPHERE:
				return "Mercurial sphere";
			case SpellType.GREASE:
				return "Grease";
			case SpellType.BLINK:
				return "Blink";
			case SpellType.FREEZE:
				return "Freeze";
			case SpellType.SCORCH:
				return "Scorch";
			case SpellType.LIGHTNING_BOLT:
				return "Lightning bolt";
			case SpellType.MAGIC_HAMMER:
				return "Magic hammer";
			case SpellType.PORTAL:
				return "Portal";
			case SpellType.PASSAGE:
				return "Passage";
			case SpellType.DOOM:
				return "Doom";
			case SpellType.AMNESIA:
				return "Amnesia";
			case SpellType.SHADOWSIGHT:
				return "Shadowsight";
			case SpellType.BLIZZARD:
				return "Blizzard";
			case SpellType.COLLAPSE:
				return "Collapse";
			case SpellType.TELEKINESIS:
				return "Telekinesis";
			case SpellType.STONE_SPIKES:
				return "Stone spikes";
			default:
				return "unknown spell";
			}
		}
		public static bool IsDamaging(SpellType spell){
			switch(spell){
			case SpellType.BLIZZARD:
			case SpellType.COLLAPSE:
			case SpellType.STONE_SPIKES:
			case SpellType.FORCE_PALM:
			case SpellType.DOOM:
			case SpellType.LIGHTNING_BOLT:
			case SpellType.MAGIC_HAMMER:
			case SpellType.MERCURIAL_SPHERE:
			case SpellType.RADIANCE:
				return true;
			}
			return false;
		}
		public static colorstring Description(SpellType spell){
			switch(spell){
			case SpellType.RADIANCE:
				return new colorstring("Light source brightens and deals 1d6 ",Color.Gray);
			case SpellType.FORCE_PALM:
				return new colorstring("Deals 1d6 damage, knocks target back ",Color.Gray);
			case SpellType.DETECT_MOVEMENT:
				return new colorstring("Nearby movement is revealed          ",Color.Gray);
			case SpellType.FLYING_LEAP:
				return new colorstring("Fly and move at double speed briefly ",Color.Gray);
			case SpellType.MERCURIAL_SPHERE:
				return new colorstring("2d6, bounces to nearby foes 3 times  ",Color.Gray);
			case SpellType.GREASE:
				return new colorstring("Creates a pool of oil on the floor   ",Color.Gray);
			case SpellType.BLINK:
				return new colorstring("Teleport a short distance randomly   ",Color.Gray);
			case SpellType.FREEZE:
				return new colorstring("Encase your target in ice            ",Color.Gray);
			case SpellType.SCORCH:
				return new colorstring("Set your target on fire              ",Color.Gray);
			case SpellType.LIGHTNING_BOLT:
				return new colorstring("3d6, jumps to other nearby foes      ",Color.Gray);
			case SpellType.MAGIC_HAMMER:
				return new colorstring("Range 1, deals 4d6 damage and stuns  ",Color.Gray);
			case SpellType.PORTAL:
				return new colorstring("Create linked teleportation portals  ",Color.Gray);
			case SpellType.PASSAGE:
				return new colorstring("Travel to the other side of a wall   ",Color.Gray);
			case SpellType.DOOM:
				return new colorstring("4d6 damage, inflicts vulnerability   ",Color.Gray);
			case SpellType.AMNESIA:
				return new colorstring("An enemy forgets your presence       ",Color.Gray);
			case SpellType.SHADOWSIGHT:
				return new colorstring("See farther and better in darkness   ",Color.Gray);
			case SpellType.BLIZZARD:
				return new colorstring("Radius 5 burst, 5d6 and slows enemies",Color.Gray);
			case SpellType.TELEKINESIS:
				return new colorstring("Throw your target forcefully         ",Color.Gray);
			case SpellType.COLLAPSE:
				return new colorstring("4d6, breaks walls & drops rubble",Color.Gray);
				//return new colorstring("Radius 1, breaks walls & drops rubble",Color.Gray);
			case SpellType.STONE_SPIKES:
				return new colorstring("Radius 2, 4d6 and creates stalagmites",Color.Gray);
			default:
				return new colorstring("  Unknown.                           ",Color.Gray);
			}
		}
		public static colorstring DescriptionWithIncreasedDamage(SpellType spell){
			switch(spell){
			case SpellType.RADIANCE:
				return new colorstring("Light source brightens and deals ",Color.Gray,"2d6 ",Color.Yellow);
			case SpellType.FORCE_PALM:
				return new colorstring("Deals ",Color.Gray,"2d6",Color.Yellow," damage, knocks target back ",Color.Gray);
			case SpellType.COLLAPSE:
				return new colorstring("4d6",Color.Yellow,", breaks walls & drops rubble",Color.Gray);
			case SpellType.STONE_SPIKES:
				return new colorstring("Radius 2, ",Color.Gray,"4d6",Color.Yellow," and creates stalagmites",Color.Gray);
			case SpellType.MERCURIAL_SPHERE:
				return new colorstring("3d6",Color.Yellow,", bounces to nearby foes 3 times  ",Color.Gray);
			case SpellType.LIGHTNING_BOLT:
				return new colorstring("4d6",Color.Yellow,", jumps to other nearby foes      ",Color.Gray);
			case SpellType.MAGIC_HAMMER:
				return new colorstring("Range 1, deals ",Color.Gray,"5d6",Color.Yellow," damage and stuns  ",Color.Gray);
			case SpellType.DOOM:
				return new colorstring("5d6",Color.Yellow," damage, inflicts vulnerability   ",Color.Gray);
			case SpellType.BLIZZARD:
				return new colorstring("Radius 5 burst, ",Color.Gray,"6d6",Color.Yellow," and slows enemies",Color.Gray);
			default:
				return Description(spell);
			}
		}
	}
}

