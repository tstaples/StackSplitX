using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackSplitX.MenuHandlers
{
    class BuyAction : ShopAction
    {
        // Default amount when shift+right clicking
        private const int DefaultShopStackAmount = 5;

        public BuyAction(IReflectionHelper reflection, IMonitor monitor, ShopMenu menu, Item item)
            : base(reflection, monitor, menu, item)
        {
            // Default amount
            this.Amount = DefaultShopStackAmount;
        }

        public override bool CanPerformAction()
        {
            var heldItem = this.Reflection.GetPrivateValue<Item>(this.NativeShopMenu, "heldItem");
            int currentMonies = ShopMenu.getPlayerCurrencyAmount(Game1.player, this.ShopCurrencyType);

            return (this.ClickedItem != null && 
                    (heldItem == null || (this.ClickedItem.canStackWith(heldItem) && heldItem.Stack < heldItem.maximumStackSize())) && // Holding the same item and not hold max stack
                    this.ClickedItem.canStackWith(this.ClickedItem) && // Item type is stackable
                    currentMonies >= this.ClickedItem.salePrice()); // Can afford
        }

        public override void PerformAction(int amount, Point clickLocation)
        {
            var priceAndStockField = this.Reflection.GetPrivateField<Dictionary<Item, int[]>>(this.NativeShopMenu, "itemPriceAndStock");
            var priceAndStockMap = priceAndStockField.GetValue();
            Debug.Assert(priceAndStockMap.ContainsKey(this.ClickedItem));

            // Calculate the number to purchase
            int numInStock = priceAndStockMap[this.ClickedItem][1];
            int itemPrice = priceAndStockMap[this.ClickedItem][0];
            int currentMonies = ShopMenu.getPlayerCurrencyAmount(Game1.player, this.ShopCurrencyType);
            amount = Math.Min(Math.Min(amount, currentMonies / itemPrice), Math.Min(numInStock, this.ClickedItem.maximumStackSize()));

            this.Monitor.Log($"Attempting to purchase {amount} of {this.ClickedItem.Name} for {itemPrice * amount}", LogLevel.Trace);

            if (amount <= 0)
                return;

            var heldItem = this.Reflection.GetPrivateValue<Item>(this.NativeShopMenu, "heldItem");

            // Try to purchase the item - method returns true if it should be removed from the shop since there's no more.
            var purchaseMethodInfo = this.Reflection.GetPrivateMethod(this.NativeShopMenu, "tryToPurchaseItem");
            int index = BuyAction.GetClickedItemIndex(this.Reflection, this.NativeShopMenu, clickLocation);
            if (purchaseMethodInfo.Invoke<bool>(this.ClickedItem, heldItem, amount, clickLocation.X, clickLocation.Y, index))
            {
                this.Monitor.Log($"Purchase of {this.ClickedItem.Name} successful", LogLevel.Trace);

                // remove the purchased item from the stock etc.
                priceAndStockMap.Remove(this.ClickedItem);
                priceAndStockField.SetValue(priceAndStockMap);

                var itemsForSaleField = this.Reflection.GetPrivateField<List<Item>>(this.NativeShopMenu, "forSale");
                var itemsForSale = itemsForSaleField.GetValue();
                itemsForSale.Remove(this.ClickedItem);
                itemsForSaleField.SetValue(itemsForSale);
            }
        }

        public static Item GetClickedShopItem(IReflectionHelper reflection, ShopMenu shopMenu, Point p)
        {
            var itemsForSale = reflection.GetPrivateValue<List<Item>>(shopMenu, "forSale");
            int index = GetClickedItemIndex(reflection, shopMenu, p);
            Debug.Assert(index < itemsForSale.Count);
            return index >= 0 ? itemsForSale[index] : null;
        }

        public static int GetClickedItemIndex(IReflectionHelper reflection, ShopMenu shopMenu, Point p)
        {
            int currentItemIndex = reflection.GetPrivateValue<int>(shopMenu, "currentItemIndex");
            var forSaleButtons = reflection.GetPrivateValue<List<ClickableComponent>>(shopMenu, "forSaleButtons");
            int saleButtonIndex = forSaleButtons.FindIndex(button => button.containsPoint(p.X, p.Y));
            return saleButtonIndex > -1 ? currentItemIndex + saleButtonIndex : -1;
        }
    }
}
