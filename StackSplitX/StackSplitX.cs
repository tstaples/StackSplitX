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

        private bool IsOpen = false;
        private bool ShouldDraw = false;
        private StackSplitMenu SplitMenu = null;
        private InventoryPage InventoryPage = null;
        private InventoryMenu InventoryMenu = null;
        private Item HeldItem = null;
        private Item HoveredItem = null;

        public override void Entry(IModHelper helper)
        {
            MenuEvents.MenuChanged += OnMenuChanged;
            MenuEvents.MenuClosed += OnMenuClosed;
        }

        private void OnMenuClosed(object sender, EventArgsClickableMenuClosed e)
        {
            if (this.IsOpen)
            {
                this.IsOpen = false;
                this.ShouldDraw = false;

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

                this.IsOpen = true;

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
                bool isAlreadyHoldingItem = (this.HeldItem != null);

                var menu = Game1.activeClickableMenu as GameMenu;
                Debug.Assert(menu.currentTab == GameMenu.inventoryTab);
                var pages = Helper.Reflection.GetPrivateValue<List<IClickableMenu>>(menu, "pages");
                this.InventoryPage = pages[GameMenu.inventoryTab] as InventoryPage;
                this.InventoryMenu = Helper.Reflection.GetPrivateValue<InventoryMenu>(this.InventoryPage, "inventory");
                this.HoveredItem = Helper.Reflection.GetPrivateValue<Item>(this.InventoryPage, "hoveredItem");
                this.HeldItem = Helper.Reflection.GetPrivateValue<Item>(this.InventoryPage, "heldItem");

                // If we were holding it and we're now clicking a slot of a different item type then hide the tooltip
                if (isAlreadyHoldingItem && this.HoveredItem?.Name != this.HeldItem?.Name)
                {
                    CleanupAfterSelectingAmount();
                    return;
                }

                // Ignore for empty slots or stacks of 1
                if (this.HeldItem == null || this.HeldItem.Stack <= 1)
                {
                    return;
                }

                this.SplitMenu = new StackSplitMenu(OnStackAmountReceived, this.HeldItem.Stack, this.HoveredItem != null ? this.HoveredItem.Stack : 0);
                this.ShouldDraw = true;
            }
            else if (e.NewState.LeftButton == ButtonState.Pressed && e.PriorState.LeftButton == ButtonState.Released &&
                     this.SplitMenu != null)
            {
                if (this.SplitMenu.ContainsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    this.SplitMenu.ReceiveLeftClick(Game1.getMouseX(), Game1.getMouseY());
                }
                else
                {
                    CleanupAfterSelectingAmount();
                }
            }
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            if (this.SplitMenu != null)
            {
                this.SplitMenu.Update();
            }
        }

        private void OnDraw(object sender, EventArgs e)
        {
            if (!this.ShouldDraw || this.SplitMenu == null)
                return;

            this.SplitMenu.draw(Game1.spriteBatch);
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

            var numHovered = this.HoveredItem != null ? this.HoveredItem.Stack : 0;
            var numHeld = this.HeldItem.Stack;
            var totalItems = numHovered + numHeld;

            if (amount == 0 && this.HoveredItem != null)
            {
                // Put the held amount back
                numHovered += numHeld;
                numHeld = 0;

                // Remove the held item
                Helper.Reflection.GetPrivateField<Item>(this.InventoryPage, "heldItem").SetValue(null);
            }
            else if (amount >= totalItems)
            {
                // Put all items into held
                numHeld = totalItems;
                numHovered = 0;

                if (this.HoveredItem != null)
                {
                    // Remove the item from the inventory as it's now all being held.
                    var index = this.InventoryMenu.actualInventory.IndexOf(this.HoveredItem);
                    if (index >= 0 && index < this.InventoryMenu.actualInventory.Count)
                    {
                        this.InventoryMenu.actualInventory[index] = null;
                    }
                }
            }
            else
            {
                // Use the input value
                numHovered = totalItems - amount;
                numHeld = amount;
            }

            this.HeldItem.Stack = numHeld;
            if (this.HoveredItem != null)
            {
                this.HoveredItem.Stack = numHovered;
            }

            CleanupAfterSelectingAmount();
        }

        private void CleanupAfterSelectingAmount()
        {
            this.ShouldDraw = false;
            this.SplitMenu = null;
            this.HoveredItem = null;
            this.HeldItem = null;
            this.InventoryPage = null;
            this.InventoryMenu = null;
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
