using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NextGen__7
{
    internal static class EduConnectResponsive
    {
        private sealed class OriginalControl
        {
            internal Rectangle Bounds;
            internal Size ParentSize;
        }

        private sealed class FormState
        {
            internal readonly Dictionary<Control, OriginalControl> Controls =
                new Dictionary<Control, OriginalControl>();
            internal bool LayingOut;
        }

        private static readonly Dictionary<Form, FormState> Attached =
            new Dictionary<Form, FormState>();

        internal static void AttachToOpenForms()
        {
            foreach (Form form in Application.OpenForms.Cast<Form>().ToArray())
            {
                if (form.IsDisposed || Attached.ContainsKey(form)) continue;
                Attach(form);
            }
        }

        private static void Attach(Form form)
        {
            var state = new FormState();
            Capture(form, state);
            Attached.Add(form, state);
            form.FormClosed += (sender, args) => Attached.Remove(form);
            form.Resize += (sender, args) => Arrange(form, state);
            form.MinimumSize = new Size(760, 520);
            form.MaximumSize = Size.Empty;
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
            form.AutoScroll = true;
            form.WindowState = FormWindowState.Maximized;
            Arrange(form, state);
        }

        private static void Capture(Control parent, FormState state)
        {
            foreach (Control child in parent.Controls)
            {
                state.Controls[child] = new OriginalControl
                {
                    Bounds = child.Bounds,
                    ParentSize = parent.ClientSize
                };
                Capture(child, state);
            }
        }

        private static void Arrange(Form form, FormState state)
        {
            if (state.LayingOut || form.IsDisposed || form.Disposing) return;
            state.LayingOut = true;
            try
            {
                form.SuspendLayout();
                ArrangeChildren(form, state);
            }
            finally
            {
                form.ResumeLayout(true);
                state.LayingOut = false;
            }
        }

        private static void ArrangeChildren(Control parent, FormState state)
        {
            parent.SuspendLayout();
            try
            {
                foreach (Control child in parent.Controls)
                {
                    OriginalControl original;
                    if (!state.Controls.TryGetValue(child, out original))
                    {
                        state.Controls[child] = new OriginalControl
                        {
                            Bounds = child.Bounds,
                            ParentSize = parent.ClientSize
                        };
                        original = state.Controls[child];
                    }

                    if (child.Dock == DockStyle.None &&
                        original.ParentSize.Width > 0 &&
                        original.ParentSize.Height > 0)
                    {
                        double x = (double)parent.ClientSize.Width /
                            original.ParentSize.Width;
                        double y = (double)parent.ClientSize.Height /
                            original.ParentSize.Height;
                        int left = (int)Math.Round(original.Bounds.Left * x);
                        int top = (int)Math.Round(original.Bounds.Top * y);
                        int width = Math.Max(1, (int)Math.Round(original.Bounds.Width * x));
                        int height = Math.Max(1, (int)Math.Round(original.Bounds.Height * y));
                        child.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        child.SetBounds(left, top, width, height);
                    }
                    ArrangeChildren(child, state);
                }
            }
            finally
            {
                parent.ResumeLayout(true);
            }
        }
    }
}
