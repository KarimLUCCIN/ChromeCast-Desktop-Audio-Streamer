/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:MiniCast.Client"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using MiniCast.Client.Spectrum.Models;
using MiniCast.Client.ViewModel.Chromecast;
using MiniCast.Client.ViewModel.Hue;

namespace MiniCast.Client.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        public static ViewModelLocator Instance { get; private set; }
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            Instance = this;

            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<AudioLoopbackViewModel>();
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<ChromecastViewModel>();
            SimpleIoc.Default.Register<MusicColorViewModel>();
            SimpleIoc.Default.Register<HueViewModel>();
            SimpleIoc.Default.Register<SettingsViewModel>();

            SimpleIoc.Default.Register<LiveColorEvaluatorModel>();
        }

        public AudioLoopbackViewModel LoopbackRecorder => ServiceLocator.Current.GetInstance<AudioLoopbackViewModel>();

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();
        public Chromecast.ChromecastViewModel Chromecast => ServiceLocator.Current.GetInstance<ChromecastViewModel>();

        public MusicColorViewModel MusicColor => ServiceLocator.Current.GetInstance<MusicColorViewModel>();
        public LiveColorEvaluatorModel LiveColor => ServiceLocator.Current.GetInstance<LiveColorEvaluatorModel>();

        public HueViewModel Hue => ServiceLocator.Current.GetInstance<HueViewModel>();
        public SettingsViewModel Settings => ServiceLocator.Current.GetInstance<SettingsViewModel>();

        public static void Cleanup()
        {
            Instance.Main.Cleanup();
            Instance.Hue.Cleanup();
            Instance.Chromecast.Cleanup();
            Instance.LoopbackRecorder.Cleanup();
        }
    }
}