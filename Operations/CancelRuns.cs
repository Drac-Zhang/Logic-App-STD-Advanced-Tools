﻿using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void CancelRuns(string workflowName)
        {
            AlertExperimentalFeature();

            string query = $"Status eq 'Running' or Status eq 'Waiting'";
            List<TableEntity> inprocessRuns = TableOperations.QueryRunTable(workflowName, query, new string[] { "Status", "PartitionKey", "RowKey" });

            if (inprocessRuns.Count == 0)
            {
                throw new UserInputException($"There's no running/waiting runs of workflow {workflowName}");
            }

            Console.WriteLine($"Found {inprocessRuns.Count} run(s) in run table.");

            string confirmationMessage = "WARNING!!!\r\n1. Cancel all the running instances will cause data lossing for any running/waiting instances.\r\n2. Run history and resubmit feature will be unavailable for all waiting runs.\r\ninput for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            string prefix = GenerateWorkflowTablePrefix(workflowName);
            string runTableName = $"flow{prefix}runs";

            TableClient runTableClient = new TableClient(AppSettings.ConnectionString, runTableName);
            
            int CancelledCount = 0;
            int FailedCount = 0;

            foreach (TableEntity te in inprocessRuns)
            {
                TableEntity updatedEntity = new TableEntity
                {
                    { "Status", "Cancelled" }
                };

                updatedEntity.PartitionKey = te.PartitionKey;
                updatedEntity.RowKey = te.RowKey;

                try
                {
                    runTableClient.UpdateEntity<TableEntity>(updatedEntity, te.ETag);
                    CancelledCount++;
                }
                catch (Exception ex)
                {
                    FailedCount++;
                }
            }

            Console.WriteLine($"{CancelledCount} runns cancelled sucessfully");

            if (FailedCount != 0)
            { 
                Console.WriteLine($"{FailedCount} runs cancelled failed due to running instances status changed, please run command again to verify whether still have running instance or not.");
            }
        }
    }
}