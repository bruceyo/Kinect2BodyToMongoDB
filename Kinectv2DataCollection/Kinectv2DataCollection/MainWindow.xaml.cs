//-------------------------------------------﻿﻿﻿
// YU Xinbo  
// bruce.xb.yu@connect.polyu.hk      
// PhD Candidate 
// in the Hong Kong PolyTechnical University   
//-------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Kinectv2DataCollection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary> KinectBodyView object which handles drawing the Kinect bodies to a View box in the UI </summary>
        private KinectBodyView kinectBodyView = null;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;
        DepthFrameReader depthFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        int frameNumber = 0;
        int countBodyArrivedFrames = 0;
        int receivedFrame = 1;
        string jointsFrames ="";//"{\"action\":\"walking\",\"frames\":[";

        class MapedValue
        {
           // public BodyFrame bodyFrame { get; set; }
            public List<Body> bodies { get; set; }

            public float A { get; set; }
            public float B { get; set; }
            public float C { get; set; }
            public float D { get; set; }

            public String time { get; set; }
        }

        float height = 0;

        string kid_name;
        int kid_stu_id;
        string kid_group;
        string kid_gender;
        string kid_birthday;

        ConcurrentDictionary<int, MapedValue> data = new ConcurrentDictionary<int, MapedValue>();
        Thread recordData = null;
        int kid_id;
        int duration = 0;
        System.Windows.Forms.Timer t;
        bool isRunning;

        MongoDbCon mongoCon = new MongoDbCon();
        protected String connectionString = "mongodb://192.168.142.128:27017";
        protected String mongo_DB = "test";

        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // initialize the BodyViewer object for displaying tracked bodies in the UI
            this.kinectBodyView = new KinectBodyView(this.kinectSensor);

            //this.bodyFrameReader = null;
            this.bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            
            this.depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            this.bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            InitializeComponent();

            // Binding the data context source for display in UI [Data Binding]
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;
        }

        private bool InitializeKinect2()
        {
            this.kinectSensor.Open();

            if (this.kinectSensor.IsOpen == true)
            {
                return true;
            }
            else 
            {
                System.Windows.Forms.MessageBox.Show("Please plug-in the Kinect device");
                return false;
            }
        }

        private void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            countBodyArrivedFrames++;
            bool dataReceived = false;
            List<Body> users = null;


            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    
                    //if (this.bodies == null)
                    //{
                    //    this.bodies = new Body[bodyFrame.BodyCount];
                    //}

                    this.bodies = new Body[bodyFrame.BodyCount];
                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
                else dataReceived = false;

                if (dataReceived)
                {
                    users = bodies.Where(s => s.IsTracked.Equals(true)).ToList();
                    if (countBodyArrivedFrames > 0 && users.Count > 0)
                    {

                        this.kinectBodyView.UpdateBodyFrame(this.bodies);
                        //    Console.WriteLine(body.IsTracked);
                        MapedValue mapedValue = new MapedValue();
                        mapedValue.A = bodyFrame.FloorClipPlane.X;
                        mapedValue.B = bodyFrame.FloorClipPlane.Y;
                        mapedValue.C = bodyFrame.FloorClipPlane.Z;
                        mapedValue.D = bodyFrame.FloorClipPlane.W;
                        String time = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFK");
                        // MongoDB insert failed when millisec only has two digitals (FF) 
                        mapedValue.time = time;
                        //Multiple users
                        mapedValue.bodies = bodies.ToList();

                        if (isRunning && time.Length == 24)
                        {
                            data[receivedFrame]= mapedValue;
                            receivedFrame++;
                        }
                    }
                }
            }
        }

        private void RecordDataThread()
        {
            Thread.Sleep(1000);
            int framecount = 1;
            
            //When there is skeleton data in the Dictionary
            while (true)
            {
                Thread.Sleep(34);
                if (data.Count > 0)
                {
                    MapedValue mapedValue_temp = data.FirstOrDefault(x => x.Key == framecount).Value;

                    //Console.WriteLine("Thread Start");
                    List<Body> users = mapedValue_temp.bodies;

                    //Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                    jointsFrames += "{\"count\":"                + framecount + 
                                    ",\"datetime\":ISODate(\""   + mapedValue_temp.time +
                                    "\"),\"name\":\""            + kid_name +
                                    "\",\"stu_id\":\""           + kid_stu_id +
                                    "\",\"group\":\""            + kid_group +
                                    "\",\"gender\":\""           + kid_gender +
                                    "\",\"birthday\":\""         + kid_birthday +
                                    "\",\"skeletons\":[{\"id\":0,\"joints\":[";
                    jointsFrames += structureSkeletonJson(mapedValue_temp.bodies[kid_id], mapedValue_temp.A, mapedValue_temp.B, mapedValue_temp.C, mapedValue_temp.D);
                    
                    users.RemoveAt(kid_id);
                    users = users.Where(s => s.IsTracked.Equals(true)).ToList();
                    if (users.Count > 0 && users[0].IsTracked == true)
                    {
                        
                        jointsFrames += ",{\"id\":1,\"joints\":[";
                        jointsFrames += structureSkeletonJson(users[0], mapedValue_temp.A, mapedValue_temp.B, mapedValue_temp.C, mapedValue_temp.D);
                    }
                    jointsFrames += "]}";

                    Console.WriteLine("Frame " + framecount + ": " + jointsFrames);

                    //Insert record to MongoDB
                   // mongoCon.insert(jointsFrames);

                    jointsFrames = "";
                    data.TryRemove(framecount, out mapedValue_temp);
                    framecount++;
                }
                else
                {

                    Dispatcher.Invoke(new Action(()=>{
                        btn_KinectSwitch.IsEnabled = true;
                        btn_RecordSwitch.IsEnabled = true;
                        btn_KinectSwitch.Content = "Kinect ON";
                        duration = Int32.Parse(txt_duration.Text) * 60;
                        data.Clear();
                        receivedFrame = 1;
                        lbl_insert_progress.Content = "Inserted to MongoDB!";
                        recordData.Abort();
                    }));

                } 
            }
        }

        public string structureSkeletonJson(Body body, float A, float B, float C, float D)
        {
            string skeleton_json="";

            //Joints Data
            JointType last = body.Joints.Keys.Last();
            foreach (JointType jointType in body.Joints.Keys)
            {
                // sometimes the depth(Z) of an inferred joint may show as negative
                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                CameraSpacePoint position = body.Joints[jointType].Position;
                if (position.Z < 0)
                {
                    position.Z = InferredZPositionClamp;
                }

                float additem1 = 0;
                float additem2 = 0;
                float additem3 = 0;

                additem1 = A * body.Joints.FirstOrDefault(x => x.Key == jointType).Value.Position.X;
                additem2 = B * body.Joints.FirstOrDefault(x => x.Key == jointType).Value.Position.Y;
                additem3 = C * body.Joints.FirstOrDefault(x => x.Key == jointType).Value.Position.Z;
                float additem1_d = A * A;
                float additem2_d = B * B;
                float additem3_d = C * C;
                float numerator = additem1 + additem2 + additem3 + D;
                float denominator = additem1_d + additem2_d + additem3_d;
                height = numerator / (float)System.Math.Sqrt(denominator);

                if (jointType.Equals(last))
                {
                    skeleton_json += "{\"jointType\":\"" + jointType + "\",\"TrackingState\":\"" + body.Joints[jointType].TrackingState + "\",\"3d\": {\"x\":" + position.X + ",\"y\":" + position.Y + ",\"z\":" + position.Z + ",\"h\":" + height + "}}]";
                }
                else
                {
                    skeleton_json += "{\"jointType\":\"" + jointType + "\",\"TrackingState\":\"" + body.Joints[jointType].TrackingState + "\",\"3d\": {\"x\":" + position.X + ",\"y\":" + position.Y + ",\"z\":" + position.Z + ",\"h\":" + height + "}},";
                }
            }
            //Joints orientation
            skeleton_json += ",\"jointOrientations\":[";
            foreach (JointType jointType in body.JointOrientations.Keys)
            {
                var orientation = body.JointOrientations[jointType].Orientation;
                if (jointType.Equals(last))
                {
                    skeleton_json += "{\"jointType\":\"" + jointType + "\",\"orientation\": {\"x\":" + orientation.X + ",\"y\":" + orientation.Y + ",\"z\":" + orientation.Z + ",\"w\":" + orientation.W + "}}]";
                }
                else
                {
                    skeleton_json += "{\"jointType\":\"" + jointType + "\",\"orientation\": {\"x\":" + orientation.X + ",\"y\":" + orientation.Y + ",\"z\":" + orientation.Z + ",\"w\":" + orientation.W + "}},";
                }
            }
            //Hands data
            skeleton_json += ",\"handstates\":[{"+"\"lefthand\":\"" + body.HandLeftState + "\",\"confidence\":\"" + body.HandLeftConfidence + "\"},{\"righthand\":\"" + body.HandRightState + "\",\"confidence\":\"" + body.HandRightConfidence +"\"}]";

            //IsRestricted
            skeleton_json += ",\"isrestricted\":\"" + body.IsRestricted + "\"";

            //Lean
            skeleton_json += ",\"lean\":{\"leantrackingstate\":\"" + body.LeanTrackingState + "\",\"x\":" + body.Lean.X + ",\"y\":" + body.Lean.Y+"}";

            //ClippedEdge
            skeleton_json += ",\"ClippedEdge\":\"" + body.ClippedEdges + "\"";

            //activity, appearence, expression are replaced by HD Face
            return skeleton_json + "}";
        }

        //Mark a skeleton as a kid
        private void skeleton_color_DropDownClosed(object sender, EventArgs e)
        {
            skeletonFocus();
        }

        public void skeletonFocus() {
            List<Pen> bodyColors = new List<Pen>();

            bodyColors.Add(new Pen(Brushes.Red, 5));
            bodyColors.Add(new Pen(Brushes.Orange, 5));
            bodyColors.Add(new Pen(Brushes.Green, 5));
            bodyColors.Add(new Pen(Brushes.Blue, 5));
            bodyColors.Add(new Pen(Brushes.Indigo, 5));
            bodyColors.Add(new Pen(Brushes.Violet, 5));

            Console.WriteLine("Color : " + skeleton_color.Text.ToString());
            switch (skeleton_color.Text.ToString())
            {
                case "Red":
                    bodyColors[0] = new Pen(Brushes.Red, 10);
                    kid_id = 0;
                    break;
                case "Orange":
                    bodyColors[1] = new Pen(Brushes.Orange, 10);
                    kid_id = 1;
                    break;
                case "Green":
                    bodyColors[2] = new Pen(Brushes.Green, 10);
                    kid_id = 2;
                    break;
                case "Blue":
                    bodyColors[3] = new Pen(Brushes.Blue, 10);
                    kid_id = 3;
                    break;
                case "Indigo":
                    bodyColors[4] = new Pen(Brushes.Indigo, 10);
                    kid_id = 4;
                    break;
                case "Violet":
                    bodyColors[5] = new Pen(Brushes.Violet, 10);
                    kid_id = 5;
                    break;
                default:
                    bodyColors[0] = new Pen(Brushes.Red, 10);
                    kid_id = 0;
                    break;
            }
            kinectBodyView.bodyColors = bodyColors;
        }

        private void btn_KinectSwitch_Click(object sender, RoutedEventArgs e)
        {
            btn_KinectSwitch.IsEnabled = false;
            if (btn_KinectSwitch.Content.ToString().Trim() == "Kinect ON")
            {

                InitializeKinect2();
                btn_KinectSwitch.Content = "Kinect OFF";

            }
            else {
                if (this.bodyFrameReader != null)
                {
                    this.kinectSensor.Close();
                    this.kinectBodyView.ClearBodyFrame();
                    btn_KinectSwitch.Content = "Kinect ON";
                }
            }
            btn_KinectSwitch.IsEnabled = true;
        }

        private void btn_RecordSwitch_Click(object sender, RoutedEventArgs e)
        {
            skeletonFocus();
            if (txt_kid_name.Text.ToString() == "" || txt_kid_id.Text.ToString() == "" || if_special.Text.ToString() == "" || cb_kid_gender.Text.ToString() == "" || dp_kid_birthday.SelectedDate > DateTime.Now )
            {
                System.Windows.Forms.MessageBox.Show("Kid's info. is not completed!");
                return;
            }
            if (txt_duration == null || txt_duration.Text == "" ) { System.Windows.Forms.MessageBox.Show("Please set the duration!"); return; }

            kid_name = txt_kid_name.Text.ToString();
            kid_stu_id = Int32.Parse(txt_kid_id.Text);
            kid_group = if_special.Text.ToString() == "Typical Control" ? "T" : "S";
            kid_gender = cb_kid_gender.Text.ToString() == "Boy"? "B":"G";
            kid_birthday = dp_kid_birthday.SelectedDate.ToString();
            if (this.bodies[kid_id].IsTracked == true)
            {

                btn_RecordSwitch.IsEnabled = false;
                btn_KinectSwitch.IsEnabled = false;

                recordData = new Thread(new ThreadStart(RecordDataThread));

                t = new System.Windows.Forms.Timer();
                t.Interval = 1000; // specify interval time as you want
                t.Tick += new EventHandler(timer_Tick);
                t.Start();
                isRunning = true;

                duration = Int32.Parse(txt_duration.Text) * 60;
                recordData.Start();
                lbl_insert_progress.Content = "Inserting to MongoDB...";
            }
            else {
                System.Windows.Forms.MessageBox.Show("Please Mark Kid's skeleton before start Kinect data collection!");
            }
        }

        //Timer handler function
        void timer_Tick(object sender, EventArgs e)
        {
            if (!isRunning)
                return;

            if (duration > 0)
            {
                duration--;
                this.lbl_timer.Content = "Remained: "+ duration / 60 + ":" + duration % 60;

            }else {
                isRunning = false;
                //Stop the kinect v2 sensor
                if (this.bodyFrameReader != null)
                {  
                    this.bodyFrameReader.FrameArrived -= bodyFrameReader_FrameArrived;
                    this.kinectSensor.Close();
                    this.kinectBodyView.ClearBodyFrame();

                }
                t.Tick -= new EventHandler(timer_Tick);
                t.Stop();
                t.Dispose();
            }
        }

        private void txt_duration_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Back))
            {
                e.Handled = true;
            }  
        }

        private void btn_ConnectDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(txt_mongo_ip.Text, Int32.Parse(txt_mongo_port.Text));

                connectionString = "mongodb://" + txt_mongo_ip.Text + ":" + txt_mongo_port.Text;
                mongo_DB = txt_mongo_db.Text;
                mongoCon._client = new MongoClient(connectionString);
                mongoCon._database = mongoCon._client.GetDatabase(mongo_DB);

                var collection = mongoCon._database.GetCollection<BsonDocument>(txt_mongo_col.Text);
                var filter = new BsonDocument();
                BsonDocument cursor = collection.Find(filter).First<BsonDocument>();

                pic_mongo_icon.Source = new BitmapImage(new Uri("pack://application:,,,/pics/mongo_on.png"));
            }
            catch (Exception excep)
            {
                System.Windows.Forms.MessageBox.Show(excep.Message, excep.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                pic_mongo_icon.Source = new BitmapImage(new Uri("pack://application:,,,/pics/mongo_off.png"));
                return;
            }
        }
    }
}
