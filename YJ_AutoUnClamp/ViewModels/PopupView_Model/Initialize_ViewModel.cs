using Common.Commands;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using YJ_AutoUnClamp.Models;

namespace YJ_AutoUnClamp.ViewModels
{
    public class Initialize_ViewModel : Child_ViewModel
    {
        #region // ICommands
        public ICommand Init_Command { get; private set; }
        #endregion
        public enum InitializeList
        {
            Unload_Y,
            Unload_Z,
            UnClamp_X,
            Max
        }
        private bool _BusyStatus;
        public bool BusyStatus
        {
            get { return _BusyStatus; }
            set { SetValue(ref _BusyStatus, value); }
        }
        private string _BusyContent;
        public string BusyContent
        {
            get { return _BusyContent; }
            set { SetValue(ref _BusyContent, value); }
        }
        private EziDio_Model Dio = SingletonManager.instance.Ez_Dio;
        private EzMotion_Model_E Ez_Model = SingletonManager.instance.Ez_Model;
        public ObservableCollection<ServoSlaveViewModel> ServoSlaves { get; set; }
        public Initialize_ViewModel()
        {
            ServoSlaves = new ObservableCollection<ServoSlaveViewModel>();

            for (int i = 0; i < (int)InitializeList.Max; i++)
            {
                ServoSlaves.Add(new ServoSlaveViewModel()
                {
                    Name = ((ServoSlave_List)i).ToString().Replace("_", " "),
                    Color = "White",
                    SlaveID = i,
                    IsChecked = false
                });
            }
        }
        private async void OnInit_Command(object obj)
        {
            string cmd = obj as string;
            bool result = false;
            switch (cmd)
            {
                case "All":
                    for (int i = 0; i < ServoSlaves.Count; i++)
                        ServoSlaves[i].IsChecked = true;
                    break;
                case "Cancel":
                    for (int i = 0; i < ServoSlaves.Count; i++)
                        ServoSlaves[i].IsChecked = false;
                    break;
                case "Init":
                    DoorOpenCheck();
                    BusyStatus = true;
                    string failedSlave = string.Empty;
                    BusyContent = "Initializing Start...";
                    // Functions
                    foreach (var slave in ServoSlaves.Where(s => s.IsChecked))
                    {
                        if (slave.Name == "UnClamp X")
                        {
                            BusyContent = "UnClamp X Initializing...";
                            result = await UnclampUnitInit();
                            if (!result)
                            {
                                if (!string.IsNullOrEmpty(failedSlave))
                                    failedSlave += ", ";
                                failedSlave += slave.Name;
                            }
                            slave.IsChecked = false;
                        }
                        else if (slave.Name == "Unload Y")
                        {
                            BusyContent = "Unload Y Initializing...";
                            result = await ServoUnloadingY();
                            if (!result)
                            {
                                if (!string.IsNullOrEmpty(failedSlave))
                                    failedSlave += ", ";
                                failedSlave += slave.Name;
                            }
                            slave.IsChecked = false;
                        }
                        else if (slave.Name == "Unload Z")
                        {
                            BusyContent = "Unload Z Initializing...";
                            result = await ServoUnloadingZ();
                            if (!result)
                            {
                                if (!string.IsNullOrEmpty(failedSlave))
                                    failedSlave += ", ";
                                failedSlave += slave.Name;
                            }
                            slave.IsChecked = false;
                        }
                    }
                    if (!string.IsNullOrEmpty(failedSlave))
                    {
                        failedSlave += " Initial faile";
                        Global.instance.ShowMessagebox(failedSlave);
                    }
                    BusyContent = string.Empty;
                    BusyStatus = false;
                    break;
            }
        }
        private async Task<bool> ServoUnloadingZ()
        {
            if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_Z_GRIP] == true
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
            {
                Global.instance.ShowMessagebox("Please proceed after checking if there is a product.");
                return false;
            }
            
            bool result = false;
            await Task.Run(() =>
            {
                if (Ez_Model.MoveReadyPosZ() == false)
                    result = false;
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                while (result)
                {
                    if (Ez_Model.IsMoveReadyPosZ() == true)
                    {
                        result = true;
                        break; // 성공 시 루프 종료
                    }
                    if (sw.ElapsedMilliseconds > 10000)
                    {
                        result = false; // 10초 후에 중단
                        break; // 10초 후에 중단
                    }
                    Task.Delay(100).Wait();
                }
            });
            SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Idle;
            return result;
        }
        private async Task<bool> ServoUnloadingY()
        {
            if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_Z_GRIP] == true
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
            {
                Global.instance.ShowMessagebox("Please proceed after checking if there is a product.");
                return false;
            }
            if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_BUFFER] == true)
            {
                Global.instance.ShowMessagebox("There is a product in the buffer section.");
                return false;
            }
            if (Ez_Model.IsMoveReadyPosZ() == false)
            {
                Global.instance.ShowMessagebox("Y initialization failed. Move the Z axis to Ready position.");
                return false;
            }
            
            bool result = false;
            await Task.Run(async () =>
            {
                Stopwatch sw = new Stopwatch();
                // Unloading X Z UP
                Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_Z_DOWN, false);
                
                sw.Restart();
                while(true)
                {
                    if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_Z_UP] == true)
                    {
                        result = true;
                        break;
                    }
                    else if (sw.ElapsedMilliseconds > 2000)
                    {
                        result = false;
                        Global.instance.ShowMessagebox("Unload Z Cyl Up Fail");
                        return;
                    }
                    await Task.Delay(100);
                }
                // X Right Move
                Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_X_FWD, false);
                sw.Restart();
                while (true)
                {
                    if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_X_RIGHT] == true)
                    {
                        result = true;
                        break;
                    }
                    else if (sw.ElapsedMilliseconds > 2000)
                    {
                        result = false;
                        Global.instance.ShowMessagebox("Y initialization failed. Move the Z axis to Ready position.");
                        break;
                    }
                    Task.Delay(100).Wait();
                }
                if (result == true)
                {
                    if (Ez_Model.MoveReadyPosY() == false)
                        result = false;
                    sw.Restart();
                    while (result)
                    {
                        if (Ez_Model.IsMoveReadyPosY() == true)
                        {
                            result = true;
                            break; // 성공 시 루프 종료
                        }
                        if (sw.ElapsedMilliseconds > 10000)
                        {
                            result = false; // 10초 후에 중단
                            break; // 10초 후에 중단
                        }
                        Task.Delay(100).Wait();
                    }
                }
                
            });
            SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Idle;
            SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadXlStep = Unit_Model.Unload_X_Step.Idle;
            
            return result;
        }
        private async Task<bool> UnclampUnitInit()
        {
            if(Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] == true
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] == true
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.BOTTOM_RETURN_Z_GRIP] == true
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.TOP_RETURN_Z_GRIP] == true
                || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNCLAMP_CV_DETECT] == true)
            {
                Global.instance.ShowMessagebox("Please remove Unclamp Unit all products");
                return false;
            }
            bool result = false;
            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();

                Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);
                Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);
                Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_Z_DOWN, false);
                Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_Z_DOWN, false);

                Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_CV_CENTERING, false);
                sw.Restart();
                while (true)
                {
                    if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_LEFT_Z_UP_CYL] == true
                    && Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_TR_RIGHT_Z_UP_CYL] == true
                    && Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.TOP_RETURN_Z_UP] == true
                    && Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.BOTTOM_RETURN_Z_UP] == true)
                    {
                        result = true;
                        break; // 성공 시 루프 종료
                    }
                    if (sw.ElapsedMilliseconds > 5000)
                    {
                        result = false; // 10초 후에 중단
                        break; // 10초 후에 중단
                    }
                    Task.Delay(100).Wait();
                }
                if ( result == true)
                {
                    Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_X_FWD, false); // Left 이동
                    Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_X_FWD, true); // Right 이동

                    sw.Restart();
                    while (true)
                    {
                        if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.BOTTOM_RETURN_X_LEFT] == true
                        && Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.TOP_RETURN_X_RIGHT_CYL] == true)
                        {
                            result = true;
                            break; // 성공 시 루프 종료
                        }
                        if (sw.ElapsedMilliseconds > 5000)
                        {
                            result = false; // 10초 후에 중단
                            break; // 10초 후에 중단
                        }
                        Task.Delay(100).Wait();
                    }
                }
                if (result == true)
                {
                    if (Ez_Model.MoveTopReadyPosX() == false)
                        result = false;
                    sw.Restart();
                    while (result)
                    {
                        if (Ez_Model.IsMoveTopReadyDoneX() == true)
                        {
                            result = true;
                            break; // 성공 시 루프 종료
                        }
                        if (sw.ElapsedMilliseconds > 5000)
                        {
                            result = false; // 10초 후에 중단
                            break; // 10초 후에 중단
                        }
                        Task.Delay(100).Wait();
                    }
                }
            });
            SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnClampStep = Unit_Model.UnClampHandStep.Idle;
            SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].RtnBtmStep = Unit_Model.ReturnBottomStep.Idle;
            SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].RtnTopStep = Unit_Model.ReturnTopStep.Idle;
            SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnclampBottomReturnDone = false;
            // Return Conveyor Interface Off
            Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_CV_INTERFACE, false);
            Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_CV_INTERFACE, false);
            return result;
        }
        private void DoorOpenCheck()
        {
            // Safety 먼저 체크
            if (!Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.REAR_LEFT_DOOR]
                || !Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.FRONT_RIGHT_DOOR]
                || !Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.FRONT_LEFT_DOOR])
            {
                Application.Current.Dispatcher.BeginInvoke(
                                (ThreadStart)(() =>
                                {
                                    // Todo : Interlock Loop Stop. 진행중인 작업 모두 정지
                                    Global.instance.InspectionStop();
                                    // Safety Popup
                                    Window window = new Safety_View();
                                    Safety_ViewModel safety_ViewModel = new Safety_ViewModel();
                                    window.DataContext = safety_ViewModel;
                                    window.ShowDialog();
                                    // Close
                                    safety_ViewModel.Dispose();
                                    safety_ViewModel = null;
                                    window.Close();
                                    window = null;
                                }), DispatcherPriority.Send);
            }
        }
        #region // override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            Init_Command = new RelayCommand(OnInit_Command);
        }
        protected override void DisposeManaged()
        {
            // ICommands 정리
            Init_Command = null;

            // ServoSlaves 컬렉션 정리
            if (ServoSlaves != null)
            {
                foreach (var slave in ServoSlaves)
                {
                    slave.Dispose(); // ServoSlaveViewModel이 IDisposable을 상속받는 경우
                }
                ServoSlaves.Clear();
                ServoSlaves = null;
            }

            // 부모 클래스의 DisposeManaged 호출
            base.DisposeManaged();
        }
        #endregion
    }
}
