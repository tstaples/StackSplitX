using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Menus;
using StardewValley;
using System.Diagnostics;

namespace StackSplitX.MenuHandlers
{
    public class ItemGrabMenuHandler : BaseMenuHandler
    {
        private InventoryMenu PlayerInventoryMenu = null;
        private InventoryMenu ItemsToGrabMenu = null;
        private ItemGrabMenu.behaviorOnItemSelect OriginalItemSelectCallback;
        private ItemGrabMenu.behaviorOnItemSelect OriginalItemGrabCallback;
        private Item HoverItem = null;
        private Item HeldItem = null;
        private int StackAmount = 0;

        public ItemGrabMenuHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        public override void Open(IClickableMenu menu)
        {
            base.Open(menu);
        }

        protected override bool CanOpenSplitMenu()
        {
            bool canOpen = (this.NativeMenu as ItemGrabMenu).allowRightClick;
            return (canOpen && base.CanOpenSplitMenu());
        }

        protected override EInputHandled OpenSplitMenu()
        {
            try
            {
                Debug.Assert(this.NativeMenu is MenuWithInventory);
                var nativeMenuWithInventory = (this.NativeMenu as MenuWithInventory);

                this.PlayerInventoryMenu = nativeMenuWithInventory.inventory;
                this.ItemsToGrabMenu = this.Helper.Reflection.GetPrivateValue<InventoryMenu>(this.NativeMenu, "ItemsToGrabMenu");

                // Emulate the right click method that would normally happen.
                this.HoverItem = nativeMenuWithInventory.hoveredItem;
                this.HeldItem = nativeMenuWithInventory.heldItem;
                this.HeldItem = this.PlayerInventoryMenu.rightClick(Game1.getOldMouseX(), Game1.getOldMouseY(), this.HeldItem);

                if (this.HeldItem == null)
                {
                    // Check the item to grab menu
                    //this.HeldItem = this.ItemsToGrabMenu.rightClick(Game1.getMouseX(), Game1.getMouseY(), this.HeldItem);
                    this.HeldItem = this.ItemsToGrabMenu.rightClick(Game1.getOldMouseX(), Game1.getOldMouseY(), this.HeldItem);
                    if (this.HeldItem == null)
                    {
                        // If there's no item here either then there's nothing else to do
                        return EInputHandled.NotHandled;
                    }
                }

                this.Monitor.Log($"Held item: {this.HeldItem?.Name}", LogLevel.Trace);
                // don't need to update native since it will be overriden when we call this.NativeMenu.receiveRightClick
                //heldItemField.SetValue(this.HeldItem);
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to get properties from native menu: {e}", LogLevel.Error);
                return EInputHandled.NotHandled;
            }

            // Try to hook into the item callbacks
            if (!HookCallbacks())
            {
                return EInputHandled.NotHandled;
            }

            // Create the split menu
            this.SplitMenu = new StackSplitMenu(
                OnStackAmountReceived,
                this.HeldItem.Stack,
                this.HoverItem != null ? this.HoverItem.Stack : 0
                );

            return EInputHandled.Consumed;
        }

        protected override void OnStackAmountReceived(string s)
        {
            // Store amount
            //this.StackAmount = s
            if (int.TryParse(s, out this.StackAmount))
            {
                // TODO: handle invalid values
                if (this.StackAmount < 0)
                {
                    // Use the default amount or 0
                    this.StackAmount = this.HeldItem != null ? this.HeldItem.Stack : 0;
                }

                // Invoke rightClickRecieved
                this.NativeMenu.receiveRightClick(Game1.getOldMouseX(), Game1.getOldMouseY());
            }

            base.OnStackAmountReceived(s);
        }

        private void OnItemSelect(Item item, Farmer who)
        {
            this.Monitor.Log("OnItemSelect", LogLevel.Trace);

            if (this.HeldItem != null && this.StackAmount > 0)
            {
                // update held item stack and item stack
                var totalItems = item.Stack + this.HeldItem.Stack;
                this.StackAmount = Math.Min(totalItems, this.StackAmount);
                this.HeldItem.Stack = this.StackAmount;
                item.Stack = totalItems - this.StackAmount;
            }

            if (this.OriginalItemSelectCallback != null)
            {
                this.OriginalItemSelectCallback(item, who);
            }
        }

        private void OnItemGrab(Item item, Farmer who)
        {
            this.Monitor.Log("OnItemGrab", LogLevel.Trace);

            if (this.HeldItem != null && this.StackAmount > 0)
            {
                // update held item stack and item stack
                var totalItems = item.Stack + this.HeldItem.Stack;
                this.StackAmount = Math.Min(totalItems, this.StackAmount);
                this.HeldItem.Stack = this.StackAmount;
                item.Stack = totalItems - this.StackAmount;
            }

            // Update stack to the amount set from OnStackAmountReceived
            if (this.OriginalItemGrabCallback != null)
            {
                this.OriginalItemGrabCallback(item, who);
            }
        }

        private bool HookCallbacks()
        {
            try
            {
                // Replace the delegates with our own
                var itemSelectCallbackField = this.Helper.Reflection
                    .GetPrivateField<ItemGrabMenu.behaviorOnItemSelect>(this.NativeMenu, "behaviorFunction");
                var itemGrabCallbackField = typeof(ItemGrabMenu).GetField("behaviorOnItemGrab");
                //var itemGrabCallbackField = this.Helper.Reflection
                //    .GetPrivateField<ItemGrabMenu.behaviorOnItemSelect>(this.NativeMenu, "behaviorOnItemGrab");

                this.OriginalItemGrabCallback = itemGrabCallbackField.GetValue(this.NativeMenu) as ItemGrabMenu.behaviorOnItemSelect;
                this.OriginalItemSelectCallback = itemSelectCallbackField.GetValue();

                itemGrabCallbackField.SetValue(this.NativeMenu, new ItemGrabMenu.behaviorOnItemSelect(this.OnItemGrab));
                itemSelectCallbackField.SetValue(this.OnItemSelect);
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to hook ItemGrabMenu callbacks: {e}", LogLevel.Error);
                return false;
            }
            return true;
        }
    }
}
