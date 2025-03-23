﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace dvmipsc
{
    public class Packet
    {
        protected PacketType type;
        protected RadioID id;
        protected Byte[] data;

        public Packet(PacketType type)
        {
            this.type = type;
            this.data = new byte[0];
        }

        public Packet(Byte[] data)
        {
            this.type = (PacketType)data[0];
            this.id = new RadioID(data, 1);
            this.data = data.Skip(5).ToArray();
        }

        public virtual byte[] Encode()
        {
            byte[] res = new byte[5 + this.data.Length];
            res[0] = (byte)this.type;
            this.id.AddToArray(res, 1, 4);
            Array.Copy(this.data, 0, res, 5, this.data.Length);
            return res;
        }

        protected virtual string DataString()
        {
            return "[" + string.Join(",", this.data) + "]";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            if (Enum.IsDefined(typeof(PacketType), this.type))
            {
                sb.AppendFormat(": {{PacketType: {0}, ID: {1}, Data: ", this.type, this.id.ToString());
            }
            else
            {
                sb.AppendFormat(": {{PacketType: {0}, ID: {1}, Data: ", Enum.Format(typeof(PacketType), this.type, "x"), this.id.ToString());
            }
            sb.Append(this.DataString());
            sb.Append("}");
            return sb.ToString();
        }

        public PacketType PacketType
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
            }
        }

        public RadioID ID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }
    }
}
