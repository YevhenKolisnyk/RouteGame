using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Route
{
    public class Game
    {
        public Field Field { get; private set; }
        private readonly Canvas canvas;
        private Station selected_move;
        private Station selected_connect;
        private readonly Dictionary<string, MediaElement> sounds;
        private DispatcherTimer timer;
        private bool? mouse;
        private bool mute;
        private Action refreshUI;

        public bool Mute
        {
            get
            {
                return mute;
            }
            set
            {
                mute = value;
                this.SaveGameData();
            }
        }

        public bool Mouse
        {
            get
            {
                if (mouse == null)
                    mouse = MouseCount > 0;
                return mouse.Value;
            }
            set
            {
                mouse = value;
                this.Stop();
                this.Field.InitSizes(value);
                this.Start();
                this.SaveGameData();
            }
        }

        public static int MouseCount
        {
            get
            {
                var cap = new MouseCapabilities();
                return cap.MousePresent;
            }
        }

        public Game(Canvas canvas, Dictionary<string, MediaElement> sounds, Action refreshUI, bool? mouse = null)
        {
            Window.Current.SizeChanged += Current_SizeChanged;
            this.canvas = canvas;
            this.sounds = sounds;
            this.mouse = mouse;
            this.refreshUI = refreshUI;
            
            
            this.Init(true, true, true);
        }

        public void Init(bool needLoadData, bool withObjects, bool start = false)
        {
            this.Field = new Field(this.canvas, this.PlaySound, this.GameOver);
            this.timer = new DispatcherTimer { Interval = new TimeSpan(100) };
            this.timer.Tick += (sender, o) => this.Field.Action();
            if (needLoadData)
            {
                this.LoadGameData().ContinueWith(
                    t =>
                    {
                        this.canvas.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => this.ReInit(withObjects, start));
                    });
            }
            else
                this.ReInit(withObjects);
        }

        private void ReInit(bool withObjects, bool start = false)
        {
            this.Field.InitSizes(this.Mouse);
            this.Field.ResetCanvas(withObjects);
            if (this.refreshUI != null)
                this.refreshUI();
            if (start)
                this.Start();
        }

        public void PlaySound(string name)
        {
            try
            {
                if (!Mute && this.sounds.ContainsKey(name))
                    sounds[name].Play();
            }
            catch (Exception)
            {
            }
        }

        void canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //Click(e.GetPosition(this.canvas));
        }

        void canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Click(e.GetCurrentPoint(this.canvas).Position);
        }

        private void Click(Point p)
        {
            var target = this.Field.Get<Station>(p);
            if (target != null && target.CanSwitch)
            {
                this.PlaySound("switch");
                target.Switch();
                return;
            }
            if (this.selected_connect == null)
            {
                if (target != null)
                {
                    this.selected_connect = target;
                    this.selected_connect.Activate(true);
                    this.PlaySound("switch");
                }
                else
                    this.Field.NewUserStation(this.canvas, p);
            }
            else
            {
                if (target != null && target != this.selected_connect)
                    this.Field.NewStationLink(this.canvas, this.selected_connect, target);
                this.selected_connect.Activate(false);
                this.selected_connect = null;
            }
        }

        void canvas_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (this.selected_move != null)
            {
                this.selected_move.Set(e.Position);
                this.selected_move.Activate(false);
                this.PlaySound("build");
            }
            this.selected_move = null;
            this.selected_connect = null;
        }

        void canvas_ManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
        {
            var station = this.Field.Get<Station>(e.Position);
            if (station.Links.Count == 0)
            {
                this.selected_move = station;
                if (this.selected_move != null)
                    this.selected_move.Activate(true);
            }
        }

        public void Start()
        {
            this.timer.Start();
            this.canvas.ManipulationStarted += canvas_ManipulationStarted;
            this.canvas.ManipulationCompleted += canvas_ManipulationCompleted;
            this.canvas.PointerPressed += canvas_PointerPressed;
            this.canvas.Tapped += canvas_Tapped;
        }

        public void Stop()
        {
            this.timer.Stop();
            this.canvas.ManipulationStarted -= canvas_ManipulationStarted;
            this.canvas.ManipulationCompleted -= canvas_ManipulationCompleted;
            this.canvas.PointerPressed -= canvas_PointerPressed;
            this.canvas.Tapped -= canvas_Tapped;
        }

        public async Task<bool> SaveGameData()
        {
            var data = new GameData();
            Field.HiScore = Math.Max(Field.HiScore, Field.Score);
            data.HiScore = this.Field.HiScore;
            data.IsMute = this.Mute;
            data.IsMouse = this.Mouse;
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("RouteData", CreationCollisionOption.OpenIfExists);
            StorageFile file = await folder.CreateFileAsync("RouteData_Default", CreationCollisionOption.ReplaceExisting);
            bool result = true;
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            using (IOutputStream outStream = stream.GetOutputStreamAt(0))
            {
                var serializer = new DataContractSerializer(typeof(GameData));
                serializer.WriteObject(outStream.AsStreamForWrite(), data);
                result = await outStream.FlushAsync();
            }
            return result;
        }

        public async Task<bool> LoadGameData()
        {
            try
            {
                GameData data = null;
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("RouteData");
                StorageFile file = await folder.GetFileAsync("RouteData_Default");
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    var serializer = new DataContractSerializer(typeof(GameData));
                    data = (GameData)serializer.ReadObject(inStream.AsStreamForRead());
                }
                if (data == null) return true;
                this.Field.HiScore = data.HiScore;
                this.mute = data.IsMute;
                this.mouse = data.IsMouse;
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public async Task<bool> Save()
        {
            this.Stop();
            var data = new SaveData();

            data.Round = this.Field.Round;
            data.ColorIndex = this.Field.ColorIndex;
            data.Money = this.Field.Money;
            data.Score = this.Field.Score;

            data.Stations_X = new List<double>();
            data.Stations_Y = new List<double>();
            data.Stations_Direction = new List<int>();
            data.Stations_Color = new List<Color>();
            data.Stations_Userdef = new List<bool>();
            var stations = this.Field.Get<Station>().ToList();
            foreach (var station in stations)
            {
                data.Stations_X.Add(station.X);
                data.Stations_Y.Add(station.Y);
                data.Stations_Direction.Add(station.Direction);
                data.Stations_Color.Add(station.BaseColor);
                data.Stations_Userdef.Add(station.Userdef);
            }
            data.Links_S1 = new List<int>();
            data.Links_S2 = new List<int>();
            var links = this.Field.Get<Link>().ToList();
            foreach (var link in links)
            {
                int i1 = stations.IndexOf(link.S1);
                int i2 = stations.IndexOf(link.S2);
                data.Links_S1.Add(i1);
                data.Links_S2.Add(i2);
            }

            data.Packets_X = new List<double>();
            data.Packets_Y = new List<double>();
            data.Packets_Color = new List<Color>();
            data.Packets_Init = new List<int>();
            data.Packets_From = new List<int>();
            data.Packets_Dest = new List<int>();
            data.Packets_Link = new List<int>();
            var packets = this.Field.Get<Packet>();
            foreach (var packet in packets)
            {
                int i_init = stations.IndexOf(packet.Init);
                int i_from = stations.IndexOf(packet.From);
                int i_dest = stations.IndexOf(packet.Dest);
                int i_link = links.IndexOf(packet.Link);

                data.Packets_X.Add(packet.X);
                data.Packets_Y.Add(packet.Y);
                data.Packets_Color.Add(packet.BaseColor);

                data.Packets_Init.Add(i_init);
                data.Packets_From.Add(i_from);
                data.Packets_Dest.Add(i_dest);
                data.Packets_Link.Add(i_link);
            }


            this.Start();

            bool result = true;
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("RouteSave", CreationCollisionOption.OpenIfExists);
            StorageFile file = await folder.CreateFileAsync("RouteSave_Default", CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            using (IOutputStream outStream = stream.GetOutputStreamAt(0))
            {
                var serializer = new DataContractSerializer(typeof(SaveData));
                serializer.WriteObject(outStream.AsStreamForWrite(), data);
                result = await outStream.FlushAsync();
            }
            return result;
        }

        public async void Load()
        {
            try
            {
                SaveData data = null;
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("RouteSave");
                StorageFile file = await folder.GetFileAsync("RouteSave_Default");
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    var serializer = new DataContractSerializer(typeof (SaveData));
                    data = (SaveData) serializer.ReadObject(inStream.AsStreamForRead());
                }
                if (data == null) return;
                this.Stop();
                
                this.Init(false, false);
                this.Field.InitParams(data.Round, data.ColorIndex, data.Money, data.Score);
                for (int i = 0; i < data.Stations_X.Count; i++)
                    this.Field.NewStationInternal(this.canvas, data.Stations_X[i], data.Stations_Y[i],
                        data.Stations_Direction[i], data.Stations_Userdef[i], data.Stations_Color[i]);
                var stations = this.Field.Get<Station>().ToList();

                for (int i = 0; i < data.Links_S1.Count; i++)
                    this.Field.NewStationLinkInternal(this.canvas, stations[data.Links_S1[i]],
                        stations[data.Links_S2[i]]);
                var links = this.Field.Get<Link>().ToList();

                for (int i = 0; i < data.Packets_X.Count; i++)
                    this.Field.NewPacketInternal(this.canvas, data.Packets_X[i], data.Packets_Y[i],
                        data.Packets_Init[i] > 0 ? stations[data.Packets_Init[i]] : null, 
                        stations[data.Packets_From[i]], stations[data.Packets_Dest[i]],
                        links[data.Packets_Link[i]], data.Packets_Color[i]);
                this.LoadGameData().ContinueWith(
                    t =>
                    {
                        this.canvas.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => this.Start());
                    });
            }
            catch (Exception)
            {
                MessageDialog dialog = new MessageDialog(
                "Nothing to load or load error!" , "Load error.");
                dialog.Commands.Add(new UICommand("OK"));
                dialog.ShowAsync().Completed = (info, status) => this.canvas.Dispatcher.RunAsync(CoreDispatcherPriority.High, this.Restart);
            }
        }

        public void GameOver()
        {
            this.PlaySound("end");
            this.Stop();
            MessageDialog dialog = new MessageDialog(
                this.Field.Score > this.Field.HiScore ?
                string.Format("Game over.\nYour result is {0}. \n Congratulations, it is new record!", this.Field.Score) :
                string.Format("Game over.\nYour result is {0}. \n Top result is {1}.", this.Field.Score, this.Field.HiScore)
                , "Game over.");
            dialog.Commands.Add(new UICommand("OK"));
            dialog.ShowAsync().Completed = (info, status) => this.canvas.Dispatcher.RunAsync(CoreDispatcherPriority.High, this.Restart);
        }

        public void Restart()
        {
            this.Stop();
            this.SaveGameData().ContinueWith(
                t =>
                {
                    this.canvas.Dispatcher.RunAsync(CoreDispatcherPriority.High, this.ReContinue);
                });
        }

        public void ReContinue()
        {
            this.Init(true, true);
            this.Start();
        }

        void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            
        }

        public void Help()
        {
            this.Stop();
            MessageDialog dialog = new MessageDialog(
                string.Format("Route Game. \n " +
                              "The goal of the game is to build connection network between Nodes (color circles), \n" +
                              " to let packets (color squares) reach the corresponding node. \n " +
                            "\n While the node is not connected it could be moved to another position with drag-n-drop. \n" +
                            "\n Each node could have maximum three links to other nodes.\n " +
                              " If there are three links the node is equipped with switch.\n" +
                            "\n When two packets collide thay are destroyed together with the link. \n" +
                            "\nTo build you need points:\n" +
                              " Node cost {0} points;\n" +
                              " Link cost {1} points;\n" +
                              " Delivered package gives you {2} points;\n" +
                              " Fee for destroying is {3} plus {4} for each package. \n" +
                            "\n During the game new base (colored) nodes appear.\n" +
                            "\n Have fun!", 
                            Field.COST_USER_STATION.ToString(),
                            Field.COST_USER_LINK.ToString(),
                            Field.COST_BOX_ARRIVED.ToString(),
                            (Field.COST_USER_LINK * Field.COST_DAMAGE_COEF).ToString(),
                            (Field.COST_BOX_ARRIVED * Field.COST_DAMAGE_COEF).ToString()
                                                     ), "Route game paused.");
            dialog.Commands.Add(new UICommand("OK"));
            dialog.ShowAsync().Completed = (info, status) => this.canvas.Dispatcher.RunAsync(CoreDispatcherPriority.High, this.Start);
        }

    }
}
