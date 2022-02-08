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
    public class Packet : Element
    {
        private static readonly Random random = new Random();
        public Station Init { get; private set; }
        public Station From { get; private set; }
        public Station Dest { get; private set; }
        public Color BaseColor { get; private set; }
        public Link Link { get; private set; }

        public Packet(Field field, Station start, Link l)
            : base(field, start.X, start.Y)
        {
            this.Init = start;
            this.Dest = l.GetOtherEnd(start);
            this.From = start;
            this.Link = l;
            this.BaseColor = this.Field.GetRandomColor();
        }

        public Packet(Field field, double x, double y, Station init, Station from, Station dest, Link link, Color baseColor)
            : base(field, x, y)
        {
            this.Init = init;
            this.Dest = dest;
            this.From = from;
            this.Link = link;
            this.BaseColor = baseColor;
        }

        protected override FrameworkElement InitSprite()
        {
            var g = new GeometryGroup();
            g.Children.Add(new RectangleGeometry()
            {
                Rect = new Rect(-Field.RADIUS_PACKET, -Field.RADIUS_PACKET, 2 * Field.RADIUS_PACKET, 2 * Field.RADIUS_PACKET)
            });
            var p = new Path { Data = g, Fill = new SolidColorBrush(this.BaseColor) };
            return p;
        }

        public override void Action()
        {
            double d = Field.Dist(this.X, this.Y, Dest.X, Dest.Y);
            if (d <= Field.SPEED_PACKET)
            {
                if (this.Init != Dest)
                    this.Init = null;
                int i = 0;
                switch (Dest.Links.Count)
                {
                    case 1:
                        i = 0;
                        break;
                    case 2:
                        var link = Dest.GetLink(this.From);
                        i = 1 - Dest.Links.IndexOf(link);
                        break;
                    case 3:
                        link = Dest.GetLink(this.From);
                        var link2 = Dest.GetSwitchedLink(link);
                        i = Dest.Links.IndexOf(link2 ?? link);
                        break;
                }
                this.Set(Dest.X, Dest.Y);
                this.From = Dest;
                this.Link = Dest.Links[i];
                this.Dest = this.Link.GetOtherEnd(Dest);
            }
            else
            {
                this.Set(this.X + Field.SPEED_PACKET * (Dest.X - this.X) / d, this.Y + Field.SPEED_PACKET * (Dest.Y - this.Y) / d);
            }
            base.Action();

            var target = this.Field.Get<Station>(new Point(this.X, this.Y), true);
            if (target != null && target.BaseColor == this.BaseColor && target != this.Init)
                this.Field.ConsumePacket(this);

            if (this.Link != null)
            {
                var collision = this.Field.GetOtherPackets(new Point(this.X, this.Y), this);
                if (collision != null)
                    this.Field.ConsumeCollision(this.Link);
            }
        }
    }

    internal static class PacketExtensions
    {
        public static void NewStationPacket(this Field field, Canvas canvas, Station station, Link link)
        {
            var packet = new Packet(field, station, link);
            packet.Register(canvas);
            field.Add(packet);
            field.ConsumeNewPacket(packet);
        }

        public static void NewPacketInternal(this Field field, Canvas canvas, double x, double y, Station init, Station from, Station dest, Link link, Color baseColor)
        {
            var packet = new Packet(field, x, y, init, from, dest, link, baseColor);
            packet.Register(canvas);
            field.Add(packet);
        }
    }
}
