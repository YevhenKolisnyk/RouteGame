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
    public abstract class Element
    {
        public Field Field { get; private set; }
        protected Canvas canvas;
        protected bool activated;
        public double X { get; private set; }
        public double Y { get; private set; }

        private FrameworkElement sprite;

        public Element()
        {
        }

        protected Element(Field field, double x, double y)
        {
            this.X = x;
            this.Y = y;
            this.Field = field;
        }

        public void Register(Canvas canvas)
        {
            this.canvas = canvas;
            this.sprite = this.InitSprite();
            canvas.Children.Add(this.sprite);
            this.Set();
        }

        public void Unregister()
        {
            this.canvas.Children.Remove(this.sprite);
        }

        protected abstract FrameworkElement InitSprite();

        public virtual void Action()
        {
            this.Set();
        }

        public virtual void Activate(bool activated)
        {
            this.activated = activated;
        }

        private void Set()
        {
            Canvas.SetLeft(this.sprite, this.X - this.sprite.ActualWidth * 0.5);
            Canvas.SetTop(this.sprite, this.Y - this.sprite.ActualHeight * 0.5);
        }

        public virtual void Set(Point p)
        {
            this.Set(p.X, p.Y);
        }

        public virtual void Set(double x, double y)
        {
            this.X = x;
            this.Y = y;
            this.Set();
        }
    }
}
