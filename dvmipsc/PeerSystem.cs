// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - IPSC
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / IP Site Connect
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2025 Caleb, K4PHP
*
*/

using System.Net;

using fnecore;

namespace dvmipsc
{
    /// <summary>
    /// Implements a peer FNE router system.
    /// </summary>
    public class PeerSystem : FneSystemBase
    {
        protected FnePeer peer;

        private IPSCPeer ipscPeer;
        private IPSCMaster ipscMaster;

        private static IPSCPeer ipscPeerInstance;
        private static IPSCMaster ipscMasterInstance;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerSystem"/> class.
        /// </summary>
        public PeerSystem() : base(Create(), CreateIpscPeer(), CreateIpscMaster())
        {
            this.peer = (FnePeer)fne;

            this.ipscPeer = ipscPeerInstance;
            this.ipscMaster = ipscMasterInstance;
        }

        /// <summary>
        /// Internal helper to instantiate a new instance of <see cref="FnePeer"/> class.
        /// </summary>
        /// <param name="config">Peer stanza configuration</param>
        /// <returns><see cref="FnePeer"/></returns>
        private static FnePeer Create()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Program.Configuration.Port);
            string presharedKey = Program.Configuration.Encrypted ? Program.Configuration.PresharedKey : null;

            if (Program.Configuration.Address == null)
                throw new NullReferenceException("address");
            if (Program.Configuration.Address == string.Empty)
                throw new ArgumentException("address");

            // handle using address as IP or resolving from hostname to IP
            try
            {
                endpoint = new IPEndPoint(IPAddress.Parse(Program.Configuration.Address), Program.Configuration.Port);
            }
            catch (FormatException)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Program.Configuration.Address);
                if (addresses.Length > 0)
                    endpoint = new IPEndPoint(addresses[0], Program.Configuration.Port);
            }

            FnePeer peer = new FnePeer(Program.Configuration.Name, Program.Configuration.PeerId, endpoint, presharedKey);

            // set configuration parameters
            peer.RawPacketTrace = Program.Configuration.RawPacketTrace;

            peer.PingTime = Program.Configuration.PingTime;
            peer.Passphrase = Program.Configuration.Passphrase;
            peer.Information.Details = ConfigurationObject.ConvertToDetails(Program.Configuration);

            peer.PeerConnected += Peer_PeerConnected;

            return peer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static IPSCPeer CreateIpscPeer()
        {
            if (Program.Configuration.Ipsc.Mode == IpscMode.PEER)
            {
                if (ipscPeerInstance == null)
                    ipscPeerInstance = new IPSCPeer("192.168.1.148", 50000, 50001, "123456");
                return ipscPeerInstance;
            }
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static IPSCMaster CreateIpscMaster()
        {
            if (Program.Configuration.Ipsc.Mode == IpscMode.MASTER)
            {
                if (ipscMasterInstance == null)
                    ipscMasterInstance = new IPSCMaster(Program.Configuration.Ipsc.Port);
                return ipscMasterInstance;
            }
            else
                return null;
        }

        /// <summary>
        /// Event action that handles when a peer connects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void Peer_PeerConnected(object sender, PeerConnectedEvent e)
        {
            // fake a group affiliation
            FnePeer peer = (FnePeer)sender;
            peer.SendMasterGroupAffiliation(1, (uint)Program.Configuration.DestinationId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task StartIPSC()
        {
            if (Program.Configuration.Ipsc.Mode == IpscMode.PEER)
                await ipscPeer.StartAsync();

            if (Program.Configuration.Ipsc.Mode == IpscMode.MASTER)
                await ipscMaster.StartAsync();
        }

        /// <summary>
        /// Helper to send a activity transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendActivityTransfer(string message)
        {
            /* stub */
        }

        /// <summary>
        /// Helper to send a diagnostics transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendDiagnosticsTransfer(string message)
        {
            /* stub */
        }
    } // public class PeerSystem
} // namespace dvmbridge