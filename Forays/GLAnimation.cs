/*Copyright (c) 2014-2015  Derrick Creamer
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
using OpenTK;
using OpenTK.Graphics;
using Utilities;
namespace Forays{
	public class AnimationParticle{
		public int frames_left;
		public float row;
		public float col;
		public FloatNumber dy = null;
		public FloatNumber dx = null;
		public int sprite_pixel_row;
		public int sprite_pixel_col;
		public int sprite_h;
		public int sprite_w;
		public Color4 primary_color;
		public Color4 secondary_color;
		public AnimationParticle(int frames,float r,float c,int spritex,int spritey,int spriteh,int spritew,Color4 primary,Color4 secondary){
			frames_left = frames;
			row = r;
			col = c;
			sprite_pixel_row = spritex;
			sprite_pixel_col = spritey;
			sprite_h = spriteh;
			sprite_w = spritew;
			primary_color = primary;
			secondary_color = secondary;
		}
	}
	public class ParticleGenerator{
		public int sprite_pixel_row;
		public int sprite_pixel_col;
		public int sprite_h;
		public int sprite_w;
		public Color4 primary_color;
		public Color4 secondary_color;
		public float origin_row;
		public float origin_col;
		public FloatNumber dist_from_origin;
		public FloatNumber delta_row;
		public FloatNumber delta_col;
		public Number num_per_frame;
		public Number duration_of_particle;
		public int total_frames;
		public int current_frame = 0;
		public ParticleGenerator(int sprite_pixel_row_,int sprite_pixel_col_,int sprite_h_,int sprite_w_,Color4 primary_color_,Color4 secondary_color_,
		                         float origin_row_,float origin_col_,FloatNumber dist_from_origin_,FloatNumber delta_row_,FloatNumber delta_col_,
		                         Number num_per_frame_,Number duration_of_particle_,int total_frames_){
			sprite_pixel_row = sprite_pixel_row_;
			sprite_pixel_col = sprite_pixel_col_;
			sprite_h = sprite_h_;
			sprite_w = sprite_w_;
			primary_color = primary_color_;
			secondary_color = secondary_color_;
			origin_row = origin_row_;
			origin_col = origin_col_;
			dist_from_origin = dist_from_origin_;
			delta_row = delta_row_;
			delta_col = delta_col_;
			num_per_frame = num_per_frame_;
			duration_of_particle = duration_of_particle_;
			total_frames = total_frames_;
		}
		public bool Update(List<AnimationParticle> l){
			if(current_frame >= total_frames){
				return false;
			}
			++current_frame;
			int num_this_frame = num_per_frame.GetValue();
			for(int num=0;num<num_this_frame;++num){
				float angle = (float)(R.r.NextDouble() * 2.0 * Math.PI);
				float dist = dist_from_origin.GetValue();
				l.Add(new AnimationParticle(duration_of_particle.GetValue(),origin_row + (float)Math.Sin(angle) * dist,origin_col + (float)Math.Cos(angle) * dist,0,0,sprite_h,sprite_w,primary_color,secondary_color));
			}
			return true;
		}
	}
	public static class Animations{
		public static List<AnimationParticle> Particles = new List<AnimationParticle>(); //todo: move animations to their own file. Let generators chain into other generators for complex and repeating effects.
		public static List<ParticleGenerator> Generators = new List<ParticleGenerator>();
		public static void Update(){
			List<AnimationParticle> removed = new List<AnimationParticle>();
			foreach(AnimationParticle p in Particles){
				p.frames_left--;
				if(p.frames_left <= 0){
					removed.Add(p);
				}
				else{
					if(p.dy != null){
						p.row += p.dy.GetValue();
					}
					if(p.dx != null){
						p.col += p.dx.GetValue();
					}
				}
			}
			foreach(AnimationParticle p in removed){
				Particles.Remove(p);
			}
			Generators.RemoveWhere(g => !g.Update(Particles));
			//GLGame.particle_surface.Disabled = false;
			Game.gl.UpdateParticles(GLGame.particle_surface,Particles);
			//Game.gl.Render();
			//Thread.Sleep(20);
			//GLGame.particle_surface.Disabled = true;
			//GLGame.particle_surface.NumElements = 0;
		}
		public static void DoStuffTodoRename(int num_per_frame_todo,int duration_todo,int num_frames_todo){
			GLGame.particle_surface.Disabled = false;
			List<AnimationParticle> l = new List<AnimationParticle>();
			float player_row = (float)Actor.player.row - 0.325f;
			float player_col;
			{
				int graphics_mode_first_col = Screen.screen_center_col - 16;
				int graphics_mode_last_col = Screen.screen_center_col + 16;
				if(graphics_mode_first_col < 0){
					graphics_mode_last_col -= graphics_mode_first_col;
					graphics_mode_first_col = 0;
				}
				else{
					if(graphics_mode_last_col >= Global.COLS){
						int diff = graphics_mode_last_col - (Global.COLS-1);
						graphics_mode_first_col -= diff;
						graphics_mode_last_col = Global.COLS-1;
					}
				}
				player_col = (float)(Actor.player.col - graphics_mode_first_col) + 0.4375f;
			}
			float dx = 0.1f;
			float dy = 0.1f;
			for(int time=0;time<num_frames_todo + duration_todo - 1;++time){
				for(int num=0;num<num_per_frame_todo;++num){
					l.Add(new AnimationParticle(time+duration_todo,player_row + (float)(R.Between(-100,100)) / 10,player_col + (float)(R.Between(-100,100)) / 10,0,0,5,3,Color4.Magenta,Color4.Yellow));
					//l.Add(new AnimationParticle(time+duration_todo,player_col,player_row,0,0,5,3,Color4.DarkRed,Color4.Cyan)); //todo
				}
				l.RemoveWhere(p => time >= p.frames_left);
				foreach(AnimationParticle p in l){
					p.col += dx;
					p.row += dy;
				}
				Game.gl.UpdateParticles(GLGame.particle_surface,l);
				Game.gl.Render(); //todo: update or render?
				Thread.Sleep(20);
			}
			GLGame.particle_surface.Disabled = true;
			GLGame.particle_surface.NumElements = 0;
		}
	}
}
