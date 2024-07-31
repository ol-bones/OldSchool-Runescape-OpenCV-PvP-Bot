using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Automation;
using System.Drawing;
using System.Diagnostics;


namespace Lol3
{
    internal class WindowFinder
    {
        //const string TargetName = "Untitled - Paint";
        const string TargetName = "RuneLite";

        Condition propCondition = new PropertyCondition(
            AutomationElement.ClassNameProperty, TargetName, PropertyConditionFlags.IgnoreCase);

        public WindowFinder()
        {

        }

        public AutomationElement FindRunescape()
        {
            AutomationElement rs = AutomationElement.RootElement.FindFirst(TreeScope.Children, propCondition);

            AutomationElementCollection rss = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);


            foreach (AutomationElement e in rss)
            {
                if (e.Current.Name.StartsWith(TargetName))
                {
                    return e;
                    //OnAddTrackedWindowEventHandler(e, null);
                }
            }

            //if (rs != null)
           // {
           //     OnAddTrackedWindowEventHandler(rs, null);
           // }

            // track windows open (requires WindowsBase, UIAutomationTypes, UIAutomationClient)
            // commented for now idk if needed and i cba
            // Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Subtree, (s, e) =>
            //{
            //   OnAddTrackedWindowEventHandler(s, e);
            //});

            return rs;
        }
    }
}
