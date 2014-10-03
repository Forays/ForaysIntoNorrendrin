/*Copyright (c) 2014  Derrick Creamer
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
	public enum ResizeOption{StretchToFit,AddBorder,ExactFitOnly};
	public class GLWindow : GameWindow{
		public List<SpriteSurface> SpriteSurfaces = new List<SpriteSurface>();
		public bool SingleSurfaceMode = false;
		
		public ResizeOption ResizingPreference = ResizeOption.StretchToFit;
		public ResizeOption ResizingFullScreenPreference = ResizeOption.StretchToFit;
		protected bool Resizing = false;
		public bool AllowScaling = false;
		public int SnapHeight = 1; //if AllowScaling is true, resizing will snap to any natural multiple of the rectangle described by these two values (in pixels).
		public int SnapWidth = 1;
		protected int current_scale = 1;
		
		public bool NoClose = false;
		public bool FullScreen = false;
		
		protected FrameEventArgs render_args = new FrameEventArgs();
		protected Dictionary<Key,bool> key_down = new Dictionary<Key,bool>();
		
		public float screen_multiplier_h = 1.0f; //this is used to create a border around the screen - especially useful if you're fullscreening & scaling sprites by integer multiples only.
		public float screen_multiplier_w = 1.0f;
		
		protected static int next_texture = 0; //used by LoadTexture to determine which texture unit is used next. Static so it might even work with multiple windows. (I don't know whether that even works.)
		
		public GLWindow(int h,int w,string title) : base(w,h,GraphicsMode.Default,title){
			VSync = VSyncMode.On;
			GL.ClearColor(0.0f,0.0f,0.0f,0.0f);
			KeyDown += KeyDownHandler;
			KeyUp += KeyUpHandler;
			Keyboard.KeyRepeat = true;
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
		protected void HandleResize(){
			ResizeOption pref = ResizingPreference;
			if(FullScreen){
				pref = ResizingFullScreenPreference;
			}
			if(AllowScaling && pref != ResizeOption.StretchToFit){
				int height_multiple = ClientRectangle.Height / SnapHeight;
				int width_multiple = ClientRectangle.Width / SnapWidth;
				int multiple = Math.Min(height_multiple,width_multiple);
				if(multiple < 1){
					multiple = 1;
				}
				current_scale = multiple;
			}
			float previous_mult_h = screen_multiplier_h;
			float previous_mult_w = screen_multiplier_w;
			switch(pref){
			case ResizeOption.StretchToFit:
				screen_multiplier_h = 1.0f;
				screen_multiplier_w = 1.0f;
				break;
			case ResizeOption.AddBorder:
				screen_multiplier_h = (float)(SnapHeight * current_scale) / (float)ClientRectangle.Height;
				screen_multiplier_w = (float)(SnapWidth * current_scale) / (float)ClientRectangle.Width;
				break;
			case ResizeOption.ExactFitOnly: //you probably don't want to use exact fit for fullscreen.
				screen_multiplier_h = 1.0f;
				screen_multiplier_w = 1.0f;
				Height = SnapHeight * current_scale;
				Width = SnapWidth * current_scale;
				break;
			}
			float ratio_h = screen_multiplier_h / previous_mult_h;
			float ratio_w = screen_multiplier_w / previous_mult_w;
			foreach(SpriteSurface s in SpriteSurfaces){
				if(s.NumElements > 0){
					GL.BindBuffer(BufferTarget.ArrayBuffer,s.ArrayBufferID);
					IntPtr vbo = GL.MapBuffer(BufferTarget.ArrayBuffer,BufferAccess.ReadWrite);
					int max = s.Rows * s.Cols * 4 * s.TotalVertexAttribSize;
					for(int i=0;i<max;i += s.TotalVertexAttribSize){
						int offset = i * 4; //4 bytes per float
						byte[] bytes = BitConverter.GetBytes(ratio_w * BitConverter.ToSingle(new byte[]{Marshal.ReadByte(vbo,offset),Marshal.ReadByte(vbo,offset+1),Marshal.ReadByte(vbo,offset+2),Marshal.ReadByte(vbo,offset+3)},0));
						for(int j=0;j<4;++j){
							Marshal.WriteByte(vbo,offset+j,bytes[j]);
						}
						offset += 4; //move to the next float
						bytes = BitConverter.GetBytes(ratio_h * BitConverter.ToSingle(new byte[]{Marshal.ReadByte(vbo,offset),Marshal.ReadByte(vbo,offset+1),Marshal.ReadByte(vbo,offset+2),Marshal.ReadByte(vbo,offset+3)},0));
						for(int j=0;j<4;++j){
							Marshal.WriteByte(vbo,offset+j,bytes[j]);
						}
					}
					GL.UnmapBuffer(BufferTarget.ArrayBuffer);
				}
			}
			GL.Viewport(ClientRectangle.X,ClientRectangle.Y,ClientRectangle.Width,ClientRectangle.Height);
			Resizing = false;
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
			GL.Viewport(ClientRectangle.X,ClientRectangle.Y,ClientRectangle.Width,ClientRectangle.Height);
		}
		public bool Update(){
			ProcessEvents();
			if(IsExiting){
				return false;
			}
			Render();
			return true;
		}
		public void Render(){
			if(Resizing){
				HandleResize();
			}
			base.OnRenderFrame(render_args);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			foreach(SpriteSurface s in SpriteSurfaces){
				if(!s.Disabled){
					if(SingleSurfaceMode){
						GL.DrawElements(PrimitiveType.Triangles,s.NumElements,DrawElementsType.UnsignedInt,IntPtr.Zero);
					}
					else{
						GL.UseProgram(s.ShaderProgramID);
						//GL.ActiveTexture(TextureUnit.Texture0 + s.TextureIndex);
						GL.Uniform1(s.UniformLocation,s.TextureIndex);
						GL.BindBuffer(BufferTarget.ArrayBuffer,s.ArrayBufferID);
						GL.BindBuffer(BufferTarget.ElementArrayBuffer,s.ElementArrayBufferID);
						int stride = sizeof(float)*s.TotalVertexAttribSize;
						int total_of_previous_attribs = 0;
						for(int i=0;i<s.VertexAttribSize.Length;++i){
							GL.EnableVertexAttribArray(i);
							GL.VertexAttribPointer(i,s.VertexAttribSize[i],VertexAttribPointerType.Float,false,stride,new IntPtr(sizeof(float)*total_of_previous_attribs));
							total_of_previous_attribs += s.VertexAttribSize[i];
						}
						GL.DrawElements(PrimitiveType.Triangles,s.NumElements,DrawElementsType.UnsignedInt,IntPtr.Zero);
						for(int i=0;i<s.VertexAttribSize.Length;++i){
							GL.DisableVertexAttribArray(i);
						}
					}
				}
			}
			SwapBuffers();
		}
		public static int LoadTexture(string filename){ //binds a texture to the next available texture unit and returns its number
			if(String.IsNullOrEmpty(filename)){
				throw new ArgumentException(filename);
			}
			int num = next_texture;
			next_texture++;
			GL.ActiveTexture(TextureUnit.Texture0 + num);
			int id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D,id);
			Bitmap bmp = new Bitmap(filename);
			BitmapData bmp_data = bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba,bmp_data.Width,bmp_data.Height,0,OpenTK.Graphics.OpenGL.PixelFormat.Bgra,PixelType.UnsignedByte,bmp_data.Scan0);
			bmp.UnlockBits(bmp_data);
			GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
			return num;
		}
		public static void ReplaceTexture(int texture_unit,string filename){ //binds a texture to the given texture unit, replacing the texture that's already there
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
		}
		public static void LoadShaders(SpriteSurface s,string vs,string fs,params string[] attributes){
			int vertex_shader = GL.CreateShader(ShaderType.VertexShader);
			int fragment_shader = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(vertex_shader,vs);
			GL.ShaderSource(fragment_shader,fs);
			GL.CompileShader(vertex_shader);
			GL.CompileShader(fragment_shader);
			int compiled;
			GL.GetShader(vertex_shader,ShaderParameter.CompileStatus,out compiled);
			if(compiled < 1){
				Console.Error.WriteLine(GL.GetShaderInfoLog(vertex_shader));
				throw new Exception("vertex shader compilation failed");
			}
			GL.GetShader(fragment_shader,ShaderParameter.CompileStatus,out compiled);
			if(compiled < 1){ 
				Console.Error.WriteLine(GL.GetShaderInfoLog(fragment_shader));
				throw new Exception("fragment shader compilation failed");
			}
			int shader_program = GL.CreateProgram();
			GL.AttachShader(shader_program,vertex_shader);
			GL.AttachShader(shader_program,fragment_shader);
			int attrib_index = 0;
			foreach(string attr in attributes){
				GL.BindAttribLocation(shader_program,attrib_index++,attr);
			}
			GL.LinkProgram(shader_program);
			s.ShaderProgramID = shader_program;
			GL.UseProgram(shader_program);
			s.UniformLocation = GL.GetUniformLocation(shader_program,"texture");
			GL.Uniform1(s.UniformLocation,s.TextureIndex);
		}
		public static int[] GetBasicFontVertexAttributeSizes(){
			return new int[]{2,2,4,4};
		}
		public static string[] GetBasicFontVertexAttributes(){
			return new string[]{"position","texcoord","color","bgcolor"};
		}
		public static float[][] GetBasicFontDefaultVertexAttributes(){ //i.e. default values for color and bgcolor
			return new float[][]{new float[]{1,1,1,1},new float[]{0,0,0,1}};
		}
		public static int[] GetBasicGraphicalVertexAttributeSizes(){
			return new int[]{2,2,4};
		}
		public static string[] GetBasicGraphicalVertexAttributes(){
			return new string[]{"position","texcoord","color"};
		}
		public static float[][] GetBasicGraphicalDefaultVertexAttributes(){ //i.e. default values for color
			return new float[][]{new float[]{1,1,1,1}};
		}
		public static string GetBasicVertexShader(){
			return 
@"#version 120
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
gl_Position = position;
}
";
		}
		public static string GetBasicFontFragmentShader(){
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
		public static string GetBasicGraphicalFragmentShader(){
			return 
@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;
varying vec4 color_fs;

void main(){
//gl_FragColor = texture2D(texture,texcoord_fs);
vec4 v = texture2D(texture,texcoord_fs);
gl_FragColor = vec4(v.r * color_fs.r,v.g * color_fs.g,v.b * color_fs.b,v.a);
//float f = 0.1 * v.r + 0.2 * v.b + 0.7 * v.g; //black & white shader
//gl_FragColor = vec4(f,f,f,v.a);
//if(v.r > 0.6 && v.g < 0.4 && v.b < 0.4){ //...with red
//gl_FragColor = vec4(v.r,f,f,v.a);
//}
}
";
		}
		public void CreateVertexArray(SpriteSurface s,int default_sprite_offset_row,int default_sprite_offset_col,params float[][] default_vertex_attributes){
			int count = s.Rows * s.Cols;
			float[] all_values = new float[count * 4 * s.TotalVertexAttribSize]; //4 vertices for each tile
			int[] indices = new int[s.NumElements];
			float tex_start_h = s.SpriteHeight * default_sprite_offset_row;
			float tex_start_w = s.SpriteWidth * default_sprite_offset_col;
			float tex_end_h = tex_start_h + s.SpriteHeightPadded;
			float tex_end_w = tex_start_w + s.SpriteWidthPadded;
			for(int i=0;i<s.Rows;++i){
				for(int j=0;j<s.Cols;++j){
					int flipped_row = (s.Rows-1) - i;
					float fi = screen_multiplier_h * (((float)flipped_row / s.HeightScale) + s.GLCoordHeightOffset);
					float fj = screen_multiplier_w * (((float)j / s.WidthScale) + s.GLCoordWidthOffset);
					float fi_plus1 = screen_multiplier_h * (((float)(flipped_row+1) / s.HeightScale) + s.GLCoordHeightOffset);
					float fj_plus1 = screen_multiplier_w * (((float)(j+1) / s.WidthScale) + s.GLCoordWidthOffset);
					float[] values = new float[4 * s.TotalVertexAttribSize];
					values[0] = fj;
					values[1] = fi;
					values[2] = tex_start_w;
					values[3] = tex_end_h;
					values[s.TotalVertexAttribSize] = fj;
					values[1 + s.TotalVertexAttribSize] = fi_plus1;
					values[2 + s.TotalVertexAttribSize] = tex_start_w;
					values[3 + s.TotalVertexAttribSize] = tex_start_h;
					values[s.TotalVertexAttribSize*2] = fj_plus1;
					values[1 + s.TotalVertexAttribSize*2] = fi_plus1;
					values[2 + s.TotalVertexAttribSize*2] = tex_end_w;
					values[3 + s.TotalVertexAttribSize*2] = tex_start_h;
					values[s.TotalVertexAttribSize*3] = fj_plus1;
					values[1 + s.TotalVertexAttribSize*3] = fi;
					values[2 + s.TotalVertexAttribSize*3] = tex_end_w;
					values[3 + s.TotalVertexAttribSize*3] = tex_end_h;
					for(int g=2;g<s.VertexAttribSize.Length;++g){ //starting at 2 because vertex position & texcoords are already done
						for(int k=0;k<s.VertexAttribSize[g];++k){
							float attrib = default_vertex_attributes[g-2][k];
							values[4+k] = attrib;
							values[4+k+s.TotalVertexAttribSize] = attrib;
							values[4+k+(s.TotalVertexAttribSize*2)] = attrib;
							values[4+k+(s.TotalVertexAttribSize*3)] = attrib;
						}
					}
					values.CopyTo(all_values,(j + i*s.Cols) * 4 * s.TotalVertexAttribSize);

					int idx4 = (j + i*s.Cols) * 4;
					int idx6 = (j + i*s.Cols) * 6;
					indices[idx6] = idx4;
					indices[idx6 + 1] = idx4 + 1;
					indices[idx6 + 2] = idx4 + 2;
					indices[idx6 + 3] = idx4;
					indices[idx6 + 4] = idx4 + 2;
					indices[idx6 + 5] = idx4 + 3;
				}
			}
			int vert_id;
			int elem_id;
			GL.GenBuffers(1,out vert_id);
			GL.GenBuffers(1,out elem_id);
			s.ArrayBufferID = vert_id;
			s.ElementArrayBufferID = elem_id;
			GL.BindBuffer(BufferTarget.ArrayBuffer,vert_id);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer,elem_id);
			GL.BufferData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)*all_values.Length),all_values,BufferUsageHint.StreamDraw);
			GL.BufferData(BufferTarget.ElementArrayBuffer,new IntPtr(sizeof(int)*indices.Length),indices,BufferUsageHint.StaticDraw);
			//int stride = sizeof(float)*s.TotalVertexAttribSize;
			//GL.EnableVertexAttribArray(0);
			//GL.EnableVertexAttribArray(1);
			//GL.VertexAttribPointer(0,2,VertexAttribPointerType.Float,false,stride,0);
			//GL.VertexAttribPointer(1,2,VertexAttribPointerType.Float,false,stride,new IntPtr(sizeof(float)*2));
			int total_of_previous_attribs = 4;
			for(int g=2;g<s.VertexAttribSize.Length;++g){
				//GL.EnableVertexAttribArray(g);
				//GL.VertexAttribPointer(g,s.VertexAttribSize[g],VertexAttribPointerType.Float,false,stride,new IntPtr(sizeof(float)*total_of_previous_attribs));
				total_of_previous_attribs += s.VertexAttribSize[g];
			}
		}
		public void UpdateVertexArray(int start_row,int start_col,SpriteSurface s,int[] sprite_offset_rows,int[] sprite_offset_cols,params float[][] vertex_attributes){
			GL.BindBuffer(BufferTarget.ArrayBuffer,s.ArrayBufferID);
			int count = sprite_offset_rows.Length;
			List<float> all_values = new List<float>(4 * s.TotalVertexAttribSize * count);
			int row = start_row;
			int col = start_col;
			for(int i=0;i<count;++i){
				float tex_start_h = s.SpriteHeight * sprite_offset_rows[i];
				float tex_start_w = s.SpriteWidth * sprite_offset_cols[i];
				float tex_end_h = tex_start_h + s.SpriteHeightPadded;
				float tex_end_w = tex_start_w + s.SpriteWidthPadded;
				int flipped_row = (s.Rows-1) - row;
				float fi = screen_multiplier_h * (((float)flipped_row / s.HeightScale) + s.GLCoordHeightOffset);
				float fj = screen_multiplier_w * (((float)col / s.WidthScale) + s.GLCoordWidthOffset);
				float fi_plus1 = screen_multiplier_h * (((float)(flipped_row+1) / s.HeightScale) + s.GLCoordHeightOffset);
				float fj_plus1 = screen_multiplier_w * (((float)(col+1) / s.WidthScale) + s.GLCoordWidthOffset);
				float[] values = new float[4 * s.TotalVertexAttribSize];
				values[0] = fj;
				values[1] = fi;
				values[2] = tex_start_w;
				values[3] = tex_end_h;
				values[s.TotalVertexAttribSize] = fj;
				values[1 + s.TotalVertexAttribSize] = fi_plus1;
				values[2 + s.TotalVertexAttribSize] = tex_start_w;
				values[3 + s.TotalVertexAttribSize] = tex_start_h;
				values[s.TotalVertexAttribSize*2] = fj_plus1;
				values[1 + s.TotalVertexAttribSize*2] = fi_plus1;
				values[2 + s.TotalVertexAttribSize*2] = tex_end_w;
				values[3 + s.TotalVertexAttribSize*2] = tex_start_h;
				values[s.TotalVertexAttribSize*3] = fj_plus1;
				values[1 + s.TotalVertexAttribSize*3] = fi;
				values[2 + s.TotalVertexAttribSize*3] = tex_end_w;
				values[3 + s.TotalVertexAttribSize*3] = tex_end_h;
				int array_idx = i*4;
				int total_of_previous_attribs = 4;
				for(int j=2;j<s.VertexAttribSize.Length;++j){ //starting at 2 because vertex position & texcoords are already done
					for(int k=0;k<s.VertexAttribSize[j];++k){
						float attrib = vertex_attributes[j-2][array_idx+k];
						values[total_of_previous_attribs+k] = attrib;
						values[total_of_previous_attribs+k+s.TotalVertexAttribSize] = attrib;
						values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*2)] = attrib;
						values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*3)] = attrib;
					}
					total_of_previous_attribs += s.VertexAttribSize[j];
				}
				all_values.AddRange(values);
				col++;
				if(col == s.Cols){
					row++;
					col = 0;
				}
			}
			int idx = (start_col + start_row*s.Cols) * 4 * s.TotalVertexAttribSize;
			GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)*idx),new IntPtr(sizeof(float)* 4 * s.TotalVertexAttribSize * count),all_values.ToArray());
		}
		public void UpdateVertexArray(int row,int col,SpriteSurface s,int sprite_offset_row,int sprite_offset_col,params float[][] vertex_attributes){
			GL.BindBuffer(BufferTarget.ArrayBuffer,s.ArrayBufferID);
			float tex_start_h = s.SpriteHeight * sprite_offset_row;
			float tex_start_w = s.SpriteWidth * sprite_offset_col;
			float tex_end_h = tex_start_h + s.SpriteHeightPadded;
			float tex_end_w = tex_start_w + s.SpriteWidthPadded;
			int flipped_row = (s.Rows-1) - row;
			float fi = screen_multiplier_h * (((float)flipped_row / s.HeightScale) + s.GLCoordHeightOffset);
			float fj = screen_multiplier_w * (((float)col / s.WidthScale) + s.GLCoordWidthOffset);
			float fi_plus1 = screen_multiplier_h * (((float)(flipped_row+1) / s.HeightScale) + s.GLCoordHeightOffset);
			float fj_plus1 = screen_multiplier_w * (((float)(col+1) / s.WidthScale) + s.GLCoordWidthOffset);
			float[] values = new float[4 * s.TotalVertexAttribSize];
			values[0] = fj;
			values[1] = fi;
			values[2] = tex_start_w;
			values[3] = tex_end_h;
			values[s.TotalVertexAttribSize] = fj;
			values[1 + s.TotalVertexAttribSize] = fi_plus1;
			values[2 + s.TotalVertexAttribSize] = tex_start_w;
			values[3 + s.TotalVertexAttribSize] = tex_start_h;
			values[s.TotalVertexAttribSize*2] = fj_plus1;
			values[1 + s.TotalVertexAttribSize*2] = fi_plus1;
			values[2 + s.TotalVertexAttribSize*2] = tex_end_w;
			values[3 + s.TotalVertexAttribSize*2] = tex_start_h;
			values[s.TotalVertexAttribSize*3] = fj_plus1;
			values[1 + s.TotalVertexAttribSize*3] = fi;
			values[2 + s.TotalVertexAttribSize*3] = tex_end_w;
			values[3 + s.TotalVertexAttribSize*3] = tex_end_h;
			int total_of_previous_attribs = 4;
			for(int j=2;j<s.VertexAttribSize.Length;++j){ //starting at 2 because vertex position & texcoords are already done
				for(int k=0;k<s.VertexAttribSize[j];++k){
					float attrib = vertex_attributes[j-2][k];
					values[total_of_previous_attribs+k] = attrib;
					values[total_of_previous_attribs+k+s.TotalVertexAttribSize] = attrib;
					values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*2)] = attrib;
					values[total_of_previous_attribs+k+(s.TotalVertexAttribSize*3)] = attrib;
				}
				total_of_previous_attribs += s.VertexAttribSize[j];
			}
			int idx = (col + row*s.Cols) * 4 * s.TotalVertexAttribSize;
			GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)*idx),new IntPtr(sizeof(float)* 4 * s.TotalVertexAttribSize),values);
		}
	}
	public class SpriteSurface{
		private static int next_id = 0;
		public int ID;
		public bool Disabled = false;
		public int Rows; //size in tiles
		public int Cols;
		public int TileHeight; //tile size in pixels
		public int TileWidth;
		public int PixelHeightOffset; //the offset in pixels, from the top-left of the window.
		public int PixelWidthOffset;
		public float GLCoordHeightOffset; //the offset in GL coordinates, -1 to 1.
		public float GLCoordWidthOffset;
		public float HeightScale; //the ratio of window space to surface space, stored in a form used for drawing.
		public float WidthScale;

		public int NumElements;
		public int ArrayBufferID;
		public int ElementArrayBufferID;
		
		public int TextureIndex;
		public float SpriteHeight; //based on relative position within the texture. used to calculate texcoords for sprites.
		public float SpriteWidth;
		public float SpriteHeightPadded; //used to calculate texcoords of sprites with padding.
		public float SpriteWidthPadded;

		public int ShaderProgramID;
		public int UniformLocation;
		public int[] VertexAttribSize;
		public int TotalVertexAttribSize = 0;
		public SpriteSurface(GLWindow window,int rows,int cols,int tile_h,int tile_w,int pixel_offset_h,int pixel_offset_w,string texture_filename,int sprite_rows,int sprite_cols,int row_of_default_sprite,int col_of_default_sprite,float normalized_sprite_height_after_padding,float normalized_sprite_width_after_padding,string vertex_shader_source,string fragment_shader_source,int[] vertex_attrib_sizes,float[][] default_vertex_attribs,params string[] vertex_attributes){
			ID = next_id++; //todo: I could change "normalized sprite height after padding" to "sprite padding height"...except that I don't pass sprite pixel size in currently.
			Rows = rows;
			Cols = cols;
			TileHeight = tile_h;
			TileWidth = tile_w;
			PixelHeightOffset = pixel_offset_h;
			PixelWidthOffset = pixel_offset_w;
			GLCoordHeightOffset = ((float)((window.ClientRectangle.Height - PixelHeightOffset) - Rows*TileHeight) / (float)window.ClientRectangle.Height) * 2.0f - 1.0f;
			GLCoordWidthOffset = ((float)PixelWidthOffset / (float)window.ClientRectangle.Width) * 2.0f - 1.0f; //todo: not sure if this should consider screen_multiplier border or not. at least I know it works if you create all the surfaces before having a border.
			float surface_to_window_h = (float)(Rows*TileHeight) / (float)window.ClientRectangle.Height * 2.0f;
			float surface_to_window_w = (float)(Cols*TileWidth) / (float)window.ClientRectangle.Width * 2.0f; //todo: also not sure if this should consider it, either.
			HeightScale = (float)Rows / surface_to_window_h;
			WidthScale = (float)Cols / surface_to_window_w;
			NumElements = Rows * Cols * 6;
			SpriteHeight = 1.0f / (float)sprite_rows;
			SpriteWidth = 1.0f / (float)sprite_cols;
			SpriteHeightPadded = SpriteHeight * normalized_sprite_height_after_padding;
			SpriteWidthPadded = SpriteWidth * normalized_sprite_width_after_padding;
			VertexAttribSize = new int[vertex_attrib_sizes.Length];
			for(int i=0;i<vertex_attrib_sizes.Length;++i){
				VertexAttribSize[i] = vertex_attrib_sizes[i];
				TotalVertexAttribSize += vertex_attrib_sizes[i];
			}
			TextureIndex = GLWindow.LoadTexture(texture_filename);
			GLWindow.LoadShaders(this,vertex_shader_source,fragment_shader_source,vertex_attributes);
			window.CreateVertexArray(this,row_of_default_sprite,col_of_default_sprite,default_vertex_attribs);
		}
	}
}
