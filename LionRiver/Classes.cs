using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;
using MapControl;
using CircularBuffer;

using OSGeo.GDAL;
using OSGeo.OSR;

namespace LionRiver
{

    //public class Map
    //{
    //    // Holds Map information
    //    private Dataset ds;
    //    private SpatialReference sr;    // Map projected coordinate system
    //    private SpatialReference wgs84; // Lat/Lon geographical coordinate system
    //    private CoordinateTransformation transform_to_latlon;
    //    private CoordinateTransformation transform_from_latlon;
    //    private double[] gtCoef = new double[6];
    //    private double[] invCoef = new double[6];

    //    public BitmapSource Image;

    //    public double MinLat;
    //    public double MinLon;
    //    public double MaxLat;
    //    public double MaxLon;

    //    public Map(string filename)
    //    {


    //        // Read dataset & raster band
    //        ds = Gdal.Open(filename, Access.GA_ReadOnly);

    //        Band band = ds.GetRasterBand(1);

    //        // Read projected coordinate system and create 
    //        sr = new SpatialReference(ds.GetProjectionRef());
    //        string wkt;
    //        Osr.GetWellKnownGeogCSAsWKT("WGS84", out wkt);
    //        wgs84 = new SpatialReference(wkt);

    //        transform_to_latlon = new CoordinateTransformation(sr, wgs84);
    //        transform_from_latlon = new CoordinateTransformation(wgs84, sr);

    //        // Read Geo Transform coefficients from dataset and calculate inverse
    //        ds.GetGeoTransform(gtCoef);
    //        Gdal.InvGeoTransform(gtCoef, invCoef);

    //        int width = band.XSize;
    //        int height = band.YSize;
    //        System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Indexed8;
    //        int stride = width * pf.BitsPerPixel / 8;

    //        byte[] pixels = new byte[width * height];

    //        if (band.GetRasterColorInterpretation() == ColorInterp.GCI_PaletteIndex)
    //        {
    //            band.ReadRaster(0, 0, width, height, pixels, width, height, 0, 0);
    //            ColorTable ct = band.GetRasterColorTable();

    //            List<Color> colors = new List<Color>();

    //            for (int i = 0; i < ct.GetCount(); i++)
    //            {
    //                ColorEntry entry = ct.GetColorEntry(i);
    //                colors.Add(Color.FromRgb(Convert.ToByte(entry.c1), Convert.ToByte(entry.c2), Convert.ToByte(entry.c3)));
    //            }

    //            BitmapPalette palette = new BitmapPalette(colors);

    //            Image = BitmapSource.Create(width, height, 96, 96, pf, palette, pixels, stride);
    //        }
    //        this.ConvertToLL(0, Image.PixelHeight, out MinLon, out MinLat);
    //        this.ConvertToLL(Image.PixelWidth, 0, out MaxLon, out MaxLat);
    //    }

    //    public void ConvertToXY(double lon, double lat, out double x, out double y)
    //    {
    //        double[] v = new double[3];
    //        transform_from_latlon.TransformPoint(v, lon, lat, 0);
    //        Gdal.ApplyGeoTransform(invCoef, v[0], v[1], out x, out y);
    //    }

    //    public void ConvertToLL(double x, double y, out double lon, out double lat)
    //    {
    //        double[] v = new double[3];
    //        double x1, y1;
    //        Gdal.ApplyGeoTransform(gtCoef, x, y, out  x1, out y1);
    //        transform_to_latlon.TransformPoint(v, x1, y1, 0);
    //        lon = v[0];
    //        lat = v[1];
    //    }
    //}

    #region Grib

    public class uvpair
    {
        public double u { get; set; }
        public double v { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class gribband
    {
        public DateTime datetime { get; set; }
        public uvpair[,] data;

        public gribband(int sizex, int sizey)
        {
            data = new uvpair[sizex, sizey];
        }
    }

    public class grib
    {
        private CoordinateTransformation transform_to_latlon;
        private CoordinateTransformation transform_from_latlon;
        private double[] gtCoef = new double[6];
        private double[] invCoef = new double[6];

        public int SizeX;
        public int SizeY;

        public double MinLat;
        public double MinLon;
        public double MaxLat;
        public double MaxLon;

        public double DeltaLat;
        public double DeltaLon;

        public Collection<gribband> band;

        public grib(ref Dataset ds)
        {
            SpatialReference sr;    // Map projected coordinate system
            SpatialReference wgs84; // Lat/Lon geographical coordinate system

            sr = new SpatialReference(ds.GetProjectionRef());
            string wkt;
            Osr.GetWellKnownGeogCSAsWKT("WGS84", out wkt);
            wgs84 = new SpatialReference(wkt);

            transform_to_latlon = new CoordinateTransformation(sr, wgs84);
            transform_from_latlon = new CoordinateTransformation(wgs84, sr);

            // Read Geo Transform coefficients from dataset and calculate inverse
            ds.GetGeoTransform(gtCoef);
            Gdal.InvGeoTransform(gtCoef, invCoef);

            SizeX = ds.RasterXSize;
            SizeY = ds.RasterYSize;

            this.ConvertToLL(0, SizeY - 1, out MinLon, out MinLat);
            this.ConvertToLL(SizeX - 1, 0, out MaxLon, out MaxLat);

            DeltaLon = (MaxLon - MinLon) / (SizeX - 1);
            DeltaLat = (MaxLat - MinLat) / (SizeY - 1);

            MinLat -= DeltaLat / 2;
            MaxLat -= DeltaLat / 2;
            MinLon += DeltaLon / 2;
            MaxLon += DeltaLon / 2;

            MinLat = MinLat > 180 ? MinLat - 360 : MinLat;
            MinLon = MinLon > 180 ? MinLon - 360 : MinLon;
            MaxLat = MaxLat > 180 ? MaxLat - 360 : MaxLat;
            MaxLon = MaxLon > 180 ? MaxLon - 360 : MaxLon;

            band = new Collection<gribband>();
        }

        public void ConvertToXY(double lon, double lat, out double x, out double y)
        {
            double[] v = new double[3];
            transform_from_latlon.TransformPoint(v, lon, lat, 0);
            Gdal.ApplyGeoTransform(invCoef, v[0], v[1], out x, out y);
        }

        public void ConvertToLL(double x, double y, out double lon, out double lat)
        {
            double[] v = new double[3];
            double x1, y1;
            Gdal.ApplyGeoTransform(gtCoef, x, y, out  x1, out y1);
            transform_to_latlon.TransformPoint(v, x1, y1, 0);
            lon = v[0];
            lat = v[1];
            lat -= DeltaLat / 2;
            lon += DeltaLon / 2;
        }

        public bool GetBandIndex(DateTime dt,ref int idx,ref double distance)
        {

            // Finds first index to Band with DateTime<dt
            // Calculates percentual time distance from Band to dt

            idx = 0;
            while (band[idx].datetime <= dt)
            {
                idx++;
                if (idx > band.Count - 1)
                    return false;
            }

            if (idx == 0)
                return false; // Required time not covered in windbands
            
            double tspan = (band[idx].datetime - band[idx - 1].datetime).TotalSeconds;
            distance = (dt - band[idx - 1].datetime).TotalSeconds / tspan;

            return true;
        }

        public uvpair GetUV(double lat, double lon, int idx,double distance)
        {
            uvpair uv = new uvpair();

            double delta_omega = lon - MinLon;
            double delta_phi = lat - MinLat;

            int i = Convert.ToInt16(Math.Floor(delta_omega / DeltaLon));
            int j = Convert.ToInt16(Math.Floor(delta_phi / DeltaLat));

            if ((i >= SizeX) || (j >= SizeY) || i < 0 || j < 0)
            {
                return null; // Out of the grid
            }

            // Invert Y axis for NE orientation instead for SE
            j = SizeY - j - 1;

            uvpair uv0 = new uvpair();
            uvpair uv1 = new uvpair();

            uv0 = band[idx - 1].data[i, j];
            uv1 = band[idx].data[i, j];

            if (uv0 != null && uv1 != null)
            {
                uv.Lat = lat;
                uv.Lon = lon;

                uv.u = uv0.u + (uv1.u - uv0.u) * distance;
                uv.v = uv0.v + (uv1.v - uv0.v) * distance;

                return uv;
            }
            else
            {
                if (uv0 != null)
                    return uv0;
                else
                    if (uv1 != null)
                        return uv1;
                    else
                        return null;
            }
        }

        public uvpair GetUVInterpolated(double lat, double lon, DateTime dt)
        {
            uvpair uv = new uvpair();

            double delta_omega = lon - MinLon;
            double delta_phi = lat - MinLat;

            int i = Convert.ToInt16(Math.Floor(delta_omega / DeltaLon));
            int j = Convert.ToInt16(Math.Floor(delta_phi / DeltaLat));

            if ((i > (SizeX-2) ) || (j > (SizeY-2 )) || i < 0 || j < 0)
            {
                return null; // Out of the grid
            }

            int idx = 0;
            while (band[idx].datetime <= dt)
            {
                idx++;
                if (idx > band.Count - 1)
                    return null;
            }

            if (idx == 0)
                return null; // Required time not covered in bands

            // Invert Y axis for NE orientation instead for SE
            j = SizeY - j - 1;

            double tspan = (band[idx].datetime - band[idx - 1].datetime).TotalSeconds;
            double t = (dt - band[idx - 1].datetime).TotalSeconds;

            uvpair uv0 = new uvpair();
            uvpair uv1 = new uvpair();

            uv0 = GetUVByIndex(lat, lon, i, j, idx - 1);
            uv1 = GetUVByIndex(lat, lon, i, j, idx);

            if (uv0 != null && uv1 != null)
            {
                uv.u = uv0.u + (uv1.u - uv0.u) * t / tspan;
                uv.v = uv0.v + (uv1.v - uv0.v) * t / tspan;

                uv.Lat = lat;
                uv.Lon = lon;

                return uv;
            }
            else
            {
                if (uv0 != null)
                    return uv0;
                else
                    if (uv1 != null)
                        return uv1;
                    else
                        return null;
            }
        }

        private uvpair GetUVByIndex(double lat, double lon, int i, int j, int windband_idx)
        {
            uvpair uv = new uvpair();

            var v = this.band[windband_idx].data;

            var uvlist = from uvp in new Collection<uvpair> { v[i, j], v[i + 1, j], v[i + 1, j - 1], v[i, j - 1] }
                         where uvp != null
                         select uvp;

            if (uvlist.Count() == 4) // We got all four vertices
            {

                double x0 = v[i, j].Lon;
                double x1 = v[i + 1, j].Lon;
                double y0 = v[i, j].Lat;
                double y1 = v[i, j - 1].Lat;

                double u00 = v[i, j].u;
                double u10 = v[i + 1, j].u;
                double u01 = v[i, j - 1].u;
                double u11 = v[i + 1, j - 1].u;

                double ua = u00 + (u10 - u00) * (lon - x0) / (x1 - x0);
                double ub = u01 + (u11 - u01) * (lon - x0) / (x1 - x0);

                uv.u = ua + (ub - ua) * (lat - y0) / (y1 - y0);

                double v00 = v[i, j].v;
                double v10 = v[i + 1, j].v;
                double v01 = v[i, j - 1].v;
                double v11 = v[i + 1, j - 1].v;

                double va = v00 + (v10 - v00) * (lon - x0) / (x1 - x0);
                double vb = v01 + (v11 - v01) * (lon - x0) / (x1 - x0);

                uv.v = va + (vb - va) * (lat - y0) / (y1 - y0);

                return uv;
            }
            else
            {
                return (from uvp in uvlist
                        orderby (uvp.u * uvp.u + uvp.v * uvp.v) descending
                        select uvp).FirstOrDefault();
            }
        }

    }

    public class windgrib : grib
    {
        public windgrib(ref Dataset ds)
            : base(ref ds)
        { }

        public uvpair GetWind(double lat, double lon, int idx, double distance)
        {
            return base.GetUV(lat, lon, idx, distance);
        }

        public uvpair GetWindInterpolated(double lat, double lon, DateTime dt)
        {
            return base.GetUVInterpolated(lat, lon, dt);
        }

    }

    public class currentgrib : grib
    {
        public currentgrib(ref Dataset ds)
            : base(ref ds)
        { }

        public uvpair GetCurrent(double lat, double lon, int idx, double distance)
        {
            return base.GetUV(lat, lon, idx, distance);
        }

        public uvpair GetCurrentInterpolated(double lat, double lon, DateTime dt)
        {
            return base.GetUVInterpolated(lat, lon, dt);
        }
    }

    #endregion

    #region Instruments
    public class IData<T>
    {
        public DateTime Time { get; set; }
        public T Val { get; set; }
    }

    public class Inst
    {
        #region Constants
        public const int MaxBuffers = 6;
        public const int BufSize = 3600;
        public const int ZoomFactor = 5; // Each buffer will average over ZoomFactor ticks on the previous one 
        public const int BufOneSec = 0;
        public const int BufFiveSec = 1;
        public const int BufHalfMin = 2;
        public const int BufTwoMin = 3;
        public const int BufTenMin = 4;
        public const int BufOneHr = 5;
        #endregion
    }

    public abstract class Instrument<T> : INotifyPropertyChanged where T : new()
    {

        public event PropertyChangedEventHandler PropertyChanged;

        #region Protected
        protected CircularBuffer<IData<T>>[] buffer = new CircularBuffer<IData<T>>[Inst.MaxBuffers];
        protected List<IData<T>>[] avgBuffer = new List<IData<T>>[Inst.MaxBuffers];
        protected CircularBuffer<T> dampBuffer;
        protected bool _valid;
        #endregion

        #region Constructors
        public Instrument(string dn = "dummy", string un = "dummy", int DampingWindow = 1)
        {
            this.Val = new T();
            this.DisplayName = dn;
            this.Units = un;
            this.dampBuffer = new CircularBuffer<T>(DampingWindow, true);
            for (int i = 0; i < Inst.MaxBuffers; i++)
            {
                buffer[i] = new CircularBuffer<IData<T>>(Inst.BufSize, true);
                avgBuffer[i] = new List<IData<T>>();
            }
        }

        //public Instrument(string dn, string un)
        //    : this(dn, un, 1)
        //{ }

        //public Instrument()
        //    : this("Dummy", "Dummy", 1)
        //{ } 
        #endregion

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public virtual T Val { get; set; }
        public string DisplayName { get; set; }
        public string Units { get; set; }

        public virtual void SetValid(DateTime dt)
        {
            dampBuffer.Put(Val);
            Val = CalculateAverage(dampBuffer);
            PushToBuffer(Val, dt, 0);
            _valid = true;
            OnPropertyChanged("FormattedValue");
        }

        public virtual void SetValid()
        {
            SetValid(System.DateTime.Now);
        }

        protected void PushToBuffer(T v, DateTime dt, int bufnr)
        {
            buffer[bufnr].Put(new IData<T> { Val = v, Time = dt });
            avgBuffer[bufnr].Add(new IData<T> { Val = v, Time = dt });
            if (avgBuffer[bufnr].Count == Inst.ZoomFactor && bufnr < Inst.MaxBuffers - 1)
            {
                var avgValList = (from insdat in avgBuffer[bufnr] select insdat.Val).ToList();
                T avg = CalculateAverage(avgValList);

                var avgTimeList = (from insdat in avgBuffer[bufnr] select insdat.Time).ToList();
                DateTime avgdt = CalculateTimeAverage(avgTimeList);

                PushToBuffer(avg, avgdt, bufnr + 1);
                avgBuffer[bufnr].Clear();
            }
        }

        public void Invalidate()
        {
            _valid = false;
            OnPropertyChanged("FormattedValue");
        }

        public bool IsValid()
        {
            return _valid;
        }

        public T Average(int level)
        {

            if (buffer[level].Size > 0)
                return buffer[level].Read(0).Val;
            else
                if (level > 0)
                    return Average(level - 1);
                else
                    return default(T);

        }

        public CircularBuffer<IData<T>> GetBuffer(int level)
        {
            return buffer[level];
        }

        public CircularBuffer<IData<T>>[] GetBuffer()
        {
            return buffer;
        }

        public abstract string FormattedValue { get; }

        protected abstract T CalculateAverage(ICollection<T> items);

        protected DateTime CalculateTimeAverage(ICollection<DateTime> items)
        {
            var averageTicks = (long)items.Select(d => d.Ticks).Average();
            return new DateTime(averageTicks);
        }
    }

    public class LinearInstrument : Instrument<double>
    {
        public LinearInstrument(string dn, string un, int dw)
            : base(dn, un, dw)
        {
        }

        public LinearInstrument(string dn, string un)
            : base(dn, un, 1)
        {
        }

        protected override double CalculateAverage(ICollection<double> items)
        {
            return items.Average();
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                    return Val.ToString("0.00");
                else
                    return "";
            }
        }

    }

    class AngularInstrument : Instrument<double>
    {
        public AngularInstrument(string dn, string un, int dw)
            : base(dn, un, dw)
        {
        }

        public AngularInstrument(string dn, string un)
            : base(dn, un, 1)
        {
        }

        protected override double CalculateAverage(ICollection<double> items)
        {
            double sumcos = 0, sumsin = 0;
            foreach (double v in items)
            {
                sumcos += Math.Cos(v * Math.PI / 180);
                sumsin += Math.Sin(v * Math.PI / 180);
            }
            return Math.Atan2(sumsin, sumcos) * 180 / Math.PI;
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                {
                    double _val = (Val + 360) % 360;
                    return _val.ToString("000");
                }
                else
                    return "";
            }
        }
    }

    class AngularInstrumentAbs : AngularInstrument
    {
        public AngularInstrumentAbs(string dn, string un, int dw)
            : base(dn, un, dw)
        {
        }

        public AngularInstrumentAbs(string dn, string un)
            : base(dn, un, 1)
        {
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                {
                    double _val = (Val + 360) % 360;
                    return _val.ToString("000");
                }
                else
                    return "";
            }
        }
    }

    class AngularInstrumentRel : AngularInstrument
    {
        public AngularInstrumentRel(string dn, string un, int dw)
            : base(dn, un, dw)
        {
        }

        public AngularInstrumentRel(string dn, string un)
            : base(dn, un, 1)
        {
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                {
                    double _val = (Val + 360) % 360;
                    if (_val > 180) _val = _val - 360;
                    return _val.ToString("###");
                }
                else
                    return "";
            }
        }

    }

    class LatitudeInstrument : AngularInstrument
    {
        public LatitudeInstrument(string dn, string un, int dw)
            : base(dn, un, dw)
        {
        }

        public LatitudeInstrument(string dn, string un)
            : base(dn, un, 1)
        {
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                {
                    double deg, min;
                    string d, m, c;

                    deg = Math.Abs(Math.Truncate(Val));
                    min = (Math.Abs(Val) - deg) * 60;

                    d = deg.ToString();
                    m = min.ToString("0.00");
                    if (Val > 0)
                        c = "N";
                    else
                        c = "S";

                    return d + "° " + m + "' " + c;
                }
                else
                    return "";
            }
        }
    }

    class LongitudeInstrument : AngularInstrument
    {
        public LongitudeInstrument(string dn, string un, int dw)
            : base(dn, un, dw)
        {
        }

        public LongitudeInstrument(string dn, string un)
            : base(dn, un, 1)
        {
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                {
                    double deg, min;
                    string d, m, c;

                    deg = Math.Abs(Math.Truncate(Val));
                    min = (Math.Abs(Val) - deg) * 60;

                    d = deg.ToString();
                    m = min.ToString("0.00");
                    if (Val > 0)
                        c = "E";
                    else
                        c = "W";

                    return d + "° " + m + "' " + c;
                }
                else
                    return "";
            }
        }
    }

    class PercentInstrument : LinearInstrument
    {
        public PercentInstrument(string dn, string un, int dw)
            : base(dn, un, dw)
        {
        }

        public PercentInstrument(string dn, string un)
            : base(dn, un, 1)
        {
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                    return Val.ToString("#%");
                else
                    return "";
            }
        }
    }

    class String2
    {
        public String2(string s)
        {
            str = s;
        }

        public String2() : this(String.Empty) { }

        public string str { get; set; }

    }

    class WaypointInstrument : Instrument<String2>
    {
        public WaypointInstrument(string dn)
            : base(dn, "", 1)
        {
        }

        protected override String2 CalculateAverage(ICollection<String2> items)
        {
            return Val;
        }

        public override string FormattedValue
        {
            get
            {
                if (_valid)
                    return Val.str;
                else
                    return "";
            }

        }

    }

    class PositionInstrument : Instrument<Location>
    {
        public PositionInstrument(string dn)
            : base(dn, "", 1)
        {
        }

        protected override Location CalculateAverage(ICollection<Location> items)
        {
            double latsumcos = 0, latsumsin = 0;
            double lonsumcos = 0, lonsumsin = 0;

            foreach (Location v in items)
            {
                latsumcos += Math.Cos(v.Latitude * Math.PI / 180);
                latsumsin += Math.Sin(v.Latitude * Math.PI / 180);
                lonsumcos += Math.Cos(v.Longitude * Math.PI / 180);
                lonsumsin += Math.Sin(v.Longitude * Math.PI / 180);
            }

            double lat = Math.Atan2(latsumsin, latsumcos) * 180 / Math.PI;
            double lon = Math.Atan2(lonsumsin, lonsumcos) * 180 / Math.PI;

            return new Location { Latitude = lat, Longitude = lon };

        }

        public override string FormattedValue
        {
            get
            {
                PositionConverter pc = new PositionConverter();
                return (string)pc.Convert(Val, typeof(string), null, null);
            }
        }

    }
    
    #endregion

    #region Polar
    public class PolarPoint
    {
        public double TWA { get; set; }
        public double SPD { get; set; }
    }

    public class PolarLine
    {
        public double TWS { get; set; }
        public double BeatTWA { get; set; }
        public double RunTWA { get; set; }
        public double BeatSPD { get; set; }
        public double RunSPD { get; set; }

        public List<PolarPoint> Points { get; set; }

        public PolarLine()
        {
            this.TWS = 0;
            this.BeatTWA = 0;
            this.RunTWA = 180;
            this.BeatSPD = 0;
            this.RunSPD = 0;
            this.Points = new List<PolarPoint>();
        }

        public PolarLine(string s)
            : this()
        {
            string[] str = s.Split(',');
            this.TWS = Convert.ToDouble(str[0]);

            double RunVMG = 0, BeatVMG = 0;
            int i = 1;

            while (i < str.Length)
            {
                PolarPoint p = new PolarPoint();
                p.TWA = Convert.ToDouble(str[i]);
                p.SPD = Convert.ToDouble(str[i + 1]);
                Points.Add(p);

                double VMG = p.SPD * Math.Cos(p.TWA * Math.PI / 180);
                if (VMG > BeatVMG)
                {
                    BeatVMG = VMG;
                    BeatTWA = p.TWA;
                    BeatSPD = p.SPD;
                }

                if (VMG < RunVMG)
                {
                    RunVMG = VMG;
                    RunTWA = p.TWA;
                    RunSPD = p.SPD;
                }
                i += 2;
            }
        }

        public double GetTarget(double twa)
        {
            PolarPoint p1 = new PolarPoint();
            PolarPoint p2 = new PolarPoint();
            PolarPoint p3 = new PolarPoint();

            foreach (PolarPoint p in Points)
            {
                if (p.TWA > twa)
                {
                    p2 = p;
                    break;
                }
                else
                    p1 = p;
            }

            p3.SPD = p1.SPD + (twa - p1.TWA) * (p2.SPD - p1.SPD) / (p2.TWA - p1.TWA);

            return p3.SPD;
        }

        public PolarPoint GetTargetVMC(double twd, double brg, double drift, double set)
        {
            PolarPoint pr = new PolarPoint();

            double maxvmc = 0;

            for (double twa = 0; twa <= 180; twa += 1)
            {

                double spd = this.GetTarget(twa);
                double sogx1 = spd * Math.Cos((twd + twa) * Math.PI / 180) + drift * Math.Cos(set * Math.PI / 180);
                double sogy1 = spd * Math.Sin((twd + twa) * Math.PI / 180) + drift * Math.Sin(set * Math.PI / 180);
                double sogx2 = spd * Math.Cos((twd - twa) * Math.PI / 180) + drift * Math.Cos(set * Math.PI / 180);
                double sogy2 = spd * Math.Sin((twd - twa) * Math.PI / 180) + drift * Math.Sin(set * Math.PI / 180);

                double sog1 = Math.Sqrt(sogx1 * sogx1 + sogy1 * sogy1);
                double sog2 = Math.Sqrt(sogx2 * sogx2 + sogy2 * sogy2);

                double cog1 = Math.Atan2(sogy1, sogx1) * 180 / Math.PI;
                double cog2 = Math.Atan2(sogy2, sogx2) * 180 / Math.PI;

                double vmc1 = sog1 * Math.Cos((brg - cog1) * Math.PI / 180);
                double vmc2 = sog2 * Math.Cos((brg - cog2) * Math.PI / 180);

                if (vmc1 > maxvmc)
                {
                    maxvmc = vmc1;
                    pr.SPD = vmc1;
                    pr.TWA = twa;
                }

                if (vmc2 > maxvmc)
                {
                    maxvmc = vmc2;
                    pr.SPD = vmc2;
                    pr.TWA = -twa;
                }
            }

            return pr;
        }
    }

    public class Polar
    {
        public string Name { get; set; }
        public List<PolarLine> Lines { get; set; }
        public Boolean IsLoaded { get; set; }

        public Polar()
        {
            this.Lines = new List<PolarLine>();
            this.IsLoaded = false;
        }

        public void Load(StreamReader f)
        {
            while (f.Peek() >= 0)
            {
                string s = f.ReadLine();
                this.Lines.Add(new PolarLine(s));
            }
            this.IsLoaded = true;
        }

        public double GetTarget(double twa, double tws)
        {
            int i = 0, j = 0;

            twa = (twa + 360) % 360;
            if (twa > 180) twa = 360 - twa;

            foreach (PolarLine pl in Lines)
            {
                if (pl.TWS > tws)
                {
                    j = Lines.IndexOf(pl);
                    break;
                }
                else
                    i = Lines.IndexOf(pl);
            }

            double bs1 = Lines[i].GetTarget(twa);
            double bs2 = Lines[j].GetTarget(twa);
            double tws1 = Lines[i].TWS;
            double tws2 = Lines[j].TWS;

            double bs = bs1 + (bs2 - bs1) * (tws - tws1) / (tws2 - tws1);

            return bs;
        }

        public PolarPoint GetBeatTarget(double tws)
        {
            int i = 0, j = 0;
            PolarPoint p = new PolarPoint();

            foreach (PolarLine pl in Lines)
            {
                if (pl.TWS > tws)
                {
                    j = Lines.IndexOf(pl);
                    break;
                }
                else
                    i = Lines.IndexOf(pl);
            }

            double tws1 = Lines[i].TWS;
            double tws2 = Lines[j].TWS;
            double bs1 = Lines[i].BeatSPD;
            double bs2 = Lines[j].BeatSPD;
            double twa1 = Lines[i].BeatTWA;
            double twa2 = Lines[j].BeatTWA;

            p.SPD = bs1 + (bs2 - bs1) * (tws - tws1) / (tws2 - tws1);
            p.TWA = twa1 + (twa2 - twa1) * (tws - tws1) / (tws2 - tws1);

            return p;
        }

        public PolarPoint GetRunTarget(double tws)
        {
            int i = 0, j = 0;
            PolarPoint p = new PolarPoint();

            foreach (PolarLine pl in Lines)
            {
                if (pl.TWS > tws)
                {
                    j = Lines.IndexOf(pl);
                    break;
                }
                else
                    i = Lines.IndexOf(pl);
            }

            double tws1 = Lines[i].TWS;
            double tws2 = Lines[j].TWS;
            double bs1 = Lines[i].RunSPD;
            double bs2 = Lines[j].RunSPD;
            double twa1 = Lines[i].RunTWA;
            double twa2 = Lines[j].RunTWA;

            p.SPD = bs1 + (bs2 - bs1) * (tws - tws1) / (tws2 - tws1);
            p.TWA = twa1 + (twa2 - twa1) * (tws - tws1) / (tws2 - tws1);

            return p;
        }

        public PolarPoint GetTargetVMC(double tws, double twd, double brg, double drift, double set)
        {
            PolarPoint p1 = new PolarPoint();
            PolarPoint p2 = new PolarPoint();
            PolarPoint p = new PolarPoint();

            int i = 0, j = 0;

            foreach (PolarLine pl in Lines)
            {
                if (pl.TWS > tws)
                {
                    j = Lines.IndexOf(pl);
                    break;
                }
                else
                    i = Lines.IndexOf(pl);
            }

            p1 = Lines[i].GetTargetVMC(twd, brg, drift, set);
            p2 = Lines[j].GetTargetVMC(twd, brg, drift, set);

            double tws1 = Lines[i].TWS;
            double tws2 = Lines[j].TWS;
            double vmc1 = p1.SPD;
            double vmc2 = p2.SPD;
            double twa1 = p1.TWA;
            double twa2 = p2.TWA;

            p.SPD = vmc1 + (vmc2 - vmc1) * (tws - tws1) / (tws2 - tws1);
            p.TWA = twa1 + (twa2 - twa1) * (tws - tws1) / (tws2 - tws1);

            return p;
        }

        public PolarPoint GetTargetBearing(double tws, double twd, double tgtcog, double drift, double set,double spdadj)
        {
            PolarPoint pr = new PolarPoint();

            double minDelta = 360; // Let's try to minimize minDelta

            for (double twa = 0; twa < 360; twa+=2)
            {
                double spd = this.GetTarget(twa, tws) * spdadj;
                double hdg = twd - twa;
                double cogx = spd * Math.Cos(hdg * Math.PI / 180) + drift * Math.Cos(set * Math.PI / 180);
                double cogy = spd * Math.Sin(hdg * Math.PI / 180) + drift * Math.Sin(set * Math.PI / 180);
                double cog = Math.Atan2(cogy, cogx) * 180 / Math.PI;
                double sog = Math.Sqrt(cogx * cogx + cogy * cogy);

                if (Math.Abs(cog - tgtcog) < minDelta)
                {
                    minDelta = Math.Abs(cog - tgtcog);
                    pr.TWA = twa;
                    pr.SPD = sog;
                }
            }

            if (minDelta <= 2 && pr.SPD > 0)  // Found a Bearing which leads to a tgtcog within 2 degrees
                return pr;          // returns the TWA and SOG resulting from SPD & DRIFT
            else
                return null;        // No match found
        }
    } 

    #endregion

    public class NMEASentence
    {

        private string name;
        public string Name
        {
            set { name = value; }
            get { return name; }
        }

        private int inport;
        public int InPort
        {
            set { inport = value; }            
            get { return inport; }
        }

        private bool outport1;
        public bool OutPort1
        {
            set { outport1 = value; }
            get { return outport1; }
        }

        private bool outport2;
        public bool OutPort2
        {
            set { outport2 = value; }
            get { return outport2; }
        }

        private bool outport3;
        public bool OutPort3
        {
            set { outport3 = value; }
            get { return outport3; }
        }

        private bool outport4;
        public bool OutPort4
        {
            set { outport4 = value; }
            get { return outport4; }
        }

        private bool generate;
        public bool Generate
        {
            set { generate = value; }
            get { return generate; }
        }

        private string comments;
        public string Comments
        {
            set { comments = value; }
            get { return comments; }
        }
    }

    public class CustomSettingConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            else
                return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
           CultureInfo culture, object value)
        {
            if (value is string)
            {
                string[] v = ((string)value).Split(new char[] { ',' });
                if (v.Length == 6)
                    return new NMEASentence() { Name = v[0], InPort = int.Parse(v[1]), OutPort1 = bool.Parse(v[2]), OutPort2 = bool.Parse(v[3]), OutPort3 = bool.Parse(v[4]), OutPort4 = bool.Parse(v[5]) };
                else
                    return new NMEASentence();
            }
            return base.ConvertFrom(context, culture, value);
        }
        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context,
           CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                NMEASentence ns = value as NMEASentence;
                return ns.Name + "," + ns.InPort.ToString() + "," + ns.OutPort1.ToString() + "," + ns.OutPort2.ToString() + "," + ns.OutPort3.ToString() + "," + ns.OutPort4.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class ObservableString : INotifyPropertyChanged
    {
        private string _value;        

        public string Value
        {
            get
            { return _value; }

            set
            {
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

    }

    public class ObservableInt : INotifyPropertyChanged
    {
        private int _value;

        public int Value
        {
            get
            { return _value; }

            set
            {
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

    }

    class GdalEnvironment
    {
        public static void SetupEnvironment(string binFolder)
        {
            SetEnvironmentVariables(binFolder);
        }

        public static void SetEnvironmentVariables(string binPath)
        {
            string ss = System.IO.Path.Combine(binPath, @"gdal-data");
            Gdal.SetConfigOption("GDAL_DATA", ss);
            Gdal.PushFinderLocation(System.IO.Path.Combine(binPath, @"gdal-data"));
            setValueNewVariable("GEOTIFF_CSV", System.IO.Path.Combine(binPath, @"gdal-data"));
            setValueNewVariable("GDAL_DRIVER_PATH", System.IO.Path.Combine(binPath, @"gdal-plugins"));
            setValueNewVariable("PROJ_LIB", System.IO.Path.Combine(binPath, @"proj\SHARE"));
        }

        private static void setValueNewVariable(string name, string value)
        {
            if (Environment.GetEnvironmentVariable(name) == null)
                Environment.SetEnvironmentVariable(name, value);
        }


    }

    #region Map Items
    public class Boat : INotifyPropertyChanged
    {
        private string name;
        private Location location;
        private double heading;
        private double course;
        private Visibility headingVisible;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public Location Location
        {
            get { return location; }
            set
            {
                location = value;
                OnPropertyChanged("Location");
            }
        }

        public double Heading
        {
            get { return heading; }
            set
            {
                heading = value;
                OnPropertyChanged("Heading");
            }
        }

        public double Course
        {
            get { return course; }
            set
            {
                course = value;
                OnPropertyChanged("Course");
            }
        }

        public Visibility HeadingVisible
        {
            get { return headingVisible; }
            set
            {
                headingVisible = value;
                OnPropertyChanged("HeadingVisible");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class Mark : INotifyPropertyChanged
    {
        private string name;
        private Location location;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public Location Location
        {
            get { return location; }
            set
            {
                location = value;
                OnPropertyChanged("Location");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class Leg : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Binding fromBinding;
        private Binding toBinding;

        private double distance, accDistance, bearing, twd, eta;

        private static readonly DependencyProperty FromLocationProperty = DependencyProperty.Register(
            "FromLocation", typeof(Location), typeof(Leg), new PropertyMetadata(null, FromLocationChanged, null));

        private static readonly DependencyProperty ToLocationProperty = DependencyProperty.Register(
            "ToLocation", typeof(Location), typeof(Leg), new PropertyMetadata(null, ToLocationChanged, null));

        public Location FromLocation
        {
            get { return (Location)GetValue(FromLocationProperty); }
            set { SetValue(FromLocationProperty, value); }
        }

        public Location ToLocation
        {
            get { return (Location)GetValue(ToLocationProperty); }
            set { SetValue(ToLocationProperty, value); }
        }

        public LocationCollection Locations { get; set; } // Used by Polyline

        public Leg(Mark m1, Mark m2)
            : base()
        {
            FromMark = m1;

            ToMark = m2;

            Locations = new LocationCollection();
            Locations.Add(FromLocation);
            Locations.Add(ToLocation);

            Distance = MainWindow.CalcDistance(Locations[0].Latitude, Locations[0].Longitude, Locations[1].Latitude, Locations[1].Longitude) / 1852;
            Bearing = MainWindow.CalcBearing(Locations[0].Latitude, Locations[0].Longitude, Locations[1].Latitude, Locations[1].Longitude);

        }

        private static void FromLocationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var leg = obj as Leg;
            var loc = e.NewValue as Location;
            if (leg.Locations != null)
            {
                leg.Locations[0] = loc;
                leg.Distance = MainWindow.CalcDistance(leg.Locations[0].Latitude, leg.Locations[0].Longitude, leg.Locations[1].Latitude, leg.Locations[1].Longitude) / 1852;
                leg.Bearing = MainWindow.CalcBearing(leg.Locations[0].Latitude, leg.Locations[0].Longitude, leg.Locations[1].Latitude, leg.Locations[1].Longitude);
                updateAccDistance(leg);
            }
        }

        private static void ToLocationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var leg = obj as Leg;
            var loc = e.NewValue as Location;
            if (leg.Locations != null)
            {
                leg.Locations[1] = loc;
                leg.Distance = MainWindow.CalcDistance(leg.Locations[0].Latitude, leg.Locations[0].Longitude, leg.Locations[1].Latitude, leg.Locations[1].Longitude) / 1852;
                leg.Bearing = MainWindow.CalcBearing(leg.Locations[0].Latitude, leg.Locations[0].Longitude, leg.Locations[1].Latitude, leg.Locations[1].Longitude);
                updateAccDistance(leg);
            }
        }

        private static void updateAccDistance(Leg leg)
        {
            do
            {
                if (leg.PreviousLeg == null)
                    leg.AccDistance = leg.Distance;
                else
                    leg.AccDistance = leg.PreviousLeg.AccDistance + leg.Distance;

                leg = leg.NextLeg;

            } while (leg != null);
        }

        public Mark FromMark
        {
            set
            {
                //BindingOperations.ClearBinding(this, FromLocationProperty);
                fromBinding = new Binding("Location");
                fromBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                fromBinding.Source = value;
                FromLocation = value.Location;
                BindingOperations.SetBinding(this, FromLocationProperty, fromBinding);
                OnPropertyChanged("FromMark");
            }
            get { return (Mark)fromBinding.Source; }
        }

        public Mark ToMark
        {
            set
            {
                toBinding = new Binding("Location");
                toBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                toBinding.Source = value;
                ToLocation = value.Location;
                BindingOperations.SetBinding(this, ToLocationProperty, toBinding);
                OnPropertyChanged("ToMark");

            }
            get { return (Mark)toBinding.Source; }
        }

        public Leg PreviousLeg;

        public Leg NextLeg;

        public double Distance
        {
            get { return distance; }
            set
            {
                distance = value;
                OnPropertyChanged("Distance");
            }
        }

        public double AccDistance
        {
            get { return accDistance; }
            set
            {
                accDistance = value;
                OnPropertyChanged("AccDistance");
            }
        }

        public double Bearing
        {
            get { return bearing; }
            set
            {
                if (value < 0) value += 360;
                bearing = value;
                OnPropertyChanged("Bearing");
            }
        }

        public double TWD
        {
            get { return twd; }
            set
            {
                twd = value;
                OnPropertyChanged("TWD");
            }
        }

        public double ETA
        {
            get { return eta; }
            set
            {
                eta = value;
                OnPropertyChanged("ETA");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class LinkedLegs : ObservableCollection<Leg>
    {

        protected override void InsertItem(int index, Leg item)
        {
            if (index > 0)
            {
                this[index - 1].NextLeg = item;
                item.PreviousLeg = this[index - 1];
                item.AccDistance = item.PreviousLeg.AccDistance + item.Distance;
            }
            else
            {
                item.AccDistance = item.Distance;
            }
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (this[index].PreviousLeg != null)
                this[index].PreviousLeg.NextLeg = this[index].NextLeg;
            if (this[index].NextLeg != null)
                this[index].NextLeg.PreviousLeg = this[index].PreviousLeg;

            base.RemoveItem(index);
        }

    }

    public class Route : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private string name;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public LinkedLegs Legs = new LinkedLegs();

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


    }

    public class SampleItemCollection : ObservableCollection<object>
    {
    }

    public class MarkItemsControl : MapItemsControl
    {
        public static readonly DependencyProperty ActiveItemProperty = DependencyProperty.RegisterAttached(
            "ActiveItem", typeof(Mark), typeof(MarkItemsControl),
            new PropertyMetadata((o, e) => ((MarkItemsControl)o).ActiveItemPropertyChanged((Mark)e.NewValue, (Mark)e.OldValue)));

        public static readonly DependencyProperty IsCurrentProperty = DependencyProperty.RegisterAttached(
            "IsCurrent", typeof(bool), typeof(MarkItemsControl), null);

        public Mark ActiveItem
        {
            get { return (Mark)GetValue(ActiveItemProperty); }
            set { SetValue(ActiveItemProperty, value); }
        }

        private void ActiveItemPropertyChanged(Mark newm, Mark oldm)
        {
            var ocontainer = ContainerFromItem(oldm);
            var ncontainer = ContainerFromItem(newm);

            if (ocontainer != null)
            {
                ocontainer.SetValue(IsCurrentProperty, false);
            }

            if (ncontainer != null)
            {
                ncontainer.SetValue(IsCurrentProperty, true);
            }
        }

        public UIElement ContainerFromItem(object item)
        {
            return item != null ? ItemContainerGenerator.ContainerFromItem(item) as UIElement : null;
        }

        public object ItemFromContainer(DependencyObject container)
        {
            return container != null ? ItemContainerGenerator.ItemFromContainer(container) : null;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MarkItem();
        }

        public static bool GetIsCurrent(UIElement element)
        {
            return (bool)element.GetValue(IsCurrentProperty);
        }

    }

    [TemplateVisualState(Name = "NotCurrent", GroupName = "CurrentStates")]
    [TemplateVisualState(Name = "Current", GroupName = "CurrentStates")]
    public class MarkItem : MapItem
    {
        public static readonly DependencyProperty IsCurrentProperty = MarkItemsControl.IsCurrentProperty.AddOwner(
            typeof(MapItem), new PropertyMetadata((o, e) => ((MarkItem)o).IsCurrentPropertyChanged((bool)e.NewValue)));

        /// <summary>
        /// Gets a value that indicates if the MapItem is the CurrentItem of the containing items collection.
        /// </summary>
        public bool IsCurrent
        {
            get { return (bool)GetValue(IsCurrentProperty); }
        }

        private void IsCurrentPropertyChanged(bool isCurrent)
        {
            var zIndex = Panel.GetZIndex(this);

            if (isCurrent)
            {
                Panel.SetZIndex(this, zIndex | 0x40000000);
                VisualStateManager.GoToState(this, "Current", true);
            }
            else
            {
                Panel.SetZIndex(this, zIndex & ~0x40000000);
                VisualStateManager.GoToState(this, "NotCurrent", true);
            }
        }
    }

    public class MapSegment : MapPath
    {
        public static readonly DependencyProperty FromLocationProperty = DependencyProperty.Register(
            "FromLocation", typeof(Location), typeof(MapSegment),
            new PropertyMetadata(null, LocationPropertyChanged));

        public static readonly DependencyProperty ToLocationProperty = DependencyProperty.Register(
            "ToLocation", typeof(Location), typeof(MapSegment),
            new PropertyMetadata(null, LocationPropertyChanged));

        public Location FromLocation
        {
            get { return (Location)GetValue(FromLocationProperty); }
            set { SetValue(FromLocationProperty, value); }
        }

        public Location ToLocation
        {
            get { return (Location)GetValue(ToLocationProperty); }
            set { SetValue(ToLocationProperty, value); }
        }

        public MapSegment()
        {
            Data = new StreamGeometry();
            this.Stroke = Brushes.Red;
            this.StrokeThickness = 3;
        }

        protected override void UpdateData()
        {
            var geometry = (StreamGeometry)Data;
            var l1 = FromLocation;
            var l2 = ToLocation;

            if (ParentMap != null && l1 != null && l2 != null)
            {
                using (var context = geometry.Open())
                {
                    var startPoint = ParentMap.MapProjection.LocationToPoint(l1);
                    var endPoint = ParentMap.MapProjection.LocationToPoint(l2);

                    context.BeginFigure(startPoint, false, false);
                    context.LineTo(endPoint, true, false);
                }                
            }
            else
            {
                geometry.Clear();
            }
        }

        private static void LocationPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var mapSegment = (MapSegment)obj;
            mapSegment.UpdateData();
            mapSegment.InvalidateVisual();
        }
    }

    public class Track : MapPanel
    {

        private MapSegment LastTrackSegment;
        private Color SegmentColor;
        private double minColorValue;
        private double minColorIndex;
        private double maxColorValue;
        private double maxColorIndex;

        public const int MaxLength = 240;

        public Track(CircularBuffer<IData<Location>>[] locations_buffer, CircularBuffer<IData<Double>>[] colors_buffer, int level,
                                        double minCval, double minCidx, double maxCval, double maxCidx)
        {
            var locations = locations_buffer[level];
            var colors = colors_buffer[level];

            //this.SegmentColor = Colors.MediumPurple;
            this.minColorValue = minCval;
            this.minColorIndex = minCidx;
            this.maxColorValue = maxCval;
            this.maxColorIndex = maxCidx;

            LinearGradientBrush cm = (LinearGradientBrush)App.Current.FindResource("ColorMap");

            if (locations.Size > 1)
            {
                Location toloc = locations.Read(0).Val;

                List<MapSegment> TrackSegments = new List<MapSegment>();

                int j = 0;

                for (int i = 1; i < Math.Min(locations.Size, Track.MaxLength); i++)
                {

                    Location fromloc = locations.Read(i).Val;
                    MapSegment ts = new MapSegment();

                    SolidColorBrush br;

                    if (colors.Size > 0)
                    {
                        while (j < colors.Size - 1 && locations.Read(i).Time < colors.Read(j).Time)
                            j++;

                        Color c = GetColor(colors.Read(j).Val, cm);
                        br = new SolidColorBrush(c);
                    }
                    else
                    {
                        br = Brushes.Lime;
                    }

                    ts.Stroke = br;
                    ts.StrokeThickness = 2;
                    ts.FromLocation = fromloc;
                    ts.ToLocation = toloc;

                    TrackSegments.Add(ts);

                    toloc = fromloc;
                }

                TrackSegments.Reverse();

                foreach (MapSegment ms in TrackSegments)
                    this.Children.Add(ms);

            }

            if (colors.Size > 0)
                SegmentColor = GetColor(colors.Read(0).Val, cm);

            LastTrackSegment = new MapSegment();
            LastTrackSegment.StrokeThickness = 2;
            LastTrackSegment.Stroke = new SolidColorBrush(SegmentColor);
            LastTrackSegment.FromLocation = GetLastLocationAvailable(locations_buffer, level);

            this.Children.Add(LastTrackSegment);

            locations.CollectionChanged += LocationCollectionChanged;
            colors.CollectionChanged += ColorsCollectionChanged;
            locations_buffer[0].CollectionChanged += CurrentLocationCollectionChanged;
        }

        private static Location GetLastLocationAvailable(CircularBuffer<IData<Location>>[] buffer, int level)
        {
            var locations = buffer[level];
            Location LastLocation = new Location();

            if (locations.Size > 0)
                LastLocation = locations.Read(0).Val;
            else
                if (level > 0)
                    LastLocation = GetLastLocationAvailable(buffer, level - 1);
                else
                    LastLocation = null;

            return LastLocation;
        }

        private Color GetColor(double val, LinearGradientBrush br)
        {

            double x = (maxColorIndex - minColorIndex) / (maxColorValue - minColorValue) * (val - minColorValue) + minColorIndex;

            if (x > maxColorIndex) x = maxColorIndex;
            if (x < minColorIndex) x = minColorIndex;

            //Clip the input if before or after the max/min offset values
            double max = br.GradientStops.Max(n => n.Offset);
            if (x > max)
            {
                x = max;
            }
            double min = br.GradientStops.Min(n => n.Offset);
            if (x < min)
            {
                x = min;
            }

            //Find gradient stops that surround the input value
            GradientStop gs0 = br.GradientStops.Where(n => n.Offset <= x).OrderBy(n => n.Offset).Last();
            GradientStop gs1 = br.GradientStops.Where(n => n.Offset >= x).OrderBy(n => n.Offset).First();

            float y = 0f;
            if (gs0.Offset != gs1.Offset)
            {
                y = (float)((x - gs0.Offset) / (gs1.Offset - gs0.Offset));
            }

            //Interpolate color channels
            Color cx = new Color();
            if (br.ColorInterpolationMode == ColorInterpolationMode.ScRgbLinearInterpolation)
            {
                float aVal = (gs1.Color.ScA - gs0.Color.ScA) * y + gs0.Color.ScA;
                float rVal = (gs1.Color.ScR - gs0.Color.ScR) * y + gs0.Color.ScR;
                float gVal = (gs1.Color.ScG - gs0.Color.ScG) * y + gs0.Color.ScG;
                float bVal = (gs1.Color.ScB - gs0.Color.ScB) * y + gs0.Color.ScB;
                cx = Color.FromScRgb(aVal, rVal, gVal, bVal);
            }
            else
            {
                byte aVal = (byte)((gs1.Color.A - gs0.Color.A) * y + gs0.Color.A);
                byte rVal = (byte)((gs1.Color.R - gs0.Color.R) * y + gs0.Color.R);
                byte gVal = (byte)((gs1.Color.G - gs0.Color.G) * y + gs0.Color.G);
                byte bVal = (byte)((gs1.Color.B - gs0.Color.B) * y + gs0.Color.B);
                cx = Color.FromArgb(aVal, rVal, gVal, bVal);
            }
            return cx;
        }

        private void LocationCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var locations = sender as CircularBuffer<IData<Location>>;

            if (locations.Size > 1)
            {
                MapSegment ts = new MapSegment();
                Brush br = new SolidColorBrush(SegmentColor);
                ts.Stroke = br;
                ts.StrokeThickness = 2;
                ts.FromLocation = locations.Read(1).Val;
                ts.ToLocation = locations.Read(0).Val;
                this.Children.Add(ts);

                LastTrackSegment.Stroke = br;
                LastTrackSegment.FromLocation = ts.ToLocation;
            }

            if (locations.Size > Track.MaxLength)
            {
                this.Children.RemoveAt(0);
            }
        }

        private void ColorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var colors = sender as CircularBuffer<IData<Double>>;
            LinearGradientBrush cm = (LinearGradientBrush)App.Current.FindResource("ColorMap");

            if (colors.Size > 0)
            {
                this.SegmentColor = GetColor(colors.Read(0).Val, cm);
            }
        }

        private void CurrentLocationCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var locations = sender as CircularBuffer<IData<Location>>;

            if (locations.Size > 0)
                LastTrackSegment.ToLocation = locations.Read(0).Val;
        }

    }

    public class MapMeasureRange : MapPath
    {
        public static readonly DependencyProperty FromLocationProperty = DependencyProperty.Register(
            "FromLocation", typeof(Location), typeof(MapMeasureRange),
            new PropertyMetadata(null, LocationPropertyChanged));

        public static readonly DependencyProperty ToLocationProperty = DependencyProperty.Register(
            "ToLocation", typeof(Location), typeof(MapMeasureRange),
            new PropertyMetadata(null, LocationPropertyChanged));

        public Location FromLocation
        {
            get { return (Location)GetValue(FromLocationProperty); }
            set { SetValue(FromLocationProperty, value); }
        }

        public Location ToLocation
        {
            get { return (Location)GetValue(ToLocationProperty); }
            set { SetValue(ToLocationProperty, value); }
        }

        public double TWD { get; set; }

        public MapMeasureRange()
        {
            Data = new StreamGeometry();
            this.Stroke = Brushes.LightGreen;
            this.StrokeDashArray = new DoubleCollection() { 2, 4 };
            this.StrokeThickness = 1;

        }

        protected override void UpdateData()
        {
            var geometry = (StreamGeometry)Data;
            var l1 = FromLocation;
            var l2 = ToLocation;

            if (ParentMap != null && l1 != null && l2 != null)
            {
                using (var context = geometry.Open())
                {
                    var center = ParentMap.MapProjection.LocationToPoint(l1);
                    var endPoint = ParentMap.MapProjection.LocationToPoint(l2);

                    var radius = Math.Sqrt((center.X - endPoint.X) * (center.X - endPoint.X) + (center.Y - endPoint.Y) * (center.Y - endPoint.Y));

                    var ptwd = new Point(center.X + radius * Math.Sin(TWD * Math.PI / 180), center.Y + radius * Math.Cos(TWD * Math.PI / 180));

                    double ControlPointRatio = (Math.Sqrt(2) - 1) * 4 / 3;

                    var x0 = center.X - radius;
                    var x1 = center.X - radius * ControlPointRatio;
                    var x2 = center.X;
                    var x3 = center.X + radius * ControlPointRatio;
                    var x4 = center.X + radius;

                    var y0 = center.Y - radius;
                    var y1 = center.Y - radius * ControlPointRatio;
                    var y2 = center.Y;
                    var y3 = center.Y + radius * ControlPointRatio;
                    var y4 = center.Y + radius;

                    context.BeginFigure(new Point(x2, y0), true, true);
                    //context.LineTo(endPoint, false, false);
                    //context.LineTo(center, false, false);
                    context.BezierTo(new Point(x3, y0), new Point(x4, y1), new Point(x4, y2), true, true);
                    context.BezierTo(new Point(x4, y3), new Point(x3, y4), new Point(x2, y4), true, true);
                    context.BezierTo(new Point(x1, y4), new Point(x0, y3), new Point(x0, y2), true, true);
                    context.BezierTo(new Point(x0, y1), new Point(x1, y0), new Point(x2, y0), true, true);

                    context.BeginFigure(center, true, true);
                    context.LineTo(endPoint, false, false);

                    context.BeginFigure(center, true, true);
                    context.LineTo(ptwd, false, false);

                }
            }
            else
            {
                geometry.Clear();
            }
        }

        private static void LocationPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var mapMeas = (MapMeasureRange)obj;
            mapMeas.UpdateData();
            mapMeas.InvalidateVisual();
        }

    }

    public class MeasureResult : INotifyPropertyChanged
    {
        private double _dst, _brg, _twa;

        private TimeSpan _ttg;

        public double DST
        {
            get
            { return _dst; }

            set
            {
                _dst = value;
                NotifyPropertyChanged("DST");
            }
        }

        public double BRG
        {
            get
            { return _brg; }

            set
            {
                _brg = value;
                NotifyPropertyChanged("BRG");
            }
        }

        public double TWA
        {
            get
            { return _twa; }

            set
            {
                _twa = value;
                NotifyPropertyChanged("TWA");
            }
        }

        public TimeSpan TTG
        {
            get
            { return _ttg; }

            set
            {
                _ttg = value;
                NotifyPropertyChanged("TTG");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

    }

    public class WindArrow : MapPath
    {
        private double Direction, Intensity, Scale;

        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
            "Location", typeof(Location), typeof(WindArrow),
            new PropertyMetadata(null, LocationPropertyChanged));

        public Location Location
        {
            get { return (Location)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        public WindArrow()
        {
            Data = new StreamGeometry();
            this.Stroke = Brushes.LightGreen;
            this.StrokeThickness = 2;
            Direction = 0;
            Intensity = 0;
            Scale = .04;
        }

        protected override void UpdateData()
        {
            var geometry = (StreamGeometry)Data;
            var l = Location;

            if (ParentMap != null && l != null)
            {

                var p1 = ParentMap.MapProjection.LocationToPoint(l);
                var pcenter = new Point(p1.X, p1.Y + 3);

                var p2 = new Point(pcenter.X, pcenter.Y + 3);
                var p3 = new Point(p2.X - 3, p2.Y);

                var p4 = new Point(pcenter.X, pcenter.Y + 2);
                var p5 = new Point(pcenter.X - 1.5, p4.Y);
                var p6 = new Point(p3.X, p4.Y);

                var p7 = new Point(pcenter.X, pcenter.Y + 1);
                var p8 = new Point(p5.X, p7.Y);
                var p9 = new Point(p6.X, p7.Y);

                var p10 = new Point(p8.X, pcenter.Y);
                var p11 = new Point(p9.X, p10.Y);

                using (var context = geometry.Open())
                {

                    context.BeginFigure(p1, true, false);
                    context.LineTo(p2, true, false);


                    if (Intensity == 1 || Intensity > 2)
                    {
                        context.LineTo(p4, false, false);
                        context.LineTo(p5, true, false);
                    }

                    if (Intensity > 1)
                    {
                        context.LineTo(p2, false, false);
                        context.LineTo(p3, true, false);
                    }

                    if (Intensity > 3)
                    {
                        context.LineTo(p5, false, false);
                        context.LineTo(p6, true, false);
                    }

                    if (Intensity > 4)
                    {
                        context.LineTo(p7, false, false);
                        context.LineTo(p8, true, false);
                    }

                    if (Intensity > 5)
                    {
                        context.LineTo(p8, false, false);
                        context.LineTo(p9, true, false);
                    }

                    if (Intensity > 6)
                    {
                        context.LineTo(pcenter, false, false);
                        context.LineTo(p10, true, false);
                    }

                    if (Intensity > 7)
                    {
                        context.LineTo(p10, false, false);
                        context.LineTo(p11, true, false);
                    }
                }

                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new RotateTransform(Direction, p1.X, p1.Y));
                tg.Children.Add(new ScaleTransform(Scale, Scale, p1.X, p1.Y));
                geometry.Transform = tg;

            }
            else
            {
                geometry.Clear();
            }
        }

        private static void LocationPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var wa = (WindArrow)obj;
            wa.UpdateData();
            wa.InvalidateVisual();
        }

        public void Set(double dir, double tws, LinearGradientBrush lgbr)
        {
            Direction = -dir;
            Intensity = Math.Truncate((tws + 2.5) / 5);
            Color c = GetColor(tws / 30, lgbr);
            SolidColorBrush br = new SolidColorBrush(c);
            this.Stroke = br;
            this.UpdateData();
            //this.InvalidateVisual();
        }

        public void SetScale(double sc)
        {
            this.Scale = sc;
            this.UpdateData();
            this.InvalidateVisual();
        }

        private Color GetColor(double x, LinearGradientBrush br)
        {

            //Clip the input if before or after the max/min offset values
            double max = br.GradientStops.Max(n => n.Offset);
            if (x > max)
            {
                x = max;
            }
            double min = br.GradientStops.Min(n => n.Offset);
            if (x < min)
            {
                x = min;
            }

            //Find gradient stops that surround the input value
            GradientStop gs0 = br.GradientStops.Where(n => n.Offset <= x).OrderBy(n => n.Offset).Last();
            GradientStop gs1 = br.GradientStops.Where(n => n.Offset >= x).OrderBy(n => n.Offset).First();

            float y = 0f;
            if (gs0.Offset != gs1.Offset)
            {
                y = (float)((x - gs0.Offset) / (gs1.Offset - gs0.Offset));
            }

            //Interpolate color channels
            Color cx = new Color();
            if (br.ColorInterpolationMode == ColorInterpolationMode.ScRgbLinearInterpolation)
            {
                float aVal = (gs1.Color.ScA - gs0.Color.ScA) * y + gs0.Color.ScA;
                float rVal = (gs1.Color.ScR - gs0.Color.ScR) * y + gs0.Color.ScR;
                float gVal = (gs1.Color.ScG - gs0.Color.ScG) * y + gs0.Color.ScG;
                float bVal = (gs1.Color.ScB - gs0.Color.ScB) * y + gs0.Color.ScB;
                cx = Color.FromScRgb(aVal, rVal, gVal, bVal);
            }
            else
            {
                byte aVal = (byte)((gs1.Color.A - gs0.Color.A) * y + gs0.Color.A);
                byte rVal = (byte)((gs1.Color.R - gs0.Color.R) * y + gs0.Color.R);
                byte gVal = (byte)((gs1.Color.G - gs0.Color.G) * y + gs0.Color.G);
                byte bVal = (byte)((gs1.Color.B - gs0.Color.B) * y + gs0.Color.B);
                cx = Color.FromArgb(aVal, rVal, gVal, bVal);
            }
            return cx;
        }

    }

    public class CurrentArrow : MapPath
    {
        private double Direction, Intensity, Scale;

        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
            "Location", typeof(Location), typeof(CurrentArrow),
            new PropertyMetadata(null, LocationPropertyChanged));

        public Location Location
        {
            get { return (Location)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        public CurrentArrow()
        {
            Data = new StreamGeometry();
            this.Stroke = Brushes.Green;
            //this.Fill = Brushes.Green;
            this.StrokeThickness = 2;
            Direction = 0;
            Intensity = 0;
            Scale = .12;
        }

        protected override void UpdateData()
        {
            var geometry = (StreamGeometry)Data;
            var l = Location;

            if (ParentMap != null && l != null)
            {

                var p1 = ParentMap.MapProjection.LocationToPoint(l);
                var p2 = new Point(p1.X , p1.Y + 9);
                var p3 = new Point(p1.X +3, p1.Y + 9);

                using (var context = geometry.Open())
                {
                    context.BeginFigure(p1, false, false);
                    context.LineTo(p2, true, false);
                    context.LineTo(p3, true, false);

                    TransformGroup tg = new TransformGroup();
                    tg.Children.Add(new RotateTransform(Direction, p1.X, p1.Y));
                    tg.Children.Add(new ScaleTransform(Scale, Scale, p1.X, p1.Y));                    
                    geometry.Transform = tg;
                }
            }
            else
            {
                geometry.Clear();
            }
        }

        private static void LocationPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var ca = (CurrentArrow)obj;
            ca.UpdateData();
            ca.InvalidateVisual();
        }

        public void Set(double dir, double cspd)
        {
            Direction = -dir;
            //Intensity = Math.Truncate((cspd + 2.5) / 5);
            //Color c = GetColor(cspd / 30, lgbr);
            //SolidColorBrush br = new SolidColorBrush(c);
            this.Scale = cspd;
            this.UpdateData();
            //this.InvalidateVisual();
        }

        public void SetScale(double sc)
        {
            this.Scale = sc;
            this.UpdateData();
            this.InvalidateVisual();
        }

    }

    public class ColorDot : MapPath
    {
        private double Intensity, Scale;

        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
            "Location", typeof(Location), typeof(ColorDot),
            new PropertyMetadata(null, LocationPropertyChanged));

        public Location Location
        {
            get { return (Location)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        public ColorDot()
        {
            Data = new StreamGeometry();
            Intensity = 0;
            Scale = .01;
        }

        protected override void UpdateData()
        {
            var geometry = (StreamGeometry)Data;
            var l = Location;

            if (ParentMap != null && l != null)
            {

                var pcenter = ParentMap.MapProjection.LocationToPoint(l);

                var p1 = new Point(pcenter.X + 1, pcenter.Y + 1);
                var p2 = new Point(pcenter.X - 1, pcenter.Y + 1);
                var p3 = new Point(pcenter.X - 1, pcenter.Y - 1);
                var p4 = new Point(pcenter.X + 1, pcenter.Y - 1);

                using (var context = geometry.Open())
                {

                    context.BeginFigure(p1, true, true);
                    context.LineTo(p2, false, false);
                    context.LineTo(p3, false, false);
                    context.LineTo(p4, false, false);

                    TransformGroup tg = new TransformGroup();
                    tg.Children.Add(new ScaleTransform(Scale, Scale, pcenter.X, pcenter.Y));                    
                    geometry.Transform = tg;
                }
            }
            else
            {
                geometry.Clear();
            }
        }

        private static void LocationPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var wa = (ColorDot)obj;
            wa.UpdateData();
            wa.InvalidateVisual();
        }

        public void SetColor(double it, LinearGradientBrush lgbr)
        {
            Color c = GetColor(it, lgbr);
            SolidColorBrush br = new SolidColorBrush(c);
            this.Fill = br;
            this.UpdateData();
            this.InvalidateVisual();
        }

        public void SetScale(double sc)
        {
            this.Scale = sc;
            this.UpdateData();
            this.InvalidateVisual();
        }

        private Color GetColor(double x, LinearGradientBrush br)
        {

            //Clip the input if before or after the max/min offset values
            double max = br.GradientStops.Max(n => n.Offset);
            if (x > max)
            {
                x = max;
            }
            double min = br.GradientStops.Min(n => n.Offset);
            if (x < min)
            {
                x = min;
            }

            //Find gradient stops that surround the input value
            GradientStop gs0 = br.GradientStops.Where(n => n.Offset <= x).OrderBy(n => n.Offset).Last();
            GradientStop gs1 = br.GradientStops.Where(n => n.Offset >= x).OrderBy(n => n.Offset).First();

            float y = 0f;
            if (gs0.Offset != gs1.Offset)
            {
                y = (float)((x - gs0.Offset) / (gs1.Offset - gs0.Offset));
            }

            //Interpolate color channels
            Color cx = new Color();
            if (br.ColorInterpolationMode == ColorInterpolationMode.ScRgbLinearInterpolation)
            {
                float aVal = (gs1.Color.ScA - gs0.Color.ScA) * y + gs0.Color.ScA;
                float rVal = (gs1.Color.ScR - gs0.Color.ScR) * y + gs0.Color.ScR;
                float gVal = (gs1.Color.ScG - gs0.Color.ScG) * y + gs0.Color.ScG;
                float bVal = (gs1.Color.ScB - gs0.Color.ScB) * y + gs0.Color.ScB;
                cx = Color.FromScRgb(aVal, rVal, gVal, bVal);
            }
            else
            {
                byte aVal = (byte)((gs1.Color.A - gs0.Color.A) * y + gs0.Color.A);
                byte rVal = (byte)((gs1.Color.R - gs0.Color.R) * y + gs0.Color.R);
                byte gVal = (byte)((gs1.Color.G - gs0.Color.G) * y + gs0.Color.G);
                byte bVal = (byte)((gs1.Color.B - gs0.Color.B) * y + gs0.Color.B);
                cx = Color.FromArgb(aVal, rVal, gVal, bVal);
            }
            return cx;
        }

    }
    
    public class WindArrowGrid : MapPanel
    {
        public WindArrowGrid(windgrib wgrib,double scale)
        {
            foreach (uvpair uv in wgrib.band[0].data)
            {
                var wa = new WindArrow();
                wa.Location = new Location(uv.Lat, uv.Lon);
                wa.SetScale(scale);
                this.Children.Add(wa);
            }
        }

        public void Update(windgrib wgrib, DateTime dt)
        {
            LinearGradientBrush cm = (LinearGradientBrush)App.Current.FindResource("ColorMap");

            int idx=0;
            double distance=0;

            if (wgrib.GetBandIndex(dt, ref idx, ref distance))
            {
                foreach (WindArrow wa in Children)
                {
                    uvpair uv = new uvpair();
                    uv = wgrib.GetWind(wa.Location.Latitude, wa.Location.Longitude, idx, distance);

                    if (uv != null)
                    {
                        double u = uv.u;
                        double v = uv.v;

                        double ang = Math.Atan2(u, v) * 180 / Math.PI;
                        double it = Math.Sqrt(u * u + v * v) * 3600 / 1852;

                        wa.Set(ang + 180, it, cm);
                    }
                    else
                    {
                        wa.Visibility = Visibility.Hidden;
                    }
                } 
            }
        }
    }

    public class CurrentArrowGrid : MapPanel
    {
        private double Scale;

        public CurrentArrowGrid(currentgrib cgrib,double scale)
        {
            this.Scale = scale;
            foreach (uvpair uv in cgrib.band[0].data)
            {
                if (uv != null)
                {
                    var ca = new CurrentArrow();
                    ca.Location = new Location(uv.Lat, uv.Lon);
                    this.Children.Add(ca);
                }
            }
        }

        public void Update(currentgrib cgrib, DateTime dt)
        {

            int idx = 0;
            double distance = 0;

            cgrib.GetBandIndex(dt, ref idx, ref distance);

            foreach (CurrentArrow ca in Children)
            {
                uvpair uv = new uvpair();
                uv = cgrib.GetCurrent(ca.Location.Latitude, ca.Location.Longitude, idx, distance);

                if (uv != null)
                {
                    double u = uv.u;
                    double v = uv.v;

                    double ang = Math.Atan2(u, v) * 180 / Math.PI + 180;
                    double it = Math.Sqrt(u * u + v * v) * 3600 / 1852;

                    ca.Set(ang, it * Scale * 0.07);
                    ca.ToolTip = it.ToString("0.0");
                }
                else
                    ca.Visibility = Visibility.Hidden;
            }
        }
    }


    #endregion
    
    #region Routing

    public class Vertex
    {
        public Location Position { get; set; }
        public List<Vertex> Neighbors;
        public DateTime Cost { get; set; }
        public Boolean Visited { get; set; }
        public Vertex Previous;

        public double TWD { get; set; }
        public double TWS { get; set; }
        public double SPD { get; set; }
        public double TWA { get; set; }
        public double BRG { get; set; }
        public double DRIFT { get; set; }
        public double SET { get; set; }



        public Vertex(Location pos,DateTime cost)
        {
            this.Neighbors = new List<Vertex>();
            this.Position = pos;
            this.Cost = cost;
            Visited = false;
        }
    }

    public class RoutingResult : MapPanel
    {
        public List<Vertex> vertexList;         // Complete list
        public List<Vertex> vertexReplayList;   // Decimated list to replay track

        public string ID { get; set; }

        public RoutingResult(List<Vertex> vList)
        {

            vertexList = new List<Vertex>();
            vertexList.AddRange(vList);   // Expects a list sorted by time (cost)

            vertexReplayList = new List<Vertex>();
            vertexReplayList.Add(vertexList[0]);

            // Only add Vertex to replay list if timespan is greater than ts1
            TimeSpan ts = vertexList[vertexList.Count() - 1].Cost - vertexList[0].Cost;
            TimeSpan ts1 = new TimeSpan(ts.Ticks / 200);
            DateTime nextDT = vertexList[0].Cost.Add(ts1);

            LinearGradientBrush cmx = (LinearGradientBrush)App.Current.FindResource("ColorMap");
                        
            for(int i=0;i<vertexList.Count-1;i++)
            {
                if (vertexList[i].Cost > nextDT)
                {
                    vertexReplayList.Add(vertexList[i]);
                    nextDT = vertexList[i].Cost.Add(ts1);
                }

                MapSegment ms = new MapSegment();
                ms.FromLocation = vertexList[i].Position;
                ms.ToLocation = vertexList[i + 1].Position;
                ms.Stroke = Brushes.DarkRed;
                this.Children.Add(ms);
            }

            for (int i = 0; i < vertexReplayList.Count - 1; i++)
            {
                WindArrow wa = new WindArrow();
                wa.Location = vertexReplayList[i].Position;

                wa.Set(vertexReplayList[i].TWD, vertexReplayList[i].TWS, cmx);
                wa.SetScale(.004);
                this.Children.Add(wa);
            }

            vertexReplayList.Add(vertexList[vertexList.Count() - 1]);
            vertexReplayList[0].BRG = vertexReplayList[1].BRG;            
        }

        public void Select()
        {
            foreach(UIElement uie in Children)
            {
                if(uie is MapSegment)
                {
                    var ms = uie as MapSegment;
                    ms.Stroke = Brushes.DarkRed;
                    ms.StrokeThickness = 5;
                }

                this.ShowWindArrows();
            }            
        }

        public void UnSelect()
        {
            foreach (UIElement uie in Children)
            {
                if (uie is MapSegment)
                {
                    var ms = uie as MapSegment;
                    ms.Stroke = Brushes.DimGray;
                    ms.StrokeThickness = 3;
                }

                this.HideWindArrows();
            }
        }

        public void ShowWindArrows()
        {
            foreach (UIElement uie in Children)
            {
                if (uie is WindArrow)
                {
                    var wa = uie as WindArrow;
                    wa.Visibility = Visibility.Visible;
                }
            }
        }

        public void HideWindArrows()
        {
            foreach (UIElement uie in Children)
            {
                if (uie is WindArrow)
                {
                    var wa = uie as WindArrow;
                    wa.Visibility = Visibility.Hidden;
                }
            }
        }

    } 

    #endregion

    [ValueConversion(typeof(double), typeof(String))]
    public class PositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value as Location;
            double deg = Math.Truncate(v.Latitude);
            double min = Math.Abs((double)v.Latitude - deg) * 60;
            string s1 = "N";
            if (deg < 0)
                s1 = "S";
            s1 += Math.Abs(deg).ToString("00") + " " + min.ToString("00.00");

            deg = Math.Truncate(v.Longitude);
            min = Math.Abs((double)v.Longitude - deg) * 60;
            string s2 = "E";
            if (deg < 0)
                s2 = "W";
            s2 += Math.Abs(deg).ToString("00") + " " + min.ToString("00.00");

            return s1 + "   " + s2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (string)value;
            List<string> dm = new List<string>(s.Split(' '));
            dm.RemoveAll(r => string.IsNullOrEmpty(r));

            Location loc = new Location();

            if (dm.Count() == 4)
            {
                double deg, min;

                if (dm[0][0] == 'N')
                {
                    dm[0].TrimStart('N');
                    double.TryParse(dm[0].Substring(1), out deg);
                    double.TryParse(dm[1], out min);
                    loc.Latitude = deg + min / 60;
                }

                if (dm[0][0] == 'S')
                {
                    double.TryParse(dm[0].Substring(1), out deg);
                    double.TryParse(dm[1], out min);
                    loc.Latitude = -deg - min / 60;
                }

                if (dm[2][0] == 'E')
                {
                    dm[0].TrimStart('E');
                    double.TryParse(dm[2].Substring(1), out deg);
                    double.TryParse(dm[3], out min);
                    loc.Longitude = deg + min / 60;
                }

                if (dm[2][0] == 'W')
                {
                    dm[0].TrimStart('W');
                    double.TryParse(dm[2].Substring(1), out deg);
                    double.TryParse(dm[3], out min);
                    loc.Longitude = -deg - min / 60;
                }

                return loc;
            }
            else
                return null;
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class VisToCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value == true)
                return Visibility.Visible;
            else
                return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((Visibility)value == Visibility.Visible)
                return true;
            else
                return false;
        }
    }

    public static class CommandLibrary
    {
        private static RoutedUICommand addMark = new RoutedUICommand("AddMark", "AddMark", typeof(CommandLibrary));
        private static RoutedUICommand deleteMark = new RoutedUICommand("DeleteMark", "DeleteMark", typeof(CommandLibrary));
        private static RoutedUICommand navigateTo = new RoutedUICommand("NavigateTo", "NavigateTo", typeof(CommandLibrary));
        private static RoutedUICommand activateRoute = new RoutedUICommand("ActivateRoute", "ActivateRoute", typeof(CommandLibrary));
        private static RoutedUICommand stopNav = new RoutedUICommand("StopNav", "StopNav", typeof(CommandLibrary));
        private static RoutedUICommand newRoute = new RoutedUICommand("NewRoute", "NewRoute", typeof(CommandLibrary));
        private static RoutedUICommand fwdRoute = new RoutedUICommand("FwdRoute", "FwdRoute", typeof(CommandLibrary));
        private static RoutedUICommand rwdRoute = new RoutedUICommand("RwdRoute", "RwdRoute", typeof(CommandLibrary));
        private static RoutedUICommand reverseRoute = new RoutedUICommand("ReverseRoute", "ReverseRoute", typeof(CommandLibrary));
        private static RoutedUICommand deleteRoute = new RoutedUICommand("DeleteRoute", "DeleteRoute", typeof(CommandLibrary));
        private static RoutedUICommand setLineBoat = new RoutedUICommand("SetLineBoat", "SetLineBoat", typeof(CommandLibrary));
        private static RoutedUICommand setLinePin = new RoutedUICommand("SetLinePin", "SetLinePin", typeof(CommandLibrary));

        public static RoutedUICommand AddMark
        {
            get { return addMark; }
        }

        public static RoutedUICommand DeleteMark
        {
            get { return deleteMark; }
        }

        public static RoutedUICommand NavigateTo
        {
            get { return navigateTo; }
        }

        public static RoutedUICommand ActivateRoute
        {
            get { return activateRoute; }
        }

        public static RoutedUICommand StopNav
        {
            get { return stopNav; }
        }

        public static RoutedUICommand NewRoute
        {
            get { return newRoute; }
        }

        public static RoutedUICommand FwdRoute
        {
            get { return fwdRoute; }
        }

        public static RoutedUICommand RwdRoute
        {
            get { return rwdRoute; }
        }

        public static RoutedUICommand ReverseRoute
        {
            get { return reverseRoute; }
        }

        public static RoutedUICommand DeleteRoute
        {
            get { return deleteRoute; }
        }

        public static RoutedUICommand SetLineBoat
        {
            get { return setLineBoat; }
        }

        public static RoutedUICommand SetLinePin
        {
            get { return setLinePin; }
        }
    }

    public enum ReceiverResult
    {
        DataReceived,
        NoData,
        WrongData
    }

    public class DataReceiverStatus
    {
        public ReceiverResult Result;
        public string Error;

        public DataReceiverStatus()
        {
            Result = ReceiverResult.NoData;
        }
    }
}


