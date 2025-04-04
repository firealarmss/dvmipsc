﻿// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Audio Bridge
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Audio Bridge
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/

using Microsoft.Extensions.Hosting;

using Serilog;

namespace dvmipsc
{
    /// <summary>
    /// Implements the FNE router.
    /// </summary>
    public class Service : BackgroundService
    {
        private List<FneSystemBase> systems = new List<FneSystemBase>();

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the list of registered running systems.
        /// </summary>
        public List<FneSystemBase> Systems => systems;

        /*
        ** Methods
        */

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        /// <returns><see cref="Task"/></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Logger.Information("[FNE Service] SYSTEM STARTING...");

            Log.Logger.Information($"[FNE Service] PEER: REGISTER SYSTEM {Program.Configuration.Name} ({Program.Configuration.PeerId})");
            PeerSystem system = new PeerSystem();
            system.StartIPSC();
            systems.Add(system);
            system.Start();

            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        /// <returns><see cref="Task"/></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[Bridge Service] SYSTEM STOPPING...");

            // deregister systems
            foreach (FneSystemBase system in systems)
            {
                Console.WriteLine($"[FNE Service] DE-REGISTER SYSTEM {system.SystemName}");
                if (system.IsStarted)
                    system.Stop();
            }
            systems = null;

            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that 
        /// represents the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="token">Triggered when <see cref="StopAsync(CancellationToken)"/> is called.</param>
        /// <returns>A <see cref="Task"/> Task that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            Log.Logger.Information("[FNE Service] SYSTEM RUNNING...");

            // idle loop (used to update rules, and other various datasets that need to update on a cycle)
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException) { /* stub */ }
            }
        }
    } // public class Service : BackgroundService
} // namespace dvmbridge