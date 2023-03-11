using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace PrivateServerConnectTool
{
    class Proxy
    {
        private bool isProxyStarted;

        private ExplicitProxyEndPoint explicitEndPoint;

        private ProxyServer proxyServer;

        private Config m_CurrentConfig;

        public Proxy()
        {
            isProxyStarted = false;
        }

        public bool IsInternalProxyEnabled()
        {
            if (isProxyStarted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsExternalProxyEnabled()
        {
            try
            {
                // Metode 1
                RegistryKey? registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                if (registry != null)
                {
                    object? v = registry.GetValue("ProxyEnable");
                    if (v != null && (int)v == 1)
                    {
                        // Metode 2
                        if (!isProxyStarted)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Does not support proxy check!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public void SetSystemProxyStatus(bool enable)
        {
            try
            {
                // Metode 1
                RegistryKey? registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                if (registry != null)
                {
                    object? v = registry.GetValue("ProxyEnable");
                    if (v != null)
                    {
                        if (enable)
                        {
                            registry.SetValue("ProxyEnable", 1);
                        }
                        else
                        {
                            registry.SetValue("ProxyEnable", 0);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Does not support proxy check!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public bool Start(Config config)
        {
            m_CurrentConfig = config;

            proxyServer = new ProxyServer();

            // Install Certificate
            UninstallCertificate();

            proxyServer.CertificateManager.EnsureRootCertificate();

            proxyServer.CertificateManager.TrustRootCertificate(true);

            // Get Request Data
            proxyServer.BeforeRequest += OnRequest;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;

            try
            {
                //Tool.findAndKillProcessRuningOn("" + port + "");
            }
            catch (Exception ex)
            {
                // skip
            }

            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Parse("127.0.0.1"), m_CurrentConfig.proxyPort, true);

            // Fired when a CONNECT request is received
            explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;

            // An explicit endpoint is where the client knows about the existence of a proxy So client sends request in a proxy friendly manner
            try
            {
                proxyServer.AddEndPoint(explicitEndPoint);
                proxyServer.Start();
            }
            catch (Exception ex)
            {
                // https://stackoverflow.com/a/41340197/3095372
                // https://stackoverflow.com/a/69051680/3095372
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Error Start Proxy: {0}", ex.InnerException.Message);
                }
                else
                {
                    Console.WriteLine("Error Start Proxy: {0}", ex.Message);
                }
                return false;
            }

            foreach (var endPoint in proxyServer.ProxyEndPoints)
            {
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);
            }

            // Only explicit proxies can be set as system proxy!
            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

            isProxyStarted = true;

            return true;
        }

        public void Stop()
        {
            if (!isProxyStarted)
            {
                return;
            }
            if (proxyServer == null)
            {
                return;
            }

            try
            {
                explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
                proxyServer.BeforeRequest -= OnRequest;
                proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Stop Proxy: ", ex);
            }
            finally
            {
                if (proxyServer.ProxyRunning)
                {
                    Console.WriteLine("Proxy Stop");
                    proxyServer.Stop();
                    //UninstallCertificate();
                    proxyServer.Dispose();
                    isProxyStarted = false;
                }
                else
                {
                    Console.WriteLine("Proxy tries to stop but the proxy is not running.");
                }
            }
        }

        public void UninstallCertificate()
        {
            proxyServer.CertificateManager.RemoveTrustedRootCertificate();
            proxyServer.CertificateManager.RemoveTrustedRootCertificateAsAdmin();
        }

        private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            // Do not decrypt SSL if not required domain/host
            string hostname = e.HttpClient.Request.RequestUri.Host;
            if (
                hostname.Contains("yuanshen.com") |
                hostname.Contains("hoyoverse.com") |
                hostname.Contains("mihoyo.com") |
                hostname.Contains(m_CurrentConfig.serverIPAddress))
            {
                e.DecryptSsl = true;
            }
            else
            {
                e.DecryptSsl = false;
            }
        }

        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            // Change Host
            string hostname = e.HttpClient.Request.RequestUri.Host;
            if (
                hostname.Contains("yuanshen.com") |
                hostname.Contains("hoyoverse.com") |
                hostname.Contains("mihoyo.com"))
            {
                var q = e.HttpClient.Request.RequestUri;

                var url = e.HttpClient.Request.Url;

                Console.WriteLine("Request Original: " + url);

                if (!m_CurrentConfig.useHTTPS)
                {
                    url = url.Replace("https", "http");
                }

                url = url.Replace(q.Host, m_CurrentConfig.serverIPAddress);

                Console.WriteLine("Request After " + url);

                // Set
                e.HttpClient.Request.Url = url;
            }
        }

        // Allows overriding default certificate validation logic
        private async Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            e.IsValid = true;
        }

    }
}
