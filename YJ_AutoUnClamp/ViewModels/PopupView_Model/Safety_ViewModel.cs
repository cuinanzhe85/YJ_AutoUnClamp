﻿using Common.Commands;
using Common.Mvvm;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YJ_AutoUnClamp.Models;
using YJ_AutoUnClamp.Utils;

namespace YJ_AutoUnClamp.ViewModels
{
    public class Safety_ViewModel : BindableAndDisposable
    {
        public ICommand Exit_ButtonCommand { get; private set; }
        public ICommand PreventCloseCommand { get; private set; }
        public ICommand DioSetCommand { get; private set; }

        private EziDio_Model _Dio { get; set; } = SingletonManager.instance.Ez_Dio;
        public EziDio_Model Dio
        {
            get { return _Dio; }
        }
        private bool _IsTopmost = true;
        public bool IsTopmost
        {
            get { return _IsTopmost; }
            set { SetValue(ref _IsTopmost, value); }
        }
        public Safety_ViewModel()
        {
            Global.Mlog.InfoFormat($"Safety Popup Open.");
            Global.instance.Set_TowerLaamp(Global.TowerLampType.Error);
            SingletonManager.instance.Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BUZZER, true);
        }
        private async void OnDioSetCommand(object obj)
        {
            string cmd = obj.ToString();
            // Buzzer Y07
            if (cmd == "Buzzer")
            {
                if (SingletonManager.instance.Ez_Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.BUZZER] == false)
                    SingletonManager.instance.Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BUZZER, true);
                else
                    SingletonManager.instance.Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BUZZER, false);
            }
            // Reset Y03
            else
            {
                SingletonManager.instance.Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.OP_BOX_RESET, true);
                await Task.Delay(3000); // 비동기 대기
                SingletonManager.instance.Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.OP_BOX_RESET, false);
            }
        }
        private void OnPreventClose(object obj) { }
        private void OnExit_Command(object obj)
        {
            Global.Mlog.Info("[USER] Safety 'Unlock' Button Click");

            if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.DOOR_FEEDBACK] == false
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.FRONT_OP_EMERGENCY_FEEDBACK] == false
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.REAR_OP_EMERGENCY_FEEDBACK] == false)
            {
                Global.instance.ShowMessagebox("SAFETY_PLC_POWER_OFF. Please Reset Swich Push.", false);
                return;
            }
            SingletonManager.instance.Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BUZZER, false);
            Global.instance.InspectionStop();
            // Exit
            Global.instance.SafetyErrorMessage = string.Empty;
            WindowManager.Instance.CloseCommand.Execute("Safety");
        }
        #region override
        /// <summary>
        /// Initialize Commands
        /// </summary>
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            Exit_ButtonCommand = new RelayCommand(OnExit_Command);
            PreventCloseCommand = new RelayCommand(OnPreventClose);
            DioSetCommand = new RelayCommand(OnDioSetCommand);
        }
        protected override void DisposeManaged()
        {
            Exit_ButtonCommand = null;
            DioSetCommand = null;
            PreventCloseCommand = null;

            base.DisposeManaged();
        }
        #endregion //override
    }
}
