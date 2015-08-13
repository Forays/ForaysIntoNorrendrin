/*Copyright (c) 2013-2015  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections;
namespace PosArrays{
	public struct pos{ //a generic position struct
		public int row;
		public int col;
		public pos(int r,int c){
			row = r;
			col = c;
		}
	}
	public class PosArray<T>{ //a 2D array with a position indexer and 1D indexer in addition to the usual 2D indexer
		public T[,] objs;
		public T this[int row,int col]{
			get{
				return objs[row,col];
			}
			set{
				objs[row,col] = value;
			}
		}
		public T this[pos p]{
			get{
				return objs[p.row,p.col];
			}
			set{
				objs[p.row,p.col] = value;
			}
		}
		public T this[int idx]{
			get{
				return objs[idx / objs.GetLength(1),idx % objs.GetLength(1)];
			}
			set{
				objs[idx / objs.GetLength(1),idx % objs.GetLength(1)] = value;
			}
		}
		public IEnumerator GetEnumerator(){
			return objs.GetEnumerator();
		}
		public PosArray(int rows,int cols){
			objs = new T[rows,cols];
		}
	}
}
