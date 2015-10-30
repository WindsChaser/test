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
using QX.Game.PathFinder;
using ArrestForever.Role;
namespace ArrestForever.Role
{
    /// <summary>
    /// MyPerson.xaml 的交互逻辑
    /// </summary>
    public partial class MyPerson : UserControl
    {
        public DispatcherTimer timer = new DispatcherTimer();//属性迭代器
		public DispatcherTimer FrameCounter = new DispatcherTimer();
        public int ImageCount = 0;//人物形象帧序列号
        public List<List<ImageSource>> imgsource;//贴图源，（人物动作，人物方向）
        public int RoleID = 1;//人物形象编号
        public Direction direct = Direction.Right;//人物运动方向编号
        public Actions action = Actions.stand;//人物当前动作
        public Actions old_action = Actions.stand;//旧·人物当前动作
        public int Zindex = 50;//画面表现层次
        public double gridSize;//格子大小
        public int xspeed;//横轴速度，移动一个格子所需要的时间
        public int yspeed;//纵轴速度，移动一个格子所需要的时间
        public System.Drawing.Point position;//当前逻辑定位格子坐标
        public MyMap map;
        /// <summary>
        /// 人物生命值和魔法值
        /// </summary>
        public int MAX_HP;
        public int MAX_MP;
        public int Hp;
        public int Mp;
        /// <summary>
        /// 下面是AI才会用到的东西
        /// </summary>
        public int AiAmount = 400;//AI专用，更新寻径算法的时间间隔
        public int AiSpeed = 300;//AI专用，每移动一格所需时间
        public List<Rectangle> rails = new List<Rectangle>();//移动轨迹点集合
        public DispatcherTimer TimerOfFind;//定时寻找目标并更新移动轨迹
        /// <summary>
        /// 以下是枚举类型和属性定义
        /// </summary>
        public enum Actions
        {
            stand, step, death
        }

        public enum Direction
        {
            Left,Right,Up,Down
        }

		public Direction direction
		{
			get { return direct; }
			set { direct = value; ImageCount = 0; }
		}

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public static readonly DependencyProperty XProperty = DependencyProperty.Register(
            "X", //属性名
            typeof(double), //属性类型
            typeof(MyPerson), //属性主人类型
            new FrameworkPropertyMetadata(
                 0.0, //初始值0
                 FrameworkPropertyMetadataOptions.None, //不特定界面修改
            //不需要属性改变回调
                 null,//new PropertyChangedCallback(QXSpiritInvalidated),
            //不使用强制回调
                 null
                 )
            );

        //精灵Y坐标(关联属性)
        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public static readonly DependencyProperty YProperty = DependencyProperty.Register(
            "Y",
            typeof(double),
            typeof(MyPerson),
            new FrameworkPropertyMetadata(
                 0.0,
                 FrameworkPropertyMetadataOptions.None,
                 null,
                 null
                 )
            );

        public int hp
        {
            get { return Hp; }
            set
            {
                if (value > MAX_HP)
                    Hp = MAX_HP;
                else if (value < 0)
                    Hp = 0;
                else Hp = value;
                blood.fill.Width = (double)Hp / MAX_HP * 482;
            }
        }

        public int mp
        {
            get { return Mp; }
            set
            {
                if (value > MAX_MP)
                    Mp = MAX_MP;
                else if (value < 0)
                    Mp = 0;
                else Mp = value;
                magic.fill.Width = (double)Mp / MAX_MP * 482;
            }
        }
        /// <summary>
        /// 以下是函数部分
        /// </summary>
        /// <param name="gridSize"></param>
        /// <param name="bimg"></param>
        /// <param name="p"></param>
        public MyPerson(double gridSize, List<List<ImageSource>> bimg, System.Drawing.Point p,MyMap map)
        {
            InitializeComponent();
            //设置布局信息
            this.map = map;
            this.gridSize = gridSize;
            imgsource = bimg;
            this.X = p.X * gridSize;
            this.Y = p.Y * gridSize;
            position = p;
            //设置生命值、魔法值信息
            MAX_HP = 300;
            MAX_MP = 300;
            hp = MAX_HP;
            mp = MAX_MP;
			action = Actions.stand;
			direction = Direction.Up;
            InitThread();
        }

        private void InitThread()
        {
			FrameCounter.Tick += new EventHandler(UpdeteImage);
			FrameCounter.Interval = TimeSpan.FromMilliseconds(95);
			FrameCounter.Start();
            timer.Tick += new EventHandler(HurtJudge);
            timer.Interval = TimeSpan.FromMilliseconds(20);
            timer.Start();
        }

        private void UpdeteImage(object sender, EventArgs e)
        {
			if (action == Actions.step)
			{
				body.Source = imgsource[(int)direction][ImageCount];
				ImageCount++;
				ImageCount = (ImageCount == imgsource[(int)direction].Count) ? 0 : ImageCount;
			}
			else
			{
				ImageCount = 0;
				body.Source = imgsource[(int)direction][0];
			}
        }

        private void HurtJudge(object sender, EventArgs e)
        {
            if (map.blocks[(int)(Y / gridSize), (int)(X / gridSize)].isExistFire)
            hp += map.blocks[(int)(Y / gridSize), (int)(X / gridSize)].HPrecover;
        }
    }
}
