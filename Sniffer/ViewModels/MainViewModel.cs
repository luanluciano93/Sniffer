using OXGaming.TibiaAPI;
using OXGaming.TibiaAPI.Constants;
using OXGaming.TibiaAPI.Utilities;
using Sniffer.Models;
using Sniffer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OXGaming.TibiaAPI.Network.ServerPackets;
using PacketApi = OXGaming.TibiaAPI.Network.Packet;
using System.Runtime.InteropServices;
using System.IO;
using TrackerConnection;
using Microsoft.Win32;

namespace Sniffer.ViewModels
{
    public class MainViewModel : PropertyChangedBase
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        public static MainWindow _window;
        public static TrackerMessage _tracker = new TrackerMessage();
        private static bool _connected = false;
        public Client _client;

        private ObservableCollection<Packet> _packets;
        public ObservableCollection<Packet> Packets
        {
            get { return _packets; }
            set
            {
                _packets = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Packet> _filteredPackets;
        public ObservableCollection<Packet> FilteredPackets
        {
            get { return _filteredPackets; }
            set
            {
                _filteredPackets = value;
                OnPropertyChanged();
            }
        }

        private Packet _selectedPacket;
        public Packet SelectedPacket
        {
            get { return _selectedPacket; }
            set
            {
                _selectedPacket = value;
                OnPropertyChanged();
            }
        }

        private int _selectedPacketTypeIndex;
        public int SelectedPacketTypeIndex
        {
            get { return _selectedPacketTypeIndex; }
            set
            {
                _selectedPacketTypeIndex = value;
                OnPropertyChanged();
            }
        }

        private int _filterOpCode;
        public string FilterOpCode
        {
            get { return _filterOpCode.ToString(); }
            set
            {
                if (int.TryParse(value, out int opCode)) {
                    _filterOpCode = opCode;
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel()
        {
        }
        private void Proxy_OnReceivedClientMessage(byte[] data)
        {
            AddToPackets(PacketType.Client, data);
        }
        private void Proxy_OnReceivedServerMessage(byte[] data)
        {
            AddToPackets(PacketType.Server, data);
        }
        public void AddToPackets(PacketType type, byte[] data)
        {
            var packet = new Packet(type, data);
            Application.Current.Dispatcher.Invoke(delegate
            {
                Packets.Add(packet);
            });

            var filterType = (PacketType)(SelectedPacketTypeIndex - 1);
            if (filterType == type || SelectedPacketTypeIndex == 0)
                if (_filterOpCode > 0)
                {
                    if (_filterOpCode == packet.OpCode)
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            FilteredPackets.Add(packet);
                        });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        FilteredPackets.Add(packet);
                    });
                }
        }
        public ICommand FilterCommand => new Command(_ => Filter());
        public ICommand IPCommand => new Command(_ => IPConnect());
        private void IPConnect()
        {
            if (_connected)
                return;

            Packets = new ObservableCollection<Packet>();
            FilteredPackets = new ObservableCollection<Packet>();

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject())) {
                if (_window.TextClientPath.Text.Length == 0) {
                    MessageBox.Show("You need to select a 'package.json' file to connect.", "Invalid file", MessageBoxButton.OK);
                    return;
                }

                if (!Directory.Exists(_window.TextClientPath.Text)) {
                    MessageBox.Show("You have selected a invalid 'package.json' file.", "Invalid file", MessageBoxButton.OK);
                    return;
                }

                try
                {
                    _client = new Client(_window.TextClientPath.Text);
                } catch {
                    MessageBox.Show("You have selected a invalid 'package.json' file.", "Invalid file", MessageBoxButton.OK);
                    return;
                }

                _client.Logger.Level = Logger.LogLevel.Error;
                _client.Logger.Output = Logger.LogOutput.Console;

                _client.Connection.OnReceivedClientMessage += Proxy_OnReceivedClientMessage;
                _client.Connection.OnReceivedServerMessage += Proxy_OnReceivedServerMessage;

                _client.Connection.OnReceivedServerFullMapPacket += Proxy_OnReceivedFullMapPacket;
                _client.Connection.OnReceivedServerDeleteOnMapPacket += Proxy_OnReceivedDeleteOnMapPacket;
                _client.Connection.OnReceivedServerCreateOnMapPacket += Proxy_OnReceivedCreateOnMapPacket;
                _client.Connection.OnReceivedServerChangeOnMapPacket += Proxy_OnReceivedChangeOnMapPacket;

                _client.Connection.OnReceivedServerBottomFloorPacket += Proxy_OnReceivedBottomFloorMapPacket;
                _client.Connection.OnReceivedServerBottomRowPacket += Proxy_OnReceivedBottomRowMapPacket;

                _client.Connection.OnReceivedServerTopFloorPacket += Proxy_OnReceivedTopFloorMapPacket;
                _client.Connection.OnReceivedServerTopRowPacket += Proxy_OnReceivedTopRowMapPacket;

                _client.Connection.OnReceivedServerLeftColumnPacket += Proxy_OnReceivedLeftColumnMapPacket;
                _client.Connection.OnReceivedServerRightColumnPacket += Proxy_OnReceivedRightColumnMapPacket;

                _client.Connection.IsClientPacketParsingEnabled = false;
                _client.Connection.IsServerPacketParsingEnabled = true;

                var uri = _window.TextIP.Text;
                var port = 7171;

                if (uri.Contains("www.tibia.com"))
                    uri = string.Empty;

                try {
                    if (_window.TextPort.Text.Length > 0)
                        port = int.Parse(_window.TextPort.Text);
                } catch {
                    MessageBox.Show("You have inserted a invalid port, using default 7171 instead.", "[Error::Sniffer]", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                bool rt = _client.StartConnection(port, uri);

                if (!rt) {
                    MessageBox.Show("There was an error when trying to connect to the host '" + (uri.Length > 0 ? uri : "official server") + "' on port '" + port + "'.", "[Error::Sniffer]", MessageBoxButton.OK);
                } else {
                    _connected = true;
                    AppendStatus("Connected");
                    Console.WriteLine("> Client version: " + _client.Version);
                }
            }
        }
        private static void AppendStatus(string value)
        {
            _window.StatusText.Content = value;
        }
        private bool Proxy_OnReceivedFullMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            FullMap fullMap = packet as FullMap;
            if (fullMap == null)
                return false;

            if (fullMap.PacketType != ServerPacketType.FullMap)
                return false;

            _ = _tracker.AppendFullMapToBuffer(fullMap.Fields);

            return true;
        }
        private bool Proxy_OnReceivedCreateOnMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            CreateOnMap createOnMap = packet as CreateOnMap;
            if (createOnMap == null)
                return false;

            if (createOnMap.PacketType != ServerPacketType.CreateOnMap)
                return false;

            _ = _tracker.AppendNewItemOnMapToBuffer(createOnMap.Position, createOnMap.StackPosition, createOnMap.ObjectInstance);

            return true;
        }
        private bool Proxy_OnReceivedDeleteOnMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            DeleteOnMap deleteOnMap = packet as DeleteOnMap;
            if (deleteOnMap == null)
                return false;

            if (deleteOnMap.PacketType != ServerPacketType.DeleteOnMap)
                return false;

            // I'm not dealing with creature death or teleport here
            if (deleteOnMap.CreatureId > 0)
                return true;

            _ = _tracker.AppendRemoveItemOnMapToBuffer(deleteOnMap.Position, deleteOnMap.StackPosition);

            return true;
        }
        private bool Proxy_OnReceivedChangeOnMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            ChangeOnMap changeOnMap = packet as ChangeOnMap;
            if (changeOnMap == null)
                return false;

            if (changeOnMap.PacketType != ServerPacketType.ChangeOnMap)
                return false;

            // I'm not dealing with creature update, transform or outfit change
            if (changeOnMap.Id >= 97 && changeOnMap.Id <= 99)
                return true;

            _ = _tracker.AppendChangeOnMapToBuffer(changeOnMap.Position, changeOnMap.StackPosition, changeOnMap.ObjectInstance);

            return true;
        }
        private bool Proxy_OnReceivedBottomFloorMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            BottomFloor bottomFloor = packet as BottomFloor;
            if (bottomFloor == null)
                return false;

            if (bottomFloor.PacketType != ServerPacketType.BottomFloor)
                return false;

            _ = _tracker.AppendFullMapToBuffer(bottomFloor.Fields);

            return true;
        }
        private bool Proxy_OnReceivedBottomRowMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            BottomRow bottomRow = packet as BottomRow;
            if (bottomRow == null)
                return false;

            if (bottomRow.PacketType != ServerPacketType.BottomRow)
                return false;

            _ = _tracker.AppendFullMapToBuffer(bottomRow.Fields);

            return true;
        }
        private bool Proxy_OnReceivedTopFloorMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            TopFloor topFloor = packet as TopFloor;
            if (topFloor == null)
                return false;

            if (topFloor.PacketType != ServerPacketType.TopFloor)
                return false;

            _ = _tracker.AppendFullMapToBuffer(topFloor.Fields);

            return true;
        }
        private bool Proxy_OnReceivedTopRowMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            TopRow topRow = packet as TopRow;
            if (topRow == null)
                return false;

            if (topRow.PacketType != ServerPacketType.TopRow)
                return false;

            _ = _tracker.AppendFullMapToBuffer(topRow.Fields);

            return true;
        }
        private bool Proxy_OnReceivedLeftColumnMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            LeftColumn leftColumn = packet as LeftColumn;
            if (leftColumn == null)
                return false;

            if (leftColumn.PacketType != ServerPacketType.LeftColumn)
                return false;

            _ = _tracker.AppendFullMapToBuffer(leftColumn.Fields);

            return true;
        }
        private bool Proxy_OnReceivedRightColumnMapPacket(PacketApi packet)
        {
            if (!_tracker.isListening())
                return true;

            RightColumn rightColumn = packet as RightColumn;
            if (rightColumn == null)
                return false;

            if (rightColumn.PacketType != ServerPacketType.RightColumn)
                return false;

            _ = _tracker.AppendFullMapToBuffer(rightColumn.Fields);

            return true;
        }
        private void Filter()
        {
            if (Packets == null || Packets.Count < 1)
                return;

            //Filter Type
            IEnumerable<Packet> filteredList = null;
            FilteredPackets.Clear();
            if (SelectedPacketTypeIndex == 1)
                filteredList = from packet in Packets where packet.Type == PacketType.Client select packet;
            else if (SelectedPacketTypeIndex == 2)
                filteredList = from packet in Packets where packet.Type == PacketType.Server select packet;
            else
                filteredList = from packet in Packets select packet;

            //Filter OpCode
            if (_filterOpCode > 0)
                filteredList = from packet in filteredList where packet.OpCode == _filterOpCode select packet;
            
            FilteredPackets = new ObservableCollection<Packet>(filteredList);
        }
        public ICommand ClearCommand => new Command(_ => Clear());
        private void Clear()
        {
            Packets.Clear();
            FilteredPackets.Clear();
        }
        public ICommand RemoveCommand => new Command(_ => Remove());
        private void Remove()
        {
            if(Packets.Contains(SelectedPacket))
                Packets.Remove(SelectedPacket);
            if (FilteredPackets.Contains(SelectedPacket))
                FilteredPackets.Remove(SelectedPacket);
        }
        public ICommand TrackerCommand => new Command(_ => ToggleTracker());
        private void ToggleTracker()
        {
            if (_tracker.isListening())
                _tracker.StopListening();
            else
                _ = _tracker.StartListening();

            _window.TrackerToggle.Content = _tracker.isListening() ? "Stop" : "Start";
            _window.TrackerStatusText.Content = _tracker.isListening() ? "Tracking" : "Not tracking";
        }
        public ICommand OpenFile => new Command(_ => OpenPackageFile());
        private void OpenPackageFile()
        {
            OpenFileDialog packageJson = new OpenFileDialog
            {
                Title = "Select your package.json file",
                Filter = "json file (*.json)|*.json*",
                FilterIndex = 1,
                Multiselect = false,
                CheckFileExists = true,
            };

            if ((bool)packageJson.ShowDialog() != true) {
                MessageBox.Show("You need to select a valid 'package.json' file to continue.", "Invalid file", MessageBoxButton.OK);
                return;
            }

            _window.TextClientPath.Text = Path.GetDirectoryName(packageJson.FileName);
            AppendStatus("Ready to connect");
        }
        public ICommand ConsoleCommand => new Command(_ => OpenConsole());
        private void OpenConsole()
        {
            AllocConsole();
            _window.ConsoleButton.IsEnabled = false;
            Console.WriteLine(">> Sniffer error log openned.");
            Console.WriteLine("> Closing it will terminate the application!!");
        }
    }
}
