﻿// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Audio Bridge
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Audio Bridge
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2025 Caleb, K4PHP
*
*/
using System;
using System.Collections.Generic;

using fnecore;

namespace dvmipsc
{
    public enum IpscMode
    {
        MASTER = 0x00,
        PEER = 0x01
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConfigLogObject
    {
        /// <summary>
        /// 
        /// </summary>
        public int DisplayLevel = 1;
        /// <summary>
        /// 
        /// </summary>
        public int FileLevel = 1;
        /// <summary>
        /// 
        /// </summary>
        public string FilePath = ".";
        /// <summary>
        /// 
        /// </summary>
        public string FileRoot = "dvmbridge";
    } // public class ConfigLogObject

    /// <summary>
    /// 
    /// </summary>
    public class IpscConfigObject
    {
        /// <summary>
        /// 
        /// </summary>
        public IpscMode Mode = IpscMode.MASTER;
        
        /// <summary>
        /// 
        /// </summary>
        public string Address = "127.0.0.1";
        
        /// <summary>
        /// 
        /// </summary>
        public int Port = 50000;

        /// <summary>
        /// 
        /// </summary>
        public bool LogKeepAlive = true;
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConfigurationObject
    {
        /// <summary>
        /// 
        /// </summary>
        public ConfigLogObject Log = new ConfigLogObject();

        /// <summary>
        /// 
        /// </summary>
        public IpscConfigObject Ipsc = new IpscConfigObject();

        /// <summary>
        /// Time in seconds between pings to peers.
        /// </summary>
        public int PingTime = 5;

        /// <summary>
        /// Flag indicating whether or not the router should debug display all packets received.
        /// </summary>
        public bool RawPacketTrace = false;

        /// <summary>
        /// Textual Name.
        /// </summary>
        public string Name = "BRIDGE";
        /// <summary>
        /// Network Peer ID.
        /// </summary>
        public uint PeerId;
        /// <summary>
        /// Hostname/IP address of FNE master to connect to.
        /// </summary>
        public string Address = "127.0.0.1";
        /// <summary>
        /// Port number to connect to.
        /// </summary>
        public int Port = 62031;
        /// <summary>
        /// FNE access password.
        /// </summary>
        public string Passphrase;

        /// <summary>
        /// Enable/Disable AES Wrapped UDP
        /// </summary>
        public bool Encrypted;
        /// <summary>
        /// Pre shared AES key for AES wrapped UDP
        /// </summary>
        public string PresharedKey;

        /// <summary>
        /// Source "Radio ID" for transmitted audio frames
        /// </summary>
        public int SourceId;

        /// <summary>
        /// Talkgroup ID for transmitted/received audio frames.
        /// </summary>
        public int DestinationId;

        /// <summary>
        /// Slot for received/transmitted audio frames.
        /// </summary>
        public int Slot = 1;

        /*
        ** Methods
        */

        /// <summary>
        /// Helper to convert the <see cref="ConfigPeerObject"/> to a <see cref="PeerDetails"/> object.
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public static PeerDetails ConvertToDetails(ConfigurationObject peer)
        {
            PeerDetails details = new PeerDetails();

            // identity
            details.Identity = peer.Name;
            details.RxFrequency = 0;
            details.TxFrequency = 0;

            // system info
            details.Latitude = 0.0d;
            details.Longitude = 0.0d;
            details.Height = 1;
            details.Location = "Digital Network";

            // channel data
            details.TxPower = 0;
            details.TxOffsetMhz = 0.0f;
            details.ChBandwidthKhz = 0.0f;
            details.ChannelID = 0;
            details.ChannelNo = 0;

            // RCON
            details.Password = "ABCD123";
            details.Port = 9990;

            details.Software = $"DVM_IPSC";//AssemblyVersion._VERSION;

            return details;
        }
    } // public class ConfigurationObject
} // namespace dvmbridge