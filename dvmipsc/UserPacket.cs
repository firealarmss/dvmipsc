using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace dvmipsc
{
    public class RTPData
    {
        private static UInt16 gSequenceNumber = 100;

        private readonly byte version;
        private readonly bool padding;
        private readonly bool extension;
        private readonly byte csrcCount;
        private readonly bool marker;
        //94 seems to be CSBK, 94 seems to be a call
        private readonly byte payloadType;
        private readonly UInt16 sequenceNumber;
        private readonly UInt32 timestamp;
        private readonly UInt32 ssrcId;

        public RTPData(byte PayloadType)
        {
            this.version = 2;
            this.padding = false;
            this.extension = false;
            this.csrcCount = 0;
            this.marker = false;
            this.payloadType = PayloadType;
            this.sequenceNumber = gSequenceNumber++;
            this.timestamp = (UInt32)(DateTime.Now.Ticks);
            this.ssrcId = 0;
        }

        public RTPData(byte[] data, int offset)
        {
            this.version = (byte)(data[offset] >> 6);
            this.padding = ((data[offset] & 0x20) != 0);
            this.extension = ((data[offset] & 0x10) != 0);
            this.csrcCount = (byte)(data[offset] & 0x0F);
            this.marker = ((data[offset + 1] & 0x80) != 0);
            this.payloadType = (byte)(data[offset + 1] & 0x7F);
            this.sequenceNumber = (UInt16)(data[offset + 2] << 8 | data[offset + 3]);
            this.timestamp = (UInt32)(data[offset + 4] << 24 | data[offset + 5] << 16 | data[offset + 6] << 8 | data[offset + 7]);
            this.ssrcId = (UInt32)(data[offset + 8] << 24 | data[offset + 9] << 16 | data[offset + 10] << 8 | data[offset + 11]);
        }

        public byte[] Encode()
        {
            byte[] ret = new byte[12];
            ret[0] = (byte)((this.version << 6) | this.csrcCount);
            if (this.padding)
            {
                ret[0] |= 0x20;
            }
            if (this.extension)
            {
                ret[0] |= 0x10;
            }
            ret[1] = this.payloadType;
            if (this.marker)
            {
                ret[1] |= 0x80;
            }
            ret[2] = (byte)(this.sequenceNumber >> 8);
            ret[3] = (byte)this.sequenceNumber;
            ret[4] = (byte)(this.timestamp >> 24);
            ret[5] = (byte)(this.timestamp >> 16);
            ret[6] = (byte)(this.timestamp >> 8);
            ret[7] = (byte)(this.timestamp);
            ret[8] = (byte)(this.ssrcId >> 24);
            ret[9] = (byte)(this.ssrcId >> 16);
            ret[10] = (byte)(this.ssrcId >> 8);
            ret[11] = (byte)(this.ssrcId);
            return ret;
        }

        public override string ToString()
        {
            return base.ToString() + string.Format("Version: {0}, Padding: {1}, Extension: {2}, CSRCCount {3}, Marker: {4}, PayloadType: {5}, Sequence Number: {6}, Timestamp: {7}, SSRCID: {8}", this.version, this.padding, this.Extension, this.csrcCount, this.marker, this.payloadType, this.sequenceNumber, this.timestamp, this.ssrcId);
        }

        public UInt16 SequenceNumber
        {
            get
            {
                return this.sequenceNumber;
            }
        }

        public bool Extension
        {
            get
            {
                return this.extension;
            }
        }
    }
}
