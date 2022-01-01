﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xenophyte_Connector_All.Utils;
using Xenophyte_Connector_All.Wallet;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Xenophyte_Rpc_Wallet.Wallet
{
    public class ClassWalletRestoreFunctions : IDisposable
    {
        /// <summary>
        /// Dispose information.
        /// </summary>
        private bool IsDisposed;

        #region Dispose functions

        ~ClassWalletRestoreFunctions()
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
            if (IsDisposed)
                return;

            IsDisposed = true;
        }

        #endregion


        /// <summary>
        /// Generate QR Code from private key + password, encrypt the QR Code bitmap with the private key, build the request to be send on the blockchain.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public string GenerateQrCodeKeyEncryptedRepresentation(string privateKey, string password)
        {
            try
            {
                QrCodeEncodingOptions options = new QrCodeEncodingOptions
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 2,
                    Height = 2,
                };

                BarcodeWriter qr = new BarcodeWriter
                {
                    Options = options,
                    Format = BarcodeFormat.QR_CODE
                };
                string sourceKey = privateKey.Trim() + "|" + password.Trim() + "|"+DateTimeOffset.Now.ToUnixTimeSeconds();
                using (var representationQrCode = new Bitmap(qr.Write(sourceKey)))
                {

                    LuminanceSource source = new BitmapLuminanceSource(representationQrCode);

                    BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
                    Result result = new MultiFormatReader().decode(bitmap);

                    if (result != null)
                    {
                        if (result.Text == sourceKey)
                        {

                            string qrCodeString = BitmapToBase64String(representationQrCode);
                            string qrCodeStringEncrypted = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, qrCodeString, privateKey, ClassWalletNetworkSetting.KeySize);
                            string qrCodeEncryptedRequest;

                            if (privateKey.Contains("$"))
                            {
                                long walletUniqueIdInstance = long.Parse(privateKey.Split(new[] { "$" }, StringSplitOptions.None)[1]);
                                qrCodeEncryptedRequest = walletUniqueIdInstance + "|" + qrCodeStringEncrypted;
                            }
                            else
                            {

                                string randomEndPrivateKey = privateKey.Remove(0, (privateKey.Length - ClassUtils.GetRandomBetween(privateKey.Length / 4, privateKey.Length / 8))); // Indicate only a small part of the end of the private key (For old private key users).
                                qrCodeEncryptedRequest = randomEndPrivateKey + "|" + qrCodeStringEncrypted;
                            }
                            string decryptQrCode = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael, qrCodeStringEncrypted, privateKey, ClassWalletNetworkSetting.KeySize);

                            using (Bitmap qrCode = Base64StringToBitmap(decryptQrCode))
                            {

                                source = new BitmapLuminanceSource(qrCode);

                                bitmap = new BinaryBitmap(new HybridBinarizer(source));
                                result = new MultiFormatReader().decode(bitmap);

                                if (result != null)
                                {
                                    if (result.Text == sourceKey)
                                    {
                                        return qrCodeEncryptedRequest;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Console.WriteLine("error to generate qr code encryption, exception: " + error.Message);
#endif
            }
            return null;
        }

        /// <summary>
        /// Convert a bitmap into byte array then in base64 string.
        /// </summary>
        /// <param name="newImage"></param>
        /// <returns></returns>
        public string BitmapToBase64String(Bitmap newImage)
        {

            Bitmap bImage = newImage;
            using (MemoryStream ms = new MemoryStream())
            {
                bImage.Save(ms, ImageFormat.Jpeg);
                byte[] byteImage = ms.ToArray();
                return Convert.ToBase64String(byteImage);
            }
        }

        /// <summary>
        /// Convert a base64 string into byte array, then into bitmap.
        /// </summary>
        /// <param name="stringImage"></param>
        /// <returns></returns>
        public Bitmap Base64StringToBitmap(string stringImage)
        {
            byte[] byteStringImage = Convert.FromBase64String(stringImage);

            using (var ms = new MemoryStream(byteStringImage))
            {
                return new Bitmap(ms);
            }
        }

    }
}
