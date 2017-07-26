using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSPG;
using System.Threading;
using System.Runtime.InteropServices;
using ConsoleLib;
using System.Diagnostics;
using System.Drawing;
using static ConsoleLib.NativeMethods;

//Game of Life simulation and other fun rules.
//Controls - 
//			Mouse - Draw and delete (Left and Right click)
//					Hold Shift to draw lines
//					Hold Ctrl to draw boxes
//			"+" -	Larger brush
//			"-" -	Smaller brush
//			Space - Pause/Unpause
//			F -		One generation/frame
//			C -		Clear the screen
//			N -		Show neighbor counts
//			H -		Hide/show HUD text
//			Tab -	Open the menu
//			Esc -	Exit the game

// Pilihp64


namespace GoL_Proj
{
	public class Program
	{
		//This program supports multiple arrays at once, although we only use one for my demo
		public static List<LifeArray> GoLs = new List<LifeArray>();
		//The main user brush
		public static GoLBrush brush;

		//Various flags for usability
		public static bool Run = true;
		static bool frameStep = false;
		static bool drawHUD = true;
		static bool drawNeighbors = false;
		static bool openMenu = false;

		static void Main(string[] args)
		{

			//Cheat font to square.
			Utility.SetConsoleFont("Raster Fonts");
			//Setup Window
			Utility.SetupWindow("GoL Fun", Console.LargestWindowWidth, Console.LargestWindowHeight, true);
			Utility.EOLWrap(false);
			//Fullscreen
			Utility.SetScreenMode(1);

			//Init some functions
			LifeArray.Init(Console.LargestWindowWidth);
			brush = new GoLBrush(0, 0, 0);
			setupMenu();

			//Events for Mouse/Window/Keyboard
			ConsoleListener.Start();
			ConsoleListener.MouseEvent += ConsoleListener_MouseEvent;
			ConsoleListener.WindowBufferSizeEvent += ConsoleListener_WindowBufferSizeEvent;
			ConsoleListener.KeyEvent += ConsoleListener_KeyEvent;

			//Main Life array
			GoLs.Add(new LifeArray(Console.LargestWindowHeight, Console.LargestWindowWidth, new byte[] { 0, 0, 1, 3, 0, 0, 0, 0, 0, 2 }));

			//Since we are starting with the array shown, need to apply colors
			GoLs[0].ApplyColors();

			//TODO: Menu things
			//		- Information, save/load data
			//TODO: copy/cut/paste

			while (true)
			{
				if (!Run)
					break;

				//Inputs would go here, but they are in another thread.

				if (openMenu)
				{
					//Init menu loop
					menuLoop();
				}

				//Yay Updates
				foreach (LifeArray GoL in GoLs)
					GoL.Update();

				if (frameStep)
				{
					frameStep = false;
					foreach (LifeArray GoL in GoLs)
						GoL.paused = true;
				}

				//Yay Draws
				Utility.LockConsole(true);
				Console.Clear();
				foreach (LifeArray GoL in GoLs)
				{
					if (drawNeighbors)
						GoL.DrawNeighbors();
					else
						GoL.Draw();
				}

				if (drawHUD)
				{
					//Draw overlay for Line/Box
					if (drawState == 1)
						GoLs[0].DrawInvertLine(start_mX, start_mY, curr_mX, curr_mY);
					else if (drawState == 2)
						GoLs[0].DrawInvertBoxOutline(start_mX, start_mY, curr_mX, curr_mY);

					//Draw Brush outline for non-box
					if (drawState!=2)
						brush.DrawOutline(GoLs[0]);

					if (GoLs[0].paused)
						GoLs[0].DrawText("-- Simulation paused --", Console.WindowWidth / 2 - 12, Console.WindowHeight - 3);
					GoLs[0].DrawText("Gen: " + GoLs[0].generation, 1, 1);
					GoLs[0].DrawText("Rule: " + GoLs[0].ruleString, 1, 2);
				}

				Utility.LockConsole(false);

				//Yay Sleep
				Thread.Sleep(25);
			}
			ConsoleListener.Stop();
		}
		/// <summary>
		/// Draws a box outline, with a specified background color
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <param name="w">Width of box</param>
		/// <param name="h">Height of box</param>
		/// <param name="c">Color of box background</param>
		private static void DrawBox(int x, int y, int w, int h, ConsoleColor c)
		{
			Console.BackgroundColor = c;
			Console.SetCursorPosition(x, y);
			//We can use the space cache here, too
			Console.Write(LifeArray.space_cache[w]);

			for (int i = 1; i < h - 1; i++)
			{
				Console.SetCursorPosition(x, y + i);
				Console.Write(" ");

				Console.SetCursorPosition(x + w - 1, y + i);
				Console.Write(" ");
			}
			if (y != y + h - 1)
			{
				Console.SetCursorPosition(x, y + h - 1);
				Console.Write(LifeArray.space_cache[w]);
			}

			Console.BackgroundColor = ConsoleColor.Black;
		}

		//Lists for buttons
		private static List<Button> Button_List;
		private static List<Button> B_List;
		private static List<Button> S_List;
		private static Button state_Button;

		//Ruleset arrays used for the menu
		private static byte[] menu_S;
		private static byte[] menu_B;
		private static byte[] menu_Combined;

		/// <summary>
		/// When one of the B buttons is pressed
		/// </summary>
		public static void pressB(Program.MouseButton button, ref byte ind)
		{
			menu_B[ind] = (byte)((menu_B[ind] == 0) ? 1 : 0);
			Program.updateMenuRule();
		}
		/// <summary>
		/// When one of the S buttons is pressed
		/// </summary>
		public static void pressS(Program.MouseButton button, ref byte ind)
		{
			menu_S[ind] = (byte)((menu_S[ind] == 0) ? 1 : 0);
			Program.updateMenuRule();
		}
		/// <summary>
		/// When the state button is pressed
		/// </summary>
		public static void pressState(Program.MouseButton button, ref byte ind)
		{
			//Go up/down based on left/right click, stay between 2 and 10
			menu_Combined[9] = (byte)(((menu_Combined[9] + 7 + (button == MouseButton.Left ? 1 : -1)) % 9) + 2);
			byte state = menu_Combined[9];
			//Generate new colors for this state amount
			Color[] col = new Color[state];
			for (int i = 1; i < state; i++)
			{
				//This fades from 255 green, to bright blue
				col[i - 1] = Color.FromArgb(0, 55 + i * (200 / (state - 1)), 255 - (255 / (state - i)));
			}
			//The behind text color
			col[state - 1] = Color.FromArgb(0, 120, 0);

			ind = state;
			GoLs[0].ChangeColor(col);
		}
		/// <summary>
		/// When a preset button is pressed, set the appropriate rules and colors
		/// </summary>
		public static void pressPreset(Program.MouseButton button, ref byte menu)
		{
			menu_Combined[9] = 2; //Set Default State
			if (menu == 0) // GoL
			{
				menu_S = new byte[9] { 0, 0, 1, 1, 0, 0, 0, 0, 0 };
				menu_B = new byte[9] { 0, 0, 0, 1, 0, 0, 0, 0, 0 };
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.FromArgb(0, 255, 0), Color.DarkGreen });

			}
			else if (menu == 1) // H Life
			{
				menu_S = new byte[9] { 0, 0, 1, 1, 0, 0, 0, 0, 0 };
				menu_B = new byte[9] { 0, 0, 0, 1, 0, 0, 1, 0, 0 };
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.Red, Color.DarkRed });
			}
			else if (menu == 2) // 2x2
			{
				menu_S = new byte[9] { 0, 1, 1, 0, 0, 1, 0, 0, 0 };
				menu_B = new byte[9] { 0, 0, 0, 1, 0, 0, 1, 0, 0 };
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.Yellow, Color.FromArgb(160, 160, 0) });
			}
			else if (menu == 3) // 34
			{
				menu_S = new byte[9] { 0, 0, 0, 1, 1, 0, 0, 0, 0 };
				menu_B = new byte[9] { 0, 0, 0, 1, 1, 0, 0, 0, 0 };
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.Magenta, Color.DarkMagenta });

			}
			else if (menu == 4) // Maze
			{
				menu_S = new byte[9] { 0, 1, 1, 1, 1, 1, 0, 0, 0 };
				menu_B = new byte[9] { 0, 0, 0, 1, 0, 0, 0, 0, 0 };
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.FromArgb(0xA8, 0xE4, 0xA0), Color.FromArgb(0x63, 0x85, 0x5E) });

			}
			else if (menu == 5) // Replicator
			{
				menu_S = new byte[9] { 0, 1, 0, 1, 0, 1, 0, 1, 0 };
				menu_B = new byte[9] { 0, 1, 0, 1, 0, 1, 0, 1, 0 };
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.Cyan, Color.DarkCyan });
			}
			else if (menu == 6) // Move
			{
				menu_S = new byte[9] { 0, 0, 1, 0, 1, 1, 0, 0, 0 };
				menu_B = new byte[9] { 0, 0, 0, 1, 0, 0, 1, 0, 1 };
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.White, Color.Gray });
			}
			else if (menu == 7) // LoTE
			{
				menu_S = new byte[9] { 0, 0, 0, 1, 1, 1, 0, 0, 1 };
				menu_B = new byte[9] { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
				menu_Combined[9] = 4; //Set State
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.Yellow, Color.Orange, Color.Red, Color.DarkRed });
			}
			else if (menu == 8) // Star
			{
				menu_S = new byte[9] { 0, 0, 0, 1, 1, 1, 1, 0, 0 };
				menu_B = new byte[9] { 0, 0, 1, 0, 0, 0, 0, 1, 1 };
				menu_Combined[9] = 6; //Set State
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.FromArgb(0, 0, 240), Color.FromArgb(0, 0, 190), Color.FromArgb(0, 0, 140), Color.FromArgb(0, 0, 110), Color.FromArgb(0, 0, 80), Color.FromArgb(0, 0, 60) });
			}
			else if (menu == 9) // Frog
			{
				menu_S = new byte[9] { 0, 1, 1, 0, 0, 0, 0, 0, 0 };
				menu_B = new byte[9] { 0, 0, 0, 1, 1, 0, 0, 0, 0 };
				menu_Combined[9] = 3; //Set State
				updateMenuRule();
				GoLs[0].ChangeColor(new Color[] { Color.FromArgb(0, 255, 0), Color.Green, Color.DarkGreen });
			}
		}
		/// <summary>
		/// Initializes buttons used in the menu
		/// </summary>
		private static void setupMenu()
		{
			//Set default values, create new Lists
			menu_S = new byte[9] { 0, 0, 1, 1, 0, 0, 0, 0, 0 };
			menu_B = new byte[9] { 0, 0, 0, 1, 0, 0, 0, 0, 0 };
			menu_Combined = new byte[10] { 0, 0, 1, 3, 0, 0, 0, 0, 0, 2 };
			Button_List = new List<Button>();
			B_List = new List<Button>();
			S_List = new List<Button>();
			//The preset buttons (could be written better)
			Button_List.Add(new Button(11, 10, 20, 1, 0, ConsoleColor.Black, pressPreset, "Conway's Life B3/S23", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 12, 17, 1, 1, ConsoleColor.Black, pressPreset, "High Life B36/S23", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 14, 12, 1, 2, ConsoleColor.Black, pressPreset, "2x2 B36/S125", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 16, 10, 1, 3, ConsoleColor.Black, pressPreset, "34 B34/S34", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 18, 14, 1, 4, ConsoleColor.Black, pressPreset, "Maze B3/S12345", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 20, 22, 1, 5, ConsoleColor.Black, pressPreset, "Replicator B1357/S1357", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 22, 14, 1, 6, ConsoleColor.Black, pressPreset, "Move B368/S245", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 24, 16, 1, 7, ConsoleColor.Black, pressPreset, "Edge B37/S3458/4", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 26, 17, 1, 8, ConsoleColor.Black, pressPreset, "Star B278/S3456/6", f_col: ConsoleColor.Cyan));
			Button_List.Add(new Button(11, 28, 14, 1, 9, ConsoleColor.Black, pressPreset, "Frog B34/S12/3", f_col: ConsoleColor.Cyan));
			// B/S buttons
			for (byte i = 0; i < 9; i++)
			{
				Button b = new Button(3 + 7 + ((i % 9) * 6), Console.WindowHeight - 19, 5, 5, i, ConsoleColor.Red, pressB);
				Button_List.Add(b); B_List.Add(b);
				b = new Button(3 + 7 + ((i % 9) * 6), Console.WindowHeight - 13, 5, 5, i, ConsoleColor.Red, pressS);
				Button_List.Add(b); S_List.Add(b);
			}
			//State button
			state_Button = new Button(65, Console.WindowHeight - 13, 5, 5, 2, ConsoleColor.Yellow, pressState);
			Button_List.Add(state_Button);
		}
		/// <summary>
		/// Applies changes from the menu_B/S arrays into the button colors. Update state button text.
		/// </summary>
		public static void updateMenuRule()
		{
			//Change the color of B/S buttons
			for (int i = 0; i < 9; i++)
			{
				menu_Combined[i] = (byte)(menu_S[i] + menu_B[i] * 2);

				if (menu_Combined[i] >= 2)
					B_List[i].back_col = ConsoleColor.Green;
				else
					B_List[i].back_col = ConsoleColor.Red;
				if (menu_Combined[i] == 1 || menu_Combined[i] == 3)
					S_List[i].back_col = ConsoleColor.Green;
				else
					S_List[i].back_col = ConsoleColor.Red;
			}
			state_Button.ind = menu_Combined[9];
		}
		//Silly rainbow value
		static int hueRotate = 0;
		/// <summary>
		/// The main loop for the menu system
		/// </summary>
		private static void menuLoop()
		{
			//Reload rule from tables if it changed.
			updateMenuRule();
			//Reset colors for menu
			SetScreenColors.ResetColors();
			//Rainbow color is on DarkGray
			SetScreenColors.SetColor(ConsoleColor.DarkGray, 255, 127, 0);

			while (openMenu)
			{
				//Draw Menu
				Utility.LockConsole(true);
				Console.Clear();

				//The big box
				DrawBox(4, 4, Console.WindowWidth - 8, Console.WindowHeight - 8, ConsoleColor.DarkGray);
				//Hue Rainbow cycler for box
				SetScreenColors.SetColor(ConsoleColor.DarkGray, (short)(hueRotate % 360), 255, 180);
				hueRotate += 3;

				Console.SetCursorPosition(Console.WindowWidth / 2 - 4, 6);
				Console.Write("GoL Menu");

				Console.SetCursorPosition(10, 8);
				Console.Write("Click a Rule below, or create your own!");

				foreach (Button b in Button_List)
					b.Draw();

				Console.SetCursorPosition(7, Console.WindowHeight - 17);
				Console.Write("B");

				Console.SetCursorPosition(7, Console.WindowHeight - 11);
				Console.Write("S");

				Console.SetCursorPosition(10, Console.WindowHeight - 21);
				Console.Write(LifeArray.BuildRuleString(menu_Combined));

				Console.SetCursorPosition(65, Console.WindowHeight - 15);
				Console.Write("States");

				Console.SetCursorPosition(Console.WindowWidth / 2 - 8, Console.WindowHeight - 3);
				Console.Write("-- Menu Mode --");

				Utility.LockConsole(false);

				Thread.Sleep(25);
			}
			//Set new rule and colors when leaving menu
			GoLs[0].ChangeRule(menu_Combined);
			GoLs[0].ApplyColors();
		}


		/// <summary>
		/// This dictionary tracks key events, useful for implementing better key tracking
		/// </summary>
		static Dictionary<ConsoleKey, bool> keyStates = new Dictionary<ConsoleKey, bool>();

		/// <summary>
		/// When a key is pressed
		/// </summary>
		/// <param name="r">The key event</param>
		private static void ConsoleListener_KeyEvent(KEY_EVENT_RECORD r)
		{
			//The default event system is terrible, so I convert it to a more usable system by tracking keys
			//Where state =  0 down, 1 stay, 2 up
			byte state = 0;

			ConsoleKey k = (ConsoleKey)r.wVirtualKeyCode;
			if (keyStates.ContainsKey(k))
			{
				//Already pressed
				if (r.bKeyDown)
					state = 1;
				else
				{
					//Up event!
					keyStates.Remove(k);
					state = 2;
				}
			}
			else
			{
				//New Press
				if (r.bKeyDown)
					keyStates.Add(k, true); //state = 0;
			}

			//Now that we have better state tracking, use it here
			if (openMenu)
			{
				//Inputs with Menu
				switch (k)
				{
					case ConsoleKey.Escape:
					case ConsoleKey.Tab:
						if (state == 0)
							openMenu = false;
						break;
					default:
						break;
				}
			}
			else
			{
				//Inputs on game
				switch (k)
				{
					case ConsoleKey.Escape: // Exit sim
						if (state == 0)
							Run = false;
						break;
					case ConsoleKey.Spacebar: // Pause sim
						if (state == 0)
							GoLs[0].paused = !GoLs[0].paused;
						break;
					case ConsoleKey.F: // Frame only
						if (state == 0)
						{
							frameStep = true;
							GoLs[0].paused = false;
						}
						break;
					case ConsoleKey.C: // Clear screen
						if (state == 0)
							GoLs[0].Reset();
						break;
					case ConsoleKey.N: // Show Neighbors
						if (state == 0)
							drawNeighbors = !drawNeighbors;
						break;
					case ConsoleKey.H: // Hide HUD text
						if (state == 0)
							drawHUD = !drawHUD;
						break;
					case ConsoleKey.Tab: // Open Menu
						if (state == 0)
							openMenu = true;
						break;
					case ConsoleKey.OemPlus: // Bigger brush
						if (state == 0)
							brush.queueChange(1, 1);
						break;
					case ConsoleKey.OemMinus: // Smaller brush
						if (state == 0)
							brush.queueChange(-1, -1);
						break;
					default:
						break;
				}
			}
		}

		private static void ConsoleListener_WindowBufferSizeEvent(WINDOW_BUFFER_SIZE_RECORD r)
		{
			//Window buffer reset? Make sure mouse cursor stays hidden
			Console.CursorVisible = false;
		}

		//Some mouse tracking vars
		static bool mouseL = false;
		static bool mouseR = false;
		static byte drawState = 0;
		static short start_mX = -1;
		static short start_mY = -1;
		static short curr_mY = -1;
		static short curr_mX = -1;

		public enum MouseButton
		{
			None,
			Left,
			Right,
			Both
		}
		public enum MouseState
		{
			Down,
			Hold,
			Up
		}

		/// <summary>
		/// My better mouse even handler.
		/// </summary>
		/// <param name="m">Which buttons are pressed</param>
		/// <param name="s">The state of the pressed buttons</param>
		/// <param name="x">X of the mouse event</param>
		/// <param name="y">Y of the mouse event</param>
		/// <param name="modState">Which modifier keys are down (ctrl/shift)</param>
		private static void HandleMouse(MouseButton m, MouseState s, short x, short y, uint modState)
		{
			if (!openMenu)
			{
				//Update the brush position during the normal game
				brush.mX = (byte)x;
				brush.mY = (byte)y;
			}

			if (openMenu)
			{
				//Menu mouse! Forward mouse to buttons
				foreach (Button b in Button_List)
					b.Press(x, y, m, s);
			}
			else if (m != MouseButton.None)
			{
				//One or both buttons changed
				if (start_mX != x || start_mY != y)
				{
					//This is a new location
					if (s == MouseState.Down)
					{
						if ((modState & 0x10) == 0x10)
						{
							//LShift, Line draw
							drawState = 1;
						}
						else if ((modState & 0x8) == 0x8)
						{
							//LControl, Box draw
							drawState = 2;
						}
						else
						{
							//Normal  mouse, delete if right is down
							foreach (LifeArray GoL in GoLs)
								brush.Apply(GoL, m == MouseButton.Right ? true : false);
						}
						start_mX = x;
						start_mY = y;
					}
					else if (s == MouseState.Hold)
					{
						if (drawState == 0)
						{
							//Draw lines between points when holding, delete if right is down
							foreach (LifeArray GoL in GoLs)
								GoL.CreateLine(start_mX, start_mY, x, y, brush, m == MouseButton.Right ? true : false);
							start_mX = x;
							start_mY = y;
						}
					}
					else if (s == MouseState.Up)
					{
						//When we release, draw line/box if appropriate
						if (drawState == 1)
						{
							foreach (LifeArray GoL in GoLs)
								GoL.CreateLine(start_mX, start_mY, x, y, brush, m == MouseButton.Right ? true : false);
						}
						else if (drawState == 2)
						{
							foreach (LifeArray GoL in GoLs)
								GoL.CreateBox(start_mX, start_mY, x, y, m == MouseButton.Right ? true : false);
						}
					}
					
				}
			}
			else
			{
				//No button is down, just movement, default the values.
				start_mX = -1;
				start_mY = -1;
				drawState = 0;
			}
			//Set some position vars so we can see it outside the function (line/box preview)
			curr_mX = x;
			curr_mY = y;
		}
		/// <summary>
		/// When a mouse event happens.
		/// </summary>
		/// <param name="r">The event</param>
		private static void ConsoleListener_MouseEvent(MOUSE_EVENT_RECORD r)
		{
			//The default events are terrible, so I convert them to something easier
			//With Down, Hold, and Up events.
			switch (r.dwEventFlags)
			{
				case 2:
				case 0:
					if ((r.dwButtonState & 0x1) == 1)
					{
						if (!mouseL)
						{
							mouseL = true;
							HandleMouse(MouseButton.Left, MouseState.Down, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						}
						else
							HandleMouse(MouseButton.Left, MouseState.Hold, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
					}
					else
					{
						if (mouseL)
						{
							mouseL = false;
							HandleMouse(MouseButton.Left, MouseState.Up, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						}
					}

					if ((r.dwButtonState & 0x2) == 2)
					{
						if (!mouseR)
						{
							mouseR = true;
							HandleMouse(MouseButton.Right, MouseState.Down, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						}
						else
							HandleMouse(MouseButton.Right, MouseState.Hold, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
					}
					else
					{
						if (mouseR)
						{
							mouseR = false;
							HandleMouse(MouseButton.Right, MouseState.Up, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						}
					}
					break;
				case 1:
					if (r.dwButtonState == 0)
					{
						HandleMouse(MouseButton.None, MouseState.Hold, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						break;
					}
					if ((r.dwButtonState & 0x1) == 1)
					{
						if ((r.dwButtonState & 0x2) == 2)
							HandleMouse(MouseButton.Both, MouseState.Hold, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						else
							HandleMouse(MouseButton.Left, mouseL? MouseState.Hold: MouseState.Down, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						//MouseL may be false, seems to only happen when unfocused
						mouseL = true;
					}
					else if ((r.dwButtonState & 0x2) == 2)
					{
						HandleMouse(MouseButton.Right, mouseR ? MouseState.Hold : MouseState.Down, r.dwMousePosition.X, r.dwMousePosition.Y, r.dwControlKeyState);
						//MouseR may be false, seems to only happen when unfocused
						mouseR = true;
					}
					break;
				default:
					break;
			}
		}

	}


	/// <summary>
	/// A generic Box class that can draw itself and check if a point is inside.
	/// I didn't really need this separate from buttons, it doesn't make much sense with all the other things it has
	/// </summary>
	public class Box
	{
		int mX;
		int mY;
		int width;
		int height;
		int topX;
		int topY;

		/// <summary>
		/// A value specific to this box
		/// </summary>
		public byte ind;

		//Some colors to draw the box with
		public ConsoleColor back_col;
		public ConsoleColor front_col;
		public ConsoleColor front_col_c;

		public bool hidden;

		/// <summary>
		/// The box draw can use a string, too
		/// </summary>
		string text;

		public Box(int x, int y, int w, int h, ConsoleColor c, byte index, string s = null, bool hid = false, ConsoleColor f_color = ConsoleColor.White)
		{
			mX = x;
			mY = y;
			width = w;
			height = h;
			topX = x + w - 1;
			topY = y + h - 1;
			back_col = c;
			front_col = f_color;
			front_col_c = f_color;
			ind = index;
			hidden = hid;
			text = s;
		}
		/// <summary>
		/// Returns true if a point is inside the box
		/// </summary>
		/// <param name="x">X position (relative to box)</param>
		/// <param name="y">Y position (relative to box)</param>
		/// <returns></returns>
		public bool Contains(int x, int y)
		{
			return x >= mX && x <= topX && y >= mY && y <= topY;
		}
		/// <summary>
		/// Draws the box
		/// </summary>
		public void Draw()
		{
			if (hidden) return;
			Console.BackgroundColor = back_col;
			Console.ForegroundColor = front_col;

			Console.SetCursorPosition(mX, mY);
			Console.Write(LifeArray.space_cache[width]);

			for (int i = 1; i < height - 1; i++)
			{
				Console.SetCursorPosition(mX, mY + i);
				Console.Write(" ");

				Console.SetCursorPosition(topX, mY + i);
				Console.Write(" ");
			}

			if (mY != topY)
			{
				Console.SetCursorPosition(mX, topY);
				Console.Write(LifeArray.space_cache[width]);
			}
			if (text != null)
			{
				Console.SetCursorPosition(mX, mY);
				Console.Write(text);
			}
			else
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.White;
				Console.SetCursorPosition(mX + width / 2, mY + height / 2);
				Console.Write(ind);
			}

			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
	public delegate void onPress(Program.MouseButton button, ref byte ind);
	/// <summary>
	/// A button which will execute a function if a coordinate is inside
	/// </summary>
	public class Button : Box
	{
		onPress func;
		public Button(int x, int y, int w, int h, byte index, ConsoleColor col, onPress f, string text = null, bool hid = false, ConsoleColor f_col = ConsoleColor.White) : base(x, y, w, h, col, index, text, hid, f_col)
		{
			func = f;
		}
		public bool wasInside = false;
		/// <summary>
		/// Will execute the box's function if the position is inside
		/// </summary>
		/// <param name="screenX">X Position (Window relative)</param>
		/// <param name="screenY">Y Position (Window relative)</param>
		/// <param name="button_state">Which buttons are down</param>
		/// <param name="mouse_state">State of buttons</param>
		public void Press(int screenX, int screenY, Program.MouseButton button_state, Program.MouseState mouse_state)
		{
			if (Contains(screenX, screenY))
			{
				switch (button_state)
				{
					case Program.MouseButton.None:
						front_col = ConsoleColor.Yellow;
						wasInside = true;
						return;
					case Program.MouseButton.Left:
					case Program.MouseButton.Right:
						if (mouse_state == Program.MouseState.Down)
							func(button_state, ref ind);
						return;

					default:
						return;
				}
			}
			else if (wasInside)
			{
				//used for mouseOver color change when leaving
				front_col = front_col_c;
				wasInside = false;
			}
		}
	}
}
