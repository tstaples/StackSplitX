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
        /// <summary>Constructs and instance.</summary>
        /// <param name="helper">Mod helper instance.</param>
        /// <param name="monitor">Monitor instance.</param>
        public InventoryPageHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        /// <summary>Notifies the page handler that it's corresponding menu has been opened.</summary>
        /// <param name="menu">The native menu owning all the pages.</param>
        /// <param name="page">The specific page this handler is for.</param>
        /// <param name="inventory">The inventory handler.</param>
        public override void Open(IClickableMenu menu, IClickableMenu page, InventoryHandler inventory)
        {
            base.Open(menu, page, inventory);

            var inventoryMenu = Helper.Reflection.GetPrivateValue<InventoryMenu>(this.MenuPage, "inventory");
            var hoveredItemField = Helper.Reflection.GetPrivateField<Item>(this.MenuPage, "hoveredItem");
            var heldItemField = Helper.Reflection.GetPrivateField<Item>(this.MenuPage, "heldItem");

            this.Inventory.Init(inventoryMenu, heldItemField, hoveredItemField);
        }
    }
}
