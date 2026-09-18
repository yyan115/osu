// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.IPC;
using osu.Game.Online.Multiplayer;

namespace osu.Game.Tests.IPC
{
    [TestFixture]
    public class WebSocketTest
    {
        [Test]
        public async Task TestClientInitiatedDuplexCommunication()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            var client = new WebSocketClient(port);

            var duplexComplete = new ManualResetEventSlim(false);

            server.MessageReceived += (clientId, msg) =>
            {
                if (msg != "PING")
                    return;

                // ReSharper disable once AccessToDisposedClosure
                server.SendAsync(clientId, "PONG").FireAndForget();
            };
            client.MessageReceived += msg =>
            {
                if (msg != "PONG")
                    return;

                duplexComplete.Set();
            };

            await server.StartAsync();
            await client.Start();

            await client.SendAsync("PING");
            Assert.That(duplexComplete.Wait(10_000));

            await client.StopAsync();
            await server.StopAsync();

            client.Dispose();
            server.Dispose();
        }

        [Test]
        public async Task TestServerInitiatedDuplexCommunication()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            var client = new WebSocketClient(port);

            var clientConnected = new ManualResetEventSlim();
            var duplexComplete = new ManualResetEventSlim();

            client.MessageReceived += msg =>
            {
                if (msg != "PING")
                    return;

                // ReSharper disable once AccessToDisposedClosure
                client.SendAsync("PONG").FireAndForget();
            };
            server.ClientConnected += _ => clientConnected.Set();
            server.MessageReceived += (_, msg) =>
            {
                if (msg != "PONG")
                    return;

                duplexComplete.Set();
            };

            await server.StartAsync();
            await client.Start();
            Assert.That(clientConnected.Wait(10_000));

            await server.SendAsync(1, "PING");
            Assert.That(duplexComplete.Wait(10_000));

            await client.StopAsync();
            await server.StopAsync();

            client.Dispose();
            server.Dispose();
        }

        [Test]
        public async Task TestServerBroadcast()
        {
            const int port = 54321;
            const int client_count = 5;

            var server = new WebSocketServer(port);
            var clients = new List<WebSocketClient>(client_count);
            var connectionCountdown = new CountdownEvent(client_count);
            var receiptCountdown = new CountdownEvent(client_count);

            for (int i = 0; i < client_count; ++i)
            {
                var client = new WebSocketClient(port);
                client.MessageReceived += msg =>
                {
                    if (msg != "HI ALL")
                        return;

                    receiptCountdown.Signal();
                };
                clients.Add(client);
            }

            server.ClientConnected += _ => connectionCountdown.Signal();

            await server.StartAsync();

            foreach (var client in clients)
                await client.Start();
            Assert.That(connectionCountdown.Wait(10_000));

            await server.BroadcastAsync("HI ALL");
            Assert.That(receiptCountdown.Wait(10_000));

            foreach (var client in clients)
            {
                await client.StopAsync();
                client.Dispose();
            }

            await server.StopAsync();
            server.Dispose();
        }

        [Test]
        public async Task TestClientSoftAborts()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            var client = new WebSocketClient(port);

            await server.StartAsync();
            await client.Start();

            await client.StopAsync();
            client.Dispose();

            await server.StopAsync();
            server.Dispose();
        }

        [Test]
        public async Task TestClientHardAborts()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            var client = new WebSocketClient(port);

            await server.StartAsync();
            await client.Start();

            await client.StopAsync(new CancellationToken(true));
            client.Dispose();

            await server.StopAsync();
            server.Dispose();
        }

        [Test]
        public async Task TestServerSoftAborts()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            var client = new WebSocketClient(port);

            await server.StartAsync();
            await client.Start();

            await server.StopAsync();
            server.Dispose();

            await client.StopAsync();
            client.Dispose();
        }

        [Test]
        public async Task TestServerHardAborts()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            var client = new WebSocketClient(port);

            await server.StartAsync();
            await client.Start();

            await server.StopAsync(new CancellationToken(true));
            server.Dispose();

            await client.StopAsync();
            client.Dispose();
        }

        [Test]
        public async Task TestClientMessageTooLong()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            var client = new WebSocketClient(port);

            var clientClosed = new ManualResetEventSlim();
            client.Closed += clientClosed.Set;

            await server.StartAsync();
            await client.Start();

            await client.SendAsync(new string('0', 9999));
            Assert.That(clientClosed.Wait(10_000));
            await client.StopAsync();
            client.Dispose();

            var client2 = new WebSocketClient(port);

            var duplexComplete = new ManualResetEventSlim();
            server.MessageReceived += (clientId, msg) =>
            {
                if (msg != "PING")
                    return;

                // ReSharper disable once AccessToDisposedClosure
                server.SendAsync(clientId, "PONG").FireAndForget();
            };
            client2.MessageReceived += msg =>
            {
                if (msg != "PONG")
                    return;

                duplexComplete.Set();
            };

            await client2.Start();
            await client2.SendAsync("PING");
            Assert.That(duplexComplete.Wait(10000));

            await client2.StopAsync();
            await server.StopAsync();

            client2.Dispose();
            server.Dispose();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task TestShutdownDoesNotLeaveUnobservedSocketErrors(bool serverFirst, bool hard)
        {
            var unobserved = new ConcurrentQueue<Exception>();

            void onUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
            {
                // Report these on the NUnit thread rather than letting a later game host inherit them.
                unobserved.Enqueue(e.Exception);
                e.SetObserved();
            }

            TaskScheduler.UnobservedTaskException += onUnobserved;

            try
            {
                for (int i = 0; i < 20; i++)
                {
                    await runShutdownCycle(serverFirst, hard).WaitAsync(TimeSpan.FromSeconds(5));
                    await Task.Delay(20);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                await Task.Delay(100);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Assert.That(unobserved, Is.Empty, "Shutdown left unobserved background socket errors.");
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= onUnobserved;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task runShutdownCycle(bool serverFirst, bool hard)
        {
            const int port = 54321;
            using var server = new WebSocketServer(port);
            using var client = new WebSocketClient(port);
            var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            server.ClientConnected += _ => connected.TrySetResult();

            await server.StartAsync();
            await client.Start();
            await connected.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var stoppingToken = new CancellationToken(hard);

            if (serverFirst)
            {
                await server.StopAsync(stoppingToken);
                await client.StopAsync();
            }
            else
            {
                await client.StopAsync(stoppingToken);
                await server.StopAsync();
            }
        }

        [Test]
        public async Task TestStartStopServerWithoutReceivingClients()
        {
            const int port = 54321;

            var server = new WebSocketServer(port);
            await server.StartAsync();
            await server.StopAsync();
            server.Dispose();
        }
    }
}
