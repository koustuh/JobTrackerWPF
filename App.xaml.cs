using JobTrackerWPF.Data;
using System.Windows;

namespace JobTrackerWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Database.Initialize();
        }
    }
}
