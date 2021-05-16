/*
 * Sviluppatore: Pulga Luca
 * Classe: 4^L
 * Data di consegna: 2021/05/17
 * Scopo: 2.Utilizzando il programma di oggi come base, creare un semplice gioco in rete 
 * in cui ci sia uno scambio di dati tra due processi (potrebbe essere un gioco di carte oppure
 * un lancio di dadi), curare anche l'interfaccia inserendo le apposite immagini.
 * GIOCO: MASTERMIND SEMPLIFICATO.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MastermindUdpClient
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class client : Window
    {
        public client()
        {
            InitializeComponent();

            // Source socket.
            // IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000); // uso loopback o anche indirizzo di 192.168.1.73
            IPEndPoint localEndPointAutomated = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 56000); // uso loopback o anche indirizzo di 192.168.1.73

            txtIpAdd.Text = localEndPointAutomated.Address.ToString();
            txtDestPort.Text = "55000";

            Thread t1 = new Thread(new ParameterizedThreadStart(SocketReceive)); // Parametizzazione di un thread.
            t1.Start(localEndPointAutomated);
        }


        /// <summary>
        /// get automated local ip.
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPAddress()
        {
            try
            {
                string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
                // Get the IP  
                string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString(); // Get local ip address

                Uri uri = new Uri("http://" + myIP);

                return uri.Host.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }

        /// <summary>
        /// control ipv4 address.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static bool IsIPv4(string ipAddress)
        {
            return Regex.IsMatch(ipAddress, @"^\d{1,3}(\.\d{1,3}){3}$") && ipAddress.Split('.').SingleOrDefault(s => int.Parse(s) > 255) == null; // Regex per controllare ip.
        }

        /// <summary>
        /// Regole del gioco.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRules_Click(object sender, RoutedEventArgs e)
        {
            Rules rules = new Rules();
            rules.Show();
        }

        /// <summary>
        /// Invio dei dati.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtDestPort.Text.Length == 0)
                    throw new Exception("Immettere una porta.");
                if (txtDestPort.Text.Length < 0 || txtDestPort.Text.Length > 65535)
                    throw new Exception("Immettere una porta valida tra 0 e 65535.");
                if (txtIpAdd.Text.Length == 0)
                    throw new Exception("Immettere un ip.");
                if (txtNumberToSend.Text.Length == 0)
                    throw new Exception("Immettere un messaggio.");
                if (txtNumberToSend.Text.Length != 5)
                    throw new Exception("Immettere un numero di 5 cifre.");
                if (!IsIPv4(txtIpAdd.Text))
                    throw new Exception("Immettere un indirizzo ip valido.");
                

                IPAddress ipDest = IPAddress.Parse(txtIpAdd.Text); // Recupero informazioni ip del destinatario.
                int portDest = int.Parse(txtDestPort.Text); // Porta di destinazione.

                IPEndPoint remoteEndPoint = new IPEndPoint(ipDest, portDest);

                // Socket abbinato al socket primario.
                Socket s = new Socket(ipDest.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                string n = txtNumberToSend.Text;
                // Leggo direttamente dall'interfaccia e scompatto direttamente in byte.
                Byte[] byteInviati = Encoding.ASCII.GetBytes(n);

                s.SendTo(byteInviati, remoteEndPoint); // byte e a chi vogliamo mandarli.


                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lblTrasmissione.Content += "You: " + n + "\n";
                }));

                if (n == "#####")
                {
                    MessageBox.Show("HAI PERSO!", "Mastermind - client - Hai perso!", MessageBoxButton.OK, MessageBoxImage.Information);
                    Thread.Sleep(1000);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lblRicezione.Content = "";
                        lblTrasmissione.Content = "";
                        txtNumberToSend.Text = "";
                        txtNumeroDaIndovinare.Text = "";
                        txtNumeroDaIndovinare.IsEnabled = true;
                        btnScelto.IsEnabled = true;
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore nell'invio del messaggio.\n" + ex.Message, "Errore nell'invio dei dati", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// Ricezione dei dati.
        /// </summary>
        /// <param name="sourceEndPoint"></param>
        public async void SocketReceive(object sourceEndPoint) // Programmazione asicrona, ascolta e continua ad utilizzare l'interfaccai perchè le 2 cose, ascolto e invio vengono supportate.
        {
            IPEndPoint sourceEP = (IPEndPoint)sourceEndPoint;

            // socket da cui riceveremo.
            // IPv4 e altre info    Tipo di socket    Tipo di protocollo.
            Socket t = new Socket(SocketType.Dgram, ProtocolType.Udp);

            t.Bind(sourceEP); // Associa un socket ad un endPoint.

            Byte[] byteRicevuti = new byte[256]; // Max ricevo 256 byte

            string message = "";
            int bytes = 0; // contatore byte ricevuti.

            // Thread continua ad ascoltare e ricevere i byte.
            // Task parte di thread.
            await Task.Run(() =>
            {
                while (true)
                {
                    // Ci avvisa quando sul socket sono arrivati dei dati.
                    if (t.Available > 0)
                    {
                        // Ricezione
                        bytes = t.Receive(byteRicevuti, byteRicevuti.Length, 0);
                        // Prendo tutti i caratteri che ho messo dentro al vettore di byte, per ogni carattere e  li concateno all0interno del messaggio.
                        message = Encoding.ASCII.GetString(byteRicevuti, 0, bytes);

                        // Gestione elementi grafici difficoltosa e non si può fare così

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            lblRicezione.Content += "Host: " + message + "\n";
                        }));

                        if (message == "#####")
                        {
                            MessageBox.Show("HAI VINTO!", "Mastermind - client - Hai vinto!", MessageBoxButton.OK, MessageBoxImage.Information);
                            Thread.Sleep(1000);
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                lblRicezione.Content = "";
                                lblTrasmissione.Content = "";
                                txtNumberToSend.Text = "";
                                txtNumeroDaIndovinare.Text = "";
                                txtNumeroDaIndovinare.IsEnabled = true;
                                btnScelto.IsEnabled = true;
                            }));
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Scelta del numero da indovinare dall'avversario.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnScelto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtNumeroDaIndovinare.Text.Length == 0)
                    throw new Exception("Scegliere il numero che il client deve indovinare.");
                if (txtNumeroDaIndovinare.Text.Length != 5)
                    throw new Exception("Immettere un numero di 5 cifre.");

                txtNumeroDaIndovinare.IsEnabled = false;
                btnScelto.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore nella scelta del numero.\n" + ex.Message, "Scegli il numero che deve essere indovinato.", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
    }
}
