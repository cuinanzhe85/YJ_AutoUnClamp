using Common.Managers;
using Common.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Data;
using YJ_AutoUnClamp.Models;
using YJ_AutoUnClamp.Utils;
using YJ_AutoUnClamp.ViewModels;
using static YJ_AutoUnClamp.Models.EziDio_Model;

namespace YJ_AutoUnClamp
{
    public enum EquipmentMode
    {
        Auto,
        Dry
    }
    public class SingletonManager : BindableAndDisposable
    {
        static public SingletonManager instance = new SingletonManager();
        // Background Thread 관련
        private BackgroundWorker UnitsProcThread;
        public bool IsWorkingUnitsProcThread = true;
        public bool IsSafetyInterLock = false;
        private bool _IsInspectionStart = false;
        public bool IsInspectionStart
        {
            get { return _IsInspectionStart; }
            set { SetValue(ref _IsInspectionStart, value); }
        }
        private msgQueue SequenceQueue; // Sequence 관련 Q

        // Out Y축 이동시 사용되는 변수
        private bool _IsY_PickupColl = false;
        public bool IsY_PickupColl
        {
            get { return _IsY_PickupColl; }
            set { SetValue(ref _IsY_PickupColl, value); }
        }
        public ObservableCollection<int> UnLoadFloor { get; set; }
        public ObservableCollection<Lift_Model> Display_Lift { get; set; }
        public int UnLoadStageNo = 0;
        public bool BottomClampDone = false;
        public bool BottomClampNG = false;
        // 7단 Loading완료 변수 
        public bool[] LoadComplete = { false, false, false };
        public string Nfc_Data = string.Empty;

        private bool _IsTcpConnected = false;
        public bool IsTcpConnected
        {
            get { return _IsTcpConnected; }
            set { SetValue(ref _IsTcpConnected, value); }
        }
        // Default Infomation
        public EquipmentMode EquipmentMode { get; set; } = EquipmentMode.Auto;

        #region // Properties
        public EziDio_Model Ez_Dio { get; set; }
        public EzMotion_Model_E Ez_Model { get; set; }
        public Serial_Model[] SerialModel { get; set; }
        public RadObservableCollection<Unit_Model> Unit_Model { get; set; }
        public RadObservableCollection<Servo_Model> Servo_Model { get; set; }
        public RadObservableCollection<Channel_Model> Channel_Model { get; set; }
        public Dictionary<string, double> Teaching_Data { get; set; }
        public ObservableCollection<SpecData_Model> Spec_Data { get; set; }
        public ModelData_Model Current_Model { get; set; }
        public SystemData_Model SystemModel { get; set; }
        public Http_Model HttpModel { get; set; }
        public Aging_Model AgingModel { get; set; }
        public HttpJson_Model HttpJsonModel { get; set; }
        public HttpClient HttpClient { get; set; }

        public TcpClient_Model TcpClient { get; set; }

        #endregion
        #region // UI Properties
        public RadObservableCollection<bool> DisplayUI_Dio { get; set; }

        #endregion
        private SingletonManager()
        {
            UnitsProcThread = new BackgroundWorker();
            SequenceQueue = new msgQueue();
            Unit_Model = new RadObservableCollection<Unit_Model>();
            Servo_Model = new RadObservableCollection<Servo_Model>();
            Ez_Model = new EzMotion_Model_E();
            Ez_Dio = new EziDio_Model();
            DisplayUI_Dio = new RadObservableCollection<bool>();
            Channel_Model = new RadObservableCollection<Channel_Model>();
            Teaching_Data = new Dictionary<string, double>();
            SerialModel = new Serial_Model[(int)Serial_Model.SerialIndex.Max];
            Current_Model = new ModelData_Model();
            Spec_Data = new ObservableCollection<SpecData_Model>();
            SystemModel = new SystemData_Model();
            HttpModel = new Http_Model();
            AgingModel = new Aging_Model();
            HttpJsonModel = new HttpJson_Model();
            HttpClient = new HttpClient();
            TcpClient = new TcpClient_Model();

            UnLoadFloor = new ObservableCollection<int>();
            for (int i = 0; i < (int)Lift_Index.Max; i++)
                UnLoadFloor.Add(0);

            for(int i=0; i< (int)Serial_Model.SerialIndex.Max; i++)
            {
                SerialModel[i] = new Serial_Model();
            }
            Display_Lift = new ObservableCollection<Lift_Model>();
            for (int i = 0; i < 3; i++)
                Display_Lift.Add(new Lift_Model("LIFT" + (i + 1)));

            // Channel Model 초기화
            for (int i = 0; i < (int)ChannelList.Max; i++)
            {
                Channel_Model.Add(new Channel_Model((ChannelList)i));
            }
            HttpClient.Timeout = TimeSpan.FromSeconds(2);
        }
        public void Run()
        {
            //BusyContent
            Global.instance.BusyContent = "Program Initialize...";
            // Load System Files.
            LoadSystemFiles();
            // Unit & Servo Init 
            Unit_Init();
            // Motion Init
            Motion_Init();
            // Dio Init
            DioBoard_Init();
            // Serial Port Init : Barcode, Label Print
            SerialPort_init();
            // Load Teaching Data
            LoadTeachFile();
            // Load Velocity Data
            LoadVelocityFiles();
            // Background Thread Start
            BackgroundThread_Init();
            Global.instance.BusyStatus = false;
            Global.instance.BusyContent = string.Empty;

            TcpClient.Connect("192.168.10.20", 8000);
        }
        private void LoadSystemFiles()
        {
            //BusyContent
            Global.instance.BusyContent = "System Operation Loading...";

            // Config 폴더 경로 설정
            string configPath = Global.instance.IniConfigPath;

            // Model, Job, Teach 폴더 경로 설정
            string modelFolder = Path.Combine(configPath, "Model");
            string specFolder = Path.Combine(configPath, "Spec");
            string jobFolder = Path.Combine(configPath, "Job");
            string teachFolder = Path.Combine(configPath, "Teach");

            // Config 폴더가 없으면 생성
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);
            // Model 폴더가 없으면 생성
            if (!Directory.Exists(modelFolder))
                Directory.CreateDirectory(modelFolder);
            // Spec 폴더가 없으면 생성
            if (!Directory.Exists(specFolder))
                Directory.CreateDirectory(specFolder);
            // Job 폴더가 없으면 생성
            if (!Directory.Exists(jobFolder))
                Directory.CreateDirectory(jobFolder);
            // Teach 폴더가 없으면 생성
            if (!Directory.Exists(teachFolder))
                Directory.CreateDirectory(teachFolder);

            var myIni = new IniFile(Global.instance.IniSystemPath);
            string section = "SYSTEM";
            string valus = myIni.Read("BARCODE_USE", section);
            SystemModel.BcrUseNotUse = valus;

            valus = myIni.Read("UNLOAD_COUNT", section);
            if (string.IsNullOrEmpty(valus))
                Channel_Model[0].UnLoadCount = "0";
            else
                Channel_Model[0].UnLoadCount = valus;

            valus = myIni.Read("NFC_USE", section);
            SystemModel.NfcUseNotUse = valus;

            valus = myIni.Read("AGINT_TIME", section);
            SystemModel.AgingTime = valus;

            valus = myIni.Read("TOP_CODE", section);
            SystemModel.TopCode = valus;

            valus = myIni.Read("AGINT_BARCODE_FILE_PATH", section);
            SystemModel.AgingBarcodFilePath = valus;
        }
       
        public void LoadTeachFile()
        {
            // Teaching Data 섹션 데이터 로드
            string teachFilePath = Path.Combine(Global.instance.IniTeachPath);
            var iniTeachFile = new IniFile(teachFilePath);

            string[] teachSection = { "Top_X", "In_Y", "In_Z", "Lift" };
            Teaching_Data.Clear();
            foreach (var sectionName in teachSection)
            {
                foreach (Teaching_List teachingItem in Enum.GetValues(typeof(Teaching_List)))
                {
                    if (teachingItem == Teaching_List.Max) // Max는 제외
                        continue;

                    // 섹션 이름이 항목 이름의 시작 부분에 포함된 경우만 처리
                    if (teachingItem.ToString().IndexOf(sectionName, StringComparison.Ordinal) != 0)
                        continue;

                    string value = iniTeachFile.Read(teachingItem.ToString(), sectionName);
                    if (double.TryParse(value, out double parsedValue))
                    {
                        Teaching_Data[teachingItem.ToString()] = parsedValue; // Teaching_Data에 저장
                    }
                }
            }
        }
        public void LoadVelocityFiles()
        {
            // INI 파일 경로 설정
            var iniVelocityFile = new IniFile(Global.instance.IniVelocityPath);

            // Motor_Velocity 섹션 데이터 로드
            string section = "Motor_Velocity";
            foreach (var servo in Servo_Model)
            {
                string servoName = servo.ServoName.ToString();

                // INI 파일에서 각 속성 값을 읽어옴
                string velocity = iniVelocityFile.Read($"{servoName}_Velocity", section);
                string accelerate = iniVelocityFile.Read($"{servoName}_Accelerate", section);
                string decelerate = iniVelocityFile.Read($"{servoName}_Decelerate", section);
                string measurementVel = iniVelocityFile.Read($"{servoName}_Measurement_Vel", section);
                string barcodeVel = iniVelocityFile.Read($"{servoName}_Barcode_Vel", section);

                // 읽어온 값을 Double로 변환하여 Servo_Model에 반영
                if (double.TryParse(velocity, out double parsedVelocity))
                    servo.Velocity = parsedVelocity;

                if (double.TryParse(accelerate, out double parsedAccelerate))
                    servo.Accelerate = parsedAccelerate;

                if (double.TryParse(decelerate, out double parsedDecelerate))
                    servo.Decelerate = parsedDecelerate;

                if (double.TryParse(measurementVel, out double parsedMeasurementVel))
                    servo.Measurement_Vel = parsedMeasurementVel;

                if (double.TryParse(barcodeVel, out double parsedBarcodeVel))
                    servo.Barcode_Vel = parsedBarcodeVel;
            }
            // Jog_Velocity 섹션 데이터 로드
            string jogSection = "Jog_Velocity";
            string[] jogLabels = { "LOW", "MIDDLE", "HIGH" }; // Jog 속도 레이블
            foreach (var servo in Servo_Model)
            {
                string servoName = servo.ServoName.ToString();

                // INI 파일에서 JogVelocity 값을 읽어와 리스트의 특정 인덱스에 직접 할당
                for (int i = 0; i < jogLabels.Length; i++)
                {
                    string jogValue = iniVelocityFile.Read($"{servoName}_JogVelocity_{jogLabels[i]}", jogSection);
                    if (double.TryParse(jogValue, out double parsedJogValue))
                    {
                        servo.JogVelocity[i] = parsedJogValue; // 특정 인덱스에 값 할당
                    }
                }
            }
        }
        private void SerialPort_init()
        {
            //BusyContent
            Global.instance.BusyContent = "Serial Port Connecting...";

            // Serial 설정 파일 경로
            var myIni = new IniFile(Global.instance.IniSystemPath);
            string section = "SERIAL";
            string key = "";
            string massage = "";
            // Barcode Serial Port Init

            for (int i = 0; i < (int)Serial_Model.SerialIndex.Max; i++)
            {
                if (i == (int)Serial_Model.SerialIndex.Nfc)
                {
                    // NFC는 별도로 처리
                    key = "NFC_PORT";
                    string nfcPort = myIni.Read(key, section);
                    SerialModel[i].PortName = key;
                    SerialModel[i].Port = nfcPort;
                    if (SerialModel[i].Open() == false)
                        massage += nfcPort + "NFC Open Fail Port open fail.\r\n";
                    continue;
                }
                else if (i == (int)Serial_Model.SerialIndex.Mes)
                {
                    // MES Port Open
                    key = "MES_PORT";
                    string nfcPort = myIni.Read(key, section);
                    SerialModel[i].PortName = key;
                    SerialModel[i].Port = nfcPort;
                    if (SerialModel[i].Open() == false)
                        massage += nfcPort + "MES Open Fail Port open fail.\r\n";
                    continue;
                }
                else
                {
                    key = $"BARCODE_PORT_{i + 1}";
                    string bcrPort = myIni.Read(key, section);
                    SerialModel[i].PortName = key;
                    SerialModel[i].Port = bcrPort;

                    if (SerialModel[i].Open() == false)
                        massage += bcrPort + "BCR Open Fail Port open fail.\r\n";
                }
            }
            if (string.IsNullOrEmpty(massage)== false)
                Global.instance.ShowMessagebox(massage);
        }
        
        private void DioBoard_Init()
        {
            //BusyContent
            Global.instance.BusyContent = "Dio Board Connecting...";
            
            for (int i =0; i < (int)DI_MAP.DI_MAX / 16; i++)
            {
                Ez_Dio.Connect(i);
            }

            for (int i = 0; i < Ez_Dio.DisplayDio_List.Count + (int)EziDio_Model.DisplayExist_List.Max; i++)
                DisplayUI_Dio.Add(false);

            // Tower Lamp Init
            Global.instance.Set_TowerLaamp(Global.TowerLampType.Init);
        }
        private void Motion_Init()
        {
            //BusyContent
            Global.instance.BusyContent = "Ez Motion Connecting...";
            string error = "";
            for (int i=0; i<(int)ServoSlave_List.Max; i++)
            {
                if (Ez_Model.Connect(i) == false)
                {
                    if (string.IsNullOrEmpty(error) == false)
                        error += ", ";
                    error += ((ServoSlave_List) i).ToString();
                }
            }
            Global.instance.BusyStatus = false;
            Global.instance.BusyContent = string.Empty;
            if (string.IsNullOrEmpty(error) == false)
            {
                error += " Ez Motion Motion Connect Fail";
                Global.instance.ShowMessagebox(error);
            }
        }
        private void Unit_Init()
        {
            //BusyContent
            Global.instance.BusyContent = "Units Initializing ...";

            // Unit Model 초기화
            for (int i = 0; i < (int)MotionUnit_List.Max; i++)
            {
                // 유닛 그룹 추가
                Unit_Model.Add(new Unit_Model((MotionUnit_List)i));
                string _UnitGroup = ((MotionUnit_List)i).ToString();

                for (int j = 0; j < (int)ServoSlave_List.Max; j++)
                {
                    // 유닛그룹에 있는 서보를 찾았을 경우 카운트 증가
                    string _ServoName = ((ServoSlave_List)j).ToString();
                    if (_ServoName.Contains(_UnitGroup))
                    {
                        Unit_Model[i].ServoNames.Add((ServoSlave_List)j);
                    }
                    Unit_Model[i].SetLastStep();
                }
            }
            // Servo Model 초기화
            for(int i = 0; i< (int)ServoSlave_List.Max; i++)
            {
                Servo_Model.Add(new Servo_Model((ServoSlave_List)i));
            }
            
        }
        // Background Worker
        private void BackgroundThread_Init()
        {
            // 작업 쓰레드 ( Station )
            UnitsProcThread.WorkerReportsProgress = false;        // 진행률 전송 여부
            UnitsProcThread.WorkerSupportsCancellation = true;    // 작업 취소 여부
            UnitsProcThread.DoWork += new DoWorkEventHandler(UnitsProc_DoWork);
            UnitsProcThread.RunWorkerAsync();
        }
        public void BackgroundThread_Stop()
        {
            // UnitsProcThread 작업 취소
            if (UnitsProcThread != null)
            {
                UnitsProcThread.CancelAsync(); // 작업 취소 요청
                while (UnitsProcThread.IsBusy) // 작업이 완료될 때까지 대기
                {
                    Thread.Sleep(10);
                }

                // 이벤트 핸들러 제거
                UnitsProcThread.DoWork -= UnitsProc_DoWork;

                // BackgroundWorker 정리
                UnitsProcThread.Dispose();
                UnitsProcThread = null;
            }
            // SequenceQueue 정리
            if (SequenceQueue != null)
            {
                SequenceQueue.Clear(); // 메시지 큐 비우기
                SequenceQueue = null;
            }
        }
        private void UnitsProc_DoWork(object sender, DoWorkEventArgs e)
        {
            // 프로그램이 종료될 때까지 아래의 루프가 반복됨
            while (true)
            {
                try
                {
                    if (UnitsProcThread.CancellationPending == true)
                        break;

                    // 메시지 큐에 메시지가 있다면 (전역으로 데이터를 송신하기 위한 목적으로 사용)
                    if (SequenceQueue.GetCount() > 0)
                    {
                        msgItem msg = SequenceQueue.GetItem(0); //  첫번째 메시지를 가져온다.

                        // 메시지의 evt(이벤트)에 따라 역할을 수행
                        switch (msg.evt)
                        {
                            case msgItem.Event.SendVisionMsg:
                                {
                                    // Vision 데이터 송신. 정상적으로 연결되어있다면
                                    break;
                                }
                        }
                    }

                    // Tact Time Display
                    for (int i = 0; i < (int)ChannelList.Max; i++)
                    {
                        if (Channel_Model[i].Status == ChannelStatus.RUNNING)
                            Channel_Model[i].GetTactTime();
                    }

                    // 데이터 송신 외에는 아래의 상태 루프를 반복적으로 수행
                    if (IsWorkingUnitsProcThread == true)
                    {
                        if (!Ez_Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.FRONT_OP_EMERGENCY_FEEDBACK]
                            || !Ez_Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.REAR_OP_EMERGENCY_FEEDBACK])
                        {
                            Global.instance.SafetyErrorMessage = "EMERGENCY Button Operation! ";
                            //IsSafetyInterLock = true;
                        }
                        if (IsSafetyInterLock == true)
                        {
                            IsWorkingUnitsProcThread = false;

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

                                    IsSafetyInterLock = false;
                                    IsWorkingUnitsProcThread = true;

                                }), DispatcherPriority.Send);
                        }
                        else
                        {
                            // 시작 신호가 들어오면 검사 Loop 반복
                            if(IsInspectionStart == true)
                            {
                                // Safety 먼저 체크
                                if (!Ez_Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.REAR_LEFT_DOOR]
                                || !Ez_Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.FRONT_RIGHT_DOOR]
                                || !Ez_Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.FRONT_LEFT_DOOR])
                                {
                                    Global.instance.SafetyErrorMessage = "DOOR IS OPEN ! ";
                                    
                                    IsSafetyInterLock = true;
                                }
                                else
                                {
                                    for (int i = 0; i < (int)MotionUnit_List.Max; i++)
                                    {
                                        if (i == (int)MotionUnit_List.Top_X
                                            || i == (int)MotionUnit_List.In_Y
                                            || i == (int)MotionUnit_List.Lift_1
                                            || i == (int)MotionUnit_List.In_CV
                                            || i == (int)MotionUnit_List.Out_CV)
                                        {
                                            Unit_Model[i].Loop();
                                            // Unclamp Mes Error
                                            if (!string.IsNullOrEmpty(Unit_Model[i].UnclampSetFailMessage))
                                            {
                                                Global.instance.Set_TowerLaamp(Global.TowerLampType.Error);
                                                Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BUZZER, true);
                                                Global.instance.ShowMessagebox(Unit_Model[i].UnclampSetFailMessage);
                                                Ez_Dio.SetIO_OutputData((int)EziDio_Model.DO_MAP.BUZZER, false);
                                                Global.instance.InspectionStop();
                                            }
                                        }
                                            
                                        Thread.Sleep(5);
                                    }
                                   Global.instance.UnLoadingTactTimeEnd();
                                }
                                if (Ez_Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OP_BOX_STOP])
                                {
                                    Global.instance.InspectionStop();
                                }
                            }
                            // 검사 시작 신호가 들어오지 않으면 스위치 체크
                            else
                            {
                                if (Ez_Dio.DI_RAW_DATA[(int)EziDio_Model.DI_MAP.OP_BOX_START])
                                {
                                    _ = Global.instance.InspectionStart();
                                }
                                Unit_Model[(int)MotionUnit_List.In_CV].UnlodingCvLogic();
                                Thread.Sleep(5);
                            }
                        }
                    }
                }
                catch (Exception ee)
                {
                    Global.ExceptionLog.ErrorFormat($"{System.Reflection.MethodBase.GetCurrentMethod().Name} - {ee}");
                }
            }
        }
        private DateTime? _StartPressedTime = null;
        private DateTime? _StopPressedTime = null;

        #region // override
        protected override void DisposeManaged()
        {
            // NMC Motion 해제
            if (Ez_Model != null)
            {
                Ez_Model.Dispose();
                Ez_Model = null;
            }
            // Dio 정리
            if (Ez_Dio != null)
            {
                Ez_Dio = null;
            }
            // Channel_Model 정리
            if (Channel_Model != null)
            {
                foreach (var channel in Channel_Model)
                {
                    channel.Dispose();
                }
                Channel_Model.Clear();
                Channel_Model = null;
            }
            // Unit_Model 정리
            if (Unit_Model != null)
            {
                Unit_Model.Clear();
                Unit_Model = null;
            }

            // Servo_Model 정리
            if (Servo_Model != null)
            {
                Servo_Model.Clear();
                Servo_Model = null;
            }
            // DisplayUI_Dio 정리
            if (DisplayUI_Dio != null)
            {
                DisplayUI_Dio.Clear();
                DisplayUI_Dio = null;
            }
            base.DisposeManaged();
        }
        #endregion
    }
}
