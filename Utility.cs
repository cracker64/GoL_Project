using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;


namespace FSPG
{
    public static class Utility
    {
        [DllImport("user32.dll")]
        static extern bool LockWindowUpdate(IntPtr hWndLock);
        [DllImport("user32.dll")]
        static extern ushort GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);        
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, uint wFlags);
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
		[StructLayout(LayoutKind.Sequential)]
		public struct COORD
		{

			public short X;
			public short Y;
			public COORD(short x, short y)
			{
				this.X = x;
				this.Y = y;
			}

		}
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetConsoleDisplayMode(
			IntPtr ConsoleOutput
			, uint Flags
			, out COORD NewScreenBufferDimensions
			);

		// Win32 constants
		const int STD_INPUT_HANDLE = -10;
        const int STD_OUTPUT_HANDLE = -11;
        const int STD_ERROR_HANDLE = -12;
        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;
        const int SWP_NOSIZE = 1;
        const uint ENABLE_PROCESSED_INPUT = 0x0001;
        const uint ENABLE_LINE_INPUT = 0x0002;
        const uint ENABLE_ECHO_INPUT = 0x0004;
        const uint ENABLE_WINDOW_INPUT = 0x0008;
        const uint ENABLE_MOUSE_INPUT = 0x0010;
        const uint ENABLE_INSERT_MODE = 0x0020;
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        const uint ENABLE_AUTO_POSITION = 0x0100;
        const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
        const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal unsafe struct CONSOLE_FONT_INFO_EX
		{
			internal uint cbSize;
			internal uint nFont;
			internal COORD dwFontSize;
			internal int FontFamily;
			internal int FontWeight;
			internal fixed char FaceName[LF_FACESIZE];
		}
		
		private const int TMPF_TRUETYPE = 4;
		private const int LF_FACESIZE = 32;
		private static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetCurrentConsoleFontEx(
			IntPtr consoleOutput,
			bool maximumWindow,
			ref CONSOLE_FONT_INFO_EX consoleCurrentFontEx);


		[DllImport("kernel32.dll", SetLastError = true)]
		static extern int SetConsoleFont(
			IntPtr hOut,
			uint dwFontNum
			);


		// class fields
		static Random mPRNG = new Random();
        static bool mGoodRead = true;

        /// <summary>
        /// Use this to format the console window.
        /// </summary>
        /// <param name="title">This string will be written in the window's title bar</param>
        /// <param name="width">The new width of the window</param>
        /// <param name="height">The new height of the window</param>
        /// <param name="center">Whether or not to center the window on the screen</param>
        public static void SetupWindow(string title, int width, int height, bool center = true)
        {
            Console.Title = title;

            Console.SetWindowSize(width, height);
            Console.SetBufferSize(width, height);

            if (center)
            {
                RECT rect;
                if (!GetWindowRect(new HandleRef(
                    Process.GetCurrentProcess().GetLifetimeService(),
                    Process.GetCurrentProcess().MainWindowHandle), out rect))
                    return;

                int sw = GetSystemMetrics(SM_CXSCREEN);
                int sh = GetSystemMetrics(SM_CYSCREEN);
                int ww = rect.Right - rect.Left;
                int wh = rect.Bottom - rect.Top;
                int x = sw / 2 - ww / 2;
                int y = sh / 2 - wh / 2;
                SetWindowPos(Process.GetCurrentProcess().MainWindowHandle,
                    0, x, y, 0, 0, SWP_NOSIZE);
            }
        }

        /// <summary>
        /// Locks/unlocks the memory used by Console to display output.
        /// Helps reduce screen flicker when:
        /// lock applied before Clear(), and unlock applied after Write(s)
        /// </summary>
        /// <param name="applyLock">Whether or not to lock</param>
        public static void LockConsole(bool applyLock)
        {
            if (applyLock)
            {
                LockWindowUpdate(Process.GetCurrentProcess().MainWindowHandle);
            }
            else
            {
                LockWindowUpdate(IntPtr.Zero);
            }
        }

        /// <summary>
        /// By default when you write to the bottom-right coordinate of the console, 
        /// it wraps the cursor position, effectively creating a new line and scrolling 
        /// the console window by 1 row. This has the effect of not being able to write
        /// on the last line. You can prevent that functionality by turning this OFF.
        /// </summary>
        /// <param name="applyWrap">Whether or not to wrap last new line of console</param>
        public static void EOLWrap(bool applyWrap)
        {
            uint mode;
            IntPtr stdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);

            if (!GetConsoleMode(stdoutHandle, out mode))
                return;

            if (applyWrap)
                mode |= ENABLE_WRAP_AT_EOL_OUTPUT;
            else
                mode &= ~ENABLE_WRAP_AT_EOL_OUTPUT;

			SetConsoleMode(stdoutHandle, mode);
		}
		
		//Set screen mode, 1 is fullscreen, 4 is windowed.
		 public static void SetScreenMode(uint mode)
		{
			IntPtr stdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
			//I'm putting my fullscreen change here.
			COORD xy = new COORD(100, 100);
			SetConsoleDisplayMode(stdoutHandle, mode, out xy);
		}

		//Set the font of the window, hacky 20x20 font right now.
		public static void SetConsoleFont(string fontName = "Lucida Console")
		{
			unsafe
			{
				IntPtr hnd = GetStdHandle(STD_OUTPUT_HANDLE);
				if (hnd != INVALID_HANDLE_VALUE)
				{
					CONSOLE_FONT_INFO_EX info = new CONSOLE_FONT_INFO_EX();
					info.cbSize = (uint)Marshal.SizeOf(info);

					// Set console font to Lucida Console.
					CONSOLE_FONT_INFO_EX newInfo = new CONSOLE_FONT_INFO_EX();
					newInfo.cbSize = (uint)Marshal.SizeOf(newInfo);
					newInfo.FontFamily = 0;
					IntPtr ptr = new IntPtr(newInfo.FaceName);
					Marshal.Copy(fontName.ToCharArray(), 0, ptr, fontName.Length);
					
					int screenWidth = Screen.PrimaryScreen.Bounds.Width;
					int screenHeight = Screen.PrimaryScreen.Bounds.Height;
				
					short sX = (short)((screenWidth / 96) + 1);
					short sY = (short)(screenHeight / 54);
					// Get some settings from current font.
					newInfo.dwFontSize = new COORD(sX, sY);//17,16 gives a 16x16 block (or 20x20 with 125% scale)
					newInfo.FontWeight = info.FontWeight;
					SetCurrentConsoleFontEx(hnd, false, ref newInfo);
				}
			}
		}

		/// <summary>
		/// Returns a Unix timestamp,
		/// which is the number of seconds since The Epoch (January 1 1970).
		/// </summary>
		public static int UnixNow()
        {
            return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        /// <summary>
        /// Seeds the pseudo-random number generator in this class.
        /// </summary>
        /// <param name="x"></param>
        public static void SeedPRNG(int seed)
        {
            mPRNG = new Random(seed);
        }

        /// <summary>
        /// Returns a pseudo-random number
        /// from the pseudo-random number generator within this class.
        /// </summary>
        public static int Rand()
        {
            return mPRNG.Next();
        }

        /// <summary>
        /// Writes a message to the center of the screen.
        /// An optional vertical offset can be specified.
        /// </summary>
        /// <param name="msg">The message to write</param>
        /// <param name="offset">The vertical offset from the center of screen</param>
        public static void WriteCentered(string msg, int offset = 0)
        {
            int x = (Console.WindowWidth / 2) - (msg.Length / 2);
            int y = (Console.WindowHeight / 2) + offset;

            // make sure the cursor position is on the screen
            if (x < 0)
                x = 0;
            else if (x >= Console.WindowWidth)
                x = Console.WindowWidth - 1;
            if (y < 0)
                y = 0;
            else if (y >= Console.WindowHeight)
                y = Console.WindowHeight - 1;

            Console.SetCursorPosition(x, y);
            Console.Write(msg); 
        }

        /// <summary>
        /// Draws a box (or "window") within the console.
        /// </summary>
        /// <param name="left">The x coordinate of the top-left of the box</param>
        /// <param name="top">The y coordinate of the top-left of the box</param>
        /// <param name="width">The width of the box</param>
        /// <param name="height">The height of the box</param>
        /// <param name="dbl">Use single or double lines for border</param>
        public static void DrawBox(int left, int top, int width, int height, bool dbl = false)
        {
            string singleLine = "┌─┐│└┘";
            string doubleLine = "╔═╗║╚╝";
            string set = dbl ? doubleLine : singleLine;

            Console.SetCursorPosition(left, top);
            Console.Write(set[0]);

            for (int col = 0; col < width - 2; col++)
            {
                Console.Write(set[1]);
            }

            Console.Write(set[2]);

            int x = left + width - 1;
            int y = top + 1;

            for (int row = 0; row < height - 2; row++, y++)
            {
                Console.SetCursorPosition(left, y);
                Console.Write(set[3]);

                Console.SetCursorPosition(x, y);
                Console.Write(set[3]);
            }

            y = top + height - 1;

            Console.SetCursorPosition(left, y);
            Console.Write(set[4]);

            for (int col = 0; col < width - 2; col++)
            {
                Console.Write(set[1]);
            }

            Console.Write(set[5]);
        }

        /// <summary>
        /// Same as ReadInt, only for byte input.
        /// </summary>
        public static byte ReadByte()
        {
            string raw = "";
            byte result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToByte(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for sbyte input.
        /// </summary>
        public static sbyte ReadSByte()
        {
            string raw = "";
            sbyte result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToSByte(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for short input.
        /// </summary>
        public static short ReadShort()
        {
            string raw = "";
            short result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToInt16(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for ushort input.
        /// </summary>
        public static ushort ReadUShort()
        {
            string raw = "";
            ushort result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToUInt16(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Attempts to read an Int32 (int) from the console.
        /// The integer input, if the read succeeded, is returned.
        /// This function will prevent any program crashes.
        /// If Exceptions occur, they are caught, ignored and 0 is returned.
        /// </summary>
        public static int ReadInt()
        {
            string raw = "";
            int result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToInt32(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for uint input.
        /// </summary>
        public static uint ReadUInt()
        {
            string raw = "";
            uint result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToUInt32(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for long input.
        /// </summary>
        public static long ReadLong()
        {
            string raw = "";
            long result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToInt64(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for ulong input.
        /// </summary>
        public static ulong ReadULong()
        {
            string raw = "";
            ulong result = 0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToUInt64(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for float input.
        /// </summary>
        public static float ReadFloat()
        {
            string raw = "";
            float result = 0.0f;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToSingle(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// Same as ReadInt, only for double input.
        /// </summary>
        public static double ReadDouble()
        {
            string raw = "";
            double result = 0.0;

            try
            {
                raw = Console.ReadLine();
                result = Convert.ToDouble(raw);
                mGoodRead = true;
            }
            catch (Exception)
            {
                mGoodRead = false;
            }

            return result;
        }

        /// <summary>
        /// The success of the last Read call.
        /// False if last Read failed, True otherwise. 
        /// </summary>
        public static bool IsReadGood()
        {
            return mGoodRead;
        }

        /// <summary>
        /// Returns true if they key is currently pressed.
        /// </summary>
        /// <param name="key">The Key to check</param>
        public static bool GetKeyState(ConsoleKey key)
        {
            return ((GetAsyncKeyState((int)key) & 0x8000) != 0);
        }

        /// <summary>
        /// Eats any remaining key presses in the input buffer.
        /// This is often necessary after a game loop if GetKeyState is used.
        /// </summary>
        public static void FlushConsoleInput()
        {
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
        }
    }
}
