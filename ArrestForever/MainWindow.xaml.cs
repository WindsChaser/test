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
using ArrestForever.Role;
using QX.Game.PathFinder;

namespace ArrestForever
{
	public partial class MainWindow : Window
	{
		private Source source;
		private MyMap testMap;
		private MyPerson person1, person2;
		private MyPerson MainPerson;
		private bool isServer = true;
		private int numX = 16;
		private int numY = 16;
		private double gridSize;
		private int MapRrefreshAmount = 10;
		private NetworkControl networkcontrol;
		private CreatGameDialog creatGameDialog;
		private JoinGameDialog joinGameDialog;
		private DispatcherTimer TimerOfEffect;
		private DispatcherTimer TimerOfHMReset;
		private int tag1;
		private int CurrentSkill = 0;
		private MediaPlayer bgm = new MediaPlayer();
		public MainWindow()
		{
			bgm.Open( new Uri( @"Source\MusicSource\beforeBGM.mp3", UriKind.Relative ) );
			bgm.MediaEnded += ( o, e ) =>
				{
					bgm.Stop();
					bgm.Play();
				};
			bgm.Play();
			InitializeComponent();
			ThreadPool.SetMaxThreads( 40, 20 );
		}

		private void SetMapEnvironment()
		{
			List<System.Drawing.Point> re = testMap.CreatBrickBlocks( 50 );
			sendCreatBlockCommand( re );
			RefreshBlock( re, true );//生成障碍物
									 //设置地图自动更新
			testMap.TimerOfUpdate = new DispatcherTimer();
			testMap.TimerOfUpdate.Tick += new EventHandler( _updateBlocks );
			testMap.TimerOfUpdate.Interval = TimeSpan.FromMilliseconds( 1000 );
			testMap.TimerOfUpdate.Start();
			//设置地图刷新HP恢复药水
			testMap.TimerOfHpRecover = new DispatcherTimer();
			testMap.TimerOfHpRecover.Tick += TimerOfHpRecover_Tick;
			testMap.TimerOfHpRecover.Interval = TimeSpan.FromSeconds( 10 );
			testMap.TimerOfHpRecover.Start();
			//设置地图刷新MP恢复药水
			testMap.TimerOfMpRecover = new DispatcherTimer();
			testMap.TimerOfMpRecover.Tick += TimerOfMpRecover_Tick;
			testMap.TimerOfMpRecover.Interval = TimeSpan.FromSeconds( 10 );
			testMap.TimerOfMpRecover.Start();
			//设置角色HP/MP校正
			TimerOfHMReset = new DispatcherTimer();
			TimerOfHMReset.Tick += ( object sdr, EventArgs ea ) =>
				{
					networkcontrol.AddGameCommand( "reset;" + "1;" + person1.hp + ";" + person1.mp );
					networkcontrol.AddGameCommand( "reset;" + "2;" + person2.hp + ";" + person2.mp );
				};
			TimerOfHMReset.Interval = TimeSpan.FromMilliseconds( 200 );
			TimerOfHMReset.Start();
		}

		private void TimerOfMpRecover_Tick( object sender, EventArgs e )
		{
			Random rad = new Random( (int)DateTime.Now.Ticks );
			while ( true )
			{
				int pid = testMap.freeBlocks[rad.Next( testMap.freeBlocks.Count )];
				MyBlock block = testMap.blocks[pid / numX, pid % numX];
				if ( block.isExistProp == false )
				{
					networkcontrol.AddGameCommand( "drug;" + "m;" + pid );
					setMpDrug( pid );
					return;
				}
			}
		}

		private void setMpDrug( int pid )
		{
			MyBlock block = testMap.blocks[pid / numX, pid % numX];
			block.isExistProp = true;
			block.MPrecover += 100;
			block.Drug = new Image
			{
				Source = source.mapsource.MpDrug,
				Width = gridSize
			};
			this.Carrier.Children.Add( block.Drug );
			Canvas.SetLeft( block.Drug, pid % numX * gridSize );
			Canvas.SetTop( block.Drug, pid / numX * gridSize - gridSize * 0.3 );
			PlayMapEffects( new System.Drawing.Point( pid % numX, pid / numX ) );
		}

		void TimerOfHpRecover_Tick( object sender, EventArgs e )
		{
			Random rad = new Random( (int)DateTime.Now.Ticks );
			while ( true )
			{
				int pid = testMap.freeBlocks[rad.Next( testMap.freeBlocks.Count )];
				MyBlock block = testMap.blocks[pid / numX, pid % numX];
				if ( block.isExistProp == false )
				{
					networkcontrol.AddGameCommand( "drug;" + "h;" + pid );
					setHpDrug( pid );
					return;
				}
			}
		}

		private void setHpDrug( int pid )
		{
			MyBlock block = testMap.blocks[pid / numX, pid % numX];
			block.isExistProp = true;
			block.HPrecover += 100;
			block.Drug = new Image
			{
				Source = source.mapsource.HpDrug,
				Width = gridSize
			};
			this.Carrier.Children.Add( block.Drug );
			Canvas.SetLeft( block.Drug, pid % numX * gridSize );
			Canvas.SetTop( block.Drug, pid / numX * gridSize - gridSize * 0.3 );
			PlayMapEffects( new System.Drawing.Point( pid % numX, pid / numX ) );
		}

		private void GameInit()
		{
			tag1 = 0;
			LoadSource();
			LoadElements();
			DoubleAnimation doubleAnimation = new DoubleAnimation( 0, -160, new Duration( TimeSpan.FromMilliseconds( 2000 ) ) )
			{
				EasingFunction = new BackEase() { EasingMode = EasingMode.EaseInOut }
			};
			doubleAnimation.Completed += ( object sender, EventArgs e ) =>
				{
					this.startmask_1.Visibility = Visibility.Collapsed;
				};
			startmask_1.BeginAnimation( Canvas.LeftProperty, doubleAnimation );
			doubleAnimation = new DoubleAnimation( 320, 640, new Duration( TimeSpan.FromMilliseconds( 2000 ) ) )
			{
				EasingFunction = new BackEase() { EasingMode = EasingMode.EaseInOut }
			};
			doubleAnimation.Completed += ( object sender, EventArgs e ) =>
			{
				this.startmask_2.Visibility = Visibility.Collapsed;
				if ( isServer )
					SetMapEnvironment();
				this.PreviewKeyDown += Carrier_KeyDown;
				bgm.Play();
				TimerOfEffect = new DispatcherTimer();
				TimerOfEffect.Tick += ( object sdr, EventArgs ea ) =>
				{
					Random rad = new Random( (int)DateTime.Now.Ticks );
					System.Drawing.Point p = new System.Drawing.Point( rad.Next( 0, 16 ), rad.Next( 0, 16 ) );
					PlayMapEffects( p );
				};
				TimerOfEffect.Interval = TimeSpan.FromMilliseconds( 5000 );
				TimerOfEffect.Start();
			};
			startmask_2.BeginAnimation( Canvas.LeftProperty, doubleAnimation );
			this.slogan.Visibility = Visibility.Collapsed;
		}

		void networkcontrol_NewCommand()
		{
			this.Dispatcher.BeginInvoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
			 {
				 String str;
				 lock ( networkcontrol.CommandList_receive )
				 {
					 str = networkcontrol.CommandList_receive.Dequeue();
				 }
				 String[] chips = str.Split( ';' );
				 MyPerson person = null;
				 if ( chips[0] == "person" )
				 {
					 if ( chips[1] == "1" )
						 person = person1;
					 else if ( chips[1] == "2" )
						 person = person2;
					 person.position = new System.Drawing.Point( Int32.Parse( chips[2] ), Int32.Parse( chips[3] ) );
					 if ( chips[4] == "left" )
						 person.direction = MyPerson.Direction.Left;
					 if ( chips[4] == "right" )
						 person.direction = MyPerson.Direction.Right;
					 if ( chips[4] == "up" )
						 person.direction = MyPerson.Direction.Up;
					 if ( chips[4] == "down" )
						 person.direction = MyPerson.Direction.Down;
					 MoveOfPerson( person );
				 }
				 else if ( chips[0] == "bomb" )
				 {
					 if ( chips[1] == "1" )
						 person1.mp -= 20;
					 else if ( chips[1] == "2" )
						 person2.mp -= 20;
					 int i = Int32.Parse( chips[4] );
					 TypeOfBomb type = TypeOfBomb.normal;
					 switch ( i )
					 {
						 case 0:
							 type = TypeOfBomb.normal;
							 break;
						 case 1:
							 type = TypeOfBomb.around_small;
							 break;
						 case 2:
							 type = TypeOfBomb.around_big;
							 break;
						 case 3:
							 type = TypeOfBomb.row;
							 break;
						 case 4:
							 type = TypeOfBomb.column;
							 break;
						 case 5:
							 type = TypeOfBomb.row_column;
							 break;
						 case 6:
							 type = TypeOfBomb.diagonal;
							 break;
					 }
					 SetBomb( new System.Drawing.Point( Int32.Parse( chips[2] ), Int32.Parse( chips[3] ) ), 3000, type );
					 Paragraph p = new Paragraph( new Run( "->请注意炸弹！" ) );
					 Tip.Document.Blocks.Add( p );
					 Tip.ScrollToEnd();
				 }
				 else if ( chips[0] == "brick" )
				 {
					 List<System.Drawing.Point> temp = new List<System.Drawing.Point>();
					 for ( int i = 3; i < chips.Length; i++ )
					 {
						 String[] s = chips[i].Split( ',' );
						 temp.Add( new System.Drawing.Point( Int32.Parse( s[0] ), Int32.Parse( s[1] ) ) );
					 }
					 if ( chips[1] == "creat" )
						 RefreshBlock( temp, true );
					 else if ( chips[1] == "delete" )
						 RefreshBlock( temp, false );
				 }
				 else if ( chips[0] == "drug" )
				 {
					 int pid = Int32.Parse( chips[2] );
					 if ( chips[1] == "h" )
						 setHpDrug( pid );
					 else if ( chips[1] == "m" )
						 setMpDrug( pid );
					 Paragraph p = new Paragraph( new Run( "->新的药品出现" ) );
					 Tip.Document.Blocks.Add( p );
					 Tip.ScrollToEnd();
				 }
				 else if ( chips[0] == "reset" )
				 {
					 if ( chips[1] == "1" )
					 {
						 person1.hp = Int32.Parse( chips[2] );
						 person1.mp = Int32.Parse( chips[3] );
					 }
					 else if ( chips[1] == "2" )
					 {
						 person2.hp = Int32.Parse( chips[2] );
						 person2.mp = Int32.Parse( chips[3] );
					 }
				 }
				 else if ( chips[0] == "start!" )
				 {
					 Paragraph p = new Paragraph( new Run( "->游戏开始！" ) );
					 Tip.Document.Blocks.Add( p );
					 Tip.ScrollToEnd();
					 GameInit();
				 }
				 else
				 {
					 Paragraph p;
					 if ( str.StartsWith( "Client" ) )
					 {
						 p = new Paragraph( new Run( str ) { Foreground = new SolidColorBrush( Colors.Green ) } );
					 }
					 else
						 p = new Paragraph( new Run( str ) { Foreground = new SolidColorBrush( Colors.Red ) } );
					 Message.Document.Blocks.Add( p );
					 Message.ScrollToEnd();
				 }
			 } );
		}

		void networkcontrol_NewRequest()
		{
			this.Dispatcher.BeginInvoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
			 {
				 String str;
				 lock ( networkcontrol.RequestList_receive )
				 {
					 str = networkcontrol.RequestList_receive.Dequeue();
				 }
				 String[] chips = str.Split( ';' );
				 if ( chips[0] == "person" && chips[1] == "2" )
				 {
					 if ( chips[2] == "left" )
					 {
						 if ( testMap.isBlockFree( person2.position.Y, person2.position.X - 1 ) )
						 {
							 person2.position.X--;
							 person2.direction = MyPerson.Direction.Left;
							 networkcontrol.AddGameCommand( "person;" + 2 + ";" + person2.position.X + ";" + person2.position.Y + ";" + "left" );
							 MoveOfPerson( person2 );
						 }
					 }
					 else if ( chips[2] == "right" )
					 {
						 if ( testMap.isBlockFree( person2.position.Y, person2.position.X + 1 ) )
						 {
							 person2.position.X++;
							 person2.direction = MyPerson.Direction.Right;
							 networkcontrol.AddGameCommand( "person;" + 2 + ";" + person2.position.X + ";" + person2.position.Y + ";" + "right" );
							 MoveOfPerson( person2 );
						 }
					 }
					 else if ( chips[2] == "up" )
					 {
						 if ( testMap.isBlockFree( person2.position.Y - 1, person2.position.X ) )
						 {
							 person2.position.Y--;
							 person2.direction = MyPerson.Direction.Up;
							 networkcontrol.AddGameCommand( "person;" + 2 + ";" + person2.position.X + ";" + person2.position.Y + ";" + "up" );
							 MoveOfPerson( person2 );
						 }
					 }
					 else if ( chips[2] == "down" )
					 {
						 if ( testMap.isBlockFree( person2.position.Y + 1, person2.position.X ) )
						 {
							 person2.position.Y++;
							 person2.direction = MyPerson.Direction.Down;
							 networkcontrol.AddGameCommand( "person;" + 2 + ";" + person2.position.X + ";" + person2.position.Y + ";" + "down" );
							 MoveOfPerson( person2 );
						 }
					 }
					 else if ( chips[2] == "bomb" )
					 {
						 if ( !testMap.isBombExist( getExactPosition( person2 ).Y, getExactPosition( person2 ).X ) && person2.mp > 20 )
						 {
							 System.Drawing.Point p = getExactPosition( person2 );
							 networkcontrol.AddGameCommand( "bomb;" + "2;" + p.X + ";" + p.Y + ";" + chips[3] );
							 person2.mp -= 20;
							 int i = Int32.Parse( chips[3] );
							 TypeOfBomb type = TypeOfBomb.normal;
							 switch ( i )
							 {
								 case 0:
									 type = TypeOfBomb.normal;
									 break;
								 case 1:
									 type = TypeOfBomb.around_small;
									 break;
								 case 2:
									 type = TypeOfBomb.around_big;
									 break;
								 case 3:
									 type = TypeOfBomb.row;
									 break;
								 case 4:
									 type = TypeOfBomb.column;
									 break;
								 case 5:
									 type = TypeOfBomb.row_column;
									 break;
								 case 6:
									 type = TypeOfBomb.diagonal;
									 break;
							 }
							 SetBomb( p, 3000, type );
						 }
					 }
				 }
				 else if ( chips[0] == "ready" )
				 {
					 Console.WriteLine( "ready" );
					 creatGameDialog.Dispatcher.BeginInvoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
						  {
							 creatGameDialog.Confirm.IsEnabled = true;
						 } );
				 }
				 else
				 {
					 Paragraph p = new Paragraph( new Run( str ) );
					 Message.Document.Blocks.Add( p );
					 Message.ScrollToEnd();
				 }
			 } );
		}

		private void LoadElements()
		{
			this.gridSize = Carrier.Width / numX;//确定地图格子大小
			Bomb.gridSize = this.gridSize;//
			testMap = new MyMap( numX, numY, (int)Carrier.Width, (int)Carrier.Height );
			person1 = new MyPerson( gridSize, source.personsource1.body, new System.Drawing.Point( 0, 0 ), testMap );
			testMap.personBlocks.Add( person1 );
			person2 = new MyPerson( gridSize, source.personsource2.body, new System.Drawing.Point( 15, 15 ), testMap );
			testMap.personBlocks.Add( person2 );
			person1.Width = person1.Height = gridSize;
			person2.Width = person2.Width = gridSize;
			person2.Faction.Fill = new SolidColorBrush( Colors.Blue );
			person2.Faction.Text = "主角2";
			LoadMap();//加载地图
			LoadPerson( person1 );
			person1.xspeed = 400;
			person1.yspeed = 400;
			LoadPerson( person2 );
			person2.xspeed = 400;
			person2.yspeed = 400;//加载人物
		}

		private void sendCreatBlockCommand( List<System.Drawing.Point> re )
		{
			String str = "brick;" + "creat;" + re.Count;
			for ( int i = 0; i < re.Count; i++ )
				str += ";" + re[i].X + "," + re[i].Y;
			networkcontrol.AddGameCommand( str );
		}

		private void sendDeleteBlockCommand( List<System.Drawing.Point> re )
		{
			String str = "brick;" + "delete;" + re.Count;
			for ( int i = 0; i < re.Count; i++ )
				str += ";" + re[i].X + "," + re[i].Y;
			networkcontrol.AddGameCommand( str );
		}

		private void LoadPerson( MyPerson person )
		{
			Carrier.Children.Add( person );
			Binding bid = new Binding( "X" );
			bid.Source = person;
			bid.Mode = BindingMode.TwoWay;
			person.SetBinding( Canvas.LeftProperty, bid );
			bid = new Binding( "Y" );
			bid.Source = person;
			bid.Mode = BindingMode.TwoWay;
			person.SetBinding( Canvas.TopProperty, bid );
			Canvas.SetLeft( person, person.position.X * gridSize );
			Canvas.SetTop( person, person.position.Y * gridSize );
			Canvas.SetZIndex( person, person.Zindex );
		}

		private void LoadMap()
		{
			//加载地图背景
			testMap.MapBackground.Source = source.mapsource.background_p;
			Carrier.Children.Add( testMap.MapBackground );
			Canvas.SetLeft( testMap.MapBackground, 0 );
			Canvas.SetTop( testMap.MapBackground, 0 );
			//循环加载块贴图
			testMap.brick_p = source.mapsource.brick_p;
			for ( int i = 0; i < numY; i++ )
				for ( int j = 0; j < numX; j++ )
				{
					Carrier.Children.Add( testMap.blocks[i, j].rect );
					Canvas.SetLeft( testMap.blocks[i, j].rect, j * gridSize );
					Canvas.SetTop( testMap.blocks[i, j].rect, i * gridSize - gridSize * testMap.blocks[i, j].positionOffset );
					Canvas.SetZIndex( testMap.blocks[i, j].rect, i );
					RefreshBlock( j, i );
				}
			//加载技能图标
			s1.Source = source.bombsource.skill1;
			s2.Source = source.bombsource.skill2;
			s3.Source = source.bombsource.skill3;
			s4.Source = source.bombsource.skill4;
			s5.Source = source.bombsource.skill5;
			s6.Source = source.bombsource.skill6;
			s7.Source = source.bombsource.skill7;
		}

		private void LoadSource()
		{
			source = new Source( 0 );
			Bomb.bomb_img.Add( source.bombsource.bomb );
			Bomb.bomb_effects.Add( source.bombsource.effect );
			bgm.Open( new Uri( @"Source\MusicSource\snowBGM.mp3", UriKind.Relative ) );
		}

		private void RefreshBlock( int X, int Y )
		{
			testMap.blocks[Y, X].rect.Source = testMap.blocks[Y, X].imageSource;
			Canvas.SetZIndex( testMap.blocks[Y, X].rect, testMap.blocks[Y, X].Zindex );
			DoubleAnimation doubleAnimation = new DoubleAnimation( testMap.blocks[Y, X].rect.Opacity, testMap.blocks[Y, X].opc, new Duration( TimeSpan.FromMilliseconds( 500 ) ) );
			testMap.blocks[Y, X].rect.BeginAnimation( Image.OpacityProperty, doubleAnimation );
		}

		private void RefreshBlock( MyBlock blc_tmp )
		{
			blc_tmp.rect.Source = blc_tmp.imageSource;
			Canvas.SetZIndex( blc_tmp.rect, blc_tmp.Zindex );
			DoubleAnimation doubleAnimation = new DoubleAnimation( blc_tmp.rect.Opacity, blc_tmp.opc, new Duration( TimeSpan.FromMilliseconds( 500 ) ) );
			blc_tmp.rect.BeginAnimation( Image.OpacityProperty, doubleAnimation );
		}

		private void RefreshBlock( List<System.Drawing.Point> block_tmp, bool flag )
		{
			Thread t = new Thread( () =>
				 {
					 for ( int i = 0; i < block_tmp.Count; i++ )
					 {
						 this.Dispatcher.Invoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
						 {
							 if ( flag )
							 {
								 testMap.ChangeToBrick( block_tmp[i] );
							 }
							 else
								 testMap.ChangeToFree( block_tmp[i] );
							 RefreshBlock( testMap.blocks[block_tmp[i].Y, block_tmp[i].X] );
						 } );//然而我并不知道为什么不能用BeginInvoke...
						Thread.Sleep( 40 );
					 }
				 } );
			t.IsBackground = true;
			t.Start();
		}

		private void RefreshMap()
		{
			for ( int i = 0; i < numY; i++ )
				for ( int j = 0; j < numX; j++ )
				{
					RefreshBlock( j, i );
				}
		}

		private void Carrier_KeyDown( object sender, KeyEventArgs e )
		{
			//对键盘事件的响应处理
			if ( isServer )
			{
				switch ( e.Key )
				{
					case Key.Left:
						if ( testMap.isBlockFree( person1.position.Y, person1.position.X - 1 ) )
						{
							person1.position.X--;
							person1.direction = MyPerson.Direction.Left;
							networkcontrol.AddGameCommand( "person;" + 1 + ";" + person1.position.X + ";" + person1.position.Y + ";" + "left" );
							MoveOfPerson( person1 );
						}
						break;
					case Key.Right:
						if ( testMap.isBlockFree( person1.position.Y, person1.position.X + 1 ) )
						{
							person1.position.X++;
							person1.direction = MyPerson.Direction.Right;
							networkcontrol.AddGameCommand( "person;" + 1 + ";" + person1.position.X + ";" + person1.position.Y + ";" + "right" );
							MoveOfPerson( person1 );
						}
						break;
					case Key.Up:
						if ( testMap.isBlockFree( person1.position.Y - 1, person1.position.X ) )
						{
							person1.position.Y--;
							person1.direction = MyPerson.Direction.Up;
							networkcontrol.AddGameCommand( "person;" + 1 + ";" + person1.position.X + ";" + person1.position.Y + ";" + "up" );
							MoveOfPerson( person1 );
						}
						break;
					case Key.Down:
						if ( testMap.isBlockFree( person1.position.Y + 1, person1.position.X ) )
						{
							person1.position.Y++;
							person1.direction = MyPerson.Direction.Down;
							networkcontrol.AddGameCommand( "person;" + 1 + ";" + person1.position.X + ";" + person1.position.Y + ";" + "down" );
							MoveOfPerson( person1 );
						}
						break;
					case Key.Space:
						{
							if ( !testMap.isBombExist( getExactPosition( person1 ).Y, getExactPosition( person1 ).X ) && person1.mp > 20 )
							{
								System.Drawing.Point p = getExactPosition( person1 );
								networkcontrol.AddGameCommand( "bomb;" + "1;" + p.X + ";" + p.Y + ";" + CurrentSkill );
								person1.mp -= 20;
								TypeOfBomb type = TypeOfBomb.normal;
								switch ( CurrentSkill )
								{
									case 0:
										type = TypeOfBomb.normal;
										break;
									case 1:
										type = TypeOfBomb.around_small;
										break;
									case 2:
										type = TypeOfBomb.around_big;
										break;
									case 3:
										type = TypeOfBomb.row;
										break;
									case 4:
										type = TypeOfBomb.column;
										break;
									case 5:
										type = TypeOfBomb.row_column;
										break;
									case 6:
										type = TypeOfBomb.diagonal;
										break;
								}
								SetBomb( p, 3000, type );
							}
						}
						break;
					case Key.Z:
						CurrentSkill = 0;
						Canvas.SetLeft( sm, 60 );
						break;
					case Key.X:
						CurrentSkill = 1;
						Canvas.SetLeft( sm, 110 );
						break;
					case Key.C:
						CurrentSkill = 2;
						Canvas.SetLeft( sm, 160 );
						break;
					case Key.V:
						CurrentSkill = 3;
						Canvas.SetLeft( sm, 210 );
						break;
					case Key.B:
						CurrentSkill = 4;
						Canvas.SetLeft( sm, 260 );
						break;
					case Key.N:
						CurrentSkill = 5;
						Canvas.SetLeft( sm, 310 );
						break;
					case Key.M:
						CurrentSkill = 6;
						Canvas.SetLeft( sm, 360 );
						break;
					default:
						;
						break;
				}
			}
			else
			{
				switch ( e.Key )
				{
					case Key.Left:
						networkcontrol.AddGameRequest( "person;" + 2 + ";" + "left" );
						break;
					case Key.Right:
						networkcontrol.AddGameRequest( "person;" + 2 + ";" + "right" );
						break;
					case Key.Up:
						networkcontrol.AddGameRequest( "person;" + 2 + ";" + "up" );
						break;
					case Key.Down:
						networkcontrol.AddGameRequest( "person;" + 2 + ";" + "down" );
						break;
					case Key.Space:
						networkcontrol.AddGameRequest( "person;" + 2 + ";" + "bomb" + ";" + CurrentSkill );
						break;
					case Key.Z:
						CurrentSkill = 0;
						Canvas.SetLeft( sm, 60 );
						break;
					case Key.X:
						CurrentSkill = 1;
						Canvas.SetLeft( sm, 110 );
						break;
					case Key.C:
						CurrentSkill = 2;
						Canvas.SetLeft( sm, 160 );
						break;
					case Key.V:
						CurrentSkill = 3;
						Canvas.SetLeft( sm, 210 );
						break;
					case Key.B:
						CurrentSkill = 4;
						Canvas.SetLeft( sm, 260 );
						break;
					case Key.N:
						CurrentSkill = 5;
						Canvas.SetLeft( sm, 310 );
						break;
					case Key.M:
						CurrentSkill = 6;
						Canvas.SetLeft( sm, 360 );
						break;
				}
			}
			e.Handled = true;//事件路由终止
		}

		private void SetBomb( System.Drawing.Point p, int ms, TypeOfBomb t )
		{
			ThreadPool.QueueUserWorkItem( new WaitCallback( ( object state ) =>
			  {
				  Bomb bomb = null;
				  Image rect = null;
				  this.Dispatcher.Invoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
				 {
					  bomb = new Bomb( p, TimeSpan.FromMilliseconds( ms ), t, testMap );
					  rect = bomb.rect;
					  Carrier.Children.Add( rect );
					  Canvas.SetLeft( rect, Bomb.gridSize * bomb.point.X );
					  Canvas.SetTop( rect, Bomb.gridSize * bomb.point.Y );
					  Canvas.SetZIndex( rect, p.Y );
					  testMap.bombBlocks.Add( bomb );
					  testMap.blocks[p.Y, p.X].isExistBomb = true;
				  } );
				  Thread.Sleep( (int)bomb.time.TotalMilliseconds - 5 );
				  List<System.Drawing.Point> bomb_points = null;
				  List<Image> bomb_effect = null;
				  this.Dispatcher.BeginInvoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
				 {
					  source.musicsource.Bomb.Stop();
					  source.musicsource.Bomb.Play();
					  bomb_points = bomb.getExplodePoint();
					  bomb.frameCounter.Stop();
					  Carrier.Children.Remove( rect );
					  testMap.bombBlocks.Remove( bomb );
					  testMap.blocks[p.Y, p.X].isExistBomb = false;
					  testMap.blocks[p.Y, p.X].isExistFire = true;
					  bomb_effect = new List<Image>();
					  for ( int i = 0; i < bomb_points.Count; i++ )
					  {
						  bomb_effect.Add( new Image()
						  {
							  Source = bomb.bomb_eff[0].Source,
							  Width = gridSize
						  } );
						  Carrier.Children.Add( bomb_effect[i] );
						  Canvas.SetLeft( bomb_effect[i], bomb_points[i].X * gridSize );
						  Canvas.SetTop( bomb_effect[i], bomb_points[i].Y * gridSize );
						  testMap.blocks[bomb_points[i].Y, bomb_points[i].X].isExistFire = true;
						  testMap.blocks[bomb_points[i].Y, bomb_points[i].X].HPrecover -= 10;
					  }
				  } );
				  Thread.Sleep( 3000 );
				  this.Dispatcher.BeginInvoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
				 {
					  for ( int i = 0; i < bomb_points.Count; i++ )
					  {
						  Carrier.Children.Remove( bomb_effect[i] );
						  testMap.blocks[bomb_points[i].Y, bomb_points[i].X].isExistFire = false;
						  testMap.blocks[bomb_points[i].Y, bomb_points[i].X].HPrecover += 10;
					  }
				  } );
			  } ), null );
		}

		private void MoveOfPerson( MyPerson person )
		{
			Point p2 = new Point( ( person.position.X ) * gridSize, ( person.position.Y ) * gridSize );
			Storyboard storyboard = new Storyboard();
			//创建X轴方向动画
			DoubleAnimation doubleAnimation = new DoubleAnimation(
			person.X, p2.X, new Duration( TimeSpan.FromMilliseconds( person.xspeed ) ) );
			Storyboard.SetTarget( doubleAnimation, person );
			Storyboard.SetTargetProperty( doubleAnimation, new PropertyPath( "X" ) );
			storyboard.Children.Add( doubleAnimation );
			//创建Y轴方向动画
			doubleAnimation = new DoubleAnimation(
			  person.Y, p2.Y, new Duration( TimeSpan.FromMilliseconds( person.yspeed ) ) );
			Storyboard.SetTarget( doubleAnimation, person );
			Storyboard.SetTargetProperty( doubleAnimation, new PropertyPath( "Y" ) );
			storyboard.Children.Add( doubleAnimation );
			//将动画动态加载进资源内
			if ( !Resources.Contains( "rectAnimation" ) )
				Resources.Add( "rectAnimation", storyboard );
			//动画播放
			storyboard.Completed += ( object sender, EventArgs e ) =>
			{
				person.action = MyPerson.Actions.stand;
			};
			person.action = MyPerson.Actions.step;
			Canvas.SetZIndex( person, person.position.Y );
			if ( testMap.blocks[person.position.Y, person.position.X].isExistProp )
			{
				person.hp += testMap.blocks[person.position.Y, person.position.X].HPrecover;
				person.mp += testMap.blocks[person.position.Y, person.position.X].MPrecover;
				testMap.blocks[person.position.Y, person.position.X].isExistProp = false;
				testMap.blocks[person.position.Y, person.position.X].HPrecover = 0;
				testMap.blocks[person.position.Y, person.position.X].MPrecover = 0;
				source.musicsource.Recover.Stop();
				source.musicsource.Recover.Play();
				Carrier.Children.Remove( testMap.blocks[person.position.Y, person.position.X].Drug );
			}
			storyboard.Begin();
		}

		private void dispatcherTimer_Tick( object sender, EventArgs e )
		{
			//人物隐身效果
			DoubleAnimation doubleAnimation = new DoubleAnimation( 0.5, 1, new Duration( TimeSpan.FromMilliseconds( 50 ) ) );
			person1.BeginAnimation( Image.OpacityProperty, doubleAnimation );
		}

		private void _updateBlocks( object sender, EventArgs e )
		{
			//随机删除和重建障碍物
			List<System.Drawing.Point> del_temp = testMap.DeleteBrickBlocks( MapRrefreshAmount );
			List<System.Drawing.Point> cre_temp = testMap.CreatBrickBlocks( MapRrefreshAmount );
			sendDeleteBlockCommand( del_temp );
			sendCreatBlockCommand( cre_temp );
			updateBlocks( del_temp, cre_temp );
		}

		private void updateBlocks( List<System.Drawing.Point> del_temp, List<System.Drawing.Point> cre_temp )
		{
			//随机删除和重建障碍物
			RefreshBlock( del_temp, false );
			RefreshBlock( cre_temp, true );
		}

		private System.Drawing.Point getExactPosition( MyPerson person )
		{
			int x = (int)( Canvas.GetLeft( person ) / gridSize );
			int y = (int)( Canvas.GetTop( person ) / gridSize );
			return new System.Drawing.Point( x, y );
		}

		private void PlayMapEffects( System.Drawing.Point p )
		{
			MapEffect me = new MapEffect( source.mapsource.effect, gridSize );
			Carrier.Children.Add( me.rect );
			Canvas.SetBottom( me.rect, ( numY - p.Y - 1 ) * gridSize );
			Canvas.SetLeft( me.rect, ( p.X + 2 ) * gridSize );
			me.end += () =>
				{
					Carrier.Children.Remove( me.rect );
				};
			me.InitThread();
		}

		private void CreatGame_Click( object sender, RoutedEventArgs e )
		{
			creatGameDialog = new CreatGameDialog();
			creatGameDialog.CreatServer.Click += CreatServer_Click;
			creatGameDialog.Confirm.Click += Confirm_Click;
			creatGameDialog.Clear.Click += Clear_Click;
			creatGameDialog.Closed += ( object a, EventArgs b ) =>
			{
				try
				{
					networkcontrol.CreatGame_thread.Abort();
					networkcontrol.WaitClient_thread.Abort();
				}
				catch ( Exception ex ) { };
			};
			creatGameDialog.Confirm.IsEnabled = false;
			creatGameDialog.Owner = this;
			creatGameDialog.ShowDialog();
		}

		void Clear_Click( object sender, RoutedEventArgs e )
		{
			networkcontrol.Clients.Clear();
			creatGameDialog.IPList.Items.Clear();
		}

		void Confirm_Click( object sender, RoutedEventArgs e )
		{
			creatGameDialog.Close();
			MainPerson = person1;
			this.isServer = true;
			networkcontrol.AddGameCommand( "start!" );
			GameInit();//开始游戏
		}

		void CreatServer_Click( object sender, RoutedEventArgs e )
		{
			if ( networkcontrol == null )
			{
				networkcontrol = new NetworkControl();
				networkcontrol.CreatGameServer();
				networkcontrol.NewClient += networkcontrol_NewClient;
			}
			networkcontrol.WaitClient();
		}

		void networkcontrol_NewClient()
		{
			creatGameDialog.Dispatcher.BeginInvoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
			 {
				 creatGameDialog.IPList.Items.Clear();
				 for ( int i = 0; i < networkcontrol.Clients.Count; i++ )
					 creatGameDialog.IPList.Items.Add( networkcontrol.Clients[i].Client.LocalEndPoint.ToString() );
				 networkcontrol.NewRequest += networkcontrol_NewRequest;
				 networkcontrol.StartMessageQueue_server();
			 } );
		}

		private void JoinGame_Click( object sender, RoutedEventArgs e )
		{
			joinGameDialog = new JoinGameDialog();
			joinGameDialog.Find.Click += Find_Click;
			joinGameDialog.Confirm.Click += Confirm_Click_join;
			joinGameDialog.Quit.Click += Quit_Click;
			joinGameDialog.Closed += ( object a, EventArgs b ) =>
			{
				try
				{
					networkcontrol.JoinGame_thread.Abort();
					networkcontrol.LinkServer_thread.Abort();
				}
				catch ( Exception ex ) { };
			};
			joinGameDialog.Owner = this;
			joinGameDialog.ShowDialog();
		}

		void Quit_Click( object sender, RoutedEventArgs e )
		{
			//throw new NotImplementedException();
		}

		private void Confirm_Click_join( object sender, RoutedEventArgs e )
		{
			joinGameDialog.Close();
			networkcontrol.StartMessageQueue_client();
			MainPerson = person2;
			networkcontrol.NewCommand += networkcontrol_NewCommand;
			this.isServer = false;
			networkcontrol.AddGameRequest( "ready;" );
			///开始游戏
		}

		void Find_Click( object sender, RoutedEventArgs e )
		{
			if ( networkcontrol == null )
			{
				networkcontrol = new NetworkControl();
				networkcontrol.NewServer += networkcontrol_NewServer;
			}
			networkcontrol.LinkServer_thread = new Thread( () =>
			 {
				 networkcontrol.CreatGameBroadcastLinker();
			 } );
			networkcontrol.LinkServer_thread.IsBackground = true;
			networkcontrol.LinkServer_thread.Start();
		}

		void networkcontrol_NewServer()
		{
			joinGameDialog.Dispatcher.BeginInvoke( DispatcherPriority.Normal, (ThreadStart)delegate ()
			 {
				 joinGameDialog.IPList.Items.Clear();
				 joinGameDialog.IPList.Items.Add( networkcontrol.broadcastClient.GameServerIEP.ToString() );
			 } );
		}

		private void Message_send_Click( object sender, RoutedEventArgs e )
		{
			if ( isServer )
			{
				networkcontrol.AddGameCommand( "Server->  " + DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ) + "\n  " + Message_temp.Text );
				Message.Document.Blocks.Add( new Paragraph( new Run( "Server->  " + DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ) + "\n  " + Message_temp.Text )
				{
					Foreground = new SolidColorBrush( Colors.Red )
				} ) );
			}
			else
			{
				networkcontrol.AddGameRequest( "Client->  " + DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ) + "\n  " + Message_temp.Text );
				Message.Document.Blocks.Add( new Paragraph( new Run( "Client->  " + DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ) + "\n  " + Message_temp.Text ) { Foreground = new SolidColorBrush( Colors.Green ) } ) );
			}
			Message.ScrollToEnd();
		}

		private void getFocus( object sender, RoutedEventArgs e )
		{
			this.PreviewKeyDown -= Carrier_KeyDown;
		}

		private void lostFocus( object sender, RoutedEventArgs e )
		{
			this.PreviewKeyDown += Carrier_KeyDown;
		}

		private void bgm_slider_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			bgm.Volume = bgm_slider.Value / 100;
		}
	}
}