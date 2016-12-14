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
using Microsoft.Xna.Framework.Input;

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

        protected override bool CanOpenSplitMenu()
        {
            bool canOpen = (this.NativeMenu as ItemGrabMenu).allowRightClick;
            return (canOpen && base.CanOpenSplitMenu());
        }

        public override void CloseSplitMenu()
        {
            base.CloseSplitMenu();

            if (this.CallbacksHooked)
                this.Monitor.Log("[CloseSplitMenu] Callbacks shouldn't be hooked", LogLevel.Error);
        }

        protected override EInputHandled CancelMove()
        {
            base.CancelMove();

            if (this.HoverItem != null)
            {
                // If being cancelled from a click else-where then the keyboad state won't have shift held (unless they're still holding it),
                // in which case the default right-click behavior will run and only a single item will get moved instead of half the stack.
                // Therefore we must make sure it's still using our callback so we can correct the amount.
                HookCallbacks();

                // Run the regular command
                this.NativeMenu?.receiveRightClick(this.ClickItemLocation.X, this.ClickItemLocation.Y);

                CloseSplitMenu();

                // Consume input so the menu doesn't run left click logic as well
                return EInputHandled.Consumed;
            }

            // Consume input so the menu doesn't run left click logic as well
            return EInputHandled.NotHandled;
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

            // Do nothing if we're not hovering over an item
            if (this.HoverItem == null)
            {
                this.Monitor.Log("No hover item", LogLevel.Trace);
                return EInputHandled.NotHandled;
            }

            // Store where the cursor was in case it moves before submitting the split menu
            // as proceeding parts will use the now moved mouse location.
            this.ClickItemLocation = new Point(Game1.getOldMouseX(), Game1.getOldMouseY());

            this.TotalItems = this.HoverItem.Stack;
            this.StackAmount = (int)Math.Ceiling(this.TotalItems / 2.0); // default at half

            this.Monitor.Log($"Hovered item: {this.HoverItem.Name} | Total items: {this.TotalItems} | Held amount: {this.StackAmount} | Hovered amount: {this.HoverItem.Stack}", LogLevel.Trace);

            // Create the split menu
            this.SplitMenu = new StackSplitMenu(
                OnStackAmountReceived,
                this.StackAmount,
                this.TotalItems / 2
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
                    if (!HookCallbacks())
                        throw new Exception("Failed to hook callbacks");

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
            //this.Monitor.Log("OnItemSelect", LogLevel.Trace);

            MoveItems(item, who, this.PlayerInventoryMenu, this.OriginalItemSelectCallback);
        }

        private void OnItemGrab(Item item, Farmer who)
        {
            //this.Monitor.Log("OnItemGrab", LogLevel.Trace);

            MoveItems(item, who, this.ItemsToGrabMenu, this.OriginalItemGrabCallback);
        }

        private void MoveItems(Item item, Farmer who, InventoryMenu inventory, ItemGrabMenu.behaviorOnItemSelect callback)
        {
            Debug.Assert(this.StackAmount > 0);

            // Get the held item now that it's been set by the native receiveRightClick call
            this.HeldItem = GetHeldItem();
            if (this.HeldItem != null)
            {
                // update held item stack and item stack
                int numCurrentlyHeld = this.HeldItem.Stack; // How many we're actually holding.
                int numInPile = this.HoverItem.Stack + item.Stack;
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
                return true;

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

        private Item GetHeldItem()
        {
            var nativeMenuWithInventory = (this.NativeMenu as MenuWithInventory);
            return nativeMenuWithInventory.heldItem;
        }
    }
}
