using CsvHelper.Configuration.Attributes;

namespace Application.DTOs.NasaDto
{
    public class FireDto
    {
        [Name("country_id")]
        public string CountryId { get; set; }

        [Name("latitude")]
        public double Latitude { get; set; }

        [Name("longitude")]
        public double Longitude { get; set; }

        [Name("bright_ti4")]
        public double BrightTi4 { get; set; }

        [Name("scan")]
        public double Scan { get; set; }

        [Name("track")]
        public double Track { get; set; }

        [Name("acq_date")]
        public DateTime AcqDate { get; set; }

        [Name("acq_time")]
        public int AcqTime { get; set; }

        [Name("satellite")]
        public string Satellite { get; set; }

        [Name("instrument")]
        public string Instrument { get; set; }

        [Name("confidence")]
        public string Confidence { get; set; }

        [Name("version")]
        public string Version { get; set; }

        [Name("bright_ti5")]
        public double BrightTi5 { get; set; }

        [Name("frp")]
        public double Frp { get; set; }

        [Name("daynight")]
        public string Daynight { get; set; }
    }
}
