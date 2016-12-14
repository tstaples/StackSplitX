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
    public abstract class BaseMenuHandler : IMenuHandler
    {
        private bool IsMenuOpen = false;
        protected StackSplitMenu SplitMenu = null;
        protected IClickableMenu NativeMenu { get; set; }
        protected IModHelper Helper { get; private set; }
        protected IMonitor Monitor { get; private set; }

        public BaseMenuHandler(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;
        }

        public virtual bool IsOpen()
        {
            return this.IsMenuOpen;
        }

        public virtual void Open(IClickableMenu menu)
        {
            this.NativeMenu = menu;
            this.IsMenuOpen = true;
        }

        public virtual void Close()
        {
            this.IsMenuOpen = false;
            this.SplitMenu = null;
        }

        public virtual void Update()
        {
            this.SplitMenu?.Update();
        }

        public virtual void CloseSplitMenu()
        {
            if (this.SplitMenu != null)
            {
                this.SplitMenu = null;
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (this.SplitMenu != null)
            {
                this.SplitMenu.draw(spriteBatch);
            }
        }

        public EInputHandled HandleMouseInput(MouseState priorState, MouseState newState)
        {
            // Was right click pressed
            if (Utils.WasPressedThisFrame(priorState.RightButton, newState.RightButton))
            {
                // Invoke split menu if the modifier key was also down
                if (IsModifierKeyDown() && CanOpenSplitMenu())
                {
                    // Cancel the current operation
                    if (this.SplitMenu != null)
                    {
                        CancelMove();
                    }

                    // TODO: have this return consumed for shops and stuff where we need to act before the click happens
                    return OpenSplitMenu();
                }
                return EInputHandled.NotHandled;
            }
            else if (Utils.WasPressedThisFrame(priorState.LeftButton, newState.LeftButton) && this.SplitMenu != null)
            {
                // If the player clicks within the bounds of the tooltip then forward the input to that. 
                // Otherwise they're clicking elsewhere and we should close the tooltip.
                if (this.SplitMenu.ContainsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    this.SplitMenu.ReceiveLeftClick(Game1.getMouseX(), Game1.getMouseY());
                    return EInputHandled.Consumed;
                }
                else
                {
                    // Lost focus; cancel the move (run default behavior)
                    return CancelMove();
                }
            }
            else if (this.SplitMenu != null)
            {
                // For other input events (ie. mouse move) don't close the split menu if it's open.
                return EInputHandled.Handled;
            }
            return EInputHandled.NotHandled;
        }

        public EInputHandled HandleKeyboardInput(Keys keyPressed)
        {
            if (ShouldConsumeKeyboardInput(keyPressed))
            {
                return EInputHandled.Handled;
            }
            return EInputHandled.NotHandled;
        }

        protected virtual bool ShouldConsumeKeyboardInput(Keys keyPressed)
        {
            return (this.SplitMenu != null);
        }

        protected virtual bool CanOpenSplitMenu()
        {
            return true;
        }

        protected abstract EInputHandled OpenSplitMenu();

        // Called when lost focus
        protected virtual EInputHandled CancelMove()
        {
            this.Monitor.Log("Canceled move", LogLevel.Trace);
            return EInputHandled.NotHandled;
        }

        protected virtual void OnStackAmountReceived(string s)
        {
            CloseSplitMenu();
        }

        #region Input Util
        protected bool IsModifierKeyDown()
        {
            // TODO: load modifier keys from config settings
            return Utils.IsAnyKeyDown(Game1.oldKBState, new Keys[] { Keys.LeftAlt, Keys.LeftShift });
        }
        #endregion Input Util
    }
}
