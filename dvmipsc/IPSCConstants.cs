// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - IPSC
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / IP Site Connect
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2025 Caleb, K4PHP
*
*/

namespace dvmipsc
{
    public static class IPSCConstants
    {
        // Peer Status Bit Masks
        public const byte PEER_OP_MSK = 0b01000000;
        public const byte PEER_MODE_MSK = 0b00110000;
        public const byte PEER_MODE_ANALOG = 0b00010000;
        public const byte PEER_MODE_DIGITAL = 0b00100000;
        public const byte IPSC_TS1_MSK = 0b00001100;
        public const byte IPSC_TS2_MSK = 0b00000011;

        // Service Flags
        public const byte CSBK_MSK = 0b10000000;
        public const byte RPT_MON_MSK = 0b01000000;
        public const byte CON_APP_MSK = 0b00100000;
        public const byte XNL_STAT_MSK = 0b10000000;
        public const byte XNL_MSTR_MSK = 0b01000000;
        public const byte XNL_SLAVE_MSK = 0b00100000;
        public const byte PKT_AUTH_MSK = 0b00010000;
        public const byte DATA_CALL_MSK = 0b00001000;
        public const byte VOICE_CALL_MSK = 0b00000100;
        public const byte MSTR_PEER_MSK = 0b00000001;

        // Timeslot Call & Status Byte Masks
        public const byte END_MSK = 0b01000000;
        public const byte TS_CALL_MSK = 0b00100000;

        // RTP Header Masks
        public const byte RTP_VER_MSK = 0b11000000;
        public const byte RTP_PAD_MSK = 0b00100000;
        public const byte RTP_EXT_MSK = 0b00010000;
        public const byte RTP_CSIC_MSK = 0b00001111;
        public const byte RTP_MRKR_MSK = 0b10000000;
        public const byte RTP_PAY_TYPE_MSK = 0b01111111;

        // IPSC Message Types
        public enum IPSCMessageType : byte
        {
            CALL_CONFIRMATION = 0x05,
            TXT_MESSAGE_ACK = 0x54,
            CALL_MON_STATUS = 0x61,
            CALL_MON_RPT = 0x62,
            CALL_MON_NACK = 0x63,
            XCMP_XNL = 0x70,
            GROUP_VOICE = 0x80,
            PVT_VOICE = 0x81,
            GROUP_DATA = 0x83,
            PVT_DATA = 0x84,
            RPT_WAKE_UP = 0x85,
            UNKNOWN_COLLISION = 0x86,
            MASTER_REG_REQ = 0x90,
            MASTER_REG_REPLY = 0x91,
            PEER_LIST_REQ = 0x92,
            PEER_LIST_REPLY = 0x93,
            PEER_REG_REQ = 0x94,
            PEER_REG_REPLY = 0x95,
            MASTER_ALIVE_REQ = 0x96,
            MASTER_ALIVE_REPLY = 0x97,
            PEER_ALIVE_REQ = 0x98,
            PEER_ALIVE_REPLY = 0x99,
            DE_REG_REQ = 0x9A,
            DE_REG_REPLY = 0x9B
        }

        // IPSC Version Information
        public const byte IPSC_VER_17 = 0x02;
        public const byte IPSC_VER_16 = 0x01;
        public const byte LINK_TYPE_IPSC = 0x04;

        // IPSC Version and Link Type (4-byte version field in registration packets)
        public static readonly byte[] IPSC_VER = { LINK_TYPE_IPSC, IPSC_VER_17, LINK_TYPE_IPSC, IPSC_VER_16 };

        // Burst Data Types
        public enum BURST_DATA_TYPE
        {
            PI_HEADER = 0x00,
            VOICE_HEAD = 0x01,
            VOICE_TERM = 0x02,
            SLOT1_VOICE = 0x0A,
            SLOT2_VOICE = 0x8A
        };

        // Security Levels
        public enum SEC_LEVELS
        {
            NONE,
            BASIC,
            ENHANCED
        };
    }
}
