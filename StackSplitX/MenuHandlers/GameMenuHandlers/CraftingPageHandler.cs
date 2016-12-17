using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;

namespace StackSplitX.MenuHandlers
{
    public class CraftingPageHandler : GameMenuPageHandler<CraftingPage>
    {
        private Point ClickItemLocation;

        public CraftingPageHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
        }

        public override EInputHandled OpenSplitMenu(out int stackAmount)
        {
            stackAmount = 1; // Craft 1 by default

            var hoverRecipe = Helper.Reflection.GetPrivateValue<CraftingRecipe>(this.MenuPage, "hoverRecipe");
            var hoveredItem = hoverRecipe?.createItem();
            var heldItem = Helper.Reflection.GetPrivateValue<Item>(this.MenuPage, "heldItem");
            var cooking = Helper.Reflection.GetPrivateValue<bool>(this.MenuPage, "cooking");

            // If we're holding an item already then it must stack with the item we want to craft.
            if (hoveredItem == null || (heldItem != null && heldItem.Name != hoveredItem.Name))
                return EInputHandled.NotHandled;

            // Only allow items that can actually stack
            var extraIems = cooking ? Utility.getHomeOfFarmer(Game1.player).fridge.items : null;
            if (!hoveredItem.canStackWith(hoveredItem) || !hoverRecipe.doesFarmerHaveIngredientsInInventory(extraIems))
                return EInputHandled.NotHandled;

            this.ClickItemLocation = new Point(Game1.getOldMouseX(), Game1.getOldMouseY());
            return EInputHandled.Consumed;
        }

        public override void OnStackAmountEntered(int amount)
        {
            // TODO: check the max amonut able to be crafted to avoid unnecessary iterations
            for (int i = 0; i < amount; ++i)
            {
                this.MenuPage.receiveRightClick(this.ClickItemLocation.X, this.ClickItemLocation.Y);
            }
        }
    }
}
