using System;

namespace GoL_Proj
{
	public class GoLBrush
	{
		//Position
		public byte mX;
		public byte mY;
		//Size
		byte sizeX;
		byte sizeY;
		//Type 0-Square 1-Circle(Not-Implemented)
		byte type;
		/// <summary>
		/// The generated array which is used for editing
		/// </summary>
		byte[,] brush;
		/// <summary>
		/// The generated array which is used to displaying the brush
		/// </summary>
		byte[,] outline;
		byte brushW;
		byte brushH;

		//Queue size changes from input thread
		int tempCX;
		int tempCY;
		/// <summary>
		/// If there is a waiting brush size change
		/// </summary>
		bool pendingChange;

		public GoLBrush(byte x, byte y, byte t)
		{
			sizeX = x;
			sizeY = y;
			type = t;
			mX = 0;
			mY = 0;
			tempCX = 0;
			tempCY = 0;
			pendingChange = false;
			generateBrush();
		}
		/// <summary>
		/// Generate new byte arrays for the current brush size.
		/// </summary>
		public void generateBrush()
		{
			brushW = (byte)((sizeX * 2) + 1);
			brushH = (byte)((sizeY * 2) + 1);
			brush = new byte[brushH, brushW];
			outline = new byte[brushH, brushW];

			switch (type)
			{
				default:
				case 0: // Square brush
					//Fill entire grid
					for (int i=0;i<brushH;i++)
						for (int j=0;j<brushW;j++)
						{
							brush[i, j] = 1;
						}
					//Special case for 0 radius brush, don't draw any outline
					if (sizeX == 0 && sizeY == 0)
					{
						outline[0, 0] = 0;
						break;
					}
					//Otherwise draw the borders in the outline
					for (int i = 0; i < brushW; i++)
					{
						outline[0, i] = 1;
						outline[brushH-1, i] = 1;
					}
					for (int i = 0; i < brushH; i++)
					{
						outline[i, 0] = 1;
						outline[i,brushW-1 ] = 1;
					}
					break;
				case 1: // Circle brush
					//Circle stuff here
					break;
			}
		}
		/// <summary>
		/// Queue a brush size change which the main update will apply
		/// </summary>
		/// <param name="cx">Change in X</param>
		/// <param name="cy">Change in Y</param>
		public void queueChange(int cx, int cy)
		{
			pendingChange = true;
			tempCX = cx;
			tempCY = cy;
		}
		/// <summary>
		/// Apply waiting changes to brush size and generate arrays
		/// </summary>
		public void changeBrush()
		{
			sizeX = (byte)(sizeX+tempCX);
			sizeY = (byte)(sizeY+tempCY);
			if (sizeX > 200)
				sizeX = 20;
			else if (sizeX > 20)
				sizeX = 0;
			if (sizeY > 200)
				sizeY = 20;
			else if (sizeY > 20)
				sizeY = 0;

			//Recalc brush/outline
			generateBrush();

			pendingChange = false;
		}
		/// <summary>
		/// Draws the outline of a brush on a LifeArray
		/// </summary>
		/// <param name="L">The LifeArray to draw on</param>
		public void DrawOutline(LifeArray L)
		{
			if (pendingChange)
				changeBrush();
			for (int i = 0; i < brushH; i++)
				for (int j = 0; j < brushW; j++)
				{
					//I haven't found why this crashes sometimes.
					try
					{
						if (outline[i, j] == 1)
							L.DrawInvertText(" ", j + mX - sizeX, i + mY - sizeY);
					}
					catch
					{}
				}
		}
		/// <summary>
		/// Edit the given LifeArray with this brush
		/// </summary>
		/// <param name="L">The LifeArray to edit</param>
		/// <param name="delete">If you want to delete cells instead of create</param>
		public void Apply(LifeArray L, bool delete)
		{
			if (pendingChange)
				changeBrush();
			for (int i = 0; i < brushH; i++)
				for (int j = 0; j < brushW; j++)
				{
					L.EditPoint(j + mX - sizeX, i + mY - sizeY, delete);
				}
		}
	}
}