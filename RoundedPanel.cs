using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;

namespace NextGen__7
{
    public class RoundedPanel : Panel
    {
        private int cornerRadius = 15;
        private Color normalColor = Color.FromArgb(241, 246, 253);
        private Color hoverColor = Color.FromArgb(222, 235, 250);
        private Color borderColor = Color.FromArgb(113, 145, 187);
        private bool hovered;
        private bool actionInProgress;
        private long lastCompletion;

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
            if (!Enabled || !Visible || actionInProgress) return;
            long now = Stopwatch.GetTimestamp();
            if (lastCompletion != 0 &&
                now - lastCompletion < Stopwatch.Frequency * 350L / 1000L)
                return;

            EventHandler callback = Clicked;
            if (callback == null) return;

            actionInProgress = true;
            IDisposable loading = null;
            try
            {
                loading = FormLoadingIndicator.Show(FindForm());
                callback(this, EventArgs.Empty);
            }
            finally
            {
                if (loading != null) loading.Dispose();
                lastCompletion = Stopwatch.GetTimestamp();
                actionInProgress = false;
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
            RefreshHoverState();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            RefreshHoverState();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            RefreshHoverState();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible)
                SetHovered(false);
            else if (IsHandleCreated)
                RefreshHoverState();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            RefreshHoverState();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            TrackChild(e.Control);
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            UntrackChild(e.Control);
            base.OnControlRemoved(e);
            RefreshHoverState();
        }

        private void TrackChild(Control child)
        {
            child.MouseEnter += ChildMouseEnterOrLeave;
            child.MouseLeave += ChildMouseEnterOrLeave;
            child.MouseMove += ChildMouseMove;
            child.ControlAdded += ChildControlAdded;
            child.ControlRemoved += ChildControlRemoved;

            foreach (Control descendant in child.Controls)
                TrackChild(descendant);
        }

        private void UntrackChild(Control child)
        {
            child.MouseEnter -= ChildMouseEnterOrLeave;
            child.MouseLeave -= ChildMouseEnterOrLeave;
            child.MouseMove -= ChildMouseMove;
            child.ControlAdded -= ChildControlAdded;
            child.ControlRemoved -= ChildControlRemoved;

            foreach (Control descendant in child.Controls)
                UntrackChild(descendant);
        }

        private void ChildControlAdded(object sender, ControlEventArgs e)
        {
            TrackChild(e.Control);
        }

        private void ChildControlRemoved(object sender, ControlEventArgs e)
        {
            UntrackChild(e.Control);
            RefreshHoverState();
        }

        private void ChildMouseEnterOrLeave(object sender, EventArgs e)
        {
            RefreshHoverState();
        }

        private void ChildMouseMove(object sender, MouseEventArgs e)
        {
            RefreshHoverState();
        }

        private void RefreshHoverState()
        {
            if (!IsHandleCreated || IsDisposed)
                return;

            Point pointer = PointToClient(Cursor.Position);
            bool inside = Visible && Enabled &&
                ClientRectangle.Contains(pointer) &&
                (Region == null || Region.IsVisible(pointer));
            SetHovered(inside);
        }

        private void SetHovered(bool value)
        {
            if (hovered == value && BackColor == (value ? hoverColor : normalColor))
                return;

            hovered = value;
            BackColor = value ? hoverColor : normalColor;
            Invalidate();
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
