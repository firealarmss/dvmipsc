// SPDX-License-Identifier: GPL-2.0-only
/**
* Digital Voice Modem - IPSC
* GPLv2 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / IPSC
* @license GPLv2 License (https://opensource.org/licenses/GPL-2.0)
*
*   Copyright (C) 
*
*/

#include "Common.h"
#include <cstdint>
#include <vcruntime_string.h>
#include "MBEInterleaver.cpp"

using namespace System;
using namespace System::Runtime::InteropServices;

namespace vocoder
{
    public ref class AMBEUtils
    {
    public:
        static const int AMBE_CODEWORD_BITS = 49;
        static const int AMBE_INTERLEAVED_BITS = 72;

        static void ProcessAmbe49(array<Byte>^ inAmbe49, [Out] array<Byte>^% outAmbe72)
        {
            if (inAmbe49 == nullptr || inAmbe49->Length != 7)
                throw gcnew ArgumentException("Input AMBE must be 7 bytes (49 bits).");

            outAmbe72 = gcnew array<Byte>(9);
            pin_ptr<Byte> pinInAmbe49 = &inAmbe49[0];
            pin_ptr<Byte> pinOutAmbe72 = &outAmbe72[0];

            convert49BitTo72BitAMBE(pinInAmbe49, pinOutAmbe72);
        }

        static void ProcessAmbe72(array<Byte>^ inAmbe72, [Out] array<Byte>^% outAmbe49)
        {
            MBEInterleaver^ interleaver = gcnew MBEInterleaver(MBEMode::DMRAMBE);

            array<Byte>^ decodedBits;
            int decodeErrors = interleaver->decode(inAmbe72, decodedBits);

            if (decodeErrors > 0)
                throw gcnew Exception("MBE decode encountered errors");

            outAmbe49 = gcnew array<Byte>(decodedBits->Length);
            Array::Copy(decodedBits, outAmbe49, decodedBits->Length);
        }

    private:

        static void convert49BitTo72BitAMBE(uint8_t* inAmbe49, uint8_t* outAmbe72)
        {
            uint8_t ambe49bits[49];
            char ambeFrame[4][24];

            memset(ambeFrame, 0, 24 * 4);

            uint8_t tmp = 0;
            int pos = 0;
            for (int j = 0; j < 7; j++)
            {
                for (int i = 7; i > -1; i--)
                {
                    tmp = inAmbe49[pos >> 3] & (1 << i);
                    ambe49bits[pos] = tmp ? 1 : 0;
                    pos++;
                }
            }

            convert49BitAmbeTo72BitFrames(ambe49bits, (uint8_t*)&ambeFrame);
            mbe_demodulateAmbe3600x2450Data(ambeFrame);
            interleave((uint8_t*)ambeFrame, outAmbe72);
        }

        /// <summary>Calculates parity for AMBE codewords.</summary>
        static int Parity(int cw)
        {
            int p = cw & 0xFF;
            p ^= (cw >> 8) & 0xFF;
            p ^= (cw >> 16) & 0xFF;
            p ^= (p >> 4);
            p ^= (p >> 2);
            p ^= (p >> 1);
            return (p & 1);
        }

        /// <summary>Interleaves the 72-bit AMBE frame.</summary>
        static void interleave(uint8_t* ambe_fr, uint8_t* dataOut)
        {
            int bitIndex = 0;
            int	w = 0;
            int x = 0;
            int	y = 0;
            int z = 0;
            uint8_t bit0;
            uint8_t bit1;

            for (int i = 0; i < 36; i++)
            {
                bit1 = ambe_fr[rW[w] * 24 + rX[x]]; // bit 1
                bit0 = ambe_fr[rY[y] * 24 + rZ[z]]; // bit 0

                dataOut[bitIndex >> 3] = ((dataOut[bitIndex >> 3] << 1) & 0xfe) | bit1;
                bitIndex++;

                dataOut[bitIndex >> 3] = ((dataOut[bitIndex >> 3] << 1) & 0xfe) | bit0;
                bitIndex++;

                w++;
                x++;
                y++;
                z++;
            }
        }

        static int parity(int cw) {
            /* XOR the bytes of the codeword */
            int p = cw & 0xff;
            p = p ^ ((cw >> 8) & 0xff);
            p = p ^ ((cw >> 16) & 0xff);

            /* XOR the halves of the intermediate result */
            p = p ^ (p >> 4);
            p = p ^ (p >> 2);
            p = p ^ (p >> 1);

            /* return the parity result */
            return(p & 1);
        }

        /**
        * This function calculates [23,12] Golay codewords.
        * The format of the returned longint is [checkbits(11),data(12)].
        **/
        static int golay2312word(int cw) {
            int POLY = 0xAE3;            /* or use the other polynomial, 0xC75 */
            cw = cw & 0x0fff;             // Strip off check bits and only use data
            int c = cw;                      /* save original codeword */
            for (int i = 1; i < 13; i++) {
                /* examine each data bit */
                if (cw & 1) {            /* test data bit */
                    cw = cw ^ POLY;      /* XOR polynomial */
                }
                cw = cw >> 1;            /* shift intermediate result */
            }
            return((cw << 12) | c);      /* assemble codeword */
        }

        static void mbe_demodulateAmbe3600x2450Data(char ambe_fr[4][24])
        {
            int i, j, k;
            unsigned short pr[115];
            unsigned short foo = 0;

            // create pseudo-random modulator
            for (i = 23; i >= 12; i--)
            {
                foo <<= 1;
                foo |= ambe_fr[0][i];
            }
            pr[0] = (16 * foo);
            for (i = 1; i < 24; i++)
            {
                pr[i] = (173 * pr[i - 1]) + 13849 - (65536 * (((173 * pr[i - 1]) + 13849) / 65536));
            }
            for (i = 1; i < 24; i++)
            {
                pr[i] = pr[i] / 32768;
            }

            // demodulate ambe_fr with pr
            k = 1;
            for (j = 22; j >= 0; j--)
            {
                ambe_fr[1][j] = ((ambe_fr[1][j]) ^ pr[k]);
                k++;
            }
        }

        static void convert49BitAmbeTo72BitFrames(uint8_t* inAmbe49bits, uint8_t* ambe_frOut)
        {
            //Place bits into the 4x24 frames.  [bit0...bit23]
            //fr0: [P e10 e9 e8 e7 e6 e5 e4 e3 e2 e1 e0 11 10 9 8 7 6 5 4 3 2 1 0]
            //fr1: [e10 e9 e8 e7 e6 e5 e4 e3 e2 e1 e0 23 22 21 20 19 18 17 16 15 14 13 12 xx]
            //fr2: [34 33 32 31 30 29 28 27 26 25 24 x x x x x x x x x x x x x]
            //fr3: [48 47 46 45 44 43 42 41 40 39 38 37 36 35 x x x x x x x x x x]

            // ecc and copy C0: 12bits + 11ecc + 1 parity
            // First get the 12 bits that actually exist
            // Then calculate the golay codeword
            // And then add the parity bit to get the final 24 bit pattern

            int tmp = 0;

            //grab the 12 MSB
            for (int i = 11; i > -1; i--) {
                tmp = (tmp << 1) | (inAmbe49bits[i] ? 1 : 0);
            }

            tmp = golay2312word(tmp);               //Generate the 23 bit result
            int parityBit = parity(tmp);
            tmp = tmp | (parityBit << 23);           //And create a full 24 bit value

            for (int i = 23; i > -1; i--) {
                ambe_frOut[i] = (tmp & 1);
                tmp = tmp >> 1;
            }

            // C1: 12 bits + 11ecc (no parity)
            tmp = 0;
            //grab the next 12 bits
            for (int i = 23; i > 11; i--) {
                tmp = (tmp << 1) | (inAmbe49bits[i] ? 1 : 0);
            }

            tmp = golay2312word(tmp);                    //Generate the 23 bit result

            for (int j = 22; j > -1; j--) {
                ambe_frOut[1 * 24 + j] = tmp & 1;
                tmp = tmp >> 1;
            }

            //C2: 11 bits (no ecc)
            for (int j = 10; j > -1; j--) {
                ambe_frOut[2 * 24 + j] = inAmbe49bits[34 - j];
            }

            //C3: 14 bits (no ecc)
            for (int j = 13; j > -1; j--) {
                ambe_frOut[3 * 24 + j] = inAmbe49bits[48 - j];
            }

        }
    };
}
