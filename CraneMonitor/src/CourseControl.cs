using System;
using System.Collections;

namespace CraneMonitor
{
    [Serializable]
    public class PlayRecord : IComparable
    {
        public PlayRecord() { SetInitValue(); }

        private bool[] hits;
        private TimeSpan record_time;

        public bool[] Hits
        {
            get { return hits; }
            set
            {
                hits = new bool[value.Length];
                for (int i = 0; i < value.Length; i++) hits[i] = value[i];
            }
        }
        // Public Property - XmlIgnore as it doesn't serialize anyway
        [System.Xml.Serialization.XmlIgnore]
        public TimeSpan RecordTime
        {
            get { return record_time; }
            set { record_time = value; }
        }
        // Pretend property for serialization
        [System.Xml.Serialization.XmlElement("RecordTimeAsTicks")]
        public long RecordTimeAsTicks
        {
            get { return record_time.Ticks; }
            set { record_time = new TimeSpan(value); }
        }

        public DateTime StartTime { get; set; }
        public string Name { get; set; }
        //public bool Gender { get; set; }

        public void SetInitValue()  // default parameters
        {
            hits = new bool[36];
            for (int i = 0; i < 36; i++) hits[i] = false;
            RecordTime = TimeSpan.Zero;
            StartTime = DateTime.Now;
            Name = "Anonymous";
            //Gender = true;
        }

        public int GetNumHit()
        {
            int NumHit = 0;
            for (int i = 0; i < hits.Length; i++) NumHit += hits[i] ? 1 : 0;
            return NumHit;
        }

        private TimeSpan GetPenaltiedTime()
        {
            int NumHit = GetNumHit();
            return RecordTime;// + new TimeSpan(0, 0, NumHit);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) throw new ArgumentNullException();
            PlayRecord other = obj as PlayRecord;

            TimeSpan time1 = GetPenaltiedTime();
            TimeSpan time2 = other.GetPenaltiedTime();
            return (time1 < time2) ? -1 : 1;
        }

        public string GetTimeAsText()
        {
            return string.Format("{0:00}:{1:00}:{2:00}", RecordTime.Minutes, RecordTime.Seconds, RecordTime.Milliseconds/10);
        }

        public string GetPenaltiedTimeAsText()
        {
            TimeSpan time = GetPenaltiedTime();
            return string.Format("{0:00}:{1:00}:{2:00}", time.Minutes, time.Seconds, time.Milliseconds/10);
        }
    }

    public class PlayRecordSet
    {
        private System.Collections.Generic.List<PlayRecord> RecordSet = new System.Collections.Generic.List<PlayRecord>();
        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(PlayRecord));

        public PlayRecordSet()
        {
            ReadFiles();
        }

        private string GetPath()
        {
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetEntryAssembly();
            string[] SplPath = myAssembly.Location.Split('\\');
            SplPath[SplPath.Length - 1] = "";
            string path = string.Join("\\", SplPath);
            return path;
        }

        public void ReadFiles()
        {
            string[] files = System.IO.Directory.GetFiles(GetPath());
            foreach (string file in files)
            {
                string ext = System.IO.Path.GetExtension(file);
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                if (ext == ".xml" && name.Substring(0, 6) == "record")
                {
                    try
                    {
                        using (System.IO.FileStream stream = new System.IO.FileStream(file, System.IO.FileMode.Open))
                        {
                            PlayRecord item = serializer.Deserialize(stream) as PlayRecord;
                            if (item == null) item = new PlayRecord();
                            RecordSet.Add(item);
                        }
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show(e.ToString(), file);
                        return;
                    }
                }
            }
        }

        public bool SaveAndAdd(PlayRecord item)
        {
            DateTime time = DateTime.Now;
            string filename = string.Format("record{0:0000}{1:00}{2:00}_{3:00}{4:00}{5:00}.xml", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
            try
            {
                using (System.IO.FileStream stream = new System.IO.FileStream(GetPath() + filename, System.IO.FileMode.Create))
                {
                    serializer.Serialize(stream, item);
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString(), filename);
                return false;
            }
            RecordSet.Add(item);
            return true;
        }

        public PlayRecord[] GetTopRanking(int count)
        {
            PlayRecord[] rank = new PlayRecord[count];
            PlayRecord[] list = RecordSet.ToArray();
            Array.Sort(list);
            for (int i = 0; i < count; i++) rank[i] = list[i];
            return rank;
        }

        public void GetRanking(PlayRecord item, out int order, out int total)
        {
            order = 0;
            for (int i = 0; i < RecordSet.Count; i++)
            {
                if (RecordSet[i].CompareTo(item) < 0)
                    order++;
            }
            total = RecordSet.Count;
        }

        public int Size { get { return RecordSet.Count; } }
    }

    public delegate void AutoStartEventHandler(bool IsStartEvent);

    public class CourseControl
    {
        public MainWindow Form;
        public AutoStartEventHandler AutoStartHandler;

        public LogWindow log = null;
        private PlayRecordSet Records = new PlayRecordSet();

        public CourseControl()
        {
        }

        public void SetLog(LogWindow obj) { log = obj; }

        // ------------------------------------------------------------

        SocketClient DisplayClient = new CraneMonitor.SocketClient();
        //UdpClient DisplayClient = new CraneMonitor.UdpClient();

        public bool DisplayStart(string ip, int port)
        {
            DisplayClient.LogHandler = log.WriteLogWarning;
            DisplayClient.ErrorLogHandler = log.WriteLogError;
            //DisplayClient.ReceiveHandler = ImuReceiveHandler;

            DisplayClient.Init(ip, port);
            if (DisplayClient.Connected())
            {
                //ThreadBegin();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool DisplayStop()
        {
            DisplayClient.Close();
            if (!DisplayClient.Connected())
            {
                //ThreadEnd();
                return true;
            }
            else
            {
                return false;
            }
        }

        // ------------------------------------------------------------

        SerialClient Contact1Client = new CraneMonitor.SerialClient();
        SerialClient Contact2Client = new CraneMonitor.SerialClient();
        SerialClient Contact3Client = new CraneMonitor.SerialClient();

        private bool ContactStart(SerialClient contact, string port, MessageEventHandler ReceiveHandler, string[] InitializeCommand)
        {
            contact.LogHandler = log.WriteLogWarning;
            contact.ErrorLogHandler = log.WriteLogError;
            contact.ReceiveHandler = ReceiveHandler;

            contact.Init(port);
            foreach(string command in InitializeCommand) contact.Send(command);
            return contact.Connected();
        }

        private bool ContactStop(SerialClient contact)
        {
            contact.Close();
            return !contact.Connected();
        }

        bool PrevStartStop = false;
#if false
        public void Contact1ReceiveHandler(object sender, string message)
        {
            message = message.Trim('\r');
            if (message.Length > 2 || message.Length == 0) return;
            uint contact = Convert.ToUInt32(message, 16);
            for (int i = 0; i < 8; i++) ObstacleHits[2 * i + 0] = ObstacleHits[2 * i + 1] = ((contact & (1 << i)) != 0) ? true : false;
        }
        public void Contact2ReceiveHandler(object sender, string message)
        {
            message = message.Trim('\r');
            if (message.Length > 2 || message.Length == 0) return;
            uint contact = Convert.ToUInt32(message, 16);
            for (int i = 0; i < 8; i++) ObstacleHits[2 * (i + 8) + 0] = ObstacleHits[2 * (i + 8) + 1] = ((contact & (1 << i)) != 0) ? true : false;
        }
        public void Contact3ReceiveHandler(object sender, string message)
        {
            message = message.Trim('\r');
            if (message.Length > 2 || message.Length == 0) return;
            uint contact = Convert.ToUInt32(message, 16);
            for (int i = 0; i < 2; i++) ObstacleHits[2 * (i + 16) + 0] = ObstacleHits[2 * (i + 16) + 1] = ((contact & (1 << i)) != 0) ? true : false;

            NumHits = 0;
            for (int i = 0; i < NumObstacles; i++) if (ObstacleHits[i]) NumHits++;

            bool StartStop = ((contact & (1 << 2)) != 0) ? true : false; 
            if(StartStop != PrevStartStop)
            {
                AutoStartHandler(StartStop);    // 1->0のときスタート、0->1のときストップとする
                PrevStartStop = StartStop;
            }
        }
#else
        public void Contact1ReceiveHandler(object sender, string message)
        {
            message = message.Trim('\r');
            if (message.Length > 2 || message.Length == 0) return;
            uint contact = Convert.ToUInt32(message, 16);
            for (int i = 0; i < 7; i++) ObstacleHits[2 * (i + 8) + 0] = ObstacleHits[2 * (i + 8) + 1] = ((contact & (1 << i)) != 0) ? true : false;
        }
        public void Contact2ReceiveHandler(object sender, string message)
        {
            message = message.Trim('\r');
            if (message.Length > 2 || message.Length == 0) return;
            uint contact = Convert.ToUInt32(message, 16);
            for (int i = 2; i < 8; i++) ObstacleHits[2 * (i + 0) + 0] = ObstacleHits[2 * (i + 0) + 1] = ((contact & (1 << i)) != 0) ? true : false;

            ObstacleHits[2 * (0 + 0) + 0] = ObstacleHits[2 * (0 + 0) + 1] = ((contact & (1 << 0)) != 0) ? true : false;

            NumHits = 0;
            for (int i = 0; i < NumObstacles; i++) if (ObstacleHits[i]) NumHits++;

        }
        public void Contact3ReceiveHandler(object sender, string message)
        {
            message = message.Trim('\r');
            if (message.Length > 2 || message.Length == 0) return;
            uint contact = Convert.ToUInt32(message, 16);
            for (int i = 0; i < 2; i++) ObstacleHits[2 * (i + 16) + 0] = ObstacleHits[2 * (i + 16) + 1] = ((contact & (1 << i)) != 0) ? true : false;


            bool StartStop = ((contact & (1 << 2)) != 0) ? true : false;
            if (StartStop != PrevStartStop)
            {
                AutoStartHandler(StartStop);    // 1->0のときスタート、0->1のときストップとする
                PrevStartStop = StartStop;
            }
        }
#endif
        public bool Contact1Start(string port) { return ContactStart(Contact1Client, port, Contact1ReceiveHandler, new string[] { "gpio iodir ff\r" }); }
        public bool Contact2Start(string port) { return ContactStart(Contact2Client, port, Contact2ReceiveHandler, new string[] { "gpio iodir fe\r", "gpio set 0\r" }); }
        public bool Contact3Start(string port) { return ContactStart(Contact3Client, port, Contact3ReceiveHandler, new string[] { "gpio iodir 07\r" }); }
        public bool Contact1Stop() { return ContactStop(Contact1Client); }
        public bool Contact2Stop() { return ContactStop(Contact2Client); }
        public bool Contact3Stop() { return ContactStop(Contact3Client); }

        // ------------------------------------------------------------
#if false
        System.Threading.Thread thread = null;
        bool threadActive = true;

        private void ThreadProc()
        {
            while (threadActive)
            {

                System.Threading.Thread.Sleep(500);
            }
        }

        private void ThreadBegin()
        {
            this.threadActive = true;
            this.thread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadProc));
            this.thread.Start();
        }

        private void ThreadEnd()
        {
            if (this.thread != null)
            {
                this.threadActive = false;
                this.thread.Join();
                this.thread = null;
            }
        }
#endif
        // ------------------------------------------------------------

        PlayRecord CurrentRecord = null;

        public bool GameRegister()
        {
            if(TimerEnabled) return false;

            CurrentRecord = new PlayRecord();
            RegisterUser RegisterWnd = new RegisterUser();
            RegisterWnd.Title = "Player Registration";
            RegisterWnd.PlayerName = CurrentRecord.Name;
            RegisterWnd.Message1.Content = "";
            RegisterWnd.Message2.Content = "";

            if (RegisterWnd.ShowDialog() == true)
            {
                CurrentRecord.Name = RegisterWnd.UserName.Text;
                return true;
            }
            else
            {
                CurrentRecord = null;
                return false;
            }
        }

        // ------------------------------------------------------------


        bool RequestUpdateRanking = true;
        bool RequestUpdateStart = false;
        bool RequestUpdateStop = false;

        bool TimerEnabled = false;
        bool PauseEnabled = false;

        DateTime StartTime;
        DateTime PauseStartTime;
        TimeSpan MeasuredTime;

        public bool GameStart()
        {
            if (CurrentRecord == null)
            {
                System.Windows.Forms.MessageBox.Show("Player name is not registered yet.", "ERROR");
                return false;
            }
            RequestUpdateStart = true;
            ClearHits();

            TimerEnabled = !TimerEnabled;
            StartTime = DateTime.Now;

            CurrentRecord.StartTime = DateTime.Now;
            return true;
        }
        public bool GameStop()
        {
            RequestUpdateStop = true;

            TimerEnabled = !TimerEnabled;
            MeasuredTime = DateTime.Now - StartTime;

            CurrentRecord.RecordTime = MeasuredTime;
            CurrentRecord.Hits = ObstacleHits;

            RegisterUser RegisterWnd = new RegisterUser();
            RegisterWnd.Title = "Result Registration";
            RegisterWnd.PlayerName = CurrentRecord.Name;
            //RegisterWnd.Message1.Content = string.Format("Your record is   {0} ({1} hits) => {2}", CurrentRecord.GetTimeAsText(), CurrentRecord.GetNumHit(), CurrentRecord.GetPenaltiedTimeAsText());
            RegisterWnd.Message1.Content = string.Format("あなたの記録は {0} ({1} hits) => {2}", CurrentRecord.GetTimeAsText(), CurrentRecord.GetNumHit(), CurrentRecord.GetPenaltiedTimeAsText());
            int order, total;
            Records.GetRanking(CurrentRecord, out order, out total);
            //RegisterWnd.Message2.Content = string.Format("Ranking :  {0} out of {1}.", order + 1, total + 1);
            RegisterWnd.Message2.Content = string.Format("あなたは {1} 人中 {0} 位です！", order + 1, total + 1);
            if (RegisterWnd.ShowDialog() == true)
            {
                CurrentRecord.Name = RegisterWnd.PlayerName;
                Records.SaveAndAdd(CurrentRecord);
                CurrentRecord = null;
                RequestUpdateRanking = true;
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool GameStartPause()
        {
            if (!TimerEnabled) return false;
            RequestUpdateStop = true;

            PauseEnabled = !PauseEnabled;
            PauseStartTime = DateTime.Now;
            MeasuredTime = DateTime.Now - StartTime;

            return true;
        }
        public bool GameStopPause()
        {
            RequestUpdateStart = true;

            PauseEnabled = !PauseEnabled;
            StartTime += (DateTime.Now - PauseStartTime);

            return true;
        }

        const int NumObstacles = 36;
        bool[] ObstacleHits = new bool[NumObstacles];
        bool Blink = false;

        int DivRatioObstacleBlink = 5;
        int TimeIndexObstacleBlink = 0;
        int NumHits = 0;
        int PrevNumHits = 0;

        private void ClearHits()
        {
            for (int i = 0; i < NumObstacles; i++) ObstacleHits[i] = false;
            NumHits = 0;
            PrevNumHits = 0;

            if (Contact2Client.Connected()) {
                // 接触センサの状態をクリアする
                Contact2Client.Send("gpio clear 0\r");
                System.Threading.Thread.Sleep(100);
                Contact2Client.Send("gpio set 0\r");
            }
        }

        public void UpdateDisplay() // called by main thread
        {
            if (DisplayClient.Connected())
            {
                if (TimerEnabled && !PauseEnabled)
                {
                    TimeSpan span = DateTime.Now - StartTime;
                    DisplayClient.Send(String.Format("set timer {0:00}:{1:00}:{2:00}", span.Minutes, span.Seconds, span.Milliseconds/10));
                }
                else
                {
                    DisplayClient.Send(String.Format("set timer {0:00}:{1:00}:{2:00}", MeasuredTime.Minutes, MeasuredTime.Seconds, MeasuredTime.Milliseconds/10));
                }

                if (TimeIndexObstacleBlink == DivRatioObstacleBlink)
                {
                    string command = "set course ";
                    for (int i = 0; i < NumObstacles; i++) command += (ObstacleHits[i] ? "2" : (Blink ? "0" : "1"));
                    Blink = !Blink;
                    DisplayClient.Send(command);
                    TimeIndexObstacleBlink = 0;

                    if (Contact1Client.Connected()) Contact1Client.Send("gpio readall\r");
                    if (Contact2Client.Connected()) Contact2Client.Send("gpio readall\r");
                    if (Contact3Client.Connected()) Contact3Client.Send("gpio readall\r");
                }
                TimeIndexObstacleBlink++;

                if(NumHits != PrevNumHits)
                {
                    DisplayClient.Send("play sound1");
                    PrevNumHits = NumHits;
                }
                if (RequestUpdateRanking)
                {
                    UpdateRanking();
                    RequestUpdateRanking = false;
                }
                if (RequestUpdateStart)
                {
                    DisplayClient.Send("play sound2");
                    RequestUpdateStart = false;
                }
                if (RequestUpdateStop)
                {
                    DisplayClient.Send("play sound3");
                    RequestUpdateStop = false;
                }

            }
        }

        // ------------------------------------------------------------

        private const int NumRankingTops = 10;
        private const int MaxNameLen = 16;

        void UpdateRanking()
        {
            int NumRanking = Math.Min(Records.Size, NumRankingTops);
            PlayRecord[] ranking = Records.GetTopRanking(NumRanking);
            for (int i = 0; i < NumRanking; i++)
            {
                string Name;
                int len = ranking[i].Name.Length;
                if (len < MaxNameLen)
                    Name = ranking[i].Name + new String('_', MaxNameLen - len);
                else
                    Name = ranking[i].Name.Substring(0, MaxNameLen);
                DisplayClient.Send(string.Format("set ranking {0} {1}.__{2}_{3}___{4}HITS", i, i + 1, Name, ranking[i].GetTimeAsText(), ranking[i].GetNumHit()));
            }
            for (int i = NumRanking; i < NumRankingTops; i++)
                DisplayClient.Send(string.Format("set ranking {0} {1}.", i, i + 1));
        }
    }
}
