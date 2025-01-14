using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace CaptureScreenApp
{
    /*
     * Scrivi un software in c# che avvii una piccola form con un pulsante "Copia". Il click su questo pulsante deve chiedere, solo per la prima volta, di indicare due punti sullo schermo che indicano i vertici di un rettangolo. Una volta identificato questo rettangolo, ad ogni click del pulsante deve eseguire un capture screen di quella porzione di monitor e salvarlo come jpg in una cartella "CrazyPub" sul Desktop dell'utente corrente
     * Modifica così il comportamento dell'applicazione. Aggiungi un pulsante "Multi". Se si clicca tale pulsante, oltre a quanto già detto, richiedi anche di cliccare su un terzo punto e tienilo in memoria. A quel punto chiedi un numero intero e ripeti queste operazioni per il numero di volte indicate da quel numero: cattura lo schermo tra le prime due coordinate, clicca sullo schermo sul terzo punto (simula il click dell'utente su un button di un'altra app in sfondo) e riparti con un nuovo capture dello schermo tra le prime due coordinate. Ad ogni capture incrementa il numero X di uno nel nome del file salvato, che è così strutturato: CrazyCatpture_X. Il numero X deve avere un padding a sinistra con 0 per essere formato in totale da 3 cifre (esempio 002)
     */
    partial class MainForm : Form
    {
        private Button btnCapture;
        private Button btnMulti;
        private Rectangle captureRectangle;
        private Point? firstPoint = null;
        private Point? thirdPoint = null;
        private bool isRectangleSet = false;
        private int counter = 0;

        public MainForm()
        {
            this.Text = "Screen Capture Tool";
            this.Size = new Size(400, 200);

            btnCapture = new Button
            {
                Text = "Copia",
                Size = new Size(100, 50),
                Location = new Point(50, 50),
                Anchor = AnchorStyles.None
            };
            btnCapture.Click += BtnCapture_Click;
            this.Controls.Add(btnCapture);

            btnMulti = new Button
            {
                Text = "Multi",
                Size = new Size(100, 50),
                Location = new Point(200, 50),
                Anchor = AnchorStyles.None
            };
            btnMulti.Click += BtnMulti_Click;
            this.Controls.Add(btnMulti);
        }

        private void BtnCapture_Click(object sender, EventArgs e)
        {
            if (!isRectangleSet)
            {
                MessageBox.Show("Seleziona i due punti per il rettangolo cliccando sullo schermo.", "Imposta Rettangolo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetCaptureRectangle();
            }
            else
            {
                CaptureScreenAndSave();
            }
        }

        private void BtnMulti_Click(object sender, EventArgs e)
        {
            if (!isRectangleSet)
            {
                MessageBox.Show("Seleziona i due punti per il rettangolo cliccando sullo schermo.", "Imposta Rettangolo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetCaptureRectangle();
            }

            MessageBox.Show("Clicca sul terzo punto per selezionare il bottone di destinazione.", "Imposta Punto", MessageBoxButtons.OK, MessageBoxIcon.Information);
            thirdPoint = GetPointFromUser();

            if (thirdPoint.HasValue)
            {
                int repetitions = GetRepetitionsFromUser();

                for (int i = 0; i < repetitions; i++)
                {
                    CaptureScreenAndSave();
                    SimulateMouseClick(thirdPoint.Value);
                    System.Threading.Thread.Sleep(500); // Attesa per evitare conflitti
                }

                MessageBox.Show("Operazione completata.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SetCaptureRectangle()
        {
            this.Hide(); // Nasconde la finestra per consentire la selezione
            using (var overlay = new OverlayForm())
            {
                overlay.PointSelected += (p) =>
                {
                    if (firstPoint == null)
                    {
                        firstPoint = p;
                        MessageBox.Show("Primo punto selezionato. Clicca sul secondo punto.");
                    }
                    else
                    {
                        Point secondPoint = p;
                        captureRectangle = new Rectangle(
                            Math.Min(firstPoint.Value.X, secondPoint.X),
                            Math.Min(firstPoint.Value.Y, secondPoint.Y),
                            Math.Abs(secondPoint.X - firstPoint.Value.X),
                            Math.Abs(secondPoint.Y - firstPoint.Value.Y)
                        );

                        isRectangleSet = true;
                        firstPoint = null;
                        overlay.Close(); // Chiude l'overlay
                        MessageBox.Show("Rettangolo impostato correttamente.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Show();
                    }
                };

                overlay.ShowDialog();
            }
        }

        private Point? GetPointFromUser()
        {
            using (var overlay = new OverlayForm())
            {
                Point? selectedPoint = null;
                overlay.PointSelected += (p) =>
                {
                    selectedPoint = p;
                    overlay.Close();
                };

                overlay.ShowDialog();
                return selectedPoint;
            }
        }

        private int GetRepetitionsFromUser()
        {
            string input = Interaction.InputBox("Inserisci il numero di ripetizioni:", "Ripetizioni", "1");
            if (int.TryParse(input, out int repetitions) && repetitions > 0)
            {
                return repetitions;
            }
            else
            {
                MessageBox.Show("Inserimento non valido. Verrà utilizzato il valore predefinito (1).", "Avviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 1;
            }
        }

        private void CaptureScreenAndSave()
        {
            try
            {
                using (Bitmap bitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(captureRectangle.Location, Point.Empty, captureRectangle.Size);
                    }

                    string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CrazyPub");
                    Directory.CreateDirectory(folderPath);

                    string filePath = Path.Combine(folderPath, $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}_{counter.ToString("0000")}.png");
                    bitmap.Save(filePath, ImageFormat.Png);
                    counter++;

                    if (!thirdPoint.HasValue)
                    {
                        MessageBox.Show($"Screenshot salvato in: {filePath}", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il salvataggio: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SimulateMouseClick(Point point)
        {
            Cursor.Position = point;
            mouse_event(MouseEventFlags.LeftDown | MouseEventFlags.LeftUp, 0, 0, 0, 0);
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [Flags]
        private enum MouseEventFlags
        {
            LeftDown = 0x02,
            LeftUp = 0x04
        }

        private class OverlayForm : Form
        {
            public event Action<Point> PointSelected;

            public OverlayForm()
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.BackColor = Color.Black;
                this.Opacity = 0.5;
                this.TopMost = true;
                this.Click += OverlayForm_Click;
            }

            private void OverlayForm_Click(object sender, EventArgs e)
            {
                var mouseEvent = e as MouseEventArgs;
                if (mouseEvent != null)
                {
                    PointSelected?.Invoke(mouseEvent.Location);
                }
            }
        }
    }
}
