using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace upSidetone.Util;

// via https://stackoverflow.com/a/20453504

/*

Usage:

CommonUtil.Run(() =>
{
    // some actions
}, TimeSpan.FromMilliseconds(5000));

*/

public static class ActionExtensions {
    private static System.Windows.Forms.Timer dispatcherTimer;
    //private static DispatcherTimer dispatcherTimer;
    public static void RunAfter(this Action action, TimeSpan span) {
        //dispatcherTimer = new DispatcherTimer { Interval = span };
        dispatcherTimer = new System.Windows.Forms.Timer { Interval = (int)span.TotalMilliseconds };
        dispatcherTimer.Tick += (sender, args) => {
            var timer = sender as DispatcherTimer;
            timer?.Stop();
            action();
        };
        dispatcherTimer.Start();
    }
}

public static class CommonUtil {
    public static void Run(Action action, TimeSpan afterSpan) {
        action.RunAfter(afterSpan);
    }

}