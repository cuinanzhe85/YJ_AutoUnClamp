using Common.Commands;
using Common.Managers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace YJ_AutoUnClamp.ViewModels
{
    public class SystemData_ViewModel : Child_ViewModel
    {
        #region // ICommands
        public ICommand Save_Command { get; private set; }
        public ICommand Bottom_Command { get; private set; }
        #endregion

        private ObservableCollection<string> _UseNotUse = new ObservableCollection<string>();
        public ObservableCollection<string> UseNotUse
        {
            get { return _UseNotUse; }
            set { SetValue(ref _UseNotUse, value); }
        }
        private string _BcrUseNotuse;
        public string BcrUseNotuse
        {
            get { return _BcrUseNotuse; }
            set { SetValue(ref _BcrUseNotuse, value); }
        }
        private string _TopCode;
        public string TopCode
        {
            get { return _TopCode; }
            set { SetValue(ref _TopCode, value); }
        }
        private string _NfcDelay;
        public string NfcDelay
        {
            get { return _NfcDelay; }
            set { SetValue(ref _NfcDelay, value); }
        }
        private string _NfcUseNotuse;
        public string NfcUseNotuse
        {
            get { return _NfcUseNotuse; }
            set { SetValue(ref _NfcUseNotuse, value); }
        }
        private string _HttpSendData;
        public string HttpSendData
        {
            get { return _HttpSendData; }
            set { SetValue(ref _HttpSendData, value); }
        }
        private string _AgingTime;
        public string AgingTime
        {
            get { return _AgingTime; }
            set { SetValue(ref _AgingTime, value); }
        }
        private string _AgingBarcodFilePath;
        public string AgingBarcodFilePath
        {
            get { return _AgingBarcodFilePath; }
            set { SetValue(ref _AgingBarcodFilePath, value); }
        }
        public SystemData_ViewModel()
        {
            UseNotUse.Add("Not Use");
            UseNotUse.Add("Use");

            BcrUseNotuse = SingletonManager.instance.SystemModel.BcrUseNotUse;
            NfcUseNotuse = SingletonManager.instance.SystemModel.NfcUseNotUse;
            AgingTime = SingletonManager.instance.SystemModel.AgingTime;
            TopCode = SingletonManager.instance.SystemModel.TopCode;
            AgingBarcodFilePath = SingletonManager.instance.SystemModel.AgingBarcodFilePath;
            NfcDelay = SingletonManager.instance.SystemModel.NfcDelay;
        }
        private void OnSave_Command(object obj)
        {
            try
            {
                if (MessageBox.Show("Do you want to save the modification data?.", "System Manager", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
                {
                    return;
                }
                Global.Mlog.Info($"[USER] System Data 'Save' Button Click");
                var myIni = new IniFile(Global.instance.IniSystemPath);
              
                string Section = "SYSTEM";
                
                //myIni.Write("BARCODE_USE", BcrUseNotuse, Section);
                //Global.Mlog.Info(" BARCODE_USE = " + BcrUseNotuse);

                //SingletonManager.instance.SystemModel.BcrUseNotUse = BcrUseNotuse;

                myIni.Write("NFC_USE", NfcUseNotuse, Section);
                Global.Mlog.Info(" NFC_USE = " + NfcUseNotuse);

                SingletonManager.instance.SystemModel.NfcUseNotUse = NfcUseNotuse;
                SingletonManager.instance.Channel_Model[0].MesResult = NfcUseNotuse;

                myIni.Write("AGINT_TIME", AgingTime, Section);
                Global.Mlog.Info(" AGINT_TIME = " + AgingTime);

                SingletonManager.instance.SystemModel.AgingTime = AgingTime;

                myIni.Write("NFC_DELAY_TIME", NfcDelay, Section);
                Global.Mlog.Info(" NFC_DELAY_TIME = " + NfcDelay);

                SingletonManager.instance.SystemModel.NfcDelay = NfcDelay;

                //myIni.Write("AGINT_BARCODE_FILE_PATH", AgingBarcodFilePath, Section);
                //Global.Mlog.Info(" AGINT_BARCODE_FILE_PATH = " + AgingTime);

                //SingletonManager.instance.SystemModel.AgingBarcodFilePath = AgingBarcodFilePath;

                //myIni.Write("TOP_CODE", TopCode, Section);
                //Global.Mlog.Info(" TOP_CODE = " + TopCode);

                //SingletonManager.instance.SystemModel.TopCode = TopCode;
            }
            catch (Exception e)
            {
                Global.ExceptionLog.Info(e.ToString());
                Global.instance.ShowMessagebox("Save Fail.");
            }
        }
        private void OnBottom_Command(object obj)
        {
        }

        #region override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();
            Save_Command = new RelayCommand(OnSave_Command);
            Bottom_Command = new RelayCommand(OnBottom_Command);
        }
        protected override void DisposeManaged()
        {
            Save_Command = null;
            Bottom_Command = null;
            base.DisposeManaged();
        }
        #endregion
    }
}
