﻿using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Globalization;

namespace LogicAppAdvancedTool.Operations
{
    public static class CleanUpContainers
    {
        public static void Run(string workflowName, string date)
        {
            int targetDate = Int32.Parse(date);
            string formattedDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            string containerPrefix;
            if (string.IsNullOrEmpty(workflowName))
            {
                containerPrefix = CommonOperations.GenerateLogicAppPrefix();
            }
            else
            {
                containerPrefix = CommonOperations.GenerateWorkflowTablePrefix(workflowName);
            }

            containerPrefix = $"flow{containerPrefix}";

            BlobServiceClient client = new BlobServiceClient(AppSettings.ConnectionString);
            List<BlobContainerItem> containers = client.GetBlobContainers(BlobContainerTraits.Metadata, BlobContainerStates.None, containerPrefix).ToList();

            if (containers.Count == 0)
            {
                Console.WriteLine($"No blob containers found.");
            }

            List<string> containerList = containers
                                            .Where(x => int.Parse(x.Name.Substring(34, 8)) < targetDate)
                                            .Select(s => s.Name)
                                            .ToList();

            Console.WriteLine($"There are {containerList.Count} containers found, please enter \"P\" to print the list or press any other key to continue without print list");
            if (Console.ReadLine().ToLower() == "p")
            {
                ConsoleTable table = new ConsoleTable("Contianer Name");

                foreach (string containerName in containerList)
                {
                    table.AddRow(containerName);
                }

                table.Print();
            }

            string confirmationMessage = $"WARNING!!!\r\nDeleted those container will cause run history data lossing which executed before {formattedDate} \r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            foreach (string containerName in containerList)
            { 
                client.DeleteBlobContainer(containerName);
            }

            Console.WriteLine("Clean up succeeded");
        }
    }
}
