﻿using Common.Commands;
using Common.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Data;
using YJ_AutoUnClamp.Models;
using YJ_AutoUnClamp.Utils;

namespace YJ_AutoUnClamp.ViewModels
{
    public class Auto_ViewModel : Child_ViewModel
    {
        #region // ICommands
        public ICommand RightMenu_PopupCommand { get; private set; }
        public ICommand PointView_PopupCommand { get; private set; }
        #endregion
        #region // PopupManager
        enum AutoMenu_PopupList
        {
            Mode,
            Initialize,
            Origin,
            Dio,
            Info,
        }
        private readonly Dictionary<AutoMenu_PopupList, Func<(Window, Child_ViewModel)>> PopupFactories;
        #endregion
        #region // UI Properties
        private string _EquipmentMode = SingletonManager.instance.EquipmentMode.ToString();
        public string EquipmentMode
        {
            get { return _EquipmentMode; }
            set { SetValue(ref _EquipmentMode, value); }
        }
        public RadObservableCollection<bool> DisplayUI_Dio
        {
            get { return SingletonManager.instance.DisplayUI_Dio; }
        }
        public RadObservableCollection<Channel_Model> Channels
        {
            get { return SingletonManager.instance.Channel_Model; }
        }
        private bool _IsStartEnable = true;
        public bool IsStartEnable
        {
            get { return _IsStartEnable; }
            set { SetValue(ref _IsStartEnable, value); }
        }
        private bool _IsStopEnable = false;
        public bool IsStopEnable
        {
            get { return _IsStopEnable; }
            set { SetValue(ref _IsStopEnable, value); }
        }
        private ObservableCollection<Lift_Model> _LiftData = SingletonManager.instance.Display_Lift;
        public ObservableCollection<Lift_Model> LiftData
        {
            get { return _LiftData; }
        }
        #endregion
        public Auto_ViewModel()
        {
            PopupFactories = new Dictionary<AutoMenu_PopupList, Func<(Window, Child_ViewModel)>>
            {
                { AutoMenu_PopupList.Mode, () => (new EquipmentMode_View(), new EquipmentMode_ViewModel()) },
                { AutoMenu_PopupList.Initialize, () => (new Initialize_View(), new Initialize_ViewModel()) },
                { AutoMenu_PopupList.Origin, () => (new Origin_View(), new Origin_ViewModel()) },
                { AutoMenu_PopupList.Dio, () => (new Dio_View(), new Dio_ViewModel()) }
            };
        }
        private async void OnRightMenu_Command(object obj)
        {
            string cmd = obj.ToString();
            if (cmd == "Safety")
            {
                // Todo : In, Out Grip Check
                SingletonManager.instance.IsSafetyInterLock = true;
            }
            else if(cmd == "Start")
            {
                if (SingletonManager.instance.IsInspectionStart == false)
                    await Global.instance.InspectionStart();
            }
            else if(cmd == "Stop")
            {
                if (SingletonManager.instance.IsInspectionStart == true)
                    Global.instance.InspectionStop();
            }
            else
            {
                if (Enum.TryParse(obj.ToString(), out AutoMenu_PopupList popup))
                {
                    PopupManager.ShowPopupView(PopupFactories, popup);
                    if (popup == AutoMenu_PopupList.Mode)
                    {
                        EquipmentMode = SingletonManager.instance.EquipmentMode.ToString();
                    }
                }
            }
        }
        private void OnPointView_Command(object obj)
        {
            if(obj.ToString() == "0")
            {
                SingletonManager.instance.Channel_Model[0].Status = ChannelStatus.RUNNING;
                SingletonManager.instance.Channel_Model[0].Barcode = "12345678";
                SingletonManager.instance.Channel_Model[0].StartTactTime();
            }
            else if (obj.ToString() == "1")
            {
                SingletonManager.instance.Channel_Model[1].Status = ChannelStatus.OK;
                SingletonManager.instance.Channel_Model[1].Barcode = "12345678";
                SingletonManager.instance.Channel_Model[1].TactTime = "10";
            }
            else if (obj.ToString() == "2")
            {
                SingletonManager.instance.Channel_Model[2].Status = ChannelStatus.NG;
                SingletonManager.instance.Channel_Model[2].Barcode = "12345678";
                SingletonManager.instance.Channel_Model[2].TactTime = "10";
            }
            else
            {
                SingletonManager.instance.Channel_Model[0].StopTactTime();
            }
        }

        #region override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            RightMenu_PopupCommand = new RelayCommand(OnRightMenu_Command);
            PointView_PopupCommand = new RelayCommand(OnPointView_Command);
        }
        protected override void DisposeManaged()
        {
            // ICommands 해제
            RightMenu_PopupCommand = null;
            PointView_PopupCommand = null;

            // PopupFactories 해제
            if (PopupFactories != null)
            {
                PopupFactories.Clear();
            }

            base.DisposeManaged();
        }
        #endregion
    }
    public class Lift_Model : BindableAndDisposable
    {
        private string _LiftName;
        public string LiftName
        {
            get { return _LiftName; }
            set { SetValue(ref _LiftName, value); }
        }
        private ObservableCollection<bool> _Floor;
        public ObservableCollection<bool> Floor
        {
            get { return _Floor; }
            set { SetValue(ref _Floor, value); }
        }
        public Lift_Model(string lift)
        {
            Floor = new ObservableCollection<bool>();
            for (int i = 0; i < 7; i++)
                Floor.Add(false);

            this.LiftName = lift;
        }
    }
}