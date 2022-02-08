using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Route
{
    public class Field
    {
        private readonly Action<string> playSound;
        private readonly Action gameOver;
        public const int COST_BOX_ARRIVED = 2;
        public const int COST_USER_STATION = 6;
        public const int COST_USER_LINK = 2;
        public const int MONEY_INITIAL = 24;
        public const int COST_DAMAGE_COEF = 2;

        public static double MaxSize
        {
            get { return Math.Max(Window.Current.Bounds.Width, Window.Current.Bounds.Height); }
        }

        public static double MinSize
        {
            get { return Math.Min(Window.Current.Bounds.Width, Window.Current.Bounds.Height); }
        }

        public static double RADIUS_STATION;
        public static double RADIUS_PACKET;
        public static double RADIUS_SWITCH;

        private static double RADIUS_SEARCH_CLICK;
        private static double RADIUS_SEARCH_ARRIVE;
        private static double RADIUS_SEARCH_COLLIDE;

        public static double SPEED_PACKET;


        public void InitSizes(bool isMouse)
        {
            RADIUS_STATION = isMouse ? 8 : 8 * MinSize / 400.0;
            RADIUS_PACKET = isMouse ? 4 : 4 * MinSize / 400.0;
            RADIUS_SWITCH = isMouse ? 24 : 24 * MinSize / 400.0;

            RADIUS_SEARCH_CLICK = isMouse ? 24 : 24 * MinSize / 400.0;
            RADIUS_SEARCH_ARRIVE = isMouse ? 8 : 8 * MinSize / 400.0;
            RADIUS_SEARCH_COLLIDE = isMouse ? 2 : 2 * MinSize / 400.0;

            SPEED_PACKET = isMouse ? 0.5 : 0.5 * MinSize / 400.0;

            this.ResetCanvas(false);
            foreach (var element in this.elements)
            {
                element.Register(this.canvas);
            }
        }

        public void ResetCanvas(bool withObjects)
        {
            if (this.canvas.Children.Count > 3)
            {
                for (int i = this.canvas.Children.Count-1; i >= 3; i--)
                    this.canvas.Children.RemoveAt(i);
            }
            else
            {
                this.canvas.Children.Clear();
                Rectangle background = new Rectangle()
                {
                    Height = Field.MaxSize,
                    Width = Field.MaxSize,
                    Fill = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128))
                };
                this.canvas.Children.Add(background);
                Canvas.SetZIndex(background, -100);
                this.canvas.Children.Add(this.ScoreBox);
                this.canvas.Children.Add(this.MoneyBox);
                this.canvas.ManipulationMode = ManipulationModes.TranslateInertia;
            }

            if (withObjects)
            {
                var s1 = this.NewRandomStation(this.canvas);
                var ud = this.NewRandomStation(this.canvas, s1);
                this.NewStationLink(this.canvas, s1, ud);
                var s2 = this.NewRandomStation(this.canvas, adjointStation: s1);
            }
        }

        public const int ScoreHeightTop = 72;
        public const int ScoreHeightBottom = 72;

        private readonly List<Element> elements = new List<Element>();
        
        private static readonly List<Color> colors = new List<Color>()
        {
            Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 0, 255, 0), 
            Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 0, 255, 255), Color.FromArgb(255, 255, 0, 255), 
            Color.FromArgb(255, 160, 82, 45), Color.FromArgb(255, 127, 255, 0), Color.FromArgb(255, 0, 0, 0)
        };
        private static readonly Random random = new Random();
        private List<Tuple<Link, int>> removeAnimation = new List<Tuple<Link, int>>();

        public int ColorIndex { get; private set; }
        public int Round { get; private set; }
        private Canvas canvas;
        private readonly TextBlock ScoreBox;
        private readonly TextBlock MoneyBox;
        private int score;
        private int money;

        public int HiScore { get; set; }

        public int Score
        {
            get { return score; }
            private set
            {
                score = value;
                this.ScoreBox.Text = string.Format("Score: {0}", score.ToString());
            }
        }

        public int Money
        {
            get { return money; }
            private set
            {
                money = value;
                this.MoneyBox.Text = string.Format("Points: {0}", money.ToString());
            }
        }

        public Field(Canvas canvas, Action<string> playSound, Action gameOver)
        {
            this.canvas = canvas;
            this.playSound = playSound;
            this.gameOver = gameOver;

            this.ScoreBox = new TextBlock()
            {
                FontFamily = new FontFamily("Courier New"),
                FontSize = MinSize / 16,
                IsColorFontEnabled = true,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0))
            };
            
            this.MoneyBox = new TextBlock()
            {
                FontFamily = new FontFamily("Courier New"),
                FontSize = MinSize / 16,
                IsColorFontEnabled = true,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 150, 0)),
                Padding = new Thickness() { Left = MinSize * 0.5 },
                TextAlignment = TextAlignment.Right
            };

            this.Score = 0;
            this.Money = MONEY_INITIAL;
           
        }

        public void Add(Element e)
        {
            this.elements.Add(e);
        }

        public IEnumerable<T> Get<T>() where T : Element
        {
            return this.elements.OfType<T>();
        }

        public void Action()
        {
            this.elements.ToList().ForEach(e => e.Action());
            if (this.Round/1000 == (int) Math.Pow(2, this.ColorIndex))
                this.NewRandomStation(this.canvas);
            if (this.Round % 2000 == 0)
            {
                this.CheckEndGame();
                this.ConsumeTick();
            }
            this.Round++;
            foreach (var tuple in this.removeAnimation.ToList())
            {
                this.removeAnimation.Remove(tuple);
                if (tuple.Item1.RemoveAnimate(tuple.Item2))
                    tuple.Item1.Unregister();
                else
                    this.removeAnimation.Add(new Tuple<Link, int>(tuple.Item1, tuple.Item2 + 1));
            }
        }

        public T Get<T>(Point p, bool exact = false) where T : Element
        {
            return this.elements.OfType<T>().FirstOrDefault(e => Math.Abs(p.X - e.X) + Math.Abs(p.Y - e.Y) <=
                (exact ? RADIUS_SEARCH_ARRIVE : RADIUS_SEARCH_CLICK));
        }

        public Packet GetOtherPackets(Point p, Packet el)
        {
            return this.elements.OfType<Packet>().Where(l => l != el && l.Link == el.Link)
                .FirstOrDefault(e => Math.Abs(p.X - e.X) + Math.Abs(p.Y - e.Y) <= RADIUS_SEARCH_COLLIDE);
        }

        public int GetPacketsCount(Link l = null)
        {
            return this.elements.OfType<Packet>().Count(p => l == null || p.Link == l);
        }

        public int GetStationCount(bool includeUserdef = false)
        {
            return this.elements.OfType<Station>().Count(s => includeUserdef || !s.Userdef);
        }

        public Color GetNextColor()
        {
            return this.GetColor(this.ColorIndex++);
        }

        public Color GetRandomColor()
        {
            return this.GetColor(random.Next(this.ColorIndex));
        }

        private Color GetColor(int index)
        {
            return index < colors.Count ? colors[index] : Color.FromArgb(255, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
        }

        public void ConsumePacket(Packet packet)
        {
            this.RemovePacket(packet);
            this.Score++;
            this.Money += 2;
            this.playSound("success");
        }

        private void RemovePacket(Packet p)
        {
            p.Unregister();
            this.elements.Remove(p);
        }

        public bool ConsumeNewStation(Station s)
        {
            if (this.Money < COST_USER_STATION) return false;
            this.Money -= COST_USER_STATION;
            this.playSound("build");
            return true;
        }

        public bool ConsumeNewLink(Link l)
        {
            if (this.Money < COST_USER_LINK) return false;
            this.Money -= COST_USER_LINK;
            this.playSound("build");
            return true;
        }

        public void ConsumeCollision(Link l)
        {
            this.playSound("error");
            this.Money -= COST_USER_LINK * COST_DAMAGE_COEF;
            var packets = this.elements.OfType<Packet>().Where(p => p.Link == l).ToList();
            this.Money -= COST_BOX_ARRIVED * COST_DAMAGE_COEF * packets.Count;
            packets.ForEach(this.RemovePacket);
            l.S1.Links.Remove(l);
            l.S1.HideSwitch();
            l.S2.Links.Remove(l);
            l.S2.HideSwitch();
            this.elements.Remove(l);
            this.removeAnimation.Add(new Tuple<Link, int>(l, 0));
            if (this.Money < 0)
                if (!this.CheckSolution())
                    this.gameOver();
        }

        public void ConsumeTick()
        {
            this.Money -= COST_USER_LINK / 2;
            this.playSound("cash");
        }

        public void ConsumeNewPacket(Packet p)
        {
            this.playSound("new");
        }

        public static double Dist(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public void CheckEndGame()
        {
            if (this.Money < 0)
                this.gameOver();
        }

        public void InitParams(int round, int colorIndex, int money, int score)
        {
            this.Round = round;
            this.ColorIndex = colorIndex;
            this.Money = money;
            this.Score = score;
        }

        private bool CheckSolution()
        {
            var stations = this.elements.OfType<Station>().Where(s => !s.Userdef).ToList();
            var links = this.elements.OfType<Link>().ToList();
            bool result = false;
            for (int i = 0; i < stations.Count(); i++)
                for (int j = i+1; j < stations.Count(); j++)
                {
                    if (result) break;
                    result = result || this.CheckLinkBetween(stations[i], stations[j], links);
                }
            return result;
        }

        private bool CheckLinkBetween(Station s1, Station s2, List<Link> allLinks)
        {
            List<Station> marked = new List<Station>();
            marked.Add(s1);
            bool wasAdded = true;
            while (wasAdded)
            {
                wasAdded = false;
                foreach (Station station in marked.ToList())
                {
                    foreach (Link link in station.Links)
                    {
                        var other = link.GetOtherEnd(station);
                        if (!marked.Contains(other))
                        {
                            marked.Add(other);
                            wasAdded = true;
                        }
                    }
                }
            }
            return marked.Contains(s2);
            
        }
    }
}
