using System;
using System.Collections;

namespace CraneMonitor
{
    [Serializable]
    public class PlayRecord : IComparable
    {
        public PlayRecord() { SetInitValue(); }

        private TimeSpan record_time;

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
            RecordTime = TimeSpan.Zero;
            StartTime = DateTime.Now;
            Name = "Anonymous";
            //Gender = true;
        }

        private TimeSpan GetPenaltiedTime()
        {
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
            return string.Format("{0:00}:{1:00}.{2:00}", RecordTime.Minutes, RecordTime.Seconds, RecordTime.Milliseconds/10);
        }

        public string GetPenaltiedTimeAsText()
        {
            TimeSpan time = GetPenaltiedTime();
            return string.Format("{0:00}:{1:00}.{2:00}", time.Minutes, time.Seconds, time.Milliseconds/10);
        }

        public string GetStartTimeAsText()
        {
            return string.Format("{0}/{1}, {2:00}:{3:00}", StartTime.Month, StartTime.Day, StartTime.Hour, StartTime.Minute);
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

    public class RankingControl
    {
        public MainWindow Form;

        public LogWindow log = null;
        private PlayRecordSet Records = new PlayRecordSet();

        public RankingControl()
        {
        }

        public void SetLog(LogWindow obj) { log = obj; }

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

        public bool RequestUpdateRanking = true;
        public bool RequestUpdateStart = false;
        public bool RequestUpdateStop = false;

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

            RegisterUser RegisterWnd = new RegisterUser();
            RegisterWnd.Title = "Result Registration";
            RegisterWnd.PlayerName = CurrentRecord.Name;
            //RegisterWnd.Message1.Content = string.Format("Your record is   {0} ({1} hits) => {2}", CurrentRecord.GetTimeAsText(), CurrentRecord.GetNumHit(), CurrentRecord.GetPenaltiedTimeAsText());
            RegisterWnd.Message1.Content = string.Format("あなたの記録は {0}", CurrentRecord.GetTimeAsText());
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

        public String GetElapseTimeText()
        {
            if (TimerEnabled && !PauseEnabled)
            {
                TimeSpan span = DateTime.Now - StartTime;
                return String.Format("{0:00}:{1:00}.{2:00}", span.Minutes, span.Seconds, span.Milliseconds / 10);
            }
            else
            {
                return String.Format("{0:00}:{1:00}.{2:00}", MeasuredTime.Minutes, MeasuredTime.Seconds, MeasuredTime.Milliseconds / 10);
            }

        }

        // ------------------------------------------------------------

        private const int NumRankingTops = 10;
        private const int MaxNameLen = 16;

        public String GetRankingText()
        {
            string text = "";
            int NumRanking = Math.Min(Records.Size, NumRankingTops);
            PlayRecord[] ranking = Records.GetTopRanking(NumRanking);
            for (int i = 0; i < NumRanking; i++)
            {
                string Name;
                int len = ranking[i].Name.Length;
                if (len < MaxNameLen)
                    Name = ranking[i].Name + new String(' ', MaxNameLen - len);
                else
                    Name = ranking[i].Name.Substring(0, MaxNameLen);
                text += String.Format("{0}  {1} {2}  ({3})\r\n", i + 1, Name, ranking[i].GetTimeAsText(), ranking[i].GetStartTimeAsText());
            }
            return text;
        }
    }
}
