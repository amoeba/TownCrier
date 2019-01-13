using System;
using System.Collections.Generic;
using System.Text;

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
            if (InDungeon())
            {
                return ToDungeonString();
            } else
            {
                return ToCoordString();
            }
        }

        public string ToDungeonString()
        {
            return "In dungeon (Landcell " + Landcell + ").";
        }

        public string ToCoordString()
        {
            return Math.Abs(Latitude).ToString("0.00") + (Latitude >= 0 ? "N" : "S") + ", "
                 + Math.Abs(Longitude).ToString("0.00") + (Longitude >= 0 ? "E" : "W");
        }

        bool InDungeon()
        {
            return (Landcell & 0x0000FF00) != 0;
        }
    }
}
