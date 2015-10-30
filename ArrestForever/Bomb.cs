using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Threading;
using System.ComponentModel;

namespace ArrestForever
{
    public enum TypeOfBomb
    {
        normal,
        around_small, around_big,
        row, column, row_column,
        diagonal
    }
    public enum MyRole
    {
        A,B
    }
    public class Bomb
    {
        public System.Drawing.Point point;
        public TimeSpan time;
        public TypeOfBomb type;
        public MyRole role;
        public MyMap map;
        public DispatcherTimer frameCounter;
        public Image rect;
        public List<ImageSource> rect_eff;
        public List<Image> bomb_eff;
        public static List<List<ImageSource>> bomb_img = new List<List<ImageSource>>();
        public static List<ImageSource> bomb_effects=new List<ImageSource>();
        public int Counter;
        public static double gridSize;
        private byte[,] MirrorMap;

        public Bomb(System.Drawing.Point p, TimeSpan ti, TypeOfBomb ty, MyMap map)
        {
            point = p;
            time = TimeSpan.FromMilliseconds(ti.TotalMilliseconds);
            type = ty;
            this.map = map;
            bomb_eff = new List<Image>();
            rect = new Image();
            Counter = 0;
            rect_eff = bomb_img[0];
            rect.Width = gridSize;
            rect.Source = rect_eff[Counter++];
            for (int i = 0; i < bomb_effects.Count; i++)
            {
                Image img = new Image();
                img.Source = bomb_effects[i];
                img.Height = gridSize;
                bomb_eff.Add(img);
            }
			startFrameCounter();
        }

        public void startFrameCounter()
        {
            frameCounter = new DispatcherTimer();
            frameCounter.Tick += new EventHandler(framecounter);
            frameCounter.Interval = TimeSpan.FromMilliseconds(300);
            frameCounter.Start();
        }

        private void framecounter(object sender, EventArgs e)
        {
            rect.Source = rect_eff[Counter];
            Counter = (Counter + 1) % rect_eff.Count;
        }

        public List<System.Drawing.Point> getExplodePoint()
        {
            List<System.Drawing.Point> tmp_point = new List<System.Drawing.Point>();
            this.MirrorMap = map.getMirrorMap(false);
            switch (type)
            {
                case TypeOfBomb.normal:
                    tmp_point.Add(new System.Drawing.Point(point.X, point.Y));
                    break;
                case TypeOfBomb.around_small:
                    {
                        if (isBlockFree(point.X, point.Y - 1))
                            tmp_point.Add(new System.Drawing.Point(point.X, point.Y - 1));
                        if (isBlockFree(point.X, point.Y + 1))
                            tmp_point.Add(new System.Drawing.Point(point.X, point.Y + 1));
                        if (isBlockFree(point.X - 1, point.Y))
                            tmp_point.Add(new System.Drawing.Point(point.X - 1, point.Y));
                        if (isBlockFree(point.X + 1, point.Y))
                            tmp_point.Add(new System.Drawing.Point(point.X + 1, point.Y));
                    }
                    break;
                case TypeOfBomb.around_big:
                    {
                        if (isBlockFree(point.X, point.Y - 1))
                            tmp_point.Add(new System.Drawing.Point(point.X, point.Y - 1));
                        if (isBlockFree(point.X, point.Y + 1))
                            tmp_point.Add(new System.Drawing.Point(point.X, point.Y + 1));
                        if (isBlockFree(point.X - 1, point.Y))
                            tmp_point.Add(new System.Drawing.Point(point.X - 1, point.Y));
                        if (isBlockFree(point.X + 1, point.Y))
                            tmp_point.Add(new System.Drawing.Point(point.X + 1, point.Y));
                        if (isBlockFree(point.X - 1, point.Y - 1))
                            tmp_point.Add(new System.Drawing.Point(point.X - 1, point.Y - 1));
                        if (isBlockFree(point.X + 1, point.Y - 1))
                            tmp_point.Add(new System.Drawing.Point(point.X + 1, point.Y - 1));
                        if (isBlockFree(point.X - 1, point.Y + 1))
                            tmp_point.Add(new System.Drawing.Point(point.X - 1, point.Y + 1));
                        if (isBlockFree(point.X + 1, point.Y + 1))
                            tmp_point.Add(new System.Drawing.Point(point.X + 1, point.Y + 1));
                    }
                    break;
                case TypeOfBomb.row:
                    {
                        for (int i = 0; i < MirrorMap.GetLength(0); i++)
                        {
                            if (isBlockFree(i, point.Y))
                                tmp_point.Add(new System.Drawing.Point(i, point.Y));
                        }
                    }
                    break;
                case TypeOfBomb.column:
                    {
                        for (int i=0;i<MirrorMap.GetLength(1);i++)
                        {
                            if (isBlockFree(point.X, i))
                                tmp_point.Add(new System.Drawing.Point(point.X,i));
                        }
                    }
                    break;
				case TypeOfBomb.row_column:
					{
						for (int i = 0; i < MirrorMap.GetLength(0); i++)
						{
							if (isBlockFree(i, point.Y))
								tmp_point.Add(new System.Drawing.Point(i, point.Y));
						}
						for (int i = 0; i < MirrorMap.GetLength(1); i++)
						{
							if (isBlockFree(point.X, i))
								tmp_point.Add(new System.Drawing.Point(point.X, i));
						}
					}
					break;
				case TypeOfBomb.diagonal:
					{
						for (int i = point.X, j = point.Y; i < MirrorMap.GetLength(0) && j < MirrorMap.GetLength(1); i++, j++)
							tmp_point.Add(new System.Drawing.Point(i, j));
						for (int i = point.X, j = point.Y; i < MirrorMap.GetLength(0) && j >= 0; i++, j--)
							tmp_point.Add(new System.Drawing.Point(i, j));
						for (int i = point.X, j = point.Y; i >= 0 && j < MirrorMap.GetLength(1); i--, j++)
							tmp_point.Add(new System.Drawing.Point(i, j));
						for (int i = point.X, j = point.Y; i >= 0 && j >= 0; i--, j--)
							tmp_point.Add(new System.Drawing.Point(i, j));
					}
					break;
            }
            return tmp_point;
        }

        private bool isBlockFree(int X, int Y)
        {
            int X_Limit = MirrorMap.GetLength(1);
            int Y_Limit = MirrorMap.GetLength(0);
            if (X >= 0 && Y >= 0 && X < X_Limit && Y < Y_Limit && MirrorMap[X, Y] == 1)
                return true;
            return false;
        }
    }
}
