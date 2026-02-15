using System;
using System.Globalization;

namespace AisStreamer;

public static class NmeaEncoder
{
    public static int NavStatusToCode(string? status)
    {
        var s = status?.ToLowerInvariant() ?? "";
        if (s.Contains("under way using engine")) return 0;
        if (s.Contains("at anchor")) return 1;
        if (s.Contains("not under command")) return 2;
        if (s.Contains("restricted manoeuvra")) return 3;
        if (s.Contains("constrained by")) return 4;
        if (s.Contains("moored")) return 5;
        if (s.Contains("aground")) return 6;
        if (s.Contains("engaged in fishing")) return 7;
        if (s.Contains("under way sailing")) return 8;
        return 15;
    }

    private static byte[] BuildType1Bits(AisRecord r)
    {
        var bits = new byte[168];

        void SetBits(int offset, int length, int value)
        {
            for (var i = 0; i < length; i++)
            {
                var bitIndex = offset + i;
                var bitValue = (value >> (length - 1 - i)) & 1;
                bits[bitIndex] = (byte)bitValue;
            }
        }

        void SetSignedBits(int offset, int length, int value)
        {
            var mask = (1 << length) - 1;
            SetBits(offset, length, value & mask);
        }

        SetBits(0, 6, 1);
        SetBits(6, 2, 0);
        SetBits(8, 30, r.Mmsi);
        SetBits(38, 4, NavStatusToCode(r.NavigationalStatus));

        int rotRaw;
        if (double.IsNaN(r.Rot) || r.Rot == 0.0)
            rotRaw = 0;
        else
        {
            var sign = r.Rot < 0.0 ? -1.0 : 1.0;
            rotRaw = Math.Clamp((int)(sign * Math.Sqrt(Math.Abs(r.Rot) / 4.733)), -126, 126);
        }
        SetSignedBits(42, 8, rotRaw);

        var sogRaw = double.IsNaN(r.Sog) ? 1023 : Math.Min((int)(r.Sog * 10.0), 1022);
        SetBits(50, 10, sogRaw);
        SetBits(60, 1, 0);

        var lonRaw = double.IsNaN(r.Longitude) ? 0x6791AC0 : (int)(r.Longitude * 600000.0);
        SetSignedBits(61, 28, lonRaw);

        var latRaw = double.IsNaN(r.Latitude) ? 0x3412140 : (int)(r.Latitude * 600000.0);
        SetSignedBits(89, 27, latRaw);

        var cogRaw = double.IsNaN(r.Cog) ? 3600 : Math.Min((int)(r.Cog * 10.0), 3599);
        SetBits(116, 12, cogRaw);

        var hdgRaw = (r.Heading < 0 || r.Heading > 359) ? 511 : r.Heading;
        SetBits(128, 9, hdgRaw);
        SetBits(137, 6, r.Timestamp.Second);
        SetBits(143, 2, 0);
        SetBits(145, 3, 0);
        SetBits(148, 1, 0);
        SetBits(149, 19, 0);

        return bits;
    }

    private static (string Payload, int FillBits) EncodeBitsToArmor(byte[] bits)
    {
        var charCount = (bits.Length + 5) / 6;
        var fillBits = charCount * 6 - bits.Length;
        var chars = new char[charCount];
        for (var i = 0; i < charCount; i++)
        {
            var value = 0;
            for (var j = 0; j < 6; j++)
            {
                var bitIndex = i * 6 + j;
                var bit = bitIndex < bits.Length ? bits[bitIndex] : 0;
                value = (value << 1) | bit;
            }
            chars[i] = (char)(value < 40 ? value + 48 : value + 56);
        }
        return (new string(chars), fillBits);
    }

    private static string NmeaChecksum(string sentence)
    {
        var cs = 0;
        foreach (var c in sentence)
            cs ^= c;
        return cs.ToString("X2");
    }

    public static string ToNmea0183(AisRecord r)
    {
        var (payload, fillBits) = EncodeBitsToArmor(BuildType1Bits(r));
        var body = $"AIVDM,1,1,,A,{payload},{fillBits}";
        return $"!{body}*{NmeaChecksum(body)}";
    }

    public static string ToNmeaCoord(double degrees)
    {
        var d = Math.Abs(degrees);
        var deg = (int)Math.Truncate(d);
        var minutes = (d - deg) * 60.0;
        return $"{deg:D2}{minutes.ToString("00.0000", CultureInfo.InvariantCulture)}";
    }

    public static string ToGprmc(AisRecord r)
    {
        var time = r.Timestamp.ToString("HHmmss.ff");
        var date = r.Timestamp.ToString("ddMMyy");
        var lat = ToNmeaCoord(r.Latitude);
        var ns = r.Latitude >= 0.0 ? "N" : "S";
        var lon = ToNmeaCoord(r.Longitude);
        var ew = r.Longitude >= 0.0 ? "E" : "W";
        var sog = double.IsNaN(r.Sog) ? "" : r.Sog.ToString("F1", CultureInfo.InvariantCulture);
        var cog = double.IsNaN(r.Cog) ? "" : r.Cog.ToString("F1", CultureInfo.InvariantCulture);
        var body = $"GPRMC,{time},A,{lat},{ns},{lon},{ew},{sog},{cog},{date},,,";
        return $"${body}*{NmeaChecksum(body)}";
    }
}
