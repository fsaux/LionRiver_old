using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;

namespace LionRiver
{
    public partial class MainWindow : Window
    {
        public static double ConvertToDeg(string pos)
        {
            double deg = 0, min = 0;
            int i = pos.IndexOf('.');
            if (i != -1)
            {
                deg = double.Parse(pos.Substring(0, i - 2));
                min = double.Parse(pos.Substring(i - 2));
            }
            else
            {
                deg = double.Parse(pos.Substring(0, pos.Length - 2));
            }

            return deg + min / 60;
        }

        public static void CalcPosition(double lat, double lon, double dist, double bearing, ref double nlat, ref double nlon)
        {
            double q;
            lat = lat * Math.PI / 180;
            lon = lon * Math.PI / 180;

            nlat = lat + dist / 6371000 * Math.Cos(bearing * Math.PI / 180);
            double dphi = Math.Log(Math.Tan(nlat / 2 + Math.PI / 4) / Math.Tan(lat / 2 + Math.PI / 4));
            if (bearing == 90 || bearing == 270)
                q = Math.Cos(lat);
            else
                q = (nlat - lat) / dphi;
            double dlon = dist / 6371000 * Math.Sin(bearing * Math.PI / 180) / q;
            nlon = (lon + dlon + Math.PI) % (2 * Math.PI) - Math.PI;

            nlat = nlat * 180 / Math.PI;
            nlon = nlon * 180 / Math.PI;

        }

        public static double CalcBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double brg;

            lat1 = lat1 * Math.PI / 180;
            lon1 = lon1 * Math.PI / 180;
            lat2 = lat2 * Math.PI / 180;
            lon2 = lon2 * Math.PI / 180;

            double dphi = Math.Log(Math.Tan(lat2 / 2 + Math.PI / 4) / Math.Tan(lat1 / 2 + Math.PI / 4));
            brg = Math.Atan2(lon2 - lon1, dphi) * 180 / Math.PI;

            return brg;
        }

        public static double CalcDistance(double lat1, double lon1, double lat2, double lon2)
        {

            double dst,q,tc;

            lat1 = lat1 * Math.PI / 180;
            lon1 = lon1 * Math.PI / 180;
            lat2 = lat2 * Math.PI / 180;
            lon2 = lon2 * Math.PI / 180;

            double dlon_W = (lon2 - lon1) % (2 * Math.PI);
            double dlon_E = (lon1 - lon2) % (2 * Math.PI);
            double dphi = Math.Log(Math.Tan(lat2 / 2 + Math.PI / 4) / Math.Tan(lat1 / 2 + Math.PI / 4));
            if (Math.Abs(lat2 - lat1) < 1e-15)
                q = Math.Cos(lat1);
            else
                q = (lat2 - lat1) / dphi;
            if (dlon_W < dlon_E)
            {
                tc = Math.Atan2(-dlon_W, dphi) % (2 * Math.PI);
                dst = Math.Sqrt(Math.Pow(q * dlon_W, 2) + Math.Pow(lat2 - lat1, 2));
            }
            else
            {
                tc = Math.Atan2(dlon_E, dphi) % (2 * Math.PI);
                dst = Math.Sqrt(Math.Pow(q * dlon_E, 2) + Math.Pow(lat2 - lat1, 2));
            }

            return dst * 6371000;
        }

        public static double ConvertTo180(double ang)
        {
            if (ang > 180) return 360 - ang;
            else
                return ang;
        }

        public void CalcNav(DateTime now, bool bypassComm = false)
        {
            #region Primitives
            if (rmc_received || bypassComm)
            {
                LAT.Val = lat;
                LON.Val = lon;
                SOG.Val = sog;
                COG.Val = cog;
                LAT.SetValid(now);
                LON.SetValid(now);
                SOG.SetValid(now);
                COG.SetValid(now);
                RMC_received_Timer.Start();
            }

            if (vhw_received || bypassComm)
            {
                SPD.Val = spd;
                SPD.SetValid(now);
            }

            if (dpt_received || bypassComm)
            {
                DPT.Val = dpt;
                DPT.SetValid(now);
            }

            if (mwv_received || bypassComm)
            {
                AWA.Val = awa;
                AWS.Val = aws;
                AWA.SetValid(now);
                AWS.SetValid(now);
            }

            if (mtw_received || bypassComm)
            {
                TEMP.Val = temp;
                TEMP.SetValid(now);
            }

            if (hdg_received || bypassComm)
            {
                double mv = Properties.Settings.Default.MagVar; //default
                if (mvar2 != 0) mv = mvar2;                     //From HDG
                if (mvar1 != 0) mv = mvar1;                     //From RMC

                MVAR.Val = mv;
                MVAR.SetValid(now);

                if (bypassComm)
                    mv = 0;         // heading from log file is "true heading" no need for correction

                HDT.Val = hdg + mv;
                HDT.SetValid(now);
            }

            #endregion

            #region Position, Leg bearing, distance, XTE and VMG

            if (LAT.IsValid() && LON.IsValid())
            {
                POS.Val.Latitude = LAT.Val;
                POS.Val.Longitude = LON.Val;
                POS.SetValid(now);
            }
            else
            {
                POS.Invalidate();
            }

            if (ActiveLeg != null)
            {
                LWLAT.Val = ActiveLeg.FromLocation.Latitude;
                LWLAT.SetValid(now);
                LWLON.Val = ActiveLeg.FromLocation.Longitude;
                LWLON.SetValid(now);
                LWPT.Val.str = ActiveLeg.FromMark.Name;
                LWPT.SetValid(now);
            }
            else
            {
                LWLAT.Invalidate();
                LWLON.Invalidate();
                LWPT.Invalidate();
            }

            if (!bypassComm || replayLog)
            {
                if (ActiveMark != null && POS.IsValid())
                {
                    WLAT.Val = ActiveMark.Location.Latitude;
                    WLAT.SetValid(now);
                    WLON.Val = ActiveMark.Location.Longitude;
                    WLON.SetValid(now);
                    WPT.Val.str = ActiveMark.Name;
                    WPT.SetValid(now);
                    BRG.Val = CalcBearing(LAT.Val, LON.Val, WLAT.Val, WLON.Val);
                    BRG.SetValid(now);
                    DST.Val = CalcDistance(LAT.Val, LON.Val, WLAT.Val, WLON.Val) / 1852;
                    DST.SetValid(now);
                }
                else
                {
                    WLAT.Invalidate();
                    WLON.Invalidate();
                    WPT.Invalidate();
                    BRG.IsValid();
                    DST.IsValid();
                }
            }

            if (WPT.IsValid() && LWPT.IsValid())
            {
                LEGBRG.Val = CalcBearing(LWLAT.Val, LWLON.Val, WLAT.Val, WLON.Val);
                LEGBRG.SetValid(now);
            }
            else
            {
                if (LEGBRG.IsValid())
                    LEGBRG.Invalidate();
            }

            if (LWPT.IsValid())
            {
                XTE.Val = Math.Asin(Math.Sin(DST.Val * 1.852 / 6371) * Math.Sin((BRG.Val - LEGBRG.Val) * Math.PI / 180)) * 6371 / 1.852;
                XTE.SetValid(now);
            }
            else
                if (XTE.IsValid())
                    XTE.Invalidate();

            if (SOG.IsValid() && BRG.IsValid())
            {
                VMGWPT.Val = SOG.Val * Math.Cos((COG.Val - BRG.Val) * Math.PI / 180);
                VMGWPT.SetValid(now);
            }
            else
            {
                if (VMGWPT.IsValid())
                    VMGWPT.Invalidate();
            }
            #endregion

            #region True Wind
            if (AWA.IsValid() && SPD.IsValid())
            {
                double Dx = AWS.Val * Math.Cos(AWA.Val * Math.PI / 180) - SPD.Val;
                double Dy = AWS.Val * Math.Sin(AWA.Val * Math.PI / 180);
                TWS.Val = Math.Sqrt(Dx * Dx + Dy * Dy);
                TWS.SetValid(now);
                TWA.Val = Math.Atan2(Dy, Dx) * 180 / Math.PI;
                TWA.SetValid(now);
                VMG.Val = SPD.Val * Math.Cos(TWA.Val * Math.PI / 180);
                VMG.SetValid(now);
            }
            else
            {
                if (TWS.IsValid())
                    TWS.Invalidate();
                if (TWA.IsValid())
                    TWA.Invalidate();
                if (VMG.IsValid())
                    VMG.Invalidate();
            }

            if (TWS.IsValid() && HDT.IsValid())
            {
                TWD.Val = HDT.Val + TWA.Val;
                TWD.SetValid(now);
            }
            else
            {
                if (TWD.IsValid())
                    TWD.Invalidate();
            }
            #endregion

            #region Heel
            //if (AWA.IsValid() && SPD.IsValid())
            //{
            //    double k = 7,
            //            a = 2,
            //            b = 200,
            //            c = 1.5;

            //    var awa = Math.Abs(AWA.Val);
            //    var aws = AWS.Val;

            //    HEEL.Val = k * awa * Math.Pow(aws, c) / (Math.Pow(awa, a) + b);
            //    if (HEEL.Val > 45) HEEL.Val = 45;
            //    HEEL.SetValid(now);
            //}
            //else
            //{
            //    if (HEEL.IsValid())
            //        HEEL.Invalidate();
            //}

            #endregion

            #region Drift
            if (SOG.IsValid() && COG.IsValid() && HDT.IsValid())
            {
                double Dx = SOG.Val * Math.Cos(COG.Val * Math.PI / 180) - SPD.Val * Math.Cos(HDT.Val * Math.PI / 180);
                double Dy = SOG.Val * Math.Sin(COG.Val * Math.PI / 180) - SPD.Val * Math.Sin(HDT.Val * Math.PI / 180);
                DRIFT.Val = Math.Sqrt(Dx * Dx + Dy * Dy);
                DRIFT.SetValid(now);
                SET.Val = Math.Atan2(Dy, Dx) * 180 / Math.PI;
                SET.SetValid(now);
            }
            else
            {
                if (DRIFT.IsValid())
                    DRIFT.Invalidate();
                if (SET.IsValid())
                    SET.Invalidate();
            }
            #endregion

            #region Performance
            if (BRG.IsValid() && TWD.IsValid() && SPD.IsValid() && NavPolar.IsLoaded)
            {
                double Angle = Math.Abs((TWD.Val - BRG.Val) % 360);
                if (Angle > 180) Angle = 360 - Angle;

                PolarPoint pb = NavPolar.GetBeatTarget(TWS.Average(Inst.BufHalfMin));
                PolarPoint pr = NavPolar.GetRunTarget(TWS.Average(Inst.BufHalfMin));

                if (Math.Abs(Angle) <= pb.TWA) // Beating
                {
                    TGTSPD.Val = pb.SPD;
                    TGTSPD.SetValid(now);
                    TGTTWA.Val = pb.TWA;
                    TGTTWA.SetValid(now);
                    PERF.Val = VMG.Val / (pb.SPD * Math.Cos(pb.TWA * Math.PI / 180));
                    PERF.SetValid(now);

                    sailingMode = SailingMode.Beating;
                }

                if (Math.Abs(Angle) < pr.TWA && Math.Abs(Angle) > pb.TWA) // Reaching
                {
                    TGTSPD.Val = NavPolar.GetTarget(Math.Abs(TWA.Average(Inst.BufHalfMin)), TWS.Average(Inst.BufHalfMin));
                    TGTSPD.SetValid(now);
                    TGTTWA.Val = Math.Abs(TWA.Val);
                    TGTTWA.SetValid(now);
                    PERF.Val = SPD.Val / TGTSPD.Val;
                    if (VMGWPT.Val < 0) PERF.Val = -PERF.Val;
                    PERF.SetValid(now);

                    sailingMode = SailingMode.Reaching;
                }

                if (Math.Abs(Angle) >= pr.TWA) // Running
                {
                    TGTSPD.Val = pr.SPD;
                    TGTSPD.SetValid(now);
                    TGTTWA.Val = pr.TWA;
                    TGTTWA.SetValid(now);
                    PERF.Val = VMG.Val / (pr.SPD * Math.Cos(pr.TWA * Math.PI / 180));
                    PERF.SetValid(now);

                    sailingMode = SailingMode.Running;
                }

            }
            else
            {
                if (TGTSPD.IsValid())
                    TGTSPD.Invalidate();
                if (TGTTWA.IsValid())
                    TGTTWA.Invalidate();
                if (PERF.IsValid())
                    PERF.Invalidate();

                sailingMode = SailingMode.None;
            }
            #endregion

            #region Line
            if (p1_set && p2_set && LAT.IsValid() && HDT.IsValid())
            {
                double p3_lat = LAT.Val, p3_lon = LON.Val;

                if (Properties.Settings.Default.GPSoffsetToBow != 0)
                    CalcPosition(LAT.Val, LON.Val, Properties.Settings.Default.GPSoffsetToBow, HDT.Val, ref p3_lat, ref p3_lon);
                double brg32 = CalcBearing(p3_lat, p3_lon, p2_lat, p2_lon);
                double dst32 = CalcDistance(p3_lat, p3_lon, p2_lat, p2_lon);

                LINEDST.Val = dst32 * Math.Sin((linebrg - brg32) * Math.PI / 180);
                LINEDST.SetValid(now);
            }
            else
            {
                if (LINEDST.IsValid())
                    LINEDST.Invalidate();
            }
            #endregion

            #region Route nav
            if (!bypassComm)
            {
                if (ActiveMark != null && DST.IsValid() && !ManOverBoard)
                {
                    if (DST.Val <= Properties.Settings.Default.WptProximity)
                    {
                        (new SoundPlayer(@".\Sounds\BELL7.WAV")).PlaySync();
                        if (ActiveLeg != null)
                        {
                            if (ActiveLeg.NextLeg != null)
                            {
                                ActiveLeg = ActiveLeg.NextLeg;
                                ActiveMark = ActiveLeg.ToMark;
                            }
                            else
                            {
                                ActiveMark = null;
                                ActiveLeg = null;
                                ActiveRoute = null;
                            }
                        }
                        else
                        {
                            ActiveMark = null;
                        }
                    }
                }
            }

            if (ActiveRoute != null)
            {
                if (ActiveLeg.NextLeg != null && TWD.IsValid())
                {
                    NTWA.Val = TWD.Average(Inst.BufTwoMin) - ActiveLeg.NextLeg.Bearing;
                    NTWA.SetValid();
                }
                else
                {
                    NTWA.Invalidate();
                }
            }

            #endregion

            #region Laylines

            //if (DRIFT.IsValid() && PERF.IsValid() && TWD.IsValid())
            //{
            //    double relset = SET.Average(Inst.BufTenMin) - TWD.Average(Inst.BufHalfMin);
            //    double dxs = TGTSPD.Average(Inst.BufHalfMin) * Math.Cos(TGTTWA.Average(Inst.BufHalfMin) * Math.PI / 180) + DRIFT.Average(Inst.BufTenMin) * Math.Cos(relset * Math.PI / 180);
            //    double dys = TGTSPD.Average(Inst.BufHalfMin) * Math.Sin(TGTTWA.Average(Inst.BufHalfMin) * Math.PI / 180) + DRIFT.Average(Inst.BufTenMin) * Math.Sin(relset * Math.PI / 180);

            //    TGTCOGs.Val = Math.Atan2(dys, dxs) * 180 / Math.PI + TWD.Average(Inst.BufHalfMin);
            //    TGTCOGs.SetValid(now);
            //    TGTSOGs.Val = Math.Sqrt(dxs * dxs + dys * dys);
            //    TGTSOGs.SetValid(now);

            //    double dxp = TGTSPD.Average(Inst.BufHalfMin) * Math.Cos(-TGTTWA.Average(Inst.BufHalfMin) * Math.PI / 180) + DRIFT.Average(Inst.BufTenMin) * Math.Cos(relset * Math.PI / 180);
            //    double dyp = TGTSPD.Average(Inst.BufHalfMin) * Math.Sin(-TGTTWA.Average(Inst.BufHalfMin) * Math.PI / 180) + DRIFT.Average(Inst.BufTenMin) * Math.Sin(relset * Math.PI / 180);

            //    TGTCOGp.Val = Math.Atan2(dyp, dxp) * 180 / Math.PI + TWD.Average(Inst.BufHalfMin);
            //    TGTCOGp.SetValid(now);
            //    TGTSOGp.Val = Math.Sqrt(dxp * dxp + dyp * dyp);
            //    TGTSOGp.SetValid(now);
            //}
            //else
            //{
            //    if (TGTCOGs.IsValid())
            //        TGTCOGs.Invalidate();
            //    if (TGTSOGs.IsValid())
            //        TGTSOGs.Invalidate();
            //    if (TGTCOGp.IsValid())
            //        TGTCOGp.Invalidate();
            //    if (TGTSOGp.IsValid())
            //        TGTSOGp.Invalidate();
            //}

            #endregion

        }

        public void CalcMeasure()
        {
            if (measureRange.FromLocation != null && measureRange.ToLocation != null)
            {
                double lat1 = measureRange.FromLocation.Latitude;
                double lon1 = measureRange.FromLocation.Longitude;
                double lat2 = measureRange.ToLocation.Latitude;
                double lon2 = measureRange.ToLocation.Longitude;

                measureResult.DST = CalcDistance(lat1, lon1, lat2, lon2) / 1852;
                double brg = CalcBearing(lat1, lon1, lat2, lon2);
                measureResult.BRG = (brg + 360) % 360;
                if (TWS.IsValid())
                    measureResult.TWA = TWD.Average(Inst.BufHalfMin) - brg;

                double vmc = 0;
                PolarPoint p = new PolarPoint();

                if (SOG.IsValid())
                    vmc = SOG.Average(Inst.BufHalfMin);

                if ( TWD.IsValid() && SPD.IsValid() && NavPolar.IsLoaded)
                {
                    double Angle = Math.Abs(measureResult.TWA % 360);
                    if (Angle > 180) Angle = 360 - Angle;
                    if (Angle < 50)
                    {
                        p = NavPolar.GetBeatTarget(TWS.Average(Inst.BufHalfMin));
                        vmc = p.SPD * Math.Cos(p.TWA * Math.PI / 180);
                    }
                    else
                        if (Angle > 140)
                        {
                            p = NavPolar.GetRunTarget(TWS.Average(Inst.BufHalfMin));
                            vmc = -p.SPD * Math.Cos(p.TWA * Math.PI / 180);
                        }
                        else
                            vmc = NavPolar.GetTarget(Angle, TWS.Average(Inst.BufHalfMin));
                }

                if (vmc != 0)
                    measureResult.TTG = TimeSpan.FromHours(measureResult.DST / vmc);
                else
                    measureResult.TTG = TimeSpan.FromHours(0);


            }
        }

        public static void CalcLongNav(DateTime now)
        {

            if (TWA.IsValid() && BRG.IsValid() && DRIFT.IsValid() && NavPolar.IsLoaded)
            {
                PolarPoint p = NavPolar.GetTargetVMC(TWS.Average(Inst.BufTwoMin), TWD.Average(Inst.BufTwoMin), BRG.Val, DRIFT.Average(Inst.BufTwoMin), SET.Average(Inst.BufTwoMin));
                TGTVMC.Val = p.SPD;
                TGTVMC.SetValid(now);
                TGTCTS.Val = TWD.Average(Inst.BufTwoMin) + p.TWA;
                TGTCTS.SetValid(now);
            }
        }

        public static void CalcRouteData()
        {
            //if (SelectedRoute != null)
            //{
            //    if (SelectedRoute.Legs.Count > 0)
            //    {
            //        Waypoint w0 = SelectedRoute.Legs[0].ToWpt;
            //        SelectedRoute.Legs[0].FromWpt = null;
            //        SelectedRoute.Legs[0].AccDist = "";
            //        SelectedRoute.Legs[0].Distance = "";
            //        SelectedRoute.Legs[0].Bearing = "";
            //        SelectedRoute.Legs[0].TWA = "";
            //        SelectedRoute.Legs[0].ETA = "";

            //        double accu = 0;

            //        for (int i = 1; i < SelectedRoute.Legs.Count; i++)
            //        {
            //            Waypoint w1 = SelectedRoute.Legs[i].ToWpt;
            //            SelectedRoute.Legs[i].FromWpt = w0;
            //            double distance = CalcDistance(w0.Lat, w0.Lon, w1.Lat, w1.Lon) / 1852;
            //            accu += distance;
            //            SelectedRoute.Legs[i].AccDist = accu.ToString("#.##");
            //            SelectedRoute.Legs[i].Distance = distance.ToString("#.##");
            //            double bearing = (CalcBearing(w0.Lat, w0.Lon, w1.Lat, w1.Lon) + 360) % 360;
            //            SelectedRoute.Legs[i].Bearing = bearing.ToString("#");
            //            if (TWD.IsValid())
            //            {
            //                double twangle = ((TWD.LongAverage - bearing) + 360) % 360;
            //                if (twangle > 180)
            //                    twangle = twangle - 360;
            //                SelectedRoute.Legs[i].TWA = twangle.ToString("#");
            //            }
            //            w0 = w1;
            //        }
            //    }
            //}
        }

    }
}