// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - IPSC
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / IP Site Connect
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2024 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2025 Caleb, K4PHP
*
*/

using System.Net;
using System.Net.Sockets;
using static dvmipsc.IPSCConstants;
using fnecore;
using Serilog;

namespace dvmipsc
{
    public class IPSCMaster
    {
        public delegate void GroupVoiceReceivedHandler(string peerId, byte[] data);
        public event GroupVoiceReceivedHandler OnGroupVoiceReceived;

        private readonly UdpClient _udpServer;
        private readonly int _port;
        private readonly IPEndPoint _masterEndPoint;
        private readonly Dictionary<string, PeerInfo> _peers;
        private readonly object _lock = new();

        public IPSCMaster(int port)
        {
            _port = port;
            _udpServer = new UdpClient(_port);
            _masterEndPoint = new IPEndPoint(IPAddress.Any, _port);
            _peers = new Dictionary<string, PeerInfo>();
        }

        public async Task StartAsync()
        {
            Log.Logger.Information($"(IPSC) Master started on port {_port}...");
            while (true)
            {
                UdpReceiveResult result = await _udpServer.ReceiveAsync();
                ProcessPacket(result.Buffer, result.RemoteEndPoint);
            }
        }

        private void ProcessPacket(byte[] data, IPEndPoint sender)
        {
            if (data.Length < 2)
            {
                Log.Logger.Error($"[{sender}] Invalid packet received.");
                return;
            }

            IPSCMessageType packetType = (IPSCMessageType)data[0];
            string peerId = BitConverter.ToString(data, 1, 4);

            //Log.Logger.Debug($"[{sender}] Packet received: {packetType} from Peer {peerId}");

            switch (packetType)
            {
                case IPSCMessageType.MASTER_REG_REQ:
                    HandleRegistration(peerId, sender, data);
                    break;

                case IPSCMessageType.MASTER_ALIVE_REQ:
                    HandleMasterAliveRequest(peerId, sender);
                    break;

                case IPSCMessageType.PEER_ALIVE_REQ:
                    HandleKeepAlive(peerId, sender);
                    break;

                case IPSCMessageType.PEER_LIST_REQ:
                    SendPeerList(peerId, sender);
                    break;

                case IPSCMessageType.GROUP_DATA:
                    //Console.WriteLine(FneUtils.HexDump(data));
                    break;

                case IPSCMessageType.GROUP_VOICE:
                    HandleGroupVoice(peerId, sender, data);
                    break;

                case IPSCMessageType.XCMP_XNL:
                    //HandleXnl(peerId, sender, data);
                    break;

                default:
                    // Console.WriteLine(FneUtils.HexDump(data));
                    Log.Logger.Warning($"[{sender}] Unknown message type: {packetType}");
                    break;
            }
        }

        private void HandleMasterAliveRequest(string peerId, IPEndPoint sender)
        {
            lock (_lock)
            {
                if (_peers.ContainsKey(peerId))
                {
                    _peers[peerId].LastSeen = DateTime.UtcNow;
                    Log.Logger.Information($"[{sender}] MASTER_ALIVE_REQ received from {peerId}");

                    byte[] response = CreateMasterAliveReply(peerId);
                    _udpServer.Send(response, response.Length, sender);
                    Log.Logger.Information($"[{sender}] Sent MASTER_ALIVE_REPLY to {peerId}");
                }
                else
                {
                    Log.Logger.Warning($"[{sender}] MASTER_ALIVE_REQ from unknown peer {peerId}");
                }
            }
        }

        private void HandleXnl(string peerId, IPEndPoint sender, byte[] data)
        {
            byte[] xnl = ExtractXnlData(data);

            Log.Logger.Information($"(IPSC) XNL/XCMP message received");

            //Console.WriteLine(FneUtils.HexDump(ExtractXnlData(data)));
        }

        private void HandleXcmp(string peerId, IPEndPoint sender, byte[] data)
        {
            int xcmpMessage = (int)ReadInt16(data, 14);

            Log.Logger.Information($"(IPSC) XCMP Message: {xcmpMessage}");
        }

        public static byte[] ExtractXnlData(byte[] data)
        {
            byte[] array = new byte[(int)(ReadUInt16(data, 5) + 2)];
            Buffer.BlockCopy(data, 5, array, 0, Math.Min(array.Length, data.Length - 5));
            return array;
        }

        public static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)((int)data[offset] << 8 | (int)data[offset + 1]);
        }

        public static short ReadInt16(byte[] data, int offset)
        {
            return (short)((int)data[offset] << 8 | (int)data[offset + 1]);
        }

        private void HandleGroupVoice(string peerId, IPEndPoint sender, byte[] data)
        {
            lock (_lock)
            {
                //if (_peers.ContainsKey(peerId))
                //{
                    //Log.Logger.Information($"[{sender}] GROUP_VOICE {peerId}");
                    //Console.WriteLine(BitConverter.ToString(data));

                    OnGroupVoiceReceived?.Invoke(peerId, data);
                //}
                //else
                //{
                //    Console.WriteLine($"[{sender}] GROUP_VOICE from unknown peer {peerId}");
                //}
            }
        }

        private void HandleRegistration(string peerId, IPEndPoint sender, byte[] data)
        {
            lock (_lock)
            {
                if (!_peers.ContainsKey(peerId))
                {
                    _peers[peerId] = new PeerInfo
                    {
                        IP = sender.Address.ToString(),
                        Port = sender.Port,
                        LastSeen = DateTime.UtcNow
                    };

                    Console.WriteLine($"[{sender}] Peer Registered: {peerId}");
                }

                byte[] response = CreateMasterRegReply(peerId);
                //Console.WriteLine(FneUtils.HexDump(response));
                _udpServer.Send(response, response.Length, sender);
                Log.Logger.Information($"[{sender}] Sent MASTER_REG_REPLY to {peerId}");
            }
        }

        private void HandleKeepAlive(string peerId, IPEndPoint sender)
        {
            lock (_lock)
            {
                if (_peers.ContainsKey(peerId))
                {
                    _peers[peerId].LastSeen = DateTime.UtcNow;
                    Log.Logger.Information($"[{sender}] Keep-Alive received from {peerId}");

                    byte[] response = CreateKeepAliveReply(peerId);
                    _udpServer.Send(response, response.Length, sender);
                    Log.Logger.Information($"[{sender}] Sent MASTER_ALIVE_REPLY to {peerId}");
                }
                else
                {
                    Log.Logger.Warning($"[{sender}] Keep-Alive from unknown peer {peerId}");
                }
            }
        }

        private void SendPeerList(string peerId, IPEndPoint sender)
        {
            lock (_lock)
            {
                if (!_peers.ContainsKey(peerId))
                {
                    Log.Logger.Warning($"[{sender}] Peer List Request from *UNREGISTERED* peer {peerId}");
                    return;
                }

                List<byte> packet = new List<byte>
                {
                    (byte)IPSCMessageType.PEER_LIST_REPLY
                };

                byte[] peerIdBytes = peerId.Split('-')
                                           .Select(x => Convert.ToByte(x, 16))
                                           .ToArray();
                packet.AddRange(new List<byte> { 0x00, 0x00, 0x00, 0x0E });

                ushort peerListLength = (ushort)(_peers.Count * 11);
                byte[] peerListLengthBytes = BitConverter.GetBytes(peerListLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(peerListLengthBytes);
                packet.AddRange(peerListLengthBytes);

                foreach (var peer in _peers)
                {
                    byte[] peerIdRaw = peer.Key.Split('-')
                                               .Select(x => Convert.ToByte(x, 16))
                                               .ToArray();

                    IPAddress ipAddress = IPAddress.Parse(peer.Value.IP);
                    byte[] peerIpBytes = ipAddress.GetAddressBytes();

                    byte[] peerPortBytes = BitConverter.GetBytes((ushort)peer.Value.Port);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(peerPortBytes);

                    byte peerLinking = 0x04;

                    packet.AddRange(peerIdRaw);
                    packet.AddRange(peerIpBytes);
                    packet.AddRange(peerPortBytes);
                    packet.Add(peerLinking);
                }

                _udpServer.Send(packet.ToArray(), packet.Count, sender);
                Log.Logger.Information($"[{sender}] Sent PEER_LIST_REPLY to {peerId}");
            }
        }

        private byte[] CreateMasterAliveReply(string peerId)
        {
            List<byte> packet = new List<byte>
            {
                (byte)IPSCMessageType.MASTER_ALIVE_REPLY
            };
            packet.AddRange(StringToByteArray(peerId));

            return packet.ToArray();
        }

        public static byte[] StringToByteArray(string hexString)
        {
            return hexString
                .Split('-')
                .Select(s => Convert.ToByte(s, 16))
                .ToArray();
        }

        private byte[] CreateMasterRegReply(string peerId)
        {
            List<byte> packet = new List<byte>
            {
                (byte)IPSCMessageType.MASTER_REG_REPLY
            };

            byte[] peerIdBytes = StringToByteArray(peerId);
            packet.AddRange(new List<byte> { 0x00, 0x00, 0x00, 0x0E});

            packet.AddRange(new List<byte> { 0x6A });
            packet.AddRange(new List<byte> { 0x00, 0x00, 0x80, 0x4D });
            packet.AddRange(new List<byte> { 0x00, 0x02 });
            packet.AddRange(new List<byte> { 0x04, 0x07, 0x04, 0x00 });

            //Log.Logger.Debug(FneUtils.HexDump(packet.ToArray()));

            return packet.ToArray();
        }

        public void SendGroupVoice(byte[] data)
        {
            List<byte> packet = new List<byte>
            {
                (byte)IPSCMessageType.GROUP_VOICE
            };

            packet.AddRange(data);

            _udpServer.Send(packet.ToArray(), packet.Count, _masterEndPoint);
        }

        private byte[] CreateKeepAliveReply(string peerId)
        {
            List<byte> packet = new List<byte>
            {
                (byte)IPSCMessageType.MASTER_ALIVE_REPLY
            };
            packet.AddRange(StringToByteArray(peerId));
            return packet.ToArray();
        }

        public void CheckPeerTimeouts()
        {
            while (true)
            {
                Thread.Sleep(30000);
                lock (_lock)
                {
                    DateTime now = DateTime.UtcNow;
                    var expiredPeers = _peers
                        .Where(p => (now - p.Value.LastSeen).TotalSeconds > 120)
                        .Select(p => p.Key)
                        .ToList();

                    foreach (var peerId in expiredPeers)
                    {
                        _peers.Remove(peerId);
                        Log.Logger.Information($"Peer {peerId} removed due to timeout.");
                    }
                }
            }
        }

        private class PeerInfo
        {
            public string IP { get; set; }
            public int Port { get; set; }
            public DateTime LastSeen { get; set; }
        }
    }
}
