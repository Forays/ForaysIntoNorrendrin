/*Copyright (c) 2016  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
namespace Nym{
	public class Name { //todo: none of this code works on uppercase.
		public string Singular, Plural;
		public bool usesAn, uncountable, noArticles, secondPerson;

		//todo: convert to string?

		/// <param name="rawName">"Foo" is pluralized as "Foos".  "Pair~ of Foos" becomes "Pair of Foos" and is pluralized as "Pairs of Foos".
		/// "Foo Complex~~" is pluralized as "Foo Complexes". "Berry" is pluralized as "Berries".</param>
		/// <param name="exceptionToAAnRule">Default is to check for [AEIOU]. This bool is for exceptions to that rule.</param>
		/// <param name="uncountable">Marks uncountable nouns like "water", "courage", and "equipment". These names don't receive quantities or "a/an".</param>
		/// <param name="noArticles">Indistinct or unique names might not accept articles, like "something" or "Excalibur".</param>
		/// <param name="secondPerson">Probably used for the name "you", to work correctly with verbs.</param>
		public Name(string rawName, bool exceptionToAAnRule = false, bool uncountable = false, bool noArticles = false, bool secondPerson = false) {
			if(rawName.Contains("~")) {
				Singular = rawName.Replace("~","");
				Plural = rawName.Replace("~~","es");
				Plural = Plural.Replace("~","s");
			}
			else {
				Singular = rawName;
				if(rawName.EndsWith("y") && !rawName.EndsWith("ay") && !rawName.EndsWith("ey") && !rawName.EndsWith("oy") && !rawName.EndsWith("uy")) {
					Plural = rawName.Substring(0, rawName.Length - 1) + "ies";
				}
				else {
					if(rawName.EndsWith("sh") || rawName.EndsWith("ch") || rawName.EndsWith("s") || rawName.EndsWith("z") || rawName.EndsWith("x")) {
						Plural = rawName + "es";
					}
					else {
						Plural = rawName + "s";
					}
				}
			}
			this.uncountable = uncountable;
			this.noArticles = noArticles;
			this.secondPerson = secondPerson;
			SetAAn(Singular, exceptionToAAnRule);
		}
		/// <param name="singular">Define the EXACT string used for the singular name.</param>
		/// <param name="plural">Define the EXACT string used for the plural name.</param>
		/// <param name="exceptionToAAnRule">Default is to check for [AEIOU]. This bool is for exceptions to that rule.</param>
		/// <param name="uncountable">Marks uncountable nouns like "water", "courage", and "equipment". These names don't receive quantities or "a/an".</param>
		/// <param name="noArticles">Indistinct or unique names might not accept articles, like "something" or "Excalibur".</param>
		/// <param name="secondPerson">Probably used for the name "you", to work correctly with verbs.</param>
		public Name(string singular, string plural, bool exceptionToAAnRule = false, bool uncountable = false, bool noArticles = false, bool secondPerson = false) {
			this.Singular = singular;
			this.Plural = plural;
			this.uncountable = uncountable;
			this.noArticles = noArticles;
			this.secondPerson = secondPerson;
			SetAAn(singular,exceptionToAAnRule);
		}
		private void SetAAn(string singular, bool exceptionToRule) {
			if(singular.Length > 0) {
				if(singular[0] == 'a' || singular[0] == 'e' || singular[0] == 'i' || singular[0] == 'o' || singular[0] == 'u') {
					usesAn = true;
				}
			}
			if(exceptionToRule) usesAn = !usesAn;
		}
	}

	public interface INamed {
		Name Name { get; }
		int Quantity { get; }
		Func<string> GetExtraInfo { get; }
	}

	public class Named : INamed { //this class is for convenience; it isn't required for anything.
		public Name Name { get; set; }
		public int Quantity { get; set; }
		public Func<string> GetExtraInfo { get; set; }

		public Named(string rawName, int qty = 1, Func<string> getExtraInfo = null) : this(new Name(rawName), qty, getExtraInfo) { }
		public Named(Name name, int qty = 1, Func<string> getExtraInfo = null) {
			Name = name;
			Quantity = qty;
			GetExtraInfo = getExtraInfo;
		}
	}

	public static class NameExtensions {
		public static string IsAre(this INamed n) {
			if(n.Quantity != 1 || n.Name.secondPerson) return "are";
			else return "is";
		}
		public static string ThisThese(this INamed n) {
			if(n.Quantity != 1) return "these";
			else return "this";
		}
		public static string GetName(this INamed n, params NameElement[] elements) => n.Name.GetName(n.Quantity, n.GetExtraInfo, elements);
		public static string GetName(this Name name, params NameElement[] elements) => name.GetName(1,null,elements);
		public static string GetName(this Name name, int qty, Func<string> getExtraInfo, params NameElement[] elements) {
			string articleStr = null;
			string qtyStr = null;
			string nameStr = null;
			string extraStr = null;
			string verbStr = null; //verb or possessive, actually

			if(qty == 1 || name.uncountable) {
				nameStr = name.Singular;
			}
			else {
				nameStr = name.Plural;
				qtyStr = qty.ToString() + " ";
			}

			foreach(NameElement e in elements) {
				if(e.verb != null) {
					verbStr = " " + Verbs.Conjugate(e.verb, e.thirdPersonSingular, qty != 1, name.secondPerson);
					continue;
				}
				if(e == NameElement.The) {
					if(!name.noArticles) articleStr = "the ";
					continue;
				}
				if(e == NameElement.An) {
					if(!name.noArticles && qty == 1) {
						if(name.uncountable) articleStr = "some ";
						else articleStr = name.usesAn? "an " : "a ";
					}
					continue;
				}
				if(e == NameElement.Possessive) {
					if(name.secondPerson) verbStr = "r"; // this forms "your". Feels hacky but it works.
					else verbStr = "'s";
					continue;
				}
				if(e == NameElement.Plural) {
					nameStr = name.Plural;
					continue;
				}
				if(e == NameElement.Qty) {
					if(!name.uncountable) qtyStr = qty.ToString() + " ";
					continue;
				}
				if(e == NameElement.NoQty) {
					qtyStr = null;
					continue;
				}
				if(e == NameElement.Extra) {
					extraStr = getExtraInfo?.Invoke();
					continue;
				}
			}
			return articleStr + qtyStr + nameStr + extraStr + verbStr;
		}
	}

	public class NameElement {
		public string verb;
		public string thirdPersonSingular;
		/// <param name="verb">For example, "attack".</param>
		/// <param name="thirdPersonSingular">For example, "attacks".</param>
		public NameElement(string verb = null,string thirdPersonSingular = null) {
			this.verb = verb;
			this.thirdPersonSingular = thirdPersonSingular;
		}
		/// <summary></summary>
		public static NameElement Verb(string verb,string thirdPersonSingular = null) {
			return new NameElement(verb,thirdPersonSingular);
		}
		/// <summary></summary>
		public static readonly NameElement The = new NameElement();
		/// <summary></summary>
		public static readonly NameElement An = new NameElement();
		/// <summary></summary>
		public static readonly NameElement Possessive = new NameElement();
		/// <summary></summary>
		public static readonly NameElement Plural = new NameElement();
		/// <summary></summary>
		public static readonly NameElement Qty = new NameElement(); //todo, Qty is currently only good for showing "1". Remove or keep?
		/// <summary></summary>
		public static readonly NameElement NoQty = new NameElement();
		/// <summary></summary>
		public static readonly NameElement Extra = new NameElement();
		/// <summary></summary>
		public static readonly NameElement Are = new NameElement("are","is");
	}

	public static class Verbs {
		/// <param name="verb">For example, "attack".</param>
		/// <param name="thirdPersonSingular">For example, "attacks".</param>
		public static void Register(string verb, string thirdPersonSingular) {
			if(registered.ContainsKey(verb)) registered.Remove(verb);
			registered.Add(verb,thirdPersonSingular);
		}
		/// <param name="verb">For example, "attack".</param>
		/// <param name="thirdPersonSingular">For example, "attacks".</param>
		public static string Conjugate(string verb, string thirdPersonSingular, bool plural, bool secondPerson) {
			if(plural || secondPerson) return verb;
			else {
				if(registered.ContainsKey(verb)) return registered[verb];
				else {
					if(thirdPersonSingular != null) return thirdPersonSingular; // use this one if it has been provided
					else { // otherwise, use the default, which does *not* attempt to handle every single rule.
						if(verb.EndsWith("sh") || verb.EndsWith("ch") || verb.EndsWith("s") || verb.EndsWith("z") || verb.EndsWith("x")) {
							return verb + "es";
						}
						if(verb.EndsWith("y")) {
							if(!verb.EndsWith("ay") && !verb.EndsWith("ey") && !verb.EndsWith("oy") && !verb.EndsWith("uy")) {
								return verb.Substring(0,verb.Length - 1) + "ies";
							}
						}
						return verb + "s";
					}
				}
			}
		}
		private static Dictionary<string,string> registered = new Dictionary<string,string>();
		static Verbs(){
			Register("do", "does"); //todo, i'm sure there are lots more
		}
	}
}

