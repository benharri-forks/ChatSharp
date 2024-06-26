﻿using System;
using System.Globalization;

namespace ChatSharp
{
    /// <summary>
    ///     Represents a message timestamp received from a server.
    /// </summary>
    public class Timestamp
    {
        /// <summary>
        ///     Initializes and parses the timestamp received from the server.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="compatibility">
        ///     Enable pre-ZNC 1.0 compatibility. In previous versions of the tag,
        ///     servers sent a unix timestamp instead of a ISO 8601 string.
        /// </param>
        internal Timestamp(string date, bool compatibility = false)
        {
            if (!compatibility)
            {
                if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                        out var parsedDate))
                    throw new ArgumentException("The date string was provided in an invalid format.", date);

                Date = parsedDate;
                UnixTimestamp = Date.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            }
            else
            {
                if (!double.TryParse(date, out var parsedTimestamp))
                    throw new ArgumentException("The timestamp string was provided in an invalid format.", date);

                UnixTimestamp = parsedTimestamp;
                Date = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(UnixTimestamp);
            }
        }

        /// <summary>
        ///     A date representation of the timestamp.
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        ///     A unix epoch representation of the timestamp.
        /// </summary>
        public double UnixTimestamp { get; }

        /// <summary>
        ///     True if this timestamp is equal to another (compares unix timestamps).
        /// </summary>
        public bool Equals(Timestamp other)
        {
            return (int)other.UnixTimestamp == (int)UnixTimestamp;
        }

        /// <summary>
        ///     True if this timestamp is equal to another (compares unix timestamps).
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Timestamp timestamp)
                return Equals(timestamp);
            return false;
        }

        /// <summary>
        ///     Returns a ISO 8601 string representation of the timestamp.
        /// </summary>
        public string ToISOString()
        {
            return Date.ToString(@"yyyy-MM-dd\THH:mm:ss.fff\Z");
        }

        /// <summary>
        ///     Returns the hash code of the unix timestamp.
        /// </summary>
        public override int GetHashCode()
        {
            return UnixTimestamp.GetHashCode();
        }
    }
}