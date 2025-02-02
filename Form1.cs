using System.Drawing;
using System.Windows.Forms;
using System;
using Timer = System.Windows.Forms.Timer;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace HandWriteRecognize
{
    public partial class Form1 : Form
    {
        PictureBox pb = new PictureBox
        {
            Dock = DockStyle.Fill
        };
        PictureBox imagepb = new PictureBox();
        Bitmap bmp;
        Timer tm;
        Timer savetm;
        Graphics g;
        private string uploadedImagePath = "";
        private bool isDrawing = false;
        private bool isErasing = false;
        string outputText = "Output:";
        private int saveInterval = 500;
        private int thickness = 25;
        private Point previousPoint;
        private Point canvaSPoint = new Point(200, 0); // Canva Start Point
        private Point canvaFPoint = new Point(
            Screen.PrimaryScreen.Bounds.Width,
            Screen.PrimaryScreen.Bounds.Height - 200
        );
        private Size canvaSize;

        public Button createButton(string text, Point point, Size size)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = point;
            button.Size = size;
            return button;
        }

        public void saveBitmap(Bitmap bmp, string path)
        {   
            Bitmap croppedBitmap = bmp.Clone(new Rectangle(canvaSPoint.X, canvaSPoint.Y, canvaSize.Width, canvaSize.Height), bmp.PixelFormat);
            croppedBitmap = new Bitmap(croppedBitmap, canvaSize.Width / 6, canvaSize.Height / 6);
            string filePath = path;
            croppedBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public Form1()
        {
            InitializeComponent();

            Button button = createButton("Fazer upload", new Point(10, 70), new Size(100, 30));
            button.Click += uploadImage;
            this.Controls.Add(button);

            Button button2 = createButton("Fechar imagem", new Point(10, 100), new Size(100, 30));
            button2.Click += closeImage;
            this.Controls.Add(button2);

            Button button3 = createButton("Tirar print", new Point(10, 130), new Size(100, 30));
            button3.Click += printScreen;
            this.Controls.Add(button3);

            this.KeyPreview = true;

            this.tm = new Timer();
            this.tm.Interval = 20;

            this.savetm = new Timer();
            this.savetm.Interval = saveInterval;

            this.BackColor = Color.White;

            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;

            this.Controls.Add(pb);

            pb.MouseDown += pb_MouseDown;
            pb.MouseMove += pb_MouseMove;
            pb.MouseUp += pb_MouseUp;

            canvaSize = new Size(canvaFPoint.X - canvaSPoint.X, canvaFPoint.Y - canvaSPoint.Y);

            this.KeyDown += (o, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Application.Exit();

                if (e.KeyCode == Keys.Back)
                    clearPanel();

                if (e.KeyCode == Keys.E)
                {
                    this.thickness = isErasing ? thickness /= 2 : thickness *= 2;
                    isErasing = !isErasing;
                }
            };

            this.MouseWheel += (o, e) =>
            {
                if (e.Delta > 0)
                    this.thickness++;
                else
                {
                    if (this.thickness > 1)
                    {
                        this.thickness--;
                    }
                }
            };

            this.Load += (o, e) =>
            {
                this.bmp = new Bitmap(pb.Width, pb.Height);
                g = Graphics.FromImage(bmp);
                g.Clear(Color.White);
                this.pb.Image = bmp;
            };

            tm.Tick += (o, e) =>
            {
                Frame();
                pb.Refresh();
            };

            savetm.Tick += (o, e) =>
            {
                saveBitmap(bmp, "screen.png");
                runPython();
            };

            tm.Start();
            savetm.Start();
        }

        void Frame()
        {
            Font font = new Font("Arial", 12);
            Brush brush = Brushes.Black;
            Pen pen = new Pen(brush);

            g.FillRectangle(Brushes.LightGray, 0, 0, canvaSPoint.X, pb.Height);
            g.DrawLine(pen, canvaSPoint.X - 1, canvaSPoint.Y, canvaSPoint.X - 1, canvaFPoint.Y);
            g.FillRectangle(Brushes.LightGray, 0, canvaFPoint.Y, pb.Width, pb.Height);
            g.DrawLine(pen, canvaSPoint.X, canvaFPoint.Y + 1, canvaFPoint.X, canvaFPoint.Y + 1);

            string thicknessText = $"{thickness}";
            g.DrawString(thicknessText, font, brush, new PointF(10, 10));

            string commandsText = "E = Erase\nBackSpace = Clear";
            g.DrawString(commandsText, font, brush, new PointF(10, 30));

            g.DrawString("Interval: " + this.savetm.Interval, font, brush, new PointF(10, 170));

            var splitText = uploadedImagePath.Split('\\');
            string file = splitText[splitText.Length - 1];
            int text_length = (int)canvaSPoint.X / 20;
            if (file.Length > text_length)
                file = file.Substring(0, text_length - 3) + "...";

            g.DrawString(file, font, brush, new PointF(110, 75));

            g.DrawString(outputText, new Font("Arial", 24), brush, new PointF(canvaSPoint.X, canvaFPoint.Y + 10));

            if (this.isErasing)
            {
                this.Cursor = new Cursor("./cursors/aero_unavail.cur");
                return;
            }
            this.Cursor = new Cursor("./cursors/aero_pen.cur");
        }

        private void clearPanel()
        {
            g.Clear(Color.White);
            pb.Invalidate();
        }
        private void pb_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            previousPoint = e.Location;
        }
        private void pb_MouseUp(object sender, MouseEventArgs e)
            => isDrawing = false;
        private void pb_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                Brush brush = isErasing ? Brushes.White : Brushes.Black;
                var deltaX = e.X - previousPoint.X;
                var deltaY = e.Y - previousPoint.Y;
                var dist = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
                for (float d = 0; d < 1; d += 1f / dist)
                {
                    var x = (1 - d) * previousPoint.X + d * e.X;
                    var y = (1 - d) * previousPoint.Y + d * e.Y;
                    g.FillEllipse(brush,
                        x - thickness / 2,
                        y - thickness / 2,
                        thickness, thickness
                    );
                }
                previousPoint = e.Location;
                pb.Invalidate();
            }
        }

        private void printScreen(object sender, EventArgs e)
        {
            saveBitmap(bmp, "screenshot.png");
        }
        private void uploadImage(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Arquivos de imagem|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Todos os arquivos|*.*"
            };

            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                try
                {
                    this.savetm.Interval = 4000;
                    uploadedImagePath = openFileDialog.FileName;

                    Bitmap image = new Bitmap(uploadedImagePath);
                    image.Save("uploaded.png", System.Drawing.Imaging.ImageFormat.Png);

                    image = new Bitmap(image, canvaSize.Width, canvaSize.Height);
                    imagepb.Location = new Point(canvaSPoint.X, canvaSPoint.Y);
                    imagepb.Size = canvaSize;
                    imagepb.Image = image;

                    pb.Controls.Add(imagepb);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocorreu um erro ao carregar a imagem: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void closeImage(object sender, EventArgs e)
        {
            pb.Controls.Remove(imagepb);
            uploadedImagePath = "";
            File.Delete("uploaded.png");
            this.savetm.Interval = saveInterval;
        }

        async private void runPython()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("http://127.0.0.1:8000/");

            if (response.IsSuccessStatusCode)
            {
                outputText = "Output: " + await response.Content.ReadAsStringAsync();
            }
        }
    }

}
