using System;

namespace TownCrier
{
    // Adapted from GoArrow by Ben Howell
    class Location
    {
        int Landcell;
        double YOffset;
        double XOffset;
        double Latitude;
        double Longitude;

        public Location(int landcell, double yOffset, double xOffset)
        {
            Landcell = landcell;
            YOffset = yOffset;
            XOffset = xOffset;
            Latitude = GetLatitude();
            Longitude = GetLongitude();
        }

        double GetLatitude()
        {
            uint l = (uint)((Landcell & 0x00FF0000) / 0x2000);
            return (l + YOffset / 24.0 - 1019.5) / 10.0;
        }

        double GetLongitude()
        {
            uint l = (uint)((Landcell & 0xFF000000) / 0x200000);
            return (l + XOffset / 24.0 - 1019.5) / 10.0;
        }

        public override string ToString()
        {
            return ToCoordString() + ToIndoorString();
        }

        public string ToCoordString()
        {
            return Math.Abs(Latitude).ToString("0.00") + (Latitude >= 0 ? "N" : "S") + ", "
                 + Math.Abs(Longitude).ToString("0.00") + (Longitude >= 0 ? "E" : "W");
        }

        public string ToIndoorString()
        {
            if (IsIndoors())
            {
                return " (Indoors, landcell " + Landcell + ")";
            }
            else
            {
                return "";
            }
        }

        bool IsIndoors()
        {
            return (Landcell & 0x0000FF00) != 0;
        }
    }
}
