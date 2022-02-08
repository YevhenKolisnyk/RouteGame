using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using Route;

namespace RouteGame
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Game game;
        private Dictionary<string, MediaElement> sounds = new Dictionary<string, MediaElement>();

        public MainPage()
        {
            this.InitializeComponent();
            sounds.Add("new", this.SoundNew);
            sounds.Add("success", this.SoundSuccess);
            sounds.Add("switch", this.SoundSwitch);
            sounds.Add("build", this.SoundBuild);
            sounds.Add("error", this.SoundError);
            sounds.Add("end", this.SoundEnd);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.game = new Game(this.Canvas, sounds, this.RefreshUI);
        }

        private void RefreshUI()
        {
            this.Mute.Opacity = this.game.Mute ? 0.2 : 1.0;
            this.Mouse.Opacity = !this.game.Mouse ? 0.2 : 1.0;
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.game != null)
                this.game.Save();
        }

        private void Load_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.game != null)
                this.game.Load();
        }

        private void Restart_OnClick(object sender, RoutedEventArgs e)
        {
            this.game.Restart();
        }

        private void Mute_OnClick(object sender, RoutedEventArgs e)
        {
            this.game.Mute = !this.game.Mute;
            this.Mute.Opacity = this.game.Mute ? 0.2 : 1.0;
            if (!this.game.Mute)
                this.SoundNew.Play();
        }

        private void Mouse_OnClick(object sender, RoutedEventArgs e)
        {
            this.game.Mouse = !this.game.Mouse;
            this.Mouse.Opacity = !this.game.Mouse ? 0.2 : 1.0;
            if (!this.game.Mute)
                this.game.PlaySound("new");
        }

        private void Help_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.game != null)
                this.game.Help();
        }
    }
}
