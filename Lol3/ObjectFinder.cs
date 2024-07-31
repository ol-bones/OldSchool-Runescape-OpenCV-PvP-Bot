using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Lol3
{
    class ScreenObject
    {
        public string Name = "undefined";
        public Mat templateMaterial;

        public int x, y, w, h;

        public bool expectedVisible;
        public bool isVisible;
        public bool neverVisible;

        public ScreenObject(string Name, Mat templateMaterial, int x, int y, int w, int h)
        {
            this.Name = Name;
            this.templateMaterial = templateMaterial;
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            this.expectedVisible = true;
            this.isVisible = true;

            this.neverVisible = false;
        }
    }

    enum Inventory_Screen
    {
        Inventory = 0,
        Attack_Style = 1,
        Prayer = 2,
        Spellbook = 3,
        Other = -255
    }

    internal class ObjectFinder
    {
        string TemplateFolder = "C:\\Users\\xxx\\source\\repos\\Lol3\\Lol3\\Content\\Templates\\";

        string[] TemplatesNames = { "inventory_frame_top_left.png", "inventory_button.png" };

        public Dictionary<string, Dictionary<string, ScreenObject>> ScreenObjects = new Dictionary<string, Dictionary<string, ScreenObject>>();

        public ScreenOverlayForm Overlay;

        bool Inventory_Position_Found = false;

        Rectangle ROI = new Rectangle();

        private static Mutex mutex = new Mutex();
        private static Mutex mutex2 = new Mutex();

        Mouse.MousePoint[] Inventory_Pixels = {
            new Mouse.MousePoint(3546,1127), // Inventory
            new Mouse.MousePoint(3367,1123), // Attack_Style
            new Mouse.MousePoint(3672,1128), // Prayer
            new Mouse.MousePoint(3736,1126)  // Spellbook
        };

        public static readonly string[] prayerNames = {
            "attack",
            "strength",
            "defence",
            "mystic",
            "eagle",
            "range",
            "mage",
            "melee",
            "protect",
            "smite"
        };

        public static readonly Mouse.MousePoint Venge_Pixel = new Mouse.MousePoint(3567,593);

        public static readonly Mouse.MousePoint[] Prayer_Pixels = {
            new Mouse.MousePoint(3456,780), // attack
            new Mouse.MousePoint(3722,716), // strength
            new Mouse.MousePoint(3663,710), // defence
            new Mouse.MousePoint(3445,858), // mystic
            new Mouse.MousePoint(3730,779), // eagle
            new Mouse.MousePoint(3593,788), // range
            new Mouse.MousePoint(3522,781), // mage
            new Mouse.MousePoint(3660,786), // melee
            new Mouse.MousePoint(3454,707), // protect
            new Mouse.MousePoint(3660,854)  // smite
        };

        public static Dictionary<string, Mouse.MousePoint> Prayer_Pixels_Dictionary = new Dictionary<string, Mouse.MousePoint>{
           {"attack", new Mouse.MousePoint(3456,780)},
           {"strength", new Mouse.MousePoint(3722,716)},
           {"defence", new Mouse.MousePoint(3663,710)},
           {"mystic", new Mouse.MousePoint(3445,858)},
           {"eagle", new Mouse.MousePoint(3730,779)},
           {"range", new Mouse.MousePoint(3593,788)},
           {"mage", new Mouse.MousePoint(3522,781)},
           {"melee", new Mouse.MousePoint(3660,786)},
           {"protect", new Mouse.MousePoint(3454,707)},
           {"smite", new Mouse.MousePoint(3660,854)}
        };

        public static Dictionary<string, bool> Prayer_States = new Dictionary<string, bool>{
           {"attack", false},
           {"strength", false},
           {"defence", false},
           {"mystic", false},
           {"eagle", false},
           {"range", false},
           {"mage", false},
           {"melee", false},
           {"protect", false},
           {"smite", false}
        };

        string[] pointNames = {
            "Inventory",
            "Attack_Style",
            "Prayer",
            "Spellbook",
        };

        static Inventory_Screen SelectedScreen = Inventory_Screen.Other;

        KeyBinder Binder;

        public ObjectFinder()
        {
        
        }

        static bool isHandling = false;
        public void Initialise(ScreenOverlayForm overlayRef, KeyBinder binderRef)
        {
            this.Overlay = overlayRef;
            this.Binder = binderRef;

            LoadTemplates();

            var screenStateLogger = new Screenshotter();
            screenStateLogger.ScreenRefreshed += async (sender, bitmap) =>
            {
                try
                {
                    if (isHandling) return;

                    isHandling = true;

                    using (Mat Screen = Emgu.CV.BitmapExtension.ToMat(bitmap))
                    {
                        await Task.Run(() => DoMatching(Screen));
                    }
                    isHandling = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    isHandling = false;
                }
            };

            screenStateLogger.Start();
        }

        public void DoMatching(Mat Screen)
        {
            try
            {
                using (Image<Gray, Byte> GreyScreen = Screen.ToImage<Gray, Byte>(true))
                {
                    mutex.WaitOne();

                    ScreenObject Inventory_Bottom_Left_Buttons = ScreenObjects["Misc"]["inventory_buttons_bottom_left.png"];

                    if (!Inventory_Position_Found)
                    {
                        Find_Inventory_Position(GreyScreen);
                    }
                    else
                    {
                        using (Image<Bgr, Byte> BgrScreen = Screen.ToImage<Bgr, Byte>(true))
                        {
                            SelectedScreen = Get_Open_Inventory_Screen(BgrScreen);
                            switch (SelectedScreen)
                            {
                                case Inventory_Screen.Inventory:
                                    this.Overlay.SetSelectedInventoryScreen("Inventory");
                                        break;
                                case Inventory_Screen.Attack_Style:
                                    this.Overlay.SetSelectedInventoryScreen("Attack_Style");
                                    break;
                                case Inventory_Screen.Prayer:
                                    this.Overlay.SetSelectedInventoryScreen("Prayer");
                                    break;
                                case Inventory_Screen.Spellbook:
                                    this.Overlay.SetSelectedInventoryScreen("Spellbook");
                                    break;
                                default:
                                    this.Overlay.SetSelectedInventoryScreen("Other");
                                    break;
                            }

                            Mouse.MousePoint pos = Mouse.GetCursorPosition();
                            Bgr mousePixel = BgrScreen[pos.Y, Math.Max(0, pos.X - (int)(5120 * 0.65))];
                            this.Overlay.SetMouseRGB((mousePixel.Red, mousePixel.Green, mousePixel.Blue));

                            if (SelectedScreen == Inventory_Screen.Prayer)
                            {
                                GetPrayerStates(BgrScreen);
                            }

                            GreyScreen.ROI = this.ROI;

                            if (SelectedScreen == Inventory_Screen.Inventory)
                            {
                                Find_Templates(GreyScreen, "Inventory_Items", 250000);
                            }
                            else
                            {
                                foreach (var targetObject in ScreenObjects["Inventory_Items"])
                                {
                                    this.Overlay.SetTargetRectVisible(targetObject.Key, false);
                                    targetObject.Value.expectedVisible = false;
                                }
                            }

                            if (SelectedScreen == Inventory_Screen.Spellbook)
                            {
                                bool vengeAvailable = VengePixelRed(BgrScreen);
                                if (vengeAvailable)
                                {
                                    // need a higher threshold for this idk why
                                    Find_Templates(GreyScreen, "Spellbook", 1000000);
                                }
                                else
                                {
                                    Find_Templates(GreyScreen, "Spellbook", 250000);
                                }
                            }
                            else
                            {
                                foreach (var targetObject in ScreenObjects["Spellbook"])
                                {
                                    this.Overlay.SetTargetRectVisible(targetObject.Key, false);
                                    targetObject.Value.expectedVisible = false;
                                }
                            }
                        }

                        this.Binder.Update(SelectedScreen, ScreenObjects);
                        this.Overlay.InvokeInvalidate();
                    }
                }
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

        public void ResetPrayerStates()
        {
            foreach (var state in prayerNames)
            {
                Prayer_States[state] = false;
            }
        }

        public bool VengePixelRed(Image<Bgr, Byte> BgrScreen)
        {
            Bgr pixel1 = BgrScreen[Venge_Pixel.Y + 1, Venge_Pixel.X - (int)(5120 * 0.65)];
            Bgr pixel2 = BgrScreen[Venge_Pixel.Y + 1, Venge_Pixel.X + 1 - (int)(5120 * 0.65)];
            Bgr pixel3 = BgrScreen[Venge_Pixel.Y, Venge_Pixel.X + 1 - (int)(5120 * 0.65)];
            Bgr pixel4 = BgrScreen[Venge_Pixel.Y, Venge_Pixel.X - (int)(5120 * 0.65)];

            double redThreshold = 230;
            double greenThreshold = 60;
            double blueThreshold = 60;

            bool pixel1_bright = pixel1.Red > redThreshold && pixel1.Green <= greenThreshold && pixel1.Blue <= blueThreshold;
            bool pixel2_bright = pixel2.Red > redThreshold && pixel2.Green <= greenThreshold && pixel2.Blue <= blueThreshold;
            bool pixel3_bright = pixel3.Red > redThreshold && pixel3.Green <= greenThreshold && pixel3.Blue <= blueThreshold;
            bool pixel4_bright = pixel4.Red > redThreshold && pixel4.Green <= greenThreshold && pixel4.Blue <= blueThreshold;

            return pixel1_bright && pixel2_bright && pixel3_bright && pixel4_bright;
        }

        public void GetPrayerStates(Image<Bgr, Byte> BgrScreen)
        {
            ResetPrayerStates();

            int i = 0;
            foreach (var state in prayerNames)
            {
                Bgr pixel1 = BgrScreen[Prayer_Pixels[i].Y + 1, Prayer_Pixels[i].X - (int)(5120 * 0.65)];
                Bgr pixel2 = BgrScreen[Prayer_Pixels[i].Y + 1, Prayer_Pixels[i].X + 1 - (int)(5120 * 0.65)];
                Bgr pixel3 = BgrScreen[Prayer_Pixels[i].Y, Prayer_Pixels[i].X + 1 - (int)(5120 * 0.65)];
                Bgr pixel4 = BgrScreen[Prayer_Pixels[i].Y, Prayer_Pixels[i].X - (int)(5120 * 0.65)];

                double redThreshold = 120;
                double greenThreshold = 120;
                double blueThreshold = 90;

                bool pixel1_bright = pixel1.Red > redThreshold && pixel1.Green > greenThreshold && pixel1.Blue > blueThreshold;
                bool pixel2_bright = pixel2.Red > redThreshold && pixel2.Green > greenThreshold && pixel2.Blue > blueThreshold;
                bool pixel3_bright = pixel3.Red > redThreshold && pixel3.Green > greenThreshold && pixel3.Blue > blueThreshold;
                bool pixel4_bright = pixel4.Red > redThreshold && pixel4.Green > greenThreshold && pixel4.Blue > blueThreshold;

                Prayer_States[state] = pixel1_bright && pixel2_bright && pixel3_bright && pixel4_bright;

                i++;
            }
        }

        public Inventory_Screen Get_Open_Inventory_Screen(Image<Bgr, Byte> BgrScreen)
        {
            for (int i = 0; i < this.Inventory_Pixels.Length; i++)
            {
                Bgr pixel1 = BgrScreen[this.Inventory_Pixels[i].Y + 1, this.Inventory_Pixels[i].X - (int)(5120 * 0.65)];
                Bgr pixel2 = BgrScreen[this.Inventory_Pixels[i].Y + 1, this.Inventory_Pixels[i].X + 1 - (int)(5120 * 0.65)];
                Bgr pixel3 = BgrScreen[this.Inventory_Pixels[i].Y, this.Inventory_Pixels[i].X + 1 - (int)(5120 * 0.65)];
                Bgr pixel4 = BgrScreen[this.Inventory_Pixels[i].Y, this.Inventory_Pixels[i].X - (int)(5120 * 0.65)];

                double redThreshold = 100;

                bool pixel1_red = pixel1.Red > pixel1.Blue + pixel1.Green && pixel1.Red > redThreshold;
                bool pixel2_red = pixel2.Red > pixel2.Blue + pixel2.Green && pixel2.Red > redThreshold;
                bool pixel3_red = pixel3.Red > pixel3.Blue + pixel3.Green && pixel3.Red > redThreshold;
                bool pixel4_red = pixel4.Red > pixel4.Blue + pixel4.Green && pixel4.Red > redThreshold;

                if (pixel1_red && pixel2_red && pixel3_red && pixel4_red)
                {
                    return (Inventory_Screen)i;
                }
            }

            return Inventory_Screen.Other;
        }

        public void Find_Templates(Image<Gray, Byte> GreyScreen, string TemplateCategory, double threshold)
        {
            try
            {
                Parallel.ForEach(ScreenObjects[TemplateCategory].Where(x => x.Value.neverVisible == false), (targetObject) =>
                {
                    targetObject.Value.expectedVisible = true;

                    Mat result = new Mat();
                    CvInvoke.MatchTemplate(GreyScreen, targetObject.Value.templateMaterial, result, Emgu.CV.CvEnum.TemplateMatchingType.Sqdiff);


                    CvInvoke.Threshold(result, result, 0.8, 1, ThresholdType.ToZero);
                    result.MinMax(out double[] minVal, out double[] maxVal, out Point[] minLoc, out Point[] maxLoc);

                    int x = minLoc[0].X;
                    int y = minLoc[0].Y;
                    int w = maxLoc[0].X + targetObject.Value.w;
                    int h = maxLoc[0].Y + targetObject.Value.h;

                    if (x != 0 && y != 0 && minVal[0] < threshold)
                    {
                        this.Overlay.SetTargetRectPosition(targetObject.Key, this.ROI.X + x + (int)(5120 * 0.65), this.ROI.Y + y);
                        this.Overlay.SetTargetRectVisible(targetObject.Key, true);

                        targetObject.Value.isVisible = true;
                        targetObject.Value.x = this.ROI.X + x + (int)(5120 * 0.65);
                        targetObject.Value.y = this.ROI.Y + y;
                    }
                    else
                    {
                        targetObject.Value.isVisible = false;
                        this.Overlay.SetTargetRectVisible(targetObject.Key, false);
                    }
                });

                foreach (var targetObject in ScreenObjects[TemplateCategory].Where(x => x.Value.neverVisible == true && x.Value.expectedVisible == true))
                {
                    this.Overlay.SetTargetRectVisible(targetObject.Key, false);
                    targetObject.Value.expectedVisible = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Find_Inventory_Position(Image<Gray, Byte> GreyScreen)
        {
            try
            {
                ScreenObject Inventory_Bottom_Left_Buttons = ScreenObjects["Misc"]["inventory_buttons_bottom_left.png"];

                if (Inventory_Bottom_Left_Buttons == null)
                {
                    Inventory_Bottom_Left_Buttons.x = 0;
                    Inventory_Bottom_Left_Buttons.y = 0;
                    Inventory_Bottom_Left_Buttons.isVisible = false;
                    Inventory_Bottom_Left_Buttons.expectedVisible = true;

                    throw new FileNotFoundException("Inventory button object not loaded");
                }

                Mat result = new Mat();
                CvInvoke.MatchTemplate(GreyScreen, Inventory_Bottom_Left_Buttons.templateMaterial, result, Emgu.CV.CvEnum.TemplateMatchingType.Sqdiff);

                result.MinMax(out double[] minVal, out double[] maxVal, out Point[] minLoc, out Point[] maxLoc);

                int x = minLoc[0].X;
                int y = minLoc[0].Y;
                int w = maxLoc[0].X + Inventory_Bottom_Left_Buttons.w;
                int h = maxLoc[0].Y + Inventory_Bottom_Left_Buttons.h;


                if (x != 0 && y != 0)
                {
                    this.Overlay.SetTargetRectPosition(Inventory_Bottom_Left_Buttons.Name, x + (int)(5120 * 0.65), y);
                    this.Overlay.SetTargetRectVisible(Inventory_Bottom_Left_Buttons.Name, true);

                    Inventory_Bottom_Left_Buttons.x = x;
                    Inventory_Bottom_Left_Buttons.y = y;

                    Inventory_Bottom_Left_Buttons.isVisible = true;
                    Inventory_Bottom_Left_Buttons.expectedVisible = true;

                    Inventory_Position_Found = true;

                    int Inventory_Height = 490;
                    int Inventory_Height_Offset = 20;

                    int Inventory_Width = 360;
                    int Inventory_Width_Offset = 70;

                    int Inventory_Y = y - Inventory_Height - Inventory_Height_Offset;
                    int Inventory_X = x + Inventory_Width_Offset;

                    this.Overlay.AddTargetRect("Inventory", Inventory_Width, Inventory_Height);
                    this.Overlay.SetTargetRectPosition("Inventory", Inventory_X + (int)(5120 * 0.65), Inventory_Y);
                    this.Overlay.SetTargetRectVisible("Inventory", true);

                    this.ROI.X = Inventory_X;
                    this.ROI.Y = Inventory_Y;
                    this.ROI.Width = Inventory_Width;
                    this.ROI.Height = Inventory_Height;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void LoadTemplates()
        {
            string[] ObjectDirs = Directory.GetDirectories(TemplateFolder);

            foreach (string ObjectDir in ObjectDirs)
            {
                DirectoryInfo subDirectory = new DirectoryInfo(ObjectDir);
                string subdirectoryName = subDirectory.Name;

                FileInfo[] templateFiles = subDirectory.GetFiles("*.png");

                ScreenObjects.Add(subdirectoryName, new Dictionary<string, ScreenObject>());

                foreach (var template in templateFiles)
                {
                    try
                    {
                        Mat mat = CvInvoke.Imread(template.FullName, Emgu.CV.CvEnum.ImreadModes.Grayscale);

                        this.Overlay.AddTargetRect(template.Name, mat.Width, mat.Height);

                        ScreenObjects[subdirectoryName].Add(template.Name, new ScreenObject(
                            template.Name,
                            mat,
                            0,
                            0,
                            mat.Width,
                            mat.Height
                        ));

                        Console.WriteLine("Loaded template mat: {0}", template);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}
