using Common.Mvvm;
using System;
using System.IO.Ports;

namespace YJ_AutoUnClamp.Models
{
    public class SystemData_Model : BindableAndDisposable
    {
        public string _BcrUseNotUse;
        public string BcrUseNotUse
        {
            get { return _BcrUseNotUse; }
            set { SetValue(ref _BcrUseNotUse, value); }
        }
        public string _NfcUseNotUse;
        public string NfcUseNotUse
        {
            get { return _NfcUseNotUse; }
            set { SetValue(ref _NfcUseNotUse, value); }
        }
        public string _TopCode;
        public string TopCode
        {
            get { return _TopCode; }
            set { SetValue(ref _TopCode, value); }
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
        public SystemData_Model()
        {
            
        }
       
    }
}
