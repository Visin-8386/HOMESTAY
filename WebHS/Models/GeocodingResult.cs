namespace WebHS.Models
{
    /// <summary>
    /// Represents the result of a geocoding operation
    /// </summary>
    public class GeocodingResult
    {
        /// <summary>
        /// Indicates whether the geocoding operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The latitude coordinate
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// The formatted address returned by the geocoding service
        /// </summary>
        public string FormattedAddress { get; set; } = string.Empty;

        /// <summary>
        /// The source of the geocoding result (e.g., "Vietnam Local Database", "Enhanced Nominatim", "Photon", etc.)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata or raw response data from the geocoding service
        /// </summary>
        public object? Metadata { get; set; }

        /// <summary>
        /// Error message if the geocoding operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Confidence score of the geocoding result (0.0 to 1.0)
        /// </summary>
        public double? Confidence { get; set; }

        /// <summary>
        /// Address components for Vietnamese address structure
        /// </summary>
        public AddressComponents? Components { get; set; }
    }

    /// <summary>
    /// Structured address components for Vietnamese addresses
    /// </summary>
    public class AddressComponents
    {
        /// <summary>
        /// House number (Số nhà)
        /// </summary>
        public string? HouseNumber { get; set; }

        /// <summary>
        /// Street name (Tên đường)
        /// </summary>
        public string? StreetName { get; set; }

        /// <summary>
        /// Ward name (Phường/Xã)
        /// </summary>
        public string? Ward { get; set; }

        /// <summary>
        /// District name (Quận/Huyện)
        /// </summary>
        public string? District { get; set; }

        /// <summary>
        /// Province/City name (Tỉnh/Thành phố)
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// Country name (Quốc gia)
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Postal code (Mã bưu điện)
        /// </summary>
        public string? PostalCode { get; set; }
    }
}
