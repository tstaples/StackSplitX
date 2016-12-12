using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackSplitX.MenuHandlers
{
    public class GameMenuHandler : BaseMenuHandler
    {
        private InventoryPage InventoryPage = null;
        private InventoryMenu InventoryMenu = null;
        private Item HeldItem = null;
        private Item HoveredItem = null;

        public GameMenuHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        public override bool IsOpen()
        {
            return base.IsOpen();
        }

        public override void Open(IClickableMenu menu)
        {
            base.Open(menu);
        }

        public override void Close()
        {
            base.Close();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        protected override bool CanOpenSplitMenu()
        {
            // Check the current tab is valid
            Debug.Assert(this.NativeMenu != null && this.NativeMenu is GameMenu);
            return (this.NativeMenu as GameMenu).currentTab == GameMenu.inventoryTab;
        }

        protected override EInputHandled OpenSplitMenu()
        {
            bool isAlreadyHoldingItem = (this.HeldItem != null);

            try
            {
                var pages = Helper.Reflection.GetPrivateValue<List<IClickableMenu>>(this.NativeMenu, "pages");
                this.InventoryPage = pages[GameMenu.inventoryTab] as InventoryPage;
                this.InventoryMenu = Helper.Reflection.GetPrivateValue<InventoryMenu>(this.InventoryPage, "inventory");
                this.HoveredItem = Helper.Reflection.GetPrivateValue<Item>(this.InventoryPage, "hoveredItem");

                // Get the current held item if any
                var heldItemField = this.Helper.Reflection.GetPrivateField<Item>(this.InventoryPage, "heldItem");
                this.HeldItem = heldItemField.GetValue();
                // Emulate the right click method that would normally happen (native code passes in held item hence above).
                this.HeldItem = this.InventoryMenu.rightClick(Game1.getMouseX(), Game1.getMouseY(), this.HeldItem);
                // Update the native object's held item.
                this.Monitor.Log($"Held item: {this.HeldItem.Name}", LogLevel.Trace);
                heldItemField.SetValue(this.HeldItem);
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to get Inventory values: {e}", LogLevel.Error);
                return EInputHandled.NotHandled;
            }

            if (this.HoveredItem == null)
            {
                this.Monitor.Log("Not hovering over any item", LogLevel.Trace);
                return EInputHandled.NotHandled;
            }

            // If we were holding it and we're now clicking a slot of a different item type then hide the tooltip
            if (isAlreadyHoldingItem && this.HoveredItem?.Name != this.HeldItem?.Name)
            {
                this.Monitor.Log($"Already holding item: {this.HeldItem.Name} and a different item was selected.", LogLevel.Trace);
                return EInputHandled.NotHandled;
            }

            // Ignore for empty slots or stacks of 1
            if (this.HeldItem == null || this.HeldItem.Stack <= 1)
            {
                this.Monitor.Log("Held item is invalid or the stack is too small.", LogLevel.Trace);
                return EInputHandled.NotHandled;
            }

            // Create the split menu
            this.SplitMenu = new StackSplitMenu(
                OnStackAmountReceived, 
                this.HeldItem.Stack, 
                this.HoveredItem != null ? this.HoveredItem.Stack : 0
                );

            return EInputHandled.Consumed;
        }

        protected override void OnStackAmountReceived(string s)
        {
            Monitor.Log(s);

            int amount = -1;
            if (!int.TryParse(s, out amount))
            {
                this.Monitor.Log("Invalid amount input", LogLevel.Trace);
                base.OnStackAmountReceived(s);
                return;
            }

            if (amount < 0)
            {
                this.Monitor.Log("Amount is < 0", LogLevel.Trace);
                base.OnStackAmountReceived(s);
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

            base.OnStackAmountReceived(s);
        }
    }
}
