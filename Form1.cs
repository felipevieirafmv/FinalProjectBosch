using System.Drawing;
using System.Windows.Forms;
using System;
using Timer = System.Windows.Forms.Timer;
using System.Diagnostics;

namespace HandWriteRecognize
{
    public partial class Form1 : Form
    {
        PictureBox pb = new PictureBox {
            Dock = DockStyle.Fill
        };
        PictureBox imagepb = new PictureBox();
        Bitmap bmp;
        Timer tm;
        Graphics g;
        string uploadedImagePath = "";
        private bool isDrawing = false;
        private bool isErasing = false;
        private int thickness = 5;
        private Point previousPoint;
        private Point canvaSPoint = new Point(500, 0); // Canva Start Point
        private Size canvaSize;

        public Button createButton(string text, Point point, Size size)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = point;
            button.Size = size;
            return button;
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

            Button button3 = createButton("Fazer leitura\nda imagem", new Point(10, 130), new Size(100, 40));
            // button3.Click += ;
            this.Controls.Add(button3);

            Button button4 = createButton("Tirar print", new Point(10, 170), new Size(100, 30));
            button4.Click += printScreen;
            this.Controls.Add(button4);

            this.KeyPreview = true;

            this.tm = new Timer();
            this.tm.Interval = 20;

            this.BackColor = Color.White;

            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;

            this.Controls.Add(pb);
            pb.MouseDown += pb_MouseDown;
            pb.MouseMove += pb_MouseMove;
            pb.MouseUp += pb_MouseUp;

            canvaSize = new Size(pb.Width - canvaSPoint.X, pb.Height - canvaSPoint.Y);

            this.KeyDown += (o, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Application.Exit();

                if (e.KeyCode == Keys.Back)
                    clearPanel();

                if (e.KeyCode == Keys.E)
                    isErasing = !isErasing;
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

            tm.Start();
        }

        void Frame()
        {
            Font font = new Font("Arial", 12);
            Brush brush = Brushes.Black;
            Pen pen = new Pen(brush);

            g.FillRectangle(Brushes.LightGray, 0, 0, canvaSPoint.X, this.Height);
            g.DrawLine(pen, canvaSPoint.X - 1, canvaSPoint.Y, canvaSPoint.X - 1, this.Height);

            string thicknessText = $"{thickness}";
            g.DrawString(thicknessText, font, brush, new PointF(10, 10));

            string commandsText = "E = Erase\nBackSpace = Clear";
            g.DrawString(commandsText, font, brush, new PointF(10, 30));

            var splitText = uploadedImagePath.Split('\\');
            string file = splitText[splitText.Length - 1];
            if (file.Length > 10)
                file = file.Substring(0, 7) + "...";
            g.DrawString(file, font, brush, new PointF(110, 75));

            if (this.isErasing)
            {
                this.Cursor = new Cursor("./cursors/aero_unavail.cur");
                return;
            }
            this.Cursor = new Cursor("./cursors/aero_pen.cur");
        }

        private void clearPanel()
        {
            this.thickness = 5;
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
            Bitmap croppedBitmap = bmp.Clone(new Rectangle(canvaSPoint.X, canvaSPoint.Y, pb.Width - canvaSPoint.X, pb.Height - canvaSPoint.Y), bmp.PixelFormat);
            string filePath = "screenshot.png";
            croppedBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            MessageBox.Show("Captura de tela salva com sucesso em: " + filePath, "Sucesso");
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
                    uploadedImagePath = openFileDialog.FileName;
                    imagepb.Location = new Point(canvaSPoint.X, canvaSPoint.Y);
                    imagepb.Size = canvaSize;
                    imagepb.Image = new Bitmap(uploadedImagePath);
                    pb.Controls.Add(imagepb);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocorreu um erro ao carregar a imagem: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void closeImage(object sender, EventArgs e)
            => pb.Controls.Remove(imagepb);
    }

}
