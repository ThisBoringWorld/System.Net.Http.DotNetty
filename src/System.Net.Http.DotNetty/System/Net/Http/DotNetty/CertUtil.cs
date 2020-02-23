using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http.DotNetty
{
    /// <summary>
    /// 证书工具
    /// </summary>
    public static class CertUtil
    {
        #region Public 方法

        /// <summary>
        /// 通过本机证书检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool CheckByLocalMachineCerts(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (certificate is null || chain is null)
            {
                return false;
            }

            X509Certificate2 cacert = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
            using (X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var trustedCerts = store.Certificates.Find(X509FindType.FindByThumbprint, cacert.Thumbprint, true);
                return trustedCerts.Count > 0;
            };
        }

        #endregion Public 方法
    }
}