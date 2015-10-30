using System;
using System.IO;
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
using System.Collections;
using ArrestForever.Role;

namespace ArrestForever
{
    public class MyMap
    {
        public int width_num { get; set; }
        public int height_num { get; set; }
        public double gridSiZe;
        public MyBlock[,] blocks;
        //用于维护地图坐标属性的4个List
        public List<int> freeBlocks = new List<int>();
        public List<int> brickBlocks = new List<int>();
        public List<MyPerson> personBlocks = new List<MyPerson>();
        public List<Bomb> bombBlocks=new List<Bomb>();
        //地图设置
        public DispatcherTimer TimerOfUpdate;
        public DispatcherTimer TimerOfHpRecover;
        public DispatcherTimer TimerOfMpRecover;
        public DispatcherTimer TimerOfEffect;
        //地图背景和块贴图
        public Image MapBackground = new Image();
		public Image decoration1 = new Image();
		public Image decoration2 = new Image();
		public ImageSource brick_p;

		public MyMap(int w, int h, double wl, double hl)
		{
			width_num = w;
			height_num = h;
			blocks = new MyBlock[h, w];
			MapBackground.Width = wl;
			MapBackground.Height = hl;
			gridSiZe = wl / w;
			//循环设置地图块
			for (int i = 0; i < h; i++)
				for (int j = 0; j < w; j++)
				{
					blocks[i, j] = new MyBlock();
					blocks[i, j].rect.Width = gridSiZe;
					freeBlocks.Add(i * w + j);
				}
			for (int i = 0; i < h; i++)
				for (int j = 0; j < w; j++)
				{
					if (blocks[i, j].isFree)
						freeBlocks.Add(i * w + j);
				}
		}
        //随机创建障碍物
        public List<System.Drawing.Point> CreatBrickBlocks(int num)
        {
            List<System.Drawing.Point> rt = new List<System.Drawing.Point>();
            while (num > 0)
            {
                Random rad = new Random((int)DateTime.Now.Ticks - num);
                int Pid = freeBlocks[rad.Next(0, freeBlocks.Count - 1)];
                int w, h;
                w = Pid / width_num;
                h = Pid % width_num;
                if (isPersonExist(w, h)||isBombExist(w,h))
                    continue;
                rt.Add(new System.Drawing.Point(h,w));
                num--;
            }
            return rt;
        }
        //改变为墙
        public void ChangeToBrick(System.Drawing.Point point)
        {
            int w = point.Y;
            int h = point.X;
            this.blocks[w, h].setSource(brick_p);
            this.blocks[w, h].Zindex = 100;
            this.blocks[w, h].opc = 0.8;
            setBlocksUsed(w, h);
        }
        //随机删除障碍物
        public List<System.Drawing.Point> DeleteBrickBlocks(int num)
        {
            List<System.Drawing.Point> rt = new List<System.Drawing.Point>();
            while (num > 0)
            {
                Random rad = new Random((int)DateTime.Now.Ticks - num);
                int Pid = brickBlocks[rad.Next(0, brickBlocks.Count - 1)];
                int w, h;
                w = Pid / width_num;
                h = Pid % width_num;
                rt.Add(new System.Drawing.Point(h,w));
                num--;
            }
            return rt;
        }
        //改变为空
        public void ChangeToFree(System.Drawing.Point point)
        {
            int w = point.Y;
            int h = point.X;
            this.blocks[w, h].Zindex = 0;
            this.blocks[w, h].opc = 0;
            setBlocksFree(w, h);
        }
        //设置某个地图块为空闲状态
        public void setBlocksFree(int w, int h)
        {
            this.blocks[w, h].isFree = true;
            brickBlocks.Remove(w * width_num + h);
            freeBlocks.Add(w * width_num + h);
        }
        //设置某个地图块为使用状态
        public void setBlocksUsed(int w, int h)
        {
            this.blocks[w, h].isFree = false;
            freeBlocks.Remove(w * width_num + h);
            brickBlocks.Add(w * width_num + h);
        }
        //检测指定位置是否存在角色
        public bool isPersonExist(int w, int h)
        {
            for (int i = 0; i < personBlocks.Count; i++)
                if (personBlocks[i].position.Y == w && personBlocks[i].position.X == h)
                    return true;
            return false;
        }
        //检测指定位置是否存在炸弹
        public bool isBombExist(int w, int h)
        {
            for (int i = 0; i < bombBlocks.Count; i++)
            {
                if (bombBlocks[i].point.Y == w && bombBlocks[i].point.X == h)
                    return true;
            }
            return false;
        }
        //检测指定地图块是否为空
        public bool isBlockFree(int w, int h)
        {
			return w >= 0 && w < height_num && h >= 0 && h < width_num && (this.blocks[w, h].isFree) && !(isPersonExist(w, h)) && !isBombExist(w, h);
        }
        //返回镜像地图
        public byte[,] getMirrorMap(bool flag)
        {
            int[] MirroList = new int[brickBlocks.Count];
            brickBlocks.CopyTo(MirroList, 0);
            byte[,] MirrorMap = new byte[width_num, height_num];
            for (int i = 0; i < width_num; i++)
            {
                for (int j = 0; j < height_num; j++)
                    MirrorMap[i, j] = 1;
            }
            for (int i = 0; i < MirroList.Length; i++)
            {
                MirrorMap[MirroList[i] % width_num, MirroList[i] / width_num] = 0;
            }
            if (flag)
            for (int i = 0; i < bombBlocks.Count;i++)
            {
                MirrorMap[bombBlocks[i].point.X, bombBlocks[i].point.Y] = 0;
            }
                return MirrorMap;
        }
    }
    //自定义地图块类
    public class MyBlock
    {
        public bool isFree;
        public bool isExistBomb;
        public bool isExistFire;
        public bool isExistProp;
        public Image rect;
        public double positionOffset = 0.51;
        public int Zindex = 0;
        public double opc = 0;
        public int HPrecover = 0;
        public int MPrecover = 0;
        public ImageSource imageSource = null;
        public Image Drug = null;
        public MyBlock()
        {
            rect = new Image();
            rect.Opacity = 0;
            isFree = true;
            isExistBomb = false;
            isExistFire = false;
            isExistProp = false;
        }
        //加载块贴图
        public void setSource(ImageSource bimg)
        {
            imageSource = bimg;
        }
    }
}
