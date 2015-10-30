using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ArrestForever
{
	public class MapEffect
	{
		private List<ImageSource> frames;
		DispatcherTimer frameCounter;
		public Image rect;
		int Count;
		public delegate void End();
		public event End end;
		public MapEffect(List<ImageSource> img,double width)
		{
			frames = img;
			rect = new Image()
			{
				Width = width * 5
			};
		}

		public void InitThread()
		{
			frameCounter = new DispatcherTimer();
			frameCounter.Tick += frameCounter_Tick;
			frameCounter.Interval = TimeSpan.FromMilliseconds(50);
			frameCounter.Start();
		}

		private void frameCounter_Tick(object sender, EventArgs e)
		{
			rect.Source = frames[Count];
			Count++;
			if (Count == frames.Count)
			{
				Count = 0;
				end();
			}
		}
	}
}
