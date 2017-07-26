// Copyright Alex Shvedov
// Modified by MercuryP with color specifications
// Use this code in any way you want

using System;
using System.Diagnostics;                // for Debug
using System.Drawing;                    // for Color (add reference to  System.Drawing.assembly)
using System.Runtime.InteropServices;    // for StructLayout

namespace GoL_Proj
{
	public class SetScreenColors
	{
		[StructLayout(LayoutKind.Sequential)]
		internal struct COORD
		{
			internal short X;
			internal short Y;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct SMALL_RECT
		{
			internal short Left;
			internal short Top;
			internal short Right;
			internal short Bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct COLORREF
		{
			internal uint ColorDWORD;

			internal COLORREF(Color color)
			{
				ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
			}

			internal COLORREF(uint r, uint g, uint b)
			{
				ColorDWORD = r + (g << 8) + (b << 16);
			}

			internal Color GetColor()
			{
				return Color.FromArgb((int)(0x000000FFU & ColorDWORD),
									  (int)(0x0000FF00U & ColorDWORD) >> 8, (int)(0x00FF0000U & ColorDWORD) >> 16);
			}

			internal void SetColor(Color color)
			{
				ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct CONSOLE_SCREEN_BUFFER_INFO_EX
		{
			internal int cbSize;
			internal COORD dwSize;
			internal COORD dwCursorPosition;
			internal ushort wAttributes;
			internal SMALL_RECT srWindow;
			internal COORD dwMaximumWindowSize;
			internal ushort wPopupAttributes;
			internal bool bFullscreenSupported;
			internal COLORREF black;
			internal COLORREF darkBlue;
			internal COLORREF darkGreen;
			internal COLORREF darkCyan;
			internal COLORREF darkRed;
			internal COLORREF darkMagenta;
			internal COLORREF darkYellow;
			internal COLORREF gray;
			internal COLORREF darkGray;
			internal COLORREF blue;
			internal COLORREF green;
			internal COLORREF cyan;
			internal COLORREF red;
			internal COLORREF magenta;
			internal COLORREF yellow;
			internal COLORREF white;
		}

		const int STD_OUTPUT_HANDLE = -11;                                        // per WinBase.h
		internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);    // per WinBase.h

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

		// Set a specific console color to an RGB color
		// The default console colors used are gray (foreground) and black (background)
		public static int SetColor(ConsoleColor consoleColor, Color targetColor)
		{
			return SetColor(consoleColor, (uint)targetColor.R, targetColor.G, targetColor.B);
		}
		/// <summary>
		/// A variant of SetColor which converts HSV color to RGB first
		/// </summary>
		public static int SetColor(ConsoleColor consoleColor, short hue, uint s, uint v)
		{
			//This function from The Powder Toy
			float hh, ss, vv, c, x;
			int m;
			int r=0, g=0, b=0;
			hh = hue / 60.0f;//normalize values
			ss = s / 255.0f;
			vv = v / 255.0f;
			c = vv * ss;
			x = c * (1 - Math.Abs( (hh % 2.0f) - 1));
			if (hh < 1)
			{
				r = (int)(c * 255.0);
				g = (int)(x * 255.0);
				b = 0;
			}
			else if (hh < 2)
			{
				r = (int)(x * 255.0);
				g = (int)(c * 255.0);
				b = 0;
			}
			else if (hh < 3)
			{
				r = 0;
				g = (int)(c * 255.0);
				b = (int)(x * 255.0);
			}
			else if (hh < 4)
			{
				r = 0;
				g = (int)(x * 255.0);
				b = (int)(c * 255.0);
			}
			else if (hh < 5)
			{
				r = (int)(x * 255.0);
				g = 0;
				b = (int)(c * 255.0);
			}
			else if (hh < 6)
			{
				r = (int)(c * 255.0);
				g = 0;
				b = (int)(x * 255.0);
			}
			m = (int)((vv - c) * 255.0);
			r += m;
			g += m;
			b += m;
			return SetColor(consoleColor, (uint)r, (uint)g, (uint)b);
		}

		public static int SetColor(ConsoleColor color, uint r, uint g, uint b)
		{
			CONSOLE_SCREEN_BUFFER_INFO_EX csbe = new CONSOLE_SCREEN_BUFFER_INFO_EX();
			csbe.cbSize = (int)Marshal.SizeOf(csbe);                    // 96 = 0x60
			IntPtr hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);    // 7
			if (hConsoleOutput == INVALID_HANDLE_VALUE)
			{
				return Marshal.GetLastWin32Error();
			}
			bool brc = GetConsoleScreenBufferInfoEx(hConsoleOutput, ref csbe);
			if (!brc)
			{
				return Marshal.GetLastWin32Error();
			}

			switch (color)
			{
				case ConsoleColor.Black:
					csbe.black = new COLORREF(r, g, b);
					break;
				case ConsoleColor.DarkBlue:
					csbe.darkBlue = new COLORREF(r, g, b);
					break;
				case ConsoleColor.DarkGreen:
					csbe.darkGreen = new COLORREF(r, g, b);
					break;
				case ConsoleColor.DarkCyan:
					csbe.darkCyan = new COLORREF(r, g, b);
					break;
				case ConsoleColor.DarkRed:
					csbe.darkRed = new COLORREF(r, g, b);
					break;
				case ConsoleColor.DarkMagenta:
					csbe.darkMagenta = new COLORREF(r, g, b);
					break;
				case ConsoleColor.DarkYellow:
					csbe.darkYellow = new COLORREF(r, g, b);
					break;
				case ConsoleColor.Gray:
					csbe.gray = new COLORREF(r, g, b);
					break;
				case ConsoleColor.DarkGray:
					csbe.darkGray = new COLORREF(r, g, b);
					break;
				case ConsoleColor.Blue:
					csbe.blue = new COLORREF(r, g, b);
					break;
				case ConsoleColor.Green:
					csbe.green = new COLORREF(r, g, b);
					break;
				case ConsoleColor.Cyan:
					csbe.cyan = new COLORREF(r, g, b);
					break;
				case ConsoleColor.Red:
					csbe.red = new COLORREF(r, g, b);
					break;
				case ConsoleColor.Magenta:
					csbe.magenta = new COLORREF(r, g, b);
					break;
				case ConsoleColor.Yellow:
					csbe.yellow = new COLORREF(r, g, b);
					break;
				case ConsoleColor.White:
					csbe.white = new COLORREF(r, g, b);
					break;
			}
			++csbe.srWindow.Bottom;
			++csbe.srWindow.Right;
			brc = SetConsoleScreenBufferInfoEx(hConsoleOutput, ref csbe);
			if (!brc)
			{
				return Marshal.GetLastWin32Error();
			}
			return 0;
		}

		/// <summary>
		/// Reset the colors back to default
		/// </summary>
		public static void ResetColors()
		{
			SetColor(ConsoleColor.DarkBlue, Color.DarkBlue);
			SetColor(ConsoleColor.DarkCyan, Color.DarkCyan);
			SetColor(ConsoleColor.DarkGreen, Color.DarkGreen);
			SetColor(ConsoleColor.DarkMagenta, Color.DarkMagenta);
			SetColor(ConsoleColor.DarkRed, Color.DarkRed);
			SetColor(ConsoleColor.DarkYellow, (uint)160,160,0);
			SetColor(ConsoleColor.Gray, Color.Gray);
			SetColor(ConsoleColor.DarkGray, Color.DarkGray);
			SetColor(ConsoleColor.Blue, Color.Blue);
			SetColor(ConsoleColor.Green, (uint)0,255,0);
			SetColor(ConsoleColor.Cyan, Color.Cyan);
			SetColor(ConsoleColor.Red, Color.Red);
			SetColor(ConsoleColor.Magenta, Color.Magenta);
		}

		public static int SetScreenColor(Color foregroundColor, Color backgroundColor)
		{
			int irc;
			irc = SetColor(ConsoleColor.Gray, foregroundColor);
			if (irc != 0) return irc;
			irc = SetColor(ConsoleColor.Black, backgroundColor);
			if (irc != 0) return irc;

			return 0;
		}
	}
}