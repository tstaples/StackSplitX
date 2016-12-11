using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Reflection;
using StardewModdingAPI.Events;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

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
        private bool shouldDraw = false;
        private StackSplitMenu stackSplitMenu = null;
        private InventoryPage inventoryPage = null;
        private InventoryMenu inventoryMenu = null;
        private Item heldItem = null;
        private Item hoveredItem = null;

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
                shouldDraw = false;

                ControlEvents.MouseChanged -= OnMouseStateChanged;
                GraphicsEvents.OnPostRenderEvent -= OnDraw;
            }
        }

        private void OnMenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            DebugPrintMenuInfo(e.PriorMenu, e.NewMenu);

            // TODO: remove if this nolonger occurs here
            if (e.PriorMenu != null && e.PriorMenu.GetType() == e.NewMenu.GetType())
            {
                Monitor.Log("resize event");
                return;
            }

            if (!(e.NewMenu is GameMenu))
            {
                return;
            }

            var menu = e.NewMenu as GameMenu;
            if (menu.currentTab == GameMenu.inventoryTab)
            {
                Monitor.Log("Inventory open");

                isOpen = true;

                var pages = Helper.Reflection.GetPrivateValue<List<IClickableMenu>>(menu, "pages");

                ControlEvents.MouseChanged += OnMouseStateChanged;
                GraphicsEvents.OnPostRenderEvent += OnDraw;
            }
        }

        private void OnMouseStateChanged(object sender, EventArgsMouseStateChanged e)
        {
            if (e.NewState.RightButton == ButtonState.Released && e.PriorState.RightButton == ButtonState.Pressed &&
                IsAnyKeyDown(Game1.oldKBState, new Keys[] {Keys.LeftAlt, Keys.LeftShift }))
            {
                Monitor.Log("hotkey macro pressed");

                var menu = Game1.activeClickableMenu as GameMenu;
                Debug.Assert(menu.currentTab == GameMenu.inventoryTab);
                var pages = Helper.Reflection.GetPrivateValue<List<IClickableMenu>>(menu, "pages");
                inventoryPage = pages[GameMenu.inventoryTab] as InventoryPage;
                inventoryMenu = Helper.Reflection.GetPrivateValue<InventoryMenu>(inventoryPage, "inventory");
                hoveredItem = Helper.Reflection.GetPrivateValue<Item>(inventoryPage, "hoveredItem");
                heldItem = Helper.Reflection.GetPrivateValue<Item>(inventoryPage, "heldItem");

                if (heldItem == null)
                {
                    return;
                }

                Monitor.Log($"Hovered item: {hoveredItem} | held item: {heldItem}");

                stackSplitMenu = new StackSplitMenu(OnStackAmountReceived, heldItem.Stack, hoveredItem != null ? hoveredItem.Stack : 0);
                shouldDraw = true;
            }
        }

        private void OnDraw(object sender, EventArgs e)
        {
            if (!shouldDraw || stackSplitMenu == null)
                return;

            stackSplitMenu.draw(Game1.spriteBatch);
        }

        private void OnStackAmountReceived(string s)
        {
            Monitor.Log(s);

            int amount = -1;
            if (!int.TryParse(s, out amount))
            {
                Monitor.Log("Invalid amount input");
                CleanupAfterSelectingAmount();
                return;
            }

            if (amount < 0)
            {
                CleanupAfterSelectingAmount();
                return;
            }

            var numHovered = hoveredItem != null ? hoveredItem.Stack : 0;
            var numHeld = heldItem.Stack;
            var totalItems = numHovered + numHeld;

            if (amount == 0 && hoveredItem != null)
            {
                // Put the held amount back
                numHovered += numHeld;
                numHeld = 0;

                // Remove the held item
                Helper.Reflection.GetPrivateField<Item>(inventoryPage, "heldItem").SetValue(null);
            }
            else if (amount >= totalItems)
            {
                // Put all items into held
                numHeld = totalItems;
                numHovered = 0;

                if (hoveredItem != null)
                {
                    // Remove the item from the inventory as it's now all being held.
                    var index = inventoryMenu.actualInventory.IndexOf(hoveredItem);
                    if (index >= 0 && index < inventoryMenu.actualInventory.Count)
                    {
                        inventoryMenu.actualInventory[index] = null;
                    }
                }
            }
            else
            {
                // Use the input value
                numHovered = totalItems - amount;
                numHeld = amount;
            }

            heldItem.Stack = numHeld;
            if (hoveredItem != null)
            {
                hoveredItem.Stack = numHovered;
            }

            CleanupAfterSelectingAmount();
        }

        private void CleanupAfterSelectingAmount()
        {
            shouldDraw = false;
            stackSplitMenu = null;
            hoveredItem = null;
            heldItem = null;
            inventoryPage = null;
            inventoryMenu = null;
        }

        private bool IsAnyKeyDown(KeyboardState state, Keys[] keys)
        {
            foreach (var key in keys)
            {
                if (state.IsKeyDown(key))
                {
                    return true;
                }
            }
            return false;
        }

        #region DebugMenuPrint
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
        #endregion DebugMenuPrint
    }
}
