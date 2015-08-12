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
using Utilities;
namespace Forays{
	public class Queue{
		public LinkedList<Event> list;
		public int turn;
		public Event current_event = null;
		public int Tiebreaker{get{
				if(current_event == null){
					return -1;
				}
				return current_event.tiebreaker;
				/*if(list.Count > 0){
					return list.First.Value.tiebreaker;
				}
				else{
					return -1;
				}*/
			}
		}
		public static Buffer B;
		public Queue(Game g){
			list = new LinkedList<Event>();
			turn = 0;
			B = g.B;
		}
		public void Add(Event e){
			if(e.TimeToExecute() == turn){ //0-time action
				list.AddFirst(e); //this means that creating 0-delay events can put them in the wrong order. this hasn't been a problem yet.
			}
			else{
				if(list.First==null){
					list.AddFirst(e);
				}
				else{
					if(e >= list.Last.Value){
						list.AddLast(e);
					}
					else{
						if(e < list.First.Value){
							list.AddFirst(e);
						}
						else{ //it's going between two events
							LinkedListNode<Event> current = list.Last;
							while(true){
								if(e >= current.Previous.Value){
									list.AddAfter(current.Previous,e);
									return;
								}
								else{
									current = current.Previous;
								}
							}
						}
					}
				}
			}
		}
		public void Pop(){
			current_event = list.First.Value;
			turn = current_event.TimeToExecute();
			current_event.Execute();
			list.Remove(current_event);
			/*turn = list.First.Value.TimeToExecute();
			Event e = list.First.Value;
			e.Execute();
			list.Remove(e);*/
		}
		public void ResetForNewLevel(){
			LinkedList<Event> newlist = new LinkedList<Event>();
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(current.Value.target == Event.player){
					newlist.AddLast(current.Value);
				}
			}
			list = newlist;
		}
		public void KillEvents(PhysicalObject target,EventType type){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				current.Value.Kill(target,type);
			}
		}
		public void KillEvents(PhysicalObject target,AttrType attr){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				current.Value.Kill(target,attr);
			}
		}
		public Event FindAttrEvent(PhysicalObject target,AttrType attr){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(!current.Value.dead && current.Value.target == target && current.Value.type == EventType.REMOVE_ATTR && current.Value.attr == attr){
					return current.Value;
				}
			}
			return null;
		}
		public Event FindTargetedEvent(PhysicalObject target,EventType type){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(!current.Value.dead && current.Value.target == target && current.Value.type == type){
					return current.Value;
				}
			}
			return null;
		}
		public void RemoveTilesFromEventAreas(List<Tile> to_be_removed,EventType type){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(current.Value.type == type){
					foreach(Tile t in to_be_removed){
						if(current.Value.area.Contains(t)){
							current.Value.area.Remove(t);
							if(current.Value.area.Count == 0){
								current.Value.dead = true;
								break;
							}
						}
					}
				}
			}
		}
		public bool Contains(EventType type){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(current.Value.type == type){
					return true;
				}
			}
			return false;
		}
		public void UpdateTiebreaker(int new_tiebreaker){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(current.Value.tiebreaker >= new_tiebreaker){
					current.Value.tiebreaker++;
				}
			}
		}
	}
	public class Event{
		public PhysicalObject target;
		public List<Tile> area = null;
		public int delay;
		public EventType type;
		public AttrType attr;
		public FeatureType feature;
		public int value;
		public int secondary_value = 0;
		public string msg;
		public List<PhysicalObject> msg_objs; //used to determine visibility of msg
		public int time_created;
		public bool dead;
		public int tiebreaker;
		public static Queue Q;
		public static Buffer B;
		public static Map M;
		public static Actor player;
		public Event(){}
		public Event(PhysicalObject target_,int delay_){
			target=target_;
			delay=delay_;
			type=EventType.MOVE;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_){ //todo: try removing some of these constructors. maybe FINALLY do Event.Create() and/or Event.RemoveAttr(...), Event.Move(...). that might work.
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=1;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,int value_){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=value_;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,string msg_){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=1;
			msg=msg_;
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,int value_,string msg_){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=value_;
			msg=msg_;
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,string msg_,params PhysicalObject[] objs){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=1;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,int value_,string msg_,params PhysicalObject[] objs){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=value_;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(int delay_,EventType type_){
			target=null;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,EventType type_){
			target=target_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,int delay_,EventType type_,int value_){
			target=target_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=value_;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(int delay_,string msg_,params PhysicalObject[] objs){
			target=null;
			delay=delay_;
			type=EventType.ANY_EVENT;
			attr=AttrType.NO_ATTR;
			value=0;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(List<Tile> area_,int delay_,EventType type_){
			target=null;
			/*area = new List<Tile>(); //todo: reverted this. hope it works.
			foreach(Tile t in area_){
				area.Add(t);
			}*/
			area=area_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(List<Tile> area_,int delay_,EventType type_,int value_){
			target=null;
			area=area_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=value_;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(List<Tile> area_,int delay_,EventType type_,string msg_,params PhysicalObject[] objs){
			target=null;
			area=area_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,List<Tile> area_,int delay_,EventType type_){
			target=target_;
			area=area_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,List<Tile> area_,int delay_,EventType type_,AttrType attr_,int value_,string msg_,params PhysicalObject[] objs){
			target=target_;
			area=area_;
			delay=delay_;
			type=type_;
			attr=attr_;
			value=value_;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,List<Tile> area_,int delay_,EventType type_,AttrType attr_,FeatureType feature_,int value_,string msg_,params PhysicalObject[] objs){
			target=target_;
			area=area_;
			delay=delay_;
			type=type_;
			attr=attr_;
			feature = feature_;
			value=value_;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
		}
		public static void RemoveGas(List<Tile> area,int delay,FeatureType gas,int chance){
			Q.Add(new Event(null,area,delay,EventType.REMOVE_GAS,AttrType.NO_ATTR,gas,chance,""));
		}
		public int TimeToExecute(){ return delay + time_created; }
		public void Kill(PhysicalObject target_,EventType type_){
			if(msg_objs != null && (type == type_ || type_ == EventType.ANY_EVENT)){
				if(msg_objs.Contains(target_)){
					msg_objs.Remove(target_);
				}
			}
			Tile t = target_ as Tile;
			if(t != null && area != null && area.Contains(t)){
/*				target = null;
				if(msg_objs != null){
					msg_objs.Clear();
					msg_objs = null;
				}
				area.Clear();
				area = null;
				dead = true;*/
				area.Remove(t);
			}
			if(target_ == target && type == EventType.TOMBSTONE_GHOST && (type_ == type || type_ == EventType.ANY_EVENT)){
				target = null;
				return; //don't destroy the event, just remove the reference to the ghost.
			}
			if(target == target_ && (type == type_ || type_ == EventType.ANY_EVENT)){
				target = null;
				if(msg_objs != null){
					msg_objs.Clear();
					msg_objs = null;
				}
				if(area != null){
					area.Clear();
					area = null;
				}
				dead = true;
			}
			if(type_ == EventType.CHECK_FOR_HIDDEN && type == EventType.CHECK_FOR_HIDDEN){
				dead = true;
			}
			if(target_ == null && type_ == EventType.REGENERATING_FROM_DEATH && type == EventType.REGENERATING_FROM_DEATH){
				dead = true;
			}
			if(target_ == null && type_ == EventType.POLTERGEIST && type == EventType.POLTERGEIST){
				dead = true;
			}
			if(target_ == null && type_ == EventType.RELATIVELY_SAFE && type == EventType.RELATIVELY_SAFE){
				dead = true;
			}
			if(target_ == null && type_ == EventType.BLAST_FUNGUS && type == EventType.BLAST_FUNGUS){
				dead = true;
			}
		}
		public void Kill(PhysicalObject target_,AttrType attr_){
			if(target==target_ && type==EventType.REMOVE_ATTR && attr==attr_){
				target = null;
				if(msg_objs != null){
					msg_objs.Clear();
					msg_objs = null;
				}
				if(area != null){
					area.Clear();
					area = null;
				}
				dead = true;
			}
		}
		public void Execute(){
			if(!dead){
				switch(type){
				case EventType.MOVE:
				{
					Actor temp = target as Actor;
					temp.Input();
					break;
				}
				case EventType.REMOVE_ATTR:
				{
					Actor temp = target as Actor;
					if(attr == AttrType.FLYING){
						temp.attrs[AttrType.DESCENDING] = 2;
						if(temp == player){
							B.Add("You start to descend as your flight wears off. ");
							B.Print(true);
						}
						break;
					}
					if(attr == AttrType.SHINING){
						int old_rad = temp.LightRadius();
						temp.attrs[attr] -= value;
						if(old_rad != temp.LightRadius() && !temp.HasAttr(AttrType.BURROWING)){
							temp.UpdateRadius(old_rad,temp.LightRadius());
						}
						break;
					}
					if(temp.type == ActorType.BERSERKER && attr == AttrType.COOLDOWN_2){
						temp.attrs[attr] = 0; //this hack can probably be removed
					}
					else{
						temp.attrs[attr] -= value;
					}
					if(attr == AttrType.BURNING && temp.LightRadius() == 0 && !temp.HasAttr(AttrType.BURROWING)){
						temp.UpdateRadius(1,0);
					}
					if(attr == AttrType.TELEPORTING){
						temp.attrs[attr] = 0;
					}
					if(attr==AttrType.CONVICTION){
						if(temp.HasAttr(AttrType.IN_COMBAT)){
							temp.attrs[AttrType.CONVICTION] += value; //whoops, undo that
						}
						else{
							temp.attrs[AttrType.BONUS_SPIRIT] -= value;      //otherwise, set things to normal
							temp.attrs[AttrType.BONUS_COMBAT] -= (value+1) / 2;
							if(temp.attrs[AttrType.KILLSTREAK] >= 2){
								B.Add("You wipe off your weapon. ");
							}
							temp.attrs[AttrType.KILLSTREAK] = 0;
						}
					}
					if(attr==AttrType.COOLDOWN_1 && temp.type == ActorType.BERSERKER){
						B.Add(temp.Your() + " rage diminishes. ",temp);
						B.Add(temp.the_name + " dies. ",temp);
						temp.Kill();
					}
					break;
				}
				case EventType.REMOVE_GAS:
				{
					List<Tile> removed = new List<Tile>();
					foreach(Tile t in area){
						if(t.Is(feature)){
							if(R.PercentChance(value)){
								t.RemoveFeature(feature);
								removed.Add(t);
							}
						}
						else{
							removed.Add(t);
						}
					}
					foreach(Tile t in removed){
						area.Remove(t);
					}
					if(area.Count > 0){
						Event.RemoveGas(area,100,feature,value);
					}
					break;
				}
				case EventType.CHECK_FOR_HIDDEN:
				{
					List<Tile> removed = new List<Tile>();
					foreach(Tile t in area){
						if(player.CanSee(t)){
							int exponent = player.DistanceFrom(t) + 1;
							if(player.magic_trinkets.Contains(MagicTrinketType.RING_OF_KEEN_SIGHT)){
								--exponent;
							}
							if(!t.IsLit()){
								if(!player.HasAttr(AttrType.SHADOWSIGHT)){
									++exponent;
								}
							}
							if(exponent > 8){
								exponent = 8; //because 1 in 256 is enough.
							}
							int difficulty = 1;
							for(int i=exponent;i>0;--i){
								difficulty = difficulty * 2;
							}
							if(R.Roll(difficulty) == difficulty){
								if(t.IsTrap() || t.Is(TileType.FIRE_GEYSER) || t.Is(TileType.FOG_VENT) || t.Is(TileType.POISON_GAS_VENT)){
									t.name = Tile.Prototype(t.type).name;
									t.a_name = Tile.Prototype(t.type).a_name;
									t.the_name = Tile.Prototype(t.type).the_name;
									t.symbol = Tile.Prototype(t.type).symbol;
									t.color = Tile.Prototype(t.type).color;
									B.Add("You notice " + t.AName(true) + ". ");
								}
								else{
									if(t.type == TileType.HIDDEN_DOOR){
										t.Toggle(null);
										B.Add("You notice a hidden door. ");
									}
								}
								removed.Add(t);
							}
						}
					}
					foreach(Tile t in removed){
						area.Remove(t);
					}
					if(area.Count > 0){
						Q.Add(new Event(area,100,EventType.CHECK_FOR_HIDDEN));
					}
					break;
				}
				case EventType.RELATIVELY_SAFE:
				{
					if(M.AllActors().Count == 1 && !Q.Contains(EventType.POLTERGEIST)
					&& !Q.Contains(EventType.REGENERATING_FROM_DEATH) && !Q.Contains(EventType.MIMIC) && !Q.Contains(EventType.MARBLE_HORROR)){
						//B.Add("The dungeon is still and silent. ");
						B.Add("The dungeon is utterly silent for a moment. ");
						B.PrintAll();
					}
					else{
						Q.Add(new Event((R.Roll(20)+30)*100,EventType.RELATIVELY_SAFE));
					}
					break;
				}
				case EventType.POLTERGEIST:
				{
					if(target != null && target is Actor){ //target can either be a stolen item, or the currently manifested poltergeist.
						Q.Add(new Event(target,area,(R.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,""));
						break; //if it's manifested, the event does nothing for now.
					}
					if(area.Any(t => t.actor() == player)){
						bool manifested = false;
						if(value == 0){
							B.Add("You feel like you're being watched. ");
						}
						else{
							if(target != null){ //if it has a stolen item
								Tile tile = null;
								tile = area.Where(t => t.actor() == null && t.DistanceFrom(player) >= 2
								                  && t.HasLOE(player) && t.FirstActorInLine(player) == player).Random();
								if(tile != null){
									Actor temporary = new Actor(ActorType.POLTERGEIST,"something",'G',Color.DarkGreen,1,1,0,0);
									temporary.a_name = "something";
									temporary.the_name = "something";
									temporary.p = tile.p;
									temporary.inv = new List<Item>();
									temporary.inv.Add(target as Item);
									Item item = temporary.inv[0];
									if(item.NameOfItemType() == "orb"){
										temporary.inv[0].Use(temporary,temporary.GetBestExtendedLineOfEffect(player));
									}
									else{
										B.Add("Something throws " + item.AName() + ". ",temporary);
										B.DisplayNow();
										Screen.AnimateProjectile(tile.GetBestExtendedLineOfEffect(player).ToFirstSolidTileOrActor(),new colorchar(item.color,item.symbol));
										player.tile().GetItem(item);
										B.Add(item.TheName() + " hits you. ");
										player.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),temporary,"a flying " + item.Name());
									}
									target = null;
								}
								else{
									Q.Add(new Event(target,area,100,EventType.POLTERGEIST,AttrType.NO_ATTR,value,""));
									return; //try again next turn
								}
							}
							else{
								if(value >= 2 && area.Any(t => t.DistanceFrom(player) == 1 && t.passable && t.actor() == null)){
									Tile tile = area.Where(t => t.DistanceFrom(player) == 1 && t.passable && t.actor() == null).Random();
									B.DisplayNow();
									for(int i=4;i>0;--i){
										Screen.AnimateStorm(tile.p,i,2,1,'G',Color.DarkGreen);
									}
									Actor a = Actor.Create(ActorType.POLTERGEIST,tile.row,tile.col,TiebreakerAssignment.UseCurrent);
									Q.KillEvents(a,EventType.MOVE);
									a.Q0();
									a.player_visibility_duration = -1;
									B.Add("A poltergeist manifests in front of you! ");
									Q.Add(new Event(a,area,(R.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,""));
									manifested = true;
								}
								else{
									if(player.tile().type == TileType.DOOR_O){
										B.Add("The door slams closed on you! ");
										player.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"a slamming door");
									}
									else{
										Tile tile = null; //check for items to throw...
										tile = area.Where(t => t.inv != null && t.actor() == null && t.DistanceFrom(player) >= 2
										                  && t.HasLOE(player) && t.FirstActorInLine(player) == player).Random();
										if(tile != null){
											Actor temporary = new Actor(ActorType.POLTERGEIST,"something",'G',Color.DarkGreen,1,1,0,0);
											temporary.a_name = "something";
											temporary.the_name = "something";
											temporary.p = tile.p;
											temporary.inv = new List<Item>();
											if(tile.inv.quantity <= 1){
												temporary.inv.Add(tile.inv);
												tile.inv = null;
											}
											else{
												temporary.inv.Add(new Item(tile.inv,-1,-1));
												tile.inv.quantity--;
											}
											M.Draw();
											Item item = temporary.inv[0];
											if(item.NameOfItemType() == "orb"){
												temporary.inv[0].Use(temporary,temporary.GetBestExtendedLineOfEffect(player));
											}
											else{
												B.Add("Something throws " + item.TheName() + ". ",temporary);
												B.DisplayNow();
												Screen.AnimateProjectile(tile.GetBestExtendedLineOfEffect(player).ToFirstSolidTileOrActor(),new colorchar(item.color,item.symbol));
												player.tile().GetItem(item);
												B.Add(item.TheName() + " hits you. ");
												player.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),temporary,"a flying " + item.Name());
											}
										}
										else{
											if(area.Any(t => t.type == TileType.DOOR_O || t.type == TileType.DOOR_C)){
												Tile door = area.Where(t=>t.type == TileType.DOOR_O || t.type == TileType.DOOR_C).Random();
												if(door.type == TileType.DOOR_C){
													if(player.CanSee(door)){
														B.Add("The door flies open! ",door);
													}
													else{
														if(door.seen || player.DistanceFrom(door) <= 12){
															B.Add("You hear a door slamming. ");
														}
													}
													door.Toggle(null);
												}
												else{
													if(door.actor() == null){
														if(player.CanSee(door)){
															B.Add("The door slams closed! ",door);
														}
														else{
															if(door.seen || player.DistanceFrom(door) <= 12){
																B.Add("You hear a door slamming. ");
															}
														}
														door.Toggle(null);
													}
													else{
														if(player.CanSee(door)){
															B.Add("The door slams closed on " + door.actor().TheName(true) + "! ",door);
														}
														else{
															if(player.DistanceFrom(door) <= 12){
																B.Add("You hear a door slamming and a grunt of pain. ");
															}
														}
														door.actor().TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"a slamming door");
													}
												}
											}
											else{
												B.Add("You hear mocking laughter from nearby. ");
											}
										}
									}
								}
							}
						}
						if(!manifested){
							Q.Add(new Event(target,area,(R.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,value+1,""));
						}
					}
					else{
						Q.Add(new Event(target,area,(R.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,""));
					}
					break;
				}
				case EventType.MIMIC:
				{
					Item item = target as Item;
					if(area[0].inv != item){ //it could have been picked up by the player or moved in another way
						foreach(Tile t in M.AllTiles()){ //if it was moved, make the correction to the event's area.
							if(t.inv == item){
								area = new List<Tile>{t};
								break;
							}
						}
					}
					if(area[0].inv == item){
						bool attacked = false;
						if(player.DistanceFrom(area[0]) == 1 && area[0].actor() == null){
							if(player.TotalSkill(SkillType.STEALTH) * 5 < R.Roll(1,100)){
								B.Add(item.TheName(true) + " suddenly grows tentacles! ");
								attacked = true;
								area[0].inv = null;
								Actor a = Actor.Create(ActorType.MIMIC,area[0].row,area[0].col,TiebreakerAssignment.UseCurrent);
								Q.KillEvents(a,EventType.MOVE);
								a.Q0();
								a.player_visibility_duration = -1;
								a.symbol = item.symbol;
								a.color = item.color;
							}
						}
						if(!attacked){
							Q.Add(new Event(target,area,100,EventType.MIMIC,AttrType.NO_ATTR,0,""));
						}
					}
					else{ //if the item is missing, we assume that the player just picked it up
						List<Tile> open = new List<Tile>();
						foreach(Tile t in player.TilesAtDistance(1)){
							if(t.passable && t.actor() == null){
								open.Add(t);
							}
						}
						if(open.Count > 0){
							Tile t = open.Random();
							B.Add(item.TheName() + " suddenly grows tentacles! ");
							Actor a = Actor.Create(ActorType.MIMIC,t.row,t.col,TiebreakerAssignment.UseCurrent);
							Q.KillEvents(a,EventType.MOVE);
							a.Q0();
							a.player_visibility_duration = -1;
							a.symbol = item.symbol;
							a.color = item.color;
							player.inv.Remove(item);
						}
						else{
							B.Add("Your pack feels lighter. ");
							player.inv.Remove(item);
						}
					}
					break;
				}
				case EventType.GRENADE:
				{
					Tile t = target as Tile;
					if(t.Is(FeatureType.GRENADE)){
						t.features.Remove(FeatureType.GRENADE);
						B.Add("The grenade explodes! ",t);
						if(t.seen){
							Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
						}
						B.DisplayNow();
						t.ApplyExplosion(1,"an exploding grenade");
						/*List<pos> cells = new List<pos>();
						foreach(Tile tile in t.TilesWithinDistance(1)){
							if(tile.passable && tile.seen){ //animation LOS check here
								cells.Add(tile.p);
							}
						}
						Screen.AnimateMapCells(cells,new colorchar('*',Color.RandomExplosion));
						Actor a = t.actor();
						if(a != null){
							a.attrs[AttrType.TURN_INTO_CORPSE] = 1;
						}
						foreach(Actor a2 in t.ActorsWithinDistance(1)){
							a2.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),null,"an exploding grenade");
						}
						if(a != null){
							int dir = Global.RandomDirection();
							if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
								t.TileInDirection(dir).KnockObjectBack(a,1);
							}
							a.CorpseCleanup();
						}
						t.MakeNoise(8);*/
					}
					break;
				}
				case EventType.BLAST_FUNGUS:
				{
					Item i = target as Item;
					i.other_data--;
					if(i.other_data == 0){
						Tile t = null;
						if(U.BoundsCheck(i.row,i.col) && M.tile[i.p].inv == i){
							t = M.tile[i.p];
							t.inv = null;
						}
						else{
							foreach(Actor a in M.AllActors()){
								if(a.inv.Contains(i)){
									a.inv.Remove(i);
									t = a.tile();
									break;
								}
							}
						}
						if(t != null){
							B.Add("The blast fungus explodes! ",t);
							if(t.seen){
								Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
							}
							B.DisplayNow();
							t.ApplyExplosion(3,"an exploding blast fungus");
						}
					}
					else{
						Tile t = null;
						if(U.BoundsCheck(i.row,i.col) && M.tile[i.p].inv == i){
							t = M.tile[i.p];
						}
						else{
							foreach(Actor a in M.AllActors()){
								if(a.inv.Contains(i)){
									t = a.tile();
									break;
								}
							}
						}
						if(t != null && t.seen){
							Screen.AnimateMapCell(t.row,t.col,new colorchar(i.other_data.ToString()[0],Color.Red),100);
						}
						Q.Add(new Event(i,100,EventType.BLAST_FUNGUS));
					}
					break;
				}
				case EventType.STALAGMITE:
				{
					if(value > 1){
						int stalagmites = 0; //number removed
						int number_left = 0;
						List<Tile> crumbled = new List<Tile>();
						foreach(Tile tile in area){
							if(tile.type == TileType.STALAGMITE){
								if(R.OneIn(value)){
									crumbled.Add(tile);
									tile.Toggle(null);
									++stalagmites;
								}
								else{
									++number_left;
								}
							}
						}
						if(stalagmites > 0){
							if(stalagmites > 1){
								B.Add("The stalagmites crumble. ",crumbled.ToArray());
							}
							else{
								B.Add("The stalagmite crumbles. ",crumbled.ToArray());
							}
						}
						if(number_left > 0){
							Q.Add(new Event(area,100,EventType.STALAGMITE,value));
						}
					}
					else{
						int stalagmites = 0;
						foreach(Tile tile in area){
							if(tile.type == TileType.STALAGMITE){
								stalagmites++;
							}
						}
						if(stalagmites > 0){
							if(stalagmites > 1){
								B.Add("The stalagmites crumble. ",area.ToArray());
							}
							else{
								B.Add("The stalagmite crumbles. ",area.ToArray());
							}
							foreach(Tile tile in area){
								if(tile.type == TileType.STALAGMITE){
									tile.Toggle(null);
								}
							}
						}
					}
					break;
				}
				case EventType.FIRE_GEYSER:
				{
					int frequency = value / 10; //9-39
					int variance = value % 10; //0-9
					int variance_amount = (frequency * variance) / 10;
					int number_of_values = variance_amount*2 + 1;
					int minimum_value = frequency - variance_amount;
					if(minimum_value < 5){
						int diff = 5 - minimum_value;
						number_of_values -= diff;
						minimum_value = 5;
					}
					int delay = ((minimum_value - 1) + R.Roll(number_of_values)) * 100;
					Q.Add(new Event(target,delay+200,EventType.FIRE_GEYSER,value));
					Q.Add(new Event(target,delay,EventType.FIRE_GEYSER_ERUPTION,2));
					break;
				}
				case EventType.FIRE_GEYSER_ERUPTION:
				{
					foreach(Tile t in target.TilesWithinDistance(2)){
						t.RemoveFeature(FeatureType.FOG);
					}
					//int old_radius = target.light_radius;
					//target.UpdateRadius(old_radius,2,true);
					B.Add(target.the_name + " spouts flames! ",target);
					if(target.actor() != null){
						target.actor().ApplyBurning();
					}
					for(int i=0;i<4;++i){
						Tile t = target.TilesWithinDistance(2).Where(x=>target.HasLOE(x)).Random();
						if(t != null){
							if(t.passable){
								t.AddFeature(FeatureType.FIRE);
							}
							else{
								t.ApplyEffect(DamageType.FIRE);
							}
						}
					}
					//target.UpdateRadius(2,old_radius,true);
					if(value > 0){
						Q.Add(new Event(target,100,EventType.FIRE_GEYSER_ERUPTION,value - 1));
					}
					break;
				}
				case EventType.FOG_VENT:
				{
					Tile current = target as Tile;
					if(!current.Is(FeatureType.FOG)){
						current.AddFeature(FeatureType.FOG);
						List<Tile> new_area = new List<Tile>{current};
						Q.RemoveTilesFromEventAreas(new_area,EventType.REMOVE_GAS);
						Event.RemoveGas(new_area,600,FeatureType.FOG,25);
						//Q.Add(new Event(new_area,600,EventType.FOG,25));
					}
					else{
						for(int tries=0;tries<50;++tries){
							List<Tile> open = new List<Tile>();
							foreach(Tile t in current.TilesAtDistance(1)){ //perhaps the rework could involve refreshing the duration of nearby tiles - if enough are refreshed, then no new tiles need to be added
								if(t.passable){
									open.Add(t);
									if(!t.Is(FeatureType.FOG)){
										open.Add(t); //3x as likely if it can expand there
										open.Add(t);
									}
								}
							}
							if(open.Count > 0){
								Tile possible = open.Random();
								if(!possible.Is(FeatureType.FOG)){
									possible.AddFeature(FeatureType.FOG);
									List<Tile> new_area = new List<Tile>{possible};
									Q.RemoveTilesFromEventAreas(new_area,EventType.REMOVE_GAS);
									Event.RemoveGas(new_area,600,FeatureType.FOG,25);
									break;
								}
								else{
									current = possible;
								}
							}
							else{
								break;
							}
						}
					}
					Q.Add(new Event(target,100,EventType.FOG_VENT));
					break;
				}
				case EventType.POISON_GAS_VENT:
				{
					Tile current = target as Tile;
					if(R.OneIn(7)){
						int num = R.Roll(5) + 2;
						List<Tile> new_area = new List<Tile>();
						for(int i=0;i<num;++i){
							if(!current.Is(FeatureType.POISON_GAS)){
								current.AddFeature(FeatureType.POISON_GAS);
								new_area.Add(current);
							}
							else{
								for(int tries=0;tries<50;++tries){
									List<Tile> open = new List<Tile>();
									foreach(Tile t in current.TilesAtDistance(1)){
										if(t.passable){
											open.Add(t);
										}
									}
									if(open.Count > 0){
										Tile possible = open.Random();
										if(!possible.Is(FeatureType.POISON_GAS)){
											possible.AddFeature(FeatureType.POISON_GAS);
											new_area.Add(possible);
											break;
										}
										else{
											current = possible;
										}
									}
									else{
										break;
									}
								}
							}
						}
						if(new_area.Count > 0){
							B.Add("Toxic vapors pour from " + target.the_name + "! ",target);
							Event.RemoveGas(new_area,200,FeatureType.POISON_GAS,18);
						}
					}
					Q.Add(new Event(target,100,EventType.POISON_GAS_VENT));
					break;
				}
				case EventType.STONE_SLAB:
				{
					Tile t = target as Tile;
					if(t.type == TileType.STONE_SLAB && (t.IsLitFromAnywhere(true) || area.Any(x=>x.actor()!=null))){
						bool vis = player.CanSee(t);
						t.Toggle(null);
						//t.Toggle(null,TileType.FLOOR);
						//t.symbol = '-';
						//t.revealed_by_light = true;
						if(!vis && player.CanSee(t)){
							vis = true;
						}
						if(vis){
							B.Add("The stone slab rises with a grinding sound. ");
						}
						else{
							if(player.DistanceFrom(t) <= 6){
								B.Add("You hear a grinding sound. ");
							}
						}
					}
					else{
						if(t.type == TileType.STONE_SLAB_OPEN && !t.IsLitFromAnywhere(true) && t.actor() == null && !area.Any(x=>x.actor()!=null)){
							bool vis = player.CanSee(t);
							//t.Toggle(null,TileType.STONE_SLAB);
							t.Toggle(null);
							if(!vis && player.CanSee(t)){
								vis = true;
							}
							if(vis){
								B.Add("The stone slab descends with a grinding sound. ");
							}
							else{
								if(player.DistanceFrom(t) <= 6){
									B.Add("You hear a grinding sound. ");
								}
							}
						}
					}
					Q.Add(new Event(target,area,100,EventType.STONE_SLAB));
					break;
				}
				case EventType.MARBLE_HORROR:
				{
					Tile t = target as Tile;
					if(t.type == TileType.STATUE){
						if(value == 1 && player.CanSee(t) && !t.IsLit() && t.actor() == null){ //if target was visible last turn & this turn, and it's currently in darkness...
							t.TransformTo(TileType.FLOOR);
							Actor a = Actor.Create(ActorType.MARBLE_HORROR,t.row,t.col,TiebreakerAssignment.AtEnd); //todo: not sure - should this get a placeholder like poltergeist and mimic?
							foreach(Event e in Q.list){
								if(e.target == a && e.type == EventType.MOVE){
									e.dead = true;
									break;
								}
							}
							a.Q0();
							switch(R.Roll(2)){
							case 1:
								B.Add("You think that statue might have just moved... ");
								B.Print(true);
								break;
							case 2:
								B.Add("The statue turns its head to face you. ");
								B.Print(true);
								break;
							}
						}
						else{
							if(player.CanSee(t)){
								Q.Add(new Event(target,100,EventType.MARBLE_HORROR,1));
							}
							else{
								Q.Add(new Event(target,100,EventType.MARBLE_HORROR,0));
							}
						}
					}
					break;
				}
				case EventType.REGENERATING_FROM_DEATH:
				{
					int health = value;
					int permanent_damage = secondary_value;
					if(target.tile().Is(FeatureType.TROLL_CORPSE)){ //otherwise, assume it was destroyed by fire
						int maxhp = Actor.Prototype(ActorType.TROLL).maxhp;
						int recovered = Actor.Prototype(ActorType.TROLL).attrs[AttrType.REGENERATING];
						if(health + recovered > maxhp - permanent_damage){
							recovered = (maxhp - permanent_damage) - health;
						}
						health += recovered;
						if(permanent_damage >= maxhp){
							break;
						}
						if(health > 0 && target.actor() == null){
							Actor a = Actor.Create(ActorType.TROLL,target.row,target.col,TiebreakerAssignment.UseCurrent);
							a.curhp = health;
							a.attrs[AttrType.PERMANENT_DAMAGE] = permanent_damage;
							a.attrs[AttrType.NO_ITEM]++;
							a.attrs[AttrType.DANGER_SENSED]++;
							B.Add("The troll stands up! ",target);
							a.player_visibility_duration = -1;
							if(target.tile().type == TileType.DOOR_C){
								target.tile().Toggle(a);
							}
							target.tile().features.Remove(FeatureType.TROLL_CORPSE);
							a.attrs[AttrType.WANDERING]++;
						}
						else{
							int roll = R.Roll(20);
							if(health == -1){
								roll = 1;
							}
							if(health == 0){
								roll = 3;
							}
							switch(roll){
							case 1:
							case 2:
								B.Add("The troll's corpse twitches. ",target);
								break;
							case 3:
							case 4:
								B.Add("You hear sounds coming from the troll's corpse. ",target);
								break;
							case 5:
								B.Add("The troll on the floor regenerates. ",target);
								break;
							default:
								break;
							}
							Event e = new Event(target,100,EventType.REGENERATING_FROM_DEATH);
							e.value = health;
							e.secondary_value = permanent_damage;
							Q.Add(e);
						}
					}
					if(target.tile().Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){ //otherwise, assume it was destroyed by fire
						int maxhp = Actor.Prototype(ActorType.TROLL_BLOODWITCH).maxhp;
						int recovered = Actor.Prototype(ActorType.TROLL_BLOODWITCH).attrs[AttrType.REGENERATING];
						if(health + recovered > maxhp - permanent_damage){
							recovered = (maxhp - permanent_damage) - health;
						}
						health += recovered;
						if(permanent_damage >= maxhp){
							break;
						}
						if(recovered > 0){
							List<pos> cells = new List<pos>();
							List<colorchar> cch = new List<colorchar>();
							foreach(pos p2 in target.PositionsWithinDistance(4)){
								if(target.HasLOE(M.tile[p2]) && player.CanSee(M.tile[p2])){
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
							foreach(Actor a in target.ActorsWithinDistance(4)){
								if(target.HasLOE(a)){
									if(a == player){
										B.Add("Ow! ");
									}
									a.TakeDamage(DamageType.NORMAL,DamageClass.MAGICAL,recovered,null,"trollish blood magic");
								}
							}
						}
						if(health > 0 && target.actor() == null){
							Actor a = Actor.Create(ActorType.TROLL_BLOODWITCH,target.row,target.col,TiebreakerAssignment.UseCurrent);
							a.curhp = health;
							a.attrs[AttrType.PERMANENT_DAMAGE] = permanent_damage;
							a.attrs[AttrType.NO_ITEM]++;
							a.attrs[AttrType.DANGER_SENSED]++;
							B.Add("The troll bloodwitch rises! ",target);
							a.player_visibility_duration = -1;
							if(attr == AttrType.COOLDOWN_1){
								a.attrs[AttrType.COOLDOWN_1]++;
							}
							if(target.tile().type == TileType.DOOR_C){
								target.tile().Toggle(a);
							}
							target.tile().features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
							a.attrs[AttrType.WANDERING]++;
						}
						else{
							int roll = R.Roll(20);
							if(health == -1){
								roll = 1;
							}
							if(health == 0){
								roll = 3;
							}
							switch(roll){
							case 1:
							case 2:
								B.Add("The bloodwitch's corpse twitches. ",target);
								break;
							case 3:
							case 4:
								B.Add("You feel a pulse like a heartbeat coming from the bloodwitch. ",target);
								break;
							case 5:
								B.Add("The troll bloodwitch on the floor regenerates. ",target);
								break;
							default:
								break;
							}
							Event e = new Event(target,100,EventType.REGENERATING_FROM_DEATH);
							e.value = health;
							e.secondary_value = permanent_damage;
							Q.Add(e);
						}
					}
					break;
				}
				case EventType.REASSEMBLING:
				{
					Tile t = target as Tile;
					if(t.Is(FeatureType.BONES)){
						if(t.actor() == null){
							Actor a = Actor.Create(ActorType.SKELETON,target.row,target.col,TiebreakerAssignment.UseCurrent);
							B.Add("The skeleton reassembles itself. ",target);
							a.player_visibility_duration = -1;
							if(target.tile().type == TileType.DOOR_C){
								target.tile().Toggle(a);
							}
							target.tile().features.Remove(FeatureType.BONES);
							if(R.OneIn(3)){
								a.attrs[AttrType.WANDERING]++;
							}
						}
						else{
							Q.Add(new Event(target,100,EventType.REASSEMBLING));
						}
					}
					break;
				}
				case EventType.SHIELDING:
				{
					List<pos> cells = new List<pos>();
					List<colorchar> symbols = new List<colorchar>();
					int animation_delay = 75;
					foreach(Tile tile in area){
						colorchar cch = tile.visual;
						if(tile.actor() != null){
							if(!tile.actor().HasAttr(AttrType.SHIELDED)){
								tile.actor().attrs[AttrType.SHIELDED] = 1;
								B.Add(tile.actor().YouAre() + " shielded. ",tile.actor());
							}
							if(player.CanSee(tile.actor())){
								animation_delay = 150;
								cch = tile.actor().visual;
							}
						}
						cch.bgcolor = Color.Blue;
						if(Global.LINUX && !Screen.GLMode){
							cch.bgcolor = Color.DarkBlue;
						}
						if(cch.color == cch.bgcolor){
							cch.color = Color.Black;
						}
						if(cch.c == '.'){
							cch.c = '+';
						}
						symbols.Add(cch);
						cells.Add(tile.p);
					}
					M.Draw();
					Screen.AnimateMapCells(cells,symbols,animation_delay);
					--value;
					if(value > 0){
						Q.Add(new Event(area,100,EventType.SHIELDING,value));
					}
					break;
				}
				case EventType.FINAL_LEVEL_SPAWN_CULTISTS:
				{
					int num_cultists = M.AllActors().Where(x=>x.Is(ActorType.FINAL_LEVEL_CULTIST)).Count;
					if(num_cultists < 5){
						Actor a = M.SpawnMob(ActorType.CULTIST);
						if(a != null){
							List<Actor> group = null;
							if(a.group != null){
								group = new List<Actor>(a.group);
								a.group.Clear();
							}
							else{
								group = new List<Actor>{a};
							}
							List<int> valid_circles = new List<int>();
							for(int i=0;i<5;++i){
								if(M.FinalLevelSummoningCircle(i).PositionsWithinDistance(2).Any(x=>M.tile[x].Is(TileType.DEMONIC_IDOL))){
									valid_circles.Add(i);
								}
							}
							foreach(Actor a2 in group){
								int i = valid_circles.RemoveLast();
								pos circle = M.FinalLevelSummoningCircle(i);
								a2.FindPath(circle.row,circle.col);
								a2.attrs[AttrType.COOLDOWN_2] = i;
								a2.type = ActorType.FINAL_LEVEL_CULTIST;
								a2.group = null;
								if(!R.OneIn(20)){
									a2.attrs[AttrType.NO_ITEM] = 1;
								}
							}
						}
					}
					Q.Add(new Event(R.Between(5,8)*100,EventType.FINAL_LEVEL_SPAWN_CULTISTS));
					break;
				}
				/*case EventType.BOSS_SIGN:
				{
					string s = "";
					switch(R.Roll(8)){
					case 1:
						s = "You see scratch marks on the walls and floor. ";
						break;
					case 2:
						s = "There are deep gouges in the floor here. ";
						break;
					case 3:
						s = "The floor here is scorched and blackened. ";
						break;
					case 4:
						s = "You notice bones of an unknown sort on the floor. ";
						break;
					case 5:
						s = "You hear a distant roar. ";
						break;
					case 6:
						s = "You smell smoke. ";
						break;
					case 7:
						s = "You spot a large reddish scale on the floor. ";
						break;
					case 8:
						s = "A small tremor shakes the area. ";
						break;
					default:
						s = "Debug message. ";
						break;
					}
					if(!player.HasAttr(AttrType.RESTING)){
						B.AddIfEmpty(s);
					}
					Q.Add(new Event((R.Roll(20)+35)*100,EventType.BOSS_SIGN));
					break;
				}
				case EventType.BOSS_ARRIVE:
				{
					bool spawned = false;
					Actor a = null;
					if(M.AllActors().Count == 1 && !Q.Contains(EventType.POLTERGEIST)){
						List<Tile> trolls = new List<Tile>();
						for(LinkedListNode<Event> current = Q.list.First;current!=null;current = current.Next){
							if(current.Value.type == EventType.REGENERATING_FROM_DEATH){
								trolls.Add((current.Value.target) as Tile);
							}
						}
						foreach(Tile troll in trolls){
							if(troll.Is(FeatureType.TROLL_CORPSE)){
								B.Add("The troll corpse burns to ashes! ",troll);
								troll.features.Remove(FeatureType.TROLL_CORPSE);
							}
							else{
								if(troll.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
									B.Add("The troll bloodwitch corpse burns to ashes! ",troll);
									troll.features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
								}
							}
						}
						Q.KillEvents(null,EventType.REGENERATING_FROM_DEATH);
						List<Tile> goodtiles = M.AllTiles();
						List<Tile> removed = new List<Tile>();
						foreach(Tile t in goodtiles){
							if(!t.passable || t.Is(TileType.CHASM) || player.CanSee(t)){
								removed.Add(t);
							}
						}
						foreach(Tile t in removed){
							goodtiles.Remove(t);
						}
						if(goodtiles.Count > 0){
							B.Add("You hear a loud crash and a nearby roar! ");
							Tile t = goodtiles[R.Roll(goodtiles.Count)-1];
							a = Actor.Create(ActorType.FIRE_DRAKE,t.row,t.col,true,false);
							spawned = true;
						}
						else{
							if(M.AllTiles().Any(t=>t.passable && !t.Is(TileType.CHASM) && t.actor() == null)){
								B.Add("You hear a loud crash and a nearby roar! ");
								Tile tile = M.AllTiles().Where(t=>t.passable && !t.Is(TileType.CHASM) && t.actor() == null).Random();
								a = Actor.Create(ActorType.FIRE_DRAKE,tile.row,tile.col,true,false);
								spawned = true;
							}
						}
					}
					if(!spawned){
						Q.Add(new Event(null,null,(R.Roll(20)+10)*100,EventType.BOSS_ARRIVE,attr,value,""));
					}
					else{
						if(value > 0){
							a.curhp = value;
						}
						else{ //if there's no good value, this means that this is the first appearance.
							B.Add("The ground shakes as dust and rocks fall from the cavern ceiling. ");
							B.Add("This place is falling apart! ");
							List<Tile> floors = M.AllTiles().Where(t=>t.passable && t.type != TileType.CHASM && player.tile() != t);
							Tile tile = null;
							if(floors.Count > 0){
								tile = floors.Random();
								(tile as Tile).Toggle(null,TileType.CHASM);
							}
							Q.Add(new Event(tile,100,EventType.FLOOR_COLLAPSE));
							Q.Add(new Event((R.Roll(20)+20)*100,EventType.CEILING_COLLAPSE));

						}
					}
					break;
				}
				case EventType.FLOOR_COLLAPSE:
				{
					Tile current = target as Tile;
					int tries = 0;
					if(current != null){
						for(tries=0;tries<50;++tries){
							List<Tile> open = new List<Tile>();
							foreach(Tile t in current.TilesAtDistance(1)){
								if(t.passable || t.Is(TileType.RUBBLE)){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								Tile possible = open.Random();
								if(!possible.Is(TileType.CHASM)){
									possible.Toggle(null,TileType.CHASM);
									List<Tile> open_neighbors = possible.TilesAtDistance(1).Where(t=>t.passable && t.type != TileType.CHASM);
									int num_neighbors = open_neighbors.Count;
									while(open_neighbors.Count > num_neighbors/2){
										Tile neighbor = open_neighbors.RemoveRandom();
										neighbor.Toggle(null,TileType.CHASM);
									}
									break;
								}
								else{
									current = possible;
								}
							}
							else{
								break;
							}
						}
					}
					if(tries == 50 || current == null){
						List<Tile> floors = M.AllTiles().Where(t=>t.passable && t.type != TileType.CHASM && player.tile() != t);
						if(floors.Count > 0){
							target = floors.Random();
							(target as Tile).Toggle(null,TileType.CHASM);
						}
					}
					Q.Add(new Event(target,100,EventType.FLOOR_COLLAPSE));
					break;
				}
				case EventType.CEILING_COLLAPSE:
				{
					B.Add("The ground shakes and debris falls from the ceiling! ");
					for(int i=1;i<Global.ROWS-1;++i){
						for(int j=1;j<Global.COLS-1;++j){
							Tile t = M.tile[i,j];
							if(t.Is(TileType.WALL)){
								int num_walls = t.TilesAtDistance(1).Where(x=>x.Is(TileType.WALL)).Count;
								if(num_walls < 8 && R.OneIn(20)){
									if(R.CoinFlip()){
										t.Toggle(null,TileType.FLOOR);
										foreach(Tile neighbor in t.TilesAtDistance(1)){
											neighbor.solid_rock = false;
										}
									}
									else{
										t.Toggle(null,TileType.RUBBLE);
										foreach(Tile neighbor in t.TilesAtDistance(1)){
											neighbor.solid_rock = false;
											if(neighbor.type == TileType.FLOOR && R.OneIn(10)){
												neighbor.Toggle(null,TileType.RUBBLE);
											}
										}
									}
								}
							}
							else{
								int num_walls = t.TilesAtDistance(1).Where(x=>x.Is(TileType.WALL)).Count;
								if(num_walls == 0 && R.OneIn(100)){
									if(R.OneIn(6)){
										t.Toggle(null,TileType.RUBBLE);
									}
									foreach(Tile neighbor in t.TilesAtDistance(1)){
										if(neighbor.type == TileType.FLOOR && R.OneIn(6)){
											neighbor.Toggle(null,TileType.RUBBLE);
										}
									}
								}
							}
						}
					}
					Q.Add(new Event((R.Roll(20)+20)*100,EventType.CEILING_COLLAPSE));
					break;
				}*/
				case EventType.NORMAL_LIGHTING:
				{
					bool check_for_torch_dimming = false;
					if(M.wiz_lite){
						B.Add("The supernatural brightness fades from the air. ");
					}
					if(M.wiz_dark){
						B.Add("The supernatural darkness fades from the air. ");
						check_for_torch_dimming = true;
					}
					M.wiz_lite = false;
					M.wiz_dark = false;
					if(check_for_torch_dimming && player.HasAttr(AttrType.DIM_LIGHT)){
						player.CalculateDimming();
					}
					break;
				}
				case EventType.TELEPORTAL:
				{
					Tile t = target as Tile;
					if(t != null && t.Is(FeatureType.TELEPORTAL,FeatureType.STABLE_TELEPORTAL)){
						if(t.Is(FeatureType.TELEPORTAL)){
							value--; //unstable teleportals (from the item) degrade each turn
						}
						else{
							if(value < 100){
								value++; //stable ones repair themselves after use
							}
						}
						Actor a = t.actor();
						Tile dest = null;
						if(a != null && !a.HasAttr(AttrType.JUST_TELEPORTED,AttrType.IMMOBILE)){
							if(area != null){
								dest = area.Random();
							}
							else{
								List<Tile> tiles = M.AllTiles().Where(x => x.passable && x.actor() == null && t.ApproximateEuclideanDistanceFromX10(x) >= 45);
								dest = tiles.Random();
							}
							if(dest != null){
								a.RefreshDuration(AttrType.JUST_TELEPORTED,101);
								value -= 25;
								bool visible = false;
								if(a == player){
									B.Add("You disappear into the teleportal. ");
								}
								else{
									if(player.CanSee(a)){
										visible = true;
										B.Add(a.the_name + " disappears into the teleportal. ",t);
									}
								}
								a.Move(dest.row,dest.col);
								if(a != player && player.CanSee(a)){
									if(visible){
										B.Add(a.the_name + " reappears. ",a);
									}
									else{
										B.Add(a.a_name + " suddenly appears! ",a);
									}
								}
							}
						}
						else{
							if(a != null && a.HasAttr(AttrType.JUST_TELEPORTED)){
								a.RefreshDuration(AttrType.JUST_TELEPORTED,101);
							}
						}
						if(t.inv != null && t.Is(FeatureType.TELEPORTAL)){
							List<Tile> tiles = M.AllTiles().Where(x => x.passable && x.inv == null && t.ApproximateEuclideanDistanceFromX10(x) >= 45);
							dest = tiles.Random();
							if(dest != null){
								Item i = t.inv;
								bool visible = false;
								if(player.CanSee(t)){
									visible = true;
									B.Add(i.TheName(true) + " disappears into the teleportal. ",t);
								}
								t.inv = null;
								dest.GetItem(i);
								if(player.CanSee(dest)){
									if(visible){
										B.Add(i.TheName(true) + " reappears. ",dest);
									}
									else{
										B.Add(i.AName(true) + " suddenly appears! ",dest);
									}
								}
							}
						}
						if(value > 0){
							Q.Add(new Event(target,area,100,EventType.TELEPORTAL,AttrType.NO_ATTR,value,""));
							if(value < 25){
								if(dest != null || R.OneIn(8)){
									B.Add("The teleportal flickers. ",t,dest);
								}
							}
						}
						else{
							if(t.Is(FeatureType.TELEPORTAL)){
								t.RemoveFeature(FeatureType.TELEPORTAL);
							}
							if(t.Is(FeatureType.STABLE_TELEPORTAL)){
								foreach(Tile t2 in area){
									Event e2 = Q.FindTargetedEvent(t2,EventType.TELEPORTAL);
									if(e2 != null && t2.features.Contains(FeatureType.STABLE_TELEPORTAL)){
										e2.area.Remove(t);
										if(e2.area.Count == 0){
											t2.RemoveFeature(FeatureType.STABLE_TELEPORTAL);
											//t2.AddFeature(FeatureType.INACTIVE_TELEPORTAL);
											e2.dead = true;
										}
									}
								}
								t.RemoveFeature(FeatureType.STABLE_TELEPORTAL);
							}
							B.Add("The teleportal flickers and vanishes. ",t,dest);
						}
					}
					break;
				}
				case EventType.BREACH:
				{
					if(!R.OneIn(3)){
						Tile t = area.WhereGreatest(x=>x.DistanceFrom(target)).Random();
						if(t != null){
							t.Toggle(null);
							if(t.actor() != null || t.inv != null){
								foreach(Tile nearby in M.ReachableTilesByDistance(t.row,t.col,false)){
									if(t.inv != null && nearby.inv == null){
										nearby.GetItem(t.inv);
										t.inv = null;
										if(t.actor() == null){
											break;
										}
									}
									if(t.actor() != null && nearby.actor() == null){
										t.actor().Move(nearby.row,nearby.col);
										if(t.inv == null){
											break;
										}
									}
								}
								if(t.actor() != null){ //if there wasn't an actual path to a passable tile, just move to the nearest
									for(int i=1;i<Math.Max(Global.ROWS,Global.COLS);++i){
										List<Tile> tiles = t.TilesAtDistance(i).Where(x=>x.passable && x.actor() == null);
										bool done = false;
										while(tiles.Count > 0){
											Tile dest = tiles.Random();
											t.actor().Move(dest.row,dest.col);
											done = true;
											break;
										}
										if(done){
											break;
										}
									}
								}
								if(t.inv != null){
									for(int i=1;i<Math.Max(Global.ROWS,Global.COLS);++i){
										List<Tile> tiles = t.TilesAtDistance(i).Where(x=>x.passable && x.inv == null);
										bool done = false;
										while(tiles.Count > 0){
											Tile dest = tiles.Random();
											dest.GetItem(t.inv);
											t.inv = null;
											done = true;
											break;
										}
										if(done){
											break;
										}
									}
								}
							}
							if(t.features.Count > 0){
								t.features.Clear();
							}
							area.Remove(t);
						}
					}
					if(area.Count > 0){
						Q.Add(new Event(target,area,100,EventType.BREACH));
					}
					break;
				}
				case EventType.GRAVE_DIRT:
				{
					foreach(Tile t in area){
						Actor a = t.actor();
						if(a != null && a.type != ActorType.CORPSETOWER_BEHEMOTH && !a.HasAttr(AttrType.IMMOBILE,AttrType.JUST_GRABBED,AttrType.FROZEN,AttrType.FLYING) && R.OneIn(12)){
							if(player.CanSee(a)){
								B.Add("A dead hand reaches up and grabs " + a.the_name + "! ",t);
							}
							if(a == player){
								B.Print(true);
							}
							if(a.HasAttr(AttrType.SLIMED,AttrType.OIL_COVERED,AttrType.BRUTISH_STRENGTH)){
								if(player.CanSee(a)){
									B.Add(a.You("slip") + " out of its grasp. ",t);
								}
							}
							else{
								int duration = R.Roll(4) * 100;
								a.attrs[AttrType.IMMOBILE]++;
								Q.Add(new Event(a,duration,AttrType.IMMOBILE,"The dead hand releases " + a.TheName(true) + ". ",t)); //it'd be nice to check LOS here
								a.RefreshDuration(AttrType.JUST_GRABBED,duration + 100);
							}
						}
					}
					Q.Add(new Event(area,100,EventType.GRAVE_DIRT));
					break;
				}
				case EventType.TOMBSTONE_GHOST:
				{
					if(area.Count > 0){
						Tile t = area[0];
						if(target == null && t.actor() == player){
							foreach(Tile t2 in M.ReachableTilesByDistance(player.row,player.col,false)){
								if(t2.passable && t2.actor() == null){
									Actor ghost = Actor.Create(ActorType.GHOST,t2.row,t2.col);
									if(ghost != null){
										target = ghost;
										ghost.player_visibility_duration = -1;
										ghost.target = player;
										t.color = Color.White;
										B.Add("A vengeful ghost rises! ");
										B.PrintAll();
										break;
									}
								}
							}
						}
						Q.Add(new Event(target,area,100,EventType.TOMBSTONE_GHOST));
					}
					break;
				}
				case EventType.POPPIES:
				{
					List<Tile> new_area = new List<Tile>();
					bool recalculate_distance_map = false;
					foreach(Tile t in area){
						if(t.type == TileType.POPPY_FIELD){
							new_area.Add(t);
							Actor a = t.actor();
							if(a == player){
								Help.TutorialTip(TutorialTopic.Poppies);
							}
							if(a != null && !a.HasAttr(AttrType.NONLIVING,AttrType.PLANTLIKE)){
								if(a.attrs[AttrType.POPPY_COUNTER] < 4){
									a.GainAttrRefreshDuration(AttrType.POPPY_COUNTER,200);
									if(a == player && a.attrs[AttrType.POPPY_COUNTER] == 1){
										B.Add("You breathe in the overwhelming scent of the poppies. ");
									}
								}
								else{
									a.RefreshDuration(AttrType.POPPY_COUNTER,200);
								}
								if(a.attrs[AttrType.POPPY_COUNTER] >= 4){
									if(!a.HasAttr(AttrType.ASLEEP,AttrType.JUST_AWOKE)){
										if(a.ResistedBySpirit()){
											if(player.HasLOS(a)){
												B.Add(a.You("resist") + " falling asleep. ",a);
											}
										}
										else{
											if(player.HasLOS(a)){
												B.Add(a.You("fall") + " asleep in the poppies. ",a);
												//B.Add("The poppies lull " + a.the_name + " to sleep. ",a);
											}
											a.attrs[AttrType.ASLEEP] = R.Between(4,6);
										}
									}
									/*a.ApplyStatus(AttrType.MAGICAL_DROWSINESS,(R.Roll(3)+4)*100);
									if(a == player && !a.HasAttr(AttrType.MAGICAL_DROWSINESS)){
										//B.Add("The poppies make you drowsy. ");
										Help.TutorialTip(TutorialTopic.Drowsiness);
									}
									a.RefreshDuration(AttrType.MAGICAL_DROWSINESS,a.DurationOfMagicalEffect((R.Roll(3)+4)) * 100,a.YouFeel() + " less drowsy. ",a);*/
								}
							}
						}
						else{
							recalculate_distance_map = true;
						}
					}
					if(new_area.Count > 0){
						Q.Add(new Event(new_area,100,EventType.POPPIES));
						if(recalculate_distance_map){
							M.poppy_distance_map = M.tile.GetDijkstraMap(x=>!M.tile[x].Is(TileType.POPPY_FIELD),x=>M.tile[x].passable && !M.tile[x].Is(TileType.POPPY_FIELD));
						}
					}
					break;
				}
				case EventType.BURROWING:
				{
					List<Tile> open = area.Where(x=>x.passable && x.actor() == null);
					Actor a = target as Actor;
					if(open.Count > 0){
						Tile t = open.Random();
						Event e = new Event(a,100,EventType.MOVE);
						e.tiebreaker = this.tiebreaker;
						Q.Add(e);
						a.attrs[AttrType.BURROWING] = 0;
						a.Move(t.row,t.col);
						if(player.CanSee(a)){
							a.AnimateStorm(1,2,3,'*',Color.Gray);
						}
						B.Add(a.TheName(true) + " emerges from the ground. ",a,t);
					}
					else{
						if(a.HasAttr(AttrType.REGENERATING)){
							a.curhp += a.attrs[AttrType.REGENERATING];
							if(a.curhp > a.maxhp){
								a.curhp = a.maxhp;
							}
						}
						Q.Add(new Event(target,area,100,EventType.BURROWING));
					}
					break;
				}
				case EventType.SPAWN_WANDERING_MONSTER:
				{
					int spawn_chance = 2;
					foreach(Actor a in Actor.tiebreakers){
						if(a != player && a != null && !a.HasAttr(AttrType.IMMOBILE) && (a.group == null || a.group.Count == 0 || a.group[0] == a)){
							spawn_chance *= 2;
							if(spawn_chance >= 65536){
								break;
							}
						}
					}
					if(R.OneIn(spawn_chance)){
						if(M.extra_danger < 8 && R.CoinFlip() && M.current_level != 1){
							M.extra_danger++;
							B.Add("You sense danger. ");
						}
						Actor a = M.SpawnWanderingMob();
						if(a != null){
							a.attrs[AttrType.WANDERING] = 1;
							a.attrs[AttrType.NO_ITEM] = 1;
							if(player.CanSee(a)){
								B.Add("You suddenly sense the presence of " + a.AName(true) + ". ");
							}
						}
					}
					Q.Add(new Event(R.Between(20,60)*100,EventType.SPAWN_WANDERING_MONSTER));
					break;
				}
				/*case EventType.GAS_UPDATE:
				{
					int ROWS = Global.ROWS;
					int COLS = Global.COLS;
					float[,] g = null;
					for(int num=0;num<3;++num){
						g = new float[ROWS,COLS];
						for(int i=1;i<ROWS-1;++i){
							for(int j=1;j<COLS-1;++j){
								if(M.tile[i,j].passable){
									float neighbors_total = 0.0f;
									int open = 0;
									foreach(int dir in U.EightDirections){
										if(M.tile[i,j].TileInDirection(dir).passable){
											pos p = new pos(i,j).PosInDir(dir);
											neighbors_total += M.gas[p.row,p.col];
											++open;
										}
									}
									if(open > 0){
										float avg = neighbors_total / (float)open;
										float d = 0.03f * open;
										g[i,j] = M.gas[i,j] * (1-d) + avg * d;
									}
								}
							}
						}
						M.gas = g;
					}
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(g[i,j] > 0.0f){
								if(g[i,j] <= 0.001f){
									g[i,j] = 0.0f;
									M.tile[i,j].features.Remove(FeatureType.POISON_GAS);
								}
								else{
									g[i,j] -= 0.001f;// * (float)R.r.NextDouble();
									M.tile[i,j].features.AddUnique(FeatureType.POISON_GAS);
								}
							}
							else{
								M.tile[i,j].features.Remove(FeatureType.POISON_GAS);
							}
						}
					}
					Q.Add(new Event(100,EventType.GAS_UPDATE));
					break;
				}*/
				case EventType.FIRE:
				{
					List<Tile> chance_to_burn = new List<Tile>(); //tiles that might be affected
					List<Tile> chance_to_die_out = new List<Tile>(); //fires that might die out
					List<PhysicalObject> no_fire = new List<PhysicalObject>();
					foreach(PhysicalObject o in new List<PhysicalObject>(Fire.burning_objects)){
						if(o.IsBurning()){
							foreach(Tile neighbor in o.TilesWithinDistance(1)){
								if(neighbor.actor() != null && neighbor.actor() != o){
									if(neighbor.actor() == player){
										if(!player.HasAttr(AttrType.JUST_SEARED,AttrType.FROZEN,AttrType.DAMAGE_RESISTANCE)){
											B.Add("The heat sears you! ");
										}
										player.RefreshDuration(AttrType.JUST_SEARED,50);
									}
									neighbor.actor().TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,false,1,null,"searing heat");
								}
								//every actor adjacent to a burning object takes proximity fire damage. (actors never get set on
								//  fire directly this way, but an actor covered in oil will ignite if it takes any fire damage)
								//every tile adjacent to a burning object has a chance to be affected by fire. oil-covered objects are always affected.
								//if the roll is passed, fire is applied to the tile.
								chance_to_burn.AddUnique(neighbor);
							}
							if(o is Tile){
								chance_to_die_out.AddUnique(o as Tile);
							}
						}
						else{
							no_fire.AddUnique(o);
						}
					}
					foreach(Tile t in chance_to_burn){
						if(R.OneIn(6) || t.Is(FeatureType.OIL,FeatureType.SPORES,FeatureType.CONFUSION_GAS) || t.Is(TileType.BARREL)){
							t.ApplyEffect(DamageType.FIRE);
						}
					}
					foreach(Tile t in chance_to_die_out){
						if(!t.Is(TileType.BARREL)){
							bool more_flammable_terrain = false;
							bool more_fire = false;
							bool final_level_demonic_idol_present = false; //this will soon become a check for any terrain that prevents fires from dying
							foreach(Tile neighbor in t.TilesAtDistance(1)){
								if(neighbor.IsCurrentlyFlammable()){
									more_flammable_terrain = true;
								}
								if(neighbor.Is(TileType.DEMONIC_IDOL)){
									final_level_demonic_idol_present = true;
								}
								if(neighbor.IsBurning()){
									more_fire = true;
								}
							}
							if(final_level_demonic_idol_present){
								continue; //this fire never goes out
							}
							int chance = 5;
							if(more_fire){
								chance = 10;
							}
							if(more_flammable_terrain){
								chance = 20;
							}
							if(R.OneIn(chance)){
								t.RemoveFeature(FeatureType.FIRE);
								Fire.burning_objects.Remove(t);
								if(t.name == "floor" && t.type != TileType.BREACHED_WALL){
									if(R.OneIn(4)){
										t.color = Color.Gray;
									}
									else{
										t.color = Color.TerrainDarkGray;
									}
								}
							}
						}
					}
					foreach(PhysicalObject o in no_fire){
						Fire.burning_objects.Remove(o);
					}
					if(Fire.burning_objects.Count > 0){
						Event e = new Event(100,EventType.FIRE);
						Q.Add(e);
						Fire.fire_event = e;
					}
					else{
						Fire.fire_event = null;
					}
					break;
				}
				}
				if(msg != ""){
					if(msg_objs == null){
						B.Add(msg);
					}
					else{
						if(msg_objs.Count == 1 && msg_objs[0] is Actor && (msg_objs[0] as Actor).HasAttr(AttrType.BURROWING)){
							//do nothing
						}
						else{
							B.Add(msg,msg_objs.ToArray());
						}
					}
				}
			}
		}
		/*public static bool operator <(Event one,Event two){
			return one.TimeToExecute() < two.TimeToExecute();
		}
		public static bool operator >(Event one,Event two){
			return one.TimeToExecute() > two.TimeToExecute();
		}
		public static bool operator <=(Event one,Event two){
			return one.TimeToExecute() <= two.TimeToExecute();
		}
		public static bool operator >=(Event one,Event two){
			return one.TimeToExecute() >= two.TimeToExecute();
		}*/
		public static bool operator <(Event one,Event two){
			if(one.TimeToExecute() < two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() > two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker < two.tiebreaker){
				return true;
			}
			if(one.tiebreaker > two.tiebreaker){
				return false;
			}
			if(one.type == EventType.MOVE && two.type != EventType.MOVE){
				return true;
			}
			if(one.type == EventType.FIRE && two.type != EventType.MOVE && two.type != EventType.FIRE){
				return true;
			}
			return false;
		}
		public static bool operator >(Event one,Event two){ //currently unused
			if(one.TimeToExecute() > two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() < two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker > two.tiebreaker){
				return true;
			}
			if(one.tiebreaker < two.tiebreaker){
				return false;
			}
			if(two.type == EventType.MOVE && one.type != EventType.MOVE){
				return true;
			}
			if(two.type == EventType.FIRE && one.type != EventType.MOVE && one.type != EventType.FIRE){
				return true;
			}
			return false;
		}
		public static bool operator <=(Event one,Event two){ //currently unused
			if(one.TimeToExecute() < two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() > two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker < two.tiebreaker){
				return true;
			}
			if(one.tiebreaker > two.tiebreaker){
				return false;
			}
			if(one.type == EventType.MOVE){
				return true;
			}
			if(one.type == EventType.FIRE && two.type != EventType.MOVE){
				return true;
			}
			if(one.type != EventType.MOVE && two.type != EventType.MOVE && one.type != EventType.FIRE && two.type != EventType.FIRE){
				return true;
			}
			return false;
		}
		public static bool operator >=(Event one,Event two){
			if(one.TimeToExecute() > two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() < two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker > two.tiebreaker){
				return true;
			}
			if(one.tiebreaker < two.tiebreaker){
				return false;
			}
			if(two.type == EventType.MOVE){
				return true;
			}
			if(two.type == EventType.FIRE && one.type != EventType.MOVE){
				return true;
			}
			if(one.type != EventType.MOVE && two.type != EventType.MOVE && one.type != EventType.FIRE && two.type != EventType.FIRE){
				return true;
			}
			return false;
		}
	}
	public static class Fire{
		public static List<PhysicalObject> burning_objects = new List<PhysicalObject>();
		public static Event fire_event = null;
		public static void AddBurningObject(PhysicalObject o){
			if(fire_event == null){
				Event player_move = Event.Q.FindTargetedEvent(Event.player,EventType.MOVE);
				int fire_time = player_move.TimeToExecute();
				int remainder = fire_time % 100;
				if(remainder != 0){
					fire_time = (fire_time - remainder) + 100;
				}
				fire_event = new Event(fire_time - Event.Q.turn,EventType.FIRE);
				fire_event.tiebreaker = 0;
				Event.Q.Add(fire_event);
			}
			burning_objects.AddUnique(o);
		}
	}
}

