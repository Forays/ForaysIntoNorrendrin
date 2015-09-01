//
using System;
using OpenTK.Graphics;
using Utilities;
namespace Forays{
	public enum Color{Black,White,Gray,Red,Green,Blue,Yellow,Magenta,Cyan,DarkGray,DarkRed,DarkGreen,DarkBlue,DarkYellow,
		DarkMagenta,DarkCyan,RandomFire,RandomIce,RandomLightning,RandomBreached,RandomExplosion,RandomGlowingFungus,
		RandomTorch,RandomDoom,RandomConfusion,RandomDark,RandomBright,RandomRGB,RandomDRGB,RandomRGBW,RandomCMY,
		RandomDCMY,RandomCMYW,RandomRainbow,RandomAny,OutOfSight,TerrainDarkGray,DarkerGray,HealthBar,StatusEffectBar,DarkerRed,
		DarkerMagenta,Transparent};
	public static class Colors{
		public const Color darkcolor = Color.DarkCyan; //for darkened map objects
		public const Color unseencolor = Color.OutOfSight;
		public const Color status_highlight = Color.Green;
		public static ConsoleColor GetColor(Color c){
			switch(c){
			case Color.Black:
			return ConsoleColor.Black;
			case Color.White:
			return ConsoleColor.White;
			case Color.Gray:
			return ConsoleColor.Gray;
			case Color.Red:
			return ConsoleColor.Red;
			case Color.Green:
			return ConsoleColor.Green;
			case Color.Blue:
			return ConsoleColor.Blue;
			case Color.Yellow:
			return ConsoleColor.Yellow;
			case Color.Magenta:
			return ConsoleColor.Magenta;
			case Color.Cyan:
			return ConsoleColor.Cyan;
			case Color.DarkGray:
			return ConsoleColor.DarkGray;
			case Color.DarkRed:
			return ConsoleColor.DarkRed;
			case Color.DarkGreen:
			return ConsoleColor.DarkGreen;
			case Color.DarkBlue:
			return ConsoleColor.DarkBlue;
			case Color.DarkYellow:
			return ConsoleColor.DarkYellow;
			case Color.DarkMagenta:
			return ConsoleColor.DarkMagenta;
			case Color.DarkCyan:
			return ConsoleColor.DarkCyan;
			case Color.RandomFire:
			case Color.RandomIce:
			case Color.RandomLightning:
			case Color.RandomBreached:
			case Color.RandomExplosion:
			case Color.RandomGlowingFungus:
			case Color.RandomTorch:
			case Color.RandomDoom:
			case Color.RandomConfusion:
			case Color.RandomDark:
			case Color.RandomBright:
			case Color.RandomRGB:
			case Color.RandomDRGB:
			case Color.RandomRGBW:
			case Color.RandomCMY:
			case Color.RandomDCMY:
			case Color.RandomCMYW:
			case Color.RandomRainbow:
			case Color.RandomAny:
			case Color.OutOfSight:
			case Color.TerrainDarkGray:
			case Color.HealthBar:
			case Color.StatusEffectBar:
			return GetColor(ResolveColor(c));
			default:
			return ConsoleColor.Black;
			}
		}
		public static Color ResolveColor(Color c){
			switch(c){
			case Color.RandomFire:
			return R.Choose(Color.Red,Color.DarkRed,Color.Yellow);
			case Color.RandomIce:
			return R.Choose(Color.White,Color.Cyan,Color.Blue,Color.DarkBlue);
			case Color.RandomLightning:
			return R.Choose(Color.White,Color.Yellow,Color.Yellow,Color.DarkYellow);
			case Color.RandomBreached:
			if(R.OneIn(4)){
				return Color.DarkGreen;
			}
			return Color.Green;
			case Color.RandomExplosion:
			if(R.OneIn(4)){
				return Color.Red;
			}
			return Color.DarkRed;
			case Color.RandomGlowingFungus:
			if(R.OneIn(35)){
				return Color.DarkCyan;
			}
			return Color.Cyan;
			case Color.RandomTorch:
			if(R.OneIn(8)){
				if(R.CoinFlip()){
					return Color.White;
				}
				else{
					return Color.Red;
				}
			}
			return Color.Yellow;
			case Color.RandomDoom:
			return R.Choose(Color.DarkGray,Color.DarkGray,Color.DarkRed,Color.DarkMagenta);
			case Color.RandomConfusion:
			if(R.OneIn(16)){
				return R.Choose(Color.Red,Color.Green,Color.Blue,Color.Cyan,Color.Yellow,Color.White);
			}
			return Color.Magenta;
			case Color.RandomDark:
			return R.Choose(Color.DarkBlue,Color.DarkCyan,Color.DarkGray,Color.DarkGreen,Color.DarkMagenta,Color.DarkRed,Color.DarkYellow);
			case Color.RandomBright:
			return R.Choose(Color.Red,Color.Green,Color.Blue,Color.Cyan,Color.Yellow,Color.Magenta,Color.White,Color.Gray);
			case Color.RandomRGB:
			return R.Choose(Color.Red,Color.Green,Color.Blue);
			case Color.RandomDRGB:
			return R.Choose(Color.DarkRed,Color.DarkGreen,Color.DarkBlue);
			case Color.RandomRGBW:
			return R.Choose(Color.Red,Color.Green,Color.Blue,Color.White);
			case Color.RandomCMY:
			return R.Choose(Color.Cyan,Color.Magenta,Color.Yellow);
			case Color.RandomDCMY:
			return R.Choose(Color.DarkCyan,Color.DarkMagenta,Color.DarkYellow);
			case Color.RandomCMYW:
			return R.Choose(Color.Cyan,Color.Magenta,Color.Yellow,Color.White);
			case Color.RandomRainbow:
			return R.Choose(Color.Red,Color.Green,Color.Blue,Color.Cyan,Color.Yellow,Color.Magenta,Color.DarkBlue,Color.DarkCyan,Color.DarkGreen,Color.DarkMagenta,Color.DarkRed,Color.DarkYellow);
			case Color.RandomAny:
			return R.Choose(Color.Red,Color.Green,Color.Blue,Color.Cyan,Color.Yellow,Color.Magenta,Color.DarkBlue,Color.DarkCyan,Color.DarkGreen,Color.DarkMagenta,Color.DarkRed,Color.DarkYellow,Color.White,Color.Gray,Color.DarkGray);
			case Color.OutOfSight:
			if(Global.Option(OptionType.DARK_GRAY_UNSEEN)){
				if(Screen.GLMode){
					return Color.DarkerGray;
				}
				else{
					return Color.DarkGray;
				}
			}
			else{
				return Color.DarkBlue;
			}
			case Color.TerrainDarkGray:
			if(Screen.GLMode || !Global.Option(OptionType.DARK_GRAY_UNSEEN)){
				return Color.DarkGray;
			}
			else{
				return Color.Gray;
			}
			case Color.HealthBar:
			if(Screen.GLMode){
				return Color.DarkerRed;
			}
			else{
				return Color.DarkRed;
			}
			case Color.StatusEffectBar:
			if(Screen.GLMode){
				return Color.DarkerMagenta;
			}
			else{
				return Color.DarkMagenta;
			}
			default:
			return c;
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
			case Color.DarkerRed:
			return new Color4(80,0,0,255); //DarkRed is 139 red
			case Color.DarkerMagenta:
			return new Color4(80,0,80,255); //DarkMagenta is 139 red and blue
			case Color.Transparent:
			return Color4.Transparent;
			default:
			return Color4.Black;
			}
		}
	}
}
