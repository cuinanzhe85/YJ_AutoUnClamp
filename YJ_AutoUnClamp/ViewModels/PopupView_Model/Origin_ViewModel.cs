using Common.Commands;
using Common.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using YJ_AutoUnClamp.Models;

namespace YJ_AutoUnClamp.ViewModels
{
    public class ServoSlaveViewModel : BindableAndDisposable
    {
        public string Name { get; set; }
        public int SlaveID { get; set; }

        private string _Color;
        public string Color
        {
            get { return _Color; }
            set { SetValue(ref _Color, value); }
        }
        private bool _IsChecked;
        public bool IsChecked
        {
            get { return _IsChecked; }
            set { SetValue(ref _IsChecked, value); }
        }
    }
    public class Origin_ViewModel : Child_ViewModel
    {
        #region // ICommands
        public ICommand Servo_Command { get; private set; }
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
        private EziDio_Model Dio = SingletonManager.instance.Dio;
        private EzMotion_Model_E Ez_Model = SingletonManager.instance.Ez_Model;
        public ObservableCollection<ServoSlaveViewModel> ServoSlaves { get; set; }
        public Origin_ViewModel()
        {
            ServoSlaves = new ObservableCollection<ServoSlaveViewModel>();

            for (int i = 0; i < (int)ServoSlave_List.Max; i++)
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
        private async void OnServo_Command(object obj)
        {
            string cmd = obj as string;
            bool result = false;
            switch (cmd)
            {
                case "All":
                    for (int i = 0; i < ServoSlaves.Count; i++)
                    {
                        ServoSlaves[i].IsChecked = true;
                    }
                    break;
                case "On":
                    foreach (var slave in ServoSlaves.Where(s => s.IsChecked))
                    {
                        result = SingletonManager.instance.Ez_Model.SetServoOn(slave.SlaveID, true);
                        slave.Color = result ? "LawnGreen" : "White";
                        //slave.IsChecked = false;
                        if (result == true)
                            Global.instance.ShowMessagebox($"Servo : {slave.Name} Power On Success", false);
                        else
                            Global.instance.ShowMessagebox("Servo : {slave.Name} Power On Fail");
                    }
                    break;
                case "Off":
                    string failedSlaves = string.Empty;
                    foreach (var slave in ServoSlaves.Where(s => s.IsChecked))
                    {
                        result = SingletonManager.instance.Ez_Model.SetServoOn(slave.SlaveID, false);
                        if (!result)
                        {
                            if (!string.IsNullOrEmpty(failedSlaves))
                                failedSlaves += ", ";
                            failedSlaves += slave.Name;
                        }
                        //slave.IsChecked = false;
                    }
                    if (!string.IsNullOrEmpty(failedSlaves))
                    {
                        string failedMessage = $"Failed to turn off the following Servo: {failedSlaves}";
                        Global.Mlog.Error(failedMessage);
                        Global.instance.ShowMessagebox(failedMessage);
                    }
                    break;
                case "AlarmReset":
                    string failedSlave = string.Empty;
                    foreach (var slave in ServoSlaves.Where(s => s.IsChecked))
                    {
                        result = SingletonManager.instance.Ez_Model.ServoAlarmReset(slave.SlaveID);
                        if (!result)
                        {
                            if (!string.IsNullOrEmpty(failedSlave))
                                failedSlave += ", ";
                            failedSlave += slave.Name;
                        }
                       // slave.IsChecked = false;
                    }
                    if (!string.IsNullOrEmpty(failedSlave))
                    {
                        string failedMessage = $"Failed to Alam Reset the following Servo: {failedSlave}";
                        Global.Mlog.Error(failedMessage);
                        Global.instance.ShowMessagebox(failedMessage);
                    }
                    break;
                case "Origin":
                    if (DoorOpenCheck() == true)
                        break;
                    BusyStatus = true;
                    // 선택된 슬레이브 필터링
                    var selectedSlaves = ServoSlaves.Where(slave => slave.IsChecked).ToList();
                    var failedSlavesList = new List<string>();
                    Dio.Set_HandlerInit(); 
                    await Task.Delay(1000);
                    // Servo Origin
                    var Slave = ServoSlaves[(int)ServoSlave_List.In_Z_Handler_Z];
                    if (Slave.IsChecked == true)
                    {
                        if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_Z_GRIP] == true
                            || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
                        {
                            Global.instance.ShowMessagebox("Please proceed after checking if there is a product.");
                            return;
                        }
                        BusyContent = $"Please Wait. Now Servo Origin...{Slave.Name}";
                        result = await SingletonManager.instance.Ez_Model.ServoOrigin(Slave.SlaveID);
                        Slave.Color = result ? "LawnGreen" : "White";
                        //Slave.IsChecked = false;
                        if (!result)
                        {
                            failedSlavesList.Add(Slave.Name);
                        }
                        SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Idle;
                    }
                    
                    Slave = ServoSlaves[(int)ServoSlave_List.Top_X_Handler_X];
                    if (Slave.IsChecked == true)
                    {
                        if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] == true
                            || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] == true
                            || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.BOTTOM_RETURN_Z_GRIP] == true
                            || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.TOP_RETURN_Z_GRIP] == true
                            || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNCLAMP_CV_DETECT] == true)
                        {
                            Global.instance.ShowMessagebox("Please remove Unclamp Unit all products");
                            return;
                        }
                        BusyContent = $"Please Wait. Now Servo Origin...{Slave.Name}";
                        result = await SingletonManager.instance.Ez_Model.ServoOrigin(Slave.SlaveID);
                        Slave.Color = result ? "LawnGreen" : "White";
                        //Slave.IsChecked = false;
                        if (!result)
                        {
                            failedSlavesList.Add(Slave.Name);
                        }

                        SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnClampStep = Unit_Model.UnClampHandStep.Idle;
                        SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].RtnBtmStep = Unit_Model.ReturnBottomStep.Idle;
                        SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].RtnTopStep = Unit_Model.ReturnTopStep.Idle;
                        SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Top_X].UnclampBottomReturnDone = false;
                        // Return Conveyor Interface Off
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BOTTOM_RETURN_CV_INTERFACE, false);
                        Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.TOP_RETURN_CV_INTERFACE, false);
                    }
                        
                    Slave = ServoSlaves[(int)ServoSlave_List.In_Y_Handler_Y];
                    if (Slave.IsChecked == true)
                    {
                        if (Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_Z_GRIP] == true
                            || Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
                        {
                            Global.instance.ShowMessagebox("Please proceed after checking if there is a product.");
                            return;
                        }
                        BusyContent = $"Please Wait. Now Servo Origin...{Slave.Name}";
                        result = await SingletonManager.instance.Ez_Model.ServoOrigin(Slave.SlaveID);
                        Slave.Color = result ? "LawnGreen" : "White";
                        //Slave.IsChecked = false;
                        if (!result)
                        {
                            failedSlavesList.Add(Slave.Name);
                        }
                        SingletonManager.instance.Unit_Model[(int)MotionUnit_List.In_Y].UnloadYStep = Unit_Model.Unload_Y_Step.Idle;
                    }
                    
                    if (selectedSlaves.Any())
                    {
                        // 병렬로 작업 실행
                        var tasks = selectedSlaves.Select(async slave =>
                        {
                            if (slave.SlaveID == (int)ServoSlave_List.Lift_1_Z
                            || slave.SlaveID == (int)ServoSlave_List.Lift_2_Z
                            || slave.SlaveID == (int)ServoSlave_List.Lift_3_Z)
                            {
                                if (slave.IsChecked == true)
                                {
                                    BusyContent = $"Please Wait. Now Servo Origin...{slave.Name}";
                                    result = await SingletonManager.instance.Ez_Model.ServoOrigin(slave.SlaveID);
                                    slave.Color = result ? "LawnGreen" : "White";
                                    //slave.IsChecked = false;
                                    if (!result)
                                    {
                                        failedSlavesList.Add(slave.Name);
                                    }
                                }
                            }
                            
                        });
                        // 모든 작업 완료 대기
                        await Task.WhenAll(tasks);
                        SingletonManager.instance.Unit_Model[(int)MotionUnit_List.Lift_1].LiftStep = Unit_Model.Lift_Step.Idle;
                    }
                    // 실패한 슬레이브가 있는 경우 메시지 표시
                    if (failedSlavesList.Any())
                    {
                        string failedMessage = $"Failed to complete origin operation for the following Servo(s): {string.Join(", ", failedSlavesList)}";
                        Global.Mlog.Error(failedMessage);
                        Global.instance.ShowMessagebox(failedMessage);
                    }
                    else
                    {
                        Global.instance.ShowMessagebox("Origin Success", false);
                    }
                    BusyContent = string.Empty;
                    BusyStatus = false;
                    break;
            }
        }
        private bool DoorOpenCheck()
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
                return true;
            }
            return false;
        }
        #region // override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            Servo_Command = new RelayCommand(OnServo_Command);
        }
        protected override void DisposeManaged()
        {
            // ICommands 정리
            Servo_Command = null;

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
