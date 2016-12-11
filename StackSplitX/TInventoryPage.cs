using System.Reflection;
using StardewValley.Menus;
using StardewValley;

namespace StackSplitX
{
    public class TInventoryPage : InventoryPage
    {
        private TInventoryMenu myInventoryMenu = null;

        public InventoryPage BaseInventoryPage { get; private set; }

        public Item HeldItem
        {
            get { return (Item)Utils.GetNativeFieldInfoByName<InventoryPage>("heldItem").GetValue(BaseInventoryPage); }
            set { Utils.GetNativeFieldInfoByName<InventoryPage>("heldItem").SetValue(BaseInventoryPage, value); }
        }

        public TInventoryPage(int x, int y, int width, int height) : base(x, y, width, height)
        {
            InventoryMenu inventoryMenu = Utils.GetNativeField<InventoryMenu, InventoryPage>(this, "inventory");
            myInventoryMenu = TInventoryMenu.ConstructFromBaseClass(inventoryMenu);
        }

        public static TInventoryPage ConstructFromBaseClass(InventoryPage baseClass)
        {
            var s = new TInventoryPage(baseClass.xPositionOnScreen, baseClass.yPositionOnScreen, baseClass.width, baseClass.height);
            s.BaseInventoryPage = baseClass;
            return s;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            HeldItem = myInventoryMenu.CustomRightClick(x, y, HeldItem, playSound);
            //base.receiveRightClick(x, y, playSound);
        }
    }
}
