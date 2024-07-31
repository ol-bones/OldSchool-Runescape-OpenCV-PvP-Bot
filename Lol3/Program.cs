using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Automation;
using System.Drawing;
using System.Diagnostics;

namespace Lol3
{
    internal static class Program
    {

        public static ScreenOverlayForm Overlay;
        public static ObjectFinder Finder;
        public static KeyBinder Binder;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Overlay = new ScreenOverlayForm();
            Finder = new ObjectFinder();
            Binder = new KeyBinder(Finder);

            // probably a race condition to wait for the window to actually open
            Task.Run(() =>
            {
                OnAddTrackedWindowEventHandler(new WindowFinder().FindRunescape());

                Finder.Initialise(Overlay, Binder);
            });

            Application.Run(Overlay);
        }

        public static void OnAddTrackedWindowEventHandler(AutomationElement window)
        {
            if (window == null) return;

            try
            {
                Console.WriteLine("Added window '" + window.Current.Name + "'");
                Overlay.AddTrackedWindow(window);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                OnTrackedWindowClosedEventHandler(window);
            }
        }

        public static void OnTrackedWindowClosedEventHandler(AutomationElement element)
        {
            Overlay.RemoveTrackedWindow(element);
        }
    }
}
