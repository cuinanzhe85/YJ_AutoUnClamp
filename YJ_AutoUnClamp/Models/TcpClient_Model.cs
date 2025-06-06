using Common.Mvvm;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace YJ_AutoUnClamp.Models
{
    public class TcpClient_Model : BindableAndDisposable
    {
        private Thread rThread = null;
        public Thread rReconnectThread = null;
        public bool IsReconnectThreadClose = false;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        Ping ping = new Ping();
        PingReply pingReply;
        private byte STX = 0x02;
        private byte ETX = 0x03;

        private bool _ReconnectThreadRun = false;
        private string IpAddress = "192.168.10.20";
        private int Port = 8000;
        private string _TcpReceiveData = string.Empty;
        public string TcpReceiveData
        {
            get { return _TcpReceiveData; }
            set { SetValue(ref _TcpReceiveData, value); }
        }

        // 서버에 연결
        public bool Connect(string host, int port)
        {
            try
            {
                IpAddress = host;
                Port = port;
                _client = new TcpClient();
                // 연결 시도 (await로 연결 완료까지 대기)
                _client.Connect(host, port);

                // 연결 성공하면 스트림 가져오기
                _stream = _client.GetStream();
                Global.Mlog.Info("서버에 연결되었습니다.");

                _cts = new CancellationTokenSource();

                // 백그라운드에서 수신 처리 시작
               // Task.Run(() => ReceiveLoopAsync(_cts.Token));

                this.rThread = new Thread(new ThreadStart(Receive));
                this.rThread.Start(); //메시지 읽어오는 스레드 시작

                _ReconnectThreadRun = false;
                return true; // 연결 성공
            }
            catch (Exception ex)
            {
                Global.Mlog.Info($"서버 연결 실패: {ex.Message}");
                if (_ReconnectThreadRun == false)
                {
                    this.rReconnectThread = new Thread(new ThreadStart(TcpReconnect));
                    rReconnectThread.Start();
                }
                return false; // 연결 실패
            }
        }

        // 메시지 보내기
        public void TcpSendMessage(string message)
        {
            if (_client?.Connected == true)
            {
                TcpReceiveData = string.Empty;
                byte[] data = Encoding.UTF8.GetBytes(message);
                byte[] sendByte = new byte[data.Length + 2];
                Buffer.BlockCopy(data, 0, sendByte, 1, data.Length);
                sendByte[0] = STX;
                sendByte[sendByte.Length-1] = ETX;
                _stream.Write(sendByte, 0, sendByte.Length);
            }
            else
            {
                Global.Mlog.Info("서버에 연결되어 있지 않습니다.");
            }
        }

        // 메시지 수신 루프 (백그라운드 Task)
        private void Receive()
        {
            // Reading을 위한 스레드
            byte[] data = new byte[1024];

            while (_client.Connected == true)
            {
                try
                {
                    StringBuilder myCompleteMessage = new StringBuilder();
                    if (_stream.CanRead)
                    {
                        byte[] myReadBuffer = new byte[1024];
                        int numberOfBytesRead = 0;

                        // Incoming message may be larger than the buffer size.
                        do
                        {
                            numberOfBytesRead = _stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                            myCompleteMessage.AppendFormat("{0}", Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead));
                        }
                        while (_stream.DataAvailable);
                        if (numberOfBytesRead == 0)
                        {
                            SingletonManager.instance.IsTcpConnected = false;
                            break;
                        }

                        string cmdSTR = myCompleteMessage.ToString();
                        if (cmdSTR.Length > 10)
                        {
                            SingletonManager.instance.IsTcpConnected = true;
                            TcpReceiveData = cmdSTR.Substring(1, cmdSTR.Length - 2);
                        }
                    }

                    if (_client.Connected == false)
                    {
                        SingletonManager.instance.IsTcpConnected = false;
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }
            Disconnect();

            this.rReconnectThread = new Thread(new ThreadStart(TcpReconnect));
            rReconnectThread.Start();
        }
        public void TcpReconnect()
        {
            _ReconnectThreadRun = true;
            try
            {
                pingReply = ping.Send(IPAddress.Parse(IpAddress));
                while (SingletonManager.instance.IsTcpConnected == false)
                {
                    if (pingReply.Status == IPStatus.Success)
                    {
                        if (Connect(IpAddress, Port)==false)
                            pingReply = ping.Send(IPAddress.Parse(IpAddress));
                    }
                    else
                    {
                        pingReply = ping.Send(IPAddress.Parse(IpAddress));
                        SingletonManager.instance.IsTcpConnected = false;
                    }
                    if (IsReconnectThreadClose == true)
                        break;
                    Thread.Sleep(2000);
                }
            }
            catch { }
        }
        // 연결 종료
        public void Disconnect()
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
        }
    }
}
