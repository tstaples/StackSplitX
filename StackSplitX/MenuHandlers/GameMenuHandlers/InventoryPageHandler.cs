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
        private InventoryHandler Inventory = null;

        public InventoryPageHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
            this.Inventory = new InventoryHandler(helper.Reflection, monitor);
        }

        public override EInputHandled OpenSplitMenu(out int stackAmount)
        {
            stackAmount = 0;

            var inventoryMenu = Helper.Reflection.GetPrivateValue<InventoryMenu>(this.MenuPage, "inventory");
            var hoveredItemField = Helper.Reflection.GetPrivateField<Item>(this.MenuPage, "hoveredItem");
            var heldItemField = Helper.Reflection.GetPrivateField<Item>(this.MenuPage, "heldItem");

            this.Inventory.Init(inventoryMenu, heldItemField, hoveredItemField);
            if (this.Inventory.WasClicked(Game1.getMouseX(), Game1.getMouseY()))
            {
                this.Inventory.SelectItem(Game1.getMouseX(), Game1.getMouseY());
                if (this.Inventory.CanSplitSelectedItem())
                {
                    stackAmount = this.Inventory.GetDefaultSplitStackAmount();

                    return EInputHandled.Consumed;
                }
            }
            return EInputHandled.NotHandled;
        }

        public override EInputHandled CancelMove()
        {
            this.Inventory.CancelSplit();
            return base.CancelMove();
        }

        public override void OnStackAmountEntered(int amount)
        {
            this.Inventory.SplitSelectedItem(amount);
        }
    }
}
