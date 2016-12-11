using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace StackSplitX
{
    public class StackSplitX : Mod
    {
        /* 
            Menus:
            - inventory
            - chest
            - shop
        */

        private bool isOpen = false;
        private bool shiftHeld = false;
        private TInventoryPage myInventoryPage;
        //private TInventoryMenu myInventoryMenu = null;

        public override void Entry(IModHelper helper)
        {
            MenuEvents.MenuChanged += OnMenuChanged;
            MenuEvents.MenuClosed += OnMenuClosed;
        }

        private void OnMenuClosed(object sender, EventArgsClickableMenuClosed e)
        {
            if (isOpen)
            {
                isOpen = false;
                ControlEvents.MouseChanged += OnMouseStateChanged;
                ControlEvents.KeyPressed += OnKeyPressed;
                ControlEvents.KeyReleased += OnKeyReleased;
            }
        }

        private void OnMenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            DebugPrintMenuInfo(e.PriorMenu, e.NewMenu);

            if (e.PriorMenu != null && e.PriorMenu.GetType() == e.NewMenu.GetType())
            {
                Monitor.Log("resize event");
                return;
            }

            if (e.NewMenu is InventoryPage)
            {
                isOpen = true;

                myInventoryPage = TInventoryPage.ConstructFromBaseClass((InventoryPage)Game1.activeClickableMenu);
                //InventoryMenu inventoryMenu = Utils.GetNativeField<InventoryMenu, InventoryPage>(inventoryPage, "inventory");
                //myInventoryMenu = TInventoryMenu.ConstructFromBaseClass(inventoryMenu);

                ControlEvents.MouseChanged += OnMouseStateChanged;
                ControlEvents.KeyPressed += OnKeyPressed;
                ControlEvents.KeyReleased += OnKeyReleased;
            }
        }

        private void OnKeyReleased(object sender, EventArgsKeyPressed e)
        {
            shiftHeld = !(e.KeyPressed == Keys.LeftShift || e.KeyPressed == Keys.RightShift);
        }

        private void OnKeyPressed(object sender, EventArgsKeyPressed e)
        {
            shiftHeld = (e.KeyPressed == Keys.LeftShift || e.KeyPressed == Keys.RightShift);
        }

        private void OnMouseStateChanged(object sender, EventArgsMouseStateChanged e)
        {
            if (e.NewState.RightButton == ButtonState.Released && shiftHeld)
            {
                Monitor.Log("hotkey macro pressed");
            }
        }

        private void DebugPrintMenuInfo(IClickableMenu priorMenu, IClickableMenu newMenu)
        {
#if DEBUG
            try
            {
                string priorName = "None";
                if (priorMenu != null)
                {
                    priorName = priorMenu.GetType().Name;
                }
                string newName = newMenu.GetType().Name;
                Monitor.Log("Menu changed from: " + priorName + " to " + newName);
            }
            catch (Exception ex)
            {
                Monitor.Log("Error getting menu name: " + ex);
            }
#endif
        }
    }
}
