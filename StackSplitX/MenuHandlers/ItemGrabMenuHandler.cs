using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Menus;
using StardewValley;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace StackSplitX.MenuHandlers
{
    public class ItemGrabMenuHandler : BaseMenuHandler
    {
        private InventoryMenu PlayerInventoryMenu = null;
        private InventoryMenu ItemsToGrabMenu = null;
        private bool CallbacksHooked = false;
        private ItemGrabMenu.behaviorOnItemSelect OriginalItemSelectCallback;
        private ItemGrabMenu.behaviorOnItemSelect OriginalItemGrabCallback;
        private Item HoverItem = null;
        private Item HeldItem = null;
        private int StackAmount = 0;
        private int TotalItems = 0;
        private Point ClickItemLocation;

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

        public override void CloseSplitMenu()
        {
            if (this.SplitMenu != null)
            {
                base.CloseSplitMenu();
                //RevertItems();
                //CancelMove();
                RestoreNativeCallbacks();
            }
        }

        protected override EInputHandled CancelMove()
        {
            base.CancelMove();

            // Run the regular command
            this.NativeMenu?.receiveRightClick(this.ClickItemLocation.X, this.ClickItemLocation.Y);

            // Close the menu
            CloseSplitMenu();

            // Consume input so the menu doesn't run left click logic as well
            return EInputHandled.Consumed;
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
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to get properties from native menu: {e}", LogLevel.Error);
                return EInputHandled.NotHandled;
            }

            if (this.HoverItem == null)
            {
                this.Monitor.Log("No hover item", LogLevel.Trace);
                return EInputHandled.NotHandled;
            }

            // Try to hook into the item callbacks
            if (!HookCallbacks())
            {
                return EInputHandled.NotHandled;
            }

            // Store where the cursor was in case it moves before submitting the split menu
            // as proceeding parts will use the now moved mouse location.
            this.ClickItemLocation = new Point(Game1.getOldMouseX(), Game1.getOldMouseY());

            // Check if we're selecting an item in the player intentory
            this.HeldItem = this.PlayerInventoryMenu.rightClick(ClickItemLocation.X, ClickItemLocation.Y, this.HeldItem);
            if (this.HeldItem == null)
            {
                // Check the item to grab menu
                //this.HeldItem = this.ItemsToGrabMenu.rightClick(Game1.getMouseX(), Game1.getMouseY(), this.HeldItem);
                this.HeldItem = this.ItemsToGrabMenu.rightClick(ClickItemLocation.X, ClickItemLocation.Y, this.HeldItem);
                if (this.HeldItem == null)
                {
                    // If there's no item here either then there's nothing else to do
                    this.Monitor.Log("No held item", LogLevel.Trace);
                    RestoreNativeCallbacks();
                    return EInputHandled.NotHandled;
                }

                this.Monitor.Log("Item is from grab menu", LogLevel.Trace);
            }
            else // debug
            {
                this.Monitor.Log("Item is from inventory", LogLevel.Trace);
                //OnItemSelect(this.HeldItem, Game1.player);
            }

            this.TotalItems = this.HoverItem.Stack + this.HeldItem.Stack;
            this.StackAmount = this.HeldItem.Stack;

            this.Monitor.Log($"Held item: {this.HeldItem?.Name}", LogLevel.Trace);

            // Create the split menu
            this.SplitMenu = new StackSplitMenu(
                OnStackAmountReceived,
                this.HeldItem.Stack,
                this.HoverItem.Stack
                //this.HoverItem != null ? this.HoverItem.Stack : 0
                );

            return EInputHandled.Consumed;
        }

        protected override void OnStackAmountReceived(string s)
        {
            // Store amount
            if (int.TryParse(s, out this.StackAmount))
            {
                if (this.StackAmount > 0)
                {
                    this.NativeMenu.receiveRightClick(this.ClickItemLocation.X, this.ClickItemLocation.Y);
                }
                else
                {
                    RevertItems();
                }
            }

            base.OnStackAmountReceived(s);
        }

        private void OnItemSelect(Item item, Farmer who)
        {
            this.Monitor.Log("OnItemSelect", LogLevel.Trace);

            MoveItems(item, who, this.PlayerInventoryMenu, this.OriginalItemSelectCallback);
        }

        private void OnItemGrab(Item item, Farmer who)
        {
            this.Monitor.Log("OnItemGrab", LogLevel.Trace);

            MoveItems(item, who, this.ItemsToGrabMenu, this.OriginalItemGrabCallback);
        }

        private void MoveItems(Item item, Farmer who, InventoryMenu inventory, ItemGrabMenu.behaviorOnItemSelect callback)
        {
            Debug.Assert(this.StackAmount > 0);
            if (this.HeldItem != null)
            {
                // shift held while submitting, causing it to do normal shift-click behavior
                bool shiftHeld = (item.Stack > 1);

                // update held item stack and item stack
                int numCurrentlyHeld = this.HeldItem.Stack; // How many we're actually holding.
                int numInPile = this.HoverItem.Stack + item.Stack;//(shiftHeld ? item.Stack : 0);
                int wantToHold = Math.Min(this.TotalItems, Math.Max(this.StackAmount, 0));

                this.HoverItem.Stack = this.TotalItems - wantToHold;
                this.HeldItem.Stack = wantToHold;

                item.Stack = wantToHold;

                // Remove the empty item from the inventory
                if (this.HoverItem.Stack <= 0)
                {
                    int index = inventory.actualInventory.IndexOf(this.HoverItem);
                    if (index > -1)
                        inventory.actualInventory[index] = null;
                }
            }

            RestoreNativeCallbacks();

            // Update stack to the amount set from OnStackAmountReceived
            callback?.Invoke(item, who);
        }

        private void RevertItems()
        {
            if (this.HoverItem != null && this.TotalItems > 0)
            {
                this.Monitor.Log("Reverting items", LogLevel.Trace);
                this.HoverItem.Stack = this.TotalItems;

                RestoreNativeCallbacks();
            }
        }

        private bool HookCallbacks()
        {
            if (this.CallbacksHooked)
            {
                throw new Exception("Callbacks already hooked");
            }

            try
            {
                // Replace the delegates with our own
                var itemSelectCallbackField = this.Helper.Reflection.GetPrivateField<ItemGrabMenu.behaviorOnItemSelect>(this.NativeMenu, "behaviorFunction");
                var itemGrabCallbackField = typeof(ItemGrabMenu).GetField("behaviorOnItemGrab");

                this.OriginalItemGrabCallback = itemGrabCallbackField.GetValue(this.NativeMenu) as ItemGrabMenu.behaviorOnItemSelect;
                this.OriginalItemSelectCallback = itemSelectCallbackField.GetValue();

                itemGrabCallbackField.SetValue(this.NativeMenu, new ItemGrabMenu.behaviorOnItemSelect(this.OnItemGrab));
                itemSelectCallbackField.SetValue(this.OnItemSelect);

                this.CallbacksHooked = true;
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to hook ItemGrabMenu callbacks: {e}", LogLevel.Error);
                return false;
            }
            return true;
        }

        private void RestoreNativeCallbacks()
        {
            if (!this.CallbacksHooked)
                return;

            try
            {
                var itemSelectCallbackField = this.Helper.Reflection.GetPrivateField<ItemGrabMenu.behaviorOnItemSelect>(this.NativeMenu, "behaviorFunction");
                var itemGrabCallbackField = typeof(ItemGrabMenu).GetField("behaviorOnItemGrab");

                itemSelectCallbackField.SetValue(this.OriginalItemSelectCallback);
                itemGrabCallbackField.SetValue(this.NativeMenu, this.OriginalItemGrabCallback);

                this.CallbacksHooked = false;
            }
            catch (Exception)
            {
                this.Monitor.Log("Failed to restore native callbacks");
            }
        }
    }
}
