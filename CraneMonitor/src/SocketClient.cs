using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraneMonitor
{
    public delegate void MessageEventHandler(object sender, string message);

    class SocketClient
    {
        public MessageEventHandler LogHandler;
        public MessageEventHandler ErrorLogHandler;
        public MessageEventHandler ReceiveHandler;

        string ipOrHost = "localhost";
        int port = 8080;
        System.Net.Sockets.TcpClient tcp = null;
        System.Net.Sockets.NetworkStream ns = null;
        System.Threading.Thread thread = null;
        bool threadActive = true;

        public SocketClient()
        {
        }

        public void Init(string ipOrHost, int port)
        {
            this.ipOrHost = ipOrHost;
            this.port = port;

            try
            {
                this.tcp = new System.Net.Sockets.TcpClient(ipOrHost, port);
            }
            catch(Exception e)
            {
                ErrorLogHandler(this, e.ToString());
                return;
            }
            LogHandler(this, String.Format("Connected to the server {0}:{1}. The client is {2}:{3}.",
                ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Address,
                ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Port,
                ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Address,
                ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Port));

            this.ns = tcp.GetStream();

            // set timeout (default is infinite)
            // this.ns.ReadTimeout = 10000;
            this.ns.WriteTimeout = 10000;

            if (ReceiveHandler == null) return;
            this.threadActive = true;
            this.thread = new System.Threading.Thread(new System.Threading.ThreadStart(ReceiveProc));
            this.thread.Start();
        }

        private void ReceiveProc()
        {

            while (threadActive)
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes = new byte[256];
                int resSize = 0;

                do
                {
                    try
                    {
                        // データの一部を受信する
                        resSize = ns.Read(resBytes, 0, resBytes.Length);
                        // Readが0を返した時はサーバーが切断したと判断
                        if (resSize == 0)
                            throw new Exception("Server connection was closed.");
                    }
                    catch (Exception e)
                    {
                        LogHandler(this, e.ToString());
                        threadActive = false;
                        break;
                    }
                    // 受信したデータを蓄積する
                    ms.Write(resBytes, 0, resSize);
                } while (ns.DataAvailable || resBytes[resSize - 1] != '\n') ;

                // 受信したデータを文字列に変換
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.Close();
                // 末尾の\nを削除
                resMsg = resMsg.TrimEnd('\n');
                string[] resMsgList = resMsg.Split('\n');
                foreach(var msg in resMsgList) {
                    if (msg.Length != 0) ReceiveHandler(this, msg);
                }
            }
        }

        public void Send(string message)
        {
            if (this.ns == null) return;

            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            byte[] sendBytes = enc.GetBytes(message);
            try
            {
                this.ns.Write(sendBytes, 0, sendBytes.Length);
            }
            catch(Exception e)
            {
                ErrorLogHandler(this, e.ToString());
                Close();
            }
        }

        public bool Connected() { return this.ns != null; }

        public void Close()
        {
            if(this.ns != null)
            {
                this.ns.Close();
                this.ns = null;
            }
            if(this.tcp != null)
            {
                this.tcp.Close();
                this.tcp = null;
            }

            if(this.thread != null)
            {
                this.threadActive = false;
                this.thread.Join();
                this.thread = null;
            }
        }
    }

    class SerialClient
    {
        public MessageEventHandler LogHandler;
        public MessageEventHandler ErrorLogHandler;
        public MessageEventHandler ReceiveHandler;

        public String PortName { get; set; }
        public int BaudRate { get; set; }
        public System.IO.Ports.Parity Parity { get; set; }
        public int DataBits { get; set; }
        public System.IO.Ports.StopBits StopBits { get; set; }

        private System.IO.Ports.SerialPort port = null;
        System.Threading.Thread thread = null;
        bool threadActive = true;

        public SerialClient()
        {
            // default settings
            BaudRate = 9600;
            Parity = System.IO.Ports.Parity.None;
            DataBits = 8;
            StopBits = System.IO.Ports.StopBits.One;
        }

        public void Init(string PortName/*, int BaudRate, System.IO.Ports.Parity Parity, int DataBits, System.IO.Ports.StopBits StopBits*/)
        {
            this.PortName = PortName;
            //this.BaudRate = BaudRate;
            //this.Parity = Parity;
            //this.DataBits = DataBits;
            //this.StopBits = StopBits;

            try
            {
                this.port = new System.IO.Ports.SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
                this.port.Open();

            }
            catch (Exception e)
            {
                ErrorLogHandler(this, e.ToString());
                return;
            }
            LogHandler(this, String.Format("Connected to the {0}. ", PortName));

            if (ReceiveHandler == null) return;
            this.threadActive = true;
            this.thread = new System.Threading.Thread(new System.Threading.ThreadStart(ReceiveProc));
            this.thread.Start();
        }

        private void ReceiveProc()
        {
#if true    // trueは正確な処理だけど'\n'で区切られた文字列がバッファサイズ以内でないとエラーになる
            byte[] resBytes = new byte[1024];
            int resSize = 0;

            while (threadActive)
            {
                try
                {
                    if (resSize == resBytes.Length)
                        throw new Exception("Buffer is full. The message between delimitters is too long.");
                    // データの一部を受信する
                    int read = port.Read(resBytes, resSize, resBytes.Length - resSize);
                    // Readが0を返した時はサーバーが切断したと判断
                    if (read == 0)
                        throw new Exception("Server connection was closed.");
                    resSize += read;
                }
                catch (Exception e)
                {
                    LogHandler(this, e.ToString());
                    threadActive = false;
                    break;
                }
                for (int i = resSize - 1; i >= 0; i--)
                {
                    if (resBytes[i] == '\n')
                    {
                        // 受信したデータを文字列に変換
                        System.Text.Encoding enc = System.Text.Encoding.UTF8;
                        string resMsg = enc.GetString(resBytes, 0, i + 1);
                        // 末尾の\nを削除
                        resMsg = resMsg.TrimEnd('\n');
                        string[] resMsgList = resMsg.Split('\n');
                        foreach (var msg in resMsgList)
                        {
                            if (msg.Length != 0) ReceiveHandler(this, msg);
                        }
                        for (int j = i + 1, k = 0; j < resSize; j++, k++) resBytes[k] = resBytes[j];
                        resSize -= (i + 1);
                        break;
                    }
                }
            }
#else
            while (threadActive)
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes = new byte[256];
                int resSize = 0;

                do
                {
                    try
                    {
                        // データの一部を受信する
                        resSize = port.Read(resBytes, 0, resBytes.Length);
                        // Readが0を返した時はサーバーが切断したと判断
                        if (resSize == 0)
                            throw new Exception("Server connection was closed.");
                    }
                    catch (Exception e)
                    {
                        LogHandler(this, e.ToString());
                        threadActive = false;
                        break;
                    }
                    // 受信したデータを蓄積する
                    ms.Write(resBytes, 0, resSize);
                } while (resBytes[resSize - 1] != '\n');

                // 受信したデータを文字列に変換
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.Close();
                // 末尾の\nを削除
                resMsg = resMsg.TrimEnd('\n');
                string[] resMsgList = resMsg.Split('\n');
                foreach (var msg in resMsgList)
                {
                    if (msg.Length != 0) ReceiveHandler(this, msg);
                }
            }
#endif
        }

        public void Send(string message)
        {
            if (this.port == null) return;

            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            byte[] sendBytes = enc.GetBytes(message);
            try
            {
                this.port.Write(sendBytes, 0, sendBytes.Length);
            }
            catch (Exception e)
            {
                ErrorLogHandler(this, e.ToString());
                Close();
            }
        }

        public bool Connected() { return this.port != null; }

        public void Close()
        {
            if (this.port != null)
            {
                this.port.Close();
                this.port = null;
            }

            if (this.thread != null)
            {
                this.threadActive = false;
                this.thread.Join();
                this.thread = null;
            }
        }
    }

    class UdpClient
    {
        public MessageEventHandler LogHandler;
        public MessageEventHandler ErrorLogHandler;
        public MessageEventHandler ReceiveHandler;

        string ipOrHost = "localhost";
        int port = 8080;
        System.Net.Sockets.UdpClient udp = null;
        System.Threading.Thread thread = null;
        bool threadActive = true;

        public UdpClient()
        {
        }

        public void Init(string ipOrHost, int port)
        {
            this.ipOrHost = ipOrHost;
            this.port = port;

            try
            {
                System.Net.IPAddress localAddress = System.Net.IPAddress.Parse(ipOrHost);

                //UdpClientを作成し、ローカルエンドポイントにバインドする
                System.Net.IPEndPoint localEP = new System.Net.IPEndPoint(localAddress, port);
                this.udp = new System.Net.Sockets.UdpClient(localEP);
            }
            catch (Exception e)
            {
                ErrorLogHandler(this, e.ToString());
                return;
            }
            LogHandler(this, String.Format("Connected to the {0}:{1}. ", ipOrHost, port));

            if (ReceiveHandler == null) return;
            this.threadActive = true;
            this.thread = new System.Threading.Thread(new System.Threading.ThreadStart(ReceiveProc));
            this.thread.Start();
        }

        private void ReceiveProc()
        {
            while (threadActive)
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes;
                int resSize = 0;

                do
                {
                    try
                    {
                        System.Net.IPEndPoint remoteEP = null;
                        resBytes = udp.Receive(ref remoteEP);
                        resSize = resBytes.Length;
                    }
                    catch (Exception e)
                    {
                        LogHandler(this, e.ToString());
                        threadActive = false;
                        break;
                    }
                    // 受信したデータを蓄積する
                    ms.Write(resBytes, 0, resSize);
                } while (resBytes[resSize - 1] != '\n');

                // 受信したデータを文字列に変換
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.Close();
                // 末尾の\nを削除
                resMsg = resMsg.TrimEnd('\n');
                string[] resMsgList = resMsg.Split('\n');
                foreach (var msg in resMsgList)
                {
                    if (msg.Length != 0) ReceiveHandler(this, msg);
                }
            }
        }

        public void Send(string message)
        {
            if (this.udp == null) return;

            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            byte[] sendBytes = enc.GetBytes(message);
            try
            {
                this.udp.Send(sendBytes, sendBytes.Length, this.ipOrHost, this.port);
            }
            catch (Exception e)
            {
                ErrorLogHandler(this, e.ToString());
                Close();
            }
        }

        public bool Connected() { return this.udp != null; }

        public void Close()
        {
            if (this.udp != null)
            {
                this.udp.Close();
                this.udp = null;
            }

            if (this.thread != null)
            {
                this.threadActive = false;
                this.thread.Join();
                this.thread = null;
            }
        }
    }
}
