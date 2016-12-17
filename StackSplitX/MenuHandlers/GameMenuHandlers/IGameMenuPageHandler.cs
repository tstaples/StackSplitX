using StardewValley.Menus;

namespace StackSplitX.MenuHandlers
{
    public interface IGameMenuPageHandler
    {
        void Open(IClickableMenu menu, IClickableMenu page);
        void Close();
        EInputHandled OpenSplitMenu(out int stackAmount);
        void OnStackAmountEntered(int amount);
    }
}
