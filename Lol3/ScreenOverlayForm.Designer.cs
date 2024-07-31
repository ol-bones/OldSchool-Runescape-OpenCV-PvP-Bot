using System;

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

using System.Windows;
using System.Windows.Forms;
using System.Windows.Automation;

namespace Lol3
{
    partial class ScreenOverlayForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private readonly HashSet<AutomationElement> _windows = new HashSet<AutomationElement>();

        private Dictionary<string, (bool Visible, Rect rect)> targetRects = new Dictionary<string, (bool, Rect)>();

        private static Mutex mutex = new Mutex();
        private static Mutex mutex2 = new Mutex();
        public void Setup()
        {
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            TransparencyKey = BackColor;
            DoubleBuffered = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                const int WS_EX_TRANSPARENT = 0x20;
                const int WS_EX_LAYERED = 0x80000;
                const int WS_EX_NOACTIVATE = 0x8000000;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public void AddTargetRect(string name, double w, double h)
        {
            try
            {
                mutex.WaitOne();
                this.targetRects.Add(name, (false, new Rect(0, 0, w, h)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public void SetTargetRectVisible(string name, bool visible)
        {
            try
            {
                mutex.WaitOne();

                double oldW = this.targetRects[name].rect.Width;
                double oldH = this.targetRects[name].rect.Height;
                double x = this.targetRects[name].rect.X;
                double y = this.targetRects[name].rect.Y;

                this.targetRects[name] = (visible, new Rect(x, y, oldW, oldH));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        public void SetTargetRectPosition(string name, double x, double y)
        {
            try
            {
                mutex.WaitOne();

                double oldW = this.targetRects[name].rect.Width;
                double oldH = this.targetRects[name].rect.Height;
                bool oldVisiblity = this.targetRects[name].Visible;

                this.targetRects[name] = (oldVisiblity, new Rect(x, y, oldW, oldH));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public void SetSelectedInventoryScreen(string name)
        {
            try
            {
                mutex.WaitOne();

                selectedInventoryScreen = name;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        public void SetMouseRGB((double, double, double) rgb)
        {
            try
            {
                mutex.WaitOne();

                MouseRGB = (rgb.Item1, rgb.Item2, rgb.Item3);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        public void VengeModeOn()
        {
            try
            {
                mutex.WaitOne();

                vengeMode = true;
                bridMode = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        public void BridModeOn()
        {
            try
            {
                mutex.WaitOne();

                vengeMode = false;
                bridMode = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public void setActionsCount(int curActions)
        {
            try
            {
                mutex.WaitOne();

                actions = curActions;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        static DateTime _lastTime; // marks the beginning the measurement began
        static int _framesRendered; // an increasing count
        static int _fps; // the FPS calculated from the last measurement

        static FontFamily fontFamily = new FontFamily("Times New Roman");
        static Font font = new Font(fontFamily, 32, FontStyle.Bold, GraphicsUnit.Pixel);
        static SolidBrush solidBrush = new SolidBrush(Color.FromArgb(255, 255, 0, 0));

        static string selectedInventoryScreen = "Other";

        static (double, double, double) MouseRGB = (0.0, 0.0, 0.0);

        static bool vengeMode = false;
        static bool bridMode = false;
        static int actions = -1;

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);
                mutex.WaitOne();

                _framesRendered++;

                if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
                {
                    // one second has elapsed 

                    _fps = _framesRendered;
                    _framesRendered = 0;
                    _lastTime = DateTime.Now;
                }

                foreach (var window in _windows.ToArray())
                {
                    Rect rect;
                    try
                    {
                        rect = window.Current.BoundingRectangle;
                    }
                    catch
                    {
                        // error, window's gone
                        _windows.Remove(window);
                        continue;
                    }

                    // draw a yellow rectangle around window
                    using (var pen = new Pen(Color.Red, 20))
                    {
                        e.Graphics.DrawRectangle(pen, (float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
                    }
                    using (var pen = new Pen(Color.Green, 2))
                    {
                        foreach (var targetRect in targetRects.Values)
                        {
                            if (!targetRect.Visible) continue;

                            e.Graphics.DrawRectangle(pen,
                                (float)targetRect.rect.X,
                                (float)targetRect.rect.Y,
                                (float)targetRect.rect.Width,
                                (float)targetRect.rect.Height
                            );
                        }
                    }
                }
                Mouse.MousePoint pos = Mouse.GetCursorPosition();

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

                int si = 0;
                foreach(var state in ObjectFinder.Prayer_States)
                {
                    if (state.Value == true)
                    {
                        e.Graphics.DrawString(state.Key + " on", font, solidBrush, new PointF(5120 - 300, 350 + (50*si)));
                        si++;
                    }
                }

                using (SolidBrush colourBrush = new SolidBrush(Color.FromArgb(255, (int)MouseRGB.Item1, (int)MouseRGB.Item2, (int)MouseRGB.Item3)))
                {
                    e.Graphics.DrawString(MouseRGB.ToString(), font, colourBrush, new PointF(5120 - 300, 300));
                }

                e.Graphics.DrawString(selectedInventoryScreen, font, solidBrush, new PointF(5120 - 300, 250));

                e.Graphics.DrawString(pos.ToString(), font, solidBrush, new PointF(5120 - 300, 200));

                e.Graphics.DrawString("FPS: " + _fps.ToString(), font, solidBrush, new PointF(5120-300, 100));

                e.Graphics.DrawString(vengeMode ? "Venge Mode" : bridMode ? "Brid Mode" : "No Mode", font, solidBrush, new PointF(5120 - 300, 50));
                e.Graphics.DrawString(String.Format("Actions: {0}", actions), font, solidBrush, new PointF(5120 - 500, 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        // ensure we call Invalidate on UI thread
        public void InvokeInvalidate()
        {
            try
            {
                mutex2.WaitOne();
                BeginInvoke((Action)(() => { Invalidate(); }));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex2.ReleaseMutex();
            }
        }

        public void RemoveTrackedWindow(AutomationElement element)
        {
            try
            {
                mutex.WaitOne();

                _windows.Remove(element);
                InvokeInvalidate();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public void AddTrackedWindow(AutomationElement element)
        {
            try
            {
                mutex.WaitOne();

                _windows.Add(element);
                InvokeInvalidate();

                // follow target window position
                Automation.AddAutomationPropertyChangedEventHandler(element, TreeScope.Element, (s, e) =>
                {
                    InvokeInvalidate();
                }, AutomationElement.BoundingRectangleProperty);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);

            this.Setup();
        }

        #endregion
    }
}