using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace test_jira_sandbox.Models
{
    public class JiraAdfDescription
    {
        [JsonProperty("content")]
        public List<JiraContent>? Content;

        [JsonProperty("type")]
        public readonly string? Type = "doc";

        [JsonProperty("version")]
        public readonly int? Version = 1;
    }

    public class JiraAdfDescriptionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(JiraAdfDescription);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var description = reader.Value?.ToString();

            return new JiraAdfDescription
            {
                Content =
            [
                new JiraContent
                {
                    Content =
                    [
                        new JiraContent
                        {
                            Text = description,
                            Type = JiraContentType.Text
                        }
                    ],
                    Type = JiraContentType.Paragraph
                }
            ]
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jiraAdfDescription = value as JiraAdfDescription;

            writer.WriteStartObject();
            writer.WritePropertyName("content");
            serializer.Serialize(writer, jiraAdfDescription?.Content);
            writer.WritePropertyName("type");
            writer.WriteValue(jiraAdfDescription?.Type);
            writer.WritePropertyName("version");
            writer.WriteValue(jiraAdfDescription?.Version);
            writer.WriteEndObject();
        }
    }

    public class JiraContent
    {
        [JsonProperty("content")]
        public List<JiraContent>? Content;

        [JsonProperty("text")]
        public string? Text;

        private string? _type;

        [JsonProperty("type")]
        public string? Type
        {
            get => _type;
            set
            {
                if (!JiraContentType.IsValid(value))
                {
                    throw new ArgumentException("Invalid JiraContentType value");
                }
                _type = value;
            }
        }
    }

    public class JiraContentType
    {
        public const string Paragraph = "paragraph";
        public const string Text = "text";

        public static bool IsValid(string? value)
        {
            return value == Paragraph || value == Text;
        }
    }

    public class JiraField
    {
        [JsonProperty("key")]
        public string? Key;

        [JsonProperty("id")]
        public string? Id;

        [JsonProperty("name")]
        public string? Name;

        [JsonProperty("type")]
        public string? Type;
    }

    public class JiraId
    {
        [JsonProperty("id")]
        public string? Id;
    }

    public class JiraPayload
    {
        [JsonProperty("fields")]
        public JiraPayloadFields? Fields;

        public JiraPayload(string? jsonData, JsonSerializerSettings? jsonSerializerSettings)
        {
            Fields = JsonConvert.DeserializeObject<JiraPayloadFields>(jsonData, jsonSerializerSettings);
        }

        public bool IsValidJiraPayload()
        {
            return Fields?.Project?.Id != null &&
                   Fields?.Project?.Id != string.Empty &&
                   Fields?.Project?.Id?.Length > 0 &&
                   Fields?.Summary != null &&
                   Fields?.Summary != string.Empty &&
                   Fields?.Summary?.Length > 0 &&
                   Fields?.IssueType?.Id != null &&
                   Fields?.IssueType?.Id != string.Empty &&
                   Fields?.IssueType?.Id?.Length > 0;
        }

        public string ReasonForInvalidJiraPayload()
        {
            if (Fields?.Project?.Id == null ||
                   Fields?.Project?.Id == string.Empty ||
                   Fields?.Project?.Id?.Length == 0)
            {
                return $"project id is invalid: {Fields?.Project?.Id}";
            }
            else if (Fields?.Summary == null ||
                   Fields?.Summary == string.Empty ||
                   Fields?.Summary?.Length == 0)
            {
                return $"summary is invalid: {Fields?.Summary}";
            }
            else if (Fields?.IssueType?.Id == null ||
                   Fields?.IssueType?.Id == string.Empty ||
                   Fields?.IssueType?.Id?.Length == 0)
            {
                return $"issue type id is invalid: {Fields?.IssueType?.Id}";
            }

            return $"the {nameof(ReasonForInvalidJiraPayload)} method has likely not been updated to include all required fields";
        }

        public string SerializeWithoutStatus(JsonSerializerSettings jsonSerializerSettings)
        {
            var fieldsCopy = new JObject(JObject.FromObject(Fields, JsonSerializer.Create(jsonSerializerSettings)));
            fieldsCopy.Remove("status");
            var payloadCopy = new JObject
            {
                ["fields"] = fieldsCopy
            };
            return payloadCopy.ToString();
        }
    }

    public class JiraPayloadFields
    {
        [JsonProperty("assignee")]
        public JiraId? Assignee;

        [JsonExtensionData]
        public IDictionary<string, JToken>? CustomFields;

        [JsonProperty("description")]
        public JiraAdfDescription? Description;
        /*
        TODO: Validation on this property
       {
           "id": "10001",
           "name": "Task"
       },
       {
           "id": "10002",
           "name": "Bug"
       },
       {
           "id": "10003",
           "name": "Story"
       },
       {
           "id": "10004",
           "name": "Epic"
       },
       {
           "id": "10005",
           "name": "Subtask"
       }
       */
        [JsonProperty("issuetype")]
        public JiraId? IssueType;

        [JsonProperty("project")]
        public JiraId? Project;

        /*
         TODO: Validation on this property
        {
            "id": "11",
            "name": "To Do"
        },
        {
            "id": "21",
            "name": "In Progress"
        },
        {
            "id": "31",
            "name": "Done"
        }
         */
        [JsonProperty("status")]
        public JiraId? Status;

        [JsonProperty("summary")]
        public string? Summary;
    }
}
