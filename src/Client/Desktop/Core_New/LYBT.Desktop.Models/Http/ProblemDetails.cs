using System.Collections.Generic;

namespace LYBT.Desktop.Models.Http
{
    /// <summary>
    /// RFC 7807 Problem Details for HTTP APIs
    /// </summary>
    public class ProblemDetails
    {
        /// <summary>
        /// A URI reference that identifies the problem type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// A short, human-readable summary of the problem type
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The HTTP status code
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// A human-readable explanation specific to this occurrence of the problem
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// A URI reference that identifies the specific occurrence of the problem
        /// </summary>
        public string? Instance { get; set; }

        /// <summary>
        /// Additional problem-specific extension members
        /// </summary>
        public Dictionary<string, object?> Extensions { get; set; } = new Dictionary<string, object?>();
    }
}