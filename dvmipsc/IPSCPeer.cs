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

using System.Net.Sockets;
using System.Net;
using fnecore;
using Serilog;
using static dvmipsc.IPSCConstants;

namespace dvmipsc
{
    public class IPSCPeer
    {
        public delegate void GroupVoiceReceivedHandler(string peerId, byte[] data);
        public event GroupVoiceReceivedHandler OnGroupVoiceReceived;

        private readonly UdpClient _udpClient;
        private readonly int _localPort;
        private readonly IPEndPoint _masterEndPoint;
        private readonly string _peerId;
        private bool _registered = false;
        private readonly object _lock = new();

        public IPSCPeer(string masterIp, int masterPort, int localPort, string peerId)
        {
            _localPort = localPort;
            _peerId = peerId;
            _udpClient = new UdpClient(_localPort);
            _masterEndPoint = new IPEndPoint(IPAddress.Parse(masterIp), masterPort);
        }

        public async Task StartAsync()
        {
            Log.Logger.Information($"(IPSC) Connecting peer to master {_masterEndPoint.Address}:{_masterEndPoint.Port} ");

            _ = Task.Run(() => RegistrationLoop());
            _ = Task.Run(() => KeepAliveLoop());

            while (true)
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync();
                ProcessPacket(result.Buffer, result.RemoteEndPoint);
            }
        }

        private async Task RegistrationLoop()
        {
            while (!_registered)
            {
                Log.Logger.Information("(IPSC) Sending Master Registration Request");
                byte[] registrationPacket = CreateRegistrationPacket();
                await _udpClient.SendAsync(registrationPacket, registrationPacket.Length, _masterEndPoint);
                await Task.Delay(5000);
            }
        }

        private void ProcessPacket(byte[] data, IPEndPoint sender)
        {
            if (data.Length < 1)
            {
                Log.Logger.Error("(IPSC) Invalid packet received.");
                return;
            }

            IPSCMessageType packetType = (IPSCMessageType)data[0];
            //Console.WriteLine($"[{sender}] Packet received: {packetType}");

            switch (packetType)
            {
                case IPSCMessageType.MASTER_REG_REPLY:
                    HandleRegistrationReply(data);
                    break;

                case IPSCMessageType.MASTER_ALIVE_REPLY:
                    Log.Logger.Information("(IPSC) Keep Alive Response");
                    break;

                case IPSCMessageType.PEER_REG_REQ:
                    HandlePeerRegReq(data);
                    break;

                case IPSCMessageType.GROUP_VOICE:
                    HandleGroupVoice(data);
                    break;

                case IPSCMessageType.PEER_LIST_REPLY:
                    Log.Logger.Information("(IPSC) Peer List Updated");
                    break;

                case IPSCMessageType.RPT_WAKE_UP:
                    Log.Logger.Information("(IPSC) Repeater Wake Up");
                    break;

                default:
                    Log.Logger.Warning($"Unknown message type: {packetType}");
                    break;
            }
        }

        private void HandlePeerRegReq(byte[] data)
        {
            lock (_lock)
            {
                List<byte> packet = new List<byte>
                {
                    (byte)IPSCMessageType.PEER_REG_REPLY
                };
                packet.AddRange(new List<byte> { 0x00, 0x00, 0x00, 0x0e });
                //packet.AddRange(new List<byte> { 0x6A });
                //packet.AddRange(new List<byte> { 0x00, 0x00, 0xa0, 0x2C });
                packet.AddRange(new List<byte> { 0x04, 0x08, 0x04, 0x01 });

                _udpClient.SendAsync(packet.ToArray(), packet.ToArray().Length, _masterEndPoint);

                //Console.WriteLine("Peer reg reply");
            }
        }

        private void HandleRegistrationReply(byte[] data)
        {
            lock (_lock)
            {
                _registered = true;
                Console.WriteLine(FneUtils.HexDump(data));
                Log.Logger.Information("(IPSC) Successfully registered with Master");
                byte[] keepAlivePacket = CreatePeerListReqPacket();
                _udpClient.SendAsync(keepAlivePacket, keepAlivePacket.Length, _masterEndPoint);
            }
        }

        private void HandleGroupVoice(byte[] data)
        {
            OnGroupVoiceReceived?.Invoke("1", data);

            //Console.WriteLine("Received Group Voice Data: " + BitConverter.ToString(data));
        }

        private async Task KeepAliveLoop()
        {
            while (true)
            {
                await Task.Delay(10000);
                if (_registered)
                {
                    byte[] keepAlivePacket = CreateKeepAlivePacket();
                    await _udpClient.SendAsync(keepAlivePacket, keepAlivePacket.Length, _masterEndPoint);
                    Log.Logger.Information("(IPSC) Keep Alive Request");
                }
            }
        }

        private byte[] CreateRegistrationPacket()
        {
            List<byte> packet = new List<byte>
            {
                (byte)IPSCMessageType.MASTER_REG_REQ
            };
            packet.AddRange(new List<byte> { 0x00, 0x00, 0x00, 0x0e });

            packet.AddRange(new List<byte> { 0x6A });
            packet.AddRange(new List<byte> { 0x00, 0x00, 0xa0, 0x2C });
            packet.AddRange(new List<byte> { 0x04, 0x08, 0x04, 0x01 });

            return packet.ToArray();
        }

        private byte[] CreatePeerListReqPacket()
        {
            List<byte> packet = new List<byte>
            {
                (byte)IPSCMessageType.PEER_LIST_REQ
            };
            packet.AddRange(new List<byte> { 0x00, 0x00, 0x00, 0x0e });
            packet.AddRange(new List<byte> { 0x6A });
            packet.AddRange(new List<byte> { 0x00, 0x00, 0xa0, 0x2C });
            packet.AddRange(new List<byte> { 0x04, 0x08, 0x04, 0x01 });
            return packet.ToArray();
        }

        private byte[] CreateKeepAlivePacket()
        {
            List<byte> packet = new List<byte>
            {
                (byte)IPSCMessageType.MASTER_ALIVE_REQ
            };
            packet.AddRange(new List<byte> { 0x00, 0x00, 0x00, 0x0e });
            packet.AddRange(new List<byte> { 0x6A });
            packet.AddRange(new List<byte> { 0x00, 0x00, 0xa0, 0x2C });
            packet.AddRange(new List<byte> { 0x04, 0x08, 0x04, 0x01 });
            return packet.ToArray();
        }
    }
}
