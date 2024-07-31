using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lol3
{
    internal class KeyBinder
    {
        globalKeyboardHook keyHook;

        ObjectFinder Finder;


        private static Mutex mutex = new Mutex();

        public List<Action> Actions = new List<Action>();
        bool LastResult = false;
        bool LastActionComplete = true;

        public KeyBinder(ObjectFinder finder)
        {
            this.Finder = finder;

            keyHook = new globalKeyboardHook();

            keyHook.HookedKeys.Add(Keys.PageUp);
            keyHook.HookedKeys.Add(Keys.PageDown);

            keyHook.KeyDown += new KeyEventHandler(gkh_KeyDown);
            Finder = finder;
        }

        void gkh_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                mutex.WaitOne();
                Console.WriteLine("KeyDown: {0}", e.KeyCode.ToString());
                e.Handled = true;

                if (e.KeyCode == Keys.PageUp)
                {
                    foreach (var category in this.Finder.ScreenObjects)
                    {
                        foreach (var screenObject in category.Value)
                        {
                            if (ItemPicker.StaffTypes.Contains(screenObject.Value.Name) ||
                                ItemPicker.MagicTopTypes.Contains(screenObject.Value.Name) ||
                                ItemPicker.MagicBottomTypes.Contains(screenObject.Value.Name))
                            {
                                screenObject.Value.neverVisible = true;
                            }
                        }
                    }

                    this.Finder.Overlay.VengeModeOn();

                    if (!keyHook.HookedKeys.Contains(Keys.V))
                    {
                        keyHook.HookedKeys.Add(Keys.V);
                    }

                    if(!keyHook.HookedKeys.Contains(Keys.Q))
                    {
                        keyHook.HookedKeys.Add(Keys.Q);
                    }

                    if (keyHook.HookedKeys.Contains(Keys.E))
                    {
                        keyHook.HookedKeys.Remove(Keys.E);
                    }
                }

                if (e.KeyCode == Keys.PageDown)
                {
                    foreach (var category in this.Finder.ScreenObjects)
                    {
                        foreach (var screenObject in category.Value)
                        {
                            if (ItemPicker.StaffTypes.Contains(screenObject.Value.Name) ||
                                ItemPicker.MagicTopTypes.Contains(screenObject.Value.Name) ||
                                ItemPicker.MagicBottomTypes.Contains(screenObject.Value.Name))
                            {
                                screenObject.Value.neverVisible = false;
                            }
                        }
                    }

                    this.Finder.Overlay.BridModeOn();

                    if (!keyHook.HookedKeys.Contains(Keys.V))
                    {
                        keyHook.HookedKeys.Remove(Keys.V);
                    }

                    if (!keyHook.HookedKeys.Contains(Keys.Q))
                    {
                        keyHook.HookedKeys.Add(Keys.Q);
                    }

                    if (keyHook.HookedKeys.Contains(Keys.E))
                    {
                        keyHook.HookedKeys.Add(Keys.E);
                    }
                }

                if (e.KeyCode == Keys.V)
                {
                    VengeAndEquipMaul();
                }

                if (e.KeyCode == Keys.E)
                {
                    Equip3ItemsWithChecks(new string[]
                    {
                        "black_dhide_inventory.png",
                        "black_dhide_legs_inventory.png",
                        ItemPicker.PickBest("Range_Weapon", ref this.ScreenObjects)
                    });
                }

                if (e.KeyCode == Keys.Q)
                {
                    Equip3ItemsWithChecksAndSelectSpell(new string[]
                    {
                        ItemPicker.PickBest("Magic_Top", ref this.ScreenObjects),
                        ItemPicker.PickBest("Magic_Bottoms", ref this.ScreenObjects),
                        ItemPicker.PickBest("Staff", ref this.ScreenObjects),
                        ItemPicker.PickBest("Offhand", ref this.ScreenObjects)
                    }, "ice_barrage_spellbook.png");
                }
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

        Inventory_Screen CurrentScreen;
        Dictionary<string, Dictionary<string, ScreenObject>> ScreenObjects;

        bool ActionError = false;
        public void Update(Inventory_Screen currentScreen, Dictionary<string, Dictionary<string, ScreenObject>> screenObjects)
        {
            try
            {
                mutex.WaitOne();

                this.Finder.Overlay.setActionsCount(Actions.Count);

                if (ActionError)
                {
                    Actions.Clear();
                    LastActionComplete = true;
                    ActionError = false;
                }

                this.CurrentScreen = currentScreen;
                this.ScreenObjects = screenObjects;

                if (Actions.Count > 0 && LastActionComplete)
                {
                    Action currentAction = Actions.First();
                    Actions.RemoveAt(0);

                    LastActionComplete = false;
                    Task.Run(() => currentAction.Invoke());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Actions.Clear();
                LastActionComplete = true;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public void VengeAndEquipMaul()
        {
            if (Actions.Count == 0)
            {
                Actions.AddRange(new Action[]
                {
                    () => OpenSpellbook(),
                    () => LastResult = Wait(new Random().Next(75,100)),
                    () => LastResult = CheckSpellbookOpen(),
                    () =>
                    {
                        if(LastResult == true)
                        {
                            Actions.AddRange(new Action[]
                            {
                                () => LastResult = SelectSpell("venge_spellbook.png"),
                                () => LastResult = Wait(new Random().Next(5,25)),
                                () => OpenPrayer(),
                                () => LastResult = Wait(new Random().Next(75,100)),
                            //    () => LastResult = CheckPrayerOpen(),
                            //    () =>
                             //   {
                             //       if(LastResult == true)
                          //          {
                                  //      Actions.AddRange(new Action[]
                                  //      {
                                            () => SelectPrayer("attack"),
                                            () => SelectPrayer("strength"),
                                            () => SelectPrayer("smite"),
                                        //    () =>
                                         //   {
                                          //      if(LastResult)
                                            //    {
                                            //        Actions.AddRange(new Action[]
                                                  //  {
                                                        () => LastResult = OpenInventory(),
                                                        () => LastResult = Wait(new Random().Next(75,100)),
                                                        () => LastResult = CheckInventoryOpen(),
                                                        () =>
                                                        {
                                                            if(LastResult == true)
                                                            {
                                                                Actions.AddRange(new Action[]
                                                                {
                                                                    () => LastResult = EquipItems(new string[] {"elder_maul_inventory.png" }),
                                                                    () =>
                                                                    {
                                                                        if(LastResult)
                                                                        {
                                                                            CentreMouse();
                                                                            LastActionComplete = true;
                                                                            ActionError = true;
                                                                        }
                                                                        else
                                                                        {
                                                                            LastActionComplete = true;
                                                                            ActionError = true;
                                                                        }
                                                                    }
                                                                });
                                                            }

                                                            LastActionComplete = true;
                                                        }
                                                    //});
                                                //}

                                               // LastActionComplete = true;
                                           // }
                                   //    });
                                //    }
//
                               //    LastActionComplete = true;
                               // }
                            });
                        }

                        LastActionComplete = true;
                    }
                });
            }
        }

        public void Equip3ItemsWithChecks(string[] itemNames)
        {
            if (Actions.Count == 0)
            {
                Actions.AddRange(new Action[]
                {
                    () => LastResult = CheckInventoryOpen(),
                    () =>
                    {
                        if(LastResult == true)
                        {
                            Actions.AddRange(new Action[]
                            {
                                () => LastResult = CheckItemsVisible(itemNames),
                                () =>
                                {
                                    if(LastResult == true)
                                    {
                                        Actions.AddRange(new Action[]
                                        {
                                            () => LastResult = EquipItems(itemNames),
                                            () => LastResult = Wait(200),
                                            () => LastResult = CheckItemsNotVisible(itemNames),
                                            () => LastResult = CheckPrayerOpen(),
                                            () =>
                                            {
                                                if(LastResult == true)
                                                {
                                                    Actions.AddRange(new Action[]
                                                    {
                                                        () => SelectPrayer("eagle")
                                                    });
                                                }
                                                else
                                                {
                                                    Actions.AddRange(new Action[]
                                                    {
                                                        () => OpenPrayer(),
                                                        () => LastResult = Wait(new Random().Next(50,75)),
                                                        () => LastResult = CheckPrayerOpen(),
                                                        () =>
                                                        {
                                                            if(LastResult == true)
                                                            {
                                                                Actions.AddRange(new Action[]
                                                                {
                                                                    () => SelectPrayer("eagle")
                                                                });
                                                            }

                                                            LastActionComplete = true;
                                                        }
                                                    });
                                                }
                                            
                                                LastActionComplete = true;
                                            }
                                        });
                                    }
                                    LastActionComplete = true;
                                }
                            });
                        }
                        else
                        {
                            Actions.AddRange(new Action[]
                            {
                                () => LastResult = OpenInventory(),
                                () => LastResult = Wait(new Random().Next(50,75)),
                                () => LastResult = CheckInventoryOpen(),
                                () =>
                                {
                                    if(LastResult == true)
                                    {
                                        Actions.AddRange(new Action[]
                                        {
                                            () => LastResult = CheckItemsVisible(itemNames),
                                            () =>
                                            {
                                                if(LastResult == true)
                                                {
                                                    Actions.AddRange(new Action[]
                                                    {
                                                        () => LastResult = EquipItems(itemNames),
                                                        () => LastResult = Wait(200),
                                                        () => LastResult = CheckItemsNotVisible(itemNames),
                                                        () => LastResult = CheckPrayerOpen(),
                                                        () =>
                                                        {
                                                            if(LastResult == true)
                                                            {
                                                                Actions.AddRange(new Action[]
                                                                {
                                                                    () => SelectPrayer("eagle")
                                                                });
                                                            }
                                                            else
                                                            {
                                                                Actions.AddRange(new Action[]
                                                                {
                                                                    () => OpenPrayer(),
                                                                    () => LastResult = Wait(new Random().Next(25,50)),
                                                                    () => LastResult = CheckPrayerOpen(),
                                                                    () =>
                                                                    {
                                                                        if(LastResult == true)
                                                                        {
                                                                            Actions.AddRange(new Action[]
                                                                            {
                                                                                () => SelectPrayer("eagle")
                                                                            });
                                                                        }

                                                                        LastActionComplete = true;
                                                                    }
                                                                });
                                                            }

                                                            LastActionComplete = true;
                                                        }
                                                    });
                                                }
                                                else
                                                {
                                                    ActionError = true;
                                                }

                                                LastActionComplete = true;
                                            }
                                        });

                                        LastActionComplete = true;
                                    }
                                    else
                                    {
                                        ActionError = true;
                                        LastActionComplete = true;
                                    }
                                }
                            });
                        }

                        LastActionComplete = true;
                    }
                });
            }
        }

        public void Equip3ItemsWithChecksAndSelectSpell(string[] itemNames, string spell)
        {
            if (Actions.Count == 0)
            {
                Actions.AddRange(new Action[]
                {
                    () => LastResult = CheckInventoryOpen(),
                    () =>
                    {
                        if(LastResult == true)
                        {
                            Actions.AddRange(new Action[]
                            {
                                () => LastResult = CheckItemsVisible(itemNames),
                                () =>
                                {
                                    if(LastResult == true)
                                    {
                                        Actions.AddRange(new Action[]
                                        {
                                            () => LastResult = EquipItems(itemNames),
                                            //() => LastResult = Wait(200),
                                            //() => LastResult = CheckItemsNotVisible(itemNames),
                                            () =>
                                            {
                                                if(LastResult == true)
                                                {
                                                    Actions.AddRange(new Action[]
                                                    {
                                                        () => OpenPrayer(),
                                                        () => LastResult = Wait(new Random().Next(25,50)),
                                                        () => LastResult = CheckPrayerOpen(),
                                                        () =>
                                                        {
                                                            if(LastResult == true)
                                                            {
                                                                Actions.AddRange(new Action[]
                                                                {
                                                                    () => SelectPrayer("mystic"),
                                                                    () => OpenSpellbook(),
                                                                    () => LastResult = Wait(new Random().Next(25,50)),
                                                                    () => LastResult = CheckSpellbookOpen(),
                                                                    () =>
                                                                    {
                                                                        if(LastResult == true)
                                                                        {
                                                                            Actions.AddRange(new Action[]
                                                                            {
                                                                                () => LastResult = SelectSpell(spell),
                                                                                () =>
                                                                                {   
                                                                                    if(LastResult)
                                                                                    {
                                                                                        CentreMouse();
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        LastActionComplete = true;
                                                                                    }
                                                                                }
                                                                            });
                                                                        }
                                                                        LastActionComplete = true;
                                                                    }
                                                                });

                                                                LastActionComplete = true;
                                                            }

                                                            LastActionComplete = true;
                                                        }
                                                    });

                                                    LastActionComplete = true;
                                                }
                                                else
                                                {
                                                    ActionError = true;
                                                }
                                            }
                                        });
                                    }
                                    LastActionComplete = true;
                                }
                            });
                        }
                        else
                        {
                            Actions.AddRange(new Action[]
                            {
                                () => LastResult = OpenInventory(),
                                () => LastResult = Wait(new Random().Next(25,50)),
                                () => LastResult = CheckInventoryOpen(),
                                () =>
                                {
                                    if(LastResult == true)
                                    {
                                        Actions.AddRange(new Action[]
                                        {
                                            () => LastResult = CheckItemsVisible(itemNames),
                                            () =>
                                            {
                                                if(LastResult == true)
                                                {
                                                    Actions.AddRange(new Action[]
                                                    {
                                                        () => LastResult = EquipItems(itemNames),
                                                        //() => LastResult = Wait(200),
                                                        //() => LastResult = CheckItemsNotVisible(itemNames),
                                                        () =>
                                                        {
                                                            if(LastResult == true)
                                                            {
                                                                Actions.AddRange(new Action[]
                                                                {
                                                                    () => OpenPrayer(),
                                                                    () => LastResult = Wait(new Random().Next(25,50)),
                                                                    () => LastResult = CheckPrayerOpen(),
                                                                    () =>
                                                                    {
                                                                        if(LastResult == true)
                                                                        {
                                                                            Actions.AddRange(new Action[]
                                                                            {
                                                                                () => SelectPrayer("mystic"),
                                                                                () => OpenSpellbook(),
                                                                                () => LastResult = Wait(new Random().Next(25,50)),
                                                                                () => LastResult = CheckSpellbookOpen(),
                                                                                () =>
                                                                                {
                                                                                    if(LastResult == true)
                                                                                    {
                                                                                        Actions.AddRange(new Action[]
                                                                                        {
                                                                                            () => LastResult = SelectSpell(spell),
                                                                                            () =>
                                                                                            {
                                                                                                if(LastResult)
                                                                                                {
                                                                                                    CentreMouse();
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    LastActionComplete = true;
                                                                                                }
                                                                                            }
                                                                                        });
                                                                                    }
                                                                                    LastActionComplete = true;
                                                                                }
                                                                            });
                                                                        }

                                                                        LastActionComplete = true;
                                                                    }
                                                                });

                                                                LastActionComplete = true;
                                                            }
                                                            else
                                                            {
                                                                ActionError = true;
                                                            }
                                                        }
                                                    });
                                                }
                                                else
                                                {
                                                    ActionError = true;
                                                }

                                                LastActionComplete = true;
                                            }
                                        });

                                        LastActionComplete = true;
                                    }
                                    else
                                    {
                                        ActionError = true;
                                        LastActionComplete = true;
                                    }
                                }
                            });
                        }

                        LastActionComplete = true;
                    }
                });
            }
        }

        public bool CheckInventoryOpen()
        {
            try
            {
                return this.CurrentScreen == Inventory_Screen.Inventory;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }

        public bool CheckSpellbookOpen()
        {
            try
            {
                return this.CurrentScreen == Inventory_Screen.Spellbook;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }
        public bool CheckPrayerOpen(bool dontComplete = false)
        {
            try
            {
                return this.CurrentScreen == Inventory_Screen.Prayer;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                if (!dontComplete)
                {
                    LastActionComplete = true;
                }
            }
        }

        public bool CheckItemsVisible(string[] itemNames)
        {
            try
            {
                if (itemNames == null || itemNames.Length == 0) return false;

                int itemsVisible = 0;
                foreach (string itemName in itemNames)
                {
                    ScreenObject targetObject = this.ScreenObjects["Inventory_Items"][itemName];

                    if (targetObject == null) return false;

                    if (targetObject.isVisible) itemsVisible++;
                }

                return itemsVisible > 0;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }
        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
        }

        public bool EquipItems(string[] itemNames)
        {
            try
            {
                if (itemNames == null || itemNames.Length == 0) return false;

                List<(double, ScreenObject)> targetsByDistanceFromLeft = new List<(double, ScreenObject)>();
                foreach (string itemName in itemNames)
                {
                    ScreenObject targetObject = this.ScreenObjects["Inventory_Items"][itemName];

                    if (targetObject == null || !targetObject.isVisible) continue;

                    Mouse.MousePoint mpos = Mouse.GetCursorPosition();

                    double dist = GetDistance(mpos.X, mpos.Y, targetObject.x, targetObject.y);

                    targetsByDistanceFromLeft.Add((dist, targetObject));
                }

                targetsByDistanceFromLeft.Sort((a, b) => a.Item1.CompareTo(b.Item1));

                int i = 0;
                foreach((double, ScreenObject) sortedTarget in targetsByDistanceFromLeft)
                {
                    ScreenObject targetObject = sortedTarget.Item2;
                    Actions.InsertRange(0 + i, new Action[]
                    {
                        () => Mouse.MoveMouse(targetObject.x + (targetObject.w/2) - (targetObject.w/4), targetObject.y + (targetObject.h / 2) - (targetObject.h/3), (targetObject.w/4), (targetObject.h/3), true, () => LastActionComplete = true)
                    });

                    i++;
                }

                return true;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }
        public bool SelectSpell(string spell)
        {
            try
            {

                ScreenObject targetObject = this.ScreenObjects["Spellbook"][spell];

                if (targetObject == null || !targetObject.isVisible)
                {
                    LastActionComplete = true;
                    return false;
                }


                Mouse.MoveMouse(targetObject.x + (targetObject.w / 2) - (targetObject.w / 4), targetObject.y + (targetObject.h / 2) - (targetObject.h / 3), (targetObject.w / 4), (targetObject.h / 3), true, () => LastActionComplete = true);

                return true;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public bool SelectPrayer(string prayer)
        {
            try
            {
                if (!CheckPrayerOpen(true)) return false;
                if (ObjectFinder.Prayer_States[prayer] == true)
                {
                    LastActionComplete = true;
                    return true;
                }

                Mouse.MousePoint prayerButton = ObjectFinder.Prayer_Pixels_Dictionary[prayer];

                Mouse.MoveMouse(prayerButton.X, prayerButton.Y, 40, 40, true, () => LastActionComplete = true);

                return true;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public bool CheckItemsNotVisible(string[] itemNames)
        {
            try
            {
                if (itemNames == null || itemNames.Length == 0) return false;

                foreach (string itemName in itemNames)
                {
                    ScreenObject targetObject = this.ScreenObjects["Inventory_Items"][itemName];

                    if (targetObject == null) return false;

                    if (targetObject.isVisible) return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }

        public bool Wait(int ms)
        {
            try
            {
                Thread.Sleep(ms);
                return true;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }
        public bool OpenInventory()
        {
            try
            {
                if (CheckInventoryOpen()) return true;
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_2);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                ActionError = true;
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }
        public bool OpenSpellbook()
        {
            try
            {
                if (CheckSpellbookOpen()) return true;
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_1);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                ActionError = true;
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }

        public bool OpenPrayer()
        {
            try
            {
                if (CheckPrayerOpen(true)) return true;
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_3);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                ActionError = true;
                return false;
            }
            finally
            {
                LastActionComplete = true;
            }
        }

        public bool CentreMouse()
        {
            try
            {
                Mouse.MoveMouse(2560 + 100,720,60,60, false, () => LastActionComplete = true);

                return true;
            }
            catch (Exception ex)
            {
                ActionError = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
