using Common.Commands;
using Common.Managers;
using Common.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using static YJ_AutoUnClamp.Models.Serial_Model;

namespace YJ_AutoUnClamp.ViewModels
{
    public class TcpSerial_ViewModel : Child_ViewModel
    {
        #region // ICommands
        public ICommand Save_Command { get; private set; }
        public ICommand BcrPort_Command { get; private set; }
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
        private string[] _BarCodePort = new string[(int)SerialIndex.Max];
        public string[] BarCodePort
        {
            get { return _BarCodePort; }
            set { SetValue(ref _BarCodePort, value); }
        }
        private string _NfcData;
        public string NfcData
        {
            get { return _NfcData; }
            set { SetValue(ref _NfcData, value); }
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

            BarCodePort = new string[(int)SerialIndex.Max];
            for (int i = 0; i < (int)SerialIndex.Max; i++)
            {
                BarCodePort[i] = SingletonManager.instance.SerialModel[i].Port;
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
                        myIni.Write(key, BarCodePort[i], Section);
                        Global.Mlog.Info($" {key} = " + BarCodePort[i]);
                        SingletonManager.instance.SerialModel[i].PortName = key;
                        SingletonManager.instance.SerialModel[i].Port = BarCodePort[i];
                    }
                    else
                    {
                        key = $"BARCODE_PORT_{i + 1}";
                        myIni.Write(key, BarCodePort[i], Section);
                        Global.Mlog.Info($" {key} = " + BarCodePort[i]);
                        SingletonManager.instance.SerialModel[i].PortName = key;
                        SingletonManager.instance.SerialModel[i].Port = BarCodePort[i];
                    }
                }
            }
            catch(Exception e)
            {
                Global.ExceptionLog.Info(e.ToString());
                Global.instance.ShowMessagebox("Save Fail.");
            }
        }
        public async void OnBcrPort_Command(object obj)
        {
            switch (obj.ToString())
            {
                case "BcrPort1":
                    SingletonManager.instance.SerialModel[0].PortName = "BARCODE_PORT_1";
                    SingletonManager.instance.SerialModel[0].Open();
                    break;
                case "BcrPort2":
                    SingletonManager.instance.SerialModel[1].PortName = "BARCODE_PORT_2";
                    SingletonManager.instance.SerialModel[1].Open();
                    break;
                case "BcrPort3":
                    SingletonManager.instance.SerialModel[2].PortName = "BARCODE_PORT_3";
                    SingletonManager.instance.SerialModel[2].Open();
                    break;
                case "NfcPort":
                    SingletonManager.instance.SerialModel[3].PortName = "NFC_PORT";
                    SingletonManager.instance.SerialModel[3].Open();
                    break;
                case "BcrTest1":
                    await BcrData_Triger(0);
                    break;
                case "BcrTest2":
                    await BcrData_Triger(1);
                    break;
                case "BcrTest3":
                    await BcrData_Triger(2);
                    break;
                case "NfcTest":
                    await NFC_DataRead();
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
                    if (SingletonManager.instance.SerialModel[index].IsBcrReceived == true)
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
        private async Task NFC_DataRead()
        {
            if (SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].IsConnected != true)
                return;
            NfcData = "";
            await Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                sw.Restart();
                while (true)
                {
                    if (SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].IsBcrReceived == true)
                    {
                        NfcData = SingletonManager.instance.SerialModel[(int)SerialIndex.Nfc].NfcData;
                        Global.Mlog.Info($"NFC Read : {NfcData}");
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 10000)
                    {
                        MessageBox.Show("NFC read fail.", "NFC");
                        break;
                    }
                }
            });
        }
        #region override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();
            Save_Command = new RelayCommand(OnSave_Command);
            BcrPort_Command = new RelayCommand(OnBcrPort_Command);
            TCP_Command = new RelayCommand(OnTCP_Command);
        }
        protected override void DisposeManaged()
        {
            Save_Command = null;
            BcrPort_Command = null;
            base.DisposeManaged();
        }
        #endregion
    }
}
