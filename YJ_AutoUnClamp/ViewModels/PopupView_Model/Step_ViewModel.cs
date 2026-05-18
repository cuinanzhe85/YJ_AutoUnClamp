using Common.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Timers;
using YJ_AutoUnClamp.Models;

namespace YJ_AutoUnClamp.ViewModels.PopupView_Model
{
    public class Step_Model : BindableAndDisposable
    {
        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { SetValue(ref _Title, value); }
        }
        private int _UnClampStep;
        public int UnClampStep
        {
            get { return _UnClampStep; }
            set { SetValue(ref _UnClampStep, value); }
        }
        private int _RtnBtmStep;
        public int RtnBtmStep
        {
            get { return _RtnBtmStep; }
            set { SetValue(ref _RtnBtmStep, value); }
        }
        private int _RtnTopStep;
        public int RtnTopStep
        {
            get { return _RtnTopStep; }
            set { SetValue(ref _RtnTopStep, value); }
        }
        private int _UnloadYStep;
        public int UnloadYStep
        {
            get { return _UnloadYStep; }
            set { SetValue(ref _UnloadYStep, value); }
        }
        private int _UnloadXlStep;
        public int UnloadXlStep
        {
            get { return _UnloadXlStep; }
            set { SetValue(ref _UnloadXlStep, value); }
        }
        private int _UnloadCvStep;
        public int UnloadCvStep
        {
            get { return _UnloadCvStep; }
            set { SetValue(ref _UnloadCvStep, value); }
        }
        private int _NgCVStep;
        public int NgCVStep
        {
            get { return _NgCVStep; }
            set { SetValue(ref _NgCVStep, value); }
        }
        private int _LiftStep;
        public int LiftStep
        {
            get { return _LiftStep; }
            set { SetValue(ref _LiftStep, value); }
        }

        public Step_Model(string title)
        {
            this.Title = title;
        }
    }
    public class Step_ViewModel : Child_ViewModel
    {
        private ObservableCollection<Step_Model> _Step_Model;
        public ObservableCollection<Step_Model> Step_Model
        {
            get { return _Step_Model; }
            set { SetValue(ref _Step_Model, value); }
        }
        private Timer StepTimer;
        public Step_ViewModel()
        {
            // Product Count Model 생성 및 데이터 할당
            Step_Model = new ObservableCollection<Step_Model>();
            Step_Model.Add(new Step_Model("Auto UnClamp"));

            StepTimer = new Timer(50); // 1초마다 업데이트
            StepTimer.Elapsed += UpdateStepDisplay;
            StepTimer.Start();
        }
        private void UpdateStepDisplay(object sender, ElapsedEventArgs e)
        {
            var unitModel = SingletonManager.instance.Unit_Model;
            if (unitModel.Count > 0)
            {
                Step_Model[0].UnClampStep = (int)unitModel[(int)MotionUnit_List.Top_X].UnClampStep;
                Step_Model[0].RtnBtmStep = (int)unitModel[(int)MotionUnit_List.Top_X].RtnBtmStep;
                Step_Model[0].RtnTopStep = (int)unitModel[(int)MotionUnit_List.Top_X].RtnTopStep;
                Step_Model[0].UnloadYStep = (int)unitModel[(int)MotionUnit_List.In_Y].UnloadYStep;
                Step_Model[0].UnloadXlStep = (int)unitModel[(int)MotionUnit_List.In_Y].UnloadXlStep;
                Step_Model[0].UnloadCvStep = (int)unitModel[(int)MotionUnit_List.In_CV].UnloadCvStep;
                Step_Model[0].NgCVStep = (int)unitModel[(int)MotionUnit_List.In_CV].NgCVStep;
                Step_Model[0].LiftStep = (int)unitModel[(int)MotionUnit_List.Lift_1].LiftStep;
            }
        }
        #region // Override
        protected override void InitializeCommands()
        {
            base.InitializeCommands();
        }
        protected override void DisposeManaged()
        {
            // ObservableCollection 해제

            if (Step_Model != null)
            {
                foreach (var item in Step_Model)
                    (item as IDisposable)?.Dispose();
                Step_Model.Clear();
                Step_Model = null;
            }
            if (StepTimer != null)
            {
                StepTimer.Stop();
                StepTimer.Elapsed -= UpdateStepDisplay;
                StepTimer.Dispose();
                StepTimer = null;
            }
            base.DisposeManaged();
        }
        #endregion
    }
}
