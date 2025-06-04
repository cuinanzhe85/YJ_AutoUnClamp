using Common.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Windows.Media.Imaging.Shapes;
using YJ_AutoUnClamp.Utils.EzMotion_E;
using static YJ_AutoUnClamp.Models.EziDio_Model;

namespace YJ_AutoUnClamp.Models
{
    public class EziDio_Model : BindableAndDisposable
    {
        public enum DI_MAP
        {
            // X000 ~ X00F
            FRONT_OP_EMERGENCY_FEEDBACK,    // X00
            OP_BOX_START,                   // X01
            OP_BOX_STOP,                    // X02
            OP_BOX_RESET,                   // X03
            DOOR_FEEDBACK,                  // X04
            REAR_OP_EMERGENCY_FEEDBACK,     // X05
            X006,X007,
            INTERFACE_FRONT_SAFETY,         // X08
            REAR_INTERFACE_1,               // X09
            REAR_INTERFACE_2,               // X0A
            X00B,
            FRONT_LEFT_DOOR,                // X0C
            FRONT_RIGHT_DOOR,               // X0D
            REAR_LEFT_DOOR,                 // X0E
            REAR_RIGHT_DOOR,                // X0F
            //X010~X01F
            IN_CV_DETECT_FIRST,             // X10
            IN_CV_DETECT_MID,               // X11
            IN_CV_DETECT_END,               // X12
            X013,
            UNLOAD_LD_Z_UNGRIP_CYL,        // X14
            UNLOAD_LD_Z_GRIP_CYL,          // X15
            UNLOAD_LD_Z_GRIP_DETECT,       // X16
            UNCLAMP_CV_DETECT,              // X17
            UNCLAMP_CV_CENTERING_FWD_CYL,         // X18
            UNCLAMP_CV_CENTERING_BWD_CYL,        // X19
            UNCLAMP_CV_UP_CYL,              // X1A
            UNCLAMP_CV_DOWN_CYL,            // X1B
            OUT_PP_LEFT_Z_UP_CYL,           // X1C
            OUT_PP_LEFT_Z_DOWN_CYL,         // X1D
            OUT_PP_LEFT_Z_GRIP_CYL,         // X1E
            OUT_PP_LEFT_Z_UNGRIP_CYL,       // X1F
            //X020~X02F 
            OUT_PP_LEFT_Z_GRIP_FINGER_F_CYL, // X20
            OUT_PP_LEFT_Z_GRIP_FINGER_R_CYL,// X21
            OUT_PP_TR_RIGHT_Z_UP_CYL,       // X22
            OUT_PP_TR_RIGHT_Z_DOWN_CYL,    // X23
            OUT_PP_TR_RIGHT_Z_VACUUM,       // X24
            X025,
            TOP_JIG_CV_DETECT_1,            // X26
            TOP_JIG_CV_DETECT_2,            // X27
            TOP_RETURN_X_LEFT_CYL,          // X28
            TOP_RETURN_X_RIGHT_CYL,         // X29
            TOP_RETURN_Z_UP,                // X2A
            TOP_RETURN_Z_DOWN,              // X2B
            TOP_RETURN_Z_GRIP,              // X2C
            TOP_RETURN_Z_UNGRIP,            // X2D
            BOTTOM_RETURN_X_LEFT,        // X2E
            BOTTOM_RETURN_X_RIGHT,       // X2F
            //X030~X03F
            BOTTOM_RETURN_Z_UP,             // X30
            BOTTOM_RETURN_Z_DOWN,           // X31
            BOTTOM_RETURN_Z_GRIP,           // X32
            BOTTOM_RETURN_Z_UNGRIP,         // X33
            LIFT_1_JIG_IN_1,                // X34
            LIFT_1_JIG_OUT_2,               // X35
            LIFT_2_JIG_IN_1,                // X36
            LIFT_2_JIG_OUT_2,               // X37
            LIFT_3_JIG_IN_1,                // X38
            LIFT_3_JIG_OUT_2,               // X39
            OUT_PP_RIGHT_TURN,              // X3A
            OUT_PP_RIGHT_RETURN,            // X3B
            RIGHT_L_DOOR,                   // X3C
            RIGHT_R_DOOR,                   // X3D
            X03E,X03F,      
            //X040~X04F
            AGING_CV_1_UPPER_INTERFACE,     // X40
            AGING_CV_1_LOW_INTERFACE,       // X41
            AGING_CV_2_UPPER_INTERFACE,     // X42
            AGING_CV_2_LOW_INTERFACE,       // X43
            AGING_CV_3_UPPER_INTERFACE,     // X44
            AGING_CV_3_LOW_INTERFACE,       // X45
            BOTTOM_RETURN_CV_INTERFACE,     // X46
            TOP_RETURN_CV_INTERFACE,        // X47
            UNLOAD_X_LEFT,                     // X48
            UNLOAD_X_RIGHT,                     // X49
            UNLOAD_Z_UP,                      // X4A
            UNLOAD_Z_DOWN,                    // X4B
            UNLOAD_Z_GRIP,                     // X4C
            UNLOAD_Z_UNGRIP,                   // X4D
            UNLOAD_BUFFER,                  // X4E
            X04F,
            DI_MAX
        }
        public enum DO_MAP
        {
            // Y000 ~ Y00F
            Y000,                               // Y000
            OP_BOX_START,                       // Y001
            OP_BOX_STOP,                        // Y002
            OP_BOX_RESET,                       // Y003
            TOWER_LAMP_GREEN,                   // Y004
            TOWER_LAMP_RED,                     // Y005
            TOWER_LAMP_YELLOW,                  // Y006
            BUZZER,                             // Y007
            INTERFACE_FRONT_MC_SEND,            // Y008
            INTERFACE_REAR_MC_SEND,             // Y009
            Y00A, Y00B, Y00C, Y00D, Y00E, Y00F,
            // Y010 ~ Y01F
            INPUT_LEFT_SET_CV_RUN,              // Y010
            UNCLAMP_CV_RUN,                     // Y011
            TOP_JIG_CV_RUN,                     // Y012
            LIFT_CV_RUN_1,                      // Y013
            LIFT_CV_RUN_2,                      // Y014
            LIFT_CV_RUN_3,                      // Y015
            Y16,Y17,
            UNLOAD_LD_Z_GRIP,                 // Y018
            UNCLAMP_CV_CENTERING,               // Y19
            UNCLAMP_CV_STOPPER_UP,                    // Y01A
            UNCLAMP_LEFT_Z_DOWN,                   // Y01B
            UNCLAMP_LEFT_Z_GRIP,                 // Y01C
            UNCLAMP_LEFT_Z_GRIP_F_FINGER,        // Y01D
            UNCLAMP_RIGHT_Z_DOWN,                // Y01E
            UNCLAMP_RIGHT_Z_VACUUM,              // Y01F
            // Y020 ~ Y02F
            TOP_RETURN_X_FWD,                   // Y020
            TOP_RETURN_Z_DOWN,                    // Y021
            TOP_RETURN_Z_GRIP,                  // Y022
            BOTTOM_RETURN_X_FWD,                // Y023
            BOTTOM_RETURN_Z_DOWN,                 // Y024
            BOTTOM_RETURN_Z_GRIP,               // Y025
            OUT_PP_RIGHT_Z_BLOW,                // Y026
            UNLOAD_X_FWD,                         // Y027
            UNLOAD_Z_DOWN,                          // Y028
            UNLOAD_Z_GRIP,                        // Y029
            OUT_PP_LEFT_Z_GRIP_R_FINGER,        // Y02A
            OUT_PP_RIGHT_TURN,                  // Y02B
            Y02C, Y02D,Y02E,Y02F,
            // Y030 ~ Y03F
            AGING_INVERT_CV_UPPER_INTERFACE_1,  // Y030
            AGING_INVERT_CV_LOW_INTERFASE_1,    // Y031
            AGING_INVERT_CV_UPPER_INTERFACE_2,  // Y032
            AGING_INVERT_CV_LOW_INTERFASE_2,    // Y033
            AGING_INVERT_CV_UPPER_INTERFACE_3,  // Y034
            AGING_INVERT_CV_LOW_INTERFASE_3,    // Y035
            BOTTOM_RETURN_CV_INTERFACE,         // Y036
            TOP_RETURN_CV_INTERFACE,            // Y037
            Y038, Y039, Y03A, Y03B,
            Y03C, Y03D, Y03E, Y03F,
            
            DO_MAX
        }
        public enum DisplayExist_List
        {
            UNCLAMP_SET,
            UNLOADING_BUFF,
            UNLOADING_Y,
            UNLOADING_X,
            UNCLAMP_TOP,
            RET_BOTTOM,
            RET_TOP,
            Max
        }
        public List<int> DisplayDio_List = new List<int>
        {
            (int)DI_MAP.UNLOAD_Z_UP,                    //0
            (int)DI_MAP.UNLOAD_Z_DOWN,                  //1
            (int)DI_MAP.UNLOAD_Z_GRIP,                  //2
            (int)DI_MAP.UNLOAD_Z_UNGRIP,                //3
            (int)DI_MAP.UNLOAD_LD_Z_GRIP_CYL,           //4
            (int)DI_MAP.UNLOAD_LD_Z_GRIP_DETECT,        //5
            (int)DI_MAP.OUT_PP_LEFT_Z_UP_CYL,           //6
            (int)DI_MAP.OUT_PP_LEFT_Z_DOWN_CYL,         //7
            (int)DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL,         //8
            (int)DI_MAP.OUT_PP_LEFT_Z_UNGRIP_CYL,       //9
            (int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_F_CYL,//10
            (int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_R_CYL,//11
            (int)DI_MAP.OUT_PP_TR_RIGHT_Z_UP_CYL,       //12
            (int)DI_MAP.OUT_PP_TR_RIGHT_Z_DOWN_CYL,     //13
            (int)DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM,       //14
            (int)DI_MAP.TOP_RETURN_Z_UP,                //15
            (int)DI_MAP.TOP_RETURN_Z_DOWN,              //16
            (int)DI_MAP.TOP_RETURN_Z_GRIP,              //17
            (int)DI_MAP.TOP_RETURN_Z_UNGRIP,            //18
            (int)DI_MAP.BOTTOM_RETURN_Z_UP,             //19
            (int)DI_MAP.BOTTOM_RETURN_Z_DOWN,           //20
            (int)DI_MAP.BOTTOM_RETURN_Z_GRIP,           //21
            (int)DI_MAP.BOTTOM_RETURN_Z_UNGRIP,         //22
            (int)DI_MAP.AGING_CV_1_UPPER_INTERFACE,     //23
            (int)DI_MAP.AGING_CV_1_LOW_INTERFACE,       //24
            (int)DI_MAP.AGING_CV_2_UPPER_INTERFACE,     //25
            (int)DI_MAP.AGING_CV_2_LOW_INTERFACE,       //26
            (int)DI_MAP.AGING_CV_3_UPPER_INTERFACE,     //27
            (int)DI_MAP.AGING_CV_3_LOW_INTERFACE,       //28
            (int)DI_MAP.BOTTOM_RETURN_CV_INTERFACE,     //29
            (int)DI_MAP.TOP_RETURN_CV_INTERFACE,        //30
            (int)DI_MAP.UNLOAD_BUFFER,                  //31
        };
        public enum Dio_Slave
        {
            DIO_1 = 8
        }
        private Thread DioThread;
        private bool IsDioThreadRunning { get; set; } = false;
        private volatile bool _shouldStop = false;
        private readonly Dictionary<DisplayExist_List, Func<bool>> DisplayExistMapping;
        public IPAddress IpAddress { get; set; }
        public bool[] IsConnected { get; set; } = { false, false, false, false, false, false, false, false, false, false, false, false, false };
        public int DioSlave { get; set; } = 1;
        public int Dio_InputCount { get; set; } = (int)DI_MAP.DI_MAX - 1;
        public int Dio_OutputCount { get; set; } = (int)DO_MAP.DO_MAX - 1;
        // Input Label
        public List<string> Input_Label { get; set; } = new List<string>();
        // Output Label
        public List<string> Output_Label { get; set; } = new List<string>();
        // Input Address
        public List<string> Input_Address { get; set; } = new List<string>();
        // Output Address
        public List<string> Output_Address { get; set; } = new List<string>();

        private ObservableCollection<bool> _DI_RAW_DATA = new ObservableCollection<bool>();
        public ObservableCollection<bool> DI_RAW_DATA
        {
            get
            {
                return _DI_RAW_DATA;
            }
        }
        private ObservableCollection<bool> _DO_RAW_DATA = new ObservableCollection<bool>();
        public ObservableCollection<bool> DO_RAW_DATA
        {
            get
            {
                return _DO_RAW_DATA;
            }
        }
        private ObservableCollection<bool> _DO_OPER_DATA = new ObservableCollection<bool>();
        public ObservableCollection<bool> DO_OPER_DATA
        {
            get { return _DO_OPER_DATA; }
        }
        
        public EziDio_Model()
        {
            // Data Init
            string label = string.Empty;
            for (int i = 0; i < (int)DI_MAP.DI_MAX; i++)
            {
                Input_Address.Add(string.Format("X{0:X2}", i));
                DI_RAW_DATA.Add(false);
                // set input label name
                label = ((DI_MAP)i).ToString().Replace('_', ' ');
                Input_Label.Add(label);
            }
            for (int i = 0; i < (int)DO_MAP.DO_MAX; i++)
            {
                Output_Address.Add(string.Format("Y{0:X2}", i));
                DO_RAW_DATA.Add(false);
                DO_OPER_DATA.Add(false);
                // set output label name
                label = ((DO_MAP)i).ToString().Replace('_', ' ');
                Output_Label.Add(label);
            }
            //Auto UI Display Dio
            DisplayExistMapping = new Dictionary<DisplayExist_List, Func<bool>>
            {
                { DisplayExist_List.UNCLAMP_SET, () => DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] }, // 32
                { DisplayExist_List.UNLOADING_BUFF, () => DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER] },         //33
                {//34
                    DisplayExist_List.UNLOADING_Y,
                    () =>
                        SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                            ? DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_CYL]
                            :!DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_CYL] && !DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_UNGRIP_CYL]
                },
                {//35
                    DisplayExist_List.UNLOADING_X,
                    () =>
                        SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                            ? DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_GRIP]
                            :!DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_GRIP] && !DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_UNGRIP]
                },

                {//36
                    DisplayExist_List.UNCLAMP_TOP,
                    () =>
                        SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                            ? DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL]
                            :!DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] && !DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_UNGRIP_CYL]
                },
                {//37
                    DisplayExist_List.RET_TOP,
                    () =>
                        SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                            ? DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_GRIP]
                            :!DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_GRIP] && !DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_UNGRIP]
                },
                {//38
                    DisplayExist_List.RET_BOTTOM,
                    () =>
                        SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                            ? DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_GRIP]
                            :!DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_GRIP] && !DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_UNGRIP]
                }
            };
        }
        public async Task<bool> Connect(int iSlaveNo)
        {
            try
            {
                int slave = (int)Dio_Slave.DIO_1 + iSlaveNo;
                // Already Connected
                if (IsConnected[iSlaveNo] == true)
                {
                    Close(iSlaveNo);
                    await Task.Delay(1000);
                }
                if (iSlaveNo == 0) IpAddress = IPAddress.Parse("192.168.0.8");
                if (iSlaveNo == 1) IpAddress = IPAddress.Parse("192.168.0.9");
                if (iSlaveNo == 2) IpAddress = IPAddress.Parse("192.168.0.10");
                if (iSlaveNo == 3) IpAddress = IPAddress.Parse("192.168.0.11");
                if (iSlaveNo == 4) IpAddress = IPAddress.Parse("192.168.0.12");

                // Is not 0 == Connect Success
                if (EziMOTIONPlusELib.FAS_ConnectTCP(IpAddress, slave) == true)
                {
                    IsConnected[iSlaveNo] = true;
                    Global.Mlog.Info($"EziMotion DIO Board Connect Success. IP Address : {IpAddress.ToString()}, Slave : {slave}");

                    // Thread Start
                    DioThread = new Thread(ThreadReceive);
                    DioThread.Start();
                    IsDioThreadRunning = true;
                    return true;
                }
                // Is 0 == Connect Fail
                else
                {
                    IsConnected[iSlaveNo] = false;
                    Global.Mlog.Info($"EziMotion DIO Board Connect Fail. IP Address : {IpAddress.ToString()}, Slave : {slave}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Global.ExceptionLog.Info(e.ToString());
                return false;
            }
        }
        public void Close(int iSlave)
        {
            try
            {
                int slave = (int)Dio_Slave.DIO_1 + iSlave;
                if (IsConnected[iSlave])
                {
                    EziMOTIONPlusELib.FAS_Close(slave);
                    IsConnected[iSlave] = false;
                    Global.Mlog.Info($"EziMotion DIO Board Disconnect Success. IP Address : {IpAddress}, Slave : {slave}");
                }

                if (DioThread != null)
                {
                    _shouldStop = true;  // 스레드 종료 신호 설정
                    if (!DioThread.Join(5000))  // 스레드가 종료될 때까지 최대 5초 대기
                    {
                        Global.Mlog.Info("DioThread did not terminate in a timely fashion.");
                    }
                    DioThread = null;
                    IsDioThreadRunning = false;
                }
            }
            catch (Exception e)
            {
                Global.ExceptionLog.Info(e.ToString());
            }
        }

        public void GetIO_InputData(int slave)
        {
            uint dwInput = 0;
            uint dwLatch = 0;
            int dioSlave = (int)Dio_Slave.DIO_1+ slave;
            int nRtn = EziMOTIONPlusELib.FAS_GetInput(dioSlave, ref dwInput, ref dwLatch);

            if (nRtn == EziMOTIONPlusELib.FMM_OK)
            {
                UpdateRawData(slave,DI_RAW_DATA, dwInput,false);
                UpdateUiDio();
            }
        }
        public uint GetIO_OutputData(int slave)
        {
            uint uOutput = 0;
            uint uStatus = 0;
            int dioSlave = (int)Dio_Slave.DIO_1 + slave;
            int nRtn = EziMOTIONPlusELib.FAS_GetOutput(dioSlave, ref uOutput, ref uStatus);

            if (nRtn == EziMOTIONPlusELib.FMM_OK)
            {
                UpdateRawData(slave, DO_RAW_DATA, uOutput, true);
                return uOutput;
            }
            return 0;
        }
        
        public void UpdateIO_OperData()
        {
            // DO_RAW_DATA와 DO_OPER_DATA를 비교하여 변경된 항목만 업데이트
            for (int i = 0; i < DO_RAW_DATA.Count; i++)
            {
                if (DO_OPER_DATA[i] != DO_RAW_DATA[i])
                {
                    DO_OPER_DATA[i] = DO_RAW_DATA[i];
                }
            }
        }
        public bool SetIO_OutputData(int index, bool OnOff)
        {
            UpdateIO_OperData();
            // IO Bit를 통해 Slave NO를 계산한다.
            int slave = (int)Dio_Slave.DIO_1+ index / 16;
            // 해당 Slave 의 제어해야할 bit No를 계산한다.
            int uOotBit = (int)index % 16;

            uint OnBit=0, OffBit=0;
            // 제어해야할 Bit를 기존 출력 중 IO정보와 합쳐서 Send buf에 담는다
            if (OnOff == true)
                OnBit = (uint)(1u << uOotBit);
            else if (OnOff == false)
                OffBit = (uint)(1u << uOotBit);

            uint onShift = OnBit << 16;
            uint offShift = OffBit << 16;
            int nRtn = EziMOTIONPlusELib.FAS_SetOutput(slave, onShift, offShift);

            if (nRtn != EziMOTIONPlusELib.FMM_OK)
            {
                Global.Mlog.Info("Function(SetIO_OutputData) was failed.");
                return false;
            }
            
            return true;
        }
        public void Set_HandlerInit()
        {
            SetIO_OutputData((int)DO_MAP.UNCLAMP_LEFT_Z_DOWN, false); 
            SetIO_OutputData((int)DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false); 
            SetIO_OutputData((int)DO_MAP.BOTTOM_RETURN_Z_DOWN, false); // Bottom Return up
            SetIO_OutputData((int)DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false); // Bottom Return up
            SetIO_OutputData((int)DO_MAP.TOP_RETURN_Z_DOWN, false);  // Top Return up
            SetIO_OutputData((int)DO_MAP.UNLOAD_Z_DOWN, false); Thread.Sleep(1000); // Unloading X Z Up
            SetIO_OutputData((int)DO_MAP.UNLOAD_X_FWD, false);// Unloading X Right
            SetIO_OutputData((int)DO_MAP.TOP_RETURN_X_FWD, false);  // Top  Left 
            SetIO_OutputData((int)DO_MAP.BOTTOM_RETURN_X_FWD, false);  // Top  Left 
        }
        public void ThreadReceive()
        {
            UpdateIO_OperData();
            while (!_shouldStop)
            {
                try
                {
                    for (int i = 0; i < (int)DI_MAP.DI_MAX / 16; i++)
                    {
                        // Get Input Data
                        GetIO_InputData(i);
                        Thread.Sleep(1);
                    }
                    for (int j = 0; j < (int)DO_MAP.DO_MAX / 16; j++)
                    {
                        // Get Output Data
                        GetIO_OutputData(j);
                        Thread.Sleep(1);
                    }
                }
                catch (Exception e)
                {
                    //IsConnected[iSlave] = false;
                    string error = e.ToString();
                    Global.ExceptionLog.ErrorFormat($"{System.Reflection.MethodBase.GetCurrentMethod().Name} - {error}");
                }

                Thread.Sleep(5);
            }
            // 스레드 종료 후 상태 업데이트
            IsDioThreadRunning = false;
            Global.Mlog.Info("DioThread has stopped.");
        }
        public void DioThreadStop()
        {
            // 스레드 종료 플래그 설정
            _shouldStop = true;

            // DioThread가 실행 중인지 확인
            if (DioThread != null && DioThread.IsAlive)
            {
                try
                {
                    // 스레드가 종료될 때까지 대기
                    DioThread.Join(500); // 최대 500ms 대기
                }
                catch (ThreadStateException ex)
                {
                    Global.ExceptionLog.ErrorFormat($"DioThreadStop - ThreadStateException: {ex}");
                }
                catch (ThreadInterruptedException ex)
                {
                    Global.ExceptionLog.ErrorFormat($"DioThreadStop - ThreadInterruptedException: {ex}");
                }
                finally
                {
                    // DioThread 객체를 null로 설정하여 참조 해제
                    DioThread = null;
                    IsDioThreadRunning = false;
                }
            }
            else
            {
                IsDioThreadRunning = false;
            }
        }
        private void UpdateRawData(int slave ,ObservableCollection<bool> rawData, uint data , bool InOutFlag)
        {
            int count = rawData.Count;
            int StartIndex = slave * 16;
            for (int i = 0; i < 16; i++)
            {
                bool newValue;
                if (InOutFlag == true)
                    newValue = ((data >> (i+16)) & 1u) == 1u;
                else
                    newValue = ((data >> i) & 1u) == 1u;
                if (rawData[StartIndex + i] != newValue)
                {
                    rawData[StartIndex + i] = newValue;
                }
            }
        }
        private void UpdateUiDio()
        {
            // LINQ를 사용하여 필요한 값만 추출
            for (int i = 0; i < DisplayDio_List.Count; i++)
            {
                if (SingletonManager.instance.DisplayUI_Dio[i] != DI_RAW_DATA[DisplayDio_List[i]])
                    SingletonManager.instance.DisplayUI_Dio[i] = DI_RAW_DATA[DisplayDio_List[i]];
            }

            // DisplayExistMapping을 사용하여 UI 업데이트
            foreach (var mapping in DisplayExistMapping)
            {
                int targetIndex = DisplayDio_List.Count + (int)mapping.Key;
                bool newValue = mapping.Value();
                if (SingletonManager.instance.DisplayUI_Dio[targetIndex] != newValue)
                {
                    SingletonManager.instance.DisplayUI_Dio[targetIndex] = newValue;
                }
            }
        }
    }
}
