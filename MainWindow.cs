using System;
using System.Threading;
using Gdk;
using Gtk;
using Window = Gtk.Window;
using CliWrap;
using System.Text;
using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace programma
{
    class MainWindow : Window
    {
        private Builder _builder;
        private FileChooserButton _loadImageButton;
        private Image _displayImage;
        private Button _extract;
        private TextView _textBox;
        private Button _textSave;
        private Button _textCanc;
        private LevelBar _levelBar;
        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        Pixbuf pixbuf;

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            this._builder = builder;
            builder.Autoconnect(this);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _loadImageButton = (FileChooserButton) _builder.GetObject("loadImageButton");
            
            if(_loadImageButton != null)
            {
                _loadImageButton.FileSet += OnloadImageButtonClicked;
            }
            else
            {
                ShowDialog("Impossibile trovare l'elemento che apre l'immagine nella GUI");
            }

            //inizializzo i componenti
            _displayImage = (Image)_builder.GetObject("displayImage");
            _extract = (Button)_builder.GetObject("estrai");
            _textBox = (TextView)_builder.GetObject("textBox");
            _textSave = (Button)_builder.GetObject("textSave");
            _textCanc = (Button)_builder.GetObject("textCanc");
            _levelBar = (LevelBar)_builder.GetObject("levelBar");

            //Assegno ad ogni evento una funzione
            _extract.Clicked += OnLoadExtractText;
            _textCanc.Clicked += clearSettings;
            _textSave.Clicked += stringDataSaving;

            //Filtro esclusivo per png
            FileFilter filter = new();
            filter.AddPattern("*.png");
            filter.Name = "PNG files";
            _loadImageButton.AddFilter(filter);
        }
        private void OnloadImageButtonClicked(object sender, EventArgs args)
        {
            //prendeo il percordo dell'immagine
            pixbuf = new (_loadImageButton.Filename);
            LoadAndDisplayImage();
        }
        private void LoadAndDisplayImage()
        {
            //minimo altezza e larghezza immagine
            int newWidth = Math.Min(_displayImage.AllocatedWidth, pixbuf.Width);
            int newHeight = Math.Min(_displayImage.AllocatedHeight, pixbuf.Height);

            //calcolo altezza e larghezza in base all'immagine inserita
            if(_displayImage.AllocatedHeight < pixbuf.Height)
            {
                newWidth = (int)(newWidth * _displayImage.AllocatedHeight / (double)pixbuf.Width);
            }

            if(_displayImage.AllocatedWidth < pixbuf.Width)
            {
                newHeight = (int)(newHeight * _displayImage.AllocatedWidth / (double)pixbuf.Height);
            }

            //Ridimensionamento dell'immagine
            Pixbuf newPixbuf = new (pixbuf.Colorspace, pixbuf.HasAlpha, pixbuf.BitsPerSample, newWidth, newHeight);
            pixbuf.Scale(newPixbuf, 0, 0, newWidth, newHeight, 0, 0, (double) newWidth / pixbuf.Width, (double) newHeight / pixbuf.Height, InterpType.Bilinear);
            
            //mostro l'immagine
            _displayImage.Pixbuf = newPixbuf;
        }

        private async void OnLoadExtractText (object sender, EventArgs args)
        {
            //StringBuilder che conterrà il testo dell'immagine
            StringBuilder stringa = new StringBuilder();

            await Cli.Wrap("Tesseract").WithArguments($"-l eng+ita \"{_loadImageButton.Filename}\" stdout").WithStandardOutputPipe(PipeTarget.ToStringBuilder(stringa)).ExecuteAsync();

            //Inserisco il testo nel textBox su display
            _textBox.Buffer.Text = stringa.ToString();
        }

        private void clearSettings (object sender, EventArgs args) 
        {
            //Pulisco: l'immagine, il testo ed il selettore di file
            _textBox.Buffer.Text = string.Empty;
            _loadImageButton.UnselectAll();
            _displayImage.Clear();
        }

        private void stringDataSaving (object sender, EventArgs args)
        {
            DateTime timeCurrent = DateTime.Now;
            
            //Creo un dictionary con i dati che mi interessano
            var data = new Dictionary <string, string>
            {
                {"timeCurrent", timeCurrent.ToString("dd-MM-yyyy hh:mm tt")},
                {"text", _textBox.Buffer.Text}
            };

            //Trasformo il dizionario in lista
            List<Dictionary<string, string>> dataList;

            if(File.Exists("salvataggi.json"))
            {
                //Leggo il contenuto del file.json
                string existingJson = File.ReadAllText("salvataggi.json");
                if(!string.IsNullOrEmpty(existingJson))
                {
                    //Deserializzo il file e lo aggiungo alla lista
                    dataList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(existingJson);
                }
                else
                {
                    //Creo una nuova lista vuota se il file è vuoto
                    dataList = new List<Dictionary<string, string>>();
                }
            }
            else 
            {
                dataList = new List<Dictionary<string, string>>();
            }

            //Aggiungo il nuovo dato alla lista, serializzo nuovamente il file e scrivo tutto il contenuto della lista sul file
            dataList.Add(data);
            string updateJson = JsonSerializer.Serialize(dataList, new JsonSerializerOptions {WriteIndented = true});
            File.WriteAllText("salvataggi.json", updateJson);

            //Pop-up con salvataggio confermato
            ShowDialog("Salvataggio avvenuto con successo!");
        }

        private void ShowDialog(string message)
        {
            //Pop-up
            var dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, message);
            dialog.Run();
            dialog.Destroy();
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}