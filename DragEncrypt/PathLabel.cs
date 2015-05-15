using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace DragEncrypt
{
    /// <summary>
    /// Based on: http://stackoverflow.com/questions/2397860/c-sharp-winforms-smart-textbox-control-to-auto-format-path-length-based-on-tex
    /// </summary>
    class PathLabel : Label
    {
        [Browsable(false)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set { base.AutoSize = false; }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var flags = TextFormatFlags.PathEllipsis | GetContentAlligment();
            var color = Enabled ? DefaultForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, color, flags);
        }

        private TextFormatFlags GetContentAlligment()
        {
            switch (TextAlign)
            {
                case ContentAlignment.BottomCenter:
                    return TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter;
                case ContentAlignment.BottomLeft:
                    return TextFormatFlags.Bottom| TextFormatFlags.Left;
                case ContentAlignment.BottomRight:
                    return TextFormatFlags.Bottom | TextFormatFlags.Right;
                case ContentAlignment.MiddleCenter:
                    return TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;
                case ContentAlignment.MiddleLeft:
                    return TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                case ContentAlignment.MiddleRight:
                    return TextFormatFlags.VerticalCenter | TextFormatFlags.Right;
                case ContentAlignment.TopCenter:
                    return TextFormatFlags.Top | TextFormatFlags.HorizontalCenter;
                case ContentAlignment.TopLeft:
                    return TextFormatFlags.Top | TextFormatFlags.Left;
                case ContentAlignment.TopRight:
                    return TextFormatFlags.Top | TextFormatFlags.Right;
                default:
                    return TextFormatFlags.Top | TextFormatFlags.Left;
            }
        }
    }
}
