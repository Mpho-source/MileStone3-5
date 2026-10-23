
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NextGen__7
{
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 15;

        public Color NormalColor { get; set; } = Color.White;

        public Color HoverColor { get; set; } =
            Color.FromArgb(230, 237, 249);

        public Color BorderColor { get; set; } =
            Color.FromArgb(215, 222, 232);

        public event EventHandler Clicked;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            BackColor = NormalColor;
            Cursor = Cursors.Hand;
            ResizeRedraw = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GetRoundedPath())
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            BackColor = HoverColor;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            BackColor = NormalColor;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Clicked?.Invoke(this, e);
        }

        private GraphicsPath GetRoundedPath()
        {
            GraphicsPath path = new GraphicsPath();

            if (Width <= 1 || Height <= 1)
                return path;

            int radius = Math.Max(
                1,
                Math.Min(
                    CornerRadius,
                    Math.Min(Width, Height) / 2));

            int diameter = radius * 2;
            int w = Width - 1;
            int h = Height - 1;

            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddArc(w - diameter, 0, diameter, diameter, 270, 90);
            path.AddArc(
                w - diameter, h - diameter,
                diameter, diameter, 0, 90);
            path.AddArc(0, h - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();

            return path;
        }

        private void UpdateRegion()
        {
            if (Width <= 1 || Height <= 1)
                return;

            using (GraphicsPath path = GetRoundedPath())
            {
                Region oldRegion = Region;
                Region = new Region(path);
                oldRegion?.Dispose();
            }

            Invalidate();
        }
    }
}
