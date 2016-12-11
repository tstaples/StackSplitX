using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;

namespace StackSplitX
{
    public class TInventoryMenu : InventoryMenu
    {
        public InventoryMenu BaseInventoryPage { get; private set; }

        public static TInventoryMenu ConstructFromBaseClass(InventoryMenu baseClass)
        {
            var s = new TInventoryMenu(0, 0, true);
            s.BaseInventoryPage = baseClass;
            return s;
        }

		public TInventoryMenu(int xPosition, int yPosition, bool playerInventory) : base(xPosition, yPosition, playerInventory)
        {

        }


        public Item CustomRightClick(int x, int y, Item toAddTo, bool playSound = true)
        {
            foreach (ClickableComponent current in this.inventory)
            {
                int num = Convert.ToInt32(current.name);
                if (current.containsPoint(x, y) && (this.actualInventory[num] == null || this.highlightMethod(this.actualInventory[num])) && num < this.actualInventory.Count && this.actualInventory[num] != null)
                {
                    if (this.actualInventory[num] is Tool && (toAddTo == null || toAddTo is StardewValley.Object) && (this.actualInventory[num] as Tool).canThisBeAttached((StardewValley.Object)toAddTo))
                    {
                        Item result = (this.actualInventory[num] as Tool).attach((toAddTo == null) ? null : ((StardewValley.Object)toAddTo));
                        return result;
                    }
                    if (toAddTo == null)
                    {
                        if (this.actualInventory[num].maximumStackSize() != -1)
                        {
                            if (num == Game1.player.CurrentToolIndex && this.actualInventory[num] != null && this.actualInventory[num].Stack == 1)
                            {
                                this.actualInventory[num].actionWhenStopBeingHeld(Game1.player);
                            }
                            Item one = this.actualInventory[num].getOne();
                            if (this.actualInventory[num].Stack > 1 && Game1.isOneOfTheseKeysDown(Game1.oldKBState, new InputButton[]
                            {
                                new InputButton(Keys.LeftShift)
                            }))
                            {
                                one.Stack = (int)Math.Ceiling((double)this.actualInventory[num].Stack / 2.0);
                                this.actualInventory[num].Stack = this.actualInventory[num].Stack / 2;
                            }
                            else if (this.actualInventory[num].Stack == 1)
                            {
                                this.actualInventory[num] = null;
                            }
                            else
                            {
                                this.actualInventory[num].Stack--;
                            }
                            if (this.actualInventory[num] != null && this.actualInventory[num].Stack <= 0)
                            {
                                this.actualInventory[num] = null;
                            }
                            if (playSound)
                            {
                                Game1.playSound("dwop");
                            }
                            Item result = one;
                            return result;
                        }
                    }
                    else if (this.actualInventory[num].canStackWith(toAddTo) && toAddTo.Stack < toAddTo.maximumStackSize())
                    {
                        if (Game1.isOneOfTheseKeysDown(Game1.oldKBState, new InputButton[]
                        {
                            new InputButton(Keys.LeftShift)
                        }))
                        {
                            toAddTo.Stack += (int)Math.Ceiling((double)this.actualInventory[num].Stack / 2.0);
                            this.actualInventory[num].Stack = this.actualInventory[num].Stack / 2;
                        }
                        else
                        {
                            toAddTo.Stack++;
                            this.actualInventory[num].Stack--;
                        }
                        if (playSound)
                        {
                            Game1.playSound("dwop");
                        }
                        if (this.actualInventory[num].Stack <= 0)
                        {
                            if (num == Game1.player.CurrentToolIndex)
                            {
                                this.actualInventory[num].actionWhenStopBeingHeld(Game1.player);
                            }
                            this.actualInventory[num] = null;
                        }
                        Item result = toAddTo;
                        return result;
                    }
                }
            }
            return toAddTo;
        }
    }
}
