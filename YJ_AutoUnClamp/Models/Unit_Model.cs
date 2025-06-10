using Common.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        Floor_6,
        Floor_7,
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
        private EziDio_Model Dio = SingletonManager.instance.Ez_Dio;
        private EzMotion_Model_E Ez_Model = SingletonManager.instance.Ez_Model;

        private bool _NoneSetTest = true; // Set Test Mode

        private bool _isLoopRunning = false;
        public bool UnloadYPutDownMoving=false;
        public bool UnclampBottomReturnDone = false;

        public string UnclampSetFailMessage = string.Empty;
        Stopwatch _TimeDelay = new Stopwatch();
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
            Move_PutDown_Done_Check,
            Top_Hand_PutDown,
            Top_Hand_PutDown_Check,
            Top_Hand_UnGrip_Check,
            Top_Hand_PutDown_Up_Check,
            Set_MES_Send,
            Set_MES_Result_Wait,
            Set_Hand_PutDown,
            set_Hand_PutDown_Check,
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
            CV_Stop_Wait
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
            CV_Stop
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
                    UnClampStep = UnClampHandStep.Rtn_Bottom_Handle_Check;

                    Global.Mlog.Info($"UnClampHandStep => Rtn_Bottom_Handle_Check");
                    break;
                case UnClampHandStep.Rtn_Bottom_Handle_Check:
                    // Return Bottom Handler 위치가 left 일때 X handler Ready위치로 이동
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_X_LEFT] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_UNGRIP] == true
                        &&  UnclampBottomReturnDone == false)
                    {
                        Global.Mlog.Info($"UnClampHandStep => Ready_Move_Done");
                        Global.Mlog.Info($"UnClampHandStep => Move Ready X");
                        if (Ez_Model.MoveTopReadyPosX()==true)
                            UnClampStep = UnClampHandStep.Ready_Move_Done;
                    }
                    break;
                case UnClampHandStep.Ready_Move_Done:
                    // Ready위치 도착 확인
                    if (Ez_Model.IsMoveTopReadyDoneX()==true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => Move Ready Done X");
                        Global.instance.TactTimeStart = false;
                        UnClampStep = UnClampHandStep.UnClamping_Wait;

                        Global.Mlog.Info($"UnClampHandStep => UnClamping_Wait");
                    }
                    break;
                case UnClampHandStep.UnClamping_Wait:
                    // Clamp가 진입하면 Top Hand Down (도착 센서 및 Centering 전지 확인)
                    if ((Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true 
                        && Dio.DO_RAW_DATA[(int)DO_MAP.UNCLAMP_CV_RUN] == false)
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        Global.instance.TactTimeStart = true;
                        Global.instance.UnLoadingTactTimeStart();
                        UnclampBottomReturnDone = true;

                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, true);
                        Dio_Output(DO_MAP.UNCLAMP_CV_CENTERING, true);
                        UnClampStep = UnClampHandStep.Top_Hand_PickUp_Down_Check;

                        Global.Mlog.Info($"UnClampHandStep => UnclampBottomReturnDone : {UnclampBottomReturnDone.ToString()}");
                        Global.Mlog.Info($"UnClampHandStep => Left Z Down");
                        Global.Mlog.Info($"UnClampHandStep => CV Centering FWD");
                        Global.Mlog.Info($"UnClampHandStep => Top_Hand_PickUp_Down_Check");
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
                        Global.Mlog.Info($"UnClampHandStep => Top_Hand_PickUp_Down_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_FR_Grip_Check:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_F_CYL] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_R_CYL] == true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_GRIP, true);
                        UnClampStep = UnClampHandStep.Top_Hand_Grip_Check;

                        Global.Mlog.Info($"UnClampHandStep => Left Z Grip");
                        Global.Mlog.Info($"UnClampHandStep => Top_Hand_Grip_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_Grip_Check:
                    // Top Hand Grip 완료후 Up
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_CYL] == true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);
                        UnClampStep = UnClampHandStep.Top_Hand_Up_Check;

                        Global.Mlog.Info($"UnClampHandStep => Left Z Up");
                        Global.Mlog.Info($"UnClampHandStep => Top_Hand_Up_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_Up_Check:
                    // Up 완료 후 Set Hand PickUP 위치 이동 & Turn
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_UP_CYL] == true)
                    {
                        Dio_Output(DO_MAP.OUT_PP_RIGHT_TURN, true);
                        Global.Mlog.Info($"UnClampHandStep => Right Turn");

                        Global.Mlog.Info($"UnClampHandStep => Top Pickup Right Pos X");
                        if (Ez_Model.MoveTopPickUpRightPosX()==true)
                            UnClampStep = UnClampHandStep.Set_Hand_PickUp_Move_Check;
                        Global.Mlog.Info($"UnClampHandStep => Set_Hand_PickUp_Move_Check");
                    }
                    break;
                case UnClampHandStep.Set_Hand_PickUp_Move_Check:
                    // Set Handler PickUp위치 도착 및 Turn 완료확인 후  Down
                    if (Ez_Model.IsMoveTopPickUpRightDoneX()==true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_RIGHT_RETURN] == true
                        )
                    {
                        Global.Mlog.Info($"UnClampHandStep => Right Z Down");
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, true);

                        UnClampStep = UnClampHandStep.Set_Hand_Down_Check;
                        Global.Mlog.Info($"UnClampHandStep => Set_Hand_Down_Check");
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
                        Global.Mlog.Info($"UnClampHandStep => Set_Hand_Vacuum_Check");
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
                        Global.Mlog.Info($"UnClampHandStep => Set_Hand_Up_Check");
                    }
                    break;
                case UnClampHandStep.Set_Hand_Up_Check:
                    // Set hand up완료 후 Put down위치로 이동 & hand Return
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_UP_CYL] == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => Right Turn");
                        Dio_Output(DO_MAP.OUT_PP_RIGHT_TURN, false);

                        Global.Mlog.Info($"UnClampHandStep => Top Move PutDown Pos X");
                        if (Ez_Model.MoveTopPutDownPosX()==true)
                            UnClampStep = UnClampHandStep.Move_PutDown_Done_Check;
                        Global.Mlog.Info($"UnClampHandStep => Move_PutDown_Done_Check");
                    }
                    break;
                case UnClampHandStep.Move_PutDown_Done_Check:
                    // putdown위치 도착 확인 후 top hand cv에 clamp가 있는지 확인 한다.
                    if (Ez_Model.IsMoveTopPutDownDoneX()==true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_RIGHT_TURN] == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => Top_Hand_PutDown");
                        UnClampStep = UnClampHandStep.Top_Hand_PutDown;
                    }
                    break;
                case UnClampHandStep.Top_Hand_PutDown:
                    // cv에 clamp가 없으면 top hand down
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.TOP_JIG_CV_DETECT_1] == false
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        Global.Mlog.Info($"UnClampHandStep => Left Top Clamp Down");
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, true);
                        UnClampStep = UnClampHandStep.Top_Hand_PutDown_Check;
                        Global.Mlog.Info($"UnClampHandStep => Top_Hand_PutDown_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_PutDown_Check:
                    // top down 완료하면 ungrip
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_DOWN_CYL] == true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_GRIP, false);
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_GRIP_F_FINGER, false);
                        Dio_Output(DO_MAP.OUT_PP_LEFT_Z_GRIP_R_FINGER, false);
                        UnClampStep = UnClampHandStep.Top_Hand_UnGrip_Check;
                        Global.Mlog.Info($"UnClampHandStep => Top_Hand_UnGrip_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_UnGrip_Check:
                    // ungrip완료후 top hand up
                    bool ret = Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_F_CYL];
                    ret = Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_R_CYL];

                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_UNGRIP_CYL] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_F_CYL] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_GRIP_FINGER_R_CYL] == false)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_LEFT_Z_DOWN, false);

                        Global.Mlog.Info($"UnClampHandStep => Left Z Up");
                        UnClampStep = UnClampHandStep.Top_Hand_PutDown_Up_Check;
                        Global.Mlog.Info($"UnClampHandStep => Nest Step : Top_Hand_PutDown_Up_Check");
                    }
                    break;
                case UnClampHandStep.Top_Hand_PutDown_Up_Check:
                    // top hand up완료하면 Set Hand Down 조건 확인으로 넘어간다.
                    if ((Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_LEFT_Z_UP_CYL] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.REAR_INTERFACE_1] == false)
                        || _NoneSetTest == true
                        || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        Global.Mlog.Info($"UnClampHandStep => NFC Use Setting : {SingletonManager.instance.SystemModel.NfcUseNotUse}");
                        if (SingletonManager.instance.SystemModel.NfcUseNotUse == "Use")
                        {
                            UnClampStep = UnClampHandStep.Set_MES_Send;
                            Global.Mlog.Info($"UnClampHandStep => Set_MES_Send");
                        }
                        else
                        {
                            UnClampStep = UnClampHandStep.Set_Hand_PutDown;
                            Global.Mlog.Info($"UnClampHandStep => Set_Hand_PutDown");
                        }
                    }
                    break;
                case UnClampHandStep.Set_MES_Send:
                    //if (!string.IsNullOrEmpty(SingletonManager.instance.Nfc_Data))
                    string nfc = SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].NfcData;
                    Global.Mlog.Info($"UnClampHandStep => NFC Data : {nfc}");
                    if (!string.IsNullOrEmpty(nfc))
                    {
                        //SingletonManager.instance.HttpJsonModel.SendRequest("saveInspInfo", nfc, "PASS");
                        SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Mes].SendMes(nfc);
                        UnClampStep = UnClampHandStep.Set_MES_Result_Wait;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Set_MES_Result_Wait");
                        _TimeDelay.Restart();
                    }
                    else
                    {
                        Global.Mlog.Info($"UnClampHandStep => NFC Data is Empty");
                        UnclampSetFailMessage = "NFC Data is Empty";
                        UnClampStep = UnClampHandStep.Idle;
                    }
                    break;
                case UnClampHandStep.Set_MES_Result_Wait:
                    if (SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].IsReceived == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => MES Reveive : {SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].MesResult}");
                        if (SingletonManager.instance.SerialModel[(int)Serial_Model.SerialIndex.Nfc].MesResult == "PASS")
                        {
                            Global.Mlog.Info($"UnClampHandStep => MES Result : PASS");

                            UnClampStep = UnClampHandStep.Set_Hand_PutDown;
                            Global.Mlog.Info($"UnClampHandStep => Next Step : Set_Hand_PutDown");
                        }
                        else
                        {
                            Global.Mlog.Info($"UnClampHandStep => MES Result : FAIL");
                            UnclampSetFailMessage = "MES Result : FAIL";
                            UnClampStep = UnClampHandStep.Idle;
                            Global.Mlog.Info($"UnClampHandStep => Next Step : Idle");
                        }
                    }
                    else if (_TimeDelay.ElapsedMilliseconds > 2000)
                    {
                        UnclampSetFailMessage = "MES Result Receive Timeout.";
                        UnClampStep = UnClampHandStep.Idle;
                        Global.Mlog.Info($"UnClampHandStep => Next Step : Idle");
                    }
                    if (_TimeDelay.ElapsedMilliseconds == 0)
                        _TimeDelay.Restart();
                    break;
                case UnClampHandStep.Set_Hand_PutDown:
                    // set putdown 위치에 제품이 없으면 & Return 되있으면 down
                    /*조건 확인 필요, 인터페이 신호가 있을꺼 같음*/
                    /***************************************************/
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_RIGHT_TURN] == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => SET Rirht PutDown Z Down");
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, true);
                        UnClampStep = UnClampHandStep.set_Hand_PutDown_Check;
                        Global.Mlog.Info($"UnClampHandStep => set_Hand_PutDown_Check");
                    }
                    break;
                case UnClampHandStep.set_Hand_PutDown_Check:
                    // set hand down완료후 blow on
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_DOWN_CYL] == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => SET Right Z BLOW On");
                        Global.Mlog.Info($"UnClampHandStep => Right Z UP");
                        Dio_Output(DO_MAP.OUT_PP_RIGHT_Z_BLOW, true);
                        Dio_Output(DO_MAP.UNCLAMP_RIGHT_Z_DOWN, false);

                        Global.Mlog.Info($"UnClampHandStep => Set_Hand_PutDown_Up_Check");
                        UnClampStep = UnClampHandStep.Set_Hand_PutDown_Up_Check;
                    }
                    break;
                case UnClampHandStep.Set_Hand_PutDown_Up_Check:
                    // up완료하면 Idel로 이동한다.
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.OUT_PP_TR_RIGHT_Z_UP_CYL] == true)
                    {
                        Global.Mlog.Info($"UnClampHandStep => SET Right Z BLOW Off");
                        Dio_Output(DO_MAP.OUT_PP_RIGHT_Z_BLOW, false);
                        UnClampStep = UnClampHandStep.Idle;
                        Global.Mlog.Info($"UnClampHandStep => Idle");
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
                        //&& Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_CENTERING_FWD_CYL] == false 
                        && Ez_Model.IsMoveTopPutDownDoneX() == true
                        && UnclampBottomReturnDone == true)
                    {
                        Global.Mlog.Info($"ReturnBottomStep => UnclampBottomReturnDone : {UnclampBottomReturnDone.ToString()}");
                        Global.Mlog.Info($"ReturnBottomStep => PutDown Pos Done X");
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_DOWN] == true)
                            Dio_Output(DO_MAP.BOTTOM_RETURN_Z_DOWN, false);
                        Dio_Output(DO_MAP.BOTTOM_RETURN_X_FWD, true);
                        Global.Mlog.Info($"ReturnBottomStep => Bottom Retutn X Move Right");

                        RtnBtmStep = ReturnBottomStep.Right_Move_Done;
                        Global.Mlog.Info($"ReturnBottomStep => Right_Move_Done");
                    }
                    if (SingletonManager.instance.EquipmentMode == EquipmentMode.Dry
                        &&Ez_Model.IsMoveTopPutDownDoneX() == true
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
                    }
                    break;
                case ReturnBottomStep.Left_Move_Done:
                    // put down위치 도착하면 clamp장비한데 Interface신호 on
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_X_LEFT] == true)
                    {
                        Global.Mlog.Info($"ReturnBottomStep => Interface Send On");
                        Dio_Output(DO_MAP.BOTTOM_RETURN_CV_INTERFACE, true);
                        RtnBtmStep = ReturnBottomStep.Clamp_IF_Return_Wait;
                        Global.Mlog.Info($"ReturnBottomStep => Clamp_IF_Return_Wait");
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
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.BOTTOM_RETURN_Z_UNGRIP] == true)
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
                        UnclampBottomReturnDone = false;
                        Global.Mlog.Info($"ReturnBottomStep => UnclampBottomReturnDone : {UnclampBottomReturnDone.ToString()}");
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

                    Global.Mlog.Info($"Lift_Step => Clamp_IF_Wait");
                    Global.Mlog.Info($"Lift_Step => Unloading Y Ready Check");
                    break;
                case Lift_Step.Clamp_IF_Wait:
                    // Clamp Interface 기다린다
                    if (Ez_Model.IsMoveReadyPosY() == true)
                    {
                        // Up 1
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_UPPER_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] != true)
                        {
                            SingletonManager.instance.UnLoadStageNo = 0;
                            if (Ez_Model.MoveMoveLiftInputPos(SingletonManager.instance.UnLoadStageNo) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_1_UPPER_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {SingletonManager.instance.UnLoadStageNo.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Upper Position");
                                Global.Mlog.Info($"Lift_Step => Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Input_Move_Donw;
                            }
                        }
                        // Low1
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_1_LOW_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] != true)
                        {
                            SingletonManager.instance.UnLoadStageNo = 0;
                            if (Ez_Model.MoveMoveLiftLowPos(SingletonManager.instance.UnLoadStageNo) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_1_LOW_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {SingletonManager.instance.UnLoadStageNo.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Low Position");
                                Global.Mlog.Info($"Lift_Step => Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Down_Done;
                            }
                        }
                        // Up2
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_UPPER_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] != true)
                        {
                            SingletonManager.instance.UnLoadStageNo = 1;
                            if (Ez_Model.MoveMoveLiftInputPos(SingletonManager.instance.UnLoadStageNo) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_2_UPPER_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {SingletonManager.instance.UnLoadStageNo.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Upper Position");
                                Global.Mlog.Info($"Lift_Step => Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Input_Move_Donw;
                            }
                        }
                        // Low2
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_2_LOW_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] != true)
                        {
                            SingletonManager.instance.UnLoadStageNo = 1;
                            if (Ez_Model.MoveMoveLiftLowPos(SingletonManager.instance.UnLoadStageNo) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_2_LOW_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {SingletonManager.instance.UnLoadStageNo.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Low Position");
                                Global.Mlog.Info($"Lift_Step => Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Down_Done;
                            }
                        }
                        // Up3
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_UPPER_INTERFACE] == true
                           && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] != true)
                        {
                            SingletonManager.instance.UnLoadStageNo = 2;
                            if (Ez_Model.MoveMoveLiftInputPos(SingletonManager.instance.UnLoadStageNo) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_3_UPPER_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {SingletonManager.instance.UnLoadStageNo.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Upper Position");
                                Global.Mlog.Info($"Lift_Step => Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Input_Move_Donw;
                            }
                        }
                        //Low3
                        else if (Dio.DI_RAW_DATA[(int)DI_MAP.AGING_CV_3_LOW_INTERFACE] == true
                            && Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] != true)
                        {
                            SingletonManager.instance.UnLoadStageNo = 2;
                            if (Ez_Model.MoveMoveLiftLowPos(SingletonManager.instance.UnLoadStageNo) == true)
                            {
                                Global.Mlog.Info($"Lift_Step => AGING_CV_3_LOW_INTERFACE On");
                                Global.Mlog.Info($"Lift_Step => {SingletonManager.instance.UnLoadStageNo.ToString()}");
                                Global.Mlog.Info($"Lift_Step => Lift Move Low Position");
                                Global.Mlog.Info($"Lift_Step => Lift_Input_Move_Donw");
                                LiftStep = Lift_Step.Lift_Down_Done;
                            }
                        }
                    }
                    break;
                case Lift_Step.Lift_Input_Move_Donw:
                    if (Ez_Model.IsMoveLiftInputDone(SingletonManager.instance.UnLoadStageNo) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => Lift Move Upper Position Done");
                        Global.Mlog.Info($"Lift_Step => Lift {SingletonManager.instance.UnLoadStageNo.ToString()} CV On");
                        // Lift CV On
                        Dio_Output(DO_MAP.LIFT_CV_RUN_1 + SingletonManager.instance.UnLoadStageNo, true);

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
                        Global.Mlog.Info($"Lift_Step => Lift_CV_Stop");
                    }
                    break;
                case Lift_Step.Lift_Down_Done:
                    // 1층도착하였으면 Interface On 하고 CV Run
                    if (Ez_Model.IsMoveLiftLowDone(SingletonManager.instance.UnLoadStageNo) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => Lift Move Low Position Done");
                        Global.Mlog.Info($"Lift_Step => Lift {SingletonManager.instance.UnLoadStageNo.ToString()} CV On");
                        // Lift CV On
                        Dio_Output(DO_MAP.LIFT_CV_RUN_1 + SingletonManager.instance.UnLoadStageNo, true);

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
                        Global.Mlog.Info($"Lift_Step => Lift_CV_Stop");
                    }
                    break;
                case Lift_Step.Lift_CV_Stop:
                    // Lift CV의 진입 완료신호 받으면 Interface Off  Lift Cv Stop
                    if (SingletonManager.instance.UnLoadStageNo == (int)Lift_Index.Lift_1)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] == true)
                        {
                            _TimeDelay.Restart();
                            LiftStep = Lift_Step.Lift_CV_Stop_Wait;

                            Global.Mlog.Info($"Lift_Step => Lift 1 Detect Sensor On");
                            Global.Mlog.Info($"Lift_Step => Lift_CV_Stop_Wait");
                        }
                    }
                    else if (SingletonManager.instance.UnLoadStageNo == (int)Lift_Index.Lift_2)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] == true)
                        {
                            _TimeDelay.Restart();
                            LiftStep = Lift_Step.Lift_CV_Stop_Wait;
                            Global.Mlog.Info($"Lift_Step => Lift 2 Detect Sensor On");
                            Global.Mlog.Info($"Lift_Step => Lift_CV_Stop_Wait");
                        }
                    }
                    else if (SingletonManager.instance.UnLoadStageNo == (int)Lift_Index.Lift_3)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] == true)
                        {
                            _TimeDelay.Restart();
                            LiftStep = Lift_Step.Lift_CV_Stop_Wait;
                            Global.Mlog.Info($"Lift_Step => Lift 3 Detect Sensor On");
                            Global.Mlog.Info($"Lift_Step => Lift_CV_Stop_Wait");
                        }
                    }
                    break;
                case Lift_Step.Lift_CV_Stop_Wait:
                    if (_TimeDelay.ElapsedMilliseconds > 1000)
                    {
                        // Lift CV On
                        Dio_Output(DO_MAP.LIFT_CV_RUN_1 + SingletonManager.instance.UnLoadStageNo, false);
                        Global.Mlog.Info($"Lift_Step => Lift {(SingletonManager.instance.UnLoadStageNo).ToString()} CV Off");
                        
                        LiftStep = Lift_Step.Aging_CV_Stop;
                        Global.Mlog.Info($"Lift_Step => Aging_CV_Stop");
                    }
                    break;
                case Lift_Step.Aging_CV_Stop:
                    // Interfa off
                    Global.Mlog.Info($"Lift_Step => Lift {(SingletonManager.instance.UnLoadStageNo).ToString()} Interface Off");
                    LiftInterfaceOnOff(SingletonManager.instance.UnLoadStageNo, false);

                    if (Ez_Model.IsMoveLiftLowDone(SingletonManager.instance.UnLoadStageNo) == true)
                    {
                        if (SingletonManager.instance.SystemModel.BcrUseNotUse == "Use")
                        {
                            LiftStep = Lift_Step.Lift_Low_Move_Upper;
                            Global.Mlog.Info($"Lift_Step => Lift_Low_Move_Upper");
                        }
                        else
                        {
                            LiftStep = Lift_Step.Lift_UnlodingPosMove;
                            Global.Mlog.Info($"Lift_Step => Lift_UnlodingPosMove");
                        }
                    }
                    else
                    {
                        Global.Mlog.Info($"Lift_Step => Barcode Use : {SingletonManager.instance.SystemModel.BcrUseNotUse}");
                        if (SingletonManager.instance.SystemModel.BcrUseNotUse == "Use")
                        {
                            LiftStep = Lift_Step.Clamp_BarCode_Read;
                            Global.Mlog.Info($"Lift_Step => Clamp_BarCode_Read");
                        }
                        else
                        {
                            LiftStep = Lift_Step.Lift_UnlodingPosMove;
                            Global.Mlog.Info($"Lift_Step => Lift_UnlodingPosMove");
                        }
                    }
                    break;
                case Lift_Step.Lift_Low_Move_Upper:

                     Ez_Model.MoveMoveLiftInputPos(SingletonManager.instance.UnLoadStageNo);
                     LiftStep = Lift_Step.Lift_Low_Move_Upper_Doe;
                     Global.Mlog.Info($"Lift_Step => Lift_Low_Move_Upper_Doe");
                    break;
                case Lift_Step.Lift_Low_Move_Upper_Doe:
                    if (Ez_Model.IsMoveLiftInputDone(SingletonManager.instance.UnLoadStageNo) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => Barcode Use : {SingletonManager.instance.SystemModel.BcrUseNotUse}");
                        if (SingletonManager.instance.SystemModel.BcrUseNotUse == "Use")
                        {
                            LiftStep = Lift_Step.Clamp_BarCode_Read;
                            Global.Mlog.Info($"Lift_Step => Clamp_BarCode_Read");
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
                    SingletonManager.instance.SerialModel[SingletonManager.instance.UnLoadStageNo].SendBcrTrig();
                    LiftStep = Lift_Step.Clamp_BarCode_Read_Done;
                    Global.Mlog.Info($"Lift_Step => Clamp_BarCode_Read_Done");
                    _TimeDelay.Restart();
                    break;
                case Lift_Step.Clamp_BarCode_Read_Done:
                    if (_TimeDelay.ElapsedMilliseconds < 1000)
                    {
                        if (SingletonManager.instance.SerialModel[SingletonManager.instance.UnLoadStageNo].IsReceived == true)
                        {
                            Global.Mlog.Info($"Lift_Step => Barcode Read OK");
                            Global.Mlog.Info($"Lift_Step => Barcode : {SingletonManager.instance.SerialModel[SingletonManager.instance.UnLoadStageNo].Barcode}");
                            LiftStep = Lift_Step.Clamp_InDate_Read;
                            Global.Mlog.Info($"Lift_Step => Next Step : Clamp_InDate_Read");
                        }
                    }
                    else
                    {
                        Global.Mlog.Info($"Lift_Step => Barcode Read Retry");
                        LiftStep = Lift_Step.Clamp_BarCode_Read;
                    }
                    break;
                case Lift_Step.Clamp_InDate_Read:
                    if (SingletonManager.instance.IsTcpConnected == true)
                    {
                        Global.Mlog.Info($"Lift_Step => TCP Barcode Send");
                        string barcode = SingletonManager.instance.SerialModel[SingletonManager.instance.UnLoadStageNo].Barcode;
                        SingletonManager.instance.TcpClient.TcpSendMessage(barcode);
                        LiftStep = Lift_Step.Clamp_InDate_Waite;

                        SingletonManager.instance.Channel_Model[SingletonManager.instance.UnLoadStageNo].Barcode = barcode;
                        Global.Mlog.Info($"Lift_Step => Next Step : Clamp_InDate_Waite");
                        _TimeDelay.Restart();
                    }
                    break;
                case Lift_Step.Clamp_InDate_Waite:
                    if (_TimeDelay.ElapsedMilliseconds < 1000)
                    {
                        if (SingletonManager.instance.TcpClient.TcpReceiveData != "")
                        {
                            SingletonManager.instance.Channel_Model[SingletonManager.instance.UnLoadStageNo].Barcode += (" : " + SingletonManager.instance.TcpClient.TcpReceiveData);
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
                    if (SingletonManager.instance.AgingModel.AgingTimeCheck(SingletonManager.instance.UnLoadStageNo) == true)
                    {
                        LiftStep = Lift_Step.Lift_UnlodingPosMove;
                        Global.Mlog.Info($"Lift_Step => Lift_UnlodingPosMove");
                    }
                    else
                    {
                        _TimeDelay.Restart();
                        LiftStep = Lift_Step.Aging_Time_Check_Wait;
                        Global.Mlog.Info($"Lift_Step => Aging_Time_Check_Wait");
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
                        if (Ez_Model.MoveMoveLiftUnloadingPos(SingletonManager.instance.UnLoadStageNo) == true)
                        {
                            Global.Mlog.Info($"Lift_Step => Lift {(SingletonManager.instance.UnLoadStageNo).ToString()} Move Loding Position");
                            LiftStep = Lift_Step.Lift_UnlodingPos_Done;
                            Global.Mlog.Info($"Lift_Step => Lift_UnlodingPos_Done");
                        }
                    }
                    break;
                case Lift_Step.Lift_UnlodingPos_Done:
                    // Lift 2층 도착 확인한다.
                    if (Ez_Model.IsMoveLiftUnloadingDone(SingletonManager.instance.UnLoadStageNo) == true)
                    {
                        Global.Mlog.Info($"Lift_Step => IsMoveLiftUnloadingDone Done");
                        SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] = (int)Floor_Index.Max;
                        for (int i = 0; i < (int)Floor_Index.Max; i++)
                            SingletonManager.instance.Display_Lift[SingletonManager.instance.UnLoadStageNo].Floor[i] = true;
                        LiftStep = Lift_Step.Idle;
                        Global.Mlog.Info($"Lift_Step => Idle");
                    }
                    break;
                
            }
            int step = (int)LiftStep;
            Global.instance.Write_Sequence_Log("LIFT_STEP", step.ToString());
            Global.instance.Write_Sequence_Log("LIFT_STAGE", SingletonManager.instance.UnLoadStageNo.ToString());

        }
        private void UnClampCvLogic()
        {
            switch(UnClampCvStep)
            {
                case UnClamp_CV_Step.Idle:
                    UnClampCvStep = UnClamp_CV_Step.CV_Run;
                    break;
                case UnClamp_CV_Step.CV_Run:
                    // 제품 없으면 cv run
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_CENTERING_BWD_CYL] == true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_CV_RUN, true);

                        UnClampCvStep = UnClamp_CV_Step.CV_Stop;
                    }
                    else if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == false
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_CENTERING_BWD_CYL] != true)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_CV_CENTERING, false);
                    }
                    break;
                case UnClamp_CV_Step.CV_Stop:
                    // 도착 센서가 들어오면 cv stop하고 centering전진
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true)
                    {
                        _TimeDelay.Restart();
                        UnClampCvStep = UnClamp_CV_Step.CV_Stop_Wait;
                    }
                    break;
                case UnClamp_CV_Step.CV_Stop_Wait:
                    if (_TimeDelay.ElapsedMilliseconds > 1000)
                    {
                        Dio_Output(DO_MAP.UNCLAMP_CV_RUN, false);

                        UnClampCvStep = UnClamp_CV_Step.Idle;
                    }
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
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_Z_UP] == true)
                    {
                        // Unclamp cv 제품 있고 CV end 에 제품없으면 run
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == false
                            //&& Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true
                            || Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == false)
                            //&& (Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_FIRST] == true || Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_MID] == true))
                        {
                            Dio_Output(DO_MAP.INPUT_LEFT_SET_CV_RUN, true);
                            UnloadCvStep = Unload_CV_Step.CV_Stop;
                        }
                    }
                    break;
                case Unload_CV_Step.CV_Stop:
                    // unclamp cv에 제품이 있으고 & cv end 신호가 들어오면 stop
                    // X Right위치에서 Down상태이면 stop
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNCLAMP_CV_DETECT] == true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.IN_CV_DETECT_END] == true)
                    {
                        Dio_Output(DO_MAP.INPUT_LEFT_SET_CV_RUN, false);
                        UnloadCvStep = Unload_CV_Step.Idle;
                    }
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == true
                        && Dio.DO_RAW_DATA[(int)DO_MAP.UNLOAD_Z_DOWN] == true)
                    {
                        Dio_Output(DO_MAP.INPUT_LEFT_SET_CV_RUN, false);
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
                    if (SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] > 0)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => {SingletonManager.instance.UnLoadStageNo.ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => {SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo].ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => MovePickUpPosY");
                        Ez_Model.MovePickUpPosY(SingletonManager.instance.UnLoadStageNo);
                        UnloadYStep = Unload_Y_Step.Move_Y_PickUp_Done;

                        Global.Mlog.Info($"Unload_Y_Step => Move_Y_PickUp_Done");
                    }
                    if (SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] == 0
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER] == true)
                    {
                        // UI 변수 초기화
                        SingletonManager.instance.Channel_Model[SingletonManager.instance.UnLoadStageNo].Barcode = string.Empty;

                        Ez_Model.MovePickUpPosY(2);
                        UnloadYStep = Unload_Y_Step.Move_X_Pickup_Wait;
                    }
                    // Pickup완료 하였으면 다음 stage 확인
                    else if (SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] == 0
                        && SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                    {
                        SingletonManager.instance.UnLoadStageNo += 1;
                        // 3번 stage까지 확인하고 다시 1번으로 넘어간다.
                        if (SingletonManager.instance.UnLoadStageNo == 3)
                            SingletonManager.instance.UnLoadStageNo = 0;
                         SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] = (int)Floor_Index.Max;
                            for (int i = 0; i < (int)Floor_Index.Max; i++)
                                SingletonManager.instance.Display_Lift[SingletonManager.instance.UnLoadStageNo].Floor[i] = true;

                        UnloadYPutDownMoving = false;
                        UnloadYStep = Unload_Y_Step.Idle;
                    }
                        
                    // 전체 stage에 제품이 없으면 ready 위치도 이동후 다시 돌알아와서 Stage 체크한다
                    // 이미 Ready 위치에 있으면 Idle로 다시 보낸다
                    //UnloadYStep = Unload_Y_Step.Move_Y_Ready_Check;
                    break;
                case Unload_Y_Step.Move_X_Pickup_Wait:
                    if (Ez_Model.IsMovePickUpPosY(2)== true)
                    {
                        UnloadYPutDownMoving = false;
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == true
                        && Dio.DO_RAW_DATA[(int)DO_MAP.UNLOAD_X_FWD] != true
                        && Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_BUFFER] == false
                        )
                        {
                            Ez_Model.MoveReadyPosY();
                            UnloadYStep = Unload_Y_Step.Move_Y_Ready_Done;
                        }
                    }
                    break;
                case Unload_Y_Step.Move_Y_Ready_Done:
                    if (Ez_Model.IsMoveReadyPosY() == true)
                    {
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_X_RIGHT] == true
                        && Dio.DO_RAW_DATA[(int)DO_MAP.UNLOAD_X_FWD] != true)
                        {
                            UnloadYStep = Unload_Y_Step.Idle;
                        }
                    }
                    break;
                case Unload_Y_Step.Move_Y_PickUp_Done:
                    // 픽업위치 도착하면 Z pickup이동
                    if (Ez_Model.IsMovePickUpPosY(SingletonManager.instance.UnLoadStageNo) ==true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => Lift {SingletonManager.instance.UnLoadStageNo.ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => IsMovePickUpPosY Done");
                        UnloadYPutDownMoving = false;
                        Global.Mlog.Info($"Unload_Y_Step => UnloadYPutDownMoving : {UnloadYPutDownMoving.ToString()}");

                        Ez_Model.MovePickUpPosZ();
                        UnloadYStep = Unload_Y_Step.Move_Z_PickUp_Down_Done;

                        Global.Mlog.Info($"Unload_Y_Step => MovePickUpPosZ");
                        Global.Mlog.Info($"Unload_Y_Step => Move_Z_PickUp_Down_Done");
                    }
                    break;
                case Unload_Y_Step.Move_Z_PickUp_Down_Done:
                    if (Ez_Model.IsMovePickUpPosZ()==true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMovePickUpPosZ Done");
                        Global.Mlog.Info($"Unload_Y_Step => UNLOAD_LD_Z_GRIP_DETECT(DI) : {Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_DETECT].ToString()}");
                        if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_DETECT] == true
                            || SingletonManager.instance.EquipmentMode == EquipmentMode.Dry)
                        {
                            Global.Mlog.Info($"Unload_Y_Step => Grip");
                            Dio_Output(DO_MAP.UNLOAD_LD_Z_GRIP, true);
                            UnloadYStep = Unload_Y_Step.Grip_Check;
                        }
                        else
                        {
                            SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] -= 1;
                            int floor = SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo];
                            SingletonManager.instance.Display_Lift[SingletonManager.instance.UnLoadStageNo].Floor[floor] = false;
                            UnloadYStep = Unload_Y_Step.Move_Y_PickUp_Done;

                            Global.Mlog.Info($"Unload_Y_Step => Next Floor : {floor.ToString()}");
                        }
                    }
                    break;
                case Unload_Y_Step.Grip_Check:
                    if (Dio.DI_RAW_DATA[(int)DI_MAP.UNLOAD_LD_Z_GRIP_CYL] == true)
                    {
                        Ez_Model.MoveReadyPosZ();
                        UnloadYStep = Unload_Y_Step.Move_Z_Ready_Done;

                        Global.Mlog.Info($"Unload_Y_Step => MoveReadyPosZ");
                        Global.Mlog.Info($"Unload_Y_Step => Move_Z_Ready_Done");
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
                    }
                    break;
                case Unload_Y_Step.Move_Z_PutDown_Done:
                    if (Ez_Model.IsMovePutDownPosZ() == true)
                    {
                        Global.Mlog.Info($"Unload_Y_Step => IsMovePutDownPosZ Done");
                        Global.Mlog.Info($"Unload_Y_Step => Ungrip");
                        Dio_Output(DO_MAP.UNLOAD_LD_Z_GRIP, false);
                        Global.instance.UnLoadCountPlus();
                        UnloadYStep = Unload_Y_Step.UnGrip_Check;

                        Global.Mlog.Info($"Unload_Y_Step => UnGrip_Check");
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

                        SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] -= 1;
                        int floor = SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo];
                        SingletonManager.instance.Display_Lift[SingletonManager.instance.UnLoadStageNo].Floor[floor] = false;
                       
                        UnloadYStep = Unload_Y_Step.Idle;
                        Global.Mlog.Info($"Unload_Y_Step => Next Floor : {floor.ToString()}");
                        Global.Mlog.Info($"Unload_Y_Step => Next Floor : Idle");
                    }
                    break; 
            }
            int step = (int)UnloadYStep;
            Global.instance.Write_Sequence_Log("UNLOAD_Y_STEP", step.ToString());
            Global.instance.Write_Sequence_Log("UNLOAD_FLOOW", SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo].ToString());
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
                SingletonManager.instance.UnLoadStageNo = 0;
                SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] = 0;
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
            SingletonManager.instance.UnLoadStageNo = Convert.ToInt32(value);
            value = myIni.Read("UNLOAD_FLOOW", "SEQUENCE");
            SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] = Convert.ToInt32(value);
            //value = myIni.Read("RTN_BTM_DONE", "SEQUENCE");
            //if (value == "") UnclampBottomReturnDone = false;
            //else    UnclampBottomReturnDone = bool.Parse(value); 
        }
        private void StageCheck()
        {
            if (SingletonManager.instance.UnLoadFloor[SingletonManager.instance.UnLoadStageNo] == 0)
            {
                if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_1_JIG_OUT_2] == true)
                {
                    SingletonManager.instance.UnLoadStageNo = (int)Lift_Index.Lift_1;
                }
                else if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_2_JIG_OUT_2] == true)
                {
                    SingletonManager.instance.UnLoadStageNo = (int)Lift_Index.Lift_2;
                }
                else if (Dio.DI_RAW_DATA[(int)DI_MAP.LIFT_3_JIG_OUT_2] == true)
                {
                    SingletonManager.instance.UnLoadStageNo = (int)Lift_Index.Lift_3;
                }
            }
        }
    }
}
