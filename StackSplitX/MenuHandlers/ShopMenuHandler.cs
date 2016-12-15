using Microsoft.Xna.Framework;
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
    public class ShopMenuHandler : BaseMenuHandler
    {
        internal enum ESaleType
        {
            Purchasing,
            Selling
        }

        private InventoryMenu Inventory = null;
        private Item ClickedItem = null;
        private Point ClickItemLocation;
        private int StackAmount = 0;
        private ESaleType SaleType;

        public ShopMenuHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        protected override float GetRightClickPollingInterval()
        {
            return 300f; // From ShopMenu.receiveRightClick
        }

        protected override EInputHandled OpenSplitMenu()
        {
            var shopMenu = this.NativeMenu as ShopMenu;
            try
            {
                Debug.Assert(this.NativeMenu is ShopMenu);
                this.Inventory = this.Helper.Reflection.GetPrivateValue<InventoryMenu>(shopMenu, "inventory");
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to get properties from native menu: {e}", LogLevel.Error);
                return EInputHandled.NotHandled;
            }

            this.ClickItemLocation = new Point(Game1.getOldMouseX(), Game1.getOldMouseY());
            if (ClickedShopItem(this.ClickItemLocation))
            {
                this.Monitor.Log("Clicked shop", LogLevel.Trace);

                this.StackAmount = 5; // Default shift-rightclick amount (TODO: acount for clicking inventory)
                this.SaleType = ESaleType.Purchasing;
                //this.StackAmount = (int)Math.Ceiling(this.HoverItem.Stack / 2.0); // default at half
                // TODO
                return EInputHandled.NotHandled;
            }

            this.ClickedItem = this.Inventory.getItemAt(this.ClickItemLocation.X, this.ClickItemLocation.Y);
            if (this.ClickedItem == null || !shopMenu.highlightItemToSell(this.ClickedItem) || this.ClickedItem.Stack <= 1)
            {
                return EInputHandled.NotHandled;
            }

            this.Monitor.Log($"Clicked inventory item: {this.ClickedItem.Name}", LogLevel.Trace);
            this.SaleType = ESaleType.Selling;
            this.StackAmount = (int)Math.Ceiling(this.ClickedItem.Stack / 2.0);

            this.SplitMenu = new StackSplitMenu(
                OnStackAmountReceived,
                this.StackAmount,
                0
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
                    switch (this.SaleType)
                    {
                        case ESaleType.Purchasing:
                            // TODO
                            break;
                        case ESaleType.Selling:
                            SellItem(this.ClickedItem, this.StackAmount);
                            break;
                    }

                }
            }

            base.OnStackAmountReceived(s);
        }

        private void SellItem(Item item, int amount)
        {
            amount = Math.Min(amount, item.Stack);
            this.StackAmount = amount; // Remove if we don't need to carry this around

            // Sell item
            int currencyType = this.Helper.Reflection.GetPrivateValue<int>(this.NativeMenu, "currency");
            int price = CalculateSalePrice(item, amount);
            ShopMenu.chargePlayer(Game1.player, currencyType, price);
            this.Monitor.Log($"Charged player {price} for {amount} of {item.Name}");

            // Update the stack amount/remove the item
            var actualInventory = this.Inventory.actualInventory;
            var index = actualInventory.IndexOf(item);
            if (index >= 0 && index < actualInventory.Count)
            {
                int amountRemaining = item.Stack - amount;
                if (amountRemaining > 0)
                    actualInventory[index].Stack = amountRemaining;
                else
                    actualInventory[index] = null;
            }

            Game1.playSound("purchaseClick");

            // The animation seems to only play when we sell 1
            if (amount == 1)
            {
                // Play the sell animation
                var animationsField = this.Helper.Reflection.GetPrivateField<List<TemporaryAnimatedSprite>>(this.NativeMenu, "animations");
                var animations = animationsField.GetValue();

                // Messy because it's a direct copy-paste from the source code
                Vector2 value = this.Inventory.snapToClickableComponent(this.ClickItemLocation.X, this.ClickItemLocation.Y);
                animations.Add(new TemporaryAnimatedSprite(Game1.debrisSpriteSheet, new Rectangle(Game1.random.Next(2) * Game1.tileSize, 256, Game1.tileSize, Game1.tileSize), 9999f, 1, 999, value + new Vector2(32f, 32f), false, false)
                {
                    alphaFade = 0.025f,
                    motion = Utility.getVelocityTowardPoint(new Point((int)value.X + 32, (int)value.Y + 32), Game1.dayTimeMoneyBox.position + new Vector2(96f, 196f), 12f),
                    acceleration = Utility.getVelocityTowardPoint(new Point((int)value.X + 32, (int)value.Y + 32), Game1.dayTimeMoneyBox.position + new Vector2(96f, 196f), 0.5f)
                });

                animationsField.SetValue(animations);
            }
        }

        private int CalculateSalePrice(Item item, int amount)
        {
            // Formula from ShopMenu.cs
            float sellPercentage = this.Helper.Reflection.GetPrivateValue<float>(this.NativeMenu, "sellPercentage");
            int price = 0;
            if (item is StardewValley.Object)
            {
                float sellPrice = (item as StardewValley.Object).sellToStorePrice();
                price = (int)(sellPrice * sellPercentage);
            }
            else
            {
                price = (int)(item.salePrice() * 0.5f * sellPercentage) * amount;
            }

            // Invert so we give the player money instead (shitty but it's what the game does).
            return -price;
        }

        private bool ClickedShopItem(Point p)
        {
            var shopMenu = this.NativeMenu as ShopMenu;
            var forSaleButtons = this.Helper.Reflection.GetPrivateValue<List<ClickableComponent>>(shopMenu, "forSaleButtons");
            for (int i = 0; i < forSaleButtons.Count; ++i)
            {
                if (forSaleButtons[i].bounds.Contains(p))
                    return true;
            }
            return false;
        }
    }
}
