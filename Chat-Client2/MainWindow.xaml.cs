using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using WebSocketSharp;
using Newtonsoft.Json;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebSocket ws;
        private string userId;


        public MainWindow()
        {
            InitializeComponent();

            userId = "Client User2";
            string serverUrl = "ws://localhost:8080";

            ConnectToServer(serverUrl);
        }

        
        private void ConnectToServer(string serverUrl)
        {
            ws = new WebSocket(serverUrl);

            ws.OnOpen += (sender, e) =>
            {
                ws.Send(JsonConvert.SerializeObject(new
                {
                    type = "identify",
                    userId = userId
                }));
            };

            ws.OnMessage += (sender, e) =>
            {
                var message = JsonConvert.DeserializeObject<dynamic>(e.Data);
                
                Dispatcher.Invoke(() =>
                {
                    switch ((string)message.type)
                    {
                        case "connected":
                            MessageList.Items.Add("Connected as user "+message.userId);
                            break;
                            
                        case "chat":
                            string displayMessage = message.timestamp+" - "+message.userId+": "+message.content;
                            MessageList.Items.Add(displayMessage);
                            break;
                    }
                });
            };

            ws.OnError += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageList.Items.Add("Error: "+e.Message);
                });
            };

            ws.OnClose += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageList.Items.Add("Disconnected from server");
                });
            };

            ws.Connect();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MessageInput.Text))
                return;

            try
            {
                ws.Send(JsonConvert.SerializeObject(new
                {
                    type = "chat",
                    userId = userId,
                    content = MessageInput.Text
                }));

                MessageInput.Clear();
            }
            catch (Exception ex)
            {
                MessageList.Items.Add("Error sending message: "+ex.Message);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                ws.Close();
            }
            base.OnClosing(e);
        }
    }
}
