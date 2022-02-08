using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Route
{
    public class Station : Element
    {

        private static readonly Random random = new Random();
        private Canvas ic;
        public bool Userdef { get; private set; }
        public int Direction { get; private set; }
        public Color BaseColor { get; private set; }
        public IList<Link> Links { get; private set; }

        public Station(Field field, double x, double y, bool userdef, int direction = 0, Color? baseColor = null)
            : base(field, x, y)
        {
            this.Links = new List<Link>();
            this.Direction = direction;
            this.Userdef = userdef;
            if (baseColor.HasValue)
                this.BaseColor = baseColor.Value;
            else
                if (!userdef)
                    this.BaseColor = field.GetNextColor();
        }

        protected override FrameworkElement InitSprite()
        {
            var c = new UserControl();
            this.ic = new Canvas();
            c.Content = this.ic;

            var g = new GeometryGroup();
            g.Children.Add(new EllipseGeometry() { Center = new Point(0, 0), RadiusX = Field.RADIUS_STATION, RadiusY = Field.RADIUS_STATION });
            var p = new Path
            {
                Data = g,
                Fill = new SolidColorBrush(this.Userdef ? Color.FromArgb(255, 255, 255, 255) : this.BaseColor)
            };
            this.ic.Children.Add(p);

            return c;
        }

        public override void Action()
        {
            if (this.Userdef)
                return;

            if (random.Next((int)Math.Pow(2, 6 + 2 * this.Field.GetPacketsCount() - this.Field.GetStationCount())) == 1)
            {
                var freeLinks = this.Links.Where(l => this.Field.GetPacketsCount(l) == 0).ToList();
                if (freeLinks.Any())
                {
                    var link = freeLinks[random.Next(freeLinks.Count())];
                    this.Field.NewStationPacket(this.canvas, this, link);
                }
            }
        }

        public void AddLink(Link link)
        {
            this.Links.Add(link);
        }

        public override void Activate(bool activated)
        {
            if (activated)
            {
                if (!this.activated)
                {
                    var g = new GeometryGroup();
                    g.Children.Add(new EllipseGeometry() { Center = new Point(0, 0), RadiusX = Field.RADIUS_STATION, RadiusY = Field.RADIUS_STATION });
                    var p = new Path { Data = g, Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)) };
                    this.ic.Children.Add(p);
                }
            }
            else
            {
                for (int i = this.ic.Children.Count - 1; i > 0; i--)
                    this.ic.Children.RemoveAt(i);
            }
            base.Activate(activated);
        }

        public override void Set(Point p)
        {
            base.Set(p);
            this.Links.ForEach(l => l.Set());
        }

        public bool CanSwitch
        {
            get { return this.Links.Count == 3; }
        }

        public void Switch()
        {
            if (this.CanSwitch)
            {
                Direction = (Direction + 1) % 3;
                RedrawDirection();
            }
        }

        public void HideSwitch()
        {
            this.Links.ForEach(l => l.Grey(this, true));
        }

        public void RedrawDirection()
        {
            if (this.CanSwitch)
                this.Links.ForEach(l => l.Grey(this, this.Links.IndexOf(l) == this.Direction));
        }

        public bool HasLink(Station d)
        {
            return this.Links.Any(l => l.Contains(d));
        }

        public Link GetLink(Station s)
        {
            return this.Links.First(l => l.Contains(s));
        }

        public Link GetSwitchedLink(Link l)
        {
            int i = this.Links.IndexOf(l);
            return Direction == i ? null : this.Links.First(a => a != l && a != this.Links[this.Direction]);
        }
    }

    internal static class StationExtensions
    {
        private static readonly Random random = new Random();

        private static double RandomInRange(double from, double to)
        {
            return from + random.NextDouble() * (to - from);
        }

        public static Station NewRandomStation(this Field field, Canvas canvas, Station baseStation = null, Station adjointStation = null)
        {
            double x;
            double y;
            double canvasHeight = Field.MinSize;
            double canvasWidth = Field.MaxSize;
            double shift = 12 * Field.RADIUS_STATION;
            if (baseStation == null)
            {
                double maxXSize = canvasWidth - shift;
                double maxYSize = Math.Min(canvasHeight - shift, canvasHeight - Field.ScoreHeightBottom);
                double minYSize = Math.Max(Field.ScoreHeightTop, shift);
                x = RandomInRange(shift, maxXSize);
                if (adjointStation == null)
                    y = RandomInRange(minYSize, maxYSize);
                else
                    if (adjointStation.Y > canvasHeight * 0.5)
                        y = RandomInRange(minYSize, canvasHeight * 0.5);
                    else
                        y = RandomInRange(canvasHeight * 0.5, maxYSize);
            }
            else
            {
                if (baseStation.Y > canvasHeight * 0.5)
                    y = baseStation.Y - shift;
                else
                    y = baseStation.Y + shift;
                x = baseStation.X;
            }
            Station s = new Station(field, x, y,
                    baseStation != null);
            field.Add(s);
            s.Register(canvas);
            return s;
        }

        public static void NewUserStation(this Field field, Canvas canvas, Point p)
        {
            Station s = new Station(field, p.X, p.Y, true);
            if (field.ConsumeNewStation(s))
            {
                field.Add(s);
                s.Register(canvas);
            }
        }

        public static void NewStationInternal(this Field field, Canvas canvas, double x, double y, int direction, bool userdef, Color baseColor)
        {
            Station s = new Station(field, x, y, userdef, direction, baseColor);
            field.Add(s);
            s.Register(canvas);
        }
    }
}