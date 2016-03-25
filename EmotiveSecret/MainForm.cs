using NeuroSky.ThinkGear;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmotiveSecret
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

      
        static Connector connector;
        static bool golfZoneDemo = false;
        static double task_famil_baseline, task_famil_cur, task_famil_change;
        static bool task_famil_first;
        static double mental_eff_baseline, mental_eff_cur, mental_eff_change;
        static bool mental_eff_first;


        static double LastAttention = 0;
        static double LastMeditation = 0;

        //List<ThinkGearValue> EEG_DataList = new List<ThinkGearValue>();
        List<double> DeltaList = new List<double>();
        List<double> ThetaList = new List<double>();
        List<double> Alpha1List = new List<double>();
        List<double> Alpha2List = new List<double>();
        List<double> Beta1List = new List<double>();
        List<double> Beta2List = new List<double>();
        List<double> Gamma1List = new List<double>();
        List<double> Gamma2List = new List<double>();


    


        /**
       * Called when a device is connected 
       已经连接设备成功后被触发
       */
        private void OnDeviceConnected(object sender, EventArgs e)
        {
            Connector.DeviceEventArgs de = (Connector.DeviceEventArgs)e;

            Console.WriteLine("Device found on: " + de.Device.PortName);
            //this.ChangeLabelText("Device found on: " + de.Device.PortName,this.lb_Message);
            MyMsgBoxShow("设备位于: " + de.Device.PortName);
            de.Device.DataReceived += new EventHandler(OnDataReceived);
        }

        /**
         * Called when scanning fails
         连接设备出错时被触发
         */
        private void OnDeviceFail(object sender, EventArgs e)
        {
            Console.WriteLine("No devices found! :(");
            //this.ChangeLabelText("No devices found! :(",this.lb_Message);
            MyMsgBoxShow("未找到脑波设备!");
        }

        /**
         * Called when each port is being validated
         验证端口时被触发
         */
        private void OnDeviceValidating(object sender, EventArgs e)
        {
            Console.WriteLine("Validating: ");
            //this.ChangeLabelText("Validating: ",this.lb_Message);
            MyMsgBoxShow("正在寻找设备....");
        }

        private static byte rcv_poorSignal_last = 255; // start with impossible value


        private static byte rcv_poorSignal;
        private static byte rcv_poorSig_cnt = 0;


        /**
         * Called when data is received from a device
         收到数据时被触发
         */
        private void OnDataReceived(object sender, EventArgs e)
        {
            string tempmsg = "";
            //Device d = (Device)sender;
            Device.DataEventArgs de = (Device.DataEventArgs)e;
            NeuroSky.ThinkGear.DataRow[] tempDataRowArray = de.DataRowArray;

            TGParser tgParser = new TGParser();
            tgParser.Read(de.DataRowArray);

            /* Loop through new parsed data */
            for (int i = 0; i < tgParser.ParsedData.Length; i++)
            {
                if (tgParser.ParsedData[i].ContainsKey("MSG_MODEL_IDENTIFIED"))
                {
                    Console.WriteLine("Model Identified");
                    //this.ChangeLabelText("Model Identified", this.lb_Message);
                    MyMsgBoxShow("确认模型。");
                    connector.setMentalEffortRunContinuous(true);
                    connector.setMentalEffortEnable(true);
                    connector.setTaskFamiliarityRunContinuous(true);
                    connector.setTaskFamiliarityEnable(true);
                    connector.setPositivityEnable(false);
                    //
                    // the following are included to demonstrate the overide messages
                    //
                    connector.setRespirationRateEnable(true); // not allowed with EEG
                    connector.setPositivityEnable(true);// not allowed when famil/diff are enabled
                }
                if (tgParser.ParsedData[i].ContainsKey("MSG_ERR_CFG_OVERRIDE"))
                {
                    Console.WriteLine("ErrorConfigurationOverride: " + tgParser.ParsedData[i]["MSG_ERR_CFG_OVERRIDE"]);
                    //this.ChangeLabelText("ErrorConfigurationOverride: " + tgParser.ParsedData[i]["MSG_ERR_CFG_OVERRIDE"],this.lb_Message);
                    MyMsgBoxShow("ErrorConfigurationOverride: " + tgParser.ParsedData[i]["MSG_ERR_CFG_OVERRIDE"]);
                }
                if (tgParser.ParsedData[i].ContainsKey("MSG_ERR_NOT_PROVISIONED"))
                {
                    Console.WriteLine("ErrorModuleNotProvisioned: " + tgParser.ParsedData[i]["MSG_ERR_NOT_PROVISIONED"]);
                    //this.ChangeLabelText("ErrorModuleNotProvisioned: " + tgParser.ParsedData[i]["MSG_ERR_NOT_PROVISIONED"],this.lb_Message);
                    MyMsgBoxShow("ErrorModuleNotProvisioned: " + tgParser.ParsedData[i]["MSG_ERR_NOT_PROVISIONED"]);
                }

                ////
                if (tgParser.ParsedData[i].ContainsKey("TimeStamp"))
                {
                    //Console.WriteLine("TimeStamp");
                }
                if (tgParser.ParsedData[i].ContainsKey("Raw"))
                {
                    //Console.WriteLine("Raw: " + tgParser.ParsedData[i]["Raw"]);
                }
                if (tgParser.ParsedData[i].ContainsKey("RespiratoryRate"))
                {
                    //Console.WriteLine("RespiratoryRate: " + tgParser.ParsedData[i]["RespiratoryRate"]);
                }
                if (tgParser.ParsedData[i].ContainsKey("RawCh1"))
                {
                    //Console.WriteLine("RawCh1: " + tgParser.ParsedData[i]["RawCh1"]);
                }
                if (tgParser.ParsedData[i].ContainsKey("RawCh2"))
                {
                    //Console.Write(", Raw Ch2:" + tgParser.ParsedData[i]["RawCh2"]);
                }
                if (tgParser.ParsedData[i].ContainsKey("PoorSignal"))
                {
                    // NOTE: this doesn't work well with BMD sensors Dual Headband or CardioChip

                    rcv_poorSignal = (byte)tgParser.ParsedData[i]["PoorSignal"];
                    if (rcv_poorSignal != rcv_poorSignal_last || rcv_poorSig_cnt >= 30)
                    {
                        // when there is a change of state OR every 30 reports
                        rcv_poorSig_cnt = 0; // reset counter
                        rcv_poorSignal_last = rcv_poorSignal;
                        if (rcv_poorSignal == 0)
                        {
                            // signal is good, we are connected to a subject
                            Console.WriteLine("SIGNAL: we have good contact with the subject");
                            //this.ChangeLabelText("SIGNAL: we have good contact with the subject", this.lb_Message);
                            MyMsgBoxShow("SIGNAL:连接信号正常.");
                        }
                        else
                        {
                            Console.WriteLine("SIGNAL: is POOR: " + rcv_poorSignal);
                            //this.ChangeLabelText("SIGNAL: is POOR: " + rcv_poorSignal, this.lb_Message);
                            MyMsgBoxShow("SIGNAL:连接信号较弱:" + rcv_poorSignal);
                        }
                    }
                    else rcv_poorSig_cnt++;
                }

                #region 各种数据 
                //Delta  δ
                if (tgParser.ParsedData[i].ContainsKey("EegPowerDelta"))
                {
                    if (tgParser.ParsedData[i]["EegPowerDelta"] != 0)
                    {
                        //EEG_DATA.EegPowerDelta = tgParser.ParsedData[i]["EegPowerDelta"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerDelta.Text = tgParser.ParsedData[i]["EegPowerDelta"].ToString();
                        //}));

                        if (DeltaList.Count >= 8)
                        {
                            DeltaList.RemoveAt(0);
                        }
                        DeltaList.Add(tgParser.ParsedData[i]["EegPowerDelta"]);
                    }
                }

                //Theta
                if (tgParser.ParsedData[i].ContainsKey("EegPowerTheta"))
                {
                    if (tgParser.ParsedData[i]["EegPowerTheta"] != 0)
                    {
                        // EEG_DATA.EegPowerTheta = tgParser.ParsedData[i]["EegPowerTheta"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerTheta.Text = tgParser.ParsedData[i]["EegPowerTheta"].ToString();
                        //}));
                        if (ThetaList.Count >= 8)
                        {
                            ThetaList.RemoveAt(0);
                        }
                        ThetaList.Add(tgParser.ParsedData[i]["EegPowerTheta"]);
                    }
                }

                //Alpha
                if (tgParser.ParsedData[i].ContainsKey("EegPowerAlpha1"))
                {
                    if (tgParser.ParsedData[i]["EegPowerAlpha1"] != 0)
                    {

                        //EEG_DATA.EegPowerAlpha1 = tgParser.ParsedData[i]["EegPowerAlpha1"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerAlpha1.Text = tgParser.ParsedData[i]["EegPowerAlpha1"].ToString();
                        //}));

                        if (Alpha1List.Count >= 8)
                        {
                            Alpha1List.RemoveAt(0);
                        }
                        Alpha1List.Add(tgParser.ParsedData[i]["EegPowerAlpha1"]);
                    }
                }
                if (tgParser.ParsedData[i].ContainsKey("EegPowerAlpha2"))
                {
                    if (tgParser.ParsedData[i]["EegPowerAlpha2"] != 0)
                    {

                        // EEG_DATA.EegPowerAlpha2 = tgParser.ParsedData[i]["EegPowerAlpha2"];

                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerAlpha2.Text = tgParser.ParsedData[i]["EegPowerAlpha2"].ToString();
                        //}));

                        if (Alpha2List.Count >= 8)
                        {
                            Alpha2List.RemoveAt(0);
                        }
                        Alpha2List.Add(tgParser.ParsedData[i]["EegPowerAlpha2"]);
                    }
                }


                //Beta  
                if (tgParser.ParsedData[i].ContainsKey("EegPowerBeta1"))
                {
                    tempmsg += "Beta1";
                    if (tgParser.ParsedData[i]["EegPowerBeta1"] != 0)
                    {
                        // EEG_DATA.EegPowerBeta1 = tgParser.ParsedData[i]["EegPowerBeta1"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerBeta1.Text = tgParser.ParsedData[i]["EegPowerBeta1"].ToString();
                        //}));
                        if (Beta1List.Count >= 8)
                        {
                            Beta1List.RemoveAt(0);
                        }
                        Beta1List.Add(tgParser.ParsedData[i]["EegPowerBeta1"]);
                    }
                }
                if (tgParser.ParsedData[i].ContainsKey("EegPowerBeta2"))
                {
                    if (tgParser.ParsedData[i]["EegPowerBeta2"] != 0)
                    {
                        //EEG_DATA.EegPowerBeta2 = tgParser.ParsedData[i]["EegPowerBeta2"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerBeta2.Text = tgParser.ParsedData[i]["EegPowerBeta2"].ToString();
                        //}));
                        if (Beta2List.Count >= 8)
                        {
                            Beta2List.RemoveAt(0);
                        }
                        Beta2List.Add(tgParser.ParsedData[i]["EegPowerBeta2"]);
                    }
                }

                //Gamma
                if (tgParser.ParsedData[i].ContainsKey("EegPowerGamma1"))
                {
                    if (tgParser.ParsedData[i]["EegPowerGamma1"] != 0)
                    {
                        //EEG_DATA.EegPowerGamma1 = tgParser.ParsedData[i]["EegPowerGamma1"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerGamma1.Text = tgParser.ParsedData[i]["EegPowerGamma1"].ToString();
                        //}));
                        if (Gamma1List.Count >= 8)
                        {
                            Gamma1List.RemoveAt(0);
                        }
                        Beta2List.Add(tgParser.ParsedData[i]["EegPowerGamma1"]);
                    }

                }
                if (tgParser.ParsedData[i].ContainsKey("EegPowerGamma2"))
                {
                    if (tgParser.ParsedData[i]["EegPowerGamma2"] != 0)
                    {
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_EegPowerGamma2.Text = tgParser.ParsedData[i]["EegPowerGamma2"].ToString();
                        //}));

                        //EEG_DATA.EegPowerGamma2 = tgParser.ParsedData[i]["EegPowerGamma2"];

                        if (Gamma2List.Count >= 8)
                        {
                            Gamma2List.RemoveAt(0);
                        }
                        Beta2List.Add(tgParser.ParsedData[i]["EegPowerGamma2"]);
                    }

                }



                //Attention 专注度
                if (tgParser.ParsedData[i].ContainsKey("Attention"))
                {
                    if (tgParser.ParsedData[i]["Attention"] != 0)
                    {
                        if (tgParser.ParsedData[i]["Attention"] != LastAttention)
                        {
                            float attentionAngel = GetAngel(tgParser.ParsedData[i]["Attention"] - LastAttention);
                            RotateFormCenter(this.pb_Attention, attentionAngel);
                            LastAttention = tgParser.ParsedData[i]["Attention"];
                        }
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_Attention.Text = tgParser.ParsedData[i]["Attention"].ToString();
                        //}));
                    }
                }

                //Meditation 放松度
                if (tgParser.ParsedData[i].ContainsKey("Meditation"))
                {
                    if (tgParser.ParsedData[i]["Meditation"] != 0)
                    {
                        if (tgParser.ParsedData[i]["Meditation"] != LastMeditation)
                        {
                            float meditationAngel = GetAngel(tgParser.ParsedData[i]["Meditation"] - LastMeditation);
                            RotateFormCenter(this.pb_Meditation, meditationAngel);
                            LastMeditation = tgParser.ParsedData[i]["Meditation"];
                        }
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_Meditation.Text = tgParser.ParsedData[i]["Meditation"].ToString();
                        //}));
                    }
                }


                #endregion




                if (!golfZoneDemo) // turn this off for the Golf Zone Demo  演示
                {
                    if (tgParser.ParsedData[i].ContainsKey("BlinkStrength"))
                    {
                        Console.WriteLine("\t\tBlinkStrength: " + tgParser.ParsedData[i]["BlinkStrength"]);
                        //EEG_DATA.BlinkStrength = (Double)tgParser.ParsedData[i]["BlinkStrength"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_BlinkStrength.Text = tgParser.ParsedData[i]["BlinkStrength"].ToString();
                        //}));
                    }

                    if (tgParser.ParsedData[i].ContainsKey("MentalEffort"))
                    {
                        //EEG_DATA.MentalEffort = (Double)tgParser.ParsedData[i]["MentalEffort"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_MentalEffort.Text = tgParser.ParsedData[i]["MentalEffort"].ToString();
                        //}));
                        mental_eff_cur = (Double)tgParser.ParsedData[i]["MentalEffort"];
                        if (mental_eff_first)
                        {
                            mental_eff_first = false;
                        }
                        else {
                            /*
                             * calculate the percentage change from the previous sample
                             * 计算从以前的样本的百分比变化
                             */
                            mental_eff_change = calcPercentChange(mental_eff_baseline, mental_eff_cur);
                            if (mental_eff_change > 500.0 || mental_eff_change < -500.0)
                            {
                                Console.WriteLine("\t\tMental Effort: excessive range");
                                //this.ChangeLabelText("Mental Effort: excessive range",this.lb_Message);
                                MyMsgBoxShow("Mental Effort:超出范围!");
                            }
                            else {
                                Console.WriteLine("\t\tMental Effort: " + mental_eff_change + " %");
                                //this.ChangeLabelText("Mental Effort: " + mental_eff_change + " %", this.lb_Message);
                                MyMsgBoxShow("\t\tMental Effort: " + mental_eff_change + " %");
                            }
                        }
                        mental_eff_baseline = mental_eff_cur;
                    }

                    if (tgParser.ParsedData[i].ContainsKey("TaskFamiliarity"))
                    {
                        // EEG_DATA.TaskFamiliarity = (Double)tgParser.ParsedData[i]["TaskFamiliarity"];
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_TaskFamiliarity.Text = tgParser.ParsedData[i]["TaskFamiliarity"].ToString();
                        //}));
                        task_famil_cur = (Double)tgParser.ParsedData[i]["TaskFamiliarity"];
                        if (task_famil_first)
                        {
                            task_famil_first = false;
                        }
                        else {
                            /*
                             * calculate the percentage change from the previous sample
                             * 计算从以前的样本的百分比变化
                             */
                            task_famil_change = calcPercentChange(task_famil_baseline, task_famil_cur);
                            if (task_famil_change > 500.0 || task_famil_change < -500.0)
                            {
                                Console.WriteLine("\t\tTask Familiarity: excessive range");
                                //this.ChangeLabelText("Task Familiarity: excessive range",this.lb_Message);
                                MyMsgBoxShow("Task Familiarity:超出范围!");
                            }
                            else {
                                Console.WriteLine("\t\tTask Familiarity: " + task_famil_change + " %");
                                //this.ChangeLabelText("Task Familiarity: " + task_famil_change + " %", this.lb_Message);
                                this.MyMsgBoxShow("Task Familiarity: " + task_famil_change + " %");
                            }
                        }
                        task_famil_baseline = task_famil_cur;
                    }

                    if (tgParser.ParsedData[i].ContainsKey("Positivity"))
                    {
                        // EEG_DATA.Positivity = tgParser.ParsedData[i].ContainsKey("Positivity");
                        //this.Invoke(new MethodInvoker(delegate ()
                        //{
                        //    this.lb_Positivity.Text = tgParser.ParsedData[i]["Positivity"].ToString();
                        //}));
                        Console.WriteLine("\t\tPositivity: " + tgParser.ParsedData[i]["Positivity"]);
                        //this.ChangeLabelText("Positivity: " + tgParser.ParsedData[i]["Positivity"], this.lb_Message);
                        MyMsgBoxShow("Positivity: " + tgParser.ParsedData[i]["Positivity"]);
                    }
                }
                if (golfZoneDemo)
                {
                    if (tgParser.ParsedData[i].ContainsKey("ReadyZone"))
                    {
                        Console.Write("\t\tGolfZone: ");
                        if (tgParser.ParsedData[i]["ReadyZone"] == 3)
                        {
                            Console.WriteLine("Elite: you are the best, putt when you are ready");
                        }
                        else if (tgParser.ParsedData[i]["ReadyZone"] == 2)
                        {
                            Console.WriteLine("Intermediate: you are good, relax and putt smoothly");
                        }
                        else if (tgParser.ParsedData[i]["ReadyZone"] == 1)
                        {
                            Console.WriteLine("Beginner: take a breath and don't rush");
                        }
                        else
                        {
                            Console.WriteLine("Try to relax, focus on your target");
                        }
                    }
                }
            }
            DrawLineGraph(this.pb_EEG);
        }

        private void MyMsgBoxShow(string msg)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                this.tb_Msg.Text += msg + "\n";
                this.tb_Msg.Focus();
            }));
        }

        //旋转PictureBox
        private void RotateFormCenter(PictureBox pb, float angle)
        {
            Image img = pb.Image;
            int newWidth = Math.Max(img.Height, img.Width);
            Bitmap bmp = new Bitmap(newWidth, newWidth);
            Graphics g = Graphics.FromImage(bmp);
            Matrix x = new Matrix();
            PointF point = new PointF(img.Width / 2f, img.Height / 2f);
            x.RotateAt(angle, point);
            g.Transform = x;
            g.DrawImage(img, 0, 0);
            g.Dispose();
            img = bmp;
            pb.Image = img;
        }

        private void ChangeLabelText(string msg, Label label)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                label.Text = msg;
            }));
        }

        static double calcPercentChange(double baseline, double current)
        {
            double change;

            if (baseline == 0.0) baseline = 1.0; //don't allow divide by zero
                                                 /*
                                                  * calculate the percentage change
                                                  */
            change = current - baseline;
            change = (change / baseline) * 1000.0 + 0.5;
            change = Math.Floor(change) / 10.0;
            return (change);
        }

        void DrawLineGraph(PictureBox pb)
        {
            Graphics g = pb.CreateGraphics();
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            DrawList(DeltaList, pb, g, new Pen(Color.Red, 3));
            DrawList(ThetaList, pb, g, new Pen(Color.Yellow, 3));
            DrawList(Alpha1List, pb, g, new Pen(Color.Blue, 3));
            DrawList(Alpha2List, pb, g, new Pen(Color.Pink, 3));
            DrawList(Beta1List, pb, g, new Pen(Color.Orange, 3));
            DrawList(Beta2List, pb, g, new Pen(Color.Cyan, 3));
            DrawList(Gamma1List, pb, g, new Pen(Color.OliveDrab, 3));
            DrawList(Gamma2List, pb, g, new Pen(Color.DarkOrange, 3));
            g.Dispose();
        }

        void DrawList(List<double> datalist, PictureBox pb, Graphics g, Pen pen)
        {
            int widthx = pb.Size.Width / 5;
            for (int i = 0; i < datalist.Count - 1; i++)
            {
                Point p1 = new Point(widthx * i, (GetY(pb.Size.Height, (int)datalist[i])));
                Point p2 = new Point(widthx * (i + 1), (GetY(pb.Size.Height, (int)datalist[i + 1])));
                g.DrawLine(pen, p1, p2);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Initialize a new Connector and add event handlers
            connector = new Connector();
            connector.DeviceConnected += new EventHandler(OnDeviceConnected);
            connector.DeviceConnectFail += new EventHandler(OnDeviceFail);
            connector.DeviceValidating += new EventHandler(OnDeviceValidating);

            // Scan for devices
            // add this one to scan 1st
            //connector.ConnectScan("COM40");
            connector.ConnectScan("COM17");
            //start the mental effort and task familiarity calculations
            if (golfZoneDemo)
            {
                connector.setMentalEffortEnable(false);
                connector.setTaskFamiliarityEnable(false);
                connector.setBlinkDetectionEnabled(false);
            }
            else {
                connector.enableTaskDifficulty(); //depricated
                connector.enableTaskFamiliarity(); //depricated

                connector.setMentalEffortRunContinuous(true);
                connector.setMentalEffortEnable(true);
                connector.setTaskFamiliarityRunContinuous(true);
                connector.setTaskFamiliarityEnable(true);

                connector.setBlinkDetectionEnabled(true);
            }
            task_famil_baseline = task_famil_cur = task_famil_change = 0.0;
            task_famil_first = true;
            mental_eff_baseline = mental_eff_cur = mental_eff_change = 0.0;
            mental_eff_first = true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            connector.Close();
            this.Dispose();
            Application.Exit();
        }

        //获取角度
        float GetAngel(double value)
        {
            float result = 270 * ((float)value / 100);
            return result;
        }

        //获取在坐标轴Y轴的坐标
        int GetY(int heigh, int value)
        {
            int heighy = 1000000 / heigh;
            int result = (1000000 - value) / heighy;
            return result;
        }


        private void tb_Msg_TextChanged(object sender, EventArgs e)
        {
            this.tb_Msg.SelectionStart = this.tb_Msg.Text.Length;
            this.tb_Msg.SelectionLength = 0;
            this.tb_Msg.Focus();
        }
    }
}
