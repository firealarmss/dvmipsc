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

using fnecore;
using fnecore.DMR;
using Serilog;
using vocoder;
using static dvmipsc.IPSCConstants;

namespace dvmipsc
{
    public abstract partial class FneSystemBase : fnecore.FneSystemBase
    {
        private const int DMR_AMBE_LENGTH_BYTES = 27;
        private const int DMR_SHORT_AMBE_LENGTH_BYTES = 21;

        private EmbeddedData embeddedData;

        private int dmrSeqNo = 0;
        private byte dmrN = 0;

        /// <summary>
        /// Callback used to validate incoming DMR data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="message">Raw message data</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected override bool DMRDataValidate(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId, byte[] message)
        {
            return true;
        }

        /// <summary>
        /// Creates an DMR frame message.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="frameType"></param>
        /// <param name="n"></param>
        private void CreateDMRMessage(ref byte[] data, FrameType frameType, byte seqNo, byte n, uint srcId, uint dstId, byte slot)
        {
            RemoteCallData callData = new RemoteCallData()
            {
                SrcId = srcId,
                DstId = dstId,
                FrameType = frameType,
                Slot = slot
            };

            CreateDMRMessage(ref data, callData, seqNo, n);
        }

        /// <summary>
        /// Helper to send a DMR terminator with LC message.
        /// </summary>
        private void SendDMRTerminator(uint srcId, uint dstId, byte slot)
        {
            RemoteCallData callData = new RemoteCallData()
            {
                SrcId = srcId,
                DstId = dstId,
                FrameType = FrameType.DATA_SYNC,
                Slot = slot
            };

            SendDMRTerminator(callData, ref dmrSeqNo, ref dmrN, embeddedData);
        }

        private void SendDMRCall(byte[] pkt)
        {
            byte[] dmrpkt = null;
            byte[] dmrData = new byte[DMR_FRAME_LENGTH_BYTES];

            ushort pktSeq = 0;

            FnePeer peer = (FnePeer)fne;

            pktSeq = peer.pktSeq(true);

            uint sourceId = (uint)((pkt[6] << 16) | (pkt[7] << 8) | pkt[8]);
            uint destinationId = (uint)((pkt[9] << 16) | (pkt[10] << 8) | pkt[11]);
            int callPriority = pkt[12]; 
            int callTag = (pkt[13] << 24) | (pkt[14] << 16) | (pkt[15] << 8) | pkt[16];
            int control = pkt[17];


            int timeslot = ((control & TS_CALL_MSK) != 0 ? 1 : 0) + 1;
            bool isEnd = (control & END_MSK) != 0;

            int rtpPayload = pkt[30];
            int rtpSeq = (pkt[20] << 8) | pkt[21];

            // Console.WriteLine($"0x{rtpPayload:X2} 0x{rtpSeq:X4}");

            switch ((BURST_DATA_TYPE)rtpPayload)
            {
                case BURST_DATA_TYPE.PI_HEADER:
                    dmrN = (byte)(dmrSeqNo % 6);
                    Log.Logger.Warning("PI HEADER RECEIVED, NOT SUPPORTED!");
                    break;

                case BURST_DATA_TYPE.VOICE_HEAD:
                    dmrN = (byte)(dmrSeqNo % 6);
                    Log.Logger.Information($"(IPSC) Voice Transmission Start; slot: {timeslot} srcId: {sourceId}, dstId: {destinationId}");
                    this.status[timeslot].RxTGId = (uint)destinationId;
                    this.status[timeslot].RxSeq = (uint)rtpSeq;
                    pktSeq = peer.pktSeq(true);

                    // send DMR voice header
                    dmrData = new byte[DMR_FRAME_LENGTH_BYTES];

                    // generate DMR LC
                    LC dmrLC = new LC();
                    dmrLC.FLCO = (byte)DMRFLCO.FLCO_GROUP;
                    dmrLC.SrcId = sourceId;
                    dmrLC.DstId = destinationId;
                    embeddedData.SetLC(dmrLC);


                    // generate the Slot TYpe
                    SlotType slotType = new SlotType();
                    slotType.DataType = (byte)DMRDataType.VOICE_LC_HEADER;
                    slotType.GetData(ref dmrData);

                    FullLC.Encode(dmrLC, ref dmrData, DMRDataType.VOICE_LC_HEADER);

                    // generate DMR network frame
                    dmrpkt = new byte[DMR_PACKET_SIZE];
                    CreateDMRMessage(ref dmrpkt, FrameType.VOICE_SYNC, (byte)dmrSeqNo, 0, sourceId, destinationId, (byte)timeslot);
                    Buffer.BlockCopy(dmrData, 0, dmrpkt, 20, DMR_FRAME_LENGTH_BYTES);

                    dmrpkt[15U] = 0x21;

                    peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_DMR), dmrpkt, pktSeq, txStreamId);

                    dmrSeqNo++;
                    break;

                case BURST_DATA_TYPE.VOICE_TERM:
                    Log.Logger.Information($"(IPSC) Voice Transmission End; slot: {timeslot} srcId: {sourceId}, dstId: {destinationId}");
                    SendDMRTerminator((uint)sourceId, (uint)destinationId, (byte)timeslot);
                    break;

                case BURST_DATA_TYPE.SLOT1_VOICE:
                case BURST_DATA_TYPE.SLOT2_VOICE:
                    try
                    {
                        Console.WriteLine(FneUtils.HexDump(pkt));

                        FrameType frameType = FrameType.VOICE_SYNC;

                        List<byte[]> data = new List<byte[]>(3);
                        byte[] pload = new byte[pkt.Length - 30];

                        byte[] ambeFrame1 = new byte[7];
                        byte[] ambeFrame2 = new byte[7];
                        byte[] ambeFrame3 = new byte[7];

                        dmrpkt = new byte[DMR_PACKET_SIZE];

                        dmrN = (byte)(dmrSeqNo % 6);

                        Array.Copy(pkt, 30, pload, 0, pkt.Length - 30);

                        int bitOffset = 0;
                        int payloadIndex = 3;

                        for (int i = 0; i < 3; i++)
                        {
                            byte[] ambe49 = new byte[7];

                            int j = 0;

                            while (j < 7)
                            {
                                if (payloadIndex >= pload.Length)
                                {
                                    Log.Logger.Error($"BUG BUG: payloadIndex {payloadIndex} exceeds pload size {pload.Length}");
                                    return;
                                }

                                byte b = pload[payloadIndex];

                                if (bitOffset > 0)
                                {
                                    if (j > 0)
                                    {
                                        ambe49[j - 1] |= (byte)(b >> (8 - bitOffset));
                                    }
                                    ambe49[j] = (byte)(b << bitOffset);
                                }
                                else
                                {
                                    ambe49[j] = b;
                                }

                                j++;
                                payloadIndex++;
                            }

                            // mask out extra bits in the last byte
                            ambe49[6] &= 0x80;

                            bitOffset += 2;
                            payloadIndex--;

                            // ecc and interleave 49 bit IPSC AMBE
                            byte[] expandedFrame = new byte[9];
                            AMBEUtils.ProcessAmbe49(ambe49, out expandedFrame);

                            data.Add(expandedFrame);
                        }

                        byte[] ambeBytes = new byte[DMR_AMBE_LENGTH_BYTES];

                        for (int i = 0; i < 3; i++)
                        {
                            Buffer.BlockCopy(data[i], 0, ambeBytes, i * 9, 9);
                        }

                        Buffer.BlockCopy(ambeBytes, 0, dmrData, 0, 13);
                        dmrData[13U] = (byte)(ambeBytes[13U] & 0xF0);
                        dmrData[19U] = (byte)(ambeBytes[13U] & 0x0F);
                        Buffer.BlockCopy(ambeBytes, 14, dmrData, 20, 13);

                        //Buffer.BlockCopy(ambeBytes, 0, dmrData, 2, 13);

                        if (dmrN == 0)
                        {
                            frameType = FrameType.VOICE_SYNC;
                        }
                        else
                        {
                            frameType = FrameType.VOICE;

                            byte lcss = embeddedData.GetData(ref dmrData, dmrN);

                            // generated embedded signalling
                            EMB emb = new EMB();
                            emb.ColorCode = 0;
                            emb.LCSS = lcss;
                            emb.Encode(ref dmrData);
                        }

                        CreateDMRMessage(ref dmrpkt, frameType, (byte)dmrSeqNo, dmrN, sourceId, destinationId, (byte)timeslot);

                        Buffer.BlockCopy(dmrData, 0, dmrpkt, 20, DMR_FRAME_LENGTH_BYTES);

                        peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_DMR), dmrpkt, pktSeq, txStreamId);

                        Log.Logger.Information($"(IPSC) DMRD: Traffic *VOICE FRAME    * PEER {fne.PeerId} SRC_ID {sourceId} TGID {destinationId} TS {timeslot} VC{dmrN} [STREAM ID {txStreamId}]");

                        //Console.WriteLine(FneUtils.HexDump(dmrpkt));
                        dmrSeqNo++;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex.Message);
                    }
                    break;

                default:
                    Log.Logger.Error($"Unknown RTP Payload Type: 0x{rtpPayload:X2}");
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="e"></param>
        private void DMRDecodeAudioFrame(byte[] data, DMRDataReceivedEvent e)
        {
            // Console.WriteLine(FneUtils.HexDump(data));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codewordBits"></param>
        /// <param name="codeword"></param>
        /// <param name="lengthBytes"></param>
        /// <param name="lengthBits"></param>
        private void packBitsToBytes(byte[] codewordBits, out byte[] codeword, int lengthBytes, int lengthBits)
        {
            codeword = new byte[lengthBytes];

            int processed = 0, bitPtr = 0, bytePtr = 0;
            for (int i = 0; i < lengthBytes; i++)
            {
                codeword[i] = 0;
                for (int j = 7; -1 < j; j--)
                {
                    if (processed < lengthBits)
                    {
                        codeword[bytePtr] = (byte)(codeword[bytePtr] | (byte)((codewordBits[bitPtr] & 1) << ((byte)j & 0x1F)));
                        bitPtr++;
                    }

                    processed++;
                }

                bytePtr++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="in72"></param>
        /// <param name="out49"></param>
        private void to49(byte[] in72, out byte[] out49)
        {
            byte[] bits49 = new byte[49];

            AMBEUtils.ProcessAmbe72(in72, out bits49);

            packBitsToBytes(bits49, out out49, 7, 49);
        }

        /// <summary>
        /// Event handler used to process incoming DMR data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void DMRDataReceived(object sender, DMRDataReceivedEvent e)
        {
            DateTime pktTime = DateTime.Now;

            byte[] data = new byte[DMR_FRAME_LENGTH_BYTES];
            Buffer.BlockCopy(e.Data, 20, data, 0, DMR_FRAME_LENGTH_BYTES);
            byte bits = e.Data[15];

            if (e.CallType == CallType.GROUP)
            {
                //Console.WriteLine(FneUtils.HexDump(data));

                if (e.SrcId == 0)
                {
                    Log.Logger.Warning($"({SystemName}) DMRD: Received call from SRC_ID {e.SrcId}? Dropping call data.");
                    return;
                }

                if (e.StreamId != status[e.Slot].RxStreamId)
                {
                    status[e.Slot].RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *CALL START     * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} [STREAM ID {e.StreamId}]");

                    // if we can, use the LC from the voice header as to keep all options intact
                    if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.VOICE_LC_HEADER))
                    {
                        LC lc = FullLC.Decode(data, DMRDataType.VOICE_LC_HEADER);
                        status[e.Slot].DMR_RxLC = lc;
                    }
                    else // if we don't have a voice header; don't wait to decode it, just make a dummy header
                        status[e.Slot].DMR_RxLC = new LC()
                        {
                            SrcId = e.SrcId,
                            DstId = e.DstId
                        };

                    status[e.Slot].DMR_RxPILC = new PrivacyLC();
                    Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] RX_LC {FneUtils.HexDump(status[e.Slot].DMR_RxLC.GetBytes())}");
                }

                // if we can, use the PI LC from the PI voice header as to keep all options intact
                if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.VOICE_PI_HEADER))
                {
                    PrivacyLC lc = FullLC.DecodePI(data);
                    status[e.Slot].DMR_RxPILC = lc;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *CALL PI PARAMS  * PEER {e.PeerId} DST_ID {e.DstId} TS {e.Slot + 1} ALGID {lc.AlgId} KID {lc.KId} [STREAM ID {e.StreamId}]");
                    Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] RX_PI_LC {FneUtils.HexDump(status[e.Slot].DMR_RxPILC.GetBytes())}");
                }

                if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.TERMINATOR_WITH_LC) && (status[e.Slot].RxType != FrameType.TERMINATOR))
                {
                    TimeSpan callDuration = pktTime - status[0].RxStart;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *CALL END       * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} DUR {callDuration} [STREAM ID {e.StreamId}]");
                }

                if (e.FrameType == FrameType.VOICE_SYNC || e.FrameType == FrameType.VOICE)
                {
                    byte[] ambe = new byte[DMR_AMBE_LENGTH_BYTES];
                    Buffer.BlockCopy(data, 0, ambe, 0, 14);
                    ambe[13] &= 0xF0;
                    ambe[13] |= (byte)(data[19] & 0x0F);
                    Buffer.BlockCopy(data, 20, ambe, 14, 13);
                    DMRDecodeAudioFrame(ambe, e);
                }

                status[e.Slot].RxRFS = e.SrcId;
                status[e.Slot].RxType = e.FrameType;
                status[e.Slot].RxTGId = e.DstId;
                status[e.Slot].RxTime = pktTime;
                status[e.Slot].RxStreamId = e.StreamId;
            }

            //ipsc.SendGroupVoice(data);
        }
    }
}
