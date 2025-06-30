using Common.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YJ_AutoUnClamp.Models
{
    public class Aging_Model
    {
        public Aging_Model() { }
        public bool AgingTimeCheck(int index)
        {
            if (SingletonManager.instance.TcpClient.TcpReceiveData != "")
            if (!string.IsNullOrEmpty(SingletonManager.instance.TcpClient.TcpReceiveData))
            {
                string nowTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                DateTime now = DateTime.ParseExact(nowTime, "yyyyMMddHHmmss", null);

                DateTime Loading = DateTime.ParseExact(SingletonManager.instance.TcpClient.TcpReceiveData, "yyyyMMddHHmmss", null);
                
                TimeSpan diff = now - Loading;

                int AgingTimeSec = Convert.ToInt32(SingletonManager.instance.SystemModel.AgingTime);
                if (AgingTimeSec < diff.TotalMinutes)
                {
                    Global.Mlog.Info($"Lift_Step => Clamp Input Time [{SingletonManager.instance.TcpClient.TcpReceiveData}]");
                    Global.Mlog.Info($"Lift_Step => UnClamp Input Time [{now.ToString()}]");
                    Global.Mlog.Info($"Lift_Step => Aging Time Setting [{AgingTimeSec.ToString()}]");
                    return true;
                }
            }
            return false;
        }
    }
}
