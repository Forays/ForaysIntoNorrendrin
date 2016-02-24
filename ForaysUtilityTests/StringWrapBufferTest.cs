using System;
using NUnit.Framework;
using ForaysUtilities;
using System.Collections.Generic;

namespace ForaysUtilityTests {
	[TestFixture] public class StringWrapBufferTests {
		[TestCase] public void ConstructorAndArguments() {
			Assert.Throws(typeof(ArgumentOutOfRangeException),() => { new StringWrapBuffer(5,0); }); //maxLength less than 1 throws
			StringWrapBuffer buffer = new StringWrapBuffer(-3,1,null,new char[0]);
			Assert.AreEqual(buffer.MaxLength,1);
			Assert.AreEqual(buffer.MaxLines,-3);
		}
		[TestCase] public void AddContentsAndClear() {
			var buffer = new StringWrapBuffer(4,7);
			Assert.AreEqual(buffer.Contents.Count,0); //0 strings after creation
			Assert.AreEqual(buffer.Clear().Count,0); //0 strings after clear
			buffer.Add(null);
			buffer.Add("");
			buffer.Add("   "); //leading discarded separators are removed from strings that begin a new line
			Assert.AreEqual(buffer.Contents.Count,0); //0 strings after empty adds
			buffer.Add("Hello. ");
			Assert.AreEqual(buffer.Contents.Count,1); //1 string after add
			Assert.AreEqual(buffer.Contents[0],"Hello. "); //first string matches input
			Assert.AreEqual(buffer.Clear()[0],"Hello. "); //first string of cleared contents matches input
			Assert.AreEqual(buffer.Contents.Count,0); //0 strings after clear

		}
		[TestCase] public void LineOverflow() {
			var buffer = new StringWrapBuffer(2,10); //with this constructor: space is discarded, hyphen is retained
			buffer.Add("Hello.   Goodbye. ");
			Assert.AreEqual(buffer.Contents.Count,2); //2 strings after add & wrap
			Assert.AreEqual(buffer.Contents[0],"Hello."); //wrap should happen at edge of discarded characters
			Assert.AreEqual(buffer.Contents[1],"Goodbye. "); //spaces not used for wrapping should be unaffected

			buffer = new StringWrapBuffer(2,10,new char[] {'+'},null); //+ is retained
			buffer.Add("Hello.+Goodbye.+");
			Assert.AreEqual(buffer.Contents.Count,2); //2 strings after add & wrap
			Assert.AreEqual(buffer.Contents[0],"Hello.+"); //chosen + should be retained

			buffer.Clear();
			buffer.Add("abcdefghijklm");
			Assert.AreEqual(buffer.Contents.Count,2); //2 strings after add & wrap
			Assert.AreEqual(buffer.Contents[0],"abcdefghij"); //max length used when no separators present
			Assert.AreEqual(buffer.Contents[1],"klm");

			buffer = new StringWrapBuffer(-1,5);
			buffer.Add("abcde  ");
			Assert.AreEqual(buffer.Contents.Count,1); //discarded spaces should not create new line
			Assert.AreEqual(buffer.Contents[0],"abcde");

			buffer = new StringWrapBuffer(-1,20);
			buffer.Add("0 1 2 3 4 5 6 7 8 9 ");
			buffer.MaxLength = 12;
			Assert.AreEqual(buffer.Contents.Count,2); //should wrap onto 2nd line
			Assert.AreEqual(buffer.Contents[0],"0 1 2 3 4 5");
			Assert.AreEqual(buffer.Contents[1],"6 7 8 9 ");
			buffer.MaxLength = 3;
			Assert.AreEqual(buffer.Contents.Count,3); //should discard spaces and end up with 3 lines
			Assert.AreEqual(buffer.Contents[0],"0 1 2 3 4 5"); //the MaxLength value of 3 should not affect previous lines
			Assert.AreEqual(buffer.Contents[1],"6 7");
			Assert.AreEqual(buffer.Contents[2],"8 9"); //the trailing space should be discarded too

			buffer = new StringWrapBuffer(-1,1,null,new char[] {'!'}); //! is discarded
			buffer.Add("abc!!!!hijklm"); //expected lines: a b c h i j k l m
			Assert.AreEqual(buffer.Contents.Count,9);
			Assert.AreEqual(buffer.Contents[2],"c");
			Assert.AreEqual(buffer.Contents[3],"h");

			Assert.Throws(typeof(ArgumentOutOfRangeException),()=> { buffer.MaxLength = 0; });
		}
		[TestCase] public void BufferOverflow() {
			var buffer = new StringWrapBuffer(1,4);
			buffer.Add("abcde");
			Assert.AreEqual(buffer.Contents.Count,1); //only the overflow should remain
			Assert.AreEqual(buffer.Contents[0],"e");

			List<string> bufferOverflow = new List<string>();
			buffer.BufferFull += list => {
				foreach(string s in list) {
					bufferOverflow.Add(s);
				}
			};
			buffer.Add("fghijk"); //adding atop the "e"
			Assert.AreEqual(buffer.Contents[0],"ijk"); //only the overflow should remain
			Assert.AreEqual(bufferOverflow.Count,1);
			Assert.AreEqual(bufferOverflow[0],"efgh"); //this list should have received the full buffer's contents

			buffer = new StringWrapBuffer(3,4);
			buffer.BufferFull += list => {
				foreach(string s in list) {
					bufferOverflow.Add(s);
				}
			};
			bufferOverflow.Clear();
			buffer.Add("one two four five ");
			Assert.AreEqual(buffer.Contents.Count,1);
			Assert.AreEqual(buffer.Contents[0],"five"); //only "five" should remain
			Assert.AreEqual(bufferOverflow.Count,3);
			Assert.AreEqual(bufferOverflow[0],"one");
			Assert.AreEqual(bufferOverflow[2],"four");

			buffer.MaxLines = -1;
			buffer.Clear();
			bufferOverflow.Clear();
			buffer.Add("one two four five ");
			buffer.MaxLines = 4;
			Assert.AreEqual(4,buffer.Contents.Count);
			buffer.MaxLines = 3;
			Assert.AreEqual(1,buffer.Contents.Count);
		}
		[TestCase] public void ReservedSpace() {
			var buffer = new StringWrapBuffer(3,10);
			buffer.ReservedSpace = 4;
			List<string> bufferOverflow = new List<string>();
			buffer.BufferFull += list => {
				foreach(string s in list) {
					bufferOverflow.Add(s);
				}
			};
			buffer.Add("Absolutely");
			buffer.Add("positively");
			buffer.Add("fantastic.");
			Assert.AreEqual(3,buffer.Contents.Count);
			buffer.Add(" ");
			Assert.AreEqual(3,buffer.Contents.Count);
			Assert.AreEqual(buffer.Contents[2],"fantastic.");
			buffer.Add("?");
			Assert.AreEqual(3,bufferOverflow.Count);
			Assert.AreEqual("Absolutely",bufferOverflow[0]);
			Assert.AreEqual("fantas",bufferOverflow[2]);
			Assert.AreEqual(1,buffer.Contents.Count);
			Assert.AreEqual("tic. ?",buffer.Contents[0]); //the space is retained, now that it *isn't* the wrap location!

			buffer = new StringWrapBuffer(1,10);
			buffer.ReservedSpace = 4;
			bufferOverflow.Clear();
			buffer.BufferFull += list => {
				foreach(string s in list) {
					bufferOverflow.Add(s);
				}
			};
			buffer.Add("hey, great    ");
			Assert.AreEqual(1,buffer.Contents.Count);
			Assert.AreEqual(0,bufferOverflow.Count);
			Assert.AreEqual("hey, great",buffer.Contents[0]);
			buffer.ConfirmReservedSpace();
			Assert.AreEqual(1,bufferOverflow.Count);
			Assert.AreEqual("hey,",bufferOverflow[0]);
			Assert.AreEqual("great    ",buffer.Contents[0]);

			Assert.Throws(typeof(ArgumentOutOfRangeException),()=>buffer.ReservedSpace = -1);
		}
	}
}
