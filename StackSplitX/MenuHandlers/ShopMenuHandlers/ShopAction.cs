using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;

namespace StackSplitX.MenuHandlers
{
    public abstract class ShopAction : IShopAction
    {
        protected IReflectionHelper Reflection { get; private set; }
        protected IMonitor Monitor { get; private set; }
        protected ShopMenu NativeShopMenu { get; private set; }
        protected InventoryMenu Inventory { get; private set; }
        protected int ShopCurrencyType { get; private set; }
        protected Item ClickedItem = null;
        protected int Amount { get; set; } = 0;

        public int StackAmount => this.Amount;


        /// <summary>Constructor.</summary>
        /// <param name="reflection">ReflectionHelper</param>
        /// <param name="monitor">Monitor.</param>
        /// <param name="menu">Native shop menu.</param>
        /// <param name="item">Clicked item that this action will act on.</param>
        public ShopAction(IReflectionHelper reflection, IMonitor monitor, ShopMenu menu, Item item)
        {
            this.Reflection = reflection;
            this.Monitor = monitor;
            this.NativeShopMenu = menu;
            this.ClickedItem = item;

            try
            {
                this.Inventory = this.Reflection.GetPrivateValue<InventoryMenu>(this.NativeShopMenu, "inventory");
                this.ShopCurrencyType = this.Reflection.GetPrivateValue<int>(this.NativeShopMenu, "currency");
            }
            catch (Exception e)
            {
                this.Monitor.Log($"Failed to get native shop data: {e}");
            }
        }

        /// <summary>Gets the size of the stack the action is acting on.</summary>
        public int GetStackAmount()
        {
            return this.Amount;
        }

        /// <summary>Verifies the conditions to perform te action.</summary>
        public abstract bool CanPerformAction();

        /// <summary>Does the action.</summary>
        /// <param name="amount">Number of items.</param>
        /// <param name="clickLocation">Where the player clicked.</param>
        public abstract void PerformAction(int amount, Point clickLocation);
    }
}
