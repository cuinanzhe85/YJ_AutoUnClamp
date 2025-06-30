using Common.Commands;
using System.Timers;
using System.Windows.Input;
using Telerik.Windows.Data;
using YJ_AutoUnClamp.Models;

namespace YJ_AutoUnClamp.ViewModels
{
    public class DioManager_ViewModel : Child_ViewModel
    {
        #region // ICommand Property
        public ICommand Dio_Command { get; private set; }
        #endregion
        private EziDio_Model _Dio = SingletonManager.instance.Dio;
        public EziDio_Model Dio
        {
            get { return _Dio; }
            set { SetValue(ref _Dio, value); }
        }
        private EzMotion_Model_E _Motion = SingletonManager.instance.Ez_Model;
        public EzMotion_Model_E Motion
        {
            get { return _Motion; }
            set { SetValue(ref _Motion, value); }
        }
        private RadObservableCollection<bool> _DioUI;
        public RadObservableCollection<bool> DioUI
        {
            get { return _DioUI; }
            set { SetValue(ref _DioUI, value); }
        }
        private Timer DioTimer;

        public DioManager_ViewModel()
        {
            DioUI = new RadObservableCollection<bool>();
            for (int i = 0; i < Dio.DO_RAW_DATA.Count; i++)
            {
                DioUI.Add(false);
            }
            UupdateTimer_Dio();
        }
        private void UupdateTimer_Dio()
        {
            DioTimer = new Timer(300);
            DioTimer.Elapsed += OnTimerElapsed;
            DioTimer.AutoReset = true;
            DioTimer.Start();
        }
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            for (int i = 0; i < Dio.DO_RAW_DATA.Count; i++)
            {
                DioUI[i] = Dio.DO_RAW_DATA[i];
            }
        }
        private void OnDio_Command(object obj)
        {
            switch (obj.ToString())
            {
                case "SetUpDown":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_DOWN] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_DOWN, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);
                    break;
                case "Turn":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.OUT_PP_RIGHT_TURN] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.OUT_PP_RIGHT_TURN, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.OUT_PP_RIGHT_TURN, false);
                    break;
                case "Vacuum":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_VACUUM] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_VACUUM, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_VACUUM, false);
                    break;
                case "UnclampUpDown":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_DOWN] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_DOWN, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);
                    break;
                case "UnclampGripUnGrip1":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_GRIP] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_GRIP, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_GRIP, false);
                    break;
                case "UnclampGripUnGrip2":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_GRIP_F_FINGER] == false
                        || Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.OUT_PP_LEFT_Z_GRIP_R_FINGER] == false)
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_GRIP_F_FINGER, true);
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.OUT_PP_LEFT_Z_GRIP_R_FINGER, true);
                    }
                    else
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_GRIP_F_FINGER, false);
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.OUT_PP_LEFT_Z_GRIP_R_FINGER, false);
                    }
                    break;
                case "Centering":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNCLAMP_CV_CENTERING] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_CV_CENTERING, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_CV_CENTERING, false);
                    break;
                case "UnClampCvRunStop":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNCLAMP_CV_RUN] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_CV_RUN, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_CV_RUN, false);
                    break;
                case "RtnBtmUpDown":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.BOTTOM_RETURN_Z_DOWN] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_Z_DOWN, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_Z_DOWN, false);
                    break;
                case "RtnBtmGrip":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.BOTTOM_RETURN_Z_GRIP] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_Z_GRIP, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_Z_GRIP, false);
                    break;
                case "RtnBtmLR":
                    if (Motion.IsMoveUnclampPutDownDoneX() == false)
                    {
                        Global.instance.ShowMessagebox("Unclamp X is not Putdown Position.");
                        break;
                    }
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.BOTTOM_RETURN_X_FWD] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_X_FWD, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_X_FWD, false);
                    break;
                case "RtnTopUpDown":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.TOP_RETURN_Z_DOWN] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_Z_DOWN, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_Z_DOWN, false);
                    break;
                case "RtnTopGrip":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.TOP_RETURN_Z_GRIP] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_Z_GRIP, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_Z_GRIP, false);
                    break;
                case "RtnTopLR":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.TOP_RETURN_X_FWD] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_X_FWD, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_X_FWD, false);
                    break;
                case "RtnTopCV":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.TOP_JIG_CV_RUN] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_JIG_CV_RUN, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_JIG_CV_RUN, false);
                    break;
                case "Z_GripUnGrip":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNLOAD_LD_Z_GRIP] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_LD_Z_GRIP, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_LD_Z_GRIP, false);
                    break;
                case "LiftCvRunStop1":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.LIFT_CV_RUN_1] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.LIFT_CV_RUN_1, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.LIFT_CV_RUN_1, false);
                    break;
                case "LiftCvRunStop2":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.LIFT_CV_RUN_2] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.LIFT_CV_RUN_2, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.LIFT_CV_RUN_2, false);
                    break;
                case "LiftCvRunStop3":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.LIFT_CV_RUN_3] == false)
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.LIFT_CV_RUN_3, true);
                    else
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.LIFT_CV_RUN_3, false);
                    break;
                case "UnloadUpDownX":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNLOAD_Z_DOWN] == false)
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_Z_DOWN, true);
                    }
                    else
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_Z_DOWN, false);
                    }
                    break;
                case "UnloadGripX":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNLOAD_Z_GRIP] == false)
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_Z_GRIP, true);
                    }
                    else
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_Z_GRIP, false);
                    }
                    break;
                case "UnloadLR_X":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.UNLOAD_X_FWD] == false)
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_X_FWD, true);
                    }
                    else
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_X_FWD, false);
                    }
                    break;
                case "UnloadCV":
                    if (Dio.DO_RAW_DATA[(int)EziDio_Model.DO_MAP.INPUT_LEFT_SET_CV_RUN] == false)
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.INPUT_LEFT_SET_CV_RUN, true);
                    }
                    else
                    {
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.INPUT_LEFT_SET_CV_RUN, false);
                    }
                    break;
               
            }
        }
        #region // override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            // RelayCommand
            Dio_Command = new RelayCommand(OnDio_Command);
        }
        protected override void DisposeManaged()
        {
            Dio_Command = null;
            // Dispose Timer
            if (DioTimer != null)
            {
                DioTimer.Stop();
                DioTimer.Elapsed -= OnTimerElapsed;
                DioTimer.Dispose();
                DioTimer = null;
            }
            base.DisposeManaged();
        }
        #endregion
    }
}
