using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackSplitX.MenuHandlers
{
    public class InventoryPageHandler : GameMenuPageHandler<InventoryPage>
    {
        private InventoryMenu InventoryMenu = null;
        private Item HeldItem = null;
        private Item HoveredItem = null;

        public InventoryPageHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        public override EInputHandled OpenSplitMenu(out int stackAmount)
        {
            stackAmount = 0;
            bool isAlreadyHoldingItem = (this.HeldItem != null);

            try
            {
                this.InventoryMenu = Helper.Reflection.GetPrivateValue<InventoryMenu>(this.MenuPage, "inventory");
                this.HoveredItem = Helper.Reflection.GetPrivateValue<Item>(this.MenuPage, "hoveredItem");

                // Get the current held item if any
                var heldItemField = this.Helper.Reflection.GetPrivateField<Item>(this.MenuPage, "heldItem");
                this.HeldItem = heldItemField.GetValue();
                // Emulate the right click method that would normally happen (native code passes in held item hence above).
                this.HeldItem = this.InventoryMenu.rightClick(Game1.getMouseX(), Game1.getMouseY(), this.HeldItem);
                // Update the native object's held item.
                heldItemField.SetValue(this.HeldItem);
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to get Inventory values: {e}", LogLevel.Error);
                return EInputHandled.NotHandled;
            }

            // If we were holding it and we're now clicking a slot of a different item type then hide the tooltip
            if (this.HoveredItem == null ||
                (isAlreadyHoldingItem && this.HoveredItem?.Name != this.HeldItem?.Name) ||
                (this.HeldItem == null || this.HeldItem.Stack <= 1)) // Ignore for empty slots or stacks of 1
            {
                return EInputHandled.NotHandled;
            }

            stackAmount = this.HeldItem.Stack;

            return EInputHandled.Consumed;
        }

        // TODO: improve this
        public override void OnStackAmountEntered(int amount)
        {
            if (amount < 0)
            {
                return;
            }

            var numHovered = this.HoveredItem != null ? this.HoveredItem.Stack : 0;
            var numHeld = this.HeldItem.Stack;
            var totalItems = numHovered + numHeld;

            //this.Monitor.Log($"Item: {this.HeldItem.Name} | Num held: {numHeld} | Num Hovered: {numHovered} | Total: {totalItems}");
            if (amount == 0 && this.HoveredItem != null)
            {
                // Put the held amount back
                numHovered += numHeld;
                numHeld = 0;

                // Remove the held item
                Helper.Reflection.GetPrivateField<Item>(this.MenuPage, "heldItem").SetValue(null);
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
                this.HoveredItem.Stack = numHovered;
        }
    }
}
