using StardewModdingAPI;
using StardewValley.Menus;
using System;

namespace StackSplitX.MenuHandlers
{
    // This class is for handling the standalone crafting pages (aka cooking).
    // TODO: reduce duplication between this and the game menu/crafting page.
    class CraftingMenuHandler : BaseMenuHandler<CraftingPage>
    {
        private CraftingPageHandler CraftingPageHandler;

        public CraftingMenuHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
            this.CraftingPageHandler = new CraftingPageHandler(helper, monitor);
        }

        public override void Open(IClickableMenu menu)
        {
            base.Open(menu);
            this.CraftingPageHandler.Open(menu, this.NativeMenu);
        }

        public override void Close()
        {
            base.Close();
            this.CraftingPageHandler.Close();
        }

        protected override EInputHandled OpenSplitMenu()
        {
            int stackAmount = 0;
            var handled = this.CraftingPageHandler.OpenSplitMenu(out stackAmount);
            if (handled != EInputHandled.NotHandled)
            {
                this.SplitMenu = new StackSplitMenu(OnStackAmountReceived, stackAmount);
            }
            return handled;
        }

        protected override void OnStackAmountReceived(string s)
        {
            int amount = 0;
            if (int.TryParse(s, out amount))
            {
                this.CraftingPageHandler.OnStackAmountEntered(amount);
            }
            base.OnStackAmountReceived(s);
        }
    }
}
