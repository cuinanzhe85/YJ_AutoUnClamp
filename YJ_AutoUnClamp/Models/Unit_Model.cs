using Common.Managers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Windows.Data;
using static YJ_AutoUnClamp.Models.EziDio_Model;

namespace YJ_AutoUnClamp.Models
{
    public enum Direction
    {
        CW = 0,
        CCW = 1
    }
    public enum MotionUnit_List
    {
        In_Y,
        In_Z,
        Top_X,
        Lift_1,
        Lift_2,
        Lift_3,
        In_CV,
        Out_CV,
        Max
    }
    public enum ServoSlave_List
    {
        In_Y_Handler_Y,
        In_Z_Handler_Z,
        Top_X_Handler_X,
        Lift_1_Z,
        Lift_2_Z,
        Lift_3_Z,
        Max
    }
    public enum Lift_Index
    {
        Lift_1,
        Lift_2,
        Lift_3,
        Max
    }
    public enum Floor_Index
    {
        Floor_1,
        Floor_2,
        Floor_3,
        Floor_4,
        Floor_5,
        Max
    }
    
    public class Unit_Model
    {
        public MotionUnit_List UnitGroup { get; set; }
        public int UnitID { get; set; }
        public List<ServoSlave_List> ServoNames { get; set; }
        public Unit_Model(MotionUnit_List unit)
        {
            UnitGroup = unit;
            UnitID = (int)unit;
            ServoNames = new List<ServoSlave_List>();
        }
        private RadObservableCollection<Channel_Model> Channel_Model = SingletonManager.instance.Channel_Model;
        private Dictionary<string,double> Teaching_Data = SingletonManager.instance.Teaching_Data;
        private EziDio_Model Dio = SingletonManager.instance.Dio;
        private EzMotion_Model_E Ez_Model = SingletonManager.instance.Ez_Model;

        private bool _NoneSetTest = false; // Set Test Mode

        private bool _isLoopRunning = false;
        public bool UnloadYPutDownMoving=false;
        public bool UnclampBottomReturnDone = false;
        public int UnclmapVacuumRetry = 0;

        public string UnclampFailMessage = string.Empty;
        public int UnloadingLiftIndexY = 0;
        public int UnloadingLiftPickupIndex = 0;
        public int BarcodeRetry = 0;
        Stopwatch _TimeDelay = new Stopwatch(); 
        Stopwatch _ReturnBottomTimeDelay = new Stopwatch();
        Stopwatch _ReturnTopTimeDelay = new Stopwatch();
        Stopwatch _UnclampTimeDelay = new Stopwatch();
        Stopwatch _UnloadingCVTim= new Stopwatch(); 
        // Steps
        public enum UnClampHandStep
        {
            Idle,
            Rtn_Bottom_Handle_Check,
            Ready_Move_Done,
            UnClamping_Wait,
            Top_Hand_PickUp_Down_Check,
            Top_Hand_FR_Grip_Check,
            Top_Hand_Grip_Check,
            Top_Hand_Up_Check,
            Set_Hand_PickUp_Move_Check,
            Set_Hand_Down_Check,
            Set_Hand_Vacuum_Check,
            Set_Hand_Up_Check,
            Move_PutDown,
            Move_PutDown_Done_Check,
            Top_Hand_PutDown,
            Top_Hand_PutDown_Check,
            Top_Hand_UnGrip_Check,
            Set_Handler_Turn_Check,
            Top_Hand_PutDown_Up_Check,
            NFC_Data_Wait,
            Set_MES_Send,
            Set_MES_Result_Wait,
            Set_Hand_PutDown,
            set_Hand_PutDown_Check,
            Left_Z_Ungrip_Check,
            Set_Hand_PutDown_Up_Check
        }
        public enum ReturnBottomStep
        {
            Idle,
            Ready_Move_Check,
            Right_Move_Done,
            Bottom_Centering_BWD_Check,
            Hand_Down_Check,
            Grip_Check,
            Hand_Up_Check,
            Left_Move_Done,
            Clamp_IF_Return_Wait,
            PutDown_Down_Check,
            PutDown_UnGrip_Check,
            PutDown_Up_Check,
        }
        public enum ReturnTopStep
        {
            Idle,
            Top_Clamp_Arrival_Check,
            Hand_Down_Check,
            Grip_Check,
            Hand_Up_Check,
            Left_Move_Done,
            Clamp_Return_Wait,
            PutDown_Down_Check,
            UnGrip_Check,
            PutDown_Up_Check,
            Right_Move_Done
        }
        public enum Top_Out_CV
        {
            Idle,
            CV_Run_Condtion_Check,
            CV_Stop_Wait,
            CV_Stop
        }
        public enum Lift_Step
        {
            Idle,
            Clamp_IF_Wait,
            Lift_Input_Move_Donw,
            Lift_Down_Done,
            Lift_CV_Stop,
            UnloadPosMove,
            Lift_CV_Stop_Wait,
            Lift_Detect_Check,
            Aging_CV_Stop,
            Lift_Low_Move_Upper,
            Lift_Low_Move_Upper_Doe,
            Clamp_BarCode_Read,
            Clamp_BarCode_Read_Done,
            Clamp_InDate_Read,
            Clamp_InDate_Waite,
            Aging_Time_Check,
            Aging_Time_Check_Wait,
            Lift_UnlodingPosMove,
            Lift_UnlodingPos_Done

        }
        public enum Lift_Next_Low_Step
        {
            Idle,
            Interface_Check,
            Lift_Low_Move_Done,
            Interface_Off_Check
        }
        public enum UnClamp_CV_Step
        {
            Idle,
            CV_Run,
            CV_Stop,
            CV_Stop_Wait,
            Centering_Check
        }
        public enum Unload_CV_Step
        {
            Idle,
            CV_Run,
            CV_Stop,
            CV_ReRun
        }
        public enum Unload_X_Step
        {
            Idle,
            Left_Move,
            Left_Check,
            Left_Down_Check,
            Left_Grip_Check,
            Left_Up_Check,
            Right_Check,
            Right_Down_Check,
            Right_UnGrip_Check,
            Right_Up_Check
        }
        public enum Unload_Y_Step
        {
            Idle,
            Z_Ready_Check,
            PickUp_Stage_Check,
            Move_X_Pickup_Wait,
            Move_Y_Ready_Done,
            Move_Y_PickUp_Done,
            Move_Z_PickUp_Down_Done,
            Grip_Detect_Check,
            Grip_Check,
            Move_Z_Ready_Done,
            Move_Y_PutDown_Done,
            Move_Z_PutDown_Done,
            UnGrip_Check,
            Move_Z_PutDown_Up_Done,
        }
        public UnClampHandStep UnClampStep = UnClampHandStep.Idle;
        public ReturnBottomStep RtnBtmStep = ReturnBottomStep.Idle;
        public ReturnTopStep RtnTopStep = ReturnTopStep.Idle;
        public Top_Out_CV TopOutCVStep = Top_Out_CV.Idle;
        public Lift_Step LiftStep = Lift_Step.Idle;
        public UnClamp_CV_Step UnClampCvStep = UnClamp_CV_Step.Idle;
        public Unload_CV_Step UnloadCvStep = Unload_CV_Step.Idle;
        public Unload_X_Step UnloadXlStep = Unload_X_Step.Idle;
        public Unload_Y_Step UnloadYStep = Unload_Y_Step.Idle;
        public Lift_Next_Low_Step LiftNextLowStep = Lift_Next_Low_Step.Idle;
        public void Loop()
        {
            // Task.Delay를 사용하는경우 Loop 동작 확인후 리턴. 중복호출 방지
            if (_isLoopRunning)
                return;

            switch (UnitGroup)
            {
                case MotionUnit_List.Top_X:
                    UnClampXHandlerLogic();
                    ReturnBottomHandlerLogic();
                    ReturnTopHandlerLogic();
                    break;
                case MotionUnit_List.In_Y:
                    UnloadYHandlerLogic();
                    UnloadXLogic();
                    break;
                case MotionUnit_List.Lift_1:
                    LiftLogic();
                    //LiftNextLowMoveLogic();
                    break;
                case MotionUnit_List.In_CV:
                    UnClampCvLogic();
                    UnlodingCvLogic();
                    break;
                case MotionUnit_List.Out_CV:
                    TopOutCVLogic();
                    break;
            }
        }
        private void UnClampXHandlerLogic()
        {
            switch(UnClampStep)
            {
                case UnClampHandStep.Idle:
                    // Up상태가 아니면 Up한다.
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_DOWN_CYL] == true)
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_DOWN_CYL] == true)
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);

                    Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_VACUUM, false);
                    UnClampStep = UnClampHandStep.Rtn_Bottom_Handle_Check;

                    Global.Mlog.Info($"UnClampHandStep => Next Step : Rtn_Bottom_Handle_Check");
                    break;
                case UnClampHandStep.Rtn_Bottom_Handle_Check:
                    // Return Bottom Handler 위치가 left 일때 X handler Ready위치로 이동
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_X_RIGHT] != true
                        && Dio.DO_RAW_DATA[(int)DO_MAP.BOTTOM_RETURN_X_FWD] == false
                        && UnclampBottomReturnDone == false)
                    {
                        Global.Mlog.Info($"UnClampHandStep => Ready_Move_Done");
                        Global.Mlog.Info($"UnClampHandStep => X : L Pickup Pos Move");
                        if (Ez_Model.MoveUnClampLeftPickupPosX()==true)
                        {
                            UnClampStep = UnClampHandStep.Ready_Move_Done;
                            Global.Mlog.Info($"UnClampHandStep => Next Step : Ready_Move_Done");
                        }
                    }
                    break;
                case UnClampHandStep.Ready_Move_Done:
                    // Ready위치 도착 확인
                    if (Ez_Model.IsMoveUnClampLeftPickupDoneX()==true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => X : L Pickup Pos Move Done");
                        UnClampStep = UnClampHandStep.UnClamping_Wait;

                        Global.Mlog.Info($"UnClampHandStep => Next Step : UnClamping_Wait");
                    }
                    break;
                case UnClampHandStep.UnClamping_Wait:
                    // Clamp가 진입하면 Top Hand Down (도착 센서 및 Centering 전지 확인)
                    if ((Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true 
                        && Dio.DO_RAW_DATA[(int)DO_MAP.UNCLAMP_CV_RUN] == false)
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_CV_CENTERING, true);
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, true);
                        UnClampStep = UnClampHandStep.Top_Hand_PickUp_Down_Check;

                        Global.Mlog.Info($"UnClampHandStep => Left Z Down");
                        Global.Mlog.Info($"UnClampHandStep => CV Centering FWD");
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Top_Hand_PickUp_Down_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_PickUp_Down_Check:
                    // Top Hand Down 상태확인 완료 후 Grip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_DOWN_CYL] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_CENTERING_BWD_CYL] != true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_GRIP_F_FINGER, true);
                        Dio_Output(DO_MAP.OUT_PP_LEFT_Z_GRIP_R_FINGER, true);
                        UnClampStep = UnClampHandStep.Top_Hand_FR_Grip_Check;

                        Global.Mlog.Info($"UnClampHandStep => F_FINGER Grip");
                        Global.Mlog.Info($"UnClampHandStep => R_FINGER Grip");
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Top_Hand_PickUp_Down_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_FR_Grip_Check:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_F_CYL] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_R_CYL] == true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_GRIP, true);
                        UnClampStep = UnClampHandStep.Top_Hand_Grip_Check;

                        Global.Mlog.Info($"UnClampHandStep => Left Z Grip");
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Top_Hand_Grip_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_Grip_Check:
                    // Top Hand Grip 완료후 Up
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] == true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);
                        UnClampStep = UnClampHandStep.Top_Hand_Up_Check;

                        Global.Mlog.Info($"UnClampHandStep => Left Z Up");
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Top_Hand_Up_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_Up_Check:
                    // Up 완료 후 Set Hand PickUP 위치 이동 & Turn
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_UP_CYL] == true)
                    {
                        Dio_Output(DO_MAP.OUT_PP_RIGHT_TURN, true);
                        Global.Mlog.Info($"UnClampHandStep => Right Handler Turn");

                        Global.Mlog.Info($"UnClampHandStep => Unclmap Right Pickup Pos X");
                        if (Ez_Model.MoveUnclmapRightPickUpPosX()==true)
                            UnClampStep = UnClampHandStep.Set_Hand_PickUp_Move_Check;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_PickUp_Move_Check");
                    }
                    break;
                case UnClampHandStep.Set_Hand_PickUp_Move_Check:
                    // Set Handler PickUp위치 도착 및 Turn 완료확인 후  Down
                    if (Ez_Model.IsMoveUnclampRightPickUpDoneX()==true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_RIGHT_RETURN] == true
                        )
                    {
                        Global.Mlog.Info($"UnClampHandStep => Right Z Down");
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, true);

                        UnClampStep = UnClampHandStep.Set_Hand_Down_Check;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_Down_Check");
                    }
                    break;
                case UnClampHandStep.Set_Hand_Down_Check:
                    // Down 완료하면 Vacuum On
                    /***************************************************/
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_DOWN_CYL] == true)
                    {
                        if (_NoneSetTest == false)
                            Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_VACUUM, true);
                        Global.Mlog.Info($"UnClampHandStep => Vacuum On");

                        UnClampStep = UnClampHandStep.Set_Hand_Vacuum_Check;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_Vacuum_Check");
                    }
                    break;
                case UnClampHandStep.Set_Hand_Vacuum_Check:
                    // Vacuum 확인후 Up
                    /***************************************************/
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] == true
                        || _NoneSetTest == true
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);

                        Global.Mlog.Info($"UnClampHandStep => Right Z UP");
                        UnClampStep = UnClampHandStep.Set_Hand_Up_Check;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_Up_Check");
                    }
                    else
                    {
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);
                        UnClampStep = UnClampHandStep.Set_Hand_Up_Check;
                    }
                        break;
                case UnClampHandStep.Set_Hand_Up_Check:
                    // Set hand up완료 후 Put down위치로 이동 & hand Return
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_UP_CYL] == true)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] != true
                        && _NoneSetTest == false)
                        {
                            if (UnclmapVacuumRetry < 1)
                            {
                                UnclmapVacuumRetry++;
                                UnClampStep = UnClampHandStep.Set_Hand_PickUp_Move_Check;
                                break;
                            }
                            else
                            {
                                UnclmapVacuumRetry = 0;
                                UnclampFailMessage = "SET Vacuum Pickup Fail.Remove the product and start again.\r\nSET Vacuum 실패, SET 제거 후 재 시작하세요.";
                                Global.Mlog.Info($"UnClampHandStep => SET Vacuum Pickup Fail");
                                UnClampStep = UnClampHandStep.Move_PutDown;
                                Global.Mlog.Info($"UnClampHandStep => Next Step : Move_PutDown");
                                break;
                            }
                        }

                        SingletonManager.instance.Channel_Model[0].CnNomber = "--";
                        SingletonManager.instance.Channel_Model[0].MesResult = "--";

                        SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].NfcData = string.Empty;
                        SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].IsReceived = false;

                        UnclmapVacuumRetry = 0;
                        Global.Mlog.Info($"UnClampHandStep => Right Turn");
                        // Interface CV에 set가 없으면 turn. nfc scaner 
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.REAR_INTERFACE_1] == false)
                        {
                            Dio_Output(DO_MAP.OUT_PP_RIGHT_TURN, false);
                        }

                        Global.Mlog.Info($"UnClampHandStep => Unclamp Move PutDown Pos X");
                        if (Ez_Model.MoveUnclampPutDownPosX()==true)
                        {
                            // PutDown으로 이동할때 Return Bottom Handler Pickup 돌수있도록 수정
                            UnclampBottomReturnDone = true;
                            Global.Mlog.Info($"UnClampHandStep => UnclampBottomReturnDone = true");
                            UnClampStep = UnClampHandStep.Move_PutDown_Done_Check;
                            Global.Mlog.Info($"UnClampHandStep => Next Step : Move_PutDown_Done_Check");
                        }
                    }
                    break;
                case UnClampHandStep.Move_PutDown:
                    Global.Mlog.Info($"UnClampHandStep => Unclamp Move PutDown Pos X");
                    if (Ez_Model.MoveUnclampPutDownPosX() == true)
                    {
                        // PutDown으로 이동할때 Return Bottom Handler Pickup 돌수있도록 수정
                        UnclampBottomReturnDone = true;
                        Global.Mlog.Info($"UnClampHandStep => UnclampBottomReturnDone = true");
                        UnClampStep = UnClampHandStep.Move_PutDown_Done_Check;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Move_PutDown_Done_Check");
                    }
                    break;
                case UnClampHandStep.Move_PutDown_Done_Check:
                    // putdown위치 도착 확인 후 top hand cv에 clamp가 있는지 확인 한다.
                    if (Ez_Model.IsMoveUnclampPutDownDoneX()==true)
                    {
                        if ((Dio.DI_RAW_DATA[(int)DI_MAP.REAR_INTERFACE_1] == false)
                        || _NoneSetTest == true
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                        {
                            Dio_Output(DO_MAP.OUT_PP_RIGHT_TURN, false);
                            Global.Mlog.Info($"UnClampHandStep => Next Step : Top_Hand_PutDown");
                            UnClampStep = UnClampHandStep.Set_Handler_Turn_Check;
                        }
                        else
                        {
                            SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].NfcData = string.Empty;
                            SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].IsReceived = false;
                        }
                    }
                    break;
                case UnClampHandStep.Set_Handler_Turn_Check:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_RIGHT_TURN] == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => NFC Use : {SingletonManager.instance.SystemModel.NfcUseNotUse}");
                        // SET가 없으면 MES Skip한다.(Vacuum error 발생시 수동 SET 회수한 상황일때 발생)
                        if (SingletonManager.instance.SystemModel.NfcUseNotUse == "Use"
                            && (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_VACUUM] == true
                            && _NoneSetTest == false))
                        {
                            UnClampStep = UnClampHandStep.NFC_Data_Wait;
                            _UnclampTimeDelay.Restart();
                            Global.Mlog.Info($"UnClampHandStep => NFC_Data_Wait");
                        }
                        else
                        {
                            SingletonManager.instance.Channel_Model[0].CnNomber = "--";
                            SingletonManager.instance.Channel_Model[0].MesResult = "Not Use";
                            UnClampStep = UnClampHandStep.Set_Hand_PutDown;
                            Global.Mlog.Info($"UnClampHandStep => Set_Hand_PutDown");
                        }
                    }
                    break;
                case UnClampHandStep.NFC_Data_Wait:
                    int delay = 1000;
                    if (!string.IsNullOrEmpty(SingletonManager.instance.SystemModel.NfcDelay))
                        delay = (int)Convert.ToInt32(SingletonManager.instance.SystemModel.NfcDelay);

                    if (_UnclampTimeDelay.ElapsedMilliseconds > delay)
                    {
                        UnClampStep = UnClampHandStep.Set_MES_Send;
                    }
                    else if (SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Mes].IsReceived == true)
                    {
                        UnClampStep = UnClampHandStep.Set_MES_Send;
                    }
                    break;
                case UnClampHandStep.Set_MES_Send:
                    //if (!string.IsNullOrEmpty(SingletonManager.instance.Nfc_Data))
                    string nfc = SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].NfcData;
                    Global.Mlog.Info($"UnClampHandStep => NFC Data : {nfc}");
                    if (!string.IsNullOrEmpty(nfc))
                    {
                        SingletonManager.instance.Channel_Model[0].CnNomber = nfc;
                        SingletonManager.instance.Channel_Model[0].MesResult = "--";
                        Global.instance.MES_LOG(nfc, "Send");
                        //SingletonManager.instance.HttpJsonModel.SendRequest("saveInspInfo", nfc, "PASS");
                        SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Mes].SendMes(nfc);
                        UnClampStep = UnClampHandStep.Set_MES_Result_Wait;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_MES_Result_Wait");
                        _UnclampTimeDelay.Restart();
                    }
                    else
                    {
                        Global.Mlog.Info($"UnClampHandStep => NFC Data is Empty");
                        UnclampFailMessage = "NFC Data is Empty";
                        SingletonManager.instance.Channel_Model[0].CnNomber = "--";
                        SingletonManager.instance.Channel_Model[0].MesResult = "Empty";
                        UnClampStep = UnClampHandStep.Set_Hand_PutDown;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_PutDown");
                    }
                    break;
                case UnClampHandStep.Set_MES_Result_Wait:
                    if (SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Mes].IsReceived == true)
                    {
                        nfc = SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].NfcData;
                        Global.Mlog.Info($"UnClampHandStep => MES Reveive : {SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Mes].MesResult}");
                        if (SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Mes].MesResult == "OK")
                        {
                            SingletonManager.instance.Channel_Model[0].CnNomber = nfc;
                            SingletonManager.instance.Channel_Model[0].MesResult = "OK";
                            Global.Mlog.Info($"UnClampHandStep => MES Result : OK ({nfc})");
                            Global.instance.MES_LOG(nfc, "OK");
                        }
                        else
                        {
                            SingletonManager.instance.Channel_Model[0].CnNomber = nfc;
                            SingletonManager.instance.Channel_Model[0].MesResult = "NG";

                            Global.Mlog.Info($"UnClampHandStep => MES Result : NG ({nfc})");
                            UnclampFailMessage = $"C/N : {nfc}\r\nMES Result : NG ";
                            Global.instance.MES_LOG(nfc, "NG");
                        }
                        UnClampStep = UnClampHandStep.Set_Hand_PutDown;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_PutDown");
                    }
                    else if (_UnclampTimeDelay.ElapsedMilliseconds > 15000)
                    {
                        nfc = SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].NfcData;
                        SingletonManager.instance.Channel_Model[0].CnNomber = nfc;
                        SingletonManager.instance.Channel_Model[0].MesResult = "TIMEOUT";
                        Global.instance.MES_LOG(nfc, "TIMEOUT");

                        UnclampFailMessage = $"C/N : {nfc}\r\nMES Result Receive Timeout.";
                        UnClampStep = UnClampHandStep.Set_Hand_PutDown;
                        Global.Mlog.Info($"UnClampHandStep => MES : TIMEOUT ({nfc})");
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Idle");
                    }
                    if (_UnclampTimeDelay.ElapsedMilliseconds == 0)
                        _UnclampTimeDelay.Restart();
                    break;
                case UnClampHandStep.Set_Hand_PutDown:
                    // set putdown Return 되있으면 down
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_RIGHT_TURN] == true
                        && (Dio.DI_RAW_DATA[(int)DI_MAP.REAR_INTERFACE_1] == false || _NoneSetTest == true)
                        && Dio.DI_RAW_DATA[(int)DI_MAP.TOP_JIG_CV_DETECT_1] == false)
                    {
                        Global.Mlog.Info($"UnClampHandStep => SET Rirht PutDown Z Down");
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, true);
                        Global.Mlog.Info($"UnClampHandStep => SET Left PutDown Z Down");
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, true);

                        UnClampStep = UnClampHandStep.set_Hand_PutDown_Check;
                        Global.Mlog.Info($"UnClampHandStep => set_Hand_PutDown_Check");
                    }
                    break;
                case UnClampHandStep.set_Hand_PutDown_Check:
                    // set hand down완료후 blow on
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_DOWN_CYL] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_DOWN_CYL] == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => SET Right Z Vacuum Off");
                        Global.Mlog.Info($"UnClampHandStep => SET Right Z BLOW On");
                        Global.Mlog.Info($"UnClampHandStep => Right Z UP");
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_VACUUM, false);
                        Dio_Output(DO_MAP.OUT_PP_RIGHT_Z_BLOW, true);
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);

                        Global.Mlog.Info($"UnClampHandStep => Left Ungrip");
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_GRIP_F_FINGER, false);
                        Dio_Output(DO_MAP.OUT_PP_LEFT_Z_GRIP_R_FINGER, false);
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_GRIP, false);

                        Global.instance.TactTimeStart = false;
                        Global.Mlog.Info($"Tacttime : {SingletonManager.instance.Channel_Model[0].TactTime}");
                        Global.TTlog.Info($"Tacttime : {SingletonManager.instance.Channel_Model[0].TactTime}");

                        Global.Mlog.Info($"UnClampHandStep => Next Step : Left_Z_Ungrip_Check");
                        UnClampStep = UnClampHandStep.Left_Z_Ungrip_Check;
                    }
                    break;
                case UnClampHandStep.Left_Z_Ungrip_Check:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_F_CYL] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_R_CYL] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] == false)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);
                        UnClampStep = UnClampHandStep.Set_Hand_PutDown_Up_Check;
                        Global.Mlog.Info($"UnClampHandStep => Unclamp Left Z Up");

                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_PutDown_Up_Check");
                    }
                    break;
                case UnClampHandStep.Set_Hand_PutDown_Up_Check:
                    // up완료하면 Idel로 이동한다.
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_UP_CYL] == true 
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_UP_CYL] == true)
                    {
                        // Unclamp Count 증가
                        Global.instance.UnLoadCountPlus();
                        // Tacttime 시작
                        Global.instance.UnLoadingTactTimeStart();
                        Global.instance.TactTimeStart = true;

                        Global.Mlog.Info($"UnClampHandStep => SET Right Z BLOW Off");
                        Dio_Output(DO_MAP.OUT_PP_RIGHT_Z_BLOW, false);
                        UnClampStep = UnClampHandStep.Idle;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Idle");
                    }
                    break;
                
            }
            int step = (int)UnClampStep;
            Global.instance.Write_Sequence_Log("UNCLAMP_STEP", step.ToString());
        }
        public void ReturnBottomHandlerLogic()
        {
            switch(RtnBtmStep)
            {
                case ReturnBottomStep.Idle:
                    // Putdown 위치가 아니면 put down으로 이동 시킨다
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_X_RIGHT] == false)
                        Dio_Output(DO_MAP.BOTTOM_RETURN_X_FWD, false);
                    RtnBtmStep = ReturnBottomStep.Ready_Move_Check;
                    Global.Mlog.Info($"ReturnBottomStep => Ready_Move_Check");
                    break;
                case ReturnBottomStep.Ready_Move_Check:
                    // put down위치에서 clamp 도착신호 on& top hand put down위치에 있으면 Pickup 위치 이동
                    // cv에 다시 들어온 clamp임을 확인하기 위하여 centring 전진상태를 확인한다.
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true
                        && Ez_Model.IsMoveUnclampRightPickUpDoneX() != true
                        && UnclampBottomReturnDone == true)
                    {
                        Global.Mlog.Info($"ReturnBottomStep => UnclampBottomReturnDone : {UnclampBottomReturnDone.ToString()}");
                        Global.Mlog.Info($"ReturnBottomStep => X is not Right Pickup Position");
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_DOWN] == true)
                            Dio_Output(DO_MAP.BOTTOM_RETURN_Z_DOWN, false);
                        Dio_Output(DO_MAP.BOTTOM_RETURN_X_FWD, true);
                        Global.Mlog.Info($"ReturnBottomStep => Bottom Retutn X Move Right");

                        RtnBtmStep = ReturnBottomStep.Right_Move_Done;
                        Global.Mlog.Info($"ReturnBottomStep => Right_Move_Done");
                    }
                    if (SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                        &&Ez_Model.IsMoveUnclampRightPickUpDoneX() != true
                        && UnclampBottomReturnDone == true)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_DOWN] == true)
                            Dio_Output(DO_MAP.BOTTOM_RETURN_Z_DOWN, false);
                        Dio_Output(DO_MAP.BOTTOM_RETURN_X_FWD, true);

                        RtnBtmStep = ReturnBottomStep.Right_Move_Done;
                    }
                    break;
                case ReturnBottomStep.Right_Move_Done:
                    // pick up위치 도착하면 Bottom centering bwd
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_X_RIGHT] == true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_CV_CENTERING, false);
                        Global.Mlog.Info($"ReturnBottomStep => CV Centering BWD");
                        RtnBtmStep = ReturnBottomStep.Bottom_Centering_BWD_Check;
                        Global.Mlog.Info($"ReturnBottomStep => Bottom_Centering_BWD_Check");
                    }
                    break;
                case ReturnBottomStep.Bottom_Centering_BWD_Check:
                    // centering 후진 확인 후 hand down
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_CENTERING_BWD_CYL] == true)
                    {
                        Dio_Output(DO_MAP.BOTTOM_RETURN_Z_DOWN, true);
                        Global.Mlog.Info($"ReturnBottomStep => Z Down");
                        Global.Mlog.Info($"ReturnBottomStep => Hand_Down_Check");
                        RtnBtmStep = ReturnBottomStep.Hand_Down_Check;
                    }
                    break;
                case ReturnBottomStep.Hand_Down_Check:
                    // down 완료 후 Grip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_DOWN] == true)
                    {
                        Dio_Output(DO_MAP.BOTTOM_RETURN_Z_GRIP, true);
                        Global.Mlog.Info($"ReturnBottomStep => Z Grip");
                        RtnBtmStep = ReturnBottomStep.Grip_Check;
                        Global.Mlog.Info($"ReturnBottomStep => Grip_Check");
                    }
                    break;
                case ReturnBottomStep.Grip_Check:
                    // grip 확인 후 up
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_GRIP] == true)
                    {
                        Dio_Output(DO_MAP.BOTTOM_RETURN_Z_DOWN, false);
                        Global.Mlog.Info($"ReturnBottomStep => Z Up");
                        RtnBtmStep = ReturnBottomStep.Hand_Up_Check;
                        Global.Mlog.Info($"ReturnBottomStep => Hand_Up_Check");
                    }
                    break;
                case ReturnBottomStep.Hand_Up_Check:
                    // Up 완료하면 Put down 위치 이동 
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_UP] == true)
                    {
                        Global.Mlog.Info($"ReturnBottomStep => Bottom X Move Left");
                        Dio_Output(DO_MAP.BOTTOM_RETURN_X_FWD, false);
                        RtnBtmStep = ReturnBottomStep.Left_Move_Done;
                        Global.Mlog.Info($"ReturnBottomStep => Left_Move_Done");
                        _ReturnBottomTimeDelay.Restart();
                    }
                    break;
                case ReturnBottomStep.Left_Move_Done:
                    // Return Bottom Left 이동 시작 하고 200ms 뒤 X Handler Pickup 이동하도록 한다.
                    if (_ReturnBottomTimeDelay.ElapsedMilliseconds > 300)
                    {
                        UnclampBottomReturnDone = false;
                        Global.Mlog.Info($"ReturnBottomStep => UnclampBottomReturnDone : {UnclampBottomReturnDone.ToString()}");
                    }
                    // put down위치 도착하면 clamp장비한데 Interface신호 on
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_X_LEFT] == true)
                    {
                        UnclampBottomReturnDone = false;

                        Global.Mlog.Info($"ReturnBottomStep => Interface Send On");
                        Dio_Output(DO_MAP.BOTTOM_RETURN_CV_INTERFACE, true);
                        RtnBtmStep = ReturnBottomStep.Clamp_IF_Return_Wait;
                        Global.Mlog.Info($"ReturnBottomStep => Clamp_IF_Return_Wait");

                        _ReturnBottomTimeDelay.Restart();
                    }
                    break;
                case ReturnBottomStep.Clamp_IF_Return_Wait:
                    // clamp장비 Interface 응답 들어오면 cv에 제품있는지 확인하고 Down
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_CV_INTERFACE] == true
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        Dio_Output(DO_MAP.BOTTOM_RETURN_Z_DOWN, true);
                        Global.Mlog.Info($"ReturnBottomStep => Bottom CV Z PutDown");

                        RtnBtmStep = ReturnBottomStep.PutDown_Down_Check;
                        Global.Mlog.Info($"ReturnBottomStep => PutDown_Down_Check");
                    }
                    //else if (_ReturnBottomTimeDelay.ElapsedMilliseconds > 5000)
                    //{
                    //    UnclampFailMessage = "Return Bottom Conveyor Putdown Time out";
                    //    _ReturnBottomTimeDelay.Reset();
                    //}
                    //else if (_ReturnBottomTimeDelay.ElapsedMilliseconds == 0)
                    //    _ReturnBottomTimeDelay.Restart();
                    break;
                case ReturnBottomStep.PutDown_Down_Check:
                    // Down완료 후 Ungrip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_DOWN] == true)
                    {
                        Global.Mlog.Info($"ReturnBottomStep => Ungrip");
                        Dio_Output(DO_MAP.BOTTOM_RETURN_Z_GRIP, false);
                        RtnBtmStep = ReturnBottomStep.PutDown_UnGrip_Check;
                        Global.Mlog.Info($"ReturnBottomStep => PutDown_UnGrip_Check");
                    }
                    break;
                case ReturnBottomStep.PutDown_UnGrip_Check:
                    // up완료 후 Interface off
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_GRIP] == false)
                    {
                        Global.Mlog.Info($"ReturnBottomStep => Z Up");
                        Dio_Output(DO_MAP.BOTTOM_RETURN_Z_DOWN, false);
                        RtnBtmStep = ReturnBottomStep.PutDown_Up_Check;
                        Global.Mlog.Info($"ReturnBottomStep => PutDown_Up_Check");
                    }
                    break;
                case ReturnBottomStep.PutDown_Up_Check:
                    // up완료 후 Interface off
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_UP] == true)
                    {
                        Dio_Output(DO_MAP.BOTTOM_RETURN_CV_INTERFACE, false);
                        Global.Mlog.Info($"ReturnBottomStep => Interface Send Off");
                        RtnBtmStep = ReturnBottomStep.Idle;
                    }
                    break;
            }
            int step = (int)RtnBtmStep;
            Global.instance.Write_Sequence_Log("RTN_BTM_STEP", step.ToString());
            Global.instance.Write_Sequence_Log("RTN_BTM_DONE", UnclampBottomReturnDone.ToString());
        }
        public void ReturnTopHandlerLogic()
        {
            switch(RtnTopStep)
            {
                case ReturnTopStep.Idle:
                    // Right 위치가 아니면 Right 이동 시킨다
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_DOWN] == true)
                        Dio_Output(DO_MAP.TOP_RETURN_Z_DOWN, false);
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_X_LEFT_CYL] == true)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_X_FWD, true);
                    }
                    RtnTopStep = ReturnTopStep.Top_Clamp_Arrival_Check;
                    break;
                case ReturnTopStep.Top_Clamp_Arrival_Check:
                    // hand가 right위치에 있고 Clamp가 진입되있으면 down
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_X_RIGHT_CYL] == true
                        && (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_JIG_CV_DETECT_2] == true 
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry))
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_Z_DOWN, true);

                        RtnTopStep = ReturnTopStep.Hand_Down_Check;
                    }
                    else if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_X_RIGHT_CYL] != true)
                        Dio_Output(DO_MAP.TOP_RETURN_X_FWD, true);
                    break;
                case ReturnTopStep.Hand_Down_Check:
                    // down완료 후 grip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_DOWN] == true)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_Z_GRIP, true);

                        RtnTopStep = ReturnTopStep.Grip_Check;
                    }
                    break;
                case ReturnTopStep.Grip_Check:
                    // grip 완료후 up
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_GRIP] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_UNGRIP] == false)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_Z_DOWN, false);

                        RtnTopStep = ReturnTopStep.Hand_Up_Check;
                    }
                    break;
                case ReturnTopStep.Hand_Up_Check:
                    // up 완료 후 left로 이동
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_UP] == true)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_X_FWD, false);

                        RtnTopStep = ReturnTopStep.Left_Move_Done;
                    }
                    break;
                case ReturnTopStep.Left_Move_Done:
                    // Put down위치에 도착하면 Clamp에게 Interface on
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_X_LEFT_CYL] == true)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_CV_INTERFACE, true);

                        RtnTopStep = ReturnTopStep.Clamp_Return_Wait;
                        _ReturnTopTimeDelay.Restart();
                    }
                    break;
                case ReturnTopStep.Clamp_Return_Wait:
                    // clmap interface 응답오면 cv에 제품있는지 확인하고 down한다.
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_CV_INTERFACE] == true
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_Z_DOWN, true);
                        RtnTopStep = ReturnTopStep.PutDown_Down_Check;
                    }
                    //else if (_ReturnTopTimeDelay.ElapsedMilliseconds > 5000)
                    //{
                    //    UnclampFailMessage = "Return Top Conveyor Putdown Time out";
                    //    _ReturnTopTimeDelay.Reset();
                    //}
                    //else if (_ReturnTopTimeDelay.ElapsedMilliseconds == 0)
                    //    _ReturnTopTimeDelay.Restart();
                    break;
                case ReturnTopStep.PutDown_Down_Check:
                    // down 완료후 ungrip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_DOWN] == true)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_Z_GRIP, false);
                        RtnTopStep = ReturnTopStep.UnGrip_Check;
                    }
                    break;
                case ReturnTopStep.UnGrip_Check:
                    // ungrip완료 후 up
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_UNGRIP] == true)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_Z_DOWN, false);

                        RtnTopStep = ReturnTopStep.PutDown_Up_Check;
                    }
                    break;
                case ReturnTopStep.PutDown_Up_Check:
                    // up완료하면 Interface off하고 right로 이동
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_Z_UP] == true)
                    {
                        Dio_Output(DO_MAP.TOP_RETURN_CV_INTERFACE, false);
                        Dio_Output(DO_MAP.TOP_RETURN_X_FWD, true);
                        RtnTopStep = ReturnTopStep.Right_Move_Done;
                    }
                    break;
                case ReturnTopStep.Right_Move_Done:
                    // right 위치 도착 확인하면서 interface off 되였는지 확인하고 될때까지 보내고 다음 step으로 지나간다.
                    //if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_CV_INTERFACE] == false
                     if( Dio.DI_RAW_DATA[(int)DI_MAP.TOP_RETURN_X_RIGHT_CYL] == true)
                    {
                        RtnTopStep = ReturnTopStep.Idle;
                    }
                    break;

            }
            int step = (int)RtnTopStep;
            Global.instance.Write_Sequence_Log("RTN_TOP_STEP", step.ToString());
        }
        private void TopOutCVLogic()
        {
            switch (TopOutCVStep)
            {
                case Top_Out_CV.Idle:
                    
                    TopOutCVStep = Top_Out_CV.CV_Run_Condtion_Check;
                    break;
                case Top_Out_CV.CV_Run_Condtion_Check:
                    // cv 끝단 clamp없면 cv run 
                    // Top Clamp도착& Top Hand Up상태이면
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_JIG_CV_DETECT_1] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.TOP_JIG_CV_DETECT_2] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_UP_CYL] == true)
                    {
                        Dio_Output(DO_MAP.TOP_JIG_CV_RUN, true);

                        TopOutCVStep = Top_Out_CV.CV_Stop_Wait;
                    }
                    break;
                case Top_Out_CV.CV_Stop_Wait:
                    // Clamp  cv 끝에 도착하면 stop
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_JIG_CV_DETECT_2] == true)
                    {
                        _TimeDelay.Restart();

                        TopOutCVStep = Top_Out_CV.CV_Stop;
                    }
                    break;
                case Top_Out_CV.CV_Stop:
                    // Clamp  cv 끝에 도착하면 stop
                    if (_TimeDelay.ElapsedMilliseconds > 100)
                    {
                        Dio_Output(DO_MAP.TOP_JIG_CV_RUN, false);

                        TopOutCVStep = Top_Out_CV.Idle;
                    }
                    break;
            }
            int step = (int)TopOutCVStep;
            Global.instance.Write_Sequence_Log("TOP_OUT_CV_STEP", step.ToString());
        }
        public void LiftLogic()
        {
            switch(LiftStep)
            {
                case Lift_Step.Idle:
                    LiftStep = Lift_Step.Clamp_IF_Wait;
                    Dio_Output(DO_MAP.LIFT_CV_RUN_1, false);
                    Dio_Output(DO_MAP.LIFT_CV_RUN_2, false);
                    Dio_Output(DO_MAP.LIFT_CV_RUN_3, false);
                    Global.Mlog.Info($"Lift_Step => Next Step : Clamp_IF_Wait");
                    break;
                case Lift_Step.Clamp_IF_Wait:
                    // Clamp Interface 기다린다
                    //if (Ez_Model.IsMoveReadyPosY() == true)
                    {
                        // Up 1
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_UPPER_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] != true)
                        {
                            UnloadingLiftPickupIndex = 0;
                            if (Ez_Model.MoveMoveLiftUpperPos(UnloadingLiftPickupIndex) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_1_UPPER_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {UnloadingLiftPickupIndex.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Upper Position");
                                Global.Mlog.Info($"Lift_Step => Next Step : Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Input_Move_Donw;
                            }
                        }
                        // Low1
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_LOW_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] != true)
                        {
                            UnloadingLiftPickupIndex = 0;
                            if (Ez_Model.MoveMoveLiftLowPos(UnloadingLiftPickupIndex) == true)
                            {
                                if (Dio.DO_RAW_DATA[(int)DO_MAP.TOWER_LAMP_RED] == false)
                                    Global.instance.Set_TowerLamp(Global.TowerLampType.Start);
                                Global.Mlog.Info($"Lift_Step => AGING_CV_1_LOW_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {UnloadingLiftPickupIndex.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Low Position");
                                Global.Mlog.Info($"Lift_Step => Next Step : Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Down_Done;
                                _TimeDelay.Restart();
                            }
                        }
                        // Up2
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_UPPER_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] != true)
                        {
                            UnloadingLiftPickupIndex = 1;
                            if (Ez_Model.MoveMoveLiftUpperPos(UnloadingLiftPickupIndex) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_2_UPPER_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {UnloadingLiftPickupIndex.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Upper Position");
                                Global.Mlog.Info($"Lift_Step => Next Step : Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Input_Move_Donw;
                            }
                        }
                        // Low2
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_LOW_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] != true)
                        {
                            UnloadingLiftPickupIndex = 1;
                            if (Ez_Model.MoveMoveLiftLowPos(UnloadingLiftPickupIndex) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_2_LOW_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {UnloadingLiftPickupIndex.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Low Position");
                                Global.Mlog.Info($"Lift_Step => Next Step : Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Down_Done;
                                _TimeDelay.Restart();
                            }
                        }
                        // Up3
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_UPPER_INTERFACE] == true
                           && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] != true)
                        {
                            UnloadingLiftPickupIndex = 2;
                            if (Ez_Model.MoveMoveLiftUpperPos(UnloadingLiftPickupIndex) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_3_UPPER_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {UnloadingLiftPickupIndex.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Upper Position");
                                Global.Mlog.Info($"Lift_Step => Next Step : Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Input_Move_Donw;
                            }
                        }
                        //Low3
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_LOW_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] != true)
                        {
                            UnloadingLiftPickupIndex = 2;
                            if (Ez_Model.MoveMoveLiftLowPos(UnloadingLiftPickupIndex) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_3_LOW_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {UnloadingLiftPickupIndex.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Low Position");
                                Global.Mlog.Info($"Lift_Step => Next Step : Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Down_Done;
                                _TimeDelay.Restart();
                            }
                        } 
                    }
                    break;
                case Lift_Step.Lift_Input_Move_Donw:
                    if (Ez_Model.IsMoveLiftUpperDone(UnloadingLiftPickupIndex) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => Lift Move Upper Position Done");
                        Global.Mlog.Info($"Lift_Step => Lift {UnloadingLiftPickupIndex.ToString()} CV On");
                        // Lift CV On
                        Dio_Output(DO_MAP.LIFT_CV_RUN_1 + UnloadingLiftPickupIndex, true);

                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_UPPER_INTERFACE] == true)
                        {
                            Dio_Output(DO_MAP.AGING_INVERT_CV_UPPER_INTERFACE_1, true);
                            Global.Mlog.Info($"Lift_Step => AGING_INVERT_CV_UPPER_INTERFACE_1 On");
                        }
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_UPPER_INTERFACE] == true)
                        {
                            Dio_Output(DO_MAP.AGING_INVERT_CV_UPPER_INTERFACE_2, true);
                            Global.Mlog.Info($"Lift_Step => AGING_INVERT_CV_UPPER_INTERFACE_2 On");
                        }
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_UPPER_INTERFACE] == true)
                        {
                            Dio_Output(DO_MAP.AGING_INVERT_CV_UPPER_INTERFACE_3, true);
                            Global.Mlog.Info($"Lift_Step => AGING_CV_3_UPPER_INTERFACE On");
                        }
                        LiftStep = Lift_Step.Lift_CV_Stop;
                        Global.Mlog.Info($"Lift_Step => Next Step : Lift_CV_Stop");
                    }
                    break;
                case Lift_Step.Lift_Down_Done:
                    // 1층도착하였으면 Interface On 하고 CV Run
                    if (Ez_Model.IsMoveLiftLowDone(UnloadingLiftPickupIndex) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => Lift Move Low Position Done");
                        Global.Mlog.Info($"Lift_Step => Lift {(UnloadingLiftPickupIndex + 1)} CV On");
                        // Lift CV On
                        Dio_Output(DO_MAP.LIFT_CV_RUN_1 + UnloadingLiftPickupIndex, true);

                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_LOW_INTERFACE] == true)
                        {
                            Dio_Output(DO_MAP.AGING_INVERT_CV_LOW_INTERFASE_1, true);
                            Global.Mlog.Info($"Lift_Step => AGING_INVERT_CV_LOW_INTERFASE_1 On");
                        }
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_LOW_INTERFACE] == true)
                        {
                            Dio_Output(DO_MAP.AGING_INVERT_CV_LOW_INTERFASE_2, true);
                            Global.Mlog.Info($"Lift_Step => AGING_INVERT_CV_LOW_INTERFASE_2 On");
                        }
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_LOW_INTERFACE] == true)
                        {
                            Dio_Output(DO_MAP.AGING_INVERT_CV_LOW_INTERFASE_3, true);
                            Global.Mlog.Info($"Lift_Step => AGING_INVERT_CV_LOW_INTERFASE_3 On");
                        }
                        LiftStep = Lift_Step.Lift_CV_Stop;
                        Global.Mlog.Info($"Lift_Step => Next Step : Lift_CV_Stop");
                    }
                    else if (_TimeDelay.ElapsedMilliseconds >15000)
                    {
                        UnclampFailMessage = $"Lift_Step => Lift {(UnloadingLiftPickupIndex + 1)} Low Down Time out.";
                        Global.Mlog.Info(UnclampFailMessage);
                        LiftStep = Lift_Step.Clamp_IF_Wait;
                    }
                    break;
                case Lift_Step.Lift_CV_Stop:
                    // Lift CV의 진입 완료신호 받으면 Interface Off  Lift Cv Stop
                    if (UnloadingLiftPickupIndex == (int)Lift_Index.Lift_1)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] == true)
                        {
                            _TimeDelay.Restart();
                            LiftStep = Lift_Step.Lift_CV_Stop_Wait;

                            Global.Mlog.Info($"Lift_Step => Lift 1 Detect Sensor On");
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_CV_Stop_Wait");
                        }
                    }
                    else if (UnloadingLiftPickupIndex == (int)Lift_Index.Lift_2)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] == true)
                        {
                            _TimeDelay.Restart();
                            LiftStep = Lift_Step.Lift_CV_Stop_Wait;
                            Global.Mlog.Info($"Lift_Step => Lift 2 Detect Sensor On");
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_CV_Stop_Wait");
                        }
                    }
                    else if (UnloadingLiftPickupIndex == (int)Lift_Index.Lift_3)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] == true)
                        {
                            _TimeDelay.Restart();
                            LiftStep = Lift_Step.Lift_CV_Stop_Wait;
                            Global.Mlog.Info($"Lift_Step => Lift 3 Detect Sensor On");
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_CV_Stop_Wait");
                        }
                    }
                    break;
                case Lift_Step.Lift_CV_Stop_Wait:
                    if (_TimeDelay.ElapsedMilliseconds > 1000)
                    {
                        // Lift CV On
                        Dio_Output(DO_MAP.LIFT_CV_RUN_1 + UnloadingLiftPickupIndex, false);
                        Global.Mlog.Info($"Lift_Step => Lift {(UnloadingLiftPickupIndex).ToString()} CV Off");
                        
                        LiftStep = Lift_Step.Lift_Detect_Check;
                        Global.Mlog.Info($"Lift_Step => Next Step : Lift_Detect_Check");
                    }
                    else if (_TimeDelay.ElapsedMilliseconds ==0)
                    {
                        _TimeDelay.Restart();  
                    }
                    break;
                case Lift_Step.Lift_Detect_Check:
                    if (UnloadingLiftPickupIndex == (int)Lift_Index.Lift_1
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_IN_1] == true)
                    {
                        UnclampFailMessage = $"Lift-1 In Detect check";
                        break;
                    }
                    else if (UnloadingLiftPickupIndex == (int)Lift_Index.Lift_2
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_IN_1] == true)
                    {
                        UnclampFailMessage = $"Lift-2 In Detect check";
                        break;
                    }
                    else if (UnloadingLiftPickupIndex == (int)Lift_Index.Lift_3
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_IN_1] == true)
                    {
                        UnclampFailMessage = $"Lift-3 In Detect check";
                        break;
                    }
                    LiftStep = Lift_Step.Aging_CV_Stop;
                    Global.Mlog.Info($"Lift_Step => Next Step : Aging_CV_Stop");
                    break;
                case Lift_Step.Aging_CV_Stop:
                    // Interfa off
                    Global.Mlog.Info($"Lift_Step => Lift {(UnloadingLiftPickupIndex).ToString()} Interface Off");
                    LiftInterfaceOnOff(UnloadingLiftPickupIndex, false);

                    if (Ez_Model.IsMoveLiftLowDone(UnloadingLiftPickupIndex) == true)
                    {
                        if (SingletonManager.instance.SystemModel.BcrUseNotUse == "Use")
                        {
                            LiftStep = Lift_Step.Lift_Low_Move_Upper;
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_Low_Move_Upper");
                        }
                        else
                        {
                            LiftStep = Lift_Step.Lift_UnlodingPosMove;
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_UnlodingPosMove");
                        }
                    }
                    else
                    {
                        Global.Mlog.Info($"Lift_Step => Barcode Use : {SingletonManager.instance.SystemModel.BcrUseNotUse}");
                        if (SingletonManager.instance.SystemModel.BcrUseNotUse == "Use")
                        {
                            LiftStep = Lift_Step.Clamp_BarCode_Read;
                            Global.Mlog.Info($"Lift_Step => Next Step : Clamp_BarCode_Read");
                        }
                        else
                        {
                            LiftStep = Lift_Step.Lift_UnlodingPosMove;
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_UnlodingPosMove");
                        }
                    }
                    break;
                case Lift_Step.Lift_Low_Move_Upper:

                     Ez_Model.MoveMoveLiftUpperPos(UnloadingLiftPickupIndex);
                     LiftStep = Lift_Step.Lift_Low_Move_Upper_Doe;
                     Global.Mlog.Info($"Lift_Step => Next Step : Lift_Low_Move_Upper_Doe");
                    break;
                case Lift_Step.Lift_Low_Move_Upper_Doe:
                    if (Ez_Model.IsMoveLiftUpperDone(UnloadingLiftPickupIndex) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => Barcode Use : {SingletonManager.instance.SystemModel.BcrUseNotUse}");
                        if (SingletonManager.instance.SystemModel.BcrUseNotUse == "Use")
                        {
                            LiftStep = Lift_Step.Clamp_BarCode_Read;
                            Global.Mlog.Info($"Lift_Step => Next Step : Clamp_BarCode_Read");
                        }
                        else
                        {
                            LiftStep = Lift_Step.Lift_UnlodingPosMove;
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_UnlodingPosMove");
                        }
                    }
                    break;
                case Lift_Step.Clamp_BarCode_Read:

                    Global.Mlog.Info($"Lift_Step => Barcode Trig Send");
                    SingletonManager.instance.SerialModel[UnloadingLiftPickupIndex].SendBcrTrig();
                    LiftStep = Lift_Step.Clamp_BarCode_Read_Done;
                    Global.Mlog.Info($"Lift_Step => Next Step : Clamp_BarCode_Read_Done");
                    _TimeDelay.Restart();
                    break;
                case Lift_Step.Clamp_BarCode_Read_Done:
                    if (_TimeDelay.ElapsedMilliseconds < 1000)
                    {
                        if (SingletonManager.instance.SerialModel[UnloadingLiftPickupIndex].IsReceived == true)
                        {
                            Global.Mlog.Info($"Lift_Step => Barcode Read OK");
                            Global.Mlog.Info($"Lift_Step => Barcode : {SingletonManager.instance.SerialModel[UnloadingLiftPickupIndex].Barcode}");
                            LiftStep = Lift_Step.Clamp_InDate_Read;
                            Global.Mlog.Info($"Lift_Step => Next Step : Clamp_InDate_Read");
                        }
                    }
                    else
                    {
                        if (BarcodeRetry < 3)
                        {
                            BarcodeRetry++;
                            Global.Mlog.Info($"Lift_Step => Barcode Read Retry");
                            LiftStep = Lift_Step.Clamp_BarCode_Read;
                        }
                        else
                        {
                            BarcodeRetry = 0;
                            UnclampFailMessage = $"Barcode Read NG [Lift {UnloadingLiftPickupIndex + 1}]";
                            LiftStep = Lift_Step.Clamp_BarCode_Read;
                        }
                    }
                    break;
                case Lift_Step.Clamp_InDate_Read:
                    if (SingletonManager.instance.IsTcpConnected == true)
                    {
                        Global.Mlog.Info($"Lift_Step => TCP Barcode Send");
                        string barcode = SingletonManager.instance.SerialModel[UnloadingLiftPickupIndex].Barcode;
                        SingletonManager.instance.TcpClient.TcpSendMessage(barcode);
                        LiftStep = Lift_Step.Clamp_InDate_Waite;

                        SingletonManager.instance.Channel_Model[UnloadingLiftPickupIndex].Barcode = barcode;
                        Global.Mlog.Info($"Lift_Step => Next Step : Clamp_InDate_Waite");
                        _TimeDelay.Restart();
                    }
                    else
                    {
                        BarcodeRetry = 0;
                        UnclampFailMessage = $"Clamp <-> Unclmap TCP is Disconnected.";
                        LiftStep = Lift_Step.Clamp_BarCode_Read;
                    }
                    break;
                case Lift_Step.Clamp_InDate_Waite:
                    if (_TimeDelay.ElapsedMilliseconds < 1000)
                    {
                        if (SingletonManager.instance.TcpClient.TcpReceiveData != "")
                        {
                            SingletonManager.instance.Channel_Model[UnloadingLiftPickupIndex].Barcode += (" : " + SingletonManager.instance.TcpClient.TcpReceiveData);
                            LiftStep = Lift_Step.Aging_Time_Check;
                            Global.Mlog.Info($"Lift_Step => Next Step : Aging_Time_Check");
                        }
                    }
                    else
                    {
                        LiftStep = Lift_Step.Clamp_InDate_Read;
                    }
                    break;
                case Lift_Step.Aging_Time_Check:
                    if (SingletonManager.instance.AgingModel.AgingTimeCheck(UnloadingLiftPickupIndex) == true)
                    {
                        LiftStep = Lift_Step.Lift_UnlodingPosMove;
                        Global.Mlog.Info($"Lift_Step => Next Step : Lift_UnlodingPosMove");
                    }
                    else
                    {
                        _TimeDelay.Restart();
                        LiftStep = Lift_Step.Aging_Time_Check_Wait;
                        Global.Mlog.Info($"Lift_Step => Next Step : Aging_Time_Check_Wait");
                    }
                    break;
                case Lift_Step.Aging_Time_Check_Wait:
                    if (_TimeDelay.ElapsedMilliseconds > 60000)
                    {
                        LiftStep = Lift_Step.Clamp_BarCode_Read;
                    }
                    break;
                case Lift_Step.Lift_UnlodingPosMove:
                    
                    ///if (Ez_Model.IsMoveReadyPosY() == true)
                    {
                        //Global.Mlog.Info($"Lift_Step => IsMoveReadyPosY Done");
                        // Lift가 이미 Unloading 위치로 이동
                        if (Ez_Model.MoveMoveLiftUnloadingPos(UnloadingLiftPickupIndex) == true)
                        {
                            Global.Mlog.Info($"Lift_Step => Lift {(UnloadingLiftPickupIndex).ToString()} Move Loding Position");
                            LiftStep = Lift_Step.Lift_UnlodingPos_Done;
                            Global.Mlog.Info($"Lift_Step => Next Step : Lift_UnlodingPos_Done");
                        }
                    }
                    break;
                case Lift_Step.Lift_UnlodingPos_Done:
                    // Lift 2층 도착 확인한다.
                    if (Ez_Model.IsMoveLiftUnloadingDone(UnloadingLiftPickupIndex) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => IsMoveLiftUnloadingDone Done");
                        SingletonManager.instance.UnLoadFloor[UnloadingLiftPickupIndex] = (int)Floor_Index.Max;
                        for (int i = 0; i < (int)Floor_Index.Max; i++)
                            SingletonManager.instance.Display_Lift[UnloadingLiftPickupIndex].Floor[i] = true;
                        LiftStep = Lift_Step.Idle;
                        Global.Mlog.Info($"Lift_Step => Next Step : Idle");
                    }
                    break;
                
            }
            int step = (int)LiftStep;
            Global.instance.Write_Sequence_Log("LIFT_STEP", step.ToString());
            Global.instance.Write_Sequence_Log($"LIFT_STAGE", UnloadingLiftPickupIndex.ToString());

        }
        private void LiftNextLowMoveLogic()
        {
            switch (LiftNextLowStep)
            {
                case Lift_Next_Low_Step.Idle:
                    LiftNextLowStep = Lift_Next_Low_Step.Interface_Check;
                    break;
                case Lift_Next_Low_Step.Interface_Check:
                    // Low1
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_LOW_INTERFACE] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] != true)
                    {
                        if (Ez_Model.MoveMoveLiftLowPos(0) == true)
                        {
                            LiftNextLowStep = Lift_Next_Low_Step.Lift_Low_Move_Done;
                        }
                    }
                    // Low2
                    else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_LOW_INTERFACE] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] != true)
                    {
                        if (Ez_Model.MoveMoveLiftLowPos(1) == true)
                        {
                            LiftNextLowStep = Lift_Next_Low_Step.Lift_Low_Move_Done;
                        }
                    }
                    // Low3
                    else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_LOW_INTERFACE] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] != true)
                    {
                        if (Ez_Model.MoveMoveLiftLowPos(2) == true)
                        {
                            LiftNextLowStep = Lift_Next_Low_Step.Lift_Low_Move_Done;
                        }
                    }
                    break;
                case Lift_Next_Low_Step.Lift_Low_Move_Done:
                    // Low1
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_LOW_INTERFACE] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] != true)
                    {
                        if (Ez_Model.IsMoveLiftLowDone(0) == true)
                        {
                            LiftNextLowStep = Lift_Next_Low_Step.Interface_Off_Check;
                        }
                    }
                    // Low2
                    else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_LOW_INTERFACE] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] != true)
                    {
                        if (Ez_Model.IsMoveLiftLowDone(1) == true)
                        {
                            LiftNextLowStep = Lift_Next_Low_Step.Interface_Off_Check;
                        }
                    }
                    // Low3
                    else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_LOW_INTERFACE] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] != true)
                    {
                        if (Ez_Model.IsMoveLiftLowDone(2) == true)
                        {
                            LiftNextLowStep = Lift_Next_Low_Step.Interface_Off_Check;
                        }
                    }
                    break;
                case Lift_Next_Low_Step.Interface_Off_Check:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_LOW_INTERFACE] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_LOW_INTERFACE] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_LOW_INTERFACE] == false)
                    {
                        LiftNextLowStep = Lift_Next_Low_Step.Idle;
                    }
                    break;
            }
        }
        private void UnClampCvLogic()
        {
            switch(UnClampCvStep)
            {
                case UnClamp_CV_Step.Idle:
                    UnClampCvStep = UnClamp_CV_Step.CV_Run;
                    Dio_Output(DO_MAP.UNCLAMP_CV_STOPPER_UP, true);
                    _TimeDelay.Restart();
                    break;
                case UnClamp_CV_Step.CV_Run:
                    // 제품 없으면 cv run
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_CENTERING_BWD_CYL] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == true)
                    {
                        if (Dio.DO_RAW_DATA[(int)DO_MAP.TOWER_LAMP_RED] == false)
                            Global.instance.Set_TowerLamp(Global.TowerLampType.Start);

                        Dio_Output(DO_MAP.UNCLAMP_CV_RUN, true);

                        UnClampCvStep = UnClamp_CV_Step.CV_Stop;
                    }
                    else if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_CENTERING_BWD_CYL] != true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_CV_CENTERING, false);
                    }
                    else if (_TimeDelay.ElapsedMilliseconds > 60000)
                    {
                         _TimeDelay.Reset();
                        if (Dio.DO_RAW_DATA[(int)DO_MAP.TOWER_LAMP_YELLOW] == false)
                            Global.instance.Set_TowerLamp(Global.TowerLampType.Stop);

                        Global.instance.TactTimeStart = false;
                        SingletonManager.instance.Channel_Model[0].TactTime = "0.0";
                    }
                    break;
                case UnClamp_CV_Step.CV_Stop:
                    // 도착 센서가 들어오면 cv stop하고 centering전진
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true)
                    {
                        _TimeDelay.Restart();
                        UnClampCvStep = UnClamp_CV_Step.CV_Stop_Wait;
                    }
                    else if (Dio.DO_RAW_DATA[(int)DO_MAP.UNCLAMP_CV_RUN] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == true)
                        UnClampCvStep = UnClamp_CV_Step.Idle;
                    break;
                case UnClamp_CV_Step.CV_Stop_Wait:
                    if (_TimeDelay.ElapsedMilliseconds > 200)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_CV_RUN, false);

                        UnClampCvStep = UnClamp_CV_Step.Idle;
                    }
                    if (_TimeDelay.ElapsedMilliseconds == 0)
                        _TimeDelay.Restart();
                    break;
            }
            int step = (int)UnClampCvStep;
            Global.instance.Write_Sequence_Log("UNCLAMP_CV_STEP", step.ToString());
        }
        public void UnlodingCvLogic()
        {
            switch (UnloadCvStep)
            {
                case Unload_CV_Step.Idle:
                    UnloadCvStep = Unload_CV_Step.CV_Run;
                    break;
                case Unload_CV_Step.CV_Run:
                    // In X가 Right위치에서 Down상태가 아니면
                     // Unclamp cv 제품 있고 CV end 에 제품없으면 run
                    if ((Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == false || Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == false )
                        && (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == false || Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_UP] == true)
                        && SingletonManager.instance.IsInspectionStart == true)
                    {
                        Dio_Output(DO_MAP.INPUT_LEFT_SET_CV_RUN, true);
                        UnloadCvStep = Unload_CV_Step.CV_Stop;
                        _UnloadingCVTim.Restart();
                    }
                    break;
                case Unload_CV_Step.CV_Stop:
                    // unclamp cv에 제품이 있으고 & cv end 신호가 들어오면 stop
                    // X Right위치에서 Down상태이면 stop
                    if ((Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == true)
                        || (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == true && Dio.DO_RAW_DATA[(int)DO_MAP.UNLOAD_Z_DOWN] == true)
                        || (SingletonManager.instance.IsInspectionStart == false && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == true))
                    {
                        Dio_Output(DO_MAP.INPUT_LEFT_SET_CV_RUN, false);
                        UnloadCvStep = Unload_CV_Step.Idle;
                        break;
                    }
                    // 1분 구동후 센서에 제품감지가 되지 않으면 멈추고 clamp 대기한다.
                    else if (_UnloadingCVTim.ElapsedMilliseconds > 60000
                        && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] != true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_FIRST] != true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] != true)
                    {
                        Dio_Output(DO_MAP.INPUT_LEFT_SET_CV_RUN, false);
                        UnloadCvStep = Unload_CV_Step.CV_ReRun;
                    }
                    break;
                case Unload_CV_Step.CV_ReRun:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == true
                       ||Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_FIRST] == true)
                    {
                        UnloadCvStep = Unload_CV_Step.Idle;
                    }
                    break;
            }
            int step = (int)UnloadCvStep;
            Global.instance.Write_Sequence_Log("UNLOAD_CV_STEP", step.ToString());
        }
        private void UnloadXLogic()
        {
            switch(UnloadXlStep)
            {
                case Unload_X_Step.Idle:
                    // right위치가 이니고 Up되여 있지 않으면 UP하고 rightr오 리동 시킨다
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_DOWN] == true)
                        Dio_Output(DO_MAP.UNLOAD_Z_DOWN, false);
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_LEFT] == true)
                        Dio_Output(DO_MAP.UNLOAD_X_FWD, false);
                    UnloadXlStep = Unload_X_Step.Left_Move;
                    break;
                case Unload_X_Step.Left_Move:
                    // clamp도착이 센서가 들어오고
                    // y hand가  Ready위치 이거나 Lift 1,2,3 에 있으면 Left 이동
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER] == true
                        && Ez_Model.IsUnloadSafetyPosY() == true
                         && UnloadYPutDownMoving == false)
                    {
                        Dio_Output(DO_MAP.UNLOAD_X_FWD, true);
                        UnloadXlStep = Unload_X_Step.Left_Check;
                    }
                    else if (SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                        && Ez_Model.IsUnloadSafetyPosY() == true
                        && UnloadYPutDownMoving == false)
                    {
                        Dio_Output(DO_MAP.UNLOAD_X_FWD, true);
                        UnloadXlStep = Unload_X_Step.Left_Check;
                    }
                    break;
                case Unload_X_Step.Left_Check:
                    // Left도착후 Down
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_LEFT] == true)
                    {
                        Dio_Output(DO_MAP.UNLOAD_Z_DOWN, true);

                        UnloadXlStep = Unload_X_Step.Left_Down_Check;
                    }
                    break;
                case Unload_X_Step.Left_Down_Check:
                    // Down완료후 Grip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_DOWN] == true)
                    {
                        Dio_Output(DO_MAP.UNLOAD_Z_GRIP, true);

                        UnloadXlStep = Unload_X_Step.Left_Grip_Check;
                    }
                    break;
                case Unload_X_Step.Left_Grip_Check:
                    // Grip 완료 후 up
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_GRIP] == true)
                    {
                        Dio_Output(DO_MAP.UNLOAD_Z_DOWN, false);
                        UnloadXlStep = Unload_X_Step.Left_Up_Check;
                    }
                    break;
                case Unload_X_Step.Left_Up_Check:
                    // Up완료 후 Right 이동
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_UP] == true)
                    {
                        Dio_Output(DO_MAP.UNLOAD_X_FWD, false);
                        UnloadXlStep = Unload_X_Step.Right_Check;
                    }
                    break;
                case Unload_X_Step.Right_Check:
                    //Right 도착 후 CV putdown위치 제품 없으면 Down
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_FIRST] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_MID] == false)
                    {
                        Dio_Output(DO_MAP.UNLOAD_Z_DOWN, true);
                        UnloadXlStep = Unload_X_Step.Right_Down_Check;
                    }
                    break;
                case Unload_X_Step.Right_Down_Check:
                    // down 확인 후 ungrip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_DOWN] == true)
                    {
                        Dio_Output(DO_MAP.UNLOAD_Z_GRIP, false);
                        UnloadXlStep = Unload_X_Step.Right_UnGrip_Check;
                    }
                    break;
                case Unload_X_Step.Right_UnGrip_Check:
                    // ungrip확인 후 up
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_UNGRIP] == true)
                    {
                        Dio_Output(DO_MAP.UNLOAD_Z_DOWN, false);
                        UnloadXlStep = Unload_X_Step.Right_Up_Check;
                    }
                    break;
                case Unload_X_Step.Right_Up_Check:
                    // u
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_UP] == true)
                        UnloadXlStep = Unload_X_Step.Idle;
                    break;
            }
            int step = (int)UnloadXlStep;
            Global.instance.Write_Sequence_Log("UNLOAD_X_STEP", step.ToString());
        }
        private void UnloadYHandlerLogic()
        {
            switch(UnloadYStep)
            {
                case Unload_Y_Step.Idle:
                    // Z ready 위치가 아니면 ready로 이동시킨다
                    if (Ez_Model.IsMoveReadyPosZ() == false)
                        Ez_Model.MoveReadyPosZ();
                    UnloadYStep = Unload_Y_Step.Z_Ready_Check;
                    break;
                case Unload_Y_Step.Z_Ready_Check:
                    if (Ez_Model.IsMoveReadyPosZ() == true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMoveReadyPosZ Done");
                        Global.Mlog.Info($"Unload_Y_Step => PickUp_Stage_Check");
                        if( Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
                            Dio_Output(DO_MAP.UNLOAD_LD_Z_GRIP, false);
                        UnloadYStep = Unload_Y_Step.PickUp_Stage_Check;
                    }
                    break;
                case Unload_Y_Step.PickUp_Stage_Check:
                    // Pickup 중이면 작업 이어서 진행
                    if (UnloadingLiftIndexY == 0)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] == true && Ez_Model.IsMoveLiftUnloadingDone(UnloadingLiftIndexY) == true)
                        {
                            if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] == 0)
                                SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] = (int)Floor_Index.Max;
                            Global.Mlog.Info($"Unload_Y_Step => {UnloadingLiftIndexY.ToString()}");
                            Global.Mlog.Info($"Unload_Y_Step => {SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY].ToString()}");
                            Global.Mlog.Info($"Unload_Y_Step => MovePickUpPosY");
                            Ez_Model.MovePickUpPosY(UnloadingLiftIndexY);
                            UnloadYStep = Unload_Y_Step.Move_Y_PickUp_Done;

                            Global.Mlog.Info($"Unload_Y_Step => Move_Y_PickUp_Done");
                        }
                        else
                        {
                            UnloadYPutDownMoving = false;
                            UnloadingLiftIndexY++;
                            if (Ez_Model.IsMovePickUpPosY(2) == false)
                                Ez_Model.MovePickUpPosY(2);
                            UnloadYStep = Unload_Y_Step.Move_Y_Ready_Done;
                        }
                    }
                    else if (UnloadingLiftIndexY == 1)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] == true && Ez_Model.IsMoveLiftUnloadingDone(UnloadingLiftIndexY) == true)
                        {
                            if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] == 0)
                                SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] = (int)Floor_Index.Max;
                            Global.Mlog.Info($"Unload_Y_Step => {UnloadingLiftIndexY.ToString()}");
                            Global.Mlog.Info($"Unload_Y_Step => {SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY].ToString()}");
                            Global.Mlog.Info($"Unload_Y_Step => MovePickUpPosY");
                            Ez_Model.MovePickUpPosY(UnloadingLiftIndexY);
                            UnloadYStep = Unload_Y_Step.Move_Y_PickUp_Done;

                            Global.Mlog.Info($"Unload_Y_Step => Move_Y_PickUp_Done");
                        }
                        else
                        {
                            UnloadYPutDownMoving = false;
                            UnloadingLiftIndexY++;
                            if (Ez_Model.IsMovePickUpPosY(2) == false)
                                Ez_Model.MovePickUpPosY(2);
                            UnloadYStep = Unload_Y_Step.Move_Y_Ready_Done;
                        }
                    }
                    else if (UnloadingLiftIndexY == 2)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] == true && Ez_Model.IsMoveLiftUnloadingDone(UnloadingLiftIndexY) == true)
                        {
                            if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] == 0)
                                SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] = (int)Floor_Index.Max;
                            Global.Mlog.Info($"Unload_Y_Step => {UnloadingLiftIndexY.ToString()}");
                            Global.Mlog.Info($"Unload_Y_Step => {SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY].ToString()}");
                            Global.Mlog.Info($"Unload_Y_Step => MovePickUpPosY");
                            Ez_Model.MovePickUpPosY(UnloadingLiftIndexY);
                            UnloadYStep = Unload_Y_Step.Move_Y_PickUp_Done;

                            Global.Mlog.Info($"Unload_Y_Step => Move_Y_PickUp_Done");
                        }
                        else
                        {
                            UnloadYPutDownMoving = false;
                            UnloadingLiftIndexY =0;
                            if (Ez_Model.IsMovePickUpPosY(2) == false)
                                Ez_Model.MovePickUpPosY(2);
                            UnloadYStep = Unload_Y_Step.Move_Y_Ready_Done;
                        }
                    }
                    //if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] > 0)
                    //{
                    //    Global.Mlog.Info($"Unload_Y_Step => {UnloadingLiftIndexY.ToString()}");
                    //    Global.Mlog.Info($"Unload_Y_Step => {SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY].ToString()}");
                    //    Global.Mlog.Info($"Unload_Y_Step => MovePickUpPosY");
                    //    Ez_Model.MovePickUpPosY(UnloadingLiftIndexY);
                    //    UnloadYStep = Unload_Y_Step.Move_Y_PickUp_Done;

                    //    Global.Mlog.Info($"Unload_Y_Step => Move_Y_PickUp_Done");
                    //}
                    //if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] == 0
                    //    && Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER] == true)
                    //{
                    //    // UI 변수 초기화
                    //    SingletonManager.instance.Channel_Model[UnloadingLiftIndexY].Barcode = string.Empty;

                    //    Ez_Model.MovePickUpPosY(2);
                    //    UnloadYStep = Unload_Y_Step.Move_Y_Ready_Done;
                    //}
                    //else if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] == 0)
                    //{
                    //    UnloadingLiftIndexY++;
                    //    if (UnloadingLiftIndexY >= 3)
                    //        UnloadingLiftIndexY = 0;
                    //}
                    //// Pickup완료 하였으면 다음 stage 확인
                    //else if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] == 0
                    //    && SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    //{
                    //    UnloadingLiftIndexY += 1;
                    //    // 3번 stage까지 확인하고 다시 1번으로 넘어간다.
                    //    if (UnloadingLiftIndexY == 3)
                    //        UnloadingLiftIndexY = 0;
                    //    SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] = (int)Floor_Index.Max;
                    //    for (int i = 0; i < (int)Floor_Index.Max; i++)
                    //        SingletonManager.instance.Display_Lift[UnloadingLiftIndexY].Floor[i] = true;

                    //    UnloadYPutDownMoving = false;
                    //    UnloadYStep = Unload_Y_Step.Idle;
                    //}
                        
                    // 전체 stage에 제품이 없으면 ready 위치도 이동후 다시 돌알아와서 Stage 체크한다
                    // 이미 Ready 위치에 있으면 Idle로 다시 보낸다
                    //UnloadYStep = Unload_Y_Step.Move_Y_Ready_Check;
                    break;
                case Unload_Y_Step.Move_Y_Ready_Done:
                    if (Ez_Model.IsMovePickUpPosY(2)== true)// && Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER] == false)
                    {
                        UnloadYPutDownMoving = false;
                        UnloadYStep = Unload_Y_Step.Idle;
                    }
                    break;
                case Unload_Y_Step.Move_Y_PickUp_Done:
                    if (Ez_Model.IsUnloadSafetyPosY() == true)
                    {
                        if (UnloadYPutDownMoving == true)
                        {
                            UnloadYPutDownMoving = false;
                        }
                    }
                    // 픽업위치 도착하면 Z pickup이동
                    if (Ez_Model.IsMovePickUpPosY(UnloadingLiftIndexY) ==true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => Lift {UnloadingLiftIndexY.ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => IsMovePickUpPosY Done");
                        UnloadYPutDownMoving = false;
                        Global.Mlog.Info($"Unload_Y_Step => UnloadYPutDownMoving : {UnloadYPutDownMoving.ToString()}");

                        Ez_Model.MovePickUpPosZ(UnloadingLiftIndexY);
                        UnloadYStep = Unload_Y_Step.Move_Z_PickUp_Down_Done;
                        _TimeDelay.Restart();
                        Global.Mlog.Info($"Unload_Y_Step => MovePickUpPosZ");
                        Global.Mlog.Info($"Unload_Y_Step => Move_Z_PickUp_Down_Done");
                    }
                    break;
                case Unload_Y_Step.Move_Z_PickUp_Down_Done:
                    if (Ez_Model.IsMovePickUpPosZ(UnloadingLiftIndexY) ==true)
                    {
                        UnloadYStep = Unload_Y_Step.Grip_Detect_Check;
                        _TimeDelay.Restart();
                    }
                    else if (_TimeDelay.ElapsedMilliseconds > 2000)
                    {
                        UnclampFailMessage = $"Unloading Z Down Timeout [Lift {UnloadingLiftIndexY + 1}]";
                        SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] = (int)Floor_Index.Max;
                        for (int i =0; i< (int)Floor_Index.Max; i++)
                            SingletonManager.instance.Display_Lift[UnloadingLiftIndexY].Floor[i] = true;

                        Global.Mlog.Info(UnclampFailMessage);
                        UnloadYStep = Unload_Y_Step.Idle;
                    }
                    break;
                case Unload_Y_Step.Grip_Detect_Check:
                    // Detect sensor delay time
                    if (_TimeDelay.ElapsedMilliseconds > 50)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMovePickUpPosZ Done");
                        Global.Mlog.Info($"Unload_Y_Step => UNLOAD_LD_Z_GRIP_DETECT(DI) : {Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_DETECT].ToString()}");

                        if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_DETECT] == true
                            || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                        {
                            Global.Mlog.Info($"Unload_Y_Step => Grip");
                            Dio_Output(DO_MAP.UNLOAD_LD_Z_GRIP, true);
                            _TimeDelay.Restart();

                            UnloadYStep = Unload_Y_Step.Grip_Check;
                        }
                        else
                        {
                            // 제품 감지가 되지않으면 다음 층으로 이동
                            SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] -= 1;
                            int floor = SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY];
                            SingletonManager.instance.Display_Lift[UnloadingLiftIndexY].Floor[floor] = false;
                            UnloadYStep = Unload_Y_Step.Move_Y_PickUp_Done;

                            Global.Mlog.Info($"Unload_Y_Step => Next Floor : {floor.ToString()}");
                        }
                    }
                    break;
                case Unload_Y_Step.Grip_Check:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true
                        && _TimeDelay.ElapsedMilliseconds > 300)
                    {
                        Ez_Model.MoveReadyPosZ();
                        UnloadYStep = Unload_Y_Step.Move_Z_Ready_Done;

                        Global.Mlog.Info($"Unload_Y_Step => MoveReadyPosZ");
                        Global.Mlog.Info($"Unload_Y_Step => Move_Z_Ready_Done");
                    }
                    else if (_TimeDelay.ElapsedMilliseconds > 2000)
                    {
                        // Grip NG 발생시 Ungrip 하고 조치완료 대기
                        UnclampFailMessage = $"Unloading Grip Timeout";
                        Dio_Output(DO_MAP.UNLOAD_LD_Z_GRIP, false);

                        UnloadYStep = Unload_Y_Step.Move_Z_PickUp_Down_Done;
                        Global.Mlog.Info($"Unload_Y_Step => Unloading Grip Timeout");
                        Global.Mlog.Info($"Unload_Y_Step => Move_Z_PickUp_Down_Done");
                    }
                    break;
                case Unload_Y_Step.Move_Z_Ready_Done:
                    // Z Up완료 후 Pudown위치에 clamp가 없고 RIGHT위치에 있으면 Y putdown위치 이동
                    if (Ez_Model.IsMoveReadyPosZ() == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER] != true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == true
                        && Dio.DO_RAW_DATA[(int)DO_MAP.UNLOAD_X_FWD] != true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMoveReadyPosZ Done");
                        Global.Mlog.Info($"Unload_Y_Step => UNLOAD_BUFFER(X4E) : {Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER].ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => UNLOAD_X_RIGHT(X49) : {Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT].ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => UNLOAD_X_FWD(Y27) : {Dio.DO_RAW_DATA[(int)DO_MAP.UNLOAD_X_FWD].ToString()}");

                        Ez_Model.MovePutDownPosY();
                        UnloadYPutDownMoving = true;
                        UnloadYStep = Unload_Y_Step.Move_Y_PutDown_Done;

                        Global.Mlog.Info($"Unload_Y_Step => MovePutDownPosY");
                        Global.Mlog.Info($"Unload_Y_Step => UnloadYPutDownMoving : true");
                        Global.Mlog.Info($"Unload_Y_Step => Move_Y_PutDown_Done");
                    }
                    break;
                case Unload_Y_Step.Move_Y_PutDown_Done:
                    if (Ez_Model.IsMovePutDownPosY() == true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMovePutDownPosY Done");
                        Global.Mlog.Info($"Unload_Y_Step => MovePutDownPosZ");

                        Ez_Model.MovePutDownPosZ();
                        UnloadYStep = Unload_Y_Step.Move_Z_PutDown_Done;

                        Global.Mlog.Info($"Unload_Y_Step => Move_Z_PutDown_Done");
                        _TimeDelay.Restart();
                    }
                    break;
                case Unload_Y_Step.Move_Z_PutDown_Done:
                    if (Ez_Model.IsMovePutDownPosZ() == true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMovePutDownPosZ Done");
                        Global.Mlog.Info($"Unload_Y_Step => Ungrip");
                        Dio_Output(DO_MAP.UNLOAD_LD_Z_GRIP, false);
                        
                        UnloadYStep = Unload_Y_Step.UnGrip_Check;

                        Global.Mlog.Info($"Unload_Y_Step => UnGrip_Check");
                    }
                    else if (_TimeDelay.ElapsedMilliseconds > 5000)
                    {
                        UnclampFailMessage = "Unloading Z Putdown Time out;";
                        UnloadYStep = Unload_Y_Step.Move_Z_PutDown_Done;
                    }
                    break;
                case Unload_Y_Step.UnGrip_Check:
                    // clamp가 아직 남아있으면 
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_UNGRIP_CYL] == true)
                    {
                        Ez_Model.MoveReadyPosZ();
                        Global.Mlog.Info($"Unload_Y_Step => MoveReadyPosZ");
                        UnloadYStep = Unload_Y_Step.Move_Z_PutDown_Up_Done;

                        Global.Mlog.Info($"Unload_Y_Step => Move_Z_PutDown_Up_Done");
                    }
                    break;
                case Unload_Y_Step.Move_Z_PutDown_Up_Done:
                    if (Ez_Model.IsMoveReadyPosZ() == true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMoveReadyPosZ Done");

                        SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] -= 1;
                        if (SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] < 0)
                            SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY] = 0;
                        int floor = SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY];
                        SingletonManager.instance.Display_Lift[UnloadingLiftIndexY].Floor[floor] = false;
                       
                        UnloadYStep = Unload_Y_Step.Idle;
                        Global.Mlog.Info($"Unload_Y_Step => Next Floor : {floor.ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => Next Floor : Idle");
                    }
                    break; 
            }
            int step = (int)UnloadYStep;
            Global.instance.Write_Sequence_Log("UNLOAD_Y_STEP", step.ToString());
            Global.instance.Write_Sequence_Log("UNLOAD_INDEX_Y", UnloadingLiftIndexY.ToString());
            Global.instance.Write_Sequence_Log("UNLOAD_FLOOW", SingletonManager.instance.UnLoadFloor[UnloadingLiftIndexY].ToString());
        }
        private bool Dio_Output(DO_MAP io, bool OnOff)
        {
            bool result = false;
            result = Dio.SetIO_OutputData((int)io, OnOff);
            
            Thread.Sleep(5);
            return result;
        }
        
        private void LiftInterfaceOnOff(int Index, bool OnOff)
        {
            if (Index == (int)Lift_Index.Lift_1)
            {
                if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_UPPER_INTERFACE] == true)
                {
                    Dio_Output(DO_MAP.AGING_INVERT_CV_UPPER_INTERFACE_1, OnOff);
                }
                else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_LOW_INTERFACE] == true)
                {
                    Dio_Output(DO_MAP.AGING_INVERT_CV_LOW_INTERFASE_1, OnOff);
                }
            }
            else if (Index == (int)Lift_Index.Lift_2)
            {
                if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_UPPER_INTERFACE] == true)
                {
                    Dio_Output(DO_MAP.AGING_INVERT_CV_UPPER_INTERFACE_2, OnOff);
                }
                else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_LOW_INTERFACE] == true)
                {
                    Dio_Output(DO_MAP.AGING_INVERT_CV_LOW_INTERFASE_2, OnOff);
                }
            }
            else if (Index == (int)Lift_Index.Lift_3)
            {
                if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_UPPER_INTERFACE] == true)
                {
                    Dio_Output(DO_MAP.AGING_INVERT_CV_UPPER_INTERFACE_3, OnOff);
                }
                else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_LOW_INTERFACE] == true)
                {
                    Dio_Output(DO_MAP.AGING_INVERT_CV_LOW_INTERFASE_3, OnOff);
                }
            }
        }
        public void SetLastStep()
        {
            var myIni = new IniFile(Global.instance.IniSequencePath);
            string value = "";
            value = myIni.Read("UNLOAD_Y_STEP", "SEQUENCE");
            if (string.IsNullOrEmpty(value) == true)
            {
                LiftStep = Lift_Step.Idle;
                UnClampStep = UnClampHandStep.Idle;
                RtnBtmStep = ReturnBottomStep.Idle;
                RtnTopStep = ReturnTopStep.Idle;
                TopOutCVStep = Top_Out_CV.Idle;
                UnClampCvStep = UnClamp_CV_Step.Idle;
                UnloadCvStep = Unload_CV_Step.Idle;
                UnloadXlStep = Unload_X_Step.Idle;
                UnloadYStep = Unload_Y_Step.Idle;
                UnloadingLiftIndexY = 0;
                UnloadingLiftPickupIndex = 0;
                SingletonManager.instance.UnLoadFloor[0] = 0;
                SingletonManager.instance.UnLoadFloor[1] = 0;
                SingletonManager.instance.UnLoadFloor[2] = 0;
                return;
            }
            UnloadYStep = (Unload_Y_Step)Convert.ToInt16(value);
            value = myIni.Read("LIFT_STEP", "SEQUENCE");
            LiftStep = (Lift_Step)Convert.ToInt16(value);
            value = myIni.Read("UNCLAMP_STEP", "SEQUENCE");
            UnClampStep = (UnClampHandStep)Convert.ToInt16(value);
            value = myIni.Read("RTN_BTM_STEP", "SEQUENCE");
            RtnBtmStep = (ReturnBottomStep)Convert.ToInt16(value);
            value = myIni.Read("RTN_TOP_STEP", "SEQUENCE");
            RtnTopStep = (ReturnTopStep)Convert.ToInt16(value);
            value = myIni.Read("TOP_OUT_CV_STEP", "SEQUENCE");
            TopOutCVStep = (Top_Out_CV)Convert.ToInt16(value);
            value = myIni.Read("UNCLAMP_CV_STEP", "SEQUENCE");
            UnClampCvStep = (UnClamp_CV_Step)Convert.ToInt16(value);
            value = myIni.Read("UNLOAD_CV_STEP", "SEQUENCE");
            UnloadCvStep = (Unload_CV_Step)Convert.ToInt16(value);
            value = myIni.Read("UNLOAD_X_STEP", "SEQUENCE");
            UnloadXlStep = (Unload_X_Step)Convert.ToInt16(value);
            value = myIni.Read("LIFT_STAGE", "SEQUENCE");
            //value = myIni.Read("RTN_BTM_DONE", "SEQUENCE");
            //if (value == "") UnclampBottomReturnDone = false;
            //else    UnclampBottomReturnDone = bool.Parse(value); 
        }
    }
}
