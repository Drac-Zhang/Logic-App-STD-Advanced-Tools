﻿using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Storage.Blobs;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void CheckConnectivity(string logicAppName)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(ConnectionString);
            ConnectionValidator connectionValidator = new ConnectionValidator(connectionInfo, logicAppName);

            List<ValidationInfo> results = connectionValidator.Validate();

            if (results != null) 
            {
                ConsoleTable consoleTable = new ConsoleTable("EndPoint", "Type", "DNS Test", "Endpoint IP", "TCP Port (443)", "Auth Status");

                foreach (ValidationInfo result in results)
                { 
                    string endpoint = result.Endpoint;

                    consoleTable.AddRow(endpoint, result.ServiceType.ToString(), result.DNSStatus.ToString(), result.GetIPsAsString(), result.PingStatus.ToString(), result.AuthStatus.ToString());
                }

                consoleTable.Print();
            }
        }

        public class ConnectionValidator
        {
            private string LogicAppName;
            private ConnectionInfo ConnectionInfo;
            private List<ValidationInfo> Results;
            public ConnectionValidator(ConnectionInfo connectionInfo, string logicAppName)
            {
                ConnectionInfo = connectionInfo;
                LogicAppName = logicAppName;

                Results = new List<ValidationInfo>
                {
                    new ValidationInfo(connectionInfo.BlobEndpoint, StorageType.Blob),
                    new ValidationInfo(connectionInfo.FileEndpoint, StorageType.File),
                    new ValidationInfo(connectionInfo.QueueEndpoint, StorageType.Queue),
                    new ValidationInfo(connectionInfo.TableEndpoint, StorageType.Table)
                };
            }

            //temp resolution, need to improve in the future
            public List<ValidationInfo> Validate()
            {
                foreach (ValidationInfo info in Results)
                {
                    try
                    {
                        IPAddress[] ipAddressess = Dns.GetHostAddresses(info.Endpoint);

                        info.IPs = ipAddressess;
                        info.DNSStatus = ValidateStatus.Succeeded;
                    }
                    catch
                    {
                        info.DNSStatus = ValidateStatus.Failed;
                    }
                }

                foreach (ValidationInfo info in Results)
                {
                    if (info.DNSStatus != ValidateStatus.Succeeded)
                    {
                        continue;
                    }

                    try
                    {
                        foreach (IPAddress ip in info.IPs)
                        {
                            using (TcpClient client = new TcpClient(ip.ToString(), 443)) { };
                        }

                        info.PingStatus = ValidateStatus.Succeeded;
                        
                    }
                    catch
                    {
                        info.PingStatus = ValidateStatus.Failed;
                    }
                }

                foreach (ValidationInfo info in Results)
                {
                    try
                    {
                        if (info.PingStatus != ValidateStatus.Succeeded)
                        {
                            continue;
                        }

                        switch (info.ServiceType)
                        {
                            case StorageType.Blob:
                                BlobServiceClient blobClient = new BlobServiceClient(ConnectionString);
                                blobClient.GetProperties();
                                break;
                            case StorageType.File:
                                ShareServiceClient shareClient = new ShareServiceClient(ConnectionString);
                                shareClient.GetProperties();
                                break;
                            case StorageType.Queue:
                                QueueServiceClient queueClient = new QueueServiceClient(ConnectionString);
                                queueClient.GetProperties();
                                break;
                            case StorageType.Table:
                                TableServiceClient tableClient = new TableServiceClient(ConnectionString);
                                tableClient.GetProperties();
                                break;
                            default: break;
                        }

                        info.AuthStatus = ValidateStatus.Succeeded;
                    }
                    catch
                    { 
                        info.AuthStatus = ValidateStatus.Failed;
                    }
                }

                return Results;
            }
        }

        public enum StorageType
        { 
            Blob = 1,
            File = 2,
            Queue = 4,
            Table = 8
        }

        public enum ValidateStatus
        { 
            Succeeded,
            Failed,
            NotApplicable
        }

        public class ValidationInfo
        {
            public string Endpoint;
            public StorageType ServiceType;
            public IPAddress[] IPs;
            public ValidateStatus DNSStatus;
            public ValidateStatus PingStatus;
            public ValidateStatus AuthStatus;

            public ValidationInfo(string endpoint, StorageType serviceType)
            {
                Endpoint = endpoint;
                ServiceType = serviceType;
                DNSStatus = ValidateStatus.NotApplicable;
                PingStatus = ValidateStatus.NotApplicable;
                AuthStatus = ValidateStatus.NotApplicable;
            }

            public string GetIPsAsString()
            {
                if (IPs != null)
                {
                    string IPsAsString = "";
                    foreach (IPAddress ip in IPs)
                    {
                        IPsAsString += ip.ToString() + " ";
                    }

                    return IPsAsString.TrimEnd();
                }

                return "N/A";
            }
        }

        public class ConnectionInfo
        {
            private Dictionary<string, string> CSInfo;
            public ConnectionInfo(string connectionString) 
            {
                CSInfo = new Dictionary<string, string>();

                string[] Infos = connectionString.Split(";");
                foreach (string Info in Infos)
                {
                    string[] KV = Info.Split("=");
                    CSInfo.Add(KV[0], KV[1]);
                }

                BlobEndpoint = $"{AccountName}.blob.{EndpointSuffix}";
                FileEndpoint = $"{AccountName}.file.{EndpointSuffix}";
                QueueEndpoint = $"{AccountName}.queue.{EndpointSuffix}";
                TableEndpoint = $"{AccountName}.table.{EndpointSuffix}";
            }

            public string BlobEndpoint { get; private set; }
            public string FileEndpoint { get; private set; }
            public string QueueEndpoint { get; private set; }
            public string TableEndpoint { get; private set; }

            public string DefaultEndpointsProtocol
            {
                get 
                {
                    return CSInfo["DefaultEndpointsProtocol"];
                }
            }

            public string AccountName
            {
                get
                {
                    return CSInfo["AccountName"];
                }
            }

            public string AccountKey
            {
                get
                {
                    return CSInfo["AccountKey"];
                }
            }

            public string EndpointSuffix
            {
                get
                {
                    return CSInfo["EndpointSuffix"];
                }
            }
        }
    }
}