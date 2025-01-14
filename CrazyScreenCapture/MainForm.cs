using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CaptureScreenApp
{
    /*
     * Scrivi un software in c# che avvii una piccola form con un pulsante "Copia". Il click su questo pulsante deve chiedere, solo per la prima volta, di indicare due punti sullo schermo che indicano i vertici di un rettangolo. Una volta identificato questo rettangolo, ad ogni click del pulsante deve eseguire un capture screen di quella porzione di monitor e salvarlo come jpg in una cartella "CrazyPub" sul Desktop dell'utente corrente
     * Modifica così il comportamento dell'applicazione. Aggiungi un pulsante "Multi". Se si clicca tale pulsante, oltre a quanto già detto, richiedi anche di cliccare su un terzo punto e tienilo in memoria. A quel punto chiedi un numero intero e ripeti queste operazioni per il numero di volte indicate da quel numero: cattura lo schermo tra le prime due coordinate, clicca sullo schermo sul terzo punto (simula il click dell'utente su un button di un'altra app in sfondo) e riparti con un nuovo capture dello schermo tra le prime due coordinate. Ad ogni capture incrementa il numero X di uno nel nome del file salvato, che è così strutturato: CrazyCatpture_X. Il numero X deve avere un padding a sinistra con 0 per essere formato in totale da 3 cifre (esempio 002)
     * Aggiungi un delay di 2 secondi dopo il click, prima di eseguire il capture screen
     * Quando si completa la procedura, anzichè aprire il popup, visualizza una scritta "TERMINATO" direttamente sulla form. Questa scritta deve sparire quando lancio una nuova cattura
     * Modifica in modo che il tempo tra un click e l'altro non sia fisso a 2 secondi ma sia richiesto all'utente con una inputbox
     */
    partial class MainForm : Form
    {
        private Rectangle captureRectangle;
        private Point? firstPoint = null;
        private Point? thirdPoint = null;
        private bool isRectangleSet = false;
        private int counter = 0;
        private int delayInMilliseconds = 2000; // Default delay

        public MainForm()
        {
            InitializeComponent();
            this.Text = "Screen Capture Tool";
        }

        private void BtnCapture_Click(object sender, EventArgs e)
        {
            ClearStatus();
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
            ClearStatus();
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
                delayInMilliseconds = GetDelayFromUser();

                for (int i = 0; i < repetitions; i++)
                {
                    SimulateMouseClick(thirdPoint.Value);
                    System.Threading.Thread.Sleep(delayInMilliseconds); // Delay personalizzato
                    CaptureScreenAndSave();
                }

                ShowStatus("TERMINATO");
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
            string input = Microsoft.VisualBasic.Interaction.InputBox("Inserisci il numero di ripetizioni:", "Ripetizioni", "1");
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

        private int GetDelayFromUser()
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Inserisci il tempo di attesa tra un click e l'altro (in millisecondi):", "Delay", delayInMilliseconds.ToString());
            if (int.TryParse(input, out int delay) && delay > 0)
            {
                return delay;
            }
            else
            {
                MessageBox.Show("Inserimento non valido. Verrà utilizzato il valore predefinito (2000 ms).", "Avviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 2000;
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

        private void ShowStatus(string message)
        {
            lblStatus.Text = message;
            lblStatus.Visible = true;
        }

        private void ClearStatus()
        {
            lblStatus.Visible = false;
            lblStatus.Text = string.Empty;
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
