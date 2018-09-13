﻿/*
* Copyright © 2017-2018 Cloudveil Technology Inc.  
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using NETWORKLIST;
using NLog;
using System;
using System.Runtime.InteropServices.ComTypes;

namespace Citadel.Core.Windows.Util.Net
{
    /// <summary>
    /// Handler for network state change notifications.
    /// </summary>
    public delegate void ConnectionStateChangeHandler();

    /// <summary>
    /// Class we use to analayze network state information. This is a bit repetative but abstracts
    /// away our underlying implementation.
    /// </summary>
    public class NetworkStatus
    {
        public event ConnectionStateChangeHandler ConnectionStateChanged;

        private static NetworkStatus s_instance;

        static NetworkStatus()
        {
            s_instance = new NetworkStatus();            
        }

        public static NetworkStatus Default
        {
            get
            {
                return s_instance;
            }
        }

        private NetworkListUtil m_nListUtil;

        /// <summary>
        /// Gets whether or not the device has internet access that is not proxied nor behind a
        /// captive portal.
        /// </summary>
        public bool HasUnencumberedInternetAccess
        {
            get
            {
                return (HasIpv4InetConnection || HasIpv6InetConnection) && (!BehindIPv4CaptivePortal && !BehindIPv6CaptivePortal) && (!BehindIPv4Proxy && !BehindIPv6Proxy);
            }
        }

        /// <summary>
        /// Gets whether or not any of the device IPV4 connections have detected that they are behind a
        /// captive portal.
        /// </summary>
        public bool BehindIPv4CaptivePortal
        {
            get
            {
                return m_nListUtil.BehindIPv4CaptivePortal;
            }
        }

        /// <summary>
        /// Gets whether or not any of the device IPV6 connections have detected that they are behind a
        /// captive portal.
        /// </summary>
        public bool BehindIPv6CaptivePortal
        {
            get
            {
                return m_nListUtil.BehindIPv6CaptivePortal;
            }
        }

        /// <summary>
        /// Gets whether or not any of the device IPV4 connections have detected that they are behind a
        /// proxy.
        /// </summary>
        public bool BehindIPv4Proxy
        {
            get
            {
                return m_nListUtil.BehindIPv4Proxy;
            }
        }

        /// <summary>
        /// Gets whether or not any of the device IPV6 connections have detected that they are behind a
        /// proxy.
        /// </summary>
        public bool BehindIPv6Proxy
        {
            get
            {
                return m_nListUtil.BehindIPv6Proxy;
            }
        }

        /// <summary>
        /// Gets whether or not any of the device IPV4 connections have been determined to be capable
        /// of reaching the internet.
        /// </summary>
        public bool HasIpv4InetConnection
        {
            get
            {
                return m_nListUtil.HasIpv4InetConnection;
            }
        }

        /// <summary>
        /// Gets whether or not any of the device IPV6 connections have been determined to be capable
        /// of reaching the internet.
        /// </summary>
        public bool HasIpv6InetConnection
        {
            get
            {
                return m_nListUtil.HasIpv6InetConnection;
            }
        }

        /// <summary>
        /// Private ctor since we're cheezy and use a singleton.
        /// </summary>
        private NetworkStatus()
        {
            m_nListUtil = new NetworkListUtil();

            m_nListUtil.ConnectionStateChanged += () =>
            {
                ConnectionStateChanged?.Invoke();
            };
        }
    }

    /// <summary>
    /// Internal class that hooks into the NETWORKLIST COM object for analyzing aspects of our
    /// network connections.
    /// </summary>
    internal class NetworkListUtil : INetworkListManagerEvents, INetworkEvents, INetworkConnectionEvents
    {
        private Logger m_logger;

        public event ConnectionStateChangeHandler ConnectionStateChanged;

        private enum NLM_INTERNET_CONNECTIVITY : uint
        {
            NLM_INTERNET_CONNECTIVITY_WEBHIJACK = 0x1,
            NLM_INTERNET_CONNECTIVITY_PROXIED = 0x2,
            NLM_INTERNET_CONNECTIVITY_CORPORATE = 0x4
        };

        /// <summary>
        /// Iterates over all IPV4 connections and checks to see if the
        /// NLM_INTERNET_CONNECTIVITY_WEBHIJACK flag is set for that connnection. Will return true on
        /// the very first connection that has this flag set. False if no connections have this flag set.
        /// </summary>
        public bool BehindIPv4CaptivePortal
        {
            get
            {
                foreach(INetwork nw in m_networkListManager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED))
                {
                    try
                    {
                        INetwork network = m_networkListManager.GetNetwork(nw.GetNetworkId());
                        IPropertyBag nwp = (IPropertyBag)network;

                        object resv4;
                        nwp.RemoteRead("NA_InternetConnectivityV4", out resv4, null, 0, null);
                        NLM_INTERNET_CONNECTIVITY resv4Cast = (NLM_INTERNET_CONNECTIVITY)resv4;

                        if(resv4Cast.HasFlag(NLM_INTERNET_CONNECTIVITY.NLM_INTERNET_CONNECTIVITY_WEBHIJACK))
                        {
                            return true;
                        }
                    }
                    catch(Exception e)
                    {
                        LoggerUtil.RecursivelyLogException(m_logger, e);
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Iterates over all IPV6 connections and checks to see if the
        /// NLM_INTERNET_CONNECTIVITY_WEBHIJACK flag is set for that connnection. Will return true on
        /// the very first connection that has this flag set. False if no connections have this flag set.
        /// </summary>
        public bool BehindIPv6CaptivePortal
        {
            get
            {
                foreach(INetwork nw in m_networkListManager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED))
                {
                    try
                    {
                        INetwork network = m_networkListManager.GetNetwork(nw.GetNetworkId());
                        IPropertyBag nwp = (IPropertyBag)network;

                        object resv6;
                        nwp.RemoteRead("NA_InternetConnectivityV6", out resv6, null, 0, null);
                        NLM_INTERNET_CONNECTIVITY resv6Cast = (NLM_INTERNET_CONNECTIVITY)resv6;

                        if(resv6Cast.HasFlag(NLM_INTERNET_CONNECTIVITY.NLM_INTERNET_CONNECTIVITY_WEBHIJACK))
                        {
                            return true;
                        }
                    }
                    catch(Exception e)
                    {
                        LoggerUtil.RecursivelyLogException(m_logger, e);
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Iterates over all IPV4 connections and checks to see if the
        /// NLM_INTERNET_CONNECTIVITY_PROXIED flag is set for that connnection. Will return true on
        /// the very first connection that has this flag set. False if no connections have this flag set.
        /// </summary>
        public bool BehindIPv4Proxy
        {
            get
            {
                foreach(INetwork nw in m_networkListManager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED))
                {
                    try
                    {
                        INetwork network = m_networkListManager.GetNetwork(nw.GetNetworkId());
                        IPropertyBag nwp = (IPropertyBag)network;

                        object resv4;
                        nwp.RemoteRead("NA_InternetConnectivityV4", out resv4, null, 0, null);
                        NLM_INTERNET_CONNECTIVITY resv4Cast = (NLM_INTERNET_CONNECTIVITY)resv4;

                        if(resv4Cast.HasFlag(NLM_INTERNET_CONNECTIVITY.NLM_INTERNET_CONNECTIVITY_PROXIED))
                        {
                            return true;
                        }
                    }
                    catch(Exception e)
                    {
                        LoggerUtil.RecursivelyLogException(m_logger, e);
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Iterates over all IPV6 connections and checks to see if the
        /// NLM_INTERNET_CONNECTIVITY_PROXIED flag is set for that connnection. Will return true on
        /// the very first connection that has this flag set. False if no connections have this flag set.
        /// </summary>
        public bool BehindIPv6Proxy
        {
            get
            {
                foreach(INetwork nw in m_networkListManager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED))
                {
                    try
                    {
                        INetwork network = m_networkListManager.GetNetwork(nw.GetNetworkId());
                        IPropertyBag nwp = (IPropertyBag)network;

                        object resv6;
                        nwp.RemoteRead("NA_InternetConnectivityV6", out resv6, null, 0, null);
                        NLM_INTERNET_CONNECTIVITY resv6Cast = (NLM_INTERNET_CONNECTIVITY)resv6;

                        if(resv6Cast.HasFlag(NLM_INTERNET_CONNECTIVITY.NLM_INTERNET_CONNECTIVITY_PROXIED))
                        {
                            return true;
                        }
                    }
                    catch(Exception e)
                    {
                        LoggerUtil.RecursivelyLogException(m_logger, e);
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Iterates over all IPV4 connections and checks to see if the
        /// NLM_CONNECTIVITY_IPV4_INTERNET flag is set for that connnection, which means that Windows
        /// has determined that the internet is reachable from the connection in question. Will
        /// return true on the very first connection that has this flag set. False if no connections
        /// have this flag set, which means no internet detected.
        /// </summary>
        public bool HasIpv4InetConnection
        {
            get
            {
                foreach(INetwork nw in m_networkListManager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED))
                {
                    try
                    {
                        if(nw.GetConnectivity().HasFlag(NLM_CONNECTIVITY.NLM_CONNECTIVITY_IPV4_INTERNET))
                        {
                            return true;
                        }
                    }
                    catch(Exception e)
                    {
                        LoggerUtil.RecursivelyLogException(m_logger, e);
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Iterates over all IPV6 connections and checks to see if the
        /// NLM_CONNECTIVITY_IPV6_INTERNET flag is set for that connnection, which means that Windows
        /// has determined that the internet is reachable from the connection in question. Will
        /// return true on the very first connection that has this flag set. False if no connections
        /// have this flag set, which means no internet detected.
        /// </summary>
        public bool HasIpv6InetConnection
        {
            get
            {
                foreach(INetwork nw in m_networkListManager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED))
                {
                    try
                    {
                        if(nw.GetConnectivity().HasFlag(NLM_CONNECTIVITY.NLM_CONNECTIVITY_IPV6_INTERNET))
                        {
                            return true;
                        }
                    }
                    catch(Exception e)
                    {
                        LoggerUtil.RecursivelyLogException(m_logger, e);
                    }
                }

                return false;
            }
        }

        private INetworkListManager m_networkListManager;
        private IConnectionPoint m_connectionPoint;
        private int m_cookie = 0;

        public NetworkListUtil()
        {
            m_logger = LoggerUtil.GetAppWideLogger();

            m_networkListManager = new NETWORKLIST.NetworkListManager();
            ConnectToNetworkListManagerEvents();
        }

        /// <summary>
        /// Bit of a hack because of our singleton but we want to make sure
        /// we disconnect from COM stuff.
        /// </summary>
        ~NetworkListUtil()
        {   
            DisconnectFromNetworkListManagerEvents();
        }

        private void ConnectToNetworkListManagerEvents()
        {
            IConnectionPointContainer icpc = (IConnectionPointContainer)m_networkListManager;
            Guid typeGuid = typeof(INetworkListManagerEvents).GUID;
            icpc.FindConnectionPoint(ref typeGuid, out m_connectionPoint);
            m_connectionPoint.Advise(this, out m_cookie);
        }

        private void DisconnectFromNetworkListManagerEvents()
        {
            m_connectionPoint.Unadvise(m_cookie);
        }

        public void ConnectivityChanged(NLM_CONNECTIVITY newConnectivity)
        {
            ConnectionStateChanged?.Invoke();
        }

        public void NetworkConnectivityChanged(Guid networkId, NLM_CONNECTIVITY newConnectivity)
        {
            ConnectionStateChanged?.Invoke();            
        }

        public void NetworkPropertyChanged(Guid networkId, NLM_NETWORK_PROPERTY_CHANGE Flags)
        {
            // Don't care.
        }

        public void NetworkConnectionConnectivityChanged(Guid connectionId, NLM_CONNECTIVITY newConnectivity)
        {
            // Don't care.
        }

        public void NetworkConnectionPropertyChanged(Guid connectionId, NLM_CONNECTION_PROPERTY_CHANGE Flags)
        {
            // Don't care.
        }

        public void NetworkAdded(Guid networkId)
        {
            // Don't care.
        }

        public void NetworkDeleted(Guid networkId)
        {
            // Don't care.
        }
    }
}