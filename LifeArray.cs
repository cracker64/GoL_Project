using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GoL_Proj
{
	public class LifeArray
	{
		/// <summary>
		/// The main state array used for simulating the ruleset
		/// </summary>
		public byte[,] GoL_Array;
		/// <summary>
		/// The count of neighbors in 1 radius
		/// </summary>
		byte[,] neighbors;
		int mWidth;
		int mHeight;
		int mX;
		int mY;
		/// <summary>
		/// The Colors that will apply to Console when activated 
		/// Colors are indexed with the state of a cell: {1, ... , ruleset[9], behindTextColor}
		/// </summary>
		Color[] real_Colors;

		// 0,1,2,3,4,5,6,7,8,STATES} live=1  spawn=2 spawn&live=3
		//{0,0,1,3,0,0,0,0,0,2     } is Conway's GoL
		/// <summary>
		/// The B and S rules the Array will follow each generation
		/// </summary>
		public byte[] ruleset;
		/// <summary>
		/// The string representation of the ruleset, showing B's, S's and States
		/// </summary>
		public string ruleString
		{
			get;
			private set;
		}
		/// <summary>
		/// Number of generations since start or clear
		/// </summary>
		public int generation;

		/// <summary>
		/// The simulation pause state
		/// </summary>
		public bool paused = true;
		/// <summary>
		/// The visibility of the simulation
		/// </summary>
		public bool hidden = false;

		/// <summary>
		/// An array of strings consisting of spaces which is pre-generated. Goes up to the width of screen
		/// </summary>
		public static string[] space_cache;

		/// <summary>
		/// Builds an array of all possible lengths of strings consisting of spaces to the size of the screen width.
		/// </summary>
		/// <param name="screenW">The size of the screen width</param>
		public static void Init(int screenW)
		{
			space_cache = new string[screenW + 1];
			space_cache[0] = "";
			for (int i = 1; i <= screenW; i++)
			{
				string a = "";
				for (int j = 0; j < i; j++)
				{
					a += " ";
				}
				space_cache[i] = a;
			}
		}
		/// <summary>
		/// Create a Bn/Sn/n string representation of a given ruleset
		/// </summary>
		/// <param name="rule">The rule to create a string of</param>
		/// <returns></returns>
		public static string BuildRuleString(byte[] rule)
		{
			if (rule.Length != 10)
				return "ERR";
			string r = "B";
			for (int i = 0; i < 9; i++)
				if ((rule[i] & 2) == 2)
					r += i;
			r += "/S";
			for (int i = 0; i < 9; i++)
				if ((rule[i] & 1) == 1)
					r += i;
			//We are assuming 2 states in a string where no state is shown.
			if (rule[9] > 2)
				r += ("/" + rule[9]);
			return r;
		}

		public LifeArray(int height, int width, byte[] rule, int x = 0, int y = 0)
		{
			GoL_Array = new byte[height, width];
			neighbors = new byte[height, width];
			mWidth = width;
			mHeight = height;
			mX = x;
			mY = y;
			ruleset = rule;
			generation = 0;
			ruleString = BuildRuleString(rule);
			real_Colors = new Color[2] { Color.FromArgb(0, 255, 0), Color.DarkGreen };
		}
		//A little helper for drawing more optimally, contains where each fragment is to be drawn
		public struct drawHelper
		{
			public int x;
			public int y;
			public byte state;
			public string s;
		}
		/// <summary>
		/// Draw the GoL_Array to the console
		/// </summary>
		public void Draw()
		{
			if (hidden) return;
			//More efficient draw, pre-process the array into strings for less Write() calls

			byte lastState = 0;
			int firstX = 0;
			int firstY = 0;
			int counter = 0;
			List<drawHelper> toDraw = new List<drawHelper>();
			drawHelper d = new drawHelper();
			for (int i = 0; i < mHeight; i++)
			{
				lastState = 0;
				counter = 0;
				for (int j = 0; j < mWidth; j++)
				{
					if (GoL_Array[i, j] > 1)
					{
						//We found something to draw! Note that state 0 and 1 are both dead here (Black)
						counter++;
						if (lastState != GoL_Array[i, j])
						{
							//Something different than the last cell, add previous to list (if it existed), init the new cell
							if (lastState != 0)
							{
								counter--;
								d.x = firstX;
								d.y = firstY;
								d.state = lastState;
								d.s = space_cache[counter];
								toDraw.Add(d);
								counter = 1;
							}
							firstX = j;
							firstY = i;
							lastState = GoL_Array[i, j];
						}
					}
					else
					{
						//Nothing found, if we had a cell previously, add to list and reset the counter. 
						if (lastState != 0)
						{
							d.x = firstX;
							d.y = firstY;
							d.state = lastState;
							d.s = space_cache[counter];
							toDraw.Add(d);
						}
						counter = 0;
						lastState = 0;
					}
				}
				//We can't draw past the screen edge, so we need to add our last cell (if we had one) at the end of each row.
				if (lastState != 0)
				{
					d.x = firstX;
					d.y = firstY;
					d.state = lastState;
					d.s = space_cache[counter];
					toDraw.Add(d);
				}
			}

			if (ruleset[9] == 2)
			{
				//If we only have two states, this is a more efficient draw method by only changing BackgroundColor once.
				Console.BackgroundColor = (ConsoleColor)2;
				foreach (drawHelper dr in toDraw)
				{
					Console.SetCursorPosition(mX + dr.x, mY + dr.y);
					Console.Write(dr.s);
				}
			}
			else
			{
				//If we have multiple states, we need to change BackgroundColor more often, which is super slow :(
				foreach (drawHelper dr in toDraw)
				{
					Console.BackgroundColor = (ConsoleColor)dr.state;
					Console.SetCursorPosition(mX + dr.x, mY + dr.y);
					Console.Write(dr.s);
				}
			}
			//Our default background is Black
			Console.BackgroundColor = ConsoleColor.Black;
		}

		/// <summary>
		/// Draw the neighbor count for each cell, very slow
		/// </summary>
		public void DrawNeighbors()
		{
			//We are recounting neighbors here which is inefficient, A better one would only calculate this during changes.
			//The default 96x54 screen is small enough to not matter
			neighbors = new byte[mHeight, mWidth];
			for (int i = 0; i < mHeight; i++)
			{
				for (int j = 0; j < mWidth; j++)
				{
					if (GoL_Array[i, j] == (ruleset[9]))
					{
						//For each living sell, set neighbor for all 8 nearby (wrapping)
						for (int ii = -1; ii < 2; ii++)
							for (int jj = -1; jj < 2; jj++)
							{
								if (jj != 0 || ii != 0)
									neighbors[(i + ii + mHeight) % mHeight, (j + jj + mWidth) % mWidth]++;
							}
					}
				}
			}
			Console.BackgroundColor = (ConsoleColor)2;
			Console.ForegroundColor = ConsoleColor.Black;
			//This lets us not set the color every single cell if there are multiple in a row
			byte last_color = 2;

			for (int i = 0; i < mHeight; i++)
			{
				for (int j = 0; j < mWidth; j++)
				{
					if (GoL_Array[i, j] > 1)
					{
						//Color setting for something alive
						if (last_color != GoL_Array[i, j])
						{
							Console.BackgroundColor = (ConsoleColor)GoL_Array[i, j];
							last_color = GoL_Array[i, j];
						}
						if ((ruleset[neighbors[i, j]] & 1) != 1)
							Console.ForegroundColor = ConsoleColor.Red;
						else
							Console.ForegroundColor = ConsoleColor.Black;
						//Draw count
						Console.SetCursorPosition(mX + j, mY + i);
						Console.Write(neighbors[i, j]);
					}
					else if (neighbors[i, j] > 0)
					{
						//Dead color setting, black background
						if (last_color != 0)
						{
							Console.BackgroundColor = (ConsoleColor)0;
							last_color = 0;
						}
						if (ruleset[neighbors[i, j]] >= 2)
							Console.ForegroundColor = ConsoleColor.Green;
						else
							Console.ForegroundColor = ConsoleColor.White;

						//Draw count
						Console.SetCursorPosition(mX + j, mY + i);
						Console.Write(neighbors[i, j]);
					}
				}
			}
			//Our default colors
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
		}

		/// <summary>
		/// Draws text on top of the LifeArray, this lets us preserve background color to know cell states.
		/// </summary>
		/// <param name="str">String to be drawn</param>
		/// <param name="x">X position (Relative to LifeArray)</param>
		/// <param name="y">Y position (Relative to LifeArray)</param>
		public void DrawText(string str, int x, int y)
		{
			if (hidden) return;
			Console.ForegroundColor = ConsoleColor.White;
			Console.SetCursorPosition(x + mX, y + mY);
			//We have to go through each character and possibly use different background color
			for (int i = 0; i < str.Length; i++)
			{
				//Use a different color on non-dead cells.
				if (GoL_Array[y, x + i] > 1)
					Console.BackgroundColor = (ConsoleColor)ruleset[9] + 1;
				else
					Console.BackgroundColor = ConsoleColor.Black;
				//Write the char
				Console.Write(str[i]);
			}
			Console.BackgroundColor = ConsoleColor.Black;
		}
		/// <summary>
		/// Draws a string where the background is always inverted from the default color (It stands out).
		/// </summary>
		/// <param name="str">String to be drawn</param>
		/// <param name="x">X position (Relative to LifeArray)</param>
		/// <param name="y">Y position (Relative to LifeArray)</param>
		public void DrawInvertText(string str, int x, int y)
		{
			if (hidden || x < 0 || y < 0 || x >= mWidth || y >= mHeight) return;
			Console.SetCursorPosition(x + mX, y + mY);

			for (int i = 0; i < str.Length; i++)
			{
				if (GoL_Array[y, x + i] > 1)
					Console.BackgroundColor = (ConsoleColor)13; //13 is the magic color I picked for storing invert
				else
					Console.BackgroundColor = ConsoleColor.White;
				//Draw the char
				Console.Write(str[i]);
			}
			Console.BackgroundColor = ConsoleColor.Black;
		}
		/// <summary>
		/// Draws a box outline where the background is always inverted from the default color (It stands out).
		/// </summary>
		/// <param name="x1">X start position (Relative to LifeArray)</param>
		/// <param name="y1">Y start position (Relative to LifeArray)</param>
		/// <param name="x2">X end position (Relative to LifeArray)</param>
		/// <param name="y2">Y end position (Relative to LifeArray)</param>
		public void DrawInvertBoxOutline(int x1, int y1, int x2, int y2)
		{
			//Swap vars if needed, so we can always increment the loop
			int i, j;
			if (x1 > x2)
			{
				i = x2;
				x2 = x1;
				x1 = i;
			}
			if (y1 > y2)
			{
				j = y2;
				y2 = y1;
				y1 = j;
			}
			//Draw top and bottom
			for (int x = x1; x <= x2; x++)
			{
				DrawInvertText(" ", x, y1);
				DrawInvertText(" ", x, y2);
			}
			//Draw sides
			for (int y = y1+1; y <= y2-1; y++)
			{
				DrawInvertText(" ", x1, y);
				DrawInvertText(" ", x2, y);
			}
		}
		/// <summary>
		/// Draws a line where the background is always inverted from the default color (It stands out).
		/// </summary>
		/// <param name="x1">X start position (Relative to LifeArray)</param>
		/// <param name="y1">Y start position (Relative to LifeArray)</param>
		/// <param name="x2">X end position (Relative to LifeArray)</param>
		/// <param name="y2">Y end position (Relative to LifeArray)</param>
		public void DrawInvertLine(int x1, int y1, int x2, int y2)
		{
			//This line draw is from The Power Toy <3
			bool reverseXY = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
			int x, y, dx, dy, sy;
			float e, de;
			if (reverseXY)
			{
				y = x1;
				x1 = y1;
				y1 = y;
				y = x2;
				x2 = y2;
				y2 = y;
			}
			if (x1 > x2)
			{
				y = x1;
				x1 = x2;
				x2 = y;
				y = y1;
				y1 = y2;
				y2 = y;
			}
			dx = x2 - x1;
			dy = Math.Abs(y2 - y1);
			e = 0.0f;
			if (dx != 0)
				de = dy / (float)dx;
			else
				de = 0.0f;
			y = y1;
			sy = (y1 < y2) ? 1 : -1;
			for (x = x1; x <= x2; x++)
			{
				if (reverseXY)
					DrawInvertText(" ", y, x);
				else
					DrawInvertText(" ", x, y);
				e += de;
				if (e >= 0.5f)
				{
					y += sy;
					if ((y1 < y2) ? (y <= y2) : (y >= y2))
					{
						if (reverseXY)
							DrawInvertText(" ", y, x);
						
						else
							DrawInvertText(" ", x, y);
					}
					e -= 1.0f;
				}
			}
		}

		/// <summary>
		/// Calculate one generation of the GoL_Array with ruleset
		/// </summary>
		public void Update()
		{
			//Do nothing if paused
			if (paused) return;
			generation++;
			//Count neighbors
			neighbors = new byte[mHeight, mWidth];
			for (int i = 0; i < mHeight; i++)
			{
				for (int j = 0; j < mWidth; j++)
				{
					if (GoL_Array[i, j] == (ruleset[9]))
					{
						//Set neighbor for all 8
						for (int ii = -1; ii < 2; ii++)
							for (int jj = -1; jj < 2; jj++)
							{
								if (jj != 0 || ii != 0)
									neighbors[(i + ii + mHeight) % mHeight, (j + jj + mWidth) % mWidth]++;
							}
					}
					else if (GoL_Array[i, j] > 0)
					{
						//This cell is dying
						GoL_Array[i, j]--;
					}
				}
			}
			for (int i = 0; i < mHeight; i++)
			{
				for (int j = 0; j < mWidth; j++)
				{
					//Check rules now!
					if (GoL_Array[i, j] == (ruleset[9]) && (ruleset[neighbors[i, j]] & 1) != 1)
					{
						//It died
						GoL_Array[i, j]--;
					}
					else if (GoL_Array[i, j] == 0 && ruleset[neighbors[i, j]] >= 2)
						GoL_Array[i, j] = ruleset[9];//Born!
				}
			}
		}
		/// <summary>
		/// Clears the array and resets generation counter.
		/// </summary>
		public void Reset()
		{
			GoL_Array = new byte[mHeight, mWidth];
			generation = 0;
		}
		/// <summary>
		/// Change the ruleset, this will convert previous living cells (max state) to the new max state.
		/// </summary>
		/// <param name="rule">The new ruleset to use</param>
		public void ChangeRule(byte[] rule)
		{
			if (rule.Length != 10)
				return;
			//Delete non-alive states, convert alive to the new max
			if (rule[9] != ruleset[9])
			{
				for (int i = 0; i < mHeight; i++)
				{
					for (int j = 0; j < mWidth; j++)
					{
						if (GoL_Array[i, j] == ruleset[9])
							GoL_Array[i, j] = rule[9];
						else
							GoL_Array[i, j] = 0;
					}
				}
			}
			//Use a clone or else it becomes the exact same object
			ruleset = (byte[])rule.Clone();
			ruleString = BuildRuleString(rule);
		}
		/// <summary>
		/// Sets specific ConsoleColors to values in real_Colors for drawing any color
		/// </summary>
		public void ApplyColors()
		{
			//Set color 1 to always be Black, so we don't see state=1 cells which are dead
			SetScreenColors.SetColor((ConsoleColor)1, Color.Black);
			//Anything over 2 has a color inside real_Colors
			for (int i = 2; i <= real_Colors.Length + 1; i++)
			{
				//computed i values are: 2 to (Length+1)	[0 to (Length-1)]
				SetScreenColors.SetColor((ConsoleColor)i,  real_Colors[i - 2]);
			}
			//Create an inverted color for the last (which is the Living state) and save it to color 13
			Color last = real_Colors[real_Colors.Length - 1];
			SetScreenColors.SetColor((ConsoleColor)13, Color.FromArgb(40 + (127-(last.R/2)), 40 + (127 - (last.G / 2)), 40 + (127 - (last.B / 2))));
		}
		/// <summary>
		/// Set the real_Colors array
		/// </summary>
		/// <param name="states">The new colors</param>
		public void ChangeColor(Color[] states)
		{
			real_Colors = states;
		}
		/// <summary>
		/// Returns true if the coords are inside
		/// </summary>
		/// <param name="x">X position to check (Relative to Window)</param>
		/// <param name="y">Y position to check (Relative to Window)</param>
		/// <returns></returns>
		public bool Contains(int x, int y)
		{
			return x >= mX && x < mWidth + mX && y >= mY && y < mHeight + mY;
		}
		/// <summary>
		/// Edit a single cell in the array, if contained.
		/// </summary>
		/// <param name="screenx">X position to check (Relative to Window)</param>
		/// <param name="screeny">Y position to check (Relative to Window)</param>
		/// <param name="delete">If you want to delete a cell, instead of create</param>
		public void EditPoint(int screenx, int screeny, bool delete)
		{
			if (Contains(screenx, screeny))
			{
				GoL_Array[screeny - mY, screenx - mX] = (byte)(delete ? 0 : ruleset[9]);
			}
		}
		/// <summary>
		/// Edit each point inside a box.
		/// </summary>
		/// <param name="x1">X start position (Relative to Window)</param>
		/// <param name="y1">Y start position (Relative to Window)</param>
		/// <param name="x2">X  end position (Relative to Window)</param>
		/// <param name="y2">Y end position (Relative to Window)</param>
		/// <param name="delete">If you want to delete a cell, instead of create</param>
		public void CreateBox(int x1, int y1, int x2, int y2, bool delete)
		{
			int i, j;
			if (x1 > x2)
			{
				i = x2;
				x2 = x1;
				x1 = i;
			}
			if (y1 > y2)
			{
				j = y2;
				y2 = y1;
				y1 = j;
			}
			for (int x = x1; x<=x2; x++)
			{
				for (int y=y1;y<=y2; y++)
				{
					EditPoint(x, y, delete);
				}
			}
		}
		/// <summary>
		/// Edit each point on a line, with the supplied brush.
		/// </summary>
		/// <param name="x1">X start position (Relative to Window)</param>
		/// <param name="y1">Y start position (Relative to Window)</param>
		/// <param name="x2">X end position (Relative to Window)</param>
		/// <param name="y2">Y end position (Relative to Window)</param>
		/// <param name="b">The brush that will be used to draw</param>
		/// <param name="delete">If you want to delete a cell, instead of create</param>
		public void CreateLine(int x1, int y1, int x2, int y2, GoLBrush b, bool delete)
		{
			//This line draw is from The Power Toy <3
			bool reverseXY = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
			int x, y, dx, dy, sy;
			float e, de;
			if (reverseXY)
			{
				y = x1;
				x1 = y1;
				y1 = y;
				y = x2;
				x2 = y2;
				y2 = y;
			}
			if (x1 > x2)
			{
				y = x1;
				x1 = x2;
				x2 = y;
				y = y1;
				y1 = y2;
				y2 = y;
			}
			dx = x2 - x1;
			dy = Math.Abs(y2 - y1);
			e = 0.0f;
			if (dx!=0)
				de = dy / (float)dx;
			else
				de = 0.0f;
			y = y1;
			sy = (y1 < y2) ? 1 : -1;
			for (x = x1; x <= x2; x++)
			{
				if (reverseXY)
				{
					b.mX = (byte)y;
					b.mY = (byte)x;
					b.Apply(this, delete);
				}
				else
				{
					b.mX = (byte)x;
					b.mY = (byte)y;
					b.Apply(this, delete);
				}
				e += de;
				if (e >= 0.5f)
				{
					y += sy;
					if ((y1 < y2) ? (y <= y2) : (y >= y2))
					{
						if (reverseXY)
						{
							b.mX = (byte)y;
							b.mY = (byte)x;
							b.Apply(this, delete);
						}
						else
						{
							b.mX = (byte)x;
							b.mY = (byte)y;
							b.Apply(this, delete);
						}
					}
					e -= 1.0f;
				}
			}
		}
	}
}
