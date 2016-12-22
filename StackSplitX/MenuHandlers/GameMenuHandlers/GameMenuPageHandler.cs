using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackSplitX.MenuHandlers
{
    public abstract class GameMenuPageHandler<TPageType> : IGameMenuPageHandler where TPageType : IClickableMenu
    {

        protected IClickableMenu NativeMenu { get; private set; }
        protected TPageType MenuPage { get; private set; }
        protected IModHelper Helper { get; private set; }
        protected IMonitor Monitor { get; private set; }

        public GameMenuPageHandler(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;
        }

        public virtual void Open(IClickableMenu menu, IClickableMenu page)
        {
            this.NativeMenu = menu;
            this.MenuPage = page as TPageType;
        }

        public virtual void Close()
        {
            this.NativeMenu = null;
            this.MenuPage = null;
        }

        public virtual EInputHandled OpenSplitMenu(out int stackAmount)
        {
            stackAmount = 0;
            return EInputHandled.NotHandled;
        }

        public virtual EInputHandled CancelMove()
        {
            return EInputHandled.NotHandled;
        }

        public virtual void OnStackAmountEntered(int amount)
        {

        }
    }
}
