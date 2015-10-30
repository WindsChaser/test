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
using System.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Threading;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;
using Microsoft.DirectX.AudioVideoPlayback;
using System.Media;

namespace ArrestForever
{
    public class Source
    {
		public static XElement GetTreeNode(XElement XML, string newroot, string attribute, string value)
		{
			return XML.DescendantsAndSelf(newroot).Single(X => X.Attribute(attribute).Value == value);
		}
        public MapSource mapsource;
        public PersonSource personsource1;
		public PersonSource personsource2;
        public BombSource bombsource;
        public MusicSource musicsource;
		public int SourceID;
        public Source(int ID)
        {
			SourceID = ID;
            mapsource = new MapSource(SourceID);
			personsource1 = new PersonSource(0);
			personsource2 = new PersonSource(1);
			bombsource = new BombSource(SourceID);
			musicsource = new MusicSource(SourceID);
        }
        public class MapSource
        {
			public ImageSource start_p1;
			public ImageSource start_p2;
            public ImageSource background_p;
            public ImageSource brick_p;
			public ImageSource HpDrug;
			public ImageSource MpDrug;
			public ImageSource HMDrug;
			public ImageSource Decoration1;
			public ImageSource Decoration2;
			public List<ImageSource> effect = new List<ImageSource>();
			XElement temp;
			public MapSource(int ID)
			{
				XElement config = XElement.Load(@"Source\MapSource\MapConfig.xml");
				XElement MapDate = Source.GetTreeNode(config, "Map", "Sign", ID + "");
				temp = MapDate.Element("Background");
				background_p = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("StartMask1");
				start_p1 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("StartMask2");
				start_p2 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("Brick");
				brick_p = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("HPDrug");
				HpDrug = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("MPDrug");
				MpDrug = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("HMDrug");
				HMDrug = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("Decoration1");
				Decoration1 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("Decoration2");
				Decoration2 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = MapDate.Element("Effect");
				var decoder = new GifBitmapDecoder(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative)
					, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
				for (int i = 0; i < decoder.Frames.Count; i++)
					effect.Add(decoder.Frames[i]);
			}
        }
        public class PersonSource
        {
			public List<ImageSource> up = new List<ImageSource>();
			public List<ImageSource> down = new List<ImageSource>();
			public List<ImageSource> left = new List<ImageSource>();
			public List<ImageSource> right = new List<ImageSource>();
			public List<List<ImageSource>> body = new List<List<ImageSource>>();
			XElement temp;
			GifBitmapDecoder decoder;
			public PersonSource(int ID)
			{
				XElement config = XElement.Load(@"Source\PersonSource\PersonConfig.xml");
				XElement PersonDate = Source.GetTreeNode(config, "Person", "Sign", ID + "");
				temp = PersonDate.Element("left");
				decoder = new GifBitmapDecoder(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative)
					,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.Default);
				for (int i = 0; i < decoder.Frames.Count; i++)
					left.Add(decoder.Frames[i]);
				temp = PersonDate.Element("right");
				decoder = new GifBitmapDecoder(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative)
					, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
				for (int i = 0; i < decoder.Frames.Count; i++)
					right.Add(decoder.Frames[i]);
				temp = PersonDate.Element("up");
				decoder = new GifBitmapDecoder(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative)
					, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
				for (int i = 0; i < decoder.Frames.Count; i++)
					up.Add(decoder.Frames[i]);
				temp = PersonDate.Element("down");
				decoder = new GifBitmapDecoder(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative)
					, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
				for (int i = 0; i < decoder.Frames.Count; i++)
					down.Add(decoder.Frames[i]);
				body.Add(left);
				body.Add(right);
				body.Add(up);
				body.Add(down);
			}
        }
        public class BombSource
        {
			public List<ImageSource> bomb = new List<ImageSource>();
			public ImageSource effect;
			public ImageSource skill1;
			public ImageSource skill2;
			public ImageSource skill3;
			public ImageSource skill4;
			public ImageSource skill5;
			public ImageSource skill6;
			public ImageSource skill7;
			XElement temp;
			GifBitmapDecoder decoder;
			public BombSource(int ID)
			{
				XElement config = XElement.Load(@"Source\BombSource\BombConfig.xml");
				XElement BombDate = Source.GetTreeNode(config, "Bomb", "Sign", ID + "");
				temp = BombDate.Element("bomb");
				decoder = new GifBitmapDecoder(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative)
					, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
				for (int i = 0; i < decoder.Frames.Count; i++)
					bomb.Add(decoder.Frames[i]);
				temp = BombDate.Element("effect");
				effect = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = BombDate.Element("skill1");
				skill1 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = BombDate.Element("skill2");
				skill2 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = BombDate.Element("skill3");
				skill3 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = BombDate.Element("skill4");
				skill4 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = BombDate.Element("skill5");
				skill5 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = BombDate.Element("skill6");
				skill6 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
				temp = BombDate.Element("skill7");
				skill7 = BitmapFrame.Create(new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative));
			}
        }
        public class MusicSource
        {
			public Uri explode;
			public Uri step;
			public Uri skill;
			public Uri recover;
			public Uri iceflush1;
			public Uri iceflush2;
			public Uri hurt_m;
			public Uri hurt_f;
			public MediaPlayer Bomb = new MediaPlayer();
			public MediaPlayer Recover = new MediaPlayer();
			/// <summary>
			/// 
			/// </summary>
			XElement temp;
			public MusicSource(int ID)
			{
				XElement config = XElement.Load(@"Source\MusicSource\MusicConfig.xml");
				XElement MapDate = Source.GetTreeNode(config, "Music", "Sign", ID + "");
				temp = MapDate.Element("explode");
				explode = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				temp = MapDate.Element("step");
				step = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				temp = MapDate.Element("skill");
				skill = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				temp = MapDate.Element("recover");
				recover = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				temp = MapDate.Element("iceflush1");
				iceflush1 = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				temp = MapDate.Element("iceflush2");
				iceflush2 = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				temp = MapDate.Element("hurt_m");
				hurt_m = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				temp = MapDate.Element("hurt_f");
				hurt_f = new Uri(string.Format(@"{0}", temp.Attribute("Src").Value), UriKind.Relative);
				Bomb.Open(explode);
				Recover.Open(recover);
			}
			public void Play(Uri uri)
			{
				//ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
				//{
				//	MediaPlayer temp = new MediaPlayer();
				//	temp.Open(uri);
				//	temp.MediaEnded += (object sender, EventArgs e) =>
				//	{
				//		temp.Close();
				//	};
				//	temp.Play();
				//}), uri);
			}
        }
    }
}
