using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackSplitX.MenuHandlers
{
    public class ShopMenuHandler : BaseMenuHandler
    {
        internal enum ESaleType
        {
            Purchasing,
            Selling
        }

        // Default amount when shift+right clicking
        private const int DefaultShopStackAmount = 5;

        private InventoryMenu Inventory = null;
        private Item ClickedItem = null;
        private Point ClickItemLocation;
        private int StackAmount = 0;
        private int ShopCurrencyType = 0;
        private ESaleType SaleType;

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
            OnStackAmountReceived(this.StackAmount.ToString());
            return EInputHandled.NotHandled;
        }

        protected override EInputHandled OpenSplitMenu()
        {
            try
            {
                Debug.Assert(this.NativeMenu is ShopMenu);
                var shopMenu = this.NativeMenu as ShopMenu;

                this.Inventory = this.Helper.Reflection.GetPrivateValue<InventoryMenu>(shopMenu, "inventory");
                this.ShopCurrencyType = this.Helper.Reflection.GetPrivateValue<int>(this.NativeMenu, "currency");
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to get properties from native menu: {e}", LogLevel.Error);
                return EInputHandled.NotHandled;
            }

            // Check if it was the shop or inventory that was clicked and initialize the appropriate values
            this.ClickItemLocation = new Point(Game1.getOldMouseX(), Game1.getOldMouseY());
            if (!TryShopClicked(this.ClickItemLocation) && !TryInventoryClicked(this.ClickItemLocation))
                return EInputHandled.NotHandled;

            this.SplitMenu = new StackSplitMenu(OnStackAmountReceived, this.StackAmount);

            return EInputHandled.Consumed;
        }

        protected override void OnStackAmountReceived(string s)
        {
            // Store amount
            if (int.TryParse(s, out this.StackAmount))
            {
                if (this.StackAmount > 0)
                {
                    if (this.SaleType == ESaleType.Purchasing)
                    {
                        BuyItem(this.ClickedItem, this.StackAmount);
                    }
                    else if (this.SaleType == ESaleType.Selling)
                    {
                        SellItem(this.ClickedItem, this.StackAmount);
                    }
                }
            }

            base.OnStackAmountReceived(s);
        }

        private bool TryShopClicked(Point p)
        {
            var heldItem = this.Helper.Reflection.GetPrivateValue<Item>(this.NativeMenu, "heldItem");

            this.ClickedItem = GetClickedShopItem(this.ClickItemLocation);
            if (this.ClickedItem != null && (heldItem == null || this.ClickedItem.canStackWith(heldItem)))
            {
                this.StackAmount = DefaultShopStackAmount;
                this.SaleType = ESaleType.Purchasing;

                return true;
            }
            return false;
        }

        private bool TryInventoryClicked(Point p)
        {
            var shopMenu = this.NativeMenu as ShopMenu;

            this.ClickedItem = this.Inventory.getItemAt(this.ClickItemLocation.X, this.ClickItemLocation.Y);
            if (this.ClickedItem != null && shopMenu.highlightItemToSell(this.ClickedItem) && this.ClickedItem.Stack > 1)
            {
                this.SaleType = ESaleType.Selling;
                this.StackAmount = (int)Math.Ceiling(this.ClickedItem.Stack / 2.0);

                return true;
            }
            return false;
        }

        private void SellItem(Item item, int amount)
        {
            amount = Math.Min(amount, item.Stack);
            this.StackAmount = amount; // Remove if we don't need to carry this around

            // Sell item
            int price = CalculateSalePrice(item, amount);
            ShopMenu.chargePlayer(Game1.player, this.ShopCurrencyType, price);
            this.Monitor.Log($"Charged player {price} for {amount} of {item.Name}", LogLevel.Trace);

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

        private Item GetClickedShopItem(Point p)
        {
            var itemsForSale = this.Helper.Reflection.GetPrivateValue<List<Item>>(this.NativeMenu, "forSale");
            int index = GetClickedItemIndex(p);
            Debug.Assert(index < itemsForSale.Count);
            return index >= 0 ? itemsForSale[index] : null;
        }

        private int GetClickedItemIndex(Point p)
        {
            int currentItemIndex = this.Helper.Reflection.GetPrivateValue<int>(this.NativeMenu, "currentItemIndex");
            var forSaleButtons = this.Helper.Reflection.GetPrivateValue<List<ClickableComponent>>(this.NativeMenu, "forSaleButtons");
            int saleButtonIndex = forSaleButtons.FindIndex(button => button.containsPoint(p.X, p.Y));
            return currentItemIndex + saleButtonIndex;
        }

        private void BuyItem(Item item, int amount)
        {
            var priceAndStockField = this.Helper.Reflection.GetPrivateField<Dictionary<Item, int[]>>(this.NativeMenu, "itemPriceAndStock");
            var priceAndStockMap = priceAndStockField.GetValue();
            Debug.Assert(priceAndStockMap.ContainsKey(item));

            // Calculate the number to purchase
            int numInStock = priceAndStockMap[item][1];
            int itemPrice = priceAndStockMap[item][0];
            int currentMonies = ShopMenu.getPlayerCurrencyAmount(Game1.player, this.ShopCurrencyType);
            amount = Math.Min(Math.Min(amount, currentMonies / itemPrice), numInStock);

            this.Monitor.Log($"Attempting to purchase {amount} of {item.Name} for {itemPrice * amount}", LogLevel.Trace);

            if (amount <= 0)
                return;

            var heldItem = this.Helper.Reflection.GetPrivateValue<Item>(this.NativeMenu, "heldItem");

            // Try to purchase the item - method returns true if it should be removed from the shop since there's no more.
            var purchaseMethodInfo = this.Helper.Reflection.GetPrivateMethod(this.NativeMenu, "tryToPurchaseItem");
            var p = this.ClickItemLocation;
            if (purchaseMethodInfo.Invoke<bool>(item, heldItem, amount, p.X, p.Y, GetClickedItemIndex(p)))
            {
                this.Monitor.Log($"Purchase of {item.Name} successful", LogLevel.Trace);

                // remove the purchased item from the stock etc.
                priceAndStockMap.Remove(item);
                priceAndStockField.SetValue(priceAndStockMap);

                var itemsForSaleField = this.Helper.Reflection.GetPrivateField<List<Item>>(this.NativeMenu, "forSale");
                var itemsForSale = itemsForSaleField.GetValue();
                itemsForSale.Remove(item);
                itemsForSaleField.SetValue(itemsForSale);
            }
        }
    }
}
