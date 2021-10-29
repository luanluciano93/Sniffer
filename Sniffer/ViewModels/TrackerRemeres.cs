using OXGaming.TibiaAPI.Utilities;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using OXGaming.TibiaAPI.Appearances;

namespace TrackerConnection
{
    public class TrackerMessage
    {
        private static Socket server;

        private static bool persistSender = true;

        private static List<byte[]> buffer = new List<byte[]>();

        private static int intervalSender = 100; // Milliseconds

        private static int trackerPort = 8119;

        private static bool isConnected = false;
        enum TrackerMessageHeader
        {
            FullMap = 0x03,
            CreateOnMap = 0x04,
            DeleteOnMap = 0x05,
            ChangeOnMap = 0x06
        }
        public TrackerMessage()
        {
        }

        public async Task StartListening()
        {
            if (isConnected)
                return;

            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, trackerPort);

            server = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try {
                server.Connect(localEndPoint);
            } catch {
                MessageBox.Show("Failed to connect to remeres, are you sure its online?", "[Error::Tracker]", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!server.Connected) {
                MessageBox.Show("Failed to connect to remeres, are you sure its online?", "[Error::Tracker]", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            buffer.Clear();
            isConnected = true;
            while (persistSender) {
                await Task.Delay(intervalSender);
                if (!server.Connected) {
                    MessageBox.Show("Tracker has lost connection to remeres.", "[Error::Tracker]", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }

                if (buffer.Count == 0)
                    continue;

                try {
                    if (buffer[0].Length != 0)
                        server.Send(buffer[0], 0, buffer[0].Length, 0);

                    buffer.RemoveAt(0);
                } catch {
                    MessageBox.Show("Failed to send map data to remeres.", "[Error::Tracker]", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }

            }

            server.Disconnect(false);
            server.Dispose();
            isConnected = false;
            MessageBox.Show("Tracker is now disconnected and is no longer tracking map!", "Tracker information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public bool isListening()
        {
            return isConnected && persistSender;
        }
        public void StopListening()
        {
            persistSender = false;
        }
        public async Task AppendFullMapToBuffer(List<(int jumps, List<ObjectInstance> objects, Position position)> fields)
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms)) {
                // 4 bytes header. We are dealing with it later
                bw.Write((uint)0);

                // Sending full map data
                bw.Write((byte)TrackerMessageHeader.FullMap);

                // Amount of position we received
                bw.Write((ushort)fields.Count);

                int messageSize = 3;
                foreach (var field in fields) {
                    // Position on map
                    bw.Write((ushort)field.position.X);
                    bw.Write((ushort)field.position.Y);
                    bw.Write((byte)field.position.Z);

                    // Amount of position we received
                    bw.Write((byte)field.objects.Count);

                    messageSize += 6;
                    field.objects.Reverse();
                    foreach (var obj in field.objects) {
                        messageSize += 1;
                        // Writing a bool value, if true then its a creature, otherwhise it's a item.
                        if (obj.Id >= 97 && obj.Id <= 99) {
                            // It's a creature, then bool is true
                            bw.Write((byte)1);

                            // Creature name
                            //bw.Write(obj.Type.Name);
                        } else {
                            // It's a item, then bool is false
                            bw.Write((byte)0);

                            // Item data
                            bw.Write((ushort)obj.Id); // ClientID
                            bw.Write((ushort)obj.Data); // SubType
                            messageSize += 4;
                        }
                    }
                }

                // Ddealing with the message header now
                bw.Seek(0, SeekOrigin.Begin);
                bw.Write((uint)messageSize);
            }

            byte[] byteData = ms.ToArray();
            ms.Dispose();
            buffer.Add(byteData);
            await Task.CompletedTask;
        }
        public async Task AppendNewItemOnMapToBuffer(Position location, int stackPos, ObjectInstance obj)
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // 4 bytes header. We are dealing with it later
                bw.Write((uint)0);

                // Sending full map data
                bw.Write((byte)TrackerMessageHeader.CreateOnMap);

                // Position on map
                bw.Write((ushort)location.X);
                bw.Write((ushort)location.Y);
                bw.Write((byte)location.Z);

                // Stack position on the specific tile
                bw.Write((byte)stackPos);

                int messageSize = 8;
                if (obj.Id >= 97 && obj.Id <= 99) {
                    // It's a creature, then bool is true
                    bw.Write((byte)1);

                    // Creature name
                    //bw.Write(obj.Type.Name);
                } else {
                    // It's a item, then bool is false
                    bw.Write((byte)0);

                    // Item data
                    bw.Write((ushort)obj.Id); // ClientID
                    bw.Write((ushort)obj.Data); // SubType
                    messageSize += 4;
                }

                // Ddealing with the message header now
                bw.Seek(0, SeekOrigin.Begin);
                bw.Write((uint)messageSize);
            }

            byte[] byteData = ms.ToArray();
            ms.Dispose();
            buffer.Add(byteData);
            await Task.CompletedTask;
        }
        public async Task AppendRemoveItemOnMapToBuffer(Position location, int stackPos)
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // 4 bytes header
                bw.Write((uint)7);

                // Sending full map data
                bw.Write((byte)TrackerMessageHeader.DeleteOnMap);

                // Position on map
                bw.Write((ushort)location.X);
                bw.Write((ushort)location.Y);
                bw.Write((byte)location.Z);

                // Stack position on the specific tile
                bw.Write((byte)stackPos);
            }

            byte[] byteData = ms.ToArray();
            ms.Dispose();
            buffer.Add(byteData);
            await Task.CompletedTask;
        }
        public async Task AppendChangeOnMapToBuffer(Position location, int stackPos, ObjectInstance obj)
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // 4 bytes header
                bw.Write((uint)11);

                // Sending full map data
                bw.Write((byte)TrackerMessageHeader.ChangeOnMap);

                // Position on map
                bw.Write((ushort)location.X);
                bw.Write((ushort)location.Y);
                bw.Write((byte)location.Z);

                // Stack position on the specific tile
                bw.Write((byte)stackPos);

                bw.Write((ushort)obj.Id); // ClientID
                bw.Write((ushort)obj.Data); // SubType
            }

            byte[] byteData = ms.ToArray();
            ms.Dispose();
            buffer.Add(byteData);
            await Task.CompletedTask;
        }
    }
}