﻿using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xenophyte_Connector_All.RPC;
using Xenophyte_Connector_All.Setting;
using Xenophyte_Connector_All.Utils;
using Xenophyte_Connector_All.Wallet;
using Xenophyte_Rpc_Wallet.ConsoleObject;
using Xenophyte_Rpc_Wallet.Database;
using Xenophyte_Rpc_Wallet.Setting;

namespace Xenophyte_Rpc_Wallet.Wallet
{
    public class ClassWalletUpdater
    {
        private static Thread _threadAutoUpdateWallet;
        private const string RpcTokenNetworkNotExist = "not_exist";
        private const string RpcTokenNetworkWalletAddressNotExist = "wallet_address_not_exist";
        private const string RpcTokenNetworkWalletBusyOnUpdate = "WALLET-BUSY-ON-UPDATE";
        private static Dictionary<string, int> _listOfSeedNodesSpeed = new Dictionary<string, int>();

        /// <summary>
        /// Enable auto update wallet system.
        /// </summary>
        public static void EnableAutoUpdateWallet()
        {

            if (_threadAutoUpdateWallet != null && (_threadAutoUpdateWallet.IsAlive || _threadAutoUpdateWallet != null))
            {
                _threadAutoUpdateWallet.Abort();
                GC.SuppressFinalize(_threadAutoUpdateWallet);
            }

            _threadAutoUpdateWallet = new Thread(delegate ()
            {
                while (!Program.Exit)
                {
                    try
                    {
                        if (ClassRpcDatabase.RpcDatabaseContent.Count > 0)
                        {
                            string getSeedNodeRandom = string.Empty;
                            bool seedNodeSelected = false;
                            if (ClassConnectorSetting.SeedNodeIp.Count > 1)
                            {
                                foreach (var seedNode in GetSeedNodeSpeedList().ToArray())
                                {
                                    getSeedNodeRandom = seedNode.Key;
                                    Task taskCheckSeedNode = Task.Run(async () => seedNodeSelected = await CheckTcp.CheckTcpClientAsync(seedNode.Key, ClassConnectorSetting.SeedNodeTokenPort));
                                    taskCheckSeedNode.Wait(ClassConnectorSetting.MaxTimeoutConnect);
                                    if (seedNodeSelected)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                getSeedNodeRandom = ClassConnectorSetting.SeedNodeIp.ToArray().ElementAt(0).Key;
                                seedNodeSelected = true;
                            }
                            if (seedNodeSelected)
                            {
                                foreach (var walletObject in ClassRpcDatabase.RpcDatabaseContent.ToArray()) // Copy temporaly the database of wallets in the case of changes on the enumeration done by a parallal process, update all of them.
                                {
                                    try
                                    {
                                        if (Program.Exit)
                                        {
                                            break;
                                        }


                                        if (!walletObject.Value.GetWalletUpdateStatus() && walletObject.Value.GetLastWalletUpdate() <= DateTimeOffset.Now.ToUnixTimeSeconds())
                                        {
                                            ClassRpcDatabase.RpcDatabaseContent[walletObject.Key].SetLastWalletUpdate(DateTimeOffset.Now.ToUnixTimeSeconds() + ClassRpcSetting.WalletUpdateInterval);
                                            ClassRpcDatabase.RpcDatabaseContent[walletObject.Key].SetWalletOnUpdateStatus(true);
                                            UpdateWalletTarget(getSeedNodeRandom, walletObject.Key);
                                        }

                                    }
                                    catch (Exception error)
                                    {
#if DEBUG
                                        Console.WriteLine("Error on update wallet object address: " + walletObject.Key + " | Exception: " + error.Message);
#endif
                                    }
                                }
                            }


                        }
                    }
                    catch (Exception error)
                    {
#if DEBUG
                        Console.WriteLine("EnableAutoUpdateWallet function Exception: " + error.Message);
#endif
                    }
                    Thread.Sleep(ClassRpcSetting.WalletUpdateInterval * 1000);
                }
            });
            _threadAutoUpdateWallet.Start();
        }

        /// <summary>
        /// Update wallet target
        /// </summary>
        /// <param name="getSeedNodeRandom"></param>
        /// <param name="walletAddress"></param>
        private static void UpdateWalletTarget(string getSeedNodeRandom, string walletAddress)
        {
            ThreadPool.QueueUserWorkItem(async delegate
            {

#if DEBUG
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
#endif
                try
                {
                    if (!await GetWalletBalanceTokenAsync(getSeedNodeRandom, walletAddress))
                    {
                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetLastWalletUpdate(0);
#if DEBUG
                        Console.WriteLine("Wallet: " + walletAddress + " update failed. Node: " + getSeedNodeRandom);
#endif
                    }
                    else
                    {
                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetLastWalletUpdate(DateTimeOffset.Now.ToUnixTimeSeconds() + ClassRpcSetting.WalletUpdateInterval);
#if DEBUG
                        Console.WriteLine("Wallet: " + walletAddress + " updated successfully. Node: " + getSeedNodeRandom);
#endif
                    }
                }
                catch (Exception error)
                {
#if DEBUG
                    Console.WriteLine("Error on update wallet: " + walletAddress + " exception: " + error.Message);
#endif
                }
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Wallet: " + walletAddress + " updated in: " + stopwatch.ElapsedMilliseconds + " ms. Node: " + getSeedNodeRandom);
#endif
                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnUpdateStatus(false);

            });
        }

        /// <summary>
        /// Disable auto update wallet system.
        /// </summary>
        public static void DisableAutoUpdateWallet()
        {
            if (_threadAutoUpdateWallet != null && (_threadAutoUpdateWallet.IsAlive || _threadAutoUpdateWallet != null))
            {
                _threadAutoUpdateWallet.Abort();
                GC.SuppressFinalize(_threadAutoUpdateWallet);
            }
        }

        /// <summary>
        /// Update Wallet target
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        public static async Task UpdateWallet(string walletAddress)
        {
            string getSeedNodeRandom = string.Empty;
            bool seedNodeSelected = false;
            foreach (var seedNode in GetSeedNodeSpeedList().ToArray())
            {
                getSeedNodeRandom = seedNode.Key;
                Task taskCheckSeedNode = Task.Run(async () => seedNodeSelected = await CheckTcp.CheckTcpClientAsync(seedNode.Key, ClassConnectorSetting.SeedNodeTokenPort));
                taskCheckSeedNode.Wait(ClassConnectorSetting.MaxTimeoutConnect);
                if (seedNodeSelected)
                {
                    break;
                }
            }
            if (seedNodeSelected)
            {
                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnUpdateStatus(true);
                await GetWalletBalanceTokenAsync(getSeedNodeRandom, walletAddress);
                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnUpdateStatus(false);
            }
        }


        /// <summary>
        /// Get Seed Node list sorted by the faster to the slowest one.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, int> GetSeedNodeSpeedList()
        {
            if (_listOfSeedNodesSpeed.Count == 0)
            {
                foreach (var seedNode in ClassConnectorSetting.SeedNodeIp.ToArray())
                {

                    try
                    {
                        int seedNodeResponseTime = -1;
                        Task taskCheckSeedNode = Task.Run(() => seedNodeResponseTime = CheckPing.CheckPingHost(seedNode.Key, true));
                        taskCheckSeedNode.Wait(ClassConnectorSetting.MaxPingDelay);
                        if (seedNodeResponseTime == -1)
                        {
                            seedNodeResponseTime = ClassConnectorSetting.MaxSeedNodeTimeoutConnect;
                        }
#if DEBUG
                        Console.WriteLine(seedNode.Key + " response time: " + seedNodeResponseTime + " ms.");
#endif
                        _listOfSeedNodesSpeed.Add(seedNode.Key, seedNodeResponseTime);

                    }
                    catch
                    {
                        _listOfSeedNodesSpeed.Add(seedNode.Key, ClassConnectorSetting.MaxSeedNodeTimeoutConnect); // Max delay.
                    }

                }
            }
            else if (_listOfSeedNodesSpeed.Count != ClassConnectorSetting.SeedNodeIp.Count)
            {
                ClassConsole.ConsoleWriteLine("New seed node(s) listed, update the list of seed nodes sorted by their ping time.", ClassConsoleColorEnumeration.IndexConsoleYellowLog);
                var tmpListOfSeedNodesSpeed = new Dictionary<string, int>();
                foreach (var seedNode in ClassConnectorSetting.SeedNodeIp.ToArray())
                {

                    try
                    {
                        int seedNodeResponseTime = -1;
                        Task taskCheckSeedNode = Task.Run(() => seedNodeResponseTime = CheckPing.CheckPingHost(seedNode.Key, true));
                        taskCheckSeedNode.Wait(ClassConnectorSetting.MaxPingDelay);
                        if (seedNodeResponseTime == -1)
                        {
                            seedNodeResponseTime = ClassConnectorSetting.MaxSeedNodeTimeoutConnect;
                        }
#if DEBUG
                        Console.WriteLine(seedNode.Key + " response time: " + seedNodeResponseTime + " ms.");
#endif
                        tmpListOfSeedNodesSpeed.Add(seedNode.Key, seedNodeResponseTime);

                    }
                    catch
                    {
                        tmpListOfSeedNodesSpeed.Add(seedNode.Key, ClassConnectorSetting.MaxSeedNodeTimeoutConnect); // Max delay.
                    }

                }
                _listOfSeedNodesSpeed = tmpListOfSeedNodesSpeed;
                ClassConsole.ConsoleWriteLine("List of seed nodes sorted by their ping time done.");

            }
            return _listOfSeedNodesSpeed.ToArray().OrderBy(u => u.Value).ToDictionary(z => z.Key, y => y.Value);
        }

        /// <summary>
        /// Get wallet token from token system.
        /// </summary>
        /// <param name="getSeedNodeRandom"></param>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        private static async Task<string> GetWalletTokenAsync(string getSeedNodeRandom, string walletAddress)
        {
            string encryptedRequest = ClassRpcWalletCommand.TokenAsk + "|empty-token|" + (DateTimeOffset.Now.ToUnixTimeSeconds() + 1).ToString("F0");
            encryptedRequest = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, encryptedRequest, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword(), ClassWalletNetworkSetting.KeySize);
            string responseWallet = await ProceedTokenRequestHttpAsync("http://" + getSeedNodeRandom + ":" + ClassConnectorSetting.SeedNodeTokenPort + "/" + ClassConnectorSettingEnumeration.WalletTokenType + "|" + walletAddress + "|" + encryptedRequest);
            //string responseWallet = await ProceedTokenRequestTcpAsync(getSeedNodeRandom, ClassConnectorSetting.SeedNodeTokenPort, ClassConnectorSettingEnumeration.WalletTokenType + "|" + walletAddress + "|" + encryptedRequest);

            try
            {
                var responseWalletJson = JObject.Parse(responseWallet);
                responseWallet = responseWalletJson["result"].ToString();
                if (responseWallet != RpcTokenNetworkNotExist)
                {
                    responseWallet = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael, responseWallet, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword(), ClassWalletNetworkSetting.KeySize);
                    var splitResponseWallet = responseWallet.Split(new[] { "|" }, StringSplitOptions.None);
                    if ((long.Parse(splitResponseWallet[splitResponseWallet.Length - 1]) + 10) - DateTimeOffset.Now.ToUnixTimeSeconds() < 60)
                    {
                        if (long.Parse(splitResponseWallet[splitResponseWallet.Length - 1]) + 60 >= DateTimeOffset.Now.ToUnixTimeSeconds())
                        {
                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletCurrentToken(true, splitResponseWallet[1]);
                            return splitResponseWallet[1];
                        }
                        else
                        {
                            return RpcTokenNetworkNotExist;
                        }
                    }
                    else
                    {
                        return RpcTokenNetworkNotExist;
                    }
                }
                else
                {
                    return RpcTokenNetworkNotExist;
                }
            }
            catch(Exception error)
            {
#if DEBUG
                Debug.WriteLine("Exception GetWalletTokenAsync: " + error.Message);
#endif
                return RpcTokenNetworkNotExist;
            }
        }

        /// <summary>
        /// Update wallet balance from token system.
        /// </summary>
        /// <param name="getSeedNodeRandom"></param>
        /// <param name="walletAddress"></param>
        public static async Task<bool> GetWalletBalanceTokenAsync(string getSeedNodeRandom, string walletAddress)
        {
            string token = await GetWalletTokenAsync(getSeedNodeRandom, walletAddress);
            if (token != RpcTokenNetworkNotExist)
            {
                if (ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletCurrentToken().Item1)
                {
                    token = ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletCurrentToken().Item2;
                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletCurrentToken(false, string.Empty);
                    string encryptedRequest = ClassRpcWalletCommand.TokenAskBalance + "|" + token + "|" + (DateTimeOffset.Now.ToUnixTimeSeconds() + 1);
                    encryptedRequest = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, encryptedRequest, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword(), ClassWalletNetworkSetting.KeySize);
                    string responseWallet = await ProceedTokenRequestHttpAsync("http://" + getSeedNodeRandom + ":" + ClassConnectorSetting.SeedNodeTokenPort + "/" + ClassConnectorSettingEnumeration.WalletTokenType + "|" + walletAddress + "|" + encryptedRequest);

                    try
                    {
                        var responseWalletJson = JObject.Parse(responseWallet);
                        responseWallet = responseWalletJson["result"].ToString();
                        if (responseWallet != RpcTokenNetworkNotExist)
                        {
                            responseWallet = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael, responseWallet, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword() + token, ClassWalletNetworkSetting.KeySize);
                            if (responseWallet != ClassAlgoErrorEnumeration.AlgoError)
                            {
                                string walletBalance = responseWallet;
                                var splitWalletBalance = walletBalance.Split(new[] { "|" }, StringSplitOptions.None);
                                if ((long.Parse(splitWalletBalance[splitWalletBalance.Length - 1]) + 10) - DateTimeOffset.Now.ToUnixTimeSeconds() < 60)
                                {
                                    if (long.Parse(splitWalletBalance[splitWalletBalance.Length - 1]) + 10 >= DateTimeOffset.Now.ToUnixTimeSeconds())
                                    {
                                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletBalance(splitWalletBalance[1]);
                                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletPendingBalance(splitWalletBalance[2]);
                                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletUniqueId(splitWalletBalance[3]);
                                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletAnonymousUniqueId(splitWalletBalance[4]);
                                        return true;
                                    }
                                    else
                                    {
                                        ClassConsole.ConsoleWriteLine("Wallet packet time balance token request: " + walletBalance + " is expired.", ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                                    }
                                }
                                else
                                {
                                    ClassConsole.ConsoleWriteLine("Wallet packet time balance token request: " + walletBalance + " is too huge", ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                                }
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        Debug.WriteLine("Exception GetWalletBalanceTokenAsync: " + error.Message);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }


        /// <summary>
        /// Send a transaction from a selected wallet address stored to a specific wallet address target.
        /// </summary>
        /// <param name="getSeedNodeRandom"></param>
        /// <param name="walletAddress"></param>
        /// <param name="walletAddressTarget"></param>
        /// <param name="amount"></param>
        /// <param name="fee"></param>
        /// <param name="anonymous"></param>
        /// <returns></returns>
        private static async Task<string> SendWalletTransactionTokenAsync(string getSeedNodeRandom, string walletAddress, string walletAddressTarget, string amount, string fee, bool anonymous)
        {

            string tokenWallet = await GetWalletTokenAsync(getSeedNodeRandom, walletAddress);
            if (tokenWallet != RpcTokenNetworkNotExist)
            {
                if (ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletCurrentToken().Item1)
                {
                    tokenWallet = ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletCurrentToken().Item2;
                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletCurrentToken(false, string.Empty);

                    string encryptedRequest;
                    if (anonymous)
                    {
                        encryptedRequest = ClassRpcWalletCommand.TokenAskWalletSendTransaction + "|" + tokenWallet + "|" + walletAddressTarget + "|" + amount + "|" + fee + "|1|" + (DateTimeOffset.Now.ToUnixTimeSeconds() + 1).ToString("F0");
                    }
                    else
                    {
                        encryptedRequest = ClassRpcWalletCommand.TokenAskWalletSendTransaction + "|" + tokenWallet + "|" + walletAddressTarget + "|" + amount + "|" + fee + "|0|" + (DateTimeOffset.Now.ToUnixTimeSeconds() + 1).ToString("F0");
                    }
                    encryptedRequest = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, encryptedRequest, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword(), ClassWalletNetworkSetting.KeySize);
                    string responseWallet = await ProceedTokenRequestHttpAsync("http://" + getSeedNodeRandom + ":" + ClassConnectorSetting.SeedNodeTokenPort + "/" + ClassConnectorSettingEnumeration.WalletTokenType + "|" + walletAddress + "|" + encryptedRequest);

                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnUpdateStatus(false);
                    try
                    {
                        var responseWalletJson = JObject.Parse(responseWallet);
                        responseWallet = responseWalletJson["result"].ToString();
                        if (responseWallet != RpcTokenNetworkNotExist)
                        {
                            responseWallet = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael, responseWallet, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword() + tokenWallet, ClassWalletNetworkSetting.KeySize);
                            if (responseWallet != ClassAlgoErrorEnumeration.AlgoError)
                            {
                                string walletTransaction = responseWallet;
                                if (responseWallet != RpcTokenNetworkNotExist)
                                {
                                    var splitWalletTransaction = walletTransaction.Split(new[] { "|" }, StringSplitOptions.None);
                                    if ((long.Parse(splitWalletTransaction[splitWalletTransaction.Length - 1]) + 10) - DateTimeOffset.Now.ToUnixTimeSeconds() < 60)
                                    {
                                        if (long.Parse(splitWalletTransaction[splitWalletTransaction.Length - 1]) + 10 >= DateTimeOffset.Now.ToUnixTimeSeconds())
                                        {
                                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletBalance(splitWalletTransaction[1]);
                                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletPendingBalance(splitWalletTransaction[2]);
                                            ClassConsole.ConsoleWriteLine("Send transaction response " + splitWalletTransaction[0] + " from wallet address " + walletAddress + " of amount " + amount + " " + ClassConnectorSetting.CoinNameMin + " fee " + fee + " " + ClassConnectorSetting.CoinNameMin + " transaction hash: " + splitWalletTransaction[3].ToLower() + " to target -> " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                            return splitWalletTransaction[0] + "|" + splitWalletTransaction[3];
                                        }
                                        return splitWalletTransaction[0] + "|expired_packet";
                                    }
                                }
                                else
                                {
                                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                    return ClassRpcWalletCommand.SendTokenTransactionBusy + "|None";
                                }
                            }
                            else
                            {
                                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                return ClassRpcWalletCommand.SendTokenTransactionBusy + "|None";
                            }
                        }
                        else
                        {
                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                            return ClassRpcWalletCommand.SendTokenTransactionRefused + "|None";
                        }
                    }
                    catch (Exception error)
                    {
#if DEBUG
                    Debug.WriteLine("Exception SendWalletTransactionTokenAsync: " + error.Message);
#endif
                    }
                }
            }

            ClassConsole.ConsoleWriteLine("Send transaction refused from wallet address " + walletAddress + " of amount " + amount + " " + ClassConnectorSetting.CoinNameMin + " fee " + fee + " " + ClassConnectorSetting.CoinNameMin + " to target -> " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
            return ClassRpcWalletCommand.SendTokenTransactionRefused + "|None";
        }


        /// <summary>
        /// Send a transaction from a selected wallet address stored to a specific wallet address target.
        /// </summary>
        /// <param name="getSeedNodeRandom"></param>
        /// <param name="walletAddress"></param>
        /// <param name="walletAddressTarget"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        private static async Task<string> SendWalletTransferTokenAsync(string getSeedNodeRandom, string walletAddress, string walletAddressTarget, string amount)
        {
            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(walletAddressTarget))
            {
                string tokenWallet = await GetWalletTokenAsync(getSeedNodeRandom, walletAddress);
                if (tokenWallet != RpcTokenNetworkNotExist)
                {
                    if (ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletCurrentToken().Item1)
                    {
                        tokenWallet = ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletCurrentToken().Item2;
                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletCurrentToken(false, string.Empty);

                        string privateKeyTarget = ClassRpcDatabase.RpcDatabaseContent[walletAddressTarget].GetWalletPrivateKey(); 
                        if (privateKeyTarget.Contains("$"))
                        {
                            privateKeyTarget = privateKeyTarget.Split(new[] { "$" }, StringSplitOptions.None)[0];
                        }
                        string keyTargetRequest = walletAddressTarget + ClassRpcDatabase.RpcDatabaseContent[walletAddressTarget].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddressTarget].GetWalletPassword() + privateKeyTarget;
                        string encryptedTargetRequest = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, amount, keyTargetRequest, ClassWalletNetworkSetting.KeySize);


                        string encryptedRequest = ClassRpcWalletCommand.TokenAskWalletTransfer + "|" + tokenWallet + "|" + walletAddressTarget + "|" + encryptedTargetRequest + "|" + (DateTimeOffset.Now.ToUnixTimeSeconds() + 1).ToString("F0");
                        encryptedRequest = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, encryptedRequest, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword(), ClassWalletNetworkSetting.KeySize);

                        string responseWallet = await ProceedTokenRequestHttpAsync("http://" + getSeedNodeRandom + ":" + ClassConnectorSetting.SeedNodeTokenPort + "/" + ClassConnectorSettingEnumeration.WalletTokenType + "|" + walletAddress + "|" + encryptedRequest);

                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnUpdateStatus(false);
                        var responseWalletJson = JObject.Parse(responseWallet);
                        responseWallet = responseWalletJson["result"].ToString();
                        if (responseWallet != RpcTokenNetworkNotExist)
                        {
                            responseWallet = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael, responseWallet, walletAddress + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPublicKey() + ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletPassword() + tokenWallet, ClassWalletNetworkSetting.KeySize);
                            if (responseWallet != ClassAlgoErrorEnumeration.AlgoError)
                            {
                                string walletTransaction = responseWallet;
                                if (responseWallet != RpcTokenNetworkNotExist)
                                {
                                    var splitWalletTransaction = walletTransaction.Split(new[] { "|" }, StringSplitOptions.None);
                                    if ((long.Parse(splitWalletTransaction[splitWalletTransaction.Length - 1]) + 10) - DateTimeOffset.Now.ToUnixTimeSeconds() < 60)
                                    {
                                        if (long.Parse(splitWalletTransaction[splitWalletTransaction.Length - 1]) + 10 >= DateTimeOffset.Now.ToUnixTimeSeconds())
                                        {
                                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletBalance(splitWalletTransaction[1]);
                                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletPendingBalance(splitWalletTransaction[2]);
                                            ClassConsole.ConsoleWriteLine("Send transfer response " + splitWalletTransaction[0] + " from wallet address " + walletAddress + " of amount " + amount + " " + ClassConnectorSetting.CoinNameMin + " transaction hash: " + splitWalletTransaction[3].ToLower() + " to target -> " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                            return splitWalletTransaction[0] + "|" + splitWalletTransaction[3];
                                        }
                                        return splitWalletTransaction[0] + "|expired_packet";
                                    }
                                }
                                else
                                {
                                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                    return ClassRpcWalletCommand.SendTokenTransferBusy + "|None";
                                }
                            }
                            else
                            {
                                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                return ClassRpcWalletCommand.SendTokenTransferBusy + "|None";
                            }
                        }
                        else
                        {
                            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                            return ClassRpcWalletCommand.SendTokenTransferRefused + "|None";
                        }
                    }
                }
                else
                {
                    ClassConsole.ConsoleWriteLine("Send transfer refused from wallet address " + walletAddress + " of amount " + amount + " to target -> " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                    return ClassRpcWalletCommand.SendTokenTransferRefused + "|None";
                }
            }
            else
            {
                ClassConsole.ConsoleWriteLine("Send transfer refused from wallet address " + walletAddress + " of amount " + amount + " to target -> " + walletAddressTarget + " | RPC Wallet don't contain wallet informations of: " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);

                return ClassRpcWalletCommand.SendTokenTransactionInvalidTarget + "|None";
            }
            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
            return ClassRpcWalletCommand.SendTokenTransferRefused + "|None";

        }

        /// <summary>
        /// Proceed token request throught http protocol.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<string> ProceedTokenRequestHttpAsync(string url)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.ConnectionLimit = 65535;
            request.KeepAlive = false;
            request.Timeout = ClassRpcSetting.WalletMaxKeepAliveUpdate * 1000;
            request.UserAgent = ClassConnectorSetting.CoinName + " RPC Wallet - " + Assembly.GetExecutingAssembly().GetName().Version + "R";
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();

            }
        }

        /// <summary>
        /// Send a transaction with token system with a selected wallet address, amount and fee.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="amount"></param>
        /// <param name="fee"></param>
        /// <param name="walletAddressTarget"></param>
        /// <param name="anonymous"></param>
        /// <returns></returns>
        public static async Task<string> ProceedTransactionTokenRequestAsync(string walletAddress, string amount, string fee, string walletAddressTarget, bool anonymous)
        {
            if (anonymous)
            {
                ClassConsole.ConsoleWriteLine("Attempt to send an anonymous transaction from wallet address " + walletAddress + " of amount " + amount + " " + ClassConnectorSetting.CoinNameMin + " fee " + fee + " " + ClassConnectorSetting.CoinNameMin + " and anonymous fee option of: " + ClassConnectorSetting.MinimumWalletTransactionAnonymousFee + " " + ClassConnectorSetting.CoinNameMin + " to target -> " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
            }
            else
            {
                ClassConsole.ConsoleWriteLine("Attempt to send transaction from wallet address " + walletAddress + " of amount " + amount + " " + ClassConnectorSetting.CoinNameMin + " fee " + fee + " " + ClassConnectorSetting.CoinNameMin + " to target -> " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
            }
            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(walletAddress))
            {
                if (!ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletUpdateStatus() && !ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletOnSendTransactionStatus())
                {
                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(true);
                    decimal balanceFromDatabase = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletBalance().Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo);
                    decimal balanceFromRequest = decimal.Parse(amount.Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo);
                    decimal feeFromRequest = decimal.Parse(fee.Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo);

                    if (balanceFromRequest + feeFromRequest <= balanceFromDatabase)
                    {

                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetLastWalletUpdate(DateTimeOffset.Now.ToUnixTimeSeconds());
                        string getSeedNodeRandom = string.Empty;
                        bool seedNodeSelected = false;
                        foreach (var seedNode in GetSeedNodeSpeedList().ToArray())
                        {
                            getSeedNodeRandom = seedNode.Key;
                            Task taskCheckSeedNode = Task.Run(async () => seedNodeSelected = await CheckTcp.CheckTcpClientAsync(seedNode.Key, ClassConnectorSetting.SeedNodeTokenPort));
                            taskCheckSeedNode.Wait(ClassConnectorSetting.MaxTimeoutConnect);
                            if (seedNodeSelected)
                            {
                                break;
                            }
                        }
                        if (seedNodeSelected)
                        {
                            try
                            {
                                return await SendWalletTransactionTokenAsync(getSeedNodeRandom, walletAddress, walletAddressTarget, amount, fee, anonymous);
                            }
                            catch (Exception error)
                            {
                                ClassConsole.ConsoleWriteLine("Error on send transaction from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " exception: " + error.Message, ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
#if DEBUG
                                Console.WriteLine("Error on send transaction from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " exception: " + error.Message);
#endif
                                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                return ClassRpcWalletCommand.SendTokenTransactionRefused + "|None";
                            }
                        }

                        ClassConsole.ConsoleWriteLine("Error on send transaction from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " exception: can't connect on each seed nodes checked.", ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                        return ClassRpcWalletCommand.SendTokenTransactionRefused + "|None";
                    }

                    ClassConsole.ConsoleWriteLine("Error on send transaction from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " amount insufficient.", ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                    return ClassRpcWalletCommand.SendTokenTransactionRefused + "|None";
                }

                if (ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletUpdateStatus())
                {
                    return RpcTokenNetworkWalletBusyOnUpdate + "|None";
                }

                return ClassRpcWalletCommand.SendTokenTransactionBusy + "|None";
            }

            return RpcTokenNetworkWalletAddressNotExist + "|None";

        }

        /// <summary>
        /// Send a transaction with token system with a selected wallet address, amount and fee.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="amount"></param>
        /// <param name="walletAddressTarget"></param>
        /// <returns></returns>
        public static async Task<string> ProceedTransferTokenRequestAsync(string walletAddress, string amount, string walletAddressTarget)
        {

            ClassConsole.ConsoleWriteLine("Attempt to send transfer from wallet address " + walletAddress + " of amount " + amount + " to target -> " + walletAddressTarget, ClassConsoleColorEnumeration.IndexConsoleYellowLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);

            if (ClassRpcDatabase.RpcDatabaseContent.ContainsKey(walletAddress))
            {
                if (!ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletUpdateStatus() && !ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletOnSendTransactionStatus())
                {
                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(true);

                    decimal balanceFromDatabase = decimal.Parse(ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletBalance().Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo);
                    decimal balanceFromRequest = decimal.Parse(amount.Replace(".", ","), NumberStyles.Currency, Program.GlobalCultureInfo);
                    if (balanceFromRequest <= balanceFromDatabase)
                    {
                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetLastWalletUpdate(DateTimeOffset.Now.ToUnixTimeSeconds());
                        string getSeedNodeRandom = string.Empty;
                        bool seedNodeSelected = false;
                        foreach (var seedNode in GetSeedNodeSpeedList().ToArray())
                        {
                            getSeedNodeRandom = seedNode.Key;
                            var random = getSeedNodeRandom;
                            Task taskCheckSeedNode = Task.Run(async () => seedNodeSelected = await CheckTcp.CheckTcpClientAsync(random, ClassConnectorSetting.SeedNodeTokenPort));
                            taskCheckSeedNode.Wait(ClassConnectorSetting.MaxTimeoutConnect);
                            if (seedNodeSelected)
                            {
                                break;
                            }
                        }
                        if (seedNodeSelected)
                        {
                            try
                            {
                                return await SendWalletTransferTokenAsync(getSeedNodeRandom, walletAddress, walletAddressTarget, amount);
                            }
                            catch (Exception error)
                            {
                                ClassConsole.ConsoleWriteLine("Error on send transfer from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " exception: " + error.Message, ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
#if DEBUG
                                Console.WriteLine("Error on send transaction from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " exception: " + error.Message);
#endif
                                ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                                return ClassRpcWalletCommand.SendTokenTransferRefused + "|None";
                            }
                        }

                        ClassConsole.ConsoleWriteLine("Error on send transfer from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " exception: can't connect on each seed nodes checked.", ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                        ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                        return ClassRpcWalletCommand.SendTokenTransferRefused + "|None";
                    }

                    ClassConsole.ConsoleWriteLine("Error on send transfer from wallet: " + ClassRpcDatabase.RpcDatabaseContent[walletAddress] + " amount insufficient.", ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelWalletObject);
                    ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
                    return ClassRpcWalletCommand.SendTokenTransferRefused + "|None";
                }

                if (ClassRpcDatabase.RpcDatabaseContent[walletAddress].GetWalletUpdateStatus())
                {
                    return RpcTokenNetworkWalletBusyOnUpdate + "|None";
                }

                return ClassRpcWalletCommand.SendTokenTransferBusy + "|None";
            }
            ClassRpcDatabase.RpcDatabaseContent[walletAddress].SetWalletOnSendTransactionStatus(false);
            return RpcTokenNetworkWalletAddressNotExist + "|None";

        }
    }
}
