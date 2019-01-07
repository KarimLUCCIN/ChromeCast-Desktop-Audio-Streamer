using GalaSoft.MvvmLight;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using MiniCast.Client.ViewModel.Chromecast;

namespace MiniCast.Client.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private HamburgerMenuItemCollection _menuItems;
        private HamburgerMenuItemCollection _menuOptionItems;

        public MainViewModel()
        {
            this.CreateMenuItems();

            ViewModelLocator.Instance.Hue.BeginUpdateEveryFrame();
        }

        public void CreateMenuItems()
        {
            MenuItems = new HamburgerMenuItemCollection
            {
                new HamburgerMenuIconItem()
                {
                    Icon = new PackIconMaterial() {Kind = PackIconMaterialKind.Cast},
                    Label = "Chromecast",
                    ToolTip = "Chromecast Settings.",
                    Tag =  ViewModelLocator.Instance.Chromecast
                },
                new HamburgerMenuIconItem()
                {
                    Icon = new PackIconMaterial() {Kind = PackIconMaterialKind.LightbulbOn},
                    Label = "Hue",
                    ToolTip = "Philips Hue.",
                    Tag =  ViewModelLocator.Instance.Hue
                },
                new HamburgerMenuIconItem()
                {
                    Icon = new PackIconMaterial() {Kind = PackIconMaterialKind.Music},
                    Label = "Music Colors",
                    ToolTip = "Music Color Setup",
                    Tag = ViewModelLocator.Instance.MusicColor
                }
            };

            MenuOptionItems = new HamburgerMenuItemCollection
            {
                new HamburgerMenuIconItem()
                {
                    Icon = new PackIconMaterial() {Kind = PackIconMaterialKind.Settings},
                    Label = "Settings",
                    ToolTip = "General Settings.",
                    Tag = ViewModelLocator.Instance.Settings
                }
            };
        }

        public HamburgerMenuItemCollection MenuItems
        {
            get { return _menuItems; }
            set
            {
                if (Equals(value, _menuItems)) return;
                _menuItems = value;
                RaisePropertyChanged();
            }
        }

        public HamburgerMenuItemCollection MenuOptionItems
        {
            get { return _menuOptionItems; }
            set
            {
                if (Equals(value, _menuOptionItems)) return;
                _menuOptionItems = value;
                RaisePropertyChanged();
            }
        }
    }
}
