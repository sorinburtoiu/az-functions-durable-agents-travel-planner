using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TravelPlannerFunctions.Models;

    /// <summary>
    /// The custom resolver was added because there was no way to set NullValueHandling.Ignore.
    /// </summary>
    public static class OpenApiCustomExampleResolver
    {
        private static readonly JsonSerializerSettings settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = FormatHelpers.DateFormat,
        };

        public static KeyValuePair<string, OpenApiExample> Resolve<T>(string name, T instance, NamingStrategy namingStrategy = null)
        {
            name.ThrowIfNullOrWhiteSpace();
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            DefaultContractResolver contractResolver = new()
            {
                NamingStrategy = namingStrategy ?? new DefaultNamingStrategy()
            };
            settings.ContractResolver = contractResolver;
            IOpenApiAny value = OpenApiExampleFactory.CreateInstance(instance, settings);
            OpenApiExample value2 = new OpenApiExample
            {
                Value = value
            };
            return new KeyValuePair<string, OpenApiExample>(name, value2);
        }
    }

        /// <summary>
    /// Helper used to avoid hardcoding for type formats.
    /// </summary>
    public static class FormatHelpers
    {
        /// <summary>
        /// The date format used when no time is needed.
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// The date time format.
        /// </summary>
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// The format used to add a timestamp in file names.
        /// It has '-' instead of space from the default format (yyyy‑MM‑dd HH:mm:ss) to avoid spaces in file names.
        /// </summary>
        public const string TimestampForFilesFormat = "-yyyy‑MM‑dd-HH:mm:ss";

        /// <summary>
        /// The format is used to represent a date with time.
        /// </summary>
        public const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.000Z";

        public static string FormatDate(DateTime? date) => date.HasValue ? date.Value.ToString(DateFormat) : string.Empty;

        /// <summary>
        /// The format used to add a timestamp in SharePoint names.
        /// It has '-' instead of space from the default format (yyyy‑MM‑dd HH:mm:ss) to avoid spaces in file names.
        /// The colon sign ":" is removed.
        /// </summary>
        public const string TimestampForSharepointFormat = "-yyyy‑MM‑dd-HHmmss";
    }

