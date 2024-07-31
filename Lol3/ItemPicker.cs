using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lol3
{
    internal class ItemPicker
    {

        static Dictionary<string, List<string>> sortedItemsByPreference = new Dictionary<string, List<string>>();

        public static List<string> StaffTypes = new List<string>(new string[]
        {
            "toxic_staff_inventory.png",
            "ahrims_staff_inventory.png",
            "ancient_staff_inventory.png",
            "battlestaff_inventory.png",
            "white_staff_inventory.png",
            "air_staff_inventory.png",
            "water_staff_inventory.png"
        });

        public static List<string> MagicTopTypes = new List<string>(new string[]
        {
            "dark_mystic_top_inventory.png",
            "xerician_top_inventory.png"
        });

        public static List<string> MagicBottomTypes = new List<string>(new string[]
        {
            "dark_mystic_bottoms_inventory.png",
            "xerician_bottom_inventory.png"
        });

        public static List<string> RangeWeaponTypes = new List<string>(new string[]
        {
            "blowpipe_inventory.png",
            "heavy_balista_inventory.png",
            "rune_crossbow_inventory.png",
            "msb_inventory.png"
        });

        public static List<string> OffhandTypes = new List<string>(new string[]
        {
            "purple_book_inventory.png"
        });

        public static List<string> MeleeWeaponTypes = new List<string>(new string[]
        {
            "elder_maul_inventory.png",
            "claws_inventory.png"
        });

        static ItemPicker()
        {
            sortedItemsByPreference.Add("Staff", StaffTypes);

            sortedItemsByPreference.Add("Magic_Top", MagicTopTypes);

            sortedItemsByPreference.Add("Magic_Bottoms", MagicBottomTypes);

            sortedItemsByPreference.Add("Range_Weapon", RangeWeaponTypes);

            sortedItemsByPreference.Add("Offhand", OffhandTypes);

            sortedItemsByPreference.Add("Melee", MeleeWeaponTypes);
        }

        public static string PickBest(string type, ref Dictionary<string, Dictionary<string, ScreenObject>> ScreenObjects)
        {
            try
            {
                bool foundItem = false;
                string selectedItem = string.Empty;

                foreach(var item in sortedItemsByPreference[type])
                {
                    ScreenObject targetObject = ScreenObjects["Inventory_Items"][item];

                    if (targetObject == null) continue;
                    if (targetObject.isVisible)
                    {
                        selectedItem = targetObject.Name;
                        foundItem = true;
                        break;
                    }
                }

                return selectedItem;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return string.Empty;
            }
        }
    }
}
