using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NextGen__7
{
    public class RoundedPanel : Panel
    {
        private int cornerRadius = 15;
        private Color normalColor = Color.FromArgb(241, 246, 253);
        private Color hoverColor = Color.FromArgb(222, 235, 250);
        private Color borderColor = Color.FromArgb(155, 181, 211);
        private bool hovered;

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = Math.Max(2, value); UpdateRegion(); }
        }

        public Color NormalColor
        {
            get { return normalColor; }
            set { normalColor = value; BackColor = hovered ? hoverColor : normalColor; Invalidate(); }
        }

        public Color HoverColor
        {
            get { return hoverColor; }
            set { hoverColor = value; if (hovered) BackColor = hoverColor; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; Invalidate(); }
        }

        public event EventHandler Clicked;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = normalColor;
            Cursor = Cursors.Hand;
        }

        public void Activate()
        {
            if (Enabled && Visible)
            {
                EventHandler callback = Clicked;
                if (callback != null) callback(this, EventArgs.Empty);
            }
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
            using (GraphicsPath path = RoundedPath())
            using (Pen pen = new Pen(borderColor, 2f))
                e.Graphics.DrawPath(pen, path);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovered = true;
            BackColor = hoverColor;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Point location = PointToClient(Cursor.Position);
            if (!ClientRectangle.Contains(location))
            {
                hovered = false;
                BackColor = normalColor;
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Activate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                Activate();
                e.Handled = true;
            }
        }

        private GraphicsPath RoundedPath()
        {
            GraphicsPath path = new GraphicsPath();
            if (Width < 3 || Height < 3) return path;
            int r = Math.Max(2, Math.Min(cornerRadius, Math.Min(Width, Height) / 2));
            int d = r * 2;
            int w = Width - 1;
            int h = Height - 1;
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(w - d, 0, d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0, 90);
            path.AddArc(0, h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void UpdateRegion()
        {
            if (Width < 3 || Height < 3) return;
            using (GraphicsPath path = RoundedPath())
            {
                Region previous = Region;
                Region = new Region(path);
                if (previous != null) previous.Dispose();
            }
            Invalidate();
        }
    }
}
