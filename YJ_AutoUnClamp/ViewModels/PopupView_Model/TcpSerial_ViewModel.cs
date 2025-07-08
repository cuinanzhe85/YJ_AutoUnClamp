using Common.Commands;
using Common.Managers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static YJ_AutoUnClamp.Models.Serial_Model;

namespace YJ_AutoUnClamp.ViewModels
{
    public class TcpSerial_ViewModel : Child_ViewModel
    {
        #region // ICommands
        public ICommand Save_Command { get; private set; }
        public ICommand ComPort_Command { get; private set; }
        public ICommand TCP_Command { get; private set; }
        #endregion
        private ObservableCollection<string> _PortNames = new ObservableCollection<string>();
        public ObservableCollection<string> PortNames
        {
            get { return _PortNames; }
            set { SetValue(ref _PortNames, value); }
        }
        private ObservableCollection<string> _bcrData = new ObservableCollection<string>();
        public ObservableCollection<string> bcrData
        {
            get { return _bcrData; }
            set { SetValue(ref _bcrData, value); }
        }
        private string[] _SetComPort = new string[(int)SerialIndex.Max];
        public string[] SetComPort
        {
            get { return _SetComPort; }
            set { SetValue(ref _SetComPort, value); }
        }
        private string _NfcData;
        public string NfcData
        {
            get { return _NfcData; }
            set { SetValue(ref _NfcData, value); }
        }
        private string _MesData;
        public string MesData
        {
            get { return _MesData; }
            set { SetValue(ref _MesData, value); }
        }
        private string _TcpSendData;
        public string TcpSendData
        {
            get { return _TcpSendData; }
            set { SetValue(ref _TcpSendData, value); }
        }
        private string _TcpReceiveData;
        public string TcpReceiveData
        {
            get { return _TcpReceiveData; }
            set { SetValue(ref _TcpReceiveData, value); }
        }

        public TcpSerial_ViewModel()
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            foreach (string port in ports)
                PortNames.Add(port);

            SetComPort = new string[(int)SerialIndex.Max];
            for (int i = 0; i < (int)SerialIndex.Max; i++)
            {
                SetComPort[i] = SingletonManager.instance.SerialModel[i].Port;
                bcrData.Add("Empty");
            }
                
        }
        private void OnSave_Command(object obj)
        {
            try
            {
                if (MessageBox.Show("Do you want to save the modification data?.", "Serial Setting Data", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
                {
                    return;
                }
                Global.Mlog.Info($"[USER] SerialPort 'Save' Button Click");
                var myIni = new IniFile(Global.instance.IniSystemPath);
                // Gocator Front,Rear Ip Set
                string Section = "SERIAL";
                string key = "";
                for (int i = 0; i < (int)SerialIndex.Max; i++)
                {
                    if (i == (int)SerialIndex.Nfc)
                    {
                        key = $"NFC_PORT";
                        myIni.Write(key, SetComPort[i], Section);
                        Global.Mlog.Info($" {key} = " + SetComPort[i]);
                        SingletonManager.instance.SerialModel[i].PortName = key;
                        SingletonManager.instance.SerialModel[i].Port = SetComPort[i];
                    }
                    else if (i == (int)SerialIndex.Mes)
                    {
                        key = $"MES_PORT";
                        myIni.Write(key, SetComPort[i], Section);
                        Global.Mlog.Info($" {key} = " + SetComPort[i]);
                        SingletonManager.instance.SerialModel[i].PortName = key;
                        SingletonManager.instance.SerialModel[i].Port = SetComPort[i];
                    }
                    //else
                    //{
                    //    key = $"BARCODE_PORT_{i + 1}";
                    //    myIni.Write(key, SetComPort[i], Section);
                    //    Global.Mlog.Info($" {key} = " + SetComPort[i]);
                    //    SingletonManager.instance.SerialModel[i].PortName = key;
                    //    SingletonManager.instance.SerialModel[i].Port = SetComPort[i];
                    //}
                }
            }
            catch(Exception e)
            {
                Global.ExceptionLog.Info(e.ToString());
                Global.instance.ShowMessagebox("Save Fail.");
            }
        }
        public async void OnComPort_Command(object obj)
        {
            switch (obj.ToString())
            {
                //case "BcrPort1":
                //    SingletonManager.instance.SerialModel[(int)SerialIndex.bcr1].PortName = "BARCODE_PORT_1";
                //    if (SingletonManager.instance.SerialModel[(int)SerialIndex.bcr1].Open() == true)
                //        MessageBox.Show("BCR 1 Port Open Success.");
                //    else
                //        MessageBox.Show("BCR 1 Port Open Fail.");
                //    break;
                //case "BcrPort2":
                //    SingletonManager.instance.SerialModel[(int)SerialIndex.bcr2].PortName = "BARCODE_PORT_2";
                //    if (SingletonManager.instance.SerialModel[(int)SerialIndex.bcr2].Open() == true)
                //        MessageBox.Show("BCR 2 Port Open Success.");
                //    else
                //        MessageBox.Show("BCR 2 Port Open Fail.");
                //    break;
                //case "BcrPort3":
                //    SingletonManager.instance.SerialModel[(int)SerialIndex.bcr3].PortName = "BARCODE_PORT_3";
                //    if (SingletonManager.instance.SerialModel[(int)SerialIndex.bcr3].Open() == true)
                //        MessageBox.Show("BCR 3 Port Open Success.");
                //    else
                //        MessageBox.Show("BCR 3 Port Open Fail.");
                //    break;
                case "NfcPort":
                    SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].PortName = "NFC_PORT";
                    if (SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].Open() == true)
                        MessageBox.Show("NFC Port Open Success.");
                    else
                        MessageBox.Show("NFC Port Open Fail.");
                    break;
                case "MesPort":
                    SingletonManager.instance.SerialModel[(int)SerialIndex.Mes].PortName = "MES_PORT";
                    if (SingletonManager.instance.SerialModel[(int)SerialIndex.Mes].Open()==true)
                        MessageBox.Show("MES Port Open Success.");
                    else
                        MessageBox.Show("MES Port Open Fail.");
                    break;
                //case "BcrTest1":
                //    await BcrData_Triger((int)SerialIndex.bcr1);
                //    break;
                //case "BcrTest2":
                //    await BcrData_Triger((int)SerialIndex.bcr2);
                //    break;
                //case "BcrTest3":
                //    await BcrData_Triger((int)SerialIndex.bcr3);
                //    break;
                case "NfcTest":
                    await NFC_DataRead();
                    break;
                case "MesTest":
                    await MesTest();
                    break;
            }
        }
        private async void OnTCP_Command(object obj)
        {
            switch (obj.ToString())
            {
                case "SEND":
                    TcpReceiveData = "Empty";
                    await Tcp_DataSend();
                    break;
                case "CONNECT":
                    Tcp_Connect();
                    break;
            }
        }
        private void Tcp_Connect()
        {
            if (SingletonManager.instance.IsTcpConnected == true)
                SingletonManager.instance.TcpClient.Disconnect();

            if (SingletonManager.instance.TcpClient.Connect() == true)
            {
                Global.Mlog.Info("TCP Connect Success.");
                MessageBox.Show("TCP Connect Success.", "TCP", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Global.Mlog.Info("TCP Connect Fail.");
                MessageBox.Show("TCP Connect Fail.", "TCP", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task Tcp_DataSend()
        {
            if (SingletonManager.instance.IsTcpConnected == true)
            {
                SingletonManager.instance.TcpClient.TcpSendMessage(TcpSendData);

                TcpReceiveData = "";
                await Task.Run(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Restart();
                    while (true)
                    {
                        if (SingletonManager.instance.TcpClient.TcpReceiveData != "")
                        {
                            TcpReceiveData = SingletonManager.instance.TcpClient.TcpReceiveData;
                            break;
                        }
                        if (sw.ElapsedMilliseconds > 1500)
                        {
                            MessageBox.Show("TCP Data Send fail.", "TCP");
                            break;
                        }
                    }
                });
            }
            else
            {
                Global.instance.ShowMessagebox("TCP is Disconnected.");
            }
            
        }
        private async Task BcrData_Triger(int index)
        {
            if (SingletonManager.instance.SerialModel[index].IsConnected != true)
                return;
            bcrData[index] = "";
            SingletonManager.instance.SerialModel[index].SendBcrTrig();
            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                while (true)
                {
                    if (SingletonManager.instance.SerialModel[index].IsReceived == true)
                    {
                        bcrData[index] = SingletonManager.instance.SerialModel[index].Barcode;
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 1500)
                    {
                        MessageBox.Show("BCR Barcode read fail.", "BCR");
                        break;
                    }
                }
            });
        }
        private async Task MesTest()
        {
            if (SingletonManager.instance.SerialModel[(int)SerialIndex.Mes].IsConnected != true)
                return;
            SingletonManager.instance.SerialModel[(int)SerialIndex.Mes].SendMes(MesData);
            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                while (true)
                {
                    if (SingletonManager.instance.SerialModel[(int)SerialIndex.Mes].IsReceived == true)
                    {
                        MessageBox.Show($"MES Receive : {SingletonManager.instance.SerialModel[(int)SerialIndex.Mes].MesResult}", "MES");
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 1500)
                    {
                        MessageBox.Show("MES Receive Timeout.", "MES");
                        break;
                    }
                }
            });
        }
        private async Task NFC_DataRead()
        {
            if (SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].IsConnected != true)
                return;
            NfcData = "";
            SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].IsReceived = false;
            SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].NewNfcData = string.Empty; 
            await Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                sw.Restart();
                while (true)
                {
                    if (!string.IsNullOrEmpty(SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].NewNfcData))
                    {
                        NfcData = SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].NewNfcData;
                        Global.Mlog.Info($"NFC Read : {NfcData}");
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 5000)
                    {
                        MessageBox.Show("NFC read fail.", "NFC");
                        break;
                    }
                    await Task.Delay(10); // Wait for 100ms before checking again
                }
            });
        }
        #region override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();
            Save_Command = new RelayCommand(OnSave_Command);
            ComPort_Command = new RelayCommand(OnComPort_Command);
            TCP_Command = new RelayCommand(OnTCP_Command);
        }
        protected override void DisposeManaged()
        {
            Save_Command = null;
            ComPort_Command = null;
            TCP_Command = null;
            base.DisposeManaged();
        }
        #endregion
    }
}
