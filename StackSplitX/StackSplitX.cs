using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Reflection;
using StardewModdingAPI.Events;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using StackSplitX.MenuHandlers;

namespace StackSplitX
{
    public class StackSplitX : Mod
    {
        private bool IsSubscribed = false;
        private Dictionary<Type, IMenuHandler> MenuHandlers;
        private IMenuHandler CurrentMenuHandler;

        public override void Entry(IModHelper helper)
        {
            MenuEvents.MenuChanged += OnMenuChanged;
            MenuEvents.MenuClosed += OnMenuClosed;

            this.MenuHandlers = new Dictionary<Type, IMenuHandler>()
            {
                { typeof(GameMenu), new GameMenuHandler(helper, this.Monitor) }
                //{ typeof(ShopMenu), new ShopMenuHandler(helper) },
                //{ typeof(ItemGrabMenu), new ItemGrabMenuHandler(helper) },
                //{ typeof(JunimoNoteMenu), new JunimoNoteMenuHandler(helper) }
            };
        }

        private void SubscribeEvents()
        {
            if (!this.IsSubscribed)
            {
                ControlEvents.MouseChanged += OnMouseStateChanged;
                ControlEvents.KeyPressed += OnKeyPressed;
                GraphicsEvents.OnPostRenderEvent += OnDraw;

                this.IsSubscribed = true;
            }
        }

        private void UnsubscribeEvents()
        {
            if (this.IsSubscribed)
            {
                ControlEvents.MouseChanged -= OnMouseStateChanged;
                ControlEvents.KeyPressed -= OnKeyPressed;
                GraphicsEvents.OnPostRenderEvent -= OnDraw;

                this.IsSubscribed = false;
            }
        }

        private void OnMenuClosed(object sender, EventArgsClickableMenuClosed e)
        {
            if (this.CurrentMenuHandler != null)
            {
                this.CurrentMenuHandler.Close();
                this.CurrentMenuHandler = null;

                UnsubscribeEvents();
            }
        }

        private void OnMenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            DebugPrintMenuInfo(e.PriorMenu, e.NewMenu);

            // Resize event; ignore
            if (e.PriorMenu != null && e.PriorMenu.GetType() == e.NewMenu.GetType())
            {
                return;
            }

            if (this.MenuHandlers.ContainsKey(e.NewMenu.GetType()))
            {
                // Close the current one of it's valid
                if (this.CurrentMenuHandler != null)
                {
                    this.CurrentMenuHandler.Close();
                }

                this.CurrentMenuHandler = this.MenuHandlers[e.NewMenu.GetType()];
                this.CurrentMenuHandler.Open(e.NewMenu);

                SubscribeEvents();
            }
        }
        
        
        private void OnMouseStateChanged(object sender, EventArgsMouseStateChanged e)
        {
            if (this.CurrentMenuHandler != null && this.CurrentMenuHandler.IsOpen())
            {
                switch (this.CurrentMenuHandler.HandleMouseInput(e.PriorState, e.NewState))
                {
                    case EInputHandled.Handled:
                        break;
                    case EInputHandled.Consumed:
                        // Consume mouse input.
                        this.Monitor.Log($"Input consumed by handler: {this.CurrentMenuHandler}", LogLevel.Trace);
                        Game1.oldMouseState = e.NewState;
                        break;
                    case EInputHandled.NotHandled:
                        // The click wasn't handled meaning the split menu no longer has focus and should be closed.
                        this.CurrentMenuHandler.CloseSplitMenu();
                        break;
                }
            }
        }

        private void OnKeyPressed(object sender, EventArgsKeyPressed e)
        {
            // Intercept keyboard input while the tooltip is active so numbers don't change the actively equipped item etc.
            // TODO: remove null checks if these events are only called subscribed when it's valid
            if (this.CurrentMenuHandler != null && 
                this.CurrentMenuHandler.HandleKeyboardInput(e.KeyPressed) == EInputHandled.Handled)
            {
                // Obey unless we're hitting 'cancel' keys.
                if (e.KeyPressed != Keys.Escape)
                {
                    Game1.oldKBState = Keyboard.GetState();
                }
                else
                {
                    this.CurrentMenuHandler.CloseSplitMenu();
                }
            }
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            if (this.CurrentMenuHandler != null)
            {
                this.CurrentMenuHandler.Update();
            }
        }

        private void OnDraw(object sender, EventArgs e)
        {
            if (this.CurrentMenuHandler != null)
            {
                this.CurrentMenuHandler.Draw(Game1.spriteBatch);
            }
        }

        #region DebugMenuPrint
        private void DebugPrintMenuInfo(IClickableMenu priorMenu, IClickableMenu newMenu)
        {
#if DEBUG
            try
            {
                string priorName = "None";
                if (priorMenu != null)
                {
                    priorName = priorMenu.GetType().Name;
                }
                string newName = newMenu.GetType().Name;
                Monitor.Log("Menu changed from: " + priorName + " to " + newName);
            }
            catch (Exception ex)
            {
                Monitor.Log("Error getting menu name: " + ex);
            }
#endif
        }
        #endregion DebugMenuPrint
    }
}
