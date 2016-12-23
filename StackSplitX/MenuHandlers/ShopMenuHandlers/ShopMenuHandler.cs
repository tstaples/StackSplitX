using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackSplitX.MenuHandlers
{
    public class ShopMenuHandler : BaseMenuHandler<ShopMenu>
    {
        /// <summary>Where the player clicked when the split menu was opened.</summary>
        private Point ClickItemLocation;

        /// <summary>The shop action for the current operation.</summary>
        private IShopAction CurrentShopAction = null;

        /// <summary>Constructs and instance.</summary>
        /// <param name="helper">Mod helper instance.</param>
        /// <param name="monitor">Monitor instance.</param>
        public ShopMenuHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        /// <summary>How long the right click has to be held for before the receiveRIghtClick gets called rapidly (See Game1.Update)</summary>
        /// <returns>The polling interval.</returns>
        protected override float GetRightClickPollingInterval()
        {
            return 300f; // From ShopMenu.receiveRightClick
        }

        /// <summary>Called when the current handler loses focus when the split menu is open, allowing it to cancel the operation or run the default behaviour.</summary>
        /// <returns>If the input was handled or consumed.</returns>
        protected override EInputHandled CancelMove()
        {
            // TODO: add mod config to set whether or not default behavior should be used when canceling
            // StackAmount will be default for the appropriate sale type, so just fake the input being submitted.
            OnStackAmountReceived(this.CurrentShopAction?.StackAmount.ToString());
            return EInputHandled.NotHandled;
        }

        /// <summary>Main event that derived handlers use to setup necessary hooks and other things needed to take over how the stack is split.</summary>
        /// <returns>If the input was handled or consumed.</returns>
        protected override EInputHandled OpenSplitMenu()
        {
            // Check if it was the shop or inventory that was clicked and initialize the appropriate values
            this.ClickItemLocation = new Point(Game1.getOldMouseX(), Game1.getOldMouseY());
            this.CurrentShopAction = GetShopAction(this.ClickItemLocation);
            if (this.CurrentShopAction == null || !this.CurrentShopAction.CanPerformAction())
                return EInputHandled.NotHandled;

            this.SplitMenu = new StackSplitMenu(OnStackAmountReceived, this.CurrentShopAction.StackAmount);

            return EInputHandled.Consumed;
        }

        /// <summary>Callback given to the split menu that is invoked when a value is submitted.</summary>
        /// <param name="s">The user input.</param>
        protected override void OnStackAmountReceived(string s)
        {
            int amount = 0;
            if (int.TryParse(s, out amount))
            {
                if (amount > 0)
                {
                    this.CurrentShopAction.PerformAction(amount, this.ClickItemLocation);
                }
            }

            base.OnStackAmountReceived(s);
        }

        // TODO: use inventoryclicked event to make this cleaner.
        /// <summary>Determines which shop action to create based on what was selected.</summary>
        /// <param name="p">The click location.</param>
        /// <returns>The shop action instance.</returns>
        private IShopAction GetShopAction(Point p)
        {
            // Check if we selected an item in the inventory
            var inventory = this.Helper.Reflection.GetPrivateValue<InventoryMenu>(this.NativeMenu, "inventory");
            var item = inventory.getItemAt(this.ClickItemLocation.X, this.ClickItemLocation.Y);
            if (item != null)
                return new SellAction(this.Helper.Reflection, this.Monitor, this.NativeMenu, item);

            // Check if we clicked a shop item
            item = BuyAction.GetClickedShopItem(this.Helper.Reflection, this.NativeMenu, this.ClickItemLocation);
            if (item != null)
                return new BuyAction(this.Helper.Reflection, this.Monitor, this.NativeMenu, item);
            return null;
        }
    }
}
