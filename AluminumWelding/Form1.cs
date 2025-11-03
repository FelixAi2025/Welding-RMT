using Advantech.Adam;
using Cognex.VisionPro;//添加Cog视觉库命名空间
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.PMAlign;//添加Cog视觉库命名空间
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;//添加Cog视觉库命名空间
using NLog;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;



namespace AluminumWelding
{
    public partial class Form1 : Form
    {
        #region fields and initialization
        private float X;//当前窗体的宽度自适应窗体
        private float Y;//当前窗体的高度 自适应窗体
        static bool gAlarmReset, stopAlarmReset;
        static string gFormateFullName;
        static int numRecord;//
        static readonly Logger parentLog = LogManager.GetCurrentClassLogger();


        //20250929 bool difStartFault, difEndFault;

        //线程定时器        
        /*  20250326  static Adam2650 adam2650Module;
        static AutoResetEvent autoResetEvent1;        
         static CamerAlgorithm Camer1Alg, Camer2Alg, Camer3Alg, Camer4Alg;*/
        Adam2650 adam2650Module;
        AutoResetEvent autoResetEvent1;
        CamerAlgorithm Camer1Alg; //20250929 , Camer2Alg, Camer3Alg, Camer4Alg;

        System.Threading.Timer timer1;
        delegate void WinformHandler(string value);
        int invokeCount;//考虑非静态
        DateTime beforDTThreading, afterDTThreading; //20250320, beforDTAlarm, afterDTAlarm;
        TimeSpan tsThreading; //20250320, tsAlarm;
        bool fixFault, running, toStop, cam1Used, cam2Used, cam3Used, cam4Used;//20250320, alarmOutput1, alarmOutput2, alarmOutput3, alarmOutput4 , alarmRsPassed1, alarmRsPassed2, alarmRsPassed3, alarmRsPassed4;
        int iii; //, jjj;
        CamerFixture Camer1Fix; //20250929, Camer2Fix, Camer3Fix, Camer4Fix;
        bool alarmming;
        //删除早期图片
        string driveLetter;
        string targetFolder;
        double spaceThreshold;



        public Form1()
        {
            InitializeComponent();

            iii = 1;

            //X = this.Width;//获取窗体的宽度//自适应窗体
            //Y = this.Height;//获取窗体的高度//自适应窗体

            gFormateFullName = @"C:\Users\Admin\Image Formate\Formate.jpg";

            Camer1Alg = new CamerAlgorithm("/CogAcqFifoTool11.vpp", "/ToolGroup运行.vpp", cogRecordDisplay1, textBox1, label7, label11, "图片");

            //亚当模块
            adam2650Module = new Adam2650("10.0.0.1", 502); //IP address and  modbus TCP port: 502
            //删除早期图片
            driveLetter = "D:";  //指定要检测的驱动器
            targetFolder = @"D:\1现场图";  // 要删除的文件地址
            //剩余剩余空间阈值，这里设为1T。 1GB（1 * 1024 * 1024 * 1024    字节）
            spaceThreshold = 3 * 1024.0 * 1024.0 * 1024.0 * 1024.0;

        }



        #endregion


        #region 自适应窗体

        private void Form1_Load(object sender, EventArgs e)
        {

            X = this.Width;//获取窗体的宽度	
            Y = this.Height;//获取窗体的高度	
            // setTag(this);//调用方法	
        }

        private void Form1_Resize(object sender, EventArgs e)//这一句需 右击窗体空白处》属性》事件》双击 Resize》出现这个方法》里面写入代码》》》》》》》
        {
            float newx = (this.Width) / X; //窗体宽度缩放比例
            float newy = (this.Height) / Y;//窗体高度缩放比例
            setControls(newx, newy, this);//随窗体改变控件大小
        }
        private void setTag(Control cons) //自适应窗体
        {
            foreach (Control con in cons.Controls)//循环窗体中的控件
            {
                con.Tag = con.Width + ":" + con.Height + ":" + con.Left + ":" + con.Top + ":" + con.Font.Size;
                if (con.Controls.Count > 0)
                    setTag(con);
            }
        }

        private void setControls(float newx, float newy, Control cons) //自适应窗体
        {

            //遍历窗体中的控件，重新设置控件的值
            foreach (Control con in cons.Controls)
            {

                if (con.Tag == null) continue; if (con.Tag == null) continue; //需要这一句，否则报异常。 或者用try{}catch(Exception Excep){;} 忽视异常

                string[] mytag = con.Tag.ToString().Split(new char[] { ':' });//获取控件的Tag属性值，并分割后存储字符串数组
                float a = System.Convert.ToSingle(mytag[0]) * newx;//根据窗体缩放比例确定控件的值，宽度
                con.Width = (int)a;//宽度
                a = System.Convert.ToSingle(mytag[1]) * newy;//高度
                con.Height = (int)(a);
                a = System.Convert.ToSingle(mytag[2]) * newx;//左边距离
                con.Left = (int)(a);
                a = System.Convert.ToSingle(mytag[3]) * newy;//上边缘距离
                con.Top = (int)(a);
                Single currentSize = System.Convert.ToSingle(mytag[4]) * newy;//字体大小
                con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                if (con.Controls.Count > 0)
                {
                    setControls(newx, newy, con);
                }
            }

        }


        #endregion


        #region buttons

        private void button1_Click(object sender, EventArgs e) //关闭窗口
        {
            stop();
            //20250506
            //20251017临时 FolderDelete FolderDelete1 = new FolderDelete(driveLetter, targetFolder, spaceThreshold);
            //20251017临时 FolderDelete1.deletePicture();
            // 关闭亚当模块
            adam2650Module.Output(3, 0);
            adam2650Module.Closing();
            //20250326 LogManager.Shutdown();
            this.Dispose(); //完成资源释放和清理，然后关闭form窗口
            System.Environment.Exit(0);  ////强制退出，立即终止整个应用程序，包括线程。

        }

        private void button2_Click(object sender, EventArgs e)//timer call //启动
        {
            Console.WriteLine("\n 启动开始.");
            cam1Used = cam2Used = cam3Used = cam4Used = false; //20250402
            stopAlarmReset = gAlarmReset = false;//20250310
            toStop = false;//20250320 
            Camer1Fix = new CamerFixture("/CogAcqFifoTool11.vpp", "/ToolGroup定位.vpp", cogRecordDisplay1, textBox1, label7, "定位图片"); //20250319 label8, "1#西南");
            //20250929
            //Camer2Fix = new CamerFixture("/CogAcqFifoTool22.vpp", "/ToolGroup交联后相机Common定位.vpp", cogRecordDisplay2, textBox2, label22, "2#东南"); //20250319  label8, "2#东南");
            //Camer3Fix = new CamerFixture("/CogAcqFifoTool33.vpp", "/ToolGroup交联后相机Common定位.vpp", cogRecordDisplay3, textBox3, label24, "3#东北"); //20250319  label8, "3#东北");
            //Camer4Fix = new CamerFixture("/CogAcqFifoTool44.vpp", "/ToolGroup交联后相机Common定位.vpp", cogRecordDisplay4, textBox4, label26, "4#西北"); //20250319  label8, "4#西北");

            Camer1Fix.Run();
            ////20250310            while (!Camer1Fix.CamerFixed) { Console.WriteLine("相机1 定位问题。。。。。。。。。。。。。。。。。。。。。。。。。"); }
            if (Camer1Fix.FixFault) { Camer1Fix.labelAlarmInfo.ForeColor = Color.Red; goto FixFault; }  ////20250310  
            //20250929
            //Camer2Fix.Run();
            //if (Camer2Fix.FixFault) { Camer2Fix.labelAlarmInfo.ForeColor = Color.Red; goto FixFault; }  ////20250310    ////20250311 
            //Camer3Fix.Run();
            //if (Camer3Fix.FixFault) { Camer3Fix.labelAlarmInfo.ForeColor = Color.Red; goto FixFault; }  ////20250310   ////20250311 
            //Camer4Fix.Run();
            //if (Camer4Fix.FixFault) { Camer4Fix.labelAlarmInfo.ForeColor = Color.Red; goto FixFault; }  ////20250310    ////20250311

            //20250929  int milSeconds = checkEdge();//找边是否正确   milSeconds用于拍照频率    
            //20250929 
            /*
            if (difStartFault)
            {
                //20250326Console.WriteLine("Fix问题:4个图片，上边距离相差太大。缺陷或中心不对");
                parentLog.Warn("Fix问题:4个图片，上边距离相差太大。缺陷或中心不对");
                Camer1Fix.labelAlarmInfo.ForeColor = Color.Red;
                Camer1Fix.labelAlarmInfo.Text = "Fix边问题:4个图片，上边距离相差太大。缺陷或中心不对";
                //20250318临时                goto FixFault;
            }
            if (difEndFault)
            {
                //20250326Console.WriteLine("Fix问题:4个图片，下边距离相差太大。缺陷或中心不对");
                parentLog.Warn("Fix问题:4个图片，下边距离相差太大。缺陷或中心不对");
                Camer1Fix.labelAlarmInfo.ForeColor = Color.Red;
                Camer1Fix.labelAlarmInfo.Text = "Fix问题:4个图片，下边距离相差太大。缺陷或中心不对";
                //20250318临时     goto FixFault;
            }
            */
            fixFault = false;
            polygonDataTransfor();
            Camer1Fix = null;
            //20250929 
            //Camer2Fix = null;
            //Camer3Fix = null;
            //Camer4Fix = null;
            GC.Collect();//20250303

            Camer1Alg.PolygonSet();//20250303 
            //20250929 
            //Camer2Alg.PolygonSet();//20250304 
            //Camer3Alg.PolygonSet();//20250304 
            //Camer4Alg.PolygonSet();//20250304 

            //20250411 iii = 1;
            //20250411 invokeCount = 1;
            autoResetEvent1 = new AutoResetEvent(false);
            TimerCallback timeCallBack1 = thread1Method;//RunMethod参数为Object,所以不用()? 还是callback实例只能用无()的方法赋值？
            //   Console.WriteLine("{0} 新定时器启动.\n", DateTime.Now.ToString("HH:mm:ss.fff"));
            //20250929 timer1 = new System.Threading.Timer(timeCallBack1, autoResetEvent1, 00, milSeconds); //时间设定 //20250321  500);//
            timer1 = new System.Threading.Timer(timeCallBack1, autoResetEvent1, 00, 500); //时间设定 //20250321  500);//
            running = true;
            //20250326 Console.WriteLine("\n 启动成功.");
            parentLog.Info("\n 启动成功.");
            Console.WriteLine("\n 启动成功.");
            return;
        FixFault:
            parentLog.Info("\n 启动失败.");
            Console.WriteLine("\n 启动失败.");
            fixFault = true;
            adam2650Module.Output(3, 1);
            //20250319  label8.ForeColor = Color.Red;
        }
        private void button3_Click(object sender, EventArgs e)//停止
        {
            stop();
        }
        private void button4_Click(object sender, EventArgs e)  //报警复位
        {
            gAlarmReset = true;
            //20250320 beforDTAlarm = System.DateTime.Now; //20250319
            //20250929  Camer1Alg.AlarmRsPassed = Camer2Alg.AlarmRsPassed = Camer3Alg.AlarmRsPassed = Camer4Alg.AlarmRsPassed = false;//20250320
            Camer1Alg.AlarmRsPassed = false;//20250320
            //220250506 Application.DoEvents();//220250427a 
            if (fixFault)
            {
                adam2650Module.Output(3, 0);
                //20250929  Camer1Fix.labelAlarmInfo.ForeColor = Camer2Fix.labelAlarmInfo.ForeColor = Camer3Fix.labelAlarmInfo.ForeColor = Camer4Fix.labelAlarmInfo.ForeColor = Color.Black; //20250320
                Camer1Fix.labelAlarmInfo.ForeColor = Color.Black; //20250320
                Camer1Fix.labelAlarmInfo.Text = "焊接缺陷：" + Camer1Fix.RunningInfo + "，报警复位状态";  ////20250320 label7
                //20250929 
                //Camer2Fix.labelAlarmInfo.Text = "2#东南：" + Camer2Fix.RunningInfo + "，报警复位状态"; //20250320label22
                //Camer3Fix.labelAlarmInfo.Text = "3#东北：" + Camer3Fix.RunningInfo + "，报警复位状态";//20250320 label24
                //Camer4Fix.labelAlarmInfo.Text = "4#西北：" + Camer4Fix.RunningInfo + "，报警复位状态";//20250320 label26
            }
            else if (!running)
            {
                adam2650Module.Output(3, 0);

                label8.ForeColor = Color.Black; //20250320
                if (stopAlarmReset) return;//20250423 
                if (Camer1Alg.Alarm)
                {
                    Camer1Alg.labelAlarmInfo.ForeColor = Color.Black;  // Label7                
                    //20250411 Camer1Alg.labelAlarmInfo.Text = "1#西南：" + Camer1Alg.RunningInfo + ", 报警复位状态";
                    Camer1Alg.labelAlarmInfo.Text = Camer1Alg.labelAlarmInfo.Text + ", 报警复位状态";
                }
                //20250929 
                //if (Camer2Alg.Alarm)
                //{
                //    Camer2Alg.labelAlarmInfo.ForeColor = Color.Black; //Label22
                //    //20250411 Camer2Alg.labelAlarmInfo.Text = "2#东南：" + Camer2Alg.RunningInfo + ", 报警复位状态";
                //    Camer2Alg.labelAlarmInfo.Text = Camer2Alg.labelAlarmInfo.Text + ", 报警复位状态";
                //}
                //if (Camer3Alg.Alarm)
                //{
                //    Camer3Alg.labelAlarmInfo.ForeColor = Color.Black;  //Label24
                //    //20250411 Camer3Alg.labelAlarmInfo.Text = "3#东北：" + Camer3Alg.RunningInfo + ", 报警复位状态";
                //    Camer3Alg.labelAlarmInfo.Text = Camer3Alg.labelAlarmInfo.Text + ", 报警复位状态";
                //}
                //if (Camer4Alg.Alarm)
                //{
                //    Camer4Alg.labelAlarmInfo.ForeColor = Color.Black;  //Label26
                //    //20250411 Camer4Alg.labelAlarmInfo.Text = "4#西北：" + Camer4Alg.RunningInfo + ", 报警复位状态";
                //    Camer4Alg.labelAlarmInfo.Text = Camer4Alg.labelAlarmInfo.Text + ", 报警复位状态";
                //}
                stopAlarmReset = true;//20250423
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            numRecord = 0;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            numRecord = 2;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            numRecord = 3;
        }



        /*
int checkEdge()
{
//{ ///20250321
double difCable1EdgeStartX = Math.Abs(Camer1Fix.X0 - Camer1Fix.X2);
double difCable1EdgeEndX = Math.Abs(Camer1Fix.X1 - Camer1Fix.X3);
//220250427a    labelWidUp1.Text = difCable1EdgeStartX.ToString("F1");
//220250427a labelWidDn1.Text = difCable1EdgeEndX.ToString("F1");

double difCable2EdgeStartX = Math.Abs(Camer2Fix.X0 - Camer2Fix.X2);
double difCable2EdgeEndX = Math.Abs(Camer2Fix.X1 - Camer2Fix.X3);
//220250427a  labelWidUp2.Text = difCable2EdgeStartX.ToString("F1");
//220250427a  labelWidDn2.Text = difCable2EdgeEndX.ToString("F1");

double difCable3EdgeStartX = Math.Abs(Camer3Fix.X0 - Camer3Fix.X2);
double difCable3EdgeEndX = Math.Abs(Camer3Fix.X1 - Camer3Fix.X3);
//220250427a  labelWidUp3.Text = difCable3EdgeStartX.ToString("F1");
//220250427a   labelWidDn3.Text = difCable3EdgeEndX.ToString("F1");

double difCable4EdgeStartX = Math.Abs(Camer4Fix.X0 - Camer4Fix.X2);
double difCable4EdgeEndX = Math.Abs(Camer4Fix.X1 - Camer4Fix.X3);
//220250427a  labelWidUp4.Text = difCable4EdgeStartX.ToString("F1");
//220250427a  labelWidDn4.Text = difCable4EdgeEndX.ToString("F1");

double[] difStart = { difCable1EdgeStartX, difCable2EdgeStartX, difCable3EdgeStartX, difCable4EdgeStartX };
double difStartMax = difStart.Max();
double difStartMin = difStart.Min();


double[] difEnd = { difCable1EdgeEndX, difCable2EdgeEndX, difCable3EdgeEndX, difCable4EdgeEndX };
double difEndMax = difEnd.Max();
double difEndMin = difEnd.Min();


if ((difStartMax - difStartMin) > difStartMin * 0.15)
{
    difStartFault = true;
}
else
{
    difStartFault = false;
}
if ((difEndMax - difEndMin) > difEndMin * 0.85)
{
    difEndFault = true;
}
else
{
    difEndFault = false;
}

///20250321 根据电缆宽度，计算运行速度
double widBtmAverage = difStart.Average(); 

int milSeconds = (int)Math.Round(125 + (widBtmAverage - 230) * (830 - 230) / (500 - 125));
if (milSeconds < 125) milSeconds = 125;
else if (milSeconds > 500) milSeconds = 500;
return milSeconds;
}
*/

        void polygonDataTransfor()
        {
            Camer1Alg.X0 = Camer1Fix.X0;
            Camer1Alg.Y0 = Camer1Fix.Y0;
            Camer1Alg.X1 = Camer1Fix.X1;
            Camer1Alg.Y1 = Camer1Fix.Y1;
            Camer1Alg.X2 = Camer1Fix.X2;
            Camer1Alg.Y2 = Camer1Fix.Y2;
            Camer1Alg.X3 = Camer1Fix.X3;
            Camer1Alg.Y3 = Camer1Fix.Y3;
            //20250929 
            /*
            Camer2Alg.X0 = Camer2Fix.X0;
            Camer2Alg.Y0 = Camer2Fix.Y0;
            Camer2Alg.X1 = Camer2Fix.X1;
            Camer2Alg.Y1 = Camer2Fix.Y1;
            Camer2Alg.X2 = Camer2Fix.X2;
            Camer2Alg.Y2 = Camer2Fix.Y2;
            Camer2Alg.X3 = Camer2Fix.X3;
            Camer2Alg.Y3 = Camer2Fix.Y3;

            Camer3Alg.X0 = Camer3Fix.X0;
            Camer3Alg.Y0 = Camer3Fix.Y0;
            Camer3Alg.X1 = Camer3Fix.X1;
            Camer3Alg.Y1 = Camer3Fix.Y1;
            Camer3Alg.X2 = Camer3Fix.X2;
            Camer3Alg.Y2 = Camer3Fix.Y2;
            Camer3Alg.X3 = Camer3Fix.X3;
            Camer3Alg.Y3 = Camer3Fix.Y3;

            Camer4Alg.X0 = Camer4Fix.X0;
            Camer4Alg.Y0 = Camer4Fix.Y0;
            Camer4Alg.X1 = Camer4Fix.X1;
            Camer4Alg.Y1 = Camer4Fix.Y1;
            Camer4Alg.X2 = Camer4Fix.X2;
            Camer4Alg.Y2 = Camer4Fix.Y2;
            Camer4Alg.X3 = Camer4Fix.X3;
            Camer4Alg.Y3 = Camer4Fix.Y3;
            */
        }

        void stop()
        {
            //停机            
            toStop = true;
            if (running)
            {
                autoResetEvent1.WaitOne(10000, false);
                //  Console.WriteLine("\n执行线程停.");
                timer1.Dispose();//执行这一步，timer1停止运行，否则Timer会一直循环下去。
                running = false;
                // toStop = false;
                //20250326  Console.WriteLine("\n定时器 已停止.");
                parentLog.Info("\n定时器 已停止.");
            }
            /*
             * //20250507 
           Camer1Alg.dispose();
           Camer2Alg.dispose();
           Camer3Alg.dispose();
           Camer4Alg.dispose();*/
            /*//20250506  
             //删除早期图片
            FolderDelete FolderDelete1 = new FolderDelete(driveLetter, targetFolder, spaceThreshold);
            FolderDelete1.deletePicture();*/
        }

        #endregion

        #region Timer
        public void thread1Method(Object objParameter)
        {
            if (toStop)
            {
                parentLog.Info("线程  停止..............................................................");
                autoResetEvent1.Set();
                return;
            }
            beforDTThreading = System.DateTime.Now;
            RunMethod("");

            //20250929 if (!(cam1Used ^ Camer1Alg.AlarmRsPassed) && !(cam2Used ^ Camer2Alg.AlarmRsPassed) && !(cam3Used ^ Camer3Alg.AlarmRsPassed) && !(cam4Used ^ Camer4Alg.AlarmRsPassed)) // rasing edge of gAlarmReset               
            if (!(cam1Used ^ Camer1Alg.AlarmRsPassed)) // rasing edge of gAlarmReset               
            {
                gAlarmReset = false;
            }

            //20250929  if (Camer1Alg.Alarm || Camer2Alg.Alarm || Camer3Alg.Alarm || Camer4Alg.Alarm)
            if (Camer1Alg.Alarm)
            {
                adam2650Module.Output(3, 1);
                alarmming = true;
                parentLog.Info("Alarm Output...........................");
            }
            else
            {
                adam2650Module.Output(3, 0);
                alarmming = false;
                parentLog.Info("Alarm Output stopped...................................");
            }
            afterDTThreading = System.DateTime.Now;
            tsThreading = afterDTThreading.Subtract(beforDTThreading);
            switch (iii)
            {
                case 1:
                    //parentLog.Info("相机1 调用用时： {0} ", tsThreading.TotalSeconds.ToString("0.0000")); // 20250318
                    //20250929  iii = 2;
                    iii = 4;
                    break;
                //20250929 
                //case 2:
                //    //parentLog.Info("相机2 调用用时： {0}", tsThreading.TotalSeconds.ToString("0.0000"));// 20250318
                //    iii = 3;
                //    break;
                //case 3:
                //    //parentLog.Info("相机3 调用用时： {0}", tsThreading.TotalSeconds.ToString("0.0000"));// 20250318
                //    iii = 4;
                //    break;
                case 4:
                    /// 20250318 
                    //parentLog.Info("相机4 调用用时： {0}", tsThreading.TotalSeconds.ToString("0.0000"));

                    // if (toStop) 
                    //   break;//20250411
                    /*20250411
                     {
                        iii = 9;
                        parentLog.Info("线程  停止..............................................................");
                        autoResetEvent1.Set(); 
                     
                    }
                   // else * */
                    iii = 1;
                    break;
            }


        }
        private void RunMethod(string value)
        {
            //220250506 parentLog.Info("RunMethod。。。。。。。。。。。。。。。。。。。。。。。。");
            //20250305 beforDT1 = System.DateTime.Now;
            //必须有这一句检查Winform控件程序，以欺骗系统，否则系统会报异常，过不了
            //20250326 if (this.richTextBox1.InvokeRequired)
            if (this.label8.InvokeRequired)
            {
                WinformHandler winformHandler0 = new WinformHandler(RunMethod);
                //20250326  parentLog.Info("\n 相机1 label8 线程检查");
                IAsyncResult result = this.label8.BeginInvoke(winformHandler0, new object[] { value });
                try { winformHandler0.EndInvoke(result); }
                catch { }
                return;
                /*20250305
             catch (Exception ex)
            {
                Console.WriteLine("\n 1#写label8异常：{0}", ex);
            }*/
            }
            //20250403 else
            //20250403{
            // parentLog.Info("内switch。。。。。。。。。。。。。。。。。。。。。。。。");
            invokeCount++;
            switch (iii)
            {

                case 1:
                    //if (toStop) break;
                    parentLog.Info("焊接缺陷 线程运行， 第{0}次.", (invokeCount).ToString());
                    Camer1Alg.Run();
                    cam1Used = true;
                    break;
                //20250929 
                //case 2:
                //    parentLog.Info("相机2 线程运行， 第{0}次", (invokeCount).ToString());
                //    Camer2Alg.Run();
                //    cam2Used = true;
                //    break;
                //case 3:
                //    parentLog.Info("相机3 线程运行 第{0}次", (invokeCount).ToString());
                //    Camer3Alg.Run();
                //    cam3Used = true;
                //    break;
                case 4:
                    parentLog.Info("相机测量 线程运行 第{0}次", (invokeCount).ToString());
                    //20250929   Camer4Alg.Run();
                    cam4Used = true;
                    break;
            }
            //parentLog.Info("label8。。。。。。。。。。。。。。。。。。。。。。。。");
            if (alarmming) label8.ForeColor = Color.Red;  //20250320
            else label8.ForeColor = Color.Black; //20250320
        }




        #endregion
        class CamerFixture
        {
            static readonly Logger localLog = LogManager.GetCurrentClassLogger();
            private CogToolGroup cogToolGroup1;
            private CogPMAlignTool cogPMAlignTool1 = new CogPMAlignTool();
            private CogAcqFifoTool cogAcqFifoTool1;//= new CogAcqFifoTool();
            //private CogCaliperTool cogCaliperTool2;
            // private int numPMAlign;
            // private double numCaliper;
            private int pictureNum;
            //20250424  private string nameTime;
            //private Label labelStatus;//20250319  , label8Tag; 
            public Label labelAlarmInfo;//20250319
            private TextBox textBoxRunTimes;
            //20250326 private RichTextBox richTextBoxCamFix;
            private string _runningInfo;
            private string numCamer;
            private string pictureDir;
            private bool _alarm, _fixFault; //20250311 theCogAlarm,
            private CogRecordDisplay cogRecordDisplayFix;
            CogImageConvertTool cogImageConvertTool1; //20250423 
            private int cogAlarmCount1, cogAlarmCount2, cogAlarmCount3, cogAlarmCount4;
            private CogToolBlock toolBlock;
            public double X0, Y0, X1, Y1, X2, Y2, X3, Y3;
            //private CogPolygon cogPolygon1;

            public CamerFixture(string visionProFifoName, string visionProPosDir, CogRecordDisplay cogRecordDisplayIn, TextBox textBoxRunTimes, Label labelAlarmInfo, string numCamer) //20250319    Label label8Tag,
            {
                //20250303  cogToolGroup1 = CogSerializer.LoadObjectFromFile(AppDomain.CurrentDomain.BaseDirectory + "/ToolGroup交联后相机1定位.vpp") as CogToolGroup; //(@"D:\1开发\程序\1程序配套\VisionPro Template\" + "/ToolGroup定位.vpp") as CogToolGroup; 
                //20250303 
                _fixFault = false;
                cogToolGroup1 = CogSerializer.LoadObjectFromFile(AppDomain.CurrentDomain.BaseDirectory + visionProPosDir) as CogToolGroup;
                pictureNum = 1;
                //20250929 cogAlarmCount1 = cogAlarmCount2 = cogAlarmCount3 = cogAlarmCount4 = 0;
                cogAlarmCount1 = 0;
                //20250424   nameTime = "";
                //20250311 theCogAlarm = false;
                cogPMAlignTool1 = cogToolGroup1.Tools["CogPMAlignTool1"] as CogPMAlignTool;
                //20250423 cogAcqFifoTool1 = cogToolGroup1.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                cogAcqFifoTool1 = CogSerializer.LoadObjectFromFile(AppDomain.CurrentDomain.BaseDirectory + visionProFifoName) as CogAcqFifoTool; //20250423  
                // cogCaliperTool2 = cogToolGroup1.Tools["CogCaliperTool2"] as CogCaliperTool;
                toolBlock = cogToolGroup1.Tools["CogToolBlock2"] as CogToolBlock;
                cogImageConvertTool1 = cogToolGroup1.Tools["CogImageConvertTool1"] as CogImageConvertTool; //20250423 
                this.cogRecordDisplayFix = cogRecordDisplayIn;
                this.textBoxRunTimes = textBoxRunTimes;
                //20250303 this.label1 = label1;
                //20250303 this.label3 = label3;
                //20250326 this.labelStatus = labelStatusIn;
                this.labelAlarmInfo = labelAlarmInfo;
                //20250319   this.label8Tag = label8Tag;
                this.numCamer = numCamer;
                //20250326 this.richTextBoxCamFix = richTextBoxCamFixIn;
                _runningInfo = "runInfo: Fix initialized";
            }
            public bool FixFault
            {
                get { return _fixFault; }
            }
            public String RunningInfo
            {
                get
                {
                    return _runningInfo;
                }
            }

            public bool Alarm
            {
                get { return _alarm; }
                set { _alarm = value; }
            }
            public void Run()
            {
                //20250326 richTextBoxCamFix.AppendText("\n Run CamerFixture：");
                cogAcqFifoTool1.Run(); //20250423 
                cogImageConvertTool1.InputImage = cogAcqFifoTool1.OutputImage;  //20250423 
                cogToolGroup1.Run();

                // Console.WriteLine("\n 运行外围");
                #region   Cognex 运行状态监测
                try
                {
                    //20250929 _fixFault = (bool)toolBlock.Outputs["Fault"].Value; //是否超过设定最小宽度200
                    // Console.WriteLine("\n 宽度：{0}", numCaliper);

                    //20250929 if (_fixFault)
                    if (cogPMAlignTool1.Results.Count == 0)
                    {
                        //20250311 theCogAlarm = true;
                        _fixFault = true; //20251020
                        _runningInfo = numCamer + "相机问题： 未抓到焊缝";
                        //20250326  richTextBoxCamFix.AppendText(numCamer + "相机问题： 抓到的宽度小于最低要求");
                        //20250326 Console.WriteLine("\n{0}相机问题： 抓到的宽度小于最低要求", numCamer);
                        localLog.Error("{0}相机问题： 未抓到焊缝", numCamer);
                    }
                    else
                    {
                        cogAlarmCount1++;
                        _runningInfo = "：Ok";
                        //20250311 theCogAlarm = false;
                        //this.label1.Text = NumPMAlign.ToString("0");
                        /*
                          cogPolygon3.SetVertex(0, (double)toolBlock.Outputs["Polygen0X"].Value, (double)toolBlock.Outputs["Polygen0Y"].Value);
                          cogPolygon3.SetVertex(1, (double)toolBlock.Outputs["Polygen1X"].Value, (double)toolBlock.Outputs["Polygen1Y"].Value);
                          cogPolygon3.SetVertex(2, (double)toolBlock.Outputs["Polygen2X"].Value, (double)toolBlock.Outputs["Polygen2Y"].Value);
                          cogPolygon3.SetVertex(3, (double)toolBlock.Outputs["Polygen3X"].Value, (double)toolBlock.Outputs["Polygen3Y"].Value);
                         */

                        X0 = (double)toolBlock.Outputs["StartX1"].Value;
                        X1 = (double)toolBlock.Outputs["EndX1"].Value;
                        Y0 = (double)toolBlock.Outputs["StartY1"].Value;
                        Y1 = (double)toolBlock.Outputs["EndY1"].Value;
                        X2 = (double)toolBlock.Outputs["StartX2"].Value;
                        X3 = (double)toolBlock.Outputs["EndX2"].Value;
                        Y2 = (double)toolBlock.Outputs["StartY2"].Value;
                        Y3 = (double)toolBlock.Outputs["EndY2"].Value;

                    }

                }
                catch (Exception ex)
                {
                    _fixFault = true;
                    _runningInfo = numCamer + "相机异常： " + "定位失败";
                    //20250326 richTextBoxCamFix.AppendText("定位异常：  " + ex);
                    //20250326 Console.WriteLine("\n{0}相机定位异常{1,2}", numCamer.ToString(), ex);
                    localLog.Error("{0}相机定位异常{1}", numCamer.ToString(), ex);
                }
                #endregion               //Cognex 运行状态导入。 结束

                #region             // 报警处理

                if (gAlarmReset)
                {
                    if (!_fixFault)
                    {
                        _alarm = false;
                        this.labelAlarmInfo.Text = "";
                    }
                    else
                    {
                        this.labelAlarmInfo.ForeColor = Color.Black;
                        //20250319 this.label8Tag.ForeColor = Color.Black;
                        this.labelAlarmInfo.Text = numCamer + "：" + _runningInfo + ", 报警复位状态";
                    }
                }
                else
                {
                    if (_fixFault)
                    {
                        //  Console.WriteLine(" _runningInfo 非OK   。。。。。。。。。。。。。。。。。。。。。。。。...................");        

                        _alarm = true;
                        //20250319   this.label8Tag.ForeColor = Color.Red;//20250310
                        this.labelAlarmInfo.ForeColor = Color.Red;
                        this.labelAlarmInfo.Text = numCamer + "：" + _runningInfo;
                    }
                }

                #endregion      //报警处理结束

                //20250326 this.labelStatus.Text = numCamer + "：" + _runningInfo;
                this.textBoxRunTimes.Text = "Fix: " + pictureNum + Environment.NewLine;
                this.textBoxRunTimes.SelectionStart = this.textBoxRunTimes.TextLength;
                this.textBoxRunTimes.ScrollToCaret();
                cogRecordDisplayFix.Record = cogToolGroup1.CreateLastRunRecord().SubRecords[0];// myRecord.SubRecords[0];//新增 [0]中设为0,1 对应VisonPro中选取不同LastRun画面  cogRecordDisplay1为拖入Form1的Cog控件，把CreateLastRunRecord()画面放入 cogRecordDisplay1
                // 四个图中，其中一个附加边框没有，原因是其中一个边框数据没有导入进来。缺乏这样的句子Camer4Alg.X0 = Camer4Fix.X0;
                cogRecordDisplayFix.AutoFit = true; //使图像在CogRecordDisplay1 中自适应

                //20250424  nameTime = DateTime.Now.ToString("HH-mm-ss_fff");
                if (!_fixFault) ////20250311 theCogAlarm)
                {
                    pictureDir = @"D:\1现场图\" + DateTime.Now.Date.ToString("yyyy-MM-dd") + @"\" + numCamer + @"\";         //设置当前目录

                    //20250326 localLog.Info("\n{0}：OK 照片地址。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。", numCamer);
                }
                else
                {
                    pictureDir = @"D:\1现场图\" + DateTime.Now.Date.ToString("yyyy-MM-dd") + @"\" + numCamer + @"\NG\";         //设置当前目录
                    //20250326 localLog.Info("\n{0}：错误 照片存储地址。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。", numCamer);
                }

                if (!System.IO.Directory.Exists(pictureDir)) System.IO.Directory.CreateDirectory(pictureDir);   //该路径不存在时，在当前文件目录下创建文件夹
                // var formateDir = AppDomain.CurrentDomain.BaseDirectory + "Formate.jpg";
                //20250303 var formateDir = @"D:\1开发\程序\1程序配套\Image Formate\Formate.jpg";
                ImageCompress.CompressionImage(cogAcqFifoTool1.OutputImage, gFormateFullName, pictureDir, "定位图", 83, "compress");//83调节压缩大小  
                //20250326 localLog.Info("\n{0}：定位图。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。", numCamer);
                // SaveRecordDisplay(pictureDir + nameTime + ".jpg", pictureDir); //保存CogRecordDisplay显示结果图像，这个图片无法被 VisionPro 的Image Source 读取。格式不对
                //SaveImage(iCogImage1, pictureDir + nameTime + ".jpg");//直接保存Fifo图片，无压缩，无重画像素
                pictureNum++;
                //20250326 richTextBoxCamFix.AppendText("\ncogFixAlarmCount1：" + cogAlarmCount1.ToString() + ", cogFixAlarmCount2：" + cogAlarmCount2.ToString() + ", cogFixAlarmCount3：" + cogAlarmCount3.ToString() + ", cogFixAlarmCount4：" + cogAlarmCount4.ToString());
                //20250326 localLog.Info("\ncogFixAlarmCount1：" + cogAlarmCount1.ToString() + ", cogFixAlarmCount2：" + cogAlarmCount2.ToString() + ", cogFixAlarmCount3：" + cogAlarmCount3.ToString() + ", cogFixAlarmCount4：" + cogAlarmCount4.ToString());
                // richTextBox1.AppendText("\nLabel Red：" + labelRed3Count.ToString() + "\nLabel Black：" + labelBlack3Count.ToString());
                // richTextBox1.AppendText("\nBlob输入：" + cogPolygon1.GetVertexX(0).ToString() + ", " + cogPolygon1.GetVertexX(1).ToString() + ", " + cogPolygon1.GetVertexX(2).ToString() + ", " + cogPolygon1.GetVertexX(3).ToString());
                //20250326 richTextBoxCamFix.SelectionStart = richTextBoxCamFix.Text.Length;
                //20250326 richTextBoxCamFix.ScrollToCaret();

            }
        }
        class CamerAlgorithm
        {
            static readonly Logger localLog = LogManager.GetCurrentClassLogger();
            CogToolGroup cogToolGroupOpe;
            private CogAcqFifoTool cogAcqFifoTool1;
            //241025 private CogDisplay cogDisplay1;
            private ICogImage iCogImage1;
            private int pictureNum;
            private int cogAlarmCount1, cogAlarmCount2, cogAlarmCount3, cogAlarmCount4;
            double cogPolygonBlobDarkVertexX0, cogPolygonBlobDarkVertexX1;
            //20250424 private string nameTime;
            private Label labelTimeUsed;  //20250326 labelStatus,
            public Label labelAlarmInfo;
            private TextBox textBoxRunTimes;
            //20250326 private RichTextBox richTextBox1;
            private string _runningInfo;
            private string numCamer;
            private string pictureDir;//20250304, visionProOpeDir;
            string runningInfoCog, runningInfoCsharp;
            private bool _alarm, _alarmRsPassed, cogAlarm, cSharpAlarmFlag, CsharpAlarm;
            // private bool theAlarm;
            private CogToolBlock toolBlockSum, toolBlock1; //Cognex.VisionPro.ToolBlock.CogToolBlock toolBlock3; 
            // private CogFindLineTool cogFindLineTool1, cogFindLineTool2;
            CogImageConvertTool cogImageConvertTool1;
            // CogBlobTool cogBlob1;
            // CogBlobResultCollection cogBlobs;
            //CogIPOneImageTool cogIPOneImageTool1;
            // CogHistogramTool cogHistogramTool1;
            private CogPolygon cogPolygonIn; //20250929, , cogPolygonMade, cogPolygonIPOneImage, cogPolygonBlobDark;  //20250423   cogPolygonIn, cogPolygonMade;
            Cognex.VisionPro.ICogRecord lastRecord; //
            //20250303  private CogColorExtractorTool cogColorExtractorTool1;
            private CogRecordDisplay cogRecordDisplayAlgo;
            Type systemType;//20250507  
            private DateTime beforDTRun, afterDTRun;
            private TimeSpan tsRun;
            public double X0, Y0, X1, Y1, X2, Y2, X3, Y3, difLineFixStartX, difLineFixEndX, difLineNowStartX, difLineNowEndX;
            //public bool done;
            //20250320  public CamerAlgorithm(string visionProOpeDir, CogRecordDisplay cogRecordDisplayIn, RichTextBox richTextBox1, TextBox textBoxRunTimes, Label labelStatusIn, Label labelAlarmInfo, Label labelTimeUsedIn, string numCamer)
            public CamerAlgorithm(string visionProFifoName, string visionProOpeName, CogRecordDisplay cogRecordDisplayIn, TextBox textBoxRunTimes, Label labelAlarmInfo, Label labelTimeUsedIn, string numCamer)
            {
                // TODO: Complete member initialization
                //241025 this.cogDisplay1 = cogDisplay1;
                this.cogRecordDisplayAlgo = cogRecordDisplayIn;
                //20250303 cogToolGroup1 = CogSerializer.LoadObjectFromFile(AppDomain.CurrentDomain.BaseDirectory + "/ToolGroup交联后相机1运行.vpp") as CogToolGroup; //(@"D:\1开发\程序\1程序配套\VisionPro Template\" + "/ToolGroup运算.vpp") as CogToolGroup; //(AppDomain.CurrentDomain.BaseDirectory + "/ToolGroup运算.vpp") as CogToolGroup; //
                cogToolGroupOpe = CogSerializer.LoadObjectFromFile(AppDomain.CurrentDomain.BaseDirectory + visionProOpeName) as CogToolGroup;
                pictureNum = 1;
                //20250320cogAlarmCount1 = cogAlarmCount2 = cogAlarmCount3 = cogAlarmCount4 = 0;
                //20250320 nameTime = "";
                //20250320 cogAlarm=_alarm = false;

                //20250320 iCogImage1 = null;
                //20250423 cogAcqFifoTool1 = cogToolGroup1.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                cogAcqFifoTool1 = CogSerializer.LoadObjectFromFile(AppDomain.CurrentDomain.BaseDirectory + visionProFifoName) as CogAcqFifoTool; //20250423                 
                // cogIPOneImageTool1 = cogToolGroupOpe.Tools["CogIPOneImageTool1"] as CogIPOneImageTool;
                //20250423cogPolygonIPOneImage = cogIPOneImageTool1.Region as CogPolygon;

                cogImageConvertTool1 = cogToolGroupOpe.Tools["CogImageConvertTool1"] as CogImageConvertTool;
                // cogBlob1 = cogToolGroupOpe.Tools["CogBlobToolUpDark"] as CogBlobTool;
                //cogPolygonBlobDark = cogBlob1.Region as CogPolygon;
                cogPolygonIn = new CogPolygon();
                // cogPolygonMade = new CogPolygon();
                toolBlock1 = cogToolGroupOpe.Tools["CogToolBlock1"] as CogToolBlock;
                toolBlockSum = cogToolGroupOpe.Tools["CogToolBlock汇总"] as CogToolBlock;
                //toolBlockUp = cogToolGroup1.Tools["CogToolBlock上"] as CogToolBlock;
                //   cogBlobToolUp = cogToolGroup1.Tools["CogBlobTool上"] as CogBlobTool;
                //cogFindLineTool1 = cogToolGroupOpe.Tools["CogFindLineTool线1左"] as CogFindLineTool;
                //cogFindLineTool2 = cogToolGroupOpe.Tools["CogFindLineTool线2右"] as CogFindLineTool;
                //20250303  cogColorExtractorTool1 = cogToolGroup1.Tools["CogColorExtractorTool1"] as CogColorExtractorTool;

                this.textBoxRunTimes = textBoxRunTimes;
                //20250303 this.label1 = label1;
                //20250303 this.label3 = label3;
                //20250326 this.labelStatus = labelStatusIn;
                this.labelAlarmInfo = labelAlarmInfo;
                this.numCamer = numCamer;
                //20250326 this.richTextBox1 = richTextBox1;//241023日16点 
                this.labelTimeUsed = labelTimeUsedIn;
                _runningInfo = "runInfo initialized";
            }

            #region getSet
            public bool Alarm
            {
                get { return _alarm; }
                //20250312   set { theAlarm = value; }
            }

            public string RunningInfo
            {
                get { return _runningInfo; }
            }


            public bool AlarmRsPassed  //20250320 
            {
                get { return _alarmRsPassed; }
                set { _alarmRsPassed = value; }
            }
            #endregion
            public void PolygonSet()
            {
                //    cogIPOneImageTool1.Region = cogPolygon3;
                //cogColorExtractorTool1.Region = cogPolygon3;
                //    cogBlobToolUp.Region = cogPolygon3;
                //20250929
                //cogFindLineTool1.RunParams.ExpectedLineSegment.StartX = X0;
                //cogFindLineTool1.RunParams.ExpectedLineSegment.EndX = X1;
                //cogFindLineTool1.RunParams.ExpectedLineSegment.StartY = Y0;
                //cogFindLineTool1.RunParams.ExpectedLineSegment.EndY = Y1;

                //cogFindLineTool2.RunParams.ExpectedLineSegment.StartX = X2;
                //cogFindLineTool2.RunParams.ExpectedLineSegment.EndX = X3;
                //cogFindLineTool2.RunParams.ExpectedLineSegment.StartY = Y2;
                //cogFindLineTool2.RunParams.ExpectedLineSegment.EndY = Y3;

                //difLineFixStartX = Math.Abs(X0 - X2);//
                //difLineFixEndX = Math.Abs(X1 - X3);//

                ///20250314
                cogPolygonIn.NumVertices = 4;
                cogPolygonIn.SetVertex(0, X0, Y0);
                cogPolygonIn.SetVertex(1, X1, Y1);
                cogPolygonIn.SetVertex(2, X3, Y3);
                cogPolygonIn.SetVertex(3, X2, Y2);
                cogPolygonIn.Color = CogColorConstants.Green;

                toolBlock1.Inputs["X0"].Value = X0;
                toolBlock1.Inputs["Y0"].Value = Y0;
                toolBlock1.Inputs["X1"].Value = X1;
                toolBlock1.Inputs["Y1"].Value = Y1;
                toolBlock1.Inputs["X2"].Value = X2;
                toolBlock1.Inputs["Y2"].Value = Y2;
                toolBlock1.Inputs["X3"].Value = X3;
                toolBlock1.Inputs["Y3"].Value = Y3;


                //20250929
                //cogIPOneImageTool1.Region = cogPolygonIn;
                //cogColorExtractorTool1.Region = cogPolygon3;
                //    cogBlobToolUp.Region = cogPolygon3;
                ///20250314
                ///
                /*//20250506a 
                localLog.Info("TriggerMode before running:{0}---------------", cogAcqFifoTool1.Operator.OwnedTriggerParams.TriggerModel.ToString());
                //cogAcqFifoTool1.Operator.OwnedTriggerParams.TriggerModel = Cognex.VisionPro.CogAcqTriggerModelConstants.FreeRun;
                localLog.Info("TriggerMode after running:{0}---------------", cogAcqFifoTool1.Operator.OwnedTriggerParams.TriggerModel.ToString());

                systemType = cogAcqFifoTool1.Operator.OwnedTriggerParams.TriggerModel.GetType();
                localLog.Info("TriggerType:{0}---------------", systemType.ToString());            
                //cogAcqFifoTool1.Run();
                ///20250506a */

                //20250423 cogToolGroupAgo.Run();
            }
            /* //20250507 
             public void dispose()
             {
                 cogAcqFifoTool1.Dispose();
             }*/
            public void Run()
            {
                beforDTRun = System.DateTime.Now;

                //this sentence added reduces running time from 0.135ms to 0.035ms
                //20250326  richTextBox1.AppendText("\nColorExtracotr坐标：" + cogPolygonIPOneImage.GetVertexX(0).ToString() + ", " + cogPolygonIPOneImage.GetVertexX(1).ToString() + ", " + cogPolygonIPOneImage.GetVertexX(2).ToString() + ", " + cogPolygonIPOneImage.GetVertexX(3).ToString());//241023日16点 
                //20250326 richTextBox1.AppendText(numCamer + "\nStartX1，EndX1，StartX2， EndX2：" + X0.ToString() + ", " + X1.ToString() + ", " + X2.ToString() + ", " + X3.ToString());
                //20250326 localLog.Info(numCamer + "\nStartX1，EndX1，StartX2， EndX2：" + X0.ToString() + ", " + X1.ToString() + ", " + X2.ToString() + ", " + X3.ToString());
                cogAcqFifoTool1.Run(); //20250423
                cogImageConvertTool1.InputImage = cogAcqFifoTool1.OutputImage; //20250423 
                //20250507  cogImageConvertTool1.InputImage = cogAcqFifoTool1.CreateLastRunRecord() as ICogImage; //20250507 
                cogToolGroupOpe.Run();
                lastRecord = cogToolGroupOpe.CreateLastRunRecord();

                //20250425  getBlobsOfUpDark();
                inspectCog();
                doAlarm();
                doWinForm();
                savePictures();

                pictureNum++;

                afterDTRun = System.DateTime.Now;
                tsRun = afterDTRun.Subtract(beforDTRun);
                localLog.Info(numCamer + "相机运行用时： {0} \n", tsRun.TotalSeconds.ToString("0.0000"));
                labelTimeUsed.Text = tsRun.TotalSeconds.ToString("0.0000");//20250305  label14.Text = 
                // done = true;


            }

            //20250929
            //void getBlobsOfUpDark()//blobDark的边缘去掉 20250424
            //{
            //    bool blobFlag;
            //    cogPolygonBlobDarkVertexX0 = cogPolygonBlobDark.GetVertexX(0);//get image's polygon vertex X
            //    cogPolygonBlobDarkVertexX1 = cogPolygonBlobDark.GetVertexX(1);
            //    try
            //    {
            //        cogBlobs = cogBlob1.Results.GetBlobs();
            //        if (cogBlobs != null && cogBlobs.Count > 0)
            //        {
            //            int ii = 0;
            //            CsharpAlarm = false;
            //            foreach (CogBlobResult cogBlob in cogBlobs)
            //            {
            //                blobFlag = true;
            //                //cogBlob.GetBoundary().Color = CogColorConstants.Red;
            //                for (int j = 0; j < cogBlob.GetBoundary().GetVertices().GetLength(0); j++)
            //                {
            //                    double xxx = cogBlob.GetBoundary().GetVertexX(j);
            //                    //20250423 if (xxx < (double)toolBlock1.Outputs["X0"].Value + 10 || xxx > (double)toolBlock1.Outputs["X1"].Value - 10)
            //                    if (xxx < cogPolygonBlobDarkVertexX0 + 10 || xxx > cogPolygonBlobDarkVertexX1 - 10)
            //                    {
            //                        blobFlag = false;
            //                        break;
            //                    }
            //                    Application.DoEvents(); //220250427a 
            //                }
            //                if (blobFlag)
            //                {
            //                    // cogToolGroupOpe.AddGraphicToRunRecord(cogBlob.GetBoundary(), lastRecord, "CogBlobToolUpDark.BlobImage", "");
            //                    cogToolGroupOpe.AddGraphicToRunRecord(cogBlob.GetBoundary(), lastRecord, "CogImageConvertTool1.OutputImage", "");  ///20250314画框
            //                    CsharpAlarm = true;
            //                }

            //                if (+ii >= 6) break;
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        parentLog.Info("Blob missing: \n" + ex);

            //    }

            //}

            //20250929
            void inspectCog()
            {
                cogAlarmCount1++;
                try
                {
                    //20250929
                    //cogPolygonMade.NumVertices = 4;
                    //cogPolygonMade.SetVertex(0, cogFindLineTool1.Results.GetLineSegment().StartX, cogFindLineTool1.Results.GetLineSegment().StartY);
                    //cogPolygonMade.SetVertex(1, cogFindLineTool1.Results.GetLineSegment().EndX, cogFindLineTool1.Results.GetLineSegment().EndY);
                    //cogPolygonMade.SetVertex(2, cogFindLineTool2.Results.GetLineSegment().EndX, cogFindLineTool2.Results.GetLineSegment().EndY);
                    //cogPolygonMade.SetVertex(3, cogFindLineTool2.Results.GetLineSegment().StartX, cogFindLineTool2.Results.GetLineSegment().StartY);
                    //cogPolygonMade.Color = CogColorConstants.Yellow;

                    //difLineNowStartX = Math.Abs(cogFindLineTool1.Results.GetLineSegment().StartX - cogFindLineTool2.Results.GetLineSegment().StartX);
                    //difLineNowEndX = Math.Abs(cogFindLineTool1.Results.GetLineSegment().EndX - cogFindLineTool2.Results.GetLineSegment().EndX);
                    // //20250320 _theAlarm = true;
                    //if (difLineNowStartX > difLineFixStartX * 1.1)
                    //{
                    //    localLog.Warn("\n{0}相机， theCogAlarm ={1},问题：上边太宽.....................", numCamer, cogAlarm.ToString());
                    //    localLog.Info("\n{0}相机，现宽:{1}，首宽:{2}", numCamer, difLineNowStartX.ToString("f1"), difLineFixStartX.ToString("f1"));
                    //    _runningInfo = numCamer.ToString() + "问题：上边太宽，请点启按钮,再点停按钮";
                    //}
                    //else if (difLineNowStartX < difLineFixStartX * 0.9)
                    //{
                    //    localLog.Warn("\n {0}相机， theCogAlarm ={1},问题：上边太窄.....................", numCamer, cogAlarm.ToString());
                    //    localLog.Info("\n{0}相机，现宽{1}，首宽{2}", numCamer, difLineNowStartX.ToString("f1"), difLineFixStartX.ToString("f1"));
                    //    _runningInfo = numCamer.ToString() + "问题：上边太窄，请点启按钮,再点停按钮";
                    //}
                    //else if (difLineNowEndX > difLineFixEndX * 1.1)
                    //{
                    //    localLog.Warn("\n , {0}， theCogAlarm ={1},问题：下边太宽.....................", numCamer, cogAlarm.ToString());
                    //    localLog.Info("\n{0}相机，现宽{1}，首宽{2}", numCamer, difLineNowStartX.ToString("f1"), difLineFixStartX.ToString("f1"));
                    //    _runningInfo = numCamer.ToString() + "问题：下边太宽，请点启按钮,再点停按钮";
                    //}
                    //else if (difLineNowEndX < difLineFixEndX * 0.9)
                    //{
                    //    localLog.Warn("\n {0}相机， theCogAlarm ={1},问题：下边太窄.....................", numCamer, cogAlarm.ToString());
                    //    localLog.Info("\n{0}相机，现宽{1}，首宽{2}", numCamer, difLineNowStartX.ToString("f1"), difLineFixStartX.ToString("f1"));
                    //    _runningInfo = numCamer.ToString() + "问题：下边太窄，请点启按钮,再点停按钮";
                    //}
                    /////
                    //else
                    //{
                    cogAlarm = (bool)toolBlockSum.Outputs["alarm"].Value;
                    localLog.Info("\n  {0}， cogAlarm ={1} 运行.....................", numCamer, cogAlarm.ToString());
                    if (cogAlarm || CsharpAlarm)
                    {
                        if (cogAlarm)
                            runningInfoCog = (string)toolBlockSum.Outputs["alarmInfo"].Value;//20250427 _runningInfo = (string)toolBlockSum.Outputs["alarmInfo"].Value;
                        else runningInfoCog = "";//20250427 _runningInfo = "";

                        if (CsharpAlarm) runningInfoCsharp = " blobDark: B";//20250427  if (CsharpAlarm) _runningInfo = _runningInfo + " blobDark: B";
                        else runningInfoCsharp = "";//20250427 

                        _runningInfo = runningInfoCog + runningInfoCsharp; //20250427 
                        // cogAlarmCount2++;
                        //20250326 localLog.Info("\n 内部报警 {0}, _runningInfo= {1},theCogAlarm ={2} .....................", numCamer, _runningInfo, cogAlarm.ToString());
                    }
                    else
                    {
                        _runningInfo = "：Ok";
                        //20250326 richTextBox1.AppendText("\n\n No Alarmming");
                        //20250326 localLog.Info("No Alarmming");
                        //  cogAlarmCount3++;
                        //20250326 Console.WriteLine("\n 内部未报警 {0},theCogAlarm ={1}。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。", numCamer, cogAlarm.ToString());
                        //20250326  localLog.Info("\n 内部未报警 {0},theCogAlarm ={1}。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。。", numCamer, cogAlarm.ToString());
                    }
                    //}
                }
                catch (Exception ex)
                {
                    _runningInfo = "缺陷：Cog运行故障，检查抓图是否正确";
                    cogAlarm = true;
                    //20250326   richTextBox1.AppendText("\n 异常1：  " + ex);
                    //20250326  Console.WriteLine("\n{0}报警：Cog问题{1}", numCamer, ex);
                    localLog.Error("\n{0}报警：Cog问题{1}，检查抓图是否正确", numCamer, ex);
                }
                //this.label1.Text = NumPMAlign.ToString("0");
            }
            void doAlarm()
            {
                //20250424
                /* if (CsharpAlarm || !cSharpAlarmFlag)
                 {
                     _runningInfo = _runningInfo + " blobDark: B";
                     cSharpAlarmFlag = true;
                 }
                 else if (!CsharpAlarm)
                 {
                     cSharpAlarmFlag = false;
                 }*/

                if (gAlarmReset && !_alarmRsPassed)
                {

                    if (cogAlarm) //20250930 if (cogAlarm || CsharpAlarm)
                    {
                        this.labelAlarmInfo.ForeColor = Color.Black;
                        this.labelAlarmInfo.Text = numCamer + "：" + _runningInfo + ", 报警复位状态";
                    }
                    else
                    {
                        _alarm = false;
                        this.labelAlarmInfo.Text = "";
                    }
                }
                else if (cogAlarm)  //else if (cogAlarm || CsharpAlarm)
                {
                    //  Console.WriteLine(" _runningInfo 非OK   。。。。。。。。。。。。。。。。。。。。。。。。...................");
                    _alarm = true;
                    this.labelAlarmInfo.ForeColor = Color.Red;
                    this.labelAlarmInfo.Text = numCamer + ": " + _runningInfo;
                }
                if (gAlarmReset) _alarmRsPassed = true; ;//20250320
            }

            void doWinForm()
            {
                //20250326   this.labelStatus.Text = _runningInfo;  //numCamer + "：" + _runningInfo;
                //   iCogImage1 = cogAcqFifoTool1.OutputImage;

                //  Console.WriteLine("{0}  step2.\n", DateTime.Now.ToString("h:mm:ss.fff"));
                //myRecord = cogToolGroup1.CreateLastRunRecord();  //新增  CreateLastRunRecord()画面赋值给myRecord 
                //241025 cogDisplay1.Image = iCogImage1;
                //241025 cogDisplay1.Fit();

                // cogRecordDisplay1.Image = iCogImage1;
                //
                // cogPolygonIPOneImage.Color = CogColorConstants.Yellow; //  
                //cogPolygonIPOneImage.LineWidthInScreenPixels = 2; //  
                //20250314  cogToolGroup1.AddGraphicToRunRecord(cogPolygonIPOneImage, lastRecord, "CogAcqFifoTool1.OutputImage", "");  /// 画框
                cogToolGroupOpe.AddGraphicToRunRecord(cogPolygonIn, lastRecord, "CogImageConvertTool1.InputImage", "");  ///20250314 画框
                //cogToolGroupOpe.AddGraphicToRunRecord(cogPolygonMade, lastRecord, "CogImageConvertTool1.InputImage", "");  ///20250314画框
                //  cogRecordDisplayAlgo.Record = cogToolGroup1.CreateLastRunRecord().SubRecords[NumRecord];// myRecord.SubRecords[0];//新增 [0]中设为0,1 对应VisonPro中选取不同LastRun画面  cogRecordDisplay1为拖入Form1的Cog控件，把CreateLastRunRecord()画面放入 cogRecordDisplay1
                // 四个图中，其中一个附加边框没有，原因是其中一个边框数据没有导入进来。缺乏这样的句子Camer4Alg.X0 = Camer4Fix.X0;
                cogRecordDisplayAlgo.Record = lastRecord.SubRecords[numRecord]; // myRecord.SubRecords[0];//新增 [0]中设为0,1 对应VisonPro中选取不同LastRun画面  cogRecordDisplay1为拖入Form1的Cog控件，把CreateLastRunRecord()画面放入 cogRecordDisplay1
                localLog.Info("\n 图片号码    {0}：", numRecord);
                //20250305 cogRecordDisplay1.Image = cogIPOneImageTool1.InputImage;  
                cogRecordDisplayAlgo.AutoFit = true; //使图像在CogRecordDisplay1 中自适应
                //  Console.WriteLine("{0} step3.\n", DateTime.Now.ToString("h:mm:ss.fff"));
                //20251020 this.textBoxRunTimes.Text = pictureNum + Environment.NewLine;
                this.textBoxRunTimes.Text = pictureNum + "            " + numRecord + Environment.NewLine;
                this.textBoxRunTimes.SelectionStart = this.textBoxRunTimes.TextLength;
                this.textBoxRunTimes.ScrollToCaret();
                //20250326 richTextBox1.AppendText("\ncogAlgAlarmCount1：" + cogAlarmCount1.ToString() + ", cogAlgAlarmCount2：" + cogAlarmCount2.ToString() + ", cogAlgAlarmCount3：" + cogAlarmCount3.ToString());
                //20250326 localLog.Info("\ncogAlgAlarmCount1：" + cogAlarmCount1.ToString() + ", cogAlgAlarmCount2：" + cogAlarmCount2.ToString() + ", cogAlgAlarmCount3：" + cogAlarmCount3.ToString());
                //richTextBox1.AppendText("\nLabel Red：" + labelRedCount.ToString() + "\nLabel Black：" + labelBlackCount.ToString());
                // richTextBox1.AppendText("\nFix output X: " + doublePolygen0X.ToString() + ", " + doublePolygen1X.ToString() + ", " + doublePolygen2X.ToString() + ", " + doublePolygen3X.ToString());
                //richTextBox1.AppendText("\nBlob输入：" + cogPolygonBlob.GetVertexX(0).ToString() + ", " + cogPolygonBlob.GetVertexX(1).ToString() + ", " + cogPolygonBlob.GetVertexX(2).ToString() + ", " + cogPolygonBlob.GetVertexX(3).ToString());
                //20250326 richTextBox1.SelectionStart = richTextBox1.Text.Length;
                //20250326 richTextBox1.ScrollToCaret();

            }

            void savePictures()
            {
                //20250424  nameTime = DateTime.Now.ToString("HH-mm-ss_fff");
                /* if (!cogAlarm) //(_runningInfo == "：Ok" && !cogAlarm)
                     pictureDir = @"D:\检测图\" + DateTime.Now.Date.ToString("yyyy-MM-dd") + @"\" + numCamer + @"\";         //设置当前目录
                 else
                     pictureDir = @"D:\检测图\" + DateTime.Now.Date.ToString("yyyy-MM-dd") + @"\" + numCamer + @"\NG\";         //设置当前目录
                 */
                if (cogAlarm || CsharpAlarm)
                    pictureDir = @"D:\1现场图\" + DateTime.Now.Date.ToString("yyyy-MM-dd") + @"\" + numCamer + @"\NG\";         //设置当前目录
                else
                    pictureDir = @"D:\1现场图\" + DateTime.Now.Date.ToString("yyyy-MM-dd") + @"\" + numCamer + @"\";         //设置当前目录

                if (!System.IO.Directory.Exists(pictureDir)) System.IO.Directory.CreateDirectory(pictureDir);   //该路径不存在时，在当前文件目录下创建文件夹
                // var formateDir = AppDomain.CurrentDomain.BaseDirectory + "Formate.jpg";
                //20250303 var formateDir = @"D:\1开发\程序\1程序配套\Image Formate\Formate.jpg";
                ImageCompress.CompressionImage(cogAcqFifoTool1.OutputImage, gFormateFullName, pictureDir, "", 73, "compress");//83调节压缩大小  
                //20250507 ImageCompress.CompressionImage(cogImageConvertTool1.InputImage, gFormateFullName, pictureDir, "", 100, "compress");//83调节压缩大小   //20250507


                // SaveRecordDisplay(pictureDir + nameTime + ".jpg", pictureDir); //保存CogRecordDisplay显示结果图像，这个图片无法被 VisionPro 的Image Source 读取。格式不对
                //SaveImage(iCogImage1, pictureDir + nameTime + ".jpg");//直接保存Fifo图片，无压缩，无重画像素
            }
            /*  private void SaveImage(ICogImage iCogImage, string fileName)
              {
                  if (null == iCogImage) return;
                  using (CogImageFileTool cogImageFileTool1 = new CogImageFileTool())
                  {
                      cogImageFileTool1.InputImage = iCogImage;
                      cogImageFileTool1.Operator.Open(fileName, CogImageFileModeConstants.Write);
                      cogImageFileTool1.Run();
                  }
              }*/
            /*    private void SaveRecordDisplay(string fileName, string CurrentDirectory)
                {
                    if (!System.IO.Directory.Exists(CurrentDirectory)) System.IO.Directory.CreateDirectory(CurrentDirectory);   //该路径不存在时，在当前文件目录下创建文件夹"导出.."
                    Bitmap bitmap = cogRecordDisplay1.CreateContentBitmap(Cognex.VisionPro.Display.CogDisplayContentBitmapConstants.Display) as Bitmap;
                    bitmap.Save(fileName);
                }*/


        }




















    }



    public static class ImageCompress
    {
        static Bitmap bitmap1;
        static readonly Logger localLog = LogManager.GetCurrentClassLogger();
        ///	<summary>													
        ///	图片压缩													
        ///	</summary>													
        ///	<param	name="imagePath">图片文件路径</param>												
        ///	<param	name="targetFolder">保存文件夹</param>												
        ///	<param	name="quality">压缩质量</param>												
        ///	<param	name="fileSuffix">压缩后的文件名后缀（防止直接覆盖原文件）</param>												
        public static void CompressionImage(ICogImage iCogImage1, string formateDir, string targetDir, string fixName, long quality = 100, string fileSuffix = "compress")
        {
            /*   if (!File.Exists(imagePath))
               {
                   throw new FileNotFoundException();
               }
               if (!Directory.Exists(targetFolder))
               {
                   Directory.CreateDirectory(targetFolder);
               }
               var fileInfo = new FileInfo(imagePath);
              */
            try
            {
                bitmap1 = iCogImage1.ToBitmap();
                var fileFullName = targetDir + System.DateTime.Now.ToString("HH-mm-ss_fff") + fixName + ".jpg";//targetFolder+fileName; //Path.Combine(targetFolder, fileName);
                var imageByte = CompressionImage(bitmap1, formateDir, quality);
                var ms = new MemoryStream(imageByte);
                var image = Image.FromStream(ms);
                image.Save(fileFullName);
                ms.Close();
                ms.Dispose();
                image.Dispose();
            }
            catch (Exception ex)
            {
                localLog.Info("\n警告: 相机通电了么？网线正常么？ \n 其他程序在使用相机？格式图片路径对么？ \n,{0}", ex);
            }
        }
        private static byte[] CompressionImage(Bitmap bitmap1, string formateDir, long quality)
        {
            using (var fileStream1 = new FileStream(formateDir, FileMode.Open))
            {
                using (var image1 = Image.FromStream(fileStream1))
                {
                    var codecInfo = GetEncoder(image1.RawFormat);
                    var myEncoder = System.Drawing.Imaging.Encoder.Quality;
                    var myEncoderParameters = new EncoderParameters(1);
                    var myEncoderParameter = new EncoderParameter(myEncoder, quality);
                    myEncoderParameters.Param[0] = myEncoderParameter;
                    using (var ms = new MemoryStream())
                    {
                        bitmap1.Save(ms, codecInfo, myEncoderParameters);
                        myEncoderParameters.Dispose();
                        myEncoderParameter.Dispose();
                        return ms.ToArray();
                    }
                }
            }
        }
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }


    class Adam2650
    {
        static readonly Logger localLog = LogManager.GetCurrentClassLogger();
        private AdamSocket adamModbus;
        private bool connected;
        // private string moduleIP;
        // private int modbusTcpPort;
        public Adam2650(string moduleIP, int modbusTcpPort)
        {
            connected = false;
            // this.moduleIP = moduleIP;
            // this.modbusTcpPort = modbusTcpPort;
            adamModbus = new AdamSocket();
            adamModbus.SetTimeout(1000, 1000, 1000); // set timeout for TCP
            if (adamModbus.Connect(moduleIP, ProtocolType.Tcp, modbusTcpPort))
                connected = true;
            else
            {
                connected = false;
                MessageBox.Show("声光报警 初始未连接上");
                localLog.Error("声光报警 初始未连接上");
            }

        }
        public void Output(int OutputChannel, int setReset)
        {
            try
            {
                adamModbus.Modbus().ForceSingleCoil(17 + OutputChannel, setReset);//设定输出通道channel为true或False  
                connected = true;
            }
            catch (Exception ex)
            {
                connected = false;
                MessageBox.Show("声光报警 后续未连接上");
                localLog.Error("声光报警 后续未连接上");
            }
        }
        public void Closing()
        {
            adamModbus.Disconnect(); // disconnect slave   
        }

    }

    class FolderDelete
    {
        static readonly Logger localLog = LogManager.GetCurrentClassLogger();
        // 要删除文件的文件夹路径
        //string targetFolder = @"D:\测试待删除X"; 
        string targetFolder; //?? D:\测试待删除X\
        // 剩余空间阈值，这里设为7T。 1GB（1 * 1024 * 1024 * 1024字节）
        double spaceThreshold;
        double _currentFreeSpace;
        DriveInfo drive;
        public string information;
        public FolderDelete(string driveLetter, string targetFolder, double spaceThreshold)
        {
            this.targetFolder = targetFolder;
            this.spaceThreshold = spaceThreshold;
            drive = new DriveInfo(driveLetter);// 要检查的磁盘驱动器号，string driveLetter = "D:";
        }

        public double CurrentFreeSpace
        {
            get { return _currentFreeSpace; }
        }
        public void deletePicture()
        {
            if (!drive.IsReady)
            {
                information = "指定的磁盘未准备好";
                localLog.Error("指定的磁盘未准备好");
                MessageBox.Show("指定的磁盘未准备好");
                return;
            }

            //double currentfreeSpace = drive.AvailableFreeSpace;
            _currentFreeSpace = drive.AvailableFreeSpace;
            // double spaceToGet = spaceThreshold - currentfreeSpace;
            //20250326 Console.WriteLine("现有空间{0} GB", (_currentFreeSpace / 1024.0 / 1024.0 / 1024.0).ToString("F3"));
            localLog.Info("现有空间{0} TB", (_currentFreeSpace / 1024.0 / 1024.0 / 1024.0 / 1024.0).ToString("F5"));
            if (_currentFreeSpace >= spaceThreshold)
            {
                information = "磁盘空间充足，无需删除文件";
                localLog.Info("磁盘空间充足，无需删除文件");
                return;
            }
            localLog.Info("是否删除早期图片，可能需要几分钟");
            DialogResult result1 = MessageBox.Show("\n可用空间不足，只有：" + (_currentFreeSpace / 1024.0 / 1024.0 / 1024.0 / 1024.0).ToString("F5") + " TB"
                               + "\n\n是否删除早期图片?\n可能需要几分钟......",
                                "信息", MessageBoxButtons.OKCancel,
                                 MessageBoxIcon.Question);//MessageBoxButtons.YesNoCancel);//
            localLog.Info("开始删除过期图片及文件");
            DeleteOldFolders();
            return;
        }

        //void DeleteOldFolders(string folderPath, double spaceRequired)
        public void DeleteOldFolders()
        {
            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    information = "指定的文件夹不存在：" + targetFolder;
                    localLog.Error("指定的文件夹不存在：{0}", targetFolder);
                    return;
                }
                DirectoryInfo[] subDirectories = new DirectoryInfo(targetFolder).GetDirectories()
                                                                      .OrderBy(f => f.CreationTime)
                                                                      .ToArray();
                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    try
                    {
                        //file.Delete();	
                        //  currentFreeSpace += (double)GetDirectorySize(subDirectory.FullName); //  .FullName 把subDirectory的路径名也加了进来
                        //   Console.WriteLine("已删除文件夹: {0}", subDirectory);


                        double folderSize = (double)GetDirectorySize(subDirectory.FullName); //  .FullName 把subDirectory的路径名也加了进来
                        _currentFreeSpace += folderSize; //  .FullName 把subDirectory的路径名也加了进来

                        // information = "正删除文件夹: {0}" + subDirectory;
                        subDirectory.Delete(true);
                        localLog.Info("已删除文件夹: {0}, 现有空间{1} TB", subDirectory, (_currentFreeSpace / 1024.0 / 1024.0 / 1024.0 / 1024.0).ToString("F5"));
                        // information = "已删除文件夹: {0}" + subDirectory;
                        //if (spaceFreed >= spaceRequired)
                        if (_currentFreeSpace >= spaceThreshold)
                        {
                            information = "已成功释放足够的磁盘空间";
                            localLog.Info("已成功释放足够的磁盘空间");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        information = "删除文件夹时出错: " + subDirectory;
                        localLog.Error("删除文件夹 {0} 时出错: {1}", subDirectory, ex.Message);
                    }
                }

                if (_currentFreeSpace < spaceThreshold)
                {
                    information = "指定子文件夹已删除，\n 未能释放足够的磁盘空间";
                    localLog.Warn("指定子文件夹已删除，\n 未能释放足够的磁盘空间");
                }
            }
            catch (Exception ex)
            {
                //20250326 Console.WriteLine("发生错误: {0}", ex.Message);
                information = "删除文件夹时出错: " + ex.Message;
                localLog.Error("发生错误: {0}", ex.Message);
            }
        }

        static double GetDirectorySize(string directoryPath)
        {
            double size = 0;
            // 获取文件夹内的所有文件	
            string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                size += (double)fileInfo.Length;
            }
            return size;
        }
    }


}
