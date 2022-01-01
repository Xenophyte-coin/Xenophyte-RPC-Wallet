﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xenophyte_Connector_All.Utils;
using Xenophyte_Connector_All.Wallet;
using Xenophyte_Rpc_Wallet.ConsoleObject;
using Xenophyte_Rpc_Wallet.Setting;
using Xenophyte_Rpc_Wallet.Utility;
using Xenophyte_Rpc_Wallet.Wallet;


namespace Xenophyte_Rpc_Wallet.Database
{
    public class ClassRpcDatabaseEnumeration
    {
        public const string DatabaseWalletStartLine = "[WALLET]";
    }

    public class ClassRpcDatabase
    {
        private static string _rpcDatabasePassword; // This password permit to decrypt each lines of the database.
        private const string RpcDatabaseFile = "\\rpcdata.xenodb"; // Content every wallet informations.
        private const string RpcDatabaseFileBackup = "\\rpcdata-bak.xenodb"; // Backup content of every wallet informations.
        private const string RpcDatabaseBackupDirectory = "\\Backup\\";
        public static Dictionary<string, ClassWalletObject> RpcDatabaseContent; // Content every wallets (wallet address and public key only)
        private static StreamWriter _rpcDatabaseStreamWriter; // Permit to keep alive a stream writer for write a new wallet information created.
        public static bool InSave;
        public static bool InSaveBackup;
        public static bool PasswordIsSetByArgument;
        private static CancellationTokenSource _cancellationTokenTaskBackupDatatabase;

        /// <summary>
        /// Set rpc database password.
        /// </summary>
        /// <param name="password"></param>
        public static void SetRpcDatabasePassword(string password)
        {
            _rpcDatabasePassword = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, password, password, ClassWalletNetworkSetting.KeySize); // Encrypt the password with the password.
        }

        /// <summary>
        /// Load RPC Database file.
        /// </summary>
        /// <returns></returns>
        public static bool LoadRpcDatabaseFile()
        {
            RpcDatabaseContent = new Dictionary<string, ClassWalletObject>();
            try
            {
                if (File.Exists(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile)))
                {
                    using (FileStream fs = File.Open(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (BufferedStream bs = new BufferedStream(fs))
                        {
                            using (StreamReader sr = new StreamReader(bs))
                            {
                                string line;
                                int lineRead = 0;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    lineRead++;
                                    if (line.StartsWith(ClassRpcDatabaseEnumeration.DatabaseWalletStartLine))
                                    {
                                        string walletData = line.Replace(ClassRpcDatabaseEnumeration.DatabaseWalletStartLine, "");
                                        walletData = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael, walletData, _rpcDatabasePassword, ClassWalletNetworkSetting.KeySize);
                                        if (walletData != ClassAlgoErrorEnumeration.AlgoError)
                                        {
                                            var splitWalletData = walletData.Split(new[] { "|" }, StringSplitOptions.None);
                                            var walletAddress = splitWalletData[0];
                                            if (!RpcDatabaseContent.ContainsKey(walletAddress))
                                            {
                                                var walletPublicKey = splitWalletData[1];
                                                var walletPrivateKey = splitWalletData[2];
                                                var walletPinCode = splitWalletData[3];
                                                var walletPassword = splitWalletData[4];
                                                var walletObject = new ClassWalletObject(walletAddress, walletPublicKey, walletPassword, walletPrivateKey, walletPinCode, walletData);
                                                if (!RpcDatabaseContent.ContainsKey(walletAddress))
                                                {
                                                    RpcDatabaseContent.Add(walletAddress, walletObject);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ClassConsole.ConsoleWriteLine("Decrypt database of wallets failed at line: " + lineRead);
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    File.Create(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile)).Close();
                }
            }
            catch
            {
                return false;
            }
            _rpcDatabaseStreamWriter = new StreamWriter(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile), true, Encoding.UTF8, 8192) { AutoFlush = true };
            return true;
        }

        /// <summary>
        /// Force to save whole databases of wallets.
        /// </summary>
        public static async Task<bool> SaveWholeRpcWalletDatabaseFile()
        {
            while (InSave)
            {
                await Task.Delay(100);
            }
            InSave = true;
            bool success = false;
            ClassConsole.ConsoleWriteLine("RPC Wallet on saving wallet database, please wait a moment..", ClassConsoleColorEnumeration.IndexConsoleRedLog, Program.LogLevel);
            while (!success)
            {
                try
                {
                    try
                    {
                        _rpcDatabaseStreamWriter?.Close();
                        _rpcDatabaseStreamWriter?.Dispose();
                    }
                    catch
                    {
                        // Ignored
                    }

                    File.Copy(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile), ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFileBackup + "-" + DateTimeOffset.Now.ToUnixTimeMilliseconds())); // Backup wallet database just in case.

                    File.Create(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile)).Close();

                    _rpcDatabaseStreamWriter = new StreamWriter(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile), true, Encoding.UTF8, 8192) { AutoFlush = true };

                    foreach (var wallet in RpcDatabaseContent.ToArray())
                    {
                        string encryptedWallet = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, wallet.Value.GetWalletAddress() + "|" + wallet.Value.GetWalletPublicKey() + "|" + wallet.Value.GetWalletPrivateKey() + "|" + wallet.Value.GetWalletPinCode() + "|" + wallet.Value.GetWalletPassword(), _rpcDatabasePassword, ClassWalletNetworkSetting.KeySize);
                        _rpcDatabaseStreamWriter.WriteLine(ClassRpcDatabaseEnumeration.DatabaseWalletStartLine + encryptedWallet);
                    }
                    success = true;
                }
                catch
                {
                    _rpcDatabaseStreamWriter = new StreamWriter(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile), true, Encoding.UTF8, 8192) { AutoFlush = true };
                }
            }
            ClassConsole.ConsoleWriteLine("RPC Wallet on saving wallet database saved successfully.", ClassConsoleColorEnumeration.IndexConsoleGreenLog, Program.LogLevel);

            InSave = false;
            return true;
        }

        /// <summary>
        /// Insert a new wallet informations to the datbases file.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="walletPublicKey"></param>
        /// <param name="walletPrivateKey"></param>
        /// <param name="walletPinCode"></param>
        /// <param name="walletPassword"></param>
        public static async void InsertNewWalletAsync(string walletAddress, string walletPublicKey, string walletPrivateKey, string walletPinCode, string walletPassword)
        {
            await Task.Factory.StartNew(delegate
            {
                InSave = true;
                bool success = false;
                ClassConsole.ConsoleWriteLine("RPC Wallet on saving wallet database, please wait a moment..", ClassConsoleColorEnumeration.IndexConsoleRedLog, Program.LogLevel);
                string encryptedWallet = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, walletAddress + "|" + walletPublicKey + "|" + walletPrivateKey + "|" + walletPinCode + "|" + walletPassword, _rpcDatabasePassword, ClassWalletNetworkSetting.KeySize);

                while (!success)
                {
                    try
                    {
                        if (!RpcDatabaseContent.ContainsKey(walletAddress))
                        {
                            var walletObject = new ClassWalletObject(walletAddress, walletPublicKey, walletPassword, walletPrivateKey, walletPinCode, encryptedWallet);
                            RpcDatabaseContent.Add(walletAddress, walletObject);
                        }
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                }
                success = false;
                while (!success)
                {
                    try
                    {
                        _rpcDatabaseStreamWriter.WriteLine(ClassRpcDatabaseEnumeration.DatabaseWalletStartLine + encryptedWallet);
                        success = true;
                    }
                    catch
                    {
                        _rpcDatabaseStreamWriter = new StreamWriter(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseFile), true, Encoding.UTF8, 8192) { AutoFlush = true };
                        success = false;
                    }
                }
                InSave = false;
                ClassConsole.ConsoleWriteLine("RPC Wallet on saving wallet database saved successfully.", ClassConsoleColorEnumeration.IndexConsoleGreenLog, Program.LogLevel);
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
        }

        /// <summary>
        /// Enable Backup Wallet Database System.
        /// </summary>
        public static void EnableBackupWalletDatabaseSystem()
        {
            _cancellationTokenTaskBackupDatatabase = new CancellationTokenSource();

            try
            {
                Task.Factory.StartNew(async delegate
                {
                    ClassConsole.ConsoleWriteLine("RPC Wallet Backup system enabled.", ClassConsoleColorEnumeration.IndexConsoleGreenLog, Program.LogLevel);

                    if (!Directory.Exists(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseBackupDirectory)))
                    {
                        Directory.CreateDirectory(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseBackupDirectory));
                    }

                    while (!Program.Exit)
                    {
                        try
                        {
                            string backupFileName = RpcDatabaseFile + "-" + DateTimeOffset.Now.ToUnixTimeSeconds().ToString("F0");
                            string backupFilePath = ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseBackupDirectory + backupFileName);
                            if (!File.Exists(backupFilePath))
                            {
                                InSaveBackup = true;
                                File.Create(backupFilePath).Close();
                                using (StreamWriter writer = new StreamWriter(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseBackupDirectory + backupFileName)))
                                {
                                    foreach (var wallet in RpcDatabaseContent.ToArray())
                                    {
                                        string encryptedWallet = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, wallet.Value.GetWalletAddress() + "|" + wallet.Value.GetWalletPublicKey() + "|" + wallet.Value.GetWalletPrivateKey() + "|" + wallet.Value.GetWalletPinCode() + "|" + wallet.Value.GetWalletPassword(), _rpcDatabasePassword, ClassWalletNetworkSetting.KeySize);
                                        writer.WriteLine(ClassRpcDatabaseEnumeration.DatabaseWalletStartLine + encryptedWallet);
                                    }
                                }
                                ClassConsole.ConsoleWriteLine("RPC Wallet save successfully backup of wallet database in: " + backupFilePath);
                            }
                            if (ClassRpcSetting.WalletEnableAutoRemoveBackupSystem)
                            {
                                var fileNames = Directory.EnumerateFiles(ClassUtility.ConvertPath(AppDomain.CurrentDomain.BaseDirectory + RpcDatabaseBackupDirectory), "*.*", SearchOption.TopDirectoryOnly);
                                if (fileNames.Count() > 0)
                                {
                                    foreach (var backupFileNameSaved in fileNames)
                                    {
                                        DateTime backupFileNameSavedDate = File.GetLastWriteTime(backupFileNameSaved);
                                        long backupFileNameSavedDateSecond = ((long)(backupFileNameSavedDate - new DateTime(1970, 1, 1)).TotalSeconds);
                                        if (DateTimeOffset.Now.ToUnixTimeSeconds() - backupFileNameSavedDateSecond > ClassRpcSetting.WalletBackupLapsingTimeLimit)
                                        {
                                            File.Delete(backupFileNameSaved);
                                            ClassConsole.ConsoleWriteLine("Old backup file removed: " + backupFileNameSaved);
                                        }
                                    }
                                }

                            }
                        }
                        catch (Exception error)
                        {
#if DEBUG
                            ClassConsole.ConsoleWriteLine("RPC Wallet save error backup of wallet database | Exception: " + error.Message, ClassConsoleColorEnumeration.IndexConsoleRedLog, ClassConsoleLogLevelEnumeration.LogLevelGeneral);
#endif
                        }
                        InSaveBackup = false;
                        await Task.Delay(ClassRpcSetting.WalletIntervalBackupSystem * 1000);
                    }
                }, _cancellationTokenTaskBackupDatatabase.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Catch the exception once the Task is cancelled
            }
        }

        /// <summary>
        /// Stop backup wallet database system.
        /// </summary>
        public static void StopBackupWalletDatabaseSystem()
        {
            try
            {
                if (_cancellationTokenTaskBackupDatatabase != null)
                {
                    if (!_cancellationTokenTaskBackupDatatabase.IsCancellationRequested)
                    {
                        _cancellationTokenTaskBackupDatatabase.Cancel();
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
