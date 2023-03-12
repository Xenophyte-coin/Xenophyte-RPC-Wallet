using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xenophyte_Connector_All.Setting;
using Xenophyte_Connector_All.Utils;
using Xenophyte_Connector_All.Wallet;
using Xenophyte_Rpc_Wallet.ConsoleObject;
using Xenophyte_Rpc_Wallet.Database;
using Xenophyte_Rpc_Wallet.Setting;
using Xenophyte_Rpc_Wallet.Utility;
using Xenophyte_Rpc_Wallet.Wallet;

namespace Xenophyte_Rpc_Wallet.API
{
    public class ClassApiEnumeration
    {
        public const string GetTotalWalletIndex = "get_total_wallet_index"; // Number of total wallet created.
        public const string GetTotalWalletTransactionSync = "get_total_transaction_sync"; // Return the total amount of transaction(s) sync.
        public const string GetWalletAddressByIndex = "get_wallet_address_by_index"; // Get a wallet address by an index selected.
        public const string GetWalletBalanceByIndex = "get_wallet_balance_by_index"; // Get a wallet balance and pending balance by an index selected.
        public const string GetWalletBalanceByWalletAddress = "get_wallet_balance_by_wallet_address"; // Get a wallet balance and pending balance by an wallet address selected.
        public const string GetWalletTotalTransactionByIndex = "get_wallet_total_transaction_by_index"; // Get the total transaction sync from an index selected.
        public const string GetWalletTotalAnonymousTransactionByIndex = "get_total_anonymous_transaction_by_index"; // Get the total anonymous transaction sync from an index selected.
        public const string GetWalletTotalTransactionByWalletAddress = "get_wallet_total_transaction_by_wallet_address"; // Get the total transaction sync from an wallet address selected.
        public const string GetWalletTotalAnonymousTransactionByWalletAddress = "get_total_anonymous_transaction_by_wallet_address"; // Get the total anonymous transaction sync from an wallet address selected.
        public const string GetWalletTransaction = "get_wallet_transaction"; // Get a selected transaction by an index selected and a wallet address selected.
        public const string GetWholeWalletTransactionByRange = "get_whole_wallet_transaction_by_range"; // Get a selected transaction by a range selected and a wallet address selected.
        public const string GetWalletAnonymousTransaction = "get_wallet_anonymous_transaction"; // Get a selected anonymous transaction by an index selected and a wallet address selected.
        public const string GetWalletTransactionByHash = "get_wallet_transaction_by_hash"; // Get a selected transaction by a transaction hash selected and a wallet address selected.
        public const string GetTransactionByHash = "get_transaction_by_hash"; // Get a transaction selected by hash.
        public const string SendTransactionByWalletAddress = "send_transaction_by_wallet_address"; // Sent a transaction from a selected wallet address.
        public const string SendTransferByWalletAddress = "send_transfer_by_wallet_address"; // Sent a transfer from a selected wallet address.
        public const string TaskSendTransaction = "task_send_transaction"; // Schedule a task for send a transaction.
        public const string TaskSendTransfer = "task_send_transfer"; // Schedule a task for send a transaction.
        public const string GetTaskScheduled = "get_task_scheduled"; // Get task information scheduled.
        public const string ClearTask = "clear_task"; // Clear complete/failed task.
        public const string UpdateWalletByAddress = "update_wallet_by_address"; // Update manually a selected wallet by his address target.
        public const string UpdateWalletByIndex = "update_wallet_by_index"; // Update manually a selected wallet by his index target.
        public const string CreateWallet = "create_wallet"; // Create a new wallet, return wallet address.
        public const string CreateWalletError = "create_wallet_error"; // Return an error pending to create a wallet.
        public const string PacketNotExist = "not_exist";
        public const string PacketError = "packet_error";
        public const string WalletNotExist = "wallet_not_exist";
        public const string IndexNotExist = "index_not_exist";
        public const string IndexError = "index_error";
        public const string TransactionEmpty = "transaction_empty";
        public const string WalletBusyOnUpdate = "wallet_busy_on_update";
        public const string ClearTaskComplete = "clear_task_complete";
        public const string ClearTaskError = "clear_task_error";
        public const string TaskInsertError = "task_insert_error";
        public const string TaskNotExist = "task_not_exist";
        public const string TaskDisabled = "task_disabled";
    }

    public class ClassApi
    {

        private static bool _listenApiHttpConnectionStatus;
        private static CancellationTokenSource _cancellationTokenApiHttpConnection;
        private static TcpListener _listenerApiHttpConnection;
        public const int MaxKeepAlive = 30;
       
        /// <summary>
        /// Enable http/https api of the remote node, listen incoming connection throught web client.
        /// </summary>
        public static void StartApiHttpServer()
        {
            _listenApiHttpConnectionStatus = true;

            _listenerApiHttpConnection = new TcpListener(IPAddress.Parse(ClassRpcSetting.RpcWalletApiIpBind), ClassRpcSetting.RpcWalletApiPort);

            _listenerApiHttpConnection.Start();

            _cancellationTokenApiHttpConnection = new CancellationTokenSource();

            try
            {
                Task.Factory.StartNew(async delegate
                {
                    while (_listenApiHttpConnectionStatus && !Program.Exit)
                    {
                        try
                        {
                            await _listenerApiHttpConnection.AcceptTcpClientAsync().ContinueWith(async clientAsync =>
                            {
                                try
                                {
                                    var client = await clientAsync;
                                    CancellationTokenSource cancellationApi = new CancellationTokenSource();
                                    await Task.Factory.StartNew(async () =>
                                    {
                                        var ip = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString();
                                        using (var clientApiHttpObject = new ClassClientApiHttpObject(client, ip, cancellationApi))
                                        {
                                            await clientApiHttpObject.StartHandleClientHttpAsync();
                                        }
                                    }, cancellationApi.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                                }
                                catch
                                {
                                    // Ignored
                                }
                            });
                        }
                        catch
                        {

                        }
                    }
                }, _cancellationTokenApiHttpConnection.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Stop Api HTTP Server
        /// </summary>
        public static void StopApiHttpServer()
        {
            _listenApiHttpConnectionStatus = false;
            try
            {
                if (_cancellationTokenApiHttpConnection != null)
                {
                    if (!_cancellationTokenApiHttpConnection.IsCancellationRequested)
                    {
                        _cancellationTokenApiHttpConnection.Cancel();
                    }
                }
            }
            catch
            {
                // Ignored
            }
            try
            {
                _listenerApiHttpConnection.Stop();
            }
            catch
            {
                // Ignored
            }
        }
    }

    public class ClassClientApiHttpObject : IDisposable
    {
        #region Disposing Part Implementation 

        private bool _disposed;

        ~ClassClientApiHttpObject()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
            }

            _disposed = true;
        }

        #endregion

        private bool _clientStatus;
        private TcpClient _client;
        private string _ip;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ip"></param>
        /// <param name="cancellationTokenSource"></param>
        public ClassClientApiHttpObject(TcpClient client, string ip, CancellationTokenSource cancellationTokenSource)
        {
            _clientStatus = true;
            _client = client;
            _ip = ip;
            _cancellationTokenSource = cancellationTokenSource;
        }

        private async void MaxKeepAliveFunctionAsync()
        {
            var dateConnection = DateTimeOffset.Now.ToUnixTimeSeconds() + ClassApi.MaxKeepAlive;
            while(dateConnection > DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                await Task.Delay(1000);
            }
            CloseClientConnection();
        }

        /// <summary>
        /// Start to listen incoming client.
        /// </summary>
        /// <returns></returns>
        public async Task StartHandleClientHttpAsync()
        {
            var isWhitelisted = true;

            if (ClassRpcSetting.RpcWalletApiIpWhitelist.Count > 0)
            {
                if (!ClassRpcSetting.RpcWalletApiIpWhitelist.Contains(_ip))
                {
                    ClassConsole.ConsoleWriteLine(_ip + " is not whitelisted.", ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelApi);
                    isWhitelisted = false;
                }
            }

            if (isWhitelisted)
            {
                try
                {
                    await Task.Factory.StartNew(() => MaxKeepAliveFunctionAsync(), _cancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Current).ConfigureAwait(false);
                    while (_clientStatus)
                    {
                        try
                        {
                            using (NetworkStream clientHttpReader = new NetworkStream(_client.Client))
                            {
                                using (BufferedStream bufferedStreamNetwork = new BufferedStream(clientHttpReader, ClassConnectorSetting.MaxNetworkPacketSize))
                                {
                                    byte[] buffer = new byte[ClassConnectorSetting.MaxNetworkPacketSize];

                                    int received = await bufferedStreamNetwork.ReadAsync(buffer, 0, buffer.Length);
                                    if (received > 0)
                                    {
                                        string packet = Encoding.UTF8.GetString(buffer, 0, received);
                                        if (ClassRpcSetting.RpcWalletApiEnableXForwardedForResolver)
                                        {
                                            try
                                            {
                                                if (!GetAndCheckForwardedIp(packet))
                                                {
                                                    break;
                                                }
                                            }
                                            catch
                                            {
                                                // Ignored
                                            }
                                        }
                                        packet = ClassUtility.GetStringBetween(packet, "GET /", "HTTP");
                                        packet = packet.Replace("%7C", "|"); // Translate special character | 
                                        packet = packet.Replace(" ", ""); // Remove empty,space characters
                                        ClassConsole.ConsoleWriteLine("HTTP API - packet received from IP: " + _ip + " - " + packet, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelApi);
                                        if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                        {
                                            packet = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael, packet, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                            if (packet == ClassAlgoErrorEnumeration.AlgoError)
                                            {
                                                ClassConsole.ConsoleWriteLine("HTTP API - wrong packet received from IP: " + _ip + " - " + packet, ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelApi);
                                                break;
                                            }
                                            ClassConsole.ConsoleWriteLine("HTTP API - decrypted packet received from IP: " + _ip + " - " + packet, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelApi);
                                        }
                                        await HandlePacketHttpAsync(packet);

                                    }

                                    break;
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            ClassConsole.ConsoleWriteLine("HTTP API - exception error: " + error.Message, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelApi);
                            break;
                        }
                    }
                }
                catch
                {
                    // Ignored
                }
            }
            CloseClientConnection();
        }

        /// <summary>
        /// This method permit to get back the real ip behind a proxy and check the list of banned IP.
        /// </summary>
        private bool GetAndCheckForwardedIp(string packet)
        {
            var splitPacket = packet.Split(new[] { "\n" }, StringSplitOptions.None);
            foreach (var packetEach in splitPacket)
            {
                if (!string.IsNullOrEmpty(packetEach))
                {
                    if (packetEach.ToLower().Contains("x-forwarded-for: "))
                    {
                        _ip = packetEach.ToLower().Replace("x-forwarded-for: ", "");
                        ClassConsole.ConsoleWriteLine("HTTP/HTTPS API - X-Forwarded-For ip of the client is: " + _ip, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelApi);
                        if (ClassRpcSetting.RpcWalletApiIpWhitelist.Count > 0)
                        {
                            if (!ClassRpcSetting.RpcWalletApiIpWhitelist.Contains(_ip))
                            {
                                return false;
                            }
                        }
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// Close connection incoming from the client.
        /// </summary>
        private void CloseClientConnection()
        {
            _clientStatus = false;
            try
            {
                _client?.Close();
                _client?.Dispose();
            }
            catch
            {
                // Ignored
            }
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                }
            }
            catch
            {
                // Ignored
            }
        }

        /// <summary>
        /// Handle get request received from client.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task HandlePacketHttpAsync(string packet)
        {
            try
            {
                if (packet.Contains("|"))
                {
                    var splitPacket = packet.Split(new[] { "|" }, StringSplitOptions.None);

                    switch (splitPacket[0])
                    {
                        case ClassApiEnumeration.UpdateWalletByAddress:
                            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(splitPacket[1]))
                            {
                                if (!ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletUpdateStatus())
                                {
                                    await ClassWalletUpdater.UpdateWallet(splitPacket[1]);

                                    var walletUpdateJsonObject = new ClassApiJsonWalletUpdate()
                                    {
                                        wallet_address = splitPacket[1],
                                        wallet_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                        wallet_pending_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletPendingBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                        wallet_unique_id = long.Parse(ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletUniqueId()),
                                        wallet_unique_anonymous_id = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletAnonymousUniqueId(), NumberStyles.Currency, Program.GlobalCultureInfo)
                                    };

                                    string data = JsonConvert.SerializeObject(walletUpdateJsonObject);
                                    if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                    {
                                        data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                    }
                                    StringBuilder builder = new StringBuilder();
                                    builder.AppendLine(@"HTTP/1.1 200 OK");
                                    builder.AppendLine(@"Content-Type: text/plain");
                                    builder.AppendLine(@"Content-Length: " + data.Length);
                                    builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                    builder.AppendLine(@"");
                                    builder.AppendLine(@"" + data);
                                    await SendPacketAsync(builder.ToString());
                                    builder.Clear();
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletBusyOnUpdate);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.UpdateWalletByIndex:
                            if (int.TryParse(splitPacket[1], out var walletIndexUpdate))
                            {
                                int counter = 0;
                                bool found = false;
                                foreach (var walletObject in ClassRpcDatabase.RpcDatabaseContent.ToArray())
                                {
                                    counter++;
                                    if (counter == walletIndexUpdate)
                                    {
                                        found = true;
                                        if (!ClassRpcDatabase.RpcDatabaseContent[walletObject.Key].GetWalletUpdateStatus())
                                        {
                                            await ClassWalletUpdater.UpdateWallet(walletObject.Key);

                                            var walletUpdateJsonObject = new ClassApiJsonWalletUpdate()
                                            {
                                                wallet_address = walletObject.Key,
                                                wallet_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletObject.Key].GetWalletBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                                wallet_pending_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletObject.Key].GetWalletPendingBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                                wallet_unique_id = long.Parse(ClassRpcDatabase.RpcDatabaseContent[walletObject.Key].GetWalletUniqueId()),
                                                wallet_unique_anonymous_id = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletObject.Key].GetWalletAnonymousUniqueId(), NumberStyles.Currency, Program.GlobalCultureInfo)
                                            };

                                            string data = JsonConvert.SerializeObject(walletUpdateJsonObject);
                                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            {
                                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                            }
                                            StringBuilder builder = new StringBuilder();
                                            builder.AppendLine(@"HTTP/1.1 200 OK");
                                            builder.AppendLine(@"Content-Type: text/plain");
                                            builder.AppendLine(@"Content-Length: " + data.Length);
                                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                            builder.AppendLine(@"");
                                            builder.AppendLine(@"" + data);
                                            await SendPacketAsync(builder.ToString());
                                            builder.Clear();
                                            break;
                                        }

                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletBusyOnUpdate);
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletAddressByIndex:
                            if (int.TryParse(splitPacket[1], out var walletIndex))
                            {
                                bool found = false;
                                int counter = 0;
                                foreach (var walletObject in ClassRpcDatabase.RpcDatabaseContent.ToArray())
                                {
                                    counter++;
                                    if (counter == walletIndex)
                                    {
                                        found = true;
                                        await BuildAndSendHttpPacketAsync(walletObject.Key);
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletBalanceByIndex:
                            if (int.TryParse(splitPacket[1], out var walletIndex2))
                            {
                                bool found = false;
                                int counter = 0;
                                foreach (var walletObject in ClassRpcDatabase.RpcDatabaseContent.ToArray())
                                {
                                    counter++;
                                    if (counter == walletIndex2)
                                    {
                                        found = true;

                                        var walletBalanceJsonObject = new ClassApiJsonWalletBalance()
                                        {
                                            wallet_address = walletObject.Key,
                                            wallet_balance = decimal.Parse(walletObject.Value.GetWalletBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                            wallet_pending_balance = decimal.Parse(walletObject.Value.GetWalletPendingBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                        };

                                        string data = JsonConvert.SerializeObject(walletBalanceJsonObject);
                                        if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);

                                        StringBuilder builder = new StringBuilder();
                                        builder.AppendLine(@"HTTP/1.1 200 OK");
                                        builder.AppendLine(@"Content-Type: text/plain");
                                        builder.AppendLine(@"Content-Length: " + data.Length);
                                        builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                        builder.AppendLine(@"");
                                        builder.AppendLine(@"" + data);
                                        await SendPacketAsync(builder.ToString());
                                        builder.Clear();
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWholeWalletTransactionByRange:
                            if (long.TryParse(splitPacket[1], out var startIndex))
                            {
                                if (long.TryParse(splitPacket[2], out var endIndex))
                                {
                                    if (ClassRpcDatabase.RpcDatabaseContent.Count > 0)
                                    {
                                        long totalTransactionIndex = 0;
                                        long totalTransactionTravel = 0;

                                        Dictionary<string, ClassApiJsonTransaction> listOfTransactionPerRange = new Dictionary<string, ClassApiJsonTransaction>();
                                        foreach (var walletObject in ClassSyncDatabase.DatabaseTransactionSync.ToArray().OrderBy(value => value.Value))
                                        {

                                            string transaction = walletObject.Key;

                                            if (transaction != null)
                                            {

                                                var splitTransaction = transaction.Split(new[] { "#" }, StringSplitOptions.None);

                                                bool transactionIgnored = false;

                                                if (ClassRpcSetting.RpcWalletEnableEuropeanExchangeRule)
                                                {
                                                    if (splitTransaction[0] == ClassSyncDatabaseEnumeration.DatabaseAnonymousTransactionMode || splitTransaction[3] == ClassSyncDatabaseEnumeration.DatabaseAnonymousTransactionType)
                                                    {
                                                        transactionIgnored = true;
                                                    }
                                                }

                                                if (!transactionIgnored)
                                                {
                                                    totalTransactionIndex++;

                                                    if (totalTransactionTravel <= endIndex)
                                                    {
                                                        totalTransactionTravel++;
                                                        if (totalTransactionTravel >= startIndex && totalTransactionTravel <= endIndex)
                                                        {
                                                            if (!listOfTransactionPerRange.ContainsKey(splitTransaction[2] + splitTransaction[9]))
                                                            {

                                                                var transactionJsonObject = new ClassApiJsonTransaction()
                                                                {
                                                                    index = totalTransactionIndex,
                                                                    wallet_address = splitTransaction[9],
                                                                    mode = splitTransaction[0],
                                                                    type = splitTransaction[1],
                                                                    hash = splitTransaction[2],
                                                                    wallet_dst_or_src = splitTransaction[3],
                                                                    amount = decimal.Parse(splitTransaction[4], NumberStyles.Currency, Program.GlobalCultureInfo),
                                                                    fee = decimal.Parse(splitTransaction[5], NumberStyles.Currency, Program.GlobalCultureInfo),
                                                                    timestamp_send = long.Parse(splitTransaction[6]),
                                                                    timestamp_recv = long.Parse(splitTransaction[7]),
                                                                    blockchain_height = splitTransaction[8]
                                                                };

                                                                listOfTransactionPerRange.Add(splitTransaction[2] + splitTransaction[9], transactionJsonObject);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        
                                        if (listOfTransactionPerRange.Count > 0)
                                        {

                                            string data = JsonConvert.SerializeObject(listOfTransactionPerRange.Values.ToArray());
                                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            {
                                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                            }
                                            StringBuilder builder = new StringBuilder();
                                            builder.AppendLine(@"HTTP/1.1 200 OK");
                                            builder.AppendLine(@"Content-Type: text/plain");
                                            builder.AppendLine(@"Content-Length: " + data.Length);
                                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                            builder.AppendLine(@"");
                                            builder.AppendLine(@"" + data);
                                            await SendPacketAsync(builder.ToString());
                                            builder.Clear();
                                            GC.SuppressFinalize(data);
                                            listOfTransactionPerRange.Clear();
                                        }
                                        else
                                        {
                                            await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TransactionEmpty);

                                        }
                                    }
                                    else
                                    {
                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                                    }
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexError);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexError);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletBalanceByWalletAddress:
                            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(splitPacket[1]))
                            {
                                var walletBalanceJsonObject = new ClassApiJsonWalletBalance()
                                {
                                    wallet_address = splitPacket[1],
                                    wallet_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                    wallet_pending_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletPendingBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                };

                                string data = JsonConvert.SerializeObject(walletBalanceJsonObject);
                                if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                {
                                    data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                }
                                StringBuilder builder = new StringBuilder();
                                builder.AppendLine(@"HTTP/1.1 200 OK");
                                builder.AppendLine(@"Content-Type: text/plain");
                                builder.AppendLine(@"Content-Length: " + data.Length);
                                builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                builder.AppendLine(@"");
                                builder.AppendLine(@"" + data);
                                await SendPacketAsync(builder.ToString());
                                builder.Clear();
                            }
                            else
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);

                            break;
                        case ClassApiEnumeration.SendTransactionByWalletAddress:
                            if (splitPacket.Length >= 6)
                            {
                                var walletAddressSource = splitPacket[1];
                                var amount = splitPacket[2];
                                var fee = splitPacket[3];
                                var anonymousOption = splitPacket[4];
                                var walletAddressTarget = splitPacket[5];

                                if (anonymousOption == "1")
                                {
                                    string result = await ClassWalletUpdater.ProceedTransactionTokenRequestAsync(walletAddressSource, amount, fee, walletAddressTarget, true);
                                    var splitResult = result.Split(new[] { "|" }, StringSplitOptions.None);
                                    var sendTransactionJsonObject = new ClassApiJsonSendTransaction()
                                    {
                                        result = splitResult[0],
                                        hash = splitResult[1].ToLower(),
                                        wallet_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddressSource].GetWalletBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                        wallet_pending_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddressSource].GetWalletPendingBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                    };

                                    string data = JsonConvert.SerializeObject(sendTransactionJsonObject);
                                    if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                        data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                    
                                    StringBuilder builder = new StringBuilder();
                                    builder.AppendLine(@"HTTP/1.1 200 OK");
                                    builder.AppendLine(@"Content-Type: text/plain");
                                    builder.AppendLine(@"Content-Length: " + data.Length);
                                    builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                    builder.AppendLine(@"");
                                    builder.AppendLine(@"" + data);
                                    await SendPacketAsync(builder.ToString());
                                    builder.Clear();
                                }
                                else
                                {
                                    if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(walletAddressSource))
                                    {

                                        string result = await ClassWalletUpdater.ProceedTransactionTokenRequestAsync(walletAddressSource, amount, fee, walletAddressTarget, false);
                                        var splitResult = result.Split(new[] { "|" }, StringSplitOptions.None);

                                        var sendTransactionJsonObject = new ClassApiJsonSendTransaction()
                                        {
                                            result = splitResult[0],
                                            hash = splitResult[1].ToLower(),
                                            wallet_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddressSource].GetWalletBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                            wallet_pending_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddressSource].GetWalletPendingBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                        };

                                        string data = JsonConvert.SerializeObject(sendTransactionJsonObject);
                                        if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                       
                                        StringBuilder builder = new StringBuilder();
                                        builder.AppendLine(@"HTTP/1.1 200 OK");
                                        builder.AppendLine(@"Content-Type: text/plain");
                                        builder.AppendLine(@"Content-Length: " + data.Length);
                                        builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                        builder.AppendLine(@"");
                                        builder.AppendLine(@"" + data);
                                        await SendPacketAsync(builder.ToString());
                                        builder.Clear();
                                    }

                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.PacketNotExist);
                            }
                            break;
                        case ClassApiEnumeration.TaskSendTransaction:
                            if (ClassRpcSetting.RpcWalletEnableApiTaskScheduler)
                            {
                                if (splitPacket.Length >= 6)
                                {
                                    if (long.TryParse(splitPacket[6], out var timeTask))
                                    {
                                        var walletAddressSource = splitPacket[1];
                                        var amount = splitPacket[2];
                                        var fee = splitPacket[3];
                                        var anonymousOption = splitPacket[4];
                                        var walletAddressTarget = splitPacket[5];

                                        var resultTask = ClassApiTaskScheduler.InsertTaskScheduled(ClassApiTaskType.API_TASK_TYPE_TRANSACTION, walletAddressSource, amount, fee, anonymousOption, walletAddressTarget, timeTask);
                                        if (resultTask.Item1)
                                        {
                                            var resultTaskJsonObject = new ClassApiJsonTaskSubmit()
                                            {
                                                result = "OK",
                                                task_hash = resultTask.Item2
                                            };
                                            string data = JsonConvert.SerializeObject(resultTaskJsonObject);
                                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            {
                                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                            }
                                            StringBuilder builder = new StringBuilder();
                                            builder.AppendLine(@"HTTP/1.1 200 OK");
                                            builder.AppendLine(@"Content-Type: text/plain");
                                            builder.AppendLine(@"Content-Length: " + data.Length);
                                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                            builder.AppendLine(@"");
                                            builder.AppendLine(@"" + data);
                                            await SendPacketAsync(builder.ToString());
                                            builder.Clear();
                                        }
                                        else
                                        {
                                            await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskInsertError);
                                        }
                                    }
                                    else
                                    {
                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskInsertError);
                                    }
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.PacketError);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskDisabled);
                            }
                            break;
                        case ClassApiEnumeration.TaskSendTransfer:
                            if (ClassRpcSetting.RpcWalletEnableApiTaskScheduler)
                            {
                                if (splitPacket.Length >= 5)
                                {
                                    if (long.TryParse(splitPacket[4], out var timeTask))
                                    {
                                        var walletAddressSource = splitPacket[1];
                                        var amount = splitPacket[2];
                                        var walletAddressTarget = splitPacket[3];

                                        var resultTask = ClassApiTaskScheduler.InsertTaskScheduled(ClassApiTaskType.API_TASK_TYPE_TRANSFER, walletAddressSource, amount, string.Empty, string.Empty, walletAddressTarget, timeTask);
                                        if (resultTask.Item1)
                                        {
                                            var resultTaskJsonObject = new ClassApiJsonTaskSubmit()
                                            {
                                                result = "OK",
                                                task_hash = resultTask.Item2
                                            };
                                            string data = JsonConvert.SerializeObject(resultTaskJsonObject);
                                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            {
                                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                            }
                                            StringBuilder builder = new StringBuilder();
                                            builder.AppendLine(@"HTTP/1.1 200 OK");
                                            builder.AppendLine(@"Content-Type: text/plain");
                                            builder.AppendLine(@"Content-Length: " + data.Length);
                                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                            builder.AppendLine(@"");
                                            builder.AppendLine(@"" + data);
                                            await SendPacketAsync(builder.ToString());
                                            builder.Clear();
                                        }
                                        else
                                        {
                                            await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskInsertError);
                                        }
                                    }
                                    else
                                    {
                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskInsertError);
                                    }
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.PacketError);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskDisabled);
                            }
                            break;
                        case ClassApiEnumeration.GetTaskScheduled:
                            if (splitPacket.Length >= 1)
                            {
                                if (ClassRpcSetting.RpcWalletEnableApiTaskScheduler)
                                {
                                    if (ClassApiTaskScheduler.DictionaryApiTaskScheduled.ContainsKey(splitPacket[1]))
                                    {
                                        var taskObject = ClassApiTaskScheduler.DictionaryApiTaskScheduled[splitPacket[1]];
                                        string data;
                                        if (!string.IsNullOrEmpty(taskObject.TaskResult))
                                        {
                                            var taskContentJsonObject = new ClassApiJsonTaskContent()
                                            {
                                                task_date_scheduled = taskObject.TaskDate,
                                                task_status = Enum.GetName(typeof(ClassApiTaskStatus), taskObject.TaskStatus),
                                                task_type = Enum.GetName(typeof(ClassApiTaskType), taskObject.TaskType),
                                                task_wallet_src = taskObject.TaskWalletSrc,
                                                task_amount = decimal.Parse(taskObject.TaskWalletAmount.Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo),
                                                task_fee = decimal.Parse(taskObject.TaskWalletFee.Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo),
                                                task_anonymity = taskObject.TaskWalletAnonymity == "1",
                                                task_wallet_dst = taskObject.TaskWalletDst,
                                                task_result = taskObject.TaskResult.Split(new[] { "|" }, StringSplitOptions.None)[0],
                                                task_tx_hash = taskObject.TaskResult.Split(new[] { "|" }, StringSplitOptions.None)[1]
                                            };
                                            data = JsonConvert.SerializeObject(taskContentJsonObject);
                                        }
                                        else
                                        {
                                            var taskContentJsonObject = new ClassApiJsonTaskContent()
                                            {
                                                task_date_scheduled = taskObject.TaskDate,
                                                task_status = Enum.GetName(typeof(ClassApiTaskStatus), taskObject.TaskStatus),
                                                task_type = Enum.GetName(typeof(ClassApiTaskType), taskObject.TaskType),
                                                task_wallet_src = taskObject.TaskWalletSrc,
                                                task_amount = decimal.Parse(taskObject.TaskWalletAmount.Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo),
                                                task_fee = decimal.Parse(taskObject.TaskWalletFee.Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo),
                                                task_anonymity = taskObject.TaskWalletAnonymity == "1",
                                                task_wallet_dst = taskObject.TaskWalletDst
                                            };
                                            data = JsonConvert.SerializeObject(taskContentJsonObject);
                                        }
                                        if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                        {
                                            data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                        }
                                        StringBuilder builder = new StringBuilder();
                                        builder.AppendLine(@"HTTP/1.1 200 OK");
                                        builder.AppendLine(@"Content-Type: text/plain");
                                        builder.AppendLine(@"Content-Length: " + data.Length);
                                        builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                        builder.AppendLine(@"");
                                        builder.AppendLine(@"" + data);
                                        await SendPacketAsync(builder.ToString());
                                        builder.Clear();
                                    }
                                    else
                                    {
                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskNotExist);
                                    }
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskDisabled);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.PacketError);
                            }
                            break;
                        case ClassApiEnumeration.SendTransferByWalletAddress:
                            if (splitPacket.Length >= 3)
                            {
                                var walletAddressSource = splitPacket[1];
                                var amount = splitPacket[2];
                                var walletAddressTarget = splitPacket[3];

                                string result = await ClassWalletUpdater.ProceedTransferTokenRequestAsync(walletAddressSource, amount, walletAddressTarget);
                                var splitResult = result.Split(new[] { "|" }, StringSplitOptions.None);

                                var sendTransactionJsonObject = new ClassApiJsonSendTransfer()
                                {
                                    result = splitResult[0],
                                    hash = splitResult[1].ToLower(),
                                    wallet_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddressSource].GetWalletBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                    wallet_pending_balance = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddressSource].GetWalletPendingBalance(), NumberStyles.Currency, Program.GlobalCultureInfo),
                                };

                                string data = JsonConvert.SerializeObject(sendTransactionJsonObject);
                                if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                {
                                    data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                }
                                StringBuilder builder = new StringBuilder();
                                builder.AppendLine(@"HTTP/1.1 200 OK");
                                builder.AppendLine(@"Content-Type: text/plain");
                                builder.AppendLine(@"Content-Length: " + data.Length);
                                builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                builder.AppendLine(@"");
                                builder.AppendLine(@"" + data);
                                await SendPacketAsync(builder.ToString());
                                builder.Clear();

                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.PacketNotExist);
                            }
                            break;
                        case ClassApiEnumeration.CreateWallet:
                            if (long.TryParse(splitPacket[1], out var timeout))
                            {
                                var dateTimeEnd = DateTimeOffset.Now.ToUnixTimeSeconds() + timeout;

                                bool success = false;
                                while (dateTimeEnd >= DateTimeOffset.Now.ToUnixTimeSeconds())
                                {
                                    using (var walletCreatorObject = new ClassWalletCreator())
                                    {
                                        await Task.Run(async delegate
                                        {
                                            if (!await walletCreatorObject.StartWalletConnectionAsync(ClassWalletPhase.Create, ClassUtility.MakeRandomWalletPassword()))
                                            {
                                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.CreateWalletError);
                                            }
                                        }).ConfigureAwait(false);

                                        while (walletCreatorObject.WalletCreateResult == ClassWalletCreatorEnumeration.WalletCreatorPending)
                                        {
                                            await Task.Delay(100);
                                            if (dateTimeEnd < DateTimeOffset.Now.ToUnixTimeSeconds())
                                            {
                                                break;
                                            }
                                        }
                                        switch (walletCreatorObject.WalletCreateResult)
                                        {
                                            case ClassWalletCreatorEnumeration.WalletCreatorSuccess:
                                                success = true;
                                                await BuildAndSendHttpPacketAsync(walletCreatorObject.WalletAddressResult);
                                                dateTimeEnd = DateTimeOffset.Now.ToUnixTimeSeconds();
                                                break;
                                        }
                                    }
                                }
                                if (!success)
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.CreateWalletError);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.CreateWalletError);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletTotalTransactionByIndex:
                            if (int.TryParse(splitPacket[1], out var walletIndex3))
                            {
                                bool found = false;
                                int counter = 0;
                                foreach (var walletObject in ClassRpcDatabase.RpcDatabaseContent.ToArray())
                                {
                                    counter++;
                                    if (counter == walletIndex3)
                                    {
                                        found = true;

                                        var walletTotalTransactionJsonObject = new ClassApiJsonWalletTotalTransaction()
                                        {
                                            wallet_address = walletObject.Key,
                                            wallet_total_transaction = walletObject.Value.GetWalletTotalTransactionSync()
                                        };
                                        string data = JsonConvert.SerializeObject(walletTotalTransactionJsonObject);
                                        if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                        {
                                            data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                        }
                                        StringBuilder builder = new StringBuilder();
                                        builder.AppendLine(@"HTTP/1.1 200 OK");
                                        builder.AppendLine(@"Content-Type: text/plain");
                                        builder.AppendLine(@"Content-Length: " + data.Length);
                                        builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                        builder.AppendLine(@"");
                                        builder.AppendLine(@"" + data);
                                        await SendPacketAsync(builder.ToString());
                                        builder.Clear();
                                    }
                                }
                                if (!found)
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletTotalAnonymousTransactionByIndex:
                            if (int.TryParse(splitPacket[1], out var walletIndex4))
                            {
                                bool found = false;
                                int counter = 0;
                                foreach (var walletObject in ClassRpcDatabase.RpcDatabaseContent.ToArray())
                                {
                                    counter++;
                                    if (counter == walletIndex4)
                                    {
                                        found = true;
                                        var walletTotalTransactionJsonObject = new ClassApiJsonWalletTotalAnonymousTransaction()
                                        {
                                            wallet_address = walletObject.Key,
                                            wallet_total_anonymous_transaction = walletObject.Value.GetWalletTotalAnonymousTransactionSync()
                                        };
                                        string data = JsonConvert.SerializeObject(walletTotalTransactionJsonObject);
                                        if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                        {
                                            data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                        }
                                        StringBuilder builder = new StringBuilder();
                                        builder.AppendLine(@"HTTP/1.1 200 OK");
                                        builder.AppendLine(@"Content-Type: text/plain");
                                        builder.AppendLine(@"Content-Length: " + data.Length);
                                        builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                        builder.AppendLine(@"");
                                        builder.AppendLine(@"" + data);
                                        await SendPacketAsync(builder.ToString());
                                        builder.Clear();
                                    }
                                }
                                if (!found)
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletTotalTransactionByWalletAddress:
                            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(splitPacket[1]))
                            {
                                var walletTotalTransactionJsonObject = new ClassApiJsonWalletTotalTransaction()
                                {
                                    wallet_address = splitPacket[1],
                                    wallet_total_transaction = ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletTotalTransactionSync()
                                };
                                string data = JsonConvert.SerializeObject(walletTotalTransactionJsonObject);
                                if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                {
                                    data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                }
                                StringBuilder builder = new StringBuilder();
                                builder.AppendLine(@"HTTP/1.1 200 OK");
                                builder.AppendLine(@"Content-Type: text/plain");
                                builder.AppendLine(@"Content-Length: " + data.Length);
                                builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                builder.AppendLine(@"");
                                builder.AppendLine(@"" + data);
                                await SendPacketAsync(builder.ToString());
                                builder.Clear();
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletTotalAnonymousTransactionByWalletAddress:
                            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(splitPacket[1]))
                            {
                                var walletTotalTransactionJsonObject = new ClassApiJsonWalletTotalAnonymousTransaction()
                                {
                                    wallet_address = splitPacket[1],
                                    wallet_total_anonymous_transaction = ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletTotalAnonymousTransactionSync()
                                };
                                string data = JsonConvert.SerializeObject(walletTotalTransactionJsonObject);
                                if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                {
                                    data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                }
                                StringBuilder builder = new StringBuilder();
                                builder.AppendLine(@"HTTP/1.1 200 OK");
                                builder.AppendLine(@"Content-Type: text/plain");
                                builder.AppendLine(@"Content-Length: " + data.Length);
                                builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                builder.AppendLine(@"");
                                builder.AppendLine(@"" + data);
                                await SendPacketAsync(builder.ToString());
                                builder.Clear();
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletTransaction:
                            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(splitPacket[1]))
                            {
                                if (int.TryParse(splitPacket[2], out var transactionIndex))
                                {
                                    if (transactionIndex > 0)
                                    {
                                        transactionIndex--;

                                        string transaction = ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletTransactionSyncByIndex(transactionIndex);
                                        if (transaction != null)
                                        {
                                            var splitTransaction = transaction.Split(new[] { "#" }, StringSplitOptions.None);

                                            var transactionJsonObject = new ClassApiJsonTransaction()
                                            {
                                                index = (transactionIndex + 1),
                                                wallet_address = splitPacket[1],
                                                mode = splitTransaction[0],
                                                type = splitTransaction[1],
                                                hash = splitTransaction[2],
                                                wallet_dst_or_src = splitTransaction[3],
                                                amount = decimal.Parse(splitTransaction[4], NumberStyles.Currency, Program.GlobalCultureInfo),
                                                fee = decimal.Parse(splitTransaction[5], NumberStyles.Currency, Program.GlobalCultureInfo),
                                                timestamp_send = long.Parse(splitTransaction[6]),
                                                timestamp_recv = long.Parse(splitTransaction[7]),
                                                blockchain_height = splitTransaction[8]
                                            };

                                            string data = JsonConvert.SerializeObject(transactionJsonObject);
                                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            {
                                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                            }
                                            StringBuilder builder = new StringBuilder();
                                            builder.AppendLine(@"HTTP/1.1 200 OK");
                                            builder.AppendLine(@"Content-Type: text/plain");
                                            builder.AppendLine(@"Content-Length: " + data.Length);
                                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                            builder.AppendLine(@"");
                                            builder.AppendLine(@"" + data);
                                            await SendPacketAsync(builder.ToString());
                                            builder.Clear();
                                        }
                                        else
                                        {
                                            await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                        }
                                    }
                                    else
                                    {
                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                    }
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletAnonymousTransaction:
                            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(splitPacket[1]))
                            {
                                if (int.TryParse(splitPacket[2], out var transactionIndex))
                                {
                                    if (transactionIndex > 0)
                                    {
                                        transactionIndex--;

                                        string transaction = ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletAnonymousTransactionSyncByIndex(transactionIndex);
                                        if (transaction != null)
                                        {
                                            var splitTransaction = transaction.Split(new[] { "#" }, StringSplitOptions.None);

                                            var transactionJsonObject = new ClassApiJsonTransaction()
                                            {
                                                index = (transactionIndex + 1),
                                                wallet_address = splitPacket[1],
                                                mode = splitTransaction[0],
                                                type = splitTransaction[1],
                                                hash = splitTransaction[2],
                                                wallet_dst_or_src = splitTransaction[3],
                                                amount = decimal.Parse(splitTransaction[4], NumberStyles.Currency, Program.GlobalCultureInfo),
                                                fee = decimal.Parse(splitTransaction[5], NumberStyles.Currency, Program.GlobalCultureInfo),
                                                timestamp_send = long.Parse(splitTransaction[6]),
                                                timestamp_recv = long.Parse(splitTransaction[7]),
                                                blockchain_height = splitTransaction[8]
                                            };

                                            string data = JsonConvert.SerializeObject(transactionJsonObject);
                                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                            {
                                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                            }
                                            StringBuilder builder = new StringBuilder();
                                            builder.AppendLine(@"HTTP/1.1 200 OK");
                                            builder.AppendLine(@"Content-Type: text/plain");
                                            builder.AppendLine(@"Content-Length: " + data.Length);
                                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                            builder.AppendLine(@"");
                                            builder.AppendLine(@"" + data);
                                            await SendPacketAsync(builder.ToString());
                                            builder.Clear();
                                        }
                                        else
                                        {
                                            await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                        }
                                    }
                                    else
                                    {
                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                    }
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetWalletTransactionByHash:
                            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(splitPacket[1]))
                            {


                                var transaction = ClassRpcDatabase.RpcDatabaseContent[splitPacket[1]].GetWalletAnyTransactionSyncByHash(splitPacket[2]);
                                if (transaction != null)
                                {
                                    var splitTransaction = transaction.Item2.Split(new[] { "#" }, StringSplitOptions.None);

                                    var transactionJsonObject = new ClassApiJsonTransaction()
                                    {
                                        index = (transaction.Item1+1),
                                        wallet_address = splitPacket[1],
                                        mode = splitTransaction[0],
                                        type = splitTransaction[1],
                                        hash = splitTransaction[2],
                                        wallet_dst_or_src = splitTransaction[3],
                                        amount = decimal.Parse(splitTransaction[4], NumberStyles.Currency, Program.GlobalCultureInfo),
                                        fee = decimal.Parse(splitTransaction[5], NumberStyles.Currency, Program.GlobalCultureInfo),
                                        timestamp_send = long.Parse(splitTransaction[6]),
                                        timestamp_recv = long.Parse(splitTransaction[7]),
                                        blockchain_height = splitTransaction[8]
                                    };

                                    string data = JsonConvert.SerializeObject(transactionJsonObject);
                                    if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                    {
                                        data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                    }
                                    StringBuilder builder = new StringBuilder();
                                    builder.AppendLine(@"HTTP/1.1 200 OK");
                                    builder.AppendLine(@"Content-Type: text/plain");
                                    builder.AppendLine(@"Content-Length: " + data.Length);
                                    builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                    builder.AppendLine(@"");
                                    builder.AppendLine(@"" + data);
                                    await SendPacketAsync(builder.ToString());
                                    builder.Clear();
                                }
                                else
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                }
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.WalletNotExist);
                            }
                            break;
                        case ClassApiEnumeration.GetTransactionByHash:
                            {
                                bool found = false;

                                foreach(string walletAddress in ClassRpcDatabase.RpcDatabaseContent.Keys.ToArray())
                                {
                                    if (_cancellationTokenSource.IsCancellationRequested)
                                        break;

                                    var transaction = ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletAnyTransactionSyncByHash(splitPacket[1]);
                                    if (transaction != null)
                                    {
                                        found = true;
                                        var splitTransaction = transaction.Item2.Split(new[] { "#" }, StringSplitOptions.None);

                                        var transactionJsonObject = new ClassApiJsonTransaction()
                                        {
                                            index = (transaction.Item1 + 1),
                                            wallet_address = walletAddress,
                                            mode = splitTransaction[0],
                                            type = splitTransaction[1],
                                            hash = splitTransaction[2],
                                            wallet_dst_or_src = splitTransaction[3],
                                            amount = decimal.Parse(splitTransaction[4], NumberStyles.Currency, Program.GlobalCultureInfo),
                                            fee = decimal.Parse(splitTransaction[5], NumberStyles.Currency, Program.GlobalCultureInfo),
                                            timestamp_send = long.Parse(splitTransaction[6]),
                                            timestamp_recv = long.Parse(splitTransaction[7]),
                                            blockchain_height = splitTransaction[8]
                                        };

                                        string data = JsonConvert.SerializeObject(transactionJsonObject);
                                        if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                        {
                                            data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                        }
                                        StringBuilder builder = new StringBuilder();
                                        builder.AppendLine(@"HTTP/1.1 200 OK");
                                        builder.AppendLine(@"Content-Type: text/plain");
                                        builder.AppendLine(@"Content-Length: " + data.Length);
                                        builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                        builder.AppendLine(@"");
                                        builder.AppendLine(@"" + data);
                                        await SendPacketAsync(builder.ToString());
                                        builder.Clear();

                                        break;
                                    }
                                    
                                }
                                
                                if (!found || _cancellationTokenSource.IsCancellationRequested)
                                {
                                    await BuildAndSendHttpPacketAsync(ClassApiEnumeration.IndexNotExist);
                                }
                            }
                            break;
                        default:
                            await BuildAndSendHttpPacketAsync(ClassApiEnumeration.PacketNotExist);
                            break;
                    }
                }
                else
                {
                    string data;
                    StringBuilder builder;
                    switch (packet)
                    {
                        case ClassApiEnumeration.ClearTask:
                            if (ClassRpcSetting.RpcWalletEnableApiTaskScheduler)
                            {
                                var clearTaskResult = ClassApiTaskScheduler.ClearTaskScheduledComplete();

                                var clearTaskJsonObject = new ClassApiJsonTaskClearResult();
                                
                                if (clearTaskResult.Item1)
                                {
                                    clearTaskJsonObject.result = ClassApiEnumeration.ClearTaskComplete;
                                    clearTaskJsonObject.total_task_cleared = clearTaskResult.Item2;
                                }
                                else
                                {
                                    clearTaskJsonObject.result = ClassApiEnumeration.ClearTaskError;
                                    clearTaskJsonObject.total_task_cleared = clearTaskResult.Item2;
                                }
                                data = JsonConvert.SerializeObject(clearTaskJsonObject);
                                if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                                {
                                    data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                                }
                                builder = new StringBuilder();
                                builder.AppendLine(@"HTTP/1.1 200 OK");
                                builder.AppendLine(@"Content-Type: text/plain");
                                builder.AppendLine(@"Content-Length: " + data.Length);
                                builder.AppendLine(@"Access-Control-Allow-Origin: *");
                                builder.AppendLine(@"");
                                builder.AppendLine(@"" + data);
                                await SendPacketAsync(builder.ToString());
                                builder.Clear();
                            }
                            else
                            {
                                await BuildAndSendHttpPacketAsync(ClassApiEnumeration.TaskDisabled);
                            }
                            break;
                        case ClassApiEnumeration.GetTotalWalletTransactionSync:

                            var totalTransactionSyncObject = new ClassApiJsonTotalTransactionSync()
                            {
                                result = ClassSyncDatabase.DatabaseTransactionSync.Count
                            };

                            data = JsonConvert.SerializeObject(totalTransactionSyncObject);
                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                            {
                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                            }
                            builder = new StringBuilder();
                            builder.AppendLine(@"HTTP/1.1 200 OK");
                            builder.AppendLine(@"Content-Type: text/plain");
                            builder.AppendLine(@"Content-Length: " + data.Length);
                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                            builder.AppendLine(@"");
                            builder.AppendLine(@"" + data);
                            await SendPacketAsync(builder.ToString());
                            builder.Clear();
                            break;
                        case ClassApiEnumeration.GetTotalWalletIndex:

                            var totalWalletObject = new ClassApiJsonTotalWalletCount()
                            {
                                result = ClassRpcDatabase.RpcDatabaseContent.Count
                            };
                            data = JsonConvert.SerializeObject(totalWalletObject);
                            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
                            {
                                data = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, data, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
                            }
                            builder = new StringBuilder();
                            builder.AppendLine(@"HTTP/1.1 200 OK");
                            builder.AppendLine(@"Content-Type: text/plain");
                            builder.AppendLine(@"Content-Length: " + data.Length);
                            builder.AppendLine(@"Access-Control-Allow-Origin: *");
                            builder.AppendLine(@"");
                            builder.AppendLine(@"" + data);
                            await SendPacketAsync(builder.ToString());
                            builder.Clear();
                            break;
                        case ClassApiEnumeration.CreateWallet:
                            using (var walletCreatorObject = new ClassWalletCreator())
                            {
                                await Task.Factory.StartNew(async delegate
                                {
                                    if (!await walletCreatorObject.StartWalletConnectionAsync(ClassWalletPhase.Create, ClassUtility.MakeRandomWalletPassword()))
                                    {
                                        ClassConsole.ConsoleWriteLine("RPC Wallet cannot create a new wallet.", ClassConsoleColorEnumeration.IndexConsoleRedLog, Program.LogLevel);
                                    }
                                }, _cancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Current).ConfigureAwait(false);

                                while (walletCreatorObject.WalletCreateResult == ClassWalletCreatorEnumeration.WalletCreatorPending)
                                {
                                    await Task.Delay(100);
                                }
                                switch (walletCreatorObject.WalletCreateResult)
                                {
                                    case ClassWalletCreatorEnumeration.WalletCreatorError:
                                        await BuildAndSendHttpPacketAsync(ClassApiEnumeration.CreateWalletError);
                                        break;
                                    case ClassWalletCreatorEnumeration.WalletCreatorSuccess:
                                        await BuildAndSendHttpPacketAsync(walletCreatorObject.WalletAddressResult);
                                        break;
                                }
                            }
                            break;
                        default:
                            await BuildAndSendHttpPacketAsync(ClassApiEnumeration.PacketNotExist);
                            break;
                    }
                }
            }
            catch(Exception error)
            {
#if DEBUG
                Console.WriteLine("HandlePacketHttpAsync exception | " + error.Message);
#endif
                Dictionary<string, string> dictionaryException = new Dictionary<string, string>()
                {
                    { "result", ClassApiEnumeration.PacketError },
                    { "exception", error.Message }
                };
                await BuildAndSendHttpPacketAsync(null, true, dictionaryException);
            }
        }

        /// <summary>
        /// build and send http packet to client.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="multiResult"></param>
        /// <param name="dictionaryContent"></param>
        /// <returns></returns>
        private async Task BuildAndSendHttpPacketAsync(string content, bool multiResult = false, Dictionary<string, string> dictionaryContent = null)
        {
            string contentToSend;
            if (!multiResult)
            {
                contentToSend = BuildJsonString(content);
            }
            else
            {
                contentToSend = BuildFullJsonString(dictionaryContent);
            }
            if (ClassRpcSetting.RpcWalletApiKeyRequestEncryption != string.Empty)
            {
                contentToSend = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, contentToSend, ClassRpcSetting.RpcWalletApiKeyRequestEncryption, ClassWalletNetworkSetting.KeySize);
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(@"HTTP/1.1 200 OK");
            builder.AppendLine(@"Content-Type: application/json");
            builder.AppendLine(@"Content-Length: " + contentToSend.Length);
            builder.AppendLine(@"Access-Control-Allow-Origin: *");
            builder.AppendLine(@"");
            builder.AppendLine(@"" + contentToSend);
            await SendPacketAsync(builder.ToString());
            builder.Clear();
        }

        /// <summary>
        /// Return content converted for json.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string BuildJsonString(string content)
        {
            JObject jsonContent = new JObject
            {
                { "result", content },
                { "version", Assembly.GetExecutingAssembly().GetName().Version.ToString() },
                { "date_packet", DateTimeOffset.Now.ToUnixTimeSeconds() }
            };
            return JsonConvert.SerializeObject(jsonContent);
        }

        /// <summary>
        /// Return content converted for json.
        /// </summary>
        /// <param name="dictionaryContent"></param>
        /// <returns></returns>
        private string BuildFullJsonString(Dictionary<string, string> dictionaryContent)
        {
            JObject jsonContent = new JObject();
            foreach (var content in dictionaryContent)
            {
                jsonContent.Add(content.Key, content.Value);
            }
            jsonContent.Add("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            jsonContent.Add("date_packet", DateTimeOffset.Now.ToUnixTimeSeconds());
            return JsonConvert.SerializeObject(jsonContent);
        }

        /// <summary>
        /// Send packet to client.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task SendPacketAsync(string packet)
        {
            try
            {

                using (var networkStream = new NetworkStream(_client.Client))
                {
                    using (BufferedStream bufferedStreamNetwork = new BufferedStream(networkStream, ClassConnectorSetting.MaxNetworkPacketSize))
                    {
                        var bytePacket = Encoding.UTF8.GetBytes(packet);
                        await bufferedStreamNetwork.WriteAsync(bytePacket, 0, bytePacket.Length).ConfigureAwait(false);
                        await bufferedStreamNetwork.FlushAsync().ConfigureAwait(false);
                    }
                }
            }
            catch
            {
                // Ignored
            }
        }
    }
}
