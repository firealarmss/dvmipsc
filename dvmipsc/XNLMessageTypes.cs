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
    public enum XNLMessageTypes : short
    {
        XNL_MASTER_STATUS_BRDCST = 2,
        XNL_DEVICE_MASTER_QUERY,
        XNL_DEVICE_AUTH_KEY_REQUEST,
        XNL_DEVICE_AUTH_KEY_REPLY,
        XNL_DEVICE_CONN_REQUEST,
        XNL_DEVICE_CONN_REPLY,
        XNL_DEVICE_SYSMAP_REQUEST,
        XNL_DEVICE_SYSMAP_BRDCST,
        XNL_DATA_MSG = 11,
        XNL_DATA_MSG_ACK,
        XNL_AUDIO = 250,
        XNL_AUDIO_AMBE_V3,
        XNL_AUDIO_PCM_V3,
        XNL_AUDIO_AMBE_V4 = 507,
        XNL_AUDIO_PCM_V4
    }
}
