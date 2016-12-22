using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackSplitX.MenuHandlers
{
    public class GameMenuHandler : BaseMenuHandler<GameMenu>
    {
        protected const int InvalidTab = -1;

        private Dictionary<int, IGameMenuPageHandler> PageHandlers;
        private IGameMenuPageHandler CurrentPageHandler = null;
        private int PreviousTab = InvalidTab;
        private int CurrentTab => this.NativeMenu.currentTab;

        public GameMenuHandler(IModHelper helper, IMonitor monitor)
            : base(helper, monitor)
        {
            PageHandlers = new Dictionary<int, IGameMenuPageHandler>()
            {
                { GameMenu.inventoryTab, new InventoryPageHandler(helper, monitor) },
                { GameMenu.craftingTab, new CraftingPageHandler(helper, monitor) }
            };
        }

        public override void Close()
        {
            base.Close();

            this.CurrentPageHandler?.Close();
            this.CurrentPageHandler = null;
            this.PreviousTab = InvalidTab;
        }

        protected override EInputHandled CancelMove()
        {
            base.CancelMove();
            return this.CurrentPageHandler != null
                ? this.CurrentPageHandler.CancelMove()
                : EInputHandled.NotHandled;
        }

        protected override bool CanOpenSplitMenu()
        {
            // Check the current tab is valid
            return this.PageHandlers.ContainsKey(this.CurrentTab);
        }

        protected override EInputHandled OpenSplitMenu()
        {
            if (!ChangeTabs(this.CurrentTab))
            {
                this.Monitor.Log($"Could not change to tab {this.CurrentTab}", LogLevel.Trace);
                return EInputHandled.NotHandled;
            }

            int stackAmount = 0;
            var handled = this.CurrentPageHandler.OpenSplitMenu(out stackAmount);
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
                this.CurrentPageHandler.OnStackAmountEntered(amount);
            }
            base.OnStackAmountReceived(s);
        }

        private bool ChangeTabs(int newTab)
        {
            if (this.PreviousTab == newTab)
                return true;

            this.CurrentPageHandler?.Close();

            if (this.PageHandlers.ContainsKey(newTab))
            {
                this.PreviousTab = newTab;
                this.CurrentPageHandler = this.PageHandlers[newTab];

                var pages = Helper.Reflection.GetPrivateValue<List<IClickableMenu>>(this.NativeMenu, "pages");
                this.CurrentPageHandler.Open(this.NativeMenu, pages[newTab]);
                return true;
            }
            return false;
        }
    }
}
