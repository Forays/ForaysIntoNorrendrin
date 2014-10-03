/*Copyright (c) 2013-2014  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
//This file contains utility classes and extension methods. Some are meant to be used with 2D grids, while others are more general. (v5+)
namespace Utilities{
	public struct pos{ //a generic position object
		public int row;
		public int col;
		public pos(int r,int c){
			row = r;
			col = c;
		}
	}
	public class PosArray<T>{ //a 2D array with a position indexer in addition to the usual 2-int indexer
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
		public PosArray(int rows,int cols){
			objs = new T[rows,cols];
		}
	}
	public class Dict<TKey,TValue>{ //a Dictionary that returns the default value for keys that haven't been added
		public Dictionary<TKey,TValue> d;
		public TValue this[TKey key]{
			get{
				return d.ContainsKey(key)? d[key] : default(TValue);
			}
			set{
				d[key] = value;
			}
		}
		public Dict(){ d = new Dictionary<TKey,TValue>(); }
		public Dict(Dict<TKey,TValue> d2){ d = new Dictionary<TKey, TValue>(d2.d); }
	}
	public struct cell{ //a position that holds an integer value, useful with priority queues
		public pos p;
		public int row{
			get{
				return p.row;
			}
			set{
				p.row = value;
			}
		}
		public int col{
			get{
				return p.col;
			}
			set{
				p.col = value;
			}
		}
		public int value;
		public cell(int row_,int col_,int value_){
			p = new pos(row_,col_);
			value = value_;
		}
	}
	public delegate int SortValue<T>(T t);
	public class PriorityQueue<T>{
		public LinkedList<T> list;
		public SortValue<T> priority;
		public PriorityQueue(SortValue<T> sort_value){
			list = new LinkedList<T>();
			priority = sort_value;
		}
		public void Add(T t){
			if(list.First == null){
				list.AddFirst(t);
			}
			else{
				LinkedListNode<T> current = list.First;
				while(true){
					if(priority(t) < priority(current.Value)){
						current = current.Next;
						if(current == null){
							list.AddLast(t);
							break;
						}
					}
					else{
						list.AddBefore(current,t);
						break;
					}
				}
			}
		}
		public T Pop(){
			T result = list.First.Value;
			list.Remove(result);
			return result;
		}
	}
	public static class U{ //for Utility, of course
		public static int HorizontalMinimum = 1; //When using a method like PositionsWithinDistance(x), these bounds will be considered if
		public static int HorizontalMaximum = -1;//  min <= max (this isn't the case by default).
		public static int VerticalMinimum = 1;   //  If you only care about one dimension, you can set only
		public static int VerticalMaximum = -1;  //  that one, and the other dimension will be ignored.
		public static void SetBounds(int h_min,int h_max,int v_min,int v_max){
			HorizontalMinimum = h_min;
			HorizontalMaximum = h_max;
			VerticalMinimum = v_min;
			VerticalMaximum = v_max;
		}
		public static void SetBoundsStartingAtZero(int height,int width){
			HorizontalMinimum = 0;
			HorizontalMaximum = height - 1;
			VerticalMinimum = 0;
			VerticalMaximum = width - 1;
		}
		public static bool BoundsCheck(this pos p){
			if(HorizontalMinimum <= HorizontalMaximum){
				if(p.row < HorizontalMinimum || p.row > HorizontalMaximum){
					return false;
				}
			}
			if(VerticalMinimum <= VerticalMaximum){
				if(p.col < VerticalMinimum || p.col > VerticalMaximum){
					return false;
				}
			}
			return true;
		}
		public static bool BoundsCheck(int r,int c){
			if(HorizontalMinimum <= HorizontalMaximum){
				if(r < HorizontalMinimum || r > HorizontalMaximum){
					return false;
				}
			}
			if(VerticalMinimum <= VerticalMaximum){
				if(c < VerticalMinimum || c > VerticalMaximum){
					return false;
				}
			}
			return true;
		}
		public static bool BoundsCheck<T>(this pos p,PosArray<T> array){ return p.BoundsCheck(array,true); }
		public static bool BoundsCheck<T>(this pos p,PosArray<T> array,bool allow_map_edges){
			int h = array.objs.GetLength(0);
			int w = array.objs.GetLength(1);
			if(p.row < 0 || p.row > h-1 || p.col < 0 || p.col > w-1){
				return false;
			}
			if(!allow_map_edges){
				if(p.row == 0 || p.row == h-1 || p.col == 0 || p.col == w-1){
					return false;
				}
			}
			return true;
		}
		public static int[] EightDirections = {8,9,6,3,2,1,4,7}; //these all start at 8 (up) and go clockwise.
		public static int[] FourDirections = {8,6,2,4}; //the directions correspond to the numbers on a keyboard's numpad.
		public static int[] DiagonalDirections = {9,3,1,7};
		public static int[] NineDirections = {5,8,9,6,3,2,1,4,7}; //eight, plus the direction that corresponds to "here".
		public static int RotateDir(this int dir,bool clockwise){ return dir.RotateDir(clockwise,1); }
		public static int RotateDir(this int dir,bool clockwise,int times){
			if(dir == 5){
				return 5;
			}
			if(times < 0){
				times = -times;
				clockwise = !clockwise;
			}
			for(int i=0;i<times;++i){
				switch(dir){
				case 7:
					dir = clockwise?8:4;
					break;
				case 8:
					dir = clockwise?9:7;
					break;
				case 9:
					dir = clockwise?6:8;
					break;
				case 4:
					dir = clockwise?7:1;
					break;
				case 6:
					dir = clockwise?3:9;
					break;
				case 1:
					dir = clockwise?4:2;
					break;
				case 2:
					dir = clockwise?1:3;
					break;
				case 3:
					dir = clockwise?2:6;
					break;
				default:
					return 0;
				}
			}
			return dir;
		}
		public static List<int> GetArc(this int i,int distance){ return i.GetArc(distance,true); } //returns a list of directions: the original direction plus some number of other adjacent directions. 'distance' is how far you want to go out on each side.
		public static List<int> GetArc(this int i,int distance,bool clockwise){
			List<int> result = new List<int>();
			for(int num = -distance;num <= distance;++num){
				result.Add(i.RotateDir(clockwise,num));
			}
			return result;
		}
		public static int DirectionOf(this pos p,pos obj){ //determines which of the 8 directions is closest to the actual direction
			int row = p.row;
			int col = p.col;
			int dy = Math.Abs(obj.row - row);
			int dx = Math.Abs(obj.col - col);
			if(dy == 0){
				if(col < obj.col){
					return 6;
				}
				if(col > obj.col){
					return 4;
				}
				else{
					if(dx == 0){
						return 5;
					}
				}
			}
			if(dx == 0){
				if(row > obj.row){
					return 8;
				}
				else{
					if(row < obj.row){
						return 2;
					}
				}
			}
			if(row+col == obj.row+obj.col){ //slope is -1
				if(row > obj.row){
					return 9;
				}
				else{
					if(row < obj.row){
						return 1;
					}
				}
			}
			if(row-col == obj.row-obj.col){ //slope is 1
				if(row > obj.row){
					return 7;
				}
				else{
					if(row < obj.row){
						return 3;
					}
				}
			}
			// calculate all other dirs here
			int primary; //orthogonal
			int secondary; //diagonal
			int dprimary = Math.Min(dy,dx);
			int dsecondary = Math.Max(dy,dx);
			if(row < obj.row){ //down
				if(col < obj.col){ //right
					secondary = 3;
					if(dx > dy){ //slope less than 1
						primary = 6;
					}
					else{ //slope greater than 1
						primary = 2;
					}
				}
				else{ //left
					secondary = 1;
					if(dx > dy){ //slope less than 1
						primary = 4;
					}
					else{ //slope greater than 1
						primary = 2;
					}
				}
			}
			else{ //up
				if(col < obj.col){ //right
					secondary = 9;
					if(dx > dy){ //slope less than 1
						primary = 6;
					}
					else{ //slope greater than 1
						primary = 8;
					}
				}
				else{ //left
					secondary = 7;
					if(dx > dy){ //slope less than 1
						primary = 4;
					}
					else{ //slope greater than 1
						primary = 8;
					}
				}
			}
			int tiebreaker = primary;
			float ratio = (float)dprimary / (float)dsecondary;
			if(ratio < 0.5f){
				return primary;
			}
			else{
				if(ratio > 0.5f){
					return secondary;
				}
				else{
					return tiebreaker;
				}
			}
		}
		public static int DistanceFrom(this pos p,pos dest){ return p.DistanceFrom(dest.row,dest.col); }
		public static int DistanceFrom(this pos p,int r,int c){
			int dy = Math.Abs(r-p.row);
			int dx = Math.Abs(c-p.col);
			if(dx > dy){
				return dx;
			}
			else{
				return dy;
			}
		}
		public static int ChebyshevDistanceFromX10(this pos p,pos dest){ return p.ChebyshevDistanceFromX10(dest.row,dest.col); } //this is the same as DistanceFrom x 10
		public static int ChebyshevDistanceFromX10(this pos p,int dest_r,int dest_c){ //note that distances are multiplied by 10 so that integer euclidean distances work properly
			int dy = Math.Abs(p.row-dest_r) * 10;
			int dx = Math.Abs(p.col-dest_c) * 10;
			if(dx > dy){
				return dx;
			}
			else{
				return dy;
			}
		}
		public static int ManhattanDistanceFromX10(this pos p,pos dest){ return p.ManhattanDistanceFromX10(dest.row,dest.col); }
		public static int ManhattanDistanceFromX10(this pos p,int dest_r,int dest_c){
			int dy = Math.Abs(p.row-dest_r) * 10; //these are x10 so they can be compared to the estimated euclidean one below.
			int dx = Math.Abs(p.col-dest_c) * 10;
			return dy + dx;
		}
		public static int ApproximateEuclideanDistanceFromX10(this pos p,pos dest){ return p.ApproximateEuclideanDistanceFromX10(dest.row,dest.col); }
		public static int ApproximateEuclideanDistanceFromX10(this pos p,int dest_r,int dest_c){ //no trig here. a position 1 step away diagonally has a distance of 15, not 14 or 1.41.
			int dy = Math.Abs(p.row-dest_r) * 10;
			int dx = Math.Abs(p.col-dest_c) * 10;
			if(dx > dy){
				return dx + (dy/2);
			}
			else{
				return dy + (dx/2);
			}
		}
		public static List<pos> PositionsWithinDistance(this pos p,int dist){ return p.PositionsWithinDistance(dist,false,false); }
		public static List<pos> PositionsWithinDistance(this pos p,int dist,bool exclude_origin,bool ignore_bounds){
			List<pos> result = new List<pos>();
			for(int i=p.row-dist;i<=p.row+dist;++i){
				for(int j=p.col-dist;j<=p.col+dist;++j){
					if(i!=p.row || j!=p.col || exclude_origin==false){
						if(ignore_bounds || BoundsCheck(i,j)){
							result.Add(new pos(i,j));
						}
					}
				}
			}
			return result;
		}
		public static List<pos> PositionsAtDistance(this pos p,int dist){ return p.PositionsAtDistance(dist,false); }
		public static List<pos> PositionsAtDistance(this pos p,int dist,bool ignore_bounds){
			List<pos> result = new List<pos>();
			for(int i=p.row-dist;i<=p.row+dist;++i){
				for(int j=p.col-dist;j<=p.col+dist;++j){
					if(p.DistanceFrom(i,j) == dist){
						if(ignore_bounds || BoundsCheck(i,j)){
							result.Add(new pos(i,j));
						}
					}
					else{
						j = p.col+dist-1;
					}
				}
			}
			return result;
		}
		public static pos PosInDir(this pos p,int dir){
			switch(dir){
			case 7:
				return new pos(p.row-1,p.col-1);
			case 8:
				return new pos(p.row-1,p.col);
			case 9:
				return new pos(p.row-1,p.col+1);
			case 4:
				return new pos(p.row,p.col-1);
			case 5:
				return p;
			case 6:
				return new pos(p.row,p.col+1);
			case 1:
				return new pos(p.row+1,p.col-1);
			case 2:
				return new pos(p.row+1,p.col);
			case 3:
				return new pos(p.row+1,p.col+1);
			default:
				return new pos(-1,-1);
			}
		}
		public static T Random<T>(this List<T> l){ //now, some utility extension methods for lists.
			if(l.Count == 0){
				return default(T);
			}
			return l[R.Roll(l.Count)-1];
		}
		public static T RemoveRandom<T>(this List<T> l){
			if(l.Count == 0){
				return default(T);
			}
			int idx = R.Roll(l.Count)-1;
			T result = l[idx];
			l.RemoveAt(idx);
			//T result = l[R.Roll(l.Count)-1];
			//l.Remove(result);
			return result;
		}
		public static T RemoveLast<T>(this List<T> l){
			if(l.Count == 0){
				return default(T);
			}
			int idx = l.Count-1;
			T result = l[idx];
			l.RemoveAt(idx);
			//T result = l[l.Count-1];
			//l.Remove(result);
			return result;
		}
		public static void AddUnique<T>(this List<T> l,T obj){
			if(!l.Contains(obj)){
				l.Add(obj);
			}
		}
		public static List<T> Randomize<T>(this List<T> l){ //this one operates on the given list, rather than returning a new one
			List<T> temp = new List<T>(l);
			l.Clear();
			while(temp.Count > 0){
				l.Add(temp.RemoveRandom());
			}
			return l;
		}
		public delegate bool BooleanDelegate<T>(T t);
		public static List<T> Where<T>(this List<T> l,BooleanDelegate<T> condition){
			List<T> result = new List<T>();
			foreach(T t in l){
				if(condition(t)){
					result.Add(t);
				}
			}
			return result;
		}
		public static bool Any<T>(this List<T> l,BooleanDelegate<T> condition){
			foreach(T t in l){
				if(condition(t)){
					return true;
				}
			}
			return false;
		}
		public static bool All<T>(this List<T> l,BooleanDelegate<T> condition){
			foreach(T t in l){
				if(!condition(t)){
					return false;
				}
			}
			return true;
		}
		public delegate int IntegerDelegate<T>(T t);
		public static List<T> WhereGreatest<T>(this List<T> l,IntegerDelegate<T> value){
			List<T> result = new List<T>();
			int highest = 0;
			bool first = true;
			foreach(T t in l){
				int i = value(t);
				if(first){
					first = false;
					highest = i;
					result.Add(t);
				}
				else{
					if(i > highest){
						highest = i;
						result.Clear();
						result.Add(t);
					}
					else{
						if(i == highest){
							result.Add(t);
						}
					}
				}
			}
			return result;
		}
		public static List<T> WhereLeast<T>(this List<T> l,IntegerDelegate<T> value){
			List<T> result = new List<T>();
			int lowest = 0;
			bool first = true;
			foreach(T t in l){
				int i = value(t);
				if(first){
					first = false;
					lowest = i;
					result.Add(t);
				}
				else{
					if(i < lowest){
						lowest = i;
						result.Clear();
						result.Add(t);
					}
					else{
						if(i == lowest){
							result.Add(t);
						}
					}
				}
			}
			return result;
		}
		public static void RemoveWhere<T>(this List<T> l,BooleanDelegate<T> condition){
			List<T> removed = new List<T>();
			foreach(T t in l){
				if(condition(t)){
					removed.Add(t);
				}
			}
			foreach(T t in removed){
				l.Remove(t);
			}
		}
		public static int Modulo(this int i,int mod){ //true modulo, for wrapping in a specific range
			int r = i % mod;
			if(r < 0){
				return r + mod;
			}
			else{
				return r;
			}
		}
		public static string PadOuter(this string s,int totalWidth){ //and the missing counterpart to PadRight and PadLeft
			return s.PadOuter(totalWidth,' ');
		}
		public static string PadOuter(this string s,int totalWidth,char paddingChar){
			if(s.Length >= totalWidth){
				return s;
			}
			int added = totalWidth - s.Length;
			string left = "";
			for(int i=0;i<(added+1)/2;++i){
				left = left + paddingChar;
			}
			string right = "";
			for(int i=0;i<added/2;++i){
				right = right + paddingChar;
			}
			return left + s + right;
		}
		public static List<pos> AllPositions<T>(this PosArray<T> array){
			List<pos> result = new List<pos>();
			int rows = array.objs.GetLength(0);
			int cols = array.objs.GetLength(1);
			for(int i=0;i<rows;++i){
				for(int j=0;j<cols;++j){
					result.Add(new pos(i,j));
				}
			}
			return result;
		}
		public static List<pos> AllPositions<T>(this T[,] array){
			List<pos> result = new List<pos>();
			int rows = array.GetLength(0);
			int cols = array.GetLength(1);
			for(int i=0;i<rows;++i){
				for(int j=0;j<cols;++j){
					result.Add(new pos(i,j));
				}
			}
			return result;
		}
		public static pos RandomPosition<T>(this PosArray<T> array,bool allow_borders){
			if(allow_borders){
				return new pos(R.Between(0,array.objs.GetLength(0)),R.Between(0,array.objs.GetLength(1)));
			}
			else{
				return new pos(R.Between(1,array.objs.GetLength(0)-1),R.Between(1,array.objs.GetLength(1)-1));
			}
		}
		public static List<pos> PositionsWhere<T>(this PosArray<T> array,BooleanPositionDelegate condition){
			return array.objs.PositionsWhere(condition);
			/*List<pos> result = new List<pos>();
			int rows = array.objs.GetLength(0);
			int cols = array.objs.GetLength(1);
			for(int i=0;i<rows;++i){
				for(int j=0;j<cols;++j){
					pos p = new pos(i,j);
					if(condition(p)){
						result.Add(p);
					}
				}
			}
			return result;*/
		}
		public static List<pos> PositionsWhere<T>(this T[,] array,BooleanPositionDelegate condition){
			List<pos> result = new List<pos>();
			int rows = array.GetLength(0);
			int cols = array.GetLength(1);
			for(int i=0;i<rows;++i){
				for(int j=0;j<cols;++j){
					pos p = new pos(i,j);
					if(condition(p)){
						result.Add(p);
					}
				}
			}
			return result;
		}
		public static List<pos> PositionsWhereGreatest<T>(this PosArray<T> array,IntegerPositionDelegate value){ //are these useful with no Dijkstra specialization? I'm not sure.
			return array.objs.PositionsWhereGreatest(value);
		}
		public static List<pos> PositionsWhereGreatest<T>(this T[,] array,IntegerPositionDelegate value){
			List<pos> result = new List<pos>();
			int rows = array.GetLength(0);
			int cols = array.GetLength(1);
			int highest = 0;
			bool first = true;
			for(int i=0;i<rows;++i){
				for(int j=0;j<cols;++j){
					pos p = new pos(i,j);
					int val = value(p);
					if(first){
						first = false;
						highest = val;
						result.Add(p);
					}
					else{
						if(val > highest){
							highest = val;
							result.Clear();
							result.Add(p);
						}
						else{
							if(val == highest){
								result.Add(p);
							}
						}
					}
				}
			}
			return result;
		}
		public static List<pos> PositionsWhereLeast<T>(this PosArray<T> array,IntegerPositionDelegate value){
			return array.objs.PositionsWhereLeast(value);
		}
		public static List<pos> PositionsWhereLeast<T>(this T[,] array,IntegerPositionDelegate value){
			List<pos> result = new List<pos>();
			int rows = array.GetLength(0);
			int cols = array.GetLength(1);
			int lowest = 0;
			bool first = true;
			for(int i=0;i<rows;++i){
				for(int j=0;j<cols;++j){
					pos p = new pos(i,j);
					int val = value(p);
					if(first){
						first = false;
						lowest = val;
						result.Add(p);
					}
					else{
						if(val < lowest){
							lowest = val;
							result.Clear();
							result.Add(p);
						}
						else{
							if(val == lowest){
								result.Add(p);
							}
						}
					}
				}
			}
			return result;
		}
		public delegate bool BooleanPositionDelegate(pos p);
		public static List<pos> GetFloodFillPositions<T>(this PosArray<T> array,pos origin,bool exclude_origin,BooleanPositionDelegate condition){
			return array.GetFloodFillPositions(new List<pos>{origin},exclude_origin,condition);
		}
		public static List<pos> GetFloodFillPositions<T>(this PosArray<T> array,List<pos> origins,bool exclude_origins,BooleanPositionDelegate condition){
			List<pos> result = new List<pos>(origins);
			PosArray<bool> result_map = new PosArray<bool>(array.objs.GetLength(0),array.objs.GetLength(1));
			foreach(pos origin in origins){
				result_map[origin] = true;
			}
			List<pos> frontier = new List<pos>(origins);
			while(frontier.Count > 0){
				pos p = frontier.RemoveLast();
				foreach(pos neighbor in p.PositionsAtDistance(1,true).Where(x=>x.BoundsCheck(array))){
					if(!result_map[neighbor] && condition(neighbor)){
						result_map[neighbor] = true;
						frontier.Add(neighbor);
						result.Add(neighbor);
					}
				}
			}
			if(exclude_origins){
				foreach(pos origin in origins){
					result.Remove(origin);
				}
			}
			return result;
		}
		public static PosArray<bool> GetFloodFillArray<T>(this PosArray<T> array,pos origin,bool exclude_origin,BooleanPositionDelegate condition){
			return array.GetFloodFillArray(new List<pos>{origin},exclude_origin,condition);
		}
		public static PosArray<bool> GetFloodFillArray<T>(this PosArray<T> array,List<pos> origins,bool exclude_origins,BooleanPositionDelegate condition){
			PosArray<bool> result_map = new PosArray<bool>(array.objs.GetLength(0),array.objs.GetLength(1));
			foreach(pos origin in origins){
				result_map[origin] = true;
			}
			List<pos> frontier = new List<pos>(origins);
			while(frontier.Count > 0){
				pos p = frontier.RemoveLast();
				foreach(pos neighbor in p.PositionsAtDistance(1,true).Where(x=>x.BoundsCheck(array))){
					if(!result_map[neighbor] && condition(neighbor)){
						result_map[neighbor] = true;
						frontier.Add(neighbor);
					}
				}
			}
			if(exclude_origins){
				foreach(pos origin in origins){
					result_map[origin] = false;
				}
			}
			return result_map;
		}
		public static List<pos> GetRandomizedFloodFillPositions<T>(this PosArray<T> array, pos origin,int desired_count,bool exclude_origin_from_count,bool exclude_origin_from_result,bool cardinal_directions_only,BooleanPositionDelegate condition){
			return array.GetRandomizedFloodFillPositions(new List<pos>{origin},desired_count,exclude_origin_from_count,exclude_origin_from_result,cardinal_directions_only,condition);
		}
		public static List<pos> GetRandomizedFloodFillPositions<T>(this PosArray<T> array, List<pos> origins,int desired_count,bool exclude_origins_from_count,bool exclude_origins_from_result,bool cardinal_directions_only,BooleanPositionDelegate condition){
			List<pos> result = new List<pos>();
			PosArray<bool> result_map = new PosArray<bool>(array.objs.GetLength(0),array.objs.GetLength(1));
			List<pos> frontier = new List<pos>();
			int count = 0;
			int[] dirs = cardinal_directions_only? U.FourDirections : U.EightDirections;
			foreach(pos origin in origins){
				result_map[origin] = true;
				if(condition(origin)){
					if(!exclude_origins_from_count){
						++count;
					}
					if(!exclude_origins_from_result){
						result.Add(origin);
					}
				}
				foreach(int dir in dirs){
					pos neighbor = origin.PosInDir(dir);
					if(neighbor.BoundsCheck(array) && !result_map[neighbor]){
						result_map[neighbor] = true;
						frontier.Add(neighbor);
					}
				}
			}
			while(frontier.Count > 0 && count < desired_count){
				pos p = frontier.RemoveRandom();
				if(condition(p)){
					result.Add(p);
					++count;
					foreach(int dir in dirs){
						pos neighbor = p.PosInDir(dir);
						if(neighbor.BoundsCheck(array) && !result_map[neighbor]){
							result_map[neighbor] = true;
							frontier.Add(neighbor);
						}
					}
				}
			}
			return result;
		}
		public static int DijkstraMax = int.MaxValue;
		public static int DijkstraMin = int.MinValue;
		public static bool IsValidDijkstraValue(this int i){
			if(i == DijkstraMin || i == DijkstraMax){
				return false;
			}
			return true;
		}
		public delegate int IntegerPositionDelegate(pos p);
		public static PosArray<int> GetDijkstraMap<T>(this PosArray<T> array,BooleanPositionDelegate is_blocked,List<pos> sources){ return array.GetDijkstraMap(is_blocked,x=>1,sources); }
		public static PosArray<int> GetDijkstraMap<T>(this PosArray<T> array,BooleanPositionDelegate is_blocked,IntegerPositionDelegate get_cost,List<pos> sources){ return array.GetDijkstraMap(is_blocked,get_cost,sources,new pos(-1,-1)); }
		public static PosArray<int> GetDijkstraMap<T>(this PosArray<T> array,BooleanPositionDelegate is_blocked,IntegerPositionDelegate get_cost,List<pos> sources,pos destination){
			int height = array.objs.GetLength(0);
			int width = array.objs.GetLength(1);
			PosArray<int> map = new PosArray<int>(height,width);
			PriorityQueue<pos> frontier = new PriorityQueue<pos>(x => -map[x]);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(is_blocked(new pos(i,j))){
						map[i,j] = DijkstraMin;
					}
					else{
						map[i,j] = DijkstraMax;
					}
				}
			}
			foreach(pos p in sources){
				map[p] = 0;
				frontier.Add(p);
			}
			while(frontier.list.Count > 0){
				pos p = frontier.Pop();
				if(p.Equals(destination)){ //if a destination is supplied, a partial map will be returned that contains the shortest path.
					break;
				}
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(p.row+s >= 0 && p.row+s < height && p.col+t >= 0 && p.col+t < width){
							pos neighbor = new pos(p.row+s,p.col+t);
							int cost = get_cost(neighbor);
							if(map[neighbor] > map[p]+cost){
								map[neighbor] = map[p]+cost;
								frontier.Add(neighbor);
							}
						}
					}
				}
			}
			return map;
		}
		public static PosArray<int> GetDijkstraMap<T>(this PosArray<T> array,BooleanPositionDelegate is_blocked,BooleanPositionDelegate is_source){ return array.GetDijkstraMap(is_blocked,is_source,x=>0,x=>1); }
		public static PosArray<int> GetDijkstraMap<T>(this PosArray<T> array,BooleanPositionDelegate is_blocked,BooleanPositionDelegate is_source,IntegerPositionDelegate source_value,IntegerPositionDelegate get_cost){ return array.GetDijkstraMap(is_blocked,is_source,source_value,get_cost,new pos(-1,-1)); }
		public static PosArray<int> GetDijkstraMap<T>(this PosArray<T> array,BooleanPositionDelegate is_blocked,BooleanPositionDelegate is_source,IntegerPositionDelegate source_value,IntegerPositionDelegate get_cost,pos destination){
			int height = array.objs.GetLength(0);
			int width = array.objs.GetLength(1);
			PosArray<int> map = new PosArray<int>(height,width);
			PriorityQueue<pos> frontier = new PriorityQueue<pos>(x => -map[x]);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					pos p = new pos(i,j);
					if(is_source(p)){
						map[p] = source_value(p);
						frontier.Add(p);
					}
					else{
						if(is_blocked(p)){
							map[p] = DijkstraMin;
						}
						else{
							map[p] = DijkstraMax;
						}
					}
				}
			}
			while(frontier.list.Count > 0){
				pos p = frontier.Pop();
				if(p.Equals(destination)){
					break;
				}
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(p.row+s >= 0 && p.row+s < height && p.col+t >= 0 && p.col+t < width){
							pos neighbor = new pos(p.row+s,p.col+t);
							int cost = get_cost(neighbor);
							if(map[neighbor] > map[p]+cost){
								map[neighbor] = map[p]+cost;
								frontier.Add(neighbor);
							}
						}
					}
				}
			}
			return map;
		}
		public static PosArray<int> GetManhattanDijkstraMap<T>(this PosArray<T> array,BooleanPositionDelegate is_blocked,BooleanPositionDelegate is_source){
			int height = array.objs.GetLength(0);
			int width = array.objs.GetLength(1);
			PosArray<int> map = new PosArray<int>(height,width);
			PriorityQueue<pos> frontier = new PriorityQueue<pos>(x => -map[x]);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					pos p = new pos(i,j);
					if(is_source(p)){
						map[p] = 0;
						frontier.Add(p);
					}
					else{
						if(is_blocked(p)){
							map[p] = DijkstraMin;
						}
						else{
							map[p] = DijkstraMax;
						}
					}
				}
			}
			while(frontier.list.Count > 0){
				pos p = frontier.Pop();
				foreach(int dir in U.FourDirections){
					pos neighbor = p.PosInDir(dir);
					if(neighbor.BoundsCheck(map)){
						int cost = 1;
						if(map[neighbor] > map[p]+cost){
							map[neighbor] = map[p]+cost;
							frontier.Add(neighbor);
						}
					}
				}
			}
			return map;
		}
		public static List<pos> GetAStarPath<T>(this PosArray<T> array,pos start,pos goal,BooleanPositionDelegate is_blocked){
			return array.GetAStarPath(start,goal,is_blocked,x=>1,1);
		}
		public static List<pos> GetAStarPath<T>(this PosArray<T> array,pos start,pos goal,BooleanPositionDelegate is_blocked,IntegerPositionDelegate get_cost,int default_cost){
			int height = array.objs.GetLength(0);
			int width = array.objs.GetLength(1);
			PosArray<int> map = new PosArray<int>(height,width);
			PriorityQueue<pos> frontier = new PriorityQueue<pos>(x => -(map[x] + x.DistanceFrom(goal)*default_cost));
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(is_blocked(new pos(i,j))){
						map[i,j] = DijkstraMin;
					}
					else{
						map[i,j] = DijkstraMax;
					}
				}
			}
			frontier.Add(start);
			map[start] = 0;
			while(frontier.list.Count > 0){
				pos p = frontier.Pop();
				if(p.Equals(goal)){
					List<pos> result = new List<pos>();
					pos current_position = goal;
					while(true){
						List<pos> valid = current_position.PositionsAtDistance(1).Where(x=>map[x].IsValidDijkstraValue() && map[x] < map[current_position]).WhereLeast(x=>map[x]).WhereLeast(x=>x.ApproximateEuclideanDistanceFromX10(current_position));
						if(valid.Count > 0){
							/*if(deterministic_results){
								current_position = valid.Last();
							}
							else{*/
								current_position = valid.Random();
							//}
							result.Add(current_position);
							if(current_position.Equals(start)){
								result.Reverse();
								return result;
							}
						}
						else{
							return null;
						}
					}
				}
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(p.row+s >= 0 && p.row+s < height && p.col+t >= 0 && p.col+t < width){
							pos neighbor = new pos(p.row+s,p.col+t);
							int cost = get_cost(neighbor);
							if(map[neighbor] > map[p]+cost){
								map[neighbor] = map[p]+cost;
								frontier.Add(neighbor);
							}
						}
					}
				}
			}
			return null; //no path found
		}
		public static int PathingDistanceFrom<T>(this PosArray<T> array,pos start,pos goal,BooleanPositionDelegate is_blocked){
			return array.PathingDistanceFrom(start,goal,is_blocked,x=>1,1);
		}
		public static int PathingDistanceFrom<T>(this PosArray<T> array,pos start,pos goal,BooleanPositionDelegate is_blocked,IntegerPositionDelegate get_cost,int default_cost){
			int height = array.objs.GetLength(0);
			int width = array.objs.GetLength(1);
			PosArray<int> map = new PosArray<int>(height,width);
			PriorityQueue<pos> frontier = new PriorityQueue<pos>(x => -(map[x] + x.DistanceFrom(goal)*default_cost));
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(is_blocked(new pos(i,j))){
						map[i,j] = DijkstraMin;
					}
					else{
						map[i,j] = DijkstraMax;
					}
				}
			}
			frontier.Add(start);
			map[start] = 0;
			while(frontier.list.Count > 0){
				pos p = frontier.Pop();
				if(p.Equals(goal)){
					return map[p];
				}
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(p.row+s >= 0 && p.row+s < height && p.col+t >= 0 && p.col+t < width){
							pos neighbor = new pos(p.row+s,p.col+t);
							int cost = get_cost(neighbor);
							if(map[neighbor] > map[p]+cost){
								map[neighbor] = map[p]+cost;
								frontier.Add(neighbor);
							}
						}
					}
				}
			}
			return -1; //no path found
		}
		public static T GetWrapped<T>(this PosArray<T> array,pos p){
			return array.GetWrapped(p.row,p.col);
		}
		public static T GetWrapped<T>(this PosArray<T> array,int row,int col){
			return array[row.Modulo(array.objs.GetLength(0)),col.Modulo(array.objs.GetLength(1))];
		}
		public static void SetWrapped<T>(this PosArray<T> array,pos p,T t){
			array.SetWrapped(p.row,p.col,t);
		}
		public static void SetWrapped<T>(this PosArray<T> array,int row,int col,T t){
			array[row.Modulo(array.objs.GetLength(0)),col.Modulo(array.objs.GetLength(1))] = t;
		}
		public static PosArray<float> GetNoise(int height,int width){
			return GetNoise(Math.Max(height,width));
		}
		public static PosArray<float> GetNoise(int size){
			int size2 = 2;
			while(size2+1 < size){
				size2 *= 2;
			}
			++size2; //power of two, plus one
			PosArray<float> n = new PosArray<float>(size2,size2);
			int step = 1;
			while(DiamondStep(n,step)){
				SquareStep(n,step);
				++step;
			}
			return n;
		}
		private static bool DiamondStep(this PosArray<float> a,int step){
			int divisions = 1; //divisions^2 is the number of squares
			while(step > 1){
				divisions *= 2;
				--step;
			}
			int increment = a.objs.GetUpperBound(0) / divisions;
			if(increment == 1){
				return false; //done!
			}
			for(int i=0;i<divisions;++i){
				for(int j=0;j<divisions;++j){
					float total = 0;
					total += a[i*increment,j*increment];
					total += a[i*increment,(j+1)*increment];
					total += a[(i+1)*increment,j*increment];
					total += a[(i+1)*increment,(j+1)*increment];
					total = total / 4;
					double val = (R.r.NextDouble() - 0.5) * 2;
					total += (float)(val / divisions);
					a[i*increment + increment/2,j*increment + increment/2] = total;
				}
			}
			return true;
		}
		private static void SquareStep(this PosArray<float> a,int step){
			int divisions = 1;
			while(step > 0){
				divisions *= 2;
				--step;
			}
			int increment = a.objs.GetUpperBound(0) / divisions;
			for(int i=0;i<=divisions;++i){
				for(int j=0;j<=divisions;++j){
					if((i+j)%2 == 1){
						float total = 0;
						total += a.GetWrapped((i-1)*increment,j*increment);
						total += a.GetWrapped(i*increment,(j-1)*increment);
						total += a.GetWrapped(i*increment,(j+1)*increment);
						total += a.GetWrapped((i+1)*increment,j*increment);
						total = total / 4;
						double val = (R.r.NextDouble() - 0.5) * 2;
						total += (float)(val / (divisions/2));
						a[i*increment,j*increment] = total;
					}
				}
			}
		}
	}
	public static class R{ //random methods
		public static Random r = new Random();
		public static void SetSeed(int seed){ r = new Random(seed); }
		public static int Roll(int dice,int sides){
			if(sides == 0){
				return 0;
			}
			int total = 0;
			for(int i=0;i<dice;++i){
				total += r.Next(1,sides+1); //Next's maxvalue is exclusive, thus the +1
			}
			return total;
		}
		public static int Roll(int sides){
			if(sides == 0){
				return 0;
			}
			int total = 0;
			total += r.Next(1,sides+1); //Next's maxvalue is exclusive, thus the +1
			return total;
		}
		public static int Between(int a,int b){ //inclusive
			int min = Math.Min(a,b);
			int max = Math.Max(a,b);
			return Roll((max-min)+1) + (min-1);
		}
		public static bool CoinFlip(){
			return r.Next(1,3) == 2;
		}
		public static bool OneIn(int x){
			return r.Next(1,x+1) == x;
		}
		public static bool PercentChance(int x){
			return r.Next(1,101) <= x;
		}
	}
	public enum NumberType{Value,Range,Sequence,Delta};
	public class Number{
		public NumberType Type = NumberType.Value;
		public int Value = 0;
		public Number RangeMin = null;
		public Number RangeMax = null;
		public List<Number> Sequence = null;
		public int sequence_index = 0;
		public Number Delta = null; //todo: some of these should be properties, with validation on changes.
		public static implicit operator int(Number n){
			return n.GetValue();
		}
		public int GetValue(){ //also, need to enforce the requirement that every number eventually evaluates to an int.
			switch(Type){
			case NumberType.Range:
				return R.Between(RangeMin.GetValue(),RangeMax.GetValue());
			case NumberType.Sequence:
			{
				int result = Sequence[sequence_index].GetValue();
				++sequence_index;
				if(sequence_index == Sequence.Count){
					sequence_index = 0;
				}
				return result;
			}
			case NumberType.Delta:
			{
				int result = Value;
				Value += Delta.GetValue();
				return result;
			}
			case NumberType.Value:
			default:
				return Value;
			}
		}
		public static Number CreateValue(int value){
			Number n = new Number();
			n.Value = value;
			return n;
		}
		public static Number CreateRange(Number min,Number max){
			Number n = new Number();
			n.Type = NumberType.Range;
			n.RangeMin = min;
			n.RangeMax = max;
			return n;
		}
		public static Number CreateRange(int min,int max){
			return CreateRange(CreateValue(min),CreateValue(max));
		}
		public static Number CreateRange(Number min,int max){
			return CreateRange(min,CreateValue(max));
		}
		public static Number CreateRange(int min,Number max){
			return CreateRange(CreateValue(min),max);
		}
		public static Number CreateSequence(){
			Number n = new Number();
			n.Type = NumberType.Sequence;
			n.Sequence = new List<Number>();
			return n;
		}
		public Number Add(int value){
			if(Type != NumberType.Sequence){
				throw new InvalidOperationException("This method can only be used with a Sequence");
			}
			Sequence.Add(CreateValue(value));
			return this;
		}
		public Number Add(Number number){
			if(Type != NumberType.Sequence){
				throw new InvalidOperationException("This method can only be used with a Sequence");
			}
			Sequence.Add(number);
			return this;
		}
		public static Number CreateDelta(int initial_value,Number delta){
			Number n = new Number();
			n.Type = NumberType.Delta;
			n.Value = initial_value;
			n.Delta = delta;
			return n;
		}
		public static Number CreateDelta(int initial_value,int delta){
			return CreateDelta(initial_value,CreateValue(delta));
		}
	}
	public class FloatNumber{
		public NumberType Type = NumberType.Value;
		public float Value = 0;
		public FloatNumber RangeMin = null;
		public FloatNumber RangeMax = null;
		public List<FloatNumber> Sequence = null;
		public int sequence_index = 0;
		public FloatNumber Delta = null; //todo: some of these should be properties, with validation on changes.
		public static implicit operator float(FloatNumber n){
			return n.GetValue();
		}
		public float GetValue(){ //also, need to enforce the requirement that every number eventually evaluates to a float.
			switch(Type){
			case NumberType.Range:
			{
				while(true){
					float min = RangeMin.GetValue();
					float max = RangeMax.GetValue();
					float result = (float)(R.r.NextDouble() * (double)(max-min) + (double)min);
					if(!float.IsInfinity(result) && !float.IsNaN(result)){
						return result;
					}
				}
			}
			case NumberType.Sequence:
			{
				float result = Sequence[sequence_index].GetValue();
				++sequence_index;
				if(sequence_index == Sequence.Count){
					sequence_index = 0;
				}
				return result;
			}
			case NumberType.Delta:
			{
				float result = Value;
				Value += Delta.GetValue();
				return result;
			}
			case NumberType.Value:
			default:
				return Value;
			}
		}
		public static FloatNumber CreateValue(float value){
			FloatNumber n = new FloatNumber();
			n.Value = value;
			return n;
		}
		public static FloatNumber CreateRange(FloatNumber min,FloatNumber max){
			FloatNumber n = new FloatNumber();
			n.Type = NumberType.Range;
			n.RangeMin = min;
			n.RangeMax = max;
			return n;
		}
		public static FloatNumber CreateRange(float min,float max){
			return CreateRange(CreateValue(min),CreateValue(max));
		}
		public static FloatNumber CreateRange(FloatNumber min,float max){
			return CreateRange(min,CreateValue(max));
		}
		public static FloatNumber CreateRange(float min,FloatNumber max){
			return CreateRange(CreateValue(min),max);
		}
		public static FloatNumber CreateSequence(){
			FloatNumber n = new FloatNumber();
			n.Type = NumberType.Sequence;
			n.Sequence = new List<FloatNumber>();
			return n;
		}
		public FloatNumber Add(float value){
			if(Type != NumberType.Sequence){
				throw new InvalidOperationException("This method can only be used with a Sequence");
			}
			Sequence.Add(CreateValue(value));
			return this;
		}
		public FloatNumber Add(FloatNumber number){
			if(Type != NumberType.Sequence){
				throw new InvalidOperationException("This method can only be used with a Sequence");
			}
			Sequence.Add(number);
			return this;
		}
		public static FloatNumber CreateDelta(float initial_value,FloatNumber delta){
			FloatNumber n = new FloatNumber();
			n.Type = NumberType.Delta;
			n.Value = initial_value;
			n.Delta = delta;
			return n;
		}
		public static FloatNumber CreateDelta(float initial_value,float delta){
			return CreateDelta(initial_value,CreateValue(delta));
		}
	}
}
