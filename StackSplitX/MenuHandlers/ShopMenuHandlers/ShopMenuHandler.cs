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
        private Point ClickItemLocation;
        private IShopAction CurrentShopAction = null;

        public ShopMenuHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        protected override float GetRightClickPollingInterval()
        {
            return 300f; // From ShopMenu.receiveRightClick
        }

        protected override EInputHandled CancelMove()
        {
            // TODO: add mod config to set whether or not default behavior should be used when canceling
            // StackAmount will be default for the appropriate sale type, so just fake the input being submitted.
            OnStackAmountReceived(this.CurrentShopAction?.StackAmount.ToString());
            return EInputHandled.NotHandled;
        }

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
