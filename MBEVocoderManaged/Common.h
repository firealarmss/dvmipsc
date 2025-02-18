// SPDX-License-Identifier: GPL-2.0-only
/**
* Digital Voice Modem - MBE Vocoder
* GPLv2 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / MBE Vocoder
* @license GPLv2 License (https://opensource.org/licenses/GPL-2.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/
#if !defined(__COMMON_H__)
#define __COMMON_H__

namespace vocoder
{
    // ---------------------------------------------------------------------------
    //  Constants
    // ---------------------------------------------------------------------------

    public enum class MBEMode {
        DMRAMBE,
        IMBE,                   // e.g. IMBE used by P25
    };

    const int rW[36] = {
      0, 1, 0, 1, 0, 1,
      0, 1, 0, 1, 0, 1,
      0, 1, 0, 1, 0, 1,
      0, 1, 0, 1, 0, 2,
      0, 2, 0, 2, 0, 2,
      0, 2, 0, 2, 0, 2
    };

    const int rX[36] = {
      23, 10, 22, 9, 21, 8,
      20, 7, 19, 6, 18, 5,
      17, 4, 16, 3, 15, 2,
      14, 1, 13, 0, 12, 10,
      11, 9, 10, 8, 9, 7,
      8, 6, 7, 5, 6, 4
    };

    const int rY[36] = {
      0, 2, 0, 2, 0, 2,
      0, 2, 0, 3, 0, 3,
      1, 3, 1, 3, 1, 3,
      1, 3, 1, 3, 1, 3,
      1, 3, 1, 3, 1, 3,
      1, 3, 1, 3, 1, 3
    };

    const int rZ[36] = {
      5, 3, 4, 2, 3, 1,
      2, 0, 1, 13, 0, 12,
      22, 11, 21, 10, 20, 9,
      19, 8, 18, 7, 17, 6,
      16, 5, 15, 4, 14, 3,
      13, 2, 12, 1, 11, 0
    };
} // namespace vocoder

#endif // __COMMON_H__
