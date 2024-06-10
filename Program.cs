using System;
using Gtk;

namespace programma
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            var app = new Application("org.prova.prova", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            var win = new MainWindow();
            app.AddWindow(win);

            try
            {
                var icon = new Gdk.Pixbuf("Icona.jpg");
                win.Icon = icon;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore: Non è stato possibile caricare l'icona. " + ex.Message);
            }

            win.Show();
            Application.Run();
        }
    }
}
