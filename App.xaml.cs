using System.Windows;
using calendarrrrrrrrrr.Data;

namespace calendarrrrrrrrrr
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DatabaseService.Initialize();
        }
    }
}
