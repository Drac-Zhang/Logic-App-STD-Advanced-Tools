﻿using Azure.Data.Tables;
using Newtonsoft.Json;
using System;

namespace LogicAppAdvancedTool.Structures
{
    #region Run history action content data structure
    public class ConnectorPayloadStructure
    {
        public NestedContentLinks nestedContentLinks { get; set; }
    }

    public class NestedContentLinks
    {
        public CommonPayloadStructure body { get; set; }
    }

    public class CommonPayloadStructure
    {
        public string inlinedContent { get; set; }
        public string contentVersion { get; set; }
        public int contentSize { get; set; }
        public ContentHash contentHash { get; set; }
        public string uri { get; set; }
    }

    public class ContentHash
    {
        public string algorithm { get; set; }
        public string value { get; set; }
    }

    public class ActionError
    {
        public string code { get; set; }
        public string message { get; set; }
    }
    #endregion

    public class HistoryRecords
    {
        public DateTimeOffset Timestamp { get; private set; }
        public string ActionName { get; private set; }
        public string Code { get; private set; }
        public dynamic InputContent { get; private set; }
        public dynamic OutputContent { get; private set; }
        public ActionError Error { get; private set; }
        public string RepeatItemName { get; private set; }
        public int? RepeatItemIdenx { get; private set; }
        public string ActionRepetitionName { get; private set; }

        [JsonIgnore]
        public CommonPayloadStructure InputsLink { get; private set; }
        [JsonIgnore]
        public CommonPayloadStructure OutputsLink { get; private set; }

        public HistoryRecords(TableEntity tableEntity)
        {
            this.Timestamp = tableEntity.GetDateTimeOffset("Timestamp") ?? DateTimeOffset.MinValue;
            this.ActionName = tableEntity.GetString("ActionName");
            this.Code = tableEntity.GetString("Code");
            this.RepeatItemName = tableEntity.GetString("RepeatItemScopeName");
            this.RepeatItemIdenx = tableEntity.GetInt32("RepeatItemIndex");
            this.ActionRepetitionName = tableEntity.GetString("ActionRepetitionName");

            this.InputContent = new ContentDecoder(tableEntity.GetBinary("InputsLinkCompressed")).ActualContent;
            this.OutputContent = new ContentDecoder(tableEntity.GetBinary("OutputsLinkCompressed")).ActualContent;

            string rawError = CommonOperations.DecompressContent(tableEntity.GetBinary("Error"));
            this.Error = String.IsNullOrEmpty(rawError) ? null : JsonConvert.DeserializeObject<ActionError>(rawError);
        }
    }

    public class ActionPayload
    {
        public DateTimeOffset Timestamp { get; private set; }
        public string ActionName { get; private set; }
        public dynamic InputContent { get; private set; }
        public dynamic OutputContent { get; private set; }
        public int? RepeatItemIdenx { get; private set; }
        public string FlowRunSequenceId { get; private set; }

        public ActionPayload(TableEntity tableEntity)
        {
            this.Timestamp = tableEntity.GetDateTimeOffset("Timestamp") ?? DateTimeOffset.MinValue;
            this.ActionName = tableEntity.GetString("ActionName") ?? tableEntity.GetString("TriggerName");
            this.RepeatItemIdenx = tableEntity.GetInt32("RepeatItemIndex");
            this.InputContent = new ContentDecoder(tableEntity.GetBinary("InputsLinkCompressed")).ActualContent;
            this.OutputContent = new ContentDecoder(tableEntity.GetBinary("OutputsLinkCompressed")).ActualContent;
            this.FlowRunSequenceId = tableEntity.GetString("FlowRunSequenceId");
        }
    }

    public class RunHistoryInBlob
    { 
        public RunHistoryInBlobInner content { get; set; }
    }

    public class RunHistoryInBlobInner
    {
        [JsonProperty(PropertyName = "$content-type")]
        public string ContentType { get; set; }

        [JsonProperty(PropertyName = "$content")]
        public string Content { get; set; }
    }
}
