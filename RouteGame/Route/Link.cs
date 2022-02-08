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
    public class Link : Element
    {
        public Station S1 { get; private set; }
        public Station S2 { get; private set; }
        private Canvas ic;
        private Line l;
        private Line dl1;
        private Line dl2;

        public Link(Field field, Station s1, Station s2)
            : base(field, GetPosition(s1, s2).X, GetPosition(s1, s2).Y)
        {
            this.S1 = s1;
            this.S2 = s2;
        }

        protected override FrameworkElement InitSprite()
        {
            var c = new UserControl();
            this.ic = new Canvas();
            c.Content = this.ic;
            this.l = new Line() { Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) };
            this.dl1 = new Line() { Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)) };
            this.dl2 = new Line() { Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)) };
            this.ic.Children.Add(this.l);
            this.ic.Children.Add(this.dl1);
            this.ic.Children.Add(this.dl2);
            this.AdjustLine();
            return c;
        }

        public void Set()
        {
            base.Set(AdjustLine());
        }

        private Point AdjustLine()
        {
            Point p = GetPosition(this.S1, this.S2);
            this.l.X1 = S1.X - p.X;
            this.l.Y1 = S1.Y - p.Y;
            this.l.X2 = S2.X - p.X;
            this.l.Y2 = S2.Y - p.Y;
            double l = Field.Dist(this.S1.X, this.S1.Y, this.S2.X, this.S2.Y);
            double dl = Math.Min(l / 3.0, Field.RADIUS_SWITCH);

            this.dl1.X1 = this.l.X1;
            this.dl1.Y1 = this.l.Y1;
            this.dl1.X2 = this.l.X1 + (this.l.X2 - this.l.X1) * dl / l;
            this.dl1.Y2 = this.l.Y1 + (this.l.Y2 - this.l.Y1) * dl / l;

            this.dl2.X1 = this.l.X2;
            this.dl2.Y1 = this.l.Y2;
            this.dl2.X2 = this.l.X2 + (this.l.X1 - this.l.X2) * dl / l;
            this.dl2.Y2 = this.l.Y2 + (this.l.Y1 - this.l.Y2) * dl / l;

            return p;
        }

        private static Point GetPosition(Station s1, Station s2)
        {
            return new Point((s1.X + s2.X) * 0.5, (s1.Y + s2.Y) * 0.5);
        }

        public Station GetOtherEnd(Station s)
        {
            return this.S1 == s ? S2 : S1;
        }

        public bool Contains(Station s)
        {
            return this.S1 == s || this.S2 == s;
        }

        public void Grey(Station s, bool disabled)
        {
            this.Set();
            Line dl = this.S1 == s ? this.dl1 : this.dl2;
            dl.Stroke = disabled
                ? new SolidColorBrush(Color.FromArgb(0, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(255, 255, 140, 0));
            dl.StrokeThickness = disabled ? 1 : 3;
        }

        public bool RemoveAnimate(int position)
        {
            this.l.Stroke = new SolidColorBrush(Color.FromArgb((byte)(255 - 25 * position), 255, 0, 0));
            return position >= 10;
        }
    }

    internal static class LinkExtensions
    {
        public static void NewStationLink(this Field field, Canvas canvas, Station s1, Station s2)
        {
            if (s1.Links.Count >= 3 || s2.Links.Count >= 3) return;
            if (s1.HasLink(s2) || s2.HasLink(s1)) return;
            var link = new Link(field, s1, s2);
            if (field.ConsumeNewLink(link))
            {
                s1.AddLink(link);
                s2.AddLink(link);
                link.Register(canvas);
                field.Add(link);
                s1.RedrawDirection();
                s2.RedrawDirection();
            }
        }

        public static void NewStationLinkInternal(this Field field, Canvas canvas, Station s1, Station s2)
        {
            var link = new Link(field, s1, s2);
            s1.AddLink(link);
            s2.AddLink(link);
            link.Register(canvas);
            field.Add(link);
            s1.RedrawDirection();
            s2.RedrawDirection();
        }
    }
}
