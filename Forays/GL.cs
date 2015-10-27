/*Copyright (c) 2014-2015  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace GLDrawing{
	public enum ResizeOption{StretchToFit,AddBorder,SnapWindow,NoResize};
	public class GLWindow : GameWindow{
		public List<Surface> Surfaces = new List<Surface>();

		public ResizeOption ResizingPreference = ResizeOption.StretchToFit;
		public ResizeOption ResizingFullScreenPreference = ResizeOption.StretchToFit;
		protected bool Resizing = false;
		public int SnapWidth = 1; //if EnforceRatio is true, the AddBorder and SnapWindow options will require that the new multiples of SnapHeight and SnapWidth be equal.
		public int SnapHeight = 1; //Assuming SnapW is 100 and SnapH is 50:  If EnforceRatio is true, the valid sizes are 100x50, 200x100, 300x150, and so on.
		public bool EnforceRatio = false; //If EnforceRatio is false, 100x750 is a valid size, as are 900x50 and 200x200.

		public bool NoClose = false;
		public bool FullScreen = false;
		
		protected FrameEventArgs render_args = new FrameEventArgs();
		protected Dictionary<Key,bool> key_down = new Dictionary<Key,bool>();
		protected bool DepthTestEnabled = false;
		protected int LastShaderID = -1;

		public Action FinalResize = null;
		protected Rectangle internalViewport;
		public Rectangle Viewport{ get{ return internalViewport; }
			set{
				internalViewport = value;
				GL.Viewport(value);
			}
		}
		public void SetViewport(int x,int y,int width,int height){
			internalViewport = new Rectangle(x,y,width,height);
			GL.Viewport(x,y,width,height);
		}

		public GLWindow(int w,int h,string title) : base(w,h,GraphicsMode.Default,title){
			VSync = VSyncMode.On;
			GL.ClearColor(0.0f,0.0f,0.0f,0.0f);
			GL.EnableVertexAttribArray(0); //these 2 attrib arrays are always on, for position and texcoords.
			GL.EnableVertexAttribArray(1);
			KeyDown += KeyDownHandler;
			KeyUp += KeyUpHandler;
			Keyboard.KeyRepeat = true;
			internalViewport = new Rectangle(0,0,w,h);
		}
		protected virtual void KeyDownHandler(object sender,KeyboardKeyEventArgs args){
			key_down[args.Key] = true;
		}
		protected virtual void KeyUpHandler(object sender,KeyboardKeyEventArgs args){
			key_down[args.Key] = false;
		}
		public bool KeyIsDown(Key key){
			bool value;
			key_down.TryGetValue(key,out value);
			return value;
		}
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e){
			e.Cancel = NoClose;
			base.OnClosing(e);
		}
		protected override void OnFocusedChanged(EventArgs e){
			base.OnFocusedChanged(e);
			if(Focused){
				key_down[Key.AltLeft] = false; //i could simply reset the whole dictionary, too...
				key_down[Key.AltRight] = false;
				key_down[Key.ShiftLeft] = false;
				key_down[Key.ShiftRight] = false;
				key_down[Key.ControlLeft] = false;
				key_down[Key.ControlRight] = false;
			}
		}
		protected override void OnResize(EventArgs e){
			Resizing = true;
		}
		public void DefaultHandleResize(){
			ResizeOption pref = ResizingPreference;
			if(FullScreen){
				pref = ResizingFullScreenPreference;
			}
			switch(pref){
			case ResizeOption.StretchToFit:
				SetViewport(ClientRectangle.X,ClientRectangle.Y,ClientRectangle.Width,ClientRectangle.Height);
				break;
			case ResizeOption.AddBorder: //should AddBorder allow the screen to be resized below SnapWidth x SnapHeight? Maybe an option?
			{
				int height_multiple = ClientRectangle.Height / SnapHeight;
				int width_multiple = ClientRectangle.Width / SnapWidth;
				if(EnforceRatio){
					int min = Math.Min(height_multiple,width_multiple);
					height_multiple = min;
					width_multiple = min;
				}
				if(height_multiple < 1){
					height_multiple = 1;
				}
				if(width_multiple < 1){
					width_multiple = 1;
				}
				int view_height = SnapHeight * height_multiple;
				int view_width = SnapWidth * width_multiple;
				SetViewport((ClientRectangle.Width - view_width)/2,(ClientRectangle.Height - view_height)/2,view_width,view_height);
				break;
			}
			case ResizeOption.SnapWindow: //you probably don't want to use this option for fullscreen.
			{
				int height_multiple = ClientRectangle.Height / SnapHeight;
				int width_multiple = ClientRectangle.Width / SnapWidth;
				if(EnforceRatio){
					int min = Math.Min(height_multiple,width_multiple);
					height_multiple = min;
					width_multiple = min;
				}
				if(height_multiple < 1){
					height_multiple = 1;
				}
				if(width_multiple < 1){
					width_multiple = 1;
				}
				Height = SnapHeight * height_multiple;
				Width = SnapWidth * width_multiple;
				SetViewport(ClientRectangle.X,ClientRectangle.Y,ClientRectangle.Width,ClientRectangle.Height);
				break;
			}
			case ResizeOption.NoResize:
				Height = SnapHeight;
				Width = SnapWidth;
				break;
			}
		}
		public void ToggleFullScreen(){ ToggleFullScreen(!FullScreen); }
		public void ToggleFullScreen(bool fullscreen_on){
			FullScreen = fullscreen_on;
			if(fullscreen_on){
				WindowState = WindowState.Fullscreen;
			}
			else{
				WindowState = WindowState.Normal;
			}
			Resizing = true; //todo: changed this to simply set Resizing instead of actually calling resizing methods.
		}
		public bool WindowUpdate(){
			ProcessEvents();
			if(IsExiting){
				return false;
			}
			if(Resizing){
				//HandleResize();
				FinalResize?.Invoke();
				Resizing = false;
			}
			DrawSurfaces();
			return true;
		}
		public void DrawSurfaces(){
			base.OnRenderFrame(render_args);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			foreach(Surface s in Surfaces){
				if(!s.Disabled){
					if(DepthTestEnabled != s.UseDepthBuffer){
						if(s.UseDepthBuffer){
							GL.Enable(EnableCap.DepthTest);
						}
						else{
							GL.Disable(EnableCap.DepthTest);
						}
						DepthTestEnabled = s.UseDepthBuffer;
					}
					if(LastShaderID != s.shader.ShaderProgramID){
						GL.UseProgram(s.shader.ShaderProgramID);
						LastShaderID = s.shader.ShaderProgramID;
					}
					GL.Uniform2(s.shader.OffsetUniformLocation,s.raw_x_offset,s.raw_y_offset);
					GL.Uniform1(s.shader.TextureUniformLocation,s.texture.TextureIndex);
					GL.BindBuffer(BufferTarget.ElementArrayBuffer,s.vbo.ElementArrayBufferID);
					GL.BindBuffer(BufferTarget.ArrayBuffer,s.vbo.PositionArrayBufferID);
					GL.VertexAttribPointer(0,s.vbo.PositionDimensions,VertexAttribPointerType.Float,false,sizeof(float)*s.vbo.PositionDimensions,new IntPtr(0)); //position
					GL.BindBuffer(BufferTarget.ArrayBuffer,s.vbo.OtherArrayBufferID);
					int stride = sizeof(float) * s.vbo.VertexAttribs.TotalSize;
					GL.VertexAttribPointer(1,s.vbo.VertexAttribs.Size[0],VertexAttribPointerType.Float,false,stride,new IntPtr(0)); //texcoords
					int total_of_previous_attribs = s.vbo.VertexAttribs.Size[0];
					for(int i=1;i<s.vbo.VertexAttribs.Size.Length;++i){
						GL.EnableVertexAttribArray(i+1); //i+1 because 0 and 1 are always on (for position & texcoords)
						GL.VertexAttribPointer(i+1,s.vbo.VertexAttribs.Size[i],VertexAttribPointerType.Float,false,stride,new IntPtr(sizeof(float)*total_of_previous_attribs));
						total_of_previous_attribs += s.vbo.VertexAttribs.Size[i];
					}
					GL.DrawElements(PrimitiveType.Triangles,s.vbo.NumElements,DrawElementsType.UnsignedInt,IntPtr.Zero);
					for(int i=1;i<s.vbo.VertexAttribs.Size.Length;++i){
						GL.DisableVertexAttribArray(i+1);
					}
				}
			}
			SwapBuffers();
		}
		/*public static void ReplaceTexture(int texture_unit,string filename){ //binds a texture to the given texture unit, replacing the texture that's already there
			if(String.IsNullOrEmpty(filename)){
				throw new ArgumentException(filename);
			}
			GL.ActiveTexture(TextureUnit.Texture0 + texture_unit);
			int id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D,id);
			Bitmap bmp = new Bitmap(filename);
			BitmapData bmp_data = bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba,bmp_data.Width,bmp_data.Height,0,OpenTK.Graphics.OpenGL.PixelFormat.Bgra,PixelType.UnsignedByte,bmp_data.Scan0);
			bmp.UnlockBits(bmp_data);
			GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
		}*/
		public void UpdatePositionVertexArray(Surface s,IList<int> index_list,IList<int> layout_list = null){
			UpdatePositionVertexArray(s,-1,index_list,layout_list);
		}
		public void UpdatePositionVertexArray(Surface s,int start_index,IList<int> index_list,IList<int> layout_list = null){
			int count = index_list.Count;
			if(layout_list == null){
				layout_list = new int[count]; //if not supplied, assume layout 0.
			}
			float[] values = new float[count * 4 * s.vbo.PositionDimensions]; //2 or 3 dimensions for 4 vertices for each tile
			int[] indices = null;
			if(start_index < 0 && s.vbo.NumElements != count * 6){
				indices = new int[count * 6];
				s.vbo.NumElements = count * 6;
			}
			else{
				if(start_index >= 0){
					if((start_index + count) * 6 > s.vbo.NumElements && s.vbo.NumElements > 0){
						throw new ArgumentException("Error: start_index + count is bigger than VBO size. To always replace the previous data, set start_index to -1.");
					} //todo: I could also just ignore the start_index if there's too much data.
					if((start_index + count) * 4 * s.vbo.PositionDimensions > s.vbo.PositionDataSize && s.vbo.PositionDataSize > 0){
						throw new ArgumentException("Error: (start_index + count) * total_attrib_size is bigger than VBO size. To always replace the previous data, set start_index to -1.");
					}
				}
			}
			float width_ratio = 2.0f / (float)Viewport.Width;
			float height_ratio = 2.0f / (float)Viewport.Height;
			//float width_ratio = 2.0f / (float)ClientRectangle.Width; //todo: eventually this part might not be based on the current window. Seems like a good idea.
			//float height_ratio = 2.0f / (float)ClientRectangle.Height;
			int current_total = 0;
			foreach(int i in index_list){
				float x_offset = (float)s.layouts[layout_list[current_total]].HorizontalOffsetPx;
				float y_offset = (float)s.layouts[layout_list[current_total]].VerticalOffsetPx;
				float x_w = (float)s.layouts[layout_list[current_total]].CellWidthPx;
				float y_h = (float)s.layouts[layout_list[current_total]].CellHeightPx;
				float cellx = s.layouts[layout_list[current_total]].X(i) + x_offset;
				float celly = s.layouts[layout_list[current_total]].Y(i) + y_offset;
				float x = cellx * width_ratio - 1.0f;
				float y = celly * height_ratio - 1.0f;
				float x_plus1 = (cellx + x_w) * width_ratio - 1.0f;
				float y_plus1 = (celly + y_h) * height_ratio - 1.0f;

				int N = s.vbo.PositionDimensions;
				int idxN = current_total * 4 * N;

				values[idxN] = x; //the 4 corners, flipped so it works with the inverted Y axis
				values[idxN + 1] = y_plus1;
				values[idxN + N] = x;
				values[idxN + N + 1] = y;
				values[idxN + N*2] = x_plus1;
				values[idxN + N*2 + 1] = y;
				values[idxN + N*3] = x_plus1;
				values[idxN + N*3 + 1] = y_plus1;
				if(N == 3){
					float z = s.layouts[layout_list[current_total]].Z(i);
					values[idxN + 2] = z;
					values[idxN + N + 2] = z;
					values[idxN + N*2 + 2] = z;
					values[idxN + N*3 + 2] = z;
				}

				if(indices != null){
					int idx4 = current_total * 4;
					int idx6 = current_total * 6;
					indices[idx6] = idx4;
					indices[idx6 + 1] = idx4 + 1;
					indices[idx6 + 2] = idx4 + 2;
					indices[idx6 + 3] = idx4;
					indices[idx6 + 4] = idx4 + 2;
					indices[idx6 + 5] = idx4 + 3;
				}
				current_total++;
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer,s.vbo.PositionArrayBufferID);
			if((start_index < 0 && s.vbo.PositionDataSize != values.Length) || s.vbo.PositionDataSize == 0){
				GL.BufferData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)*values.Length),values,BufferUsageHint.StreamDraw);
				s.vbo.PositionDataSize = values.Length;
			}
			else{
				int offset = start_index;
				if(offset < 0){
					offset = 0;
				}
				GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float) * 4 * s.vbo.PositionDimensions * offset),new IntPtr(sizeof(float) * values.Length),values);
				//GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(0),new IntPtr(sizeof(float) * values.Length),values);
			}
			if(indices != null){
				GL.BindBuffer(BufferTarget.ElementArrayBuffer,s.vbo.ElementArrayBufferID);
				GL.BufferData(BufferTarget.ElementArrayBuffer,new IntPtr(sizeof(int)*indices.Length),indices,BufferUsageHint.StaticDraw);
			}
		}
		public void UpdatePositionSingleVertex(Surface s,int index,int layout = 0){
			float[] values = new float[4 * s.vbo.PositionDimensions]; //2 or 3 dimensions for 4 vertices
			float width_ratio = 2.0f / (float)Viewport.Width;
			float height_ratio = 2.0f / (float)Viewport.Height;
			//float width_ratio = 2.0f / (float)ClientRectangle.Width; //todo: eventually this part might not be based on the current window. Seems like a good idea.
			//float height_ratio = 2.0f / (float)ClientRectangle.Height;
			float x_offset = (float)s.layouts[layout].HorizontalOffsetPx;
			float y_offset = (float)s.layouts[layout].VerticalOffsetPx;
			float x_w = (float)s.layouts[layout].CellWidthPx;
			float y_h = (float)s.layouts[layout].CellHeightPx;
			float cellx = s.layouts[layout].X(index) + x_offset;
			float celly = s.layouts[layout].Y(index) + y_offset;
			float x = cellx * width_ratio - 1.0f;
			float y = celly * height_ratio - 1.0f;
			float x_plus1 = (cellx + x_w) * width_ratio - 1.0f;
			float y_plus1 = (celly + y_h) * height_ratio - 1.0f;

			int N = s.vbo.PositionDimensions;

			values[0] = x; //the 4 corners, flipped so it works with the inverted Y axis
			values[1] = y_plus1;
			values[N] = x;
			values[N + 1] = y;
			values[N*2] = x_plus1;
			values[N*2 + 1] = y;
			values[N*3] = x_plus1;
			values[N*3 + 1] = y_plus1;
			if(N == 3){
				float z = s.layouts[layout].Z(index);
				values[2] = z;
				values[N + 2] = z;
				values[N*2 + 2] = z;
				values[N*3 + 2] = z;
			}

			GL.BindBuffer(BufferTarget.ArrayBuffer,s.vbo.PositionArrayBufferID);
			GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float) * 4 * s.vbo.PositionDimensions * index),new IntPtr(sizeof(float) * values.Length),values);
		}
		public void UpdateOtherVertexArray(Surface s,IList<int> sprite_index,params IList<float>[] vertex_attributes){
			UpdateOtherVertexArray(s,-1,sprite_index,new int[sprite_index.Count],vertex_attributes); //default to sprite type 0.
		} //should I add more overloads here?
		public void UpdateOtherVertexArray(Surface s,int start_index,IList<int> sprite_index,IList<int> sprite_type,params IList<float>[] vertex_attributes){
			int count = sprite_index.Count;
			int a = s.vbo.VertexAttribs.TotalSize;
			int a4 = a * 4;
			if(start_index >= 0 && (start_index + count) * a4 > s.vbo.OtherDataSize && s.vbo.OtherDataSize > 0){
				throw new ArgumentException("Error: (start_index + count) * total_attrib_size is bigger than VBO size. To always replace the previous data, set start_index to -1.");
			}
			if(sprite_type == null){
				sprite_type = new int[sprite_index.Count];
			}
			float[] all_values = new float[count * a4];
			for(int i=0;i<count;++i){
				SpriteType sprite = s.texture.Sprite[sprite_type[i]];
				float tex_start_x = sprite.X(sprite_index[i]);
				float tex_start_y = sprite.Y(sprite_index[i]);
				float tex_end_x = tex_start_x + sprite.SpriteWidth;
				float tex_end_y = tex_start_y + sprite.SpriteHeight;
				float[] values = new float[a4];
				values[0] = tex_start_x; //the 4 corners, texcoords:
				values[1] = tex_end_y;
				values[a] = tex_start_x;
				values[a + 1] = tex_start_y;
				values[a*2] = tex_end_x;
				values[a*2 + 1] = tex_start_y;
				values[a*3] = tex_end_x;
				values[a*3 + 1] = tex_end_y;
				int prev_total = 2;
				for(int g=1;g<s.vbo.VertexAttribs.Size.Length;++g){ //starting at 1 because texcoords are already done
					int attrib_size = s.vbo.VertexAttribs.Size[g];
					for(int k=0;k<attrib_size;++k){
						float attrib = vertex_attributes[g-1][k + i*attrib_size]; // -1 because the vertex_attributes array doesn't contain texcoords here in the update method.
						values[prev_total + k] = attrib;
						values[prev_total + k + a] = attrib;
						values[prev_total + k + a*2] = attrib;
						values[prev_total + k + a*3] = attrib;
					}
					prev_total += attrib_size;
				}
				values.CopyTo(all_values,i * a4);
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer,s.vbo.OtherArrayBufferID);
			if((start_index < 0 && s.vbo.OtherDataSize != a4 * count) || s.vbo.OtherDataSize == 0){
				GL.BufferData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float) * a4 * count),all_values,BufferUsageHint.StreamDraw);
				s.vbo.OtherDataSize = a4 * count;
			}
			else{
				int offset = start_index;
				if(offset < 0){
					offset = 0;
				}
				GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float) * a4 * offset),new IntPtr(sizeof(float) * a4 * count),all_values);
				//GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(0),new IntPtr(sizeof(float) * a4 * count),all_values);
			}
		}
		public void UpdateOtherSingleVertex(Surface s,int index,int sprite_index,int sprite_type,params IList<float>[] vertex_attributes){
			int a = s.vbo.VertexAttribs.TotalSize;
			int a4 = a * 4;
			float[] values = new float[a4];
			SpriteType sprite = s.texture.Sprite[sprite_type];
			float tex_start_x = sprite.X(sprite_index);
			float tex_start_y = sprite.Y(sprite_index);
			float tex_end_x = tex_start_x + sprite.SpriteWidth;
			float tex_end_y = tex_start_y + sprite.SpriteHeight;
			values[0] = tex_start_x; //the 4 corners, texcoords:
			values[1] = tex_end_y;
			values[a] = tex_start_x;
			values[a + 1] = tex_start_y;
			values[a*2] = tex_end_x;
			values[a*2 + 1] = tex_start_y;
			values[a*3] = tex_end_x;
			values[a*3 + 1] = tex_end_y;
			int prev_total = 2;
			for(int g=1;g<s.vbo.VertexAttribs.Size.Length;++g){ //starting at 1 because texcoords are already done
				int attrib_size = s.vbo.VertexAttribs.Size[g];
				for(int k=0;k<attrib_size;++k){
					float attrib = vertex_attributes[g-1][k]; // -1 because the vertex_attributes array doesn't contain texcoords here in the update method.
					values[prev_total + k] = attrib;
					values[prev_total + k + a] = attrib;
					values[prev_total + k + a*2] = attrib;
					values[prev_total + k + a*3] = attrib;
				}
				prev_total += attrib_size;
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer,s.vbo.OtherArrayBufferID);
			GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float) * a4 * index),new IntPtr(sizeof(float) * a4),values);
		}
	}
	public delegate float PositionFromIndex(int idx);
	public delegate void SurfaceUpdateMethod(SurfaceDefaults defaults);
	public class SurfaceDefaults{
		public List<int> positions = null;
		public List<int> layouts = null;
		public List<int> sprites = null;
		public List<int> sprite_types = null;
		public List<float>[] other_data = null;

		public int single_position = -1;
		public int single_layout = -1;
		public int single_sprite = -1;
		public int single_sprite_type = -1;
		public List<float>[] single_other_data = null;

		public int fill_count = -1;
		public SurfaceDefaults(){}
		public SurfaceDefaults(SurfaceDefaults other){
			if(other.positions != null){
				positions = new List<int>(other.positions);
			}
			if(other.layouts != null){
				layouts = new List<int>(other.layouts);
			}
			if(other.sprites != null){
				sprites = new List<int>(other.sprites);
			}
			if(other.sprite_types != null){
				sprite_types = new List<int>(other.sprite_types);
			}
			if(other.other_data != null){
				other_data = new List<float>[other.other_data.GetLength(0)];
				int idx = 0;
				foreach(List<float> l in other.other_data){
					other_data[idx] = new List<float>(l);
					++idx;
				}
			}
			single_position = other.single_position;
			single_layout = other.single_layout;
			single_sprite = other.single_sprite;
			single_sprite_type = other.single_sprite_type;
			single_other_data = other.single_other_data;
			fill_count = other.fill_count;
		}
		public void FillValues(bool fill_positions,bool fill_other_data){
			if(fill_count <= 0){
				return;
			}
			if(fill_positions){
				if(single_position != -1){ //if this value is -1, nothing can be added anyway.
					if(positions == null){
						positions = new List<int>();
					}
					while(positions.Count < fill_count){
						positions.Add(single_position);
					}
				}
				if(single_layout != -1){
					if(layouts == null){
						layouts = new List<int>();
					}
					while(layouts.Count < fill_count){
						layouts.Add(single_layout);
					}
				}
			}
			if(fill_other_data){
				if(single_sprite != -1){
					if(sprites == null){
						sprites = new List<int>();
					}
					while(sprites.Count < fill_count){
						sprites.Add(single_sprite);
					}
				}
				if(single_sprite_type != -1){
					if(sprite_types == null){
						sprite_types = new List<int>();
					}
					while(sprite_types.Count < fill_count){
						sprite_types.Add(single_sprite_type);
					}
				}
				if(single_other_data != null){
					if(other_data == null){
						other_data = new List<float>[single_other_data.GetLength(0)];
						for(int i=0;i<other_data.GetLength(0);++i){
							other_data[i] = new List<float>();
						}
					}
					int idx = 0;
					foreach(List<float> l in other_data){
						while(l.Count < fill_count * single_other_data[idx].Count){
							foreach(float f in single_other_data[idx]){
								l.Add(f);
							}
						}
						++idx;
					}
				}
			}
		}
	}
	public class Surface{
		public GLWindow window;
		public VBO vbo;
		public Texture texture;
		public Shader shader;
		public List<CellLayout> layouts = new List<CellLayout>();
		protected SurfaceDefaults defaults = new SurfaceDefaults();
		public float raw_x_offset = 0.0f;
		public float raw_y_offset = 0.0f; //todo: should be properties
		private int x_offset_px;
		private int y_offset_px;
		public bool UseDepthBuffer = false;
		public bool Disabled = false;
		public SurfaceUpdateMethod UpdateMethod = null;
		public SurfaceUpdateMethod UpdatePositionsOnlyMethod = null;
		public SurfaceUpdateMethod UpdateOtherDataOnlyMethod = null;
		protected Surface(){}
		public static Surface Create(GLWindow window_,string texture_filename,params int[] vertex_attrib_counts){
			return Create(window_,texture_filename,Shader.DefaultFS(),false,vertex_attrib_counts);
		}
		public static Surface Create(GLWindow window_,string texture_filename,string frag_shader,bool has_depth,params int[] vertex_attrib_counts){
			Surface s = new Surface();
			s.window = window_;
			int dims = has_depth? 3 : 2;
			s.UseDepthBuffer = has_depth;
			VertexAttributes attribs = VertexAttributes.Create(vertex_attrib_counts);
			s.vbo = VBO.Create(dims,attribs);
			s.texture = Texture.Create(texture_filename);
			s.shader = Shader.Create(frag_shader);
			if(window_ != null){
				window_.Surfaces.Add(s);
			}
			return s;
		}
		public void RemoveFromWindow(){
			if(window != null){
				window.Surfaces.Remove(this);
			}
		}
		public void SetOffsetInPixels(int x_offset_px,int y_offset_px){
			this.x_offset_px = x_offset_px;
			this.y_offset_px = y_offset_px;
			raw_x_offset = (float)(x_offset_px * 2) / (float)window.Viewport.Width;
			raw_y_offset = (float)(y_offset_px * 2) / (float)window.Viewport.Height; 
		}
		public void ChangeOffsetInPixels(int dx_offset_px,int dy_offset_px){
			x_offset_px += dx_offset_px;
			y_offset_px += dy_offset_px;
			raw_x_offset += (float)(dx_offset_px * 2) / (float)window.Viewport.Width;
			raw_y_offset += (float)(dy_offset_px * 2) / (float)window.Viewport.Height; 
		}
		//public void XOffsetPx(){ return x_offset_px; }
		//public void YOffsetPx(){ return y_offset_px; }
		public int TotalXOffsetPx(CellLayout layout){
			return x_offset_px + layout.HorizontalOffsetPx;
		}
		public int TotalXOffsetPx(){
			return x_offset_px + layouts[0].HorizontalOffsetPx;
		}
		public int TotalYOffsetPx(CellLayout layout){
			return y_offset_px + layout.VerticalOffsetPx;
		}
		public int TotalYOffsetPx(){
			return y_offset_px + layouts[0].VerticalOffsetPx;
		}
		public void SetEasyLayoutCounts(params int[] counts_per_layout){
			if(counts_per_layout.GetLength(0) != layouts.Count){
				throw new ArgumentException("SetEasyLayoutCounts: Number of arguments (" + counts_per_layout.GetLength(0) + ") must match number of layouts (" + layouts.Count + ").");
			}
			defaults.positions = new List<int>(); //this method creates the default lists used by Update()
			defaults.layouts = new List<int>();
			int idx = 0;
			foreach(int count in counts_per_layout){
				for(int i=0;i<count;++i){
					defaults.positions.Add(i);
					defaults.layouts.Add(idx);
				}
				++idx;
			}
			defaults.fill_count = defaults.positions.Count;
		}
		public void SetDefaultPosition(int position_index){
			defaults.single_position = position_index;
		}
		public void SetDefaultLayout(int layout_index){
			defaults.single_layout = layout_index;
		}
		public void SetDefaultSprite(int sprite_index){
			defaults.single_sprite = sprite_index;
		}
		public void SetDefaultSpriteType(int sprite_type_index){
			defaults.single_sprite_type = sprite_type_index;
		}
		public void SetDefaultOtherData(params List<float>[] other_data){
			defaults.single_other_data = other_data;
		}
		/*public void SetDefaults(IList<int> positions,IList<int> layouts,IList<int> sprites,IList<int> sprite_types,params IList<float>[] other_data){
			defaults = new SurfaceDefaults();
			if(positions != null){
				defaults.positions = new List<int>(positions);
			}
			if(layouts != null){
				defaults.layouts = new List<int>(layouts);
			}
			if(sprites != null){
				defaults.sprites = new List<int>(sprites);
			}
			if(sprite_types != null){
				defaults.sprite_types = new List<int>(sprite_types);
			}
			if(other_data != null){
				defaults.other_data = new List<float>[other_data.GetLength(0)];
				int idx = 0;
				foreach(List<float> l in other_data){
					other_data[idx] = new List<float>(l);
					++idx;
				}
			}
		}*/
		public void SetDefaults(SurfaceDefaults new_defaults){
			defaults = new_defaults;
		}
		public void DefaultUpdate(){
			SurfaceDefaults d = new SurfaceDefaults(defaults);
			d.FillValues(true,true);
			window.UpdatePositionVertexArray(this,d.positions,d.layouts);
			window.UpdateOtherVertexArray(this,-1,d.sprites,d.sprite_types,d.other_data);
		}
		public void DefaultUpdatePositions(){
			SurfaceDefaults d = new SurfaceDefaults(defaults);
			d.FillValues(true,false);
			window.UpdatePositionVertexArray(this,d.positions,d.layouts);
		}
		public void DefaultUpdateOtherData(){
			SurfaceDefaults d = new SurfaceDefaults(defaults);
			d.FillValues(false,true);
			window.UpdateOtherVertexArray(this,-1,d.sprites,d.sprite_types,d.other_data);
		}
		public void Update(){
			if(UpdateMethod != null){
				SurfaceDefaults d = new SurfaceDefaults(defaults);
				UpdateMethod(d);
				d.FillValues(true,true);
				window.UpdatePositionVertexArray(this,d.positions,d.layouts);
				window.UpdateOtherVertexArray(this,-1,d.sprites,d.sprite_types,d.other_data);
			}
		}
		public void UpdatePositionsOnly(){
			if(UpdatePositionsOnlyMethod != null){
				SurfaceDefaults d = new SurfaceDefaults(defaults);
				UpdatePositionsOnlyMethod(d);
				d.FillValues(true,false);
				window.UpdatePositionVertexArray(this,d.positions,d.layouts);
			}
		}
		public void UpdateOtherDataOnly(){
			if(UpdateOtherDataOnlyMethod != null){
				SurfaceDefaults d = new SurfaceDefaults(defaults);
				UpdateOtherDataOnlyMethod(d);
				d.FillValues(false,true);
				window.UpdateOtherVertexArray(this,-1,d.sprites,d.sprite_types,d.other_data);
			}
		}
	}
	public class VBO{
		public int PositionArrayBufferID;
		public int OtherArrayBufferID;
		public int ElementArrayBufferID;
		public VertexAttributes VertexAttribs;
		public int PositionDimensions = 2; //this value controls whether 2 or 3 values are stored for position.
		public int NumElements = 0;
		public int PositionDataSize = 0; //these 2 values track the number of float values in the VBOs.
		public int OtherDataSize = 0;
		protected VBO(){}
		public static VBO Create(){
			VBO v = new VBO();
			GL.GenBuffers(1,out v.PositionArrayBufferID);
			GL.GenBuffers(1,out v.OtherArrayBufferID);
			GL.GenBuffers(1,out v.ElementArrayBufferID);
			return v;
		}
		public static VBO Create(int position_dimensions,VertexAttributes attribs){
			VBO v = new VBO();
			GL.GenBuffers(1,out v.PositionArrayBufferID);
			GL.GenBuffers(1,out v.OtherArrayBufferID);
			GL.GenBuffers(1,out v.ElementArrayBufferID);
			v.PositionDimensions = position_dimensions;
			v.VertexAttribs = attribs;
			return v;
		}
	}
	public class VertexAttributes{
		public float[][] Defaults;
		public int[] Size;
		public int TotalSize;

		public static VertexAttributes Create(params float[][] defaults){
			VertexAttributes v = new VertexAttributes();
			int count = defaults.GetLength(0);
			v.Defaults = new float[count][];
			v.Size = new int[count];
			v.TotalSize = 0;
			int idx = 0;
			foreach(float[] f in defaults){
				v.Defaults[idx] = f;
				v.Size[idx] = f.GetLength(0);
				v.TotalSize += v.Size[idx];
				++idx;
			}
			return v;
		}
		public static VertexAttributes Create(params int[] counts){ //makes zeroed arrays in the given counts.
			VertexAttributes v = new VertexAttributes();
			int count = counts.GetLength(0);
			v.Defaults = new float[count][];
			v.Size = new int[count];
			v.TotalSize = 0;
			int idx = 0;
			foreach(int i in counts){
				v.Defaults[idx] = new float[i]; //todo: this method needs a note:  which attribs are assumed to be here already? if you Create(2), is that texcoords? and what?
				v.Size[idx] = i;
				v.TotalSize += i;
				++idx;
			}
			return v;
		}
	}
	public class Texture{
		public int TextureIndex;
		public int TextureHeightPx;
		public int TextureWidthPx;
		public int DefaultSpriteTypeIndex = 0;
		public List<SpriteType> Sprite = null;

		protected static int next_texture = 0;
		protected static int max_textures = -1; //Currently, max_textures serves only to crash in a better way. Eventually I'll figure out how to swap texture units around, todo!
		protected static Dictionary<string,Texture> texture_info = new Dictionary<string,Texture>(); //the Textures contained herein are used only to store index/height/width
		public static Texture Create(string filename,string textureToReplace = null){
			Texture t = new Texture();
			t.Sprite = new List<SpriteType>();
			if(textureToReplace != null){
				t.ReplaceTexture(filename,textureToReplace);
			}
			else{
				t.LoadTexture(filename);
			}
			return t;
		}
		protected Texture(){}
		protected void LoadTexture(string filename){
			if(String.IsNullOrEmpty(filename)){
				throw new ArgumentException(filename);
			}
			if(texture_info.ContainsKey(filename)){
				Texture t = texture_info[filename];
				TextureIndex = t.TextureIndex;
				TextureHeightPx = t.TextureHeightPx;
				TextureWidthPx = t.TextureWidthPx;
			}
			else{
				if(max_textures == -1){
					GL.GetInteger(GetPName.MaxTextureImageUnits,out max_textures);
				}
				int num = next_texture++;
				if(num == max_textures){ //todo: eventually fix this
					throw new NotSupportedException("This machine only supports " + num + " texture units, and this GL code isn't smart enough to switch them out yet, sorry.");
				}
				GL.ActiveTexture(TextureUnit.Texture0 + num);
				int id = GL.GenTexture(); //todo: eventually i'll want to support more than 16 or 32 textures. At that time I'll need to store this ID somewhere.
				GL.BindTexture(TextureTarget.Texture2D,id); //maybe a list of Scenes which are lists of textures needed, and then i'll bind all those and make sure to track their texture units.
				Bitmap bmp = new Bitmap(filename);
				BitmapData bmp_data = bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba,bmp_data.Width,bmp_data.Height,0,OpenTK.Graphics.OpenGL.PixelFormat.Bgra,PixelType.UnsignedByte,bmp_data.Scan0);
				bmp.UnlockBits(bmp_data);
				GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
				TextureIndex = num;
				TextureHeightPx = bmp.Height;
				TextureWidthPx = bmp.Width;
				Texture t = new Texture(); //this one goes into the dictionary as an easy way to store the index/height/width of this filename.
				t.TextureIndex = num;
				t.TextureHeightPx = bmp.Height;
				t.TextureWidthPx = bmp.Width;
				texture_info.Add(filename,t);
			}
		}
		protected void ReplaceTexture(string filename,string replaced){
			if(String.IsNullOrEmpty(filename)){
				throw new ArgumentException(filename);
			}
			if(texture_info.ContainsKey(filename)){
				Texture t = texture_info[filename];
				TextureIndex = t.TextureIndex;
				TextureHeightPx = t.TextureHeightPx;
				TextureWidthPx = t.TextureWidthPx;
			}
			else{
				int num;
				if(texture_info.ContainsKey(replaced)){
					num = texture_info[replaced].TextureIndex;
					texture_info.Remove(replaced);
				}
				else{
					if(max_textures == -1){
						GL.GetInteger(GetPName.MaxTextureImageUnits,out max_textures);
					}
					num = next_texture++;
					if(num == max_textures){ //todo: eventually fix this
						throw new NotSupportedException("This machine only supports " + num + " texture units, and this GL code isn't smart enough to switch them out yet, sorry.");
					}
				}
				GL.ActiveTexture(TextureUnit.Texture0 + num);
				int id = GL.GenTexture(); //todo: eventually i'll want to support more than 16 or 32 textures. At that time I'll need to store this ID somewhere.
				GL.BindTexture(TextureTarget.Texture2D,id); //maybe a list of Scenes which are lists of textures needed, and then i'll bind all those and make sure to track their texture units.
				Bitmap bmp = new Bitmap(filename);
				BitmapData bmp_data = bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba,bmp_data.Width,bmp_data.Height,0,OpenTK.Graphics.OpenGL.PixelFormat.Bgra,PixelType.UnsignedByte,bmp_data.Scan0);
				bmp.UnlockBits(bmp_data);
				GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
				TextureIndex = num;
				TextureHeightPx = bmp.Height;
				TextureWidthPx = bmp.Width;
				Texture t = new Texture(); //this one goes into the dictionary as an easy way to store the index/height/width of this filename.
				t.TextureIndex = num;
				t.TextureHeightPx = bmp.Height;
				t.TextureWidthPx = bmp.Width;
				texture_info.Add(filename,t);
			}
		}
	}
	public class SpriteType{ //each different arrangement of sprites on a sheet gets its own SpriteType. Many, like fonts, will use only a single SpriteType for the whole sheet.
		public PositionFromIndex X; //SpriteType is pretty similar to CellLayout. Any chance they could ever be combined?
		public PositionFromIndex Y;
		public float SpriteHeight; //0 to 1, not pixels
		public float SpriteWidth;
		public int DefaultSpriteIndex;
		public static SpriteType DefineSingleRowSprite(Surface surface,int sprite_width_px){
			SpriteType s = new SpriteType();
			float texcoord_width = (float)sprite_width_px * 1.0f / (float)surface.texture.TextureWidthPx;
			s.X = idx => idx * texcoord_width;
			s.Y = idx => 0;
			s.SpriteWidth = texcoord_width;
			s.SpriteHeight = 1.0f;
			if(surface != null){
				surface.texture.Sprite.Add(s);
			}
			return s;
		}
		public static SpriteType DefineSingleRowSprite(Surface surface,int sprite_width_px,int padding_between_sprites_px){
			SpriteType s = new SpriteType();
			float px_width = 1.0f / (float)surface.texture.TextureWidthPx;
			float texcoord_width = (float)sprite_width_px * px_width;
			float texcoord_start = texcoord_width + (float)padding_between_sprites_px * px_width;
			s.X = idx => idx * texcoord_start;
			s.Y = idx => 0;
			s.SpriteWidth = texcoord_width;
			s.SpriteHeight = 1.0f;
			if(surface != null){
				surface.texture.Sprite.Add(s);
			}
			return s;
		}
		public static SpriteType DefineSpriteAcross(Surface surface,int sprite_width_px,int sprite_height_px,int num_columns){
			SpriteType s = new SpriteType();
			float texcoord_width = (float)sprite_width_px * 1.0f / (float)surface.texture.TextureWidthPx;
			float texcoord_height = (float)sprite_height_px * 1.0f / (float)surface.texture.TextureHeightPx;
			s.X = idx => (idx % num_columns) * texcoord_width;
			s.Y = idx => (idx / num_columns) * texcoord_height;
			s.SpriteWidth = texcoord_width;
			s.SpriteHeight = texcoord_height;
			if(surface != null){
				surface.texture.Sprite.Add(s);
			}
			return s;
		}
		public static SpriteType DefineSpriteAcross(Surface surface,int sprite_width_px,int sprite_height_px,int num_columns,int h_offset_px,int v_offset_px){
			SpriteType s = new SpriteType();
			float texcoord_width = (float)sprite_width_px * 1.0f / (float)surface.texture.TextureWidthPx;
			float texcoord_height = (float)sprite_height_px * 1.0f / (float)surface.texture.TextureHeightPx;
			s.X = idx => ((idx % num_columns) * sprite_width_px + h_offset_px) * 1.0f / (float)surface.texture.TextureWidthPx;
			s.Y = idx => ((idx / num_columns) * sprite_height_px + v_offset_px) * 1.0f / (float)surface.texture.TextureHeightPx;
			s.SpriteWidth = texcoord_width;
			s.SpriteHeight = texcoord_height;
			if(surface != null){
				surface.texture.Sprite.Add(s);
			}
			return s;
		}
		public static SpriteType DefineSpriteDown(Surface surface,int sprite_width_px,int sprite_height_px,int num_rows){
			SpriteType s = new SpriteType();
			float texcoord_width = (float)sprite_width_px * 1.0f / (float)surface.texture.TextureWidthPx;
			float texcoord_height = (float)sprite_height_px * 1.0f / (float)surface.texture.TextureHeightPx;
			s.X = idx => (idx / num_rows) * texcoord_width;
			s.Y = idx => (idx % num_rows) * texcoord_height;
			s.SpriteWidth = texcoord_width;
			s.SpriteHeight = texcoord_height;
			if(surface != null){
				surface.texture.Sprite.Add(s);
			}
			return s;
		}
		public static SpriteType DefineSpriteDown(Surface surface,int sprite_width_px,int sprite_height_px,int num_rows,int h_offset_px,int v_offset_px){
			SpriteType s = new SpriteType();
			float texcoord_width = (float)sprite_width_px * 1.0f / (float)surface.texture.TextureWidthPx;
			float texcoord_height = (float)sprite_height_px * 1.0f / (float)surface.texture.TextureHeightPx;
			s.X = idx => ((idx / num_rows) * sprite_width_px + h_offset_px) * 1.0f / (float)surface.texture.TextureWidthPx;
			s.Y = idx => ((idx % num_rows) * sprite_height_px + v_offset_px) * 1.0f / (float)surface.texture.TextureHeightPx;
			s.SpriteWidth = texcoord_width;
			s.SpriteHeight = texcoord_height;
			if(surface != null){
				surface.texture.Sprite.Add(s);
			}
			return s;
		}
	}
	public class CellLayout{
		public PositionFromIndex X;
		public PositionFromIndex Y;
		public PositionFromIndex Z = null; //Z isn't used unless the VBO object has PositionDimensions set to 3.
		public int CellHeightPx; //in pixels
		public int CellWidthPx;
		public int VerticalOffsetPx;
		public int HorizontalOffsetPx;

		public static CellLayout CreateGrid(Surface s,int rows,int cols,int cell_height_px,int cell_width_px,int v_offset_px,int h_offset_px,PositionFromIndex z = null){
			CellLayout c = new CellLayout();
			c.CellHeightPx = cell_height_px;
			c.CellWidthPx = cell_width_px;
			c.VerticalOffsetPx = v_offset_px;
			c.HorizontalOffsetPx = h_offset_px;
			c.X = idx => (idx % cols) * c.CellWidthPx;
			c.Y = idx => (idx / cols) * c.CellHeightPx;
			c.Z = z;
			if(s != null){
				s.layouts.Add(c);
			}
			return c;
		}
		public static CellLayout CreateIso(Surface s,int rows,int cols,int cell_height_px,int cell_width_px,int v_offset_px,int h_offset_px,int cell_v_offset_px,int cell_h_offset_px,PositionFromIndex z = null,PositionFromIndex elevation = null){
			CellLayout c = new CellLayout();
			c.CellHeightPx = cell_height_px;
			c.CellWidthPx = cell_width_px;
			c.VerticalOffsetPx = v_offset_px;
			c.HorizontalOffsetPx = h_offset_px;
			c.X = idx => (rows - 1 - (idx/cols) + (idx%cols)) * cell_h_offset_px;
			if(elevation == null){
				c.Y = idx => ((idx/cols) + (idx%cols)) * cell_v_offset_px;
			}
			else{
				c.Y = idx => ((idx/cols) + (idx%cols)) * cell_v_offset_px + elevation(idx);
			}
			c.Z = z;
			if(s != null){
				s.layouts.Add(c);
			}
			return c;
		}
		public static CellLayout CreateIsoAtOffset(Surface s,int rows,int cols,int base_start_row,int base_start_col,int base_rows,int cell_height_px,int cell_width_px,int v_offset_px,int h_offset_px,int cell_v_offset_px,int cell_h_offset_px,PositionFromIndex z = null,PositionFromIndex elevation = null){
			CellLayout c = new CellLayout();
			c.CellHeightPx = cell_height_px;
			c.CellWidthPx = cell_width_px;
			c.VerticalOffsetPx = v_offset_px;
			c.HorizontalOffsetPx = h_offset_px;
			c.X = idx => (base_rows - 1 - (idx/cols + base_start_row) + (idx%cols + base_start_col)) * cell_h_offset_px;
			if(elevation == null){
				c.Y = idx => ((idx/cols + base_start_row) + (idx%cols + base_start_col)) * cell_v_offset_px;
			}
			else{
				c.Y = idx => ((idx/cols + base_start_row) + (idx%cols + base_start_col)) * cell_v_offset_px + elevation(idx);
			}
			c.Z = z;
			if(s != null){
				s.layouts.Add(c);
			}
			return c;
		}
		public static CellLayout Create(Surface s,int cell_height_px,int cell_width_px,int v_offset_px,int h_offset_px,PositionFromIndex x,PositionFromIndex y,PositionFromIndex z = null){
			CellLayout c = new CellLayout(); //todo: fix x/y order for entire file?
			c.CellHeightPx = cell_height_px;
			c.CellWidthPx = cell_width_px;
			c.VerticalOffsetPx = v_offset_px;
			c.HorizontalOffsetPx = h_offset_px;
			c.X = x;
			c.Y = y;
			c.Z = z;
			if(s != null){
				s.layouts.Add(c);
			}
			return c;
		}
	}
	public class Shader{
		public int ShaderProgramID;
		public int OffsetUniformLocation;
		public int TextureUniformLocation;

		protected class id_and_programs{
			public int id;
			public Dictionary<int,Shader> programs = null;
		}
		protected static Dictionary<string,id_and_programs> compiled_vs = new Dictionary<string,id_and_programs>();
		protected static Dictionary<string,int> compiled_fs = new Dictionary<string,int>();

		public static Shader Create(string frag_shader){
			return Create(DefaultVS(),frag_shader);
		}
		public static Shader Create(string vert_shader,string frag_shader){
			Shader s = new Shader();
			int vertex_shader = -1;
			if(compiled_vs.ContainsKey(vert_shader)){
				vertex_shader = compiled_vs[vert_shader].id;
			}
			else{
				vertex_shader = GL.CreateShader(ShaderType.VertexShader);
				GL.ShaderSource(vertex_shader,vert_shader);
				GL.CompileShader(vertex_shader);
				int compiled;
				GL.GetShader(vertex_shader,ShaderParameter.CompileStatus,out compiled);
				if(compiled < 1){
					Console.Error.WriteLine(GL.GetShaderInfoLog(vertex_shader));
					throw new Exception("vertex shader compilation failed");
				}
				id_and_programs v = new id_and_programs();
				v.id = vertex_shader;
				compiled_vs.Add(vert_shader,v);
			}
			int fragment_shader = -1;
			if(compiled_fs.ContainsKey(frag_shader)){
				fragment_shader = compiled_fs[frag_shader];
			}
			else{
				fragment_shader = GL.CreateShader(ShaderType.FragmentShader);
				GL.ShaderSource(fragment_shader,frag_shader);
				GL.CompileShader(fragment_shader);
				int compiled;
				GL.GetShader(fragment_shader,ShaderParameter.CompileStatus,out compiled);
				if(compiled < 1){ 
					Console.Error.WriteLine(GL.GetShaderInfoLog(fragment_shader));
					throw new Exception("fragment shader compilation failed");
				}
				compiled_fs.Add(frag_shader,fragment_shader);
			}
			if(compiled_vs[vert_shader].programs != null && compiled_vs[vert_shader].programs.ContainsKey(fragment_shader)){
				s.ShaderProgramID = compiled_vs[vert_shader].programs[fragment_shader].ShaderProgramID;
				s.OffsetUniformLocation = compiled_vs[vert_shader].programs[fragment_shader].OffsetUniformLocation;
				s.TextureUniformLocation = compiled_vs[vert_shader].programs[fragment_shader].TextureUniformLocation;
			}
			else{
				int shader_program = GL.CreateProgram();
				GL.AttachShader(shader_program,vertex_shader);
				GL.AttachShader(shader_program,fragment_shader);
				int attrib_index = 0;
				foreach(string attr in new string[]{"position","texcoord","color","bgcolor"}){
					GL.BindAttribLocation(shader_program,attrib_index++,attr);
				}
				GL.LinkProgram(shader_program);
				s.ShaderProgramID = shader_program;
				s.OffsetUniformLocation = GL.GetUniformLocation(shader_program,"offset");
				s.TextureUniformLocation = GL.GetUniformLocation(shader_program,"texture");
				if(compiled_vs[vert_shader].programs == null){
					compiled_vs[vert_shader].programs = new Dictionary<int,Shader>();
				}
				Shader p = new Shader();
				p.ShaderProgramID = shader_program;
				p.OffsetUniformLocation = s.OffsetUniformLocation;
				p.TextureUniformLocation = s.TextureUniformLocation;
				compiled_vs[vert_shader].programs.Add(fragment_shader,p);
			}
			return s;
		}
		public static string DefaultVS(){
			return 
@"#version 120
uniform vec2 offset;

attribute vec4 position;
attribute vec2 texcoord;
attribute vec4 color;
attribute vec4 bgcolor;

varying vec2 texcoord_fs;
varying vec4 color_fs;
varying vec4 bgcolor_fs;

void main(){
 texcoord_fs = texcoord;
 color_fs = color;
 bgcolor_fs = bgcolor;
 gl_Position = vec4(position.x + offset.x,-position.y - offset.y,position.z,1);
}
";
		}
		public static string DefaultFS(){ //todo: I could make a builder for these, kinda. It could make things like alpha testing optional.
			return 
@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;

void main(){
 vec4 v = texture2D(texture,texcoord_fs);
 if(v.a < 0.1){
  discard;
 }
 //gl_FragColor = texture2D(texture,texcoord_fs);
 gl_FragColor = v;
}
";
		}
		public static string FontFS(){
			return 
@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;
varying vec4 color_fs;
varying vec4 bgcolor_fs;

void main(){
 vec4 v = texture2D(texture,texcoord_fs);
 if(v.r == 1.0 && v.g == 1.0 && v.b == 1.0){
  gl_FragColor = color_fs;
 }
 else{
  gl_FragColor = bgcolor_fs;
 }
}
";
		}
		public static string AAFontFS(){
			return 
				@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;
varying vec4 color_fs;
varying vec4 bgcolor_fs;

void main(){
 vec4 v = texture2D(texture,texcoord_fs);
 gl_FragColor = vec4(bgcolor_fs.r * (1.0 - v.a) + color_fs.r * v.a,bgcolor_fs.g * (1.0 - v.a) + color_fs.g * v.a,bgcolor_fs.b * (1.0 - v.a) + color_fs.b * v.a,bgcolor_fs.a * (1.0 - v.a) + color_fs.a * v.a);
}
";
		}
		public static string TintFS(){
			return 
@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;
varying vec4 color_fs;

void main(){
 vec4 v = texture2D(texture,texcoord_fs);
 if(v.a < 0.1){
  discard;
 }
 gl_FragColor = vec4(v.r * color_fs.r,v.g * color_fs.g,v.b * color_fs.b,v.a * color_fs.a);
}
";
		}
		public static string NewTintFS(){
			return 
				@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;
varying vec4 color_fs;
varying vec4 bgcolor_fs;

void main(){
 vec4 v = texture2D(texture,texcoord_fs);
 if(v.a < 0.1){
  discard;
 }
 gl_FragColor = vec4(v.r * color_fs.r + bgcolor_fs.r,v.g * color_fs.g + bgcolor_fs.g,v.b * color_fs.b + bgcolor_fs.b,v.a * color_fs.a + bgcolor_fs.a);
}
";
		}
		public static string GrayscaleFS(){
			return 
@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;

void main(){
 vec4 v = texture2D(texture,texcoord_fs);
 if(v.a < 0.1){
  discard;
 }
 float f = 0.1 * v.r + 0.2 * v.b + 0.7 * v.g;
 gl_FragColor = vec4(f,f,f,v.a);
}
";
		}
		public static string GrayscaleWithColorsFS(){
			return 
@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;

void main(){
 vec4 v = texture2D(texture,texcoord_fs);
 if(v.a < 0.1){
  discard;
 }
 float f = 0.1 * v.r + 0.7 * v.g + 0.2 * v.b;
 gl_FragColor = vec4(f,f,f,v.a);
 if((v.r > 0.6 && v.g < 0.4 && v.b < 0.4) || (v.g > 0.6 && v.r < 0.4 && v.b < 0.4) || (v.b > 0.6 && v.r < 0.4 && v.g < 0.4)){
  gl_FragColor = v;
 }
}
";
		}
	}
}
