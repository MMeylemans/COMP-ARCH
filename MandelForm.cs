using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MandelbrotC
{
    public class MandelForm : Form
    {
        private readonly PictureBox canvas = new PictureBox
        {
            Width = 640,
            Height = 640,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Normal
        };

        private readonly TextBox tbCx = new TextBox();
        private readonly TextBox tbCy = new TextBox();
        private readonly TextBox tbScale = new TextBox();
        private readonly TextBox tbMax = new TextBox();
        private readonly Button btnGo = new Button { Text = "Go!" };

        private readonly ComboBox cbPreset = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };

        // Sliders voor de kleueren
        private readonly TrackBar tbHue = new TrackBar { Minimum = 0, Maximum = 360, Value = 0, TickFrequency = 30, Width = 200 };
        private readonly TrackBar tbSat = new TrackBar { Minimum = 0, Maximum = 100, Value = 85, TickFrequency = 10, Width = 200 };
        private readonly TrackBar tbVal = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, Width = 200 };

        private double centerX = -0.5;
        private double centerY = 0.0;
        private double scale = 0.005;
        private int maxIter = 300;

        private readonly CultureInfo culture = CultureInfo.CurrentCulture;

        public MandelForm()
        {
            Text = "MandelbrotC";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Layout van alle knopjes etc.
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10,
                Padding = new Padding(10),
                AutoSize = true
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            grid.Controls.Add(new Label { Text = "midden x:", AutoSize = true }, 0, 0);
            grid.Controls.Add(tbCx, 1, 0);
            grid.Controls.Add(new Label { Text = "midden y:", AutoSize = true }, 0, 1);
            grid.Controls.Add(tbCy, 1, 1);
            grid.Controls.Add(new Label { Text = "schaal:", AutoSize = true }, 0, 2);
            grid.Controls.Add(tbScale, 1, 2);
            grid.Controls.Add(new Label { Text = "max aantal:", AutoSize = true }, 0, 3);
            grid.Controls.Add(tbMax, 1, 3);
            grid.Controls.Add(new Label { Text = "presets:", AutoSize = true }, 0, 4);
            grid.Controls.Add(cbPreset, 1, 4);

            // HSV-sliders voor de kleurtjes
            grid.Controls.Add(new Label { Text = "Kleurtoon (H):", AutoSize = true }, 0, 5);
            grid.Controls.Add(tbHue, 1, 5);
            grid.Controls.Add(new Label { Text = "Verzadiging (S):", AutoSize = true }, 0, 6);
            grid.Controls.Add(tbSat, 1, 6);
            grid.Controls.Add(new Label { Text = "Helderheid (V):", AutoSize = true }, 0, 7);
            grid.Controls.Add(tbVal, 1, 7);

            grid.Controls.Add(btnGo, 1, 8);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Controls.Add(grid, 0, 0);
            root.Controls.Add(canvas, 1, 0);
            Controls.Add(root);

            this.ClientSize = new Size(1000, 720);
            this.MinimumSize = new Size(1000, 740);

            // Presets met 2 beroemde plekken op de set en een paar zelfgekozen die we mooi vonden
            cbPreset.Items.AddRange(new object[]
            {
                "Basic",
                "Seahorse Valley",
                "Elephant Valley",
                "Plek 1",
                "Plek 2",
                "Plek 3"
            });

            // Preset wordt direct toegepast
            cbPreset.SelectedIndexChanged += (_, __) =>
            {
                if (cbPreset.SelectedIndex >= 0)
                    ApplyPreset(cbPreset.SelectedIndex);
            };

            btnGo.Click += (_, __) =>
            {
                ReadUI();
                Render();
            };
            canvas.MouseClick += Canvas_MouseClick;

            // Sliders updaten gelijk het plaatje
            tbHue.Scroll += (_, __) => Render();
            tbSat.Scroll += (_, __) => Render();
            tbVal.Scroll += (_, __) => Render();

            PutStateInUI();
            Render();
        }

        // Past een preset toe op de parameters
        private void ApplyPreset(int idx)
        {
            switch (idx)
            {
                case 0: centerX = -0.5; centerY = 0.0; scale = 0.005; maxIter = 300; break;
                case 1: centerX = -0.745; centerY = 0.105; scale = 0.0008; maxIter = 500; break;
                case 2: centerX = 0.285; centerY = 0.01; scale = 0.0015; maxIter = 400; break;
                case 3: centerX = -0.088; centerY = 0.654; scale = 0.0007; maxIter = 600; break;
                case 4: centerX = -1.25066; centerY = 0.02012; scale = 0.00008; maxIter = 1000; break;
                case 5: centerX = -1.24977375; centerY = 0.04585625; scale = 2.5e-06; maxIter = 450; break;
                default: return;
            }
            PutStateInUI();
            Render();
        }

        // Zet de huidige parameterwaarden in de UI
        private void PutStateInUI()
        {
            tbCx.Text = centerX.ToString("R", culture);
            tbCy.Text = centerY.ToString("R", culture);
            tbScale.Text = scale.ToString("R", culture);
            tbMax.Text = maxIter.ToString(culture);
        }

        // Leest de input uit de UI en past de parameters aan
        private void ReadUI()
        {
            if (double.TryParse(tbCx.Text, NumberStyles.Float, culture, out var cx)) centerX = cx;
            if (double.TryParse(tbCy.Text, NumberStyles.Float, culture, out var cy)) centerY = cy;
            if (double.TryParse(tbScale.Text, NumberStyles.Float, culture, out var sc) && sc > 0) scale = sc;
            if (int.TryParse(tbMax.Text, NumberStyles.Integer, culture, out var mi) && mi > 0) maxIter = mi;
        }

        // Linkermuisknop zoomt in, rechtermuisknop zoomt uit
        private void Canvas_MouseClick(object? sender, MouseEventArgs e)
        {
            centerX = PixelToX(e.X);
            centerY = PixelToY(e.Y);

            double factor = (e.Button == MouseButtons.Right) ? 2.0 : 0.5;
            scale *= factor;

            PutStateInUI();
            Render();
        }

        private double PixelToX(int px) => centerX + (px - canvas.Width / 2.0) * scale;

        private double PixelToY(int py) => centerY - (py - canvas.Height / 2.0) * scale;

        // Tekent de set
        private void Render()
        {
            int w = canvas.Width;
            int h = canvas.Height;

            byte[] buf = new byte[w * h * 3];

            int p = 0;
            for (int py = 0; py < h; py++)
            {
                double y0 = PixelToY(py);
                for (int px = 0; px < w; px++)
                {
                    double x0 = PixelToX(px);
                    int iter = BerekenIteraties(x0, y0, maxIter, out double zn2);

                    Color c = BepaalKleur(iter, zn2, maxIter);

                    buf[p++] = c.B;
                    buf[p++] = c.G;
                    buf[p++] = c.R;
                }
            }

            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, w, h);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = data.Stride;
                if (stride == w * 3)
                {
                    Marshal.Copy(buf, 0, data.Scan0, buf.Length);
                }
                else
                {
                    for (int row = 0; row < h; row++)
                    {
                        IntPtr dst = data.Scan0 + row * stride;
                        Marshal.Copy(buf, row * w * 3, dst, w * 3);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            var old = canvas.Image;
            canvas.Image = bmp;
            old?.Dispose();
        }

        // berekent het aantal iteraties voor een punt 
        private static int BerekenIteraties(double x, double y, int maxIter, out double zn2)
        {
            double a = 0.0, b = 0.0;
            int i = 0;

            while (i < maxIter)
            {
                double a2 = a * a;
                double b2 = b * b;

                if (a2 + b2 > 4.0) break;

                double twoab = 2.0 * a * b;
                a = a2 - b2 + x;
                b = twoab + y;
                i++;
            }
            zn2 = a * a + b * b;
            return i;
        }

        // Bepaalt de kleur van een punt op basis van het aantal iteraties en de HSV
        private Color BepaalKleur(int iter, double zn2, int maxIter)
        {
            if (iter >= maxIter) return Color.Black;

            double mod = Math.Sqrt(zn2);
            if (mod <= 1.0000001) mod = 1.0000001;
            double nu = iter + 1.0 - Math.Log(Math.Log(mod)) / Math.Log(2.0);
            double t = nu / maxIter;

            double h = (tbHue.Value + 360.0 * t) % 360.0;
            double s = tbSat.Value / 100.0;
            double v = tbVal.Value / 100.0;

            return HsvToRgb(h, s, v);
        }

        // waarde kan alleen tussen 0 en 255 liggen
        private static int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);

        // HSV naar RGB met de standaard formule
        private static Color HsvToRgb(double h, double s, double v)
        {
            h = (h % 360 + 360) % 360;
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r1 = 0, g1 = 0, b1 = 0;
            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            int r = (int)Math.Round((r1 + m) * 255.0);
            int g = (int)Math.Round((g1 + m) * 255.0);
            int b = (int)Math.Round((b1 + m) * 255.0);
            return Color.FromArgb(Clamp(r), Clamp(g), Clamp(b));
        }
    }
}
