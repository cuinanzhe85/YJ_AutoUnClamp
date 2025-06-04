using Common.Commands;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using YJ_AutoUnClamp.Models;

namespace YJ_AutoUnClamp.ViewModels
{
    public class Initialize_ViewModel : Child_ViewModel
    {
        #region // ICommands
        public ICommand Init_Command { get; private set; }
        #endregion
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

            for (int i = 0; i < (int)ServoSlave_List.Max; i++)
            {
                if ( i != (int)ServoSlave_List.Lift_1_Z
                    && i != (int)ServoSlave_List.Lift_2_Z
                    && i != (int)ServoSlave_List.Lift_3_Z)
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
                    BusyStatus = true;
                    string failedSlave = string.Empty;
                    BusyContent = "Initializing Servo Slaves...";
                    // Functions
                    foreach (var slave in ServoSlaves.Where(s => s.IsChecked))
                    {
                        if (slave.Name == "Top X Handler X")
                        {
                            Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_X_FWD, false);

                            result = await ServoUnclamp();
                            if (!result)
                            {
                                if (!string.IsNullOrEmpty(failedSlave))
                                    failedSlave += ", ";
                                failedSlave += slave.Name;
                            }
                            slave.IsChecked = false;
                        }
                        else if (slave.Name == "In Y Handler Y")
                        {
                            result = await ServoUnloadingY();
                            if (!result)
                            {
                                if (!string.IsNullOrEmpty(failedSlave))
                                    failedSlave += ", ";
                                failedSlave += slave.Name;
                            }
                            slave.IsChecked = false;
                        }
                        else if (slave.Name == "In Z Handler Z")
                        {
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
                    BusyContent = string.Empty;
                    BusyStatus = false;
                    break;
            }
        }
        private async Task<bool> ServoUnloadingZ()
        {
            if (Ez_Model.MoveReadyPosZ() == false)
                return false;
            bool result = false;
            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                while (true)
                {
                    if (Ez_Model.IsMoveReadyPosZ() == true)
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
            });
            if (result == true)
            {
                if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Grip_Check;
                }
                else
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Idle;
                }
            }
            return result;
        }
        private async Task<bool> ServoUnloadingY()
        {
            // Unloading X Right
            Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_Z_DOWN, false);
            await Task.Delay(1000);
            Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNLOAD_X_FWD, false);
            if (Ez_Model.IsMoveReadyPosZ() == false)
            {
                Global.instance.ShowMessagebox("Y initialization failed. Move the Z axis to Ready position.");
                return false;
            }
            if (Ez_Model.MoveReadyPosY() == false)
                return false;
            bool result = false;
            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                while (true)
                {
                    if (Ez_Model.IsMoveReadyPosY() == true)
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
            });
            if (result == true)
            {
                if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Grip_Check;
                }
                else
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Idle;
                }
                // Unloading X Step 설정
                if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_Z_GRIP] == true)
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadXlStep = Unit_Model.Unload_X_Step.Left_Up_Check;
                }
                else
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadXlStep = Unit_Model.Unload_X_Step.Idle;
                }
            }
            return result;
        }
        private async Task<bool> ServoUnclamp()
        {
            Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);
            Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);
            if (Ez_Model.MoveTopReadyPosX() == false)
                return false;
            bool result = false;
            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                while (true)
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
            });
            if (result == true)
            {
                if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] == true)
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnClampStep = Unit_Model.UnClampHandStep.Set_Hand_Up_Check;
                }
                else if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] == false
                    && Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] == true)
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnClampStep = Unit_Model.UnClampHandStep.Top_Hand_Up_Check;
                }
                else if(Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] == true
                    && Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] == false)
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnClampStep = Unit_Model.UnClampHandStep.Set_Hand_Up_Check;
                }
                else
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnClampStep = Unit_Model.UnClampHandStep.Idle;
                }

                if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.BOTTOM_RETURN_Z_GRIP] == true)
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].RtnBtmStep = Unit_Model.ReturnBottomStep.Left_Move_Done;
                }
                else
                {
                    SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].RtnBtmStep = Unit_Model.ReturnBottomStep.Idle;
                }
            }
            return result;
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
