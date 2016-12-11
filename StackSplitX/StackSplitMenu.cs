using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Menus;

namespace StackSplitX
{
    public class StackSplitMenu : NamingMenu
    {
        /// <summary>The amount being currently held by the player.</summary>
        public int HeldStackAmount { get; private set; }

        /// <summary>The amount in the original stack.</summary>
        public int CurrentStackAmount { get; private set; }

        public StackSplitMenu(NamingMenu.doneNamingBehavior onDoneNaming, int heldStackAmount, int currentStackAmount)
            : base(onDoneNaming, "Select Amount", heldStackAmount.ToString())
        {
            CurrentStackAmount = currentStackAmount;
            HeldStackAmount = heldStackAmount;
        }
    }
}
